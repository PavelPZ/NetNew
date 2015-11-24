using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LMComLib;
using LMNetLib;
using System.Configuration;
using System.IO.Compression;

namespace DesignNew {

  public static class FileSources {

    public static IEnumerable<string> pathsFromDpl(string dplPath, Langs lang = Langs.no) {
      var urls = filesFromDpl(dplPath, lang == Langs.no ? null : Consts.swLangs);
      return urls.Select(f => pathFromUrl(f));
    }

    public static IEnumerable<string> getUrls(filter filt) {
      return getDpls(filt).SelectMany(dpl => filesFromDpl(dpl, filt.langs));
    }

    public static string pathFromUrl(string url) {
      return basicPath(url) + url.Replace('/', '\\');
    }

    public static filter zipSWFilesFilter(params servConfig.Apps[] apps) {
      return new filter {
        apps = apps.ToArray(), // LowUtils.EnumGetValues<servConfig.Apps>().ToArray(),
        langs = Consts.swLangs,
        allBrendMasks = new string[] { brendJSMask, brendCSSMask, brendMMMask },
        allSkinMasks = new string[] { skinJSMask, skinCSSMask, skinMMMask },
        allFixs = new string[] { "js", "css", "jsmin", "cssmin", "mm", "js{loc}", "js{loc}min", "js-other" },
        allSkins = Consts.allSkins.ToArray(),
        allBrands = Consts.allBrands.ToArray()
      };
    }

    public static filter indexPartFilter(bool isJS, servConfig.Apps app, servConfig.SkinIds skin, servConfig.Brands brand, Langs lang, bool isMin) {
      return new filter {
        apps = new servConfig.Apps[] { app },
        langs = new Langs[] { lang },
        allBrendMasks = new string[] { isJS ? brendJSMask : brendCSSMask },
        allSkinMasks = new string[] { isJS ? skinJSMask : skinCSSMask },
        allBrands = new string[] { brand.ToString() },
        allSkins = new string[] { skin.ToString() },
        allFixs = isJS
          ? (!isMin ? new string[] { "js", "js{loc}" } : new string[] { "jsmin", "js{loc}min" })
          : (!isMin ? new string[] { "css" } : new string[] { "cssmin" })
      };
    }

    public class deplyConfig {
      //public string[] regExs; //regular expression filter na soubory v self adresari
      public string[] includes; //pridej soubory
    }

    public class filter {
      public servConfig.Apps[] apps;
      public Langs[] langs;
      public string[] allFixs;
      public string[] allBrendMasks;
      public string[] allBrands;
      public string[] allSkinMasks;
      public string[] allSkins;
    }

    public static string swLang(Langs lang) {
      var lngStr = lang.ToString().Replace('_', '-');
      return lngStr == "sp-sp" ? "es-es" : lngStr;
    }

    public static long zipSWFiles(string zipFile, IEnumerable<string> urls) {
      if (File.Exists(zipFile)) File.Delete(zipFile);
      long len = 0;
      using (var zipStr = File.OpenWrite(zipFile))
      using (ZipArchive zip = new ZipArchive(zipStr, ZipArchiveMode.Create)) {
        var memBuf = new MemoryStream();
        foreach (var url in urls.Select(u => u.ToLower()).Distinct()) {
          var gzipAble = Consts.gzipExtensions.Contains(Path.GetExtension(url));
          ZipArchiveEntry entry = zip.CreateEntry(url, gzipAble ? CompressionLevel.Optimal : CompressionLevel.NoCompression);
          var data = File.ReadAllBytes(FileSources.pathFromUrl(url));
          using (var str = entry.Open()) str.Write(data, 0, data.Length);
          len += data.Length;
          if (gzipAble && data.Length > 100) {
            entry = zip.CreateEntry(url + ".gzip", CompressionLevel.NoCompression);
            memBuf.SetLength(0);
            using (var gzipStr = new GZipStream(memBuf, CompressionMode.Compress, true)) gzipStr.Write(data, 0, data.Length);
            var gzipData = memBuf.ToArray();
            len += gzipData.Length;
            using (var str = entry.Open()) str.Write(gzipData, 0, gzipData.Length);
          }
        }
      }
      return len;
    }

