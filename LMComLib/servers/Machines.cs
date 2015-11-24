using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Web.UI.WebControls;
using System.Runtime.InteropServices;
using System.Web.Hosting;
using System.DirectoryServices;

namespace LMComLib {

  public static class CommonLocales {
    public static string JazykAplikace { get { return CSLocalize.localize("02b242ceb290418d8db4163e4e9b9ef5", LocPageGroup.LMComLib, "Jazyk aplikace"); } }
    public static string LoginTitle { get { return CSLocalize.localize("a03d125eac70451299ec0253a77eb8b5", LocPageGroup.LMComLib, "Přihlášení"); } }
    public static string LoginText { get { return CSLocalize.localize("81e373e71cb743468b933fca70c57f8d", LocPageGroup.LMComLib, "Přihlaste se jedním z následujících způsobů:"); } }

  }

  public enum WebAppIds {
    unknown,
    lmcom_all, //lm.com, lm.cz a vyuka na localhost, newbuild, lm-backup
    lmcom, //lm.com na FE
    lmcz, //lm.cz na FE
    vyuka, //vyuka na FE
    lmorg, //lm.org na FE
  }

  public static class Machines {
    //public const string pz_comp = "pz-acer-2010";
    public const string pz_comp = "pz-w8virtual";
    public const string pj_comp = "pj-w8-virtual";
    public const string rj_comp = "rj-w8-virtual";
    public const string kz_comp = "brigadnik";
    public const string zz_comp = "zdenka-w7";
    public const string jz_comp = "Kuba-PC";
    public static bool debugNoInternet;
    static bool data = false;
    public static bool doCache = true;
    public static bool isLMCom = false;
    public static void setIsLMCom(bool isLmCom) { isLMCom = isLmCom; }
    static bool isLMComDebugMode = false;
    public static string EA_as_LMCom_Auth;

    public static string appData = HostingEnvironment.MapPath("~/app_data") + "\\";
    public static string appRoot = HostingEnvironment.MapPath("~/") ?? "";

    //k ~/xxx vrati http://...
    public static string fullHttpUrl(string absolutePath, HttpContext ctx = null) {
      if (ctx == null) ctx = HttpContext.Current; if (ctx == null) throw new Exception("HttpContext.Current==null");
      return ctx.Request.Url.GetLeftPart(UriPartial.Authority) + VirtualPathUtility.ToAbsolute(absolutePath);
    }


