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
using System.Reflection;

namespace CourseMeta {

  //pro prevod old to new: variany souboru
  public enum oldeaDataType {
    lmdata, //puvodni script
    lmdataNew, //by hand nebo transform
    xml, //EANew-DeployGenerator na lmdata
    xmlNew, //EANew-DeployGenerator na lmdataNew
    webOld, //data z lmcom webu
    no,
  }

  public enum dictTypes {
    unknown,
    no, //bez slovniku
    L, //standardni Lingea
  }

  [Flags]
  public enum runtimeType {
    no = 0,
    //objects
    courseNode = 0x1, //uzly, ktere v JS agreguji vysledky svych podrizenych uzlu. V JS hlavni metoda refresh (plus set x getUser)
    multiTask = 0x2,
    product = 0x4, //produkt
    test = 0x8, //cely eTestMe test, obsahuje taskTestSkill's
    grammarRoot = 0x10, //grammar - cast produktuexpand
    taskCourse = 0x20, //kus kurzu
    taskPretest = 0x40, //cely pretest
    taskPretestTask = 0x80, //jedna cast pretestu, modLike
    taskTestInCourse = 0x100, //modLike. Test kurzu, obdoba test, obsahuje taskTestSkill 
    taskTestSkill = 0x200, //skill testu, modLike
    ex = 0x400, //cviceni    
    dynamicModuleData = 0x800, //popis dynamickych module dat
    project = 0x1000,
    //questionnaire = 0x20000, //cviceni s dotaznikem pretestu
    //flags
    //task = 0x1000, //umi osetrit zelenou sipku
    mod = 0x2000, //priznak zabaleni dat (modul, root gramatiky apod.)
    dynamicTestModule = 0x4000, //priznak pro dynamickou generaci dat testu dle popisu v dynamicModuleData objektu
    skipAbleRoot = 0x8000, //drzi seznam skiped nodes
    grammar = 0x10000,
    instrs = 0x20000,
    //skipAbleCourseNode = 0x10000,
    noDict = 0x40000, //modul s dotaznikem pretestu
    //questionnaireMod = modLike | dictOnly,
    //allModules = modLike | taskTestInCourse | taskTestSkill | taskPretestTask, //nodes, co se chovaji pri design time buildu jako moduly. 
    //allNoDictModules = taskTestInCourse | taskTestSkill | dictOnly, //modules, co pri design time buildu vytvari pouze lokalizaci.
    publisher = 0x80000,
    sitemap = 0x100000,
    products = 0x200000,
    mediaCutFile = 0x400000,
    mediaDir = 0x800000,
    error = 0x1000000,
    testTaskGroup = 0x2000000,
    multiTest = 0x4000000, //test se sklada z vice testu, s dotaznikem
    multiQuestionnaire = 0x8000000, //dotaznik multitestu
    testDemo = 0x10000000, //demo test
    //runtimePage = 0x400000,
    productNew = 0x20000000, //novy produkt pomoci AngularJS. Příznak slouží k modifikaci skoku z hlavní obrazovky webu na produkt.
  }

  [XmlInclude(typeof(data)), XmlInclude(typeof(products)), XmlInclude(typeof(publisher)), XmlInclude(typeof(project)), XmlInclude(typeof(sitemap)), XmlInclude(typeof(ex)),
  XmlInclude(typeof(taskTestSkill)), XmlInclude(typeof(test)), XmlInclude(typeof(taskTestInCourse)), XmlInclude(typeof(mod)), XmlInclude(typeof(taskCourse)), XmlInclude(typeof(multiTask))]
  public class data {

    [XmlAttribute]
    public string title;
    [XmlAttribute, DefaultValue(0), JsonIgnore]
    public int order;
    [XmlAttribute, JsonGenOnly]
    public string url;
    [XmlAttribute, DefaultValue(0)]
    public LineIds line; //line dat
    [XmlAttribute, DefaultValue(0)]
    public runtimeType type;
    [XmlAttribute]
    public string name;
    [XmlAttribute]
    public string other; //muze byt JSON s ostatnimi informacemi

    [XmlAttribute, DefaultValue(0)]
    public int ms;


    //fake udaje pro prevod z OldEA formatu. V novem formatu je nahrazeno url.
    [XmlIgnore, JsonIgnore]
    public string spaceId { get { return _spaceId; } set { _spaceId = value; setUrl(); } }
    string _spaceId;
    [XmlIgnore, JsonIgnore]
    public string globalId { get { return _globalId; } set { _globalId = value; setUrl(); } }
    string _globalId;
    string setUrl() {
      url = (spaceId == null && globalId == null ? null : (string.IsNullOrEmpty(spaceId) || string.IsNullOrEmpty(globalId) ? spaceId + globalId : spaceId + "/" + globalId).Replace("/home.htm", null));
      if (this is ex || url.EndsWith("/")) return url;
      url += "/";
      return url;
    }
    static public string urlStripLast(string url) { return url[url.Length - 1] == '/' ? url.Substring(0, url.Length - 1) : url; }
    [XmlAttribute, JsonIgnore]
    public Langs[] allLocs; //vsechny dostupne lokalizace


    [XmlAttribute, JsonIgnore]
    public string styleSheet; //data, vytazena z .css

    public void modifyUrls(string prefix, string localVsNetPublisherId) {
      foreach (var d in scan())
        if (d.url.StartsWith(prefix)) d.url = localVsNetPublisherId + d.url.Substring(prefix.Length);
    }

    public void adjustMaxScore() {
      if (this is ex) return; //maxScore je nastaveno pri builid
      if (Items != null && Items.OfType<ex>().Any(e => e.isOldEa)) {
        ms = Items.OfType<ex>().Count();
        return;
      }
      ms = 0;
      if (Items != null) foreach (var it in Items) { it.adjustMaxScore(); ms += it.ms; }
    }

    //[XmlAttribute, JsonIgnore]
    //public string css;

    [XmlIgnore, JsonIgnore]
    public string style {
      get {
        if (_style == "?") {
          var styleFn = Machines.rootDir + url.Replace('/', '\\') + "meta.css";
          _style = File.Exists(styleFn) ? File.ReadAllText(styleFn, Encoding.UTF8) : null;
        }
        return _style;
      }
      set { _style = value; }
    }
    string _style = "?";

    public bool isType(runtimeType tp) { return (type & tp) == tp; }
    //[XmlAttribute, JsonIgnore]
    //public Langs[] locs; //mozna lokalizace dat 

    //[XmlAttribute, DefaultValue(0)]
    //public testMe.Skills skill;
    //[XmlAttribute, DefaultValue(false)]
    //public bool isDataHolder; //data cviceni nebo gramatiky jsou hromadne ulozena v takto oznacenem uzlu.

    [XmlIgnore, JsonIgnore, JsonGenOnly]
    public data parent;

    [XmlAttribute, DefaultValue(0)]
    public int uniqId; //pro Xrefs: jednoznacna identifikace uzlu sitemap. docXrefPage.sitemapParents

    [XmlElementAttribute(typeof(data))]
    [XmlElementAttribute(typeof(dynamicModuleData))]
    [XmlElementAttribute(typeof(testTaskGroup))]
    [XmlElementAttribute(typeof(taskTestSkill))]
    [XmlElementAttribute(typeof(multiTask))]
    [XmlElementAttribute(typeof(taskCourse))]
    [XmlElementAttribute(typeof(mod))]
    [XmlElementAttribute(typeof(test))]
    [XmlElementAttribute(typeof(taskTestInCourse))]
    [XmlElementAttribute(typeof(products))]
    [XmlElementAttribute(typeof(product))]
    [XmlElementAttribute(typeof(ptr))]
    [XmlElementAttribute(typeof(publisher))]
    [XmlElementAttribute(typeof(project))]
    [XmlElementAttribute(typeof(ex))]
    public data[] Items;

