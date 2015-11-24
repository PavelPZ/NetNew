using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Xml.Linq;
using LMComLib;
using LMNetLib;
using System.ComponentModel;
using Newtonsoft.Json;
using CourseModel;
using System.Xml.Serialization;

namespace LMComLib {

  //JsonIgnore - Newtonsoft i generace JS ignoruje
  //JsonIgnore + JsonGenOnly - Newtonsoft ignoruje, generace JS neignoruje
  public class JsonGenOnlyAttribute : Attribute { }

  public enum FeatureType {
    LineNames,
    LangLists,
    LangToLineNames,
    LineToLangNames,
    LangToEADir,
    gaffFill_normTable,
    authorWebXSD,
    //InheritanceTree,
  }

  public interface ICSharpToTypeScript {
    IEnumerable<Type> Types();
    IEnumerable<Type> ExtendedTypes();
    IEnumerable<Type> Enums();
    IEnumerable<Type> ConstEnums();
    string TsPath();
    string Module();
    IEnumerable<string> Uses();
    bool generateFeature(FeatureType type);
    jsonMLMeta getJsonMLMeta();
    //string code();
  }

  public class RegisterImpl : ICSharpToTypeScript {
    public RegisterImpl(string nameSpace, string tsPath, IEnumerable<Type> enums, IEnumerable<Type> constEnums, params Type[] types) {
      this.nameSpace = nameSpace; this.tsPath = tsPath;
      this.enums = enums == null ? new Type[0] : enums.ToArray();
      this.constEnums = constEnums == null ? new Type[0] : constEnums.ToArray();
      this.types = types == null ? new Type[0] : types; }
    string nameSpace; string tsPath; Type[] enums; Type[] constEnums; Type[] types;
    public IEnumerable<Type> Types() { return types; }
    public IEnumerable<Type> ExtendedTypes() { yield break; }
    public IEnumerable<Type> Enums() { return enums; }
    public IEnumerable<Type> ConstEnums() { return constEnums; }
    public string TsPath() { return tsPath; }
    public string Module() { return nameSpace; }
    public IEnumerable<string> Uses() { yield break; }
    public bool generateFeature(FeatureType type) { return false; }
    public jsonMLMeta getJsonMLMeta() { return null; }
  }


  public static class CSharpToTypeScript {

    static Assembly cmdAss;
    static bool isCourseModel(ICSharpToTypeScript info) { return info.Module() == "CourseModel"; }
    public static void Generate(string dllPath, params string[] registers) {
      Assembly ass = Assembly.GetEntryAssembly();
      GenerateFromInfos(registers.Select(cls => ass.CreateInstance(cls)));
      //foreach (var cls in registers) {
      //  ICSharpToTypeScript info = (ICSharpToTypeScript)ass.CreateInstance(cls);
      //  CSharpToTypeScript.Generate(info);
      //}
    }
    public static IEnumerable<ICSharpToTypeScript> GetInfos(IEnumerable<Object> infos) {
      return infos.OfType<ICSharpToTypeScript>();
    }
    public static void GenerateFromInfos(IEnumerable<Object> infos) {
      Assembly ass = Assembly.GetCallingAssembly();
      cmdAss = Assembly.Load(ass.GetReferencedAssemblies().First(a => a.Name == "ObjectModel"));
      foreach (var info in GetInfos(infos)) Generate(info);
    }
    public static IEnumerable<Type> GeneratedTypes(IEnumerable<Object> infos) {
      return GetInfos(infos).SelectMany(inf => inf.Types());
    }
    public static void GenerateStr(StringBuilder sb, ICSharpToTypeScript info) {
      foreach (var u in info.Uses()) {
        sb.Append("/// <reference path=\""); sb.Append(u); sb.AppendLine("\" />");
      }
      sb.Append("module "); sb.Append(info.Module()); sb.AppendLine(" {");
      //if (info.getJsonMLMeta() != null) sb.AppendLine(" export var meta: { [type:string]:CourseModel.jsClassMeta;} = null;"); //{'~rootTag':<any>'" + info.getJsonMLMeta().root.Name + "' };");
      foreach (var en in info.Enums()) GenEnum(en, sb, false);
      foreach (var en in info.ConstEnums()) GenEnum(en, sb, true);
      foreach (var en in info.Types()) {
        GenType(en, info.ExtendedTypes().ToArray(), info.Module(), sb);
        //finishCourseModelType(info, en, sb);
      }
      var meta = info.getJsonMLMeta();
      if (meta != null) {
        sb.Append("export var meta: CourseModel.jsonMLMeta = <any>");
        var json = JsonConvert.SerializeObject(meta, Formatting.None, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore });
        sb.Append(json.Replace("###\"", null).Replace("\"###", null)); sb.AppendLine(";");
        foreach (var tp in meta.types.Values) {
          sb.Append(string.Format("export var t{0} = '{1}'; ", tp.name, tp.tagName));
        }
      }

