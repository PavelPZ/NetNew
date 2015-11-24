using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Resources;
using System.IO;
using System.Web;
using System.Web.Hosting;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Xml.Linq;
using System.Xml;
using System.Linq.Expressions;
using System.Data.Common;
using System.Threading;
using System.Web.UI.WebControls;
using System.ComponentModel;

/*
 ***** nbsp management *******
 * v .ASPX, .TRANS, .RESX je &nbsp;
 * v XML, online editoru, excelu a databázi je $nbsp;
 */

using LMNetLib;
using LMComLib;
using System.Configuration;

namespace Trados {

  public partial class Sentence {
    public void finish() {
      SrcHash = LowUtils.crc(SrcText);
      SrcLen = SrcText == null ? 0 : SrcText.Length;
      TransHash = LowUtils.crc(TransText);
      TransLen = TransText == null ? 0 : TransText.Length;
      string hashTxt = SrcText + TransText;
      Hash = LowUtils.crc(hashTxt);
    }
    public static Sentence insert(TradosDataContext db, Page page, string name, string srcText, Langs srcLang, string transText, Langs transLang) {
      var sent = insert(db, name, srcText, srcLang, transText, transLang); sent.Page = page; return sent;
    }
    public static Sentence insert(TradosDataContext db, int pageId, string name, string srcText, Langs srcLang, string transText, Langs transLang) {
      var sent = insert(db, name, srcText, srcLang, transText, transLang); sent.PageId = pageId; return sent;
    }
    static Sentence insert(TradosDataContext db, string name, string srcText, Langs srcLang, string transText, Langs transLang) {
      Trados.Sentence sent = new Trados.Sentence();
      db.Sentences.InsertOnSubmit(sent);
      sent.Name = name;
      sent.SrcLang = (short)srcLang;
      sent.SrcText = srcText;
      sent.TransLang = (short)transLang;
      sent.TransText = transText;
      //sent.TransText = sent.TransText.Replace("\r\n", crlfCode);
      sent.finish();
      return sent;
    }
  }

  public partial class Lookup {
    public static Lookup insert(TradosDataContext db, string srcText, Langs srcLang, string transText, Langs transLang) {
      Lookup res = new Lookup(); db.Lookups.InsertOnSubmit(res);
      res.finish(srcText, srcLang, transText, transLang);
      return res;
    }
    public void finish(string srcText, Langs srcLang, string transText, Langs transLang) {
      SrcText = srcText; SrcLang = (short)srcLang; TransText = transText; TransLang = (short)transLang;
      finish();
    }
    public void finish() {
      SrcLen = SrcText.Length; SrcHash = LowUtils.crc(SrcText);
      TransLen = TransText.Length; TransHash = LowUtils.crc(TransText);
      string hastTxt = SrcText + TransText;
      Hash = LowUtils.crc(hastTxt);
    }
    /// <summary>
    /// Aktualizuje lookup pro sentenci
    /// </summary>
    public void finish(Trados.Sentence srcSent) {
      finish(srcSent.SrcText, (Langs)srcSent.SrcLang, srcSent.TransText, (Langs)srcSent.TransLang);
    }

  }
}

namespace LMComLib {

  public static class TradosDOC {
    static void doc() {
      /*========== hlavni metody ============*/
      //Vytvoreni primarnich RESX souboru k ASPX, LMDATA, LMAP, JS, CS
      TradosLib.GenResx(null);
      //Import primarnich RESX souboru do Trados databaze
      TradosLib.oper1(LocPageGroup.other);
      //Priprava vet pro preklad
      TradosLib.tradosOper2_forLang(LocPageGroup.other, Langs.no, false);
      //Export pripravenych vet z Trados DB do Excelu
      TradosLib.ExportXml(LocPageGroup.other, Langs.no, null, null);
      //Upload Excelu do Tradso DB
      TradosLib.ImportXml(null, null, null);
      //Automaticky preklad OK vet
      TradosLib.AutoTranslate(LocPageGroup.other, Langs.no, null);
      //Generace jazykovych RESX souboru nebo lokalizovaneho .JS souboru
      TradosLib.GenerateResx(LocPageGroup.other, Langs.no);
      //Lokalizovany LMDATA soubor (merge puvodniho LMDATA s jazykovym RESX)
      TradosLib.LocalizeXml(null, Langs.no);
      //Prejmenovani *.ASPX na *.ASPX.TRANS resp *.ASPX.TRANSSRC
      TradosLib.RenameAspx(null, true);

    }
    /*========== Ostatni typy ==========*/
    //Trados konfiguracni soubor
    public static LocCfg cfg;
    //Identifikace BasicPath
    public static BasicPathType bpt;
    //Typy souboru k lokalizaci
    public static LocFileType lt;
    //Stav vety vzhledem k prekladu v Excelu 
    public static LocCommand lc;
  }

  /// <summary>
  /// Typy souboru dle nasledneho zpracovani
  /// </summary>
  public enum LocFileType {
    lmap,
    lmdata,
    aspx,
    cs,
    js,
    sitemap,
    //appdata,
    downloadXml,
    loc
  }

  /// <summary>
  /// Stav vety vzhledem k lokalizaci
  /// </summary>
  public enum LocCommand {
    TRANS, //prvni preklad (prelozeny zdroj a trans je pradny), neni exact match nabidka (z lookup table)
    OK, //prvni preklad, je exact match nabidka
    CHANGE, //lisi se aktualni a prelozeny zdroj, neni exact match nabidka
    CHOICE, //lisi se aktualni a prelozeny zdroj, je exact match nabidka 
    DONE, //veta prelozena: shoduje ze aktualni zdroj s prelozenym zdrojem
    NONE, //TRANS zdroje je prazdny, neni co prekladat
  }

  public class GenResxContext {
    public GenResxContext(LocPageGroup grp) {
      this.grp = grp;
    }
    public LocPageGroup grp;
    public List<TradosLib.resxNameValue> toTrans = new List<TradosLib.resxNameValue>();
    public Dictionary<string, bool> ids = new Dictionary<string, bool>();
    public List<TradosLib.resxNameValue> toTransJS = new List<TradosLib.resxNameValue>();
  }

  /// <summary>
  /// Prekladova knihovna
  /// </summary>
  public static class TradosLib {

    public static void recomputeSentHash(int skipInterv) {
      int cnt = Machines.getTradosContext(false).Sentences.Count();
      int intCnt = skipInterv;
      foreach (var interv in LowUtils.intervals(cnt, 2000).Skip(skipInterv)) {
        Trados.TradosDataContext db = Machines.getTradosContext(); db.CommandTimeout = 1000000;
        foreach (var sent in db.Sentences.Skip(interv.skip).Take(interv.take)) {
          sent.SrcHash = LowUtils.crc(sent.SrcText);
          sent.TransHash = LowUtils.crc(sent.TransText);
          string hashTxt = sent.SrcText + sent.TransText;
          sent.Hash = LowUtils.crc(hashTxt);
        }
        db.SubmitChanges();
        Console.WriteLine(intCnt.ToString());
        intCnt++;
      }
    }
    public static void recomputeLookupHash(int skipInterv) {
      int cnt = Machines.getTradosContext(false).Lookups.Count();
      int intCnt = skipInterv;
      foreach (var interv in LowUtils.intervals(cnt, 2000).Skip(skipInterv)) {
        Trados.TradosDataContext db = Machines.getTradosContext(); db.CommandTimeout = 1000000;
        foreach (var sent in db.Lookups.Skip(interv.skip).Take(interv.take)) {
          sent.SrcHash = LowUtils.crc(sent.SrcText);
          sent.TransHash = LowUtils.crc(sent.TransText);
          string hashTxt = sent.SrcText + sent.TransText;
          sent.Hash = LowUtils.crc(hashTxt);
        }
        db.SubmitChanges();
        Console.WriteLine(intCnt.ToString());
        intCnt++;
      }
    }


    public static string fnStartNoSlash = (Machines.basicPath + @"rew\EduAuthorNew").ToLower(); //@"q:\LMNet2\WebApps\EduAuthorNew\";
    public static string fnStart = fnStartNoSlash + "\\"; // @"q:\LMNet2\WebApps\EduAuthorNew\";


    //************* novy EA
    public class tradosPage {
      public string FileName; //Pages.FileName
      public Langs[] Langs;
      public Langs srcLang;
      public NameValueString[] sentences;
    }
    //public class tradosSents {
    //  public string Name; //Sentence.Name, cesta od "level" (bez) 
    //  public string TransText; //obsah
    //}

    //pageFilter a sentFilter slouzi pro hromadne delete.
    //sentFilter muze byt not null pouze tehdy, obsahuje-li pageFilter uplnou cestu k urovni (napr. lm/oldea/data/english1). Toto se ale v oper1New nekontroluje
    //pages jsou data pro Insert.
    public static void oper1NewTradosPagePart(string sentFilter, tradosPage page, bool isFakeRussian) {
      Trados.TradosDataContext db = Machines.getTradosContext(); db.CommandTimeout = 1000000;
      var singlePage = db.Pages.FirstOrDefault(p => p.FileName == page.FileName);
      var emptyData = page.sentences == null || page.sentences.Length == 0;
      if (singlePage == null && emptyData) return; //nothing todo
      //delete sentences na zaklade sentFilter
      if (singlePage != null) { //stranka existuje
        var delDb = Machines.getTradosContext();
        delDb.Sentences.DeleteAllOnSubmit(delDb.Sentences.Where(s => s.PageId == singlePage.Id && s.Name.StartsWith(sentFilter)));
        delDb.SubmitChanges();
      }
      //chybi data => delete page
      if (emptyData && singlePage != null) { db.Pages.DeleteOnSubmit(singlePage); return; }
      if (!emptyData && singlePage == null) db.Pages.InsertOnSubmit(singlePage = new Trados.Page { FileName = page.FileName, PageGroup = (short)(isFakeRussian ? LocPageGroup.fakeRussian : LocPageGroup.newEA), Langs = null /*TODO*/ });
      else {
        //kontrola zdrojoveho jazyka stranky - nesmi se zmenit
        var actSrcLang = (Langs)db.Sentences.Where(s => s.PageId == singlePage.Id).Select(s => s.TransLang).FirstOrDefault();
        if (actSrcLang != Langs.no && page.srcLang != actSrcLang) throw new Exception("single page TransLang mishmash");
      }
      //insert sentences pod singlePage stranku, sentences !=null && singlePage!=null
      foreach (var s in page.sentences)
        Trados.Sentence.insert(db, singlePage, s.Name, null, Langs.no, s.Value, page.srcLang);
      db.SubmitChanges();
    }

    public static void oper1NewTradosPages(tradosPage[] pages, bool isFakeRussian) {
      Trados.TradosDataContext db = Machines.getTradosContext(); db.CommandTimeout = 1000000;
      //insert pages
      foreach (var page in pages) {
        //kontrola jednoznacnosti sentence name
        var dupls = page.sentences.GroupBy(s => s.Name).Where(g => g.Select(s => s.Value).Distinct().Count() > 1).ToArray();
        if (dupls.Length > 0) throw new Exception(@"d:\LMCom\rew\LMComLib\Framework\TradosLib.cs, oper1NewTradosPages");
        var delDb = Machines.getTradosContext();
        delDb.Pages.DeleteAllOnSubmit(delDb.Pages.Where(p => p.FileName == page.FileName));
        delDb.SubmitChanges();

        var pg = new Trados.Page { FileName = page.FileName, PageGroup = (short)(isFakeRussian ? LocPageGroup.fakeRussian : LocPageGroup.newEA), SeeUrl = page.FileName };
        foreach (var s in page.sentences.GroupBy(s => s.Name).Select(g => g.First()))
          Trados.Sentence.insert(db, pg, s.Name, null, Langs.no, s.Value, page.srcLang);
      }
      db.SubmitChanges();
    }

    public static void oper5TradosPage(string[] fileNames, Dictionary<Langs, Dictionary<string, string>> tradosCache) {
      Trados.TradosDataContext db = Machines.getTradosContext(false); db.CommandTimeout = 1000000;
      foreach (var snt in db.Sentences.Where(s => fileNames.Contains(s.Page.FileName)).Select(s => new { s.Name, s.TransLang, s.TransText })) {
        var natLang = (Langs)snt.TransLang; if (natLang == Langs.es_es) natLang = Langs.sp_sp;
        if (!tradosCache.ContainsKey(natLang)) continue; //muze nastat pouze v debug mode, kdy je omezen sortiment jazyku
        tradosCache[natLang][snt.Name] = snt.TransText;
      }
    }

