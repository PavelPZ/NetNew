//using LMComLib;
//using LMNetLib;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Runtime.Serialization;
//using System.Runtime.Serialization.Json;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Web;
//using System.Xml.Linq;

//namespace Translator {

//  //static char[] delims = new char[] { ' ', '"', '’', '\'' };
//  //var bing = File.ReadAllLines(@"d:\LMCom\rew\Web4\RwDicts\LMDict\Bing\en-cs.txt").Select(w => w.Split('=')).ToDictionary(w => w[0].Trim().ToLower(), w => w[1]);
//  //var google = File.ReadAllLines(@"d:\LMCom\rew\Web4\RwDicts\LMDict\Google\en-cs.txt").Select(w => w.Split('=')).ToDictionary(w => w[0].Trim().ToLower(), w => w[1].Replace("&quot;", null).Replace("&#39;", "’"));
//  //var allKeys = bing.Keys.Concat(google.Keys).Distinct();
//  //Dictionary<string, HashSet<string>> res2 = new Dictionary<string, HashSet<string>>();
//  //foreach (var k in allKeys) {
//  //  string s1 = null; string s2 = null; if (bing.TryGetValue(k, out s1)) s1 = s1.Trim(delims); if (google.TryGetValue(k, out s2)) s2 = s2.Trim(delims);
//  //  if (s1 == string.Empty || string.Compare(s1, k.Trim(delims), true) == 0) s1 = null; if (s2 == string.Empty || string.Compare(s2, k.Trim(delims), true) == 0) s2 = null;
//  //  if (s1 == null && s1 == s2) continue;
//  //  if (s1 != null && s2 != null && char.ToLower(s1[0]) == char.ToLower(s2[0])) s2 = null;
//  //  var r = new HashSet<string>();
//  //  if (s1 != null) r.Add(s1);
//  //  if (s2 != null) r.Add(s2);
//  //  res2[k] = r;
//  //}
//  //File.WriteAllLines(@"d:\LMCom\rew\Web4\RwDicts\LMDict\en-cs.txt", res2.OrderBy(kv => kv.Key).Select(kv => kv.Key + "=" + kv.Value.Aggregate((r, i) => r + "," + i)));

//  //Translator.ITranslate google = new Translator.Google();
//  //Translator.ITranslate bing = new Translator.Google();
//  //var src = new string[] { "koně" };
//  //var gtrans = google.Translate("cs", "en", new string[] { "koně", "skola" });
//  //var gres = src.Zip(gtrans, (s, t) => s + "=" + t).Aggregate((r, i) => r + "\r\n" + i);
//  //var btrans = google.Translate("cs", "en", new string[] { "koně", "skola" });
//  //var bres = src.Zip(btrans, (s, t) => s + "=" + t).Aggregate((r, i) => r + "\r\n" + i);
//  //bres = null;

//  //Translator.ITranslate bing = new Translator.Bing();
//  //Translator.ITranslate google = new Translator.Google();
//  //var words = CourseDictionary.wordsForCourse(XmlUtils.FileToObject<schools.DictCrsWords>(@"d:\LMCom\rew\Web4\RwDicts\CourseWords_en_gb.xml")).Select(w => w.word).Distinct().ToArray();
//  //var batch = 0;
//  //foreach (var chunk in words.Chunk(500))
//  //  File.WriteAllLines(string.Format(@"d:\temp\google-en-cs-{0}.txt", batch++), google.Translate("en", "cs", chunk, (s, t) => s + "=" + t));

//  public enum TranslatorTypes { google, bing }

//  public abstract class TranslateLow {

//    public static void CallTranslator(Translator.TranslatorTypes type, Langs courseLang, Langs nativeLang) {
//      var usedWords = LingeaDictionary.wordsForCourse(XmlUtils.FileToObject<schools.DictCrsWords>(string.Format(@"d:\LMCom\rew\Web4\RwDicts\UsedWords\CourseWords_{0}.xml", courseLang))).Select(w => w.word.ToLower().Trim()).Distinct().ToArray();
//      var translatedFn = string.Format(@"d:\LMCom\rew\Web4\RwDicts\Sources\{0}\{1}-{2}.txt", type, nativeLang, courseLang);
//      var translated = File.Exists(translatedFn) ? File.ReadAllLines(translatedFn).Where(l => !string.IsNullOrEmpty(l)).Select(l => l.Split('=')).ToDictionary(p => p[0].Trim().ToLower(), p => p[1].Trim()) : new Dictionary<string, string>();
//      var toTranslate = usedWords.Except(translated.Keys).ToArray();
//      Translator.TranslateLow trans = Translator.TranslateLow.Create(type);
//      try {
//        trans.BatchTranslate(courseLang, nativeLang, toTranslate, 20, 100, (src, tr) => translated.Add(src, tr));
//      } finally {
//        File.WriteAllLines(translatedFn, translated.Select(kv => kv.Key + "=" + kv.Value));
//      }
//    }

