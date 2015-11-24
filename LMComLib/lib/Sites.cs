using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Globalization;
using System.Xml.Serialization;
using System.Threading;

using LMNetLib;

namespace LMComLib {

  [EnumDescrAttribute(typeof(LMApps), "web=web,commerce=commerce,ea=vyukova data")]
  public enum LMApps {
    no,
    /// <summary>
    /// commercni nadstavba (CMS stranky)
    /// </summary>
    web,
    /// <summary>
    /// EduAuthor
    /// </summary>
    ea,
    commerce,
    /*z historickych duvodu
    download,
    security,
    register,
    /// <summary>
    /// aplikace, vyzadujici databazi
    /// </summary>
    db,
     */
  }

  [EnumDescrAttribute(typeof(SecurityDir), "Pages=Anonymous,Secured=Logged,Admin=Administrator")]
  public enum SecurityDir {
    no,
    Pages,
    Secured,
    Admin,
  }
  public enum SiteMapNodeType {
    no,
    lmp,
    aspx,
    page,
    img,
    //virtualImg,
    folder
  }


  ///// <summary>
  ///// Konfigurace domen
  ///// </summary>
  //public class SiteInfos {
  //  /// <summary>
  //  /// Seznam konfiguraci jednotlivych domen
  //  /// Domena s identifikaci Domains.no slouzi jako vzor: jeji vlastnosti se vyuziji jako difotni hodnoty
  //  /// </summary>
  //  public SiteInfo[] Items;

  //  static SiteInfos instance;
  //  public static SiteInfos Instance {
  //    get {
  //      lock (typeof(SiteInfos)) {
  //        if (instance == null) {
  //          //instance = (SiteInfos)XmlUtils.FileToObject(System.Configuration.ConfigurationManager.AppSettings["sitesInfo"], typeof(SiteInfos));
  //          instance = (SiteInfos)XmlUtils.FileToObject(Machines.basicPath + @"rew\LMCom\App_Data\Sites.xml", typeof(SiteInfos));
  //          SiteInfo init = null;
  //          foreach (SiteInfo si in instance.Items)
  //            if (si.Id == Domains.no) { init = si; break; }
  //          foreach (SiteInfo si in instance.Items)
  //            if (si.Id != Domains.no)
  //              si.Finish(init);
  //        }
  //        return instance;
  //      }
  //    }
  //  }
  //  /// <summary>
  //  /// K identifikaci domeny vrati jeji konfiguracni objekt
  //  /// </summary>
  //  /// <param name="id">identifikace domeny</param>
  //  /// <returns>konfiguracni objekt</returns>
  //  public static SiteInfo getSiteInfo(Domains id) {
  //    foreach (SiteInfo si in Instance.Items)
  //      if (si.Id == id) return si;
  //    throw new Exception(string.Format("LMComLib.SiteInfos.getSiteInfo: missing SiteInfo in ~/Sites.xml ({0})", id));
  //  }

  //  //******* authority management
  //  public const string c_scormServerId = "scorm";
  //  public const string c_scormBuildServerId = "scormBuild";
  //  public const string c_registerOffline = "registeroffline";
  //  public const string c_remoteProfile = "remoteprofile";
  //  public const string c_rpcServerId = "rpcserver";
  //  public const string c_appId = "app";
  //  internal const string c_defaultMachine = "app";

  //  public static string RPC_defaultUrl(Domains site, string serverName) {
  //    return getDefaultAuthority(site, c_rpcServerId, serverName);
  //  }

  //  public static string RPC_url(Domains site, string serverName) {
  //    return getAuthority(site, c_rpcServerId, serverName);
  //  }

  //  public static string getAuthority(Domains site, LMApps app) {
  //    return getAuthority(site, c_appId, app.ToString());
  //  }

  //  public static string getAuthority(Domains site, string groupId, string authorityId) {
  //    SiteInfo si = getSiteInfo(site);
  //    string mach = System.Environment.MachineName.ToLower();
  //    string id = groupId + "." + authorityId;
  //    Dictionary<string, string> machValues;
  //    string val;
  //    if (si.machines.TryGetValue(mach, out machValues)) {
  //      if (machValues.TryGetValue(id, out val)) return val;
  //    }
  //    if (si.machines.TryGetValue(c_defaultMachine, out machValues)) {
  //      if (machValues.TryGetValue(id, out val)) return val;
  //    }
  //    return null;
  //  }

  //  public static string getDefaultAuthority(Domains site, string groupId, string authorityId) {
  //    SiteInfo si = getSiteInfo(site);
  //    string id = groupId + "." + authorityId;
  //    Dictionary<string, string> machValues;
  //    string val;
  //    if (si.machines.TryGetValue(c_defaultMachine, out machValues)) {
  //      if (machValues.TryGetValue(id, out val)) return val;
  //    }
  //    return null;
  //  }


