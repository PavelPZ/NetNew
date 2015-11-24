//TODO
// - trasovani Security modulu
// - fake Login.aspx, která redirektuje na správnou Login.apsx
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using System.Web.SessionState;
using System.Xml;
using System.Xml.Linq;
using System.Globalization;
using System.Xml.Serialization;
using System.Configuration;
using System.Web.Security;
using System.Web.Profile;
using System.Security.Principal;
using System.Text;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using LMNetLib;

namespace LMComLib {

  /// <summary>
  /// Helper objekt pro User management
  /// </summary>
  //public static class LMStatus {

  //  public static bool IsInRole(LMComRole role) {
  //    long r = (long) role;
  //    return (Cookie.roles & r) == r;
  //  }

  //  /// <summary>
  //  /// Vrati ticket-cookie, vytvori kdyz neexistuje.
  //  /// Zaroven zajisti existenci aktualniho profilu v cache.
  //  /// </summary>
  //  public static LMCookie Cookie {
  //    get { return getCookie(); }
  //  }

  //  static public bool IsAuth() { return Cookie != null && !string.IsNullOrEmpty(Cookie.EMail); }

  //  /// <summary>
  //  /// Vrati ticket-cookie, nevytvari, kdyz neexistuje.
  //  /// </summary>
  //  public static LMCookie CookieLow {
  //    get { return GetCookieLow(HttpContext.Current); }
  //  }

  //  public static LMCookie GetCookieLow(HttpContext ctx) {
  //    return ctx == null ? null : (LMCookie)ctx.Items[LowUtils.c_cookieName];
  //  }

  //  /// <summary>
  //  /// ID aktualniho profilu
  //  /// </summary>
  //  public static Int64 UserId {
  //    get { return Cookie.id; }
  //  }

  //  /// <summary>
  //  /// Aktualni profil
  //  /// </summary>
  //  public static ProfileData Profile {
  //    get {
  //      getCookie();
  //      return LMComDataProvider.getProfileFromCache(UserId);
  //    }
  //  }

  //  /// <summary>
  //  /// Vola se vzdy, kdyz uzivatel pomoci GUI provedl Logout
  //  /// </summary>
  //  //public static void logout() {
  //  //  trace(null, "LMCookie.logout Start: {0}", HttpContext.Current.User.Identity.Name);
  //  //  loginLogout(null, null);
  //  //  Order.ClearInstance();
  //  //  //HttpContext.Current.Response.Redirect(urlInfo.HomeUrl());
  //  //}

  //  /*public static void logoutNew() {
  //    trace(null, "LMCookie.logout Start: {0}", HttpContext.Current.User.Identity.Name);
  //    loginLogout(null, null);
  //    Order.ClearInstance();
  //  }*/
  //  /// <summary>
  //  /// Vola se na konci Loginu
  //  /// </summary>
  //  public static bool login(string eMail, string psw) {
  //    trace(null, "LMCookie.login Start: {0}", eMail);
  //    return loginLogout(eMail, psw);
  //  }

  //  public static string getPassword(string email) {
  //    return LMComDataProvider.Instance.getPassword(email);
  //  }

  //  ////Prevod profilu z vyuky na lm.cz
  //  //public static string createUserFromVyuka(string eMail, string psw) {
  //  //  ProfileData prof = LMComDataProvider.readProfile(eMail, null);
  //  //  if (prof == null) return string.Format("Uživatel {0} neexistuje!", eMail);
  //  //  if (prof.Site != Domains.sz) return string.Format("Profil uživatele {0} je již převeden!", eMail);
  //  //  prof.Password = psw;
  //  //  LMComDataProvider.WriteProfile(prof);
  //  //  MailSender.sendMail(MailTypes.ConfirmRegistration, Domains.com, SubDomains.com_cz, Langs.cs_cz, eMail, prof);
  //  //  return null;
  //  //}

  //  //public static bool createUserStart(string eMail, string psw) {
  //  //  return createUserStart(urlInfo.GetUrlInfo(), eMail, psw, null, null)>0;
  //  //}

  //  public static Int64 rew_createUserStart(SubDomains subSite, string email, string login, Action<ProfileData> fillProfile) {
  //    Int64 id = LMComDataProvider.rew_readProfileId(email, login, null);
  //    if (id > 0) return -1;
  //    //if (id > 0) return id;
  //    ProfileData prof = LMComDataProvider.createProfileStart(Domains.com);
  //    if (login != null) {
  //      prof.Roles = 0; prof.ActivationMailSent = null;
  //    } else
  //      prof.ActivationMailSent = DateTime.UtcNow;
  //    prof.Email = email;
  //    prof.Login = login;
  //    fillProfile(prof);
  //    LMComDataProvider.WriteProfile(prof);
  //    return prof.Id;
  //  }

  //  //public static bool rew_fillProfile(Int64 id, Func<ProfileData,bool> fillProfile) {
  //  //  LMComData2.User user; var db = Machines.getContext();
  //  //  var prof = localProvider.readProfileLow(db, id, out user);
  //  //  if (prof==null || !fillProfile(prof)) return false;
  //  //  localProvider.updateProfileLow(user, prof);
  //  //  db.SubmitChanges();
  //  //  return true;
  //  //}

  //  public static void rew_createUserEnd(Int64 id) {
  //    ProfileData prof = LMComDataProvider.readProfile(id);
  //    if (prof == null) throw new Exception();
  //    if (!prof.Anonymous) return;
  //    prof.ActivationMailSent = null; prof.Roles = 0;
  //    LMComDataProvider.WriteProfile(prof);
  //  }

