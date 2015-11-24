//using EcmaScript.NET;
using LMComLib;
using LMNetLib;
using Newtonsoft.Json;
using schools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
//using Yahoo.Yui.Compressor;

namespace Packager {

  public class ConfigLow : schools.config {

    //public string navBar() { return themeDefauleNavbar ? "default" : "inverse"; }

    //*** download
    public int expiresDays; //pocet dni expirace downloadu
    public int arm; //1..run armadilo
    public int install; //1..po vzbudovani setupu instaluj
    public bool dictLingeaTTS; //offline slovnik lingea s TTS zvukem
    //public bool withReports;

    //*** odvozene
    public int targetId { get { return (int)target; } set { } }
    public string langStr { get { return lang == null ? null : ((Langs)lang).ToString().Replace('_', '-'); } set { } }
    //public bool callInit { get { return target != Targets.sl; } set { } } //ma se do inicializacniho JS generovat volani Init
    public string loggerUrl; //url pro zaslani logu

    //*** funkce
    public schools.config copyTo(schools.config cfg) {
      base.copyTo(cfg);
      if (!(cfg is ConfigLow)) return cfg;
      ConfigLow cl = cfg as ConfigLow;
      cl.arm = arm;
      cl.install = install;
      cl.dictLingeaTTS = dictLingeaTTS;
      cl.loggerUrl = loggerUrl;
      cl.UseParentCfg = UseParentCfg;
      cl.expiresDays = expiresDays;
      return cfg;

    }
  }

  public class Config : ConfigLow {
    //*** products
    [XmlIgnore]
    public CourseMeta.product prod { get { return _prod == null ? _prod = RewApp.findProduct(productId) : _prod; } set { _prod = value; } } CourseMeta.product _prod;
    //public CourseMeta.productDescrLow prod { get { return _prod == null ? _prod = RewApp.findProduct(_productId) : _prod; } set { _prod = value; } } CourseMeta.productDescrLow _prod;
    //public string courseId { get { return _productId == null && _prod == null ? null : prod.ProductId; } set { } } //@PRODID
    public string productId; // { get { return _productId == null && _prod == null ? null : (_productId == null ? _prod.productId() : _productId); } set { _productId = value; } } string _productId;
    //public Langs[] WebLangs;
  }

  //public class BatchProduct {
  //  [XmlAttribute]
  //  public string email;
  //}

  public class BatchVersion {
    [XmlAttribute]
    public batchVersion version;
    public string forceServiceUrl; //natvrdo se nastavi service URL, napr. URL Scorm persistence pro skoda auto
    public string forceLoggerUrl; //log Url. Je=li "no", pak se log dava pouze pres clipboard
    public licenceConfig licenceConfig; //config licence
    public string LoggerEmails;
    public string ConnectionString;
    public string SSASConnectionString;
    public string basicPath;
    public string smtp;
    public void copyTo(schools.config cfg) {
      cfg.forceServiceUrl = forceServiceUrl;
      cfg.forceLoggerUrl = forceLoggerUrl;
      if (licenceConfig != null) cfg.licenceConfig = licenceConfig.copy();
      if (cfg.GetType() == typeof(WebSoftwareBatch)) {
        var webSwBatch = (WebSoftwareBatch)cfg;
        XElement root; XmlDocument doc;
        if (!webSwBatch.isScormExNet35) {
          root = new XElement("appSettings", new XElement("add", new XAttribute("key", "Logger.emails"), new XAttribute("value", LoggerEmails)));
          root.Add(new XElement("add", new XAttribute("key", "JSCramblerDebug"), new XAttribute("value", "?url?")));
          if (SSASConnectionString != null) root.Add(new XElement("add", new XAttribute("key", "SSAS.ConnectionString"), new XAttribute("value", SSASConnectionString)));
          switch (smtp) { //6.5.2015, http://bt.langmaster.cz/com/edit_bug.aspx?id=2141
            case "LANGMaster": smtp = "Chilkat.MailMan=SGORDICMAILQ_tPNaZl6U8K3D;Chilkat.Mht=SGORDICMHT_TQ85okoF8B4v;Email.SmtpHost=medea.inway.cz;Email.SmtpUsername=obchod@langmaster.cz;Email.SmtpPassword=lmobchodlm"; break;
            case "Chinh": smtp = "Chilkat.MailMan=SGORDICMAILQ_tPNaZl6U8K3D;Chilkat.Mht=SGORDICMHT_TQ85okoF8B4v;Email.SmtpHost=pop.edusoft.com.vn;Email.SmtpUsername=etestme@edusoft.com.vn;Email.SmtpPassword=langmaster2015"; break;
          }
          if (!string.IsNullOrEmpty(smtp))
            root.Add(smtp.Split(';').Select(nv => nv.Split('=')).Select(nv => new XElement("add", new XAttribute("key", nv[0]), new XAttribute("value", nv[1]))));
          if (!string.IsNullOrEmpty(basicPath))
            root.Add(new XElement("add", new XAttribute("key", "BasicPath"), new XAttribute("value", basicPath)));
          root.Add(new XElement("add", new XAttribute("key", "PDF.Cert.Password"), new XAttribute("value", "xcv456dfg258_")));
          root.Add(new XElement("add", new XAttribute("key", "PDF.Cert"), new XAttribute("value", @"{0}\rew\web4\App_Data\testMe\langmaster_pdf.pfx")));
          root.Add(new XElement("add", new XAttribute("key", "PDF.Font"), new XAttribute("value", @"{0}\rew\web4\App_Data\testMe\verdana.ttf")));
          root.Add(new XElement("add", new XAttribute("key", "PDF.FontBold"), new XAttribute("value", @"{0}\rew\web4\App_Data\testMe\verdanab.ttf")));
          doc = new XmlDocument(); doc.LoadXml(root.ToString());
          webSwBatch.configAppSettings = doc["appSettings"];
        }
        var parts = ConnectionString.Split(new string[] { "###" }, StringSplitOptions.None);
        root = new XElement("connectionStrings", new XElement("add", new XAttribute("name", "Container"), new XAttribute("connectionString", parts[0]), new XAttribute("providerName", parts[1])));
        doc = new XmlDocument(); doc.LoadXml(root.ToString());
        webSwBatch.configConnectionString = doc["connectionStrings"];
      }
    }
  }

  public enum batchVersion { no, release, fe5, localhost }

