using Course;
using LMComLib;
using LMNetLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

#if SILVERLIGHT
public class NonSerialized : Attribute { }
#endif

namespace Rw
{
    public enum CreativeCommonLic {
    unknown,
    cc_by,
    cc_by_sa,
    cc_by_nd,
    cc_by_nc,
    cc_by_nc_sa,
    cc_by_nc_nd,
  }
}

namespace schools
{

    //public enum dictTypes {
    //  no = 0, //bez slovniku
    //  lingWeb = 1, //Lingea slovnik z libgea webu
    //  lingOffline = 2, //lingea slovnik offline z napr. rew\Web4\Schools\EAData\cs-cz\lingDict_italian3_xl10_sm_shome_dhtm.rjson
    //  //lingLMWeb = 3, //TODO: lingea slovnik z lm webu
    //  //LMOffline = 4, //TODO: LM slovnik offline
    //  ttsUrl = 99, //priznak url ke zvukum, vedouci do d:\LMCom\rew\Web4\RwTTS\ adresare
    //}

    public enum persistTypes {
    no = 0, //difotni
    persistNewEA = 1,
    persistScormEx = 2,
    persistScormLocal = 3,
    persistMemory = 4,
  }

  public enum LicenceType {
    //domena musi byt znama a course.js je v index.htm. 
    //licenceExpiration neni nula => licence vyprsi za licenceExpiration dni. Jinak plati navzdy.
    fix,
    //domena se ignoruje, course.js se pri prvnim spusteni vytvori dynamicky pro aktualni domenu (temporary se pouzije course.js s expiraci).
    //licenceExpiration == 0 => pouzije se expirace po licenceExpiration dnech a pak vzdy po licenceExpirationInterval dnech. 
    //licenceExpirationInterval = maxint => pak se opakovane vytvoreni neuplatni a budto licence plati stale (licenceExpiration==0) nebo navzdy vyprsi (po licenceExpiration dnech)
    dynamic,
  }

  //rucne editovany XML v rew\Downloads\JSCrambler\_course\ s informacemi o licenci
  public class licence {
    [XmlAttribute]
    public string Url; //identifikace licence, API Url pro scorm, root URL pro web
    [XmlAttribute]
    public DateTime Created; //datum vytvoreni = datum prvniho spusteni aplikace uzivatelem
    [XmlAttribute]
    public int ProlongDays; //o jaky interval se prodluzuje ochrana JS
    public List<licenceMonth> Months; // informace o mesicnim provozu
    public List<licenceBuy> Buys; //zakoupene licence
  }
  public class licenceBuy {
    [XmlAttribute]
    public DateTime Created; //datum nakupu
    [XmlAttribute]
    public int UserMonths; //pocet clovekomesicu
    [XmlAttribute]
    public string Info; //dalsi informace (variabilni symbol apod.)
  }
  public class licenceMonth {
    [XmlAttribute]
    public short Year; //rok
    [XmlAttribute]
    public byte Month; //mesic
    public List<licenceUser> Users; //uzivatele za mesic
  }
  public class licenceUser {
    [XmlAttribute]
    public DateTime Created; //datum vytvoreni = datum prvniho spusteni aplikace uzivatelem v danem mesici
    [XmlAttribute]
    public string Id; //SCORM Id nebo LANGMaster UserId
    [XmlAttribute]
    public string Name; //dalsi informace (email, scorm first last name apod.)
    [XmlAttribute]
    public string rootCourse; //kurz pro SCORM modul
  }
  //Vysledek overeni licencem, plneno v d:\LMCom\rew\Web4\DynamicScript.ashx, pouzito v d:\LMCom\rew\Web4\JsLib\JS\Boot.ts
  public class licenceRequest {
    [XmlAttribute]
    public Targets Target;
    [XmlAttribute]
    public string rootCourse; //kurz pro SCORM modul
    //z Cookie
    [XmlAttribute]
    public OtherType Type;
    [XmlAttribute]
    public string TypeId;
    [XmlAttribute]
    public string FirstName;
    [XmlAttribute]
    public string LastName;
    [XmlAttribute]
    public string EMail;
    [XmlAttribute]
    public string Login;
    //z licenceConfig
    [XmlAttribute]
    public string appUrl; //url scorm api nebo url stranku s web aplikaci
    [XmlAttribute]
    public uint courseVersion; //verze _course souboru. 0 => aktualni soubor (pouze pro ladeni z localhost nebo FE5)

    public string GlobalId() {
      string id;
      switch (Type) {
        case OtherType.LANGMaster: id = EMail; break;
        case OtherType.LANGMasterNoEMail: id = Login; break;
        default: id = TypeId; break;
      }
      return ((int)Type).ToString() + "-" + id;
    }

  }

