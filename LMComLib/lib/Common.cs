using LMNetLib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace LMComLib {

  //Objekt, ktery si pro JS serialiazci pamatuje svuj typ
  public class JSTyped {
    public JSTyped() { Type = GetType().Name; }
    [XmlIgnore]
    public string Type; //GetType.FullName
  }

  public enum FillCompanyUserInfoType {
    user = 0,
    company = 1,
    lector = 2
  }

  public enum CompService {
    eduLand = 0,
    LMS = 1,
    eTestMe = 2,
    //eTestMe_eduLand = 10,
  }

  /// <summary>
  /// Role pro lm.com
  /// </summary>
  [Flags]
#if !SILVERLIGHT && !PORTABLE
  [EnumDescrAttribute(typeof(LMComRole), "User=registrovaný,Admin=administrátor,IntranetAdmin=administrátor Intranetu,IntranetUser=uživatel Intranetu")]
#endif
  public enum LMComRole : ulong {
    User = 0,
    Admin = 1,
    IntranetAdmin = 0x2,
    IntranetUser = 0x4,
    eTestMe_Tutor = 0x8,
    highSchool_Partner = 0x10,
    highSchool_PartnerTest = 0x20,
  }

  public class GeneratePdfItem {
    public string Name;
    public string Value;
    public int FontSize;
    public bool IsBold;
  }

  public static class AppConfig {

#if !SILVERLIGHT
    public delegate Stream GetAppConfigStreamEvent();
    public static GetAppConfigStreamEvent onGet;
#endif
    public static XElement Config {
      get {
#if !SILVERLIGHT
        using (Stream str = onGet())
#else
        using (Stream str = Application.GetResourceStream(new Uri("AppConfig.xml", UriKind.Relative)).Stream)
#endif
          return config == null ? config = XElement.Load(str) : config;
      }
    } static XElement config;
    public static Dictionary<string, string> AppSettings {
      get {
        if (appSettings == null) {
          appSettings = new Dictionary<string, string>();
          foreach (XElement el in Config.Element("appSettings").Elements())
            appSettings.Add(el.Attribute("key").Value, el.Attribute("value").Value);
        }
        return appSettings;
      }
    } static Dictionary<string, string> appSettings;
#if !WINDOWS_PHONE
    public static bool IsOutOfBrowser = false;
    public static string StorageBasicPath = null;
    public static int Quota = 0;
#endif
  }

  public enum SoundFormat {
    Wav = 0,
    Flac = 1,
    Raw = 2,
    Mp3 = 3
  }

  public enum AudioMP3BitRate {
    mbrAuto, mbr32, mbr40, mbr48, mbr56, mbr64, mbr80, mbr96,
    mbr112, mbr128, mbr144, mbr160, mbr192, mbr224, mbr256, mbr320
  }


  public enum MP3Quality {
    mp3ql0, mp3ql1, mp3ql2, mp3ql3, mp3ql4, mp3ql5,
    mp3ql6, mp3ql7, mp3ql8, mp3ql9
  }



  public delegate void LMEventHandler<T>(T e);
  public delegate void LMEventHandler();
  public delegate void LMAction<T>(LMEventHandler<T> completed);
  public delegate void LMAction(LMEventHandler completed);


  /// <summary>
  /// identifikace domen, na kterych jsou provozovany lm.com aplikace 
  /// !!!! Musi souhlasit query TDomains v WinLicence.pas.
  /// </summary>

  public static class CommonLib {
    public static XNamespace html = "http://www.w3.org/1999/xhtml";

    public const string OLIhtmlUrl = "http://www.open-learning-initiative.org/html";
    public static XNamespace OLIhtml = OLIhtmlUrl;

    public const string OLIUrl = "http://www.open-learning-initiative.org";
    public const string OLI = OLIUrl;
    //jednoduchy platformove nezavisly string hash (SL, WP7 apod.)
    public static int StringHash(string inStr) {
      int res = 0;
      for (int i = 0; i < inStr.Length; i++) res = 31 * res + (int)inStr[i];
      return res;
    }

    static CommonLib() {
      langTitle.Add(Langs.no, "");
      langTitle.Add(Langs.cs_cz, "Česky");
      langTitle.Add(Langs.de_de, "Deutsch");
      langTitle.Add(Langs.en_gb, "English");
      langTitle.Add(Langs.sk_sk, "Slovensky");
      langTitle.Add(Langs.sp_sp, "Еspañol");
      langTitle.Add(Langs.ru_ru, "Русский");
      langTitle.Add(Langs.it_it, "Italiano");
      langTitle.Add(Langs.fr_fr, "Français");

      langTitle.Add(Langs.pl_pl, "Polski");
      langTitle.Add(Langs.hu_hu, "Magyar");
      langTitle.Add(Langs.el_gr, "Ελληνικά");
      langTitle.Add(Langs.sl_si, "Slovenski");
      langTitle.Add(Langs.nl_nl, "Nederlands");
      langTitle.Add(Langs.tr_tr, "Türkçe");
      langTitle.Add(Langs.pt_pt, "Português");
      langTitle.Add(Langs.vi_vn, "Tiếng Việt");
      langTitle.Add(Langs.uk_ua, "Українська");
      langTitle.Add(Langs.ro_ro, "Română");
      langTitle.Add(Langs.hr_hr, "Hrvatski");
      langTitle.Add(Langs.bg_bg, "Български");
      langTitle.Add(Langs.bs, "Bosenstina, common.cs");
      langTitle.Add(Langs.ca_es, "Català");
      langTitle.Add(Langs.da_dk, "Dansk");
      langTitle.Add(Langs.fi_fi, "Suomi");
      langTitle.Add(Langs.nb_no, "Norsk (bokmål)");
      langTitle.Add(Langs.sv_se, "Svenska");
      langTitle.Add(Langs.ja_jp, "日本語");
      langTitle.Add(Langs.ko_kr, "한국어");
      langTitle.Add(Langs.ar_sa, "العربية");
      langTitle.Add(Langs.he_il, "עברית‏");
      langTitle.Add(Langs.th_th, "ภาษาไทย");
      langTitle.Add(Langs.lv_lv, "Latviešu‏");
      langTitle.Add(Langs.lt_lt, "Lietuvių");
      langTitle.Add(Langs.pt_br, "Português brasileiro");
      langTitle.Add(Langs.zh_hk, "粵語");
      langTitle.Add(Langs.zh_cn, "文言");
      langTitle.Add(Langs.sq_al, "Shqip");
    }

    public static Langs[] allLangs = LowUtils.EnumGetValues<Langs>().ToArray();
    //Male lokalizace
    public static Langs[] smallLocalizations = new Langs[] {
      Langs.cs_cz, Langs.bg_bg, Langs.bs, Langs.de_de, Langs.el_gr, Langs.en_gb, Langs.sp_sp, Langs.fr_fr, Langs.hr_hr, 
      Langs.hu_hu, Langs.it_it, Langs.ja_jp, Langs.ko_kr, Langs.lt_lt, Langs.nl_nl, Langs.pl_pl, Langs.pt_br, 
      Langs.pt_pt, Langs.ro_ro, Langs.ru_ru, Langs.sk_sk, Langs.sl_si, Langs.th_th, Langs.tr_tr, Langs.uk_ua, 
      Langs.vi_vn, Langs.zh_cn, Langs.ar_sa 
    };
    /*public static Langs[] smallLocalizations = new Langs[] { Langs.en_gb, Langs.de_de, Langs.sp_sp, Langs.cs_cz, Langs.sk_sk, Langs.ru_ru, Langs.fr_fr, Langs.it_it, Langs.zh_cn, Langs.ko_kr, 
      Langs.lt_lt, Langs.th_th, Langs.zh_hk, Langs.pl_pl, Langs.tr_tr, Langs.vi_vn };*/
    //Velke lokalizace
    public static Langs[] bigLocalizations = new Langs[] { 
      Langs.en_gb, Langs.de_de, Langs.sp_sp, Langs.cs_cz, Langs.sk_sk, Langs.ru_ru, Langs.fr_fr, 
      Langs.it_it, Langs.pl_pl, Langs.vi_vn, Langs.tr_tr, Langs.lt_lt, Langs.zh_cn,  Langs.bg_bg, Langs.bs,
      Langs.ar_sa 
    };

    //Lingea stredni slovniky
    public static Langs[] CurrentMiddleLangsLingea = new Langs[] { Langs.en_gb, Langs.de_de, Langs.fr_fr, Langs.it_it, Langs.cs_cz, Langs.sk_sk, Langs.ru_ru, Langs.sp_sp }; //aktualni stredni slovniky
    //Lingea male slovniky
    /*public static Langs[] CurrentLangsLingea = new Langs[] {
      Langs.en_gb, Langs.bg_bg, Langs.fr_fr, Langs.hr_hr, Langs.it_it, Langs.hu_hu, Langs.de_de, Langs.nl_nl, Langs.pl_pl, 
      Langs.pt_pt, Langs.ro_ro, Langs.ru_ru, Langs.el_gr, Langs.sl_si, Langs.sp_sp, Langs.tr_tr, Langs.uk_ua, Langs.vi_vn, Langs.cs_cz, 
      Langs.sk_sk, Langs.sq_al, Langs.lt_lt,
      Langs.fi_fi, Langs.sv_se, Langs.da_dk, Langs.nb_no, 
      //Langs.ko_kr, Langs.ja_jp, Langs.pt_br, Langs.th_th, Langs.zh_cn
      //Langs.ca_es, 
    };*/
    //Mame slovniky pro tyto lokalizace
    public static Langs[] CurrentLangsLingeaLocLow = new Langs[] { Langs.bg_bg, Langs.cs_cz, Langs.en_gb, Langs.fr_fr, Langs.de_de, Langs.el_gr, Langs.hr_hr, Langs.hu_hu, Langs.it_it, 
      Langs.lt_lt, Langs.lv_lv, Langs.nl_nl, Langs.pl_pl, Langs.pt_pt, Langs.ro_ro, Langs.ru_ru, Langs.sl_si, Langs.sk_sk, Langs.sp_sp, Langs.sr_latn_cs, Langs.tr_tr, Langs.uk_ua, Langs.vi_vn};
    //nesmi jich ale byt vice nez je malych lokalizaci
    public static Langs[] CurrentLangsLingeaLoc = CurrentLangsLingeaLocLow.Intersect(smallLocalizations).ToArray();
    //mame tyto slovniky
    public static Langs[] CurrentLangsLingeaCrs = new Langs[] { Langs.bg_bg, Langs.sq_al, Langs.ca_es, Langs.cs_cz, Langs.da_dk, Langs.en_gb, Langs.fi_fi, Langs.fr_fr, Langs.de_de, Langs.el_gr, Langs.hr_hr, 
      Langs.hu_hu, Langs.it_it, Langs.lt_lt, Langs.lv_lv, Langs.nl_nl, Langs.nb_no, Langs.pl_pl, Langs.pt_pt, Langs.ro_ro, Langs.ru_ru, Langs.sv_se, Langs.sl_si, Langs.sk_sk, Langs.sp_sp, 
      Langs.sr_latn_cs, Langs.tr_tr, Langs.uk_ua, Langs.vi_vn};

    //public static CourseIds[] allTestTypes = new CourseIds[] { CourseIds.eTestMe_A1, eTestMe_A2, eTestMe_B1, eTestMe_B2, eTestMe_C1, eTestMe_C2, eTestMe_All };


    public static Langs[] allExtendedLangs = new Langs[] { Langs.hu_hu };
    public static Langs[] allPADLangs = new Langs[] { Langs.en_gb, Langs.de_de, Langs.sp_sp, Langs.ru_ru };
    public static CourseIds[] allCourses = new CourseIds[] { CourseIds.English, CourseIds.German, CourseIds.Spanish, CourseIds.Italian, CourseIds.French };
    public static LineIds[] allLines = new LineIds[] { LineIds.English, LineIds.German, LineIds.Spanish, LineIds.Italian, LineIds.French };
    public static LineIds[] allOnlineCourses_Lines = new LineIds[] { LineIds.English, LineIds.German, LineIds.Spanish, LineIds.Italian, LineIds.French, LineIds.Russian };
    public static LineIds[] hasDialogWeek = allLines;
    public static Domains[] allSites = new Domains[] { Domains.sz, Domains.com, Domains.gopas };
    public static Dictionary<Langs, string> langTitle = new Dictionary<Langs, string>();
    public static CourseIds[] allRewises = new CourseIds[] { CourseIds.RewiseEnglish, CourseIds.RewiseGerman, CourseIds.RewiseSpanish, CourseIds.RewiseItalian, CourseIds.RewiseFrench, CourseIds.RewiseRussian };
    public static Langs[] allRewiseLangs = new Langs[] { Langs.en_gb, Langs.de_de, Langs.sp_sp, Langs.cs_cz, Langs.sk_sk, Langs.ru_ru, Langs.it_it, Langs.fr_fr };
    public static LineIds[] allRewiseLines = new LineIds[] { LineIds.English, LineIds.German, LineIds.Spanish, LineIds.Italian, LineIds.French, LineIds.Czech, LineIds.Slovak, LineIds.Russian };
    public static Langs[] allOrgLangs = new Langs[] { Langs.en_gb, Langs.de_de, Langs.sp_sp/*, Langs.fr_fr, Langs.it_it*/ };

    public static CourseIds[] allETCourses = new CourseIds[] { CourseIds.TN_Afrikaans, CourseIds.TN_Albanian, CourseIds.TN_Arabic,  CourseIds.TN_Arabic_Classical, CourseIds.TN_Arabic_Modern_Standard,  
      CourseIds.TN_Armenian, CourseIds.TN_Assamese, CourseIds.TN_Azeri, CourseIds.TN_Basque, CourseIds.TN_Bengali, 
        CourseIds.TN_Breton, CourseIds.TN_Bulgarian, CourseIds.TN_Cantonese, CourseIds.TN_Catalan, CourseIds.TN_Corsican, CourseIds.TN_Croatian, CourseIds.TN_Czech, CourseIds.TN_Danish, 
        CourseIds.TN_Dutch, CourseIds.TN_English, CourseIds.TN_English_American, CourseIds.TN_Estonian, CourseIds.TN_Finnish, 
        CourseIds.TN_French, CourseIds.TN_French_Canadian, CourseIds.TN_Galician, CourseIds.TN_Georgian, CourseIds.TN_German, CourseIds.TN_Greek, CourseIds.TN_Hausa, CourseIds.TN_Hebrew, 
        CourseIds.TN_Hungarian, CourseIds.TN_Chinese_Mandarin, CourseIds.TN_Icelandic, CourseIds.TN_Igbo, CourseIds.TN_Indonesian, 
        CourseIds.TN_Irish, CourseIds.TN_Italian, CourseIds.TN_Japanese, CourseIds.TN_Khmer, CourseIds.TN_Kirghiz, CourseIds.TN_Korean, CourseIds.TN_Latvian, CourseIds.TN_Lithuanian, 
        CourseIds.TN_Macedonian, CourseIds.TN_Malay, CourseIds.TN_Malayalam, CourseIds.TN_Maltese, 
        CourseIds.TN_Maori, CourseIds.TN_Mongolian, CourseIds.TN_Norwegian, CourseIds.TN_Occitan, CourseIds.TN_Pashto, CourseIds.TN_Persian, CourseIds.TN_Polish, CourseIds.TN_Portuguese, 
        CourseIds.TN_Portuguese_Brazilian, CourseIds.TN_Quechua, CourseIds.TN_Romanian, 
        CourseIds.TN_Russian, CourseIds.TN_Serbian, CourseIds.TN_Sesotho, CourseIds.TN_Slovak, CourseIds.TN_Slovenian, CourseIds.TN_Spanish, CourseIds.TN_Spanish_Latin_American, 
        CourseIds.TN_Swahili, CourseIds.TN_Swedish, CourseIds.TN_Thai, CourseIds.TN_Tibetan, CourseIds.TN_Tswana, CourseIds.TN_Turkish, 
        CourseIds.TN_Ukrainian, CourseIds.TN_Urdu, CourseIds.TN_Uzbek, CourseIds.TN_Vietnamese, CourseIds.TN_Xhosa, CourseIds.TN_Yoruba, CourseIds.TN_Zulu };

    public static Langs[] allETLangs = allETCourses.Select(c => CourseIdToLang(c)).Distinct().ToArray();
    public static LineIds[] allETLines = allETCourses.Select(c => CourseIdToLineId(c)).Distinct().ToArray();

    public static CourseIds[] extraETCourses = new CourseIds[] { CourseIds.TN_English_American, CourseIds.TN_Arabic_Classical, CourseIds.TN_Arabic_Modern_Standard, CourseIds.TN_Spanish_Latin_American, CourseIds.TN_French_Canadian };
    public static CourseIds[] allUniqueETCourses = allETCourses.Except(extraETCourses).ToArray();

    public static LineIds[] allCPVLines = new LineIds[] { LineIds.Afrikaans, LineIds.Albanian, LineIds.Arabic, LineIds.Armenian, LineIds.Assamese, LineIds.Azerbaijani, LineIds.Basque, LineIds.Bengali, 
        LineIds.Breton, LineIds.Bulgarian, LineIds.Bossna, LineIds.Cantonese, LineIds.Catalan, LineIds.Corsican, LineIds.Croatian, LineIds.Czech, LineIds.Danish, LineIds.Dutch, LineIds.English, LineIds.Estonian, LineIds.Finnish, 
        LineIds.French, LineIds.Galician, LineIds.Georgian, LineIds.German, LineIds.Greek, LineIds.Hausa, LineIds.Hebrew, LineIds.Hungarian, LineIds.Chinese_Mandarin, LineIds.Icelandic, LineIds.Igbo, LineIds.Indonesian, 
        LineIds.Irish, LineIds.Italian, LineIds.Japanese, LineIds.Khmer, LineIds.Kirghiz, LineIds.Korean, LineIds.Latvian, LineIds.Lithuanian, LineIds.Macedonian, LineIds.Malay, /*LineIds.Malayalam,*/ LineIds.Maltese, 
        LineIds.Maori, LineIds.Mongolian, LineIds.Norwegian, LineIds.Occitan, LineIds.Pashto, LineIds.Persian, LineIds.Polish, LineIds.Portuguese, LineIds.Portuguese_Brazilian, LineIds.Quechua, LineIds.Romanian, 
        LineIds.Russian, LineIds.Serbian, LineIds.Sesotho, LineIds.Slovak, LineIds.Slovenian, LineIds.Spanish, LineIds.Swahili, LineIds.Swedish, LineIds.Thai, LineIds.Tibetan, LineIds.Tswana, LineIds.Turkish, 
        LineIds.Ukrainian, LineIds.Urdu, LineIds.Uzbek, LineIds.Vietnamese, LineIds.Xhosa, LineIds.Yoruba, LineIds.Zulu};
    public static LineIds[] rewiseLocalizations = allCPVLines;
    public static Langs[] rewiseLangs = allCPVLines.Select(l => CommonLib.LineIdToLang(l)).ToArray();

    public static Langs[] hasHelp = bigLocalizations.Except(new Langs[] { Langs.pl_pl }).ToArray();
    public static LineIds[] allLMComLines = allCPVLines;
    public static LineIds[] allCpvLines = allLMComLines.Except(new LineIds[] { LineIds.Armenian, LineIds.Tibetan }).ToArray();

    public static CourseIds[] allETAudio = allETCourses.Select(c => LowUtils.EnumParse<CourseIds>(c.ToString().Replace("TN_", "TN_Audio_"))).ToArray();
    public static CourseIds[] allETCPV = allETCourses.Select(c => LowUtils.EnumParse<CourseIds>(c.ToString().Replace("TN_", "TN_Pronunc_"))).ToArray();
    public static CourseIds[] allETRewise = allETCourses.Select(c => LowUtils.EnumParse<CourseIds>(c.ToString().Replace("TN_", "TN_Rewise_"))).ToArray();

    public static LineIds[] FullLines = allLines;

    public static bool CompanyLang_isOK(Langs lng) {
      switch (lng) {
        case Langs.zh_cn:
        case Langs.vi_vn:
        case Langs.cs_cz:
        case Langs.sk_sk:
        case Langs.en_gb: return true;
        default: return false;
      }
    }

    public static bool NewLangHome_isOK(Langs lng) {
      switch (lng) {
        case Langs.ro_ro:
        case Langs.tr_tr:
        case Langs.vi_vn:
        case Langs.lt_lt:
        case Langs.fr_fr:
        case Langs.ru_ru:
        case Langs.bg_bg:
        case Langs.de_de:
        case Langs.it_it:
        case Langs.cs_cz:
        case Langs.en_gb:
        case Langs.sk_sk:
        case Langs.sp_sp:
        case Langs.uk_ua:
        case Langs.zh_cn:
        case Langs.ja_jp:
        case Langs.ko_kr:
        case Langs.th_th:
        case Langs.pt_br:
        case Langs.pt_pt:
        case Langs.nl_nl:
        case Langs.hr_hr:
          return true;
        default:
          return false;
      }
    }

    public static CourseIds crsIdToCrsGroup(CourseIds crsId) {
      switch (crsId) {
        case CourseIds.IsEduLand_EuroEnglish:
        case CourseIds.IsEduLand_Other: return crsId;
        case CourseIds.EuroEnglish: return CourseIds.IsEduLand_EuroEnglish;
        case CourseIds.eTestMe_EnglishBig: return CourseIds.eTestMeBig;
        case CourseIds.eTestMe_EnglishSmall: return CourseIds.eTestMeSmall;
        default: return CourseIds.IsEduLand_Other;
      }
    }

    static string langToLingeaLangLow(Langs lng) {
      switch (lng) {
        //case Langs.en_nz: return "en";
        case Langs.cs_cz: return "cz";
        case Langs.de_de: return "ge";
        case Langs.el_gr: return "gr";
        case Langs.sp_sp: return "sp";
        case Langs.ja_jp: return "jp";
        case Langs.uk_ua: return "ua";
        case Langs.vi_vn: return "vn";
        case Langs.sl_si: return "si";
        case Langs.zh_cn: return "cn";
        case Langs.sq_al: return "al";
        case Langs.ca_es: return "ct";
        case Langs.da_dk: return "dk";
        case Langs.sv_se: return "se";
        case Langs.nb_no: return "no";
        case Langs.pt_pt: return "pt";
        default: return null;
      }
    }
    public static string langToLingeaLang(Langs lng) {
      string res = langToLingeaLangLow(lng);
      return res == null ? htmlLang(lng) : res;
    }
    public static Langs lingeaLangToLang(string ling) {
      Langs res = LowUtils.EnumGetValues<Langs>().FirstOrDefault(l => langToLingeaLangLow(l) == ling);
      return res == Langs.no ? LowUtils.EnumGetValues<Langs>().FirstOrDefault(l => htmlLang(l) == ling) : res;
    }

    public static string[] demoModules = new string[] {
      "english1/l08/a",
      "english2/l04/a",
      "english3/l03/b",
      "english4/l12/d",
      "english5/l02/a",
      "french1/l04/a",
      "french2/l04/a",
      "french3/l08/b",
      "spanish1/l04/b",
      "spanish2/l04/a",
      "spanish3/l07/b",
      "italian1/l04/a",
      "italian2/l08/a",
      "italian3/l08/a",
      "russian1/lesson4/chaptera",
      "russian2/lesson1/chapterc",
      "russian3/lesson1/chaptera",
      "german1/les4/chapc",
      "german2/les3/chapb",
      "german3/les1/chapa",
    }.Select(s => s.ToLowerInvariant()).ToArray();


    public static LineIds[] MiddleLines = CurrentLangsLingeaCrs.Select(l => CommonLib.LangToLineId(l)).Where(l => !FullLines.Contains(l)).ToArray(); //kurzy, ke kterym mame maly slovnik Lingea

    public static Langs[] FullLangsLingea = new Langs[] {Langs.en_gb, Langs.bg_bg, Langs.bs, Langs.fr_fr, Langs.hr_hr, Langs.it_it, Langs.ca_es, Langs.hu_hu, 
        Langs.de_de, Langs.nl_nl,   Langs.pl_pl, Langs.pt_pt, Langs.ro_ro, Langs.ru_ru, Langs.el_gr, Langs.sl_si, Langs.sp_sp, Langs.tr_tr, Langs.uk_ua, 
        Langs.vi_vn, Langs.cs_cz, Langs.sk_sk,
        Langs.fi_fi, Langs.sv_se, Langs.da_dk, Langs.nb_no, Langs.sq_al, Langs.ja_jp, Langs.pt_br, Langs.zh_cn, Langs.ko_kr, Langs.ar_sa, Langs.he_il, Langs.th_th, 
        Langs.lv_lv, Langs.lt_lt, Langs.mk_mk, Langs.sr_latn_cs}; //vsechny soucasne planovane i

    public static Langs[] FullMiddleLangsLingea = new Langs[] {Langs.en_gb, Langs.fr_fr, Langs.it_it, Langs.hu_hu, Langs.de_de, Langs.pl_pl, Langs.cs_cz, Langs.sk_sk, Langs.bg_bg, Langs.hr_hr, 
      Langs.ro_ro, Langs.ru_ru, Langs.sr_latn_cs, Langs.uk_ua, Langs.sp_sp, Langs.nl_nl}; //vsechny soucasen i planovane stredni slovniky



    /*public static Langs[] LingeaLocalizationLangs = new Langs[] {Langs.en_gb, Langs.fr_fr, Langs.it_it, Langs.hu_hu, Langs.de_de, Langs.pl_pl, Langs.cs_cz,
        Langs.sk_sk, Langs.bg_bg, Langs.hr_hr, Langs.ro_ro, Langs.ru_ru, Langs.uk_ua, Langs.sp_sp, Langs.nl_nl, 
        Langs.el_gr, Langs.pt_pt, Langs.sl_si, Langs.tr_tr, Langs.vi_vn, Langs.lt_lt,
        //Langs.ko_kr, Langs.ja_jp, Langs.pt_br, Langs.th_th, Langs.zh_cn
    }; //rodny jazyk, ke kteremu mame slovnik a specializovane lokalizovany kurz*/

    //public static Langs[] smallLocalizations = LingeaLocalizationLangs;

    public const byte MaxCourseCount = 5;
    public const byte WrongIndex = 255;
    public static IEnumerable<byte> CourseIndexes() {
      for (byte b = 0; b < MaxCourseCount; b++) yield return b;
    }
    public static byte CourseIndex(CourseIds crsId) {
      switch (crsId) {
        case CourseIds.English: return 0;
        case CourseIds.German: return 1;
        case CourseIds.Spanish: return 2;
        case CourseIds.Italian: return 3;
        case CourseIds.French: return 4;
        default: return WrongIndex;
      }
    }

    //Duplicity: Langs[] dupls = new Langs[] { Langs.en_us, Langs.fr_ca, Langs.es_mx, Langs.pt_br, Langs.zh_hk };
    public static string htmlLang(Langs lng) {
      switch (lng) {
        case Langs.cs_cz: return "cs";
        case Langs.de_de: return "de";
        case Langs.en_gb: return "en";
        case Langs.sp_sp: return "es";
        case Langs.sk_sk: return "sk";
        case Langs.vi_vn: return "vi";
        case Langs.ru_ru: return "ru";
        case Langs.fr_fr: return "fr";
        case Langs.it_it: return "it";
        #region EuroTalk6
        case Langs.fi_fi: return "fi";
        case Langs.sv_se: return "sv";
        case Langs.da_dk: return "da";
        case Langs.nb_no: return "no";
        case Langs.af_za: return "af";
        case Langs.sq_al: return "sq";
        case Langs.ar_sa: return "ar";
        case Langs.hy_am: return "hy";
        case Langs.as_in: return "as";
        case Langs.az_latn_az: return "az";
        case Langs.eu_es: return "eu";
        case Langs.bn_in: return "bn";
        case Langs.be_by: return "be";
        case Langs.pt_br: return "pt";
        case Langs.br_fr: return "br";
        case Langs.bg_bg: return "bg";
        case Langs.bs: return "bs";
        case Langs.fr_ca: return "fr";
        case Langs.zh_hk: return "zh";
        case Langs.ca_es: return "ca";
        case Langs.co_fr: return "co";
        case Langs.hr_hr: return "hr";
        case Langs.nl_nl: return "nl";
        case Langs.en_us: return "en";
        case Langs.et_ee: return "et";
        case Langs.gl_es: return "gl";
        case Langs.ka_ge: return "ka";
        case Langs.el_gr: return "el";
        case Langs.gu_in: return "gu";
        case Langs.ha_latn_ng: return "ha";
        case Langs.he_il: return "iw";
        case Langs.hi_in: return "hi";
        case Langs.hu_hu: return "hu";
        case Langs.zh_cn: return "zh-CN";
        case Langs.is_is: return "is";
        case Langs.ig_ng: return "ig";
        case Langs.id_id: return "id";
        case Langs.ga_ie: return "ga";
        case Langs.ja_jp: return "ja";
        case Langs.kn_in: return "kn";
        case Langs.km_kh: return "km";
        case Langs.ky_kg: return "ky";
        case Langs.ko_kr: return "ko";
        case Langs.lo_la: return "lo";
        case Langs.es_mx: return "es";
        case Langs.lv_lv: return "lv";
        case Langs.lt_lt: return "lt";
        case Langs.mk_mk: return "mk";
        case Langs.ms_my: return "ms";
        case Langs.ml_in: return "ml";
        case Langs.mt_mt: return "mt";
        case Langs.mi_nz: return "mi";
        case Langs.mr_in: return "mr";
        case Langs.mn_mn: return "mn";
        case Langs.ne_np: return "ne";
        case Langs.oc_fr: return "oc";
        case Langs.ps_af: return "ps";
        case Langs.fa_ir: return "fa";
        case Langs.pl_pl: return "pl";
        case Langs.pt_pt: return "pt";
        case Langs.pa_in: return "pa";
        case Langs.quz_pe: return "qu";
        case Langs.ro_ro: return "ro";
        case Langs.sr_latn_cs: return "sr";
        case Langs.nso_za: return "ns";
        case Langs.si_lk: return "si";
        case Langs.sl_si: return "sl";
        case Langs.sw_ke: return "sw";
        case Langs.ta_in: return "ta";
        case Langs.te_in: return "te";
        case Langs.th_th: return "th";
        case Langs.bo_cn: return "bo";
        case Langs.tn_za: return "tn";
        case Langs.tr_tr: return "tr";
        case Langs.uk_ua: return "uk";
        case Langs.ur_pk: return "ur";
        case Langs.uz_latn_uz: return "uz";
        case Langs.cy_gb: return "cy";
        case Langs.xh_za: return "xh";
        case Langs.yo_ng: return "yo";
        case Langs.zu_za: return "zu";
        #endregion
        //default: throw new Exception("Missing code here!");
        default: return null;
      }
    }

    public static CourseIds LineIdToCourseId(LineIds crs) {
      switch (crs) {
        case LineIds.English: return CourseIds.English;
        case LineIds.German: return CourseIds.German;
        case LineIds.Spanish: return CourseIds.Spanish;
        case LineIds.French: return CourseIds.French;
        case LineIds.Italian: return CourseIds.Italian;
        case LineIds.Russian: return CourseIds.Russian;
        default: return CourseIds.no;
      }
    }

    public static LineIds CourseIdToLineId(CourseIds crs) {
      switch (crs) {
        //
        case CourseIds.English:
        case CourseIds.EnglishE:
        case CourseIds.EuroEnglish:
        case CourseIds.EnglishBerlitz:
        case CourseIds.Elements:
        case CourseIds.Millenium:
        case CourseIds.EngAct:
        case CourseIds.Toefl:
        case CourseIds.DictEn_Cs:
        case CourseIds.DictEn_Sk:
        case CourseIds.DictEn:
        case CourseIds.DictEn_De:
        case CourseIds.DictEn_Fr:
        case CourseIds.DictEn_Sp:
        case CourseIds.DictEn_It:
        case CourseIds.DictEn_Ru:
        case CourseIds.ElementsAndTest:
        case CourseIds.eTestMe_EnglishBig:
        case CourseIds.eTestMe_EnglishSmall:
        case CourseIds.VNEng3:
        case CourseIds.VNEng4:
        case CourseIds.VNEng5:
        case CourseIds.VNEng6:
        case CourseIds.VNEng7:
        case CourseIds.VNEng8:
        case CourseIds.VNEng9:
        case CourseIds.VNEng10:
        case CourseIds.VNEng11:
        case CourseIds.VNEng12:
          //case CourseIds.Pronunc_English:
          return LineIds.English;
        case CourseIds.German:
        case CourseIds.GermanBerlitz:
        case CourseIds.Tangram:
        case CourseIds.DictDe_Cs:
        case CourseIds.DictDe_Sk:
        case CourseIds.DictDe_En:
        case CourseIds.DictDe_Fr:
        case CourseIds.DictDe_Sp:
        case CourseIds.DictDe_It:
          //case CourseIds.Pronunc_German:
          return LineIds.German;
        case CourseIds.Spanish:
        case CourseIds.SpanishBerlitz:
        case CourseIds.Mirada:
        case CourseIds.DictSp_Cs:
        case CourseIds.DictSp_Sk:
        case CourseIds.DictSp_En:
        case CourseIds.DictSp_De:
          return LineIds.Spanish;
        case CourseIds.French:
        case CourseIds.FrenchBerlitz:
        case CourseIds.Facettes:
        case CourseIds.DictFr_Cs:
        case CourseIds.DictFr_Sk:
        case CourseIds.DictFr_En:
        case CourseIds.DictFr_De:
          return LineIds.French;
        case CourseIds.Italian:
        case CourseIds.ItalianBerlitz:
        case CourseIds.Espresso:
        case CourseIds.DictIt_En:
        case CourseIds.DictIt_De:
          return LineIds.Italian;
        case CourseIds.Russian:
        case CourseIds.RussianBerlitz:
        case CourseIds.DictRu_Cs:
        case CourseIds.DictRu_Sk:
        case CourseIds.DictRu_En:
          return LineIds.Russian;
        #region EuroTalk5
        case CourseIds.TN_Afrikaans: return LineIds.Afrikaans;
        case CourseIds.TN_Albanian: return LineIds.Albanian;
        case CourseIds.TN_Arabic: return LineIds.Arabic;
        case CourseIds.TN_Arabic_Classical: return LineIds.Arabic;
        case CourseIds.TN_Arabic_Modern_Standard: return LineIds.Arabic;
        case CourseIds.TN_Armenian: return LineIds.Armenian;
        case CourseIds.TN_Assamese: return LineIds.Assamese;
        case CourseIds.TN_Azeri: return LineIds.Azerbaijani;
        case CourseIds.TN_Basque: return LineIds.Basque;
        case CourseIds.TN_Bengali: return LineIds.Bengali;
        case CourseIds.TN_Portuguese_Brazilian: return LineIds.Portuguese_Brazilian;
        case CourseIds.TN_Breton: return LineIds.Breton;
        case CourseIds.TN_Bulgarian: return LineIds.Bulgarian;
        case CourseIds.TN_Bossna: return LineIds.Bossna;
        case CourseIds.TN_French_Canadian: return LineIds.French;
        case CourseIds.TN_Cantonese: return LineIds.Cantonese;
        case CourseIds.TN_Catalan: return LineIds.Catalan;
        case CourseIds.TN_Corsican: return LineIds.Corsican;
        case CourseIds.TN_Croatian: return LineIds.Croatian;
        case CourseIds.TN_Czech: return LineIds.Czech;
        case CourseIds.TN_Danish: return LineIds.Danish;
        case CourseIds.TN_Dutch: return LineIds.Dutch;
        case CourseIds.TN_English: return LineIds.English;
        case CourseIds.TN_English_American: return LineIds.English;
        case CourseIds.TN_Estonian: return LineIds.Estonian;
        case CourseIds.TN_Finnish: return LineIds.Finnish;
        case CourseIds.TN_French: return LineIds.French;
        case CourseIds.TN_Galician: return LineIds.Galician;
        case CourseIds.TN_Georgian: return LineIds.Georgian;
        case CourseIds.TN_German: return LineIds.German;
        case CourseIds.TN_Greek: return LineIds.Greek;
        case CourseIds.TN_Hausa: return LineIds.Hausa;
        case CourseIds.TN_Hebrew: return LineIds.Hebrew;
        case CourseIds.TN_Hungarian: return LineIds.Hungarian;
        case CourseIds.TN_Chinese_Mandarin: return LineIds.Chinese_Mandarin;
        case CourseIds.TN_Icelandic: return LineIds.Icelandic;
        case CourseIds.TN_Igbo: return LineIds.Igbo;
        case CourseIds.TN_Indonesian: return LineIds.Indonesian;
        case CourseIds.TN_Irish: return LineIds.Irish;
        case CourseIds.TN_Italian: return LineIds.Italian;
        case CourseIds.TN_Japanese: return LineIds.Japanese;
        case CourseIds.TN_Khmer: return LineIds.Khmer;
        case CourseIds.TN_Kirghiz: return LineIds.Kirghiz;
        case CourseIds.TN_Korean: return LineIds.Korean;
        case CourseIds.TN_Spanish_Latin_American: return LineIds.Spanish;
        case CourseIds.TN_Latvian: return LineIds.Latvian;
        case CourseIds.TN_Lithuanian: return LineIds.Lithuanian;
        case CourseIds.TN_Macedonian: return LineIds.Macedonian;
        case CourseIds.TN_Malay: return LineIds.Malay;
        case CourseIds.TN_Malayalam: return LineIds.Malayalam;
        case CourseIds.TN_Maltese: return LineIds.Maltese;
        case CourseIds.TN_Maori: return LineIds.Maori;
        case CourseIds.TN_Mongolian: return LineIds.Mongolian;
        case CourseIds.TN_Occitan: return LineIds.Occitan;
        case CourseIds.TN_Norwegian: return LineIds.Norwegian;
        case CourseIds.TN_Pashto: return LineIds.Pashto;
        case CourseIds.TN_Persian: return LineIds.Persian;
        case CourseIds.TN_Polish: return LineIds.Polish;
        case CourseIds.TN_Portuguese: return LineIds.Portuguese;
        case CourseIds.TN_Quechua: return LineIds.Quechua;
        case CourseIds.TN_Romanian: return LineIds.Romanian;
        case CourseIds.TN_Russian: return LineIds.Russian;
        case CourseIds.TN_Serbian: return LineIds.Serbian;
        case CourseIds.TN_Sesotho: return LineIds.Sesotho;
        case CourseIds.TN_Slovak: return LineIds.Slovak;
        case CourseIds.TN_Slovenian: return LineIds.Slovenian;
        case CourseIds.TN_Spanish: return LineIds.Spanish;
        case CourseIds.TN_Swahili: return LineIds.Swahili;
        case CourseIds.TN_Swedish: return LineIds.Swedish;
        case CourseIds.TN_Thai: return LineIds.Thai;
        case CourseIds.TN_Tibetan: return LineIds.Tibetan;
        case CourseIds.TN_Tswana: return LineIds.Tswana;
        case CourseIds.TN_Turkish: return LineIds.Turkish;
        case CourseIds.TN_Ukrainian: return LineIds.Ukrainian;
        case CourseIds.TN_Urdu: return LineIds.Urdu;
        case CourseIds.TN_Uzbek: return LineIds.Uzbek;
        case CourseIds.TN_Vietnamese: return LineIds.Vietnamese;
        case CourseIds.TN_Xhosa: return LineIds.Xhosa;
        case CourseIds.TN_Yoruba: return LineIds.Yoruba;
        case CourseIds.TN_Zulu: return LineIds.Zulu;
        case CourseIds.TN_Audio_Afrikaans: return LineIds.Afrikaans;
        case CourseIds.TN_Audio_Albanian: return LineIds.Albanian;
        case CourseIds.TN_Audio_Arabic: return LineIds.Arabic;
        case CourseIds.TN_Audio_Arabic_Classical: return LineIds.Arabic;
        case CourseIds.TN_Audio_Arabic_Modern_Standard: return LineIds.Arabic;
        case CourseIds.TN_Audio_Armenian: return LineIds.Armenian;
        case CourseIds.TN_Audio_Assamese: return LineIds.Assamese;
        case CourseIds.TN_Audio_Azeri: return LineIds.Azerbaijani;
        case CourseIds.TN_Audio_Basque: return LineIds.Basque;
        case CourseIds.TN_Audio_Bengali: return LineIds.Bengali;
        case CourseIds.TN_Audio_Portuguese_Brazilian: return LineIds.Portuguese_Brazilian;
        case CourseIds.TN_Audio_Breton: return LineIds.Breton;
        case CourseIds.TN_Audio_Bulgarian: return LineIds.Bulgarian;
        case CourseIds.TN_Audio_Bossna: return LineIds.Bossna;
        case CourseIds.TN_Audio_French_Canadian: return LineIds.French;
        case CourseIds.TN_Audio_Cantonese: return LineIds.Cantonese;
        case CourseIds.TN_Audio_Catalan: return LineIds.Catalan;
        case CourseIds.TN_Audio_Corsican: return LineIds.Corsican;
        case CourseIds.TN_Audio_Croatian: return LineIds.Croatian;
        case CourseIds.TN_Audio_Czech: return LineIds.Czech;
        case CourseIds.TN_Audio_Danish: return LineIds.Danish;
        case CourseIds.TN_Audio_Dutch: return LineIds.Dutch;
        case CourseIds.TN_Audio_English: return LineIds.English;
        case CourseIds.TN_Audio_English_American: return LineIds.English;
        case CourseIds.TN_Audio_Estonian: return LineIds.Estonian;
        case CourseIds.TN_Audio_Finnish: return LineIds.Finnish;
        case CourseIds.TN_Audio_French: return LineIds.French;
        case CourseIds.TN_Audio_Galician: return LineIds.Galician;
        case CourseIds.TN_Audio_Georgian: return LineIds.Georgian;
        case CourseIds.TN_Audio_German: return LineIds.German;
        case CourseIds.TN_Audio_Greek: return LineIds.Greek;
        case CourseIds.TN_Audio_Hausa: return LineIds.Hausa;
        case CourseIds.TN_Audio_Hebrew: return LineIds.Hebrew;
        case CourseIds.TN_Audio_Hungarian: return LineIds.Hungarian;
        case CourseIds.TN_Audio_Chinese_Mandarin: return LineIds.Chinese_Mandarin;
        case CourseIds.TN_Audio_Icelandic: return LineIds.Icelandic;
        case CourseIds.TN_Audio_Igbo: return LineIds.Igbo;
        case CourseIds.TN_Audio_Indonesian: return LineIds.Indonesian;
        case CourseIds.TN_Audio_Irish: return LineIds.Irish;
        case CourseIds.TN_Audio_Italian: return LineIds.Italian;
        case CourseIds.TN_Audio_Japanese: return LineIds.Japanese;
        case CourseIds.TN_Audio_Khmer: return LineIds.Khmer;
        case CourseIds.TN_Audio_Kirghiz: return LineIds.Kirghiz;
        case CourseIds.TN_Audio_Korean: return LineIds.Korean;
        case CourseIds.TN_Audio_Spanish_Latin_American: return LineIds.Spanish;
        case CourseIds.TN_Audio_Latvian: return LineIds.Latvian;
        case CourseIds.TN_Audio_Lithuanian: return LineIds.Lithuanian;
        case CourseIds.TN_Audio_Macedonian: return LineIds.Macedonian;
        case CourseIds.TN_Audio_Malay: return LineIds.Malay;
        case CourseIds.TN_Audio_Malayalam: return LineIds.Malayalam;
        case CourseIds.TN_Audio_Maltese: return LineIds.Maltese;
        case CourseIds.TN_Audio_Maori: return LineIds.Maori;
        case CourseIds.TN_Audio_Mongolian: return LineIds.Mongolian;
        case CourseIds.TN_Audio_Occitan: return LineIds.Occitan;
        case CourseIds.TN_Audio_Norwegian: return LineIds.Norwegian;
        case CourseIds.TN_Audio_Pashto: return LineIds.Pashto;
        case CourseIds.TN_Audio_Persian: return LineIds.Persian;
        case CourseIds.TN_Audio_Polish: return LineIds.Polish;
        case CourseIds.TN_Audio_Portuguese: return LineIds.Portuguese;
        case CourseIds.TN_Audio_Quechua: return LineIds.Quechua;
        case CourseIds.TN_Audio_Romanian: return LineIds.Romanian;
        case CourseIds.TN_Audio_Russian: return LineIds.Russian;
        case CourseIds.TN_Audio_Serbian: return LineIds.Serbian;
        case CourseIds.TN_Audio_Sesotho: return LineIds.Sesotho;
        case CourseIds.TN_Audio_Slovak: return LineIds.Slovak;
        case CourseIds.TN_Audio_Slovenian: return LineIds.Slovenian;
        case CourseIds.TN_Audio_Spanish: return LineIds.Spanish;
        case CourseIds.TN_Audio_Swahili: return LineIds.Swahili;
        case CourseIds.TN_Audio_Swedish: return LineIds.Swedish;
        case CourseIds.TN_Audio_Thai: return LineIds.Thai;
        case CourseIds.TN_Audio_Tibetan: return LineIds.Tibetan;
        case CourseIds.TN_Audio_Tswana: return LineIds.Tswana;
        case CourseIds.TN_Audio_Turkish: return LineIds.Turkish;
        case CourseIds.TN_Audio_Ukrainian: return LineIds.Ukrainian;
        case CourseIds.TN_Audio_Urdu: return LineIds.Urdu;
        case CourseIds.TN_Audio_Uzbek: return LineIds.Uzbek;
        case CourseIds.TN_Audio_Vietnamese: return LineIds.Vietnamese;
        case CourseIds.TN_Audio_Xhosa: return LineIds.Xhosa;
        case CourseIds.TN_Audio_Yoruba: return LineIds.Yoruba;
        case CourseIds.TN_Audio_Zulu: return LineIds.Zulu;
        case CourseIds.TN_Pronunc_Afrikaans: return LineIds.Afrikaans;
        case CourseIds.TN_Pronunc_Albanian: return LineIds.Albanian;
        case CourseIds.TN_Pronunc_Arabic: return LineIds.Arabic;
        case CourseIds.TN_Pronunc_Arabic_Classical: return LineIds.Arabic;
        case CourseIds.TN_Pronunc_Arabic_Modern_Standard: return LineIds.Arabic;
        case CourseIds.TN_Pronunc_Armenian: return LineIds.Armenian;
        case CourseIds.TN_Pronunc_Assamese: return LineIds.Assamese;
        case CourseIds.TN_Pronunc_Azeri: return LineIds.Azerbaijani;
        case CourseIds.TN_Pronunc_Basque: return LineIds.Basque;
        case CourseIds.TN_Pronunc_Bengali: return LineIds.Bengali;
        case CourseIds.TN_Pronunc_Portuguese_Brazilian: return LineIds.Portuguese_Brazilian;
        case CourseIds.TN_Pronunc_Breton: return LineIds.Breton;
        case CourseIds.TN_Pronunc_Bulgarian: return LineIds.Bulgarian;
        case CourseIds.TN_Pronunc_French_Canadian: return LineIds.French;
        case CourseIds.TN_Pronunc_Cantonese: return LineIds.Cantonese;
        case CourseIds.TN_Pronunc_Catalan: return LineIds.Catalan;
        case CourseIds.TN_Pronunc_Corsican: return LineIds.Corsican;
        case CourseIds.TN_Pronunc_Croatian: return LineIds.Croatian;
        case CourseIds.TN_Pronunc_Czech: return LineIds.Czech;
        case CourseIds.TN_Pronunc_Danish: return LineIds.Danish;
        case CourseIds.TN_Pronunc_Dutch: return LineIds.Dutch;
        case CourseIds.TN_Pronunc_English: return LineIds.English;
        case CourseIds.TN_Pronunc_English_American: return LineIds.English;
        case CourseIds.TN_Pronunc_Estonian: return LineIds.Estonian;
        case CourseIds.TN_Pronunc_Finnish: return LineIds.Finnish;
        case CourseIds.TN_Pronunc_French: return LineIds.French;
        case CourseIds.TN_Pronunc_Galician: return LineIds.Galician;
        case CourseIds.TN_Pronunc_Georgian: return LineIds.Georgian;
        case CourseIds.TN_Pronunc_German: return LineIds.German;
        case CourseIds.TN_Pronunc_Greek: return LineIds.Greek;
        case CourseIds.TN_Pronunc_Hausa: return LineIds.Hausa;
        case CourseIds.TN_Pronunc_Hebrew: return LineIds.Hebrew;
        case CourseIds.TN_Pronunc_Hungarian: return LineIds.Hungarian;
        case CourseIds.TN_Pronunc_Chinese_Mandarin: return LineIds.Chinese_Mandarin;
        case CourseIds.TN_Pronunc_Icelandic: return LineIds.Icelandic;
        case CourseIds.TN_Pronunc_Igbo: return LineIds.Igbo;
        case CourseIds.TN_Pronunc_Indonesian: return LineIds.Indonesian;
        case CourseIds.TN_Pronunc_Irish: return LineIds.Irish;
        case CourseIds.TN_Pronunc_Italian: return LineIds.Italian;
        case CourseIds.TN_Pronunc_Japanese: return LineIds.Japanese;
        case CourseIds.TN_Pronunc_Khmer: return LineIds.Khmer;
        case CourseIds.TN_Pronunc_Kirghiz: return LineIds.Kirghiz;
        case CourseIds.TN_Pronunc_Korean: return LineIds.Korean;
        case CourseIds.TN_Pronunc_Spanish_Latin_American: return LineIds.Spanish;
        case CourseIds.TN_Pronunc_Latvian: return LineIds.Latvian;
        case CourseIds.TN_Pronunc_Lithuanian: return LineIds.Lithuanian;
        case CourseIds.TN_Pronunc_Macedonian: return LineIds.Macedonian;
        case CourseIds.TN_Pronunc_Malay: return LineIds.Malay;
        case CourseIds.TN_Pronunc_Malayalam: return LineIds.Malayalam;
        case CourseIds.TN_Pronunc_Maltese: return LineIds.Maltese;
        case CourseIds.TN_Pronunc_Maori: return LineIds.Maori;
        case CourseIds.TN_Pronunc_Mongolian: return LineIds.Mongolian;
        case CourseIds.TN_Pronunc_Occitan: return LineIds.Occitan;
        case CourseIds.TN_Pronunc_Norwegian: return LineIds.Norwegian;
        case CourseIds.TN_Pronunc_Pashto: return LineIds.Pashto;
        case CourseIds.TN_Pronunc_Persian: return LineIds.Persian;
        case CourseIds.TN_Pronunc_Polish: return LineIds.Polish;
        case CourseIds.TN_Pronunc_Portuguese: return LineIds.Portuguese;
        case CourseIds.TN_Pronunc_Quechua: return LineIds.Quechua;
        case CourseIds.TN_Pronunc_Romanian: return LineIds.Romanian;
        case CourseIds.TN_Pronunc_Russian: return LineIds.Russian;
        case CourseIds.TN_Pronunc_Serbian: return LineIds.Serbian;
        case CourseIds.TN_Pronunc_Sesotho: return LineIds.Sesotho;
        case CourseIds.TN_Pronunc_Slovak: return LineIds.Slovak;
        case CourseIds.TN_Pronunc_Slovenian: return LineIds.Slovenian;
        case CourseIds.TN_Pronunc_Spanish: return LineIds.Spanish;
        case CourseIds.TN_Pronunc_Swahili: return LineIds.Swahili;
        case CourseIds.TN_Pronunc_Swedish: return LineIds.Swedish;
        case CourseIds.TN_Pronunc_Thai: return LineIds.Thai;
        case CourseIds.TN_Pronunc_Tibetan: return LineIds.Tibetan;
        case CourseIds.TN_Pronunc_Tswana: return LineIds.Tswana;
        case CourseIds.TN_Pronunc_Turkish: return LineIds.Turkish;
        case CourseIds.TN_Pronunc_Ukrainian: return LineIds.Ukrainian;
        case CourseIds.TN_Pronunc_Urdu: return LineIds.Urdu;
        case CourseIds.TN_Pronunc_Uzbek: return LineIds.Uzbek;
        case CourseIds.TN_Pronunc_Vietnamese: return LineIds.Vietnamese;
        case CourseIds.TN_Pronunc_Xhosa: return LineIds.Xhosa;
        case CourseIds.TN_Pronunc_Yoruba: return LineIds.Yoruba;
        case CourseIds.TN_Pronunc_Zulu: return LineIds.Zulu;
        case CourseIds.TN_Rewise_Afrikaans: return LineIds.Afrikaans;
        case CourseIds.TN_Rewise_Albanian: return LineIds.Albanian;
        case CourseIds.TN_Rewise_Arabic: return LineIds.Arabic;
        case CourseIds.TN_Rewise_Arabic_Classical: return LineIds.Arabic;
        case CourseIds.TN_Rewise_Arabic_Modern_Standard: return LineIds.Arabic;
        case CourseIds.TN_Rewise_Armenian: return LineIds.Armenian;
        case CourseIds.TN_Rewise_Assamese: return LineIds.Assamese;
        case CourseIds.TN_Rewise_Azeri: return LineIds.Azerbaijani;
        case CourseIds.TN_Rewise_Basque: return LineIds.Basque;
        case CourseIds.TN_Rewise_Bengali: return LineIds.Bengali;
        case CourseIds.TN_Rewise_Portuguese_Brazilian: return LineIds.Portuguese_Brazilian;
        case CourseIds.TN_Rewise_Breton: return LineIds.Breton;
        case CourseIds.TN_Rewise_Bulgarian: return LineIds.Bulgarian;
        case CourseIds.TN_Rewise_French_Canadian: return LineIds.French;
        case CourseIds.TN_Rewise_Cantonese: return LineIds.Cantonese;
        case CourseIds.TN_Rewise_Catalan: return LineIds.Catalan;
        case CourseIds.TN_Rewise_Corsican: return LineIds.Corsican;
        case CourseIds.TN_Rewise_Croatian: return LineIds.Croatian;
        case CourseIds.TN_Rewise_Czech: return LineIds.Czech;
        case CourseIds.TN_Rewise_Danish: return LineIds.Danish;
        case CourseIds.TN_Rewise_Dutch: return LineIds.Dutch;
        case CourseIds.TN_Rewise_English: return LineIds.English;
        case CourseIds.TN_Rewise_English_American: return LineIds.English;
        case CourseIds.TN_Rewise_Estonian: return LineIds.Estonian;
        case CourseIds.TN_Rewise_Finnish: return LineIds.Finnish;
        case CourseIds.TN_Rewise_French: return LineIds.French;
        case CourseIds.TN_Rewise_Galician: return LineIds.Galician;
        case CourseIds.TN_Rewise_Georgian: return LineIds.Georgian;
        case CourseIds.TN_Rewise_German: return LineIds.German;
        case CourseIds.TN_Rewise_Greek: return LineIds.Greek;
        case CourseIds.TN_Rewise_Hausa: return LineIds.Hausa;
        case CourseIds.TN_Rewise_Hebrew: return LineIds.Hebrew;
        case CourseIds.TN_Rewise_Hungarian: return LineIds.Hungarian;
        case CourseIds.TN_Rewise_Chinese_Mandarin: return LineIds.Chinese_Mandarin;
        case CourseIds.TN_Rewise_Icelandic: return LineIds.Icelandic;
        case CourseIds.TN_Rewise_Igbo: return LineIds.Igbo;
        case CourseIds.TN_Rewise_Indonesian: return LineIds.Indonesian;
        case CourseIds.TN_Rewise_Irish: return LineIds.Irish;
        case CourseIds.TN_Rewise_Italian: return LineIds.Italian;
        case CourseIds.TN_Rewise_Japanese: return LineIds.Japanese;
        case CourseIds.TN_Rewise_Khmer: return LineIds.Khmer;
        case CourseIds.TN_Rewise_Kirghiz: return LineIds.Kirghiz;
        case CourseIds.TN_Rewise_Korean: return LineIds.Korean;
        case CourseIds.TN_Rewise_Spanish_Latin_American: return LineIds.Spanish;
        case CourseIds.TN_Rewise_Latvian: return LineIds.Latvian;
        case CourseIds.TN_Rewise_Lithuanian: return LineIds.Lithuanian;
        case CourseIds.TN_Rewise_Macedonian: return LineIds.Macedonian;
        case CourseIds.TN_Rewise_Malay: return LineIds.Malay;
        case CourseIds.TN_Rewise_Malayalam: return LineIds.Malayalam;
        case CourseIds.TN_Rewise_Maltese: return LineIds.Maltese;
        case CourseIds.TN_Rewise_Maori: return LineIds.Maori;
        case CourseIds.TN_Rewise_Mongolian: return LineIds.Mongolian;
        case CourseIds.TN_Rewise_Occitan: return LineIds.Occitan;
        case CourseIds.TN_Rewise_Norwegian: return LineIds.Norwegian;
        case CourseIds.TN_Rewise_Pashto: return LineIds.Pashto;
        case CourseIds.TN_Rewise_Persian: return LineIds.Persian;
        case CourseIds.TN_Rewise_Polish: return LineIds.Polish;
        case CourseIds.TN_Rewise_Portuguese: return LineIds.Portuguese;
        case CourseIds.TN_Rewise_Quechua: return LineIds.Quechua;
        case CourseIds.TN_Rewise_Romanian: return LineIds.Romanian;
        case CourseIds.TN_Rewise_Russian: return LineIds.Russian;
        case CourseIds.TN_Rewise_Serbian: return LineIds.Serbian;
        case CourseIds.TN_Rewise_Sesotho: return LineIds.Sesotho;
        case CourseIds.TN_Rewise_Slovak: return LineIds.Slovak;
        case CourseIds.TN_Rewise_Slovenian: return LineIds.Slovenian;
        case CourseIds.TN_Rewise_Spanish: return LineIds.Spanish;
        case CourseIds.TN_Rewise_Swahili: return LineIds.Swahili;
        case CourseIds.TN_Rewise_Swedish: return LineIds.Swedish;
        case CourseIds.TN_Rewise_Thai: return LineIds.Thai;
        case CourseIds.TN_Rewise_Tibetan: return LineIds.Tibetan;
        case CourseIds.TN_Rewise_Tswana: return LineIds.Tswana;
        case CourseIds.TN_Rewise_Turkish: return LineIds.Turkish;
        case CourseIds.TN_Rewise_Ukrainian: return LineIds.Ukrainian;
        case CourseIds.TN_Rewise_Urdu: return LineIds.Urdu;
        case CourseIds.TN_Rewise_Uzbek: return LineIds.Uzbek;
        case CourseIds.TN_Rewise_Vietnamese: return LineIds.Vietnamese;
        case CourseIds.TN_Rewise_Xhosa: return LineIds.Xhosa;
        case CourseIds.TN_Rewise_Yoruba: return LineIds.Yoruba;
        case CourseIds.TN_Rewise_Zulu: return LineIds.Zulu;
        case CourseIds.StudyDictsq_en:
        case CourseIds.StudyDictsq_fr:
        case CourseIds.StudyDictsq_it:
        case CourseIds.StudyDictsq_de:
        case CourseIds.StudyDictsq_ru:
        case CourseIds.StudyDictsq_sp:
        case CourseIds.StudyDictsq_cs:
        case CourseIds.StudyDictsq_sk:
        case CourseIds.StudyDictsq_bg:
        case CourseIds.StudyDictsq_hr:
        case CourseIds.StudyDictsq_ca:
        case CourseIds.StudyDictsq_hu:
        case CourseIds.StudyDictsq_nl:
        case CourseIds.StudyDictsq_pl:
        case CourseIds.StudyDictsq_pt:
        case CourseIds.StudyDictsq_ro:
        case CourseIds.StudyDictsq_el:
        case CourseIds.StudyDictsq_sl:
        case CourseIds.StudyDictsq_tr:
        case CourseIds.StudyDictsq_uk:
        case CourseIds.StudyDictsq_vi:
        case CourseIds.StudyDictsq_fi:
        case CourseIds.StudyDictsq_sv:
        case CourseIds.StudyDictsq_da:
        case CourseIds.StudyDictsq_nb:
        case CourseIds.StudyDictsq_ja:
        case CourseIds.StudyDictsq_ptbr:
        case CourseIds.StudyDictsq_zh:
        case CourseIds.StudyDictsq_ko:
        case CourseIds.StudyDictsq_ar:
        case CourseIds.StudyDictsq_he:
        case CourseIds.StudyDictsq_th:
        case CourseIds.StudyDictsq_lv:
        case CourseIds.StudyDictsq_lt:
        case CourseIds.StudyDictsq_mk:
        case CourseIds.StudyDictsq_sr:
          return LineIds.Albanian;
        case CourseIds.StudyDictar_en:
        case CourseIds.StudyDictar_fr:
        case CourseIds.StudyDictar_it:
        case CourseIds.StudyDictar_de:
        case CourseIds.StudyDictar_ru:
        case CourseIds.StudyDictar_sp:
        case CourseIds.StudyDictar_cs:
        case CourseIds.StudyDictar_sk:
        case CourseIds.StudyDictar_bg:
        case CourseIds.StudyDictar_hr:
        case CourseIds.StudyDictar_ca:
        case CourseIds.StudyDictar_hu:
        case CourseIds.StudyDictar_nl:
        case CourseIds.StudyDictar_pl:
        case CourseIds.StudyDictar_pt:
        case CourseIds.StudyDictar_ro:
        case CourseIds.StudyDictar_el:
        case CourseIds.StudyDictar_sl:
        case CourseIds.StudyDictar_tr:
        case CourseIds.StudyDictar_uk:
        case CourseIds.StudyDictar_vi:
        case CourseIds.StudyDictar_fi:
        case CourseIds.StudyDictar_sv:
        case CourseIds.StudyDictar_da:
        case CourseIds.StudyDictar_nb:
        case CourseIds.StudyDictar_sq:
        case CourseIds.StudyDictar_ja:
        case CourseIds.StudyDictar_ptbr:
        case CourseIds.StudyDictar_zh:
        case CourseIds.StudyDictar_ko:
        case CourseIds.StudyDictar_he:
        case CourseIds.StudyDictar_th:
        case CourseIds.StudyDictar_lv:
        case CourseIds.StudyDictar_lt:
        case CourseIds.StudyDictar_mk:
        case CourseIds.StudyDictar_sr:
          return LineIds.Arabic;
        case CourseIds.StudyDictptbr_en:
        case CourseIds.StudyDictptbr_fr:
        case CourseIds.StudyDictptbr_it:
        case CourseIds.StudyDictptbr_de:
        case CourseIds.StudyDictptbr_ru:
        case CourseIds.StudyDictptbr_sp:
        case CourseIds.StudyDictptbr_cs:
        case CourseIds.StudyDictptbr_sk:
        case CourseIds.StudyDictptbr_bg:
        case CourseIds.StudyDictptbr_hr:
        case CourseIds.StudyDictptbr_ca:
        case CourseIds.StudyDictptbr_hu:
        case CourseIds.StudyDictptbr_nl:
        case CourseIds.StudyDictptbr_pl:
        case CourseIds.StudyDictptbr_pt:
        case CourseIds.StudyDictptbr_ro:
        case CourseIds.StudyDictptbr_el:
        case CourseIds.StudyDictptbr_sl:
        case CourseIds.StudyDictptbr_tr:
        case CourseIds.StudyDictptbr_uk:
        case CourseIds.StudyDictptbr_vi:
        case CourseIds.StudyDictptbr_fi:
        case CourseIds.StudyDictptbr_sv:
        case CourseIds.StudyDictptbr_da:
        case CourseIds.StudyDictptbr_nb:
        case CourseIds.StudyDictptbr_sq:
        case CourseIds.StudyDictptbr_ja:
        case CourseIds.StudyDictptbr_zh:
        case CourseIds.StudyDictptbr_ko:
        case CourseIds.StudyDictptbr_ar:
        case CourseIds.StudyDictptbr_he:
        case CourseIds.StudyDictptbr_th:
        case CourseIds.StudyDictptbr_lv:
        case CourseIds.StudyDictptbr_lt:
        case CourseIds.StudyDictptbr_mk:
        case CourseIds.StudyDictptbr_sr:
          return LineIds.Portuguese_Brazilian;
        case CourseIds.StudyDictbg_en:
        case CourseIds.StudyDictbg_fr:
        case CourseIds.StudyDictbg_it:
        case CourseIds.StudyDictbg_de:
        case CourseIds.StudyDictbg_ru:
        case CourseIds.StudyDictbg_sp:
        case CourseIds.StudyDictbg_cs:
        case CourseIds.StudyDictbg_sk:
        case CourseIds.StudyDictbg_hr:
        case CourseIds.StudyDictbg_ca:
        case CourseIds.StudyDictbg_hu:
        case CourseIds.StudyDictbg_nl:
        case CourseIds.StudyDictbg_pl:
        case CourseIds.StudyDictbg_pt:
        case CourseIds.StudyDictbg_ro:
        case CourseIds.StudyDictbg_el:
        case CourseIds.StudyDictbg_sl:
        case CourseIds.StudyDictbg_tr:
        case CourseIds.StudyDictbg_uk:
        case CourseIds.StudyDictbg_vi:
        case CourseIds.StudyDictbg_fi:
        case CourseIds.StudyDictbg_sv:
        case CourseIds.StudyDictbg_da:
        case CourseIds.StudyDictbg_nb:
        case CourseIds.StudyDictbg_sq:
        case CourseIds.StudyDictbg_ja:
        case CourseIds.StudyDictbg_ptbr:
        case CourseIds.StudyDictbg_zh:
        case CourseIds.StudyDictbg_ko:
        case CourseIds.StudyDictbg_ar:
        case CourseIds.StudyDictbg_he:
        case CourseIds.StudyDictbg_th:
        case CourseIds.StudyDictbg_lv:
        case CourseIds.StudyDictbg_lt:
        case CourseIds.StudyDictbg_mk:
        case CourseIds.StudyDictbg_sr:
          return LineIds.Bulgarian;
        case CourseIds.StudyDictca_en:
        case CourseIds.StudyDictca_fr:
        case CourseIds.StudyDictca_it:
        case CourseIds.StudyDictca_de:
        case CourseIds.StudyDictca_ru:
        case CourseIds.StudyDictca_sp:
        case CourseIds.StudyDictca_cs:
        case CourseIds.StudyDictca_sk:
        case CourseIds.StudyDictca_bg:
        case CourseIds.StudyDictca_hr:
        case CourseIds.StudyDictca_hu:
        case CourseIds.StudyDictca_nl:
        case CourseIds.StudyDictca_pl:
        case CourseIds.StudyDictca_pt:
        case CourseIds.StudyDictca_ro:
        case CourseIds.StudyDictca_el:
        case CourseIds.StudyDictca_sl:
        case CourseIds.StudyDictca_tr:
        case CourseIds.StudyDictca_uk:
        case CourseIds.StudyDictca_vi:
        case CourseIds.StudyDictca_fi:
        case CourseIds.StudyDictca_sv:
        case CourseIds.StudyDictca_da:
        case CourseIds.StudyDictca_nb:
        case CourseIds.StudyDictca_sq:
        case CourseIds.StudyDictca_ja:
        case CourseIds.StudyDictca_ptbr:
        case CourseIds.StudyDictca_zh:
        case CourseIds.StudyDictca_ko:
        case CourseIds.StudyDictca_ar:
        case CourseIds.StudyDictca_he:
        case CourseIds.StudyDictca_th:
        case CourseIds.StudyDictca_lv:
        case CourseIds.StudyDictca_lt:
        case CourseIds.StudyDictca_mk:
        case CourseIds.StudyDictca_sr:
          return LineIds.Catalan;
        case CourseIds.StudyDicthr_en:
        case CourseIds.StudyDicthr_fr:
        case CourseIds.StudyDicthr_it:
        case CourseIds.StudyDicthr_de:
        case CourseIds.StudyDicthr_ru:
        case CourseIds.StudyDicthr_sp:
        case CourseIds.StudyDicthr_cs:
        case CourseIds.StudyDicthr_sk:
        case CourseIds.StudyDicthr_bg:
        case CourseIds.StudyDicthr_ca:
        case CourseIds.StudyDicthr_hu:
        case CourseIds.StudyDicthr_nl:
        case CourseIds.StudyDicthr_pl:
        case CourseIds.StudyDicthr_pt:
        case CourseIds.StudyDicthr_ro:
        case CourseIds.StudyDicthr_el:
        case CourseIds.StudyDicthr_sl:
        case CourseIds.StudyDicthr_tr:
        case CourseIds.StudyDicthr_uk:
        case CourseIds.StudyDicthr_vi:
        case CourseIds.StudyDicthr_fi:
        case CourseIds.StudyDicthr_sv:
        case CourseIds.StudyDicthr_da:
        case CourseIds.StudyDicthr_nb:
        case CourseIds.StudyDicthr_sq:
        case CourseIds.StudyDicthr_ja:
        case CourseIds.StudyDicthr_ptbr:
        case CourseIds.StudyDicthr_zh:
        case CourseIds.StudyDicthr_ko:
        case CourseIds.StudyDicthr_ar:
        case CourseIds.StudyDicthr_he:
        case CourseIds.StudyDicthr_th:
        case CourseIds.StudyDicthr_lv:
        case CourseIds.StudyDicthr_lt:
        case CourseIds.StudyDicthr_mk:
        case CourseIds.StudyDicthr_sr:
          return LineIds.Croatian;
        case CourseIds.StudyDictcs_en:
        case CourseIds.StudyDictcs_fr:
        case CourseIds.StudyDictcs_it:
        case CourseIds.StudyDictcs_de:
        case CourseIds.StudyDictcs_ru:
        case CourseIds.StudyDictcs_sp:
        case CourseIds.StudyDictcs_sk:
        case CourseIds.StudyDictcs_bg:
        case CourseIds.StudyDictcs_hr:
        case CourseIds.StudyDictcs_ca:
        case CourseIds.StudyDictcs_hu:
        case CourseIds.StudyDictcs_nl:
        case CourseIds.StudyDictcs_pl:
        case CourseIds.StudyDictcs_pt:
        case CourseIds.StudyDictcs_ro:
        case CourseIds.StudyDictcs_el:
        case CourseIds.StudyDictcs_sl:
        case CourseIds.StudyDictcs_tr:
        case CourseIds.StudyDictcs_uk:
        case CourseIds.StudyDictcs_vi:
        case CourseIds.StudyDictcs_fi:
        case CourseIds.StudyDictcs_sv:
        case CourseIds.StudyDictcs_da:
        case CourseIds.StudyDictcs_nb:
        case CourseIds.StudyDictcs_sq:
        case CourseIds.StudyDictcs_ja:
        case CourseIds.StudyDictcs_ptbr:
        case CourseIds.StudyDictcs_zh:
        case CourseIds.StudyDictcs_ko:
        case CourseIds.StudyDictcs_ar:
        case CourseIds.StudyDictcs_he:
        case CourseIds.StudyDictcs_th:
        case CourseIds.StudyDictcs_lv:
        case CourseIds.StudyDictcs_lt:
        case CourseIds.StudyDictcs_mk:
        case CourseIds.StudyDictcs_sr:
          return LineIds.Czech;
        case CourseIds.StudyDictda_en:
        case CourseIds.StudyDictda_fr:
        case CourseIds.StudyDictda_it:
        case CourseIds.StudyDictda_de:
        case CourseIds.StudyDictda_ru:
        case CourseIds.StudyDictda_sp:
        case CourseIds.StudyDictda_cs:
        case CourseIds.StudyDictda_sk:
        case CourseIds.StudyDictda_bg:
        case CourseIds.StudyDictda_hr:
        case CourseIds.StudyDictda_ca:
        case CourseIds.StudyDictda_hu:
        case CourseIds.StudyDictda_nl:
        case CourseIds.StudyDictda_pl:
        case CourseIds.StudyDictda_pt:
        case CourseIds.StudyDictda_ro:
        case CourseIds.StudyDictda_el:
        case CourseIds.StudyDictda_sl:
        case CourseIds.StudyDictda_tr:
        case CourseIds.StudyDictda_uk:
        case CourseIds.StudyDictda_vi:
        case CourseIds.StudyDictda_fi:
        case CourseIds.StudyDictda_sv:
        case CourseIds.StudyDictda_nb:
        case CourseIds.StudyDictda_sq:
        case CourseIds.StudyDictda_ja:
        case CourseIds.StudyDictda_ptbr:
        case CourseIds.StudyDictda_zh:
        case CourseIds.StudyDictda_ko:
        case CourseIds.StudyDictda_ar:
        case CourseIds.StudyDictda_he:
        case CourseIds.StudyDictda_th:
        case CourseIds.StudyDictda_lv:
        case CourseIds.StudyDictda_lt:
        case CourseIds.StudyDictda_mk:
        case CourseIds.StudyDictda_sr:
          return LineIds.Danish;
        case CourseIds.StudyDictnl_en:
        case CourseIds.StudyDictnl_fr:
        case CourseIds.StudyDictnl_it:
        case CourseIds.StudyDictnl_de:
        case CourseIds.StudyDictnl_ru:
        case CourseIds.StudyDictnl_sp:
        case CourseIds.StudyDictnl_cs:
        case CourseIds.StudyDictnl_sk:
        case CourseIds.StudyDictnl_bg:
        case CourseIds.StudyDictnl_hr:
        case CourseIds.StudyDictnl_ca:
        case CourseIds.StudyDictnl_hu:
        case CourseIds.StudyDictnl_pl:
        case CourseIds.StudyDictnl_pt:
        case CourseIds.StudyDictnl_ro:
        case CourseIds.StudyDictnl_el:
        case CourseIds.StudyDictnl_sl:
        case CourseIds.StudyDictnl_tr:
        case CourseIds.StudyDictnl_uk:
        case CourseIds.StudyDictnl_vi:
        case CourseIds.StudyDictnl_fi:
        case CourseIds.StudyDictnl_sv:
        case CourseIds.StudyDictnl_da:
        case CourseIds.StudyDictnl_nb:
        case CourseIds.StudyDictnl_sq:
        case CourseIds.StudyDictnl_ja:
        case CourseIds.StudyDictnl_ptbr:
        case CourseIds.StudyDictnl_zh:
        case CourseIds.StudyDictnl_ko:
        case CourseIds.StudyDictnl_ar:
        case CourseIds.StudyDictnl_he:
        case CourseIds.StudyDictnl_th:
        case CourseIds.StudyDictnl_lv:
        case CourseIds.StudyDictnl_lt:
        case CourseIds.StudyDictnl_mk:
        case CourseIds.StudyDictnl_sr:
          return LineIds.Dutch;
        case CourseIds.StudyDicten_fr:
        case CourseIds.StudyDicten_it:
        case CourseIds.StudyDicten_de:
        case CourseIds.StudyDicten_ru:
        case CourseIds.StudyDicten_sp:
        case CourseIds.StudyDicten_cs:
        case CourseIds.StudyDicten_sk:
        case CourseIds.StudyDicten_bg:
        case CourseIds.StudyDicten_hr:
        case CourseIds.StudyDicten_ca:
        case CourseIds.StudyDicten_hu:
        case CourseIds.StudyDicten_nl:
        case CourseIds.StudyDicten_pl:
        case CourseIds.StudyDicten_pt:
        case CourseIds.StudyDicten_ro:
        case CourseIds.StudyDicten_el:
        case CourseIds.StudyDicten_sl:
        case CourseIds.StudyDicten_tr:
        case CourseIds.StudyDicten_uk:
        case CourseIds.StudyDicten_vi:
        case CourseIds.StudyDicten_fi:
        case CourseIds.StudyDicten_sv:
        case CourseIds.StudyDicten_da:
        case CourseIds.StudyDicten_nb:
        case CourseIds.StudyDicten_sq:
        case CourseIds.StudyDicten_ja:
        case CourseIds.StudyDicten_ptbr:
        case CourseIds.StudyDicten_zh:
        case CourseIds.StudyDicten_ko:
        case CourseIds.StudyDicten_ar:
        case CourseIds.StudyDicten_he:
        case CourseIds.StudyDicten_th:
        case CourseIds.StudyDicten_lv:
        case CourseIds.StudyDicten_lt:
        case CourseIds.StudyDicten_mk:
        case CourseIds.StudyDicten_sr:
          return LineIds.English;
        case CourseIds.StudyDictfi_en:
        case CourseIds.StudyDictfi_fr:
        case CourseIds.StudyDictfi_it:
        case CourseIds.StudyDictfi_de:
        case CourseIds.StudyDictfi_ru:
        case CourseIds.StudyDictfi_sp:
        case CourseIds.StudyDictfi_cs:
        case CourseIds.StudyDictfi_sk:
        case CourseIds.StudyDictfi_bg:
        case CourseIds.StudyDictfi_hr:
        case CourseIds.StudyDictfi_ca:
        case CourseIds.StudyDictfi_hu:
        case CourseIds.StudyDictfi_nl:
        case CourseIds.StudyDictfi_pl:
        case CourseIds.StudyDictfi_pt:
        case CourseIds.StudyDictfi_ro:
        case CourseIds.StudyDictfi_el:
        case CourseIds.StudyDictfi_sl:
        case CourseIds.StudyDictfi_tr:
        case CourseIds.StudyDictfi_uk:
        case CourseIds.StudyDictfi_vi:
        case CourseIds.StudyDictfi_sv:
        case CourseIds.StudyDictfi_da:
        case CourseIds.StudyDictfi_nb:
        case CourseIds.StudyDictfi_sq:
        case CourseIds.StudyDictfi_ja:
        case CourseIds.StudyDictfi_ptbr:
        case CourseIds.StudyDictfi_zh:
        case CourseIds.StudyDictfi_ko:
        case CourseIds.StudyDictfi_ar:
        case CourseIds.StudyDictfi_he:
        case CourseIds.StudyDictfi_th:
        case CourseIds.StudyDictfi_lv:
        case CourseIds.StudyDictfi_lt:
        case CourseIds.StudyDictfi_mk:
        case CourseIds.StudyDictfi_sr:
          return LineIds.Finnish;
        case CourseIds.StudyDictfr_en:
        case CourseIds.StudyDictfr_it:
        case CourseIds.StudyDictfr_de:
        case CourseIds.StudyDictfr_ru:
        case CourseIds.StudyDictfr_sp:
        case CourseIds.StudyDictfr_cs:
        case CourseIds.StudyDictfr_sk:
        case CourseIds.StudyDictfr_bg:
        case CourseIds.StudyDictfr_hr:
        case CourseIds.StudyDictfr_ca:
        case CourseIds.StudyDictfr_hu:
        case CourseIds.StudyDictfr_nl:
        case CourseIds.StudyDictfr_pl:
        case CourseIds.StudyDictfr_pt:
        case CourseIds.StudyDictfr_ro:
        case CourseIds.StudyDictfr_el:
        case CourseIds.StudyDictfr_sl:
        case CourseIds.StudyDictfr_tr:
        case CourseIds.StudyDictfr_uk:
        case CourseIds.StudyDictfr_vi:
        case CourseIds.StudyDictfr_fi:
        case CourseIds.StudyDictfr_sv:
        case CourseIds.StudyDictfr_da:
        case CourseIds.StudyDictfr_nb:
        case CourseIds.StudyDictfr_sq:
        case CourseIds.StudyDictfr_ja:
        case CourseIds.StudyDictfr_ptbr:
        case CourseIds.StudyDictfr_zh:
        case CourseIds.StudyDictfr_ko:
        case CourseIds.StudyDictfr_ar:
        case CourseIds.StudyDictfr_he:
        case CourseIds.StudyDictfr_th:
        case CourseIds.StudyDictfr_lv:
        case CourseIds.StudyDictfr_lt:
        case CourseIds.StudyDictfr_mk:
        case CourseIds.StudyDictfr_sr:
          return LineIds.French;
        case CourseIds.StudyDictde_en:
        case CourseIds.StudyDictde_fr:
        case CourseIds.StudyDictde_it:
        case CourseIds.StudyDictde_ru:
        case CourseIds.StudyDictde_sp:
        case CourseIds.StudyDictde_cs:
        case CourseIds.StudyDictde_sk:
        case CourseIds.StudyDictde_bg:
        case CourseIds.StudyDictde_hr:
        case CourseIds.StudyDictde_ca:
        case CourseIds.StudyDictde_hu:
        case CourseIds.StudyDictde_nl:
        case CourseIds.StudyDictde_pl:
        case CourseIds.StudyDictde_pt:
        case CourseIds.StudyDictde_ro:
        case CourseIds.StudyDictde_el:
        case CourseIds.StudyDictde_sl:
        case CourseIds.StudyDictde_tr:
        case CourseIds.StudyDictde_uk:
        case CourseIds.StudyDictde_vi:
        case CourseIds.StudyDictde_fi:
        case CourseIds.StudyDictde_sv:
        case CourseIds.StudyDictde_da:
        case CourseIds.StudyDictde_nb:
        case CourseIds.StudyDictde_sq:
        case CourseIds.StudyDictde_ja:
        case CourseIds.StudyDictde_ptbr:
        case CourseIds.StudyDictde_zh:
        case CourseIds.StudyDictde_ko:
        case CourseIds.StudyDictde_ar:
        case CourseIds.StudyDictde_he:
        case CourseIds.StudyDictde_th:
        case CourseIds.StudyDictde_lv:
        case CourseIds.StudyDictde_lt:
        case CourseIds.StudyDictde_mk:
        case CourseIds.StudyDictde_sr:
          return LineIds.German;
        case CourseIds.StudyDictel_en:
        case CourseIds.StudyDictel_fr:
        case CourseIds.StudyDictel_it:
        case CourseIds.StudyDictel_de:
        case CourseIds.StudyDictel_ru:
        case CourseIds.StudyDictel_sp:
        case CourseIds.StudyDictel_cs:
        case CourseIds.StudyDictel_sk:
        case CourseIds.StudyDictel_bg:
        case CourseIds.StudyDictel_hr:
        case CourseIds.StudyDictel_ca:
        case CourseIds.StudyDictel_hu:
        case CourseIds.StudyDictel_nl:
        case CourseIds.StudyDictel_pl:
        case CourseIds.StudyDictel_pt:
        case CourseIds.StudyDictel_ro:
        case CourseIds.StudyDictel_sl:
        case CourseIds.StudyDictel_tr:
        case CourseIds.StudyDictel_uk:
        case CourseIds.StudyDictel_vi:
        case CourseIds.StudyDictel_fi:
        case CourseIds.StudyDictel_sv:
        case CourseIds.StudyDictel_da:
        case CourseIds.StudyDictel_nb:
        case CourseIds.StudyDictel_sq:
        case CourseIds.StudyDictel_ja:
        case CourseIds.StudyDictel_ptbr:
        case CourseIds.StudyDictel_zh:
        case CourseIds.StudyDictel_ko:
        case CourseIds.StudyDictel_ar:
        case CourseIds.StudyDictel_he:
        case CourseIds.StudyDictel_th:
        case CourseIds.StudyDictel_lv:
        case CourseIds.StudyDictel_lt:
        case CourseIds.StudyDictel_mk:
        case CourseIds.StudyDictel_sr:
          return LineIds.Greek;
        case CourseIds.StudyDicthe_en:
        case CourseIds.StudyDicthe_fr:
        case CourseIds.StudyDicthe_it:
        case CourseIds.StudyDicthe_de:
        case CourseIds.StudyDicthe_ru:
        case CourseIds.StudyDicthe_sp:
        case CourseIds.StudyDicthe_cs:
        case CourseIds.StudyDicthe_sk:
        case CourseIds.StudyDicthe_bg:
        case CourseIds.StudyDicthe_hr:
        case CourseIds.StudyDicthe_ca:
        case CourseIds.StudyDicthe_hu:
        case CourseIds.StudyDicthe_nl:
        case CourseIds.StudyDicthe_pl:
        case CourseIds.StudyDicthe_pt:
        case CourseIds.StudyDicthe_ro:
        case CourseIds.StudyDicthe_el:
        case CourseIds.StudyDicthe_sl:
        case CourseIds.StudyDicthe_tr:
        case CourseIds.StudyDicthe_uk:
        case CourseIds.StudyDicthe_vi:
        case CourseIds.StudyDicthe_fi:
        case CourseIds.StudyDicthe_sv:
        case CourseIds.StudyDicthe_da:
        case CourseIds.StudyDicthe_nb:
        case CourseIds.StudyDicthe_sq:
        case CourseIds.StudyDicthe_ja:
        case CourseIds.StudyDicthe_ptbr:
        case CourseIds.StudyDicthe_zh:
        case CourseIds.StudyDicthe_ko:
        case CourseIds.StudyDicthe_ar:
        case CourseIds.StudyDicthe_th:
        case CourseIds.StudyDicthe_lv:
        case CourseIds.StudyDicthe_lt:
        case CourseIds.StudyDicthe_mk:
        case CourseIds.StudyDicthe_sr:
          return LineIds.Hebrew;
        case CourseIds.StudyDicthu_en:
        case CourseIds.StudyDicthu_fr:
        case CourseIds.StudyDicthu_it:
        case CourseIds.StudyDicthu_de:
        case CourseIds.StudyDicthu_ru:
        case CourseIds.StudyDicthu_sp:
        case CourseIds.StudyDicthu_cs:
        case CourseIds.StudyDicthu_sk:
        case CourseIds.StudyDicthu_bg:
        case CourseIds.StudyDicthu_hr:
        case CourseIds.StudyDicthu_ca:
        case CourseIds.StudyDicthu_nl:
        case CourseIds.StudyDicthu_pl:
        case CourseIds.StudyDicthu_pt:
        case CourseIds.StudyDicthu_ro:
        case CourseIds.StudyDicthu_el:
        case CourseIds.StudyDicthu_sl:
        case CourseIds.StudyDicthu_tr:
        case CourseIds.StudyDicthu_uk:
        case CourseIds.StudyDicthu_vi:
        case CourseIds.StudyDicthu_fi:
        case CourseIds.StudyDicthu_sv:
        case CourseIds.StudyDicthu_da:
        case CourseIds.StudyDicthu_nb:
        case CourseIds.StudyDicthu_sq:
        case CourseIds.StudyDicthu_ja:
        case CourseIds.StudyDicthu_ptbr:
        case CourseIds.StudyDicthu_zh:
        case CourseIds.StudyDicthu_ko:
        case CourseIds.StudyDicthu_ar:
        case CourseIds.StudyDicthu_he:
        case CourseIds.StudyDicthu_th:
        case CourseIds.StudyDicthu_lv:
        case CourseIds.StudyDicthu_lt:
        case CourseIds.StudyDicthu_mk:
        case CourseIds.StudyDicthu_sr:
          return LineIds.Hungarian;
        case CourseIds.StudyDictzh_en:
        case CourseIds.StudyDictzh_fr:
        case CourseIds.StudyDictzh_it:
        case CourseIds.StudyDictzh_de:
        case CourseIds.StudyDictzh_ru:
        case CourseIds.StudyDictzh_sp:
        case CourseIds.StudyDictzh_cs:
        case CourseIds.StudyDictzh_sk:
        case CourseIds.StudyDictzh_bg:
        case CourseIds.StudyDictzh_hr:
        case CourseIds.StudyDictzh_ca:
        case CourseIds.StudyDictzh_hu:
        case CourseIds.StudyDictzh_nl:
        case CourseIds.StudyDictzh_pl:
        case CourseIds.StudyDictzh_pt:
        case CourseIds.StudyDictzh_ro:
        case CourseIds.StudyDictzh_el:
        case CourseIds.StudyDictzh_sl:
        case CourseIds.StudyDictzh_tr:
        case CourseIds.StudyDictzh_uk:
        case CourseIds.StudyDictzh_vi:
        case CourseIds.StudyDictzh_fi:
        case CourseIds.StudyDictzh_sv:
        case CourseIds.StudyDictzh_da:
        case CourseIds.StudyDictzh_nb:
        case CourseIds.StudyDictzh_sq:
        case CourseIds.StudyDictzh_ja:
        case CourseIds.StudyDictzh_ptbr:
        case CourseIds.StudyDictzh_ko:
        case CourseIds.StudyDictzh_ar:
        case CourseIds.StudyDictzh_he:
        case CourseIds.StudyDictzh_th:
        case CourseIds.StudyDictzh_lv:
        case CourseIds.StudyDictzh_lt:
        case CourseIds.StudyDictzh_mk:
        case CourseIds.StudyDictzh_sr:
          return LineIds.Chinese_Mandarin;
        case CourseIds.StudyDictit_en:
        case CourseIds.StudyDictit_fr:
        case CourseIds.StudyDictit_de:
        case CourseIds.StudyDictit_ru:
        case CourseIds.StudyDictit_sp:
        case CourseIds.StudyDictit_cs:
        case CourseIds.StudyDictit_sk:
        case CourseIds.StudyDictit_bg:
        case CourseIds.StudyDictit_hr:
        case CourseIds.StudyDictit_ca:
        case CourseIds.StudyDictit_hu:
        case CourseIds.StudyDictit_nl:
        case CourseIds.StudyDictit_pl:
        case CourseIds.StudyDictit_pt:
        case CourseIds.StudyDictit_ro:
        case CourseIds.StudyDictit_el:
        case CourseIds.StudyDictit_sl:
        case CourseIds.StudyDictit_tr:
        case CourseIds.StudyDictit_uk:
        case CourseIds.StudyDictit_vi:
        case CourseIds.StudyDictit_fi:
        case CourseIds.StudyDictit_sv:
        case CourseIds.StudyDictit_da:
        case CourseIds.StudyDictit_nb:
        case CourseIds.StudyDictit_sq:
        case CourseIds.StudyDictit_ja:
        case CourseIds.StudyDictit_ptbr:
        case CourseIds.StudyDictit_zh:
        case CourseIds.StudyDictit_ko:
        case CourseIds.StudyDictit_ar:
        case CourseIds.StudyDictit_he:
        case CourseIds.StudyDictit_th:
        case CourseIds.StudyDictit_lv:
        case CourseIds.StudyDictit_lt:
        case CourseIds.StudyDictit_mk:
        case CourseIds.StudyDictit_sr:
          return LineIds.Italian;
        case CourseIds.StudyDictja_en:
        case CourseIds.StudyDictja_fr:
        case CourseIds.StudyDictja_it:
        case CourseIds.StudyDictja_de:
        case CourseIds.StudyDictja_ru:
        case CourseIds.StudyDictja_sp:
        case CourseIds.StudyDictja_cs:
        case CourseIds.StudyDictja_sk:
        case CourseIds.StudyDictja_bg:
        case CourseIds.StudyDictja_hr:
        case CourseIds.StudyDictja_ca:
        case CourseIds.StudyDictja_hu:
        case CourseIds.StudyDictja_nl:
        case CourseIds.StudyDictja_pl:
        case CourseIds.StudyDictja_pt:
        case CourseIds.StudyDictja_ro:
        case CourseIds.StudyDictja_el:
        case CourseIds.StudyDictja_sl:
        case CourseIds.StudyDictja_tr:
        case CourseIds.StudyDictja_uk:
        case CourseIds.StudyDictja_vi:
        case CourseIds.StudyDictja_fi:
        case CourseIds.StudyDictja_sv:
        case CourseIds.StudyDictja_da:
        case CourseIds.StudyDictja_nb:
        case CourseIds.StudyDictja_sq:
        case CourseIds.StudyDictja_ptbr:
        case CourseIds.StudyDictja_zh:
        case CourseIds.StudyDictja_ko:
        case CourseIds.StudyDictja_ar:
        case CourseIds.StudyDictja_he:
        case CourseIds.StudyDictja_th:
        case CourseIds.StudyDictja_lv:
        case CourseIds.StudyDictja_lt:
        case CourseIds.StudyDictja_mk:
        case CourseIds.StudyDictja_sr:
          return LineIds.Japanese;
        case CourseIds.StudyDictko_en:
        case CourseIds.StudyDictko_fr:
        case CourseIds.StudyDictko_it:
        case CourseIds.StudyDictko_de:
        case CourseIds.StudyDictko_ru:
        case CourseIds.StudyDictko_sp:
        case CourseIds.StudyDictko_cs:
        case CourseIds.StudyDictko_sk:
        case CourseIds.StudyDictko_bg:
        case CourseIds.StudyDictko_hr:
        case CourseIds.StudyDictko_ca:
        case CourseIds.StudyDictko_hu:
        case CourseIds.StudyDictko_nl:
        case CourseIds.StudyDictko_pl:
        case CourseIds.StudyDictko_pt:
        case CourseIds.StudyDictko_ro:
        case CourseIds.StudyDictko_el:
        case CourseIds.StudyDictko_sl:
        case CourseIds.StudyDictko_tr:
        case CourseIds.StudyDictko_uk:
        case CourseIds.StudyDictko_vi:
        case CourseIds.StudyDictko_fi:
        case CourseIds.StudyDictko_sv:
        case CourseIds.StudyDictko_da:
        case CourseIds.StudyDictko_nb:
        case CourseIds.StudyDictko_sq:
        case CourseIds.StudyDictko_ja:
        case CourseIds.StudyDictko_ptbr:
        case CourseIds.StudyDictko_zh:
        case CourseIds.StudyDictko_ar:
        case CourseIds.StudyDictko_he:
        case CourseIds.StudyDictko_th:
        case CourseIds.StudyDictko_lv:
        case CourseIds.StudyDictko_lt:
        case CourseIds.StudyDictko_mk:
        case CourseIds.StudyDictko_sr:
          return LineIds.Korean;
        case CourseIds.StudyDictlv_en:
        case CourseIds.StudyDictlv_fr:
        case CourseIds.StudyDictlv_it:
        case CourseIds.StudyDictlv_de:
        case CourseIds.StudyDictlv_ru:
        case CourseIds.StudyDictlv_sp:
        case CourseIds.StudyDictlv_cs:
        case CourseIds.StudyDictlv_sk:
        case CourseIds.StudyDictlv_bg:
        case CourseIds.StudyDictlv_hr:
        case CourseIds.StudyDictlv_ca:
        case CourseIds.StudyDictlv_hu:
        case CourseIds.StudyDictlv_nl:
        case CourseIds.StudyDictlv_pl:
        case CourseIds.StudyDictlv_pt:
        case CourseIds.StudyDictlv_ro:
        case CourseIds.StudyDictlv_el:
        case CourseIds.StudyDictlv_sl:
        case CourseIds.StudyDictlv_tr:
        case CourseIds.StudyDictlv_uk:
        case CourseIds.StudyDictlv_vi:
        case CourseIds.StudyDictlv_fi:
        case CourseIds.StudyDictlv_sv:
        case CourseIds.StudyDictlv_da:
        case CourseIds.StudyDictlv_nb:
        case CourseIds.StudyDictlv_sq:
        case CourseIds.StudyDictlv_ja:
        case CourseIds.StudyDictlv_ptbr:
        case CourseIds.StudyDictlv_zh:
        case CourseIds.StudyDictlv_ko:
        case CourseIds.StudyDictlv_ar:
        case CourseIds.StudyDictlv_he:
        case CourseIds.StudyDictlv_th:
        case CourseIds.StudyDictlv_lt:
        case CourseIds.StudyDictlv_mk:
        case CourseIds.StudyDictlv_sr:
          return LineIds.Latvian;
        case CourseIds.StudyDictlt_en:
        case CourseIds.StudyDictlt_fr:
        case CourseIds.StudyDictlt_it:
        case CourseIds.StudyDictlt_de:
        case CourseIds.StudyDictlt_ru:
        case CourseIds.StudyDictlt_sp:
        case CourseIds.StudyDictlt_cs:
        case CourseIds.StudyDictlt_sk:
        case CourseIds.StudyDictlt_bg:
        case CourseIds.StudyDictlt_hr:
        case CourseIds.StudyDictlt_ca:
        case CourseIds.StudyDictlt_hu:
        case CourseIds.StudyDictlt_nl:
        case CourseIds.StudyDictlt_pl:
        case CourseIds.StudyDictlt_pt:
        case CourseIds.StudyDictlt_ro:
        case CourseIds.StudyDictlt_el:
        case CourseIds.StudyDictlt_sl:
        case CourseIds.StudyDictlt_tr:
        case CourseIds.StudyDictlt_uk:
        case CourseIds.StudyDictlt_vi:
        case CourseIds.StudyDictlt_fi:
        case CourseIds.StudyDictlt_sv:
        case CourseIds.StudyDictlt_da:
        case CourseIds.StudyDictlt_nb:
        case CourseIds.StudyDictlt_sq:
        case CourseIds.StudyDictlt_ja:
        case CourseIds.StudyDictlt_ptbr:
        case CourseIds.StudyDictlt_zh:
        case CourseIds.StudyDictlt_ko:
        case CourseIds.StudyDictlt_ar:
        case CourseIds.StudyDictlt_he:
        case CourseIds.StudyDictlt_th:
        case CourseIds.StudyDictlt_lv:
        case CourseIds.StudyDictlt_mk:
        case CourseIds.StudyDictlt_sr:
          return LineIds.Lithuanian;
        case CourseIds.StudyDictmk_en:
        case CourseIds.StudyDictmk_fr:
        case CourseIds.StudyDictmk_it:
        case CourseIds.StudyDictmk_de:
        case CourseIds.StudyDictmk_ru:
        case CourseIds.StudyDictmk_sp:
        case CourseIds.StudyDictmk_cs:
        case CourseIds.StudyDictmk_sk:
        case CourseIds.StudyDictmk_bg:
        case CourseIds.StudyDictmk_hr:
        case CourseIds.StudyDictmk_ca:
        case CourseIds.StudyDictmk_hu:
        case CourseIds.StudyDictmk_nl:
        case CourseIds.StudyDictmk_pl:
        case CourseIds.StudyDictmk_pt:
        case CourseIds.StudyDictmk_ro:
        case CourseIds.StudyDictmk_el:
        case CourseIds.StudyDictmk_sl:
        case CourseIds.StudyDictmk_tr:
        case CourseIds.StudyDictmk_uk:
        case CourseIds.StudyDictmk_vi:
        case CourseIds.StudyDictmk_fi:
        case CourseIds.StudyDictmk_sv:
        case CourseIds.StudyDictmk_da:
        case CourseIds.StudyDictmk_nb:
        case CourseIds.StudyDictmk_sq:
        case CourseIds.StudyDictmk_ja:
        case CourseIds.StudyDictmk_ptbr:
        case CourseIds.StudyDictmk_zh:
        case CourseIds.StudyDictmk_ko:
        case CourseIds.StudyDictmk_ar:
        case CourseIds.StudyDictmk_he:
        case CourseIds.StudyDictmk_th:
        case CourseIds.StudyDictmk_lv:
        case CourseIds.StudyDictmk_lt:
        case CourseIds.StudyDictmk_sr:
          return LineIds.Macedonian;
        case CourseIds.StudyDictnb_en:
        case CourseIds.StudyDictnb_fr:
        case CourseIds.StudyDictnb_it:
        case CourseIds.StudyDictnb_de:
        case CourseIds.StudyDictnb_ru:
        case CourseIds.StudyDictnb_sp:
        case CourseIds.StudyDictnb_cs:
        case CourseIds.StudyDictnb_sk:
        case CourseIds.StudyDictnb_bg:
        case CourseIds.StudyDictnb_hr:
        case CourseIds.StudyDictnb_ca:
        case CourseIds.StudyDictnb_hu:
        case CourseIds.StudyDictnb_nl:
        case CourseIds.StudyDictnb_pl:
        case CourseIds.StudyDictnb_pt:
        case CourseIds.StudyDictnb_ro:
        case CourseIds.StudyDictnb_el:
        case CourseIds.StudyDictnb_sl:
        case CourseIds.StudyDictnb_tr:
        case CourseIds.StudyDictnb_uk:
        case CourseIds.StudyDictnb_vi:
        case CourseIds.StudyDictnb_fi:
        case CourseIds.StudyDictnb_sv:
        case CourseIds.StudyDictnb_da:
        case CourseIds.StudyDictnb_sq:
        case CourseIds.StudyDictnb_ja:
        case CourseIds.StudyDictnb_ptbr:
        case CourseIds.StudyDictnb_zh:
        case CourseIds.StudyDictnb_ko:
        case CourseIds.StudyDictnb_ar:
        case CourseIds.StudyDictnb_he:
        case CourseIds.StudyDictnb_th:
        case CourseIds.StudyDictnb_lv:
        case CourseIds.StudyDictnb_lt:
        case CourseIds.StudyDictnb_mk:
        case CourseIds.StudyDictnb_sr:
          return LineIds.Norwegian;
        case CourseIds.StudyDictpl_en:
        case CourseIds.StudyDictpl_fr:
        case CourseIds.StudyDictpl_it:
        case CourseIds.StudyDictpl_de:
        case CourseIds.StudyDictpl_ru:
        case CourseIds.StudyDictpl_sp:
        case CourseIds.StudyDictpl_cs:
        case CourseIds.StudyDictpl_sk:
        case CourseIds.StudyDictpl_bg:
        case CourseIds.StudyDictpl_hr:
        case CourseIds.StudyDictpl_ca:
        case CourseIds.StudyDictpl_hu:
        case CourseIds.StudyDictpl_nl:
        case CourseIds.StudyDictpl_pt:
        case CourseIds.StudyDictpl_ro:
        case CourseIds.StudyDictpl_el:
        case CourseIds.StudyDictpl_sl:
        case CourseIds.StudyDictpl_tr:
        case CourseIds.StudyDictpl_uk:
        case CourseIds.StudyDictpl_vi:
        case CourseIds.StudyDictpl_fi:
        case CourseIds.StudyDictpl_sv:
        case CourseIds.StudyDictpl_da:
        case CourseIds.StudyDictpl_nb:
        case CourseIds.StudyDictpl_sq:
        case CourseIds.StudyDictpl_ja:
        case CourseIds.StudyDictpl_ptbr:
        case CourseIds.StudyDictpl_zh:
        case CourseIds.StudyDictpl_ko:
        case CourseIds.StudyDictpl_ar:
        case CourseIds.StudyDictpl_he:
        case CourseIds.StudyDictpl_th:
        case CourseIds.StudyDictpl_lv:
        case CourseIds.StudyDictpl_lt:
        case CourseIds.StudyDictpl_mk:
        case CourseIds.StudyDictpl_sr:
          return LineIds.Polish;
        case CourseIds.StudyDictpt_en:
        case CourseIds.StudyDictpt_fr:
        case CourseIds.StudyDictpt_it:
        case CourseIds.StudyDictpt_de:
        case CourseIds.StudyDictpt_ru:
        case CourseIds.StudyDictpt_sp:
        case CourseIds.StudyDictpt_cs:
        case CourseIds.StudyDictpt_sk:
        case CourseIds.StudyDictpt_bg:
        case CourseIds.StudyDictpt_hr:
        case CourseIds.StudyDictpt_ca:
        case CourseIds.StudyDictpt_hu:
        case CourseIds.StudyDictpt_nl:
        case CourseIds.StudyDictpt_pl:
        case CourseIds.StudyDictpt_ro:
        case CourseIds.StudyDictpt_el:
        case CourseIds.StudyDictpt_sl:
        case CourseIds.StudyDictpt_tr:
        case CourseIds.StudyDictpt_uk:
        case CourseIds.StudyDictpt_vi:
        case CourseIds.StudyDictpt_fi:
        case CourseIds.StudyDictpt_sv:
        case CourseIds.StudyDictpt_da:
        case CourseIds.StudyDictpt_nb:
        case CourseIds.StudyDictpt_sq:
        case CourseIds.StudyDictpt_ja:
        case CourseIds.StudyDictpt_ptbr:
        case CourseIds.StudyDictpt_zh:
        case CourseIds.StudyDictpt_ko:
        case CourseIds.StudyDictpt_ar:
        case CourseIds.StudyDictpt_he:
        case CourseIds.StudyDictpt_th:
        case CourseIds.StudyDictpt_lv:
        case CourseIds.StudyDictpt_lt:
        case CourseIds.StudyDictpt_mk:
        case CourseIds.StudyDictpt_sr:
          return LineIds.Portuguese;
        case CourseIds.StudyDictro_en:
        case CourseIds.StudyDictro_fr:
        case CourseIds.StudyDictro_it:
        case CourseIds.StudyDictro_de:
        case CourseIds.StudyDictro_ru:
        case CourseIds.StudyDictro_sp:
        case CourseIds.StudyDictro_cs:
        case CourseIds.StudyDictro_sk:
        case CourseIds.StudyDictro_bg:
        case CourseIds.StudyDictro_hr:
        case CourseIds.StudyDictro_ca:
        case CourseIds.StudyDictro_hu:
        case CourseIds.StudyDictro_nl:
        case CourseIds.StudyDictro_pl:
        case CourseIds.StudyDictro_pt:
        case CourseIds.StudyDictro_el:
        case CourseIds.StudyDictro_sl:
        case CourseIds.StudyDictro_tr:
        case CourseIds.StudyDictro_uk:
        case CourseIds.StudyDictro_vi:
        case CourseIds.StudyDictro_fi:
        case CourseIds.StudyDictro_sv:
        case CourseIds.StudyDictro_da:
        case CourseIds.StudyDictro_nb:
        case CourseIds.StudyDictro_sq:
        case CourseIds.StudyDictro_ja:
        case CourseIds.StudyDictro_ptbr:
        case CourseIds.StudyDictro_zh:
        case CourseIds.StudyDictro_ko:
        case CourseIds.StudyDictro_ar:
        case CourseIds.StudyDictro_he:
        case CourseIds.StudyDictro_th:
        case CourseIds.StudyDictro_lv:
        case CourseIds.StudyDictro_lt:
        case CourseIds.StudyDictro_mk:
        case CourseIds.StudyDictro_sr:
          return LineIds.Romanian;
        case CourseIds.StudyDictru_en:
        case CourseIds.StudyDictru_fr:
        case CourseIds.StudyDictru_it:
        case CourseIds.StudyDictru_de:
        case CourseIds.StudyDictru_sp:
        case CourseIds.StudyDictru_cs:
        case CourseIds.StudyDictru_sk:
        case CourseIds.StudyDictru_bg:
        case CourseIds.StudyDictru_hr:
        case CourseIds.StudyDictru_ca:
        case CourseIds.StudyDictru_hu:
        case CourseIds.StudyDictru_nl:
        case CourseIds.StudyDictru_pl:
        case CourseIds.StudyDictru_pt:
        case CourseIds.StudyDictru_ro:
        case CourseIds.StudyDictru_el:
        case CourseIds.StudyDictru_sl:
        case CourseIds.StudyDictru_tr:
        case CourseIds.StudyDictru_uk:
        case CourseIds.StudyDictru_vi:
        case CourseIds.StudyDictru_fi:
        case CourseIds.StudyDictru_sv:
        case CourseIds.StudyDictru_da:
        case CourseIds.StudyDictru_nb:
        case CourseIds.StudyDictru_sq:
        case CourseIds.StudyDictru_ja:
        case CourseIds.StudyDictru_ptbr:
        case CourseIds.StudyDictru_zh:
        case CourseIds.StudyDictru_ko:
        case CourseIds.StudyDictru_ar:
        case CourseIds.StudyDictru_he:
        case CourseIds.StudyDictru_th:
        case CourseIds.StudyDictru_lv:
        case CourseIds.StudyDictru_lt:
        case CourseIds.StudyDictru_mk:
        case CourseIds.StudyDictru_sr:
          return LineIds.Russian;
        case CourseIds.StudyDictsr_en:
        case CourseIds.StudyDictsr_fr:
        case CourseIds.StudyDictsr_it:
        case CourseIds.StudyDictsr_de:
        case CourseIds.StudyDictsr_ru:
        case CourseIds.StudyDictsr_sp:
        case CourseIds.StudyDictsr_cs:
        case CourseIds.StudyDictsr_sk:
        case CourseIds.StudyDictsr_bg:
        case CourseIds.StudyDictsr_hr:
        case CourseIds.StudyDictsr_ca:
        case CourseIds.StudyDictsr_hu:
        case CourseIds.StudyDictsr_nl:
        case CourseIds.StudyDictsr_pl:
        case CourseIds.StudyDictsr_pt:
        case CourseIds.StudyDictsr_ro:
        case CourseIds.StudyDictsr_el:
        case CourseIds.StudyDictsr_sl:
        case CourseIds.StudyDictsr_tr:
        case CourseIds.StudyDictsr_uk:
        case CourseIds.StudyDictsr_vi:
        case CourseIds.StudyDictsr_fi:
        case CourseIds.StudyDictsr_sv:
        case CourseIds.StudyDictsr_da:
        case CourseIds.StudyDictsr_nb:
        case CourseIds.StudyDictsr_sq:
        case CourseIds.StudyDictsr_ja:
        case CourseIds.StudyDictsr_ptbr:
        case CourseIds.StudyDictsr_zh:
        case CourseIds.StudyDictsr_ko:
        case CourseIds.StudyDictsr_ar:
        case CourseIds.StudyDictsr_he:
        case CourseIds.StudyDictsr_th:
        case CourseIds.StudyDictsr_lv:
        case CourseIds.StudyDictsr_lt:
        case CourseIds.StudyDictsr_mk:
          return LineIds.Serbian;
        case CourseIds.StudyDictsk_en:
        case CourseIds.StudyDictsk_fr:
        case CourseIds.StudyDictsk_it:
        case CourseIds.StudyDictsk_de:
        case CourseIds.StudyDictsk_ru:
        case CourseIds.StudyDictsk_sp:
        case CourseIds.StudyDictsk_cs:
        case CourseIds.StudyDictsk_bg:
        case CourseIds.StudyDictsk_hr:
        case CourseIds.StudyDictsk_ca:
        case CourseIds.StudyDictsk_hu:
        case CourseIds.StudyDictsk_nl:
        case CourseIds.StudyDictsk_pl:
        case CourseIds.StudyDictsk_pt:
        case CourseIds.StudyDictsk_ro:
        case CourseIds.StudyDictsk_el:
        case CourseIds.StudyDictsk_sl:
        case CourseIds.StudyDictsk_tr:
        case CourseIds.StudyDictsk_uk:
        case CourseIds.StudyDictsk_vi:
        case CourseIds.StudyDictsk_fi:
        case CourseIds.StudyDictsk_sv:
        case CourseIds.StudyDictsk_da:
        case CourseIds.StudyDictsk_nb:
        case CourseIds.StudyDictsk_sq:
        case CourseIds.StudyDictsk_ja:
        case CourseIds.StudyDictsk_ptbr:
        case CourseIds.StudyDictsk_zh:
        case CourseIds.StudyDictsk_ko:
        case CourseIds.StudyDictsk_ar:
        case CourseIds.StudyDictsk_he:
        case CourseIds.StudyDictsk_th:
        case CourseIds.StudyDictsk_lv:
        case CourseIds.StudyDictsk_lt:
        case CourseIds.StudyDictsk_mk:
        case CourseIds.StudyDictsk_sr:
          return LineIds.Slovak;
        case CourseIds.StudyDictsl_en:
        case CourseIds.StudyDictsl_fr:
        case CourseIds.StudyDictsl_it:
        case CourseIds.StudyDictsl_de:
        case CourseIds.StudyDictsl_ru:
        case CourseIds.StudyDictsl_sp:
        case CourseIds.StudyDictsl_cs:
        case CourseIds.StudyDictsl_sk:
        case CourseIds.StudyDictsl_bg:
        case CourseIds.StudyDictsl_hr:
        case CourseIds.StudyDictsl_ca:
        case CourseIds.StudyDictsl_hu:
        case CourseIds.StudyDictsl_nl:
        case CourseIds.StudyDictsl_pl:
        case CourseIds.StudyDictsl_pt:
        case CourseIds.StudyDictsl_ro:
        case CourseIds.StudyDictsl_el:
        case CourseIds.StudyDictsl_tr:
        case CourseIds.StudyDictsl_uk:
        case CourseIds.StudyDictsl_vi:
        case CourseIds.StudyDictsl_fi:
        case CourseIds.StudyDictsl_sv:
        case CourseIds.StudyDictsl_da:
        case CourseIds.StudyDictsl_nb:
        case CourseIds.StudyDictsl_sq:
        case CourseIds.StudyDictsl_ja:
        case CourseIds.StudyDictsl_ptbr:
        case CourseIds.StudyDictsl_zh:
        case CourseIds.StudyDictsl_ko:
        case CourseIds.StudyDictsl_ar:
        case CourseIds.StudyDictsl_he:
        case CourseIds.StudyDictsl_th:
        case CourseIds.StudyDictsl_lv:
        case CourseIds.StudyDictsl_lt:
        case CourseIds.StudyDictsl_mk:
        case CourseIds.StudyDictsl_sr:
          return LineIds.Slovenian;
        case CourseIds.StudyDictsp_en:
        case CourseIds.StudyDictsp_fr:
        case CourseIds.StudyDictsp_it:
        case CourseIds.StudyDictsp_de:
        case CourseIds.StudyDictsp_ru:
        case CourseIds.StudyDictsp_cs:
        case CourseIds.StudyDictsp_sk:
        case CourseIds.StudyDictsp_bg:
        case CourseIds.StudyDictsp_hr:
        case CourseIds.StudyDictsp_ca:
        case CourseIds.StudyDictsp_hu:
        case CourseIds.StudyDictsp_nl:
        case CourseIds.StudyDictsp_pl:
        case CourseIds.StudyDictsp_pt:
        case CourseIds.StudyDictsp_ro:
        case CourseIds.StudyDictsp_el:
        case CourseIds.StudyDictsp_sl:
        case CourseIds.StudyDictsp_tr:
        case CourseIds.StudyDictsp_uk:
        case CourseIds.StudyDictsp_vi:
        case CourseIds.StudyDictsp_fi:
        case CourseIds.StudyDictsp_sv:
        case CourseIds.StudyDictsp_da:
        case CourseIds.StudyDictsp_nb:
        case CourseIds.StudyDictsp_sq:
        case CourseIds.StudyDictsp_ja:
        case CourseIds.StudyDictsp_ptbr:
        case CourseIds.StudyDictsp_zh:
        case CourseIds.StudyDictsp_ko:
        case CourseIds.StudyDictsp_ar:
        case CourseIds.StudyDictsp_he:
        case CourseIds.StudyDictsp_th:
        case CourseIds.StudyDictsp_lv:
        case CourseIds.StudyDictsp_lt:
        case CourseIds.StudyDictsp_mk:
        case CourseIds.StudyDictsp_sr:
          return LineIds.Spanish;
        case CourseIds.StudyDictsv_en:
        case CourseIds.StudyDictsv_fr:
        case CourseIds.StudyDictsv_it:
        case CourseIds.StudyDictsv_de:
        case CourseIds.StudyDictsv_ru:
        case CourseIds.StudyDictsv_sp:
        case CourseIds.StudyDictsv_cs:
        case CourseIds.StudyDictsv_sk:
        case CourseIds.StudyDictsv_bg:
        case CourseIds.StudyDictsv_hr:
        case CourseIds.StudyDictsv_ca:
        case CourseIds.StudyDictsv_hu:
        case CourseIds.StudyDictsv_nl:
        case CourseIds.StudyDictsv_pl:
        case CourseIds.StudyDictsv_pt:
        case CourseIds.StudyDictsv_ro:
        case CourseIds.StudyDictsv_el:
        case CourseIds.StudyDictsv_sl:
        case CourseIds.StudyDictsv_tr:
        case CourseIds.StudyDictsv_uk:
        case CourseIds.StudyDictsv_vi:
        case CourseIds.StudyDictsv_fi:
        case CourseIds.StudyDictsv_da:
        case CourseIds.StudyDictsv_nb:
        case CourseIds.StudyDictsv_sq:
        case CourseIds.StudyDictsv_ja:
        case CourseIds.StudyDictsv_ptbr:
        case CourseIds.StudyDictsv_zh:
        case CourseIds.StudyDictsv_ko:
        case CourseIds.StudyDictsv_ar:
        case CourseIds.StudyDictsv_he:
        case CourseIds.StudyDictsv_th:
        case CourseIds.StudyDictsv_lv:
        case CourseIds.StudyDictsv_lt:
        case CourseIds.StudyDictsv_mk:
        case CourseIds.StudyDictsv_sr:
          return LineIds.Swedish;
        case CourseIds.StudyDictth_en:
        case CourseIds.StudyDictth_fr:
        case CourseIds.StudyDictth_it:
        case CourseIds.StudyDictth_de:
        case CourseIds.StudyDictth_ru:
        case CourseIds.StudyDictth_sp:
        case CourseIds.StudyDictth_cs:
        case CourseIds.StudyDictth_sk:
        case CourseIds.StudyDictth_bg:
        case CourseIds.StudyDictth_hr:
        case CourseIds.StudyDictth_ca:
        case CourseIds.StudyDictth_hu:
        case CourseIds.StudyDictth_nl:
        case CourseIds.StudyDictth_pl:
        case CourseIds.StudyDictth_pt:
        case CourseIds.StudyDictth_ro:
        case CourseIds.StudyDictth_el:
        case CourseIds.StudyDictth_sl:
        case CourseIds.StudyDictth_tr:
        case CourseIds.StudyDictth_uk:
        case CourseIds.StudyDictth_vi:
        case CourseIds.StudyDictth_fi:
        case CourseIds.StudyDictth_sv:
        case CourseIds.StudyDictth_da:
        case CourseIds.StudyDictth_nb:
        case CourseIds.StudyDictth_sq:
        case CourseIds.StudyDictth_ja:
        case CourseIds.StudyDictth_ptbr:
        case CourseIds.StudyDictth_zh:
        case CourseIds.StudyDictth_ko:
        case CourseIds.StudyDictth_ar:
        case CourseIds.StudyDictth_he:
        case CourseIds.StudyDictth_lv:
        case CourseIds.StudyDictth_lt:
        case CourseIds.StudyDictth_mk:
        case CourseIds.StudyDictth_sr:
          return LineIds.Thai;
        case CourseIds.StudyDicttr_en:
        case CourseIds.StudyDicttr_fr:
        case CourseIds.StudyDicttr_it:
        case CourseIds.StudyDicttr_de:
        case CourseIds.StudyDicttr_ru:
        case CourseIds.StudyDicttr_sp:
        case CourseIds.StudyDicttr_cs:
        case CourseIds.StudyDicttr_sk:
        case CourseIds.StudyDicttr_bg:
        case CourseIds.StudyDicttr_hr:
        case CourseIds.StudyDicttr_ca:
        case CourseIds.StudyDicttr_hu:
        case CourseIds.StudyDicttr_nl:
        case CourseIds.StudyDicttr_pl:
        case CourseIds.StudyDicttr_pt:
        case CourseIds.StudyDicttr_ro:
        case CourseIds.StudyDicttr_el:
        case CourseIds.StudyDicttr_sl:
        case CourseIds.StudyDicttr_uk:
        case CourseIds.StudyDicttr_vi:
        case CourseIds.StudyDicttr_fi:
        case CourseIds.StudyDicttr_sv:
        case CourseIds.StudyDicttr_da:
        case CourseIds.StudyDicttr_nb:
        case CourseIds.StudyDicttr_sq:
        case CourseIds.StudyDicttr_ja:
        case CourseIds.StudyDicttr_ptbr:
        case CourseIds.StudyDicttr_zh:
        case CourseIds.StudyDicttr_ko:
        case CourseIds.StudyDicttr_ar:
        case CourseIds.StudyDicttr_he:
        case CourseIds.StudyDicttr_th:
        case CourseIds.StudyDicttr_lv:
        case CourseIds.StudyDicttr_lt:
        case CourseIds.StudyDicttr_mk:
        case CourseIds.StudyDicttr_sr:
          return LineIds.Turkish;
        case CourseIds.StudyDictuk_en:
        case CourseIds.StudyDictuk_fr:
        case CourseIds.StudyDictuk_it:
        case CourseIds.StudyDictuk_de:
        case CourseIds.StudyDictuk_ru:
        case CourseIds.StudyDictuk_sp:
        case CourseIds.StudyDictuk_cs:
        case CourseIds.StudyDictuk_sk:
        case CourseIds.StudyDictuk_bg:
        case CourseIds.StudyDictuk_hr:
        case CourseIds.StudyDictuk_ca:
        case CourseIds.StudyDictuk_hu:
        case CourseIds.StudyDictuk_nl:
        case CourseIds.StudyDictuk_pl:
        case CourseIds.StudyDictuk_pt:
        case CourseIds.StudyDictuk_ro:
        case CourseIds.StudyDictuk_el:
        case CourseIds.StudyDictuk_sl:
        case CourseIds.StudyDictuk_tr:
        case CourseIds.StudyDictuk_vi:
        case CourseIds.StudyDictuk_fi:
        case CourseIds.StudyDictuk_sv:
        case CourseIds.StudyDictuk_da:
        case CourseIds.StudyDictuk_nb:
        case CourseIds.StudyDictuk_sq:
        case CourseIds.StudyDictuk_ja:
        case CourseIds.StudyDictuk_ptbr:
        case CourseIds.StudyDictuk_zh:
        case CourseIds.StudyDictuk_ko:
        case CourseIds.StudyDictuk_ar:
        case CourseIds.StudyDictuk_he:
        case CourseIds.StudyDictuk_th:
        case CourseIds.StudyDictuk_lv:
        case CourseIds.StudyDictuk_lt:
        case CourseIds.StudyDictuk_mk:
        case CourseIds.StudyDictuk_sr:
          return LineIds.Ukrainian;
        case CourseIds.StudyDictvi_en:
        case CourseIds.StudyDictvi_fr:
        case CourseIds.StudyDictvi_it:
        case CourseIds.StudyDictvi_de:
        case CourseIds.StudyDictvi_ru:
        case CourseIds.StudyDictvi_sp:
        case CourseIds.StudyDictvi_cs:
        case CourseIds.StudyDictvi_sk:
        case CourseIds.StudyDictvi_bg:
        case CourseIds.StudyDictvi_hr:
        case CourseIds.StudyDictvi_ca:
        case CourseIds.StudyDictvi_hu:
        case CourseIds.StudyDictvi_nl:
        case CourseIds.StudyDictvi_pl:
        case CourseIds.StudyDictvi_pt:
        case CourseIds.StudyDictvi_ro:
        case CourseIds.StudyDictvi_el:
        case CourseIds.StudyDictvi_sl:
        case CourseIds.StudyDictvi_tr:
        case CourseIds.StudyDictvi_uk:
        case CourseIds.StudyDictvi_fi:
        case CourseIds.StudyDictvi_sv:
        case CourseIds.StudyDictvi_da:
        case CourseIds.StudyDictvi_nb:
        case CourseIds.StudyDictvi_sq:
        case CourseIds.StudyDictvi_ja:
        case CourseIds.StudyDictvi_ptbr:
        case CourseIds.StudyDictvi_zh:
        case CourseIds.StudyDictvi_ko:
        case CourseIds.StudyDictvi_ar:
        case CourseIds.StudyDictvi_he:
        case CourseIds.StudyDictvi_th:
        case CourseIds.StudyDictvi_lv:
        case CourseIds.StudyDictvi_lt:
        case CourseIds.StudyDictvi_mk:
        case CourseIds.StudyDictvi_sr:
          return LineIds.Vietnamese;
        case CourseIds.MidDictbg_en:
        case CourseIds.MidDictbg_fr:
        case CourseIds.MidDictbg_it:
        case CourseIds.MidDictbg_hu:
        case CourseIds.MidDictbg_de:
        case CourseIds.MidDictbg_pl:
        case CourseIds.MidDictbg_cs:
        case CourseIds.MidDictbg_sk:
        case CourseIds.MidDictbg_hr:
        case CourseIds.MidDictbg_ro:
        case CourseIds.MidDictbg_ru:
        case CourseIds.MidDictbg_sr:
        case CourseIds.MidDictbg_uk:
        case CourseIds.MidDictbg_sp:
        case CourseIds.MidDictbg_nl:
          return LineIds.Bulgarian;
        case CourseIds.MidDicthr_en:
        case CourseIds.MidDicthr_fr:
        case CourseIds.MidDicthr_it:
        case CourseIds.MidDicthr_hu:
        case CourseIds.MidDicthr_de:
        case CourseIds.MidDicthr_pl:
        case CourseIds.MidDicthr_cs:
        case CourseIds.MidDicthr_sk:
        case CourseIds.MidDicthr_bg:
        case CourseIds.MidDicthr_ro:
        case CourseIds.MidDicthr_ru:
        case CourseIds.MidDicthr_sr:
        case CourseIds.MidDicthr_uk:
        case CourseIds.MidDicthr_sp:
        case CourseIds.MidDicthr_nl:
          return LineIds.Croatian;
        case CourseIds.MidDictcs_en:
        case CourseIds.MidDictcs_fr:
        case CourseIds.MidDictcs_it:
        case CourseIds.MidDictcs_hu:
        case CourseIds.MidDictcs_de:
        case CourseIds.MidDictcs_pl:
        case CourseIds.MidDictcs_sk:
        case CourseIds.MidDictcs_bg:
        case CourseIds.MidDictcs_hr:
        case CourseIds.MidDictcs_ro:
        case CourseIds.MidDictcs_ru:
        case CourseIds.MidDictcs_sr:
        case CourseIds.MidDictcs_uk:
        case CourseIds.MidDictcs_sp:
        case CourseIds.MidDictcs_nl:
          return LineIds.Czech;
        case CourseIds.MidDictnl_en:
        case CourseIds.MidDictnl_fr:
        case CourseIds.MidDictnl_it:
        case CourseIds.MidDictnl_hu:
        case CourseIds.MidDictnl_de:
        case CourseIds.MidDictnl_pl:
        case CourseIds.MidDictnl_cs:
        case CourseIds.MidDictnl_sk:
        case CourseIds.MidDictnl_bg:
        case CourseIds.MidDictnl_hr:
        case CourseIds.MidDictnl_ro:
        case CourseIds.MidDictnl_ru:
        case CourseIds.MidDictnl_sr:
        case CourseIds.MidDictnl_uk:
        case CourseIds.MidDictnl_sp:
          return LineIds.Dutch;
        case CourseIds.MidDicten_fr:
        case CourseIds.MidDicten_it:
        case CourseIds.MidDicten_hu:
        case CourseIds.MidDicten_de:
        case CourseIds.MidDicten_pl:
        case CourseIds.MidDicten_cs:
        case CourseIds.MidDicten_sk:
        case CourseIds.MidDicten_bg:
        case CourseIds.MidDicten_hr:
        case CourseIds.MidDicten_ro:
        case CourseIds.MidDicten_ru:
        case CourseIds.MidDicten_sr:
        case CourseIds.MidDicten_uk:
        case CourseIds.MidDicten_sp:
        case CourseIds.MidDicten_nl:
          return LineIds.English;
        case CourseIds.MidDictfr_en:
        case CourseIds.MidDictfr_it:
        case CourseIds.MidDictfr_hu:
        case CourseIds.MidDictfr_de:
        case CourseIds.MidDictfr_pl:
        case CourseIds.MidDictfr_cs:
        case CourseIds.MidDictfr_sk:
        case CourseIds.MidDictfr_bg:
        case CourseIds.MidDictfr_hr:
        case CourseIds.MidDictfr_ro:
        case CourseIds.MidDictfr_ru:
        case CourseIds.MidDictfr_sr:
        case CourseIds.MidDictfr_uk:
        case CourseIds.MidDictfr_sp:
        case CourseIds.MidDictfr_nl:
          return LineIds.French;
        case CourseIds.MidDictde_en:
        case CourseIds.MidDictde_fr:
        case CourseIds.MidDictde_it:
        case CourseIds.MidDictde_hu:
        case CourseIds.MidDictde_pl:
        case CourseIds.MidDictde_cs:
        case CourseIds.MidDictde_sk:
        case CourseIds.MidDictde_bg:
        case CourseIds.MidDictde_hr:
        case CourseIds.MidDictde_ro:
        case CourseIds.MidDictde_ru:
        case CourseIds.MidDictde_sr:
        case CourseIds.MidDictde_uk:
        case CourseIds.MidDictde_sp:
        case CourseIds.MidDictde_nl:
          return LineIds.German;
        case CourseIds.MidDicthu_en:
        case CourseIds.MidDicthu_fr:
        case CourseIds.MidDicthu_it:
        case CourseIds.MidDicthu_de:
        case CourseIds.MidDicthu_pl:
        case CourseIds.MidDicthu_cs:
        case CourseIds.MidDicthu_sk:
        case CourseIds.MidDicthu_bg:
        case CourseIds.MidDicthu_hr:
        case CourseIds.MidDicthu_ro:
        case CourseIds.MidDicthu_ru:
        case CourseIds.MidDicthu_sr:
        case CourseIds.MidDicthu_uk:
        case CourseIds.MidDicthu_sp:
        case CourseIds.MidDicthu_nl:
          return LineIds.Hungarian;
        case CourseIds.MidDictit_en:
        case CourseIds.MidDictit_fr:
        case CourseIds.MidDictit_hu:
        case CourseIds.MidDictit_de:
        case CourseIds.MidDictit_pl:
        case CourseIds.MidDictit_cs:
        case CourseIds.MidDictit_sk:
        case CourseIds.MidDictit_bg:
        case CourseIds.MidDictit_hr:
        case CourseIds.MidDictit_ro:
        case CourseIds.MidDictit_ru:
        case CourseIds.MidDictit_sr:
        case CourseIds.MidDictit_uk:
        case CourseIds.MidDictit_sp:
        case CourseIds.MidDictit_nl:
          return LineIds.Italian;
        case CourseIds.MidDictpl_en:
        case CourseIds.MidDictpl_fr:
        case CourseIds.MidDictpl_it:
        case CourseIds.MidDictpl_hu:
        case CourseIds.MidDictpl_de:
        case CourseIds.MidDictpl_cs:
        case CourseIds.MidDictpl_sk:
        case CourseIds.MidDictpl_bg:
        case CourseIds.MidDictpl_hr:
        case CourseIds.MidDictpl_ro:
        case CourseIds.MidDictpl_ru:
        case CourseIds.MidDictpl_sr:
        case CourseIds.MidDictpl_uk:
        case CourseIds.MidDictpl_sp:
        case CourseIds.MidDictpl_nl:
          return LineIds.Polish;
        case CourseIds.MidDictro_en:
        case CourseIds.MidDictro_fr:
        case CourseIds.MidDictro_it:
        case CourseIds.MidDictro_hu:
        case CourseIds.MidDictro_de:
        case CourseIds.MidDictro_pl:
        case CourseIds.MidDictro_cs:
        case CourseIds.MidDictro_sk:
        case CourseIds.MidDictro_bg:
        case CourseIds.MidDictro_hr:
        case CourseIds.MidDictro_ru:
        case CourseIds.MidDictro_sr:
        case CourseIds.MidDictro_uk:
        case CourseIds.MidDictro_sp:
        case CourseIds.MidDictro_nl:
          return LineIds.Romanian;
        case CourseIds.MidDictru_en:
        case CourseIds.MidDictru_fr:
        case CourseIds.MidDictru_it:
        case CourseIds.MidDictru_hu:
        case CourseIds.MidDictru_de:
        case CourseIds.MidDictru_pl:
        case CourseIds.MidDictru_cs:
        case CourseIds.MidDictru_sk:
        case CourseIds.MidDictru_bg:
        case CourseIds.MidDictru_hr:
        case CourseIds.MidDictru_ro:
        case CourseIds.MidDictru_sr:
        case CourseIds.MidDictru_uk:
        case CourseIds.MidDictru_sp:
        case CourseIds.MidDictru_nl:
          return LineIds.Russian;
        case CourseIds.MidDictsr_en:
        case CourseIds.MidDictsr_fr:
        case CourseIds.MidDictsr_it:
        case CourseIds.MidDictsr_hu:
        case CourseIds.MidDictsr_de:
        case CourseIds.MidDictsr_pl:
        case CourseIds.MidDictsr_cs:
        case CourseIds.MidDictsr_sk:
        case CourseIds.MidDictsr_bg:
        case CourseIds.MidDictsr_hr:
        case CourseIds.MidDictsr_ro:
        case CourseIds.MidDictsr_ru:
        case CourseIds.MidDictsr_uk:
        case CourseIds.MidDictsr_sp:
        case CourseIds.MidDictsr_nl:
          return LineIds.Serbian;
        case CourseIds.MidDictsk_en:
        case CourseIds.MidDictsk_fr:
        case CourseIds.MidDictsk_it:
        case CourseIds.MidDictsk_hu:
        case CourseIds.MidDictsk_de:
        case CourseIds.MidDictsk_pl:
        case CourseIds.MidDictsk_cs:
        case CourseIds.MidDictsk_bg:
        case CourseIds.MidDictsk_hr:
        case CourseIds.MidDictsk_ro:
        case CourseIds.MidDictsk_ru:
        case CourseIds.MidDictsk_sr:
        case CourseIds.MidDictsk_uk:
        case CourseIds.MidDictsk_sp:
        case CourseIds.MidDictsk_nl:
          return LineIds.Slovak;
        case CourseIds.MidDictsp_en:
        case CourseIds.MidDictsp_fr:
        case CourseIds.MidDictsp_it:
        case CourseIds.MidDictsp_hu:
        case CourseIds.MidDictsp_de:
        case CourseIds.MidDictsp_pl:
        case CourseIds.MidDictsp_cs:
        case CourseIds.MidDictsp_sk:
        case CourseIds.MidDictsp_bg:
        case CourseIds.MidDictsp_hr:
        case CourseIds.MidDictsp_ro:
        case CourseIds.MidDictsp_ru:
        case CourseIds.MidDictsp_sr:
        case CourseIds.MidDictsp_uk:
        case CourseIds.MidDictsp_nl:
          return LineIds.Spanish;
        case CourseIds.MidDictuk_en:
        case CourseIds.MidDictuk_fr:
        case CourseIds.MidDictuk_it:
        case CourseIds.MidDictuk_hu:
        case CourseIds.MidDictuk_de:
        case CourseIds.MidDictuk_pl:
        case CourseIds.MidDictuk_cs:
        case CourseIds.MidDictuk_sk:
        case CourseIds.MidDictuk_bg:
        case CourseIds.MidDictuk_hr:
        case CourseIds.MidDictuk_ro:
        case CourseIds.MidDictuk_ru:
        case CourseIds.MidDictuk_sr:
        case CourseIds.MidDictuk_sp:
        case CourseIds.MidDictuk_nl:
          return LineIds.Ukrainian;
        #endregion
        default: return LineIds.no;
      }
    }

    public static Langs CourseIdToLang(CourseIds line, bool raiseExp = true) {
      switch (line) {
        case CourseIds.MSOffice2007_EN:
        case CourseIds.MSWord2007_EN:
        case CourseIds.MSExcel2007_EN:
        case CourseIds.MSPowerPoint2007_EN:
        case CourseIds.MSOutlook2007_EN:
        case CourseIds.MSAccess2007_EN:
        case CourseIds.Internet_EN:
        case CourseIds.WindowsVista_EN:
        case CourseIds.MSEcdl5_EN:
        case CourseIds.MSEcdl6_EN:
        case CourseIds.MSOffice2010_EN:
        case CourseIds.MSWord2010_EN:
        case CourseIds.MSExcel2010_EN:
        case CourseIds.MSPowerPoint2010_EN:
        case CourseIds.MSOutlook2010_EN:
        case CourseIds.MSAccess2010_EN:
        case CourseIds.Windows7_EN:
          return Langs.en_gb;
        case CourseIds.MSOffice2007_SK:
        case CourseIds.MSWord2007_SK:
        case CourseIds.MSExcel2007_SK:
        case CourseIds.MSPowerPoint2007_SK:
        case CourseIds.MSOutlook2007_SK:
        case CourseIds.MSAccess2007_SK:
        case CourseIds.Internet_SK:
        case CourseIds.WindowsVista_SK:
        case CourseIds.MSEcdl5_SK:
        case CourseIds.MSEcdl6_SK:
        case CourseIds.MSOffice2010_SK:
        case CourseIds.MSWord2010_SK:
        case CourseIds.MSExcel2010_SK:
        case CourseIds.MSPowerPoint2010_SK:
        case CourseIds.MSOutlook2010_SK:
        case CourseIds.MSAccess2010_SK:
        case CourseIds.Windows7_SK:
          return Langs.sk_sk;
        case CourseIds.MSExcel2007:
        case CourseIds.MSWord2007:
        case CourseIds.MSOffice2007:
        case CourseIds.MSExcel2003:
        case CourseIds.MSWord2003:
        case CourseIds.MSOffice2003:
        case CourseIds.MSExcel2000:
        case CourseIds.MSWord2000:
        case CourseIds.MSOffice2000:
        case CourseIds.MSExcelXP:
        case CourseIds.MSWordXP:
        case CourseIds.MSOfficeXP:
        case CourseIds.MSEcdl: // verze 4
        case CourseIds.MSEcdl5:
        case CourseIds.MSEcdl6:
        case CourseIds.MSPowerPoint2007:
        case CourseIds.MSOutlook2007:
        case CourseIds.MSAccess2007:
        case CourseIds.Internet:
        case CourseIds.WindowsVista:
        case CourseIds.MSOffice2010:
        case CourseIds.MSWord2010:
        case CourseIds.MSExcel2010:
        case CourseIds.MSPowerPoint2010:
        case CourseIds.MSOutlook2010:
        case CourseIds.MSAccess2010:
        case CourseIds.Windows7:
        case CourseIds.GopasNewProduct5:
        case CourseIds.GopasNewProduct6:
        case CourseIds.GopasNewProduct7:
        case CourseIds.GopasNewProduct8:
        case CourseIds.GopasNewProduct9:
        case CourseIds.GopasNewProduct10:
        case CourseIds.GopasNewProduct11:
        case CourseIds.GopasNewProduct12:
        case CourseIds.GopasNewProduct13:
        case CourseIds.GopasNewProduct14:
        case CourseIds.GopasNewProduct15:
        case CourseIds.GopasNewProduct16:
        case CourseIds.GopasNewProduct17:
        case CourseIds.GopasNewProduct18:
        case CourseIds.GopasNewProduct19:
        case CourseIds.GopasNewProduct20:
          return Langs.cs_cz;
        case CourseIds.MSOffice2010_DE:
        case CourseIds.MSWord2010_DE:
        case CourseIds.MSExcel2010_DE:
        case CourseIds.MSPowerPoint2010_DE:
        case CourseIds.MSOutlook2010_DE:
        case CourseIds.Windows7_DE:
          return Langs.de_de;
        case CourseIds.MSOffice2010_FR:
        case CourseIds.MSWord2010_FR:
        case CourseIds.MSExcel2010_FR:
        case CourseIds.MSPowerPoint2010_FR:
        case CourseIds.MSOutlook2010_FR:
        case CourseIds.MSAccess2010_FR:
        case CourseIds.Windows7_FR:
          return Langs.fr_fr;
        //
        case CourseIds.RewiseEnglish:
        case CourseIds.English:
        case CourseIds.EnglishE:
        case CourseIds.ElementsAndTest:
        case CourseIds.EuroEnglish:
        case CourseIds.IsEduLand_EuroEnglish:
        case CourseIds.EnglishBerlitz:
        case CourseIds.eTestMe_EnglishBig:
        case CourseIds.eTestMe_EnglishSmall:
        case CourseIds.VNEng3:
        case CourseIds.VNEng4:
        case CourseIds.VNEng5:
        case CourseIds.VNEng6:
        case CourseIds.VNEng7:
        case CourseIds.VNEng8:
        case CourseIds.VNEng9:
        case CourseIds.VNEng10:
        case CourseIds.VNEng11:
        case CourseIds.VNEng12:
          //case CourseIds.Pronunc_English:
          return Langs.en_gb;
        case CourseIds.RewiseFrench:
        case CourseIds.French:
        case CourseIds.FrenchBerlitz:
          return Langs.fr_fr;
        case CourseIds.RewiseGerman:
        case CourseIds.German:
        case CourseIds.GermanBerlitz:
          //case CourseIds.Pronunc_German:
          return Langs.de_de;
        case CourseIds.RewiseItalian:
        case CourseIds.Italian:
        case CourseIds.ItalianBerlitz:
          return Langs.it_it;
        case CourseIds.RewiseRussian:
        case CourseIds.Russian:
        case CourseIds.RussianBerlitz:
          return Langs.ru_ru;
        case CourseIds.RewiseSpanish:
        case CourseIds.Spanish:
        case CourseIds.SpanishBerlitz:
          return Langs.sp_sp;
        #region EuroTalk5
        case CourseIds.TN_Afrikaans: return Langs.af_za;
        case CourseIds.TN_Albanian: return Langs.sq_al;
        case CourseIds.TN_Arabic: return Langs.ar_sa;
        case CourseIds.TN_Arabic_Classical: return Langs.ar_sa;
        case CourseIds.TN_Arabic_Modern_Standard: return Langs.ar_sa;
        case CourseIds.TN_Armenian: return Langs.hy_am;
        case CourseIds.TN_Assamese: return Langs.as_in;
        case CourseIds.TN_Azeri: return Langs.az_latn_az;
        case CourseIds.TN_Basque: return Langs.eu_es;
        case CourseIds.TN_Bengali: return Langs.bn_in;
        case CourseIds.TN_Portuguese_Brazilian: return Langs.pt_br;
        case CourseIds.TN_Breton: return Langs.br_fr;
        case CourseIds.TN_Bossna: return Langs.bs;
        case CourseIds.TN_Bulgarian: return Langs.bg_bg;
        case CourseIds.TN_French_Canadian: return Langs.fr_fr;
        case CourseIds.TN_Cantonese: return Langs.zh_hk;
        case CourseIds.TN_Catalan: return Langs.ca_es;
        case CourseIds.TN_Corsican: return Langs.co_fr;
        case CourseIds.TN_Croatian: return Langs.hr_hr;
        case CourseIds.TN_Czech: return Langs.cs_cz;
        case CourseIds.TN_Danish: return Langs.da_dk;
        case CourseIds.TN_Dutch: return Langs.nl_nl;
        case CourseIds.TN_English: return Langs.en_gb;
        case CourseIds.TN_English_American: return Langs.en_gb;
        case CourseIds.TN_Estonian: return Langs.et_ee;
        case CourseIds.TN_Finnish: return Langs.fi_fi;
        case CourseIds.TN_French: return Langs.fr_fr;
        case CourseIds.TN_Galician: return Langs.gl_es;
        case CourseIds.TN_Georgian: return Langs.ka_ge;
        case CourseIds.TN_German: return Langs.de_de;
        case CourseIds.TN_Greek: return Langs.el_gr;
        case CourseIds.TN_Hausa: return Langs.ha_latn_ng;
        case CourseIds.TN_Hebrew: return Langs.he_il;
        case CourseIds.TN_Hungarian: return Langs.hu_hu;
        case CourseIds.TN_Chinese_Mandarin: return Langs.zh_cn;
        case CourseIds.TN_Icelandic: return Langs.is_is;
        case CourseIds.TN_Igbo: return Langs.ig_ng;
        case CourseIds.TN_Indonesian: return Langs.id_id;
        case CourseIds.TN_Irish: return Langs.ga_ie;
        case CourseIds.TN_Italian: return Langs.it_it;
        case CourseIds.TN_Japanese: return Langs.ja_jp;
        case CourseIds.TN_Khmer: return Langs.km_kh;
        case CourseIds.TN_Kirghiz: return Langs.ky_kg;
        case CourseIds.TN_Korean: return Langs.ko_kr;
        case CourseIds.TN_Spanish_Latin_American: return Langs.sp_sp;
        case CourseIds.TN_Latvian: return Langs.lv_lv;
        case CourseIds.TN_Lithuanian: return Langs.lt_lt;
        case CourseIds.TN_Macedonian: return Langs.mk_mk;
        case CourseIds.TN_Malay: return Langs.ms_my;
        case CourseIds.TN_Malayalam: return Langs.ml_in;
        case CourseIds.TN_Maltese: return Langs.mt_mt;
        case CourseIds.TN_Maori: return Langs.mi_nz;
        case CourseIds.TN_Mongolian: return Langs.mn_mn;
        case CourseIds.TN_Occitan: return Langs.oc_fr;
        case CourseIds.TN_Norwegian: return Langs.nb_no;
        case CourseIds.TN_Pashto: return Langs.ps_af;
        case CourseIds.TN_Persian: return Langs.fa_ir;
        case CourseIds.TN_Polish: return Langs.pl_pl;
        case CourseIds.TN_Portuguese: return Langs.pt_pt;
        case CourseIds.TN_Quechua: return Langs.quz_pe;
        case CourseIds.TN_Romanian: return Langs.ro_ro;
        case CourseIds.TN_Russian: return Langs.ru_ru;
        case CourseIds.TN_Serbian: return Langs.sr_latn_cs;
        case CourseIds.TN_Sesotho: return Langs.nso_za;
        case CourseIds.TN_Slovak: return Langs.sk_sk;
        case CourseIds.TN_Slovenian: return Langs.sl_si;
        case CourseIds.TN_Spanish: return Langs.sp_sp;
        case CourseIds.TN_Swahili: return Langs.sw_ke;
        case CourseIds.TN_Swedish: return Langs.sv_se;
        case CourseIds.TN_Thai: return Langs.th_th;
        case CourseIds.TN_Tibetan: return Langs.bo_cn;
        case CourseIds.TN_Tswana: return Langs.tn_za;
        case CourseIds.TN_Turkish: return Langs.tr_tr;
        case CourseIds.TN_Ukrainian: return Langs.uk_ua;
        case CourseIds.TN_Urdu: return Langs.ur_pk;
        case CourseIds.TN_Uzbek: return Langs.uz_latn_uz;
        case CourseIds.TN_Vietnamese: return Langs.vi_vn;
        case CourseIds.TN_Xhosa: return Langs.xh_za;
        case CourseIds.TN_Yoruba: return Langs.yo_ng;
        case CourseIds.TN_Zulu: return Langs.zu_za;
        case CourseIds.TN_Audio_Afrikaans: return Langs.af_za;
        case CourseIds.TN_Audio_Albanian: return Langs.sq_al;
        case CourseIds.TN_Audio_Arabic: return Langs.ar_sa;
        case CourseIds.TN_Audio_Arabic_Classical: return Langs.ar_sa;
        case CourseIds.TN_Audio_Arabic_Modern_Standard: return Langs.ar_sa;
        case CourseIds.TN_Audio_Armenian: return Langs.hy_am;
        case CourseIds.TN_Audio_Assamese: return Langs.as_in;
        case CourseIds.TN_Audio_Azeri: return Langs.az_latn_az;
        case CourseIds.TN_Audio_Basque: return Langs.eu_es;
        case CourseIds.TN_Audio_Bengali: return Langs.bn_in;
        case CourseIds.TN_Audio_Portuguese_Brazilian: return Langs.pt_br;
        case CourseIds.TN_Audio_Breton: return Langs.br_fr;
        case CourseIds.TN_Audio_Bulgarian: return Langs.bg_bg;
        case CourseIds.TN_Audio_Bossna: return Langs.bs;
        case CourseIds.TN_Audio_French_Canadian: return Langs.fr_fr;
        case CourseIds.TN_Audio_Cantonese: return Langs.zh_hk;
        case CourseIds.TN_Audio_Catalan: return Langs.ca_es;
        case CourseIds.TN_Audio_Corsican: return Langs.co_fr;
        case CourseIds.TN_Audio_Croatian: return Langs.hr_hr;
        case CourseIds.TN_Audio_Czech: return Langs.cs_cz;
        case CourseIds.TN_Audio_Danish: return Langs.da_dk;
        case CourseIds.TN_Audio_Dutch: return Langs.nl_nl;
        case CourseIds.TN_Audio_English: return Langs.en_gb;
        case CourseIds.TN_Audio_English_American: return Langs.en_gb;
        case CourseIds.TN_Audio_Estonian: return Langs.et_ee;
        case CourseIds.TN_Audio_Finnish: return Langs.fi_fi;
        case CourseIds.TN_Audio_French: return Langs.fr_fr;
        case CourseIds.TN_Audio_Galician: return Langs.gl_es;
        case CourseIds.TN_Audio_Georgian: return Langs.ka_ge;
        case CourseIds.TN_Audio_German: return Langs.de_de;
        case CourseIds.TN_Audio_Greek: return Langs.el_gr;
        case CourseIds.TN_Audio_Hausa: return Langs.ha_latn_ng;
        case CourseIds.TN_Audio_Hebrew: return Langs.he_il;
        case CourseIds.TN_Audio_Hungarian: return Langs.hu_hu;
        case CourseIds.TN_Audio_Chinese_Mandarin: return Langs.zh_cn;
        case CourseIds.TN_Audio_Icelandic: return Langs.is_is;
        case CourseIds.TN_Audio_Igbo: return Langs.ig_ng;
        case CourseIds.TN_Audio_Indonesian: return Langs.id_id;
        case CourseIds.TN_Audio_Irish: return Langs.ga_ie;
        case CourseIds.TN_Audio_Italian: return Langs.it_it;
        case CourseIds.TN_Audio_Japanese: return Langs.ja_jp;
        case CourseIds.TN_Audio_Khmer: return Langs.km_kh;
        case CourseIds.TN_Audio_Kirghiz: return Langs.ky_kg;
        case CourseIds.TN_Audio_Korean: return Langs.ko_kr;
        case CourseIds.TN_Audio_Spanish_Latin_American: return Langs.sp_sp;
        case CourseIds.TN_Audio_Latvian: return Langs.lv_lv;
        case CourseIds.TN_Audio_Lithuanian: return Langs.lt_lt;
        case CourseIds.TN_Audio_Macedonian: return Langs.mk_mk;
        case CourseIds.TN_Audio_Malay: return Langs.ms_my;
        case CourseIds.TN_Audio_Malayalam: return Langs.ml_in;
        case CourseIds.TN_Audio_Maltese: return Langs.mt_mt;
        case CourseIds.TN_Audio_Maori: return Langs.mi_nz;
        case CourseIds.TN_Audio_Mongolian: return Langs.mn_mn;
        case CourseIds.TN_Audio_Occitan: return Langs.oc_fr;
        case CourseIds.TN_Audio_Norwegian: return Langs.nb_no;
        case CourseIds.TN_Audio_Pashto: return Langs.ps_af;
        case CourseIds.TN_Audio_Persian: return Langs.fa_ir;
        case CourseIds.TN_Audio_Polish: return Langs.pl_pl;
        case CourseIds.TN_Audio_Portuguese: return Langs.pt_pt;
        case CourseIds.TN_Audio_Quechua: return Langs.quz_pe;
        case CourseIds.TN_Audio_Romanian: return Langs.ro_ro;
        case CourseIds.TN_Audio_Russian: return Langs.ru_ru;
        case CourseIds.TN_Audio_Serbian: return Langs.sr_latn_cs;
        case CourseIds.TN_Audio_Sesotho: return Langs.nso_za;
        case CourseIds.TN_Audio_Slovak: return Langs.sk_sk;
        case CourseIds.TN_Audio_Slovenian: return Langs.sl_si;
        case CourseIds.TN_Audio_Spanish: return Langs.sp_sp;
        case CourseIds.TN_Audio_Swahili: return Langs.sw_ke;
        case CourseIds.TN_Audio_Swedish: return Langs.sv_se;
        case CourseIds.TN_Audio_Thai: return Langs.th_th;
        case CourseIds.TN_Audio_Tibetan: return Langs.bo_cn;
        case CourseIds.TN_Audio_Tswana: return Langs.tn_za;
        case CourseIds.TN_Audio_Turkish: return Langs.tr_tr;
        case CourseIds.TN_Audio_Ukrainian: return Langs.uk_ua;
        case CourseIds.TN_Audio_Urdu: return Langs.ur_pk;
        case CourseIds.TN_Audio_Uzbek: return Langs.uz_latn_uz;
        case CourseIds.TN_Audio_Vietnamese: return Langs.vi_vn;
        case CourseIds.TN_Audio_Xhosa: return Langs.xh_za;
        case CourseIds.TN_Audio_Yoruba: return Langs.yo_ng;
        case CourseIds.TN_Audio_Zulu: return Langs.zu_za;
        case CourseIds.TN_Pronunc_Afrikaans: return Langs.af_za;
        case CourseIds.TN_Pronunc_Albanian: return Langs.sq_al;
        case CourseIds.TN_Pronunc_Arabic: return Langs.ar_sa;
        case CourseIds.TN_Pronunc_Arabic_Classical: return Langs.ar_sa;
        case CourseIds.TN_Pronunc_Arabic_Modern_Standard: return Langs.ar_sa;
        case CourseIds.TN_Pronunc_Armenian: return Langs.hy_am;
        case CourseIds.TN_Pronunc_Assamese: return Langs.as_in;
        case CourseIds.TN_Pronunc_Azeri: return Langs.az_latn_az;
        case CourseIds.TN_Pronunc_Basque: return Langs.eu_es;
        case CourseIds.TN_Pronunc_Bengali: return Langs.bn_in;
        case CourseIds.TN_Pronunc_Portuguese_Brazilian: return Langs.pt_br;
        case CourseIds.TN_Pronunc_Breton: return Langs.br_fr;
        case CourseIds.TN_Pronunc_Bulgarian: return Langs.bg_bg;
        case CourseIds.TN_Pronunc_French_Canadian: return Langs.fr_fr;
        case CourseIds.TN_Pronunc_Cantonese: return Langs.zh_hk;
        case CourseIds.TN_Pronunc_Catalan: return Langs.ca_es;
        case CourseIds.TN_Pronunc_Corsican: return Langs.co_fr;
        case CourseIds.TN_Pronunc_Croatian: return Langs.hr_hr;
        case CourseIds.TN_Pronunc_Czech: return Langs.cs_cz;
        case CourseIds.TN_Pronunc_Danish: return Langs.da_dk;
        case CourseIds.TN_Pronunc_Dutch: return Langs.nl_nl;
        case CourseIds.TN_Pronunc_English: return Langs.en_gb;
        case CourseIds.TN_Pronunc_English_American: return Langs.en_gb;
        case CourseIds.TN_Pronunc_Estonian: return Langs.et_ee;
        case CourseIds.TN_Pronunc_Finnish: return Langs.fi_fi;
        case CourseIds.TN_Pronunc_French: return Langs.fr_fr;
        case CourseIds.TN_Pronunc_Galician: return Langs.gl_es;
        case CourseIds.TN_Pronunc_Georgian: return Langs.ka_ge;
        case CourseIds.TN_Pronunc_German: return Langs.de_de;
        case CourseIds.TN_Pronunc_Greek: return Langs.el_gr;
        case CourseIds.TN_Pronunc_Hausa: return Langs.ha_latn_ng;
        case CourseIds.TN_Pronunc_Hebrew: return Langs.he_il;
        case CourseIds.TN_Pronunc_Hungarian: return Langs.hu_hu;
        case CourseIds.TN_Pronunc_Chinese_Mandarin: return Langs.zh_cn;
        case CourseIds.TN_Pronunc_Icelandic: return Langs.is_is;
        case CourseIds.TN_Pronunc_Igbo: return Langs.ig_ng;
        case CourseIds.TN_Pronunc_Indonesian: return Langs.id_id;
        case CourseIds.TN_Pronunc_Irish: return Langs.ga_ie;
        case CourseIds.TN_Pronunc_Italian: return Langs.it_it;
        case CourseIds.TN_Pronunc_Japanese: return Langs.ja_jp;
        case CourseIds.TN_Pronunc_Khmer: return Langs.km_kh;
        case CourseIds.TN_Pronunc_Kirghiz: return Langs.ky_kg;
        case CourseIds.TN_Pronunc_Korean: return Langs.ko_kr;
        case CourseIds.TN_Pronunc_Spanish_Latin_American: return Langs.sp_sp;
        case CourseIds.TN_Pronunc_Latvian: return Langs.lv_lv;
        case CourseIds.TN_Pronunc_Lithuanian: return Langs.lt_lt;
        case CourseIds.TN_Pronunc_Macedonian: return Langs.mk_mk;
        case CourseIds.TN_Pronunc_Malay: return Langs.ms_my;
        case CourseIds.TN_Pronunc_Malayalam: return Langs.ml_in;
        case CourseIds.TN_Pronunc_Maltese: return Langs.mt_mt;
        case CourseIds.TN_Pronunc_Maori: return Langs.mi_nz;
        case CourseIds.TN_Pronunc_Mongolian: return Langs.mn_mn;
        case CourseIds.TN_Pronunc_Occitan: return Langs.oc_fr;
        case CourseIds.TN_Pronunc_Norwegian: return Langs.nb_no;
        case CourseIds.TN_Pronunc_Pashto: return Langs.ps_af;
        case CourseIds.TN_Pronunc_Persian: return Langs.fa_ir;
        case CourseIds.TN_Pronunc_Polish: return Langs.pl_pl;
        case CourseIds.TN_Pronunc_Portuguese: return Langs.pt_pt;
        case CourseIds.TN_Pronunc_Quechua: return Langs.quz_pe;
        case CourseIds.TN_Pronunc_Romanian: return Langs.ro_ro;
        case CourseIds.TN_Pronunc_Russian: return Langs.ru_ru;
        case CourseIds.TN_Pronunc_Serbian: return Langs.sr_latn_cs;
        case CourseIds.TN_Pronunc_Sesotho: return Langs.nso_za;
        case CourseIds.TN_Pronunc_Slovak: return Langs.sk_sk;
        case CourseIds.TN_Pronunc_Slovenian: return Langs.sl_si;
        case CourseIds.TN_Pronunc_Spanish: return Langs.sp_sp;
        case CourseIds.TN_Pronunc_Swahili: return Langs.sw_ke;
        case CourseIds.TN_Pronunc_Swedish: return Langs.sv_se;
        case CourseIds.TN_Pronunc_Thai: return Langs.th_th;
        case CourseIds.TN_Pronunc_Tibetan: return Langs.bo_cn;
        case CourseIds.TN_Pronunc_Tswana: return Langs.tn_za;
        case CourseIds.TN_Pronunc_Turkish: return Langs.tr_tr;
        case CourseIds.TN_Pronunc_Ukrainian: return Langs.uk_ua;
        case CourseIds.TN_Pronunc_Urdu: return Langs.ur_pk;
        case CourseIds.TN_Pronunc_Uzbek: return Langs.uz_latn_uz;
        case CourseIds.TN_Pronunc_Vietnamese: return Langs.vi_vn;
        case CourseIds.TN_Pronunc_Xhosa: return Langs.xh_za;
        case CourseIds.TN_Pronunc_Yoruba: return Langs.yo_ng;
        case CourseIds.TN_Pronunc_Zulu: return Langs.zu_za;
        case CourseIds.TN_Rewise_Afrikaans: return Langs.af_za;
        case CourseIds.TN_Rewise_Albanian: return Langs.sq_al;
        case CourseIds.TN_Rewise_Arabic: return Langs.ar_sa;
        case CourseIds.TN_Rewise_Arabic_Classical: return Langs.ar_sa;
        case CourseIds.TN_Rewise_Arabic_Modern_Standard: return Langs.ar_sa;
        case CourseIds.TN_Rewise_Armenian: return Langs.hy_am;
        case CourseIds.TN_Rewise_Assamese: return Langs.as_in;
        case CourseIds.TN_Rewise_Azeri: return Langs.az_latn_az;
        case CourseIds.TN_Rewise_Basque: return Langs.eu_es;
        case CourseIds.TN_Rewise_Bengali: return Langs.bn_in;
        case CourseIds.TN_Rewise_Portuguese_Brazilian: return Langs.pt_br;
        case CourseIds.TN_Rewise_Breton: return Langs.br_fr;
        case CourseIds.TN_Rewise_Bulgarian: return Langs.bg_bg;
        case CourseIds.TN_Rewise_French_Canadian: return Langs.fr_fr;
        case CourseIds.TN_Rewise_Cantonese: return Langs.zh_hk;
        case CourseIds.TN_Rewise_Catalan: return Langs.ca_es;
        case CourseIds.TN_Rewise_Corsican: return Langs.co_fr;
        case CourseIds.TN_Rewise_Croatian: return Langs.hr_hr;
        case CourseIds.TN_Rewise_Czech: return Langs.cs_cz;
        case CourseIds.TN_Rewise_Danish: return Langs.da_dk;
        case CourseIds.TN_Rewise_Dutch: return Langs.nl_nl;
        case CourseIds.TN_Rewise_English: return Langs.en_gb;
        case CourseIds.TN_Rewise_English_American: return Langs.en_gb;
        case CourseIds.TN_Rewise_Estonian: return Langs.et_ee;
        case CourseIds.TN_Rewise_Finnish: return Langs.fi_fi;
        case CourseIds.TN_Rewise_French: return Langs.fr_fr;
        case CourseIds.TN_Rewise_Galician: return Langs.gl_es;
        case CourseIds.TN_Rewise_Georgian: return Langs.ka_ge;
        case CourseIds.TN_Rewise_German: return Langs.de_de;
        case CourseIds.TN_Rewise_Greek: return Langs.el_gr;
        case CourseIds.TN_Rewise_Hausa: return Langs.ha_latn_ng;
        case CourseIds.TN_Rewise_Hebrew: return Langs.he_il;
        case CourseIds.TN_Rewise_Hungarian: return Langs.hu_hu;
        case CourseIds.TN_Rewise_Chinese_Mandarin: return Langs.zh_cn;
        case CourseIds.TN_Rewise_Icelandic: return Langs.is_is;
        case CourseIds.TN_Rewise_Igbo: return Langs.ig_ng;
        case CourseIds.TN_Rewise_Indonesian: return Langs.id_id;
        case CourseIds.TN_Rewise_Irish: return Langs.ga_ie;
        case CourseIds.TN_Rewise_Italian: return Langs.it_it;
        case CourseIds.TN_Rewise_Japanese: return Langs.ja_jp;
        case CourseIds.TN_Rewise_Khmer: return Langs.km_kh;
        case CourseIds.TN_Rewise_Kirghiz: return Langs.ky_kg;
        case CourseIds.TN_Rewise_Korean: return Langs.ko_kr;
        case CourseIds.TN_Rewise_Spanish_Latin_American: return Langs.sp_sp;
        case CourseIds.TN_Rewise_Latvian: return Langs.lv_lv;
        case CourseIds.TN_Rewise_Lithuanian: return Langs.lt_lt;
        case CourseIds.TN_Rewise_Macedonian: return Langs.mk_mk;
        case CourseIds.TN_Rewise_Malay: return Langs.ms_my;
        case CourseIds.TN_Rewise_Malayalam: return Langs.ml_in;
        case CourseIds.TN_Rewise_Maltese: return Langs.mt_mt;
        case CourseIds.TN_Rewise_Maori: return Langs.mi_nz;
        case CourseIds.TN_Rewise_Mongolian: return Langs.mn_mn;
        case CourseIds.TN_Rewise_Occitan: return Langs.oc_fr;
        case CourseIds.TN_Rewise_Norwegian: return Langs.nb_no;
        case CourseIds.TN_Rewise_Pashto: return Langs.ps_af;
        case CourseIds.TN_Rewise_Persian: return Langs.fa_ir;
        case CourseIds.TN_Rewise_Polish: return Langs.pl_pl;
        case CourseIds.TN_Rewise_Portuguese: return Langs.pt_pt;
        case CourseIds.TN_Rewise_Quechua: return Langs.quz_pe;
        case CourseIds.TN_Rewise_Romanian: return Langs.ro_ro;
        case CourseIds.TN_Rewise_Russian: return Langs.ru_ru;
        case CourseIds.TN_Rewise_Serbian: return Langs.sr_latn_cs;
        case CourseIds.TN_Rewise_Sesotho: return Langs.nso_za;
        case CourseIds.TN_Rewise_Slovak: return Langs.sk_sk;
        case CourseIds.TN_Rewise_Slovenian: return Langs.sl_si;
        case CourseIds.TN_Rewise_Spanish: return Langs.sp_sp;
        case CourseIds.TN_Rewise_Swahili: return Langs.sw_ke;
        case CourseIds.TN_Rewise_Swedish: return Langs.sv_se;
        case CourseIds.TN_Rewise_Thai: return Langs.th_th;
        case CourseIds.TN_Rewise_Tibetan: return Langs.bo_cn;
        case CourseIds.TN_Rewise_Tswana: return Langs.tn_za;
        case CourseIds.TN_Rewise_Turkish: return Langs.tr_tr;
        case CourseIds.TN_Rewise_Ukrainian: return Langs.uk_ua;
        case CourseIds.TN_Rewise_Urdu: return Langs.ur_pk;
        case CourseIds.TN_Rewise_Uzbek: return Langs.uz_latn_uz;
        case CourseIds.TN_Rewise_Vietnamese: return Langs.vi_vn;
        case CourseIds.TN_Rewise_Xhosa: return Langs.xh_za;
        case CourseIds.TN_Rewise_Yoruba: return Langs.yo_ng;
        case CourseIds.TN_Rewise_Zulu: return Langs.zu_za;
        case CourseIds.StudyDictsq_en:
        case CourseIds.StudyDictsq_fr:
        case CourseIds.StudyDictsq_it:
        case CourseIds.StudyDictsq_de:
        case CourseIds.StudyDictsq_ru:
        case CourseIds.StudyDictsq_sp:
        case CourseIds.StudyDictsq_cs:
        case CourseIds.StudyDictsq_sk:
        case CourseIds.StudyDictsq_bg:
        case CourseIds.StudyDictsq_hr:
        case CourseIds.StudyDictsq_ca:
        case CourseIds.StudyDictsq_hu:
        case CourseIds.StudyDictsq_nl:
        case CourseIds.StudyDictsq_pl:
        case CourseIds.StudyDictsq_pt:
        case CourseIds.StudyDictsq_ro:
        case CourseIds.StudyDictsq_el:
        case CourseIds.StudyDictsq_sl:
        case CourseIds.StudyDictsq_tr:
        case CourseIds.StudyDictsq_uk:
        case CourseIds.StudyDictsq_vi:
        case CourseIds.StudyDictsq_fi:
        case CourseIds.StudyDictsq_sv:
        case CourseIds.StudyDictsq_da:
        case CourseIds.StudyDictsq_nb:
        case CourseIds.StudyDictsq_ja:
        case CourseIds.StudyDictsq_ptbr:
        case CourseIds.StudyDictsq_zh:
        case CourseIds.StudyDictsq_ko:
        case CourseIds.StudyDictsq_ar:
        case CourseIds.StudyDictsq_he:
        case CourseIds.StudyDictsq_th:
        case CourseIds.StudyDictsq_lv:
        case CourseIds.StudyDictsq_lt:
        case CourseIds.StudyDictsq_mk:
        case CourseIds.StudyDictsq_sr:
          return Langs.sq_al;
        case CourseIds.StudyDictar_en:
        case CourseIds.StudyDictar_fr:
        case CourseIds.StudyDictar_it:
        case CourseIds.StudyDictar_de:
        case CourseIds.StudyDictar_ru:
        case CourseIds.StudyDictar_sp:
        case CourseIds.StudyDictar_cs:
        case CourseIds.StudyDictar_sk:
        case CourseIds.StudyDictar_bg:
        case CourseIds.StudyDictar_hr:
        case CourseIds.StudyDictar_ca:
        case CourseIds.StudyDictar_hu:
        case CourseIds.StudyDictar_nl:
        case CourseIds.StudyDictar_pl:
        case CourseIds.StudyDictar_pt:
        case CourseIds.StudyDictar_ro:
        case CourseIds.StudyDictar_el:
        case CourseIds.StudyDictar_sl:
        case CourseIds.StudyDictar_tr:
        case CourseIds.StudyDictar_uk:
        case CourseIds.StudyDictar_vi:
        case CourseIds.StudyDictar_fi:
        case CourseIds.StudyDictar_sv:
        case CourseIds.StudyDictar_da:
        case CourseIds.StudyDictar_nb:
        case CourseIds.StudyDictar_sq:
        case CourseIds.StudyDictar_ja:
        case CourseIds.StudyDictar_ptbr:
        case CourseIds.StudyDictar_zh:
        case CourseIds.StudyDictar_ko:
        case CourseIds.StudyDictar_he:
        case CourseIds.StudyDictar_th:
        case CourseIds.StudyDictar_lv:
        case CourseIds.StudyDictar_lt:
        case CourseIds.StudyDictar_mk:
        case CourseIds.StudyDictar_sr:
          return Langs.ar_sa;
        case CourseIds.StudyDictptbr_en:
        case CourseIds.StudyDictptbr_fr:
        case CourseIds.StudyDictptbr_it:
        case CourseIds.StudyDictptbr_de:
        case CourseIds.StudyDictptbr_ru:
        case CourseIds.StudyDictptbr_sp:
        case CourseIds.StudyDictptbr_cs:
        case CourseIds.StudyDictptbr_sk:
        case CourseIds.StudyDictptbr_bg:
        case CourseIds.StudyDictptbr_hr:
        case CourseIds.StudyDictptbr_ca:
        case CourseIds.StudyDictptbr_hu:
        case CourseIds.StudyDictptbr_nl:
        case CourseIds.StudyDictptbr_pl:
        case CourseIds.StudyDictptbr_pt:
        case CourseIds.StudyDictptbr_ro:
        case CourseIds.StudyDictptbr_el:
        case CourseIds.StudyDictptbr_sl:
        case CourseIds.StudyDictptbr_tr:
        case CourseIds.StudyDictptbr_uk:
        case CourseIds.StudyDictptbr_vi:
        case CourseIds.StudyDictptbr_fi:
        case CourseIds.StudyDictptbr_sv:
        case CourseIds.StudyDictptbr_da:
        case CourseIds.StudyDictptbr_nb:
        case CourseIds.StudyDictptbr_sq:
        case CourseIds.StudyDictptbr_ja:
        case CourseIds.StudyDictptbr_zh:
        case CourseIds.StudyDictptbr_ko:
        case CourseIds.StudyDictptbr_ar:
        case CourseIds.StudyDictptbr_he:
        case CourseIds.StudyDictptbr_th:
        case CourseIds.StudyDictptbr_lv:
        case CourseIds.StudyDictptbr_lt:
        case CourseIds.StudyDictptbr_mk:
        case CourseIds.StudyDictptbr_sr:
          return Langs.pt_br;
        case CourseIds.StudyDictbg_en:
        case CourseIds.StudyDictbg_fr:
        case CourseIds.StudyDictbg_it:
        case CourseIds.StudyDictbg_de:
        case CourseIds.StudyDictbg_ru:
        case CourseIds.StudyDictbg_sp:
        case CourseIds.StudyDictbg_cs:
        case CourseIds.StudyDictbg_sk:
        case CourseIds.StudyDictbg_hr:
        case CourseIds.StudyDictbg_ca:
        case CourseIds.StudyDictbg_hu:
        case CourseIds.StudyDictbg_nl:
        case CourseIds.StudyDictbg_pl:
        case CourseIds.StudyDictbg_pt:
        case CourseIds.StudyDictbg_ro:
        case CourseIds.StudyDictbg_el:
        case CourseIds.StudyDictbg_sl:
        case CourseIds.StudyDictbg_tr:
        case CourseIds.StudyDictbg_uk:
        case CourseIds.StudyDictbg_vi:
        case CourseIds.StudyDictbg_fi:
        case CourseIds.StudyDictbg_sv:
        case CourseIds.StudyDictbg_da:
        case CourseIds.StudyDictbg_nb:
        case CourseIds.StudyDictbg_sq:
        case CourseIds.StudyDictbg_ja:
        case CourseIds.StudyDictbg_ptbr:
        case CourseIds.StudyDictbg_zh:
        case CourseIds.StudyDictbg_ko:
        case CourseIds.StudyDictbg_ar:
        case CourseIds.StudyDictbg_he:
        case CourseIds.StudyDictbg_th:
        case CourseIds.StudyDictbg_lv:
        case CourseIds.StudyDictbg_lt:
        case CourseIds.StudyDictbg_mk:
        case CourseIds.StudyDictbg_sr:
          return Langs.bg_bg;
        case CourseIds.StudyDictca_en:
        case CourseIds.StudyDictca_fr:
        case CourseIds.StudyDictca_it:
        case CourseIds.StudyDictca_de:
        case CourseIds.StudyDictca_ru:
        case CourseIds.StudyDictca_sp:
        case CourseIds.StudyDictca_cs:
        case CourseIds.StudyDictca_sk:
        case CourseIds.StudyDictca_bg:
        case CourseIds.StudyDictca_hr:
        case CourseIds.StudyDictca_hu:
        case CourseIds.StudyDictca_nl:
        case CourseIds.StudyDictca_pl:
        case CourseIds.StudyDictca_pt:
        case CourseIds.StudyDictca_ro:
        case CourseIds.StudyDictca_el:
        case CourseIds.StudyDictca_sl:
        case CourseIds.StudyDictca_tr:
        case CourseIds.StudyDictca_uk:
        case CourseIds.StudyDictca_vi:
        case CourseIds.StudyDictca_fi:
        case CourseIds.StudyDictca_sv:
        case CourseIds.StudyDictca_da:
        case CourseIds.StudyDictca_nb:
        case CourseIds.StudyDictca_sq:
        case CourseIds.StudyDictca_ja:
        case CourseIds.StudyDictca_ptbr:
        case CourseIds.StudyDictca_zh:
        case CourseIds.StudyDictca_ko:
        case CourseIds.StudyDictca_ar:
        case CourseIds.StudyDictca_he:
        case CourseIds.StudyDictca_th:
        case CourseIds.StudyDictca_lv:
        case CourseIds.StudyDictca_lt:
        case CourseIds.StudyDictca_mk:
        case CourseIds.StudyDictca_sr:
          return Langs.ca_es;
        case CourseIds.StudyDicthr_en:
        case CourseIds.StudyDicthr_fr:
        case CourseIds.StudyDicthr_it:
        case CourseIds.StudyDicthr_de:
        case CourseIds.StudyDicthr_ru:
        case CourseIds.StudyDicthr_sp:
        case CourseIds.StudyDicthr_cs:
        case CourseIds.StudyDicthr_sk:
        case CourseIds.StudyDicthr_bg:
        case CourseIds.StudyDicthr_ca:
        case CourseIds.StudyDicthr_hu:
        case CourseIds.StudyDicthr_nl:
        case CourseIds.StudyDicthr_pl:
        case CourseIds.StudyDicthr_pt:
        case CourseIds.StudyDicthr_ro:
        case CourseIds.StudyDicthr_el:
        case CourseIds.StudyDicthr_sl:
        case CourseIds.StudyDicthr_tr:
        case CourseIds.StudyDicthr_uk:
        case CourseIds.StudyDicthr_vi:
        case CourseIds.StudyDicthr_fi:
        case CourseIds.StudyDicthr_sv:
        case CourseIds.StudyDicthr_da:
        case CourseIds.StudyDicthr_nb:
        case CourseIds.StudyDicthr_sq:
        case CourseIds.StudyDicthr_ja:
        case CourseIds.StudyDicthr_ptbr:
        case CourseIds.StudyDicthr_zh:
        case CourseIds.StudyDicthr_ko:
        case CourseIds.StudyDicthr_ar:
        case CourseIds.StudyDicthr_he:
        case CourseIds.StudyDicthr_th:
        case CourseIds.StudyDicthr_lv:
        case CourseIds.StudyDicthr_lt:
        case CourseIds.StudyDicthr_mk:
        case CourseIds.StudyDicthr_sr:
          return Langs.hr_hr;
        case CourseIds.StudyDictcs_en:
        case CourseIds.StudyDictcs_fr:
        case CourseIds.StudyDictcs_it:
        case CourseIds.StudyDictcs_de:
        case CourseIds.StudyDictcs_ru:
        case CourseIds.StudyDictcs_sp:
        case CourseIds.StudyDictcs_sk:
        case CourseIds.StudyDictcs_bg:
        case CourseIds.StudyDictcs_hr:
        case CourseIds.StudyDictcs_ca:
        case CourseIds.StudyDictcs_hu:
        case CourseIds.StudyDictcs_nl:
        case CourseIds.StudyDictcs_pl:
        case CourseIds.StudyDictcs_pt:
        case CourseIds.StudyDictcs_ro:
        case CourseIds.StudyDictcs_el:
        case CourseIds.StudyDictcs_sl:
        case CourseIds.StudyDictcs_tr:
        case CourseIds.StudyDictcs_uk:
        case CourseIds.StudyDictcs_vi:
        case CourseIds.StudyDictcs_fi:
        case CourseIds.StudyDictcs_sv:
        case CourseIds.StudyDictcs_da:
        case CourseIds.StudyDictcs_nb:
        case CourseIds.StudyDictcs_sq:
        case CourseIds.StudyDictcs_ja:
        case CourseIds.StudyDictcs_ptbr:
        case CourseIds.StudyDictcs_zh:
        case CourseIds.StudyDictcs_ko:
        case CourseIds.StudyDictcs_ar:
        case CourseIds.StudyDictcs_he:
        case CourseIds.StudyDictcs_th:
        case CourseIds.StudyDictcs_lv:
        case CourseIds.StudyDictcs_lt:
        case CourseIds.StudyDictcs_mk:
        case CourseIds.StudyDictcs_sr:
          return Langs.cs_cz;
        case CourseIds.StudyDictda_en:
        case CourseIds.StudyDictda_fr:
        case CourseIds.StudyDictda_it:
        case CourseIds.StudyDictda_de:
        case CourseIds.StudyDictda_ru:
        case CourseIds.StudyDictda_sp:
        case CourseIds.StudyDictda_cs:
        case CourseIds.StudyDictda_sk:
        case CourseIds.StudyDictda_bg:
        case CourseIds.StudyDictda_hr:
        case CourseIds.StudyDictda_ca:
        case CourseIds.StudyDictda_hu:
        case CourseIds.StudyDictda_nl:
        case CourseIds.StudyDictda_pl:
        case CourseIds.StudyDictda_pt:
        case CourseIds.StudyDictda_ro:
        case CourseIds.StudyDictda_el:
        case CourseIds.StudyDictda_sl:
        case CourseIds.StudyDictda_tr:
        case CourseIds.StudyDictda_uk:
        case CourseIds.StudyDictda_vi:
        case CourseIds.StudyDictda_fi:
        case CourseIds.StudyDictda_sv:
        case CourseIds.StudyDictda_nb:
        case CourseIds.StudyDictda_sq:
        case CourseIds.StudyDictda_ja:
        case CourseIds.StudyDictda_ptbr:
        case CourseIds.StudyDictda_zh:
        case CourseIds.StudyDictda_ko:
        case CourseIds.StudyDictda_ar:
        case CourseIds.StudyDictda_he:
        case CourseIds.StudyDictda_th:
        case CourseIds.StudyDictda_lv:
        case CourseIds.StudyDictda_lt:
        case CourseIds.StudyDictda_mk:
        case CourseIds.StudyDictda_sr:
          return Langs.da_dk;
        case CourseIds.StudyDictnl_en:
        case CourseIds.StudyDictnl_fr:
        case CourseIds.StudyDictnl_it:
        case CourseIds.StudyDictnl_de:
        case CourseIds.StudyDictnl_ru:
        case CourseIds.StudyDictnl_sp:
        case CourseIds.StudyDictnl_cs:
        case CourseIds.StudyDictnl_sk:
        case CourseIds.StudyDictnl_bg:
        case CourseIds.StudyDictnl_hr:
        case CourseIds.StudyDictnl_ca:
        case CourseIds.StudyDictnl_hu:
        case CourseIds.StudyDictnl_pl:
        case CourseIds.StudyDictnl_pt:
        case CourseIds.StudyDictnl_ro:
        case CourseIds.StudyDictnl_el:
        case CourseIds.StudyDictnl_sl:
        case CourseIds.StudyDictnl_tr:
        case CourseIds.StudyDictnl_uk:
        case CourseIds.StudyDictnl_vi:
        case CourseIds.StudyDictnl_fi:
        case CourseIds.StudyDictnl_sv:
        case CourseIds.StudyDictnl_da:
        case CourseIds.StudyDictnl_nb:
        case CourseIds.StudyDictnl_sq:
        case CourseIds.StudyDictnl_ja:
        case CourseIds.StudyDictnl_ptbr:
        case CourseIds.StudyDictnl_zh:
        case CourseIds.StudyDictnl_ko:
        case CourseIds.StudyDictnl_ar:
        case CourseIds.StudyDictnl_he:
        case CourseIds.StudyDictnl_th:
        case CourseIds.StudyDictnl_lv:
        case CourseIds.StudyDictnl_lt:
        case CourseIds.StudyDictnl_mk:
        case CourseIds.StudyDictnl_sr:
          return Langs.nl_nl;
        case CourseIds.StudyDicten_fr:
        case CourseIds.StudyDicten_it:
        case CourseIds.StudyDicten_de:
        case CourseIds.StudyDicten_ru:
        case CourseIds.StudyDicten_sp:
        case CourseIds.StudyDicten_cs:
        case CourseIds.StudyDicten_sk:
        case CourseIds.StudyDicten_bg:
        case CourseIds.StudyDicten_hr:
        case CourseIds.StudyDicten_ca:
        case CourseIds.StudyDicten_hu:
        case CourseIds.StudyDicten_nl:
        case CourseIds.StudyDicten_pl:
        case CourseIds.StudyDicten_pt:
        case CourseIds.StudyDicten_ro:
        case CourseIds.StudyDicten_el:
        case CourseIds.StudyDicten_sl:
        case CourseIds.StudyDicten_tr:
        case CourseIds.StudyDicten_uk:
        case CourseIds.StudyDicten_vi:
        case CourseIds.StudyDicten_fi:
        case CourseIds.StudyDicten_sv:
        case CourseIds.StudyDicten_da:
        case CourseIds.StudyDicten_nb:
        case CourseIds.StudyDicten_sq:
        case CourseIds.StudyDicten_ja:
        case CourseIds.StudyDicten_ptbr:
        case CourseIds.StudyDicten_zh:
        case CourseIds.StudyDicten_ko:
        case CourseIds.StudyDicten_ar:
        case CourseIds.StudyDicten_he:
        case CourseIds.StudyDicten_th:
        case CourseIds.StudyDicten_lv:
        case CourseIds.StudyDicten_lt:
        case CourseIds.StudyDicten_mk:
        case CourseIds.StudyDicten_sr:
          return Langs.en_gb;
        case CourseIds.StudyDictfi_en:
        case CourseIds.StudyDictfi_fr:
        case CourseIds.StudyDictfi_it:
        case CourseIds.StudyDictfi_de:
        case CourseIds.StudyDictfi_ru:
        case CourseIds.StudyDictfi_sp:
        case CourseIds.StudyDictfi_cs:
        case CourseIds.StudyDictfi_sk:
        case CourseIds.StudyDictfi_bg:
        case CourseIds.StudyDictfi_hr:
        case CourseIds.StudyDictfi_ca:
        case CourseIds.StudyDictfi_hu:
        case CourseIds.StudyDictfi_nl:
        case CourseIds.StudyDictfi_pl:
        case CourseIds.StudyDictfi_pt:
        case CourseIds.StudyDictfi_ro:
        case CourseIds.StudyDictfi_el:
        case CourseIds.StudyDictfi_sl:
        case CourseIds.StudyDictfi_tr:
        case CourseIds.StudyDictfi_uk:
        case CourseIds.StudyDictfi_vi:
        case CourseIds.StudyDictfi_sv:
        case CourseIds.StudyDictfi_da:
        case CourseIds.StudyDictfi_nb:
        case CourseIds.StudyDictfi_sq:
        case CourseIds.StudyDictfi_ja:
        case CourseIds.StudyDictfi_ptbr:
        case CourseIds.StudyDictfi_zh:
        case CourseIds.StudyDictfi_ko:
        case CourseIds.StudyDictfi_ar:
        case CourseIds.StudyDictfi_he:
        case CourseIds.StudyDictfi_th:
        case CourseIds.StudyDictfi_lv:
        case CourseIds.StudyDictfi_lt:
        case CourseIds.StudyDictfi_mk:
        case CourseIds.StudyDictfi_sr:
          return Langs.fi_fi;
        case CourseIds.StudyDictfr_en:
        case CourseIds.StudyDictfr_it:
        case CourseIds.StudyDictfr_de:
        case CourseIds.StudyDictfr_ru:
        case CourseIds.StudyDictfr_sp:
        case CourseIds.StudyDictfr_cs:
        case CourseIds.StudyDictfr_sk:
        case CourseIds.StudyDictfr_bg:
        case CourseIds.StudyDictfr_hr:
        case CourseIds.StudyDictfr_ca:
        case CourseIds.StudyDictfr_hu:
        case CourseIds.StudyDictfr_nl:
        case CourseIds.StudyDictfr_pl:
        case CourseIds.StudyDictfr_pt:
        case CourseIds.StudyDictfr_ro:
        case CourseIds.StudyDictfr_el:
        case CourseIds.StudyDictfr_sl:
        case CourseIds.StudyDictfr_tr:
        case CourseIds.StudyDictfr_uk:
        case CourseIds.StudyDictfr_vi:
        case CourseIds.StudyDictfr_fi:
        case CourseIds.StudyDictfr_sv:
        case CourseIds.StudyDictfr_da:
        case CourseIds.StudyDictfr_nb:
        case CourseIds.StudyDictfr_sq:
        case CourseIds.StudyDictfr_ja:
        case CourseIds.StudyDictfr_ptbr:
        case CourseIds.StudyDictfr_zh:
        case CourseIds.StudyDictfr_ko:
        case CourseIds.StudyDictfr_ar:
        case CourseIds.StudyDictfr_he:
        case CourseIds.StudyDictfr_th:
        case CourseIds.StudyDictfr_lv:
        case CourseIds.StudyDictfr_lt:
        case CourseIds.StudyDictfr_mk:
        case CourseIds.StudyDictfr_sr:
          return Langs.fr_fr;
        case CourseIds.StudyDictde_en:
        case CourseIds.StudyDictde_fr:
        case CourseIds.StudyDictde_it:
        case CourseIds.StudyDictde_ru:
        case CourseIds.StudyDictde_sp:
        case CourseIds.StudyDictde_cs:
        case CourseIds.StudyDictde_sk:
        case CourseIds.StudyDictde_bg:
        case CourseIds.StudyDictde_hr:
        case CourseIds.StudyDictde_ca:
        case CourseIds.StudyDictde_hu:
        case CourseIds.StudyDictde_nl:
        case CourseIds.StudyDictde_pl:
        case CourseIds.StudyDictde_pt:
        case CourseIds.StudyDictde_ro:
        case CourseIds.StudyDictde_el:
        case CourseIds.StudyDictde_sl:
        case CourseIds.StudyDictde_tr:
        case CourseIds.StudyDictde_uk:
        case CourseIds.StudyDictde_vi:
        case CourseIds.StudyDictde_fi:
        case CourseIds.StudyDictde_sv:
        case CourseIds.StudyDictde_da:
        case CourseIds.StudyDictde_nb:
        case CourseIds.StudyDictde_sq:
        case CourseIds.StudyDictde_ja:
        case CourseIds.StudyDictde_ptbr:
        case CourseIds.StudyDictde_zh:
        case CourseIds.StudyDictde_ko:
        case CourseIds.StudyDictde_ar:
        case CourseIds.StudyDictde_he:
        case CourseIds.StudyDictde_th:
        case CourseIds.StudyDictde_lv:
        case CourseIds.StudyDictde_lt:
        case CourseIds.StudyDictde_mk:
        case CourseIds.StudyDictde_sr:
          return Langs.de_de;
        case CourseIds.StudyDictel_en:
        case CourseIds.StudyDictel_fr:
        case CourseIds.StudyDictel_it:
        case CourseIds.StudyDictel_de:
        case CourseIds.StudyDictel_ru:
        case CourseIds.StudyDictel_sp:
        case CourseIds.StudyDictel_cs:
        case CourseIds.StudyDictel_sk:
        case CourseIds.StudyDictel_bg:
        case CourseIds.StudyDictel_hr:
        case CourseIds.StudyDictel_ca:
        case CourseIds.StudyDictel_hu:
        case CourseIds.StudyDictel_nl:
        case CourseIds.StudyDictel_pl:
        case CourseIds.StudyDictel_pt:
        case CourseIds.StudyDictel_ro:
        case CourseIds.StudyDictel_sl:
        case CourseIds.StudyDictel_tr:
        case CourseIds.StudyDictel_uk:
        case CourseIds.StudyDictel_vi:
        case CourseIds.StudyDictel_fi:
        case CourseIds.StudyDictel_sv:
        case CourseIds.StudyDictel_da:
        case CourseIds.StudyDictel_nb:
        case CourseIds.StudyDictel_sq:
        case CourseIds.StudyDictel_ja:
        case CourseIds.StudyDictel_ptbr:
        case CourseIds.StudyDictel_zh:
        case CourseIds.StudyDictel_ko:
        case CourseIds.StudyDictel_ar:
        case CourseIds.StudyDictel_he:
        case CourseIds.StudyDictel_th:
        case CourseIds.StudyDictel_lv:
        case CourseIds.StudyDictel_lt:
        case CourseIds.StudyDictel_mk:
        case CourseIds.StudyDictel_sr:
          return Langs.el_gr;
        case CourseIds.StudyDicthe_en:
        case CourseIds.StudyDicthe_fr:
        case CourseIds.StudyDicthe_it:
        case CourseIds.StudyDicthe_de:
        case CourseIds.StudyDicthe_ru:
        case CourseIds.StudyDicthe_sp:
        case CourseIds.StudyDicthe_cs:
        case CourseIds.StudyDicthe_sk:
        case CourseIds.StudyDicthe_bg:
        case CourseIds.StudyDicthe_hr:
        case CourseIds.StudyDicthe_ca:
        case CourseIds.StudyDicthe_hu:
        case CourseIds.StudyDicthe_nl:
        case CourseIds.StudyDicthe_pl:
        case CourseIds.StudyDicthe_pt:
        case CourseIds.StudyDicthe_ro:
        case CourseIds.StudyDicthe_el:
        case CourseIds.StudyDicthe_sl:
        case CourseIds.StudyDicthe_tr:
        case CourseIds.StudyDicthe_uk:
        case CourseIds.StudyDicthe_vi:
        case CourseIds.StudyDicthe_fi:
        case CourseIds.StudyDicthe_sv:
        case CourseIds.StudyDicthe_da:
        case CourseIds.StudyDicthe_nb:
        case CourseIds.StudyDicthe_sq:
        case CourseIds.StudyDicthe_ja:
        case CourseIds.StudyDicthe_ptbr:
        case CourseIds.StudyDicthe_zh:
        case CourseIds.StudyDicthe_ko:
        case CourseIds.StudyDicthe_ar:
        case CourseIds.StudyDicthe_th:
        case CourseIds.StudyDicthe_lv:
        case CourseIds.StudyDicthe_lt:
        case CourseIds.StudyDicthe_mk:
        case CourseIds.StudyDicthe_sr:
          return Langs.he_il;
        case CourseIds.StudyDicthu_en:
        case CourseIds.StudyDicthu_fr:
        case CourseIds.StudyDicthu_it:
        case CourseIds.StudyDicthu_de:
        case CourseIds.StudyDicthu_ru:
        case CourseIds.StudyDicthu_sp:
        case CourseIds.StudyDicthu_cs:
        case CourseIds.StudyDicthu_sk:
        case CourseIds.StudyDicthu_bg:
        case CourseIds.StudyDicthu_hr:
        case CourseIds.StudyDicthu_ca:
        case CourseIds.StudyDicthu_nl:
        case CourseIds.StudyDicthu_pl:
        case CourseIds.StudyDicthu_pt:
        case CourseIds.StudyDicthu_ro:
        case CourseIds.StudyDicthu_el:
        case CourseIds.StudyDicthu_sl:
        case CourseIds.StudyDicthu_tr:
        case CourseIds.StudyDicthu_uk:
        case CourseIds.StudyDicthu_vi:
        case CourseIds.StudyDicthu_fi:
        case CourseIds.StudyDicthu_sv:
        case CourseIds.StudyDicthu_da:
        case CourseIds.StudyDicthu_nb:
        case CourseIds.StudyDicthu_sq:
        case CourseIds.StudyDicthu_ja:
        case CourseIds.StudyDicthu_ptbr:
        case CourseIds.StudyDicthu_zh:
        case CourseIds.StudyDicthu_ko:
        case CourseIds.StudyDicthu_ar:
        case CourseIds.StudyDicthu_he:
        case CourseIds.StudyDicthu_th:
        case CourseIds.StudyDicthu_lv:
        case CourseIds.StudyDicthu_lt:
        case CourseIds.StudyDicthu_mk:
        case CourseIds.StudyDicthu_sr:
          return Langs.hu_hu;
        case CourseIds.StudyDictzh_en:
        case CourseIds.StudyDictzh_fr:
        case CourseIds.StudyDictzh_it:
        case CourseIds.StudyDictzh_de:
        case CourseIds.StudyDictzh_ru:
        case CourseIds.StudyDictzh_sp:
        case CourseIds.StudyDictzh_cs:
        case CourseIds.StudyDictzh_sk:
        case CourseIds.StudyDictzh_bg:
        case CourseIds.StudyDictzh_hr:
        case CourseIds.StudyDictzh_ca:
        case CourseIds.StudyDictzh_hu:
        case CourseIds.StudyDictzh_nl:
        case CourseIds.StudyDictzh_pl:
        case CourseIds.StudyDictzh_pt:
        case CourseIds.StudyDictzh_ro:
        case CourseIds.StudyDictzh_el:
        case CourseIds.StudyDictzh_sl:
        case CourseIds.StudyDictzh_tr:
        case CourseIds.StudyDictzh_uk:
        case CourseIds.StudyDictzh_vi:
        case CourseIds.StudyDictzh_fi:
        case CourseIds.StudyDictzh_sv:
        case CourseIds.StudyDictzh_da:
        case CourseIds.StudyDictzh_nb:
        case CourseIds.StudyDictzh_sq:
        case CourseIds.StudyDictzh_ja:
        case CourseIds.StudyDictzh_ptbr:
        case CourseIds.StudyDictzh_ko:
        case CourseIds.StudyDictzh_ar:
        case CourseIds.StudyDictzh_he:
        case CourseIds.StudyDictzh_th:
        case CourseIds.StudyDictzh_lv:
        case CourseIds.StudyDictzh_lt:
        case CourseIds.StudyDictzh_mk:
        case CourseIds.StudyDictzh_sr:
          return Langs.zh_cn;
        case CourseIds.StudyDictit_en:
        case CourseIds.StudyDictit_fr:
        case CourseIds.StudyDictit_de:
        case CourseIds.StudyDictit_ru:
        case CourseIds.StudyDictit_sp:
        case CourseIds.StudyDictit_cs:
        case CourseIds.StudyDictit_sk:
        case CourseIds.StudyDictit_bg:
        case CourseIds.StudyDictit_hr:
        case CourseIds.StudyDictit_ca:
        case CourseIds.StudyDictit_hu:
        case CourseIds.StudyDictit_nl:
        case CourseIds.StudyDictit_pl:
        case CourseIds.StudyDictit_pt:
        case CourseIds.StudyDictit_ro:
        case CourseIds.StudyDictit_el:
        case CourseIds.StudyDictit_sl:
        case CourseIds.StudyDictit_tr:
        case CourseIds.StudyDictit_uk:
        case CourseIds.StudyDictit_vi:
        case CourseIds.StudyDictit_fi:
        case CourseIds.StudyDictit_sv:
        case CourseIds.StudyDictit_da:
        case CourseIds.StudyDictit_nb:
        case CourseIds.StudyDictit_sq:
        case CourseIds.StudyDictit_ja:
        case CourseIds.StudyDictit_ptbr:
        case CourseIds.StudyDictit_zh:
        case CourseIds.StudyDictit_ko:
        case CourseIds.StudyDictit_ar:
        case CourseIds.StudyDictit_he:
        case CourseIds.StudyDictit_th:
        case CourseIds.StudyDictit_lv:
        case CourseIds.StudyDictit_lt:
        case CourseIds.StudyDictit_mk:
        case CourseIds.StudyDictit_sr:
          return Langs.it_it;
        case CourseIds.StudyDictja_en:
        case CourseIds.StudyDictja_fr:
        case CourseIds.StudyDictja_it:
        case CourseIds.StudyDictja_de:
        case CourseIds.StudyDictja_ru:
        case CourseIds.StudyDictja_sp:
        case CourseIds.StudyDictja_cs:
        case CourseIds.StudyDictja_sk:
        case CourseIds.StudyDictja_bg:
        case CourseIds.StudyDictja_hr:
        case CourseIds.StudyDictja_ca:
        case CourseIds.StudyDictja_hu:
        case CourseIds.StudyDictja_nl:
        case CourseIds.StudyDictja_pl:
        case CourseIds.StudyDictja_pt:
        case CourseIds.StudyDictja_ro:
        case CourseIds.StudyDictja_el:
        case CourseIds.StudyDictja_sl:
        case CourseIds.StudyDictja_tr:
        case CourseIds.StudyDictja_uk:
        case CourseIds.StudyDictja_vi:
        case CourseIds.StudyDictja_fi:
        case CourseIds.StudyDictja_sv:
        case CourseIds.StudyDictja_da:
        case CourseIds.StudyDictja_nb:
        case CourseIds.StudyDictja_sq:
        case CourseIds.StudyDictja_ptbr:
        case CourseIds.StudyDictja_zh:
        case CourseIds.StudyDictja_ko:
        case CourseIds.StudyDictja_ar:
        case CourseIds.StudyDictja_he:
        case CourseIds.StudyDictja_th:
        case CourseIds.StudyDictja_lv:
        case CourseIds.StudyDictja_lt:
        case CourseIds.StudyDictja_mk:
        case CourseIds.StudyDictja_sr:
          return Langs.ja_jp;
        case CourseIds.StudyDictko_en:
        case CourseIds.StudyDictko_fr:
        case CourseIds.StudyDictko_it:
        case CourseIds.StudyDictko_de:
        case CourseIds.StudyDictko_ru:
        case CourseIds.StudyDictko_sp:
        case CourseIds.StudyDictko_cs:
        case CourseIds.StudyDictko_sk:
        case CourseIds.StudyDictko_bg:
        case CourseIds.StudyDictko_hr:
        case CourseIds.StudyDictko_ca:
        case CourseIds.StudyDictko_hu:
        case CourseIds.StudyDictko_nl:
        case CourseIds.StudyDictko_pl:
        case CourseIds.StudyDictko_pt:
        case CourseIds.StudyDictko_ro:
        case CourseIds.StudyDictko_el:
        case CourseIds.StudyDictko_sl:
        case CourseIds.StudyDictko_tr:
        case CourseIds.StudyDictko_uk:
        case CourseIds.StudyDictko_vi:
        case CourseIds.StudyDictko_fi:
        case CourseIds.StudyDictko_sv:
        case CourseIds.StudyDictko_da:
        case CourseIds.StudyDictko_nb:
        case CourseIds.StudyDictko_sq:
        case CourseIds.StudyDictko_ja:
        case CourseIds.StudyDictko_ptbr:
        case CourseIds.StudyDictko_zh:
        case CourseIds.StudyDictko_ar:
        case CourseIds.StudyDictko_he:
        case CourseIds.StudyDictko_th:
        case CourseIds.StudyDictko_lv:
        case CourseIds.StudyDictko_lt:
        case CourseIds.StudyDictko_mk:
        case CourseIds.StudyDictko_sr:
          return Langs.ko_kr;
        case CourseIds.StudyDictlv_en:
        case CourseIds.StudyDictlv_fr:
        case CourseIds.StudyDictlv_it:
        case CourseIds.StudyDictlv_de:
        case CourseIds.StudyDictlv_ru:
        case CourseIds.StudyDictlv_sp:
        case CourseIds.StudyDictlv_cs:
        case CourseIds.StudyDictlv_sk:
        case CourseIds.StudyDictlv_bg:
        case CourseIds.StudyDictlv_hr:
        case CourseIds.StudyDictlv_ca:
        case CourseIds.StudyDictlv_hu:
        case CourseIds.StudyDictlv_nl:
        case CourseIds.StudyDictlv_pl:
        case CourseIds.StudyDictlv_pt:
        case CourseIds.StudyDictlv_ro:
        case CourseIds.StudyDictlv_el:
        case CourseIds.StudyDictlv_sl:
        case CourseIds.StudyDictlv_tr:
        case CourseIds.StudyDictlv_uk:
        case CourseIds.StudyDictlv_vi:
        case CourseIds.StudyDictlv_fi:
        case CourseIds.StudyDictlv_sv:
        case CourseIds.StudyDictlv_da:
        case CourseIds.StudyDictlv_nb:
        case CourseIds.StudyDictlv_sq:
        case CourseIds.StudyDictlv_ja:
        case CourseIds.StudyDictlv_ptbr:
        case CourseIds.StudyDictlv_zh:
        case CourseIds.StudyDictlv_ko:
        case CourseIds.StudyDictlv_ar:
        case CourseIds.StudyDictlv_he:
        case CourseIds.StudyDictlv_th:
        case CourseIds.StudyDictlv_lt:
        case CourseIds.StudyDictlv_mk:
        case CourseIds.StudyDictlv_sr:
          return Langs.lv_lv;
        case CourseIds.StudyDictlt_en:
        case CourseIds.StudyDictlt_fr:
        case CourseIds.StudyDictlt_it:
        case CourseIds.StudyDictlt_de:
        case CourseIds.StudyDictlt_ru:
        case CourseIds.StudyDictlt_sp:
        case CourseIds.StudyDictlt_cs:
        case CourseIds.StudyDictlt_sk:
        case CourseIds.StudyDictlt_bg:
        case CourseIds.StudyDictlt_hr:
        case CourseIds.StudyDictlt_ca:
        case CourseIds.StudyDictlt_hu:
        case CourseIds.StudyDictlt_nl:
        case CourseIds.StudyDictlt_pl:
        case CourseIds.StudyDictlt_pt:
        case CourseIds.StudyDictlt_ro:
        case CourseIds.StudyDictlt_el:
        case CourseIds.StudyDictlt_sl:
        case CourseIds.StudyDictlt_tr:
        case CourseIds.StudyDictlt_uk:
        case CourseIds.StudyDictlt_vi:
        case CourseIds.StudyDictlt_fi:
        case CourseIds.StudyDictlt_sv:
        case CourseIds.StudyDictlt_da:
        case CourseIds.StudyDictlt_nb:
        case CourseIds.StudyDictlt_sq:
        case CourseIds.StudyDictlt_ja:
        case CourseIds.StudyDictlt_ptbr:
        case CourseIds.StudyDictlt_zh:
        case CourseIds.StudyDictlt_ko:
        case CourseIds.StudyDictlt_ar:
        case CourseIds.StudyDictlt_he:
        case CourseIds.StudyDictlt_th:
        case CourseIds.StudyDictlt_lv:
        case CourseIds.StudyDictlt_mk:
        case CourseIds.StudyDictlt_sr:
          return Langs.lt_lt;
        case CourseIds.StudyDictmk_en:
        case CourseIds.StudyDictmk_fr:
        case CourseIds.StudyDictmk_it:
        case CourseIds.StudyDictmk_de:
        case CourseIds.StudyDictmk_ru:
        case CourseIds.StudyDictmk_sp:
        case CourseIds.StudyDictmk_cs:
        case CourseIds.StudyDictmk_sk:
        case CourseIds.StudyDictmk_bg:
        case CourseIds.StudyDictmk_hr:
        case CourseIds.StudyDictmk_ca:
        case CourseIds.StudyDictmk_hu:
        case CourseIds.StudyDictmk_nl:
        case CourseIds.StudyDictmk_pl:
        case CourseIds.StudyDictmk_pt:
        case CourseIds.StudyDictmk_ro:
        case CourseIds.StudyDictmk_el:
        case CourseIds.StudyDictmk_sl:
        case CourseIds.StudyDictmk_tr:
        case CourseIds.StudyDictmk_uk:
        case CourseIds.StudyDictmk_vi:
        case CourseIds.StudyDictmk_fi:
        case CourseIds.StudyDictmk_sv:
        case CourseIds.StudyDictmk_da:
        case CourseIds.StudyDictmk_nb:
        case CourseIds.StudyDictmk_sq:
        case CourseIds.StudyDictmk_ja:
        case CourseIds.StudyDictmk_ptbr:
        case CourseIds.StudyDictmk_zh:
        case CourseIds.StudyDictmk_ko:
        case CourseIds.StudyDictmk_ar:
        case CourseIds.StudyDictmk_he:
        case CourseIds.StudyDictmk_th:
        case CourseIds.StudyDictmk_lv:
        case CourseIds.StudyDictmk_lt:
        case CourseIds.StudyDictmk_sr:
          return Langs.mk_mk;
        case CourseIds.StudyDictnb_en:
        case CourseIds.StudyDictnb_fr:
        case CourseIds.StudyDictnb_it:
        case CourseIds.StudyDictnb_de:
        case CourseIds.StudyDictnb_ru:
        case CourseIds.StudyDictnb_sp:
        case CourseIds.StudyDictnb_cs:
        case CourseIds.StudyDictnb_sk:
        case CourseIds.StudyDictnb_bg:
        case CourseIds.StudyDictnb_hr:
        case CourseIds.StudyDictnb_ca:
        case CourseIds.StudyDictnb_hu:
        case CourseIds.StudyDictnb_nl:
        case CourseIds.StudyDictnb_pl:
        case CourseIds.StudyDictnb_pt:
        case CourseIds.StudyDictnb_ro:
        case CourseIds.StudyDictnb_el:
        case CourseIds.StudyDictnb_sl:
        case CourseIds.StudyDictnb_tr:
        case CourseIds.StudyDictnb_uk:
        case CourseIds.StudyDictnb_vi:
        case CourseIds.StudyDictnb_fi:
        case CourseIds.StudyDictnb_sv:
        case CourseIds.StudyDictnb_da:
        case CourseIds.StudyDictnb_sq:
        case CourseIds.StudyDictnb_ja:
        case CourseIds.StudyDictnb_ptbr:
        case CourseIds.StudyDictnb_zh:
        case CourseIds.StudyDictnb_ko:
        case CourseIds.StudyDictnb_ar:
        case CourseIds.StudyDictnb_he:
        case CourseIds.StudyDictnb_th:
        case CourseIds.StudyDictnb_lv:
        case CourseIds.StudyDictnb_lt:
        case CourseIds.StudyDictnb_mk:
        case CourseIds.StudyDictnb_sr:
          return Langs.nb_no;
        case CourseIds.StudyDictpl_en:
        case CourseIds.StudyDictpl_fr:
        case CourseIds.StudyDictpl_it:
        case CourseIds.StudyDictpl_de:
        case CourseIds.StudyDictpl_ru:
        case CourseIds.StudyDictpl_sp:
        case CourseIds.StudyDictpl_cs:
        case CourseIds.StudyDictpl_sk:
        case CourseIds.StudyDictpl_bg:
        case CourseIds.StudyDictpl_hr:
        case CourseIds.StudyDictpl_ca:
        case CourseIds.StudyDictpl_hu:
        case CourseIds.StudyDictpl_nl:
        case CourseIds.StudyDictpl_pt:
        case CourseIds.StudyDictpl_ro:
        case CourseIds.StudyDictpl_el:
        case CourseIds.StudyDictpl_sl:
        case CourseIds.StudyDictpl_tr:
        case CourseIds.StudyDictpl_uk:
        case CourseIds.StudyDictpl_vi:
        case CourseIds.StudyDictpl_fi:
        case CourseIds.StudyDictpl_sv:
        case CourseIds.StudyDictpl_da:
        case CourseIds.StudyDictpl_nb:
        case CourseIds.StudyDictpl_sq:
        case CourseIds.StudyDictpl_ja:
        case CourseIds.StudyDictpl_ptbr:
        case CourseIds.StudyDictpl_zh:
        case CourseIds.StudyDictpl_ko:
        case CourseIds.StudyDictpl_ar:
        case CourseIds.StudyDictpl_he:
        case CourseIds.StudyDictpl_th:
        case CourseIds.StudyDictpl_lv:
        case CourseIds.StudyDictpl_lt:
        case CourseIds.StudyDictpl_mk:
        case CourseIds.StudyDictpl_sr:
          return Langs.pl_pl;
        case CourseIds.StudyDictpt_en:
        case CourseIds.StudyDictpt_fr:
        case CourseIds.StudyDictpt_it:
        case CourseIds.StudyDictpt_de:
        case CourseIds.StudyDictpt_ru:
        case CourseIds.StudyDictpt_sp:
        case CourseIds.StudyDictpt_cs:
        case CourseIds.StudyDictpt_sk:
        case CourseIds.StudyDictpt_bg:
        case CourseIds.StudyDictpt_hr:
        case CourseIds.StudyDictpt_ca:
        case CourseIds.StudyDictpt_hu:
        case CourseIds.StudyDictpt_nl:
        case CourseIds.StudyDictpt_pl:
        case CourseIds.StudyDictpt_ro:
        case CourseIds.StudyDictpt_el:
        case CourseIds.StudyDictpt_sl:
        case CourseIds.StudyDictpt_tr:
        case CourseIds.StudyDictpt_uk:
        case CourseIds.StudyDictpt_vi:
        case CourseIds.StudyDictpt_fi:
        case CourseIds.StudyDictpt_sv:
        case CourseIds.StudyDictpt_da:
        case CourseIds.StudyDictpt_nb:
        case CourseIds.StudyDictpt_sq:
        case CourseIds.StudyDictpt_ja:
        case CourseIds.StudyDictpt_ptbr:
        case CourseIds.StudyDictpt_zh:
        case CourseIds.StudyDictpt_ko:
        case CourseIds.StudyDictpt_ar:
        case CourseIds.StudyDictpt_he:
        case CourseIds.StudyDictpt_th:
        case CourseIds.StudyDictpt_lv:
        case CourseIds.StudyDictpt_lt:
        case CourseIds.StudyDictpt_mk:
        case CourseIds.StudyDictpt_sr:
          return Langs.pt_pt;
        case CourseIds.StudyDictro_en:
        case CourseIds.StudyDictro_fr:
        case CourseIds.StudyDictro_it:
        case CourseIds.StudyDictro_de:
        case CourseIds.StudyDictro_ru:
        case CourseIds.StudyDictro_sp:
        case CourseIds.StudyDictro_cs:
        case CourseIds.StudyDictro_sk:
        case CourseIds.StudyDictro_bg:
        case CourseIds.StudyDictro_hr:
        case CourseIds.StudyDictro_ca:
        case CourseIds.StudyDictro_hu:
        case CourseIds.StudyDictro_nl:
        case CourseIds.StudyDictro_pl:
        case CourseIds.StudyDictro_pt:
        case CourseIds.StudyDictro_el:
        case CourseIds.StudyDictro_sl:
        case CourseIds.StudyDictro_tr:
        case CourseIds.StudyDictro_uk:
        case CourseIds.StudyDictro_vi:
        case CourseIds.StudyDictro_fi:
        case CourseIds.StudyDictro_sv:
        case CourseIds.StudyDictro_da:
        case CourseIds.StudyDictro_nb:
        case CourseIds.StudyDictro_sq:
        case CourseIds.StudyDictro_ja:
        case CourseIds.StudyDictro_ptbr:
        case CourseIds.StudyDictro_zh:
        case CourseIds.StudyDictro_ko:
        case CourseIds.StudyDictro_ar:
        case CourseIds.StudyDictro_he:
        case CourseIds.StudyDictro_th:
        case CourseIds.StudyDictro_lv:
        case CourseIds.StudyDictro_lt:
        case CourseIds.StudyDictro_mk:
        case CourseIds.StudyDictro_sr:
          return Langs.ro_ro;
        case CourseIds.StudyDictru_en:
        case CourseIds.StudyDictru_fr:
        case CourseIds.StudyDictru_it:
        case CourseIds.StudyDictru_de:
        case CourseIds.StudyDictru_sp:
        case CourseIds.StudyDictru_cs:
        case CourseIds.StudyDictru_sk:
        case CourseIds.StudyDictru_bg:
        case CourseIds.StudyDictru_hr:
        case CourseIds.StudyDictru_ca:
        case CourseIds.StudyDictru_hu:
        case CourseIds.StudyDictru_nl:
        case CourseIds.StudyDictru_pl:
        case CourseIds.StudyDictru_pt:
        case CourseIds.StudyDictru_ro:
        case CourseIds.StudyDictru_el:
        case CourseIds.StudyDictru_sl:
        case CourseIds.StudyDictru_tr:
        case CourseIds.StudyDictru_uk:
        case CourseIds.StudyDictru_vi:
        case CourseIds.StudyDictru_fi:
        case CourseIds.StudyDictru_sv:
        case CourseIds.StudyDictru_da:
        case CourseIds.StudyDictru_nb:
        case CourseIds.StudyDictru_sq:
        case CourseIds.StudyDictru_ja:
        case CourseIds.StudyDictru_ptbr:
        case CourseIds.StudyDictru_zh:
        case CourseIds.StudyDictru_ko:
        case CourseIds.StudyDictru_ar:
        case CourseIds.StudyDictru_he:
        case CourseIds.StudyDictru_th:
        case CourseIds.StudyDictru_lv:
        case CourseIds.StudyDictru_lt:
        case CourseIds.StudyDictru_mk:
        case CourseIds.StudyDictru_sr:
          return Langs.ru_ru;
        case CourseIds.StudyDictsr_en:
        case CourseIds.StudyDictsr_fr:
        case CourseIds.StudyDictsr_it:
        case CourseIds.StudyDictsr_de:
        case CourseIds.StudyDictsr_ru:
        case CourseIds.StudyDictsr_sp:
        case CourseIds.StudyDictsr_cs:
        case CourseIds.StudyDictsr_sk:
        case CourseIds.StudyDictsr_bg:
        case CourseIds.StudyDictsr_hr:
        case CourseIds.StudyDictsr_ca:
        case CourseIds.StudyDictsr_hu:
        case CourseIds.StudyDictsr_nl:
        case CourseIds.StudyDictsr_pl:
        case CourseIds.StudyDictsr_pt:
        case CourseIds.StudyDictsr_ro:
        case CourseIds.StudyDictsr_el:
        case CourseIds.StudyDictsr_sl:
        case CourseIds.StudyDictsr_tr:
        case CourseIds.StudyDictsr_uk:
        case CourseIds.StudyDictsr_vi:
        case CourseIds.StudyDictsr_fi:
        case CourseIds.StudyDictsr_sv:
        case CourseIds.StudyDictsr_da:
        case CourseIds.StudyDictsr_nb:
        case CourseIds.StudyDictsr_sq:
        case CourseIds.StudyDictsr_ja:
        case CourseIds.StudyDictsr_ptbr:
        case CourseIds.StudyDictsr_zh:
        case CourseIds.StudyDictsr_ko:
        case CourseIds.StudyDictsr_ar:
        case CourseIds.StudyDictsr_he:
        case CourseIds.StudyDictsr_th:
        case CourseIds.StudyDictsr_lv:
        case CourseIds.StudyDictsr_lt:
        case CourseIds.StudyDictsr_mk:
          return Langs.sr_latn_cs;
        case CourseIds.StudyDictsk_en:
        case CourseIds.StudyDictsk_fr:
        case CourseIds.StudyDictsk_it:
        case CourseIds.StudyDictsk_de:
        case CourseIds.StudyDictsk_ru:
        case CourseIds.StudyDictsk_sp:
        case CourseIds.StudyDictsk_cs:
        case CourseIds.StudyDictsk_bg:
        case CourseIds.StudyDictsk_hr:
        case CourseIds.StudyDictsk_ca:
        case CourseIds.StudyDictsk_hu:
        case CourseIds.StudyDictsk_nl:
        case CourseIds.StudyDictsk_pl:
        case CourseIds.StudyDictsk_pt:
        case CourseIds.StudyDictsk_ro:
        case CourseIds.StudyDictsk_el:
        case CourseIds.StudyDictsk_sl:
        case CourseIds.StudyDictsk_tr:
        case CourseIds.StudyDictsk_uk:
        case CourseIds.StudyDictsk_vi:
        case CourseIds.StudyDictsk_fi:
        case CourseIds.StudyDictsk_sv:
        case CourseIds.StudyDictsk_da:
        case CourseIds.StudyDictsk_nb:
        case CourseIds.StudyDictsk_sq:
        case CourseIds.StudyDictsk_ja:
        case CourseIds.StudyDictsk_ptbr:
        case CourseIds.StudyDictsk_zh:
        case CourseIds.StudyDictsk_ko:
        case CourseIds.StudyDictsk_ar:
        case CourseIds.StudyDictsk_he:
        case CourseIds.StudyDictsk_th:
        case CourseIds.StudyDictsk_lv:
        case CourseIds.StudyDictsk_lt:
        case CourseIds.StudyDictsk_mk:
        case CourseIds.StudyDictsk_sr:
          return Langs.sk_sk;
        case CourseIds.StudyDictsl_en:
        case CourseIds.StudyDictsl_fr:
        case CourseIds.StudyDictsl_it:
        case CourseIds.StudyDictsl_de:
        case CourseIds.StudyDictsl_ru:
        case CourseIds.StudyDictsl_sp:
        case CourseIds.StudyDictsl_cs:
        case CourseIds.StudyDictsl_sk:
        case CourseIds.StudyDictsl_bg:
        case CourseIds.StudyDictsl_hr:
        case CourseIds.StudyDictsl_ca:
        case CourseIds.StudyDictsl_hu:
        case CourseIds.StudyDictsl_nl:
        case CourseIds.StudyDictsl_pl:
        case CourseIds.StudyDictsl_pt:
        case CourseIds.StudyDictsl_ro:
        case CourseIds.StudyDictsl_el:
        case CourseIds.StudyDictsl_tr:
        case CourseIds.StudyDictsl_uk:
        case CourseIds.StudyDictsl_vi:
        case CourseIds.StudyDictsl_fi:
        case CourseIds.StudyDictsl_sv:
        case CourseIds.StudyDictsl_da:
        case CourseIds.StudyDictsl_nb:
        case CourseIds.StudyDictsl_sq:
        case CourseIds.StudyDictsl_ja:
        case CourseIds.StudyDictsl_ptbr:
        case CourseIds.StudyDictsl_zh:
        case CourseIds.StudyDictsl_ko:
        case CourseIds.StudyDictsl_ar:
        case CourseIds.StudyDictsl_he:
        case CourseIds.StudyDictsl_th:
        case CourseIds.StudyDictsl_lv:
        case CourseIds.StudyDictsl_lt:
        case CourseIds.StudyDictsl_mk:
        case CourseIds.StudyDictsl_sr:
          return Langs.sl_si;
        case CourseIds.StudyDictsp_en:
        case CourseIds.StudyDictsp_fr:
        case CourseIds.StudyDictsp_it:
        case CourseIds.StudyDictsp_de:
        case CourseIds.StudyDictsp_ru:
        case CourseIds.StudyDictsp_cs:
        case CourseIds.StudyDictsp_sk:
        case CourseIds.StudyDictsp_bg:
        case CourseIds.StudyDictsp_hr:
        case CourseIds.StudyDictsp_ca:
        case CourseIds.StudyDictsp_hu:
        case CourseIds.StudyDictsp_nl:
        case CourseIds.StudyDictsp_pl:
        case CourseIds.StudyDictsp_pt:
        case CourseIds.StudyDictsp_ro:
        case CourseIds.StudyDictsp_el:
        case CourseIds.StudyDictsp_sl:
        case CourseIds.StudyDictsp_tr:
        case CourseIds.StudyDictsp_uk:
        case CourseIds.StudyDictsp_vi:
        case CourseIds.StudyDictsp_fi:
        case CourseIds.StudyDictsp_sv:
        case CourseIds.StudyDictsp_da:
        case CourseIds.StudyDictsp_nb:
        case CourseIds.StudyDictsp_sq:
        case CourseIds.StudyDictsp_ja:
        case CourseIds.StudyDictsp_ptbr:
        case CourseIds.StudyDictsp_zh:
        case CourseIds.StudyDictsp_ko:
        case CourseIds.StudyDictsp_ar:
        case CourseIds.StudyDictsp_he:
        case CourseIds.StudyDictsp_th:
        case CourseIds.StudyDictsp_lv:
        case CourseIds.StudyDictsp_lt:
        case CourseIds.StudyDictsp_mk:
        case CourseIds.StudyDictsp_sr:
          return Langs.sp_sp;
        case CourseIds.StudyDictsv_en:
        case CourseIds.StudyDictsv_fr:
        case CourseIds.StudyDictsv_it:
        case CourseIds.StudyDictsv_de:
        case CourseIds.StudyDictsv_ru:
        case CourseIds.StudyDictsv_sp:
        case CourseIds.StudyDictsv_cs:
        case CourseIds.StudyDictsv_sk:
        case CourseIds.StudyDictsv_bg:
        case CourseIds.StudyDictsv_hr:
        case CourseIds.StudyDictsv_ca:
        case CourseIds.StudyDictsv_hu:
        case CourseIds.StudyDictsv_nl:
        case CourseIds.StudyDictsv_pl:
        case CourseIds.StudyDictsv_pt:
        case CourseIds.StudyDictsv_ro:
        case CourseIds.StudyDictsv_el:
        case CourseIds.StudyDictsv_sl:
        case CourseIds.StudyDictsv_tr:
        case CourseIds.StudyDictsv_uk:
        case CourseIds.StudyDictsv_vi:
        case CourseIds.StudyDictsv_fi:
        case CourseIds.StudyDictsv_da:
        case CourseIds.StudyDictsv_nb:
        case CourseIds.StudyDictsv_sq:
        case CourseIds.StudyDictsv_ja:
        case CourseIds.StudyDictsv_ptbr:
        case CourseIds.StudyDictsv_zh:
        case CourseIds.StudyDictsv_ko:
        case CourseIds.StudyDictsv_ar:
        case CourseIds.StudyDictsv_he:
        case CourseIds.StudyDictsv_th:
        case CourseIds.StudyDictsv_lv:
        case CourseIds.StudyDictsv_lt:
        case CourseIds.StudyDictsv_mk:
        case CourseIds.StudyDictsv_sr:
          return Langs.sv_se;
        case CourseIds.StudyDictth_en:
        case CourseIds.StudyDictth_fr:
        case CourseIds.StudyDictth_it:
        case CourseIds.StudyDictth_de:
        case CourseIds.StudyDictth_ru:
        case CourseIds.StudyDictth_sp:
        case CourseIds.StudyDictth_cs:
        case CourseIds.StudyDictth_sk:
        case CourseIds.StudyDictth_bg:
        case CourseIds.StudyDictth_hr:
        case CourseIds.StudyDictth_ca:
        case CourseIds.StudyDictth_hu:
        case CourseIds.StudyDictth_nl:
        case CourseIds.StudyDictth_pl:
        case CourseIds.StudyDictth_pt:
        case CourseIds.StudyDictth_ro:
        case CourseIds.StudyDictth_el:
        case CourseIds.StudyDictth_sl:
        case CourseIds.StudyDictth_tr:
        case CourseIds.StudyDictth_uk:
        case CourseIds.StudyDictth_vi:
        case CourseIds.StudyDictth_fi:
        case CourseIds.StudyDictth_sv:
        case CourseIds.StudyDictth_da:
        case CourseIds.StudyDictth_nb:
        case CourseIds.StudyDictth_sq:
        case CourseIds.StudyDictth_ja:
        case CourseIds.StudyDictth_ptbr:
        case CourseIds.StudyDictth_zh:
        case CourseIds.StudyDictth_ko:
        case CourseIds.StudyDictth_ar:
        case CourseIds.StudyDictth_he:
        case CourseIds.StudyDictth_lv:
        case CourseIds.StudyDictth_lt:
        case CourseIds.StudyDictth_mk:
        case CourseIds.StudyDictth_sr:
          return Langs.th_th;
        case CourseIds.StudyDicttr_en:
        case CourseIds.StudyDicttr_fr:
        case CourseIds.StudyDicttr_it:
        case CourseIds.StudyDicttr_de:
        case CourseIds.StudyDicttr_ru:
        case CourseIds.StudyDicttr_sp:
        case CourseIds.StudyDicttr_cs:
        case CourseIds.StudyDicttr_sk:
        case CourseIds.StudyDicttr_bg:
        case CourseIds.StudyDicttr_hr:
        case CourseIds.StudyDicttr_ca:
        case CourseIds.StudyDicttr_hu:
        case CourseIds.StudyDicttr_nl:
        case CourseIds.StudyDicttr_pl:
        case CourseIds.StudyDicttr_pt:
        case CourseIds.StudyDicttr_ro:
        case CourseIds.StudyDicttr_el:
        case CourseIds.StudyDicttr_sl:
        case CourseIds.StudyDicttr_uk:
        case CourseIds.StudyDicttr_vi:
        case CourseIds.StudyDicttr_fi:
        case CourseIds.StudyDicttr_sv:
        case CourseIds.StudyDicttr_da:
        case CourseIds.StudyDicttr_nb:
        case CourseIds.StudyDicttr_sq:
        case CourseIds.StudyDicttr_ja:
        case CourseIds.StudyDicttr_ptbr:
        case CourseIds.StudyDicttr_zh:
        case CourseIds.StudyDicttr_ko:
        case CourseIds.StudyDicttr_ar:
        case CourseIds.StudyDicttr_he:
        case CourseIds.StudyDicttr_th:
        case CourseIds.StudyDicttr_lv:
        case CourseIds.StudyDicttr_lt:
        case CourseIds.StudyDicttr_mk:
        case CourseIds.StudyDicttr_sr:
          return Langs.tr_tr;
        case CourseIds.StudyDictuk_en:
        case CourseIds.StudyDictuk_fr:
        case CourseIds.StudyDictuk_it:
        case CourseIds.StudyDictuk_de:
        case CourseIds.StudyDictuk_ru:
        case CourseIds.StudyDictuk_sp:
        case CourseIds.StudyDictuk_cs:
        case CourseIds.StudyDictuk_sk:
        case CourseIds.StudyDictuk_bg:
        case CourseIds.StudyDictuk_hr:
        case CourseIds.StudyDictuk_ca:
        case CourseIds.StudyDictuk_hu:
        case CourseIds.StudyDictuk_nl:
        case CourseIds.StudyDictuk_pl:
        case CourseIds.StudyDictuk_pt:
        case CourseIds.StudyDictuk_ro:
        case CourseIds.StudyDictuk_el:
        case CourseIds.StudyDictuk_sl:
        case CourseIds.StudyDictuk_tr:
        case CourseIds.StudyDictuk_vi:
        case CourseIds.StudyDictuk_fi:
        case CourseIds.StudyDictuk_sv:
        case CourseIds.StudyDictuk_da:
        case CourseIds.StudyDictuk_nb:
        case CourseIds.StudyDictuk_sq:
        case CourseIds.StudyDictuk_ja:
        case CourseIds.StudyDictuk_ptbr:
        case CourseIds.StudyDictuk_zh:
        case CourseIds.StudyDictuk_ko:
        case CourseIds.StudyDictuk_ar:
        case CourseIds.StudyDictuk_he:
        case CourseIds.StudyDictuk_th:
        case CourseIds.StudyDictuk_lv:
        case CourseIds.StudyDictuk_lt:
        case CourseIds.StudyDictuk_mk:
        case CourseIds.StudyDictuk_sr:
          return Langs.uk_ua;
        case CourseIds.StudyDictvi_en:
        case CourseIds.StudyDictvi_fr:
        case CourseIds.StudyDictvi_it:
        case CourseIds.StudyDictvi_de:
        case CourseIds.StudyDictvi_ru:
        case CourseIds.StudyDictvi_sp:
        case CourseIds.StudyDictvi_cs:
        case CourseIds.StudyDictvi_sk:
        case CourseIds.StudyDictvi_bg:
        case CourseIds.StudyDictvi_hr:
        case CourseIds.StudyDictvi_ca:
        case CourseIds.StudyDictvi_hu:
        case CourseIds.StudyDictvi_nl:
        case CourseIds.StudyDictvi_pl:
        case CourseIds.StudyDictvi_pt:
        case CourseIds.StudyDictvi_ro:
        case CourseIds.StudyDictvi_el:
        case CourseIds.StudyDictvi_sl:
        case CourseIds.StudyDictvi_tr:
        case CourseIds.StudyDictvi_uk:
        case CourseIds.StudyDictvi_fi:
        case CourseIds.StudyDictvi_sv:
        case CourseIds.StudyDictvi_da:
        case CourseIds.StudyDictvi_nb:
        case CourseIds.StudyDictvi_sq:
        case CourseIds.StudyDictvi_ja:
        case CourseIds.StudyDictvi_ptbr:
        case CourseIds.StudyDictvi_zh:
        case CourseIds.StudyDictvi_ko:
        case CourseIds.StudyDictvi_ar:
        case CourseIds.StudyDictvi_he:
        case CourseIds.StudyDictvi_th:
        case CourseIds.StudyDictvi_lv:
        case CourseIds.StudyDictvi_lt:
        case CourseIds.StudyDictvi_mk:
        case CourseIds.StudyDictvi_sr:
          return Langs.vi_vn;
        case CourseIds.MidDictbg_en:
        case CourseIds.MidDictbg_fr:
        case CourseIds.MidDictbg_it:
        case CourseIds.MidDictbg_hu:
        case CourseIds.MidDictbg_de:
        case CourseIds.MidDictbg_pl:
        case CourseIds.MidDictbg_cs:
        case CourseIds.MidDictbg_sk:
        case CourseIds.MidDictbg_hr:
        case CourseIds.MidDictbg_ro:
        case CourseIds.MidDictbg_ru:
        case CourseIds.MidDictbg_sr:
        case CourseIds.MidDictbg_uk:
        case CourseIds.MidDictbg_sp:
        case CourseIds.MidDictbg_nl:
          return Langs.bg_bg;
        case CourseIds.MidDicthr_en:
        case CourseIds.MidDicthr_fr:
        case CourseIds.MidDicthr_it:
        case CourseIds.MidDicthr_hu:
        case CourseIds.MidDicthr_de:
        case CourseIds.MidDicthr_pl:
        case CourseIds.MidDicthr_cs:
        case CourseIds.MidDicthr_sk:
        case CourseIds.MidDicthr_bg:
        case CourseIds.MidDicthr_ro:
        case CourseIds.MidDicthr_ru:
        case CourseIds.MidDicthr_sr:
        case CourseIds.MidDicthr_uk:
        case CourseIds.MidDicthr_sp:
        case CourseIds.MidDicthr_nl:
          return Langs.hr_hr;
        case CourseIds.MidDictcs_en:
        case CourseIds.MidDictcs_fr:
        case CourseIds.MidDictcs_it:
        case CourseIds.MidDictcs_hu:
        case CourseIds.MidDictcs_de:
        case CourseIds.MidDictcs_pl:
        case CourseIds.MidDictcs_sk:
        case CourseIds.MidDictcs_bg:
        case CourseIds.MidDictcs_hr:
        case CourseIds.MidDictcs_ro:
        case CourseIds.MidDictcs_ru:
        case CourseIds.MidDictcs_sr:
        case CourseIds.MidDictcs_uk:
        case CourseIds.MidDictcs_sp:
        case CourseIds.MidDictcs_nl:
          return Langs.cs_cz;
        case CourseIds.MidDictnl_en:
        case CourseIds.MidDictnl_fr:
        case CourseIds.MidDictnl_it:
        case CourseIds.MidDictnl_hu:
        case CourseIds.MidDictnl_de:
        case CourseIds.MidDictnl_pl:
        case CourseIds.MidDictnl_cs:
        case CourseIds.MidDictnl_sk:
        case CourseIds.MidDictnl_bg:
        case CourseIds.MidDictnl_hr:
        case CourseIds.MidDictnl_ro:
        case CourseIds.MidDictnl_ru:
        case CourseIds.MidDictnl_sr:
        case CourseIds.MidDictnl_uk:
        case CourseIds.MidDictnl_sp:
          return Langs.nl_nl;
        case CourseIds.MidDicten_fr:
        case CourseIds.MidDicten_it:
        case CourseIds.MidDicten_hu:
        case CourseIds.MidDicten_de:
        case CourseIds.MidDicten_pl:
        case CourseIds.MidDicten_cs:
        case CourseIds.MidDicten_sk:
        case CourseIds.MidDicten_bg:
        case CourseIds.MidDicten_hr:
        case CourseIds.MidDicten_ro:
        case CourseIds.MidDicten_ru:
        case CourseIds.MidDicten_sr:
        case CourseIds.MidDicten_uk:
        case CourseIds.MidDicten_sp:
        case CourseIds.MidDicten_nl:
          return Langs.en_gb;
        case CourseIds.MidDictfr_en:
        case CourseIds.MidDictfr_it:
        case CourseIds.MidDictfr_hu:
        case CourseIds.MidDictfr_de:
        case CourseIds.MidDictfr_pl:
        case CourseIds.MidDictfr_cs:
        case CourseIds.MidDictfr_sk:
        case CourseIds.MidDictfr_bg:
        case CourseIds.MidDictfr_hr:
        case CourseIds.MidDictfr_ro:
        case CourseIds.MidDictfr_ru:
        case CourseIds.MidDictfr_sr:
        case CourseIds.MidDictfr_uk:
        case CourseIds.MidDictfr_sp:
        case CourseIds.MidDictfr_nl:
          return Langs.fr_fr;
        case CourseIds.MidDictde_en:
        case CourseIds.MidDictde_fr:
        case CourseIds.MidDictde_it:
        case CourseIds.MidDictde_hu:
        case CourseIds.MidDictde_pl:
        case CourseIds.MidDictde_cs:
        case CourseIds.MidDictde_sk:
        case CourseIds.MidDictde_bg:
        case CourseIds.MidDictde_hr:
        case CourseIds.MidDictde_ro:
        case CourseIds.MidDictde_ru:
        case CourseIds.MidDictde_sr:
        case CourseIds.MidDictde_uk:
        case CourseIds.MidDictde_sp:
        case CourseIds.MidDictde_nl:
          return Langs.de_de;
        case CourseIds.MidDicthu_en:
        case CourseIds.MidDicthu_fr:
        case CourseIds.MidDicthu_it:
        case CourseIds.MidDicthu_de:
        case CourseIds.MidDicthu_pl:
        case CourseIds.MidDicthu_cs:
        case CourseIds.MidDicthu_sk:
        case CourseIds.MidDicthu_bg:
        case CourseIds.MidDicthu_hr:
        case CourseIds.MidDicthu_ro:
        case CourseIds.MidDicthu_ru:
        case CourseIds.MidDicthu_sr:
        case CourseIds.MidDicthu_uk:
        case CourseIds.MidDicthu_sp:
        case CourseIds.MidDicthu_nl:
          return Langs.hu_hu;
        case CourseIds.MidDictit_en:
        case CourseIds.MidDictit_fr:
        case CourseIds.MidDictit_hu:
        case CourseIds.MidDictit_de:
        case CourseIds.MidDictit_pl:
        case CourseIds.MidDictit_cs:
        case CourseIds.MidDictit_sk:
        case CourseIds.MidDictit_bg:
        case CourseIds.MidDictit_hr:
        case CourseIds.MidDictit_ro:
        case CourseIds.MidDictit_ru:
        case CourseIds.MidDictit_sr:
        case CourseIds.MidDictit_uk:
        case CourseIds.MidDictit_sp:
        case CourseIds.MidDictit_nl:
          return Langs.it_it;
        case CourseIds.MidDictpl_en:
        case CourseIds.MidDictpl_fr:
        case CourseIds.MidDictpl_it:
        case CourseIds.MidDictpl_hu:
        case CourseIds.MidDictpl_de:
        case CourseIds.MidDictpl_cs:
        case CourseIds.MidDictpl_sk:
        case CourseIds.MidDictpl_bg:
        case CourseIds.MidDictpl_hr:
        case CourseIds.MidDictpl_ro:
        case CourseIds.MidDictpl_ru:
        case CourseIds.MidDictpl_sr:
        case CourseIds.MidDictpl_uk:
        case CourseIds.MidDictpl_sp:
        case CourseIds.MidDictpl_nl:
          return Langs.pl_pl;
        case CourseIds.MidDictro_en:
        case CourseIds.MidDictro_fr:
        case CourseIds.MidDictro_it:
        case CourseIds.MidDictro_hu:
        case CourseIds.MidDictro_de:
        case CourseIds.MidDictro_pl:
        case CourseIds.MidDictro_cs:
        case CourseIds.MidDictro_sk:
        case CourseIds.MidDictro_bg:
        case CourseIds.MidDictro_hr:
        case CourseIds.MidDictro_ru:
        case CourseIds.MidDictro_sr:
        case CourseIds.MidDictro_uk:
        case CourseIds.MidDictro_sp:
        case CourseIds.MidDictro_nl:
          return Langs.ro_ro;
        case CourseIds.MidDictru_en:
        case CourseIds.MidDictru_fr:
        case CourseIds.MidDictru_it:
        case CourseIds.MidDictru_hu:
        case CourseIds.MidDictru_de:
        case CourseIds.MidDictru_pl:
        case CourseIds.MidDictru_cs:
        case CourseIds.MidDictru_sk:
        case CourseIds.MidDictru_bg:
        case CourseIds.MidDictru_hr:
        case CourseIds.MidDictru_ro:
        case CourseIds.MidDictru_sr:
        case CourseIds.MidDictru_uk:
        case CourseIds.MidDictru_sp:
        case CourseIds.MidDictru_nl:
          return Langs.ru_ru;
        case CourseIds.MidDictsr_en:
        case CourseIds.MidDictsr_fr:
        case CourseIds.MidDictsr_it:
        case CourseIds.MidDictsr_hu:
        case CourseIds.MidDictsr_de:
        case CourseIds.MidDictsr_pl:
        case CourseIds.MidDictsr_cs:
        case CourseIds.MidDictsr_sk:
        case CourseIds.MidDictsr_bg:
        case CourseIds.MidDictsr_hr:
        case CourseIds.MidDictsr_ro:
        case CourseIds.MidDictsr_ru:
        case CourseIds.MidDictsr_uk:
        case CourseIds.MidDictsr_sp:
        case CourseIds.MidDictsr_nl:
          return Langs.sr_latn_cs;
        case CourseIds.MidDictsk_en:
        case CourseIds.MidDictsk_fr:
        case CourseIds.MidDictsk_it:
        case CourseIds.MidDictsk_hu:
        case CourseIds.MidDictsk_de:
        case CourseIds.MidDictsk_pl:
        case CourseIds.MidDictsk_cs:
        case CourseIds.MidDictsk_bg:
        case CourseIds.MidDictsk_hr:
        case CourseIds.MidDictsk_ro:
        case CourseIds.MidDictsk_ru:
        case CourseIds.MidDictsk_sr:
        case CourseIds.MidDictsk_uk:
        case CourseIds.MidDictsk_sp:
        case CourseIds.MidDictsk_nl:
          return Langs.sk_sk;
        case CourseIds.MidDictsp_en:
        case CourseIds.MidDictsp_fr:
        case CourseIds.MidDictsp_it:
        case CourseIds.MidDictsp_hu:
        case CourseIds.MidDictsp_de:
        case CourseIds.MidDictsp_pl:
        case CourseIds.MidDictsp_cs:
        case CourseIds.MidDictsp_sk:
        case CourseIds.MidDictsp_bg:
        case CourseIds.MidDictsp_hr:
        case CourseIds.MidDictsp_ro:
        case CourseIds.MidDictsp_ru:
        case CourseIds.MidDictsp_sr:
        case CourseIds.MidDictsp_uk:
        case CourseIds.MidDictsp_nl:
          return Langs.sp_sp;
        case CourseIds.MidDictuk_en:
        case CourseIds.MidDictuk_fr:
        case CourseIds.MidDictuk_it:
        case CourseIds.MidDictuk_hu:
        case CourseIds.MidDictuk_de:
        case CourseIds.MidDictuk_pl:
        case CourseIds.MidDictuk_cs:
        case CourseIds.MidDictuk_sk:
        case CourseIds.MidDictuk_bg:
        case CourseIds.MidDictuk_hr:
        case CourseIds.MidDictuk_ro:
        case CourseIds.MidDictuk_ru:
        case CourseIds.MidDictuk_sr:
        case CourseIds.MidDictuk_sp:
        case CourseIds.MidDictuk_nl:
          return Langs.uk_ua;
        #endregion
        case CourseIds.IsEduLand_Other:
        case CourseIds.eTestMeBig:
        case CourseIds.eTestMeSmall:
        /*case CourseIds.eTestMe_A1:
        case CourseIds.eTestMe_A2:
        case CourseIds.eTestMe_B1:
        case CourseIds.eTestMe_B2:
        case CourseIds.eTestMe_C1:
        case CourseIds.eTestMe_C2:
        case CourseIds.eTestMe_All:*/
        case CourseIds.no: return Langs.no;
        default: if (raiseExp) throw new Exception(line.ToString()); else return Langs.no;
      }
    }
    public static CourseIds LangToCourseId(Langs lng) {
      switch (lng) {
        case Langs.en_gb: return CourseIds.English;
        case Langs.de_de: return CourseIds.German;
        case Langs.sp_sp:
        case Langs.es_es: return CourseIds.Spanish;
        case Langs.it_it: return CourseIds.Italian;
        case Langs.fr_fr: return CourseIds.French;
        case Langs.ru_ru: return CourseIds.Russian;
        default: return CourseIds.no;
      }
    }
    public static CultureInfo LangToCulture(Langs l) {
      if (l == Langs.sp_sp) l = Langs.es_es;
      return CultureInfo.GetCultureInfo(l.ToString().Replace('_', '-'));
    }
    public static int LangToLCID(Langs l) {
      if (l == Langs.sp_sp) l = Langs.es_es;
      var cult = CultureInfo.GetCultureInfo(l.ToString().Replace('_', '-'));
      return cult.TextInfo.LCID;
    }
    public static LineIds LangToLineId(Langs lang) {
      switch (lang) {
        case Langs.af_za: return LineIds.Afrikaans;
        case Langs.sq_al: return LineIds.Albanian;
        case Langs.ar_sa: return LineIds.Arabic;
        case Langs.hy_am: return LineIds.Armenian;
        case Langs.as_in: return LineIds.Assamese;
        case Langs.az_latn_az: return LineIds.Azerbaijani;
        case Langs.eu_es: return LineIds.Basque;
        case Langs.bn_in: return LineIds.Bengali;
        case Langs.pt_br: return LineIds.Portuguese_Brazilian;
        case Langs.br_fr: return LineIds.Breton;
        case Langs.bg_bg: return LineIds.Bulgarian;
        case Langs.bs: return LineIds.Bossna;
        case Langs.fr_fr: return LineIds.French;
        case Langs.zh_hk: return LineIds.Cantonese;
        case Langs.ca_es: return LineIds.Catalan;
        case Langs.co_fr: return LineIds.Corsican;
        case Langs.hr_hr: return LineIds.Croatian;
        case Langs.cs_cz: return LineIds.Czech;
        case Langs.da_dk: return LineIds.Danish;
        case Langs.nl_nl: return LineIds.Dutch;
        case Langs.en_nz:
        case Langs.en_gb:
          return LineIds.English;
        case Langs.et_ee: return LineIds.Estonian;
        case Langs.fi_fi: return LineIds.Finnish;
        case Langs.gl_es: return LineIds.Galician;
        case Langs.ka_ge: return LineIds.Georgian;
        case Langs.de_de: return LineIds.German;
        case Langs.el_gr: return LineIds.Greek;
        case Langs.ha_latn_ng: return LineIds.Hausa;
        case Langs.he_il: return LineIds.Hebrew;
        case Langs.hu_hu: return LineIds.Hungarian;
        case Langs.zh_cn: return LineIds.Chinese_Mandarin;
        case Langs.is_is: return LineIds.Icelandic;
        case Langs.ig_ng: return LineIds.Igbo;
        case Langs.id_id: return LineIds.Indonesian;
        case Langs.ga_ie: return LineIds.Irish;
        case Langs.it_it: return LineIds.Italian;
        case Langs.ja_jp: return LineIds.Japanese;
        case Langs.km_kh: return LineIds.Khmer;
        case Langs.ky_kg: return LineIds.Kirghiz;
        case Langs.ko_kr: return LineIds.Korean;
        case Langs.es_es: 
        case Langs.sp_sp: return LineIds.Spanish;
        case Langs.lv_lv: return LineIds.Latvian;
        case Langs.lt_lt: return LineIds.Lithuanian;
        case Langs.mk_mk: return LineIds.Macedonian;
        case Langs.ms_my: return LineIds.Malay;
        case Langs.ml_in: return LineIds.Malayalam;
        case Langs.mt_mt: return LineIds.Maltese;
        case Langs.mi_nz: return LineIds.Maori;
        case Langs.mn_mn: return LineIds.Mongolian;
        case Langs.oc_fr: return LineIds.Occitan;
        case Langs.nb_no: return LineIds.Norwegian;
        case Langs.ps_af: return LineIds.Pashto;
        case Langs.fa_ir: return LineIds.Farsi;
        case Langs.pl_pl: return LineIds.Polish;
        case Langs.pt_pt: return LineIds.Portuguese;
        case Langs.quz_pe: return LineIds.Quechua;
        case Langs.ro_ro: return LineIds.Romanian;
        case Langs.ru_ru: return LineIds.Russian;
        case Langs.sr_latn_cs: return LineIds.Serbian;
        case Langs.nso_za: return LineIds.Sesotho;
        case Langs.sk_sk: return LineIds.Slovak;
        case Langs.sl_si: return LineIds.Slovenian;
        case Langs.sw_ke: return LineIds.Swahili;
        case Langs.sv_se: return LineIds.Swedish;
        case Langs.th_th: return LineIds.Thai;
        case Langs.bo_cn: return LineIds.Tibetan;
        case Langs.tn_za: return LineIds.Tswana;
        case Langs.tr_tr: return LineIds.Turkish;
        case Langs.uk_ua: return LineIds.Ukrainian;
        case Langs.ur_pk: return LineIds.Urdu;
        case Langs.uz_latn_uz: return LineIds.Uzbek;
        case Langs.vi_vn: return LineIds.Vietnamese;
        case Langs.xh_za: return LineIds.Xhosa;
        case Langs.yo_ng: return LineIds.Yoruba;
        case Langs.zu_za: return LineIds.Zulu;
        case Langs.be_by: return LineIds.Belarusian;
        case Langs.gu_in: return LineIds.Gujarati;
        case Langs.hi_in: return LineIds.Hindi;
        case Langs.kn_in: return LineIds.Kannada;
        case Langs.ta_in: return LineIds.Tamil;
        case Langs.te_in: return LineIds.Telugu;
        case Langs.cy_gb: return LineIds.Welsh;

        default: return LineIds.no;
      }
    }
    public static Langs LineIdToLang(LineIds line) {
      switch (line) {
        #region EuroTalk4
        case LineIds.Afrikaans: return Langs.af_za;
        case LineIds.Albanian: return Langs.sq_al;
        case LineIds.Arabic: return Langs.ar_sa;
        case LineIds.Armenian: return Langs.hy_am;
        case LineIds.Assamese: return Langs.as_in;
        case LineIds.Azerbaijani: return Langs.az_latn_az;
        case LineIds.Basque: return Langs.eu_es;
        case LineIds.Bengali: return Langs.bn_in;
        case LineIds.Portuguese_Brazilian: return Langs.pt_br;
        case LineIds.Breton: return Langs.br_fr;
        case LineIds.Bulgarian: return Langs.bg_bg;
        case LineIds.Bossna: return Langs.bs;
        case LineIds.French: return Langs.fr_fr;
        case LineIds.Cantonese: return Langs.zh_hk;
        case LineIds.Catalan: return Langs.ca_es;
        case LineIds.Corsican: return Langs.co_fr;
        case LineIds.Croatian: return Langs.hr_hr;
        case LineIds.Czech: return Langs.cs_cz;
        case LineIds.Danish: return Langs.da_dk;
        case LineIds.Dutch: return Langs.nl_nl;
        case LineIds.English: return Langs.en_gb;
        case LineIds.Estonian: return Langs.et_ee;
        case LineIds.Farsi: return Langs.fa_ir;
        case LineIds.Finnish: return Langs.fi_fi;
        case LineIds.Galician: return Langs.gl_es;
        case LineIds.Georgian: return Langs.ka_ge;
        case LineIds.German: return Langs.de_de;
        case LineIds.Greek: return Langs.el_gr;
        case LineIds.Hausa: return Langs.ha_latn_ng;
        case LineIds.Hebrew: return Langs.he_il;
        case LineIds.Hungarian: return Langs.hu_hu;
        case LineIds.Hindi: return Langs.hi_in;
        case LineIds.Chinese_Mandarin: return Langs.zh_cn;
        case LineIds.Icelandic: return Langs.is_is;
        case LineIds.Igbo: return Langs.ig_ng;
        case LineIds.Indonesian: return Langs.id_id;
        case LineIds.Irish: return Langs.ga_ie;
        case LineIds.Italian: return Langs.it_it;
        case LineIds.Japanese: return Langs.ja_jp;
        case LineIds.Khmer: return Langs.km_kh;
        case LineIds.Kirghiz: return Langs.ky_kg;
        case LineIds.Korean: return Langs.ko_kr;
        case LineIds.Spanish: return Langs.sp_sp;
        case LineIds.Latvian: return Langs.lv_lv;
        case LineIds.Lithuanian: return Langs.lt_lt;
        case LineIds.Macedonian: return Langs.mk_mk;
        case LineIds.Malay: return Langs.ms_my;
        case LineIds.Malayalam: return Langs.ml_in;
        case LineIds.Maltese: return Langs.mt_mt;
        case LineIds.Maori: return Langs.mi_nz;
        case LineIds.Mongolian: return Langs.mn_mn;
        case LineIds.Occitan: return Langs.oc_fr;
        case LineIds.Norwegian: return Langs.nb_no;
        case LineIds.Pashto: return Langs.ps_af;
        case LineIds.Persian: return Langs.fa_ir;
        case LineIds.Polish: return Langs.pl_pl;
        case LineIds.Portuguese: return Langs.pt_pt;
        case LineIds.Quechua: return Langs.quz_pe;
        case LineIds.Romanian: return Langs.ro_ro;
        case LineIds.Russian: return Langs.ru_ru;
        case LineIds.Serbian: return Langs.sr_latn_cs;
        case LineIds.Sesotho: return Langs.nso_za;
        case LineIds.Slovak: return Langs.sk_sk;
        case LineIds.Slovenian: return Langs.sl_si;
        case LineIds.Swahili: return Langs.sw_ke;
        case LineIds.Swedish: return Langs.sv_se;
        case LineIds.Thai: return Langs.th_th;
        case LineIds.Tibetan: return Langs.bo_cn;
        case LineIds.Tswana: return Langs.tn_za;
        case LineIds.Turkish: return Langs.tr_tr;
        case LineIds.Ukrainian: return Langs.uk_ua;
        case LineIds.Urdu: return Langs.ur_pk;
        case LineIds.Uzbek: return Langs.uz_latn_uz;
        case LineIds.Vietnamese: return Langs.vi_vn;
        case LineIds.Xhosa: return Langs.xh_za;
        case LineIds.Yoruba: return Langs.yo_ng;
        case LineIds.Zulu: return Langs.zu_za;
        #endregion
        default: return Langs.no;
      }
    }
  }
  //Neubirat, jen pridavat, je v downloadech!!!
  //po pridani zmenit i Q:\lmcom\LMCom\Services\Downloads\RunExeGui\JScript.js
  public class AudioOutFormat {
    public SoundFormat Format;
    public int OutSampleRate;
    public AudioMP3BitRate MaximumBitrate;
    public MP3Quality MP3Quality;
    public string Encode() {
      byte[] res = new byte[7];
      res[0] = (byte)Format;
      byte[] sr = BitConverter.GetBytes(OutSampleRate);
      Array.Copy(sr, 0, res, 1, 4);
      res[5] = (byte)MaximumBitrate;
      res[6] = (byte)MP3Quality;
      //Int64 lng = BitConverter.ToInt64(res, 0);
      //return lng;
      return LowUtils.bytesToString(res);
    }
  }

  public interface ISitemapNode {
    string NodeTitle { get; }
    string Url { get; }
    ISitemapNode Parent { get; }
    IEnumerable<ISitemapNode> Items { get; }
    int IconId { get; }
    //bool IsTreeViewExpanded { get; set; }
    //bool IsTreeViewSelected { get; set; }
  }

  public static class Extensions {
    public static string ElementValue(this XElement els, XName name) {
      return ElementValue(els, name, null);
    }

    public static string ElementValue(this XElement els, XName name, string defaultValue) {
      XElement el = els.Element(name);
      return el == null ? defaultValue : el.Value;
    }

    public static string AttributeValue(this XElement els, XName name, string defaultValue) {
      XAttribute attr = els.Attribute(name);
      return attr == null ? defaultValue : attr.Value;
    }
    public static void SetAttributeValue(this XElement els, XName name, string value) {
      XAttribute attr = els.Attribute(name);
      if (attr == null) els.Add(new XAttribute(name, value)); else attr.Value = value;
    }
    public static string AttributeValue(this XElement els, XName name) {
      return AttributeValue(els, name, null);
    }

    public static string soundNormalize(string w, StringBuilder sb) {
      if (sb == null) sb = new StringBuilder();
      sb.Length = 0;
      w = w.ToLower();
      bool inBlank = false;
      foreach (char ch in w)
        if (char.IsWhiteSpace(ch)) {
          if (!inBlank) sb.Append(ch);
          inBlank = true;
        } else if (char.IsLetterOrDigit(ch) || ch == '?' || ch == '\'') {
          sb.Append(ch);
          inBlank = false;
        } else if (inBlank) { //oddelovac, mezera jiz OK
        } else { //oddelovac, mezera jeste neni
          sb.Append(' ');
          inBlank = true;
        }
      return sb.ToString().Trim();
    }

  }



}
namespace CMLib {

