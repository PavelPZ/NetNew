using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web;
using System.Resources;

namespace LMComLib {

  public static class LocalizeUrl {

    static Dictionary<Langs, Dictionary<string, string>> delocalize = new Dictionary<Langs, Dictionary<string, string>>();
    static Dictionary<string, Dictionary<string, string>> localize = new Dictionary<string, Dictionary<string, string>>();
    static bool active;

    const string c_Descr = ".description"; 
    static LocalizeUrl() {
      if (active) return;
      lock (typeof(LocalizeUrl)) {
        if (active) return;
        active = true;
        if (!Directory.Exists(Machines.basicPath + @"rew\LMCom\app_globalresources")) return;
        //nalezeni vsech prelozenych RESX souboru:
        foreach (string fn in Directory.GetFiles(Machines.basicPath + @"rew\LMCom\app_globalresources", "*.resx")) {
          string[] parts = fn.ToLower().Split('.'); if (parts.Length < 4) continue;
          if (parts[parts.Length - 3] != "sitemap") continue; //neni lokalizace sitemap
          string lngStr = parts[parts.Length - 2]; //cast s jazykem
          if (lngStr == "es-es") lngStr = "sp-sp";
          Langs lng = (Langs)Enum.Parse(typeof(Langs), lngStr.Replace('-', '_'), true); //jazyk lokalizace
          //Adjust lang specific dictionaries:
          Dictionary<string, string> deloc, loc;
          if (!delocalize.TryGetValue(lng, out deloc)) {
            delocalize.Add(lng, deloc = new Dictionary<string, string>());
            localize.Add(lngStr, loc = new Dictionary<string, string>());
          } else if (!localize.TryGetValue(lngStr, out loc)) throw new Exception();
          //Fill dictionaries
          using (ResXResourceReader rdr = new ResXResourceReader(fn))
            foreach (System.Collections.DictionaryEntry de in rdr) {
              if ((string)de.Value=="fake_translation") continue;
              string url = (string)de.Key; if (!url.EndsWith(c_Descr)) continue;
              url = ((string)de.Key).Replace("_", "/").Substring(0, url.Length - c_Descr.Length);
              int lastIdx = url.LastIndexOf('/'); string basic = url.Substring(0, lastIdx + 1);
              string oldVal; string newVal = basic + (string)de.Value + ".aspx";
              if (loc.TryGetValue(url + ".aspx", out oldVal)) {
                if (string.Compare(oldVal, newVal, true) != 0)
                  throw new Exception("Localize: duplikovana URL adresa v sitemap: " + url + ".aspx, oldVal=" + oldVal + ", newVal=" + newVal + ", fn=" + fn);
              } else {
                loc.Add(url + ".aspx", basic + (string)de.Value + ".aspx");
                string key = null;
                try {
                  key = (basic + (string)de.Value).ToLower();
                  deloc.Add(key, url.Replace("pages/", null));
                } catch {
                  if (de.Value.ToString().IndexOf("###TRANS TODO###") < 0)
                    throw new Exception("Delocalize: stejny preklad dvou ruznych adres ze sitemap: " +
                      url.Replace("pages/", null) + " a " + deloc[key] + " se preklada jako " + basic + (string)de.Value);
                }
              }
              //loc[url + ".aspx"] = basic + (string)de.Value + ".aspx";
              //deloc[basic + (string)de.Value] = url.Replace("pages/", null);
            }
        }
        /*
        using (StreamWriter wr = new StreamWriter(@"c:\temp\localize.txt")) {
          foreach (var kv in localize["pl-pl"]) {
            wr.WriteLine("{0}={1}", kv.Key, kv.Value);
          }
        }
         * */
      }
      /*Priklady:
      
      deloc.Add("pages/help/cz-napoveda", "help/napoveda");
      deloc.Add("pages/help/moodle/cz-moodle-moduly", "help/moodle/moodle-moduly");
      deloc.Add("pages/help/courses/cz-jazykove-kurzy", "help/courses/jazykove-kurzy");
      
      loc.Add("pages/help/napoveda.aspx", "pages/help/cz-napoveda.aspx");
      loc.Add("pages/help/moodle/moodle-moduly.aspx", "pages/help/moodle/cz-moodle-moduly.aspx");
      loc.Add("pages/help/courses/jazykove-kurzy.aspx", "pages/help/courses/cz-jazykove-kurzy.aspx");*/
    }

    public static string UrlLocalize(string url, Domains site, Langs lng) {
      if (site != Domains.com && site != Domains.org) return url;
      int idx = url.IndexOf("pages/"); if (idx < 0) return url;
      string key = url.Substring(idx);
      return url.Substring(0, idx) + Localize(key, lng.ToString().Replace('_', '-'));
    }

    public static string LocalizeProductName(string fileName, string lng) {
      string res = Localize("pages/products/" + fileName + ".aspx", lng);
      return res.Replace("pages/products/", null).Replace(".aspx", null);
    }

    public static string Localize(string path, string lng) {
      Dictionary<string, string> lngTab;
      if (!localize.TryGetValue(lng.ToLower(), out lngTab)) return path;
      string res;
      path = path.ToLowerInvariant();
      bool isNoDict = path.EndsWith("?nodict=true");
      if (isNoDict) path = path.Substring(0, path.Length - 12);
      if (!lngTab.TryGetValue(path, out res)) return path;
      return res + (isNoDict ? "?nodict=true" : null);
    }

    public static string Delocalize(SecurityDir security, string name, Langs lng) {
      Dictionary<string, string> lngTab;
      if (!delocalize.TryGetValue(lng, out lngTab)) return name;
      string res;
      if (!lngTab.TryGetValue(security.ToString().ToLower() + "/" + name.ToLower(), out res)) return name;
      return res;
    }
  }
}