  //  public static Int64 rew_OnOtherLogin(OtherType type, string id, string email, string firstName, string lastName) {
  //    if (string.IsNullOrEmpty(email)) return -1;
  //    email = email.ToLower();
  //    //hotovo:
  //    ProfileData prof = LMComDataProvider.readOtherProfile(type, id, email);
  //    if (prof == null) { //profil neni zalozen v databazi - zaloz
  //      prof = LMComDataProvider.createProfileStart(Domains.com);
  //      prof.OtherType = type;
  //      prof.OtherId = id;
  //      prof.Roles = 0;
  //      prof.Email = email;
  //      prof.Address.FirstName = firstName;
  //      prof.Address.LastName = lastName;
  //      prof.Site = Domains.com;
  //    } else {
  //      prof.Site = Domains.com;
  //      prof.OtherType = type;
  //      prof.OtherId = id;
  //      prof.Email = email;
  //      prof.Address.FirstName = firstName;
  //      prof.Address.LastName = lastName;
  //    }
  //    //vymaz ostatni profily s tim samym emailem
  //    LMComDataProvider.removeDuplicetedEMails(email, prof.Id);
  //    LMComDataProvider.WriteProfile(prof);
  //    return prof.Id;
  //  }



  //  /// <summary>
  //  /// Pokus o zalozeni uzivatele: v pripade jiz existujiciho mailu vrati false.
  //  /// Nasledne posle aktivacni mail se zakodovanum UserId, eMail a Password
  //  /// ui==null => pouze zalozi uzivatele ale neposila email. Vyuzito napr pro eTestMe.com, v Q:\lmcom\RW2\Server\Services\MembersipNewService.svc.cs
  //  /// </summary>
  //  //public static Int64 createUserStart(urlInfo ui, string eMail, string psw, string firstName, string lastName) {
  //  //  Int64 id = LMComDataProvider.readProfileId (eMail, null);
  //  //  if (id>0) return -1;
  //  //  ProfileData prof = Profile;
  //  //  if (!prof.Anonymous) throw new Exception();
  //  //  prof = LMComDataProvider.createProfileStart(ui == null ? Domains.com : ui.SiteId);
  //  //  prof.ActivationMailSent = DateTime.UtcNow;
  //  //  prof.Address.FirstName = firstName; prof.Address.LastName = lastName;
  //  //  prof.Email = eMail; prof.Password = psw;
  //  //  if (ui != null) {
  //  //    if (Machines.debugNoInternet) {
  //  //      prof.Roles = 0; prof.ActivationMailSent = null; //simulace createUserEnd pro ladeni bez internetu
  //  //    } else
  //  //      MailSender.sendMail(MailTypes.ConfirmRegistration, ui.SiteId, ui.SubSite, ui.LangId, prof.Email, prof);
  //  //    loginProfile(prof);
  //  //  }
  //  //  LMComDataProvider.WriteProfile(prof);
  //  //  return prof.Id;
  //  //}

  //  /// <summary>
  //  /// Reakce na link z aktivacniho mailu: do profilu daneho Id vlozi Roles, cimz ucet aktivuje.
  //  /// </summary>
  //  public static ProfileData createUserEnd(urlInfo ui, Int64 id, Int64 roles) {
  //    ProfileData prof = LMComDataProvider.readProfile(id);
  //    if (prof == null) throw new Exception();
  //    if (prof.Site == Domains.sz) { //Prevod profilu z Vyuky
  //      prof.Site = Domains.com;
  //      prof.OtherType = OtherType.no;
  //      prof.OtherId = null;
  //    } else if (!prof.Anonymous)
  //      return null;
  //    prof.ActivationMailSent = null; prof.Roles = roles;
  //    if (ui != null) {
  //      loginProfile(prof);
  //    }
  //    LMComDataProvider.WriteProfile(prof);
  //    return prof;
  //  }
  //  public static ProfileData createUserEnd(urlInfo ui, Int64 id) {
  //    return createUserEnd(ui, id, 0);
  //  }

  //  public static ProfileData createUserEnd(Int64 id) {
  //    return createUserEnd(urlInfo.GetUrlInfo(), id);
  //  }

  //  /// <summary>
  //  /// autentifikace na zacatku kazdeho requestu (nebo pri login x logout)
  //  /// </summary>
  //  public static void autenticate(LMCookie cook) {
  //    if (cook == null) cook = Cookie;
  //    HttpContext.Current.User = new GenericPrincipal(new GenericIdentity(cook.EMail == null ? "" : cook.EMail), null/*PZ NEW cook.roleStrings()*/);
  //  }

  //  /// <summary>
  //  /// Ulozeni profilu, aktualizce jeho verze v cookie
  //  /// </summary>
  //  /// <returns></returns>
  //  public static void saveProfile() {
  //    LMComDataProvider.WriteProfile();
  //    //LMCookie cook = CookieLow;
  //    //if (cook == null) throw new Exception();
  //  }

  //  /// <summary>
  //  /// Volana v PostRequestHandlerExecute, v lmcom i ea
  //  /// </summary>
  //  public static void saveProfileAndCookie(HttpContext ctx) {
  //    //Uschovej Cookie
  //    LMCookie cook = saveCookie(ctx);
  //    //Anonymni nebo zadne cookie: return
  //    if (cook == null || cook.id <= 0) return;
  //    //Uschovej profil (pokud se behem requestu zmenil)
  //    ProfileData prof = LMComDataProvider.getProfileFromCache(ctx);
  //    if (prof == null) return;
  //    urlInfo ui = urlInfo.GetUrlInfo(ctx);
  //    if (ui != null) prof.Site = ui.SiteId;
  //    prof.IpAddress = LowUtils.IPAddressToInt(ctx.Request.UserHostAddress);
  //    LMComDataProvider.WriteProfile(prof);
  //  }

