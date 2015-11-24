using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LMNetLib;
using Newtonsoft.Json;
#if JavaScript
using SharpKit.JavaScript;
using SharpKit.jQuery;
#endif

namespace LMComLib {
  public enum CookieIds {
    lang,
    LMTicket,
    schools_info,
    lms_licence,
    subsite,
    returnUrl,
    oauth,
    loginEMail,
    loginLogin,
    LMJSTicket,
  }


  //public class CookieCompany {
  //  public string Comp;
  //  public int CompId;
  //  public string Department;
  //}

  public class MyCompanyLow {
    public string Title;
    public int Id;
    //public CompRole Roles;
    public CompUserRole RoleEx;
    public MyCourse[] Courses;
    public int? DepSelected; //selected department
  }

  public class CompUserRole {
    public CompRole Role;
    public HumanEvalInfo[] HumanEvalatorInfos; //jazyky HumanEvalator role
    public override string ToString() { this.Role = this.Role & CompRole.All; return XmlUtils.ObjectToString(this); } //TODO ROLE
    public static CompUserRole FromString(string value) { var res = string.IsNullOrEmpty(value) ? new CompUserRole() : XmlUtils.StringToObject<CompUserRole>(value); if (res.HumanEvalatorInfos == null) res.HumanEvalatorInfos = new HumanEvalInfo[0]; return res; }
    public static CompUserRole create(string value, CompRole role) {
      if (value == null && role != 0) return new CompUserRole { Role = role }; else return FromString(value);
    }
    public bool isEmpty() {
      return Role == 0 && (HumanEvalatorInfos == null || HumanEvalatorInfos.Length == 0);
    }
  }
  public class HumanEvalInfo {
    public LineIds lang;
    //public string[] Levels; // A1, ...
  }
  //public class HumanEvalLang {
  //  public Langs lang;
  //}

  [Flags]
  public enum CompRole: int {
    Keys = 0x1, //generace klicu pro firmu
    Products = 0x2, //sprava produktu
    Department = 0x4, //editace Company Department
    Results = 0x8, //prohlizeni vysledku studentu
    Publisher = 0x10, //pravo spravovat publisher projekty
    HumanEvalManager = 0x20, //manager pro evaluaci
    HumanEvalator = 0x40, //pravo hodnotit speaking a writing v testech apod.
    Admin = 0x8000, //pridelovani CompRoles
    All = Keys | Products | Department | Results | Publisher | HumanEvalManager | HumanEvalator | Admin, //vsechny role
  }

  public class MyCourse {
    public string ProductId; //@PRODID
    public Int64 Expired; //-1 (pouze pro isTest): jiz neni licence k testu, je tedy mozne prohlizet jen archiv
    public int LicCount; //pocet licenci pro eTestMe produkt
    public string[] LicenceKeys; //licencni klice: seznam "<UserLicences.LicenceId>|<UserLicences.Counter>"
  }

  public enum VerifyStates {
    ok = 0,
    waiting = 1, //uzivatel ceka na potvrzeni registrace
    prepared = 2, //uzivatel je pripraven nekym jinym, chybi mu ale zadani hesla
  }

  [Flags]
  public enum Role {
    Admin = 0x1, //umoznuje pridavat System adminy. Pouze PZ
    Comps = 0x2, //umoznuje pridavat firmy a jejich hlavni spravce (s roli CompRole.Admin)
    All = 0xff, //vsechny role
  }

  public class LMCookieJS {
    // Identifikace uzivatele v databazi profilu
    public Int64 id;

    //LMNEW
    //[JsonIgnore] 
    public int created;

    // Login uzivatele = EMail
    public string EMail;
    /// Login uzivatele (bez emailu)
    public string Login; //obsolete. Login je ulozen v EMail ve formatu "@login"
    public string LoginEMail;

    // Externi identifikace uzivatele
    public OtherType Type;
    public string TypeId;

    public string FirstName;
    public string LastName;

    public string OtherData; //ostatni data v json formatu

    //LMNEW
    //[JsonIgnore]
    public Role Roles;
    //LMNEW
    //[JsonIgnore]
    public VerifyStates VerifyStatus;
    //LMNEW - prijde vyhodit
    public MyCompanyLow Company;
  }

#if JavaScript
  [JsType(JsMode.Clr, Filename = "~/res/common/LMComLib.js", NativeOverloads = false, NativeCasts = true, NativeJsons = true)]
#endif
  public partial class LMCookie : LMCookieJS {

    /// <summary>
    /// Facebook identifikace uzivatele
    /// </summary>
    public long facebookId;

    /// <summary>
    /// Identifikace aktualniho kurzu, dosazuje se pri vstupu do FB aplikace
    /// </summary>
    public string courseId;

    /// <summary>
    /// DB TreeId polozky v CourseUser databazi. Dosazuje se na zacatku requestu v Q:\LMCom\lmcomlib\Servers\Persistence.cs, setLastRequest
    /// </summary>
    public Int64 CourseUserId;

    /// <summary>
    /// Celkovy pocet spusteni kurzu. Dosazuje se na zacatku requestu v Q:\LMCom\lmcomlib\Servers\Persistence.cs, setLastRequest
    /// </summary>
    public int SessionNum;

    /// <summary>
    /// Seznam identifikace uzivatele
    /// </summary>
    //public int seznamId;

    /// <summary>
    /// MOODLE User.Name
    /// </summary>
    public string moodleUserName;

    /// <summary>
    /// Stat dle IP adresy
    /// </summary>
    public string Country;

    public bool IsPersistent;

    public long CreateTick;

    /// <summary>
    /// Uzivatelovy role
    /// </summary>
    public Int64 roles;

    public static LMCookie FromBytes(byte[] data) {
      data = LowUtils.decrypt(data);
      return LowUtils.JSONDecode<LMCookie>(Encoding.UTF8.GetString(data, 0, data.Length));
      //return LowUtils.JSONDecode<LMCookie>(LZWCompressor.DecompressBin(data));// MsgPack.Serializer.BytesToObject<LMCookie>(data);
    }

    public byte[] ToBytes() {
      return LowUtils.encrypt(Encoding.UTF8.GetBytes(LowUtils.JSONEncode(this)));
      //return LZWCompressor.CompressBin(LowUtils.JSONEncode(this)).ToArray();// MsgPack.Serializer.ObjectToBytes(this);
    }

    public override string ToString() {
      //return LowUtils.JSONEncode(this);
      return LowUtils.JsObjectEncode(this);
      //return LowUtils.Base64Decode(ToBytes());
    }

    public static LMCookie FromString(string str) {
      //return LowUtils.JSONDecode<LMCookie>(str);
      return LowUtils.JsObjectDecode<LMCookie>(str);
    }

    public static Int64 otherTypeId(OtherType type, int otherId) {
      return ((Int64)type << 56) + otherId;
    }

    public static void parseOtherTypeId(Int64 id, out OtherType type, out int otherId) {
      otherId = (int)id;
      type = (OtherType)(int)(id >> 56);
    }

    public static string createOtherTypeTicket(OtherType type, int otherId, string email, string firstName, string lastName) {
      LMCookie cook = new LMCookie() { id = otherTypeId(type, otherId), EMail = email, FirstName = firstName, LastName = lastName, CreateTick = DateTime.UtcNow.Ticks };
      return string.Format("ticket={0}", cook.ToString());
    }

  }

}

