#if !net35
namespace NewData {
#else
namespace NewDataNet35 {
#endif


  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Text;
  using System.Web;
  using System.Web.Script.Serialization;
#if !net35
  using LMComLib;
  using LMNetLib;
  using Newtonsoft.Json;
  using System.Configuration;
#else
  using LMComLibNet35;
#endif


  //************  Server cast LANGMaster SCORM rozsireni **********
  public static class ScormExServer {

    static ScormExServer() {
      RewRpcLib.registerCommand(new ScormExServerPar());
    }

    //Oddelovac Base64 textu
    const char delim = ';';

    //Potrebne operace (operace je ulozena v "type" queryObj parametru, napr. ...?type=get_data1_data2&...)
    public enum requestType {
      //no,
      //Ulozeni dat do databaze. Klice jsou v Query stringu, hodnoty data1 a data2 jsou predany pomoci operace POST v parametru "par=..."
      //Parametr obsahuje 2 Base64 hodnoty, oddelene delim=; oddelovacem
      set_data,
      //Nacteni data1 a data2 ze zaznamu, ktere vraci WHERE podminka na zadane klice
      get_data1_data2,
      //Nacteni data1
      get_data1,
      //Nacteni key1str a data2
      get_key1str_data2,
      //Specialni operace: vymazani dat, zadanych 
      //1. WHERE podminkou s klici v queryObj stringu
      //2. seznamem key1str klicu (v POST, oddeleny ;)
      //vice viz metoda ProcessRequest, case requestType.del_all_key1str:
      del_all_key1str,
      //v key2int queryObj je ;-delimited seznam key2int's. Vrati ;-delimited seznam Data2
      get_key2ints_data2,
      //Nacteni data1
      get_data2,
    }

    //************ Pomocne tridy pro uchovani vysledku parsingu queryObj parametru ********

    //Abstraktni predchudce
    public abstract class par {
      public par(string name) { this.name = name; }
      string name;
      //= true, je-li queryObj parametr zadan
      public bool isValue;
      public void dump(StringBuilder sb) {
        sb.Append(name); sb.Append('='); sb.Append(getValue()); sb.Append("; ");
      }
      protected abstract string getValue();
    }

    //Jeden String parametr
    public class strPar : par {
      //Konstructor: ziskani parametru z Requestu
      public strPar(HttpContext context, string name)
        : base(name) {
        var vals = context.Request.QueryString.GetValues(name);
        isValue = vals != null;
        value = isValue ? normalizeNull(vals[0]) : null;
      }
      //Hodnota
      public string value;
      public Guid? guidValue { get { return !isValue ? (Guid?)null : new Guid(value); } }
      protected override string getValue() { return value; }
    }

    //Jeden Numericky parametr
    public class intPar : par {
      public intPar(HttpContext context, string name)
        : base(name) {
        var vals = context.Request.QueryString.GetValues(name);
        isValue = vals != null;
        value = isValue ? Int64.Parse(vals[0]) : 0;
      }
      //Hodnota
      public Int64 value;
      protected override string getValue() { return value.ToString(); }
    }

    //Jeden parametr typu seznam cisel
    public class intsPar : par {
      public intsPar(HttpContext context, string name)
        : base(name) {
        var vals = context.Request.QueryString.GetValues(name);
        isValue = vals != null;
        value = isValue && vals[0] != null && vals[0].Length > 0 ? vals[0].Split(delim).Select(s => Int64.Parse(s)).ToArray() : null;
      }
      //Hodnota
      public Int64[] value;
      protected override string getValue() { return value == null ? null : value.Select(v => v.ToString()).Aggregate((r, i) => r + "," + 1); }
    }

