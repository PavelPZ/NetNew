using LMComLib;
using LMNetLib;
using NewData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;

namespace excelReport {

  public class courseResultTypeConverter : JsonConverter {
    public override bool CanConvert(Type objectType) {
      return objectType == typeof(object);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
      JObject jObject = JObject.Load(reader);
      var res = new Dictionary<string, CourseModel.Result>();
      foreach (var prop in jObject.Properties()) {
        var item = (JObject)prop.Value;
        switch ((string)item.Property("tg")) {
          case "writing":
            var wr = serializer.Deserialize<CourseModel.WritingResult>(item.CreateReader()); wr.adjustNetDates();
            res.Add(prop.Name, wr);
            break;
          case "audio-capture":
            var ac = serializer.Deserialize<CourseModel.audioCaptureResult>(item.CreateReader()); ac.adjustNetDates();
            res.Add(prop.Name, ac);
            break;
        }
      }
      return res;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
      throw new NotImplementedException();
    }
  }

  public static class evaluatorsReport {
    public const string dEvaluators = "dataEvaluators";
    public const string dEvalPayment = "dataEvalPayment";
    public const string templateFn = @"App_Data\Reports\evaluators.xlsx";

    public static byte[] export(int companyId) {
      using (var xlsx = new xlsxFile(Machines.rootPath + templateFn)) {
        var db = Lib.CreateContext();
        //data z databaze
        var flag = (long)(CourseModel.CourseDataFlag.pcCannotEvaluate | CourseModel.CourseDataFlag.testEx); var flagNo = (long)(CourseModel.CourseDataFlag.needsEval);
        var resStr = db.CourseDatas.Where(cd => cd.CourseUser.CompanyUser.CompanyId == companyId && (cd.Flags & flag) == flag && (cd.Flags & flagNo) == 0 && !string.IsNullOrEmpty(cd.Data)).
          Select(cd => new {
            courseResult = cd.Data,
            cd.Id,
            //FE7
            //cd.RowVersion,
            cd.CourseUser.ProductId,
            cd.CourseUser.HumanAssigned,
            cd.CourseUser.HumanCompanyUserId,
          }).ToArray();
        //rozlezeni JSON s daty a obohaceni o produkt
        var converter = new courseResultTypeConverter();
        var resObjs = resStr.Select(o => new {
          ctrlResults = ((Dictionary<string, CourseModel.Result>)Newtonsoft.Json.JsonConvert.DeserializeObject<object>(o.courseResult, converter)).Values.OfType<CourseModel.HumanEvalResult>().ToArray(),
          prod = CourseMeta.LibLow.getRuntimeProd(o.ProductId),
          HumanAssigned = LowUtils.dateToNum(o.HumanAssigned),
          o.HumanCompanyUserId,
          //RowVersion = BitConverter.ToUInt64(o.RowVersion.Reverse().ToArray(), 0),
          o.Id
        }).Where(o => o.prod != null); //.ToArray();
        //SelectMany na controls Result
        var resObjs2 = resObjs.SelectMany(o => o.ctrlResults.Where(cr => cr.hLmcomId > 0).Select(cr => new {
          ctrlResult = cr,
          o.HumanCompanyUserId,
          o.HumanAssigned,
          o.prod,
          //o.RowVersion,
          o.Id
        })).ToArray();
        //Evaluators z databaze
        var evalIds = resObjs2.Select(o => o.ctrlResult.hLmcomId).Distinct().ToArray();
        var assignedIds = resObjs2.Select(o => o.HumanCompanyUserId).Distinct().ToArray();
        var evaluatorsLow = db.CompanyUsers.Where(cu => evalIds.Contains(cu.UserId) || assignedIds.Contains(cu.Id)).Select(cu => new { cu.Id, cu.UserId, cu.User.EMail, cu.User.FirstName, cu.User.LastName }).ToArray();
        var evaluatorsLMComId = evaluatorsLow.ToDictionary(e => e.UserId);
        var evaluatorsCompUserId = evaluatorsLow.ToDictionary(e => e.Id);
        //Obohaceni o evaluators a dokonceni
        var resObjs3 = resObjs2.Select(o => new {
          assignedUser = evaluatorsCompUserId[o.HumanCompanyUserId],
          evalUser = evaluatorsLMComId[o.ctrlResult.hLmcomId],
          humanDate = o.ctrlResult.hDate,
          tAssigned = new dataHelper(0, (int)o.HumanAssigned, 0),
          tEvaluated = new dataHelper(0, o.ctrlResult.hDate, 0),
          tDelay = new timeHelper((int)(o.ctrlResult.hDate - o.HumanAssigned), 0),
          o.prod,
          o.ctrlResult,
          //o.RowVersion,
          o.Id
        }).ToArray();
        //***************** dEvalPayment
        // sloupce pro vzorce
        // B1 Last Payment ID
        // D rowVersion | E levelId (1 pro A1...) | F actLevel (0 pro stare rowVersion nebo levelId) | G isSpeaking (1 x 0) | H recorded | J isWriting (1 x 0) | K words
        var rows = lib.emptyAndHeader(resObjs3).Select(t => new object[] {
          t==null ? (object)"email" : t.evalUser.EMail,
          t==null ? (object)"firstName" : t.evalUser.FirstName,
          t==null ? (object)"lastName" : t.evalUser.LastName,

          //t==null ? (object)"recordVersion" : t.RowVersion,
          t==null ? (object)"levelId" : levelIds[t.ctrlResult.hLevel] + 1,
          t==null ? (object)"actLevel" : new lib.actionFormula((r,c) => { return string.Format("=IF(D{0}<=$B$1,0,E{0})",r,c);}), //dej levelId nebo 0 pro stare rowVersion
          t==null ? (object)"isSpeaking" : t.ctrlResult.tg=="audio-capture" ? 1 : 0,
          t==null ? (object)"recorded" : new lib.formatedValue(t.ctrlResult is CourseModel.audioCaptureResult ? (double)(((CourseModel.audioCaptureResult)t.ctrlResult).recordedMilisecs) / msecPerDay : 0, lib.cellFormat.time),
          t==null ? (object)"priceRecorder" : new lib.formatedValue(
            new lib.actionFormula((r,c) => { return string.Format("H{0}*60*24*INDIRECT(ADDRESS(3,2 + F{0})) + G{0}*INDIRECT(ADDRESS(5,2 + F{0}))",r,c);}),
            lib.cellFormat.price),
          t==null ? (object)"isWriting" : t.ctrlResult.tg=="writing" ? 1 : 0,
          t==null ? (object)"words" : t.ctrlResult is CourseModel.WritingResult ? ((CourseModel.WritingResult)t.ctrlResult).words : 0,
          t==null ? (object)"priceWords" : new lib.formatedValue(
            new lib.actionFormula((r,c) => { return string.Format("K{0}*INDIRECT(ADDRESS(4,2 + F{0})) + J{0}*INDIRECT(ADDRESS(6,2 + F{0}))",r,c);}),
            lib.cellFormat.price),
          t==null ? (object)"recordId" : t.Id,
          t==null ? (object)"evaluated" : new lib.formatedValue(t.tEvaluated.date, lib.cellFormat.dateTime),
          t==null ? (object)"evaluatedYear" : t.tEvaluated.year,
          t==null ? (object)"evaluatedQuarterStr" : t.tEvaluated.quarterStr,
          t==null ? (object)"evaluatedMonth" : t.tEvaluated.monthStr,
          t==null ? (object)"evaluatedWeek" : t.tEvaluated.week,
          t==null ? (object)"level" : t.ctrlResult.hLevel,
          t==null ? (object)"prodLine" : t.prod.line.ToString(),
          t==null ? (object)"assigned" : new lib.formatedValue(t.tAssigned.date, lib.cellFormat.dateTime),
          t==null ? (object)"evalWaited" : new lib.formatedValue(t.evalUser.Id!=t.assignedUser.Id ? 0 : t.tDelay.time.TotalDays, lib.cellFormat.time),
          t==null ? (object)"score" : new lib.formatedValue(t.ctrlResult.hPercent, lib.cellFormat.percent),
          t==null ? (object)"userId" : t.evalUser.UserId,
          t==null ? (object)"prodId" : t.prod.url,
          t==null ? (object)"prodTitle" : t.prod.title,
        });
        //fill sheet
        ExcelWorksheet sheet = lib.prepareSheet(xlsx.package, dEvalPayment, 10);
        var range = lib.import(sheet, rows, 10);
        sheet.Names.Add(lib.dDataAll, range);

        return xlsx.result;
      }
    }
    const int msecPerDay = 24 * 3600 * 1000;
    static Dictionary<string, int> levelIds = new Dictionary<string, int>() { { "A1", 0 }, { "A2", 1 }, { "B1", 2 }, { "B2", 3 }, { "C1", 4 }, { "C2", 5 } };
    public static void test(string fn, int companyId) {
      if (File.Exists(fn)) File.Delete(fn);
      using (var fs = File.OpenWrite(fn)) {
        var data = export(companyId);
        fs.Write(data, 0, data.Length);
      }
    }
  }

