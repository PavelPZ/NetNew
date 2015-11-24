using DesignNew;
using LMComLib;
using LMNetLib;
using Newtonsoft.Json;
//using System.Xml;
using schools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.Xsl;
using xml = System.Xml;

namespace CourseMeta {

  public class WebDataBatch {

    public static void csiExeTest() {
      File.WriteAllText(@"d:\temp\pom.txt", "hallo");
    }


    [XmlAttribute]
    public string url;
    public BatchProduct[] products;
    public string[] productPtrs;
    [XmlAttribute]
    public Langs[] locs;
    //[XmlAttribute]
    //public Langs[] allLocs;
    [XmlAttribute]
    public dictTypes dictType;
    [XmlAttribute]
    public bool globalsOnly;
    [XmlAttribute]
    public bool genDebugJS;

    public static WebDataBatch Load(string fn) { return FinishAfterLoad(XmlUtils.FileToObject<CourseMeta.WebDataBatch>(fn)); }

    public static WebDataBatch FinishAfterLoad(WebDataBatch res) {
      if (res.productPtrs != null)
        res.products = res.productPtrs.Select(fn => XmlUtils.FileToObject<CourseMeta.WebDataBatch>(fn)).SelectMany(wb => wb.products).ToArray();
      if (res.products != null) foreach (var p in res.products) p.owner = res;
      //if (res.genDebugJS) res.allLocs = Cache.debugBigLocs;
      //else if (res.allLocs == null) res.allLocs = CommonLib.bigLocalizations;
      return res;
    }

    public IEnumerable<Packager.Consts.file> getWebGlobalFiles(product[] buildProds, LoggerMemory logger) {
      byte[] jsonProdTree, rjsonProdTree; products prods;
      var isLocalBuild = url == "/lm/lm_data/";

      getJSSitemap(isLocalBuild ? Lib.prods.Items.Cast<product>() : buildProds, out jsonProdTree, out rjsonProdTree, out prods);
      yield return new Packager.Consts.file("siteroot.js", rjsonProdTree);
      yield return new Packager.Consts.file(data.urlStripLast(url) + ".js", rjsonProdTree);

      //vyber pouze pouzite produkty:
      yield return new Packager.Consts.file("Data/productsCurrent.xml", XmlUtils.ObjectToBytes(prods));
      if (!isLocalBuild) {
        HashSet<string> prodUrls = new HashSet<string>(prods.Items.Cast<product>().Select(p => p.url));
        var expanded = new products { Items = Lib.prodExpanded.Items.Cast<product>().Where(p => prodUrls.Contains(p.url)).ToArray() };
        yield return new Packager.Consts.file("Data/ActProductsExpanded.xml", XmlUtils.ObjectToBytes(expanded));
      }
    }

    public IEnumerable<Packager.Consts.file> getWebBatchFilesNew(DesignNew.BuildIds buildId, LoggerMemory logger, string prodDir) {
      using (CourseMeta.Cache cache = new CourseMeta.Cache(logger)) {
        var buildProds = products.Select(p => p.getBuildProduct()).ToArray();
        //files
        var files = buildLib.getProductFiles(cache, buildProds, logger);

        List<Packager.Consts.file> globals = new List<Packager.Consts.file>();

        //globals do products/
        var prods = buildProds.Select(p => p.prod).ToArray();
        byte[] jsonProdTree, rjsonProdTree; products prodProxies;
        getJSSitemap(prods, out jsonProdTree, out rjsonProdTree, out prodProxies);
        var build = buildId.ToString();
        HashSet<string> prodUrls = new HashSet<string>(prodProxies.Items.Cast<product>().Select(p => p.url));
        var expanded = new products { Items = Lib.prodExpanded.Items.Cast<product>().Where(p => prodUrls.Contains(p.url)).ToArray() };
        Packager.Consts.file actProdProxy;
        globals.Add(new Packager.Consts.file("products/" + build + ".js", rjsonProdTree));
        globals.Add(actProdProxy = new Packager.Consts.file("products/" + build + ".xml", XmlUtils.ObjectToBytes(prodProxies)));
        globals.Add(new Packager.Consts.file("products/" + build + "-expanded.xml", XmlUtils.ObjectToBytes(expanded)));

        //soucet vsech product proxies do products/builds.js: z <buildIds>.xml udela buildsDir, ten serializuje
        buildsDir bd = new buildsDir() { builds = new Dictionary<string, products>() { { buildId.ToString(), prodProxies } } };
        foreach (var bld in LowUtils.EnumGetValues<BuildIds>().Where(b => b != buildId)) {
          var fn = string.Format(@"{0}{1}.xml", prodDir, bld); if (!File.Exists(fn)) continue;
          var bldPrx = XmlUtils.FileToObject<products>(fn);
          bd.builds.Add(bld.ToString(), bldPrx);
        }
        var bdJs = LowUtils.serializeObjectToJS(bd); var bdRjs = buildLib.rjsonSign + ClearScript.Lib.JSON2RJSON(bdJs);
        globals.Add(new Packager.Consts.file("products/builds.js", Encoding.UTF8.GetBytes(bdRjs)));
        globals.Add(new Packager.Consts.file("products/builds-plain.js", Encoding.UTF8.GetBytes(bdJs)));

        //return
        return files.Concat(globals);
      }
    }

    public IEnumerable<Packager.Consts.file> getWebBatchFiles(LoggerMemory logger, bool incGlobalFiles = true) {
      using (CourseMeta.Cache cache = new CourseMeta.Cache(logger)) {
        var buildProds = products.Select(p => p.getBuildProduct());
        //datove soubory
        if (!globalsOnly) foreach (var f in buildLib.getProductFiles(cache, buildProds, logger)) yield return f;
        //sitemaps apod.
        if (incGlobalFiles) foreach (var f in getWebGlobalFiles(buildProds.Select(p => p.prod).ToArray(), logger)) yield return f;
      }
    }

    public static void getJSSitemap(IEnumerable<product> products, out byte[] json, out byte[] rjson, out products prods) {
      List<product> clonedProds = new List<product>();
      foreach (var prod in products) {
        var its = prod.Items; prod.Items = null;
        var pr = (product)prod.clone(); clonedProds.Add(pr);
        //vyhod nepotrebne props
        pr.allLocs = pr.defaultLocs = null; pr.defaultDictType = dictTypes.unknown; pr.other = null;
        prod.Items = its;
      }
      prods = new products { Items = clonedProds.ToArray(), url = "/prods/" };
      string prodTree = LowUtils.serializeObjectToJS(prods);
      rjson = Encoding.UTF8.GetBytes(buildLib.rjsonSign + ClearScript.Lib.JSON2RJSON(prodTree));
      json = Encoding.UTF8.GetBytes(prodTree);
    }

  }

  public class BatchProduct {
    public byte[] siteRoot(product prod) {
      byte[] jsonProdTree, rjsonProdTree; products prods;
      WebDataBatch.getJSSitemap(new product[] { prod }, out jsonProdTree, out rjsonProdTree, out prods);
      return rjsonProdTree;
    }

    public buildProduct getBuildProduct() {
      buildProduct res;
      try {
        var prod = Lib.prodExpanded.Items.Cast<product>().First(p => p.url == id);
        res = new buildProduct {
          //PROD NEW
          prod = prod,
          natLangs = (locs ?? owner.locs) ?? CommonLib.bigLocalizations,
          dictType = dictType == dictTypes.unknown ? (owner.dictType == dictTypes.unknown ? dictTypes.no : owner.dictType) : dictType,
        };
      } catch (Exception exp) {
        throw new Exception(id, exp);
      }
      return res;
    }

    [XmlIgnore]
    public WebDataBatch owner;
    [XmlAttribute]
    public string id { get { return _id; } set { _id = value.ToLower(); } }
    string _id;
    [XmlAttribute]
    public dictTypes dictType;
    [XmlAttribute]
    public Langs[] locs;

  }

  public static class buildLib {

    public const string rjsonSign = "@";

    public static IEnumerable<Packager.Consts.file> getProductFiles(Cache cache, IEnumerable<buildProduct> products, LoggerMemory logger, sitemap sm = null) {
      //return products.SelectMany(bp => bp.getFiles(cache, sm));
      return products.AsParallel().SelectMany(bp => bp.getFiles(cache, logger, sm));
    }

    public static void writeVirtualFiles(BuildIds buildId, IEnumerable<Packager.Consts.file> constFiles) {

      var envs = buildEnvelopes.adjust();

      var files = constFiles.ToArray();
      var dirs = files.Select(f => f.destDir + "\\" + f.name.Replace('/', '\\')).ToArray();

      Parallel.ForEach(files.Where(f => f != null).Distinct(constsFileComparer), /*lib.parallelOptions,*/ file => {
        var data = file.srcData;
        var url = "/" + (file.destDir != null ? file.destDir.Replace('\\', '/') + "/" : null) + file.name;
        var fn = Machines.rootDir + url.Replace('/', '\\');
        LowUtils.AdjustFileDir(fn);
        //ev. zapis soubor na disk (napr. JS, MM soubory se nepisi) a aktualizuj obalku souboru s etagem do d:\LMCom\rew\Web4\Deploy\envelopes.xml
        envs.adjustEnvelope(buildId, url, data);
      });
      envs.save();
    }
    public static string getServerScript(string url, string content) {
      return "<script type=\"text/inpagefiles\" data-id=\"" + url + "\">" + content + "</script>";
    }
    public static string getBlendedScript(IEnumerable<Packager.Consts.file> files) {
      StringBuilder sb = new StringBuilder();
      foreach (var file in files.Where(f => f != null).Distinct(constsFileComparer)) {
        var data = file.srcData; if (data == null || !file.name.EndsWith(".js")) continue; //preskoc vse, mimo vytvorenych souboru (.js)
        var url = "/" + file.destDir.Replace('\\', '/') + "/" + file.name.Replace(".js", null);
        var js = Encoding.UTF8.GetString(file.srcData);
        sb.Append(url); sb.Append("|"); sb.Append(js); sb.Append("###");
      }
      return sb.ToString();
    }
    public static StringBuilder getServerScript(IEnumerable<Packager.Consts.file> files) {
      StringBuilder sb = new StringBuilder();
      foreach (var file in files.Where(f => f != null).Distinct(constsFileComparer)) {
        var data = file.srcData; if (data == null || !file.name.EndsWith(".js")) continue; //preskoc vse, mimo vytvorenych souboru (.js)
        var url = "/" + file.destDir.Replace('\\', '/') + "/" + file.name;
        var js = Encoding.UTF8.GetString(file.srcData);
        sb.Append("<script type=\"text/inpagefiles\" data-id=\""); sb.Append(url); sb.Append("\">");
        sb.Append(js);
        sb.AppendLine("</script>");
      }
      return sb;
    }
    public static string getResponseScript(IEnumerable<Packager.Consts.file> files) {
      StringBuilder sb = new StringBuilder();
      foreach (var file in files.Where(f => f != null).Distinct(constsFileComparer)) {
        var data = file.srcData; if (data == null || !file.name.EndsWith(".js")) continue; //preskoc vse, mimo vytvorenych souboru (.js)
        var url = "/" + file.destDir.Replace('\\', '/') + "/" + file.name;
        var js = Encoding.UTF8.GetString(file.srcData);
        sb.Append(url); sb.Append("%#%#[[[]]]"); sb.Append(js); sb.Append("%#%#[[[]]]");
      }
      return sb.ToString();
    }
    public static void zipVirtualFiles(Stream str, IEnumerable<Packager.Consts.file> files, LoggerMemory logger, Func<Packager.Consts.file, bool> filter = null, bool isUpdate = false) {
      using (ZipArchive zip = new ZipArchive(str, isUpdate ? ZipArchiveMode.Update : ZipArchiveMode.Create)) {
        var allEntries = isUpdate ? new HashSet<string>(zip.Entries.Select(en => en.FullName)) : null;
        Parallel.ForEach(files.Where(f => f != null && (filter == null || filter(f))).Distinct(constsFileComparer), file => {
          var destFn = ((file.destDir == null ? null : file.destDir + "\\") + file.name).ToLowerInvariant();
          if (allEntries != null && allEntries.Contains(destFn)) return;
          if (file.srcData != null) addFileToZip(zip, file.srcData, destFn); else addFileToZip(zip, file.srcPath, destFn, logger);
        });
      }
    }
    public static void zipVirtualFiles(string zipFn, IEnumerable<Packager.Consts.file> files, LoggerMemory logger, Func<Packager.Consts.file, bool> filter = null, bool isUpdate = false) {
      using (Stream str = File.Open(zipFn, FileMode.OpenOrCreate, FileAccess.ReadWrite)) zipVirtualFiles(str, files, logger, filter, isUpdate);
    }
    public class ConstsFileComparer : IEqualityComparer<Packager.Consts.file> { public bool Equals(Packager.Consts.file x, Packager.Consts.file y) { return x.srcPath.Equals(y.srcPath); } public int GetHashCode(Packager.Consts.file obj) { return obj.srcPath.GetHashCode(); } }
    public static IEqualityComparer<Packager.Consts.file> constsFileComparer = new ConstsFileComparer();