  [XmlInclude(typeof(ScormBatch))]
  [XmlInclude(typeof(WebSoftwareBatch))]
  public class BatchLow : ConfigLow {
    public batchVersion actBatchVersion;
    public BatchVersion[] batchVersions;
    protected void adjustVersion() {
      if (batchVersions != null) batchVersions.First(v => v.version == actBatchVersion).copyTo(this);
    }
    [XmlIgnore]
    public virtual object[] ItemsLow { get { return null; } set { } }
    //public Config[] Items;
    //public virtual string zipPrefix() { return null; }
    public virtual void adjustItems() { }
  }

  public class ScormBatchItem {
    public Config cfg;
    public CourseMeta.WebDataBatch webBatch;
    public CourseMeta.product prod;
    public LineIds line { get { return prod.line; } }
    public string title { get { return prod.title; } }
    public string prodUrl { get { return prod.url; } }
    public Langs lang;
    public string langStr { get { return lang.ToString().Replace('_', '-'); } }
    public CourseMeta.Cache cache;
  }

  public class ScormBatch : BatchLow {
    public CourseMeta.WebDataBatch data;
    public override void adjustItems() {
      adjustVersion();
      var cache = new CourseMeta.Cache(new LoggerMemory(false));
      Func<Langs, CourseMeta.product, Config> createCfg = (lng, prod) => {
        var res = new Config(); this.copyTo(res); res.lang = lng; res.prod = prod; return res;
      };
      var prodLoc = data.products.SelectMany(prod => data.locs.Select(loc =>
        new {
          prod, //single WebDataBatch products
          loc, //single WebDataBatch locs
          prodData = CourseMeta.Lib.prods.Items.Cast<CourseMeta.product>().First(p => p.url == prod.id) //produkt z d:\LMCom\rew\Web4\RowType\products.xml 
        }));
      Items = prodLoc.Select(pl => new ScormBatchItem {
        lang = pl.loc,
        prod = pl.prodData,
        cache = cache,
        webBatch = CourseMeta.WebDataBatch.FinishAfterLoad(new CourseMeta.WebDataBatch {
          dictType = data.dictType,
          products = new CourseMeta.BatchProduct[] { new CourseMeta.BatchProduct { dictType = pl.prod.dictType, id = pl.prod.id, locs = new Langs[] { pl.loc } } },
          url = data.url,
          locs = new Langs[] { pl.loc },
          //allLocs = data.allLocs,
          genDebugJS = data.genDebugJS
        }),
        cfg = createCfg(pl.loc, pl.prodData)
      }).ToArray();
    }
    [XmlIgnore]
    public ScormBatchItem[] Items { get { return _Items; } set { _Items = value; } } ScormBatchItem[] _Items;
    [XmlIgnore]
    public override object[] ItemsLow { get { return _Items; } set { _Items = (ScormBatchItem[])value; } }
  }

  public class WebSoftwareBatch : BatchLow {
    [XmlIgnore]
    public Config[] Items { get { return _Items; } set { _Items = value; } } Config[] _Items;
    [XmlIgnore]
    public override object[] ItemsLow { get { return _Items; } set { _Items = (Config[])value; } }
    [XmlAttribute]
    public Langs[] locs;
    public override void adjustItems() {
      adjustVersion();
      Func<CourseMeta.product, Langs, Config> createCfg = (prod, lang) => {
        Config res = new Config(); this.copyTo(res); res.prod = prod; res.lang = lang; return res;
      };
      if (locs != null) Items = locs.Select(l => createCfg(null, l)).ToArray();
    }
    public bool isScormExNet35; //build pouze 3 souboru: Web.ScormExNet35.config, ScormExNet35.ashx a bin\Debug\ScormExNet35.dll
    public bool isJSCramblerServer; //server slouzi jako zdroj pro protected JS a pro debug non protected JS
    public string Admins; //comma delimited seznam adminu, pro ktere se pri vytvareni DB udela ucet
    public XmlElement configConnectionString;
    public XmlElement configAppSettings;
  }

  public static class RewApp {

    public static string HomePage(Config cfg, string serverScript = "") {
      cfg.basicPath = Machines.basicPath;
      var res = new DesignTimeLib.Design.Templates.html() { cfg = cfg, serverScript = serverScript }.TransformText();
      return res;
    }

    public static string headContent(bool forStatistics, Config cfg) {
      return new DesignTimeLib.Design.Templates.htmlHead() { cfg = cfg, forStatistics = forStatistics }.TransformText();
    }

    //public static string headGroundContent(Config cfg) {
    //  return new NewData.Design.Templates.groundHead(){ cfg = cfg }.TransformText();
    //  //return new NewData.Design.Templates.htmlHead() { cfg = cfg }.TransformText();
    //}

    public static string writeJS(Config cfg, bool isGround = false) {
      StringBuilder sb = new StringBuilder();
      var isMin = cfg.version == versions.minified;
      if (!isGround && isMin && (cfg.licenceConfig == null || cfg.licenceConfig.serviceUrl == null)) throw new Exception("isMin && (cfg.licenceConfig == null || cfg.licenceConfig.serviceUrl == null)");
      foreach (var f in Consts.getFiles(Consts.pathType.relUrl, "schools", jsNewEA(cfg, isGround))) {
        if (isJQuery.IsMatch(f)) throw new Exception(); // continue; //JQueries jsou ve strance natvrdo
        var id = f.Split('.')[0];
        Action<string> writeVersion = ver => { if (isMin) sb.AppendFormat(jsTmpl, f, id, ver); else sb.AppendFormat(jsScript, f); };
        switch (id) {
          case "_rew_web":
          case "_rew_scorm": writeVersion(isMin ? "cfg.licenceConfig.rewVersion" : "0"); break;
          case "_course": writeVersion(isMin ? "cfg.licenceConfig.courseVersion" : "0"); break;
          case "_external": writeVersion(isMin ? "cfg.licenceConfig.extVersion" : "0"); break;
          case "_ground": writeVersion(isMin ? "cfg.licenceConfig.groundVersion" : "0"); break;
          default: sb.AppendFormat(isMin ? jsStaticTmpl : jsScript, f.StartsWith("../") ? f : "../schools/" + f); break;
        }
      }
      return sb.ToString();
    }
    const string jsTmpl = "\r\n  document.writeln(!isOk ? '' : \"<script type='text/javascript' src='\" + jsUrl(\"{0}\",\"{1}\",{2}) + \"'><\" + \"/script>\");";
    const string jsStaticTmpl = "\r\n  document.writeln(!isOk ? '' : \"<script type='text/javascript' src='{0}'><\" + \"/script>\");";
    const string jsScript = "\r\n  <script type='text/javascript' src='{0}'></script>";
    static Regex isJQuery = new Regex(@".*(jquery2|jquery|underscore)\.(min\.|)js$", RegexOptions.Multiline);

