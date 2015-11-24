using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.IO;
using System.Web.Hosting;
using System.Web.Profile;
using System.Threading;
using System.Globalization;
using System.CodeDom;
using System.Configuration;
using System.Diagnostics;
using LMNetLib;

namespace LMComLib {

  public class EaUrlInfoLib {
    public static CourseIds GetCourseId(string absoluteVirtualPath) {
      string path = urlInfoLow.AppDomainAppVirtualPath;
      path = absoluteVirtualPath.Substring(path.Length + 1);
      return (CourseIds)Enum.Parse(typeof(CourseIds), path.Split('/')[0], true);
    }
    public static string MapPath(string path) {
      path = VirtualPathUtility.ToAbsolute(path);
      string root = urlInfoLow.AppDomainAppVirtualPath;
      root = path.Substring(root.Length + 1);
      return @"Q:\LMNet2\WebApps\EduAuthorNew\" + root.Replace('/', '\\');
    }
  }

  public class ECannotFindUrl : Exception {
    public ECannotFindUrl(string url, string message)
      : base(message) {
      Url = url;
    }
    public string Url;
  }

  //public delegate void Process404ErrorEvent(Uri url);
  public delegate void Process404ErrorEvent(HttpContext ctx, string strServerName, string AbsolutePath, string query);
  public delegate string RedirectEvent(string url);

  public class urlInfoLow {
    public urlInfoLow(SiteMapNode nd)
      : this(nd.Url, null, false) {
      if (!int.TryParse(nd["dbId"], out dbId)) dbId = 0;
    }

    public static Process404ErrorEvent Process404Error;
    public static RedirectEvent RedirectDirectory;

    public enum urlInfoMode {
      no,
      eaSeznam,
      eaLmCz,
      eaCom,
      webSeznam,
      webLmCz,
    }

    public urlInfoLow(string url) : this(url, null) { }

    public urlInfoLow() { }

    public urlInfoLow(string url, string query) : this(url, query, true) { }

