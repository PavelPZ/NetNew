using LMComLib;
using LMNetLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
#pragma warning disable 1591


//all html elements etc.: http://www.w3.org/TR/html4/index/elements.html
//all comment tags: http://msdn.microsoft.com/en-us/library/5ast78ax.aspx
namespace CourseModel {

  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
  //[Doc(ignore = true)]
  public class docNamed : tag {
    [XmlAttribute]
    public string name;

    //[tgAt(0)]
    //[XmlIgnore, Newtonsoft.Json.JsonIgnore, LMComLib.JsonGenOnly]
    //[XmlIgnore, Newtonsoft.Json.JsonIgnore]
    //public docExample[] examples;
    //[tgAt(0, childPropTypes = "summary")]
    //[XmlIgnore, JsonIgnore, JsonGenOnly]
    [XmlAttribute]
    public string summary;

    [XmlAttribute, JsonIgnore, LMComLib.JsonGenOnly]
    public string cdata;

    //protected override bool isTagProps(tag t) {
    //  if (t is summary) summary = (summary)t; else return false;
    //  return true;
    //}
    //protected override IEnumerable<tag> getTagProps() { if (summary != null) yield return summary; }


    //protected override void setTagProps(List<tag> t) { summary = (summary)t.FirstOrDefault(tt => tt.parentProp == parentProps.summary); } 
    //protected override IEnumerable<tag> getTagProps() { if (summary != null) { summary.parentProp = parentProps.summary; yield return summary; } } //if (examples != null) foreach (var pr in examples) { pr.parentProp = parentProps.example_s; yield return pr; } }
  }

  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore | tgSt.cdata)]
  [XmlType(TypeName = "doc-type")]
  //[Doc(ignore = true)]
  public class docType : docNamed {
    [XmlAttribute(AttributeName = "is-html"), DefaultValue(false)]
    public bool isHtml;

    [XmlAttribute(AttributeName = "is-ign"), DefaultValue(false)]
    public bool isIgn;

    [tgAt(tgSt.isArray, xsdType = "tokens")]
    [XmlAttribute(AttributeName = "descendants-and-self")]
    public string[] descendantsAndSelf;

    [tgAt(tgSt.isArray, xsdType = "tokens")]
    [XmlAttribute(AttributeName = "my-props")]
    public string[] myProps;

    [XmlAttribute]
    public string xref;
  }

  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore | tgSt.cdata)]
  [XmlType(TypeName = "doc-enum")]
  //[Doc(ignore = true)]
  public class docEnum : docNamed {
    [XmlAttribute]
    public string xref;

    [tgAt(tgSt.isArray, childPropTypes = "doc-enum-item")]
    [XmlIgnore, JsonIgnore, JsonGenOnly]
    public List<docEnumItem> enums = new List<docEnumItem>();

    protected override bool isTagProps(tag t) {
      if (t is docEnumItem) enums.Add((docEnumItem)t); else return false;
      return true;
    }
    public override IEnumerable<tag> getTagProps() { return enums; }

    //protected override void setTagProps(List<tag> t) { base.setTagProps(t); enums = t.Where(tt => tt.parentProp == parentProps.enum_s).Cast<docEnumItem>().ToArray(); }
    //protected override IEnumerable<tag> getTagProps() { foreach (var it in base.getTagProps()) yield return it; if (enums != null) foreach (var pr in enums) { pr.parentProp = parentProps.enum_s; yield return pr; } }
  }

  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore | tgSt.cdata)]
  [XmlType(TypeName = "doc-enum-item")]
  //[Doc(ignore = true)]
  public class docEnumItem : docNamed {
    [XmlAttribute]
    public string xref;
  }

  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore | tgSt.cdata)]
  [XmlType(TypeName = "doc-prop")]
  //[Doc(ignore = true)]
  public class docProp : docNamed {
    [XmlAttribute(AttributeName = "owner-type")]
    public string ownerType; //id typu, definujiciho property

    [XmlAttribute(AttributeName = "data-type")]
    public string dataType; //zakodovany typ property, napr. @enum:???

    //[XmlAttribute(AttributeName = "xref-valued"), DefaultValue(false)]
    //public bool xrefValued; //u property se v xref sleduje i hodnota, napr. enum properties, class apod.

    //[XmlAttribute(AttributeName = "xref-ignore"), DefaultValue(false)]
    //public bool xrefIgnore; //property se ignoruje v xref (napr. id)

    [XmlAttribute]
    public string xref; //xref pro property

    [XmlAttribute(AttributeName = "is-html"), DefaultValue(false)]
    public bool isHtml; //html property
    //*** runtime
    public string fullName() { return ownerType + "." + name; }
  }

  //[tgAt(tgSt.docIgnore | /*tgSt.jsNo |*/ tgSt.obsolete)]
  ////[Doc(ignore = true)]
  //[TagAttribute(JSStatus = JSStatus.no)]
  //public class summary : tag {
  //}

  [XmlType(TypeName = "doc-descr")]
  [tgAt(tgSt.docIgnore | tgSt.xsdHtmlEl | tgSt.xsdNoGlobal)]
  [TagAttribute(JSStatus = JSStatus.no, CSControl = false)]
  public partial class docDescr : tag {
  }

  [tgAt(tgSt.docIgnore | tgSt.jsCtrl | tgSt.csControl, xsdChildElements = "s:[{c01: ['header-prop']},{c01: ['doc-descr']},{c0_: ['@flowContent']}]")]
  [XmlType(TypeName = "doc-example")]
  [TagAttribute(JSStatus = JSStatus.ctrl, CSControl = true)]
  //[Doc(ignore = true)]
  public partial class docExample : tag {

    [XmlAttribute]
    public bool todo;

    [XmlAttribute(AttributeName = "code-listing")]
    public string codeListing;

    [XmlAttribute(AttributeName = "code-post-listing")]
    public string codePostListing;

    [tgAt(0, childPropTypes = "header-prop")]
    [XmlIgnore, JsonIgnore, JsonGenOnly]
    public headerProp header;

    [tgAt(0, childPropTypes = "doc-descr")]
    [XmlIgnore, JsonIgnore, JsonGenOnly]
    public docDescr descr;

    [tgAt(0, childPropTypes = "eval-btn")]
    [XmlIgnore, JsonIgnore, JsonGenOnly]
    public evalButton evalBtn;

    protected override bool isTagProps(tag t) {
      if (t is headerProp) header = (headerProp)t;
      else if (t is evalButton) evalBtn = (evalButton)t;
      else if (t is docDescr) descr = (docDescr)t;
      else return false;
      return true;
    }
    public override IEnumerable<tag> getTagProps() { if (header != null) yield return header; if (descr != null) yield return descr; if (evalBtn != null) yield return evalBtn; }
    //protected override IEnumerable<tag> getTagProps() {
    //  if (header != null) { header.parentProp = parentProps.header; yield return header; }
    //  if (descr != null) { descr.parentProp = parentProps.descr; yield return descr; }
    //  if (evalBtn != null) { evalBtn.parentProp = parentProps.evalBtn; yield return evalBtn; }
    //}
    static void extractListing(XElement ex, string attrName) {
      var content = ex.Elements().Where(e => e.Name.LocalName != "header-prop" && e.Name.LocalName != "doc-descr").Select(el => new XElement(el)).ToArray();
      if (content.Length < 1) return;
      //var str0 = new XElement("div", new XAttribute("class", "source-code"), new XElement("pre", new XAttribute("class", "prettyprint lang-html"), content.Select(c => c.ToString()))).ToString();
      var str = SecurityElement.Escape (content.Select(c => c.ToString()).Aggregate((r, i) => r + i));
      ex.SetAttributeValue(attrName, str); //nd.ToString());
    }
    public static void beforeFromElement(XElement ex) {
      extractListing(ex, "code-listing");
    }
    public static void beforeXmlToJson(XElement ex) {
      extractListing(ex, "code-post-listing");
    }
  }

  //[tgAt(tgSt.docIgnore | /*tgSt.jsNo |*/ tgSt.obsolete)]
  ////[Doc(ignore = true)]
  //[TagAttribute(JSStatus = JSStatus.no)]
  //public class remarks : tag {
  //}

  [tgAt(tgSt.docIgnore | tgSt.xsdIgnore)]
  [XmlType(TypeName = "doc-tags-meta")]
  //[Doc(ignore = true)]
  public class docTagsMeta : tag {

    [tgAt(tgSt.isArray, childPropTypes = "doc-type")]
    [XmlIgnore, Newtonsoft.Json.JsonIgnore, LMComLib.JsonGenOnly]
    public List<docType> types = new List<docType>();

    [tgAt(tgSt.isArray, childPropTypes = "doc-prop")]
    [XmlIgnore, Newtonsoft.Json.JsonIgnore, LMComLib.JsonGenOnly]
    public List<docProp> props = new List<docProp>();

    [tgAt(tgSt.isArray, childPropTypes = "doc-enum")]
    [XmlIgnore, Newtonsoft.Json.JsonIgnore, LMComLib.JsonGenOnly]
    public List<docEnum> enums = new List<docEnum>();


    protected override bool isTagProps(tag t) {
      if (t is docType) types.Add((docType)t);
      else if (t is docProp) props.Add((docProp)t);
      else if (t is docEnum) enums.Add((docEnum)t);
      else return false;
      return true;
    }
    public override IEnumerable<tag> getTagProps() { return types.OfType<tag>().Concat(props).Concat(enums); }// if (sndPage != null) yield return sndPage; }

    //persistence
    //protected override void setTagProps(List<tag> t) {
    //  enum_s = t.Where(tt => tt.parentProp == parentProps.enum_s).Cast<docEnum>().ToArray();
    //  prop_s = t.Where(tt => tt.parentProp == parentProps.prop_s).Cast<docProp>().ToArray();
    //  type_s = t.Where(tt => tt.parentProp == parentProps.type_s).Cast<docType>().ToArray();
    //}
    //protected override IEnumerable<tag> getTagProps() {
    //  if (type_s != null) foreach (var pr in type_s) { pr.parentProp = parentProps.type_s; yield return pr; }
    //  if (prop_s != null) foreach (var pr in prop_s) { pr.parentProp = parentProps.prop_s; yield return pr; }
    //  if (enum_s != null) foreach (var pr in enum_s) { pr.parentProp = parentProps.enum_s; yield return pr; }
    //}

  }

}
#pragma warning restore 1591

