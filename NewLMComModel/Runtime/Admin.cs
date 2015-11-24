using LMComLib;
using LMNetLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Microsoft.Data.Entity;
using System.Linq;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Admin {

  //User s Role.Comps roli
  public class UserItem {
    public Int64 LMComId;
    public bool Deleted; //odstran Comps roli
    public string EMail;
  }
  //firma a spravce
  public class Comp {
    public int Id;
    public bool Deleted; //odstran spravcovskou roli
    public string Title;
    //public string PublisherId;
    //spravce firmy:
    public int UserId; //CompanyUser.compId
    public string EMail; //User.compId
  }
  //role pro firmu, spravovane CompRole.Admin-nem
  public class CompUserItem {
    public int UserId;
    public bool Deleted; //odstran usera
    public string EMail;
    public int CompanyId;
    //public CompRole Role;
    public CompUserRole Role;
    [XmlIgnore, JsonIgnore]
    public string RolePar { get { return RoleEx.ToString(); } set { RoleEx = CompUserRole.FromString(value); } }
    [XmlIgnore, JsonIgnore]
    public CompUserRole RoleEx { get { return Role ?? (Role = new CompUserRole()); } set { Role = value; } }
  }

  //public class CompUserRole {
  //  public CompRole Role;
  //  public Langs[] HumanEvalatorLangs; //jazyky HumanEvalator role
  //  public override string ToString() { return XmlUtils.ObjectToString(this); } //TODO ROLE
  //  public static CompUserRole FromString(string value) { return string.IsNullOrEmpty(value) ? new CompUserRole() : XmlUtils.StringToObject<CompUserRole>(value); } 
  //}

  /************ PUBLISHER *************************/
  public class CmdCreatePublProjectItem {
    public string skill;
    public string skillTitle;
    public string template;
    public string instr;
    public int pages; //kolik stranek se vygeneruje
  }
  public class CmdCreatePublProject {
    public LineIds Line;
    public string PublisherId;
    public string ProjectId;
    public string User;
    public string Password;
    public string Title;
    public CmdCreatePublProjectItem[] TestItems;
  }

  public class CmdPublChangePassword {
    public string PublisherId;
    public string ProjectId;
    public string User;
    public string Title;
    public string Password;
  }

  public class CmdGetPublProjects {
    public string PublisherId;
  }
  public class CmdGetPublProjectsResultItem {
    public LineIds Line;
    public string ProjectId;
    public string User;
    public string Password;
    public string Title;
  }
  public class CmdGetPublProjectsResult {
    public CmdGetPublProjectsResultItem[] projects;
  }

  public class CmdPublBuild {
    public string PublisherId;
    public string ProjectId;
  }


  /************ USERS *************************/
  public class CmdGetUsers {
    //Pro: Role.Admin, tj. pro PZ. 
    //Vrati: vsechny Users s non Role.Admin roli
    //Co s nimi: mohou zakladat ci mazat firmy (a jejich spravce)
    public bool IncUsers;

    //Pro: Role.Comps (ktere prideluje PZ). 
    //Vrati: vsechny firmy a jejich spravce
    //Co s nimi: mohou pridavat a editovat non CompRole.Admin role
    public bool IncComps;

    //Pro: CompRole.Admin. 
    //Vrati: vsechny CompanyUser s CompanyId v CompIds a non CompRole.Admin roli
    //Co s nimi: mohou provadet operace, prislusne roli (generovat klice, upravovat produkty apod.)
    public int[] CompIds;
  }
  public class CmdGetUsersResult {
    public UserItem[] Users;
    public Comp[] Comps;
    public CompUserItem[] CompUsers;
  }

  public class CmdSetUsers { //Plati: LMComId nebo email == 0 => Insert do stavu Prepared. compId==null => delete, else => Update
    public UserItem[] Users;
    public Comp[] OldComps;
    public Comp[] Comps;
    public CompUserItem[] CompUsers;
  }
  /************ COMPANY NEW *************************/
  //public class CompanyMeta {
  //  //public string compId; //jednoznacna identifikace company
  //  public string Title;
  //  public string ScormHost; //identifikace company pro scorm (= domena jejich scormu)
  //  public int Created;
  //}
  //public class JSCompany { //objekt pro komunikaci mezi JS a serverem
  //  public string compId;
  //  public string ETag;
  //  public CompanyMeta metaObj;
  //  public DepartmentRoot departmentsObj; //struktura oddeleni
  //  public CompanyLicences licenceObj; //licence ke kurzum
  //  public DepartmentUsages departmentUsageObj; //zatrideni users do department
  //  public CompanyUsers usersObj; //users s company roli
  //}


  /************ DEPARTMENTS *************************/
  public class CmdGetDepartment {
    public int CompanyId;
  }
  public class CmdGetDepartmentResult {
    public Department Departments;
    public int[] UsedIds;
    public IntervalsConfig IntervalsConfig;
  }
  public class CmdSetDepartment {
    public int CompanyId;
    public Department Departments;
    public IntervalsConfig IntervalsConfig;
  }
  //public class DepartmentRoot : Department {
  //  public ushort IdAutoIncrement; //citac, zajistujici jednoznacnost Department.compId
  //}
  public class Department {
    public int Id;
    public string Title;
    public Department[] Items;
    public bool isNew;
    /*** design time ***/
    [JsonIgnore]
    public NewData.CompanyDepartments db;
  }

  /************ DEPARTMENT USAGE *************************/
  //public class DepartmentUsages {
  //  public List<DepartmentUsage> Usages;
  //  public void ToBinary(BinaryWriter wr) {
  //    wr.Write(binaryVersion.v);
  //    wr.Write(Usages == null ? 0 : Usages.Count);
  //    if (Usages != null) foreach (var l in Usages) l.ToBinary(wr);
  //  }
  //  public static DepartmentUsages FromBinary(BinaryReader wr) {
  //    wr.ReadByte();
  //    var res = new DepartmentUsages();
  //    var cnt = wr.ReadInt32(); if (cnt == 0) { res.Usages = new List<DepartmentUsage>(); return res; }
  //    res.Usages = Enumerable.Range(0, cnt).Select(c => DepartmentUsage.FromBinary(wr)).ToList();
  //    return res;
  //  }
  //}
  //public class DepartmentUsage {
  //  public ushort depId;
  //  public int[] userIds;
  //  public void ToBinary(BinaryWriter wr) {
  //    wr.Write(binaryVersion.v);
  //    wr.Write(depId); wr.Write(userIds == null ? 0 : userIds.Length);
  //    if (userIds != null) foreach (var l in userIds) wr.Write(l);
  //  }
  //  public static DepartmentUsage FromBinary(BinaryReader wr) {
  //    wr.ReadByte();
  //    var res = new DepartmentUsage { depId = wr.ReadUInt16() };
  //    var cnt = wr.ReadInt32(); if (cnt == 0) return res;
  //    res.userIds = Enumerable.Range(0, cnt).Select(c => wr.ReadInt32()).ToArray();
  //    return res;
  //  }
  //}

  //****************** informace v Company o users 
  public class CompanyUsers { 
    public List<CompanyUser> Users;
  }
  public class CompanyUser {
    public string EMail;
    //public string FirstName;
    //public string LastName;
    public CompUserRole Roles;
    public bool isEmpty() { return Roles == null || Roles.isEmpty(); }
  }

  //****************** informace v Users o Company
  public class UserCompanies { //informace v User o companies nebo informace v Company o users 
    public List<UserCompany> Companies;
  }
  public class UserCompany {
    public string compId;
    public int DepartmentId;
    public LMComLib.CompUserRole Roles;
    public List<UserProduct> Products;
    public bool isEmpty() { return DepartmentId == 0 && (Roles == null || Roles.isEmpty()) && (Products == null || Products.Count == 0); }
  }

  public class UserProduct {
    public string ProductId;
    //pro test:
    public int LicKeyId; //jednoznacna identifikace testu pro uzivatele. Prevzato z licencniho klice - staci jen KeyId, protoze: companyId je v ceste k datum jiz zahrnuta a LicenceId je pro test vzdy jen jedna.
    //pro kurz:
    public int Created; //datum, odkdy se pocita expirace
    public ushort CourseDays; //: pocet dnu od Created, za ktery kurz expiruje
  }

  //public class UserKey { //klic, ulozeny u uzivatele
  //  //licence info
  //  //public string ProductId;
  //  public ushort Days;
  //  //identifikace licence
  //  //public ushort CompLicKeyId; //=Company.licenceObj.compLicKeyId (CompanyLicences.compLicKeyId), jednoznacne ID company pro generaci licencnich klicu
  //  //pouze evidence, asi neni potreba.
  //  public ushort LicId; //=ProductLicence.LicId
  //  public int KeyId; //pouzity UsedKey.KeyId
  //}

  public static class UserCompanyRelation {
    public static void refresh(CompanyUsers users, UserCompanies comps, string email, string compId, Action<UserCompany, CompanyUser> action) {
      //zajisti existenci propojovacich prvku
      var comp = comps.Companies.FirstOrDefault(u => u.compId == compId);
      if (comp == null) comps.Companies.Add(comp = new UserCompany { compId = compId, Roles = new CompUserRole(), /*Keys = new List<UserKey>(),*/ Products = new List<UserProduct>() });
      var user = users.Users.FirstOrDefault(u => u.EMail == email);
      if (user == null) users.Users.Add(user = new CompanyUser { EMail = email, Roles = new CompUserRole() });
      //modifikuj propojovaci prvky
      action(comp, user);
      //aktualizuj seznamy propojovacich prvku
      if (user.isEmpty()) users.Users = users.Users.Where(u => u.EMail != email).ToList();
      if (comp.isEmpty()) comps.Companies = comps.Companies.Where(c => c.compId != compId).ToList();
    }
  }

  /************ PRODUCTS *************************/
  public class CmdGetProducts {
    public int CompanyId;
    public bool incUsedKeys;
  }
  public class Product {
    public int Id;
    public int LastCounter;
    public short Days;
    public string ProductId;
    public bool Deleted;
    public int UsedKeys;
  }
  public class CmdSetProducts {
    public int CompanyId;
    public Product[] Products;
  }

  /************ KEYS *************************/
  public class CmdAlocKeys { //vrati prvni pouzitelny klic
    public int LicenceId;
    public int Num;
  }
  /************ DESIGN TIME *************************/
  public class CmdDsgnReadFile {
    public string FileName;
  }
  public class CmdDsgnReadFiles {
    public string[] FileNames;
  }
  public class CmdDsgnWriteDictWords {
    public string FileName;
    public string Data;
  }
  //public class CmdDsgnFormatJson {
  //  public string Data;
  //}
  public class CmdDsgnResult {
    public string[] Data;
  }

  public partial class Interval {
    public int From;
    public string Title;
    public static string localizePeriod(int from, int to, CultureInfo cult, bool isRuntime = true) {
      try {
        int m, y;
        FromMonthCode(from, out m, out y);
        var actStr = new DateTime(y, m, 1).ToString("Y", cult);
        var actCode = from.ToString(); string nextCode = "";
        Func<string> getCode = () => isRuntime ? null : " (" + actCode + "," + nextCode + ")";
        if (to < 0) return actStr + " -" + getCode();
        int nm, ny;
        FromMonthCode(to, out nm, out ny);
        nextCode = to.ToString();
        nm -= 1;
        if (nm == 0) { ny -= 1; nm = 12; }
        if (m == nm && y == ny) return actStr + getCode();
        var nextStr = new DateTime(ny, nm, 1).ToString("Y", cult);
        if (y == ny && nm == 12 && m == 1) return y.ToString() + getCode();
        if (y == ny) return cult.DateTimeFormat.GetMonthName(m) + " - " + nextStr + getCode();
        return actStr + " - " + nextStr + getCode();
      } catch {
        return string.Format("*** Error in DBExtensionCommon.cs.localizePeriod: from={0}, to={1}", from, to);
      }
    }
    public static string localizePeriod(string title, CultureInfo cult) {
      Regex parsePeriod = new Regex(@".*\(((?<from>[123]\d[01]\d),(?<to>[123]\d[01]\d)|(?<from>[123]\d[01]\d),)|,(?<to>[123]\d[01]\d)\).*", RegexOptions.Singleline);
      var match = parsePeriod.Match(title);
      if (!match.Success) return title;
      var from = match.Groups["from"].Value;
      var to = match.Groups["to"].Value;
      return localizePeriod(int.Parse(from), to == "" ? -1 : int.Parse(to), cult);
    }

    static public int MonthCode(int Month, int Year) { return (Year - 2000) * 100 + Month; }
    static public void FromMonthCode(int code, out int month, out int year) { month = code % 100; year = (code - month) / 100 + 2000; }

    public XElement toXml() {
      return new XElement("Item", new XElement("Title", Title), new XElement("From", From.ToString()));
    }
    public static Interval fromXml(XElement xml) {
      return new Interval() {
        Title = ElementValue(xml, "Title", null),
        From = int.Parse(ElementValue(xml, "From", "0")),
      };
    }
    static string ElementValue(XElement els, XName name, string defaultValue) {
      XElement el = els.Element(name);
      return el == null ? defaultValue : el.Value;
    }

  }
  public partial class Intervals {
    public Interval[] Items;

    static public int MonthCode(int Month, int Year) { return (Year - 2000) * 100 + Month; }

    public static Intervals TimeDefault() {
      var res = new Intervals {
        Items = Enumerable.Range(0, 12).Select(y => new Interval() {
          From = MonthCode(1, 2013 + y),
        }).ToArray()
      };
      return res;
    }
    public static Intervals ScoreDefault() {
      var res = new Intervals {
        Items = new Interval[] {
          new Interval() {From=99},//, Title="100%"},
          new Interval() {From=97},//, Title="98-99%"},
          new Interval() {From=94},//, Title="95-97%"},
          new Interval() {From=89},//, Title="90-94%"},
          new Interval() {From=82},//, Title="83-89%"},
          new Interval() {From=70},//, Title="71-82%"},
          new Interval() {From=56},//, Title="57-70%"},
          new Interval() {From=35},//, Title="36-56%"},
          new Interval() {From=-1},//, Title="0-35%"},
        }
      };
      return res;
    }

    public static Intervals SecDefault() {
      string[] secYearDefault = new string[] { "4.00:00:00", "2.20:00:00", "2.00:00:00", "1.10:00:00", "1.00:00:00", "17:00:0", "12:00:00", "08:30:00", "06:00:00", "04:00:00", "03:00:00", "02:00:00", "01:30:00", "01:00:00", "00:45:00", "00:30:00", "00:20:00", "00:15:00", "00:10:00", "00:00:00" };
      var res = new Intervals() {
        Items = secYearDefault.Select((s, idx) => new Interval() { From = (int)TimeSpan.Parse(s).TotalSeconds /*, Title = s + " - " + (idx == 0 ? "" : secYearDefault[idx - 1])*/ }).ToArray()
      };
      //for (int i = 0; i < res2.Items.Length; i++) res2.Items[i].IntervalId = i;
      return res;
    }
    public XElement toXml(XElement root) {
      if (Items != null && Items.Length > 0) root.Add(new XElement("Items", Items.Select(it => it.toXml())));
      return root;
    }
    public static Intervals fromXml(XElement xml) {
      var items = xml == null ? null : xml.Element("Items");
      return items == null ? null : new Intervals() {
        Items = !xml.HasElements ? null : items.Elements().Select(el => Interval.fromXml(el)).ToArray()
      };
    }
  }
  public class IntervalsConfig {
    public Intervals Secs;
    public Intervals Scores;
    public Intervals Periods;
    public XElement toXml() {
      checkEmpty(true);
      return new XElement("root",
        Secs == null ? null : Secs.toXml(new XElement("Secs")),
        Scores == null ? null : Scores.toXml(new XElement("Scores")),
        Periods == null ? null : Periods.toXml(new XElement("Periods")));
    }
    public string toString() { return toXml().ToString(); }
    public static IntervalsConfig fromXml(XElement xml) {
      return new IntervalsConfig() {
        Secs = Intervals.fromXml(xml.Element("Secs")),
        Scores = Intervals.fromXml(xml.Element("Scores")),
        Periods = Intervals.fromXml(xml.Element("Periods")),
      };
    }
    public static IntervalsConfig fromString(string str) {
      return fromXml(XElement.Parse(str)).checkEmpty(false);
    }
    IntervalsConfig checkEmpty(bool isSave) {
      if (Scores == null || Scores.Items == null || Scores.Items.Length < 2) Scores = isSave ? new Intervals() : Admin.Intervals.ScoreDefault();
      if (Secs == null || Secs.Items == null || Secs.Items.Length < 2) Secs = isSave ? new Intervals() : Admin.Intervals.SecDefault();
      if (Periods == null || Periods.Items == null || Periods.Items.Length < 2) Periods = isSave ? new Intervals() : Admin.Intervals.TimeDefault();
      return this;
    }
  }


}
namespace NewData {

