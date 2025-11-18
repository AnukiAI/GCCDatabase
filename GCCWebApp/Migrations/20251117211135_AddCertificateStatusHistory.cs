using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificateStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CertificateStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CertificateId = table.Column<int>(type: "int", nullable: false),
                    CertificateNumber = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    PreviousStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NewStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedBy = table.Column<int>(type: "int", nullable: false),
                    ChangedByUsername = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangeSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DurationInPreviousStatus = table.Column<int>(type: "int", nullable: true),
                    IsAutomatedTransition = table.Column<bool>(type: "bit", nullable: false),
                    WorkflowStep = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateStatusHistory_Certificates_CertificateId",
                        column: x => x.CertificateId,
                        principalTable: "Certificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CertificateStatusHistory_Users_ChangedBy",
                        column: x => x.ChangedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateStatusHistory_CertificateId",
                table: "CertificateStatusHistory",
                column: "CertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateStatusHistory_CertificateId_ChangedAt",
                table: "CertificateStatusHistory",
                columns: new[] { "CertificateId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateStatusHistory_CertificateId_NewStatus",
                table: "CertificateStatusHistory",
                columns: new[] { "CertificateId", "NewStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateStatusHistory_ChangedAt",
                table: "CertificateStatusHistory",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateStatusHistory_ChangedAt_NewStatus",
                table: "CertificateStatusHistory",
                columns: new[] { "ChangedAt", "NewStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateStatusHistory_ChangedBy",
                table: "CertificateStatusHistory",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateStatusHistory_NewStatus",
                table: "CertificateStatusHistory",
                column: "NewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateStatusHistory_PreviousStatus",
                table: "CertificateStatusHistory",
                column: "PreviousStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificateStatusHistory");
        }
    }
}