    urlInfoLow(string url, string query, bool addVirtualRootDir) {
      string[] parts = url.Split(new char[] { '/' }, 4);
      urlInfoMode Mode = urlInfoMode.no;
      string appVirtpath = urlInfoLow.AppDomainAppVirtualPath.ToLower();
      switch (appVirtpath) {
        case "": Mode = (parts[1] == "sz" ? urlInfoMode.no : urlInfoMode.webSeznam); break;
        //case "/data": Mode = urlInfoMode.no; break;
        case "/data": Mode = urlInfoMode.eaSeznam; break;
        case "/czdata": Mode = urlInfoMode.eaLmCz; break;
        case "/comcz":
        case "/comsk":
        case "/comen":
        case "/comvi":
        case "/comes":
        case "/comde":
        case "/comru":
        case "/comfr":
        case "/comit":
        case "/comlt":
        case "/combg":
        case "/comtr":
        case "/comkr":
        case "/comcn":
        case "/comth":
        case "/comhk":
        case "/compl":
        case "/eduauthornew":
          Mode = urlInfoMode.eaCom; break;
        case "/lmcom": Mode = (parts[2] == "com" || parts[2] == "org") ? urlInfoMode.no : urlInfoMode.webLmCz; break;
      }
      //Mode = urlInfoMode.webSeznam;
      switch (Mode) {
        case urlInfoMode.webSeznam:
        case urlInfoMode.webLmCz:
        case urlInfoMode.no:
          if (url.IndexOf("folder/") >= 0) { Type = SiteMapNodeType.folder; return; }
          if (adjustErrorUrl(ref url, ref query)) {
            if (!url.EndsWith("/")) url += '/';
            if (url.IndexOf('.') >= 0) {
              //Nastaveni Culture do threadu kvuli zobrazeni chybove hlasky ve spravnem jazyce
              parts = VirtualPathUtility.ToAppRelative(url.ToLower()).Split(new char[] { '/' }, 6);
              if (parts.Length > 3) {
                LangId = getLang((parts[3] == "sp-sp" ? "es-es" : parts[3]).Replace('-', '_'));
                try {
                  if (LangId != Langs.no) Thread.CurrentThread.CurrentUICulture = new CultureInfo(this.Lang);
                } catch { }
              }
              throw new ECannotFindUrl(url, "Cannot find URL: " + HttpContext.Current.Request.RawUrl + ", agent=" + HttpContext.Current.Request.UserAgent + ", from=" + (HttpContext.Current.Request.UrlReferrer == null ? null : HttpContext.Current.Request.UrlReferrer.AbsoluteUri));
            }
            isErrorPage = true;
          }
          string virtualRoot = addVirtualRootDir && Mode == urlInfoMode.webSeznam ? "/sz/web/cs-cz/pages" : null;
          VirtualRootAdded = virtualRoot != null;
          url = virtualRoot + url;
          RelativePath = VirtualPathUtility.ToAppRelative(url.ToLower());
          if (RelativePath.IndexOf("~/folder") == 0) { Type = SiteMapNodeType.folder; return; }
          parts = RelativePath.Split(new char[] { '/' }, 6);
          if (parts.Length < 6) return;
          string Site = parts[1]; SiteId = getSite(Site); if (SiteId == Domains.no) return;
          string App = parts[2]; AppId = getLMApp(App); if (AppId == LMApps.no) return;
          string Lang = parts[3]; LangId = getLang(Lang.Replace('-', '_')); if (LangId == Langs.no) return;
          Security = getSecurity(parts[4]); if (Security == SecurityDir.no) return;
          RestUrl = parts[5];
          if (isErrorPage) {
            RedirectDir = RedirectDirectory(RestUrl) + query;
            return;
          }
          Ext = VirtualPathUtility.GetExtension(RestUrl).Substring(1);
          Type = getNodeType(Ext);

          int idx = RestUrl.LastIndexOf('.');
          Name = RestUrl.Substring(0, idx);
          SiteName = Mode == urlInfoMode.no && AppId == LMApps.web ? LocalizeUrl.Delocalize(Security, Name, LangId) : Name;
          if (AppId == LMApps.web)
            RestUrl = parts[4] + "/" + RestUrl.Replace("home.aspx", null);

          break;
        case urlInfoMode.eaSeznam:
        case urlInfoMode.eaLmCz:
        case urlInfoMode.eaCom:
          Ext = VirtualPathUtility.GetExtension(url);
          Ext = string.IsNullOrEmpty(Ext) ? "" : Ext.Substring(1);
          Type = getNodeType(Ext);
          AppId = LMApps.ea;
          if (Mode != urlInfoMode.eaCom) {
            SiteId = Mode == urlInfoMode.eaLmCz ? Domains.cz : Domains.sz;
            LangId = Langs.cs_cz;
          } else {
            SiteId = Domains.com;
            switch (appVirtpath) {
              case "/comcz": LangId = Langs.cs_cz; break;
              case "/comsk": LangId = Langs.sk_sk; break;
              case "/comen": LangId = Langs.en_gb; break;
              case "/comvi": LangId = Langs.vi_vn; break;
              case "/comes": LangId = Langs.sp_sp; break;
              case "/comde": LangId = Langs.de_de; break;
              case "/comru": LangId = Langs.ru_ru; break;
              case "/comfr": LangId = Langs.fr_fr; break;
              case "/comit": LangId = Langs.it_it; break;
              case "/comlt": LangId = Langs.lt_lt; break;
              case "/comtr": LangId = Langs.tr_tr; break;
              case "/comkr": LangId = Langs.ko_kr; break;
              case "/comcn": LangId = Langs.zh_cn; break;
              case "/comth": LangId = Langs.th_th; break;
              case "/comhk": LangId = Langs.zh_hk; break;
              case "/compl": LangId = Langs.pl_pl; break;
              case "/combg": LangId = Langs.bg_bg; break;
              case "/combs": LangId = Langs.bs; break;
              case "/eduauthornew": LangId = Langs.en_gb; break;
            }
          }
          //DEBUG:
          //SiteId = Domains.com; LangId = Langs.fr_fr;
          Security = SecurityDir.Pages;
          //EACourse = ProductInfos.SpaceIdToCourseId(parts[2]);
          RelativePath = VirtualPathUtility.ToAppRelative(url.ToLower());
          break;
      }
      string debugLang = ConfigurationManager.AppSettings["Trados.Lang"];
      if (!string.IsNullOrEmpty(debugLang)) LangId = (Langs)Enum.Parse(typeof(Langs), debugLang.Replace('-', '_'), true);
    }

    //vraci true iff se error url nepodarilo vyresit.
    //Jinak vraci budto false a nebo se provede redirekt s ctx.Response.End() (v Process404Error)
    public static bool adjustErrorUrl(ref string url, ref string query) {
      if (Process404Error == null) return false;
      HttpContext ctx = HttpContext.Current; if (ctx == null) return false;
      if (url.IndexOf("404.aspx") < 0 || query == null || query.Length <= 2) return false;
      string errorUrl = query.Substring(5);
      Uri uri = new Uri(errorUrl);
      Process404Error(ctx, uri.Host.ToLower(), uri.AbsolutePath.ToLower(), uri.Query);
      url = uri.AbsolutePath;
      query = uri.Query;
      return true;
    }

    //protected const string seznamVirtualRootDir = "/sz/web/cs-cz/pages";
    //protected string seznamFileRootDir = seznamVirtualRootDir.Replace('/', '\\');
    //public static string virtualRootDir = System.Configuration.ConfigurationManager.AppSettings["VirtualPath.Root"];
    //public static string fileRootDir = System.Configuration.ConfigurationManager.AppSettings["VirtualPath.Root"].Replace('/', '\\');

