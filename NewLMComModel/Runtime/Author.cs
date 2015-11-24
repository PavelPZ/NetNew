using CourseMeta;
using CourseModel;
using LMComLib;
using LMNetLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Author {
  using Admin;
  using System.IO.Compression;

  public enum authorModes {
    no,
    displayEx, //pro VSAuthor: zobrazi jedno cviceni zadane URL
    displayMod, //pro VSAuthor: zobrazi kapitolu zadane URL
    //oldEA, //pro VSAuthor: zobrazi jedno cviceni z oldEA
    xref, //pro VSAuthor: zobrazi xref k jenomu adresari zadaneho URL
    doc, //zobrazi dokumentaci
    compileEx, //vrati JSON k XML
  }

  public class Server {

    static Server() {

      /* CmdXrefData */
      //Handlers.CmdService.registerCommand<CmdXrefData, CmdXrefDataResult>(par => {
      //  return new RpcResponse(XrefContext.getXrefData(par));
      //});

      /* CmdXrefData */
      Handlers.CmdService.registerCommand<CmdDeployProject, CmdDeployProjectResult>(par => {
        return new RpcResponse(DeployProject(par));
      });
    }

    //d:\LMCom\rew\SolutionToolbar\Forms\DeployForm.cs, DeployBtn_Click
    static CmdDeployProjectResult DeployProject(CmdDeployProject par) {
      var db = NewData.Lib.CreateContext(); var res = new CmdDeployProjectResult();
      string productRoot, zipFn;
      switch (par.action) {
        case DeployProjectAction.deployStart:
          //adjust company v DB
          NewData.Companies comp;
          if (!par.isCompany) {
            var user = db.Users.First(u => u.Id == par.id);
            comp = user.MyPublisher != null ? user.MyPublisher : NewData.AdminServ.createCompany(db, string.Format("{0} {1} ({2})", user.FirstName, user.LastName, user.EMail), user, true);
          } else {
            comp = db.Companies.First(c => c.Id == par.id);
          }
          NewData.Lib.SaveChanges(db);
          res.companyId = comp.Id;
          //priprav productRoot
          productRoot = urlFromDesignUrl(comp.Id, par.url);
          zipFn = Path.ChangeExtension(ex.fileNameFromUrl(productRoot + "deploy"), ".zip");
          LowUtils.AdjustFileDir(zipFn);
          break;
        case DeployProjectAction.deployEnd:
        case DeployProjectAction.remove:
          string regProductsFn, prodUrl; List<string> regs;
          //remove
          if (par.action == DeployProjectAction.remove) {
            if (!par.isCompany) {
              var user = db.Users.First(u => u.Id == par.id);
              comp = user.MyPublisher;
            } else {
              comp = db.Companies.First(c => c.Id == par.id);
            }
            if (comp == null) return res;
            par.id = comp.Id;
          }

          //common
          productRoot = urlFromDesignUrl(par.id, par.url); // /publ/0/5/folder_1/course_1/
          zipFn = Path.ChangeExtension(ex.fileNameFromUrl(productRoot + "deploy"), ".zip"); //d:\lmcom\rew\web4\publ\0\5\folder_1\course_1\deploy.zip
          prodUrl = urlFromDesignUrl(par.id, Author.Server.prodUrlFromCourseUrl(par.url)).ToLower(); // /publ/0/5/folder_1/course_1/_prod
          regProductsFn = Path.ChangeExtension(ex.fileNameFromUrl(urlFromDesignUrl(par.id, null)), ".txt"); //d:\lmcom\rew\web4\publ\0\5\meta.txt
          regs = File.Exists(regProductsFn) ? File.ReadAllLines(regProductsFn).ToList() : new List<string>();
          //delete JS a gzip from dirs
          if (regs.IndexOf(prodUrl) >= 0) { regs.Remove(prodUrl); File.WriteAllLines(regProductsFn, regs); }
          foreach (var js in Directory.GetFiles(Path.GetDirectoryName(zipFn), "*.*", SearchOption.AllDirectories)) if (delExts.Contains(Path.GetExtension(js))) File.Delete(js);

          //deployEnd
          if (par.action == DeployProjectAction.deployEnd) {
            //unzip
            using (var str = File.OpenRead(zipFn))
            using (ZipArchive zip = new ZipArchive(str, ZipArchiveMode.Read)) {
              var publisherPath = urlFromDesignUrl(par.id, null).Replace('/', '\\'); // \publ\0\5\
              foreach (var f in zip.Entries.OfType<ZipArchiveEntry>())
                using (var fStr = f.Open()) {
                  var fn = f.FullName.Substring(f.FullName.IndexOf('\\') + 1);
                  fn = Machines.rootDir + publisherPath + fn;
                  var path = Path.GetDirectoryName(fn);
                  if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                  using (var outStr = File.Create(fn)) fStr.CopyTo(outStr);
                }
            }
            File.Delete(zipFn);
            //zaregistruj produkt
            if (regs.IndexOf(prodUrl) == -1) { regs.Add(prodUrl); File.WriteAllLines(regProductsFn, regs); }
            //ticket
            var ticketFn = Machines.rootPath + @"App_Data\tickets\" + par.ticket.name;
            LowUtils.AdjustFileDir(ticketFn);
            XmlUtils.ObjectToFile(Machines.rootPath + @"App_Data\tickets\" + par.ticket.name, par.ticket);
          }

          //common - refresh publisher siteroot.js
          var sitemapUrl = urlFromDesignUrl(par.id, null); // /publ/0/5/
          var files = regs.Select(r => ex.fileNameFromUrl(r));
          var prods = new products { url = sitemapUrl, Items = files.Where(fn => File.Exists(fn)).Select(fn => data.readObject<product>(fn)).ToArray() };
          File.WriteAllText(Path.ChangeExtension(ex.fileNameFromUrl(sitemapUrl + "siteroot"), ".js"), LowUtils.serializeObjectToJS(prods), Encoding.UTF8);
          break;
      }
      return res;
    }

    public static product[] getCompanyProducts(int companyId) {
      var regProductsFn = Path.ChangeExtension(ex.fileNameFromUrl(urlFromDesignUrl(companyId, null)), ".txt"); if (!File.Exists(regProductsFn)) return null;
      var regs = File.ReadAllLines(regProductsFn).ToArray(); if (regs.Length == 0) return null;
      return regs.Select(r => ex.fileNameFromUrl(r)).Where(fn => File.Exists(fn)).Select(fn => data.readObject<product>(fn)).ToArray(); ;
    }

    public static string prodUrlFromCourseUrl(string designUrl) {
      return designUrl + "_prod";
      //var parts = designUrl.Split('/').ToList(); parts.Insert(2, "prods");
      //return LowUtils.join(parts, "/");
    }
    public static string urlFromDesignUrl(Int64 companyId, string designUrl) {
      var mod = companyId % publisherSubpathModulo;
      var group = (int)((companyId - mod) / publisherSubpathModulo);
      return string.Format("/publ/{0}/{1}{2}", group, mod, designUrl == null ? "/" : designUrl.Substring(LowUtils.nthIndexesOf(designUrl, '/', 2)));
    }
    const int publisherSubpathModulo = 500;
    static HashSet<string> delExts = new HashSet<string>() { ".js", ".gzip", ".xml" };
  }

}

namespace Admin {
  public enum CmdXrefDataOpers {
    nodeTypes, typeProps, typePropValues, typeLinks, typePropLinks, typePropValueLinks,
    nodeProps, propValues, propLinks, propValueLinks,
    refreshXref,
    checkAll,
  }
  public class CmdXrefData {
    public CmdXrefDataOpers oper;
    public string type;
    public string prop;
    public string value;
    public int nodeId;
    public int maxLinks;
    public string urlContext; //null pro globalni xref, URL pro subdir browse
  }

  public class CmdXrefDataResult {
    public string[] names;
    public xrefLink[] links;
    public string error;
  }

  public class xrefLink {
    public string title;
    public string url;
  }

  //vs.net author deployment
  public enum DeployProjectAction {
    deployStart,
    deployEnd,
    remove,
  }

  public class CmdDeployProject {
    public DeployProjectAction action;
    public bool isCompany; //pro action=deployStart: company deploy nebo individual deploy
    public bool isRemove; //odstraneni produktu
    public Int64 id; //pro isStart: companyId nebo companyUserId (jinak vracena companyId)
    public string url; //relativni url produktu (relativne k publisher root)
    public Login.CmdLoginTicket ticket; //ticket pro zalogovani
  }
  public class CmdDeployProjectResult {
    public int companyId; //hlavne pro isCompany=false; int s user individual company email
  }
}

