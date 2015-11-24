//http://www.computerhope.com/robocopy.htm
//robocopy s:\LMCom\SoundSources\ q:\LMCom\rew\SoundSources\ *.mp3 *.wav /s -XD src2 

using System;
using System.Collections.Generic;
//using System.RowType.Entity.Infrastructure;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Course;
using LMComLib;
using LMNetLib;
using Design;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using LMMedia;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using schools;

public class RwBooksMeta {
  public class link {
    [XmlAttribute]
    public string jsonId;
    [XmlAttribute]
    public int lesId;
  }
  [XmlAttribute]
  public string Name;
  [XmlAttribute]
  public short Id;
  [XmlAttribute]
  public LineIds LineId;
  public link[] courseLinks; //email lekci LM kurzu
}

public class RwBooksLoc {
  public short Id;
  public Langs Lang;
  public RwFactLoc[] Locs;
  public string AdminEMail;
  public string Trans;
  public string TransPerex;
}

public class RwFactLoc {
  public static RwFactLoc Create(int id, string src, string trans) { return new RwFactLoc() { Id = id, Trans = trans == null ? "?" : null, Src = src }; }
  public int Id;
  public string Src;
  public string Trans;
  public bool isEmpty() { return isEmpty(Trans); }
  static public bool isEmpty(string trans) { return string.IsNullOrEmpty(trans) || trans == "?"; }
}

//0000 knihy, 00.0000 lekce, 2.000.00.0000 fakty
//100 lekci na knihu, 2.000 faktu na lekci, charMax 10.000 knih
public static class RwIdManager {
  public static int getLessId(short bookId, short lessCnt) { return lessCnt * 10000 + bookId; }
  public static int getFactId(int lessId, short factCnt) { return factCnt * 1000000 + lessId; }
  public static short bookIdFromLessId(int lessId) { return (short)(lessId % 10000); }
  public static short bookIdFromFactId(int factId) { return (short)(factId / 1000000); }
  public static short lessIdFromFactId(int factId) { return (short)((factId / 10000) % 100); }
  public static bool isLessonId(Int64 id) { return id < 1000000 && id > 10000; }
  public static bool isBookId(Int64 id) { return id < 10000; }
  public static bool isFactId(Int64 id) { return id > 1000000; }

  public static string hashDir1(string name, byte mask = 0x7F) {
    var md5 = MD5.Create();
    return LowUtils.ByteToString(md5.ComputeHash(Encoding.UTF8.GetBytes(name)).Select(b => (byte)(b & mask)).Aggregate((r, i) => (byte)(r ^ i)));
  }
  public static string hashDir2(string name, byte mask = 0x7F) {
    var md5 = MD5.Create();
    byte[] single = md5.ComputeHash(Encoding.UTF8.GetBytes(name));
    return LowUtils.ByteToString(single.Take(8).Select(b => (byte)(b & mask)).Aggregate((r, i) => (byte)(r ^ i))) + "\\" + LowUtils.ByteToString(single.Skip(8).Select(b => (byte)(b & mask)).Aggregate((r, i) => (byte)(r ^ i)));
  }

}

public static class RwSound {

  public static void ListSoundFiles() {
    File.WriteAllLines(ss + "\\fileList.txt", Directory.EnumerateFiles(ss, "*", SearchOption.AllDirectories).Where(f => exts.IsMatch(f)).Select(f => f.Substring(ss.Length + 1)));
  } static string ss = Machines.basicPath + @"rew\SoundSources"; static Regex exts = new Regex(@"\.(wav|mp3)$");


  public static void CreateDirectory() {
    var root = File.ReadAllLines(ss + "\\fileList.txt").
      Where(f => f.EndsWith(".mp3")).
      Select(f => parseFile(f)).
      GroupBy(p => p.lng).
      Select(g => new lng() {
        lang = g.Key,
        items = g.
          GroupBy(i => i.w).
          Select(wg => new inf() {
            w = wg.Key,
            srcIds = wg.Select(wgi => wgi.srcId).ToArray()
          }).
          ToArray()
      });
    XmlUtils.ObjectToFile(ss + "\\words.xml", root.ToArray());
  }