  //  public static LMCookie saveCookie(HttpContext ctx) {
  //    //if (Machines.isBuildEACache_BuildCD_CrawlerLow(ctx)) return null;
  //    LMCookie cook = GetCookieLow(ctx); if (cook == null) return null;
  //    cook.saveCookie(ctx);
  //    return cook;
  //  }

  //  public static void trace(LMCookie cook, string msg) {
  //    Logging.Trace(TraceLevel.Verbose, TraceCategory.Security, cook == null ? msg : cook.id + ": " + msg);
  //  }

  //  public static void trace(LMCookie cook, string msg, params object[] pars) {
  //    Logging.Trace(TraceLevel.Verbose, TraceCategory.Security, cook == null ? string.Format(msg, pars) : string.Format(cook.id + ": " + msg, pars));
  //  }

  //  public static void loginProfile(Int64 userId) {
  //    ProfileData prof = LMComDataProvider.readProfileEx(userId, true);
  //    loginProfile(prof);
  //  }

  //  const ushort key = 32719;
  //  public static string encodeAutorisedUser(Int64 userId, string url) {
  //    byte[] data = BitConverter.GetBytes(userId);
  //    url = url.Substring(0, url.LastIndexOf('.'));
  //    if (url.Length > 10) url = url.Substring(url.Length - 10);
  //    byte[] urlByte = Encoding.UTF8.GetBytes(url.ToLower());
  //    byte[] res = new byte[data.Length + urlByte.Length];
  //    data.CopyTo(res, 0); urlByte.CopyTo(res, data.Length);
  //    return "securityId=" + ConvertNew.ToBase32(res, key, true);
  //  }
  //  public static bool decodeAutorisedUser(string code, out Int64 userId, out string url) {
  //    userId = 0; url = null;
  //    try {
  //      byte[] res = ConvertNew.FromBase32(code, key);
  //      byte[] urlByte = new byte[res.Length - 8];
  //      Array.Copy(res, 8, urlByte, 0, urlByte.Length);
  //      userId = BitConverter.ToInt64(res, 0);
  //      url = Encoding.UTF8.GetString(urlByte);
  //      return true;
  //    } catch {
  //      return false;
  //    }
  //  }

  //  public static void loginAndSaveProfile(ProfileData prof) {
  //    loginProfile(prof);
  //    LMComDataProvider.WriteProfile(prof);
  //  }

  //  public static void loginProfile(ProfileData prof) {
  //    if (prof == null) return;

  //    //10.7.2009, nektere profily v DB maji Site=0, pro jistotu...
  //    urlInfo ui = urlInfo.GetUrlInfo();
  //    if (ui != null) prof.Site = ui.SiteId;

  //    LMComDataProvider.setProfileToCache(prof);

  //    //PZ 12.7.08: cookie se pro noveho uzivatele musi vytvorit kompletne znova
  //    LMCookie cook = new LMCookie(); HttpContext.Current.Items[LowUtils.c_cookieName] = cook;

  //    cook.id = prof.Id;
  //    cook.EMail = prof.Anonymous ? null : prof.Email;
  //    cook.FirstName = prof.Address.FirstName;
  //    cook.LastName = prof.Address.LastName;
  //    cook.Login = prof.Login;
  //    cook.LoginEMail = prof.LoginEMail;
  //    cook.roles = prof.Roles == null ? 0 : (Int64)prof.Roles;
  //    cook.TypeId = prof.OtherId;
  //    cook.Type = prof.OtherType;
  //    LMStatus.saveCookie(HttpContext.Current);

  //    LMStatus.autenticate(cook);

  //  }

  //  public static bool loginLogout(string userName, string psw) {
  //    trace(null, "LMCookie.logout Start: {0}", HttpContext.Current.User.Identity.Name);
  //    ProfileData prof;
  //    HttpContext ctx = HttpContext.Current;
  //    if (userName == null) {
  //      //Novy anonymni profil
  //      prof = LMComDataProvider.getProfileFromCache(-LMComDataProvider.getUniqueId(LMComDataProvider.uiUserId));
  //      //
  //    } else {
  //      prof = LMComDataProvider.readProfile(userName, psw);
  //      if (prof != null && prof.Anonymous) prof = null;
  //    }
  //    if (prof == null) return false;
  //    loginProfile(prof);
  //    return true;
  //  }

  //  //public static string c_cookieName = "LMTicket";
  //  /// <summary>
  //  /// Konstanta: cas pro expiraci cookie (1 hodina)
  //  /// </summary>
  //  public static TimeSpan cookieExpiration = new TimeSpan(1, 0, 0);

  //  public static void createMoodleCookie(HttpContext ctx, Int64 userId) {
  //    ProfileData prof = LMComDataProvider.readProfile(userId);
  //    if (prof == null || prof.Anonymous) throw new Exception();
  //    loginProfile(prof);
  //  }

  //  static void checkCookie(HttpContext ctx, LMCookie cook) {
  //    if (!string.IsNullOrEmpty(cook.EMail)) { // && Machines.isCrawlerEx(ctx)) {
  //      Emailer em = new Emailer(null, "error@langmaster.cz", "Invalid crawler: ", ctx.Request.UserAgent);
  //      //em.From = "error@langmaster.cz";
  //      em.AddTo("pjanecek@langmaster.cz");
  //      em.AddTo("pzika@langmaster.cz");
  //      //em.Subject = "Invalid crawler: " + ctx.Request.UserAgent;
  //      em.SendMail();
  //    }
  //  }