  public enum Protocol {
    no, Pop3, SMTP,
  };


  public enum TableType {
    no = 0,
    contact = 1,
    task = 2,
    user = 3,
    email = 4,
    comment = 5,
    contact_contactFolder = 6,
    task_contact = 7,
    task_email = 8,
    campaign = 9,
    campaccepter = 10,
    campuse = 11,

    Comment_Contact = 101,
    Comment_Email = 102,
    Comment_Task = 103,
    Tag_Contact = 104,
    Tag_Email = 105,
    Tag_Task = 106,
    /// <summary>
    /// EmailEdit
    /// </summary>
    EMail_From = 110,
    /// <summary>
    /// EmailRead
    /// </summary>
    EMail_To = 111,
    PhoneCall = 112,
    Meeting = 113,
    Tag = 120
  }



  public enum EMailStatus {
    no,
    /// <summary>
    /// ReadOnly emaily přijate
    /// </summary>
    In,
    /// <summary>
    /// ReadOnly emaily odeslane
    /// </summary>
    Out,
    /// <summary>
    /// Jeste neodeslane emaily (mozno je editovat)
    /// </summary>
    Draft,
    /// <summary>
    /// EMaily k odeslani
    /// </summary>
    WaitOut,
  };

  public enum EmailToTypes {
    to,
    cc,
    bcc,
    from,
  }

