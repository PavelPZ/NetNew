//using EcmaScript.NET;
using LMComLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
//using Yahoo.Yui.Compressor;

namespace Packager {

  public static class MainPage {


    //je potreba kvuli Publisher Skin CSS
    public static string writeCss(IEnumerable<IEnumerable<string>> files, string basicUrl = "schools") {
      StringBuilder sb = new StringBuilder();
      foreach (var f in Consts.getFiles(Consts.pathType.relUrl, basicUrl, files)) sb.AppendFormat(cssTmpl, f);
      return sb.ToString();
    }
    const string cssTmpl = "  <link href='{0}' rel='stylesheet'/>\r\n";

    public static string writeJS(IEnumerable<IEnumerable<string>> files, string basicUrl = "schools") {
      StringBuilder sb = new StringBuilder();
      foreach (var f in Consts.getFiles(Consts.pathType.relUrl, basicUrl, files)) sb.AppendFormat(jsTmpl, f.StartsWith("../") ? f : "../schools/" + f);
      return sb.ToString();
    }

    public static Dictionary<string, string> htmlsDict(IEnumerable<IEnumerable<string>> sources) {
      //var errors = parseHtmls(Consts.getFiles(Consts.pathType.fileName, null, sources)).GroupBy(it => it.Item1).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
      return parseHtmls(Consts.getFiles(Consts.pathType.fileName, null, sources)).ToDictionary(iv => iv.Item1, iv => iv.Item2);
    }

    public static string htmls(IEnumerable<IEnumerable<string>> sources) {
      var fragments = parseHtmls(Consts.getFiles(Consts.pathType.fileName, null, sources));
      StringBuilder sb = new StringBuilder();
      foreach (var tp in fragments) {
        if (tp.Item1.StartsWith("!")) {
          var s = tp.Item1.Substring(1);
          sb.AppendLine(string.Format(htmlPageTmpl, s.Split(',')[0], s, tp.Item2));
        }
        else
          sb.AppendLine(string.Format(htmlTmpl, tp.Item1, tp.Item2));
      }
      return sb.ToString();
    }

    const string jsTmpl = "  <script type='text/javascript' src='{0}'></script>\r\n";

    const string htmlPageTmpl =
@"<script id=""{0}"" type=""text/x-jsrender"" data-for=""{1}"">
  {{{{for ~ActPage()}}}}
  {2}
  {{{{/for}}}}
</script>
";
    const string htmlTmpl = @"<script id=""{0}"" type=""text/x-jsrender"">{1}</script>";
    const string basicUrl = "http://www.langmaster.com/lmcom/rew/";

    static Regex removeAspComment = new Regex(@"<%--.*?--%>", RegexOptions.Singleline);

    static IEnumerable<Tuple<string, string>> parseHtmls(IEnumerable<string> files) {
      return files.SelectMany(f => toTupples(File.ReadAllText(f).Split(new string[] { "###" }, StringSplitOptions.None))).Where(t => !string.IsNullOrEmpty(t.Item1));
    }
    static IEnumerable<Tuple<string, string>> toTupples(IEnumerable<string> data) {
      if (data == null) yield break;
      bool isFirst = true; string first = null;
      foreach (var s in data) {
        if (isFirst) { first = s.Trim().ToLower(); isFirst = false; continue; }
        yield return Tuple.Create(first, removeAspComment.Replace(s, ""));
        isFirst = true;
      }
      if (!isFirst) yield return Tuple.Create(first, (string)null);
    }
  }

  public static class Consts {

    public static string basicPath = Machines.rootPath;