  //  static LMCookie getCookie() {
  //    LMCookie cook = CookieLow;
  //    if (cook != null) return cook; //cookie jiz vytvoren a ulozen v HttpContext.Current.Items
  //    urlInfo ui = urlInfo.GetUrlInfo();
  //    HttpContext ctx = HttpContext.Current;
  //    //**** Fake cookie pro ladeni autorizace Seznamu
  //    /*string sezLog = ConfigurationManager.AppSettings["debug.logToSeznam"];
  //    if (!string.IsNullOrEmpty(sezLog)) {
  //      Int64 userId;
  //      if (Int64.TryParse(sezLog, out userId)) {
  //        cook = new LMCookie(); cook.id = userId; cook.EMail = "debug@email.cz"; cook.seznamId = 1;
  //        ctx.Items[LowUtils.c_cookieName] = cook;
  //        checkCookie(ctx, cook);
  //        if (ui != null && ui.EACourse != CourseIds.no) cook.courseId = ui.EACourse.ToString();
  //        refreshLastRequest(ctx, cook);
  //        return cook;
  //      }
  //    }*/
  //    //**** LM Cookie
  //    cook = LMCookie.DeserializeCookie();
  //    //**** Seznam cookie? Hledá se specializovaný Seznam cookie
  //    LMCookie seznamCook = null; // LMComLib.Seznam.Utils.getSeznamCookie(ctx, cook, ui);
  //    if (seznamCook != null)
  //      cook = seznamCook;
  //    //**** LM anonymous cookie
  //    if (cook == null) {
  //      cook = new LMCookie();
  //      cook.id = -LMComDataProvider.getUniqueId(LMComDataProvider.uiUserId);
  //    }
  //    ctx.Items[LowUtils.c_cookieName] = cook;
  //    LMComDataProvider.checkProfileInCache(ctx, cook.id);
  //    checkCookie(ctx, cook);
  //    if (ui != null && ui.EACourse != CourseIds.no) cook.courseId = ui.EACourse.ToString();
  //    //refreshLastRequest(ctx, cook);
  //    return cook;
  //  }

  //  //static void refreshLastRequest(HttpContext ctx, LMCookie cook) {
  //  //  HttpCookie lastReqSaveCook = ctx.Request.Cookies["LastReqSave"];
  //  //  DateTime now = DateTime.UtcNow; //aktualizace data posledniho requestu
  //  //  DateTime lastReqSave = DateTime.MinValue; string crs = null;
  //  //  if (lastReqSaveCook != null) {
  //  //    try {
  //  //      string[] parts = lastReqSaveCook.Value.Split('|');
  //  //      lastReqSave = DateTime.Parse(parts[0]); crs = parts[1];
  //  //      Int64 lastReqUserId = Int64.Parse(parts[2]);
  //  //      if (lastReqUserId != cook.id) lastReqSave = DateTime.MinValue;
  //  //    } catch {
  //  //      lastReqSave = DateTime.MinValue; crs = null;
  //  //    }
  //  //  }
  //  //  TimeSpan ts = now - lastReqSave;
  //  //  urlInfo ui = urlInfo.GetUrlInfo();
  //  //  CourseIds crsId = ui == null ? CourseIds.no : ui.EACourse;
  //  //  if (crsId == CourseIds.no) crsId = cook.getCourseId();
  //  //  if (lastReqSaveCook == null || ts > Persistence.c_RefreshLastRequestInterval || crsId.ToString() != crs) { //... nejcasteji kazdych 3 minuty
  //  //    ctx.Response.Cookies["LastReqSave"].Value = now.ToString() + "|" + crsId.ToString() + "|" + cook.id.ToString();
  //  //    //ctx.Response.Cookies[LMStatus.c_cookieName].Value = JSON.ObjectToJSON(cook);
  //  //    Persistence.setLastRequest(ctx, cook, crsId);
  //  //  }
  //  //}

  //  public delegate string getOtherData(object obj);

  //  /*public static bool onGoogleFacebookLogin(HttpContext ctx, bool isFacebook, string id, string firstName, string lastName, string sex, string courseId, getOtherData otherDataEvent, object eventData) {
  //    LMCookie cook = LMStatus.Cookie;
  //    if (cook != null && cook.facebookId != 0 && isFacebook && cook.facebookId.ToString() == id && courseId == cook.courseId) return true;
  //    if (cook != null && cook.googleId != null && !isFacebook && cook.googleId == id && courseId == cook.courseId) return true;
  //    OtherType type = isFacebook ? OtherType.Facebook : OtherType.Google;
  //    return onGoogleFacebookLogin(ctx, type, id, id + "@" + type.ToString() + ".fake", firstName, lastName, sex, courseId, otherDataEvent, eventData);
  //  }*/