    public static string AppDomainAppVirtualPath {
      get {
        string res = HttpRuntime.AppDomainAppVirtualPath;
        return res == "/" ? "" : res;
      }
    }
    /*public urlInfoLow(SiteMapNode nd)
      : this(nd.Url) {
      if (!int.TryParse(nd["dbId"], out dbId)) dbId = 0;
    }*/
    public urlInfoLow(urlInfoLow urlInfo) {
      //Mode = urlInfo.Mode;
      AppId = urlInfo.AppId;
      SiteId = urlInfo.SiteId;
      LangId = urlInfo.LangId;
      Security = urlInfo.Security;
      Name = urlInfo.Name;
      SiteName = urlInfo.SiteName;
      Ext = urlInfo.Ext;
      Type = urlInfo.Type;
      RelativePath = urlInfo.RelativePath;
      EACourse = urlInfo.EACourse;
      dbId = urlInfo.dbId;
      RestUrl = urlInfo.RestUrl;
      RedirectDir = urlInfo.RedirectDir;
    }
    public bool VirtualRootAdded; //do URL adresy pridan /sz/web/cs-cz/pages prefix 
    //public urlInfoMode Mode;
    public LMApps AppId;
    public Domains SiteId;
    public Langs LangId;
    public SecurityDir Security;
    public string RestUrl; //cast URL za security, pro web bez home.aspx
    public string Name; //cast URL za security, bez extenze. Pro lokalizovane URL adresy: lokalizovana Name
    public string SiteName; //pro lokalizovane URL adresy: delokalizovana Name
    public string Ext;
    public SiteMapNodeType Type;
    public string RelativePath; //Info z puvodniho requestu pred RewritePath
    public int dbId;
    public CourseIds EACourse;
    public string RedirectDir;

    public bool isErrorPage;

    public string App { get { return AppId.ToString(); } }
    public string Site { get { return SiteId.ToString(); } }
    public string Lang { get { return LangId.ToString().Replace('_', '-'); } }

    public string getFileName() {
      if (LibConfig.Usage == LibUsage.LMComWebAdmin)
        return string.Format(@"{0}{1}\web\lang\{2}\{3}.{4}", System.Configuration.ConfigurationManager.AppSettings["lmcomAppPath"],
          SiteId, Security, Name.Replace('/', '\\'), Ext);
      else {
        string res = string.Format(@"{0}{1}\web\lang\{2}\{3}.{4}", HostingEnvironment.ApplicationPhysicalPath, SiteId, Security, Name.Replace('/', '\\'), Ext);
        return res;
      }
    }

    public string Url() {
      return string.Format(@"~/{0}/web/lang/{1}/{2}.{3}", SiteId, Security, Name, Ext);
    }

    public string AbsUrl() {
      return VirtualPathUtility.ToAbsolute(Url());
    }

    public string VisibleUrl() {
      urlInfo info = urlInfo.GetUrlInfo();
      string res = string.Format(@"~/{0}/{1}/{2}/{3}/{4}.{5}", info.Site, App, info.Lang, Security, Name.ToLower(), Ext == "lmp" ? "aspx" : Ext);
      return res.Replace("home.aspx", null);
    }

    public string AbsVisibleUrl() {
      return VirtualPathUtility.ToAbsolute(VisibleUrl());
    }

    public string Url(Domains homeSite, int dbId) {
      if (Type == SiteMapNodeType.folder) return "folder/" + dbId.ToString();
      return string.Format(@"~/{0}/web/lang/{1}/{2}.{3}",
        homeSite != Domains.site && SiteId == Domains.site ? "site-" + homeSite.ToString() : SiteId.ToString(),
        Security, Name, Ext);
    }

    static urlInfoLow() {
      foreach (Domains dom in Enum.GetValues(typeof(Domains))) {
        domains.Add(dom.ToString().ToLower(), (int)dom);
        domains.Add("site-" + dom.ToString().ToLower(), (int)Domains.site);
      }
      foreach (Langs lng in Enum.GetValues(typeof(Langs)))
        try {
          langs.Add(lng.ToString().ToLower(), (int)lng);
        } catch (Exception e) {
          throw new Exception(lng.ToString(), e);
        }
      foreach (LMApps app in Enum.GetValues(typeof(LMApps))) apps.Add(app.ToString().ToLower(), (int)app);
      foreach (SecurityDir dir in Enum.GetValues(typeof(SecurityDir))) securs.Add(dir.ToString().ToLower(), (int)dir);
    }
    static Dictionary<string, int> langs = new Dictionary<string, int>();
    static Dictionary<string, int> domains = new Dictionary<string, int>();
    static Dictionary<string, int> apps = new Dictionary<string, int>();
    static Dictionary<string, int> securs = new Dictionary<string, int>();

