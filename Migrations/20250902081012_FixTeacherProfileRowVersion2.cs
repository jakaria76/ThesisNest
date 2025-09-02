using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThesisNest.Migrations
{
    /// <inheritdoc />
    public partial class FixTeacherProfileRowVersion2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedThesisCount",
                table: "TeacherProfiles");

            migrationBuilder.DropColumn(
                name: "OngoingThesisCount",
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

            migrationBuilder.AddColumn<int>(
                name: "OngoingThesisCount",
                table: "TeacherProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