  public enum licenceResult { ok, wrongDomain, demoExpired, userMonthExpired, JSCramblerError }
  public class licenceResponse {
    public string Id;
    public licenceResult result;
    public long DemoExpired; //datum expirace dema
    public licenceRespMonth[] Months; // informace o mesicnim provozu
    public licenceRespBuy[] Buys; //zakoupene licence
  }
  public class licenceRespBuy {
    public long Created; //datum nakupu
    public int UserMonths; //pocet clovekomesicu
  }
  public class licenceRespMonth {
    public long Date; //datum
    public licenceRespUser[] Users; //uzivatele za mesic
  }
  public class licenceRespUser {
    public long Created; //datum vytvoreni = datum prvniho spusteni aplikace uzivatelem v danem mesici
    public string Id; //SCORM Id nebo LANGMaster UserId
    public string Name; //dalsi informace (email, scorm first last name apod.)
    public string rootCourse; //kurz pro SCORM modul
  }  //Konfigurace licence pro SCORM modul nebo WEB
  public class licenceConfig {
    //isDynamic=false: course.js je v index.htm. 
    //    domena==null => exception
    //    domena!=null => expirace na domenu
    //    expiration != null => datum vyprseni licence
    //    expiration == null => licence nevyprsi 
    //isDynamic=true: 
    //    domena==null => expirace na dynamicky zjistenou domena
    //    domena!=null => expirace na dopredu znamou domenu
    //    expiration != null => datum vyprseni licence
    //    expiration == null => licence nevyprsi 
    [XmlAttribute]
    public bool isDynamic;
    [XmlAttribute]
    public string domain; //ochrana na domenu
    [XmlAttribute, JsonIgnore]
    public DateTime expiration { //ochrana na pocet dni od buildu. DateTime.MinValue => neplatna expirace
      get { return intExpiration == 0 ? DateTime.MinValue : LowUtils.IntToDate(intExpiration); }
      set { intExpiration = value == DateTime.MinValue ? 0 : LowUtils.DateToInt(value); }
    }
    [XmlIgnore]
    public long intExpiration;
    [XmlAttribute]
    public string serviceUrl; //url service pro isDynamic

    [XmlIgnore]
    public string appUrl; //url scorm api nebo url stranku s web aplikaci

    [XmlAttribute]
    public uint rewVersion; //verze _rew_scorm nebo _rew_web souboru
    [XmlAttribute]
    public uint courseVersion; //verze _course souboru
    [XmlAttribute]
    public uint extVersion; //verze _externals souboru
    [XmlAttribute]
    public uint groundVersion; //verze _ground souboru
    public licenceConfig copy() {
      return new licenceConfig() {
        isDynamic = isDynamic,
        domain = domain,
        expiration = expiration,
        rewVersion = rewVersion,
        courseVersion = courseVersion,
        serviceUrl = serviceUrl,
        extVersion = extVersion,
        groundVersion = groundVersion,
      };
    }
    //rozliseni souboru pro domenu a expiraci
    public string fileSignature() {
      string dom = appUrl != null ? appUrl.Replace("/mod/scorm/player.php", null) : domain;
      if (dom == null && expiration == DateTime.MinValue) return null;
      string res = null;
      if (dom != null) res += "." + dom.Replace('.', '-');
      if (expiration != DateTime.MinValue) res += "." + expiration.ToString("yyyy-MM-dd");
      return res;
    }
  }

  public enum versions {
    no,
    debug,
    not_minified,
    minified,
  }

  public enum scormDriver { 
    no,
    moodle,
    edoceo,
  }

  public enum displayModes {
    normal,
    previewEx,
  }

  public class config {
    public config() { EADataPath = "eaimgmp3/"; }
    public string blobJS; //path s JS datovymi soubory, napr. https://lmdata.blob.core.windows.net/v1-0
    public string blobMM; //path s MM soubory
    public string dataBatchUrl; //pro web: CourseMeta.WebBatch.url. Identifikace buildu dat, aby se vedelo, ktery seznam produktu ladovat (napr. laduje se d:\LMCom\rew\Web4\prod\lm_debug.json)
    public int failLimit;
    public bool canSkipCourse;
    public bool canSkipTest;
    public bool canResetCourse;
    public bool canResetTest;
    //public SoundPlayerType ForceSoundPlayer;
    public string EADataPath; // { get { return "eaimgmp3/"; } set { } }
    public bool noCpv; //s CPV
    public Targets target;
    public versions version; //debug mod
    public bool noOldEA; //neni potreba stary EA runtime
    public persistTypes persistType; //typ persistence. no: difotni pro target.
    public string rootProductId; //@PRODID
    public string forceServiceUrl; //natvrdo se nastavi service URL, napr. URL Scorm persistence pro skoda auto
    public string forceLoggerUrl; //log Url. Je=li "no", pak se log dava pouze pres clipboard
    public string dictOfflineId; //identifikace slovniku, napr. 'L' pro Schools\EAData\cs-cz\english1_xl01_sa_shome_dhtm_L.rjson
    public bool dictNoSound; //neozvuceny slovnik
    //public dictTypes dictType;
    //public Langs dictForceLoc; //napr. en_gb pro anglictinu v anglictine
    //public Langs singleLang; //pro EN-EN: jaky slovnik se pouzije
    public scormDriver scorm_driver; //upreneni tscorm pro moodle true, pro edoceo false. True => je mozne plytvat Scorm.Finish x Init
    public bool debug_AddTimespanToJsonUrl; //zabrani cachovani JSON a RJSON souboru
    public string designId; //muze byt identifikace partnera
    public bool replaceJSON; //kvuli Sharepointu nahrad JSON koncovku
    public string debugTypes; //comma delimited seznam DEBUG jmen pro Debug.ids
    public string noDebugTypes; //comma delimited seznam DEBUG jmen pro Debug.noIds
    public OtherType[] logins; //typy loginu pro web. Difotne vse bez LANGMasterNoEMail
    public string startProcName; //difotne boot.Start, vola se v v d:\LMCom\rew\Web4\JsLib\JS\Boot.ts. Pro d:\LMCom\rew\Web4\Schools\Statistics.aspx se pouzije statistics.init.
    public Langs? lang; //jazyk lokalizace
    public bool UseParentCfg; //pro iframe se statistikami: pouzij cfg z parent window

