using LMNetLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
namespace _rn {
  public static class tool {
    public static void rename() {
      var files = File.ReadAllLines(@"d:\LMCom\rew\Web4\renamed\allxml.txt");
      Directory.Delete(@"d:\LMCom\rew\Web4\renamed", true);
      Directory.CreateDirectory(@"d:\LMCom\rew\Web4\renamed");
      File.WriteAllLines(@"d:\LMCom\rew\Web4\renamed\allxml.txt", files);
      List<string> wrongXml = new List<string>();
      Parallel.ForEach(files, fn => {
      //foreach (var fn in files) {
        if (!File.Exists(fn)) return;
        try {
          var destFn = fn.Replace(@"d:\lmcom\rew\web4\", @"d:\lmcom\rew\web4\renamed\").Replace(@"d:\lmcom\rew\eduauthornew\", @"d:\lmcom\rew\web4\renamed\eduauthornew\");
          LowUtils.AdjustFileDir(destFn);
          var root = XElement.Load(fn, LoadOptions.PreserveWhitespace);
          rename(root);
          root.Save(destFn, SaveOptions.DisableFormatting);
          //File.Copy(fn, destFn);
        } catch {
          lock (wrongXml) wrongXml.Add(fn);
        }
      });
      //}
      File.WriteAllLines(@"d:\temp\wrongXml.txt", wrongXml);
      System.Windows.Forms.MessageBox.Show(renameCount.ToString());
    }

    static void rename(XElement root) {
      string newName; Dictionary<string, string> newProps; string newProp;
      foreach (var el in root.DescendantsAndSelf().ToArray()) {
        //rename enum values
        replaceValue(el);
        renameProps.TryGetValue(el.Name.LocalName, out newProps);
        if (renameEl.TryGetValue(el.Name.LocalName, out newName)) {
          renameCount++;
          //rename element
          el.Name = newName;
        }
        if (newProps != null)
          foreach (var att in el.Attributes().ToArray()) {
            if (newProps.TryGetValue(att.Name.LocalName, out newProp)) {
              renameCount++;
              //rename attr
              el.Add(new XAttribute(newProp, att.Value));
              att.Remove();
            }
          }
      }
    }

    static void replaceValue(XElement el) {
      if (el.Name.LocalName == "check-box" || el.Name.LocalName == "check-item" || el.Name.LocalName == "macro-true-false") {
        var attr = el.Attribute("text-id");
        if (attr != null) {
          switch (attr.Value) {
            case "YesNo": attr.Value = "yes-no"; break;
            case "TrueFalse": attr.Value = "true-false"; break;
            case "no": attr.Value = "no"; break;
            default: throw new NotImplementedException();
          }
        }
        attr = el.Attribute("init-value");
        if (attr != null) {
          switch (attr.Value) {
            case "True": attr.Value = "true"; break;
            case "False": attr.Value = "false"; break;
            case "no": attr.Value = "no"; break;
            default: throw new NotImplementedException();
          }
        };
      } else if (el.Name.LocalName == "offering") {
        var attr = el.Attribute("drop-down-mode");
        if (attr != null) {
          switch (attr.Value) {
            case "discard": attr.Value = "drop-down-discard"; break;
            case "keep": attr.Value = "drop-down-keep"; break;
            case "passive": attr.Value = "gap-fill-ignore"; break;
            default: throw new NotImplementedException();
          }
        }
      }
    }

    static tool() {
      //{\"old\":\"recording\",\"new\":\"audio-capture\",\"props\":
      //{\"old\":\"check-item\",\"props\":[{\"old\":\"correct-value\"},{\"old\":\"eval-button-id\",\"new\":\"eval-btn-id\"}
      renameEl = renameData.Where(on => on.@new != null).ToDictionary(on => on.@new, on => on.old);
      renameProps = renameData.Where(on => on.props.Any(p => p.@new != null)).ToDictionary(on => on.@new == null ? on.old : on.@new, on => on.props.Where(p => p.@new != null).ToDictionary(p => p.@new, p => p.old));
    }
    static rnEl[] renameData = Newtonsoft.Json.JsonConvert.DeserializeObject<rnEl[]>("    [{\"old\":\"recording\",\"new\":\"audio-capture\",\"props\":[{\"old\":\"single-attempt\",\"new\":\"disable-re-record\"},{\"old\":\"eval-button-id\",\"new\":\"eval-btn-id\"},{\"old\":\"eval-group\"},{\"old\":\"id\"},{\"old\":\"dialog-header-id\",\"new\":\"modal-dialog-header\"},{\"old\":\"dialog-size\",\"new\":\"modal-dialog-size\"},{\"old\":\"limit-recommend\",\"new\":\"recommend-seconds-from\"},{\"old\":\"score-weight\"},{\"old\":\"limit-min\",\"new\":\"speak-seconds-from\"},{\"old\":\"limit-max\",\"new\":\"speak-seconds-to\"},{\"old\":\"record-in-dialog\",\"new\":\"stop-in-modal-dialog\"}]},{\"old\":\"body\",\"props\":[{\"old\":\"see-also-links\",\"new\":\"course-see-also-str\"},{\"old\":\"id\"},{\"old\":\"instr-title\"},{\"old\":\"instr-body\",\"new\":\"instrs-str\"},{\"old\":\"order\"}]},{\"old\":\"check-box\",\"props\":[{\"old\":\"correct-value\"},{\"old\":\"eval-button-id\",\"new\":\"eval-btn-id\"},{\"old\":\"eval-group\"},{\"old\":\"id\"},{\"old\":\"init-value\"},{\"old\":\"read-only\"},{\"old\":\"score-weight\"},{\"old\":\"skip-evaluation\"},{\"old\":\"text-type\",\"new\":\"text-id\"}]},{\"old\":\"check-item\",\"props\":[{\"old\":\"correct-value\"},{\"old\":\"eval-button-id\",\"new\":\"eval-btn-id\"},{\"old\":\"eval-group\"},{\"old\":\"id\"},{\"old\":\"init-value\"},{\"old\":\"read-only\"},{\"old\":\"score-weight\"},{\"old\":\"skip-evaluation\"},{\"old\":\"text-type\",\"new\":\"text-id\"}]},{\"old\":\"cut-dialog\",\"props\":[{\"old\":\"id\"},{\"old\":\"media-url\"}]},{\"old\":\"cut-text\",\"props\":[{\"old\":\"id\"},{\"old\":\"media-url\"}]},{\"old\":\"drop-down\",\"props\":[{\"old\":\"case-sensitive\"},{\"old\":\"correct-value\"},{\"old\":\"eval-button-id\",\"new\":\"eval-btn-id\"},{\"old\":\"eval-group\"},{\"old\":\"id\"},{\"old\":\"offering-id\"},{\"old\":\"score-weight\"},{\"old\":\"width-group\",\"new\":\"smart-width\"},{\"old\":\"width\"}]},{\"old\":\"eval-button\",\"new\":\"eval-btn\",\"props\":[{\"old\":\"eval-button-id\",\"new\":\"eval-btn-id\"},{\"old\":\"eval-group\"},{\"old\":\"id\"},{\"old\":\"score-as-ratio\",\"new\":\"ratio-score\"},{\"old\":\"score-weight\"}]},{\"old\":\"gap-fill\",\"props\":[{\"old\":\"case-sensitive\"},{\"old\":\"correct-value\"},{\"old\":\"eval-button-id\",\"new\":\"eval-btn-id\"},{\"old\":\"eval-group\"},{\"old\":\"id\"},{\"old\":\"init-value\"},{\"old\":\"offering-id\"},{\"old\":\"hint\",\"new\":\"place-holder\"},{\"old\":\"read-only\"},{\"old\":\"score-weight\"},{\"old\":\"skip-evaluation\"},{\"old\":\"width-group\",\"new\":\"smart-width\"},{\"old\":\"width\"}]},{\"old\":\"include-dialog\",\"props\":[{\"old\":\"cut-url\"},{\"old\":\"id\"}]},{\"old\":\"include-text\",\"props\":[{\"old\":\"cut-url\"},{\"old\":\"id\"}]},{\"old\":\"media-big-mark\",\"props\":[{\"old\":\"cut-url\"},{\"old\":\"id\"},{\"old\":\"media-url\"},{\"old\":\"share-media-id\",\"new\":\"share-id\"},{\"old\":\"subset\"}]},{\"old\":\"media-player\",\"props\":[{\"old\":\"cut-url\"},{\"old\":\"id\"},{\"old\":\"media-url\"},{\"old\":\"share-media-id\",\"new\":\"share-id\"},{\"old\":\"subset\"}]},{\"old\":\"media-text\",\"props\":[{\"old\":\"continue-media-id\",\"new\":\"continue-id\"},{\"old\":\"cut-url\"},{\"old\":\"id\"},{\"old\":\"hidden\",\"new\":\"is-hidden\"},{\"old\":\"passive\",\"new\":\"is-passive\"},{\"old\":\"media-url\"},{\"old\":\"share-media-id\",\"new\":\"share-id\"},{\"old\":\"subset\"}]},{\"old\":\"media-video\",\"props\":[{\"old\":\"cut-url\"},{\"old\":\"id\"},{\"old\":\"media-url\"},{\"old\":\"share-media-id\",\"new\":\"share-id\"},{\"old\":\"subset\"}]},{\"old\":\"offering\",\"props\":[{\"old\":\"mode\",\"new\":\"drop-down-mode\"},{\"old\":\"id\"},{\"old\":\"hidden\",\"new\":\"is-hidden\"},{\"old\":\"words\"}]},{\"old\":\"pairing\",\"props\":[{\"old\":\"eval-button-id\",\"new\":\"eval-btn-id\"},{\"old\":\"eval-group\"},{\"old\":\"id\"},{\"old\":\"left-width\"},{\"old\":\"left-random\",\"new\":\"random\"},{\"old\":\"score-weight\"}]},{\"old\":\"pairing-item\",\"props\":[{\"old\":\"id\"},{\"old\":\"right\"}]},{\"old\":\"radio-button\",\"props\":[{\"old\":\"correct-value\"},{\"old\":\"eval-button-id\",\"new\":\"eval-btn-id\"},{\"old\":\"eval-group\"},{\"old\":\"id\"},{\"old\":\"init-value\"},{\"old\":\"read-only\"},{\"old\":\"score-weight\"},{\"old\":\"skip-evaluation\"}]},{\"old\":\"replica\",\"props\":[{\"old\":\"actor-name\",\"new\":\"actor\"},{\"old\":\"actor-id\",\"new\":\"icon-id\"},{\"old\":\"id\"},{\"old\":\"use-phrases\",\"new\":\"sent-take\"}]},{\"old\":\"phrase\",\"new\":\"sent\",\"props\":[{\"old\":\"beg-pos\"},{\"old\":\"end-pos\"},{\"old\":\"id\"}]},{\"old\":\"phrase-replace\",\"new\":\"sent-replace\",\"props\":[{\"old\":\"id\"},{\"old\":\"replica-phrase-idx\",\"new\":\"replica-sent-idx\"},{\"old\":\"phrase-idx\",\"new\":\"sent-idx\"}]},{\"old\":\"sentence-ordering-item\",\"new\":\"sentence\",\"props\":[{\"old\":\"id\"}]},{\"old\":\"sentence-ordering\",\"props\":[{\"old\":\"eval-button-id\",\"new\":\"eval-btn-id\"},{\"old\":\"eval-group\"},{\"old\":\"id\"},{\"old\":\"score-weight\"}]},{\"old\":\"single-choice\",\"props\":[{\"old\":\"eval-button-id\",\"new\":\"eval-btn-id\"},{\"old\":\"id\"},{\"old\":\"read-only\"},{\"old\":\"score-weight\"},{\"old\":\"skip-evaluation\"}]},{\"old\":\"word-multi-selection\",\"props\":[{\"old\":\"eval-button-id\",\"new\":\"eval-btn-id\"},{\"old\":\"eval-group\"},{\"old\":\"id\"},{\"old\":\"score-weight\"},{\"old\":\"words\"}]},{\"old\":\"word-ordering\",\"props\":[{\"old\":\"correct-order\",\"new\":\"correct-value\"},{\"old\":\"eval-button-id\",\"new\":\"eval-btn-id\"},{\"old\":\"eval-group\"},{\"old\":\"id\"},{\"old\":\"score-weight\"}]},{\"old\":\"word-selection\",\"props\":[{\"old\":\"eval-button-id\",\"new\":\"eval-btn-id\"},{\"old\":\"eval-group\"},{\"old\":\"id\"},{\"old\":\"score-weight\"},{\"old\":\"words\"}]},{\"old\":\"writing\",\"props\":[{\"old\":\"eval-button-id\",\"new\":\"eval-btn-id\"},{\"old\":\"eval-group\"},{\"old\":\"id\"},{\"old\":\"number-of-rows\",\"new\":\"init-rows\"},{\"old\":\"limit-recommend\",\"new\":\"recommend-words-min\"},{\"old\":\"score-weight\"},{\"old\":\"limit-max\",\"new\":\"words-max\"},{\"old\":\"limit-min\",\"new\":\"words-min\"}]}]");
    static Dictionary<string, string> renameEl;
    static Dictionary<string, Dictionary<string, string>> renameProps;
    static int renameCount;
  }

  public class rnProp {
    public string old;
    public string @new;
  }
  public class rnEl : rnProp {
    public rnProp[] props;
  }

}