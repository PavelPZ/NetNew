//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.IO;
//using System.Diagnostics;
//using System.Web;
//using System.Web.Management;

//using LMNetLib;
//using LMComData2;

//namespace LMComLib {

//  public enum TraceCategory {
//    All,
//    JSONRpc,
//    Security,
//    Commerce,
//  }

//  [EnumDescrAttribute(typeof(EventCategory), "no=-,Error=Error,WebError=Web Error,Trace=Trace,Commerce=Commerce,Mail=Send Mail,Intranet=Intranet,SeznamRPC=Seznam RPC,DownloadMP3=Download MP3,debug=debug,PayPal=PayPal")]
//  public enum EventCategory {
//    no = 0,
//    Error = 1,
//    WebError = 2,
//    Trace = 3,
//    Commerce = 4,
//    Mail = 5,
//    Intranet = 6,
//    SeznamRPC = 7,
//    DownloadMP3 = 8,
//    debug = 9, //pro ladeni, vzdy je aktivni, nesmi se ale vyuzivat
//    PayPal = 10,
//    DownloadMoodle = 11,
//  }

//  //public class LMEvent : WebRequestEvent {
//  //  public string UserName;
//  //  public urlInfo info;
//  //  public LMCookie cook;
//  //  public string RequestUrl;
//  //  public Exception Exp;
//  //  //public string Referer;

//  //  public LMEvent(string message, object eventSource, int eventCode, int eventDetailCode)
//  //    : base(message, eventSource, WebEventCodes.WebExtendedBase + eventCode, eventDetailCode) {
//  //    info = urlInfo.GetUrlInfo();
//  //    cook = LMStatus.CookieLow;
//  //    if (HttpContext.Current != null) {
//  //      RequestUrl = HttpContext.Current.Request.Url.AbsolutePath;
//  //      RequestUrl += ", agent = " + HttpContext.Current.Request.UserAgent;
//  //      Uri rf = HttpContext.Current.Request.UrlReferrer;
//  //      if (rf != null) RequestUrl += ", referer = " + rf.AbsoluteUri;
//  //      //Referer = rf==null ? null : rf.AbsoluteUri;
//  //    }
//  //  }

//  //  public override string ToString() {
//  //    return Exp == null ? base.ToString() : base.ToString() + LowUtils.ExceptionToString(Exp, false, false);
//  //  }
//  //}

//  public class TraceEvent : LMEvent {
//    public TraceEvent(TraceLevel level, TraceCategory category, string message)
//      : base(HttpContext.Current == null ? message : HttpContext.Current.Request.UserHostAddress + ": " + message, null, (int)level, (int)category) {
//    }
//  }

//  public class ErrorEvent : TraceEvent {
//    public ErrorEvent(TraceLevel level, TraceCategory category, string message)
//      : base(level, category, message) { }
//  }

//  /// <summary>
//  /// Kody Commerce Events:
//  /// </summary>
//  [EnumDescrAttribute(typeof(CommerceEventIds), "Other=Ostatní,OrderCreate=Objednávka vytvořena,OrderSave=Objednávka uložena,OrderCopy=Kopie objednávky,PaymentStart=Přesměrování na platební bránu,PaymentEnd=Návrat z platební brány,Error=CHYBA,CreateDocument=Vytvoření dokladu,Status=Změna stavu,TaskCreated=Vytvoření úkolu,TaskFinished=Ukončení úkolu")]
//  public enum CommerceEventIds {
//    /// <summary>
//    /// Ostatni
//    /// </summary>
//    Other,
//    /// <summary>
//    /// Zacatek wizzarda, v eventDetailCode je identifikace objednávky
//    /// </summary>
//    OrderCreate,
//    /// <summary>
//    /// Ukonceni objednavky: ulozeni do databaze
//    /// </summary>
//    OrderSave,
//    /// <summary>
//    /// Kopie objednavky pri Dobirka znovu
//    /// </summary>
//    OrderCopy,
//    /// <summary>
//    /// Opusteni wizzarda a skok na platebni branu. V message je paypal, eBanka, payMuzo apod.
//    /// </summary>
//    PaymentStart,
//    /// <summary>
//    /// Navrat z wizzarda, message viz PaymentStart.
//    /// </summary>
//    PaymentEnd,
//    /// <summary>
//    /// Jakakoliv chybova hlaska, ktera se objevi uzivateli (spatny slevovy kupon apod.), v Message je text chyby, v eventDetailCode je identifikace objednávky.
//    /// </summary>
//    Error,
//    /// <summary>
//    /// Importovan doklad do Prosperu
//    /// </summary>
//    CreateDocument,
//    /// <summary>
//    /// Zmenen stav objednavky
//    /// </summary>
//    Status,
//    /// <summary>
//    /// Vytvoren task
//    /// </summary>
//    TaskCreated,
//    /// <summary>
//    /// Ukoncen task
//    /// </summary>
//    TaskFinished,

