using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class RetargetProductSubcategoryFkToProductTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_ProductSubcategories_ProductSubcategoryId",
                table: "Products");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ProductTypes_ProductSubcategoryId",
                table: "Products",
                column: "ProductSubcategoryId",
                principalTable: "ProductTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_ProductTypes_ProductSubcategoryId",
                table: "Products");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ProductSubcategories_ProductSubcategoryId",
                table: "Products",
                column: "ProductSubcategoryId",
                principalTable: "ProductSubcategories",
                principalColumn: "Id");
        }
    }
}
