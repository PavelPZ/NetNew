using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
#if !SILVERLIGHT
using System.Xml.Linq;
#endif

using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression;

using LMNetLib;

namespace LMComLib {

  public class ZipOutStream : IDisposable {

    ZipInputStream zipIn;
    byte[] buf = new byte[1024 * 64];
    public ZipOutStream(Stream str) {
      zipIn = new ZipInputStream(str);
    }
    public IEnumerable<ZipEntry> Files() {
      ZipEntry res;
      while ((res = zipIn.GetNextEntry()) != null) if (res == null) yield break; else  yield return res;
    }
    public void Dispose() {
      zipIn.Close();
    }
    public void Decompress(ZipEntry file, Stream str) {
      int size;
      while (true) {
        size = zipIn.Read(buf, 0, buf.Length); if (size <= 0) break;
        str.Write(buf, 0, (int)size);
      }
    }
    public delegate Stream createStream (string fn);

    public void Decompress(string outDir, createStream doCreate) {
      if (!outDir.EndsWith("\\")) outDir += "\\";
      foreach (ZipEntry file in Files()) {
        string outFn = outDir + file.Name;
        //LowUtils.AdjustFileDir(outFn);
        //using (FileStream fs = new FileStream(outFn, FileMode.Create, FileAccess.Write)) Decompress(file, fs);
        using (Stream fs = doCreate(outFn)) Decompress(file, fs);
      }
    }
    public void Decompress(createStream createStream) {
      foreach (ZipEntry file in Files())
        using (var str = createStream(file.Name))
          Decompress(file, str);
    }
  }

  //http://msdn.microsoft.com/en-us/magazine/cc164129.aspx
  public class ZipStream : IDisposable {

    ZipOutputStream zip;

    public ZipStream(Stream stream, bool isStreamOwner = true) {
      zip = new ZipOutputStream(stream);
      zip.IsStreamOwner = isStreamOwner;
      //zip.SetLevel(Deflater.DEFAULT_COMPRESSION);
    }

    public void Dispose() {
      zip.Close();
    }

    public static long Crc(byte[] data) {
      Crc32 crc = new Crc32();
      crc.Reset();
      crc.Update(data);
      return crc.Value;
    }

    public void AddFileToZip(byte[] fileBytes, string filePath, DateTime fileDateTime) {
      ZipEntry entry = new ZipEntry(filePath);
      entry.DateTime = fileDateTime;
      entry.Size = fileBytes.Length;

      entry.Crc = Crc(fileBytes);

      zip.PutNextEntry(entry);

      zip.Write(fileBytes, 0, fileBytes.Length);
    }

    public struct PatchInfo {
      public int Size;
      public long HeaderPatchPos;
      public long DataPos;
      public string FileName;
    }

    public void AddFileToZip(Stream fileStream, string filePath, DateTime fileDateTime) {
      byte[] buffer = new byte[fileStream.Length];
      fileStream.Read(buffer, 0, buffer.Length);
      AddFileToZip(buffer, filePath, fileDateTime);
    }

    public void AddFileToZip(string fileName, string filePath, DateTime? fileDateTime = null) {
      using (FileStream fileStream = File.OpenRead(fileName)) {
        AddFileToZip(fileStream, filePath, fileDateTime == null ? File.GetLastWriteTime(fileName) : (DateTime)fileDateTime);
      }
    }
#if !SILVERLIGHT
    /*********************************************************************************/
    /*                 PATCH jednoho souboru v ZIP                                   */
    /*********************************************************************************/

    //public void AddPatchableFileToZip(string fileName, byte[] fileBytes, string filePath) {
    //  PatchInfo res = AddPatchableFileToZip (fileBytes, filePath);
    //  new XElement("patch",
    //    new XElement("Size", res.Size.ToString()),
    //    new XElement("HeaderPatchPos", res.HeaderPatchPos.ToString()),
    //    new XElement("DataPos", res.DataPos.ToString()),
    //    new XElement("FileName", res.FileName)
    //    ).Save(fileName += ".patch");
    //}

