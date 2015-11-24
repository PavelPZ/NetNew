using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations;
using NewData;

namespace DataLib2.Migrations.Vyzva57Context_SqlServerMigrations
{
    [DbContext(typeof(Vyzva57Context_SqlServer))]
    [Migration("20151123201728_vyzva-serv-001")]
    partial class vyzvaserv001
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.0-rc1-16348")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("NewData.BlendedCompany", b =>
                {
                    b.Property<int>("Id");

                    b.Property<string>("LearningData");

                    b.HasKey("Id");
                });

            modelBuilder.Entity("NewData.BlendedCourseData", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("CourseUserId");

                    b.Property<string>("Data");

                    b.Property<long>("Flags");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasAnnotation("MaxLength", 240);

                    b.Property<string>("ShortData");

                    b.Property<string>("TaskId")
                        .HasAnnotation("MaxLength", 32);

                    b.HasKey("Id");

                    b.HasIndex("Key");

                    b.HasIndex("TaskId");
                });

            modelBuilder.Entity("NewData.BlendedCourseUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("CompanyId");

                    b.Property<long>("LMComId");

                    b.Property<string>("ProductUrl")
                        .IsRequired()
                        .HasAnnotation("MaxLength", 120);

                    b.HasKey("Id");

                    b.HasIndex("LMComId");

                    b.HasIndex("ProductUrl");
                });

            modelBuilder.Entity("NewData.BlendedCourseData", b =>
                {
                    b.HasOne("NewData.BlendedCourseUser")
                        .WithMany()
                        .HasForeignKey("CourseUserId");
                });

            modelBuilder.Entity("NewData.BlendedCourseUser", b =>
                {
                    b.HasOne("NewData.BlendedCompany")
                        .WithMany()
                        .HasForeignKey("CompanyId");
                });
        }
    }
}
