using LMNetLib;
using Packager;
using System.Collections.Generic;
using System.Linq;

namespace DesignTimeLib.Design.Templates {

  public partial class ImsManifest {
    public ScormBatchItem cfg;
    public ScormBatch batch;
    public IEnumerable<string> files { get { return Packager.RewApp.scormFilesStr(batch, cfg, false, new LoggerMemory(true)); } }
  }
  public partial class html {
    public string serverScript;
    public Config cfg;
    public string pageTitle {
      get {
        switch (cfg.designId) {
          case "skrivanek": return "Skřivánek";
          default: return "LANGMaster";
        }
      }
    }
  }

  public partial class htmlHead {
    public Config cfg;
    public bool forStatistics;
  }

  //public partial class groundHead {
  //  public Config cfg;
  //}


}