      if (info.generateFeature(FeatureType.LineNames)) generateLineNames(sb);
      if (info.generateFeature(FeatureType.LangToLineNames)) GenLangToLine(sb);
      if (info.generateFeature(FeatureType.LineToLangNames)) GenLinesToLang(sb);
      if (info.generateFeature(FeatureType.LangLists)) GenLangLists(sb);
      if (info.generateFeature(FeatureType.LangToEADir)) GenLangToEADir(sb);
      if (info.generateFeature(FeatureType.gaffFill_normTable)) GenGaffFill_normTable(sb);
      if (info.generateFeature(FeatureType.authorWebXSD)) GenAuthorWebXSD(sb);
      //GenInheritanceTree(info, sb);
      //sb.AppendLine(info.code());
      sb.AppendLine("}");
      sb.AppendLine();
    }
    public static void Generate(ICSharpToTypeScript info) {
      StringBuilder sb = new StringBuilder();
      GenerateStr(sb, info);
      File.WriteAllText(info.TsPath(), sb.ToString(), Encoding.ASCII);
    }

    private static void GenAuthorWebXSD(StringBuilder sb) {
      var json = new metaJS.xsd().toJson();
      sb.AppendLine();
      sb.AppendLine("export var metaData: xsd = ");
      sb.AppendLine(json);
      sb.AppendLine(";");
    }

    static void genLevel(IEnumerable<string> ss, StringBuilder sb, Dictionary<string, IEnumerable<string>> desc) {
      if (ss == null) { sb.Append("null"); return; }
      sb.Append("regType([");
      foreach (var s in ss) {
        sb.Append("{anc: null,name:t"); sb.Append(s); sb.Append(","); sb.Append("desc:");
        genLevel(desc.ContainsKey(s) ? desc[s] : null, sb, desc);
        sb.Append("},");
      }
      sb.Length = sb.Length - 1;
      sb.Append("])");
    }

    static void GenEnum(Type tp, StringBuilder sb, bool isConst) {
      sb.Append("export " + (isConst ? "const " : null) + "enum "); sb.Append(tp.Name); sb.AppendLine(" {");
      try {
        var vals = Enum.GetValues(tp).Cast<object>();
        if (tp == typeof(CourseIds)) vals = vals.Where(v => (int)v < (int)CourseIds.eTestMe_EnglishSmall);
        foreach (var v in vals) {
          sb.Append("  ");
          sb.Append(v.ToString());
          sb.Append(" = "); sb.Append((int)v);
          sb.AppendLine(",");
        }
      } catch (Exception exp) {
        sb.AppendLine("**** ERROR");
        sb.AppendLine(exp.Message);
      }
      sb.AppendLine("}");
      sb.AppendLine(null);
    }

    static void GenLangToLine(StringBuilder sb) {
      var langs = LowUtils.EnumGetValues<Langs>().Cast<int>().ToArray();
      var arr = new int[(int)Langs.en_nz];
      for (var i = 0; i < arr.Length; i++) arr[i] = 0;
      foreach (var l in langs)
        if (l < arr.Length)
          try { arr[l] = (int)CommonLib.LangToLineId((Langs)l); } catch { arr[l] = 0; }
      sb.Append("export var LangToLine: LineIds[] = [");
      foreach (var l in arr) { sb.Append(l); sb.Append(','); }
      sb.Length = sb.Length - 1;
      sb.AppendLine("];");
    }
    static void GenLangLists(StringBuilder sb) {
      foreach (var it in (
        new Tuple<string, IEnumerable<Langs>>[] { new Tuple<string, IEnumerable<Langs>>("bigLocalizations", CommonLib.bigLocalizations) }
        )) {
        sb.Append("export var "); sb.Append(it.Item1); sb.Append(" = [");
        sb.Append(it.Item2.Select(l => ((int)l).ToString()).Aggregate((r, i) => r + ", " + i));
        sb.Append("];\r\n");
      }
    }
    static void GenLinesToLang(StringBuilder sb) {
      var langs = LowUtils.EnumGetValues<LineIds>().Cast<int>().ToArray();
      var arr = new int[langs.Max() + 1];
      for (var i = 0; i < arr.Length; i++) arr[i] = 0;
      foreach (var l in langs)
        try { arr[l] = (int)CommonLib.LineIdToLang((LineIds)l); } catch { arr[l] = 0; }
      sb.Append("export var LineToLang: Langs[] = [");
      foreach (var l in arr) { sb.Append(l); sb.Append(','); }
      sb.Length = sb.Length - 1;
      sb.AppendLine("];");
    }

