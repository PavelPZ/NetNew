using LMComLib;
using LMNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseMeta {
  public static class LibLow {
    public static string prodsExpandedFn { get { return Machines.dataPath + "productsExpanded.xml"; } }
    public static Dictionary<string, product> runtimeProdExpanded() {
      return _runtimeProdExpanded ?? (_runtimeProdExpanded = XmlUtils.FileToObject<products>(prodsExpandedFn).Items.Cast<product>().ToDictionary(p => p.url, p => p));
    }
    static Dictionary<string, product> _runtimeProdExpanded;
    public static product getRuntimeProd(string url) {
      url = url.Split('|')[0];
      product prod;
      return runtimeProdExpanded().TryGetValue(url, out prod) ? prod : null;
    }
  }
}
