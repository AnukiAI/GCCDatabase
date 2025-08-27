using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowedIPs",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPhoneVerified",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSecurityAudit",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxConcurrentSessions",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxPasswordHistory",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordExpiresAt",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHistory",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecoveryCodes",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequirePasswordChange",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SecurityQuestions",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorEnabled",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorMethod",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorSecret",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Companies",
                type: "TEXT",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Companies",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Scopes",
                table: "ApiKeys",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ApiKeys",
                type: "TEXT",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "ApiKeys",
                type: "TEXT",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "AllowedOrigins",
                table: "ApiKeys",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "AllowedIPs",
                table: "ApiKeys",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "BlockReason",
                table: "ApiKeys",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BlockedUntil",
                table: "ApiKeys",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Environment",
                table: "ApiKeys",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsTemporarilyBlocked",
                table: "ApiKeys",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "KeyHash",
                table: "ApiKeys",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRotatedAt",
                table: "ApiKeys",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSecurityIncident",
                table: "ApiKeys",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastViolationAt",
                table: "ApiKeys",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastViolationType",
                table: "ApiKeys",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxConcurrentRequests",
                table: "ApiKeys",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RateLimit",
                table: "ApiKeys",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RateLimitPerDay",
                table: "ApiKeys",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestCount",
                table: "ApiKeys",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequireTLS",
                table: "ApiKeys",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SecurityViolationCount",
                table: "ApiKeys",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SecurityViolations",
                table: "ApiKeys",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "UsageCostPerRequest",
                table: "ApiKeys",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "ApiKeys",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive_CreatedAt",
                table: "Users",
                columns: new[] { "IsActive", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastLoginAt",
                table: "Users",
                column: "LastLoginAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Status",
                table: "Users",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Status_IsActive",
                table: "Users",
                columns: new[] { "Status", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId_CompanyId",
                table: "Products",
                columns: new[] { "CategoryId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CompanyId_IsActive",
                table: "Products",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CreatedAt",
                table: "Products",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive",
                table: "Products",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductTypeId_IsActive",
                table: "Products",
                columns: new[] { "ProductTypeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Country",
                table: "Companies",
                column: "Country");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Country_IsActive",
                table: "Companies",
                columns: new[] { "Country", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CreatedAt",
                table: "Companies",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_IsActive",
                table: "Companies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Name_Country",
                table: "Companies",
                columns: new[] { "Name", "Country" });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Website",
                table: "Companies",
                column: "Website");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_CertificationTypeId_Status",
                table: "Certificates",
                columns: new[] { "CertificationTypeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_CompanyId_Status",
                table: "Certificates",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Country",
                table: "Certificates",
                column: "Country");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Country_Status",
                table: "Certificates",
                columns: new[] { "Country", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_CreatedAt",
                table: "Certificates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_EffectiveDate",
                table: "Certificates",
                column: "EffectiveDate");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_ExpirationDate",
                table: "Certificates",
                column: "ExpirationDate");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_ExpirationDate_Status_CompanyId",
                table: "Certificates",
                columns: new[] { "ExpirationDate", "Status", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_ProductId_Status",
                table: "Certificates",
                columns: new[] { "ProductId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Status",
                table: "Certificates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_Status_ExpirationDate",
                table: "Certificates",
                columns: new[] { "Status", "ExpirationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_CreatedAt",
                table: "ApiKeys",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_CreatedBy",
                table: "ApiKeys",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_CreatedBy_IsActive",
                table: "ApiKeys",
                columns: new[] { "CreatedBy", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_ExpiresAt",
                table: "ApiKeys",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_IsActive",
                table: "ApiKeys",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_KeyHash",
                table: "ApiKeys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_KeyHash_IsActive_ExpiresAt",
                table: "ApiKeys",
                columns: new[] { "KeyHash", "IsActive", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_CreatedAt",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_IsActive",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_IsActive_CreatedAt",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_LastLoginAt",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Status",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Status_IsActive",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategoryId_CompanyId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_CompanyId_IsActive",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_CreatedAt",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsActive",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Name",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProductTypeId_IsActive",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Companies_Country",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_Country_IsActive",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_CreatedAt",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_IsActive",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_Name_Country",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_Website",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_CertificationTypeId_Status",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_CompanyId_Status",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_Country",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_Country_Status",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_CreatedAt",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_EffectiveDate",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_ExpirationDate",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_ExpirationDate_Status_CompanyId",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_ProductId_Status",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_Status",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_Certificates_Status_ExpirationDate",
                table: "Certificates");

            migrationBuilder.DropIndex(
                name: "IX_ApiKeys_CreatedAt",
                table: "ApiKeys");

            migrationBuilder.DropIndex(
                name: "IX_ApiKeys_CreatedBy",
                table: "ApiKeys");

            migrationBuilder.DropIndex(
                name: "IX_ApiKeys_CreatedBy_IsActive",
                table: "ApiKeys");

            migrationBuilder.DropIndex(
                name: "IX_ApiKeys_ExpiresAt",
                table: "ApiKeys");

            migrationBuilder.DropIndex(
                name: "IX_ApiKeys_IsActive",
                table: "ApiKeys");

            migrationBuilder.DropIndex(
                name: "IX_ApiKeys_KeyHash",
                table: "ApiKeys");

            migrationBuilder.DropIndex(
                name: "IX_ApiKeys_KeyHash_IsActive_ExpiresAt",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "AllowedIPs",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsPhoneVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastSecurityAudit",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MaxConcurrentSessions",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MaxPasswordHistory",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordHistory",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RecoveryCodes",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RequirePasswordChange",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SecurityQuestions",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TwoFactorMethod",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TwoFactorSecret",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "BlockReason",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "BlockedUntil",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "Environment",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "IsTemporarilyBlocked",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "KeyHash",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "LastRotatedAt",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "LastSecurityIncident",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "LastViolationAt",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "LastViolationType",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "MaxConcurrentRequests",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "RateLimit",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "RateLimitPerDay",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "RequestCount",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "RequireTLS",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "SecurityViolationCount",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "SecurityViolations",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "UsageCostPerRequest",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "ApiKeys");

            migrationBuilder.AlterColumn<string>(
                name: "Scopes",
                table: "ApiKeys",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ApiKeys",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "ApiKeys",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AllowedOrigins",
                table: "ApiKeys",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AllowedIPs",
                table: "ApiKeys",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
