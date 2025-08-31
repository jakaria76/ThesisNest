using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThesisNest.Migrations
{
    /// <inheritdoc />
    public partial class Initial_ThesisWorkflow_Clean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Abstract",
                table: "Theses",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CurrentVersionNo",
                table: "Theses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Theses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Keywords",
                table: "Theses",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProposalStatus",
                table: "Theses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StudentProfileId",
                table: "Theses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Theses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ThesisFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThesisId = table.Column<int>(type: "int", nullable: false),
                    GivenByTeacherProfileId = table.Column<int>(type: "int", nullable: false),
                    GivenById = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    IsChangeRequested = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThesisFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThesisFeedbacks_TeacherProfiles_GivenById",
                        column: x => x.GivenById,
                        principalTable: "TeacherProfiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ThesisFeedbacks_Theses_ThesisId",
                        column: x => x.ThesisId,
                        principalTable: "Theses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThesisVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThesisId = table.Column<int>(type: "int", nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    FileData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CommentByStudentProfileId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThesisVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThesisVersions_Theses_ThesisId",
                        column: x => x.ThesisId,
                        principalTable: "Theses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Theses_DepartmentId_ProposalStatus",
                table: "Theses",
                columns: new[] { "DepartmentId", "ProposalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Theses_StudentProfileId",
                table: "Theses",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ThesisFeedbacks_GivenById",
                table: "ThesisFeedbacks",
                column: "GivenById");

            migrationBuilder.CreateIndex(
                name: "IX_ThesisFeedbacks_ThesisId",
                table: "ThesisFeedbacks",
                column: "ThesisId");

            migrationBuilder.CreateIndex(
                name: "IX_ThesisVersions_ThesisId_VersionNo",
                table: "ThesisVersions",
                columns: new[] { "ThesisId", "VersionNo" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Theses_Departments_DepartmentId",
                table: "Theses",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Theses_StudentProfiles_StudentProfileId",
                table: "Theses",
                column: "StudentProfileId",
                principalTable: "StudentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Theses_Departments_DepartmentId",
                table: "Theses");

            migrationBuilder.DropForeignKey(
                name: "FK_Theses_StudentProfiles_StudentProfileId",
                table: "Theses");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "ThesisFeedbacks");

            migrationBuilder.DropTable(
                name: "ThesisVersions");

            migrationBuilder.DropIndex(
                name: "IX_Theses_DepartmentId_ProposalStatus",
                table: "Theses");

            migrationBuilder.DropIndex(
                name: "IX_Theses_StudentProfileId",
                table: "Theses");

            migrationBuilder.DropColumn(
                name: "Abstract",
                table: "Theses");

            migrationBuilder.DropColumn(
                name: "CurrentVersionNo",
                table: "Theses");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Theses");

            migrationBuilder.DropColumn(
                name: "Keywords",
                table: "Theses");

            migrationBuilder.DropColumn(
                name: "ProposalStatus",
                table: "Theses");

            migrationBuilder.DropColumn(
                name: "StudentProfileId",
                table: "Theses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Theses");
        }
    }
}
