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
using System.Net;
using System.Security.Principal;

namespace Wikdionary {
  public static class Lib {

    public static DictObj readDict(DictLib.dictId id) {
      return DictLib.readDict(@"d:\LMCom\rew\Web4\RwDicts\Sources\Wiktionary\", id);
    }

    //K napr. sr_latn_cs_en_gb vrati Langs
    public static void ExtractLangs(string code, out Langs crsLang, out Langs natLang) {
      natLang = LowUtils.EnumGetValues<Langs>().First(l => code.StartsWith(l.ToString()));
      code = code.Substring(natLang.ToString().Length + 1);
      crsLang = LowUtils.EnumParse<Langs>(code);
    }

    static void parseEntry(XElement en, StreamWriter wr) {
      foreach (var nd in en.Nodes()) {
        if (nd is XElement) wr.Write(string.Format("<{0}>", LowUtils.crlfSpaces(((XElement)nd).Value)));
        else if (nd is XText) wr.Write(LowUtils.crlfSpaces(((XText)nd).Value));
        else throw new Exception();
      }
    }

    //ze stazenych XML vyextrahuje text pro RJ zpracovani
    public static void ExtractTexts() {
      foreach (var fn in Directory.EnumerateFiles(@"d:\LMCom\rew\Web4\RwDicts\Sources\Wiktionary.back", "dict*.xml")) {
        var f = Path.GetFileNameWithoutExtension(fn);
        var parts = f.Split('_').Skip(1).ToArray();
        Langs natLang; Langs crsLang; ExtractLangs(f.Substring(5), out crsLang, out natLang);
        using (StreamWriter wr = new StreamWriter(string.Format(@"d:\LMCom\rew\Web4\RwDicts\Sources\Wiktionary.back\dict_{1}_{0}.txt", crsLang, natLang))) {
          foreach (var en in XElement.Load(fn).Descendants("tr")) {
            parseEntry(en.Elements().First(), wr);
            wr.Write('=');
            parseEntry(en.Elements().Last(), wr);
            wr.WriteLine();
          }
        }
      }
    }

    static Dictionary<string, Langs> codes = new Dictionary<string, Langs>() {
      {"en", Langs.en_gb},
      {"arb", Langs.ar_sa},
      {"ast", Langs.no},
      {"bg", Langs.bg_bg},
      {"ca", Langs.ca_es},
      {"cs", Langs.cs_cz},
      {"da", Langs.da_dk},
      {"nl", Langs.nl_nl},
      {"fi", Langs.fi_fi},
      {"fr", Langs.fr_fr},
      {"de", Langs.de_de},
      {"el", Langs.el_gr},
      {"he", Langs.he_il},
      {"hi", Langs.hi_in},
      {"id", Langs.id_id},
      {"hu", Langs.hu_hu},
      {"it", Langs.it_it},
      {"ja", Langs.ja_jp},
      {"ko", Langs.ko_kr},
      {"ku", Langs.ku_arab},
      {"no", Langs.nb_no},
      {"ms", Langs.ms_my},
      {"cmn", Langs.zh_cn},
      {"fa", Langs.fa_ir},
      {"pl", Langs.pl_pl},
      {"pt", Langs.pt_pt},
      {"ro", Langs.ro_ro},
      {"ru", Langs.ru_ru},
      {"sh", Langs.sr_latn_cs}, //???
      {"es", Langs.sp_sp},
      {"sv", Langs.sv_se},
      {"tr", Langs.tr_tr},
      {"vi", Langs.vi_vn},
    };
    static Langs getCodes(string cd) { try { return codes[cd]; } catch (Exception exp) { throw new Exception(cd, exp); } }

    public static Dictionary<string, string[]> MakeContent() {
      XElement root = XElement.Load(@"D:\LMCom\rew\Web4\RwDicts\Sources\Wiktionary.back\design\Index.xml");
      return root.
        Descendants("p").
        Where(d => d.AttributeValue("class") == "lang_index").
        Select(lang => lang.Elements("a").Select(a => a.AttributeValue("href"))).
        Select(hrefs => new {
          id = hrefs.Select(hr => hr.Split(new char[] { '/', '-' })).Select(parts => getCodes(parts[parts.Length - 2]).ToString() + "_" + getCodes(parts[parts.Length - 3]).ToString()).First(),
          hrefs = hrefs
        }).
        ToDictionary(ih => ih.id, ih => ih.hrefs.ToArray());
    }

    public static void Download(Dictionary<string, string[]> hrefs) {
      XElement error = new XElement("Errors");
      Parallel.ForEach(hrefs,
        new ParallelOptions { MaxDegreeOfParallelism = 1 },
        kv => {
          var wc = new WebClient();
          XElement langRoot = new XElement("root");
          foreach (var url in kv.Value) {
            var data = wc.DownloadData("http://en.wiktionary.org" + url);
            var str = Encoding.UTF8.GetString(data, 0, data.Length);
            File.WriteAllText(
              string.Format(@"d:\LMCom\rew\Web4\RwDicts\Sources\Wiktionary.back\design\{0}.htm", kv.Key),
              str);
            Packager.RewApp.run(new string[] {
              string.Format(@"d:\LMCom\rew\Web4\RwDicts\Sources\Ultralingua.back\design\tidy.exe -config d:\LMCom\rew\Web4\RwDicts\Sources\Ultralingua.back\design\tidy.cfg d:\LMCom\rew\Web4\RwDicts\Sources\Wiktionary.back\design\{0}.htm > d:\LMCom\rew\Web4\RwDicts\Sources\Wiktionary.back\design\{0}.xml",
              kv.Key)
            });
            //return;
            XElement root = XElement.Load(string.Format(@"d:\LMCom\rew\Web4\RwDicts\Sources\Wiktionary.back\design\{0}.xml", kv.Key));
            foreach (var el in root.DescendantsAndSelf()) el.Name = el.Name.LocalName;
            try {
              var tb = root.Descendants("table").Where(t => t.Parent.AttributeValue("id") == "mw-content-text").FirstOrDefault();
              if (tb == null) throw new Exception();
              langRoot.Add(tb);
            } catch {
              lock (error) error.Add(new XElement("error", new XAttribute("url", "http://en.wiktionary.org" + url), root));
            }
          }
          langRoot.Save(string.Format(@"d:\LMCom\rew\Web4\RwDicts\Sources\Wiktionary.back\{0}.xml", kv.Key));
        });
      error.Save(@"d:\temp\errors.xml");
    }
  }
}
