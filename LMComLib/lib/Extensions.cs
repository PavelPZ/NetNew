using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;

namespace LMComLib {
  public static class XExtension {

    public class KeyEqualityComparer<T> : IEqualityComparer<T> {
      private readonly Func<T, object> keyExtractor;
      public KeyEqualityComparer(Func<T, object> keyExtractor) { this.keyExtractor = keyExtractor; }
      public bool Equals(T x, T y) { return this.keyExtractor(x).Equals(this.keyExtractor(y)); }
      public int GetHashCode(T obj) { return this.keyExtractor(obj).GetHashCode(); }
    }

    public static IEnumerable<T> SideEfect<T>(this IEnumerable<T> source, Action<T> sideFnc) {
      foreach (var t in source) { sideFnc(t); yield return t; }
    }

    public static string Join<T>(this IEnumerable<T> source, string delim = ",") {
      return source.Select(s => s.ToString()).Aggregate((r, i) => r + delim + i);
    }

    public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector) {
      HashSet<TKey> keys = new HashSet<TKey>();
      foreach (T item in source) {
        TKey key = keySelector(item);
        if (!keys.Contains(key)) {
          keys.Add(key);
          yield return item;
        }
      }
    }

    public static TRes get<TKey,TRes>(this Dictionary<TKey,TRes> source, TKey key, TRes defVal) {
      TRes res;
      return source!=null && source.TryGetValue(key, out res) ? res : defVal;
    }

    public static string get(this Match source, string id) {
      var grp = source.Groups[id];
      return grp.Success ? grp.Value : null;
    }

    public static string AgregateSB(this IEnumerable<string> source, Action<StringBuilder, string> add) {
      bool isStart = true; StringBuilder sb = new StringBuilder();
      foreach (var s in source) {
        if (isStart) sb.Append(s); else add(sb, s);
        isStart = false;
      }
      return isStart ? "" : sb.ToString();
    }

    /// <summary>
    /// Break a list of items into chunks of a specific size
    /// </summary>
    public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunksize) {
      while (source.Any()) {
        yield return source.Take(chunksize);
        source = source.Skip(chunksize);
      }
    }

    public static IEnumerable<Exception> Chain(this Exception exp) {
      while (exp != null) { yield return exp; exp = exp.InnerException; }
    }

    public static IEnumerable<T> Each<T>(this IEnumerable<T> items, Action<T> action) {
      foreach (var it in items) { action(it); yield return it; }
    }

    //oproti Split ma count a vraci i oddelovac
    public static IEnumerable<List<T>> SplitEx<T>(this IEnumerable<T> source, Func<T, T, bool> isSeparator, int count = int.MaxValue) where T : class {
      List<T> list = new List<T>();
      T prev = null;
      foreach (T item in source) {
        if (count > 1 && isSeparator(prev, item)) {
          if (list.Count > 0) {
            count--;
            yield return list;
            list = new List<T>() { item };
          }
        } else
          list.Add(item);
        prev = item;
      }
      if (list.Count > 0) yield return list;
    }

    public static IEnumerable<List<T>> Split<T>(this IEnumerable<T> source, Func<T, bool> isSeparator) {
      List<T> list = new List<T>();
      foreach (T item in source) {
        if (isSeparator(item)) {
          if (list.Count > 0) {
            yield return list;
            list = new List<T>();
          }
        } else
          list.Add(item);
      }
      if (list.Count > 0) yield return list;
    }

    public static IEnumerable<TSource> TakeUntil<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) {
      return TakeUntil(source, predicate, true);
    }

    public static IEnumerable<TSource> SkipUntil<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) {
      return SkipUntil(source, predicate, true);
    }

    public static IEnumerable<TSource> TakeUntil<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, bool include) {
      foreach (TSource src in source) if (predicate(src)) { if (include) yield return src; yield break; } else yield return src;
    }

    public static IEnumerable<TSource> SkipUntil<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, bool include) {
      bool isOK = false;
      foreach (TSource src in source) if (predicate(src)) { if (include) yield return src; isOK = true; } else { if (isOK) yield return src; }
    }

    public static string InnerXml(this IEnumerable<XNode> els) {
      string res = null;
      foreach (XNode el in els)
        res += el.ToString(SaveOptions.DisableFormatting);
      return res;
    }
    public static string InnerXml(this XElement root, XNamespace toRemove = null) {
      return toRemove == null ? InnerXml(root.Nodes()) : InnerXml(root.Nodes(), toRemove);
    }
    public static string InnerXml(this IEnumerable<XNode> els, XNamespace toRemove) {
      string res = InnerXml(els); if (res == null) return res;
      return res.Replace(" xmlns=\"" + toRemove.NamespaceName + "\"", null);
    }
    public static void InnerXml(this IEnumerable<XNode> els, StringBuilder sb) {
      foreach (XNode el in els)
        sb.Append(el.ToString(SaveOptions.DisableFormatting));
    }
    public static string OuterXml(this XElement el, XNamespace toRemove) {
      return el.ToString().Replace("xmlns=\"" + toRemove.NamespaceName + "\"", null);
    }
    public static IEnumerable<XElement> ElementsAttr(this XElement el, string name, string value) {
      return el.Elements().Select(e => e.Attribute(name)).Where(a => a != null && a.Value == value).Select(a => a.Parent);
    }
    public static IEnumerable<XElement> DescendantsAttr(this XElement el, XName name, string value) {
      return el.Descendants().Select(e => e.Attribute(name)).Where(a => a != null && a.Value == value).Select(a => a.Parent);
    }
    public static IEnumerable<XAttribute> DescendantsAttr(this XElement el, XName name) {
      return el.Descendants().Select(e => e.Attribute(name)).Where(a => a != null);
    }
    public static XElement ElementAttr(this XElement el, string name, string value) {
      return ElementsAttr(el, name, value).FirstOrDefault();
    }
    public static IEnumerable<XNode> AncestorsAndSelf(this XNode nd) {
      yield return nd;
      foreach (XElement el in nd.Ancestors()) yield return el;
    }
