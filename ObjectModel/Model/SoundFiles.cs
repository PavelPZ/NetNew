using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;
using LMComLib;
using LMNetLib;
using Rewise;
using LMMedia;
//using System.Windows.Controls;
using System.Runtime.InteropServices;
//using Saluse.MediaKit.IO;
using System.Xml.Serialization;
using System.IO.Compression;

namespace Design {
  public partial class MultiFile {

    public static XNamespace siteMap = "http://www.sitemaps.org/schemas/sitemap/0.9";

    static string SoundPackData = Machines.basicPath + @"RW2\Server\Data\SoundPack";
    static string SoundData = Machines.basicPath + @"RW2\Server\Data\Sound";
    static string BingData = Machines.basicPath + @"RW2\Server\Data\BingData";
    static string DirData = Machines.basicPath + @"RW2\Server\Data\DirData";
    public static string BookSources = Machines.basicPath + @"Rw\Rewise\App_Data\rewise";

    public static void CPVSiteMap(IEnumerable<Book> bks) {
      //sitemap.xml, index.htm a BingSiteAuth.xml
      string basicUrl = "http://marker.webfreehosting.net";
      var books = bks.Select(bk => new { bk.Title, bk.Name, RelUrl = bk.LineId + "/" + bk.Name + ".htm" }).ToList();
      books.Insert(0, new { Title = "Home", Name = "", RelUrl = "." });
      XElement sitemap = new XElement(siteMap + "urlset", books.Select(bk => new XElement(siteMap + "url",
        new XElement(siteMap + "loc", basicUrl + "/" + bk.RelUrl),
        new XElement(siteMap + "lastmod", DateTime.UtcNow.ToString("yyyy-mm-dd")),
        new XElement(siteMap + "changefreq", "hourly")
      )));
      sitemap.Save(BingData + "\\sitemap.xml");
      //index.htm
      XDocument doc = LowUtils.XHTML("Index",
        null,
        books.SelectMany(bk => new XElement[] { new XElement(CommonLib.html + "a", new XAttribute("href", bk.RelUrl), bk.Title), new XElement(CommonLib.html + "br") }));
      doc.Save(BingData + "\\index.htm");
      //Bing signature
      new XElement("users", new XElement("user", "A2C8A0D3B38CF8D7634B9AC397CDA80F")).Save(BingData + "\\BingSiteAuth.xml");
    }

    public class cloneInfo {
      public string name;
      public Guid src;
      public Guid dest;
      public static cloneInfo[] infos = new cloneInfo[] {
        new cloneInfo() {name="elements", src=new Guid(" 9164b7ec-df85-4e13-b662-cb971c545048"), dest=new Guid("5a347b9f-b7e1-4722-bbdb-14c404bc73a6")},
        new cloneInfo() {name="tangram", src=new Guid(" fef12b43-2b00-4adf-9c8e-5babdca3fe45"), dest=new Guid("49670e02-11af-4ec8-b7ce-15abdef4a725")},
        new cloneInfo() {name="mirada", src=new Guid(" e56530b6-2d5b-4e33-be59-8b996459e8e1"), dest=new Guid("9a7b1c91-e15b-4f63-81e2-4e4d5dded4af")},
        new cloneInfo() {name="espresso", src=new Guid(" 995a494b-241f-4127-b0ab-3cb268cc6d47"), dest=new Guid("f964ffb1-6d99-407d-9e42-02553eacb825")},
        new cloneInfo() {name="facettes", src=new Guid(" 8c84fb25-859b-40c0-bb4e-323ce33e8135"), dest=new Guid("1f610af2-17d4-4df3-ac4d-aeee145abade")}
      };
    }


