using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;

using LMNetLib;

namespace LMComLib {

  public static class AspxParser {
    /*-------------- Parse ---------------- */
    static Regex rxScriptCData = new Regex(@"//<\!\[CDATA\[.*?//\]\]>", RegexOptions.Singleline);
    static Regex rxMetaComment = new Regex(@"<%--TB--%>.*?<%--TE--%>", RegexOptions.Singleline);
    static Regex rxComment = new Regex(@"<%--.*?--%>", RegexOptions.Singleline);
    static Regex rxDocTyx = new Regex("<!DOCTYPE.*?>");
    static Regex rxScript = new Regex(@"<script.*?</script>", RegexOptions.Singleline);
    static Regex rxAspScript = new Regex(@"<%.*?%>", RegexOptions.Singleline);
    static Regex rxOpenTag = new Regex(@"<\w+.*?>", RegexOptions.Singleline);
    static Regex rxCodedAspScript = new Regex(@"#{#\d+?#}#", RegexOptions.Singleline);
    public static Regex rxTagAttr = new Regex("\".*?\"");
    public static Regex rxTagExAttr = new Regex("'#{#.*?#}#'");
    /*-------------- ToString ---------------- */
    static Regex rxDecodeScriptCData = new Regex(@"\$\$\{\$\$.*?\$\$\}\$\$", RegexOptions.Singleline);
    static Regex rxCData = new Regex(@"<!\[CDATA\[.*?\]\]>", RegexOptions.Singleline);
    static Regex rxTagScript = new Regex(@"regexattr_.*?%}""", RegexOptions.Singleline);
    static Regex rxAttr = new Regex(@"{%.*?%}", RegexOptions.Singleline);
    static Regex rxAttrEx = new Regex(@"""{'%.*?%'}""", RegexOptions.Singleline);

    const string startTag = @"<root xmlns=""htmlPassivePage"" force_trans=""true"" xmlns:meta=""meta"" xmlns:site=""site"" xmlns:asp=""asp"" xmlns:lmlib=""lmlib"" xmlns:lm=""lm"" xmlns:lmc=""lmc"" xmlns:lms=""lms"" xmlns:sz=""sz"">";

    /// <summary>
    /// Prevod lokalizovaneho XML do .TRANS podoby
    /// </summary>
    public static string ToString(XElement root) {
      string content = root.ToString(SaveOptions.DisableFormatting);
      StringBuilder sb = new StringBuilder();
      //CDATA
      foreach (regExItem item in regExItem.Parse(content, rxCData)) {
        if (item.IsMatch)
          sb.Append(item.Value.Substring(9, item.Value.Length - 12));
        else {
          StringBuilder sb2 = new StringBuilder();
          //script primo v tagu:
          string content2 = item.Value;
          foreach (regExItem item2 in regExItem.Parse(content2, rxTagScript)) {
            if (item2.IsMatch) {
              string s = item2.Value.Substring(10, item2.Value.Length - 13);
              int startLen = char.IsDigit(s[1]) ? 6 : 5;
              s = s.Substring(startLen, s.Length - startLen);
              sb2.Append("<%"); sb2.Append(HttpUtility.HtmlDecode(s)); sb2.Append("%>");
            } else
              sb2.Append(item2.Value);
          }
          //script v atributu
          content2 = sb2.ToString();
          sb2.Length = 0;
          foreach (regExItem item2 in regExItem.Parse(content2, rxAttr)) {
            if (item2.IsMatch) {
              string s = item2.Value.Substring(2, item2.Value.Length - 4);
              sb2.Append("<%"); sb2.Append(HttpUtility.HtmlDecode(s)); sb2.Append("%>");
            } else
              sb2.Append(item2.Value);
          }
          //script v atributu, uzavren do '' misto ""
          content2 = sb2.ToString();
          sb2.Length = 0;
          foreach (regExItem item2 in regExItem.Parse(content2, rxAttrEx)) {
            if (item2.IsMatch) {
              string s = item2.Value.Substring(4, item2.Value.Length - 8);
              sb2.Append(@"'<%"); sb2.Append(HttpUtility.HtmlDecode(s)); sb2.Append("%>'");
            } else
              sb2.Append(item2.Value);
          }
          //vysledek
          sb.Append(sb2.ToString());
        }
      }
      content = sb.ToString(); sb.Length = 0;
      foreach (regExItem item in regExItem.Parse(content, rxDecodeScriptCData)) {
        if (item.IsMatch) {
          sb.Append("//<![CDATA["); sb.Append(item.Value.Substring(5, item.Value.Length - 10)); sb.Append("//]]>"); 
        } else
          sb.Append(item.Value);
      }
      if (sb.ToString().IndexOf("<XmlData>") < 0) //kvuli napr q:\LMNet2\WebApps\eduauthornew\EnglishTest\TestEnter.htm.aspx
        sb.Replace(@"xmlns=""htmlPassivePage""", @"xmlns=""http://www.w3.org/1999/xhtml""");
      sb.Replace("$nbsp;", "&nbsp;");
      //Odstraneni <root> tagu
      content = sb.ToString();
      int idx = content.IndexOf('>');
      content = content.Substring(idx + 1);
      idx = content.LastIndexOf('<');
      content = content.Substring(0, idx);
      return content;
    }

