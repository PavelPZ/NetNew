using LMComLib;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LMNetLib;
using System.Xml.Serialization;
using System.Text;
using System.Xml;
using System.Globalization;
using System.Collections;
using System.Threading;

namespace Ultralingua {

  public class Entry {
    [XmlAttribute]
    public string Header;
    public XElement Body;
    [XmlAttribute]
    public bool isPhrase;
    [XmlAttribute]
    public string Id { get { return id ?? (id = getId(Header, Body)); } set { id = value; } } string id;
    public static string getId(string Header, XElement Body) { return Header + Body.ToString().GetHashCode().ToString(); }
  }
  public class Complexes {
    public Langs crsLang;
    public Langs natLang;
    public int okCount;
    public int notFoundCount;
    public int entriesCount;
    public List<Complex> entries;
    public string[] notFound;
    public Entry[] Entries;
    public IEnumerable<Entry> fromIds(IEnumerable<string> ids) { return ids.Select(id => entryById(id)); }
    public Entry entryById(string id) { return (_entryById ?? (_entryById = Entries.ToDictionary(e => e.Id, e => e)))[id]; }
    Dictionary<string, Entry> _entryById;
  }
  public class Complex {
    [XmlAttribute]
    public string Query;
    [XmlAttribute]
    public string Reason;
    public string[] Headers;
    public string[] Smarts;
    public string[] Phrases;
  }

  public class Dict {
    public static Langs[] allLangs = new Langs[] { Langs.en_gb, Langs.de_de, Langs.it_it, Langs.sp_sp, Langs.fr_fr, Langs.pt_pt };
    static Dict() {
      //dicts.Add(new Dict { From = Langs.en_gb, To = Langs.de_de });
      //return;
      foreach (Langs to in allLangs) foreach (Langs from in allLangs) {
          if (from == to) continue;
          if (from == Langs.de_de && to == Langs.pt_pt) continue;
          if (from == Langs.pt_pt && to == Langs.de_de) continue;
          dicts.Add(new Dict { From = from, To = to });
        }
    }
    public string from { get { return decode(From); } }
    public string to { get { return decode(To); } }
    public Langs From;
    public Langs To;
    string decode(Langs lng) {
      switch (lng) {
        case Langs.en_gb: return "English";
        case Langs.de_de: return "German";
        case Langs.fr_fr: return "French";
        case Langs.sp_sp: return "Spanish";
        case Langs.it_it: return "Italian";
        case Langs.pt_pt: return "Portuguese";
        default: throw new NotImplementedException();
      }
    }
    public static List<Dict> dicts = new List<Dict>();
    //design
    public Complexes content;
  }

  public static class Lib {

