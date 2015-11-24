using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LMComLib;
using LMNetLib;
using Rewise;
using System.Text.RegularExpressions;
#if SILVERLIGHT && !eTestMeManager
using Client;
#endif

namespace LMMedia {
  public static class Paths {

    public const string dirExt = ".dir";
    public const string dataExt = ".data";

    public const int MultiFileVersion = 1;

    public struct Tuple {
      public byte Item1;
      public byte Item2;
    }

    //public enum DataSourceId {
    //  LM = 0,
    //  Lingea = 1,
    //  HowJSay = 2,
    //  EuroTalk_Male = 3,
    //  EuroTalk_Female = 4,
    //  unknown = int.MaxValue - 1,
    //  no = int.MaxValue,
    //}
    //public enum SoundSrcId {
    //  LM = 0,
    //  Lingea = 1,
    //  HowJSay = 2,
    //  EuroTalk_Male= 3,
    //  EuroTalk_Female = 4,
    //  unknown = int.MaxValue-1,
    //  no = int.MaxValue,
    //}

    public static SoundSrcId[] DataSourceOrder = new SoundSrcId[] { SoundSrcId.LM, SoundSrcId.Lingea, SoundSrcId.EuroTalk_Male, SoundSrcId.EuroTalk_Female, SoundSrcId.HowJSay };

    public enum BasicPathType {
      Xml,
      OldMP3,
      MP3,
      Wav,
      Url,
      AppUrl,
      LMComUrl,
      EduAuthorNew,
    }

    public enum UrlType {
      DirData,
      BingData,
      //EduAuthorNew,
      WeekDialogs,
      //Tests,
    }

 #if SILVERLIGHT
    static Paths() {
      configPaths = new Dictionary<BasicPathType, string>();
      foreach (BasicPathType type in new BasicPathType[] { BasicPathType.OldMP3, BasicPathType.MP3, BasicPathType.Wav, BasicPathType.AppUrl, BasicPathType.LMComUrl, BasicPathType.Xml, BasicPathType.EduAuthorNew }) {
        var s = AppConfig.AppSettings["Sound.BasicDir." + type.ToString()];
        if (!string.IsNullOrEmpty(s)) configPaths.Add(type, s);
      }
    }
#endif

    public static string getConfigPath(BasicPathType type) {
      switch (type) {
        case BasicPathType.Url: return getConfigPath(BasicPathType.AppUrl) + "/data";
#if SILVERLIGHT && !eTestMeManager
        case BasicPathType.AppUrl: return SettingModel.appRootUrl();
#endif
        default: return configPaths[type];
      }
    }

    public static string getDataPath() { return getConfigPath(BasicPathType.Url); }

    public static void finishPZCompConfig() {
      //configPaths[BasicPathType.AppUrl] = AppConfig.AppSettings["Sound.BasicDir.AppUrl.pz_acer_2010"];
      configPaths[BasicPathType.LMComUrl] = AppConfig.AppSettings["Sound.BasicDir.LMComUrl.pz_acer_2010"];
    }

    public static Paths.BasicPathType[] MP3 = new Paths.BasicPathType[] { Paths.BasicPathType.MP3 };
    public static Paths.BasicPathType[] Wav = new Paths.BasicPathType[] { Paths.BasicPathType.Wav };
    public static Paths.BasicPathType[] MP3Wav = new Paths.BasicPathType[] { Paths.BasicPathType.MP3, Paths.BasicPathType.Wav };

    public static string Ext(BasicPathType typ) {
      return typ == BasicPathType.MP3 ? ".mp3" : ".wav";
    }

    public static string FindMP3File(Langs lang, string w) {
      //var temp = DataSourceOrder.Select(externaId => EncodeWordToPath(BasicPathType.MP3, externaId, lang, w, ".mp3")).ToArray();
      return DataSourceOrder.Select(id => EncodeWordToPath(BasicPathType.MP3, id, lang, w, ".mp3")).FirstOrDefault(f => File.Exists(f));
    }

    static StringBuilder sb = new StringBuilder();

    public static string AdjustWordPath(string fn) {
      string w = Paths.DecodeFileNameToWord(Path.GetFileNameWithoutExtension(fn)).ToLower();
      string newW = LMComLib.Extensions.soundNormalize(w, sb);
      if (w == newW) return null;
      Tuple dir = WordDir(newW);
      string res = dir.Item1.ToString() + "\\" + dir.Item2.ToString() + "\\" + EncodeWordToFn(w) + Path.GetExtension(fn);
      string[] parts = fn.Split('\\');
      string[] newParts = res.Split('\\');
      for (int i = 1; i <= newParts.Length; i++) parts[parts.Length - i] = newParts[newParts.Length - i];
      return parts.Aggregate((r, i) => r + '\\' + i);
    }

    public static string EncodeWordToPath(BasicPathType basicPath, SoundSrcId srcId, Langs lang, string w, string ext) {
      if (string.IsNullOrEmpty(w)) return w;
      w = LMComLib.Extensions.soundNormalize(w, sb);
      Tuple dir = WordDir(w);
      char slash = basicPath == BasicPathType.Url ? '/' : '\\';
      return SoundBasicPath(basicPath, lang) + slash + "src" + (int)srcId + slash + dir.Item1.ToString() + slash + dir.Item2.ToString() + slash + EncodeWordToFn(w) + ext;
    }

