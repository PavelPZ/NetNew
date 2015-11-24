// $Header: /cvsroot/LMCom/lmcomlib/utils/StringUtils.cs,v 1.2 2009/11/29 14:18:35 pavel Exp $
using System;
using System.Globalization;
using System.Collections;
using System.IO;
using System.Text;

namespace LMNetLib {
  public class StringUtils {
    private StringUtils() { }

    static public string ConcatComma(IEnumerator en) {
      StringBuilder sb = new StringBuilder();
      en.Reset();
      while (en.MoveNext()) {
        if (sb.Length != 0) sb.Append(',');
        sb.Append(en.Current.ToString());
      }
      return sb.ToString();
    }

    static public string GetSafeString(string s) {
      if (s == null)
        return "";
      return s;
    }

    static public string GetSafeString(string s, int maxLength) {
      if (s == null)
        return "";
      if (s.Length <= maxLength)
        return s;
      return s.Substring(0, maxLength);
    }

    static public string GetSafeString(string s, string defaultValue) {
      return s == null ? defaultValue : s;
    }

    static public string GetSafeNullableString(string s) {
      if (s == null)
        return null;
      if (s.Length == 0)
        return null;
      return s;
    }

    static public string GetSafeNullableString(string s, int maxLength) {
      if (s == null)
        return null;
      if (s.Length == 0)
        return null;
      if (s.Length <= maxLength)
        return s;
      return s.Substring(0, maxLength);
    }

    static public int GetSafeInt(string s, int defaultValue) {
      if (s == null || s == "")
        return defaultValue;
      try { return int.Parse(s); } catch { return defaultValue; }
    }

    static public Int64 GetSafeInt64(string s, Int64 defaultValue) {
      if (s == null)
        return defaultValue;
      try { return Int64.Parse(s); } catch { return defaultValue; }
    }
    static public bool GetSafeBool(string s, bool defaultValue) {
      if (s == null) return defaultValue;
      try { return bool.Parse(s); } catch { return defaultValue; }
    }

    static public long GetSafeLong(string s, int defaultValue) {
      if (s == null) {
        return defaultValue;
      } else {
        try {
          return long.Parse(s);
        } catch (FormatException) {
          return defaultValue;
        }
      }
    }

    static public object GetSafeEnum(string s, Type enumType, object defaultValue) {
      if (s == null) {
        return defaultValue;
      } else {
        try {
          return Enum.Parse(enumType, s, true);
        } catch (ArgumentException) {
          return defaultValue;
        }
      }
    }

    static public bool IsEmpty(string s) {
      return s == null || s.Length == 0;
    }

    #region Metody proti SQL Injection
    /// <summary> Filter user input for SQL characters. </summary>
    /// <param name="sql"> Input SQL. </param>
    /// <returns> Safe input SQL. </returns>
    public static string GetSafeSqlLiteral(string sql) {
      return sql.Replace("'", "''");
    }
    /// <summary> Filter user input for SQL characters. </summary>
    /// <param name="sql"> Input SQL. </param>
    /// <returns> Safe input SQL. </returns>
    public static string GetSafeSqlFulltextLiteral(string sql) {
      string s = sql;
      s = s.Replace("\"", "\"\"");
      s = s.Replace("'", "''");
      return s;
    }
    /// <summary> Filter user input for SQL LIKE characters. </summary>
    /// <param name="sql"> Input SQL LIKE. </param>
    /// <returns> Safe input SQL LIKE. </returns>
    public static string GetSafeSqlLikeClauseLiteral(string sql) {
      // '  becomes  ''
      // [  becomes  [[]
      // %  becomes  [%]
      // _  becomes  [_]

      string s = sql;
      s = sql.Replace("'", "''");
      s = s.Replace("[", "[[]");
      s = s.Replace("%", "[%]");
      s = s.Replace("_", "[_]");
      return s;
    }
    #endregion

    static public string FileToString(string fileName, Encoding encoding) {
      TextReader reader = new StreamReader(fileName, encoding);
      try {
        return reader.ReadToEnd();
      } finally {
        reader.Close();
      }
    }

    static public string FileToString(string fileName) {
      return FileToString(fileName, Encoding.UTF8);
    }

    static public void StringToFile(string s, string fileName, Encoding encoding) {
      using (TextWriter writer = new StreamWriter(fileName, false, encoding))
        writer.Write(s);
    }

    static public void StringToFileUtf8Signature(string s, string fileName) {
      byte[] data = Encoding.UTF8.GetBytes(s);
      using (FileStream fs = new FileStream(fileName, FileMode.Create)) {
        fs.Write(utf8sign, 0, utf8sign.Length);
        fs.Write(data, 0, data.Length);
      }
    }
    static byte[] utf8sign = new byte[] { 0xEF, 0xBB, 0xBF };

    static public void StringToFile(string s, string fileName) {
      StringToFile(s, fileName, Encoding.UTF8);
    }

    static public void BytesToFile(byte[] buf, string fileName) {
      using (FileStream fs = new FileStream(fileName, FileMode.Create))
        fs.Write(buf, 0, buf.Length);
    }

    static public byte[] FileToBytes(string fileName) {
      using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read)) {
        byte[] res = new byte[fs.Length];
        fs.Read(res, 0, res.Length);
        return res;
      }
    }

    static public void StreamToFile(Stream inpStr, string fileName) {
      byte[] buf = new byte[32000];
      using (Stream outStr = new FileStream(fileName, FileMode.Create)) {
        while (true) {
          int readed = inpStr.Read(buf, 0, buf.Length); if (readed == 0) break;
          outStr.Write(buf, 0, readed);
        }
      }
    }

    static public byte[] StringToBytes(string s, Encoding encoding) {
      return encoding.GetBytes(s);
    }

    static public byte[] StringToBytes(string s) {
      return StringToBytes(s, Encoding.UTF8);
    }
  }
}
