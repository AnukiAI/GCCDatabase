using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class ChangeUserCompanyToFreeText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CR-020 (revised QC #106): UserCompany goes from an FK to free text.
            // 1) add the new string column, 2) BACKFILL it from the employer's Company.Name
            // so no existing value is lost, 3) then drop the old FK column. Order matters.
            migrationBuilder.AddColumn<string>(
                name: "UserCompany",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE u
                SET u.UserCompany = c.Name
                FROM [Users] u
                INNER JOIN [Companies] c ON u.UserCompanyId = c.Id
                WHERE u.UserCompanyId IS NOT NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Companies_UserCompanyId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_UserCompanyId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserCompanyId",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserCompanyId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserCompanyId",
                table: "Users",
                column: "UserCompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Companies_UserCompanyId",
                table: "Users",
                column: "UserCompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Best-effort reverse backfill: re-link to a Company whose Name matches the free
            // text. Values that don't match a Company (e.g. "GCC") simply stay null — they
            // were never valid FK targets, which is exactly why this CR was raised.
            migrationBuilder.Sql(@"
                UPDATE u
                SET u.UserCompanyId = c.Id
                FROM [Users] u
                INNER JOIN [Companies] c
                    ON LOWER(LTRIM(RTRIM(u.UserCompany))) = LOWER(c.Name)
                WHERE u.UserCompany IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "UserCompany",
                table: "Users");
        }
    }
}
