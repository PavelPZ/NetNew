using LMComLib;
using System;

namespace scorm {

  public class ScormCmd : LMComLib.Cmd {
    //public Int64 lmcomId; //ID uzivatele pro LM persistenci 
    public int companyId;
    public string productId;
    public string scormId; //scorm ID uzivatele pro SCORM persistenci
    public Int64 date; //datum vzniku cmd
  }

  public class Cmd_Logger: ScormCmd {
    public string id;
    public string data;
  }

  /************** TESTS ***********************/
  //public class Cmd_ReadTests : Cmd {
  //  public bool isStart;
  //  public string[] testIds;
  //}

  //public class Cmd_SaveTestWithModules : Cmd {
  //  public string data;
  //}

  //public class Cmd_SaveTest : Cmd {
  //  public string testId;
  //  public string data;
  //  public string proxy;
  //}

  //public class Cmd_SaveTestModule : Cmd_SaveTest {
  //  public int moduleId;
  //}

  //public class Cmd_LoadTestModule : Cmd {
  //  public int moduleId;
  //}

  //public class Cmd_LoadTestInfo : Cmd {
  //  public string testId;
  //}

  /************** COURSES ***********************/
  public class Cmd_resetModules : ScormCmd {
    public string[] modIds;
  }

  public class Cmd_readCrsResults : ScormCmd {
    //public bool isStart;
  }

  public class Cmd_readModuleResults : ScormCmd {
    public string key;
  }

  //public class Cmd_getArchives : Cmd { //vrati vsechna id, kde ProductId zacina na <ProductId>|
  //}

  //public class Cmd_getArchivesResult : Cmd {
  //  public int[] prodVariants;
  //}

  public class Cmd_createArchive : ScormCmd { //prejmenuje ProductId na <ProductId>|<Id> a vrati Id
  }

  public class Cmd_testResults : ScormCmd { //vrati vsechny vysledky jednoho testu
  }

  //***************** PDF 
  public class Cmd_testCert : ScormCmd {
    public Langs loc;
  }


  //public class Cmd_writeModuleResults : Cmd {
  //  public string key;
  //  public string data;
  //  public string shortData;
  //}

  public class Cmd_saveUserData : ScormCmd {
    public string[][] data; //key - shortData value
  }

  //public class Cmd_readCrsInfo : Cmd {
  //}

  //public class Cmd_getMetaCourse : Cmd {
  //}

  //public class Cmd_setMetaCourse : Cmd {
  //  public string data;
  //}

}