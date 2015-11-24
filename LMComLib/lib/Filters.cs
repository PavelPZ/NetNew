using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;
using System.Xml;
using System.Xml.Linq;

namespace LMComLib {
  public static class Filter {
    public static bool isMatch(HttpContext ctx, string filterId, string value) {
      try {
        Regex ex = null; 
        lock (typeof(Filter)) {
          ex = (Regex)ctx.Cache["filter_" + filterId];
          if (ex == null) {
            //string fn = ctx.Server.MapPath("~/app_data/filters.xml");
            string fn = Machines.basicPath + @"rew\LMCom\App_Data\Filters.xml";
            if (!File.Exists(fn)) return false;
            XElement root = XElement.Load(fn);
            StringBuilder sb = new StringBuilder(); sb.Append('(');
            foreach (XElement el in root.Elements(filterId).Single().Elements()) {
              if (sb.Length > 3) sb.Append(")|("); sb.Append(el.Value);
            }
            sb.Append(')');
            ex = new Regex(sb.ToString());
            ctx.Cache.Insert("filter_" + filterId, ex, new CacheDependency(fn));
          }
        }
        return ex.IsMatch(value);
      } catch {
        return false;
      }
    }
  }
}