    static SecurityDir getSecurity(string val) {
      int res;
      if (!securs.TryGetValue(val.ToLower(), out res)) return SecurityDir.no;
      return (SecurityDir)res;
    }
    static SiteMapNodeType getNodeType(string ext) {
      switch (ext.ToLower()) {
        case "lmp":
          return SiteMapNodeType.lmp;
        case "aspx":
          return SiteMapNodeType.aspx;
        case "folder":
          return SiteMapNodeType.folder;
        case "htm":
        case "html":
          return SiteMapNodeType.page;
        case "bmp":
        case "gif":
        case "jpg":
        case "png":
          return SiteMapNodeType.img;
        default:
          return SiteMapNodeType.no;
      }
    }
    static Domains getSite(string val) {
      int res;
      if (!domains.TryGetValue(val.ToLower(), out res)) return Domains.no;
      return (Domains)res;
    }
    static LMApps getLMApp(string val) {
      int res;
      if (!apps.TryGetValue(val.ToLower(), out res)) return LMApps.no;
      return (LMApps)res;
    }
    static Langs getLang(string val) {
      int res;
      if (!langs.TryGetValue(val.ToLower(), out res)) return Langs.no;
      return (Langs)res;
    }
    int? imgW = 0;
    int? imgH = 0;
    void adjutImg() {
      string fn = getFileName();
      if (!File.Exists(fn)) throw new Exception(string.Format("File {0} does not exist.", fn));
      System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(fn);
      imgW = bmp.Width;
      imgH = bmp.Height;
    }
    public int imgWidth() {
      if (imgW == 0) adjutImg();
      return (int)imgW;
    }

    public int imgHeight() {
      if (imgH == 0) adjutImg();
      return (int)imgH;
    }

  }

  public class EReplaceLang : Exception {
    public EReplaceLang(string msg) : base(msg) { }
  }

  /// <summary>
  /// Informace o requestu, ziskana z URL.
  /// </summary>
  public class urlInfo : urlInfoLow {

    /*static urlInfoMode getMode() {
      string mode = System.Configuration.ConfigurationManager.AppSettings["urlInfo.Mode"];
      if (string.IsNullOrEmpty(mode)) return urlInfoMode.no;
      return (urlInfoMode)Enum.Parse(typeof(urlInfoMode), mode, true);
    }*/

    public static Langs langStrToLang(string lng) {
      if (string.IsNullOrEmpty(lng)) lng = "en_gb";
      if (lng == "sp-sp") lng = "es-es";
      lng = lng.Replace("-", "_");
      return LowUtils.EnumParse<Langs>(lng);
    }

    public urlInfo(HttpContext ctx, string subSite, string lng) {
      SubSite = LowUtils.EnumParse<SubDomains>(subSite);
      LangId = langStrToLang(lng);
      SiteId = Domains.com;
      ctx.Items[uiKey] = this;
    }
    public urlInfo(string url)
      : base(url) {
      HttpContext.Current.Items[uiKey] = this;
    }
    public urlInfo()
      : base() {
      HttpContext.Current.Items[uiKey] = this;
    }
    /// <summary>
    /// konstructor pro runtime
    /// </summary>
    /// <param name="req"></param>
    public urlInfo(HttpRequest req)
      : base(req.Url.AbsolutePath, req.Url.Query) {
      HttpContext ctx = HttpContext.Current;
      ctx.Items[uiKey] = this;
      //SubSite = SiteId == Domains.com ? SubDomain.hostToComSubdomain(req.Url.Host) : SubDomains.no;
      if (AppId == LMApps.ea) return;
      if ((this.Type == SiteMapNodeType.aspx || this.Type == SiteMapNodeType.lmp) && this.LangId == Langs.lang)
        throw new EReplaceLang("Replace /LANG/ to /cs-cz/ in browser URL");
      if (isErrorPage) {
        AbsolutePath = VirtualPathUtility.ToAbsolute(RelativePath);
        AbsoluteUri = "http://" + req.Url.Host + AbsolutePath;
        if (RedirectDir != null) {
          if (VirtualRootAdded) AbsoluteUri = AbsoluteUri.Replace("/sz/web/cs-cz/pages", null);
          ctx.Response.Status = "301 Moved Permanently";
          ctx.Response.AddHeader("Location", AbsoluteUri + RedirectDir);
          ctx.Response.End();
        }
      } else {
        AbsoluteUri = req.Url.AbsoluteUri;
        AbsolutePath = req.Url.AbsolutePath;
      }
      switch (Type) {
        case SiteMapNodeType.aspx:
        case SiteMapNodeType.lmp:
          try {
            foreach (string site in new string[] { Site, "site" })
              foreach (string ext in new string[] { "lmp", "aspx" }) {
                string fixPart = "/" + App + "/lang/" + Security.ToString() + "/" + SiteName + "."; //fixni cast vsech URL
                //Nalezeni node v SiteMap
                if (site == "site") {
                  Node = SiteMap.Provider.FindSiteMapNode("~/site-" + Site + fixPart + ext); //spolecna stranka v narodnim sitemap
                  if (Node == null)
                    Node = SiteMap.Provider.FindSiteMapNode("~/site" + fixPart + ext); //spolecna stranka ve spolecnem sitemap
                } else
                  Node = SiteMap.Provider.FindSiteMapNode("~/" + site + fixPart + ext); //narodni stranka v narodni sitemap
                //nalezeni souboru, redirekt URL apod.
                if (Node != null) {
                  RewriteUrl = urlInfoLow.AppDomainAppVirtualPath + "/" + site + fixPart + "aspx";
                  isLMP = ext == "lmp";
                  return;
                }
              }
          } finally {
            if (Node != null && !int.TryParse(Node["dbId"], out dbId))
              dbId = 0;
          }
          break;
      }
    }

