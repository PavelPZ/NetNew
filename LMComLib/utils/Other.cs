// $Header: /cvsroot/LMCom/lmcomlib/utils/Other.cs,v 1.1 2009/05/07 05:15:13 pavel Exp $

namespace LMNetLib
{
  using System;
  using System.IO;
  using System.Web.Mail;
  using System.Text;
  using System.Threading;
  using System.Reflection;
  using System.Diagnostics;
  using System.Collections;
  using System.Collections.Specialized;
  using System.Runtime.InteropServices;

  /// <summary> Implementuje jinde nezaøazené metody. </summary>
  public class OtherCore
  {
    /// <summary> Nastaví hodnotu fieldu nebo propertry objektu. </summary>
    /// <param name="member"> Popis fieldu nebo property. </param>
    /// <param name="obj"> Objekt. </param>
    /// <param name="value"> Nastavovaná hodnota. </param>
    public static void SetMemberValue ( MemberInfo member, object obj, object value )
    {
      if ( member is FieldInfo )
        ((FieldInfo)member).SetValue( obj, value );
      else if ( member is PropertyInfo )
        ((PropertyInfo)member).SetValue( obj, value, null );
      else
        throw new Exception( string.Format( "OtherCode.SetMemberValue: unsupported member type '{0}', class '{1}', value '{2}'", member.GetType(), obj.GetType(), value.GetType() ) );
    }

    /// <summary> Vrátí hodnotu fieldu nebo propertry objektu. </summary>
    /// <param name="member"> Popis fieldu nebo property. </param>
    /// <param name="obj"> Objekt. </param>
    /// <returns> Hodnota fieldu nebo property. </returns>
    public static object GetMemberValue ( MemberInfo member, object obj )
    {
      if ( member is FieldInfo )
        return ((FieldInfo)member).GetValue( obj );
      else if ( member is PropertyInfo )
        return ((PropertyInfo)member).GetValue( obj, null );
      else
        throw new Exception( string.Format( "OtherCode.GetMemberValue: unsupported member type '{0}', class '{1}'", member.GetType(), obj.GetType() ) );
    }

    /// <summary> Vrátí typ fieldu nebo property objektu. </summary>
    /// <param name="member"> Popis fieldu nebo property. </param>
    /// <returns> Typ fieldu nebo property. </returns>
    public static Type GetMemberType ( MemberInfo member )
    {
      if ( member is FieldInfo )
        return ((FieldInfo)member).FieldType;
      else if ( member is PropertyInfo )
        return ((PropertyInfo)member).PropertyType;
      else
        throw new Exception( string.Format( "OtherCode.GetMemberType: unsupported member type '{0}'", member.GetType() ) );
    }

    /// <summary> Porovná obsah 2 kolekcí. </summary>
    /// <param name="enum1">Enumerátor 1. kolekce.</param>
    /// <param name="enum2">Enumerátor 2. kolekce.</param>
    /// <returns>true pokud jsou kolekce stejné.</returns>
    public static bool EnumerablesAreSame ( IEnumerator enum1, IEnumerator enum2 )
    {
      enum1.Reset();
      enum2.Reset();

      for ( ;; )
      {
        bool next1 = enum1.MoveNext();
        bool next2 = enum2.MoveNext();

        if ( !next1 || !next2 )
          return !next1 && !next2;

        if ( 0 != ((IComparable)enum1.Current).CompareTo( enum2.Current ) )
          return false;
      }
    }

    /// <summary> Vrátí zkrácený BitArray. </summary>
    /// <param name="bits"> Pùvodní BitArray. </param>
    /// <param name="length"> Požadované nová menší délka.</param>
    /// <returns> Zkrácená kopie BitArray. </returns>
    public static BitArray ShortenBitArray ( BitArray bits, int length )
    {
      BitArray result = new BitArray( length );
      for ( int i = length ; --i >= 0 ; )
        result[i] = bits[i];
      return result;
    }

