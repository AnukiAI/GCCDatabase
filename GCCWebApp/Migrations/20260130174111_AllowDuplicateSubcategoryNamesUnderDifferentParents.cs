using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AllowDuplicateSubcategoryNamesUnderDifferentParents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CertificationTypes_Name",
                table: "CertificationTypes");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationTypes_Name_ParentId",
                table: "CertificationTypes",
                columns: new[] { "Name", "ParentId" },
                unique: true,
                filter: "[ParentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationTypes_Name_TopLevel",
                table: "CertificationTypes",
                column: "Name",
                unique: true,
                filter: "[ParentId] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CertificationTypes_Name_ParentId",
                table: "CertificationTypes");

            migrationBuilder.DropIndex(
                name: "IX_CertificationTypes_Name_TopLevel",
                table: "CertificationTypes");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationTypes_Name",
                table: "CertificationTypes",
                column: "Name",
                unique: true);
        }
    }
}
