using LMComLib;
using LMNetLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web.Hosting;
using System.Xml.Serialization;

namespace ReleaseDeploy {

  public class Signature {
    public string Id;
    public DateTime Created;
    [XmlElement("Batch", typeof(Packager.ScormBatch))]
    [XmlElement("WebBatch", typeof(Packager.WebSoftwareBatch))]
    public Packager.BatchLow cfg;
  }

  public static class Lib {

    static bool avoidOldVersion = true;

    public static void Deploy(Targets target, string id) {
      string srcDir = Machines.basicPath + string.Format(@"rew\Downloads\{0}s\{1}", target, id);
      string path = srcDir;
      switch (target) {
        case Targets.scorm: path = Directory.EnumerateFiles(path).First(); break;
        case Targets.web: path = path + ".zip"; break;
        default: throw new NotImplementedException();
      }

      string destPath = id; bool isDynamic = false;

      /******** signature.xml to object *************/
      Dictionary<string, byte[]> signBin = extractSignatureBin(path);
      if (signBin != null) {
        Signature sign = XmlUtils.BytesToObject<Signature>(signBin["signature.xml"]);
        if (string.Compare(sign.Id, id, true) != 0 || sign.cfg.target != target || sign.cfg.actBatchVersion != Packager.batchVersion.release) throw new Exception("string.Compare(sign.Id, id, true) != 0 || sign.cfg.target != target || sign.cfg.actBatchVersion!=release");
        isDynamic = sign.cfg.licenceConfig.isDynamic;
        if (isDynamic && !avoidOldVersion) destPath += "_" + sign.Created.ToString("yyyy-MM-dd"); //AVOID OLD VERSION

        string rewName = string.Format("_rew_{0}", target);
        string rewFn = Machines.basicPath + string.Format(@"ReleaseDeploy\JSCrambler\{0}\{1}.js", rewName, sign.cfg.licenceConfig.rewVersion);
        string crsFn = Machines.basicPath + string.Format(@"ReleaseDeploy\JSCrambler\_course\{0}.js", sign.cfg.licenceConfig.courseVersion);
        string extFn = Machines.basicPath + string.Format(@"ReleaseDeploy\JSCrambler\_external\{0}.js", sign.cfg.licenceConfig.extVersion);
        string groundFn = Machines.basicPath + string.Format(@"ReleaseDeploy\JSCrambler\_ground\{0}.js", sign.cfg.licenceConfig.groundVersion);
        LowUtils.AdjustFileDir(rewFn); LowUtils.AdjustFileDir(crsFn); LowUtils.AdjustFileDir(extFn); LowUtils.AdjustFileDir(groundFn);

        /******* rew/[version].js, _course/[version].js, _external/[version].js  ****************/
        if (!File.Exists(rewFn)) File.WriteAllText(rewFn, Encoding.UTF8.GetString(signBin[rewName + ".js"]).Replace("debugger;", null));
        if (!File.Exists(crsFn)) File.WriteAllText(crsFn, Encoding.UTF8.GetString(signBin["_course.js"]).Replace("debugger;", null));
        if (!File.Exists(extFn)) File.WriteAllText(extFn, Encoding.UTF8.GetString(signBin["_external.js"]));
        if (!File.Exists(groundFn)) File.WriteAllText(groundFn, Encoding.UTF8.GetString(signBin["_ground.js"]));

        //var jsId = target.ToString() + ": " + destPath;
        //Handlers.Licence.JSInfo.adjust(rewFn + ".xml", sign.cfg.licenceConfig.rewVersion, jsId);
        //Handlers.Licence.JSInfo.adjust(crsFn + ".xml", sign.cfg.licenceConfig.courseVersion, jsId);
        //Handlers.Licence.JSInfo.adjust(extFn + ".xml", sign.cfg.licenceConfig.extVersion, jsId);

        /******* pro isDynamic: rew/[version].charMin.js, rew/[version].charMin.js.gzip ****************/
        if (isDynamic) {
          string rewSFn = rewFn.Replace(".js", ".min.js");
          if (!File.Exists(rewSFn) || !File.Exists(rewSFn + ".gzip")) {
            JSCrambler.Lib.Protect(new JSCrambler.FileItem[] { new JSCrambler.FileItem(rewFn, rewSFn) });
            Handlers.GZipHandler.GZip(rewSFn);
          }
        }
      }

      /******** copy *************/
      //string destPath = Machines.basicPath + string.Format(@"ReleaseDeploy\{0}\{1}s\{2}{3}", isDynamic ? "Dynamic" : "Static", target, email, isDynamic ? "_" + sign.Created.ToString("yyyy-MM-dd") : null);
      destPath = Machines.basicPath + @"ReleaseDeploy\packs\" + destPath;

      switch (target) {
        case Targets.scorm:
          if (Directory.Exists(destPath)) {
            if (!avoidOldVersion && !isDynamic && !Directory.Exists(destPath + ".last")) { //AVOID OLD VERSION
              Directory.Move(destPath, destPath + ".last");
            } else
              Directory.Delete(destPath, true);
          }
          LowUtils.CopyFolder(srcDir, destPath);
          break;
        case Targets.web:
          destPath = destPath + ".zip";
          if (!avoidOldVersion && File.Exists(destPath)) {
            if (!isDynamic && !File.Exists(destPath + ".last")) { //AVOID OLD VERSION
              File.Move(destPath, destPath + ".last");
            } else
              File.Delete(destPath);
          }
          LowUtils.AdjustFileCopy(srcDir + ".zip", destPath, true);
          break;
        default: throw new NotImplementedException();
      }
    }

