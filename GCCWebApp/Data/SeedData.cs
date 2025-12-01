using GCCWebApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GCCWebApp.Data
{
    public static class SeedData
    {
        private static string GenerateRandomAlphaNumeric(Random random, int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static async Task InitializeAsync(ApplicationDbContext context)
        {
            // Apply any pending migrations
            await context.Database.MigrateAsync();

            // NOTE: Manual seeding control - Automatic seeding is disabled
            // To seed data, use one of these approaches:
            // 1. Run Database_Scripts/PRODUCTION_ESSENTIAL_SEED.sql for production setup
            // 2. Run Database_Scripts/FULL_TEST_DATA_SEED_uniqueemails.sql for testing/development
            // 3. Run: sqlcmd -S ANUKI01 -U GCC_DevUser -P "DevP@ss2024!SQL" -d GCCDatabase_Dev -i "Database_Scripts\FULL_TEST_DATA_SEED_uniqueemails.sql"

            Console.WriteLine("Database migrations applied. No automatic seeding performed.");
            Console.WriteLine("Use SQL scripts in Database_Scripts/ folder to seed data when needed.");
        }

        /// <summary>
        /// Seeds essential security data (Roles, Permissions, Users, API Keys) that must ALWAYS exist.
        /// This method is idempotent - it checks if data exists before seeding.
        /// </summary>
        private static async Task SeedEssentialSecurityDataAsync(ApplicationDbContext context)
        {
            // Check if Roles already exist - if so, skip seeding
            if (await context.Roles.AnyAsync())
            {
                Console.WriteLine("Essential security data already exists. Skipping seeding.");
                return;
            }

            Console.WriteLine("Seeding essential security data (Roles, Permissions, Users, API Keys)...");

            // Seed Permissions
            var permissions = new List<Permission>
            {
                new Permission { Name = "View Certificates", Codename = "certificate_view", Category = "Certificate", Description = "Can view certificates" },
                new Permission { Name = "Create Certificates", Codename = "certificate_create", Category = "Certificate", Description = "Can create new certificates" },
                new Permission { Name = "Edit Certificates", Codename = "certificate_edit", Category = "Certificate", Description = "Can edit existing certificates" },
                new Permission { Name = "Delete Certificates", Codename = "certificate_delete", Category = "Certificate", Description = "Can delete certificates" },
                new Permission { Name = "Manage Users", Codename = "user_manage", Category = "User", Description = "Can manage user accounts" },
                new Permission { Name = "View Reports", Codename = "report_view", Category = "Report", Description = "Can view system reports" }
            };

            await context.Permissions.AddRangeAsync(permissions);
            await context.SaveChangesAsync();

            // Seed Roles
            var roles = new List<Role>
            {
                new Role { Name = "Admin", Description = "Full system access", IsSystem = true },
                new Role { Name = "Analyst", Description = "Certificate management access", IsSystem = true },
                new Role { Name = "External", Description = "External user access", IsSystem = true },
                new Role { Name = "Public", Description = "Public read-only access", IsSystem = true }
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();

            // Assign permissions to roles
            var adminRole = roles.First(r => r.Name == "Admin");
            var analystRole = roles.First(r => r.Name == "Analyst");
            var externalRole = roles.First(r => r.Name == "External");
            var publicRole = roles.First(r => r.Name == "Public");

            // Admin gets all permissions
            adminRole.Permissions = permissions.ToList();

            // Analyst gets certificate management permissions
            analystRole.Permissions = permissions.Where(p => p.Category == "Certificate" && p.Codename != "certificate_delete").ToList();

            // External gets view and reports
            externalRole.Permissions = permissions.Where(p => p.Codename == "certificate_view" || p.Codename == "report_view").ToList();

            // Public gets only view
            publicRole.Permissions = permissions.Where(p => p.Codename == "certificate_view").ToList();

            await context.SaveChangesAsync();

            // Seed Sample Users (for testing)
            var users = new List<User>
            {
                new User
                {
                    Username = "admin",
                    Email = "swest@anukiconsulting.com",
                    Name = "Admin",
                    FullName = "Admin User",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true,
                    IsStaff = true,
                    IsSuperuser = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "analyst",
                    Email = "analyst@test-gcc.local",
                    Name = "Jane",
                    FullName = "Jane Analyst",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true,
                    IsStaff = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "external",
                    Email = "external@test-gcc.local",
                    Name = "External",
                    FullName = "External User",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "testuser",
                    Email = "testuser@test-gcc.local",
                    Name = "Test",
                    FullName = "Test User",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // Additional realistic users for comprehensive testing
                new User
                {
                    Username = "sarah.johnson",
                    Email = "sarah.johnson@test-gcc.local",
                    Name = "Sarah",
                    FullName = "Sarah Johnson",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true,
                    IsStaff = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "mike.chen",
                    Email = "mike.chen@test-gcc.local",
                    Name = "Mike",
                    FullName = "Mike Chen",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true,
                    IsStaff = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "lisa.rodriguez",
                    Email = "lisa.rodriguez@test-gcc.local",
                    Name = "Lisa",
                    FullName = "Lisa Rodriguez",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "david.kim",
                    Email = "david.kim@test-gcc.local",
                    Name = "David",
                    FullName = "David Kim",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "emma.wilson",
                    Email = "emma.wilson@test-gcc.local",
                    Name = "Emma",
                    FullName = "Emma Wilson",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "alex.martinez",
                    Email = "alex.martinez@test-gcc.local",
                    Name = "Alex",
                    FullName = "Alex Martinez",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "jennifer.lee",
                    Email = "jennifer.lee@test-gcc.local",
                    Name = "Jennifer",
                    FullName = "Jennifer Lee",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true,
                    IsStaff = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "robert.taylor",
                    Email = "robert.taylor@test-gcc.local",
                    Name = "Robert",
                    FullName = "Robert Taylor",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true,
                    IsStaff = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "maria.garcia",
                    Email = "maria.garcia@test-gcc.local",
                    Name = "Maria",
                    FullName = "Maria Garcia",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "public.user",
                    Email = "public.user@test-gcc.local",
                    Name = "Public",
                    FullName = "Public User",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                    Status = "active",
                    TwoFactorMethod = "Email",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();

            // Assign roles to users
            var adminUser = users.First(u => u.Username == "admin");
            var analystUser = users.First(u => u.Username == "analyst");
            var externalUser = users.First(u => u.Username == "external");
            var testUser = users.First(u => u.Username == "testuser");
            var sarahUser = users.First(u => u.Username == "sarah.johnson");
            var mikeUser = users.First(u => u.Username == "mike.chen");
            var lisaUser = users.First(u => u.Username == "lisa.rodriguez");
            var davidUser = users.First(u => u.Username == "david.kim");
            var emmaUser = users.First(u => u.Username == "emma.wilson");
            var alexUser = users.First(u => u.Username == "alex.martinez");
            var jenniferUser = users.First(u => u.Username == "jennifer.lee");
            var robertUser = users.First(u => u.Username == "robert.taylor");
            var mariaUser = users.First(u => u.Username == "maria.garcia");
            var publicUser = users.First(u => u.Username == "public.user");

            // Assign appropriate roles
            adminUser.Roles = new List<Role> { adminRole };
            analystUser.Roles = new List<Role> { analystRole };
            sarahUser.Roles = new List<Role> { analystRole };
            mikeUser.Roles = new List<Role> { analystRole };
            jenniferUser.Roles = new List<Role> { analystRole };
            robertUser.Roles = new List<Role> { adminRole };

            // External company users
            externalUser.Roles = new List<Role> { externalRole };
            lisaUser.Roles = new List<Role> { externalRole };
            davidUser.Roles = new List<Role> { externalRole };
            emmaUser.Roles = new List<Role> { externalRole };
            alexUser.Roles = new List<Role> { externalRole };
            mariaUser.Roles = new List<Role> { externalRole };

            // Public users
            testUser.Roles = new List<Role> { publicRole };
            publicUser.Roles = new List<Role> { publicRole };

            await context.SaveChangesAsync();

            // Seed API Keys for testing
            var apiKeys = new List<ApiKey>
            {
                new ApiKey
                {
                    Name = "Integration Test Key",
                    Key = "gcc_test_key_1234567890abcdef",
                    KeyHash = "test_key_hash_1234567890abcdef",
                    Description = "API key for integration testing",
                    CreatedBy = adminUser.Username,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastUsedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddYears(1),
                    Environment = "Development",
                    Scopes = "read,write,admin"
                },
                new ApiKey
                {
                    Name = "Partner API Access",
                    Key = "gcc_partner_key_abcdef1234567890",
                    KeyHash = "partner_key_hash_abcdef1234567890",
                    Description = "API access for external partners",
                    CreatedBy = externalUser.Username,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastUsedAt = DateTime.UtcNow.AddDays(-5),
                    ExpiresAt = DateTime.UtcNow.AddMonths(6),
                    Environment = "Production",
                    Scopes = "read"
                },
                new ApiKey
                {
                    Name = "Mobile App Key",
                    Key = "gcc_mobile_key_fedcba0987654321",
                    KeyHash = "mobile_key_hash_fedcba0987654321",
                    Description = "API key for mobile application",
                    CreatedBy = analystUser.Username,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    LastUsedAt = DateTime.UtcNow.AddHours(-2),
                    ExpiresAt = DateTime.UtcNow.AddMonths(3),
                    Environment = "Production",
                    Scopes = "read,write"
                },
                new ApiKey
                {
                    Name = "Reporting Service",
                    Key = "gcc_report_key_1a2b3c4d5e6f7890",
                    KeyHash = "report_key_hash_1a2b3c4d5e6f7890",
                    Description = "API key for automated reporting service",
                    CreatedBy = sarahUser.Username,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    LastUsedAt = DateTime.UtcNow.AddMinutes(-30),
                    ExpiresAt = DateTime.UtcNow.AddYears(2),
                    Environment = "Production",
                    Scopes = "read,report"
                },
                new ApiKey
                {
                    Name = "Legacy Integration",
                    Key = "gcc_legacy_key_0987654321abcdef",
                    KeyHash = "legacy_key_hash_0987654321abcdef",
                    Description = "API key for legacy system integration",
                    CreatedBy = mikeUser.Username,
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6),
                    LastUsedAt = DateTime.UtcNow.AddDays(-90),
                    ExpiresAt = DateTime.UtcNow.AddDays(-30),
                    Environment = "Staging",
                    Scopes = "read"
                }
            };

            await context.ApiKeys.AddRangeAsync(apiKeys);
            await context.SaveChangesAsync();

            Console.WriteLine($"Essential security data seeded successfully: {roles.Count} roles, {permissions.Count} permissions, {users.Count} users, {apiKeys.Count} API keys");
        }

        private static async Task SeedCustomFieldsAsync(ApplicationDbContext context)
        {
            // Get the first certification type to associate with custom fields
            var defaultCertType = await context.CertificationTypes.FirstOrDefaultAsync();
            if (defaultCertType == null)
            {
                throw new InvalidOperationException("Cannot seed custom fields: No certification types exist in database");
            }

            // Seed CustomFields (Certificate Fields)
            var customFields = new List<CustomField>
            {
                // Basic Certificate Information Fields
                new CustomField { Name = "Certificate Holder Company Name", FieldType = "text", IsRequired = true, IsVisible = true, SortOrder = 1, DefaultValue = "", ValidationRules = "maxLength:255", Options = "", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Certificate Holder Contact Email", FieldType = "email", IsRequired = true, IsVisible = true, SortOrder = 2, DefaultValue = "", ValidationRules = "email,maxLength:255", Options = "", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Certificate Holder Phone Number", FieldType = "phone", IsRequired = false, IsVisible = true, SortOrder = 3, DefaultValue = "", ValidationRules = "phone,maxLength:20", Options = "", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Certificate Holder Address", FieldType = "textarea", IsRequired = false, IsVisible = true, SortOrder = 4, DefaultValue = "", ValidationRules = "maxLength:500", Options = "", CertificateTypeId = defaultCertType.Id },

                // Product Information Fields
                new CustomField { Name = "Product Brand Name", FieldType = "text", IsRequired = false, IsVisible = true, SortOrder = 5, DefaultValue = "", ValidationRules = "maxLength:255", Options = "", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Product Model Number", FieldType = "text", IsRequired = false, IsVisible = true, SortOrder = 6, DefaultValue = "", ValidationRules = "maxLength:100", Options = "", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Product SKU", FieldType = "text", IsRequired = false, IsVisible = true, SortOrder = 7, DefaultValue = "", ValidationRules = "maxLength:100", Options = "", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Product Dimensions", FieldType = "text", IsRequired = false, IsVisible = true, SortOrder = 8, DefaultValue = "", ValidationRules = "maxLength:100", Options = "", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Product Weight", FieldType = "number", IsRequired = false, IsVisible = true, SortOrder = 9, DefaultValue = "", ValidationRules = "numeric,min:0", Options = "", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Product Color Options", FieldType = "text", IsRequired = false, IsVisible = true, SortOrder = 10, DefaultValue = "", ValidationRules = "maxLength:255", Options = "", CertificateTypeId = defaultCertType.Id },

                // Manufacturing Information Fields
                new CustomField { Name = "Manufacturing Facility Name", FieldType = "text", IsRequired = false, IsVisible = true, SortOrder = 11, DefaultValue = "", ValidationRules = "maxLength:255", Options = "", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Manufacturing Facility Location", FieldType = "text", IsRequired = false, IsVisible = true, SortOrder = 12, DefaultValue = "", ValidationRules = "maxLength:255", Options = "", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Manufacturing Country", FieldType = "select", IsRequired = false, IsVisible = true, SortOrder = 13, DefaultValue = "United States", ValidationRules = "", Options = "United States,Canada,Mexico,China,India,Germany,United Kingdom,France,Italy,Japan,Other", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Manufacturing Date", FieldType = "date", IsRequired = false, IsVisible = true, SortOrder = 14, DefaultValue = "", ValidationRules = "date", Options = "", CertificateTypeId = defaultCertType.Id },

                // Environmental and Sustainability Fields
                new CustomField { Name = "Environmental Impact Statement", FieldType = "textarea", IsRequired = false, IsVisible = true, SortOrder = 15, DefaultValue = "", ValidationRules = "maxLength:1000", Options = "", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Third-Party Certifications", FieldType = "multiselect", IsRequired = false, IsVisible = true, SortOrder = 16, DefaultValue = "", ValidationRules = "", Options = "LEED,Energy Star,GREENGUARD,Cradle to Cradle,FSC,PEFC,SFI,Carbon Neutral,ISO 14001,Other", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Carbon Footprint (kg CO2e)", FieldType = "number", IsRequired = false, IsVisible = true, SortOrder = 17, DefaultValue = "", ValidationRules = "numeric,min:0", Options = "", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Water Usage (liters)", FieldType = "number", IsRequired = false, IsVisible = true, SortOrder = 18, DefaultValue = "", ValidationRules = "numeric,min:0", Options = "", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Energy Consumption (kWh)", FieldType = "number", IsRequired = false, IsVisible = true, SortOrder = 19, DefaultValue = "", ValidationRules = "numeric,min:0", Options = "", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Recycled Content Percentage", FieldType = "number", IsRequired = false, IsVisible = true, SortOrder = 20, DefaultValue = "", ValidationRules = "numeric,min:0,max:100", Options = "", CertificateTypeId = defaultCertType.Id },

                // Testing and Compliance Fields
                new CustomField { Name = "Test Report Number", FieldType = "text", IsRequired = false, IsVisible = true, SortOrder = 21, DefaultValue = "", ValidationRules = "maxLength:100", Options = "", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Test Laboratory Name", FieldType = "text", IsRequired = false, IsVisible = true, SortOrder = 22, DefaultValue = "", ValidationRules = "maxLength:255", Options = "", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Test Date", FieldType = "date", IsRequired = false, IsVisible = true, SortOrder = 23, DefaultValue = "", ValidationRules = "date", Options = "", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Compliance Standards", FieldType = "multiselect", IsRequired = false, IsVisible = true, SortOrder = 24, DefaultValue = "", ValidationRules = "", Options = "ISO 14024,ISO 14025,ISO 14040,ASTM Standards,ANSI Standards,UL Standards,Other", CertificateTypeId = defaultCertType.Id },

                // Additional Information Fields
                new CustomField { Name = "Special Notes", FieldType = "textarea", IsRequired = false, IsVisible = true, SortOrder = 25, DefaultValue = "", ValidationRules = "maxLength:2000", Options = "", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Internal Reference Number", FieldType = "text", IsRequired = false, IsVisible = true, SortOrder = 26, DefaultValue = "", ValidationRules = "maxLength:100", Options = "", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Priority Level", FieldType = "select", IsRequired = false, IsVisible = true, SortOrder = 27, DefaultValue = "Normal", ValidationRules = "", Options = "Low,Normal,High,Urgent", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Certificate Visibility", FieldType = "select", IsRequired = false, IsVisible = true, SortOrder = 28, DefaultValue = "Public", ValidationRules = "", Options = "Public,Partner Only,Internal Only,Confidential", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Renewal Reminder Date", FieldType = "date", IsRequired = false, IsVisible = true, SortOrder = 29, DefaultValue = "", ValidationRules = "date", Options = "", CertificateTypeId = defaultCertType.Id },
                new CustomField { Name = "Assigned Analyst", FieldType = "text", IsRequired = false, IsVisible = true, SortOrder = 30, DefaultValue = "", ValidationRules = "maxLength:255", Options = "", CertificateTypeId = defaultCertType.Id }
            };

            await context.CustomFields.AddRangeAsync(customFields);
            await context.SaveChangesAsync();
        }
    }
}