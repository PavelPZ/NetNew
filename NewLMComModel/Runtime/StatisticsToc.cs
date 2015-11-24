using LMComLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Statistics {

  public static class TocExs {
    public static TocExsRow createProductForCompany(int companyId, string prodUrl) {
      //TreeListTextColumn
      var product = Lib.prodDir[prodUrl]; var company = Lib.company(companyId);
      lock (product) {
        var allUsers = company.UserDir.Values.Select(usr => usr.getProductData(prodUrl)).Where(pd => pd != null).ToArray(); if (allUsers.Length == 0) return null;
        //exCount a tempData
        foreach (var t in product.scan()) t.tempData = new TocExsRow() { exCount = t.exCount * allUsers.Length, toc = t };
        //skiped
        foreach (var userData in allUsers) {
          if (userData.SkipedIds == null) continue;
          foreach (var skId in userData.SkipedIds) {
            var toc = Lib.tocIdsDir[skId];
            UserEx existingEx;
            //zaloz (pokud neexistuji) pro usera cviceni a nastav mu isSkiped
            foreach (var t in toc.scan().Where(t => t.isType(CourseMeta.runtimeType.ex))) { //pro vsechna child cviceni Skipped uzlu:
              if (!userData.Exercises.TryGetValue(t.Id, out existingEx)) userData.Exercises.Add(t.Id, existingEx = new UserEx { Toc = t, User = userData.user, Product = product });
              existingEx.isSkiped = true; //nastav isSkipped
            }
          }
        }
        //exercises
        foreach (var exGroup in allUsers.SelectMany(u => u.Exercises.Values).GroupBy(e => e.Id))
          Lib.tocIdsDir[exGroup.Key].tocTemp.setUserExs(exGroup.ToArray());
        //agregace dat
        product.refreshNumbers();
        //vybuduj tree
        return product.extractTempData(null);
      }
    }

  }

  public partial class Toc {

    public void refreshNumbers() {
      var data = tocTemp;
      if (Items != null) foreach (var it in Items) {
          it.refreshNumbers();
          TocExsRow itData = (TocExsRow)it.tempData;
          data.skipedCount += itData.skipedCount;
          data.complPassiveCnt += itData.complPassiveCnt; data.complNotPassiveCnt += itData.complNotPassiveCnt; data.elapsed += itData.elapsed;
          if (itData.points > 0) data.points += itData.points; //zaporne score => nevyhodnotitelne
        }
      data.finishAfterRefreshNumbers();
    }

    public TocExsRow tocTemp { get { return (TocExsRow)tempData; } }

    public TocExsRow extractTempData(TocExsRow parent) {
      var t = tocTemp;
      try {
        if (t.skipedCount == 0 && t.complPassiveCnt == 0 && t.complNotPassiveCnt == 0) return null;
        t.Items = Items == null ? null : Items.Select(it => it.extractTempData(t)).Where(it => it != null).ToArray();
        return t;
      } finally { //vynuluj temp data
        foreach (var it in scan()) it.tempData = null;
      }
    }

  }

  public class TocExsRow {
    public TocExsRow() { }
    public Toc toc;
    public UserEx[] userExs;
    public TocExsRow[] Items;
    public IEnumerable<TocExsRow> scan() { return XExtension.Create(this).Concat(Items==null ? Enumerable.Empty<TocExsRow>() : Items.SelectMany(it => it.scan())); }
    public TocExsRow find(int id) {
      if (toc.Id == id) return this; 
      if (Items != null) foreach (var it in Items) { var res = it.find(id); if (res != null) return res; }
      return null;
    }

    public bool done { get; set; }
    public int exCount { get; set; }
    public bool isSkiped { get; set; }
    public int complPassiveCnt { get; set; } //pocet aktivnich vyhodnocenych cviceni
    public int complNotPassiveCnt { get; set; }//pocet pasivnich vyhodnocenych cviceni
    public int skipedCount { get; set; } //pocet preskocenych cviceni
    public int points { get; set; } //skore v procentech
    public int elapsed { get; set; } //straveny cas ve vterinach
    public int score { get { return complNotPassiveCnt == 0 ? -1 : (int)(points / complNotPassiveCnt); } }

    //pro treeview
    public int Id { get { return toc.Id; } }
    public int ParentId { get { return toc.ParentId; } }
    public string Title { get { return toc.Title; } }

    //progress
    public string ProgressBarHtml { get { return Lib.progressInfo_html(exCount, skipedCount, complNotPassiveCnt + complPassiveCnt); } }
    public string ScoreBarHtml { get { return Lib.scoreInfo_html(!isSkiped && complNotPassiveCnt > 0, complNotPassiveCnt, points); } }

    public void setUserExs(UserEx[] userExs) {
      this.userExs = userExs;
      if (exCount==0) exCount = userExs.Length; // pro pouziti v GridCellTemplate
      foreach (var ex in userExs) {
        if (ex.isSkiped) { skipedCount++; continue; }
        elapsed += ex.Elapsed;
        if (ex.Score < 0) complPassiveCnt++; else complNotPassiveCnt++;
        if (ex.Score > 0) points += ex.Score;
      }
    }

    public void finishAfterRefreshNumbers() {
      if (skipedCount > 0 && skipedCount == exCount) { isSkiped = true; return; } //all child skiped => return
      if (complNotPassiveCnt + complPassiveCnt + skipedCount == exCount) done = true;
    }

  }
}