    /*public bool isFullLang() {
      return CommonLib.bigLocalizations.Contains(LangId);
    }*/

    public bool isPayPalOnlyCommerce() {
      return SiteId == Domains.com;
    }

    public static void normalizeCulture() {
      CultureInfo ci = Thread.CurrentThread.CurrentUICulture;
      if (!ci.IsNeutralCulture) return;
      switch (ci.Name) {
        case "en": ci = CultureInfo.CreateSpecificCulture("en-GB"); break;
        default: ci = CultureInfo.CreateSpecificCulture(ci.Name); break;
      }
      Thread.CurrentThread.CurrentCulture = ci;
      Thread.CurrentThread.CurrentUICulture = ci;
    }

    public static void setCulture(Langs lang) {
      setCulture(lang.ToString());
    }

    public static string setCulture(string lang) {
      return (Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = cultureInfo(lang)).Name;
    }

    public static CultureInfo cultureInfo(string lang) {
      try {
        return new CultureInfo(cultureName(lang));
      } catch {
        return new CultureInfo("en-GB");
      }
    }
    public static string cultureName(string lang) {
      if (string.IsNullOrEmpty(lang) || lang == "no") return "cs-cz";
      lang = lang.Replace("_", "-");
      if (lang == "sp-sp") lang = "es-es";
      return lang;
    }

    public void setCulture() {
      string lng = Lang;
      if (Thread.CurrentThread.CurrentUICulture.Name == lng) return;
      setCulture(lng);
    }

    /*public static string ChangeUrl(string url, string site, string app, string lang, string security) {
      return ChangeUrl(new urlInfoLow(url), site, app, lang, security);
    }*/

    public static string ChangeUrl(string url, string site, string app, string lang, string security) {
      urlInfoLow ui = new urlInfoLow(url);
      return ChangeUrl(ui, site == null ? ui.Site : site, app == null ? ui.App : app, lang == null ? ui.Lang : lang, security == null ? ui.Security.ToString() : security);
    }

    public static string ChangeUrl(urlInfoLow ui, string site, string app, string lang, string security) {
      if (ui.Type == SiteMapNodeType.no) return null;
      if (site == null) site = ui.Site;
      if (lang == null) lang = ui.Lang;
      string res = "~/" + site + "/" + (app == null ? ui.App : app) + "/" + lang + "/" + (security == null ? ui.Security.ToString() : security) + "/" + ui.Name + "." + ui.Ext;
      return LocalizeUrl.UrlLocalize(res, (Domains)Enum.Parse(typeof(Domains), site, true), (Langs)Enum.Parse(typeof(Langs), lang.Replace('-', '_'), true));
    }
    public string RewriteUrl; //adresa, odpovidajici existujicimu .ASPX nebo .LMP souboru
    public string AbsoluteUri; //Info z puvodniho requestu pred RewritePath
    public SubDomains SubSite;
    public string AbsolutePath; //Info z puvodniho requestu pred RewritePath
    //public LMComLib.Cms.Template CmsTemplate; //Template, zjistovany v runtime v AuthenticateRequest
    public SiteMapNode Node; //SiteMapNode, zjistovany v runtime v AuthenticateRequest
    public bool isLMP; //LMP nebo ASPX soubor. Zjistovano z existence prislusneho souboru

    static string uiKey = Guid.NewGuid().ToString();

    public static string siteApp(Domains site, LMApps app) {
      return site.ToString() + app.ToString();
    }

    public string siteApp() {
      return siteApp(SiteId, AppId);
    }

    public static urlInfo GetUrlInfo() {
      return GetUrlInfo(HttpContext.Current);
    }

    public struct SeeAlsoInfo {
      //public string IconUrl { get; set; }
      public string LineClass { get; set; }
      public string Url { get; set; }
      public string Title { get; set; }
    }

    public IEnumerable<SeeAlsoInfo> GetSeeAlsoInfo(IEnumerable<Langs> langs, string urlPostfix, Control ctrl) {
      var parts = Node.Url.ToLowerInvariant().Split(new string[] { "/pages/" }, StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length != 2) return Enumerable.Empty<SeeAlsoInfo>();
      string page = "pages/" + parts[1];
      return langs.Select(l => new SeeAlsoInfo() {
        LineClass = ctrl == null ? null : "comp-mod-icon-" + CommonLib.LangToLineId(l),
        //IconUrl = ctrl==null ? null : ctrl.ResolveClientUrl("~/flags/small_bitmaps/" + CommonLib.LangToLineId(l).ToString() + ".png"),
        //Url = urlInfo.getUrl(SiteId, SubDomainInfo.LANGMasterSubDomain(l), l.ToString().Replace('_', '-'), page) + urlPostfix,
        Title = CommonLib.langTitle[l]
      }).OrderBy(l => l.Title);
    }