    public string themeId; //theme 
    public string hash; //initialni Hash 
    public bool forceEval; //cviceni ze difotne zobrazi v vyhodnocenem rezimu se spravnymi odpovedmi
    public bool themeDefauleNavbar; //inverse nebo default

    public bool testGroup_debug; //provede se testTaskGroup.take = testTaskGroup.takeDebug
    public bool humanEvalMode; //cviceni ze zobrazuje v modu vyhodnoceni ucitelem

    public string basicPath; //pro ucely designtime, napr. d:\lmcom\
    public string baseTagUrl; //do home stranky se doplni <base> tag

    public LMComLib.SoundPlayerType forceDriver; 

    public displayModes displayMode;

    public string alowedParentDomain; //je-li window.parent.url domena rovna alowedParentDomain, tak se provede fake login.

    public licenceConfig licenceConfig; //config licence

    public bool vocabulary; //kurz s rewise apod. slovnickem. Nejedna se o Dictionary (= slovnik)

    public config copyTo(config cfg) {
      cfg.blobJS = blobJS;
      cfg.blobMM = blobMM;
      cfg.dataBatchUrl = dataBatchUrl;
      cfg.failLimit = failLimit;
      cfg.canSkipCourse = canSkipCourse;
      cfg.canSkipTest = canSkipTest;
      cfg.canResetCourse = canResetCourse;
      cfg.canResetTest = canResetTest;
      cfg.noCpv = noCpv;
      cfg.target = target;
      cfg.rootProductId = rootProductId;
      cfg.forceServiceUrl = forceServiceUrl;
      cfg.forceLoggerUrl = forceLoggerUrl;
      cfg.dictNoSound = dictNoSound;
      //cfg.dictType = dictType;
      //cfg.dictForceLoc = dictForceLoc;
      //cfg.singleLang = singleLang;
      cfg.dictOfflineId = dictOfflineId;
      cfg.vocabulary = vocabulary;
      cfg.persistType = persistType;
      cfg.scorm_driver = scorm_driver;
      //cfg.ForceSoundPlayer = ForceSoundPlayer;
      cfg.debug_AddTimespanToJsonUrl = debug_AddTimespanToJsonUrl;
      cfg.designId = designId;
      cfg.replaceJSON = replaceJSON;
      cfg.debugTypes = debugTypes;
      cfg.noDebugTypes = noDebugTypes;
      cfg.noOldEA = noOldEA;
      cfg.version = version;
      cfg.startProcName = startProcName;
      cfg.lang = lang;
      cfg.UseParentCfg = UseParentCfg;
      cfg.themeId = themeId;
      cfg.hash = hash;
      cfg.forceEval = forceEval;
      cfg.themeDefauleNavbar = themeDefauleNavbar;
      cfg.basicPath = basicPath;
      cfg.testGroup_debug = testGroup_debug;
      cfg.humanEvalMode = humanEvalMode;
      cfg.baseTagUrl = baseTagUrl;
      cfg.logins = logins==null ? null : logins.ToArray();
      cfg.forceDriver = forceDriver;
      cfg.displayMode = displayMode;
      cfg.alowedParentDomain = alowedParentDomain;
      if (cfg.licenceConfig == null && licenceConfig != null) cfg.licenceConfig = licenceConfig.copy();
      return cfg;
    }

    public string normalizeDestFn(string destFn) {
      destFn = destFn.ToLowerInvariant();
      if (destFn.EndsWith(".json") || destFn.EndsWith(".rjson") || destFn.EndsWith(".lst")) throw new Exception("normalizeDestFn");
      return destFn; // (replaceJSON ? destFn.Replace(".json", ".js").Replace(".rjson", ".js").Replace(".lst", ".txt") : destFn).ToLowerInvariant();
    }
  }

