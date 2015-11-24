using CourseModel;
using LMComLib;
using LMNetLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

//generuje JSON s metainformacemi pro ciste JS zadavani dat (bez XML)
namespace metaJS {

  //************** MODEL
  public partial class xsd {
    public Dictionary<string, xsdType> types;
    public xsdProp[] properties;
    public Dictionary<string, xsdEnum> enums;
  }
  public partial class xsdLow {
    public string name;
    public string summary;
    public string descr;
    public tgSt flag;
    public string _newName;
  }
  public partial class xsdType : xsdLow {
    public string ancestor;
    public xsdInheritsFrom inheritsFrom;
    public bool required;
  }
  public partial class xsdEnum : xsdLow {
    public xsdEnumItem[] enumData;
  }
  public partial class xsdEnumItem : xsdLow {
    public int value;
  }
  public enum xsdPropType { Number, String, Enum, Bool, Class }
  public enum xsdPropModifier { no, Array, ArrayArray, Dict }
  public enum xsdInheritsFrom { tag, eval, media }
  public enum xsdPropConstrains { no, regex, id, idref, intNumber, ncname }

  public partial class xsdProp : xsdLow {
    public string propOf;
    public xsdPropType type;
    public xsdPropModifier modifier;
    public string clsEnumName; //name pro type=class nebo type=enum
    public xsdPropConstrains constrains;
    public string regexConstrains;
  }

