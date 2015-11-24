using LMComLib;
using LMNetLib;
using schools;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;

namespace NewData {

  public static class Schools {

    static Schools() {

      /* SchoolCmdGetDict */
      Handlers.CmdService.registerCommand<SchoolCmdGetDict, string>(par => {
        return new RpcResponse(getDictWord(par.dictId, par.word));
      });

    }

    public static string getDictWord_(string dictId, string word) {
      string url = string.Format("http://www.lingea.cz/ilex51/lms/Home.aspx/lms?word={0}&dict={1}", HttpUtility.UrlEncode(word), HttpUtility.UrlEncode(dictId));
      string html = null;
      try {
        html = LowUtils.DownloadStr(url);
      } catch (Exception exp) {
        html = "*" + url;
      }
      //html = LowUtils.DownloadStr(url);
      StringBuilder sb = new StringBuilder();
      foreach (regExItem ri in regExItem.Parse(html, rxEntry)) {
        if (ri.IsMatch) { sb.Append('<'); sb.Append(ri.Value.Substring(2, ri.Value.Length - 4)); sb.Append('>'); } else sb.Append(HttpUtility.HtmlEncode(ri.Value));
      }
      return sb.ToString();
    }
    static Regex rxEntry = new Regex("\\\\<.*?\\\\>");

    public static string getDictWord(string dictId, string word) {
      string url = string.Format("http://lingea.eu/lms/Home/lms?dict=_{1}&word={0}", HttpUtility.UrlEncode(word), HttpUtility.UrlEncode(dictId));
      string html = null;
      try {
        html = LowUtils.DownloadStr(url);
        html = html.Replace('\'','"');
        XElement root = XElement.Parse(html);
        html = root.ToString();
      } catch (Exception exp) {
        html = "*" + url;
      }
      return html;
    }
  }
}
