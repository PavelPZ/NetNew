//JSCrambler API for .NET 4.5
//Copyright LANGMaster.com

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;


namespace JSCrambler {

  public static class Lib {

    //access and secret keys
    public static string accessKey = "3F5DFF670E1427574F92EBBFED90F0DC68ADDC2E";
    public static string secretKey = "D9954BD946208163E2AA70413B581259E316DAC0";

    /*
     * Protect JS Files.
     * 
     * Example: protect first.js and second.js. Use domain=localhost, trial expires during 1 day. Use default template (see defaultTemplate in code).
     * 
     * JSCrambler.lib.Protect(
     *  new JSCrambler.FileItemLow[] { 
     *    new JSCrambler.FileItem(@"d:\temp\first.js", @"d:\temp\first.charMin.js"),
     *    new JSCrambler.FileItem(@"d:\temp\second.js", @"d:\temp\second.charMin.js"),
     *  }, 
     *  "localhost", DateTime.UtcNow.AddDays(1));

    */
    public static void Protect(FileItemLow[] _files, string _domain_lock = null, DateTime? _expiration_date = null, Template templ = null) {

      //upload files for protection to new project and get project compId
      var upload = createProject(_files, templ, _domain_lock, _expiration_date);

      //extract data from JSON
      var projId = upload["id"].ToString();
      var parts = (IEnumerable<dynamic>)upload["sources"];
      projectFile[] fileIds = parts.Select(p => new projectFile() {
        guid = p["id"],
        extension = p["extension"],
        name = p["filename"]
      }).OfType<projectFile>().ToArray();

      //wait for project finishing
      while (true) {
        Thread.Sleep(1000);
        //get project info
        var proj = getProjectInfo(projId);
        //Error => see details in JSCrambler Web site
        string error = proj["error_id"] ?? 0.ToString();
        if (error != null && error != "0") throw new Exception("Error: " + error + ", Message: " + proj["error_message"]);
        //Not yet finished => try again after 1000 msec
        string finished_at = proj["finished_at"] ?? "".ToString();
        if (string.IsNullOrEmpty(finished_at)) continue;
        break;
      }

      // A. pres project files
      //Finished => get protected data
      addFileData(projId, fileIds);
      //save protected data
      foreach (var jsId in fileIds)
        _files[jsId.idx].writeResult(jsId.data);

      // B. pres project ZIP
      //Unzip protected files
      //Stream stream = getProjectZip(email);
      //using (var archive = new ZipArchive(stream)) {
      //  foreach (var file in archive.Entries) {
      //    var idx = jsIds.First(j => j.name == file.Name).idx; 
      //    using (var zip = file.Open())
      //    using (var unzip = _files[idx].gerDestinationStream())
      //      zip.CopyTo(unzip);
      //  }
      //}

      //Delete project
      deleteProject(projId);
    }

    //upload files for protection to new project
    public static dynamic createProject(FileItemLow[] _files, Template templ, string _domain_lock, DateTime? _expiration_date) {
      return Utf8BytesToJson(new Par(_files, templ, _domain_lock, _expiration_date).runHttpRequest());
    }

    //delete project via projectId
    public static void deleteProject(string projectId) {
      var json = Utf8BytesToJson(new Par(methods.delete, "/code/" + projectId + ".zip").runHttpRequest());
    }

    //get protected files
    public static void addFileData(string projectId, projectFile[] jsIds) {
      foreach (var jsId in jsIds)
        using (StreamReader rdr = new StreamReader(new Par(methods.get, "/code/" + projectId + "/" + jsId.id).runHttpRequest()))
          jsId.data = rdr.ReadToEnd();
    }

    //get ZIP with protected files via projectId
    public static Stream getProjectZip(string projectId) {
      return new Par(methods.get, "/code/" + projectId + ".zip").runHttpRequest();
    }

    //get project info
    public static dynamic getProjectInfo(string projectId) {
      return Utf8BytesToJson(new Par(methods.get, "/code/" + projectId + ".json").runHttpRequest());
    }

    //***************** Internal and private section
    static dynamic Utf8BytesToJson(Stream data) {
      using (StreamReader rdr = new StreamReader(data)) {
        var str = rdr.ReadToEnd();
        var res = new JavaScriptSerializer().Deserialize<dynamic>(str);
        return res;
      }
    }

    internal const string apiHost = "api.jscrambler.com";
    internal const int apiPort = 443;
    internal const int apiVersion = 3;

