using LMComLib;
using LMNetLib;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DesignNew {

  //****************************** SYNCHRONIZE dirs library
  public static class SynchronizeDirs {
    public static string synchronize(bool isJS, IDriver driver, IEnumerable<BuildIds> buildIds, IEnumerable<Langs> locs) {
      var src = buildEnvelopes.adjust();
      var dest = driver.readMap();
      var delta = src.synchronize(dest, isJS, buildIds, locs);
      if (delta == null) return "Up-to-date";
      driver.update(delta);
      return string.Format("Deleted: {0}, updated: {1}, inserted: {2}", delta.delete.Count, delta.update.Count, delta.insert.Count);
    }
  }

  //******************************** SOURCE FILE MAP
  //zpravuje d:\LMCom\rew\Web4\Deploy\envelopes.xml se seznamem souboru
  //envelopes.xml se aktualizuje v DesignTimeLib.BuildLib.writeVirtualFiles. 
  //Kazdy potrebny datovy soubor (JS nebo MM) se zaeviduje a zjisti se eTag a dalsi info (jazyk, buildId).
  public class buildEnvelopes {

    public buildEnvelope[] items { get { return dir.Values.ToArray(); } set { dir = value.ToDictionary(v => v.url); } }

    [XmlIgnore]
    public Dictionary<string, buildEnvelope> dir;

    //reset
    public static void reset() {
      if (!File.Exists(fn)) return;
      Trace.WriteLine("Delete envelopes file: " + fn);
      File.Delete(fn);
    }
    //otevreni
    public static buildEnvelopes adjust() {
      Trace.WriteLine("Open envelopes file: " + fn);
      return File.Exists(fn) ? XmlUtils.FileToObject<buildEnvelopes>(fn) : new buildEnvelopes { dir = new Dictionary<string, buildEnvelope>() };
    }
    //ulozeni
    public void save() { XmlUtils.ObjectToFile(fn, this); }
    static string fn = Machines.rootPath + @"Deploy\envelopes.xml";

    //aktualizace jednoho datoveho souboru (vcetne ev. zapisu kdyz se lisi eTag)
    public void adjustEnvelope(BuildIds buildId, string url, byte[] data = null) {
      var fn = Machines.rootDir + url.Replace('/', '\\');
      url = url.ToLower();
      buildEnvelope env;
      lock (dir) {
        if (!dir.TryGetValue(url, out env)) dir.Add(url, env = new buildEnvelope() { buildIdLst = new List<BuildIds>(), lastWriteTime = DateTime.MinValue, url = url });
      }
      if (env.buildIdLst.IndexOf(buildId) < 0) env.buildIdLst.Add(buildId);
      if (data != null) { //JS files a ostatni soubory v data
        using (var md5 = MD5.Create()) {
          var etag = Convert.ToBase64String(md5.ComputeHash(data));
          if (env.etag != etag) {
            env.etag = etag;
            File.WriteAllBytes(fn, data);
          }
          //lang
          var parts = url.Split('.');
          if (parts[parts.Length - 1] == "js" && Consts.allDataLocs.Contains(parts[parts.Length - 2])) env.lang = LowUtils.EnumParse<Langs>(parts[parts.Length - 2]);
        }
      } else { //MM files
        var lastWriteTime = File.GetLastWriteTimeUtc(fn);
        if (lastWriteTime > env.lastWriteTime) { //mm soubor je novejsi => spocitej etag
          using (var md5 = MD5.Create())
          using (var str = File.OpenRead(fn)) {
            env.etag = Convert.ToBase64String(md5.ComputeHash(str));
            env.lastWriteTime = lastWriteTime;
          }
        }
      }
    }

    public dirItemsDelta synchronize(dirItems dest, bool isJS, IEnumerable<BuildIds> buildIds, IEnumerable<Langs> langs) {
      //filter
      HashSet<BuildIds> bids = new HashSet<BuildIds>(buildIds); HashSet<Langs> lcs = new HashSet<Langs>(langs);
      var filterDir = dir.Values.Where(v => {
        if (v.url.EndsWith(".js") != isJS) return false;
        return (v.lang == Langs.no || lcs.Contains(v.lang)) && v.buildIdLst.Any(bid => bids.Contains(bid));
      }).ToDictionary(v => v.url);
      //sync
      var res = new dirItemsDelta();
      HashSet<string> srcDone = new HashSet<string>();
      for (int i = dest.items.Count - 1; i >= 0; i--) { //delete, update:
        dirItem destItem = dest.items[i]; buildEnvelope srcItem;
        if (filterDir.TryGetValue(destItem.url, out srcItem)) {
          srcDone.Add(destItem.url);
          if (srcItem.etag == destItem.etag) continue;
          res.update.Add(destItem); destItem.etag = srcItem.etag;
        } else {
          res.delete.Add(destItem);
          dest.items.RemoveAt(i);
        }
      }
      foreach (var srcItem in filterDir.Values) { //insert:
        if (srcDone.Contains(srcItem.url)) continue;
        dirItem destItem = new dirItem { url = srcItem.url, etag = srcItem.etag };
        res.insert.Add(destItem); dest.items.Add(destItem);
      }
      if (res.empty()) return null;
      res.fileMap = XmlUtils.ObjectToBytes(dest);
      return res;
    }
  }

  public class buildEnvelope { //pro JS soubory i MM soubory obsahuje etag a info, ktereho buildu je soubor soucasti
    [XmlAttribute]
    public string etag;
    [XmlAttribute]
    public string url;
    [XmlAttribute, DefaultValue(0)]
    public long modified { get { return lastWriteTime == DateTime.MinValue ? 0 : lastWriteTime.Ticks; } set { lastWriteTime = new DateTime(value); } }
    [XmlAttribute]
    public string buildIds { get { return buildIdLst.Select(bi => bi.ToString()).Aggregate((r, i) => r + "," + i); } set { buildIdLst = value.Split(',').Select(s => LowUtils.EnumParse<BuildIds>(s)).ToList(); } }
    [XmlAttribute, DefaultValue(Langs.no)]
    public Langs lang;

    [XmlIgnore]
    public DateTime lastWriteTime; //pro mm soubory: datum souboru ve filesystemu, kvuli zjisteni jeho zmeny a novemu spocitani etagu
    [XmlIgnore]
    public List<BuildIds> buildIdLst; //projekty, kam soubor patri

  }

  //******************************** DESTINATION FILE MAP

  public class dirItems {
    public List<dirItem> items = new List<dirItem>();
  }

  public class dirItem {
    [XmlAttribute]
    public string etag;
    [XmlAttribute]
    public string url;
  }

  public class dirItemsDelta {
    public List<dirItem> update = new List<dirItem>();
    public List<dirItem> insert = new List<dirItem>();
    public List<dirItem> delete = new List<dirItem>();
    public byte[] fileMap;
    public bool empty() { return update.Count + insert.Count + delete.Count == 0; }
  }

  //**************************** DRIVERS

  public interface IDriver {
    dirItems readMap();
    void update(dirItemsDelta delta);
    void deleteContainer();
  }

  //http://blogs.msdn.com/b/windowsazurestorage/archive/2014/09/08/managing-concurrency-in-microsoft-azure-storage.aspx
  //http://justazure.com/azure-blob-storage-part-6-blob-properties-metadata-etc/
  public class azureDriver : IDriver {
    CloudBlobContainer container;
    CloudBlobClient blobClient;
    public azureDriver(string connStr, string containerName) {
      //var connStr = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", accountName, accountKey);
      CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connStr);
      blobClient = storageAccount.CreateCloudBlobClient();
      var props = blobClient.GetServiceProperties();
      if (props.Cors.CorsRules.Count == 0) {
        props.Cors.CorsRules.Add(new CorsRule() {
          AllowedHeaders = new List<string>() { "*" },
          AllowedMethods = CorsHttpMethods.Get,
          AllowedOrigins = new List<string>() { "*" },
          ExposedHeaders = new List<string>() { "*" },
          MaxAgeInSeconds = 18000 // 300 minutes
        });
        blobClient.SetServiceProperties(props);
      }
      container = blobClient.GetContainerReference(containerName);
      if (container.CreateIfNotExists()) container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
    }
    public void deleteContainer() {
      container.DeleteIfExists();
    }
    public dirItems readMap() {
      CloudBlockBlob blockBlob = container.GetBlockBlobReference("container.content");
      if (blockBlob.Exists())
        using (var str = blockBlob.OpenRead()) return XmlUtils.StreamToObject<dirItems>(str);
      return new dirItems();
    }
    //https://cmatskas.com/working-with-azure-blobs-through-the-net-sdk/
    //https://msdn.microsoft.com/en-us/library/wa_storage_30_reference_home.aspx
    public void update(dirItemsDelta delta) {
      //      var rootContainer = blobClient.GetContainerReference("$root");
      //      if (rootContainer.CreateIfNotExists()) rootContainer.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

      //      var slAccess = rootContainer.GetBlockBlobReference("clientaccesspolicy.xml");
      //      slAccess.Properties.ContentType = "text/xml; charset=utf-8";
      //      slAccess.UploadText(
      //@"<?xml version=""1.0"" encoding=""utf-8""?>
      //<access-policy>
      //  <cross-domain-access>
      //    <policy>
      //      <allow-from http-methods=""*"" http-request-headers=""*"">
      //        <domain uri=""*"" />
      //        <domain uri=""http://*"" />
      //        <domain uri=""https://*"" />
      //      </allow-from>
      //      <grant-to>
      //        <resource path=""/"" include-subpaths=""true"" />
      //      </grant-to>
      //    </policy>
      //  </cross-domain-access>
      //</access-policy>");

      if (delta == null || delta.empty()) ;
      Parallel.ForEach(delta.delete, new ParallelOptions { MaxDegreeOfParallelism = paralelCount }, it => container.GetBlockBlobReference(it.url.Substring(1)).Delete());
      Parallel.ForEach(delta.update.Concat(delta.insert), new ParallelOptions { MaxDegreeOfParallelism = paralelCount }, it => {
        var blob = container.GetBlockBlobReference(it.url.Substring(1));
        var fn = Machines.rootDir + it.url.Replace('/', '\\');
        blob.Properties.ContentType = Consts.contentTypes[Path.GetExtension(fn)];
        blob.UploadFromFile(fn, FileMode.Open);
      });
      var map = container.GetBlockBlobReference("container.content");
      map.UploadFromByteArray(delta.fileMap, 0, delta.fileMap.Length);
    }
    const int paralelCount = 10;
  }

  public class fileSystemDriver : IDriver {
    public fileSystemDriver(string basicPath, string container) { this.basicPath = basicPath + "\\" + container; }
    string basicPath;
    public dirItems readMap() {
      var path = fileName("/container.content");
      return File.Exists(path) ? XmlUtils.FileToObject<dirItems>(path) : new dirItems();
    }
    public void deleteContainer() {
      Directory.Delete(basicPath, true);
    }
    public void update(dirItemsDelta delta) {
      if (delta.empty()) return;
      Parallel.ForEach(delta.delete, it => File.Delete(fileName(it.url)));
      Parallel.ForEach(delta.update.Concat(delta.insert), it => {
        var fn = fileName(it.url); Directory.CreateDirectory(Path.GetDirectoryName(fn));
        File.Copy(Machines.rootDir + it.url.Replace('/', '\\'), fn, true);
      });
      File.WriteAllBytes(fileName("/container.content"), delta.fileMap);
    }
    string fileName(string url) { return basicPath + url.Replace('/', '\\'); }
  }

}
