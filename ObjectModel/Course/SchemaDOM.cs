using LMComLib;
using LMNetLib;
using Newtonsoft.Json;
using CourseMeta;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.Xsl;
using xml = System.Xml;
using System.Reflection;
using System.Xml.Schema;

namespace CourseModel {

  //public partial class singleChoiceLow {

  //  public singleChoiceLow() { }
  //  public singleChoiceLow(object[] itemsValue) : base() { setTagItems(itemsValue); }
  //}
  public partial class smartElement {

    /************************** SMART PARSING library **************************************/
    public static IEnumerable<tag> replaceMacroHardReturn(string str, LoggerMemory wr, inlineElementTypes type = inlineElementTypes.no) {
      if (string.IsNullOrEmpty(str)) yield return new text() { title = str };
      var parts = str.Split(new string[] { "~~" }, StringSplitOptions.None);
      if (parts.Length == 1) foreach (var p in replaceMacroSoftReturn(parts[0], wr, type)) yield return p;
      else foreach (var p in parts) yield return htmlTag.create(htmlTag.p, replaceMacroSoftReturn(p, wr, type));
    }

    //*** ze zacatku radku odstrani "   12. " nebo "a1) " nebo " bE. "
    public static string removeNums(string line) { return removeNumbers.Replace(line, "").Trim(); }
    static Regex removeNumbers = new Regex(@"^\s*(\w|\d{1,2})(\)|\.\s+)", RegexOptions.Singleline);

    //*** trim dle mezery, tabulatoru a carky
    public static string trimEx(string line) { return line.Trim(new char[] { ' ', '\t', ',' }); }

    //*** trim dle mezery, crlf a tabulatoru
    public static string trim(string txt) { return txt.Trim(new char[] { ' ', '\t', '\n', '\r' }); }

    //*** split dle |. Pokud se nenalezne | tak se pro isWordSelection provadi split dle "  12) " nebo "d) " nebo "ab)"
    public static string[] splitEx(string val, bool isWordSelection = true) {
      var parts = val.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(s => trimEx(s)).ToArray();
      if (isWordSelection && parts.Length == 1) {
        parts = regExItem.Parse(val, splitNumbers).Where(r => !r.IsMatch).Select(r => trimEx(r.Value)).ToArray();
      }
      return parts;
    }
    static Regex splitNumbers = new Regex(@"\s*(\w|\d{1,2})\)\s+", RegexOptions.Singleline);

    //*** split dle cr x lf. Difotne odstrani prazdne radky.
    public static string[] splitLines(string val, bool removeEmptyLines = true) {
      return splitCrLf.Split(val).Select(s => trimEx(s)).Where(l => removeEmptyLines ? !string.IsNullOrEmpty(l) : true).ToArray();
    }
    static Regex splitCrLf = new Regex(@"\r\n|\n|\r");

    static IEnumerable<tag> replaceMacroSoftReturn(string str, LoggerMemory wr, inlineElementTypes type) {
      if (string.IsNullOrEmpty(str)) yield return new text() { title = str };
      var parts = str.Split(new string[] { "~" }, StringSplitOptions.None);
      bool firstItem = true;
      //foreach (var tg in replaceInlineControl(parts[0], wr, type)) yield return tg;
      foreach (var t in parts) {
        if (firstItem) firstItem = false; else yield return htmlTag.create(htmlTag.br);
        foreach (var tg in replaceInlineControl(t, wr, type)) yield return tg;
      }
    }


    //public static tag replaceInlineControl(string str, inlineElementTypes type) {
    //  inlineFld fld = new inlineFld(str, type, s => splitEx(s, type == inlineElementTypes.wordSelection));
    //  //Func<IEnumerable<string>, string> agr = lst => fld.Values.DefaultIfEmpty().Aggregate((r, i) => r + "|" + i);
    //  switch (fld.Name) {
    //    case inlineElementTypes.gapFill: return gapFill.createFromField(fld);
    //    case inlineElementTypes.gapFillCorrection: return gapFill.createFromField_Correction(fld);
    //    case inlineElementTypes.dropDown: return dropDown.createFromField(fld);
    //    case inlineElementTypes.wordSelection: return wordSelection.createFromField(fld);
    //    case inlineElementTypes.img: return img.createFromField(fld);
    //    case inlineElementTypes.ttsSound:
    //      //*************** 9.9.2014 - odstraneno kvuli Grafia release
    //      //yield return TtsSound.createFromField(fld); break; 
    //      return new text() { title = fld.Values.Length > 0 ? fld.Values[0] : null };
    //    case inlineElementTypes.no: throw new NotImplementedException("NO");
    //    default: throw new NotImplementedException();
    //  }
    //}

    static IEnumerable<tag> replaceInlineControl(string str, LoggerMemory wr, inlineElementTypes type) {
      if (string.IsNullOrEmpty(str)) yield break;
      foreach (var ri in regExItem.Parse(str, regExtractInlineControls)) {
        if (!ri.IsMatch) { yield return new text() { title = ri.Value }; continue; } //not match => text
        inlineFld fld = new inlineFld(ri.Value.Substring(1, ri.Value.Length - 2), type, s => splitEx(s, type == inlineElementTypes.wordSelection));
        //Func<IEnumerable<string>, string> agr = lst => fld.Values.DefaultIfEmpty().Aggregate((r, i) => r + "|" + i);
        switch (fld.Name) {
          case inlineElementTypes.gapFill: yield return gapFill.createFromField(fld); break;
          case inlineElementTypes.gapFillCorrection: yield return gapFill.createFromField_Correction(fld); break;
          case inlineElementTypes.dropDown: yield return dragTarget.createFromField(fld); break;
          case inlineElementTypes.wordSelection: yield return wordSelection.createFromField(fld); break;
          case inlineElementTypes.img: yield return img.createFromField(fld); break;
          case inlineElementTypes.ttsSound:
            //*************** 9.9.2014 - odstraneno kvuli Grafia release
            //yield return TtsSound.createFromField(fld); break; 
            yield return new text() { title = fld.Values.Length > 0 ? fld.Values[0] : null }; break;
          case inlineElementTypes.no: throw new NotImplementedException("NO");
          default: throw new NotImplementedException();
        }
      }
    }
    static Regex regExtractInlineControls = new Regex(@"{.*?}", RegexOptions.Singleline); //parse stringu na {} avorky
  }

  //k vyhozeni
  //public enum smartTagStyle {
  //  no, //smart tag je primo v kontrolce
  //  text, //smart tag je pridan z textu
  //  tag //smartTagem s correct=false je obalen tag
  //}

  public partial class smartTag : tag {

    //public smartTag() { }

    //public smartTag(tag tag) { Items = new tag[] { tag }; createStyle = smartTagStyle.tag; }

    //public smartTag(string str, bool correctAble = false, LoggerObj wr = null, inlineControlTypes type = inlineControlTypes.no) {
    //  string trimStr = trim(str); correct = !string.IsNullOrEmpty(trimStr) && trimStr[0] == '#';
    //  Items = replaceMacroHardReturn(correct ? trimStr.Substring(1) : trimStr, wr, type).ToArray(); smartText = str; createStyle = smartTagStyle.text;
    //}

    [XmlIgnore, JsonIgnore]
    public string smartText;
    //[XmlIgnore, JsonIgnore]
    //public smartTagStyle createStyle;

    protected override void setTagItems(object[] itemsValue) {
      if (itemsValue == null || itemsValue.Length != 1 || !(itemsValue[0] is string)) base.setTagItems(itemsValue);
      else Items = replaceMacroHardReturn((string)itemsValue[0], null, defaultInlineType).ToArray();
    }

    //protected override object[] getTagItems() {
    //  switch (createStyle) {
    //    case smartTagStyle.no: return base.getTagItems();
    //    case smartTagStyle.tag: return new object[] { Items[0] };
    //    case smartTagStyle.text: return new object[] { smartText };
    //    default: throw new NotImplementedException();
    //  }
    //}

    /************************** SMART PARSING library **************************************/
    public static IEnumerable<tag> replaceMacroHardReturn(string str, LoggerMemory wr, inlineControlTypes type = inlineControlTypes.no) {
      if (string.IsNullOrEmpty(str)) yield return new text() { title = str };
      var parts = str.Split(new string[] { "~~" }, StringSplitOptions.None);
      if (parts.Length == 1) foreach (var p in replaceMacroSoftReturn(parts[0], wr, type)) yield return p;
      else foreach (var p in parts) yield return htmlTag.create(htmlTag.p, replaceMacroSoftReturn(p, wr, type));
    }

    //*** ze zacatku radku odstrani "   12. " nebo "a1) " nebo " bE. "
    public static string removeNums(string line) { return removeNumbers.Replace(line, "").Trim(); }
    static Regex removeNumbers = new Regex(@"^\s*(\w|\d{1,2})(\)|\.\s+)", RegexOptions.Singleline);

    //*** trim dle mezery, tabulatoru a carky
    public static string trimEx(string line) { return line.Trim(new char[] { ' ', '\t', ',' }); }

    //*** trim dle mezery, crlf a tabulatoru
    public static string trim(string txt) { return txt.Trim(new char[] { ' ', '\t', '\n', '\r' }); }

    //*** split dle |. Pokud se nenalezne | tak se pro isWordSelection provadi split dle "  12) " nebo "d) " nebo "ab)"
    public static string[] splitEx(string val, bool isWordSelection = true) {
      //var parts = val.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(s => trimEx(s)).ToArray();
      var parts = val.Split(new char[] { '|' }, StringSplitOptions.None).Select(s => trimEx(s)).ToArray();
      if (isWordSelection && parts.Length == 1) {
        parts = regExItem.Parse(val, splitNumbers).Where(r => !r.IsMatch).Select(r => trimEx(r.Value)).ToArray();
      }
      return parts;
    }
    static Regex splitNumbers = new Regex(@"\s*(\w|\d{1,2})\)\s+", RegexOptions.Singleline);

    //*** split dle cr x lf. Difotne odstrani prazdne radky.
    public static string[] splitLines(string val, bool removeEmptyLines = true) {
      return splitCrLf.Split(val).Select(s => trimEx(s)).Where(l => removeEmptyLines ? !string.IsNullOrEmpty(l) : true).ToArray();
    }
    static Regex splitCrLf = new Regex(@"\r\n|\n|\r");

    static IEnumerable<tag> replaceMacroSoftReturn(string str, LoggerMemory wr, inlineControlTypes type) {
      if (string.IsNullOrEmpty(str)) yield return new text() { title = str };
      var parts = str.Split(new string[] { "~" }, StringSplitOptions.None);
      bool firstItem = true;
      //foreach (var tg in replaceInlineControl(parts[0], wr, type)) yield return tg;
      foreach (var t in parts) {
        if (firstItem) firstItem = false; else yield return htmlTag.create(htmlTag.br);
        foreach (var tg in replaceInlineControl(t, wr, type)) yield return tg;
      }
    }

    static IEnumerable<tag> replaceInlineControl(string str, LoggerMemory wr, inlineControlTypes type) {
      if (string.IsNullOrEmpty(str)) yield break;
      foreach (var ri in regExItem.Parse(str, regExtractInlineControls)) {
        if (!ri.IsMatch) { foreach (var tg in formatedText(ri.Value)) yield return tg; continue; } //not match => text
        inlineField fld = new inlineField(ri.Value.Substring(1, ri.Value.Length - 2), type, s => splitEx(s, type == inlineControlTypes.WordSelection));
        //Func<IEnumerable<string>, string> agr = lst => fld.Values.DefaultIfEmpty().Aggregate((r, i) => r + "|" + i);
        switch (type) {
          case inlineControlTypes.GapFill: yield return gapFill.createFromField(fld); break;
          case inlineControlTypes.GapFill_Correction: yield return gapFill.createFromField_Correction(fld); break;
          case inlineControlTypes.DragTarget: yield return dragTarget.createFromField(fld); break;
          case inlineControlTypes.WordSelection: yield return wordSelection.createFromField(fld); break;
          //case inlineControlTypes.img: yield return img.createFromField(fld); break;
          case inlineControlTypes.TtsSound:
            //*************** 9.9.2014 - odstraneno kvuli Grafia release
            //yield return TtsSound.createFromField(fld); break; 
            yield return new text() { title = fld.Values.Length > 0 ? fld.Values[0] : null }; break;
          case inlineControlTypes.no: throw new NotImplementedException("NO");
          default: throw new NotImplementedException();
        }
      }
    }
    static Regex regExtractInlineControls = new Regex(@"{.*?}", RegexOptions.Singleline); //parse stringu na {} zavorky

    public static IEnumerable<tag> formatedText(string txt) {
      //empty string or not HTML string
      if (string.IsNullOrEmpty(txt)) { yield return new text() { title = txt }; yield break; }
      var isOpen = false; var isHtml = false;
      foreach (var ch in txt) { if (ch == '<' && !isOpen) isOpen = true; else if (ch == '>' && isOpen) { isHtml = true; break; } }
      if (!isHtml) { yield return new text() { title = txt }; yield break; }
      //html string:
      XElement xml = XElement.Parse("<div>" + txt + "</div>");
      //try { xml = XElement.Parse("<div>" + txt + "</div>"); } catch { xml = null; }
      if (xml == null) { yield return new text() { title = txt }; yield break; }
      foreach (var tg in CourseModel.tag.FromElementNoCopy(xml).Items) yield return tg;
    }

  }


  public partial class pairingItem {
    public override void checkAndFinish(body pg, LoggerMemory wr) {
      base.checkAndFinish(pg, wr);
      if (string.IsNullOrEmpty(right)) { wr.ErrorLine(pg.url, " Missing Right PairingItem attribute"); return; }
      if (!allItems().Any()) { wr.ErrorLine(pg.url, " Empty Items"); return; }
    }
    public override void modifyTexts(Func<string, string> modify) {
      if (!string.IsNullOrEmpty(right)) right = modify(right);
    }
  }

  public partial class docExample {
    public override void checkAndFinish(body pg, LoggerMemory wr) {
      base.checkAndFinish(pg, wr);
      var evals = scan().OfType<evalControl>().ToArray(); //.Where(c => CourseModel.Lib.courseModelJsonMLMeta.allEvalControls.Contains(c.GetType().Name)).ToArray();
      if (evals.Length == 0 || evals.Any(e => e is evalButton)) return;
      var cnt = "ev_" + docEvalCnt++.ToString();
      evalBtn = new evalButton { id = cnt, scoreAsRatio = true };
      foreach (var eval in evals) eval.evalButtonId = cnt;
    }
    static int docEvalCnt = 0;

  }

  //public partial class docExample : macroTemplate {
  //  public override IEnumerable<tag> Generate(LoggerObj wr) {
  //    //vytahni XML zdroj do codeListing
  //    var xml = new XElement("node", Items.Where(it => it.parentProp == parentProps.no));
  //    codeListing = System.Security.SecurityElement.Escape(xml.ToString());
  //    return base.Generate(wr);
  //  }
  //}

