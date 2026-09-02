using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class CR037_MultiCountryCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CertificateCountries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CertificateId = table.Column<int>(type: "int", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormalizedCountry = table.Column<string>(type: "nvarchar(450)", nullable: true, computedColumnSql: "LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Country], N' ', N''), NCHAR(9), N''), NCHAR(10), N''), NCHAR(13), N''), N'™', N''), N'®', N''), N'©', N''), N'℠', N''), N'°', N''), N'-', N''), N'–', N''), N'—', N''), N'_', N''), N'.', N''), N',', N''), N':', N''), N';', N''), N'!', N''), N'?', N''), N'''', N''), N'‘', N''), N'’', N''), N'`', N''), N'\"', N''), N'“', N''), N'”', N''), N'(', N''), N')', N''), N'[', N''), N']', N''), N'{', N''), N'}', N''), N'/', N''), N'\\', N''), N'|', N''), N'&', N''), N'+', N''), N'*', N''), N'@', N''), N'#', N''), N'~', N''), N'^', N''), N'=', N''), N'<', N''), N'>', N''), N'%', N''))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateCountries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateCountries_Certificates_CertificateId",
                        column: x => x.CertificateId,
                        principalTable: "Certificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateCountries_CertificateId_Country",
                table: "CertificateCountries",
                columns: new[] { "CertificateId", "Country" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificateCountries_Country",
                table: "CertificateCountries",
                column: "Country");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateCountries_NormalizedCountry",
                table: "CertificateCountries",
                column: "NormalizedCountry");

            // ---------------------------------------------------------------------------------
            // CR-037 BACKFILL — mandatory, not an optimisation.
            //
            // Country filtering is a membership test against this table now. Ship the empty table
            // and every certificate that existed before this migration matches no country at all:
            // the filter panel would return zero results for all ~5,700 loaded certificates while
            // the Country column still displayed a value. That reads as "the country filter is
            // broken", which is exactly the failure this backfill exists to prevent.
            //
            // Faithful, not corrective. Whatever sits in Certificates.Country is copied verbatim,
            // including values that do not resolve against CountryList (the known "Atlantis" row,
            // and any bad Location->Country splits from the migration). Cleaning those is a data-ops
            // task needing its own change request — silently dropping them here would hide the
            // problem AND leave those certificates unfilterable.
            //
            // NULLIF/LTRIM/RTRIM skips blank and whitespace-only values. NormalizedCountry is a
            // computed column, so SQL Server populates it for every inserted row automatically.
            // NOT EXISTS keeps the statement re-runnable against the unique (CertificateId, Country)
            // index if it is ever replayed by hand.
            // ---------------------------------------------------------------------------------
            migrationBuilder.Sql(@"
                INSERT INTO [CertificateCountries] ([CertificateId], [Country])
                SELECT c.[Id], LTRIM(RTRIM(c.[Country]))
                FROM [Certificates] c
                WHERE c.[Country] IS NOT NULL
                  AND NULLIF(LTRIM(RTRIM(c.[Country])), '') IS NOT NULL
                  AND NOT EXISTS (
                        SELECT 1 FROM [CertificateCountries] cc
                        WHERE cc.[CertificateId] = c.[Id]
                          AND cc.[Country] = LTRIM(RTRIM(c.[Country]))
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificateCountries");
        }
    }
}