    public class file {
      public file() { }
      public file(string fn, string srcPath = null) {
        if (fn[0] == '/') fn = fn.Substring(1);
        fn = fn.ToLower().Replace('/', '\\');
        var idx = fn.LastIndexOf('\\');
        name = (idx < 0 ? fn : fn.Substring(idx + 1)).ToLower();
        destDir = idx < 0 ? null : fn.Substring(0, idx).ToLower();
        this.srcPath = (srcPath == null ? basicPath + (destDir == null ? null : destDir + "\\") + name : srcPath).ToLower();
      }
      public file(string fn, byte[] srcData) : this(fn) { this._srcData = srcData; }
      public file(string fn, Func<byte[]> getSrcData) : this(fn) { this.getSrcData = getSrcData; }
      public string srcPath; //napr. q:\LMCom\rew\Downloads\Common\IIS nebo 
      public byte[] srcData { get { return _srcData != null ? _srcData : (getSrcData != null ? _srcData = getSrcData() : null); } }
      byte[] _srcData;
      Func<byte[]> getSrcData;
      public string destDir; //napr. schools
      public string name; //napr. Index.html
      public void copyTo(Stream str) {
        if (srcData != null) str.Write(srcData, 0, srcData.Length); else using (var fs = File.OpenRead(srcPath)) fs.CopyTo(str);
      }
    }

    public static void saveISSFile(IEnumerable<Consts.file> files, string issFileName) {
      StringBuilder sb = new StringBuilder();
      foreach (var f in files) sb.AppendLine(Consts.ISSFile(f));
      File.WriteAllText(issFileName, sb.ToString());
    }

    //pro Download
    static string ISSFile(Consts.file file) {
      return string.Format(@"source: {0}; DestDir: {{app}}\{{#path}}data\{1}; DestName:{2}; Flags: ignoreversion; ", file.srcPath, file.destDir, file.name);
    }

    ////pro Scorm manifest
    //public static void scormManifestFiles(IEnumerable<Consts.file> files, string issFileName, bool replaceJSON) {
    //  StringBuilder sb = new StringBuilder();
    //  foreach (var f in files) sb.AppendLine(Consts.scormManifestFile(f, replaceJSON));
    //  File.WriteAllText(issFileName, sb.ToString());
    //}

    ////pro Scorm manifest
    //static string scormManifestFile(Consts.file file, bool replaceJSON) {
    //  return string.Format(@"<file href=""{0}"" />", (file.destDir + "/" + (replaceJSON ? file.name.Replace(".json", ".jsn") : file.name)).ToLowerInvariant().Replace('\\', '/'));
    //}

    ////Scorm ZIP predpis
    //public static void scormZipFiles(IEnumerable<Consts.file> files, string issFileName, bool replaceJSON) {
    //  StringBuilder sb = new StringBuilder();
    //  foreach (var f in files) sb.AppendLine(Consts.scormZipFile(f, replaceJSON));
    //  File.WriteAllText(issFileName, sb.ToString());
    //}

    ////Scorm ZIP predpis
    //static string scormZipFile(Consts.file file, bool replaceJSON) {
    //  return string.Format("{0}>>{1}", file.srcPath, (file.destDir == null ? null : file.destDir + "\\") + (replaceJSON ? file.name.Replace(".json", ".jsn") : file.name)).ToLowerInvariant();
    //}

    public enum pathType {
      relUrl,
      fileName,
    }

    public static IEnumerable<string> getFiles(pathType type, string urlDir, IEnumerable<IEnumerable<string>> sources) {
      foreach (var f in sources.SelectMany(s => s)) {
        switch (type) {
          case pathType.relUrl: yield return VirtualPathUtility.MakeRelative("/" + urlDir + "/x.htm", "/" + f.ToLower()); break;
          case pathType.fileName: yield return basicPath + f.ToLower().Replace('/', '\\'); break;
        }
      }
    }