    /// <summary>
    /// Import RESX souboru do databaze
    /// </summary>
    public static IEnumerable<string> oper1(LocPageGroup grp) {
      LocCfgPageGroupFilter group = LocCfg.Instance().findPageGroup(grp);
      //RESX soubory 
      List<string> files = getFiles(new LocPageGroup[] { grp }).Where(s => hasResxFile(s)).Select(s => resxFileName(s)).ToList();
      if (File.Exists(group.GlobalResourcePath)) files.Add(group.GlobalResourcePath);
      if (File.Exists(group.GlobalResourcePathJS)) files.Add(group.GlobalResourcePathJS);
      //vsechny stranky grupy k vymazani
      List<int> deletePageIds = Machines.getTradosContext(false).Pages.Where(p => p.PageGroup == (short)grp).Select(p => p.Id).ToList();
      foreach (string fn in files.Where(f => File.Exists(f))) {
        Trados.TradosDataContext db = Machines.getTradosContext();
        db.CommandTimeout = 1000000;
        //Adjustace stranky
        Trados.Page page = db.Pages.Where(pg => pg.FileName == fn).SingleOrDefault();
        if (page == null) { //nova stranka
          page = new Trados.Page();
          db.Pages.InsertOnSubmit(page);
        } else { //stranka existuje: vymaz vety
          //odstran stranku ze stranek k vymazani
          deletePageIds.Remove(page.Id);
          //Vymazani vsech vet ke strance:
          Trados.TradosDataContext pomDb = Machines.getTradosContext();
          pomDb.CommandTimeout = 1000000;
          pomDb.ExecuteCommand("DELETE FROM Sentence WHERE pageid={0} and srclang={1}", page.Id, (short)Langs.no);
        }
        page.FileName = fn.ToLowerInvariant();
        page.PageGroup = (short)grp;
        page.SeeUrl = computeSeeUrl(group, page.FileName);
        page.Langs = group.fingLangFilter(fn);
        db.SubmitChanges();
        StringBuilder sb = new StringBuilder();
        //Vety z resource:
        using (ResXResourceReader rdr = new ResXResourceReader(fn))
          foreach (System.Collections.DictionaryEntry de in rdr) {
            Trados.Sentence.insert(db, page.Id, (string)de.Key, null, Langs.no, normalizeXmlText(((string)de.Value).Replace("&nbsp;", "$nbsp;"), sb), group.PrimaryLang);
          }
        db.SubmitChanges();
        //Odstraneni (**) zavorek ve zdrojovych resx
        string f = StringUtils.FileToString(fn);
        f = transFinal(f);
        StringUtils.StringToFile(f, fn);

        yield return fn;
      }
      //Vymaz stranky pro neexistujici soubory
      Trados.TradosDataContext delDb = Machines.getTradosContext();
      foreach (int id in deletePageIds) {
        delDb.ExecuteCommand("DELETE FROM Sentence WHERE pageid={0}", id);
        delDb.ExecuteCommand("DELETE FROM Pages WHERE id={0}", id);
      }
    }

    public static void oper2(LocPageGroup group, Langs transLang, bool adjustStrong) {
      Trados.TradosDataContext db = Machines.getTradosContext();
      if (db.Locks.Where(l => l.Locked && group == (LocPageGroup)l.PageGroup && l.Lang == (short)transLang).Any())
        throw new Exception("Jedna Skupin souborů a Jazyka ja zablokována (locked). Nejdříve odblokujte na unlock.aspx stránce.");
      TradosLib.tradosOper2_forLang(group, transLang, adjustStrong);
    }

    public static void oper3(LocPageGroup group, Langs transLang, bool doLock, List<LocCommand> commands, Stream str) {
      Trados.TradosDataContext db = Machines.getTradosContext();
      Trados.Lock lck = new Trados.Lock(); db.Locks.InsertOnSubmit(lck);
      lck.Created = DateTime.UtcNow;
      lck.Lang = (short)transLang;
      lck.PageGroup = (short)group;
      lck.Locked = doLock;
      db.SubmitChanges();
      TradosLib.ExportXml(group, transLang, str, commands);
    }

    public static void oper4(string fileContent, bool ignoreSentNotExist, Langs srcLang, Langs destLang, StringBuilder log) {
      Trados.Sentence sent = null;
      if (ignoreSentNotExist) {
        sent = new Trados.Sentence();
        sent.SrcLang = (short)srcLang;
        sent.TransLang = (short)destLang;
      }
      TradosLib.ImportXml(fileContent, sent, log);
    }

    public static void oper5(LocPageGroup group, Langs transLang) {
      TradosLib.GenerateResx(group, transLang);
    }

    public static void AddResourceKeyToSitemap(string siteFn, string urlFixPart) {
      XElement root = XElement.Load(siteFn); urlFixPart = urlFixPart.ToLowerInvariant();
      foreach (XElement node in root.Descendants(sitemap + "siteMapNode")) {
        string url = node.AttributeValue("url"); if (url == null) continue;
        url = url.ToLowerInvariant().Split('.')[0];
        if (!url.StartsWith(urlFixPart)) throw new Exception();
        url = url.Substring(urlFixPart.Length);
        if (url.IndexOf('_') >= 0) throw new Exception(url);
        url = url.Replace('/', '_');
        XAttribute attr = node.Attribute("resourceKey");
        if (attr == null) node.Add(attr = new XAttribute("resourceKey", url)); else attr.Value = url;
      }
      root.Save(siteFn);
    }

    public static void cancelOper2(LocPageGroup group, Langs transLang) {
      Trados.TradosDataContext db = Machines.getTradosContext();
      foreach (int pageId in db.Pages.Where(p => p.PageGroup == (short)group).Select(p => p.Id))
        db.Sentences.DeleteAllOnSubmit(db.Sentences.Where(s2 => s2.PageId == pageId && s2.TransLang == (short)transLang));
      db.SubmitChanges();
    }

    public const string crlfCode = "###CRLF###";
    public static XNamespace lm = "lm";
    public static XNamespace lmlib = "lmlib";
    public static XNamespace html = "htmlPassivePage";
    public static XNamespace excelHtml = "http://www.w3.org/TR/REC-html40";
    public static XNamespace asp = "asp";
    public static XNamespace site = "site";
    public static XNamespace meta = "meta";
    public static XNamespace xmlns = "http://www.w3.org/2000/xmlns/";
    public static XNamespace sitemap = "http://schemas.microsoft.com/AspNet/SiteMap-File-1.0";
    public static XNamespace empty = "";
    //public static XNamespace W3Chtml = "http://www.w3.org/1999/xhtml";
    public const string attrStart = "$trans;";

    public class Anot {
      public int Id = -1;
      public int Count;
      public bool isBasic;
      //public string Name;
      public static void GetAnotId(XElement el, out int id, out int count) {
        Anot res = GetAnot(el);
        if (res.Id == -1) {
          res.Id = 0;
          XElement root = el.AncestorsAndSelf().Last();
          Dictionary<string, int> dictCount = root.Annotation<Dictionary<string, int>>();
          if (dictCount == null) {
            dictCount = new Dictionary<string, int>(); root.AddAnnotation(dictCount);
          }
          string nm = el.Name.ToString();
          if (!dictCount.TryGetValue(nm, out res.Id)) dictCount.Add(nm, res.Id); else res.Id++;
          dictCount[nm] = res.Id;
        }
        id = res.Id; count = res.Count; res.Count++;
      }
      /*public static string GetAnotName(XNode el) {
        Anot res = el.Annotation<Anot>();
        return res == null || string.IsNullOrEmpty(res.Name) ? null : res.Name;
      }
      public static void SetAnotName(XNode el, string name) {
        GetAnot(el).Name = name;
      }*/
      public static Anot GetAnot(XNode el) {
        Anot res = el.Annotation<Anot>();
        if (res == null) el.AddAnnotation(res = new Anot());
        return res;
      }
      public static bool IsBasic(XNode el) {
        Anot res = el.Annotation<Anot>(); if (res == null) return false;
        return res.isBasic;
      }
    }

    /// <summary>
    /// Prevede LMDATA, LMAP nebo ASPX-like soubory do XML.
    /// </summary>
    /// <param name="virtualFn"></param>
    /// <returns></returns>
    public static XElement fileToXml(string virtualFn) {
      try { //XXX1
        //var cfg = ConfigLow.actConfig(false);
        //string txt = cfg != null && cfg.readNewFileIfExist != null ? cfg.readNewFileIfExist(virtualFn) : File.ReadAllText(virtualFn, Encoding.UTF8);
        string txt = LMComLib.TradosLib.readLMDataFile!=null ? LMComLib.TradosLib.readLMDataFile() : File.ReadAllText(virtualFn, Encoding.UTF8);
        XElement res;
        if (getFileType(virtualFn) == LocFileType.aspx) {
          res = AspxParser.ToXml(txt);
        } else
          res = XElement.Parse(txt, LoadOptions.PreserveWhitespace);
        foreach (XText nd in res.DescendantNodes().Where(n => n.NodeType == XmlNodeType.Text).Cast<XText>().Where(t => !string.IsNullOrEmpty(t.Value)))
          nd.Value = HtmlToXmlEntity.NormalizeEntities(nd.Value);
        return res;
      } catch (Exception exp) {
        throw new Exception("File " + virtualFn, exp);
      }
    }

    //static string bestNewFileName(string virtualFn) {
    //  var cfg = ConfigLow.actConfig();
    //  if (!cfg.readNewIfExist) return virtualFn;
    //  var newFnMask = Path.GetFileName(virtualFn).Replace(".htm.aspx.lmdata", ".*.xml");
    //  var fns = Directory.EnumerateFiles(Path.GetDirectoryName(virtualFn), newFnMask); //.Where(f => numXmlRx.IsMatch(f)).ToArray();
    //  var matches = fns.Select(f => numXmlRx.Match(f)).Where(m => m.Success).ToArray();
    //  if (matches.Length == 0) return virtualFn;
    //  var maxIdx = matches.Select(m => int.Parse(m.Groups[1].Value)).Max();
    //  cfg.newFileExisted = true;
    //  return virtualFn.Replace(".htm.aspx.lmdata", "." + maxIdx.ToString() + ".xml");
    //}
    //static Regex numXmlRx = new Regex(@"\.([1-9]\d*)\.xml$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

    /// <summary>
    /// Vrati typ zpracovavaneho souboru
    /// </summary>
    public static LocFileType getFileType(string fn) {
      if (fn.ToLowerInvariant().IndexOf("downloads.xml") >= 0) return LocFileType.downloadXml;
      switch (Path.GetExtension(fn).ToLowerInvariant()) {
        case ".lmdata":
        case ".htm":
          return LocFileType.lmdata;
        case ".lmap":
          return LocFileType.lmap;
        case ".aspx":
        case ".ascx":
        case ".html": //pro JsRender templates v Rew aplikaci
        case ".master":
          return LocFileType.aspx;
        case ".cs":
          return LocFileType.cs;
        case ".js":
        case ".ts":
          return LocFileType.js;
        case ".sitemap":
          return LocFileType.sitemap;
        //case ".appdata":
        //return LocFileType.appdata;
        default:
          if (fn.IndexOf("lmcomlibcs") > 0 || fn.IndexOf("ea_codecs") > 0 || fn.IndexOf("ecommercecs") > 0 || fn.IndexOf("registercs") > 0
            || fn.IndexOf("helpcs") > 0 || fn.IndexOf("ea_englishcs") > 0 || fn.IndexOf("ea_germancs") > 0 || fn.IndexOf("ea_spanishcs") > 0
            || fn.IndexOf("ea_italiancs") > 0 || fn.IndexOf("complmcomcs") > 0
            || fn.IndexOf("ea_frenchcs") > 0 || fn.IndexOf("ea_russiancs") > 0 || fn.IndexOf("facebookcs") > 0 || fn.IndexOf("moodlecs") > 0
            || fn.IndexOf("rew_schoolcs") > 0)
            return LocFileType.cs;
          else if (fn.IndexOf("ea_codejs") > 0 || fn.IndexOf("rew_schooljs") > 0 || fn.IndexOf("rew_rewisejs") > 0)
            return LocFileType.js;
          else throw new Exception("Missing code here! " + fn);
      }
    }

    /// <summary>
    /// Vygeneruje RESX z grupy souboru
    /// </summary>
    public static IEnumerable<string> GenResx(GenResxContext ctx) {
      //LocCfgPageGroupFilter group = LocCfg.Instance().findPageGroup(ctx.grp);
      //Stranky pro existujici RESX soubory 
      foreach (string fn in getFiles(new LocPageGroup[] { ctx.grp }))
        if (GenResx(fn, ctx) != null) yield return fn;
    }

