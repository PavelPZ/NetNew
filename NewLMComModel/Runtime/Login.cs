using LMComLib;
using LMNetLib;
using Login;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Login {

  /******************* login ************************/
  public class CmdAdjustUser {
    public LMComLib.OtherType provider;
    public string providerId;
    public string email;
    public string firstName;
    public string lastName;
  }
  public class CmdAdjustScormUser {
    public string companyHost;
    public string login;
    public string firstName;
    public string lastName;
    public bool isNotAttempted;
    public string productId; //isNotAttempted => vymaz data productId metakurzu
  }
  public class CmdAdjustScormUserResult {
    public LMCookieJS Cookie;
    public int companyId;
  }
  public enum CmdLmLoginError {
    no,
    userExist,
    cannotFindUser,
    passwordNotExists,
  }
  public class CmdLmLogin {
    public string login; //obsolete. Login je ulozen v EMail ve formatu "@login"
    public string email;
    public string password;
    public string otherData; //json s ostatnimi login udaji
    public string ticket;
  }
  //public class CmdId {
  //  public Int64 companyUserId;
  //}
  public class CmdProfile : LMComLib.Cmd {
    public LMCookieJS Cookie;
  }
  public class CmdRegister : CmdProfile {
    public string password;
    public SubDomains subSite;
  }

  public class CmdConfirmRegistration : LMComLib.Cmd {
  }
  public class CmdChangePassword : LMComLib.Cmd {
    public string oldPassword;
    public string newPassword;
  }

  public class CmdLoginTicket {
    public string name; //jmeno souboru s ticketem v App_Data\tickets\
    public string email; //compId uzivatele
    public string hash; //po zalogovani jdi na tento hash
  }

  public class JSUser {
    public string email;
    public string ETag;
    public LMCookieJS dataObj; //meta informace
    public Admin.UserCompanies companiesObj; //company informace: role, licence apod.
  }

}

namespace NewData {

  //public enum VerifyStates {
  //  ok = 0,
  //  waiting = 1, //uzivatel ceka na potvrzeni registrace
  //  prepared = 2, //uzivatel je pripraven nekym jinym, chybi mu ale zadani hesla
  //}

  public static class Login {