    internal static Template defaultTemplate_ = new Template() {
      mode = Modes.start,
      //member_enumeration = true,
      //literal_hooking = true,
      //dead_code = true,
      //string_splitting = true,
      //function_reorder = true,
      //function_outlining = true,
      //dot_notation_elimination = true,
      //rename_local = true,
      //whitespace = true,
      //literal_duplicates = true,
      //constant_folding = true,
      //dead_code_elimination = true,
      //dictionary_compression = true,
      //self_defending = true,
    };

    internal static Template defaultTemplate = new Template() {
      mode = Modes.start, //195.055 => 114.203
      literal_hooking = true, //116.807
      //dead_code = true, //116.665
      //string_splitting = true, //144.255
      function_reorder = true, //114.116
      function_outlining = true, //136.199
      member_enumeration = true, //114.758
      dot_notation_elimination = true, //122.426
      self_defending = false, //149.424 zlobi

      rename_local = true, //94.289
      whitespace = true, //113.587
      literal_duplicates = true, //113.877
      constant_folding = true, //113.911
      dead_code_elimination = true, //114.317
      dictionary_compression = true, //94.776
      //rename_local + whitespace + literal_duplicates + dead_code_elimination + dictionary_compression //91.259
      //... + self_defending + dot_notation_elimination + member_enumeration //123.675
      //... + literal_hooking + string_splitting + function_reorder + function_outlining //118.158
      //all //468.790
      //all - function_outlining  //382.620
      //all - function_reorder  //433.897
      //all - string_splitting  //156.971
      //all - self_defending  //189.474
      //all - string_splitting - function_outlining //127.535
    };
  }

  public class projectFile {
    public string guid;
    public string extension;
    public string name;
    public string id { get { return guid + "." + extension; } }
    public string data;
    public int idx { get { return int.Parse(name.Replace("file_", null).Replace(".js", null)); } }
  }

  internal enum methods { get, post, delete }

  public abstract class FileItemLow {
    protected abstract byte[] getSourceContent();
    internal abstract Stream gerDestinationStream();
    internal abstract void writeResult(string data);
    protected abstract string getSourceExtension();

    internal NameValue toNameValue(ref int cnt) { init(); return new NameValue() { name = "file_" + cnt.ToString(), value = md5 }; }
    internal ByteArrayContent toContent(ref int cnt) {
      init();
      var res = new ByteArrayContent(content);
      res.Headers.ContentType = new MediaTypeHeaderValue("text/plain") { CharSet = "utf-8" };
      res.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "file_" + cnt + Path.GetExtension(getSourceExtension()), Name = "file_" + cnt++ };
      return res;
    }

