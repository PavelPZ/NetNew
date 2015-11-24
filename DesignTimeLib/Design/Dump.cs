using LMComLib;
using LMNetLib;
using Microsoft.Data.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

//Pouziti napr.
//using (var fs = File.OpenWrite(@"d:\temp\dump.xslt"))
//  NewData.Dump.dumpExcel (NewData.Dump.dumpRows(NewData.Dump.LANGMasterScorm()),fs);
//using (var fs = File.OpenWrite(@"d:\temp\dump.xslt"))
//  NewData.Dump.dumpExcel(NewData.Dump.dumpRows(NewData.Dump.Web()), fs);

namespace NewData {
  public static class Dump {
    public class scormEx {
      public string UserId { get; set; }
      public long AttemptId { get; set; }
      //public int AttemptId { get; set; }
      public string AttemptIdStr { get; set; }
      public Guid? AttemptIdGuid { get; set; }
      public string Key1Str { get; set; }
      public string Data1 { get; set; }
    }
    public static IEnumerable<schools.dumpData> LANGMasterScorm() {
      //EF7
      yield break;
      //var db = Lib.CreateContext();
      //var data = db.Database.SqlQuery<scormEx>("SELECT * FROM LANGMasterScorms WHERE Key1Int=1").ToArray();
      ////var data = db.Database.SqlQuery<scormEx>("SELECT * FROM [ScormExes] WHERE Key1Int=1").ToArray();
      //return data.Select(d => new schools.dumpData() {
      //  UserId = d.UserId,
      //  AttemptId = d.AttemptId != 0 ? d.AttemptId.ToString() : (d.AttemptIdStr != null ? d.AttemptIdStr : (d.AttemptIdGuid != null ? d.AttemptIdStr.ToString() : null)),
      //  ModuleId = d.Key1Str,
      //  ModuleData = Newtonsoft.Json.JsonConvert.DeserializeObject<schools.ModUser>(Encoding.UTF8.GetString(Convert.FromBase64String(d.Data1))),
      //});
    }
    public static IEnumerable<schools.dumpData> Web() {
      var db = Lib.CreateContext();
      return db.CourseDatas.Where(cd => cd.Key != "@meta").Include(c => c.CourseUser).Include(c => c.CourseUser.CompanyUser).Include(c => c.CourseUser.CompanyUser.User).ToArray().Select(d => new schools.dumpData() {
        UserId = d.CourseUser.CompanyUser.UserId.ToString(),
        UserInfo = d.CourseUser.CompanyUser.User.EMail + ", " + d.CourseUser.CompanyUser.User.FirstName + ", " + d.CourseUser.CompanyUser.User.LastName,
        CompanyId = d.CourseUser.CompanyUser.CompanyId,
        AttemptId = d.CourseUser.ProductId.ToString(),
        ModuleId = d.Key,
        ModuleData = Newtonsoft.Json.JsonConvert.DeserializeObject<schools.ModUser>(d.Data),
      });
    }

