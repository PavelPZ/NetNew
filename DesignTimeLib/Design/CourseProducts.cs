using LMComLib;
using LMNetLib;
using schools;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CourseMeta {

  public static class LangCourses {
    public static IEnumerable<data> generateGrafiaProducts() {
      string[] ids = new string[] { "od1_Administrativ", "od2_Geschaftsverhandlungen", "od3_Hotel", "od4_Seniorenpflege", "od5_Logistik", "od6_Autoteile", "od7_Elektrotechnik", "od8_Maschinenbau" };

      //yield return prodDef.genCourse(lib.publishers, "grafia", "od1_8", CourseIds.German, false, "Open Door",
      //  ids.Select(email => new ptr(new taskCourse(), "/grafia/opendoor/de/" + email + "/") { takeChilds = childMode.selfChild }).ToArray()
      //  );

      foreach (var id in ids)
        yield return prodDef.genCourse(Lib.publishers, "grafia", id, CourseIds.German, false, dictTypes.no, new Langs[] { Langs.cs_cz }, Lib.publishers.find("/grafia/opendoor/de/" + id + "/").title,
          new ptr(new taskCourse(), "/grafia/opendoor/de/" + id + "/") { takeChilds = childMode.selfChild }
          //prodDef.genTaskCourse(null, "/grafia/opendoor/de/" + email, new ptr("/grafia/opendoor/de/" + email))
        );
    }

    public static IEnumerable<data> generateExamplesProduct() {
      yield return prodDef.genCourse(Lib.publishers, "lm", "English_a1_less3", CourseIds.English, false, dictTypes.L, null, "English course A1, lesson 3",
        //prodDef.genTaskCourse("Test English Beginners, Lesson 3", "lm/oldea/english1", new ptr("lm/oldea/english1", 2, 1)),
        new ptr(new taskCourse(), "/lm/oldea/english1/", 2, 1) { takeChilds = childMode.child },
        oldGramm.getPtr(CourseIds.English, 0)
      );
      yield return prodDef.genCourse(Lib.publishers, "lm", "prods/examples", CourseIds.English, false, dictTypes.L, null, "Doc Examples",
        new ptr(new multiTask(), "/lm/examples/") { takeChilds = childMode.selfChild }
      );
      yield return prodDef.genCourse(Lib.publishers, "lm", "prods/docexamples", CourseIds.English, false, dictTypes.L, null, "Examples",
        new ptr(new multiTask(), "/lm/docexamples/") { takeChilds = childMode.selfChild }
      );
    }

    public static IEnumerable<data> generateTestMeProduct() {
      string[] ids = new string[] { "a1", "a2", "b1", "b2", "c1", "c2" };
      foreach (var id in ids)
        yield return prodDef.genCourse(Lib.publishers, "lm", "prods/etestme/english/" + id, CourseIds.English, true, dictTypes.no, new Langs[] { Langs.cs_cz }, "eTestMe " + id.ToUpper(),
          new ptr(true, "/lm/etestme/english/" + id + "/") { takeChilds = childMode.selfChild }
        );
    }

    public static IEnumerable<data> generateJJN() {
      string[] ids = new string[] { "a1", "a2", "b1", "b2", "c1", "c2" };
      //CourseIds[] langs = new CourseIds[] { CourseIds.English, CourseIds.German, CourseIds.French, CourseIds.Italian, CourseIds.Russian, CourseIds.Spanish };
      string rootUrl; data node;
      foreach (var id in ids) {
        rootUrl = "/lm/etestme/english/" + id + "/";
        node = Lib.publishers.find(rootUrl);
        yield return prodDef.genCourse(Lib.publishers, "lm", "prods/etestme-comp/english/" + id, CourseIds.English, true, dictTypes.no, new Langs[] { Langs.cs_cz }, node.title + " (" + transComplete[CourseIds.English] + ")",
          new ptr(string.Format("needs=recording;demoTestUrl=/lm/prods/etestme-comp-demo/english/{0}/", id), rootUrl) { takeChilds = childMode.selfChild }
        );
        yield return prodDef.genCourse(Lib.publishers, "lm", "prods/etestme-std/english/" + id, CourseIds.English, true, dictTypes.no, new Langs[] { Langs.cs_cz }, node.title + " (" + transStandard[CourseIds.English] + ")",
          new ptr(string.Format("needs=playing;demoTestUrl=/lm/prods/etestme-std-demo/english/{0}/", id), rootUrl) { takeChilds = childMode.selfChild, take = 3 }
        );
        yield return prodDef.genCourse(Lib.publishers, "lm", "prods/etestme-comp-demo/english/" + id, CourseIds.English, true, dictTypes.no, new Langs[] { Langs.cs_cz }, node.title + " (demo for complete)",
          skrivanek_Demo(false, CourseIds.English, true, id, node.title + " (demo for complete)")
        );
        yield return prodDef.genCourse(Lib.publishers, "lm", "prods/etestme-std-demo/english/" + id, CourseIds.English, true, dictTypes.no, new Langs[] { Langs.cs_cz }, node.title + " (demo for standard)",
          skrivanek_Demo(false, CourseIds.English, false, id, node.title + " (demo for standard)")
        );
      }
      rootUrl = "/lm/etestme/english/";
      node = Lib.publishers.find(rootUrl);
      yield return prodDef.genCourse(Lib.publishers, "lm", "prods/etestme-comp/english/all", CourseIds.English, true, dictTypes.no, new Langs[] { Langs.cs_cz }, node.title + " (" + transComplete[CourseIds.English] + ")",
        new ptr(true, rootUrl) { takeChilds = childMode.skrivanek_multiTest_compl }
      );
      yield return prodDef.genCourse(Lib.publishers, "lm", "prods/etestme-std/english/all", CourseIds.English, true, dictTypes.no, new Langs[] { Langs.cs_cz }, node.title + " (" + transStandard[CourseIds.English] + ")",
        new ptr(true, rootUrl) { takeChilds = childMode.skrivanek_multiTest_std }
      );
    }

    public static IEnumerable<data> generateSkrivanekProduct() {
      string[] ids = new string[] { "a1", "a2", "b1", "b2", "c1", "c2" };
      CourseIds[] langs = new CourseIds[] { CourseIds.English, CourseIds.German, CourseIds.French, CourseIds.Italian, CourseIds.Russian, CourseIds.Spanish };
      string rootUrl; data node;
      foreach (var lang in langs) {
        foreach (var id in ids) {
          rootUrl = "/skrivanek/" + lang.ToString() + "/" + id + "/";
          node = Lib.publishers.find(rootUrl);
          yield return prodDef.genCourse(Lib.publishers, "skrivanek", "prods/etestme-comp/" + lang.ToString() + "/" + id, lang, true, dictTypes.no, new Langs[] { Langs.cs_cz }, node.title + " (" + transComplete[lang] + ")",
            new ptr(
              //lang != CourseIds.English ? "needs=recording" : string.Format("needs=recording;demoTestUrl=/skrivanek/prods/etestme-comp-demo/{0}/{1}/", lang, id),
              string.Format("needs=recording;demoTestUrl=/skrivanek/prods/etestme-comp-demo/{0}/{1}/", lang, id), rootUrl) { takeChilds = childMode.selfChild }
          );
          yield return prodDef.genCourse(Lib.publishers, "skrivanek", "prods/etestme-std/" + lang.ToString() + "/" + id, lang, true, dictTypes.no, new Langs[] { Langs.cs_cz }, node.title + " (" + transStandard[lang] + ")",
            new ptr(
              //lang != CourseIds.English ? "needs=playing" : string.Format("needs=playing;demoTestUrl=/skrivanek/prods/etestme-std-demo/{0}/{1}/", lang, id),
              string.Format("needs=playing;demoTestUrl=/skrivanek/prods/etestme-std-demo/{0}/{1}/", lang, id),
              rootUrl) { takeChilds = childMode.selfChild, take = 3 }
          );
          yield return prodDef.genCourse(Lib.publishers, "skrivanek", "prods/etestme-comp-demo/" + lang.ToString() + "/" + id, lang, true, dictTypes.no, new Langs[] { Langs.cs_cz }, node.title + " (demo for complete)",
            skrivanek_Demo(true, lang, true, id, node.title + " (demo for complete)")
          );
          yield return prodDef.genCourse(Lib.publishers, "skrivanek", "prods/etestme-std-demo/" + lang.ToString() + "/" + id, lang, true, dictTypes.no, new Langs[] { Langs.cs_cz }, node.title + " (demo for standard)",
            skrivanek_Demo(true, lang, false, id, node.title + " (demo for standard)")
          );
        }
        rootUrl = "/skrivanek/" + lang.ToString() + "/";
        node = Lib.publishers.find(rootUrl);
        yield return prodDef.genCourse(Lib.publishers, "skrivanek", "prods/etestme-comp/" + lang.ToString() + "/all", lang, true, dictTypes.no, new Langs[] { Langs.cs_cz }, node.title + " (" + transComplete[lang] + ")",
          new ptr(true, rootUrl) { takeChilds = childMode.skrivanek_multiTest_compl }
        );
        yield return prodDef.genCourse(Lib.publishers, "skrivanek", "prods/etestme-std/" + lang.ToString() + "/all", lang, true, dictTypes.no, new Langs[] { Langs.cs_cz }, node.title + " (" + transStandard[lang] + ")",
          new ptr(true, rootUrl) { takeChilds = childMode.skrivanek_multiTest_std }
        );
      }
    }

    public static IEnumerable<data> generateBlendedProduct() {
      Dictionary<CourseIds, string[]> ids = new Dictionary<CourseIds, string[]>() {
        { CourseIds.English,  new string[] { "lessons/lesson_a1_1", "tests/checkpointtesta1_1", "pretesta1", "pretesta2", "pretestb1", "pretestb2" } },
      };
      data node;
      foreach (var langProds in ids) {
        var rootUrl = "/lm/blended/" + langProds.Key.ToString() + "/";
        foreach (var id in langProds.Value) {
          var url = rootUrl + id;
          var prodId = url.Replace('/', '_');
          url += "/";
          node = Lib.publishers.find(url);
          var res = prodDef.genCourse(Lib.publishers, "lm", "prods" + prodId, langProds.Key, id != "lessons/lesson_a1_1", dictTypes.no, new Langs[] { Langs.cs_cz, Langs.en_gb }, node.title,
            new ptr(true, url) { takeChilds = childMode.selfChild }
          );
          yield return res;
        }
      }
      CourseIds[] vyzva57 = new CourseIds[] { CourseIds.English, CourseIds.German, CourseIds.French };
      //CourseIds[] vyzva57 = new CourseIds[] { CourseIds.English };
      foreach (var lang in vyzva57) {
        var url = ("/lm/BLCourse/" + lang.ToString()).ToLower();
        var prodId = url.Replace('/', '_');
        url += "/";
        node = Lib.publishers.find(url);
        var res = prodDef.genCourse(Lib.publishers, "lm", "prods" + prodId, lang, false, dictTypes.L, new Langs[] { Langs.cs_cz, Langs.en_gb }, node.title,
          new ptr(true, url) { takeChilds = childMode.child }
        );
        res.other = node.other; res.type = runtimeType.product | runtimeType.productNew;
        yield return res;
      }
    }

    public static IEnumerable<data> generateEdusoftProduct() {
      string[] ids = new string[] { "demo/t01", "demo/t02", "demo/t03", "demo/t04", "demo/t05", "demo/t06", "demo/t07", "demo/t08", "demo/t09", "demo/t10", };
      CourseIds[] langs = new CourseIds[] { CourseIds.English };
      data node;
      foreach (var id in ids) {
        var rootUrl = "/edusoft/english/" + id + "/";
        node = Lib.publishers.find(rootUrl);
        yield return prodDef.genCourse(Lib.publishers, "edusoft", "prods/etestme-demo/english/" + id, CourseIds.English, true, dictTypes.no, new Langs[] { Langs.vi_vn, Langs.en_gb }, node.title,
          new ptr(true, "/edusoft/english/" + id + "/") { takeChilds = childMode.selfChild }
        );
      }
    }


    static Dictionary<CourseIds, string> transStandard = new Dictionary<CourseIds, string> { { CourseIds.English, "standard" }, { CourseIds.German, "standard" }, { CourseIds.Russian, "стандартный" }, { CourseIds.Spanish, "estándar" }, { CourseIds.Italian, "standard" }, { CourseIds.French, "standard" }, };
    static Dictionary<CourseIds, string> transComplete = new Dictionary<CourseIds, string> { { CourseIds.English, "complete" }, { CourseIds.German, "komplett" }, { CourseIds.Russian, "полный" }, { CourseIds.Spanish, "completo" }, { CourseIds.Italian, "completo" }, { CourseIds.French, "complet" }, };
    //static Dictionary<string, int> skillOrder = new Dictionary<string, int>() { {  } };

    static test skrivanek_Demo(bool isSkrivanek, CourseIds lang, bool isCompl, string id, string title) {
      var skills = test_Demo_map(isSkrivanek).Element(lang.ToString()).Element(id).Element(isCompl.ToString());
      test res = new test {
        isDemoTest = true,
        title = title,
        Items = skills.Elements().Select(skill => new taskTestSkill {
          type = runtimeType.taskTestSkill | runtimeType.noDict | runtimeType.mod | runtimeType.dynamicModuleData,
          title = skill.Name.LocalName,
          skill = skill.Name.LocalName,
          minutes = 5,
          //Items = skill.Elements().Select(grp => new ptr(true, (isSkrivanek ? "/skrivanek" : "/lm") + string.Format("/demo/{0}/{1}/{2}/ex", lang, skill.Name.LocalName, grp.Name.LocalName)) { takeChilds = childMode.selfChild }).ToArray(),
          Items = skill.Elements().Select(grp => new ptr(true, string.Format("/skrivanek/demo/{0}/{1}/{2}/ex", lang, skill.Name.LocalName, grp.Name.LocalName)) { takeChilds = childMode.selfChild }).ToArray(),
        }).ToArray()
      };
      return res;
    }

    public static XElement test_Demo_map(bool isSkrivanek) {
      var idx = isSkrivanek ? 0 : 1;
      if (_test_Demo_maps[idx] == null) {
        string[] ids = new string[] { "a1", "a2", "b1", "b2", "c1", "c2" };
        CourseIds[] langs = isSkrivanek ? new CourseIds[] { CourseIds.English, CourseIds.German, CourseIds.French, CourseIds.Italian, CourseIds.Russian, CourseIds.Spanish } : new CourseIds[] { CourseIds.English };
        string[] skills = new string[] { testMe.Skills.UseEnglish, testMe.Skills.Reading, testMe.Skills.Listening, testMe.Skills.Speaking, testMe.Skills.Writing };
        XElement root, xlang, xid, xisCompl, xskill; root = new XElement("root");
        foreach (var lang in langs) {
          root.Add(xlang = new XElement(lang.ToString()));
          foreach (var id in ids) {
            xlang.Add(xid = new XElement(id));
            foreach (var isCompl in new bool[] { true, false }) {
              xid.Add(xisCompl = new XElement(isCompl.ToString()));
              foreach (var skill in skills.Where(s => isCompl || testMe.Skills.stdOld.Contains(s))) {
                xisCompl.Add(xskill = new XElement(skill));
                var dir = string.Format(Machines.rootPath + (isSkrivanek ? @"skrivanek\{0}\{1}\{2}" : @"lm\etestme\{0}\{1}\{2}"), lang, id, skill);
                xskill.Add(Directory.EnumerateDirectories(dir).Select(d => new XElement(d.Split('\\').Last().ToLower())).Distinct());
              }
            }
          }
        }
        _test_Demo_maps[idx] = root;
      }
      return _test_Demo_maps[idx];
    }
    static XElement[] _test_Demo_maps = new XElement[] { null, null };

    static void skrivanek_demo_fakexml() {
      var root = CourseMeta.LangCourses.test_Demo_map(true);

      var groups = root.Elements().Select(xlang => new {
        lang = xlang.Name.LocalName,
        groups = xlang.Elements().SelectMany(xx => xx.Elements().SelectMany(x => x.Elements().SelectMany(xskill => xskill.Elements().Select(e => e.Name.LocalName).Select(grp => (xskill.Name.LocalName + "/" + grp).ToLower())))).Distinct().OrderBy(s => s)
      });

      var skillGrps = groups.Select(lang => new {
        lang.lang,
        skills = lang.groups.Select(sg => sg.Split('/')).GroupBy(sg => sg[0]).Select(sg => new {
          skill = sg.Key,
          grps = sg.Select(g => g[1]).ToArray()
        }).ToArray()
      }).ToArray();

      foreach (var lang in skillGrps) {
        var langDir = string.Format(@"d:\LMCom\rew\Web4\skrivanek\demo\{0}", lang.lang);
        foreach (var skill in lang.skills) {
          var skillDir = langDir + "\\" + skill.skill;
          foreach (var grp in skill.grps) {
            var grpDir = skillDir + "\\" + grp;
            LMNetLib.LowUtils.AdjustDir(grpDir);
            //<testTaskGroup take="1" order="1" designTitle="ListeningGapfill_Multitask" />
            CourseMeta.data.writeObject(new testTaskGroup { title = grp, take = 1, type = runtimeType.testTaskGroup, designTitle = grp }, grpDir + "\\meta.xml");
            File.Copy(@"D:\temp\ex.xml", grpDir + "\\ex.xml");
          }
          //<taskTestSkill type="taskTestSkill noDict mod dynamicModuleData" title="demo" skill="Listening" order="3" minutes="25" />
          CourseMeta.data.writeObject(new taskTestSkill { title = skill.skill, skill = skill.skill, minutes = 5, type = runtimeType.taskTestSkill | runtimeType.noDict | runtimeType.mod | runtimeType.dynamicModuleData }, skillDir + "\\meta.xml");
        }
        //<testGlobalAdmin title="English Demo" type="testGlobalAdmin" />
        CourseMeta.data.writeObject(new test { title = string.Format("{0} demo", lang.lang), type = runtimeType.test }, langDir + "\\meta.xml");
      }
    }

  }

  public static class OldLangCourses {

    //****************** COMMON
    public class levelDescr {
      public int levIdx; //poradove cislo urovne, 0, 1, ...
      public int skip; //preskocene lekce urovne
      public int take; //vem lekce urovne
      public data toLevel(CourseIds crsId, int idx, string title = null) { //z urovne vygeneruj kurz (= jeden button na home kurzu)
        string crsStr = crsId.ToString().ToLower();
        string levUrl;
        if (crsId == CourseIds.Russian && levIdx == 3) levUrl = "/lm/russian4";
        else if (crsId == CourseIds.EnglishE) levUrl = string.Format("/lm/oldea/english{0}e", levIdx + 1);
        else levUrl = string.Format("/lm/oldea/{0}{1}", crsStr, /*crsStrCommonLib.CourseIdToLineId(crsId)*/ levIdx + 1).ToLower();
        return prodDef.genTaskCourse(title, levUrl + "_" + idx + "/", new ptr(true, levUrl + "/", skip, take));
      }
    }

    const string globProductIdMask = "{0}_{1}_{2}";

    //****************** STANDARD
    public static IEnumerable<data> generateStandard() {
      return langStrings.generateAll();
    }

    public class courseParts {
      public int startIdx;
      public data lev1; //prvni cast urovne
      public data lev2; //druha cast urovne. Pro # uroven nemciny a pro rustinu je null
      public IEnumerable<data> levels() { yield return lev1; if (lev2 != null) yield return lev2; }
    }
    public class langStrings {
      public string[] levNames;
      public string partMask;
      public string withTest;
      public int[] partsTake;
      public string[] partsSERR;
      public static IEnumerable<data> generateAll() {
        foreach (var crs in courseTitle.Keys)
          foreach (var prod in levelTitle[CommonLib.CourseIdToLang(crs)].generate(crs)) yield return prod;
      }
      IEnumerable<data> generate(CourseIds crsId) {
        string title = courseTitle[crsId];
        var levParts = getParts(crsId).ToArray(); //urovne kurzu. Sestavaji budto z jedne nebou dvou casti
        var levels = levParts.SelectMany(p => p.levels()).ToArray(); //nejmensi casti kurzu
        //generuj celky kurz
        yield return prodDef.genCourse(Lib.publishers, "lm", string.Format(globProductIdMask, crsId, "0", levels.Length), crsId, false, dictTypes.L, null, title,
          levels,
          oldGramm.getPtr(crsId)
        );
        //generuj urovne a pulorovne
        for (var levIdx = 0; levIdx < levParts.Length; levIdx++) {
          var levPart = levParts[levIdx];
          yield return prodDef.genCourse(Lib.publishers, "lm", string.Format(globProductIdMask, crsId, levPart.startIdx, levPart.levels().Count()), crsId, false, dictTypes.L, null, title + ", " + levNames[levIdx],
            levPart.lev1, levPart.lev2,
            oldGramm.getPtr(crsId, levIdx)
          );
          if (levPart.lev2 != null) {
            yield return prodDef.genCourse(Lib.publishers, "lm", string.Format(globProductIdMask, crsId, levPart.startIdx, 1), crsId, false, dictTypes.L, null, title + ", " + levNames[levIdx] + ", " + string.Format(partMask, 1),
              levPart.lev1,
              oldGramm.getPtr(crsId, levIdx)
            );
            yield return prodDef.genCourse(Lib.publishers, "lm", string.Format(globProductIdMask, crsId, levPart.startIdx + 1, 1), crsId, false, dictTypes.L, null, title + ", " + levNames[levIdx] + ", " + string.Format(partMask, 2),
              levPart.lev2,
              oldGramm.getPtr(crsId, levIdx)
            );
          }
        }
      }
      IEnumerable<courseParts> getParts(CourseIds crsId) {
        int startIdx = 0;
        for (var levIdx = 0; levIdx < partsTake.Length; levIdx++) {
          if (partsTake[levIdx] < 0)
            yield return new courseParts { //pouze jedna cast na level
              startIdx = startIdx,
              lev1 = new levelDescr { levIdx = levIdx }.toLevel(crsId, 0, string.Format("{0} ({1})", levNames[levIdx], partsSERR[levIdx * 2])),
              lev2 = null
            };
          else
            yield return new courseParts { //dve casti na level
              startIdx = startIdx++,
              lev1 = new levelDescr { levIdx = levIdx, take = partsTake[levIdx] }.toLevel(crsId, 0, string.Format("{0} {1} ({2})", levNames[levIdx], string.Format(partMask, 1), partsSERR[levIdx * 2])),
              lev2 = new levelDescr { levIdx = levIdx, skip = partsTake[levIdx] }.toLevel(crsId, 1, string.Format("{0} {1} ({2})", levNames[levIdx], string.Format(partMask, 2), partsSERR[levIdx * 2 + 1]))
            };
          startIdx++;
        }
      }
    }

    static Dictionary<Langs, langStrings> levelTitle = new Dictionary<Langs, langStrings>() {
      {Langs.en_gb, new langStrings {
        levNames = new string[] {"Beginners", "False Beginners", "Pre-intermediate", "Intermediate", "Upper Intermediate"},
        partMask = "part {0}",
        withTest = " with Tests",
        partsTake = new int[] {8,8,8,8,9},
        partsSERR = new string[] {"A1","A1","A1-A2","A1-A2","A2","A2-B1","A2-B1","B1","B1-B2","B1-B2"},
      }},
      {Langs.de_de, new langStrings {
        levNames = new string[] {"Anfänger", "Mittelfortgeschrittene", "Fortgeschrittene"},
        partMask = "Teil {0}",
        withTest = "",
        partsTake = new int[] {6,6,-1},
        partsSERR = new string[] {"A1","A1-A2","A2","A2-B1","B1-B2"},
      }},
      {Langs.sp_sp, new langStrings {
        levNames = new string[] {"Inicial", "Intermedio", "Avanzado"},
        partMask = "parte {0}",
        withTest = "",
        partsTake = new int[] {9,7,4},
        partsSERR = new string[] {"A1","A1-A2","A2","A2-B1","B1","B1-B2"},
      }},
      {Langs.fr_fr, new langStrings {
        levNames = new string[] {"Débutants", "Débutants avancés", "Avancés"},
        partMask = "partie {0}",
        withTest = "",
        partsTake = new int[] {7,6,4},
        partsSERR = new string[] {"A1","A1-A2","A2","A2-B1","B1","B1-B2"},
      }},
      {Langs.it_it, new langStrings {levNames = new string[] {"Principiante", "Intermedio", "Avanzato"},
        partMask = "parte {0}",
        withTest = "",
        partsTake = new int[] {5,5,5},
        partsSERR = new string[] {"A1","A1-A2","A2","A2-B1","B1","B1-B2"},
      }},
      {Langs.ru_ru, new langStrings {
        levNames = new string[] {"Начинающие", "Начальный средний", "Средний уровень", "Продвинутый уровень"},
        partMask = "XXXXX", //rustina nema casti 
        withTest = "",
        partsTake = new int[] {-1,-1,-1},
        partsSERR = new string[] {"A1","","A2","","A2-B1"},
      }}
    };
    static Dictionary<CourseIds, string> courseTitle = new Dictionary<CourseIds, string>() {
      {CourseIds.English, "English Courses"},
      {CourseIds.EnglishE, "LANGMaster English"},
      {CourseIds.German, "Deutschkurs"},
      {CourseIds.Spanish, "Curso de español"},
      {CourseIds.French, "Cours de français"},
      {CourseIds.Italian, "Corso di Italiano"},
      {CourseIds.Russian, "Курсы русского"},
    };

    //****************** A1, A2, ..., vcetne zemedelky
    public static IEnumerable<data> generateA1_C2() {
      foreach (var ds in descrsAC4) foreach (var p in ds.generate()) yield return p;
    }

    public class crsDescr {
      public CourseIds course;
      public string titleMask;
      public string productIdMask;
      public levelDescr[] levels;
      public int startLev;
      public IEnumerable<data> generate() {
        int lev = startLev > 0 ? startLev : 1; string crsStr = course.ToString().ToLower();
        foreach (var chunk in levels.Chunk<levelDescr>(4)) {
          var part = 1;
          var levStr = lev < 3 ? "A" + lev.ToString() : "B" + (lev - 2).ToString();
          foreach (var p in chunk) {
            var title = string.Format(titleMask, levStr, part); var productId = string.Format(productIdMask ?? globProductIdMask, course, levStr, part);
            yield return prodDef.genCourse(Lib.publishers, "lm", productId, course, false, dictTypes.L, null, title,
              p.toLevel(course, part),
              oldGramm.getPtr(course, p.levIdx)
            );
            part++;
          }
          lev++;
        }
      }
    }

    static crsDescr[] descrsAC4 = new crsDescr[] { 
      //CZU, A1
      new crsDescr {course = CourseIds.English, titleMask = "English course {0}, part {1}", productIdMask = "{0}_{1}_CZU{2}", levels = new levelDescr[] {
        new levelDescr{levIdx = 1, take = 4},
        new levelDescr{levIdx = 1, skip = 4, take = 4},
        new levelDescr{levIdx = 1, skip = 8, take = 4},
        new levelDescr{levIdx = 1, skip = 12},
      }},
      //English, A1..B2
      new crsDescr {course = CourseIds.English, titleMask = "English course {0}, part {1}", levels = new levelDescr[] {
        new levelDescr{levIdx = 0, take = 8},
        new levelDescr{levIdx = 0, skip = 8},
        new levelDescr{levIdx = 1, take = 8},
        new levelDescr{levIdx = 1, skip = 8},
        new levelDescr{levIdx = 2, take = 4},
        new levelDescr{levIdx = 2, skip = 4, take = 4},
        new levelDescr{levIdx = 2, skip = 8, take = 4},
        new levelDescr{levIdx = 2, skip = 12},
        new levelDescr{levIdx = 3, take = 4},
        new levelDescr{levIdx = 3, skip = 4, take = 4},
        new levelDescr{levIdx = 3, skip = 8, take = 4},
        new levelDescr{levIdx = 3, skip = 12},
        new levelDescr{levIdx = 4, take = 4},
        new levelDescr{levIdx = 4, skip = 4, take = 5},
        new levelDescr{levIdx = 4, skip = 9, take = 5},
        new levelDescr{levIdx = 4, skip = 14},
        //TODO dalsi urovne
      }},
      //Ostani jazyky, u rustiny jen 3 urovne
      new crsDescr {course = CourseIds.German, titleMask = "Deutschkurs {0}, Teil {1}", levels = new levelDescr[] {
        new levelDescr{levIdx = 0, take = 2},
        new levelDescr{levIdx = 0, skip = 2, take = 2},
        new levelDescr{levIdx = 0, skip = 4, take = 2},
        new levelDescr{levIdx = 0, skip = 6, take = 2},
        new levelDescr{levIdx = 0, skip = 8, take = 2},
        new levelDescr{levIdx = 0, skip = 10},
        new levelDescr{levIdx = 1, take = 2},
        new levelDescr{levIdx = 1, skip = 2, take = 2},
        new levelDescr{levIdx = 1, skip = 4, take = 2},
        new levelDescr{levIdx = 1, skip = 6, take = 2},
        new levelDescr{levIdx = 1, skip = 8, take = 2},
        new levelDescr{levIdx = 1, skip = 10},
        new levelDescr{levIdx = 2, take = 1},
        new levelDescr{levIdx = 2, skip = 1, take = 1},
        new levelDescr{levIdx = 2, skip = 2, take = 1},
        new levelDescr{levIdx = 2, skip = 3},
      }},
    new crsDescr {course = CourseIds.Russian, titleMask = "Курсы русского {0}, часть {1}", levels = new levelDescr[] {
        new levelDescr{levIdx = 0, take = 2},
        new levelDescr{levIdx = 0, skip = 2, take = 2},
        new levelDescr{levIdx = 0, skip = 4, take = 2},
        new levelDescr{levIdx = 0, skip = 6},
        new levelDescr{levIdx = 1, take = 2},
        new levelDescr{levIdx = 1, skip = 2, take = 2},
        new levelDescr{levIdx = 1, skip = 4, take = 2},
        new levelDescr{levIdx = 1, skip = 6},
        new levelDescr{levIdx = 2, take = 2},
        new levelDescr{levIdx = 2, skip = 2, take = 3},
        new levelDescr{levIdx = 2, skip = 5, take = 2},
        new levelDescr{levIdx = 2, skip = 7},
        new levelDescr{levIdx = 3, take = 2},
        new levelDescr{levIdx = 3, skip = 2, take = 2},
        new levelDescr{levIdx = 3, skip = 4, take = 3},
        new levelDescr{levIdx = 3, skip = 7},
      }},
      //new crsDescr {course = CourseIds.Russian, titleMask = "Курсы русского {0}, часть {1}", startLev = 4, levels = new levelDescr[] {
      //  new levelDescr{levIdx = 3, take = 2},
      //  new levelDescr{levIdx = 3, skip = 2, take = 2},
      //  new levelDescr{levIdx = 3, skip = 4, take = 3},
      //  new levelDescr{levIdx = 3, skip = 7},
      //}},
     new crsDescr {course = CourseIds.Italian, titleMask = "Corso di Italiano {0}, parte {1}", levels = new levelDescr[] {
        new levelDescr{levIdx = 0, take = 3},
        new levelDescr{levIdx = 0, skip = 3, take = 2},
        new levelDescr{levIdx = 0, skip = 5, take = 2},
        new levelDescr{levIdx = 0, skip = 7},
        new levelDescr{levIdx = 1, take = 3},
        new levelDescr{levIdx = 1, skip = 3, take = 3},
        new levelDescr{levIdx = 1, skip = 6, take = 2},
        new levelDescr{levIdx = 1, skip = 8},
        new levelDescr{levIdx = 2, take = 2},
        new levelDescr{levIdx = 2, skip = 2, take = 2},
        new levelDescr{levIdx = 2, skip = 4, take = 3},
        new levelDescr{levIdx = 2, skip = 7},

      }},
     new crsDescr {course = CourseIds.Spanish, titleMask = "Curso de español {0}, parte {1}", levels = new levelDescr[] {
        new levelDescr{levIdx = 0, take = 4},
        new levelDescr{levIdx = 0, skip = 4, take = 4},
        new levelDescr{levIdx = 0, skip = 8, take = 5},
        new levelDescr{levIdx = 0, skip = 13},
        new levelDescr{levIdx = 1, take = 4},
        new levelDescr{levIdx = 1, skip = 4, take = 4},
        new levelDescr{levIdx = 1, skip = 8, take = 3},
        new levelDescr{levIdx = 1, skip = 11},
        new levelDescr{levIdx = 2, take = 2},
        new levelDescr{levIdx = 2, skip = 2, take = 2},
        new levelDescr{levIdx = 2, skip = 4, take = 2},
        new levelDescr{levIdx = 2, skip = 6},

      }},
    new crsDescr {course = CourseIds.French, titleMask = "Cours de français {0}, partie {1}", levels = new levelDescr[] {
        new levelDescr{levIdx = 0, take = 3},
        new levelDescr{levIdx = 0, skip = 3, take = 3},
        new levelDescr{levIdx = 0, skip = 6, take = 3},
        new levelDescr{levIdx = 0, skip = 9},
        new levelDescr{levIdx = 1, take = 3},
        new levelDescr{levIdx = 1, skip = 3, take = 3},
        new levelDescr{levIdx = 1, skip = 6, take = 3},
        new levelDescr{levIdx = 1, skip = 9},
        new levelDescr{levIdx = 2, take = 2},
        new levelDescr{levIdx = 2, skip = 2, take = 2},
        new levelDescr{levIdx = 2, skip = 4, take = 2},
        new levelDescr{levIdx = 2, skip = 6},

      }},
    };

    //public static IEnumerable<productDescrLow> generateProducts(int cnt) {

    //  yield return new productDescrEx() {
    //    course = CourseIds.English,
    //    courseId = cnt++,
    //    title = "English course A1, part 1",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "English_A1_CZU1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "English course A1, start",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, take = 4 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.English,
    //    courseId = cnt++,
    //    title = "English course A1, part 2",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "English_A1_CZU2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "English course A1, part 2",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, skip = 4,  take = 4 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.English,
    //    courseId = cnt++,
    //    title = "English course A1, part 3",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "English_A1_CZU3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "English course A1, part 3",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, skip = 8, take = 4 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.English,
    //    courseId = cnt++,
    //    title = "English course A1, part 4",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "English_A1_CZU4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "English course A1, part 4",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, skip = 12 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.English,
    //    courseId = cnt++,
    //    title = "English course A1, part 1",
    //    _grammarLevels = new int[] { 0 },
    //    _productId = "English_A1_1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "English course A1, start",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 0, take = 8 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.English,
    //    courseId = cnt++,
    //    title = "English course A1, part 2",
    //    _grammarLevels = new int[] { 0 },
    //    _productId = "English_A1_2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "English course A1, part 2",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 0, skip = 8 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.English,
    //    courseId = cnt++,
    //    title = "English course A1, part 3",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "English_A1_3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "English course A1, part 3",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, take = 8 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.English,
    //    courseId = cnt++,
    //    title = "English course A1, part 4",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "English_A1_4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "English course A1, part 4",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, skip = 8 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.English,
    //    courseId = cnt++,
    //    title = "English course A2, part 1",
    //    _grammarLevels = new int[] { 2 },
    //    _productId = "English_A2_1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "English course A2, part 1",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 2, take = 4 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.English,
    //    courseId = cnt++,
    //    title = "English course A2, part 2",
    //    _grammarLevels = new int[] { 2 },
    //    _productId = "English_A2_2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "English course A2, part 2",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 2, skip = 4,  take = 4 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.English,
    //    courseId = cnt++,
    //    title = "English course A2, part 3",
    //    _grammarLevels = new int[] { 2 },
    //    _productId = "English_A2_3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "English course A2, part 3",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 2, skip = 8,  take = 4 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.English,
    //    courseId = cnt++,
    //    title = "English course A2, part 4",
    //    _grammarLevels = new int[] { 2 },
    //    _productId = "English_A2_4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "English course A2, part 4",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 2, skip = 12 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.English,
    //    courseId = cnt++,
    //    title = "English course B1, part 1",
    //    _grammarLevels = new int[] { 3 },
    //    _productId = "English_B1_1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "English course B1, part 1",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 3, take = 4 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.English,
    //    courseId = cnt++,
    //    title = "English course B1, part 2",
    //    _grammarLevels = new int[] { 3 },
    //    _productId = "English_B1_2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "English course B1, part 2",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 3, skip = 4,  take = 4 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.English,
    //    courseId = cnt++,
    //    title = "English course B1, part 3",
    //    _grammarLevels = new int[] { 3 },
    //    _productId = "English_B1_3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "English course B1, part 3",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 3, skip = 8, take = 4 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.English,
    //    courseId = cnt++,
    //    title = "English course B1, part 4",
    //    _grammarLevels = new int[] { 3 },
    //    _productId = "English_B1_4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "English course B1, part 4",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 3, skip = 12 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.English,
    //    courseId = cnt++,
    //    title = "English course B2, part 1",
    //    _grammarLevels = new int[] { 4 },
    //    _productId = "English_B2_1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "English course B2, part 1",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 4, take = 4 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.English,
    //    courseId = cnt++,
    //    title = "English course B2, part 2",
    //    _grammarLevels = new int[] { 4 },
    //    _productId = "English_B2_2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "English course B2, part 2",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 4, skip = 4,  take = 5 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.English,
    //    courseId = cnt++,
    //    title = "English course B2, part 3",
    //    _grammarLevels = new int[] { 4 },
    //    _productId = "English_B2_3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "English course B2, part 3",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 4, skip = 9,  take = 5 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.English,
    //    courseId = cnt++,
    //    title = "English course B2, part 4",
    //    _grammarLevels = new int[] { 4 },
    //    _productId = "English_B2_4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "English course B2, part 4",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 4, skip = 14 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.German,
    //    courseId = cnt++,
    //    title = "Deutschkurs A1, Teil 1",
    //    _grammarLevels = new int[] { 0 },
    //    _productId = "German_A1_1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //          new productPartEx() {
    //            title = "Deutschkurs A1, Teil 1",
    //            levs = new productLessInterval[] {  new productLessInterval() { levIdx = 0, take = 2 }  }
    //          }
    //        }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.German,
    //    courseId = cnt++,
    //    title = "Deutschkurs A1, Teil 2",
    //    _grammarLevels = new int[] { 0 },
    //    _productId = "German_A1_2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //          new productPartEx() {
    //            title = "Deutschkurs A1, Teil 2",
    //            levs = new productLessInterval[] {  new productLessInterval() { levIdx = 0, skip = 2, take = 2 }  }
    //          }
    //        }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.German,
    //    courseId = cnt++,
    //    title = "Deutschkurs A1, Teil 3",
    //    _grammarLevels = new int[] { 0 },
    //    _productId = "German_A1_3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //          new productPartEx() {
    //            title = "Deutschkurs A1, Teil 3",
    //            levs = new productLessInterval[] {  new productLessInterval() { levIdx = 0, skip = 4, take = 2 }  }
    //          }
    //        }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.German,
    //    courseId = cnt++,
    //    title = "Deutschkurs A1, Teil 4",
    //    _grammarLevels = new int[] { 0 },
    //    _productId = "German_A1_4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //          new productPartEx() {
    //            title = "Deutschkurs A1, Teil 4",
    //            levs = new productLessInterval[] {  new productLessInterval() { levIdx = 0, skip = 6, take = 2 }  }
    //          }
    //        }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.German,
    //    courseId = cnt++,
    //    title = "Deutschkurs A2, Teil 1",
    //    _grammarLevels = new int[] { 0 },
    //    _productId = "German_A2_1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //          new productPartEx() {
    //            title = "Deutschkurs A2, Teil 1",
    //            levs = new productLessInterval[] {  new productLessInterval() { levIdx = 0, skip = 8, take = 2 }  }
    //          //modifySiteMap = germanRenameLess
    //}
    //        }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.German,
    //    courseId = cnt++,
    //    title = "Deutschkurs A2, Teil 2",
    //    _grammarLevels = new int[] { 0 },
    //    _productId = "German_A2_2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //          new productPartEx() {
    //            title = "Deutschkurs A2, Teil 2",
    //            levs = new productLessInterval[] {  new productLessInterval() { levIdx = 0, skip = 10 }  }
    //          //modifySiteMap = germanRenameLess
    //}
    //        }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.German,
    //    courseId = cnt++,
    //    title = "Deutschkurs A2, Teil 3",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "German_A2_3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //          new productPartEx() {
    //            title = "Deutschkurs A2, Teil 3",
    //            levs = new productLessInterval[] {  new productLessInterval() { levIdx = 1, take = 2 }  }
    //          //modifySiteMap = germanRenameLess
    //}
    //        }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.German,
    //    courseId = cnt++,
    //    title = "Deutschkurs A2, Teil 4",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "German_A2_4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //          new productPartEx() {
    //            title = "Deutschkurs A2, Teil 4",
    //            levs = new productLessInterval[] {  new productLessInterval() { levIdx = 1, skip = 2, take = 2 }  }
    //          //modifySiteMap = germanRenameLess
    //}
    //        }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.German,
    //    courseId = cnt++,
    //    title = "Deutschkurs B1, Teil 1",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "German_B1_1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //          new productPartEx() {
    //            title = "Deutschkurs B1, Teil 1",
    //            levs = new productLessInterval[] {  new productLessInterval() { levIdx = 1, skip = 4, take = 2 }  }
    //          //modifySiteMap = germanRenameLess
    //}
    //        }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.German,
    //    courseId = cnt++,
    //    title = "Deutschkurs B1, Teil 2",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "German_B1_2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //          new productPartEx() {
    //            title = "Deutschkurs B1, Teil 2",
    //            levs = new productLessInterval[] {  new productLessInterval() { levIdx = 1, skip = 6, take = 2 }  }
    //          //modifySiteMap = germanRenameLess
    //}
    //        }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.German,
    //    courseId = cnt++,
    //    title = "Deutschkurs B1, Teil 3",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "German_B1_3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //          new productPartEx() {
    //            title = "Deutschkurs B1, Teil 3",
    //            levs = new productLessInterval[] {  new productLessInterval() { levIdx = 1, skip = 8, take = 2 }  }
    //          //modifySiteMap = germanRenameLess
    //}
    //        }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.German,
    //    courseId = cnt++,
    //    title = "Deutschkurs B1, Teil 4",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "German_B1_4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //          new productPartEx() {
    //            title = "Deutschkurs B1, Teil 4",
    //            levs = new productLessInterval[] {  new productLessInterval() { levIdx = 1, skip = 10 }  }
    //          //modifySiteMap = germanRenameLess
    //}
    //        }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.German,
    //    courseId = cnt++,
    //    title = "Deutschkurs B2, Teil 1",
    //    _grammarLevels = new int[] { 2 },
    //    _productId = "German_B2_1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //          new productPartEx() {
    //            title = "Deutschkurs B2, Teil 1",
    //            levs = new productLessInterval[] {  new productLessInterval() { levIdx = 2, take = 1 }  }
    //          }
    //        }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.German,
    //    courseId = cnt++,
    //    title = "Deutschkurs B2, Teil 2",
    //    _grammarLevels = new int[] { 2 },
    //    _productId = "German_B2_2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //          new productPartEx() {
    //            title = "Deutschkurs B2, Teil 2",
    //            levs = new productLessInterval[] {  new productLessInterval() { levIdx = 2, skip = 1, take = 1 }  }
    //  }
    //        }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.German,
    //    courseId = cnt++,
    //    title = "Deutschkurs B2, Teil 3",
    //    _grammarLevels = new int[] { 2 },
    //    _productId = "German_B2_3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //          new productPartEx() {
    //            title = "Deutschkurs B2, Teil 3",
    //            levs = new productLessInterval[] {  new productLessInterval() { levIdx = 2, skip = 2, take = 1 }  }
    //      }
    //        }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.German,
    //    courseId = cnt++,
    //    title = "Deutschkurs B2, Teil 4",
    //    _grammarLevels = new int[] { 2 },
    //    _productId = "German_B2_4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //          new productPartEx() {
    //            title = "Deutschkurs B2, Teil 4",
    //            levs = new productLessInterval[] {  new productLessInterval() { levIdx = 2, skip = 3, take = 3 }  }
    //      }
    //        }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Russian,
    //    courseId = cnt++,
    //    title = "Курсы русского A1, часть 1",
    //    _productId = "Russian_A1_1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Курсы русского A1, часть 1",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 0, take = 2 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Russian,
    //    courseId = cnt++,
    //    title = "Курсы русского A1, часть 2",
    //    _productId = "Russian_A1_2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Курсы русского A1, часть 2",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 0, skip = 2,  take = 2 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Russian,
    //    courseId = cnt++,
    //    title = "Курсы русского A1, часть 3",
    //    _productId = "Russian_A1_3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Курсы русского A1, часть 3",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 0, skip = 4, take = 2 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Russian,
    //    courseId = cnt++,
    //    title = "Курсы русского A1, часть 4",
    //    _productId = "Russian_A1_4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Курсы русского A1, часть 4",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 0, skip = 6 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Russian,
    //    courseId = cnt++,
    //    title = "Курсы русского A2, часть 1",
    //    _productId = "Russian_A2_1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Курсы русского A2, часть 1",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, take = 2 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Russian,
    //    courseId = cnt++,
    //    title = "Курсы русского A2, часть 2",
    //    _productId = "Russian_A2_2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Курсы русского A2, часть 2",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, skip = 2,  take = 2 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Russian,
    //    courseId = cnt++,
    //    title = "Курсы русского A2, часть 3",
    //    _productId = "Russian_A2_3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Курсы русского A2, часть 3",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, skip = 4, take = 2 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Russian,
    //    courseId = cnt++,
    //    title = "Курсы русского A2, часть 4",
    //    _productId = "Russian_A2_4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Курсы русского A2, часть 4",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, skip = 6 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Russian,
    //    courseId = cnt++,
    //    title = "Курсы русского B1, часть 1",
    //    _productId = "Russian_B1_1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Курсы русского B1, часть 1",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 2, take = 2 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Russian,
    //    courseId = cnt++,
    //    title = "Курсы русского B1, часть 2",
    //    _productId = "Russian_B1_2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Курсы русского B1, часть 2",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 2, skip = 2,  take = 3 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Russian,
    //    courseId = cnt++,
    //    title = "Курсы русского B1, часть 3",
    //    _productId = "Russian_B1_3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Курсы русского B1, часть 3",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 2, skip = 5, take = 2 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Russian,
    //    courseId = cnt++,
    //    title = "Курсы русского B1, часть 4",
    //    _productId = "Russian_B1_4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Курсы русского B1, часть 4",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 2, skip = 7 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrNew() {
    //    course = CourseIds.Russian,
    //    courseId = cnt++,
    //    title = "Курсы русского B2, часть 1",
    //    dataPath = "russian4",
    //    _productId = "Russian_B2_1".ToLower(),
    //    _locs = new Langs[] { Langs.cs_cz },
    //    designFormat = DesignFormat.word,
    //    _productParts = new productPartNew[] {
    //          new productPartNew() {
    //            title = "Курсы русского B2, часть 1",
    //            refreshLessonsEx = () => task.newEALessons("russian4").Skip (0).Take(2).ToArray(), //dynamicke kvuli refresh pri buildu
    //          }
    //        }
    //  };
    //  yield return new productDescrNew() {
    //    course = CourseIds.Russian,
    //    courseId = cnt++,
    //    title = "Курсы русского B2, часть 2",
    //    dataPath = "russian4",
    //    _productId = "Russian_B2_2".ToLower(),
    //    _locs = new Langs[] { Langs.cs_cz },
    //    designFormat = DesignFormat.word,
    //    _productParts = new productPartNew[] {
    //          new productPartNew() {
    //            title = "Курсы русского B2, часть 2",
    //            refreshLessonsEx = () => task.newEALessons("russian4").Skip (2).Take(2).ToArray(), //dynamicke kvuli refresh pri buildu
    //          }
    //        }
    //  };
    //  yield return new productDescrNew() {
    //    course = CourseIds.Russian,
    //    courseId = cnt++,
    //    title = "Курсы русского B2, часть 3",
    //    dataPath = "russian4",
    //    _productId = "Russian_B2_3".ToLower(),
    //    _locs = new Langs[] { Langs.cs_cz },
    //    designFormat = DesignFormat.word,
    //    _productParts = new productPartNew[] {
    //          new productPartNew() {
    //            title = "Курсы русского B2, часть 3",
    //            refreshLessonsEx = () => task.newEALessons("russian4").Skip (4).Take(3).ToArray(), //dynamicke kvuli refresh pri buildu
    //          }
    //        }
    //  };
    //  yield return new productDescrNew() {
    //    course = CourseIds.Russian,
    //    courseId = cnt++,
    //    title = "Курсы русского B2, часть 4",
    //    dataPath = "russian4",
    //    _productId = "Russian_B2_4".ToLower(),
    //    _locs = new Langs[] { Langs.cs_cz },
    //    designFormat = DesignFormat.word,
    //    _productParts = new productPartNew[] {
    //          new productPartNew() {
    //            title = "Курсы русского B2, часть 4",
    //            refreshLessonsEx = () => task.newEALessons("russian4").Skip (7).Take(3).ToArray(), //dynamicke kvuli refresh pri buildu
    //          }
    //        }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Italian,
    //    courseId = cnt++,
    //    title = "Corso di Italiano A1, parte 1",
    //    _grammarLevels = new int[] { 0 },
    //    _productId = "Italian_A1_1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Corso di Italiano A1, parte 1",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 0, take = 3 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Italian,
    //    courseId = cnt++,
    //    title = "Corso di Italiano A1, parte 2",
    //    _grammarLevels = new int[] { 0 },
    //    _productId = "Italian_A1_2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Corso di Italiano A1, parte 2",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 0, skip = 3,  take = 2 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Italian,
    //    courseId = cnt++,
    //    title = "Corso di Italiano A1, parte 3",
    //    _grammarLevels = new int[] { 0 },
    //    _productId = "Italian_A1_3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Corso di Italiano A1, parte 3",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 0, skip = 5, take = 2 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Italian,
    //    courseId = cnt++,
    //    title = "Corso di Italiano A1, parte 4",
    //    _grammarLevels = new int[] { 0 },
    //    _productId = "Italian_A1_4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Corso di Italiano A1, parte 4",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 0, skip = 7 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Italian,
    //    courseId = cnt++,
    //    title = "Corso di Italiano A2, parte 1",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "Italian_A2_1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Corso di Italiano A2, parte 1",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, take = 3 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Italian,
    //    courseId = cnt++,
    //    title = "Corso di Italiano A2, parte 2",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "Italian_A2_2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Corso di Italiano A2, parte 2",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, skip = 3,  take = 3 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Italian,
    //    courseId = cnt++,
    //    title = "Corso di Italiano A2, parte 3",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "Italian_A2_3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Corso di Italiano A2, parte 3",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, skip = 6, take = 2 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Italian,
    //    courseId = cnt++,
    //    title = "Corso di Italiano A2, parte 4",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "Italian_A2_4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Corso di Italiano A2, parte 4",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, skip = 8 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Italian,
    //    courseId = cnt++,
    //    title = "Corso di Italiano B1, parte 1",
    //    _grammarLevels = new int[] { 2 },
    //    _productId = "Italian_B1_1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Corso di Italiano B1, parte 1",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 2, take = 2 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Italian,
    //    courseId = cnt++,
    //    title = "Corso di Italiano B1, parte 2",
    //    _grammarLevels = new int[] { 2 },
    //    _productId = "Italian_B1_2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Corso di Italiano B1, parte 2",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 2, skip = 2,  take = 2 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Italian,
    //    courseId = cnt++,
    //    title = "Corso di Italiano B1, parte 3",
    //    _grammarLevels = new int[] { 2 },
    //    _productId = "Italian_B1_3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Corso di Italiano B1, parte 3",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 2, skip = 4, take = 3 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Italian,
    //    courseId = cnt++,
    //    title = "Corso di Italiano B1, parte 4",
    //    _grammarLevels = new int[] { 2 },
    //    _productId = "Italian_B1_4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Corso di Italiano B1, parte 4",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 2, skip = 7 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Spanish,
    //    courseId = cnt++,
    //    title = "Curso de español A1, parte 1",
    //    _grammarLevels = new int[] { 0 },
    //    _productId = "Spanish_A1_1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Curso de español A1, parte 1",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 0, take = 4 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Spanish,
    //    courseId = cnt++,
    //    title = "Curso de español A1, parte 2",
    //    _grammarLevels = new int[] { 0 },
    //    _productId = "Spanish_A1_2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Curso de español A1, parte 2",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 0, skip = 4,  take = 4 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Spanish,
    //    courseId = cnt++,
    //    title = "Curso de español A1, parte 3",
    //    _grammarLevels = new int[] { 0 },
    //    _productId = "Spanish_A1_3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Curso de español A1, parte 3",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 0, skip = 8, take = 5 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Spanish,
    //    courseId = cnt++,
    //    title = "Curso de español A1, parte 4",
    //    _grammarLevels = new int[] { 0 },
    //    _productId = "Spanish_A1_4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Curso de español A1, parte 4",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 0, skip = 13 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Spanish,
    //    courseId = cnt++,
    //    title = "Curso de español A2, parte 1",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "Spanish_A2_1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Curso de español A2, parte 1",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, take = 4 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Spanish,
    //    courseId = cnt++,
    //    title = "Curso de español A2, parte 2",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "Spanish_A2_2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Curso de español A2, parte 2",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, skip = 4,  take = 4 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Spanish,
    //    courseId = cnt++,
    //    title = "Curso de español A2, parte 3",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "Spanish_A2_3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Curso de español A2, parte 3",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, skip = 8, take = 3 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Spanish,
    //    courseId = cnt++,
    //    title = "Curso de español A2, parte 4",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "Spanish_A2_4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Curso de español A2, parte 4",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, skip = 11 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Spanish,
    //    courseId = cnt++,
    //    title = "Curso de español B1, parte 1",
    //    _grammarLevels = new int[] { 2 },
    //    _productId = "Spanish_B1_1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Curso de español B1, parte 1",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 2, take = 2 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Spanish,
    //    courseId = cnt++,
    //    title = "Curso de español B1, parte 2",
    //    _grammarLevels = new int[] { 2 },
    //    _productId = "Spanish_B1_2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Curso de español B1, parte 2",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 2, skip = 2,  take = 2 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Spanish,
    //    courseId = cnt++,
    //    title = "Curso de español B1, parte 3",
    //    _grammarLevels = new int[] { 2 },
    //    _productId = "Spanish_B1_3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Curso de español B1, parte 3",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 2, skip = 4, take = 2 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.Spanish,
    //    courseId = cnt++,
    //    title = "Curso de español B1, parte 4",
    //    _grammarLevels = new int[] { 2 },
    //    _productId = "Spanish_B1_4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Curso de español B1, parte 4",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 2, skip = 6 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.French,
    //    courseId = cnt++,
    //    title = "Cours de français A1, partie 1",
    //    _grammarLevels = new int[] { 0 },
    //    _productId = "French_A1_1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Cours de français A1, partie 1",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 0, take = 3 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.French,
    //    courseId = cnt++,
    //    title = "Cours de français A1, partie 2",
    //    _grammarLevels = new int[] { 0 },
    //    _productId = "French_A1_2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Cours de français A1, partie 2",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 0, skip = 3,  take = 3 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.French,
    //    courseId = cnt++,
    //    title = "Cours de français A1, partie 3",
    //    _grammarLevels = new int[] { 0 },
    //    _productId = "French_A1_3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Cours de français A1, partie 3",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 0, skip = 6, take = 3 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.French,
    //    courseId = cnt++,
    //    title = "Cours de français A1, partie 4",
    //    _grammarLevels = new int[] { 0 },
    //    _productId = "French_A1_4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Cours de français A1, partie 4",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 0, skip = 9 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.French,
    //    courseId = cnt++,
    //    title = "Cours de français A2, partie 1",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "French_A2_1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Cours de français A2, partie 1",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, take = 3 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.French,
    //    courseId = cnt++,
    //    title = "Cours de français A2, partie 2",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "French_A2_2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Cours de français A2, partie 2",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, skip = 3,  take = 3 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.French,
    //    courseId = cnt++,
    //    title = "Cours de français A2, partie 3",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "French_A2_3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Cours de français A2, partie 3",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, skip = 6, take = 3 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.French,
    //    courseId = cnt++,
    //    title = "Cours de français A2, partie 4",
    //    _grammarLevels = new int[] { 1 },
    //    _productId = "French_A2_4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Cours de français A2, partie 4",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 1, skip = 9 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.French,
    //    courseId = cnt++,
    //    title = "Cours de français B1, partie 1",
    //    _grammarLevels = new int[] { 2 },
    //    _productId = "French_B1_1",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Cours de français B1, partie 1",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 2, take = 2 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.French,
    //    courseId = cnt++,
    //    title = "Cours de français B1, partie 2",
    //    _grammarLevels = new int[] { 2 },
    //    _productId = "French_B1_2",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Cours de français B1, partie 2",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 2, skip = 2,  take = 2 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.French,
    //    courseId = cnt++,
    //    title = "Cours de français B1, partie 3",
    //    _grammarLevels = new int[] { 2 },
    //    _productId = "French_B1_3",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Cours de français B1, partie 3",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 2, skip = 4, take = 2 } }
    //        }
    //      }
    //  };
    //  yield return new productDescrEx() {
    //    course = CourseIds.French,
    //    courseId = cnt++,
    //    title = "Cours de français B1, partie 4",
    //    _grammarLevels = new int[] { 2 },
    //    _productId = "French_B1_4",
    //    _locs = new Langs[] { Langs.cs_cz },
    //    setProductParts = new productPartEx[] {
    //        new productPartEx() {
    //          title = "Cours de français B1, partie 4",
    //          levs = new productLessInterval[] { new productLessInterval() { levIdx = 2, skip = 6 } }
    //        }
    //      }
    //  };
    //}
  }

}
