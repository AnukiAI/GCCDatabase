using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class DistributeCertificatesAcrossTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Distribute certificates across all 22 certification types
            // This ensures all types appear in the filter list
            migrationBuilder.Sql(@"
                -- Update certificates to distribute across all certification types
                WITH NumberedCertificates AS (
                    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) as RowNum
                    FROM Certificates
                )
                UPDATE c
                SET CertificationTypeId = 1 + ((nc.RowNum - 1) % 22)
                FROM Certificates c
                INNER JOIN NumberedCertificates nc ON c.Id = nc.Id;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
