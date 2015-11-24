//http://msdn2.microsoft.com/en-us/library/aa140066(office.10).aspx
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Reflection;

//using LMComLib.Admin;
using LMNetLib;
using System.Xml.Linq;

namespace LMComLib {

  public enum LookupType {
    no,
    TaskType,
    Domains,
    Product,
    ProductNew,
    ProductLicence,
    OrderStatus,
    BillingMethods,
    ShippingMethods,
    Licencors,
    LicencorsEx,
    EventCategory,
    LMApps,
  }

  //public static class Lookups {
  //  public static string GetLoopupValue(LookupType type, int val) {
  //    switch (type) {
  //      case LookupType.Domains: return EnumDescrAttribute.getDescr(typeof(Domains), val);
  //      case LookupType.TaskType: return EnumDescrAttribute.getDescr(typeof(TaskType), val);
  //      case LookupType.Product: return ((LMComLib.Cms.Product)CacheItems.GetTemplate(val)).Title;
  //      case LookupType.ProductNew: return ProductCatalogueItems.Instance.getEx(val).ShortTitle;
  //      case LookupType.ProductLicence:
  //        ProductLicence it = ProductCatalogue.getFirstLicenceFromAllCommerce(val/*, Langs.cs_cz*/);
  //        //ProductLicence lic = ProductLicence.fincLicence(val);
  //        return it == null ? "-" : it.ShortTitle;
  //      case LookupType.OrderStatus: {
  //          switch ((OrderStatus)val) {
  //            case OrderStatus.CekaNaPlatbu: return "Čeká na platbu";
  //            case OrderStatus.Hotovo: return "Ukončena";
  //            case OrderStatus.Zrusena: return "Zrušena";
  //            case OrderStatus.Storno: return "Storno";
  //            default: return EnumDescrAttribute.getDescr(typeof(OrderStatus), val);
  //          }
  //        }
  //      case LookupType.BillingMethods: {
  //          switch ((BillingMethods)val) {
  //            case BillingMethods.PayPal: return "Platba PayPal";
  //            default: return EnumDescrAttribute.getDescr(typeof(BillingMethods), val);
  //          }
  //        }
  //      case LookupType.ShippingMethods: return EnumDescrAttribute.getDescr(typeof(ShippingMethods), val);
  //      case LookupType.Licencors: return RoyalityTable.royalityTableItem(val).Title;
  //      case LookupType.LicencorsEx: return RoyalityTable.royalityTableItem(val).Licencor;
  //      case LookupType.EventCategory: return EnumDescrAttribute.getDescr(typeof(EventCategory), val);
  //      case LookupType.LMApps: return EnumDescrAttribute.getDescr(typeof(LMApps), val);
  //      default: throw new Exception();
  //    }
  //  }

  //}
  public enum ExcelFormat {
    Number,
    Currency,
    Date,
    Lookup,
    String,
  }
  public class ExcelColumn {
    public string ColumnName;
    public string Title;
    public ExcelFormat Format;
    public LookupType Lookup;
    public Func<object, object> getData;
    public ExcelColumn(string columnName, ExcelFormat format) : this(columnName, columnName, format) { }
    public ExcelColumn(string columnName, string title, ExcelFormat format) {
      ColumnName = columnName;
      Title = title;
      Format = format;
      Lookup = LookupType.no;
    }
    public ExcelColumn(string columnName, LookupType lookup) : this(columnName, columnName, lookup) { }
    public ExcelColumn(string columnName, string title, LookupType lookup) {
      ColumnName = columnName;
      Title = title;
      Format = ExcelFormat.Lookup;
      Lookup = lookup;
    }

