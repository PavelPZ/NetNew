using LMComLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Http;

namespace Handlers {

  public static class CSharpToTypescript {
    public static Object[] allObjects = new Object[] { 
        new LMComLib.Common(), new Login.Register(), new Admin.Register(),
        new Rew.LMComLibRegister(), new Rew.Register(), new Rew.RwRegister(),
        new schools.CourseRegister(), new schools.Register(), new schools.LMComLibRegister(),
        new scorm.Register(), new testMe.Register(),
        new CourseModel.Register(),
        new CourseMeta.Register(),
        new AzureData.Register(),
        new metaJS.Register()
    };
    public static void genAll() {
      CSharpToTypeScript.GenerateFromInfos(allObjects);
      var js = jsWebApiProxy.jsProxyGenerator.LMGetJSProxy();
      File.WriteAllText(@"d:\LMCom\rew\Web4\Schools\GenProxy.ts", js, Encoding.UTF8);
    }
  }

  public static class Request {
    static Request() {
      foreach (var t in XExtension.Create<Type>(
        Machines.isPZComp() || Machines.isFE5() || ReleaseDeploy.Lib.config<Packager.WebSoftwareBatch>().isJSCramblerServer ? typeof(Handlers.Licence) : typeof(bool),
        //ReleaseDeploy.lib.config<Packager.WebBatch>().isJSCramblerServer ? typeof(Handlers.Licence) : typeof(bool),
        typeof(NewData.ScormExServer),
        //typeof(NewDataNet35.ScormExServer),
        typeof(LMComLib.Logger),
        typeof(NewData.Login),
        //typeof(NewData.Test),
        typeof(NewData.My),
        typeof(NewData.AdminServ),
        typeof(NewData.Schools),
        typeof(NewData.Scorm),
        typeof(Author.Server)
        //,typeof(testMe.Skrivanek)

     )) System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(t.TypeHandle);
    }
    public static void Process(HttpContext context, Action<Exception> catchExp) {
      RewRpcLib.ProcessRequest(context, catchExp);
    }
  }
}

namespace LMComLib {
  public class Common : ICSharpToTypeScript {

    //public string code() { return null; }
    public IEnumerable<Type> Types() {
      yield return typeof(Cmd); 
      yield return typeof(CmdEMail);
      yield return typeof(LMCookieJS);
      yield return typeof(MyCompanyLow);
      yield return typeof(CompUserRole);
      yield return typeof(HumanEvalInfo);
      //yield return typeof(HumanEvalLang);
      yield return typeof(MyCourse);
      yield return typeof(RpcResponse);
      yield return typeof(JSTyped);
      yield return typeof(lmConsoleSend);
    }
    public IEnumerable<Type> ExtendedTypes() {
      yield return typeof(CmdEMail);
      yield return typeof(LMCookieJS);
    }
    public IEnumerable<Type> Enums() {
      yield return typeof(CookieIds);
      yield return typeof(CompRole);
      yield return typeof(Role);
      yield return typeof(CourseIds);
      yield return typeof(Domains);
      yield return typeof(errorId);
      yield return typeof(Langs);
      yield return typeof(LMSSize);
      yield return typeof(LMSType);
      yield return typeof(Targets);
      yield return typeof(OtherType);
      yield return typeof(SubDomains);
      yield return typeof(LineIds);
      yield return typeof(SoundSrcId);
      yield return typeof(ExerciseStatus);
      yield return typeof(SoundPlayerType);
      yield return typeof(BooleanEx);
      yield return typeof(VerifyStates);
    }
    public IEnumerable<Type> ConstEnums() { yield break; }
    public string TsPath() { return Machines.rootPath + @"JsLib\JS\GenLMComLib.ts"; }
    public string Module() { return "LMComLib"; }
    public IEnumerable<string> Uses() { yield break; }
    public bool generateFeature(FeatureType type) {
      switch (type) {
        case FeatureType.LangToEADir: return true;
        case FeatureType.LineToLangNames: return true;
        case FeatureType.LangToLineNames: return true;
        case FeatureType.LangLists: return true;
        default: return false;
      }
    }
    public CourseModel.jsonMLMeta getJsonMLMeta() { return null; }
  }
}