  public class xlsxFile : IDisposable {
    public xlsxFile(string template) {
      resultMs = new MemoryStream(); MemoryStream templateMs = new MemoryStream();
      using (var fs = File.OpenRead(template)) fs.CopyTo(templateMs); templateMs.Seek(0, SeekOrigin.Begin);
      package = new ExcelPackage(resultMs, templateMs);
    }
    void IDisposable.Dispose() {
      if (package == null) return;
      package.Dispose(); package = null;
    }
    public ExcelPackage package;
    MemoryStream resultMs;
    public byte[] result {
      get {
        package.Save(); package.Dispose(); package = null;
        return resultMs.ToArray();
      }
    }
  }

  public static class tests {
    public const string dTests = "dataTests";
    public const string dSkills = "dataSkills";
    public const string dIntpsIpDist = "dataIntpsIpDist"; //ruzna IP pro testGlobalAdmin
    public const string dIntpsIp2 = "dataIntpsIp2"; //alespon 2 ruzna IP pro testGlobalAdmin
    public const string dIntps2 = "dataIntps2"; //alespon 2 preruseni pro testGlobalAdmin
    public const string templateFn = @"App_Data\Reports\tests.xlsx";

    public static byte[] export(int companyId) {
      using (var xlsx = new xlsxFile(Machines.rootPath + templateFn)) {
        //doRead all testMe.result from DB
        var db = Lib.CreateContext();
        var flag = (long)CourseModel.CourseDataFlag.testImpl_result; var flagNo = (long)CourseModel.CourseDataFlag.needsEval;
        departmentNode root; var departments = lib.readDepartment(companyId, out root);
        //vsechny company licence 
        var lics = db.UserLicences.Where(ul => ul.CompanyLicence.CompanyId == companyId).Select(ul => new licence { UserId = ul.UserId, LicenceId = ul.LicenceId, Created = ul.CompanyLicence.Created }).ToLookup(ul => ul.UserId);
        testMe.result temp;
        var testResults = db.CourseDatas.
          Where(cd => cd.CourseUser.CompanyUser.CompanyId == companyId && (cd.Flags & flag) == flag && (cd.Flags & flagNo) == 0).
          Select(cd => new { cd.Data, cd.CourseUserId, cd.CourseUser.CompanyUser.DepartmentId }).
          ToArray().
          Select(str => new testRow {
            lics = lics[str.CourseUserId].ToArray(),
            department = str.DepartmentId == null ? departmentNode._threeLevelsTitles : departments[(int)str.DepartmentId].threeLevelsTitles(),
            data = temp = JsonConvert.DeserializeObject<testMe.result>(str.Data),
            prod = CourseMeta.LibLow.getRuntimeProd(temp.productUrl)
          }).Where(o => o.prod != null); //.ToArray();
        //naplneni excel souboru
        fillTests(testResults, xlsx.package);
        fillSkills(testResults, xlsx.package);
        fillInterrupts(testResults, xlsx.package, dIntpsIpDist);
        fillInterrupts(testResults, xlsx.package, dIntpsIp2);
        fillInterrupts(testResults, xlsx.package, dIntps2);
        return xlsx.result;
      }
    }

