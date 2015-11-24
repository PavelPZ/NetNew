using LMComLib;
using LMNetLib;
//http://usejsdoc.org/index.html
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace CourseModel {
#pragma warning disable 1591

  #region BASIC

  [Flags]
  public enum CourseDataFlag {
    needsEval = 0x1, //cviceni vyzaduje vyhodnoceni ucitelem.
    //cviceni neni vyhondotitelne pocitacem (z "needsEval" plyne "pcCannotEvaluate".
    //pro ucitele je mozne pripravit Browse vsech techto cviceni pro zaky.
    pcCannotEvaluate = 0x2,
    hasExternalAttachments = 0x4, //cviceni obsahuje odkazy na externi soubory (napr. .mp3) 
    done = 0x8, //hotove cviceni
    passive = 0x10, //pasivni (nevyhodnotitelne) cviceni

    //ruzne typy zaznamu
    testImpl_result = 0x20, //TestMe\TestExercise.ts, testMe.testImpl, testImpl.resultKey
    testImpl = 0x40, //TestMe\TestExercise.ts, testMe.testImpl
    testSkillImpl = 0x80, //TestMe\TestExercise.ts, testMe.testSkillImpl
    ex = 0x100, //jakekoliv cviceni
    skipAbleRoot = 0x200, //skipAbleRoot
    modImpl = 0x400, //modImpl
    pretestImp = 0x800, //pretestImp
    multiTestImpl = 0x1000, //multiTestImpl
    testEx = 0x2000, //rozsireni informace o "ex": navic cviceni testu

    blPretestItem = 0x4000,
    blLesson = 0x8000,
    blTest = 0x10000,

    blPretest = 0x20000,
    blProductHome = 0x40000,

    blPretestEx = 0x80000,

    //allSpecialTypes = testImpl_result | testImpl | testSkillImpl,

    all = needsEval | pcCannotEvaluate | hasExternalAttachments | done | passive | testImpl_result | testImpl | testSkillImpl | testEx | skipAbleRoot | modImpl | pretestImp | multiTestImpl | testEx,
  }


  //*****************************************
  //              Evicence kontrolek
  //*****************************************
  [Flags]
  public enum tgSt {
    //****** class
    //JSStatus
    //jsGenericHtml = 0, //default, jedna se o standardni HTML tagy. Nejsou obaleny Course.control a maji standardni c_tag template
    jsCtrl = 0x1, //kontrolky - maji JS Course.control class, maji vlastni template, maji initProc(phase) call
    //jsNo = 0x2, //v JS se nevyskytuji (napr. templates se rozgeneruji jiz v C# runtime) nebo nemaji vlastni template (napr. smartTag, tag) a pouzije se c_genitems template
    cdata = 0x2,

    csControl = 0x4, //tag je v CS kodu chapan jako kontrolka (neni to tedy prosty HTML tag). Zpracovava se v adjustWidths, postProcess_AdjustId, postProcess_ExpandMacros, vola se checkAndFinish apod.

    isEval = 0x8, //vyhodnotitelna CSControl

    //****** attr
    //inItems = 0x10, //atribut je zadan v items
    isArray = 0x20, //atribut je array. Budto muze byt zaroven inItems nebo string attribut (| delimited)
    noJSONQuote = 0x40, //boolean nebo number attribute => pri prevodu z HTML do JSON se nepouzivaji uvozovky 

    //****** doc
    docIgnore = 0x80, //ignore prop x tag
    xsdIgnore = 0x100, //negeneruj do XSD
    xmlIgnore = 0x200, //XmlIgnore atribut
    jsonIgnore = 0x400, //JsonIgnore atribut
    //docIsHtml = 0x100, //html prop x tag
    //docXrefValued = 0x80, //je property, u ktere se pri cross referencich sleduje i hodnota. Vsechny enum props jsou takove automaticky
    //docXrefIgnore = 0x200, //nesleduj prop x tag v cross referencich
    //****** typ zpracovani XML attributu pri CSS preprocessu 
    //cssTagIds = 0x800, //hodnotou je seznam selektoru a hodnot, (napr. "#(#id1, .class)|w1|w2"). Vysledkem se seznam Idu a hodnot (napr. "#id1|#id2|w1|w2") 
    obsolete = 0x800, //prijde vyhodit

    //xsdHtml, //rozsireni HTML tagu => tag odvozen z flowContentElement XSD HTML5 elementu, ma mixed="true"
    //xsdHtmlAttrs, //HTML atributy => tag ma globalAttributeGroup atributy
    xsdHtmlEl = 0x1000, //HTML childs => tag ma flowContent sequence group a mixed="true" (pokud neni xsdNoMixed)
    xsdNoMixed = 0x2000, //viz xsdHtmlEl
    xsdString = 0x4000, //element ma cdata nebo string content
    xsdNoGlobal = 0x8000, //element neni v obecnych Items
    xsdIgnoreTagAttrs = 0x10000, //element nema tag attributes (class, ...)
    xsdMixed = 0x20000, //force mixed
    xsdRequiredAttr = 0x40000, //required

    //JS editor
    metaJS_browse = 0x80000, //property je povolena v napr. gap-fill(XXX) atributech


  }


  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Enum, Inherited = false)]
  public class tgAtAttribute : Attribute {
    public tgAtAttribute(tgSt status) { this.st = status; }
    public tgSt st;
    public string xsdType;
    public string _oldName;
    public string copyDocFrom;
    public string xsdChildElements; //seznam povolenych child XSD elementu
    public string childPropTypes; //nazev typu childu, ktery se dosadi do teto property
    public static tgAtAttribute fromTag(MemberInfo tp) { return tp.GetCustomAttributes(typeof(tgAtAttribute), false).Cast<tgAtAttribute>().FirstOrDefault(); }
    public static tgSt flagsFromTag(MemberInfo tp) { var attr = tp.GetCustomAttributes(typeof(tgAtAttribute), false).Cast<tgAtAttribute>().FirstOrDefault(); return attr == null ? 0 : attr.st; }
    public static IEnumerable<tgSt> sts(MemberInfo tp, params tgSt[] from) {
      var attr = fromTag(tp); if (attr == null) yield break;
      foreach (var st in from ?? LMNetLib.LowUtils.EnumGetValues<tgSt>()) if ((attr.st & st) == st) yield return st;
    }
    public static string stsStr(MemberInfo tp, params tgSt[] from) {
      return sts(tp, from).Select(s => "tgSt." + s.ToString()).DefaultIfEmpty("0").Aggregate((r, i) => r + " | " + i);
    }
    public bool isSt(tgSt st) { return (this.st & st) == st; }
  }

  public enum JSStatus {
    no, //v JS se nevyskytuji (napr. templates se rozgeneruji jiz v C# runtime) nebo nemaji vlastni template (napr. smartTag) a pouzije se c_noop template
    //JS tagy
    genericHtml, //jedna se o standardni HTML tagy. Nejsou obaleny Course.control a maji standardni c_tag template
    ctrl, //kontrolky - maji JS Course.control class a maji vlastni template
  }

  [AttributeUsage(AttributeTargets.Class)]
  public class TagAttributeAttribute : Attribute {
    public TagAttributeAttribute() : base() { CSControl = false; isEval = false; JSStatus = JSStatus.genericHtml; }
    // tak je v CS kodu chapan jako kontrolka (neni to tedy prosty HTML tag). Zpracovava se v adjustWidths, postProcess_AdjustId, postProcess_ExpandMacros, vola se checkAndFinish apod.
    public bool CSControl;
    // vyhodnotitelna CSControl
    public bool isEval;
    // typ template v JS runtime
    public JSStatus JSStatus;
  }

  //evidence kontrolek
  public class TagStatic {
    static TagStatic() {
      tagInfo = allTagTypes().ToDictionary(t => t.Name, t => new TagStatic(t));
    }
    public TagStatic(Type tp) {
      var attr = tp.GetCustomAttributes(typeof(TagAttributeAttribute), true).OfType<TagAttributeAttribute>().First();
      if (tp == typeof(singleChoice)) {
        isEval = attr.isEval;
      }
      isEval = attr.isEval;
      CSControl = attr.CSControl;
      JSStatus = attr.JSStatus;
      //if (JSStatus != JSStatus.no && tp.IsSubclassOf(typeof(tagStyled))) throw new Exception("JSStatus != JSStatus.no && GetType().IsSubclassOf(typeof(TagStyled))");
    }
    [JsonIgnore]
    public bool CSControl;
    public bool isEval;
    public JSStatus JSStatus;

    public static Dictionary<string, TagStatic> tagInfo;
    public static IEnumerable<Type> allTagTypes() {
      var tg = typeof(tag);
      return tg.Assembly.GetTypes().Where(t => t == tg || t.IsSubclassOf(tg));
    }
  }

  //*****************************************
  //              Tag
  //*****************************************
  /// <summary>
  /// tag
  /// </summary>
  /// tag descr
  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
  [TagAttribute]
  public partial class tag {
    public const string c_default = "default";

    public IEnumerable<tag> allItems() { return Items == null || Items.Length == 0 ? getTagProps() : Items.Concat(getTagProps()); }

    protected object[] getTagItems() { return allItems().Select(it => it is text ? (object)((text)it).title : it).ToArray(); }

    public virtual IEnumerable<tag> getTagProps() { yield break; }

    /** @summary jednoznacna identifikace elementu
     *  @descr ??
    */
    [tgAt(tgSt.metaJS_browse, xsdType = "xs:ID"), XmlAttribute]
    public string id;

    /** @summary seznam CSS classes
     *  @descr ??
    */
    [tgAt(tgSt.isArray | tgSt.docIgnore), XmlAttribute]
    public string[] @class {
      get { return cls != null && cls.Length == 1 ? cls.Concat(new string[] { " " }).ToArray() : cls; } //nejaka chyba v XML serializaci: class="no-class" nefunguje
      set { cls = value == null ? null : value.ToArray(); }
    }
    string[] cls;

    [tgAt(tgSt.docIgnore), XmlIgnore, JsonIgnore]
    public string classSetter {
      set { if (string.IsNullOrEmpty(value)) cls = null; else cls = value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); }
    }

    /** @summary CSS pro descendant elementy tohoto elementu, napr. gap-fill { case-sensitive:true; }
     *  @descr 
    */
    [XmlAttribute(AttributeName = "style-sheet"), JsonIgnore]
    public string styleSheet;

    [XmlAttribute, tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
    public string srcpos;

    //[tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
    //[JsonIgnore]
    //public attr[] attrs;

    public const string _parentProp = "parent-prop";
    public const string _javascriptType = "data";
    public static HashSet<string> preLike = new HashSet<string>() { "pre", "code", "script" };
    public tgAtAttribute tgAttribute() {
      return this.GetType().GetCustomAttributes(typeof(tgAtAttribute), false).Cast<tgAtAttribute>().FirstOrDefault();
    }

    //public static Dictionary<string, string> constrainsRegEx = new Dictionary<string, string>() {
    //  {evalControl.xsdType_eval_group, evalControl.xsdType_eval_group_regex},
    //  {sentReplace .sndReplace_sndDialogIdx, sentReplace .sndReplace_sndDialogIdx_regex},
    //  {urlTag.mediaTag_format, urlTag.mediaTag_format_regex},
    //};
  }

  public class attr {
    [XmlAttribute]
    public string name;
    [XmlAttribute]
    public string value;
  }

  #endregion

  #region GENERAL
  //*****************************************
  //              General Types
  //*****************************************
  /// <summary>
  /// Dialog speaker identification
  /// </summary>
  /// descr
  [tgAt(0, _oldName="replica-actor")]
  public enum IconIds {
    no,
    /// <summary>
    /// Dialog speaker A
    /// </summary>
    a,
    b,
    c,
    d,
    e,
    f
  }

  [tgAt(tgSt.docIgnore)]
  public enum inlineControlTypes {
    no,
    GapFill,
    GapFill_Correction,
    WordSelection,
    DragTarget,
    img,
    TtsSound,
  }

  [tgAt(tgSt.docIgnore)]
  public enum inlineElementTypes {
    no,
    gapFill,
    gapFillCorrection,
    wordSelection,
    dropDown,
    img,
    ttsSound,
  }

  public enum CheckItemTexts {
    [tgAt(0, _oldName = "yes-no"), XmlEnum(Name = "yes-no")]
    yesNo,
    [tgAt(0, _oldName = "true-false"), XmlEnum(Name = "true-false")]
    trueFalse,
    no,
  }

  public partial class seeAlsoLink {
    [XmlAttribute]
    public string url;
    [XmlAttribute]
    public string title;
  }

  public partial class head {
    public string title;
    public string style;
  }
  public partial class html {
    public head head;
    public body body;
  }

  public partial class Score {
    [XmlAttribute, DefaultValue(0)]
    public int s;
    [XmlAttribute, DefaultValue(0)]
    public int ms;
    [XmlAttribute, DefaultValue(0)]
    public CourseDataFlag flag;
  }
  #endregion

  #region Data tags
  //*****************************************
  //              Data tags
  //*****************************************
  [tgAt(/*tgSt.jsNo |*/ tgSt.csControl | tgSt.obsolete | tgSt.docIgnore)]
  [TagAttribute(JSStatus = JSStatus.no, CSControl = true)]
  [XmlType(TypeName = "smart-tag")]
  public partial class smartTag : tag {
    [XmlAttribute, DefaultValue(false)]
    public bool correct;
    [tgAt(tgSt.docIgnore)]
    [XmlAttribute(AttributeName = "default-inline-type"), DefaultValue(0)]
    public inlineControlTypes defaultInlineType;
  }

  [tgAt(tgSt.docIgnore)]
  public enum smartElementTypes {
    no,
    [XmlEnum(Name = "gap-fill")]
    gapFill,
    [XmlEnum(Name = "drop-down")]
    dropDown,
    offering,
    img,
    [XmlEnum(Name = "word-selection")]
    wordSelection,
  }

  [tgAt(tgSt.docIgnore)]
  public enum smartOfferingMode {
    [XmlEnum(Name = "gap-fill")]
    gapFill,
    [XmlEnum(Name = "drop-down-discard")]
    dropDownDiscard,
    [XmlEnum(Name = "drop-down-keep")]
    dropDownKeep,
    [tgAt(0, _oldName = "gap-fill-ignore"), XmlEnum(Name = "gap-fill-passive")]
    gapFillPassive,
  }


  public partial class smartElementLow : macroTemplate {
  }
  //http://daringfireball.net/projects/markdown/
  //https://michelf.ca/projects/php-markdown/extra/
  [tgAt(tgSt.csControl | tgSt.cdata)]
  [XmlType(TypeName = "smart-element")]
  public partial class smartElement : smartElementLow {
    [XmlAttribute(AttributeName = "inline-type")]
    public smartElementTypes inlineType;

  }

  [tgAt(tgSt.csControl | tgSt.cdata)]
  [XmlType(TypeName = "smart-pairing")]
  public partial class smartPairing : smartElementLow {
    [XmlAttribute, tgAt(0), DefaultValue(false)]
    public bool random;

    [XmlAttribute(AttributeName = "left-width"), tgAt(0), DefaultValue(pairingLeftWidth.normal)]
    public pairingLeftWidth leftWidth;
  }

  [tgAt(tgSt.csControl | tgSt.cdata)]
  [XmlType(TypeName = "smart-offering")]
  public partial class smartOffering : smartElementLow {
    [XmlAttribute]
    public string words;
    [XmlAttribute, DefaultValue(0)]
    public smartOfferingMode mode;
  }

  [tgAt(tgSt.csControl | tgSt.xsdHtmlEl | tgSt.docIgnore)]
  [TagAttribute(JSStatus = JSStatus.no, CSControl = true)]
  public partial class node : tag {
  }

  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
  [TagAttribute(JSStatus = JSStatus.no, CSControl = true)]
  public partial class text : tag {
    [XmlAttribute]
    public string title;
  }

  [tgAt(tgSt.docIgnore | tgSt.xsdString)]
  public partial class error : tag {
    [XmlAttribute]
    public string msg;
  }

  [XmlType(TypeName = "header-prop")]
  [tgAt(tgSt.docIgnore | tgSt.xsdHtmlEl | tgSt.xsdNoGlobal)]
  [TagAttribute(JSStatus = JSStatus.no, CSControl = false)]
  public partial class headerProp : tag {
  }

  #endregion

  #region CONTROLS
  //*****************************************
  //              CONTROLS
  //*****************************************
  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
  [TagAttribute(CSControl = true, JSStatus = JSStatus.ctrl, isEval = true)]
  public class evalControl : tag {
    public const string xsdType_eval_group = "eval-group-type";
    public const string xsdType_eval_group_regex = @"^((and)-\w+-(exchangeable)|(and)-\w+|\w+-(exchangeable))$";
    public static Regex evalRegEx = new Regex(xsdType_eval_group_regex);
    /** @summary and-[id] nebo [id]-exchangeable nebo and-[id]-exchangeable.
     * Pro radioButton pouze [id]
     *  @descr ??
    */
    [tgAt(tgSt.metaJS_browse, xsdType = xsdType_eval_group)]
    [XmlAttribute(AttributeName = "eval-group")]
    public string evalGroup;

    [tgAt(tgSt.metaJS_browse), XmlAttribute(AttributeName = "score-weight"), DefaultValue(0)]
    public int scoreWeight;

    [tgAt(tgSt.metaJS_browse, xsdType = "xs:IDREF", _oldName = "eval-btn-id"), XmlAttribute(AttributeName = "eval-button-id")]
    public string evalButtonId;

    public virtual int getMaxScore() { return scoreWeight == 0 ? 100 : scoreWeight; }

    public virtual bool isReadOnly() { return false; }
    public virtual bool isSkipEvaluation() { return false; }
  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl | tgSt.xsdIgnore | tgSt.xsdMixed)]
  [TagAttribute(CSControl = true, JSStatus = JSStatus.ctrl, isEval = false)] //v course.ts, registerControls neni potreba pro Page vytvaret result. Proto isEval = false
  [XmlType(TypeName = "body")]
  public partial class body : tag {

    [XmlIgnore, JsonIgnore, JsonGenOnly]
    [tgAt(tgSt.docIgnore, childPropTypes = "_snd-page")]
    public _sndPage sndPage;

    [XmlIgnore, JsonIgnore, JsonGenOnly]
    [tgAt(tgSt.docIgnore, childPropTypes = "_eval-page")]
    public _evalPage evalPage;

    protected override bool isTagProps(tag t) {
      if (t is _sndPage) sndPage = (_sndPage)t;
      else if (t is _evalPage) evalPage = (_evalPage)t;
      else return false;
      return true;
    }
    public override IEnumerable<tag> getTagProps() { if (sndPage != null) yield return sndPage; if (evalPage != null) yield return evalPage; }

    [XmlAttribute]
    [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
    public string url;

    [XmlAttribute, DefaultValue(0)]
    [tgAt(0)]
    public int order;

    [XmlAttribute(AttributeName = "instr-title")]
    public string instrTitle;

    [XmlAttribute(AttributeName = "instr-body"), tgAt(0, _oldName = "instrs-str")]
    public string instrBody { get { return instrs == null || instrs.Length == 0 ? null : instrs.Aggregate((r, i) => r + "|" + i); } set { instrs = string.IsNullOrEmpty(value) ? null : value.Split('|'); } }

    [XmlAttribute(AttributeName = "see-also-str")]
    [tgAt(tgSt.docIgnore)]
    //[Doc(ignore = true)]
    public string seeAlsoStr {
      get {
        return seeAlso == null ? null : seeAlso.Select(sa => sa.url + "|" + sa.title).Aggregate((r, i) => r + "#" + i);
      }
      set {
        seeAlso = string.IsNullOrEmpty(value) ? null : value.Split('#').Select(l => l.Split('|')).Select(arr => new seeAlsoLink() { url = arr[0], title = arr.Length > 1 ? arr[1] : null }).ToArray();
      }
    }

    [tgAt(tgSt.docIgnore)]
    [XmlAttribute]
    public string externals;

    [XmlAttribute(AttributeName = "see-also-links")]
    [tgAt(0, _oldName = "course-see-also-str")]
    public string seeAlsoLinks;

    [XmlAttribute(AttributeName = "old-ea-is-passive"), DefaultValue(false)]
    [tgAt(tgSt.docIgnore)]
    public bool oldEaIsPassive;

    [XmlAttribute(AttributeName = "is-old-ea"), DefaultValue(false)]
    [tgAt(tgSt.docIgnore)]
    public bool isOldEa;

    [XmlIgnore, JsonIgnore]
    public string[] instrs { get { return _instrs; } set { _instrs = value == null ? null : value.Where(v => v != null).Select(v => v.ToLower()).ToArray(); } } string[] _instrs;

    [XmlIgnore, JsonIgnore]
    [tgAt(tgSt.docIgnore)]
    public seeAlsoLink[] seeAlso;
  }

  public class evalBtnResult : Result {
    public bool Value;
  }

  /** @summary tlacitko pro vyhodnoceni jedne skupiny vyhodnotitelnych elementu.
   *  @descr ??
  */
  [tgAt(tgSt.jsCtrl | tgSt.csControl, _oldName = "eval-btn"), XmlType(TypeName = "eval-button")]
  [TagAttribute(CSControl = true, JSStatus = JSStatus.ctrl, isEval = true)]
  public class evalButton : evalControl {

    [tgAt(0, _oldName = "ratio-score"), XmlAttribute(AttributeName = "score-as-ratio"), DefaultValue(false)]
    public bool scoreAsRatio;
  }

  public enum threeStateBool {
    no,
    [tgAt(0, _oldName = "true")]
    @true,
    [tgAt(0, _oldName = "false")]
    @false
  }

  public partial class checkLow : evalControl {
    [XmlAttribute(AttributeName = "correct-value"), DefaultValue(false)]
    public bool correctValue;

    [tgAt(0, _oldName = "text-id"), XmlAttribute(AttributeName = "text-type"), DefaultValue(0)]
    public CheckItemTexts textType;

    [XmlAttribute(AttributeName = "init-value"), DefaultValue(0)]
    public threeStateBool initValue;

    [XmlAttribute(AttributeName = "read-only"), DefaultValue(false)]
    public bool readOnly;

    [XmlAttribute(AttributeName = "skip-evaluation"), DefaultValue(false)]
    public bool skipEvaluation;

    public override int getMaxScore() { return skipEvaluation ? 0 : base.getMaxScore(); }
    public override bool isReadOnly() { return readOnly; }
    public override bool isSkipEvaluation() { return skipEvaluation; }
  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl)]
  [XmlType(TypeName = "check-box")]
  public partial class checkBox : checkLow {
  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl | tgSt.xsdHtmlEl)]
  [XmlType(TypeName = "check-item")]
  public partial class checkItem : checkLow {
  }

  //[tgAt(tgSt.jsCtrl | tgSt.csControl)]
  //[XmlType]
  //public partial class classification : evalControl {

  //  [XmlAttribute(AttributeName = "correct-value"), DefaultValue(false)]
  //  public bool correctValue;

  //  [XmlAttribute(AttributeName = "text-id"), DefaultValue(0)]
  //  public CheckItemTexts textId;
  //}

  [tgAt(tgSt.metaJS_browse, _oldName = "offering-mode")]
  public enum offeringDropDownMode {
    [tgAt(0, _oldName = "drop-down-discard"), XmlEnum("drop-down-discard")]
    dropDownDiscard,
    [tgAt(0, _oldName = "drop-down-keep"), XmlEnum("drop-down-keep")]
    dropDownKeep,
    /// <summary>
    /// 
    /// </summary>
    /// pokud jsou ve cviceni gap-fill's bez offering-id, tak se nepridaji do offeringu.
    [tgAt(0, _oldName = "gap-fill-ignore"), XmlEnum("gap-fill-ignore")]
    gapFillIgnore,
  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl)]
  [TagAttribute(CSControl = true, JSStatus = JSStatus.ctrl, isEval = false)]
  public partial class offering : tag {
    /// <summary>
    /// 
    /// </summary>
    /// seznam prvku nabidky, oddeleny "|"
    [XmlAttribute]
    public string words;

    /// <summary>
    /// 
    /// </summary>
    /// pro "drop-down" tagy: drop-down-discard" => kazdy prvek nabidky muze byt vybrana pouze jednim drop-down elementem.
    /// drop-down-mode="keep" => jeden prvek nabidky muze pouzit vice drop-down elementu
    [XmlAttribute, tgAt(tgSt.metaJS_browse, _oldName = "drop-down-mode"), DefaultValue(0)]
    public offeringDropDownMode mode;

    /// <summary>
    /// 
    /// </summary>
    /// pro offering s drop-down : offering se nezobrazi.
    [tgAt(tgSt.metaJS_browse, _oldName = "is-hidden"), XmlAttribute, DefaultValue(false)]
    public bool hidden;
  }


  //******************** Single Choice

  [tgAt(tgSt.jsCtrl | tgSt.csControl | tgSt.xsdHtmlEl)]
  [XmlType(TypeName = "radio-button")]
  public partial class radioButton : evalControl {

    [XmlAttribute(AttributeName = "correct-value"), DefaultValue(false)]
    public bool correctValue;

    [XmlAttribute(AttributeName = "init-value"), DefaultValue(false)]
    public bool initValue;

    [XmlAttribute(AttributeName = "read-only"), DefaultValue(false)]
    public bool readOnly;

    [XmlAttribute(AttributeName = "skip-evaluation"), DefaultValue(false)]
    public bool skipEvaluation;

    public override bool isReadOnly() { return readOnly; }
    public override bool isSkipEvaluation() { return skipEvaluation; }
  }

  [tgAt(tgSt.csControl, xsdChildElements = "c0_:['radio-button']")]
  [XmlType(TypeName = "single-choice")]
  [TagAttribute(CSControl = true)]
  public partial class singleChoice : tag {
    [XmlAttribute(AttributeName = "read-only"), DefaultValue(false)]
    public bool readOnly;

    [XmlAttribute(AttributeName = "skip-evaluation"), DefaultValue(false)]
    public bool skipEvaluation;

    [tgAt(0), XmlAttribute(AttributeName = "score-weight")]
    public int scoreWeight;

    [tgAt(0, xsdType = "xs:IDREF", _oldName = "eval-btn-id"), XmlAttribute(AttributeName = "eval-button-id")]
    public string evalButtonId;


  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl), XmlType(TypeName = "word-selection")]
  public partial class wordSelection : evalControl {

    [XmlAttribute]
    public string words;

  }


  public class wordMultiSelectionResult : Result {
    public int[] Values;
  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl), XmlType(TypeName = "word-multi-selection")]
  public partial class wordMultiSelection : evalControl {

    [XmlAttribute]
    public string words;

  }

  public class orderingResult : Result {
    public int[] indexes;
  }
  [tgAt(tgSt.jsCtrl | tgSt.csControl), XmlType(TypeName = "word-ordering")]
  public partial class wordOrdering : evalControl {
    [tgAt(0, _oldName = "correct-value"), XmlAttribute(AttributeName = "correct-order")]
    public string correctOrder; //napr. how#How|are|you
  }
  [tgAt(tgSt.jsCtrl | tgSt.csControl, xsdChildElements = "c0_:['sentence-ordering-item']"), XmlType(TypeName = "sentence-ordering")]
  public partial class sentenceOrdering : evalControl {
  }
  [tgAt(tgSt.jsCtrl | tgSt.csControl | tgSt.xsdHtmlEl, _oldName = "sentence"), XmlType(TypeName = "sentence-ordering-item")]
  public partial class sentenceOrderingItem : tag {
  }

  //******************** EDIT
  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
  public partial class edit : evalControl {
    /// <summary>
    /// Spravana hodnota vyhodnotitelneho elementu.
    /// </summary>
    [XmlAttribute(AttributeName = "correct-value")]
    public string correctValue;

    /// <summary>
    /// vsem elementum se stejnou hodnotou smartWidth se nastavi stejna sirka (rovna maximu z sirky techto elementu)
    /// </summary>
    [tgAt(tgSt.metaJS_browse, xsdType = "xs:NCName", _oldName = "smart-width")]
    [XmlAttribute(AttributeName = "width-group")]
    public string widthGroup;

    [tgAt(tgSt.metaJS_browse), XmlAttribute, DefaultValue(0)]
    public int width;

    /// <summary>
    ///id "offering" elementu, do ktereho se pridaji vsechny spravne hodnoty z correctValue.
    /// </summary>
    /// Pri nastaveni offeringId se zaroven na stejnou hodnotu nastavi i smartWidth (pokud smartWidth jiz neni nastavena na neco jineho)
    [tgAt(tgSt.metaJS_browse, xsdType = "xs:IDREF")]
    [XmlAttribute(AttributeName = "offering-id")]
    public string offeringId;

    [tgAt(tgSt.metaJS_browse), XmlAttribute(AttributeName = "case-sensitive"), DefaultValue(false)]
    public bool caseSensitive;

  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl)]
  [XmlType(TypeName = "gap-fill")]
  public partial class gapFill : edit {
    [tgAt(tgSt.metaJS_browse, _oldName = "place-holder"), XmlAttribute(AttributeName = "hint")]
    public string hint;

    [XmlAttribute(AttributeName = "init-value")]
    public string initValue;

    [tgAt(tgSt.metaJS_browse), XmlAttribute(AttributeName = "read-only"), DefaultValue(false)]
    public bool readOnly;

    [tgAt(tgSt.metaJS_browse), XmlAttribute(AttributeName = "skip-evaluation"), DefaultValue(false)]
    public bool skipEvaluation;

    public override int getMaxScore() { return skipEvaluation ? 0 : base.getMaxScore(); }
    public override bool isReadOnly() { return readOnly; }
    public override bool isSkipEvaluation() { return skipEvaluation; }

  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl)]
  [XmlType(TypeName = "drop-down")]
  public partial class dropDown : edit {

    [tgAt(tgSt.metaJS_browse | tgSt.xsdIgnore | tgSt.docIgnore), XmlAttribute(AttributeName = "gap-fill-like"), DefaultValue(false)]
    public bool gapFillLike;
  }


  //******************** Pairing
  [tgAt(tgSt.jsCtrl | tgSt.csControl, xsdChildElements = "c0_:['pairing-item']")]
  public partial class pairing : evalControl {
    [XmlAttribute(AttributeName = "left-random"), tgAt(0, _oldName = "random"), DefaultValue(false)]
    public bool leftRandom;

    [XmlAttribute(AttributeName = "left-width"), tgAt(0), DefaultValue(pairingLeftWidth.normal)]
    public pairingLeftWidth leftWidth;

    public override int getMaxScore() { return scoreWeight == 0 ? 100 * Items.Length : scoreWeight; }
  }
  public enum pairingLeftWidth {
    normal, //300px
    small, //200px
    xsmall, //140px
    large //400px
  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl | tgSt.xsdHtmlEl)]
  [TagAttribute(CSControl = true, JSStatus = JSStatus.ctrl, isEval = false)]
  [XmlType(TypeName = "pairing-item")]
  public partial class pairingItem : tag {
    [XmlAttribute]
    public string right;

  }

  //******************** Human Eval
  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
  [XmlType(TypeName = "human-eval")]
  public abstract partial class humanEval : evalControl {
    [tgAt(0), XmlAttribute(AttributeName = "is-passive"), DefaultValue(false)]
    public bool isPassive;
    //AngularJS
    //public humanEval() { isPassive = true; }
    public override int getMaxScore() { return isPassive ? 0 : base.getMaxScore(); }
  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl | tgSt.xsdHtmlEl)]
  public partial class writing : humanEval {

    [tgAt(0, _oldName = "recommend-words-min"), XmlAttribute(AttributeName = "limit-recommend"), DefaultValue(0)]
    public int limitRecommend;

    [tgAt(0, _oldName = "words-min"), XmlAttribute(AttributeName = "limit-min"), DefaultValue(0)]
    public int limitMin;

    [tgAt(0, _oldName = "words-max"), XmlAttribute(AttributeName = "limit-max"), DefaultValue(0)]
    public int limitMax;

    [tgAt(0, _oldName = "init-rows"), XmlAttribute(AttributeName = "number-of-rows"), DefaultValue(0)]
    public int numberOfRows;
  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl | tgSt.xsdHtmlEl)]
  [XmlType(TypeName = "recording")]
  public partial class recording : humanEval {

    [tgAt(0, _oldName = "recommend-seconds-from"), XmlAttribute(AttributeName = "limit-recommend"), DefaultValue(0)]
    public int limitRecommend;

    [tgAt(0, _oldName = "speak-seconds-from"), XmlAttribute(AttributeName = "limit-min"), DefaultValue(0)]
    public int limitMin;

    [tgAt(0, _oldName = "speak-seconds-to"), XmlAttribute(AttributeName = "limit-max"), DefaultValue(0)]
    public int limitMax;

    [tgAt(0, _oldName = "stop-in-modal-dialog"), XmlAttribute(AttributeName = "record-in-dialog"), DefaultValue(false)]
    public bool recordInDialog;

    [tgAt(0, xsdType = "xs:IDREF", _oldName = "modal-dialog-header"), XmlAttribute(AttributeName = "dialog-header-id")]
    public string dialogHeaderId;

    [tgAt(0, _oldName = "modal-dialog-size"), XmlAttribute(AttributeName = "dialog-size"), DefaultValue(modalSize.normal)]
    public modalSize dialogSize;

    [tgAt(0, _oldName = "disable-re-record"), XmlAttribute(AttributeName = "single-attempt"), DefaultValue(false)]
    public bool singleAttempt;


  }
  public enum modalSize {
    normal,
    small,
    large
  }


  #endregion

  #region MACROS
  //*****************************************
  //              MACROS
  //*****************************************
  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
  [TagAttribute(CSControl = true, JSStatus = JSStatus.ctrl, isEval = false)]
  public partial class macro : tag {
  }

  [tgAt(tgSt.docIgnore)]
  public enum listIcon {
    number,
    letter,
    upperLetter,
    angleDoubleRight,
    angleRight,
    arrowCircleORight,
    arrowCircleRight,
    arrowRight,
    caretRight,
    caretSquareORight,
    chevronCircleRight,
    chevronRight,
    handORight,
    longArrowRight,
    play,
    playCircle,
    playCircleO,
    circleONotch,
    cog,
    refresh,
    spinner,
    squareO,
    bullseye,
    asterisk,
    circle,
    circleO,
    circleThin,
    dotCircleO
  }

  [tgAt(tgSt.docIgnore)]
  public enum colors {
    black,
    white,
    primary,
    success,
    info,
    warning,
    danger,
  }

  [tgAt(tgSt.csControl, xsdChildElements = "c0_:['li']")]
  public partial class list : macro {

    [XmlAttribute, tgAt(0)]
    public string delim;

    [XmlAttribute(AttributeName = "is-striped"), DefaultValue(false), tgAt(0)]
    public bool isStriped;

    [XmlAttribute, tgAt(0)]
    public listIcon icon;

    [XmlAttribute, tgAt(0)]
    public colors color;
  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl | tgSt.xsdHtmlEl | tgSt.xsdNoMixed)]
  [XmlType(TypeName = "list-group")]
  public partial class listGroup : macro {
    [XmlAttribute(AttributeName = "is-striped"), DefaultValue(false), tgAt(0)]
    public bool isStriped;
  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl | tgSt.xsdHtmlEl)]
  [XmlType(TypeName = "two-column")]
  public partial class twoColumn : macro {
  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl | tgSt.xsdMixed, xsdChildElements = "s:[{c01: ['header-prop']},{c0_: ['@flowContent']}]")]
  public partial class panel : macro {

    protected override bool isTagProps(tag t) { if (t is headerProp) header = (headerProp)t; else return false; return true; }
    public override IEnumerable<tag> getTagProps() { if (header != null) yield return header; }

    [tgAt(tgSt.docIgnore, childPropTypes = "header-prop")]
    [XmlIgnore, JsonIgnore, JsonGenOnly]
    public headerProp header;
  }
  #endregion

  #region EVALS
  //*****************************************
  //              EVALS
  //*****************************************

  //********************** RUNTIME

  [XmlType(TypeName = "_eval-page")]
  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
  public partial class _evalPage : tag { //of _evalBtn
    [XmlAttribute(AttributeName = "max-score")]
    public int maxScore;

    [XmlIgnore, JsonIgnore]
    public Dictionary<string, string[]> radioGroupsObj;

    //skupiny radiobuttonu. V javascript se jimi ridi zaskrtnuti pouze jednoho radio v skupine
    [XmlAttribute(AttributeName = "radio-groups")]
    public string radioGroups {
      get {
        if (radioGroupsObj == null) return null;
        var groups = radioGroupsObj.Select(kv => kv.Key + ":" + kv.Value.DefaultIfEmpty().Aggregate((r, i) => r + "," + i));
        return groups.DefaultIfEmpty().Aggregate((r, i) => r + "|" + i);
      }
      set {
        if (string.IsNullOrEmpty(value)) { radioGroupsObj = null; return; }
        radioGroupsObj = value.Split('|').Select(grp => grp.Split(':')).Select(kv => new { key = kv[0], value = kv[1].Split(',') }).ToDictionary(kv => kv.key, kv => kv.value);
      }
    }
  }

  [XmlType(TypeName = "_eval-btn")]
  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
  public partial class _evalBtn : tag { //of _evalGroup
    [XmlAttribute(AttributeName = "btn-id")]
    public string btnId;
  }

  [XmlType(TypeName = "_eval-group")]
  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
  public partial class _evalGroup : tag {
    [XmlAttribute(AttributeName = "is-and"), DefaultValue(false)]
    public bool isAnd;
    [XmlAttribute(AttributeName = "is-exchangeable"), DefaultValue(false)]
    public bool isExchangeable;
    [tgAt(tgSt.isArray)]
    [XmlAttribute(AttributeName = "eval-control-ids")]
    public string[] evalControlIds;
    [XmlIgnore, JsonIgnore]
    public int maxScore;
  }

  #endregion

  #region MEDIA
  //*****************************************
  //              MEDIA
  //*****************************************

  //********************** RUNTIME
  [XmlType(TypeName = "_snd-page")]
  [tgAt(tgSt.jsCtrl | tgSt.docIgnore | tgSt.xsdIgnore)]
  public partial class _sndPage : tag {
  }

  [XmlType(TypeName = "_snd-file-group")]
  [tgAt(tgSt.jsCtrl | tgSt.docIgnore | tgSt.xsdIgnore)]
  public partial class _sndFileGroup : urlTag {

  }

  [XmlType(TypeName = "_snd-group")]
  [tgAt(tgSt.jsCtrl | tgSt.docIgnore | tgSt.xsdIgnore)]
  public partial class _sndGroup : tag {
    [XmlIgnore, JsonIgnore]
    public List<_sndInterval> intervals = new List<_sndInterval>();

    [XmlIgnore, JsonIgnore]
    public _sndFile sf; //pomocny field pro designtime

  }

  [XmlType(TypeName = "_snd-interval")]
  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
  public partial class _sndInterval : tag {
  }

  [XmlType(TypeName = "_snd-sent")]
  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
  public partial class _sndSent : tag {
    [XmlAttribute, tgAt(0)]
    public int idx;
    [XmlAttribute(AttributeName = "beg-pos"), tgAt(0)]
    public int begPos;
    [XmlAttribute(AttributeName = "end-pos"), tgAt(0)]
    public int endPos;
    [XmlAttribute, tgAt(0)]
    public string text;
    [XmlAttribute, tgAt(0)]
    public string actor;
  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl, xsdChildElements = mediaTag.mediaXsdChilds)]
  [XmlType(TypeName = "media-text")]
  public partial class mediaText : mediaTag {

    [tgAt(0, xsdType = "xs:IDREF", _oldName = "continue-id")]
    [XmlAttribute(AttributeName = "continue-media-id"), JsonIgnore]
    public string continueMediaId; //pointer na mediaTag, definujici pokracovani v hrani

    [tgAt(0, _oldName = "is-passive")]
    [XmlAttribute, DefaultValue(false)]
    public bool passive;

    [tgAt(tgSt.docIgnore)]
    [XmlAttribute(AttributeName = "is-old-to-new"), DefaultValue(false)]
    public bool isOldToNew;

    [tgAt(0, _oldName = "is-hidden")]
    [XmlAttribute, DefaultValue(false)]
    public bool hidden;
  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl | tgSt.xsdIgnore | tgSt.docIgnore)]
  [XmlType(TypeName = "_media-replica")]
  public partial class _mediaReplica : tag {

    [XmlAttribute(AttributeName = "icon-id"), tgAt(0), DefaultValue(0)]
    public IconIds iconId;

    [XmlAttribute(AttributeName = "dlg-left"), tgAt(0)]
    public bool dlgLeft; //pozice zobacku repliky

    [XmlAttribute, tgAt(0)]
    public string actor;
  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl | tgSt.xsdIgnore | tgSt.docIgnore | tgSt.xsdMixed /*nechapu, BT 2179 byla zpusobena timto */)]
  [XmlType(TypeName = "_media-sent")]
  public partial class _mediaSent : tag {
    [XmlAttribute, tgAt(0)]
    public int idx;
  }

  //********************** DESIGN TIME
  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
  public partial class include : tag {
    /** @summary pointer na XML file s sndDialog nebo sndText (extenze se ignoruje). 
     *  @descr 
     */
    [tgAt(tgSt.xsdRequiredAttr)]
    [XmlAttribute(AttributeName = "cut-url")]
    public string cutUrl;
  }

  [XmlType(TypeName = "include-text")]
  [tgAt(tgSt.xsdNoGlobal | tgSt.xsdIgnoreTagAttrs, xsdChildElements = "c0_:['phrase-replace']")]
  public partial class includeText : include {

  }
  [XmlType(TypeName = "include-dialog")]
  [tgAt(tgSt.xsdNoGlobal | tgSt.xsdIgnoreTagAttrs, xsdChildElements = "c0_:['phrase-replace']")]
  public partial class includeDialog : include {

  }

  [XmlType(TypeName = "phrase-replace")]
  [tgAt(tgSt.xsdHtmlEl | tgSt.xsdNoGlobal | tgSt.xsdIgnoreTagAttrs, _oldName = "sent-replace")]
  public partial class phraseReplace : tag {
    public const string sndReplace_sndDialogIdx = "sndReplace_sndDialogIdx";
    public const string sndReplace_sndDialogIdx_regex = @"^\d+\.\d+$";

    [tgAt(0, xsdType = "xs:int", _oldName = "sent-idx")]
    [XmlAttribute(AttributeName = "phrase-idx")]
    public int phraseIdx;

    [tgAt(0, xsdType = sndReplace_sndDialogIdx, _oldName = "replica-sent-idx")]
    [XmlAttribute(AttributeName = "replica-phrase-idx")]
    public string replicaPhraseIdx;
  }

  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
  public partial class _sndFile : urlTag {
    public const string mediaTag_sequence = "sequence-type";
    public const string mediaTag_sequence_regex = @"^(\d+|-\d+|\d+-\d+|\d+-)(,\d+|,-\d+|,\d+-\d+|,\d+-)*$";

    [XmlIgnore, JsonIgnore, JsonGenOnly]
    [tgAt(tgSt.docIgnore, childPropTypes = "include-text|include-dialog")]
    public include file;

    protected override bool isTagProps(tag t) { if (t is include) { file = (include)t; return true; } return false; }
    public override IEnumerable<tag> getTagProps() { if (file != null) yield return file; }

  }

  [tgAt(tgSt.csControl | tgSt.xsdNoGlobal | tgSt.xsdIgnoreTagAttrs, xsdChildElements = "s:[{c01:['include-text']},{c0_:['replica']}]")]
  [XmlType(TypeName = "cut-dialog")]
  public partial class cutDialog : _sndFile { //of sndReplica
  }

  [tgAt(tgSt.csControl | tgSt.xsdNoGlobal | tgSt.xsdIgnoreTagAttrs, xsdChildElements = "c01:[{c01:['include-dialog']},{c0_:['phrase']}]")]
  [XmlType(TypeName = "cut-text")]
  public partial class cutText : _sndFile { //of sndSent
  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl | tgSt.xsdHtmlEl | tgSt.xsdNoGlobal | tgSt.xsdIgnoreTagAttrs, _oldName = "sent")]
  [XmlType(TypeName = "phrase")]
  [TagAttribute(CSControl = true, JSStatus = JSStatus.ctrl, isEval = false)]
  public partial class phrase : tag { //nemuze byt ani z tagu (nemohla by byt JS control) ani z mediaTagu (v JS chybi napr. html Clearing)

    [tgAt(0)]
    [XmlAttribute(AttributeName = "beg-pos"), DefaultValue(0)]
    public int begPos;

    [tgAt(0)]
    [XmlAttribute(AttributeName = "end-pos"), DefaultValue(0)]
    public int endPos;

    [XmlIgnore, JsonIgnore]
    public int idx;
  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl | tgSt.xsdNoGlobal | tgSt.xsdIgnoreTagAttrs, xsdChildElements = "c0_:['phrase']")]
  [XmlType(TypeName = "replica")]
  [TagAttribute(CSControl = true, JSStatus = JSStatus.ctrl)]
  public partial class replica : tag {
    [tgAt(0, _oldName = "icon-id")]
    [XmlAttribute(AttributeName = "actor-id"), DefaultValue(0)]
    public IconIds actorId;

    [tgAt(0, _oldName = "actor")]//tgSt.xsdIgnore | tgSt.obsolete)]
    [XmlAttribute(AttributeName = "actor-name")]
    public string actorName;

    /// <summary>
    /// 
    /// </summary>
    /// uvedena konstrukce slouží k vytvoření dialogu z plain textu. Podporován je POUZE souvislý text (bez přeskakování zvukových vět). 
    /// Takže zápis je ten, že pro každou repliku se určí POČET vět repliky (určovat začátek a konec je zbytečně složité). 
    /// Další replika začíná první větou po poslední větě předchozí repliky. 
    /// jestli preci jenom ale nebude nejlepsi "take-phrases" (puvodne "sent-take").
    [XmlAttribute(AttributeName = "number-of-phrases")]
    [tgAt(0, _oldName = "sent-take")]
    public int numberOfPhrases;

  }

  public partial class urlTag : tag {
    public const string mediaTag_format = "format-type";
    public const string mediaTag_format_regex = @"^.*\.mp3$|^.*@((std-4|std-2)$|(16by9|4by3):((\d+|\*)-((\w|\.)*webm|(\w|\.)*mp4)+(,(\w|\.)*webm|,(\w|\.)*mp4)*)+(\|(\d+|\*)-((\w|\.)*webm|(\w|\.)*mp4)+(,(\w|\.)*webm|,(\w|\.)*mp4)*)*)$";

    [XmlAttribute(AttributeName = "media-url"), JsonIgnore]
    [tgAt(0, xsdType = mediaTag_format)]
    public string mediaUrl;


  }

  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
  [XmlType(TypeName = "media-tag")]
  public partial class mediaTag : urlTag {

    public const string mediaXsdChilds = "c01: ['include-text','include-dialog','cut-text','cut-dialog']";

    [XmlAttribute(AttributeName = "cut-url"), JsonIgnore]
    [tgAt(0)]
    public string cutUrl;

    [XmlAttribute]
    [tgAt(0, xsdType = _sndFile.mediaTag_sequence), JsonIgnore]
    public string subset;

    [XmlAttribute(AttributeName = "share-media-id"), JsonIgnore]
    [tgAt(0, xsdType = "xs:IDREF", _oldName = "share-id")]
    public string shareMediaId; //pointer na mediaTag, jehoz zvukovou sekvenci sdilim 

    [XmlAttribute(AttributeName = "_sent-group-id")]
    [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
    public string _sentGroupId; //vysledek design zpracovani - identifikace seqence


    [XmlIgnore, JsonIgnore]
    [tgAt(tgSt.docIgnore, childPropTypes = "cut-dialog|cut-text|include-text|include-dialog")]
    public tag file;

    protected override bool isTagProps(tag t) { if (t is _sndFile || t is include) { file = t; return true; } return false; }
    public override IEnumerable<tag> getTagProps() { if (file != null) yield return file; }

  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl, xsdChildElements = mediaTag.mediaXsdChilds)]
  [XmlType(TypeName = "media-big-mark")]
  public partial class mediaBigMark : mediaTag {
  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl, xsdChildElements = mediaTag.mediaXsdChilds)]
  [XmlType(TypeName = "media-player")]
  public partial class mediaPlayer : mediaTag {
  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl, xsdChildElements = mediaTag.mediaXsdChilds)]
  [XmlType(TypeName = "media-video")]
  public partial class mediaVideo : mediaTag {
  }


  [tgAt(tgSt.jsCtrl | tgSt.csControl | tgSt.docIgnore)]
  [XmlType(TypeName = "tts-sound")]
  public partial class ttsSound : mediaTag {
    [XmlAttribute]
    public string text;
  }
  #endregion

  #region TEMPLATES

  //*****************************************
  //              Templates
  //*****************************************
  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
  [TagAttribute(JSStatus = JSStatus.no, CSControl = true)]
  [XmlType(TypeName = "macro-template")]
  public partial class macroTemplate : macro {

    [XmlAttribute]
    public string name;

    [XmlAttribute, JsonIgnore, LMComLib.JsonGenOnly]
    public string cdata;

  }

  [tgAt(tgSt.csControl | tgSt.xsdString)]
  [XmlType(TypeName = "inline-tag")]
  public partial class inlineTag : macroTemplate {

    [XmlAttribute(AttributeName = "inline-type"), DefaultValue(0)]
    public inlineElementTypes inlineType;
  }

  [tgAt(tgSt.csControl | tgSt.cdata)]
  [XmlType(TypeName = "macro-true-false")]
  public partial class macroTrueFalse : macroTemplate {
    [XmlAttribute(AttributeName = "text-id"), DefaultValue(0)]
    public CheckItemTexts textId;
  }

  [tgAt(tgSt.csControl | tgSt.cdata)]
  [XmlType(TypeName = "macro-single-choices")]
  public partial class macroSingleChoices : macroTemplate {
  }

  [tgAt(tgSt.csControl | tgSt.cdata)]
  [XmlType(TypeName = "macroPairing")]
  public partial class macroPairing : macroTemplate {
  }

  [tgAt(tgSt.csControl | tgSt.cdata)]
  [XmlType(TypeName = "macro-table")]
  public partial class macroTable : macroTemplate {
    [XmlAttribute(AttributeName = "inline-type"), DefaultValue(0)]
    public inlineControlTypes inlineType;
  }

  [tgAt(tgSt.csControl | tgSt.cdata)]
  [XmlType(TypeName = "macro-list-word-ordering")]
  public partial class macroListWordOrdering : macroTemplate {
  }

  [tgAt(tgSt.csControl | tgSt.cdata)]
  [XmlType(TypeName = "macro-list")]
  public partial class macroList : macroTemplate {
    [XmlAttribute(AttributeName = "inline-type"), DefaultValue(0)]
    public inlineControlTypes inlineType;
  }

  [tgAt(tgSt.csControl | tgSt.cdata)]
  [XmlType(TypeName = "macro-icon-list")]
  public partial class macroIconList : macroTemplate {
    [XmlAttribute, tgAt(0)]
    public string delim;

    [XmlAttribute(AttributeName = "is-striped"), DefaultValue(false), tgAt(0)]
    public bool isStriped;

    [XmlAttribute, tgAt(0)]
    public listIcon icon;

    [XmlAttribute, tgAt(0)]
    public colors color;
  }

  [tgAt(tgSt.csControl | tgSt.cdata)]
  [XmlType(TypeName = "macro-article")]
  public partial class macroArticle : macroTemplate {
  }

  [tgAt(tgSt.csControl | tgSt.cdata)]
  [XmlType(TypeName = "macro-vocabulary")]
  public partial class macroVocabulary : macroTemplate {
  }

  [tgAt(tgSt.csControl | tgSt.cdata)]
  [XmlType(TypeName = "macro-video")]
  public partial class macroVideo : macroTemplate {

    [XmlAttribute(AttributeName = "cut-url")]
    [tgAt(0)]
    public string cutUrl;

    [XmlAttribute(AttributeName = "media-url")]
    [tgAt(0, xsdType = urlTag.mediaTag_format)]
    public string mediaUrl;

    [XmlAttribute(AttributeName = "display-style"), tgAt(0)]
    public string displayStyle;
  }
  #endregion

  #region HTML
  //*****************************************
  //              HTML Tags
  //*****************************************
  //http://www.w3.org/TR/html4/index/elements.html

  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
  [XmlType(TypeName = "html-tag")]
  public partial class htmlTag : tag {
    [XmlAttribute(AttributeName = "tag-name")]
    public string tagName;
    public static htmlTag create(string name, params object[] pars) {
      var res = new htmlTag { tagName = name, Items = pars.OfType<tag>().ToArray() };
      foreach (var par in pars.OfType<IEnumerable<tag>>())
        res.Items = res.Items == null ? par.ToArray() : res.Items.Concat(par).ToArray();
      var cnt = 0;
      while (cnt < pars.Length) {
        if (pars[cnt] is string) {
          switch ((string)pars[cnt]) {
            case "class": res.classSetter = (string)pars[cnt + 1]; break;
            case "id": res.id = (string)pars[cnt + 1]; break;
            case "styleSheet": res.styleSheet = (string)pars[cnt + 1]; break;
            default:
              if (res.attrs == null) res.attrs = new List<attr>();
              res.attrs.Add(new attr { name = (string)pars[cnt], value = (string)pars[cnt + 1] });
              break;
          }
          cnt++;
        }
        cnt++;
      }
      if (res.Items.Length == 0) res.Items = null;
      if (res.attrs != null && res.attrs.Count == 0) res.attrs = null;
      return res;
    }

    [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
    public List<attr> attrs;

    public const string div = "div";
    public const string tr = "tr";
    public const string td = "td";
    public const string table = "table";
    //public const string img = "img";
    public const string ul = "ul";
    public const string li = "li";
    public const string br = "br";
    public const string p = "p";
    public const string h3 = "h3";
    public const string h4 = "h4";
    //public const string script = "script";
    public const string i = "i";
  }

  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore | tgSt.cdata)]
  public partial class script : tag {
    [XmlAttribute, JsonIgnore, LMComLib.JsonGenOnly]
    public string cdata;
  }

  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
  public partial class img : tag {
    [XmlAttribute]
    public string src;
  }

  public partial class tag {
    [XmlElementAttribute("html-tag", typeof(htmlTag))]
    [XmlElementAttribute("img", typeof(img))]
    [XmlElementAttribute("script", typeof(script))]
    //[XmlElementAttribute("a", typeof(a))]
    //[XmlElementAttribute("script", typeof(script))]
    [XmlElementAttribute("header-prop", typeof(headerProp))]

    [XmlElementAttribute("tts-sound", typeof(ttsSound))]
    [XmlElementAttribute("media-tag", typeof(mediaTag))]
    [XmlElementAttribute("media-big-mark", typeof(mediaBigMark))]
    [XmlElementAttribute("media-player", typeof(mediaPlayer))]
    //[XmlElementAttribute("media-title", typeof(mediaTitle))]
    [XmlElementAttribute("media-video", typeof(mediaVideo))]
    //[XmlElementAttribute("media-dialog", typeof(mediaDialog))]
    //[XmlElementAttribute("media-replica", typeof(mediaReplica))]
    [XmlElementAttribute("media-text", typeof(mediaText))]
    [XmlElementAttribute("phrase", typeof(phrase))]
    [XmlElementAttribute("phrase-replace", typeof(phraseReplace))]
    [XmlElementAttribute("replica", typeof(replica))]
    [XmlElementAttribute("include-text", typeof(includeText))]
    [XmlElementAttribute("include-dialog", typeof(includeDialog))]
    //[XmlElementAttribute("snd-page", typeof(sndPage))]
    [XmlElementAttribute("cut-dialog", typeof(cutDialog))]
    [XmlElementAttribute("cut-text", typeof(cutText))]
    [XmlElementAttribute("_media-replica", typeof(_mediaReplica))]
    [XmlElementAttribute("_media-sent", typeof(_mediaSent))]
    [XmlElementAttribute("_snd-page", typeof(_sndPage))]
    [XmlElementAttribute("_snd-file-group", typeof(_sndFileGroup))]
    [XmlElementAttribute("_snd-group", typeof(_sndGroup))]
    [XmlElementAttribute("_snd-interval", typeof(_sndInterval))]
    [XmlElementAttribute("_snd-sent", typeof(_sndSent))]

    [XmlElementAttribute("eval-button", typeof(evalButton))]
    [XmlElementAttribute("_eval-page", typeof(_evalPage))]
    [XmlElementAttribute("_eval-btn", typeof(_evalBtn))]
    [XmlElementAttribute("_eval-group", typeof(_evalGroup))]

    [XmlElementAttribute("text", typeof(text))]
    [XmlElementAttribute("page", typeof(body))]
    //[XmlElementAttribute("eval-group-btn", typeof(evalGroupBtn))]
    [XmlElementAttribute("macro", typeof(macro))]
    [XmlElementAttribute("list", typeof(list))]
    [XmlElementAttribute("list-group", typeof(listGroup))]
    [XmlElementAttribute("two-column", typeof(twoColumn))]
    [XmlElementAttribute("panel", typeof(panel))]
    [XmlElementAttribute("node", typeof(node))]
    [XmlElementAttribute("human-eval", typeof(humanEval))]
    [XmlElementAttribute("gap-fill", typeof(gapFill))]
    [XmlElementAttribute("radio-button", typeof(radioButton))]
    [XmlElementAttribute("pairing-item", typeof(pairingItem))]
    [XmlElementAttribute("pairing", typeof(pairing))]
    [XmlElementAttribute("single-choice", typeof(singleChoice))]
    [XmlElementAttribute("smart-tag", typeof(smartTag))]
    [XmlElementAttribute("smart-element", typeof(smartElement))]
    [XmlElementAttribute("smart-offering", typeof(smartOffering))]
    [XmlElementAttribute("smart-pairing", typeof(smartPairing))]
    [XmlElementAttribute("word-selection", typeof(wordSelection))]
    [XmlElementAttribute("word-multi-selection", typeof(wordMultiSelection))]
    [XmlElementAttribute("word-ordering", typeof(wordOrdering))]
    [XmlElementAttribute("sentence-ordering", typeof(sentenceOrdering))]
    [XmlElementAttribute("sentence-ordering-item", typeof(sentenceOrderingItem))]
    [XmlElementAttribute("writing", typeof(writing))]
    [XmlElementAttribute("recording", typeof(recording))]
    [XmlElementAttribute("check-item", typeof(checkItem))]
    [XmlElementAttribute("check-box", typeof(checkBox))]
    [XmlElementAttribute("extension", typeof(extension))]

    //[XmlElementAttribute("possibilities", typeof(possibilities))]
    //[XmlElementAttribute("drag-source", typeof(dragSource))]
    //[XmlElementAttribute("drag-target", typeof(dragTarget))]


    [XmlElementAttribute("offering", typeof(offering))]
    [XmlElementAttribute("drop-down", typeof(dropDown))]

    [XmlElementAttribute("macro-true-false", typeof(macroTrueFalse))]
    [XmlElementAttribute("macro-single-choices", typeof(macroSingleChoices))]
    [XmlElementAttribute("macro-pairing", typeof(macroPairing))]
    [XmlElementAttribute("macro-table", typeof(macroTable))]
    [XmlElementAttribute("macro-list-word-ordering", typeof(macroListWordOrdering))]
    [XmlElementAttribute("macro-list", typeof(macroList))]
    [XmlElementAttribute("macro-icon-list", typeof(macroIconList))]
    [XmlElementAttribute("macro-article", typeof(macroArticle))]
    [XmlElementAttribute("macro-vocabulary", typeof(macroVocabulary))]
    [XmlElementAttribute("macro-video", typeof(macroVideo))]
    [XmlElementAttribute("inline-tag", typeof(inlineTag))]

    [XmlElementAttribute("doc-type", typeof(docType))]
    [XmlElementAttribute("doc-enum", typeof(docEnum))]
    [XmlElementAttribute("doc-enum-item", typeof(docEnumItem))]
    [XmlElementAttribute("doc-prop", typeof(docProp))]
    //[XmlElementAttribute("summary", typeof(summary))]
    [XmlElementAttribute("doc-example", typeof(docExample))]
    [XmlElementAttribute("doc-descr", typeof(docDescr))]
    //[XmlElementAttribute("remarks", typeof(remarks))]
    [XmlElementAttribute("error", typeof(error))]

    [XmlTextAttribute(typeof(string)), JsonIgnore]
    //[Doc(ignore = true)]
    [tgAt(tgSt.docIgnore)]
    public object[] items { get { return getTagItems(); } set { setTagItems(value); } }

  }
  #endregion

  #region PERSISTENCE
  //*************** items persistence
  public partial class tag {
    [tgAt(tgSt.docIgnore)]
    [XmlIgnore, JsonIgnore, LMComLib.JsonGenOnly]
    //[Doc(ignore = true)]
    public tag[] Items;

    //******************** Serializace x deserializace tagu
    protected virtual void setTagItems(object[] itemsValue) {
      if (itemsValue == null || itemsValue.Length == 0) { Items = null; return; }
      List<tag> its = new List<tag>();
      var attr = tgAtAttribute.fromTag(GetType());
      var st = attr == null ? 0 : attr.st;
      //List<tag> parentProps = null;
      foreach (object it in itemsValue) {
        if (it is string) {
          //mixin content?
          if (this is htmlTag ||
            (st & tgSt.xsdMixed) != 0 ||
            ((st & tgSt.xsdHtmlEl) != 0 && (st & tgSt.xsdNoMixed) == 0) ||
            (st & tgSt.cdata) != 0 ||
            (st & tgSt.xsdString) != 0) {

            var tt = it as string;
            tt = tt.Replace("<", "&lt;").Replace(">", "&gt;");
            its.Add(new text() { title = tt }); //nahrada textu Text tagem
          }
          continue;
        }
        tag t = (tag)it;
        if (!isTagProps(t)) { //tag neni v self property 
          //if (t.parentProp != CourseModel.parentProps.no) {
          //  if (parentProps == null) parentProps = new List<tag>();
          //  parentProps.Add((tag)t);
          //} else
          its.Add(t); //add items tag
        }
      }
      //if (parentProps != null) setTagProps(parentProps);
      Items = its.ToArray();
    }
    protected virtual bool isTagProps(tag t) { return false; }
    protected virtual void setTagProps(List<tag> t) { throw new Exception("setTagProps"); }
  }

  //metadata pro jsonML serializaci
  public partial class jsonMLMeta {
    public string rootTagName;
    public Dictionary<string, jsClassMeta> types;
  }

  public partial class jsClassMeta {
    public tgSt st;
    public string xsdChildElements;
    public string anc;
    public Dictionary<string, jsPropMeta> props;
  }

  public partial class jsPropMeta {
    public tgSt st;
    public string enumType;
    public string childPropTypes; //nazev typu childu, ktery se dosadi do teto property
  }

  public class Result : Score {
    public string tg;
    //public CourseDataFlag flag;
    //public int ms;
    //public int s;
  }

  public interface IAdjustNetDates {
    void adjustNetDates();
  }
  public partial class HumanEvalResult : Result {
    public int hPercent;
    public string hEmail;
    public long hLmcomId;
    public string hLevel;
    public int hDate; //datum vyhodnoceni. bowser.ts, Utils.nowToNum()
  }

  public class PageUser : Result {
    [XmlAttribute, DefaultValue(0)]
    public int i;
    //public Score s;
    public ExerciseStatus st;
    [XmlAttribute, DefaultValue(0)]
    public int bt;
    [XmlAttribute, DefaultValue(0)]
    public int et;
    [XmlAttribute, DefaultValue(0)]
    public int t;
    public Object Results; //pro Tag.Id vysledek kontrolky
  }

  public class PairingResult : Result {
    public int[] Value;
  }

  public class GapFillResult : Result {
    public string Value;
  }

  public class CheckItemResult : Result {
    public bool? Value; //spravna odpoved
  }

  public class SingleChoiceResult : Result {
    public int? Value;
  }

  public class WordSelectionResult : SingleChoiceResult {
  }

  public class WritingResult : HumanEvalResult {
    public string text;
    public int words;
    public int hMin;
    public int hMax;
    public int hRecommendMin;
  }

  public class audioCaptureResult : HumanEvalResult {
    public string audioUrl;
    public int recordedMilisecs;
    public int hRecommendFrom;
    public int hFrom;
    public int hTo;
  }

  public class extensionResult : Result {
    public bool Value;
  }

  [tgAt(tgSt.jsCtrl | tgSt.csControl | tgSt.cdata | tgSt.docIgnore)]
  public partial class extension : evalControl {

    [XmlAttribute]
    public string data;

    [XmlAttribute, JsonIgnore, LMComLib.JsonGenOnly]
    public string cdata;

    public override int getMaxScore() { return 0; }
  }


  public static class getAll {
    public static IEnumerable<Type> Enums() {
      yield return typeof(IconIds);
      yield return typeof(CheckItemTexts);
      yield return typeof(inlineControlTypes);
      yield return typeof(JSStatus);
      yield return typeof(CourseDataFlag);
      yield return typeof(modalSize);
      //yield return typeof(htmlDir);
      //yield return typeof(parentProps);
      yield return typeof(tgSt);
      //yield return typeof(smartTagStyle); 
      yield return typeof(offeringDropDownMode);
      yield return typeof(smartOfferingMode);
      yield return typeof(inlineElementTypes);
      yield return typeof(smartElementTypes);
      yield return typeof(colors);
      yield return typeof(listIcon);
      yield return typeof(pairingLeftWidth);
      yield return typeof(threeStateBool);
      //yield return typeof(mediaSentHidden);
    }

    public static IEnumerable<Type> Types() {

      //yield return typeof(sndFormat);
      //yield return typeof(sndFormatExt);
      //yield return typeof(sndFormatItem);
      //yield return typeof(sndFormatRatio);

      //ancestors
      yield return typeof(tag);
      //yield return typeof(tagStyled);
      //yield return typeof(tagHtml);

      yield return typeof(seeAlsoLink);
      yield return typeof(html);
      yield return typeof(head);
      yield return typeof(evalControl);
      yield return typeof(jsonMLMeta);
      yield return typeof(jsClassMeta);
      yield return typeof(jsPropMeta);

      yield return typeof(htmlTag);
      yield return typeof(attr);
      yield return typeof(script);
      yield return typeof(img);
      //yield return typeof(a);

      //Objects
      yield return typeof(TagStatic);

      yield return typeof(text);
      yield return typeof(body);
      yield return typeof(headerProp);
      yield return typeof(macro);
      yield return typeof(humanEval);
      yield return typeof(Score);
      yield return typeof(ttsSound);

      //yield return typeof(singleChoiceLow);

      //vyhodnoceni
      yield return typeof(evalButton);//obsolete
      //yield return typeof(evalGroupBtn);
      yield return typeof(dropDown);
      yield return typeof(edit);
      yield return typeof(gapFill);
      yield return typeof(radioButton);
      yield return typeof(checkLow);
      yield return typeof(checkItem);
      yield return typeof(checkBox);
      yield return typeof(pairingItem);
      yield return typeof(pairing);
      yield return typeof(singleChoice);
      yield return typeof(wordSelection);
      yield return typeof(wordMultiSelection);
      yield return typeof(wordOrdering);
      yield return typeof(orderingResult);
      yield return typeof(sentenceOrdering);
      yield return typeof(sentenceOrderingItem);
      yield return typeof(extension);

      yield return typeof(writing);
      yield return typeof(recording);

      //bez vyhodnoceni
      yield return typeof(list);
      yield return typeof(listGroup);
      yield return typeof(twoColumn);
      yield return typeof(panel);
      yield return typeof(node);
      yield return typeof(offering);

      //yield return typeof(dragSource);//obsolete
      //yield return typeof(dragTarget);//obsolete
      //yield return typeof(possibilities); //obsolete

      //zvuk
      //yield return typeof(media);
      yield return typeof(urlTag);
      yield return typeof(mediaTag);
      yield return typeof(mediaBigMark);
      yield return typeof(mediaPlayer);
      //yield return typeof(mediaTitle);
      yield return typeof(mediaVideo);
      //yield return typeof(include);
      //yield return typeof(mediaDialog);
      //yield return typeof(mediaReplica);
      yield return typeof(mediaText);
      //yield return typeof(sent);
      //yield return typeof(replica);
      //yield return typeof(includeText);
      //yield return typeof(includeDialog);
      //yield return typeof(sentReplace);
      //yield return typeof(sndPage);
      //yield return typeof(_sndFile);
      //yield return typeof(sndDialog);
      //yield return typeof(sndText);
      yield return typeof(_mediaReplica);
      yield return typeof(_mediaSent);
      yield return typeof(_sndPage);
      yield return typeof(_sndFileGroup);
      yield return typeof(_sndGroup);
      yield return typeof(_sndInterval);
      yield return typeof(_sndSent);

      //kvuli (jen) dokumentaci?
      yield return typeof(_sndFile);
      yield return typeof(cutDialog);
      yield return typeof(cutText);
      yield return typeof(phrase);
      yield return typeof(replica);
      yield return typeof(include);
      yield return typeof(includeText);
      yield return typeof(includeDialog);
      yield return typeof(phraseReplace);

      yield return typeof(_evalPage);
      yield return typeof(_evalBtn);
      yield return typeof(_evalGroup);
      //yield return typeof(PassiveDialog);

      //macro Templates. V JS nejsou viditelna
      yield return typeof(macroTemplate);
      yield return typeof(macroTrueFalse);
      yield return typeof(macroSingleChoices);
      yield return typeof(macroPairing);
      yield return typeof(macroTable);
      yield return typeof(macroListWordOrdering);
      yield return typeof(macroList);
      yield return typeof(macroIconList);
      yield return typeof(macroArticle);
      yield return typeof(macroVocabulary);
      yield return typeof(macroVideo);
      yield return typeof(inlineTag);

      yield return typeof(smartTag);
      yield return typeof(smartElementLow);
      yield return typeof(smartElement);
      yield return typeof(smartOffering);
      yield return typeof(smartPairing);

      yield return typeof(docTagsMeta);
      yield return typeof(docNamed);
      yield return typeof(docType);
      yield return typeof(docEnum);
      yield return typeof(docEnumItem);
      yield return typeof(docProp);
      yield return typeof(docDescr);
      //yield return typeof(summary);
      yield return typeof(docExample);
      //yield return typeof(remarks);

      //results
      yield return typeof(Result);
      yield return typeof(PageUser);
      yield return typeof(PairingResult);
      yield return typeof(SingleChoiceResult);
      yield return typeof(WordSelectionResult);
      yield return typeof(audioCaptureResult);
      yield return typeof(WritingResult);
      yield return typeof(GapFillResult);
      yield return typeof(HumanEvalResult);
      yield return typeof(CheckItemResult);
      yield return typeof(evalBtnResult);
      yield return typeof(wordMultiSelectionResult);
      yield return typeof(extensionResult);

    }
  }

  #endregion
#pragma warning restore 1591

}

