using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddParentIdToCertificationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "CertificationTypes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificationTypes_ParentId",
                table: "CertificationTypes",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_CertificationTypes_CertificationTypes_ParentId",
                table: "CertificationTypes",
                column: "ParentId",
                principalTable: "CertificationTypes",
                principalColumn: "Id");

            // Set up parent-child relationship: "Waste System Audit" is a child of "Waste Diversion from Landfill"
            migrationBuilder.Sql(@"
                UPDATE CertificationTypes
                SET ParentId = (SELECT Id FROM CertificationTypes WHERE Name = 'Waste Diversion from Landfill')
                WHERE Name = 'Waste System Audit';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CertificationTypes_CertificationTypes_ParentId",
                table: "CertificationTypes");

            migrationBuilder.DropIndex(
                name: "IX_CertificationTypes_ParentId",
                table: "CertificationTypes");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "CertificationTypes");
        }
    }
}