    static void fillInterrupts(IEnumerable<testRow> src, ExcelPackage package, string filter) {
      var dataRows = src.SelectMany(t => filterIntps(t.data, filter).Select(s => new { interrupt = s, testSrc = t })).Select(sk => new {
        isHeader = false,
        src = sk.interrupt,
        testSrc = sk.testSrc,
        ipParts = IPPart(sk.interrupt.ip),
        tEnd = new dataHelper(0, sk.testSrc.data.skills.Select(s => s.finished).Max(), 0),
        userName = lib.userName(sk.testSrc.data.eMail, sk.testSrc.data.firstName, sk.testSrc.data.lastName),
        tElapsed = new timeHelper(sk.interrupt.end - sk.interrupt.beg, 0)
      }).ToArray();
      var rows = lib.emptyAndHeader(dataRows).Select(t => new object[] {
        t==null ? (object)"testId" : t.userName + ": " + t.testSrc.prod.title + " (" + t.testSrc.data.id + ")",
        t==null ? (object)"ipAddress" : t.src.ip,
        t==null ? (object)"prodLine" :t.testSrc.prod.line.ToString(),
        t==null ? (object)"prodLevel" :t.testSrc.data.level,
        t==null ? (object)"licenceId" : licId(t.testSrc),
        t==null ? (object)"elapsed" : new lib.formatedValue(t.tElapsed.time.TotalDays, lib.cellFormat.time),
        t==null ? (object)"ip1" : t.ipParts[0],
        t==null ? (object)"ip2" : t.ipParts[1],
        t==null ? (object)"ip3" : t.ipParts[2],
        t==null ? (object)"ip4" : t.ipParts[3],
        t==null ? (object)"name" : t.userName,
        t==null ? (object)"prodTitle" : t.testSrc.prod.title,
        t==null ? (object)"end" : new lib.formatedValue(t.tEnd.date, lib.cellFormat.date),
        t==null ? (object)"endTime" : t.tEnd.date,
        t==null ? (object)"endYear" : t.tEnd.year,
        t==null ? (object)"endQuarter" : t.tEnd.quarter,
        t==null ? (object)"endQuarterStr" : t.tEnd.quarterStr,
        t==null ? (object)"endMonth" : t.tEnd.month,
        t==null ? (object)"endMonthStr" : t.tEnd.monthStr,
        t==null ? (object)"endWeek" : t.tEnd.week,
        t==null ? (object)"elapsedStr" : t.tElapsed.timeStr,
        t==null ? (object)"elapsedHour" : t.tElapsed.hour,
        t==null ? (object)"elapsedMinute" : t.tElapsed.minute,
        t==null ? (object)"elapsedSecond" : t.tElapsed.second,
        t==null ? (object)"elapsedTotalSecond" : t.tElapsed.totalSecond,
        t==null ? (object)"depLev1" : t.testSrc.department[0],
        t==null ? (object)"depLev2" : t.testSrc.department[1],
        t==null ? (object)"depLev3" : t.testSrc.department[2],
      });
      ExcelWorksheet sheet = lib.prepareSheet(package, filter);
      var range = lib.import(sheet, rows);
      sheet.Names.Add(lib.dDataAll, range);
    }