  using Admin;
  using System.Globalization;
  using System.IO;
  using System.Text.RegularExpressions;
  using System.Xml.Linq;

  public static class AdminServ {

    static AdminServ() {

      /* GetUsers */
      Handlers.CmdService.registerCommand<CmdGetUsers, CmdGetUsersResult>(par => {
        return new RpcResponse(GetUsers(par));
      });

      /* SetUsers */
      Handlers.CmdService.registerCommand<CmdSetUsers, bool>(par => {
        SetUsers(par);
        return new RpcResponse();
      });

      /* CmdGetProducts */
      Handlers.CmdService.registerCommand<CmdGetProducts, Product[]>(par => {
        return new RpcResponse(GetProducts(par));
      });

      /* CmdSetProducts */
      Handlers.CmdService.registerCommand<CmdSetProducts, bool>(par => {
        SetProducts(par);
        return new RpcResponse();
      });

      /* CmdGetDepartment */
      Handlers.CmdService.registerCommand<CmdGetDepartment, CmdGetDepartmentResult>(par => {
        return new RpcResponse(GetDepartment(par.CompanyId));
      });

      /* CmdSetDepartment */
      Handlers.CmdService.registerCommand<CmdSetDepartment, bool>(par => {
        SetDepartment(par);
        return new RpcResponse();
      });

      /* CmdAlocKeys */
      Handlers.CmdService.registerCommand<CmdAlocKeys, int>(par => {
        return new RpcResponse(AlocKeys(par));
      });

      ///* CmdDsgnReadFile */
      //Handlers.CmdService.registerCommand<CmdDsgnReadFile, string>(par => {
      //  try {
      //    return new RpcResponse(File.ReadAllText(Machines.basicPath + par.FileName));
      //  } catch {
      //    return new RpcResponse(1, Machines.basicPath + par.FileName);
      //  }
      //});

      ///* CmdDsgnReadFiles */
      //Handlers.CmdService.registerCommand<CmdDsgnReadFiles, CmdDsgnResult>(par => {
      //  List<string> res = new List<string>();
      //  foreach (var fn in par.FileNames)
      //    try {
      //      res.Add(File.ReadAllText(Machines.basicPath + fn));
      //    } catch {
      //      res.Add(null);
      //    }
      //  return new RpcResponse(new CmdDsgnResult() { Data = res.ToArray() });
      //});

      ///* CmdDsgnWriteFile */
      //Handlers.CmdService.registerCommand<CmdDsgnWriteDictWords, bool>(par => {
      //  try {
      //    var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<schools.DictWords>(par.Data);
      //    var f = obj.courses.First();
      //    XmlUtils.ObjectToFile(Machines.rootPath + @"RwDicts\UsedWords\" + par.FileName + "_" + f.lang.ToString() + ".xml", new schools.DictCrsWords() {
      //      lang = f.lang,
      //      exs = obj.courses.SelectMany(c => c.exs).ToArray()
      //    });
      //    return new RpcResponse(true);
      //  } catch {
      //    return new RpcResponse(1, Machines.basicPath + par.FileName);
      //  }
      //});

      ///* CmdDsgnWriteFile */
      //Handlers.CmdService.registerCommand<DictEntryCmd, DictEntryRes>(par => {
      //  return new RpcResponse(dictLingea.processCommand(par));
      //});

      ///* CmdGetPublProjects */
      //Handlers.CmdService.registerCommand<CmdGetPublProjects, CmdGetPublProjectsResult>(par => {
      //  return new RpcResponse(GetPublProjects(par));
      //});

      ///* CmdCreatePublProject */
      //Handlers.CmdService.registerCommand<CmdPublChangePassword, bool>(par => {
      //  return new RpcResponse(PublChangePassword(par));
      //});

      ///* CmdPublBuild */
      //Handlers.CmdService.registerCommand<CmdPublBuild, bool>(par => {
      //  return new RpcResponse(PublBuild(par));
      //});

      ///* CmdCreatePublProject */
      //Handlers.CmdService.registerCommand<CmdCreatePublProject, bool>(par => {
      //  return new RpcResponse(CreatePublProject(par));
      //});

    }

