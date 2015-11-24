using CourseMeta;
using LMComLib;
using LMNetLib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Yahoo.Yui.Compressor;

namespace DesignNew {

  public static class buildTasks {

    static buildTasks() {
      Cfg.init(@"d:\lmcom\rew\webcommon\wwwroot\servconfig.js");
    }

    //*********** AZURE_publish
    public static void AZURE_publish(
      string container, //napr. "js-v001" nebo "mm-v001"
      bool isJS, //JS soubory nebo multimedia soubory
      IEnumerable<BuildIds> buildIds, //napr. English,Skrivanek, ...
      IEnumerable<Langs> locs //lokalizace
      ) {
      runTask("AZURE_publish", () => {
        Trace.TraceWarning("container: {1}, isJS: {2}, buildIds: {3}, locs: {4}, connection: {0}",
          Cfg.cfg.azure.connectionString,
          container,
          isJS,
          buildIds.Join(),
          locs.Join()
          );
        var msg = SynchronizeDirs.synchronize(
          isJS,
          new azureDriver(Cfg.cfg.azure.connectionString, container),
          buildIds,
          locs
        );
        Trace.WriteLine("Synchronize info: " + msg);
        return null;
      });
    }

    //*********** AZURE_delete
    public static void AZURE_delete(string container/*napr. "js-v001" nebo "mm-v001"*/) {
      runTask("AZURE_delete", () => {
        new azureDriver(Cfg.cfg.azure.connectionString, container).deleteContainer();
        Trace.WriteLine("Container: " + container);
        return null;
      });
    }

