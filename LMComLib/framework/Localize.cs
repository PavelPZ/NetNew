using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Resources;
using System.IO;
using System.Web.Hosting;
using System.Threading;
using System.Xml.Linq;
using System.Globalization;
using System.Configuration;

using LMNetLib;

namespace LMComLib {

  /// <summary>
  /// Lokalizovane strnanky jsou sdruzeny do skupin. Skupina je ulozena v databazi v Pages.PageGroup.
  /// Skupina je nadefinovana v configu v Filters (urcena maskou do filesystemu)
  /// ENUM je v databazi
  /// </summary>
  public enum LocPageGroup {
    other = 0,
    EA_English = 1,
    EA_German = 2,
    EA_Italian = 3,
    EA_French = 4,
    EA_Spanish = 5,
    EA_Russian = 6,
    EA_Chinese = 7,
    EA_Test = 8,
    EA_Code = 9,
    LMComLib = 10,
    eCommerce = 11,
    Register = 12,
    Help = 13,
    Moodle = 14,
    Facebook = 15,
    EngBusiness = 16,
    CPV = 17,
    eTestMe = 18,

    compLMCom = 19,

    //rewise = 20,

    rew_school = 21,
    rew_rewise = 22,

    //Skupiny pro novy author, data a publishery
    newEA = 23,
    fakeRussian = 24,

  }

  public static class CSLocalize {


    /// <summary>
    /// Odstrani pomocne zavorky z přeloženého řetězce
    /// </summary>
    //static Regex rxTransFinal = new Regex(@"\(\*(\.|\,|\w|\s)*?\*\)");
    static Regex rxTransFinal = new Regex(@"\(\*.*?\*\)");
    public static string transFinal(string trans) {
      if (trans == null) return null;
      return rxTransFinal.Replace(trans, "");
    }

    /// <summary>
    /// Jeden rozlezeny .RESX soubor
    /// </summary>
    public class LangItem : Dictionary<string, string> {
      public LangItem(string fn) {
        XElement root = XElement.Load(fn);
        foreach (XElement el in root.Descendants("data"))
          try {
            Add((string)el.Attribute("name"), transFinal(el.Element("value").Value));
          } catch (Exception exp) {
            throw new Exception(string.Format("{0}:  {1}", fn, (string)el.Attribute("name")), exp);
          }
      }
      public LangItem(string lang, params string[] fns) {
        string val = null;
        foreach (string fn in fns) {
          string actFn = lang == "no" ? fn : fn.Replace(".resx", "." + lang + ".resx");
          if (!File.Exists(actFn)) continue;
          XElement root = XElement.Load(actFn);
          foreach (XElement el in root.Descendants("data"))
            try {
              val = transFinal(el.Element("value").Value);
              Add((string)el.Attribute("name"), val);
            } catch (Exception exp) {
              throw new Exception(string.Format("{0}:  {1}, Lang={2}, OldVal={3}, NewVal={4}", fn, (string)el.Attribute("name"), lang, this[(string)el.Attribute("name")], val), exp);
            }
        }
      }
    }

    /// <summary>
    /// Skupina .RESX souboru, pro kazdy jazyk jeden. Nebo (pro LMComLib) wrapper pro ASP.NET ResourceManager
    /// </summary>
    public class LangItems : Dictionary<string, LangItem> { 
      //LM mechanismus lokalizace
      public LangItems(params string[] fns) {
        foreach (Langs lang in LowUtils.EnumGetValues<Langs>()) {
          string lng = lang.ToString().Replace('_', '-').ToLower();
          LangItem res = new LangItem(lng, fns); if (res.Count <= 0) continue;
          Add(lng, res);
          //resMan = new ResourceManager("LMComLib.trados_globalresources.lmcomlibcs", typeof(LangItems).Assembly);
        }
      }
      /// <summary>
      /// ASP.NET mechanismus lokalizace
      /// </summary>
      public ResourceManager resMan;
      public LangItems(LocPageGroup grp) {
        resMan = new ResourceManager("LMComLib.trados_globalresources.lmcomlibcs", typeof(LangItems).Assembly);
      }

      /// <summary>
      /// vrati string, lokalizovany LM zpusobem (pres trados_globalresources adresar)
      /// </summary>
      public string GetString(string name, CultureInfo cult, string defaultValue) {
        //Pro dany jazyk:
        LangItem li;
        if (!TryGetValue(cult.Name.ToLower(), out li) && !TryGetValue("no", out li)) return transFinal(defaultValue); //
        //Jazyk existuje, najdi retezec:
        string res;
        if (!((LangItem)li).TryGetValue(name, out res)) return transFinal(defaultValue);
        //Retezec existuje
        return res;
      }
    }

    /// <summary>
    /// Skupina skupin .RESX souboru, pro kazdou page grupu jedena (pro LMComLib a EA_Code)
    /// </summary>
    static Dictionary<LocPageGroup, LangItems> globalRes;