  //}
  ///// <summary>
  ///// Konfigurace jedne domeny
  ///// </summary>
  //public class SiteInfo {
  //  /// <summary>
  //  /// Identifikace domeny
  //  /// </summary>
  //  public Domains Id;
  //  /// <summary>
  //  /// Difotni URL authority, napr. www.langmaster.cz nebo vyuka.lide.cz. Vraci ji funkce 
  //  /// getAppAuthority nebo getOtherAuthority v pripade, ze se nenajde konkretni autorita.
  //  /// </summary>
  //  public string Authority;
  //  /// <summary>
  //  /// Seznam jazykovych verzi, do kterych je aplikace na domene lokalizovana
  //  /// </summary>
  //  public string[] Langs;
  //  /// <summary>
  //  /// Nedifotni authorities pro jednotlive aplikace (LMApps), RPC (napr. scorm) apod.
  //  /// Format app.ea.pzika-notebook=localhost/eduauthor, app.ea=data.langmaster.cz, rpcserver.scorm=server.langmaster.cz apod., neboli
  //  /// GroupId, AuthorityId, nepovinny MachineName, value.
  //  /// </summary>
  //  public string[] Authorities;

  //  public string SubDownloadInfoPath;

  //  [XmlIgnore]
  //  internal Dictionary<string, Dictionary<string, string>> machines = new Dictionary<string, Dictionary<string, string>>();

  //  /// <summary>
  //  /// Konfigurace Security RPC serveru
  //  /// </summary>
  //  //public RpcSecufity Security;
  //  /// <summary>
  //  /// Konfigurace Logging RPC serveru
  //  /// </summary>
  //  //public RpcLogging Logging;
  //  /// <summary>
  //  /// Konfigurace Scorm RPC serveru
  //  /// </summary>
  //  //public RpcConfig Scorm;
  //  public CourseIds[] Products;
  //  public LMSType[] Lms;
  //  //public DownloadCenterConfig DownloadCenter;
  //  /// <summary>
  //  /// Dokončení konfigu: převzetí difotních hodnot
  //  /// </summary>
  //  /// <param name="init">SiteInfo objekt s difotními hodnotami</param>
  //  public void Finish(SiteInfo init) {
  //    if (Langs == null) Langs = init.Langs;
  //    if (Products == null) Products = init.Products;
  //    if (Lms == null) Lms = init.Lms;
  //    //Authorities
  //    if (Authorities != null)
  //      foreach (string s in Authorities) {
  //        string[] eqParts = s.Split('=');
  //        string[] dotParts = eqParts[0].Split('.');
  //        string[] appParts = dotParts[1].Split(':');
  //        //Machine
  //        string machine = dotParts.Length == 2 ? SiteInfos.c_defaultMachine : dotParts[2];
  //        Dictionary<string, string> machineValues;
  //        if (!machines.TryGetValue(machine, out machineValues)) {
  //          machineValues = new Dictionary<string, string>();
  //          machines.Add(machine, machineValues);
  //        }
  //        //hodnoty
  //        foreach (string app in appParts)
  //          machineValues.Add(dotParts[0] + "." + app, eqParts[1]);
  //      }
  //    /*string authority = SiteInfos.Authority(Id);
  //    if (Security == null) Security = new RpcSecufity(); Security.Finish(init.Security, authority);
  //    if (Scorm == null) Scorm = new RpcConfig(); Scorm.Finish(init.Scorm, authority);
  //    if (DownloadCenter == null) DownloadCenter = new DownloadCenterConfig(); DownloadCenter.Finish(init.DownloadCenter, authority);*/
  //  }

  //  string normalizeLang(CultureInfo cult) {
  //    if (Langs == null || Langs.Length == 0) return null;
  //    string lng = cult.Name == "" ? "en" : (cult.IsNeutralCulture ? cult.Name : cult.Parent.Name);
  //    switch (lng) {
  //      case "en": lng = "en-GB"; break;
  //      default: lng = CultureInfo.CreateSpecificCulture(lng).Name; break;
  //    }
  //    if (Array.IndexOf<string>(Langs, lng) < 0) return null;
  //    return lng;
  //  }

  //  public void setLang(string defaultValue) {
  //    string lng = null;
  //    if (!string.IsNullOrEmpty(defaultValue))
  //      lng = normalizeLang(new CultureInfo(defaultValue));
  //    if (lng == null)
  //      normalizeLang(Thread.CurrentThread.CurrentUICulture);
  //    if (lng == null)
  //      lng = Langs == null || Langs.Length == 0 ? "en-GB" : Langs[0];
  //    urlInfo.setCulture(lng);
  //    /*if (Thread.CurrentThread.CurrentUICulture.Name != lng) {
  //      Thread.CurrentThread.CurrentCulture = new CultureInfo(lng);
  //      Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;
  //    }*/
  //  }
  //}