    //*********** COURSE_build
    public static void COURSE_build(BuildIds buildId, IEnumerable<Langs> locs, dictTypes dictType = dictTypes.no) {
      runTask("COURSE_build", () => {
        Trace.TraceWarning("BuildId: {0}, locs: {1}, dictType: {2}", buildId, locs.Join(), dictType);
        LoggerMemory log = new LoggerMemory(true);
        try {
          CourseMeta.Lib.init(log, @"d:\lmcom\", false);
          if (!log.hasError) {
            //vytvoreni WebDataBatch, vse je pripraveno nahradit DATA-{0}.xml pouhymi seznamy produkt IDS
            var productIds = File.ReadAllLines(string.Format(@"D:\LMCom\rew\DeployGUI\Web4CourseProducts\{0}.txt", buildId));
            Trace.TraceWarning("Start build {0} products", productIds.Length);
            var batch = new WebDataBatch { dictType = dictType, locs = locs.ToArray(), products = productIds.Select(id => new BatchProduct { id = id }).ToArray() };
            WebDataBatch.FinishAfterLoad(batch);
            //seznam souboru, v product adresari jsou metainformace o produktech, v envelope jsou aktualizovane informace.
            var files = batch.getWebBatchFilesNew(buildId, log, @"d:\LMCom\rew\Web4\products\");
            buildLib.writeVirtualFiles(buildId, files);
          }
        } catch (Exception exp) {
          log.ErrorLine("Exception", LowUtils.ExceptionToString(exp));
        }
        return log.hasError ? log.Log() : null;
      });
    }

    //*********** COURSE_resetEnvelope
    public static void COURSE_resetEnvelope() {
      runTask("COURSE_resetEnvelope", () => { buildEnvelopes.reset(); return null; });
    }

    //*********** TYPESCRIPT_compile
    public static void TYPESCRIPT_compile(string tsconfig) {
      runTask("TYPESCRIPT_compile", () => {
        Trace.TraceWarning("tsconfig: {0}", tsconfig);
        string output;
        var error = LowUtils.runExecutable(ConfigurationManager.AppSettings["tsc.exe"], "--project \"" + tsconfig + "\"", out output);
        return output;
      });
    }

    //*********** SW_WEB4refresh
    public static void SW_web4() {
      runTask("SW_WEB4refresh", () => {
        //JS minify
        minifier.jsMinify("/deploy/web4/js-externals.dpl.json", "/deploy/web4/mins/externals.min.js");
        minifier.jsMinify("/deploy/web4/js-web.dpl.json", "/deploy/web4/mins/web.min.js");
        foreach (var lang in Consts.swLangs) minifier.jsMinify("/deploy/web4/js{loc}.dpl.json", string.Format("/deploy/web4/mins/{0}.min.js", FileSources.swLang(lang)), lang);
        //CSS minify
        minifier.cssInPlaceMinify("/deploy/web4/css.dpl.json");
        //index HTML parts minify
        var htmlFnMask = "/deploy/web4/index-parts/{0}.html";
        foreach (var brand in Consts.allBrands) {
          var res = Packager.MainPage.htmls(Packager.RewApp.htmlNewEA(true, brand == servConfig.Brands.lm.ToString() ? null : brand.ToString()));
          File.WriteAllText(FileSources.pathFromUrl(string.Format(htmlFnMask, brand)), res);
        };
        return null;
      });
    }

    //*********** SW_deploy
    public static void SW_web_and_web4() {
      runTask("SW_deploy", () => {
        //*** COMMON refresh
        //JS minify
        minifier.jsMinify("/deploy/oauth/js-all.dpl.json", "/deploy/oauth/mins/all.min.js");
        minifier.jsMinify("/deploy/web/js-externals.dpl.json", "/deploy/web/mins/externals.min.js");
        minifier.jsMinify("/deploy/web/js-common.dpl.json", "/deploy/web/mins/common.min.js");
        //*** ZIP
        var files = FileSources.getUrls(FileSources.zipSWFilesFilter(servConfig.Apps.web, servConfig.Apps.web4, servConfig.Apps.oauth)).ToArray();
        File.WriteAllLines(@"d:\temp\sw_deploy.txt", files);
        var len = FileSources.zipSWFiles(@"D:\LMCom\rew\WebCommon\swfiles.zip", files);
        Trace.TraceWarning("ZIP files: {0}, len: {1}", files.Length, len);
        return null;
      });
    }

    //*********** CODE
    public static void CODE() {
      runTask("CODE", () => {
        return null;
      });
    }

    //*********** TYPESCRIPT_fromCS_web
    public static void CS_to_typescrit_web() {
      runTask("TYPESCRIPT_fromCS_web", () => {
        StringBuilder sb = new StringBuilder();
        CSharpToTypeScript.GenerateStr(sb, new RegisterImpl("LMComLib", null,
          null,
          new Type[] { typeof(Langs) }
          ));
        CSharpToTypeScript.GenerateStr(sb, new RegisterImpl("servConfig", null,
          null,
          new Type[] { typeof(servConfig.oAuthProviders), typeof(servConfig.SkinIds), typeof(servConfig.Brands), typeof(servConfig.Apps)},
          typeof(servConfig.Root), typeof(servConfig.Azure), typeof(servConfig.ViewPars),
          typeof(servConfig.oAuthConfig), typeof(servConfig.oAuthItem)));
        File.WriteAllText(@"D:\LMCom\rew\WebCommon\wwwroot\Common\CsShared.ts", sb.ToString(), Encoding.ASCII);
        return null;
      });
    }

    static void runTask(string taskName, Func<string> task) {
      Trace.WriteLine(taskName + " START");
      Trace.Indent();
      var error = task();
      if (error != null) {
        Trace.WriteLine(error);
        throw new Exception("See details in Output");
      }
      Trace.Unindent();
      Trace.WriteLine(taskName + " END");
    }

    static string minify(string basicPath, IEnumerable<string> srcFiles, bool isJs, string lng) {
      StringBuilder sb = new StringBuilder();
      Compressor compr = isJs ? (Compressor)new JavaScriptCompressor() : new CssCompressor();
      foreach (var fn in srcFiles.Select(f => basicPath + string.Format(f, lng).Replace('/', '\\'))) {
        var res = compr.Compress(File.ReadAllText(fn, Encoding.UTF8));
        sb.Append(res);
      }
      return sb.ToString();
    }

  }

}
//public class deployToAzure : Task {
//  public override bool Execute() {
//    Log.LogMessage(">>> deployToAzure START");
//    var msg = SynchronizeDirs.synchronize(
//      isJS,
//      new azureDriver(accountName, accountKey, container),
//      buildIds.Split(',').Select(bi => LowUtils.EnumParse<BuildIds>(bi)),
//      langs.Split(',').Select(bi => LowUtils.EnumParse<Langs>(bi))
//    );
//    Log.LogMessage(msg);
//    Log.LogMessage(">>> deployToAzure END");
//    return true;
//  }
//  [Required]
//  public bool isJS { get; set; } //true pro JS soubory, other pro multimedia soubory 
//  [Required]
//  public string accountName { get; set; } //napr. "lmdata"
//  [Required]
//  public string accountKey { get; set; } //napr. "Hx//uWeo6vDSA2BHbBJP7HZviSSE6D8qZhGV7f4G778yPcfGOiBODF6o7Cg6029JiqnpMm1U8KrlD3+hycYiEw=="
//  [Required]
//  public string langs { get; set; } //napr. "cs_cz,en_gb"
//  [Required]
//  public string buildIds { get; set; } //napr. "blended"
//  [Required]
//  public string container { get; set; } //napr. "js-v001" nebo "mm-v001"
//}

//HTML cast index.html do d:\LMCom\rew\WebCode\App_Data\html*.txt
//public class htmlFiles : Task {
//  public override bool Execute() {
//    Log.LogMessage(">>> htmlFiles START");
//    Log.LogMessage(htmlFile);
//    foreach (var designId in LowUtils.EnumGetValues<Consts.Brands>()) {
//      var res = Packager.MainPage.htmls(Packager.RewApp.htmlNewEA(true, designId == Consts.Brands.lm ? null : designId.ToString()));
//      File.WriteAllText(string.Format(htmlFile, designId), res);
//    }
//    Log.LogMessage(">>> htmlFiles END");
//    return true;
//  }
//  [Required]
//  public string htmlFile { get; set; }
//}

////SW files do d:\LMCom\rew\WebCode\App_Data\swfiles.zip
//public class zipSWFiles : Task {
//  public override bool Execute() {
//    Log.LogMessage(">>> zipSWFiles START");
//    Log.LogMessage(basicPath + " > " + zipFile);
//    Deploy.zipSWFiles(basicPath, zipFile, Deploy.allSWFiles);
//    Log.LogMessage(">>> zipSWFiles END");
//    return true;
//  }
//  [Required]
//  public string basicPath { get; set; }
//  [Required]
//  public string zipFile { get; set; }
//}

//minify JS a CSS
//public class _minify : Task {
//  public override bool Execute() {
//    Log.LogMessage(">>> minify START");
//    generate(Deploy.externals.SelectMany(f => f), "externals", true, null);
//    generate(Deploy.web.SelectMany(f => f), "web", true, null);
//    foreach (var lng in Deploy.validLangStrs) {
//      generate(Deploy.loc.SelectMany(f => f), lng, true, lng);
//    }
//    generate(Deploy.css, null, false, null);
//    Log.LogMessage(">>> minify END");
//    return true;
//  }
//  void generate(IEnumerable<string> srcFiles, string name, bool isJs, string lng) {
//    StringBuilder sb = new StringBuilder();
//    Compressor compr = isJs ? (Compressor)new JavaScriptCompressor() : new CssCompressor();
//    foreach (var fn in srcFiles.Select(f => basicPath + string.Format(f, lng).Replace('/', '\\'))) {
//      //Log.LogMessage(fn);
//      var res = compr.Compress(File.ReadAllText(fn, Encoding.UTF8));
//      sb.Append(res);
//    }
//    var minFn = isJs ? basicPath + @"deploy\" + name + ".min.js" : basicPath + @"jslib\css\lm.min.css";
//    Log.LogMessage("> " + minFn);
//    File.WriteAllText(minFn, sb.ToString());
//  }
//  [Required]
//  public string basicPath { get; set; }
//}

//public static void SW_oldBuild(string srcDir, string zipFile, Func<string, IEnumerable<string>> getFiles) {
//  runTask("SW_oldBuild", () => {
//    Trace.TraceWarning("srcDir: {0}, zipFile: {1}", srcDir, zipFile);

//    //HTML fragmenty pro generaci index.html
//    var htmlFnMask = "/deploy/web4/index-parts/{0}.html";
//    var stringEntries = LowUtils.EnumGetValues<Consts.Brands>().Select(designId => {
//      var res = Packager.MainPage.htmls(Packager.RewApp.htmlNewEA(true, designId == Consts.Brands.lm ? null : designId.ToString()));
//      return new Deploy.stringEntry { localPath = string.Format(htmlFnMask, designId), data = res };
//    });
//    var htmlFragments = Deploy.getEntries(stringEntries, true);
//    //SW soubory
//    var allFiles = Deploy.getEntries(srcDir, getFiles(srcDir));
//    //MIN soubory
//    var mins1 = XExtension.Create<Deploy.stringEntry>(
//      new Deploy.stringEntry { localPath = "", data = minify(srcDir, Deploy.externals.SelectMany(f => f), true, null) },
//      new Deploy.stringEntry { localPath = "", data = minify(srcDir, Deploy.web.SelectMany(f => f), true, null) },
//      new Deploy.stringEntry { localPath = "", data = minify(srcDir, Deploy.css, false, null) }
//    );
//    var mins2 = Deploy.validLangStrs.Select(lng =>
//      new Deploy.stringEntry { localPath = "", data = minify(srcDir, Deploy.loc.SelectMany(f => f), true, lng) }
//    );

//    //All to ZIP
//    Deploy.zipSWFiles(zipFile, htmlFragments.Concat(allFiles).Concat(Deploy.getEntries(mins1)).Concat(Deploy.getEntries(mins2)));
//    return null;
//  });
//}

//public static IEnumerable<zipEntry> getEntries(string srcDir, IEnumerable<string> files, bool noGZip = false) {
//  foreach (var fn in files) yield return new zipEntry { srcDir = srcDir, localPath = fn.Substring(srcDir.Length), noGZip = noGZip };
//}

//public static IEnumerable<zipEntry> getEntries(IEnumerable<stringEntry> getStringEntries, bool noGZip = false) {
//  foreach (var se in getStringEntries) yield return new zipEntry { localPath = se.localPath, data = Encoding.UTF8.GetBytes(se.data), noGZip = noGZip };
//}
//public struct stringEntry { public string localPath; public string data; }

//public class zipEntry {
//  public string localPath;
//  public bool noGZip;
//  public string srcDir;
//  public byte[] data; //=null => pak je obsah v srcDir + localPath

//  public byte[] getData() { return data != null ? data : File.ReadAllBytes(srcDir + localPath); }
//}


//jadro deploymentu WEB aplikace (vytvoreni napr. swfiles.zip)
//public static void zipSWFiles(string basicPath, string zipFile, Func<string, IEnumerable<string>> getFiles) {
//  if (File.Exists(zipFile)) File.Delete(zipFile);
//  using (var zipStr = File.OpenWrite(zipFile))
//  using (ZipArchive zip = new ZipArchive(zipStr, ZipArchiveMode.Create)) {
//    foreach (var fn in getFiles(basicPath).Select(f => f.Replace('/', '\\'))) {
//      ZipArchiveEntry entry = zip.CreateEntry(fn);
//      var inpFn = basicPath + fn;
//      using (var inpStr = File.OpenRead(inpFn))
//      using (var str = entry.Open()) inpStr.CopyTo(str);
//      if (gzipExtensions.Contains(Path.GetExtension(inpFn))) {
//        entry = zip.CreateEntry(fn + ".gzip", CompressionLevel.NoCompression);
//        using (var inpStr = File.OpenRead(inpFn)) {
//          if (inpStr.Length < 100) continue;
//          using (var str = entry.Open())
//          using (var gzipStr = new GZipStream(str, CompressionMode.Compress))
//            inpStr.CopyTo(gzipStr);
//        }
//      }
//    }
//  }
//}

////******************* soubory pro SW deployment
////******************* soubory pro generaci index.html
////seznam JS a CSS souboru (pro debug a minify mode)
//public static IEnumerable<string> allSWFiles(string basicPath) {
//  var JSs = validDesignIds.SelectMany(designId => validLangStrs.SelectMany(lang => new bool[] { true, false }.Select(isMin => new { designId, lang, isMin }))).SelectMany(slb => allJS(slb.isMin, slb.lang, slb.designId));
//  var CSSs = validDesignIds.SelectMany(designId => new bool[] { true, false }.Select(isMin => new { designId, isMin })).SelectMany(slb => allCSS(slb.isMin, slb.designId));
//  var other = File.ReadAllLines(basicPath + @"Deploy\otherServer.txt").Concat(File.ReadAllLines(basicPath + @"Deploy\otherClient.txt"));
//  return JSs.Concat(CSSs).Concat(other).Where(s => !string.IsNullOrEmpty(s)).Select(s => s.ToLower()).Distinct().OrderBy(s => s);
//}

//public static IEnumerable<string> allJS(bool isMin, string lang, string designId) {
//  var jss = isMin ? jsMins.Select(s => string.Format(s, lang)) : externals.SelectMany(s => s).Concat(web.SelectMany(s => s)).Concat(loc.SelectMany(s => s).Select(s => string.Format(s, lang)));
//  IEnumerable<string> skin = Enumerable.Empty<string>();
//  if (!string.IsNullOrEmpty(designId)) { string[] sk; if (jsSkins.TryGetValue(designId, out sk)) skin = sk; }
//  return jquery(isMin).Concat(jss).Concat(skin);
//}

//public static IEnumerable<string> allCSS(bool isMin, string designId) {
//  var cssList = isMin ? cssMins : css;
//  IEnumerable<string> skin = Enumerable.Empty<string>();
//  if (!string.IsNullOrEmpty(designId)) { string[] sk; if (cssSkins.TryGetValue(designId, out sk)) skin = sk; }
//  return cssList.Concat(skin);
//}

//public static partial class Deployx {

//  public static HashSet<string> validDesignIds = new HashSet<string>(LowUtils.EnumGetValues<Consts.Brands>().Select(id => id == Consts.Brands.lm ? null : id.ToString()));
//  public static Langs[] validLangs = new Langs[] { Langs.cs_cz, Langs.en_gb };
//  public static string[] validLangStrs = validLangs.Select(l => l.ToString().Replace('_', '-')).ToArray();
//  public static string[] validExtensions = new string[] { ".css", ".eot", ".gif", ".html", ".jpg", ".js", ".otf", ".pdf", ".png", ".svg", ".ttf", ".woff", ".woff2", ".xap", ".xlsx" };

//  //**************************** CSS logic
//  public static string[] cssMins = new string[] {
//    "jslib/css/lm.min.css",
//  };
//  public static string[] css = new string[] {
//    "font-awesome/lm/externals.css",
//    //"jslib/ea/ea.css",
//    "blendedapi/styles/style.css",
//    "jslib/css/lm.css",
//  };
//  public static Dictionary<string, string[]> cssSkins = new Dictionary<string, string[]>() {
//    {"skrivanek", new string[] { "jslib/skins/skrivanek/css.css" } },
//    {"grafia", new string[] { "jslib/skins/grafia/css.css" } },
//    {"chinh", new string[] { "jslib/skins/chinh/css.css" } }
//  };

//  //**************************** JS logic
//  static IEnumerable<string> jquery(bool isMin) { yield return isMin ? "jslib/scripts/jquery.min.js" : "jslib/scripts/jquery.js"; }

//  public static string[] jsMins = new string[] {
//    "deploy/externals.min.js",
//    "deploy/web.min.js",
//    "deploy/{0}.min.js"
//  };

//  public static string[] jsExternal = new string[] {
//    "jslib/scripts/underscore.js",
//    "jslib/scripts/angular.js",
//    "jslib/scripts/angular-route.js",
//    "jslib/scripts/angular-animate.js",
//    "jslib/scripts/angular-cookies.js",
//    "jslib/scripts/angular-ui-router.js",
//    "jslib/scripts/ui-bootstrap-tpls.js",

//    "jslib/scripts/jquery-migrate.js",
//    "JsLib/Scripts/jqvalidator/core.js",
//    "JsLib/Scripts/jqvalidator/delegate.js",
//    "JsLib/Scripts/jqvalidator/lm_remote.js",
//    "bs3/js/modal.js",
//    "bs3/js/tooltip.js",
//    "JsLib/JS/External/jqvalidator.js",
//    "JsLib/Scripts/ui/jquery-ui.js",
//    "JsLib/Scripts/query.autosize.js",
//    "jslib/scripts/globalize.js",
//    "jslib/scripts/jquery.ba-hashchange.js",
//    "jslib/scripts/json2.js",
//    "jslib/scripts/jsRender.js",
//    "jslib/scripts/knockout.js",
//    "jslib/scripts/knockout.validation.js",
//    "jslib/scripts/knockout-delegatedEvents.js",
//    "jslib/js/external/KnockoutPlugins.js",
//    "jslib/js/external/ClosureLib.js",
//    "jslib/js/external/ClosureLibLow.js",
//  };

//  public static string[] jsGround = new string[] {
//    "jslib/js/GenLMComLib.js",
//    "jslib/js/bowser.js",
//    "jslib/js/boot.js",

//    "jslib/js/unicode.js",
//    "jslib/js/base32.js",
//    "jslib/js/external/rjson.js",
//    "jslib/js/Utils.js",
//    "jslib/js/Ajax.js",

//    "jslib/ea/earepl.js",
//  };

//  public static string[] jsModel = new string[] {
//    "jslib/js/gui.js",
//    "jslib/js/ModelBase.js",
//    "jslib/js/Utils.js",
//    "jslib/js/ViewBase.js",
//    "jslib/js/Ajax.js",
//    "jslib/js/Validate.js",
//    "JsLib/Controls/TreeView/TreeView.js",
//    "blendedapi/oldBoot.js"
//  };

//  public static string[] jsScorm = new string[] {
//    "login/GenLogin.js",
//    "jslib/js/scorm.js",
//  };

//  public static string[] jsLogin = new string[] {
//    "jslib/js/OAuth.js",
//    "jslib/js/EMailer.js",
//    "login/GenLogin.js",
//    "login/Model.js",
//    "login/Login.js",
//    "login/LMLogin.js",
//    "login/Register.js",
//    "login/ChangePassword.js",
//    "login/ForgotPassword.js",
//    "login/Profile.js",
//    "login/ConfirmRegistration.js"
//  };

//  public static string[] jsAdmin = new string[] {
//    "admin/GenAdmin.js",
//    "admin/admin.js",
//    "admin/KeyGen.js",
//    "admin/Products.js",
//    "admin/HumanEval.js",
//    "admin/HumanEvalManager.js",
//    "admin/CompAdmins.js",
//    "Admin/UserResults.js",
//    "Admin/Departments.js",
//  };

//  public static string[] jsSchoolStart = new string[] {
//    "jslib/js/lmconsole.js",
//    "jslib/scripts/waitforimages.js",
//    //"schools/gencourse.js", prazdny
//    "schools/genschools.js",
//    "schools/genproxy.js",
//    //"schools/genazure.js", prazdny
//    "schools/interfaces.js",
//    "jslib/js/sound/mp3WorkerLib.js",
//    "jslib/js/sound/soundNew.js",
//    "jslib/js/sound/Html5Recorder.js",
//    "jslib/js/sound/wavePcm.js",
//    "jslib/js/unicode.js",
//    "jslib/js/base32.js",
//    "jslib/js/keys.js",
//    "jslib/ea/eaextension.js",
//    "jslib/controls/dict/dict.js",
//    "jslib/js/external/rjson.js",
//    //"schools/lib.js",
//    "schools/products.js",
//    "schools/topBar.js",
//    "schools/model.js",
//    "schools/persist.js",
//    "schools/persistlocal.js",
//    "schools/persistnewea.js",
//    "schools/persistdownload.js",
//    //"schools/persistphonegap.js",
//    "schools/persistscormex.js",
//    "schools/persistmemory.js",
//    "schools/splash.js",
//    "schools/CourseMetaGui.js",
//  };

//  public static string[] jsSchoolEnd = new string[] {
//    "schools/my.js",
//    "schools/exercise.js",
//    "schools/genscorm.js",
//    "schools/cpv.js",
//    "schools/gramm.js",
//  };

//  public static string[] jsCourse = new string[] {
//    "courses/GenCourseModel.js",
//    "testme/GenTestModel.js",
//    "courses/GenCourseMeta.js",
//    "courses/CourseModel.js",
//    "courses/Course.js",
//    "courses/CourseLib.js",
//    "courses/CourseMeta.js",
//    //"courses/CourseStatus.js",
//    "courses/GapFill.js",
//    "courses/Pairing.js",
//    "courses/ordering.js",
//    "courses/SingleChoice.js",
//    "courses/Media.js",
//    "courses/eval.js",
//    "courses/CheckItem.js",
//    "courses/codes/chinhSpeaking.js",
//    "courses/codes/docReference.js",
//    "testme/testexercise.js",
//    "testme/testresult.js",
//    "Author/vsNet.js",
//    "Author/doc.js",
//    "Author/xref.js",
//    "jslib/scripts/prettify.js",
//  };

//  public static string[] jsBlended = new string[] {
//    "blendedapi/scripts/lib.js",
//    "blendedapi/scripts/loader.js",
//    "blendedapi/scripts/directives.js",
//    "blendedapi/scripts/tasks.js",
//    "blendedapi/scripts/exercise.js",
//    "blendedapi/scripts/exercisesimple.js",
//    "blendedapi/scripts/module.js",
//    "blendedapi/scripts/stateman.js",

//    "blendedapi/vyzva/scripts/lib.js",
//    "blendedapi/vyzva/scripts/intranet.js",
//    //"blendedapi/vyzva/scripts/directives.js",

//    "blendedapi/vyzva/views/managerlangmaster.js",
//    "blendedapi/vyzva/views/managerschool.js",
//    "blendedapi/vyzva/views/exercise.js",
//    "blendedapi/vyzva/views/module.js",
//    "blendedapi/vyzva/views/pretest.js",
//    "blendedapi/vyzva/views/lector.js",
//    "blendedapi/vyzva/views/home.js",
//    "blendedapi/vyzva/views/faq.js",
//    "blendedapi/vyzva/views/testhw.js",
//    "blendedapi/vyzva/views/vyzvademo.js",
//    "blendedapi/vyzva/views/vyzvaprovoz.js",

//    "blendedapi/vyzva/views/lector/_tabs.js",

//    "blendedapi/vyzva/app.js",
//    "blendedapi/app.js",
//  };

//  public static string[] jsLoc = new string[] {
//    "schools/loc/tradosdata.{0}.js",
//    "jslib/scripts/cultures/globalize.culture.{0}.js"
//  };

//  public static Dictionary<string, string[]> jsSkins = new Dictionary<string, string[]>() {
//    {"skrivanek", new string[] { "jslib/skins/skrivanek/script.js" } },
//    {"grafia", new string[] { "jslib/skins/grafia/script.js" } },
//    {"chinh", new string[] { "jslib/skins/chinh/script.js" } }
//  };

//  public static string[][] externals = new string[][] { jsExternal };
//  public static string[][] web = new string[][] { jsGround, jsModel, jsLogin, jsAdmin, jsSchoolStart, jsSchoolEnd, jsCourse, jsBlended };
//  public static string[][] loc = new string[][] { jsLoc };

//}

//*********** SW_zipFiles
//public static void SW_oldBuild(string srcDir, string zipFile, Func<string, IEnumerable<string>> getFiles) {
//  runTask("SW_oldBuild", () => {
//    Trace.TraceWarning("srcDir: {0}, zipFile: {1}", srcDir, zipFile);

//    //HTML fragmenty pro generaci index.html
//    var htmlFnMask = "app-school/index-html-parts/{0}.html";
//    var stringEntries = LowUtils.EnumGetValues<Consts.Brands>().Select(designId => {
//      var res = Packager.MainPage.htmls(Packager.RewApp.htmlNewEA(true, designId == Consts.Brands.lm ? null : designId.ToString()));
//      return new Deploy.stringEntry { localPath = string.Format(htmlFnMask, designId), data = res };
//    });
//    var htmlFragments = Deploy.getEntries(stringEntries, true);
//    //SW soubory
//    var allFiles = Deploy.getEntries(srcDir, getFiles(srcDir));
//    //MIN soubory
//    var mins1 = XExtension.Create<Deploy.stringEntry>(
//      new Deploy.stringEntry { localPath = "", data = minify(srcDir, Deploy.externals.SelectMany(f => f), true, null) },
//      new Deploy.stringEntry { localPath = "", data = minify(srcDir, Deploy.web.SelectMany(f => f), true, null) },
//      new Deploy.stringEntry { localPath = "", data = minify(srcDir, Deploy.css, false, null) }
//    );
//    var mins2 = Deploy.validLangStrs.Select(lng =>
//      new Deploy.stringEntry { localPath = "", data = minify(srcDir, Deploy.loc.SelectMany(f => f), true, lng) }
//    );

//    //All to ZIP
//    Deploy.zipSWFiles(zipFile, htmlFragments.Concat(allFiles).Concat(Deploy.getEntries(mins1)).Concat(Deploy.getEntries(mins2)));
//    return null;
//  });
//}