    public void finishCell(XmlWriter wr, object value) {
      if (value == DBNull.Value || value == null) return;
      switch (Format) {
        case ExcelFormat.Date: wr.WriteAttributeString("StyleID", ExcelExport.ns_ss, "fmtDate"); return;
        case ExcelFormat.Currency: wr.WriteAttributeString("StyleID", ExcelExport.ns_ss, "fmtCurrency"); return;
        //case ExcelFormat.Lookup: wr.WriteAttributeString("StyleID", ExcelExport.ns_ss, "fmtRight"); return;
      }
    }
    public void finishData(XmlWriter wr, object value) {
      if (getData != null) value = getData(value);
      if (value == DBNull.Value || value == null) {
        wr.WriteAttributeString("Type", ExcelExport.ns_ss, "String");
        return;
      }
      switch (Format) {
        case ExcelFormat.String: wr.WriteAttributeString("Type", ExcelExport.ns_ss, "String"); return;
        case ExcelFormat.Date: wr.WriteAttributeString("Type", ExcelExport.ns_ss, "DateTime"); return;
        case ExcelFormat.Currency: wr.WriteAttributeString("Type", ExcelExport.ns_ss, "Number"); return;
        case ExcelFormat.Lookup: wr.WriteAttributeString("Type", ExcelExport.ns_ss, "String"); return;
        case ExcelFormat.Number: wr.WriteAttributeString("Type", ExcelExport.ns_ss, "Number"); return;
      }
    }
    public void finishValue(XmlWriter wr, object value) {
      if (getData != null) value = getData(value);
      if (value == DBNull.Value || value == null) { wr.WriteString("-"); return; }
      switch (Format) {
        case ExcelFormat.String: wr.WriteString(value.ToString()); break;
        //case ExcelFormat.Lookup: wr.WriteString(Lookups.GetLoopupValue(Lookup, Convert.ToInt32(value))); break;
        case ExcelFormat.Currency: wr.WriteValue(Convert.ToDecimal(value)); break;
        case ExcelFormat.Number: wr.WriteValue(Convert.ToInt64(value)); break;
        case ExcelFormat.Date: wr.WriteString((Convert.ToDateTime(value)).ToString("yyyy-MM-dd")); break;
      }
    }


  }
  public class ExcelColumns : Dictionary<string, ExcelColumn> {
    public ExcelColumns(params ExcelColumn[] columns)
      : base() {
      foreach (ExcelColumn col in columns)
        Add(col.ColumnName, col);
    }
    public ExcelColumns(Type type)
      : base() {
        foreach (var prop in type.GetProperties()) Add(prop.Name, new ExcelColumn(
          prop.Name,
          prop.PropertyType==typeof(DateTime) ? ExcelFormat.Date : (
          prop.PropertyType == typeof(int) || prop.PropertyType == typeof(long) || prop.PropertyType == typeof(short) || prop.PropertyType == typeof(byte) || prop.PropertyType == typeof(uint) ? ExcelFormat.Number : ExcelFormat.String
          )
        ));
    }
    ExcelColumn defCol = new ExcelColumn(null, ExcelFormat.String);
    public void finishCell(string columnId, XmlWriter wr, object value) {
      ExcelColumn col;
      if (!TryGetValue(columnId, out col)) col = defCol;
      col.finishCell(wr, value);
    }
    public void finishData(string columnId, XmlWriter wr, object value) {
      ExcelColumn col;
      if (!TryGetValue(columnId, out col)) col = defCol;
      col.finishData(wr, value);
    }
    public void finishValue(string columnId, XmlWriter wr, object value) {
      ExcelColumn col;
      if (!TryGetValue(columnId, out col)) col = defCol;
      col.finishValue(wr, value);
    }
  }

  public static class ExcelExport {
    const string ns_def = "urn:schemas-microsoft-com:office:spreadsheet";
    public static XNamespace excelDef = ns_def;
    public const string ns_ss = "urn:schemas-microsoft-com:office:spreadsheet";
    const string ns_x = "urn:schemas-microsoft-com:office:excel";

    public static XDocument Combine(IEnumerable<XElement> parts, bool ignoreHeader) {
      XDocument res = XDocument.Parse(parts.First().ToString());
      //res.Root.Descendants().Select(el => el.Attributes(excelDef + "ExpandedRowCount")).First().Remove();
      res.AddFirst(new XProcessingInstruction("mso-application", "progid=\"Excel.Sheet\""));
      XElement resTable = res.Root.Element(excelDef + "Worksheet").Element(excelDef + "Table");
      XAttribute attr = resTable.Attribute(excelDef + "ExpandedRowCount");
      if (attr != null) attr.Remove();
      foreach (XElement tab in parts.Skip(1))
        resTable.Add(tab.Element(excelDef + "Worksheet").Element(excelDef + "Table").Elements(excelDef + "Row").Skip(ignoreHeader ? 1 : 0));
      return res;
    }

