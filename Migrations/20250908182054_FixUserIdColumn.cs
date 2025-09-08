using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThesisNest.Migrations
{
    /// <inheritdoc />
    public partial class FixUserIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeacherProfiles_AspNetUsers_ApplicationUserId",
                table: "TeacherProfiles");

            migrationBuilder.DropIndex(
                name: "IX_TeacherProfiles_ApplicationUserId",
                table: "TeacherProfiles");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "TeacherProfiles");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "TeacherProfiles",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TeacherProfiles",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "TeacherProfiles",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TeacherProfiles",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "TeacherProfiles",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherProfiles_ApplicationUserId",
                table: "TeacherProfiles",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherProfiles_AspNetUsers_ApplicationUserId",
                table: "TeacherProfiles",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