    static Login() {

      /* OTHER LOGIN */
      Handlers.CmdService.registerCommand<CmdAdjustUser, CmdProfile>(par => {
        //var prof = azure.lib.isActive ? azure.Login.OnOtherLogin(par.provider, par.providerId, par.compId, par.firstName, par.lastName) : OnOtherLogin(par.provider, par.providerId, par.compId, par.firstName, par.lastName);
        var prof = OnOtherLogin(par.provider, par.providerId, par.email, par.firstName, par.lastName);
        return new RpcResponse(new CmdProfile() { Cookie = prof });
      });

      /* SCORM LOGIN */
      Handlers.CmdService.registerCommand<CmdAdjustScormUser, CmdAdjustScormUserResult>(par => {
        return new RpcResponse(OnScormLogin(par));
      });

      /* LM LOGIN */
      Handlers.CmdService.registerCommand<CmdLmLogin, CmdProfile>(par => {
        //var prof = azure.lib.isActive ? azure.Login.OnLMLogin(par) : OnLMLogin(par);
        var prof = OnLMLogin(par);
        return prof == null ? new RpcResponse((int)CmdLmLoginError.cannotFindUser, null) : new RpcResponse(new CmdProfile() { Cookie = prof });
      });

      /* REGISTER */
      Handlers.CmdService.registerCommand<CmdRegister, Int64>(par => {
        //var lmcomId = azure.lib.isActive ? azure.Login.CreateLmUserStart(par.Cookie, par.password) : NewData.Login.CreateLmUserStart(par.Cookie, par.password);
        var lmcomId = NewData.Login.CreateLmUserStart(par.Cookie, par.password);
        return lmcomId < 0 ? new RpcResponse((int)CmdLmLoginError.userExist, null) : new RpcResponse(lmcomId);
      });

      /* SAVE CHANGED PROFILE */
      Handlers.CmdService.registerCommand<CmdProfile, bool>(par => {
        //if (azure.lib.isActive) azure.Login.SaveProfile(par.Cookie); else SaveProfile(par.Cookie);
        SaveProfile(par.Cookie);
        return new RpcResponse();
      });

      /* CONFIRM REGISTRATION */
      Handlers.CmdService.registerCommand<CmdConfirmRegistration, bool>(par => {
        NewData.Login.CreateLmUserEnd(par.lmcomId);
        return new RpcResponse();
      });

      /* CHANGE PASSWORD */
      Handlers.CmdService.registerCommand<CmdChangePassword, bool>(par => {
        //return (azure.lib.isActive ? azure.Login.ChangePassword(par.lmcomId, par.oldPassword, par.newPassword) : ChangePassword(par.lmcomId, par.oldPassword, par.newPassword)) ? new RpcResponse() : new RpcResponse((int)CmdLmLoginError.passwordNotExists, null);
        return (ChangePassword(par.lmcomId, par.oldPassword, par.newPassword)) ? new RpcResponse() : new RpcResponse((int)CmdLmLoginError.passwordNotExists, null);
      });

      /* SEND EMAIL */
      Handlers.CmdService.registerCommand<CmdEMail, bool>(par => {
        if (par.isForgotPassword) { //poslani emailu se zapomenutym heslem: dopln heslo z databaze do emailu
          //var psw = azure.lib.isActive ? azure.Login.GetPassword(par.To) : GetPassword(par.To);
          var psw = GetPassword(par.To);
          if (psw == null)
            return new RpcResponse((int)CmdLmLoginError.cannotFindUser, null);
          else
            par.Html = par.Html.Replace("$PASSWORD$", psw);
        }
        Emailer.Attachment att = par.isAtt ? new Emailer.Attachment(par.attFile, Encoding.UTF8.GetBytes(par.attContent), par.attContentType) : null;
        var err = LMComLib.Emailer.SendEMail(par.To, par.From, par.Subject, par.Html, att, par.Cc);
        return err == null ? new RpcResponse() : new RpcResponse() { error = 999, errorText = err, result = null };
      });

    }

    public struct El { public El(string e, string l) { email = e == null ? null : e.ToLower(); login = l == null ? null : l.ToLower(); } public string email; public string login;}

    public static LMCookieJS OnLMLogin(CmdLmLogin par) {
      var db = Lib.CreateContext();
      Users usr;
      if (par.ticket != null) {
        var ticketDir = Machines.rootPath + @"App_Data\tickets";
        var ticketFn = ticketDir + "\\" + par.ticket;
        if (!File.Exists(ticketFn)) return null;
        try {
          var email = XmlUtils.FileToObject<CmdLoginTicket>(ticketFn).email;
          usr = db.Users.FirstOrDefault(u => u.EMail == email);
          //vymazani nevyuzitych ticketu (5 minut zivotnosti)
          var now = DateTime.UtcNow;
          foreach (var tfn in Directory.GetFiles(ticketDir).Where(f => (now - File.GetCreationTime(f)).TotalMinutes > 5)) File.Delete(tfn);
        } finally { File.Delete(ticketFn); }
      } else {
        var psw = LowUtils.unpackStr(par.password);
        var el = new El(par.email, par.login);
        usr = db.Users.FirstOrDefault(u => u.EMail == el.email && u.Login == el.login);
        if (usr == null || usr.Password != psw) usr = null;
      }
      //Expression<Func<User, bool>> em, lg;
      //if (el.compId == null) em = u => u.compId == null; else em = u => u.compId == el.compId;
      //if (el.login == null) lg = u => u.Login == null; else lg = u => u.Login == el.login;
      //var userObj = db.Users.Where(em).Where(lg).FirstOrDefault();
      //var tp = userObj == null ? OtherType.no : (OtherType)userObj.OtherType;
      if (
        usr == null ||
        (VerifyStates)usr.VerifyStatus != VerifyStates.ok) return null;
      return user2cookie(usr);
    }