  //  public static bool onGoogleFacebookLogin(HttpContext ctx, OtherType type, string id, string email, string firstName, string lastName, string sex, string courseId, getOtherData otherDataEvent, object eventData) {
  //    if (string.IsNullOrEmpty(email)) return false;
  //    email = email.ToLower();
  //    //hotovo:
  //    string dataStr = otherDataEvent != null ? otherDataEvent(eventData) : null;
  //    ProfileData prof = LMComDataProvider.readOtherProfile(type, id, email);
  //    if (string.IsNullOrEmpty(email)) {
  //      if (prof != null) email = prof.Email;
  //      if (string.IsNullOrEmpty(email)) email = "fake@" + type.ToString() + ".www";
  //    }
  //    if (prof == null) { //profil neni zalozen v databazi - zaloz
  //      prof = LMComDataProvider.createProfileStart(Domains.com);
  //      prof.OtherType = type;
  //      prof.OtherId = id;
  //      prof.Roles = 0;
  //      prof.Email = email;
  //      prof.Address.FirstName = firstName;
  //      prof.Address.LastName = lastName;
  //      prof.Male = sex == null ? true : sex.ToLower() == "male";
  //      prof.Site = Domains.com;
  //      prof.OtherData = dataStr;
  //      LMComDataProvider.WriteProfile(prof);
  //    } else {
  //      prof.Site = Domains.com;
  //      prof.OtherData = dataStr;
  //      prof.OtherType = type;
  //      prof.OtherId = id;
  //      prof.Email = email;
  //      prof.Address.FirstName = firstName;
  //      prof.Address.LastName = lastName;
  //    }
  //    //vymaz ostatni profily s tim samym emailem
  //    LMComDataProvider.removeDuplicetedEMails(email, prof.Id);
  //    //
  //    LMComDataProvider.setProfileToCache(prof);
  //    LMCookie cook = new LMCookie();
  //    cook.EMail = prof.Email;
  //    cook.id = prof.Id;
  //    cook.Type = type;
  //    cook.TypeId = id;
  //    cook.FirstName = firstName;
  //    cook.LastName = lastName;
  //    HttpContext.Current.Items[LowUtils.c_cookieName] = cook;
  //    LMComDataProvider.checkProfileInCache(ctx, cook.id);
  //    //refreshLastRequest(ctx, cook);
  //    return false;
  //  }

  //  public static void fakeSetCookie(HttpContext ctx, string email) {
  //    LMCookie cook = new LMCookie();
  //    cook.EMail = email;
  //    cook.id = 123454321;
  //    cook.Type = OtherType.no;
  //    cook.TypeId = "123454321";
  //    HttpContext.Current.Items[LowUtils.c_cookieName] = cook;
  //    LMStatus.saveCookie(ctx);
  //  }

  //  public static void EMailLogin(HttpContext ctx, string email) {
  //    ProfileData prof = LMComDataProvider.readProfile(email, null);
  //    loginProfile(prof);
  //  }

  //  const int mdl_CompanyFldNum = 1;
  //  const int mdl_UrlFldNum = 2;

  //  //public static void refreshMoodleProfile(ProfileData prof, Moodle.mdl_user user) {
  //  //  prof.Email = user.email == null ? user.id + "@fake.com" : user.email;
  //  //  prof.Site = Domains.org;
  //  //  prof.Address.FirstName = user.firstname;
  //  //  prof.Address.LastName = user.lastname;
  //  //  try {
  //  //    prof.Address.CompanyName = user.mdl_user_info_datas.First(dt => Convert.ToInt32(dt.fieldid) == mdl_CompanyFldNum).data;
  //  //    prof.Address.Web = user.mdl_user_info_datas.First(dt => Convert.ToInt32(dt.fieldid) == mdl_UrlFldNum).data;
  //  //  } catch { }
  //  //}

  //  //public static bool onMoodleLogin(HttpContext ctx, Moodle.mdl_user user) {
  //  //  OtherType type = OtherType.Moodle;
  //  //  ProfileData prof = LMComDataProvider.readOtherProfile(type, user.id.ToString());
  //  //  if (prof == null) { //profil neni zalozen v databazi - zaloz
  //  //    prof = LMComDataProvider.createProfileStart(Domains.org);
  //  //    prof.OtherType = type;
  //  //    prof.OtherId = user.id.ToString();
  //  //    prof.Roles = 0;
  //  //  }
  //  //  refreshMoodleProfile(prof, user);
  //  //  LMComDataProvider.WriteProfile(prof);
  //  //  LMComDataProvider.setProfileToCache(prof);
  //  //  LMCookie cook = new LMCookie();
  //  //  cook.EMail = prof.Email;
  //  //  cook.id = prof.Id;
  //  //  cook.Type = OtherType.Moodle;
  //  //  cook.TypeId = user.username;
  //  //  HttpContext.Current.Items[LowUtils.c_cookieName] = cook;
  //  //  LMComDataProvider.checkProfileInCache(ctx, cook.id);
  //  //  refreshLastRequest(ctx, cook);
  //  //  return false;
  //  //}

  //  public static string GetActStatus(HttpContext context, LMCookie cook) {
  //    urlInfo ui = urlInfo.GetUrlInfo(context);
  //    string res;
  //    if (cook == null || cook.EMail == null) {
  //      res = "{'isAuthenticated':false,'id':-1";
  //      if (context.Request["forceLogin"] == "true") res += ",'forceLogin':true";
  //    } else {
  //      res = "{'isAuthenticated':true,'id':" + cook.id.ToString() + ",'server':";
  //      res += "'" + cook.Type.ToString() + "'";
  //      res += ",'courseId':'" + cook.courseId + "'";
  //      res += ",'email':'" + cook.EMail + "'";
  //    }
  //    res += ",'site':'" + ui.Site + "'";
  //    res += ",'country':'" + cook.Country + "','lang':'" + ui.Lang + "'";
  //    return res + "}";
  //  }

