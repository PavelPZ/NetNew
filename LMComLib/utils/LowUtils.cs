using LMComLib;
using Newtonsoft.Json;
// $Header: /cvsroot/LMCom/lmcomlib/utils/LowUtils.cs,v 1.37 2012/10/18 19:14:33 pavel Exp $
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace LMNetLib {

  public static class ThreadRandom {
    private static Random _global = new Random();
    [ThreadStatic]
    private static Random _local;

    public static int Next(int maxValue) {
      Random inst = _local;
      if (inst == null) {
        int seed;
        lock (_global) seed = _global.Next(maxValue);
        _local = inst = new Random(seed);
      }
      return inst.Next(maxValue);
    }
  }


  //public class CData : IXmlSerializable {
  //  private string _value;

  //  /// <summary>
  //  /// Allow direct assignment from string:
  //  /// CData cdata = "abc";
  //  /// </summary>
  //  /// <param name="value"></param>
  //  /// <returns></returns>
  //  public static implicit operator CData(string value) {
  //    return new CData(value);
  //  }

  //  /// <summary>
  //  /// Allow direct assigment to string
  //  /// string str = cdata;
  //  /// </summary>
  //  /// <param name="cdata"></param>
  //  /// <returns></returns>
  //  public static implicit operator string(CData cdata) {
  //    return cdata._value;
  //  }

  //  public CData()
  //    : this(string.Empty) {
  //  }

  //  public CData(string value) {
  //    _value = value;
  //  }

  //  public override string ToString() {
  //    return _value;
  //  }

  //  public System.Xml.Schema.XmlSchema GetSchema() {
  //    return null;
  //  }

  //  public void ReadXml(System.Xml.XmlReader reader) {
  //    _value = reader.ReadElementString();
  //  }

  //  public void WriteXml(System.Xml.XmlWriter writer) {
  //    writer.WriteCData(_value);
  //  }
  //}

  public abstract class LoggerObj : IDisposable {
    public virtual void Dispose() { }
    public bool hasError;
    public bool strictChecking;
    public string context;
    const string errorPrefix = "****** ";
    protected virtual string erroPr(string ctx) {
      //System.Diagnostics.Debugger.Break();
      return "\r\n****** " + ctx + ": ";
    }
    protected abstract void write(string msg);
    public void AppendLine(string msg = null) { if (!string.IsNullOrEmpty(msg)) write(msg + "\r\n"); }
    public void AppendLineFmt(string msg, params object[] pars) { AppendLine(string.Format(msg, pars)); }
    public void Append(string msg) { write(msg); }
    public void AppendFmt(string msg, params object[] pars) { write(string.Format(msg, pars)); }
    public virtual void ErrorLine(string ctx, string msg) { hasError = true; AppendLine(erroPr(ctx) + msg); }
    public virtual void ErrorLineFmt(string ctx, string msg, params object[] pars) { hasError = true; AppendLineFmt(erroPr(ctx) + msg, pars); }
  }

  public class LoggerFile : LoggerObj {
    TextWriter wr;
    public LoggerFile(string fn) { wr = TextWriter.Synchronized(new StreamWriter(fn)); }
    public override void Dispose() { wr.Close(); }
    protected override void write(string msg) { wr.Write(msg); }
  }

  public class LoggerDummy : LoggerObj {
    protected override void write(string msg) { }
  }

  public class LoggerMemory : LoggerObj {
    public bool saveDumpXml;
    public XElement dumpXml;
    public bool isVsNet; //bezim v kontextu VS.NET
    public string vsNetGlobalPublisherDir; //lokalni publisher ID pro vsnet, napr. "/publisher/". V sitemap author buildu produktu jsou URL adresy, zacinajicim timto retezcem nahrazeny, napr. /publ/1/1/
    public bool vsNetForBrowseAction; //v sitemap pro browse jsou vsechny folder nodes nahrazeny data. Root je nahrazen by mod


    public LoggerMemory(bool strictChecking) { this.strictChecking = strictChecking; }
    public LoggerMemory() : this(false) { }

    protected override void write(string msg) { System.Diagnostics.Debugger.Break(); throw new NotImplementedException(); }

    public void ErrorExp(string ctx, Exception exp) {
      hasError = true;
      lock (typeof(LoggerMemory)) {
        HashSet<string> el;
        if (!data.TryGetValue(ctx, out el)) data.Add(ctx, el = new HashSet<string>());
        el.Add(LowUtils.ExceptionToString(exp));
      }
    }
    public override void ErrorLine(string ctx, string msg) {
      hasError = true;
      lock (typeof(LoggerMemory)) {
        HashSet<string> el;
        if (!data.TryGetValue(ctx, out el)) data.Add(ctx, el = new HashSet<string>());
        el.Add(msg);
      }
    }
    public override void ErrorLineFmt(string ctx, string msg, params object[] pars) { hasError = true; ErrorLine(ctx, string.Format(msg, pars)); }

    public string LogHtml() {
      return Log().Replace("\r\n", "<br/>");
    }
    public string Log() {
      StringBuilder sb = new StringBuilder();
      foreach (var kv in data) {
        sb.Append("****** "); sb.AppendLine(kv.Key);
        foreach (var s in kv.Value) {
          sb.Append("=== ");
          //if (s.Length < 2) System.Diagnostics.Debugger.Break();
          //sb.AppendLine(s[1]);
          sb.AppendLine(s);
        }
        sb.AppendLine();
      }
      return sb.ToString();
    }

    public void clear() { data.Clear(); hasError = false; vsNetForBrowseAction = false; vsNetGlobalPublisherDir = null; }

    public Dictionary<string, HashSet<string>> data = new Dictionary<string, HashSet<string>>();
  }
  public class LoggerMemory_ : LoggerMemory {
    public LoggerMemory_(bool strictChecking) { this.strictChecking = strictChecking; }
    StringWriter wr = new StringWriter();
    protected override void write(string msg) { wr.Write(msg); }
    public string Log() {
      StringBuilder sb = new StringBuilder();
      foreach (var grp in wr.ToString().Split(new string[] { "####" }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim(new char[] { '\r', '\n', ' ' }).Split(new string[] { "$$$$" }, StringSplitOptions.RemoveEmptyEntries)).GroupBy(l => l[0])) {
        sb.Append("****** "); sb.AppendLine(grp.Key);
        foreach (var s in grp) {
          sb.Append("=== ");
          if (s.Length < 2) System.Diagnostics.Debugger.Break();
          //sb.AppendLine(s[1]);
          sb.AppendLine(s[s.Length < 2 ? 0 : 1]);
        }
        sb.AppendLine();
      }
      return sb.ToString();
    }
    protected override string erroPr(string ctx) {
      //System.Diagnostics.Debugger.Break();
      return "####" + ctx + "$$$$";
    }
  }

  public class NameValue<T> {
    [XmlAttribute]
    public string Name;
    [XmlAttribute]
    public T Value;
  }

  public class NameValueString : NameValue<string> {
    public NameValueString() { }
    public NameValueString(string name, string value) { Name = name; Value = value; }
  }

  public class igbnoreAcute : IEqualityComparer<string> {
    bool IEqualityComparer<string>.Equals(string s1, string s2) {
      return s1.Replace("ß", "ss").Normalize(NormalizationForm.FormD).Where(c => c != '\'' && CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).SequenceEqual(
        s2.Replace("ß", "ss").Normalize(NormalizationForm.FormD).Where(c => c != '\'' && CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark));
    }

    int IEqualityComparer<string>.GetHashCode(string obj) {
      return LowUtils.computeHashCodes(obj.Replace("ß", "ss").Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).OfType<Object>());
    }
    public static bool isSame(string s1, string s2) { return ((IEqualityComparer<string>)Instance).Equals(s1, s2); }
    public static igbnoreAcute Instance = new igbnoreAcute();
    public static string removeAcute(string w) { return new string(w.Replace("ß", "ss").Normalize(NormalizationForm.FormD).Where(c => c != '\'' && CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()); }
  }

  public static class LookupLib {
    public static ILookup<string, string> invert(ILookup<string, string> src) {
      return src.SelectMany(kv => kv.Select(v => new { k = kv.Key, v })).ToLookup(kv => kv.v, kv => kv.k);
    }
    public static ILookup<string, string> invert(Dictionary<string, HashSet<string>> src) {
      return src.SelectMany(kv => kv.Value.Select(v => new { k = kv.Key, v })).ToLookup(kv => kv.v, kv => kv.k);
    }
    public static IEnumerable<string> toStrings(ILookup<string, string> src) {
      return src.OrderBy(k => k.Key).Select(kv => kv.Key + ": " + kv.Aggregate((r, i) => r + "|" + i));
    }
    public static IEnumerable<string> toStrings(Dictionary<string, HashSet<string>> src) {
      return src.OrderBy(k => k.Key).Select(kv => kv.Key + ": " + kv.Value.Aggregate((r, i) => r + "|" + i));
    }
    public static ILookup<string, string> fromStrings(IEnumerable<string> src) {
      return src.Select(l => l.Split(new string[] { ": " }, StringSplitOptions.RemoveEmptyEntries)).
      SelectMany(parts => parts[1].Split('|').Select(p => new { key = parts[0], value = p })).
      ToLookup(kv => kv.value, kv => kv.key);
    }
    public static ILookup<string, string> fromDictHash(Dictionary<string, HashSet<string>> src) {
      return src.SelectMany(kv => kv.Value.Select(v => new { k = kv.Key, v })).ToLookup(kv => kv.k, kv => kv.v);
    }
    public static ILookup<string, string> extract(ILookup<string, string> src, ILookup<string, string> lk) {
      return src.SelectMany(kv => kv.Select(v => kv.Key + "\x0" + v)).Except(lk.SelectMany(kv => kv.Select(v => kv.Key + "\x0" + v))).Select(kv => kv.Split('\x0')).ToLookup(p => p[0], p => p[1]);
    }
  }

  [Serializable()]
  public class SerializableDictionary<TKey, TVal> : Dictionary<TKey, TVal>, IXmlSerializable, ISerializable {
    #region Constants
    private const string DictionaryNodeName = "Dictionary";
    private const string ItemNodeName = "Item";
    private const string KeyNodeName = "Key";
    private const string ValueNodeName = "Value";
    #endregion
    #region Constructors
    public SerializableDictionary() {
    }

    public SerializableDictionary(IDictionary<TKey, TVal> dictionary)
      : base(dictionary) {
    }

    public SerializableDictionary(IEqualityComparer<TKey> comparer)
      : base(comparer) {
    }

    public SerializableDictionary(int capacity)
      : base(capacity) {
    }

    public SerializableDictionary(IDictionary<TKey, TVal> dictionary, IEqualityComparer<TKey> comparer)
      : base(dictionary, comparer) {
    }

    public SerializableDictionary(int capacity, IEqualityComparer<TKey> comparer)
      : base(capacity, comparer) {
    }

    #endregion
    #region ISerializable Members

    protected SerializableDictionary(SerializationInfo info, StreamingContext context) {
      int itemCount = info.GetInt32("ItemCount");
      for (int i = 0; i < itemCount; i++) {
        KeyValuePair<TKey, TVal> kvp = (KeyValuePair<TKey, TVal>)info.GetValue(String.Format("Item{0}", i), typeof(KeyValuePair<TKey, TVal>));
        this.Add(kvp.Key, kvp.Value);
      }
    }

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
      info.AddValue("ItemCount", this.Count);
      int itemIdx = 0;
      foreach (KeyValuePair<TKey, TVal> kvp in this) {
        info.AddValue(String.Format("Item{0}", itemIdx), kvp, typeof(KeyValuePair<TKey, TVal>));
        itemIdx++;
      }
    }

    #endregion
    #region IXmlSerializable Members

    void IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) {
      //writer.WriteStartElement(DictionaryNodeName);
      foreach (KeyValuePair<TKey, TVal> kvp in this) {
        writer.WriteStartElement(ItemNodeName);
        writer.WriteStartElement(KeyNodeName);
        KeySerializer.Serialize(writer, kvp.Key);
        writer.WriteEndElement();
        writer.WriteStartElement(ValueNodeName);
        ValueSerializer.Serialize(writer, kvp.Value);
        writer.WriteEndElement();
        writer.WriteEndElement();
      }
      //writer.WriteEndElement();
    }

    void IXmlSerializable.ReadXml(System.Xml.XmlReader reader) {
      if (reader.IsEmptyElement) {
        return;
      }

      // Move past container
      if (!reader.Read()) {
        throw new XmlException("Error in Deserialization of Dictionary");
      }

      //reader.ReadStartElement(DictionaryNodeName);
      while (reader.NodeType != XmlNodeType.EndElement) {
        reader.ReadStartElement(ItemNodeName);
        reader.ReadStartElement(KeyNodeName);
        TKey key = (TKey)KeySerializer.Deserialize(reader);
        reader.ReadEndElement();
        reader.ReadStartElement(ValueNodeName);
        TVal value = (TVal)ValueSerializer.Deserialize(reader);
        reader.ReadEndElement();
        reader.ReadEndElement();
        this.Add(key, value);
        reader.MoveToContent();
      }
      //reader.ReadEndElement();

      reader.ReadEndElement(); // Read End Element to close Read of containing node
    }

    System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() {
      return null;
    }

    #endregion
    #region Private Properties
    protected XmlSerializer ValueSerializer
    {
      get
      {
        if (valueSerializer == null) {
          valueSerializer = new XmlSerializer(typeof(TVal));
        }
        return valueSerializer;
      }
    }

    private XmlSerializer KeySerializer
    {
      get
      {
        if (keySerializer == null) {
          keySerializer = new XmlSerializer(typeof(TKey));
        }
        return keySerializer;
      }
    }
    #endregion
    #region Private Members
    private XmlSerializer keySerializer = null;
    private XmlSerializer valueSerializer = null;
    #endregion
  }

  //public class TitleUrl {
  //  public TitleUrl() { }
  //  public TitleUrl(string title, string url, string target) {
  //    Title = title; Url = url; Target = target;
  //  }
  //  public TitleUrl(string title, string url) {
  //    Title = title; Url = url; Target = "_top";
  //  }
  //  public TitleUrl(SiteMapNode nd) {
  //    Title = nd.Title; Url = LMComLib.urlInfo.GetUrl(nd); Target = "_top";
  //  }
  //  public TitleUrl(string title, string url, bool b) {
  //    Title = title; Url = LMComLib.urlInfo.GetUrl(SiteMap.Provider.FindSiteMapNode(url)); Target = "_top";
  //  }
  //  public TitleUrl(string url) : this(SiteMap.Provider.FindSiteMapNode(url)) { }
  //  public string Title { get; set; }
  //  public string Url { get; set; }
  //  public string Target;
  //}

  /*public class MoodleCourseRoot : MoodleCourseItem {
    public MoodleCourseRoot() { }
    public MoodleCourseRoot(string title, string url, string descr): base(title, url, descr) {}
    public string[] OtherFiles;
  }*/

  public class MoodleCourseItem {
    public MoodleCourseItem() { }
    public MoodleCourseItem(string title, string url, string descr) {
      Title = title; Url = url; Descr = descr;
    }
    public string Descr;
    public string Title;
    public string Url;
    public MoodleCourseItem[] Items;
    public IEnumerable<MoodleCourseItem> getItems() {
      yield return this;
      if (Items != null) foreach (MoodleCourseItem it in Items) foreach (MoodleCourseItem subIt in it.getItems()) yield return subIt;
    }
    public bool HasChilds() {
      return Items != null && Items.Length > 0;
    }
  }

  [AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
  public class EnumDescrAttribute : System.Attribute {
    public EnumDescrAttribute(Type tp, string descr) {
      tp = getType(tp);
      foreach (string nv in descr.Split(',')) {
        string[] parts = nv.Split('=');
        try {
          Descr.Add((ulong)(int)Enum.Parse(tp, parts[0]), parts[1]);
        } catch {
          Descr.Add((ulong)Enum.Parse(tp, parts[0]), parts[1]);
        }
      }
    }
    Dictionary<ulong, string> Descr = new Dictionary<ulong, string>();
    static Type getType(Type enumType) {
      return enumType.IsGenericType && enumType.GetGenericTypeDefinition() == typeof(Nullable<>) ? Nullable.GetUnderlyingType(enumType) : enumType;
    }
    public static string getDescr(Type enumType, int value) {
      enumType = getType(enumType);
      foreach (EnumDescrAttribute attr in enumType.GetCustomAttributes(typeof(EnumDescrAttribute), false)) {
        string res;
        if (!attr.Descr.TryGetValue((ulong)value, out res)) return "-";
        return res;
      }
      return string.Format("missing EnumDescrAttribute at {0} enum", enumType.Name);
    }
    public static int getValue(Type enumType, string descr) {
      enumType = getType(enumType);
      foreach (EnumDescrAttribute attr in enumType.GetCustomAttributes(typeof(EnumDescrAttribute), false)) {
        foreach (KeyValuePair<ulong, string> kv in attr.Descr)
          if (string.Compare(kv.Value, descr) == 0) return (int)kv.Key;
      }
      return -1;
    }
    public static IEnumerable<string> getValues(Type enumType) {
      return getInfo(enumType).Values;
    }
    public static Dictionary<ulong, string> getInfo(Type enumType) {
      enumType = getType(enumType);
      foreach (EnumDescrAttribute attr in enumType.GetCustomAttributes(typeof(EnumDescrAttribute), false))
        return attr.Descr;
      throw new Exception(enumType.FullName);
    }
  }

  public class LMException : Exception {
    public LMException(string message, params object[] args) : base(string.Format(message, args)) { }
  }

  public delegate void TransAddStringEvent(string key, string val);
  public delegate string TransGetStringEvent(string key);

  // Pøíklad použití:
  //  string hashed = Utils.LowUtils.HashPassword("heslo");
  //  string heslo = Utils.LowUtils.DehashPassword(hashed);

  /// <summary>Uzitecne funkce.</summary>
  public static class LowUtils {

    public static JsonSerializerSettings jsonSet = new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore };

    public static string serializeObjectToJS(object obj) {
      return JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented, jsonSet);
    }

    public static string join(IEnumerable<string> lines, string d) {
      if (lines == null) return null;
      StringBuilder sb = new StringBuilder(); bool exists = false;
      foreach (var l in lines) { exists = true; sb.Append(l); sb.Append(d); }
      if (exists) sb.Length = sb.Length = sb.Length - d.Length;
      return sb.ToString();
      //return s.DefaultIfEmpty().Aggregate((r, i) => r + d + i);
    }

    public static IEnumerable<int> indexesOf(string s, char ch) {
      if (string.IsNullOrEmpty(s)) yield break;
      var idx = -1;
      while (true) {
        idx = s.IndexOf(ch, idx + 1); if (idx < 0) yield break;
        yield return idx;
      }
    }
    public static int nthIndexesOf(string s, char ch, int nth) {
      nth--; if (nth < 0) return -1;
      if (string.IsNullOrEmpty(s)) return -1;
      var idx = -1;
      while (nth >= 0) {
        idx = s.IndexOf(ch, idx + 1); if (idx < 0) return -1;
        nth--;
      }
      return idx;
    }

    public static string fromCammelCase(string nm) {
      List<char> res = new List<char>(); bool first = true;
      foreach (var ch in nm) {
        if (char.IsUpper(ch)) { if (!first) res.Add('-'); first = false; res.Add(char.ToLower(ch)); } else res.Add(ch);
        first = false;
      }
      return new string(res.ToArray());
    }

    public static string toCammelCase(string nm) {
      if (string.IsNullOrEmpty(nm)) return nm;
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

    static LowUtils() {
      // Allocate table
      _crc32Table = new uint[256];
      // For each byte
      for (uint n = 0; n < 256; n++) {
        // For each bit
        uint c = n;
        for (int k = 0; k < 8; k++) {
          // Compute value
          if (0 != (c & 1)) {
            c = 0xedb88320 ^ (c >> 1);
          } else {
            c = c >> 1;
          }
        }
        // Store result in table
        _crc32Table[n] = c;
      }
    }

    //http://en.wikipedia.org/wiki/Pearson_hashing
    public static byte pearsonHash8(byte[] data, byte addToFirstByte = 0) {
      byte res = 0; bool firstByte = true;
      foreach (var c in data) {
        byte val = firstByte ? (byte)(c + addToFirstByte) : c; firstByte = false; // add addToFirstByte to first byte => odlisna hodnota hashe pro ruzna addToFirstByte
        byte index = (byte)(res ^ val);
        res = pearsonTable[index];
      }
      return res;
    }
    public static UInt16 pearsonHash16(byte[] data) { //concatenate vice hashes
      byte[] bres = new byte[] { pearsonHash8(data, 0), pearsonHash8(data, 1) };
      return BitConverter.ToUInt16(bres, 0);
    }
    public static UInt16 pearsonHash16(string data) {
      return pearsonHash16(Encoding.UTF8.GetBytes(data));
    }
    static byte[] pearsonTable = new byte[] {
      98, 6, 85,150, 36, 23,112,164,135,207,169, 5, 26, 64,165,219, // 1
      61, 20, 68, 89,130, 63, 52,102, 24,229,132,245, 80,216,195,115, // 2
      90,168,156,203,177,120, 2,190,188, 7,100,185,174,243,162, 10, // 3
      237, 18,253,225, 8,208,172,244,255,126,101, 79,145,235,228,121, // 4
      123,251, 67,250,161, 0,107, 97,241,111,181, 82,249, 33, 69, 55, // 5
      59,153, 29, 9,213,167, 84, 93, 30, 46, 94, 75,151,114, 73,222, // 6
      197, 96,210, 45, 16,227,248,202, 51,152,252,125, 81,206,215,186, // 7
      39,158,178,187,131,136, 1, 49, 50, 17,141, 91, 47,129, 60, 99, // 8
      154, 35, 86,171,105, 34, 38,200,147, 58, 77,118,173,246, 76,254, // 9
      133,232,196,144,198,124, 53, 4,108, 74,223,234,134,230,157,139, // 10
      189,205,199,128,176, 19,211,236,127,192,231, 70,233, 88,146, 44, // 11
      183,201, 22, 83, 13,214,116,109,159, 32, 95,226,140,220, 57, 12, // 12
      221, 31,209,182,143, 92,149,184,148, 62,113, 65, 37, 27,106,166, // 13
      3, 14,204, 72, 21, 41, 56, 66, 28,193, 40,217, 25, 54,179,117, // 14
      238, 87,240,155,180,170,242,212,191,163, 78,218,137,194,175,110, // 15
      43,119,224, 71,122,142, 42,160,104, 48,247,103, 15, 11,138,239  // 16
    };


    //http://blogs.msdn.com/b/delay/archive/2009/01/14/free-hash-a-reusable-crc-32-hashalgorithm-implementation-for-net.aspx    
    public static uint crc(byte[] data) {
      uint _crc32Value = uint.MaxValue;
      for (int i = 0; i < data.Length; i++) {
        byte index = (byte)(_crc32Value ^ data[i]);
        _crc32Value = _crc32Table[index] ^ ((_crc32Value >> 8) & 0xffffff);
      }
      return _crc32Value;
    }
    public static int crc(string data) {
      if (string.IsNullOrEmpty(data)) return 0;
      return (int)crc(Encoding.UTF8.GetBytes(data));
    }
    private static readonly uint[] _crc32Table;

    public static string GetIpAddress(HttpRequest req) {
      if (req.ServerVariables == null) return null;
      var ip = ipHeaders.Select(hdr => req.ServerVariables[hdr]).Where(val => !string.IsNullOrEmpty(val)).FirstOrDefault();
      return ip == null ? null : ip.Split(',')[0];
    }
    static string[] ipHeaders = new string[] { "HTTP_CLIENT_IP", "HTTP_X_FORWARDED_FOR", "HTTP_X_FORWARDED", "HTTP_X_CLUSTER_CLIENT_IP", "HTTP_FORWARDED_FOR", "HTTP_FORWARDED", "REMOTE_ADDR" };


    //http://stackoverflow.com/questions/892618/create-a-hashcode-of-two-numbers
    public static int computeHashCodes(IEnumerable<Object> items) { return items.Select(item => item == null ? 0 : item.GetHashCode()).Aggregate(23, (hash, itemHash) => hash * 31 + itemHash); }
    public static int computeHashCode(params object[] items) { return computeHashCodes(items); }
    public static int computeCompare<T>(T o1, T o2, params Func<T, object>[] parts) {
      foreach (var p in parts) {
        IComparable c1 = (IComparable)p(o1); IComparable c2 = (IComparable)p(o2);
        if (c1 == null) { if (c2 == null) continue; else return 1; } else if (c2 == null) return -1;
        var res = c1.CompareTo(c2); if (res != 0) return res;
      }
      return 0;
    }

    public static void FtpUpload(string ftpUrl, string userName, string password, string folderName, string fileName, byte[] file) {
      FtpWebRequest request = WebRequest.Create(new Uri(string.Format(@"ftp://{0}/{1}{2}", ftpUrl, folderName == null ? null : folderName + "/", fileName))) as FtpWebRequest;
      request.Method = WebRequestMethods.Ftp.UploadFile;
      request.UseBinary = true;
      request.UsePassive = true;
      request.KeepAlive = true;
      request.Credentials = new NetworkCredential(userName, password);
      request.ContentLength = file.Length;
      using (Stream requestStream = request.GetRequestStream()) requestStream.Write(file, 0, file.Length);
    }

    public static void FtpDownload(string ftpUrl, string userName, string password, string folderName, string fileName, Action<Stream> save) {
      FtpWebRequest request = WebRequest.Create(new Uri(string.Format(@"ftp://{0}/{1}{2}", ftpUrl, folderName == null ? null : folderName + "/", fileName))) as FtpWebRequest;
      request.Method = WebRequestMethods.Ftp.DownloadFile;
      request.UseBinary = true;
      request.UsePassive = true;
      request.KeepAlive = true;
      request.Credentials = new NetworkCredential(userName, password);
      FtpWebResponse response = (FtpWebResponse)request.GetResponse();
      using (Stream responseStream = response.GetResponseStream()) save(responseStream);
    }

    public static IEnumerable<string> scanDir(string basicPath, Regex filter, string mask = "*.*") {
      return Directory.EnumerateFiles(basicPath, mask, SearchOption.AllDirectories).Select(f => f.ToLower()).Where(f => filter.IsMatch(f));
    }

    //public static string GetImageFilenameExtension(ImageFormat format) {
    //  return ImageCodecInfo.GetImageEncoders()
    //                       .First(x => x.FormatID == format.Guid)
    //                       .FilenameExtension
    //                       .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
    //                       .First()
    //                       .Trim('*')
    //                       .ToLower();
    //}

    public static string InnerXml(XNode node) {
      using (var reader = node.CreateReader()) {
        reader.MoveToContent();
        return reader.ReadInnerXml();
      }
    }

    public static string packStr(string str) {
      return LowUtils.Base64Decode(LowUtils.encrypt(Encoding.UTF8.GetBytes(str)));
    }

    public static string unpackStr(string str) {
      var data = LowUtils.decrypt(LowUtils.Base64Encode(str));
      return Encoding.UTF8.GetString(data, 0, data.Length);
    }

    public static string JsObjectEncode(object obj) {
      return packStr(JsonConvert.SerializeObject(obj));
    }

    public static byte[] JsObjectEncodeBin(object obj) {
      return LowUtils.encrypt(Encoding.UTF8.GetBytes(LowUtils.JSONEncode(obj)));
    }

    public static T JsObjectDecode<T>(string str) {
      return JsonConvert.DeserializeObject<T>(unpackStr(str));
    }

    public static string JSONEncode(object obj, IEnumerable<Type> knownTypes) {
      return SerializeToJSONObject(obj, knownTypes);
    }

    public static string JSONEncode(object obj) {
      return JsonConvert.SerializeObject(obj);
    }

    public static string JSONEncodeDict(IEnumerable<object> dict, StringBuilder sb = null) {
      if (sb == null) sb = new StringBuilder();
      sb.Append('{');
      if (dict != null) foreach (var kv in dict) {
          string k; string v; bool isObj;
          if (kv is KeyValuePair<string, string>) {
            isObj = false;
            var skv = (KeyValuePair<string, string>)kv; k = skv.Key; v = skv.Value;
          } else if (kv is KeyValuePair<string, Dictionary<string, string>>) {
            isObj = true;
            var skv = (KeyValuePair<string, Dictionary<string, string>>)kv; k = skv.Key; v = skv.Value == null ? "null" : JSONEncodeDict(skv.Value.Cast<object>());
          } else
            throw new Exception();
          sb.Append('"'); sb.Append(HttpUtility.JavaScriptStringEncode(k)); sb.Append('"');
          sb.Append(':');
          if (isObj) sb.Append(v); else { sb.Append('"'); sb.Append(HttpUtility.JavaScriptStringEncode(v)); sb.Append('"'); }
          sb.Append(',');
        }
      sb.Length = sb.Length - 1;
      sb.Append('}');
      return sb.ToString();
    }

    public static T JSONDecode<T>(string str) {
      return DeserializeJSONObject<T>(str);
    }

    static Regex removeType = new Regex("\"__type\":\"([^\\\"]|\\.)*\",", RegexOptions.Singleline);

    public static string SerializeToJSONObject(object obj, IEnumerable<Type> knownTypes) {
      var serializer = new DataContractJsonSerializer(obj.GetType(), knownTypes);
      var ms = new MemoryStream();
      serializer.WriteObject(ms, obj);
      return removeType.Replace(Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length), "");
    }
    public static string SerializeToJSONObject(object obj) {
      var serializer = new DataContractJsonSerializer(obj.GetType());
      var ms = new MemoryStream();
      serializer.WriteObject(ms, obj);
      return Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
    }

    public static T DeserializeJSONObject<T>(string response) {
      var serializer = new DataContractJsonSerializer(typeof(T));
      using (var mo = new MemoryStream(Encoding.UTF8.GetBytes(response)))
        return (T)serializer.ReadObject(mo);
    }

    public static object DeserializeJSONObject(Type tp, string response) {
      var serializer = new DataContractJsonSerializer(tp);
      using (var mo = new MemoryStream(Encoding.UTF8.GetBytes(response)))
        return serializer.ReadObject(mo);
    }

    public static short intDateToDay(Int64 intDate) { return (short)((double)intDate / msecInDay); }
    public static DateTime datDateToDate(short intDay) { return jsStart.AddDays(intDay); }

    const int msecInDay = 1000 * 60 * 60 * 24;
    public static Int64 DateToInt(DateTime dt, long? def = null) {
      if (dt < jsStart) {
        if (def != null) return (Int64)def;
        dt = jsStart;
      }
      TimeSpan ts = dt - jsStart;
      return (long)(ts.TotalMilliseconds + 0.5);
    }
    public static DateTime IntToDate(Int64 val, DateTime? obj = null) {
      return obj == null ? jsStart.AddMilliseconds(val) : (DateTime)obj;
    }
    public static DateTime jsStart = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);


    public static long DateToJsGetTime(DateTime dt) {
      return (long)((dt.ToUniversalTime().Ticks - DatetimeMinTimeTicks) / 10000);
    }
    public static DateTime JsGetTimeToDate(long ms) {
      return jsStart.AddMilliseconds(ms);
    }
    static DateTime jsStartNew = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    static long DatetimeMinTimeTicks = jsStartNew.Ticks;

    public static void EnableViewState(Page pg) {
      pg.EnableViewState = true; MasterPage mp = pg.Master; while (mp != null) { mp.EnableViewState = true; mp = mp.Master; }
    }
    //private static string cryptkey = "cryptkey"; // TODO zvolit heslo a asi není dobré mít heslo ve statické promìnné

    //TRANS support - staticke events
    public static TransAddStringEvent TransAddString;
    public static TransGetStringEvent TransGetString;

    public static IEnumerable<string> filesFromDir(string dir, string ext) {
      foreach (string fn in Directory.GetFiles(dir, ext == null ? "*.*" : ext))
        yield return fn;
      foreach (string subDir in Directory.GetDirectories(dir))
        foreach (string d in filesFromDir(subDir, ext)) yield return d;
    }

    public static byte[] HexEncode(string s) {
      return StringToBytes(s);
    }
    public static string HexDecode(byte[] bytes) {
      return BytesToString(bytes);
    }
    public static byte[] Base64Encode(string str) {
      return Convert.FromBase64String(str);
    }
    public static string Base64Decode(byte[] data) {
      return Convert.ToBase64String(data);
    }
    public static byte[] UTF8Encode(string str) {
      return Encoding.UTF8.GetBytes(str);
    }
    public static string UTF8Decode(byte[] data) {
      return Encoding.UTF8.GetString(data, 0, data.Length);
    }

    /*/// <summary> Zašifruje heslo pevným klíèem </summary>
    /// <param name="password"> heslo </param>
    /// <returns> zašifrované heslo </returns>
    public static string HashPassword(string password) {
      RC4Engine eng = new RC4Engine();
      eng.EncryptionKey = cryptkey;
      eng.InClearText = password;
      eng.Encrypt();
      return eng.CryptedText;
    }

    /// <summary> Dešifruje heslo zašifrované pevným klíèem </summary>
    /// <param name="password"> zašifrované heslo </param>
    /// <returns> heslo </returns>
    public static string DehashPassword(string password) {
      RC4Engine eng = new RC4Engine();
      eng.EncryptionKey = cryptkey;
      eng.CryptedText = password;
      eng.Decrypt();
      return eng.InClearText;
    }*/

    // Get the key and IV.
    static byte[] cryptKey = new byte[] { 175, 208, 15, 226, 93, 35, 194, 4, 112, 29, 139, 207, 79, 114, 35, 211 };
    static byte[] cryptIV = new byte[] { 11, 99, 144, 8, 78, 133, 147, 31 };

    public static byte[] Encrypt(byte[] data) {
      RC2CryptoServiceProvider rc2CSP = new RC2CryptoServiceProvider();
      // Get an encryptor.
      ICryptoTransform encryptor = rc2CSP.CreateEncryptor(cryptKey, cryptIV);
      // Encrypt the data as an array of encrypted bytes in memory.
      MemoryStream buf = new MemoryStream();
      CryptoStream csEncrypt = new CryptoStream(buf, encryptor, CryptoStreamMode.Write);
      // Write all data to the crypto stream and flush it.
      csEncrypt.Write(data, 0, data.Length);
      csEncrypt.FlushFinalBlock();
      return buf.ToArray();
    }
    public static byte[] Decrypt(byte[] data) {
      RC2CryptoServiceProvider rc2CSP = new RC2CryptoServiceProvider();
      ICryptoTransform decryptor = rc2CSP.CreateDecryptor(cryptKey, cryptIV);
      MemoryStream msDecrypt = new MemoryStream(data);
      CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
      byte[] res = new byte[data.Length];
      csDecrypt.Read(res, 0, data.Length);
      return res;
    }

    /*public static Type FindClassType(string id)
    {
      TObjId objId = new TObjId(id);
      return FindClassType(objId.SpaceId, objId.GlobalId, true);
    }
    /// <summary>Nalezne typ.</summary>
    /// <param name="assemblyName">Assembly pro LoadWithPartialName funkci</param>
    /// <param name="className">Jmeno tridy, vcetne namespace</param>
    public static Type FindClassType(string assemblyName, string className, bool raiseException)
    {
      //Assembly ass = Assembly.LoadWithPartialName (assemblyName);
      Assembly ass = assemblyName == null || assemblyName == "~" ? ObjectSectionHandler.RootAssembly : Assembly.Load(assemblyName);
      if (ass == null)
      {
        if (raiseException) throw new LMException("FindClassType: cannot find assembly {0}", assemblyName);
        return null;
      }
      Type tp = ass.GetType(className, false, true);
      if (tp == null)
      {
        if (raiseException) throw new LMException("FindClassType: cannot find class {0}", className);
        return null;
      }
      return tp;
    }


    public static object CreateClass(string id)
    {
      Type tp = FindClassType(id);
      return Activator.CreateInstance(tp);
    }
    /// <summary>Nalezne typ a vytvori objekt.</summary>
    /// <param name="assemblyName">Assembly pro LoadWithPartialName funkci</param>
    /// <param name="className">Jmeno tridy, vcetne namespace</param>
    public static object CreateClass(string assemblyName, string className, bool raiseException)
    {
      Type tp = FindClassType(assemblyName, className, raiseException);
      if (tp == null) return null;
      return Activator.CreateInstance(tp);
    }*/

    public static void AdjustFileDir(string fileName) {
      string dir = Path.GetDirectoryName(fileName);
      if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }

    public static void AdjustFileCopy(string srcFile, string destFile, bool mustOverride) {
      if (!mustOverride && File.Exists(destFile)) return;
      AdjustFileDir(destFile);
      File.Copy(srcFile, destFile, true);
    }

    public static void AdjustFileMove(string srcFile, string destFile) {
      if (File.Exists(destFile)) File.Delete(destFile);
      AdjustFileDir(destFile);
      File.Move(srcFile, destFile);
    }

    public static void AdjustDir(string dir) {
      if (dir.EndsWith("\\")) dir = dir.Remove(dir.Length - 1);
      if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }

    public static void EmptyDir(string dir) {
      if (!Directory.Exists(dir)) return;
      foreach (string file in Directory.GetFiles(dir))
        try { File.Delete(file); } catch (Exception exp) { throw new Exception(file, exp); }
      foreach (string subDir in Directory.GetDirectories(dir)) {
        EmptyDir(subDir);
        try { Directory.Delete(subDir); } catch (Exception exp) { throw new Exception(subDir, exp); }
      }
    }

    public static void EmptyDirNoExp(string dir) {
      if (!Directory.Exists(dir)) return;
      foreach (string file in Directory.GetFiles(dir))
        try { File.Delete(file); } catch { }
      foreach (string subDir in Directory.GetDirectories(dir)) {
        EmptyDirNoExp(subDir);
        try { Directory.Delete(subDir); } catch { }
      }
    }

    public static void DeleteDir(string dir, string mask) {
      if (!Directory.Exists(dir)) return;
      string[] files = Directory.GetFiles(dir, mask);
      foreach (string file in files)
        try { File.Delete(file); } catch (Exception exp) { throw new Exception(file, exp); }
    }

    public static void DeleteDir(string dir) {
      if (!Directory.Exists(dir)) return;
      string[] files = Directory.GetFiles(dir);
      foreach (string file in files)
        try { File.Delete(file); } catch (Exception exp) { throw new Exception(file, exp); }
      foreach (string subDir in Directory.GetDirectories(dir))
        try { Directory.Delete(subDir); } catch (Exception exp) { throw new Exception(subDir, exp); }
      try { Directory.Delete(dir); } catch (Exception exp) { throw new Exception(dir, exp); }
    }

    public static void DeleteDirNoExp(string dir) {
      if (!Directory.Exists(dir)) return;
      string[] files = Directory.GetFiles(dir);
      foreach (string file in files)
        try { File.Delete(file); } catch { }
      foreach (string subDir in Directory.GetDirectories(dir))
        try { Directory.Delete(subDir); } catch { }
      try { Directory.Delete(dir); } catch { }
    }

    public static string runCmdBatch(IEnumerable<string> lines) {
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
        return err;
        //output = null;
      }
    }

    public static string runExecutable(string path, string arguments, out string output) {
      var procInfo = new System.Diagnostics.ProcessStartInfo() {
        WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized,
        CreateNoWindow = true,
        FileName = path,
        Arguments = arguments,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardInput = false,
        RedirectStandardError = true,
        WorkingDirectory = @"c:\temp\",
      };

      using (var proc = System.Diagnostics.Process.Start(procInfo))
      using (var sErr = proc.StandardError)
      using (var sOut = proc.StandardOutput) {
        proc.WaitForExit();
        var err = sErr.ReadToEnd().Trim();
        output = sOut.ReadToEnd().Trim();
        if (string.IsNullOrEmpty(output)) output = null;
        return string.IsNullOrEmpty(err) ? null : err;
      }
    }


    public static void CopyFolder(string source, string destination) {
      string xcopyPath = Environment.GetEnvironmentVariable("WINDIR") + @"\System32\xcopy.exe";
      ProcessStartInfo info = new ProcessStartInfo(xcopyPath);
      //info.UseShellExecute = false;
      //info.RedirectStandardOutput = true;
      info.Arguments = string.Format("\"{0}\" \"{1}\" /E /I", source, destination);

      Process process = Process.Start(info);
      process.WaitForExit();
      //string result = process.StandardOutput.ReadToEnd();

      if (process.ExitCode != 0) {
        // Or your own custom exception, or just return false if you prefer.
        throw new InvalidOperationException(string.Format("Failed to copy {0} to {1}: {2}", source, destination, ""));
      }
    }

    public static void CopyDir(string srcDir, string destDir, string mask) {
      if (!Directory.Exists(srcDir)) return;
      if (mask == null) mask = "*.*";
      srcDir = srcDir.TrimEnd('\\'); destDir = destDir.TrimEnd('\\');
      AdjustDir(destDir);
      foreach (string file in Directory.GetFiles(srcDir, mask)) {
        string destFn = destDir + @"\" + Path.GetFileName(file);
        File.Copy(file, destFn, true);
      }
      foreach (string dir in Directory.GetDirectories(srcDir)) {
        string destFn = destDir + @"\" + Path.GetFileName(dir);
        CopyDir(dir, destFn, mask);
      }
    }

    public delegate bool CopyDirEvent(string fn);

    public static void CopyDir(string srcDir, CopyDirEvent doCopy, string destDir) {
      if (!Directory.Exists(srcDir)) return;
      AdjustDir(destDir);
      foreach (string file in Directory.GetFiles(srcDir)) {
        if (!doCopy(file)) continue;
        string destFn = destDir + @"\" + Path.GetFileName(file);
        File.Copy(file, destFn, true);
      }
      foreach (string dir in Directory.GetDirectories(srcDir)) {
        if (!doCopy(dir)) continue;
        string destFn = destDir + @"\" + Path.GetFileName(dir);
        CopyDir(dir, doCopy, destFn);
      }
    }

    public static string EpaDelphiUrlDecode(string url) {
      if (url == null) return null;
      StringBuilder sb = new StringBuilder();
      int idx = 0; string s;
      while (idx < url.Length) {
        if (url[idx] == '@') {
          s = url.Substring(idx + 1, 4);
          idx += 4;
          sb.Append(Convert.ToChar(Convert.ToInt16(s, 16)));
        } else if (url[idx] == '*') {
          s = url.Substring(idx + 1, 2);
          idx += 2;
          sb.Append(Convert.ToChar(Convert.ToByte(s, 16)));
        } else
          sb.Append(url[idx]);
        idx++;
      }
      return sb.ToString();
    }
    /// <summary>Nacteni prvniho sheetu z excel souboru do netypovaneho datasetu</summary>
    //Není-li HKEY_LOCAL_MACHINE/SOFTWARE/Microsoft/Jet/4.0/Engines/Excel/TypeGuessRows rovno nule a není-li v prvních 8 øádcích 
    //øetìz delší než 255 znakù, provede zarovnání na 255 znakù.
    public static DataSet ReadExcelTable(string excelFileName, bool useHeader) {
      string connString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties='Excel 8.0;" + (useHeader ? "" : "HDR=No;") + "IMEX=1'";
      DataSet ds = new DataSet();
      using (OleDbConnection conn = new OleDbConnection(string.Format(connString, excelFileName))) {
        conn.Open();
        DataTable schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
        string firstSheet = (string)schema.Rows[0]["TABLE_NAME"];
        using (OleDbDataAdapter adapter = new OleDbDataAdapter(string.Format("SELECT * FROM [{0}]", firstSheet), conn))
          adapter.Fill(ds, "Table");
      }
      return ds;
    }
    [DllImport("Kernel32.dll")]
    private static extern Int32 FileTimeToDosDateTime(ref System.Runtime.InteropServices.ComTypes.FILETIME fileTime, ref UInt16 date, ref UInt16 time);
    public static int DateTimeToDosDateTime(DateTime date) {
      long fileTime = date.ToFileTime();
      System.Runtime.InteropServices.ComTypes.FILETIME ft;
      ft.dwHighDateTime = (int)(fileTime >> 32);
      ft.dwLowDateTime = (int)fileTime;
      UInt16 dt = 0; UInt16 time = 0;
      FileTimeToDosDateTime(ref ft, ref dt, ref time);
      return ((int)dt << 16) + time;
    }
    [DllImport("Kernel32.dll")]
    private static extern Int32 DosDateTimeToFileTime(UInt16 date, UInt16 time, ref long fileTime);
    public static DateTime DosDateTimeToDateTime(int dosTime) {
      UInt16 date = (UInt16)(dosTime >> 16);
      UInt16 time = (UInt16)dosTime;
      long fileTime = 0;
      DosDateTimeToFileTime(date, time, ref fileTime);
      return DateTime.FromFileTime(fileTime);
    }
    static double GetInternetExplorerVersion(HttpRequest request) {
      try {
        string userAgent = request.ServerVariables["HTTP_USER_AGENT"];
        if (userAgent == null) return 0;
        userAgent = userAgent.ToUpper();
        if (userAgent.IndexOf("MSIE") != -1) {
          string versionText = userAgent.Substring(userAgent.IndexOf("MSIE") + 5);
          versionText = versionText.Substring(0, versionText.IndexOf(";"));
          // Verze je vzdy v en-US formatu cisla
          double version = Convert.ToDouble(versionText, new CultureInfo("en-US").NumberFormat);
          return version;
        }
        return 0;
      } catch {
        return 0;
      }
    }

    private const double RequiredInternetExplorerVersion = 5.5;

    public static bool ExplorerOK(HttpRequest request) {
      return GetInternetExplorerVersion(request) >= RequiredInternetExplorerVersion;
    }

    public static string NetLangToWinLang(string netLang) {
      if (netLang == null || netLang.Length < 5 || netLang[2] != '-')
        return netLang;
      switch (netLang.ToLower()) {
        case "cs-cz": return "CSY";
        case "sk-sk": return "SKY";
        case "en-us":
        case "en-gb": return "ENG";
        case "de-de": return "DEU";
        case "tr-tr": return "TRK";
        case "vi-vn": return "VIT";
        case "es-es": return "ESN";
        case "ru-ru": return "RUS";
        default: return "NetLangToWinLang error " + netLang;
      }
    }

    public static string WinLangToNetLang(string winLang) {
      if (winLang == null || winLang.Length != 3)
        return winLang;
      switch (winLang.ToUpper()) {
        case "CSY": return "cs-CZ";
        case "SKY": return "sk-SK";
        case "ENG": return "en-GB";
        case "DEU": return "de-DE";
        case "TRK": return "tr-TR";
        case "VIT": return "vi-VN";
        case "ESN": return "es-ES";
        case "RUS": return "ru-RU";
        default: return "WinLangToNetLang error" + winLang;
      }
    }

    public static string ShortDateTime(DateTime dt) {
      return string.Concat(dt.ToShortDateString(), " (", dt.ToShortTimeString(), ")");
    }
    public static DateTime SqlDateTime(DateTime dt) {
      return dt.AddMilliseconds(-dt.Millisecond);
    }
    public static string SqlDateTimeString(DateTime dt) {
      return dt.ToString("yyyy-MM-dd HH:mm:ss.000");
    }
    public static Control FindControlEx(Control ctrl, string id) {
      id = id.ToLower();
      if (ctrl.ID != null && ctrl.ID.ToLower() == id) return ctrl;
      foreach (Control ctr in ctrl.Controls) {
        Control res = FindControlEx(ctr, id);
        if (res != null) return res;
      }
      return null;
    }
    public static Control FindControlEx(Control ctrl, CtrlCondition cond) {
      if (cond(ctrl)) return ctrl;
      foreach (Control ctr in ctrl.Controls) {
        Control res = FindControlEx(ctr, cond);
        if (res != null) return res;
      }
      return null;
    }
    public static IEnumerable<Control> FindControlsEx(Control ctrl, CtrlCondition cond) {
      if (cond(ctrl)) yield return ctrl;
      foreach (Control ctr in ctrl.Controls)
        foreach (Control subCtrl in FindControlsEx(ctr, cond))
          yield return subCtrl;
    }
    public static IEnumerable<Control> AllControls(Control ctrl) {
      yield return ctrl;
      foreach (Control ctr in ctrl.Controls)
        foreach (Control subCtrl in AllControls(ctr))
          yield return subCtrl;
    }
    public static Control FindParent(Control ctrl, CtrlCondition cond) {
      do {
        if (cond(ctrl)) return ctrl;
        ctrl = ctrl.Parent;
      }
      while (ctrl != null);
      return null;
    }

    public static IEnumerable<Control> EnumControls(Control root, Type tp) {
      foreach (Control ctr in root.Controls) {
        if (tp == null || ctr.GetType() == tp) yield return ctr;
        foreach (Control subCtr in EnumControls(ctr, tp)) yield return subCtr;
      }
    }
    public delegate bool CtrlCondition(Control obj);
    public static IEnumerable<Control> EnumControlsEx(Control root, CtrlCondition cond) {
      foreach (Control ctr in root.Controls) {
        if (cond(ctr)) yield return ctr;
        foreach (Control subCtr in EnumControlsEx(ctr, cond)) yield return subCtr;
      }
    }

    public static object ObjectPropFld(string name, object obj, string defaultValue = null) {
      MemberInfo fld = obj.GetType().GetMember(name).FirstOrDefault();
      if (fld is PropertyInfo)
        return ((PropertyInfo)fld).GetValue(obj, null);
      else if (fld is FieldInfo)
        return ((FieldInfo)fld).GetValue(obj);
      else
        return defaultValue ?? "Missing prop: " + name;
    }
    public static string FormatNamedProps(string mask, Func<string, string> findValue) {
      return formatExRegex.Replace(mask, m => findValue(m.Value.Substring(2, m.Value.Length - 4)));
    }
    static Regex formatExRegex = new Regex(@"\[#.*?#\]", RegexOptions.Singleline);

    public delegate string findValueEvent(string name);
    static char[] fmtChars = new char[] { '[', '#', '#', ']' };
    public static string FormatEx(string mask, findValueEvent findValue) {
      return FormatEx(mask, findValue, new StringBuilder());
    }
    public static string FormatEx(string mask, findValueEvent findValue, StringBuilder buf) {
      if (mask == null) return null;
      if (mask == string.Empty) return string.Empty;
      if (buf == null) buf = new StringBuilder();
      buf.Length = 0;
      int status = 0;
      char ch; int begIdx = 0; string id;
      for (int i = 0; i < mask.Length; i++) {
        ch = mask[i];
        switch (status) {
          case 0:
            if (ch == fmtChars[0]) status = 1;
            else buf.Append(ch);
            break;
          case 1:
            if (ch == fmtChars[1]) { begIdx = i + 1; status = 3; } else { buf.Append(fmtChars[0]); buf.Append(ch); status = 0; }
            break;
          /*case 2:
            if (char.IsLetterOrDigit(ch)) { begIdx = i; status = 3; } else if (ch != ' ') throw new Exception(string.Format("FormatEx error: in {0} on {1} position.", mask, i));
            break;*/
          case 3:
            if (ch == fmtChars[2]) {
              if (i + 1 >= mask.Length)
                throw new Exception(string.Format("FormatEx error: in {0} on {1} position.", mask, i));
              if (mask[i + 1] != fmtChars[3]) continue;
              id = mask.Substring(begIdx, i - begIdx);
              status = 0;
              string val = findValue(id);
              if (val != null)
                buf.Append(val);
              else
                throw new Exception(string.Format("FormatEx error: in <{0}> cannot replace <{1}> string.", mask, id));
              status = 5;
            }
            //else if (ch == fmtChars[2]) status = 5;
            //else throw new Exception(string.Format("FormatEx error: in {0} on {1} position.", mask, i));
            //}
            break;
          /*case 4:
            if (ch == ' ') continue;
            if (ch == fmtChars[2]) status = 5;
            else throw new Exception(string.Format("FormatEx error: in {0} on {1} position.", mask, i));
            break;*/
          case 5:
            if (ch == fmtChars[3]) { status = 0; continue; }
            throw new Exception(string.Format("FormatEx error: in {0} on {1} position.", mask, i));
        }
      }
      return buf.ToString();
    }

    public static string FormatEx(string mask, params object[] nameValues) {
      return FormatEx(mask, delegate (string id) {
        for (int j = 0; j < nameValues.Length; j += 2)
          if ((string)nameValues[j] == id) return nameValues[j + 1].ToString();
        return null;
      });
    }
    public static string FormatExDict(string mask, Dictionary<string, string> dict) {
      return FormatEx(mask, delegate (string id) {
        string res;
        return dict.TryGetValue(id, out res) ? res : null;
      }, null);
    }
    public static string FormatEx(string mask, Dictionary<string, string> dict, StringBuilder sb) {
      return FormatEx(mask, delegate (string id) {
        string res;
        return dict.TryGetValue(id, out res) ? res : null;
      }, sb);
    }
    public static string FormatEx(string mask, Dictionary<string, object> dict, StringBuilder sb) {
      return FormatEx(mask, delegate (string id) {
        object res;
        return dict.TryGetValue(id, out res) ? res.ToString() : null;
      }, sb);
    }

    public static IEnumerable<SiteMapNode> allNodes(SiteMapNode node) {
      return allNodes(node, null);
    }
    public delegate bool SiteMapNodeCondition(SiteMapNode nd);
    public static IEnumerable<SiteMapNode> allNodes(SiteMapNode node, SiteMapNodeCondition cond) {
      if (cond == null || cond(node)) yield return node;
      if (!node.HasChildNodes) yield break;
      foreach (SiteMapNode nd in node.ChildNodes)
        foreach (SiteMapNode subNd in allNodes(nd, cond))
          yield return subNd;
    }
    public static IEnumerable<SiteMapNode> parentNodes(SiteMapNode node, SiteMapNodeCondition cond) {
      if (node.ParentNode == null) yield break;
      if (cond == null || cond(node)) yield return node;
      foreach (SiteMapNode subNd in parentNodes(node.ParentNode, cond)) yield return subNd;
    }

    public static IEnumerable<SiteMapNode> parentNodes(SiteMapNode node, bool incSelf) {
      if (incSelf) yield return node;
      while (node.ParentNode != null) { node = node.ParentNode; yield return node; }
    }
    public static string JSONToId(string spaceId, string globalId) {
      globalId = globalId.ToLower().Replace("_", "_u").Replace("/", "_s").Replace(".", "_d").Replace("-", "_c");
      spaceId = spaceId.ToLower().Replace("_", "_u").Replace("/", "_s").Replace(".", "_d").Replace("-", "_c");
      return spaceId + "_x" + globalId;
    }
    public static void JSONFromId(string id, out string spaceId, out string globalId) {
      int idx = id.IndexOf("_x");
      spaceId = id.Substring(0, idx).Replace("_s", "/").Replace("_d", ".").Replace("_u", "_").Replace("_c", "-");
      globalId = id.Substring(idx + 2).Replace("_s", "/").Replace("_d", ".").Replace("_u", "_").Replace("_c", "-");
    }

    public static string c_cookieName = "LMTicket";
    public const ushort encryptKey = 18475;

    const ushort c1 = 52845; const ushort c2 = 22719;
    public static void Encrypt(ref byte[] data, int start, int len, ushort key) {
      for (int i = 0; i < data.Length; i++) {
        data[i] = (byte)(data[i] ^ (key >> 8));
        key = (ushort)((data[i] + key) * c1 + c2);
      }
    }
    public static void Decrypt(ref byte[] data, int start, int len, ushort key) {
      byte old;
      for (int i = 0; i < data.Length; i++) {
        old = data[i];
        data[i] = (byte)(old ^ (key >> 8));
        key = (ushort)((old + key) * c1 + c2);
      }
    }
    public static void Encrypt(ref byte[] data, ushort key) {
      Encrypt(ref data, 0, data.Length, key);
    }
    public static byte[] encrypt(byte[] data) {
      return Encrypt(data, encryptKey);
    }
    public static byte[] decrypt(byte[] data) {
      return Decrypt(data, encryptKey);
    }
    public static byte[] Encrypt(byte[] data, ushort key) {
      Encrypt(ref data, key); return data;
    }
    public static void Decrypt(ref byte[] data, ushort key) {
      Decrypt(ref data, 0, data.Length, key);
    }
    public static byte[] Decrypt(byte[] data, ushort key) {
      Decrypt(ref data, 0, data.Length, key);
      return data;
    }
    public static byte[] Decrypt(Stream str, ushort key) {
      byte[] data = StreamToByte(str);
      Decrypt(ref data, key);
      return data;
    }
    public static string Encrypt(string str, ushort key = encryptKey) {
      if (str == null) return null;
      byte[] data = Encoding.Unicode.GetBytes(str);
      Encrypt(ref data, key);
      return Convert.ToBase64String(data);
    }
    public static string Decrypt(string str, ushort key = encryptKey) {
      if (str == null) return null;
      byte[] data = Convert.FromBase64String(str);
      Decrypt(ref data, key);
      return Encoding.Unicode.GetString(data);
    }
    public static string EncryptHex(string str, ushort key = encryptKey) {
      byte[] data = UTF8Encode(str);
      Encrypt(ref data, key);
      return BytesToString(data);
    }
    public static string DecryptHex(string str, ushort key = encryptKey) {
      byte[] data = StringToBytes(str);
      Decrypt(ref data, key);
      return UTF8Decode(data);
    }
    public static string RelativeUrl(string url) {
      return VirtualPathUtility.MakeRelative(HttpContext.Current.Request.Url.AbsolutePath, url).ToLower();
    }
    public static string RelativeUrl(SiteMapNode nd) {
      return RelativeUrl(nd.Url);
    }

    public static string FullUrl(string relativeUrl) {
      return HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) + VirtualPathUtility.ToAbsolute(relativeUrl);
    }

    public static DateTime startDate = new DateTime(2007, 1, 1);

    public static bool CompareByteArrays(byte[] data1, byte[] data2) {
      // If both are null, they're equal
      if (data1 == null && data2 == null)
        return true;
      // If either but not both are null, they're not equal
      if (data1 == null || data2 == null)
        return false;
      if (data1.Length != data2.Length)
        return false;
      for (int i = 0; i < data1.Length; i++)
        if (data1[i] != data2[i])
          return false;
      return true;
    }
    static char[] hex = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
    public static string BytesToString(byte[] bytes) {
      if (bytes == null) return null;
      char[] res = new char[bytes.Length * 2];
      for (int i = 0; i < bytes.Length; i++) {
        res[i * 2] = hex[bytes[i] >> 4];
        res[i * 2 + 1] = hex[bytes[i] & 0x0F];
      }
      return new string(res);
    }
    public static string ByteToString(byte b) {
      char[] res = new char[2];
      res[0] = hex[b >> 4];
      res[1] = hex[b & 0x0F];
      return new string(res);
    }
    static byte charToByte(char ch) {
      if (ch >= '0' && ch <= '9')
        return (byte)((byte)ch - (byte)'0');
      else if (ch >= 'A' && ch <= 'F')
        return (byte)((byte)ch - (byte)'A' + 10);
      return 0;
    }
    public static byte[] StringToBytes(string s) {
      if (string.IsNullOrEmpty(s)) return null;
      s = s.ToUpper();
      s = s.Replace('O', '0');
      byte[] res = new byte[s.Length >> 1];
      for (int i = 0; i < res.Length; i++)
        res[i] = (byte)((charToByte(s[i * 2]) << 4) + charToByte(s[i * 2 + 1]));
      return res;
    }

    public static XNamespace xf = "http://www.microsoft.com/LinqToXmlTransform/2007";
    public static XName at = xf + "ApplyTransforms";    // Build a transformed XML tree per the annotations

    static XElement XForm(XElement source) {

      if (source.Annotation<XElement>() != null) {
        XElement anno = source.Annotation<XElement>();
        return new XElement(anno.Name,
            anno.Attributes(),
            anno
            .Nodes()
            .Select(
                (XNode n) => {
                  XElement annoEl = n as XElement;
                  if (annoEl != null) {
                    if (annoEl.Name == at)
                      return (object)(
                          source.Nodes()
                          .Select(
                              (XNode n2) => {
                                XElement e2 = n2 as XElement;
                                if (e2 == null)
                                  return n2;
                                else
                                  return XForm(e2);
                              }
                          )
                      );
                    else
                      return n;
                  } else
                    return n;
                }
            )
        );
      } else {
        return new XElement(source.Name,
            source.Attributes(),
            source
                .Nodes()
                .Select(n => {
                  XElement el = n as XElement;
                  if (el == null)
                    return n;
                  else
                    return XForm(el);
                }
                )
        );
      }
    }
    public static Stream getStream(string fn, bool pathIsUrl, out long len) {
      len = 0;
      Stream res;
      if (!pathIsUrl) {
        res = new FileStream(fn, FileMode.Open, FileAccess.Read, FileShare.Read);
        len = res.Length;
      } else {
        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(fn);
        WebResponse resp = req.GetResponse();
        res = resp.GetResponseStream();
        len = resp.ContentLength;
      }
      return res;
    }

    public static Stream DownloadStream(string url) {
      HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
      req.Timeout = 600000; //10 minut
      WebResponse resp = req.GetResponse();
      return resp.GetResponseStream();
    }


    public static byte[] Download(string url) {
      HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
      req.Timeout = 600000; //10 minut
      WebResponse resp = req.GetResponse();
      using (Stream str = resp.GetResponseStream()) {
        MemoryStream memStr = new MemoryStream();
        CopyStream(str, memStr);
        return memStr.ToArray();
      }
    }

    public static string DownloadStr(string url) {
      return Encoding.UTF8.GetString(Download(url));
    }


    public static bool DownloadLargeFile(HttpContext ctx, string fn, bool pathIsUrl, string contentType, string contentDisposition) {
      if (fn == null) return false;
      try {
        long len;
        using (Stream fs = getStream(fn, pathIsUrl, out len)) {
          ctx.Server.ScriptTimeout = 30000000;
          ctx.Response.ClearContent();
          ctx.Response.AddHeader("Content-Length", len.ToString());
          ctx.Response.ContentType = contentType;
          ctx.Response.AddHeader("Content-Disposition", contentDisposition);
          byte[] buffer = new Byte[10000];
          long dataToRead = len;
          int length;
          while (dataToRead > 0) {
            // Verify that the client is connected.
            if (ctx.Response.IsClientConnected) {
              // Read the data in buffer.
              length = fs.Read(buffer, 0, 10000);
              // Write the data to the current output stream.
              ctx.Response.OutputStream.Write(buffer, 0, length);
              // Flush the data to the HTML output.
              ctx.Response.Flush();
              dataToRead = dataToRead - length;
            } else {
              //prevent infinite loop if user disconnects
              return false;
            }
          }
          return true;
        }
      } finally {
        ctx.Response.Close();
      }
    }

    public struct interval {
      public interval(int start, int end) { this.start = start; this.end = end; }
      public int start; public int end;
      public int skip { get { return start; } }
      public int take { get { return end - start + 1; } }
    }
    public static IEnumerable<interval> intervals(int count, int intLen) {
      int num = count / intLen;
      if (num * intLen < count) num++;
      for (int i = 0; i < num; i++) {
        int st = i * intLen; int en = st + intLen - 1;
        if (en > count - 1) en = count - 1;
        yield return new interval(st, en);
      }
    }

    public static void CopyStream(Stream inStr, Stream outStr) {
      CopyStream(inStr, outStr, 4096);
    }

    public static void CopyStream(Stream inStr, Stream outStr, int buffSize) {
      byte[] bs = new byte[buffSize];
      int numRead;
      while ((numRead = inStr.Read(bs, 0, bs.Length)) > 0)
        outStr.Write(bs, 0, numRead);
    }

    public static byte[] StreamToByte(Stream inStr) {
      MemoryStream buf = new MemoryStream();
      CopyStream(inStr, buf);
      return buf.ToArray();
    }

    public static void CopyStream(Stream input, int inpPos, int inpLen, Stream output) {
      byte[] buffer = new byte[32768];
      input.Seek(inpPos, SeekOrigin.Begin);
      while (inpLen > 0) {
        int len = inpLen > buffer.Length ? buffer.Length : inpLen;
        if (input.Read(buffer, 0, len) != len) throw new Exception();
        output.Write(buffer, 0, len);
        inpLen -= len;
      }
    }

    public static void ExpirePageCache(Page pg) {
      pg.Response.Cache.SetNoStore();
      pg.Response.Cache.SetCacheability(HttpCacheability.NoCache);
      pg.Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
      pg.Response.Cache.SetLastModified(DateTime.UtcNow);
      pg.Response.Cache.SetAllowResponseInBrowserHistory(false);
      if (pg.Header != null) {
        HtmlMeta metaTag = new HtmlMeta();
        HtmlHead HeadTag = (HtmlHead)pg.Header;
        metaTag.Attributes.Add("name", "Cache-Control");
        metaTag.Attributes.Add("content", "no-store, must-revalidate");
        HeadTag.Controls.Add(metaTag);
      }
      pg.Response.AddHeader("Cache-Control", "no-store, must-revalidate");
    }

    public static UInt32 IPAddressToInt(IPAddress addr) {
      UInt32 ipnum = 0;
      byte[] b = addr.GetAddressBytes();
      for (int i = 0; i < 4; ++i) {
        UInt32 y = b[i];
        if (y < 0) {
          y += 256;
        }
        ipnum += y << ((3 - i) * 8);
      }
      return ipnum;
    }

    public static UInt32 IPAddressToInt(string addr) {
      if (string.IsNullOrEmpty(addr)) return 0;
      return IPAddressToInt(IPAddress.Parse(addr));
    }

    public static int IndexOfEx(string s, char ch, int count) {
      int pos = -1;
      for (int i = 0; i < s.Length - 1; i++) if (s[i] == '/') { count--; if (count == 0) { pos = i; break; } }
      return pos;
    }
    public static bool SplitEx(string s, char ch, int count, out string part1, out string part2) {
      part1 = null; part2 = null;
      int idx = IndexOfEx(s, ch, count); if (idx < 0) return false;
      part1 = s.Substring(0, idx); part2 = s.Substring(idx + 1, s.Length - idx - 1);
      return true;
    }
    public static void RedirectTopFrame(HttpResponse response, string url) {
      response.Write("<script type=\"text/javascript\">\n" +
                     "if (parent != self) \n" +
                     "top.location.href = \"" + url + "\";\n" +
                     "else self.location.href = \"" + url + "\";\n" +
                     "</script>");
      response.End();
    }

    public static string ContentTypeToExtension(string contentType) {
      switch (contentType.ToLower()) {
        case "application/envoy": return "evy";
        case "application/fractals": return "fif";
        case "application/futuresplash": return "spl";
        case "application/hta": return "hta";
        case "application/internet-property-stream": return "acx";
        case "application/mac-binhex40": return "hqx";
        case "application/msword": return "doc";
        case "application/octet-stream": return "bin";
        case "application/oda": return "oda";
        case "application/olescript": return "axs";
        case "application/pdf": return "pdf";
        case "application/pics-rules": return "prf";
        case "application/pkcs10": return "p10";
        case "application/pkix-crl": return "crl";
        case "application/postscript": return "eps";
        case "application/rtf": return "rtf";
        case "application/set-payment-initiation": return "setpay";
        case "application/set-registration-initiation": return "setreg";
        case "application/vnd.ms-excel": return "xls";
        case "application/vnd.ms-outlook": return "msg";
        case "application/vnd.ms-pkicertstore": return "sst";
        case "application/vnd.ms-pkiseccat": return "cat";
        case "application/vnd.ms-pkistl": return "stl";
        case "application/vnd.ms-powerpoint": return "ppt";
        case "application/vnd.ms-project": return "mpp";
        case "application/vnd.ms-works": return "wps";
        case "application/winhlp": return "hlp";
        case "application/x-bcpio": return "bcpio";
        case "application/x-cdf": return "cdf";
        case "application/x-compress": return "z";
        case "application/x-compressed": return "tgz";
        case "application/x-cpio": return "cpio";
        case "application/x-csh": return "csh";
        case "application/x-director": return "dir";
        case "application/x-dvi": return "dvi";
        case "application/x-gtar": return "gtar";
        case "application/x-gzip": return "gz";
        case "application/x-hdf": return "hdf";
        case "application/x-internet-signup": return "ins";
        case "application/x-iphone": return "iii";
        case "application/x-javascript": return "js";
        case "application/x-latex": return "latex";
        case "application/x-msaccess": return "mdb";
        case "application/x-mscardfile": return "crd";
        case "application/x-msclip": return "clp";
        case "application/x-msdownload": return "dll";
        case "application/x-msmediaview": return "m14";
        case "application/x-msmetafile": return "wmf";
        case "application/x-msmoney": return "mny";
        case "application/x-mspublisher": return "pub";
        case "application/x-msschedule": return "scd";
        case "application/x-msterminal": return "trm";
        case "application/x-mswrite": return "wri";
        case "application/x-netcdf": return "cdf";
        case "application/x-perfmon": return "pma";
        case "application/x-pkcs12": return "p12";
        case "application/x-pkcs7-certificates": return "spc";
        case "application/x-pkcs7-certreqresp": return "p7r";
        case "application/x-pkcs7-mime": return "p7c";
        case "application/x-pkcs7-signature": return "p7s";
        case "application/x-sh": return "sh";
        case "application/x-shar": return "shar";
        case "application/x-shockwave-flash": return "swf";
        case "application/x-stuffit": return "sit";
        case "application/x-sv4cpio": return "sv4cpio";
        case "application/x-sv4crc": return "sv4crc";
        case "application/x-tar": return "tar";
        case "application/x-tcl": return "tcl";
        case "application/x-tex": return "tex";
        case "application/x-texinfo": return "texinfo";
        case "application/x-troff": return "tr";
        case "application/x-troff-man": return "man";
        case "application/x-troff-me": return "me";
        case "application/x-troff-ms": return "ms";
        case "application/x-ustar": return "ustar";
        case "application/x-wais-source": return "src";
        case "application/x-x509-ca-cert": return "cer";
        case "application/ynd.ms-pkipko": return "pko";
        case "application/zip": return "zip";
        case "audio/basic": return "au";
        case "audio/mid": return "mid";
        case "audio/mpeg": return "mp3";
        case "audio/x-aiff": return "aif";
        case "audio/x-mpegurl": return "m3u";
        case "audio/x-pn-realaudio": return "ram";
        case "audio/x-wav": return "wav";
        case "image/bmp": return "bmp";
        case "image/cis-cod": return "cod";
        case "image/gif": return "gif";
        case "image/ief": return "ief";
        case "image/jpeg": return "jpg";
        case "image/pipeg": return "jfif";
        case "image/svg+xml": return "svg";
        case "image/tiff": return "tif";
        case "image/x-cmu-raster": return "ras";
        case "image/x-cmx": return "cmx";
        case "image/x-icon": return "ico";
        case "image/x-portable-anymap": return "pnm";
        case "image/x-portable-bitmap": return "pbm";
        case "image/x-portable-graymap": return "pgm";
        case "image/x-portable-pixmap": return "ppm";
        case "image/x-rgb": return "rgb";
        case "image/x-xbitmap": return "xbm";
        case "image/x-xpixmap": return "xpm";
        case "image/x-xwindowdump": return "xwd";
        case "message/rfc822": return "mht";
        case "text/css": return "css";
        case "text/h323": return "323";
        case "text/html": return "htm";
        case "text/iuls": return "uls";
        case "text/plain": return "txt";
        case "text/richtext": return "rtx";
        case "text/scriptlet": return "sct";
        case "text/tab-separated-values": return "tsv";
        case "text/webviewhtml": return "htt";
        case "text/x-component": return "htc";
        case "text/x-setext": return "etx";
        case "text/x-vcard": return "vcf";
        case "video/mpeg": return "mpg";
        case "video/quicktime": return "mov";
        case "video/x-la-asf": return "lsf";
        case "video/x-ms-asf": return "asf";
        case "video/x-msvideo": return "avi";
        case "video/x-sgi-movie": return "movie";
        case "x-world/x-vrml": return "vrml";
        default: return "bin";
      }
    }

    public static short BoolToShort(bool? val) {
      return val == null ? (short)0 : (val == true ? (short)1 : (short)-1);
    }

    public static bool? ShortToBool(short val) {
      return val < 0 ? false : (val == 0 ? null : (bool?)true);
    }

    public static string fileBytesToString(string fn) {
      using (FileStream fs = new FileStream(fn, FileMode.Open, FileAccess.Read))
      using (BinaryReader rdr = new BinaryReader(fs, Encoding.Unicode)) {
        return bytesToString(rdr.ReadBytes((int)fs.Length));
      }
    }

    public static string bytesToString(byte[] data) {
      return Convert.ToBase64String(data);
    }

    public static byte[] stringToBytes(string str) {
      return Convert.FromBase64String(str);
    }


    public static T EnumParse<T>(string str) {
      return (T)Enum.Parse(typeof(T), str, true);
    }

    public static T EnumParse<T>(string str, T defaultValue) {
      if (string.IsNullOrEmpty(str)) return defaultValue;
      try {
        return (T)Enum.Parse(typeof(T), str, true);
      } catch { return defaultValue; }
    }
    public static bool IsNumeric(Type type) {
      if (!type.IsEnum) {
        switch (Type.GetTypeCode(type)) {
          case TypeCode.Char:
          case TypeCode.SByte:
          case TypeCode.Byte:
          case TypeCode.Int16:
          case TypeCode.UInt16:
          case TypeCode.Int32:
          case TypeCode.UInt32:
          case TypeCode.Int64:
          case TypeCode.UInt64:
          case TypeCode.Single:
          case TypeCode.Double:
            return true;
        }
      }
      return false;
    }

    public static XDocument XHTML(string title, IEnumerable<object> head, IEnumerable<object> body) {
      XDocument doc = new XDocument(
        new XDeclaration("1.0", "utf-8", null),
        new XDocumentType("html", "-//W3C//DTD XHTML 1.0 Strict//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd", null),
        new XElement(CommonLib.html + "html",
          new XElement(CommonLib.html + "head",
            new XElement(CommonLib.html + "title", title),
            head
          ),
          new XElement(CommonLib.html + "body",
            body
          )
        )
      );
      return doc;
    }

    public static string crlfSpaces(string s) { return spacesRx.Replace(s.Replace('\r', ' ').Replace('\n', ' '), " ").Trim(); }
    static Regex spacesRx = new Regex(@"[\s]{2,}", RegexOptions.Singleline);

    public static IEnumerable<T> EnumGetValues<T>() {
      return Enum.GetValues(typeof(T)).Cast<T>();
    }

    public static string removeDiakritic(string str) {
      if (str == null) return str;
      char[] res = new char[str.Length];
      for (int i = 0; i < str.Length; i++) res[i] = removeDiakritic(str[i]);
      return new string(res);
    }

    public static char removeDiakritic(char ch) {
      switch (ch) {
        case 'á': return 'a';
        case 'Á': return 'A';
        case 'è': return 'c';
        case 'È': return 'C';
        case 'ï': return 'd';
        case 'Ï': return 'D';
        case 'é': return 'e';
        case 'É': return 'E';
        case 'ì': return 'e';
        case 'Ì': return 'E';
        case 'í': return 'i';
        case 'Í': return 'I';
        case 'ò': return 'n';
        case 'Ò': return 'N';
        case 'ó': return 'o';
        case 'Ó': return 'O';
        case 'ø': return 'r';
        case 'Ø': return 'R';
        case 'š': return 's';
        case 'Š': return 'S';
        case 'ť': return 't';
        case 'Ť': return 'T';
        case 'ú': return 'u';
        case 'Ú': return 'U';
        case 'ù': return 'u';
        case 'Ù': return 'U';
        case 'ý': return 'y';
        case 'Ý': return 'Y';
        case 'ž': return 'z';
        case 'Ž': return 'Z';
        default: return ch;
      }
    }

    public static string ExceptionToString(Exception exp, bool incTitle, bool isHtml) {
      string lf = isHtml ? "\r\n</br>" : "\r\n";
      string res = null;
      while (true) {
        if (exp == null) return res;
        if (incTitle) res += exp.Message; res += lf;
        incTitle = true;
        res += isHtml ? exp.StackTrace.Replace("\r", lf) : exp.StackTrace; res += lf;
        exp = exp.InnerException;
      }
    }

    public static string ExceptionMsgToString(Exception exp) {
      string res = null;
      while (true) {
        if (exp == null) return res;
        res += (res != null ? " # " : null) + exp.Message;
        exp = exp.InnerException;
      }
    }

    public static string ExceptionToString(Exception exp, bool incTitle) {
      return ExceptionToString(exp, incTitle, false);
    }

    public static string ExceptionToString(Exception exp) {
      return ExceptionToString(exp, true);
    }

    /// <summary>
    /// Encodes a string to be represented as a string literal. The format
    /// is essentially a JSON string.
    /// 
    /// The string returned includes outer quotes 
    /// Example Output: "Hello \"Rick\"!\r\nRock on"
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string EncodeJsString(string s, bool withQuote = true) {
      StringBuilder sb = new StringBuilder();
      if (withQuote) sb.Append("\'");
      foreach (char c in s) {
        switch (c) {
          case '\'':
            sb.Append("\\\'");
            break;
          case '\"':
            sb.Append("\\\"");
            break;
          case '\\':
            sb.Append("\\\\");
            break;
          case '\b':
            sb.Append("\\b");
            break;
          case '\f':
            sb.Append("\\f");
            break;
          case '\n':
            sb.Append("\\n");
            break;
          case '\r':
            sb.Append("\\r");
            break;
          case '\t':
            sb.Append("\\t");
            break;
          default:
            int i = (int)c;
            if (i < 32 || i > 127) {
              sb.AppendFormat("\\u{0:X04}", i);
            } else {
              sb.Append(c);
            }
            break;
        }
      }
      if (withQuote) sb.Append("\'");

      return sb.ToString();
    }

    public static IEnumerable<T> Distinct_PreserveOrder<T, TKey>(this IEnumerable<T> source, Func<T, TKey> compare) {
      HashSet<TKey> set = new HashSet<TKey>();
      foreach (T item in source)
        if (set.Add(compare(item))) yield return item;
    }

    //pro Contact manager:
    public static string encodeUnsubscribe(MemoryStream str, string sender, string email, Langs lng, string blockListName) {
      str.SetLength(0);
      BinaryWriter wr = new BinaryWriter(str);
      wr.WriteStringEx(sender);
      wr.WriteStringEx(email);
      wr.Write((byte)lng);
      wr.WriteStringEx(blockListName);
      return LowUtils.bytesToString(str.ToArray());
    }

    public static void decodeUnsubscribe(MemoryStream str, string val, out string sender, out string email, out Langs lng, out string blockListName) {
      byte[] data = LowUtils.stringToBytes(val);
      str.SetLength(0); str.Write(data, 0, data.Length); str.Seek(0, SeekOrigin.Begin);
      BinaryReader rdr = new BinaryReader(str);
      sender = rdr.ReadStringEx();
      email = rdr.ReadStringEx();
      lng = (Langs)rdr.ReadByte();
      blockListName = rdr.ReadStringEx();
    }


    public static void RegisterCSSInclude(Page page, string name, string url) {
      page.LoadComplete += (s, a) => {
        name = name.Replace('.', '_').Replace('-', '_');
        if (page.Header.FindControl(name) != null) return;
        HtmlLink objLink = new HtmlLink() { ID = name, Href = url };
        objLink.Attributes["rel"] = "stylesheet";
        objLink.Attributes["type"] = "text/css";
        page.Header.Controls.Add(objLink);
      };
    }








    /****************************** Module Header ******************************\
    * Module Name:	Program.cs
    * Project:		CSCheckOSBitness
    * Copyright (c) Microsoft Corporation.
    * 
    * The code sample demonstrates how to determine whether the operating system 
    * of the current machine or any remote machine is a 64-bit operating system.
    * 
    * This source is subject to the Microsoft Public License.
    * See http://www.microsoft.com/opensource/licenses.mspx#Ms-PL.
    * All other rights reserved.
    * 
    * THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
    * EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED 
    * WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
    \***************************************************************************/

    #region Is64BitOperatingSystem (IsWow64Process)

    public static bool Is64BitCompile() {
      return IntPtr.Size == 8;
    }

    /// <summary>
    /// The function determines whether the current operating system is a 
    /// 64-bit operating system.
    /// </summary>
    /// <returns>
    /// The function returns true if the operating system is 64-bit; 
    /// otherwise, it returns false.
    /// </returns>
    public static bool Is64BitOperatingSystem() {
      if (IntPtr.Size == 8)  // 64-bit programs run only on Win64
            {
        return true;
      } else  // 32-bit programs run on both 32-bit and 64-bit Windows
            {
        // Detect whether the current process is a 32-bit process 
        // running on a 64-bit system.
        bool flag;
        return ((DoesWin32MethodExist("kernel32.dll", "IsWow64Process") &&
            IsWow64Process(GetCurrentProcess(), out flag)) && flag);
      }
    }

    /// <summary>
    /// The function determins whether a method exists in the export 
    /// table of a certain module.
    /// </summary>
    /// <param name="moduleName">The name of the module</param>
    /// <param name="methodName">The name of the method</param>
    /// <returns>
    /// The function returns true if the method specified by methodName 
    /// exists in the export table of the module specified by moduleName.
    /// </returns>
    static bool DoesWin32MethodExist(string moduleName, string methodName) {
      IntPtr moduleHandle = GetModuleHandle(moduleName);
      if (moduleHandle == IntPtr.Zero) {
        return false;
      }
      return (GetProcAddress(moduleHandle, methodName) != IntPtr.Zero);
    }

    [DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr GetModuleHandle(string moduleName);

    [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
    static extern IntPtr GetProcAddress(IntPtr hModule,
        [MarshalAs(UnmanagedType.LPStr)]string procName);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

    #endregion


    public static long nowToInt() { return dateToInt(DateTime.UtcNow); }
    public static int nowToNum() { return dateToNum(DateTime.UtcNow); }
    public static string formatDateLow(DateTime dt, CultureInfo ci) { return dt.ToString("d", ci); }
    public static string formatTimeLow(DateTime dt, CultureInfo ci) { return dt.ToString("HH:mm:ss", ci); }

    //vteriny
    public static int dateToNum(DateTime dt) { return (int)(dateToInt(dt) / 1000); }
    public static DateTime numToDate(long num) { return intToDate(num * 1000); }
    public static string formatDate(long sec, CultureInfo ci) { return formatDateLow(numToDate(sec), ci); }
    public static string formatDateTime(long sec, CultureInfo ci) { return formatDate(sec, ci) + formatTimeLow(numToDate(sec), ci); }

    //miliseconds
    public static long dateToInt(DateTime dt) { return DateToInt(dt); }
    public static DateTime intToDate(long num) { return IntToDate(num); }
    public static string intToDateStr(long num, CultureInfo ci) { return formatDateLow(intToDate(num), ci); }
    public static string intToDateStrLong(long num, CultureInfo ci) { return intToDate(num).ToString("D", ci); }

    //days
    public static long dayToInt(DateTime dt) { return Convert.ToInt64((dateToInt(dt) + 1) / msecInDay); }
    public static DateTime intToDay(int num) { return new DateTime(num * msecInDay); }
    public static string formatDay(int day, CultureInfo ci) { return formatDateLow(intToDay(day), ci); }

    public static string formatTimeSpan(long secs) {
      var s = Convert.ToInt32(secs % 60); secs = secs / 60;
      var m = Convert.ToInt32(secs % 60);
      var h = Convert.ToInt32(secs / 60);
      return h.ToString() + ":" + (m < 10 ? "0" : "") + m.ToString() + ":" + (s < 10 ? "0" : "") + s.ToString();
    }

    public static bool IsTheSameDay(DateTime date1, DateTime date2) {
      return (date1.Year == date2.Year && date1.DayOfYear == date2.DayOfYear);
    }
  }



  public class LocalServiceConfig {
    public string Class = "";
    public string Assembly = "";
    public bool TCPChannel = false;
  }

  public class RemoteServiceConfig {
    public string Name = "";
    public bool TCPChannel = false;
    public string Url = "";
  }

  public class TCPManagerConfig {
    public const string lmTCPName = "TCPManager_";
    public int Port = 0;
  }

  /*
  public class TCPManager
  {

    public static TCPManagerConfig Config;

    public static void Init(bool local)
    {
      Config = local ? (TCPManagerConfig)ConfigurationManager.GetSection(TCPManagerConfig.lmTCPName) : null;
      if (Config != null && Config.Port > 0)
        ChannelServices.RegisterChannel(new TcpChannel(Config.Port), false);
    }

    public static void RegisterService(LocalServiceConfig cfg, Type tp)
    {
      if (cfg.TCPChannel)
        RemotingConfiguration.RegisterWellKnownServiceType(tp, tp.Name, WellKnownObjectMode.Singleton);
    }
  }
   * */

  public class MyUtf8 : UTF8Encoding {
    static public MyUtf8 Instance = new MyUtf8();

    static byte[] _preamble = new byte[0];

    public override byte[] GetPreamble() {
      return _preamble;
    }
  }

  public struct regExItemPos {
    public regExItemPos(bool isMatch, int idx, int len, Match match) { IsMatch = isMatch; index = idx; length = len; this.match = match; }
    public bool IsMatch; public int index; public int length; public Match match;
    public static IEnumerable<regExItemPos> Parse(string s, Regex ex) {
      int pos = 0;
      foreach (Match match in ex.Matches(s)) {
        if (pos < match.Index)
          yield return new regExItemPos(false, pos, match.Index - pos, null);
        yield return new regExItemPos(true, match.Index, match.Length, match);
        pos = match.Index + match.Length;
      }
      if (pos < s.Length)
        yield return new regExItemPos(false, pos, s.Length - pos, null);
    }
  }
  public struct regExItem {
    public regExItem(bool isMatch, string val, Match match) { IsMatch = isMatch; Value = val; this.match = match; }
    public bool IsMatch; public string Value; public Match match;
    public static IEnumerable<regExItem> Parse(string s, Regex ex) {
      int pos = 0;
      foreach (Match match in ex.Matches(s)) {
        if (pos < match.Index)
          yield return new regExItem(false, s.Substring(pos, match.Index - pos), null);
        yield return new regExItem(true, s.Substring(match.Index, match.Length), match);
        pos = match.Index + match.Length;
      }
      if (pos < s.Length)
        yield return new regExItem(false, s.Substring(pos, s.Length - pos), null);
    }
  }

  //http://corner-house.blogspot.cz/2006/03/accessing-shared-folders-in-aspnet.html
  public static class ShareThis {

    public struct NETRESOURCE {
      public int dwScope;
      public int dwType;
      public int dwDisplayType;
      public int dwUsage;

      [MarshalAs(UnmanagedType.LPStr)]
      public string lpLocalName;

      [MarshalAs(UnmanagedType.LPStr)]
      public string lpRemoteName;

      [MarshalAs(UnmanagedType.LPStr)]
      public string lpComment;
      [MarshalAs(UnmanagedType.LPStr)]
      public string lpProvider;
    }

    //WIN32API - WNetAddConnection2
    [DllImport("mpr.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern int WNetAddConnection2A(
      [MarshalAs(UnmanagedType.LPArray)]NETRESOURCE[] lpNetResource,
        [MarshalAs(UnmanagedType.LPStr)]string lpPassword,
        [MarshalAs(UnmanagedType.LPStr)]string lpUserName,
        int dwFlags);

    //WIN32API - WNetCancelConnection2
    [DllImport("mpr.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern int WNetCancelConnection2A([MarshalAs(UnmanagedType.LPStr)]string lpName, int dwFlags, int fForce);

    public static void SetConnection(string share, string username, string password, Action action) {
      if (share == null) action();
      else {
        WNetAddConnection2A(
          new NETRESOURCE[] { new NETRESOURCE() { lpRemoteName = share, lpLocalName = "", dwScope = 0, dwUsage = 0, lpComment = "", lpProvider = "" } },
          password, username, 0);
        try {
          action();
        } finally {
          WNetCancelConnection2A(share, 0, -1);
        }
      }
    }
  }

  public interface ITree {
    int Id { get; }
    string Title { get; }
    bool HasChild { get; }
    IEnumerable<ITree> Childs { get; }
  }

  public static class TypeHelper {
    public static object Create(Type typ) {
      return typ.GetConstructor(Type.EmptyTypes).Invoke(null);
    }
    public static Type PropType(Type typ, string propName) {
      PropertyInfo prop = typ.GetProperty(propName);
      return prop != null ? prop.PropertyType : typ.GetField(propName).FieldType;
    }
    public static void SetValue(object obj, string propName, object val) {
      PropertyInfo prop = obj.GetType().GetProperty(propName);
      if (prop != null) prop.SetValue(obj, val, null);
      else obj.GetType().GetField(propName).SetValue(obj, val);
    }
    public static object GetValue(object obj, string propName) {
      PropertyInfo prop = obj.GetType().GetProperty(propName);
      return prop != null ? prop.GetValue(obj, null) : obj.GetType().GetField(propName).GetValue(obj);
    }
  }

  public class JsonHelper {
    /// <summary>
    /// JSON Serialization
    /// </summary>
    public static string Serialize(object t) {
      DataContractJsonSerializer ser = new DataContractJsonSerializer(t.GetType());
      using (MemoryStream ms = new MemoryStream()) {
        ser.WriteObject(ms, t);
        return Encoding.UTF8.GetString(ms.ToArray());
      }
    }
    /// <summary>
    /// JSON Deserialization
    /// </summary>
    public static T Deserialize<T>(string jsonString) {
      DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
      using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
        return (T)ser.ReadObject(ms);
    }
  }

  public static class StaticReflection {

    public static MemberInfo GetMember<T>(Expression<Func<T, object>> expression) {
      MemberExpression memberExpression = expression.Body as MemberExpression;
      if (memberExpression != null) return memberExpression.Member;
      UnaryExpression unaryExpression = expression.Body as UnaryExpression;
      if (unaryExpression != null) {
        if (unaryExpression.Operand is MethodCallExpression) {
          var methodExpression = (MethodCallExpression)unaryExpression.Operand;
          return methodExpression.Method;
        }
        return ((MemberExpression)unaryExpression.Operand).Member;
      }
      throw new Exception();
    }

    public static string GetMemberName<T>(Expression<Func<T, object>> expression) { return GetMember<T>(expression).Name; }
    public static IEnumerable<string> GetMemberNames<T>(params Expression<Func<T, object>>[] expressions) { foreach (var e in expressions) yield return GetMemberName<T>(e); }
    public static IEnumerable<MemberInfo> GetMembers<T>(params Expression<Func<T, object>>[] expressions) { foreach (var e in expressions) yield return GetMember<T>(e); }

    //public static string GetMemberName<T>(
    //    this T instance,
    //    Expression<Func<T, object>> expression) {
    //  return GetMemberName(expression);
    //}

    //public static string GetMemberName<T>(
    //    Expression<Func<T, object>> expression) {
    //  if (expression == null) {
    //    throw new ArgumentException(
    //        "The expression cannot be null.");
    //  }

    //  return GetMemberName(expression.Body);
    //}

    //public static string GetMemberName<T>(
    //    this T instance,
    //    Expression<Action<T>> expression) {
    //  return GetMemberName(expression);
    //}

    //public static string GetMemberName<T>(
    //    Expression<Action<T>> expression) {
    //  if (expression == null) {
    //    throw new ArgumentException(
    //        "The expression cannot be null.");
    //  }

    //  return GetMemberName(expression.Body);
    //}

    //private static string GetMemberName(
    //    Expression expression) {
    //  if (expression == null) {
    //    throw new ArgumentException(
    //        "The expression cannot be null.");
    //  }

    //  if (expression is MemberExpression) {
    //    // Reference type property or field
    //    var memberExpression =
    //        (MemberExpression)expression;
    //    return memberExpression.Member.Name;
    //  }

    //  if (expression is MethodCallExpression) {
    //    // Reference type method
    //    var methodCallExpression =
    //        (MethodCallExpression)expression;
    //    return methodCallExpression.Method.Name;
    //  }

    //  if (expression is UnaryExpression) {
    //    // Property, field of method returning value type
    //    var unaryExpression = (UnaryExpression)expression;
    //    return GetMemberName(unaryExpression);
    //  }

    //  throw new ArgumentException("Invalid expression");
    //}

    //private static string GetMemberName(
    //    UnaryExpression unaryExpression) {
    //  if (unaryExpression.Operand is MethodCallExpression) {
    //    var methodExpression =
    //        (MethodCallExpression)unaryExpression.Operand;
    //    return methodExpression.Method.Name;
    //  }

    //  return ((MemberExpression)unaryExpression.Operand)
    //      .Member.Name;
    //}
  }

  public class FtpClient {
    public FtpClient(string ftpUrl, string userName, string password, string folderName) {
      this.ftpUrl = ftpUrl; this.userName = userName; this.password = password; this.folderName = folderName;
    }
    string ftpUrl; string userName; string password; string folderName;
    public static void FtpDelete(string ftpUrl, string userName, string password, string folderName, string fileName) {

    }
    public void FtpUpload(string fileName, byte[] file, Action<int> progress, Action<Exception> completed) {
      using (var worker = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true }) {
        worker.DoWork += (s, a) => {
          var url = string.Format(@"ftp://{0}{1}{2}", ftpUrl, folderName == null ? null : folderName + "/", fileName);
          FtpWebRequest request = WebRequest.Create(new Uri(url)) as FtpWebRequest;
          request.Method = WebRequestMethods.Ftp.UploadFile;
          request.UseBinary = true;
          request.UsePassive = true;
          request.KeepAlive = true;
          request.Credentials = new NetworkCredential(userName, password);
          request.ContentLength = file.Length;
          using (Stream requestStream = request.GetRequestStream()) {
            foreach (var intv in LowUtils.intervals(file.Length, 1000)) {
              if (worker.CancellationPending) { a.Cancel = true; break; }
              requestStream.Write(file, intv.start, intv.take);
              worker.ReportProgress((int)(100 * intv.start / file.Length));
            }
          }
        };
        if (progress != null) worker.ProgressChanged += (s, a) => progress(a.ProgressPercentage);
        if (completed != null) worker.RunWorkerCompleted += (s, a) => completed(a.Cancelled ? new ECancelException() : a.Error);
        worker.RunWorkerAsync();
      }
    }
    public class ECancelException : Exception { }

    //http://stackoverflow.com/questions/966578/parse-response-from-ftp-list-command-syntax-variations
    static Regex parseDirListing = new Regex(@"^(?<dir>[\-ld])(?<permission>([\-r][\-w][\-xs]){3})\s+(?<filecode>\d+)\s+(?<owner>\S+)\s+(?<group>\S+)\s+(?<size>\d+)\s+(?<timestamp>((?<month>\w{3})\s+(?<day>\d{1,2})\s+(?<hour>\d{1,2}):(?<minute>\d{2}))|((?<month>\w{3})\s+(?<day>\d{1,2})\s+(?<year>\d{4})))\s+(?<name>.+)$");

  }
  //knihovny:
  //http://www.limilabs.com/

  public static class LMJson {
    //normal
    public static string Encode(object obj, bool formated = false) { return JsonConvert.SerializeObject(obj, formated ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None); }
    public static T Decode<T>(string str) { return JsonConvert.DeserializeObject<T>(str); }

    //encrypt and base64
    public static string EncodePackStr(object obj) { return LowUtils.packStr(Encode(obj)); }

    //encrypt
    public static byte[] EncodeEncrypt(object obj) { return LowUtils.encrypt(Encoding.UTF8.GetBytes(Encode(obj))); }

  }

}