    static void addFileToZip(ZipArchive zip, string srcFn, string destFn, LoggerMemory logger) {
      try {
        if (destFn.IndexOf(".js.js") > 0)
          throw new Exception();
        if (!gzipExt.Contains(Path.GetExtension(destFn)))
          lock (zip) LMZipArchive.addFileToZip(zip, srcFn, destFn);
        else
          addFileToZip(zip, File.ReadAllBytes(srcFn), destFn);
      } catch (Exception exp) {
        logger.ErrorLineFmt("?", "Cannot read file for zip: {0} ({1})", srcFn, LowUtils.ExceptionToString(exp));
      }
    }
    static void addFileToZip(ZipArchive zip, byte[] data, string destFn) {
      var ext = Path.GetExtension(destFn);
      DateTime dt = DateTime.UtcNow;
      if (ext != ".dll") {
        //CRC je UINT. shift right = deleno 4, coz je maximalne 0x3fffffff
        uint secs = (uint)ZipStream.Crc(data);
        secs = secs >> 2;
        dt = zipStartDate.AddSeconds(secs);
      }
      lock (zip) LMZipArchive.addFileToZip(zip, data, destFn, dt);
      if (gzipExt.Contains(ext)) {
        if (data.Length < 100) return;
        MemoryStream gzipMs = new MemoryStream();
        using (GZipStream gzip = new GZipStream(gzipMs, CompressionMode.Compress, true)) gzip.Write(data, 0, data.Length);
        gzipMs.Seek(0, SeekOrigin.Begin);
        lock (zip) LMZipArchive.addFileToZip(zip, gzipMs, destFn + ".gzip", dt);
      }
    }
    static DateTime zipStartDate = new DateTime(2014, 1, 12).AddSeconds(-(0xffffffff >> 2));
    static HashSet<string> gzipExt = new HashSet<string>() { ".txt", ".lst", ".json", ".rjson", ".js", ".css", ".html", ".otf", ".svg", ".woff", ".ttf", ".eot" };
  }

  public class buildProduct {

    public product prod;
    public buildModule instrs; //modul s instrukcemi, ktery se nebuilduje standardnim zpusobem ale dava se primo do HTML stranky
    public Langs[] natLangs;
    public dictTypes dictType;

    public IEnumerable<Packager.Consts.file> getFiles(Cache cache, LoggerMemory logger, sitemap sm = null) {
      if (sm == null) sm = Lib.publishers;

      //adjust product object
      var prod = (product)this.prod.clone(); prod.finishRead();
      //moduly
      var mods = cache.adjustCacheForModules(prod, natLangs, dictType, logger, sm);
      instrs = mods.FirstOrDefault(m => m.myData.isType(runtimeType.instrs));
      foreach (var f in mods.Where(m => m != instrs).SelectMany(mod => mod.getFiles(cache, natLangs))) yield return f;

      var prodFileBase = data.urlStripLast(prod.url);
      //sitemap localization and instrs localization
      foreach (var natLang in natLangs) {
        //lokalizace instrukci
        buildModuleLoc instrLocMod = instrs == null ? null : instrs.locs.FirstOrDefault(l => l.natLang == natLang);
        var instrLoc = instrLocMod == null ? Enumerable.Empty<buildModuleLoc.KeysValue>() : instrLocMod.locKeyValue();
        //lokalizace sitemap
        Dictionary<string, string> loc = sitemapToTransSentences(cache, prod, natLang);
        var siteLoc = loc.Select(kv => new buildModuleLoc.KeysValue { keys = kv.Key.Split('/'), value = kv.Value, idx = 0 });
        //vytvor lokalizacni JS soubor
        var locStr = buildModuleLoc.locToTree(siteLoc.Concat(instrLoc).ToArray());
        if (locStr == null) continue;
        yield return new Packager.Consts.file(prodFileBase + "." + natLang.ToString() + ".js", Encoding.UTF8.GetBytes(locStr));
      }
      //sitemap
      prod.adjustMaxScore(); //spocti resultMs (charMax score). musi byt az po adjustCacheForModules, kde se nacitaji cviceni a pocita resultMs
      if (logger.isVsNet && logger.vsNetGlobalPublisherDir != null) prod.modifyUrls(prod.url.Substring(0, LowUtils.nthIndexesOf(prod.url, '/', 2) + 1), logger.vsNetGlobalPublisherDir);
      string prodTree = LowUtils.serializeObjectToJS(prod);
      yield return new Packager.Consts.file(prodFileBase + ".js", Encoding.UTF8.GetBytes(buildLib.rjsonSign + ClearScript.Lib.JSON2RJSON(prodTree)));
      prod.Items = null; prod.styleSheet = null; prod.ms = 0; prod.defaultDictType = dictTypes.unknown; prod.defaultLocs = null;
      yield return new Packager.Consts.file(prodFileBase + ".xml", Encoding.UTF8.GetBytes(data.writeObject(prod)));
      //instrs
      if (instrs != null) {
        StringBuilder sb = new StringBuilder();
        sb.Append('{'); bool first = true;
        foreach (var pg in instrs.pages) {
          if (first) first = false; else sb.Append(',');
          sb.Append('"'); sb.Append(pg.page.url); sb.Append("\":");
          var json = Encoding.UTF8.GetString(pg.json);
          sb.Append(json);
        }
        sb.Append('}');
        yield return new Packager.Consts.file(prodFileBase + "_instrs.js", Encoding.UTF8.GetBytes(sb.ToString()));
      }
    }
    //static runtimeType wrongTestTypes = runtimeType.taskTestSkill | runtimeType.taskTestInCourse | runtimeType.testGlobalAdmin;

    Dictionary<string, string> sitemapToTransSentences(Cache cache, data root, Langs natLang) {
      var toLoc = root.scan().Where(d => d.title != null && d.title.StartsWith("{{")).ToArray();
      cache.locStringsFromTrados(toLoc);
      Dictionary<string, string> res = new Dictionary<string, string>();
      string trans, key;

      var lookup = toLoc.SelectMany(d => locLib.getLocId2EnglishLoc(d.title, d.url, null).Select(nv => new {
        Name = (key = d.url + "/" + nv.Name),
        Value = cache.tradosCache[natLang].TryGetValue(key, out trans) ? trans : null
      })).
      Where(nv => nv.Value != null).
      ToLookup(nv => nv.Name);

      var nameValues = lookup.Select(l => new { l.Key, Values = l.Select(kv => kv.Value).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToArray() }).Where(kv => kv.Values.Length > 0).ToArray();
      var err = nameValues.Where(kv => kv.Values.Length > 1).ToArray();
      if (err.Length > 0) throw new Exception("duplicities");

      return nameValues.ToDictionary(nv => nv.Key, nv => nv.Values.Single());
    }

  }

  //globalni data modulu pro produkt
  public class buildModule {
    public string url;
    public CachePage[] pages;
    public List<buildModuleLoc> locs;
    public data myData;
    public IEnumerable<Packager.Consts.file> getFiles(Cache cache, Langs[] natLangs) {
      if (locs != null) foreach (var f in locs.Where(l => natLangs.Contains(l.natLang)).SelectMany(l => l.getFiles(cache))) yield return f;
      foreach (var f in pages.SelectMany(pg => pg.getFiles())) yield return f;
    }
  }

  public class buildModuleLoc {
    public buildModule owner;
    public Langs natLang;
    public Dictionary<string, string> locStrings;
    public CacheDict dict;
    public IEnumerable<Packager.Consts.file> getFiles(Cache cache) {
      if (dict != null && dict.externals != null) foreach (var url in dict.externals) yield return new Packager.Consts.file(url);
      var jsonBytes = locJson(); if (jsonBytes == null) yield break;
      yield return new Packager.Consts.file(data.urlStripLast(owner.url) + "." + natLang.ToString() + ".js", () => jsonBytes);
    }
    public IEnumerable<KeysValue> locKeyValue() { return locStrings.Select(kv => new KeysValue { keys = kv.Key.Split('/'), value = kv.Value, idx = 0 }); }
    byte[] locJson() {
      var locStr = locToTree(locKeyValue().ToArray());
      if ((dict == null || dict.dict == null) && locStr == null) return null;
      var dictStr = dict == null || dict.dict == null ? null : LowUtils.serializeObjectToJS(dict.dict);
      var res = "{\r\n\"loc\" : " + (locStr ?? "{}") + ",\r\n\"dict\" : " + (dictStr == null ? "null" : ClearScript.Lib.JSON2RJSON(dictStr)) + "\r\n}";
      return Encoding.UTF8.GetBytes(res);
    }
    public static string locToTree(KeysValue[] keyVals) {
      if (keyVals == null || keyVals.Length == 0) return null;
      var sb = new StringBuilder();
      using (var wr = new JsonTextWriter(new StringWriter(sb))) {
        wr.Formatting = Formatting.None;
        Action<IEnumerable<KeysValue>> writeSubTree = null;
        writeSubTree = kvs => {
          wr.WriteStartObject();
          foreach (var g in kvs.GroupBy(kv => kv.keys[kv.idx])) {
            wr.WritePropertyName(g.Key);
            var f = g.First();
            if (f.idx == f.keys.Length - 1) wr.WriteValue(f.value);
            else writeSubTree(g.Select(kv => new KeysValue { idx = kv.idx + 1, keys = kv.keys, value = kv.value }));
          }
          wr.WriteEndObject();
        };
        writeSubTree(keyVals);
        return sb.ToString();
      }
    }
    public struct KeysValue { public string[] keys; public string value; public int idx; }
  }

