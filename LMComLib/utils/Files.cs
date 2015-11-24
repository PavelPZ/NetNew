// $Header: /cvsroot/LMCom/lmcomlib/utils/Files.cs,v 1.1 2009/05/07 05:15:13 pavel Exp $

namespace LMNetLib {
  using System;
  using System.IO;
  using System.Collections.Generic;
  using System.Security;
  using System.Collections;
  using System.Collections.Specialized;
  using System.Text.RegularExpressions;

  /// <summary> Souborové utility. </summary>
  public class Files {
    /// <summary> Pøeète obsah souboru do stringu. </summary>
    /// <param name="fileName">Jméno souboru.</param>
    /// <returns>Stringový obsah souboru.</returns>
    public static string FileToString(string fileName) {
      StreamReader sr = File.OpenText(fileName);

      try {
        return sr.ReadToEnd();
      } finally {
        sr.Close();
      }
    }

    /*public static void BytesToFile(byte[] data, string fileName) {
      using (FileStream sr = new FileStream (fileName, FileMode.Create, FileAccess.Write))
        sr.Write(data, 0, data.Length);
    }*/

    /// <summary> Pøeète obsah souboru do stringu. </summary>
    /// <param name="fileName">Jméno souboru.</param>
    /// <param name="coding">Kódování souboru.</param>
    /// <returns>Stringový obsah souboru.</returns>
    public static string FileToString(string fileName, System.Text.Encoding coding) {
      StreamReader fileStream = new StreamReader(fileName, coding);
      try {
        return fileStream.ReadToEnd();
      } finally {
        fileStream.Close();
      }
    }

    /// <summary> Zapíše string do souboru. </summary>
    /// <param name="str">string k zapsání do souboru.</param>
    /// <param name="fileName">Jméno souboru.</param>
    public static void StringToFile(string str, string fileName) {
      StreamWriter sr = File.CreateText(fileName);
      try {
        sr.Write(str);
        sr.Flush();
      } finally {
        sr.Close();
      }
    }

    /// <summary> Nahradí v cestì na soubor jinou základní cestu. </summary>
    /// <param name="fileName"> Pùvodní cesta na soubor. </param>
    /// <param name="basePath"> Nová základní cesta. Musí konèit na zpìtné lomítko. </param>
    /// <param name="constSegments"> Poèet pevných èástí (oddìlené jsou zpìtnými lomítky) cesty ve jménu souboru.
    /// Kladné èíslo znaèí od zaèátkou cesty, záporné od konce. </param>
    /// <returns> Cesta na soubor s novou základní cestou. </returns>
    public static string ReplaceBasePath(string fileName, string basePath, int constSegments) {
      int segmentPos;

      if (constSegments > 0) {
        segmentPos = -1;
        for (; --constSegments >= 0; )
          segmentPos = fileName.IndexOf(Path.DirectorySeparatorChar, segmentPos + 1);
      } else {
        segmentPos = fileName.Length;
        for (; ++constSegments <= 0; )
          segmentPos = fileName.LastIndexOf(Path.DirectorySeparatorChar, segmentPos - 1);
      }

      return basePath + fileName.Substring(segmentPos + 1);
    }
  }

  /// <summary> Urèuje formu/formát vráceného jména souboru ve <see cref="ScanDir.FileName"/>. </summary>
  /// <remarks>
  /// Položky jsem vysvìtleny na souboru c:\pom\file.txt a BasicPath=c:\
  /// </remarks>
  public enum FileNameMode {
    /// <summary> c:\pom\file.txt (platí pro soubor c:\pom\file.txt a BasicPath=c:\). </summary>
    FullPath,
    /// <summary> c:\pom\file (platí pro soubor c:\pom\file.txt a BasicPath=c:\). </summary>
    FullName,
    /// <summary> pom\file.txt (platí pro soubor c:\pom\file.txt a BasicPath=c:\). </summary>
    RelPath,
    /// <summary> pom\file (platí pro soubor c:\pom\file.txt a BasicPath=c:\). </summary>
    RelName,
    /// <summary> file.txt (platí pro soubor c:\pom\file.txt a BasicPath=c:\). </summary>
    Path,
    /// <summary> file (platí pro soubor c:\pom\file.txt a BasicPath=c:\). </summary>
    Name,
    /// <summary> .txt (platí pro soubor c:\pom\file.txt a BasicPath=c:\). </summary>
    Extension,
  }