namespace AzureData {
  public class Register : ICSharpToTypeScript {
    public IEnumerable<Type> Types() {
      yield break;
    }
    public IEnumerable<Type> ExtendedTypes() {
      yield break;
    }
    public IEnumerable<Type> Enums() {
      //yield return typeof(userPropIds);
      //yield return typeof(companyPropIds);
      //yield return typeof(courseDataOper);
      yield break;
    }
    public IEnumerable<Type> ConstEnums() { yield break; }

    public string TsPath() { return Machines.rootPath + @"schools\GenAzure.ts"; }
    public string Module() { return "AzureData"; }

    public IEnumerable<string> Uses() { yield break; }
    public bool generateFeature(FeatureType type) { return false; }
    public CourseModel.jsonMLMeta getJsonMLMeta() { return null; }
  }

}
namespace CourseModel {

  public class Register : ICSharpToTypeScript {
    //public string code() {
    //return null; // JsCode();
    //}
    public IEnumerable<Type> Types() {
      return getAll.Types();
    }
    public IEnumerable<Type> ExtendedTypes() {
      yield break;
    }
    public IEnumerable<Type> Enums() {
      return getAll.Enums();
    }
    public IEnumerable<Type> ConstEnums() { yield break; }
    public string TsPath() { return Machines.rootPath + @"Courses\GenCourseModel.ts"; }
    public string Module() { return "CourseModel"; }

    public IEnumerable<string> Uses() { yield return "../jslib/js/GenLMComLib.ts"; yield return "../schools/GenSchools.ts"; }
    public bool generateFeature(FeatureType type) { return type == FeatureType.gaffFill_normTable; }

    //public static string JsCode() {
    //  return "export var tagInfos = " + JsonConvert.SerializeObject(CourseModel.TagStatic.tagInfo) + ";";
    //}
    public CourseModel.jsonMLMeta getJsonMLMeta() { return Lib.courseModelJsonMLMeta; }
  }

}

namespace CourseMeta {
  public class Register : ICSharpToTypeScript {
    //public string code() { return null; }
    public IEnumerable<Type> Types() {
      yield return typeof(data);
      yield return typeof(products);
      yield return typeof(product);
      yield return typeof(sitemap);
      yield return typeof(project);
      yield return typeof(publisher);
      yield return typeof(ex);
      yield return typeof(ptr);
      yield return typeof(dynamicModuleData);
      yield return typeof(testTaskGroup);
      yield return typeof(taskTestSkill);
      yield return typeof(multiTask);
      yield return typeof(taskCourse);
      yield return typeof(mod);
      yield return typeof(test);
      yield return typeof(taskTestInCourse);
    }
    public IEnumerable<Type> ExtendedTypes() {
      yield break;
    }
    public IEnumerable<Type> Enums() {
      yield return typeof(runtimeType);
      yield return typeof(childMode);
      yield return typeof(dictTypes);
      yield return typeof(testNeeds);
    }
    public IEnumerable<Type> ConstEnums() { yield break; }
    public string TsPath() { return Machines.rootPath + @"Courses\GenCourseMeta.ts"; }
    public string Module() { return "CourseMeta"; }

    public IEnumerable<string> Uses() { yield break; }// return "../jslib/js/GenLMComLib.ts"; yield return "../schools/GenSchools.ts"; }
    public bool generateFeature(FeatureType type) { return false; }
    public CourseModel.jsonMLMeta getJsonMLMeta() { return CourseModel.Lib.courseMetaJsonMLMeta; }
  }

}

namespace testMe {

  public class Register : ICSharpToTypeScript {

