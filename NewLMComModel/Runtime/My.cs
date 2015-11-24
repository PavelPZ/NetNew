using LMComLib;
using LMNetLib;
using Login;
using Microsoft.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace Login {

  /*********************** My Learning *************************/

  //public class MyDataNew {
  //  public LMCookieJS cookie;
  //  public string IP; //ip adresa v dobe spusteni session
  //  public Admin.JSCompany[] Companies;
  //}

  public class MyData {
    public Int64 UserId; //lmcom email
    public string IP; //ip adresa v dobe spusteni session
    public MyCompany[] Companies;
    public Role Roles;
  }

  public class MyCompany {
    public string Title;
    public int Id;
    public LMComLib.CompUserRole RoleEx;
    public MyCourse[] Courses;
    public Admin.CmdGetDepartmentResult DepTree; //tree s company departments
    public int? DepSelected; //selected department
    public CourseMeta.product[] companyProducts; //kurzy, vyrtvorene pod hlavickou company v LM Authorovi
    public Int64 PublisherOwnerUserId; //pro pripad, ze tato company je fake company individualniho publishear - jeho User.compId 
  }

  public class CmdMyInit : LMComLib.Cmd {
  }

  public class CmdSaveDepartment {
    public Int64 userId;
    public int companyId;
    public int? departmentId;
  }

  public class CmdEnterLicKey : LMComLib.Cmd {
    public int CompLicId;
    public int Counter;
  }

  public enum EnterLicenceResult {
    ok, //zalozena
    added, //zalozena jiz drive
    used, //pouzita nekym jinym
    wrongId, //licenceId neexistuje
    wrongCounter, //counter je mensi nez last lic counter
  }

  public class CmdEnterLicKeyResult {
    public EnterLicenceResult res;
    public int CompanyId;
    public string ProductId;
    public Int64 Expired;
  }

  //*************************************************
  //*************** human eval
  //*************************************************

  //*** sprava hodnotitelu, get
  public class CmdHumanEvalManagerEvsGet {
    public Int64 lmcomId;
    public int companyId;
  }
  public class CmdHumanEvalManagerEvsItem {
    public int companyUserId; //CompanyUser.email evaluatora
    public string email; //jeho compId
    public string name; //a jmeno
    public HumanEvalInfo[] evalInfos;
  }

  //*** sprava hodnotitelu, save
  public class CmdHumanEvalManagerEvsSave {
    public int companyUserId; //==0 => novy hodnotitel (hleda se pomoci compId), <0 => delete
    //novy hodnotitel:
    public int companyId;
    public string email;
    public HumanEvalInfo[] evalInfos;
  }


  //*** vsechny jazyky testu k vyhodnoceni
  public class CmdHumanEvalManagerLangs {
    public Int64 lmcomId;
    public int companyId;
  }

  public class CmdHumanEvalManagerLangsResult {
    public HumanEvalManagerLangItem[] lines;
  }

  public class HumanEvalManagerLangItem {
    public LineIds line;
    public int count;
  }

  //*** dej informace, potrebne k zaukolovani hodnotitelu
  public class CmdHumanEvalManagerGet {
    public Int64 lmcomId;
    public LineIds courseLang;
    public int companyId;
  }

  public class CmdHumanEvalManagerGetResult {
    public CmdHumanEvaluatorGet[] evaluators; //vsechny evaluatori vcetne jejich nesplnenych ukolu
    public CmdHumanStudent[] toEvaluate; //vsechny jeste neprirazene ukoly
  }

  public class CmdHumanEvaluatorGet {
    public int companyUserId; //CompanyUser.email evaluatora
    public string email; //jeho compId
    public string name; //a jmeno
    public CmdHumanStudent[] toDo; //ukoly, co jeste nema splnene
  }

  public class CmdHumanStudent {
    public int courseUserId; //user.email
    public string productId; //identifikace produktu k evaluaci
    public long assigned; //datum prirazeni evaluace, javascript Utils.nowToNum() funkci
  }

  //*** zaukolovani hodnotitelu
  public class CmdHumanEvalManagerSet {
    public CmdHumanEvaluatorSet[] evaluators; //nove zaukolovani evaluatoru
  }

  public class CmdHumanEvaluatorSet {
    public int companyUserId; //CompanyUser.email evaluatora
    public int[] courseUserIds; //kurzy k evaluaci
  }

  //*** eval ukoly pro lmcomid evaluatora
  public class CmdHumanEvalGet {
    public Int64 lmcomId;
    public int companyId;
  }
  public class CmdHumanEvalGetResult {
    public int companyUserId;
    public HumanEvalGetResultItem[] todo;
  }

  public class HumanEvalGetResultItem {
    public int courseUserId; //user.email
    public int companyUserId; //CompanyUser.email evaluatora
    public string productId; //identifikace produktu k evaluaci
    public long assigned; //datum prirazeni evaluace, javascript Utils.nowToNum() funkci
  }

  //*** jeden testGlobalAdmin k vyhodnoceni
  public class CmdHumanEvalTest {
    public int companyUserId; //CompanyUser.email evaluatora
    public int courseUserId;
  }
  public class CmdHumanEvalTestResult {
    public long testUser_lmcomId; //user.email
    public string[] urls;
  }

  //*************************************************
  //*************** reports
  //*************************************************
  public enum CmdReportType {
    evaluators,
    test,
  }
  public class CmdReport {
    public long self; //email zalogovaneho uzivatele
    public int companyId; //aktualni firma
    public CmdReportType type;
  }

  public class CmdPaymentReport : CmdReport {
    public HumanPayment cfg;
  }

  public class HumanPaymentsCfg {
    public HumanPayment[] payments;
  }
  public class HumanPayment {
    public long created; //datum vytvoreni zaznamu 
    public UInt64 lastRowVersion; //last CourseDatas.RowVersion, first je rovno lastRowVersion+1 predchoziho HumanPayment
    public Dictionary<string, Double> prices; //cennik
  }

}

