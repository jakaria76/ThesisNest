using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThesisNest.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTeacherProfile_Config : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedThesisCount",
                table: "TeacherProfiles");

            migrationBuilder.DropColumn(
                name: "GoogleScholarUrl",
                table: "TeacherProfiles");

            migrationBuilder.DropColumn(
                name: "LinkedInUrl",
                table: "TeacherProfiles");

            migrationBuilder.DropColumn(
                name: "OngoingThesisCount",
                table: "TeacherProfiles");

            migrationBuilder.DropColumn(
                name: "Orcid",
                table: "TeacherProfiles");

            migrationBuilder.DropColumn(
                name: "ResearchGateUrl",
                table: "TeacherProfiles");

            migrationBuilder.DropColumn(
                name: "ScopusId",
                table: "TeacherProfiles");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                table: "TeacherProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletedThesisCount",
                table: "TeacherProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "GoogleScholarUrl",
                table: "TeacherProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedInUrl",
                table: "TeacherProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OngoingThesisCount",
                table: "TeacherProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Orcid",
                table: "TeacherProfiles",
                type: "nvarchar(19)",
                maxLength: 19,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResearchGateUrl",
                table: "TeacherProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScopusId",
                table: "TeacherProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "TeacherProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