    //Vsechny parametry z rozparsovaneho queryObj string
    public class pars {
      //Konstructor: parse parametru z Requestu
      public pars(HttpContext context) {
        type = LowUtils.EnumParse<requestType>(context.Request["type"]);
        //if (type == requestType.no) return;  //type. Neexistuje => return
        UserId = context.Request["userid"]; //userid
        if (string.IsNullOrEmpty(UserId)) throw new Exception(); //userid neexistuje => exception
        //kvuli ladeni je attemptid zadan v queryObj stringu. V eDoceo verzi se vezme se ze eDoceo session 
        //Z edoceo session se vezme tehdy, pokud se v queryObj parametru attemptid objevi (hodnota je ignorovana)
        AttemptId = new intPar(context, "attemptid");
        AttemptIdStr = new strPar(context, "attemptidstr");
        AttemptIdGuid = new strPar(context, "attemptidguid");
        Key1Str = new strPar(context, "key1str");
        Key2Str = new strPar(context, "key2str");
        Key1Int = new intPar(context, "key1int");
        Key2Int = new intPar(context, "key2int");
        Key2Ints = new intsPar(context, "key2ints");
        Date = new intPar(context, "date");
      }
      public string dump() {
        StringBuilder sb = new StringBuilder();
        sb.Append("userId="); sb.AppendLine(UserId);
        sb.Append("type="); sb.AppendLine(type.ToString());
        foreach (var p in XExtension.Create<par>(AttemptId, AttemptIdStr, AttemptIdGuid, Key1Str, Key2Str, Key1Int, Key2Int, Key2Ints, Date).Where(p => p != null)) p.dump(sb);
        return sb.ToString();
      }
      //Typ operace
      public requestType type;
      //User compId
      public string UserId;
      //kvuli ladeni je attemptid zadan v queryObj stringu. V eDoceo verzi se vezme se ze eDoceo session 
      //Z edoceo session se vezme tehdy, pokud se v queryObj parametru attemptid objevi (hodnota je ignorovana)
      public intPar AttemptId;
      public strPar AttemptIdStr;
      public strPar AttemptIdGuid;
      //Klicova hodnota typu string
      public strPar Key1Str;
      //Klicova hodnota typu string
      public strPar Key2Str;
      //Klicova hodnota typu number
      public intPar Key1Int;
      //Klicova hodnota typu number
      public intPar Key2Int;
      //Hodnota typu seznam cisel
      public intsPar Key2Ints;
      //datum zapisu
      public intPar Date;

      //Kontrola existence parametru. Vyvola vyjímmku, pokud neni nektery ze zadanych parametru uveden v queryObj
      //public void checkValues(params par[] pars) {
      //  if (pars.Any(p => !p.isValue)) throw new Exception();
      //}
    }

    static Dictionary<string, bool> guidProcessed = new Dictionary<string, bool>();
    static TimeSpan maxWait = new TimeSpan(0, 0, 5);

    public class ScormExServerPar : RewRpcLib.reqParLow {
      public override bool isMyType(string type) {
        if (types == null) types = LowUtils.EnumGetValues<requestType>().ToDictionary(v => v.ToString().ToLower(), v => true);
        return types.ContainsKey(type.ToLower());
      } Dictionary<string, bool> types;