    /// <summary>
    /// Operace 1: prevod ASPX do XML
    /// </summary>
    public static XElement ToXml(string content) {
      content = startTag + Parse(content) + "</root>";
      try {
        XElement root = XElement.Parse(content, LoadOptions.PreserveWhitespace);
        //Maji vsechny tagy s lokalizovanymi atributy ID?
        XElement[] wrongEls = TradosLib.transAspxAttributes(root).Select(a => a.Parent).Distinct().Where(el => el.Attribute("ID") == null).ToArray();
        if (wrongEls.Length > 0) {
          StringBuilder sb = new StringBuilder();
          foreach (XElement el in wrongEls) {
            sb.Append(el.ToString()); sb.Append("\r\n");
          }
          content = sb.ToString();
          throw new Exception("Missing ID at localized tag");
        }
        //lmlib:TransBlock tagy
        foreach (XElement el in root.Descendants(TradosLib.lmlib + "TransBlock"))
          TransBlock.setText(el);
        return root;
      } catch (Exception e) {
        throw new Exception(content + "\r\n<!--" + e.Message + "-->");
      }
    }

    /// <summary>
    /// Pomocna funkce pro Operaci 1: ASPX parsing tak, aby byl mozny prevod do XML
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    static string Parse(string content) {
      List<string> fragments = new List<string>();
      StringBuilder sb = new StringBuilder();
      //ScriptCData
      sb.Length = 0;
      foreach (regExItem item in regExItem.Parse(content, rxScriptCData))
        if (item.IsMatch) {
          sb.Append("$${$$"); sb.Append(item.Value.Substring(11, item.Value.Length-16)); sb.Append("$$}$$");
        } else
          sb.Append(item.Value);
      content = sb.ToString();
      //comment, doctype, ItemTemplate, Script, ostatni aspx script
      foreach (Regex ex in new Regex[] { rxMetaComment, rxComment, rxDocTyx, rxScript, rxAspScript }) {
        sb.Length = 0;
        foreach (regExItem item in regExItem.Parse(content, ex))
          if (item.IsMatch) {
            sb.Append("#{#"); sb.Append(fragments.Count); sb.Append("#}#");
            fragments.Add(item.Value);
          } else
            sb.Append(item.Value);
        content = sb.ToString();
      }
      //<%%> v tagu osetri zvlast
      sb.Length = 0;
      foreach (regExItem item in regExItem.Parse(content, rxOpenTag)) {
        if (item.IsMatch)
          sb.Append(processTag(item.Value, fragments));
        else
          sb.Append(item.Value);
      }
      content = sb.ToString();
      //ostatni <%%> (mimo tag) do CDATA
      sb.Length = 0;
      foreach (regExItem item in regExItem.Parse(content, rxCodedAspScript)) {
        if (item.IsMatch)
          sb.Append(encodeAspScript(item.Value, fragments, encodeType.cdata));
        else
          sb.Append(item.Value);
      }
      //&nbsp;
      sb.Replace("&nbsp;", "$nbsp;");
      sb.Replace("http://www.w3.org/1999/xhtml", TradosLib.html.NamespaceName);
      content = sb.ToString();
      //DeEntitize vše mimo CDATA
      sb.Length = 0;
      foreach (regExItem item in regExItem.Parse(content, rxCData)) {
        if (item.IsMatch) sb.Append(item.Value);
        else sb.Append(HtmlToXmlEntity.DeEntitize(item.Value));
      }
      content = sb.ToString();
      return content;
    }

