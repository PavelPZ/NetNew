using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace testMe {

  //predpis na vytvoreni testu
  //public enum Skills : int {
  //  no = 0, UseLanguage = 1, Reading = 2, Listening = 3, Speaking = 4, Writing = 5,
  //}

  //predpis na vytvoreni testu
  public static class Skills {
    public const string no = "no";
    public const string UseLanguage = "UseLanguage";
    public const string UseEnglish = "UseEnglish"; //obsolete
    public const string Reading = "Reading";
    public const string Listening = "Listening";
    public const string Speaking = "Speaking";
    public const string Writing = "Writing";
    public static string[] all = new string[] { UseLanguage, Reading, Listening, Speaking, Writing };
    public static string[] std = new string[] { UseLanguage, Reading, Listening };
    public static string[] stdOld = new string[] { UseEnglish, Reading, Listening };
  }

  //user data
  public enum Status {
    no = 0,
    Started = 1, //test zapocat
    Interrupted = 2, //pro history: test prerusen
    SendedToEvaluation = 3, //hotovo, ceka na vyhodnoceni
    EvalAssigned = 4, //po SendedToEvaluation: v HighSchool konzoli prirazeno tutorovi
    Evaluated = 5, //po SendedToEvaluation: vyhodnoceno
  }

  public class userData {
    public int started;
    public string ip; //IP adresa v dobe vytvorenu userData
    public interrupt[] interrupts; //evidence preruseni
  }

  public class multiUserData {
    public string level; //pro multilevel test, zvolena uroven
  }

  public class skillUserData {
    public string[] modUrls;
    public int started;
    public int finished;
    public int elapsed;
  }

  public class interrupt {
    public int beg; //maximum z end vsech cviceni testu. Neboli datum posledni prace s testem, tj zacatek preruseni.
    public int end; //DateTime.now, neboli datum dalsiho vstupu do testu, neboli konec preruseni.
    public string ip; //IP adresa v dobe vytvoreni preruseni
    //[XmlIgnore, JsonIgnore]
    //public ushort reportNum;
  }

  public class result {
    public string domain; //JS Pager.basicDir, bez protokolu. Urcuje aplikaci a tedy i databazi, napr. test.langmaster.com/alpha
    public int id; //dbo.CourseUsers.Id, jednoznacne identifikuje test v databazi
    public string title; //nazev testu, title test node v sitemap
    public string company; //dbo.Companies.Title
    public string firstName;
    public string lastName;
    public string eMail;
    public skillResult[] skills; //skills info
    public string ip; //IP adresa v dobe vytvorenu userData
    public interrupt[] interrupts; //evidence preruseni
    //public int? score; //celkove score
    //public int ms; public int s;
    public int score; //() { return ms == 0 ? 0 : s * 100 / ms; }
    public CourseModel.CourseDataFlag flag;
    public int companyId;
    public string productUrl;
    public Int64 lmcomId;
    public string level; //A1, ...
  }

  public class skillResult : CourseModel.Score {
    public string skill;
    public string title; //title testSkillImpl node v sitemap
    //public int score; //() { return ms == 0 ? 0 : s * 100 / ms; }
    //public int ms; public int s;
    public int scoreWeight; //vaha score v procentech
    public int started; //datum zacatku skill
    public int finished; //datum ukonceni skill
    public int elapsed; //cas straveny testem

    public int score() { return ms == 0 ? -1 : (int)Math.Round((double)s / ms * 100); }
  }

}