    //public PatchInfo AddPatchableFileToZip(byte[] fileBytes, string filePath) {
    //  ZipEntry entry = new ZipEntry(filePath);
    //  PatchInfo res = new PatchInfo() {FileName = filePath};
    //  entry.DateTime = DateTime.UtcNow;
    //  entry.Size = res.Size = fileBytes.Length;
    //  entry.CompressionMethod = CompressionMethod.Stored;

    //  zip.PutNextEntry(entry);

    //  res.HeaderPatchPos = zip.headerPatchPos;
    //  //zip.patchEntryHeader = false;
    //  res.DataPos = zip.baseOutputStream.Position;
    //  zip.Write(fileBytes, 0, fileBytes.Length);
    //  return res;
    //}

    static PatchInfo infoFromXml(string fileName) {
      XElement el = XElement.Load(fileName + ".patch");
      PatchInfo res = new PatchInfo();
      res.Size = int.Parse(el.Element("Size").Value);
      res.HeaderPatchPos = int.Parse(el.Element("HeaderPatchPos").Value);
      res.DataPos = int.Parse(el.Element("DataPos").Value);
      res.FileName = el.Element("FileName").Value;
      return res;
    }

    public static void Patch(string fileName, byte[] newWalue) {
      Patch(fileName, infoFromXml(fileName), newWalue);
    }

    public static void Patch(string fileName, PatchInfo info, byte[] newWalue) {
      if (newWalue.Length != info.Size) throw new Exception("newWalue.Length!=info.Size");
      using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite)) {
        ICSharpCode.SharpZipLib.Checksums.Crc32 crc = new ICSharpCode.SharpZipLib.Checksums.Crc32();
        crc.Reset();
        crc.Update(newWalue);
        int crcValue = (int)crc.Value;
        fs.Seek(info.HeaderPatchPos, SeekOrigin.Begin);
        WriteLeInt(fs, crcValue);
        WriteLeInt(fs, info.Size);
        WriteLeInt(fs, info.Size);
        fs.Seek(info.DataPos, SeekOrigin.Begin);
        fs.Write(newWalue, 0, newWalue.Length);
      }
    }
    public static void Patch(string fileName, PatchInfo info, byte[] newWalue, byte fillValue, Stream outStr) {
      if (newWalue.Length > info.Size) throw new Exception("newWalue.Length > info.Size");
      if (newWalue.Length < info.Size) {
        byte[] nw = new byte[info.Size]; newWalue.CopyTo(nw, 0);
        for (int i = newWalue.Length; i < nw.Length; i++) nw[i] = fillValue;
        newWalue = nw;
      }
      using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read)) {
        ICSharpCode.SharpZipLib.Checksums.Crc32 crc = new ICSharpCode.SharpZipLib.Checksums.Crc32();
        crc.Reset();
        crc.Update(newWalue);
        int crcValue = (int)crc.Value;
        int actPos = 0;
        int len = (int)info.HeaderPatchPos;
        LowUtils.CopyStream(fs, 0, len, outStr); actPos += len;
        WriteLeInt(outStr, crcValue); actPos += 4;
        WriteLeInt(outStr, info.Size); actPos += 4;
        WriteLeInt(outStr, info.Size); actPos += 4;
        len = (int)info.DataPos - actPos;
        LowUtils.CopyStream(fs, actPos, len, outStr); actPos += len;
        outStr.Write(newWalue, 0, newWalue.Length); actPos += newWalue.Length;
        len = (int)fs.Length - actPos;
        LowUtils.CopyStream(fs, actPos, len, outStr); actPos += len;
      }
    }

    public static void Patch(string fileName, byte[] newWalue, byte fillValue, Stream outStr) {
      Patch(fileName, infoFromXml(fileName), newWalue, fillValue, outStr);
    }

    static void WriteLeShort(Stream str, int value) {
      str.WriteByte((byte)(value & 0xff));
      str.WriteByte((byte)((value >> 8) & 0xff));
    }
    static void WriteLeInt(Stream str, int value) {
      WriteLeShort(str, value);
      WriteLeShort(str, value >> 16);
    }
#endif
  }
}
