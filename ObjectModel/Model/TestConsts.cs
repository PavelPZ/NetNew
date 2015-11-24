using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LMNetLib;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using LMComLib;

namespace Course {

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class TestProxy {
    public int Id; //Id testu
    public TestStatus Status;
    public byte[] dbPool; //struktura pointeru testu pro DB test
    public string Title;
    public string Descr;
    //public LineIds Line { get; set; } //jazyk testu
    public CourseIds CourseId { get; set; } //jazyk testu
    public Levels Level; //Uroven testu
    public DateTime Created; //Datum vytvoreni zaznamu
    public string CompanyEMail = ""; //email firmy, co poslala invitation
    public string EMail; //email testovaneho
    public string EvaluatorLock; //email lektora, ktery ma zablokovan test pro editaci
    public string Evaluator; //email lektora, ktery dokoncil evaluaci
    public DateTime StartEvaluation; //datum zacatku evaluace
    public DateTime EndEvaluation; //datum konce evaluace
    public string FirstName; //jmeno v dobe zalozeni testu
    public string LastName; //a prijmeni
    //pro davkovy import testu
    public Int64 LMComUserId; //lmcom id uzivatele
    public Guid ImportID; //interni identifikace testu
    public LineIds ForceLine;
    public int MetaTaskId; //parseInt pro json metatasku

    //subcesta k datum, napr. 
    //"Tests/Tutorials/cs_cz/english" pro s:\LMCom\RW2\Server\Url\Tests\Tutorials\English\cs_cz\A1\useenglish\vocabulary\pairing\1.xml 
    //"Tests/Design/english"
    //"Tests" pro s:\LMCom\RW2\Server\Url\Tests\English\A1\useenglish\vocabulary\pairing\1.xml 
    public string SubPath;
    public string MySubPath { get { return TestDBPointer.isDbCoursePath(SubPath) ? TestDBPointer.dbIdFromCoursePath(SubPath).Item2 : SubPath + "/" + CommonLib.CourseIdToLineId(CourseId).ToString(); } }
    //public string MySubPath { get { return SubPath + "/" + CommonLib.CourseIdToLineId(CourseId).ToString(); } }
    public List<HistoryItem> History { get { return history ?? (history = new List<HistoryItem>()); } set { history = value; } } List<HistoryItem> history;
    public List<SkillLevel> SkillLevels; //urovne testovani (budto po selftestu nebo po pevnem urceni urovne)
    public List<ModuleData> CourseTree { get; set; }//jednotlive casti testu 

    /*public void ModuleFinished(Guid id) {
      CourseTree.First(m => m.Id == id).Status = ExerciseStatus.Evaluated;
    }*/

    public void SetStatus(TestStatus value) {
      if (Status == value) return;
      Status = value;
      History.Add(new HistoryItem() { Created = DateTime.UtcNow, Status = value });
    }
    public void ModuleProxyToCourseTree(ModuleData proxy) {
      for (int i = 0; i < CourseTree.Count; i++) if (proxy.Id == CourseTree[i].Id) { CourseTree[i] = proxy; break; }
    }
    void ModuleProxyToCourseTree(ModuleData[] proxies) { foreach (ModuleData proxy in proxies) ModuleProxyToCourseTree(proxy); }

    public class SkillLevel { public Skills Skill; public Levels Level; } //level pro danou skill

    public void initSkillLevels(Levels lev) { //inicializuje SkillLevels, vsechny na jednu level
      SkillLevels = lev == Levels.Assessment || lev == Levels.EslTest || lev == Levels.Demo ? null : LowUtils.EnumGetValues<Skills>().Select(s => new SkillLevel() { Skill = s, Level = lev }).ToList();
    }

    //Persistence:
    public byte[] Encode() { return XmlUtils.SerializeAndCompress(this); }
    public static TestProxy Decode(IdDataBin data) {
      TestProxy res = XmlUtils.DecompressAndDeserialize<TestProxy>(data.DataBin);
      res.Id = data.Id;
      return res;
    }
    public static TestProxy Decode(IdDataBin data, IEnumerable<IdDataBin> moduleProxy) {
      TestProxy res = XmlUtils.DecompressAndDeserialize<TestProxy>(data.DataBin);
      res.Id = data.Id;
      res.ModuleProxyToCourseTree(moduleProxy);
      return res;
    }
    public void ModuleProxyToCourseTree(IEnumerable<IdDataBin> moduleProxy) {
      ModuleProxyToCourseTree(moduleProxy.Select(m => ModuleData.Decode(m)).ToArray());
    }
    public TestCriteria CreateCriteria() {
      HistoryItem it = history == null ? null : history.FirstOrDefault(h => h.Status == TestStatus.InvitationAccepted);
      DateTime invitated = it == null ? LMNetLib.LowUtils.startDate : it.Created;
      TestCriteria res = new TestCriteria() {
        EMail = EMail,
        DataBin = new IdDataBin() { Id = Id, DataBin = Encode() },
        Status = (short)Status,
        CompanyEMail = CompanyEMail,
        EvaluatorLock = EvaluatorLock,
        Level = Level,
        CourseId = CourseId,
        Invitated = invitated,
        Start = LowUtils.startDate,
        End = LowUtils.startDate,
        Title = Title,
        Descr = Descr,
        LMComUserId = LMComUserId,
        ImportID = ImportID,
      };
      if (Status == TestStatus.Evaluated) {
        LevelScore score = GetLevelScore();

        res.ScoreLevel = score.level; //level budto pevna (pro Level!=Details.EslTest, kdy jsou vsechny moduly stejne levels) nebo vypocitana (kazdy modul jina uroven)
        res.Score = (short)score.score; //score pro pevnou level (pro Level!=Details.EslTest) nebo vzhledem k vypocitane ScoreLevel

        res.AbsLevel = score.absLevel; //vypocitana absolutni level (pro Level==Details.EslTest stejna jako ScoreLevel)
        res.AbsScore = (short)score.absScore; //score vzhledem k AbsLevel

        res.GlobalScore = (short)score.globalScore; //odpovida skore pro Level=C2
        res.GlobalScores = CourseTree.Select(m => string.Format("{0}={1}", m.Skill, TestProxy.GetGlobalScore(m))).Aggregate((r, i) => r + ";" + i);

        res.Interruptions = (short)InterruptedLow;
        res.Start = StartedLow;
        res.End = FinishedLow;
        res.ElapsedSeconds = (short)ElapsedSecondsLow;

      }
      return res;
    }

    //************ Score
    public int InterruptedLow { get { return History.Where(h => h.Status == TestStatus.Interrupted).Count(); } }
    public DateTime StartedLow { get { return CourseTree.First().Started; } }
    public DateTime FinishedLow { get { return CourseTree.Last().Finished; } }
    public int ElapsedSecondsLow { get { return CourseTree.Sum(m => m.ElapsedTime) / 1000; } }

    public struct LevelScore {
      public Levels level; //level budto pevna (pro test!=Details.EslTest, kdy jsou vsechny moduly stejne levels) nebo vypocitana (kazdy modul jina uroven)
      public int score; //score pro pevnou level (pro test!=Details.EslTest) nebo vzhledem k vypocitane level
      public Levels absLevel; //vzdy vypocitana level
      public int absScore; //skore vyhledem k absLevel
      public int globalScore; //absolutni score 0..100
#if SILVERLIGHT && !eTestMeManager && !SLLIB
      public override string ToString() {
        if (score < 0) return Score.WaitingText();
        return string.Format("{0} ({1}%)", TestHelper.LevelTextShort(level), score);
      }
#endif
    }

    public static Levels[] LevelsBasic = new Levels[] { Levels.A1, Levels.A2, Levels.B1, Levels.B2, Levels.C1, Levels.C2, };
    public static int LevelNum(Levels lev) { return (int)lev; }
    public LevelScore GetLevelScore() { return GetLevelScore(Level, CourseTree); }

    static double Weights(Levels lev) { return lev == Levels.no ? 1.0 : weights[LevelNum(lev)]; }

    public static int GetGlobalScore(ModuleData mod) {
      double Correct = mod.Score.From == 0 ? 0.0 : (double)mod.Score.Correct / mod.Score.From * Weights(mod.Level);
      return (int)(Correct * 100);
    }
    public static LevelScore GetLevelScore(ModuleData mod) {
      double Correct = mod.Score.From == 0 ? 0.0 : (double)mod.Score.Correct / mod.Score.From * Weights(mod.Level);
      return GetLevelScore(mod.Level, Correct);
    }

    //weights: vahy jednotlivych levels v procentech
    static LevelScore GetLevelScore(Levels level, IEnumerable<ModuleData> data) {
      if (data.Count() == 0) return new LevelScore();
      double Correct = 0; int cnt = 0;
      foreach (ModuleData mod in data) {
        if (mod.Score.NeedsHumanEval) return new LevelScore() { score = -1 };
        var w = mod.LevelWeight==0 ? 1 : mod.LevelWeight;
        Correct += mod.Score.From == 0 ? 0.0 : (double)mod.Score.Correct / mod.Score.From * Weights(mod.Level) * w;
        cnt += w;
      }
      Correct = Correct / cnt;
      return GetLevelScore(level, Correct);
    }

    //relativni (level/score) skore na absolutni (score)
    static LevelScore GetLevelScore(Levels level, double Correct) {
      LevelScore res = new LevelScore();
      //globalni score (vzhledem k C2). 100 v C2 ma globalScore 100. Nizsi urovne maji globalni score vzdy o c_delta krat nizsi.
      res.globalScore = (int)(Correct * 100);
      if (level == Levels.no) {
        res.absLevel = res.level = Levels.no;
        res.score = res.absScore = res.globalScore;
      } else {
        //vypocti absolutni level a absolutni score. Vstupem je globalScore, na zaklade nej spocte optimalni absScore a absLevel tak, aby byly porovnatelne EslTest a A1,...,C2 testy.
        res.absLevel = Levels.A1;
        foreach (Levels lev in LevelsBasic)
          if (Correct <= Weights(lev) * 0.9) { res.absLevel = lev; break; }
        res.absScore = (int)(Correct / weights[LevelNum(res.absLevel)] * 100);
        //level a score (do diplomu apod.) - pro A1...C2 testy Level zustava, pro EslTest se prepocte na absLevel x absScore
        res.level = LevelsBasic.Contains(level) ? level : res.absLevel;
        res.score = (int)(Correct / Weights(res.level) * 100);
      }
      return res;
    }
    const double c_delta = 0.85;
    static double[] weights { get { if (_weights == null) { _weights = new double[6]; _weights[5] = 1.0; for (int i = 4; i >= 0; i--) _weights[i] = _weights[i + 1] * c_delta; } return _weights; } } static double[] _weights;

  }

  public class HistoryItem {
    public TestStatus Status;
    public DateTime Created; //Datum vytvoreni zaznamu
  }

  public class TestUserInfo {
    public string EMail;
#if SILVERLIGHT
    public UserData User { get { return user == null && !string.IsNullOrEmpty(MembershipData) ? user = XmlUtils.StringToObject<UserData>(MembershipData) : user; } } internal UserData user;
#endif
    public string MembershipData; //Uzivateluv profil, existuje-li
    public List<TestProxy> Tests { get; set; } //Info o vsech testech
  }

}