    public static BitArray BitArrayCreate ( Type enumType, params int[] values)
    {
      BitArray res = new BitArray (  Enum.GetValues ( enumType).Length);
      foreach (int i in values)
        res[i] = true;
      return res;
    }
    /// <summary> Zjistí, jestli 2 bitová pole mají spoleèné bity. 
    /// Pole nemusí mít stejnou délku, bity navíc se berou jako shodné. </summary>
    /// <param name="bits1"> 1. bitové pole. </param>
    /// <param name="bits2"> 2. bitové pole. </param>
    /// <returns> true, pokud mají obì pole spoleèné bity. </returns>
    public static bool BitsIntersect ( BitArray bits1, BitArray bits2 )
    {
      return BitsIntersect( bits1, bits2, false );
    }

    /// <summary> Vrátí true, pokud jsou všechny bity BitArray false. </summary>
    /// <param name="bits"> Bitové pole. </param>
    /// <returns> true, pokud jsou všechny bity pole false. </returns>
    public static bool BitArrayAllFalse ( BitArray bits )
    {
      Int32[] array = new Int32[ ( bits.Count + 31 ) /  32 ];
      bits.CopyTo( array, 0 );
      // projeï všechny prvky až na poslední
      for ( int i = array.Length - 1 ; --i >= 0 ; )
        if ( array[i] != 0 )
          return false;
      // zkontroluj poslední prvek, který velmi pravdìpodobnì ukládá "necelý" 32 poèet bitù
      int countMod32 = bits.Count % 32;
      return 0 == ( array[ array.Length - 1] & ~( countMod32 == 0 ? 0 : ~1 << ( countMod32 - 1 ) ) );
    }

    /// <summary> Zjistí, jestli 2 bitová pole mají spoleèné bity. </summary>
    /// <param name="bits1"> 1. bitové pole. </param>
    /// <param name="bits2"> 2. bitové pole. </param>
    /// <param name="samesize"> true pokud musí být obì pole stejné délky. </param>
    /// <returns> true, pokud mají obì pole spoleèné bity. </returns>
    public static bool BitsIntersect ( BitArray bits1, BitArray bits2, bool samesize )
    {
      if ( bits1 != null && bits2 != null )
      {
        BitArray intersection;
        if ( bits1.Length == bits2.Length )
          intersection = ((BitArray)bits1.Clone()).And( bits2 );
        else
        {
          if ( samesize )
            throw new Exception( string.Format( "Other.BitsIntersect: BitArrays nemají stejnou velikost {0} <> {1}", bits1.Length, bits2.Length ) );

          BitArray bits1Temp, bits2Temp;

          if ( bits1.Length > bits2.Length )
          {
            bits1Temp = ShortenBitArray( bits1, bits2.Length );
            bits2Temp = bits2;
          }
          else
          {
            bits1Temp = (BitArray)bits1.Clone();
            bits2Temp = ShortenBitArray( bits2, bits1.Length );
          }

          intersection = bits1Temp.And( bits2Temp );
        }

        for ( int i = intersection.Length ; --i >= 0 ; )
          if ( intersection[i] )
            return true;
      }

      return false;
    }

    /// <summary> Zjistí, jestli stejnì dlouhé BitArrays mají nastavené stejné bity. </summary>
    /// <param name="bits1"> 1. BitArray. </param>
    /// <param name="bits2"> 2. BitArray. </param>
    /// <returns> true, když mají nastavené stejné bity. </returns>
    public static bool BitArraysEqual ( BitArray bits1, BitArray bits2 )
    {
      if ( bits1.Count != bits2.Count )
        throw new Exception( string.Format( "OtherCore.BitArraysEqual: different BitArray sizes {0} != {1}", bits1.Count != bits2.Count ) );

      return EnumerablesAreSame( bits1.GetEnumerator(), bits2.GetEnumerator() );
//
//      int[] array1 = new int[ ( bits1.Count + 31 ) / 32 ];
//      bits1.CopyTo( array1, 0 );
//      int[] array2 = new int[ array1.Length ];
//      bits2.CopyTo( array2, 0 );
//
//      for ( int index = array1.Length ; --index >= 0 ; )
//        if ( array1[ index ] != array2[ index ] )
//          return false;
//
//      return true;
    }

    /// <summary> Založí StringCollection z pole stringù. </summary>
    /// <param name="strings"> Pole stringù. </param>
    /// <returns> StringCollection vzniklá z pole stringù. </returns>
    public static StringCollection StringCollection ( params string[] strings )
    {
      StringCollection result = new StringCollection();
      if ( strings != null )
        foreach( string str in strings )
          result.Add( str );
      return result;
    }