    //Save knihy plus pro LM Hueber produkty vytvori originalni Hueber klon dle tabulky cloneInfo
    static void CPVSaveAndCloneBookXml(Book bk, string basicBingData, string lineDescrFn, List<Book> folders) {
      bool first = true;
    start:
      string fnBingData = basicBingData + bk.Name;
      LowUtils.AdjustFileDir(fnBingData);
      //HTML header
      bk.ToXHtml().Save(fnBingData + ".htm");
      //ZIP s book content
      using (FileStream fs = new FileStream(fnBingData + ".olix", FileMode.Create))
      using (ZipStream str = new ZipStream(fs))
        str.AddFileToZip(bk.SaveToBytes(), "book.xml", DateTime.UtcNow);
      folders.Add(bk);
      //Clone:
      if (!first) return;
      cloneInfo ci = cloneInfo.infos.FirstOrDefault(c => c.src == bk.BookId); if (ci == null) return;
      //Copy book:
      MemoryStream ms = new MemoryStream(); XmlUtils.ObjectToStream(bk, ms); ms.Seek(0, SeekOrigin.Begin); bk = XmlUtils.StreamToObject<Book>(ms);
      //Refresh book:
      bk.OrigFileName = @"c:\" + ci.name + ".xml";
      string descrFn = lineDescrFn + ci.name + ".xml";
      XElement descr = XElement.Load(lineDescrFn + ci.name + ".xml");
      bk.Title = descr.Element("title").Value;
      bk.HtmlPerex = descr.Element(CommonLib.OLIhtml + "perex").Elements().First();
      bk.BookId = ci.dest;
      var tits = descr.Element("titles").Elements().Select(e => e.Value).ToArray();
      for (int i = 0; i < bk.Folders.Length; i++) bk.Folders[i].Title = tits[i];
      first = false;
      goto start;
    }

    public static void CPVBookXml(IEnumerable<Book> books) {
      using (StreamWriter wr = new StreamWriter(@"c:\temp\sound_pack.txt")) {
        foreach (var line in books.GroupBy(bk => bk.LineId)) {
          List<Book> folders = new List<Book>();
          var g = line.ToArray();
          string lineDescrFn = GroupInfoData + line.Key.ToString() + "_";
          Langs lng = CommonLib.LineIdToLang(line.Key);
          string basicBingData = BingData + "\\" + lng.ToString() + "\\";
          foreach (var grp in line.GroupBy(bk => bk.Group)) {
            string descrFn = lineDescrFn + grp.Key + ".xml";
            XElement descr = File.Exists(descrFn) ? XElement.Load(lineDescrFn + grp.Key + ".xml") : null;
            Book bk;
            if (grp.Count() == 1) {
              bk = grp.First();
              if (descr != null) {
                bk.Title = descr.Element("title").Value;
                bk.HtmlPerex = descr.Element(CommonLib.OLIhtml + "perex").Elements().First();
              } else {
                //DEBUG:
                XElement a = bk.HtmlPerex.Descendants(CommonLib.OLIhtml + "a").Single();
                XAttribute href = a.Attribute("href");
                if (href.Value.IndexOf("/TalkNow/") < 0) throw new Exception();
                href.Value = "http://www.langmaster.com";
              }
            } else {
              Book first = grp.First();
              bk = new Book() {
                Title = descr.Element("title").Value,
                HtmlPerex = descr.Element(CommonLib.OLIhtml + "perex").Elements().First(),
                LineId = first.LineId,
                BookId = first.BookId,
                AdminEMail = first.AdminEMail,
                OrigFileName = @"c:\" + grp.Key + ".xml"
              };
              bk.Folders = grp.Select(b => new Folder() { Folders = b.Folders, Title = descr.Element("titles").Element(b.Name).Value }).ToArray();
              //jednoznacne ocislovani lekci v knize
              int cnt = 1;
              foreach (Lesson les in Folder.Descendants<Lesson>(bk)) les.Id = cnt++;
            }
            if (bk.Licence == Rw.CreativeCommonLic.unknown) bk.Licence = Rw.CreativeCommonLic.cc_by;
            CPVSaveAndCloneBookXml(bk, basicBingData, lineDescrFn, folders);
          }
          foreach (Book b in folders) {
            b.Folders = null;
            b.OrigFileName = Path.GetFileName(b.OrigFileName).Replace(".xml", ".olix");
          }
          LineRoot lineRoot = new LineRoot() { Line = line.Key, Folders = folders.ToArray() };
          XmlUtils.ObjectToFile(basicBingData + "books.xml", lineRoot);
        }
      }
      //sitemap.xml, index.htm a BingSiteAuth.xml
      //CPVPackSiteMap(books, "http://marker.webfreehosting.net");
    }
    static string GroupInfoData = Machines.basicPath + @"Rw\Rewise\App_Data\rewise\Groups\";