    //public string code() { return null; }
    public IEnumerable<Type> Types() {
      yield return typeof(userData);
      yield return typeof(multiUserData);
      yield return typeof(skillUserData);
      yield return typeof(interrupt);
      yield return typeof(result);
      yield return typeof(skillResult);

      //yield return typeof(CmdSkrivanek);
      //yield return typeof(CmdSkrivanekResult);
      //yield return typeof(CmdSkrivanekSuggests);
    }
    public IEnumerable<Type> ExtendedTypes() {
      yield break;
      //yield return typeof(CmdSkrivanek);
      //yield return typeof(CmdSkrivanekSuggests);
    }
    public IEnumerable<Type> Enums() {
      //yield return typeof(Skills);
      yield return typeof(Status);
      //yield return typeof(CmdSkrivanekErrors);
    }
    public IEnumerable<Type> ConstEnums() { yield break; }
    public string TsPath() { return Machines.rootPath + @"testMe\GenTestModel.ts"; }
    public string Module() { return "testMe"; }

    public IEnumerable<string> Uses() { yield break; }
    public bool generateFeature(FeatureType type) { return false; }
    public CourseModel.jsonMLMeta getJsonMLMeta() { return null; }
  }
}

namespace schools {
  public class LMComLibRegister : ICSharpToTypeScript {
    //public string code() { return null; }
    public IEnumerable<Type> Types() {
      yield return typeof(LMComLib.DictInfo.Dict);
    }
    public IEnumerable<Type> ExtendedTypes() {
      yield break;
    }
    public IEnumerable<Type> Enums() {yield break;    }
    public IEnumerable<Type> ConstEnums() { yield break; }
    public string TsPath() { return Machines.rootPath + @"Schools\GenLMComLib.ts"; }
    public string Module() { return "LMComLib"; }

    public IEnumerable<string> Uses() { yield return "../jslib/js/GenLMComLib.ts"; }
    public bool generateFeature(FeatureType type) { return false; }
    public CourseModel.jsonMLMeta getJsonMLMeta() { return null; }
  }

  public class CourseRegister : ICSharpToTypeScript {

    //public string code() { return null; }

    public IEnumerable<Type> Types() {
      yield break;
    }
    public IEnumerable<Type> ExtendedTypes() {
      yield break;
    }
    public IEnumerable<Type> Enums() {
      yield break;
      //yield return typeof(Course.TestStatus);
      //yield return typeof(Course.Skills);
    }
    public IEnumerable<Type> ConstEnums() { yield break; }
    public string TsPath() { return Machines.rootPath + @"Schools\GenCourse.ts"; }
    public string Module() { return "Course"; }

    public IEnumerable<string> Uses() { yield break; }
    public bool generateFeature(FeatureType type) { return false; }
    public CourseModel.jsonMLMeta getJsonMLMeta() { return null; }
  }

  public class Register : ICSharpToTypeScript {
    //public string code() { return null; }

    public IEnumerable<Type> Types() {
      //yield return typeof(course);
      //yield return typeof(folder);
      //yield return typeof(lesson);
      //yield return typeof(mod);
      //yield return typeof(exStatic);
      //yield return typeof(page);
      //yield return typeof(root);
      yield return typeof(seeAlsoLink);
      //yield return typeof(grammarNode);
      //yield return typeof(SchoolCmdCreateTest);
      //yield return typeof(SchoolCmdTestInfo);
      //yield return typeof(SchoolCmdTestInfoResult);
      //yield return typeof(SchoolCmdTestInfoItem);
      //yield return typeof(SchoolCmdGetDict);
      yield return typeof(config);
      yield return typeof(licenceConfig);
      yield return typeof(licenceResponse);
      yield return typeof(licenceRespMonth);
      yield return typeof(licenceRespBuy);
      yield return typeof(licenceRespUser);
      yield return typeof(licenceRequest);
      yield return typeof(Dict);
      yield return typeof(DictItem);
      yield return typeof(DictItemRoot);
      yield return typeof(DictExWords);
      yield return typeof(DictCrsWords);
      yield return typeof(DictWords);
      yield return typeof(ModUser);
      //yield return typeof(SchoolCmdGetDictResult);
      //yield return typeof(taskInfo);
      //yield return typeof(metaCourse);
      //yield return typeof(metaTask);
      //yield return typeof(taskAttempt);
      //yield return typeof(courseAttempt);
      //yield return typeof(testAttempt);
      //yield return typeof(PretestMeta);
    }
    public IEnumerable<Type> ExtendedTypes() {
      yield break;
      //yield return typeof(SchoolCmdCreateTest);
      //yield return typeof(SchoolCmdTestInfo);
      //yield return typeof(SchoolCmdGetDict);
      //yield return typeof(config);
    }
    public IEnumerable<Type> Enums() {
      yield return typeof(persistTypes);
      yield return typeof(ExFormat);
      //yield return typeof(dictTypes);
      yield return typeof(seeAlsoType);
      yield return typeof(licenceResult);
      yield return typeof(versions);
      yield return typeof(DictEntryType);
      yield return typeof(scormDriver);
      yield return typeof(displayModes);

      //yield return typeof(taskStatus);
      //yield return typeof(taskTypes);

      //yield return typeof(LicenceType);
      //yield return typeof(DictUrltype);
    }
    public IEnumerable<Type> ConstEnums() { yield break; }
    public string TsPath() { return Machines.rootPath + @"Schools\GenSchools.ts"; }
    public string Module() { return "schools"; }

