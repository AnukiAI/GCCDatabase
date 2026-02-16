using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class FixPathwayUniqueIndexForSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pathways_Name",
                table: "Pathways");

            migrationBuilder.CreateIndex(
                name: "IX_Pathways_Name",
                table: "Pathways",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pathways_Name",
                table: "Pathways");

            migrationBuilder.CreateIndex(
                name: "IX_Pathways_Name",
                table: "Pathways",
                column: "Name",
                unique: true);
        }
    }
}
