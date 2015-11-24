using DesignNew;
using LMComLib;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Hosting;
using LMNetLib;

namespace WebCode {

  public static class Handler {
  }

  
  public class UrlRewrite : IHttpModule {


    void IHttpModule.Init(HttpApplication context) {

      swFile.extractSwFilesToCache(); //soubory z d:\LMCom\rew\WebCode\App_Data\swfiles.zip do cache

      context.BeginRequest += (s, a) => {
        HttpApplication app = (HttpApplication)s;
        var pth = relPath(app.Request.Url.LocalPath);
        if (pth == null) { app.Response.Redirect("~/schools/index.html"); return; }
        //**** software file (z swfiles.zip nebo schools/index-*.html) z cache
        swFile sf;
        lock (swFile.swFiles) swFile.swFiles.TryGetValue(pth, out sf);
        if (sf != null) { makeResponseFromCache(sf, app); return; } //request z cache => return

        //**** prvni pristup k index file ASPX - nastav MemoryStream do filteru tak, aby se v context.UpdateRequestCache dala vygenerovana stranka dat do cache
        if (pth == "schools/index.aspx") {
          var oldFilter = app.Response.Filter; //http://romsteady.blogspot.cz/2008/12/workaround-aspnet-response-filter-is.html
          app.Response.Filter = new MemoryStream();
          oldFilter.Dispose();
          return;
        }

        //**** prvni pristup k index file HTML => proved rewrite
        if (pth.StartsWith("schools/index")) {
          //https://forums.iis.net/t/1146511.aspx
          NameValueCollection headers = new NameValueCollection(app.Request.Headers);
          headers["Orig-url"] = pth;
          var parts = pth.Split('.')[0].Split('-'); string lang = null; string isDebug = null; string designId = null;
          switch (parts.Length) {
            case 1: break;
            case 2: lang = parts[1]; break;
            case 3: lang = parts[1] + "-" + parts[2]; break;
            case 4: lang = parts[1] + "-" + parts[2]; isDebug = parts[3]; break;
            case 5: lang = parts[1] + "-" + parts[2]; isDebug = parts[3]; designId = parts[4]; break;
            default: throw new Exception(pth);
          }
          app.Server.TransferRequest("~/schools/index.aspx" +
            (lang == null ? null : "?lang=" + lang) +
            (isDebug == null ? null : "&isDebug=" + isDebug) +
            (designId == null ? null : "&designId=" + designId),
            false, "GET", headers);
        }

      };

      context.UpdateRequestCache += (s, a) => {
        HttpApplication app = (HttpApplication)s;
        var pth = relPath(app.Request.Url.LocalPath);
        if (pth != "schools/index.aspx") return;
        //zpracovani index.aspx
        MemoryStream filter = (MemoryStream)app.Response.Filter; //vygenerovana index-*.html stranka
        string oldPth = app.Request.Headers["Orig-url"]; //a jeji puvodni URL
        var sf = swFile.addToCache(oldPth, ".html", filter.ToArray()); //vlozeni index-*.html do cache
        makeResponseFromCache(sf, app); //naplneni response
      };
    }

    void makeResponseFromCache(swFile sf, HttpApplication app) {
      string eTag = app.Request.Headers["If-None-Match"];
      app.Response.AppendHeader("Etag", sf.eTag);
      app.Response.ContentType = DesignNew.Consts.contentTypes[sf.ext];
      if (sf.eTag == eTag) { //souhlasi eTag => not modified
        app.Response.ClearContent();
        app.Response.StatusCode = (int)HttpStatusCode.NotModified;
        app.Response.AddHeader("Content-Length", "0");
        app.Response.SuppressContent = true;
      } else { //nova stranka (a ev. jeji gzip verze)
        string acceptEncoding = app.Request.Headers["Accept-Encoding"];
        app.Response.StatusCode = (int)HttpStatusCode.OK;
        if (!string.IsNullOrEmpty(acceptEncoding) && acceptEncoding.Contains("gzip") && sf.gzipData != null) {
          app.Response.AppendHeader("Content-Encoding", "gzip");
          app.Response.OutputStream.Write(sf.gzipData, 0, sf.gzipData.Length);
        } else {
          app.Response.OutputStream.Write(sf.data, 0, sf.data.Length);
        }
      }
      app.Response.End();
    }

    void IHttpModule.Dispose() { }

    string relPath(string localPath) {
      var toRemove = HostingEnvironment.ApplicationVirtualPath.Length + (HostingEnvironment.ApplicationVirtualPath == "/" ? 0 : 1);
      return toRemove >= localPath.Length ? null : localPath.Substring(toRemove).ToLower();
    }

    public class swFile {

      public static Dictionary<string, swFile> swFiles = new Dictionary<string, swFile>(); //cache

      public swFile(string nm) { name = nm; ext = Path.GetExtension(name); }

      public void setData(byte[] d, MD5 md5) { data = d; eTag = Convert.ToBase64String(md5.ComputeHash(d)); }

      public string name;
      public string ext;
      public string eTag;
      public byte[] data;
      public byte[] gzipData;

      public static swFile addToCache(string name, string ext, byte[] data) { //index-*.html do cache
        lock (swFiles) {
          swFile actFile;
          if (swFiles.TryGetValue(name, out actFile)) return actFile;
          swFiles.Add(name, actFile = new swFile(name) { ext  = ext });
          using (MD5 md5 = MD5.Create()) actFile.setData(data, md5);
          using (var ms = new MemoryStream()) {
            using (var gzip = new GZipStream(ms, CompressionMode.Compress)) gzip.Write(data, 0, data.Length);
            actFile.gzipData = ms.ToArray();
          }
          return actFile;
        }
      }

      public static void extractSwFilesToCache() { //soubory z d:\LMCom\rew\WebCode\App_Data\swfiles.zip do cache pri startu aplikace
        var zipFn = @"d:\LMCom\rew\WebCommon\swfiles.zip"; // HostingEnvironment.MapPath("~/app_data/swfiles.zip");
        var mStr = new MemoryStream();
        using (MD5 md5 = MD5.Create())
        using (var zipStr = File.OpenRead(zipFn))
        using (ZipArchive zip = new ZipArchive(zipStr, ZipArchiveMode.Read)) {
          foreach (var f in zip.Entries)
            using (var fStr = f.Open()) {
              mStr.SetLength(0);
              fStr.CopyTo(mStr);
              string name = f.FullName.Replace('\\', '/'); bool isGZip = false;
              if (name.EndsWith(".gzip")) {
                name = name.Substring(0, name.Length - 5);
                isGZip = true;
              }
              swFile actFile;
              if (!swFiles.TryGetValue(name, out actFile)) swFiles.Add(name, actFile = new swFile(name));
              if (isGZip) actFile.gzipData = mStr.ToArray(); else actFile.setData(mStr.ToArray(), md5);
            }
        }
      }
    }

  }

}
