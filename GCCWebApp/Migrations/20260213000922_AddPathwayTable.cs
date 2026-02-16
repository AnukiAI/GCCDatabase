using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPathwayTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PathwayId",
                table: "Certificates",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Pathways",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pathways", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_PathwayId",
                table: "Certificates",
                column: "PathwayId");

            migrationBuilder.CreateIndex(
                name: "IX_Pathways_IsActive",
                table: "Pathways",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Pathways_IsActive_SortOrder",
                table: "Pathways",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Pathways_Name",
                table: "Pathways",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pathways_SortOrder",
                table: "Pathways",
                column: "SortOrder");

            migrationBuilder.AddForeignKey(
                name: "FK_Certificates_Pathways_PathwayId",
                table: "Certificates",
                column: "PathwayId",
                principalTable: "Pathways",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Seed initial pathway values
            migrationBuilder.InsertData(
                table: "Pathways",
                columns: new[] { "Name", "Description", "IsActive", "IsDeleted", "SortOrder", "CreatedAt", "UpdatedAt" },
                values: new object[] { "Energy usage reduction", null, true, false, 1, DateTime.UtcNow, DateTime.UtcNow });

            migrationBuilder.InsertData(
                table: "Pathways",
                columns: new[] { "Name", "Description", "IsActive", "IsDeleted", "SortOrder", "CreatedAt", "UpdatedAt" },
                values: new object[] { "Onsite fossil fuel usage reduction", null, true, false, 2, DateTime.UtcNow, DateTime.UtcNow });

            migrationBuilder.InsertData(
                table: "Pathways",
                columns: new[] { "Name", "Description", "IsActive", "IsDeleted", "SortOrder", "CreatedAt", "UpdatedAt" },
                values: new object[] { "Renewable electricity usage", null, true, false, 3, DateTime.UtcNow, DateTime.UtcNow });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Certificates_Pathways_PathwayId",
                table: "Certificates");

            migrationBuilder.DropTable(
                name: "Pathways");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_PathwayId",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "PathwayId",
                table: "Certificates");
        }
    }
}