  /// <summary>
  /// K identifikaci domeny vrati authority, napr. langmaster.us, vuyka.lide.cz apod.
  /// </summary>
  /// <param name="dom">identifikace domeny</param>
  /// <returns>authority</returns>
  /*static string fakeAuthority = Guid.NewGuid().ToString();
  static string Authority(Domains dom)
  {
    if (dom == Domains.site) return fakeAuthority; 
    foreach (SiteInfo si in Instance.Items)
      if (si.Id == dom) return si.Authority.ToLower();
    throw new Exception("SiteInfos.Authority: " + dom.ToString());
  }*/
  /*public static string NormalizeAuthority()
  {
    return Authority(Domain(Authority()));
  }*/
  /// <summary>
  /// K authority vrati identifikaci domeny
  /// </summary>
  /// <param name="authority">authority, napr. moodle.vyuka.lide.cz nebo moodle.langmaster.us</param>
  /// <returns>identifikace domeny</returns>
  /*static Domains Domain(string authority)
  {
    foreach (Domains dom in Enum.GetValues(typeof(Domains)))
      if (authority.ToLower().IndexOf(Authority(dom)) >= 0)
        return dom;
    return Domains.localhost;
    //throw new Exception(string.Format("SiteInfos.Domain: {0}", authority));
  }*/
  /// <summary>
  /// Vrati URL s RPC serverem pro danou domenu a nazev RPC funkce.
  /// URL adresa je ulozena v jednom z RPC configu (RpcConfig.Url). Tento konfig se nalezne 
  /// dle RpcConfig.Prefix (musi byt stejny jako cast nazvu RPC funkce pred teckou).
  /// </summary>
  /// <param name="domain">identifikace domeny</param>
  /// <param name="fncName">jmeno funkce ve formatu prefix.nazev</param>
  /// <returns></returns>
  /*public static string RPC_url(Domains domain, string fncName)
  {
    string prefix = fncName.Split('.')[0].ToLower();
    SiteInfo site = getSiteInfo(domain);
    foreach (RpcConfig cfg in new RpcConfig[] { site.Security, site.Scorm, site.Logging })
      if (cfg.Prefix.ToLower() == prefix) return cfg.Url;
    throw new Exception(string.Format("SiteInfos.RPC_url: cannot find url for {0}.{1}", domain, fncName));
  }*/
  /*static string Authority()
  {
    if (HttpContext.Current == null) return null;
    return HttpContext.Current.Request.Url.Authority;
  }*/
}
/*
/// <summary>
/// Trida pro konfiguraci jednoho RPC serveru
/// </summary>
public class RpcConfig {
  /// <summary>
  /// Dokonceni objektu
  /// </summary>
  /// <param name="init">Objekt s difotnimu hodnotami</param>
  /// <param name="domain">retezec s domenou, napr. langmaster.cz nebo vyuka.lide.cz</param>
  public void Finish(RpcConfig init, string authority) {
    if (Prefix == null) Prefix = init.Prefix;
    if (Url == null) Url = init.Url;
    //Eventualni uprava obecne URL adresy: {0} je nahrazeno authority.
    //Napr. z http://security.{0} vznikne http://security.vyuka.lide.cz
    Url = string.Format(Url, authority);
  }
  /// <summary>
  /// Prefix pro nazev RPC funkci, napr. Test.
  /// </summary>
  public string Prefix;
  /// <summary>
  /// URL adresa stranky s RPC serverem
  /// </summary>
  public string Url;
}
/// <summary>
/// Konfigurace Security serveru (uzivatelsky profil)
/// </summary>
public class RpcSecufity : RpcConfig {
  /// <summary>
  /// URL adresa s login dialogem
  /// </summary>
  public string LoginUrl;
  public void Finish(RpcSecufity init, string authority) {
    base.Finish(init, authority);
    if (LoginUrl == null) LoginUrl = init.LoginUrl;
    LoginUrl = string.Format(LoginUrl, authority);
  }
}
/// <summary>
/// Logging server, s filtrovanim, jake vsechny kategorie chyb se loguji. 
/// Je-li ContextFilter=null, loguje se vse
/// </summary>
public class RpcLogging : RpcConfig {
  /// <summary>
  /// Seznam kategorii, ktere se loguji
  /// </summary>
  public string[] ContextFilter;
}
public class DownloadCenterConfig {
  public string UpdateUrl;
  public void Finish(DownloadCenterConfig init, string authority) {
    if (UpdateUrl == null) UpdateUrl = init.UpdateUrl;
    UpdateUrl = string.Format(UpdateUrl, authority);
  }
}
 * */
