using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Migrations;

namespace DataLib2.Migrations.NewLMComContext_SqliteMigrations
{
    public partial class lmcomsqlite001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Created = table.Column<DateTime>(nullable: false),
                    HumanEvalPaymentConfig = table.Column<string>(nullable: true),
                    IntervalsConfig = table.Column<string>(nullable: true),
                    ScormHost = table.Column<string>(nullable: true),
                    Title = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });
            migrationBuilder.CreateTable(
                name: "LANGMasterScorms",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApiUrlCrc = table.Column<int>(nullable: false, defaultValue: 0),
                    AttemptId = table.Column<long>(nullable: false),
                    AttemptIdGuid = table.Column<Guid>(nullable: true),
                    AttemptIdStr = table.Column<string>(nullable: true),
                    Data1 = table.Column<string>(nullable: true),
                    Data2 = table.Column<string>(nullable: true),
                    Date = table.Column<long>(nullable: false),
                    Key1Int = table.Column<long>(nullable: false),
                    Key1Str = table.Column<string>(nullable: true),
                    Key2Int = table.Column<long>(nullable: false),
                    Key2Str = table.Column<string>(nullable: true),
                    UserId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LANGMasterScorms", x => x.Id);
                });
            migrationBuilder.CreateTable(
                name: "CompanyDepartments",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompanyId = table.Column<int>(nullable: false),
                    ParentId = table.Column<int>(nullable: true, defaultValue: 0),
                    Title = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyDepartments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyDepartments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyDepartments_CompanyDepartments_ParentId",
                        column: x => x.ParentId,
                        principalTable: "CompanyDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateTable(
                name: "CompanyLicences",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompanyId = table.Column<int>(nullable: false),
                    Created = table.Column<DateTime>(nullable: false),
                    Days = table.Column<short>(nullable: false),
                    LastCounter = table.Column<int>(nullable: false),
                    ProductId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyLicences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyLicences_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Created = table.Column<DateTime>(nullable: false),
                    EMail = table.Column<string>(nullable: true),
                    FirstName = table.Column<string>(nullable: true),
                    LastName = table.Column<string>(nullable: true),
                    Login = table.Column<string>(nullable: true),
                    LoginEMail = table.Column<string>(nullable: true),
                    MyPublisherId = table.Column<int>(nullable: true),
                    OtherData = table.Column<string>(nullable: true),
                    OtherId = table.Column<string>(nullable: true),
                    OtherType = table.Column<short>(nullable: false),
                    Password = table.Column<string>(nullable: true),
                    Roles = table.Column<long>(nullable: false),
                    VerifyStatus = table.Column<short>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Companies_MyPublisherId",
                        column: x => x.MyPublisherId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateTable(
                name: "CompanyUsers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompanyId = table.Column<int>(nullable: false),
                    Created = table.Column<DateTime>(nullable: false),
                    DepartmentId = table.Column<int>(nullable: true),
                    RolePar = table.Column<string>(nullable: true),
                    Roles = table.Column<long>(nullable: false),
                    UserId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyUsers_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyUsers_CompanyDepartments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "CompanyDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateTable(
                name: "CourseUsers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Created = table.Column<DateTime>(nullable: false),
                    HumanAssigned = table.Column<DateTime>(nullable: false),
                    HumanCompanyUserId = table.Column<int>(nullable: false, defaultValue: 0),
                    ProductId = table.Column<string>(nullable: true),
                    UserId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseUsers_CompanyUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "CompanyUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateTable(
                name: "CourseDatas",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CourseUserId = table.Column<int>(nullable: false),
                    Data = table.Column<string>(nullable: false),
                    Date = table.Column<long>(nullable: false),
                    Flags = table.Column<long>(nullable: false, defaultValue: 0L),
                    Key = table.Column<string>(nullable: false),
                    ShortData = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseDatas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseDatas_CourseUsers_CourseUserId",
                        column: x => x.CourseUserId,
                        principalTable: "CourseUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateTable(
                name: "UserLicences",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Counter = table.Column<int>(nullable: false),
                    Created = table.Column<DateTime>(nullable: false),
                    LicenceId = table.Column<int>(nullable: false),
                    Started = table.Column<DateTime>(nullable: false),
                    UserId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLicences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLicences_CompanyLicences_LicenceId",
                        column: x => x.LicenceId,
                        principalTable: "CompanyLicences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserLicences_CourseUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "CourseUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateIndex(
                name: "IX_Companies_ScormHost",
                table: "Companies",
                column: "ScormHost");
            migrationBuilder.CreateIndex(
                name: "IX_CourseDatas_Flags",
                table: "CourseDatas",
                column: "Flags");
            migrationBuilder.CreateIndex(
                name: "IX_CourseDatas_Key",
                table: "CourseDatas",
                column: "Key");
            migrationBuilder.CreateIndex(
                name: "IX_CourseUsers_ProductId",
                table: "CourseUsers",
                column: "ProductId");
            migrationBuilder.CreateIndex(
                name: "IX_UserLicences_LicenceId_Counter",
                table: "UserLicences",
                columns: new[] { "LicenceId", "Counter" },
                unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_Users_EMail",
                table: "Users",
                column: "EMail");
            migrationBuilder.CreateIndex(
                name: "IX_Users_OtherId",
                table: "Users",
                column: "OtherId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("CourseDatas");
            migrationBuilder.DropTable("LANGMasterScorms");
            migrationBuilder.DropTable("UserLicences");
            migrationBuilder.DropTable("CompanyLicences");
            migrationBuilder.DropTable("CourseUsers");
            migrationBuilder.DropTable("CompanyUsers");
            migrationBuilder.DropTable("CompanyDepartments");
            migrationBuilder.DropTable("Users");
            migrationBuilder.DropTable("Companies");
        }
    }
}
