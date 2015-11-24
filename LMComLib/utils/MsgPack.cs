using System;
#if JavaScript
using System.Collections.Generic;
using System.Linq;
using SharpKit.JavaScript;
using System.Text;
using LMNetLib;
using LMComLib;
using SharpKit.jQuery;
#else
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using MsgPack;
using LMNetLib;
using System.IO;
using System.Net;
#endif


//https://github.com/msgpack/msgpack-cli/wiki/Manual-packings
//http://jsinq.codeplex.com/
namespace MsgPack {
#if !JavaScript

  //objekt pro netypovou serializaci x deserializaci
  public class PackResult : IPackResult {
    public Dictionary<string, object> Map; //name - value par, value je budto literal nebo PackResult
    public List<object> Array; //array of literals nebo PackResult

    internal PackResult(bool isTree) {
      if (isTree) Array = new List<object>(); else Map = new Dictionary<string, object>();
    }
    string propName;
    void IPackResult.addValue(object val) {
      if (Array != null)
        Array.Add(val);
      else {
        if (propName == null) propName = (string)val;
        else { Map.Add(propName, val); propName = null; }
      }
    }
    IPackResult IPackResult.createPropObject(string propName, bool isArray) {
      return new PackResult(isArray);
    }
  }

  //Deserialize interface pro parseLow
  internal interface IPackResult {
    void addValue(object val);
    IPackResult createPropObject(string propName, bool isArray);
  }

  //wrapper pro MessagePack serializaci x deserializaci
  public static class Serializer {

    public static string BytesToHex(byte[] data) { return null; }
    public static string BytesToBase64(byte[] data) { return null; }
    public static byte[] HexToBytesx(string str) { return null; }
    public static byte[] Base64ToBytes(string str) { return null; }

    //unpack typed object
    public static T parseObject<T>(Unpacker unp) where T : class, new() {
      unp.Read();
      T res = new T();
      if (!unp.IsMapHeader) throw new Exception();
      using (var subUnp = unp.ReadSubtree()) parseLow(subUnp, new ObjPackResult(res));
      return res;
    }

    //unpack typed object array
    public static IEnumerable<T> parseObjectArray<T>(Unpacker unp) where T : class, new() {
      unp.Read();
      if (!unp.IsArrayHeader) throw new Exception();
      ObjPackResult res = new ObjPackResult(null, () => new T());
      using (var subUnp = unp.ReadSubtree()) parseLow(subUnp, res);
      return res.Array.Cast<T>();
    }

    //unpack untyped object
    public static PackResult parse(Unpacker unp) {
      unp.Read();
      if (!unp.IsMapHeader) throw new Exception();
      PackResult res = new PackResult(false);
      using (var subUnp = unp.ReadSubtree()) parseLow(subUnp, res);
      return res;
    }

    //pack typed object
    public static MessagePackObject packObj(object obj) {
      MessagePackObjectDictionary dict = new MessagePackObjectDictionary();
      foreach (var pr in allProps(obj.GetType())) {
        object val = pr.Get(obj);
        if (val == null) continue;
        if (pr.isArray) {
          dict.Add(new MessagePackObject(pr.PropName), packArray((val as System.Collections.IEnumerable).Cast<object>().Select(v => pr.ConvertTo(v))));
        } else
          dict.Add(new MessagePackObject(pr.PropName), isRegistered(val.GetType()) ? packObj(val) : MessagePackObject.FromObject(pr.ConvertTo(val)));
      }
      return new MessagePackObject(dict);
    }

    //pack typed object array
    public static MessagePackObject packArray(IEnumerable<object> objs) {
      var l = objs.Select(a => (isRegistered(a.GetType()) ? packObj(a) : MessagePackObject.FromObject(a))).ToList(); //TODO: difotni converter
      return new MessagePackObject(l);
    }