  //public enum DictUrltype { 
  //  lingeaOnline = 0,
  //  lingeaOffline = 1,
  //}
  public class DictExWords {
    public string modId;
    public string exId;
    public string normalized;
    //public string[] blocks; //bloky textu, ziskane z HTML tagu z JS
    //public string[] words; //slova, vznikla parsovanim textu z blocks
  }

  public class DictCrsWords {
    public LMComLib.Langs lang;
    public string fileName;
    public DictExWords[] exs;
  }

  public class DictWords {
    public DictCrsWords[] courses;
  }

  public class Dict {
    public Langs crsLang;
    public Langs natLang;
    public Dictionary<string, string> Tags; //prevod cislo tagu => tak name pro DictItemTag
    public Dictionary<string, int> Keys; //prevod slovo v kurzu => jednoznacny key Lingea hesla
    public Dictionary<string, DictItemRoot> Entries; //prevod Key => zakodovane HTML s heslem
  }
  public enum DictEntryType { lingeaOld, rj, Wiktionary }
  public class DictItemRoot: DictItem {
    public DictEntryType type;
    public string[] soundFiles;
  }
  public class DictItem {
    public short tag;
    public string urls; //zakodovany seznam URL do slovniku dictTypes:value|..., napr. 0:http://www.lingea.cz/ilex51/lms/t/aenru80000001664.mp3|1:ru_ru/arucz103a708
    public DictItem[] items;
    public string text;
  }

  public class SchoolCmdGetDict {
    public string dictId;
    public string word;
  }

  //public class SchoolCmdCreateTest {
  //  public string dbTestFileName;
  //  public long lmcomUserId;
  //  public int companyId;
  //  public string productId;
  //  //public string email;
  //  //public string firstName;
  //  //public string lastName;
  //  //public string companyEMail;
  //}
  //public class SchoolCmdTestInfo {
  //  public string getIds;
  //}
  //public class SchoolCmdTestInfoItem {
  //  public int id; //identifikace testu
  //  public long RepStart;
  //  public long RepEnd;
  //  public int RepInterruptions;
  //  public int RepScore;
  //  public testMe.Status status;
  //  //pomocne informace pro SingleTask testy
  //  public int metataskId; //identifikace kurzu v ramci metakurzu
  //}
  //public class SchoolCmdTestInfoResult {
  //  public SchoolCmdTestInfoItem[] testUserInfos;
  //}

  //Client.xap service
  public class SchoolCmdFillCompanyUserInfoTestId {
    public int userTestId;
  }
  public class SchoolCmdLoadTestInfo {
    public int userTestId;
  }
  public class SchoolCmdLoadModule {
    public int moduleId;
  }
  public class SchoolCmdSaveTest {
    public TestCriteria test;
  }
  public class SchoolCmdSaveModule {
    public ModuleCriteria module;
  }

  //public enum taskStatus {
  //  toRun = 0, //budouci kurzy
  //  testFailed = 1, //budouci test (jako toRun), predchozi test nebyl uspesny
  //  toStart = 2, //vse je splneno pro start kurzu, jeste ale neni nastartovan. Temporary priznak, ktery vzdy pri nacteni metakurzu meni na toRun
  //  started = 3, //kurz je nastartovan
  //  completed = 4,
  //  skipped = 5, //Preskocen
  //  canceled = 6, //pro archiv testu: test se dostal do archivu operaci cancelTestSkip
  //}

  //public enum taskTypes {
  //  no = 0,
  //  pretest = 1,
  //  test = 2,
  //  course = 3,
  //}

  //public class taskInfo {
  //  public taskTypes type;
  //  public metaTask metaTask;
  //  public taskAttempt task;
  //  public courseAttempt course;
  //  public PretestMeta pretest;
  //  public testAttempt test;
  //}

  //public class metaCourse {
  //  public bool startInfoShowed;
  //  public PretestMeta pretest; //metainformace o pretestu
  //  public int companyId;
  //  public string scormApiUrl;
  //  public string courseId;  //productId, napr. 104
  //  public metaTask[] tasks;
  //  public int elapsedSeconds;
  //  public bool completed;
  //  public int score;
  //}

  //public class metaTask {
  //  public string title;
  //  public string url; //Interface.ts, course.jsonId
  //  public string testFileName; //eTestMe databaze, <Project FileName>/<Test.FileName> (identifikace testu z databaze)
  //  public courseAttempt course;
  //  public testAttempt test;
  //  public string[] removedModules;
  //  public bool resetedForTestFailes;
  //}

  //public class taskAttempt {
  //  public taskStatus status;
  //  public double start; //msec: doplnen pri zmene stavu z toStart na started. Pro kurz a status=skipped: priznak, ze kurz byl pred skip ve stavu started.
  //  public taskAttempt[] archive;
  //  //pro dokonceny pokus:
  //  public double end; //Doplnen z MyModule.End nebo testInfo.RepEnd pri zmene stavu z started do completed
  //  public int score;
  //}