    public static string resxFileName(string fn) {
      LocFileType ft = getFileType(fn);
      if (ft == LocFileType.sitemap /*|| ft == LocFileType.appdata*/) {
        fn = fn.ToLowerInvariant();
        string name = Path.GetFileName(fn).ToLowerInvariant() + ".resx";
        if (fn.IndexOf(@"\eduauthornew\") > 0)
          return (Machines.basicPath + @"rew\EduAuthorNew\app_globalresources\" + name).ToLowerInvariant();
        else
          throw new NotImplementedException();
        //else if (fn.IndexOf(@"\rw2\") > 0)
        //  return (Machines.basicPath + @"RW2\Server\App_GlobalResources\" + name).ToLowerInvariant();
        //else
        //  return (Machines.basicPath + @"LMCom\app_globalresources\" + name).ToLowerInvariant();
      } else {
        string dir = Path.GetDirectoryName(fn);
        string name = Path.GetFileName(fn).Replace(".aspx.lmdata", null);
        return (dir + @"\App_LocalResources\" + name + ".resx").ToLowerInvariant();
      }
    }

    static bool hasResxFile(string fn) {
      LocFileType ft = getFileType(fn);
      return ft == LocFileType.aspx || ft == LocFileType.lmap || ft == LocFileType.lmdata || ft == LocFileType.sitemap /*|| ft == LocFileType.appdata*/ || ft == LocFileType.downloadXml;
    }

    static string resxFileName(string fn, Langs lang) {
      return fn.Replace(".resx", "." + lang.ToString().Replace('_', '-') + ".resx");
    }

    /// <summary>
    /// Vygeneruje RESX z jednoho souboru
    /// </summary>
    public static XElement GenResx(string fn, GenResxContext ctx) {
      LocFileType fileType = getFileType(fn);
      if (fileType == LocFileType.aspx && (ctx.grp == LocPageGroup.rew_school || ctx.grp == LocPageGroup.rew_rewise)) fileType = LocFileType.js;
      XElement root = null;
      //Vytazeni konstant z CSharp kodu
      if (fileType == LocFileType.aspx || fileType == LocFileType.cs) {
        //if (fileType == LocFileType.cs && !File.Exists(fn))
        //fn = fn.Replace(".cs",null);
        string content = File.ReadAllText(fn);
        bool modified;
        content = CSParser.Parse(content, ctx, fileType, out modified);
        if (modified) File.WriteAllText(fn, content);
      }
      //Vytazeni konstant z JS kodu
      if (fileType == LocFileType.js) {
        string content = File.ReadAllText(fn);
        bool modified;
        content = JSParser.Parse(content, ctx, out modified);
        if (modified) File.WriteAllText(fn, content);
      }
      StringBuilder sb = new StringBuilder();
      //Pres XML
      if (fileType == LocFileType.aspx || fileType == LocFileType.lmap || fileType == LocFileType.lmdata || fileType == LocFileType.sitemap /*|| fileType == LocFileType.appdata*/ || fileType == LocFileType.downloadXml) {
        root = /*fileType == LocFileType.appdata ? null : */fileToXml(fn);
        /*if (ctx.grp == LocPageGroup.CPV) { //Pro CPV se jmeno prvku k prekladu sklada z parent chainu
          XElement body = root.Element(html + "body");
          foreach (XElement el in body.Descendants().Where(e => e.Attribute("force_trans") != null || (!e.HasElements && !e.Parents(false).Any(p => p.Attribute("force_trans") != null)))) {
            if (el.Value.StartsWith("@")) continue;
            sb.Length = 0; XElement e = el;
            while (e != body) { sb.Insert(0, "." + e.Name.LocalName); e = e.Parent; }
            sb.Remove(0, 1);
            Anot.SetAnotName(el, sb.ToString());
          }
        }*/
        //generace RESX
        string outFn = resxFileName(fn);
        resxNameValue[] items = resxItems(root, fileType).ToArray();
        if (items.Length == 0) {
          //vymazani RESX (i jazykovych)
          if (File.Exists(outFn)) File.Delete(outFn);
          foreach (Langs lng in Enum.GetValues(typeof(Langs))) {
            string transFn = resxFileName(outFn, lng);
            if (File.Exists(transFn)) File.Delete(transFn);
          }
          //return null;
        }
        if (items.Length > 0) {
          LowUtils.AdjustFileDir(outFn);
          using (ResXResourceWriter wr = new ResXResourceWriter(outFn))
            foreach (resxNameValue nv in items)
              wr.AddResource(nv.Name, nv.Value.Replace("$nbsp;", "&nbsp;"));
        }
        //Modifikace ASPX stranky
        if (fileType == LocFileType.aspx) {
          //New: title atribut v lmdata XML u LMDataControl kontrolky se lokalizuje pres CSLocalize
          foreach (XAttribute attr in root.Descendants().Where(el => el.Name.Namespace == lm).Select(el => el.Attribute("title")).Where(a => a != null && a.Parent.Attribute("id") != null))
            ctx.toTrans.Add(new TradosLib.resxNameValue(LMDataControlResId(fn, attr), attr.Value, (XAttribute)attr));
          foreach (resxNameValue nv in items)
            if (nv.Attr != null) AspxParser.modifyAspxPage(nv.Attr);
            else AspxParser.modifyAspxPage(nv.Nodes, nv.Name, nv.Value);
          //XCData dt = (XCData) root.DescendantNodes().Where(nd => nd.NodeType == XmlNodeType.CDATA && ((XCData)nd).Value.IndexOf("/*LMDataControl.Localize*/null") > 0).FirstOrDefault();  
          //if (dt!=null)
          root.Save(fn + ".xml", SaveOptions.DisableFormatting);
          string content = AspxParser.ToString(root);
          if (!File.Exists(fn + ".transsrc"))
            StringUtils.StringToFileUtf8Signature(content, fn + ".trans");
          //File.WriteAllText(fn + ".trans", content);
        }
      }
      return root;
    }
    const string fnEnd = ".hmt.aspx";
    public static string LMDataControlResId(string fn, XAttribute attr) {
      fn = fn.Substring(fnStart.Length, fn.Length - fnStart.Length - fnEnd.Length).Replace('\\', '_').ToLowerInvariant();
      return fn + "." + attr.Parent.Attribute("id").Value + "." + attr.Name.LocalName;
    }

    /// <summary>
    /// Pomocny objekt pro prvek RESX souboru
    /// </summary>
    public struct resxNameValue {
      public resxNameValue(string name, string value, XAttribute attr) {
        Name = name; Value = value;
        Attr = attr;
        Nodes = null;
      }
      public resxNameValue(string name, string value, List<XNode> nodes) {
        Name = name; Value = value;
        Nodes = nodes;
        Attr = null;
      }
      /// <summary>
      /// RESX Id
      /// </summary>
      public string Name;
      /// <summary>
      /// RESX value
      /// </summary>
      public string Value;
      /// <summary>
      /// Prvek vznikl z atributu
      /// </summary>
      public XAttribute Attr;
      /// <summary>
      /// Prvek vznikl ze zeznamu elementu
      /// </summary>
      public List<XNode> Nodes;
    }

    /// <summary>
    /// Podminka na XML usek, ktery se preklada
    /// </summary>
    /// <returns></returns>
    public static Func<XElement, bool> isTransElement() {
      return e => isTrans(e);
    }

    public static bool isTrans(XElement e) {
      if (e.Name == html + "trans") return true;
      XAttribute attr = e.Attribute("force_trans"); if (attr == null) return false;
      return (string)attr == "true";
    }

    public static Func<XElement, bool> isNoTransElement() {
      return e => isNoTrans(e);
    }

    static bool isNoTrans(XElement e) {
      if (e.Name.LocalName == "notrans") return true;
      XAttribute attr = e.Attribute("force_trans"); if (attr == null) return false;
      return (string)attr == "notrans";
    }

    /// <summary>
    /// normalizuje mezery v textu
    /// </summary>
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
      //sb.Replace("&", "&amp;"); sb.Replace("<", "&lt;"); sb.Replace(">", "&gt;");
      return HtmlToXmlEntity.DeEntitize(sb.ToString());
    }

    /// <summary>
    /// Vrat prvky RESX souboru k prekladu
    /// </summary>
    public static IEnumerable<resxNameValue> resxItems(XElement root, LocFileType fileType) {
      StringBuilder sb = new StringBuilder();
      //LMAP:
      if (fileType == LocFileType.lmap) {
        int cnt = 0;
        //Preklad LMAP souboru (<Script Type="LMCAPTION" Command=" (я) дýмаю{t=myslím si}" />)
        foreach (string txt in transLMAP(root))
          yield return new resxNameValue(getResxId(ref cnt), normalizeXmlText(txt, sb), (XAttribute)null);
        yield break;
      }
      //Else: Atributy k prekladu
      if (fileType == LocFileType.lmdata)
        foreach (XAttribute attr in transAttributes(root))
          yield return new resxNameValue(getResxId(attr), normalizeXmlText(((string)attr).Substring(attrStart.Length), sb), attr);
      else if (fileType == LocFileType.aspx)
        foreach (XAttribute attr in transAspxAttributes(root))
          yield return new resxNameValue(getAspxResxId(attr), normalizeXmlText((string)attr, sb), attr);
      //Preklad prosteho HTML textu
      if (fileType == LocFileType.lmdata || fileType == LocFileType.aspx) {
        //souvisle useky k prekladu
        foreach (List<XNode> fragment in transFragments(root))
          yield return new resxNameValue(getResxId(fragment[0].Parent, fileType),
            normalizeXmlText(fragment.InnerXml(html), sb), fragment);
      }
      if (fileType == LocFileType.sitemap) {
        foreach (var el in root.Descendants().Select(e => new {
          key = e.Attribute("resourceKey"),
          title = e.Attribute("title"),
          descr = e.Attribute("description")
        }).Where(kv => kv.key != null && (kv.title != null || kv.descr != null))) {
          if (el.key.Value.IndexOf('.') >= 0) throw new Exception("Wrong resourceKey " + el.key.Value);
          if (el.title != null)
            yield return new resxNameValue(el.key.Value + ".title", normalizeXmlText(el.title.Value, sb), el.key);
          if (el.descr != null)
            yield return new resxNameValue(el.key.Value + ".description", el.descr.Value, el.key);
        }
      }
      if (fileType == LocFileType.downloadXml) {
        foreach (XElement el in root.Element("Sites").Elements("SiteInfo").Where(el => el.Element("Site").Value == "com").Elements("Downloads").Elements("DownloadInfo")) {
          string id = "com." + el.Element("CourseId").Value;
          foreach (resxNameValue resx in extractDownloads(el, id)) yield return resx;
          foreach (XElement subEl in el.Element("Items").Elements("DownloadInfo")) {
            string subId = id + "." + subEl.Element("Id").Value;
            foreach (resxNameValue resx in extractDownloads(subEl, subId)) yield return resx;
          }
        }
      }
    }

    static IEnumerable<resxNameValue> extractDownloads(XElement root, string id) {
      foreach (XElement el in root.Elements().Where(el => el.Name == "GroupSubName" || el.Name == "DesktopName" || el.Name == "AppName"))
        yield return new resxNameValue(id + "." + el.Name.LocalName, el.Value, (XAttribute)null);
    }

    static void modifyDownloads(XElement root, CourseIds prodUrl, string id, Langs[] lngs, CSLocalize.LangItems li, StringBuilder sb) {
      foreach (XElement el in root.Elements().Where(el => el.Name == "GroupSubName" || el.Name == "DesktopName" || el.Name == "AppName")) {
        sb.Length = 0;
        foreach (Langs lng in lngs) {
          if (sb.Length > 0) sb.Append("~~°°^^");
          string l = lng.ToString().Replace('_', '-');
          //if (l == "es-es") l = "sp-sp";
          sb.Append(l);
          if (l == "sp-sp") l = "es-es";
          sb.Append('=');
          string val = li.GetString(id + "." + el.Name, new System.Globalization.CultureInfo(l), "*** missing translation ***");
          //Nemodifikuj AppName a DesktopName:
          if (!prodUrl.ToString().Contains("Berlitz")) {
            if (el.Name == "DesktopName") val += " (LANGMaster.com)";
            else if (el.Name == "AppName") val = "LANGMaster.com: " + val;
          }
          sb.Append(val);
        }
        el.Value = sb.ToString();
      }
    }

    //public static void LocalizeDownloadsXml(XElement root) {
    //  CSLocalize.LangItems li = new CSLocalize.LangItems(Machines.basicPath + @"rew\LMCom\App_Data\app_localresources\downloads.xml.resx");
    //  StringBuilder sb = new StringBuilder();
    //  foreach (XElement comNode in root.Element("Sites").Elements("SiteInfo").Where(el => el.Element("IsLocalized").Value == "true")) {
    //    Domains site = (Domains)Enum.Parse(typeof(Domains), comNode.Element("Site").Value);
    //    //Langs[] lngs = comNode.Element("AllLangs").Elements("Langs").Select(el => (Langs)Enum.Parse(typeof(Langs), el.Value)).ToArray();
    //    foreach (XElement el in comNode.Elements("Downloads").Elements("DownloadInfo")) {
    //      CourseIds prodUrl = (CourseIds)Enum.Parse(typeof(CourseIds), el.Element("CourseId").Value);
    //      Langs[] lngs = ProductCatalogueItems.Instance.DownloadLangs(site, prodUrl).ToArray();
    //      string id = "com." + el.Element("CourseId").Value;
    //      modifyDownloads(el, prodUrl, id, lngs, li, sb);
    //      foreach (XElement subEl in el.Element("Items").Elements("DownloadInfo")) {
    //        string subId = id + "." + subEl.Element("Id").Value;
    //        modifyDownloads(subEl, prodUrl, subId, lngs, li, sb);
    //      }
    //    }
    //  }
    //}

    /// <summary>
    /// Jednoznacne ocislovani elementu: kazdy TagName ma svuj vlastni citac
    /// </summary>
    /*static void setUniqueIds(XElement root) {
      //ocislovani elementu:
      Dictionary<string, int> dictCount = new Dictionary<string, int>();
      foreach (XElement el in root.DescendantsAndSelf()) {
        string nm = el.Name.ToString();
        if (!dictCount.ContainsKey(nm)) { dictCount.Add(nm, 0); el.AddAnnotation(0); } else { int cnt = dictCount[nm] + 1; dictCount[nm] = cnt; el.AddAnnotation(cnt); }
      }
    }*/
    /// <summary>
    /// identifikace resource pro atribut
    /// </summary>
    static string getResxId(XAttribute attr) {
      return "A" + attr.Parent.Name.LocalName + attr.Parent.Annotation(typeof(int)) + attr.Name.LocalName;
    }

    /// <summary>
    /// identifikace resource pro atribut asp tagu
    /// </summary>
    static string getAspxResxId(XAttribute attr) {
      XAttribute idAttr = attr.Parent.Attribute("ID");
      if (idAttr == null) throw new Exception("Missing ID at asp: tag with localized property");
      return (string)idAttr + "Res." + attr.Name.LocalName;
    }

    /// <summary>
    /// identifikace souvisleho fragmentu k prekladu v TRANS elementu
    /// </summary>
    static string getResxId(XElement fragmentParent, LocFileType fileType) {
      //string nm = Anot.GetAnotName(fragmentParent);
      //if (nm != null) return nm;
      int id; int count; Anot.GetAnotId(fragmentParent, out id, out count);
      return "T" + fragmentParent.Name.LocalName + (id == 0 ? null : id.ToString()) + (count == 0 ? null : "_" + count.ToString()) + (fileType == LocFileType.aspx ? ".Text" : null);
    }

    /// <summary>
    /// identifikace vety v LMAP souboru
    /// </summary>
    static string getResxId(ref int cnt) {
      try {
        return "W" + cnt.ToString();
      } finally { cnt++; }
    }
    /// <summary>
    /// vsechny atributy k prekladu pro LMDATA
    /// </summary>
    static IEnumerable<XAttribute> transAttributes(XElement root) {
      return root.DescendantsAndSelf().Attributes().Where(a => ((string)a).StartsWith(attrStart));
    }

    /// <summary>
    /// vsechny atributy k prekladu pro ASPX
    /// </summary>
    public static IEnumerable<XAttribute> transAspxAttributes(XElement root) {
      foreach (XAttribute attr in root.DescendantsAndSelf().Where(e => e.Name.Namespace == asp || e.Name.Namespace == lmlib || e.Name.Namespace == site || e.Name.Namespace == lm).Attributes().
        Where(a => a.Name.LocalName == "Text" && ((string)a).IndexOf("{'%") < 0 && ((string)a).IndexOf("{%") < 0)) yield return attr;
      foreach (XAttribute attr in root.DescendantsAndSelf().Where(e => e.Name.Namespace == asp).Attributes().
        Where(a => a.Name.LocalName == "ErrorMessage")) yield return attr;
    }

    /// <summary>
    /// zvukove vety k prekladu
    /// </summary>
    static IEnumerable<string> transLMAP(XElement root) {
      return
        from XElement el in root.Descendants("Script")
        from XAttribute attr in el.Attributes("Command")
        let val = extractLmapTrans((string)attr)
        where !string.IsNullOrEmpty(val)
        select val;
    }

    public static string extractLmapTrans(string txt) {
      int idx = txt.IndexOf("{t="); if (idx < 0) return null;
      idx += 3;
      int endIdx = txt.IndexOf('}', idx);
      return txt.Substring(idx, endIdx - idx);
    }

    /// <summary>
    /// test na Inline element 
    /// </summary>
    public static string[] inlineTags = new string[] { "b", "u", "i", "span", "a" };
    public static bool isInline(XElement el) {
      if (el.Elements().Where(e => !isInline(e)).Any()) return false;
      if (isNoTrans(el)) return true;
      if (el.Name.Namespace != html) return false;
      if (!inlineTags.Contains(el.Name.LocalName)) return false;
      if (el.Name.LocalName == "span") {
        if (el.Attributes().Count() != 1) return false;
        if (el.Attributes().Single().Name.LocalName != "class") return false;
      } else if (el.Name.LocalName == "a") {
        if (el.Attributes().Count() != 1) return false;
        if (el.Attributes().Single().Name.LocalName != "href") return false;
      } else {
        if (el.HasAttributes) return false;
      }
      return true;
    }

    /// <summary>
    /// Vrati true iff node je z pohledu prekladu prazdny
    /// </summary>
    static bool emptyNode(XNode nd) {
      if (nd is XElement) {
        XElement el = (XElement)nd;
        if (isNoTrans(el)) return false;
        foreach (XNode subNd in el.Nodes())
          if (!emptyNode(subNd)) return false;
        return true;
      } else if (nd is XComment)
        return true;
      else if (isBlankText(((XText)nd).Value))
        return true;
      else
        return false;
    }

    static bool isBlankText(string txt) {
      txt = txt.Replace("$nbsp;", " ");
      foreach (char ch in txt)
        if (!char.IsWhiteSpace(ch)) return false;
      return true;
    }

    /// <summary>
    /// normalizuje fragment z pohledu prekladu
    /// </summary>
    static void normalizeList(List<XNode> list) {
      //Vyhod prazdne nody na zacatku a na konci
      while (list.Count > 0)
        if (emptyNode(list[0])) list.RemoveAt(0); else break;
      while (list.Count > 0)
        if (emptyNode(list[list.Count - 1])) list.RemoveAt(list.Count - 1); else break;
      //Pro jednoprvkovy seznam: snaz se zanorit co nejhloubeji:
      if (list.Count == 1) {
        XNode nd = list[0];
        while (true) {
          if (nd is XText) { list.Clear(); list.Add(nd); break; } //jednoprvkovy seznam text
          XElement el = (XElement)nd;
          if (isNoTrans(el)) { list.Clear(); break; }
          if (el.Nodes().Count() == 1) nd = el.FirstNode; //jiny jednoprvkovy seznam: vrat jedineho childa
          else { list.Clear(); list.AddRange(el.Nodes()); normalizeList(list); break; }
        }
      }
    }

    /// <summary>
    /// Zakladni TRANS metoda: z HMLT vybere fragmenty k lokalizaci
    /// </summary>
    static IEnumerable<List<XNode>> transFragments(XElement root) {
      //Cyklus pres zakladni nodes: budto texty ktere nejsou v NOTRTANS a nebo vlastni NOTRANS
      foreach (XNode nd in
          from XNode n in root.DescendantNodes()
          where (
            (n.NodeType == XmlNodeType.Text &&
             n.Ancestors().Where(isTransElement()).Any() &&
             !n.Ancestors().Where(isNoTransElement()).Any() &&
             !n.Ancestors(html + "title").Any())
            ||
            (n.NodeType == XmlNodeType.Element &&
             isNoTrans((XElement)n)))
          select n)
        ///... pro kazdy zakladni nodes anotaci oznac nejvyssiho parenta, ktery je jeste inline element
        foreach (XNode el in nd.AncestorsAndSelf())
          if (!isInline(el.Parent)) {
            Anot.GetAnot(el).isBasic = true;
            break;
          }
      //cyklus pres parenty, obsahujici oznacene node
      //v techto parentech vybere souvisle useky z oznacenych childu
      List<XNode> res = new List<XNode>();
      foreach (XElement el in root.DescendantNodes().Where(e => Anot.IsBasic(e)).Select(el => el.Parent).Distinct()) {
        bool inMarked = false; res.Clear();
        foreach (XNode nd in el.Nodes()) {
          if (inMarked) {
            if (!Anot.IsBasic(nd)) {
              inMarked = false; normalizeList(res); if (res.Count > 0) { yield return res; res = new List<XNode>(); }
            } else res.Add(nd);
          } else {
            if (Anot.IsBasic(nd)) { res.Clear(); res.Add(nd); inMarked = true; }
          }
        }
        if (inMarked) {
          normalizeList(res); if (res.Count > 0) { yield return res; res = new List<XNode>(); }
        }
      }
    }

    public static IEnumerable<XElement> debugTrans(XElement trans) {
      foreach (List<XNode> els in transFragments(trans)) {
        XElement el = new XElement(html + "root",
          from XNode nd in els select nd is XText ? (XNode)new XText(nd.ToString()) : (XNode)XElement.Parse(nd.ToString())
          );
        yield return el;
      }
    }

    /// <summary>
    /// HELPER: Spocte URL adresu s preview stranky
    /// </summary>
    static string computeSeeUrl(LocCfgPageGroupFilter group, string fn) {
      if (group.Group == LocPageGroup.EA_English || group.Group == LocPageGroup.EA_French || group.Group == LocPageGroup.EA_German ||
        group.Group == LocPageGroup.EA_Italian || group.Group == LocPageGroup.EA_Russian || group.Group == LocPageGroup.EA_Spanish || group.Group == LocPageGroup.EA_Chinese) {
        return "{0}" + fn.
          Replace('\\', '/').
          Replace(fnStart, null).
          Replace("app_localresources/", null).
          Replace(".resx", null);
      }
      return fn.Substring(group.BasicPath.Length + 1).Replace(".resx", null);
    }

    /// <summary>
    /// HELPER: enumareace primarnich RESX soubory (odpovidajici adresari a masce)
    /// </summary>
    static IEnumerable<string> getFiles(string basicPath, string ext, string mask) {
      DirectoryInfo dir = new DirectoryInfo(basicPath);
      Regex regEx = new Regex(mask);
      foreach (FileInfo file in dir.GetFiles(ext, SearchOption.AllDirectories)) {
        string fn = file.FullName.Remove(0, basicPath.Length + 1).ToLowerInvariant();
        if (!regEx.IsMatch(fn)) continue;
        yield return file.FullName.ToLowerInvariant();
      }
    }
    static IEnumerable<string> getFiles(string basicPath, IEnumerable<string> exts, string mask) {
      foreach (string ext in exts)
        foreach (string fn in getFiles(basicPath, ext, mask))
          yield return fn;
    }

    public static IEnumerable<string> getFiles(IEnumerable<LocPageGroup> grps) {
      foreach (string fn in getFiles(grps, Langs.no)) yield return fn;
    }

    public static IEnumerable<string> getFiles(IEnumerable<LocPageGroup> grps, Langs lng) {
      foreach (LocPageGroup grp in grps) {
        LocCfgPageGroupFilter group = LocCfg.Instance().findPageGroup(grp);
        if (group.Extensions != null && group.FileMask != null) {
          foreach (string fn in getFiles(group.BasicPath, group.Extensions, group.FileMask)) {
            string shortFn = fn.Substring(group.BasicPath.Length).ToLowerInvariant();
            if (group.Exclude != null && group.Exclude.Contains(shortFn)) continue;
            yield return fn;
          }
        }
        if (group.Include != null)
          foreach (string fn in group.Include)
            yield return group.BasicPath + fn;
        if (group.IncludeEx != null)
          foreach (LocIncludeEx ie in group.IncludeEx.Where(i => lng == Langs.no || i.langs.Contains(lng)))
            yield return group.BasicPath + ie.FileName;
      }
    }

    public static void tradosOper2_forLang(LocPageGroup grp, Langs transLang, bool isAutoTrans) {
      LocCfgPageGroupFilter group = LocCfg.Instance().findPageGroup(grp);
      Langs srcLang = group.FindSrcLang(transLang);
      foreach (int pageId in Machines.getTradosContext().Pages.
          Where(p => p.PageGroup == (short)grp && (p.Langs == null || p.Langs.Contains(transLang.ToString()))).
          Select(p => p.Id)) {
        Trados.TradosDataContext db = Machines.getTradosContext();
        if (Machines.sb != null) Machines.sb.Length = 0;
        //Name vsech zdrojovych vet
        var srcIds = db.Sentences.Where(s => s.PageId == pageId && s.TransLang == (short)srcLang).Select(s => s.Name).ToArray();
        //Vsechny doposud pripravene vety
        var oldSents = db.Sentences.Where(s2 => s2.PageId == pageId && s2.TransLang == (short)transLang).ToArray();
        //Vymaz nepotrebne vety (neboli prelozene vety, ktere nejsou ve zdroji)
        db.Sentences.DeleteAllOnSubmit(oldSents.Where(s => !srcIds.Contains(s.Name)));
        //Vsechny zdroje a lookup preklady
        var autoTrans = db.Sentences.
          Where(s => s.PageId == pageId && s.TransLang == (short)srcLang && s.TransText != null).
          Select(s => new {
            name = s.Name,
            srcText = s.TransText,
            transTexts = db.Lookups.Where(l => l.SrcLang == (short)srcLang && l.TransLang == (short)transLang && l.SrcHash == s.TransHash && l.SrcText == s.TransText).
              Select(l => l.TransText).ToArray()
          }).
          ToArray();
        //Vytvoreni novych nebo uprava starych vet
        foreach (string name in srcIds) { //pro vsechny potrebne vety
          Trados.Sentence sent;
          //if (name == "l09/a/hufex1_l09_a03/Ttrans5") 
          //  sent = null;
          try {
            sent = oldSents.Where(s => s.Name == name).SingleOrDefault(); //stara veta
          } catch (Exception exp) {
            throw new Exception(string.Format("oldSents.SingleOrDefault: name={0}, grp={1}, lang={2}", name, grp, transLang), exp);
          }
          try {
            var item = autoTrans.Where(at => at.name == name).SingleOrDefault(); //novy zdroj a ev. preklad z lookupu
            if (sent == null) { //zalozeni nove vety
              if (isAutoTrans && item != null && item.transTexts.Count() == 1) //existuje prave jeden lookup, dopln novy zdroj a preklad (veta DONE)
                Trados.Sentence.insert(db, pageId, name, item.srcText, (Langs)srcLang, item.transTexts.Single(), transLang);
              else //ne, nova veta je prezdna (TRANS)
                Trados.Sentence.insert(db, pageId, name, null, (Langs)srcLang, null, transLang);
            } else { //veta existuje
              if (isAutoTrans && item != null && item.transTexts.Count() == 1) { //existuje prave jeden lookup
                sent.SrcText = item.srcText; sent.TransText = item.transTexts.Single(); //aktualizuj vetu (veta DONE)
                sent.finish();
              }
            }
          } catch (Exception exp) {
            throw new Exception(string.Format("autoTrans.SingleOrDefault: name={0}, grp={1}, lang={2}", name, grp, transLang), exp);
          }
        }
        db.SubmitChanges();
      }
    }

    /// <summary>
    /// Pred save vety: ev. pridani do lookup tabulky
    /// </summary>
    public static void RefreshLookup(Trados.TradosDataContext db, Trados.Sentence srcSent, string oldSrcText, string oldTransText, StringBuilder sb, StringBuilder log) {
      if (string.IsNullOrEmpty(srcSent.SrcText) || string.IsNullOrEmpty(srcSent.TransText)) return;
      Trados.Lookup[] lkps;
      if (!string.IsNullOrEmpty(oldSrcText))
        lkps = db.Lookups.Where(l => l.SrcLang == srcSent.SrcLang && l.TransLang == srcSent.TransLang &&
        ((l.SrcHash == srcSent.SrcHash && l.SrcText == srcSent.SrcText) ||
        (l.SrcHash == LowUtils.crc(oldSrcText) && l.SrcText == oldSrcText))).ToArray();
      else
        lkps = db.Lookups.
          Where(l => l.SrcLang == srcSent.SrcLang && l.TransLang == srcSent.TransLang && l.SrcHash == srcSent.SrcHash && l.SrcText == srcSent.SrcText).
          ToArray();
      //Lookup zaznam nove verze sentence
      Trados.Lookup newLkp;
      try {
        newLkp = lkps.Where(l => l.SrcText == srcSent.SrcText && l.TransText == srcSent.TransText).SingleOrDefault();
      } catch (Exception exp) {
        throw new Exception(string.Format("Lookup duplicites error: src={0}, trans={1}", srcSent.SrcText, srcSent.TransText), exp);
      }
      //Lookup zaznam stare verze sentence
      //Jedna se o opravu? Kdyz ano, budto puvodni polozku smaz (existuje-li jiz nova) nebo uprav (neexistuje-li)
      Trados.Lookup oldLkp;
      try {
        oldLkp = lkps.Where(l => !string.IsNullOrEmpty(oldSrcText) && !string.IsNullOrEmpty(oldTransText) && l.SrcText == oldSrcText && l.TransText == oldTransText).SingleOrDefault();
      } catch (Exception exp) {
        throw new Exception(string.Format("Lookup duplicites error: src={0}, trans={1}", oldSrcText, oldTransText), exp);
      }
      if (oldLkp != null) {
        if (newLkp == null) oldLkp.finish(srcSent); //else db.Lookups.DeleteOnSubmit(oldLkp);
        return;
      }
      //nejedna se o opravu, novy zaznam v lookup jiz existuje: neni co delat
      if (newLkp != null) return;
      //Kontrola duplicit
      if (lkps.Where(l => l.SrcText == srcSent.SrcText).Any())
        if (log != null)
          log.AppendFormat("Vznikla duplicita v Lookup tabulce, doporučujeme zkontrolovat (src={0}, trans={1})<br/>", srcSent.SrcText, srcSent.TransText);
      //Lookup polozka neexistuje, zaloz:
      Trados.Lookup lkp = new Trados.Lookup();
      db.Lookups.InsertOnSubmit(lkp);
      lkp.finish(srcSent);
    }

    /// <summary>
    /// Normalizace textu: lowercase, pryc zbytecne mezery
    /// </summary>
    static string normalizeSrc(string src, StringBuilder sb) {
      if (string.IsNullOrEmpty(src)) return src;
      //string res = src.Replace(newlineCode, " ").ToLower();
      sb.Length = 0;
      int st = 0;
      foreach (char ch in src.ToLower()) {
        if (st == 0) {
          if (char.IsWhiteSpace(ch)) st = 1;
          sb.Append(ch);
        } else {
          if (!char.IsWhiteSpace(ch)) { st = 0; sb.Append(ch); }
        }
      }
      return sb.ToString();
    }

    /// <summary>
    /// Pro skupinu stranek a jazyk vygeneruje RESX soubory (nebo lokalizovany .JS soubor)
    /// </summary>
    public static void GenerateResx(LocPageGroup grp, Langs transLang) {
      Trados.TradosDataContext db = Machines.getTradosContext(false);
      foreach (Trados.Page pg in db.Pages.Where(p => p.PageGroup == (short)grp))
        GenerateResx(db, pg, transLang);
    }

    public static void GenerateResx(Trados.TradosDataContext db, Trados.Page pg, Langs transLang) {
      if (pg.PageGroup == (short)LocPageGroup.newEA || pg.PageGroup == (short)LocPageGroup.fakeRussian) return;
      LocFileType ft = TradosLib.getFileType(pg.FileName.Replace(".resx", null));
      LocPageGroup grp = (LocPageGroup)pg.PageGroup;
      if (ft == LocFileType.js) {
        //generace lokalizovaneho .JS souboru misto .RESX
        string fn = null; string fnContent = null;
        LocCfgPageGroupFilter group = LocCfg.Instance().findPageGroup(grp);
        switch (grp) {
          case LocPageGroup.EA_Code:
            fn = group.BasicPath + @"\framework\script\lm\tradosData." + transLang.ToString().Replace('_', '-') + ".js";

            break;
          case LocPageGroup.rew_school:
            fn = group.BasicPath + @"\schools\loc\tradosData." + transLang.ToString().Replace('_', '-') + ".js";
            fnContent = NewEATradosLib.locJS(db.Sentences.Where(s => s.PageId == pg.Id && s.TransLang == (short)transLang).ToDictionary(sent => sent.Name, sent => sent.TransText == null ? "###TRANS TODO###" : transFinal(sent.TransText)));
            fnContent = "var tradosData = " + fnContent + "; tradosData[\"forceLang\"] = \"" + transLang.ToString().Replace('_', '-') + "\";";
            break;
          case LocPageGroup.rew_rewise:
            fn = group.BasicPath + @"\rewise\loc\tradosData." + transLang.ToString().Replace('_', '-') + ".js";
            break;
        }
        if (fn != null) {
          using (StreamWriter wr = new StreamWriter(fn)) {
            if (fnContent != null) wr.Write(fnContent);
            else {
              wr.WriteLine("var tradosData = [];");
              foreach (Trados.Sentence sent in db.Sentences.Where(s => s.PageId == pg.Id && s.TransLang == (short)transLang)) {
                wr.Write("tradosData['"); wr.Write(sent.Name); wr.Write("'] = '");
                wr.Write(sent.TransText == null ? "###TRANS TODO###" : transFinal(sent.TransText).Replace("'", "\\'"));
                wr.WriteLine("';");
              }
            }
          }
        }
      } else {
        bool isPartial = pg.Langs != null && !pg.Langs.Contains(transLang.ToString());
        string fn = resxFileName(pg.FileName, transLang);
        Langs dbLang = isPartial ? Langs.en_gb : transLang; //Difotne je vse anglicky
        LowUtils.AdjustFileDir(fn);
        using (ResXResourceWriter wr = new ResXResourceWriter(fn))
          foreach (Trados.Sentence sent in db.Sentences.Where(s => s.PageId == pg.Id && s.TransLang == (short)dbLang))
            wr.AddResource(sent.Name, sent.TransText == null ? "###TRANS TODO###" : transFinal(sent.TransText).Replace("$nbsp;", "&nbsp;").Replace(crlfCode, " "));/*sent.TransText.Replace(crlfCode, "\r\n"));*/
        if (ft == LocFileType.lmdata && (grp == LocPageGroup.CPV || fn.ToLowerInvariant().IndexOf(@"\localizecommon\") >= 0)) {
          //vystup primo lokalizovaneho lmdata
          string lmdata = pg.FileName.Replace(".resx", null).Replace(@"app_localresources\", null);
          string resData = pg.FileName.Replace(".resx", null).Replace(".lmdata", "." + transLang.ToString().Replace('_', '-') + ".lmdata");
          using (FileStream fs = new FileStream(resData, FileMode.Create, FileAccess.Write))
            LocalizeXmlLow(lmdata, transLang, fs, false);
        }
      }
    }

    static string addTrans(string prop, string trans, Langs lang, Dictionary<Langs, string> buf) {
      LocalizeLib.LocStringDecode(prop, ref buf);
      if (buf.Count == 0) return prop;
      buf[lang] = trans == null ? "###TRANS TODO###" : transFinal(trans);
      return LocalizeLib.LocStringEncode(buf);
    }

    /// <summary>
    /// Odstrani pomocne zavorky z přeloženého řetězce
    /// </summary>
    //static Regex rxTransFinal = new Regex(@"\(\*(\.|\,|\w|\s)*?\*\)");
    static Regex rxTransFinal = new Regex(@"\(\*.*?\*\)");
    public static string transFinal(string trans) {
      if (trans == null) return null;
      return rxTransFinal.Replace(trans, "");
    }

    public static LocCommand excelFlag(string oldSrcText, string newSrcText) {
      if (string.IsNullOrEmpty(newSrcText)) return LocCommand.NONE;
      if (oldSrcText == newSrcText) return LocCommand.DONE;
      if (oldSrcText == null) return LocCommand.TRANS;
      if (oldSrcText != newSrcText) return LocCommand.CHANGE;
      throw new Exception("Missing code here!");
    }

    public static string dataPath(string seeUrl, Langs lang, LocPageGroup grp) {
      return string.Format(seeUrl, dataPath(lang, grp));
    }

    static string dataPath(Langs lang, LocPageGroup grp) {
      if (grp.ToString().IndexOf("EA") == 0) {
        string prefix = "http://test.langmaster.cz/";
        switch (lang) {
          case Langs.cs_cz: return prefix + "eduauthornew/";
          default:
            string lng = lang.ToString().Substring(3);
            return prefix + "com" + lng + "/";
        }
      } else {
        return "http://test.langmaster.cz/lmcom/com/" + lang.ToString().Replace('_', '-') + "/";
      }
    }

    /*static bool isUrl(string s) {
      return !string.IsNullOrEmpty(s);// && (s.IndexOf("http://")==0 || s.IndexOf("{0}")==0);
    }*/

    public struct exportCmlItem {
      public exportCmlItem(string SeeUrl, int Id, string Name, LocCommand actCmd, string TransText, string OldSrcText, string NewSrcText, int newSrcId) {
        this.SeeUrl = SeeUrl; this.Id = Id; this.Name = Name; this.actCmd = actCmd; this.TransText = TransText;
        this.OldSrcText = OldSrcText; this.NewSrcText = NewSrcText; this.newSrcId = newSrcId;
      }
      public exportCmlItem(string SeeUrl, int Id, string Name, LocCommand actCmd, string TransText, string OldSrcText, string NewSrcText) :
        this(SeeUrl, Id, Name, actCmd, TransText, OldSrcText, NewSrcText, 0) { }

      public string SeeUrl; //url s nahledem 
      public int Id;
      public LocCommand actCmd; //OK, DONE, TRANS apod.
      public string TransText; //preklad
      public string OldSrcText; //Odpovida aktualni verzi prakladu
      public string NewSrcText; //Novy text k prekladu: ten je nutno znova prelozit
      public string Name; //Identifikace vety
      public int newSrcId;
      //kvuli data binding
      public string seeUrl { get { return SeeUrl; } }
      public int id { get { return Id; } }
      public LocCommand cmd { get { return actCmd; } }
      public string transText { get { return TransText; } }
      public string newSrcText { get { return NewSrcText; } }
      public string oldSrcText { get { return OldSrcText; } }
    }

    public static void AutoTranslate(LocPageGroup grp, Langs transLang, StringBuilder log) {
      List<LocCommand> filter = new List<LocCommand>(); filter.Add(LocCommand.OK);
      StringBuilder sb = new StringBuilder();
      foreach (exportCmlItem item in ExportXmlItems(grp, transLang, filter)) {
        Trados.TradosDataContext db = Machines.getTradosContext();
        Trados.Sentence sent = db.Sentences.Where(s => s.Id == item.Id).Single();
        string srcText = item.NewSrcText;
        string transText = item.TransText;
        if (!checkTrans(item.Id, ref srcText, ref transText, sb, log)) continue;
        string oldTransText = sent.TransText;
        sent.SrcText = srcText;
        sent.TransText = transText;
        sent.finish();
        RefreshLookup(db, sent, excelTextToXmlText(item.oldSrcText, log), oldTransText, sb, log);
        db.SubmitChanges();
      }
    }

    public static int ExportXml(LocPageGroup grp, Langs transLang, Stream xml, List<LocCommand> filter) {
      XElement pattern = XElement.Load(HostingEnvironment.ApplicationPhysicalPath + "app_data/TradosExcelPattern.xml", LoadOptions.PreserveWhitespace);
      XNamespace ad = "urn:schemas-microsoft-com:office:spreadsheet";
      XNamespace ss = "urn:schemas-microsoft-com:office:spreadsheet";
      XElement insertPoint = pattern.Descendants(ad + "Row").Single();
      Langs srcLang = LocCfg.Instance().findPageGroup(grp).FindSrcLang(transLang);
      int cnt = 0;
      foreach (var itemGroup in ExportXmlItems(grp, transLang, filter).GroupBy(it => it.SeeUrl)) {
        bool headerOK = false; //ridi pripravu prave jednoho radku s URL adresou na grupu
        foreach (exportCmlItem item in itemGroup) {
          if (!headerOK) {
            headerOK = true;
            insertPoint.AddAfterSelf(
              new XElement(ad + "Row",
                string.IsNullOrEmpty(itemGroup.Key)/*!isUrl(itemGroup.Key)*/ ? null : new XElement(ad + "Cell",
                  new XAttribute(ss + "Index", 2),
                  new XAttribute(ss + "StyleID", "s71"),
                  new XAttribute(ss + "HRef", dataPath(itemGroup.Key, transLang, grp)),
                  new XElement(ad + "Data",
                    new XAttribute(ss + "Type", "String"),
                    "GOTO Translation"
                  )
                ),
                string.IsNullOrEmpty(itemGroup.Key)/*!isUrl(itemGroup.Key)*/ ? null : new XElement(ad + "Cell",
                  new XAttribute(ss + "Index", 3),
                  new XAttribute(ss + "StyleID", "s71"),
                  new XAttribute(ss + "HRef", dataPath(itemGroup.Key, srcLang, grp)),
                  new XElement(ad + "Data",
                    new XAttribute(ss + "Type", "String"),
                    "GOTO Source"
                  )
                ),
                new XElement(ad + "Cell",
                  new XAttribute(ss + "Index", 4),
              //new XAttribute(ss + "StyleID", "s71"),
                  new XElement(ad + "Data",
                    new XAttribute(ss + "Type", "String"),
                    itemGroup.Key
                  )
                )
              )
            );
            insertPoint = (XElement)insertPoint.NextNode;
          }
          cnt++;
          insertPoint.AddAfterSelf(
            new XElement(ad + "Row",
            //new XAttribute(ss + "AutoFitHeight", 1),
              new XElement(ad + "Cell",
                new XElement(ad + "Data",
                  new XAttribute(ss + "Type", "String"),
                  item.actCmd.ToString()
                )
              ),
              new XElement(ad + "Cell",
                new XElement(ad + "Data",
                  new XAttribute(ss + "Type", "String"),
                  HttpUtility.HtmlDecode(item.TransText)
                )
              ),
              new XElement(ad + "Cell",
                new XElement(ad + "Data",
                  new XAttribute(ss + "Type", "String"),
                  HttpUtility.HtmlDecode(item.NewSrcText)
                )
              ),
              new XElement(ad + "Cell",
                new XElement(ad + "Data",
                  new XAttribute(ss + "Type", "String"),
                  HttpUtility.HtmlDecode(item.OldSrcText)
                )
              ),
              new XElement(ad + "Cell",
                new XElement(ad + "Data",
                  new XAttribute(ss + "Type", "Number"),
                  item.Id
                )
              )
            )
          );
          insertPoint = (XElement)insertPoint.NextNode;
        }
      }
      string output = pattern.ToString(SaveOptions.DisableFormatting);
      output = @"<?xml version=""1.0""?><?mso-application progid=""Excel.Sheet""?>" + output.Replace(crlfCode, "&#10;");
      byte[] outputByte = Encoding.UTF8.GetBytes(output);
      xml.Write(outputByte, 0, outputByte.Length);
      return cnt;
    }

    /// <summary>
    /// vrati vety k prekladu, v excel kodovani
    /// </summary>
    public static IEnumerable<exportCmlItem> ExportXmlItems(Trados.TradosDataContext db, Trados.Page pg, Langs srcLang, Langs transLang, List<LocCommand> filter) {
      StringBuilder sb = new StringBuilder();

      /*var s1 =
        (from Trados.Sentence s in db.Sentences.Where(s => s.PageId == pg.Id && s.TransLang == (short)transLang)
         let newSrc = db.Sentences.Where(s2 => s2.PageId == s.PageId && s2.TransLang == s.SrcLang && s2.Name == s.Name).Select(s2 => new { s2.TransText, s2.TransHash, s2.Id }).Single()
         select new {
           sent = s,
           newSrcText = newSrc.TransText,
           newSrcId = newSrc==null ? -1 : newSrc.Id,
           lookups = db.Lookups.Where(l => l.SrcLang == (short)srcLang && l.TransLang == (short)transLang && l.SrcHash == newSrc.TransHash).
             Select(l => new { l.SrcText, l.TransText })
         }).ToArray();
      if (s1 == null) yield break;*/

      var sentSrcTexts =
        (from Trados.Sentence s in db.Sentences.Where(s => s.PageId == pg.Id && s.TransLang == (short)transLang)
         //let newSrc = db.Sentences.Where(s2 => s2.PageId == s.PageId && s2.TransLang == s.SrcLang && s2.Name == s.Name).Select(s2 => new { s2.TransText, s2.TransHash, s2.Id }).Single()
         let newSrc = db.Sentences.Where(s2 => s2.PageId == s.PageId && s2.TransLang == s.SrcLang && s2.Name == s.Name).Select(s2 => new { s2.TransText, s2.TransHash, s2.Id }).First()
         select new {
           sent = s,
           newSrcText = newSrc.TransText,
           newSrcId = newSrc == null ? -1 : newSrc.Id,
           lookups = db.Lookups.Where(l => l.SrcLang == (short)srcLang && l.TransLang == (short)transLang && l.SrcHash == newSrc.TransHash).
             Select(l => new { l.SrcText, l.TransText })
         }).ToArray();
      //Cyklus pres sentence k prekladu (obohacene o text zdrojove sentence)
      foreach (var sentSrcText in sentSrcTexts) {
        string oldSrcText = xmlToExcel(sentSrcText.sent.SrcText, sb);
        string newSrcText = xmlToExcel(sentSrcText.newSrcText, sb);
        LocCommand actCmd = excelFlag(oldSrcText, newSrcText);
        if (actCmd == LocCommand.NONE) continue; //Zdroj je prazdny, neni co prekladat
        //Lookup zaznam: exact match na SrcText
        string[] lkp = sentSrcText.lookups.Where(l => l.SrcText == sentSrcText.newSrcText).Select(l => l.TransText).Distinct().ToArray();
        //lookup zaznam existuje: zmena TRANS nebo CHANGE na OK commandu pro pripad unique exact match v lookup
        if ((actCmd == LocCommand.TRANS || actCmd == LocCommand.CHANGE) && lkp != null && lkp.Length > 0)
          actCmd = lkp.Length == 1 ? LocCommand.OK : LocCommand.CHOICE;
        if (filter != null && filter.IndexOf(actCmd) < 0) continue;
        //Spocitej TransText:
        string transTxt;
        if (actCmd == LocCommand.TRANS)  //predpripravi text s NOTRANS prvky
          transTxt = transXmlToExcel(sentSrcText.sent.TransText, sentSrcText.newSrcText, sb);
        else if (actCmd == LocCommand.OK)  //predpripravi text s NOTRANS prvky
          transTxt = xmlToExcel(lkp[0], sb);
        else if (actCmd == LocCommand.CHOICE) { //nabidne volbu z puvodniho prekladu a vsech lookups
          sb.Length = 0;
          if (!string.IsNullOrEmpty(sentSrcText.sent.TransText)) sb.Append(sentSrcText.sent.TransText + crlfCode + "=============" + crlfCode);
          foreach (string ls in lkp.Where(s => s != sentSrcText.sent.TransText))
            sb.Append(ls + crlfCode + "=============" + crlfCode);
          transTxt = xmlToExcel(sb.ToString(), sb);
        } else //puvodni TRANS text (pro done nebo change)
          transTxt = xmlToExcel(sentSrcText.sent.TransText, sb);
        //return
        yield return new exportCmlItem(
          pg.SeeUrl,
          sentSrcText.sent.Id,
          sentSrcText.sent.Name,
          actCmd,
          transTxt,
          oldSrcText,
          newSrcText,
          sentSrcText.newSrcId);
      }
    }

    public static IEnumerable<exportCmlItem> ExportXmlItems(LocPageGroup grp, Langs transLang, List<LocCommand> filter) {
      Langs srcLang = LocCfg.Instance().findPageGroup(grp).FindSrcLang(transLang);
      Trados.TradosDataContext db = Machines.getTradosContext(false);
      foreach (Trados.Page pg in db.Pages.Where(p => p.PageGroup == (short)grp))
        foreach (exportCmlItem item in ExportXmlItems(db, pg, srcLang, transLang, filter))
          yield return item;

    }

    static Regex rxBrackets = new Regex(@"(\[\%.*?\%\])|(\[\(\%.*?\%\)\])");

    /// <summary>
    /// Prevede trans XML do excelu. V pripade prvniho prekladu (trans prazdny) jej inicializuje ze sourc
    /// </summary>
    public static string transXmlToExcel(string trans, string source, StringBuilder sb) {
      if (string.IsNullOrEmpty(trans)) {
        //Predpriprav [%..%] [(%..%)] zavorky pro prvni preklad
        trans = xmlToExcel(source, sb);
        sb.Length = 0;
        foreach (regExItem match in regExItem.Parse(trans, rxBrackets))
          if (match.IsMatch) { sb.Append("##????##"); sb.Append(match.Value); }
        return sb.Length > 0 ? sb.ToString() + "##????##" : null;
      } else
        //dalsi preklad, pouze zakodu
        return xmlToExcel(trans, sb);
    }

    /// <summary>e
    /// V txt je budto string nebo XML. Vrati budto string nebo Excel kod (XML s nahrazenymi <>&amp; apod.)
    /// </summary>
    public static string xmlToExcel(string txt, StringBuilder sb = null) {
      try {
        if (hasXmlTags(txt) == 0) return txt;
        XElement el = XElement.Parse("<root>" + txt + "</root>", LoadOptions.PreserveWhitespace);
        foreach (XElement span in el.Descendants("span").Where(e => !e.AncestorsAndSelf().Where(isNoTransElement()).Any())) {
          string cls = (string)span.Attribute("class");
          span.RemoveAttributes();
          span.Name = cls;
        }
        if (sb == null) sb = new StringBuilder(); else sb.Length = 0;
        childsToExcel(el, sb);
        return sb.ToString();
      } catch (Exception exp) {
        throw new Exception(txt, exp);
      }
    }

    #region xmlToExcel
    static void childsToExcel(XElement el, StringBuilder sb) {
      foreach (XNode nd in el.Nodes()) nodeToExcel(nd, sb);
    }

    static void nodeToExcel(XNode nd, StringBuilder sb) {
      if (nd is XText) sb.Append(((XText)nd).ToString());
      else {
        XElement el = (XElement)nd; string nm = el.Name.LocalName.ToLowerInvariant();
        if (el.Name.LocalName == "a") {
          sb.Append("[a%"); sb.Append(el.AttributeValue("href")); sb.Append("a%]");
          string inner = el.Nodes().InnerXml();
          if (inner != null) sb.Append(inner.Replace('<', '{').Replace('>', '}'));
          sb.Append("[%a]");
        } else if (!isNoTrans(el)) {
          if (el.Nodes().Any()) {
            sb.Append('{'); sb.Append(nm); sb.Append('}');
            childsToExcel(el, sb);
            sb.Append("{/"); sb.Append(nm); sb.Append('}');
          } else {
            sb.Append('{'); sb.Append(nm); sb.Append("/}");
          }
        } else if (el.Name.LocalName == "notrans") {
          sb.Append("[%"); sb.Append(el.Nodes().InnerXml().Replace('<', '{').Replace('>', '}')); sb.Append("%]");
        } else {
          sb.Append("[(%"); sb.Append(el.ToString(SaveOptions.DisableFormatting).Replace('<', '{').Replace('>', '}')); sb.Append("%)]");
        }
      }
    }

    static int hasXmlTags(string txt) {
      if (string.IsNullOrEmpty(txt)) return 0;
      int gtCnt = txt.ToCharArray().Where(ch => ch == '<').Count(); if (gtCnt == 0) return 0;
      int ltCnt = txt.ToCharArray().Where(ch => ch == '>').Count(); if (gtCnt != ltCnt) return 0;
      return ltCnt;
    }

    #endregion xmlToExcel

    /// <summary>
    /// V txt je Excel. Vrati XML
    /// </summary>
    static XElement excelToXml(string txt, int id, StringBuilder log) {
      txt = "<root>" + txt.Replace("{", "<").Replace("}", ">");
      //Osetreni nejednoznacnosti a%]: konec notrans nebo konec <a href=""> tagu.
      if (txt.IndexOf("[%a]") >= 0)
        txt = txt.Replace("[%a]", "</a>").Replace("[a%", "<a href=\"").Replace("a%]", "\">");
      else
        txt = txt.Replace("[%", "<notrans>").Replace("%]", "</notrans>");
      txt = txt.Replace("[(%", null).Replace("%)]", null) + "</root>";
      try {
        XElement res = XElement.Parse(txt, LoadOptions.PreserveWhitespace);
        foreach (XElement el in res.Descendants()
          .Where(e => !e.AncestorsAndSelf().Where(isNoTransElement()).Any() && !(new string[] { "i", "u", "b", "a" }).Contains(e.Name.LocalName))) {
          el.Add(new XAttribute("class", el.Name.LocalName)); el.Name = "span";
        }
        return res;
      } catch (Exception e) {
        if (log != null) { log.Append("*** Parse error in "); log.Append(id); log.Append(": "); log.Append(txt); log.Append(": "); log.Append(e.Message); log.Append("<br/>"); }
        return null;
      }
    }


    /// <summary>
    /// V txt je budto string nebo Excel. Vrati budto string nebo XML
    /// </summary>
    public static string excelTextToXmlText(string txt, StringBuilder log = null) {
      if (hasExcelTags(txt, 0, log) == 0) return txt;
      XElement el = excelToXml(txt, 0, log);
      if (el == null)
        throw new Exception();
      return el.InnerXml();
    }


    public static bool checkBlank(string txt) {
      if (string.IsNullOrEmpty(txt)) return true;
      XElement trans = XElement.Parse("<root> " + txt + " </root>", LoadOptions.PreserveWhitespace);
      foreach (XElement el in trans.Descendants("notrans").ToArray()) {
        string cnt = el.Value;
        el.RemoveNodes(); el.Add(new XText("##||##" + cnt + "##||##"));
      }
      string[] parts = trans.Value.Split(new string[] { "##||##" }, StringSplitOptions.None);
      if (parts.Length <= 1) return true;
      for (int i = 1; i < parts.Length; i++) {
        string s1 = parts[i - 1]; string s2 = parts[i];
        if (
          (string.IsNullOrEmpty(s1) || char.IsLetterOrDigit(s1[s1.Length - 1])) &&
          (string.IsNullOrEmpty(s2) || char.IsLetterOrDigit(s2[0]))
         )
          return false;
      }
      return true;
    }

    static string normalizeNoTrans(string noTrans) {
      return noTrans.ToLower().Trim().TrimEnd('.', ',', '"', '?', ':', ' ');
    }

    public static bool checkTrans(int id, ref string srcText, ref string transText, StringBuilder sb, StringBuilder log) {
      string transRes = null; string srcRes = null;
      srcText = TradosLib.normalizeXmlText(srcText, sb); transText = TradosLib.normalizeXmlText(transText, sb);
      int transTags = hasExcelTags(transText, id, log); if (transTags < 0) return false; //rozdilny XEpocet { a } v trans
      int srcTags = hasExcelTags(srcText, id, log); if (srcTags < 0) return false; //rozdilny pocet { a } v src
      if (transTags != srcTags) { //rozdilny pocet { mezi src a trans
        log.Append("--- Warning in "); log.Append(id);
        log.Append("<br/>");
        log.Append("Source: "); log.Append(srcText); log.Append("<br/>");
        log.Append("Trans: "); log.Append(transText); log.Append("<br/>");
        log.Append(": Number of HTML tags in source and trans is different."); log.Append("<br/>");
        return false;
      }
      //Analyzuj trans
      XElement trans = null;
      //if (transTags == 0) transRes = transText; //obycejny text
      //else { //XML text
      trans = excelToXml(transText, id, log); if (trans == null) return false; //parse error
      transRes = TradosLib.normalizeXmlText(trans.InnerXml(), sb);
      if (transTags == 0) trans = null;
      //}
      //Analyzuj src
      XElement src = null;
      //if (srcTags == 0) srcRes = srcText;
      //else {
      src = excelToXml(srcText, id, log); if (src == null) return false;
      srcRes = TradosLib.normalizeXmlText(src.InnerXml(), sb);
      if (srcTags == 0) src = null;
      //}
      //Kontrola NOTRANS: stejny pocet, stejny obsah
      if (src != null) {
        List<string> srcNoTrans = src.Descendants().Where(isNoTransElement()).Select(e => normalizeNoTrans(e.Value/*innerXml()*/)).ToList();
        if (srcNoTrans.Count > 0) {
          if (trans == null) {
            log.Append("*** Error in "); log.Append(id); log.Append("<br/>");
            log.Append("Source: "); log.Append(srcText); log.Append("<br/>");
            log.Append("Trans: "); log.Append(transText); log.Append("<br/>");
            log.Append(": Different [%...%] content in source and trans.");
            log.Append(" Source contains [%...%], trans does not contain [%...%].");
            log.Append("<br/>"); return false;
          } else {
            List<string> transNoTrans = trans.Descendants().Where(isNoTransElement()).Select(e => normalizeNoTrans(e.Value/*innerXml()*/)).ToList();
            if (transNoTrans.Count != srcNoTrans.Count) {
              log.Append("*** Error in "); log.Append(id); log.Append("<br/>");
              log.Append("Source: "); log.Append(srcText); log.Append("<br/>");
              log.Append("Trans: "); log.Append(transText); log.Append("<br/>");
              log.Append(": Different [%...%] content in source and trans.");
              log.Append(" Different number of [%...%].");
              log.Append("<br/>"); return false;
            } else {
              srcNoTrans.Sort(); transNoTrans.Sort(); bool ok = true;
              for (int i = 0; i < srcNoTrans.Count; i++)
                if (srcNoTrans[i] != transNoTrans[i]) {
                  log.Append("*** Error in "); log.Append(id); log.Append("<br/>");
                  log.Append("Source: "); log.Append(srcText); log.Append("<br/>");
                  log.Append("Trans: "); log.Append(transText); log.Append("<br/>");
                  log.Append(": Different [%...%] content in source and trans (");
                  log.Append("source='"); log.Append(srcNoTrans[i]); log.Append("', trans="); log.Append("'"); log.Append(transNoTrans[i]); log.Append("')");
                  log.Append("<br/>"); ok = false; break;
                }
              if (!ok) return false;
            }
          }
        }
      }
      srcText = srcRes; transText = transRes;
      return true;
    }

    public static void ImportXml(string xml, Trados.Sentence dummySent, StringBuilder log) {
      if (xml[0] != '<') xml = xml.Remove(0, 1);
      //xml = xml.Replace("&#10;", crlfCode);
      xml = xml.Replace("&#10;", " ");
      XElement xmlData = XElement.Parse(xml, LoadOptions.PreserveWhitespace);
      XNamespace ad = "urn:schemas-microsoft-com:office:spreadsheet";
      XNamespace ss = "urn:schemas-microsoft-com:office:spreadsheet";
      //ID vet s priznakem OK z Excel XML
      var okNodeIds =
        from el in xmlData.Descendants(ad + "Data")
        where
          (string)el.Attribute(ss + "Type") == "Number" && //ciselny node, obsahujici sentence ID
          el.Parent.Parent.Elements().ElementAt(0).Elements().ElementAt(0).Value.ToLower() == "ok" //veta oznacena jako OK
        select int.Parse(el.Value);
      StringBuilder sb = new StringBuilder();
      //Vety k prekladu z
      foreach (int okId in okNodeIds) {
        try {
          XElement row = xmlData.Descendants(ad + "Data").Where(el => (string)el.Attribute(ss + "Type") == "Number" && el.Value == okId.ToString()).Single().Parent.Parent;
          insertTrans(okId, dummySent,
            row.Elements().ElementAt(3).Value, //stary vzor je v Cell s indexem 3
            row.Elements().ElementAt(2).Value, //novy vzor je v Cell s indexem 2
            row.Elements().ElementAt(1).Value, //preklad je v Cell s indexem 1
            log, sb);
        } catch {/*loguje se*/}
      }
    }

    public static Trados.Sentence insertTrans(int id, Trados.Sentence dummySent, string oldSrcText, string newSrcText, string transText, StringBuilder log, StringBuilder sb) {
      if (!checkTrans(id, ref newSrcText, ref transText, sb, log)) return null; //kontrola stejnych NOTRANS 
      Trados.TradosDataContext db = Machines.getTradosContext();
      Trados.Sentence sent = null;
      try {
        sent = db.Sentences.Where(s => s.Id == id).Single();
      } catch (Exception exp) {
        if (dummySent != null) {
          sent = dummySent;
          oldSrcText = null;
        } else
          throw new Exception(string.Format("Sentence {0}", id), exp);
      }
      string oldTransText = sent.TransText;

      sent.SrcText = newSrcText;
      sent.TransText = normalizeXmlText(transText, sb);
      sent.finish();

      RefreshLookup(db, sent, excelTextToXmlText(oldSrcText, log), oldTransText, sb, log);
      db.SubmitChanges();
      return sent;
    }

    static int hasExcelTags(string txt, int id, StringBuilder log) {
      if (string.IsNullOrEmpty(txt)) return 0;
      int gtCnt = txt.Split(new string[] { "{", "[%" }, StringSplitOptions.None).Length - 1;
      int ltCnt = txt.Split(new string[] { "}", "%]" }, StringSplitOptions.None).Length - 1;
      if (gtCnt != ltCnt) {
        if (log != null) { log.Append("*** Error in "); log.Append(id); log.Append(": "); log.Append("Missing '{' or '}' character in HTML tag"); log.Append("<br/>"); }
        return -1;
      }
      return ltCnt;
    }

    static string getResxValue(string id, XElement res, string fn) {
      try {
        //LM16
        //var cfg = NewEATradosLib.HackEx();
        //if (cfg != null && cfg.courseId != CourseIds.EnglishE) {
        //  return NewEATradosLib.onGetResxValue(id, fn);
        //}
        return res.Descendants(empty + "value").Where(el => (string)el.Parent.Attribute("name") == id).Select(e => e.Value).Single();
      } catch {
        return "*** getResxValue trans to do ***";
        //throw new Exception(string.Format("File {0}, id={1}, xml={2}", fn, id, res.ToString()), exp);
      }
    }

    public static void LocalizeXmlLow(string fn, Langs lng, Stream str, bool removeForceTrans) {
      XElement el = LocalizeXml(fn, lng, removeForceTrans);
      //force xmlns:
      foreach (XAttribute attr in el.Attributes().Where(a => a.Name.Namespace == xmlns || a.Name.LocalName == "xmlns").ToArray())
        attr.Remove();
      el.Add(new XAttribute(xmlns + "lm", "lm"), new XAttribute("xmlns", "htmlPassivePage"));
      byte[] data = Encoding.UTF8.GetBytes(el.ToString(SaveOptions.DisableFormatting));
      str.Write(data, 0, data.Length);
    }

    public static Stream LocalizeXmlLow(string fn, Langs lng) { return LocalizeXmlLow(fn, lng, true); }

    static Stream LocalizeXmlLow(string fn, Langs lng, bool removeForceTrans) {
      Stream str = new MemoryStream();
      LocalizeXmlLow(fn, lng, str, removeForceTrans);
      return str;
    }

    public static Stream LocalizeXml_ModifyXml(string virtualFn, Action<XElement> dictModify) {
      Stream str = new MemoryStream();
      Langs lng = (Langs)Enum.Parse(typeof(Langs), Thread.CurrentThread.CurrentUICulture.Name.Replace('-', '_'), true);

      XElement el = LocalizeXml(virtualFn, lng, true);
      //force xmlns:
      foreach (XAttribute attr in el.Attributes().Where(a => a.Name.Namespace == xmlns || a.Name.LocalName == "xmlns").ToArray())
        attr.Remove();
      el.Add(new XAttribute(xmlns + "lm", "lm"), new XAttribute("xmlns", "htmlPassivePage"));
      if (dictModify != null) dictModify(el.Descendants().First(n => n.Name.LocalName == "body"));
      byte[] data = Encoding.UTF8.GetBytes(el.ToString(SaveOptions.DisableFormatting));
      str.Write(data, 0, data.Length);

      return str;
    }

    public static Stream LocalizeXml(string fn) {
      return LocalizeXmlLow(fn, (Langs)Enum.Parse(typeof(Langs), Thread.CurrentThread.CurrentUICulture.Name.Replace('-', '_'), true), true);
    }

    public static XElement LocalizeXml(string fn, Langs lang) { return LocalizeXml(fn, lang, true); }

    static XElement LocalizeXml(string virtualFn, Langs lang, bool removeForceTrans) {
      if (lang == Langs.sp_sp) lang = Langs.es_es;
      XElement root = fileToXml(virtualFn);
      //string dir = Path.GetDirectoryName(fn);
      //string name = Path.GetFileName(fn).Replace(".aspx.lmdata", null);
      string outFn = resxFileName(resxFileName(virtualFn), lang); // dir + @"\App_LocalResources\" + name + "." + lang.ToString().Replace('_', '-') + ".resx";
      //LM16
      //var cfg = NewEATradosLib.HackEx();
      //if (cfg != null && cfg.courseId != CourseIds.EnglishE && !File.Exists(outFn) && lang == Langs.cs_cz) {
      //  outFn = outFn.Replace("cs-cz.", "");
      //}

      if (File.Exists(outFn)) {
        //setUniqueIds(root); //jednoznacne ocislovani uzlu (je jedna ciselna rada pro kazdy tagName) 
        LocFileType fileType = getFileType(virtualFn);
        //Vyhod (**) zavorky
        string f = transFinal(File.ReadAllText(outFn));
        XElement res = XElement.Parse(f);
        foreach (XAttribute attr in transAttributes(root))
          attr.Value = getResxValue(getResxId(attr), res, outFn);// res.Descendants("value").Where(el => (string)el.Parent.Attribute("name") == id).Select(e => e.Value).Single();
        List<XNode> delNodes = new List<XNode>();
        //kazdy TRANS tag muze mit nekolik souvislych useku k prekladu 
        foreach (List<XNode> src in transFragments(root)) {
          //Preklad do XML fragmentu:
          string id = getResxId(src[0].Parent, fileType);
          XElement newNodes = XElement.Parse(@"<root xmlns=""" + html.NamespaceName + @""">" + getResxValue(id, res, outFn) + "</root>", LoadOptions.PreserveWhitespace);
          src[0].AddAfterSelf(newNodes.Nodes());
          delNodes.AddRange(src);
        }
        //}
        delNodes.Remove<XNode>();
      }
      //Vyhod trans, notrans tagy
      foreach (XElement el in root.Descendants().Where(el => el.Name == html + "trans" || el.Name == html + "notrans" || el.Name == html + "transhelp").ToArray()) {
        if (el.Nodes().Any())
          el.AddAfterSelf(el.Nodes());
        el.Remove();
      }
      //Vyhod transhelp tagy
      foreach (XElement el in root.Descendants().Where(el => el.Name == html + "transhelp"))
        el.Remove();
      //Vyhod force_trans attribute
      if (removeForceTrans) foreach (XAttribute attr in root.DescendantsAndSelf().SelectMany(el => el.Attributes("force_trans")))
          attr.Remove();
      //XXX1
      if (oldToNewTransform!=null) oldToNewTransform(root);
      return root;
    }

    public static Action<XElement> oldToNewTransform;
    public static Func<string> readLMDataFile;

    public class multiTrans {
      public multiTrans(int count, string txt) { Count = count; Txt = txt; }
      public int Count; public string Txt;
    }

    /// <summary>
    /// prejmenuje ASPX soubory do runtime nebo debug podoby 
    /// </summary>
    public static void RenameAspx(IEnumerable<LocPageGroup> grps, bool forRuntime) {
      foreach (string fn in getFiles(grps)) {
        if (getFileType(fn) != LocFileType.aspx) continue;
        string src = fn + ".transsrc";
        string trans = fn + ".trans";
        if (forRuntime) {
          if (!File.Exists(fn) || File.Exists(src) || !File.Exists(trans)) continue;
          File.Move(fn, src);
          File.Move(trans, fn);
        } else {
          if (!File.Exists(fn) || !File.Exists(src) || File.Exists(trans)) continue;
          File.Move(fn, trans);
          File.Move(src, fn);
        }
      }
    }

    static string normalizeLookupXml(string s) {
      if (string.IsNullOrEmpty(s)) return s;
      XElement root;
      try {
        root = XElement.Parse("<root>" + s + "</root>");
      } catch {
        root = XElement.Parse("<root>" + HtmlToXmlEntity.DeEntitize(s) + "</root>");
      }
      foreach (XText nd in root.DescendantNodes().Where(n => n.NodeType == XmlNodeType.Text).Cast<XText>().Where(t => !string.IsNullOrEmpty(t.Value)))
        nd.Value = HtmlToXmlEntity.NormalizeEntities(nd.Value);
      return root.InnerXml();

    }

    static string normalizeLookupXml2(string s, StringBuilder sb) {
      string res = normalizeXmlText(s, sb);
      if (res != s)
        return res;
      return res;
    }

    public static void NormalizeLookupXml() {
      StringBuilder sb = new StringBuilder();
      Trados.TradosDataContext db = Machines.getTradosContext();
      foreach (Trados.Lookup lkp in db.Lookups) {
        //foreach (Trados.Lookup lkp in db.Lookups.Where(l => (l.SrcText != null && l.SrcText.Contains("&")) || (l.TransText != null && l.TransText.Contains("&")))) {
        lkp.SrcText = normalizeLookupXml2(lkp.SrcText, sb);
        lkp.TransText = normalizeLookupXml2(lkp.TransText, sb);
        lkp.finish();
      }
      foreach (Trados.Sentence sent in db.Sentences) {
        sent.SrcText = normalizeLookupXml2(sent.SrcText, sb);
        sent.TransText = normalizeLookupXml2(sent.TransText, sb);
        sent.finish();
      }
      db.SubmitChanges();
    }

  }

  /// <summary>
  /// Definice odkud kam se preklada
  /// </summary>
  public class LocCfgTransDef {
    public Langs Src;
    public Langs Trans;
  }

  public enum BasicPathType {
    EA,
    lmcom,
    LMComLib,
    Rw,
    rewise,
    rwcourse,
  }

  /// <summary>
  /// File maska, definujici grupu stranek k prekladu
  /// </summary>
  public class LocCfgPageGroupFilter {
    /// <summary>
    /// Identifikace grupy
    /// </summary>
    public LocPageGroup Group;
    /// <summary>
    /// Typ adresare pro hledani RESX a LMDATA souboru
    /// </summary>
    public BasicPathType BasicPathType;
    /// <summary>
    /// Bacic path
    /// </summary>
    public string BasicPath {
      get {
        switch (BasicPathType) {
          case BasicPathType.EA: return TradosLib.fnStartNoSlash;
          case BasicPathType.LMComLib: return Machines.basicPath + @"rew\lmcomlib";
          case BasicPathType.rewise: return ConfigurationManager.AppSettings["trados-rew_school-dir"] ?? Machines.rootPath;
          case BasicPathType.rwcourse: return Machines.rootDir;
          //case BasicPathType.lmcom: return Machines.basicPath + @"lmcom";
          //case BasicPathType.Rw: return Machines.basicPath + @"rw2";
          default: throw new Exception("Missing code here!");
        }
        //return Machines.basicPath + @"rew\EduAuthorNew\";
        //if (Machines.machine == "xacer-pz")
        //  switch (BasicPathType) {
        //    case BasicPathType.EA: return @"q:\Trados";
        //    case BasicPathType.lmcom: return Machines.basicPath + @"lmcom";
        //    case BasicPathType.LMComLib: return Machines.basicPath + @"lmcomlib";
        //    case BasicPathType.Rw: return Machines.basicPath + @"rw2";
        //    case BasicPathType.rewise: return Machines.basicPath + @"rew\web4";
        //    default: throw new Exception("Missing code here!");
        //  } else
        //  switch (BasicPathType) {
        //    case BasicPathType.EA: return @"q:\LMNet2\WebApps\EduAuthorNew";
        //    case BasicPathType.lmcom: return Machines.basicPath + @"lmcom";
        //    case BasicPathType.LMComLib: return Machines.basicPath + @"lmcomlib";
        //    case BasicPathType.Rw: return Machines.basicPath + @"rw2";
        //    case BasicPathType.rewise: return Machines.basicPath + @"rew\web4";
        //    default: throw new Exception("Missing code here!");
        //  }
      }
    }
    public string GlobalResourcePath {
      get {
        return (BasicPath + @"\trados_globalresources\" + Group.ToString() + "cs.resx").ToLowerInvariant();
      }
    }
    public string GlobalResourcePathJS {
      get {
        return (BasicPath + @"\trados_globalresources\" + Group.ToString() + "js.resx").ToLowerInvariant();
      }
    }
    /// <summary>
    /// Maska do filesystemu vzhledem k BasicPath
    /// </summary>
    public string FileMask;
    /// <summary>
    /// Zdrojovy jazyk grupy
    /// </summary>
    public Langs PrimaryLang;
    /// <summary>
    /// speciální směr jazyku pro grupu
    /// </summary>
    public LocCfgTransDef[] TransDef;
    /// <summary>
    /// Pro grupu vrati zdrojovy jazyk k jazyku prekladu. Chyba je-li transLang=TransLang
    /// </summary>
    public Langs FindSrcLang(Langs transLang) {
      if (transLang == PrimaryLang) throw new Exception("Špatně vybarný jazyk: jazyk zdroje je roven jazyku překladu");
      LocCfgTransDef res = null;
      if (TransDef != null)
        res = TransDef.Where(d => d.Trans == transLang).FirstOrDefault();
      if (res != null) return res.Src;
      res = LocCfg.Instance().TransDef.Where(d => d.Trans == transLang).FirstOrDefault();
      if (res != null) return res.Src;
      throw new Exception("Missing TransDef item in LocCfg for lang: " + transLang.ToString());
    }
    /// <summary>
    /// Extensions pro filtry na souboru
    /// </summary>
    public string[] Extensions;
    /// <summary>
    /// Nazvy vyloucenych souboru
    /// </summary>
    public string[] Exclude;
    /// <summary>
    /// Nazvy vybranych souboru
    /// </summary>
    public string[] Include;
    /// <summary>
    /// Nazvy vybranych souboru, ktere se lokalizuje jen do nekterych jazyku
    /// </summary>
    public LocIncludeEx[] IncludeEx;

    public string fingLangFilter(string resxFile) {
      if (IncludeEx == null) return null;
      return IncludeEx.Where(ic => TradosLib.resxFileName(BasicPath + ic.FileName) == resxFile).Select(ic => ic.Langs).FirstOrDefault();
    }
  }

  public class LocIncludeEx {
    public string FileName;
    public string Langs;
    Langs[] ls;
    public Langs[] langs {
      get {
        if (ls == null) ls = Langs.Split(',').Select(s => (Langs)Enum.Parse(typeof(Langs), s)).ToArray();
        return ls;
      }
    }
  }

  /// <summary>
  /// Konfig
  /// </summary>
  public class LocCfg {
    /// <summary>
    /// Definice trid stranek
    /// </summary>
    public LocCfgPageGroupFilter[] Groups;
    /// <summary>
    /// Definice odkud kam se preklada
    /// </summary>
    public LocCfgTransDef[] TransDef;
    static LocCfg instance;
    public static void Refresh() {
      instance = null;
    }
    public static LocCfg Instance() {
      if (instance == null) {
        var fn = HostingEnvironment.ApplicationPhysicalPath + "app_data/Trados.xml";
        if (!File.Exists(fn)) fn = @"d:\LMCom\rew\Web4\App_Data\Trados.xml";
        instance = (LocCfg)XmlUtils.FileToObject(fn, typeof(LocCfg));
      }
      return instance;
    }

    public class GroupSrcLang {
      public static GroupSrcLang NullValue = new GroupSrcLang() { TransDef = new LocCfgTransDef() { Src = Langs.no } };
      public LocPageGroup Group;
      public LocCfgTransDef TransDef;
    }

    public IEnumerable<GroupSrcLang> SrcLangForDestLang(Langs transLang) {
      //return Enumerable.Empty <GroupSrcLang>();
      var res = Groups.Where(g => g.TransDef != null).
        Select(gs => gs.TransDef.Where(td => td.Trans == transLang).Select(td => new GroupSrcLang() { Group = gs.Group, TransDef = td }).SingleOrDefault()).
        Where(r => r != null).ToArray();
      var def = Groups.Select(gs => TransDef.Where(td => td.Trans == transLang).Select(td => new GroupSrcLang() { Group = gs.Group, TransDef = td }).SingleOrDefault()).
        Where(r => r != null);
      return res.Concat(def.Where(dg => !res.Any(rg => rg.Group == dg.Group)));
    }

    public IEnumerable<string> TransDefs() {
      return
        TransDef.Select(s => s.Src.ToString() + "=>" + s.Trans.ToString()).Concat(
          Groups.Where(g => g.TransDef != null).
            SelectMany(gs => gs.TransDef).
            Select(s => s.Src.ToString() + "=>" + s.Trans.ToString())
        ).Distinct();
    }
    public static void decodeLangs(string langs, out Langs srcLang, out Langs transLang) {
      string[] parts = langs.Split(new string[] { "=>" }, StringSplitOptions.RemoveEmptyEntries);
      srcLang = (Langs)Enum.Parse(typeof(Langs), parts[0]);
      transLang = (Langs)Enum.Parse(typeof(Langs), parts[1]);
    }
    /// <summary>
    /// Najde info o grupe
    /// </summary>
    public LocCfgPageGroupFilter findPageGroup(LocPageGroup group) {
      foreach (LocCfgPageGroupFilter grp in Groups)
        if (grp.Group == group) return grp;
      throw new Exception("Missing Group in LocCfg for group: " + group.ToString());
    }
  }

  public class TransBlock : PlaceHolder {

    string text;
    [Localizable(true)]
    public string Text { get { return text; } set { text = value; if (text != null) text = text.Replace("&nbsp;", "$nbsp;"); } }
    public bool TransVersion { get; set; }

    protected override void OnInit(EventArgs e) {
      base.OnInit(e);
      EnableViewState = false;
    }

    protected override void CreateChildControls() {
      try {
        base.CreateChildControls();
        if (!TransVersion) return;
        XElement root = XElement.Parse("<root>" + Text + "</root>");
        Dictionary<string, Control> ctrls = new Dictionary<string, Control>();
        foreach (Control ctrl in Controls)
          if (ctrl.ID != null) ctrls.Add(ctrl.ID.ToLowerInvariant(), ctrl); else continue;
        Controls.Clear();
        //modifikuj vyznacne tagy, nahrad SPAN tagy znackou
        foreach (XElement el in root.Descendants().Where(el => el.Name.LocalName == "span").ToArray()) {
          string id = el.Attribute("class").Value.ToLowerInvariant();
          Control ctrl;
          if (!ctrls.TryGetValue(id, out ctrl)) throw new Exception(string.Format("{2}.{0} tag ID in translation  1 '{1}')", id, Text, ID));
          //Control ctrl = ctrls[id];//Controls.Cast<Control>().Where(c => c.ID == id).First();
          //Uprav ASP kontrolky
          if (ctrl is PlaceHolder) {
          } else if (ctrl is HyperLink) {
            ((HyperLink)ctrl).Text = el.Value;
          } else if (ctrl is HtmlAnchor) {
            ctrl.Controls.Add(new LiteralControl(el.Value));
          }
          //nahrad SPAN znackou
          el.AddAfterSelf(new XText("###{" + id + "}###")); el.Remove();
        }
        //XML to string, rozdeleni dle znacek, doplneni LiteralControls
        string xml = root.InnerXml();
        foreach (regExItem reg in regExItem.Parse(xml, reg_marks)) {
          if (reg.IsMatch) {
            string id = reg.Value.Substring(4, reg.Value.Length - 8);
            Control ctrl;
            if (!ctrls.TryGetValue(id, out ctrl)) throw new Exception(string.Format("Missing {0} tag in translation 2 '{1}'", id, Text));
            Controls.Add(ctrl);
            ctrls.Remove(id);
          } else Controls.Add(new LiteralControl(reg.Value.Replace("$nbsp;", "&nbsp;")));
        }
        if (Text != "###TRANS TODO###")
          if (ctrls.Count > 0) throw new Exception(string.Format("Missing {0} tag in translation 3 '{1}'", ctrls.Keys.First(), Text));
      } catch (Exception exp) {
        throw new Exception(string.Format("Id={0}", ID), exp);
      }
    }
    static Regex reg_marks = new Regex("###{.*?}###");

    static IEnumerable<XElement> tags(XElement root, bool isOK) {
      return isOK ?
        root.Descendants().Where(e => e.Name.Namespace != TradosLib.html || !inlineTags.Contains(e.Name.LocalName)) :
        root.Descendants().Where(e => e.Name.Namespace == TradosLib.html && inlineTags.Contains(e.Name.LocalName));
    }
    static string[] inlineTags = new string[] { "b", "u", "i", "em", "br" };

    static string innerXml(string xml) {
      int begIdx = xml.IndexOf('>'); int endIdx = xml.LastIndexOf('<');
      return xml.Substring(begIdx + 1, endIdx - begIdx - 1);
    }

    static bool inPlaceHolder(XElement root, XNode nd) {
      while (nd != root) {
        if (nd is XElement && ((XElement)nd).Name.LocalName == "PlaceHolder") return true;
        nd = nd.Parent;
      }
      return false;
    }

    static string id(XElement el) {
      XAttribute attr = el.Attribute("ID");
      if (attr == null) attr = el.Attribute("id");
      return attr == null ? null : attr.Value.ToLower();
    }

    public static void setText(XElement root) {
      //Kontrola, zdali vsechny tagy obsahuji ID nebo id
      if (tags(root, true).Where(el => id(el) == null).Any())
        throw new Exception("Some <asp:???> tag in TransBlock has not ID: " + root.ToString());
      //Kopie TransBlock
      XElement textRoot = XElement.Parse(root.ToString()); textRoot.Add(new XAttribute("xmlns", "htmlPassivePage"));
      foreach (XElement el in textRoot.Descendants()) {
        XAttribute attr = el.Attribute("xmlns"); if (attr != null) attr.Remove();
      }
      //Nahrada tagu
      foreach (XElement asp in tags(textRoot, true).ToArray()) {
        XElement span = new XElement(TradosLib.html + "span", new XAttribute("class", id(asp)));
        asp.AddAfterSelf(span);
        asp.Remove();
        switch (asp.Name.LocalName) {
          case "PlaceHolder": break;
          case "HyperLink":
            span.Add(new XText(asp.Attribute("Text").Value));
            break;
          case "a":
            span.Add(new XText(asp.Value));
            break;
        }
      }
      //Ulozeni kopie do Text atributu
      root.Add(new XAttribute("Text", innerXml(textRoot.ToString())));
      //V puvodnim tagu: TransVersion=true
      root.Add(new XAttribute("TransVersion", "true"));
      //V puvodnim tagu: vymazani vsech text nodes:
      foreach (XText txt in root.DescendantNodes().Where(nd => nd is XText && !inPlaceHolder(root, nd)).ToArray())
        txt.Remove();
      //V puvodnim tagu: vymazani vsech inline tagu
      foreach (XElement asp in tags(root, false).Where(el => !inPlaceHolder(root, el)).ToArray()) {
        asp.AddAfterSelf(asp.Nodes()); asp.Remove();
      }
      //V puvodnim tagu: vymazani lokalizovanych atributu
      foreach (XElement asp in tags(root, true).ToArray()) {
        switch (asp.Name.LocalName) {
          case "HyperLink":
            asp.Attribute("Text").Remove();
            break;
        }
      }
    }

  }
}