  //  /*public static string GetActStatus(HttpContext context) {
  //    urlInfo ui = urlInfo.GetUrlInfo(context);
  //    LMCookie cook = LMStatus.GetCookieLow(context);
  //    string res;
  //    if (cook == null || cook.EMail == null) {
  //      res = "{'isAuthenticated':false,'id':-1";
  //      if (context.Request["forceLogin"] == "true") res += ",'forceLogin':true";
  //    } else {
  //      res = "{'isAuthenticated':true,'id':" + cook.id.ToString() + ",'server':";
  //      res += "'" + cook.Type.ToString() + "'";
  //      res += ",'courseId':'" + cook.courseId + "'";
  //      res += ",'email':'" + cook.EMail + "'";
  //    }
  //    res += ",'site':'" + ui.Site + "'";
  //    res += ",'country':'" + cook.Country + "','lang':'" + ui.Lang + "'";
  //    return res + "}";
  //  }*/

  //}

  [EnumDescrAttribute(typeof(ProfileType), "personal=jednotlivec,company=firma,school=škola")]
  public enum ProfileType {
    personal, company, school
  };

  public class Address {
    /// <summary>
    /// Pro instituce (firmy, skoly): jmeno instituce
    /// </summary>
    public string CompanyName;
    public string FirstName;
    public string LastName;
    public string Street;
    public string City;
    public string Zip;
    public string Web;
    /// <summary>
    /// Z cisleniku
    /// kod |       popis        | regiondph 
    //AT  | Rakousko           | B
    //BE  | Belgie             | B
    //CY  | Kypr               | B
    //CZ  | Česká republika    | A
    //DE  | Německo            | B
    //DK  | Dánsko             | B
    //EE  | Estonsko           | B
    //ES  | Španělsko          | B
    //FI  | Finsko             | B
    //FR  | Francie (a Monako) | B
    //GB  | Spojené království | B
    //GR  | Řecko              | B
    //HU  | Maďarsko           | B
    //IE  | Irsko              | B
    //IT  | Itálie             | B
    //LT  | Litva              | B
    //LU  | Lucembursko        | B
    //LV  | Lotyšsko           | B
    //MT  | Malta              | B
    //NL  | Nizozemí           | B
    //PL  | Polsko             | B
    //PT  | Portugalsko        | B
    //SE  | Švédsko            | B
    //SI  | Slovinsko          | B
    //SK  | Slovensko          
    /// </summary>
    public string Country = "CZ";
    public string Fulltext() {
      return CompanyName + " " + FirstName + " " + LastName + " " + Street + " " + City + " " + Zip;
    }
    public string Title {
      get {
        string res = null;
        if (!string.IsNullOrEmpty(CompanyName))
          res = CompanyName + ": ";
        res += FirstName + " " + LastName;
        return res;
      }
    }
  }
  /// <summary>
  /// Pomocny objekt: vyznacna data z ProfileData, slouzici k zapisu do databaze
  /// </summary>
  public class ProfileDataCriteria {
    public ProfileDataCriteria(ProfileData prof) {
      Id = prof.Id;
      Email = prof.Email == null ? null : prof.Email.ToLower();
      Created = prof.Created;
      ActivationMailSent = prof.ActivationMailSent;
      Password = prof.Password;
      Roles = prof.Roles;
      //Version = prof.Version;
      Title = prof.Address.Title;
      Fulltext = prof.Fulltext;
      Type = prof.Type;
      Male = prof.Male;
      OtherType = prof.OtherType;
      OtherId = prof.OtherId;
      ICQ = prof.ICQ;
      FirstName = prof.Address.FirstName;
      LastName = prof.Address.LastName;
      Site = prof.Site;
      IpAddress = prof.IpAddress;
      Login = prof.Login;
    }
    public Int64 Id;
    public string Email;
    public string Login;
    public DateTime Created;
    public DateTime? ActivationMailSent;
    public string Password;
    public string Title;
    public string Fulltext;
    public Int64? Roles;
    //public int Version;
    public bool Male;
    public ProfileType Type;
    public OtherType OtherType;
    public string OtherId;
    public string FirstName;
    public string LastName;
    public string ICQ;
    public Domains Site;
    public uint IpAddress;
    public string getAsString() {
      return XmlUtils.ObjectToString(this);
    }
    public static ProfileDataCriteria setAsString(string s) {
      return (ProfileDataCriteria)XmlUtils.StringToObject(s, typeof(ProfileDataCriteria));
    }
  }

  public enum OtherType { 
    no = 0,
    Seznam = 1,
    Facebook = 2,
    Google = 3,
    Moodle = 4,
    Yahoo = 5,
    MyOpenId = 6,
    eTestMeId = 7,
    Microsoft = 8,
    LinkedIn = 9,
    LANGMaster = 10,
    LANGMasterNoEMail = 11,
    scorm = 12, //login z LMS. User login je roven company host plus scorm user Id
  }

  /// <summary>
  /// Zaznam o provedeni jedne akce
  /// </summary>
  public class FBActionHistory {
    public const string okResult = "ok";
    public const string cancelResult = "cancel";
    public const string ignoreResult = "ignore";
    public int SessionNum; //v jake session
    public DateTime SessionDate; //kdy
    public string Result = ignoreResult; //s jakym vysledkem
  }
  /// <summary>
  /// Hlida poradove cislo session, kdy byla provedena akce
  /// </summary>
  public class FBSessionNum {
    public byte Id;
    public List<FBActionHistory> History;
    public void OnAction(int sessionNum, string result) { //akce provedena s vysledkem...
      if (History == null) History = new List<FBActionHistory>();
      FBActionHistory last = LastAction();
      if (last != null && last.SessionNum == sessionNum) {
        if (last.Result == FBActionHistory.ignoreResult) last.Result = result;
      } if (last != null && last.Result == result && (result == FBActionHistory.okResult || result == FBActionHistory.ignoreResult)) { //OK a ignore vysledky se neopakuji
        last.SessionNum = sessionNum;
      } else
        History.Add(new FBActionHistory() { SessionNum = sessionNum, SessionDate = DateTime.UtcNow, Result = result });
    }
    public FBActionHistory LastAction() {
      if (History == null || History.Count == 0) return null;
      return History.Last();
    }
  }

