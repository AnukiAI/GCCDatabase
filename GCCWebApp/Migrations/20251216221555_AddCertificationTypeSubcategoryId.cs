using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificationTypeSubcategoryId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CertificationTypeSubcategoryId",
                table: "Certificates",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_CertificationTypeSubcategoryId",
                table: "Certificates",
                column: "CertificationTypeSubcategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_CertificationTypes_CertificationTypeSubcategoryId",
                table: "Certificates",
                column: "CertificationTypeSubcategoryId",
                principalTable: "CertificationTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_CertificationTypes_CertificationTypeSubcategoryId",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_CertificationTypeSubcategoryId",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "CertificationTypeSubcategoryId",
                table: "Certificates");
        }
    }
}