    //config 
    public static string writeCfg(Config ccfg) {
      //if (ccfg.UseParentCfg) return "<script type='text/javascript'>var cfg = window.parent.window['cfg']; cfg.startProcName = 'statistics.init';</script>\r\n";
      schools.config cfg = new schools.config();
      ccfg.copyTo(cfg);
      cfg.rootProductId = ccfg.prod == null ? cfg.rootProductId : ccfg.prod.url;
      string cfgStr = JsonConvert.SerializeObject(cfg);
      var cfgHtml = string.Format("<script type='text/javascript'>\r\nvar cfg = {0};\r\n</script>\r\n", cfgStr);
      return cfgHtml; // +bsHtml;
    }
    const string bsHtml = @"
<script type='text/javascript'>
  var bsUrl = '<link href=""../font-awesome/lm/externals' + (cfg.themeId ? cfg.themeId : '') + '.css"" rel=""stylesheet""/>';
  var lmUrl = '<link href=""../jslib/css/lm' + (cfg.themeId ? cfg.themeId : '') + '.css"" rel=""stylesheet""/>';
  document.write(bsUrl);
  document.write(lmUrl);
</script>
";

    public static CourseMeta.product findProduct(string prodUrl) {
      return prodUrl == null ? null : CourseMeta.Lib.prods.Items.Cast<CourseMeta.product>().First(p => string.Compare(p.url, prodUrl, true) == 0);
    }

    static IEnumerable<string> webLangFiles(ConfigLow cfg) { //Include JS souboru do manifestu, scorm zipu apod.
      var lng = cfg.lang == null ? "{#lang}" : (cfg.lang == Langs.sp_sp ? Langs.es_es : cfg.lang).ToString().Replace('_', '-');
      var htmlLang = cfg.lang == null ? "{#htmllang}" : CommonLib.htmlLang((Langs)cfg.lang);
      yield return string.Format("schools/loc/tradosdata.{0}.js", lng);
      yield return string.Format("jslib/scripts/cultures/globalize.culture.{0}.js", lng);
      if (cfg.lang == null)
        yield return string.Format("JsLib/Scripts/jqvalidator/localization/messages_{0}.js", htmlLang);
      else
        if (cfg.lang != Langs.en_gb) yield return string.Format("JsLib/Scripts/jqvalidator/localization/messages_{0}.js", htmlLang);
      //if (cfg.lang!=null && cfg.lang!=Langs.en_gb) yield return string.Format("JsLib/Scripts/jqvalidator/localization/messages_{0}.js", CommonLib.htmlLang((Langs)cfg.lang));
    }
    //Common files: CSS, JS, CSS bitmaps
    static IEnumerable<Consts.file> commonFiles(ConfigLow cfg) {
      foreach (var f in publisherSkinCss(cfg).Concat(jsNewEA(cfg, false)).Concat(jsNewEA(cfg, true)).Concat(imgFontsEtc(cfg)).SelectMany(s => s).Distinct().Select(fn => new Consts.file(fn)))
        yield return f;
      string min = cfg.version == versions.minified ? ".min" : null;

      //rucne pridavane soubory, jejichz seznam je v AddWebSoftwareFiles.txt
      foreach (var fn in File.ReadAllLines(@"D:\LMCom\REW\CubesDeployment\AddWebSoftwareFiles.txt")) yield return new Consts.file(fn);
      //yield return new Consts.file("Schools/SLExtension.xap");
      //yield return new Consts.file("jslib/js/lmconsoleinit.js");
      //yield return new Consts.file("jslib/ea/ea.css");
      //yield return new Consts.file("blendedapi/styles/style.less");

      yield return new Consts.file("jslib/scripts/jquery" + min + ".js");
      yield return new Consts.file("jslib/scripts/jquery2" + min + ".js");
      yield return new Consts.file("font-awesome/lm/externals" + cfg.themeId + ".css");
      yield return new Consts.file("jslib/css/lm" + cfg.themeId + ".css");
      //if (cfg.version == versions.minified) {
      //  yield return new Consts.file("jslib/js/sound/libmp3lame.js", File.ReadAllBytes(@"d:\LMCom\rew\Web4\Schools\_lame.charMin.js"));
      //  //yield return new Consts.file("jslib/js/sound/libmp3lame.js.gzip", File.ReadAllBytes(@"d:\LMCom\rew\Web4\Schools\_lame.charMin.js.gzip"));
      //} else
      //  yield return new Consts.file("jslib/js/sound/libmp3lame.js");
    }

    static void addFileToZip(ConfigLow cfg, ZipArchive zip, byte[] data, string destFn) {
      addFileToZip(cfg, zip, ms => ms.Write(data, 0, data.Length), destFn);
    }

    static void addFileToZip(ConfigLow cfg, ZipArchive zip, Action<Stream> fillData, string destFn, bool isGZip = true) {
      MemoryStream ms = new MemoryStream();
      destFn = cfg.normalizeDestFn(destFn);
      ms.SetLength(0); fillData(ms);
      var ext = Path.GetExtension(destFn);
      DateTime dt = DateTime.UtcNow;
      if (ext != ".dll") {
        //CRC je UINT. shift left = deleno 4, coz je maximalne 0x3fffffff
        var data = ms.ToArray();
        uint secs = (uint)ZipStream.Crc(data);
        secs = secs >> 2;
        dt = zipStartDate.AddSeconds(secs);
      }
      ms.Seek(0, SeekOrigin.Begin);
      lock (typeof(RewApp)) LMZipArchive.addFileToZip(zip, ms, destFn, dt); // zip.AddFileToZip(data, destFn, res2);
      if (!isGZip) return;
      switch (ext) {
        case ".txt":
        case ".lst":
        case ".json":
        case ".rjson":
          throw new Exception("addFileToZip: wrong extension - " + destFn);
        case ".js":
        case ".css":
        case ".html":
        case ".otf":
        case ".svg":
        case ".woff":
        case ".ttf":
        case ".eot":
          if (ms.Length < 100) return;
          MemoryStream gzipMs = new MemoryStream();
          ms.Position = 0; gzipMs.SetLength(0);
          using (GZipStream gzip = new GZipStream(gzipMs, CompressionMode.Compress, true)) ms.CopyTo(gzip);
          gzipMs.Seek(0, SeekOrigin.Begin);
          lock (typeof(RewApp)) LMZipArchive.addFileToZip(zip, gzipMs, destFn + ".gzip", dt); //zip.AddFileToZip(gzipMs.ToArray(), destFn + ".gzip", res2);
          break;
      }
    }