    public static void CPVPackBookSound(IEnumerable<Book> books) {
      using (StreamWriter wr = new StreamWriter(@"c:\temp\sound_pack.txt")) {
        foreach (var grp in books.GroupBy(bk => bk.LineId)) {
          Langs lng = CommonLib.LineIdToLang(grp.Key);
          string basicSoundPackData = SoundPackData + "\\" + lng.ToString() + "\\";
          string basicSoundData = SoundData + "\\" + lng.ToString() + "\\";
          //Sound for books
          foreach (Book bk in grp) {
            string name = bk.Name;
            string fnSoundPackData = basicSoundPackData + name;

            var words = bk.Sounds().Select(s => s.Key).Distinct().ToArray(); //vsechny potrebne zvuky
            wr.WriteLine(words.Count().ToString() + "   :" + bk.OrigFileName);
            List<string> notFound = CPVPackBook(words, fnSoundPackData, lng); //zapakuje MP3 soubory a vrati seznam nenalezenych zvuku
          }
        }
      }
    }

    static short fileLen(string path) {
      StringBuilder str = new StringBuilder(128);
      SendString("open \"" + path + "\" type mpegvideo alias MediaFile", null);
      try {
        SendString("set MediaFile time format milliseconds", null);
        SendString("status MediaFile length", str);
        UInt64 Lng = Convert.ToUInt64(str.ToString()) / 100;
        return (short)Lng;
      } finally {
        SendString("close MediaFile", null);
      }
    }
    [DllImport("winmm.dll")]
    static extern int mciSendString(string strCommand, StringBuilder strReturn, int iReturnLength, IntPtr hwndCallback);
    [DllImport("winmm.dll")]
    static extern bool mciGetErrorString(int fdwError, StringBuilder lpszErrorText, int cchErrorText);
    static void SendString(string strCommand, StringBuilder res) {
      int Err = mciSendString(strCommand, res, res == null ? 0 : res.Capacity, IntPtr.Zero); if (Err == 0) return;
      StringBuilder sb = new StringBuilder(128);
      mciGetErrorString(Err, sb, sb.Capacity);
      throw new Exception(sb.ToString());
    }

    //static int fileLen2(string path) {
    //  using (MP3PCMStream str = new MP3PCMStream(path)) {
    //    str.DecodeFrames(2);
    //    float totalMilliseconds = str.LastHeader.GetTotalMilliseconds((int)str.Length);
    //    return (int)Math.Round(totalMilliseconds);
    //  }
    //}

    //public static void CreateSoundDirectory(IEnumerable<Book> books) {
    //  MediaElement el = new MediaElement();
    //  using (StreamWriter wr = new StreamWriter(@"c:\temp\sound_directory.txt")) {
    //    foreach (var grp in books.GroupBy(bk => bk.LineId)) {
    //      Langs lng = CommonLib.LineIdToLang(grp.Key);
    //      string basicDirData = DirData + "\\" + lng.ToString() + "\\";
    //      //All sounded words to Directory
    //      string fnSoundData = basicDirData + "all";
    //      LowUtils.AdjustFileDir(fnSoundData);
    //      var langGroup = DiskSoundFile.GetFiles(Paths.MP3, lng);
    //      using (FileStream dirFs = new FileStream(fnSoundData + Paths.dirExt, FileMode.Create))
    //      using (BinaryWriter dirWr = new BinaryWriter(dirFs)) {
    //        dirWr.Write(Paths.MultiFileVersion);
    //        dirWr.Write((byte)lng); dirWr.Write(true);
    //        foreach (DiskSoundFile file in langGroup.GroupBy(g => g.Word).Select(g => g.Max())) {
    //          int len = 0;
    //          try { len = fileLen2(file.FullPath); } catch (Exception exp) { wr.WriteLine(exp.Message); len = 0; }
    //          dirWr.Write(file.Word); dirWr.Write((byte)file.Id); dirWr.Write(len);
    //        }
    //      }
    //    }
    //  }
    //}