  public class FBCourseProfile {
    //Identifikace kurzu 
    public CourseIds CrsId;
    public byte LastVisibleActionId; //Id akce, zjistene pro posledni session cislo LastActionIdSessionNum
    public int LastVisibleActionIdSessionNum; //cislo session, pro kterou byla naposledy zjistena akce
    public int LastOnlineActionsSessionNum; //cislo session, pro kterou byly naposledy provedeny online akce
    /// <summary>
    /// Seznam zaznamu o akcich
    /// </summary>
    public List<FBSessionNum> SessionNums;
    public FBSessionNum getSessionNum(byte id) {
      if (SessionNums == null) SessionNums = new List<FBSessionNum>();
      FBSessionNum res = SessionNums.FirstOrDefault(s => s.Id == id);
      if (res == null) SessionNums.Add(res = new FBSessionNum() { Id = id });
      return res;
    }
  }

  //public class FBProfile {
  //  public List<FBCourseProfile> Courses;
  //  public FBCourseProfile getCourse(CourseIds crsId) {
  //    if (Courses == null) Courses = new List<FBCourseProfile>();
  //    FBCourseProfile res = Courses.FirstOrDefault(c => c.CrsId == crsId);
  //    if (res == null) Courses.Add(res = new FBCourseProfile() { CrsId = crsId });
  //    return res;
  //  }
  //  public static FBSessionNum getSessionNum(CourseIds crsId, byte id) {
  //    return LMStatus.Profile.FB.getCourse(crsId).getSessionNum(id);
  //  }
  //  /// <summary>
  //  /// Data, vytahana z Profile.OtherData
  //  /// </summary>
  //  //[XmlIgnore]
  //  //public Dictionary<FBProfileDataEnum, string> otherData;
  //}
  //http://wiki.developers.facebook.com/index.php/Users.getInfo
  /*public enum FBProfileDataEnum {
    name,
    pic_small,
    profile_url
  }*/

  public class ProfileData {
    /// <summary>
    /// Jednoznacna identifikace profilu
    /// </summary>
    public Int64 Id;

    public Domains Site;

    /// <summary>
    /// IP adresa uzivatele
    /// </summary>
    public uint IpAddress;

    /// <summary>
    /// Identifikace ciziho profilu
    /// </summary>
    public OtherType OtherType;
    /// <summary>
    /// Hodnota cizi identifikace
    /// </summary>
    public string OtherId;
    /// <summary>
    /// Ostatni data, poskytnuta cizi aplikaci
    /// </summary>
    public string OtherData;
    /// <summary>
    /// Verze profilu, pocinaje od 1
    /// </summary>
    //public int Version;
    /// <summary>
    /// Priznak anonymniho profilu
    /// </summary>
    public bool Anonymous {
      get { return Id <= 0 || Roles == null; }
    }
    /// <summary>
    /// Role uživatele
    /// </summary>
    public Int64? Roles;
    /// <summary>
    /// EMail
    /// </summary>
    public string Email;
    public string Login;
    public string LoginEMail;
    public int DepartmentId;
    /// <summary>
    /// Datum vytvoreni profilu
    /// </summary>
    public DateTime Created;
    /// <summary>
    /// Datum zaslani mailu s potvrzenim registrace
    /// </summary>
    public DateTime? ActivationMailSent;
    /// <summary>
    /// Pro registrovaneho uzivatele: heslo, jinak null;
    /// </summary>
    public string Password;
    /// <summary>
    /// Preferovaný jazyk
    /// </summary>
    public string Lang;
    /// <summary>
    /// Preference: zasílat obchodní informace
    /// </summary>
    public bool SendBusinessInfo;
    /// <summary>
    /// Typ profilu
    /// </summary>
    public ProfileType Type;
    /// <summary>
    /// Pro instituce (firmy, skoly): ICO
    /// </summary>
    public string IC;
    /// <summary>
    /// Pro instituce (firmy, skoly): DIC
    /// </summary>
    public string DIC;
    /// <summary>
    /// Pro instituce (firmy, skoly): Fax
    /// </summary>
    public string Fax;
    /// <summary>
    /// Pro skoly navic: REDIZO
    /// </summary>
    public string REDIZO;
    /// <summary>
    /// Obsahuje telefoní kontakt
    /// </summary>
    public string Phone;

    /// <summary>
    /// Adresa pro fakturaci
    /// </summary>
    public Address Address = new Address();
    /// <summary>
    /// Adresa dodání
    /// </summary>
    public Address ShippingAddress = new Address();
    /// <summary>
    /// Priznak, zdali ShippingAddress obsahuje validni informace
    /// </summary>
    public bool HasShippingAddress;

    /// <summary>
    /// Adresa, ze které uživatel do lm.com poprvé přišel
    /// </summary>
    public string Referer;

    /// <summary>
    /// Adresa, ze které uživatel do lm.com poprvé přišel
    /// </summary>
    public string ICQ;

    /// <summary>
    /// Muz ci zena
    /// </summary>
    public bool Male;