      public override string ProcessRequest(HttpContext context) {

        Container db; string txt = null;

        //Parse queryObj stringu
        pars par = new pars(context);
        Logger.LogLow(() => ">>> Input:" + par.dump());
        try {
          //Dle typu operace
          switch (par.type) {
            //case requestType.logger:
            //  Logger.SendLog(context);
            //  break;
            //case requestType.no:
            //  try {
            //    if (lib.CreateContext().LANGMasterScorms.FirstOrDefault(s => s.compId == -1) == null)
            //      context.Response.Write("<h2>Handler called</h2>");
            //  } catch (Exception exp) {
            //    context.Response.Write("<h2>Error</h2>");
            //    context.Response.Write(LowUtils.ExceptionToString(exp, true));
            //  }
            //  break;
            //Set data
            case requestType.set_data:
              //par.checkValues(par.AttemptId, par.Key1Int, par.Key1Str, par.Key2Int, par.Key2Str);
              //Vlastni data jsou v "par" hodnote POST
              var postData = RewRpcLib.getPostData(context);
              string[] parts = postData.Split(delim); //ziskani "par" hodnoty a split
              if (parts.Length != 2) throw new Exception(); //kontrola
              //ulozeni dat
              setData(par, parts[0], parts[1]);
              break;
            //Get data
            case requestType.get_data1:
            case requestType.get_data2:
            case requestType.get_data1_data2:
            case requestType.get_key1str_data2:
            case requestType.get_key2ints_data2:
              //par.checkValues(par.Key1Int, par.AttemptId);
              //nacteni dat, viz getData comment
              txt = getData(par.type, par);
              break;
            case requestType.del_all_key1str:
              //par.checkValues(par.Key1Int, par.AttemptId);

              //*******************************
              //Realizace operace
              //DELETE FROM LANGMasterScorms WHERE <podminka z queryObj stringu> AND key1str IN ('key1','key2',... klice z POST hodnoty)
              //Klicu muze byt hodne, nejdrive se tedy z DB zjisti vsechny hodnoty compId, teprve na tyto hodnoty se pouzije IN operator
              db = Lib.CreateContext();
              //nacteni <compId,Key1Str> z DB pro vsechny zaznamy, urcene klici
              var all = getQuery(db, par).Select(c => new { c.Id, c.Key1Str }).ToArray();
              //Ziskani vsech Key1Str
              string[] keys = RewRpcLib.getPostData(context).Split(delim);
              //z nactenych <compId,Key1Str> vyber compId, odpovidajici klicum v POST. Z vsech compId udelej comma delimited string 
              var idCond = all.Where(ik => keys.Any(k => k == ik.Key1Str)).Select(ik => ik.Id.ToString()).DefaultIfEmpty().Aggregate((r, i) => r + "," + i);
              //comma delimited string pouzij v IN
#if !net35
              //EF7 TODO
              //if (!string.IsNullOrEmpty(idCond)) db.Database.ExecuteSqlCommand("DELETE FROM LANGMasterScorms WHERE id in (" + idCond + ")");
#else
          db.ExecuteCommand("DELETE FROM LANGMasterScorms WHERE id in (" + idCond + ")");
#endif
              break;
            default:
              Logger.Log(">>> Error: Unknown type: " + par.type.ToString());
              throw new Exception("Unknown type: " + par.type.ToString());
          }
          
          if (txt == null) return null;
          //Logger.Log(">>> Output: " + txt);
          var jsonpCallback = context.Request["callback"]; //pro eDoceo JSONP neni potreba realizovat
          if (!string.IsNullOrEmpty(jsonpCallback)) txt = jsonpCallback + "(" + JsonConvert.SerializeObject(txt) + ")"; //pro eDoceo JSONP neni potreba realizovat
        } catch (Exception exp) {
          Logger.Error(exp);
        }
        return txt;
      }
    }
    
