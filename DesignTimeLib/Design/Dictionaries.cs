//http://www.computerhope.com/robocopy.htm
//robocopy s:\LMCom\SoundSources\ q:\LMCom\rew\SoundSources\ *.mp3 *.wav /s -XD src2 
/*
SQL Server word breking and stemming

Dostupne jazyky:
select * from sys.fulltext_languages

SELECT  *  FROM sys.dm_fts_parser ('FORMSOF( FREETEXT, "koněm")', 1036, 0, 1)  
SELECT * FROM sys.dm_fts_parser (N'FORMSOF ( FREETEXT, "''берлинский")', 1049, 0, 1)

*/

using LMComLib;
using LMNetLib;
using Newtonsoft.Json;
using schools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

public static class DictLib {

  public static IDictionary<CourseMeta.dictTypes, DictForCourse.dictOptions> dictOptions = new DictForCourse.dictOptions[] {
    new DictForCourse.dictOptions { FriendlyId = CourseMeta.dictTypes.L, forAllCourses = false, lingeaOnly = true }
  }.ToDictionary(d => d.FriendlyId, d => d);

  public struct dictId {
    public Langs crsLang; public Langs natLang; public string prefix;
    public static dictId parse(string s, string prefix) {
      dictId dict = new dictId { prefix = prefix }; if (prefix != null) s = s.Substring(prefix.Length);
      foreach (var lng in LowUtils.EnumGetValues<Langs>()) {
        var ls = langStr[lng] + "_";
        if (s.StartsWith(ls)) {
          dict.natLang = lng; string subS = s.Substring(ls.Length);
          foreach (var toLng in LowUtils.EnumGetValues<Langs>()) {
            if (subS == langStr[toLng]) { dict.crsLang = toLng; return dict; }
          }
        }
      }
      throw new Exception(s);
    }
    public string fileName(string basicPath) { return basicPath + string.Format("{0}{1}_{2}.xml", prefix, natLang, crsLang); }
    static Dictionary<Langs, string> langStr = LowUtils.EnumGetValues<Langs>().ToDictionary(l => l, l => l.ToString());
  }

  public static IEnumerable<dictId> allDicts(string basicPath, string prefix = null) {
    return Directory.EnumerateFiles(basicPath, prefix + "*.xml").Select(fn => fn.Replace(".xml", null).Substring(basicPath.Length)).Select(s => dictId.parse(s, prefix));
  }
  public static DictObj readDict(string basicPath, dictId id) {
    return XmlUtils.FileToObject<DictObj>(id.fileName(basicPath));
  }