    private static bool PublBuild(CmdPublBuild par) {
      return false;
    }

    private static bool PublChangePassword(CmdPublChangePassword par) {
      var projFn = Machines.rootPath + @"Publishers\" + par.PublisherId + @"\" + par.ProjectId + @"\meta.xml";
      var proj = XmlUtils.FileToObject<CourseMeta.project>(projFn);
      proj.FtpPassword = par.Password; proj.FtpUser = par.User; proj.title = par.Title;
      XmlUtils.ObjectToFile(projFn, proj);
      return true;
    }

    private static bool CreatePublProject(CmdCreatePublProject par) {
      var publDir = Machines.rootPath + @"Publishers\" + par.PublisherId;
      if (!Directory.Exists(publDir)) {
        CourseMeta.publisher publ = new CourseMeta.publisher { title = par.PublisherId };
        LowUtils.AdjustDir(publDir);
        XmlUtils.ObjectToFile(publDir + @"\meta.xml", publ);
      }
      var dir = publDir + @"\" + par.ProjectId;
      if (Directory.Exists(dir)) throw new Exception("Directory already exists: " + dir);
      LowUtils.AdjustDir(dir);
      CourseMeta.project prj = new CourseMeta.project { title = par.Title, line = par.Line, FtpPassword = par.Password, FtpUser = par.User };
      XmlUtils.ObjectToFile(dir + @"\meta.xml", prj);
      return true;
    }

