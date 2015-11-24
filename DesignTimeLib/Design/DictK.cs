using LMComLib;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LMNetLib;
using System.Xml.Serialization;
using System.Text;
using System.Xml;
using System.Globalization;
using System.Collections;

namespace DictK {
  public static class Lib {
    public static void HtmlToXml() {
      foreach (var fn in Directory.EnumerateFiles(@"d:\LMCom\rew\Web4\RwDicts\Sources\KDictionaries.back", "words_*.xml")) {
        var dictXml = XElement.Load(fn).Element("body");
        //do Xml bez MDictionaryEntry tagu jej vloz
        foreach (var el in dictXml.Elements().Where(en => !en.DescendantsAttr("class", "MDictionaryEntry").Any()).Select(en => en.DescendantsAttr("class", "MHeadword").Select(h => h.Parents(false).Where(p => p.AttributeValue("align") == "left").First()).First()))
          el.Add(new XAttribute("class", "MDictionaryEntry"));
        var res = dictXml.Elements().Select(e => new MEntry(e.DescendantsAttr("class", "MDictionaryEntry"))).ToArray();
        XmlUtils.ObjectToFile(fn.Replace("words_", "tree_words_"), res);
      }
    }
    public static IEnumerable<MEntry> readDict() {
      return Directory.EnumerateFiles(@"d:\LMCom\rew\Web4\RwDicts\Sources\KDictionaries.back", "tree_words_*.xml").SelectMany(fn => XmlUtils.FileToObject<MEntry[]>(fn));
    }
  }

  public class lex {
    public string cls;
    public XElement content;
    public XElement[] others;
  }

  public class parseCtx {
    public parseCtx(IEnumerable<XElement> enumEls) {
      lex lastSym; symbols.Enqueue(lastSym = new lex()); int actLxIdx = 0; XElement[] els = enumEls.ToArray();

      Action<int> finishLex = i => lastSym.others = els.Skip(actLxIdx).Take(i - actLxIdx).Where(el => el.Name.LocalName != "img" && el.Name.LocalName != "br").ToArray();

      for (int i = 0; i < els.Length; i++) {
        var el = els[i]; var cls = el.AttributeValue("class");
        if (isTag.Contains(cls)) {
          finishLex(i);
          symbols.Enqueue(lastSym = new lex { cls = cls, content = el });
          actLxIdx = i + 1;
        }
      }
      finishLex(els.Length);
    }
    static HashSet<string> isTag = new HashSet<string>(new string[] { "MHeadword", "MPronunciation", "MDefinition", "MPartOfSpeech", "MSenseNumber", "MExample", "MTranslation", "MCompare" });
    public Queue<lex> symbols = new Queue<lex>();
  }

  public class MItem {
    public MItem() { }
    public MItem(parseCtx ctx) {
      var lex = ctx.symbols.Peek(); ctx.symbols.Dequeue();
      if (!(this is MDictionaryEntry)) processContent(lex);
      others = lex.others.Length == 0 ? null : lex.others;
    }
    [XmlAttribute]
    public string content;
    public XElement xmlContent;
    public XElement[] others;
    public virtual IEnumerable<MItem> childs() { yield break; }
    public virtual void processContent(lex lx) {
      foreach (var e in lx.content.Elements().ToArray()) if (e.Name.LocalName == "br") e.Remove();
      if (lx.content.HasElements) xmlContent = lx.content; else content = LowUtils.crlfSpaces(lx.content.Value);
    }
    public IEnumerable<MItem> descendants() { return XExtension.Create(this).Concat(childs().SelectMany(ch => ch.descendants())); }
  }

  public class MEntry {
    public MEntry() { }
    public MEntry(IEnumerable<XElement> els) {
      dictEntries = els.Select(el => new MDictionaryEntry(el.Elements())).ToList();
    }
    public List<MDictionaryEntry> dictEntries = new List<MDictionaryEntry>();
  }

  public class MDictionaryEntry : MItem {
    public List<MHeadword> headwords = new List<MHeadword>();

    public MDictionaryEntry() : base() { }
    public MDictionaryEntry(IEnumerable<XElement> els) {
      var ctx = new parseCtx(els);
      others = ctx.symbols.Peek().others; if (others.Length == 0) others = null; ctx.symbols.Dequeue(); //prvni symbol obsahuje prefix
      while (ctx.symbols.Count > 0) {
        var actData = ctx.symbols.Peek().cls;
        switch (actData) {
          case "MHeadword": headwords.Add(new MHeadword(ctx)); break;
          default: throw new Exception();
        }
      }
    }
    public override IEnumerable<MItem> childs() { return headwords; }
  }

