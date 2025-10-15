using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCCWebApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_SqlServer_Final : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RateLimitPerMinute = table.Column<int>(type: "int", nullable: true),
                    RateLimitPerHour = table.Column<int>(type: "int", nullable: true),
                    RateLimitPerDay = table.Column<int>(type: "int", nullable: true),
                    AllowedIPs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AllowedOrigins = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Scopes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KeyHash = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    MaxConcurrentRequests = table.Column<int>(type: "int", nullable: true),
                    RequireTLS = table.Column<bool>(type: "bit", nullable: false),
                    LastSecurityIncident = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SecurityViolationCount = table.Column<int>(type: "int", nullable: false),
                    IsTemporarilyBlocked = table.Column<bool>(type: "bit", nullable: false),
                    BlockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RateLimit = table.Column<int>(type: "int", nullable: true),
                    SecurityViolations = table.Column<int>(type: "int", nullable: false),
                    LastViolationAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastViolationType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BlockReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestCount = table.Column<int>(type: "int", nullable: false),
                    LastRotatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Environment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsageCostPerRequest = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CertificateAggregates",
                columns: table => new
                {
                    Period = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PeriodDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalCertificates = table.Column<int>(type: "int", nullable: false),
                    ActiveCertificates = table.Column<int>(type: "int", nullable: false),
                    ExpiredCertificates = table.Column<int>(type: "int", nullable: false),
                    ExpiringThisMonth = table.Column<int>(type: "int", nullable: false),
                    CertificatesByType = table.Column<string>(type: "TEXT", nullable: false),
                    CertificatesByCountry = table.Column<string>(type: "TEXT", nullable: false),
                    CertificatesByCompany = table.Column<string>(type: "TEXT", nullable: false),
                    AverageValidityPeriod = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateAggregates", x => new { x.Period, x.PeriodDate });
                });

            migrationBuilder.CreateTable(
                name: "CertificationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AmazonCode = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContactInfo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(175)", maxLength: 175, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ETLAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JobType = table.Column<int>(type: "int", nullable: false),
                    IsIncremental = table.Column<bool>(type: "bit", nullable: false),
                    LastRunTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DataSources = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Transformations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Destinations = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Settings = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CronSchedule = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ETLConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ETLDataQualityIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssueType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpectedValue = table.Column<string>(type: "TEXT", nullable: true),
                    ActualValue = table.Column<string>(type: "TEXT", nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ETLDataQualityIssues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ETLLogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LogLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Properties = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ETLLogEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Partners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContactInfo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Codename = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TagType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsStaff = table.Column<bool>(type: "bit", nullable: false),
                    IsSuperuser = table.Column<bool>(type: "bit", nullable: false),
                    RequirePasswordReset = table.Column<bool>(type: "bit", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PasswordChangedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TwoFactorSecret = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPhoneVerified = table.Column<bool>(type: "bit", nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    RecoveryCodes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastSecurityAudit = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SecurityQuestions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    MaxConcurrentSessions = table.Column<int>(type: "int", nullable: false),
                    AllowedIPs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequirePasswordChange = table.Column<bool>(type: "bit", nullable: false),
                    PasswordExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PasswordHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxPasswordHistory = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Options = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidationRules = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CertificateTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomFields_CertificationTypes_CertificateTypeId",
                        column: x => x.CertificateTypeId,
                        principalTable: "CertificationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductTypes_ProductCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ProductCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductTypes_ProductTypes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "ProductTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PermissionRole",
                columns: table => new
                {
                    PermissionsId = table.Column<int>(type: "int", nullable: false),
                    RolesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionRole", x => new { x.PermissionsId, x.RolesId });
                    table.ForeignKey(
                        name: "FK_PermissionRole_Permissions_PermissionsId",
                        column: x => x.PermissionsId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PermissionRole_Roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyUser",
                columns: table => new
                {
                    CompaniesId = table.Column<int>(type: "int", nullable: false),
                    UsersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyUser", x => new { x.CompaniesId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_CompanyUser_Companies_CompaniesId",
                        column: x => x.CompaniesId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyUser_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartnerUser",
                columns: table => new
                {
                    PartnersId = table.Column<int>(type: "int", nullable: false),
                    UsersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerUser", x => new { x.PartnersId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_PartnerUser_Partners_PartnersId",
                        column: x => x.PartnersId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartnerUser_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleUser",
                columns: table => new
                {
                    RolesId = table.Column<int>(type: "int", nullable: false),
                    UsersId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleUser", x => new { x.RolesId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_RoleUser_Roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleUser_Users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductSubcategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ProductTypeId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSubcategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSubcategories_ProductTypes_ProductTypeId",
                        column: x => x.ProductTypeId,
                        principalTable: "ProductTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Sku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Upc = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Asin = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ProductTypeId = table.Column<int>(type: "int", nullable: true),
                    ProductSubcategoryId = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Products_ProductCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ProductCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Products_ProductSubcategories_ProductSubcategoryId",
                        column: x => x.ProductSubcategoryId,
                        principalTable: "ProductSubcategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Products_ProductTypes_ProductTypeId",
                        column: x => x.ProductTypeId,
                        principalTable: "ProductTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Certificates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(175)", maxLength: 175, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Asin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Upc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gtin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ean = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CertificateFile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CertificateImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Partner = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    CertificationTypeId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Certificates_CertificationTypes_CertificationTypeId",
                        column: x => x.CertificationTypeId,
                        principalTable: "CertificationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Certificates_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Certificates_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Certificates_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Certificates_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductIdentifiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    IdentifierType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IdentifierValue = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductIdentifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductIdentifiers_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductTag",
                columns: table => new
                {
                    ProductsId = table.Column<int>(type: "int", nullable: false),
                    TagsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTag", x => new { x.ProductsId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_ProductTag_Products_ProductsId",
                        column: x => x.ProductsId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductTag_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CertificateDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsVirusScanned = table.Column<bool>(type: "bit", nullable: false),
                    PassedVirusScan = table.Column<bool>(type: "bit", nullable: false),
                    VirusScanDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CertificateId = table.Column<int>(type: "int", nullable: false),
                    UploadedBy = table.Column<int>(type: "int", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateDocuments_Certificates_CertificateId",
                        column: x => x.CertificateId,
                        principalTable: "Certificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CertificateDocuments_Users_UploadedBy",
                        column: x => x.UploadedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CertificateProductIds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CertificateId = table.Column<int>(type: "int", nullable: false),
                    ProductIdType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ProductIdValue = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Partner = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProductSku = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateProductIds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateProductIds_Certificates_CertificateId",
                        column: x => x.CertificateId,
                        principalTable: "Certificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CertificateProductIds_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CertificateProductIds_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CertificateTag",
                columns: table => new
                {
                    CertificatesId = table.Column<int>(type: "int", nullable: false),
                    TagsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateTag", x => new { x.CertificatesId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_CertificateTag_Certificates_CertificatesId",
                        column: x => x.CertificatesId,
                        principalTable: "Certificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CertificateTag_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomFieldValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CertificateId = table.Column<int>(type: "int", nullable: false),
                    FieldId = table.Column<int>(type: "int", nullable: false),
                    TextValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumberValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    DateValue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomFieldValues_Certificates_CertificateId",
                        column: x => x.CertificateId,
                        principalTable: "Certificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomFieldValues_CustomFields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "CustomFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                unique: true,
                filter: "[KeyHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_KeyHash_IsActive_ExpiresAt",
                table: "ApiKeys",
                columns: new[] { "KeyHash", "IsActive", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateAggregates_PeriodDate",
                table: "CertificateAggregates",
                column: "PeriodDate");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateDocuments_CertificateId",
                table: "CertificateDocuments",
                column: "CertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateDocuments_FileHash",
                table: "CertificateDocuments",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateDocuments_UploadedBy",
                table: "CertificateDocuments",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_CertificateId",
                table: "CertificateProductIds",
                column: "CertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_CertificateId_IsActive",
                table: "CertificateProductIds",
                columns: new[] { "CertificateId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_CertificateId_ProductIdType",
                table: "CertificateProductIds",
                columns: new[] { "CertificateId", "ProductIdType" });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_CreatedAt",
                table: "CertificateProductIds",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_CreatedBy",
                table: "CertificateProductIds",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_IsActive",
                table: "CertificateProductIds",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_Partner",
                table: "CertificateProductIds",
                column: "Partner");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_Partner_Status_IsActive",
                table: "CertificateProductIds",
                columns: new[] { "Partner", "Status", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_ProductIdType",
                table: "CertificateProductIds",
                column: "ProductIdType");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_ProductIdType_ProductIdValue",
                table: "CertificateProductIds",
                columns: new[] { "ProductIdType", "ProductIdValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_ProductIdValue",
                table: "CertificateProductIds",
                column: "ProductIdValue");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_Status",
                table: "CertificateProductIds",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_Status_IsActive_CreatedAt",
                table: "CertificateProductIds",
                columns: new[] { "Status", "IsActive", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateProductIds_UpdatedBy",
                table: "CertificateProductIds",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_CertificationTypeId",
                table: "Certificates",
                column: "CertificationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_CertificationTypeId_Status",
                table: "Certificates",
                columns: new[] { "CertificationTypeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_CompanyId",
                table: "Certificates",
                column: "CompanyId");

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
                name: "IX_Certificates_CreatedBy",
                table: "Certificates",
                column: "CreatedBy");

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
                name: "IX_Certificates_Number",
                table: "Certificates",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificates_ProductId",
                table: "Certificates",
                column: "ProductId");

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
                name: "IX_Certificates_UpdatedBy",
                table: "Certificates",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateTag_TagsId",
                table: "CertificateTag",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationTypes_Name",
                table: "CertificationTypes",
                column: "Name",
                unique: true);

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
                name: "IX_Companies_Name",
                table: "Companies",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Name_Country",
                table: "Companies",
                columns: new[] { "Name", "Country" });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Website",
                table: "Companies",
                column: "Website");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyUser_UsersId",
                table: "CompanyUser",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFields_CertificateTypeId",
                table: "CustomFields",
                column: "CertificateTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldValues_CertificateId",
                table: "CustomFieldValues",
                column: "CertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldValues_FieldId",
                table: "CustomFieldValues",
                column: "FieldId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Partners_CreatedAt",
                table: "Partners",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Partners_IsActive",
                table: "Partners",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Partners_IsActive_SortOrder",
                table: "Partners",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Partners_Name",
                table: "Partners",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Partners_SortOrder",
                table: "Partners",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_PartnerUser_UsersId",
                table: "PartnerUser",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRole_RolesId",
                table: "PermissionRole",
                column: "RolesId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Codename",
                table: "Permissions",
                column: "Codename",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_Name",
                table: "ProductCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductIdentifiers_ProductId_IdentifierType_IdentifierValue",
                table: "ProductIdentifiers",
                columns: new[] { "ProductId", "IdentifierType", "IdentifierValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId_CompanyId",
                table: "Products",
                columns: new[] { "CategoryId", "CompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CompanyId",
                table: "Products",
                column: "CompanyId");

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
                name: "IX_Products_ProductSubcategoryId",
                table: "Products",
                column: "ProductSubcategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductTypeId",
                table: "Products",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductTypeId_IsActive",
                table: "Products",
                columns: new[] { "ProductTypeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSubcategories_ProductTypeId",
                table: "ProductSubcategories",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTag_TagsId",
                table: "ProductTag",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTypes_CategoryId",
                table: "ProductTypes",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTypes_ParentId",
                table: "ProductTypes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleUser_UsersId",
                table: "RoleUser",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

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
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "CertificateAggregates");

            migrationBuilder.DropTable(
                name: "CertificateDocuments");

            migrationBuilder.DropTable(
                name: "CertificateProductIds");

            migrationBuilder.DropTable(
                name: "CertificateTag");

            migrationBuilder.DropTable(
                name: "CompanyUser");

            migrationBuilder.DropTable(
                name: "CustomFieldValues");

            migrationBuilder.DropTable(
                name: "ETLAlerts");

            migrationBuilder.DropTable(
                name: "ETLConfigurations");

            migrationBuilder.DropTable(
                name: "ETLDataQualityIssues");

            migrationBuilder.DropTable(
                name: "ETLLogEntries");

            migrationBuilder.DropTable(
                name: "PartnerUser");

            migrationBuilder.DropTable(
                name: "PermissionRole");

            migrationBuilder.DropTable(
                name: "ProductIdentifiers");

            migrationBuilder.DropTable(
                name: "ProductTag");

            migrationBuilder.DropTable(
                name: "RoleUser");

            migrationBuilder.DropTable(
                name: "Certificates");

            migrationBuilder.DropTable(
                name: "CustomFields");

            migrationBuilder.DropTable(
                name: "Partners");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "CertificationTypes");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "ProductSubcategories");

            migrationBuilder.DropTable(
                name: "ProductTypes");

            migrationBuilder.DropTable(
                name: "ProductCategories");
        }
    }
}
