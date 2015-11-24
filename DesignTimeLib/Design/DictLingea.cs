using Course;
using LMComLib;
using LMNetLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using schools;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
//using System.RowType.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
//XX
//using Translator;

namespace Admin {

  public class ByHand {
    [XmlAttribute]
    public string entryId;
    [XmlAttribute]
    public Langs okCrs;
    public string id() { return entryId; }
  }

  public enum DictEntryCmdType {
    loadDict,
    saveEntry,
    statistics
  }
  public class DictEntryCmd {
    public DictEntryCmdType type;
    public Langs crsLang;
    public Langs natLang;
    public string entryId;
    public string soundMaster;
    public string html;
    public Langs okCrs;
    public Langs okCrsMaybe;
    public int todoCount;
    public int allCount;
    public string id() { return entryId; }
  }

  public class DictEntryRes {
    public DictEntryCmd[] entries;
  }

  public class dictLingea {

    static Dictionary<Langs, Dictionary<Langs, LingeaDictionary.LingeaDictFile>> entries;
    static LingeaDictionary.DictEntry[] dictEntries;

    public static Admin.DictEntryRes processCommand(Admin.DictEntryCmd cmd) {
      Func<Langs, Langs, IEnumerable<ByHand>> getHands = (crsL, natL) => {
        var fn = Machines.rootPath + string.Format(@"RwDicts\Sources\LingeaOld\design\byHand_{0}_{1}.xml", crsL, natL);
        IEnumerable<string> fns = File.Exists(fn) ? XExtension.Create(fn) : Directory.EnumerateFiles(Machines.rootPath + @"RwDicts\Sources\LingeaOld\design", string.Format("byHand_{0}_{1}*.xml", crsL, natL));
        return fns.SelectMany(f => XmlUtils.FileToObject<List<ByHand>>(f));
      };
      var hands = getHands(cmd.crsLang, cmd.natLang).ToDictionary(bh => bh.id(), bh => bh);
      var handFn = Machines.rootPath + string.Format(@"RwDicts\Sources\LingeaOld\design\byHand_{0}_{1}.xml", cmd.crsLang, cmd.natLang);
      ByHand edited;
      if (entries == null) entries = LingeaDictionary.getOldLingeaEntries();
      if (dictEntries == null) dictEntries = XmlUtils.FileToObject<LingeaDictionary.DictEntry[]>(Machines.rootPath + @"RwDicts\Sources\LingeaOld\design\entriesInfo.xml");
      switch (cmd.type) {
        case Admin.DictEntryCmdType.loadDict:
          Func<LingeaDictionary.DictEntry, Langs> getHand = de => hands.TryGetValue(de.id(), out edited) ? edited.okCrs : Langs.no;
          var res = dictEntries.
            //Where(de => cmd.crsLang == de.crsLang && cmd.natLang == de.natLang).
            Where(de => de.okCrs == Langs.no && cmd.crsLang == de.crsLang && cmd.natLang == de.natLang).
            Select(de => new DictEntryCmd {
              entryId = de.entryId,
              soundMaster = de.soundMaster,
              html = entries[de.crsLang][de.natLang].entryIdToEntry[de.entryId].ToString(),
              okCrs = getHand(de),
            }).ToArray();
          //return new Admin.DictEntryRes { entries = res2.Length > 1000 ? res2.Where(de => de.okCrsMaybe == Langs.no).Take(1000).ToArray() : res2 };
          return new Admin.DictEntryRes { entries = res };
        case Admin.DictEntryCmdType.saveEntry:
          if (!hands.TryGetValue(cmd.id(), out edited)) { edited = new ByHand { entryId = cmd.entryId /*, crsLang = cmd.crsLang, natLang = cmd.natLang, okCrsReason = cmd.okCrsReason*/ }; hands.Add(edited.id(), edited); }
          edited.okCrs = cmd.okCrs;
          XmlUtils.ObjectToFile(handFn, hands.Values.ToList());
          return new Admin.DictEntryRes();
        case DictEntryCmdType.statistics:
          var files0 = dictEntries.Where(de => de.okCrs == Langs.no);
          var files1 = files0.GroupBy(de => new { de.crsLang, de.natLang }).Where(g => g.Count() > 0).Select(g => new { g.Key, count = g.Count() });
          var files2 = files1.
            Select(g => new {
              g.Key,
              count = g.count
            });
          var files3 = files2.Select(fc => new DictEntryCmd {
            allCount = fc.count,
            todoCount = fc.count - getHands(fc.Key.crsLang, fc.Key.natLang).Where(d => d.okCrs != Langs.no).Count(),
            crsLang = fc.Key.crsLang,
            natLang = fc.Key.natLang,
          }).
          ToArray();
          return new Admin.DictEntryRes {
            entries = files3
          };
        default:
          throw new NotImplementedException();
      }
    }
  }
}

public static class LingeaDictionary {