//    public static void CallTranslatorAll(Translator.TranslatorTypes type) {
//      Translator.TranslateLow trans = Translator.TranslateLow.Create(type);
//      foreach (var crs in DictLib.crsLangs.Intersect(trans.langs())) foreach (var nat in CommonLib.smallLocalizations.Intersect(trans.langs()))
//          if (crs != nat) CallTranslator(type, crs, nat);
//    }

//    public static TranslateLow Create(TranslatorTypes type) {
//      switch (type) {
//        case TranslatorTypes.bing: return new Bing();
//        case TranslatorTypes.google: return new Google();
//        default: throw new NotImplementedException();
//      }
//    }

//    protected abstract string translateSingle(string token, string from, string to, string text);
//    protected abstract IEnumerable<string> translateArray(string authToken, string from, string to, string[] texts);

//    public virtual string AuthToken() { return null; }
//    public abstract Langs[] langs();
//    public IEnumerable<string> Translate(string token, Langs from, Langs to, IEnumerable<string> texts) {
//      foreach (var text in texts) yield return translateSingle(token, getLang(from), getLang(to), text);
//    }
//    public IEnumerable<T> Translate<T>(string token, Langs from, Langs to, IEnumerable<string> texts, Func<string, string, T> transform) {
//      foreach (var text in texts) yield return transform(text, translateSingle(token, getLang(from), getLang(to), text));
//    }
//    public void BatchTranslate(Langs from, Langs to, IEnumerable<string> texts, int chunkLen, int tokenLen, Action<string, string> saveResult) {
//      foreach (var tokenCh in texts.Chunk(chunkLen * tokenLen)) {
//        var token = AuthToken();
//        Parallel.ForEach(tokenCh.Chunk(chunkLen), /*new ParallelOptions() { MaxDegreeOfParallelism=1 },*/ batchCh => {
//          var src = batchCh.ToArray();
//          var trans = translateArray(token, getLang(from), getLang(to), batchCh.ToArray()).ToArray();
//          lock (saveResult) if (src.Zip(trans, (s, t) => { saveResult(s, t); return true; }).Count() == 0) return;
//        });
//      }
//    }
//    string getLang(Langs l) {
//      intLangs();
//      return codeToLang[l];
//    }
//    protected void intLangs() {
//      if (codeToLang == null) lock (GetType()) if (codeToLang == null) {
//            codeToLang = langs().ToDictionary(s => s, s => CommonLib.htmlLang(s));
//            decodeToLang = codeToLang.ToDictionary(c => c.Value, c => c.Key);
//          }
//    }
//    protected Dictionary<Langs, string> codeToLang; protected Dictionary<string, Langs> decodeToLang;
//  }
//  //https://console.developers.google.com/project/405149658531/apiui/api/translate
//  //https://developers.google.com/translate/v2/using_rest
//  public class Google : TranslateLow {
//    static string key = HttpUtility.UrlEncode("AIzaSyB_GkY60HSddZJwQa-gaoIZK61QDkdfQH0");
//    protected override string translateSingle(string token, string from, string to, string text) {
//      return translateArray(token, from, to, new string[] { text }).First();
//      //var cl = new WebClient();
//      //cl.Encoding = Encoding.UTF8;
//      //var resp = cl.DownloadString(string.Format("https://www.googleapis.com/language/translate/v2?key={0}&source={1}&target={2}&q={3}", key, from, to, HttpUtility.UrlEncode(text)));
//      //dynamic cell = Newtonsoft.Json.JsonConvert.DeserializeObject(resp);
//      //return cell.data.translations[0].translatedText.ToString();
//    }
//    protected override IEnumerable<string> translateArray(string authToken, string from, string to, string[] texts) {
//      var cl = new WebClient();
//      cl.Encoding = Encoding.UTF8;
//      var url = string.Format("https://www.googleapis.com/language/translate/v2?key={0}&source={1}&target={2}", key, from, to) + texts.Select(t => "&q=" + HttpUtility.UrlEncode(t)).Aggregate((r, i) => r + i);
//      var resp = cl.DownloadString(url);
//      dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(resp);
//      var transs = (IEnumerable<dynamic>)obj.data.translations;
//      return transs.Select(t => t.translatedText.ToString()).Cast<string>();
//      //return transs.Cast<string>();
//      //return cell.data.translations[0].translatedText.ToString();
//    }
//    static Langs[] _langs = new Langs[] { Langs.af_za, Langs.ar_sa, Langs.az_latn_az, Langs.be_by, Langs.bg_bg, Langs.bn_in, Langs.ca_es, Langs.cs_cz, Langs.cy_gb, Langs.da_dk, Langs.de_de, Langs.el_gr, Langs.en_gb, Langs.et_ee, Langs.eu_es, Langs.fa_ir, Langs.fi_fi, Langs.fr_fr, Langs.ga_ie, Langs.gl_es, Langs.gu_in, Langs.he_il, Langs.hi_in, Langs.hr_hr, Langs.hu_hu, Langs.id_id, Langs.is_is, Langs.it_it, Langs.ja_jp, Langs.ka_ge, Langs.kn_in, Langs.ko_kr, Langs.lt_lt, Langs.lv_lv, Langs.mk_mk, Langs.ms_my, Langs.mt_mt, Langs.nl_nl, Langs.nb_no, Langs.pl_pl, Langs.pt_pt, Langs.ro_ro, Langs.ru_ru, Langs.sk_sk, Langs.sl_si, Langs.sp_sp, Langs.sq_al, Langs.sr_latn_cs, Langs.sv_se, Langs.sw_ke, Langs.ta_in, Langs.te_in, Langs.th_th, Langs.tr_tr, Langs.uk_ua, Langs.ur_pk, Langs.vi_vn, Langs.zh_cn };
//    public override Langs[] langs() { return _langs; }