  public class CacheDict {
    public schools.Dict dict;
    public HashSet<string> externals = new HashSet<string>(); //seznam pouzitych zvuku
    public HashSet<string> notFound = new HashSet<string>(); //seznam ohybu, ke kterym se nenaslo entry
  }
  public class external {
    public string url;
    public byte[] json;
  }
  public class CachePage {

    public CachePage(Langs crsLang, CourseModel.body page, LoggerMemory logger) {
      this.page = page;
      pageJsons(page, out json);

      //ostatni externals
      //if (page.url == "/skrivanek/english/a1/listening/listeningtruefalse_multitask/v1_1") {
      //  var ctrls = page.scan().ToArray();
      //  if (ctrls == null) return;
      //}
      foreach (var extFn in page.scan().SelectMany(t => t.getExternals(page)).Distinct()) {
        if (extFn.IndexOf("://") > 0) continue;
        externals.Add(new external { url = VirtualPathUtility.Combine(page.url, extFn.ToLower()) });
      }

      //words a trans
      //LOCDEBUG
      if (!logger.isVsNet) {
        words = page.dictSentences().SelectMany(s => DictLib.wordsForDesignTime(s, crsLang)).Distinct().ToArray();
        transSentences = page.toTransSentences(logger).ToArray();
      }

      page.Items = null;
    }

    public static void pageJsons(CourseModel.body page, out byte[] json) {
      string pgStr = CourseModel.tag.pageToJsons(page);
      json = Encoding.UTF8.GetBytes(pgStr);
    }

    public CourseModel.body page;
    public byte[] json;
    public List<external> externals = new List<external>();
    public string[] words;
    public NameValueString[] transSentences;

    public IEnumerable<Packager.Consts.file> getFiles() {
      yield return new Packager.Consts.file(page.url + ".js", json);
      if (externals != null)
        foreach (var ext in externals) {
          if (ext.json != null)
            yield return new Packager.Consts.file(ext.url + ".js", ext.json);
          else
            yield return new Packager.Consts.file(ext.url);
        }
    }
  }

  //cachovana lokalizace modulu
  public class CacheLoc {

    public CacheLoc(Cache parent, data productModule, Langs natLang) {
      this.parent = parent; this.productModule = productModule; this.natLang = natLang;
      crsLang = CommonLib.LineIdToLang(productModule.getLine());
      if (crsLang == natLang) { locStrings = new Dictionary<string, string>(); return; }
      var allSents = productModule.scan().OfType<ex>().Select(e => parent.allPages[e.url]).Where(pg => pg.transSentences != null).SelectMany(pg => pg.transSentences);
      var natTrans = parent.tradosCache[natLang];
      string val;
      locStrings = allSents.Select(s => new { s.Name, Value = natTrans.TryGetValue(s.Name, out val) ? val : null }).Where(nv => nv.Value != null).ToDictionary(s => s.Name, s => s.Value);
    }

    public Dictionary<dictTypes, CacheDict> dicts = new Dictionary<dictTypes, CacheDict>();
    public Dictionary<string, string> locStrings;

    public Cache parent;
    public data productModule;
    public Langs crsLang;
    public Langs natLang;

    public CacheDict adjustDict(data module, Langs natLang, dictTypes dictType) {
      if (dictType == dictTypes.no || natLang == crsLang) return null;
      CacheDict res;
      if (dicts.TryGetValue(dictType, out res)) return res;
      res = new CacheDict();
      var pages = module.scan().OfType<ex>().Select(e => parent.allPages[e.url]);
      DictLib.dictOptions[dictType].createDict(crsLang, natLang, pages.SelectMany(p => p.words), res);
      dicts.Add(dictType, res);
      return res;
    }
  }


  public class Cache : IDisposable {

    public Cache(LoggerMemory logger, Langs[] bigLocs = null) {
      //this.isDebug = isDebug; 
      this.logger = logger;
      //parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 1 };
      //this.bigLocs = bigLocs ?? debugBigLocs;
      parallelOptions = new ParallelOptions();
      this.bigLocs = bigLocs ?? CommonLib.bigLocalizations;
    }
    public static Langs[] debugBigLocs = new Langs[] { Langs.en_gb, Langs.cs_cz };
    public void trace(string msg) { }

    //cache nactenych (primarnich) stranek
    public Dictionary<string, CachePage> allPages = new Dictionary<string, CachePage>();
    //public CachePage getFromAllPage(string email) {
    //  CachePage res2;
    //  if (!allPages.TryGetValue(email, out res2)) return null;
    //  return res2;
    //}
    //cache slovnikuu a lokalizace k modulu
    public Dictionary<moduleKey, CacheLoc> allLocs = new Dictionary<moduleKey, CacheLoc>(modComparer);
    //cache lokalizovanych retezcu, nactenych z Tradosu
    public Dictionary<Langs, Dictionary<string, string>> tradosCache = CommonLib.bigLocalizations.ToDictionary(l => l, l => new Dictionary<string, string>()); //natLang, url => trans
    List<string> readedTradosPages = new List<string>(); //hlida ktere trados pages jsou jiz nacteny do tradosCache
    //objekty pro lock(odentifikovane stringem
    public Dictionary<string, object> locks = new Dictionary<string, object>();

    public ParallelOptions parallelOptions = new ParallelOptions();
    //public bool isDebug;
    public Langs[] bigLocs;
    public LoggerMemory logger;

    public void locStringsFromTrados(IEnumerable<data> datas) {
      //nacteni novych prekladu
      lock (tradosCache) {
        var newPages = datas.Select(ex => ex.getTradosPage()).Distinct().Except(readedTradosPages).ToArray();
        if (newPages.Length == 0) return;
        TradosLib.oper5TradosPage(newPages, tradosCache);
        readedTradosPages.AddRange(newPages);
      }
    }

    //cachuji se vsechny mozne lokalizace, vraci se jen aktualne pozadovane. 
    //vsechny mozne lokalizace musi byt podmnozinou CommonLib.bigLocalizations
    public List<buildModule> adjustCacheForModules(data prod, Langs[] prodNatLangs, dictTypes dictType, LoggerMemory logger, sitemap siteMap = null) {

      //zajisti nacteni vsech stranek (Course.Page) produktu, do pages
      Parallel.ForEach(prod.scan().OfType<ex>(), parallelOptions, e => {
        object lockObj; lock (locks) if (!locks.TryGetValue(e.url, out lockObj)) locks.Add(e.url, lockObj = new object());
        lock (lockObj) {
          if (allPages.ContainsKey(e.url)) return;
          var pg = e.readPage(logger); if (pg == null) return;
          var cachePg = new CachePage(CommonLib.LineIdToLang(e.getLine()), pg, logger);
          lock (allPages) allPages.Add(e.url, cachePg);
        }
      });

      //zajisteni nacteni vsech prekladu z Tradosu
      //LOCDEBUG
      if (!logger.isVsNet)
        locStringsFromTrados(prod.scan().OfType<ex>());

      //priprav moduly produku (obsahuji url, nactene stranky a lokalizace)
      List<buildModule> resList = new List<buildModule>();
      Parallel.ForEach(prod.scan().Where(d => d.isType(runtimeType.mod)),
        //new ParallelOptions { MaxDegreeOfParallelism = 1 },
        parallelOptions,
        productModule => {
          object lockObj; lock (locks) if (!locks.TryGetValue(productModule.url, out lockObj)) locks.Add(productModule.url, lockObj = new object());
          lock (lockObj) {
            bool noDict = (productModule.type & runtimeType.noDict) != 0;
            var exs = productModule.scan().OfType<ex>().ToArray();
            buildModule resMod = new buildModule {
              url = productModule.url,
              pages = productModule.scan().OfType<ex>().Select(e => allPages[e.url]).ToArray(),
              locs = new List<buildModuleLoc>(),
              myData = productModule,
            };
            lock (resList) resList.Add(resMod);
            var crsLang = CommonLib.LineIdToLang(productModule.getLine());
            Langs[] allLangs = Lib.getAllLocs(bigLocs, productModule, logger, siteMap).ToArray();
            Langs[] prodLangs = prodNatLangs ?? allLangs;
            if (prodLangs.Except(allLangs).Any()) { logger.ErrorLine(productModule.url, "Not available natLang"); return; }
            foreach (var natLang in allLangs) {
              CacheLoc modLoc; var key = new moduleKey { url = productModule.url, natLang = natLang };
              lock (allLocs)
                if (!allLocs.TryGetValue(key, out modLoc))
                  allLocs.Add(key, modLoc = new CacheLoc(this, productModule, natLang));
              var dict = noDict || dictType == dictTypes.no || logger.isVsNet ? null : modLoc.adjustDict(productModule, natLang, dictType);
              if (prodLangs.Contains(natLang)) {
                //logger.AppendLineFmt("Building {0} in {1}", resMod.url, natLang);
                var natTrans = tradosCache[natLang];
                buildModuleLoc res = new buildModuleLoc {
                  owner = resMod,
                  natLang = natLang,
                  locStrings = crsLang == natLang ? new Dictionary<string, string>() : modLoc.locStrings,
                  dict = dict
                };
                if (res.dict != null && (res.dict.dict == null || res.dict.dict.Entries == null)) res.dict = null;
                resMod.locs.Add(res);
              }
            }
          }
        });

      return resList;
    }

    public struct moduleKey : IEqualityComparer<moduleKey> {
      public string url;
      public Langs natLang;
      bool IEqualityComparer<moduleKey>.Equals(moduleKey x, moduleKey y) { return x.natLang == y.natLang && x.url == y.url; }
      int IEqualityComparer<moduleKey>.GetHashCode(moduleKey obj) { return LowUtils.computeHashCode(obj.url, obj.natLang); }
    }

    static IEqualityComparer<moduleKey> modComparer = new moduleKey();


    void IDisposable.Dispose() {
      if (logger != null) logger.Dispose();
    }
  }

  public static class Lib {

    //static lib() {
    //  sitemap.getSitemap = () => { init(new LoggerDummy()); return _publishers; };
    //}
    static sitemap _publishers;