    private static CmdGetPublProjectsResult GetPublProjects(CmdGetPublProjects par) {
      var dir = Machines.rootPath + @"Publishers\" + par.PublisherId;
      return new CmdGetPublProjectsResult {
        projects = !Directory.Exists(dir) ? null : Directory.
          GetDirectories(dir).
          Select(d => new { id = d.Substring(dir.Length + 1), obj = XmlUtils.FileToObject<CourseMeta.project>(d + @"\meta.xml") }).
          Select(prj => new CmdGetPublProjectsResultItem { Line = prj.obj.line, Title = prj.obj.title, Password = prj.obj.FtpPassword, User = prj.obj.FtpUser, ProjectId = prj.id }).
          ToArray()
      };
    }

    static void SetDepartment(CmdSetDepartment par) {
      var db = Lib.CreateContext();
      //interval configs
      var comp = db.Companies.First(c => c.Id == par.CompanyId);
      comp.IntervalsConfig = par.IntervalsConfig == null ? null : par.IntervalsConfig.toString();
      //nacti vsechny departments pro aktualni firmu
      var allDeps = db.CompanyDepartments.Where(d => d.CompanyId == par.CompanyId).ToDictionary(d => d.Id, d => d);
      //najdi a vloz nebo aktualizuj
      ForEach(par.Departments, (parent, act) => {
        if (act.isNew) {
          db.CompanyDepartments.Add(act.db = new CompanyDepartments() {
            Title = act.Title,
            Parent = parent == null ? null : parent.db,
            CompanyId = par.CompanyId,
          });
        } else {
          if (!allDeps.TryGetValue(act.Id, out act.db)) throw new Exception();
          act.db.Parent = parent == null ? null : parent.db;
          act.db.Title = act.Title;
          allDeps.Remove(act.Id);
        }
      });
      //zbyle vymaz
      foreach (var kv in allDeps) db.CompanyDepartments.Remove(kv.Value);
      //save
      Lib.SaveChanges(db);
    }