    /// <summary> Obsahuje li cesta mezery, tak ji obalí uvozovkami. </summary>
    /// <param name="path"> Cesta na napø. soubor. </param>
    /// <returns> Upravená cesta. </returns>
    public static string QuotePath ( string path )
    {
      return -1 == path.IndexOf( ' ' ) ? path : "\"" + path + "\"";
    }

    /// <summary> Za null string vrátí jeho zadanou náhradu. </summary>
    /// <param name="value"> Vstupní string. </param>
    /// <param name="replacement"> Náhrada za null stringy. </param>
    /// <returns> Normalizovaný string. </returns>
    public static string UnNullString ( string value, string replacement )
    {
      return value != null ? value : replacement;
    }

    /// <summary> Z null stringù vrátí prázdný string. </summary>
    /// <param name="value"> Vstupní string. </param>
    /// <returns> Non-null string. </returns>
    public static string UnNullString ( string value )
    {
      return UnNullString( value, "" );
    }

    /// <summary> Spustí synchronnì externí proces nebo dokument v normálním oknì. </summary>
    /// <param name="executable">Cesta k souboru, který se má spustit nebo otevøít.</param>
    /// <param name="arguments">Argumenty.</param>
    /// <returns>Objekt typu Process popisující spuštìný proces.</returns>
    public static Process LaunchProcess ( string executable, string arguments )
    {
      bool dummy;

      return LaunchProcess( executable, arguments, true, ProcessWindowStyle.Normal, ProcessPriorityClass.Normal, true, false, false, false, Int32.MaxValue, out dummy );
    }

    /// <summary> Spustí synchronnì externí proces nebo dokument v normálním oknì. </summary>
    /// <param name="executable">Cesta k souboru, který se má spustit nebo otevøít.</param>
    /// <param name="arguments">Argumenty.</param>
    /// <param name="priorityClass">Priorita procesu.</param>
    /// <returns>Objekt typu Process popisující spuštìný proces.</returns>
    public static Process LaunchProcess ( string executable, string arguments, ProcessPriorityClass priorityClass )
    {
      bool dummy;

      return LaunchProcess( executable, arguments, true, ProcessWindowStyle.Normal, priorityClass, true, false, false, false, Int32.MaxValue, out dummy );
    }

    /// <summary> Spustí externí proces nebo dokument v normálním oknì. </summary>
    /// <param name="executable">Cesta k souboru, který se má spustit nebo otevøít.</param>
    /// <param name="arguments">Argumenty.</param>
    /// <param name="synchronous">true, pokud se má èeka na ukonèení procesu.</param>
    /// <returns>Objekt typu Process popisující spuštìný proces.</returns>
    public static Process LaunchProcess ( string executable, string arguments, bool synchronous )
    {
      bool dummy;

      return LaunchProcess( executable, arguments, synchronous, ProcessWindowStyle.Normal, ProcessPriorityClass.Normal, true, false, false, false, Int32.MaxValue, out dummy );
    }

    /// <summary> Spustí externí proces. </summary>
    /// <param name="executable">Cesta k souboru, který se má spustit nebo otevøít.</param>
    /// <param name="arguments">Argumeny.</param>
    /// <param name="synchronous">true, pokud se má èeka na ukonèení procesu.</param>
    /// <param name="windowStyle">Styl okna procesu.</param>
    /// <param name="priorityClass">Priorita procesu.</param>
    /// <param name="useShellExecute">false, pokud se má process spustit pøímo.</param>
    /// <param name="redirectStdOut">true, pokud se má výstup pøesmìrovat do Process.StandardOutput.</param>
    /// <param name="redirectStdErr">true, pokud se má error výstup pøesmìrovat do Process.StandardError.</param>
    /// <param name="redirectStdInput">true, pokud se má vstup pøesmìrovat do Process.StandardInput.</param>
    /// <param name="timeout">Pøi synchronous true urèuje timout procesu, Int32.MaxValue znamená bez timeoutu.</param>
    /// <param name="wasTimeout">true, pokud procesu vypršel timeout.</param>
    /// <returns>Objekt typu Process popisující spuštìný proces.</returns>
    public static Process LaunchProcess ( string executable, string arguments, bool synchronous,
      ProcessWindowStyle windowStyle, ProcessPriorityClass priorityClass,
      bool useShellExecute, bool redirectStdOut, bool redirectStdErr, bool redirectStdInput, int timeout, out bool wasTimeout )
    {
      Process prc = new Process();
      prc.StartInfo = new ProcessStartInfo( executable, arguments );
      prc.StartInfo.WindowStyle = windowStyle;
      prc.StartInfo.CreateNoWindow = windowStyle == ProcessWindowStyle.Hidden;
      prc.StartInfo.UseShellExecute = useShellExecute;
      prc.StartInfo.RedirectStandardOutput = redirectStdOut;
      prc.StartInfo.RedirectStandardError = redirectStdErr;
      prc.StartInfo.RedirectStandardInput = redirectStdInput;
      prc.Start();
      prc.PriorityClass = priorityClass;
      if ( synchronous )
      {
        if ( timeout == Int32.MaxValue )
        {
          prc.WaitForExit();
          wasTimeout = false;
        }
        else
          wasTimeout = !prc.WaitForExit( timeout );
      }
      else
        wasTimeout = false;

      return prc;
    }