    //Definice produktu v kodu
    public static sitemap publishers { get { init(new LoggerMemory(true)); return _publishers; } }
    public static products prods;
    public static products prodExpanded;
    //public static product getRuntimeProd(string url) {
    //  url = url.Split('|')[0];
    //  product prod;
    //  return runtimeProdExpanded().TryGetValue(url, out prod) ? prod : null;
    //}
    //public static Dictionary<string, product> runtimeProdExpanded() {
    //  return _runtimeProdExpanded ?? (_runtimeProdExpanded = XmlUtils.FileToObject<products>(prodsExpandedFn).Items.Cast<product>().ToDictionary(p => p.url, p => p));
    //}
    //static Dictionary<string, product> _runtimeProdExpanded;

    //nejmensi spolecna mnozina lokalizaci. Lokalizace je budto v parents allLocs nebo (kdyz je null) v bigLocs
    public static IEnumerable<Langs> getAllLocs(Langs[] bigLocs, data dt, LoggerMemory logger, sitemap sm = null) {
      var allLng = bigLocs.ToDictionary(l => l, l => 0);
      var nodes = dt.scan().OfType<ex>().Select(e => (sm ?? publishers).find(e.url, logger)).Where(nd => nd != null).ToArray();
      foreach (var e in nodes) {
        var d = (sm ?? publishers).find(e.url, logger); if (d == null) continue; //error je v logger
        var par = d.parents().FirstOrDefault(p => p.allLocs != null);
        var locs = par == null ? bigLocs : par.allLocs;
        foreach (var l in locs) allLng[l] = allLng[l] + 1;
      }
      return allLng.Where(kv => kv.Value == nodes.Length).Select(kv => kv.Key);
    }


    public static CourseIds[] allCourses = new CourseIds[] { CourseIds.English, CourseIds.EnglishE, CourseIds.German, CourseIds.Spanish, CourseIds.French, CourseIds.Italian, CourseIds.Russian };

    public static void init(LoggerMemory log, string _basicPath = null, bool reinit = false) {
      if (_basicPath != null) Machines._basicPath = _basicPath;
      init(null, null, reinit, log);
    }
    public static string siteFn { get { return Machines.dataPath + "productSiteMap.xml"; } }
    public static string prodsFn { get { return Machines.dataPath + "products.xml"; } }
    //public static string prodsExpandedFn { get { return Machines.dataPath + "productsExpanded.xml"; } }

    public static void refresh(LoggerMemory log) {
      initiated = false;
      init(log);
    }

    public static void init(string statisticDir, string rwDataSourcePath, bool reinit, LoggerMemory log) {
      if (statisticDir != null) Machines._statisticDir = statisticDir;
      if (rwDataSourcePath != null) Machines._rwDataSourcePath = rwDataSourcePath;
      if (!reinit && initiated) return;
      initiated = true;

      if (!reinit && File.Exists(siteFn)) {
        _publishers = XmlUtils.FileToObject<sitemap>(siteFn);
        _publishers.finish();
      } else {
        _publishers = createSitemap(reinit, log);
        _publishers.finishCreate();
        XmlUtils.ObjectToFile(siteFn, _publishers);
        _publishers.finish();
        //xref.lib.generateXref();
      }

      if (!reinit && File.Exists(prodsFn) && File.Exists(LibLow.prodsExpandedFn)) {
        prods = XmlUtils.FileToObject<products>(prodsFn); prods.finishRead();
        prodExpanded = XmlUtils.FileToObject<products>(LibLow.prodsExpandedFn); prodExpanded.finishRead();
      } else {
        try {
          prods = new products {
            //type = runtimeType.products,
            //items = new data[0]
            Items = Enumerable.Empty<data>().
              Concat(OldLangCourses.generateStandard()).
              Concat(LangCourses.generateExamplesProduct()).
              Concat(LangCourses.generateGrafiaProducts()).
              Concat(OldLangCourses.generateA1_C2()).
              Concat(LangCourses.generateTestMeProduct()).
              Concat(LangCourses.generateJJN()).
              Concat(LangCourses.generateSkrivanekProduct()).
              Concat(LangCourses.generateEdusoftProduct()).
              Concat(LangCourses.generateBlendedProduct()).
              ToArray()
          };
          //aktualizace local sitemaps
          byte[] jsonProdTree, rjsonProdTree; products ps;
          WebDataBatch.getJSSitemap(prods.Items.Cast<product>(), out jsonProdTree, out rjsonProdTree, out ps);
          var prodTreeStr = Encoding.UTF8.GetString(rjsonProdTree);
          var fn = Machines.rootPath + @"siteroot.all.js"; if (File.Exists(fn)) File.SetAttributes(fn, FileAttributes.Archive); File.WriteAllText(fn, prodTreeStr, Encoding.UTF8);
        } catch (Exception exp) {
          log.ErrorExp("CourseMeta.Lib.init, prods = new products", exp);
          return;
        }
        //pridej rucne nadefinvoane produkty z d:\LMCom\rew\Web4\prod\XmlSource\
        string[] addInProductUrls = new string[] {
          "/lm/blended/english/Dialogs.product",
          "/lm/blended/german/Dialogs.product",
          "/lm/blended/french/Dialogs.product",
          "/lm/blended/english/blended.product",
          "/lm/blended/german/blended.product",
          "/lm/blended/french/blended.product",
          "/lm/blcourse/langmastermanager.product",
          "/lm/blcourse/schoolmanager.product",
        };
        //var addInProds = Directory.EnumerateFiles(Machines.dataPath + @"xmlsource", "*.xml").Select(fn => data.readObject<data>(fn)).ToArray();
        var addInProds = addInProductUrls.Select(pr => Machines.rootDir + pr.Replace('/', '\\')).Select(fn => data.readObject<data>(fn)).ToArray();
        foreach (var addIn in addInProds) if (!addIn.url.EndsWith("/")) addIn.url += "/";
        prods.Items = prods.Items.Concat(addInProds).ToArray();
        //pridej instrukce
        //foreach (var prod in prods.Items) prod.Items = prod.Items.Concat(XExtension.Create<data>(
        //  new ptr(null, "/data/instr/std/") { takeChilds = childMode.selfChild }
        //  )).ToArray();
        XmlUtils.ObjectToFile(prodsFn, prods);
        File.WriteAllLines(Machines.dataPath + "productNames.txt", prods.Items.Select(p => p.url).OrderBy(p => p));
        //XmlUtils.ObjectToFile(@"d:\LMCom\rew\Downloads\Common\batches\webs\LM_Data_New.xml", new WebDataBatch {
        //  url = "/lm/lm_data_new/",
        //  dictType = dictTypes.L,
        //  products = prods.Items.Cast<product>().Select(p => new BatchProduct {
        //    email = p.url,
        //    dictType = p.defaultDictType,
        //    locs = p.defaultLocs
        //  }).ToArray()
        //});

        prodExpanded = (products)prodDef.expand(prods.clone(), _publishers, log);
        foreach (var prod in prodExpanded.Items) prodDef.addInstructions(prod, log);
        int uniqId = 0;
        HashSet<string> checkUnique = new HashSet<string>();
        foreach (var prod in prodExpanded.Items.Cast<product>()) {
          foreach (var dt in prod.scan()) {
            if (string.IsNullOrEmpty(dt.url)) dt.url = dt.parent.url + (uniqId++).ToString() + "/";
            var id = prod.url + "@" + dt.url;
            if (checkUnique.Contains(id)) throw new Exception("Duplicities " + id);
            checkUnique.Add(id);
          }
        };
        XmlUtils.ObjectToFile(LibLow.prodsExpandedFn, prodExpanded);
        prods.finishRead();
        prodExpanded.finishRead();
      }
    }
    static bool initiated = false;

    static sitemap createSitemap(bool reinit, LoggerMemory log) {
      //stare kurzy a gramatika
      project oldPrj; var oldProjFn = oldMetaFn; //Machines.rootPath + @"lm\oldea\meta.xml";
      if (!reinit && File.Exists(oldProjFn))
        oldPrj = XmlUtils.FileToObject<project>(oldProjFn);
      else {
        oldPrj = new project { title = "oldea", spaceId = "/lm/oldea/" };

        var xml = XElement.Load(Machines.rootPath + @"Schools\Design\Statistic_CourseStructureEx.xml");
        var grammXml = XElement.Load(Machines.rootPath + @"Schools\Design\Statistic_Grammar.xml");
        var allCoursesStr = allCourses.ToDictionary(c => c.ToString().ToLower(), c => true);

        var oldLevels = xml.Elements().Where(e => allCoursesStr.ContainsKey(e.AttributeValue("id").ToLower())).SelectMany(el => fromSitemapLib.course(oldPrj, el)).Cast<data>();
        int fakeId = 0;
        var oldGrammar = grammXml.Elements().Select(el => fromSitemapLib.grammar(oldPrj, el, 0, ref fakeId)).Cast<data>().ToArray();
        oldGramm.setBookName(oldGrammar); //spaceid hlavnich folderu (A1, A1Kub apod.)
        oldPrj.Items = oldLevels.Concat(oldGrammar).ToArray();
        XmlUtils.ObjectToFile(oldProjFn, oldPrj);
      }

      //zbyle XML z oldea


      var lmPubl = sitemap.readFromFilesystem(Machines.rootPath + "lm\\meta.xml");
      lmPubl.Items = new data[] {
        oldPrj,
        sitemap.fromFileSystem("/lm/russian4/", log),
        sitemap.fromFileSystem("/lm/examples/", log),
        sitemap.fromFileSystem("/lm/docExamples/", log),
        sitemap.fromFileSystem("/lm/pjExamples/", log),
        sitemap.fromFileSystem("/lm/etestme/", log),
        sitemap.fromFileSystem("/lm/author/", log),
        sitemap.fromFileSystem("/lm/blended/", log),
        sitemap.fromFileSystem("/lm/ea/", log),
        sitemap.fromFileSystem("/lm/blcourse/", log),
      };

      //nove kurzy a return
      //var newdProjects = publisherPaths.Select(ps => publisher.fromFilesystem(ps)).ToArray();
      //newdProjects[0].items = XExtension.Create(oldPrj).Concat(newdProjects[0].items).ToArray();
      //var cssFn = Machines.basicPath + @"rew\Web4\RowType\style.css";
      sitemap res = (sitemap)sitemap.readFromFilesystem(Machines.rootPath + @"meta.xml");
      res.Items = new data[] {
        lmPubl,
        sitemap.fromFileSystem("/grafia/", log),
        sitemap.fromFileSystem("/data/instr/", log),
        sitemap.fromFileSystem("/skrivanek/", log),
        sitemap.fromFileSystem("/edusoft/", log),
      };
      return res;
    }

    public class RJ {
      public string fileId;
      public string sentId;
      public string rj;
      public string sk;
      public string cz;
      public string en = "*** todo";
    }

