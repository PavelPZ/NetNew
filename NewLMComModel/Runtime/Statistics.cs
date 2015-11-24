using LMComLib;
using LMNetLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Statistics {

  public static class Lib {

    static Lib() {
      Machines._rootDir = Path.GetDirectoryName(HostingEnvironment.MapPath("~/x.x"));
      adjustProducts(); //Toc
    }

    public static Company company(int id) {
      ObjectCache cache = MemoryCache.Default;
      var compId = id.ToString();
      Company res;
      lock (typeof(Lib)) {
        if (cache.Contains(compId) /*&& Machines.machine!="pz-w8virtual"*/ && Machines.machine!="lm-frontend-5") return (Company)cache.Get(compId);
        res = adjustCompany(id);
        cache.Add(compId, res, new CacheItemPolicy { SlidingExpiration = slideExpiration });
      }
      return res;
    }
    static TimeSpan slideExpiration = new TimeSpan(0, 30, 0);

    //struktura vsech Web produktu
    public static Dictionary<string, Prod> prodDir;
    public static CurrentProd[] currentProducts;
    public static List<Toc> tocIdsDir = new List<Toc>(); //plati tocIdsDir[toc.compId]==toc

    //JSON obsah ShortData pro cviceni
    public struct exResult {
      public bool done;
      public ushort end;
      public ushort elapsed;
      public sbyte score;
    }

    //nacti runtime data pro company
    static Company adjustCompany(int id) {
      Company res = new Company { Id = id };
      var db = NewData.Lib.CreateContext();
      Action<User, int> fillDep = (u, depId) => u.Department = res.DepartDir[depId];

      //Departments a intervals
      var deps = NewData.AdminServ.GetDepartment(id);
      res.DepartDir = new Dictionary<int, Department>();
      res.Departs = adjustDeps(deps.Departments, null, res.DepartDir);

      //all company users
      res.UserDir = db.CompanyUsers.
        Where(cu => cu.CompanyId == id && cu.DepartmentId != null).
        Select(cu => new {
          depId = cu.CompanyDepartment.Id,
          compUserId = cu.Id,
          user = new User {
            FirstName = cu.User.FirstName,
            LastName = cu.User.LastName,
            EMail = cu.User.EMail
          }
        }).
        Each(cu => cu.user.Department = res.DepartDir[cu.depId]).
        ToDictionary(cu => cu.compUserId, cu => cu.user);

      //CourseData
      foreach (var userProducts in db.CourseDatas.Where(cd => cd.CourseUser.CompanyUser.CompanyId == id).Select(cd => new { cd.ShortData, cd.CourseUser.ProductId, cd.Key, compUserId = cd.CourseUser.UserId }).GroupBy(ex => new { ex.ProductId, ex.compUserId })) {
        var pd = prodDir[userProducts.Key.ProductId]; User usr; if (!res.UserDir.TryGetValue(userProducts.Key.compUserId, out usr)) continue;
        var data = userProducts.Where(e => !string.IsNullOrEmpty(e.ShortData)).ToArray(); if (data.Length <= 0) continue;

        //Skip data (course root je vzdy prvni node produktu, viz d:\LMCom\rew\Web4\Courses\CourseLib.ts 
        var skipDb = data.FirstOrDefault(d => d.Key == pd.actCourseRootUrl); HashSet<int> skipList = null;
        if (skipDb != null) { //existuje dato pro courseRoot
          var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(skipDb.ShortData);
          if (dict.Count > 0) {
            if (usr.SkipedIds == null) usr.SkipedIds = new Dictionary<string, HashSet<int>>();
            usr.SkipedIds.Add(skipDb.ProductId, skipList = new HashSet<int>(dict.Keys.Select(key => pd.Dir[key].Id)));
          }
        }

        //exercise results
        Dictionary<int, UserEx> exercs = null;
        foreach (var exDb in data) {
          Toc tc; pd.Dir.TryGetValue(exDb.Key, out tc); if (tc == null || !tc.isType(CourseMeta.runtimeType.ex)) continue; //neni v TOC nebo neni cviceni -> ignoruj
          var exRes = Newtonsoft.Json.JsonConvert.DeserializeObject<exResult>(exDb.ShortData);
          //if (!exRes.done || (skipList != null && skipList.Overlaps(tc.parentsIds()))) continue; //cviceni neni hotovo nebo jeho parent je skiped => ignoruj ho
          if (!exRes.done) continue; //cviceni neni hotovo => ignoruj ho
          if (exercs == null) {
            if (usr.Exercises == null) usr.Exercises = new Dictionary<string, Dictionary<int, UserEx>>();
            usr.Exercises.Add(userProducts.Key.ProductId, exercs = new Dictionary<int, UserEx>());
          }
          exercs.Add(tc.Id, new UserEx {
            End = exRes.end,
            Score = exRes.score,
            Elapsed = exRes.elapsed,
            Toc = tc,
            User = usr,
            Product = pd,
          });
        }
      }
      return res;
    }

    static bool datOk(CourseMeta.runtimeType type) { return (type & wrong) == 0; } static CourseMeta.runtimeType wrong = CourseMeta.runtimeType.taskTestInCourse | CourseMeta.runtimeType.taskPretest | CourseMeta.runtimeType.test;

    //Nacti strukturu vsech Web produktu
    public static void adjustProducts() {
      if (prodDir != null) return;
      var prodExpandedFn = Machines.rootPath + @"Data\ActProductsExpanded.xml";
      CourseMeta.products prodsDb = XmlUtils.FileToObject<CourseMeta.products>(prodExpandedFn);
      prodDir = prodsDb.Items.Select(p => adjustProd((CourseMeta.product)p)).ToDictionary(p => p.Url, p => p);
      foreach (var pr in prodDir.Values) pr.readed();
      //aktualni produkty
      var prodCurrentFn = Machines.rootPath + @"Data\productsCurrent.xml";
      var prods = File.Exists(prodCurrentFn) ? XmlUtils.FileToObject<CourseMeta.products>(prodCurrentFn) : prodsDb;
        //XmlUtils.FileToObject<CourseMeta.products>(prodCurrentFn).items.OfType<CourseMeta.product>() :
        //prodsDb.items.OfType<CourseMeta.product>();
      currentProducts = prods.Items.OfType<CourseMeta.product>().Select(p => new CurrentProd { Url = p.url, Title = p.title }).ToArray();
    }
    static Prod adjustProd(CourseMeta.product data) {
      var res = new Prod { Url = data.url, Dir = new Dictionary<string, Toc>(), Title = data.title, actCourseRootUrl = data.Items[0].url };
      res.Items = data.Items[0].Items.Select(dt => adjustToc(dt, res.Dir, 0)).ToArray(); //prvni polozka v items produktu je kurz x testGlobalAdmin
      if (res.Items.Length == 1) res.Items = res.Items[0].Items;
      return res;
    }
    static Toc adjustToc(CourseMeta.data data, Dictionary<string, Toc> dir, int lev) {
      var res = new Toc { Title = data.title, Type = data.type };
      res.Items = data.Items == null ? null : data.Items.Select(dt => adjustToc(dt, dir, lev + 1)).ToArray();
      dir.Add(data.url, res);
      return res;
    }

    //Nacti departments a intervals pro firmu
    static Department adjustDeps(Admin.Department srcDep, Department parent, Dictionary<int, Department> dir) {
      if (srcDep == null) return null;
      var res = new Department {
        Title = srcDep.Title,
        Parent = parent,
      };
      res.Childs = srcDep.Items == null ? null : srcDep.Items.Select(sd => adjustDeps(sd, res, dir)).ToArray();
      dir.Add(srcDep.Id, res);
      return res;
    }


    public static string progressInfo_html(int exCount, int skipedCount, int completed) {
      var sk = (int)(skipedCount * 100 / exCount); var compl = (int)(completed * 100 / exCount); var todo = (int)((exCount - skipedCount - completed) * 100 / exCount);
      var diff = 100 - todo - sk - compl;
      if (diff != 0) { if (sk > compl && sk > todo) sk = sk += diff; else if (compl > todo) compl += diff; else todo += diff; }
      var txt1 = string.Format("{0}/{1}/{2}", skipedCount, completed, exCount - skipedCount - completed, exCount);
      var txt2 = string.Format("(%: {1}/{2}/{3})", exCount, sk, compl, todo);
      return string.Format(progressInfo_fragment, progressWidth, (int)(progressWidth * skipedCount / exCount), (int)(progressWidth * completed / exCount), txt1, txt2);
    } const string progressInfo_fragment = "<div class='dx-bar dx-progress' style='width:{0}px'><div class='bar c1' style='width:{1}px'></div><div class='bar c2' style='left:{1}px;width:{2}px'></div><div class='bar-title'>{3} <small>{4}</small></div></div>";

    public static string scoreInfo_html(bool visible, int complNotPassiveCnt, int points) {
      if (!visible) return null;
      var txt = (int)(points / complNotPassiveCnt);
      return visible ? string.Format(scoreInfo_fragment, scoreWidth, (int)(scoreWidth * points / complNotPassiveCnt / 100), txt, (int)(points / 100), complNotPassiveCnt) : null;
      //} const string scoreInfo_fragment = "<div class='dx-bar dx-score' style='width:{0}px'><div class='bar c1' style='width:{1}px'></div><div class='bar-title'>{2}% <small>({3}/{4})</small></div></div>";
    } const string scoreInfo_fragment = "<div class='dx-bar dx-score' style='width:{0}px'><div class='bar c1' style='width:{1}px'></div><div class='bar-title'>{2}%</div></div>";

    public const int progressWidth = 200;
    public const int scoreWidth = 150;

  }

  //Runtime dato pro jednu firmu
  public class Company {
    public int Id;
    public Dictionary<int, User> UserDir; //companyUserId => user
    public Dictionary<int, Department> DepartDir; //departmentId => Departments
    public Department Departs; //stromova struktura departments
    public IntervalsConfig Intervals; //intervaly
    //controls caches
    Dictionary<string, TocExsRow> tocExsCache;
    public TocExsRow adjustProductForCompany(string prodUrl) {
      if (tocExsCache == null) tocExsCache = new Dictionary<string, TocExsRow>();
      TocExsRow res;
      if (!tocExsCache.TryGetValue(prodUrl, out res)) tocExsCache.Add(prodUrl, res = TocExs.createProductForCompany(Id, prodUrl));
      return res;
    }
  }

  //Runtime dato pro jednoho firemniho uzivatele
  public class User {
    public string EMail;
    public string FirstName;
    public string LastName;
    public Department Department;
    public Dictionary<string, HashSet<int>> SkipedIds; //skiped Url pro produkty
    public Dictionary<string, Dictionary<int, UserEx>> Exercises; //cviceni pro produkty

    //vybere TOC data ke konkretnimu produktu
    public ProductData getProductData(string prodUrl) {
      var pd = new ProductData { user = this };
      if (Exercises == null || !Exercises.TryGetValue(prodUrl, out pd.Exercises)) pd.Exercises = new Dictionary<int, UserEx>();
      if (SkipedIds == null || !SkipedIds.TryGetValue(prodUrl, out pd.SkipedIds)) pd.SkipedIds = null;
      return (pd.Exercises == null || pd.Exercises.Count == 0) && (pd.SkipedIds == null || pd.SkipedIds.Count == 0) ? null : pd;
    }
    public class ProductData { public User user; public HashSet<int> SkipedIds; public Dictionary<int, UserEx> Exercises; public bool isSkiped(int id) { return SkipedIds != null && SkipedIds.Contains(id); } }
  }

  //firemni department
  public class Department {
    public string Title;
    public Department Parent;
    public Department[] Childs;
  }

  //vysledek jednoho cviceni
  public class UserEx {
    public User User;
    public Toc Toc;
    public Prod Product;

    public bool isSkiped;
    public UInt16 End;
    public UInt16 Elapsed;
    public sbyte Score;
    public sbyte IntScore;
    public sbyte IntElapsed;
    public sbyte IntEpoch;

    public int Id { get { return Toc.Id; } }
    public int ParentId { get { return Toc.Parent.Id; } }

    public UserEx self { get { return this; } }

    //view
    public bool done { get { return !isSkiped; } }
    public int exCount { get { return 1; } }
    public int complPassiveCnt { get { return isSkiped || Score >= 0 ? 0 : 1; } }
    public int complNotPassiveCnt { get { return isSkiped || Score < 0 ? 0 : 1; } }
    public int skipedCount { get { return isSkiped ? 1 : 0; } }
    public int score { get { return isSkiped || Score < 0 ? 0 : Score; } }
    public int elapsed { get { return isSkiped ? 0 : Elapsed; } }

    //tocDetail grid
    public int tocId { get { return Toc.Id; } }
    public string userTitle {
      get {
        if (string.IsNullOrEmpty(User.FirstName) && string.IsNullOrEmpty(User.LastName)) return User.EMail;
        return User.FirstName + " " + User.LastName + " (" + User.EMail + ")";
      }
    }

    public string ProgressBarHtml { get { return Lib.progressInfo_html(1, skipedCount, complNotPassiveCnt + complPassiveCnt); } }
    public string ScoreBarHtml { get { return Lib.scoreInfo_html(!isSkiped && complNotPassiveCnt > 0, 1, score); } }
  }

  /************ Prod a Toc: globalni staticka struktura pro cely web *************/
  public class Prod : Toc {
    public string Url { get; set; }
    public string actCourseRootUrl;
    public Dictionary<string, Toc> Dir;
  }

  public class CurrentProd {
    public string Url { get; set; }
    public string Title { get; set; }
  }


  public partial class Toc {
    public int Id;
    public int exCount;
    public string Title;
    public CourseMeta.runtimeType Type;
    public int ParentId { get { return Parent == null ? 0 : Parent.Id; } }
    public Toc Parent;
    public Toc[] Items;
    public bool isType(CourseMeta.runtimeType tp) { return (Type & tp) == tp; }
    public object tempData; //temorary data pro not thread save budovani konkretnich Toc struktur
    public void readed() {
      Id = Lib.tocIdsDir.Count;
      Lib.tocIdsDir.Add(this);
      if (Items == null || Items.Length == 0) { exCount = 1; return; }
      foreach (var it in Items) { it.Parent = this; it.readed(); exCount += it.exCount; }
    }
    public IEnumerable<int> parentsIds() { var p = Parent; while (p != null) { yield return p.Id; p = p.Parent; } }
    public IEnumerable<Toc> parents() { var p = Parent; while (p != null) { yield return p; p = p.Parent; } }
    public IEnumerable<Toc> scan() { yield return this; if (Items != null) foreach (var t in Items.SelectMany(it => it.scan())) yield return t; }
  }

  public enum IntType { score, elapsed, period }

  public class Interval {
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

    //pracovni field, slouzi k prenosu compId z napr. lmsql_ScoreIntervals do lmsql_Intervals_FillRow
    public int id;
    public IntType type;
    [XmlIgnore]
    public Interval[] items;
    public string getTitle() {
      if (!string.IsNullOrEmpty(Title)) return Title;
      var idx = Array.IndexOf(items, this);
      switch (type) {
        case IntType.score:
          var max = idx == 0 ? 100 : items[idx - 1].From;
          var min = items[idx].From + 1;
          if (max == min) return min.ToString() + '%';
          return min.ToString() + " - " + max.ToString() + '%';
        case IntType.elapsed:
          if (idx == 0) return toStringValue() + " -";
          return toStringValue() + " - " + items[idx - 1].toStringValue();
        case IntType.period:
          return localizePeriod(items[idx].From, idx == items.Length - 1 ? -1 : items[idx + 1].From, CultureInfo.GetCultureInfo("en-gb"), false);
        default:
          throw new NotImplementedException();
      }
    }

    string toStringValue() {
      switch (type) {
        case IntType.elapsed:
          TimeSpan ts = new TimeSpan(0, 0, From);
          return ts.ToString();
        case IntType.period:
          return null;
        default:
          throw new NotImplementedException();
      }
    }

    public static int ScoreId(Interval[] ints, byte? score) {
      if (score == null) return ints.Length - 1;
      byte s = (byte)score;
      for (int i = 0; i < ints.Length; i++) if (s > ints[i].From) return i;
      return 0;
    }

    public static int SecId(Interval[] secInt, int sec) {
      for (int i = 0; i < secInt.Length; i++) if (sec >= secInt[i].From) return i;
      return 0;
    }
  }

  public class Intervals {
    public Interval[] Items;

    static public int MonthCode(int Month, int Year) { return (Year - 2000) * 100 + Month; }

    public static Intervals PeriodDefault() {
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
          new Interval() {From=99},
          new Interval() {From=97},
          new Interval() {From=94},
          new Interval() {From=89},
          new Interval() {From=82},
          new Interval() {From=70},
          new Interval() {From=56},
          new Interval() {From=35},
          new Interval() {From=-1},
        }
      };
      return res;
    }

    public static Intervals ElapsedDefault() {
      string[] secYearDefault = new string[] { "4.00:00:00", "2.20:00:00", "2.00:00:00", "1.10:00:00", "1.00:00:00", "17:00:0", "12:00:00", "08:30:00", "06:00:00", "04:00:00", "03:00:00", "02:00:00", "01:30:00", "01:00:00", "00:45:00", "00:30:00", "00:20:00", "00:15:00", "00:10:00", "00:00:00" };
      var res = new Intervals() {
        Items = secYearDefault.Select((s, idx) => new Interval() { From = (int)TimeSpan.Parse(s).TotalSeconds /*, Title = s + " - " + (idx == 0 ? "" : secYearDefault[idx - 1])*/ }).ToArray()
      };
      return res;
    }
  }

  public class IntervalsConfig {
    public Intervals Secs; //elapsed
    public Intervals Scores; //score
    public Intervals Periods; //perion
    IntervalsConfig checkEmpty(bool isSave) {
      if (Scores == null || Scores.Items == null || Scores.Items.Length < 2) Scores = isSave ? new Intervals() : Intervals.ScoreDefault();
      if (Secs == null || Secs.Items == null || Secs.Items.Length < 2) Secs = isSave ? new Intervals() : Intervals.ElapsedDefault();
      if (Periods == null || Periods.Items == null || Periods.Items.Length < 2) Periods = isSave ? new Intervals() : Intervals.PeriodDefault();
      return this;
    }
  }

}