  //public class courseAttempt : taskAttempt {
  //  public int elapsedSeconds;
  //  public int progress;
  //}

  //public class testAttempt : taskAttempt {
  //  public int userTestId; //Licence DB, UserTest.Id. <=0 => test nezalozen
  //  public int interruptions; //pocet preruseni testu
  //}

  //public class PretestMeta : taskAttempt { //metainformace o pretestu
  //  public string result; //identifikace kurzu, ktery je vysledkem pretestu. //Interface.ts, course.id
  //}



  //public class Prod {
  //  public int id;
  //  public string title;
  //  public string path; //path, relativne k q:\LMCom\rew\Web4\Schools\Courses\
  //  public LineIds line;
  //}

  //public class root {
  //  //public int uniqueId;
  //  //public CourseIds courseId;
  //  public string url; //jednoznacna identifikace produktu = (meta)kurzu //@PRODID
  //  public CourseIds pretestCrsId; //identifikace pretestu, no => neni pretest
  //  public LineIds line; //line
  //  public string title;
  //  public string fileName; //path, relativne k q:\LMCom\rew\Web4\Schools\Courses\
  //  public course[] courses;
  //  public grammarNode grammar; //gramatika
  //}

  ///************ GRAMMAR ******************/
  //public class grammarNode : folder {
  //  [DefaultValue(null)]
  //  public grammarNode[] items;
  //  [DefaultValue(CourseIds.no)]
  //  public CourseIds courseId;
  //  [DefaultValue(0)]
  //  public int LMLevel; //level kurzu: anglictina 1..5, euroenglish 1, ostatni 1..3

  //  //kvuli generaci do Typescriptu:
  //  [XmlIgnore, NonSerialized]
  //  public int idx;
  //  [XmlIgnore, NonSerialized]
  //  public int id;
  //}


  //public abstract class folder {
  //  public string title;
  //  public string url;

  //  //kvuli sitemap
  //  [JsonIgnore]
  //  public string spaceId;
  //  [JsonIgnore]
  //  public string globalId;

  //  //kvuli generaci do Typescriptu:
  //  [XmlIgnore, NonSerialized]
  //  public bool isGreen;
  //  [XmlIgnore, NonSerialized]
  //  public bool isCompleted;
  //}

  //public class course : folder {
  //  public string testFileName;
  //  public string level; //textovy popis urovne znalosti, napr. A1-A2
  //  public lesson[] lessons;

  //  //kvuli sitemap
  //  public CourseIds courseId;

  //  //kvuli generaci do Typescriptu:
  //  [XmlIgnore, NonSerialized]
  //  public int modCount; //celkovy pocet modulu
  //  [XmlIgnore, NonSerialized]
  //  public int beg;
  //  [XmlIgnore, NonSerialized]
  //  public int end;
  //  [XmlIgnore, NonSerialized]
  //  public int evalCount; //splnenych modulu
  //  [XmlIgnore, NonSerialized]
  //  public int skipedCount; //preskocenych modulu
  //  [XmlIgnore, NonSerialized]
  //  public bool started; //existuje zacaty modul
  //  [XmlIgnore, NonSerialized]
  //  public int score; //prumerne score u dokoncenych vyhodnotitelnych modulu
  //  [XmlIgnore, NonSerialized]
  //  public int elapsedSeconds;
  //}
  //public class lesson : folder {
  //  public mod[] modules;

  //  //kvuli sitemap
  //  public string globalIdOrderNum;

  //  //pro lekci testMe
  //  public testMe.Skills testSkill;
  //  public int testDurationSec; //cas pro test, ve vterinach

  //  //kvuli generaci do Typescriptu:
  //  [XmlIgnore, NonSerialized]
  //  public int score;
  //  [XmlIgnore, NonSerialized]
  //  public int progress;
  //  [XmlIgnore, NonSerialized]
  //  public int elapsedSeconds;
  //  [XmlIgnore, NonSerialized]
  //  public bool skipped;
  //  [XmlIgnore, NonSerialized]
  //  public int evalCount; //splnenych modulu
  //  [XmlIgnore, NonSerialized]
  //  public int skipedCount; //preskocenych modulu
  //  [XmlIgnore, NonSerialized]
  //  public bool started; //existuje zacaty modul
  //  [XmlIgnore, NonSerialized]
  //  public int beg;
  //  [XmlIgnore, NonSerialized]
  //  public int end;

  //}

  public enum ExFormat {
    ea,
    rew,
  }
  //jedno cviceni v zdrojich kapitoly v q:\LMCom\rew\Web4\Schools\EAData\?\?.txt
  //public class exStatic {
  //  public ExFormat format;
  //  public string title; //titulek
  //  public string url; //identifikace, napr. hueex4_l10_e01