  static hlp parseFile(string f) {
    string[] parts = f.Split('\\');
    f = parts.Last().Split('.')[0];
    return new hlp() { lng = LowUtils.EnumParse<Langs>(parts[0]), srcId = (SoundSrcId)(Convert.ToByte(parts[1].Last()) - Convert.ToByte('0')), w = Paths.DecodeFileNameToWord(f) };
  }
  public struct hlp {
    public Langs lng;
    public string w;
    public SoundSrcId srcId;
  }
  public struct inf {
    [XmlAttribute]
    public string w;
    [XmlAttribute]
    public SoundSrcId[] srcIds;
  }
  public struct lng {
    [XmlAttribute]
    public Langs lang;
    public inf[] items;
  }

  public static void MergeSounds() {
    var soundDict = SoundDict;
    Rewise.Book[] books = RwBooksDesign.getBooks().ToArray();
    StringBuilder sb = new StringBuilder();
    var rwWords = books.
      SelectMany(b => b.lessons().
        SelectMany(l => l.SrcFacts.
          SelectMany(f => GetSoundKeys(f.getSide(Langs.no).Text, sb)).
            Select(s => new {
              l = CommonLib.LineIdToLang(b.LineId),
              w = s,
              b = b.Title,
              les = l.Title,
            }))).
      ToArray();
    File.WriteAllLines(ss + @"\\missingSound.txt", rwWords.
      Where(rw => !soundDict[rw.l].ContainsKey(rw.w)).
      OrderBy(rw => rw.l).
      Select(rw => rw.b + "." + rw.les + "." + rw.l.ToString() + ": " + rw.w));
    //File.WriteAllLines(ss + @"\\missingSound.txt", rwWords.Where(rw => rw.l == Langs.ru_ru && rw.w.Contains(' ')).
    //  Where(rw => !soundDict[rw.l].ContainsKey(rw.w)).
    //  Select(rw => rw.w + "\t" + rw.w.Replace(" ", null)));
  }

  //public static SoundSrcId[][] getSoundSrc(string factText, Langs loc, StringBuilder sb) {
  //  var sd = soundDict[loc];
  //  return GetSound(factText, sb).Select (ft => getSoundSrc(sd, ft.Key)).ToArray();
  //}
  static SoundSrcId[] getSoundSrc(Dictionary<string, SoundSrcId[]> sd, string w) { SoundSrcId[] res; return sd.TryGetValue(w, out res) ? res : null; }

  static Dictionary<Langs, Dictionary<string, SoundSrcId[]>> SoundDict {
    get { return soundDict != null ? soundDict : soundDict = XmlUtils.FileToObject<RwSound.lng[]>(ss + "\\words.xml").ToDictionary(l => l.lang, l => l.items.ToDictionary(i => i.w, i => i.srcIds)); }
    set { soundDict = value; }
  } static Dictionary<Langs, Dictionary<string, SoundSrcId[]>> soundDict;


  //public struct KeyText { public string Key; public string Text; }

  public static char russianAccent = '\x301'; //rusky prizvuk, Convert.ToInt32(ch)==769 
  //public static char russianOSmall = '\x43E';
  //public static char russianOBig = '\x41E';
  //static Regex rxBracket = new Regex(@"\{.*?\}", RegexOptions.Singleline); //slozene zavorky, další varianta, stanou se kulatou zavorku. Pro zvuk se obsah maze.
  //static Regex rxComment = new Regex(@"\(.*?\)", RegexOptions.Singleline); //kulate zavorky, komentář. Pro zvuk se obsah maze.
  //static Regex rxOption = new Regex(@"\[.*?\]", RegexOptions.Singleline); //hranate zavorky, option, stanou se mezerou. Pro zvuk se obsah maze.
  //static Regex rxRussianAccent = new Regex(russianAccent, RegexOptions.Singleline); //rusky prizvuk

  static Regex rxSoundAll = new Regex(@"\[.*?\]|\(.*?\)|\{.*?\}|" + russianAccent, RegexOptions.Singleline);

  //static Regex rxSound = new Regex(@"\{%.*?%\}", RegexOptions.Singleline); //sound zavorky, asi poznamka pro mluvicho, maze se pro zvuk i zobrazeni.
  static Regex rxDoubleSpaces = new Regex(@"\s\s+", RegexOptions.Singleline); //zdvojene mezery, crlf a tab se nahradi mezerou