    public static string basicPath {
      get {
        if (_basicPath != null) return _basicPath;
        if (isFE5()) _basicPath = ConfigurationManager.AppSettings["BasicPath.FE5"] ?? ConfigurationManager.AppSettings["BasicPath"];
        else if (isPZComp()) _basicPath = ConfigurationManager.AppSettings["BasicPath.pz"] ?? ConfigurationManager.AppSettings["BasicPath"];
        else _basicPath = ConfigurationManager.AppSettings["BasicPath"];
        if (_basicPath == null) {
          try {
            var ar = appRoot.ToLower();
            if (ar.IndexOf(@"rew\web4\") >= 0) _basicPath = appRoot.ToLower().Replace(@"rew\web4\", null);
          }
          catch { }
        }
        if (_basicPath == null) _basicPath = @"d:\lmcom\";
        //var def = (ConfigurationManager.AppSettings["BasicPath"] ?? @"q:\lmcom\");
        //if (isFE5()) return ConfigurationManager.AppSettings["BasicPath.FE5"] ?? def;
        //if (isPZComp()) return ConfigurationManager.AppSettings["BasicPath.pz"] ?? def;
        //else 
        //throw new Exception(def);
        return _basicPath;
      }
    }
    public static string _basicPath;
    public static string tempDir = ConfigurationManager.AppSettings["TempPath"] ?? @"c:\temp\";

    public static string rwDataSourcePath {
      get {
        return _rwDataSourcePath ?? (ConfigurationManager.AppSettings["DataSourcePath" + (isPZComp() ? ".pz" : null)] ?? basicPath);
      }
    }
    public static string _rwDataSourcePath;

    //public static string rootDir { get { return Machines.basicPath + @"rew\web4\data"; } }
    public static string rootDir {
      get {
        if (_rootDir != null) return _rootDir;
        if (HttpContext.Current != null) {
          _rootDir = HttpContext.Current.Server.MapPath("~/");
          _rootDir = _rootDir.Substring(0, _rootDir.Length - 1);
        }
        else
          _rootDir = Machines.basicPath + @"rew\web4";
        return _rootDir;
        //return _rootDir ?? (_rootDir = (HttpContext.Current == null ? Machines.basicPath + @"rew\web4" : HttpContext.Current.Server.MapPath("~/")));
      }
    }
    public static string _rootDir;
    public static string rootPath { get { return rootDir + "\\"; } }
    //public static string publDir { get { return rootPath + "data"; } }
    //public static string publDir { get { return rootDir; } }
    //public static string publPath { get { return publDir + "\\"; } }
    public static string dataDir { get { return _dataDir ?? rootPath + "data"; } }
    public static string _dataDir;
    public static string dataPath { get { return dataDir + "\\"; } }
    public static string oldEaLinePath { get { return rootPath + @"lm\oldea\"; } }

    public static string statisticDir {
      get {
        if (_statisticDir != null) return _statisticDir;
        if (isPZComp()) return ConfigurationManager.AppSettings["LMComData.pz"] ?? (ConfigurationManager.AppSettings["LMComData"] ?? @"q:\lmcom\");
        else return ConfigurationManager.AppSettings["LMComData"] ?? @"q:\disk_q\lmcom\LMCom\App_Data\";
      }
    }
    public static string _statisticDir;


    /*public static bool isEaLMComBuild {
      get {
        return isEaLMComBuildEx(HttpContext.Current);
      }
    }
    public static bool isEaLMComBuildEx(HttpContext ctx) {
      //http://browsers.garykeith.com/ BorwserCaps
      return ctx != null && ctx.Request.Browser.Crawler;
    }*/
    public static string machine;
    public static bool noFB() {
      return false; // !(/*WebAppId==WebAppIds.lmcom &&*/ (machine == pz_comp || machine == "lm-frontend-2"));
    }
    static Machines() {
      machine = ConfigurationManager.AppSettings["Machines.machine"];
      if (string.IsNullOrEmpty(machine)) machine = System.Environment.MachineName.ToLower();

      debugNoInternet = ConfigurationManager.AppSettings["debug.noInternet"] == "true" && isPZComp();

      string[] parts = null;
      string par = ConfigurationManager.AppSettings["Machines.Data"];
      if (!string.IsNullOrEmpty(par)) {
        parts = par.ToLower().Split(',');
        data = Array.IndexOf<string>(parts, machine) >= 0;
      }
      par = ConfigurationManager.AppSettings["Machines.NoCache"];
      if (!string.IsNullOrEmpty(par)) {
        parts = par.ToLower().Split(',');
        doCache = Array.IndexOf<string>(parts, machine) < 0;
      }
      par = ConfigurationManager.AppSettings["Machines.LMCom"];
      if (!string.IsNullOrEmpty(par)) {
        parts = par.ToLower().Split(',');
        isLMCom = Array.IndexOf<string>(parts, machine) >= 0;
        //if (isLMCom && HttpRuntime.AppDomainAppVirtualPath.ToLower().IndexOf("eduauthornew") >= 0) isLMCom = false;
      }
      par = ConfigurationManager.AppSettings["Machines.DebugTrue"];
      if (!string.IsNullOrEmpty(par)) {
        parts = par.ToLower().Split(',');
        isLMComDebugMode = Array.IndexOf<string>(parts, machine) >= 0;
      }
      //jmeno asp.net aplikace
      string ai = isPZComp() || machine == "newbuild" ? ConfigurationManager.AppSettings["debug.WebAppId"] : System.Web.Hosting.HostingEnvironment.SiteName;
      WebAppId = WebAppIds.unknown;
      if (ai != null)
        try { WebAppId = (WebAppIds)Enum.Parse(typeof(WebAppIds), ai, false); } catch { }
      if (isPZComp()) {
        EA_as_LMCom_Auth = ConfigurationManager.AppSettings["Machines.eaAsLMCom"];
        //if (!string.IsNullOrEmpty(EA_as_LMCom_Auth)) isLMCom = true;
        //if (!string.IsNullOrEmpty(par)) {
        //EA_as_LMCom_Auth = par == "true";
        //isLMCom = true;
        //}
      }
      /*
      new DebugEvent(System.Web.Hosting.HostingEnvironment.ApplicationID, 1).Raise();
      new DebugEvent(System.Web.Hosting.HostingEnvironment.SiteName, 2).Raise();
      DirectoryEntry entry = new System.DirectoryServices.DirectoryEntry("IIS://localhost/W3SVC");
      DirectoryEntry[] webs = entry.Children.Cast<DirectoryEntry>().ToArray().Where(en => propValue(en,"KeyType") == "IIsWebServer").ToArray();

      new DebugEvent(
        webs.SelectMany(w => apps(w)).Select(a => a.Properties["AppRoot"].Value.ToString()).Aggregate((r,i) => r + "###" + i )
      , 1).Raise();


      DirectoryEntry app = webs.SelectMany(w => apps(w)).
        Where(a => a.Properties["AppRoot"].Value.ToString() == System.Web.Hosting.HostingEnvironment.ApplicationID).First();
      try {
        WebAppId = (WebAppIds)Enum.Parse(typeof(WebAppIds), propValue(app, "AppFriendlyName"), false);
      } catch { 
        WebAppId = WebAppIds.unknown; 
      }*/
    }
    static IEnumerable<DirectoryEntry> apps(DirectoryEntry root) {
      if (Convert.ToInt32(root.Properties["AppIsolated"].Value) == 2) yield return root;
      foreach (DirectoryEntry de in root.Children)
        foreach (DirectoryEntry subDe in apps(de)) yield return subDe;
    }
    static string propValue(DirectoryEntry root, string name) {
      PropertyValueCollection obj = (PropertyValueCollection)root.Properties[name];
      return obj == null ? null : obj.Value.ToString();
    }

    /*public static Npgsql.NpgsqlConnection createProsperConnection() {
      string connName = data ? "ProsperDataConn" : "ProsperDataConnTest";
      return new Npgsql.NpgsqlConnection(ConfigurationManager.ConnectionStrings[connName].ConnectionString);
    }*/

    public static WebAppIds WebAppId;

    public static string LMDataConnectionString() {
      string connName = data ? "LMComDataConn" : "LMComDataConnTest";
      var cn = ConfigurationManager.ConnectionStrings[connName];
      return cn == null ? null : ConfigurationManager.ConnectionStrings[connName].ConnectionString;
    }

    /*
    public static string ProsperDataConnectionString() {
      string connName = data ? "ProsperData" : "ProsperDataTest";
      return ConfigurationManager.ConnectionStrings[connName].ConnectionString;
    }

    public static ProsperData.Context ProsperData() {
      ProsperData.Context res = new ProsperData.Context(ProsperDataConnectionString());
      if (isPZComp() && sb != null) res.Log = wr;
      return res;
    }
     */

    public static void FinishSqlDataSource(SqlDataSource ds) {
      string connName = data ? "LMComDataConn" : "LMComDataConnTest";
      ConnectionStringSettings sett = ConfigurationManager.ConnectionStrings[connName];
      ds.ConnectionString = sett.ConnectionString;
      ds.ProviderName = sett.ProviderName;
    }

    public static bool debugMachine() {
      return isPZComp() || machine == "newbuild" || machine == "lm-backup";
    }

    public static string PayPalInfo(string name) {
      return ConfigurationManager.AppSettings[(debugMachine() || Machines.machine == "lm-frontend-2" ? "PayPalNew.Debug" : "PayPalNew.") + name];
    }

    public static bool isDebugDatabase {
      get { return !data; }
    }

    public static StringBuilder sb = new StringBuilder();
    public static StringWriter wr = new StringWriter(sb); //wr = null;

    //public static LMComData2.LMComDataContext getContext() {
    //  return getContext(true);
    //}

    //public static LMComData2.LMComDataContext getContext(bool objectTrackingEnabled) {
    //  var cn = LMDataConnectionString(); if (cn == null) return null;
    //  LMComData2.LMComDataContext ctx = new LMComData2.LMComDataContext(cn);
    //  ctx.ObjectTrackingEnabled = objectTrackingEnabled;
    //  if (isPZComp() && sb != null) ctx.Log = wr;
    //  return ctx;
    //}

    //public static Moodle.MoodleData getMoodleContext(bool objectTrackingEnabled) {
    //  Moodle.MoodleData ctx = new Moodle.MoodleData(ConfigurationManager.ConnectionStrings["MoodleData"].ConnectionString);
    //  ctx.ObjectTrackingEnabled = objectTrackingEnabled;
    //  if (isPZComp() && sb != null) ctx.Log = wr;
    //  return ctx;
    //}

    [DllImport("mpr.dll")]
    public static extern int WNetGetConnection(string localName, StringBuilder remoteName, int remoteNameLength);

    //public static Trados.TradosDataContext getTradosContext() {
    //  Trados.TradosDataContext ctx = new Trados.TradosDataContext(TradosConnectionString());
    //  ctx.CommandTimeout = 3000;
    //  if (isPZComp()) ctx.Log = wr;
    //  return ctx;
    //}

    //public static Trados.TradosDataContext getTradosContext(bool ObjectTrackingEnabled) {
    //  Trados.TradosDataContext ctx = new Trados.TradosDataContext(TradosConnectionString());
    //  ctx.CommandTimeout = 1000;
    //  ctx.ObjectTrackingEnabled = ObjectTrackingEnabled;
    //  if (isPZComp()) ctx.Log = wr;
    //  return ctx;
    //}

    //public static string TradosConnectionString() {
    //  return _tradosConnectionString ?? ConfigurationManager.ConnectionStrings["TradosData"].ConnectionString;
    //}
    //public static string _tradosConnectionString;

    public static string RewiseConnectionString() {
      string connName = data ? "Rewise" : "RewiseTest";
      return ConfigurationManager.ConnectionStrings[connName].ConnectionString;
    }

    //public static RewiseData.RewiseDataContext getRewiseContext(bool ObjectTrackingEnabled) {
    //  RewiseData.RewiseDataContext ctx = new RewiseData.RewiseDataContext(RewiseConnectionString());
    //  ctx.ObjectTrackingEnabled = ObjectTrackingEnabled;
    //  if (isPZComp()) ctx.Log = wr;
    //  return ctx;
    //}

    //public static RewiseData.RewiseDataContext getRewiseContext() {
    //  return getRewiseContext(true);
    //}



    public static bool eaIsLMComDebugMode() {
      return isLMComDebugMode;
    }

    public static bool NextVersion {
      get { return !data; }
    }

    //public static bool doEaCache(HttpContext ctx) {
    //  if (!Machines.doCache && !isBuildEACacheEx(ctx)) return false;
    //  urlInfo ui = urlInfo.GetUrlInfo();
    //  if (ui == null) return false;
    //  return ui.EACourse != CourseIds.no;
    //}

    //public static bool isBuildEACacheEx(HttpContext ctx) {
    //  //return true;
    //  return ctx != null && ctx.Request.Headers["User-Agent"] == "LANGMasterCacheBuild";
    //}

    //public static bool isBuildEACache {
    //  get { return isBuildEACacheEx(HttpContext.Current); }
    //}

    //public static bool isBuildEACache_BuildCD_Crawler {
    //  get {
    //    return isBuildEACache_BuildCD_CrawlerLow(HttpContext.Current);
    //  }
    //}

    //public static bool isBuildEACache_BuildCD_CrawlerLow(HttpContext ctx) {
    //  if (ctx == null) return false;
    //  return isBuildEACacheEx(ctx) || isCrawlerEx(ctx) || isEABuildCDEx(ctx);
    //}

    //public static bool isEABuildCDEx(HttpContext ctx) {
    //  return !string.IsNullOrEmpty(ctx.Request.Headers["Deploy-Config"]); //z EADeploy.pas
    //}

    //public static bool isEABuildCD {
    //  get { return isEABuildCDEx(HttpContext.Current) && !ConfigLow.isLMComCacheDeployment(); }
    //}

    ////static Regex agentEx = null;
    //public static bool isCrawlerEx(HttpContext ctx) {
    //  string agent = ctx.Request.UserAgent;
    //  if (string.IsNullOrEmpty(agent)) return false;
    //  return Filter.isMatch(ctx, "crawlers", agent);
    //  /*if (agentEx == null) {
    //    string crvs = ConfigurationManager.AppSettings["Machines.Crawlers"];
    //    agentEx = new Regex(crvs, RegexOptions.Compiled);
    //  }
    //  bool res = agentEx.IsMatch(agent);
    //  return res;*/
    //}

    //public static bool isCrawler {
    //  get { return isCrawlerEx(HttpContext.Current); }
    //}

    public static bool isBuildEACache_BuildCD_Crawler {
      get {
        return false;
      }
    }


    static string adminIP = ConfigurationManager.AppSettings["Machines.AdminIP"];

    public static void checkAdminIP(HttpContext ctx) {
      string ip = ctx.Request.UserHostAddress;
      if (ip.IndexOf("192.168.0.") == 0) return;
      if (adminIP.IndexOf(ip) >= 0) return;
      ctx.Response.Write("<h1>" + CSLocalize.localize("586eda399bb0468a9c26226270eefa4d", LocPageGroup.LMComLib, "Neoprávněný přístup ") + ctx.Request.UserHostAddress + "</h1>");
      ctx.Response.End();
    }

    public static bool isSeznamDebug() {
      return isPZComp();
    }


    public static bool isFE5() {
      return machine == "LM-FRONTEND-5".ToLower();
    }
    public static bool isPZComp() {
      //return false;
      bool res = machine == pz_comp || machine == "pz-w8" || machine == jz_comp || machine == "zvahov";
      return res && ConfigurationManager.AppSettings["ignoreIsPZComp"] != "true";
    }

  }
}