  public interface IMacroTemplate {
    IEnumerable<tag> Generate(body pg, LoggerMemory wr);
  }

  public partial class macroTemplate : IMacroTemplate {
    public virtual IEnumerable<tag> Generate(body pg, LoggerMemory wr) { yield return this; }
  }

  public class inlineFld {
    static inlineFld() {
      //napr. "   WordSelection par1   = a s a s , par2=p2" je WordSelection s par1="a s a s ", par2="p2"
      //nameNameValue = new Regex(
      //  @"^\s*(?<name>(" +
      //  LowUtils.EnumGetValues<inlineElementTypes>().Where(c => c != inlineElementTypes.no).Select(c => c.ToString()).Aggregate((r, i) => r + "|" + i) +
      //  @"))?(\s*(?<id>\w+)\s*=(?<value>\s*[^,]*)(,|\Z))*",
      //  RegexOptions.Singleline);

      nameNameValue = new Regex(
        @"^\s*((?<name>(" +
        LowUtils.EnumGetValues<inlineElementTypes>().Where(c => c != inlineElementTypes.no).Select(c => c.ToString()).Aggregate((r, i) => r + "|" + i) +
        //@")):)?((?<value>.*?[^\|]*)(\||\Z))*",
        @")):)?(?<value>.*)",
        RegexOptions.Singleline);
    }
    public class prop {
      public string Name;
      public string Value;
    }
    public inlineFld(string fldText, inlineElementTypes type = inlineElementTypes.no, Func<string, string[]> split = null) {
      Text = fldText;
      if (fldText == null) return;
      if (fldText == string.Empty) { Values = new string[] { "" }; return; }
      //string[] parts = split == null ? fldText.Split('|') : split(fldText);
      var match = nameNameValue.Match(fldText);
      var nameStr = match.Groups["name"].Captures.Cast<Capture>().Select(c => c.Value).SingleOrDefault();
      //Props = match.Groups["id"].Captures.Cast<Capture>().Zip(match.Groups["value"].Captures.Cast<Capture>(), (i, v) => new prop() { Name = i.Value, Value = v.Value }).ToArray();
      var vals = match.Groups["value"].Captures.OfType<Capture>().ToArray();
      Values = vals.Length == 1 ? vals[0].Value.Split('|') : null;// match.Groups["value"].Captures.OfType<Capture>().Select(c => c.Value).SingleOrDefault().Split('|');
                                                                  //Values = nameStr != null || Props.Length > 0 ? parts.Skip(1).ToArray() : parts;
      Name = nameStr == null ? type : LowUtils.EnumParse<inlineElementTypes>(nameStr);
    }
    public string Text;
    public inlineElementTypes Name;
    //public prop[] Props;
    public string[] Values;
    static Regex nameNameValue;

    //public string findProp(string name, string defaultVal = null) {
    //  return Props.Where(p => p.Name == name).Select(p => p.Value).FirstOrDefault() ?? defaultVal;
    //}

    public string inlineFieldValues(int skip = 0) {
      if (Values.Length <= skip) return null;
      return Values.Skip(skip).Aggregate((r, i) => r + "|" + i.Trim());
    }
  }

  //OBSOLETE
  public class inlineField {
    static inlineField() {
      //napr. "   WordSelection par1   = a s a s , par2=p2" je WordSelection s par1="a s a s ", par2="p2"
      nameNameValue = new Regex(
        @"^\s*(?<name>(" +
        LowUtils.EnumGetValues<inlineControlTypes>().Where(c => c != inlineControlTypes.no).Select(c => c.ToString()).Aggregate((r, i) => r + "|" + i) +
        @"))?(\s*(?<id>\w+)\s*=(?<value>\s*[^,]*)(,|\Z))*",
        RegexOptions.Singleline);
    }
    public class prop {
      public string Name;
      public string Value;
    }
    public inlineField(string fldText, inlineControlTypes type = inlineControlTypes.no, Func<string, string[]> split = null) {
      Text = fldText;
      //if (string.IsNullOrEmpty(fldText)) return;
      if (fldText == null) return;
      if (fldText == string.Empty) { Values = new string[] { "" }; return; }
      string[] parts = split == null ? fldText.Split('|') : split(fldText);
      var match = nameNameValue.Match(parts[0]);
      var nameStr = match.Groups["name"].Captures.Cast<Capture>().Select(c => c.Value).SingleOrDefault();
      Props = match.Groups["id"].Captures.Cast<Capture>().Zip(match.Groups["value"].Captures.Cast<Capture>(), (i, v) => new prop() { Name = i.Value, Value = v.Value }).ToArray();
      Values = nameStr != null || Props.Length > 0 ? parts.Skip(1).ToArray() : parts;
      Name = nameStr == null ? type : LowUtils.EnumParse<inlineControlTypes>(nameStr);
    }
    public string Text;
    public inlineControlTypes Name;
    public prop[] Props;
    public string[] Values;
    static Regex nameNameValue;

    public string findProp(string name, string defaultVal = null) {
      return Props.Where(p => p.Name == name).Select(p => p.Value).FirstOrDefault() ?? defaultVal;
    }
    public string inlineFieldValues(int skip = 0) {
      if (Values.Length <= skip) return null;
      return Values.Skip(skip).Aggregate((r, i) => r + "|" + i.Trim());
    }
  }

  public class jsCourseMeta : jsonMLMeta {
    public jsCourseMeta(Type root, bool isCammelCase)
      : base(root, isCammelCase) {
      allCSControls = new HashSet<string>(types.Values.Where(t => (t.st & tgSt.csControl) != 0).Select(t => t.tagName));
      allCSAttrs = new HashSet<string>(allCSControls.SelectMany(n => types[n].allProps.Keys.Select(k => k)));
      //allCssTagIds = new HashSet<string>(allCSControls.SelectMany(n => types[n].allProps.Where(p => (p.Value.st & tgSt.cssTagIds) != 0).Select(p => n + "." + p.Value.tagName)));
      allEvalControls = new HashSet<string>(types.Values.Where(t => (t.st & tgSt.isEval) != 0).Select(t => t.tagName));
    }
    public bool? isLMProp(string propName) { //null => both, true => lm only, false => html only
      return allCSAttrs.Contains(propName);
    }
    //pomocna data
    [JsonIgnore]
    public HashSet<string> allCSControls; //vsechny kontrolky, ktere jsou v procesu LM CSS
    [JsonIgnore]
    public HashSet<string> allCSAttrs; //vsechny atributy kontrolek, ktere jsou v procesu LM CSS
    [JsonIgnore]
    public HashSet<string> allEvalControls; //napr. seznam vsech cssTagIds atributu, napr. possibilities.words
  }

  //metadata pro jsonML serializaci
  public partial class jsonMLMeta {
    public jsonMLMeta(Type root, bool isCammelCase) {
      this.root = root; this.isCammelCase = isCammelCase;
      try {
        types = root.Assembly.GetTypes().Where(t => root.IsAssignableFrom(t)).Select(t => new jsClassMeta(t, this)).ToDictionary(t => t.tagName);
      } catch (ReflectionTypeLoadException ex) {
        StringBuilder sb = new StringBuilder();
        foreach (Exception exSub in ex.LoaderExceptions) {
          sb.AppendLine(exSub.Message);
          FileNotFoundException exFileNotFound = exSub as FileNotFoundException;
          if (exFileNotFound != null) {
            if (!string.IsNullOrEmpty(exFileNotFound.FusionLog)) {
              sb.AppendLine("Fusion Log:");
              sb.AppendLine(exFileNotFound.FusionLog);
            }
          }
          sb.AppendLine();
        }
        string errorMessage = sb.ToString();
        throw new Exception(errorMessage);
      }
      rootTagName = root.Name;
      foreach (var tp in types.Values)
        try {
          tp.allProps = tp.parents(true).SelectMany(p => p.props.Values).ToDictionary(p => p.tagName);
        } catch (Exception exp) {
          throw new Exception("jsonMLMeta constructor: type=" + tp.name, exp);
        }
      valueTypeProps = new HashSet<string>(types.Values.SelectMany(t => t.allProps.Values.Where(p => (p.st & tgSt.noJSONQuote) != 0).Select(p => t.name + "." + p.name)));
    }
    public string toCammelCase(string str) { return isCammelCase ? LowUtils.toCammelCase(str) : str; }
    public string fromCammelCase(string str) { return isCammelCase ? LowUtils.fromCammelCase(str) : str; }
    [JsonIgnore]
    public Type root;
    [JsonIgnore]
    public bool isCammelCase;
    [JsonIgnore]
    public HashSet<string> valueTypeProps = new HashSet<string>();
    [JsonIgnore]
    public HashSet<string> ignoredProps = new HashSet<string>();
  }

  public partial class jsClassMeta {
    public jsClassMeta(Type tp, jsonMLMeta meta) {
      this.meta = meta; this.tp = tp;
      var attr = tgAtAttribute.fromTag(tp);
      st = attr == null ? 0 : attr.st;
      xsdChildElements = attr == null ? null : attr.xsdChildElements;
      if (typeof(evalControl).IsAssignableFrom(tp)) st |= tgSt.isEval;
      tagName = meta.fromCammelCase(name);
      //foreach (var pr in meta.parentsAndSelf(meta.root, tp).
      //  SelectMany(t =>
      var tempProps = tp.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Select(f => new jsPropMeta(f, f.FieldType, this)).
            Concat(tp.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Select(f => new jsPropMeta(f, f.PropertyType, this))).
            Where(p => /*(p.st & tgSt.xmlIgnore) == 0 &&*/ p.name != "items").
            ToArray();
      var propNames = tempProps.Select(p => p.tagName).ToArray();
      if (propNames.Distinct().Count() < tempProps.Length)
        throw new Exception();
      props = tempProps./*Where(p => (p.st & tgSt.jsonIgnore) == 0).*/ToDictionary(p => p.tagName);
      anc = tp == meta.root ? null : meta.fromCammelCase(tp.BaseType.Name);
    }
    [JsonIgnore]
    public string name { get { return tp.Name; } }
    [JsonIgnore]
    public string tagName; //fromCammelCase(name)
    [JsonIgnore]
    public jsonMLMeta meta;
    [JsonIgnore]
    public Type tp;

    [JsonIgnore]
    public Dictionary<string, jsPropMeta> allProps;
    //public IEnumerable<jsPropMeta> allProps() {
    //  return parents(true).SelectMany(p => p.props.Values);
    //}
    public IEnumerable<jsClassMeta> parents(bool incSelf) {
      var act = this;
      while (true) {
        if (incSelf) yield return act; else incSelf = true;
        if (act.tp == meta.root) yield break;
        act = meta.types[act.anc];
      }
    }
  }

  public partial class jsPropMeta {
    public jsPropMeta(MemberInfo member, Type type, jsClassMeta owner) {
      this.owner = owner;
      propType = type;
      name = member.Name;
      tagName = owner.meta.fromCammelCase(name);
      attr = tgAtAttribute.fromTag(member);
      st = attr == null ? 0 : attr.st;
      childPropTypes = attr == null ? null : attr.childPropTypes;
      //if (!string.IsNullOrEmpty(childPropTypes)) st |= tgSt.inItems;
      if (type.IsValueType && !type.IsEnum) st |= tgSt.noJSONQuote;
      if (member.GetCustomAttributes(typeof(JsonIgnoreAttribute), true).Any() && !member.GetCustomAttributes(typeof(JsonGenOnlyAttribute), true).Any()) st |= tgSt.jsonIgnore;
      if (member.GetCustomAttributes(typeof(XmlIgnoreAttribute), true).Any()) st |= tgSt.xmlIgnore;
      enumType = type.IsEnum ? "###" + type.FullName + "###" : null;
    }
    [JsonIgnore]
    public string name;
    [JsonIgnore]
    public string tagName; //fromCammelCase(name)
    [JsonIgnore]
    public Type propType;
    [JsonIgnore]
    public jsClassMeta owner;
    [JsonIgnore]
    public tgAtAttribute attr;
  }

  public static class Lib {

    public static jsCourseMeta courseModelJsonMLMeta = new jsCourseMeta(typeof(tag), true);
    public static jsonMLMeta courseMetaJsonMLMeta = new jsonMLMeta(typeof(CourseMeta.data), false);
    //Prevede Tidy-ed XML to Page XML
    public static XElement TidyXmlToPage(XElement el) {
      var body = el.Element("body");
      foreach (var at in body.Descendants().SelectMany(d => d.Attributes()).Where(a => (a.Name.LocalName == "colspan" || a.Name.LocalName == "rowspan") && a.Value == "0")) at.Remove();
      var pageXml = new XElement("Page", body.Elements(), body.Attributes().Where(a => a.Name.LocalName != "id"));
      var page = tag.FromElement<body>(pageXml);
      page.title = el.Element("head").Element("title").Value;
      return page.ToElement();
    }

    public struct scanResult {
      public tag tag;
      public TagStatic tagStatic;
      public tag parent;
      public int parentIdx;
    }

    public static IEnumerable<scanResult> scanControlsEx(tag tg, tag parent = null, int parentIdx = -1) {
      TagStatic ts = TagStatic.tagInfo[tg.GetType().Name];
      if (ts.CSControl) yield return new scanResult() { tag = tg, tagStatic = ts, parent = parent, parentIdx = parentIdx };
      //TagStatic ts;
      //if (TagStatic.tagInfo.TryGetValue(tg.GetType().Name, out ts)) yield return new scanResult() { tag = tg, tagStatic = ts, parent = parent, parentIdx = parentIdx };
      foreach (var t in tg.allItems().SelectMany((t, idx) => scanControlsEx(t, tg, idx))) yield return t;
    }

    const bool dumpXml = true;

