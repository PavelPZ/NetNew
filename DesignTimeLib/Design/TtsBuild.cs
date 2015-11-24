using LMComLib;
using LMNetLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Tts {

  public static class Lib {
    //public static void exportAllTtsCourseSounds(CourseMeta.productDescrNew prod) {
    //  var dir = Dir.Load(prod.courseLang);
    //  StringBuilder sb = new StringBuilder();
    //  foreach (string w in prod.
    //    Modules().
    //    SelectMany(m => m.exs.Select(e => CourseModel.lib.ReadExercise(e, sb/*, true*/))).
    //    SelectMany(pg => CourseModel.lib.Scan(pg).Select(s => s.tag).OfType<CourseModel.TtsSound>()).
    //    Select(s => s.Text.ToLower().Trim())) {
    //    dir.adjustItem(w);
    //  }
    //  dir.Save();
    //}
  }

  //informace propName TTS souborech v napr. Web4\RwTTS\de_de\
  public class Dir {
    public static Dir Load(Langs lang, bool forceScan = false) {
      var fn = string.Format(@"{0}RwTTS\{1}\map.xml", Machines.rootPath, lang);
      if (File.Exists(fn) && !forceScan) { var res = XmlUtils.FileToObject<Dir>(fn); res.selfPath = fn; return res; }
      LowUtils.AdjustFileDir(fn);
      return new Dir() {
        lang = lang,
        Items = new DirectoryInfo(Path.GetDirectoryName(fn)).GetFiles("*.xml", SearchOption.AllDirectories).Where(f => f.Name.ToLower() != "map.xml").Select(f => XmlUtils.FileToObject<DirItem>(f.FullName)).ToList(),
        selfPath = fn,
      };
    }
    public Langs lang;
    public void Reset() { if (File.Exists(selfPath)) File.Delete(selfPath); }
    public List<DirItem> Items;
    public void Save() { XmlUtils.ObjectToFile(selfPath, this); }
    public DirItem adjustItem(string text) {
      var res = Items.FirstOrDefault(i => i.text == text);
      if (res != null) {
        var mp3Exists = File.Exists(fileName(res, false));
        if (mp3Exists && res.version == 0) res.version = 1;
        else if (!mp3Exists && res.version != 0) res.version = 0;
        return res;
      }
      Reset();
      var freeDir = Items.GroupBy(i => i.dirId).OrderBy(g => g.Key).FirstOrDefault(g => g.Count() < 300);
      int dirId; int fileId;
      if (freeDir == null) { dirId = Items.Select(i => i.dirId).DefaultIfEmpty(-1).Max() + 1; fileId = 0; }
      else { dirId = freeDir.Key; fileId = freeDir.Select(f => f.fileId).Max() + 1; }
      Items.Add(res = new DirItem() { text = text, dirId = dirId, fileId = fileId });
      var itemFn = fileName(res, true); //string.Format(@"{0}\{1}\{2}.xml", Path.GetDirectoryName(selfPath), res2.dirId, res2.fileId);
      LowUtils.AdjustFileDir(itemFn); XmlUtils.ObjectToFile(itemFn, res);
      return res;
    }
    public string url(DirItem it) {
      return string.Format("{0};{1}/{2}/{3}", 1 /*(int)schools.dictTypes.ttsUrl*/, lang, it.dirId, it.fileId);
    }
    public string fileName(DirItem it, bool isXml) {
      return string.Format(@"{0}\{1}\{2}.{3}", Path.GetDirectoryName(selfPath), it.dirId, it.fileId, isXml ? "xml" : "mp3");
    }
    string selfPath;
  }

  //Jeden TTS soubor
  public class DirItem {
    [XmlAttribute]
    public string text;
    [XmlAttribute]
    public int dirId;
    [XmlAttribute]
    public int fileId;
    [XmlAttribute]
    public int version; //0..nenahrano, 1..nahrano, 2..zkontrolovano
    [XmlAttribute]
    public string soundedInfo; //je null IFF zvuk neni nahran. Muze obsahovat informaci propName zpusobu nahravky (jmeno TTS mluvciho apod.)
  }

  //Objekt pro prenos nahravaciho requestu a zpet
  public static class ToRecord {
    public static void generateRecordingRequest(Langs lang) {
      Dir dir = Dir.Load(lang, true); 
      string recDir = recordDir(lang);
      if (Directory.Exists(recDir)) Directory.Delete(recDir, true);
      LowUtils.AdjustDir(recDir);
      foreach (var it in dir.Items.Where(it => it.version == 0)) File.WriteAllText(string.Format(@"{0}\{1}-{2}.txt", recDir, it.dirId, it.fileId), it.text);
    }
    public static void acceptRecording(Langs lang, string soundedInfo) {
      Dir dir = Dir.Load(lang, true); 
      dir.Reset();
      string recDir = recordDir(lang);
      foreach (var mp3 in new DirectoryInfo(recDir).GetFiles("*.mp3", SearchOption.TopDirectoryOnly)) {
        var parts = mp3.Name.Split('.').First().Split('-'); int dirId = int.Parse(parts[0]); int fileId = int.Parse(parts[1]);
        var di = dir.Items.FirstOrDefault(i => i.dirId == dirId && i.fileId == fileId);
        if (di == null) continue;
        var destPath = dir.fileName(di, false);
        if (File.Exists(destPath)) File.Delete(destPath);
        File.Move(mp3.FullName, destPath);
        di.soundedInfo = new string[] { soundedInfo, DateTime.Today.ToShortDateString() }.Aggregate((r, i) => r + ";" + i);
        di.version = 1;
        XmlUtils.ObjectToFile(dir.fileName(di, true), di);
      }
    }
    static string recordDir(Langs lang) { return string.Format(@"{0}\{1}", recordBasicPath, lang); }
    const string recordBasicPath = @"d:\\TTS_work";
  }

  //public class Batch {
  //  public string BasicPath;
  //  public string tempDir;
  //  public BatchSound[] ToRecord; //je potreba ozvucit
  //  public BatchSound[] Recorded; //je jiz ozvuceno
  //  public string email;

  //  public void End() {
  //    foreach (var bi in ToRecord) {
  //      string src = string.Format(@"{0}\{1}", tempDir, bi.compId);
  //      if (!File.Exists(src + ".mp3")) continue;
  //      string dest = BasicPath + "\\" + bi.RelPath;
  //      LowUtils.AdjustFileDir(dest);
  //      File.Move(src + ".mp3", dest + ".mp3");
  //      File.Move(src + ".txt", dest + ".txt");
  //    }
  //    Directory.Delete(tempDir, true);
  //  }
  //  public void DeleteUnused() {
  //    foreach (var f in Recorded.Except(ToRecord)) {
  //      try { File.Delete(BasicPath + "\\" + f.RelPath + ".mp3"); }
  //      catch { }
  //      try { File.Delete(BasicPath + "\\" + f.RelPath + ".txt"); }
  //      catch { }
  //    }
  //  }
  //}
  //public class BatchSound : IEqualityComparer<BatchSound> {
  //  public int compId;
  //  public string RelPath;
  //  public string Text;

  //  bool IEqualityComparer<BatchSound>.Equals(BatchSound x, BatchSound y) { return x.Text.Equals(y.Text); }
  //  int IEqualityComparer<BatchSound>.GetHashCode(BatchSound cell) { return cell.Text.GetHashCode(); }
  //}
  //public static class Build {
  //  public static void Start(Batch batch, bool all, string tempDir) {
  //    batch.tempDir = tempDir;
  //    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
  //    Directory.CreateDirectory(tempDir);
  //    int cnt = 0;
  //    foreach (var bi in batch.ToRecord.Where(s => all ? true : !File.Exists(batch.BasicPath + "\\" + s.RelPath))) {
  //      bi.compId = cnt++;
  //      File.WriteAllText(string.Format(@"{0}\{1}.txt", tempDir, bi.compId), bi.Text);
  //    }
  //    XmlUtils.ObjectToFile(tempDir + "\\" + batchFn, batch);
  //  }
  //  public static Batch Load(string tempDir) {
  //    var res2 = XmlUtils.FileToObject<Batch>(tempDir + "\\" + batchFn);
  //    res2.tempDir = tempDir;
  //    return res2;
  //  }
  //  const string batchFn = @"batch.xml";

  //  public static Batch courseTtsSounds(ProductsDefine.productDescrNew prod) {
  //    string bp = (Machines.rwDataSourcePath + @"rew\Web4\RwCourses\" + prod.dataPath.Replace('/', '\\')).ToLower();
  //    var res2 = new Batch() {
  //      BasicPath = bp,
  //      ToRecord = courseTtsSoundsControls(prod).ToArray(),
  //      Recorded = courseTtsSoundsFiles(bp).ToArray()
  //    };
  //    res2.email = prod._productId;
  //    return res2;
  //  }

  //  static IEnumerable<BatchSound> courseTtsSoundsFiles(string basicPath) {
  //    var filesNames = Directory.EnumerateFiles(basicPath, "*.*", SearchOption.AllDirectories).Where(f => ttsMask.IsMatch(f)).Select(f => f.Substring(basicPath.Length + 1)).ToArray();
  //    var fileGroups = filesNames.Select(f => f.ToLower().Split('.')).GroupBy(fe => fe[0]).ToArray();
  //    var files = fileGroups.Where(fes => fes.Select(fe => fe[1]).OrderBy(s => s).SequenceEqual(ttsExts)).Select(fes => fes.Key).ToArray();
  //    foreach (var f in files) yield return new BatchSound() {
  //      Text = File.ReadAllText(basicPath + "\\" + f + ".txt"),
  //      RelPath = f
  //    };
  //  }
  //  static string[] ttsExts = new string[] { "mp3", "txt" };
  //  static Regex ttsMask = new Regex(@"(?i:\\tts\\.*(\.txt|\.mp3))$");

  //  static IEnumerable<BatchSound> courseTtsSoundsControls(ProductsDefine.productDescrNew prod) {
  //    StringBuilder sb = new StringBuilder();
  //    foreach (var testEx in prod.Modules().SelectMany(m => m.exs)) {
  //      var pg = CourseModel.lib.ReadExercise(testEx, sb);
  //      foreach (var ts in CourseModel.lib.Scan(pg).Select(s => s.tag).OfType<CourseModel.TtsSound>()) yield return new BatchSound() {
  //        Text = ts.Text,
  //        RelPath = string.Format(@"{0}\tts\{1}_{2}", testEx.modInfo.pathOrderNum.Substring(prod.dataPath.Length + 1), pg.email, ts.email).Replace('/', '\\'),
  //      };
  //    }
  //  }

  //}
}
