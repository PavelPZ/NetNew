using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Migrations;
using Microsoft.Data.Entity.Metadata;

namespace DataLib2.Migrations.Vyzva57Context_SqlServerMigrations
{
    public partial class vyzvaserv001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlendedCompany",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false),
                    LearningData = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlendedCompany", x => x.Id);
                });
            migrationBuilder.CreateTable(
                name: "BlendedCourseUser",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CompanyId = table.Column<int>(nullable: false),
                    LMComId = table.Column<long>(nullable: false),
                    ProductUrl = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlendedCourseUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlendedCourseUser_BlendedCompany_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "BlendedCompany",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateTable(
                name: "BlendedCourseData",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CourseUserId = table.Column<int>(nullable: false),
                    Data = table.Column<string>(nullable: true),
                    Flags = table.Column<long>(nullable: false),
                    Key = table.Column<string>(nullable: false),
                    ShortData = table.Column<string>(nullable: true),
                    TaskId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlendedCourseData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlendedCourseData_BlendedCourseUser_CourseUserId",
                        column: x => x.CourseUserId,
                        principalTable: "BlendedCourseUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateIndex(
                name: "IX_BlendedCourseData_Key",
                table: "BlendedCourseData",
                column: "Key");
            migrationBuilder.CreateIndex(
                name: "IX_BlendedCourseData_TaskId",
                table: "BlendedCourseData",
                column: "TaskId");
            migrationBuilder.CreateIndex(
                name: "IX_BlendedCourseUser_LMComId",
                table: "BlendedCourseUser",
                column: "LMComId");
            migrationBuilder.CreateIndex(
                name: "IX_BlendedCourseUser_ProductUrl",
                table: "BlendedCourseUser",
                column: "ProductUrl");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("BlendedCourseData");
            migrationBuilder.DropTable("BlendedCourseUser");
            migrationBuilder.DropTable("BlendedCompany");
        }
    }
}