    static LangItems getManager(LocPageGroup actGrp) {
      lock (typeof(CSLocalize)) {
        if (globalRes == null) {
          //Vytvoreni vsech skupin RESX souboru pro vsechny PageGrupy
          globalRes = new Dictionary<LocPageGroup, LangItems>();
          string dir = HostingEnvironment.ApplicationPhysicalPath + @"trados_globalresources\"; //root
          foreach (LocPageGroup grp in Enum.GetValues(typeof(LocPageGroup))) { //pro kazdou grupu
            LangItems man;
            if (grp != LocPageGroup.LMComLib) {
              string fn = dir + grp.ToString() + "cs.resx"; //jmeno nelokalizovaneho RESX souboru
              if (!File.Exists(fn)) continue;
              man = new LangItems(fn); //skupina RESX souboru: lokalizovane i nelokalizovane
            } else {
              man = new LangItems(grp); //LMComLib: cesta pres standardni ASP.NET mechanismus
            }
            globalRes.Add(grp, man);
          }
        }
        LangItems res;
        return globalRes.TryGetValue(actGrp, out res) ? res : null;
      }
    }

    /*static Regex rxTransHelp = new Regex(@"<transhelp>.*?</transhelp>", RegexOptions.Singleline | RegexOptions.Compiled);
    public static string generateResxString(string txt) {
      if (txt == null) return null;
      string res = null;
      foreach (regExItem item in regExItem.Parse(txt, rxTransHelp))
        if (!item.IsMatch) res += item.Value;
      return res;
    }*/
    /// <summary>
    /// Hlavni procedura pro lokalizaci .CS souboru
    /// </summary>
    public static string localize(string guid, LocPageGroup grp, string txt) {
      if (guid == null) return transFinal(txt); //jeste neproslo lokalizacnim mechanismem
      else {
        CultureInfo cult = Thread.CurrentThread.CurrentUICulture;
        LangItems man = getManager(grp); //dej spravnou skupinu RESX souboru pro zadanou PageGroup
        if (man == null) return transFinal(txt); //neni takova: vrat puvodni retezec
        if (man.resMan != null) return transFinal(man.resMan.GetString(guid, cult) ?? txt); //Lokalizace ASP.NET mechanismem, pres ResourceManager
        return transFinal(man.GetString(guid, cult, txt) ?? txt); //Lokalizace LM mechanismem pres obsah trados_globalresources/ adresare
      }
    }
    public static string localize(string guid, LocPageGroup grp, Langs lng, string txt) {
      if (guid == null || lng==Langs.no) return transFinal(txt); //jeste neproslo lokalizacnim mechanismem
      else {
        string cultStr = lng.ToString().Replace('_','-');
        if (cultStr == "sp-sp") cultStr = "es-es";
        CultureInfo cult = new CultureInfo(cultStr);
        LangItems man = getManager(grp); //dej spravnou skupinu RESX souboru pro zadanou PageGroup
        if (man == null) return transFinal(txt); //neni takova: vrat puvodni retezec
        if (man.resMan != null) return transFinal(man.resMan.GetString(guid, cult) ?? txt); //Lokalizace ASP.NET mechanismem, pres ResourceManager
        return transFinal(man.GetString(guid, cult, txt) ?? txt); //Lokalizace LM mechanismem pres obsah trados_globalresources/ adresare
      }
    }
  }

  public static class LocalizeLib {

    public const string lineDel = "~~°°^^";

    public static Dictionary<Langs, string> LocStringDecode(string src) {
      Dictionary<Langs, string> res = null;
      LocStringDecode(src, ref res);
      return res;
    }
    public static void LocStringDecode(string src, ref Dictionary<Langs, string> res) {
      if (string.IsNullOrEmpty(src)) {
        if (res != null) res.Clear();
        return;
      }
      if (res == null) res = new Dictionary<Langs, string>(); else res.Clear();
      string[] lines = src.Split(new string[] { lineDel }, StringSplitOptions.RemoveEmptyEntries);
      foreach (string line in lines) {
        if (string.IsNullOrEmpty(line)) continue;
        string[] parts = line.Split(new char[] { '=' }, 2);
        //Jazyk=Hodnota?
        Langs lng;
        try { lng = parts.Length == 2 ? (Langs)Enum.Parse(typeof(Langs), parts[0].Replace('-', '_')) : Langs.no; } catch { lng = Langs.no; }
        if (lng == Langs.no) { //ne: musi byt jeden radek
          if (lines.Length > 1) throw new Exception();
          res.Add(Langs.no, line);
          return;
        }
        res[lng] = parts[1];
      }
    }

    //
    public static string LocStringDecode(string src, Langs lang, string defaultValue) {
      if (string.IsNullOrEmpty(src)) return defaultValue;
      string lngStart = lang.ToString().Replace('_', '-') + "=";
      int startIdx = src.IndexOf(lngStart);
      if (startIdx < 0)
        return src.IndexOf('=') < 0 ? src : defaultValue; //je jazykovy format, vrat null, jinak src
      int endIdx = src.IndexOf(lineDel, startIdx + 1);
      if (endIdx < 0) endIdx = src.Length;
      startIdx += lngStart.Length;
      return src.Substring(startIdx, endIdx - startIdx);
    }

    public static string LocStringDecode(string src, Langs lang) {
      return LocStringDecode(src, lang, null);
    }

    public static string LocStringEncode(Dictionary<Langs, string> dict) {
      if (dict == null) return null;
      string res;
      if (dict.Count == 1 && dict.TryGetValue(Langs.no, out res)) return res;
      StringBuilder sb = new StringBuilder();
      foreach (KeyValuePair<Langs, string> kv in dict) {
        sb.Append(kv.Key.ToString().Replace('_', '-')); sb.Append('='); sb.Append(kv.Value); sb.Append(lineDel);
      }
      sb.Length = sb.Length - lineDel.Length;
      return sb.ToString();
    }
  }
}
