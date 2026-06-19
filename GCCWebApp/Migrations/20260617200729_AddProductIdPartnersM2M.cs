using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddProductIdPartnersM2M : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CertificateProductIdPartners",
                columns: table => new
                {
                    CertificateProductIdId = table.Column<int>(type: "int", nullable: false),
                    PartnersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateProductIdPartners", x => new { x.CertificateProductIdId, x.PartnersId });
                    table.ForeignKey(
                        name: "FK_CertificateProductIdPartners_CertificateProductIds_CertificateProductIdId",
                        column: x => x.CertificateProductIdId,
                        principalTable: "CertificateProductIds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CertificateProductIdPartners_Partners_PartnersId",
                        column: x => x.PartnersId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIdPartners_PartnersId",
                table: "CertificateProductIdPartners",
                column: "PartnersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificateProductIdPartners");
        }
    }
}