    //FBProfile fb;
    //public FBProfile FB {
    //  get { if (fb == null) fb = new FBProfile(); return fb; }
    //  set { fb = value; }
    //}
    /*public Dictionary<FBProfileDataEnum, string> getFBOtherData() {
      if (FB.otherData != null) return FB.otherData;
      FB.otherData = new Dictionary<FBProfileDataEnum, string>();
      XElement el = string.IsNullOrEmpty(OtherData) ? null : XElement.Parse(OtherData);
      foreach (FBProfileDataEnum fld in Enum.GetValues(typeof(FBProfileDataEnum)))
        FB.otherData.Add(fld, el == null ? null : el.ElementValue(fld.ToString()));
      return FB.otherData;
    }*/

    //static char[] delims;
    /// <summary>
    /// Textová informace pro fulltext
    /// </summary>
    [XmlIgnore]
    public string Fulltext {
      get {
        string res = Email + " " + IC + " " + DIC + " " + Fax + " " + REDIZO + " " + Phone + " " + Address.Fulltext() + (HasShippingAddress ? " " + ShippingAddress.Fulltext() : null);
        return res;
        /*
        res = res.ToLower();
        string[] parts = res.Split(delims, StringSplitOptions.RemoveEmptyEntries);
        StringBuilder sb = new StringBuilder();
        foreach (string part in parts) {
          sb.Append(' '); sb.Append(part);
        }
        return sb.ToString();
        */
      }
    }

    /// <summary>
    /// Xml tvar objektu, kvuli zjisteni, zdali se profil zmenil
    /// </summary>
    [XmlIgnore]
    public string oldAsString;

    /// <summary>
    /// Serializace
    /// </summary>
    public string getAsString() {
      return XmlUtils.ObjectToString(this);
    }

    /// <summary>
    /// Deserializace
    /// </summary>
    public static ProfileData setAsString(string s) {
      return (ProfileData)XmlUtils.StringToObject(s, typeof(ProfileData));
    }

  }

  //[Serializable]
  //public class LMComProfile : ProfileBase {
  //  //ProfileData _Data;
  //  [SettingsAllowAnonymousAttribute(true)]
  //  public ProfileData Data {
  //    get { return LMStatus.Profile; }
  //    set { }
  //  }


  //}

  /// <summary>
  /// Deserializovany obsah cookie
  /// </summary>
  public partial class LMCookie {

    public static string read(CookieIds id, string def = null) {
      HttpCookie cook = HttpContext.Current.Request.Cookies[id.ToString()];
      return cook != null ? cook.Value : def;
    }
    public static void write(CookieIds id, string val, bool persist = false) {
      HttpContext.Current.Response.Cookies.Add(new HttpCookie(id.ToString(), val) {
        Domain = cookieDomain(HttpContext.Current),
        Path = "/",
        Expires = persist ? DateTime.UtcNow.AddMilliseconds(100000000) : DateTime.MinValue
      });
    }
    public static void remove(CookieIds id) {
      HttpContext.Current.Response.Cookies.Add(new HttpCookie(id.ToString(), "") {
        Domain = cookieDomain(HttpContext.Current),
        Path = "/",
        Expires = DateTime.UtcNow.AddYears(-1)
      });
    }

    public CourseIds getCourseId() {
      if (facebookId == 0 || string.IsNullOrEmpty(courseId)) return CourseIds.no;
      try {
        return (CourseIds)Enum.Parse(typeof(CourseIds), courseId);
      } catch {
        return CourseIds.no;
      }
    }

   
    //const ushort cookieCryptKey = 18475;
    /// <summary>
    /// Nacte a deserializuje cookie, vrati null, kdyz neexistuje
    /// </summary>
    /// <returns></returns>
    public static LMCookie DeserializeCookie() {
      return DeserializeCookie(HttpContext.Current);
    }

    public static LMCookie DeserializeJSCookie() {
      string val = LMCookie.read(CookieIds.LMJSTicket, null);
      if (val == null) return null;
      try {
        return LMCookie.FromString(val);
      } catch { return null; }
    }
    public static LMCookie DeserializeCookie(HttpContext ctx) {
      string val = LMCookie.read(CookieIds.LMTicket,null);
      if (val == null || Machines.isBuildEACache_BuildCD_Crawler) return null;
      try {
        return LMCookie.FromString(val);
        /*
        //val = Machines.isDebugDatabase ? cook.Value :
        val = LowUtils.DecryptHex(cook.Value, LowUtils.encryptKey);
        //LMCookie cookObj = (LMCookie)JSON.JSONToObject<LMCookie>(val);
        LMCookie cookObj = LowUtils.DeserializeJSONObject<LMCookie>(val);
        //if (cookObj.MachineName != System.Environment.MachineName) 
        //throw new Exception(string.Format("LoadBalance Error: oldMachine={0}, newMachine={1}", cookObj.MachineName, System.Environment.MachineName));
        return cookObj;*/
      } catch (Exception exp) {
        //if (Machines.machine == "lm-frontend-4")
        //Logging.Trace(TraceLevel.Error, TraceCategory.Security, "LMCookie.DeserializeCookie, cookie=" + val + ", cook.Value=" + val + ", error=" + exp.Message);
        return null;
      }
    }

    public static string cookieDomain(HttpContext ctx) {
      string authority = ctx.Request.Url.Authority;
      if (authority.IndexOf('.') >= 0 && !char.IsDigit(authority[0])) {
        string[] parts = authority.Split('.');
        if (parts.Length > 2) return parts[parts.Length - 2] + "." + parts[parts.Length - 1];
      }
      return null;
    }

    public void saveCookie(HttpContext ctx, bool persistent) {
      if (Machines.isBuildEACache_BuildCD_Crawler) return;
      LMCookie.write(CookieIds.LMTicket, ToString(), persistent);
    }
    public void saveCookie(HttpContext ctx) {
      saveCookie(ctx, false);
    }

  }

  
}