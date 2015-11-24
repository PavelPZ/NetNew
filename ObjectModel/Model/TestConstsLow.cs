using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using LMNetLib;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using LMComLib;

namespace Course {

  public class exportUploadQuery {
    public DBExporterLow.TableId tbId;
    public int parentDbId;
    public int dbId;
    public DBQuestionPool.Mode DBQuestionPoolMode;
    public string CompanyEMail;
    public UIDTypes UIDType;
    public int UIDIncrement;
    public int rwLoc_BookId;
    public LineIds rwLoc_LocId;
    public string newTaskType;
    public string newTaskVariant;
  }

  public enum ServerCommands {
    //AdminInitDB,
    CreateTask,
    //createTestLicenceForDB = 2,
    //getServerUrl = 3,
    exportToZip,
    exportProjectForRun,
    exportTestForRun,
    importFromZip,
    rewiseDesignImportEuroTalk,
    rewiseDesignImportLANGMaster,
    rewiseExportProject,
    rewiseImportProject,
    rewiseCompileProject,
    rewiseCompileAll,
    rewiseImportBook,
    rewiseExportBook,
    getUniqueId,
    rewiseAddBookLoc,
    rewiseDeleteBookLoc,
    testExport,
    testImport,
    testProjectExport,
    testProjectImport,
  }

  public enum UIDTypes {
    no = 0,
    rwBook = 1,
    rwLesson = 2,
    rwFact = 3,
    test = 4
  }

  public enum TestTaskTypes : int {
    Listening_SingleChoice = 0,
    Listening_TrueFalse = 1,
  }
  public enum TestManRoles : int {
    Creator = 0,
    Reader = 1
  }
  public enum Instructions : int {
    truefalselistening1 = 0,
    singlechoicelistening = 1,
  }

  public static class DBExporterLow {
    public enum TableId {
      project,
      test,
      skill,
      group,
      task
    }
    public enum Format {
      zip,
      xml
    }
  }


  public enum ExternalResultType {
    no,
    zip_wma,
    wav,
    speex,
  }

  public static class ResultExtension {
    public static string Ext(ExternalResultType type) {
      switch (type) {
        case ExternalResultType.speex: return ".lmspeex";
        case ExternalResultType.wav: return ".wav";
        case ExternalResultType.zip_wma: return ".wma";
        default: throw new NotImplementedException();
      }
    }
    public static ExternalResultType Type(string url) {
      string ext = url.Substring(url.LastIndexOf('.'));
      switch (ext.ToLower()) {
        case ".lmspeex": return ExternalResultType.speex;
        case ".wav": return ExternalResultType.wav;
        case ".mp3":
        case ".wma": return ExternalResultType.zip_wma;
        default: throw new NotImplementedException();
      }
    }
  }

  public enum TestStatus {
    no = 0,
    Invitated = 1,
    InvitationAccepted = 2,
    //Started,
    Started_selfTesting = 3, //jsem ve fazi selftestu => pokracovani v selftestu
    Started_levelsKnown =4, //jsou znamy urovne testovani: budto po selftestu nebo po pevnem urceni urovne. => Start stranka
    Started_testing = 5, //testovani => pokracovani v testu
    //tested, //ukonceno testovani => finalni stranka
    Interrupted = 6,
    SendedToEvaluation = 7, //hotovo, ceka na vyhodnoceni
    //EvaluationStarted,
    Evaluated = 8, //po SendedToEvaluation: vyhodnoceno
    EvalAssigned = 9, //po SendedToEvaluation: v HighSchool konzoli prirazeno tutorovi
  }

  public struct IdDataBin {
    public int Id;
    public byte[] DataBin;
  }

  public class LoadCourseInfoData {
    //public string Data;
    public IdDataBin DataBin;
    //public string[] ModuleData;
    public IdDataBin[] ModuleDataBin;
  }

  public class FillCompanyUserInfoData {
    public class User { public string EMail; public string FirstName; public string LastName; public string MembershipData; } //ELandUser data
    public class Courses { /*public int Id;*/ public string EMail; /*public string Data;*/ public IdDataBin DataBin; }
    public class Module { public int CourseId; /*public string ProxyData;*/ public IdDataBin ProxyDataBin; }
    public User[] Profiles;
    public Courses[] Tests;
    public Module[] Modules;
  }

  public class CreateUserData {
    public string EMail;
    public string FirstName;
    public string LastName;
  }

  public enum TestLockMode {
    doLock,
    forceLock,
    unlock
  }

  public enum TestInvitationResult {
    ok,
    alreadyInvitated,
    licenceLimitExceeded
  }

  public class CreateTestResult {
    //public IdDataBin DataBin;
    public int TestId;
    public string FirstName;
    public string LastName;
    public string EMail;
    public string CompanyEMail;
    public TestInvitationResult Result;
  }

  public class TestCriteria {
    //public Guid TestId;
    //public int Id;
    public Int64 LMComUserId;
    public string EMail;
    public IdDataBin DataBin;
    /*-------------------*/
    public short Status;
    public string CompanyEMail;
    public string EvaluatorLock;
    public Levels Level; //Uroven testu
    public CourseIds CourseId;
    public string Title;
    public string Descr;
    public DateTime Invitated;
    /*-------------------*/
    public DateTime Start;
    public DateTime End;
    public short ElapsedSeconds;
    public short Interruptions;
    public Levels ScoreLevel;
    public short Score;
    public short AbsScore;
    public Levels AbsLevel;
    public short GlobalScore;
    public string GlobalScores;
    /*-------------------*/
    public bool Demo_DeleteAllOtherTests;
    public Guid ImportID; //interni identifikace testu
  }