    static IEnumerable<testMe.interrupt> filterIntps(testMe.result src, string id) {
      switch (id) {
        case dIntpsIpDist: foreach (var it in allIntps(src).DistinctBy(s => s.ip)) yield return it; break; //distinct
        case dIntpsIp2: var allDist = allIntps(src).DistinctBy(s => s.ip).ToArray(); if (allDist.Length <= 1) yield break; foreach (var it in allDist) yield return it; break; //alespon dve ruzne IP
        case dIntps2:
          var allInts = allIntps(src).ToArray(); if (allInts.Length <= 1) yield break;
          foreach (var it in allInts.Skip(1)) yield return it; break; //alespon dve ruzne IP
        default: throw new NotImplementedException();
      }
    }

    static IEnumerable<testMe.interrupt> allIntps(testMe.result src) {
      var beg = src.skills.Select(s => s.started).Min();
      yield return new testMe.interrupt { beg = beg, end = beg, ip = src.ip };
      if (src != null) foreach (var s in src.interrupts) yield return s;
    }

    static List<string> IPPart(string ip) {
      var parts = ip.Split('.').ToList();
      while (parts.Count < 4) parts.Add("-");
      return parts;
    }

    static void fillSkills(IEnumerable<testRow> src, ExcelPackage package) {
      var dataRows = src.SelectMany(t => t.data.skills.Select(s => new { skill = s, testSrc = t })).Select(sk => new {
        isHeader = false,
        src = sk.skill,
        testSrc = sk.testSrc,
        tStart = new dataHelper(0, sk.skill.started, 0),
        tEnd = new dataHelper(0, sk.skill.finished, 0),
        tElapsed = new timeHelper(sk.skill.elapsed, 0),
      }).ToArray();
      var rows = lib.emptyAndHeader(dataRows).Select(t => new object[] {
        t==null ? (object)"name" : lib.userName(t.testSrc.data.eMail, t.testSrc.data.firstName, t.testSrc.data.lastName),
        t==null ? (object)"prodTitle" : t.testSrc.prod.title,
        t==null ? (object)"skill" :t.src.skill,
        t==null ? (object)"prodLine" :t.testSrc.prod.line.ToString(),
        t==null ? (object)"prodLevel" :t.testSrc.data.level,
        t==null ? (object)"licenceId" : licId(t.testSrc),
        t==null ? (object)"email" : t.testSrc.data.eMail,
        t==null ? (object)"firstName" : t.testSrc.data.firstName,
        t==null ? (object)"lastName" : t.testSrc.data.lastName,
        t==null ? (object)"testId" : t.testSrc.data.id,
        t==null ? (object)"score" : new lib.formatedValue(Math.Round((decimal)t.src.score() / 100, 2), lib.cellFormat.percent),
        t==null ? (object)"prodId" : t.testSrc.data.productUrl,
        t==null ? (object)"start" : new lib.formatedValue(t.tStart.date, lib.cellFormat.date),
        t==null ? (object)"startTime" : new lib.formatedValue(t.tStart.date, lib.cellFormat.dateTime),
        t==null ? (object)"startYear" : t.tStart.year,
        t==null ? (object)"startQuarter" : t.tStart.quarter,
        t==null ? (object)"startQuarterStr" : t.tStart.quarterStr,
        t==null ? (object)"startMonth" : t.tStart.month,
        t==null ? (object)"startMonthStr" : t.tStart.monthStr,
        t==null ? (object)"startWeek" : t.tStart.week,
        t==null ? (object)"end" :new lib.formatedValue( t.tEnd.date, lib.cellFormat.date),
        t==null ? (object)"endTime" : new lib.formatedValue( t.tEnd.date, lib.cellFormat.dateTime),
        t==null ? (object)"endYear" : t.tEnd.year,
        t==null ? (object)"endQuarter" : t.tEnd.quarter,
        t==null ? (object)"endQuarterStr" : t.tEnd.quarterStr,
        t==null ? (object)"endMonth" : t.tEnd.month,
        t==null ? (object)"endMonthStr" : t.tEnd.monthStr,
        t==null ? (object)"endWeek" : t.tEnd.week,
        t==null ? (object)"elapsed" : new lib.formatedValue( t.tElapsed.time.TotalDays, lib.cellFormat.time),
        t==null ? (object)"elapsedStr" : t.tElapsed.timeStr,
        t==null ? (object)"elapsedHour" : t.tElapsed.hour,
        t==null ? (object)"elapsedMinute" : t.tElapsed.minute,
        t==null ? (object)"elapsedSecond" : t.tElapsed.second,
        t==null ? (object)"elapsedTotalSecond" : t.tElapsed.totalSecond,
        t==null ? (object)"depLev1" : t.testSrc.department[0],
        t==null ? (object)"depLev2" : t.testSrc.department[1],
        t==null ? (object)"depLev3" : t.testSrc.department[2],
      });
      ExcelWorksheet sheet = lib.prepareSheet(package, dSkills);
      var range = lib.import(sheet, rows);
      sheet.Names.Add(lib.dDataAll, range);
    }

