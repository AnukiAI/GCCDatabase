using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificationResultsToCertificate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CertificationResults",
                table: "Certificates",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertificationResults",
                table: "Certificates");
        }
    }
}