    public static void PostProcessPage(ref body pg, CourseMeta.ex ex, LoggerMemory sb) {
      var memoryLogger = sb as LoggerMemory;
      XElement debugXml = memoryLogger != null && memoryLogger.saveDumpXml ? (memoryLogger.dumpXml == null ? (memoryLogger.dumpXml = new XElement("root")) : memoryLogger.dumpXml) : null;

      int cnt = 0;
      //unique tag IDS
      var uniqTagIds = new HashSet<string>();
      var allTags = pg.scan().ToArray(); //optimalizace: all tags do array

      //normalize instrs
      if (pg.instrs != null) pg.instrs = prodDef.instrUrls(pg.instrs).ToArray();

      foreach (var tg in allTags.Where(t => !(t is text))) {
        if (string.IsNullOrEmpty(tg.id)) uniqTagIds.Add(tg.id = string.Format("_{0}", cnt++)); //nove id
        else if (uniqTagIds.Contains(tg.id)) sb.ErrorLine(pg.url, " Duplicate id: " + tg.id); //duplicitni id
        else uniqTagIds.Add(tg.id); //existujici id
      }

      //expand macro a (include + replace) sound
      postProcess_ExpandMacros(pg, pg, sb, true); dump(debugXml, pg, "postProcess_ExpandMacros");

      //unique tag IDS
      uniqTagIds.Clear();
      allTags = pg.scan().ToArray(); //optimalizace: all tags do array
      foreach (var tg in allTags.Where(t => !(t is text))) {
        if (string.IsNullOrEmpty(tg.id)) uniqTagIds.Add(tg.id = string.Format("_{0}", cnt++)); //nove id
        else if (uniqTagIds.Contains(tg.id)) sb.ErrorLine(pg.url, " Duplicate id: " + tg.id); //duplicitni id
        else uniqTagIds.Add(tg.id); //existujici id
      }

      //expand mediaText, vytvor sound grupy a intervaly
      postProcess_Sound(pg, allTags, sb); dump(debugXml, pg, "postProcess_Sound");

      //CSS styles. zmeni pg
      //CSS.applyCSS(ref pg, ex); dump(debugXml, pg, "applyCSS");

      //kontrola a dokonceni
      foreach (var ctrl in pg.scan()) ctrl.checkAndFinish(pg, sb); if (debugXml != null) dump(debugXml, pg, "checkAndFinish");

      //remove fake macro tags
      postProcess_ExpandMacros(pg, pg, sb, false); dump(debugXml, pg, "postProcess_ExpandMacros");

      //vytvoreni struktury pro vyhodnoceni
      postProcess_Evals(pg, sb); dump(debugXml, pg, "postProcess_Evals");

      if (debugXml != null) debugXml.Add(new XCData("\r\n" + CourseModel.tag.pageToJsons(pg) + "\r\n"));

      //kontrola unique Id, pro jistotu. V XML pribyly napr. eval grupy apod.
      var dupls = pg.scan().Select(c => c.id).Where(id => !string.IsNullOrEmpty(id)).GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
      if (dupls.Length > 0) sb.ErrorLine(pg.url, " 'eval-group' cannot has the same value as any 'id' value: " + dupls.Aggregate((r, i) => r + "," + i));

      //See also
      //TODO if (sitemapData == null || !(root is Page)) return;
      //TODO var pg = root as Page;
      //pg.seeAlso = (pg.seeAlso == null ? Enumerable.Empty<schools.seeAlsoLink>() : pg.seeAlso).Concat(postProcess_SeeAlso(pg, sitemapData/*, isLMAuthor*/, err)).ToArray();
      //if (pg.seeAlso.Length == 0) pg.seeAlso = null;
    }

    //private static void postProcess_SameWidth(body pg, LoggerObj sb) {
    //  var widthTags = pg.scan().Where(e => e is offering | e is edit).ToArray();
    //  HashSet<string> edProcessed = new HashSet<string>();
    //  List<string> swGroup = new List<string>();
    //  foreach (var off in widthTags.OfType<offering>()) {
    //    foreach (var edId in off.words.Split('|').Where(w => w.Length>2 && w[0]=='#')) edProcessed.Add(edId.Substring(1));
    //    swGroup.Add(off.words);
    //  }
    //  foreach (var smartGrp in widthTags.OfType<edit>().Where(e => !edProcessed.Contains(e.id) && !string.IsNullOrEmpty(e.smartWidth)).GroupBy(e => e.smartWidth)) {
    //    swGroup.Add(smartGrp.Select(e => "#" + e.id).Aggregate((r, i) => r + "|" + i));
    //  }
    //  pg.sameWidthGroups = swGroup.Count == 0 ? null : swGroup.ToArray();
    //}

    static void dump(XElement debugXml, body pg, string title) {
      if (!dumpXml || debugXml == null) return;
      var res = pg.ToElement(); res.Attributes().Remove(); res.Element("head").SetElementValue("title", title);
      res.Descendants().Select(e => e.Attribute("code-listing")).Remove();
      debugXml.Add(res);
    }

    static void postProcess_Evals(body pg, LoggerMemory sb) {
      pg.evalPage = new _evalPage();
      //AngularJS
      var allEvals = pg.scan().OfType<evalControl>().ToArray(); if (allEvals.Length == 0) return;
      //var allEvals = pg.scan().OfType<evalControl>().Where (c => !(c is writing) && !(c is recording)).ToArray();
      if (allEvals.Length == 0) return;
      foreach (var rd in allEvals.OfType<radioButton>().Where(rb => string.IsNullOrEmpty(rb.evalGroup))) rd.evalGroup = "and-_frb";
      //kontrola: jedna eval grupa = jeden evalBtn
      var wrongs = allEvals.Where(t => !string.IsNullOrEmpty(t.evalGroup) && !(t is evalButton)).GroupBy(e => e.evalGroup).Where(grp => grp.Select(t => t.evalButtonId).Distinct().Count() > 1).ToArray();
      if (wrongs.Length > 0) { sb.ErrorLineFmt(pg.url, "Some eval-group has more than single eval-btn-id", wrongs.Select(g => g.Key).Aggregate((r, i) => r + ", " + i)); return; }

      //finishes
      Func<evalControl[], _evalGroup, _evalGroup> finishEvGrp = (ctrls, ev) => {
        string groupId = "";
        if (!string.IsNullOrEmpty(ev.id)) {
          var grps = evalControl.evalRegEx.Match(ev.id).Groups.OfType<Group>();
          ev.isAnd = grps.Any(g => g.Value == "and");
          ev.isExchangeable = grps.Any(g => g.Value == "exchangeable");
          groupId = ev.id.Replace("and-", null).Replace("-exchangeable", null);
        }

        //kontroly
        var evTypes = ctrls.Select(e => e.GetType()).Distinct().ToArray();
        if (evTypes.Any(t => t == typeof(radioButton))) {
          var radios = ctrls.OfType<radioButton>(); bool isReadOnlyEtc = false;
          if (evTypes.Length > 1) sb.ErrorLineFmt(pg.url, "Radio-buttons have to have its own eval-group");
          if (!ev.isAnd) sb.ErrorLineFmt(pg.url, "Radio-buttons have to have AND eval-group only");
          if (ctrls.Length < 2) { sb.ErrorLineFmt(pg.url, "At least 2 radioButtons required (id={0})", ev.id); }
          if (radios.Any(r => r.skipEvaluation)) { isReadOnlyEtc = true; foreach (var r in radios) r.skipEvaluation = true; }
          if (radios.Any(r => r.readOnly)) { isReadOnlyEtc = true; foreach (var r in radios) { r.readOnly = true; r.skipEvaluation = false; } }//readonly ma prednost
          if (!isReadOnlyEtc && radios.Where(r => r.correctValue).Count() != 1) sb.ErrorLineFmt(pg.url, "Just one radio-button with correct-value==true required (id={0})", ev.id);

          //foreach (var r in radios) r.group = groupId;
          if (ctrls.Length > 0) {
            if (pg.evalPage.radioGroupsObj == null) pg.evalPage.radioGroupsObj = new Dictionary<string, string[]>();
            pg.evalPage.radioGroupsObj[groupId] = ctrls.Select(e => e.id).ToArray();
          }
        } else if (evTypes.Any(t => t == typeof(wordSelection))) {
          if (ev.id != null) {
            var wordSels = ctrls.OfType<wordSelection>().ToArray(); //vsechny
            if (evTypes.Length > 1) sb.ErrorLineFmt(pg.url, "word-selection have to have its own eval-group, eval-group={0}", ev.id);
            if (!ev.isAnd) sb.ErrorLineFmt(pg.url, "word-selection have to have AND eval-group only, eval-group={0}", ev.id);
            if (ctrls.Length > 0) {
              wordSelection.checkWords(ctrls.Cast<wordSelection>().Select(w => w.words), ev.id, pg, sb);
              if (pg.evalPage.radioGroupsObj == null) pg.evalPage.radioGroupsObj = new Dictionary<string, string[]>();
              pg.evalPage.radioGroupsObj[groupId] = ctrls.Select(e => e.id).ToArray();
            }
          }
        } else if (ev.isExchangeable) {
          if (evTypes[0] != typeof(gapFill) && evTypes[0] != typeof(dropDown)) sb.ErrorLineFmt(pg.url, "exchangeable eval-group is only for gap-fill or drop-down");
          if (evTypes.Length > 1) { sb.ErrorLineFmt(pg.url, "exchangeable eval-group cannot contain mix of gap-fill and drop-down"); return ev; }
          var isGapFill = evTypes[0] == typeof(gapFill);
          var vals = (isGapFill ? ctrls.OfType<gapFill>().SelectMany(e => e.correctValue.Split('|').Select(v => gapFill.normalize(v, e.caseSensitive))) : ctrls.Cast<dropDown>().Select(d => d.correctValue)).Where(v => !v.StartsWith(edit.fakeEdit)).ToArray();
          if (vals.Distinct().Count() < vals.Length) sb.ErrorLine(pg.url, "exchangeable eval-group has to have different correct-value's (for gap-fill is value normalized)");
          if (!isGapFill && ctrls.Cast<dropDown>().Select(d => d.offeringId).Where(o => !string.IsNullOrEmpty(o)).Distinct().Count() > 1)
            sb.ErrorLine(pg.url, "Two drop-down's with different offering-id attribute in one eval-group");
        }
        ev.maxScore = 0;
        ctrls = ctrls.Where(c => !c.isSkipEvaluation() && !c.isReadOnly()).ToArray();
        if (ctrls.Length > 0) {
          foreach (var c in ctrls) ev.maxScore += c.getMaxScore();
          if (ev.isAnd) ev.maxScore = (int)Math.Round((double)ev.maxScore / ctrls.Length, 0);
        }
        ev.evalControlIds = ctrls.Select(e => e.id).ToArray();
        return ev;
      };
      Func<_evalBtn, _evalBtn> finishEvBtn = ev => {
        if (string.IsNullOrEmpty(ev.id)) return ev;
        if (!allEvals.OfType<evalButton>().Any(e => e.id == ev.id)) sb.ErrorLineFmt(pg.url, "Cannto find evalBtn.id={0}", ev.id);
        return ev;
      };

      //eval tree
      pg.evalPage.Items = allEvals.Where(c => !(c is evalButton)).GroupBy(e => e.evalButtonId).Select(btnGrp => finishEvBtn(new _evalBtn {
        btnId = btnGrp.Key,
        Items = btnGrp.GroupBy(e => e.evalGroup).Select(evalGrp => finishEvGrp(evalGrp.ToArray(), new _evalGroup { id = evalGrp.Key })).Where(g => g.evalControlIds != null && g.evalControlIds.Length > 0).ToArray()
      })).Where(bt => bt.Items != null && bt.Items.Length > 0).ToArray();
      //max score
      pg.evalPage.maxScore = 0;
      foreach (var btn in pg.evalPage.Items.Cast<_evalBtn>()) {
        foreach (var grp in btn.Items.Cast<_evalGroup>()) pg.evalPage.maxScore += grp.maxScore;
      }
      //v finalnim XML jiz neni potreba
      foreach (var ev in allEvals) ev.evalButtonId = ev.evalGroup = null;
    }