    public static string isl(Langs lng) {
      switch (lng) {
        case Langs.ar_sa: return @"Languages\Arabic.isl";
        case Langs.bg_bg: return @"Languages\Bulgarian.isl";
        case Langs.ca_es: return @"Languages\Catalan.isl";
        case Langs.cs_cz: return @"Languages\Czech.isl";
        case Langs.da_dk: return @"Languages\Danish.isl";
        case Langs.de_de: return @"Languages\German.isl";
        case Langs.el_gr: return @"Languages\Greek.isl";
        case Langs.en_gb: return @"default.isl";
        case Langs.es_es: return @"Languages\Spanish.isl";
        case Langs.fi_fi: return @"Languages\Finnish.isl";
        case Langs.fr_fr: return @"Languages\French.isl";
        case Langs.he_il: return @"Languages\Hebrew.isl";
        case Langs.hr_hr: return @"Languages\Croatian.isl";
        case Langs.hu_hu: return @"Languages\Hungarian.isl";
        case Langs.it_it: return @"Languages\Italian.isl";
        case Langs.ja_jp: return @"Languages\Japanese.isl";
        case Langs.ko_kr: return @"Languages\Korean.isl";
        case Langs.lv_lv: return @"Languages\Latvian.isl";
        case Langs.nb_no: return @"Languages\Norwegian.isl";
        case Langs.nl_nl: return @"Languages\Dutch.isl";
        case Langs.pl_pl: return @"Languages\Polish.isl";
        case Langs.pt_br: return @"Languages\BrazilianPortuguese.isl";
        case Langs.pt_pt: return @"Languages\Portuguese.isl";
        case Langs.ro_ro: return @"Languages\Romanian.isl";
        case Langs.ru_ru: return @"Languages\Russian.isl";
        case Langs.sk_sk: return @"Languages\Slovak.isl";
        case Langs.sl_si: return @"Languages\Slovenian.isl";
        case Langs.sp_sp: return @"Languages\Spanish.isl";
        case Langs.sv_se: return @"Languages\Swedish.isl";
        case Langs.th_th: return @"Languages\Thai.isl";
        case Langs.tr_tr: return @"Languages\Turkish.isl";
        case Langs.uk_ua: return @"Languages\Ukrainian.isl";
        case Langs.vi_vn: return @"Languages\Vietnamese.isl";
        case Langs.zh_cn: return @"Languages\ChineseSimp.isl";
        case Langs.lt_lt: return @"Languages\Lithuanian.isl";
        case Langs.bs: return @"default.isl";
        default: throw new Exception("~/Services/Downloads/Generators/Setup_CommonCode.ascx: missing code here");
      }
    }

    /************** COMMON ************************/
    public static string[] jsExternal = new string[] {
      "jslib/scripts/jquery-migrate.js",
      "JsLib/Scripts/jqvalidator/core.js",
      "JsLib/Scripts/jqvalidator/delegate.js",
      "JsLib/Scripts/jqvalidator/lm_remote.js",
      "bs3/js/modal.js",
      "bs3/js/tooltip.js",
      "JsLib/JS/External/jqvalidator.js",
      "JsLib/Scripts/ui/jquery-ui.js",
      "JsLib/Scripts/query.autosize.js",
      "jslib/scripts/globalize.js",
      "jslib/scripts/jquery.ba-hashchange.js",
      "jslib/scripts/json2.js",
      "jslib/scripts/jsRender.js",
      "jslib/scripts/knockout.js",
      "jslib/scripts/knockout.validation.js",
      "jslib/scripts/knockout-delegatedEvents.js",
      "jslib/js/external/KnockoutPlugins.js",
      "jslib/js/external/ClosureLib.js",
      "jslib/js/external/ClosureLibLow.js",
      "jslib/js/GenLMComLib.js",
      "jslib/js/bowser.js",
      "jslib/js/boot.js",
    };

    public static string[] jsGround = new string[] {
      "jslib/js/unicode.js",
      "jslib/js/base32.js",
      "jslib/js/external/rjson.js",
      "jslib/js/Utils.js",
      "jslib/js/Ajax.js",
    };

