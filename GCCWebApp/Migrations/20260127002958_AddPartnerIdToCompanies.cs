using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerIdToCompanies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PartnerId",
                table: "Companies",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "Certificates",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(7)",
                oldMaxLength: 7);

            migrationBuilder.CreateTable(
                name: "CertificatePartners",
                columns: table => new
                {
                    CertificatesId = table.Column<int>(type: "int", nullable: false),
                    PartnersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificatePartners", x => new { x.CertificatesId, x.PartnersId });
                    table.ForeignKey(
                        name: "FK_CertificatePartners_Certificates_CertificatesId",
                        column: x => x.CertificatesId,
                        principalTable: "Certificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CertificatePartners_Partners_PartnersId",
                        column: x => x.PartnersId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_PartnerId",
                table: "Companies",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificatePartners_PartnersId",
                table: "CertificatePartners",
                column: "PartnersId");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Partners_PartnerId",
                table: "Companies",
                column: "PartnerId",
                principalTable: "Partners",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Partners_PartnerId",
                table: "Companies");

            migrationBuilder.DropTable(
                name: "CertificatePartners");

            migrationBuilder.DropIndex(
                name: "IX_Companies_PartnerId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PartnerId",
                table: "Companies");

            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "Certificates",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