  static IEnumerable<string> GetSoundKeys(string srcText, StringBuilder sb) {
    if (string.IsNullOrEmpty(srcText)) yield break;
    foreach (string s in srcText.Split(';')) {
      //Snd
      //string snd = rxBracket.Replace(rxComment.Replace(rxOption.Replace(rxRussianAccent.Replace(s,""), ""), ""), "").ToLower();
      string snd = rxSoundAll.Replace(s, "").ToLower();
      sb.Length = 0; bool inBlank = false;
      foreach (char ch in snd)
        if (char.IsWhiteSpace(ch)) {
          if (!inBlank) sb.Append(ch);
          inBlank = true;
        } else if (char.IsLetterOrDigit(ch) || ch == '?' || ch == '\'') {
          sb.Append(ch);
          inBlank = false;
        } else if (inBlank) { //oddelovac, mezera jiz OK
        } else { //oddelovac, mezera jeste neni
          sb.Append(' ');
          inBlank = true;
        }
      yield return sb.ToString().Trim();
    }
  }

  static IEnumerable<string> GetSoundText(string srcText) {
    if (string.IsNullOrEmpty(srcText)) return Enumerable.Empty<string>();
    return srcText.Split(';').Select(txt => rxDoubleSpaces.Replace(txt, " ").Trim());
  }

  public static Rew.FactSound[] getFactText(string srcText, Langs loc, StringBuilder sb) {
    if (loc == Langs.no) return null;
    var sd = SoundDict[loc];
    return GetSoundKeys(srcText, sb).Zip(GetSoundText(srcText), (key, value) => sndInfo(key, value, getSoundSrc(sd, key))).ToArray();
  }

  static Rew.FactSound sndInfo(string key, string value, SoundSrcId[] srcIds) {
    if (srcIds == null) return new Rew.FactSound() { Text = value };
    return new Rew.FactSound() { Text = value, SndSrcs = srcIds };
  }

  public static Rew.FactSrc createFact(Rewise.SrcFact f, Langs srcLang, Langs loc, StringBuilder sb) {
    return new Rew.FactSrc() { DbId = f.Id, Type = f.Type, Question = getFactText(f.getAnswer(Langs.no), srcLang, sb), Answer = getFactText(f.getAnswer(loc), loc, sb) };
  }

}

public static class RwBooksDesign {

  static bool createMeta(Rewise.Book bk, ref RwBooksMeta res) {
    var less = bk.lessons().Where(l => !string.IsNullOrEmpty(l.jsonId)).Select(l => new RwBooksMeta.link() { jsonId = l.jsonId, lesId = l.Id }).ToArray();
    if (less.Length == 0) less = null;
    bool isCreated = res == null;
    if (isCreated) res = new RwBooksMeta();
    res.Id = bk.Id;
    res.Name = bk.Name;
    res.courseLinks = less;
    res.LineId = bk.LineId;
    return isCreated;
  }

