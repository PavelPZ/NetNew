using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.Zip;
using LMNetLib;
namespace LMComLib {
  //xmlns:i="http://www.w3.org/2001/XMLSchema-instance"   
  public static class ZipWrapper {
    public static void ObjectToFile(object inst, string fn) {
      using (Stream str = new FileStream(fn, FileMode.Create))
        Serialize(inst, str);
    }
    public static object FileToObject(Type t, string fn) {
      using (Stream str = new FileStream(fn, FileMode.Open))
        return Deserialize(t, str);
    }
    public static void CompressToFile(object inst, string fn) {
      using (Stream str = new FileStream(fn, FileMode.Create))
        SerializeAndCompress(inst, str);
    }
    public static Stream CompressStream(Stream output) {
      Deflater defl = new ICSharpCode.SharpZipLib.Zip.Compression.Deflater(9, false);
      DeflaterOutputStream res = new DeflaterOutputStream(output, defl);
      res.IsStreamOwner = false; return res;
    }
    public class StreamName {
      public string Path;
      public Stream Str;
    }
    public static IEnumerable<StreamName> DecompressFile(string fn) {
      ZipFile file = new ZipFile(fn);
      return file.Cast<ZipEntry>().Where(en => !en.IsDirectory).Select(en => new StreamName() { Str = file.GetInputStream(en), Path = en.Name });
    }
    public static void Compress(Stream input, Stream output) {
      using (Stream s = CompressStream(output))
        LowUtils.CopyStream(input, s);
    }
    public static byte[] Compress(byte[] input) {
      MemoryStream inStr = new MemoryStream(input);
      MemoryStream outStr = new MemoryStream();
      Compress(inStr, outStr);
      return outStr.ToArray();
    }
    public static void Serialize(object inst, Stream str) {
      DataContractSerializer dcs = createSerializer(inst.GetType());
      dcs.WriteObject(str, inst);
    }
    public static byte[] Serialize(object inst) {
      DataContractSerializer dcs = createSerializer(inst.GetType());
      using (MemoryStream str = new MemoryStream()) {
        dcs.WriteObject(str, inst);
        return str.ToArray();
      }
    }
    public static string SerializeToString(object inst) {
      byte[] data = Serialize(inst);
      return Encoding.UTF8.GetString(data, 0, data.Length);
    }
    public static byte[] Decompress(byte[] data) {
      using (MemoryStream inStr = new MemoryStream(data))
      using (MemoryStream outStr = new MemoryStream()) {
        Decompress(inStr, outStr);
        return outStr.ToArray();
      }
    }
    public static Stream DecompressStream(Stream str) {
      Deflater defl = new Deflater(9, false);
      DeflaterOutputStream res = new DeflaterOutputStream(str, defl);
      res.IsStreamOwner = false; return res;
    }
    public static string DecompressUTF8(byte[] data) {
      byte[] bin = Decompress(data);
      return Encoding.UTF8.GetString(bin, 0, bin.Length);
    }
    public static void SerializeAndCompress(object inst, Stream str) {
      using (Stream s = DecompressStream(str)) Serialize(inst, s);
    }
    public static byte[] SerializeAndCompress(object inst) {
      MemoryStream ms = new MemoryStream(); SerializeAndCompress(inst, ms);
      return ms.ToArray();
    }
    public static object Deserialize(Type t, Stream str) {
      return createSerializer(t).ReadObject(str);
    }
    public static object Deserialize(Type t, byte[] data) {
      return createSerializer(t).ReadObject(new MemoryStream(data));
    }
    public static object Deserialize(Type t, string data) {
      byte[] binData = Encoding.UTF8.GetBytes(data); return Deserialize(t, binData);
    }
    public static void Decompress(Stream input, Stream output) {
      using (Stream s2 = new InflaterInputStream(input))
        LowUtils.CopyStream(s2, output);
    }
    public static object DecompressAndDeserialize(Type t, Stream input) {
      using (Stream s2 = new InflaterInputStream(input)) return Deserialize(t, s2);
    }
    public static T DecompressAndDeserialize<T>(Stream input) {
      using (Stream s2 = new InflaterInputStream(input)) return (T) Deserialize(typeof(T), s2);
    }
    public static object DecompressAndDeserialize(Type t, byte[] data) {
      MemoryStream ms = new MemoryStream(data); using (Stream s2 = new InflaterInputStream(ms))
        return Deserialize(t, s2);
    }
    static DataContractSerializer createSerializer(Type t) {
      return new DataContractSerializer(t);
    }
  }
}