    private static DateTime assmVerLimitMin = new DateTime( 2000, 1, 1 );
    private static DateTime assmVerLimitMax = DateTime.UtcNow.AddMonths( 1 );

    /// <summary> Vytvoøí string s versí zadaného assembly. Pøedpokládá, že assembly je versováno atributem ve
    /// tvaru [assembly: AssemblyVersion("major.minor.*")]. Výsledkem je string major.minor.revision.build date time </summary>
    /// <param name="assembly"> Assembly. </param>
    /// <returns> Verse assembly. </returns>
    public static string GetAssemblyVersion ( Assembly assembly )
    {
      Version ver = assembly.GetName().Version;
      string result = string.Format( "{0} {1}.{2}.{3}.{4}", assembly.GetName().Name, ver.Major, ver.Minor, ver.Build, ver.Revision );
      if ( ver.Revision != 0 )
      {
        DateTime dt = assmVerLimitMin.AddDays( ver.Build ).AddSeconds( ver.Revision * 2 );
        if ( dt < assmVerLimitMax )
          result += dt.ToString( " dd.MM.yyyy HH:mm:ss" );
      }
      return result;
    }

    /// <summary> Do výsledku zapíše na jednotlivé øádky verse použitých assemblies. </summary>
    /// <param name="lineDelim"> Oddìlovaè øádkù. </param>
    /// <returns>Verse assemblies. </returns>
    public static string GetAssembliesVersion ( string lineDelim )
    {
      ArrayList list = new ArrayList();
      foreach ( Assembly assembly in AppDomain.CurrentDomain.GetAssemblies() )
        list.Add( GetAssemblyVersion( assembly ) );
      list.Sort();
      StringBuilder result = new StringBuilder();
      foreach ( string assmStr in list )
      {
        if ( result.Length != 0 )
          result.Append( lineDelim );
        result.Append( assmStr );
      }
      return result.ToString();
    }

    /// <summary> Do výsledku zapíše na jednotlivé øádky verse použitých assemblies. </summary>
    /// <returns>Verse assemblies. </returns>
    public static string GetAssembliesVersion ()
    {
      return GetAssembliesVersion( "\r\n" );
    }

    /// <summary>Replaces backslashes by slashes.</summary>
    /// <param name="input">Input string.</param>
    /// <returns>Returns original string with replaced backslashes by slashes.</returns>
    public static string EnsureSlashes ( string input )
    {
      return input.Replace( '\\', '/' );
    }

    /// <summary> Replaces slashes by backslashes.</summary>
    /// <param name="Input">Input string.</param>
    /// <returns>Returns original string with placed slashes by backslashes.</returns>
    public static string EnsureBackSlashes ( string Input )
    {
      return Input.Replace( '/', '\\' );
    }
  }