    public static urlInfo GetUrlInfo(HttpContext ctx) {
      return ctx == null ? null : (urlInfo)HttpContext.Current.Items[uiKey];
    }

    //public static string GetUrl(LMApps appId, string path) {
    //  return GetUrlInfo().getUrl(appId, path);
    //}

    //public string getUrl(LMApps appId, string path) {
    //  return getUrl(SiteId, SubSite, appId, Lang, path, VirtualRootAdded);
    //}

    //public static string getUrl(Domains site, SubDomains subSite, string lang, string path) {
    //  return getUrl(site, subSite, LMApps.web, lang, path, true);
    //}

    //public string getUrlEa(CourseIds crsId, string path) {
    //  return getUrl(LMApps.ea, crsId.ToString().Replace('_', '-') + "/" + path);
    //}

    //public string getUrl(Domains site, SubDomains subSite, LMApps appId, string path) {
    //  return getUrl(site, subSite, appId, Lang, path, VirtualRootAdded);
    //}

    public static bool isSeznam() {
      urlInfo ui = urlInfo.GetUrlInfo();
      return ui != null && ui.SiteId == Domains.sz;
    }

    //public static string getDefaultUrl(Domains site, SubDomains subSite, LMApps appId, string lang, string path, bool virtualRootAdded) {
    //  path = path.ToLower();
    //  if (appId == LMApps.web && path != null)
    //    path = path.Replace(".lmp", ".aspx").Replace("home.aspx", null);
    //  string basicPath = SiteInfos.getDefaultAuthority(site, SiteInfos.c_appId, appId.ToString());
    //  switch (appId) {
    //    case LMApps.ea: return basicPath + path;
    //    case LMApps.web: if (virtualRootAdded) return basicPath + (path == null ? null : path.Replace("pages/", null)); else break;
    //  }
    //  return string.Format(@"{0}{1}/{2}/{3}/{4}", basicPath, site, appId, lang, path);
    //}

    //public static string getUrl(Domains site, SubDomains subSite, LMApps appId, string lang, string path, bool virtualRootAdded) {
    //  return getUrl(site, subSite, appId, lang, path, virtualRootAdded, Machines.isBuildEACache);
    //}

    public static string comBasicPath() {
      return comBasicPath(urlInfo.GetUrlInfo().SubSite, LMApps.web);
    }

    public static string comBasicPath(SubDomains subSite, LMApps appId) {
      return null;
      //switch (appId) {
      //  case LMApps.commerce:
      //  case LMApps.web:
      //    return SubDomain.subdomainToUrl(subSite) + "/lmcom/";
      //  case LMApps.ea:
      //  //return SubDomain.subdomainToUrl(SubDomains.com) + "/";
      //  case LMApps.no:
      //    return SubDomain.subdomainToUrl(subSite) + "/";
      //  default:
      //    throw new NotImplementedException();
      //}
    }

    //public static string getUrl(Domains site, SubDomains subSite, LMApps appId, string lang, string path, bool virtualRootAdded, bool isDefault) {
    //  path = path.ToLowerInvariant();
    //  if (appId == LMApps.web && path != null)
    //    path = path.Replace(".lmp", ".aspx").Replace("home.aspx", null);
    //  string basicPath;
    //  if (site == Domains.com) basicPath = comBasicPath(subSite, appId);
    //  else if (site == Domains.cz) basicPath = "http://www.langmaster.cz/lmcom/";
    //  else basicPath = isDefault ?
    //      SiteInfos.getDefaultAuthority(site, SiteInfos.c_appId, (appId == LMApps.no ? LMApps.web : appId).ToString()) :
    //      SiteInfos.getAuthority(site, appId == LMApps.no ? LMApps.web : appId);

    //  switch (appId) {
    //    case LMApps.no: return basicPath + path;
    //    case LMApps.ea:
    //      if (site == Domains.cz || site == Domains.sz) return basicPath + path;
    //      Langs lng = (Langs)Enum.Parse(typeof(Langs), lang.Replace('-', '_'));
    //      return basicPath + LangToEADir(lng) + "/" + path;
    //    case LMApps.web:
    //      if (virtualRootAdded && site == Domains.sz)
    //        return basicPath + (path == null ? null : path.Replace("pages/", null));
    //      else
    //        break;
    //  }
    //  if ((site == Domains.com || site == Domains.org) && appId == LMApps.web) {
    //    path = LocalizeUrl.Localize(path, lang);
    //  }
    //  return string.Format(@"{0}{1}/{2}/{3}/{4}", basicPath, site, appId, lang, path);
    //}