    public static void ForEach(Department self, Action<Department, Department> act, Department parent = null) {
      act(parent, self);
      if (self.Items != null) foreach (var ch in self.Items) ForEach(ch, act, self);
    }

    public static CmdGetDepartmentResult GetDepartment(int companyId) {
      var db = Lib.CreateContext();
      var allDeps = db.CompanyDepartments.Where(d => d.CompanyId == companyId).ToArray();
      var allUsed = db.CompanyUsers.Where(d => d.CompanyId == companyId && d.DepartmentId != null).Select(u => u.DepartmentId).Distinct().ToArray();
      var xml = db.Companies.First(c => c.Id == companyId).IntervalsConfig;
      IntervalsConfig intCfg = null;
      try {
        if (xml != null) intCfg = IntervalsConfig.fromString(xml);
        if (intCfg != null) {
          if (intCfg.Scores == null || intCfg.Scores.Items == null || intCfg.Scores.Items.Length < 2) intCfg.Scores = Admin.Intervals.ScoreDefault();
          if (intCfg.Secs == null || intCfg.Secs.Items == null || intCfg.Secs.Items.Length < 2) intCfg.Secs = Admin.Intervals.SecDefault();
          if (intCfg.Periods == null || intCfg.Periods.Items == null || intCfg.Periods.Items.Length < 2) intCfg.Periods = Admin.Intervals.TimeDefault();
        }
      } finally {
        if (intCfg == null) intCfg = new Admin.IntervalsConfig() {
          Scores = Admin.Intervals.ScoreDefault(),
          Secs = Admin.Intervals.SecDefault(),
          Periods = Admin.Intervals.TimeDefault()
        };
      }
      return new CmdGetDepartmentResult() {
        Departments = GetDepartment(allDeps),
        UsedIds = allUsed.OfType<int>().ToArray(),
        IntervalsConfig = intCfg,
      };
    }
    //static CmdGetDepartmentResult GetDepartment(CmdGetDepartment par) {
    //  var db = lib.CreateContext();
    //  var allDeps = db.CompanyDepartments.Where(d => d.CompanyId == par.CompanyId).ToArray();
    //  var allUsed = db.CompanyUsers.Where(d => d.CompanyId == par.CompanyId && d.DepartmentId != null).Select(u => u.DepartmentId).Distinct().ToArray();
    //  return new CmdGetDepartmentResult() {
    //    Departments = GetDepartment (allDeps),
    //    UsedIds = allUsed.OfType<int>().ToArray()
    //  };
    //}
    static Department GetDepartment(CompanyDepartments[] allDb, CompanyDepartments actDb = null) {
      if (allDb == null || allDb.Length == 0) return null;
      if (actDb == null) actDb = allDb.First(d => d.ParentId == null);
      return new Department() {
        Id = actDb.Id,
        Title = actDb.Title,
        Items = allDb.Where(d => d.ParentId == actDb.Id).Select(d => GetDepartment(allDb, d)).ToArray()
      };
    }