    static void GenGaffFill_normTable(StringBuilder sb) {
      sb.AppendLine();
      sb.AppendLine("export var gaffFill_normTable: { [charCode: number]: string; } = {");
      foreach (var kv in CourseModel.gapFill.gaffFill_normTable) sb.AppendFormat("{0}:'{1}',", kv.Key, kv.Value);
      sb.Length = sb.Length - 1;
      sb.AppendLine();
      sb.AppendLine("};");
    }

    static void GenLangToEADir(StringBuilder sb) {
      sb.Append("export var LangToEADir = {");
      foreach (var l in LowUtils.EnumGetValues<Langs>()) {
        string dir = null;
        try { dir = LMComLib.urlInfo.LangToEADir(l); } catch { dir = null; }
        if (dir == null) continue;
        sb.Append("'"); sb.Append(((int)l).ToString()); sb.Append("'"); sb.Append(":'"); sb.Append(dir); sb.Append("',");
      }
      sb.Length = sb.Length - 1;
      sb.AppendLine("};");
    }


    public static string GenInlineTypeParse(Type type, HashSet<string> allTypes) {
      if (type == null) return "";
      if (type.ToString() == "System.Void") return "void";
      if (type.FullName.StartsWith("System.") || !type.IsClass || allTypes.Contains(type.FullName))
        return LMComLib.CSharpToTypeScript.fieldTypeForProxy(type);
      else if (type.IsArray) {
        var tp = type.GetElementType();
        if (tp.FullName.StartsWith("System.") || !tp.IsClass || allTypes.Contains(tp.FullName))
          return LMComLib.CSharpToTypeScript.fieldTypeForProxy(tp) + "[]";
        else
          return LMComLib.CSharpToTypeScript.GenInlineTypeForProxy(tp, allTypes) + "[]";
      } else
        return LMComLib.CSharpToTypeScript.GenInlineTypeForProxy(type, allTypes);
    }

    public static string GenInlineTypeForProxy(Type tp, HashSet<string> allTypes) {
      StringBuilder sb = new StringBuilder();
      //if (tp.Name == "evaluatorsForLineResult")
      //  sb.Append("");
      sb.Append("{ ");
      var bindingFlags = BindingFlags.Instance | BindingFlags.Public;
      foreach (var fld in tp.GetFields(bindingFlags)) {
        if (fld.Name == "typeOfs" || (fld.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Any() && !fld.GetCustomAttributes(typeof(JsonGenOnlyAttribute), false).Any())) continue;
        sb.Append(" ");
        sb.Append(fld.Name);
        if (nulableFieldType(fld.FieldType)) sb.Append('?');
        sb.Append(": ");
        sb.Append(GenInlineTypeParse(fld.FieldType, allTypes));
        //fieldType(fld.FieldType, "", sb);
        sb.Append("; ");
      }
      sb.Append(" }");
      return sb.ToString();
    }

    public static string fieldTypeForProxy(Type FieldType) {
      StringBuilder sb = new StringBuilder();
      fieldType(FieldType, null, sb);
      return sb.ToString();
    }

