using LMComLib;
using LMNetLib;
using scorm;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace NewData {

  public static class Scorm {
    static Scorm() {

      /******************* LOGGER ***********************/
      Handlers.CmdService.registerCommand<Cmd_Logger, bool>(par => {
        Emailer.SendEMail(ConfigurationManager.AppSettings["Logger.emails"] ?? @"support@langmaster.com", "logger@langmaster.com", "Message from logger", "See attachment", new Emailer.Attachment("LogFile", Encoding.UTF8.GetBytes(par.data), "text/plain"));
        return new RpcResponse();
      });


      /* Cmd_resetModules */
      Handlers.CmdService.registerCommand<Cmd_resetModules, bool>(par => {
        resetModules(par);
        return new RpcResponse();
      });

      /* Cmd_readCrsResults */
      Handlers.CmdService.registerCommand<Cmd_readCrsResults, string[][]>(par => {
        return new RpcResponse(readCrsResults(par));
      });

      /* Cmd_readModuleResults */
      Handlers.CmdService.registerCommand<Cmd_readModuleResults, string>(par => {
        return new RpcResponse(readModuleResults(par));
      });

      /* Cmd_saveUserData */
      Handlers.CmdService.registerCommand<Cmd_saveUserData, bool>(par => {
        saveUserData(par);
        return new RpcResponse();
      });

      /* Cmd_getArchives */
      Handlers.CmdService.registerCommand<Cmd_createArchive, bool>(par => {
        return new RpcResponse(createArchive(par));
      });

      /* Cmd_testResults */
      Handlers.CmdService.registerCommand<Cmd_testResults, string[]>(par => {
        return new RpcResponse(testResults(par));
      });

      /* Cmd_TestCert */
      Handlers.CmdService.registerCommand<Cmd_testCert, bool>(par => {
        return new RpcResponse(testCert(par));
      });

    }

    static string[] testResults(Cmd_testResults par) {
      var db = Lib.CreateContext();
      return db.CourseDatas.
        Where(c => c.CourseUser.CompanyUser.UserId == par.lmcomId && c.CourseUser.CompanyUser.CompanyId == par.companyId && c.CourseUser.ProductId.StartsWith(par.productId) && c.Key == "result").
        Select(c => c.Data).
        ToArray();
    }

    static bool testCert(Cmd_testCert par) {
      var resStr = NewData.Scorm.getScormData(par, "result");
      testResultHelper test = Newtonsoft.Json.JsonConvert.DeserializeObject<testResultHelper>(resStr);
      GeneratePdfItem[] items = CreateCert(test, Langs.en_gb);
      //var designId = ReleaseDeploy.Lib.signature().cfg.designId;
      //!!!! 
      //11 /17/2015: ReleaseDeploy.Lib.adjustActConfig().designId prevedeno do DesignTimeLib. Je nutno zjistovat jinak 
      //radeji exception
      throw new Exception("//11 /17/2015: ReleaseDeploy.Lib.adjustActConfig().designId prevedeno do DesignTimeLib. Je nutno zjistovat jinak ");
      string designId = null; // ReleaseDeploy.Lib.adjustActConfig().designId;
      var skins = string.IsNullOrEmpty(designId) ? Enumerable.Empty<string>() : XExtension.Create(designId);
      var locs = par.loc == Langs.no ? Enumerable.Empty<string>() : XExtension.Create("_" + par.loc.ToString());
      string fn = null;
      foreach (var skin in skins.Concat(XExtension.Create("default"))) foreach (var loc in locs.Concat(XExtension.Create(""))) {
        fn = Machines.rootPath + string.Format(@"JsLib\skins\{0}\eTestMeCertifikat{1}.pdf", skin, loc);
        if (File.Exists(fn)) break;
      }
      var pdf = PdfGenerator.createPdf(fn, items);
      NewModel.Lib.downloadResponse(pdf, "application/pdf", "certificate.pdf");
      return true;
    }

    public class testResultHelper : testMe.result {
      public long started() { return this.skills.Select(sk => sk.started).Min(); }
      public long finished() { return this.skills.Select(sk => sk.finished).Max(); }
      public long elapsed() { return this.skills.Select(sk => sk.elapsed).Sum(); }
      public string dateTxt(CultureInfo ci) { return dateTxtProc(LowUtils.numToDate(this.started()), LowUtils.numToDate(this.finished()), ci); }
      public static string dateTxtProc(DateTime stDt, DateTime finDt, CultureInfo ci) {
        var stD = LowUtils.formatDateLow(stDt, ci); var stT = LowUtils.formatTimeLow(stDt, ci);
        var finD = LowUtils.formatDateLow(finDt, ci); var finT = LowUtils.formatTimeLow(finDt, ci);
        return LowUtils.IsTheSameDay(stDt, finDt) ? stD + " (" + stT + " - " + finT + ")" : stD + " " + stT + " - " + finD + " " + finT;
      }
      public string elapsedTxt() { return LowUtils.formatTimeSpan(this.elapsed()); }
      public string interruptsTxt() {
        if (this.interrupts == null || this.interrupts.Length == 0) return "0";
        var len = 0; this.interrupts.Select(it => it.end - it.beg).Sum();
        return this.interrupts.Length.ToString() + "x, duration " + LowUtils.formatTimeSpan(len);
      }
      //ipsTxt() {
      //  var ips = _.map(this.interrupts, it => it.ip); ips.push(this.ip); ips = _.uniq(ips);
      //  var huge = ips.length > 2; ips = ips.slice(0, 2); var res2 = ips.join(', ');
      //  return huge ? res2 + ',...' : res2;
      //}
      public string subTitleShort(CultureInfo ci) { return LowUtils.formatDateLow(LowUtils.numToDate(this.started()), ci); }
      public string subTitleLong(CultureInfo ci) { return this.title + ": " + subTitleShort(ci); }

      public void skrivanekHack(DateTime date) {
      }
    }

    public static GeneratePdfItem[] CreateCert(testResultHelper test, Langs lng) {
      var ci = new CultureInfo(lng.ToString().Replace('_', '-'));
      return XExtension.Create<GeneratePdfItem>(
        new GeneratePdfItem() { Name = "name", Value = string.Format("{0} {1}", test.firstName, test.lastName), IsBold = true, FontSize = 18 },
        new GeneratePdfItem() { Name = "level", Value = test.score.ToString(), IsBold = true, FontSize = 18 },
        new GeneratePdfItem() { Name = "level2", Value = string.Format("{0} ({1}%)", test.title, test.score), IsBold = false, FontSize = 11 },
        new GeneratePdfItem() { Name = "leveldetail", Value = test.title, IsBold = true, FontSize = 18 },
        new GeneratePdfItem() { Name = "start", Value = LowUtils.formatDate(test.started(), ci), IsBold = false, FontSize = 9 },
        new GeneratePdfItem() { Name = "endshort", Value = test.dateTxt(ci), IsBold = false, FontSize = 11 },
        new GeneratePdfItem() { Name = "end", Value = LowUtils.formatDate(test.finished(), ci), IsBold = false, FontSize = 9 },
        new GeneratePdfItem() { Name = "time", Value = test.elapsedTxt(), IsBold = false, FontSize = 9 },
        new GeneratePdfItem() { Name = "domain", Value = test.domain, IsBold = false, FontSize = 9 },
        new GeneratePdfItem() { Name = "id", Value = test.domain + "|" + test.id.ToString(), IsBold = false, FontSize = 9 },
        new GeneratePdfItem() { Name = "email", Value = test.eMail, IsBold = false, FontSize = 9 },
        new GeneratePdfItem() { Name = "invitator", Value = test.company, IsBold = false, FontSize = 9 }
      ).Concat(test.skills.SelectMany(m => moduleCert(m, ci))).ToArray();
    }

    static IEnumerable<GeneratePdfItem> moduleCert(testMe.skillResult mod, CultureInfo ci) {
      char id;
      switch (mod.skill) {
        case testMe.Skills.UseLanguage: id = 'g'; break;
        case testMe.Skills.Listening: id = 'l'; break;
        case testMe.Skills.Reading: id = 'r'; break;
        case testMe.Skills.Speaking: id = 's'; break;
        case testMe.Skills.Writing: id = 'w'; break;
        default: throw new NotImplementedException();
      }
      yield return new GeneratePdfItem() { Name = id + "_level", Value = mod.title, IsBold = false, FontSize = 11 };
      yield return new GeneratePdfItem() { Name = id + "_score", Value = mod.score().ToString(), IsBold = false, FontSize = 11 };
      yield return new GeneratePdfItem() { Name = id + "_start", Value = LowUtils.formatDate(mod.started, ci), IsBold = false, FontSize = 9 };
      yield return new GeneratePdfItem() { Name = id + "_end", Value = LowUtils.formatDate(mod.finished, ci), IsBold = false, FontSize = 9 };
      yield return new GeneratePdfItem() { Name = id + "_time", Value = LowUtils.formatTimeSpan(mod.elapsed), IsBold = false, FontSize = 9 };
      yield return new GeneratePdfItem() { Name = id + "_startend", Value = testResultHelper.dateTxtProc(LowUtils.numToDate(mod.started), LowUtils.numToDate(mod.finished), ci), IsBold = false, FontSize = 9 };
    }

    static void saveUserData(Cmd_saveUserData par) {
      if (par.data == null || par.data.Length == 0) return;
      var db = Lib.CreateContext();
      CourseUsers crsUser; CourseDatas[] cdatas = null; CompanyUsers compUser;
      if (Lib.adjustCourseUser(db, par.lmcomId, par.companyId, par.productId, out crsUser, out compUser)) {
        var keys = par.data.Select(d => d[0]).ToArray();
        cdatas = crsUser.CourseDatas.Where(d => keys.Contains(d.Key)).ToArray();
      } else
        cdatas = new CourseDatas[0];
      foreach (var kv in par.data) {
        var cdata = cdatas.FirstOrDefault(cd => cd.Key == kv[0]);
        if (cdata == null) db.CourseDatas.Add(cdata = new CourseDatas() {
          Key = kv[0],
          CourseUser = crsUser,
          Data = ""
        });
        cdata.ShortData = kv[1];
        cdata.Data = kv[2] ?? "";
        cdata.Flags = long.Parse(kv.Length <= 3 || kv[3] == null ? "0" : kv[3]);
      }
      Lib.SaveChanges(db);
    }

    static string readModuleResults(Cmd_readModuleResults par) {
      return getScormData(par, par.key);
    }

    static string[][] readCrsResults(Cmd_readCrsResults par) {
      var db = Lib.CreateContext();
      var idData = db.CourseDatas.
        Where(c => c.CourseUser.CompanyUser.UserId == par.lmcomId && c.CourseUser.CompanyUser.CompanyId == par.companyId && c.CourseUser.ProductId == par.productId && c.ShortData != null).
        Select(c => new { c.Key, c.ShortData }).
        ToArray();
      return idData.Select(d => new string[] { d.Key, d.ShortData }).ToArray();
    }

    //static int[] getArchives(Cmd_getArchives par) {
    //  return null;
    //}

    static int createArchive(Cmd_createArchive par) { //par.ProductId musi byt testGlobalAdmin, jinak vznikne neporadek v licencich
      var db = Lib.CreateContext();
      var crsUser = db.CourseUsers.Where(cu => cu.ProductId == par.productId && cu.CompanyUser.CompanyId == par.companyId && cu.CompanyUser.UserId == par.lmcomId).FirstOrDefault();
      if (crsUser == null) return 0;
      crsUser.ProductId = crsUser.ProductId + "|" + crsUser.Id.ToString();
      var licences = crsUser.UserLicences.Skip(1).ToArray(); //vsechny licence mimo prvni...
      if (licences.Length > 0) { //...preved na noveho CrsUsera
        var crsNewUser = new CourseUsers() {
          UserId = crsUser.UserId,
          Created = DateTime.UtcNow,
          ProductId = par.productId,
        };
        db.CourseUsers.Add(crsNewUser);
        foreach (var lic in licences) lic.CourseUser = crsNewUser;
      }
      Lib.SaveChanges(db);
      return crsUser.Id;
    }

    static void resetModules(Cmd_resetModules par) {
      var db = Lib.CreateContext();
      var all = db.CourseDatas.
        Where(c => c.CourseUser.CompanyUser.UserId == par.lmcomId && c.CourseUser.CompanyUser.CompanyId == par.companyId && c.CourseUser.ProductId == par.productId && c.ShortData != null).
        Select(c => new { c.Id, c.Key }).ToArray();
      var dict = par.modIds.ToDictionary(s => s, s => true);
      //var idCond = all.Where(ik => dict.ContainsKey(ik.Key)).Select(ik => ik.Id.ToString()).DefaultIfEmpty().Aggregate((r, i) => r + "," + i).ToArray();
      //db.Database.ExecuteSqlCommand("DELETE FROM courseDatas WHERE id in (" + idCond + ")");
      //EF7
      var idCond = all.Where(ik => dict.ContainsKey(ik.Key)).Select(ik => ik.Id).ToArray();
      db.CourseDatas.RemoveRange(db.CourseDatas.Where(cd => idCond.Contains(cd.Id)));
      Lib.SaveChanges(db);
    }

    //static void setScormData(ScormCmd par, string key, string data, string shortData, NewData.CourseDataFlag flags, Int64 date) {
    //  var db = lib.CreateContext();
    //  CourseUser crsUser; CourseData cdata = null; CompanyUser compUser;
    //  if (lib.adjustCourseUser(db, par.companyUserId, par.companyId, par.productId, out crsUser, out compUser)) {
    //    cdata = crsUser.CourseDatas.FirstOrDefault(d => d.Key == key);
    //  }
    //  if (cdata == null) db.CourseDatas.Add(cdata = new CourseData() {
    //    Key = key,
    //    CourseUser = crsUser
    //  });
    //  cdata.RowType = data; cdata.ShortData = shortData; cdata.Date = date; cdata.Flags = (long)flags;
    //  lib.SaveChanges(db);
    //}


    public static string getScormData(ScormCmd par, string key) {
      var db = Lib.CreateContext();
      return db.CourseDatas.
        Where(c =>
          c.CourseUser.CompanyUser.UserId == par.lmcomId &&
          c.CourseUser.CompanyUser.CompanyId == par.companyId &&
          c.CourseUser.ProductId == par.productId &&
          c.Key == key).
        Select(c => c.Data).FirstOrDefault();
    }

  }
}
