namespace LMNetLib {
  using System;
  using System.IO;
  using System.Text;
  using System.Reflection;
  using System.Collections;
  using System.Collections.Specialized;
  using System.Globalization;
  using System.Xml;
  using System.Xml.Serialization;
  using System.Runtime.Serialization.Formatters.Binary;
  //using System.Runtime.Serialization.Formatters.Soap;

  /// <summary> Core knihovna konverzních funkcí. </summary>
  public class ConvertsCore {

    /// <summary> Obsah souboru pøedeve do base64 stringu. </summary>
    /// <param name="file"> Cesta na soubor k pøevedení. </param>
    /// <returns> Base64 string s obsahem souboru. </returns>
    public static string FileToBase64(string file) {
      FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
      BinaryReader binaryReader = new BinaryReader(fileStream);
      try {
        // HS Soubor nesmí být delší než int.MaxValue
        byte[] fileInBytes = binaryReader.ReadBytes((int)fileStream.Length);
        return Convert.ToBase64String(fileInBytes);
      } finally {
        binaryReader.Close();
      }
    }

    /// <summary> Pøevede string do Base64. </summary>
    /// <param name="str"> String k pøevodu do Base64. </param>
    /// <returns> Base64 podoba vstupmího stringu. </returns>
    public static string StringToBase64(string str) {
      return Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
    }

    /// <summary> Pøevede Base64 na string. </summary>
    /// <param name="base64"> Base64 podoba string. </param>
    /// <returns> Odkódovaný string. </returns>
    public static string Base64ToString(string base64) {
      return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }

    /// <summary> Pomocí .NET BinaryFormatter serializuje objekt do pole bajtù. </summary>
    /// <param name="obj"> Objekt k serializaci. </param>
    /// <returns> Serializovaný objekt. </returns>
    public static byte[] Obj2Bytes(object obj) {
      using (MemoryStream ms = new MemoryStream()) {
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.Serialize(ms, obj);
        return ms.ToArray();
      }
    }

    /// <summary> Pomocí .NET BinaryFormatter serializuje objekt do pole bajtù. </summary>
    /// <param name="obj"> Objekt k serializaci. </param>
    /// <returns> Serializovaný objekt. </returns>
    public static void Obj2File(object obj, string fileName) {
      using (FileStream ms = new FileStream(fileName, FileMode.Create)) {
        BinaryFormatter formatter = new BinaryFormatter();
        formatter.Serialize(ms, obj);
      }
    }

    /// <summary> Z pomocí .NET BinaryFormatter serializovaného objektù udìlá jeho instanci. </summary>
    /// <param name="bytes"> Serializovaný objekt.</param>
    /// <returns> Nová instance objektu. </returns>
    public static object Bytes2Obj(byte[] bytes) {
      using (MemoryStream ms = new MemoryStream(bytes)) {
        BinaryFormatter formatter = new BinaryFormatter();
        return formatter.Deserialize(ms);
      }
    }

    public static object File2Obj(string file) {
      using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read)) {
        BinaryFormatter formatter = new BinaryFormatter();
        return formatter.Deserialize(fileStream);
      }
    }

  }
}
