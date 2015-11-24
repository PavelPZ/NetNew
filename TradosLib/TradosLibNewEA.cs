using LMNetLib;
using LMComLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace LMComLib {

  public class NewEATradosCtx {
    public List<NewEATradosLib.pageInfo> pages = new List<NewEATradosLib.pageInfo>();
  }

  /// <summary>
  public static class NewEATradosLib {

    //z D:\NetNew\ObjectModel\Course\Meta.cs
    public static IEnumerable<TradosLib.tradosPage> tradosOper1Pages(IEnumerable<CourseMeta.data> nodes, LoggerMemory log, bool isFakeRussian) {
      return nodes.SelectMany(n => n.scan()).GroupBy(dt => dt.getTradosPage()).
        Where(g => g.Key != null).
        Select(g => new TradosLib.tradosPage {
          srcLang = isFakeRussian ? Langs.cs_cz : Langs.en_gb,
          FileName = g.Key,
          sentences = g.OfType<CourseMeta.ex>().SelectMany(e => e.toTransSentences(log)).Concat(
            g.SelectMany(dt => CourseMeta.locLib.getLocId2EnglishLoc(dt.title, dt.url, null).Select(nv => new NameValueString { Name = dt.url + "/" + nv.Name, Value = nv.Value }))
        ).
        Where(kv => isFakeRussian ? !kv.Name.EndsWith("ahtmltitle") : true).
        ToArray()
        });
    }
    public static void tradosOper1(IEnumerable<CourseMeta.data> nodes, LoggerMemory log) {
      var pages = tradosOper1Pages(nodes, log, false).ToArray();
      TradosLib.oper1NewTradosPages(pages, false);
    }
    public static void tradosOper1(CourseMeta.data node, LoggerMemory log) {
      TradosLib.oper1NewTradosPages(tradosOper1Pages(XExtension.Create(node), log, false).ToArray(), false);
    }


    public static Langs[] AllLocs = CommonLib.bigLocalizations;
    //public static Langs[] AllLocs = new Langs[] { Langs.cs_cz }; //CommonLib.bigLocalizations

    public class pageInfo {
      public string id; //identifikace stranky, napr. hueex0_l01_a01
      public CourseIds courseId;
      public string EAPath; //cesta ke strance relativne k EA root
      public Dictionary<Langs, Dictionary<string, string>> loc; //vsechny lokalizace
    }

    //static List<pageInfo> pages = new List<pageInfo>();
    //static pageInfo actPage;

    //public static Func<ConfigNewEA> doHackEx;
    public static Func<bool> doHack;
    public static bool Hack() { return doHack != null && doHack(); }
    //public static ConfigNewEA HackEx() { return doHackEx == null ? null : doHackEx(); }

    //**************** Volano v q:\LMNet2\WebApps\EduAuthorNew\framework\deployment\EANew-DeployGenerator.ascx
    public static string onGetResxValue(string id, string fn) { //notifikace o potrebe vlozit do stranky lokalizovany retezec
      return "{{" + id + "}}";
    }

    //**************** Volano v q:\LMNet2\WebApps\EduAuthorNew\framework\deployment\EANew-Deploy.aspx
    public static void pageGroupStart(NewEATradosCtx ctx) {  //notifikace o zacatku zpracovani skupiny stranek
      ctx.pages.Clear(); //actPage = null;
    }
    public static pageInfo pageStart(NewEATradosCtx ctx, CourseIds crsId, string pageId, string eaPath) {  //notifikace o zacatku zpracovani pageId-stranky (obsazen v fileName)
      // english1/grammar/sec01/G01.htm => q:\LMNet2\WebApps\EduAuthorNew\english1\grammar\sec01\App_LocalResources\g01.htm
      var res = new pageInfo() { id = pageId, EAPath = eaPath, courseId = crsId };
      ctx.pages.Add(res);//, fileName = @"q:\LMNet2\WebApps\EduAuthorNew\" + pageId });
      return res;
    }

    public static string locJS(Dictionary<string, string> nameValue) {
      return JsonConvert.SerializeObject(nameValue, Newtonsoft.Json.Formatting.Indented);
    }
    //zpracovani captured stranek (instrukce, stranky gramatiky a kurzu)
    //singlePage: pro instrukce, pouze jedna stranka, neni potreba tabulka dle stranek
    public static Dictionary<Langs, string> pageGroupEnd(NewEATradosCtx ctx, bool singlePage = false) { //vysledky lokalizace na konci zpracovani skupiny stranek
      if (singlePage && ctx.pages.Count != 1) throw new Exception();
      //pages muze byt predvyplnen z jineho zdroje nez odpovidajici .RESX soubor
      foreach (var pg in ctx.pages) pg.loc = loadAllResx(pg);
      //pro kazdy jazyk stringy k lokalizaci
      Dictionary<string, Dictionary<string, string>> groupLoc = new Dictionary<string, Dictionary<string, string>>();
      Dictionary<Langs, string> res = new Dictionary<Langs, string>();
      foreach (Langs lng in AllLocs) {
        if (singlePage) res[lng] = JsonConvert.SerializeObject(ctx.pages[0].loc[lng], Newtonsoft.Json.Formatting.Indented);
        else {
          groupLoc.Clear();
          foreach (var pg in ctx.pages) groupLoc.Add(pg.id, pg.loc[lng]);
          res[lng] = JsonConvert.SerializeObject(groupLoc, Newtonsoft.Json.Formatting.Indented);
        }
      }
      ctx.pages.Clear(); //actPage = null;
      return res;
    }

    //nacte vsechna .resx
    static Dictionary<Langs, Dictionary<string, string>> loadAllResx(pageInfo pg) {
      Dictionary<Langs, Dictionary<string, string>> res = new Dictionary<Langs, Dictionary<string, string>>();
      Dictionary<string, string> lngRes;
      string prefix = @"d:\LMCom\rew\EduAuthorNew\" + pg.EAPath.Replace('/', '\\');
      prefix = Path.GetDirectoryName(prefix) + "\\App_LocalResources\\" + Path.GetFileName(prefix);
      foreach (Langs lng in new Langs[] { Langs.no }.Concat(AllLocs)) {
        var fn = prefix + (lng == Langs.no ? null : "." + (lng == Langs.sp_sp ? Langs.es_es : lng).ToString().Replace('_', '-')) + ".resx";
        try {
          if (pg.courseId == CourseIds.EnglishE || !File.Exists(fn))
            //lngRes = def == null ? null : new Dictionary<string, string>(def);
            lngRes = null;
          else {
            lngRes = new Dictionary<string, string>();
            string f = CSLocalize.transFinal(File.ReadAllText(fn));
            XElement root = XElement.Parse(f);
            foreach (var el in root.Descendants("data"))
              try {
                lngRes.Add(el.AttributeValue("name"), el.ElementValue("value"));
              } catch (Exception e) {
                throw new Exception(el.AttributeValue("name"), e);
              }
          }
          switch (lng) {
            case Langs.no: res[Langs.cs_cz] = lngRes; break;
            case Langs.cs_cz: break;
            default: res[lng] = lngRes; break;
          }
          //if (lng == Langs.no) def = lngRes;
        } catch (Exception e) {
          throw new Exception(fn, e);
        }
      }
      //Dopln chybejici resx a chybejici stringy
      Dictionary<string, string> def = res.ContainsKey(Langs.en_gb) ? res[Langs.en_gb] : null;
      foreach (var kv in res.ToArray()) {
        if (kv.Value == null) res[kv.Key] = def; //neexistuje RESX soubor
        else if (def != null) { //existuje RESX soubor ale muze obsahovat neprelozene stringy
          foreach (var tr in kv.Value.Where(kvv => kvv.Value == "###TRANS TODO###" && def.ContainsKey(kvv.Key)).ToArray()) 
            kv.Value[tr.Key] = def[tr.Key]; //neprelozeny string
        }
      }
      //loc byl predvyplnen => obohat jim res
      if (pg.loc != null) {
        if (res.First().Value == null)
          res = pg.loc; //zadny preklad, pouzij predvyplneny loc
        else
          foreach (var kv in res.Where(k => pg.loc.ContainsKey(k.Key))) //preklad => obohat o loc
            foreach (var lkv in pg.loc[kv.Key])
              kv.Value[lkv.Key] = lkv.Value;
      }
      return res;
    }

  }
}
