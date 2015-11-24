using System;
using System.Collections.Generic;

namespace TradosDT {
  public partial class Lock {
    public int Id { get; set; } // Id (Primary key)
    public DateTime Created { get; set; } // Created
    public short Lang { get; set; } // Lang
    public short PageGroup { get; set; } // PageGroup
    public bool Locked { get; set; } // Locked
  }

  // Lookup
  public partial class Lookup {
    public Lookup() {
      Hash = 0;
    }
    public int Id { get; set; } // Id (Primary key)
    public short SrcLang { get; set; } // SrcLang
    public string SrcText { get; set; } // SrcText
    public int SrcHash { get; set; } // SrcHash
    public int SrcLen { get; set; } // SrcLen
    public short TransLang { get; set; } // TransLang
    public string TransText { get; set; } // TransText
    public int TransHash { get; set; } // TransHash
    public int TransLen { get; set; } // TransLen
    public int Hash { get; set; } // Hash

  }

  // Pages
  public partial class Page {
    public Page() {
      Sentences = new HashSet<Sentence>();
    }
    public int Id { get; set; } // Id (Primary key)
    public string FileName { get; set; } // FileName
    public short PageGroup { get; set; } // PageGroup
    public string SeeUrl { get; set; } // SeeUrl
    public string Langs { get; set; } // Langs

    // Reverse navigation
    public virtual ICollection<Sentence> Sentences { get; set; } // Sentence.FK_Sentence_Page

  }

  // Sentence
  public partial class Sentence {
    public int Id { get; set; } // Id (Primary key)
    public string Name { get; set; } // Name
    public int PageId { get; set; } // PageId
    public short SrcLang { get; set; } // SrcLang
    public string SrcText { get; set; } // SrcText
    public int SrcHash { get; set; } // SrcHash
    public int SrcLen { get; set; } // SrcLen
    public short TransLang { get; set; } // TransLang
    public string TransText { get; set; } // TransText
    public int TransHash { get; set; } // TransHash
    public int TransLen { get; set; } // TransLen
    public int Hash { get; set; } // Hash

    // Foreign keys
    public virtual Page Page { get; set; } // FK_Sentence_Page
  }
}