    static List<string> CPVPackBook(IEnumerable<string> words, string fn, Langs lang) {
      List<string> res = new List<string>();
      LowUtils.AdjustFileDir(fn);
      Dictionary<string, bool> included = new Dictionary<string, bool>();
      using (FileStream dirFs = new FileStream(fn + Paths.dirExt, FileMode.Create))
      using (BinaryWriter dirWr = new BinaryWriter(dirFs)) {
        using (FileStream dataFs = new FileStream(fn + Paths.dataExt, FileMode.Create)) {
          dirWr.Write(Paths.MultiFileVersion);
          dirWr.Write((byte)lang); dirWr.Write(false);
          foreach (string w in words) {
            if (included.ContainsKey(w)) continue;
            included.Add(w, true);
            string mp3Fn = Paths.FindMP3File(lang, w);
            if (mp3Fn == null) { res.Add(w); continue; }
            dirWr.Write(w);
            //continue;
            //Data
            int pos = (int)dirFs.Position; dirWr.Write(pos);
            using (FileStream fs = new FileStream(mp3Fn, FileMode.Open)) LowUtils.CopyStream(fs, dataFs);
            dirWr.Write((int)dirFs.Position - pos);
          }
        }
      }
      return res;
    }
  }

  public static class BooksLib {

    //public static LineIds[] AllLines = new LineIds[] { LineIds.English, LineIds.German, LineIds.Spanish, LineIds.Italian, LineIds.French, };

    //public static IEnumerable<LineIds> GetLines(LineIds line) { return line == LineIds.no ? AllLines : new LineIds[] { line }; }

    public static IEnumerable<Book> Books(LineIds line) {
      return BooksFn().Select(f => Book.Load(f)).Where(bk => line == LineIds.no || bk.LineId == line);
    }
    public static IEnumerable<string> BooksFn() {
      ScanDir sd = new ScanDir();
      sd.BasicPath = MultiFile.BookSources;
      sd.DirsToResult = false;
      sd.FileMask = @"(?i:\.xml)$";
      return sd.FileName(FileNameMode.FullPath).Select(f => f.ToLower()).Where(f => f.IndexOf("stopwords.xml") < 0 && f.IndexOf("en_gb.xml") < 0 && f.IndexOf(@"\groups\") < 0);
    }
  }

  public class DiskSoundFile : IComparable<DiskSoundFile> {
    public string Word;
    public string FullPath;
    public Langs Lang;
    public Paths.BasicPathType Ext;
    public SoundSrcId Id;

    public int Prio { get { return Array.IndexOf<SoundSrcId>(Paths.DataSourceOrder, Id); } }

    public static IEnumerable<DiskSoundFile> GetFiles(Paths.BasicPathType[] exts, Langs lang) {
      foreach (var fn in exts.SelectMany(e => fileNames(Paths.SoundBasicPath(e, lang), e))) {
        string[] parts = fn.Item1.Split('\\'); string id = parts[parts.Length - 4].Substring(3);
        yield return new DiskSoundFile() {
          Word = Paths.DecodeFileNameToWord(Path.GetFileNameWithoutExtension(fn.Item1)),
          FullPath = fn.Item1,
          Lang = (Langs)Enum.Parse(typeof(Langs), parts[parts.Length - 5]),
          Id = (SoundSrcId)int.Parse(id),
          Ext = fn.Item2
        };
      }
    }

    static IEnumerable<Tuple<string, Paths.BasicPathType>> fileNames(string basicPath, Paths.BasicPathType ext) {
      ScanDir sd = new ScanDir();
      sd.BasicPath = basicPath;
      sd.DirsToResult = false;
      sd.FileMask = @"(?i:\" + Paths.Ext(ext) + ")$";
      return sd.FileName(FileNameMode.FullPath).Select(f => new Tuple<string, Paths.BasicPathType>(f.ToLower(), ext));
    }

    int IComparable<DiskSoundFile>.CompareTo(DiskSoundFile other) {
      foreach (SoundSrcId id in Paths.DataSourceOrder) {
        if (id == Id && id != other.Id) return 1;
        else if (id != Id && id == other.Id) return -1;
        else if (id == Id && id == other.Id) return 0;
      }
      throw new Exception();
    }
  }
}


