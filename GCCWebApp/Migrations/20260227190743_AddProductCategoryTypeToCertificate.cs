using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCategoryTypeToCertificate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductCategoryId",
                table: "Certificates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductSubcategoryId",
                table: "Certificates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductTypeId",
                table: "Certificates",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_ProductCategoryId",
                table: "Certificates",
                column: "ProductCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_ProductSubcategoryId",
                table: "Certificates",
                column: "ProductSubcategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_ProductTypeId",
                table: "Certificates",
                column: "ProductTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_ProductCategories_ProductCategoryId",
                table: "Certificates",
                column: "ProductCategoryId",
                principalTable: "ProductCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_ProductTypes_ProductSubcategoryId",
                table: "Certificates",
                column: "ProductSubcategoryId",
                principalTable: "ProductTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_ProductTypes_ProductTypeId",
                table: "Certificates",
                column: "ProductTypeId",
                principalTable: "ProductTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Backfill existing certificates from their Product's category/type
            migrationBuilder.Sql(@"
                UPDATE c
                SET c.ProductCategoryId = p.CategoryId,
                    c.ProductTypeId = p.ProductTypeId
                FROM Certificates c
                INNER JOIN Products p ON c.ProductId = p.Id
                WHERE p.CategoryId IS NOT NULL OR p.ProductTypeId IS NOT NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_ProductCategories_ProductCategoryId",
                table: "Certificates");

            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_ProductTypes_ProductSubcategoryId",
                table: "Certificates");

            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_ProductTypes_ProductTypeId",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_ProductCategoryId",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_ProductSubcategoryId",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_ProductTypeId",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "ProductCategoryId",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "ProductSubcategoryId",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "ProductTypeId",
                table: "Certificates");
        }
    }
}