    public static string[] jsModel = new string[] {
      "jslib/js/gui.js",
      "jslib/js/ModelBase.js",
      "jslib/js/Utils.js",
      "jslib/js/ViewBase.js",
      "schools/genproxy.js",
      "jslib/js/Ajax.js",
      "jslib/js/Validate.js",
      "JsLib/Controls/TreeView/TreeView.js",
      "blendedapi/oldBoot.js"
    };

    public static string[] htmlJsLib = new string[] {
      "JsLib/Controls/Common/Breadcrumb.html",
      "JsLib/Controls/Common/Input.html",
      "JsLib/Controls/Common/MenuItem.html",
      "JsLib/Controls/Common/OkCancel.html",
      "JsLib/Controls/Common/TwoColumn.html",
      "JsLib/Controls/CrsItem/CrsItem.html",
      "JsLib/Controls/TreeView/TreeView.html",
    };
    public static string[] htmlLogin = new string[] {
      "Login/ConfirmRegistration.html",
      "Login/EMails.html",
      "Login/ForgotPassword.html",
      "Login/ChangePassword.html",
      "Login/LMLogin.html",
      "Login/Login.html",
      "Login/Profile.html",
      "Login/Register.html",
    };

    /************** EA ************************/
    public static string[] jsEA = new string[] {
      "jslib/ea/ea.js",
    };

    public static string[] jsEARepl = new string[] {
      "jslib/ea/earepl.js",
    };

    /************** LOGIN ************************/
    public static string[] jsLogin = new string[] {
      "jslib/js/OAuth.js",
      "jslib/js/EMailer.js",
      "login/GenLogin.js",
      "login/Model.js",
      "login/Login.js",
      "login/LMLogin.js",
      "login/Register.js",
      "login/ChangePassword.js",
      "login/ForgotPassword.js",
      "login/Profile.js",
      "login/ConfirmRegistration.js"
    };


    /************** COURSE ************************/
    public static string[] jsCourse = new string[] {
      "courses/GenCourseModel.js",
      "testme/GenTestModel.js",
      "courses/GenCourseMeta.js",
      "courses/CourseModel.js",
      "courses/Course.js",
      "courses/CourseLib.js",
      "courses/CourseMeta.js",
      "courses/CourseStatus.js",
      "courses/GapFill.js",
      "courses/Pairing.js",
      "courses/ordering.js",
      "courses/SingleChoice.js",
      "courses/Media.js",
      "courses/eval.js",
      "courses/CheckItem.js",
      "courses/codes/chinhSpeaking.js",
      "courses/codes/docReference.js",
      "testme/testexercise.js",
      "testme/testresult.js",
      "Author/vsNet.js",
      "Author/doc.js",
      "Author/xref.js",
      "jslib/scripts/prettify.js",
    };

    public static string[] htmlCourse = new string[] {
      "courses/course.html",
      "courses/GapFill.html",
      "courses/Pairing.html",
      "courses/ordering.html",
      "courses/SingleChoice.html",
      "courses/Media.html",
      "courses/CheckItem.html",
      "courses/codes/chinhSpeaking.html",
      "courses/codes/docReference.html",
      "courses/Macro.html",
      "testMe/TestExercise.html",
      "testMe/TestResult.html",
      "testMe/TestHome.html",
      "Author/vsNet.html",
      "Author/doc.html",
      "Author/xref.html",
    };

    /************** SCHOOL ************************/
    public static string[] jsAdmin = new string[] {
      "admin/GenAdmin.js",
      "admin/admin.js",
      "admin/KeyGen.js",
      "admin/Products.js",
      "admin/HumanEval.js",
      "admin/HumanEvalManager.js",
      "admin/CompAdmins.js",
      "Admin/UserResults.js",
      "Admin/Departments.js",
    };

    public static string[] jsScorm = new string[] {
      "login/GenLogin.js",
      "jslib/js/scorm.js",
    };