  public static void exportWordTypes() {
    //KDict
    var items = DictK.Lib.readDict().SelectMany(en => en.dictEntries).SelectMany(de => de.descendants());
    File.WriteAllLines(@"d:\temp\kdict.txt", items.OfType<DictK.MPartOfSpeech>().Select(p => p.content).Distinct().OrderBy(w => w));
    //UltraLingua
    var ulDicts = DictLib.allDicts(@"d:\LMCom\rew\Web4\RwDicts\Sources\Ultralingua.back\", "dict_").Select(d => Ultralingua.Lib.readDict(d));
    File.WriteAllLines(@"d:\temp\UltraLingua.txt", ulDicts.SelectMany(dict => dict.Entries.SelectMany(en => en.Body.DescendantsAttr("class", "partofspeech").Select(e => e.Value))).Distinct().OrderBy(w => w));
    //Lingea
    var lingDicts = DictLib.allDicts(@"d:\LMCom\rew\Web4\RwDicts\Sources\LingeaOld\").Select(d => LingeaDictionary.readDict(d));
    File.WriteAllLines(@"d:\temp\lingea.txt", lingDicts.SelectMany(dict => dict.entries.SelectMany(en => en.entry.DescendantsAttr("class", "morf").Select(e => e.Value))).Distinct().OrderBy(w => w));
    //Wiki
    var wikiDicts = DictLib.allDicts(@"d:\LMCom\rew\Web4\RwDicts\Sources\Wiktionary\", "dict_").Select(d => Wikdionary.Lib.readDict(d));
    File.WriteAllLines(@"d:\temp\wiki.txt", wikiDicts.SelectMany(dict => dict.entries.SelectMany(en => en.entry.DescendantsAttr("class", "morf").Select(e => e.Value))).Distinct().OrderBy(w => w));
    //RJ
    var rjDicts = DictLib.allDicts(@"d:\LMCom\rew\Web4\RwDicts\Sources\RJ\").Select(d => DictLib.readDict(@"d:\LMCom\rew\Web4\RwDicts\Sources\RJ\", d));
    File.WriteAllLines(@"d:\temp\rj.txt", rjDicts.SelectMany(dict => dict.entries.SelectMany(en => en.entry.DescendantsAttr("class", "morf").Select(e => e.Value))).Distinct().OrderBy(w => w));
  }

  public static IEnumerable<string> RunStemming(Langs crsLang, string word) {
    if (string.IsNullOrEmpty(word)) return Enumerable.Empty<string>();
    DataSet ds = new DataSet();
    adjustLcids();
    var lcid = CommonLib.LangToLCID(crsLang);
    if (!allLCIDs.Any(l => l == lcid)) return Enumerable.Empty<string>();
    var q = string.Format("SELECT  *  FROM sys.dm_fts_parser (N'FORMSOF( FREETEXT, \"{0}\")', {1}, 0, 1)", (crsLang == Langs.ru_ru ? normalizeRussian(word) : word).Replace("'", "''"), lcid);
    ds.Clear();
    using (SqlConnection subconn = new SqlConnection(connStr))
    using (SqlDataAdapter adapter = new SqlDataAdapter { SelectCommand = new SqlCommand(q, subconn) }) adapter.Fill(ds);
    return ds.Tables[0].Rows.Cast<DataRow>().Select(r => (string)r["display_term"]).Distinct().OrderBy(s => s);
  }
  const string connStr = @"Data Source=pz-w8virtual\SQLserver;Initial Catalog=fulltext;Integrated Security=true";
  static int[] allLCIDs;
  static void adjustLcids() {
    if (allLCIDs == null) lock (typeof(DictLib)) if (allLCIDs == null) using (SqlConnection conn = new SqlConnection(connStr)) {
            conn.Open();
            DataSet dsLcids = new DataSet { Locale = CultureInfo.InvariantCulture };
            using (SqlDataAdapter adapter = new SqlDataAdapter { SelectCommand = new SqlCommand("select * from sys.fulltext_languages", conn) }) adapter.Fill(dsLcids);
            allLCIDs = dsLcids.Tables[0].Rows.Cast<DataRow>().Select(r => (int)r["lcid"]).ToArray();
          }
  }
  public static void RunStemming<T>(IEnumerable<Langs> langs, Func<Langs, IEnumerable<string>> langWords, Func<Langs, T> langStart, Action<Langs, string, IEnumerable<string>, T> langPush, Action<Langs, T> langEnd, Impersonator imp) {
    try {
      adjustLcids();
      Parallel.ForEach(
        langs,
        //new ParallelOptions { MaxDegreeOfParallelism = 1 },
        crsLang => {
          var lcid = CommonLib.LangToLCID(crsLang);
          if (!allLCIDs.Any(l => l == lcid)) return;
          var usedWords = langWords(crsLang);
          var rs = langStart(crsLang);
          var blocks = new ConcurrentDictionary<T, bool>();
          Parallel.ForEach(usedWords,
            //new ParallelOptions { MaxDegreeOfParallelism = 1 },
            word => {
              if (string.IsNullOrEmpty(word)) return;
              DataSet ds = new DataSet();
              var q = string.Format("SELECT  *  FROM sys.dm_fts_parser (N'FORMSOF( FREETEXT, \"{0}\")', {1}, 0, 1)", (crsLang == Langs.ru_ru ? normalizeRussian(word) : word).Replace("'", "''"), lcid);
              ds.Clear();
              using (WindowsIdentity.Impersonate(imp.token))
              using (SqlConnection subconn = new SqlConnection(connStr))
              using (SqlDataAdapter adapter = new SqlDataAdapter { SelectCommand = new SqlCommand(q, subconn) }) adapter.Fill(ds);
              var items = ds.Tables[0].Rows.Cast<DataRow>().Select(r => (string)r["display_term"]).Distinct().OrderBy(s => s);
              if (items.Any()) lock (usedWords) langPush(crsLang, word, items, rs);
            });
          langEnd(crsLang, rs);
        });
    } catch (Exception exp) {
      if (exp == null) return;
    }
  }

  public static void RunStemming() {
    using (var imp = new Impersonator("pavel", "LANGMaster", "zvahov88_"))
      RunStemming<List<string>>(
        crsLangs,
        lng => LingeaDictionary.wordsForCourse(XmlUtils.FileToObject<schools.DictCrsWords>(string.Format(@"d:\LMCom\rew\Web4\RwDicts\UsedWords\CourseWords_{0}.xml", lng))).Select(w => w.word.ToLower().Trim()).Distinct().ToArray(),
        lng => new List<string>(),
        (lng, word, row, res) => res.Add(row.AgregateSB((sb, i) => { sb.Append("|"); sb.Append(i); })),
        (lng, res) => File.WriteAllLines(string.Format(@"d:\LMCom\rew\Web4\RwDicts\UsedWords\CourseWordsStems_{0}.txt", lng), res),
        imp
      );
  }

  public static void ExportUsedWords() {
    foreach (var courseLang in crsLangs) {
      var fn = string.Format(@"d:\LMCom\rew\Web4\RwDicts\UsedWords\CourseWordsFlat_{0}.txt", courseLang);
      File.WriteAllLines(fn,
        LingeaDictionary.wordsForCourse(XmlUtils.FileToObject<schools.DictCrsWords>(string.Format(@"d:\LMCom\rew\Web4\RwDicts\UsedWords\CourseWords_{0}.xml", courseLang))).Select(w => w.word.ToLower().Trim()).OrderBy(w => w).Distinct()
      );
    }
  }

  public static Langs[] crsLangs = new Langs[] { Langs.en_gb, Langs.de_de, Langs.fr_fr, Langs.sp_sp, Langs.it_it, Langs.ru_ru };
  //public static Langs[] nativeLangs = CommonLib.bigLocalizations;
  public static Dictionary<char, string> wrongCyrilic = new Dictionary<char, string>() { { 'á', "а\x301" }, { 'a', "а" }, { 'p', "р" }, { 'e', "е" }, { 'y', "у" }, { 'c', "с" }, { 'ë', "ё" }, { 'ý', "у\x301" }, { 'é', "е\x301" }, { 'x', "х" }, { 'ó', "о\x301" }, { 'm', "м" }, { 'o', "о" }, };

  public static string normalizeRussian(string s) {
    string rep;
    return s.Select(ch => wrongCyrilic.TryGetValue(ch, out rep) ? rep : ch.ToString()).Aggregate((r, i) => r + i);
  }

  public static string russianRemoveAkcent(Langs courseLang, string v) { return courseLang == Langs.ru_ru ? v.ToLower().Replace('á', 'a').Replace('é', 'e').Replace('í', 'i').Replace('ó', 'o').Replace('ú', 'u').Replace('ý', 'y') : v.ToLower(); }

  const char wrongAccent = '\x00B4';


  //musi funkcne odpovidat d:\LMCom\rew\Web4\JsLib\Controls\Dict\Dict.ts, wordsForDesignTime
  public static IEnumerable<string> wordsForDesignTime(string sent, Langs crsLang) {
    if (string.IsNullOrEmpty(sent)) yield break;
    sent = sent.ToLower();
    if (crsLang == Langs.ru_ru) sent = normalizeRussian(sent); List<char> word = new List<char>();
    for (int i = 0; i <= sent.Length; i++) {
      char ch = i < sent.Length ? sent[i] : ' '; var isWord = isWordChar(ch);
      if (isWord) word.Add(ch);
      else if (word.Count > 0 && !isWord) { yield return new String(word.ToArray()); word.Clear(); }
    }
  }
  const char russianAccent = '\u0301';
  static bool isWordChar(char ch) { return ch == russianAccent || char.IsLetter(ch) || ch == '-'; }
}

//Lingea like heslo
public class DictEntryObj {
  [XmlAttribute]
  public string entryId;
  [XmlAttribute]
  public schools.DictEntryType type;
  //[XmlAttribute]
  [XmlIgnore]
  public string soundMaster;
  public XElement entry;
  public string[] headWords;
  public string[] courseWords;
}

//Lingea like slovnik
public class DictObj {
  public Langs crsLang;
  public Langs natLang;
  public DictEntryObj[] entries;
}

public static class DictForCourse {

  //pomocne heslo pro course-link
  public class DictFoundRes {
    public int entryCount; //jednoznacne cislo entry
    public schools.DictEntryType type { get { return entry.type; } }
    public DictEntryObj entry;
    //public string soundMaster; //prirazeny zvuk
    public HashSet<string> words = new HashSet<string>();
    public string key() { return getKey(type, entry.entryId); }
    public static string getKey(schools.DictEntryType type, string word) { return ((int)type).ToString() + word; }
  }

  //spolecny option pro course -> dict link
  public class dictOptions {
    public dictOptions() {
      string dirFn = Machines.rootPath + @"RwDicts\LingeaSound\dir.txt";
      if (!File.Exists(dirFn)) {
        var temp = Directory.EnumerateFiles(Machines.rootPath + @"RwDicts\LingeaSound", "*.info", SearchOption.AllDirectories).
          Select(fn => fn.Split('\\').Reverse().Take(2).Concat(File.ReadAllText(fn).Split('=')).ToArray()).
          Select(parts => new { id = parts[1] + "/" + parts[0].Replace(".info", null), crsLang = LowUtils.EnumParse<Langs>(parts[2]), word = parts[3] }).ToArray();
        sounds = temp.GroupBy(s => s.crsLang).ToDictionary(g => g.Key, g => g.GroupBy(cg => cg.word.ToLower()).ToDictionary(cg => cg.Key, cg => cg.First().id));
        File.WriteAllLines(dirFn, sounds.SelectMany(kv => kv.Value.Select(skv => kv.Key.ToString() + "|" + skv.Key + "|" + skv.Value)).OrderBy(w => w));
      } else {
        sounds = File.ReadAllLines(dirFn).Select(l => l.Split('|')).
          Select(parts => new {
            crsLang = LowUtils.EnumParse<Langs>(parts[0]),
            text = parts[1],
            url = parts[2]
          }).
          GroupBy(l => l.crsLang).
          ToDictionary(g => g.Key, g => g.ToDictionary(tu => tu.text, tu => tu.url));
      }
    }
    public CourseMeta.dictTypes FriendlyId; //rozliseni d:\LMCom\rew\Web4\Schools\EAData\cs-cz\english5_xl02_sb_shome_dhtm_<FriendlyId>.json/rjson/txt souboru
    public bool lingeaOnly; //Lingea forms, pouze Lingea slovnik
    public DictEntryType[] dictTypes; //obecne forms, obecne slovniky
    public bool forAllCourses; //pro ladeni - jeden slovnik pro cely kurz
    public Dictionary<Langs, Dictionary<string, string>> sounds; //tabulka crsLang, text zvuku, URL zvuku
    public int errSound;
    public dictCrsData getCrsData(Langs crsLang) {
      dictCrsData res;
      lock (this) if (!crsData.TryGetValue(crsLang, out res)) if (!crsData.TryGetValue(crsLang, out res)) crsData.Add(crsLang, res = new dictCrsData(crsLang, this));
      return res;
    }
    Dictionary<Langs, dictCrsData> crsData = new Dictionary<Langs, dictCrsData>();

    public void createDict(Langs crsLang, Langs natLang, IEnumerable<string> words, CourseMeta.CacheDict res) {
      var crsData = getCrsData(crsLang);
      var forms = crsData.getWordsForms(words.Distinct().OrderBy(w => w));
      crsData.getNatData(natLang).dictForText(forms, res);
    }

  }

  public class getDictResult {
    public schools.Dict dict;
    public HashSet<string> sounds = new HashSet<string>(); //seznam pouzitych zvuku
    public HashSet<string> notFound = new HashSet<string>(); //seznam ohybu, ke kterym se nenaslo entry
  }

  //data pro jeden crsLang pro course-link
  public class dictCrsData {
    public dictCrsData(Langs crsLang, dictOptions options) {
      this.options = options; this.crsLang = crsLang;
      var formsFn = string.Format(Machines.rootPath + @"RwDicts\Forms\{0}forms_{1}.xml", options.lingeaOnly ? "ling_" : null, crsLang);
      try {
        forms = LookupLib.fromStrings(File.ReadAllLines(formsFn));
      } catch {
        throw;
      }
      sounds = options.sounds[crsLang];
    }
    public dictOptions options;
    public Langs crsLang;
    ILookup<string, string> forms; //tabulka Ohyb => zakladni tvary
    public Dictionary<string, List<string>> getWordsForms(IEnumerable<string> words) { //words jsou ohyby, ke kterym se hleda heslo. Da k nim sebe, sebe.toLowerCase a zakladni tvary
      return words.ToDictionary(w => w, w => XExtension.Create(w, w.ToLower()).Concat(forms[w]).Distinct().ToList());
    }
    public Dictionary<string, string> sounds; //tabulka text zvuku, URL zvuku
    public dictNatData getNatData(Langs natLang) {
      dictNatData res;
      lock (this) if (!natData.TryGetValue(natLang, out res)) if (!natData.TryGetValue(natLang, out res)) natData.Add(natLang, res = new dictNatData(natLang, this));
      return res;
    }
    Dictionary<Langs, dictNatData> natData = new Dictionary<Langs, dictNatData>();
  }

  //data pro jeden slovnik (crsLang/natLang par) pro course-link
  public class dictNatData {
    public dictNatData(Langs natLang, dictCrsData crsData) {
      this.crsData = crsData; this.natLang = natLang;
      foreach (DictEntryType type in crsData.options.lingeaOnly ? XExtension.Create(DictEntryType.lingeaOld) : crsData.options.dictTypes) {
        switch (type) {
          case DictEntryType.lingeaOld:
            string fn = Machines.rootPath + string.Format(@"RwDicts\Sources\LingeaOld\{1}_{0}.xml", crsData.crsLang, natLang);
            if (!File.Exists(fn)) continue;
            var dict = XmlUtils.FileToObject<DictObj>(fn);
            //provazani hesla se zvukem
            foreach (var en in dict.entries) {
              foreach (var snd in en.entry.Descendants("sound")) {
                if (snd.Value.StartsWith("@")) {
                  string url;
                  if (!crsData.sounds.TryGetValue(snd.Value.Substring(1), out url)) continue;
                  snd.Value = url;
                }
              }
            }
            addDictEntries(dict.entries);
            break;
          default:
            throw new NotImplementedException();
        }
      }
    }
    public DictEntryObj find(IEnumerable<string> keys) {
      DictEntryObj de;
      foreach (var key in keys) foreach (var src in sources) if ((de = src[key].FirstOrDefault()) != null) return de;
      foreach (var key in keys) foreach (var src in sourcesLower) if ((de = src[key].FirstOrDefault()) != null) return de;
      return null;
    }
    public dictCrsData crsData;
    public Langs natLang;
    public void addDictEntries(IEnumerable<DictEntryObj> entries) {
      sources.Add(entries.SelectMany(entry => entry.headWords.Select(stem => new { stem, entry })).ToLookup(se => se.stem, se => se.entry));
      sourcesLower.Add(entries.SelectMany(entry => entry.headWords.Where(w => w != w.ToLower()).Select(stem => new { stem = stem.ToLower(), entry })).ToLookup(se => se.stem, se => se.entry));
    }
    List<ILookup<string, DictEntryObj>> sources = new List<ILookup<string, DictEntryObj>>(); //pro vsechna hesla ve slovniku: tabulka case senzitive headword => hesla
    List<ILookup<string, DictEntryObj>> sourcesLower = new List<ILookup<string, DictEntryObj>>(); //tabulka case unsenzitive headword => hesla

    public XElement findEntry(string word, Action<XElement> modifySoundTag = null) {
      foreach (var w in crsData.getWordsForms(XExtension.Create(word))) {
        DictEntryObj dictEntry = find(w.Value);
        if (dictEntry == null) continue;
        string key = DictFoundRes.getKey(dictEntry.type, dictEntry.entryId);
        var snd = dictEntry.entry.Descendants("sound").FirstOrDefault();
        if (modifySoundTag != null && snd != null) modifySoundTag(snd);
        else if (snd != null && snd.Value.StartsWith("@")) snd.Remove();
        return dictEntry.entry;
      }
      return null;
    }

    public void dictForText(Dictionary<string, List<string>> wordForms, CourseMeta.CacheDict res) {
      var entries = new Dictionary<string, DictFoundRes>();

      foreach (var wordForm in wordForms) { //kv.key - hledane slovo, kv.Value - hledane slovo + lowercase hledane slovo + seznam zakladnich tvaru slova
        DictEntryObj dictEntry = find(wordForm.Value); //vrat ze vsech kandidatu jedno nalezne entry (dle zvolene strategie)
        if (dictEntry == null) { res.notFound.Add(wordForm.Key); continue; } //nenalezeno => dej mezi nenalezene
        string key = DictFoundRes.getKey(dictEntry.type, dictEntry.entryId); //nalezeno - klic
        DictFoundRes entry;
        if (!entries.TryGetValue(key, out entry)) {
          entries.Add(key, entry = new DictFoundRes { entry = dictEntry }); // najdi dle klice
          lock (dictEntry) {
            var snd = dictEntry.entry.Descendants("sound").FirstOrDefault();
            var sndVal = snd == null ? null : snd.Value;
            if (sndVal != null) if (sndVal.StartsWith("@")) snd.Remove(); else res.externals.Add(sndVal);
          }
        }
        entry.words.Add(wordForm.Key); //pridej ohyb tvar
      }
      res.dict = createDict(entries.Values); //k nalezenym heslum vytvori slovnik
      res.dict.crsLang = crsData.crsLang; res.dict.natLang = natLang;
    }


  }

  /************** encodeEntry do JS able tvaru ***************/
  static string tagToStr(XElement tag) {
    return tag.Name.LocalName + tag.Attributes().Select(a => " " + a.Name.LocalName + "='" + a.Value + "'").DefaultIfEmpty().Aggregate((r, i) => r + i);
  }

  static DictItem encodeHtml(DictFoundRes root, XNode nd, Dictionary<string, short> tagStrToInt) {
    Func<short, string, DictItem> createRes = (tag, text) => {
      var res = root == null ? new DictItem() : new DictItemRoot { type = root.type }; //, soundFiles = new string[] { root.entry.soundMaster } };
      res.tag = tag; res.text = text;
      return res;
    };
    if (nd.NodeType == System.Xml.XmlNodeType.Text) return createRes(0, ((XText)nd).Value);
    else {
      var el = nd as XElement;
      var res = createRes(tagStrToInt[tagToStr(el)], null);
      var nodes = el.Nodes(); var first = nodes.FirstOrDefault();
      if (first != null && first.NodeType == System.Xml.XmlNodeType.Text) { nodes = nodes.Skip(1); res.text = ((XText)first).Value; }
      if (nodes.Count() > 0) res.items = nodes.Select(n => encodeHtml(null, n, tagStrToInt)).ToArray();
      return res;
    }
  }

  static schools.Dict createDict(IEnumerable<DictFoundRes> entries) {
    if (entries.Count() == 0) return new schools.Dict();
    short tagId = 1; var tagStrToInt = entries.SelectMany(ke => ke.entry.entry.DescendantsAndSelf().Select(d => tagToStr(d))).Distinct().ToDictionary(str => str, str => tagId++);
    int entryCnt = 0; foreach (var en in entries) en.entryCount = entryCnt++;
    var dict = new schools.Dict();
    dict.Tags = tagStrToInt.ToDictionary(kv => kv.Value.ToString(), kv => kv.Key);
    //zakodovane entries (misto tagu jsou jen typy)
    dict.Entries = entries.ToDictionary(k => k.entryCount.ToString(), k => (DictItemRoot)encodeHtml(k, k.entry.entry/*.Nodes().First()*/, tagStrToInt));
    //slovnik word -> entry email
    dict.Keys = entries.SelectMany(e => e.words.Select(word => new { e.entryCount, word })).GroupBy(cw => cw.word).ToDictionary(cw => cw.Key, cw => cw.First().entryCount);
    return dict;
  }

  //************ slovniky ke kurzum
  static void DictForCourses(Langs crsLang, IEnumerable<Langs> natLangs, DictCrsWords langCrs, dictOptions options) {
    //dictCrsData crsData = options.getCrsData(crsLang);
    //if (options.forAllCourses) {
    //  //jeden slovnik pro cely kurz
    //  var wordForms = crsData.getWordsForms(langCrs.exs.SelectMany(e => e.normalized.Split(' ')).Distinct().OrderBy(w => w)); //.SelectMany(s => jsCompatibleWordWrap(s)).Distinct()); //.Where(w => w == "abbreviations"));
    //  Parallel.ForEach(natLangs.Where(n => n != crsLang),
    //    //new ParallelOptions { MaxDegreeOfParallelism = 1 },
    //    natLang => {
    //      dictNatData natData = crsData.getNatData(natLang);
    //      var dict = natData.dictForText(wordForms, null);
    //      if (dict == null) return;
    //      EADeployLib.writeFile(
    //        @"d:\temp\",
    //        crsLang.ToString() + ".json",
    //        natLang,
    //        JsonConvert.SerializeObject(dict, Newtonsoft.Json.Formatting.Indented, EADeployLib.jsonSet),
    //        true);
    //    });
    //} else {
    //  //pro kazdy modul jeden slovnik
    //  var mods = langCrs.exs.GroupBy(e => e.modId).Select(g => new { jsonId = g.Key, exs = g.ToArray(), isGramm = g.Key.StartsWith(grammSpace) }).ToArray();
    //  Parallel.ForEach(natLangs.Where(n => n != crsLang),
    //    //new ParallelOptions { MaxDegreeOfParallelism = 1 },
    //    natLang => {
    //      dictNatData natData = crsData.getNatData(natLang);
    //      List<string> notFound = new List<string>();
    //      foreach (var mod in mods) {
    //        //if (mod.jsonId != "russian2_xlesson5_schapterc_shome_dhtm") continue;
    //        var words = crsData.getWordsForms(mod.exs.SelectMany(testEx => testEx.normalized.Split(' ')).Distinct().OrderBy(w => w)); //.SelectMany(s => jsCompatibleWordWrap(s)).Distinct());
    //        //HashSet<string> wordsSet = new HashSet<string>(words);
    //        var dict = natData.dictForText(words, null);
    //        if (dict == null) continue;
    //        var dirName = mod.isGramm ? "EAGrammar" : "EAData";
    //        var modId = mod.isGramm ? mod.jsonId.Replace(grammSpace, null) : mod.jsonId;
    //        var modDir = Machines.basicPath + @"rew\Web4\Schools\" + dirName + @"\";
    //        EADeployLib.writeFile(
    //          modDir,
    //          modId + "_" + options.FriendlyId + ".json",
    //          natLang,
    //          JsonConvert.SerializeObject(dict, Newtonsoft.Json.Formatting.Indented, EADeployLib.jsonSet),
    //          true);
    //        //info propName zvukovych souborech
    //        File.WriteAllLines(
    //          modDir + natLang.ToString().Replace('_', '-') + @"\" + modId + "_" + options.FriendlyId + ".txt",
    //          dict.sounds);
    //        notFound.AddRange(dict.notFound);
    //        //dict.Entries.SelectMany(e => e.Value.soundFiles.Where(s => !string.IsNullOrEmpty(s)).Select(s => @"RwDicts\LingeaSound\" + s + ".mp3")).Distinct());
    //      }
    //      File.WriteAllLines(string.Format(@"d:\LMCom\rew\Web4\RwDicts\NotFounds\{0}_{1}_{2}.txt", natLang, crsLang, options.FriendlyId), notFound.OrderBy(w => w));
    //    });
    //}
  }
  const string grammSpace = "grammar/";


  public static void cacheModuleDicts(CourseMeta.data root, Langs crsLang, Langs[] natLangs, List<string> log) {

  }

  public static void DictForCourses() {
    //dictOptions options = new dictOptions() { forAllCourses = true, lingeaOnly = false, dictTypes = new DictEntryType[] { DictEntryType.lingeaOld} };
    dictOptions options = new dictOptions() { FriendlyId = CourseMeta.dictTypes.L, forAllCourses = false, lingeaOnly = true };
    Parallel.ForEach(DictLib.crsLangs, //.Where(l => l == Langs.de_de),
      //new ParallelOptions { MaxDegreeOfParallelism = 1 },
      lng => DictForCourses(lng,
        CommonLib.bigLocalizations, //.Where(l => l == Langs.bg_bg),
        XmlUtils.FileToObject<DictCrsWords>(string.Format(Machines.rootPath + @"RwDicts\UsedWords\CourseWords_{0}.xml", lng)),
        options)
    );
  }

}

//public static schools.Dict dictForText(Langs crsLang, Langs natLang, Dictionary<string, List<string>> wordForms, dictNatData dictData) {
//  var entries = new Dictionary<string, DictFoundRes>();

//  foreach (var wordForm in wordForms) { //kv.key - hledane slovo, kv.Value - hledane slovo + lowercase hledane slovo + seznam zakladnich tvaru slova
//    DictEntryObj dictEntry = dictData.find(wordForm.Value); //vrat ze vsech kandidatu jedno nalezne entry (dle zvolene strategie)
//    if (dictEntry == null) { dictData.notFound.Add(wordForm.Key); continue; } //nenalezeno => dej mezi nenalezene
//    string key = DictFoundRes.getKey(dictEntry.type, dictEntry.entryId); //nalezeno - klic
//    DictFoundRes entry;
//    if (!entries.TryGetValue(key, out entry)) {
//      entries.Add(key, entry = new DictFoundRes { entry = dictEntry }); // najdi dle klice
//      var snd = dictEntry.entry.Descendants("sound").FirstOrDefault();
//      var sndVal = snd == null ? null : snd.Value;
//      if (sndVal != null) if (sndVal.StartsWith("@")) snd.Remove(); else dictData.sounds.Add(sndVal);
//    }
//    entry.words.Add(wordForm.Key); //pridej ohyb tvar
//  }
//  var dict = createDict(entries.Values); //k nalezenym heslum vytvori slovnik
//  if (dict != null) { dict.crsLang = crsLang; dict.natLang = natLang; }
//  return dict;
//}


//static IEnumerable<string> jsCompatibleWordWrap(string sent) {
//  if (string.IsNullOrEmpty(sent)) yield break;
//  sent = sent.ToLower(); int wordStart = -1;
//  //if (sent.IndexOf("бóльно")>=0)
//  for (int i = 0; i <= sent.Length; i++) {
//    var ch = i == sent.Length ? ' ' : sent[i];
//    string russ;
//    if (DictLib.wrongCyrilic.TryGetValue(ch, out russ)) ch = russ[0];
//    var isLetter = char.IsLetter(ch) || ch == '\x301';
//    if (wordStart == -1 && isLetter) wordStart = i;
//    else if (wordStart != -1 && !isLetter) { yield return sent.Substring(wordStart, i - wordStart).Replace("\x301", null); wordStart = -1; }
//  }
//}
//  ////obsolete
//  //static void LingeaOldToModules(Langs nativeLang, IEnumerable<Langs> courseLangs, Func<Langs, IEnumerable<modExs>> getMods, Func<XElement, transformEntryPar, XElement> hideLingea) {
//  //  foreach (var courseLang in courseLangs) {
//  //    if (!allDicts.Any(d => d.nativeLang == nativeLang && d.crsLang == courseLang)) continue;

//  //    var transformPar = new transformEntryPar() { courseLang = courseLang, nativeLang = nativeLang };


//  //    //if (lng != Langs.ru_ru || nativeLang!=Langs.cs_cz) return; //DEBUG

//  //    /* word: slovo v textu, napr. nicer
//  //     * entry: XML s lingea heslem ve slovniku
//  //     * email, entryId: napr. nice#nicest#nice one!#nicer
//  //     */


//  //    var allIdToEntry = IdToEntry[nativeLang][courseLang];
//  //    var allWordToId = WordToId[nativeLang][courseLang];

//  //    //Directory 
//  //    var exerciseIdToUsedWords = courseWords[courseLang].
//  GroupBy(w => w.moduleId + "/" + w.exId).
//  ToDictionary(
//    g => g.Key,
//    g => g.Select(w => w.word).Distinct()
//  );
//  //    IEnumerable<string> exWords = null;
//  //    LingeaSndFiles files = LingeaSndFiles.addSoundsStart(courseLang);
//  //    object buff = null;
//  //    foreach (var mod in getMods(courseLang)) { //pro vsechny pozadovane moduly
//  //vsechna slova vyskytujici se v modulu:
//  var usedWords = mod.exs. //vsechna cviceni modulu
//    //nova verze EA ma jako testEx.compId rovno celou cestu
//    Where(testEx => exerciseIdToUsedWords.TryGetValue(mod.jsonId + "/" + testEx.Substring(testEx.LastIndexOf("/") + 1), out exWords) || exerciseIdToUsedWords.TryGetValue(mod.jsonId + "/" + testEx, out exWords)).
//    SelectMany(exId => exWords).
//    Distinct().
//    ToArray();

//  //ID hesel modulu
//  string eId1 = null;
//  var entryIds = usedWords.Where(w => allWordToId.TryGetValue(w, out eId1)).Select(w => eId1).Distinct().ToArray();
//  //slovnik neprazdnych hesel modulu
//  XElement entry = null; int entrCnt = 0;
//  var usedEntries = entryIds.
//    Where(entryId => allIdToEntry.TryGetValue(entryId, out entry) && (entry = hideLingea == null ? entry : hideLingea(entry, transformPar)).HasElements).
//    Select(entryId => new { entryId = entryId, entry = entry, entryCount = entrCnt++ }).
//    ToArray();
//  //kratka identifikace hesel modulu
//  var idToNumId = usedEntries.ToDictionary(e => e.entryId, e => e.entryCount);
//  //slovnik word => count
//  string eId2 = null; int numId = -1;
//  var wordToNumId = usedWords.Where(w => allWordToId.TryGetValue(w, out eId2) && idToNumId.TryGetValue(eId2, out numId)).ToDictionary(w => w, w => numId.ToString());

//  /************** normalizeSoundElements ***************/
//  List<string> sndFileNames = new List<string>();
//  List<string> wrongSndFileNames = new List<string>();
//  //sound element nahradi <sound type='sound|lex_ful_wsnd'>url</sound>. Take naplni sndFileNames ev. wrongSndFileNames.
//  Action<XElement> normalizeSoundElements = null;
//  normalizeSoundElements = root => {
//    foreach (var el in root.Elements().ToArray()) {
//      switch (el.AttributeValue("class")) {
//        case "lex_ful_wsnd": el.ReplaceWith(new XElement("sound", new XAttribute("type", "lex_ful_wsnd"), el.Value)); break;
//        case "sound":
//          var url = el.AttributeValue("url"); if (string.IsNullOrEmpty(url)) { root.Remove(); continue; }
//          var fileName = url.Split('/').Last().Replace(".mp3", null);
//          var sndFile = string.IsNullOrEmpty(fileName) ? null : files.findViaFileName(fileName, ref buff);
//          string offurl = null;
//          if (sndFile == null) {
//            wrongSndFileNames.Add(fileName);
//          } else {
//            offurl = courseLang.ToString() + "/" + sndFile.fileName;
//            offurl = string.Format("RwDicts/LingeaSound/{0}.mp3", offurl).ToLower();
//            sndFileNames.Add(offurl);
//          }
//          el.ReplaceWith(new XElement("sound", offurl)); break;
//      }
//      normalizeSoundElements(el);
//    }
//  };
//  foreach (var el in usedEntries) normalizeSoundElements(el.entry);

//  /************** encodeLingeaEntry ***************/
//  Func<XElement, string> tagToStr = tag => { return tag.Name.LocalName + tag.Attributes().Select(a => " " + a.Name.LocalName + "='" + a.Value + "'").DefaultIfEmpty().Aggregate((r, i) => r + i); };

//  //slovnik tagu, pouzitich v entry XML. Slouzi k logicke kompresi entry, misto tagu je jen jeho email
//  short tagId = 1;
//  var tagStrToInt = usedEntries.SelectMany(ke => ke.entry.Descendants().Select(d => tagToStr(d))).Distinct().ToDictionary(str => str, str => tagId++);

//  Func<XNode, DictItem> encodeLingeaEntry = null;
//  encodeLingeaEntry = nd => {
//    if (nd.NodeType == System.Xml.XmlNodeType.Text) return new DictItem() { text = ((XText)nd).Value };
//    else {
//      var el = nd as XElement;
//      var res2 = new DictItem() { tag = tagStrToInt[tagToStr(el)] };
//      var nodes = el.Nodes(); var first = nodes.FirstOrDefault();
//      if (first != null && first.NodeType == System.Xml.XmlNodeType.Text) { nodes = nodes.Skip(1); res2.text = ((XText)first).Value; }
//      if (nodes.Count() > 0) res2.items = nodes.Select(n => encodeLingeaEntry(n)).ToArray();
//      return res2;
//    }
//  };

//  /************** DictObj ***************/
//  var dict = new DictObj();
//  dict.Tags = tagStrToInt.ToDictionary(kv => kv.Value.ToString(), kv => kv.Key);
//  //zakodovane entries (misto tagu jsou jen typy)
//  dict.Entries = usedEntries.ToDictionary(k => k.entryCount.ToString(), k => encodeLingeaEntry(k.entry.Nodes().First()));
//  //slovnik word -> entry email
//  dict.Keys = wordToNumId;
//  EADeployLib.writeFile(Machines.basicPath + @"rew\Web4\Schools\EAData\", "lingDict_" + mod.jsonId + ".json", nativeLang,
//    JsonConvert.SerializeObject(dict, Newtonsoft.Json.Formatting.Indented, EADeployLib.jsonSet), true);
//  //info propName zvukovych souborech
//  File.WriteAllLines(Machines.basicPath + @"rew\Web4\Schools\EAData\" + nativeLang.ToString().Replace('_', '-') + @"\lingDictSound_" + mod.jsonId + ".txt", sndFileNames.Distinct());
//  if (wrongSndFileNames.Count > 0) File.WriteAllLines(Machines.basicPath + @"rew\Web4\Schools\EAData\" + nativeLang.ToString().Replace('_', '-') + @"\lingDictSound_" + mod.jsonId + ".wrong", wrongSndFileNames.Distinct());
//  //    }
//  //    var ttsDir = transformPar.RowType as Tts.Dir;
//  //    if (ttsDir != null) ttsDir.Save();
//  //  }
//  //}
//  //pro Grafia kurz modifikuje slovnikova data tak, aby  z nich nebyla poznat Lingea. Slovnik ozvuci pomoci TST.
//  static XElement hideLingea(XElement entryXml, transformEntryPar par) {
//    var words = entryXml.Descendants().
//      Where(d => d.AttributeValue("class") == "trans").
//      SelectMany(el => el.Nodes().OfType<XText>()).
//      SelectMany(t => t.Value.Split(',').Select(w => w.Trim())).
//      ToArray();
//    var vals = entryXml.AttributeValue("vals");
//    if (words.Length == 0) return new XElement("w", new XAttribute("vals", vals));
//    XElement res2 = XElement.Parse(simpleEntry);
//    string soundText = null;
//    foreach (var a in res2.DescendantNodes().OfType<XText>()) {
//      switch (a.Value) {
//        case "entr": a.Value = soundText = entryXml.Descendants().First(e => e.AttributeValue("class") == "entr").Value.Replace("*", null); break;
//        case "trans": a.Value = words[rnd.Next(words.Length)]; break;//words.Aggregate((r,i) => r + ", " + i); break;
//      }
//    }
//    foreach (var a in res2.DescendantsAndSelf().SelectMany(e => e.Attributes())) {
//      switch (a.Value) {
//        case "vals": a.Value = vals; break;
//        case "url":
//          if (par.RowType == null) par.RowType = Tts.Dir.Load(par.courseLang);
//          Tts.Dir tstDir = (Tts.Dir)par.RowType;
//          soundText = soundText.Trim().ToLower();
//          Tts.DirItem item = tstDir.adjustItem(soundText);
//          a.Value = tstDir.url(item);
//          break;
//      }
//    }
//    //Sound
//    return res2;
//  }
//  static Random rnd = new Random(1);
//  const string simpleEntry = @"
//  <w vals=""vals"">
//    <div class=""entry"">
//      <div class=""head"">
//        <span class=""entr"">entr</span>
//        <span class=""sound"" url=""url""></span>
//      </div>
//      <div class=""sense"">
//        <span class=""trans"">trans</span>
//      </div>
//    </div>
//  </w>
//";