    //pack untyped object
    public static MessagePackObject pack(PackResult obj) {
      if (obj.Map != null) {
        MessagePackObjectDictionary dict = new MessagePackObjectDictionary();
        foreach (var pr in obj.Map) {
          if (pr.Value == null) continue;
          MessagePackObject val = pr.Value is PackResult ? pack((PackResult)pr.Value) : MessagePackObject.FromObject(pr.Value);
          dict.Add(new MessagePackObject(pr.Key), val);
        }
        return new MessagePackObject(dict);
      } else if (obj.Array != null) {
        List<MessagePackObject> lst = obj.Array.Select(v => v is PackResult ? pack((PackResult)v) : MessagePackObject.FromObject(v)).ToList();
        return new MessagePackObject(lst);
      } else throw new Exception();
    }

    //odvozene utility
    public static void ObjectToStream(object obj, Stream str) {
      using (Packer packer = Packer.Create(str, false)) packObj(obj).PackToMessage(packer, null);
    }
    public static byte[] ObjectToBytes(object obj) {
      using (MemoryStream ms = new MemoryStream())
      using (Packer packer = Packer.Create(ms, false)) {
        packObj(obj).PackToMessage(packer, null);
        return ms.ToArray();
      }
    }
    public static byte[] ObjectToBytesEnc(object obj) {
      byte[] res = ObjectToBytes(obj);
      return LowUtils.Encrypt(res, LowUtils.encryptKey);
    }
    public static void ObjectsToStream(IEnumerable<object> objs, Stream str) {
      using (Packer packer = Packer.Create(str, false)) packArray(objs).PackToMessage(packer, null);
    }
    public static byte[] ObjectsToBytes(IEnumerable<object> objs) {
      using (MemoryStream ms = new MemoryStream())
      using (Packer packer = Packer.Create(ms, false)) {
        packArray(objs).PackToMessage(packer, null);
        return ms.ToArray();
      }
    }

    public static T StreamToObject<T>(Stream str) where T : class, new() {
      using (Unpacker unp = Unpacker.Create(str, false)) return parseObject<T>(unp);
    }
    public static T BytesToObject<T>(byte[] data) where T : class, new() {
      if (data == null || data.Length==0) return null;
      using (MemoryStream ms = new MemoryStream(data)) return StreamToObject<T>(ms);
    }
    public static IEnumerable<T> StreamToObjects<T>(Stream str) where T : class, new() {
      using (Unpacker unp = Unpacker.Create(str, false)) return parseObjectArray<T>(unp);
    }
    public static IEnumerable<T> BytesToObjects<T>(byte[] data) where T : class, new() {
      if (data == null || data.Length == 0) return null;
      using (MemoryStream ms = new MemoryStream(data)) return StreamToObjects<T>(ms);
    }

    //register info for typed pack x unpack
    public static void Register(Type typ, Prop[] arrayProps, params string[] skalarProps) {
      if (infos.ContainsKey(typ)) return;
      infos.Add(typ, new SerializeInfo() {
        Props = arrayProps.Concat(skalarProps.Select(p => new Prop(p))).ToArray()
      });
    }

