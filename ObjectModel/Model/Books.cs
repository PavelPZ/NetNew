using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
#if SILVERLIGHT
using System.Windows;
#endif
using System.Xml.Linq;
using System.Xml.Serialization;
using LMComLib;
using LMNetLib;
using System.ComponentModel;

namespace Rewise {

  public interface IParented {
    IParented getParent();
    IEnumerable<IParented> getChilds();
  }

  public static class Parented {

    static IEnumerable<IParented> getParents(IParented src) {
      while (src != null) { src = src.getParent(); if (src != null) yield return src; }
    }

    public static T getParent<T>(IParented src) {
      return getParents(src).OfType<T>().FirstOrDefault();
    }

    public static IEnumerable<T> Nodes<T>(IParented src) {
      if (src is T) yield return (T)src;
      foreach (IParented p in src.getChilds()) foreach (T nd in Nodes<T>(p)) yield return nd;
    }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class LineRoot : Folder {

    public DateTime LastRefresh;

    public LineIds Line;

    public static Folder Build(LineIds line, IEnumerable<Book> books) {
      Folder res = new LineRoot() { Line = line, Title = line.ToString(), Folders = books.ToArray() };
      return res.Folders.Length == 1 ? res.Folders[0] : res;
    }
#if SILVERLIGHT
    public static LineRoot Load(string fn) { using (Stream str = Fil.er.OpenRead(fn)) return XmlUtils.StreamToObject<LineRoot>(str); }
    public void Save(string fn) { using (Stream str = Fil.er.OpenWrite(fn)) XmlUtils.ObjectToStream(this, str); }
#endif
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public class Locale {
    public Langs Lang;
    public string Text;
    public string TextSrc;
    public static string getLocTitle(Langs lng, IEnumerable<Locale> locs, string def = null) {
      Locale res = getLoc(lng, locs); return res == null ? def : res.Text;
    }
    public static Locale getLoc(Langs lng, IEnumerable<Locale> locs) {
      return locs == null ? null : locs.FirstOrDefault(l => l.Lang == lng);
    }
    public static T adjustLoc<T>(Langs lng, ref T[] locs) where T : Locale {
      T res = locs == null ? null : locs.FirstOrDefault(l => l.Lang == lng);
      if (res == null) { res = (T)typeof(T).GetConstructor(Type.EmptyTypes).Invoke(null); if (locs == null) locs = new T[] { res }; else { Array.Resize<T>(ref locs, locs.Length + 1); locs[locs.Length - 1] = res; } }
      return res;
    }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public class LocaleBook : Locale {
    public string AdminEMail;
    public string Perex;
    //public bool? IsDefault; //kniha v dane lokalizaci je dulezita a objevuje se na rewise home po zalozeni rewise
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class Folder {
    [XmlArrayItem(typeof(Folder))]
    [XmlArrayItem(typeof(Lesson))]
    [XmlArrayItem(typeof(Book))]
    [XmlArrayItem(typeof(LineRoot))]
    public Folder[] Folders;

    public string Title;
    [XmlArrayItem(typeof(Locale))]
    [XmlArrayItem(typeof(LocaleBook))]
    public Locale[] LocTitle;
    public string getLocTitle(Langs lng) { return Locale.getLocTitle(lng, LocTitle, Title); }
    public Locale getLoc(Langs lng) { return Locale.getLoc(lng, LocTitle); }
    public Locale adjustLoc(Langs lng) { return Locale.adjustLoc(lng, ref LocTitle); }

    public static IEnumerable<T> Descendants<T>(Folder root) where T : Folder {
      return Descendants(root).OfType<T>();
    }

    public static IEnumerable<Folder> Descendants(Folder root) {
      if (root == null || root.Folders == null) yield break;
      foreach (Folder fld in root.Folders) {
        yield return fld;
        foreach (Folder subFld in Descendants(fld)) yield return subFld;
      }
    }

    public static void setParent(object root) {
      if (root is Lesson) {
        Lesson rootLes = (Lesson)root;
        if (rootLes.SrcFacts != null) foreach (SrcFact fact in rootLes.SrcFacts) { fact.Parent = rootLes; setParent(fact); }
      } else if (root is Folder) {
        Folder rootFld = (Folder)root;
        if (rootFld.Folders != null) foreach (Folder fld in rootFld.Folders) { fld.Parent = rootFld; setParent(fld); }
      } else if (root is SrcFact) {
        SrcFact rootFact = (SrcFact)root;
        if (rootFact.Answer != null) foreach (FactSide side in rootFact.Answer) { side.MySrcFact = rootFact; setParent(side); }
      }
    }

    [XmlIgnore]
    public Folder Parent;

  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class Lesson : Folder {

    //public short Order;
    public int Id;

    public SrcFact[] SrcFacts { get { return srcFacts == null ? emptySrcFacts : srcFacts; } set { srcFacts = value; } }

    static SrcFact[] emptySrcFacts = new SrcFact[0];
    SrcFact[] srcFacts;

    public string jsonId; //identifikace odpovidajici lekci kurzu, napr. english1_xl01_shome_dhtm v q:\LMCom\rew\Web4\Schools\EACourses\English_0_1.json

  }

  public delegate Book ExpandProxyEvent(Book proxy);

  //public enum LicenceIds {
  //  unknown,
  //  cc_by,
  //  cc_by_sa,
  //  cc_by_nd,
  //  cc_by_nc,
  //  cc_by_nc_sa,
  //  cc_by_nc_nd,
  //}

  public enum DataType {
    unknown,
    vocabulary,
    vocabularyBuilder,
    dialog,
    soundFile,
  }

  [Flags]
  public enum Criteria {
    euroTalk = 0x1,
    lm = 0x2,
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial  class Book : Folder {

    public const string ForCPV = "4fa29a5168a14f619051b559b2c4664e";
    public const string ForRewise = "b1b88d032f4e4788b913bba8c1a24be";

    public short Id;
    public Guid BookId;
    public Criteria Criteria;
    //public string AuthorId;
    public string Perex;
    //public new LocaleBook[] LocTitle;
    //public IEnumerable<LocaleBook> locs { get { return LocTitle.Cast<LocaleBook>(); } }
    //public Locale[] LocPerex;
    public XElement HtmlPerex;
    public string Descr;
    public XElement HtmlDescr;
    public LineIds LineId;
    public Rw.CreativeCommonLic Licence;
    public DataType Type;
    public string AdminEMail;
    public string OrigFileName;
    public LineIds DefaultLocalize; //difotni lokalizace
    public short Order;
    public bool IsDefault; //kniha v dane lokalizaci je dulezita a objevuje se na rewise home po zalozeni rewise

    //pro novou verzi Rewise a design time - lokalni Id
    [XmlIgnore]
    public ushort localId;

    public IEnumerable<SrcFact> getFacts() { return Descendants<Lesson>(this).SelectMany(l => l.SrcFacts); }
    public SrcFact findFact(Int64 id) { return getFacts().First(f => f.Id == id); }
    public Lesson findLess(int id) { return lessons().First(l => l.Id == id); }
    //public int Words { get { return words > 0 ? words : words = getFacts().Where(f => !f.IsPhrase).Count(); } set { words = value; } } int words;
    //public int Phrases { get { return phrases > 0 ? phrases : phrases = getFacts().Where(f => f.IsPhrase).Count(); } set { phrases = value; } } int phrases;

    //public string getLocPerex(Langs lng) { return Locale.getLocTitle(lng, LocPerex, Perex); }
    //public LocaleBook adjustLoc(Langs lng) { return Locale.adjustLoc(lng, ref LocTitle); }
    //public string getLocAminEMail(Langs lng) { var l = Locale.getLoc(lng, LocTitle); if (l == null || string.IsNullOrEmpty(l.AdminEMail)) return AdminEMail; return l.AdminEMail; }
    //public double locRatio(Langs lng) { return getFacts().Where(f => f.findSide(lng)!=null).Count() / getFacts().Count(); }
    public string getLocPerex(Langs loc) { LocaleBook lb = (LocaleBook) getLoc(loc); return lb == null ? Perex : lb.Perex; }
    public string getLocAminEMail(Langs loc) { LocaleBook lb = (LocaleBook)getLoc(loc); return lb == null ? AdminEMail : lb.AdminEMail; }
    public new LocaleBook adjustLoc(Langs loc) { LocaleBook[] lb = (LocaleBook[]) LocTitle;      try { return Locale.adjustLoc(loc, ref lb); } finally { LocTitle = lb; } }


#if !SILVERLIGHT
    public string Group;
#endif

    public string Name;
    //[XmlIgnore]
    //public string Name { get { return Path.GetFileNameWithoutExtension(OrigFileName).ToLower(); } }

    public static string lineSignature(LineIds line) { return AppConfig.Config.Element("lineIds").Element(line.ToString()).Value; }

    public XDocument ToXHtml_() {
      XElement perex, descr;
      if (HtmlPerex != null) { LMComLib.XExtension.ChangeNamespace(HtmlPerex, CommonLib.OLIhtml, CommonLib.html); perex = HtmlPerex; } else perex = new XElement(CommonLib.html + "div", Perex);
      if (HtmlDescr != null) { LMComLib.XExtension.ChangeNamespace(HtmlDescr, CommonLib.OLIhtml, CommonLib.html); descr = HtmlDescr; } else descr = new XElement(CommonLib.html + "div", Descr);
      return LowUtils.XHTML(Title,
        new object[] { 
          meta("Search.OLILine", lineSignature(LineId)),
          meta("Search.OLIType", ForCPV),
          meta("Search.OLILicence", Licence.ToString()),
          meta("OLI.BookId", BookId.ToString().ToLower()),
          meta("OLI.LineId", LineId.ToString()),
          meta("OLI.AdminEMail", AdminEMail),
          meta("OLI.Words", Folder.Descendants<Lesson>(this).SelectMany(l => l.SrcFacts).Where(fc => !fc.IsPhrase).Count().ToString()),
          meta("OLI.Phrases", Folder.Descendants<Lesson>(this).SelectMany(l => l.SrcFacts).Where(fc => fc.IsPhrase).Count().ToString()),
          meta("OLI.LocalizedTo", Folder.Descendants<Lesson>(this).First().SrcFacts[0].Answer.Where(a => a.Lang!=Langs.no).Select(a => a.Lang.ToString().Replace('_','-')).Aggregate((r,i) => r + " " + i)),
        },
        new object[] { 
          new XElement(CommonLib.html + "h1", Title),
          perex,
          descr
        });
    }

    public XDocument ToXHtml() {
      XElement perex, descr;
      if (HtmlPerex != null) { LMComLib.XExtension.ChangeNamespace(HtmlPerex, CommonLib.OLIhtml, CommonLib.html); perex = HtmlPerex; } else perex = new XElement(CommonLib.html + "div", Perex);
      if (HtmlDescr != null) { LMComLib.XExtension.ChangeNamespace(HtmlDescr, CommonLib.OLIhtml, CommonLib.html); descr = HtmlDescr; } else descr = new XElement(CommonLib.html + "div", Descr);
      return LowUtils.XHTML(Title + " (* " + lineSignature(LineId) + " " + ForCPV + " " + Licence.ToString() + " *)",
        new object[] { 
          meta("Search.OLILine", lineSignature(LineId)),
          meta("Search.OLIType", ForCPV),
          meta("Search.OLILicence", Licence.ToString()),
          meta("OLI.BookId", BookId.ToString().ToLower()),
          meta("OLI.LineId", LineId.ToString()),
          meta("OLI.AdminEMail", AdminEMail),
          meta("OLI.Words", Folder.Descendants<Lesson>(this).SelectMany(l => l.SrcFacts).Where(fc => !fc.IsPhrase).Count().ToString()),
          meta("OLI.Phrases", Folder.Descendants<Lesson>(this).SelectMany(l => l.SrcFacts).Where(fc => fc.IsPhrase).Count().ToString()),
          meta("OLI.LocalizedTo", Folder.Descendants<Lesson>(this).First().SrcFacts[0].Answer.Where(a => a.Lang!=Langs.no).Select(a => a.Lang.ToString().Replace('_','-')).Aggregate((r,i) => r + " " + i)),
        },
        new object[] { 
          new XElement(CommonLib.html + "h1", Title),
          perex,
          descr
        });
    }

    XElement meta(string name, string value) { return new XElement(CommonLib.html + "meta", new XAttribute("name", name), new XAttribute("content", value ?? string.Empty)); }

    //public static Book FromZip(Stream str) {
    //  using (ZipOutStream zipStr = new ZipOutStream(str))
    //  using (MemoryStream ms = new MemoryStream()) {
    //    zipStr.Decompress(zipStr.Files().First(), ms);
    //    byte[] buf = ms.GetBuffer();
    //    string s = Encoding.UTF8.GetString(buf, 0, buf.Length);
    //    ms.Seek(0, SeekOrigin.Begin);
    //    return Book.Load(ms);
    //  }
    //}

    static string licenceUrl(Rw.CreativeCommonLic lic) {
      switch (lic) {
        case Rw.CreativeCommonLic.cc_by: return "http://creativecommons.org/licenses/by/3.0/";
        case Rw.CreativeCommonLic.cc_by_nc: return "http://creativecommons.org/licenses/by-nc/3.0/";
        case Rw.CreativeCommonLic.cc_by_nc_nd: return "http://creativecommons.org/licenses/by-nc-nd/3.0/";
        case Rw.CreativeCommonLic.cc_by_nc_sa: return "http://creativecommons.org/licenses/by-nc-sa/3.0/";
        case Rw.CreativeCommonLic.cc_by_nd: return "http://creativecommons.org/licenses/by-nd/3.0/";
        case Rw.CreativeCommonLic.cc_by_sa: return "http://creativecommons.org/licenses/by-sa/3.0/";
        default: return null;
      }
    }

    static string licenceTitle(Rw.CreativeCommonLic lic) {
      switch (lic) {
        case Rw.CreativeCommonLic.cc_by: return "Attribution";
        case Rw.CreativeCommonLic.cc_by_nc: return "Attribution Non-Commercial";
        case Rw.CreativeCommonLic.cc_by_nc_nd: return "Attribution Non-Commercial No Derivatives";
        case Rw.CreativeCommonLic.cc_by_nc_sa: return "Attribution Non-Commercial Share Alike";
        case Rw.CreativeCommonLic.cc_by_nd: return "Attribution No Derivatives";
        case Rw.CreativeCommonLic.cc_by_sa: return "Attribution Share Alike";
        default: return null;
      }
    }


    public static Book Load(Stream str) {
      Book res = XmlUtils.StreamToObject<Book>(str); Folder.setParent(res);
      return res;
    }
    public void Save() { Save(OrigFileName); }
    public byte[] SaveToBytes() { return XmlUtils.ObjectToBytes(this); }
    public IEnumerable<Lesson> lessons() { return Descendants(this).OfType<Lesson>(); }

#if SILVERLIGHT
    public void Save(string fn) {
      using (Stream str = Fil.er.OpenWrite(fn)) XmlUtils.ObjectToStream(this, str);
    }
    public static Book Load(string fileName) { using (Stream str = Fil.er.OpenRead(fileName)) return Load(str); }
#else
    public void Save(string fileName) { XmlUtils.ObjectToFile(fileName, this); }
    public static Book Load(string fileName) {
      Book res = XmlUtils.FileToObject<Book>(fileName); res.OrigFileName = fileName; Folder.setParent(res); return res;
    }
#endif

    public IEnumerable<FactSound> Sounds() { return Facts().Select(f => f.findSide(Langs.no)).SelectMany(s => s.Sounds); }

    public IEnumerable<SrcFact> Facts() {
      StringBuilder sb = new StringBuilder();
      return lessons().SelectMany(l => l.SrcFacts);
    }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class SrcFact {
    public bool IsPhrase;
    public FactTypes Type;
    public int Id;
    public FactSide[] Answer;

    [XmlIgnore]
    public Lesson Parent;

    public string findAnswer(Langs lang) {
      return getSide(lang).Text;
    }
    public string getAnswer(Langs lang) {
      var s = getSide(lang);
      return s==null ? null : s.Text;
    }
    public FactSide findSide(Langs lang) {
      return Answer.Where(a => a.Lang == lang).First();
    }
    public FactSide getSide(Langs lang) {
      return Answer.Where(a => a.Lang == lang).FirstOrDefault();
    }
    public FactSide adjustSide(Langs lang) {
      FactSide res = Answer.Where(a => a.Lang == lang).FirstOrDefault();
      if (res == null) { res = new FactSide() { Lang = lang }; Array.Resize<FactSide>(ref Answer, Answer.Length + 1); Answer[Answer.Length - 1] = res; }
      return res;
    }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class FactSide {
    public string Text;
    public string TextSrc;
    [DefaultValue(Langs.no)]
    public Langs Lang;

    [XmlIgnore]
    public SrcFact MySrcFact;

    [XmlIgnore]
    public FactSound[] Sounds { get { return sounds == null ? sounds = GetSound(sb).ToArray() : sounds; } } FactSound[] sounds; static StringBuilder sb = new StringBuilder();

    IEnumerable<FactSound> GetSound(StringBuilder sb) { return LMMedia.Paths.GetSound(Text, sb).Select(kt => new FactSound() { Key = kt.Key, Text = kt.Text, MyFactSide = this }); }
  }

  public partial class FactSound {
    public FactSide MyFactSide;
    public string Key;
    public string Text;
  }

}