    //Insert nebo Update
    static void setData(pars par, string Data1, string Data2) {
      var db = Lib.CreateContext();

      //Where podminka na vsechny klice
      LANGMasterScorms cdata = db.LANGMasterScorms.
        FirstOrDefault(s => s.UserId == par.UserId
          && (par.AttemptIdStr.value==null || s.AttemptIdStr == par.AttemptIdStr.value)
          //&& s.AttemptIdGuid == par.AttemptIdGuid.guidValue  
          && s.AttemptId == par.AttemptId.value
          && (par.Key1Str.value==null || s.Key1Str == par.Key1Str.value)
          && (par.Key2Str.value==null || s.Key2Str == par.Key2Str.value)
          && s.Key1Int == par.Key1Int.value 
          && s.Key2Int == par.Key2Int.value);
      //Zaznam nenalezen, zaloz novy
      if (cdata == null)
#if !net35
        db.LANGMasterScorms.Add(cdata = new LANGMasterScorms() {
#else
        db.LANGMasterScorms.InsertOnSubmit(cdata = new LANGMasterScorm() {
#endif
          UserId = par.UserId,
          AttemptId = par.AttemptId.value,
          AttemptIdStr = par.AttemptIdStr.value,
          AttemptIdGuid = par.AttemptIdGuid.guidValue,
          Key1Str = normalizeNull(par.Key1Str.value),
          Key2Str = normalizeNull(par.Key2Str.value),
          Key1Int = par.Key1Int.value,
          Key2Int = par.Key2Int.value,
          Date = par.Date.value,
        });
      //V zaznamu aktualizuj Data1 a Data2 pole
      cdata.Data1 = Data1; cdata.Data2 = Data2;
      //Save DB
#if !net35
      Lib.SaveChanges(db);
#else
      db.SubmitChanges();
#endif
    }

    //Select
    static string getData(requestType type, pars par) {
      var db = Lib.CreateContext();
      //Where cast queryObj
      var q = getQuery(db, par);
      bool isLog = Logger.LogLowId() != null;
      string[] res;
      string[] log = null;
      //Select cast queryObj
      switch (type) {
        case requestType.get_data1: res = q.Select(s => s.Data1).ToArray(); if (isLog) log = res.Select(s => Encoding.UTF8.GetString(Convert.FromBase64String(s))).ToArray(); break; //Data1
        case requestType.get_data2: res = q.Select(s => s.Data2).ToArray(); if (isLog) log = res.Select(s => Encoding.UTF8.GetString(Convert.FromBase64String(s))).ToArray(); break; //Data1
        case requestType.get_key1str_data2: res = q.SelectMany(s => XExtension.Return(s.Key1Str, s.Data2)).ToArray(); break; //Key1Str, Data2
        case requestType.get_data1_data2: res = q.SelectMany(s => XExtension.Return(s.Data1, s.Data2)).ToArray(); if (isLog) log = res.Select(s => Encoding.UTF8.GetString(Convert.FromBase64String(s))).ToArray(); break; //Data1, Data2
        case requestType.get_key2ints_data2: res = q.Select(s => s.Data2).ToArray(); if (isLog) log = res.Select(s => Encoding.UTF8.GetString(Convert.FromBase64String(s))).ToArray(); break; //Data2
        default: throw new NotImplementedException();
      }
      if (isLog && log != null) Logger.Log(">>> GetData: " + (log.Length==0 ? "" : log.Aggregate((r, i) => r + "\r\n" + i)));
      //Vsechny hodnoty proloz oddelovacem
      StringBuilder sb = new StringBuilder();
      foreach (var s in res) { sb.Append(s); sb.Append(delim); }
      if (sb.Length > 0) sb.Length = sb.Length - 1;
      //vraceni hodnoty pro response
      return sb.ToString();
    }

    //Definice WHERE podminky
    static IEnumerable<LANGMasterScorms> getQuery(Container db, pars par) {
      var res = db.LANGMasterScorms.Where(s => s.UserId == par.UserId); //Test na email vzdy
      if (par.AttemptId.isValue) res = res.Where(s => s.AttemptId == par.AttemptId.value); //neprazdny AttemptId, pridej podminku
      if (par.AttemptIdStr.isValue) res = res.Where(s => s.AttemptIdStr == par.AttemptIdStr.value); //neprazdny AttemptId, pridej podminku
      if (par.AttemptIdGuid.isValue) res = res.Where(s => s.AttemptIdGuid == par.AttemptIdGuid.guidValue); //neprazdny AttemptId, pridej podminku
      if (par.Key1Str.isValue) res = res.Where(s => s.Key1Str == par.Key1Str.value); //neprazdne Key1Str, pridej podminku
      if (par.Key2Str.isValue) res = res.Where(s => s.Key2Str == par.Key2Str.value); //...
      if (par.Key1Int.isValue) res = res.Where(s => s.Key1Int == par.Key1Int.value);
      if (par.Key2Int.isValue) res = res.Where(s => s.Key2Int == par.Key2Int.value);
      if (par.Key2Ints.isValue) res = par.Key2Ints.value == null ? res.Where(s => false) : res.Where(s => par.Key2Ints.value.Contains(s.Key2Int));
      return res;
    }

    static string normalizeNull(string val) { return val == "" ? null : val; }

  }
}