    static string processTag(string content, List<string> fragments) {
      //<%%> v atributu
      StringBuilder sb = new StringBuilder();
      foreach (regExItem item in regExItem.Parse(content, rxTagAttr)) {
        if (item.IsMatch) {
          sb.Append(processAttr(item.Value, fragments));
        } else
          sb.Append(item.Value);
      }
      content = sb.ToString();
      //<%%> v atributu v '' misto v "" uvozovkach
      sb.Length = 0;
      foreach (regExItem item in regExItem.Parse(content, rxTagExAttr)) {
        if (item.IsMatch)
          sb.Append(encodeAspScript(item.Value.Substring(1, item.Value.Length - 2), fragments, encodeType.attrEx));
        else
          sb.Append(item.Value);
      }
      content = sb.ToString();
      //<%%> v tagu mimo atribut
      sb.Length = 0; int cnt = 0;
      foreach (regExItem item in regExItem.Parse(content, rxCodedAspScript)) {
        if (item.IsMatch) {
          sb.Append(" regexattr_"); sb.Append(cnt); sb.Append("=\"");
          sb.Append(encodeAspScript(item.Value, fragments, encodeType.attr)); sb.Append("\" ");
          cnt++;
        } else
          sb.Append(item.Value);
      }
      content = sb.ToString();
      return content;
    }

    static string processAttr(string content, List<string> fragments) {
      StringBuilder sb = new StringBuilder();
      foreach (regExItem item in regExItem.Parse(content, rxCodedAspScript)) {
        if (item.IsMatch)
          sb.Append(encodeAspScript(item.Value, fragments, encodeType.attr));
        else
          sb.Append(item.Value);
      }
      content = sb.ToString();
      return content;
    }

    enum encodeType {
      attr,
      attrEx,
      cdata,
    }

    static string encodeAspScript(string content, List<string> fragments, encodeType type) {
      content = content.Substring(3, content.Length - 6);
      int idx = int.Parse(content);
      string frag = fragments[idx];
      switch (type) {
        case encodeType.cdata: return "<![CDATA[" + frag + "]]>";
        case encodeType.attr: return "{%" + HttpUtility.HtmlEncode(frag.Substring(2, frag.Length - 4)) + "%}";
        case encodeType.attrEx: return "\"{'%" + HttpUtility.HtmlEncode(frag.Substring(2, frag.Length - 4)) + "%'}\"";
        default: throw new Exception("Missing code here!");

      }
    }

    public static void modifyAspxPage(XAttribute attr) {
      if (attr.Name.Namespace == TradosLib.lm || attr.Parent.Attribute(TradosLib.meta + "resourcekey") != null) return;
      attr.Parent.Add(new XAttribute(TradosLib.meta + "resourcekey", (string)attr.Parent.Attribute("ID") + "Res"));
    }
    public static void modifyAspxPage(List<XNode> nodes, string id, string value) {
      nodes[0].AddBeforeSelf(
        new XText(" "),
        new XElement(TradosLib.asp + "Label",
          new XAttribute("runat", "server"),
          new XAttribute(TradosLib.meta + "resourcekey", id.Replace(".Text", null)),
          new XAttribute("Text", value)
        ),
        new XText(" ")
      );
      nodes.Remove<XNode>();
    }
  }

