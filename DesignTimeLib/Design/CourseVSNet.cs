using LMComLib;
using LMNetLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Linq;
using CourseMeta;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace Author {

  public interface INodeContext {
    sitemap getSiteMap(LoggerMemory logger);
    LineIds line { get; set; }
    data actNode { get; set; }
    string url { get; set; }
  }

  // ***************** FILE CONTEXT
  public class fileContext : INodeContext {
    public static void adjustFileContext(string fn, object vsNetHiearchy, publisherContext publ, ref fileContext ctx) {
      if (fn.EndsWith("\\")) { fn += "meta.xml"; if (!File.Exists(fn)) { ctx = null; return; } }
      fn = fn.ToLower();
      ctx = new fileContext(fn, vsNetHiearchy) { publ = publ };
    }
    public fileContext(string url) { this.url = url; }

    fileContext(string fn, object vsNetHiearchy) {
      fileName = fn; this.vsNetHiearchy = vsNetHiearchy;
      availableActions = actions.browse | actions.saveHtml;
      url = fileName.Substring(Machines.rootDir.Length).Replace('\\', '/').Replace("/meta.xml", "/").Replace(".xml", null);
      bool isFolder = url.EndsWith("/");
      actNode = isFolder ? data.readObject<data>(fileName) : null;
      created = DateTime.UtcNow;
      var dir = Path.GetDirectoryName(fn);
      var parPath = dir.Substring(Machines.rootPath.Length).Split('\\');
      bool lineOK = false; bool actionOK = !isFolder; //action se zjistuje pouze pro foldery
      for (int idx = parPath.Length - 1; idx >= 0; idx--) {
        var parFn = Machines.rootPath + parPath.Take(idx + 1).Aggregate((r, i) => r + "\\" + i) + "\\meta.xml";
        var nd = data.readObject<data>(parFn);
        if (!lineOK && nd.line != LineIds.no) { line = nd.line; lineOK = true; }
        if (!actionOK) {
          if (nd.isType(runtimeType.taskCourse)) {
            availableActions |= (actions.addFolder | actions.addMod | actions.build); actionOK = true;
          } else if (nd.isType(runtimeType.testTaskGroup)) {
            availableActions |= actions.addEx; actionOK = true;
          } else if (nd.isType(runtimeType.taskTestSkill)) {
            availableActions |= actions.addTestTaskGroup; actionOK = true;
          } else if (nd.isType(runtimeType.test)) {
            availableActions |= (actions.addTestSkill | actions.build); actionOK = true;
          } else if (nd.isType(runtimeType.publisher)) {
            availableActions |= actions.addFolder; availableActions |= actions.addTest; availableActions |= actions.addCourse; actionOK = true;
          } else if (nd.isType(runtimeType.mod)) {
            availableActions |= (actions.addFolder | actions.addEx); actionOK = true;
          }
        }
        if (lineOK && actionOK) break;
      }
    }
    public publisherContext publ;
    public data actNode { get; set; }
    public LineIds line { get; set; }
    public string fileName;
    public string url { get; set; }
    public actions availableActions;
    public bool hasAction(actions act) { return (availableActions & act) != 0; }
    public DateTime created;
    public object vsNetHiearchy;
    //public nodeContext nodeCtx;
    public HashSet<string> folderContent {
      get {
        if (_folderContent == null) {
          var dir = Path.GetDirectoryName(fileName);
          _folderContent = new HashSet<string>(Directory.GetDirectories(dir).Concat(Directory.GetFiles(dir)).Select(f => f.Substring(dir.Length + 1).ToLower()));
        }
        return _folderContent;
      }
    } HashSet<string> _folderContent;
    public sitemap getSiteMap(LoggerMemory logger) {
      if (_siteMap == null) {
        _siteMap = new sitemap(actNode.url, logger, sitemap.fromFileSystem("/data/instr/std/", logger), sitemap.fromFileSystem("/data/instr/new/", logger));
        if (logger.vsNetForBrowseAction) { //uprava sitemap pro vs.net browse - pouze taskCourse, data a testEx nodes. Nahrad vsechny testGlobalAdmin... nodes apod.
          XElement root;
          using (StringReader rdr = new StringReader(XmlUtils.ObjectToString(_siteMap))) root = XElement.Load(rdr);
          foreach (var highLev in root.Elements().Where(he => he.AttributeValue("type", "").IndexOf("instrs") < 0)) {
            highLev.Name = "taskCourse"; highLev.SetAttributeValue("type", "mod");
            foreach (var el in highLev.Descendants().Where(e => e.Name.LocalName != "ex")) { el.Name = "data"; el.SetAttributeValue("type", null); }
          }
          _siteMap = XmlUtils.StringToObject<sitemap>(root.ToString());
          _siteMap.finish();
        }
      }
      return _siteMap;
    } sitemap _siteMap;
  }

  //***************** ACTIONS

  [Flags]
  public enum actions {
    browse = 0x1,
    saveHtml = 0x2,
    xref = 0x4,
    addFolder = 0x8,
    addTest = 0x20,
    addCourse = 0x40,
    addTestSkill = 0x80,
    addTestTaskGroup = 0x100,
    addMod = 0x200,
    addEx = 0x400,
    build = 0x800,
  }

  public class vsNetConfig {
    static vsNetConfig() { AppSettings = new vsNetConfig(); }
    public static string configFn;
    public static vsNetConfig AppSettings;
    public string this[string name] {
      get {
        if (configFn != null) {
          if (data == null) data = XElement.Load(Machines.isPZComp() ? @"d:\LMCom\rew\SolutionToolbar\Resources\PZApp.config" : configFn).Element("appSettings").Elements().ToDictionary(e => e.AttributeValue("key"), e => e.AttributeValue("value"));
          string val; return data.TryGetValue(name, out val) ? val : null;
        } else
          return System.Configuration.ConfigurationManager.AppSettings[name];
      }
    }
    static Dictionary<string, string> data;
  }

  public class publisherContext {

    public publisherContext(string rootDir) {
      rootDir = rootDir.ToLower();
      var metaFn = rootDir + "meta.xml";

      do {
        if (!File.Exists(metaFn)) break;
        var parts = metaFn.Split('\\');
        var newDir = parts.Take(parts.Length - 2).Aggregate((r, i) => r + "\\" + i);
        try {
          var nd = data.readObject<data>(metaFn);
          if (nd is publisher) {
            publisher = (publisher)nd;
            Machines._rootDir = newDir;
            publisherFn = metaFn;
            webAppUrl = vsNetConfig.AppSettings["webAppUrl"];
            return;
          }
        } catch (Exception exp) {
          throw new Exception("XML Error in " + metaFn, exp);
        }
        metaFn = newDir + "\\meta.xml";
      } while (true);

      throw new Exception("Cannot find project");
    }
    public string webAppUrl;
    public string publisherFn;
    public publisher publisher;
  }

  public static class vsNetServer {

    // ***************** INIT
    static vsNetServer() {
      //TODO: k cemu je a kde chybi (11/24/2015)
      //Machines._tradosConnectionString = "Data Source=195.250.145.26;Initial Catalog=LMTrados;Persist Security Info=True;User ID=lmcomdatatest;Password=lmcomdatatest";
    }

    public static LoggerMemory log = new LoggerMemory(true) { isVsNet = true };

    public static string resourcePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).ToLower() + @"\resources\";

    public static IEnumerable<Packager.Consts.file> buildExFiles(Stream str, string url, LoggerMemory logger) {
      bool isXsdError;
      var xml = CourseModel.tag.loadExerciseXml(str, url, logger, out isXsdError);
      return buildExFiles(ex.readPage(xml, url, logger), logger);
    }

    public static IEnumerable<Packager.Consts.file> buildExFiles(string url, LoggerMemory logger) {
      return buildExFiles (ex.readPage(url, logger), logger);
    }

    static IEnumerable<Packager.Consts.file> buildExFiles(CourseModel.body page, LoggerMemory logger) {
      //vybuduj page a jeji instrukce
      return page == null ? Enumerable.Empty<Packager.Consts.file>() : XExtension.Create(page).Concat(page.instrs == null ? Enumerable.Empty<CourseModel.body>() : page.instrs.Select(u => ex.readPage(u, logger))).
        Select(p => new CachePage(Langs.no, p, logger)).
        SelectMany(p => p.getFiles());
    }

    public class serverContext : INodeContext {
      public serverContext(string ctxUrl, LoggerMemory logger) {
        url = ctxUrl;
        string fileName = ex.fileNameFromUrl(ctxUrl);
        //nacti actNode
        actNode = data.readObject<data>(fileName);
        //zjisti Line
        line = LineIds.no; data nd = actNode;
        while (true) {
          line = nd.line;
          if (line != LineIds.no) break;
          var parts = fileName.Split('\\'); fileName = parts.Take(parts.Length - 2).Aggregate((r, i) => r + "\\" + i) + "\\meta.xml";
          if (!File.Exists(fileName)) break;
          try {
            nd = data.readObject<data>(fileName);
          } catch (Exception exp) {
            throw new Exception("XML Error in " + fileName, exp);
          }
        }
        //vybuduj sitemap z actNode
        siteMap = new sitemap(actNode.url, logger, sitemap.fromFileSystem("/data/instr/std/", logger), sitemap.fromFileSystem("/data/instr/new/", logger));
        //if (actNode.isType(runtimeType.project)) XmlUtils.ObjectToFile(Path.ChangeExtension(fileName, ".sitemap"), siteMap);
      }
      public sitemap getSiteMap(LoggerMemory logger) { return siteMap; }
      public sitemap siteMap;
      public LineIds line { get; set; }
      public data actNode { get; set; }
      public string url { get; set; }
    }

    public static void getPublishProduct(Stream str, fileContext actCtx, string prodUrl, string globalPublisherDir, IEnumerable<Packager.Consts.file> files = null) {
      vsNetServer.log.clear();
      vsNetServer.log.vsNetGlobalPublisherDir = globalPublisherDir;
      //var log = new LoggerMemory(true) { isVsNet = true, vsNetGlobalPublisherDir = globalPublisherDir };
      var prod = new product {
        url = prodUrl,
        styleSheet = ex.stdStyle,
        line = actCtx.line,
        //defaultDictType = dictTypes.no,
        //defaultLocs = new Langs[] {Langs.en_gb},
        title = actCtx.actNode.title,
        type = actCtx.actNode.isType(runtimeType.test) ? runtimeType.product | runtimeType.test : runtimeType.product,
        Items = new data[] { new ptr(true, actCtx.url) { takeChilds = childMode.selfChild } }
      };
      getPostDataFilesZip(str, actCtx, prod, vsNetServer.log, files);
    }

    //vybuduje HTML na klientovi za pomoci template. Template se vytvari pomoci getHtmlFromScratch, s promenou casti: %#%scriptData%#% a %#%hash%#%.
    public static string getHtmFromTemplate(fileContext ctx) {
      vsNetServer.log.clear();
      vsNetServer.log.vsNetForBrowseAction = true;
      //var log = new LoggerMemory(true) { isVsNet = true, vsNetForBrowseAction = true };
      var postData = vsNetServer.getPostDataStr(ctx, null, vsNetServer.log);
      var isMod = ctx.url.EndsWith("/");
      string hash = !isMod ? XExtension.Create("vsNet".ToLower(), "vsNetExModel".ToLower(), ctx.url).Aggregate((r, i) => r + "@" + i) : "";
      var tempFn = vsNetServer.resourcePath + "author/" + (isMod ? "modTemplate" : "exTemplate") + ".htm";
      var html = FormatNamedProps(File.ReadAllText(tempFn), key => {
        switch (key) {
          case "scriptData": return postData.ToString();
          case "hash": return hash;
          case "baseTagUrl": return vsNetConfig.AppSettings["webAppUrl"];
          default: throw new NotImplementedException();
        }
      });
      return html;
    }

    public static string FormatNamedProps(string mask, Func<string, string> findValue) {
      return formatExRegex.Replace(mask, m => findValue(m.Value.Substring(3, m.Value.Length - 6)));
    }
    static Regex formatExRegex = new Regex(@"\[%#.*?#%\]", RegexOptions.Singleline);

    //vybuduje HTML na serveru
    public static string getHtmlFromScratch(INodeContext ctx, string baseUrl, string hash, LoggerMemory log, Action<StringBuilder> finishScriptData = null) {
      log.isVsNet = true;
      StringBuilder scriptData = vsNetServer.getPostDataStr(ctx, null, log, finishScriptData);
      //scriptData.Append(CourseMeta.buildLib.getServerScript(Author.vsNetServer.buildModFiles(url, out rootProductId, log)));
      //scriptData.Append(vsNetServer.buildPostDataStr(ctx, null, log));
      //if (finishScriptData != null) finishScriptData(scriptData);
      var html = Packager.RewApp.HomePage(new Packager.Config() {
        target = LMComLib.Targets.author,
        version = schools.versions.debug,
        lang = LMComLib.Langs.en_gb,
        persistType = schools.persistTypes.persistMemory,
        forceEval = true,
        baseTagUrl = baseUrl,
        hash = hash,
        rootProductId = vsNetProductId,
        forceDriver = SoundPlayerType.HTML5,
      }, scriptData.ToString());
      return html;
    }

    public static void saveHtmlData(fileContext ctx) {
      var html = getHtmFromTemplate(ctx);
      if (vsNetServer.log.hasError) return;
      SaveFileDialog saveDlg = new SaveFileDialog();
      saveDlg.InitialDirectory = Convert.ToString(Environment.SpecialFolder.MyDocuments);
      saveDlg.Filter = "Html file|*.htm";
      saveDlg.FilterIndex = 1;
      saveDlg.FileName = "page.htm";
      if (saveDlg.ShowDialog() == DialogResult.OK)
        File.WriteAllText(saveDlg.FileName, html);
    }

    public static IEnumerable<Packager.Consts.file> getPostDataFiles(INodeContext ctx, product prod, LoggerMemory log) {
      var isModule = ctx.url.EndsWith("/");
      return isModule ? vsNetServer.getModuleFiles(ctx, prod, log) : vsNetServer.buildExFiles(ctx.url, log);
    }

    public static StringBuilder getPostDataStr(INodeContext ctx, product prod, LoggerMemory log, Action<StringBuilder> finishScriptData = null) {
      StringBuilder scriptData = buildLib.getServerScript(getPostDataFiles(ctx, prod, log));
      if (finishScriptData != null) finishScriptData(scriptData);
      return scriptData;
    }

    public static void getPostDataFilesZip(Stream str, INodeContext ctx, product prod, LoggerMemory log, IEnumerable<Packager.Consts.file> files = null) {
      buildLib.zipVirtualFiles(str, getPostDataFiles(ctx, prod, log).Concat(files == null ? Enumerable.Empty<Packager.Consts.file>() : files), log);
    }

    public const string vsNetProductId = "/data/vsnet/mod/";

    public static IEnumerable<Packager.Consts.file> getModuleFiles(INodeContext ctx, product prod, LoggerMemory logger) {
      try {
        //var ctx = new serverContext(url, logger);
        if (ctx.line == LineIds.no) { logger.ErrorLine("?", "Unknown product Line"); return Enumerable.Empty<Packager.Consts.file>(); }
        if (prod == null) prod = new product {
          url = vsNetProductId,
          styleSheet = ex.stdStyle,
          line = ctx.line,
          title = ctx.actNode.title,
          //Items = new data[] { new ptr(ctx.actNode.type == runtimeType.no ? new taskCourse() : null, ctx.url) { takeChilds = childMode.selfChild } }
          Items = new data[] { new ptr(true, ctx.url) { takeChilds = childMode.selfChild } }
        };
        var sm = ctx.getSiteMap(logger);
        prod = (product)prodDef.expand(prod, sm, logger);
        prodDef.addInstructions(prod, logger);
        var bldProd = new buildProduct {
          prod = prod,
          natLangs = new Langs[] { Langs.cs_cz },
          dictType = dictTypes.L
        };
        Cache cache = new Cache(logger, new Langs[] { Langs.cs_cz });
        return bldProd.getFiles(cache, logger, sm).ToArray();
      } catch (Exception exp) {
        logger.ErrorLineFmt("?", ">>>> Compiling Error {0}", LowUtils.ExceptionToString(exp));
        return Enumerable.Empty<Packager.Consts.file>();
      }
    }

    //public static product vsNetProduct(string url, serverContext ctx) { 
    //  new product {
    //      url = productUrl,
    //      styleSheet = testEx.stdStyle,
    //      line = ctx.line,
    //      title = ctx.actNode.title,
    //      Items = new data[] { new ptr(ctx.actNode.type == runtimeType.no ? new taskCourse() : null, url) { takeChilds = childMode.selfChild } }
    //    }
    //}

    //public static IEnumerable<Packager.Consts.file> buildModFiles(string url, string productUrl, LoggerMemory logger) {
    //  return buildModFiles(url, new product {
    //      url = productUrl,
    //      styleSheet = testEx.stdStyle,
    //      line = ctx.line,
    //      title = ctx.actNode.title,
    //      Items = new data[] { new ptr(ctx.actNode.type == runtimeType.no ? new taskCourse() : null, url) { takeChilds = childMode.selfChild } }
    //    }, logger);
    //}
    //public static void getWebRequestData(actions action, fileContext actCtx, string webAppUrl, out string href, out string postData) {
    //  href = webAppUrl + "author/author.aspx?url=" + actCtx.url;
    //  authorModes mode;
    //  switch (action) {
    //    case actions.browse:
    //    //case actions.saveHtml:
    //      mode = actCtx.url.EndsWith("/") ? authorModes.displayMod : authorModes.displayEx;
    //      var log = new LoggerMemory(true) { isVsNet = true }; string rootProductId = "/data/vsnet/mod/";
    //      postData = vsNetServer.buildPostData(actCtx.url, rootProductId, log);
    //      href += "&rootProductId=" + rootProductId;
    //      if (log.hasError) postData = "###" + log.Log();
    //      if (action == actions.browse) href += "&forceeval=true";
    //      break;
    //    case actions.xref:
    //      mode = authorModes.xref;
    //      postData = null;
    //      break;
    //    default:
    //      throw new NotImplementedException();
    //  }
    //  href += "&mode=" + mode.ToString();
    //}

    //capturaPhaseOnly: true => only capture do d:\temp\ea.xml
    //capturaPhaseOnly: false => no capture, nacteni from d:\temp\ea.xml
    //capturaPhaseOnly: null => capture i nacteni
    //readNewIfExist: ridi cteni nove verze, kdyz existuje
    //public static Packager.Consts.file buildOldEAFile(string url, bool readNewIfExist, bool? capturePhaseOnly = null) {
    //  XElement xml;
    //  if (capturePhaseOnly != false) { //pro ladeni, nejdrive capture cviceni z oldEA a ulozeni jej do temp souboru
    //    var testEx = (testEx)CourseMeta.lib.publishers.find(url);
    //    xml = CourseMeta.lib.dataFromEALow(testEx, readNewIfExist);
    //    if (capturePhaseOnly == true) { CourseModel.tag.saveExXml(@"d:\temp\ea.xml", xml); return null; }
    //  } else //pro ladeni, nacte cviceni z temp souboru
    //    xml = XElement.Load(@"d:\temp\ea.xml");
    //  var body = CourseMeta.testEx.readPage(xml, url, CourseMeta.lib.publishers, null);
    //  byte[] jsonBytes; 
    //  CourseMeta.CachePage.pageJsons(body, out jsonBytes);
    //  return new Packager.Consts.file(url.Substring(1) + ".js", jsonBytes);
    //}

  }


}