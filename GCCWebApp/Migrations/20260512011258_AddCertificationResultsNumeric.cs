using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificationResultsNumeric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CertificationResultsNumeric",
                table: "Certificates",
                type: "decimal(18,4)",
                nullable: true,
                computedColumnSql: "\r\n                        CASE\r\n                          WHEN [CertificationResults] IS NULL OR [CertificationResults] = N'' THEN NULL\r\n                          WHEN [CertificationResults] LIKE N'%[%]%' THEN TRY_CAST(REPLACE([CertificationResults], N'%', N'') AS DECIMAL(18,4))\r\n                          WHEN TRY_CAST([CertificationResults] AS DECIMAL(18,4)) < 1 THEN TRY_CAST([CertificationResults] AS DECIMAL(18,4)) * 100\r\n                          ELSE TRY_CAST([CertificationResults] AS DECIMAL(18,4))\r\n                        END",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_CertificationResultsNumeric",
                table: "Certificates",
                column: "CertificationResultsNumeric");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Certificates_CertificationResultsNumeric",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "CertificationResultsNumeric",
                table: "Certificates");
        }
    }
}