  public class MHeadword : MItem {
    public MHeadword() : base() { }
    public MHeadword(parseCtx ctx)
      : base(ctx) {
      while (ctx.symbols.Count > 0) {
        var actData = ctx.symbols.Peek();
        switch (actData.cls) {
          case "MPronunciation": items.Add(new MPronunciation(ctx)); break;
          case "MCompare": items.Add(new MCompare(ctx)); break;
          case "MDefinition": items.Add(new MDefinition(ctx)); break;
          case "MSenseNumber": items.Add(new MSenseNumber(ctx)); break;
          case "MPartOfSpeech": items.Add(new MPartOfSpeech(ctx)); break;
          default: return;
        }
      }
    }
    [XmlArrayItem("MPartOfSpeech", typeof(MPartOfSpeech))]
    [XmlArrayItem("MSenseNumber", typeof(MSenseNumber))]
    [XmlArrayItem("MDefinition", typeof(MDefinition))]
    [XmlArrayItem("MCompare", typeof(MCompare))]
    [XmlArrayItem("MPronunciation", typeof(MPronunciation))]
    public List<MItem> items = new List<MItem>();
    public override IEnumerable<MItem> childs() { return items; }
  }

  public class MCompare : MItem {
    public MCompare() : base() { }
    public MCompare(parseCtx ctx) : base(ctx) { }
  }

  public class MExample : MItem {
    public MExample() : base() { }
    public MExample(parseCtx ctx) : base(ctx) { }
  }

  public class MTranslation : MItem {
    [XmlAttribute]
    public Langs lang;
    public MTranslation() : base() { }
    public MTranslation(parseCtx ctx) : base(ctx) { }
    public override void processContent(lex lx) {
      foreach (var e in lx.content.Elements().ToArray()) {
        if (e.AttributeValue("class") == "MLang") {
          LineIds line;
          switch (e.Value) {
            case "Chinese Simplified": line = LineIds.Chinese; break;
            case "Chinese Traditional": line = LineIds.Chinese_Mandarin; break;
            case "Portuguese Brazil": line = LineIds.Portuguese_Brazilian; break;
            case "Portuguese Portugal": line = LineIds.Portuguese; break;
            default: line = LowUtils.EnumParse<LineIds>(e.Value); break;
          }
          lang = CommonLib.LineIdToLang(line);
        }
        e.Remove();
      }
      content = LowUtils.crlfSpaces(lx.content.Value);
    }
  }

  public class MPronunciation : MItem {
    public MPronunciation() : base() { }
    public MPronunciation(parseCtx ctx) : base(ctx) { }
  }

  public class MDefinition : MItem {
    public MDefinition() : base() { }
    public MDefinition(parseCtx ctx)
      : base(ctx) {
      while (ctx.symbols.Count > 0) {
        var actData = ctx.symbols.Peek();
        switch (actData.cls) {
          case "MExample": if (translations.Count > 0) throw new Exception(); examples.Add(new MExample(ctx)); break;
          case "MTranslation": translations.Add(new MTranslation(ctx)); break;
          default: return;
        }
      }
    }
    public List<MExample> examples = new List<MExample>();
    public List<MTranslation> translations = new List<MTranslation>();
    public override IEnumerable<MItem> childs() { return examples; return translations; }
  }

  public class MPartOfSpeech : MItem {
    public MPartOfSpeech() : base() { }
    public MPartOfSpeech(parseCtx ctx)
      : base(ctx) {
      while (ctx.symbols.Count > 0) {
        var actData = ctx.symbols.Peek();
        switch (actData.cls) {
          case "MDefinition": items.Add(new MDefinition(ctx)); break;
          case "MSenseNumber": items.Add(new MSenseNumber(ctx)); break;
          default: return;
        }
      }
    }
    [XmlArrayItem("MSenseNumber", typeof(MSenseNumber))]
    [XmlArrayItem("MDefinition", typeof(MDefinition))]
    public List<MItem> items = new List<MItem>();
    public override IEnumerable<MItem> childs() { return items; }
  }

  public class MSenseNumber : MItem {
    public MSenseNumber() : base() { }
    public MSenseNumber(parseCtx ctx)
      : base(ctx) {
      while (ctx.symbols.Count > 0) {
        var actData = ctx.symbols.Peek();
        switch (actData.cls) {
          case "MDefinition": items.Add(new MDefinition(ctx)); break;
          default: return;
        }
      }
    }
    public List<MDefinition> items = new List<MDefinition>();
    public override IEnumerable<MItem> childs() { return items; }
  }
}