    public static LMCookieJS OnOtherLogin(OtherType otherType, string otherId, string email, string firstName, string lastName) {
      var db = Lib.CreateContext();
      var el = new El(email, null);
      var usr = db.Users.FirstOrDefault(u => u.EMail == el.email && u.Login == null);
      if (usr == null) db.Users.Add(usr = new Users() { EMail = email, Created = DateTime.UtcNow });
      usr.VerifyStatus = (short)VerifyStates.ok;
      usr.OtherType = (short)otherType; usr.OtherId = otherId; usr.FirstName = firstName; usr.LastName = lastName;
      Lib.SaveChanges(db);
      return user2cookie(usr);
    }

    public static CmdAdjustScormUserResult OnScormLogin(CmdAdjustScormUser par) {
      var db = Lib.CreateContext();
      par.companyHost = par.companyHost.ToLowerInvariant();
      par.login = par.login.ToLowerInvariant();
      var login = par.companyHost + "/" + par.login;

      var usr = db.Users.FirstOrDefault(u => u.Login == login);
      var comp = db.Companies.Where(c => c.ScormHost == par.companyHost).FirstOrDefault();
      CompanyUsers compUser = null;
      Logger.Log("CmdAdjustScormUserResult Start");
      if (usr == null) //adjust user
        db.Users.Add(usr = new Users() { Created = DateTime.UtcNow, EMail = null, Login = login, FirstName = par.firstName, LastName = par.lastName, LoginEMail = null, OtherId = par.login, OtherType = (short)LMComLib.OtherType.scorm });
      else if (comp != null) //comp a user nejsou null => testGlobalAdmin na compuser
        compUser = db.CompanyUsers.FirstOrDefault(cu => cu.CompanyId == comp.Id && cu.UserId == usr.Id);

      if (comp == null) //adjust company
        db.Companies.Add(comp = new Companies() { ScormHost = par.companyHost, Title = par.companyHost, Created = DateTime.UtcNow });

      if (compUser == null) //adjust comp user
        db.CompanyUsers.Add(compUser = new CompanyUsers() { User = usr, Created = DateTime.UtcNow, Company = comp });
      else if (par.isNotAttempted) { //compUser existuje a isNotAttempted => vymaz vysledky kurzu
        Logger.Log("prodId={0}, userId={1}", par.productId, compUser.Id);
        //foreach (var modData in db.CourseDatas.Where(cd => cd.CourseUser.ProductId == par.productId && cd.CourseUser.email == compUser.compId)) db.CourseDatas.DeleteObject(modData);
        var crsUser = compUser.CourseUsers.FirstOrDefault(crsUsr => crsUsr.ProductId == par.productId);
        if (crsUser != null) db.CourseUsers.Remove(crsUser);
      }

      Lib.SaveChanges(db);

      CmdAdjustScormUserResult res = new CmdAdjustScormUserResult() {
        Cookie = user2cookie(usr),
        companyId = comp.Id,
      };
      return res;
    }

