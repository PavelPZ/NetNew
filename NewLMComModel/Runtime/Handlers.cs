using LMComLib;
using LMNetLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Handlers {

  public static class sessionManager {
    public static bool checkSession(Int64 lmcomId, Int64 sessionId) {
      if (lmcomId == 0) return true;
      lock (typeof(sessionManager)) {
        item user; // = sessionCache.Get(lmcomId.ToString()) as item;
        var now = DateTime.Now;
        if (!sessions.TryGetValue(lmcomId, out user)) {
          user = new item { actSes = sessionId, lastTouch = DateTime.Now };
          sessions.Add(lmcomId, user);
          return true;
        }
        user.lastTouch = now;
        //odstran zaznamy starsi nez 2 dny
        if (now - lastClearDate > clearInterval) {
          lastClearDate = DateTime.Now;
          foreach (var kv in sessions) {
            if (now - kv.Value.lastTouch > lastTouchInterval) sessions.Remove(kv.Key);
          }
        }
        //kontrola session
        if (user.actSes == sessionId) return true; //stejna session
        if (user.oldSess != null && user.oldSess.Contains(sessionId)) return false; //expired session
                                                                                    //dej puvodni act session do oldSess a aktualizuj act session
        if (user.oldSess == null) user.oldSess = new HashSet<long>();
        user.oldSess.Add(user.actSes); //jina session - dej act session do oldSess
        user.actSes = sessionId; //nastav actSes
        return true;
      }
    }
    //static MemoryCache sessionCache = new MemoryCache("sessionManager", null);
    static Dictionary<Int64, item> sessions = new Dictionary<Int64, item>();
    static DateTime lastClearDate = DateTime.Now;
    static TimeSpan clearInterval = new TimeSpan(1, 0, 0); //1 hod
    static TimeSpan lastTouchInterval = new TimeSpan(2, 0, 0, 0); //1 den
    public class item {
      public HashSet<Int64> oldSess; //stare sessions uzivatele
      public Int64 actSes; //aktualni session uzivatele
      public DateTime lastTouch; //posledni aktualizace teto item
    }
  }

  public class CmdService : RewRpcLib.reqParLow {

    public static void registerCommand<TRq, TResp>(Func<TRq, RpcResponse> action) where TRq : class {
      RewRpcLib.registerCommand(new CmdService() { input = typeof(TRq), output = typeof(TResp), action = (Object obj) => action((TRq)obj) });
    }

    public override bool isMyType(string type) {
      return string.Compare(input.FullName, type, true) == 0;
    }
    public Type input;
    public Type output;
    public Func<Object, RpcResponse> action;
    public override string ProcessRequest(HttpContext context) {
      bool isXml = context.Request["format"] == "xml";
      string txt;
      //Logger ("type={0}", type);
      try {

        //deserialization
        object req;
        try {
          var data = RewRpcLib.getPostData(context);
          string timestamp = context.Request["timestamp"] ?? "none";
          Logger.Log("<#{0} {1} INPUT: type={2}, IP = {3}, data={4}", timestamp, DateTime.UtcNow.ToString(), input.FullName, LowUtils.GetIpAddress(HttpContext.Current.Request), data.Length > 2000 ? data.Substring(0, 2000) : data);
          if (isXml)
            req = XmlUtils.StringToObject(data, input);
          else
            req = JsonConvert.DeserializeObject(data, input);
        }
        catch (Exception exp) {
          Logger.Log("Deserialize error");
          throw new Exception("Deserialize error", exp);
        }

        //Odriznuti dvojiho zalogovani kvuli Vyzva 57
        LMComLib.Cmd sessCmd = req as LMComLib.Cmd;
        if (false && sessCmd != null && !sessionManager.checkSession(sessCmd.lmcomId, sessCmd.sessionId)) {
          Logger.Log("Warning: User logged under other account");
          //check session
          var err = new RpcResponse(998, null);
          txt = isXml ? XmlUtils.ObjectToString(err) : JsonConvert.SerializeObject(err);
        }
        else {
          //response
          RpcResponse resp = action(req);
          txt = isXml ? XmlUtils.ObjectToString(resp.result) : JsonConvert.SerializeObject(resp);
          Logger.Log("OUTPUT: type={0}, data={1}\r\n#>", input.FullName, txt.Length > 2000 ? txt.Substring(0, 2000) : txt);
        }
        //}
      }
      catch (System.Threading.ThreadAbortException th) {
        throw th;
      }
      catch (Exception exp) {
        if (isXml) throw;
        Logger.Error(exp);
        var err = new RpcResponse(999, string.Format("ERROR: Type={0}\r\nExp={1}\r\n#>", input.FullName, LowUtils.ExceptionToString(exp, true)));
        txt = isXml ? XmlUtils.ObjectToString(err) : JsonConvert.SerializeObject(err);
      }
      var jsonpCallback = context.Request["callback"];
      if (!string.IsNullOrEmpty(jsonpCallback)) {
        txt = jsonpCallback + "(" + txt + ")";
        context.Response.ContentType = "application/x-javascript";
      }
      else
        context.Response.ContentType = "text/plain";
      return txt;
    }

    //internal static void registerCommand<T1, T2>(Func<CmdHumanEvalStudent, RpcResponse> func) {
    //  throw new NotImplementedException();
    //}
  }

  public static class Licence {

    static Licence() {
      RewRpcLib.registerCommand(new Engine());
    }

    //static string licDir = Machines.basicPath + @"rew\Downloads\Licences\";

    public static string LicenceId(string appUrl) {
      string domain; char licDir;
      return LicenceId(appUrl, out domain, out licDir);
    }
    public static string LicenceId(string appUrl, out string domain, out char licDir) {
      licDir = ' '; domain = null;
      if (appUrl == null) return null;
      var apiUrl = appUrl.Split(new char[] { '/' }, 2);
      domain = apiUrl[0];
      string file = apiUrl[1].Replace("mod/scorm/player.php", "[moodle]").Replace("index.html", null).Replace('/', '-').Replace('.', '-');
      var parts = domain.Split('.');
      licDir = parts.Length > 2 ? parts[parts.Length - 2][0] : domain[0];
      return domain + "-" + file;
    }

    public class JSInfo {
      //public static void adjust(string fn, Int64 Version, string email) {
      //  var res2 = File.Exists(fn) ? XmlUtils.FileToObject<JSInfo>(fn) : new JSInfo() { UsedBy = new List<string>() };
      //  res2.Version = Version;
      //  if (res2.UsedBy.IndexOf(email) < 0) res2.UsedBy.Add(email);
      //  XmlUtils.ObjectToFile(fn, res2);
      //}
      public Int64 Version;
      public string Domain;
      public DateTime ValidTo;
      //public List<string> UsedBy;
    }

    public class Engine : RewRpcLib.reqParLow {

      static bool isSourceWeb = ConfigurationManager.AppSettings["isSourceWeb"] == "true";

      public override bool isMyType(string type) {
        if (types == null) {
          types = LowUtils.EnumGetValues<LMComLib.Targets>().ToDictionary(v => "_rew_" + v.ToString().ToLower(), v => true);
          types.Add("_course", true);
          types.Add("_external", true);
        }
        if (type == null) return true;  //CD Cargo Hack
        return types.ContainsKey(type.ToLower());
      }
      Dictionary<string, bool> types;

      public override string ProcessRequest(HttpContext context) {
        var isDebug = Logger.isDebug(context);

        var type = context.Request["type"] ?? context.Request["file"]; //CD Cargo Hack

        bool isSourceWebMachine = Machines.isPZComp() || Machines.isFE5() || isSourceWeb;
        Logger.Log(@"Lib.Licence.ProcessRequest rewFile={0}", type);
        if (type == "_rew_scorm" || type == "_rew_web" || type == "_external" || (type == "_course" && (isSourceWebMachine || isDebug || context.Request["data"] == null))) {
          var rewVersion = isSourceWebMachine ? 0 : uint.Parse(context.Request["version"]);
          var rewArchive = isSourceWebMachine ? Machines.appRoot + @"schools\" + type + ".js" : Machines.appData + @"JSCrambler\" + type + "\\" + rewVersion + ".js"; //d:\LMCom\ReleaseDeploy\JSCrambler\_rew_web\418460831.js
          var rewMin = isSourceWebMachine || isDebug ? rewArchive : rewArchive.Replace(".js", ".min.js"); //d:\LMCom\ReleaseDeploy\JSCrambler\_rew_web\418460831.charMin.js
          bool minExists = File.Exists(rewMin);

          if (!File.Exists(rewArchive)) {
            Logger.Error(string.Format("Lib.Licence.ProcessRequest: !File.Exists({0})", rewArchive));
            //throw new Exception(string.Format("lib.Licence.ProcessRequest: !File.Exists({0})", rewArchive));
            context.Response.End();
          }

          if (!minExists) { // || (!debugMachines && File.GetLastWriteTime(rewArchive) > File.GetLastWriteTime(rewMin))) {
            if (type != "_course") {
              Logger.Error(string.Format("Lib.Licence.ProcessRequest REW: invalid service JSCramber call: isDebug=" + isDebug.ToString() + ", file=" + rewMin + ", type=" + type));
              context.Response.End();
            }
            Logger.Log(@"Lib.Licence.ProcessRequest REW: call JSCrambler start: isDebug=" + isDebug.ToString() + ", file=" + rewMin + ", type=" + type, true);
            lock (typeof(Licence)) {
              JSCrambler.Lib.Protect(new JSCrambler.FileItemLow[] { new JSCrambler.FileItem(rewArchive, rewMin) });
              GZipHandler.GZip(rewMin);
            }
            Logger.Log(@"Lib.Licence.ProcessRequest REW: call JSCrambler end", true);
          }
          GZipHandler.MakeResponse(context, rewMin);
        }

        if (type != "_course") context.Response.End();

        //pozadavek na licenci
        var licReq = LowUtils.JsObjectDecode<schools.licenceRequest>(context.Request["data"]);
        string domain; char licDir;
        var licId = LicenceId(licReq.appUrl, out domain, out licDir); // domain + "-" + file;

        //Web4\App_Data\Licences\l\localhost-rew-schools-newea-aspx.xml
        var path = Machines.appData + @"Licences\" + licDir + "\\" + licId + ".xml";

        //ReleaseDeploy\JSCrambler\_course\633471504.js
        var crsArchive = isSourceWebMachine ? Machines.appRoot + @"Schools\_course.js" : Machines.appData + @"JSCrambler\_course\" + licReq.courseVersion + ".js";
        if (!File.Exists(crsArchive))
          throw new Exception(string.Format("Lib.Licence.ProcessRequest: !File.Exists({0})", crsArchive));
        if (isSourceWebMachine && !File.Exists(crsArchive.Replace(".js", ".min.js")))
          throw new Exception(string.Format("Lib.Licence.ProcessRequest: !File.Exists({0}.min.js)", crsArchive));

        //vysledek licence
        schools.licenceResponse err = new schools.licenceResponse() { Id = licId };
        var lic = checkLicence(licReq, err, path, licId);

        //send JS file
        if (err.result != schools.licenceResult.ok) {//Error JS
          err.Months = lic.Months.Select(m => new schools.licenceRespMonth() {
            Date = LowUtils.DateToInt(new DateTime(m.Year, m.Month, 1).AddMonths(1).AddDays(-1)),
            Users = m.Users.Select(u => new schools.licenceRespUser() {
              Created = LowUtils.DateToInt(u.Created),
              Id = u.Id,
              Name = u.Name,
            }).ToArray()
          }).ToArray();
          err.Buys = lic.Buys.Select(b => new schools.licenceRespBuy() {
            Created = LowUtils.DateToInt(b.Created),
            UserMonths = b.UserMonths,
          }).ToArray();
          Logger.Log(@"Lib.Licence.ProcessRequest Licence Error: ", JsonConvert.SerializeObject(err));
          context.Response.Write("splash.error = " + JsonConvert.SerializeObject(err) + "; ViewBase.initBootStrapApp = boot.Error;");
          context.Response.End();
        }
        Int64 version = isSourceWebMachine ? 0 : Int64.Parse(Path.GetFileNameWithoutExtension(crsArchive));
        var jsPath = isSourceWebMachine ? crsArchive.Replace(".js", ".min.js") : path.Replace(".xml", ".js");
        var jsTextPath = isSourceWebMachine ? null : path.Replace(".xml", ".txt");
        var jsExists = isSourceWebMachine ? true : File.Exists(jsPath);
        var jsTextExists = isSourceWebMachine ? true : File.Exists(jsTextPath);
        bool jsOk = isSourceWebMachine ? true : false;
        if (!isSourceWebMachine && jsExists && jsTextExists) {
          var validTo = XmlUtils.FileToObject<JSInfo>(jsTextPath);
          var jsCreated = File.GetLastWriteTime(jsPath);
          jsOk = (licReq.courseVersion == validTo.Version) && validTo.ValidTo > DateTime.UtcNow;
        }
        if (!jsOk) { //Call JSCrambler:
          Logger.Log(@"Lib.Licence.ProcessRequest CRS: call JSCrambler start", true);
          lock (typeof(Licence)) {
            var validTo = DateTime.UtcNow.AddDays(jsExists ? lic.ProlongDays + 2 : 36);
            JSCrambler.Lib.Protect(new JSCrambler.FileItemLow[] { new JSCrambler.FileItem(crsArchive, jsPath) }, domain, validTo);
            XmlUtils.ObjectToFile(jsTextPath, new JSInfo() {
              ValidTo = validTo.AddDays(-2),
              Domain = domain,
              Version = version
            });
            GZipHandler.GZip(jsPath);
          }
          Logger.Log(@"Lib.Licence.ProcessRequest CRS: call JSCrambler end", true);
        }
        //else
        //  Logger.Log(@"lib.Licence.ProcessRequest CRS: Min file OK");
        GZipHandler.MakeResponse(context, jsPath);
        return null;
      }
    }

    static schools.licence checkLicence(schools.licenceRequest licReq, schools.licenceResponse err, string path, string licId) {
      var now = DateTime.UtcNow; bool changed = false;


      //nacteni ci vytvoreni licence
      schools.licence lic = (schools.licence)HttpContext.Current.Cache.Get(licId);
      if (lic == null) {
        if (File.Exists(path)) {
          lic = XmlUtils.FileToObject<schools.licence>(path);
        }
        else {
          lic = new schools.licence() {
            Url = licReq.appUrl,
            Created = now,
            ProlongDays = 31,
            Months = new List<schools.licenceMonth>(),
            Buys = new List<schools.licenceBuy>() { new schools.licenceBuy () {
              Created = now,
              UserMonths = 100,
              Info = "demo"
            }}
          };
          var dir = Path.GetDirectoryName(path);
          if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
          changed = true;
        }
        HttpContext.Current.Cache.Insert(licId, lic, new System.Web.Caching.CacheDependency(path));
      }

      //testGlobalAdmin expirace dema
      if (lic.Buys.Count == 1 && lic.Created.AddDays(32) < now) { err.result = schools.licenceResult.demoExpired; err.DemoExpired = LowUtils.DateToInt(lic.Created.AddDays(31)); return lic; }

      //Pridej mesic a uzivatele
      var y = (short)now.Year; var m = (byte)now.Month;
      var month = lic.Months.FirstOrDefault(mm => mm.Month == m && mm.Year == y);
      if (month == null) {
        changed = true;
        month = new schools.licenceMonth() {
          Month = m,
          Year = y,
          Users = new List<schools.licenceUser>()
        };
        lic.Months.Add(month);
      }
      var gi = licReq.GlobalId();
      var user = month.Users.FirstOrDefault(u => u.Id == gi && u.rootCourse == licReq.rootCourse);
      if (user == null) {
        changed = true;
        user = new schools.licenceUser() {
          Id = gi,
          rootCourse = licReq.rootCourse,
          Created = now,
          Name = licReq.LastName + (string.IsNullOrEmpty(licReq.FirstName) ? null : " " + licReq.FirstName),
        };
        month.Users.Add(user);
      }

      //kontrola poctu licenci
      if (allUsers(lic) > allUserMonths(lic)) { err.result = schools.licenceResult.userMonthExpired; return lic; }

      //save
      if (changed) XmlUtils.ObjectToFile(path, lic);
      return lic;
    }

    static int allUsers(schools.licence lic) {
      return lic.Months.Select(m => m.Users == null ? 0 : m.Users.Count).Sum();
    }
    static int allUserMonths(schools.licence lic) {
      return lic.Buys.Select(b => b.UserMonths).Sum();
    }
  }

  public class GZipHandler : IHttpHandler {

    public static void GZip(string fn) {
      using (var inFs = new FileStream(fn, FileMode.Open, FileAccess.Read))
      using (var ouFs = new FileStream(fn + ".gzip", FileMode.Create, FileAccess.Write))
      using (GZipStream gzip = new GZipStream(ouFs, CompressionMode.Compress, true)) inFs.CopyTo(gzip);
    }

    public void ProcessRequest(HttpContext context) {
      MakeResponse(context, context.Request.PhysicalPath);
    }
    public bool IsReusable { get { return true; } }

    public static void MakeResponse(HttpContext context, string fn, Action<string> makeGZip = null) {
      fn = dataPath(fn);
      var request = context.Request; var resp = context.Response;
      resp.Cache.SetCacheability(HttpCacheability.Private);
      resp.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
      string acceptEncoding = request.Headers["Accept-Encoding"];
      //Content type
      resp.ContentType = contentTypes[Path.GetExtension(fn)];
      //Modified?
      DateTime modified = File.GetLastWriteTime(fn);
      string header = request.Headers["If-Modified-Since"];
      if (header != null) {
        DateTime isModifiedSince;
        if (DateTime.TryParse(header, out isModifiedSince) && Math.Abs((modified - isModifiedSince).TotalSeconds) <= 2) {
          resp.StatusCode = 304;
          resp.SuppressContent = true;
          resp.StatusDescription = "Not Modified";
          resp.AddHeader("Content-Length", "0");
          resp.Cache.SetLastModified(modified);
          resp.End();
        }
      }
      //Machines.rootDir
      //...Yes
      resp.Cache.SetLastModified(modified);
      resp.ContentEncoding = Encoding.UTF8;

      if (string.IsNullOrEmpty(acceptEncoding) || !acceptEncoding.Contains("gzip")) {
        resp.WriteFile(fn);
        resp.End();
      }
      if (!File.Exists(fn + ".gzip")) {
        if (makeGZip == null) {
          if (!File.Exists(fn) && (localizedFileMask.IsMatch(fn) || instructionFileMask.IsMatch(fn))) resp.Write("{}"); //neni lokalizovany soubor => vrat {}
          else resp.WriteFile(fn);
          resp.End();
        }
        makeGZip(fn);
      }
      resp.AppendHeader("Content-Encoding", "gzip");
      resp.WriteFile(fn + ".gzip");
      resp.End();
    }
    static Regex localizedFileMask = new Regex(@"\w+\.\w{2}_\w{2}\.js$"); //maska pro lokalizovane JS soubory, napr. xxx.cs-cz.js
    static Regex instructionFileMask = new Regex(@"_instrs.js$"); //maska pro instrukce (kvuli fake produktum), napr. _instrs.js

    static string dataPath(string fn) {
      var rp = Machines.rootPath;
      if (!fn.StartsWith(rp)) return fn;
      var rel = fn.Substring(rp.Length);
      var dir = rel.Split('\\')[0];
      switch (dir) {
        case "lm":
        case "data":
        case "media":
        case "publ":
        case "grafia":
        case "skrivanek":
        case "edusoft":
        case "rwbooks":
        case "rwdicts":
        case "rwtts":
          return fn;
        default: return fn;
      }
    }

    static Dictionary<string, string> contentTypes = new Dictionary<string, string> {
      {".json", "text/json"},
      {".rjson", "text/rjson"},
      {".js", "application/x-javascript"},
      {".html", "text/html"},
      {".css", "text/css"},
      {".svg", "image/svg+xml"},
      {".ttf", "application/x-font-ttf"},
      {".otf", "application/x-font-opentype"},
      {".woff", "application/font-woff"},
      {".eot", "application/vnd.ms-fontobject"},

      {".mp4", "video/mp4"},
      {".webm", "video/webm"},
      {".gif", "image/gif"},
      {".png", "image/png"},
      {".bmp", "image/bmp"},
    };
  }
}


