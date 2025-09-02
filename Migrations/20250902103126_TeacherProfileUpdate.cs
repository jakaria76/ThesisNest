using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThesisNest.Migrations
{
    /// <inheritdoc />
    public partial class TeacherProfileUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "TeacherProfiles",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "TeacherProfiles",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "TeacherProfiles");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "TeacherProfiles");
        }
    }
}
