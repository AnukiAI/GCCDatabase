using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedBy = table.Column<int>(type: "int", nullable: false),
                    ChangedByUsername = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangeSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditHistory_Users_ChangedBy",
                        column: x => x.ChangedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditHistory_Action",
                table: "AuditHistory",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditHistory_ChangedAt",
                table: "AuditHistory",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditHistory_ChangedAt_EntityType",
                table: "AuditHistory",
                columns: new[] { "ChangedAt", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditHistory_ChangedBy",
                table: "AuditHistory",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AuditHistory_ChangedBy_ChangedAt",
                table: "AuditHistory",
                columns: new[] { "ChangedBy", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditHistory_EntityId",
                table: "AuditHistory",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditHistory_EntityType",
                table: "AuditHistory",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditHistory_EntityType_Action",
                table: "AuditHistory",
                columns: new[] { "EntityType", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditHistory_EntityType_EntityId_ChangedAt",
                table: "AuditHistory",
                columns: new[] { "EntityType", "EntityId", "ChangedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditHistory");
        }
    }
}