    public static void addFilesToZip(BatchLow cfg, IEnumerable<Consts.file> files, Stream str, Action<ZipArchive> zipData) {//, Action<ZipStream> zipData) {
      using (ZipArchive zip = new ZipArchive(str, ZipArchiveMode.Create)) {
        if (zipData != null) zipData(zip);
        Parallel.ForEach(files, new ParallelOptions() { MaxDegreeOfParallelism = 6 }, f => { //bez MaxDegreeOfParallelism dojde k OutOfMemory exception
          //Parallel.ForEach(files, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, f => { //bez MaxDegreeOfParallelism dojde k OutOfMemory exception
          var destFn = (/*cfg.zipPrefix() +*/ (f.destDir == null ? null : f.destDir + "\\") + f.name).ToLowerInvariant();
          addFileToZip(cfg, zip, f.copyTo, destFn, cfg is WebSoftwareBatch);
        });
      }
    }
    static DateTime zipStartDate = new DateTime(2014, 1, 12).AddSeconds(-(0xffffffff >> 2));

    static IEnumerable<Consts.file> uniqFiles(IEnumerable<Consts.file> files) {
      return files.Distinct(new ConstsFileComparer());
    }

    static void DownloadProduct(Config cfg) {
      File.WriteAllText(Machines.rootPath + @"rew\Downloads\Common\IIS\indexTemplate-download.htm", HomePage(cfg));
      var prodPathPrefix = issDestPath + "products\\" + cfg.prod.url.Replace('/', '-');
      // TODO Consts.saveISSFile(uniqFiles(productFiles(cfg).Concat(productSwFiles(cfg))), prodPathPrefix + "_files.iss");
    }

    static void ISSInclude(Config cfg) {
      //spolecne SW files
      Consts.saveISSFile(commonFiles(cfg), issDestPath + "globalFiles.iss");
      //produkt specific files (vcetne home)
      foreach (var prod in CourseMeta.Lib.prods.Items.Cast<CourseMeta.product>()) {
        cfg.prod = prod;
        DownloadProduct(cfg);
      }
    }
    static string issDestPath = Machines.basicPath + @"rew\Downloads\Common\IIS\";

    public static void runBatch(string cmdFile) {
      run(File.ReadAllLines(cmdFile));
    }

    public static void run(IEnumerable<string> lines) {
      var procInfo = new System.Diagnostics.ProcessStartInfo() {
        WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized,
        CreateNoWindow = true,
        FileName = @"c:\Windows\System32\cmd.exe",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardInput = true,
        RedirectStandardError = true,
        WorkingDirectory = @"c:\temp\",
        //Domain = "LANGMASTER",
        //UserName = "pavel",
        //Password = new System.Security.SecureString()
      };
      //string pas = "zvahov88_";
      //for (int i = 0; i < pas.Length; i++) { procInfo.Password.AppendChar(pas[i]); }

      using (var proc = System.Diagnostics.Process.Start(procInfo))
      using (var sOut = proc.StandardOutput)
      using (var sErr = proc.StandardError) {
        using (var sIn = proc.StandardInput) {
          foreach (var line in lines) sIn.WriteLine(line);
          sIn.WriteLine("EXIT");
        }
        var output = sOut.ReadToEnd();
        var err = sErr.ReadToEnd().Trim();
        if (!string.IsNullOrEmpty(err)) throw new Exception(err);
        output = null;
      }
    }

    public static IEnumerable<string> minifiedGroundJS() {
      yield return jsExternal;
      yield return jsGround;
    }

    public static IEnumerable<IEnumerable<string>> notMinifiedGroundJS() {
      yield return Consts.jsExternal;
      yield return Consts.jsGround;
    }

    public static IEnumerable<string> minifiedJS(ConfigLow cfg) {
      //yield return jsQuery; yield return jsQuery2;
      yield return jsExternal;
      yield return cfg.noOldEA ? jsOldEA : jsOldEARepl;
      yield return jsMinifiedTarget[cfg.target];
      if (cfg.licenceConfig == null || !cfg.licenceConfig.isDynamic) yield return jsCourse;
    }

    public static IEnumerable<IEnumerable<string>> notMinifiedJS(ConfigLow cfg) {
      //yield return Consts.jsQuery;
      //yield return Consts.jsQuery2;
      yield return Consts.jsExternal;
      yield return cfg.noOldEA ? Consts.jsEARepl : Consts.jsEA;
      yield return Consts.jsModel;
      if (cfg.target == Targets.scorm) yield return Consts.jsScorm;
      if (cfg.target == Targets.web) yield return Consts.jsLogin;
      if (cfg.target == Targets.web) yield return Consts.jsAdmin;
      yield return Consts.jsSchoolStart;
      if (cfg.licenceConfig == null || !cfg.licenceConfig.isDynamic) {
        yield return Consts.jsSchoolEnd;
        yield return Consts.jsCourse;
        yield return Consts.jsAuthorWeb;
        yield return Consts.jsBlended;
      }

    }

    const string jsExternal = "schools/_external";
    const string jsGround = "schools/_ground";
    const string jsOldEA = "schools/_ea";
    const string jsOldEARepl = "schools/_earepl";
    const string jsLame = "schools/_lame";
    public const string jsCourse = "schools/_course";
    static Dictionary<Targets, string> jsMinifiedTarget = new Dictionary<Targets, string>() {
      {Targets.web, "schools/_rew_web"},
      {Targets.scorm, "schools/_rew_scorm"},
      {Targets.download, "schools/_rew_download"}
    };
    static IEnumerable<string> protectedJS() { return jsMinifiedTarget.Values.Concat(XExtension.Create(jsCourse)); }

    static string basicPath = Machines.rootPath;

    static void writeFile(string fn, string cont) {
      fn = fn.Replace('/', '\\'); cont = cont.TrimEnd();
      if (File.Exists(fn) && File.ReadAllText(fn).TrimEnd() == cont) return;
      File.WriteAllText(fn, cont);
    }