namespace NewData {

  public static class My {

    static My() {

      /* Init User MyLearning page */
      Handlers.CmdService.registerCommand<CmdMyInit, MyData>(par => {
        return new RpcResponse(Init(par.lmcomId));
      });

      /* CmdEnterLicKey */
      Handlers.CmdService.registerCommand<CmdEnterLicKey, CmdEnterLicKeyResult>(par => {
        return new RpcResponse(AddLicence(par.CompLicId, par.lmcomId, par.Counter));
      });

      /* CmdSaveDepartment */
      Handlers.CmdService.registerCommand<CmdSaveDepartment, bool>(par => {
        return new RpcResponse(SaveDepartment(par));
      });


      //****** Human Eval
      /* CmdHumanEvalManagerLangs */
      Handlers.CmdService.registerCommand<CmdHumanEvalManagerLangs, CmdHumanEvalManagerLangsResult>(par => {
        return new RpcResponse(humanEvalManagerLangs(par));
      });

      /* CmdHumanEvalManagerEvsGet */
      Handlers.CmdService.registerCommand<CmdHumanEvalManagerEvsGet, CmdHumanEvalManagerEvsItem>(par => {
        return new RpcResponse(humanEvalManagerEvsGet(par));
      });

      /* CmdHumanEvalManagerEvsSave */
      Handlers.CmdService.registerCommand<CmdHumanEvalManagerEvsSave, bool>(par => {
        return new RpcResponse(humanEvalManagerEvsSave(par));
      });

      /* CmdHumanEvalManagerGet */
      Handlers.CmdService.registerCommand<CmdHumanEvalManagerGet, CmdHumanEvalManagerGetResult>(par => {
        return new RpcResponse(humanEvalManagerGet(par));
      });

      /* CmdHumanEvalManagerSet */
      Handlers.CmdService.registerCommand<CmdHumanEvalManagerSet, bool>(par => {
        return new RpcResponse(humanEvalManagerSet(par));
      });

      /* CmdHumanEvalGet */
      Handlers.CmdService.registerCommand<CmdHumanEvalGet, CmdHumanEvalGetResult>(par => {
        return new RpcResponse(humanEvalGet(par));
      });

      /* CmdHumanEvalTest */
      Handlers.CmdService.registerCommand<CmdHumanEvalTest, CmdHumanEvalTestResult>(par => {
        return new RpcResponse(humanEvalTest(par));
      });

      /* CmdReport */
      Handlers.CmdService.registerCommand<CmdReport, bool>(par => {
        return new RpcResponse(report(par));
      });
    }