    public IEnumerable<string> Uses() { yield break; } // yield return "../jslib/js/GenLMComLib.ts"; yield return "GenCourse.ts"; }
    public bool generateFeature(FeatureType type) { return false; }
    public CourseModel.jsonMLMeta getJsonMLMeta() { return null; }
  }
}

namespace Rew {

  public class RwRegister : ICSharpToTypeScript {
    //public string code() { return null; }

    public IEnumerable<Type> Types() {
      yield break;
    }
    public IEnumerable<Type> ExtendedTypes() {
      yield break;
    }
    public IEnumerable<Type> ConstEnums() { yield break; }
    public IEnumerable<Type> Enums() {
      yield return typeof(Rw.CreativeCommonLic);
    }
    public string TsPath() { return Machines.rootPath + @"Rewise\GenRw.ts"; }
    public string Module() { return "Rw"; }

    public IEnumerable<string> Uses() { yield break; }
    public bool generateFeature(FeatureType type) { return false; }
    public CourseModel.jsonMLMeta getJsonMLMeta() { return null; }
  }

  public class LMComLibRegister : ICSharpToTypeScript {
    //public string code() { return null; }

    public IEnumerable<Type> Types() {
      yield break;
    }
    public IEnumerable<Type> ExtendedTypes() {
      yield break;
    }
    public IEnumerable<Type> Enums() {
      yield return typeof(Rw.CreativeCommonLic);
      yield return typeof(FactTypes);
    }
    public IEnumerable<Type> ConstEnums() { yield break; }
    public string TsPath() { return Machines.rootPath + @"Rewise\GenLMComLib.ts"; }
    public string Module() { return "LMComLib"; }

    public IEnumerable<string> Uses() { yield break; }
    public bool generateFeature(FeatureType type) { return false; }
    public CourseModel.jsonMLMeta getJsonMLMeta() { return null; }
  }