  public static DictObj readDict(DictLib.dictId id) {
    return DictLib.readDict(@"d:\LMCom\rew\Web4\RwDicts\Sources\LingeaOld\", id);
  }

  //Priklad: <w vals="nice#nicest#nice one!#nicer"> <div class="entry">...

  //k wordu z JavaScriptu nalezne ID lingea hesla, 
  //napr. 
  //{nice, nice#nicest#nice one!#nicer},
  //{nicest, nice#nicest#nice one!#nicer},
  //{nice one!, nice#nicest#nice one!#nicer},
  //{nicer, nice#nicest#nice one!#nicer},
  static Dictionary<Langs, Dictionary<Langs, Dictionary<string, string>>> WordToId { get { initLingea(); return _wordToId; } }
  static Dictionary<Langs, Dictionary<Langs, Dictionary<string, string>>> _wordToId;

  //k ID lingea hesla nalezne heslo, napr.
  //{nice#nicest#nice one!#nicer, XElement s heslem}
  static Dictionary<Langs, Dictionary<Langs, Dictionary<string, XElement>>> IdToEntry { get { initLingea(); return _idToEntry; } }
  static Dictionary<Langs, Dictionary<Langs, Dictionary<string, XElement>>> _idToEntry;

  static void initLingea() {
    if (_wordToId != null) return;
    _wordToId = new Dictionary<Langs, Dictionary<Langs, Dictionary<string, string>>>();
    _idToEntry = new Dictionary<Langs, Dictionary<Langs, Dictionary<string, XElement>>>();
    foreach (var natLng in CommonLib.bigLocalizations) {
      var natWord = new Dictionary<Langs, Dictionary<string, string>>(); _wordToId.Add(natLng, natWord);
      var natEntry = new Dictionary<Langs, Dictionary<string, XElement>>(); _idToEntry.Add(natLng, natEntry);
      foreach (var lng in DictLib.crsLangs) {
        var dictFn = Machines.rootPath + @"RwDicts\Sources\LingeaOld\lingea_" + natLng.ToString() + "_" + lng.ToString() + ".xml";
        if (!File.Exists(dictFn)) continue;
        XElement root = XElement.Load(dictFn);
        natWord.Add(
          lng,
          root.Elements().SelectMany(e => e.AttributeValue("vals").
            Split('#').
            Select(v => new { entryId = e.AttributeValue("vals"), wordInstance = v })).
          ToDictionary(kv => kv.wordInstance, kv => kv.entryId)
        );
        natEntry.Add(
          lng,
          root.Elements().ToDictionary(el => el.AttributeValue("vals"), el => el)
        );
      }
    }
  }

  public class wordInf {
    public Langs courseLang;
    public string moduleId;
    public string exId;
    public string word;
  }

  static string[] courseWordFiles = new string[] { "CourseWords", "grafia" };
  static Tuple<string, Langs>[] textWordFiles = new Tuple<string, Langs>[] { new Tuple<string, Langs>("RJ_Lingea", Langs.en_gb) };

  public static IEnumerable<wordInf> wordsForCourse(DictCrsWords langCrs) {
    return langCrs.exs.
      SelectMany(ex => ex.normalized.Split(' ').//.words.Where(w => w.Any(ch => char.IsLetter(ch))).
        Select(w => new wordInf() {
          courseLang = langCrs.lang,
          moduleId = ex.modId,
          exId = ex.exId,
          word = w,
        }));
  }

  static Dictionary<Langs, wordInf[]> courseWords
  {
    get
    {
      if (_courseWords == null) {
        //var courses = courseWordFiles.SelectMany(f => JArray.Parse(File.ReadAllText(Machines.basicPath + @"rew\Web4\RwDicts\" + f + ".json")));
        //d:\LMCom\rew\Web4\RwDicts\CourseWords.xml, grafia.xml apod.
        var wordInfos = courseWordFiles.
          SelectMany(f => XmlUtils.FileToObject<schools.DictWords>(Machines.rootPath + @"RwDicts\UsedWords\" + f + ".xml").courses.
            SelectMany(crs => crs.exs.
              SelectMany(ex => ex.normalized.Split(' '). //.words.
                Select(w => new wordInf() {
                  courseLang = crs.lang,
                  moduleId = ex.modId,
                  exId = ex.exId,
                  word = w,
                }))));
        //slovicka pro RJ wordlisty z d:\LMCom\rew\Web4\RwDicts\RJ_Lingea.txt
        wordInfos = wordInfos.Concat(
          textWordFiles.SelectMany(tf => File.ReadAllLines(Machines.rootPath + @"RwDicts\UsedWords\" + tf.Item1 /*RJ_Lingea*/ + ".txt").Select(t => new wordInf() {
            courseLang = tf.Item2, /*Langs.en_gb*/
            word = t, /*neprelozene slovicko z d:\LMCom\rew\Web4\RwDicts\RJ_Lingea.txt*/
            exId = tf.Item2.ToString(), /*"en_gb"*/
            moduleId = tf.Item2.ToString() /*"en_gb"*/
          }))
        );
        _courseWords = wordInfos.GroupBy(w => w.courseLang).ToDictionary(g => g.Key, g => g.ToArray());
      }
      return _courseWords;
    }
  }
  static Dictionary<Langs, wordInf[]> _courseWords;

  static void captureLingea(Langs courseLang, Langs nativeLang, string dictCode, bool updateOnly = true, bool tryNullEntries = false) {
    if (!DictLib.crsLangs.Contains(courseLang)) return;
    //if (courseLang != Langs.ru_ru) return;
    string fn = Machines.rootPath + @"RwDicts\Sources\LingeaOld\lingea_" + nativeLang.ToString() + "_" + courseLang.ToString() + ".xml";

    _idToEntry = null; _wordToId = null;
    //doposud nactena entry dej do dictionary
    Dictionary<string, string> word_entry = File.Exists(fn) && updateOnly ?
      XElement.Load(fn).Elements().
      SelectMany(e => e.
        AttributeValue("vals").
        Split('#').
        Select(w => DictLib.russianRemoveAkcent(courseLang, w)).
        Distinct().
        Select(key => new { key = key.ToLower(), value = e.HasElements ? e.FirstNode.ToString() : null })).
        GroupBy(kv => kv.key).
        ToDictionary(g => g.Key, g => g.First().value)
      : new Dictionary<string, string>();
    //v courseWords jsou slova upravena takto: lowercase, odstraneny carky nad pismeny.
    var allWords = courseWords[courseLang].Select(w => w.word).Distinct(); //vsechna slova ve vsech kurzech s courseLang
    Action save = () => {
      //V word_entry je: word -> null nebo word -> xmlstring
      XElement langRoot = new XElement("root", new XAttribute("lang", courseLang.ToString()));
      foreach (var grp in word_entry.Where(kv => kv.Value != null).GroupBy(kv => kv.Value)) {
        var vals = grp.Select(g => g.Key).Aggregate((r, i) => r + "#" + i);
        langRoot.Add(new XElement("w", new XAttribute("vals", vals), XElement.Parse(grp.First().Value)));
      }
      foreach (var kv in word_entry.Where(kv => kv.Value == null))
        langRoot.Add(new XElement("w", new XAttribute("vals", kv.Key)));

      //save
      langRoot.Save(fn);
    };
    int cnt = 0;
    foreach (var word in allWords) {
      string entry;
      if (word_entry.TryGetValue(word.ToLower(), out entry) && (entry != null || !tryNullEntries)) continue;
      entry = NewData.Schools.getDictWord(dictCode, courseLang == Langs.ru_ru ? DictLib.normalizeRussian(word) : word);
      //return;
      try { XElement.Parse(entry); } catch { entry = null; }
      word_entry.Add(word, entry);
      //save po 1000 heslech kvuli padu
      cnt++; if (cnt > 1000) { save(); cnt = 0; }
    }
    save();
  }
  //obsolete
  static void captureLingeaOld(Langs courseLang, Langs nativeLang, string dictCode, bool updateOnly = true, bool tryNullEntries = false) {
    if (!DictLib.crsLangs.Contains(courseLang)) return;
    //if (courseLang != Langs.ru_ru) return;
    string fn = Machines.rootPath + @"RwDicts\Sources\LingeaOld\lingea_" + nativeLang.ToString() + "_" + courseLang.ToString() + ".xml";

    _idToEntry = null; _wordToId = null;
    //doposud nactena entry dej do dictionary
    Dictionary<string, string> word_entry = File.Exists(fn) && updateOnly ?
      XElement.Load(fn).Elements().
      SelectMany(e => e.
        AttributeValue("vals").
        Split('#').
        Select(w => DictLib.russianRemoveAkcent(courseLang, w)).
        Distinct().
        Select(key => new { key = key.ToLower(), value = e.HasElements ? e.FirstNode.ToString() : null })).
        GroupBy(kv => kv.key).
        ToDictionary(g => g.Key, g => g.First().value)
      : new Dictionary<string, string>();
    //v courseWords jsou slova upravena takto: lowercase, odstraneny carka nad pismeny.
    var allWords = courseWords[courseLang].Select(w => w.word).Distinct(); //vsechna slova ve vsech kurzech s courseLang
    Action save = () => {
      //V word_entry je: word -> null nebo word -> xmlstring
      XElement langRoot = new XElement("root", new XAttribute("lang", courseLang.ToString()));
      foreach (var grp in word_entry.Where(kv => kv.Value != null).GroupBy(kv => kv.Value)) {
        var vals = grp.Select(g => g.Key).Aggregate((r, i) => r + "#" + i);
        langRoot.Add(new XElement("w", new XAttribute("vals", vals), XElement.Parse(grp.First().Value)));
      }
      foreach (var kv in word_entry.Where(kv => kv.Value == null))
        langRoot.Add(new XElement("w", new XAttribute("vals", kv.Key)));

      //save
      langRoot.Save(fn);
    };
    int cnt = 0;
    foreach (var word in allWords) {
      string entry;
      if (word_entry.TryGetValue(word.ToLower(), out entry) && (entry != null || !tryNullEntries)) continue;
      entry = NewData.Schools.getDictWord(dictCode, courseLang == Langs.ru_ru ? DictLib.normalizeRussian(word) : word);
      //return;
      try { XElement.Parse(entry); } catch { entry = null; }
      word_entry.Add(word, entry);
      //save po 1000 heslech kvuli padu
      cnt++; if (cnt > 1000) { save(); cnt = 0; }
    }
    save();
  }

  public class transformEntryPar {
    public Langs courseLang;
    public Langs nativeLang;
    public object Data;
  }

  public class LingeaSndClone {
    public string word;
    public string fileName;
  }
  public class LingeaSndFile {
    public string fileName;
    public long crc;
    public List<LingeaSndClone> clones;
    public bool theSame(LingeaSndFiles owner, byte[] data) {
      byte[] self = File.ReadAllBytes(Path(owner, true));
      return self.SequenceEqual(data);
    }
    public string Path(LingeaSndFiles owner, bool isMP3) { return owner.BasicPath + "\\" + fileName + (isMP3 ? ".mp3" : ".xml"); }
  }
  public class LingeaSndFiles {
    static string basicPath = @"d:\LMCom\rew\Web4\RwDicts\LingeaSound";
    public string BasicPath { get { return basicPath + "\\" + lng.ToString(); } }
    public Langs lng;
    public List<LingeaSndFile> files;
    public void addSound(string fileName, byte[] data, string word) {
      fileName = fileName.ToLower();
      long crc = ZipStream.Crc(data);
      var sameFile = files.Where(f => f.crc == crc).FirstOrDefault(f => f.theSame(this, data));
      if (sameFile == null) {
        files.Add(sameFile = new LingeaSndFile() {
          fileName = fileName,
          crc = crc,
          clones = new List<LingeaSndClone>() {  new LingeaSndClone() { fileName = fileName, word = word, }
        }
        });
        File.WriteAllBytes(sameFile.Path(this, true), data);
      } else {
        if (sameFile.clones.Any(f => f.fileName == fileName)) return;
        sameFile.clones.Add(new LingeaSndClone() { fileName = fileName, word = word, });
      }
      XmlUtils.ObjectToFile(sameFile.Path(this, false), sameFile);
    }
    public static void addSound(LingeaSndFile[] files, LingeaSndFiles[] owners, LingeaSndFile newFile, LingeaSndFiles newOwner) {
      var data = File.ReadAllBytes(newFile.Path(newOwner, true));
      var sameFile = files.Zip(owners, (file, owner) => new { file, owner }).FirstOrDefault(f => f.file.theSame(f.owner, data));
      if (sameFile == null) return;
      //merge newFile do sameFile a vymaz newFile
      foreach (var cl in newFile.clones) if (!sameFile.file.clones.Any(c => c.fileName == cl.fileName))
          sameFile.file.clones.Add(cl);
      File.Copy(newFile.Path(newOwner, true), @"d:\temp\duplSounds\" + newFile.fileName + ".mp3", true);
      File.Delete(newFile.Path(newOwner, true));
      File.Copy(newFile.Path(newOwner, false), @"d:\temp\duplSounds\" + newFile.fileName + ".xml", true);
      File.Delete(newFile.Path(newOwner, false));
      XmlUtils.ObjectToFile(sameFile.file.Path(sameFile.owner, false), sameFile.file);
    }
    public LingeaSndFile findViaFileName(string fileName) {
      fileName = fileName.ToLower();
      return files.FirstOrDefault(f => f.clones.Any(c => c.fileName == fileName));
    }
    public LingeaSndFile findViaFileName(string fileName, ref object buff) {
      Dictionary<string, LingeaSndFile> clones = buff as Dictionary<string, LingeaSndFile>;
      if (clones == null) clones = files.SelectMany(f => f.clones.Select(c => c.fileName).Select(c => new { fn = c, fo = f })).ToDictionary(fnfo => fnfo.fn, fnfo => fnfo.fo);
      buff = clones;
      LingeaSndFile res;
      return clones.TryGetValue(fileName.ToLower(), out res) ? res : null;
    }

    public static LingeaSndFiles addSoundsStart(Langs lng, bool force = false) {
      string fn = basicPath + "\\" + lng.ToString() + ".xml";
      if (!force && File.Exists(fn)) {
        var res = XmlUtils.FileToObject<LingeaSndFiles>(fn);
        //File.Delete(fn);
        return res;
      } else {
        var res = new LingeaSndFiles() { lng = lng };
        res.files = Directory.EnumerateFiles(res.BasicPath, "*.xml").Select(f => XmlUtils.FileToObject<LingeaSndFile>(f)).ToList();
        XmlUtils.ObjectToFile(fn, res);
        return res;
      }
    }
    public void addSoundsEnd() {
      string fn = basicPath + "\\" + lng.ToString() + ".xml";
      XmlUtils.ObjectToFile(fn, this);
    }
  }

  public struct dicts {
    public Langs nativeLang;
    public Langs crsLang;
    public string FileName() { return fileName(nativeLang, crsLang); }
    public static string fileName(Langs nat, Langs crs) {
      return Machines.rootPath + @"RwDicts\Sources\LingeaOld\lingea_" + nat.ToString() + "_" + crs.ToString() + ".xml";
    }
  }


  //vsechny kombinace crsLang,nativeLang odvozene ze souboru z d:\LMCom\rew\Web4\RwDicts\
  static IEnumerable<dicts> allDicts
  {
    get
    {
      if (_allDicts == null) {
        _allDicts = CommonLib.bigLocalizations.SelectMany(nat => DictLib.crsLangs.
          Where(crs => File.Exists(dicts.fileName(nat, crs))).
          Select(crs => new dicts() { crsLang = crs, nativeLang = nat })).ToArray();
      }
      return _allDicts;
    }
  }
  static dicts[] _allDicts;

  //static void LingeaToModules(IEnumerable<Langs> nativeLangs, DictWords allUsedWords, Func<XElement, transformEntryPar, XElement> hideLingea) {
  //  foreach (var langCrs in allUsedWords.courses) {
  //    var courseLang = langCrs.lang;
  //    var mods = langCrs.exs.GroupBy(e => e.modId).Select(g => new modExs() { jsonId = g.Key, exs = g.Select(e => e.exId).ToArray(), isGramm = g.Key.StartsWith(grammSpace) });
  //    foreach (var nativeLang in nativeLangs) {
  //      if (!allDicts.Any(d => d.nativeLang == nativeLang && d.crsLang == courseLang)) continue;

  //      var transformPar = new transformEntryPar() { courseLang = courseLang, nativeLang = nativeLang };


  //      //if (lng != Langs.ru_ru || nativeLang!=Langs.cs_cz) return; //DEBUG

  //      /* word: slovo v textu, napr. nicer
  //       * entry: XML s lingea heslem ve slovniku
  //       * email, entryId: napr. nice#nicest#nice one!#nicer
  //       */


  //      var allIdToEntry = IdToEntry[nativeLang][courseLang];
  //      var allWordToId = WordToId[nativeLang][courseLang];

  //      //Directory 
  //      var exerciseIdToUsedWords = wordsForCourse(langCrs).
  //        GroupBy(w => w.moduleId + "/" + w.exId).
  //        ToDictionary(
  //          g => g.Key,
  //          g => g.Select(w => w.word).Distinct()
  //        );
  //      IEnumerable<string> exWords = null;
  //      LingeaSndFiles files = LingeaSndFiles.addSoundsStart(courseLang);
  //      object buff = null;
  //      foreach (var mod in mods) { //pro vsechny pozadovane moduly
  //        //if (nativeLang != Langs.de_de || !mod.isGramm) continue;

  //        //if (!mod.isGramm) continue;
  //        //vsechna slova vyskytujici se v modulu:
  //        var usedWords = mod.exs. //vsechna cviceni modulu
  //          //nova verze EA ma jako testEx.compId rovno celou cestu
  //          Where(testEx => exerciseIdToUsedWords.TryGetValue(mod.jsonId + "/" + testEx.Substring(testEx.LastIndexOf("/") + 1), out exWords) || exerciseIdToUsedWords.TryGetValue(mod.jsonId + "/" + testEx, out exWords)).
  //          SelectMany(exId => exWords).
  //          Distinct().
  //          ToArray();

  //        //ID hesel modulu
  //        string eId1 = null;
  //        var entryIds = usedWords.Where(w => allWordToId.TryGetValue(w, out eId1)).Select(w => eId1).Distinct().ToArray();
  //        //slovnik neprazdnych hesel modulu
  //        XElement entry = null; int entrCnt = 0;
  //        var usedEntries = entryIds.
  //          Where(entryId => allIdToEntry.TryGetValue(entryId, out entry) && (entry = hideLingea == null ? entry : hideLingea(entry, transformPar)).HasElements).
  //          Select(entryId => new { entryId = entryId, entry = entry, entryCount = entrCnt++ }).
  //          ToArray();
  //        //kratka identifikace hesel modulu
  //        var idToNumId = usedEntries.ToDictionary(e => e.entryId, e => e.entryCount);
  //        //slovnik word => count
  //        string eId2 = null; int numId = -1;
  //        var wordToNumId = usedWords.Where(w => allWordToId.TryGetValue(w, out eId2) && idToNumId.TryGetValue(eId2, out numId)).ToDictionary(w => w, w => numId);

  //        /************** normalizeSoundElements ***************/
  //        List<string> sndFileNames = new List<string>();
  //        List<string> wrongSndFileNames = new List<string>();
  //        //sound element nahradi <sound type='sound|lex_ful_wsnd'>url</sound>. Take naplni sndFileNames ev. wrongSndFileNames.
  //        Action<XElement> normalizeSoundElements = null;
  //        normalizeSoundElements = root => {
  //          foreach (var el in root.Elements().ToArray()) {
  //            switch (el.AttributeValue("class")) {
  //              case "lex_ful_wsnd": el.ReplaceWith(new XElement("sound", new XAttribute("type", "lex_ful_wsnd"), el.Value)); break;
  //              case "sound":
  //                var url = el.AttributeValue("url"); if (string.IsNullOrEmpty(url)) { root.Remove(); continue; }
  //                var fileName = url.Split('/').Last().Replace(".mp3", null);
  //                var sndFile = string.IsNullOrEmpty(fileName) ? null : files.findViaFileName(fileName, ref buff);
  //                string offurl = null;
  //                if (sndFile == null) {
  //                  wrongSndFileNames.Add(fileName);
  //                } else {
  //                  offurl = courseLang.ToString() + "/" + sndFile.fileName;
  //                  offurl = string.Format("RwDicts/LingeaSound/{0}.mp3", offurl).ToLower();
  //                  sndFileNames.Add(offurl);
  //                }
  //                el.ReplaceWith(new XElement("sound", offurl)); break;
  //            }
  //            normalizeSoundElements(el);
  //          }
  //        };
  //        foreach (var el in usedEntries) normalizeSoundElements(el.entry);

  //        /************** encodeLingeaEntry ***************/
  //        Func<XElement, string> tagToStr = tag => { return tag.Name.LocalName + tag.Attributes().Select(a => " " + a.Name.LocalName + "='" + a.Value + "'").DefaultIfEmpty().Aggregate((r, i) => r + i); };

  //        //slovnik tagu, pouzitich v entry XML. Slouzi k logicke kompresi entry, misto tagu je jen jeho email
  //        short tagId = 1;
  //        var tagStrToInt = usedEntries.SelectMany(ke => ke.entry.Descendants().Select(d => tagToStr(d))).Distinct().ToDictionary(str => str, str => tagId++);

  //        Func<XNode, DictItemRoot> encodeLingeaEntry = null;
  //        encodeLingeaEntry = nd => {
  //          if (nd.NodeType == System.Xml.XmlNodeType.Text) return new DictItemRoot() { text = ((XText)nd).Value };
  //          else {
  //            var el = nd as XElement;
  //            var res2 = new DictItemRoot() { tag = tagStrToInt[tagToStr(el)] };
  //            var nodes = el.Nodes(); var first = nodes.FirstOrDefault();
  //            if (first != null && first.NodeType == System.Xml.XmlNodeType.Text) { nodes = nodes.Skip(1); res2.text = ((XText)first).Value; }
  //            if (nodes.Count() > 0) res2.items = nodes.Select(n => encodeLingeaEntry(n)).ToArray();
  //            return res2;
  //          }
  //        };

  //        /************** DictObj ***************/
  //        var dict = new DictObj();
  //        dict.Tags = tagStrToInt.ToDictionary(kv => kv.Value.ToString(), kv => kv.Key);
  //        //zakodovane entries (misto tagu jsou jen typy)
  //        dict.Entries = usedEntries.ToDictionary(k => k.entryCount.ToString(), k => encodeLingeaEntry(k.entry.Nodes().First()));
  //        //slovnik word -> entry email
  //        dict.Keys = wordToNumId;
  //        var dirName = mod.isGramm ? "EAGrammar" : "EAData";
  //        var modId = mod.isGramm ? mod.jsonId.Replace(grammSpace, null) : mod.jsonId;
  //        EADeployLib.writeFile(Machines.basicPath + @"rew\Web4\Schools\" + dirName + @"\", "lingDict_" + modId + ".json", nativeLang,
  //          JsonConvert.SerializeObject(dict, Newtonsoft.Json.Formatting.Indented, EADeployLib.jsonSet), true);
  //        //info o zvukovych souborech
  //        File.WriteAllLines(Machines.basicPath + @"rew\Web4\Schools\" + dirName + @"\" + nativeLang.ToString().Replace('_', '-') + @"\lingDictSound_" + modId + ".txt", sndFileNames.Distinct());
  //        if (wrongSndFileNames.Count > 0) File.WriteAllLines(Machines.basicPath + @"rew\Web4\Schools\" + dirName + @"\" + nativeLang.ToString().Replace('_', '-') + @"\lingDictSound_" + modId + ".wrong", wrongSndFileNames.Distinct());
  //      }
  //      var ttsDir = transformPar.RowType as Tts.Dir;
  //      if (ttsDir != null) ttsDir.Save();
  //    }
  //  }
  //}
  const string grammSpace = "grammar/";



  public static void CaptureLingeaAll() {
    foreach (var dict in LMComLib.DictInfo.InstanceNew.Dicts)
      captureLingea(dict.From, dict.To, dict.Code);
  }

  public class LingeaDictFile {
    public LingeaDictFile() {
    }
    public LingeaDictFile(XElement root, Langs cl = Langs.no, Langs nl = Langs.no) {
      this.root = root;
      crsLang = LowUtils.EnumParse<Langs>(root.AttributeValue("crsLang", cl.ToString()));
      nativeLang = LowUtils.EnumParse<Langs>(root.AttributeValue("nativeLang", nl.ToString()));
      var subroot = root.Element("entries");
      if (subroot != null) entryIdToEntry = subroot.Elements().ToDictionary(el => el.AttributeValue("id"), el => el); else entryIdToEntry = new Dictionary<string, XElement>();
      subroot = root.Element("courseUses");
    }
    public XElement Save() {
      StringBuilder sb = new StringBuilder();
      XElement res = new XElement("root",
        new XAttribute("crsLang", crsLang.ToString()),
        new XAttribute("nativeLang", nativeLang.ToString()),
        new XElement("entries", entryIdToEntry.OrderBy(e => e.Key).Select(ie => ie.Value))
      );
      return res;
    }
    public Langs crsLang;
    public Langs nativeLang;
    public string[] missing;
    public Dictionary<string, XElement> entryIdToEntry;
    public XElement root;
  }


  static Func<string, string> urlToFile = url => url.Split('/').Last().Split('.').First();
  public static Func<Langs, Langs, string> encodeLangs = (l1, l2) => { return (int)l1 > (int)l2 ? l1.ToString() + l2.ToString() : l2.ToString() + l1.ToString(); };
  public static Func<string, string> dictFromCode = code => { var di = LMComLib.DictInfo.Instance.Dicts.First(d => d.Code == code); return encodeLangs(di.From, di.To); };

  public enum OKCrsReason {
    no,
    soundMaster,
    pronunc,
    russian,
    charsOK,
    wordsOK,
    viaPronunciation,
    morf_nm_nf,
    morf_m_pl_f,
    byHand
  }

  public class DictEntry {
    [XmlAttribute]
    public string entryId;
    [XmlAttribute]
    public Langs crsLang;
    [XmlAttribute]
    public Langs natLang;
    [XmlAttribute]
    public string soundMaster;
    [XmlAttribute]
    public Langs okCrs;
    [XmlAttribute]
    public OKCrsReason okCrsReason;

    //[XmlIgnore]
    //public string[] headWords;
    [XmlIgnore]
    public LangCounts wordStats;
    //[XmlAttribute]
    //[XmlIgnore]
    //public string dictId;
    //[XmlAttribute]
    //[XmlIgnore]
    //public string soundDictId;
    [XmlIgnore]
    //[XmlAttribute]
    public string soundFile;
    [XmlIgnore]
    //[XmlAttribute]
    public string pronText;
    //public string[] soundWords;
    [XmlIgnore]
    public XElement entry;
    [XmlIgnore]
    //[XmlAttribute]
    public Langs okCrsMaybe;
    public IEnumerable<Langs> bothLangs() { return XExtension.Create(crsLang, natLang); }
    public string id() { return entryId; }
    public string wordId() { return entryId.Split('|')[2]; }
    public string[] headWords
    {
      get
      {
        return _headWords == null ?
          (_headWords = splitHeadwords(entry.DescendantsAttr("class", "entr").First().Value).Distinct().ToArray()) :
          _headWords;
      }
      set { _headWords = value; }
    }
    string[] _headWords;

    public DictEntryObj toNew(Impersonator imp) {
      using (WindowsIdentity.Impersonate(imp.token)) {
        return new DictEntryObj {
          entryId = entryId,
          entry = entry.Elements().First(),
          soundMaster = soundMaster,
          type = DictEntryType.lingeaOld,
          headWords = headWords,
          courseWords = entry.AttributeValue("courseUsed", "").Split('|').Select(w => w.ToLower().Replace("'", null)).Except(headWords.Select(h => h.ToLower())).Where(w => !string.IsNullOrEmpty(w)).ToArray(),
          //headWords.Select(hw => new CourseDictionary2.DictStem { type = CourseDictionary2.DictStemType.wordId, word = hw }).
          //  Concat(headWords.ToArray().SelectMany(w => CourseDictionary.RunStemming(okCrs, w)).Distinct().Select(w => new CourseDictionary2.DictStem { type = CourseDictionary2.DictStemType.stem, word = w })).
          //  Concat(entry.AttributeValue("courseUsed", "").Split('|').Where(w => !string.IsNullOrEmpty(w)).Select(w => new CourseDictionary2.DictStem { type = CourseDictionary2.DictStemType.courseUses, word = w })).
          //  ToArray()
        };
      };
    }
  }

  public class GoogleGelp {
    [XmlAttribute]
    public Langs crsLang;
    [XmlAttribute]
    public Langs natLang;
    [XmlAttribute]
    public string entryId;
    [XmlAttribute]
    public string soundMaster;
    public string[] natWords;
    public string[] crsWords;
    //XXX
    //public Google.DetectRes[] crsDetects;
    //public Google.DetectRes[] natDetects;
    public dynamic crsDetects;
    public dynamic natDetects;
    public string id() { return entryId; }
  }

  public struct LangCount {[XmlAttribute] public Langs lang;[XmlAttribute] public int count;[XmlAttribute] public string chars; }
  public struct LangCounts {
    [XmlAttribute]
    public string crsCharsOk;
    [XmlAttribute]
    public string crsCharsWrong;
    [XmlAttribute]
    public string natCharsOk;
    [XmlAttribute]
    public string natCharsWrong;
    [XmlAttribute]
    public int crsWordsOk;
    [XmlAttribute]
    public int crsWordsWrong;
    [XmlAttribute]
    public int natWordsOk;
    [XmlAttribute]
    public int natWordsWrong;
    [XmlIgnore]
    public string[] natWords;
    [XmlIgnore]
    public string[] crsWords;
  }

  public static void googleDetectLang(IEnumerable<DictEntry> entries, List<GoogleGelp> start, string fn) {
    //XXX
    //var goog = new Translator.Google();
    dynamic goog = null;
    //dynamic
    Func<string[], /*XXXGoogle.DetectRes[]*/dynamic> getDetect = words => {
      if (words.Length == 0) return null;
      var txt = words.Aggregate((r, i) => r + " " + i);
      if (txt.Length > 1800) txt = txt.Substring(0, 1800);
      return goog.detect(txt).ToArray();
    };
    Parallel.ForEach(entries, de => {
      try {
        var gh = new LingeaDictionary.GoogleGelp {
          entryId = de.entryId,
          crsLang = de.crsLang,
          natLang = de.natLang,
          crsWords = de.wordStats.crsWords,
          natWords = de.wordStats.natWords,
          crsDetects = getDetect(de.wordStats.crsWords),
          natDetects = getDetect(de.wordStats.natWords),
          soundMaster = de.soundMaster,
        };
        lock (start) {
          if (start.Count % 500 == 0) XmlUtils.ObjectToFile(fn, start);
          start.Add(gh);
        }
      } catch { }
    });
    XmlUtils.ObjectToFile(fn, start);
  }

  public static IEnumerable<XElement> dumpDictEntries(IEnumerable<DictEntry> entrs) {
    return entrs.Select(en => new XElement("entry", XElement.Parse(XmlUtils.ObjectToString(en)), en.entry));
  }
  public static void dumpDictEntries(string fn, IEnumerable<DictEntry> entrs) {
    new XElement("root", dumpDictEntries(entrs)).Save(fn);
  }
  public static void dumpViaEntryId(IEnumerable<DictEntry> entries, string fn, string entryId) {
    dumpDictEntries(fn, entries.Where(e => e.entryId == entryId));
  }

  public static List<DictEntry> errors = new List<DictEntry>();

  public static char[] russianAlphabet = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя".ToCharArray();
  public static char[] win1252 = Enumerable.Range(0, 255).Select(i => Convert.ToChar(i)).Where(ch => char.IsLetter(ch) && !("žŽšŠ".Contains(ch))).Concat("œ".ToCharArray()).ToArray();
  static char[] delims = Enumerable.Range(0, 255).Select(b => Convert.ToChar(b)).Where(ch => !char.IsLetter(ch) && ch != '\'' && ch != '-' && ch != '(' && ch != ')').ToArray();

  static IEnumerable<string> split(string s) { return s.ToLower().Split(delims, StringSplitOptions.RemoveEmptyEntries); }
  static IEnumerable<string> splitHeadwords(string s) {
    s = s.Replace("\x301", null);
    s = wrongs.Replace(s, "");
    var matches = regExItem.Parse(s, brackets).ToArray();
    if (matches.Length == 1) foreach (var v in s.Split(',').Select(t => t.Trim())) yield return v;
    else {
      var f = matches.First(); string beg = f.IsMatch ? null : f.Value;
      var l = matches.Last(); string end = l.IsMatch ? null : l.Value;
      var m = matches.First(t => t.IsMatch).Value; m = m.Substring(1, m.Length - 2);
      if (m.IndexOf(',') < 0) {
        yield return beg + end;
        yield return beg + m + end;
      } else {
        foreach (var v in m.Split(',')) yield return beg + v + end;
      }
    }
  }
  static Regex brackets = new Regex(@"\(.*?\)"); static Regex wrongs = new Regex(@"(\*|1|2|\(s'\)|\(se\))");

  //public class modExs {
  //  public static modExs formMod(CourseMeta.modInfo mod) { return new modExs() { jsonId = mod.jsonId, exs = mod.exs.Select(e => e.compId).ToArray() }; }
  //  public bool isGramm;
  //  public string jsonId;
  //  public string[] exs;
  //}

  public static void getDictEntries(string srcDir) {

    if (dictEntries != null) return;
    errors.Clear();
    Func<Langs, Langs, string> dictCode = (l1, l2) => LMComLib.DictInfo.GetDict(l1, l2).Code;
    Func<Langs, string> dictHalfCode = l => CommonLib.langToLingeaLang(l);

    //sound files namapovane na master sound file a seznam slovicek, ktere jsou ozvuceny
    var soundFileToMastersAndUsages = DictLib.crsLangs.SelectMany(l => LingeaSndFiles.addSoundsStart(l).files.SelectMany(f => f.clones.Select(c => new {
      lang = l,
      c.fileName,
      soundMaster = f.fileName,
      //words = f.clones.SelectMany(cl => cl.word.Split('#')).Distinct()
    }))).
      GroupBy(f => f.fileName).ToDictionary(
        g => g.Key,
        g => new DictEntry { soundMaster = g.First().lang.ToString() + "/" + g.Select(t => t.soundMaster).Distinct().Single()/*, soundWords = g.SelectMany(t => t.words).Distinct().ToArray()*/ }
     );

    //slova z entry
    Regex tagEx = new Regex(@"<.*?>", RegexOptions.Singleline);
    Func<XElement, IEnumerable<string>> removeTags = el => split(tagEx.Replace(el.ToString(), " "));
    Func<XElement, XElement> srcFromTrans = el => { foreach (var src in el.Descendants().Where(e => new string[] { "in_c", "in_v", "in_g", "in_w", "in_pp", "in_d" }.Contains(e.AttributeValue("class"))).ToArray()) src.Remove(); return el; };
    Func<DictEntry, string[][]> extractWords = de => {
      //if (de.wordId() == "dvd" && de.natLang==Langs.ru_ru) {
      //  de.entry = srcFromTrans(de.entry);
      //}
      var trans = de.entry.DescendantsAttr("class", "trans").Concat(de.entry.DescendantsAttr("class", "trg")).Select(el => srcFromTrans(el)).SelectMany(el => removeTags(el)).Distinct();
      var src = de.entry.DescendantsAttr("class", "src").SelectMany(el => removeTags(el)).Concat(de.headWords).Distinct();
      return new string[][] { src.ToArray(), trans.ToArray() };
    };
    //Dictionary<string, string[]> entryWords = null;


    //*********** vytvoreni hesla
    Regex entrTagToSoundTitleEx = new Regex(@"(\d|\*|\(.*?\))");
    Func<string, string> entrTagToSoundTitle = s => entrTagToSoundTitleEx.Replace(s, "").ToLower().Trim().Replace("\x301", null);
    Func<Langs, Langs, KeyValuePair<string, XElement>, DictEntry> createEntry = (crsLang, natLang, id_entry) => {
      var res = new DictEntry {
        //dictId = dictCode(crsLang, natLang),
        entryId = id_entry.Key,
        //soundTitle = title = entrTagToSoundTitle(id_entry.Value.DescendantsAttr("class", "entr").First().Value),
        //headWords = splitHeadwords(id_entry.Value.DescendantsAttr("class", "entr").First().Value).ToArray(),
        crsLang = crsLang,
        natLang = natLang,
        entry = id_entry.Value,
      };
      res.soundFile = id_entry.Value.AttributeValue("oldLngSound");
      var pron = id_entry.Value.DescendantsAttr("class", "pron").FirstOrDefault();
      res.pronText = pron == null ? null : pron.Value;
      DictEntry sndFileInfo = null;
      if (res.soundFile != null) {
        //res2.soundDictId = res2.soundFile.Substring(1, 4);
        soundFileToMastersAndUsages.TryGetValue(res.soundFile, out sndFileInfo);
        if (sndFileInfo != null) res.soundMaster = sndFileInfo.soundMaster;
        //res2.soundWords = sndFileInfo.soundWords;
      }
      var wordsFromEntry = extractWords(res);
      res.wordStats = new LangCounts {
        crsWords = wordsFromEntry[0],
        natWords = wordsFromEntry[1]
      };
      return res;
    };

    dictEntries = getOldLingeaEntries(srcDir).
      //k entry dej seznam used Words (puvodni "vals" atribut "w" tagu slovniku)
      SelectMany(crs => crs.Value.
        //SideEfect(nat => entryWords = nat.Value.wordToEntryId.GroupBy(d => d.Value).ToDictionary(d => d.Key, d => d.Select(t => t.Value).Distinct().ToArray())).
        SelectMany(nat => nat.Value.entryIdToEntry.Select(kv => createEntry(crs.Key, nat.Key, kv)))
      ).ToArray();

    //zjisti jazyk pres SoundMaster file
    var soundMasterLangs = dictEntries.Where(de => de.soundMaster != null).GroupBy(de => de.soundMaster).Select(g => new {
      g.Key, //soundMaser
      langs = new {
        langs = DictLib.crsLangs.Where(l => g.All(de => de.bothLangs().Contains(l))).Distinct().ToArray(), //spolecne Crs jazyk pro stejny zvuk
        dicts = g.Select(t => encodeLangs(t.crsLang, t.natLang)).Distinct().ToArray() //ruzne slovniky
      }
    }).
    Where(kv => kv.langs.dicts.Length > 2 && kv.langs.langs.Length == 1). //musi byt alespon 2 slovniky a prave jeden jazyk
    ToDictionary(kv => kv.Key, kv => kv.langs.langs[0]); //tabulka soundMaster => jazyk

    int ru = 0; int morf1 = 0; int morf1Err = 0; int morf2 = 0; int cwin1252 = 0;
    //foreach (var de in dictEntries.Where(de => de.wordId() == "affittare")) {
    foreach (var de in dictEntries) {

      //Pres spolecny soundMaster
      if (de.soundMaster != null) {
        Langs l;
        if (soundMasterLangs.TryGetValue(de.soundMaster, out l)) {
          if (de.okCrs == Langs.no) {
            de.okCrs = l;
            de.okCrsReason = OKCrsReason.soundMaster;
          } else if (de.okCrs != l)
            //errors.Add(de); 
            throw new Exception();
        }
      }
      //nemcina a francouzstina - vyslovnost
      if (de.pronText != null && (de.crsLang == Langs.en_gb && de.natLang != Langs.fr_fr || de.natLang == Langs.en_gb && de.crsLang != Langs.fr_fr)) {
        if (de.okCrs == Langs.no) {
          de.okCrs = Langs.en_gb;
          de.okCrsReason = OKCrsReason.pronunc;
        } else if (de.okCrs != Langs.en_gb)
          //errors.Add(de); 
          throw new Exception();
      }
      if (de.pronText != null && (de.crsLang == Langs.fr_fr && de.natLang != Langs.en_gb || de.natLang == Langs.fr_fr && de.crsLang != Langs.en_gb)) {
        if (de.okCrs == Langs.no) {
          de.okCrs = Langs.fr_fr;
          de.okCrsReason = OKCrsReason.pronunc;
        } else if (de.okCrs != Langs.fr_fr)
          //errors.Add(de); 
          throw new Exception();
      }
      //rustina
      if (de.crsLang == Langs.ru_ru || de.natLang == Langs.ru_ru) {
        Func<string[], bool> isRussian = words => words.SelectMany(w => w.ToCharArray()).Intersect(russianAlphabet).Any();
        var natIsRussian = isRussian(de.wordStats.natWords);
        var crsIsRussian = isRussian(de.wordStats.crsWords);
        Langs crsLng;
        Langs notRuLang = de.crsLang == Langs.ru_ru ? de.natLang : de.crsLang;

        if (natIsRussian && !crsIsRussian) {
          crsLng = notRuLang;
        } else if (crsIsRussian && !natIsRussian) {
          crsLng = Langs.ru_ru;
        } else
          crsLng = Langs.no; ;
        if (crsLng != Langs.no)
          if (de.okCrs == Langs.no) {
            ru++;
            de.okCrs = crsLng;
            de.okCrsReason = OKCrsReason.russian;
          } else if (de.okCrs != crsLng)
            //errors.Add(de); 
            throw new Exception();
      }

      //v en_gb, fr_fr x sp_sp : okCrs neni en_gb iff existuje <span class="morf"> nf nebo nm
      bool isFr = !de.bothLangs().Except(XExtension.Create(Langs.en_gb, Langs.fr_fr)).Any();
      bool isSp = !de.bothLangs().Except(XExtension.Create(Langs.en_gb, Langs.sp_sp)).Any();
      if (isFr || isSp) {
        if (de.entry.DescendantsAttr("class", "morf").Any(el => new string[] { "nm", "nf" }.Contains(el.Value.Trim()))) {
          Langs lng = isFr ? Langs.fr_fr : Langs.sp_sp;
          if (de.okCrs == Langs.no) {
            morf1++;
            de.okCrs = lng;
            de.okCrsReason = OKCrsReason.morf_nm_nf;
          } else if (de.okCrs != lng) {
            morf1Err++;
            //1 chyba, soufflé
            //errors.Add(de); 
            //throw new Exception();
          }
        }
      }
      //v en_gb, de_de x it_it : okCrs neni en_gb iff existuje <span class="morf"> m pl | m | f pl | f
      bool isDe = !de.bothLangs().Except(XExtension.Create(Langs.en_gb, Langs.de_de)).Any();
      bool isIt = !de.bothLangs().Except(XExtension.Create(Langs.en_gb, Langs.it_it)).Any();
      if (isDe || isDe) {
        if (de.entry.DescendantsAttr("class", "morf").Any(el => new string[] { "m pl", "m", "f pl", "f" }.Contains(el.Value.Trim()))) {
          Langs lng = isDe ? Langs.de_de : Langs.it_it;
          if (de.okCrs == Langs.no) {
            morf2++;
            de.okCrs = lng;
            de.okCrsReason = OKCrsReason.morf_m_pl_f;
          } else if (de.okCrs != lng)
            throw new Exception();
        }
      }

      if (de.crsLang != Langs.ru_ru && de.natLang != Langs.ru_ru && DictLib.crsLangs.Contains(de.crsLang) && !DictLib.crsLangs.Contains(de.natLang)) {
        Func<string[], bool> notWin1252 = words => words.SelectMany(w => w.ToCharArray()).Where(ch => char.IsLetter(ch)).Except(win1252).Any();
        var natNotWin1252 = notWin1252(de.wordStats.natWords);
        var crsNotWin1252 = notWin1252(de.wordStats.crsWords);
        if (natNotWin1252 != crsNotWin1252) {
          Langs crsLng;
          if (natNotWin1252 && !crsNotWin1252)
            crsLng = de.crsLang;
          else if (crsNotWin1252 && !natNotWin1252) { //šř v 
            crsLng = de.natLang;
          } else
            throw new Exception();
          if (de.okCrs == Langs.no) {
            cwin1252++;
            de.okCrs = crsLng;
            de.okCrsReason = OKCrsReason.charsOK;
          } else if (de.okCrs != crsLng)
            throw new Exception();
        }
        //cwin1252Err++;
      }

    }
    if (morf1Err > 1) throw new Exception();

    //***************** grupy dle vyslovnosti
    Langs[] enfr = new Langs[] { Langs.en_gb, Langs.fr_fr };
    Dictionary<string, Langs> pronToLang = new Dictionary<string, Langs>();
    foreach (var entr in dictEntries.Where(de => de.pronText != null).GroupBy(de => de.wordId())) {
      pronToLang.Clear();
      foreach (var pronGrp in entr.GroupBy(de => de.pronText).Where(g => g.Count() > 1)) {
        foreach (var de in pronGrp) {
          var otherLang = de.bothLangs().Except(enfr).FirstOrDefault();
          if (otherLang == Langs.no) continue;
          pronToLang[de.pronText] = de.crsLang == otherLang ? de.natLang : de.crsLang;
        };
      }
      if (pronToLang.Count() < 2) continue;
      foreach (var de in entr) {
        Langs pronl;
        if (!pronToLang.TryGetValue(de.pronText, out pronl)) {
          if (de.bothLangs().Intersect(enfr).Count() == 2)
            pronl = pronToLang.First().Value == Langs.en_gb ? Langs.fr_fr : Langs.en_gb;
          else
            continue;
        }
        if (de.okCrs == Langs.no) {
          de.okCrs = pronl;
          de.okCrsReason = OKCrsReason.viaPronunciation;
        } else if (de.okCrs != pronl)
          throw new Exception();
      }
    }


    //******************* rucne upravena slovicka 
    Dictionary<string, Admin.ByHand> hands = new Dictionary<string, Admin.ByHand>();
    var files = Directory.EnumerateFiles(@"d:\LMCom\rew\Web4\RwDicts\Sources\LingeaOld\design", "byHand_*.xml");
    foreach (var fn in files) foreach (var h in XmlUtils.FileToObject<List<Admin.ByHand>>(fn))
        hands.Add(h.id(), h);
    //var hands = Directory.EnumerateFiles(@"d:\LMCom\rew\Web4\RwDicts\Sources\LingeaOld\design", "byHand_*.xml").SelectMany(fn => XmlUtils.FileToObject<List<Admin.ByHand>>(fn)).ToDictionary(cr => cr.email(), cr => cr);
    foreach (var de in dictEntries.Where(de => de.okCrs == Langs.no && hands.ContainsKey(de.id()))) { de.okCrs = hands[de.id()].okCrs; de.okCrsReason = OKCrsReason.byHand; }

    //******************* zdroj spravnych slov jsou doposud oznacena slova v Lingea slovniku. Pro dany slovnik se udela rozdil spravnych slov.
    var allLangs = dictEntries.Where(de => de.okCrs != Langs.no).SelectMany(de => de.bothLangs()).Distinct().ToArray();
    Func<DictEntry, Langs, IEnumerable<string>> getWords = (de, l) => de.okCrs == l ? de.wordStats.crsWords : de.wordStats.natWords;

    var sources = allLangs.ToDictionary(
      l => l,
      l => dictEntries.Where(de => de.okCrs != Langs.no && de.bothLangs().Contains(l)).SelectMany(de => getWords(de, l)).Distinct().ToArray()
    );
    Dictionary<Langs, Dictionary<Langs, Dictionary<string, bool>>> diffWordsTab = allLangs.ToDictionary(myLang => myLang, myLang => allLangs.Where(nl => nl != myLang).ToDictionary(otherLang => otherLang, otherLang => {
      try {
        var ml = sources[myLang]; var ol = sources[otherLang];
        return ml.Except(ol).ToDictionary(w => w, w => true);
      } catch { return new Dictionary<string, bool>(); }
    }));
    Func<Langs, Langs, Dictionary<string, bool>> diffWords = (myLang, otherLang) => {
      return diffWordsTab.ContainsKey(myLang) && diffWordsTab[myLang].ContainsKey(otherLang) ? diffWordsTab[myLang][otherLang] : null;
    };
    //foreach (var de in dictEntries.Where(de => de.wordId() == "cofounder").Where(de => de.okCrs == Langs.no && de.crsLang != Langs.ru_ru && !CourseDictionary.crsLangs.Contains(de.natLang))) {
    foreach (var de in dictEntries.Where(de => de.okCrs == Langs.no && de.crsLang != Langs.ru_ru && !DictLib.crsLangs.Contains(de.natLang))) {
      var crsOkW = diffWords(de.crsLang, de.natLang);
      var natOkW = diffWords(de.natLang, de.crsLang);
      if (crsOkW == null || natOkW == null) continue;
      var isCrs = de.wordStats.crsWords.Where(w => crsOkW.ContainsKey(w)).Any() || de.wordStats.natWords.Where(w => natOkW.ContainsKey(w)).Any();
      var isNat = de.wordStats.natWords.Where(w => crsOkW.ContainsKey(w)).Any() || de.wordStats.crsWords.Where(w => natOkW.ContainsKey(w)).Any();

      if (isCrs == isNat) continue;

      var lng = isCrs ? de.crsLang : de.natLang;
      if (de.okCrs == Langs.no) {
        de.okCrs = lng;
        de.okCrsReason = OKCrsReason.wordsOK;
      } else if (de.okCrs != lng)
        throw new Exception();
    }

    //*********************** SOUND by hand
    var soundHands = XmlUtils.FileToObject<DictEntry[]>(@"d:\LMCom\rew\Web4\RwDicts\Sources\LingeaOld\design\entriesSoundByHand.xml").ToDictionary(de => de.entryId, de => de);
    foreach (var de in dictEntries) {
      DictEntry sh;
      if (!soundHands.TryGetValue(de.entryId, out sh)) continue;
      de.soundMaster = sh.soundMaster;
    }

    //oprava nekolika hesel v kodu, kvuli soundMaster:
    foreach (var en in dictEntries) { //.SelectMany(d => d.entries)) {
      switch (en.soundMaster) {
        case "en_gb/aenge3225654": en.headWords = new string[] { "crackdown" }; en.entry.DescendantsAttr("class", "entr").First().Value = "crackdown"; break;
        case "de_de/aenge68bb654": en.soundMaster = null; break;
        case "en_gb/aenbg23cb736": en.headWords = new string[] { "checkout" }; en.entry.DescendantsAttr("class", "entr").First().Value = "checkout"; break;
        case "de_de/aenge800026b4654": if (en.headWords[0] != "einig") en.soundMaster = null; break;
        case "de_de/aenge80006d5a654": if (en.headWords[0] != "niedrig") en.soundMaster = null; break;
        case "it_it/ageit7675661": if (en.entryId == "fr_fr|de_de|porto") en.soundMaster = null; break;
        //case "it_it/aspit7b9e663": if (en.entryId == "en_gb|sp_sp|sereno") en.soundMaster = null; break;
        case "it_it/aspit7b9e663": en.soundMaster = null; break;
        case "it_it/aspite03663": if (en.entryId == "de_de|sp_sp|atar") en.soundMaster = null; break;
        case "en_gb/afren193c659": en.soundMaster = "en_gb/aenbg190c736"; break;
        case "fr_fr/afrvn9d1c688": en.soundMaster = "it_it/afrit9d1d662"; break;
        case "fr_fr/afrvna71c688": en.soundMaster = "fr_fr/afrcza71d669"; break;
        case "fr_fr/afrvnc0ea688": en.soundMaster = "fr_fr/afrczc0eb669"; break;
        //case "it_it/aspit7b9e663": en.soundMaster = "it_it/aenit8000f16c660"; break;
        case "sp_sp/asplt7692724": en.soundMaster = "sp_sp/aensp80007692656"; break;
      }
    }

    //k jednomu Headword jeden zvuk
    Dictionary<Langs, Dictionary<string, string>> headwordToSoundMaster = dictEntries.Where(de => de.soundMaster != null).GroupBy(de => de.okCrs).ToDictionary(
      g => g.Key,
      g => g.GroupBy(de => de.headWords[0].ToLower()).ToDictionary(
        sg => sg.Key,
        sg => sg.Select(d => d.soundMaster).Distinct().Single()));

    //dopln soundMaster do entries
    var cnt = dictEntries.Where(en => en.soundMaster == null).Count();
    foreach (var de in dictEntries.Where(en => en.soundMaster == null && DictLib.crsLangs.Contains(en.okCrs))) {
      string sm;
      if (!headwordToSoundMaster[de.okCrs].TryGetValue(de.headWords[0].ToLower(), out sm)) continue;
      de.soundMaster = sm;
    }
    cnt = dictEntries.Where(en => en.soundMaster == null).Count();

    //vypis word lists
    //var sourcesAfter = allLangs.ToDictionary(
    //  l => l,
    //  l => dictEntries.Where(de => de.okCrs != Langs.no).Where(de => de.bothLangs().Contains(l)).SelectMany(de => getWords(de, l)).Distinct().ToArray()
    //);
    //foreach (var src in sourcesAfter) File.WriteAllLines(string.Format(@"d:\LMCom\rew\Web4\RwDicts\Sources\LingeaOld\design\wordList_{0}.txt", src.Key), src.Value.OrderBy(w => w));

  }
  public static DictEntry[] dictEntries;

  public static IEnumerable<DictObj> repairLingea(string srcDir, Impersonator imp) {
    var allnew = XmlUtils.FileToObject<LingeaDictionary.DictEntry[]>(@"d:\LMCom\rew\Web4\RwDicts\Sources\LingeaOld\design\entriesInfo.xml");
    var allOld = LingeaDictionary.getOldLingeaEntries(srcDir).SelectMany(kv => kv.Value).SelectMany(kv => kv.Value.entryIdToEntry).ToDictionary(kv => kv.Key, kv => kv.Value);

    //k jednomu zvuku jeden headword
    Dictionary<Langs, Dictionary<string, string>> soundMasterToHeadword = allnew.Where(de => de.soundMaster != null).GroupBy(de => de.okCrs).ToDictionary(
      g => g.Key,
      g => g.GroupBy(de => de.soundMaster).ToDictionary(
        sg => sg.Key,
        sg => sg.Select(d => d.headWords[0].ToLower()).Distinct().Single()));

    //pripojeni .info souboru s headword k .mp3 souboru
    foreach (var lKv in soundMasterToHeadword) foreach (var hKv in lKv.Value) {
        string right = @"d:\LMCom\rew\Web4\RwDicts\LingeaSound\" + hKv.Key.Replace('/', '\\') + ".mp3";
        if (!File.Exists(right)) throw new Exception();
        File.WriteAllText(right.Replace(".mp3", ".info"), lKv.Key.ToString() + "=" + hKv.Value);
      }

    //vytvor nova hesla - merge entriesInfo.xml s LingeaOld.back (LingeaOld\temp1). Pridej a napln sound tag v entry
    List<DictObj> res = new List<DictObj>();
    Parallel.ForEach(allnew.Where(de => de.okCrs != Langs.no).GroupBy(dn => dn.okCrs),
      //new ParallelOptions() { MaxDegreeOfParallelism = 1 },
      crs => {
        Parallel.ForEach(crs.GroupBy(dc => dc.crsLang == dc.okCrs ? dc.natLang : dc.crsLang),
          //new ParallelOptions() { MaxDegreeOfParallelism = 1 },
          nat => {
            foreach (var newDe in nat) {
              //merge
              newDe.entry = allOld[newDe.entryId];
              //sound tag
              var snd = newDe.entry.Descendants("sound").FirstOrDefault();
              if (snd == null) {
                snd = new XElement("sound");
                var head = newDe.entry.DescendantsAttr("class", "head").First();
                var pron = head.Elements().FirstOrDefault(e => e.AttributeValue("class") == "pron");
                if (pron != null) pron.AddAfterSelf(snd);
                else {
                  var morf = head.Elements().FirstOrDefault(e => e.AttributeValue("class") == "morf");
                  if (morf != null) morf.AddAfterSelf(snd);
                  else head.Add(snd);
                }
              }
              snd.Value = (string.IsNullOrEmpty(newDe.soundMaster) ? "@" + newDe.headWords[0].ToLower() : @"RwDicts/LingeaSound/" + newDe.soundMaster + ".mp3").ToLower();
            }
            lock (res) res.Add(new DictObj { crsLang = crs.Key, natLang = nat.Key, entries = nat.Select(n => n.toNew(imp)).DistinctBy(n => n.entry.ToString()).ToArray() });
          });
      });

    return res;
  }

  //**************************** LingeaOld.back => LingeaOld\temp1
  public static void OldToNew1() {
    Regex removaBracketeEx = new Regex(@"\(.*?\)|\*|\u0301", RegexOptions.Singleline);
    Func<string, string> removeBracket = ent => removaBracketeEx.Replace(ent.ToLower(), "");
    Func<XElement, string> entryHead = ent => ent.Descendants().Where(el => el.AttributeValue("class") == "entr").First().Value;


    var entries = getOldLingeaEntries(@"d:\LMCom\rew\Web4\RwDicts\Sources\LingeaOld.back\");
    var allUsedWords = DictLib.crsLangs.ToDictionary(l => l, l => File.ReadAllLines(Machines.rootPath + string.Format(@"RwDicts\UsedWords\FlatCourseWords_{0}.txt", l)).ToDictionary(w => w, w => true));
    Func<string, string> lettersOnly = w => new String(w.ToLower().Where(ch => char.IsLetter(ch)).ToArray());

    using (StreamWriter wr = new StreamWriter(@"d:\temp\log.txt")) {
      Parallel.ForEach(entries.SelectMany(c => c.Value).Select(n => n.Value).ToArray(),
        //new ParallelOptions() { MaxDegreeOfParallelism = 1 },
        de => {
          //zaeviduj nenalezena hesla
          var notFoundEls = de.root.Elements().Where(el => !el.HasElements).ToArray();
          //de.missing = notFoundEls.Select(el => el.AttributeValue("vals")).ToArray();
          foreach (var el in notFoundEls) el.Remove();
          //found
          var founds = new XElement("entries", de.root.Elements());
          de.root.RemoveNodes(); de.root = null;
          HashSet<string> useToId = new HashSet<string>();
          foreach (var el in founds.Elements()) {
            var val = el.Attribute("val"); if (val != null) val.Remove();
            var vals = el.Attribute("vals"); vals.Remove();
            var usedIn = vals == null ? "" : vals.Value.Split('#').Distinct().Select(w => w.ToLower()).Where(w => allUsedWords[de.crsLang].ContainsKey(w)).DefaultIfEmpty().Aggregate((r, i) => r + "|" + i);
            if (usedIn != null && usedIn.Length > 0) el.Add(new XAttribute("courseUsed", usedIn));
            var idEx = removeBracket(entryHead(el));
            var id = de.crsLang.ToString() + "|" + de.nativeLang.ToString() + "|" + lettersOnly(idEx);
            var cnt = 0;
            while (useToId.Contains(id + (cnt == 0 ? null : "-" + cnt.ToString()))) cnt++;
            if (cnt > 0) id = id + "-" + (cnt++).ToString();
            useToId.Add(id);
            de.entryIdToEntry.Add(id, el);
            el.SetAttributeValue("id", id);
            el.SetAttributeValue("head", idEx);
            //replace lng sound
            var snd = el.DescendantsAttr("class", "sound").FirstOrDefault();
            if (snd != null) {
              try {
                //el.Add(new XAttribute("oldLngSound", sndFileToMainSnd[cn.crsLang][urlToFile(snd.AttributeValue("url"))].main));
                el.Add(new XAttribute("oldLngSound", urlToFile(snd.AttributeValue("url"))));
              } catch {
                lock (wr) wr.WriteLine(string.Format("{0}={1}", de.crsLang, urlToFile(snd.AttributeValue("url"))));
              }
              snd.ReplaceWith(new XElement("sound"));
            }
          }
          //cn.wordToEntryId = useToId.SelectMany(kv => kv.Value.Split('#').Distinct().Select(w => w.ToLower()).Where(w => allUsedWords[cn.crsLang].ContainsKey(w)).Select(v => new { kv.Key, Val = v })).ToDictionary(kv => kv.Val, kv => kv.Key);
        });
    }
    saveOldLingeaEntries(entries, Machines.rootPath + @"RwDicts\Sources\LingeaOld\temp1\");
  }

  //LingeaOld\temp1 => LingeaOld\design\entriesInfo.xml
  public static void OldToNew2() {
    dictEntries = null;
    getDictEntries(Machines.rootPath + @"RwDicts\Sources\LingeaOld\temp1\");
    XmlUtils.ObjectToFile(Machines.rootPath + @"RwDicts\Sources\LingeaOld\design\entriesInfo.xml", dictEntries);
    dictEntries = null;
  }

  //LingeaOld\temp1 + LingeaOld\design\entriesInfo.xml => LingeaOld
  public static void OldToNew3() {
    using (var imp = new Impersonator("pavel", "LANGMaster", "zvahov88_")) {
      var files = repairLingea(Machines.rootPath + @"RwDicts\Sources\LingeaOld\temp1\", imp);
      foreach (var fn in Directory.EnumerateFiles(Machines.rootPath + @"RwDicts\Sources\LingeaOld", "*.xml")) File.Delete(fn);
      foreach (var f in files) XmlUtils.ObjectToFile(string.Format(Machines.rootPath + @"RwDicts\Sources\LingeaOld\{1}_{0}.xml", f.crsLang, f.natLang), f);
    }
  }

  public static Dictionary<Langs, Dictionary<Langs, LingeaDictionary.LingeaDictFile>> getOldLingeaEntries(string dir = null) { //CrsLang => NativeLang => Entries
    var oldLingeaEntries = new Dictionary<Langs, Dictionary<Langs, LingeaDictionary.LingeaDictFile>>();
    if (dir == null) dir = Machines.rootPath + @"RwDicts\Sources\LingeaOld\";
    foreach (var crsLang in CommonLib.allLangs) { // crsLangs) {
      Dictionary<Langs, LingeaDictionary.LingeaDictFile> crs = null;
      foreach (var natLng in CommonLib.allLangs) { //nativeLangs) {
        //var dictFn = dir + "lingea_" + natLng.ToString() + "_" + crsLang.ToString() + ".xml";
        var dictFn = dir + natLng.ToString() + "_" + crsLang.ToString() + ".xml";
        if (!File.Exists(dictFn)) continue;
        if (crs == null) oldLingeaEntries[crsLang] = crs = new Dictionary<Langs, LingeaDictionary.LingeaDictFile>();
        var root = XElement.Load(dictFn);
        try {
          crs.Add(natLng, new LingeaDictionary.LingeaDictFile(root, crsLang, natLng));
        } catch (Exception e) {
          new Exception(dictFn, e);
        }
      }
    }
    return oldLingeaEntries;
  }
  public static void saveOldLingeaEntries(Dictionary<Langs, Dictionary<Langs, LingeaDictionary.LingeaDictFile>> entries, string dir = null) {
    saveOldLingeaEntries(entries.SelectMany(e => e.Value).Select(kv => kv.Value), dir);
  }

  public static void saveOldLingeaEntries(IEnumerable<LingeaDictionary.LingeaDictFile> entries, string dir = null) {
    if (dir == null) dir = Machines.rootPath + @"RwDicts\Sources\LingeaOld\";
    Parallel.ForEach(entries, dict => {
      //var dictFn = dir + "lingea_" + dict.nativeLang.ToString() + "_" + dict.crsLang.ToString() + ".xml";
      var dictFn = dir + dict.nativeLang.ToString() + "_" + dict.crsLang.ToString() + ".xml";
      dict.Save().Save(dictFn);
    });
  }

}

/*
public class groupSet {
      public string key { get { return _key != null ? _key : _key = values.Select(g => g.AgregateSB((sb, i) => { sb.Append('|'); sb.Append(i); })).OrderBy(w => w).AgregateSB((sb, i) => { sb.Append('#'); sb.Append(i); }); } } string _key;
      public groupSet Add(string[] newGrp) {
        for (int i = 0; i < values.Count; i++) {
          var oldGrp = values[i];
          if (!newGrp.Except(oldGrp).Any()) return this; //stara obsahuje novou nebo jsou rovny
          if (!oldGrp.Except(newGrp).Any()) { values[i] = newGrp; return this; } //nova obsahuje starou - nahrad
        }
        values.Add(newGrp); return this; //vsechny jsou odlisne => pridej
      }
      public List<string[]> values = new List<string[]>();
    }

    public class group {
      public group(string[] value) {
        this.value = value;
        key = value.AgregateSB((sb, i) => { sb.Append('|'); sb.Append(i); });
      }
      public string key;
      public string[] value;
    }
 * 
  Dictionary<Langs, HashSet<string>> stemmed = new Dictionary<Langs, HashSet<string>>();
      Dictionary<Langs, HashSet<string>> toStem = new Dictionary<Langs, HashSet<string>>();
      foreach (var l in CourseDictionary.crsLangs) {
        stemmed.Add(l, new HashSet<string>()); toStem.Add(l, new HashSet<string>(File.ReadAllLines(string.Format(@"d:\LMCom\rew\Web4\RwDicts\Sources\LingeaOld\design\wordList_{0}.txt", l))));
      }
      while (toStem.Any(s => s.Value.Count > 0)) {
        CourseDictionary.RunStemming<Dictionary<string, string>>(
          CourseDictionary.crsLangs,
          lng => { var res2 = toStem[lng].ToArray(); foreach (var w in res2) stemmed[lng].Add(w); toStem[lng].Clear(); return res2; },
          lng => null,
          (lng, word, rowStart, res2) => {
            var stemmed_lng = stemmed[lng]; var toStem_lng = toStem[lng];
            foreach (var w in rowStart) if (!stemmed_lng.Contains(w)) toStem_lng.Add(w);
          },
          (lng, res2) => { }
        );
      }
* 
 */

//******************* isUniqueNatLang
//jeden spolecny jazyk, v grupe nesmi byt hasEntryIdInTrans
//var singleNatLang = dictEntries.GroupBy(de => de.entryId).Where(g => g.Count() > 2 && !g.Any(t => t.wordStats.hasEntryIdInTrans)).Select(g => new {
//  g,
//  langs = CourseDictionary.CourseDictionary.crsLangs.Where(l => g.All(de => XExtension.Create(de.crsLang, de.natLang).Contains(l))).ToArray()
//}).Where(gl => gl.langs.Length == 1);
////jeden spolecny jazyk, neduplicitni prave strany
//var uniqueNatLang = singleNatLang.Select(gl => new { gl.g, lang = gl.langs[0] }).Select(gl => new {
//  gl.g,
//  gl.lang,
//  isUniqueNatLang = gl.g.Select(de => gl.lang == de.crsLang ? de.natLang : de.crsLang).Distinct().Count() == gl.g.Count()
//}).Where(gs => gs.isUniqueNatLang);
//foreach (var ch in uniqueNatLang) foreach (var de in ch.g) {
//    if (de.okCrs == Langs.no) {
//de.okCrs = ch.lang;
//de.okCrsReason = OKCrsReason.uniqueGroupNatLang;
////    } else if (de.okCrs != ch.lang)
//errors.Add(de); //throw new Exception();
//  }

////******************* Google detect
//var google = XmlUtils.FileToObject<List<LingeaDictionary.GoogleGelp>>(@"d:\LMCom\rew\Web4\RwDicts\Sources\LingeaOld\design\googleDetectLangs.xml").
//  Where(s => s.crsDetects != null && s.crsDetects.Length == 1 && s.natDetects != null && s.natDetects.Length == 1).
//  Select(s => new { email = s.crsLang.ToString() + s.natLang.ToString() + s.entryId, s.crsLang, s.natLang, crsDLang = s.crsDetects[0].language, natDLang = s.natDetects[0].language, crsConf = s.crsDetects[0].confidence, natConf = s.natDetects[0].confidence }).
//  Where(s => XExtension.Create(s.crsLang, s.natLang).Concat(XExtension.Create(s.crsDLang, s.natDLang)).Distinct().Count() == 2).
//  //Where(s => s.natConf > 0.75 || s.crsConf > 0.75).
//  ToDictionary(s => s.email, s => s);

//foreach (var de in dictEntries.Where(wu => wu.okCrs == Langs.no)) {
//  var key = de.crsLang.ToString() + de.natLang.ToString() + de.entryId;
//  if (!google.ContainsKey(key)) continue;
//  var g = google[key];
//  de.okCrsMaybe = g.crsDLang; de.okCrsReason = OKCrsReason.google;
//}

//if ((de.wordStats.natWordsOk > cntWord || de.wordStats.crsWordsOk > cntWord) && de.wordStats.natWordsWrong == 0 && de.wordStats.crsWordsWrong == 0) {
//  if (de.okCrs == Langs.no) {
//    de.okCrsReason = de.okCrsReason == OKCrsReason.google && de.okCrsMaybe == de.crsLang ? OKCrsReason.wordsGoogle : OKCrsReason.wordsOK;
//    de.okCrsMaybe = de.crsLang;
//  } else if (de.okCrs != de.crsLang) 
//    errors.Add(de);
//}
//if ((de.wordStats.natWordsWrong > cntWord || de.wordStats.crsWordsWrong > cntWord) && de.wordStats.natWordsOk == 0 && de.wordStats.crsWordsOk == 0) {
//  if (de.okCrs == Langs.no) {
//    de.okCrsReason = de.okCrsReason == OKCrsReason.google && de.okCrsMaybe == de.natLang ? OKCrsReason.wordsGoogle : OKCrsReason.wordsOK;
//    de.okCrsMaybe = de.natLang;
//    changed++;
//  } else if (de.okCrs != de.natLang)
//    errors.Add(de);
//}

////  if (de.okCrsMaybe == Langs.no) {
////    if ((de.wordStats.natCharsOk.Length > cntChar || de.wordStats.crsCharsOk.Length > cntChar) && de.wordStats.natCharsWrong == "" && de.wordStats.crsCharsWrong == "") {
//if (de.okCrs == Langs.no) {
//  de.okCrsMaybe = de.crsLang;
//  de.okCrsReason = OKCrsReason.charsOK;
//} else if (de.okCrs != de.crsLang) { } //throw new Exception();
////    }
////    if ((de.wordStats.natCharsWrong.Length > cntChar || de.wordStats.crsCharsWrong.Length > cntChar) && de.wordStats.natCharsOk == "" && de.wordStats.crsCharsOk == "") {
//if (de.okCrs == Langs.no) {
//  de.okCrsMaybe = de.natLang;
//  de.okCrsReason = OKCrsReason.charsOK;
//} else if (de.okCrs != de.natLang) { } //throw new Exception();
////    }
////  }

////public abstract class EntrySources {
////  public DictStemType[] stemTypes;
////  //public abstract void match(Langs crsLang, Langs natLang, HashSet<string> words, Dictionary<string, DictFoundRes> entries, Dictionary<string, DictEntry[]> stems);
////  //public abstract DictEntry match(Langs crsLang, Langs natLang, IEnumerable<string> forms);
////  public abstract Dictionary<string, DictEntry[]> init(Langs crsLang, Langs natLang);
////}

////public class LingeaOldEntrySources : EntrySources {
////  public override Dictionary<string, DictEntry> init(Langs crsLang, Langs natLang) {
////    string fn = string.Format(@"d:\LMCom\rew\Web4\RwDicts\Sources\LingeaOld\lingea_{1}_{0}.xml", crsLang, natLang);
////    if (!File.Exists(fn)) return null;
////    var dict = XmlUtils.FileToObject<DictObj>(fn);
////    return dict.entries.SelectMany(entry => entry.Stems.Where(se => se.type==DictStemType.wordId).Select(stem => new { stem = stem.key(), entry })).ToDictionary(se => se.stem, se => se.entry);
////.GroupBy(se => se.stem).ToDictionary(g => g.Key, g => g.Select(se => se.entry).ToArray());
////  }
////  //public override DictEntry match(Langs crsLang, Langs natLang, IEnumerable<string> forms) { 
////  ////public override void match(Langs crsLang, Langs natLang, HashSet<string> words, Dictionary<string, DictFoundRes> entries, Dictionary<string, DictEntry[]> stems) {
////  //  if (stems == null) return;
////  //  foreach (var word in words.ToArray()) {
////  //    //nalezne heslo dle jednoho z typu stemu
////  //    DictEntry[] dictEntries = null;
////  //    stemTypes.Select(st => stems.TryGetValue(DictStem.getKey(st, word), out dictEntries) ? dictEntries : null).Where(x => dictEntries != null).FirstOrDefault();
////  //    if (dictEntries == null) continue;
////  //    words.Remove(word);
////  //    DictFoundRes entry;
////  //    foreach (var dictEntry in dictEntries) {
////  string key = DictFoundRes.getKey(DictEntryType.lingeaOld, dictEntry.entryId);
////  if (!entries.TryGetValue(key, out entry)) entries.Add(key, entry = new DictFoundRes { entry = dictEntry/*, type = DictEntryType.lingeaOld*/ });
////  entry.words.Add(word);
////  //    }
////  //  }
////  //}
////  //public ILookup<string, DictEntry> initTest(Langs crsLang, Langs natLang) {
////  //  string fn = string.Format(@"d:\LMCom\rew\Web4\RwDicts\Sources\LingeaOld\lingea_{1}_{0}.xml", crsLang, natLang);
////  //  if (!File.Exists(fn)) return null;
////  //  var dict = XmlUtils.FileToObject<DictObj>(fn);
////  //  //using (StreamWriter wr = new StreamWriter(string.Format(@"d:\temp\multi_{1}_{0}.txt", crsLang, natLang)))
////  //  //foreach (var multi in dict.entries.DistinctBy(en => en.entry.ToString()).Select(entry => new { entry, stems = entry.Stems.Select(s => s.word).OrderBy(w => w).Aggregate((r, i) => r + "|" + i) }).GroupBy(es => es.stems).Where(g => g.Count() > 1)) {
////  //  //  wr.WriteLine();
////  //  //  wr.WriteLine("*********** " + multi.Key);
////  //  //  foreach (var en in multi) wr.WriteLine(en.entry.entry.ToString());
////  //  //}
////  //  //return null;
////  //  return dict.entries.SelectMany(entry => entry.Stems.Where(s => s.type == DictStemType.stem).Select(stem => new { entry, stem })).ToLookup(es => es.stem.word, es => es.entry);
////  //}
////  //public void matchTest(Langs crsLang, Langs natLang, HashSet<string> words, Dictionary<string, DictFoundRes> entries, ILookup<string, DictEntry> stems,  Dictionary<string, DictEntry[]> stemsOld) {
////  //  if (stems == null) return;
////  //  using (var imp = new Impersonator("pavel", "LANGMaster", "zvahov88_")) {
////  //    CourseDictionary.RunStemming<StringBuilder>(
////  XExtension.Create(crsLang),
////  lng => words.OrderBy(w => w).Distinct(),
////  lng => new StringBuilder(),
////  (lng, word, rowStart, res2) => {
////    DictEntry[] oldEn = null; string oldEntryId = null;
////    if (stemsOld.TryGetValue(DictStem.getKey(DictStemType.courseUses, word), out oldEn)) {
////      oldEntryId = oldEn.Single().entryId;
////    }

////    var wordSt = rowStart.OrderBy(w => w).ToArray();
////    var wordStems = wordSt.Aggregate((r, i) => r + "|" + i);
////    var founds = wordSt.Select(wordStem => stems[wordStem].ToArray()).ToArray();
////    var foundEntries = wordSt.SelectMany(wordStem => stems[wordStem].Select(entry => new {
////      wordStem,
////      entry,
////      stems = entry.Stems.Where(s => s.type == DictStemType.stem).Select(s => s.word).ToArray(),
////      stemsTxt = entry.Stems.Where(s => s.type == DictStemType.stem).Select(s => s.word).OrderBy(w => w).Aggregate((r, i) => r + "|" + i)
////    })).DistinctBy(en => en.entry.entry.ToString()).ToArray();
////    var exact = foundEntries.FirstOrDefault();
////    if (foundEntries.Length > 1) {
////      exact = foundEntries.FirstOrDefault(fe => fe.entry.Stems.Where(s => s.type == DictStemType.wordId).Any(s => s.word == word)); //word je v entry headwords
////      if (exact == null) exact = foundEntries.FirstOrDefault(fe => fe.stems.Length==wordSt.Length && fe.stems.Except(wordSt).Count() == 0); //entry and word stems are equals
////      if (exact == null) exact = foundEntries.MinByOrDefault(fe => fe.stems.Intersect(wordSt).Count() / (fe.stems.Except(wordSt).Count()+1)); //lepsi pomer stejnych entry and word stems
////    }

////    if (exact!=null && exact.entry.entryId!=oldEntryId) {
////      res2.AppendLine();
////      res2.Append("********************* "); res2.Append(word); res2.Append(", entry="); res2.Append(exact.entry.entry.DescendantsAttr("class", "entr").First().Value);
////      res2.Append(" >>> exactStems =");res2.Append(exact.stemsTxt);
////      res2.Append(" >>> wordStems ="); res2.Append(wordStems);
////      res2.Append(" >>> oldEntryId="); res2.AppendLine(oldEntryId);
////      foreach (var fe in foundEntries.Where(fe => fe != exact)) {
////        res2.Append(fe.entry.entry.DescendantsAttr("class", "entr").First().Value);
////        res2.Append(" >>> ");
////        res2.AppendLine(fe.stemsTxt);
////      }
////    }
////  },
////  (lng, res2) => File.WriteAllText(string.Format(@"d:\temp\notExact_{1}_{0}.txt", crsLang, natLang), res2.ToString()),
////  imp
////  //    );
////  //  }
////  //  //foreach (var word in words.ToArray()) {
////  //  //  //nalezne heslo dle jednoho z typu stemu
////  //  //  DictEntry[] en = null;
////  //  //  stemTypes.Select(st => stems.TryGetValue(DictStem.getKey(st, word), out en) ? en : null).Where(x => en != null).FirstOrDefault();
////  //  //  if (en == null) continue;
////  //  //  words.Remove(word);
////  //  //  DictFoundRes entry;
////  //  //  foreach (var e in en) {
////  //  //    string key = DictFoundRes.getKey(DictEntryType.lingeaOld, e.entryId);
////  //  //    if (!entries.TryGetValue(key, out entry)) entries.Add(key, entry = new DictFoundRes { entry = e, type = DictEntryType.lingeaOld });
////  //  //    entry.words.Add(word);
////  //  //  }
////  //  //}
////  //}
////}

////public class RjSources : EntrySources {
////  //public override DictEntry match(Langs crsLang, Langs natLang, IEnumerable<string> forms) { return null; }
////  public override Dictionary<string, DictEntry> init(Langs crsLang, Langs natLang) { return null; }
////}

////public class UltralinguaSources : EntrySources {
////  //public override DictEntry match(Langs crsLang, Langs natLang, IEnumerable<string> forms) { return null; }
////  public override Dictionary<string, DictEntry> init(Langs crsLang, Langs natLang) { return null; }
////}

////pro zadane vety, zdrojovy a cilove jazyky pripravi slovnik
////public static void OpravaLingeaSounds() {
////  foreach (var lng in crsLangs) LowUtils.AdjustDir(string.Format(@"d:\LMCom\rew\Web4\RwDicts\LingeaSound\{0}", lng.ToString()));
////  foreach (var lng in crsLangs) {
////    string dir = string.Format(@"d:\LMCom\rew\Web4\RwDicts\LingeaSound_old\{0}", lng.ToString());
////    var files = LingeaSndFiles.addSoundsStart(lng);
////    foreach (var mp3Fn in Directory.EnumerateFiles(dir, "*.mp3")) {
//files.addSound(Path.GetFileNameWithoutExtension(mp3Fn), File.ReadAllBytes(mp3Fn), File.ReadAllText(Path.ChangeExtension(mp3Fn, ".txt")));
////    }
////    files.addSoundsEnd();
////  }
////}

////public static void CaptureOldLingeaSound() {
////  var dicts = allDicts;//lingeaWords.Keys.SelectMany(nativeLang => lingeaWords[nativeLang].Keys.Select(crsLang => new { nativeLang, crsLang }));
////  var wordUrls = dicts.
////    SelectMany(d => IdToEntry[d.nativeLang][d.crsLang].
//SelectMany(el => el.Value.DescendantsAttr("url").Select(a => new { crsLang = d.crsLang, url = a.Value.ToLower(), entryId = el.Key }))).
//GroupBy(w => new { w.crsLang, w.url }).
//ToArray();
////  Parallel.ForEach(wordUrls.GroupBy(wu => wu.Key.crsLang).ToArray(), crsDict => { //crsDict.key je crsLang, items jsou grupy <crsLang, url> => entryId's
////    var dictWordId = crsDict.Select(w => new {
//url = w.Key.url,
//email = w.Key.url.Replace(".mp3", null).Split('/').Last(),
////word = w.SelectMany(t => t.entryId.ToLower().Replace("[", null).Replace("]", null).Split(new char[] { '#', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)).Distinct().Single() });
//wordInstances = w.SelectMany(t => t.entryId.ToLower().Split('#')).Distinct() //.Aggregate((r, i) => r + "#" + i)
////    });
////    var dictWordIdGrp = dictWordId.GroupBy(lg => lg.email);
////    var files = LingeaSndFiles.addSoundsStart(crsDict.Key);
////    foreach (var w in dictWordIdGrp) {
//var fileName = w.Key.ToLower();
//if (files.findViaFileName(fileName) != null) continue;
//var url = w.First().url;
//var data = LowUtils.Download(url);
//var word = w.SelectMany(t => t.wordInstances).Distinct().Aggregate((r, i) => r + "#" + i);//.Single();
//files.addSound(fileName, data, word);
////    }
////    files.addSoundsEnd();
////  });
////}

////public static void AnalyzeAndNormalizeLingea() {
////  //Func<string, string> encodeRussianEkcent = s => {
////  //  char[] chars = s.ToCharArray();
////  //  for (int i = chars.Length - 2; i >= 0; i--) {
////  //    if (chars[i] != wrongAccent) continue;
////  //    var ch = chars[i]; chars[i] = chars[i + 1]; chars[i + 1] = RwSound.russianAccent;
////  //  }
////  //  return new string(chars);//.Replace(RwSound.russianAccent.ToString(), "&#0769;");
////  //};
////  //foreach (var file in Directory.EnumerateFiles(@"d:\LMCom\rew\Web4\RwDicts", "*ru_ru*.xml")) {
////  //  XElement root = XElement.Load(file);
////  //  foreach (var nd in root.DescendantNodes().OfType<XText>()) nd.Value = encodeRussianEkcent(nd.Value);
////  //  root.Save(file);
////  //}
////  throw new Exception("Obsolete");
////  //NormalizeUsedWords(CourseDictionary.nativeLangs, XmlUtils.FileToObject<schools.DictWords>(Machines.basicPath + @"rew\Web4\RwDicts\CourseWords.xml"));
////  //analyzeLingeaDictionary();
////}

//////vystup vsech v kurzech pouzitych slov vcetne ohybu
////static void NormalizeUsedWords(IEnumerable<Langs> nativeLangs, DictWords allUsedWords) {
////  string outputDir = @"d:\LMCom\rew\Web4\RwDicts\normalize\";
////  foreach (var langCrs in allUsedWords.courses) {
////    var courseLang = langCrs.lang;
////    var usedWords = langCrs.exs.SelectMany(testEx => testEx.words).Distinct();
////    Dictionary<string, HashSet<string>> normalized = new Dictionary<string, HashSet<string>>();
////    foreach (var nativeLang in nativeLangs) {
//if (!allDicts.Any(d => d.nativeLang == nativeLang && d.crsLang == courseLang)) continue;
//var allIdToEntry = IdToEntry[nativeLang][courseLang];
//var allWordToId = WordToId[nativeLang][courseLang];
//Func<string, bool> hasKey = w => {
//  string email; if (!allWordToId.TryGetValue(w, out email)) return false;
//  return allIdToEntry[email].HasElements;
//};
//var hasEntries = usedWords.Select(w => new { word = w, hasEntry = hasKey(w) }).ToArray();
//File.WriteAllLines(string.Format(outputDir + "normalized_ok_{0}_{1}.txt", courseLang, nativeLang), hasEntries.Where(e => e.hasEntry).Select(e => e.word).OrderBy(n => n));
//File.WriteAllLines(string.Format(outputDir + "normalized_wrong_{0}_{1}.txt", courseLang, nativeLang), hasEntries.Where(e => !e.hasEntry).Select(e => e.word).OrderBy(n => n));

//foreach (var wordEntry in hasEntries.Where(e => e.hasEntry).Select(e => new { word = e.word, entry = allIdToEntry[allWordToId[e.word]] })) {
//  var head = wordEntry.entry.Descendants().Where(el => el.AttributeValue("class") == "entr").Select(n => n.Value.TrimEnd('1', '*')).First();
//  HashSet<string> stems;
//  if (!normalized.TryGetValue(head, out stems)) normalized.Add(head, stems = new HashSet<string>());
//  stems.Add(wordEntry.word);
//}
////    }
////    File.WriteAllLines(string.Format(outputDir + "normalized_{0}.txt", courseLang), normalized.Select(kv => kv.Key + ": " + kv.Value.Aggregate((r, i) => r + ", " + i)));
////  }
////}

//////analyza dostupnych Lingea slovniku. Vstupem je d:\LMCom\rew\Web4\RwDicts\analyse\LingeaSeznam.txt, vystupem d:\LMCom\rew\Web4\RwDicts\analyse\dictInfoNew.xml
////static void analyzeLingeaDictionary() {
////  string dictAnalDir = @"d:\LMCom\rew\Web4\RwDicts\analyse\";
////  var newCodes = File.ReadAllLines(dictAnalDir + "LingeaSeznam.txt").Select(l => l.Split('"')[1].Replace("_", null)).ToArray();
////  File.WriteAllLines(dictAnalDir + "newCodes.txt", newCodes.OrderBy(n => n));
////  var newDicts = newCodes.Select(c => new { nat = c.Substring(0, 2), crs = c.Substring(2, 2) });
////  var allLang = newDicts.SelectMany(nc => new string[] { nc.nat, nc.crs }).Distinct().OrderBy(n => n);

////  var dicts = XmlUtils.FileToObject<DictInfo>(Machines.basicPath + @"rew\Web4\RwDicts\DictInfos.xml").Dicts.Where(d => d.Code != null);
////  var infos = dicts.Select(di => new { crs = di.From, nat = di.To, crsC = di.Code.Substring(0, 2), natC = di.Code.Substring(2, 2) }).ToArray();
////  Dictionary<string, Langs> langCodes = new Dictionary<string, Langs>();
////  foreach (var info in infos) { langCodes[info.crsC] = info.crs; langCodes[info.natC] = info.nat; }
////  var unknownLang = allLang.Except(langCodes.Keys);
////  langCodes["ee"] = Langs.et_ee;
////  langCodes["fi"] = Langs.fi_fi;
////  langCodes["lv"] = Langs.lv_lv;
////  langCodes["no"] = Langs.nb_no;
////  langCodes["se"] = Langs.sv_se;
////  langCodes["sr"] = Langs.sr_latn_cs;
////  File.WriteAllLines(dictAnalDir + "unknownLang.txt", unknownLang.OrderBy(n => n));
////  File.WriteAllLines(dictAnalDir + "langCodes.txt", allLang.Select(l => l + "=" + langCodes[l].ToString()).OrderBy(n => n));

////  File.WriteAllLines(dictAnalDir + "allLang.txt", allLang);
////  var allDict = allLang.SelectMany(nat => allLang.Select(crs => new { nat, crs })).Where(nc => nc.nat != nc.crs);

////  var allValid = allDict.Where(nc => Array.IndexOf<string>(newCodes, nc.nat + nc.crs) >= 0 || Array.IndexOf<string>(newCodes, nc.crs + nc.nat) >= 0);
////  File.WriteAllLines(dictAnalDir + "allValid.txt", allValid.Select(nc => nc.nat + nc.crs).OrderBy(n => n));

////  var missing = allValid.Select(nc => nc.nat + nc.crs).Except(newCodes);
////  File.WriteAllLines(dictAnalDir + "missing.txt", missing.OrderBy(n => n));

////  List<DictInfo.DictObj> validDicts = newDicts.Select(nd => new DictInfo.DictObj() {
////    From = langCodes[nd.crs],
////    To = langCodes[nd.nat],
////    Code = nd.nat + nd.crs,
////    Native = true,
////  }).ToList();
////  foreach (var m in missing) validDicts.Add(new DictInfo.DictObj() {
////    From = langCodes[m.Substring(2, 2)],
////    To = langCodes[m.Substring(0, 2)],
////    Code = m.Substring(2, 2) + m.Substring(0, 2),
////  });
////  XmlUtils.ObjectToFile(dictAnalDir + "dictInfoNew.xml", new DictInfo() { Dicts = validDicts.ToArray() });

////  var newAll = validDicts.Select(d => d.Code);
////  if (newAll.Except(newCodes).Count() > 0) throw new Exception();
////  if (newCodes.Except(newAll).Count() > 0) throw new Exception();
////  var FromTos = validDicts.Select(d => d.From.ToString() + d.To.ToString());
////  var dupls = FromTos.GroupBy(n => n).Where(g => g.Count() > 1).ToArray();
////  if (FromTos.Count() != FromTos.Distinct().Count()) throw new Exception();

////  var crsNat = newDicts.Select(c => new { nat = c.crs, crs = c.nat });
////  var nats = newDicts.Select(nc => nc.nat).Distinct().OrderBy(n => n);
////  var crss = newDicts.Select(nc => nc.crs).Distinct().OrderBy(n => n);
////  File.WriteAllLines(dictAnalDir + "allLang.txt", allLang);
////  File.WriteAllLines(dictAnalDir + "nats.txt", nats);
////  File.WriteAllLines(dictAnalDir + "crss.txt", crss);
////  File.WriteAllLines(dictAnalDir + "nats-crss.txt", newDicts.Except(crsNat).Select(nc => nc.nat + nc.crs).OrderBy(n => n));
////  File.WriteAllLines(dictAnalDir + "crss-nats.txt", crsNat.Except(newDicts).Select(nc => nc.crs + nc.nat).OrderBy(n => n));
////}

////public static void LingeaToModulesAll() {
////  throw new Exception("Obsolete");
////  //LingeaToModules(CourseDictionary.nativeLangs, XmlUtils.FileToObject<schools.DictWords>(Machines.basicPath + @"rew\Web4\RwDicts\CourseWords.xml"), null);
////  //usedWords(string dictFn) { //@"rew\Web4\RwDicts\" + f + ".xml"
////  //foreach (var lng in CourseDictionary.nativeLangs) LingeaToModules(lng);
////}

//////obsolete
////public static void LingeaOldToGrafiaModules() {
////  var modules = ProductsDefine.Def.generateGrafiaProducts(ProductsDefine.lib.GrafiaCount).OfType<ProductsDefine.productDescrNew>().SelectMany(p => p.Modules()).ToArray();
////  LingeaOldToModules(Langs.cs_cz, new Langs[] { Langs.de_de }, lng => modules.Select(m => modExs.formMod(m)), hideLingea);
////}

//////obsolete
////static void LingeaOldToModules(Langs nativeLang) {
////  LingeaOldToModules(nativeLang, crsLangs, lng => Exercise.EAModules(ProductsDefine.lib.root).Where(m => CommonLib.CourseIdToLang(m.crsId) == lng).Select(m => modExs.formMod(m)), null);
////}