  public enum EmailToPart {
    addr,
    name,
    addrname,
  }

  public enum EMailCampaignStatus {
    clientCreated,
    serverFinished,
    error
  }

  //zpusob nacteni emailu
  public enum GetEmailsType {
    no,
    TosNames, //nactou se To adresy i jmena priloh
  }

  //zpusob nacteni emailu
  public enum GetContactType {
    no, //pouze Contact objekt
    Folder, //ke kontaktu se nacte i seznam folderu
  }

  public enum DeleteType {
    email
  }

  public enum DefaultEMailFolders {
    no, Inbox, Send_Items, Outbox, Deleted, Drafts, Campaigns, last
  };

  public enum DefaultContactFolders {
    no, Users, NewContacts, last
  };

  public enum DefaultFileFolders {
    no, Default, last
  };

  public enum DefaultRoles {
    no, Admin, last
  };

  public enum DefaultTags {
    no, Red, Blue, Yellow, Orange, Green, last,
  };

  public enum File_GetType {
    no,
    recurseFiles
  }

  public enum CustomerDataIds {
    EMailFolder, ContactFolder, FileFolder, Role, Tag, EMailAccounts,
    TaskProjects, TaskStatus, TaskPriority, TaskCategory,
    fake_Acepters,
    fake_TagNoPrivate,
    fake_UserIdentities,
    fake_CampAccepterFiles
  };

  public enum NotifyType {
    emailIn = 1,
    emailOut = 2,
    editContact = 3,
    addComment = 4,
    editComment = 5,
    newTask = 6,
    editTask = 7,
  }


  public enum FileUploadType {
    Attachment,
    File,
    Temp,
  }
}