//    public class DetectRes {
//      public Langs language;
//      public double confidence;
//    }

//    public IEnumerable<DetectRes> detect(string data) {
//      Func<string, Langs> getLang = s => { try { return decodeToLang[s]; } catch { return Langs.no; } };
//      intLangs();
//      var cl = new WebClient();
//      cl.Encoding = Encoding.UTF8;
//      var url = string.Format("https://www.googleapis.com/language/translate/v2/detect?key={0}&q={1}", key, HttpUtility.UrlEncode(data));
//      string resp = cl.DownloadString(url);
//      dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(resp);
//      var transs = (IEnumerable<dynamic>)obj.data.detections;
//      var test = transs.First();
//      return transs.SelectMany(t => (IEnumerable<dynamic>)t).Select(t => new DetectRes {
//        language = getLang(t.language.ToString()),
//        confidence = double.Parse(t.confidence.ToString())
//      });
//    }
//  }


//  //Get Client compId and Client Secret from https://datamarket.azure.com/developer/applications/
//  //Refer obtaining AccessToken (http://msdn.microsoft.com/en-us/library/hh454950.aspx) 
//  public class Bing : TranslateLow {

//    static Langs[] _langs = new Langs[] { Langs.ar_sa, Langs.bg_bg, Langs.ca_es, Langs.cs_cz, Langs.cy_gb, Langs.da_dk, Langs.de_de, Langs.el_gr, Langs.en_gb, Langs.et_ee, Langs.fa_ir, Langs.fi_fi, Langs.fr_fr, Langs.hi_in, Langs.hu_hu, Langs.id_id, Langs.it_it, Langs.ja_jp, Langs.ko_kr, Langs.lt_lt, Langs.lv_lv, Langs.ms_my, Langs.mt_mt, Langs.nl_nl, Langs.nb_no, Langs.pl_pl, Langs.pt_pt, Langs.ro_ro, Langs.ru_ru, Langs.sk_sk, Langs.sl_si, Langs.sp_sp, Langs.sv_se, Langs.th_th, Langs.tr_tr, Langs.uk_ua, Langs.ur_pk, Langs.vi_vn };
//    public override Langs[] langs() { return _langs; }

//    public void Test() {
//      string res;
//      res = DetectMethod(AuthToken(), "koněm");
//      //res2 = DetectMethod(AuthToken(), "škola");
//      //res2 = TranslateMethod(AuthToken(), "cs", "en", "koněm");
//      res = translateSingle(AuthToken(), "cs", "en", "koně");
//      res = GetTranslationsMethod(AuthToken(), "en", "cs", "Where").ToString();
//      res = translateArray(AuthToken(), "en", "cs", new string[] { "Where", "job" }).Aggregate((r, i) => r + "," + i);
//      //res2 = TranslateMethod(AuthToken(), "en", "cs", "Fiona");
//      res = null;
//    }

