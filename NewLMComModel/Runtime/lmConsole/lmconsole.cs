using LMComLib;
using LMNetLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using lmc = lmConsole;

namespace LMComLib {
  public class lmConsoleSend {
    public long now { get { return _now; } set { _now = value; date = lmConsole.lib.formatDate(LowUtils.numToDate(value)); } } long _now;
    public string nowStr;
    public string email;
    public string replEmail;
    public string problem;
    public string action;
    public string other;
    [JsonIgnore]
    public string log; //data logu pro jednu Save akci uzivatele
    public string date;
    public bool hasError;
  }
  public class lmConsoleComp {
    public string compId;
    public lmConsoleSend[] sends;
  }
}

namespace lmConsole {

  public static class lib {
    public static void getZippedLogOnRemoteServer(Action<string> processFn) {
      try {
        var dir = Machines.rootPath + @"app_data\logs";
        var tempDir = Path.GetTempPath() + @"\lmconsole"; if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); Directory.CreateDirectory(tempDir); var zipFile = tempDir + @"\dataSrc.zip";
        ZipFile.CreateFromDirectory(dir, zipFile);
        processFn(zipFile);
        //AFTER FINISHING: 
        LowUtils.EmptyDir(dir);
      } catch { }
    }
    public static void downloadAndProcessLogs() {
      var nowStr = formatDate(DateTime.UtcNow);
      var globPath = Machines.rootPath + @"app_data\globalLogs\".ToLower();
      foreach (var srv in servers) {
        var serverId = srv.Key; var url = srv.Value; bool hasError = false;
        //Download and Unzip
        var nowDir = globPath + nowStr; if (!Directory.Exists(nowDir)) Directory.CreateDirectory(nowDir);
        var tempDir = nowDir + @"\" + serverId + @"\temp"; if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); Directory.CreateDirectory(tempDir);
        var actDir = nowDir + @"\" + serverId; Directory.CreateDirectory(actDir);
        var zipFile = actDir + @"\data.zip";
        var wc = new WebClient();
        var zipData = wc.DownloadData(url + "/mp3Uploader.ashx?phase=lmconsole-download&fileUrl=");
        File.WriteAllBytes(zipFile, zipData);
        ZipFile.ExtractToDirectory(zipFile, tempDir);
        //process log
        var srvLogs = new List<lmConsoleComp>();
        var files = Directory.GetFiles(tempDir, "*.log").Select(f => f.ToLower()).ToArray();
        var processed = new HashSet<string>();
        foreach (var js in files.Select(f => f.ToLower())) {
          var m = userLogMask.Match(js); if (!m.Success) continue;
          var servFn = js.Replace(".js.log", ".log"); if (!File.Exists(servFn)) continue;
          string servUnused = null;
          var res = new lmConsoleComp { //pro kazdy cmputer
            compId = m.Groups["id"].Value,
            sends = mergeLogs(File.ReadAllText(servFn), File.ReadAllText(js), s => servUnused = s).ToArray(), //pro kazdu computer a send
          };
          srvLogs.Add(res);
          //processed.Add(servFn); 
          File.WriteAllText(servFn, servUnused);
          processed.Add(js);
          foreach (var send in res.sends) {
            var sendFn = string.Format(@"{0}\{1}-{2}.htm", actDir, res.compId, send.now);
            var toTransform = new NewData.Runtime.lmConsole.withError(nowStr + " - " + serverId + " - " + res.compId + " - " + send.date, send.log, send);
            send.hasError = toTransform.count > 0;
            hasError = hasError || send.hasError;
            File.WriteAllText(sendFn, toTransform.TransformText());
          }
          var objFn = string.Format(@"{0}\{1}.json", actDir, res.compId);
          File.WriteAllText(objFn, JsonConvert.SerializeObject(res, Formatting.Indented));
        }
        var notProcessed = files.Where(f => !processed.Contains(f)).ToArray(); // not \ddd. 
        var externalErrors = new List<string>();
        foreach (var gl in notProcessed) {
          var destFn = gl.Replace(tempDir, actDir).Replace(".log", ".htm");
          var tx = File.ReadAllText(gl);
          if (tx.IndexOf("ERROR") >= 0) {
            var tr = new NewData.Runtime.lmConsole.withError(nowStr + " - " + serverId + " - " + Path.GetFileName(destFn), tx);
            hasError = hasError || tr.count > 0;
            File.WriteAllText(destFn, tr.TransformText());
            externalErrors.Add(Path.GetFileName(destFn));
          }
        }
        Directory.Delete(tempDir, true);
        //download index
        var htmlFn = string.Format(@"{0}\index.htm", actDir);
        File.WriteAllText(htmlFn, new NewData.Runtime.lmConsole.serverComp(serverId + ", " + nowStr, srvLogs, externalErrors).TransformText());
        //priznak chyby
        if (hasError) {
          htmlFn = string.Format(@"{0}\error.txt", actDir);
          File.WriteAllText(htmlFn, "");
        }
      }
      File.WriteAllText(globPath + @"\index.htm", new NewData.Runtime.lmConsole.index(globPath).TransformText());
    }

    public static string formatDate(DateTime now) {
      return string.Format(@"{0:D4}-{1:D2}-{2:D2} {3:D2}-{4:D2}", now.Year, now.Month, now.Day, now.Hour, now.Minute);
    }
    public static string markError(string txt, ref int cnt) { //, Func<int, string> getUrl = null) {
      int count = cnt;
      //var res = errorMask.Replace(txt, s => "<a name=\"b" + (getUrl == null ? count++.ToString() : getUrl(count++)) + "\" class=\"label label-danger\">ERROR</a>");
      var res = errorMask.Replace(txt, s => "<a name=\"b" + count++.ToString() + "\" class=\"label label-danger\">ERROR</a>");
      cnt = count; return res;
    } static Regex errorMask = new Regex("ERROR", RegexOptions.Singleline);


