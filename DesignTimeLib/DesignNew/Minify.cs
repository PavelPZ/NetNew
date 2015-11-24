using LMComLib;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Yahoo.Yui.Compressor;

namespace DesignNew {

  public static class minifier {

    static string jsMinify(IEnumerable<string> files) {
      StringBuilder sb = new StringBuilder(); 
      var compr = new JavaScriptCompressor();
      foreach (var fn in files) {
        var file = File.ReadAllText(fn, Encoding.UTF8);
        var res = compr.Compress(file);
        sb.Append(res); 
      }
      return sb.ToString();
    }

    public static void jsMinify(string dplUrl, string dest, Langs lng = Langs.no) {
      var files = FileSources.pathsFromDpl(dplUrl, lng).ToArray();
      var destFn = FileSources.pathFromUrl(dest);
      var res = jsMinify(files);
      File.WriteAllText(destFn, res);
      Trace.TraceInformation("Minified JS: " + destFn);
    }

    public static void cssInPlaceMinify(string dplUrl) {
      var files = FileSources.pathsFromDpl(dplUrl).ToArray();
      var compr = new CssCompressor();
      foreach (var fn in files) {
        var res = compr.Compress(File.ReadAllText(fn, Encoding.UTF8));
        var destFn = fn.Replace(".css", ".min.css");
        File.WriteAllText(destFn, res);
        Trace.TraceInformation("Minified CSS: " + destFn);
      }
    }
  }


}
