using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using LMComLib;
using System.Reflection;
using System.Xml.Linq;
using System.ComponentModel;

namespace Course {

  public struct Score {
    public int Correct; public int From;
    public bool NeedsHumanEval;
    public Score(int correct, int from) { Correct = correct; From = from; NeedsHumanEval = false; }
    public Score(bool needsHumanEval) { Correct = 0; From = 0; NeedsHumanEval = needsHumanEval; }
    public int ToPercent() { return From == 0 ? 0 : (int)(100.0 * Correct / From); }
    public override string ToString() {
      return
#if SILVERLIGHT && !eTestMeManager && !SLLIB
 NeedsHumanEval ? (string)Client.App.Current.Resources["_TestModel.WaitingForEvaluation"] :
#endif
        //string.Format("{0}% ({1}/{2})", ToPercent(), Correct, From);
 string.Format("{0}%", ToPercent(), Correct, From);
    }

#if SILVERLIGHT && !eTestMeManager && !SLLIB
    public static string WaitingText() {
      return (string)Client.App.Current.Resources["_TestModel.WaitingForEvaluation"];
    }
#endif
  }

  public partial class CoursePage {
    public string InstructionPtr;
    public Headered Instruction;
    public CoursePageStyles? Style { get; set; }
    [Flags]
    public enum CoursePageStyles {
      HAlignLeft = 0x1, HAlignRight = 0x2, HAlignStretch = 0x4, HAlignCenter = 0x8, WidthSmall = 0x10, WidthNormal = 0x20, WidthLarge = 0x40,
      Normal = 0x80, Large = 0x100, ExtraLarge = 0x200, Small = 0x400, WeightNormal = 0x800, WeightBold = 0x1000
    }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class Words : Exercise, IExpandable {
    public string Title;
    IEnumerable<Exercise> IExpandable.Expand() {
      return string.IsNullOrEmpty(Title) ? Enumerable.Empty<Exercise>() : Title.Split(new char[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Select(s => new Text() { Title = s }).Cast<Exercise>();
    }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class List : Exercise {
    public Exercise[] Items;
    public override IEnumerable<Exercise> SubExercises { get { return getSubExcs(Items); } }
    public override void Expand() { Items = expandHelper(Items); }
    public override object getTemplate() { return ActTemplate; }
    [DefaultValue(null)]
    public ListTemplates? ActTemplate;
    public enum ListTemplates { no, Paragraph }
    public ListStyles? Style { get; set; }
    [Flags]
    public enum ListStyles { MarginRight = 0x1, MarginRightLarge = 0x2, MarginBottom = 0x4, MarginBottomLarge = 0x8 }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class EvalList : MultiExercise {
    public Exercise[] Items { get; set; }
    public override IEnumerable<Exercise> SubExercises { get { return getSubExcs(Items); } }
    public override void Expand() { Items = expandHelper(Items); }
    public override object getTemplate() { return ActTemplate; }
    [DefaultValue(null)]
    public List.ListTemplates? ActTemplate;
    public List.ListStyles? Style { get; set; }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class Headered : Exercise {
    public Exercise Header { get; set; }
    public Exercise Body { get; set; }
    public override IEnumerable<Exercise> SubExercises { get { if (Header != null) yield return Header; if (Body != null) yield return Body; } }
    public override void Expand() { Header = expandHelper(Header); Body = expandHelper(Body); }
    public HeaderedStyles? Style { get; set; }
    [Flags]
    public enum HeaderedStyles { IndentLeft = 0x1 }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class Text : Exercise {
    public string Title { get; set; }
    public TextStyles? Style { get; set; }
    [Flags]
    public enum TextStyles { Normal = 0x1, Large = 0x2, ExtraLarge = 0x4, Small = 0x8, WeightNormal = 0x10, WeightBold = 0x20, }
    public TextStyles? DefaultStyle { get { return TextStyles.Normal; } }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class HtmlText : Exercise {
    public XElement Title { get; set; }
    public Text.TextStyles? Style { get; set; }
    public Text.TextStyles? DefaultStyle { get { return Text.TextStyles.Normal; } }
  }

  public enum PlayType {
    web,
    isolatedStorage,
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class Sound : Exercise {
    public string Url { get; set; }
    public PlayType Type { get; set; }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class CheckItem : EvalExercise {
    public Exercise Title { get; set; } //Text nebo HtmlText apod.
    public bool? InitValue; //inicialni hodnota
    public bool CorrectValue; //spravna odpoved
    public override object getTemplate() { return ActTemplate; }
    [DefaultValue(null)]
    public CheckItemTemplates? ActTemplate;
    public enum CheckItemTemplates { TrueFalse, YesNo }
    public override IEnumerable<Exercise> SubExercises { get { if (Title != null) yield return Title; } }
    public override void Expand() { Title = expandHelper(Title); }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class CheckItemResult : Result { }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class GapFill : EvalExercise {
    public string InitValue; //inicialni hodnota
    public string[] CorrectValue { get; set; }

    string normalize(string value) {
      int st = 0; string res = null;
      foreach (char chh in value) {
        char ch = char.ToLower(chh);
        switch (st) {
          case 0: if (char.IsWhiteSpace(ch)) continue; res += convertChar(ch); st = 1; break; //jsem v WhiteSpace stavu
          case 1: if (char.IsWhiteSpace(ch)) { st = 2; continue; } res += convertChar(ch); break; //jsem v text tvaru
          case 2: if (char.IsLetterOrDigit(res[res.Length - 1]) && char.IsLetterOrDigit(ch)) res += " "; res += convertChar(ch); st = 1; break; //prvni mezera po textu
        }
      }
      return res;
    }
    const string appostrofs = "'`´“\"‘’‛“”‟′″";
    char convertChar(char ch) {
      if (appostrofs.IndexOf(ch) >= 0) return '\"';
      return ch;
    }
    public Text.TextStyles? Style { get; set; }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class GapFillResult : Result { }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public class PairingItem {
    public Exercise Left { get; set; }
    public Text Right { get; set; }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class Pairing : EvalExercise {
    [DefaultValue(false)]
    public bool CombineResultOr = false;
    public PairingItem[] Items { get; set; }
    public override IEnumerable<Exercise> SubExercises {
      get {
        if (Items == null) yield break;
        foreach (PairingItem it in Items) {
          if (it.Left != null) yield return it.Left; if (it.Right != null) yield return it.Right;
        }
      }
    }
    public PairingResult myResult { get { return (PairingResult)MyResult; } }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class PairingResult : Result { }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class SingleChoice : EvalExercise {
    public Exercise[] Items { get; set; }
    public int? InitValue;
    public int CorrectValue;
    public override IEnumerable<Exercise> SubExercises { get { return getSubExcs(Items); } }
    public override void Expand() { Items = expandHelper(Items); }
    public override object getTemplate() { return ActTemplate; }
    [DefaultValue(null)]
    public SingleChoiceTemplates? ActTemplate;
    public enum SingleChoiceTemplates { WordSelection }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class SingleChoiceResult : Result { }

  //[XmlRoot(Namespace = CommonLib.OLIUrl)]
  //public class Mixed : EvalList { }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public class DummyResult : Result { }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class Writing : NeedsEvalExercise {
    public Exercise Title { get; set; }
    public int WordsMin { get; set; }
    public int WordsMax { get; set; }
  }
  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class WritingResult : NeedsEvalResult { }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class Speaking : NeedsEvalExercise {
    public string SoundUrl;
    public Exercise Title { get; set; }
    public XElement[] Paragraphs { get; set; }
    public int SpeakSecondsFrom { get; set; }
    public int SpeakSecondsTo { get; set; }
    public int PrepareSecondsFrom { get; set; }
    public int PrepareSecondsTo { get; set; }
  }
  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class SpeakingResult : NeedsEvalResult { }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class MultiExercise : EvalExercise {
    public bool? CombineResultOr;
    protected bool InitCombineResult() { return true; }
    bool combineResultOr() { return CombineResultOr == null ? InitCombineResult() : (bool)CombineResultOr; }
    public override Score GetScore() {
      Score res = new Score();
      if (combineResultOr()) {
        foreach (EvalExercise ex in EvalSubExercises()) { Score subScore = ex.GetScore(); res.From += subScore.From; res.Correct += subScore.Correct; res.NeedsHumanEval = res.NeedsHumanEval || subScore.NeedsHumanEval; }
      } else {
        res.From = 1; res.Correct = 1;
        foreach (EvalExercise ex in EvalSubExercises()) {
          Score subScore = ex.GetScore();
          if (subScore.From != subScore.Correct) { res.Correct = 0; return res; }
          res.NeedsHumanEval = res.NeedsHumanEval || subScore.NeedsHumanEval;
        }
      }
      return res;
    }
    public override Result CreateResult(EvalExercise ex) { return new DummyResult(); }
  }

  //Cast cviceni s vlastni evaluaci. Zrejme se nevyuziva
  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class Part : MultiExercise {
    public Exercise Body { get; set; }
    public override IEnumerable<Exercise> SubExercises { get { if (Body != null) yield return Body; } }
    public override void Expand() { Body = expandHelper(Body); }
    //public override bool IsValid() { return !EvalSubExercises().Any(s => !s.IsValid()); }
  }

  //Asi stejne jako EvalList s ListTemplates.no template
  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class Parts : Exercise { 
    public Exercise[] Items { get; set; }
    public override IEnumerable<Exercise> SubExercises { get { return getSubExcs(Items); } }
    public override void Expand() { Items = expandHelper(Items); }
    public List.ListStyles? Style { get; set; }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class ReplicaLike : List {
    public Rewise.RoleId RoleId { get; set; }
  }

  [XmlInclude(typeof(CheckItemResult))]
  [XmlInclude(typeof(SingleChoiceResult))]
  [XmlInclude(typeof(PairingResult))]
  [XmlInclude(typeof(DummyResult))]
  [XmlInclude(typeof(GapFillResult))]
  [XmlInclude(typeof(WritingResult))]
  [XmlInclude(typeof(SpeakingResult))]
  public partial class PageResult { }

  [XmlInclude(typeof(CoursePage))]
  [XmlInclude(typeof(Words))]
  [XmlInclude(typeof(ReplicaLike))]
  [XmlInclude(typeof(Parts))]
  [XmlInclude(typeof(Part))]
  [XmlInclude(typeof(GapFill))]
  [XmlInclude(typeof(Sound))]
  [XmlInclude(typeof(Text))]
  [XmlInclude(typeof(HtmlText))]
  [XmlInclude(typeof(CheckItem))]
  [XmlInclude(typeof(List))]
  [XmlInclude(typeof(EvalList))]
  [XmlInclude(typeof(Headered))]
  [XmlInclude(typeof(SingleChoice))]
  [XmlInclude(typeof(Pairing))]
  [XmlInclude(typeof(Writing))]
  [XmlInclude(typeof(Speaking))]
  public partial class Exercise { }

}