//    PaySend,
//    PayPalIPNStart,
//    PayPalIPNEnd,
//    PayPalIPNError,
//  }

//  public class CommerceEvent : LMEvent {
//    public int OrderId;

//    public CommerceEvent(CommerceEventIds eventCode, string message, int orderId)
//      : base(
//      (eventCode == CommerceEventIds.Other ? null : EnumDescrAttribute.getDescr(typeof(CommerceEventIds), (int)eventCode)) +
//      (message == null ? null : ": " + message),
//      null, (int)eventCode, 0) {
//      OrderId = orderId;
//    }
//    public CommerceEvent(CommerceEventIds eventCode, string message, int orderId, Exception exp): this (eventCode, message, orderId) {
//      detail = exp.StackTrace;
//    }
//    public CommerceEvent(CommerceEventIds eventCode, int orderId)
//      : this(eventCode, null, orderId) { }
//    public CommerceEvent(string message, int orderId)
//      : this(CommerceEventIds.Other, message, orderId) { }
//    string detail;
//    public override string ToString() {
//      return string.IsNullOrEmpty(detail) ? base.ToString() : detail;
//    }
//  }

//  public enum DownloadMode {
//    start,
//    end,
//    error,
//  }

//  public class DownloadEvent : LMEvent {
//    public DownloadEvent(DownloadMode mode, string url, int num)
//      : base(mode.ToString() + ": " + num.ToString() + " - " + url, null, (int)mode, num) {
//    }
//  }

//  //public class DownloadMoodleEvent : LMEvent {
//  //  public DownloadMoodleEvent(SL.Licence lic)
//  //    : base(ZipWrapper.SerializeToString(lic), null, 0, 0) {
//  //  }
//  //}

//  public class PayPalEvent : LMEvent {
//    public const int Completed = WebEventCodes.WebExtendedBase + 1;
//    public const int UnCompleted = WebEventCodes.WebExtendedBase + 2;
//    public int OrderId;
//    string detail;
//    public PayPalEvent(string msg, string detail): base (msg, null, 1, 1) {
//      this.detail = detail;
//    }
//    /*public PayPalEvent(PayPalIPN info)
//      : base(info.LogMessage(), null, info.Completed ? Completed - WebEventCodes.WebExtendedBase : UnCompleted - WebEventCodes.WebExtendedBase, (int)info.Price.Typ) {
//      detail = XmlUtils.ObjectToString(info);
//      OrderId = info.OrderId;
//    }*/
//    public override string ToString() {
//      return detail;
//    }
//  }

//  public class DebugEvent : LMEvent {
//    public DebugEvent(int num) : this(null, num) { }

//    public DebugEvent(string msg, int num)
//      : base(msg + ":" + num.ToString(), null, num, num) {
//    }
//  }

//  public enum SeznamProxyId {
//    no,
//    ITicket,
//    IUser,
//    ILideUser,
//    ISession,
//    IWallet,
//  }

