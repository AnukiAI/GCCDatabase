using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class FixProductIdUniqueIndex_PerCertificate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CertificateProductIds_ProductIdType_ProductIdValue",
                table: "CertificateProductIds");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_CertificateId_ProductIdType_ProductIdValue",
                table: "CertificateProductIds",
                columns: new[] { "CertificateId", "ProductIdType", "ProductIdValue" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CertificateProductIds_CertificateId_ProductIdType_ProductIdValue",
                table: "CertificateProductIds");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_ProductIdType_ProductIdValue",
                table: "CertificateProductIds",
                columns: new[] { "ProductIdType", "ProductIdValue" },
                unique: true);
        }
    }
}