    static bool checkRole(Container db, Int64 lmcomUserId, int companyId, CompRole role) {
      var r = db.CompanyUsers.Where(c => c.CompanyId == companyId && c.UserId == lmcomUserId).Select(c => c.Roles).FirstOrDefault();
      return (r & (long)role) == (long)role;
    }

    /******** REPORT *******************************************************************************************/
    static bool report(CmdReport par) {
      string fileName;
      byte[] data = excelReport.lib2.getResponse(par, out fileName);
      NewModel.Lib.downloadResponse(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName + ".xlsx");
      return true;
    }


    /******** HUMAN EVAL *******************************************************************************************/

    static CmdHumanEvalManagerEvsItem[] humanEvalManagerEvsGet(CmdHumanEvalManagerEvsGet par) {
      var db = Lib.CreateContext();
      //kontrola role
      if (!checkRole(db, par.lmcomId, par.companyId, CompRole.HumanEvalManager)) throw new Exception("Missing HumanEvalManager role for humanEvalManagerGet call");

      var evaluators = db.CompanyUsers.Where(cu => cu.CompanyId == par.companyId && (cu.Roles & (long)CompRole.HumanEvalator) == (long)CompRole.HumanEvalator).Select(cu => new { companyUserId = cu.Id, email = cu.User.EMail, name = cu.User.FirstName + " " + cu.User.LastName, cu.RolePar }).ToArray();
      return evaluators.Select(e => new CmdHumanEvalManagerEvsItem {
        companyUserId = e.companyUserId,
        email = e.email,
        name = e.name,
        evalInfos = CompUserRole.FromString(e.RolePar).HumanEvalatorInfos
      }).ToArray();
    }

    static bool humanEvalManagerEvsSave(CmdHumanEvalManagerEvsSave par) {
      var db = Lib.CreateContext();
      CompanyUsers cu;
      if (par.companyUserId < 0) {
        cu = db.CompanyUsers.First(c => c.Id == -par.companyUserId);
        //var ex = cu.RoleParEx;
        var ex = Lib.getRoleParEx(cu);
        ex.HumanEvalatorInfos = null; ex.Role &= ~CompRole.HumanEvalator;
        //cu.RoleParEx = ex;
        Lib.setRoleParEx(cu, ex);
        db.SaveChanges();
        return true;
      } else if (par.companyUserId == 0) {
        par.email = par.email.ToLower();
        var u = db.Users.Where(c => c.EMail == par.email).FirstOrDefault(); if (u == null) return false;
        cu = db.CompanyUsers.Where(c => c.UserId == u.Id && c.CompanyId == par.companyId).FirstOrDefault();
        if (cu == null) db.CompanyUsers.Add(cu = new CompanyUsers { CompanyId = par.companyId, UserId = u.Id, Created = DateTime.UtcNow });
      } else
        cu = db.CompanyUsers.First(c => c.Id == par.companyUserId);
      //CompUserRole rx = cu.RoleParEx;
      var rx = Lib.getRoleParEx(cu);
      rx.Role = rx.Role |= CompRole.HumanEvalator;
      rx.HumanEvalatorInfos = par.evalInfos;
      //cu.RoleParEx = rx;
      Lib.setRoleParEx(cu, rx);
      db.SaveChanges();
      return true;
    }

    private static CmdHumanEvalManagerLangsResult humanEvalManagerLangs(CmdHumanEvalManagerLangs par) {
      var db = Lib.CreateContext();
      //kontrola role
      if (!checkRole(db, par.lmcomId, par.companyId, CompRole.HumanEvalManager)) throw new Exception("Missing HumanEvalManager role for humanEvalManagerGet call");
      var todoFlag = (long)(CourseModel.CourseDataFlag.needsEval | CourseModel.CourseDataFlag.testEx);
      var prodIds = db.CourseUsers.
        Where(cu => cu.CompanyUser.CompanyId == par.companyId && cu.CourseDatas.Any(cd => (cd.Flags & todoFlag) == todoFlag)).
        Select(cu => cu.ProductId).ToArray();
      //var prodMeta = CourseMeta.Lib.runtimeProdExpanded();
      var prodLines = prodIds.
        Select(p => CourseMeta.LibLow.getRuntimeProd(p)).
        Where(p => p != null).
        Select(p => p.line).
        GroupBy(l => l).
        Select(g => new HumanEvalManagerLangItem { line = g.Key, count = g.Count() }).
        ToArray();
      return new CmdHumanEvalManagerLangsResult { lines = prodLines };
    }