//  public class SeznamRpcEvent : LMEvent {
//    public SeznamRpcEvent(bool isRequest, SeznamProxyId id, string url, string xml, long reqNum)
//      : base(makeMessage(isRequest, id, url, xml, reqNum), null, (int)id, (int)reqNum) {
//      this.xml = xml;
//    }
//    public SeznamRpcEvent(bool isRequest, SeznamProxyId id, string url, Stream str, long reqNum) : this(isRequest, id, url, streamToString(str), reqNum) { }
//    public SeznamRpcEvent(SeznamProxyId id, long reqNum, string exception) : base(makeError(exception), null, (int)id, (int)reqNum) { }

//    public static string streamToString(Stream str) {
//      str.Seek(0, SeekOrigin.Begin);
//      byte[] mem = new byte[str.Length];
//      str.Read(mem, 0, mem.Length);
//      return Encoding.UTF8.GetString(mem);
//    }

//    static string nameFromXml(string xml, bool isRequest) {
//      if (!isRequest) return null;
//      int begIdx = xml.IndexOf("<methodName>"); if (begIdx <= 0) return null;
//      begIdx += 12;
//      int endIdx = xml.IndexOf("</methodName>"); if (endIdx <= 0) return null;
//      string res = xml.Substring(begIdx, endIdx - begIdx);
//      string[] parts = res.Split('.');
//      if (parts.Length == 2) res = parts[1];
//      return "." + res;
//    }

//    public static string makeMessage(bool isRequest, SeznamProxyId id, string url, string xml, long reqNum) {
//      return (isRequest ? "Request: " : "Response: ") + id.ToString() + nameFromXml(xml, isRequest) + " " + url + " (" + reqNum + ")";
//    }

//    public static string makeError(string exception) {
//      return "Response ERROR: " + exception;
//    }

//    string xml;
//    public override string ToString() {
//      return xml;
//    }
//  }

//  public class MailEvent : LMEvent {
//    public int OrderId;
//    public string Text;
//    public MailEvent(string mailId, string text)
//      : this(mailId, 0, text) {
//    }
//    public MailEvent(string mailId, int orderId, string text)
//      : base("Poslán mail: " + mailId, null, 0, 0) {
//      OrderId = orderId;
//      Text = text;
//    }
//    public override string ToString() {
//      return Text;
//    }
//  }

//  public class IntranetEvent : LMEvent {

//    public IntranetEvent(string message, int batchId)
//      : base(message + (batchId == 0 ? null : " ( číslo " + batchId.ToString() + ")"), null, 0, batchId) { }
//    public IntranetEvent(string message)
//      : this(message, 0) { }
//  }

//  public class Logging {
//    static TraceSwitch[] switches;
//    static TraceSwitch all;
//    static Logging() {
//      Array cats = Enum.GetValues(typeof(TraceCategory));
//      switches = new TraceSwitch[cats.Length];
//      foreach (int i in cats)
//        switches[i] = new TraceSwitch(((TraceCategory)i).ToString(), "");
//      all = switches[(int)TraceCategory.All];
//    }
//    public static void Trace(TraceLevel level, TraceCategory category, string message) {
//      TraceSwitch sw = switches[(int)category];
//      if (sw.Level < level && (sw == all || all.Level < level)) return;
//      switch (level) {
//        case TraceLevel.Error: new ErrorEvent(level, category, message).Raise(); break;
//        case TraceLevel.Warning:
//        case TraceLevel.Info:
//        case TraceLevel.Verbose: new TraceEvent(level, category, message).Raise(); break;
//      }
//    }
//    public static void Trace(TraceLevel level, TraceCategory category, string fmt, params object[] pars) {
//      Trace(level, category, string.Format(fmt, pars));
//    }
//  }

//  public class LMMailEventProvider : WebEventProvider {
//    string[] mailsTo;
//    List<string> wrongFragments = new List<string>();