  public class Register : ICSharpToTypeScript {
    //public string code() { return null; }
    public IEnumerable<Type> Types() {
      yield return typeof(AddLessonCmd);
      yield return typeof(AddRewiseCmd);
      yield return typeof(BookGroupSrc);
      yield return typeof(BookSrc);
      yield return typeof(DelLessonCmd);
      yield return typeof(DelRewiseCmd);
      yield return typeof(FactSrc);
      yield return typeof(FactSound);
      yield return typeof(History);
      yield return typeof(LangToLearn);
      yield return typeof(LessonDataSrc);
      yield return typeof(LessonSrc);
      yield return typeof(LineSrc);
      yield return typeof(LocSrc);
      yield return typeof(LocSrcCmd);
      yield return typeof(LocSrcResult);
      yield return typeof(MyFact);
      yield return typeof(MyFactCmd);
      yield return typeof(MyLineOption);
      yield return typeof(MyRewise);
      yield return typeof(MyRewiseCmd);
      yield return typeof(MyRewiseOptions);
      yield return typeof(MyRewiseResult);
      yield return typeof(ReadLessonCmd);
      yield return typeof(SaveFactCmd);
      yield return typeof(SetMyOptionsCmd);
      yield return typeof(Crs2RwMapItem);
    }
    public IEnumerable<Type> ExtendedTypes() {
      yield return typeof(MyRewiseOptions);
      yield return typeof(MyRewise);
      yield return typeof(LangToLearn);
      yield return typeof(MyLineOption);
      yield return typeof(AddLessonCmd);
      yield return typeof(AddRewiseCmd);
      yield return typeof(DelLessonCmd);
      yield return typeof(DelRewiseCmd);
      yield return typeof(LocSrcCmd);
      yield return typeof(MyFactCmd);
      yield return typeof(MyRewiseCmd);
      yield return typeof(ReadLessonCmd);
      yield return typeof(SaveFactCmd);
      yield return typeof(SetMyOptionsCmd);
    }
    public IEnumerable<Type> Enums() {
      yield return typeof(FactStatus);
    }
    public IEnumerable<Type> ConstEnums() { yield break; }
    public string TsPath() { return Machines.rootPath + @"Rewise\GenRew.ts"; }
    public string Module() { return "Rew"; }

    public IEnumerable<string> Uses() { yield return "GenLMComLib.ts"; yield return "GenRw.ts"; }
    public bool generateFeature(FeatureType type) {
      switch (type) {
        case FeatureType.LineNames: return true;
        default: return false;
      }
    }
    public CourseModel.jsonMLMeta getJsonMLMeta() { return null; }
  }

}

namespace Login {

  public class Register : ICSharpToTypeScript {

    public IEnumerable<Type> Types() {
      yield return typeof(CmdAdjustUser);
      yield return typeof(CmdAdjustScormUser);
      yield return typeof(CmdAdjustScormUserResult);
      yield return typeof(CmdConfirmRegistration);
      yield return typeof(CmdChangePassword);
      //yield return typeof(CmdId);
      yield return typeof(CmdLmLogin);
      yield return typeof(CmdMyInit);
      yield return typeof(CmdSaveDepartment);
      yield return typeof(CmdProfile);
      yield return typeof(CmdRegister);
      yield return typeof(MyCompany);
      //yield return typeof(MyCourse);
      yield return typeof(MyData);
      //yield return typeof(MyDataNew);
      yield return typeof(CmdEnterLicKey);
      yield return typeof(CmdEnterLicKeyResult);

      yield return typeof(CmdHumanEvalManagerLangs);
      yield return typeof(CmdHumanEvalManagerLangsResult);
      yield return typeof(HumanEvalManagerLangItem);
      yield return typeof(CmdHumanEvalManagerEvsGet);
      yield return typeof(CmdHumanEvalManagerEvsItem);
      yield return typeof(CmdHumanEvalManagerEvsSave);
      yield return typeof(CmdHumanEvalManagerGet);
      yield return typeof(CmdHumanEvalManagerGetResult);
      yield return typeof(CmdHumanEvaluatorGet);
      yield return typeof(CmdHumanStudent);
      yield return typeof(CmdHumanEvalManagerSet);
      yield return typeof(CmdHumanEvaluatorSet);
      yield return typeof(CmdHumanEvalGet);
      yield return typeof(CmdHumanEvalGetResult);
      yield return typeof(HumanEvalGetResultItem);
      yield return typeof(CmdHumanEvalTest);
      yield return typeof(CmdHumanEvalTestResult);
      yield return typeof(CmdReport);
      yield return typeof(CmdPaymentReport);
      yield return typeof(HumanPaymentsCfg);
      yield return typeof(HumanPayment);
    }
    public IEnumerable<Type> ExtendedTypes() {
      yield return typeof(CmdSaveDepartment);
      yield return typeof(CmdAdjustUser);
      yield return typeof(CmdAdjustScormUser);
      yield return typeof(CmdLmLogin);
      //neni potreba Create objekt
      yield return typeof(CmdConfirmRegistration);
      yield return typeof(CmdChangePassword);
      yield return typeof(CmdMyInit);
      yield return typeof(CmdProfile);
      yield return typeof(CmdRegister);
      yield return typeof(CmdEnterLicKey);

      yield return typeof(CmdHumanEvalManagerLangs);
      yield return typeof(CmdHumanEvalManagerEvsGet);
      yield return typeof(CmdHumanEvalManagerEvsSave);
      yield return typeof(CmdHumanEvalManagerGet);
      yield return typeof(CmdHumanEvalManagerSet);
      yield return typeof(CmdHumanEvalGet);
      yield return typeof(CmdHumanEvalTest);
      yield return typeof(CmdReport);
      yield return typeof(CmdPaymentReport);
    }
    public IEnumerable<Type> Enums() {
      yield return typeof(CmdLmLoginError);
      //yield return typeof(CompRole);
      yield return typeof(Role);
      yield return typeof(EnterLicenceResult);
      yield return typeof(CmdReportType);
    }
    public IEnumerable<Type> ConstEnums() { yield break; }
    public string TsPath() { return Machines.rootPath + @"Login\GenLogin.ts"; }
    public string Module() { return "Login"; }

