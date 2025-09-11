using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThesisNest.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelsAfterRoleRemoval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safely drop Role column ONLY if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE Name = N'Role' AND Object_ID = Object_ID(N'AspNetUsers')
                )
                BEGIN
                    ALTER TABLE [AspNetUsers] DROP COLUMN [Role];
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate Role column if needed (rollback)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns 
                    WHERE Name = N'Role' AND Object_ID = Object_ID(N'AspNetUsers')
                )
                BEGIN
                    ALTER TABLE [AspNetUsers] ADD [Role] NVARCHAR(MAX) NOT NULL DEFAULT('');
                END
            ");
        }
    }
}