  //naimportuje a upravi puvodni data z q:\lmcom\Rw\Rewise\App_Data\rewise\.
  public static void primaryImport() {

    Dictionary<string, RwBooksMeta> bookIds = File.Exists(metaPath) ? RWMeta.ToDictionary(bm => bm.Name, bm => bm) : new Dictionary<string, RwBooksMeta>();
    short lastId = bookIds.Values.Select(v => v.Id).DefaultIfEmpty().Max();
    XElement courses = XElement.Load(Machines.statisticDir + @"Statistic_CourseStructure.xml"); //pro provazani slovnicku s LM kurzy
    List<RwFactLoc> loc = new List<RwFactLoc>();
    //List<string> crsLevels = new List<string>(); //pro kontrolu
    foreach (Rewise.Book bk in BooksLib.Books(LineIds.no)) {
      short lessCnt = 1; loc.Clear();
      bk.Name = Path.GetFileNameWithoutExtension(bk.OrigFileName).ToLower();
      string levelId = bkNameToLevelId(bk.Name);
      XElement level = levelId == null ? null : courses.Descendants("level").FirstOrDefault(el => el.AttributeValue("spaceId") == levelId);
      bk.IsDefault = true;
      Langs srcLang = CommonLib.LineIdToLang(bk.LineId);

      //metadata
      RwBooksMeta meta;
      //Book.compId
      if (bookIds.TryGetValue(bk.Name, out meta)) bk.Id = meta.Id; else { lastId++; bk.Id = lastId; }

      foreach (var l in bk.lessons()) {
        short factCnt = 1;
        l.Id = RwIdManager.getLessId(bk.Id, lessCnt++);
        loc.Add(RwFactLoc.Create(l.Id, l.Title, null));
        //nalezeni jsonId kurzu
        if (level != null) {
          var ln = level.Elements().SingleOrDefault(el => el.AttributeValue("title").ToLower() == l.Title.ToLower());
          if (ln == null) { } //crsLevels.Add(email + ":" + l.Title);
          else l.jsonId = LowUtils.JSONToId(ln.AttributeValue("spaceId"), ln.AttributeValue("globalId"));
        }
        foreach (var f in l.SrcFacts) {
          f.Id = RwIdManager.getFactId(l.Id, factCnt++);
          foreach (var s in f.Answer) {
            s.Text = rxCommentSoundBracket.Replace(s.Text.Trim(), "");
            if (s.Lang == Langs.no && srcLang == Langs.ru_ru || s.Lang == Langs.ru_ru) s.Text = DictLib.normalizeRussian(s.Text); //nahrada propName ruskym propName
          }
          f.Type = f.IsPhrase ? FactTypes.phrase : FactTypes.word;
          loc.Add(RwFactLoc.Create(f.Id, f.getAnswer(Langs.no), null));
        }
      }

      //aktualizuj metainformaci 
      if (createMeta(bk, ref meta)) bookIds.Add(bk.Name, meta); //nova metainformace => pridej do kolekce

      XmlUtils.ObjectToFile(textsPath + bk.Name + ".xml", bk);
      var lfn = destPath + @"Texts\" + bk.Name + @"\no.xml";
      LowUtils.AdjustFileDir(lfn);
      XmlUtils.ObjectToFile(lfn, new RwBooksLoc() { Id = bk.Id, Locs = loc.ToArray(), Lang = Langs.no, AdminEMail = "?", Trans = "?", TransPerex = "?" });
    }
    //var notLess = crsLevels.Aggregate((r, i) => r + "; " + i); //seznam rewise lekci, co se nenasly v kurzu
    RWMeta = bookIds.Values.ToArray();
  }
  static Regex rxCommentSoundBracket = new Regex(@"\{%.*?%\}", RegexOptions.Singleline); //asi poznamky pro mluvciho
  static RwBooksMeta[] RWMeta { get { return rwMeta == null ? rwMeta = XmlUtils.FileToObject<RwBooksMeta[]>(metaPath) : rwMeta; } set { rwMeta = value; XmlUtils.ObjectToFile(metaPath, value); } } static RwBooksMeta[] rwMeta;
  static string destPath = Machines.rootPath + @"RwBooks\";
  static string textsPath = destPath + @"Texts\";
  static string metaPath = destPath + "BookMeta.xml";
  static string courseRewise = Machines.rootPath + @"Schools\EARewise\";
  static string rewFileName(Langs lng, string fn) { return string.Format(Machines.rootPath + @"RwBooks\Runtime\{0}\{1}", lng.ToString().Replace('_', '-'), fn); }
  const double locRatio = 0.7; //pomer, od ktereho se povazuje kniha za lokalizovanou

  //odvozeni space email pro course level z jmena rewise slovnicku
  static string bkNameToLevelId(string name) {
    var bk = name.Substring(0, name.Length - 1); string lev; int numInc = 0;
    switch (bk) {
      case "hue": lev = "english"; numInc = 1; break; //anglictina cislovana od 0
      case "hum": lev = "spanish"; break;
      case "huf": lev = "french"; break;
      case "hui": lev = "italian"; break;
      case "hut": lev = "german"; break;
      case "russian": lev = "russian"; break;
      default: return null;
    }
    return lev + (int.Parse(name.Substring(name.Length - 1)) + numInc).ToString();
  }