    public static Int64 CreateLmUserStart(LMCookieJS cook, string psw) {
      var db = Lib.CreateContext();
      var el = new El(cook.EMail, cook.Login);
      psw = LowUtils.unpackStr(psw);
      var usr = db.Users.FirstOrDefault(u => u.EMail == el.email && u.Login == el.login);
      if (usr != null)
        switch ((VerifyStates)usr.VerifyStatus) {
          case VerifyStates.ok:
            if (usr.OtherType == (short)OtherType.LANGMaster || usr.OtherType == (short)OtherType.LANGMasterNoEMail)
              return -1; //user already registered
            else { //zmena z FB, Google apod. login na LM login
              usr.Password = psw;
              usr.OtherType = (short)OtherType.LANGMaster;
              usr.OtherId = null;
              usr.FirstName = cook.FirstName;
              usr.LastName = cook.LastName;
              Lib.SaveChanges(db);
              return usr.Id;
            }
          case VerifyStates.waiting:
            return usr.Id;
          case VerifyStates.prepared:
            usr.Password = psw;
            usr.VerifyStatus = (short)VerifyStates.waiting;
            Lib.SaveChanges(db);
            return usr.Id;
        }
      if (usr == null)
        db.Users.Add(usr = new Users() {
          Created = DateTime.UtcNow,
          VerifyStatus = cook.Type == OtherType.LANGMasterNoEMail ? (short)VerifyStates.ok : (short)VerifyStates.waiting,
          Password = psw,
        });
      if (el.email == masterEMail) {
        usr.Roles = (Int64)Role.Admin;
        usr.VerifyStatus = (short)VerifyStates.ok;
      }
      cookie2user(cook, usr);
      Lib.SaveChanges(db);
      return usr.Id;
    } const string masterEMail = "pzika@langmaster.cz";

    public static void CreateLmUserEnd(long id) {
      var db = Lib.CreateContext();
      var usr = db.Users.FirstOrDefault(u => u.Id == id);
      if (usr == null || (usr.VerifyStatus == (short)VerifyStates.ok && usr.OtherType == (short)OtherType.LANGMaster)) return;
      if (!isLMComUser(usr)) {
        usr.OtherType = (short)OtherType.LANGMaster;
        usr.OtherId = null;
      }
      usr.VerifyStatus = (short)VerifyStates.ok;
      Lib.SaveChanges(db);
    }

    public static Users PrepareUser(string email, Container db, bool addCompsRole = false) {
      var res = new Users() { Created = DateTime.UtcNow, EMail = email, VerifyStatus = (short)VerifyStates.prepared, Roles = addCompsRole ? (long)Role.Comps : 0 };
      db.Users.Add(res);
      return res;
    }

    public static void SaveProfile(LMCookieJS cook) {
      var db = Lib.CreateContext();
      var usr = db.Users.First(u => u.Id == cook.id);
      cookie2user(cook, usr);
      Lib.SaveChanges(db);
    }

    public static bool ChangePassword(long id, string oldPsw, string newPsw) {
      var db = Lib.CreateContext();
      var usr = db.Users.First(u => u.Id == id);
      if (usr.Password != oldPsw) return false;
      usr.Password = newPsw;
      Lib.SaveChanges(db);
      return true;
    }

    public static string GetPassword(string email) {
      email = email.ToLower();
      var db = Lib.CreateContext();
      var usr = db.Users.FirstOrDefault(u => u.EMail == email);
      if (usr == null || !isLMComUser(usr) || usr.OtherType != (short)OtherType.LANGMaster) return null;
      return usr.Password;
    }

    /*** private ***/
    static bool isLMComUser(Users usr) {
      var tp = (OtherType)usr.OtherType;
      return tp == OtherType.LANGMaster || tp == OtherType.LANGMasterNoEMail;
    }

    static LMCookieJS user2cookie(Users usr) {
      return new LMCookieJS() {
        id = usr.Id,
        EMail = usr.EMail,
        FirstName = usr.FirstName,
        LastName = usr.LastName,
        Login = usr.Login,
        LoginEMail = usr.LoginEMail,
        Type = (OtherType)usr.OtherType,
        TypeId = usr.OtherId,
        OtherData = usr.OtherData,
      };
    }
    static void cookie2user(LMCookieJS cook, Users usr) {
      var el = new El(cook.EMail, cook.Login);
      usr.EMail = el.email;
      usr.FirstName = cook.FirstName;
      usr.LastName = cook.LastName;
      usr.Login = el.login;
      usr.LoginEMail = cook.LoginEMail;
      usr.OtherType = (short)cook.Type;
      usr.OtherId = cook.TypeId;
      usr.OtherData = cook.OtherData;
    }



  }
}
