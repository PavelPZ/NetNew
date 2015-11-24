using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LMComLib {


  public static class LicenceContst {
    //num konstanty jsou soucasti SQL stored procedury!!!
    public enum Status {
      no, //unknown
      ok = 1, //vse OK
      needsLogin, //nutno se zalogovat
      buyLicence, //nutne koupit licenci
      otherLogged = 4, //nekdo jiny se zalogoval na uzivatele
      blocked, //LM uzivatele v DB zablokoval
      dailySoundExceded, //vycerpan maxDailySound slovicek na bezny den
      dailyInstallationsExceded, //vycerpan maxInstallatiPerDay pocet instalaci, ktere se behem jednoho dne spusti
      alreadyUsed, //licence jiz pouzita
      waitForPayPal,
    }

    public const int maxInstallatiPerDay = 3; //maximalni pocet dennich SL instalaci
    public const int maxDailySound = 300; //maximalni denni davka slovicek na prehrani
  }

  //Informace o licenci kvůli určení ceny pro nákup
  public class LicProductInfo {
    public CourseIds CourseId;
    public Currency Price;
    public DateTime Created;
  }

  //Informace o IPN z PayPalu
  public class IpnInfo {
    public DateTime Created;
    public Currency Price;
    public string EMail;
    public string Status;
    public CourseIds CourseId;
  }

  public enum EMailType {
    PayPalServer,
    PayPalClient,
    Feedback,
    Error,
    Licence,
  }

  public enum LMMembershipCreateStatus {
    // Summary:
    //     The user was successfully created.
    Success = 0,
    //
    // Summary:
    //     The user name was not found in the database.
    InvalidUserName = 1,
    //
    // Summary:
    //     The password is not formatted correctly.
    InvalidPassword = 2,
    //
    // Summary:
    //     The password question is not formatted correctly.
    InvalidQuestion = 3,
    //
    // Summary:
    //     The password answer is not formatted correctly.
    InvalidAnswer = 4,
    //
    // Summary:
    //     The e-mail address is not formatted correctly.
    InvalidEmail = 5,
    //
    // Summary:
    //     The user name already exists in the database for the application.
    DuplicateUserName = 6,
    //
    // Summary:
    //     The e-mail address already exists in the database for the application.
    DuplicateEmail = 7,
    //
    // Summary:
    //     The user was not created, for a reason defined by the provider.
    UserRejected = 8,
    //
    // Summary:
    //     The provider user key is of an invalid extType or format.
    InvalidProviderUserKey = 9,
    //
    // Summary:
    //     The provider user key already exists in the database for the application.
    DuplicateProviderUserKey = 10,
    //
    // Summary:
    //     The provider returned an error that is not described by other System.Web.Security.MembershipCreateStatus
    //     enumeration values.
    ProviderError = 11,
    CannotSendEMail = 12,
    AddToRole = 13,
    Unknown = 14,
  }

  public enum Membership_Role {
    Admin,
    Company,
    Lector,
    LangSchool,
    Demo,
  }

}