    public class Prop { //info about prop
      //Scalar constructors:
      public Prop(string propName) : this(false, propName, null, null, null, null, null) { }//scalar simple prop
      public Prop(string propName, Func<object, bool, object> ConverterTo) : this(false, propName, null, null, null, null, ConverterTo) { }//scalar converted prop
      public Prop(string propName, Func<object, object> getter, Action<object, object> setter) : this(false, propName, getter, setter, null, null, null) { }//scalar simple prop
      public Prop(string propName, Func<object, object> getter, Action<object, object> setter, Func<object, bool, object> ConverterTo) : this(false, propName, getter, setter, null, null, ConverterTo) { }//scalar converted prop
      public Prop(string propName, Func<object, object> getter, Action<object, object> setter, Func<object> createNewItem) : this(false, propName, getter, setter, null, createNewItem, null) { }//scalar simple prop
      public Prop(string propName, Func<object, object> getter, Action<object, object> setter, Func<object> createNewItem, Func<object, bool, object> ConverterTo) : this(false, propName, getter, setter, null, createNewItem, ConverterTo) { }//scalar converted prop
      //Array constructors:
      public Prop(string propName, Func<IEnumerable<object>, object> setArray) : this(true, propName, null, null, setArray, null, null) { } //scalar simple array
      //public Prop(string propName, Func<IEnumerable<object>, object> setArray, Func<Type> getArrayItemType) : this(true, propName, null, null, setArray, getArrayItemType, null) { }//object array
      public Prop(string propName, Func<IEnumerable<object>, object> setArray, Func<object, bool, object> ConverterTo) : this(true, propName, null, null, setArray, null, ConverterTo) { } //scalar converted array
      public Prop(string propName, Func<object, object> getter, Action<object, object> setter, Func<IEnumerable<object>, object> setArray) : this(true, propName, getter, setter, setArray, null, null) { } //scalar simple array
      public Prop(string propName, Func<object, object> getter, Action<object, object> setter, Func<IEnumerable<object>, object> setArray, Func<object> createNewItem) : this(true, propName, getter, setter, setArray, createNewItem, null) { }//object array
      public Prop(string propName, Func<object, object> getter, Action<object, object> setter, Func<IEnumerable<object>, object> setArray, Func<object, bool, object> ConverterTo) : this(true, propName, getter, setter, setArray, null, ConverterTo) { } //scalar converted array

      Prop(bool isArray, string propName, Func<object, object> getter, Action<object, object> setter, Func<IEnumerable<object>, object> finishArray, Func<object> createNewItem, Func<object, bool, object> ConverterTo) {
        this.isArray = isArray; this.PropName = propName; this.getter = getter; this.setter = setter; this.finishArray = finishArray; this.createNewItem = createNewItem; this.ConverterTo = ConverterTo;
      }
      public bool isArray;
      public string PropName; //prop name
      public Func<object, bool, object> ConverterTo; //converter do (true) a z (false) pro scalarni nebo array item property
      public Func<object> createNewItem; //vytvori array item objekt, only for typed array props (not int[] etc.)
      public Func<IEnumerable<object>, object> finishArray; //prevede seznam objektu do property formatu 
      public Func<object, object> getter; //prop getter
      public Action<object, object> setter; //prop setter

      public object Get(object obj) { return getter == null ? LMNetLib.TypeHelper.GetValue(obj, PropName) : getter(obj); }
      public void Set(object obj, object val) { if (setter == null) LMNetLib.TypeHelper.SetValue(obj, PropName, val); else setter(obj, val); }
      public object ConvertTo(object obj) { return ConverterTo == null ? obj : ConverterTo(obj, true); }
      public object ConvertFrom(object obj) { return ConverterTo == null ? obj : ConverterTo(obj, false); }
      public object CreateNew(object obj) { return createNewItem == null ? LMNetLib.TypeHelper.Create(LMNetLib.TypeHelper.PropType(obj.GetType(), PropName)) : createNewItem(); }
    }

    public class SerializeInfo { //info about serialize type
      public Prop[] Props; //list of property properties
    }

    /**** not public ****/

    //logika parsovani Unpackera
    static void parseLow(Unpacker unp, IPackResult root) {
      object val; object oldVal = null;
      while (unp.Read()) {
        if (unp.IsMapHeader || unp.IsArrayHeader) {
          IPackResult subObj = root.createPropObject((string)oldVal, unp.IsArrayHeader); //oldVal==null iff objektovy prvek array
          using (var subUnp = unp.ReadSubtree()) parseLow(subUnp, subObj);
          root.addValue(subObj);
        } else {
          val = unp.Data.Value.ToObject();
          root.addValue(val);
          oldVal = val;
        }
      }
    }