    void init() {
      if (content != null) return;
      content = getSourceContent();
      //hash:
      byte[] hashBytes = MD5.Create().ComputeHash(content);
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < hashBytes.Length; i++) sb.Append(hashBytes[i].ToString("x2"));
      md5 = sb.ToString();
    }
    byte[] content;
    string md5;
  }

  //File for encoding
  public class FileItem : FileItemLow {
    public FileItem(string src, string dest) { Src = src; Dest = dest; }
    protected override byte[] getSourceContent() { return File.ReadAllBytes(Src); }
    internal override Stream gerDestinationStream() { return new FileStream(Dest, FileMode.Create); }
    protected override string getSourceExtension() { return Path.GetExtension(Src); }
    internal override void writeResult(string data) { File.WriteAllText(Dest, data); }
    string Dest; string Src;
  }

  public enum Modes { start, mobile }

  public class Template {
    public Modes mode = Modes.start;
    public bool member_enumeration;
    public bool literal_hooking;
    public bool dead_code;
    public bool string_splitting;
    public bool function_reorder;
    public bool function_outlining;
    public bool dot_notation_elimination;
    public bool rename_local;
    public bool rename_all;
    public bool whitespace;
    public bool literal_duplicates;
    public bool constant_folding;
    public bool dead_code_elimination;
    public bool dictionary_compression;
    public bool self_defending;

    internal string domain_lock;
    internal string expiration_date;
    internal string timestamp;
    internal string signature;
    internal string access_key;
    internal IEnumerable<NameValue> toNameValues() {
      foreach (var fld in Template.Fields()) {
        string val = null;
        if (fld.FieldType == typeof(bool))
          val = (bool)fld.GetValue(this) ? "%DEFAULT%" : null;
        else if (fld.FieldType == typeof(string))
          val = (string)fld.GetValue(this);
        else if (fld.FieldType == typeof(Modes))
          val = ((Modes)fld.GetValue(this)).ToString();
        if (val != null) yield return new NameValue() { name = fld.Name, value = val };
      }
    }
    internal static FieldInfo[] Fields() { return typeof(Template).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); }
  }

  internal class Par : Template {
    //POST constructor
    internal Par(FileItemLow[] _files, Template templ, string _domain_lock, DateTime? _expiration_date) {
      if (templ == null) templ = Lib.defaultTemplate;
      foreach (var fld in Template.Fields()) fld.SetValue(this, fld.GetValue(templ)); //copy params from template
      domain_lock = _domain_lock;
      expiration_date = _expiration_date == null ? null : ((DateTime)_expiration_date).ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
      files = _files;
      finishPar(methods.post, "/code.json");
    }
    //GET or DELETE constructor
    internal Par(methods _method, string _resourcePath) {
      finishPar(_method, _resourcePath);
    }
    //Run request
    internal Stream runHttpRequest() {
      using (var client = new HttpClient() { Timeout = new TimeSpan(0, 1, 0) }) {
        using (HttpRequestMessage request = new HttpRequestMessage()) {
          request.Headers.ExpectContinue = false;
          request.RequestUri = new Uri(url);
          switch (method) {
            case methods.post:
              request.Method = HttpMethod.Post;
              var content = new MultipartFormDataContent();
              foreach (var kv in toNameValues()) content.Add(new StringContent(kv.value), kv.name);
              int cnt = 0;
              foreach (var f in files) content.Add(f.toContent(ref cnt));
              request.Content = content;
              break;
            case methods.get:
              request.Method = HttpMethod.Get;
              break;
            case methods.delete:
              request.Method = HttpMethod.Delete;
              break;
          }
          //execute request
          Task<HttpResponseMessage> httpRequest = client.SendAsync(request, HttpCompletionOption.ResponseContentRead, CancellationToken.None);
          HttpResponseMessage httpResponse = httpRequest.Result;
          if (!httpResponse.IsSuccessStatusCode) {
            string str = null;
            try {
              var errResp = httpResponse.Content.ReadAsStreamAsync();
              using (StreamReader rdr = new StreamReader(errResp.Result)) {
                str = rdr.ReadToEnd();
              }
            } catch { }

            throw new Exception("Error code: " + httpResponse.StatusCode + "ResourcePath: " + resourcePath + ", Message: " + str ?? httpResponse.ReasonPhrase);
          }
          var resp = httpResponse.Content.ReadAsStreamAsync();
          return resp.Result;
        }
      }
    }

    methods method;
    string resourcePath;
    FileItemLow[] files;
    string url; //POST, GET or DELETE url
    //set signature and basic URL
    void finishPar(methods _method, string _resourcePath) {
      method = _method; resourcePath = _resourcePath;
      timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ssZ");
      access_key = Lib.accessKey;
      signature = generateHMACSignature();
      string apiUrl = "http" + (Lib.apiPort == 443 ? "s" : "") + "://" + Lib.apiHost + (Lib.apiPort != 80 ? (":" + Lib.apiPort) : "") + (Lib.apiVersion == 0 ? null : "/v" + Lib.apiVersion);
      url = apiUrl + resourcePath;
      if (method != methods.post) url += "?" + urlQueryString(toNameValues());
    }
    string generateHMACSignature() {
      //parametry a files
      int cnt = 0;
      var pars = toNameValues();
      if (files != null) pars = pars.Concat(files.Select(f => f.toNameValue(ref cnt)));
      //data for HMAC
      string data = method.ToString().ToUpper() + ";" + Lib.apiHost.ToLower() + ";" + resourcePath + ";" + urlQueryString(pars.OrderBy(nv => nv.name));
      //make HMAC
      HMACSHA256 myhmacsha256 = new HMACSHA256(UTF8Encoding.UTF8.GetBytes(Lib.secretKey));
      byte[] hashValue = myhmacsha256.ComputeHash(UTF8Encoding.UTF8.GetBytes(data));
      return Convert.ToBase64String(hashValue);
    }
    static string urlQueryString(IEnumerable<NameValue> pars) {
      StringBuilder sb = new StringBuilder();
      foreach (var kv in pars) { sb.Append(urlEncode(kv.name)); sb.Append('='); sb.Append(urlEncode(kv.value)); sb.Append('&'); }
      sb.Length = sb.Length - 1;
      return sb.ToString();
    }
    static string urlEncode(String data) { return Uri.EscapeDataString(data).Replace("%7E", "~").Replace("+", "%20"); }
  }

  internal struct NameValue { internal string name; internal string value; }
}