//    static LMMailEventProvider() {
//    }

//    public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config) {
//      base.Initialize(name, config);
//      mailsTo = config["to"].Split(',');
//    }
//    const string okAuthority = "www.langmaster.cz#www.langmaster.com#vyuka.lide.cz";
//    const string bmpExtensions = ".bmp#.jpg#.gif#.png";

//    public override void ProcessEvent(WebBaseEvent eventRaised) {
//      if (Machines.isBuildEACache_BuildCD_Crawler) return;
//      //Filter neskodnych URL chyb
//      string requestUrl = null;
//      if (eventRaised is WebRequestErrorEvent) {
//        WebRequestInformation inf = ((WebRequestErrorEvent)eventRaised).RequestInformation;
//        requestUrl = inf.RequestUrl.ToLower();
//        if (Filter.isMatch(HttpContext.Current, "urls", requestUrl)) return;
//        //foreach (string s in new string[] { "/dbimg.aspx?lang", "_vti_", "msoffice/cltreq.asp", "/news/images/bg01.gif", "_vpi.xml", "wp-rss"})
//        //if (inf.RequestUrl.ToLower().IndexOf(s) >= 0) return;
//      }
//      string err = null;
//      if (HttpContext.Current != null) {
//        HttpContext ctx = HttpContext.Current;
//        err = "** LM Information **\n--------------\n";
//        err += "Agent: " + ctx.Request.Headers["User-Agent"] + "\n";
//        Uri reff = HttpContext.Current.Request.UrlReferrer;
//        if (reff != null) {
//          //obrazky z ciziho referera neposilat
//          int dotPos = requestUrl.LastIndexOf('.');
//          if (requestUrl != null && dotPos > 0 && okAuthority.IndexOf(reff.Authority.ToLower()) < 0 && bmpExtensions.IndexOf(requestUrl.Substring(dotPos)) >= 0) return;
//          err += "Referrer: " + reff.AbsoluteUri + "\n";
//        }
//        err += "\n";
//        LMCookie cook = LMCookie.DeserializeCookie(ctx);
//        if (cook != null)
//          err += "UserId=" + cook.id.ToString() + "\n";
//      }
//      err += eventRaised.ToString();
//      Emailer em = new Emailer(null, "error@langmaster.cz", eventRaised.GetType().Name, err.Replace("\n", "<br/>"));
//      //if (err.ToLower().IndexOf("http://vyuka.lide.cz/webresource.axd") > 0) return;
//      //em.HTML = err.Replace("\n", "<br/>");
//      //em.Subject = eventRaised.GetType().Name;
//      //em.From = "error@langmaster.cz";
//      foreach (string mail in mailsTo) em.AddTo(mail);
//      em.SendMail();
//    }

//    public override void Shutdown() {
//    }

//    public override void Flush() {
//    }

//  }

//  //public class LMEventProviderx : BufferedWebEventProvider {
//  //  public override void ProcessEventFlush(WebEventBufferFlushInfo flushInfo) { }
//  //  public static void writeEvent(LMComDataContext ctx, WebBaseEvent ev) { }
//  //}
//  //public class LMEventProvider : BufferedWebEventProvider {
//  //  public override void Initialize(string name, System.Collections.Specialized.NameValueCollection config) {
//  //    base.Initialize(name, config);
//  //  }

//  //  public override void ProcessEvent(WebBaseEvent eventRaised) {
//  //    //if (eventRaised is DebugEvent && HttpContext.Current != null && HttpContext.Current.Request != null && HttpContext.Current.Request.UserHostAddress != "212.24.151.34") return;
//  //    if (Machines.isBuildEACache_BuildCD_Crawler) return;
//  //    if (UseBuffering && !(eventRaised is DebugEvent))
//  //      base.ProcessEvent(eventRaised);
//  //    else
//  //      writeEvents(new WebBaseEvent[] { eventRaised });
//  //  }