    //mechanismus na zachovani items v meta.xml pri generaci sitemap.
    [XmlIgnore, JsonIgnore]
    public data[] dataItems;

    public virtual CourseModel.tag readTag(LoggerMemory logger) {
      var root = CourseModel.tag.loadExerciseXml(Machines.rootDir + url.Replace('/', '\\') + ".xml", logger);
      CourseModel.tag res = CourseModel.tag.FromElement<CourseModel.tag>(root, logger, url);
      //kontrola a dokonceni
      foreach (var ctrl in res.scan()) ctrl.checkAndFinish(new CourseModel.body { url = url }, logger);
      return res;
    }


    public IEnumerable<data> parents(bool withSelf = true) { var p = this; while (p != null) { if (withSelf) yield return p; withSelf = true; p = p.parent; } }
    public LineIds getLine() { if (_line == null) { var par = parents().FirstOrDefault(p => p.line != LineIds.no); _line = par == null ? LineIds.no : par.line; } return (LineIds)_line; }
    LineIds? _line;
    public string getTradosPage() {
      if (url.StartsWith("/data/instr/")) return "/data/instr/";
      else return getLine() == LineIds.no || pathParts.Length < 2 ? null : "/" + pathParts[0] + "/" + pathParts[1] + "/" + getLine().ToString().ToLower() + "/";
    }//jmeno trados stranky
    public void finishRead() { if (Items != null) { int cnt = 0; foreach (var ch in Items) { ch.parent = this; ch.order = cnt++; ch.finishRead(); } } }
    public void finishCreate() {
      var dupls = scan().GroupBy(d => d.url).Where(g => g.Count() > 1).Select(g => g.Key).Where(k => k != null).ToArray(); if (dupls.Length > 0) throw new Exception("Dupls Keys");
      int uniq = 1;
      foreach (var dt in scan()) dt.uniqId = uniq++;
    }
    public IEnumerable<data> scan() { yield return this; if (Items != null) foreach (var it in Items) foreach (var subIt in it.scan()) yield return subIt; }

    public struct scanExResult {
      public data parent;
      public int itemIdx;
    }
    //nejdrive childs, pak parent
    public IEnumerable<scanExResult> scanEx(Action<data> parentCompleted = null) {
      if (Items == null || Items.Length == 0) yield break;
      for (int i = 0; i < Items.Length; i++) {
        foreach (var t in Items.OfType<data>()) foreach (var subT in t.scanEx(parentCompleted)) yield return subT;
        yield return new scanExResult() { itemIdx = i, parent = this };
      }
      if (parentCompleted != null) parentCompleted(this);
    }

    public void AssignTo(data dt) {
      dt.title = title;
      dt.name = name;
      dt.line = line;
      dt.url = url;
      dt.order = order;
    }

    protected T cloneLow<T>(Action<T> finish = null, bool deep = true)
      where T : data, new() {
      var res = new T {
        title = title,
        name = name,
        line = line,
        url = url,
        type = type,
        order = order,
        _style = _style,
        styleSheet = styleSheet,
        ms = ms,
        other = other,
      };
      if (finish != null) finish(res);
      if (deep) {
        if (Items != null) {
          res.Items = Items.Select(it => it.clone()).ToArray();
          foreach (var it in res.Items) it.parent = res;
        }
        if (dataItems != null) {
          res.dataItems = dataItems.Select(it => it.clone()).ToArray();
        }
      }
      return res;
    }
    public virtual data clone() { return cloneLow<data>(); }

    public string fileName() { return fileNameFromUrl(url); }

    public static string urlFromFileName(string fn) {
      var prefixLen = fn.StartsWith(Machines.dataDir) ? Machines.dataDir.Length - "/data".Length : Machines.rootDir.Length;
      return fn.Substring(prefixLen).ToLower().Replace('\\', '/').Replace("/meta.xml", "/").Replace(".xml", null);
    }
    public static string fileNameFromUrl(string url) {
      var dir = (url.StartsWith("/data/") ? Machines.dataDir + url.Substring("/data".Length) : Machines.rootDir + url).Replace('/', '\\'); return dir + (!dir.EndsWith("\\") ? ".xml" : @"meta.xml");
    }

    public XElement readXml() { return XElement.Load(fileName()); }
    public string readString() { return File.ReadAllText(fileName(), Encoding.UTF8); }
    [JsonIgnore]
    public string[] pathParts { get { return _pathParts ?? (_pathParts = url.Substring(1).Split('/')); } }
    string[] _pathParts;

    //public static IEnumerable<TradosLib.tradosPage> tradosOper1Pages(IEnumerable<data> nodes, LoggerMemory log, bool isFakeRussian) {
    //  return nodes.SelectMany(n => n.scan()).GroupBy(dt => dt.getTradosPage()).
    //    Where(g => g.Key != null).
    //    Select(g => new TradosLib.tradosPage {
    //      srcLang = isFakeRussian ? Langs.cs_cz : Langs.en_gb,
    //      FileName = g.Key,
    //      sentences = g.OfType<ex>().SelectMany(e => e.toTransSentences(log)).Concat(
    //        g.SelectMany(dt => locLib.getLocId2EnglishLoc(dt.title, dt.url, null).Select(nv => new NameValueString { Name = dt.url + "/" + nv.Name, Value = nv.Value }))
    //    ).
    //    Where(kv => isFakeRussian ? !kv.Name.EndsWith("ahtmltitle") : true).
    //    ToArray()
    //    });
    //}
    //public static void tradosOper1(IEnumerable<data> nodes, LoggerMemory log) {
    //  var pages = tradosOper1Pages(nodes, log, false).ToArray();
    //  TradosLib.oper1NewTradosPages(pages, false);
    //}
    //public void tradosOper1(LoggerMemory log) {
    //  TradosLib.oper1NewTradosPages(tradosOper1Pages(XExtension.Create(this), log, false).ToArray(), false);
    //}

    public static T readObject<T>(string fn) where T : data {
      try {
        fn = fn.ToLower();
        var doc = XElement.Load(fn);
        doc.Add(new XAttribute(xsi + "type", doc.Name.LocalName)); doc.Name = "data";
        XmlSerializer serializer = new XmlSerializer(typeof(data));
        using (XmlReader rdr = doc.CreateReader()) {
          var res = (T)serializer.Deserialize(rdr);
          if (res is publisher) res.type |= runtimeType.publisher;
          else if (res is project) res.type |= runtimeType.project;
          res.url = fn.Substring(Machines.rootDir.Length).Replace('\\', '/').Replace("/meta.xml", "/").Replace(".xml", null);
          return res;
        }
      }
      catch (Exception exp) {
        throw new Exception(fn, exp);
      }
    }
    static XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

    public static void writeObject(data obj, string fn) {
      File.WriteAllText(fn, writeObject(obj), Encoding.UTF8);
    }