    public static void dumpExcel(IEnumerable<schools.dumpRow> datas, Stream str) {
      var cols = new ExcelColumns(typeof(schools.dumpRow));
      foreach (var key in cols.Keys.Where(k => k.EndsWith("Start") || k.EndsWith("End"))) {
        var col = cols[key];
        col.Format = ExcelFormat.Date;
        col.getData = d => LowUtils.IntToDate((long)d);
      }
      ExcelExport.linqToExcel(datas, str, cols);
    }
    public static IEnumerable<schools.dumpRow> dumpRows(IEnumerable<schools.dumpData> datas) {
      var Data = datas.SelectMany(m => m.ModuleData.pages.
          Where(p => p != null && p.st == ExerciseStatus.Evaluated).
          Select(p => new { m, p, d = findCopy(m.ModuleId, p.i) })).
          ToArray();
      //init exercise record
      foreach (var ex in Data) {
        ex.d.CompanyId = ex.m.CompanyId;
        ex.d.UserId = ex.m.UserId;
        ex.d.UserInfo = ex.m.UserInfo;
        ex.d.AttemptId = ex.m.AttemptId;
        ex.d.exScore = ex.p.s == null || ex.p.ms == 0 ? -1 : (int)(100.0 * ex.p.s / ex.p.ms);
        ex.d.modStart = ex.m.ModuleData.bt;
        ex.d.modEnd = ex.m.ModuleData.et;
      }
      //agregate module
      foreach (var g in Data.GroupBy(ex => new { ex.d.AttemptId, ex.d.modId })) {
        foreach (var ex in g) {
          ex.d.modPercent = (int)(100.0 * g.Count() / ex.d.modExs);
          ex.d.modSec = (int)((double)ex.m.ModuleData.t / 1000);
          var evals = g.Where(e => e.d.exScore >= 0);
          ex.d.modScore = evals.Count() == 0 ? -1 : (int)((double)evals.Sum(e => e.d.exScore) / evals.Count());
        }
      }
      //agregate Lesson
      foreach (var g in Data.GroupBy(ex => new { ex.d.AttemptId, ex.d.lessonId })) {
        foreach (var ex in g) {
          var mods = g.GroupBy(e => e.d.modId).Select(m => m.First()).ToArray();
          ex.d.lessonSec = mods.Sum(m => m.d.modSec);
          ex.d.lessonStart = mods.Min(m => m.d.modStart);
          ex.d.lessonEnd = mods.Min(m => m.d.modEnd);
          ex.d.lessonPercent = (int)(100.0 * g.Count() / ex.d.lessonExs);
          var evals = g.Where(e => e.d.exScore >= 0);
          ex.d.lessonScore = evals.Count() == 0 ? -1 : (int)((double)evals.Sum(e => e.d.exScore) / evals.Count());
        }
      }
      //agregate Level
      foreach (var g in Data.GroupBy(ex => new { ex.d.AttemptId, ex.d.levelId })) {
        foreach (var ex in g) {
          var less = g.GroupBy(e => e.d.lessonId).Select(m => m.First()).ToArray();
          ex.d.levelSec = less.Sum(m => m.d.lessonSec);
          ex.d.levelStart = less.Min(m => m.d.lessonStart);
          ex.d.levelEnd = less.Min(m => m.d.lessonEnd);
          ex.d.levelPercent = (int)(100.0 * g.Count() / ex.d.levelExs);
          var evals = g.Where(e => e.d.exScore >= 0);
          ex.d.levelScore = evals.Count() == 0 ? -1 : (int)((double)evals.Sum(e => e.d.exScore) / evals.Count());
        }
      }
      //agregate Course
      foreach (var g in Data.GroupBy(ex => ex.d.AttemptId)) {
        foreach (var ex in g) {
          var levs = g.GroupBy(e => e.d.levelId).Select(m => m.First()).ToArray();
          ex.d.courseSec = levs.Sum(m => m.d.levelSec);
          ex.d.courseStart = levs.Min(m => m.d.levelStart);
          ex.d.courseEnd = levs.Min(m => m.d.levelEnd);
          ex.d.coursePercent = (int)(100.0 * g.Count() / ex.d.courseExs);
          var evals = g.Where(e => e.d.exScore >= 0);
          ex.d.courseScore = evals.Count() == 0 ? -1 : (int)((double)evals.Sum(e => e.d.exScore) / evals.Count());
        }
      }
      return Data.Select(d => d.d);
    }
    static schools.dumpRow findCopy(string modId, int exId) {
      var d = exDir[modId + ":" + exId];
      schools.dumpRow res = new schools.dumpRow();
      foreach (var fld in typeof(schools.dumpRow).GetProperties())
        fld.SetValue(res, fld.GetValue(d));
      return res;
    }
    static Dictionary<string, schools.dumpRow> exDir {
      get {
        //if (_exDir == null) {
        //  ProductsDefine.lib.init();
        //  XElement courses = ProductsDefine.lib.root; // XElement.Load(Machines.lmcomData + @"Statistic_CourseStructureEx.xml");
        //  _exDir = courses.Descendants("page").
        //    Select(p => new {
        //      modId = LowUtils.JSONToId(p.Parent.AttributeValue("spaceId"), p.Parent.AttributeValue("globalId")),
        //      exId = p.Parent.Elements().IndexOf(e => e == p) + 1,
        //      el = p
        //    }).
        //    ToDictionary(kv => kv.modId + ":" + kv.exId, kv => new schools.dumpRow() {
        //      exId = kv.exId,
        //      testEx = kv.el.AttributeValue("title"),
        //      modId = int.Parse(kv.el.Parent.AttributeValue("order")??"0"),
        //      modExs = int.Parse(kv.el.Parent.AttributeValue("exnum")??"0"),
        //      mod = kv.el.Parent.AttributeValue("title"),
        //      lessonId = int.Parse(kv.el.Parent.Parent.AttributeValue("order") ?? "0"),
        //      lesson = kv.el.Parent.Parent.AttributeValue("title"),
        //      email = int.Parse(kv.el.Parent.Parent.Parent.AttributeValue("order") ?? "0"),
        //      level = kv.el.Parent.Parent.Parent.AttributeValue("title"),
        //      course = LowUtils.EnumParse<CourseIds>(kv.el.Parent.Parent.Parent.Parent.AttributeValue("email")),
        //    });
        //  foreach (var lg in _exDir.Values.GroupBy(testEx => new { testEx.course, testEx.lessonId })) {
        //    var sum = lg.GroupBy(e => e.modId).Select(m => m.First()).Sum(m => m.modExs);
        //    foreach (var l in lg) l.lessonExs = sum;
        //  }
        //  foreach (var lg in _exDir.Values.GroupBy(testEx => new { testEx.course, testEx.email })) {
        //    var sum = lg.GroupBy(e => e.lessonId).Select(m => m.First()).Sum(m => m.lessonExs);
        //    foreach (var l in lg) l.levelExs = sum;
        //  }
        //  foreach (var lg in _exDir.Values.GroupBy(testEx => testEx.course)) {
        //    var sum = lg.GroupBy(e => e.email).Select(m => m.First()).Sum(m => m.levelExs);
        //    foreach (var l in lg) l.courseExs = sum;
        //  }
        //}
        //var cnt = 0;
        //foreach (var testEx in _exDir.Values.OrderBy(e => e.course).ThenBy(e => e.modId)) testEx.Order = cnt++;
        return _exDir;
      }
    } static Dictionary<string, schools.dumpRow> _exDir;
  }
}