//    public override string AuthToken() {
//      AdmAuthentication admAuth = new AdmAuthentication(clientId, secretKey);
//      AdmAccessToken admToken = admAuth.GetAccessToken();
//      return "Bearer " + admToken.access_token;
//    }

//    protected override string translateSingle(string token, string from, string to, string text) {
//      //public static string TranslateMethod(string authToken, string from, string to, string text) {
//      if (text.Length > 10000) throw new Exception("Limit exceeded");
//      return get(token, "Translate?text=" + System.Web.HttpUtility.UrlEncode(text) + "&from=" + from + "&to=" + to);
//    }

//    protected override IEnumerable<string> translateArray(string authToken, string from, string to, string[] texts) {
//      if (texts.Length > 2000 || texts.Select(t => t.Length).Sum() > 10000) throw new Exception("Limit exceeded");
//      XElement xmlBody = new XElement("TranslateArrayRequest",
//        new XElement("AppId"),
//        new XElement("From", from),
//        new XElement("Options",
//          new XElement(nswebServ + "Category"),
//          new XElement(nswebServ + "ContentType", "text/plain"),
//          new XElement(nswebServ + "ReservedFlags"),
//          new XElement(nswebServ + "State"),
//          new XElement(nswebServ + "Uri"),
//          new XElement(nswebServ + "User")
//        ),
//        new XElement("Texts", texts.Select(s => new XElement(nsArr + "string", s))),
//        new XElement("To", to)
//      );
//      var res = post(authToken,
//        "TranslateArray",
//        xmlBody
//      );
//      return res.Descendants(nswebServ + "TranslatedText").Select(el => el.Value);
//    }

//    public static IEnumerable<string> GetTranslationsArrayMethod(string authToken, string from, string to, string[] texts) {
//      if (texts.Length > 2000 || texts.Select(t => t.Length).Sum() > 10000) throw new Exception("Limit exceeded");
//      XElement xmlBody = new XElement("GetTranslationsArrayRequest",
//        new XElement("AppId"),
//        new XElement("From", from),
//        new XElement("Options",
//          new XElement(nswebServ + "Category"),
//          new XElement(nswebServ + "ContentType", "text/plain"),
//          new XElement(nswebServ + "ReservedFlags"),
//          new XElement(nswebServ + "State"),
//          new XElement(nswebServ + "Uri"),
//          new XElement(nswebServ + "User")
//        ),
//        new XElement("Texts", texts.Select(s => new XElement(nsArr + "string", s))),
//        new XElement("To", to),
//        new XElement("MaxTranslations", "5")
//      );
//      var res = post(authToken,
//        "GetTranslationsArray",
//        xmlBody
//      );
//      return res.Elements().Select(el => el.Descendants(nswebServ + "TranslatedText").Select(e => e.Value).Aggregate((r, i) => r + "," + i));
//    }

//    public static string GetTranslationsMethod(string authToken, string from, string to, string text) {
//      var res = post(authToken,
//        "GetTranslations?text=" + System.Web.HttpUtility.UrlEncode(text) + "&from=" + from + "&to=" + to + "&maxTranslations=5",
//        new XElement(nswebServ + "TranslateOptions",
//          new XElement(nswebServ + "Category", "general"),
//          new XElement(nswebServ + "ContentType", "text/plain")
//        )
//      );
//      return res.Descendants(nswebServ + "TranslatedText").Select(el => el.Value).Aggregate((r, i) => r + "," + i);
//    }

//    public static string DetectMethod(string authToken, string text) {
//      return get(authToken, "Detect?text=" + System.Web.HttpUtility.UrlEncode(text));
//    }

//    static XNamespace nsArr = "http://schemas.microsoft.com/2003/10/Serialization/Arrays";
//    static XNamespace nswebServ = "http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2";

//    const string clientId = "langmaster_trans_test";
//    const string secretKey = "7GQGq+a5FPcKczHkFJd3o6g+2drHKNo4f2hTsMVPvlg=";
//    const string transUrl = "http://api.microsofttranslator.com/v2/Http.svc/";

//    public static string get(string authToken, string uri) {
//      //Keep appId parameter blank as we are sending access token in authorization header.
//      uri = transUrl + uri;
//      HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
//      httpWebRequest.Headers.Add("Authorization", authToken);
//      using (var response = httpWebRequest.GetResponse())
//      using (Stream stream = response.GetResponseStream()) {
//        DataContractSerializer dcs = new DataContractSerializer(Type.GetType("System.String"));
//        return (string)dcs.ReadObject(stream);
//      }
//    }