    static int AlocKeys(CmdAlocKeys par) {
      var db = Lib.CreateContext();
      var lic = db.CompanyLicences.First(l => l.Id == par.LicenceId);
      var res = lic.LastCounter + 1;
      lic.LastCounter += par.Num;
      Lib.SaveChanges(db);
      return res;
    }

    //pripravi jednu licenci, napr. pro Skrivanek testGlobalAdmin (kde je days=0)
    public static CompanyLicences adjustAddHocLicence(int companyId, Int64 userId, short days, string productId) {
      var db = Lib.CreateContext();
      var prod = db.CompanyLicences.Where(l => l.CompanyId == companyId && l.ProductId == productId && l.Days == days).FirstOrDefault();
      if (prod == null) db.CompanyLicences.Add(prod = new CompanyLicences {
        ProductId = productId,
        Days = days,
        LastCounter = 0,
        CompanyId = companyId,
        Created = DateTime.UtcNow,
      });
      prod.LastCounter++;
      Lib.SaveChanges(db);
      return prod;
    }

    static Product[] GetProducts(CmdGetProducts comp) {
      var db = Lib.CreateContext();
      var lics = comp.incUsedKeys ? db.CompanyLicences.Include(cl => cl.UserLicences).Where(l => l.CompanyId == comp.CompanyId).ToArray() : db.CompanyLicences.Where(l => l.CompanyId == comp.CompanyId).ToArray();
      var res = lics.
        Select(l => new Product() {
          Id = l.Id,
          Days = l.Days,
          LastCounter = l.LastCounter,
          ProductId = l.ProductId,
          UsedKeys = comp.incUsedKeys ? l.UserLicences.Count() : 0
        }).
        ToArray();
      //Author.Server.getPublProducts(comp.CompanyId).Select(p => new )
      return res;
    }

    static void SetProducts(CmdSetProducts prods) {
      if (prods.Products == null || prods.Products.Length == 0) return;
      //delete
      var delIds = prods.Products.Where(p => p.Deleted).Select(p => p.Id).ToArray();
      foreach (var id in prods.Products.Where(p => p.Deleted).Select(p => p.Id)) {
        var delDb = Lib.CreateContext();
        delDb.CompanyLicences.Remove(delDb.CompanyLicences.First(p => p.Id == id));
        try {
          Lib.SaveChanges(delDb);
        } catch { }
      }

      var prodList = prods.Products.Where(p => !p.Deleted);
      var db = Lib.CreateContext();

      //update
      var ids = prodList.Where(p => p.Id != 0 && p.LastCounter == 0).Select(p => p.Id).ToArray();
      foreach (var prodDb in db.CompanyLicences.Where(l => ids.Contains(l.Id))) {
        var pr = prods.Products.First(p => p.Id == prodDb.Id);
        prodDb.ProductId = pr.ProductId; prodDb.Days = pr.Days;
      }
      //insert
      foreach (var pr in prodList.Where(p => p.Id == 0)) {
        db.CompanyLicences.Add(new CompanyLicences() {
          ProductId = pr.ProductId,
          Days = pr.Days,
          LastCounter = 0,
          CompanyId = prods.CompanyId,
          Created = DateTime.UtcNow,
        });
      }
      Lib.SaveChanges(db);
    }

