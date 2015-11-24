using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using LMComLib;
using System.IO;
#if !PORTABLE
using LMNetLib;
#endif
using System.ComponentModel;

namespace LMComLib {

  public class Cmd {
    public Int64 lmcomId;
    public Int64 sessionId;
  }
}

namespace Rewise {
  public enum IconIds {
    no, a, b, c, d, e, f
  }

  public struct RoleId {
    public IconIds IconId { get; set; }
    public string Name;
    public bool Eq(RoleId id) {
      return Name == id.Name && IconId == id.IconId;
    }
  }

}

namespace Course {

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class ModuleData {
    public string Title;
    //public Guid Id;
    //public Guid testId;
    public int Id;
    public int CourseId;
    public Skills Skill;
    public Levels Level;
    public PageResult[] Results;
    public int LevelWeight; //vaha dovednosti v ramci testu

    public int ElapsedTime { get { return emptyResult ? elapsedTime : elapsedTime = Results.Sum(r => (int)r.ElapsedTime); } set { elapsedTime = value; } } int elapsedTime;
    public Score Score { get { return emptyResult ? score : score = GetScore(); } set { score = value; } } public Score score; //DEBUG
    public DateTime Started { get { return emptyResult ? started : started = Results.Where(p => p.Start != DateTime.MaxValue).Select(p => p.Start).DefaultIfEmpty(DateTime.UtcNow).First(); } set { started = value; } } DateTime started;
    public DateTime Finished { get { return emptyResult ? finished : finished = Results.Where(p => p.End != DateTime.MaxValue).Select(p => p.End).DefaultIfEmpty(DateTime.UtcNow).Last(); } set { finished = value; } }  DateTime finished;
    public int ResultLength { get { return emptyResult ? resultLength : resultLength = Results.Length; } set { resultLength = value; } }  int resultLength;
    public Score GetScore() {
      //DEBUG
      //return score;
      Score res = new Score();
      foreach (PageResult ex in Results.Where(r => r.Status == ExerciseStatus.Evaluated)) { res.From += ex.EvalScore.From; res.Correct += ex.EvalScore.Correct; res.NeedsHumanEval = res.NeedsHumanEval || ex.EvalScore.NeedsHumanEval; }
      return res;
    }
    //Status pro persistenci
    public ExerciseStatus Status {
      get {
        if (emptyResult || (status != ExerciseStatus.Normal && status != ExerciseStatus.Unknown)) return status;
        status = Results.Any(r => r.Status != ExerciseStatus.Evaluated) ? ExerciseStatus.Normal : ExerciseStatus.Evaluated;
        return status;
      }
      set { status = value; }
    } ExerciseStatus status;
    bool emptyResult { get { return Results == null || Results.Length == 0; } }

    public string[] ScormDataKey;
    public string[] ScormDataValue;
    [XmlIgnore]
    public TimeSpan AlowedTime; //maxiamlni povoleny cas pro modul. TimeSpan.Zero: nekonecno
    [DefaultValue(0)]
    public long AlowedTimeXml { get { return AlowedTime.Ticks; } set { AlowedTime = new TimeSpan(value); } }

    public ModuleCriteria CreateCriteria(int testId, Dictionary<string, string> scormData) {
      //Velka data, zaroven se vypocitaji properties, odvozene z Results
      if (scormData != null) { ScormDataKey = scormData.Select(kv => kv.Key).ToArray(); ScormDataValue = scormData.Select(kv => kv.Value).ToArray(); }
      byte[] encode = Encode();
      //Mala proxy data
      PageResult[] res = Results; Results = null; ScormDataValue = ScormDataKey = null;
      byte[] encodeProxy = Encode();
      Results = res;
      //Uloz do DB
      ModuleCriteria crit = new ModuleCriteria() {
        CourseId = testId,
        DataBin = new IdDataBin() { Id = Id, DataBin = encode },
        ProxyDataBin = new IdDataBin() { Id = Id, DataBin = encodeProxy },
        Status = (short)Status,
        Level = (short)Level
      };
      if (Status == ExerciseStatus.Evaluated) {
        crit.Skill = Skill;
        crit.ElapsedSeconds = (short)(ElapsedTime / 1000);
        crit.Score = (short)Score.ToPercent();
        TestProxy.LevelScore score = TestProxy.GetLevelScore(this);
        crit.AbsLevel = score.absLevel;
        crit.AbsScore = (short) score.absScore;
        crit.GlobalScore = (short)score.globalScore;
      }
      return crit;
    }
    public byte[] Encode() {
      return XmlUtils.SerializeAndCompress(this);
    }

    public static ModuleData Decode(IdDataBin data) {
      ModuleData res = XmlUtils.DecompressAndDeserialize<ModuleData>(data.DataBin);
      res.Id = data.Id;
      return res;
    }
    public static ModuleData Decode(IdDataBin data, Dictionary<string, string> scormData) {
      ModuleData res = XmlUtils.DecompressAndDeserialize<ModuleData>(data.DataBin);
      res.Id = data.Id;
      if (scormData != null) {
        scormData.Clear();
        if (res.ScormDataKey != null) for (int i = 0; i < res.ScormDataKey.Length; i++) scormData.Add(res.ScormDataKey[i], res.ScormDataValue[i]);
      }
      return res;
    }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class ExerciseField { }

  public interface IExternalResult {
    Stream Data();
    ExternalResultType Type { get; set; }
    string url { get; set; }
    Guid getId();
    //string basicPath();
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class PageResult : Result {
    [DefaultValue(null)]
    public string Title { get; set; }
    public List<Result> Results;
    [DefaultValue(ExerciseStatus.Unknown)]
    public ExerciseStatus Status { get { return status; } set { status = value; } } ExerciseStatus status;
    public Score EvalScore;
    public DateTime Start;
    public DateTime End;
    [DefaultValue(0)]
    public int ElapsedTime;
    public QuestionPointer Ptr;
    [XmlIgnore]
    public Guid Version; //verze Exercise, jehoz nese tento Result vysledky
    public void Reset() { Results = null; Version = Guid.Empty; Status = ExerciseStatus.Unknown; }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class Result {
    public int Id;
    //Q:\LMNet2\WebApps\eduauthornew\framework\script\lm\LMScormModule.js
    //this.passive = false;
    //this.forceEval = false; //Objeví se stránka po vyhodnocení, i když je score null nebo stránka je pasivní
    //this.forceNotEval = false; //Stránka po vyhodnocení se neobjeví, i když by měla
    //this.notResetable = false; //Stránku nejde vyresetovat (např. pro testy apod.)
    //this.errorLimit = 75; //Hranice v procentech, kdy se objeví "velke mnozstvi chyb, presto vyhodnotit" dialog. 0..neobjevi se nikdy
  }

  public class ChildStyles {
    public Text.TextStyles? Text { get; set; }
    public Text.TextStyles? HtmlText { get; set; }
    public Text.TextStyles? GapFill { get; set; }
    public CoursePage.CoursePageStyles? CoursePage { get; set; }
    public List.ListStyles? List { get; set; }
    public List.ListStyles? EvalList { get; set; }
    public List.ListStyles? Parts { get; set; }
    public Headered.HeaderedStyles? Headered { get; set; }
  }

  public interface IExpandable {
    IEnumerable<Exercise> Expand();
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class Exercise {

    public static Exercise FromStream(Stream str) { return XmlUtils.StreamToObject<Exercise>(str); } 
    public static Exercise FromBytes(byte[] data) { return FromString(Encoding.UTF8.GetString(data, 0, data.Length)); }
    public static Exercise FromString(string str) { return XmlUtils.StringToObject<Exercise>(str); }

    public virtual bool Validate(Action<string> error) { return true; }

    public ChildStyles ChildStyle;
    public virtual IEnumerable<Exercise> SubExercises { get { yield break; } } //prvni uroven Exercises
    public virtual object getTemplate() { return null; }
    public IEnumerable<EvalExercise> EvalSubExercises() { //prvni uroven EvalExercises
      return SubExercises.SelectMany(ex => ex is EvalExercise ? XExtension.Create<EvalExercise>((EvalExercise)ex) : ex.EvalSubExercises());
    }
    public IEnumerable<Exercise> AllParents() { Exercise par = MyParent; while (par != null) { yield return par; par = par.MyParent; } }

    [XmlIgnore]
    public Exercise Owner { get; set; }
    public CoursePage MyPage { get { return Owner is CoursePage ? (CoursePage)Owner : MyParent.MyPage; } }
    public PageResult MyPageResult { get { return MyPage.myResult; } }
    public Exercise MyParent { get { return Owner; } }
    public bool IsExercisePart { get { return GetType() == typeof(Part) || (this is EvalExercise && !AllParents().Any(p => p.IsExercisePart)); } } //cast cviceni s vlastnim vyhodnocenim a evaluaci

    //EXPANDABLE
    public virtual void Expand() { }
    public static Exercise expandHelper(Exercise by) {
      if (by == null || !(by is IExpandable)) return by;
      return ((IExpandable)by).Expand().SingleOrDefault();
    }
    protected static IEnumerable<Exercise> getSubExcs(Exercise[] excs) { return excs == null ? Enumerable.Empty<Exercise>() : excs; }
    protected static Exercise[] expandHelper(Exercise[] excs) {
      if (excs == null || !excs.Any(e => e is IExpandable)) return excs;
      List<Exercise> res = new List<Exercise>();
      foreach (Exercise ex in excs)
        if (ex is IExpandable) res.AddRange(((IExpandable)ex).Expand()); else res.Add(ex);
      return res.ToArray();
    }
#if !SILVERLIGHT
    public void Save(string path) {
      XmlUtils.ObjectToFile<Exercise>(path, this);
    }
#endif
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class CoursePage : MultiExercise {
    [XmlIgnore]
    public Guid Version;
    public override IEnumerable<Exercise> SubExercises { get { if (Content != null) yield return Content; } }
    public override void Expand() { Content = expandHelper(Content); }

    public Exercise Content { get; set; }
    [DefaultValue(false)]
    public bool NoEval; //stranka se nevyhodnocuje
    [XmlIgnore]
    public PageResult myResult { get { return (PageResult)MyResult; } }
    [XmlIgnore]
    DateTime loadedTime;
    [XmlIgnore]
    public string OrigUrl;

    //DB LAYER
    public static CoursePage CreatePage(Func<Exercise> read, string origUrl, PageResult result) {
      Exercise ex = read();
      if (ex is IExpandable) ex = expandHelper(ex);
      if (!(ex is CoursePage)) throw new Exception();
      CoursePage res = (CoursePage)ex;
      res.Loaded(result, origUrl);
      return res;
    }

    public static CoursePage CreatePage(Stream str, string origUrl, PageResult result) {
      return CreatePage(() => Exercise.FromStream(str), origUrl, result);
    }

    public static CoursePage CreatePage(byte[] data, string origUrl, PageResult result, string nameSpace) {
      return CreatePage(() => {
        string str = Encoding.UTF8.GetString(data, 0, data.Length);
        return XmlUtils.StringToObject <Exercise>(str);
      }, origUrl, result);
    }

    void Loaded(PageResult res, string origUrl) {
      OrigUrl = origUrl;
      setResult(res);
      AssignOwners(this, null);
      if (myResult.Version == Guid.Empty) myResult.Version = Version; //prvni sparovani cviceni s vysledkem
      else if (myResult.Version != Version) myResult.Reset(); //nesouhlasi verze
      AssignUniqueIds();
    }

    void AssignOwners(Exercise ex, Exercise owner) {
      ex.Owner = owner; ex.Expand();
      foreach (Exercise subex in ex.SubExercises) AssignOwners(subex, ex);
    }

    public enum PageStage { onLoad, onEval, onUnload }

    public void RefreshEvalScore() { myResult.EvalScore = GetScore(); }

    public void adjustPageResult(ExerciseStatus moduleStatus, PageStage stage) {
      if (moduleStatus != ExerciseStatus.Normal) return;
      switch (stage) {
        case PageStage.onLoad:
          if (myResult.Status == ExerciseStatus.Unknown) { myResult.Start = DateTime.UtcNow; myResult.ElapsedTime = 0; myResult.Status = ExerciseStatus.Normal; }
          loadedTime = DateTime.UtcNow;
          break;
        case PageStage.onEval:
          myResult.ElapsedTime += (int)(DateTime.UtcNow - loadedTime).TotalMilliseconds;
          myResult.End = DateTime.UtcNow;
          myResult.Status = ExerciseStatus.Evaluated;
          RefreshEvalScore();
          break;
        case PageStage.onUnload:
          if (myResult.Status == ExerciseStatus.Normal) myResult.ElapsedTime += (int)(DateTime.UtcNow - loadedTime).TotalMilliseconds;
          break;
      }
    }

    public void AssignUniqueIds() { int cnt = 1; AssignUniqueIds(Content, ref cnt); }
    void AssignUniqueIds(Exercise content, ref int cnt) { if (content is EvalExercise) ((EvalExercise)content).Id = cnt++; foreach (Exercise ex in content.SubExercises) AssignUniqueIds(ex, ref cnt); }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class EvalExercise : Exercise {
    public int Id; //jednoznacne id v ramci CoursePage
    public virtual Result CreateResult(EvalExercise ex) { throw new NotImplementedException(); }
    public virtual Score GetScore() { throw new NotImplementedException(); }
    [XmlIgnore]
    public Result MyResult {
      get {
        if (myResult != null) return myResult;
        PageResult res = MyPageResult;
        if (res.Results == null) res.Results = new List<Result>();
        myResult = res.Results.FirstOrDefault(r => r.Id == Id);
        if (myResult == null) { res.Results.Add(myResult = CreateResult(this)); myResult.Id = Id; }
        return myResult;
      }
    } Result myResult;
    protected void setResult(Result myResult) { this.myResult = myResult; }
  }

  public abstract class NeedsEvalResult : Result {
    public Score? MyScore;
  }
  public abstract class NeedsEvalExercise : EvalExercise {
    public override Score GetScore() {
      if (myResult.MyScore == null) myResult.MyScore = new Score(true);
      return (Score)myResult.MyScore;
    }
    public NeedsEvalResult myResult { get { return (NeedsEvalResult)MyResult; } }
  }

}