//    static XElement post(string authToken, string uri, XElement body) {
//      uri = transUrl + uri;
//      HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
//      httpWebRequest.Headers.Add("Authorization", authToken);
//      httpWebRequest.ContentType = "text/xml";
//      httpWebRequest.Method = "POST";
//      using (System.IO.Stream stream = httpWebRequest.GetRequestStream()) {
//        byte[] arrBytes = Encoding.UTF8.GetBytes(body.ToString());
//        stream.Write(arrBytes, 0, arrBytes.Length);
//      }
//      using (var response = httpWebRequest.GetResponse())
//      using (Stream stream = response.GetResponseStream())
//      using (StreamReader rdr = new StreamReader(stream))
//        return XElement.Parse(rdr.ReadToEnd());
//    }

//    static void ProcessWebException(WebException e) {
//      Console.WriteLine("{0}", e.ToString());
//      // Obtain detailed error information
//      string strResponse = string.Empty;
//      using (HttpWebResponse response = (HttpWebResponse)e.Response) {
//        using (Stream responseStream = response.GetResponseStream()) {
//          using (StreamReader sr = new StreamReader(responseStream, System.Text.Encoding.ASCII)) {
//            strResponse = sr.ReadToEnd();
//          }
//        }
//      }
//      Console.WriteLine("Http status code={0}, error message={1}", e.Status, strResponse);
//    }
//  }
//  [DataContract]
//  public class AdmAccessToken {
//    [DataMember]
//    public string access_token { get; set; }
//    [DataMember]
//    public string token_type { get; set; }
//    [DataMember]
//    public string expires_in { get; set; }
//    [DataMember]
//    public string scope { get; set; }
//  }

//  public class AdmAuthentication {
//    public static readonly string DatamarketAccessUri = "https://datamarket.accesscontrol.windows.net/v2/OAuth2-13";
//    private string clientId;
//    private string clientSecret;
//    private string request;
//    private AdmAccessToken token;
//    private Timer accessTokenRenewer;

//    //Access token expires every 10 minutes. Renew it every 9 minutes only.
//    private const int RefreshTokenDuration = 9;

//    public AdmAuthentication(string clientId, string clientSecret) {
//      this.clientId = clientId;
//      this.clientSecret = clientSecret;
//      //If clientid or client secret has special characters, encode before sending request
//      this.request = string.Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=http://api.microsofttranslator.com", HttpUtility.UrlEncode(clientId), HttpUtility.UrlEncode(clientSecret));
//      this.token = HttpPost(DatamarketAccessUri, this.request);
//      //renew the token every specfied minutes
//      accessTokenRenewer = new Timer(new TimerCallback(OnTokenExpiredCallback), this, TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
//    }

//    public AdmAccessToken GetAccessToken() {
//      return this.token;
//    }


//    private void RenewAccessToken() {
//      AdmAccessToken newAccessToken = HttpPost(DatamarketAccessUri, this.request);
//      //swap the new token with old one
//      //Note: the swap is thread unsafe
//      this.token = newAccessToken;
//      Console.WriteLine(string.Format("Renewed token for user: {0} is: {1}", this.clientId, this.token.access_token));
//    }

//    private void OnTokenExpiredCallback(object stateInfo) {
//      try {
//        RenewAccessToken();
//      } catch (Exception ex) {
//        Console.WriteLine(string.Format("Failed renewing access token. Details: {0}", ex.Message));
//      } finally {
//        try {
//          accessTokenRenewer.Change(TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
//        } catch (Exception ex) {
//          Console.WriteLine(string.Format("Failed to reschedule the timer to renew access token. Details: {0}", ex.Message));
//        }
//      }
//    }


//    private AdmAccessToken HttpPost(string DatamarketAccessUri, string requestDetails) {
//      //Prepare OAuth request 
//      WebRequest webRequest = WebRequest.Create(DatamarketAccessUri);
//      webRequest.ContentType = "application/x-www-form-urlencoded";
//      webRequest.Method = "POST";
//      byte[] bytes = Encoding.ASCII.GetBytes(requestDetails);
//      webRequest.ContentLength = bytes.Length;
//      using (Stream outputStream = webRequest.GetRequestStream()) {
//        outputStream.Write(bytes, 0, bytes.Length);
//      }
//      using (WebResponse webResponse = webRequest.GetResponse()) {
//        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AdmAccessToken));
//        //Get deserialized object from JSON stream
//        AdmAccessToken token = (AdmAccessToken)serializer.ReadObject(webResponse.GetResponseStream());
//        return token;
//      }
//    }
//  }
//}