    public static void Start(XmlWriter wr) {
      wr.WriteProcessingInstruction("mso-application", "progid=\"Excel.Sheet\"");
      wr.WriteStartElement("Workbook", ns_def);
      {
        wr.WriteStartElement("Styles", ns_def);
        {
          //fmtHeader
          wr.WriteStartElement("Style", ns_def);
          wr.WriteAttributeString("ID", ns_ss, "fmtHeader");
          {
            wr.WriteStartElement("Alignment", ns_def);
            wr.WriteAttributeString("Horizontal", ns_ss, "Center");
            wr.WriteEndElement();
            wr.WriteStartElement("Font", ns_def);
            wr.WriteAttributeString("Bold", ns_ss, "1");
            wr.WriteEndElement();
          }
          wr.WriteEndElement();
          //fmtCurrency
          wr.WriteStartElement("Style", ns_def);
          wr.WriteAttributeString("ID", ns_ss, "fmtCurrency");
          {
            wr.WriteStartElement("NumberFormat", ns_def);
            wr.WriteAttributeString("Format", ns_ss, "Fixed");
            wr.WriteEndElement();
          }
          wr.WriteEndElement();
          //fmtDate
          wr.WriteStartElement("Style", ns_def);
          wr.WriteAttributeString("ID", ns_ss, "fmtDate");
          {
            wr.WriteStartElement("NumberFormat", ns_def);
            wr.WriteAttributeString("Format", ns_ss, "Short Date");
            wr.WriteEndElement();
          }
          wr.WriteEndElement();
          //fmtRight
          wr.WriteStartElement("Style", ns_def);
          wr.WriteAttributeString("ID", ns_ss, "fmtRight");
          {
            wr.WriteStartElement("Alignment", ns_def);
            wr.WriteAttributeString("Horizontal", ns_ss, "Right");
            wr.WriteEndElement();
          }
          wr.WriteEndElement();
          //fmtSmall
          wr.WriteStartElement("Style", ns_def);
          wr.WriteAttributeString("ID", ns_ss, "fmtSmall");
          {
            wr.WriteStartElement("Font", ns_def);
            wr.WriteAttributeString("Size", ns_ss, "8");
            wr.WriteEndElement();
          }
          wr.WriteEndElement();
        }
        wr.WriteEndElement();
        wr.WriteStartElement("Worksheet", ns_def);
        wr.WriteAttributeString("Name", ns_ss, "Table");
        {
          wr.WriteStartElement("Table", ns_def);
          wr.WriteAttributeString("FullColumns", ns_x, "1");
          wr.WriteAttributeString("FullRows", ns_x, "1");
          {
            //wr.WriteStartElement("Row", ns_def);
          }
        }
      }
    }

    public static void End(XmlWriter wr) {
      wr.WriteEndElement();
      wr.WriteEndElement();
      wr.WriteEndElement();
    }

    public static void HeaderStart(XmlWriter wr) {
      wr.WriteStartElement("Row", ns_def);
    }

    public static void HeaderItem(XmlWriter wr, string name) {
      wr.WriteStartElement("Cell", ns_def);
      wr.WriteAttributeString("StyleID", ExcelExport.ns_ss, "fmtHeader");
      {
        wr.WriteStartElement("Data", ns_def);
        wr.WriteAttributeString("Type", ns_ss, "String");
        wr.WriteString(name);
        wr.WriteEndElement();
      }
      wr.WriteEndElement();
    }

    public static void HeaderEnd(XmlWriter wr) {
      wr.WriteEndElement();
    }

    public static void RowStart(XmlWriter wr) {
      wr.WriteStartElement("Row", ns_def);
    }

    public static void RowEnd(XmlWriter wr) {
      wr.WriteEndElement();
    }

    public static void DataItem(XmlWriter wr, ExcelFormat fmt, object value) {
      wr.WriteStartElement("Cell", ns_def);
      if (value == null) fmt = ExcelFormat.String;
      switch (fmt) {
        case ExcelFormat.Date: wr.WriteAttributeString("StyleID", ExcelExport.ns_ss, "fmtDate"); break;
        case ExcelFormat.Currency: wr.WriteAttributeString("StyleID", ExcelExport.ns_ss, "fmtCurrency"); break;
        //case ExcelFormat.Lookup: wr.WriteAttributeString("StyleID", ExcelExport.ns_ss, "fmtRight"); break;
      }
      {
        wr.WriteStartElement("Data", ns_def);
        switch (fmt) {
          case ExcelFormat.Date: wr.WriteAttributeString("Type", ExcelExport.ns_ss, "DateTime"); break;
          case ExcelFormat.Currency: wr.WriteAttributeString("Type", ExcelExport.ns_ss, "Number"); break;
          case ExcelFormat.Number: wr.WriteAttributeString("Type", ExcelExport.ns_ss, "Number"); break;
          default: wr.WriteAttributeString("Type", ExcelExport.ns_ss, "String"); break;
        }
        if (value != null)
          switch (fmt) {
            case ExcelFormat.Currency: wr.WriteValue(Convert.ToDouble(value)); break;
            case ExcelFormat.Number: wr.WriteValue(Convert.ToInt64(value)); break;
            case ExcelFormat.Date: wr.WriteString(Convert.ToDateTime(value).ToString("yyyy-MM-dd")); break;
            default: wr.WriteString(value.ToString()); break;
          }
        wr.WriteEndElement();
      }
      wr.WriteEndElement();
    }

