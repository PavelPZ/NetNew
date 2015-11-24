using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace SLRewise {

  /************ Rewise object model ************/
  public enum TodayFactStatus {
    prepared, //fakt jeste dnes nekontaktovan
    repeated, //fakt jiz kontaktovan, neni ale splnen
    finished, //fakt splnen
  }

  public enum AddToRewiseStatus {
    no = 0,
    inserted = 1,
    removed = 2,
  }

  public struct FolderId {
    public int BookId;
    public short Id;
    public string ToUrl(Type type) {
      return string.Format("/pages/dictionaries/{0}.xaml?book={1}&id={2}", type.Name, BookId, Id);
    }
    public static FolderId FromUrl(string url, out string type) {
      string[] parts = url.Split('/', '.', '&', '=', '?');
      type = parts[3];
      return new FolderId() { BookId = int.Parse(parts[6]), Id = short.Parse(parts[8]) };
    }
  }

  public struct FactSelection {
    public int Id;
    public AddToRewiseStatus Status;
  }

  public struct RewiseFactId {
    public int FactId;
    public bool SrcToDest;
  }

  public struct LearnFactId {
    public FactId FactId;
    public bool SrcToDest;
  }

  public struct FactId {
    public int Id;
    public bool IsUserFactId { get { return GlobalId != Id; } }
    public bool IsGlobalFactId { get { return GlobalId != 0; } }
    public int GlobalId; //puvodni fatcId
    const Int64 flag = 0x0000000100000000;
    public Int64 Key {
      get {
        if (!IsUserFactId) return Id;
        Int64 val = Id;
        return flag | val;
      }
    }
  }

  public struct SaveRewiseFactRec {
    public RewiseFactId Id;
    public int NextRep;
    public float InitSt;
    public float BaseFactor;
    public float Stability;
    public byte[] history;
    public short RepNum;
    public int ActiveDay;
    public TodayFactStatus ActiveDayStatus;
  }

  /// <summary>
  /// Lekce a fakty, vybrane uzivatelem k nauceni
  /// </summary>
  public class LessonSelection {
    public FolderId Id;
    public AddToRewiseStatus LessonStatus;
    public FactSelection[] FactsStatus;
    public byte[] Encode() {
      using (MemoryStream ms = new MemoryStream()) using (BinaryWriter wr = new BinaryWriter(ms)) {
        wr.Write(Id.BookId);
        wr.Write(Id.Id);
        wr.Write((short)LessonStatus);
        wr.Write(FactsStatus == null ? 0 : FactsStatus.Length);
        if (FactsStatus != null) foreach (FactSelection f in FactsStatus) {
            wr.Write(f.Id);
            wr.Write((short)f.Status);
          }
        return ms.ToArray();
      }
    }
    public static LessonSelection Decode(byte[] data) {
      LessonSelection res = new LessonSelection();
      using (MemoryStream ms = new MemoryStream(data)) using (BinaryReader rdr = new BinaryReader(ms)) {
        res.Id.BookId = rdr.ReadInt32();
        res.Id.Id = rdr.ReadInt16();
        res.LessonStatus = (AddToRewiseStatus)rdr.ReadInt16();
        int len = rdr.ReadInt32(); if (len == 0) return res;
        res.FactsStatus = new FactSelection[len];
        for (int i = 0; i < len; i++) {
          res.FactsStatus[i].Id = rdr.ReadInt32();
          res.FactsStatus[i].Status = (AddToRewiseStatus)rdr.ReadInt16();
        }
        return res;
      }
    }
  }

}
namespace LMComLib {


  public enum PropIds {
    /*IsPhrase = 0,
    Question = 1,
    Answer = 2,
    QWords = 3,
    AWords = 4,
    AddToRewiseState = 5,*/
    //User data, jednosmerna modifikace (nezapisuje se do log DB)
    SessionId = 15,
    Rw_VideoDone = 16,
    Rw_SettingsDone = 17,
    Rw_TodaySelectdictDone = 18,
    Rw_TodayRewiseStarted = 19,
    Rw_StatisticShowed = 20,
    Rw_TodayRewiseDone = 21,
    Rw_LastRepeatDay = 22,
    Rw_TimeShift = 23,
    Rw_MidnightShift = 24,
    //Fact 
    /*Rw_History = 33,
    Rw_AddToRewiseState = 34,
    Rw_RepNum = 35,
    Rw_NextRep = 36,
    Rw_InitSt = 37,
    Rw_BaseFactor = 38,
    Rw_ActiveDay = 39,
    Rw_ActiveDayStatus = 40,
    Rw_Stability = 41,*/
  }

  /*public abstract class FactLogLow {
    public int UserId { get; set; }
    public int FactId { get; set; }
    public PropIds Prop { get; set; }
    public string Value { get; set; }
    public Langs Lang { get; set; }
    public bool IsUserFact { get; set; }
    public bool IsPhrase { get; set; }
  }

  public class UserFactLog : FactLogLow {
  }

  public class FactLog : FactLogLow {
  }

  public class FactLogResult {
    public int FactId { get; set; }
    public bool IsUserFact { get; set; }
    public PropIds Prop { get; set; }
    public string Value { get; set; }
  }

  public class BookLog {
    public int UserId { get; set; }
    public int BookId { get; set; }
    public short FolderId { get; set; }
    public PropIds Prop { get; set; }
    public string Value { get; set; }
  }*/

  public class UserLog {
    public int UserId { get; set; }
    public PropIds Prop { get; set; }
    public string Value { get; set; }
  }

  public class NewFactResult {
    public int FactId { get; set; }
    public string QWords { get; set; }
    public string AWords { get; set; }
  }

}