    public static string[] htmlAdmin = new string[] {
      "Admin/Admin.html",
      "Admin/CompAdmins.html",
      "Admin/EMails.html",
      "Admin/KeyGen.html",
      "Admin/Products.html",
      "Admin/HumanEval.html",
      "Admin/HumanEvalManager.html",
      "Admin/UserResults.html",
      "Admin/Departments.html",
      //"Admin/Publishers.html",
    };

    /************** AUTHOR ************************/
    public static string[] jsAuthor = new string[] {
    };
    public static string[] htmlAuthor = new string[] {
    };

    /************** REWISE ************************/
    public static string[] jsRewise = new string[] {
      "jslib/js/external/RJSON.js",
      "Rewise/GenLMComLib.js",
      "Rewise/GenRw.js",
      "Rewise/GenRew.js",
      "Rewise/Model.js",
      "Rewise/Book.js",
      "Rewise/BookView.js",
      "Rewise/Fact.js",
      "Rewise/Home.js",
      "Rewise/HomeMobile.ascx.js",
      "Rewise/HomeView.js",
      "Rewise/JsRenderEx.js",
      "Rewise/Lesson.js",
      "Rewise/LessonView.js",
      "Rewise/RwPersist.js",
      "Rewise/RwPersistFake.js",
      "Rewise/SelectLang.js",
      "Rewise/Vocab.js",
      "Rewise/VocabView.js"
    };

    public static string[] htmlRewise = new string[] {
      "Rewise/BookMobile.html",
      "Rewise/FactMobile.html",
      "Rewise/Home.html",
      "Rewise/HomeMobile.html",
      "Rewise/LessonMobile.html",
      "Rewise/SelectLang.html",
      "Rewise/SelectLangMobile.html",
      "Rewise/Vocab.html",
      "Rewise/VocabMobile.html",
    };

    /************** SCHOOL ************************/
    public static string[] jsSchoolStart = new string[] {
      "jslib/js/lmconsole.js",
      "jslib/scripts/waitforimages.js",
      //"schools/gencourse.js",
      "schools/genschools.js",
      "schools/genazure.js",
      "schools/interfaces.js",
      "jslib/js/sound/mp3WorkerLib.js",
      "jslib/js/sound/soundNew.js",
      "jslib/js/sound/Html5Recorder.js",
      "jslib/js/sound/wavePcm.js",
      "jslib/js/unicode.js",
      "jslib/js/base32.js",
      "jslib/js/keys.js",
      "jslib/ea/eaextension.js",
      "jslib/controls/dict/dict.js",
      "jslib/js/external/rjson.js",
      //"schools/lib.js",
      "schools/products.js",
      "schools/topBar.js",
      "schools/model.js",
      "schools/persist.js",
      //"schools/persistlocal.js",
      "schools/persistnewea.js",
      //"schools/persistdownload.js",
      //"schools/persistphonegap.js",
      "schools/persistscormex.js",
      //"schools/persistmemory.js",
      "schools/splash.js",
      "schools/CourseMetaGui.js",
    };

    public static string[] jsSchoolEnd = new string[] {
      "schools/my.js",
      "schools/exercise.js",
      "schools/genscorm.js",
      "schools/cpv.js",
      "schools/gramm.js",
    };

    public static string[] jsDesign = new string[] {
      "schools/design/design.js"
    };

    public static string[] htmlSchool = new string[] {
      "Schools/Exercise.html",
      "Schools/TopBar.html",
      "Schools/My.html",
      "Schools/Cpv.html",
      "Schools/Gramm.html",
      "Schools/DictAbout.html",
      "Schools/Test.html",
      "JsLib/Controls/Dict/Dict.html",
      "schools/splash.html",
      "jslib/js/Bowser.html",
      "schools/CourseMetaGui.html",
    };