    static CmdHumanEvalManagerGetResult humanEvalManagerGet(CmdHumanEvalManagerGet par) {
      var db = Lib.CreateContext();
      //kontrola role
      if (!checkRole(db, par.lmcomId, par.companyId, CompRole.HumanEvalManager)) throw new Exception("Missing HumanEvalManager role for humanEvalManagerGet call");

      var todoFlag = (long)(CourseModel.CourseDataFlag.needsEval | CourseModel.CourseDataFlag.testEx);
      //nevyhodnocene courseUsers
      var todoCourseUsers = db.CourseUsers.
        Where(cu => cu.CompanyUser.CompanyId == par.companyId && cu.CourseDatas.Any(cd => (cd.Flags & todoFlag) == todoFlag)).
        Select(cu => new {
          cu.Id,
          cu.HumanCompanyUserId,
          cu.HumanAssigned,
          cu.ProductId
        }).ToArray();
      //vsechny firemni evaluators
      var evaluators = db.CompanyUsers.Where(cu => cu.CompanyId == par.companyId && (cu.Roles & (long)CompRole.HumanEvalator) == (long)CompRole.HumanEvalator).Select(cu => new { cu.Id, cu.User.EMail, cu.User.FirstName, cu.User.LastName, cu.RolePar }).ToArray();

      //filter pres jazyk
      CourseMeta.product tempProd;
      //var prodMeta = CourseMeta.Lib.runtimeProdExpanded();
      todoCourseUsers = todoCourseUsers.Where(c => (tempProd = CourseMeta.LibLow.getRuntimeProd(c.ProductId)) != null && tempProd.line == par.courseLang).ToArray();
      evaluators = evaluators.Where(e => LMComLib.CompUserRole.FromString(e.RolePar).HumanEvalatorInfos.Any(h => h.lang == par.courseLang)).ToArray();

      return new CmdHumanEvalManagerGetResult {
        evaluators = evaluators.Select(dbEv => new CmdHumanEvaluatorGet {
          companyUserId = dbEv.Id,
          email = dbEv.EMail,
          name = dbEv.FirstName + " " + dbEv.LastName,
          toDo = todoCourseUsers.Where(di => di.HumanCompanyUserId == dbEv.Id).Select(cu => new CmdHumanStudent { assigned = LowUtils.dateToNum(cu.HumanAssigned), productId = cu.ProductId, courseUserId = cu.Id }).ToArray()
        }).ToArray(),
        toEvaluate = todoCourseUsers.Where(di => evaluators.All(e => e.Id != di.HumanCompanyUserId)).Select(cu => new CmdHumanStudent { assigned = LowUtils.dateToNum(cu.HumanAssigned), productId = cu.ProductId, courseUserId = cu.Id }).ToArray()
      };

    }

    static bool humanEvalManagerSet(CmdHumanEvalManagerSet par) {
      var cuIds = par.evaluators.SelectMany(ev => ev.courseUserIds).ToArray();
      foreach (var interv in LowUtils.intervals(cuIds.Length, 100)) {
        var db = Lib.CreateContext();
        var toRead = cuIds.Skip(interv.skip).Take(interv.take).ToArray();
        foreach (var crsUser in db.CourseUsers.Where(cu => toRead.Contains(cu.Id))) {
          var evalId = par.evaluators.Where(e => e.courseUserIds.Contains(crsUser.Id)).Select(e => e.companyUserId).First();
          if (crsUser.HumanCompanyUserId == evalId) continue;
          crsUser.HumanCompanyUserId = evalId;
          crsUser.HumanAssigned = DateTime.UtcNow;
        }
        db.SaveChanges();
      }
      return true;
    }