    static CmdGetUsersResult GetUsers(CmdGetUsers users) {
      var db = Lib.CreateContext();
      return new CmdGetUsersResult() {
        //vsechny users s non Admin roli (Role.Comps)
        Users = !users.IncUsers ? null : db.Users.
          Where(u => u.Roles != 0).
          Select(u => new { u.Roles, u.EMail, u.Id }).
          ToArray().
          Where(u => ((Role)u.Roles & ~Role.Admin) != 0). //existuje non Admin role
          Select(u => new UserItem() {
            EMail = u.EMail,
            LMComId = u.Id,
          }).ToArray(),
        //Vsechny firmy a k nim CompanyUser s CompRole.Admin roli
        Comps = !users.IncComps ? null : db.CompanyUsers.
          Where(cu => cu.Roles >= (long)CompRole.Admin). //CompRole.Admin je nejvetsi
          Select(cu => new { cu.Id, cu.User.EMail, cu.CompanyId, cu.Company.Title }).
          ToArray().
          Select(c => new Comp() {
            Title = c.Title,
            Id = c.CompanyId,
            UserId = c.Id,
            EMail = c.EMail,
            //PublisherId = c.PublisherId,
          }).ToArray(),
        //Vsechny Company Users s nejakou non Admin roli
        CompUsers = users.CompIds == null ? null : db.CompanyUsers.
          Where(cu => cu.Roles != 0 && cu.Roles != (long)CompRole.HumanEvalator && users.CompIds.Contains(cu.CompanyId)).Select(cu => new { cu.Id, cu.User.EMail, cu.CompanyId, cu.RolePar, cu.Roles }).ToArray().
          //ToArray().
          //Where(cu => ((CompRole)cu.Roles & ~CompRole.Admin) != 0). //non admin role
          Select(cu => new CompUserItem() {
            UserId = cu.Id,
            EMail = cu.EMail,
            //Role = (CompRole)cu.Roles,
            RoleEx = CompUserRole.create(cu.RolePar, (CompRole)cu.Roles),
            //RolePar = cu.RolePar,
            CompanyId = cu.CompanyId,
          }).ToArray(),
      };
    }

    public static Companies createCompany(Container db, string title, Users usr, bool isFakePublisherCompany) {
      var compDb = new Companies() { Title = title, Created = DateTime.UtcNow }; db.Companies.Add(compDb);
      var dep = new CompanyDepartments() { Title = title, Company = compDb };
      db.CompanyDepartments.Add(dep);
      var compUser = new CompanyUsers() { Created = DateTime.UtcNow, Company = compDb, User = usr/*, RolesEx = (long)(isFakePublisherCompany ? CompRole.All : CompRole.Admin)*/, CompanyDepartment = dep };
      Lib.setRolesEx(compUser, (long)(isFakePublisherCompany ? CompRole.All : CompRole.Admin));
      db.CompanyUsers.Add(compUser);
      if (isFakePublisherCompany) usr.MyPublisher = compDb;
      return compDb;
    }