    /************** AUTHOR WEB ************************/
    public static string[] jsAuthorWebMin = new string[] { //minimalni spolecne JS, kdyz je authorweb pouzivan mimo shools/xxx
      //jsGround
      "jslib/js/unicode.js",
      "jslib/js/base32.js",
      "jslib/js/Ajax.js",
      //jsExternal
      "JsLib/Scripts/jqvalidator/core.js",
      "JsLib/Scripts/jqvalidator/delegate.js",
      "JsLib/Scripts/jqvalidator/lm_remote.js",
      "JsLib/JS/External/jqvalidator.js",
      "jslib/scripts/globalize.js",
      "jslib/js/external/ClosureLib.js",
      "jslib/js/external/ClosureLibLow.js",
      "jslib/js/GenLMComLib.js",
      "jslib/js/bowser.js",
      "courses/GenCourseModel.js",
      "testme/GenTestModel.js",
      "courses/GenCourseMeta.js",
    };

    public static string[] jsBlended = new string[] {
      "blendedapi/scripts/lib.js",
      "blendedapi/scripts/loader.js",
      "blendedapi/scripts/directives.js",
      "blendedapi/scripts/tasks.js",
      "blendedapi/scripts/exercise.js",
      "blendedapi/scripts/exercisesimple.js",
      "blendedapi/scripts/module.js",
      "blendedapi/scripts/stateman.js",

      "blendedapi/vyzva/scripts/lib.js",
      "blendedapi/vyzva/scripts/intranet.js",
      "blendedapi/vyzva/scripts/directives.js",

      "blendedapi/vyzva/views/managerlangmaster.js",
      "blendedapi/vyzva/views/managerschool.js",
      "blendedapi/vyzva/views/exercise.js",
      "blendedapi/vyzva/views/module.js",
      "blendedapi/vyzva/views/pretest.js",
      "blendedapi/vyzva/views/lector.js",
      "blendedapi/vyzva/views/home.js",
      "blendedapi/vyzva/views/faq.js",
      "blendedapi/vyzva/views/testhw.js",
      "blendedapi/vyzva/views/vyzvademo.js",
      "blendedapi/vyzva/views/vyzvaprovoz.js",

      //"blendedapi/vyzva/views/toolbar/module.js",
      //"blendedapi/vyzva/views/toolbar/run.js",
      //"blendedapi/vyzva/views/home/pretestitem.js",
      //"blendedapi/vyzva/views/home/lesson.js",
      //"blendedapi/vyzva/views/exercise/instruction.js",
      //"blendedapi/vyzva/views/exercise/modulemap.js",
      //"blendedapi/vyzva/views/lector/eval.js",
      "blendedapi/vyzva/views/lector/_tabs.js",

      //"courses/angularjs/angularjs.js",

      "blendedapi/vyzva/app.js",
      "blendedapi/app.js",
    };

    public static string[] jsAuthorWeb = new string[] {
      //"authorweb/externals/commonMark/dist/commonmark.js",
      //"authorweb/js/jquery-textrange.js",
      //"authorweb/js/metaJSGen.js",
      //"authorweb/js/metaJS.js",
      //"authorweb/js/marksLow.js",
      //"authorweb/js/metaMarks.js",
      //"authorweb/js/encodeLib.js",
      //"authorweb/js/docObjects.js",
      //"authorweb/js/marks.js",
      //"authorweb/js/marksInline.js",
      //"authorweb/js/docText.js",
      //"authorweb/js/docBlock.js",
      //"authorweb/dialogs/dialogs.js",
      //"authorweb/js/compiler.js",
      //"authorweb/js/compRenderTag.js",
    };

  }

  //static void aspNet_run_exe(string path, string pars) {
  //  Process proc = new Process();
  //  proc.StartInfo.WorkingDirectory = Machines.basicPath + @"rew\Downloads\Common\IIS";
  //  proc.StartInfo.FileName = path;
  //  proc.StartInfo.Arguments = pars;
  //  proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
  //  proc.Start();
  //  proc.WaitForExit();
  //}
}

