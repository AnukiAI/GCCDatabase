using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class FixWasteSystemAuditLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix: Waste System Audit should be Level 1, not Level 2
            migrationBuilder.Sql(
                "UPDATE CertificationTypes SET Level = 1 WHERE Name = 'Waste System Audit' AND Level = 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert: Restore Level 2 for Waste System Audit
            migrationBuilder.Sql(
                "UPDATE CertificationTypes SET Level = 2 WHERE Name = 'Waste System Audit' AND Level = 1;");
        }
    }
}