    //************************ PRIVATES
    static HashSet<string> web4Dirs = new HashSet<string>(new string[] { "admin", "app_data", "author", "blendedapi", "courses", "jslib", "login", "schools", "testme" });
    static string web4Dir = ConfigurationManager.AppSettings["filesources.web4"] ?? @"d:\LMCom\rew\Web4";
    static string commonDir = ConfigurationManager.AppSettings["filesources.webcommon"] ?? @"d:\LMCom\rew\WebCommon\wwwroot";
    static string basicPath(string url) { return web4Dirs.Contains(url.Split(new char[] { '/' }, 3)[1]) ? web4Dir : commonDir; }
    public static string urlFromPath(string path) { return (path.Substring(path.StartsWith(web4Dir) ? web4Dir.Length : commonDir.Length)).Replace('\\', '/'); }

    static IEnumerable<string> filesFromDpl(string dplUrl, Langs[] langs) {
      var dplPath = pathFromUrl(dplUrl);
      var cfg = Newtonsoft.Json.JsonConvert.DeserializeObject<deplyConfig>(File.ReadAllText(dplPath));
      HashSet<string> res = new HashSet<string>();
      var relDir = dplUrl.Substring(0, dplUrl.LastIndexOf('/'));
      Func<string, string> fullUrl = url => (url.StartsWith("/") ? url : relDir + "/" + url).ToLower();
      if (dplUrl.IndexOf("{loc}") > 0) {
        foreach (var inc in cfg.includes) foreach (var lng in langs) res.Add(string.Format(fullUrl(inc), swLang(lng)));
      } else {
        //** includes x excludes x regExs
        if (cfg.includes != null)
          foreach (var inc in cfg.includes) {
            if (inc.StartsWith("@")) {
              var regEx = inc.Substring(1);
              var urls = Directory.GetFiles(commonDir, "*.*", SearchOption.AllDirectories).Select(f => urlFromPath(f)).ToArray();
              var rx = new Regex(regEx, RegexOptions.IgnoreCase);
              foreach (var url in urls) if (rx.IsMatch(url)) res.Add(url);
            } else if (inc.EndsWith(".json")) res.UnionWith(filesFromDpl(fullUrl(inc), langs));
            else if (inc.StartsWith("!")) res.Remove(fullUrl(inc));
            else res.Add(fullUrl(inc));
          }
      }
      return res.Distinct();
    }

    const string brendJSMask = "jsbrend-{0}";
    const string brendCSSMask = "cssbrend-{0}";
    const string brendMMMask = "mmbrend-{0}";
    const string skinJSMask = "jsskin-{0}";
    const string skinCSSMask = "cssskin-{0}";
    const string skinMMMask = "mmskin-{0}";

    //static string dplPath(servConfig.Apps app, string name, string mask) { return string.Format(@"{0}\deploy\{1}\{2}.dpl.json", commonDir, app, mask == null ? name : string.Format(mask, name)); }
    static string dplUrl(servConfig.Apps app, string name, string mask) { return string.Format(@"/deploy/{0}/{1}.dpl.json", app, mask == null ? name : string.Format(mask, name)); }

    static IEnumerable<string> existedDpls(servConfig.Apps app, IEnumerable<string> names, string mask) { //dej dpl urls, ktere skutecne existuji
      return names.Select(n => dplUrl(app, n, mask)).Where(url => File.Exists(pathFromUrl(url)));
    }

    static IEnumerable<string> getDpls(filter filt = null) {
      Func<servConfig.Apps, IEnumerable<string>> allApp = app => {
        var fix = existedDpls(app, filt.allFixs, null);
        var brend = filt.allBrendMasks.SelectMany(m => existedDpls(app, filt.allBrands, m));
        var skin = filt.allSkinMasks.SelectMany(m => existedDpls(app, filt.allSkins, m));
        return fix.Concat(brend).Concat(skin);
      };
      var res = filt.apps.SelectMany(app => allApp(app));
      return res;
    }

  }
}

