// $Header: /cvsroot/LMCom/lmcomlib/utils/XmlUtils.cs,v 1.11 2012/11/15 23:03:44 pavel Exp $
using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using LMComLib;
using System.Xml.Linq;

namespace LMNetLib {
  public static class XmlUtils {

    public static XElement toXml(object obj) {
      using (var ms = new MemoryStream()) {
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.OmitXmlDeclaration = true;
        using (XmlWriter writer = XmlWriter.Create(ms, settings)) { XmlSerializer sr = new XmlSerializer(obj.GetType()); sr.Serialize(writer, obj); }
        ms.Seek(0, SeekOrigin.Begin);
        var xml = XElement.Load(ms);
        xml.Attribute(XNamespace.Xmlns + "xsi").Remove(); xml.Attribute(XNamespace.Xmlns + "xsd").Remove();
        return xml;
      }
    }

    static public void ObjectToFile(string fileName, object xmlObject) {
      XmlSerializer serializer = new XmlSerializer(xmlObject.GetType());
      using (TextWriter writer = new StreamWriter(fileName))
        serializer.Serialize(writer, xmlObject);
    }

    static public void ObjectToFile<T>(string fileName, object xmlObject) {
      XmlSerializer serializer = new XmlSerializer(typeof(T));
      using (TextWriter writer = new StreamWriter(fileName))
        serializer.Serialize(writer, xmlObject);
    }

    static public void ObjectToFile(string fileName, object xmlObject, System.Type type) {
      XmlSerializer serializer = new XmlSerializer(type);
      TextWriter writer = new StreamWriter(fileName);
      try {
        serializer.Serialize(writer, xmlObject);
      } finally {
        writer.Close();
      }
    }

    static public object FileToObject(string fileName, System.Type type) {
      XmlSerializer serializer = new XmlSerializer(type);
      TextReader reader = new StreamReader(fileName);
      object xmlObject;
      try {
        try {
          xmlObject = serializer.Deserialize(reader);
        } finally {
          reader.Close();
        }
      } catch (Exception exp) {
        throw new Exception(fileName, exp);
      }
      return xmlObject;
    }

    static public T FileToObject<T>(string fileName) {
      try {
        XmlSerializer serializer = new XmlSerializer(typeof(T));
        using (TextReader reader = new StreamReader(fileName))
          return (T)serializer.Deserialize(reader);
      } catch (Exception exp) {
        throw new Exception(fileName, exp);
      }
    }

    static public T StreamToObject<T>(Stream str) {
      XmlSerializer serializer = new XmlSerializer(typeof(T));
      using (StreamReader reader = new StreamReader(str))
        return (T)serializer.Deserialize(reader);
    }

    static public object StreamToObject(Stream str, Type t) {
      XmlSerializer serializer = new XmlSerializer(t);
      using (StreamReader reader = new StreamReader(str))
        return serializer.Deserialize(reader);
    }

    static public void XmlToFile(XmlDocument document, string fileName) {
      document.Save(fileName);
    }

    static public byte[] XmlToBytes(XmlDocument document) {
      MemoryStream stream = new MemoryStream();
      document.Save(stream);
      return stream.ToArray();
    }

    static public XmlDocument FileToXml(string fileName) {
      XmlDocument document = new XmlDocument();
      document.Load(fileName);
      return document;
    }

    static public XmlDocument BytesToXml(byte[] bytes) {
      MemoryStream stream = new MemoryStream(bytes);
      XmlDocument document = new XmlDocument();
      document.Load(stream);
      return document;
    }

    static public string ObjectToString(object xmlObject) {
      return xmlObject==null ? null : ObjectToString(xmlObject, xmlObject.GetType());
    }

    static public string ObjectToString<T>(object xmlObject) {
      return ObjectToString(xmlObject, typeof(T));
    }

    static public string ObjectToString(object xmlObject, Type type) {
      using (StringWriter writer = new StringWriter() ) {
        new XmlSerializer(type).Serialize(writer, xmlObject);
        return writer.ToString();
      }
    }

    static public object StringToObject(string s, Type type) {
      if (String.IsNullOrEmpty(s)) return null;
      using (StringReader reader = new StringReader(s))
        return new XmlSerializer(type).Deserialize(reader);
    }

    static public T StringToObject<T>(string s) where T:class {
      if (String.IsNullOrEmpty(s)) return null;
      try {
        using (StringReader reader = new StringReader(s))
          return (T)new XmlSerializer(typeof(T)).Deserialize(reader);
      } catch (Exception exp) {
        throw new Exception(exp.Message + "   " + s, exp);
      }
    }

    static public void ObjectToStream(object xmlObject, Stream str) {
      //using (var wr = XmlWriter.Create(str, new XmlWriterSettings { Indent = false })) {
      XmlSerializer serializer = new XmlSerializer(xmlObject.GetType());
      serializer.Serialize(str, xmlObject);
    }

    static public object BytesToObject(byte[] buffer, System.Type type) {
      XmlSerializer serializer = new XmlSerializer(type);
      MemoryStream memory = new MemoryStream(buffer);
      object xmlObject;
      try {
        xmlObject = serializer.Deserialize(memory);
      } finally {
        memory.Close();
      }
      return xmlObject;
    }

    static public byte[] ObjectToBytes(object xmlObject) {
      return ObjectToBytes(xmlObject, xmlObject.GetType());
    }

    static public byte[] ObjectToBytes<T>(object xmlObject) {
      return ObjectToBytes(xmlObject, typeof(T));
    }

    static byte[] ObjectToBytes(object xmlObject, Type type) {
      XmlSerializer serializer = new XmlSerializer(type, "");
      using (MemoryStream memory = new MemoryStream()) {
        serializer.Serialize(memory, xmlObject);
        return memory.ToArray();
      }
    }

    static public T BytesToObject<T>(byte[] buffer) {
      XmlSerializer serializer = new XmlSerializer(typeof(T));
      using (MemoryStream memory = new MemoryStream(buffer))
        return (T)serializer.Deserialize(memory);
    }

    public static T DecompressAndDeserialize<T>(Stream input) {
      using (Stream s2 = new InflaterInputStream(input)) return StreamToObject<T>(s2);
    }
    public static T DecompressAndDeserialize<T>(byte[] data) {
      using (MemoryStream ms = new MemoryStream(data)) using (Stream s2 = new InflaterInputStream(ms)) return StreamToObject<T>(s2);
    }

    public static void SerializeAndCompress(object inst, Stream str) {
      using (Stream s = ZipWrapper.DecompressStream(str)) ObjectToStream(inst, s);
    }

    public static byte[] SerializeAndCompress(object inst) {
      MemoryStream ms = new MemoryStream(); SerializeAndCompress(inst, ms);
      return ms.ToArray();
    }


    /*static public byte[] ObjectToUTF8Bytes(object xmlObject) {
      return Encoding.UTF8.GetBytes(ObjectToString(xmlObject));
    }

    static public T UTF8BytesToObject<T>(byte[] buffer) where T : class {
      return StringToObject<T>(Encoding.UTF8.GetString(buffer, 0, buffer.Length));
    }*/

  }
}