    public IEnumerable<string> Uses() { yield return "../jslib/js/GenLMComLib.ts"; }
    public bool generateFeature(FeatureType type) { return false; }
    public CourseModel.jsonMLMeta getJsonMLMeta() { return null; }
  }
}

namespace Admin {

  public class Register : ICSharpToTypeScript {
    //public string code() { return null; }
    public IEnumerable<Type> Types() {
      yield return typeof(CmdAlocKeys);
      yield return typeof(CmdGetProducts);
      yield return typeof(CmdGetDepartment);
      yield return typeof(CmdSetDepartment);
      yield return typeof(CmdGetDepartmentResult);
      yield return typeof(Department);
      yield return typeof(IntervalsConfig);
      yield return typeof(Intervals);
      yield return typeof(Interval);
      yield return typeof(CmdGetUsers);
      yield return typeof(CmdGetUsersResult);
      yield return typeof(CmdSetProducts);
      yield return typeof(CmdSetUsers);
      yield return typeof(Comp);
      yield return typeof(CompUserItem);
      yield return typeof(Product);
      yield return typeof(UserItem);
      yield return typeof(CmdDsgnReadFile);
      yield return typeof(CmdDsgnReadFiles);
      yield return typeof(CmdDsgnWriteDictWords);
      yield return typeof(CmdDsgnResult);

      yield return typeof(CmdGetPublProjects);
      yield return typeof(CmdGetPublProjectsResultItem);
      yield return typeof(CmdGetPublProjectsResult);
      yield return typeof(CmdCreatePublProjectItem);
      yield return typeof(CmdCreatePublProject);
      yield return typeof(CmdPublChangePassword);
      yield return typeof(CmdPublBuild);

      yield return typeof(DictEntryCmd);
      yield return typeof(DictEntryRes);

      yield return typeof(CmdXrefData);
      yield return typeof(CmdXrefDataResult);
      yield return typeof(xrefLink);

      //yield return typeof(CompanyMeta);
      //yield return typeof(DepartmentRoot);
      //yield return typeof(DepartmentUsages);
      //yield return typeof(DepartmentUsage);
      //yield return typeof(CompanyLicences);
      //yield return typeof(ProductLicence);
      //yield return typeof(UsedKey);
      //yield return typeof(GenLicKey);

      yield return typeof(CompanyUsers);
      yield return typeof(CompanyUser);
      yield return typeof(UserCompanies);
      yield return typeof(UserCompany);
      yield return typeof(UserProduct);
      //yield return typeof(UserKey);
    }
    public IEnumerable<Type> ExtendedTypes() {
      yield return typeof(CmdAlocKeys);
      yield return typeof(CmdGetProducts);
      yield return typeof(CmdGetUsers);
      yield return typeof(CmdGetUsersResult);
      yield return typeof(CmdSetProducts);
      yield return typeof(CmdSetUsers);
      yield return typeof(CmdGetDepartment);
      yield return typeof(CmdSetDepartment);
      yield return typeof(CmdDsgnReadFile);
      yield return typeof(CmdDsgnReadFiles);
      yield return typeof(CmdDsgnWriteDictWords);
      yield return typeof(DictEntryCmd);
      yield return typeof(CmdXrefData);
      yield return typeof(CmdGetPublProjects);
      yield return typeof(CmdCreatePublProject);
      yield return typeof(CmdPublChangePassword);
      yield return typeof(CmdPublBuild);
    }
    public IEnumerable<Type> Enums() {
      yield return typeof(DictEntryCmdType);
      yield return typeof(CmdXrefDataOpers);
      //yield return typeof(OKCrsReason);
      yield break;
    }
    public IEnumerable<Type> ConstEnums() { yield break; }
    public string TsPath() { return Machines.rootPath + @"Admin\GenAdmin.ts"; }
    public string Module() { return "Admin"; }