  //************** CODE
  //************** generace meta dat
  public partial class xsd {
    public xsd() {
      //dokumentace, generovana pri prekladu ObjectModel
      XElement doc = XElement.Load(@"d:\LMCom\rew\ObjectModel\bin\Debug\ObjectModel.XML");
      var ts = getAll.Types().Where(t => t == typeof(tag) || t.IsSubclassOf(typeof(tag)));
      types = ts.Select(t => new xsdType(t, doc)).ToDictionary(tr => tr.name);
      var enumTypes = new HashSet<Type>();
      var props = ts.
        SelectMany(tp =>
          tp.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).
          Where(f => checkAttrs(f)).
          Select(f => new xsdProp(f, f.FieldType, enumTypes, doc)).
          Concat(
            tp.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).
            Where(f => checkAttrs(f)).
            Select(f => new xsdProp(f, f.PropertyType, enumTypes, doc))));
      var debug = props.GroupBy(p => p.propOf + "." + p.name).Where(g => g.Count() > 1).ToArray();
      properties = props.ToArray();
      enums = enumTypes.Select(t => new xsdEnum(t, doc)).ToDictionary(e => e.name);
    }
    public string toJson() {
      return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
    public void toJson(string fn) {
      File.WriteAllText(fn, toJson());
    }
    bool checkAttrs(MemberInfo member) {
      var jsonIgnore = member.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Any();
      var jsonGenOnly = member.GetCustomAttributes(typeof(JsonGenOnlyAttribute), false).Any();
      if (jsonIgnore) {
        var st = tgAtAttribute.fromTag(member);
        //neignoruj ty, co maji dokumentaci i kdyz jsou JSONIgnore
        if (st != null && !st.isSt(tgSt.docIgnore)) return true;
      }
      return !jsonIgnore || jsonGenOnly;
    }
  }

  public partial class xsdLow {
    public void setDoc(XElement doc, string name, string subName = null) {
      var memDoc = doc.Element("members").Elements().FirstOrDefault(el => el.AttributeValue("name").Split(':')[1] == "CourseModel." + name + (subName == null ? null : "." + subName));
      if (memDoc == null) return;
      var sum = memDoc.Element("summary"); summary = sum == null ? null : sum.Value.Trim();
      var des = memDoc.Nodes().OfType<XText>().Select(n => n.Value).DefaultIfEmpty().Aggregate((r, i) => r + "\n" + i); descr = string.IsNullOrEmpty(des) ? null : des.Trim();
    }
  }

  public partial class xsdType {
    public xsdType(Type t, XElement doc) {
      var attr = tgAtAttribute.fromTag(t);
      if (attr != null) {
        flag = attr == null ? 0 : attr.st;
        _newName = attr._oldName;
      }
      name = LowUtils.fromCammelCase(t.Name); ancestor = t.Name == "tag" ? null : LowUtils.fromCammelCase(t.BaseType.Name);
      required = attr != null && attr.isSt(tgSt.xsdRequiredAttr);
      if (t.IsSubclassOf(typeof(evalControl))) inheritsFrom = xsdInheritsFrom.eval;
      else if (t.IsSubclassOf(typeof(mediaTag))) inheritsFrom = xsdInheritsFrom.media;
      setDoc(doc, t.Name);
    }
  }

  public partial class xsdEnum {
    public xsdEnum(Type t, XElement doc) {
      var attr = tgAtAttribute.fromTag(t);
      if (attr != null) {
        flag = attr == null ? 0 : attr.st;
        _newName = attr._oldName;
      }
      name = LowUtils.fromCammelCase(t.Name);
      enumData = Enum.GetValues(t).Cast<object>().Select(v => new xsdEnumItem(t, v, doc)).ToArray();
      setDoc(doc, t.Name);
    }
  }

  public partial class xsdEnumItem : xsdLow {
    public xsdEnumItem(Type enumtype, object val, XElement doc) {
      var attr = tgAtAttribute.fromTag(enumtype.GetField(val.ToString()));
      if (attr != null) {
        flag = attr == null ? 0 : attr.st;
        _newName = attr._oldName;
      }
      name = LowUtils.fromCammelCase(val.ToString()); value = (int)val;
      setDoc(doc, enumtype.Name, name);
    }
  }


  public partial class xsdProp {
    public xsdProp(MemberInfo member, Type t, HashSet<Type> enums, XElement doc) {
      setDoc(doc, member.DeclaringType.Name, member.Name);
      tgAtAttribute attr = tgAtAttribute.fromTag(member);
      if (attr == null) attr = new tgAtAttribute(0);
      flag = attr.st;
      _newName = attr._oldName;
      name = LowUtils.fromCammelCase(member.Name); propOf = LowUtils.fromCammelCase(member.DeclaringType.Name);
      if (nulableType(t)) t = Nullable.GetUnderlyingType(t);
      var dictOf = stringDictOf(t);
      if (dictOf != null) { modifier = xsdPropModifier.Dict; t = dictOf; } else {
        bool arrayOfArray;
        var arrOf = arrayOf(t, out arrayOfArray);
        if (arrOf != null) { modifier = arrayOfArray ? xsdPropModifier.ArrayArray : xsdPropModifier.Array; t = arrOf; }
      }
      type = getType(t);
      switch (type) {
        case xsdPropType.Enum:
        case xsdPropType.Class: clsEnumName = t.Namespace == "CourseModel" ? LowUtils.fromCammelCase(t.Name) : t.FullName; break;
      }
      if (type == xsdPropType.Enum) enums.Add(t);
      //constrains
      if (!string.IsNullOrEmpty(attr.xsdType))
        switch (attr.xsdType) {
          case "xs:ID": constrains = xsdPropConstrains.id; break;
          case "xs:IDREF": constrains = xsdPropConstrains.idref; break;
          case "xs:NCName": constrains = xsdPropConstrains.ncname; break;
          case "xs:int": constrains = xsdPropConstrains.intNumber; break;
          case evalControl.xsdType_eval_group: constrains = xsdPropConstrains.regex; regexConstrains = evalControl.xsdType_eval_group_regex; break;
          case phraseReplace.sndReplace_sndDialogIdx: constrains = xsdPropConstrains.regex; regexConstrains = phraseReplace.sndReplace_sndDialogIdx_regex; break;
          case urlTag.mediaTag_format: constrains = xsdPropConstrains.regex; regexConstrains = urlTag.mediaTag_format_regex; break;
          case _sndFile.mediaTag_sequence: constrains = xsdPropConstrains.regex; regexConstrains = _sndFile.mediaTag_sequence_regex; break;
          case "tokens": break;
          default: throw new NotImplementedException();
        }
    }
    static bool nulableType(Type type) {
      return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }
    static Type stringDictOf(Type type) {
      if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>) && type.GetGenericArguments()[0] == typeof(string))
        return type.GetGenericArguments()[1];
      else
        return null;
    }
    static Type arrayOf(Type type, out bool arrayOfArray) {
      arrayOfArray = false;
      if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        return type.GetGenericArguments()[0];
      else if (type.IsArray) {
        var res = type.GetElementType();
        if (res.IsArray) { arrayOfArray = true; return res.GetElementType(); } else return res;
      } else
        return null;
    }
    static Dictionary<string, xsdPropType> jsTypes = new Dictionary<string, xsdPropType>() { 
      { "UInt16", xsdPropType.Number },
      { "UInt64", xsdPropType.Number },
      { "uint", xsdPropType.Number },
      { "UInt32", xsdPropType.Number },
      { "int", xsdPropType.Number },
      { "short", xsdPropType.Number },
      { "Int16", xsdPropType.Number },
      { "Int32", xsdPropType.Number },
      { "Int64", xsdPropType.Number },
      { "long", xsdPropType.Number },
      { "Byte", xsdPropType.Number },
      { "Double", xsdPropType.Number },
      { "Boolean", xsdPropType.Bool },
      { "String", xsdPropType.String},
    };
    static xsdPropType getType(Type tp) {
      if (tp.IsEnum) return xsdPropType.Enum;
      xsdPropType res;
      if (jsTypes.TryGetValue(tp.Name, out res)) return res;
      return xsdPropType.Class;
    }
  }

  public class Register : ICSharpToTypeScript {

    public IEnumerable<Type> Types() {
      yield return typeof(xsd);
      yield return typeof(xsdLow);
      yield return typeof(xsdType);
      yield return typeof(xsdEnum);
      yield return typeof(xsdEnumItem);
      yield return typeof(xsdProp);
    }
    public IEnumerable<Type> ExtendedTypes() {
      yield break;
    }
    public IEnumerable<Type> Enums() {
      yield return typeof(xsdPropType);
      yield return typeof(xsdPropModifier);
      yield return typeof(xsdInheritsFrom);
      yield return typeof(xsdPropConstrains);
    }
    public IEnumerable<Type> ConstEnums() { yield break; }
    public string TsPath() { return Machines.rootPath + @"authorWeb\js\MetaJSGen.ts"; }
    public string Module() { return "metaJS"; }

    public IEnumerable<string> Uses() { yield break; }
    public bool generateFeature(FeatureType type) { return type == FeatureType.authorWebXSD; }
    public CourseModel.jsonMLMeta getJsonMLMeta() { return null; }
  }

}