    static void postProcess_Sound(body pg, tag[] allTags, LoggerMemory sb) {
      //registrace mediaTag a sndFile
      var tags = pg.scan().OfType<mediaTag>().ToDictionary(m => m.id);
      var files = tags.Values.Select(t => t.file).Where(f => f != null).Cast<_sndFile>().ToArray();

      pg.sndPage = new _sndPage();

      //jednoznacna identifikace sents a doplneni text a actor
      var cnt = 0;
      foreach (var f in files) {
        if (f is cutDialog) {
          foreach (var repl in f.Items.Cast<replica>()) {
            if (repl.Items == null) { sb.ErrorLineFmt(pg.url, "Missing replica sentences"); continue; }
            foreach (var s in repl.Items.Cast<phrase>()) { s.idx = cnt++; if (s.text == null) s.text = s.itemsTotext(); s.actor = repl.actorName; }
          }
        } else
          foreach (var s in f.Items.Cast<phrase>()) { s.idx = cnt++; if (s.text == null) s.text = s.itemsTotext(); }
      }

      foreach (var mt in tags.Values.OfType<mediaText>().Where(mt => !string.IsNullOrEmpty(mt.continueMediaId))) {
        if (string.IsNullOrEmpty(mt.subset)) { sb.ErrorLineFmt(pg.url, "Missing subset attribute when continue-id is not empty"); return; }
        if (!tags.ContainsKey(mt.continueMediaId)) { sb.ErrorLineFmt(pg.url, "Cannot find element from continue-id attribute"); return; }
        if (!string.IsNullOrEmpty(mt.shareMediaId)) { sb.ErrorLineFmt(pg.url, "Both continue-id and share-id attributes not allowed"); return; }
        var tg = tags[mt.continueMediaId] as mediaText;
        if (tg == null) { sb.ErrorLineFmt(pg.url, "continue-id attribute does not contains media-text element"); return; }
        if (!string.IsNullOrEmpty(tg.shareMediaId)) { sb.ErrorLineFmt(pg.url, "share-id attributes not allowed for element pointed by continue-id attribute"); return; }
        if (string.IsNullOrEmpty(tg.subset)) { sb.ErrorLineFmt(pg.url, "Missing subset attribute when pointed by continue-id attribute"); return; }
        if (tg.file != null) { sb.ErrorLineFmt(pg.url, "Cannto have sound file when pointed by continue-id attribute"); return; }
      }

      List<int> continueSeq = new List<int>(); List<int> tagSeq = new List<int>();
      int seqCount = 0;
      //List<string> sndSentences = new List<string>();

      List<_sndGroup> groups = new List<_sndGroup>();
      foreach (var t in tags.Values.Where(t => t.file != null)) {
        var isPassive = t.passiveTag();
        var sf = (_sndFile)t.file;
        var allSents = Enumerable.Range(0, sf.Items.Length).SelectMany(idx => sf.idxToSents(idx)).ToArray();
        _sndGroup sndGroup = new _sndGroup() { sf = sf, id = "seq_" + (seqCount++).ToString() }; groups.Add(sndGroup);
        continueSeq.Clear();
        if (!(t is mediaText) || string.IsNullOrEmpty(((mediaText)t).continueMediaId)) { //cely sndFile
          t._sentGroupId = sndGroup.id;
          if (string.IsNullOrEmpty(t.subset)) {
            continueSeq.AddRange(allSents.Select(s => s.idx));
            t.addSequence(sf, isPassive, Enumerable.Range(0, sf.Items.Length));
          } else {
            if (!parseSentSubset(pg, t.subset, sf.Items.Length - 1, tagSeq, sb)) break; //break => parse error
            t.addSequence(sf, isPassive, tagSeq);
            continueSeq.AddRange(tagSeq.SelectMany(idx => sf.idxToSents(idx).Select(s => s.idx)));
          }
        } else {
          mediaText tg = (mediaText)t;
          while (true) { //cyklus pres continueId chain
            tg._sentGroupId = sndGroup.id;
            if (!parseSentSubset(pg, tg.subset, sf.Items.Length - 1, tagSeq, sb)) break; //break => parse error
            tg.addSequence(sf, isPassive, tagSeq);
            continueSeq.AddRange(tagSeq.SelectMany(idx => sf.idxToSents(idx).Select(s => s.idx)));
            if (string.IsNullOrEmpty(tg.continueMediaId)) break;
            tg = (mediaText)tags[tg.continueMediaId];
          }
        }
        if (continueSeq.Distinct().Count() < continueSeq.Count) { sb.ErrorLine(pg.url, "Multiple ocurance of the same sentence defined in subset attribute"); continue; }

        // zpocteni souvislych intervalu
        if (continueSeq.Count > 0) {
          int firstSentIdx = allSents[0].idx; int begIdx = -1; int endIdx = 0;
          Action addInt = () => {
            if (begIdx < 0) return;
            _sndInterval sint = new _sndInterval { Items = allSents.Skip(begIdx - firstSentIdx).Take(endIdx - begIdx + 1).Select(s => s.toSndSent()).ToArray() };
            sndGroup.intervals.Add(sint);
          };
          foreach (var i in continueSeq) {
            if (begIdx < 0) { begIdx = endIdx = i; continue; }
            if (i != endIdx + 1) { addInt(); begIdx = endIdx = i; continue; }
            endIdx = i;
          }
          addInt();
        }
      }
      //**** obarveni vsech dialogu
      cutDialog.finishReplicas(pg, tags.Values.Select(t => t.file).OfType<_sndFile>().Where(f => f.tempReplicas != null).Select(f => f.tempReplicas).ToArray(), sb);

      //**** kontrola a vyhozeni vsech pasivnich grup
      groups = groups.Where(g => !tags.Values.Where(t => t._sentGroupId == g.id).First().passiveTag()).ToList();

      //**** shareId
      foreach (var t in tags.Values.Where(t => !string.IsNullOrEmpty(t.shareMediaId))) {
        if (!tags.ContainsKey(t.shareMediaId)) { sb.ErrorLine(pg.url, "Cannot find element from share-id attribute (note: you cannot share media-text when is-passive=true)"); continue; }
        t._sentGroupId = tags[t.shareMediaId]._sentGroupId;
      }

      //after filled
      foreach (var f in tags.Values) f.file = null;
      //maji vsechny sentGroupId?
      var noSnd = tags.Values.Where(t => string.IsNullOrEmpty(t._sentGroupId)).Select(t => t.id).DefaultIfEmpty().Aggregate((r, i) => r + "," + i);
      if (!string.IsNullOrEmpty(noSnd)) sb.ErrorLineFmt(pg.url, "Sound file not found or invalid (media tags: {0})", noSnd);

      //hotovo: vloz grupy do sndPage
      foreach (var grp in groups) { grp.Items = grp.intervals.ToArray(); grp.intervals = null; }
      Func<_sndGroup, _sndFileGroup, _sndFileGroup> finishFileGroup = (g, fg) => {
        /*fg.audioUrl = g.sf.audioUrl;*/
        fg.mediaUrl = g.sf.mediaUrl;
        //fg.videoFormat = f.sf.videoFormat != null && f.sf.videoFormat.StartsWith("@") ? _sndFile.lmFormats[f.sf.videoFormat] : f.sf.videoFormat;
        //kontrola grup
        foreach (var grp in fg.Items.Cast<_sndGroup>()) {
          var groupTags = tags.Values.Where(t => t._sentGroupId == grp.id).ToArray();
          //if (groupTags.Any(t => t.passiveTag()) && groupTags.Length > 1) sb.ErrorLineFmt(pg.url, "No share is alowed when media-text.is-passive=true : share-id=", g.id);
          if (groupTags.OfType<mediaPlayer>().Any() && g.Items.Length > 1)
            sb.ErrorLineFmt(pg.url, "Only single sound interval is alowed when media-player is in share-id group: share-id=", grp.id);
          var vidCount = groupTags.OfType<mediaVideo>().Count();
          if ((fg.isVideo && vidCount != 1) || (!fg.isVideo && vidCount != 0))
            sb.ErrorLineFmt(pg.url, "Every video share-id group must contain just one media-video element: share-id=", grp.id);
        }
        return fg;
      };
      pg.sndPage.Items = groups.GroupBy(g => g.sf.anyUrl).Select(g => finishFileGroup(g.First(), new _sndFileGroup { Items = g.ToArray() })).ToArray();

    }


    //static Regex sndFormatRegex = new Regex(_sndFile.mediaTag_format_regex);

    static bool parseSentSubset(body pg, string subset, int maxIdx, List<int> tagSeq, LoggerMemory sb) {
      tagSeq.Clear();
      foreach (var interv in subset.Split(',')) { //cyklus pres intervaly
        var idxs = interv.Split('-').Select(ti => ti == "" ? -1 : int.Parse(ti)).ToArray();
        if (idxs.Length == 2) {
          if (idxs[0] < 0) idxs[0] = 0; else if (idxs[1] < 0) idxs[1] = maxIdx;
          if (idxs[0] > idxs[1]) { for (var i = idxs[1]; i <= idxs[0]; i++) tagSeq.Add(i); } else if (idxs[0] == idxs[1]) tagSeq.Add(idxs[0]); else for (var i = idxs[0]; i <= idxs[1]; i++) tagSeq.Add(i);
        } else
          tagSeq.Add(idxs[0]);
      }
      if (tagSeq.Max() > maxIdx) { sb.ErrorLine(pg.url, "Value of sentence index from share-id attribute exceeded"); return false; }
      return true;
    }

    static void postProcess_ExpandMacros(body pg, tag tg, LoggerMemory sb, bool isStart) {
      if (tg.Items == null) return;
      if (isStart) {
        for (int i = 0; i < tg.Items.Length; i++) {
          var mac = tg.Items[i] as IMacroTemplate;
          if (mac == null) continue;
          var items = mac.Generate(pg, sb).ToArray();
          if (items.Length == 1)
            tg.Items[i] = items[0];
          else
            tg.Items[i] = new node { Items = items, styleSheet = ((tag)mac).styleSheet, temporaryMacroItem = true };
        }
      } else {
        if (tg.Items.Any(t => t.temporaryMacroItem)) {
          List<tag> its = new List<tag>();
          foreach (var subTg in tg.Items) {
            if (!subTg.temporaryMacroItem) { its.Add(subTg); continue; }
            its.AddRange(subTg.Items);
          }
          tg.Items = its.ToArray();
        }
      }
      foreach (var subTg in tg.allItems()) postProcess_ExpandMacros(pg, subTg, sb, isStart);
    }
    //public static string[] temporaryMacroItem = new string[0];

    //eval kontrolkam a Tts - prideli jednoznacne ID
    //public static void postProcess_AdjustIdAndCss(body pg) {
    //  int cnt = 0; foreach (var tg in pg.scan().Where(t => string.IsNullOrEmpty(t.id))) tg.id = string.Format("_id{0}", cnt++);
    //}