    public static Complexes readDict(DictLib.dictId id) {
      return XmlUtils.FileToObject<Complexes>(id.fileName(@"d:\LMCom\rew\Web4\RwDicts\Sources\Ultralingua.back\"));
    }

    static Func<string, string> doNormalize = input => {
      var parts = input.ToLower().Split(new char[] { '\'' }, StringSplitOptions.RemoveEmptyEntries);
      switch (parts.Length) {
        case 1: return parts[0];
        case 2: return parts[1] == "s" ? parts[0] : input;
        default: return input;
      }
    };

    static Dictionary<string, Langs> otherLangsStr = new Dictionary<string, Langs>() { 
      //{"tr", Langs.tr_tr},
      //{"ru", Langs.ru_ru},
      //{"el", Langs.el_gr},
      //{"no", Langs.nb_no},
      //{"nl", Langs.nl_nl},
      //{"pl", Langs.pl_pl},
      //{"zh_cn", Langs.zh_cn},
      {"ar", Langs.ar_sa}
    };

    public static void MakeWordList_Other() {
      using (var imp = new Impersonator("pavel", "LANGMaster", "zvahov88_")) {
        DictLib.RunStemming<List<string>>(
          otherLangsStr.Values,
          lng => {
            Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("ar-sa");
            var list = File.ReadAllLines(string.Format(@"d:\LMCom\rew\Web4\RwDicts\Sources\Wiktionary.back\src\{0}.txt", otherLangsStr.First(kv => kv.Value == lng).Key)).ToArray();
            return list.Select(l => l.Split(' ')[0]).Take(200000).
              Where(w => !string.IsNullOrEmpty(w)).
              Select(w => w.ToLower()).
              Distinct().ToArray();
          },
          lng => new List<string>(),
          (lng, word, row, res) => res.AddRange(row.Select(r => doNormalize(r)).Where(r => !string.IsNullOrEmpty(r))),
          (lng, res) => File.WriteAllLines(string.Format(Machines.rootPath + @"RwDicts\Sources\Ultralingua.back\design\WordsStems_{0}.txt", lng), res.Distinct().OrderBy(w => w)),
          imp
        );
      }
    }


    public static void MakeWordList() {
      using (var imp = new Impersonator("pavel", "LANGMaster", "zvahov88_"))
        DictLib.RunStemming<List<string>>(
          Dict.allLangs,
          lng => File.ReadAllLines(string.Format(Machines.rootPath + @"RwDicts\Sources\LingeaOld\design\wordList_{0}.txt", lng)).Concat(
            File.ReadAllLines(string.Format(Machines.rootPath + @"RwDicts\Sources\Wiktionary.back\{0}.txt", lng)).Select(l => l.Split(' ')[0]).Take(50000)).
            Where(w => !string.IsNullOrEmpty(w)).
            Select(w => w.ToLower()).
            Distinct().ToArray(),
          lng => new List<string>(),
          (lng, word, row, res) => res.AddRange(row.Select(r => doNormalize(r)).Where(r => !string.IsNullOrEmpty(r))),
          (lng, res) => File.WriteAllLines(string.Format(Machines.rootPath + @"RwDicts\Sources\Ultralingua.back\design\WordsStems_{0}.txt", lng), res.Distinct().OrderBy(w => w)),
          imp
        );
    }

    public struct part { public XElement xml; public bool isPhrase; }

    public static Complexes FinishDict(Dict dict) {
      Func<XElement, string> getHeader = el => {
        var hdrTag = el.DescendantsAttr("class", "headword").SelectMany(e => e.DescendantsAttr("class", "ultext")).Single();
        StringBuilder sb = new StringBuilder();
        foreach (var n in hdrTag.Nodes()) switch (n.NodeType) {
            case XmlNodeType.Text: sb.Append(" " + ((XText)n).Value); break;
            case XmlNodeType.Element: sb.Append(" " + ((XElement)n).Value); break;
            default: throw new Exception();
          }
        return doNormalize(rxDoubleSpaces.Replace(sb.ToString(), " ").Trim().ToLower());
      };
      Dictionary<string, Entry> availableEntries = new Dictionary<string, Entry>();
      Func<XElement, bool, string> toEntry = (el, isPhrase) => {
        var hdr = getHeader(el); var id = Entry.getId(hdr, el);
        Entry ent;
        if (!availableEntries.TryGetValue(id, out ent)) availableEntries.Add(id, new Entry { Body = el, isPhrase = isPhrase, Header = hdr });
        return id;
      };
      Func<XElement, XElement> getBody = el => { var res = el.Element("div"); return res != null && res.DescendantsAttr("class", "headword").Any() ? res : null; };

      var entries = Directory.EnumerateFiles(Machines.rootPath + @"RwDicts\Sources\Ultralingua.back", string.Format("{1}_{0}_*.xml", dict.From, dict.To)).
        Select(fn => XElement.Load(fn)).
        SelectMany(root => root.Element("body").Elements().Where(e => e.HasElements)).
        Where(en => !en.Elements().Any(p => p.Name.LocalName == "p")). //Guests get 10 free searches. etc....
        Select(en => new { query = en.Elements().First().Value, body = en.Elements().Skip(1).ToArray() }).
        Where(hb => hb.query != null).
        ToArray();
      if (entries.Length == 0) return null;
      var dictRes = new Complexes { entries = new List<Complex>(), crsLang = dict.From, natLang = dict.To };
      List<string> emptyEntries = new List<string>();
      foreach (var entry in entries.Where(hb => hb.body.Any()).DistinctBy(hb => hb.query)) {
        var st = 0; var headers = new List<string>(); var smarts = new List<string>(); var phrases = new List<string>(); string cls;
        foreach (var part in entry.body.Where(p => p.AttributeValue("class") != null)) {
          try {
            cls = part.AttributeValue("class");
            if (cls == "section-header") cls = part.Value;
            switch (cls) {
              case "term sortkey":
                if (st == 0) headers.Add(toEntry(part, false));
                else if (st == 1) smarts.Add(toEntry(part, false));
                else throw new Exception();
                break;
              case "term":
                if (st == 0) headers.Add(toEntry(part, true));
                else if (st == 1) smarts.Add(toEntry(part, true));
                else if (st == 2) phrases.Add(toEntry(part, true));
                else throw new Exception();
                break;
              case "Smart Matches":
                if (st == 0) st = 1;
                else throw new Exception();
                break;
              case "Phrases":
                if (st == 0 || st == 1) st = 2;
                else throw new Exception();
                break;
              case "error-notice result-notice":
                break;
              default:
                throw new Exception();
            }
          } catch {
            throw;
          }
        }
        if (headers.Count > 0 || smarts.Count > 0 || phrases.Count > 0)
          dictRes.entries.Add(new Complex {
            Query = entry.query,
            Headers = headers.ToArray(),
            Smarts = smarts.ToArray(),
            Phrases = phrases.ToArray(),
          });
        else
          emptyEntries.Add(entry.query);
      }
      dictRes.notFound = emptyEntries.Concat(entries.Where(hb => !hb.body.Any()).Select(hb => hb.query)).Distinct().OrderBy(w => w).ToArray();
      dictRes.okCount = dictRes.entries.Select(en => en.Query).Distinct().Count();
      dictRes.notFoundCount = dictRes.notFound.Length;
      dictRes.Entries = availableEntries.Values.ToArray();
      dictRes.entriesCount = dictRes.Entries.Length;
      XmlUtils.ObjectToFile(string.Format(Machines.rootPath + @"RwDicts\Sources\Ultralingua.back\dict_{1}_{0}.xml", dict.From, dict.To), dictRes);
      return dictRes;
    }

    public static void Op1_Finish() {
      Parallel.ForEach(Dict.dicts, new ParallelOptions { MaxDegreeOfParallelism = 2 }, dict => FinishDict(dict));
    }
    static Regex rxDoubleSpaces = new Regex(@"\s\s+", RegexOptions.Singleline); //zdvojene mezery a crlf a tab se nahradi mezerou

    public static void WordForms_() {
      var res = new HashSet<char>();
      foreach (var from in Dict.allLangs) {
        var wordList = File.ReadAllLines(string.Format(Machines.rootPath + @"RwDicts\Sources\Ultralingua.back\design\WordsStems_{0}.txt", from));
        foreach (var ch in wordList.SelectMany(c => c)) res.Add(ch);
        foreach (var fn in Directory.EnumerateFiles(@"d:\LMCom\rew\Web4\RwDicts\Sources\Ultralingua.back", string.Format("dict_*_{0}.xml", from), SearchOption.TopDirectoryOnly)) {
          Complexes dict = XmlUtils.FileToObject<Complexes>(fn);
          foreach (var en in dict.entries) {
            foreach (var ch in dict.fromIds(en.Headers).Where(e => !e.isPhrase).Select(e => e.Header).SelectMany(c => c)) res.Add(ch);
            foreach (var ch in dict.fromIds(en.Smarts).Where(e => !e.isPhrase).Select(e => e.Header).SelectMany(c => c)) res.Add(ch);
            foreach (var ch in en.Query) res.Add(ch);
          }
        }
      }
      File.WriteAllText(@"d:\temp\chars.txt", new string(res.OrderBy(ch => ch).ToArray()));
    }

    public static void LingeaWordForms() {
      //foreach (var crsLang in XExtension.Create(Langs.en_gb)) { // CourseDictionary.crsLangs) {
      //  var forms = CourseDictionary.nativeLangs.
      //    Select(natLang => string.Format(@"d:\LMCom\rew\Web4\RwDicts\Sources\LingeaOld\lingea_{1}_{0}.xml", crsLang, natLang)).
      //    Where(fn => File.Exists(fn)).Select(fn => XmlUtils.FileToObject<CourseDictionary2.Dict>(fn)).
      //    SelectMany(dict => dict.entries).
      //    Where(entry => entry.Stems.Any(st => (st.type == CourseDictionary2.DictStemType.courseUses || st.type == CourseDictionary2.DictStemType.wordId)&& st.word == "banking")).
      //    ToArray();
      //  XmlUtils.ObjectToFile(@"d:\temp\banking.xml", forms);
      //}
      foreach (var crsLang in DictLib.crsLangs) {
        var forms = CommonLib.bigLocalizations.
          Select(natLang => string.Format(@"d:\LMCom\rew\Web4\RwDicts\Sources\LingeaOld\lingea_{1}_{0}.xml", crsLang, natLang)).
          Where(fn => File.Exists(fn)).Select(fn => XmlUtils.FileToObject<DictObj>(fn)).
          SelectMany(dict => dict.entries).
          SelectMany(entry => entry.courseWords.
            SelectMany(st => entry.headWords.Select(hw => new { key = hw, value = st }))).
          ToLookup(kv => kv.key, kv => kv.value);
        using (StreamWriter wr = new StreamWriter(string.Format(@"d:\LMCom\rew\Web4\RwDicts\Forms\ling_forms_{0}.xml", crsLang))) foreach (var kv in forms.OrderBy(k => k.Key)) {
            wr.Write(kv.Key); wr.Write(": "); wr.WriteLine(kv.Distinct().Aggregate((r, i) => r + "|" + i));
          }
        using (StreamWriter wr = new StreamWriter(string.Format(@"d:\LMCom\rew\Web4\RwDicts\Forms\ling_crossref_{0}.xml", crsLang)))
          foreach (var kv in forms.SelectMany(kv => kv.Concat(XExtension.Create(kv.Key)).Distinct().Select(v => new { kv.Key, v })).ToLookup(kv => kv.v, kv => kv.Key).Where(l => l.Count() > 1).OrderBy(k => k.Key)) {
            wr.Write(kv.Key); wr.Write(": "); wr.WriteLine(kv.Aggregate((r, i) => r + "|" + i));
          }
      }
    }

    public static void WordForms() {

      List<Complex> wrongs = new List<Complex>();
      foreach (var from in Dict.allLangs) {
        //if (wrongs.Count > 20) break;
        Dictionary<string, HashSet<string>> forms = new Dictionary<string, HashSet<string>>();
        Action<IEnumerable<string>, string> addForm = (smarts, head) => {
          HashSet<string> frms;
          foreach (var smart in smarts) {
            if (!forms.TryGetValue(smart, out frms)) forms.Add(smart, frms = new HashSet<string>());
            if (head != null) frms.Add(head);
          }
        };
        Dictionary<string, HashSet<string>> duplSmarts = new Dictionary<string, HashSet<string>>();
        foreach (var fn in Directory.EnumerateFiles(@"d:\LMCom\rew\Web4\RwDicts\Sources\Ultralingua.back", string.Format("dict_*_{0}.xml", from), SearchOption.TopDirectoryOnly)) {
          Complexes dict = XmlUtils.FileToObject<Complexes>(fn);
          foreach (var en in dict.entries) {
            var headKeys = dict.fromIds(en.Headers).Where(e => !e.isPhrase).Select(e => e.Header).Distinct().ToArray();
            var smartKeys = dict.fromIds(en.Smarts).Where(e => !e.isPhrase).Select(e => e.Header).Distinct().ToArray();

            //headKeys musi byt "unicode normalize" stejne
            if (headKeys.Length > 1 && headKeys.Distinct(igbnoreAcute.Instance).Count() > 1) { en.Reason = "headKeys.Length > 1 && headKeys.Distinct(igbnoreAcute.Instance).Count() > 1"; wrongs.Add(en); continue; }
            //headKeys musi "unicode normalize"-obsahovat Query string
            if (headKeys.Length > 0 && !headKeys.Contains(en.Query, igbnoreAcute.Instance)) { en.Reason = "headKeys.Length > 0 && !headKeys.Contains(en.Query, igbnoreAcute.Instance)"; wrongs.Add(en); continue; }
            //Evidence duplicitnich smartKeys
            if (smartKeys.Length > 1 && smartKeys.Distinct(igbnoreAcute.Instance).Count() > 1) duplSmarts[en.Query] = new HashSet<string>(smartKeys);

            //v smart keys jsou normalizovane tvary pro ohyb v Query
            if (smartKeys.Length > 0) { addForm(smartKeys, en.Query); continue; }

            //v head keys jsou normalizovane tvary pro ohyb v Query
            if (headKeys.Length > 0 && smartKeys.Length == 0) { addForm(smartKeys, en.Query); continue; }

            if (headKeys.Length == 0 && smartKeys.Length == 0) continue;

            //nemelo by nastat:
            en.Reason = "else"; wrongs.Add(en); continue;
          }
        }

        var formsLk = LookupLib.fromDictHash(forms);
        var duplSmartsLk = LookupLib.fromDictHash(duplSmarts);
        //rucni oprava:
        var byHandFn = string.Format(@"d:\LMCom\rew\Web4\RwDicts\Forms\duplSmartsOk_{0}.xml", from);
        if (File.Exists(byHandFn)) {
          var byHand = LookupLib.fromStrings(File.ReadAllLines(byHandFn));
          formsLk = LookupLib.extract(formsLk, byHand);
          byHand = LookupLib.fromStrings(File.ReadAllLines(byHandFn));
          duplSmartsLk = LookupLib.extract(duplSmartsLk, LookupLib.invert(byHand));
        }

        //Export
        File.WriteAllLines(string.Format(@"d:\LMCom\rew\Web4\RwDicts\Forms\forms_{0}.xml", from), LookupLib.toStrings(formsLk));
        File.WriteAllLines(string.Format(@"d:\LMCom\rew\Web4\RwDicts\Forms\crossref_{0}.xml", from), LookupLib.toStrings(LookupLib.invert(formsLk)));

        File.WriteAllLines(string.Format(@"d:\LMCom\rew\Web4\RwDicts\Forms\duplSmarts_{0}.xml", from), LookupLib.toStrings(duplSmartsLk));

      }
      if (wrongs.Count > 0) XmlUtils.ObjectToFile(@"d:\temp\wrongs.xml", wrongs); else File.Delete(@"d:\temp\wrongs.xml");
    }
  }

}
