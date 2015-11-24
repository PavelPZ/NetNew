using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LMNetLib;
using System.Xml.Serialization;
using System.ComponentModel;

namespace LMComLib {
  public class DictInfo {
    static DictInfo() {
      Instance = XmlUtils.FileToObject<DictInfo>(Machines.rootPath + @"RwDicts\DictInfos.xml");
      //var debug = Instance.Dicts.GroupBy(d => d.Code).Where(g => g.Count() > 1).ToArray();
      Instance.Dicts = Instance.Dicts.SelectMany(d => new Dict[] { d, new Dict() {
        Code = d.Code,
        From = d.To,
        To = d.From,
        exampleFrom = d.exampleTo,
        exampleTo = d.exampleFrom,
        meaningFrom = d.meaningTo,
        meaningTo = d.meaningFrom,
        transFrom = d.transTo,
        transTo = d.wordsFrom,
        wordsFrom = d.wordsTo,
        wordsTo = d.wordsFrom
      }}).ToArray();
      InstanceNew = XmlUtils.FileToObject<DictInfo>(Machines.rootPath + @"RwDicts\dictInfoNew.xml");
      
    }
    public static DictInfo Instance;
    public static DictInfo InstanceNew;
    public static IEnumerable<Dict> AllDicts(Langs fromLang) {
      return Instance.Dicts.Where(d => d.From == fromLang);
    }
    public static bool DictExists(Langs fromLang, Langs toLang) {
      return Instance.Dicts.FirstOrDefault(d => d.From == fromLang && d.To == toLang)!=null;
    }
    public static Dict GetDict(Langs fromLang, Langs toLang) {
      return Instance.Dicts.First(d => d.From == fromLang && d.To==toLang);
    }
    public static LangInfo GetLangInfo(Langs lng) { return Instance.LangInfos.First(l => l.Lang == lng); }
    public struct LangInfo {
      public Langs Lang;
      public string Example;
      public bool HasMorph;
      public bool HasSound;
      public bool EnableDemo;
    }
    public class Dict {
      public Langs From;
      public Langs To;
      public string Code;
      public bool Native; //lingea nabizi specializovany slovnik na dict.com. Jinak se vyuziva opacny slovnik
      [DefaultValue(0)]
      public int wordsFrom;
      [DefaultValue(0)]
      public int wordsTo;
      [DefaultValue(0)]
      public int meaningFrom;
      [DefaultValue(0)]
      public int meaningTo;
      [DefaultValue(0)]
      public int exampleFrom;
      [DefaultValue(0)]
      public int exampleTo;
      [DefaultValue(0)]
      public int transFrom;
      [DefaultValue(0)]
      public int transTo;
    }
    public LangInfo[] LangInfos;
    public Dict[] Dicts;
    public static void test() {
      XmlUtils.ObjectToFile(@"c:\temp\dict.xml", new DictInfo() {
        LangInfos = new LangInfo[] {
          new LangInfo() {Lang = Langs.en_gb, Example = "xxxx xxxx", HasMorph = true, HasSound = true}
        },
        Dicts = new Dict[] {
          new Dict() {From = Langs.en_gb, To = Langs.en_gb, Code = "enge", exampleFrom = 1, exampleTo = 1, meaningFrom = 1, meaningTo = 1, transFrom = 1, transTo = 1, wordsFrom = 1, wordsTo = 1}
        }
      });
    }
  }
}
