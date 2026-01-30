using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class ProductTypesPreventDuplicatesUnderSameParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProductTypes_Name_ParentId",
                table: "ProductTypes",
                columns: new[] { "Name", "ParentId" },
                unique: true,
                filter: "[ParentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTypes_Name_TopLevel",
                table: "ProductTypes",
                column: "Name",
                unique: true,
                filter: "[ParentId] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductTypes_Name_ParentId",
                table: "ProductTypes");

            migrationBuilder.DropIndex(
                name: "IX_ProductTypes_Name_TopLevel",
                table: "ProductTypes");
        }
    }
}