  /*
  /// <summary> Tøída pro Emailing. </summary>
  public class Email
  {
    /// <summary> Odešle SMTP email. Vyžaduje nainstalovaný IIS. </summary>
    /// <param name="from"> Odesílatel. </param>
    /// <param name="to"> Pøíjemce. </param>
    /// <param name="cc"> Kopie. </param>
    /// <param name="subject"> Pøedmìt. </param>
    /// <param name="message"> Zpráva. </param>
    /// <param name="attachments"> Pøílohy. </param>
    public static void Send ( string from, string to, string cc, string subject, string message, IList attachments )
    {
      Send( from, to, cc, null, subject, message, attachments, null );
    }

    /// <summary> Odešle SMTP email. </summary>
    /// <param name="from"> Odesílatel. </param>
    /// <param name="to"> Pøíjemce. </param>
    /// <param name="cc"> Kopie. </param>
    /// <param name="subject"> Pøedmìt. </param>
    /// <param name="message"> Zpráva. </param>
    /// <param name="attachments"> Pøílohy. </param>
    /// <param name="smtpServer"> SMTP server. </param>
    public static void Send ( string from, string to, string cc, string subject, string message, IList attachments, string smtpServer )
    {
      Send( from, to, cc, null, subject, message, attachments, smtpServer );
    }

    /// <summary> Odešle SMTP email. </summary>
    /// <param name="from"> Odesílatel. </param>
    /// <param name="to"> Pøíjemce. </param>
    /// <param name="cc"> Kopie. </param>
    /// <param name="bcc"> Slepá kopie. </param>
    /// <param name="subject"> Pøedmìt. </param>
    /// <param name="message"> Zpráva. </param>
    /// <param name="attachments"> Pøílohy. </param>
    /// <param name="smtpServer"> SMTP server. </param>
    public static void Send ( string from, string to, string cc, string bcc, string subject, string message, IList attachments, string smtpServer )
    {
      try
      {
        MailMessage msg = new MailMessage();
        msg.From = from;
        msg.To = to;
        msg.Cc = cc;
        msg.Bcc = bcc;
        msg.Subject = subject;
        msg.Body = message;
        msg.BodyFormat = MailFormat.Text;
        if ( attachments != null )
          foreach ( object attachment in attachments )
            msg.Attachments.Add( attachment );
      
        if ( smtpServer != null && smtpServer.Length != 0 )
          SmtpMail.SmtpServer = smtpServer;

        SmtpMail.Send( msg );
      }
      catch ( Exception e )
      {
        throw new Exception( string.Format( "Email.Send: from '{0}', to '{1}', cc '{2}', subject '{3}', message '{4}', server '{5}', exception '{6}'", 
          from, to, cc, subject, message, smtpServer, e.ToString() ) );
      }
    }

    /// <summary> Odešle SMTP email. </summary>
    /// <param name="from"> Odesílatel. </param>
    /// <param name="to"> Pøíjemce. </param>
    /// <param name="cc"> Kopie. </param>
    /// <param name="bcc"> Slepá kopie. </param>
    /// <param name="subject"> Pøedmìt. </param>
    /// <param name="message"> Zpráva. </param>
    /// <param name="attachments"> Pøílohy. </param>
    /// <param name="smtpServer"> SMTP server. </param>
    /// <param name="mailFormat"> Formát textu. </param>
    public static void Send ( string from, string to, string cc, string bcc, string subject, string message, IList attachments, string smtpServer, MailFormat mailFormat )
    {
      try
      {
        MailMessage msg = new MailMessage();
        msg.From = from;
        msg.To = to;
        msg.Cc = cc;
        msg.Bcc = bcc;
        msg.Subject = subject;
        msg.Body = message;
        msg.BodyFormat = mailFormat;
        if ( mailFormat == MailFormat.Html )
          msg.BodyEncoding = Encoding.UTF8;
        if ( attachments != null )
          foreach ( object attachment in attachments )
            msg.Attachments.Add( attachment );
      
        if ( smtpServer != null && smtpServer.Length != 0 )
          SmtpMail.SmtpServer = smtpServer;

        SmtpMail.Send( msg );
      }
      catch ( Exception e )
      {
        throw new Exception( string.Format( "Email.Send: from '{0}', to '{1}', cc '{2}', subject '{3}', message '{4}', server '{5}', exception '{6}'", 
          from, to, cc, subject, message, smtpServer, e.ToString() ) );
      }
    }

    /// <summary> Vytvoøí v TEMP doèasný soubor (Email pøílohu) daného jména a textového obsahu. </summary>
    /// <param name="fileName"> Jméno souboru. </param>
    /// <param name="content"> Obsah souboru. </param>
    /// <returns> Pøiloha pro Email. </returns>
    public static MailAttachment CreateTextAttachment ( string fileName, string content )
    {
      string tempDir = Path.GetTempFileName();
      File.Delete( tempDir );
      Directory.CreateDirectory( tempDir );
      string fullPath = tempDir + Path.DirectorySeparatorChar + fileName;
      TextWriter file = new StreamWriter( fullPath, false, Encoding.Default );
      try
      {
        file.Write( content );
      }
      finally
      {
        file.Close();
      }
      return new MailAttachment( fullPath );
    }

    /// <summary> Smaže doèasnou Email pøílohu vyrobenou pomocí <see cref="CreateTextAttachment"/>. </summary>
    /// <param name="attachment"> Doèasná pøíloha. </param>
    public static void DeleteTextAttachment ( MailAttachment attachment )
    {
      File.Delete( attachment.Filename );
      Directory.Delete( Path.GetDirectoryName( attachment.Filename ), false );
    }
  }
   * */
  