  static string lessDir(int lessId) {
    return RwIdManager.hashDir1(lessId.ToString(), 0x3f);//RwIdManager.bookIdFromLessId(lessId).ToString();
  }

  public static IEnumerable<Rewise.Book> getBooks() {
    return RWMeta.Select(b => XmlUtils.FileToObject<Rewise.Book>(textsPath + b.Name + ".xml"));
  }
  public static void ExportJson() {

    var books = getBooks().ToArray();
    StringBuilder sb = new StringBuilder();
    //new Dictionary<string, Crs2RwMapItem>();

    //Not localized lessons
    foreach (var b in books) {
      foreach (var les in b.lessons()) {
        Rew.LessonDataSrc res = new Rew.LessonDataSrc() {
          DbId = les.Id,
          Facts = les.SrcFacts.Select(f => RwSound.createFact(f, CommonLib.LineIdToLang(b.LineId), Langs.no, sb)).ToArray()
        };
        writeFile(res, rewFileName(Langs.no, string.Format("{0}\\{1}", lessDir(les.Id), les.Id)), false);
      }
    }

    Parallel.ForEach(new Langs[] { Langs.cs_cz } /*CommonLib.rewiseLangs*/, loc => {

      //Rewise index
      var locSrc = new Rew.LocSrc() {
        Loc = loc,
        Lines = books.GroupBy(b => b.LineId).Select(lineGrp => new Rew.LineSrc() {
          Line = lineGrp.Key,
          Groups = lineGrp.GroupBy(g => g.Group).OrderBy(g => g.First().Order).Select(groupGrp => new Rew.BookGroupSrc() {
            Id = groupGrp.Select(g => g.Id).First(),
            Title = groupGrp.Key,
            Licence = (Rw.CreativeCommonLic)groupGrp.First().Licence,
            AdminEMail = groupGrp.Select(g => g.AdminEMail).FirstOrDefault(em => !string.IsNullOrEmpty(em)),
            //Company = groupGrp.Select(g => g.).FirstOrDefault(em => !string.IsNullOrEmpty(em)),
            Books = groupGrp.OrderBy(bk => bk.Order).Select(bk => new Rew.BookSrc() {
              DbId = bk.Id,
              Title = bk.getLocTitle(loc),
              Perex = bk.getLocPerex(loc),
              LocAdminEMail = bk.getLocAminEMail(loc),
              //Name = bk.book.Project.FileName + "/" + bk.book.FileName,
              Lessons = bk.lessons().Select(l => new Rew.LessonSrc() {
                DbId = l.Id,
                Title = l.getLocTitle(loc),
                Words = l.SrcFacts.Where(f => f.Type == FactTypes.word).Count(),
                Phrases = l.SrcFacts.Where(f => f.Type == FactTypes.phrase).Count(),
                LocCount = l.SrcFacts.Where(f => f.getSide(loc) != null).Count(),
                RewiseSrcFacts = l.SrcFacts,
              }).ToArray()
            }).ToArray()
          }).ToArray()
        }).ToArray()
      };
      //course to rewise map
      Dictionary<string, Rew.Crs2RwMapItem> crs2RwMap = RWMeta.Where(ln => ln.courseLinks != null).SelectMany(ln => ln.courseLinks).ToDictionary(cl => cl.jsonId, cl => new Rew.Crs2RwMapItem() {
        rwId = cl.lesId,
        locRatioPromile = 0,
      });

      //Counts, uvnitr cyklu se vytvori lesson soubory
      foreach (var ln in locSrc.Lines) {
        Langs srcLang = CommonLib.LineIdToLang(ln.Line);
        foreach (var grp in ln.Groups) {
          foreach (var bk in grp.Books) {
            bk.Words = bk.Lessons.Sum(l => l.Words);
            bk.Phrases = bk.Lessons.Sum(l => l.Phrases);
            bk.LocCount = bk.Lessons.Sum(l => l.LocCount);
            //rewise lesson
            foreach (var les in bk.Lessons) {
              if (les.LocCount <= 0) continue; //lekce neni lokalizovana
              //dopln info propName lokalizaci do crs2Rw Map.
              var crw = crs2RwMap.Values.FirstOrDefault(cm => cm.rwId == les.DbId); if (crw != null) crw.locRatioPromile = (int)(les.LocCount * 1000 / (les.Phrases + les.Words));
              //lekce
              Rew.LessonDataSrc res = new Rew.LessonDataSrc() {
                DbId = les.DbId,
                Facts = ((Rewise.SrcFact[])les.RewiseSrcFacts).Select(f => RwSound.createFact(f, srcLang, loc, sb)).ToArray()
              };
              writeFile(res, rewFileName(loc, string.Format("{0}\\{1}", lessDir(les.DbId), les.DbId)), false);
            }
          }
          grp.Words = grp.Books.Sum(l => l.Words);
          grp.Phrases = grp.Books.Sum(l => l.Phrases);
          grp.LocCount = grp.Books.Sum(l => l.LocCount);
        }
        ln.Words = ln.Groups.Sum(l => l.Words);
        ln.Phrases = ln.Groups.Sum(l => l.Phrases);
        ln.LocCount = ln.Groups.Sum(l => l.LocCount);
      }
      writeFile(locSrc, rewFileName(loc, "index"), true);
      writeFile(crs2RwMap, rewFileName(loc, "crs2RwMap"), true);

    });

  }