  public class ModuleCriteria {
    //public int Id;
    //public Guid ModuleId;
    //public Guid CourseId;
    public int CourseId;
    public IdDataBin ProxyDataBin;
    public IdDataBin DataBin;
    /*-------------------*/
    public short Status;
    public short Level;
    /*-------------------*/
    public Skills Skill;
    public short Score;
    public short ElapsedSeconds;
    public short AbsScore;
    public Levels AbsLevel;
    public short GlobalScore;
  }

  public class LectorTestQuery {
    public string[] Invitators;
    public int TestId;
    public int[] TestIds; 
  }

  public partial class TestLicOrder {
    public DateTime Created;
    public DateTime? Payed;
    public DateTime? ProformaAt;
    //public DateTime ValidTo;
    public string Title { get; set; }
    public CourseIds CourseId;
    public int LicenceNum;
    public TestLicTest[] Tests;
  }

  public class TestLicTest {
    public string EMail { get; set; }
    //public LineIds Line;
    public CourseIds CourseId;
    public Levels Level;
    public DateTime Invitated;
  }

  public class Invitation {
    public Guid Id; //Id testu
    public CourseIds CourseId; //jazyk testu
    public Levels Level; //Uroven testu
    public DateTime Created; //Datum vytvoreni zaznamu
  }

  //DB LAYER
  public class DBQuestionPool {
    public string Id;
    public string Title;
    public string BasicPath;
    public LineIds Line;
    //public int DBTestId;
    //public string Descr;
    //public Guid ExportID;
    //public string CompanyEMail;
    public enum Mode {
      random = 0,
      all = 1,
      layer_0 = 2,
      layer_1 = 3,
      layer_2 = 4,
      layer_3 = 5,
      layer_4 = 6,
      layer_5 = 7,
    }
    public class Module {
      public Skills Skill;
      public int LevelWeight;
      public int AlowedTimeMinutes;
      public Group[] Groups;
    }
    public class Group {
      public int SelectNum;
      public int Order;
      public string[] Pointers;
    }
    public Module[] Modules;
  }

  //DB LAYER
  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public class TestDBPointer : QuestionPointer {
    public int TaskId;
    public string FilePath;
    public override bool IsEqual(QuestionPointer ptr) {
      if (!base.IsEqual(ptr)) return false;
      TestDBPointer testPtr = (TestDBPointer)ptr;
      if (FilePath != null) {
        return testPtr.FilePath == null ? false : string.Compare(FilePath, testPtr.FilePath, StringComparison.InvariantCultureIgnoreCase) == 0;
      } else if (testPtr.FilePath != null)
        return false;
      else
        return TaskId == testPtr.TaskId;
      //return string.Compare(FilePath, testPtr.FilePath, StringComparison.InvariantCultureIgnoreCase)==0;
    }
    public static string dbCoursePath(string projectTest) {
      return "db/" + projectTest;
    }
    public static string dbCoursePath(int dbTestId) {
      return "db/" + dbTestId.ToString();
    }
    public static bool isDbCoursePath(string path) {
      return path.ToLower().StartsWith("db/");
    }
    //public static Tuple<int?,string> dbIdFromCoursePath(string path) {
    //  var id = path.Split('/')[1]; bool isInt = char.IsDigit(id[0]);
    //  return new Tuple<int?, string>(isInt ? (int?)int.Parse(id) : null, isInt ? null : id);
    //}
    public static Tuple<int?, string> dbIdFromCoursePath(string path) {
      var id = path.Replace("db/", null); bool isInt = char.IsDigit(id[0]);
      return new Tuple<int?, string>(isInt ? (int?)int.Parse(id) : null, isInt ? null : id);
    }
  }

  [XmlInclude(typeof(TestPointer))]
  [XmlInclude(typeof(TestDBPointer))]
  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public class QuestionPointer {
    public virtual bool IsEqual(QuestionPointer ptr) { return ptr.GetType() == this.GetType(); }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public class QuestionPool {
    public LineIds Line;
    public TestPointer[] Pointers;
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public class TestPointer : QuestionPointer {
    public Levels Level;
    public Skills Skill;
    public string SkillPath { get; set; }
    //public string TypeName;
    public TestPointer SubPointer;
    public IEnumerable<TestPointer> DescendantAndSelf() { yield return this; if (SubPointer != null) yield return SubPointer; }
    //public string Path(string subPath) { return LMMedia.Paths.GetDataPath(subPath + "/" + Level.ToString() + "/" + Skill.ToString() + "/" + SkillPath); }
    public override bool IsEqual(QuestionPointer ptr) {
      if (!base.IsEqual(ptr)) return false;
      TestPointer testPtr = (TestPointer)ptr;
      return testPtr.Level == Level && testPtr.Skill == Skill && string.Compare(testPtr.SkillPath, SkillPath, StringComparison.OrdinalIgnoreCase) == 0;
    }
  }


}

