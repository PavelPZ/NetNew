//https://github.com/aspnet/EntityFramework/wiki/Design-Meeting-Notes---July-23,-2015
//Install-Package EntityFramework.Commands -Pre 
using Microsoft.Data.Entity;
using System.Configuration;
using System.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace NewData {

  public class Vyzva57Context_Sqlite : Vyzva57Context {

    //Add-Migration vyzva-sqlite-001 -c NewData.Vyzva57Context_Sqlite
    //Update-Database vyzva-sqlite-001 -c NewData.Vyzva57Context_Sqlite
    protected override void OnConfiguring(DbContextOptionsBuilder options) {
      base.OnConfiguring(options);
      var config = ConfigurationManager.ConnectionStrings["Vyzva57-sqlite"];
      var conn = new SqliteConnection(config.ConnectionString);
      //var conn = new SqliteConnection("Data Source=" + MachinesLow.rootDir +  config.ConnectionString);
      options.UseSqlite(conn);
    }
  }

  //Add-Migration vyzva-serv-001 -c NewData.Vyzva57Context_SqlServer
  //Update-Database vyzva-serv-001 -c NewData.Vyzva57Context_SqlServer
  public class Vyzva57Context_SqlServer : Vyzva57Context {
    protected override void OnConfiguring(DbContextOptionsBuilder options) {
      var config = ConfigurationManager.ConnectionStrings["Vyzva57"];
      var conn = new SqlConnection(config.ConnectionString);
      options.UseSqlServer(conn);
    }
  }

  public class Vyzva57Context : DbContext {

    public Vyzva57Context(): base() {
      Database.EnsureCreated();
    }

    public static Vyzva57Context CreateContext() {
      //return new Vyzva57Context_SqlServer();
      return new Vyzva57Context_Sqlite();
    }

    public static void SaveChanges(Vyzva57Context db) {
      db.SaveChanges();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
      modelBuilder.Entity<BlendedCompany>(entity => {
        entity.Property(e => e.Id).ValueGeneratedNever();
      });

      modelBuilder.Entity<BlendedCourseData>(entity => {
        entity.HasIndex(c => c.Key);
        entity.HasIndex(c => c.TaskId);
        entity.Property(e => e.CourseUserId).IsRequired();
        entity.Property(e => e.Flags).IsRequired();
        entity.Property(e => e.Key).IsRequired().HasMaxLength(240);
        entity.Property(e => e.TaskId).HasMaxLength(32);
        entity.HasOne(d => d.CourseUser).WithMany(p => p.CourseDatas).HasForeignKey(d => d.CourseUserId);
      });

      modelBuilder.Entity<BlendedCourseUser>(entity => {
        entity.HasIndex(c => c.ProductUrl);
        entity.HasIndex(c => c.LMComId);
        entity.Property(e => e.CompanyId).IsRequired();
        entity.Property(e => e.LMComId).IsRequired();
        entity.Property(e => e.ProductUrl).IsRequired().HasMaxLength(120);
        entity.HasOne(d => d.Company).WithMany(p => p.CourseUsers).HasForeignKey(d => d.CompanyId);
      });

    }

    public virtual DbSet<BlendedCompany> BlendedCompanies { get; set; }
    public virtual DbSet<BlendedCourseData> BlendedCourseDatas { get; set; }
    public virtual DbSet<BlendedCourseUser> BlendedCourseUsers { get; set; }
  }
}