    public static void TableToExcel(DataTable table, Stream xml, ExcelColumns columns) {
      using (XmlWriter wr = XmlWriter.Create(xml)) {
        Start(wr);
        HeaderStart(wr);
        foreach (DataColumn col in table.Columns)
          HeaderItem(wr, col.ColumnName);
        HeaderEnd(wr);
        foreach (DataRow row in table.Rows) {
          RowStart(wr);
          foreach (DataColumn col in table.Columns) {
            object val = row[col];
            wr.WriteStartElement("Cell", ns_def);
            columns.finishCell(col.ColumnName, wr, val);
            {
              wr.WriteStartElement("Data", ns_def);
              columns.finishData(col.ColumnName, wr, val);
              columns.finishValue(col.ColumnName, wr, val);
              wr.WriteEndElement();
            }
            wr.WriteEndElement();
          }
          RowEnd(wr);
        }
        End(wr);
      }
    }

    public static void excelToObjects(XElement root, LMEventHandler<List<string>> objectReaded) {
      List<string> res = new List<string>();
      foreach (XElement row in root.Element(excelDef + "Worksheet").Element(excelDef + "Table").Elements(excelDef + "Row")) {
        res.Clear();
        foreach (XElement cell in row.Elements(excelDef + "Cell").Select(c => c.Element(excelDef + "Data"))) res.Add(cell.Value);
        objectReaded(res);
      }
    }

    public static IEnumerable<IEnumerable<string>> excelToObjects(XElement root) {
      XElement buf;
      foreach (XElement row in root.Element(excelDef + "Worksheet").Element(excelDef + "Table").Elements(excelDef + "Row"))
        yield return row.Elements(excelDef + "Cell").Select(c => (buf = c.Element(excelDef + "Data")) == null ? "" : buf.Value);
    }

    public static void linqToExcel(IEnumerable<IEnumerable<string>> data, Stream xml) {
      ExcelColumn col = new ExcelColumn(null, ExcelFormat.String);
      using (XmlWriter wr = XmlWriter.Create(xml)) {
        Start(wr);
        foreach (IEnumerable<string> actObj in data) {
          RowStart(wr);
          foreach (string val in actObj) {
            wr.WriteStartElement("Cell", ns_def);
            col.finishCell(wr, val);
            {
              wr.WriteStartElement("Data", ns_def);
              col.finishData(wr, val);
              col.finishValue(wr, val);
              wr.WriteEndElement();
            }
            wr.WriteEndElement();
          }
          RowEnd(wr);
        }
        End(wr);
      }
    }

    public static void linqToExcel(IEnumerable<object> data, Stream xml, ExcelColumns columns) {
      using (XmlWriter wr = XmlWriter.Create(xml)) {
        Start(wr);
        HeaderStart(wr);
        foreach (var col in columns) HeaderItem(wr, col.Value.Title);
        HeaderEnd(wr);
        foreach (object actObj in data) {
          RowStart(wr);
          foreach (var col in columns) {
            PropertyInfo prop = actObj.GetType().GetProperty(col.Value.ColumnName);
            object val = prop.GetValue(actObj, null);
            wr.WriteStartElement("Cell", ns_def);
            columns.finishCell(prop.Name, wr, val);
            {
              wr.WriteStartElement("Data", ns_def);
              columns.finishData(prop.Name, wr, val);
              columns.finishValue(prop.Name, wr, val);
              wr.WriteEndElement();
            }
            wr.WriteEndElement();
          }
          RowEnd(wr);
        }
        End(wr);
      }
    }

  }
}

