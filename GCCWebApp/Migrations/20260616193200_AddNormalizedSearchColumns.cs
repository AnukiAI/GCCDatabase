using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedSearchColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Tags",
                type: "nvarchar(450)",
                nullable: true,
                computedColumnSql: "LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Name], N' ', N''), NCHAR(9), N''), NCHAR(10), N''), NCHAR(13), N''), N'™', N''), N'®', N''), N'©', N''), N'℠', N''), N'°', N''), N'-', N''), N'–', N''), N'—', N''), N'_', N''), N'.', N''), N',', N''), N':', N''), N';', N''), N'!', N''), N'?', N''), N'''', N''), N'‘', N''), N'’', N''), N'`', N''), N'\"', N''), N'“', N''), N'”', N''), N'(', N''), N')', N''), N'[', N''), N']', N''), N'{', N''), N'}', N''), N'/', N''), N'\\', N''), N'|', N''), N'&', N''), N'+', N''), N'*', N''), N'@', N''), N'#', N''), N'~', N''), N'^', N''), N'=', N''), N'<', N''), N'>', N''), N'%', N''))",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Products",
                type: "nvarchar(450)",
                nullable: true,
                computedColumnSql: "LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Name], N' ', N''), NCHAR(9), N''), NCHAR(10), N''), NCHAR(13), N''), N'™', N''), N'®', N''), N'©', N''), N'℠', N''), N'°', N''), N'-', N''), N'–', N''), N'—', N''), N'_', N''), N'.', N''), N',', N''), N':', N''), N';', N''), N'!', N''), N'?', N''), N'''', N''), N'‘', N''), N'’', N''), N'`', N''), N'\"', N''), N'“', N''), N'”', N''), N'(', N''), N')', N''), N'[', N''), N']', N''), N'{', N''), N'}', N''), N'/', N''), N'\\', N''), N'|', N''), N'&', N''), N'+', N''), N'*', N''), N'@', N''), N'#', N''), N'~', N''), N'^', N''), N'=', N''), N'<', N''), N'>', N''), N'%', N''))",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "ProductCategories",
                type: "nvarchar(450)",
                nullable: true,
                computedColumnSql: "LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Name], N' ', N''), NCHAR(9), N''), NCHAR(10), N''), NCHAR(13), N''), N'™', N''), N'®', N''), N'©', N''), N'℠', N''), N'°', N''), N'-', N''), N'–', N''), N'—', N''), N'_', N''), N'.', N''), N',', N''), N':', N''), N';', N''), N'!', N''), N'?', N''), N'''', N''), N'‘', N''), N'’', N''), N'`', N''), N'\"', N''), N'“', N''), N'”', N''), N'(', N''), N')', N''), N'[', N''), N']', N''), N'{', N''), N'}', N''), N'/', N''), N'\\', N''), N'|', N''), N'&', N''), N'+', N''), N'*', N''), N'@', N''), N'#', N''), N'~', N''), N'^', N''), N'=', N''), N'<', N''), N'>', N''), N'%', N''))",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Companies",
                type: "nvarchar(450)",
                nullable: true,
                computedColumnSql: "LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Name], N' ', N''), NCHAR(9), N''), NCHAR(10), N''), NCHAR(13), N''), N'™', N''), N'®', N''), N'©', N''), N'℠', N''), N'°', N''), N'-', N''), N'–', N''), N'—', N''), N'_', N''), N'.', N''), N',', N''), N':', N''), N';', N''), N'!', N''), N'?', N''), N'''', N''), N'‘', N''), N'’', N''), N'`', N''), N'\"', N''), N'“', N''), N'”', N''), N'(', N''), N')', N''), N'[', N''), N']', N''), N'{', N''), N'}', N''), N'/', N''), N'\\', N''), N'|', N''), N'&', N''), N'+', N''), N'*', N''), N'@', N''), N'#', N''), N'~', N''), N'^', N''), N'=', N''), N'<', N''), N'>', N''), N'%', N''))",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "CertificationTypes",
                type: "nvarchar(450)",
                nullable: true,
                computedColumnSql: "LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Name], N' ', N''), NCHAR(9), N''), NCHAR(10), N''), NCHAR(13), N''), N'™', N''), N'®', N''), N'©', N''), N'℠', N''), N'°', N''), N'-', N''), N'–', N''), N'—', N''), N'_', N''), N'.', N''), N',', N''), N':', N''), N';', N''), N'!', N''), N'?', N''), N'''', N''), N'‘', N''), N'’', N''), N'`', N''), N'\"', N''), N'“', N''), N'”', N''), N'(', N''), N')', N''), N'[', N''), N']', N''), N'{', N''), N'}', N''), N'/', N''), N'\\', N''), N'|', N''), N'&', N''), N'+', N''), N'*', N''), N'@', N''), N'#', N''), N'~', N''), N'^', N''), N'=', N''), N'<', N''), N'>', N''), N'%', N''))",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedCountry",
                table: "Certificates",
                type: "nvarchar(450)",
                nullable: true,
                computedColumnSql: "LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Country], N' ', N''), NCHAR(9), N''), NCHAR(10), N''), NCHAR(13), N''), N'™', N''), N'®', N''), N'©', N''), N'℠', N''), N'°', N''), N'-', N''), N'–', N''), N'—', N''), N'_', N''), N'.', N''), N',', N''), N':', N''), N';', N''), N'!', N''), N'?', N''), N'''', N''), N'‘', N''), N'’', N''), N'`', N''), N'\"', N''), N'“', N''), N'”', N''), N'(', N''), N')', N''), N'[', N''), N']', N''), N'{', N''), N'}', N''), N'/', N''), N'\\', N''), N'|', N''), N'&', N''), N'+', N''), N'*', N''), N'@', N''), N'#', N''), N'~', N''), N'^', N''), N'=', N''), N'<', N''), N'>', N''), N'%', N''))",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedLocation",
                table: "Certificates",
                type: "nvarchar(450)",
                nullable: true,
                computedColumnSql: "LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Location], N' ', N''), NCHAR(9), N''), NCHAR(10), N''), NCHAR(13), N''), N'™', N''), N'®', N''), N'©', N''), N'℠', N''), N'°', N''), N'-', N''), N'–', N''), N'—', N''), N'_', N''), N'.', N''), N',', N''), N':', N''), N';', N''), N'!', N''), N'?', N''), N'''', N''), N'‘', N''), N'’', N''), N'`', N''), N'\"', N''), N'“', N''), N'”', N''), N'(', N''), N')', N''), N'[', N''), N']', N''), N'{', N''), N'}', N''), N'/', N''), N'\\', N''), N'|', N''), N'&', N''), N'+', N''), N'*', N''), N'@', N''), N'#', N''), N'~', N''), N'^', N''), N'=', N''), N'<', N''), N'>', N''), N'%', N''))",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedNumber",
                table: "Certificates",
                type: "nvarchar(450)",
                nullable: true,
                computedColumnSql: "LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE([Number], N' ', N''), NCHAR(9), N''), NCHAR(10), N''), NCHAR(13), N''), N'™', N''), N'®', N''), N'©', N''), N'℠', N''), N'°', N''), N'-', N''), N'–', N''), N'—', N''), N'_', N''), N'.', N''), N',', N''), N':', N''), N';', N''), N'!', N''), N'?', N''), N'''', N''), N'‘', N''), N'’', N''), N'`', N''), N'\"', N''), N'“', N''), N'”', N''), N'(', N''), N')', N''), N'[', N''), N']', N''), N'{', N''), N'}', N''), N'/', N''), N'\\', N''), N'|', N''), N'&', N''), N'+', N''), N'*', N''), N'@', N''), N'#', N''), N'~', N''), N'^', N''), N'=', N''), N'<', N''), N'>', N''), N'%', N''))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_NormalizedName",
                table: "Tags",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_Products_NormalizedName",
                table: "Products",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_NormalizedName",
                table: "ProductCategories",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_NormalizedName",
                table: "Companies",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationTypes_NormalizedName",
                table: "CertificationTypes",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_NormalizedCountry",
                table: "Certificates",
                column: "NormalizedCountry");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_NormalizedLocation",
                table: "Certificates",
                column: "NormalizedLocation");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_NormalizedNumber",
                table: "Certificates",
                column: "NormalizedNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tags_NormalizedName",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Products_NormalizedName",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductCategories_NormalizedName",
                table: "ProductCategories");

            migrationBuilder.DropIndex(
                name: "IX_Companies_NormalizedName",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_CertificationTypes_NormalizedName",
                table: "CertificationTypes");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_NormalizedCountry",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_NormalizedLocation",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_NormalizedNumber",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "CertificationTypes");

            migrationBuilder.DropColumn(
                name: "NormalizedCountry",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "NormalizedLocation",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "NormalizedNumber",
                table: "Certificates");
        }
    }
}