#if !SILVERLIGHT
    public static IEnumerable<FileInfo> GetFiles(this DirectoryInfo dir, string[] exts, SearchOption opt) {
      foreach (string ext in exts)
        foreach (FileInfo fi in dir.GetFiles(ext, opt))
          yield return fi;

    }
#endif
    public static string outerText(this XNode nd) {
      if (nd is XText) return ((XText)nd).Value;
      else if (nd is XElement) return ((XElement)nd).Value;
      throw new Exception("Missing code here!");
    }
    public static string outerText(this IEnumerable<XNode> els) {
      string res = null;
      foreach (XNode nd in els) res += nd.outerText();
      return res;
    }
    public static XNode NodeBeforeSelf(this XNode nd) {
      return nd.NodesBeforeSelf().LastOrDefault();
    }
    public static XNode NodeAfterSelf(this XNode nd) {
      return nd.NodesAfterSelf().FirstOrDefault();
    }

    public static IEnumerable<XElement> Parents(this XElement el, bool incSelf) {
      if (incSelf) yield return el;
      while (el.Parent != null) { el = el.Parent; yield return el; }
    }

    public static XElement ChangeNamespace(XElement root, XNamespace oldNs, XNamespace newNs) {
      // change the namespace of every element and attribute in the first tree
      foreach (XElement el in root.DescendantsAndSelf().Where(e => e.Name.Namespace == oldNs)) {
        el.Name = newNs.GetName(el.Name.LocalName);
        List<XAttribute> atList = el.Attributes().ToList();
        el.Attributes().Remove();
        foreach (XAttribute at in atList.Where(a => a.Name.LocalName != "xmlns")) el.Add(new XAttribute(at.Name.LocalName, at.Value));
      }
      return root;
    }

    public static IEnumerable<T> Create<T>(params T[] pars) {
      foreach (T p in pars) yield return p;
    }

    public static XElement normalizeXmlText(XElement root, StringBuilder sb) {
      foreach (XText txt in root.Nodes().OfType<XText>()) txt.Value = normalizeXmlText(txt.Value, sb);
      return root;
    }

    public static string normalizeXmlText(string txt, StringBuilder sb) {
      if (string.IsNullOrEmpty(txt)) return txt;
      sb.Length = 0;
      txt = txt.Trim();
      int st = 0;
      for (int i = 0; i < txt.Length; i++) {
        if (st == 0) {
          if (char.IsWhiteSpace(txt[i])) { sb.Append(' '); st = 1; } else sb.Append(txt[i]);
        } else {
          if (!char.IsWhiteSpace(txt[i])) { sb.Append(txt[i]); st = 0; }
        }
      }
      return sb.ToString();
    }

    public static T MinByOrDefault<T, TCompare>(this IEnumerable<T> collection, Func<T, TCompare> func) where TCompare : IComparable<TCompare> {
      T minItem = default(T); TCompare minValue = default(TCompare);
      foreach (var item in collection) {
        TCompare temp = func(item);
        if (minItem == null || temp.CompareTo(minValue) < 0) { minValue = temp; minItem = item; }
      }
      return minItem;
    }

    public static IEnumerable<T> Return<T>(params T[] pars) {
      return pars;
    }

    public static T MaxByOrDefault<T, TCompare>(this IEnumerable<T> collection, Func<T, TCompare> func) where TCompare : IComparable<TCompare> {
      return collection.MaxComparer(func);
    }

    public static T MaxComparer<T, TCompare>(this IEnumerable<T> collection, Func<T, TCompare> func) where TCompare : IComparable<TCompare> {
      T maxItem = default(T); TCompare maxValue = default(TCompare);
      foreach (var item in collection) {
        TCompare temp = func(item);
        if (maxItem == null || temp.CompareTo(maxValue) > 0) { maxValue = temp; maxItem = item; }
      }
      return maxItem;
    }

    public static int IndexOf<T>(this IEnumerable<T> collection, Func<T, bool> func) {
      int cnt = 0;
      foreach (var item in collection) if (func(item)) return cnt; else cnt++;
      return -1;
    }

    public static IEnumerable<int> IndexesOf<T>(this IEnumerable<T> collection, Func<T, bool> func) {
      int cnt = 0;
      foreach (var item in collection) { if (func(item)) yield return cnt; cnt++; }
    }

    public static void Write(this BinaryWriter wr, Guid id) { wr.Write(id.ToByteArray()); }
    public static Guid ReadGuid(this BinaryReader rdr) { byte[] data = rdr.ReadBytes(16); return new Guid(data); }
    public static void Write(this BinaryWriter wr, DateTime dt) { wr.Write(dt.ToFileTimeUtc()); }
    public static DateTime ReadDateTime(this BinaryReader rdr) { return DateTime.FromFileTimeUtc(rdr.ReadInt64()); }
    public static void WriteStringEx(this BinaryWriter wr, string str) {
      byte[] data = str == null ? null : Encoding.UTF8.GetBytes(str);
      wr.WriteByteArray(data);
    }
    public static string ReadStringEx(this BinaryReader rdr) {
      byte[] res = rdr.ReadByteArray();
      return res == null ? null : Encoding.UTF8.GetString(res, 0, res.Length);
    }
    public static void WriteByteArray(this BinaryWriter wr, byte[] data) {
      if (data == null) { wr.Write(0); return; }
      wr.Write(data.Length); wr.Write(data);
    }
    public static byte[] ReadByteArray(this BinaryReader rdr) {
      int len = rdr.ReadInt32(); if (len == 0) return null;
      return rdr.ReadBytes(len);
    }
    public static void Write(this BinaryWriter wr, Dictionary<string, string> dict) {
      if (dict == null) { wr.Write(-1); return; }
      if (dict.Count == 0) { wr.Write(0); return; }
      wr.Write(dict.Count);
      foreach (var kv in dict) { wr.Write(kv.Key); wr.Write(kv.Value); }
    }
    public static Dictionary<string, string> ReadDict(this BinaryReader rdr) {
      int len = rdr.ReadInt32();
      if (len == -1) return null;
      if (len == 0) return new Dictionary<string, string>();
      Dictionary<string, string> res = new Dictionary<string, string>();
      for (int i = 0; i < len; i++) res.Add(rdr.ReadString(), rdr.ReadString());
      return res;
    }

  }

  public class KeyEqualityComparer<T> : IEqualityComparer<T> {
    private readonly Func<T, object> keyExtractor;

    public KeyEqualityComparer(Func<T, object> keyExtractor) {
      this.keyExtractor = keyExtractor;
    }

    public bool Equals(T x, T y) {
      return this.keyExtractor(x).Equals(this.keyExtractor(y));
    }

    public int GetHashCode(T obj) {
      return this.keyExtractor(obj).GetHashCode();
    }
  }
}