    static string certUrl(int companyId, Int64 lmcomId, string productUrl) {
      var par = new scorm.Cmd_testCert { companyId = companyId, lmcomId = lmcomId, productId = productUrl };
      return string.Format("{0}?type={1}&par={2}", Machines.fullHttpUrl("~/service.ashx"), HttpUtility.UrlEncode(par.GetType().FullName), Newtonsoft.Json.JsonConvert.SerializeObject(par));
    }

    static void fillTests(IEnumerable<testRow> src, ExcelPackage package) {
      var dataRows = src.Select(t => new {
        isHeader = false,
        src = t,
        tStart = new dataHelper(0, t.data.skills.Select(s => s.started).Min(), 0),
        tEnd = new dataHelper(0, t.data.skills.Select(s => s.finished).Max(), 0),
        tElapsed = new timeHelper(t.data.skills.Select(s => s.elapsed).Sum(), 0)
      }).ToArray();
      var rows = lib.emptyAndHeader(dataRows).Select(t => new object[] {
        t==null ? (object)"name" : lib.userName(t.src.data.eMail, t.src.data.firstName, t.src.data.lastName),
        t==null ? (object)"certificate" : certUrl(t.src.data.companyId, t.src.data.lmcomId, t.src.data.productUrl),
        t==null ? (object)"email" : t.src.data.eMail,
        t==null ? (object)"firstName" : t.src.data.firstName,
        t==null ? (object)"lastName" : t.src.data.lastName,
        t==null ? (object)"id" : t.src.data.id,
        t==null ? (object)"licenceId" : licId(t.src),
        t==null ? (object)"score" : new lib.formatedValue(Math.Round((decimal)t.src.data.score / 100, 2), lib.cellFormat.percent),
        t==null ? (object)"scoreInterval" : scoreInterval.find(t.src.data.score).title,
        t==null ? (object)"prodId" : t.src.data.productUrl,
        t==null ? (object)"prodTitle" : t.src.prod.title,
        t==null ? (object)"prodLine" :t.src.prod.line.ToString(),
        t==null ? (object)"prodLevel" :t.src.data.level,
        t==null ? (object)"start" : new lib.formatedValue(t.tStart.date, lib.cellFormat.date),
        t==null ? (object)"startTime" : new lib.formatedValue(t.tStart.date, lib.cellFormat.dateTime),
        t==null ? (object)"startYear" : t.tStart.year,
        t==null ? (object)"startQuarter" : t.tStart.quarter,
        t==null ? (object)"startQuarterStr" : t.tStart.quarterStr,
        t==null ? (object)"startMonth" : t.tStart.month,
        t==null ? (object)"startMonthStr" : t.tStart.monthStr,
        t==null ? (object)"startWeek" : t.tStart.week,
        t==null ? (object)"end" :new lib.formatedValue( t.tEnd.date, lib.cellFormat.date),
        t==null ? (object)"endTime" : new lib.formatedValue( t.tEnd.date, lib.cellFormat.dateTime),
        t==null ? (object)"endYear" : t.tEnd.year,
        t==null ? (object)"endQuarter" : t.tEnd.quarter,
        t==null ? (object)"endQuarterStr" : t.tEnd.quarterStr,
        t==null ? (object)"endMonth" : t.tEnd.month,
        t==null ? (object)"endMonthStr" : t.tEnd.monthStr,
        t==null ? (object)"endWeek" : t.tEnd.week,
        t==null ? (object)"elapsed" : new lib.formatedValue( t.tElapsed.time.TotalDays, lib.cellFormat.time),
        t==null ? (object)"elapsedStr" : t.tElapsed.timeStr,
        t==null ? (object)"elapsedHour" : t.tElapsed.hour,
        t==null ? (object)"elapsedMinute" : t.tElapsed.minute,
        t==null ? (object)"elapsedSecond" : t.tElapsed.second,
        t==null ? (object)"elapsedTotalSecond" : t.tElapsed.totalSecond,
        t==null ? (object)"depLev1" : t.src.department[0],
        t==null ? (object)"depLev2" : t.src.department[1],
        t==null ? (object)"depLev3" : t.src.department[2],
      });//.ToArray();
      ExcelWorksheet sheet = lib.prepareSheet(package, dTests);
      var range = lib.import(sheet, rows);
      sheet.Names.Add(lib.dDataAll, range);
    }

