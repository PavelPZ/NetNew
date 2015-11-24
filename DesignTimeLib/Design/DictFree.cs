using LMComLib;
using LMNetLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Xml.Linq;

//http://fr.thefreedictionary.com/enqu
//http://invokeit.wordpress.com/frequency-word-lists/
//http://en.wiktionary.org/wiki/Wiktionary:Frequency_lists
namespace FreeDict {

  public class pcConfig {
    public string path;
    public string langStr;
    public Langs lang;
    public int minFileCnt;
    public static Dictionary<string, pcConfig> configs = new Dictionary<string, pcConfig>() { 
      {"lm-frontend-5", new pcConfig{path=@"c:\temp\", langStr="ru", lang=Langs.ru_ru, minFileCnt = 0}},
      {"sharedvirtual", new pcConfig{path=@"c:\temp\", langStr="web", lang=Langs.en_gb, minFileCnt = 1000}},
      {"pz-w8virtual", new pcConfig{path=@"c:\temp\", langStr="de", lang=Langs.de_de, minFileCnt = 2000}},
      {"pz-w8", new pcConfig{path=@"c:\temp\", langStr="fr", lang=Langs.fr_fr, minFileCnt = 3000}},
      //{"lm-frontend-3", new pcConfig{path=@"c:\temp\", langStr="it", lang=Langs.it_it, minFileCnt = 4000}},
      //{"lm-frontend-4", new pcConfig{path=@"c:\temp\", langStr="sp", lang=Langs.sp_sp, minFileCnt = 5000}},
    };
  }

  public static class Lib {

    static void runEntry() {
      //WebClient wc = new WebClient();
      //wc.Headers.Add("user-agent", "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)");
      while (true) {
        wordUrl word;
        if (words.Count == 0) break;
        word = words.Pop();
        string url = string.Format("http://{0}.thefreedictionary.com/{1}", word.langCode, HttpUtility.UrlEncode(word.word));
        bool inError = false;
      start:
        try {
          string data = downloadUrl(url);
          //var data = wc.DownloadData(url);
          writeWord(word.word, data);
          inError = false;
        } catch (Exception exp) {
          if (!inError) { Console.Write('*'); }
          inError = true;
          Thread.Sleep(2000); goto start;
        }
        if (DateTime.UtcNow - lastWriteTime > new TimeSpan(0, 30, 0)) { writeWords(); lastWriteTime = DateTime.UtcNow; fileDone.Clear(); }
      }
    }

    static void writeWord(string word, string html) {
      fileDone.Add(word.ToString());
      var srcFn = @"c:\temp\pom.htm"; var resFn = @"c:\temp\pom.xml";
      File.WriteAllText(srcFn, html);
      string runTidy = string.Format(@"{0}Ultralingua.back\design\tidy.exe -config {0}Ultralingua.back\design\tidy.cfg ", cfg.path) + srcFn + " > " + resFn;
      Packager.RewApp.run(new string[] { runTidy });
      var xml = XElement.Load(resFn);
      var data = xml.DescendantsAttr("id", "MainTxt").FirstOrDefault();
      XElement res = new XElement("div", new XAttribute("class", "langmaster-header"), new XElement("h1", word), data);
      fileXml.Add(res);
      actDone++; actToDo--;
      avgCount++;
      if (avgCount == 50) {
        var spend = DateTime.UtcNow - avgDate;
        var avg = (int) ((double)avgCount / spend.TotalHours);
        Console.WriteLine(); Console.Write(string.Format("done={0}, {1}/hod, todo={2}:", actDone, avg, actToDo));
        avgDate = DateTime.UtcNow; avgCount = 0;
      }
      Console.Write('.');
    }


    static void writeWords() {
      var resFn = string.Format(cfg.path + @"TheFreeDict.back\web\dict_{0}.xml", fileCount++);
      var done = File.ReadAllLines(doneFn).Concat(fileDone).Distinct().ToArray();
      fileXml.Save(resFn);
      fileXml = new XElement("root");
      File.WriteAllLines(doneFn, done);
      Console.WriteLine();
      logFmt("fileCount={0}", fileCount);
    }

    public struct wordUrl {
      public string word;
      public string langCode;
      public Langs lang;
      public override string ToString() {
        return lang.ToString() + "|" + word;
      }
    }
    static Stack<wordUrl> words;
    static HashSet<string> fileDone;
    static int fileCount;
    static XElement fileXml;
    static DateTime lastWriteTime = DateTime.UtcNow;
    static pcConfig cfg;
    static string doneFn;
    static int actDone;
    static int actToDo;
    static Func<string, string> downloadUrl;
    static DateTime avgDate;
    static int avgCount;

    static void logFmt(string msg, params object[] pars) {
      Console.WriteLine(string.Format(msg, pars));
    }

    public static void run(Func<string,string> _downloadUrl) {
      downloadUrl = _downloadUrl;
      logFmt("machine={0}", Machines.machine);
      cfg = pcConfig.configs[Machines.machine];
      logFmt("lang={0}, path={1}", cfg.lang, cfg.path);
      doneFn = cfg.path + string.Format(@"TheFreeDict.back\web\done_{0}_{1}.txt", cfg.minFileCnt, cfg.lang);
      LowUtils.AdjustFileDir(doneFn);
      if (!File.Exists(doneFn)) File.WriteAllText(doneFn, "");
      fileDone = new HashSet<string>();
      fileXml = new XElement("root");
      //foreach (var driver in drivers) init(driver);
      fileCount = Directory.EnumerateFiles(cfg.path + @"TheFreeDict.back\web", "dict_*.xml").
        Select(fn => fn.Split(new char[] { '.', '_' })).
        Select(parts => int.Parse(parts[parts.Length - 2]) + 1).
        DefaultIfEmpty().
        Max();
      fileCount = Math.Max(fileCount, cfg.minFileCnt);
      var done = new HashSet<string>(File.ReadAllLines(doneFn));
      var pom = File.ReadAllLines(string.Format(cfg.path + @"Ultralingua.back\design\WordsStems_{0}.txt", cfg.lang)).Select(w => new wordUrl {
        word = w,
        langCode = cfg.langStr,
        lang = cfg.lang
      }).Where(w => !string.IsNullOrEmpty(w.word) && !done.Contains(w.ToString()));
      words = new Stack<wordUrl>(pom);
      actDone = done.Count; actToDo = words.Count;
      logFmt("todo={0}", actToDo);
      Console.Write(actDone.ToString() + ": ");
      avgDate = DateTime.UtcNow; avgCount = 0;
      runEntry();
      writeWords();
    }

  }


}