    static Regex userLogMask = new Regex(@"\\(?<id>\d{13,})\.js\.log$", RegexOptions.IgnoreCase);
    static Regex globalLogMask = new Regex(@"\\\d{13,}\.", RegexOptions.IgnoreCase);
    static Regex extractJsonMask = new Regex(@"\*{30,}\r\n(?<json>{.*?})\r\n\*{30,}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    static Dictionary<string, string> servers = new Dictionary<string, string> { 
      //{"pz-local","http://localhost/rew"},
      {"fe5","http://test.langmaster.com/alpha"},
      {"edusoft","http://www.eduland.vn/newlm"},
      //{"lm_fe3","http://www.langmaster.com/new"},
      {"skrivanek","http://onlinetesty.skrivanek.cz"},
      //{"edusofttest","http://test.langmaster.com/edusofttest"},
      //{"alan","http://langmaster.jjlearning.com.mx/new"},
      //{"grafia","http://elearning.odbornanemcina.cz"},
    };

    static IEnumerable<lmConsoleSend> mergeLogs(string servTxt, string clientTxt, Action<string> setUnused) {
      //server parsing: vysledkem je adresar evidovanych log zaznamu
      var servMts = servLogMask.Matches(servTxt).OfType<Match>().ToArray();
      Dictionary<string, string[]> servData = new Dictionary<string, string[]>();
      string servUnused = null;
      if (servMts.Length > 0)
        for (int i = 0; i < servMts.Length; i++) {
          var startPos = servMts[i].Groups["cont"].Index;
          if (i == 0 && startPos > 10) servUnused = servTxt.Substring(0, startPos);
          var endPos = i == servMts.Length - 1 ? servTxt.Length - 1 : servMts[i + 1].Index;
          var id = servMts[i].Groups["id"].Value;
          servData.Add(id, servTxt.Substring(startPos, endPos - startPos).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Where(l => l != "#>").ToArray());
        } else
        servUnused = servTxt;
      var servUsed = new HashSet<string>();
      //client parsing: k evidovanemu JS log zaznamu prirad server log zaznam
      var clientMts = extractJsonMask.Matches(clientTxt).OfType<Match>().ToArray(); //separace jednotlivych SENDS
      for (int i = 0; i < clientMts.Length; i++) {
        lmConsoleSend obj = JsonConvert.DeserializeObject<lmConsoleSend>(clientMts[i].Groups["json"].Value); //jeden SEND
        var startPos = clientMts[i].Index + clientMts[i].Length;
        var endPos = i == clientMts.Length - 1 ? clientTxt.Length - 1 : clientMts[i + 1].Index;
        var clientLines = clientTxt.Substring(startPos, endPos - startPos).Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        var merged = new List<string>();
        foreach (var clLine in clientLines) { //merge jeden save log s server log
          merged.Add(clLine);
          var m = clLineMask.Match(clLine);
          if (m.Success) {
            string[] sls;
            var id = m.Groups["id"].Value;
            if (!servData.TryGetValue(id, out sls)) continue;
            servUsed.Add(id);
            foreach (var sl in sls) merged.Add("     >" + sl);
          }
        }
        //unused server log parts
        foreach (var kv in servData) {
          if (servUsed.Contains(kv.Key)) continue;
          servUnused += kv.Value.Aggregate((r, l) => r + "\r\n" + l);
        }
        obj.log = merged.Count == 0 ? null : merged.Aggregate((r, l) => r + "\r\n" + l);
        yield return obj;
      }
      setUnused(servUnused);
    }
    static Regex servLogMask = new Regex(@"<#(?<id>\d{13,}) (?<cont>.*?)#>", RegexOptions.Singleline);
    static Regex clLineMask = new Regex(@"^<#(?<id>\d{13,}) ");
  }
}

namespace NewData.Runtime.lmConsole {

  public partial class withError {
    public withError(string title, string txt, lmConsoleSend send = null) {
      text = lmc.lib.markError(txt, ref count).Replace("\r\n", "<br/>");
      this.title = title; this.send = send;
      if (this.send!=null && string.IsNullOrEmpty(this.send.date)) this.send.date = "no-date";
    }
    public string text;
    public string title;
    public int count;
    public lmConsoleSend send;
  }

  public partial class serverComp {
    public serverComp(string title, List<lmConsoleComp> comps, List<string> externalErrors) {
      this.title = title; this.comps = comps;
      this.externalErrors = externalErrors;
    }
    public string title;
    public List<string> externalErrors;
    public List<lmConsoleComp> comps;
  }

  public partial class index {
    public index(string globPath) {
      var errors = new HashSet<string>(Directory.GetFiles(globPath, "*error.txt", SearchOption.AllDirectories).Select(f => f.ToLower().Substring(globPath.Length).Replace(@"\error.txt", null)));
      var indexes = Directory.GetFiles(globPath, "*index.htm", SearchOption.AllDirectories).Select(f => f.ToLower().Substring(globPath.Length)).Where(f => f != "index.htm");
      data = indexes.Select(idx => idx.Split('\\')).Select(p => new titleUrl {
        title = p[0],
        server = p[1],
        url = p[0] + "/" + p[1] + "/index.htm",
        hasError = errors.Contains(p[0] + "\\" + p[1])
      }).GroupBy(tu => tu.title);
    }
    public class titleUrl { public string server; public string title; public string url; public bool hasError; }
    public IEnumerable<IGrouping<string, titleUrl>> data;
  }

}