    static CmdHumanEvalGetResult humanEvalGet(CmdHumanEvalGet par) {
      var db = Lib.CreateContext();
      int companyUserId = db.CompanyUsers.Where(cu => cu.UserId == par.lmcomId && cu.CompanyId == par.companyId).Select(cu => cu.Id).First();
      var flag = (long)(CourseModel.CourseDataFlag.needsEval | CourseModel.CourseDataFlag.testEx);
      var todoCourseUsers = db.CourseUsers.
        Where(cu => cu.HumanCompanyUserId == companyUserId && cu.CourseDatas.Any(cd => !string.IsNullOrEmpty(cd.Data) && (cd.Flags & flag) == flag)).
        Select(cu => new {
          courseUserId = cu.Id,
          cu.HumanAssigned,
          cu.ProductId,
        }).ToArray();
      return new CmdHumanEvalGetResult {
        companyUserId = companyUserId,
        todo = todoCourseUsers.Select(cu => new HumanEvalGetResultItem {
          assigned = LowUtils.dateToNum(cu.HumanAssigned),
          productId = cu.ProductId,
          courseUserId = cu.courseUserId
        }).ToArray()
      };
    }

    static CmdHumanEvalTestResult humanEvalTest(CmdHumanEvalTest par) {
      var db = Lib.CreateContext();
      var flag = (long)(CourseModel.CourseDataFlag.pcCannotEvaluate | CourseModel.CourseDataFlag.testEx);
      //long flag = (long)(CourseModel.CourseDataFlag.pcCannotEvaluate | CourseModel.CourseDataFlag.done);
      var urls = db.CourseDatas.Where(cd => cd.CourseUserId == par.courseUserId && cd.CourseUser.HumanCompanyUserId == par.companyUserId && (cd.Flags & flag) == flag).Select(cd => new { cd.Key, cd.CourseUser.CompanyUser.UserId }).ToArray();
      return new CmdHumanEvalTestResult { urls = urls.Select(u => u.Key).ToArray(), testUser_lmcomId = urls.First().UserId };
    }

    /******** MY *******************************************************************************************/

    static bool SaveDepartment(CmdSaveDepartment par) {
      var db = Lib.CreateContext();
      CompanyUsers compUser = db.CompanyUsers.First(cu => cu.CompanyId == par.companyId && cu.UserId == par.userId);
      compUser.DepartmentId = par.departmentId;
      Lib.SaveChanges(db);
      return true;
    }

    public static MyData Init(Int64 userId) {
      var db = Lib.CreateContext();

      var lics = db.UserLicences.Where(u => u.CourseUser.CompanyUser.UserId == userId).Select(l => new {
        l.Started,
        l.UserId,
        l.CompanyLicence.Days,
        l.CompanyLicence.CompanyId,
        l.CourseUser.ProductId,
        l.LicenceId,
        l.Counter,
      }).ToArray();

      //EF7
      //z nejakych pricin hlasi chybu, dotaz predelan
      //var compUserInfo = db.Users.Include(c => c.CompanyUsers).Where(u => u.Id == userId).Select(u => new {
      //  u.Roles,
      //  compUsers = u.CompanyUsers.Select(cu => new {
      //    cu.CompanyId,
      //    cu.Company.Title,
      //    cu.RolePar,
      //    cu.Roles,
      //    cu.DepartmentId,
      //    PublisherOwner = cu.Company.PublisherOwners.FirstOrDefault()
      //  })
      //}).FirstOrDefault();

      var user = db.Users.Include(c => c.CompanyUsers).ThenInclude(c => c.Company).Where(u => u.Id == userId).FirstOrDefault();
      var compUsers = user != null ? user.CompanyUsers.ToArray() : null;
      var compUserInfo = user != null ? new {
        Roles = user.Roles,
        compUsers = compUsers== null ? null : compUsers.Select(cu => new {
          cu.CompanyId,
          cu.Company.Title,
          cu.RolePar,
          cu.Roles,
          cu.DepartmentId,
          PublisherOwner = cu.Company.PublisherOwners.FirstOrDefault()
        })
      } : null;

      if (compUserInfo == null) return new MyData() { UserId = userId };
      var res = new MyData() {
        IP = HttpContext.Current == null ? null : LowUtils.GetIpAddress(HttpContext.Current.Request),
        Roles = (Role)compUserInfo.Roles,
        UserId = userId,
        Companies = compUserInfo.compUsers.Select(cu => new MyCompany() {
          RoleEx = LMComLib.CompUserRole.create(cu.RolePar, (CompRole)cu.Roles),
          Title = cu.Title,
          PublisherOwnerUserId = cu.PublisherOwner == null ? 0 : cu.PublisherOwner.Id,
          Id = cu.CompanyId,
          Courses = lics.Where(li => li.CompanyId == cu.CompanyId).GroupBy(li => li.ProductId).Select(prodLicsGrp => new MyCourse() {
            ProductId = prodLicsGrp.Key,
            Expired = LowUtils.DateToJsGetTime(prodLicsGrp.Select(d => d.Started.AddDays(d.Days)).Max()),
            LicCount = prodLicsGrp.Count(),
            LicenceKeys = prodLicsGrp.Select(l => l.LicenceId.ToString() + "|" + l.Counter.ToString()).ToArray()
          }).ToArray(),
          DepTree = NewData.AdminServ.GetDepartment(cu.CompanyId),
          DepSelected = cu.DepartmentId
          //}).Where(c => (c.RoleEx.Role != 0 && c.RoleEx.Role != CompRole.HumanEvalator)|| c.Courses.Length > 0).ToArray()
        }).Where(c => (c.RoleEx.Role != 0) || c.Courses.Length > 0).ToArray()
      };
      //Publisher products
      foreach (var comp in res.Companies) comp.companyProducts = Author.Server.getCompanyProducts(comp.Id);
      return res;
    }



