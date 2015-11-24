//using ProductsApp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
//using WebApiProxy.Core.Models;
//using WebApiProxy.Server;
//using WebApiProxy;


namespace jsWebApiProxy {

  //public class ExcludeProxy : Attribute {
  //}

  public class ControllerDefinition {
    public string Name { get; set; }
    //public string Description { get; set; }
    public IEnumerable<ActionMethodDefinition> ActionMethods { get; set; }
  }
  public class ActionMethodDefinition {
    public string HttpMethod { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public IEnumerable<ParameterDefinition> UrlParameters { get; set; }
    public ParameterDefinition BodyParameter { get; set; }
    //public string Description { get; set; }
    public Type ReturnType { get; set; }
  }
  public class ParameterDefinition {
    public string Name;
    public Type Type;
    //public string Description;
    //public bool IsOptional;
    //public object DefaultValue;
  }

  //public static class HttpActionDescriptorExtensions {
  //  public static bool IsExcluded(this HttpActionDescriptor descriptor) {
  //    return descriptor.GetCustomAttributes<ExcludeProxy>(true).Any();
  //  }
  //}

  public static class jsProxyGenerator {

    public static string LMGetJSProxy() {
      var cfg = GlobalConfiguration.Configuration;
      var descriptions = cfg.Services.GetApiExplorer().ApiDescriptions;
      var defs = LMGetMetadata(descriptions);
      var template = new DesignTimeLib.Design.jsWebApiProxy.WebApiProxyTempl(defs);
      return template.TransformText();
    }

    static IEnumerable<ControllerDefinition> LMGetMetadata(Collection<ApiDescription> descriptions) {

      var apiGroups = descriptions
          .Where(a => !a.ActionDescriptor.ControllerDescriptor.ControllerType.IsAbstract
              && !a.RelativePath.Contains("Swagger")
              && !a.RelativePath.Contains("docs"))
          .ToLookup(a => a.ActionDescriptor.ControllerDescriptor);

      return apiGroups.Select(d => new ControllerDefinition {
        Name = d.Key.ControllerName,
        ActionMethods = descriptions.Where(a => !a.RelativePath.Contains("Swagger")
          && !a.RelativePath.Contains("docs")
          && a.ActionDescriptor.ControllerDescriptor.ControllerName == d.Key.ControllerName).
        Select(a => new ActionMethodDefinition {
          Name = a.ActionDescriptor.ActionName,
          BodyParameter = a.ParameterDescriptions.
            Where(b => b.Source == ApiParameterSource.FromBody).
            Select(b => new ParameterDefinition {
              Name = b.ParameterDescriptor.ParameterName,
              Type = b.ParameterDescriptor.ParameterType,
            }).
            FirstOrDefault(),
          UrlParameters = a.ParameterDescriptions.
            Where(b => b.ParameterDescriptor != null && b.Source == ApiParameterSource.FromUri).
            Select(b => new ParameterDefinition {
              Name = b.ParameterDescriptor.ParameterName,
              Type = b.ParameterDescriptor.ParameterType,
            }),
          Url = a.RelativePath,
          ReturnType = a.ResponseDescription.ResponseType ?? a.ResponseDescription.DeclaredType,
          HttpMethod = a.HttpMethod.Method
        })
      }).
      Distinct().
      OrderBy(d => d.Name);
    }

  }


}