    static IEnumerable<IEnumerable<string>> jsNewEA(ConfigLow cfg, bool isGround) {
      switch (cfg.version) {
        case versions.debug:
          foreach (var jss in isGround ? notMinifiedGroundJS() : notMinifiedJS(cfg)) yield return jss;
          break;
        case versions.not_minified:
          yield return (isGround ? minifiedGroundJS() : minifiedJS(cfg)).Select(m => m + ".js");
          break;
        case versions.minified:
          yield return (isGround ? minifiedGroundJS() : minifiedJS(cfg)).Select(m => m + ".min.js");
          break;
      }
      yield return webLangFiles(cfg);
      var skinJs = string.Format(Machines.rootPath + @"JsLib\skins\{0}\script.js", cfg.designId);
      if (File.Exists(skinJs)) yield return new string[] { string.Format(@"JsLib/skins/{0}/script.js", cfg.designId).ToLower() };
    }

    public static IEnumerable<IEnumerable<string>> publisherSkinCss(ConfigLow cfg) {
      var skinCss = string.Format(Machines.rootPath + @"JsLib\skins\{0}\css.css", cfg.designId);
      if (File.Exists(skinCss)) yield return new string[] { string.Format(@"JsLib/skins/{0}/css.css", cfg.designId).ToLower() };
    }

    public static IEnumerable<IEnumerable<string>> htmlNewEA(bool webTarget, string designId) {
      yield return Consts.htmlJsLib;
      if (webTarget) yield return Consts.htmlLogin;
      if (webTarget) yield return Consts.htmlAdmin;
      //if (cfg.target == Targets.web || cfg.target == Targets.author) yield return Consts.htmlAuthor;
      yield return Consts.htmlCourse;
      yield return Consts.htmlSchool;
      //yield return Consts.angularHtml;
      var skinHtml = string.Format(Machines.rootPath + @"JsLib\skins\{0}\html.html", designId);
      if (File.Exists(skinHtml)) yield return new string[] { string.Format(@"JsLib/skins/{0}/html.html", designId) };

    }
    public static IEnumerable<IEnumerable<string>> htmlNewEA(ConfigLow cfg) {
      return htmlNewEA(cfg.target==Targets.web, cfg.designId);
    }