    public static string LangToEADir(Langs lng) {
      switch (lng) {
        case Langs.cs_cz: return "comcz";
        case Langs.sk_sk: return "comsk";
        case Langs.en_gb: return "comen";
        case Langs.sp_sp:
        case Langs.es_es: return "comes";
        case Langs.de_de: return "comde";
        case Langs.ru_ru: return "comru";
        case Langs.fr_fr: return "comfr";
        case Langs.it_it: return "comit";
        case Langs.zh_cn: return "comcn";
        case Langs.ko_kr: return "comko";
        case Langs.lt_lt: return "comlt";
        case Langs.tr_tr: return "comtr";
        case Langs.bs: return "combs";
        case Langs.bg_bg: return "combg";
        case Langs.zh_hk: return "comth";
        case Langs.th_th: return "comhk";
        case Langs.pl_pl: return "compl";
        case Langs.vi_vn: return "comvi";
        default: return null; 
        //default: throw new Exception("Missing code here");
      }
    }

    //public static string GetUrl(SiteMapNode nd) {
    //  string url = VirtualPathUtility.ToAppRelative(nd.Url.ToLowerInvariant());
    //  string[] parts = url.Split(new char[] { '/' }, 5);
    //  if (parts.Length != 5) throw new Exception();
    //  return GetUrl((LMApps)Enum.Parse(typeof(LMApps), parts[2], true), parts[4]);
    //}

    public static string getTitle(Domains site, string lng, string path) {
      SiteMapNode nd = SiteMap.Provider.FindSiteMapNode("~/site-" + site.ToString() + "/web/lang/" + path);
      if (nd == null) return "urlInfo.getTitle";
      CultureInfo oldCi = Thread.CurrentThread.CurrentUICulture;
      try {
        urlInfo.setCulture(lng);
        return nd.Title;
      } finally { Thread.CurrentThread.CurrentUICulture = oldCi; }
    }

    //public static string GetUrl(SiteMapNode nd, SubDomains subSite, Langs lng) {
    //  string url = VirtualPathUtility.ToAppRelative(nd.Url.ToLower());
    //  string[] parts = url.Split(new char[] { '/' }, 5);
    //  if (parts.Length != 5) throw new Exception();
    //  string site = parts[1];
    //  if (site.StartsWith("site-")) site = site.Substring(5, site.Length - 5);
    //  return getUrl(
    //    (Domains)Enum.Parse(typeof(Domains), site, true),
    //    subSite,
    //    (LMApps)Enum.Parse(typeof(LMApps), parts[2], true),
    //    lng.ToString().Replace('_', '-'),
    //    parts[4],
    //    true);
    //}

    //public static string HomeUrl(SubDomains subSite) {
    //  urlInfo ui = urlInfo.GetUrlInfo();
    //  if (ui == null) return "about:blank";
    //  return getUrl(ui.SiteId, subSite, LMApps.web, ui.Lang, "pages/", true);
    //  //return GetUrl(LMApps.web, "pages/");
    //}


    public bool isHomeUrl() {
      return Name == "home"; //)||(Name.EndsWith("/home"));
    }

    public string priceText(double price) {
      return priceText(SiteId, SubSite, price);
    }

    /*static CultureInfo kcCulture = CultureInfo.CreateSpecificCulture("cs-cz");
    static CultureInfo euroCulture = CultureInfo.CreateSpecificCulture("de-de");
    static CultureInfo usaCulture = CultureInfo.CreateSpecificCulture("en-us");
    static CultureInfo plCulture = CultureInfo.CreateSpecificCulture("pl-pl");
    static CultureInfo viCulture = CultureInfo.CreateSpecificCulture("vi-vn");
    static CultureInfo ltCulture = CultureInfo.CreateSpecificCulture("lt-lt");
    static CultureInfo ruCulture = CultureInfo.CreateSpecificCulture("ru-ru");
    static CultureInfo trCulture = CultureInfo.CreateSpecificCulture("tr-tr");
    static CultureInfo zhCulture = CultureInfo.CreateSpecificCulture("zh_cn");*/

    public static string priceText(CurrencyType type, double price) {
      return price.ToString("C", Currency.CurrencyCulture(type));
    }

    public static string priceText(Domains site, SubDomains subsite, double price) {
      switch (site) {
        case Domains.el:
        case Domains.sz:
        case Domains.cz: return price.ToString("C", Currency.kcCulture);
        //case Domains.com: return priceText(SubDomain.subDomainToCurr(subsite), price);
        default: throw new Exception("Missing code here");
      }
    }

    public static double roundPrice(CurrencyType type, double price) {
      int dec;
      switch (type) {
        case CurrencyType.csk:
        case CurrencyType.czk: dec = 0; break;
        default: dec = 1; break;
      }
      return Math.Round(price, dec);
    }

  }

  public class LMPage : Page {