  /// <summary> Typ <see cref="ScanDir"/> callbacku. </summary>
  public delegate ScanDirCallBackResult ScanDirCallBack(string path, FileSystemInfo info);

  /// <summary> Návratová hodnota <see cref="ScanDirCallBackResult"/>. </summary>
  public enum ScanDirCallBackResult {
    /// <summary>
    /// Použij soubor.
    /// </summary>
    Use,
    /// <summary>
    /// Ignoruj soubor.
    /// </summary>
    Ignore,
    /// <summary>
    /// Ukonèi prohledávání pomocí ScanDirAbortException.
    /// </summary>
    Abort,
  }

  /// <summary> Exception hozená ze <see cref="ScanDir"/> v pøípadì, že <see cref="ScanDir"/>
  /// callback vrátí <see cref="ScanDirCallBackResult.Abort"/>. </summary>
  public class ScanDirAbortException : Exception { }

  /// <summary> Tøída pro získání jmen souborù a adresáøù z file systému. </summary>
  /// <example>
  /// Vypíše plné cesty souborù *.txt a podadresáøù z adresáøe e:\x a jeho pøímých podaresáøù.
  /// <code>
  /// ScanDir sd = new ScanDir();
  ///
  /// sd.BasicPath = @"e:\x\";
  /// sd.ScanDepth = 2;
  /// sd.DirsToResult = true;
  /// sd.FileMask = @"(?i:\.txt)$";
  ///
  /// for ( int i = 0 ; i &lt; sd.Count ; i++ )
  ///   Console.WriteLine( sd.FileName( FileNameMode.FullPath , i ) );
  ///
  /// // Pøíklady zápisù Regex
  /// // prefix*.xml = @"(?i:^prefix.*\.xml)$"
  /// // *.xml = @"(?i:\.txt)$"
  /// </code>
  /// </example>
  public sealed class ScanDir {
    /// <summary> Hloubka pro procházení adresáøù až na dno (celá stromová struktura). </summary>
    public const int c_ScanFullDepth = int.MaxValue;

    private string _FileMask = null;
    /// <summary> Urèuje pomocí .NET regulárního výrazu masku jmen souborù (ne adresáøù!),
    /// které budou pøidány do výsledku. Implicitnì je null. </summary>
    /// <remarks>
    /// Pøíklady regulárních výrazù:
    /// <pre>
    /// null            všechny soubory
    /// .+              všechny soubory
    /// (?i:\.HTM)$     *.htm
    /// ^a              soubory zaèínající 'a'
    /// ^(?i:a)	        soubory zaèínající 'a' nebo 'A'
    /// the             soubory obsahující "the"
    /// </pre></remarks>
    public string FileMask {
      get { return _FileMask; }
      set {
        if (_FileMask != value) {
          Active = false;
          _FileMask = value;
        }
      }
    }

    private string _FileMaskNot = null;
    /// <summary> Urèuje pomocí .NET regulárního výrazu masku jmen souborù (ne adresáøù!),
    /// které NEbudou pøidány do výsledku. Implicitnì je null (pak není žádné omezení).
    /// Syntaxe je stejná jako u <see cref="FileMask"/>. </summary>
    public string FileMaskNot {
      get { return _FileMaskNot; }
      set {
        if (_FileMaskNot != value) {
          Active = false;
          _FileMaskNot = value;
        }
      }
    }

    private int _ScanDepth = c_ScanFullDepth;
    /// <summary> Hloubka vnoøení pøi procházení podadresáøù. 0 znamená pouze jednu root úroveò. c_ScanFullDepth znamená všechny úrovnì.
    /// Implicitnì je c_ScanFullDepth. </summary>
    public int ScanDepth {
      get { return _ScanDepth; }
      set {
        if (_ScanDepth != value) {
          Active = false;
          _ScanDepth = value;
        }
      }
    }

    private bool _KeepCase = true;
    /// <summary> true pokud se má zachovat velikost písmenek souborù,
    /// když false, tak jsou jména souborù pøevedena na malá písmenka.
    /// Implicitnì je true. </summary>
    public bool KeepCase {
      get { return _KeepCase; }
      set {
        if (_KeepCase != value) {
          Active = false;
          _KeepCase = value;
        }
      }
    }

    private bool _FilesToResult = true;
    /// <summary> true pokud mají být soubory pøidány do výsledku. Implicitnì je true. </summary>
    public bool FilesToResult {
      get { return _FilesToResult; }
      set {
        if (_FilesToResult != value) {
          Active = false;
          _FilesToResult = value;
        }
      }
    }

