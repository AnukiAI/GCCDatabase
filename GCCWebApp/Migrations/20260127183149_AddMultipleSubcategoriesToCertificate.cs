using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipleSubcategoriesToCertificate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the junction table for multi-select subcategories
            migrationBuilder.CreateTable(
                name: "CertificateCertificationTypeSubcategories",
                columns: table => new
                {
                    CertificateId = table.Column<int>(type: "int", nullable: false),
                    CertificationTypeSubcategoriesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateCertificationTypeSubcategories", x => new { x.CertificateId, x.CertificationTypeSubcategoriesId });
                    table.ForeignKey(
                        name: "FK_CertificateCertificationTypeSubcategories_Certificates_CertificateId",
                        column: x => x.CertificateId,
                        principalTable: "Certificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CertificateCertificationTypeSubcategories_CertificationTypes_CertificationTypeSubcategoriesId",
                        column: x => x.CertificationTypeSubcategoriesId,
                        principalTable: "CertificationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateCertificationTypeSubcategories_CertificationTypeSubcategoriesId",
                table: "CertificateCertificationTypeSubcategories",
                column: "CertificationTypeSubcategoriesId");

            // Migrate existing single subcategory data to the new junction table
            migrationBuilder.Sql(@"
                INSERT INTO CertificateCertificationTypeSubcategories (CertificateId, CertificationTypeSubcategoriesId)
                SELECT Id, CertificationTypeSubcategoryId
                FROM Certificates
                WHERE CertificationTypeSubcategoryId IS NOT NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificateCertificationTypeSubcategories");
        }
    }
}