    static string licId(testRow row) {
      if (row.lics.Length == 0) return "-";
      var l = row.lics.First();
      return string.Format("{0} ({1})", l.LicenceId, l.Created.ToString(dataHelper.actCult.DateTimeFormat.ShortDatePattern));
    }

    public class testRow {
      public testMe.result data;
      public CourseMeta.product prod;
      public List<string> department;
      public licence[] lics;
      public static testRow fake;
    }

    public class licence {
      public int UserId;
      public int LicenceId;
      public DateTime Created;
    }

    public static void test(string fn, int companyId) {
      if (File.Exists(fn)) File.Delete(fn);
      using (var fs = File.OpenWrite(fn)) {
        var data = export(companyId);
        fs.Write(data, 0, data.Length);
      }
    }
  }

  public class timeHelper {
    public timeHelper(int secs, int msecs) {
      time = secs > 0 ? new TimeSpan(0, 0, secs) : (msecs > 0 ? new TimeSpan(0, 0, 0, msecs) : new TimeSpan(0));
      timeStr = string.Format(@"{0:hh\:mm\:ss}", time);
      hour = (byte)time.Hours; minute = (byte)time.Minutes; second = (byte)time.Seconds; totalSecond = (int)time.TotalSeconds;
    }
    public static timeHelper fake;
    public TimeSpan time;
    public string timeStr;
    public ushort hour;
    public byte minute;
    public byte second;
    public int totalSecond;
  }
  public class dataHelper {
    public dataHelper(DateTime date) {
      this.date = date;
      dateStr = date.ToString(actCult.DateTimeFormat.ShortDatePattern);
      dateTimeStr = dateStr + " " + date.ToString(actCult.DateTimeFormat.ShortTimePattern);
      year = (short)date.Year; month = (byte)date.Month; monthStr = actCult.DateTimeFormat.MonthNames[month - 1];
      week = (byte)calendar.GetWeekOfYear(date, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
      quarter = (byte)((month - 1) / 3 + 1);
      quarterStr = quarter.ToString() + "qrt";
    }
    public dataHelper(int days, int secs, int msecs)
      : this(days > 0 ? LowUtils.intToDay(days) : (secs > 0 ? LowUtils.numToDate(secs) : (msecs > 0 ? LowUtils.intToDate(msecs) : DateTime.MinValue))) {
    }
    public DateTime date;
    public string dateStr;
    public string dateTimeStr;
    public short year;
    public byte quarter;
    public string quarterStr;
    public byte month;
    public string monthStr;
    public byte week;
    public static dataHelper fake;
    public static CultureInfo actCult = CultureInfo.InvariantCulture;
    static GregorianCalendar calendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);
  }

  public class departmentNode {
    public departmentNode parent;
    public List<departmentNode> items;
    public string title;
    public int id;
    public List<string> threeLevelsTitles() {
      var res = parents().Reverse().Select(n => n.title).Skip(1).Take(3).ToList();
      while (res.Count < 3) res.Add("@parent");
      return res;
    }
    IEnumerable<departmentNode> parents() {
      var n = this; do { yield return n; n = n.parent; } while (n != null);
    }
    public static List<string> _threeLevelsTitles = new List<string>() { "@parent", "@parent", "@parent" };
  }

  public struct scoreInterval {
    public string title;
    public int gte;
    public static scoreInterval find(int score) { return ints.First(i => i.gte <= score); }
    static scoreInterval[] ints = new scoreInterval[] { new scoreInterval { gte = 91, title = "excellent (>90)" }, new scoreInterval { gte = 83, title = "very good (83-90)" }, new scoreInterval { gte = 75, title = "good (75-82)" }, new scoreInterval { gte = 60, title = "insufficient (60-74)" }, new scoreInterval { gte = 0, title = "failed (<60)" } };
  }

  public static class lib2 {
    public static byte[] getResponse(Login.CmdReport par, out string fileName) {
      switch (par.type) {
        case Login.CmdReportType.test:
          fileName = string.Format("tests-report ({0})", respCnt++);
          return excelReport.tests.export(par.companyId);
        case Login.CmdReportType.evaluators:
          fileName = string.Format("evaluators-report ({0})", respCnt++);
          return excelReport.evaluatorsReport.export(par.companyId);
        default:
          throw new NotImplementedException();
      }
    }
    static int respCnt = 0;
  }

  public static class lib {

