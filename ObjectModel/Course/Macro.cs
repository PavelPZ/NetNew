using LMComLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using LMNetLib;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace CourseModel {


  public partial class macroArticle {
    public override IEnumerable<tag> Generate(body pg, LoggerMemory wr) {
      yield return htmlTag.create(htmlTag.h3, new text() { title = cdata });
    }
  }
  public partial class macroVocabulary {
    public override IEnumerable<tag> Generate(body pg, LoggerMemory wr) {
      yield return htmlTag.create(htmlTag.h3, new text() { title = cdata });
    }
  }

  public partial class macroVideo {
    //[XmlAttribute]
    //public string Url;
    //[XmlAttribute]
    //public string Style;

    public override IEnumerable<tag> Generate(body pg, LoggerMemory wr) {
      //if (string.IsNullOrEmpty(urlText) && string.IsNullOrEmpty(urlDialog)) wr.ErrorLine(pg.url, "Missing url-text or url-dialog attribute");
      //if (!string.IsNullOrEmpty(urlText) && !string.IsNullOrEmpty(urlDialog)) wr.ErrorLine(pg.url, "Use only one from url-text or url-dialog attributes");
      //var isDialog = !string.IsNullOrEmpty(urlDialog);
      var cid = "macrovideo_" + cnt++.ToString();
      //var file = isDialog ? (include)new includeDialog { cutUrl = urlDialog} : new includeText { cutUrl = urlText};
      switch (displayStyle) {
        case "WithDialog":
          yield return htmlTag.create(htmlTag.div,
            htmlTag.create(htmlTag.div,
              "class", "col-md-5 video-hidden-xs video-hidden-sm",
              new mediaVideo { classSetter = "video-hidden-xs video-hidden-sm", shareMediaId = cid },
              new mediaPlayer { classSetter = "hidden-xs hidden-sm", shareMediaId = cid }
            ),
            htmlTag.create(htmlTag.div,
              "class", "col-md-7",
              new mediaText { id = cid, /*audioUrl = audioUrl,*/ mediaUrl = mediaUrl, cutUrl = cutUrl }
            )
          );
          break;
        default:
          yield return htmlTag.create(htmlTag.div,
            new mediaVideo() { id = cid, /*audioUrl = audioUrl,*/ mediaUrl = mediaUrl, cutUrl = cutUrl },
            new mediaPlayer() { shareMediaId = cid });
          break;
      }
    }
    static int cnt = 0;
  }

  //public partial class inlineTag {
  //  public override IEnumerable<tag> Generate(body pg, LoggerMemory wr) {
  //    if (Items == null || Items.Length != 1 || Items[0] as text == null) wr.ErrorLine(pg.url, "Missing text");
  //    else yield return smartElement.replaceInlineControl(((text)Items[0]).title, inlineType);
  //  }
  //}

  public partial class macroTrueFalse {
    //[XmlAttribute]
    //public CheckItemTexts TextId;
    public override IEnumerable<tag> Generate(body pg, LoggerMemory wr) {
      var lines = smartElement.splitLines(cdata); if (lines.Length == 0) { wr.ErrorLine(pg.url, "Missing lines with data"); yield break; }
      lines = lines.Select(l => smartElement.removeNums(l).Trim()).ToArray();
      yield return new listGroup() {
        //width = width,
        Items = lines.Select(l => new checkItem() {
          correctValue = l.StartsWith("#"),
          textType = textId,
          Items = smartElement.replaceMacroHardReturn(l.Replace("#", null), wr).ToArray()
        }).ToArray()
      };
    }
  }

  public partial class macroSingleChoices {
    public override IEnumerable<tag> Generate(body pg, LoggerMemory wr) {
      var txt = doubleCrLf.Replace(cdata, "#####");
      var linesSingleChs = txt.Split(new string[] { "#####" }, StringSplitOptions.RemoveEmptyEntries); if (linesSingleChs.Length == 0) { wr.ErrorLine(pg.url, "Missing lines with data"); yield break; }
      var singleChs = linesSingleChs.Select(l => smartElement.splitLines(l).Select(ll => smartElement.removeNums(ll)).ToArray());
      //var cnt = 0;
      var res = new twoColumn() {
        Items = singleChs.Where(l => l.Count() > 0).Select(singleCh => new panel() {
          header = new headerProp { Items = new tag[] { htmlTag.create(htmlTag.h4, smartElement.replaceMacroHardReturn(singleCh[0], wr)) } },
          //id = cnt++<0 ? "": null, //noop majici sideefekt - zvetseni cnt
          Items = new tag[] { new singleChoice {
            Items = singleCh.Skip(1).Select(t => new radioButton{correctValue = t.StartsWith("#"), Items = new tag[] { new text{title = t.TrimStart('#')}}}).ToArray()},
          }
        }).ToArray()
      };
      yield return res;
    }
    static Regex doubleCrLf = new Regex(@"(\r\n){2,}|\r{2,}|\n{2,}", RegexOptions.Singleline);
  }

  public partial class macroListWordOrdering {
    public override IEnumerable<tag> Generate(body pg, LoggerMemory wr) {
      var lines = smartElement.splitLines(cdata); if (lines.Length < 2) { wr.ErrorLine(pg.url, "At least 2 lines with data required"); yield break; }
      yield return new listGroup() {
        isStriped = true,
        //width = width,
        Items = doubles(lines).Select(db => new node() {
          Items = new tag[] {
            new text() { title = smartElement.splitEx(db.Item1).Select(p => p.Trim()).Aggregate((r, it) => r + " / " + it) },
            htmlTag.create(htmlTag.br),
            new gapFill() { correctValue = db.Item2.Trim() }
          }
        }).ToArray()
      };
    }
    public IEnumerable<Tuple<string, string>> doubles(string[] lines) {
      for (int i = 0; i < lines.Length - 1; i += 2) yield return new Tuple<string, string>(lines[i], lines[i + 1]);
    }
  }

  public partial class macroPairing {
    public override IEnumerable<tag> Generate(body pg, LoggerMemory wr) {
      var lines = smartElement.splitLines(cdata); if (lines.Length == 0) { wr.ErrorLine(pg.url, "Missing lines with data"); yield break; }
      List<pairingItem> items = new List<pairingItem>();
      foreach (var l in lines) {
        var parts = l.Split('\\').Select(w => w.Trim()).ToArray(); if (parts.Length != 2) { wr.ErrorLine(pg.url, "Two part of pairing data required"); continue; };
        items.Add(new pairingItem() {
          right = smartElement.removeNums(parts[1]),
          Items = smartElement.replaceMacroHardReturn(smartElement.removeNums(parts[0]), wr).ToArray()
        });
      }
      yield return new pairing() { Items = items.ToArray()/*, width = width*/ };
    }
  }

  public partial class macroTable {
    public override IEnumerable<tag> Generate(body pg, LoggerMemory wr) {
      var lines = smartElement.splitLines(cdata); if (lines.Length == 0) { wr.ErrorLine(pg.url, "Missing lines with data"); yield break; }
      string tableStyleSheet;
      tag pref = prefix(this, inlineType, ref lines, out tableStyleSheet);
      if (pref != null) yield return pref;
      var cells = lines.Select(l => l.Split('\\').Select(w => w.Trim()).ToArray()).ToArray();
      if (cells.GroupBy(c => c.Length).Count() > 1) { wr.ErrorLine(pg.url, "The equal number of cells in every row required"); yield break; }
      var res = htmlTag.create(htmlTag.table,
        "class", "table table-bordered oli-table",
        //width = width,
        cells.Select((cls, rowIdx) => htmlTag.create(htmlTag.tr,
          cls.Select((c, colIdx) => createCell(c, wr, inlineType, rowIdx, colIdx))
        ))
      );
      res.styleSheet = tableStyleSheet;
      //smartElement.adjustWidths(res2, inlineType, wr);
      yield return res;
    }
    public static tag prefix(macroTemplate self, inlineControlTypes type, ref string[] lines, out string tableStyleSheet) {
      tableStyleSheet = null;
      switch (type) {
        case inlineControlTypes.DragTarget:
          return new offering() { /*width = self.width*/ };
        case inlineControlTypes.GapFill_Correction:
        case inlineControlTypes.GapFill:
          if (!lines[0].StartsWith(gridPrefix)) return null;
          var parts = smartElement.splitEx(lines[0].Substring(1));
          lines = lines.Skip(gridPrefix.Length).ToArray();
          string id = string.Format("mtgid{0}",macroTableGridId++ % 50);
          tableStyleSheet = "gap-fill {offering-id:" + id + "}";
          return new offering() { id = id, words = parts.DefaultIfEmpty().Aggregate((r, i) => r + "|" + i) };
      }
      return null;
    }
    const string gridPrefix = "#";
    static int macroTableGridId = 0;

    static htmlTag createCell(string ct, LoggerMemory wr, inlineControlTypes type, int rowIdx, int colIdx) {
      var match = cellFmt.Match(ct);
      string cls = null;
      if (match.Success) {
        var align = match.Groups["align"].Value;
        var color = match.Groups["color"].Value;
        ct = ct.Substring(match.Length);
        if (!string.IsNullOrEmpty(color)) {
          switch (color[0]) {
            case 'y': cls += rowIdx == 0 ? " oli-table-top-rounded oli-table-default" : (colIdx == 0 ? " oli-table-left-rounded oli-table-default" : " oli-table-default"); break;
            case 'b': cls += " oli-table-del"; break;
            case 'g': cls += " oli-table-default"; break;
          }
        }
        if (!string.IsNullOrEmpty(align)) {
          switch (align[0]) {
            case 'l': cls += " left"; break;
            case 'c': cls += " center"; break;
            case 'r': cls += " right"; break;
          }
          switch (align[1]) {
            case 't': cls += " top"; break;
            case 'm': cls += " middle"; break;
            case 'b': cls += " bottom"; break;
          }
        }
      }
      return htmlTag.create(htmlTag.td,
        "class", cls,
        smartTag.replaceMacroHardReturn(ct, wr, type)
      );
    }
    static Regex cellFmt = new Regex(@"^(#(?<align>(lt|lm|lb|ct|cm|cb|rt|rm|rb)?)(?<color>(y|b|g)?))", RegexOptions.Singleline);
  }

  public partial class macroList {
    public override IEnumerable<tag> Generate(body pg, LoggerMemory wr) {
      if (cdata == null) yield break; 
      var lines = smartElement.splitLines(cdata); if (lines.Length == 0) yield break;
      string tableStyleSheet;
      tag pref = macroTable.prefix(this, inlineType, ref lines, out tableStyleSheet);
      if (pref != null) yield return pref;
      var res = new listGroup() {
        //width = width,
        Items = lines.Select(l => smartElement.removeNums(l).Trim()).Select(l => new node() {
          Items = smartTag.replaceMacroHardReturn(l, wr, inlineType).ToArray()
        }).ToArray()
      };
      //smartElement.adjustWidths(res2, inlineType, wr);
      yield return res;
    }
  }

  public partial class macroIconList {
    public override IEnumerable<tag> Generate(body pg, LoggerMemory wr) {
      var lines = smartElement.splitLines(cdata); if (lines.Length == 0) return Enumerable.Empty<tag>();
      list res = new list() {
        //width = width,
        delim = delim,
        isStriped = isStriped,
        icon = icon,
        color = color,
        Items = lines.Select(l => smartElement.removeNums(l).Trim()).Select(l => htmlTag.create(htmlTag.li, smartTag.formatedText(l))).ToArray()
      };
      return (res as IMacroTemplate).Generate(pg, wr);
    }
  }

}