  //  //kvuli generaci do Typescriptu:
  //  [XmlIgnore, NonSerialized]
  //  public string html; //HTML nebo json obsah cviceni
  //  [XmlIgnore, NonSerialized]
  //  public int index; //index cviceni v .TXT souboru pro zjisteni user dat pres initResult.modUser.pages[index]
  //}

  //public class mod : folder {
  //  [DefaultValue(0)]
  //  public int exCount;

  //  //kvuli sitemap
  //  public string globalIdOrderNum;

  //  //pro modul testMe
  //  public int testTake; //kolik se z modulu vezme cviceni pro test. 0..all

  //  //kvuli generaci do Typescriptu:
  //  [XmlIgnore, NonSerialized]
  //  public bool skipped;
    
  //}

  public enum seeAlsoType {
    grammar = 0,
    ex = 1,
  }
  public class seeAlsoLink {
    [XmlAttribute]
    public string url;
    [XmlAttribute]
    public string title;
    [XmlAttribute]
    public seeAlsoType type;
    public static string encode(seeAlsoLink[] links) {
      if (links == null || links.Length == 0) return null;
      return links.Select(l => l.url + "|" + l.title + "|" + ((int)l.type).ToString()).Aggregate((r, i) => r + "#" + i);
    }

  }

  public class ModUser {
    //encoded: string;
    [XmlAttribute]
    public LMComLib.ExerciseStatus st;
    [XmlAttribute, DefaultValue(0)]
    public int ms;
    [XmlAttribute, DefaultValue(0)]
    public int s;
    [XmlAttribute, DefaultValue(0)]
    public long bt;
    [XmlAttribute, DefaultValue(0)]
    public long et;
    [XmlAttribute, DefaultValue(0)]
    public int t;
    [XmlAttribute, DefaultValue(0)]
    public int ev; //NEW: evaluated procent
    public CourseModel.PageUser[] pages;
    //scormData: Object; //Scorm konstanty, pouzivaji se pouze v opravdovem SCORM, v lm.com-EA a REW ne.
  }