    public static string userName(string email, string fm, string lm) { return email + (string.IsNullOrEmpty(fm + lm) ? null : " (" + fm + " " + lm + ")"); }

    //Osetreni prvni Row a prazdneho zdroje dat
    public static IEnumerable<T> emptyAndHeader<T>(IEnumerable<T> src) where T : class {
      var isEmpty = !src.Any();
      var empty = isEmpty ? src.FirstOrDefault() : null; //null objekt spravneho typu
      var rowsData = isEmpty ? XExtension.Create(empty) : src; //neprazdny zdroj dat
      return XExtension.Create(empty).Concat(rowsData); //prvni radek jako header
    }

    //prevede napr. 337ab7 html color do 
    public static System.Drawing.Color formHtmlColor(string htmlHex) {
      return System.Drawing.Color.FromArgb(int.Parse(htmlHex, System.Globalization.NumberStyles.HexNumber));
    }


    public static ExcelWorksheet prepareSheet(ExcelPackage package, string name, int rowShift = 0) {
      ExcelWorksheet sheet = package.Workbook.Worksheets[name];
      var dim = sheet.Dimension;
      if (dim != null && dim.Start.Row + rowShift <= dim.End.Row) sheet.Cells[dim.Start.Row + rowShift, dim.Start.Column, dim.End.Row, dim.End.Column].Clear();
      if (sheet.Names.ContainsKey(dDataAll)) sheet.Names.Remove(dDataAll);
      return sheet;
    }
    public const string dDataAll = "all";

    public static ExcelWorksheet prepareSheet2(ExcelPackage package, string name, int rowShift = 0) {
      ExcelWorksheet sheet = package.Workbook.Worksheets[name];
      var dim = sheet.Dimension;
      if (dim != null && dim.Start.Row + rowShift <= dim.End.Row) sheet.Cells[dim.Start.Row + rowShift, dim.Start.Column, dim.End.Row, dim.End.Column].Clear();
      if (sheet.Names.ContainsKey(name)) sheet.Names.Remove(name);
      return sheet;
    }


    public static Dictionary<int, departmentNode> readDepartment(int companyId, out departmentNode root) {
      var db = Lib.CreateContext(); root = null;
      var allDeps = db.CompanyDepartments.Where(d => d.CompanyId == companyId).ToArray();
      Dictionary<int, departmentNode> res = new Dictionary<int, departmentNode>();
      foreach (var depDb in allDeps) {
        departmentNode depObj; departmentNode parentObj;
        if (!res.TryGetValue(depDb.Id, out depObj)) res.Add(depDb.Id, depObj = new departmentNode { id = depDb.Id });
        depObj.title = depDb.Title;
        if (depDb.ParentId != null) { //not root
          if (!res.TryGetValue((int)depDb.ParentId, out parentObj)) { //adjust root
            res.Add((int)depDb.ParentId, parentObj = new departmentNode { id = (int)depDb.ParentId });
          }
          //propoj depOj s parentObj
          if (parentObj.items == null) parentObj.items = new List<departmentNode>();
          parentObj.items.Add(depObj); depObj.parent = parentObj;
        }
        else //root
          root = depObj;
      }
      return res;
    }

    public enum cellFormat {
      no,
      date,
      dateTime,
      price,
      time,
      intAll,
      realAll,
      percent
    }

    public class formatedValue {
      public formatedValue(object value, cellFormat fmt) {
        this.value = value; this.fmt = fmt;
      }
      public object value; public cellFormat fmt;
    }
    public class actionFormula : ICellFormula {
      public actionFormula(Func<int, int, string> action) { this.action = action; }
      Func<int, int, string> action;
      string ICellFormula.getFormula(int rowIdx, int colIdx) {
        return action(rowIdx, colIdx);
      }
    }
    public class fmtFormula : ICellFormula {
      public fmtFormula(string mask) { this.mask = mask; }
      string mask;
      string ICellFormula.getFormula(int rowIdx, int colIdx) {
        return string.Format(mask, rowIdx, colIdx);
      }
    }
    public interface ICellFormula {
      string getFormula(int rowIdx, int colIdx);
    }

