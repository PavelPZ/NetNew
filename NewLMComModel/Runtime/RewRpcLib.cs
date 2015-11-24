#if !net35
namespace LMComLib {
#else
namespace LMComLibNet35 {
#endif

  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Web;
  using System.IO;
  using System.Web.Script.Serialization;

  public class RpcResponse {
    public RpcResponse() { }
    public RpcResponse(object result) { this.result = result; }
    public RpcResponse(int error, string errorText) { this.error = error; this.errorText = errorText; }
    public string errorText;
    public int error;
    public object result;
  }

  public static class RewRpcLib {

    public static void registerCommand(reqParLow cmd) { commands.Add(cmd); }

    public abstract class reqParLow {
      public abstract bool isMyType(string type);
      public abstract string ProcessRequest(HttpContext context);
    }

    public static void ProcessRequest(HttpContext context, Action<Exception> catchExp, params reqParLow[] forceCommands) {
      try {
        ProcessRequest(context, forceCommands);
      } catch (System.Threading.ThreadAbortException errExp) {
      } catch (Exception exp) {
        if (catchExp != null) catchExp(exp); else throw;
      }
    }

    public static string getPostData(HttpContext context) {
      var data = context.Request["par"]; //data z formulare, dosazena pro crossdomain v Ajax.js, iFrameSubmit
      if (data == null) using (Stream str = context.Request.InputStream) using (StreamReader rd = new StreamReader(str)) data = rd.ReadToEnd(); //data v body, dosazena pri not crossdomain v doAjax
      if (data == null) throw new Exception();
      return data;
    }

    static List<reqParLow> commands = new List<reqParLow>();

    static Dictionary<string, bool> guidProcessed = new Dictionary<string, bool>();
    static TimeSpan maxWait = new TimeSpan(0, 0, 5);

    static void ProcessRequest(HttpContext context, params reqParLow[] forceCommands) {
      //**** crossdomain POST START
      string waitfor = context.Request["waitfor"];
      if (!string.IsNullOrEmpty(waitfor)) { //async IForm callback
        var time = DateTime.UtcNow;
        while (true) {
          System.Threading.Thread.Sleep(100);
          var t = DateTime.UtcNow - time;
          if (t > maxWait) { guidProcessed.Remove(waitfor); break; }
          lock (typeof(RewRpcLib)) {
            if (!guidProcessed.ContainsKey(waitfor)) continue;
            guidProcessed.Remove(waitfor);
            break;
          }
        }
        context.Response.ContentType = "text/plain";
        context.Response.Write(context.Request["callback"] + "()"); 
        context.Response.End();
      }

      string type = context.Request["type"] ?? context.Request["file"]; //CD Cargo Hack
      //Logger.Log("RewRpcLib.ProcessRequest: type={0}", type);
      reqParLow par = forceCommands.Concat(commands).FirstOrDefault(p => p.isMyType(type));
      if (par != null) {
        string txt = par.ProcessRequest(context);
        //Guid pozadavek
        context.Response.Write(txt);
        context.Response.Flush();
      } else {
        Logger.Error("RewRpcLib.ProcessRequest: cannot find type " + type);
        //throw new Exception("Cannot find");
      }

      //**** crossdomain POST END
      string guid = context.Request["guid"];
      if (guid != null) lock (typeof(RewRpcLib)) guidProcessed[guid] = true;

      context.Response.End();
    }

  }

}