    public static string writeObject(data obj) {
      return XmlUtils.ObjectToString(obj).
        Replace("xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"", null).
        Replace(" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"", null).
        Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n", null);
    }

  }

  public class sitemap : data {
    public sitemap() { type = runtimeType.sitemap; }
    public sitemap(string url, LoggerMemory logger, params data[] items)
      : this() {
      this.url = "/sitemap/" + url;
      Items = XExtension.Create(sitemap.fromFileSystem(url, logger)).Concat(items).ToArray();
      finish();
    }

    public void finish() {
      finishRead();
      //var grps = scan().Where(d => !(d is dynamicModuleData) && !(d is testTaskGroup)).GroupBy(d => d.url.ToLower()).ToArray();
      var grps = scan().Where(d => !string.IsNullOrEmpty(d.url)).GroupBy(d => d.url.ToLower()).ToArray();
      var wrongs = grps.Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
      if (wrongs.Length > 0) throw new Exception(wrongs.Take(Math.Min(10, wrongs.Length)).Aggregate((r, i) => r + "; " + i));
      dir = grps.ToDictionary(g => g.Key, g => g.First());
    }
    public data find(string url, LoggerMemory log = null) {
      data res;
      if (dir.TryGetValue(url.ToLower(), out res)) return res;
      if (log != null) { log.ErrorLine(url, "not in sitemap"); return null; }
      throw new Exception("Cannot find in sitemap: " + url);
    }
    public T find<T>(string url) where T : data { return (T)find(url); }
    public data findFileName(string fn) { return find(urlFromFileName(fn)); }
    public bool TryGetValue(string url, out data res) { return dir.TryGetValue(url, out res); }
    public IEnumerable<data> finds(params string[] urls) { return urls.Select(url => find(url)); }
    [XmlIgnore, JsonIgnore]
    Dictionary<string, data> dir;

    public override data clone() { return cloneLow<sitemap>(); }

    public static data readFromFilesystem(string fn) {
      try {
        var r = readObject<data>(fn); r.url = data.urlFromFileName(fn);
        var cssFn = Path.ChangeExtension(fn, ".css");
        if (File.Exists(cssFn)) r.styleSheet = File.ReadAllText(cssFn, Encoding.UTF8);
        return r;
      }
      catch (Exception e) { throw new Exception(fn, e); }
    }

    public static data readExForSitemap(string fn, LoggerMemory logger) {
      string url = CourseMeta.ex.urlFromFileName(fn);
      try {
        XElement root;
        try {
          root = XElement.Load(fn);
        }
        catch (Exception exp) {
          if (logger == null) throw exp;
          logger.ErrorLineFmt(url, "wrong XML format, {1}", fn, exp.Message);
          return null;
        }
        if (root.Name.LocalName == "html") {
          var pg = ex.readPage(root, url, logger); //kompletni readPage kvuli vypoctu maxScore
          return new ex {
            url = url,
            order = pg.order,
            title = pg.title,
            instrs = pg.instrs == null ? null : pg.instrs.Select(i => i.ToLower()).ToArray(),
            ms = pg.evalPage != null ? pg.evalPage.maxScore : 0,
          };
        }
        CourseModel.tag dt = CourseModel.tag.FromElement<CourseModel.tag>(root, logger, url);
        //var pg = res2 as CourseModel.body; var snd = res2 as CourseModel._sndFile;
        //if (pg != null) {
        var snd = dt as CourseModel._sndFile;
        if (snd != null)
          return new data { url = url, type = runtimeType.mediaCutFile, title = "Media cut file" };
        else
          return new data { url = url, type = runtimeType.error, title = "Error file" };
      }
      catch (Exception e) {
        logger.ErrorLine(url, LowUtils.ExceptionToString(e));
        return null;
      }
    }
    public const int errorSitemapOrder = 9999999;

    public static data fromFileSystem(string url, LoggerMemory logger) {
      var metaFn = ex.fileNameFromUrl(url); if (!File.Exists(metaFn)) return null;
      var dir = Path.GetDirectoryName(metaFn);
      data res = readFromFilesystem(metaFn);
      if (res.Items != null && res.Items.Length > 0) {
        res.dataItems = res.Items; res.Items = null;
      }
      var dirs = Directory.EnumerateDirectories(dir).Select(d => fromFileSystem(data.urlFromFileName(d + "/"), logger)).Where(d => d != null).ToArray();
      var files = Directory.EnumerateFiles(dir, "*.xml").Select(d => d.ToLower()).Where(f => !f.EndsWith("\\meta.xml")).Select(f => readExForSitemap(f, logger)).Where(f => f != null);
      res.Items = dirs.Concat(files).Where(d => d != null).OrderBy(d => d.order).ToArray();
      return res;
    }
  }

  public class publisher : data {
    public publisher() { type = runtimeType.publisher; }
    [XmlAttribute, JsonIgnore]
    public string vsNetData;
    [XmlAttribute, JsonIgnore]
    public string publisherRoot;
    public override data clone() { return cloneLow<publisher>(); }
  }

  public class project : data {
    public project() { type = runtimeType.project; }
    public override data clone() { return cloneLow<project>(p => { p.ftpPassword = ftpPassword; p.FtpUser = FtpUser; }); }
    [XmlIgnore]
    public string FtpPassword { get { return LowUtils.Decrypt(ftpPassword); } set { ftpPassword = LowUtils.Encrypt(value); } }
    [XmlAttribute]
    public string ftpPassword;
    [XmlAttribute]
    public string FtpUser;
  }

  public static class locLib {

    //static Dictionary<string, string> allLocs = new Dictionary<string, string>(); //pro kazdy kurz, jeho lokalizace.

    //public const string javascriptPrefix = "$";
    //public const string htmlAttrPrefix = "%";

    public static string removeLocalizedParts(string text) {
      return localizePartsRegex.Replace(text, "");
    }

    public static string initOldEALocalizeText(string text, Dictionary<string, string> loc, Action<string> notLoc) {
      return localizePartsRegex.Replace(text, m => { //modify text
        if (m.Value.IndexOf('|') > 0) return m.Value;
        var key = m.Value.Substring(2, m.Value.Length - 4);
        string val;
        if (!loc.TryGetValue(key, out val)) notLoc(key);
        return "{{" + key.ToLower() + "|" + val + "}}";
      });
    }

    public static IEnumerable<string> extractLocalizedParts(string text) {
      return localizePartsRegex.Matches(text).OfType<Match>().Select(m => m.Value.Substring(2, m.Value.Length - 4).Split('|')[0]);
    }

    //public static string initOldEALocalizeText(string text, Dictionary<string,string> loc, Action<string> notLoc ) {
    //public static string localizeText(string text, Dict<string, string> replace) {
    //  return transBracket.Replace(text, m => {
    //    var key = m.Value.Substring(2, m.Value.Length - 4);
    //    var val = replace(key);
    //    //switch (type) {
    //    //  case CourseModel.textItemType.javascript: val = val.Replace("\"", "\\\""); break;
    //    //  case CourseModel.textItemType.attribute:
    //    //    val = attrEntities.Replace(val, mm => {
    //    //      var ent = "";
    //    //      switch (mm.Value[0]) {
    //    //        case '&': ent = "amp"; break;
    //    //        case '\'': ent = "apos"; break;
    //    //        case '"': ent = "quot"; break;
    //    //        case '<': ent = "lt"; break;
    //    //        case '>': ent = "gt"; break;
    //    //        default: throw new NotImplementedException();
    //    //      }
    //    //      return "&" + ent + ";";
    //    //    }); break;
    //    //}
    //    return "{{" + key + "|" + val + "}}";
    //  });
    //} 
    public static Regex localizePartsRegex = new Regex("{{.*?}}", RegexOptions.Singleline);
    //static Regex attrEntities = new Regex(@"(&|""|'|<|>)");

    public static IEnumerable<NameValueString> getLocId2EnglishLoc(string text, string logKey, LoggerMemory log) {
      if (string.IsNullOrEmpty(text)) return Enumerable.Empty<NameValueString>();
      List<NameValueString> res = new List<NameValueString>();
      localizePartsRegex.Replace(text, m => {
        var parts = m.Value.Substring(2, m.Value.Length - 4).Split(new char[] { '|' }, 2);
        if (parts.Length < 2) log.ErrorLine("?", logKey + ": missing english loc - " + m.Value);
        //else res2.Add(new NameValueString(parts[0], TradosLib.xmlToExcel(parts[1])));
        else {
          if (parts[1].Contains('\r') || parts[1].Contains('\n')) log.ErrorLine("?", logKey + ": crlf in localized sentence - " + m.Value);
          else res.Add(new NameValueString(parts[0], parts[1]));
        }
        return "";
      });
      if (res.Count == 0) return Enumerable.Empty<NameValueString>();
      //kontrola na jednoznacnost Keys
      var lk = res.ToLookup(nv => nv.Name);
      var dupls = lk.Where(kv => kv.Select(t => t.Value).Where(v => !string.IsNullOrEmpty(v)).Distinct().Count() > 1).ToArray();
      if (dupls.Length > 0) log.ErrorLine("?", logKey + "localize source mishmash");
      //navrat jednoznacnych hodnot
      return lk.Select(t => t.First());
    }

  }

  public class oldGramm {
    static oldGramm() {
      all = new string[] { "english", "englishe" }.Select(crs => new oldGramm { crs = crs, cnt = 5, isEx = true, exName = "funlang/" }).
        Concat(new string[] { "german" }.Select(crs => new oldGramm { crs = crs, cnt = 3, isEx = true, exName = "kub/" })).
        Concat(new string[] { "spanish", "french", "italian" }.Select(crs => new oldGramm { crs = crs, cnt = 3, isEx = false })).
        ToArray();
    }
    public string crs;
    public int cnt;
    public bool isEx;
    public string exName;
    static oldGramm[] all;
    public static string pathName(string crs, int idx) {
      var og = all.First(g => g.crs == crs);
      var crsLev = crs == "englishe" ? "english" : crs; var e = crs == "englishe" ? "e" : null;
      if (og.isEx && idx >= og.cnt) return crsLev + (idx - og.cnt + 1).ToString() + e + "/" + og.exName;
      else return crsLev + (idx + 1).ToString() + e + "/grammar/";
    }
    public static ptr getPtr(CourseIds crs, params int[] levIdxs) {
      return new ptr(true, urlsLow(crs.ToString().ToLower(), levIdxs).ToArray()) { isGramm = true };
    }
    static IEnumerable<object> urlsLow(string crsStr, params int[] levIdxs) {
      if (crsStr == "russian") yield break;
      var og = all.First(g => g.crs == crsStr);
      if (levIdxs == null || levIdxs.Length == 0) levIdxs = Enumerable.Range(0, og.cnt).ToArray();
      foreach (var levIdx in levIdxs) {
        yield return string.Format("/lm/oldea/{0}", pathName(crsStr, levIdx)).ToLower();
        if (og.isEx) yield return string.Format("/lm/oldea/{0}", pathName(crsStr, levIdx + og.cnt)).ToLower();
      }
      //yield return og.isEx ? ptr.childMode.childsWithParent : ptr.childMode.self;
      yield return childMode.childsWithParentIfMulti;
      yield return runtimeType.grammarRoot;
    }
    public static void setBookName(data[] gramm) {
      foreach (var crs in gramm) {
        //var crsStr = crs.pathPart(pathPartIdx.project);
        var crsStr = crs.pathParts[2];
        var og = all.First(g => g.crs == crsStr);
        for (int i = 0; i < crs.Items.Length; i++) {
          crs.Items[i].spaceId = (string)urlsLow(crsStr, i).First();
          crs.Items[i].globalId = null;
        }
      }
    }
  }


  //public class folder : data {

  //[XmlAttribute, DefaultValue(false), JsonIgnore]
  //public bool isCached; //cviceni takto oznaceneho folderu jsou zabalena do (url + "/cache.json") souboru

  //public static folder fromSitemap(data parent, XElement root, int deep, ref int cnt) {
  //  var res2 = new folder();
  //  res2.spaceId = "data/lm/oldea/data";
  //  res2.globalId = deep == 0 ? root.AttributeValue("id") + "/grammar" : res2.globalId = cnt++.ToString();
  //  res2.isCached = deep == 1;
  //  res2.line = deep == 0 ? CommonLib.CourseIdToLineId(LowUtils.EnumParse<CourseIds>(root.AttributeValue("id"))) : LineIds.no;
  //  res2.title = getLocOldGrammTitle(root);
  //  int subCnt = cnt;
  //  fromSitemap(res2, parent, root, root.Elements("folder").Select(f => f.AttributeValue("globalId") != null ? (data)testEx.fromSitemap(res2, f) : folder.fromSitemap(res2, f, deep + 1, ref subCnt)));
  //  cnt = subCnt;
  //  return res2;
  //}

  //public static string getLocOldGrammTitle(XElement root) {
  //  var res2 = root.AttributeValue("title", "");
  //  if (!res2.StartsWith("{{")) return res2;
  //  string key = res2.Substring(2, res2.Length - 4);
  //  string val = root.Element("en_gb").Value;
  //  return "{{" + key + "|" + val + "}}";
  //}
  //public override data clone() { return cloneLow<folder>(); }

  //public class oldGramm {
  //  static oldGramm() {
  //    all = new string[] { "english", "englishe" }.Select(crs => new oldGramm { crs = crs, cnt = 5, isEx = true, exName = "funlang" }).
  //      Concat(new string[] { "german" }.Select(crs => new oldGramm { crs = crs, cnt = 3, isEx = true, exName = "kub" })).
  //      Concat(new string[] { "spanish", "french", "italian" }.Select(crs => new oldGramm { crs = crs, cnt = 3, isEx = false })).
  //      ToArray();
  //  }
  //  public string crs;
  //  public int cnt;
  //  public bool isEx;
  //  public string exName;
  //  static oldGramm[] all;
  //  public static string pathName(string crs, int idx) {
  //    var og = all.First(g => g.crs == crs);
  //    var crsLev = crs == "englishe" ? "english" : crs; var e = crs == "englishe" ? "e" : null;
  //    if (og.isEx && idx >= og.cnt) return crsLev + (idx - og.cnt + 1).ToString() + e + "/" + og.exName;
  //    else return crsLev + (idx + 1).ToString() + e + "/grammar";
  //  }
  //  public static ptr getPtr(CourseIds crs, params int[] levIdxs) {
  //    return new ptr(urlsLow(crs.ToString().ToLower(), levIdxs).ToArray());
  //  }
  //  static IEnumerable<object> urlsLow(string crsStr, params int[] levIdxs) {
  //    if (crsStr == "russian") yield break;
  //    var og = all.First(g => g.crs == crsStr);
  //    if (levIdxs == null || levIdxs.Length == 0) levIdxs = Enumerable.Range(0, og.cnt).ToArray();
  //    foreach (var levIdx in levIdxs) {
  //      yield return string.Format("lm/oldea/{0}", pathName(crsStr, levIdx)).ToLower();
  //      if (og.isEx) yield return string.Format("lm/oldea/{0}", pathName(crsStr, levIdx + og.cnt)).ToLower();
  //    }
  //    yield return og.isEx ? ptr.childMode.childsWithParent : ptr.childMode.self;
  //    yield return runtimeType.grammar;
  //  }
  //  public static void setBookName(data[] gramm) {
  //    foreach (var crs in gramm) {
  //      //var crsStr = crs.pathPart(pathPartIdx.project);
  //      var crsStr = crs.pathParts[2];
  //      var og = all.First(g => g.crs == crsStr);
  //      for (int i = 0; i < crs.items.Length; i++) {
  //        crs.items[i].spaceId = (string)urlsLow(crsStr, i).First();
  //        crs.items[i].globalId = null;
  //      }
  //    }
  //  }
  //}

  //}

  public enum childMode {
    child, //vlozi childs, ev. omezene skip-take
    self, //vlozi self
    selfChild, //vlozi self a childs, ev. omezene skip-take
    childsWithParent, //vlozi spolecneho parent a childs, specifikovane v urls
    childsWithParentIfMulti, //vlozi spolecneho parent a childs, specifikovane v urls. Parenta vlozi pouze kdyz je vic nez jeden childs
    skrivanek_multiTest_std, //specialni pravidlo pro standardni multitest skrivanka
    skrivanek_multiTest_compl, //specialni pravidlo pro standardni multitest skrivanka
  }

  public class ptr : data {

    [XmlAttribute, DefaultValue(0)]
    public childMode takeChilds;
    [XmlAttribute, DefaultValue(0)]
    public int skip;
    [XmlAttribute, DefaultValue(0)]
    public int take;
    public string[] urls;
    [XmlAttribute, DefaultValue(0)]
    public bool isGramm;
    [XmlIgnore]
    data pattern;
    [XmlAttribute]
    public string modify;

    public ptr() { }
    public ptr(bool nopar, params object[] data) {
      urls = prodDef.flateArrays(data).OfType<string>().ToArray();
      var skipTake = prodDef.flateArrays(data).OfType<int>().ToArray();
      skip = skipTake.Length > 0 ? skipTake[0] : 0; take = skipTake.Length > 1 ? skipTake[1] : 0;
      var modes = data.OfType<childMode>().ToArray();
      takeChilds = modes.Length == 0 ? childMode.child : modes[0];
    }
    public ptr(string modify, params object[] data)
      : this(true, data) {
      this.modify = modify;
    }
    public ptr(data pattern, params object[] data)
      : this(true, data) {
      this.pattern = pattern;
    }
    public IEnumerable<data> expand(sitemap root, LoggerMemory log) {
      Func<data, data> clone = dt => {
        data res;
        if (pattern != null) {
          res = pattern.clone(); dt.AssignTo(res); res.Items = dt.clone().Items;
        }
        else {
          res = dt.clone();
          if (modify != null) {
            foreach (var par in modify.Split(';').Select(p => p.Split('='))) {
              if (res is test) {
                var t = (test)res;
                switch (par[0]) {
                  case "demoTestUrl": t.demoTestUrl = par[1]; break;
                  case "needs": t.needs = LowUtils.EnumParse<testNeeds>(par[1]); break;
                  default: throw new Exception("Canot use test par: " + par[0]);
                }
              }
              else throw new Exception("Canot use ptr.modify par: " + par[0]);
            }
          }
        }
        res.line = dt.getLine();
        return res;
      };
      Func<data, data> addGrammarFlag = dt => { if (isGramm) dt.type |= runtimeType.grammarRoot; return dt; };
      data[] dts = urls.Select(url => root.find(url, log)).Where(d => d != null).ToArray();
      switch (takeChilds) {
        case childMode.self:
          foreach (var dt in dts) {
            var cl = clone(dt);
            if (!string.IsNullOrEmpty(name) && string.IsNullOrEmpty(cl.name)) cl.name = name;
            yield return addGrammarFlag(cl);
          }
          break;
        case childMode.child:
          foreach (var dt in dts) {
            if (take > 0) foreach (var d in dt.Items.Skip(skip).Take(take)) yield return addGrammarFlag(clone(d));
            else if (skip > 0) foreach (var d in dt.Items.Skip(skip)) yield return addGrammarFlag(clone(d));
            else foreach (var d in dt.Items) yield return addGrammarFlag(clone(d));
          }
          break;
        case childMode.selfChild:
          foreach (var dt in dts.Select(d => clone(d))) {
            if (dt.Items != null) dt.Items = dt.Items.OrderBy(d => d.order).ToArray();
            if (take > 0) dt.Items = dt.Items.Skip(skip).Take(take).ToArray();
            else if (skip > 0) dt.Items = dt.Items.Skip(skip).ToArray();
            yield return addGrammarFlag(dt);
          }
          break;
        case childMode.childsWithParent:
          if (dts.Select(dt => dt.parent.url).Distinct().Count() > 1) throw new Exception("childMode.childsWithParent: not common parent");
          var res = clone(root.find(dts[0].parent.url));
          res.Items = res.Items.Where(it => urls.Contains(it.url)).ToArray();
          yield return addGrammarFlag(res);
          break;
        case childMode.childsWithParentIfMulti:
          if (dts.Select(dt => dt.parent.url).Distinct().Count() > 1) throw new Exception("childMode.childsWithParent: not common parent");
          var r = clone(root.find(dts[0].parent.url));
          r.Items = r.Items.Where(it => urls.Contains(it.url)).ToArray();
          if (r.Items.Length == 1) {
            var child = r.Items[0];
            //if (type != runtimeType.no) child.type |= type;
            child.line = r.getLine();
            r = child;
          }
          yield return addGrammarFlag(r);
          break;
        case childMode.skrivanek_multiTest_std: //vezme prvni 3 prvky kazde level
        case childMode.skrivanek_multiTest_compl: //vezme vsechny prvky kazde level
          var src = dts.First();
          var res2 = clone(src);
          var isStd = takeChilds == childMode.skrivanek_multiTest_std;
          var islm = res2.url.StartsWith("/lm/");
          if (isStd)
            foreach (var it in res2.Items.Cast<test>()) it.Items = it.Items.OrderBy(d => d.order).Take(3).ToArray();
          //demoTestUrl
          foreach (var it in res2.Items.Cast<test>()) {
            //if (res2.line == LineIds.English) //zatim jen pro anglictinu!!!
            it.demoTestUrl = (islm ? "/lm" : "/skrivanek") + string.Format("/prods/etestme-{0}-demo/{1}/{2}/", takeChilds == childMode.skrivanek_multiTest_std ? "std" : "comp", res2.line, it.level);
            it.needs = isStd ? testNeeds.playing : testNeeds.recording;
          }
          //questionnaire
          int moduleCount; lock (typeof(ptr)) moduleCount = questionnaireCount++;
          data quests = root.find("/skrivanek/questionnaire/", log).clone();
          quests.url += moduleCount.ToString() + "/"; //kvuli lokalizaci musi byt URL modulu jednoznacne
          quests.line = src.getLine();
          quests.type |= runtimeType.multiQuestionnaire;
          var url = (res2.url.Replace("/skrivanek/", "/skrivanek/questionnaire/").TrimEnd('/') + (isStd ? "std" : null)).ToLower();
          quests.Items = quests.Items.Where(it => it.url == url).ToArray();
          if (quests.Items.Length == 1) {
            quests.Items[0].type |= runtimeType.multiQuestionnaire;
            Array.Resize(ref res2.Items, res2.Items.Length + 1);
            res2.Items[res2.Items.Length - 1] = quests;
          }
          yield return res2;
          break;
      }
    }
    static int questionnaireCount = 0;
    public override data clone() { return cloneLow<ptr>(p => { p.modify = modify; p.isGramm = isGramm; p.pattern = pattern; p.takeChilds = takeChilds; p.skip = skip; p.take = take; p.urls = urls == null ? null : urls.ToArray(); }); }

  }

  public static class prodDef {
    public static data genCourse(sitemap publishers, string publisherId, string prodId, CourseIds crsId, bool isTest, dictTypes defaultDictType, Langs[] defaultLocs, params object[] data) {
      var tasks = flateArrays(data).OfType<data>().Where(d => !(d is ptr) || !((ptr)d).isGramm).ToArray();
      if (tasks.Length == 0) throw new Exception("tasks.Length == 0");
      var gramPtrs = flateArrays(data).OfType<ptr>().Where(p => p.isGramm).ToArray();
      var tit = flateArrays(data).OfType<string>().FirstOrDefault();
      if (tit == null) throw new Exception("Missing Product title");
      if (tasks.Length == 1 && tasks[0].title == null) tasks[0].title = tit;
      var url = ("/" + publisherId + "/" + prodId + "/").ToLower();
      product res = new product {
        title = tit,
        line = CommonLib.CourseIdToLineId(crsId),
        url = url,
        defaultDictType = defaultDictType,
        defaultLocs = defaultLocs,
        Items = (tasks.Length == 1 ? XExtension.Create<data>(tasks[0]) : XExtension.Create<data>(new data { type = /*isTest ? runtimeType.multiTest :*/ runtimeType.multiTask, Items = tasks, url = url + "crs/", title = tit })).
          Concat(gramPtrs).ToArray()
      };
      if (isTest) res.type |= runtimeType.test;
      //if (tasks.Length > 1) res2.type |= runtimeType.multiTest;
      //}
      res.finishCreate();
      return res;
    }
    public static data genTaskCourse(string title, string url, params object[] data) {
      var ptrs = flateArrays(data).OfType<ptr>().ToArray();
      return new taskCourse {
        title = title,
        url = url,
        Items = ptrs,
      };
    }
    public static IEnumerable<object> flateArrays(object[] data) {
      foreach (var d in data)
        if (d != null) if (d.GetType().IsArray) foreach (var dd in ((IEnumerable)d).OfType<object>()) yield return dd; else yield return d;
    }
    public static IEnumerable<string> instrUrls(string[] instrs) {
      return instrs == null ? Enumerable.Empty<string>() : instrs.Select(i => i.StartsWith("/") ? i : "/data/instr/std/" + i);
    }
    public static void addInstructions(data self, LoggerMemory log) {
      if (self.Items == null) return;
      var q1 = self.scan().OfType<ex>().
          Where(e => e.instrs != null).
          SelectMany(e => e.instrs).ToArray();
      var q2 = q1.Select(i => i.StartsWith("/") ? i : "/data/instr/std/" + i).
          Distinct().
          Select(url => new ex { url = url }).
          ToArray();
      self.Items = self.Items.Concat(XExtension.Create(new mod {
        title = "Instructions",
        url = self.url.Substring(0, self.url.Length - 1) + "_instrs/",
        type = runtimeType.mod | runtimeType.instrs | runtimeType.noDict,
        Items = self.scan().OfType<ex>().
          Where(e => e.instrs != null).
          SelectMany(e => e.instrs).
          Select(i => i.StartsWith("/") ? i : "/data/instr/std/" + i).
          Distinct().
          Select(url => new ex { url = url }).
          ToArray()
      })).ToArray();
    }
    public static data expand(data self, sitemap root, LoggerMemory log) {
      //expand inPlace exercise
      //foreach (var e in self.scan().OfType<testEx>().Where(p => p.inPlace)) e.inPlacePage = e.readPage(log);
      //expand pointers
      var ptrParents = self.scan().Where(p => p.Items != null && p.Items.Any(t => t is ptr)).ToArray();
      List<data> buf = new List<data>();
      foreach (var par in ptrParents) {
        buf.Clear();
        foreach (var ch in par.Items)
          if (ch is ptr) buf.AddRange(((ptr)ch).expand(root, log)); else buf.Add(ch);
        par.Items = buf.ToArray();
      }
      //pouziti dataItems
      foreach (var nd in self.scan()) {
        if (nd.dataItems != null) {
          nd.Items = nd.dataItems;
          nd.dataItems = null;
          expand(nd, root, log);
        }
      }
      self.finishRead();
      HashSet<string> allNodes = new HashSet<string>(); List<string> errors = new List<string>();
      foreach (var prod in self.scan().Where(dt => dt.isType(runtimeType.product))) {
        if (string.IsNullOrEmpty(prod.title)) prod.title = prod.Items[0].title;
        allNodes.Clear(); errors.Clear();
        foreach (var nd in prod.scan().Where(n => !string.IsNullOrEmpty(n.url))) if (allNodes.Contains(nd.url)) errors.Add(nd.url); else allNodes.Add(nd.url);
        if (errors.Count > 0) throw new Exception("Product Url duplicity in " + prod.url + ": " + errors.Aggregate((r, i) => r + ", " + i));
        var lines = prod.scan().Select(nd => nd.line).Where(l => l != LineIds.no).Distinct().ToArray();
        if (lines.Length != 1) throw new Exception("Unknown product line: " + lines.Select(l => l.ToString()).DefaultIfEmpty().Aggregate((r, i) => r + ", " + i));
        prod.line = lines[0];
      }
      //kontrola a optimalizace pro XML ulozeni
      foreach (var nd in self.scan()) {
        if (!nd.isType(runtimeType.product)) nd.line = LineIds.no;
        nd.order = 0;
        if (nd.Items != null && nd.Items.Any(d => d.isType(runtimeType.mediaDir)))
          nd.Items = nd.Items.Where(d => !d.isType(runtimeType.mediaDir)).ToArray();
      }
      return self;
    }
  }
  //definice produktu, v items je seznam kurzu
  //public class course : data {

  //public course() { }
  //public course(sitemap publishers, string publisherId, string prodId, CourseIds crsId, params object[] data) {
  //  var levels = flateArrays(data).OfType<task>().ToArray();
  //  var ptrs = flateArrays(data).OfType<ptr>().ToArray();
  //  //var langs = expandData(data).OfType<Langs>().ToArray();
  //  var tit = flateArrays(data).OfType<string>().FirstOrDefault();
  //  tit = (tit ?? levels[0].title) ?? publishers.find(levels[0].items[0].url ?? ((ptr)levels[0].items[0]).urls[0]).title;
  //  if (levels.Length == 1 && levels[0].title == null) levels[0].title = tit;
  //  title = tit; line = CommonLib.CourseIdToLineId(crsId);
  //  url = "prod/" + (publisherId + "/" + prodId).ToLower();
  //  type = runtimeType.course;
  //  items = XExtension.Create<data>(new multiTask { type = runtimeType.multiTask, items = levels.ToArray(), url = url + "/crs", title = title }).Concat(ptrs).ToArray();
  //  checkUniqueUrls();
  //}

  //public static IEnumerable<object> flateArrays(object[] data) {
  //  foreach (var d in data)
  //    if (d != null) if (d.GetType().IsArray) foreach (var dd in ((IEnumerable)d).OfType<object>()) yield return dd; else yield return d;
  //}
  //public static data expand(data self, sitemap root) {
  //  var ptrParents = self.scan().Where(p => p.items != null && p.items.Any(t => t is ptr)).ToArray();
  //  List<data> buf = new List<data>();
  //  foreach (var par in ptrParents) {
  //    buf.Clear();
  //    foreach (var ch in par.items)
  //      if (ch is ptr) buf.AddRange(((ptr)ch).expand(root)); else buf.Add(ch);
  //    par.items = buf.ToArray();
  //  }
  //  self.fillParentsAndOrder();
  //  foreach (var nd in self.scan()) {
  //    if (nd is testEx) ((testEx)nd).isOldEA = false;
  //    else if (nd is mod) ((mod)nd).isOldEA = false;
  //    else if (nd is folder) ((folder)nd).isCached = false;
  //    nd.order = 0;
  //  }
  //  return self;
  //}
  //public override data clone() { return cloneLow<course>(); }
  //}
  //seznam produktu
  public class products : data {
    public products() { type = runtimeType.products; }
    public override data clone() { return cloneLow<products>(); }
  }

  //adresar skupin souboru dle buildId
  public class buildsDir {
    public Dictionary<string, products> builds;
  }

  public class product : data {
    [XmlAttribute, DefaultValue(dictTypes.unknown)]
    public dictTypes defaultDictType;
    [XmlAttribute]
    public Langs[] defaultLocs;

    public product() { type = runtimeType.product; }
    public override data clone() { return cloneLow<product>(p => { p.defaultDictType = defaultDictType; p.defaultLocs = defaultLocs; }); }
  }

  public class taskTestInCourse : data {
    public taskTestInCourse() { type = runtimeType.taskTestInCourse; }
    public override data clone() { return cloneLow<taskTestInCourse>(); }
  }

  public enum testNeeds { no, playing, recording }

  public class test : data {
    [XmlAttribute]
    public string demoTestUrl;
    [XmlAttribute]
    public string level;
    [XmlAttribute]
    public testNeeds needs;
    [XmlAttribute]
    public bool isDemoTest;
    public test() { type = runtimeType.test; }
    public override data clone() { return cloneLow<test>(t => { t.demoTestUrl = demoTestUrl; t.level = level; t.needs = needs; t.isDemoTest = isDemoTest; }); }
  }

  public class mod : data {
    public mod() { type = runtimeType.mod; }
    public override data clone() { return cloneLow<mod>(); }
  }

  public class taskCourse : data {
    public taskCourse() { type = runtimeType.taskCourse; }
    public override data clone() { return cloneLow<taskCourse>(); }
  }

  public class multiTask : data {
    public multiTask() { type = runtimeType.multiTask; }
    public override data clone() { return cloneLow<multiTask>(); }
  }

  public class taskTestSkill : data {
    [XmlAttribute]
    public string skill;
    [XmlAttribute, DefaultValue(0)]
    public int minutes;
    [XmlAttribute, DefaultValue(0)]
    public int scoreWeight;
    public taskTestSkill() { type = runtimeType.taskTestSkill; }
    public override data clone() { return cloneLow<taskTestSkill>(c => { c.skill = skill; c.minutes = minutes; c.scoreWeight = scoreWeight; }); }
  }

  //public class multiTask : data {

  //  public multiTask() { }
  //  //public multiTask(params object[] data) {
  //  //  title = prodDef.flateArrays(data).OfType<string>().FirstOrDefault();
  //  //  var ptrs = prodDef.flateArrays(data).OfType<ptr>().ToArray();
  //  //  url = ptrs[0].url; items = ptrs;
  //  //}

  //  //public static IEnumerable<data> fromSitemap(data parent, XElement root) {
  //  //  return root.Elements().Select(el => data.fromSitemapTask(parent, el));
  //  //}

  //  public static multiTask fromFilesystem(project parent, string dir) {
  //    multiTask res2 = sitemap.safeLoadEx<multiTask>(dir + "\\meta.xml");
  //    res2.parent = parent;
  //    res2.url = urlFromFileName(dir);
  //    //TODO res2.items = Directory.EnumerateDirectories(dir).Where(d => d.IndexOf("meta.xml") < 0).Select(d => data.fromFilesystem(res2, d)).OrderBy(l => l.order).Cast<data>().ToArray();
  //    return res2;
  //  }
  //  public override data clone() { return cloneLow<multiTask>(); }
  //}

  public static class fromSitemapLib {

    public static IEnumerable<data> course(data parent, XElement root) {
      return root.Elements().Select(el => fromSitemapTask(parent, el));
    }

    public static data grammar(data parent, XElement root, int deep, ref int cnt) {
      data res = deep == 1 ? new mod() : new data();
      res.line = deep == 0 ? CommonLib.CourseIdToLineId(LowUtils.EnumParse<CourseIds>(root.AttributeValue("id"))) : LineIds.no;
      res.spaceId = "/lm/oldea";
      if (deep == 0)
        res.globalId = root.AttributeValue("id") + "/grammar";
      else if (deep == 1) {
        var crsStr = root.Parent.AttributeValue("id");
        var idx = root.Parent.Elements("folder").IndexOf(el => el == root);
        res.globalId = oldGramm.pathName(crsStr, idx);
      }
      else
        res.globalId = res.globalId = cnt++.ToString();
      if (deep == 1) res.type = res.type | runtimeType.mod;
      //res2.type = runtimeType.lesson;
      res.title = getLocOldGrammTitle(root);
      int subCnt = cnt;
      fromSitemapLow(res, parent, root, root.Elements("folder").Select(f => f.AttributeValue("globalId") != null ? (data)fromSitemapEx(res, f, false) : grammar(res, f, deep + 1, ref subCnt)));
      cnt = subCnt;
      //if (res2.isCached) res2.tradosPage = res2.url;
      return res;
    }

    static data fromSitemapTask(data parent, XElement root) {
      var res = new data();
      fromSitemapLow(res, parent, root, null);
      res.line = CommonLib.CourseIdToLineId(LowUtils.EnumParse<CourseIds>(removeNum.Replace(res.spaceId, "")));
      res.spaceId = parent.spaceId + (parent.spaceId.EndsWith("/") ? null : "/") + res.spaceId;
      res.Items = root.Elements().Select(el => fromSitemapLesson(res, el)).ToArray();
      //res.ms = res.Items.Sum(it => it.ms);
      return res;
    }
    static Regex removeNum = new Regex(@"\d");

    static data fromSitemapLesson(data parent, XElement root) {
      var res = new data { spaceId = parent.spaceId };
      fromSitemapLow(res, parent, root, root.Elements().Select(el => fromSitemapMod(res, el)));
      //res.ms = res.Items.Sum(it => it.ms);
      return res;
    }

    static data fromSitemapMod(data parent, XElement root) {
      var res = new mod { spaceId = parent.spaceId, /*type = runtimeType.lesson,*/ type = runtimeType.mod/* isDataHolder = true*/ };
      fromSitemapLow(res, parent, root, root.Elements().Select(el => fromSitemapEx(res, el, true)));
      //res.ms = res.Items.Cast<ex>().Where(e => !e.isOldEaPassive).Count(); ;
      return res;
    }

    static ex fromSitemapEx(data parent, XElement root, bool isCourse) {
      ex res = new ex { isOldEa = true };
      if (isCourse) {
        res.globalId = parent.globalId + "/" + root.AttributeValue("url"); res.spaceId = parent.spaceId;
        var oldEx = XElement.Load(res.fileName()).Element("body");
        var instrs = oldEx.AttributeValue("instrs-str");
        if (!string.IsNullOrEmpty(instrs)) res.instrs = instrs.ToLower().Split('|');
        res.isOldEaPassive = oldEx.AttributeValue("old-ea-is-passive") == "true";
        //res.ms = oldEx.AttributeValue("old-ea-is-passive") == "true" ? 0 : 1;
        fromSitemapLow(res, parent, root, null);
      }
      else {
        res.title = getLocOldGrammTitle(root);
        fromSitemapLow(res, parent, root, null);
        res.spaceId = "/lm/oldea/" + res.spaceId;
        res.globalId = res.globalId.Replace(".htm", null);
      }
      return res;
    }

    static void fromSitemapLow(data self, data parent, XElement el, IEnumerable<data> items) {
      self.parent = parent;
      self.title = self.title ?? el.AttributeValue("title");
      self.spaceId = self.spaceId ?? el.AttributeValue("spaceId");
      self.globalId = self.globalId ?? el.AttributeValue("globalId");
      self.Items = items == null ? null : items.ToArray();
      if (self.Items != null && self.Items.Length == 0) self.Items = null;
    }

    static string getLocOldGrammTitle(XElement root) {
      var res = root.AttributeValue("title", "");
      if (!res.StartsWith("{{")) return res;
      string key = res.Substring(2, res.Length - 4);
      string val = root.Element("en_gb").Value;
      return "{{" + key + "|" + val + "}}";
    }
  }


  public class dynamicModuleData : data {
    public testTaskGroup[] groups;
    //public void finishBeforeJson(bool testDebug) {
    //  if (testDebug) {
    //    parent.Items = this.Items.OfType<testTaskGroup>().Select(tg => new data {
    //      type = runtimeType.mod,
    //      Items = tg.Items,
    //      url = "modcnt_" + (testDebugModCount++).ToString(),
    //      title = tg.designTitle ?? "part " + testDebugModCount.ToString()
    //    }).ToArray();
    //    foreach (var sr in parent.scanEx()) sr.parent.Items[sr.itemIdx].parent = sr.parent;
    //  } else {
    //    groups = Items.OfType<testTaskGroup>().ToArray(); Items = null;
    //    foreach (var tg in groups) {
    //      tg.urls = tg.Items.Select(it => it.url).ToArray(); tg.Items = null;
    //    }
    //  }
    //}
    public override data clone() { return cloneLow<dynamicModuleData>(tg => { tg.groups = groups == null ? null : groups.Select(g => g.clone()).OfType<testTaskGroup>().ToArray(); }); }
    //static int testDebugModCount = 0;
  }
  public class testTaskGroup : data {
    public testTaskGroup() { type = runtimeType.testTaskGroup; }
    public string[] urls;
    [XmlAttribute]
    public int take; //z urls vyber nahodne take-cviceni
    [XmlAttribute]
    public string designTitle;
    public override data clone() { return cloneLow<testTaskGroup>(tg => { tg.take = take; tg.designTitle = designTitle; tg.urls = urls == null ? null : urls.ToArray(); }); }
  }

  //public class mod : data {

  //  //[XmlAttribute, DefaultValue(0)]
  //  //public int testTake;
  //  //[XmlAttribute, DefaultValue(false), JsonIgnore]
  //  //public bool isOldEA;

  //  //public static mod fromSitemap(data parent, XElement root) {
  //  //  var res2 = new mod { spaceId = parent.spaceId, type = runtimeType.mod, isOldEA = true };
  //  //  fromSitemap(res2, parent, root, root.Elements().Select(el => testEx.fromSitemap(res2, el)));
  //  //  return res2;
  //  //}
  //  public static mod fromFilesystem(data parent, string dir) {
  //    var res2 = sitemap.safeLoadEx<mod>(dir + @"\meta.xml");
  //    //res2.type = runtimeType.mod;
  //    res2.parent = parent;
  //    res2.url = urlFromFileName(dir);
  //    //res2.spaceId = parent.spaceId;
  //    //var mod = CourseModel.Lib.readModule(dir);
  //    //mod res2 = new mod {
  //    //  title = mod.title,
  //    //  testTake = mod.testTake,
  //    //  spaceId = parent.spaceId,
  //    //  parent = parent,
  //    //  order = mod.order,
  //    //};
  //    //res2.globalId = dir.Substring(res2.publ.basicDir.Length + parent.spaceId.Length + 1).Replace('\\', '/').ToLowerInvariant();
  //    //res2.items = Directory.EnumerateFiles(dir, "*.xml").Select(d => d.ToLowerInvariant()).Where(fn => fn.IndexOf("meta.xml") < 0).Select(fn => testEx.fromModule(res2, fn)).OrderBy(l => l.order).Cast<data>().ToArray();
  //    res2.items = Directory.EnumerateFiles(dir, "*.xml").Select(d => d.ToLowerInvariant()).Where(d => d.IndexOf("meta.xml") < 0).Select(d => testEx.fromFilesystem(res2, d.ToLowerInvariant())).OrderBy(l => l.order).Cast<data>().ToArray();
  //    //if (res2.items.Length == 0) res2.items = null;
  //    return res2;
  //  }
  //  public override data clone() { return cloneLow<mod>(c => { c.testTake = testTake; /*c.isOldEA = isOldEA;*/ }); }

  //}

  public class ex : data {
    public ex() { type = runtimeType.ex; }

    [XmlAttribute, DefaultValue(false), JsonIgnore]
    public bool isOldEa;
    [XmlAttribute, DefaultValue(false), JsonIgnore]
    public bool isOldEaPassive;

    [XmlAttribute, JsonIgnore]
    public string[] instrs; //instrukce

    //[XmlAttribute, DefaultValue(false), JsonIgnore]
    //public bool inPlace; //obsah cviceni je soucasti sitemap

    //zakladni funkce pro nacteni cviceni
    public CourseModel.body readPageLow(XElement root, LoggerMemory logger) {
      //log orig XML

      //DOC examples
      foreach (var tag in root.Descendants("doc-example")) CourseModel.docExample.beforeFromElement(tag); //dosazeni zdroje prikladu
      //fromElement
      CourseModel.body res = CourseModel.tag.FromElement<CourseModel.body>(root, logger, url);
      res.url = url;
      CourseModel.Lib.PostProcessPage(ref res, this, logger);
      if (logger.strictChecking) { //kontrola existence externals
        foreach (var extFn in res.scan().SelectMany(t => t.getExternals(res)).Distinct()) {
          if (extFn.IndexOf("://") > 0) continue;
          var fullUrl = VirtualPathUtility.Combine(res.url, extFn.ToLower());
          var fn = Machines.rootDir + fullUrl.Replace('/', '\\');
          if (File.Exists(fn)) continue;
          logger.hasError = true;
          logger.ErrorLineFmt(url, "external file {1} does not exists", url, fn);
        }

      }
      return res;
    }

    public override CourseModel.tag readTag(LoggerMemory logger) {
      return readPage(logger);
    }

    public CourseModel.body readPage(LoggerMemory logger) {
      return readPageLow(CourseModel.tag.loadExerciseXml(data.fileNameFromUrl(url), logger), logger);
    }

    public static CourseModel.body readPage(string url, LoggerMemory logger) {
      ex fakeEx = new ex { url = url.ToLower(), styleSheet = stdStyle };
      return fakeEx.readPage(logger);
    }

    public static CourseModel.body readPage(XElement root, string url, LoggerMemory logger) {
      ex fakeEx = new ex { url = url.ToLower(), styleSheet = stdStyle };
      return fakeEx.readPageLow(root, logger);
    }

    //public CourseModel.body inPlacePage;

    public IEnumerable<NameValueString> toTransSentences(LoggerMemory logger) { return readPage(logger).toTransSentences(logger); }

    public override data clone() { return cloneLow<ex>(c => { c.isOldEa = isOldEa; c.isOldEaPassive = isOldEaPassive; c.instrs = instrs == null ? null : instrs.Select(i => i).ToArray(); /*c.inPlace = inPlace;*/ }); }

    public static string stdStyle {
      get {
        if (_stdStyle == null) {
          _stdStyle = File.ReadAllText(Machines.dataPath + @"default.css", Encoding.UTF8).Trim() + " ";
          //var fn = Machines.dataPath + @"default.css";
          //if (File.Exists(fn)) _stdStyle = File.ReadAllText(Machines.rootPath + @"data\default.css").Trim() + " ";
          //else {
          //  fn = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\resources\data\default.css";
          //  if (File.Exists(fn)) _stdStyle = File.ReadAllText(fn).Trim() + " ";
          //  else _stdStyle = "gap-fill, drop-down {  width:0; }";
          //}
        }
        return _stdStyle;
      }
    }
    static string _stdStyle;


  }

}





