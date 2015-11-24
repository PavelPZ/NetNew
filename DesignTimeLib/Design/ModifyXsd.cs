using CourseModel;
using LMComLib;
using LMNetLib;
//http://usejsdoc.org/index.html
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SchemaDefinition {
#pragma warning disable 1591
  public static class ModifyXsd {

    //static MemberInfo[] attrHTMLExtend = StaticReflection.GetMembers<tag>(t => t.parentProp, t => t.styleSheet).ToArray();

    //static XNamespace xs = "http://www.w3.org/2001/XMLSchema";
    //public static void modify() {
    //  //modify XSD
    //  var xsdFn = @"d:\LMCom\rew\SchemaDefinition\schema0.xsd";
    //  XElement xsd = XElement.Load(xsdFn);
    //  foreach (var el in xsd.Descendants(xs + "extension")) {
    //    if (el.Attribute("base") == null || el.Parent.Name.LocalName != "complexContent" || el.Parent.Attribute("mixed") == null) continue;
    //    el.Parent.Attribute("mixed").Value = "true";
    //  }
    //  xsd.Save(xsdFn);
    //  LMNetLib.XmlUtils.ObjectToFile(@"d:\LMCom\rew\Web4\Author\tagsMeta.xml", doc.export(@"d:\lmcom\rew\schemadefinition\schemadefinition.xml", null));
    //}
    //static XElement findByName(XElement root, string name) {
    //  foreach (var el in root.Elements()) {
    //    var nm = el.Attribute("name"); if (nm == null) continue;
    //    if (nm.Value == name) return el;
    //  }
    //  throw new Exception(string.Format("Cannot find element, name=", name));
    //}

    const string schemaFn = @"d:\LMCom\rew\Web4\author\CourseModelSchema.xsd";

    public static void genSchema() {
      //merge DOC to tagsMeta
      doc.export();
      //modify xhtml5.xsd
      XmlSchema schema;
      using (var str = File.OpenRead(@"d:\LMCom\rew\Web4\Author\xhtml5.xsd")) schema = XmlSchema.Read(str, null);
      var meta = Lib.courseModelJsonMLMeta;
      XmlSchemaAttributeGroup attrGrp;
      var allTypes = meta.types.Values.Where(t => (t.st & (tgSt.xsdIgnore | tgSt.obsolete)) == 0).ToArray();
      Func<jsClassMeta, IEnumerable<jsPropMeta>> myXsdProps = tp => tp.props.Values.Where(p => (p.st & (tgSt.xsdIgnore | tgSt.xmlIgnore)) == 0);
      Func<jsClassMeta, IEnumerable<jsPropMeta>> allXsdProps = tp => tp.parents(true).Where(t => (tp.st & (tgSt.xsdIgnoreTagAttrs /*| tgSt.xsdHtmlEl*/))!=0 ? t.name!="tag" : true).SelectMany(t => myXsdProps(t));
      //Func<jsClassMeta, IEnumerable<jsPropMeta>> allXsdPropsNoTag = tp => tp.parents(true).Where(t => t.name != "tag").SelectMany(t => myXsdProps(t));
      XmlSchemaComplexType complex; XmlSchemaSimpleContentExtension simpleExt;
      XmlSchemaChoice choice; XmlSchemaGroupBase grpBase;

      //*************** modifikace HTML 5 atributu a childs
      //definice parent-prop a style-sheet atributu
      schema.Items.Add(attrGrp = new XmlSchemaAttributeGroup { Name = "LM-spec-attrs" });
      //attrGrp.Attributes.Add(new XmlSchemaAttribute { Name = "parent-prop", SchemaTypeName = new XmlQualifiedName("parentProps"), DefaultValue = "no" });
      attrGrp.Attributes.Add(new XmlSchemaAttribute { Name = "style-sheet", SchemaTypeName = stringType });
      attrGrp.Attributes.Add(new XmlSchemaAttribute { Name = "data-bind", SchemaTypeName = stringType });

      //definice grupy pro all LM elements
      schema.Items.Add(new XmlSchemaGroup { Name = "LM-all-elements", Particle = choice = new XmlSchemaChoice() });
      foreach (var typ in allTypes.Where(t => (t.st & tgSt.xsdNoGlobal)==0)) choice.Items.Add(new XmlSchemaElement { RefName = new XmlQualifiedName(typ.tagName) });

      //modifikace commonPhrasingElements groups
      var commGrp = schema.Items.OfType<XmlSchemaGroup>().First(e => e.Name == "commonPhrasingElements");
      ((XmlSchemaChoice)commGrp.Particle).Items.Add(new XmlSchemaGroupRef { RefName = new XmlQualifiedName("LM-all-elements") });

      //modifikace coreAttributeGroupNodir o tag attribute group
      attrGrp = schema.Items.OfType<XmlSchemaAttributeGroup>().First(e => e.Name == "coreAttributeGroupNodir");
      attrGrp.Attributes.Add(new XmlSchemaAttributeGroupRef { RefName = new XmlQualifiedName("LM-spec-attrs") });

      //*************** dalse modifikace HTML 5
      //modifikace id atributu z token na xs:ID
      var idType = schema.Items.OfType<XmlSchemaSimpleType>().First(e => e.Name == "id");
      ((XmlSchemaSimpleTypeRestriction)idType.Content).BaseTypeName = new XmlQualifiedName("xs:ID");

      //modifikace body
      var body = schema.Items.OfType<XmlSchemaElement>().First(e => e.Name == "body");
      var ext = (XmlSchemaComplexContentExtension)((XmlSchemaComplexContent)((XmlSchemaComplexType)body.SchemaType).ContentModel).Content;
      foreach (var prop in myXsdProps(meta.types["body"])) ext.Attributes.Add(generateProp(prop));

      //modifikace li
      schema.Items.Add(new XmlSchemaElement { Name="li", SchemaTypeName = new XmlQualifiedName("li") });

      //*************** LM elements
      //RegEx "and-*-exchangeable" pro jmeno eval grupy
      XmlSchemaSimpleTypeRestriction restr;
      schema.Items.Add(new XmlSchemaSimpleType {
        Name = evalControl.xsdType_eval_group,
        Content = restr = new XmlSchemaSimpleTypeRestriction { BaseTypeName = new XmlQualifiedName("xs:string") }
      });
      restr.Facets.Add(new XmlSchemaPatternFacet { Value = evalControl.xsdType_eval_group_regex });

      //RegEx sequence zvukovuch vet
      schema.Items.Add(new XmlSchemaSimpleType {
        Name = _sndFile.mediaTag_sequence,
        Content = restr = new XmlSchemaSimpleTypeRestriction { BaseTypeName = new XmlQualifiedName("xs:string") }
      });
      restr.Facets.Add(new XmlSchemaPatternFacet { Value = _sndFile.mediaTag_sequence_regex });

      //RegEx format videa
      schema.Items.Add(new XmlSchemaSimpleType {
        Name = urlTag.mediaTag_format,
        Content = restr = new XmlSchemaSimpleTypeRestriction { BaseTypeName = new XmlQualifiedName("xs:string") }
      });
      restr.Facets.Add(new XmlSchemaPatternFacet { Value = urlTag.mediaTag_format_regex });

      //RegEx identifikace zvukove vety v dialogu
      schema.Items.Add(new XmlSchemaSimpleType {
        Name = phraseReplace.sndReplace_sndDialogIdx,
        Content = restr = new XmlSchemaSimpleTypeRestriction { BaseTypeName = new XmlQualifiedName("xs:string") }
      });
      restr.Facets.Add(new XmlSchemaPatternFacet { Value = phraseReplace.sndReplace_sndDialogIdx_regex });

      //lm typy
      foreach (var typ in allTypes) {
        if (typ.name == "sndDialog") {
          if (typ == null) return;
        }
        schema.Items.Add(new XmlSchemaElement { Name = typ.tagName, SchemaTypeName = new XmlQualifiedName(typ.tagName + "-type") });
        //element type
        if ((typ.st & tgSt.xsdHtmlEl) != 0) { //odvozeno z HTML
          var isMixed = (typ.st & tgSt.xsdNoMixed) == 0; //mixed?
          schema.Items.Add(complex = new XmlSchemaComplexType {
            Name = typ.tagName + "-type",
            IsMixed = isMixed,
            Particle = grpBase = new XmlSchemaSequence { MinOccurs = 0, MaxOccursString = "unbounded" }
          });
          grpBase.Items.Add(new XmlSchemaGroupRef { RefName = new XmlQualifiedName("flowContent") });
          //foreach (var p in allXsdPropsNoTag(typ)) complex.Attributes.Add(generateProp(p));
          foreach (var p in allXsdProps(typ)) complex.Attributes.Add(generateProp(p));
          continue;
        }
        if ((typ.st & (tgSt.xsdString | tgSt.cdata)) != 0) { //pouze string content (nebo cdata)
          schema.Items.Add(complex = new XmlSchemaComplexType {
            Name = typ.tagName + "-type",
            ContentModel = new XmlSchemaSimpleContent { Content = simpleExt = new XmlSchemaSimpleContentExtension { BaseTypeName = new XmlQualifiedName("xs:string") } }
          });
          foreach (var p in allXsdProps(typ)) simpleExt.Attributes.Add(generateProp(p));
          continue;
        }
        if (!string.IsNullOrEmpty(typ.xsdChildElements)) { //explixitne nadefinovane child elements
          var childs = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>("{" + typ.xsdChildElements + "}");
          //var obj = childs as JObject;
          //var prop = obj.Properties().FirstOrDefault();
          //var arr = prop.Value as JArray;
          schema.Items.Add(complex = new XmlSchemaComplexType {
            Name = typ.tagName + "-type",
            IsMixed = (typ.st & tgSt.xsdMixed)!=0,
            Particle = genChilds(Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>("{" + typ.xsdChildElements + "}")) as XmlSchemaParticle//choice = new XmlSchemaChoice { MaxOccursString = "unbounded", MinOccurs=0 }
          });
          //foreach (var t in typ.xsdChildElements.Split('|')) choice.Items.Add(new XmlSchemaElement { RefName = new XmlQualifiedName(t) });
          //foreach (var t in typ.xsdChildElements.Split('|')) choice.Items.Add(new XmlSchemaElement { Name = t });
          foreach (var p in allXsdProps(typ)) complex.Attributes.Add(generateProp(p));
          continue;
        }
        schema.Items.Add(complex = new XmlSchemaComplexType { Name = typ.tagName + "-type" });
        foreach (var p in allXsdProps(typ)) complex.Attributes.Add(generateProp(p));
      }

      //lm enums
      foreach (var en in getAll.Enums()) generateEnum(schema, en);

      //schema.Compile((s, a) => { });
      if (File.Exists(schemaFn)) File.Delete(schemaFn);
      using (var str = File.OpenWrite(schemaFn)) schema.Write(str);
    }

    static XmlSchemaObject genChilds(dynamic data) {
      var obj = data as JObject;
      if (obj != null) {
        var prop = obj.Properties().First();
        XmlSchemaGroupBase res; var nm = prop.Name;
        switch (nm[0]) { 
          case 'c': res = new XmlSchemaChoice(); break;
          case 's': res = new XmlSchemaSequence(); break;
          default: throw new NotImplementedException();
        }
        if (nm.Length == 3) {
          if (char.IsDigit(nm[1])) res.MinOccurs = int.Parse(Convert.ToString(nm[1]));
          if (char.IsDigit(nm[2])) res.MaxOccurs = int.Parse(Convert.ToString(nm[2])); else if (nm[2] == '_') res.MaxOccursString = "unbounded"; 
        }
        var arr = prop.Value as JArray;
        foreach (var a in arr) res.Items.Add(genChilds(a));
        return res;
      }
      var str = data as JValue;
      if (str != null) {
        var name = (string)str.Value;
        if (name[0]=='@')
          return new XmlSchemaGroupRef { RefName = new XmlQualifiedName(name.Substring(1)) };
        else 
          return new XmlSchemaElement { RefName = new XmlQualifiedName(name) };
      }
      throw new Exception();
    }

    //const string xsd = "http://www.w3.org/2001/XMLSchema";
    static XmlQualifiedName stringType = new XmlQualifiedName("string");
    //static HashSet<string> ignoreTags = new HashSet<string>();

    static void generateEnum(XmlSchema schema, Type en) {
      XmlSchemaSimpleTypeRestriction restr = new XmlSchemaSimpleTypeRestriction { BaseTypeName = stringType };
      foreach (var val in Enum.GetValues(en).Cast<object>())
        restr.Facets.Add(new XmlSchemaEnumerationFacet { Value = enumName(en, val.ToString()) });
      schema.Items.Add(new XmlSchemaSimpleType { Name = en.Name, Content = restr });
    }

    static string enumName(Type en, string name) {
      FieldInfo info = en.GetField(name);
      var attrs = info.GetCustomAttributes(typeof(XmlEnumAttribute), false);
      return attrs.Length > 0 ? ((XmlEnumAttribute)attrs[0]).Name : name;
    }

    static XmlSchemaAttribute generateProp(jsPropMeta prop) {
      if (!string.IsNullOrEmpty(prop.enumType)) {
        return new XmlSchemaAttribute { Name = prop.tagName, SchemaTypeName = new XmlQualifiedName(prop.propType.Name), DefaultValue = enumName(prop.propType, Enum.GetValues(prop.propType).Cast<object>().First().ToString()) };
      }
      string xsdType = prop.attr != null ? prop.attr.xsdType : null; string defVal = null;
      if (string.IsNullOrEmpty(xsdType)) {
        switch (prop.propType.Name) {
          case "String": xsdType = "xs:string"; break;
          case "Boolean": xsdType = "xs:boolean"; defVal = "false"; break;
          case "int":
          case "Int32":
          case "Int64":
          case "long":
            xsdType = "xs:int"; defVal = "0"; break;
        }
      }
      return new XmlSchemaAttribute { Name = prop.tagName, SchemaTypeName = new XmlQualifiedName(xsdType), DefaultValue = defVal, Use = (prop.st & tgSt.xsdRequiredAttr) != 0 ? XmlSchemaUse.Required : XmlSchemaUse.None };
    }

  }
#pragma warning restore 1591
}