    public static void packSignatureBin(string path, string rewFn, string crsFn, string extFn, string groundFn, string id, Packager.BatchLow cfg) {
      var items = cfg.ItemsLow;
      try {
        cfg.ItemsLow = null;
        //signature.sign
        using (var ms = new MemoryStream()) {
          using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true)) {
            if (rewFn != null) LMZipArchive.addFileToZip(zip, rewFn, Path.GetFileName(rewFn));
            if (crsFn != null) LMZipArchive.addFileToZip(zip, crsFn, Path.GetFileName(crsFn));
            if (extFn != null) LMZipArchive.addFileToZip(zip, extFn, Path.GetFileName(extFn));
            if (groundFn != null) LMZipArchive.addFileToZip(zip, groundFn, Path.GetFileName(groundFn));
            LMZipArchive.addFileToZip(zip, XmlUtils.ObjectToBytes(new ReleaseDeploy.Signature() {
              Id = id,
              Created = DateTime.UtcNow,
              cfg = cfg,
            }), "signature.xml");
          }
          ms.Seek(0, SeekOrigin.Begin);
          File.WriteAllBytes(path, LowUtils.Encrypt(ms.ToArray()));
          //File.WriteAllBytes(@"d:\temp\pom.zip", resultMs.ToArray());
        }
      } finally { cfg.ItemsLow = items; }
    }

    public static Dictionary<string, byte[]> extractSignatureBin(string path) {
      Dictionary<string, byte[]> dict = null;
      //extract signature.sign
      MemoryStream ms = null;
      using (var fs = File.OpenRead(path)) using (var zip = new ZipArchive(fs)) {
        var entry = zip.Entries.FirstOrDefault(e => e.FullName == "signature.sign");
        if (entry != null) using (var signStr = entry.Open()) signStr.CopyTo(ms = new MemoryStream());
      }
      if (ms != null) dict = unpackSignatureBin(ms.ToArray());
      return dict;
    }

    static Dictionary<string, byte[]> unpackSignatureBin(byte[] signSign) {
      var dict = new Dictionary<string, byte[]>();
      //decode signature.sign
      MemoryStream res = new MemoryStream(LowUtils.Decrypt(signSign));
      //Extract signature.sign data
      res.Seek(0, SeekOrigin.Begin);
      using (var zip = new ZipArchive(res)) {
        var ms = new MemoryStream();
        foreach (var ze in zip.Entries) using (var s = ze.Open()) { ms.SetLength(0); s.CopyTo(ms); dict[ze.Name] = ms.ToArray(); }
      }
      return dict;
    }


    public static Signature signatureLow() {
      var fn = HostingEnvironment.MapPath("~/signature.sign");
      if (File.Exists(fn)) {
        var dir = unpackSignatureBin(File.ReadAllBytes(fn));
        return XmlUtils.BytesToObject<Signature>(dir["signature.xml"]);
      } else
        return null;
    }

    public static Signature signature() {
      var fn = HostingEnvironment.MapPath("~/signature.sign");
      if (File.Exists(fn)) {
        var dir = unpackSignatureBin(File.ReadAllBytes(fn));
        return XmlUtils.BytesToObject<Signature>(dir["signature.xml"]);
      } else
        return new Signature();
      //zlobi pri vice SCORM davkach najednou
      //if (_signature == null) {
      //  var fn = HostingEnvironment.MapPath("~/signature.sign");
      //  if (File.Exists(fn)) {
      //    var dir = unpackSignatureBin(File.ReadAllBytes(fn));
      //    _signature = XmlUtils.BytesToObject<Signature>(dir["signature.xml"]);
      //  } else
      //    _signature = new Signature();
      //}
      //return _signature;
    } //public static Signature _signature;

    public static T config<T>() where T : Packager.BatchLow {
      var sign = signature();
      if (sign.cfg == null) sign.cfg = Activator.CreateInstance<T>();
      return (T)sign.cfg;
    }

    public static schools.config actConfig;

    public static schools.config adjustActConfig() {
      if (actConfig != null) return actConfig;
      return actConfig = ReleaseDeploy.Lib.config<Packager.BatchLow>();
    }



  }
}