//  //  public override void ProcessEventFlush(WebEventBufferFlushInfo flushInfo) {
//  //    writeEvents(flushInfo.Events);
//  //  }

//  //  public override void Shutdown() {
//  //    Flush();
//  //  }

//  //  void writeEvents(System.Collections.IEnumerable events) { 
//  //    if (Machines.isBuildEACache_BuildCD_Crawler) return;
//  //    LMComDataContext ctx = Machines.getContext();
//  //    foreach (WebBaseEvent ev in events)
//  //      writeEvent(ctx, ev);
//  //    ctx.SubmitChanges();
//  //  }

//  //  public static void writeEvent(LMComDataContext ctx, WebBaseEvent ev) {
//  //    EventsLog log = new EventsLog();
//  //    ctx.EventsLogs.InsertOnSubmit(log);
//  //    if (ev is CommerceEvent)
//  //      log.Type = (short)EventCategory.Commerce;
//  //    else if (ev is MailEvent)
//  //      log.Type = (short)EventCategory.Mail;
//  //    else if (ev is ErrorEvent)
//  //      log.Type = (short)EventCategory.Error;
//  //    else if (ev is TraceEvent)
//  //      log.Type = (short)EventCategory.Trace;
//  //    else if (ev is WebBaseErrorEvent)
//  //      log.Type = (short)EventCategory.WebError;
//  //    else if (ev is IntranetEvent)
//  //      log.Type = (short)EventCategory.Intranet;
//  //    else if (ev is SeznamRpcEvent)
//  //      log.Type = (short)EventCategory.SeznamRPC;
//  //    else if (ev is DownloadEvent)
//  //      log.Type = (short)EventCategory.DownloadMP3;
//  //    //else if (ev is DownloadMoodleEvent)
//  //    //  log.Type = (short)EventCategory.DownloadMoodle;
//  //    else if (ev is DebugEvent)
//  //      log.Type = (short)EventCategory.debug;
//  //    //else if (ev is PayPalEvent)
//  //      //log.Type = (short)EventCategory.PayPal;
//  //    else
//  //      log.Type = (short)EventCategory.no;
//  //    log.UtcTime = ev.EventTimeUtc;
//  //    log.Code = ev.EventCode;
//  //    log.DetailCode = ev.EventDetailCode;
//  //    log.Message = ev.Message != null && ev.Message.Length > 1024 ? ev.Message.Substring(0, 1024) : ev.Message;
//  //    log.MachineName = System.Environment.MachineName;
//  //    string det = ev.ToString();
//  //    if (ev is LMEvent) {
//  //      LMEvent lme = (LMEvent)ev;
//  //      if (lme.info != null) {
//  //        log.App = (short)lme.info.AppId;
//  //        log.Site = (short)lme.info.SiteId;
//  //      }
//  //      if (lme.cook != null && lme.cook.id > 0)
//  //        log.UserId = lme.cook.id;
//  //      if (ev is CommerceEvent)
//  //        log.OrderId = ((CommerceEvent)ev).OrderId;
//  //      if (ev is MailEvent)
//  //        log.OrderId = ((MailEvent)ev).OrderId;
//  //      if (lme.RequestUrl != null)
//  //        log.RequestUrl = lme.RequestUrl!=null && lme.RequestUrl.Length>1024 ? lme.RequestUrl.Substring(0, 1024) : lme.RequestUrl;
//  //      if (ev is PayPalEvent)
//  //        log.OrderId = ((PayPalEvent)ev).OrderId;
//  //    } else {
//  //      LMCookie cook = LMStatus.CookieLow;
//  //      if (cook != null)
//  //        log.UserId = cook.id;
//  //      if (HttpContext.Current != null)
//  //        log.RequestUrl = HttpContext.Current.Request.Url.AbsolutePath;
//  //    }
//  //    log.Details = det;// != null && det.Length > 500 ? det.Substring(0, 500) : det;
//  //  }

//  //}

//}

