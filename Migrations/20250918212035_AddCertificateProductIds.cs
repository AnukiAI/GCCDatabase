using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificateProductIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CertificateProductIds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CertificateId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductIdType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    ProductIdValue = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Partner = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ProductSku = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateProductIds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateProductIds_Certificates_CertificateId",
                        column: x => x.CertificateId,
                        principalTable: "Certificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CertificateProductIds_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CertificateProductIds_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_CertificateId",
                table: "CertificateProductIds",
                column: "CertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_CertificateId_IsActive",
                table: "CertificateProductIds",
                columns: new[] { "CertificateId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_CertificateId_ProductIdType",
                table: "CertificateProductIds",
                columns: new[] { "CertificateId", "ProductIdType" });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_CreatedAt",
                table: "CertificateProductIds",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_CreatedBy",
                table: "CertificateProductIds",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_IsActive",
                table: "CertificateProductIds",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_Partner",
                table: "CertificateProductIds",
                column: "Partner");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_Partner_Status_IsActive",
                table: "CertificateProductIds",
                columns: new[] { "Partner", "Status", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_ProductIdType",
                table: "CertificateProductIds",
                column: "ProductIdType");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_ProductIdType_ProductIdValue",
                table: "CertificateProductIds",
                columns: new[] { "ProductIdType", "ProductIdValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_ProductIdValue",
                table: "CertificateProductIds",
                column: "ProductIdValue");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_Status",
                table: "CertificateProductIds",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_Status_IsActive_CreatedAt",
                table: "CertificateProductIds",
                columns: new[] { "Status", "IsActive", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_UpdatedBy",
                table: "CertificateProductIds",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificateProductIds");
        }
    }
}
