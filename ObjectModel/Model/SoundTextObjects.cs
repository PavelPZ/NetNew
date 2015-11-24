using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using LMNetLib;
using LMComLib;

namespace Rewise {

  /**************************************************
     layout
   *************************************************/
  public abstract partial class SoundLow : IParented {
    [XmlIgnore]
    public IParented MyParent;
    public IParented getParent() { return MyParent; }
    public abstract IEnumerable<IParented> getChilds();

    public static void setLayoutParent(IParented self) { foreach (SoundLow l in self.getChilds().OfType<SoundLow>()) { l.MyParent = self; setLayoutParent(l); } }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class Dialog : SoundLow, IParented {
    public Replica[] Replicas { get; set; }

    public override IEnumerable<IParented> getChilds() { return Replicas.OfType<IParented>(); }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class Replica : Block, IParented {
    public RoleId Id { get; set; }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class Block : SoundLow, IParented {
    public Paragraph[] Paragraphs { get; set; }

    public override IEnumerable<IParented> getChilds() { return Paragraphs.OfType<IParented>(); }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class Paragraph : SoundLow, IParented {
    public Sentence[] Sentences { get; set; }

    public override IEnumerable<IParented> getChilds() { return Sentences.OfType<IParented>(); }
  }

  /**************************************************
     prehravani zvuku
   *************************************************/
  public class PlayItem : SoundLow, IParented { //jeden souvisly usek zvuku
    public List<Sentence> Sentences = new List<Sentence>();
    [XmlIgnore]
    public SoundFile File;

    public override IEnumerable<IParented> getChilds() { return Sentences.OfType<IParented>(); }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public class Page : SoundLow, IParented { //vsechen zvuk na strance

    public SoundFile[] SoundFiles;

    [XmlArrayItem(typeof(Dialog)), XmlArrayItem(typeof(Paragraph)), XmlArrayItem(typeof(Block))]
    public SoundLow[] Layouts;

    public override IEnumerable<IParented> getChilds() { return Layouts.OfType<IParented>(); }

    [XmlIgnore]
    public List<PlayItem> PlayPlan;

    public void Finish() {
      if (isFinished) return; isFinished = true;
      SoundLow.setLayoutParent(this); //dosad MyParent pointer do SoundLow tree
      foreach (SoundFile sf in SoundFiles) sf.MyPage = this; //SoundFile parents:
      PlayPlan = new List<PlayItem>();
      expandPointers(); //nahrada pointer Sentences sentencemi z SoundFile
      createPlayItems(); //vytvoreni linearni sekvence PlayItems
    } bool isFinished;

    void createPlayItems() {
      Guid lastFile = Guid.Empty; int lastIdx = -1;
      PlayItem lastPlan = null;
      foreach (Paragraph par in Parented.Nodes<Paragraph>(this))
        foreach (Sentence sent in par.Sentences) {
          if (sent.MyPlayItem != null) throw new Exception("More than one sentence occurence in page layout");
          sent.MyParagraph = par;
          if (sent is SoundFile) {
            PlayPlan.Add(lastPlan = new PlayItem() { MyParent = this, File = (SoundFile)sent });
            sent.MyPlayItem = lastPlan;
            lastPlan.Sentences.Add(sent);
            if (sent.EndPos == 0) sent.EndPos = int.MaxValue;
            lastFile = ((SoundFile)sent).FileId; lastIdx = 0;
          } else {
            var sf = SoundFiles.Where(f => f.Sentences != null).Select(f => new { file = f, idx = Array.IndexOf<Sentence>(f.Sentences, sent) }).Where(fi => fi.idx >= 0).First();
            if (sf.file.FileId == lastFile && sf.idx == lastIdx + 1) {
              lastPlan.Sentences.Add(sent); lastIdx = sf.idx;
            } else {
              PlayPlan.Add(lastPlan = new PlayItem() { MyParent = this, File = sf.file });
              sent.MyPlayItem = lastPlan;
              lastPlan.Sentences.Add(sent);
              lastFile = sf.file.FileId; lastIdx = sf.idx;
            }
            sent.MyPlayItem = lastPlan;
          }
        }
    }

    void expandPointers() {
      foreach (Sentence sent in Parented.Nodes<Sentence>(this)) sent.EndPos = Math.Max(sent.BegPos, sent.EndPos);
      foreach (Paragraph par in Parented.Nodes<Paragraph>(this))
        par.Sentences = par.Sentences.SelectMany(s => Enumerable.Range(s.BegPos, s.EndPos - s.BegPos + 1).Select(idx => findSentence(s.Text, idx))).ToArray();
    }

    Sentence findSentence(string fileId, int sentIdx) {
      SoundFile sf;
      if (string.IsNullOrEmpty(fileId)) sf = SoundFiles[0];
      else {
        Guid g = new Guid(fileId);
        sf = SoundFiles.First(f => f.FileId == g);
      }
      return sentIdx == -1 ? sf : sf.Sentences[sentIdx];
    }

  }

  /**************************************************
     Zvukove zdroje
   *************************************************/
  public enum SoundFileBasicPath {
    weekDialogs,
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class SoundFile : Sentence, IParented { //Nastrihany zvukovy soubor
    public Guid FileId; //identifikace file
    public string Url { get; set; }
    public Sentence[] Sentences;
    public SoundFileBasicPath BasicPath;

    [XmlIgnore]
    public Page MyPage;

    public override IParented getParent() { return MyPage; }
    public override IEnumerable<IParented> getChilds() { return Sentences.OfType<IParented>(); }
  }

  [XmlRoot(Namespace = CommonLib.OLIUrl)]
  public partial class Sentence : Word, IParented {
    public int BegPos;
    [DefaultValueAttribute(0)]
    public int EndPos;
    public string Encoded {
      get {
        return words().Select(w => w.Text).Aggregate((r, i) => r + " " + i);
      }
      set {
        Words = null; Text = null; if (string.IsNullOrEmpty(value)) return;
        var parts = value.Split(' ').Select(w => w.Trim()).Where(w => !string.IsNullOrEmpty(w)).ToArray();
        //string[] CourseTree = value.Split('|'); 
        Text = parts.First();
        Words = parts.Skip(1).Select(w => new Word() { Text = w, MySentence = this }).ToArray();
      }
    }
    [XmlIgnore]
    public Word[] Words;

    [XmlIgnore]
    public PlayItem MyPlayItem;

    [XmlIgnore]
    public Paragraph MyParagraph;

    public IEnumerable<Word> words() { yield return this; if (Words != null) foreach (Word w in Words) yield return w; }

    public override IParented getParent() { return MyParagraph; }
    public override IEnumerable<IParented> getChilds() { return Words == null ? Enumerable.Empty<IParented>() : Words.OfType<IParented>(); }
  }

  public partial class Word : IParented {
    [XmlIgnore]
    public string Text { get; set; }

    [XmlIgnore]
    public Sentence MySentence;

    public virtual IParented getParent() { return MySentence; }
    public virtual IEnumerable<IParented> getChilds() { yield break; }
  }

  /**************************************************
     TestProxy
   *************************************************/
  public static class Test {
    public static void Run() {
      Guid g1 = Guid.NewGuid(); Guid g2 = Guid.NewGuid(); Guid g3 = Guid.NewGuid();
      Page res = new Page() {
        SoundFiles = new SoundFile[] {
          new SoundFile() { FileId = g1,
            Sentences = new Sentence[] {
              new Sentence() {Text = "word1",
                Words = new Word[] {
                  new Word() {Text = "word2"}
                }
              },
              new Sentence() {Text = "word3"},
              new Sentence() {Text = "word4"}
            }
          },
          new SoundFile() {FileId = g2, Text = "word5",
            Words = new Word[] {
              new Word() {Text = "word6"}
            }
          },
          new SoundFile() { FileId = g3,
            Sentences = new Sentence[] {
              new Sentence() {Text = "word7"},
              new Sentence() {Text = "word8"}
            }
          }
        },
        Layouts = new SoundLow[] {
          new Dialog() {
            Replicas = new Replica[] {
              new Replica() {
                Paragraphs = new Paragraph[] {
                  new Paragraph() {
                    Sentences = new Sentence[] {
                      new Sentence() {Text = g1.ToString(), BegPos = 0}
                    }
                  }
                }
              },
              new Replica() {
                Paragraphs = new Paragraph[] {
                  new Paragraph() {
                    Sentences = new Sentence[] {
                      new Sentence() {Text = g1.ToString(), BegPos = 1},
                      new Sentence() {Text = g2.ToString(), BegPos = -1},
                      new Sentence() {Text = g1.ToString(), BegPos = 2},
                    }
                  }
                }
              }
            }
          },
          new Paragraph() {
            Sentences = new Sentence[] {
              new Sentence() {Text = g3.ToString(), BegPos = 0},
              new Sentence() {Text = g3.ToString(), BegPos = 1}
            }
          }
        }
      };
      XmlUtils.ObjectToFile(@"c:\temp\page.xml", res);
      res = XmlUtils.FileToObject<Page>(@"c:\temp\page.xml");
      res.Finish();
      XmlUtils.ObjectToFile(@"c:\temp\page2.xml", res);
    }
  }

}