    static void SetUsers(CmdSetUsers dt) {
      var db = Lib.CreateContext();

      /************ Users *****************/
      if (dt.Users != null && dt.Users.Length > 0) {
        //LMComId > 0 => uprav existujiciho usera
        var ids = dt.Users.Where(u => u.LMComId != 0).Select(u => u.LMComId).ToArray();
        foreach (var usr in db.Users.Where(u => ids.Contains(u.Id)))
          if (dt.Users.First(u => u.LMComId == usr.Id).Deleted) usr.Roles &= ~(long)Role.Comps; //deleted => zrus Comps roli
          else usr.Roles |= (long)Role.Comps; //neni Deleted => nastav Comps roli
        //LMComId == 0 => adjust usera
        var emails = dt.Users.Where(u => u.LMComId == 0).Select(u => u.EMail).ToList(); //vsechny nove emaily
        foreach (var usr in db.Users.Where(u => emails.Contains(u.EMail))) { //nacti existujici z DB
          usr.Roles |= (long)Role.Comps; //nastav Comps roli
          emails.Remove(usr.EMail); //vymaz z pozadovanych emailu
        }
        foreach (var em in emails) NewData.Login.PrepareUser(em, db, true); //compId nenalezen v DB => zaloz users v prepared stavu
      }
      Lib.SaveChanges(db);

      List<int> deletedComps = new List<int>(); //evicence vymazanych companies, abych je v "Company User Roles" ignoroval

      /************ Users *****************/
      if (dt.Comps != null && dt.Comps.Length > 0) {
        //new comps
        var newComps = dt.Comps.Where(c => c.Id == 0);
        var emails = newComps.Select(c => c.EMail).Where(c => c != null).ToArray();
        var newUsers = db.Users.Where(u => emails.Contains(u.EMail));
        //zaloz novou company (s primitivnim department) a ev. compUsera a Usera
        foreach (var comp in newComps) {
          var usr = newUsers.FirstOrDefault(u => u.EMail == comp.EMail);
          if (usr == null) usr = NewData.Login.PrepareUser(comp.EMail, db); //zaloz users v prepared stavu
          createCompany(db, comp.Title, usr, false);
          //var compDb = new Company() { Title = comp.Title, Created = DateTime.UtcNow }; db.Users.Add(compDb);
          //db.CompanyDepartments.Add(new CompanyDepartment() { Title = comp.Title, Company = compDb });
          //var userObj = newUsers.FirstOrDefault(u => u.compId == comp.compId);
          //if (userObj == null) userObj = NewData.Login.PrepareUser(comp.compId, db); //zaloz users v prepared stavu
          //var compUser = new CompanyUser() { Created = DateTime.UtcNow, Company = compDb, User = userObj, Roles = (long)CompRole.Admin };
          //db.CompanyUsers.Add(compUser);
        }
        //Uprav stavajici company
        var ids = dt.Comps.Where(c => c.Id != 0).Select(c => c.Id).ToArray(); //compId's existujici companies
        var compsDb = db.Companies.Where(c => ids.Contains(c.Id)).ToArray(); //nacti companies z DB
        foreach (var comp in dt.Comps.Where(c => c.Id != 0)) {
          var compDb = compsDb.First(c => c.Id == comp.Id); //aktualni comp v db
          if (comp.Deleted) { deletedComps.Add(comp.Id); db.Companies.Remove(compDb); continue; } //delete company
          compDb.Title = comp.Title; //aktualizuj //compDb.PublisherId = comp.PublisherId; 
          var old = dt.OldComps.First(c => c.Id == comp.Id); //najdi starou verzi
          if (old.EMail != comp.EMail) {//User je zmeneny
            //db.CompanyUsers.First(u => u.Id == old.UserId).RolesEx &= ~(long)CompRole.Admin; //zrus admina u old email
            var adminUsr = db.CompanyUsers.First(u => u.Id == old.UserId);
            Lib.setRolesEx(adminUsr, Lib.getRolesEx(adminUsr) & ~(long)CompRole.Admin);
            //adjust noveho admina 
            var usr = newUsers.FirstOrDefault(u => u.EMail == comp.EMail); //Zacni Userem
            CompanyUsers compUsr = null;
            if (usr == null) usr = NewData.Login.PrepareUser(comp.EMail, db); //pro neexistujiciho zaloz usera v prepared stavu
            else compUsr = usr.CompanyUsers.FirstOrDefault(cu => cu.CompanyId == comp.Id); //pro existujiciho usera nalezni Cmpany Usera
            if (compUsr == null) {
              compUsr = new CompanyUsers() { Created = DateTime.UtcNow, Company = compDb, User = usr/*, RolesEx = (long)CompRole.Admin*/ }; //Company User neexistuje => zaloz
              Lib.setRolesEx(compUsr, (long)CompRole.Admin);
            }
            //else compUsr.Roles |= (long)CompRole.Admin; //existuje, dej mu Admin roli
            else Lib.setRolesEx(compUsr, Lib.getRolesEx(compUsr) | (long)CompRole.Admin); //compUsr.RolesEx |= (long)CompRole.Admin; //existuje, dej mu Admin roli
          }
        }
      }
      Lib.SaveChanges(db);

      /************ Company User Roles *****************/
      if (dt.CompUsers != null && dt.CompUsers.Length > 0) {
        //kontrola companies, ktere byly mozna vymazany v predchozim kroku
        CompUserItem[] actUsers = dt.CompUsers.Where(u => !deletedComps.Contains(u.CompanyId)).ToArray();

        //Uprav stavajici users
        var ids = actUsers.Where(u => u.UserId > 0).Select(u => u.UserId).ToArray();
        var dbUsers = db.CompanyUsers.Where(u => ids.Contains(u.Id)).ToArray(); //nacti stavajicu users z DB
        foreach (var dbUser in dbUsers) {
          var usr = actUsers.First(u => u.UserId == dbUser.Id); //dato s novou verzi
          var oldRoles = (CompRole)dbUser.Roles; var newRoles = usr.RoleEx.Role;
          oldRoles = usr.Deleted ? oldRoles & CompRole.HumanEvalator : (oldRoles & CompRole.HumanEvalator) | newRoles;
          Lib.setRolesEx(dbUser, (long)newRoles);
          //dbUser.RolesEx = (long)oldRoles; //nastav novou roli
        }
        //Zaloz nove users
        var emails = actUsers.Where(u => u.UserId == 0).Select(u => u.EMail).ToList(); //vsechny nove emaily
        var newUsers = db.Users.Where(u => emails.Contains(u.EMail)).ToArray(); //existujici Users
        foreach (var cusr in actUsers.Where(u => u.UserId == 0)) {
          //adjust noveho admina 
          var usr = newUsers.FirstOrDefault(u => u.EMail == cusr.EMail); //Zacni Userem
          CompanyUsers compUsr = null;
          if (usr == null) usr = NewData.Login.PrepareUser(cusr.EMail, db); //pro neexistujiciho zaloz usera v prepared stavu
          else compUsr = usr.CompanyUsers.FirstOrDefault(cu => cu.CompanyId == cusr.CompanyId); //pro existujiciho usera nalezni Cmpany Usera
          var newRoles = cusr.RoleEx.Role;
          if (compUsr == null) {
            db.CompanyUsers.Add(compUsr = new CompanyUsers() { Created = DateTime.UtcNow, CompanyId = cusr.CompanyId, User = usr }); //Company User neexistuje => zaloz
            Lib.setRoleParEx(compUsr, cusr.RoleEx);
          }  else {
            var oldRoles = (CompRole)compUsr.Roles;
            newRoles |= oldRoles & CompRole.HumanEvalator;
            Lib.setRolesEx(compUsr, (long)newRoles);
            //compUsr.RolesEx = newRoles;
          }
        }
      }

      Lib.SaveChanges(db);
    }

  }

}