    private bool _DirsToResult = false;
    /// <summary> true pokud mají být adresáøe pøidány do výsledku. Implicitnì je false. </summary>
    /// <remarks>
    /// Do hierarchického výsledku jsou vždy pøidány všechny adresáøe.
    /// </remarks>
    public bool DirsToResult {
      get { return _DirsToResult; }
      set {
        if (_DirsToResult != value) {
          Active = false;
          _DirsToResult = value;
        }
      }
    }

    private string _BasicPath = "." + Path.DirectorySeparatorChar;
    /// <summary> Jméno koøenového adresáøe, ve kterém se soubory procházejí. Musí konèit na '\'. Implicitnì je ".\" </summary>
    public string BasicPath {
      get { return _BasicPath; }
      set {
        if (_BasicPath != value) {
          Active = false;
          _BasicPath = value;
          if (!_BasicPath.EndsWith("\\")) _BasicPath += '\\';
        }
      }
    }

    private ScanDirCallBack _CallBack = null;
    /// <summary> Callback volaný pro každý potencionálnì vkládaný soubor nebo adresáø.
    /// Implicitnì je null (žádný callback). </summary>
    /// <example>
    /// Tento callback zaøídí použití souborù jen mladších než 1 hodina.
    /// <code>
    /// public static ScanDirCallBackResult MyScanDirCallBack ( string path, FileSystemInfo info )
    /// {
    ///   return info.CreationTime &lt; DateTime.UtcNow.AddHours( -1 ) ? ScanDirCallBackResult.Ignore : ScanDirCallBackResult.Use;
    /// }
    /// ...
    /// sd.CallBack = new ScanDirCallBack( MyScanDirCallBack );
    /// </code>
    /// </example>
    public ScanDirCallBack CallBack {
      get { return _CallBack; }
      set {
        if (_CallBack != value) {
          Active = false;
          _CallBack = value;
        }
      }
    }

    private FileDirEntry _Hierarchy = null;
    /// <summary> Výsledek v hierarchické podobì. </summary>
    /// <example>
    /// Následující pøíklad ukazuje jednoduchý výpis hierarchického výsledku.
    /// <code>
    /// static void PrintHier ( ScanDir.FileDirEntry entry, string indent )
    /// {
    ///   foreach( ScanDir.FileDirEntry Item in entry.SubEntries )
    ///     if ( (Item.Entry.Attributes &amp; FileAttributes.Directory) == 0)
    ///       Console.WriteLine( indent + Item.Entry.Name );
    ///     else
    ///     {
    ///       Console.WriteLine( indent + ":" + Item.Entry.Name );
    ///       PrintHier( Item, indent + "  " );
    ///     }
    /// }
    /// ...
    /// PrintHier( scanDir.Hierarchy, "" );
    /// </code>
    /// </example>
    public FileDirEntry Hierarchy {
      get { return _Hierarchy; }
    }

    private StringCollection _ResultList = new StringCollection();
    /// <summary> Výsledek v linearní podobì.  </summary>
    /// <remarks>
    /// Jednotlivé položky jsou jména souborù relativnì k BasePath, adresáøe konèí vždy znakem \.
    /// </remarks>
    public StringCollection ResultList {
      get { Active = true; return _ResultList; }
    }

    /// <summary> Poèet položek v lineárním výsledku. Zpùsobuje internì nastavení Active = true. </summary>
    public int Count {
      get {
        Active = true;
        return _ResultList.Count;
      }
    }

