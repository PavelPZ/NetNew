using LMComLib;
using System.IO;

namespace ClearScript {
  public static class Lib {
    public static string JSON2RJSON(string inpJson) {
      using (var engine = new Microsoft.ClearScript.V8.V8ScriptEngine()) {
        var script = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\RJSON.js";
        if (!File.Exists(script)) script = Machines.rootPath + @"JsLib\JS\External\RJSON.js";
        engine.Execute(script, File.ReadAllText(script));
        var par = new rjsonObj { inpJson = inpJson };
        engine.AddHostObject("inpJson", par);
        return (string)engine.Evaluate("JSON.stringify(RJSON.pack(JSON.parse(inpJson.inpJson)))");
      }
    }
    public class rjsonObj { public string inpJson; public string inpTSCode; public bool isDebug;}

    public static void JsReflection(string tsFileName, string jsonMapFn) {
      using (var engine = new Microsoft.ClearScript.V8.V8ScriptEngine()) {
        engine.Execute("typescript.js", File.ReadAllText(@"d:\LMCom\rew\Web4\JsLib\Scripts\typescript.js"));
        engine.Execute("underscore.js", File.ReadAllText(@"d:\LMCom\rew\Web4\JsLib\Scripts\underscore.js"));
        engine.Execute("GenerateReflection.js", File.ReadAllText(@"d:\LMCom\rew\Web4\Author\GenerateReflection.js"));
        var par = new reflectionObj { inpTSCode = File.ReadAllText(tsFileName) };
        engine.AddHostObject("inpTSCode", par);
        File.WriteAllText(jsonMapFn + ".json", (string)engine.Evaluate("author.parseReflection(inpTSCode.inpTSCode, false)"));
        File.WriteAllText(jsonMapFn + ".debug.json", (string)engine.Evaluate("author.parseReflection(inpTSCode.inpTSCode, true)"));
      }
    }
    public class reflectionObj { public string inpTSCode; public bool isDebug; }
  }
}