  //dump vyukovych dat pro statistiky
  public class dumpDatas {
    public dumpData[] Data;
  }
  public class dumpData {
    [XmlAttribute, DefaultValue(0)]
    public int CompanyId; //companyId chybi pro ScormEx
    [XmlAttribute]
    public string UserId; //identifikace
    [XmlAttribute]
    public string UserInfo; //email, first name, last name, chybi pro ScormEx
    [XmlAttribute]
    public string AttemptId;
    [XmlAttribute]
    public string ModuleId; //napr. english1_xl01_sa_shome_dhtm
    public ModUser ModuleData; //JSON s podrobnymi daty vyuky
  }
  public class dumpRows {
    public dumpRow[] Data;
  }
  public class dumpRow {
    public int Order { get; set; } //jednoznacna identifikace radku, drzi poradi dle exId
    [XmlAttribute, DefaultValue(0)]
    public int CompanyId { get; set; } //companyId chybi pro ScormEx
    [XmlAttribute]
    public string UserId { get; set; } //identifikace
    [XmlAttribute]
    public string UserInfo { get; set; } //email, first name, last name, chybi pro ScormEx
    [XmlAttribute]
    public string AttemptId { get; set; }
    [XmlAttribute]
    public CourseIds course { get; set; }
    [XmlAttribute]
    public int courseExs { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public int coursePercent { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public int courseScore { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public long courseStart { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public long courseEnd { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public int courseSec { get; set; }
    [XmlAttribute]
    public int levelId { get; set; }
    [XmlAttribute]
    public int levelExs { get; set; }
    [XmlAttribute]
    public string level { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public int levelPercent { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public int levelScore { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public long levelStart { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public long levelEnd { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public int levelSec { get; set; }
    [XmlAttribute]
    public int lessonId { get; set; }
    [XmlAttribute]
    public int lessonExs { get; set; }
    [XmlAttribute]
    public string lesson { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public int lessonPercent { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public int lessonScore { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public long lessonStart { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public long lessonEnd { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public int lessonSec { get; set; }
    [XmlAttribute]
    public int modId { get; set; }
    [XmlAttribute]
    public int modExs { get; set; }
    [XmlAttribute]
    public string mod { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public int modPercent { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public int modScore { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public long modStart { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public long modEnd { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public int modSec { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public int exId { get; set; }
    [XmlAttribute]
    public string ex { get; set; }
    [XmlAttribute, DefaultValue(0)]
    public int exScore { get; set; } //dosazene score v procentech. -1 => nevyhodnotitelne
  }

  public class page {
    [XmlAttribute]
    public string title; //titulek cviceni
    [XmlAttribute]
    public string instrTitle; //titulek instrukce
    [XmlAttribute]
    public string[] instrs; //identifikace bodies instrukce
    public seeAlsoLink[] seeAlso; //see also links
    [XmlAttribute]
    public string courseSeeAlsoStr; //see also links z Metadat
    [XmlAttribute,DefaultValue(0)]
    public CourseIds CrsId; //course id
    [XmlAttribute,DefaultValue(false)]
    public bool isPassive; //pasivni stranka bez vyhodnoceni
    [XmlIgnore, NonSerialized]
    public string url; //JS runtime dato, plnene v exercise.ts. Pouzije se v course.ts pro vypocet relativnich odkazu na obrazky apod.
  }
}

namespace Rew
{

    /***************** MyRewise (vsechny moje rewise, nacitane z Licence DB) *********************************/
    public class MyRewiseCmd { //return MyRewise
    public long LMComId;
    public string PCSignature;
    public string DefaultOptionsJSON;
  }

  public class MyRewiseResult {
    public string OptionsJSON; //JSON s MyRewiseOptions, ulozeno v Licence.User.RewiseJSON
    public bool SignatureOK; //vysledek porovnani MyRewiseCmd.PCSignature (z MyRewise.PCSignature) s Licence.User.PCSignature
    public int UserId; //Licence.User.UserId
    public LangToLearn[] ToLearns; //info z Licence.Rewise
  }

  public class MyRewiseOptions { //globalni rewise obtions, ukladano do Licence.User.Rewise
    public LineIds ActLoc; //lokalizace SW
    public LineIds NativeLang; //nativni jazyk
    public MyLineOption[] Lines; //options pro line
  }
  public class MyLineOption { //globalni obtions pro vyucovany jazyk
    public LineIds Line; //uceny jazyk
    public LineIds Loc; //a jeho lokalizace
    public int[] BookIds; //knihy na home slovn iku (plus jejich poradi)
  }

  public class MyRewise { //persistentni dato pro posledni rewise session
    public long LMComId; //LMComUserId, nastavovano na klientovi
    public string PCSignature; //identifikace pocitace, nastavovano na klientovi, porovnavano s Licence.User.PCSignature
    public MyRewiseOptions Options; //ziskano z Licence.User.PCSignature
    public int UserId; //ziskano z Licence.User.UserId
    public LangToLearn[] ToLearns; //info z Licence.Rewise
  }

  public class LangToLearn {
    public Boolean VocabularyEmpty;
    public LineIds Line;
    public LineIds Loc;
  }

  public class SaveFactCmd {
    public int FactId;
    public string FactJSON;
  }

  //AddRewise
  public class AddRewiseCmd {
    public int UserId;
    public LineIds Line;
    public LineIds Loc;
  }

  //DelRewise
  public class DelRewiseCmd {
    public int UserId;
    public LineIds Line;
    public LineIds Loc;
  }

  //SetNativeLang
  public class SetMyOptionsCmd {
    public int UserId;
    public string OptionsJSON; //MyRewiseOptions
  }

  //********************* Moje fakty v rewise (pro kazdou Line a kazdou lokalizaci), nacitane z Licence DB. Cachuji se v Storage **********************
  public class MyFactCmd { //return Licence.RewFacts.JSON, naformatovane do JSON array.
    public int UserId;
    public LineIds Line;
    public LineIds Loc;
  }

  //public class MyFactResult {
  //  public string Facts; //Licence.RewFacts.JSON, naformatovane do JSON array
  //}
  public enum FactStatus {
    no,
    toLearn, //nachystan k uceni
    removed, //nechci se ucit, jeho lekce je ale v myrewise
    deleted, //temporary priznak: fakt odstranen v ramci odstranene lekce
  }
  public class MyFact : FactSrc {
    public int FactId; //Id faktu v Licence.RewFacts tabulce.
    public int LessDbId; //identifikace lekce
    public short Order; //Poradi faktu v LessDbId lekci
    public short NextRep; //datum dalsiho opakovani faktu
    public FactStatus Status;
    public History[] Histories;
  }
  public class History {
    public int Id;
  }

  //****************** Globalni staticka data, pro kazdou lokalizaci jedno (\RewiseAll\rewiseruntime\afrikaans.???.json) ******************
  public class LocSrcCmd {
    public LineIds Loc; //lokalizace
    public long Crc; //version. Pokud je 0, pak jeste neni nacteno
  }

  public class LocSrcResult {
    public LocSrc LocSrc; //null, pokud LocSrcCmd.Crc!=0 a verze souhlasi
    public long Crc; //aktualni verze (v pripade LocSrc!=null)
  }

  //staticka Rewise data pro jednu lokalizaci, ulozena napr. v \RewiseAll\rewiseruntime\afrikaans.???.json. Cachuje se v Storage. Je aktualni, odpovida-li Crc.
  public class LocSrc {
    public Langs Loc; //lokalizace
    //public LineIds Loc; //lokalizace
    //public long Crc; //verze
    public LineSrc[] Lines; //staticka data do lekce vcetne (lines => book groups => books => lessons)
  }
  //staticka Rewise data pro jednu lokalizaci a line, cast LocSrc. Napr. data pro anglictinu
  public class LineSrc {
    public LineIds Line;
    public int Words;
    public int Phrases;
    public int LocCount;
    //public string Learned;
    //public string Removed;
    public BookGroupSrc[] Groups;
  }
  //staticka Rewise data pro jednu skupinu knih, cast LineSrc. Napr. data pro LANGMaster English
  public class BookGroupSrc {
    public int Id; //DbId prvni knihy ve skupine
    public bool IsDefault; //Difotne zobrazit na Vocabulary strance
    public string Title;
    public Rw.CreativeCommonLic Licence;
    public string AdminEMail; //email spravce skupiny knih
    public string Company;
    public int Words;
    public int Phrases;
    public int LocCount;
    //public string Learned;
    //public string Removed;
    public BookSrc[] Books;
  }
  //staticka Rewise data pro jednu knihu, cast BookGroupSrc. Napr. data pro LANGMaster Beginner English
  public class BookSrc {
    public int DbId;
    //public string Name; //projekt/name, napr. rewiselangmaster/cambridge1
    public string Perex;
    public string Title;
    public string ImageUrl;
    public int Words;
    public int Phrases;
    public int LocCount;
    //public string Learned;
    //public string Removed;
    public LessonSrc[] Lessons;
    public string LocAdminEMail; //email lokalizatora
    //public bool HasLoc; //kniha je lokalizovana
  }
  //staticka Rewise data pro jednu lekci bez faktu, cast BookSrc. Napr. data pro 1. lekci LANGMaster Beginner English
  public class LessonSrc {
    public string Title;
    //public string BookName; //<project name>/<book name>
    public int DbId;
    public int Words;
    public int Phrases;
    public int LocCount;
    [NonSerialized]
    public object RewiseSrcFacts;
  }

  //****************** sprava rewise lekci ******************
  //Lekce je globalni staticke dato, pro kazdou lokalizaci a lekci jedno (RewiseLANGMaster\rewiseruntime\english\cambridge1\czech\225.???.json)
  //lekci si uzivatel pridava do Licence.RewFact tabulky

  public class AddLessonCmd { //nacte lekci a prida je do rewise. Vrati //return Licence.RewFacts.JSON, naformatovane do JSON array.
    public int UserId;
    public int DbId; //Id lekce v eTestMe.RwLessons.ID tabulce. Dosazuje se do Licence.RewFact.LessonId
    public string BookName;
    public LineIds Line; //kurz 
    public LineIds Loc; //lokalizace
  }
  public class DelLessonCmd { //vyhodi lekce z rewise
    public int UserId;
    public int DbId; //Id lekce v eTestMe.RwLessons.ID tabulce. Dosazuje se do Licence.RewFact.LessonId
    public LineIds Line; //kurz
    public LineIds Loc; //lokalizace
  }
  public class ReadLessonCmd { //nacte lekci a vrati LessonsDataSrc.
    public int DbId; //Id lekce v eTestMe.RwLessons.ID tabulce. Dosazuje se do Licence.RewFact.LessonId
    public string BookName;
    public LineIds Loc; //lokalizace
  }

  ////****************** Globalni staticke dato, pro kazdou lokalizaci a lekci jedno (RewiseLANGMaster\rewiseruntime\english\cambridge1\czech\225.???.json) ******************
  ////Necachuje se v Storage. Po pridani lekce do uzivatelova Rewise se lekce ulozi do Licence DB a lekce se prida do MyLessons do cache.
  //public class LessonDataSrcCmd { //vrati LessonDataSrc
  //  public int DbId; //ID Lekce
  //  public LineIds Loc; //lokalizace
  //}

  //staticka Rewise data pro jednu lekci s fakty. Ulozena napr. v RewiseLANGMaster\rewiseruntime\english\cambridge1\czech\225.json
  public class LessonDataSrc {
    public int DbId; //id lekce v q:\LMCom\rew\Web4\RwBooks\Texts\
    public FactSrc[] Facts;
  }

  public class FactSrc {
    public Int64 DbId; //id faktu v q:\LMCom\rew\Web4\RwBooks\Texts\
    public FactSound[] Question; //jedna ozvucena veta
    public FactSound[] Answer;
    public FactTypes Type;
  }

  public class FactSound {
    public string Text;
    public string SndKey; //= null => SoundKey je Text, zbaveny zavorek, dvojmezer a lowercase
    public SoundSrcId[] SndSrcs; //=null => neni ozvuceno
  }

  //obsah napr. q:\LMCom\rew\Web4\RwBooks\Runtime\cs-cz\crs2RwMap.json, k jsonId cviceni priradi Crs2RwMapItem s identifikaci Rw lekce a info o lokalizaci lekce
  public class Crs2RwMapItem {
    public int rwId;
    public int locRatioPromile; //kolik promile je lokalizovanych
  }


}