    public IEnumerable<string> FileName(FileNameMode mode) {
      for (int i = 0; i < Count; i++) yield return FileName(mode, i);
    }
    /// <summary> Vrátí položku v lineárním výsledku. Zpùsobuje internì nastavení Active = true. </summary>
    /// <param name="mode">Urèuje formu/formát vráceného jména souboru.</param>
    /// <param name="index">Index položky.</param>
    /// <returns>Položka v lineárním výsledku.</returns>
    public string FileName(FileNameMode mode, int index) {
      Active = true;

      string entry = _ResultList[index];
      switch (mode) {
        case FileNameMode.FullPath:
          return _BasicPath + entry;

        case FileNameMode.FullName:
          return Path.ChangeExtension(_BasicPath + entry, null);

        case FileNameMode.RelPath:
          return entry;

        case FileNameMode.RelName:
          return Path.ChangeExtension(entry, null);

        case FileNameMode.Path:
          return Path.GetFileName(entry);

        case FileNameMode.Name:
          if (entry.EndsWith(@"\"))
            return Path.GetFileName(entry.Substring(0, entry.Length - 1));
          else
            return Path.GetFileNameWithoutExtension(entry);

        case FileNameMode.Extension:
          return Path.GetExtension(entry);

        default:
          throw new Exception(); // Nemìlo by nastat
      }
    }

    /// <summary> Interní metoda pro pøípadnou zmìnu velikosti písmen jména souboru podle KeepCase. </summary>
    private string HandleCase(string path) {
      return _KeepCase ? path : path.ToLower();
    }

    /// <summary> Interní metoda pro zpracování jednoho souboru/adresáøe. </summary>
    /// <returns>false, pokud se má vyvolat <see cref="ScanDirAbortException"/></returns>
    private bool ProcessFile(string item, FileSystemInfo info) {
      if (null == _CallBack) {
        _ResultList.Add(HandleCase(item));
        return true;
      } else {
        switch (_CallBack(item, info)) {
          case ScanDirCallBackResult.Use:
            _ResultList.Add(HandleCase(item));
            return true;

          case ScanDirCallBackResult.Ignore:
            return true;

          case ScanDirCallBackResult.Abort:
            return false;

          default:
            throw new Exception(); // Nemìlo by nastat
        }
      }
    }

    /// <summary> Interní rekurzivní metoda pro procházení adresáøù. </summary>
    private FileDirEntry BrowseInternal(string dir, Regex regEx, Regex regExNot, int depth) {
      DirectoryInfo currentDirectory = new DirectoryInfo(_BasicPath + dir);
      FileDirEntry result = new FileDirEntry(currentDirectory);

      if (depth >= 0) {
        // Soubory
        if (_FilesToResult) {
          try {
            FileInfo[] files = currentDirectory.GetFiles();

            foreach (FileInfo oneFile in files)
              if ((null == regEx || regEx.IsMatch(oneFile.Name.ToLower())) && (null == regExNot || !regExNot.IsMatch(oneFile.Name.ToLower()))) {
                if (!ProcessFile(dir + oneFile.Name, oneFile))
                  throw new ScanDirAbortException();

                result.SubEntries.Add(new FileDirEntry(oneFile));
              }
          } catch (SecurityException) { } catch (UnauthorizedAccessException) { }
        }

        // Adresáøe
        try {
          DirectoryInfo[] directories = currentDirectory.GetDirectories();

          foreach (DirectoryInfo oneDirectory in directories) {
            if (_DirsToResult)
              if (!ProcessFile(dir + oneDirectory.Name + Path.DirectorySeparatorChar, oneDirectory))
                throw new ScanDirAbortException();

            result.SubEntries.Add(BrowseInternal(dir + oneDirectory.Name + Path.DirectorySeparatorChar, regEx, regExNot, depth - 1));
          }
        } catch (SecurityException) { } catch (UnauthorizedAccessException) { }
      }

      return result;
    }

    private bool _Active = false;
    /// <summary> Nastavení do true nastartuje hledání souborù podle ostatních nastavených parametrù.
    /// Nastavení do false (též zpùsobeno zmìnou jakéhokoliv parametru ScanDir) vynuluje výsledky. </summary>
    public bool Active {
      get { return _Active; }
      set {
        if (_Active != value) {
          _Active = value;

          if (!value) {
            _Hierarchy = null;
            _ResultList.Clear();
          } else {
            if (!Directory.Exists(_BasicPath)) return;
            _Hierarchy = BrowseInternal(
              "",
              null == _FileMask ? null : new Regex(_FileMask),
              null == _FileMaskNot ? null : new Regex(_FileMaskNot),
              _ScanDepth);
          }
        }
      }
    }

    /// <summary> Popisuje jednu položku hierarchického výsledku ScanDir,
    /// ukládající kompletní informace o souborech a adresáøích (jména, atributy, èasy, ...) /// </summary>
    public class FileDirEntry {
      /// <summary> </summary>
      public readonly FileSystemInfo Entry;
      /// <summary> </summary>
      public readonly ArrayList SubEntries = new ArrayList();
      /// <summary> </summary>
      public FileDirEntry(FileSystemInfo entry) {
        Entry = entry;
      }
    }
  }
}