    static void GenType(Type tp, Type[] extendedTypes, string module, StringBuilder sb) {
      sb.Append("export interface "); sb.Append(tp.Name);
      if (ancestors(tp).Any()) {
        sb.Append(" extends "); sb.Append(module == tp.BaseType.Namespace ? tp.BaseType.Name : tp.BaseType.FullName);
      }
      sb.AppendLine(" {");
      foreach (var fld in tp.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)) {
        if (fld.Name == "typeOfs" || (fld.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Any() && !fld.GetCustomAttributes(typeof(JsonGenOnlyAttribute), false).Any())) continue;
        sb.Append("  ");
        sb.Append(fld.Name);
        if (nulableFieldType(fld.FieldType)) sb.Append('?');
        sb.Append(": ");
        fieldType(fld.FieldType, module, sb);
        sb.AppendLine(";");
      }
      foreach (var prop in tp.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)) {
        if (prop.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Any() && !prop.GetCustomAttributes(typeof(JsonGenOnlyAttribute), false).Any()) continue;
        sb.Append("  ");
        sb.Append(prop.Name);
        if (nulableFieldType(prop.PropertyType)) sb.Append('?');
        sb.Append(": ");
        fieldType(prop.PropertyType, module, sb);
        sb.AppendLine(";");
      }
      sb.AppendLine("}");
      bool isExtended = extendedTypes.Any(t => t == tp);
      if (isExtended) { sb.Append("export var "); sb.Append(tp.Name); sb.Append("_Type = '"); sb.Append(tp.FullName); sb.AppendLine("';"); }
      if (isExtended) {
        var scormCmd = cmdAss.GetType("scorm.ScormCmd");
        var isCmd = tp == scormCmd || tp.IsSubclassOf(scormCmd);
        sb.Append("export function "); sb.Append(tp.Name); sb.Append("_Create (");
        bool first = true;
        foreach (var fld in tp.GetFields(BindingFlags.Instance | BindingFlags.Public).Where(fld => !fld.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Any())) {
          //if (fld.Name == "VerifyStatus") {
          //  var ok = !fld.GetCustomAttributes(typeof(JsonGenOnlyAttribute), true).Any();
          //  System.Diagnostics.Debugger.Break();
          //}
          if (fld.Name == "typeOfs" || (isCmd && fld.Name == "date")) continue;
          if (first) first = false; else sb.Append(", ");
          sb.Append(fld.Name); sb.Append(": "); fieldType(fld.FieldType, module, sb);
        }
        sb.Append("): "); sb.Append(tp.Name); sb.AppendLine("{");
        sb.Append("  return {");
        first = true;
        foreach (var fld in tp.GetFields(BindingFlags.Instance | BindingFlags.Public).Where(fld => !fld.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Any())) {
          if (fld.Name == "typeOfs") continue;
          if (first) first = false; else sb.Append(", ");
          sb.Append(fld.Name); sb.Append(": ");
          sb.Append(isCmd && fld.Name == "date" ? "Utils.nowToInt()" : fld.Name);
        }
        sb.AppendLine("};");
        sb.AppendLine("}");
      }
    }

    static bool nulableFieldType(Type type) {
      return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }
    static void fieldType(Type FieldType, string module, StringBuilder sb) {
      if (sb == null) sb = new StringBuilder();
      if (nulableFieldType(FieldType))
        FieldType = Nullable.GetUnderlyingType(FieldType);
      if (FieldType.IsGenericType && FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>) && FieldType.GetGenericArguments()[0] == typeof(string)) {
        sb.Append("{ [id:string]: ");
        sb.Append(getJsTypeName(FieldType.GetGenericArguments()[1], module));
        sb.Append("}");
      } else if (FieldType.IsGenericType && FieldType.GetGenericTypeDefinition() == typeof(List<>)) {
        var tp = FieldType.GetGenericArguments()[0];
        sb.Append("Array<");
        sb.Append(getJsTypeName(tp, module));
        sb.Append(">");
      } else if (FieldType.IsArray) {
        sb.Append("Array<");
        if (FieldType.GetElementType().IsArray) {
          sb.Append("Array<");
          sb.Append(getJsTypeName(FieldType.GetElementType().GetElementType(), module));
          sb.Append(">");
        } else
          sb.Append(getJsTypeName(FieldType.GetElementType(), module));
        sb.Append(">");
        //sb.Append("[]");
      } else if (FieldType.IsEnum) {
        sb.Append(module == FieldType.Namespace ? FieldType.Name : FieldType.FullName);
      } else
        sb.Append(getJsTypeName(FieldType, module));
    }

    static void generateLineNames(StringBuilder sb) {
      sb.AppendLine("export var LangTitles: Object = {");
      foreach (var ln in CommonLib.langTitle.Where(v => !string.IsNullOrEmpty(v.Value))) {
        sb.Append("  '"); sb.Append(((int)CommonLib.LangToLineId(ln.Key)).ToString()); sb.Append("'"); sb.Append(":'"); sb.Append(LowUtils.EncodeJsString(ln.Value, false)); sb.AppendLine("',");
      }
      sb.AppendLine("};");
    }
    static string getJsTypeName(Type tp, string module) {
      string res;
      if (jsTypes.TryGetValue(tp.Name, out res)) return res;
      return module == tp.Namespace ? tp.Name : tp.FullName;
    }
    static IEnumerable<Type> ancestors(Type tp) {
      while (true) {
        tp = tp.BaseType;
        if (tp == typeof(System.ValueType) || tp == typeof(System.Object) || tp.Name == "PackedObj") yield break;
        yield return tp;

      }
    }

    static Dictionary<string, string> jsTypes = new Dictionary<string, string>() {
      { "UInt16", "number" },
      { "UInt64", "number" },
      { "uint", "number" },
      { "UInt32", "number" },
      { "int", "number" },
      { "short", "number" },
      { "Int16", "number" },
      { "Int32", "number" },
      { "Int64", "number" },
      { "long", "number" },
      { "Byte", "number" },
      { "Double", "number" },
      { "Object", "any" },
      { "Boolean", "boolean" },
      { "String", "string" },
      { "Rew.int_s", "number[]" },
      { "Rew.short_s", "number[]" },
      { "Rew.Int16_s", "number[]" },
      { "Rew.Int32_s", "number[]" },
      { "Rew.Int64_s", "number[]" },
      { "Rew.long_s", "number[]" },
      { "Rew.String_s", "string[]" },
    };
  }
}
