using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdateETLConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Asin",
                table: "Products",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "Products",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "Products",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Upc",
                table: "Products",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CertificateAggregates",
                columns: table => new
                {
                    Period = table.Column<string>(type: "TEXT", nullable: false),
                    PeriodDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalCertificates = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveCertificates = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpiredCertificates = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpiringThisMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    CertificatesByType = table.Column<string>(type: "TEXT", nullable: false),
                    CertificatesByCountry = table.Column<string>(type: "TEXT", nullable: false),
                    CertificatesByCompany = table.Column<string>(type: "TEXT", nullable: false),
                    AverageValidityPeriod = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateAggregates", x => new { x.Period, x.PeriodDate });
                });

            migrationBuilder.CreateTable(
                name: "ETLAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AlertType = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsResolved = table.Column<bool>(type: "INTEGER", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Context = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ETLAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ETLConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    JobType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsIncremental = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastRunTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DataSources = table.Column<string>(type: "TEXT", nullable: false),
                    Transformations = table.Column<string>(type: "TEXT", nullable: false),
                    Destinations = table.Column<string>(type: "TEXT", nullable: false),
                    Settings = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CronSchedule = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ETLConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ETLDataQualityIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IssueType = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    FieldName = table.Column<string>(type: "TEXT", nullable: false),
                    RecordId = table.Column<string>(type: "TEXT", nullable: true),
                    ExpectedValue = table.Column<string>(type: "TEXT", nullable: true),
                    ActualValue = table.Column<string>(type: "TEXT", nullable: true),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ETLDataQualityIssues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ETLLogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LogLevel = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    Properties = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ETLLogEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateAggregates_PeriodDate",
                table: "CertificateAggregates",
                column: "PeriodDate");

            migrationBuilder.CreateIndex(
                name: "IX_ETLAlerts_CreatedAt",
                table: "ETLAlerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ETLAlerts_IsResolved",
                table: "ETLAlerts",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_ETLConfigurations_Name",
                table: "ETLConfigurations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ETLDataQualityIssues_DetectedAt",
                table: "ETLDataQualityIssues",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ETLDataQualityIssues_IssueType",
                table: "ETLDataQualityIssues",
                column: "IssueType");

            migrationBuilder.CreateIndex(
                name: "IX_ETLLogEntries_JobId",
                table: "ETLLogEntries",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_ETLLogEntries_Timestamp",
                table: "ETLLogEntries",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificateAggregates");

            migrationBuilder.DropTable(
                name: "ETLAlerts");

            migrationBuilder.DropTable(
                name: "ETLConfigurations");

            migrationBuilder.DropTable(
                name: "ETLDataQualityIssues");

            migrationBuilder.DropTable(
                name: "ETLLogEntries");

            migrationBuilder.DropColumn(
                name: "Asin",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Upc",
                table: "Products");
        }
    }
}
