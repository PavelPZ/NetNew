using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pr = jsWebApiProxy;


namespace DesignTimeLib.Design.jsWebApiProxy {

  public partial class WebApiProxyTempl {

    static WebApiProxyTempl() {
      allTypes = new HashSet<string>(LMComLib.CSharpToTypeScript.GeneratedTypes(Handlers.CSharpToTypescript.allObjects).Select(t => t.FullName));
    }

    public WebApiProxyTempl(IEnumerable<pr.ControllerDefinition> definitions) {
      this.Definitions = definitions;
    }

    public IEnumerable<pr.ControllerDefinition> Definitions { get; set; }
    static HashSet<string> allTypes;

    public string declarePars(pr.ActionMethodDefinition method) {
      var allParameters = method.UrlParameters.AsEnumerable();
      if (method.BodyParameter != null) allParameters = allParameters.Concat(new[] { method.BodyParameter });
      var selectedParameters = allParameters.Where(m => m != null).Select(m => m.Name.ToLower() + ": " + LMComLib.CSharpToTypeScript.GenInlineTypeParse(m.Type,allTypes)).ToList();
      var retType = LMComLib.CSharpToTypeScript.GenInlineTypeParse(method.ReturnType, allTypes);
      selectedParameters.Add("completed: " + (string.IsNullOrEmpty(retType) ? "() => void" : "(res: " + retType + ") => void"));
      return string.Join(", ", selectedParameters);
    }

    //products/createlmuserstart/{password} => 'products/createlmuserstart/' + password
    public string url(pr.ActionMethodDefinition method) {
      string url = method.Url.Split('?')[0];
      return ("'" + url.Replace("{", "' + ").Replace("}", " + '") + "'").Replace(" + ''", "");
    }
    public string invokePars(pr.ActionMethodDefinition method) {
      var invokePar = new {
        url = url(method).ToLower(),
        method = method.HttpMethod.ToLower(),
        queryPars = method.UrlParameters.Count() == 0 ? (string)null : "{ " + method.UrlParameters.Select(p => p.Name.ToLower()).Select(p => p + ": " + p).DefaultIfEmpty().Aggregate((r, i) => r + ", " + i) + " }",
        body = method.BodyParameter != null ? "JSON.stringify(" + method.BodyParameter.Name.ToLower() + ")" : "null"
      };
      return string.Format("{0}, '{1}', {2}, {3}, completed", invokePar.url, invokePar.method, invokePar.queryPars ?? "null", invokePar.body);
    }

  }
}
