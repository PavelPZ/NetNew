using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ViewLib {
  [ParseChildren(true, "Template")]
  public abstract class TemplateCtrl : Control, INamingContainer {
    [PersistenceMode(PersistenceMode.InnerDefaultProperty), TemplateContainer(typeof(TemplateCtrl))]
    public ITemplate Template { get; set; }
    protected override void CreateChildControls() {
      base.CreateChildControls();
      Controls.Add(new LiteralControl(BeforeText()));
      if (Template != null) Template.InstantiateIn(this);
      Controls.Add(new LiteralControl(AfterText()));
    }
    protected abstract string BeforeText();
    protected abstract string AfterText();
    protected string UrlToAnchorAttrs (string url) {
      if (string.IsNullOrEmpty(url)) return null;
      var parts = url.Split(',');
      return parts.Length == 1 ? string.Format("data-click='{0}'", parts[0]) : string.Format("data-click='{0}' data-delegate-index='{1}'", parts[0], parts[1]);
    }
  }
  public class jqm_collapsible_set : TemplateCtrl {
    protected override string BeforeText() { return  "<div data-role='collapsible-set' data-theme='b' data-content-theme='d'>"; }
    protected override string AfterText() { return "</div>"; }
  }
  public class jqm_collapsible : TemplateCtrl {
    public string Title { get; set; }
    public string Collapsed { get; set; }
    public string Url { get; set; }
    protected override string BeforeText() { return string.Format("<div data-role='collapsible' data-collapsed='{0}' {1}><h2>{2}</h2>", Collapsed, fakeCollapse(), Title); }
    protected override string AfterText() { return "</div>"; }
    string fakeCollapse() {
      if (Url == null) return null;
      return " data-finish='fake-collapsible' data-collapsed-icon='arrow-r' data-expanded-icon='arrow-r' data-iconpos='right' data-theme='c'" + UrlToAnchorAttrs(Url);
    }
  }
  public class jqm_listview : TemplateCtrl {
    public bool InSet { get; set; }
    public bool Filter { get; set; }
    protected override string BeforeText() { return string.Format("<ul data-role='listview' data-theme='d' data-divider-theme='d' {0} {1}>", InSet ? "data-inset='true'" : null, Filter ? "data-filter='true'" : null); }
    protected override string AfterText() { return "</ul>"; }
  }
  public class jqm_list_divider : TemplateCtrl {
    protected override string BeforeText() { return "<li data-role='list-divider'>"; }
    protected override string AfterText() { return "</li>"; }
  }
  public class jqm_list_item : TemplateCtrl {
    public string Url { get; set; }
    protected override string BeforeText() { return Url == null ? "<li>" : string.Format("<li><a href='#' {0}>", UrlToAnchorAttrs (Url)); }
    protected override string AfterText() { return Url == null ? "</li>" : "</a></li>"; }
  }
  public class jqm_list_fields : TemplateCtrl {
    protected override string BeforeText() { return "<li data-role='fieldcontain'>"; }
    protected override string AfterText() { return "</li>"; }
  }
  public class jqm_controlgroup : TemplateCtrl {
    protected override string BeforeText() { return string.Format("<div data-role='controlgroup'>"); }
    protected override string AfterText() { return "</div>"; }
  }
  public class jqm_button : TemplateCtrl {
    public string Url { get; set; }
    protected override string BeforeText() { return string.Format("<a href='#' data-role='button' {0}>", UrlToAnchorAttrs(Url)); }
    protected override string AfterText() { return "</a>"; }
  }
  public class JsRenderScript : TemplateCtrl {
    public bool IsRoot { get; set; }
    public string Name { get; set; }
    protected override string BeforeText() {
      return string.Format(IsRoot ? rootStart : start, Name.Split(',')[0].ToLower(), Name); 
    }
    protected override string AfterText() { return IsRoot ? rootEnd : end; }
    //const string rootStart = "<script email='{0}' type='text/x-jsrender' data-for='{0}Model'>{{{{for {0}}}}}";
    const string rootStart = "<script id='{0}' type='text/x-jsrender' data-for='{1}'>{{{{for ~ActPage()}}}}";
    const string rootEnd = "{{/for}}</script>";
    const string start = "<script id='{0}' type='text/x-jsrender'>";
    const string end = "</script>";
  }

  public class CfgPage : Page {
    public string JSONCfg { get; set; }
  }


  [ParseChildren(true, "Template")]
  public class JsRenderScriptCtrl : UserControl {

    public string Name { get; set; }
    public bool IsRoot { get; set; }

    public class TemplateOwner : Control, INamingContainer {
    }

    [PersistenceMode(PersistenceMode.InnerDefaultProperty), DefaultValue(typeof(ITemplate), ""), TemplateContainer(typeof(TemplateOwner))]
    public virtual ITemplate Template { get; set; }

    protected override void CreateChildControls() {
      BodyPlace.Controls.Clear();
      TemplateOwner ownerValue = new TemplateOwner();
      ITemplate temp = Template;
      if (temp == null) return;
      temp.InstantiateIn(ownerValue);
      BodyPlace.Controls.Add(ownerValue);
    }

    public PlaceHolder BodyPlace;
  }

}