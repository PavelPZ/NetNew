using Microsoft.Data.Entity;
using System;
using System.Configuration;
using System.Data.SqlClient;

namespace TradosDT {

  public class TradosDB : DbContext {
    public TradosDB() : base() {
      Database.EnsureCreated();
      Database.SetCommandTimeout(1000000);
    }

    public static TradosDB getTradosContext() {
      return new TradosDB();
    }
    public static TradosDB getTradosContext(bool updatable) {
      return new TradosDB();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options) {
      var config = ConfigurationManager.ConnectionStrings["TradosDT"];
      var conn = new SqlConnection(config.ConnectionString);
      options.UseSqlServer(conn);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
      modelBuilder.Entity<Lock>(entity => {
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).HasColumnName("Id").IsRequired().HasColumnType("int");//.HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        entity.Property(x => x.Created).HasColumnName("Created").IsRequired().HasColumnType("datetime");
        entity.Property(x => x.Lang).HasColumnName("Lang").IsRequired().HasColumnType("smallint");
        entity.Property(x => x.PageGroup).HasColumnName("PageGroup").IsRequired().HasColumnType("smallint");
        entity.Property(x => x.Locked).HasColumnName("Locked").IsRequired().HasColumnType("bit");

      });
      modelBuilder.Entity<Lookup>(entity => {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id).HasColumnName("Id").IsRequired().HasColumnType("int");//.HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        entity.Property(x => x.SrcLang).HasColumnName("SrcLang").IsRequired().HasColumnType("smallint");
        entity.Property(x => x.SrcText).HasColumnName("SrcText").IsRequired().HasColumnType("nvarchar");
        entity.Property(x => x.SrcHash).HasColumnName("SrcHash").IsRequired().HasColumnType("int");
        entity.Property(x => x.SrcLen).HasColumnName("SrcLen").IsRequired().HasColumnType("int");
        entity.Property(x => x.TransLang).HasColumnName("TransLang").IsRequired().HasColumnType("smallint");
        entity.Property(x => x.TransText).HasColumnName("TransText").IsRequired().HasColumnType("nvarchar");
        entity.Property(x => x.TransHash).HasColumnName("TransHash").IsRequired().HasColumnType("int");
        entity.Property(x => x.TransLen).HasColumnName("TransLen").IsRequired().HasColumnType("int");
        entity.Property(x => x.Hash).HasColumnName("Hash").IsRequired().HasColumnType("int");

        entity.HasIndex(c => new { c.SrcLang, c.TransLang, c.SrcHash });
      });
      modelBuilder.Entity<Page>(entity => {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id).HasColumnName("Id").IsRequired().HasColumnType("int");//.HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        entity.Property(x => x.FileName).HasColumnName("FileName").IsRequired().HasColumnType("varchar").HasMaxLength(256);
        entity.Property(x => x.PageGroup).HasColumnName("PageGroup").IsRequired().HasColumnType("smallint");
        entity.Property(x => x.SeeUrl).HasColumnName("SeeUrl").HasColumnType("varchar").HasMaxLength(256);
        entity.Property(x => x.Langs).HasColumnName("Langs").HasColumnType("varchar").HasMaxLength(256);
      });
      modelBuilder.Entity<Sentence>(entity => {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id).HasColumnName("Id").IsRequired().HasColumnType("int");//.HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        entity.Property(x => x.Name).HasColumnName("Name").IsRequired().HasColumnType("varchar").HasMaxLength(128);
        entity.Property(x => x.PageId).HasColumnName("PageId").IsRequired().HasColumnType("int");
        entity.Property(x => x.SrcLang).HasColumnName("SrcLang").IsRequired().HasColumnType("smallint");
        entity.Property(x => x.SrcText).HasColumnName("SrcText").HasColumnType("nvarchar");
        entity.Property(x => x.SrcHash).HasColumnName("SrcHash").IsRequired().HasColumnType("int");
        entity.Property(x => x.SrcLen).HasColumnName("SrcLen").IsRequired().HasColumnType("int");
        entity.Property(x => x.TransLang).HasColumnName("TransLang").IsRequired().HasColumnType("smallint");
        entity.Property(x => x.TransText).HasColumnName("TransText").HasColumnType("nvarchar");
        entity.Property(x => x.TransHash).HasColumnName("TransHash").IsRequired().HasColumnType("int");
        entity.Property(x => x.TransLen).HasColumnName("TransLen").IsRequired().HasColumnType("int");
        entity.Property(x => x.Hash).HasColumnName("Hash").IsRequired().HasColumnType("int");

        entity.HasOne(d => d.Page).WithMany(p => p.Sentences).HasForeignKey(d => d.PageId);
        entity.HasIndex(c => new { c.SrcLang, c.TransLang, c.SrcHash });
        entity.HasIndex(c => new { c.PageId, c.TransLang, c.Name });
      });
    }

    public DbSet<Lock> Locks { get; set; } // Lock
    public DbSet<Lookup> Lookups { get; set; } // Lookup
    public DbSet<Page> Pages { get; set; } // Pages
    public DbSet<Sentence> Sentences { get; set; } // Sentence
  }
}