    /// <summary>
    /// kazda stranka nastavuje Thread culture pro sebe (v ui.setCulture(), dle URL adresy)
    /// pro /site/web/lang/pages/Facebook/Main.master a pro Services\Select-language-com.aspx
    /// - kazda stranka nastavuje Lang do cookie, aby se priste objevil stejny jazyk pro neautorizovaneho uzivatele
    /// - kazda stranka nastavuje Lang do profilu, aby se priste objevil stejny jazyk pro autorizovaneho uzivatele
    /// </summary>
    /// <param name="ctx"></param>
    public static void DoInitializeCulture(HttpContext ctx) {
      urlInfo ui = urlInfo.GetUrlInfo();
      if (ui == null) return;
      ui.setCulture();
      switch (ui.SiteId) {
        case Domains.cz:
        case Domains.sz:
        case Domains.gopas:
          break;
        default:
          //LMStatus.Profile.Lang = ui.Lang;
          LMCookie.write(CookieIds.lang, ui.Lang, true);
          //ctx.Response.Cookies[LMWeb.Lib.lang_cookId].Value = ui.Lang;
          //ctx.Response.Cookies[LMWeb.Lib.lang_cookId].Expires = DateTime.UtcNow.AddMonths(1);
          break;
      }
    }

    public static Dictionary<string, Langs> cultures = CommonLib.smallLocalizations.Where(l => l != Langs.pt_br).
      ToDictionary(l => l.ToString().Split('_')[0].ToLower().Replace("sp", "es"));
    public static Dictionary<string, Langs> bigCultures = CommonLib.bigLocalizations.
      ToDictionary(l => l.ToString().Split('_')[0].ToLower().Replace("sp", "es"));

    /// <summary>
    /// Prevzeti jazykovych informaci (pro http://localhost/lmcom/Redirect.aspx, coz je difotni stranka .com a .org sites)
    /// </summary>
    public static string GetLang(HttpContext ctx, Dictionary<string, Langs> availableLangs) {
      if (ctx == null) return "en-gb";
      //jazyk je jiz zvolen a je v cookie
      string lng = LMCookie.read(CookieIds.lang); // ctx.Request[LMWeb.Lib.lang_cookId]; //ctx.Request.Params gets a combined collection of QueryString, Form, Cookies, and ServerVariables items.
      //difotni jazyk z prohlizece: prvni z UserLanguages, ktery je v availableLangs
      if (lng == null && availableLangs != null) {
        Langs langId = Langs.no;
        if (ctx.Request.UserLanguages != null && availableLangs != null)
          ctx.Request.UserLanguages.FirstOrDefault(
            l => availableLangs.TryGetValue(l.Split('-', ';')[0].ToLower(), out langId)
          );
        if (langId != Langs.no) lng = langId.ToString().Replace('_', '-');
      }
      if (lng == null) lng = "en-gb";
      return lng;
    }
    public static void EraseLang(HttpContext ctx, Dictionary<string, Langs> availableLangs) {
      LMCookie.remove(CookieIds.lang);
      //ctx.Response.Cookies.Add(new HttpCookie(LMWeb.Lib.lang_cookId, null) { Expires = DateTime.UtcNow });
    }
    //Osetri cookie
    protected override void InitializeCulture() {
      DoInitializeCulture(Context);
    }
  }

  public class GetUrlExpressionBuilder : System.Web.Compilation.ExpressionBuilder {
    public override bool SupportsEvaluate {
      get { return false; }
    }
    public override object EvaluateExpression(object target, BoundPropertyEntry entry, object parsedData, System.Web.Compilation.ExpressionBuilderContext context) {
      return null;
    }
    //public static string getUrl(string data) {
    //  if (string.IsNullOrEmpty(data)) return "";
    //  urlInfo ui = urlInfo.GetUrlInfo(); if (ui == null) return "";
    //  string[] parts = data.Split(',');
    //  LMApps app; string url;
    //  switch (parts.Length) {
    //    case 1:
    //      app = ui.AppId; url = parts[0];
    //      return ui.getUrl(app, url);
    //    case 2:
    //      app = (LMApps)Enum.Parse(typeof(LMApps), parts[0], true); url = parts[1];
    //      return ui.getUrl(app, url);
    //    case 3:
    //      Domains site = LowUtils.EnumParse<Domains>(parts[0]);
    //      app = (LMApps)Enum.Parse(typeof(LMApps), parts[1], true); url = parts[2];
    //      return ui.getUrl(site, ui.SubSite, app, url);
    //    default:
    //      return "error";
    //  }
    //}
    public override System.CodeDom.CodeExpression GetCodeExpression(BoundPropertyEntry entry, object parsedData, System.Web.Compilation.ExpressionBuilderContext context) {
      return new CodeMethodInvokeExpression(
        new CodeTypeReferenceExpression(typeof(GetUrlExpressionBuilder)),
        "getUrl",
        new System.CodeDom.CodePrimitiveExpression(entry.Expression));
    }





  }


}