    //**** Registrace
    static Dictionary<Type, SerializeInfo> infos = new Dictionary<Type, SerializeInfo>();

    static bool isRegistered(Type typ) { return infos.ContainsKey(typ); }

    static IEnumerable<Prop> allProps(Type typ) {
      return infos[typ].Props;
    }

    static Func<object> getCreateNewItem(Type typ, string propertyName) {
      Prop prop = infos[typ].Props.First(i => i.PropName == propertyName);
      return prop.createNewItem;
    }

    static Prop getProp(Type typ, string propertyName) {
      return infos[typ].Props.FirstOrDefault(i => i.PropName == propertyName);
    }

    //IPackResult pro typed unpack
    internal class ObjPackResult : IPackResult {
      internal ObjPackResult(object obj, Func<object> createNewItem) {
        this.obj = obj; this.createNewItem = createNewItem; Array = new List<object>();
      }
      internal ObjPackResult(object obj) {
        this.obj = obj;
      }
      internal object obj;
      string propName;
      internal List<object> Array; Func<object> createNewItem;

      void IPackResult.addValue(object val) {
        if (Array != null)
          Array.Add(val is ObjPackResult ? ((ObjPackResult)val).obj : val);
        else {
          if (propName == null) propName = (string)val;
          else try {
              Prop pr = getProp(obj.GetType(), propName); bool alreadyConverted = false;
              if (val is ObjPackResult) {
                ObjPackResult valObj = (ObjPackResult)val;
                if (valObj.Array != null) {
                  val = pr.finishArray(pr == null ? valObj.Array : valObj.Array.Select(v => pr.ConvertFrom(v)));
                  alreadyConverted = true;
                } else
                  val = valObj.obj;
              }
              pr.Set(obj, alreadyConverted ? val : pr.ConvertFrom(val));
            } finally { propName = null; }
        }
      }

      IPackResult IPackResult.createPropObject(string propName, bool isArray) {
        if (isArray) {
          //nalezen zacatek pole, vytvor novy ObjPackResult
          return new ObjPackResult(obj, getCreateNewItem(obj.GetType(), propName));
        } else {
          //budto objektova property (propName!=null) nebo prvek pole (createNewItem!=null)
          object propObj = propName == null ? createNewItem() : getProp(obj.GetType(), propName).CreateNew(obj);
          return new ObjPackResult(propObj);
        }
      }

    }

    public static void Download<T>(string url, Action<T, Exception> callback) where T : class, new() {
      WebClient wc2 = new WebClient();
      wc2.OpenReadCompleted += new OpenReadCompletedEventHandler((s2, e2) => {
        if (e2.Error != null) { callback(null, e2.Error); return; }
        byte[] data = new byte[(int)e2.Result.Length];
        e2.Result.Read(data, 0, data.Length);
        callback(BytesToObject<T>(data), null);
      });
      wc2.OpenReadAsync(new Uri(url));
    }

#else
  [JsType(JsMode.Clr, Filename = "~/res/common/MsgPack.js", NativeOverloads = false, NativeCasts = true, Name = codeNames.MsgPack_Serializer)]
  public static class Serializer {

    public static byte[] ObjectToBytes(object obj) {
      return msgpack.pack(obj, false).As<byte[]>();
    }
    public static byte[] ObjectToBytesEnc(object obj) {
      return null;
    }
    public static byte[] ObjectsToBytes(IEnumerable<object> objs) {
      return ObjectToBytes(objs.ToArray());
    }

    public static T BytesToObject<T>(byte[] data) where T : class, new() {
      return JsStringExt.MsgPack_UnpackEx<T>(data);
    }
    public static IEnumerable<T> BytesToObjects<T>(byte[] data) where T : class, new() {
      return JsStringExt.MsgPack_UnpackEx<T>(data).As<IEnumerable<T>>();
    }