    public static IEnumerable<IEnumerable<string>> imgFontsEtc(ConfigLow cfg) {
      yield return new string[] { @"JsLib\EA\img", @"JsLib\css\img", @"Schools\EAImgMp3\framework\controls\symbols", @"font-awesome\fonts\" }.SelectMany(pth => dir(pth, "*.*", true));
      //Skin PDF a IMG
      var skinDir = string.Format(Machines.rootPath + @"JsLib\skins\{0}\", cfg.designId);
      if (!string.IsNullOrEmpty(cfg.designId)) {
        if (Directory.Exists(skinDir)) yield return dir(string.Format(@"JsLib\skins\{0}", cfg.designId), "*.pdf", true);
        if (Directory.Exists(skinDir)) yield return dir(string.Format(@"JsLib\skins\{0}\img", cfg.designId), "*.*", true);
      }
      //default PDF
      skinDir = string.Format(Machines.rootPath + @"JsLib\skins\default\", cfg.designId);
      yield return dir(@"JsLib\skins\default", "*.pdf", true);
      //xslt
      yield return dir(@"app_data\Reports", "*.xlsx", true);
    }

    static IEnumerable<string> dir(string relDir, string filter, bool incSubDir) {
      var res = Directory.CreateDirectory(basicPath + relDir).
        EnumerateFiles(filter, incSubDir ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).
        Select(f => f.DirectoryName.Substring(basicPath.Length) + "\\" + f.Name).ToArray();
      return Directory.CreateDirectory(basicPath + relDir).
        EnumerateFiles(filter, incSubDir ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).
        Select(f => f.DirectoryName.Substring(basicPath.Length) + "\\" + f.Name);
    }


    static versions[] webVersions = new versions[] { versions.minified, versions.not_minified };

    static Regex imageReg = new Regex("src=\"(?<url>.*?)\"");
    static Regex wmaReg = new Regex(@", 'url':'(?<url>lm/oldea/data/.*?\.mp3)',");

    /********************** JSCRAMBLER *********************************/
    public static void jsCramblerAdjust(string cfgFn, batchVersion batchVersion = batchVersion.no) {
      BatchLow cfg = XmlUtils.FileToObject<Packager.BatchLow>(cfgFn);
      if (batchVersion != batchVersion.no) cfg.actBatchVersion = batchVersion;
      cfg.adjustItems();
      if (cfg.version != versions.minified) {
        //umozni delat non minified verzi SCORMu
        ReleaseDeploy.Lib.packSignatureBin(Machines.rootPath + @"signature.sign", null, null, null, null, Path.GetFileNameWithoutExtension(cfgFn).ToLower(), cfg);
      }

      if (cfg.licenceConfig == null ||
        (cfg.licenceConfig.isDynamic && string.IsNullOrEmpty(cfg.licenceConfig.serviceUrl)) ||
        (!cfg.licenceConfig.isDynamic && string.IsNullOrEmpty(cfg.licenceConfig.domain) && cfg.licenceConfig.intExpiration == 0))
        throw new Exception("cfg.licenceConfig == null || (!cfg.licenceConfig.isDynamic && (string.IsNullOrEmpty(cfg.licenceConfig.domain))) || (cfg.licenceConfig.isDynamic && (string.IsNullOrEmpty(cfg.licenceConfig.url)))");

      var rewFn = Machines.rootPath + jsMinifiedTarget[cfg.target].Replace('/', '\\') + ".js";
      var crsFn = Machines.rootPath + jsCourse.Replace('/', '\\') + ".js";
      var extFn = Machines.rootPath + @"Schools\_external.js";
      var groundFn = Machines.rootPath + @"Schools\_ground.js";
      var rewVersion = (uint)ZipStream.Crc(File.ReadAllBytes(rewFn));
      var crsVersion = (uint)ZipStream.Crc(File.ReadAllBytes(crsFn));
      var extVersion = (uint)ZipStream.Crc(File.ReadAllBytes(extFn));
      var groundVersion = (uint)ZipStream.Crc(File.ReadAllBytes(groundFn));

      //verze do configu
      var cfgXml = XElement.Load(cfgFn);
      foreach (var el in cfgXml.Descendants("licenceConfig")) {
        el.SetAttributeValue("rewVersion", rewVersion.ToString());
        el.SetAttributeValue("courseVersion", crsVersion.ToString());
        el.SetAttributeValue("extVersion", extVersion.ToString());
        el.SetAttributeValue("groundVersion", groundVersion.ToString());
      }
      cfgXml.Save(cfgFn);
      cfg.licenceConfig.rewVersion = rewVersion;
      cfg.licenceConfig.courseVersion = crsVersion;
      cfg.licenceConfig.extVersion = extVersion;
      cfg.licenceConfig.groundVersion = groundVersion;

      //signature.sign
      ReleaseDeploy.Lib.packSignatureBin(Machines.rootPath + @"signature.sign", rewFn, crsFn, extFn, groundFn, Path.GetFileNameWithoutExtension(cfgFn).ToLower(), cfg);

      if (cfg.version != versions.minified) return;

      if (!cfg.licenceConfig.isDynamic) { //JS a JS.charMin do Cache
        //protection COURSE soubor
        var crsCache = Machines.appData + @"JSCache\" + jsCourse.Replace("schools/", null) + "\\" + crsVersion + ".js";
        if (!File.Exists(crsCache)) File.WriteAllText(crsCache, File.ReadAllText(crsFn).Replace("debugger;", null));
        var crsMinCache = crsCache.Replace(".js", cfg.licenceConfig.fileSignature() + ".min.js");
        var crsMinFn = crsFn.Replace(".js", ".min.js");
        if (!File.Exists(crsMinCache)) JSCrambler.Lib.Protect(new JSCrambler.FileItemLow[] { new JSCrambler.FileItem(crsCache, crsMinCache) }, cfg.licenceConfig.domain, cfg.licenceConfig.expiration == DateTime.MinValue ? (DateTime?)null : cfg.licenceConfig.expiration.AddDays(1));
        File.Copy(crsMinCache, crsMinFn, true);
      }

      //protection GROUND soubor
      var groundCache = Machines.appData + @"JSCache\_ground\" + /*jsMinifiedTarget[cfg.target].Replace("schools/", null) + "\\" +*/ groundVersion + ".js";
      if (!File.Exists(groundCache)) File.WriteAllText(groundCache, File.ReadAllText(groundFn).Replace("debugger;", null));
      var groundMinFn = groundFn.Replace(".js", ".min.js");
      var groundMinCache = groundCache.Replace(".js", ".min.js");
      if (!File.Exists(groundMinCache)) JSCrambler.Lib.Protect(new JSCrambler.FileItemLow[] { new JSCrambler.FileItem(groundCache, groundMinCache) });
      File.Copy(groundMinCache, groundMinFn, true);

      //protection REW soubor
      var rewCache = Machines.appData + @"JSCache\" + jsMinifiedTarget[cfg.target].Replace("schools/", null) + "\\" + rewVersion + ".js";
      if (!File.Exists(rewCache)) File.WriteAllText(rewCache, File.ReadAllText(rewFn).Replace("debugger;", null));
      var rewMinFn = rewFn.Replace(".js", ".min.js");
      var rewMinCache = rewCache.Replace(".js", ".min.js");
      if (!File.Exists(rewMinCache)) JSCrambler.Lib.Protect(new JSCrambler.FileItemLow[] { new JSCrambler.FileItem(rewCache, rewMinCache) });
      File.Copy(rewMinCache, rewMinFn, true);
    }

    /********************** SCORM *********************************/
    public static void genScorms(string scormId, LoggerMemory log) {
      //minify(false, Targets.scorm);
      var batchFn = string.Format(Machines.basicPath + @"rew\Downloads\Common\batches\{0}.xml", scormId);
      jsCramblerAdjust(batchFn);
      ScormBatch batch = (ScormBatch)XmlUtils.FileToObject<BatchLow>(batchFn);
      batch.adjustItems();
      foreach (var cfg in batch.Items) {
        batch.lang = cfg.lang;
        var scormFn = Machines.basicPath + (batch.actBatchVersion == batchVersion.release ?
          string.Format(@"rew\Downloads\Scorms\{0}\{1}_{2}.zip", scormId, prodFileName.Replace(cfg.prodUrl, "$1").Replace('/', '_'), cfg.lang) :
          string.Format(@"rew\Downloads\Scorms\{0}_debug\{1}_{2}_{3}_{4}.zip", scormId, prodFileName.Replace(cfg.prodUrl, "$1").Replace('/', '_'), cfg.lang, batch.actBatchVersion, batch.persistType));
        if (File.Exists(scormFn)) File.Delete(scormFn);
        LowUtils.AdjustFileDir(scormFn);
        using (Stream file = File.OpenWrite(scormFn))
          addFilesToZip(batch, scormFiles(batch, cfg, true, log), file, zip => { });
      }
    }
    static Regex prodFileName = new Regex("^/(.*?)/$");

    //pro Manifest template v d:\LMCom\rew\NewLMComModel\Design\Templates\Templates.cs
    public static IEnumerable<string> scormFilesStr(ScormBatch batch, ScormBatchItem cfg, bool toZip, LoggerMemory log) {
      return Packager.RewApp.scormFiles(batch, cfg, toZip, log).Select(f => (f.destDir + "/" + batch.normalizeDestFn(f.name)).Replace('\\', '/'));
    }

    static IEnumerable<Consts.file> scormFiles(ScormBatch batch, ScormBatchItem cfg, bool toZip, LoggerMemory log) {
      IEnumerable<Consts.file> zipExtra = toZip ? XExtension.Create<Consts.file>(
        new Consts.file(@"schools\index.htm", Encoding.UTF8.GetBytes(HomePage(cfg.cfg))),
        new Consts.file(@"signature.sign"),
        new Consts.file(@"signature.sign"),
        new Consts.file("imsmanifest.xml", Encoding.UTF8.GetBytes(scormManifest(batch, cfg))),
        new Consts.file("siteroot.js", cfg.webBatch.products[0].siteRoot(cfg.prod))
        ) : Enumerable.Empty<Consts.file>();


      var a1 = cfg.webBatch.products[0].getBuildProduct().getFiles(cfg.cache, log);
      //var a1 = CourseMeta.buildLib.getProductFiles(cfg.cache, XExtension.Create<CourseMeta.buildProduct>(cfg.webBatch.products[0].getBuildProduct())).ToArray(); //cfg.webBatch.getWebBatchFiles(log).ToArray();
      var a2 = zipExtra;
      var a3 = commonFiles(cfg.cfg);
      //var a3 = productSwFiles(cfg.cfg).ToArray();
      var res = uniqFiles(a1.Concat(a2).Concat(a3));
      return res;
    }

    static string scormManifest(ScormBatch batch, ScormBatchItem cfg) {
      var xml = new DesignTimeLib.Design.Templates.ImsManifest() { cfg = cfg, batch = batch }.TransformText().Trim();
      var root = XElement.Parse(xml);
      using (var ms = new MemoryStream()) {
        using (var wr = XmlWriter.Create(ms, new XmlWriterSettings() { Encoding = Encoding.UTF8, Indent = true })) root.Save(wr);
        return Encoding.UTF8.GetString(ms.ToArray());
      }
    }

    /********************** WEB *********************************/
    static IEnumerable<Consts.file> webCodeFiles(WebSoftwareBatch batch, IEnumerable<Langs> langs, string webBatchId) {
      foreach (var fn in dir(@"bin", "*.dll", true)) yield return new Consts.file(fn);
      foreach (var fn in dir(@"bin", "*.pdb", true)) yield return new Consts.file(fn);
      foreach (var fn in dir(@"trados_globalresources", "rew_schoolcs*.resx", false)) yield return new Consts.file(fn);
      yield return new Consts.file("Global.asax");
      yield return new Consts.file("default.html");
      yield return new Consts.file("service.ashx");
      yield return new Consts.file("mp3Uploader.ashx");
      //etestme
      yield return new Consts.file("App_Data/testMe/verdanab.ttf");
      yield return new Consts.file("App_Data/testMe/verdana.ttf");
      yield return new Consts.file("App_Data/testMe/langmaster_pdf.pfx");
      //switch (batch.designId ?? "default") { 
      //  case "skrivanek":
      //    yield return new Consts.file("JsLib/skins/skrivanek/eTestMeCertifikat.pdf");
      //    break;
      //  case "default":
      //    yield return new Consts.file("JsLib/skins/default/eTestMeCertifikat.pdf");
      //    break;
      //}
      //yield return new Consts.file("ScormExNet35.ashx");
      //************* bez starych statistik
      //yield return new Consts.file(@"Schools\statistics.aspx");
      //yield return new Consts.file(@"Schools\statistics.aspx.cs");
      //yield return new Consts.file(@"statistics\default.ascx");
      //yield return new Consts.file(@"statistics\default.ascx.cs");
      //yield return new Consts.file(@"statistics\ground.master");
      //yield return new Consts.file(@"statistics\ground.master.cs");
      //yield return new Consts.file(@"statistics\toc.ascx");
      //yield return new Consts.file(@"statistics\toc.ascx.cs");
      //yield return new Consts.file(@"statistics\Statistics.js");
      yield return new Consts.file(@"JsLib\JS\Sound\wavWorker.js");

      //yield return new Consts.file(@"statistics\Statistics.js");
      //yield return new Consts.file(@"jslib\css\Statistics.css");
      //yield return new Consts.file(@"Schools\statistics.aspx.cs");
      //yield return new Consts.file(@"Schools\statistics.html");
      //yield return new Consts.file(@"Schools\statistics.js");
      //yield return new Consts.file(@"Schools\statistics.odc");
      yield return new Consts.file(@"Schools\SLExtension.xap");
      yield return new Consts.file("Web.config");
      yield return new Consts.file(@"signature.sign");
      //if (!batch.isDataOnly) yield return new Consts.file(@"app_data\stamp.txt", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("yyyy_MM_dd_hh_mm_ss")));
      yield return new Consts.file(@"app_data\stamp.js", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("yyyy_MM_dd_hh_mm_ss")));
      foreach (var cfg in batch.Items)
        yield return new Consts.file(
          string.Format(@"schools\index_{0}.html", cfg.lang),
          Encoding.UTF8.GetBytes(HomePage(cfg))
          );
    }

    public static void genWeb(string webBatchId) {
      var batchFn = string.Format(Machines.basicPath + @"rew\Downloads\Common\batches\webs\{0}.xml", webBatchId);
      //try {
      WebSoftwareBatch batch = (WebSoftwareBatch)XmlUtils.FileToObject<BatchLow>(batchFn);
      //if (batch.isDataOnly) {
      //  batch.adjustItems();
      //  var email = webBatchId + (string.IsNullOrEmpty(batch.actBatchVersion) || batch.actBatchVersion == "release" ? null : "_" + batch.actBatchVersion);
      //  string zipFn = Machines.basicPath + @"rew\Downloads\webs\" + email + ".zip";
      //  genWebLow(batch, webBatchId, batchFn, zipFn);
      //} else {
      //if (!batch.isScormExNet35) minify(false, Targets.web);
      foreach (var v in batch.batchVersions.Select(ver => ver.version)) {
        batch.Items = null; batch.actBatchVersion = v;
        var id = webBatchId + (v == batchVersion.release ? null : "_" + v);
        string zipFn = Machines.basicPath + @"rew\Downloads\webs\" + id + ".zip";
        batch.adjustItems();
        if (batch.isScormExNet35)
          genScormExNet35(batch, zipFn);
        else {
          jsCramblerAdjust(batchFn, v);
          genWebLow(batch, webBatchId, batchFn, zipFn);
        }
      }
      //} catch {
      //  string zipFn = Machines.basicPath + @"ReleaseDeploy\packs\" + webBatchId + ".zip";
      //  CourseMeta.buildLib.zipVirtualFiles(zipFn, CourseMeta.WebBatch.Load(@"d:\LMCom\rew\Downloads\Common\batches\webs\" + webBatchId + ".xml").getWebBatchFiles());
      //}
      //}
    }

    public static void genWebs(params string[] webBatchIds) {
      //minify(false, Targets.web);
      foreach (var webBatchId in webBatchIds) {
        var batchFn = string.Format(Machines.basicPath + @"rew\Downloads\Common\batches\webs\{0}.xml", webBatchId);
        jsCramblerAdjust(batchFn);
        WebSoftwareBatch batch = (WebSoftwareBatch)XmlUtils.FileToObject<BatchLow>(batchFn);
        batch.adjustItems(); //je zaroven adjutsVersion
        var id = webBatchId + (batch.actBatchVersion == batchVersion.no || batch.actBatchVersion == batchVersion.release ? null : "_" + batch.actBatchVersion.ToString());
        string zipFn = Machines.basicPath + @"rew\Downloads\webs\" + id + ".zip";
        genWebLow(batch, webBatchId, batchFn, zipFn);
      }
    }

    static void genScormExNet35(WebSoftwareBatch batch, string zipFn) {
      using (Stream str = File.OpenWrite(zipFn)) {
        using (ZipArchive zip = new ZipArchive(str, ZipArchiveMode.Create)) {
          var connStr = XElement.Parse(batch.configConnectionString.OuterXml);
          connStr.Element("add").Attribute("name").Value = "ScormExNet35Container";
          XElement config = new XElement("configuration", connStr,
            new XElement("system.web", new XElement("customErrors", new XAttribute("mode", "Off")), new XElement("compilation", new XAttribute("debug", "true"))));
          addFileToZip(batch, zip, Encoding.UTF8.GetBytes(config.ToString()), "web.config");
          addFileToZip(batch, zip, File.ReadAllBytes(basicPath + "ScormExNet35.ashx"), "ScormExNet35.ashx");
          addFileToZip(batch, zip, File.ReadAllBytes(basicPath + @"bin\ScormExNet35.dll"), @"bin\scormexnet35.dll");
          addFileToZip(batch, zip, new byte[0], @"app_data\newdatanet35.log");
          addFileToZip(batch, zip, File.ReadAllBytes(Machines.basicPath + @"rew\NewLMComModel\Data\NewData.ScormExecs.sql"), @"script.sql");

        }
      }
    }

    static void genWebLow(WebSoftwareBatch batch, string webBatchId, string batchFn, string zipFn) {
      //var f1 = batch.Items.SelectMany(c => productFiles(c)).Select(f => f.name).DefaultIfEmpty().Aggregate((r, i) => r + "\r\n" + i);
      //var f2 = batch.Items.SelectMany(c => productSwFiles(c)).Select(f => f.srcPath).DefaultIfEmpty().Aggregate((r, i) => r + "\r\n" + i);
      //var f3 = webCodeFiles(batch, batch.locs, webBatchId).Select(f => f.srcPath).DefaultIfEmpty().Aggregate((r, i) => r + "\r\n" + i);
      //var f4 = commonFiles(batch).Select(f => f.srcPath).DefaultIfEmpty().Aggregate((r, i) => r + "\r\n" + i);
      if (File.Exists(zipFn)) File.Delete(zipFn);
      //var files = batch.isDataOnly ?
      //  batch.Items.SelectMany(c => productFiles(c)) :
      //  batch.Items.SelectMany(c => productFiles(c)).
      //    Concat(productSwFiles(null)).
      var files = commonFiles(batch).
          Concat(webCodeFiles(batch, batch.locs, webBatchId));
      files = uniqFiles(files);
      //var f5 = files.Select(f => f.srcPath).DefaultIfEmpty().Aggregate((r, i) => r + "\r\n" + i);
      files = files.SelectMany(f => modifyLangs(batch, f));
      //var f6 = files.Select(f => f.srcPath).DefaultIfEmpty().Aggregate((r, i) => r + "\r\n" + i);
      using (Stream file = File.OpenWrite(zipFn)) {
        addFilesToZip(batch, files.Where(f => f.name != "courses.json" || f.destDir != @"schools\eacourses"), file, zip => {
          //if (!batch.isDataOnly) {
          addFileToZip(batch, zip, Encoding.UTF8.GetBytes(batch.configAppSettings == null ? null : batch.configAppSettings.OuterXml), /*batch.zipPrefix() +*/ "Web.AppSetting.config");
          addFileToZip(batch, zip, Encoding.UTF8.GetBytes(batch.configConnectionString == null ? null : batch.configConnectionString.OuterXml), /*batch.zipPrefix() +*/ "Web.ConnectionStrings.config");



          //addFileToZip(batch, zip, Encoding.UTF8.GetBytes("{}"), /*batch.zipPrefix() +*/ @"schools\eacourses\courses.json");

          //if (batch.isWebDeploy) {
          //  var d = basicPath + @"WebDeploy\data";
          //  foreach (var fn in Directory.EnumerateFiles(d, "*.*", SearchOption.AllDirectories))
          //    addFileToZip(batch, zip, File.ReadAllBytes(fn), fn.Substring(d.Length + 1));
          //}
          //}
        });
      }
    }
    public class ConstsFileComparer : IEqualityComparer<Consts.file> { public bool Equals(Consts.file x, Consts.file y) { return x.srcPath.Equals(y.srcPath); } public int GetHashCode(Consts.file obj) { return obj.srcPath.GetHashCode(); } }

    static IEnumerable<Consts.file> modifyLangs(WebSoftwareBatch batch, Consts.file f) {
      if (f.srcPath.IndexOf("{#lang}") < 0 && f.srcPath.IndexOf("{#htmllang}") < 0) yield return f;
      else foreach (var l in batch.locs) {
          string htmlLng = null;
          if (f.name == "messages_{#htmllang}.js") {
            switch (l) {
              case Langs.en_gb:
              case Langs.bs: continue;
              case Langs.zh_cn: htmlLng = "zh"; break;
              default: htmlLng = CommonLib.htmlLang(l); break;
            }
          }
          Langs lng = f.name != "instructions.json" && l == Langs.sp_sp ? Langs.es_es : l;
          string lstr = lng.ToString().Replace('_', '-');
          Func<string, string> replLang = str => str.Replace("{#lang}", lstr).Replace("{#htmllang}", htmlLng);
          yield return new Consts.file() {
            srcPath = replLang(f.srcPath),
            name = replLang(f.name),
            destDir = replLang(f.destDir)
          };
        }
    }

    /****************** BUILD **************************************/
    public static void BUILD(string id, Targets target, LoggerMemory log, BatchLow par) {
      //var parts = opId.ToLower().Split(':'); var email = parts[1];
      var batchFn = string.Format(Machines.basicPath + @"rew\Downloads\Common\batches\{0}{1}.xml", target == Targets.scorm ? null : @"webs\", id);
      XElement backup = null;
      try {
        XElement root = XElement.Load(batchFn); backup = XElement.Load(batchFn);
        if (par.actBatchVersion != batchVersion.no) root.SetElementValue("actBatchVersion", par.actBatchVersion.ToString());
        if (par.version != versions.no) root.SetElementValue("version", par.version.ToString());
        if (par.persistType != persistTypes.no) root.SetElementValue("persistType", par.persistType.ToString());
        root.SetElementValue("testGroup_debug", par.testGroup_debug ? "true" : "false");
        root.SetElementValue("scorm_driver", par.scorm_driver.ToString());
        root.Save(batchFn);
        switch (target) {
          case Targets.web:
            Packager.RewApp.genWeb(id);
            if (par.actBatchVersion == batchVersion.release) ReleaseDeploy.Lib.Deploy(Targets.web, id);
            break;
          case Targets.scorm:
            Packager.RewApp.genScorms(id, log);
            if (par.actBatchVersion == batchVersion.release) ReleaseDeploy.Lib.Deploy(Targets.scorm, id);
            break;
        }
      } finally {
        backup.Save(batchFn);
      }
    }
  }

}