  public static void mergeLocalizations() {
    var locs = RWMeta.
      Where(m => Directory.Exists(textsPath + m.Name)).
      SelectMany(m => Directory.GetFiles(textsPath + m.Name)).
      Where(f => Path.GetFileName(f).Contains("-")).
      Select(f => XmlUtils.FileToObject<RwBooksLoc>(f)).
      GroupBy(l => RWMeta.First(m => m.Id == l.Id).Name).
      ToArray();
    foreach (var lg in locs) {
      var fn = textsPath + lg.Key + ".xml";
      var bk = XmlUtils.FileToObject<Rewise.Book>(fn);
      var changed = false;
      foreach (var loc in lg) mergeLoc(bk, loc, ref changed);
      if (changed) XmlUtils.ObjectToFile(fn, bk);
    }
  }

  static void mergeLoc(Rewise.Book bk, RwBooksLoc loc, ref bool changed) {
    if (!RwFactLoc.isEmpty(loc.Trans) || !RwFactLoc.isEmpty(loc.TransPerex) || !RwFactLoc.isEmpty(loc.AdminEMail)) {
      Rewise.LocaleBook lb = bk.adjustLoc(loc.Lang);
      if (lb.Text != loc.Trans) { changed = true; lb.Text = loc.Trans; }
      if (lb.Perex != loc.TransPerex) { changed = true; lb.Perex = loc.TransPerex; }
      if (lb.AdminEMail != loc.AdminEMail) { changed = true; lb.AdminEMail = loc.AdminEMail; }
    }
    foreach (var l in loc.Locs.Where(l => !l.isEmpty())) {
      if (RwIdManager.isFactId(l.Id)) {
        var side = bk.findFact(l.Id).adjustSide(loc.Lang); if (side.Text != l.Trans) { changed = true; side.Text = l.Trans; }
      } else if (RwIdManager.isLessonId(l.Id)) {
        var lc = bk.findLess((int)l.Id).adjustLoc(loc.Lang); if (lc.Text != l.Trans) { changed = true; lc.Text = l.Trans; }
      }
    }
  }

  public static void writeFile(Object obj, string fnNoExt, bool isIndex) {
    LowUtils.AdjustFileDir(fnNoExt);
    string json = JsonConvert.SerializeObject(obj, isIndex ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None);
    if (isIndex) File.WriteAllText(fnNoExt + ".json", json);
    json = ClearScript.Lib.JSON2RJSON(json);
    //string crc = ZipStream.Crc(Encoding.UTF8.GetBytes(json)).ToString();
    File.WriteAllText(fnNoExt + ".rjson", json);
    //if (isIndex) File.WriteAllText(fnNoExt + ".rjson.crc", crc);
    //using (Stream msOut = File.OpenWrite(fnNoExt + ".rjson.gzip")) {
    //  using (GZipStream gzip = new GZipStream(msOut, CompressionMode.Compress))
    //  using (StreamWriter wrg = new StreamWriter(gzip))
    //    wrg.Write(json);
    //}
    //return crc;
  }
}