    public IEnumerable<string> Uses() { yield return "../login/GenLogin.ts"; }
    public bool generateFeature(FeatureType type) { return false; }
    public CourseModel.jsonMLMeta getJsonMLMeta() { return null; }
  }
}

namespace scorm {

  public class Register : ICSharpToTypeScript {
    //public string code() { return null; }

    public IEnumerable<Type> Types() {
      yield return typeof(ScormCmd);
      yield return typeof(Cmd_Logger);

      yield return typeof(Cmd_resetModules);
      yield return typeof(Cmd_readCrsResults);
      yield return typeof(Cmd_readModuleResults);
      yield return typeof(Cmd_saveUserData);
      //yield return typeof(Cmd_getArchives);
      //yield return typeof(Cmd_getArchivesResult);
      yield return typeof(Cmd_createArchive);
      yield return typeof(Cmd_testResults);
      yield return typeof(Cmd_testCert);

      //yield return typeof(Cmd_writeModuleResults);
      //yield return typeof(Cmd_readCrsInfo);
      //yield return typeof(Cmd_getMetaCourse);
      //yield return typeof(Cmd_setMetaCourse);

      //yield return typeof(Cmd_ReadTests);
      //yield return typeof(Cmd_SaveTest);
      //yield return typeof(Cmd_SaveTestWithModules);
      //yield return typeof(Cmd_SaveTestModule);
      //yield return typeof(Cmd_LoadTestModule);
      //yield return typeof(Cmd_LoadTestInfo);
    }
    public IEnumerable<Type> ExtendedTypes() {
      yield return typeof(Cmd_Logger);

      yield return typeof(Cmd_resetModules);
      yield return typeof(Cmd_readCrsResults);
      yield return typeof(Cmd_readModuleResults);
      yield return typeof(Cmd_saveUserData);
      //yield return typeof(Cmd_getArchives);
      yield return typeof(Cmd_createArchive);
      yield return typeof(Cmd_testResults);
      yield return typeof(Cmd_testCert);

      //yield return typeof(Cmd_writeModuleResults);
      //yield return typeof(Cmd_readCrsInfo);
      //yield return typeof(Cmd_getMetaCourse);
      //yield return typeof(Cmd_setMetaCourse);

      //yield return typeof(Cmd_ReadTests);
      //yield return typeof(Cmd_SaveTest);
      //yield return typeof(Cmd_SaveTestWithModules);
      //yield return typeof(Cmd_SaveTestModule);
      //yield return typeof(Cmd_LoadTestModule);
      //yield return typeof(Cmd_LoadTestInfo);
    }
    public IEnumerable<Type> Enums() {
      yield break;
    }
    public IEnumerable<Type> ConstEnums() { yield break; }
    public string TsPath() { return Machines.rootPath + @"schools\GenScorm.ts"; }
    public string Module() { return "scorm"; }

    public IEnumerable<string> Uses() { yield break; }
    public bool generateFeature(FeatureType type) { return false; }
    public CourseModel.jsonMLMeta getJsonMLMeta() { return null; }
  }
}