    public static void Download<T>(string url, Action<T, Exception> callback) where T : class, new() {
      jQuery.ajax(new AjaxSettings() {
        url = url,
        async = true,
        dataType = "jsonp",
        error = (jqXHR, code, error) => {
          callback(null, error);
        },
        success = (obj, status, jqXHR) => {
          T res = MsgPack.Serializer.BytesToObject<T>(LowUtils.Base64Encode(obj.As<JsObject>()["data"].As<string>()));
          callback(res, null);
        },
        beforeSend = (xhr, sett) => trace(() => "signature.url=" + sett.url)
      });
      /*msgpack.download(url, new ajaxOptions() { timeoutSec = 10 }, (res, opt, st) => {
        Exception exp = !st.ok ? new Exception("Return code: " + st.ToString()) : null;
        callback(res.As<T>(), exp);
      });*/
    }

    static void trace(Func<string> msg) {
      if (!Log.isLogging()) return;
      Log.log("MsgPack", msg());
    }
#endif

    public static Func<object, bool, object> DateTimeConverter = (inp, isTo) => {
      if (isTo) return LowUtils.IntToDate((Int64)inp, null);
      else return LowUtils.DateToInt((DateTime)inp);
    };

    public static Test CreateTestObject() {
      return new Test() {
        Id = 0,
        Name = "n0",
        SubTest = new Test() { Id = 1, Name = "n1", Tests = new Test[] { new Test() { Date = DateTime.Now, Id = 11, Ints = new int[] { 1, 2, 3 } }, new Test() { Id = 12 } } },
        Ints = new int[] { 1, 2, 3 },
        //Dates = new DateTime[] { DateTime.Now, DateTime.Now },
        Date = DateTime.Now
      };
    }
    public static void DoTest(StringBuilder sb) {
      string s;
      byte[] res;
#if !JavaScript
      Register(typeof(Test),
        new Prop[] {
          new Prop("Id", o => ((Test)o).Id, (o,v) => {((Test)o).Id = Convert.ToInt32(v);}),
          new Prop("Name", o => ((Test)o).Name, (o,v) => ((Test)o).Name = (string)v),
          new Prop("SubTest", o => ((Test)o).SubTest, (o,v) => ((Test)o).SubTest = (Test)v, () => new Test()),
          new Prop("Ints", o => ((Test)o).Ints, (o,v) => ((Test)o).Ints = (int[])v, val => val.Select(i => Convert.ToInt32(i)).ToArray()),
          new Prop("Tests", o => ((Test)o).Tests, (o,v) => ((Test)o).Tests = (Test[])v, val => val.Cast<Test>().ToArray(), () => new Test()),
          new Prop("DateInt", o => ((Test)o).DateInt, (o,v) => {((Test)o).DateInt = Convert.ToInt64(v);}),
          //new Prop("Dates", o => ((Test)o).Dates, (o,v) => ((Test)o).Dates = (DateTime[])v, val => val.Cast<DateTime>().ToArray(), Serializer.DateTimeConverter),
          //new Prop("Date", o => ((Test)o).Date, (o,v) => ((Test)o).Date = (DateTime)v, Serializer.DateTimeConverter),
        });


      using (MemoryStream ms = new MemoryStream()) {
        using (Packer packer = Packer.Create(ms, false))
          new MessagePackObject(new MessagePackObjectDictionary() { //MAP
            {"Field1",1234}, 
            {"Field2","1234"}, 
            {"Field3", new byte[] {1,2,3,4}},
            {"SubTree", new MessagePackObject( new MessagePackObjectDictionary() { //MAP 
              {"Field1",1234}, 
              {"Field2","1234"}, 
              {"Field3", new byte[] {1,2,3,4}},
              {"Field4", new MessagePackObject(new List<MessagePackObject>() { //ARRAY
                true,
                "2",
                1234
              })},
              {"Field5", false}
            })}
          }).PackToMessage(packer, null);
        ms.Seek(0, SeekOrigin.Begin);
        s = "[" + ms.ToArray().Select(b => b.ToString()).Aggregate((r, i) => r + "," + i) + "];";
        using (Unpacker unp = Unpacker.Create(ms, false)) {
          PackResult rr = parse(unp);
          using (MemoryStream ms2 = new MemoryStream()) {
            using (Packer packer = Packer.Create(ms2, false))
              pack(rr).PackToMessage(packer, null);
            ms2.Seek(0, SeekOrigin.Begin);
            s = "[" + ms.ToArray().Select(b => b.ToString()).Aggregate((r, i) => r + "," + i) + "];";
            using (Unpacker unp2 = Unpacker.Create(ms2, false)) {
              PackResult res2 = parse(unp2);
            }
          }
        }
      }

      sb.Append("<br/>Raw:<br/>");
      using (MemoryStream ms = new MemoryStream()) {
        using (Packer packer = Packer.Create(ms, false)) {
          packer.PackArrayHeader(2);
          packer.PackMapHeader(2);
          packer.PackString("1");
          packer.PackString("value 1");
          packer.PackString("2");
          packer.PackString("value 2");
          packer.PackMapHeader(2);
          packer.PackString("1");
          packer.PackString("value 1");
          packer.PackString("2");
          packer.PackString("value 2");
        }
        sb.Append(LowUtils.Base64Decode(ms.ToArray()));
      }

#endif
      //Object
      s = CreateTestObject().ToString();
      sb.Append("<br/><br/>*** Serialize<br/>Object<br/>");
      sb.Append(s);
      //s = "[" + res.ToArray().Select(b => b.ToString()).Aggregate((r, i) => r + "," + i) + "];";
      Test tst = Test.FromString(s);

      sb.Append("<br/><br/>Array:<br/>");
      //Array
      res = ObjectsToBytes(
        new Test[] { new Test() { Id = 11, Ints = new int[] { 1, 2, 3 } }, new Test() { Id = 12 } }
      );
      sb.Append(LowUtils.Base64Decode(res));
      //s = "[" + res.ToArray().Select(b => b.ToString()).Aggregate((r, i) => r + "," + i) + "];";
      Test[] tsts = BytesToObjects<Test>(res).ToArray();

      return;
      sb.Append("<br/><br/>Large Array:<br/>");
      //Rychlost
      List<Test> lst = new List<Test>();
      for (int i = 0; i < 10000; i++) lst.Add(new Test() { Id = 1, Name = "n1", Tests = new Test[] { new Test() { Id = 11, Ints = new int[] { 1, 2, 3 } }, new Test() { Id = 12 } } });
      res = ObjectsToBytes(lst.ToArray());
      sb.Append(LowUtils.Base64Decode(res));
      sb.Append("<br/><br/><br/>");
      tsts = BytesToObjects<Test>(res).ToArray();

    }

#if JavaScript
    [JsType(JsMode.Clr, Filename = "~/res/common/MsgPack.js", NativeOverloads = true, NativeCasts = true)]
    //[JsType(JsMode.Clr, Filename = "~/res/common/MsgPack.js", Native = true, NativeConstructors=true)]
#endif
    public class Test {
      public int Id;
      public string Name;
      public Test SubTest;
      public int[] Ints;
      public Test[] Tests;
      //public DateTime[] Dates;
      public long DateInt;
      public DateTime Date { get { return LowUtils.IntToDate(DateInt, date__); } set { DateInt = LowUtils.DateToInt(value); date__ = value; } }
      DateTime? date__;

      public static Test FromBytes(byte[] data) {
        return MsgPack.Serializer.BytesToObject<Test>(data);
      }

      public byte[] ToBytes() {
        return MsgPack.Serializer.ObjectToBytes(this);
      }

      public override string ToString() {
        return LowUtils.HexDecode(ToBytes());
      }

      public static Test FromString(string str) {
        return FromBytes(LowUtils.HexEncode(str));
      }
    }
  }
}