  /// <summary> Specifické Windows metody pro klávesnici. </summary>
  public class Keyboard
  {
    /// <summary> The GetAsyncKeyState function determines whether a key is up or down at the time the function is called, 
    /// and whether the key was pressed after a previous call to GetAsyncKeyState.  </summary>
    /// <param name="virtualKey"> Specifies a virtual key. </param>
    [DllImport("User32.dll")]
    public static extern Int16 GetAsyncKeyState ( Int32 virtualKey );

    /// <summary>The GetKeyState function retrieves the status of the specified virtual key. 
    /// The status specifies whether the key is up, down, or toggled (on, off—alternating each time the key is pressed). </summary>
    /// <param name="virtualKey"> Specifies a virtual key. </param>
    [DllImport("User32.dll")]
    public static extern Int16 GetKeyState ( Int32 virtualKey );

    /// <summary> The key is down; otherwise, it is up. </summary>
    public const Int16 KeyPressed = -32768;
    /// <summary> The key is toggled (GetKeyState) or changed state from last call (GetAsyncKeyState). </summary>
    public const Int16 KeyToggledOrChanged = 1;

    // Virtual key codes.
    public const Int32 VK_SHIFT     = 0x10;
    public const Int32 VK_CONTROL   = 0x11;
    public const Int32 VK_MENU      = 0x12;
    public const Int32 VK_LBUTTON   = 0x01;
    public const Int32 VK_RBUTTON   = 0x02;
    public const Int32 VK_CANCEL    = 0x03;
    public const Int32 VK_MBUTTON   = 0x04;
    public const Int32 VK_LWIN      = 0x5b;
    public const Int32 VK_RWIN      = 0x5c;
    public const Int32 VK_LSHIFT    = 0xa0;
    public const Int32 VK_RSHIFT    = 0xa1;
    public const Int32 VK_LCONTROL  = 0xa2;
    public const Int32 VK_RCONTROL  = 0xa3;
    public const Int32 VK_LMENU     = 0xa4;
    public const Int32 VK_RMENU     = 0xa5;
  }

  /// <summary> Rùzné normalizaèní metody. </summary>
  public class Normalize
  {
    /// <summary> Normalizuje url. </summary>
    /// <remarks> Aby bylo možné porovnat dvì url, vypoèítávat HashUrl apod. </remarks>
    /// <param name="url">url</param>
    /// <returns>Normalizovaný tvar url.</returns>
    public static string Url(string url)
    {
      return url == null ? null : url.ToLower();
    }

    /// <summary> Obalí zástupné znaky v Url hranatými závorkami tak, aby Url bylo použitelné v klauzuli LIKE SQL dotazu. </summary>
    /// <remarks> Vrací normalizovaný tvar url, tj. používá internì metodu Url. </remarks>
    /// <param name="url"> Vstupní Url. </param>
    /// <returns>Upravené Url.</returns>
    public static string UrlForLike(string url)
    {
      string newUrl = Url( url );
      if ( newUrl != null )
        return null;//SQLUtils.SafeSqlLikeClauseLiteral( newUrl );
      else
        return newUrl;
    }
  }

