using LMComLib;
using LMNetLib;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace DesignNew {

  public static class Consts {
    //mozne lokalizace dat. Pouzije se napr:
    //- pro zjisteni jazykove verze souboruu napr. .../exercise.cs_cz.js
    //- pro odliseni casti URL, napr. v ...test/common-cs_cz-skrivanek-mdl nebo schools/index
    public static Langs[] dataLocs = CommonLib.smallLocalizations;
    public static HashSet<string> allDataLocs = new HashSet<string>(dataLocs.Select(l => l.ToString())); //dir vsech dostupnych lokalizaci

    public static HashSet<string> allBrands = new HashSet<string>(LowUtils.EnumGetValues<servConfig.Brands>().Select(b => b.ToString()));
    public static HashSet<string> allSkins = new HashSet<string>(LowUtils.EnumGetValues<servConfig.SkinIds>().Select(b => b.ToString()));
    public static HashSet<string> allApps = new HashSet<string>(LowUtils.EnumGetValues<servConfig.Apps>().Select(b => b.ToString()));

    public static Langs[] swLangs = new Langs[] { Langs.ar_sa, Langs.bg_bg, Langs.bs, Langs.cs_cz, Langs.de_de, Langs.sp_sp, Langs.fr_fr, Langs.it_it, Langs.lt_lt, Langs.pl_pl, Langs.ru_ru, Langs.sk_sk, Langs.tr_tr, Langs.vi_vn, Langs.zh_cn };
    public static HashSet<string> allSwLangs = new HashSet<string>(swLangs.Select(l => l.ToString())); //dir vsech sw lokalizaci

    public static HashSet<string> gzipExtensions = new HashSet<string>(new string[] { ".css", ".html", ".js", ".otf", ".svg", ".woff", ".woff2", ".ttf", ".eot", ".map" });
    public static Dictionary<string, string> contentTypes = new Dictionary<string, string> {
      {".js", "application/x-javascript"},
      {".html", "text/html"},
      {".xml", "text/xml"},
      {".css", "text/css"},
      {".svg", "image/svg+xml"},
      {".ttf", "application/x-font-ttf"},
      {".otf", "application/x-font-opentype"},
      {".woff", "application/font-woff"},
      {".woff2", "application/font-woff2"},
      {".eot", "application/vnd.ms-fontobject"},
      {".map", "application/json"},
      

      {".pdf", "application/pdf"},
      {".xap", "application/x-silverlight-app"},
      {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},

      {".mp4", "video/mp4"},
      {".mp3", "audio/mpeg"},
      {".webm", "video/webm"},
      {".gif", "image/gif"},
      {".png", "image/png"},
      {".bmp", "image/bmp"},
      {".jpg", "image/jpg"},
    };

  }

  //**************************** Data group
  public enum BuildIds {
    blended,
    edusoft,
    english, french, german, italian, russian, spanish,
    grafia,
    lmtests,
    skrivanek
  }



}
