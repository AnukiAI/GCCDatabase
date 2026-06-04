using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCompanyToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