    public static void prepareForRussianTrans() {
      var bp = @"d:\LMCom\rew\EduAuthorNew\";
      Dictionary<string, string> fileDict = new Dictionary<string, string>();
      foreach (var fn in Enumerable.Range(1, 3).Select(idx => bp + "russian" + idx.ToString()).SelectMany(d => new string[] { "*.lmdata" }.SelectMany(ext => Directory.GetFiles(d, ext, SearchOption.AllDirectories)))) {
        var txt = File.ReadAllText(fn, Encoding.UTF8); if (txt.IndexOf("layout=\"dictionary\"") < 0) continue;
        var xml = XElement.Parse(txt);
        var snd = xml.Descendants().Where(el => el.AttributeValue("layout") == "dictionary").Single();
        var srcFile = fn.Substring(bp.Length).ToLower().Replace(@".htm.aspx.lmdata", null);
        var dictFile = snd.Attribute("file").Value.ToLower().Substring(2).Replace("_cz.xml", null).Replace('/', '\\');
        fileDict.Add(srcFile, dictFile);
      }
      File.WriteAllLines(@"d:\LMCom\rew\Web4\App_Data\russianDict.txt", fileDict.Select(kv => kv.Key + "=" + kv.Value));
      //return;
      //foreach (var fn in Enumerable.Range(1, 3).Select(idx => @"d:\LMCom\rew\EduAuthorNew\russian" + idx.ToString()).SelectMany(d => new string[] { "*.lmap", "*.lmdata", "*.htm" }.SelectMany(ext => Directory.GetFiles(d, ext, SearchOption.AllDirectories)))) {
      Dictionary<string, RJ> dict = new Dictionary<string, RJ>(); int cnt = 0;
      foreach (var fn in Enumerable.Range(1, 3).Select(idx => @"d:\LMCom\rew\EduAuthorNew\russian" + idx.ToString()).SelectMany(d => new string[] { "*.lmap" }.SelectMany(ext => Directory.GetFiles(d, ext, SearchOption.AllDirectories)))) {
        var basicPath = @"d:\lmcom\rew\eduauthornew\";
        var destFn = fn.ToLower().Replace(basicPath, @"c:\temp\russian\lmaps\");
        var xml = XElement.Load(fn); var isLmap = Path.GetExtension(fn) == ".lmap";
        var fileId = fn.Substring(basicPath.Length).Replace("_cz.xml.lmap", null).Replace("_sk.xml.lmap", null);
        if (string.Compare(fn, destFn, true) == 0) throw new Exception();
        LowUtils.AdjustFileDir(destFn);
        if (isLmap) {
          bool isCz = fn.EndsWith("cz.xml.lmap"); bool isSk = fn.EndsWith("sk.xml.lmap");
          if (!isCz && !isSk) throw new Exception(); int wordCnt = 0;
          foreach (var cmd in xml.Element("Scripts").Elements().Select(s => s.Attribute("Command"))) {
            var m = parseCmd.Match(cmd.Value); Group grp = m.Groups["trans"];
            var rjTxt = m.Groups["rj"].Value; var trans = grp.Value;
            RJ rj;
            if (!dict.TryGetValue(fileId + "." + rjTxt, out rj)) dict.Add(fileId + "." + rjTxt, rj = new RJ { rj = rjTxt, fileId = fileId, sentId = "W" + (wordCnt++).ToString() });
            if (isCz) rj.cz = trans; else rj.sk = trans;
            var newVal = cmd.Value.Substring(0, grp.Index) + "[faker-beg]@" + rj.fileId + "." + rj.sentId + "|" + trans + "[faker-end]" + cmd.Value.Substring(grp.Index + grp.Length);
            newVal = null;
            //var val = cmd.Value; var bgIdx = val.IndexOf("{t=") + 3; var txt = val.Substring(bgIdx, val.Length - bgIdx - 1);
            //txt = "&#123;&#123;t" + (cnt++).ToString() + "|" + txt + "&#125;&#125;";
            //cmd.Value = val.Substring(0, bgIdx) + txt + "}";
          }
          xml.Save(destFn);
        } else {
          foreach (var trans in xml.Descendants(html + "trans").ToArray()) {
            trans.ReplaceWith("{{t" + (cnt++).ToString() + "|" + trans.InnerXml().Replace('\r', ' ').Replace('\n', ' ').Replace(" xmlns=\"htmlPassivePage\"", null) + "}}");
          }
          File.WriteAllText(destFn, xml.ToString().Replace("html:", null), Encoding.UTF8);
        }
      }
      XmlUtils.ObjectToFile(@"d:\temp\rjDict.xml", dict.Values.ToArray());
    }
    static XNamespace html = "htmlPassivePage";
    static Regex parseCmd = new Regex(@"\s*(?<rj>.*?){t=(?<trans>.*?)}");

    public static void dataFromEA(string url, LoggerMemory log) {
      CourseMeta.Lib.init(log);
      var node = CourseMeta.Lib.publishers.find(url, log);
      foreach (var ex in node.scan().OfType<CourseMeta.ex>())
        CourseMeta.Lib.dataFromEALow(ex, oldeaDataType.lmdata, log);
    }

    public static void dataFromEA(data root, oldeaDataType lmdataType, LoggerMemory logger) {
      Parallel.ForEach(root.scan().OfType<ex>().Where(e => e.isOldEa), /*lib.parallelOptions,*/ ex => dataFromEALow(ex, lmdataType, logger));
      //Sitemap do meta.xml
      if (root is sitemap) {
        XmlUtils.ObjectToFile(oldMetaFn, ((sitemap)root).find("/lm/oldea/"));
      }
    }

    public static void dataFromEALow(ex ex, oldeaDataType lmdataType, LoggerMemory logger) {
      string fn = ex.fileName();
      LowUtils.AdjustFileDir(fn);
      var res = dataFromEAStr(ex, lmdataType, logger);
      if (res == null) return;
      File.WriteAllText(fn, res, Encoding.UTF8);
      if (logger.strictChecking) CourseMeta.ex.readPage(ex.url, logger);
    }

    public static string dataFromEAStr(string exUrl, oldeaDataType lmdataType, LoggerMemory log, Action<XElement> modifier = null) {
      var ex = (ex)CourseMeta.Lib.publishers.find(exUrl, log);
      return dataFromEAStr(ex, lmdataType, log, modifier);
    }

    public static string dataFromEAStrUrl(string exUrl, oldeaDataType lmdataType, LoggerMemory log) {
      string relPath;
      return dataFromEAStrUrl((ex)CourseMeta.Lib.publishers.find(exUrl, log), lmdataType, out relPath);
    }

    public static string dataFromEAStrUrl(ex ex, oldeaDataType lmdataType, out string relPath) {
      CourseIds crsId = LowUtils.EnumParse<CourseIds>(removeNum.Replace(ex.pathParts[2], ""));
      string relUrl = ex.pathParts.Skip(2).Aggregate((r, i) => r + "/" + i);
      relPath = relUrl.Replace('/', '\\');
      return string.Format(oldEAUrl + "framework/deployment/EANew-DeployGenerator.aspx?ExerciseUrl={0}&CourseLang={1}&courseId={2}&oldeaDataType={3}",
        HttpUtility.UrlEncode(relUrl),
        CommonLib.CourseIdToLang(crsId),
        crsId,
        lmdataType.ToString());
    }

    public static string dataFromEAStr(ex ex, oldeaDataType lmdataType, LoggerMemory logger, Action<XElement> modifier = null) {
      string relPath;
      var url = dataFromEAStrUrl(ex, lmdataType, out relPath);
      Func<string, bool, string> normalizeHref = (val, isTrans) => {
        string oldVal = val;
        try {
          if (string.IsNullOrEmpty(val)) return val = "###";
          val = val.ToLower().Replace("~/", "/lm/oldea/").Split('#')[0].Replace(".htm", null);
          if ((ex.url.IndexOf("/german2/") > 0 && val.IndexOf("/german1/") > 0) || (ex.url.IndexOf("/german3/") > 0 && val.IndexOf("/german1/") > 0) || (ex.url.IndexOf("/german3/") > 0 && val.IndexOf("/german2/") > 0) || val == "http://www.tafel.de")
            return val = "???";
          if (val[0] == '/') return val;
          return val = VirtualPathUtility.Combine(ex.url, val);
        } finally {
          //logger.ErrorLineFmt(testEx.url, "{2}{0}=>{1} ({3})", testEx.url, val, isTrans ? "* " : null, oldVal);
          //if (hrefs != null) lock (hrefs) hrefs.Add(string.Format());
        }
      };

      string res;
      try {
        WebClient wc = new WebClient();
        res = Encoding.UTF8.GetString(wc.DownloadData(url));
        if (lmdataType == oldeaDataType.lmdataNew) return res;
      } catch (Exception exp) {
        var str = url + "\r\n" + LowUtils.ExceptionToString(exp);
        logger.ErrorLine(ex.url, str);
        return null;
      }

      try {
        //[faker-beg]t0|aha![faker-end], vznikle z d:\LMCom\rew\EduAuthorNew\russian1\lesson1\ChapterA\lekce01_1_cz.xml.lmap, nahrad {{t0|aha!}}
        res = fakeRussianDictTransReg.Replace(res, "{{${trans}}}");

        XElement div = tidyHtmlToXml(res);

        //remove &amp;#231; entities
        //foreach (var txt in div.DescendantNodes().OfType<XText>()) txt.Value = removeHTMLEntities(txt.Value);

        //Obohat src o lm/oldea/. src plneno v d:\LMCom\rew\EduAuthor\Localize.cs (toHtml, absSrc = absSrc.Substring(idx + 2))
        foreach (var src in div.Descendants("img").SelectMany(d => d.Attributes("src"))) src.Value = "/lm/oldea/" + src.Value.ToLower();
        //neprazdne href="x.htm" nahrad href="/<self url bez souboru>x"
        foreach (var src in div.Descendants("a").ToArray()) {
          var attr = src.Attribute("href"); if (attr == null) continue;
          string val = normalizeHref(attr.Value, false);
          switch (val) {
            case "???": src.ReplaceWith(src.Value); break;
            case "###": break;
            default: attr.Value = val; break;
          }
        }

        //extrahuj pageInfo, plnene v 
        //d:\LMCom\rew\EduAuthor\ClientScript.cs (ClientScriptGenerate, CourseModel.Page pg = new CourseModel.Page)
        //d:\LMCom\rew\EduAuthor\lib.cs, forEAUrl
        //d:\LMCom\rew\EduAuthor\SoundMarkersLib.cs, Markers.getObjId
        var pi = div.Descendants("script").Where(s => s.AttributeValue("id") == "pageInfo").First(); pi.Remove();

        //uprav HTML5 nekompatibility
        //makeHtml5(div); CourseModel.tag.oldEAImport_InsertFakeBlankChar(div);

        var page = XmlUtils.StringToObject<CourseModel.body>(pi.Value);
        page.title = ex.title;
        page.id = ex.pathParts.Last(); // pathPart(pathPartIdx.exercise);
        if (char.IsDigit(page.id[0])) page.id = "_" + page.id;
        page.order = ex.order;
        //page.url = testEx.url;
        //vytahni zvukovy soubor
        page.externals = div.DescendantNodes().OfType<XCData>().SelectMany(n => getSoundUrlReg.Matches(n.Value).Cast<Match>().Select(m => "/" + m.Groups["url"].Value)).DefaultIfEmpty().Aggregate((r, i) => r + '|' + i);
        //nova verze OldEA => vyhod JS script
        if (!page.isOldEa) div.Descendants("script").Remove();
        //HTML
        page.Items = new CourseModel.tag[] { CourseModel.tag.FromElement<CourseModel.htmlTag>(div, null, null, false) };
        if (page.externals != null && page.externals.Length == 0) page.externals = null;
        var resFn2 = relPath.Insert(relPath.LastIndexOf('\\'), "\\app_localresources") + ".htm.resx";
        var sents = TradosDT.TradosDB.getTradosContext(false).Sentences.Where(s => s.Page.FileName.EndsWith(resFn2) && s.SrcLang == (short)Langs.cs_cz && s.TransLang == (short)Langs.en_gb).ToArray();
        var sa = page.seeAlso;
        if (sa != null) {
          if (sa.Length == 0) page.seeAlsoStr = null;
          else {
            foreach (var so in sa) if (!string.IsNullOrEmpty(so.url) && so.url.Length > 0 && so.url[0] != '/') so.url = "/" + so.url;
            page.seeAlsoStr = CourseModel.seeAlsoLink.encode(sa);
          }
        }
        //rusky slovnik
        lock (typeof(Lib)) if (russianDict == null) russianDict = File.ReadLines(Machines.rootPath + @"App_Data\russianDict.txt").Select(l => l.Split('=')).ToDictionary(l => @"/lm/oldea/" + l[0].Replace('\\', '/'), l => l[1].Insert(l[1].LastIndexOf('\\'), "\\app_localresources"));
        string rd;
        if (russianDict.TryGetValue(ex.url, out rd))
          sents = sents.Concat(TradosDT.TradosDB.getTradosContext(false).Sentences.Where(s => s.Page.FileName.Contains(rd) && s.SrcLang == (short)Langs.cs_cz && s.TransLang == (short)Langs.en_gb)).ToArray();
        //do {{}} zavorek pridej anglicky zdroj, z Tradosu
        var loc2 = sents.Where(s => s.TransText != null).ToDictionary(s => s.Name, s => extractHref.Replace(s.TransText, m => { var g = m.Groups["href"]; return "href=\"" + normalizeHref(g.Value, true) + "\""; }));
        foreach (var tag in page.scan())
          tag.modifyTexts(text => {
            return CourseMeta.locLib.initOldEALocalizeText(text, loc2, key => { logger.ErrorLine(ex.url, key + ": " + text); });
          });

        var exXml = page.ToElement(); //kontrola
        makeHtml5(exXml); CourseModel.tag.oldEAImport_InsertFakeBlankChar(exXml);

        if (modifier != null) modifier(exXml);

        return CourseModel.tag.saveExXmlStr(exXml);

        //Create and save Page
        //return CourseModel.tag.saveExXml(pageXml, true);
      } catch (Exception exp) {
        logger.ErrorLine(ex.url, LowUtils.ExceptionToString(exp));
        return null;
        //if (errors != null) lock (errors) { errors.Add(url + "\r\n" + LowUtils.ExceptionToString(exp)); }
      }
    }

    private static string removeHTMLEntities(string str) {
      return ampEnt.Replace(str, m => {
        var num = int.Parse(m.get("num"));
        return Convert.ToChar(num).ToString();
      });
    }
    static Regex ampEnt = new Regex(@"&amp;#(?<num>\d+);");

    static string oldEAUrl = ConfigurationManager.AppSettings["OldEAUrl"];

    static void addClass(XElement el, string val) {
      var cls = el.AttributeValue("class") ?? "";
      var clss = cls.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
      if (!clss.Any(c => c == val))
        el.SetAttributeValue("class", clss.Concat(XExtension.Create(val)).Aggregate((r, i) => r + " " + i));
    }

    static void makeHtml5(XElement div) {
      foreach (var nobr in div.Descendants("nobr")) {
        nobr.Name = "span";
        addClass(nobr, "nobr");
      }
      foreach (var el in div.Descendants().Where(el => el.AttributeValue("align") == ""))
        el.SetAttributeValue("align", null);
      foreach (var el in div.Descendants().Where(el => el.AttributeValue("alt") == ""))
        el.SetAttributeValue("alt", null);
      foreach (var col in div.Descendants("col")) col.RemoveNodes();
      foreach (var el in div.Descendants("p")) { //.Where(el => el.Elements().Any(e => blocks.Contains(e.Name.LocalName)))) {
        el.Name = "div";
        addClass(el, "oli-par");
      }
      //foreach (var el in div.Descendants("script")) {
      //  el.SetAttributeValue("cdata", el.Value);
      //  el.RemoveNodes();
      //}
      foreach (var attr in div.Descendants().SelectMany(e => e.Attributes("s4n_hideid")).ToArray()) {
        addClass(attr.Parent, "s4n_hideid"); attr.Remove();
      }
      foreach (var s in div.Descendants("big")) { addClass(s, "repl-big"); s.Name = "span"; }

      for (int i = 0; i < 3; i++) //zanoreni <i><b>...
        foreach (var el in div.Descendants().Where(el => inlines.Contains(el.Name.LocalName) && el.Elements().Any(e => blocks.Contains(e.Name.LocalName)))) {
          addClass(el, "repl-" + el.Name.LocalName);
          el.Name = "div";
        }
      foreach (var el in div.Descendants().Where(el => heads.Contains(el.Name.LocalName) && el.Elements("div").Any())) {
        addClass(el, el.Name.LocalName);
        el.Name = "div";
      }
      div.Descendants("a").SelectMany(e => e.Attributes("target")).Where(t => t.Value == "_new").Remove();
      foreach (var hd in div.Descendants("thead").Where(h => h.Parent.Elements().Count() == 1)) hd.Name = "tbody";

      div.Descendants("img").SelectMany(img => img.Attributes("align")).Where(a => a.Value == "center").Remove();
      foreach (var align in div.Descendants("img").SelectMany(img => img.Attributes("align"))) {
        switch (align.Value) {
          case "middle": addClass(align.Parent, "repl-align-middle"); break;
          case "bottom": addClass(align.Parent, "repl-align-bottom"); break;
          case "left": addClass(align.Parent, "pull-left"); break;
          case "right": addClass(align.Parent, "pull-right"); break;
        }
        align.Remove();
      }

      foreach (var s in div.Descendants("strike")) s.Name = "s";
    }
    static HashSet<string> inlines = new HashSet<string>(new string[] { "span", "b", "i", "small" });
    static HashSet<string> blocks = new HashSet<string>(new string[] { "p", "div", "table", "h2" });
    static HashSet<string> heads = new HashSet<string>(new string[] { "h1", "h2", "h3", "h4" });

    static Dictionary<string, string> russianDict;

    static Regex removeNum = new Regex(@"\d");
    static Regex getSoundUrlReg = new Regex(@", 'url':'(?<url>lm/oldea/.*?\.mp3)',");
    static Regex fakeRussianDictTransReg = new Regex(@"\[faker-beg\](?<trans>.*?)\[faker-end\]");
    static Regex extractHref = new Regex(@"href=\""(?<href>.*?)\""");
    static string oldMetaFn { get { return Machines.rootPath + @"lm\oldea\meta.xml"; } }

    public static void oldEA_metaxml(data root = null) {
      if (root == null) root = XmlUtils.FileToObject<project>(oldMetaFn);
      if (root.Items == null) return;
      int cnt = 10;
      foreach (var it in root.scan().Where(t => t != root && !(t is ex) && !endWithNumbers.IsMatch(t.url) && t.url.IndexOf("/grammar") < 0 && t.url.IndexOf("/funlang") < 0)) {
        string fn = Machines.rootPath + it.url.Replace('/', '\\') + @"\meta.xml";
        data dt = new data { title = it.title, line = it.line, type = it.type, order = cnt += 10 };
        File.WriteAllText(fn,
          XmlUtils.ObjectToString(dt).
          Replace("xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" ", null).
          Replace("encoding=\"utf-16\"", null),
          Encoding.UTF8
        );
      };
    }
    static Regex endWithNumbers = new Regex(@".*/\d+$");

    public static void tidyHtmlFileToXml(string fileWithoutExt) {
      var cmd = string.Format(@"""c:\Program Files (x86)\tidy\tidy.exe"" -config ""c:\Program Files (x86)\tidy\tidy.cfg"" ""{0}.html"" > ""{0}.xml""", fileWithoutExt);
      Packager.RewApp.run(new string[] { cmd });
    }
    public static XElement tidyHtmlToXml(string htmlContent) {
      List<string> scripts = new List<string>();
      htmlContent = extractScript.Replace(htmlContent, m => {
        scripts.Add(m.Groups[1].Value);
        var i0 = m.Groups[0].Index; var i1 = m.Groups[1].Index; var l0 = m.Groups[0].Length; var l1 = m.Groups[1].Length;
        //var st = htmlContent.Substring(i0, i1 - i0);
        //var en = htmlContent.Substring(i1 + l1, i0 + l0 - i1 - l1).Replace("script", "scripttag");
        //return st + "<scriptid idx=\"" + (scripts.Count - 1).ToString() + "\"/>" + en;
        var st = htmlContent.Substring(i0, i1 - i0 - 1);
        return st.Replace("<script ", "<scripttag idx=\"" + (scripts.Count - 1).ToString() + "\" ") + " />";
      });
      //HtmlAgilityPack.HtmlNode.ElementsFlags.Remove("script");
      //HtmlAgilityPack.HtmlNode.ElementsFlags.Add("scriptid", HtmlAgilityPack.HtmlElementFlag.Closed);
      HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
      doc.LoadHtml(htmlContent);
      doc.OptionOutputAsXml = true;
      using (StringWriter swr = new StringWriter())
      using (xml.XmlTextWriter wr = new xml.XmlTextWriter(swr)) {
        doc.Save(wr);
        var res = swr.ToString();
        res = res.Replace("xmlns=\"http://www.w3.org/1999/xhtml\"", null);
        //remove &amp;#231; entities
        res = removeHTMLEntities(res);
        XElement root = XElement.Parse(res, LoadOptions.PreserveWhitespace);
        foreach (var sc in root.Descendants("scripttag").ToArray()) {
          sc.Name = "script";
          var attr = sc.Attribute("idx");
          sc.Add(new XCData(scripts[int.Parse(attr.Value)]));
          attr.Remove();
        }
        return root;
      }

      //File.WriteAllText(@"d:\temp\pom.html", htmlContent);
      //tidyHtmlFileToXml(@"d:\temp\pom");
      //string xml = File.ReadAllText(@"d:\temp\pom.xml");
      //return xml;
    }
    static Regex extractScript = new Regex(@"<script .*?>(.*?)</script>", RegexOptions.Singleline);

    //public static void productSitemapBin(string path) {
    //  using (var str = File.OpenWrite(path))
    //  using (BinaryWriter wr = new BinaryWriter(str))
    //    foreach (var tf in productSitemap()) tf.toStream(wr);
    //}

    //public static IEnumerable<StoredProcedures.TocFlat> productSitemap() {
    //  int cnt = 0;
    //  foreach (var prod in products) {
    //    int actProd = cnt++;
    //    foreach (var part in prod.productParts()) {
    //      int actLev = cnt++;
    //      foreach (var less in part.getLessonsEx()) {
    //        int actLess = cnt++;
    //        foreach (var mod in less.items.Cast<mod>()) {
    //          yield return new StoredProcedures.TocFlat() {
    //            ProdAndUrl = StoredProcedures.TocFlat.getProdAndUrl(prod.ProductId, mod.url), //LowUtils.JSONToId(mod.spaceId, mod.globalId)),
    //            ModTitle = mod.title,
    //            LessId = actLess,
    //            LessTitle = less.title,
    //            LevId = actLev,
    //            LevTitle = part.title,
    //            CrsId = actProd,
    //            CrsTitle = prod.title,
    //            ProductId = prod.ProductId,
    //          };
    //        }
    //      }
    //    }
    //  }
    //}

    ////public const int GrafiaCount = 900;

    //public static productDescrLow findProduct(string productId) {
    //  productId = productId.ToLower();
    //  //return products.First(p => string.Compare(p.productId(), productId, true) == 0);
    //  return products.First(p => p.productId() == productId);
    //}

    //public static Dictionary<string, productDescrNew[]> projects() {
    //  Dictionary<string, productDescrNew[]> res2 = new Dictionary<string, productDescrNew[]>();
    //  var large = allWordProducts().GroupBy(p => p.dataGroup).Where(g => g.Count() > 1);
    //  foreach (var g in large) res2.Add(g.First().dataGroup, g.ToArray());
    //  //var small = allWordProducts().GroupBy(p => p.dataGroup).Where(g => g.Count() == 1);
    //  //res2.Add("other", small.SelectMany(g => g).ToArray());
    //  return res2;
    //}

    //public static IEnumerable<productDescrNew> allWordProducts() {
    //  return products.OfType<productDescrNew>().Where(p => p.designFormat == DesignFormat.word);
    //}

    //public static mod findModuleSitemap(CourseMeta.productDescrNew prod, string moduleDir) {
    //  var parts = moduleDir.Split('\\');
    //  var lessMod = parts.Skip(parts.Length - 2).Aggregate((r, i) => r + "/" + i);
    //  var mod = prod._productParts.SelectMany(p => p._lessonsEx).SelectMany(l => l.items.Cast<mod>()).First(m => m.globalId == lessMod);
    //  return mod;

    //  //var rootRelative = moduleDir.Substring((Machines.rwDataSourcePath + @"rew\Web4\RwCourses\").Length);
    //  //var parts = rootRelative.Split('\\');
    //  //var lessonName = parts[parts.Length - 2];
    //  //var spaceId = parts.Take(parts.Length - 2).Aggregate((r, i) => r + "/" + i);
    //  //return createModuleSitemap(spaceId, lessonName, moduleDir);
    //}

    //public static XElement createModuleSitemap(string spaceId, string lessonName, string moduleDir) {
    //  string modName = Path.GetFileName(moduleDir);
    //  var Mod = safeLoad(moduleDir + @"\meta.xml");
    //  //vyhod ev. sitemap
    //  //mod.RemoveNodes();
    //  XElement mod = new XElement("module", new XAttribute("title", Mod.AttributeValue("title")));
    //  var globalId = lessonName + "/" + modName;
    //  mod.Add(new XAttribute("spaceId", spaceId), new XAttribute("globalId", globalId), new XAttribute("globalIdOrderNum", globalId));
    //  foreach (var pageFn in Directory.EnumerateFiles(moduleDir, "*.xml").Select(d => d.ToLowerInvariant()).Where(fn => fn.IndexOf("meta.xml") < 0)) {
    //    try {
    //      var pg = safeLoad(pageFn);
    //      var pgName = Path.GetFileNameWithoutExtension(pageFn);
    //      //var pgName = all.removeOrderNum(pgNameOrderNum);
    //      mod.Add(new XElement("page", new XAttribute("title", pg.Element("info").Attribute("title").Value), new XAttribute("url", "*" + pgName)/*, new XAttribute("urlOrderNum", pgNameOrderNum)*/));
    //    } catch { }
    //  }
    //  return mod;
    //}

    ////public static XElement createSitemap(string spaceId) {
    ////  spaceId = spaceId.ToLowerInvariant();
    ////  string courseDir = Machines.rootPath + spaceId.Replace('/', '\\');
    ////  XElement courseSiteMap = safeLoad(courseDir + @"\meta.xml");
    ////  courseSiteMap.Add(new XAttribute("spaceId", spaceId), new XAttribute("globalId", ""));
    ////  foreach (var lessonDir in Directory.EnumerateDirectories(courseDir).Select(d => d.ToLowerInvariant())) {
    ////    string lessonName = Path.GetFileName(lessonDir);
    ////    var les = safeLoad(lessonDir + @"\meta.xml");
    ////    les.Add(new XAttribute("spaceId", spaceId), new XAttribute("globalId", lessonName), new XAttribute("globalIdOrderNum", lessonName));
    ////    courseSiteMap.Add(les);
    ////    foreach (var modDir in Directory.EnumerateDirectories(lessonDir).Select(d => d.ToLowerInvariant()))
    ////      les.Add(createModuleSitemap(spaceId, lessonName, modDir));
    ////    isUniqueAndNotEmpty(les.Elements().Select(el => el.AttributeValue("globalId")), "Empty, missing or not unique module globalId attribute: " + lessonDir);
    ////  }
    ////  isUniqueAndNotEmpty(courseSiteMap.Elements().Select(el => el.AttributeValue("globalId")), "Empty, missing or not unique lesson globalId attribute: " + courseDir);
    ////  return courseSiteMap;
    ////}
    ////static string removeOrderNum(string globalId) {
    ////  return noOrderNum.Replace(globalId, "${name}");
    ////}
    ////static Regex noOrderNum = new Regex(@"\b\d*(?<name>\w+/?)\b");
    //public static void isUniqueAndNotEmpty(IEnumerable<string> data, string expString) {
    //  var ids = data.ToArray();
    //  if (ids.Any(email => string.IsNullOrEmpty(email)) || ids.Distinct().Count() != ids.Length) throw new Exception(expString);
    //}


    //static XElement safeLoad(string fn) {
    //  try { return XElement.Load(fn); } catch (Exception exp) { throw new Exception("\r\n\r\n**********\r\n\r\n********** Error in " + fn + ": " + exp.Message + "\r\n\r\n********** "); }
    //}
    ////static T safeLoadEx<T>(string fn) {
    ////  try { return XmlUtils.FileToObject<T>(fn); } catch (Exception exp) { throw new Exception("\r\n\r\n**********\r\n\r\n********** Error in " + fn + ": " + exp.Message + "\r\n\r\n********** "); }
    ////}

    ////vrati jsonId vsechn modulu produktu, napr. {english1_xl01_sa_shome_dhtm, ...}
    //public static IEnumerable<string> allModules(productDescrLow prod) {
    //  //return prod == null ? Enumerable.Empty<string>() : prod.productParts().SelectMany(pd => pd.getLessons().SelectMany(l => l.Elements().Select(m => LowUtils.JSONToId(m.AttributeValue("spaceId"), m.AttributeValue("globalId")))));
    //  return prod == null ? Enumerable.Empty<string>() : prod.productParts().SelectMany(pd => pd.getLessonsEx().SelectMany(l => l.items.Select(m => LowUtils.JSONToId(m.spaceId, m.globalId))));
    //}

  }

  //public class productPartEx : productPartLow {
  //  //public override IEnumerable<XElement> getLessons() {
  //  //  //return modifySiteMap != null ? modifySiteMap(getLessonsLow()) : getLessonsLow();
  //  //  return getLessonsLow();
  //  //}
  //  //public override IEnumerable<XElement> getLessons() {
  //  //  foreach (var lev in levs) {
  //  //    var levEl = prod.allLevels(lib.root)[lev.levIdx];
  //  //    var q = levEl.Elements().Skip(lev.skip);
  //  //    if (lev.take > 0) q = q.Take(lev.take);
  //  //    foreach (var les in q) yield return les;
  //  //  }
  //  //}
  //  public override IEnumerable<lesson> getLessonsEx() {
  //    foreach (var lev in levs) {
  //      var levEl = lib.oldCourse.items.Cast<multiTask>().First(c => c.spaceId == prod.course.ToString()).items[lev.levIdx];
  //      var q = levEl.items.Skip(lev.skip);
  //      if (lev.take > 0) q = q.Take(lev.take);
  //      foreach (var les in q.Cast<lesson>()) yield return les;
  //    }
  //  }

  //  public productLessInterval[] levs;
  //  //public Func<IEnumerable<XElement>, IEnumerable<XElement>> modifySiteMap;
  //}

  //public class productLessInterval {
  //  public int levIdx;
  //  public int skip;
  //  public int take;
  //}

  //public class productDescrEx : productDescrLow {
  //  public override IEnumerable<int> grammarLevels() { return _grammarLevels == null ? base.grammarLevels() : _grammarLevels; }
  //  public override string productId() { return _productId.ToLower(); }
  //  public override IEnumerable<productPartLow> productParts() { return _productParts; }
  //  public override IEnumerable<Langs> locs() { return _locs; }
  //  public int[] _grammarLevels;
  //  public string _productId;
  //  public productPartLow[] _productParts;
  //  public productPartLow[] setProductParts { set { _productParts = value; foreach (var p in value) p.prod = this; } }
  //  public Langs[] _locs;
  //}

  ////popis produktu = metakurzu
  //public class productDescr : productDescrLow {
  //  public int skipPart; //zacatek kurzu
  //  public int takePart; //pocet casti kurzu
  //  //public courseDescr[] items; //popis kurzu

  //  public override IEnumerable<int> grammarLevels() { //urovne kurzu
  //    switch (course) {
  //      case CourseIds.Russian: return Enumerable.Empty<int>(); //.Range(skipPart, takePart);
  //    }
  //    int start = skipPart / 2;
  //    int end = (skipPart + takePart - 1) / 2;
  //    return Enumerable.Range(start, end - start + 1);
  //  }
  //  public override string productId() {
  //    var res2 = course.ToString();
  //    if (isTest) res2 += "_test";
  //    res2 += "_" + skipPart.ToString();
  //    res2 += "_" + takePart.ToString();
  //    return res2;
  //  }

  //  public override IEnumerable<productPartLow> productParts() {
  //    for (int i = skipPart; i < skipPart + takePart; i++) {
  //      productPart res2 = new productPart() { level = allLevelsEx(lib.oldCourse)[i / 2], partIdx = i, prod = this };
  //      var sp = partSplitIdx(i); string tit;
  //      if (sp < 0) {
  //        if (course == CourseIds.Russian) res2 = new productPart() { level = allLevelsEx(lib.publishers)[i], partIdx = i, prod = this };
  //        res2.lessonsEx = res2.level.items.Cast<lesson>(); tit = null;
  //      } else if ((i & 1) == 0) {
  //        res2.lessonsEx = res2.level.items.Cast<lesson>().Take(sp); tit = Def.part1[course];
  //      } else {
  //        res2.lessonsEx = res2.level.items.Cast<lesson>().Skip(sp); tit = Def.part2[course];
  //      }
  //      res2.title = res2.level.title + " " + tit;
  //      yield return res2;
  //    }
  //  }
  //  //public override IEnumerable<productPartLow> productParts() {
  //  //  for (int i = skipPart; i < skipPart + takePart; i++) {
  //  //    productPart res2 = new productPart() { root = allLevels(lib.root)[i / 2], partIdx = i, prod = this };
  //  //    var sp = partSplitIdx(i); string tit;
  //  //    if (sp < 0) {
  //  //      if (course == CourseIds.Russian) res2 = new productPart() { root = allLevels(lib.root)[i], partIdx = i, prod = this };
  //  //      res2.lessons = res2.root.Elements(); tit = null;
  //  //    } else if ((i & 1) == 0) {
  //  //      res2.lessons = res2.root.Elements().Take(sp); tit = Def.part1[course];
  //  //    } else {
  //  //      res2.lessons = res2.root.Elements().Skip(sp); tit = Def.part2[course];
  //  //    }
  //  //    res2.title = res2.root.AttributeValue("title") + " " + tit;
  //  //    yield return res2;
  //  //  }
  //  //}

  //  int levelsNum() { //pocet vsech urovni kurzu
  //    return course == CourseIds.English || course == CourseIds.EnglishE ? 5 : (course == CourseIds.Russian ? 4 : 3);
  //  }
  //  public override bool hasPretest() { //vsechny casti => ma pretest
  //    //DEBUG: return levelsNum() * 2 == takePart;
  //    return false;
  //  }
  //  public override IEnumerable<Langs> locs() { //dostupne lokalizace
  //    switch (course) {
  //      //case CourseIds.EnglishE: yield return Langs.en_gb; break;
  //      //case CourseIds.English: foreach (var lng in CommonLib.bigLocalizations.Where(l => l != CommonLib.CourseIdToLang(course))) yield return lng; break;
  //      //case CourseIds.Russian: yield return Langs.cs_cz; yield return Langs.sk_sk; break; //RUSSIAN
  //      default: foreach (var lng in CommonLib.bigLocalizations.Where(l => l != CommonLib.CourseIdToLang(course) /*&& l != Langs.vi_vn && l != Langs.zh_cn*/)) yield return lng; break;
  //    }
  //  }
  //  public int partSplitIdx(int part) {
  //    switch (course) {
  //      case CourseIds.English:
  //      case CourseIds.EnglishE:
  //        switch ((int)(part / 2)) {
  //          case 0: return 8;
  //          case 1: return 8;
  //          case 2: return 8;
  //          case 3: return 8;
  //          case 4: return 9;
  //          default: throw new Exception();
  //        }
  //      case CourseIds.German:
  //        switch ((int)(part / 2)) {
  //          case 0: return 6;
  //          case 1: return 6;
  //          case 2: return -1;
  //          default: throw new Exception();
  //        }
  //      case CourseIds.Spanish:
  //        switch ((int)(part / 2)) {
  //          case 0: return 9;
  //          case 1: return 7;
  //          case 2: return 4;
  //          default: throw new Exception();
  //        }
  //      case CourseIds.French:
  //        switch ((int)(part / 2)) {
  //          case 0: return 7;
  //          case 1: return 6;
  //          case 2: return 4;
  //          default: throw new Exception();
  //        }
  //      case CourseIds.Italian:
  //        switch ((int)(part / 2)) {
  //          case 0: return 5;
  //          case 1: return 5;
  //          case 2: return 5;
  //          default: throw new Exception();
  //        }
  //      case CourseIds.Russian:
  //        return -1;
  //      default: throw new Exception();
  //    }
  //  }
  //}

  ////popis jednoho kurzu produktu
  //public class courseDescr {
  //  [DefaultValue(0)]
  //  public int email;
  //  //public string testFileName; //pro kurzy s testem: identifikace testu
  //  public string level; //textovy popis levelu, napr. A1-A2
  //}

  ////pomocne dato, identifikujici jednu cast produktu
  //public class productPart : productPartLow {
  //  //public IEnumerable<XElement> lessons; //zeznam lekci z root
  //  public IEnumerable<lesson> lessonsEx; //zeznam lekci z root
  //  public int partIdx; //index casti (pro anglictinu cislo 0..9)
  //  public XElement root; //napr. <level order="0" title="Beginner" spaceId="english1" globalId="home.htm"> node z Q:\LMCom\LMCOM\App_Data\Statistic_CourseStructureEx.xml
  //  public data level;

  //  //public override IEnumerable<XElement> getLessons() { return lessons; }
  //  public override IEnumerable<lesson> getLessonsEx() { return lessonsEx; }

  //  public override string testFileName() {
  //    if (!((productDescr)prod).isTest) return null;
  //    switch (prod.course) {
  //      case CourseIds.English: return string.Format("ElementTests/elements{0}", partIdx + 1);
  //      default: throw new Exception();
  //    }
  //  }

  //  public override string partLevelName() {
  //    switch (prod.course) {
  //      case CourseIds.EnglishE:
  //      case CourseIds.English:
  //        switch (partIdx) {
  //          case 0: return "A1";
  //          case 1: return "A1";
  //          case 2: return "A1-A2";
  //          case 3: return "A1-A2";
  //          case 4: return "A2";
  //          case 5: return "A2-B1";
  //          case 6: return "A2-B1";
  //          case 7: return "B1";
  //          case 8: return "B1-B2";
  //          case 9: return "B1-B2";
  //          default: throw new Exception();
  //        }
  //      case CourseIds.German:
  //        switch (partIdx) {
  //          case 0: return "A1";
  //          case 1: return "A1-A2";
  //          case 2: return "A2";
  //          case 3: return "A2-B1";
  //          case 4: return "B1-B2";
  //          default: throw new Exception();
  //        }
  //      case CourseIds.Spanish:
  //        switch (partIdx) {
  //          case 0: return "A1";
  //          case 1: return "A1-A2";
  //          case 2: return "A2";
  //          case 3: return "A2-B1";
  //          case 4: return "B1";
  //          case 5: return "B1-B2";
  //          default: throw new Exception();
  //        }
  //      case CourseIds.French:
  //        switch (partIdx) {
  //          case 0: return "A1";
  //          case 1: return "A1-A2";
  //          case 2: return "A2";
  //          case 3: return "A2-B1";
  //          case 4: return "B1";
  //          case 5: return "B1-B2";
  //          default: throw new Exception();
  //        }
  //      case CourseIds.Italian:
  //        switch (partIdx) {
  //          case 0: return "A1";
  //          case 1: return "A1-A2";
  //          case 2: return "A2";
  //          case 3: return "A2-B1";
  //          case 4: return "B1";
  //          case 5: return "B1-B2";
  //          default: throw new Exception();
  //        }
  //      case CourseIds.Russian:
  //        switch (partIdx) {
  //          case 0: return "A1";
  //          case 1: return "A2";
  //          case 2: return "A2-B1";
  //          case 3: return "B1-B2";
  //          default: throw new Exception();
  //        }
  //      default: throw new Exception();
  //    }
  //  }
  //}

}


//public class numPath {
//  public numPath(string pth) {
//    order = -1; path = null;
//    var parts = pth.Split('\\');
//    var nm = parts[parts.Length - 1];
//    parts[parts.Length - 1] = removeNum.Replace(nm, m => {
//      order = int.Parse(m.Groups["num"].Value);
//      return "";
//    });
//    path = parts.Aggregate((r, i) => r + "\\" + i);
//  }
//  public int order;
//  public string path;
//}
//static Regex removeNum = new Regex(@"(?<num>^\d+)");


//var metas = Directory.EnumerateFiles(@"d:\LMCom\rew\Web4\RwCourses", "meta.xml", SearchOption.AllDirectories).ToArray();
//var deepGroups = metas.GroupBy(mt => mt.Split('\\').Length).Select(g => new { deep = g.Key, metas = g.ToArray() }).OrderByDescending(dm => dm.deep).ToArray();
//foreach (var dp in deepGroups)
//  foreach (var meta in dp.metas) {
//    foreach (var testEx in Directory.EnumerateFiles(Path.GetDirectoryName(meta), "*.xml")) {
//      numPath pth = new numPath(testEx);
//      if (pth.order >= 0) {
//        var exXml = XElement.Load(testEx);
//        exXml.Add(new XAttribute("order", pth.order.ToString()));
//        exXml.Save(pth.path);
//        File.Delete(testEx);
//      }
//    }
//    numPath dir = new numPath(Path.GetDirectoryName(meta));
//    if (dir.order >= 0) {
//      var mXml = XElement.Load(meta);
//      mXml.Add(new XAttribute("order", dir.order.ToString()));
//      mXml.Save(meta);
//      Directory.Move(Path.GetDirectoryName(meta), dir.path);
//    }
//  }
//HttpContext.Current.Response.End();

//var bp = @"d:\temp\ea\";
//var sm = XmlUtils.FileToObject<data>(@"D:\LMCom\rew\Web4\Schools\Design\data.xml");
//Action<data, int> saveMeta = (d, tp) => {
//  var fn = bp + (string.IsNullOrEmpty(d.spaceId) ? null : d.spaceId + "\\") + (string.IsNullOrEmpty(d.globalId) ? null : d.globalId + "\\") + @"meta.xml"; LowUtils.AdjustFileDir(fn); d.items = null;
//  XElement root = XElement.Parse(XmlUtils.ObjectToString(d));
//  foreach (var attr in root.Attributes().Where(a => a.IsNamespaceDeclaration).ToArray()) attr.Remove();
//  switch (tp) {
//    case 0:
//      root.Add(new XAttribute("email", root.Element("email").Value), new XAttribute("basicDir", root.Element("basicDir").Value));
//      root.Attribute("spaceId").Value = root.Attribute("spaceId").Value.TrimEnd('/');
//      break;
//    case 1:
//      break;
//    case 2:
//      break;
//    case 3:
//      break;
//  }
//  root.RemoveNodes();
//  root.Save(fn);
//};
//foreach (var publ in sm.items.Skip(1)) {
//  foreach (var proj in publ.items) {
//    foreach (var crs in proj.items) {
//      foreach (var lev in crs.items) {
//        saveMeta(lev, 3);
//      }
//      saveMeta(crs, 2);
//    }
//    saveMeta(proj, 1);
//  }
//  saveMeta(publ, 0);
//}
//return;