    static JsonSerializerSettings jsonSet = new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore };
  }


  public partial class include {

    public static _sndFile loadCutUrl(body pg, string cutUrl, LoggerMemory sb) {
      if (string.IsNullOrEmpty(cutUrl)) { sb.ErrorLineFmt(pg.url, "cut-url attribute not specified"); return null; }
      var absUrl = VirtualPathUtility.Combine(pg.url, cutUrl);
      var fn = data.fileNameFromUrl(absUrl);
      try {
        var oldSaveDump = sb.saveDumpXml; sb.saveDumpXml = false;
        try {
          bool isError;
          var root = CourseModel.tag.loadExerciseXml(fn, sb, out isError);
          if (isError) return null;
          var res = CourseModel.tag.FromElement<_sndFile>(root, sb, cutUrl);
          if (res == null) return null;
          res.includeParentUrl(absUrl);
          res.checkAndAdjustUrls(pg, sb, true);
          return res;
        } finally { sb.saveDumpXml = oldSaveDump; }
      } catch { sb.ErrorLine(pg.url, "Cannot load sndFile " + fn); return null; }
    }

    public _sndFile expand(body pg, LoggerMemory sb) {
      var res = loadCutUrl(pg, cutUrl, sb);
      if (res == null) return null;
      if (res is cutDialog && !(this is includeDialog)) { sb.ErrorLineFmt(pg.url, "Wrong include file, cut-dialog expected, cut-text found (id={0}, cut-url={1})", id, cutUrl); return null; }
      if (res is cutText && !(this is includeText)) { sb.ErrorLineFmt(pg.url, "Wrong include file, cut-text expected, cut-dialog found (id={0}, cut-url={1})", id, cutUrl); return null; }
      res.id = id;
      //nahrada vet
      if (Items != null) {
        var repls = Items.OfType<phraseReplace>().ToArray();
        if (res is cutDialog) {
          var ptrs = repls.Select(r => r.replicaPhraseIdx).GroupBy(r => r).Where(g => g.Count() > 1).ToArray();
          if (ptrs.Any(p => string.IsNullOrEmpty(p.Key))) sb.ErrorLineFmt(pg.url, "Some replica-sent-idx are empty (cut-url={0})", cutUrl);
          if (ptrs.Length > 0) sb.ErrorLineFmt(pg.url, "Some duplicates replica-sent-idx value (cut-url={0})", cutUrl);
        } else {
          var ptrs = repls.Select(r => r.phraseIdx).GroupBy(r => r).Where(g => g.Count() > 1).ToArray();
          if (ptrs.Length > 0) sb.ErrorLineFmt(pg.url, "Some duplicates sent-idx value (cut-url={0})", cutUrl);
        }
        foreach (var sentRepl in repls) {
          var sent = res.findSentToReplace(pg, sentRepl, sb); if (sent == null) continue; //error je v sb
          sent.text = sent.itemsTotext();
          sent.Items = sentRepl.Items;
          sentRepl.Items = null;
        }
      }
      return res;
    }
  }

  public interface ISndToMedia { tag getMedia(_sndFile file, bool isPassive); }

  public partial class mediaText {
    public override void addSequence(_sndFile file, bool isPassive, IEnumerable<int> seq) {
      Items = seq.Select(idx => file.Items[idx]).Cast<ISndToMedia>().Select(s => s.getMedia(file, isPassive)).ToArray();
    }
  }

  public partial class replica : ISndToMedia {
    tag ISndToMedia.getMedia(_sndFile file, bool isPassive) {
      var res = new _mediaReplica { iconId = actorId, actor = actorName, Items = Items == null ? null : Items.Cast<ISndToMedia>().Select(s => s.getMedia(null, isPassive)).ToArray() };
      if (file.tempReplicas == null) file.tempReplicas = new List<_mediaReplica>();
      file.tempReplicas.Add(res); //evidence posloupnosti replik kvuli naplneni ikon a left priznaku
      return res;
    }
  }
  public partial class phrase : ISndToMedia {
    tag ISndToMedia.getMedia(_sndFile file, bool isPassive) {
      return new _mediaSent { idx = idx, Items = isPassive && Items == null ? new tag[] { new text { title = text } } : Items };
    }
    [XmlIgnore, JsonIgnore]
    public string text;
    [XmlIgnore, JsonIgnore]
    public string actor;
    public _sndSent toSndSent() { return new _sndSent { actor = actor, begPos = begPos, endPos = endPos, idx = idx, text = text }; }//Items == null || Items.Length != 1 || !(Items[0] is text) ? null : ((text)Items[0]).title }; }
    public string itemsTotext() {
      var its = Items; //Items = null;
      if (its != null && its.Length == 1 && its[0] is text) { Items = null; return ((text)its[0]).title.Trim(); } else return null;
      //return its != null && its.Length == 1 && its[0] is text ? ((text)its[0]).title.Trim() : null;
    }

  }

  public partial class urlTag {
    [XmlIgnore, JsonIgnore]
    public string anyUrl { get { return /*audioUrl != null ? audioUrl :*/ mediaUrl; } }
    [XmlIgnore, JsonIgnore]
    public bool isVideo { get { return /*videoUrl != null;*/ mediaUrl != null && mediaUrl.IndexOf('@') >= 0; } }

    public void checkAndAdjustUrls(body pg, LoggerMemory sb, bool isCutFile) {
      mediaTag mt = this as mediaTag; _sndFile sf = this as _sndFile;
      var notEmpties = new bool[] {
        mediaUrl != null, 
        //audioUrl != null, 
        (mt != null ? mt.cutUrl : null) != null,
        (mt != null ? mt.shareMediaId : null) != null,
        (mt != null ? mt.file != null : sf.file!=null) }.
        Select(t => t ? 1 : 0).Sum();
      var cutMsg = mt != null ? ", cut-url, share-id" : null;
      string childMsg;
      if (mt != null) childMsg = "include-text, include-dialog, cut-text, cut-dialog";
      else if (this is cutDialog) childMsg = "include-text";
      else if (this is cutText) childMsg = "include-dialog";
      else throw new NotImplementedException();
      if (notEmpties == 0) { } //mozna tag, na ktery odkazuje jinu tag pomoci continue-id
      else if (notEmpties != 1) sb.ErrorLineFmt(pg.url, "Specify just one from: video-url, audio-url" + cutMsg + " attributes or " + childMsg + " elements (id={0})", id);
      expandVideoAudioFormat(pg);
      if (mt != null && mt.cutUrl != null) { mt.cutUrl = Path.ChangeExtension(mt.cutUrl, null); return; }
    }

    public void expandVideoAudioFormat(body pg) {
      //if (audioUrl != null) { audioUrl = Path.ChangeExtension(VirtualPathUtility.Combine(pg.url, audioUrl), ".mp3"); return; }
      if (isVideo) {
        var parts = mediaUrl.Split(videoFormatDelim); if (parts.Length != 2) return;
        var m = isFormatPtr.Match(parts[1]);
        if (m.Success) { var ptr = m.Groups["ptr"].Value; mediaUrl = VirtualPathUtility.Combine(pg.url, parts[0]) + videoFormatDelim + lmFormats[ptr]; }
      } else if (mediaUrl != null)
        mediaUrl = VirtualPathUtility.Combine(pg.url, mediaUrl);
    }

    public void includeParentUrl(string absUrl) {
      if (!isVideo) {
        //if (audioUrl == null) audioUrl = absUrl + ".mp3";
        if (mediaUrl == null) mediaUrl = absUrl + ".mp3";
      } else {
        var parts = mediaUrl.Split(videoFormatDelim); if (parts.Length != 2) return;
        if (string.IsNullOrEmpty(parts[0])) mediaUrl = absUrl + videoFormatDelim + parts[1];
      }
    }


    public static Dictionary<string, string> lmFormats = new Dictionary<string, string>() {
      { "std-4","16by9:*-webm,mp4|640-small.webm,small.mp4"},
      { "std-2","16by9:*-webm,mp4"}
    };
    public override IEnumerable<string> getExternals(body pg) {
      if (string.IsNullOrEmpty(mediaUrl)) yield break;
      //audio
      if (!isVideo) {
        //if (string.IsNullOrEmpty(audioUrl)) yield break;
        //yield return Path.ChangeExtension(audioUrl, ".mp3");
        yield return mediaUrl;
        yield break;
      }
      //video
      var parts = mediaUrl.Split(videoFormatDelim); if (parts.Length != 2) yield break;
      var exts = parts[1].Split(':')[1].Split('|').Select(p => p.Split('-')[1]).SelectMany(es => es.Split(','));
      foreach (var ext in exts) yield return Path.ChangeExtension(parts[0], "." + ext);
    }
    public const char videoFormatDelim = '@';
    //Regex pro zadani formatu videa
    const string webm = @"(\w|\.)*webm";
    const string mp4 = @"(\w|\.)*mp4";
    static string exts = string.Format(@"({0}|{1})+(,{0}|,{1})*", webm, mp4); //ext,ext,...
    static string fmts = string.Format(@"(\d+|\*)-{0}", exts); //width-ext,ext,...
    static string rx = string.Format(@"^.*@((std-4|std-2)$|(16by9|4by3):({0})+(\|{0})*)$", fmts); //je rovno mediaTag_format_regex. Napr. 16x9:1024-webm,mp4|384-small.webm,small.mp4 nebo @grafia

    static Regex isFormatPtr = new Regex(@"^(?<ptr>std-4|std-2)$");

  }


  public partial class _sndFile {
    [XmlIgnore, JsonIgnore]
    public List<_mediaReplica> tempReplicas;

    public virtual phrase findSentToReplace(body pg, phraseReplace repl, LoggerMemory sb) { throw new NotImplementedException(); }
    public virtual IEnumerable<phrase> idxToSents(int idx) { throw new NotImplementedException(); }
    public virtual void processInclude(body pg, LoggerMemory sb) {
      //if (!isPassive && file == null) checkAndAdjustMediaUrl(pg, ref audioUrl, ref videoUrl, sb, false);
    }

  }

  public partial class cutDialog {
    public override IEnumerable<phrase> idxToSents(int idx) { return Items[idx].Items == null ? Enumerable.Empty<phrase>() : Items[idx].Items.Cast<phrase>(); }
    public override phrase findSentToReplace(body pg, phraseReplace repl, LoggerMemory sb) {
      try {
        var parts = repl.replicaPhraseIdx.Split('.'); return (phrase)Items[int.Parse(parts[0])].Items[int.Parse(parts[1])];
      } catch {
        sb.ErrorLineFmt(pg.url, "Cannot find dialog sentence, snd-dialog.id={0}, replice-sent-idx={1}", id, repl.replicaPhraseIdx);
        return null;
      }
    }
    //vytvoreni dialogu z textu
    public override void processInclude(body pg, LoggerMemory sb) {
      var sndFile = file.expand(pg, sb);
      if (sndFile == null) { sb.ErrorLineFmt(pg.url, "Wrong include result, id={0}", id); return; }
      //audioUrl = sndFile.audioUrl; 
      mediaUrl = sndFile.mediaUrl;
      //videoFormat = sndFile.videoFormat;
      var repls = Items == null ? new replica[0] : Items.OfType<replica>().ToArray();
      //kontrola neprazdne replika list
      if (repls.Length == 0) { sb.ErrorLineFmt(pg.url, "Missing replica elements, id={0}", id); return; }
      //kontrola nenulove usePhrases
      if (repls.Any(r => r.numberOfPhrases == 0)) { sb.ErrorLineFmt(pg.url, "Missing or zero use=phrases attribute, id={0}", id); return; }
      //kontrola poctu vet
      var sum = repls.Sum(r => r.numberOfPhrases);
      if (sum > sndFile.Items.Length) { sb.ErrorLineFmt(pg.url, "Too many used sentences, id={0}, sum={1}", id, sum); return; }
      //dopln vety do replikas
      var begIdx = 0;
      foreach (var repl in repls) {
        repl.Items = sndFile.Items.Skip(begIdx).Take(repl.numberOfPhrases).ToArray();
        begIdx += repl.numberOfPhrases;
      }
    }

    public static void finishReplicas(body pg, List<_mediaReplica>[] dlgRepls, LoggerMemory sb) {
      //kontrola IconId a actor
      var wrongs = dlgRepls.SelectMany(r => r).Where(r => string.IsNullOrEmpty(r.actor) && r.iconId == IconIds.no).Count();
      if (wrongs > 0) { sb.ErrorLineFmt(pg.url, "Missing both replica.icon-id and replica.actor attribute"); return; }

      Func<int, IconIds> intToIconId = idx => (IconIds)((idx % iconsCount) + 1);
      //ikony pro aktory (globalne pro vsechny dialogy na strance)
      int evidenceIdx = iconsCount;
      var allActors = dlgRepls.SelectMany(r => r).Select(r => r.actor).Where(a => !string.IsNullOrEmpty(a)).Distinct().ToArray();
      var allActorsChoosenIcon = Enumerable.Range(evidenceIdx, allActors.Length).Select(i => intToIconId(i)).ToArray();
      evidenceIdx += allActors.Length;
      var allActorsMap = allActors.Zip(allActorsChoosenIcon, (act, ic) => new KeyValuePair<string, IconIds>(act, ic)).ToDictionary(ai => ai.Key, ai => ai.Value);

      //pro kazdy dialog
      foreach (var repls in dlgRepls) {
        var dlgActors = repls.Where(r => !string.IsNullOrEmpty(r.actor)).Select(r => r.actor).Distinct().ToArray(); //all dlg actors
        var dlgIcons = repls.Where(r => string.IsNullOrEmpty(r.actor)).Select(r => r.iconId).Distinct().ToArray(); //all dlg icons
        var usedIcons = new HashSet<IconIds>(); //used dlg icons
        var overFlowIdx = 0; //v pripade vice nez 6 aktoru na dialog - ikony se opakuji
                             //prirad icony actorum, prednost ma globalni prirazeni
        var actorMap = new Dictionary<string, IconIds>();
        foreach (var act in dlgActors) {
          var ic = allActorsMap[act];
          if (usedIcons.Contains(ic)) {
            var newIc = Enumerable.Range(evidenceIdx, iconsCount).FirstOrDefault(i => !usedIcons.Contains(intToIconId(i)));
            if (newIc == 0) newIc = overFlowIdx++; //evidenceIdx neni nikdy nula, vzdy se k ni pricita neskodna iconsCount konstanta
            ic = intToIconId(newIc);
          }
          usedIcons.Add(actorMap[act] = ic);
        }
        //prirad ikony ikonam
        var iconMap = new Dictionary<IconIds, IconIds>();
        foreach (var act in dlgIcons) {
          var newIc = Enumerable.Range(evidenceIdx, iconsCount).FirstOrDefault(i => !usedIcons.Contains(intToIconId(i)));
          if (newIc == 0) newIc = overFlowIdx++;
          usedIcons.Add(iconMap[act] = intToIconId(newIc));
        }
        //vyuziti ikon z maps
        foreach (var r in repls) r.iconId = !string.IsNullOrEmpty(r.actor) ? actorMap[r.actor] : iconMap[r.iconId];
        //posun evidenceIdx, jako maximum z pouzitych iconId. Dalsi dialog tedy s urcitou pravdepodobnosti bude mit jine ikony
        evidenceIdx = repls.Select(r => (int)r.iconId).Max() + iconsCount;
        //vypocti left x right pro umisteni repliky dialogu
        var iconIsLeftDir = new Dictionary<IconIds, bool>(); bool lastLeft = true;
        foreach (var r in repls) {
          bool rLeft;
          if (iconIsLeftDir.TryGetValue(r.iconId, out rLeft)) r.dlgLeft = rLeft;
          else {
            r.dlgLeft = iconIsLeftDir[r.iconId] = lastLeft;
            lastLeft = !lastLeft;
          }
        }
      }
    }
    static int iconsCount = LowUtils.EnumGetValues<IconIds>().Where(i => i != IconIds.no).Count();
  }

  public partial class cutText {
    public override IEnumerable<phrase> idxToSents(int idx) { return XExtension.Create((phrase)Items[idx]); }
    public override phrase findSentToReplace(body pg, phraseReplace repl, LoggerMemory sb) {
      try {
        return (phrase)Items[repl.phraseIdx];
      } catch {
        sb.ErrorLineFmt(pg.url, "Cannot find text sentence, cut-text.id={0}, sent-idx={1}", id, repl.phraseIdx);
        return null;
      }
    }
    //vytvoreni textu z dialogu
    public override void processInclude(body pg, LoggerMemory sb) {
      var sndFile = file.expand(pg, sb);
      //audioUrl = sndFile.audioUrl;
      mediaUrl = sndFile.mediaUrl;
      //videoFormat = sndFile.videoFormat;
      Items = sndFile.Items.SelectMany(f => f.Items).ToArray();
    }
  }

  //Tag objekt, predchudce vsech tagu v nove verzi dat pro kurzy
  public partial class tag {

    [XmlIgnore, JsonIgnore]
    public bool temporaryMacroItem;

    public bool passiveTag() { return this is mediaText && ((mediaText)this).passive; }

    public static void saveExXml(string fn, XElement root) {
      File.WriteAllText(fn, saveExXmlStr(root));
    }

    public static string saveExXmlStr(XElement root) {
      adjustXsdReference(root);
      using (var ms = new MemoryStream()) {
        using (var wr = XmlWriter.Create(ms, new XmlWriterSettings() { Encoding = Encoding.UTF8, OmitXmlDeclaration = true, Indent = true /*fn.IndexOf(@"\oldea\") < 0*/ })) root.Save(wr);
        return Encoding.UTF8.GetString(ms.ToArray());
      }
    }

    public static void adjustXsdReference(XElement root) {
      root.Attributes().Where(a => a.Name.Namespace == XNamespace.Xmlns).Remove();
      root.Attributes().Where(a => a.Name.Namespace == xsi).Remove();
      root.Add(
        new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
        new XAttribute(xsi + "noNamespaceSchemaLocation", xsdSchemaUrl)
      );
    }
    static XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

    public XElement ToElement() {
      XElement el;
      using (var ms = new MemoryStream())
      using (var wr = XmlWriter.Create(ms, new XmlWriterSettings { Indent = false })) {
        var ser = new XmlSerializer(this.GetType());
        ser.Serialize(wr, this);
        //XmlUtils.ObjectToStream(this, ms);
        ms.Seek(0, SeekOrigin.Begin);
        //var str = Encoding.UTF8.GetString(ms.ToArray());
        el = XElement.Load(ms, LoadOptions.PreserveWhitespace);
      }
      foreach (var attrs in el.Descendants("attrs").ToArray()) {
        try {
          attrs.Parent.Add(attrs.Elements().Select(a => new XAttribute(a.Attribute("name").Value, a.Attribute("value").Value)));
        } catch (Exception exp) {
          throw new Exception(attrs.Elements().First().Attribute("name").Value, exp);
        }
        attrs.Remove();
      }
      foreach (var htmlTag in el.Descendants("html-tag")) {
        var tn = htmlTag.Attribute("tag-name");
        htmlTag.Name = tn.Value;
        tn.Remove();
      }
      //cdata attr
      foreach (var a in el.Descendants().Select(d => d.Attribute("cdata")).Where(a => a != null)) {
        a.Parent.RemoveNodes();
        a.Parent.Add(new XCData("\r\n" + a.Value + "\r\n"));
        a.Remove();
      }
      if (this is body) {
        body pg = (body)this;
        el = new XElement("html", new XElement("head", new XElement("title", pg.title), new XElement("style", pg.bodyStyle)), el);
      }
      return el;
    }

    public static void oldEAImport_InsertFakeBlankChar(XElement root) {
      root.DescendantNodes().OfType<XComment>().Remove();
      foreach (var inl in root.Descendants().Where(e => isInline(e)).ToArray()) {
        //levy inline parent nebo inline previousNode
        if (
          (inl.PreviousNode != null && inl.PreviousNode is XElement && isInline((XElement)inl.PreviousNode)) ||
          (inl.PreviousNode == null && inl.Parent != null && isInline(inl.Parent))
          )
          inl.AddBeforeSelf(new XText("\r\n`"));
        //pravy inline parent
        if (
          (inl.NextNode != null && inl.NextNode is XElement && isInline((XElement)inl.NextNode)) ||
          (inl.NextNode == null && inl.Parent != null && isInline(inl.Parent))
          )
          inl.AddAfterSelf(new XText("\r\n`"));
      }
    }

    public static void normalizeXml(XElement root) {
      root.DescendantNodes().OfType<XComment>().Remove();
      var txts = root.DescendantNodes().OfType<XText>().Where(t => t.Value.IndexOf('`') >= 0).ToArray();
      if (txts == null) return;
      foreach (var txt in root.DescendantNodes().OfType<XText>().Where(t => !(t is XCData))) {
        var prevEl = txt.PreviousNode as XElement; var left = prevEl == null ? txt.Parent : prevEl; var leftInline = isInline(left);
        var rightEl = txt.NextNode as XElement; var right = rightEl == null ? txt.Parent : rightEl; var rightInline = isInline(right);
        txt.Value = blanks.Replace(txt.Value, m => trimNormalize(m.Groups["lblanks"].Value, m.Groups["cont"].Value, m.Groups["rblanks"].Value, leftInline, rightInline));
      }
      //var emptyTexts = root.DescendantNodes().OfType<XText>().Where(t => string.IsNullOrEmpty(t.Value)).ToArray();
      //var str = root.ToString(SaveOptions.DisableFormatting);
      //emptyTexts.Remove();
      //str = root.ToString(SaveOptions.DisableFormatting);
      root.DescendantNodes().OfType<XText>().Where(t => string.IsNullOrEmpty(t.Value)).Remove();
    }
    static string trimNormalize(string lblank, string cont, string rblank, bool leftInline, bool rightInline) {
      if (!string.IsNullOrEmpty(lblank)) lblank = leftInline && lblank.IndexOf('`') < 0 ? " " : "";
      if (!string.IsNullOrEmpty(rblank)) rblank = rightInline && rblank.IndexOf('`') < 0 ? " " : "";
      return lblank + cont + rblank;
    }
    static Regex blanks = new Regex(@"^(?<lblanks>(\s|`)*)(?<cont>.*?)(?<rblanks>(\s|`)*)$");

    static bool isInline(XElement el) { return inlines.Contains(el.Name.LocalName) || (el.Name.LocalName == "html-tag" && inlines.Contains(el.AttributeValue("tag-name"))); }
    static HashSet<string> inlines = new HashSet<string>() {
      "drag-target", "gap-fill", "smart-element", "smart-tag", "word-selection", 
      //https://developer.mozilla.org/en-US/docs/Web/HTML/Inline_elemente
      "b", "big", "i", "small", "tt", "abbr", "acronym", "cite", "code", "dfn", "em", "kbd", "strong", "samp", "var", "u", "s",
      "a", "bdo", "br", "img", "map", "object", "q", "script", "span", "sub", "sup", "button", "input", "label", "select", "textarea",
    };
    //https://developer.mozilla.org/en-US/docs/Web/HTML/Block-level_elements
    static HashSet<string> blocks = new HashSet<string>() {
      "address", "article", "aside", "audio", "blockquote","canvas","dd","div","dl","fieldset",
      "figcaption", "figure", "footer", "form","h1","h2","h3","h4","h5","h6","header", "hgroup", "hr","noscript"
    };
    const string spaceEntity = "&#32;";
    static HashSet<string> htmlTagAttrs = new HashSet<string> { "id", "class", "style-sheet", "parent-prop", "xsi", "xsd" };

    public static T FromElement<T>(XElement root, LoggerMemory logger = null, string url = null, bool normalize = true) where T : tag {
      var xml = new XElement(root); //kopie Root
      return (T)FromElementNoCopy(xml, logger, url, normalize);
    }

    static object StringToObject(string s, Type type) {
      if (String.IsNullOrEmpty(s)) return null;
      using (StringReader reader = new StringReader(s))
        return new XmlSerializer(type).Deserialize(reader);
    }

    public static tag FromElementNoCopy(XElement root, LoggerMemory sb = null, string url = null, bool normalize = true) {
      //prevedeni HTML5 elements a props
      var meta = Lib.courseModelJsonMLMeta;
      foreach (var el in (root.Name.LocalName == "html" ? root.Element("body") : root).DescendantsAndSelf().ToArray()) {
        jsClassMeta type; XElement attrs = null;
        //vsechny property typu (pokud typ existuje) nebu null (typ neexistuje, napr. DIV (ale ne A, IMG apod.))
        var props = meta.types.TryGetValue(el.Name.LocalName, out type) ? type.allProps : null;
        //cdata
        if (type != null && (type.st & tgSt.cdata) != 0) {
          var cd = el.Nodes().OfType<XCData>().FirstOrDefault();
          if (cd != null) { el.SetAttributeValue("cdata", cd.Value.Trim(new char[] { '\r', '\n' })); el.RemoveNodes(); }
        }
        // "id", "class", "style-sheet", "parent-prop" se vzdy v XML zachovaji, neprevadi se do attrs
        foreach (var at in el.Attributes().Where(a => !htmlTagAttrs.Contains(a.Name.LocalName) && (props == null || !props.ContainsKey(a.Name.LocalName))).ToArray()) {
          if (attrs == null) attrs = new XElement("attrs");
          //prvni pismeno atributu lower
          var nm = at.Name.LocalName; if (char.ToLower(nm[0]) != nm[0]) { var chars = nm.ToCharArray(); chars[0] = char.ToLower(chars[0]); nm = new String(chars); };
          attrs.Add(new XElement("attr", new XAttribute("name", nm), new XAttribute("value", at.Value)));
          at.Remove();
        }
        if (attrs != null) el.Add(attrs);
        if (props == null) { //udelej z elementu html-tag, napr. pro DIV (ale ne A, IMG apod.)
          el.Add(new XAttribute("tag-name", el.Name.LocalName));
          el.Name = "html-tag";
        };
      }
      //normalizace mezer
      if (normalize) normalizeXml(root);
      //deserializace
      Type tp = typeof(Lib).Assembly.GetType("CourseModel." + LowUtils.toCammelCase(root.Name.LocalName));
      try {
        object res;
        using (var rdr = root.CreateReader())
          res = new XmlSerializer(tp).Deserialize(rdr);
        //object res2 = StringToObject(xmlStr, tp);
        //extract body from html
        tag tg;
        if (res is html) {
          html ht = (html)res;
          ht.body.title = ht.head.title;
          ht.body.bodyStyle = ht.head.style;
          tg = ht.body;
        } else
          tg = (tag)res;

        return (tag)tg;
      } catch (Exception exp) {
        if (sb != null) { sb.ErrorLineFmt(url, "Cannot deserialize XML: {0}", LowUtils.ExceptionMsgToString(exp)); return null; } else throw exp;
      }
    }

    public static XElement loadExerciseXml(string fn, LoggerMemory logger) {
      bool isXsdError;
      return loadExerciseXml(fn, logger, out isXsdError);
    }
    public static XElement loadExerciseXml(string fn, LoggerMemory logger, out bool isXsdError) {
      var url = CourseMeta.ex.urlFromFileName(fn); isXsdError = false;
      if (!File.Exists(fn)) { logger.ErrorLineFmt(url, "File {0} does not exist", fn); return null; }
      using (var str = File.OpenRead(fn))
        return loadExerciseXml(str, CourseMeta.ex.urlFromFileName(fn), logger, out isXsdError);
    }

    public static XElement loadExerciseXml(Stream str, string url, LoggerMemory logger, out bool isXsdError) {

      bool locIsXE = isXsdError = false;

      Func<string, XElement> processError = (errMsg) => {
        var msg = string.Format("{0}: {1}", url, errMsg);
        if (logger == null) throw new Exception(msg);
        if (!locIsXE) logger.ErrorLine(CourseMeta.ex.urlFromFileName(url), errMsg); //XSD chyby jsou jiz do loggera zapsany, zapis pouze exception.
        return new XElement("html", new XElement("head", new XElement("title", "Error Page")), new XElement("body", new XElement("error", new XAttribute("msg", msg))));
      };

      try {
        if (!logger.strictChecking) {
          return XElement.Load(str, LoadOptions.PreserveWhitespace);
        } else {
          string errorMsg = null;
          initSchema();
          var xmlReaderSettings = new XmlReaderSettings { ValidationType = ValidationType.Schema, Schemas = xmlSchemaSet };
          xmlReaderSettings.ValidationEventHandler += (s, a) => {
            locIsXE = true;
            var msg = string.Format("Line {1}, Pos {2}: {0}", a.Exception.Message, a.Exception.LineNumber, a.Exception.LinePosition);
            if (logger != null) logger.ErrorLine(url, msg); else errorMsg += "\r\n=== " + msg;
          };
          using (XmlReader r = XmlReader.Create(str, xmlReaderSettings)) {
            errorMsg = null;
            //toto nefunguje pod .NET 4.6: https://msdn.microsoft.com/en-us/library/mt270286.aspx
            var res = XElement.Load(r, LoadOptions.PreserveWhitespace);
            var memoryLogger = logger as LoggerMemory; if (memoryLogger != null && memoryLogger.saveDumpXml) memoryLogger.dumpXml = new XElement("root", res);

            isXsdError = locIsXE;
            //nove nacteni, protoze XmlReader xmlReaderSettings.ValidationType nacte XML i s default hodnotami (napr. pro inline type).
            //to vadi pro docExample
            return !isXsdError ? (res.Descendants("doc-example").Any() ? XElement.Load(Machines.rootDir + url + ".xml", LoadOptions.PreserveWhitespace) : res) : processError(errorMsg);
          }
        }
      } catch (Exception exp) {
        return processError(LowUtils.ExceptionToString(exp));
      }
    }
    static void initSchema() {
      //AppContext.SetSwitch("System.Xml.IgnoreEmptyKeySequences", true); 
      if (xmlSchemaSet == null)
        lock (typeof(tag))
          if (xmlSchemaSet == null) {
            //schemaData = LowUtils.DownloadStr(xsdSchemaUrl);
            using (var str = LowUtils.DownloadStream(xsdSchemaUrl)) {
              var xmlSchema = XmlSchema.Read(str, null);
              xmlSchemaSet = new XmlSchemaSet();
              xmlSchemaSet.Add(xmlSchema);
              xmlSchemaSet.Compile();
            }
          }
    }
    //static string schemaData;
    static XmlSchemaSet xmlSchemaSet;
    public const string xsdSchemaUrl = "http://www.langmaster.com/new/author/coursemodelschema.xsd";
    //public const string xsdSchemaUrl = @"d:\LMCom\rew\Web4\Author\CourseModelSchema.xsd";


    //static Regex rxSpace = new Regex(@"\s{2,}", RegexOptions.Singleline);
    public static void xmlToJsonsLow(XNode nd, StringBuilder sb) {
      xmlToJsonsLow(nd, sb, Lib.courseModelJsonMLMeta);
    }

    public static void xmlToJsonsLowMeta(XNode nd, StringBuilder sb) {
      xmlToJsonsLow(nd, sb, Lib.courseMetaJsonMLMeta);
    }

    static void xmlToJsonsLow(XNode nd, StringBuilder sb, jsonMLMeta meta) {
      switch (nd.NodeType) {
        case XmlNodeType.Element:
          var el = (XElement)nd;
          sb.Append("[\""); sb.Append(meta.fromCammelCase(el.Name.LocalName)); sb.Append("\"");
          bool attrExist = false;
          foreach (var attr in el.Attributes()) {
            if (attr.IsNamespaceDeclaration) continue;
            if (!attrExist) { sb.Append(",{"); attrExist = true; } else sb.Append(",");
            sb.Append("\""); sb.Append(meta.fromCammelCase(attr.Name.LocalName)); sb.Append("\":");
            if (meta.valueTypeProps.Contains(meta.toCammelCase(el.Name.LocalName) + "." + meta.toCammelCase(attr.Name.LocalName))) sb.Append(attr.Value);
            else { sb.Append("\""); sb.Append(HttpUtility.JavaScriptStringEncode(attr.Value)); sb.Append("\""); }
          }
          foreach (var cd in el.Nodes().OfType<XCData>()) {
            if (!attrExist) { sb.Append(",{"); attrExist = true; } else sb.Append(",");
            sb.Append("\"cdata\":");
            sb.Append("\""); sb.Append(HttpUtility.JavaScriptStringEncode(cd.Value)); sb.Append("\"");
            break;
          }
          if (attrExist) sb.Append("}");
          foreach (var subNd in el.Nodes().Where(n => n.NodeType == XmlNodeType.Text || n.NodeType == XmlNodeType.Element)) {
            sb.Append(",");
            xmlToJsonsLow(subNd, sb, meta);
          }
          sb.Append("]");
          break;
        case XmlNodeType.CDATA:
          break;
        case XmlNodeType.Text:
          sb.Append("\"");
          sb.Append(HttpUtility.JavaScriptStringEncode(((XText)nd).Value));
          sb.Append("\"");
          break;
      }
    }

    public static string xmlToJsons(XElement pageXml) {
      normalizeXml(pageXml);
      StringBuilder sb = new StringBuilder();
      foreach (var ex in pageXml.Descendants("doc-example")) CourseModel.docExample.beforeXmlToJson(ex);
      xmlToJsonsLow(pageXml, sb);
      return sb.ToString();
    }

    public static string pageToJsons(CourseModel.body page) {
      foreach (var t in page.scan()) if (t.Items != null && t.Items.Length == 0) t.Items = null;
      if (!string.IsNullOrEmpty(page.bodyStyle)) page.bodyStyle = removeStyleCommentReg.Replace(page.bodyStyle, "");
      var pageXml = page.ToElement();
      try { return xmlToJsons(pageXml); } catch (Exception e) { throw new Exception(page.url, e); }
    }
    static Regex removeStyleCommentReg = new Regex(@"/\*.*\*/");

    public virtual void checkAndFinish(body pg, LoggerMemory wr) {
      //if (selfInfo.isEval && string.IsNullOrEmpty(id)) { wr.ErrorLine(" Missing id: " + GetType().Name); return; }
    }

    public IEnumerable<tag> scan(bool skipFirst = false) {
      if (!skipFirst) yield return this;
      foreach (var t in allItems()) foreach (var subT in t.scan(false)) yield return subT;
    }
    public struct scanExResult {
      public tag parent;
      public tag self;
      public int itemIdx;
    }
    //nejdrive childs, pak parent
    public IEnumerable<scanExResult> scanEx() {
      if (Items == null) yield break;
      for (int i = 0; i < Items.Length; i++) {
        yield return new scanExResult() { itemIdx = i, parent = this, self = Items[i] };
        foreach (var t in Items) foreach (var subT in t.scanEx()) yield return subT;
      }
    }
    public virtual IEnumerable<string> getExternals(body myPage) { yield break; }
    //public virtual IEnumerable<string> getCutFileUrl() { yield break; }

    public tag clone() {
      return (tag)XmlUtils.StringToObject(XmlUtils.ObjectToString(this), GetType());
    }

    public virtual IEnumerable<string> texts() { yield break; }

    //pro obohaceni starych EA dat o difotni anglicky preklad
    public virtual void modifyTexts(Func<string, string> modify) { }

  }

  public partial class seeAlsoLink {
    public static string encode(seeAlsoLink[] links) {
      if (links == null || links.Length == 0) return null;
      return links.Select(l => l.url + "|" + l.title).Aggregate((r, i) => r + "#" + i);
    }
  }

  public partial class body {
    //[XmlAttribute, DefaultValue(0), JsonIgnore]
    //public bool sitemapIgnore;
    [XmlIgnore, JsonIgnore]
    //[Doc(ignore = true)]
    public string title;

    [XmlIgnore, JsonIgnore]
    public string bodyStyle;

    public override IEnumerable<string> texts() {
      if (!string.IsNullOrEmpty(title)) yield return title;
      if (!string.IsNullOrEmpty(instrTitle)) yield return instrTitle;
      if (seeAlso != null) foreach (var sa in seeAlso) if (!string.IsNullOrEmpty(sa.title)) yield return sa.title;
    }

    public IEnumerable<NameValueString> toTransSentences(LoggerMemory logger) {
      return scan(). //kontrolky x tagy Course model stranky
        SelectMany(t => t.texts()). //texty a atributy tagu stranky
        SelectMany(t => locLib.getLocId2EnglishLoc(t, url, logger)). //extrahuj z nich {{name|value}}
        Select(nv => new NameValueString { Name = url + "/" + nv.Name, Value = nv.Value }); //preved do tradosSents
    }

    public override void modifyTexts(Func<string, string> modify) {
      if (!string.IsNullOrEmpty(title)) title = modify(title);
      if (!string.IsNullOrEmpty(instrTitle)) instrTitle = modify(instrTitle);
      if (seeAlso != null) foreach (var sa in seeAlso) if (!string.IsNullOrEmpty(sa.title)) sa.title = modify(sa.title);
    }

    public IEnumerable<string> dictSentences() {
      return scan().SelectMany(t => t.texts().Select(tt => locLib.removeLocalizedParts(tt)));
    }

    public override IEnumerable<string> getExternals(body myPage) { return externals == null ? Enumerable.Empty<string>() : externals.Split('|').Select(e => VirtualPathUtility.Combine(url, e)); }

  }

  public partial class text {
    public override IEnumerable<string> texts() { if (!string.IsNullOrEmpty(title)) yield return title; }
    public override void modifyTexts(Func<string, string> modify) { if (!string.IsNullOrEmpty(title)) title = modify(title); }
  }

  public partial class list : macro, IMacroTemplate {
    public override void checkAndFinish(body pg, LoggerMemory wr) {
      base.checkAndFinish(pg, wr);
      if (!allItems().Any()) { wr.ErrorLine(pg.url, " Empty Items"); return; }
      if (Items.OfType<htmlTag>().Where(ht => ht.tagName == htmlTag.li).Count() != Items.Length) { wr.ErrorLine(pg.url, " Only 'li' child tag alowed"); return; }
    }

    IEnumerable<tag> IMacroTemplate.Generate(body pg, LoggerMemory wr) {
      if (Items == null) yield break;
      var selfReplace = htmlTag.create(htmlTag.ul, "class", "fa-ul" + (isStriped ? " oli-striped-list" : null));
      //List<tag> its = new List<tag>();
      int cnt = 0;
      Func<string> iconText = () => { cnt++; return (icon == listIcon.number ? cnt.ToString() : Convert.ToChar(Convert.ToInt32(icon == listIcon.upperLetter ? 'A' : 'a') + cnt - 1).ToString()) + delim; };
      tag firstTag;
      foreach (var li in Items) {
        //i i;
        switch (icon) {
          case listIcon.number:
          case listIcon.letter:
          case listIcon.upperLetter:
            firstTag = htmlTag.create(htmlTag.i, "class", "fa-li" + " text-" + color + " lm-text-list", new text() { title = iconText() });
            break;
          default:
            firstTag = htmlTag.create(htmlTag.i, "class", "fa fa-li fa-" + LowUtils.fromCammelCase(icon.ToString()) + " text-" + color);
            break;
        }
        li.Items = XExtension.Create(firstTag).Concat(li.Items).ToArray();
        //li.Items = items.Count==0 li.Items == null ? new tag[] { i } : XExtension.Create<tag>(i).Concat(li.Items).ToArray();
      }
      selfReplace.Items = this.Items;
      yield return selfReplace;
    }
  }

  public partial class pairing {
    public override void checkAndFinish(body pg, LoggerMemory wr) {
      base.checkAndFinish(pg, wr);
      if (!allItems().Any()) {
        wr.ErrorLine(pg.url, "Empty Items: " + id); return;
      } else if (allItems().OfType<pairingItem>().Count() != allItems().Count()) {
        wr.ErrorLine(pg.url, " Only PairingItem in Pairing alowed: " + id); return;
      }
    }
  }

  public partial class offering {
    public override void checkAndFinish(body pg, LoggerMemory wr) {
      base.checkAndFinish(pg, wr);
      string editIds = null;
      if (mode != offeringDropDownMode.gapFillIgnore) {
        var allEdits = pg.scan().OfType<edit>().ToArray();
        edit[] myEdits = allEdits.Where(e => e.offeringId == id).ToArray(); //my gapfills nebo dropdowns
                                                                            //hack pro nejjednodussi cviceni, obsahujici pouze jedno offering - vsechny dropDowns a gapfill dej pod offering
        if (myEdits.Length == 0) {
          foreach (var ed in allEdits.Where(e => string.IsNullOrEmpty(e.offeringId))) ed.offeringId = id;
          myEdits = allEdits.Where(e => e.offeringId == id).ToArray(); //my gapfills nebo dropdowns
        }
        //neprazdne correct values
        if (myEdits.SelectMany(ed => ed.correctValue.Split('|')).Any(v => v == string.Empty)) { wr.ErrorLine(pg.url, " drop-down or gap-fill assigned to offering must have all corrrect values not empty, offering.id=" + id); return; }
        //mix?
        var gapFills = myEdits.OfType<gapFill>().Count();
        if (gapFills > 0 && gapFills != myEdits.Length) { wr.ErrorLine(pg.url, " gap-fill x drop-down mix for offering.id=" + id); return; } //mix => error
                                                                                                                                             //dropdown check
        if (myEdits.OfType<dropDown>().Any()) {
          if (mode == offeringDropDownMode.dropDownKeep) foreach (var dd in myEdits.Cast<dropDown>()) dd.gapFillLike = true;
          else {
            if (!string.IsNullOrEmpty(words)) { wr.ErrorLine(pg.url, " Both words and drop-down assigned to offering with drop-down-mode=discard: " + id); return; }
            var notFakes = myEdits.Where(ed => !ed.correctValue.StartsWith(edit.fakeEdit));
            if (notFakes.Select(dd => dd.correctValue).Distinct().Count() != notFakes.Count()) { wr.ErrorLine(pg.url, "drop-down assigned to offering with drop-down-mode=discard must have different correct values: " + id); return; }
          }
        }
        //K cemu je omezeni jedne eval grupy na offering?
        //kontrola evalGroup
        //var evGroups = myEdits.Where(e => e.evalGroup != null).Select(e => e.evalGroup).ToArray();
        //if (evGroups.Length > 0 && (evGroups.Length != evGroups.Length || evGroups.Distinct().Count() != 1)) { wr.ErrorLine(pg.url, " all offering members must have the same eval-group attribute value" + id); return; }
        ////none evalGroup => dosad offering ID
        //if (evGroups.Length == 0) foreach (var ed in myEdits) ed.evalGroup = "_ofg" + id; 

        editIds = myEdits.Select(e => "#" + e.id).DefaultIfEmpty().Aggregate((r, i) => r + "|" + i);
      }
      words = words + (!string.IsNullOrEmpty(words) && !string.IsNullOrEmpty(editIds) ? "|" : null) + editIds;
      if (string.IsNullOrEmpty(words)) { wr.ErrorLine(pg.url, " Missing offering Words: " + id); return; }
    }
  }

  public partial class edit {
    public override void checkAndFinish(body pg, LoggerMemory wr) {
      base.checkAndFinish(pg, wr);
      if (!isSkipEvaluation() && !isReadOnly() && correctValue == null) { wr.ErrorLine(pg.url, " Missing GapFillLike CorrectValue: " + id); return; }
    }
    public override int getMaxScore() { return correctValue.StartsWith(fakeEdit) ? 0 : base.getMaxScore(); }
    public const string fakeEdit = "???ignore???";
  }

  public partial class dropDown : edit {
    public override void checkAndFinish(body pg, LoggerMemory wr) {
      base.checkAndFinish(pg, wr);
      if (correctValue != null && correctValue.Split('|').Length > 1) { wr.ErrorLine(pg.url, " Single possibility for DragTarget CorrectValue required: " + id); return; }
    }
    public static dropDown createFromField(inlineFld fld) {
      return new dropDown() { correctValue = fld.inlineFieldValues() };
    }
  }

  public partial class dragTarget : edit {
    public override void checkAndFinish(body pg, LoggerMemory wr) {
      base.checkAndFinish(pg, wr);
      if (correctValue != null && correctValue.Split('|').Length > 1) { wr.ErrorLine(pg.url, " Single possibility for DragTarget CorrectValue required: " + id); return; }
    }
    public static dragTarget createFromField(inlineFld fld) {
      return new dragTarget() { correctValue = fld.inlineFieldValues() };
    }
    //obsolete
    public static dropDown createFromField(inlineField fld) {
      return new dropDown() { correctValue = fld.inlineFieldValues() };
    }
  }

  public partial class gapFill : edit {
    public override void checkAndFinish(body pg, LoggerMemory wr) {
      base.checkAndFinish(pg, wr);
    }
    public static gapFill createFromField(inlineFld fld) {
      return new gapFill() { correctValue = fld.inlineFieldValues() };
    }
    public static gapFill createFromField_Correction(inlineFld fld) {
      return new gapFill() { initValue = fld.Values.Length == 0 ? null : fld.Values[0], correctValue = fld.inlineFieldValues(1) };
    }
    //OBSOLETE
    public static gapFill createFromField(inlineField fld) {
      return new gapFill() { correctValue = fld.inlineFieldValues() };
    }
    //OBSOLETE
    public static gapFill createFromField_Correction(inlineField fld) {
      return new gapFill() { initValue = fld.Values.Length == 0 ? null : fld.Values[0], correctValue = fld.inlineFieldValues(1) };
    }

    public static Dictionary<int, char> gaffFill_normTable = new Dictionary<int, char>() {
      {1040, 'A'},{1072,'a'},{1042,'B'},{1074,'b'},{1045,'E'},{1077,'e'},{1050,'K'},{1082,'k'},{1052,'M'},{1084,'m'},{1053,'H'},{1085,'h'},{1054,'O'},{1086,'o'},
      {1056,'P'},{1088,'p'},{1057,'C'},{1089,'c'},{1058,'T'},{1090,'t'},{1059,'Y'},{1091,'y'},{1061,'X'},{1093,'x'},{1105,'ë'},{161,'!'},{160,' '},{191,'?'},{241,'ň'},
      {39,'’'},{96,'’'},{180,'’'},{733,'"'},{8216,'’'},{8219,'’'},{8220,'"'},{8221,'"'},{8222,'"'},{8242,'’'},{8243,'"'}
    };
    static bool relevantChars(char ch) {
      char nw;
      if (gaffFill_normTable.TryGetValue(Convert.ToInt32(ch), out nw)) ch = nw;
      return char.IsLetterOrDigit(ch);
    }
    //algoritmus musi byt stejny s d:\LMCom\rew\Web4\Courses\Course.ts, export function normalize(
    public static string normalize(string value, bool caseSensitive) {
      if (string.IsNullOrEmpty(value)) return value;
      if (!caseSensitive) value = value.ToLower();
      var chars = value.ToCharArray(); var res = new List<char>();
      var st = 0; //0..zacatek, 1..no space, 2..space 
      var charsNum = 0; var otherNum = 0;
      for (var i = 0; i < chars.Length; i++) {
        var ch = chars[i];
        switch (st) {
          case 0: if (!relevantChars(ch)) { otherNum++; continue; } st = 1; charsNum++; res.Add(ch); break; //mezery na zacatku
          case 1: if (relevantChars(ch)) { charsNum++; res.Add(ch); continue; } otherNum++; st = 2; break; //nemezery 
          case 2: if (!relevantChars(ch)) { otherNum++; continue; } st = 1; res.Add(' '); res.Add(ch); break; //mezery uprostred
        }
      }
      if (charsNum <= 2 && otherNum >= charsNum) return value;
      return new string(res.ToArray());
    }
  }

  public partial class singleChoice : IMacroTemplate {

    public override void checkAndFinish(body pg, LoggerMemory wr) {
      base.checkAndFinish(pg, wr);
      foreach (var rad in scan().OfType<radioButton>()) {
        rad.evalGroup = "and-" + id;
        rad.scoreWeight = scoreWeight == 0 ? 100 : scoreWeight;
        rad.evalButtonId = evalButtonId;
        rad.skipEvaluation = skipEvaluation;
        rad.readOnly = readOnly;
      }
    }

    IEnumerable<tag> IMacroTemplate.Generate(body pg, LoggerMemory wr) {
      //12.5.2015: kontroly presunuty do postProcess_Evals.finishEvGrp, if (evTypes.Any(t => t == typeof(radioButton))) {
      //if (Items == null || Items.Length < 2) { wr.ErrorLineFmt(pg.url, "At least 2 radioButtons required (id={0})", id); yield break; }
      //if (Items.Cast<radioButton>().Where(r => r.correctValue).Count() != 1) wr.ErrorLineFmt(pg.url, "Just one radio-button with correct-value==true required (id={0})", id);
      var table = htmlTag.create(htmlTag.table, "class", "table oli-table-checkitem-list", Items.Select(it => htmlTag.create(htmlTag.tr, htmlTag.create(htmlTag.td, it))));
      Items = new tag[] { table };
      temporaryMacroItem = true;
      yield return this;
    }

  }

  public partial class wordSelection {
    public override void checkAndFinish(body pg, LoggerMemory wr) {
      base.checkAndFinish(pg, wr);
      if (string.IsNullOrEmpty(words)) { wr.ErrorLine(pg.url, "words attribute required: " + id); return; }
      words = trimWords.Replace(words.Trim(), "|");
      if (evalGroup == null) checkWords(XExtension.Create(words), id, pg, wr); //kontrola wordSelection bez and eval grupy
    }
    public static void checkWords(IEnumerable<string> wordss, string id, body pg, LoggerMemory wr) {
      string[] parts = wordss.SelectMany(words => words.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries)).ToArray();
      if (parts.Length < 2) { wr.ErrorLine(pg.url, "At least 2 selections required: " + id); return; }
      if (parts.Where(p => p.StartsWith("#")).Count() != 1) { wr.ErrorLine(pg.url, "Single correct value (marked with #) required: " + id); return; }
    }
    //public override void checkAndFinish(TagStatic info, Tag pg, List<Tag> controls, LoggerObj wr, out Tag selfReplace) {
    //  base.checkAndFinish(info, pg, controls, wr, out selfReplace);
    //if (allItems().Any()) { wr.ErrorLine("Not empty Items: " + id); return; }
    //if (Words == null) { wr.ErrorLine("missing Words: " + id); return; }
    //Data = Words.Split('|');
    //checkAndFinishLow(info, pg, controls, wr);
    //}
    public static wordSelection createFromField(inlineFld fld) {
      return new wordSelection() { words = fld.inlineFieldValues() };
    }
    //OBSOLETE
    public static wordSelection createFromField(inlineField fld) {
      return new wordSelection() { words = fld.inlineFieldValues() };
    }

    //protected override void setTagItems(object[] itemsValue) { } //noop, jinak se vymaou Items, nastavene v Words setteru
    //protected override object[] getTagItems() { return null; }
    static Regex trimWords = new Regex(@"\s*\|\s*", RegexOptions.Singleline);
  }

  public partial class ttsSound {
    //MP3 soubor je v tts\<page id>_<id>.mp3
    public static ttsSound createFromField(inlineFld fld) {
      return new ttsSound() { text = fld.Values.Length > 0 ? fld.Values[0] : null };
    }
  }

  /****************** SOUND ***************************/

  //Predchudce vsech vizuelnich media kopntrolek. Items MediaTag obsahuji MediaSentReplace
  public partial class mediaTag : /*IIncludeHolder,*/ IMacroTemplate {

    IEnumerable<tag> IMacroTemplate.Generate(body pg, LoggerMemory sb) {
      checkAndAdjustUrls(pg, sb, false);
      //checkAndAdjustMediaUrl(pg, ref audioUrl, ref videoUrl, sb, true);
      if (!string.IsNullOrEmpty(anyUrl)) {
        file = new cutText {
          //audioUrl = audioUrl,
          mediaUrl = mediaUrl,
          Items = new tag[] { new phrase { begPos = 0, endPos = -1 } },
        };
      } else if (!string.IsNullOrEmpty(cutUrl)) {
        file = include.loadCutUrl(pg, cutUrl, sb);
      } else {
        var inner = file as _sndFile;
        if (inner != null) {
          if (!passiveTag()) inner.checkAndAdjustUrls(pg, sb, false);
          if (inner.file != null) {
            inner.processInclude(pg, sb);
          }
        } else {
          var inc = file as include;
          if (inc != null) file = inc.expand(pg, sb);
        }
      }
      yield return this;
    }
    static HashSet<string> validExts = new HashSet<string>() { "mp4", "mp3", "webm" };

    public virtual void addSequence(_sndFile file, bool isPassive, IEnumerable<int> seq) { }

  }

  public partial class _sndGroup : tag {
    [XmlIgnore, JsonIgnore]
    public bool isPassive;
  }
  //public partial class _sndFileGroup {
  //  public override IEnumerable<string> getExternals(body myPage) {
  //    //audio
  //    if (!isVideo) {
  //      if (string.IsNullOrEmpty(audioUrl)) yield break;
  //      yield return Path.ChangeExtension(audioUrl, ".mp3");
  //      yield break;
  //    }
  //    //video

  //    var exts = this.videoFormat.Split(':')[1].Split('|').Select(p => p.Split('-')[1]).SelectMany(es => es.Split(','));
  //    foreach (var ext in exts) yield return Path.ChangeExtension(audioUrl, "." + ext);
  //  }
  //}

  public partial class recording : humanEval {
    //public override IEnumerable<string> getExternals(body myPage) { if (string.IsNullOrEmpty(mediaUrl)) yield break; else yield return mediaUrl; }
  }

  public partial class img {
    public static img createFromField(inlineFld fld) {
      return null;
      //img res2 = new img();
      //res2.src = fld.prop("src");
      //if (res2.src != null) return res2;
      //res2.id = fld.prop("id");
      //if (res2.id != null) return res2;
      //res2.src = "img/" + 
    }
    public override IEnumerable<string> getExternals(body myPage) { if (string.IsNullOrEmpty(src)) yield break; else yield return src; }

  }

  //public partial class script {
  //  //[XmlElement("Data"), JsonIgnore]
  //  //public System.Xml.XmlCDataSection ExampleCDATA {
  //  //  get { return new System.Xml.XmlDocument().CreateCDataSection(Data); }
  //  //  set { Data = value.Value; }
  //  //}
  //}

  public static class doc {

    static Type[] allTagTypes = TagStatic.allTagTypes().ToArray();
    static Dictionary<string, Type> allTagTypesDir = allTagTypes.ToDictionary(t => t.Name, t => t);


    //Pouzita v d:\LMCom\rew\Web4\Author\Author.aspx.cs, dokumentace a xref
    //Aktualizace: prelozit ObjectModel.csproj, prelozit SchemaDefinition.csproj
    public static string mapWithDoc() {
      return CourseModel.tag.xmlToJsons(XElement.Load(Machines.rootPath + @"Author\tagsMeta.xml"));
    }

    //volana pri prekladu SchemaDefinition.csproj. Vytvati tagsMeta.xml, obsahujici mapu typu a properties s dokumentaci
    public static void export() {
      export(@"d:\lmcom\rew\schemadefinition\schemadefinition.xml", @"d:\LMCom\rew\Web4\Author\tagsMeta.xml");
    }

    static void export(string CSharpFn, string resFn) {
      var metaObj = createTagsMeta();
      //scharp doc se merguje do XML, nikoliv do map objektu
      if (CSharpFn != null) {
        var metaXml = metaObj.ToElement(); // XElement.Parse(XmlUtils.ObjectToString(metaObj));
        mergeTagsMetaWithCSharpDoc(metaXml, CSharpFn);
        metaObj = (docTagsMeta)tag.FromElementNoCopy(metaXml); //  XmlUtils.StringToObject<docTagsMeta>(metaXml.ToString());
      }
      metaObj.ToElement().Save(resFn);
      //LMNetLib.XmlUtils.ObjectToFile(resFn, metaObj);
    }

    static docTagsMeta createTagsMeta() {
      Dictionary<string, Type> enums = new Dictionary<string, Type>(); //evidence vsech enum props
      List<docProp> props = new List<docProp>(); //evidence vsech props
      Dictionary<string, string> takePropFrom = new Dictionary<string, string>(); //evidence vsech nahrad properties
      var res = new docTagsMeta {
        types = allTagTypes.Select(t => createType(t, enums, props, takePropFrom)).OrderBy(t => t.name).ToList(),
        enums = enums.Values.Select(tp => createEnum(tp)).ToList(),
        props = props.OrderBy(p => p.name).ToList()
      };
      //pro JS runtime: vsechny property
      var ownersProps = props.GroupBy(p => p.ownerType).ToDictionary(g => g.Key, g => g.Select(p => p.name).ToArray()); //property owneru
      foreach (var tp in res.types) { //pro kazdy typ vytahni z ancestor-and-self jeho properties
        List<string> propIds = new List<string>();
        foreach (var anc in ancestorAndSelf(tp.name)) {
          string[] ancProps;
          if (!ownersProps.TryGetValue(anc, out ancProps)) continue;
          string sameAs;
          propIds.AddRange(ancProps.Select(pr => takePropFrom.TryGetValue(anc + "." + pr, out sameAs) ? sameAs + "." + pr : anc + "." + pr));
        }
        tp.myProps = propIds.ToArray();
      }
      return res;
    }

    static Tuple<string, string> parseJSDoc(XText txt) {
      string jsDoc = txt == null ? "" : txt.Value; string summary = null; string content = null;
      //XElement summary = new XElement("summary"); //, new XAttribute(tag._parentProp, parentProps.summary.ToString()));
      var st = 0;
      //not empty lines
      foreach (var line in jsDoc.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l))) {
        if (line.StartsWith("@summary")) {
          st = 1; summary = line.Substring(8).TrimStart();
        } else if (line.StartsWith("@descr")) {
          st = 0; content += line.Substring(6).TrimStart();
        } else if (st == 0)
          summary += " " + line;
        else if (st == 1)
          content += line;
      }
      return new Tuple<string, string>(summary, content);
    }
    static void mergeTagsMetaWithCSharpDoc(XElement metaXml, string CSharpFn) {
      var mems = XElement.Load(CSharpFn).Element("members").Elements("member");
      //summary
      //foreach (var sum in mems.SelectMany(m => m.Elements("summary"))) sum.Add(new XAttribute(tag._parentProp, parentProps.summary.ToString()));
      Dictionary<string, Tuple<string, string>> members = mems.ToDictionary(m => m.Attribute("name").Value, m => parseJSDoc(m.Nodes().OfType<XText>().FirstOrDefault()));

      foreach (var tp in metaXml.Elements("doc-type")) {
        insertCSharpDoc(tp, "T:CourseModel." + tp.Attribute("name").Value, members);
      }
      foreach (var tp in metaXml.Elements("doc-prop")) {
        var propName = tp.Attribute("owner-type").Value + "." + tp.Attribute("name").Value;
        insertCSharpDoc(tp, "F:CourseModel." + propName, members);
        insertCSharpDoc(tp, "P:CourseModel." + propName, members);
      }
      foreach (var tp in metaXml.Elements("doc-enum")) {
        foreach (var en in tp.Elements())
          insertCSharpDoc(en, "F:CourseModel." + tp.Attribute("name").Value + "." + en.Attribute("name").Value, members);
        insertCSharpDoc(tp, "T:CourseModel." + tp.Attribute("name").Value, members);
      }
    }

    static void insertCSharpDoc(XElement tg, string key, Dictionary<string, Tuple<string, string>> members) {
      Tuple<string, string> els;
      if (!members.TryGetValue(key, out els)) return;
      //foreach (var txt in els.OfType<XText>().Concat(els.OfType<XElement>().SelectMany(e => e.DescendantNodes().OfType<XText>())).Where(t => !tag.preLike.Contains(t.Parent.Name.LocalName))) txt.Value = rxSpaces.Replace(rxSpace.Replace(txt.Value, " "), " ");
      if (!string.IsNullOrEmpty(els.Item1)) tg.Add(new XAttribute("summary", els.Item1));
      if (!string.IsNullOrEmpty(els.Item2)) tg.Add(new XCData(els.Item2));
    }
    static Regex rxSpace = new Regex(@"\s", RegexOptions.Singleline);
    static Regex rxSpaces = new Regex(@"\s{2,}", RegexOptions.Singleline);

    static docType createType(Type tp, Dictionary<string, Type> enums, List<docProp> props, Dictionary<string, string> takePropFrom) {
      var res = new docType {
        name = stripName(tp.FullName),
        //isHtml = isHtml(tp),
        isIgn = isIgn(tp),
        descendantsAndSelf = allTagTypes.Where(t => t == tp || t.IsSubclassOf(tp)).Select(t => t.Name).ToArray(),
      };
      registerTypeProps(res, tp, enums, props, takePropFrom);
      return res;
    }

    static docEnum createEnum(Type tp) {
      return new docEnum { name = stripName(tp.FullName), enums = Enum.GetValues(tp).Cast<object>().Select(en => new docEnumItem { name = en.ToString() }).ToList() };
    }

    static void registerTypeProps(CourseModel.docType type, Type tp, Dictionary<string, Type> enums, List<docProp> propDict, Dictionary<string, string> takePropFrom) {
      var ownPropInfos = tp.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).Where(p => !isIgn(p) && p.CanRead && p.CanWrite).Select(propMember => new { propMember = (MemberInfo)propMember, propType = propMember.PropertyType }).Concat(
        tp.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).Where(f => !isIgn(f)).Select(propMember => new { propMember = (MemberInfo)propMember, propType = propMember.FieldType })).ToArray();
      foreach (var propInfo in ownPropInfos) {
        //var sa = sameAs(propInfo.propMember);
        //if (sa != null) takePropFrom[tp.Name + "." + propInfo.propMember.Name] = sa;
        propDict.Add(finishProp(new docProp { name = propInfo.propMember.Name, ownerType = tp.Name }, propInfo.propMember, propInfo.propType, enums));
      }
    }

    //static bool isIgn(MemberInfo mem) { DocAttribute da = (DocAttribute)mem.GetCustomAttributes(typeof(DocAttribute), true).FirstOrDefault(); return da != null && da.ignore; }
    //static bool isHtml(MemberInfo mem) { DocAttribute da = (DocAttribute)mem.GetCustomAttributes(typeof(DocAttribute), true).FirstOrDefault(); return da != null && da.isHtml; }
    //static bool xrefValued(MemberInfo mem) { DocAttribute da = (DocAttribute)mem.GetCustomAttributes(typeof(DocAttribute), true).FirstOrDefault(); return da != null && da.xrefValued; }
    //static bool xrefIgnore(MemberInfo mem) { DocAttribute da = (DocAttribute)mem.GetCustomAttributes(typeof(DocAttribute), true).FirstOrDefault(); return da != null && da.xrefIgnore; }
    //static string sameAs(MemberInfo mem) { DocAttribute da = (DocAttribute)mem.GetCustomAttributes(typeof(DocAttribute), true).FirstOrDefault(); return da != null && !string.IsNullOrEmpty(da.copyDocFrom) ? da.copyDocFrom : null; }

    static bool isIgn(MemberInfo mem) { tgAtAttribute da = (tgAtAttribute)mem.GetCustomAttributes(typeof(tgAtAttribute), true).FirstOrDefault(); return da != null && da.isSt(tgSt.docIgnore); }
    //static bool isHtml(MemberInfo mem) { tgAtAttribute da = (tgAtAttribute)mem.GetCustomAttributes(typeof(tgAtAttribute), true).FirstOrDefault(); return da != null && da.isSt(tgSt.docIsHtml); }
    //static bool xrefValued(MemberInfo mem) { DocAttribute da = (DocAttribute)mem.GetCustomAttributes(typeof(DocAttribute), true).FirstOrDefault(); return da != null && da.isSt(tgSt.docIgnore); }
    //static bool xrefIgnore(MemberInfo mem) { tgAtAttribute da = (tgAtAttribute)mem.GetCustomAttributes(typeof(tgAtAttribute), true).FirstOrDefault(); return da != null && da.isSt(tgSt.docXrefIgnore); }
    //static string sameAs(MemberInfo mem) { DocAttribute da = (DocAttribute)mem.GetCustomAttributes(typeof(DocAttribute), true).FirstOrDefault(); return da != null && !string.IsNullOrEmpty(da.copyDocFrom) ? da.copyDocFrom : null; }

    static string stripName(string s) { return s.Substring("CourseModel.".Length); }
    static string[] stripDescendants(string[] s) { return s.Length == 0 ? null : s; }

    static CourseModel.docProp finishProp(CourseModel.docProp prop, MemberInfo propMember, Type propType, Dictionary<string, Type> enums) {
      //prop.xrefValued = false /*xrefValued(propMember)*/; prop.xrefIgnore = xrefIgnore(propMember); prop.isHtml = isHtml(propMember);
      if (propType.IsEnum && propType.FullName.StartsWith("CourseModel.")) {
        enums[propType.Name] = propType; prop.dataType = "@enum:" + propType.Name; //prop.xrefValued = true;
      } else if (propType.IsArray && propType.GetElementType().FullName.StartsWith("CourseModel."))
        prop.dataType = "@arrayof:" + propType.GetElementType().Name;
      else if (propType.FullName.StartsWith("CourseModel."))
        prop.dataType = "@of:" + propType.Name;
      else if (propType.IsArray && propType.GetElementType().FullName.StartsWith("System."))
        prop.dataType = "@arraysys:" + propType.GetElementType().Name;
      else if (propType.FullName.StartsWith("System."))
        prop.dataType = "@sys:" + propType.Name.Replace("Int32", "Number");
      else
        prop.dataType = "????"; // tp.FullName;
      return prop;
    }

    static IEnumerable<string> ancestorAndSelf(string tp) {
      while (true) {
        yield return tp;
        tp = allTagTypesDir[tp].BaseType.Name;
        if (!allTagTypesDir.ContainsKey(tp)) break;
      }
    }

    static string fromCammelCase(string nm) {
      List<char> res = new List<char>(); bool first = true;
      foreach (var ch in nm) {
        if (char.IsUpper(ch)) { if (!first) res.Add('-'); first = false; res.Add(char.ToLower(ch)); } else res.Add(ch);
        first = false;
      }
      return new string(res.ToArray());
    }

    public static string toCammelCase(string nm) {
      List<char> res = new List<char>(); var toUpper = false;
      foreach (var ch in nm) {
        if (ch == '-')
          toUpper = true;
        else if (toUpper) {
          res.Add(char.ToUpper(ch)); toUpper = false;
        } else
          res.Add(ch);
      }
      return new string(res.ToArray());
    }

    //static XElement xmlToCodeHtml(XElement xml) {
    //  var res2 = new XElement("pre", new XAttribute("class", "prettyprint lang-html"), xml.ToString());
    //  return res2;
    //}
  }

  public partial class HumanEvalResult : IAdjustNetDates {
    [XmlIgnore, JsonIgnore]
    public DateTime hDateNet;
    public void adjustNetDates() { hDateNet = LowUtils.numToDate(hDate); }
  }


}