  /*
  /// <summary> String (resp. obal nad ním), který v sobì dokáže uchovat svoje rùzné lokalizace. </summary>
  public class LocalizedString
  {
    /// <summary>
    /// Založí lokalizovaný string ze stringu - kontaineru, který obsahuje všechny lokalizované verse. 
    /// Implicitní jazyk je èeština.
    /// </summary>
    /// <param name="container"> Kontejner, tj. string, který obsahuje lokalizované verse.</param>
    public LocalizedString ( string container ) : this( container, "CSY" ) {}

    /// <summary> Založí lokalizovaný string ze stringu - kontaineru, který obsahuje všechny lokalizované verse.  </summary>
    /// <param name="container"> Kontejner, tj. string, který obsahuje lokalizované verse.</param>
    /// <param name="defaultLanguage"> Implicitní jazyk, pokud string není lokalizován. Zadán pomocí LCID. </param>
    public LocalizedString ( string container, int defaultLanguage ) : this ( container, ConvertsCore.LCID2ThreeLetterWindows( defaultLanguage ) ) {}

    /// <summary> Založí lokalizovaný string ze stringu - kontaineru, který obsahuje všechny lokalizované verse.  </summary>
    /// <param name="container"> Kontejner, tj. string, který obsahuje lokalizované verse.</param>
    /// <param name="defaultLanguage"> Implicitní jazyk, pokud string není lokalizován. Zadán pomocí 3písmenné zkratky (napø. ENU). </param>
    public LocalizedString ( string container, string defaultLanguage )
    {
      this.defaultLanguage = defaultLanguage;

      if ( container.Length == 0 || container[0] != signature)
      {
        strings = new StringDictionary();
        strings.Add( defaultLanguage, container );
      }
      else
        strings = CommaText.CommaTextToStringDictionary( container.Substring( signatureInString.Length ) );
    }

    /// <summary> Znaèka pro oznaèení stringového kontejneru, který obsahuje lokalizované stringy. </summary>
    private const char signature = '~';
    /// <summary> Znaèka pro oznaèení stringového kontejneru, který obsahuje lokalizované stringy. </summary>
    private const string signatureInString = "~";
    /// <summary> Uschovaný implicitní jazyk. </summary>
    private readonly string defaultLanguage;
    /// <summary> Kolekce párù jazyk-string. </summary>
    private readonly StringDictionary strings;

    /// <summary> Pøístup ke kolekci párù pro listing. </summary>
    public StringDictionary Strings
    {
      get
      {
        return strings;
      }
    }

    /// <summary> Indexer pro pøístup ke stringu daného jazyka. Jazyk zadán pomocí LCID. </summary>
    public string this[ int language ]
    {
      get
      {
        return (string)strings[ ConvertsCore.LCID2ThreeLetterWindows( language ) ];
      }
      set
      {
        strings[ ConvertsCore.LCID2ThreeLetterWindows( language ) ] = value;
      }
    }

    /// <summary> Indexer pro pøístup ke stringu daného jazyka. Jazyk zadán pomocí 3písmenné zkratky, napø. ENU. </summary>
    public string this[ string language ]
    {
      get
      {
        return (string)strings[ language ];
      }
      set
      {
        strings[ language ] = value;
      }
    }

    /// <summary> Vyjme z kolekce pøeložených stringù string daného jazyka. </summary>
    /// <param name="language"> Jazyk zadaný jako 3písmenná zkratka (napø. ENU). </param>
    public void Remove ( string language )
    {
      strings.Remove( language );
    }

    /// <summary> Vyjme z kolekce pøeložených stringù string daného jazyka. </summary>
    /// <param name="language"> Jazyk zadaný jako LCID. </param>
    public void Remove ( int language )
    {
      strings.Remove( ConvertsCore.LCID2ThreeLetterWindows( language ) );
    }

    /// <summary> Vrátí obsah stringového kontejneru obsahující všechny lokalizované stringy. </summary>
    /// <returns> Obsah stringového kontejneru obsahující všechny lokalizované stringy. </returns>
    public override string ToString ()
    {
      if ( strings.Count == 1 && strings.ContainsKey( defaultLanguage ) )
        return strings[ defaultLanguage ];
      else
        return signatureInString + CommaText.StringDictionaryToCommaText( strings );
    }
  }
   * */
}