    /*counter a companyLicenceId je jednoznacny klic do tabulky UserLicence (a zaroven informace, zakodovana v licencnim klici). 
     * Znamena to, ze kazdy klic z 0..CompanyLicences.LastCounter-1 muze byt pouzti jen jednou.*/
    public static CmdEnterLicKeyResult AddLicence(int companyLicenceId, Int64 lmcomUserId, int counter) {
      var db = Lib.CreateContext(); var res = new CmdEnterLicKeyResult();

      var compLic = db.CompanyLicences.Where(l => l.Id == companyLicenceId).FirstOrDefault(); //nacita se kvuli zjisteni posledniho pouzitelneho counteru
      if (compLic == null) { res.res = EnterLicenceResult.wrongId; return res; }
      if (counter > compLic.LastCounter) { res.res = EnterLicenceResult.wrongCounter; return res; } //nemelo by nastat

      //licence, odpovidajici klici: pro jakeho je uzivatele?
      var usedLics = db.UserLicences.Where(ul => ul.LicenceId == companyLicenceId && ul.Counter == counter).Select(ul => ul.CourseUser.CompanyUser.UserId).ToArray();
      //pro self: self jiz licenci pouzil
      if (usedLics.Any(l => l == lmcomUserId)) { res.res = EnterLicenceResult.added; return res; } //licence pro tohoto uzivatele jiz zalozena
      //pro jineho: nekdo jiny licenci pouzil
      if (usedLics.Any(l => l != lmcomUserId)) { res.res = EnterLicenceResult.used; return res; }

      //vse OK, zaregistruj pouziti licence (klice)
      CourseUsers crsUser; CompanyUsers compUser;
      DateTime startDate = DateTime.UtcNow;
      if (Lib.adjustCourseUser(db, lmcomUserId, compLic.CompanyId, compLic.ProductId, out crsUser, out compUser)) {
        var lastLic = crsUser.UserLicences.OrderByDescending(ul => ul.Started).Select(ul => new { ul.Started, ul.CompanyLicence.Days }).FirstOrDefault();
        if (lastLic != null) {
          var dt = lastLic.Started.AddDays(lastLic.Days);
          if (dt > startDate) startDate = dt;
        }
      }

      //Zalozeni nove licence
      db.UserLicences.Add(new UserLicences() {
        Started = startDate,
        CourseUser = crsUser,
        CompanyLicence = compLic,
        Counter = counter,
        Created = DateTime.UtcNow,
      });

      Lib.SaveChanges(db);

      //validTo = startDate.AddDays(compLic.Days);
      res.res = EnterLicenceResult.ok;
      res.CompanyId = compLic.CompanyId;
      res.ProductId = compLic.ProductId;
      res.Expired = LowUtils.DateToJsGetTime(startDate.AddDays(compLic.Days));
      return res;

    }

  }
}