    public static ExcelRange import(ExcelWorksheet worksheet, IEnumerable<IEnumerable<object>> data, int rowStart = 0, int colStart = 0) {
      var actCol = colStart; var lastRow = rowStart; int lastCol = 0; List<string> colNames = new List<string>();
      foreach (var row in data.Where(d => d!=null)) {
        actCol = colStart;
        if (lastRow == rowStart) foreach (var cell in row) colNames.Add((string)cell);
        foreach (var v in row) {
          var cellValue = v; cellFormat fmt = cellFormat.no;
          if (cellValue == null) cellValue = "";
          var cell = worksheet.Cells[lastRow + 1, actCol++ + 1];
          if (cellValue is formatedValue) { fmt = ((formatedValue)cellValue).fmt; cellValue = ((formatedValue)cellValue).value; }
          else {
            if (intTypes.Contains(cellValue.GetType())) fmt = cellFormat.intAll;
            else if (realTypes.Contains(cellValue.GetType())) fmt = cellFormat.realAll;
          }
          if (cellValue is ICellFormula) cell.Formula = ((ICellFormula)cellValue).getFormula(lastRow + 1, actCol); else cell.Value = cellValue;
          switch (fmt) {
            case cellFormat.dateTime: cell.Style.Numberformat.Format = dataHelper.actCult.DateTimeFormat.ShortDatePattern + " " + dataHelper.actCult.DateTimeFormat.ShortTimePattern; break;
            case cellFormat.date: cell.Style.Numberformat.Format = dataHelper.actCult.DateTimeFormat.ShortDatePattern; break;
            case cellFormat.time: cell.Style.Numberformat.Format = "[h]:mm:ss"; break;
            case cellFormat.price: cell.Style.Numberformat.Format = "0.00"; break;
            case cellFormat.intAll: cell.Style.Numberformat.Format = "0"; break;
            case cellFormat.realAll: cell.Style.Numberformat.Format = "0.00"; break;
            case cellFormat.percent: cell.Style.Numberformat.Format = "0%"; break;
          }
        }
        if (lastCol == 0) lastCol = actCol; else if (lastCol != actCol) throw new Exception();
        lastRow++;
      }
      return worksheet.Cells[rowStart + 1, colStart + 1, lastRow, lastCol];
    }
    static Type[] intTypes = new Type[] { typeof(int), typeof(uint), typeof(long), typeof(Int64), typeof(UInt64), typeof(short), typeof(byte), typeof(ushort) };
    static Type[] realTypes = new Type[] { typeof(float), typeof(double) };


    static string resultKey = "result";

    public static void tests(int companyId) {
      //var db = Lib.CreateContext();
      //MemoryStream ms = new MemoryStream();
      ////using (ExcelPackage package = new ExcelPackage(resultMs)) {
      ////  ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("RowType");
      ////  var range = import(worksheet, 0, 0, new string[] { "email", "value" }, new object[][] { new object[] { 1, "n1" }, new object[] { 2, "n2" }, new object[] { 3, "n3" } });
      ////  worksheet.Names.Add("MyRange", range);
      ////  package.Save();
      ////  var data = resultMs.ToArray();
      ////  File.WriteAllBytes(@"d:\temp\pom2.xlsx", data);
      ////}
      //using (ExcelPackage package = new ExcelPackage(ms, new FileStream(@"d:\temp\pom2.xlsx", FileMode.Open))) {
      //  ExcelWorksheet worksheet = package.Workbook.Worksheets["RowType"];
      //  if (worksheet.Dimension != null) worksheet.Cells[worksheet.Dimension.Start.Row, worksheet.Dimension.Start.Column, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column].Clear();
      //  if (worksheet.Names.ContainsKey("MyRange")) worksheet.Names.Remove("MyRange");
      //  var range = import(worksheet, new object[][] { new object[] { "email", "value" }, new object[] { 1, "n1" }, new object[] { 2, "n2" }, new object[] { 3, "n3" } });
      //  worksheet.Names.Add("MyRange", range);
      //  package.Save();
      //  var data = ms.ToArray();
      //  File.WriteAllBytes(@"d:\temp\pom2.xlsx", data);
      //}
      //return;
      //var newFile = new FileInfo(@"d:\temp\pom2.xlsx"); if (newFile.Exists) newFile.Delete();
      //using (ExcelPackage package = new ExcelPackage(newFile)) {
      //  ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("RowType");
      //  var data = db.CourseDatas.Select(d => new { d.compId, d.Key }).ToArray().Select(d => new object[] { d.compId, d.Key });
      //  import(worksheet, data);
      //  package.Save();
      //}
      //return;
      //var flag = (long)CourseModel.CourseDataFlag.testImpl_result; var flagNo = (long)CourseModel.CourseDataFlag.needsEval;
      //var ress = db.CourseDatas.Where(cd => cd.CourseUser.CompanyUser.CompanyId == companyId && (cd.Flags & flag) == flag && (cd.Flags & flagNo) == 0).Select(cd => cd.RowType).ToArray();
      //foreach (var r in ress) {
      //  var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<testMe.result>(r);
      //}
      //flag = (long)CourseModel.CourseDataFlag.pcCannotEvaluate; flagNo = (long)CourseModel.CourseDataFlag.needsEval;
      //ress = db.CourseDatas.Where(cd => cd.CourseUser.CompanyUser.CompanyId == companyId && (cd.Flags & flag) == flag && (cd.Flags & flagNo) == 0 && !string.IsNullOrEmpty(cd.RowType)).Select(cd => cd.RowType).ToArray().Where(s => !string.IsNullOrEmpty(s)).Take(1).ToArray();
      //foreach (var r in ress) {
      //  var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<object>(r, new courseResultTypeConverter());
      //}
    }

  }
}