    public static string SoundBasicPath(BasicPathType basicPath, Langs lang) {
      if (lang == Langs.no) return getConfigPath(basicPath);
#if !eTestMeManager
      if (basicPath == BasicPathType.Url)
        return (Machines.isPZComp() ? "http://download.langmaster.cz/data" : getConfigPath(basicPath)) + "/sound/" + lang.ToString();  
      else
#endif
        return getConfigPath(basicPath) + '\\' + lang.ToString();
    }

    public static string GetDataPath(string dataUrl) {
      return getDataPath() + "/" + dataUrl;
    }

    public static string BasicUrl(UrlType type) {
      return getDataPath() + "/" + type.ToString();
    }

    public static string BasicUrl(UrlType type, string url) {
      return BasicUrl(type) + "/" + url;
    }

    public static string BasicUrl(UrlType type, Langs lang) {
      string res = getDataPath() + "/" + type.ToString();
      if (lang != Langs.no) res += "/" + lang.ToString();
      return res;
    }

    public static string DecodeFileNameToWord(string fn) {
      fn = fn.ToLower();
      bool charMode = true; string res = ""; string code = "";
      foreach (char ch in fn.ToCharArray()) {
        if (charMode) {
          if (ch == '(') charMode = false; else res += ch;
        } else {
          if (ch == ')') {
            res += Convert.ToChar(int.Parse(code, System.Globalization.NumberStyles.HexNumber)); code = ""; charMode = true;
          } else
            code += ch;
        }
      }
      return res;
    }

    public static string EncodeWordToFn(string w) {
      w = LMComLib.Extensions.soundNormalize(w, sb);
      bool forceEncode = w == "nul";
      string res = "";
      foreach (char ch in w.ToCharArray()) {
        int chi = Convert.ToInt32(ch);
        if ((!forceEncode && char.IsLetterOrDigit(ch) && chi < 127) || ch == ' ') res += ch;
        else res += "(" + chi.ToString("X").ToLower() + ")";
      }
      return res;
    }

    static Dictionary<BasicPathType, string> configPaths;

    static string OldEncodeWordToFn(string w) {
      string res = "";
      foreach (char ch in w.ToCharArray())
        if (char.IsLetterOrDigit(ch) || ch == ' ') res += ch;
        else res += "(" + Convert.ToInt32(ch).ToString() + ")";
      return res;
    }

    public static string OldDecodeFnToWord(string fn) {
      bool charMode = true; string res = ""; string code = "";
      foreach (char ch in fn.ToCharArray()) {
        if (charMode) {
          if (ch == '(') charMode = false; else res += ch;
        } else {
          if (ch == ')') {
            res += Convert.ToChar(int.Parse(code)); code = ""; charMode = true;
          } else
            code += ch;
        }
      }
      return res;
    }

    static Tuple WordDir(string w) {
      int hash = CommonLib.StringHash(w);
      return new Tuple() { Item1 = (byte)((hash >> 6) & 0x3F), Item2 = (byte)(hash & 0x3F) };
    }

    public struct KeyText { public string Key; public string Text; }

    static Regex rxBracket = new Regex(@"\{.*?\}", RegexOptions.Singleline); //slozene zavorky, další varianta, stanou se kulatou zavorku. Pro zvuk se obsah maze.
    static Regex rxComment = new Regex(@"\(.*?\)", RegexOptions.Singleline); //kulate zavorky, komentář. Pro zvuk se obsah maze.
    static Regex rxOption = new Regex(@"\[.*?\]", RegexOptions.Singleline); //hranate zavorky, option, stanou se mezerou. Pro zvuk se obsah maze.
    static Regex rxSound = new Regex(@"\{%.*?%\}", RegexOptions.Singleline); //sound zavorky, asi poznamka pro mluvicho, maze se pro zvuk i zobrazeni.
    static Regex rxDoubleSpaces = new Regex(@"\s\s+", RegexOptions.Singleline); //zdvojene mezery a crlf a tab se nahradi mezerou

    public static IEnumerable<KeyText> GetSound(string srcText, StringBuilder sb) {
      if (sb == null) sb = new StringBuilder(); else sb.Length = 0;
      string text = rxSound.Replace(srcText.Trim(), ""); //odstran poznamky pro mluvicho
      text = rxDoubleSpaces.Replace(text, " "); //zdvojene mezery
      //slova k ozvuceni
      foreach (string s in text.Split(';')) {
        string snd = rxOption.Replace(s, "").Trim();
        snd = rxComment.Replace(snd, "").Trim();
        snd = rxBracket.Replace(snd, "").Trim();
        string txt = s.Trim().Replace('[', ' ').Replace(']', ' ').Replace('{', '(').Replace('}', ')');
        yield return new KeyText() { Key = LMComLib.Extensions.soundNormalize(snd, sb), Text = txt };
      }
    }

  }
}