  public static class CSParser {

    static Regex rxCSLocalize = new Regex(@"CSLocalize\.localize\(.*?""\)", RegexOptions.Singleline);

    public static string Parse(string content, GenResxContext ctx, LocFileType fileType, out bool modified) {
      modified = false;
      StringBuilder sb = new StringBuilder();
      foreach (regExItem item in regExItem.Parse(content, rxCSLocalize)) {
        if (item.IsMatch) {
          string val = item.Value.Substring(20, item.Value.Length - 21);
          string[] parts = val.Split(new char[] { ',' }, 3);
          for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();
          //prvni parametr: GUID nebo null
          string id;
          if (parts[0] == "null") {
            id = Guid.NewGuid().ToString().Replace("-", null); parts[0] = id;
            ctx.ids.Add(id, true);
            modified = true;
          } else {
            id = parts[0].Substring(1, 32);
            if (ctx.ids.ContainsKey(id)) {
              id = Guid.NewGuid().ToString().Replace("-", null);
              modified = true;
            }
            ctx.ids.Add(id, true);
          }
          //Druhy parametr: grupa
          if (parts[1] != "LocPageGroup." + ctx.grp.ToString()) {
            parts[1] = "LocPageGroup." + ctx.grp.ToString();
            modified = true;
          }
          //Modifikovane volani procedury
          sb.Append(@"CSLocalize."); //rozdeleno aby se nestalo predmetem lokalizace
          sb.Append(@"localize(""");
          sb.Append(id);
          sb.Append("\", ");
          sb.Append(parts[1]);
          sb.Append(", ");
          sb.Append(parts[2]);
          sb.Append(")");
          //Vystup hodnoty do resource:
          val = parts[2];
          if (val[0] == '@') {
            val = val.Substring(2, val.Length - 3);
            val = val.Replace("\"\"", "\"");
          } else {
            val = val.Substring(1, val.Length - 2);
            val = Regex.Unescape(val);
          }
          ctx.toTrans.Add(new TradosLib.resxNameValue(id, val, (XAttribute)null));
        } else
          sb.Append(item.Value);
      }
      content = sb.ToString();
      return content;
    }
  }

  public static class JSParser {

    static Regex rxJSLocalize = new Regex(@"CSLocalize\(.*?'\)", RegexOptions.Singleline);

    public static string Parse(string content, GenResxContext ctx, out bool modified) {
      modified = false;
      StringBuilder sb = new StringBuilder();
      foreach (regExItem item in regExItem.Parse(content, rxJSLocalize)) {
        if (item.IsMatch) {
          string val = item.Value.Substring(11, item.Value.Length - 12);
          string[] parts = val.Split(new char[] { ',' }, 2);
          for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();
          //prvni parametr: GUID nebo null
          string id;
          if (parts[0] == "id") { //definice funkce CSLocalize
            sb.Append(item.Value);
            continue;
          } else if (parts[0] == "null") {
            id = Guid.NewGuid().ToString().Replace("-", null); parts[0] = id;
            ctx.ids.Add(id, true);
            modified = true;
          } else {
            id = parts[0].Substring(1, 32);
            if (ctx.ids.ContainsKey(id)) {
              id = Guid.NewGuid().ToString().Replace("-", null);
              parts[0] = id;
              modified = true;
            }
            ctx.ids.Add(id, true);
          }
          //Modifikovane volani procedury
          sb.Append(@"CSLocalize('");
          sb.Append(id);
          sb.Append("', ");
          sb.Append(parts[1]);
          sb.Append(")");
          //Vystup hodnoty do resource:
          val = parts[1].Substring(1, parts[1].Length - 2);
          ctx.toTransJS.Add(new TradosLib.resxNameValue(id, val, (XAttribute)null));
        } else
          sb.Append(item.Value);
      }
      content = sb.ToString();
      return content;
    }
  }

}
