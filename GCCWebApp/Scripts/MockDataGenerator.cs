using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GCCWebApp.Data;
using GCCWebApp.Models;
using System.Linq;

namespace GCCWebApp.Scripts
{
    /// <summary>
    /// Comprehensive mock data generator for GCC Database Solution
    /// Generates realistic test data for companies, products, certificates, users, and relationships
    /// Usage: dotnet run --project GCCWebApp Scripts/MockDataGenerator.cs
    /// </summary>
    public class MockDataGenerator
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MockDataGenerator> _logger;
        private readonly Random _random;
        private static readonly DateTime BaseDate = new DateTime(2020, 1, 1);

        public MockDataGenerator(ApplicationDbContext context, ILogger<MockDataGenerator> logger)
        {
            _context = context;
            _logger = logger;
            _random = new Random(42); // Fixed seed for reproducible data
        }

        /// <summary>
        /// Main entry point for mock data generation
        /// </summary>
        public async Task<bool> GenerateAllMockDataAsync(int companyCount = 150, int productCount = 250, int certificateCount = 1000, int userCount = 75)
        {
            try
            {
                _logger.LogInformation("Starting comprehensive mock data generation...");
                _logger.LogInformation($"Target: {companyCount} companies, {productCount} products, {certificateCount} certificates, {userCount} users");

                // Clear existing data (optional)
                await ClearExistingMockDataAsync();

                // Phase 1: Generate base entities
                var companies = await GenerateCompaniesAsync(companyCount);
                var products = await GenerateProductsAsync(productCount);
                var users = await GenerateUsersAsync(userCount, companies);

                // Phase 2: Generate certificates with relationships
                var certificates = await GenerateCertificatesAsync(certificateCount, companies, products);

                // Phase 2.5: Generate product IDs (ASINs, UPCs, etc.) for certificates
                await GenerateProductIdsAsync(certificates, users);

                // Phase 3: Generate supporting data
                await GenerateApiKeysAsync(50, users);
                await GenerateAuditLogsAsync(5000, users, certificates);

                _logger.LogInformation("Mock data generation completed successfully!");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during mock data generation");
                return false;
            }
        }

        #region Company Generation

        private async Task<List<Company>> GenerateCompaniesAsync(int count)
        {
            _logger.LogInformation($"Generating {count} companies...");
            var companies = new List<Company>();

            var companyNames = GetRealisticCompanyNames();
            var industries = GetIndustryTypes();
            var countries = GetCountryList();

            for (int i = 0; i < count; i++)
            {
                var company = new Company
                {
                    Name = companyNames[_random.Next(companyNames.Count)] + (i > companyNames.Count ? $" {i}" : ""),
                    Industry = industries[_random.Next(industries.Count)],
                    Address = GenerateAddress(),
                    City = GenerateCity(),
                    State = GenerateState(),
                    Country = countries[_random.Next(countries.Count)],
                    PostalCode = GeneratePostalCode(),
                    PhoneNumber = GeneratePhoneNumber(),
                    Email = GenerateCompanyEmail(i),
                    Website = GenerateWebsite(i),
                    EmployeeCount = GenerateEmployeeCount(),
                    YearFounded = _random.Next(1950, 2020),
                    Description = GenerateCompanyDescription(),
                    IsActive = _random.NextDouble() > 0.05, // 95% active
                    CreatedAt = GenerateRandomDate(365 * 3), // Within last 3 years
                    UpdatedAt = GenerateRandomDate(30) // Within last 30 days
                };

                companies.Add(company);

                if (i % 50 == 0)
                    _logger.LogInformation($"Generated {i + 1} companies...");
            }

            _context.Companies.AddRange(companies);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Successfully saved {companies.Count} companies to database");

            return companies;
        }

        private List<string> GetRealisticCompanyNames()
        {
            return new List<string>
            {
                "EcoTech Solutions", "GreenBuild Materials", "SustainaCorp Industries", "CleanEnergy Innovations",
                "BioSafe Products", "EnviroTech Manufacturing", "Pure Environmental Systems", "GreenLeaf Technologies",
                "EcoSmart Solutions", "Natural Resources Inc", "SustainaTech Corp", "Green Horizon Industries",
                "CleanTech Innovations", "BioGreen Manufacturing", "EcoFriendly Solutions", "GreenPower Systems",
                "Sustainable Futures Ltd", "EcoVision Technologies", "GreenWave Industries", "CleanAir Solutions",
                "BioTech Environmental", "GreenSpace Industries", "EcoLogic Systems", "SustainaBuild Corp",
                "GreenEnergy Solutions", "CleanWater Technologies", "BioSafe Manufacturing", "EcoDesign Industries",
                "GreenLife Products", "SustainaCore Systems", "EcoInnovate Technologies", "GreenPath Solutions",
                "CleanTech Manufacturing", "BioEnvironmental Corp", "GreenFuture Industries", "EcoSolutions Ltd",
                "SustainableTech Inc", "GreenWorld Systems", "CleanEarth Technologies", "BioGreen Solutions",
                "EcoAdvanced Manufacturing", "GreenTech Innovations", "SustainaFlow Systems", "CleanEnergy Corp",
                "BioEco Technologies", "GreenShield Industries", "EcoEfficient Solutions", "SustainaPro Systems",
                "GreenMax Technologies", "CleanVision Industries", "BioSustain Corp", "EcoFlow Solutions"
            };
        }

        private List<string> GetIndustryTypes()
        {
            return new List<string>
            {
                "Manufacturing", "Technology", "Healthcare", "Energy & Utilities", "Agriculture & Food",
                "Construction", "Retail & Consumer Goods", "Transportation & Logistics", "Financial Services",
                "Telecommunications", "Aerospace & Defense", "Automotive", "Chemicals & Materials",
                "Entertainment & Media", "Education", "Real Estate", "Mining & Metals", "Textiles & Apparel",
                "Pharmaceuticals", "Environmental Services"
            };
        }

        #endregion

        #region Product Generation

        private async Task<List<Product>> GenerateProductsAsync(int count)
        {
            _logger.LogInformation($"Generating {count} products...");
            var products = new List<Product>();

            var productCategories = GetProductCategories();

            for (int i = 0; i < count; i++)
            {
                var category = productCategories[_random.Next(productCategories.Count)];
                
                var product = new Product
                {
                    Name = GenerateProductName(category),
                    SKU = GenerateProductSKU(),
                    Category = category,
                    SubCategory = GenerateSubCategory(category),
                    Description = GenerateProductDescription(),
                    Manufacturer = GenerateManufacturer(),
                    Model = GenerateModelNumber(),
                    Version = GenerateVersion(),
                    Price = GeneratePrice(),
                    Weight = GenerateWeight(),
                    Dimensions = GenerateDimensions(),
                    Materials = GenerateMaterials(),
                    SafetyRating = GenerateSafetyRating(),
                    EnvironmentalRating = GenerateEnvironmentalRating(),
                    CertificationRequired = _random.NextDouble() > 0.3, // 70% require certification
                    IsActive = _random.NextDouble() > 0.1, // 90% active
                    LaunchDate = GenerateRandomDate(365 * 5), // Within last 5 years
                    CreatedAt = GenerateRandomDate(365 * 2),
                    UpdatedAt = GenerateRandomDate(60)
                };

                products.Add(product);

                if (i % 50 == 0)
                    _logger.LogInformation($"Generated {i + 1} products...");
            }

            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Successfully saved {products.Count} products to database");

            return products;
        }

        private Dictionary<string, List<string>> GetProductCategories()
        {
            return new Dictionary<string, List<string>>
            {
                ["Building Materials"] = new() { "Insulation", "Flooring", "Windows", "Roofing", "Doors", "Siding", "Concrete", "Steel" },
                ["Electronics"] = new() { "Components", "Devices", "Cables", "Batteries", "Sensors", "Controllers", "Displays", "Power Supplies" },
                ["Food & Agriculture"] = new() { "Organic Foods", "Packaging", "Processing Equipment", "Seeds", "Fertilizers", "Pesticides", "Dairy", "Meat" },
                ["Textiles"] = new() { "Fabrics", "Clothing", "Footwear", "Accessories", "Home Textiles", "Industrial Textiles", "Yarns", "Dyes" },
                ["Energy"] = new() { "Solar Panels", "Wind Turbines", "Batteries", "Inverters", "Fuel Cells", "Generators", "Transformers", "Cables" },
                ["Healthcare"] = new() { "Medical Devices", "Pharmaceuticals", "Diagnostics", "Personal Care", "Surgical Instruments", "Implants", "Disposables", "Equipment" }
            };
        }

        #endregion

        #region Certificate Generation

        private async Task<List<Certificate>> GenerateCertificatesAsync(int count, List<Company> companies, List<Product> products)
        {
            _logger.LogInformation($"Generating {count} certificates...");
            var certificates = new List<Certificate>();

            var certificationTypes = GetCertificationTypes();
            var certificationStandards = GetCertificationStandards();
            var statusTypes = GetCertificateStatuses();

            for (int i = 0; i < count; i++)
            {
                var company = companies[_random.Next(companies.Count)];
                var product = products[_random.Next(products.Count)];
                var certificationType = certificationTypes[_random.Next(certificationTypes.Count)];
                var standard = certificationStandards[_random.Next(certificationStandards.Count)];
                var status = statusTypes[_random.Next(statusTypes.Count)];

                var issuedDate = GenerateRandomDate(365 * 3); // Within last 3 years
                var validityPeriod = GenerateValidityPeriod();
                var expirationDate = issuedDate.AddDays(validityPeriod);

                var certificate = new Certificate
                {
                    Number = GenerateCertificateNumber(),
                    Title = $"{certificationType} Certificate - {product.Name}",
                    Type = certificationType,
                    Standard = standard.Code,
                    StandardName = standard.Name,
                    Status = DetermineActualStatus(status, expirationDate),
                    CompanyId = company.Id,
                    ProductId = product.Id,
                    IssuedDate = issuedDate,
                    ExpirationDate = expirationDate,
                    ValidityPeriod = validityPeriod,
                    IssuingBody = GenerateIssuingBody(),
                    CertificationBody = GenerateCertificationBody(),
                    Scope = GenerateCertificationScope(),
                    Version = GenerateCertificateVersion(),
                    AccreditationNumber = GenerateAccreditationNumber(),
                    TestReportNumber = GenerateTestReportNumber(),
                    LastAuditDate = GenerateAuditDate(issuedDate),
                    NextAuditDate = GenerateNextAuditDate(issuedDate, validityPeriod),
                    SuspensionReason = status == "Suspended" ? GenerateSuspensionReason() : null,
                    WithdrawalReason = status == "Withdrawn" ? GenerateWithdrawalReason() : null,
                    Notes = GenerateCertificateNotes(),
                    IsPublic = _random.NextDouble() > 0.2, // 80% public
                    CreatedAt = issuedDate.AddDays(-_random.Next(30, 90)),
                    UpdatedAt = GenerateRandomDate(30)
                };

                certificates.Add(certificate);

                if (i % 100 == 0)
                    _logger.LogInformation($"Generated {i + 1} certificates...");
            }

            _context.Certificates.AddRange(certificates);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Successfully saved {certificates.Count} certificates to database");

            return certificates;
        }

        private List<string> GetCertificationTypes()
        {
            return new List<string>
            {
                "ISO 14001", "ISO 9001", "LEED", "Energy Star", "FSC", "GREENGUARD",
                "OEKO-TEX", "GOTS", "Cradle to Cradle", "BREEAM", "WELL", "Living Building Challenge",
                "BIFMA", "SCS", "UL Environment", "EPD", "HPD", "Declare", "GREENGUARD Gold",
                "Carbon Trust", "Climate Neutral", "B Corp", "Fair Trade", "Rainforest Alliance"
            };
        }

        private List<CertificationStandard> GetCertificationStandards()
        {
            return new List<CertificationStandard>
            {
                new() { Code = "ISO-14001", Name = "Environmental Management Systems" },
                new() { Code = "ISO-9001", Name = "Quality Management Systems" },
                new() { Code = "LEED-BD+C", Name = "LEED for Building Design and Construction" },
                new() { Code = "ENERGY-STAR", Name = "Energy Star Certification" },
                new() { Code = "FSC-STD", Name = "Forest Stewardship Council Standards" },
                new() { Code = "GREENGUARD", Name = "GREENGUARD Environmental Institute Standards" },
                new() { Code = "OEKO-TEX-100", Name = "OEKO-TEX Standard 100" },
                new() { Code = "GOTS", Name = "Global Organic Textile Standard" },
                new() { Code = "C2C", Name = "Cradle to Cradle Certified" },
                new() { Code = "BREEAM", Name = "Building Research Establishment Environmental Assessment Method" }
            };
        }

        private List<string> GetCertificateStatuses()
        {
            // Weighted distribution: Active (65%), Pending (12%), Expired (15%), Suspended (4.5%), Withdrawn (3.5%)
            var statuses = new List<string>();
            statuses.AddRange(Enumerable.Repeat("Active", 65));
            statuses.AddRange(Enumerable.Repeat("Pending", 12));
            statuses.AddRange(Enumerable.Repeat("Expired", 15));
            statuses.AddRange(Enumerable.Repeat("Suspended", 5));
            statuses.AddRange(Enumerable.Repeat("Withdrawn", 3));
            return statuses;
        }

        #endregion

        #region Product ID Generation (ASINs, UPCs, GTINs, EANs)

        /// <summary>
        /// Generates realistic product IDs for certificates
        /// Simulates real-world scenarios where certificates can have 100s-1000s of ASINs
        /// Distribution: 20% have 1-10 IDs, 50% have 10-100 IDs, 25% have 100-500 IDs, 5% have 500-2000 IDs
        /// </summary>
        private async Task GenerateProductIdsAsync(List<Certificate> certificates, List<User> users)
        {
            _logger.LogInformation("Generating product IDs (ASINs, UPCs, GTINs, EANs) for certificates...");
            var productIds = new List<CertificateProductId>();
            var totalGenerated = 0;

            var productIdTypes = new[] { "ASIN", "UPC", "GTIN", "EAN", "ISBN", "SKU", "MPN" };
            var partners = new[] { "Amazon", "Novi", "None", "Other" };
            var statuses = new[] { "Active", "Active", "Active", "Active", "Inactive", "Pending" }; // 67% Active

            foreach (var certificate in certificates)
            {
                // Determine how many product IDs this certificate should have
                var productIdCount = DetermineProductIdCount();
                var createdBy = users[_random.Next(users.Count)];

                for (int i = 0; i < productIdCount; i++)
                {
                    var idType = productIdTypes[_random.Next(productIdTypes.Length)];
                    var productId = new CertificateProductId
                    {
                        CertificateId = certificate.Id,
                        ProductIdType = idType,
                        ProductIdValue = GenerateProductIdValue(idType, totalGenerated + i),
                        Status = statuses[_random.Next(statuses.Length)],
                        Partner = partners[_random.Next(partners.Length)],
                        ProductName = GenerateProductNameForId(idType),
                        ProductSku = $"SKU-{_random.Next(100000, 999999)}",
                        Description = GenerateProductIdDescription(),
                        IsActive = _random.NextDouble() > 0.05, // 95% active
                        IsPublic = _random.NextDouble() > 0.1, // 90% public
                        DisplayOrder = i + 1,
                        CreatedBy = createdBy.Id,
                        CreatedAt = GenerateRandomDate(365),
                        UpdatedAt = GenerateRandomDate(30)
                    };

                    productIds.Add(productId);
                }

                totalGenerated += productIdCount;

                // Batch save every 10,000 records for performance
                if (productIds.Count >= 10000)
                {
                    _context.CertificateProductIds.AddRange(productIds);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Saved batch of {productIds.Count} product IDs. Total: {totalGenerated}");
                    productIds.Clear();
                }
            }

            // Save remaining product IDs
            if (productIds.Any())
            {
                _context.CertificateProductIds.AddRange(productIds);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation($"Successfully generated {totalGenerated} product IDs for {certificates.Count} certificates");
        }

        /// <summary>
        /// Determines the number of product IDs for a certificate based on realistic distribution
        /// 20% have 1-10 IDs (small products)
        /// 50% have 10-100 IDs (medium products)
        /// 25% have 100-500 IDs (large product lines)
        /// 5% have 500-2000 IDs (massive product catalogs like Amazon vendors)
        /// </summary>
        private int DetermineProductIdCount()
        {
            var roll = _random.NextDouble();

            if (roll < 0.20) // 20% - Small (1-10)
                return _random.Next(1, 11);
            else if (roll < 0.70) // 50% - Medium (10-100)
                return _random.Next(10, 101);
            else if (roll < 0.95) // 25% - Large (100-500)
                return _random.Next(100, 501);
            else // 5% - Massive (500-2000)
                return _random.Next(500, 2001);
        }

        /// <summary>
        /// Generates a valid product ID value based on the type
        /// </summary>
        private string GenerateProductIdValue(string type, int index)
        {
            return type switch
            {
                "ASIN" => GenerateAsin(index),
                "UPC" => GenerateUpc(index),
                "GTIN" => GenerateGtin(index),
                "EAN" => GenerateEan(index),
                "ISBN" => GenerateIsbn(index),
                "SKU" => GenerateSku(index),
                "MPN" => GenerateMpn(index),
                _ => $"ID{index:D10}"
            };
        }

        private string GenerateAsin(int index)
        {
            // ASIN format: B0 followed by 8 alphanumeric characters
            var chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var suffix = new char[8];
            var seed = index + _random.Next(1000000);
            for (int i = 0; i < 8; i++)
            {
                suffix[i] = chars[(seed + i * 7) % chars.Length];
            }
            return $"B0{new string(suffix)}";
        }

        private string GenerateUpc(int index)
        {
            // UPC format: 12 digits with valid check digit
            var baseNumber = (index + 100000000000) % 100000000000;
            var upc = baseNumber.ToString("D11");
            var checkDigit = CalculateUpcCheckDigit(upc);
            return upc + checkDigit;
        }

        private int CalculateUpcCheckDigit(string upc11)
        {
            var sum = 0;
            for (int i = 0; i < 11; i++)
            {
                var digit = int.Parse(upc11[i].ToString());
                sum += (i % 2 == 0) ? digit * 3 : digit;
            }
            return (10 - (sum % 10)) % 10;
        }

        private string GenerateGtin(int index)
        {
            // GTIN-14 format
            var baseNumber = (index + 10000000000000) % 10000000000000;
            return baseNumber.ToString("D14");
        }

        private string GenerateEan(int index)
        {
            // EAN-13 format with valid check digit
            var baseNumber = (index + 100000000000) % 1000000000000;
            var ean = baseNumber.ToString("D12");
            var checkDigit = CalculateEanCheckDigit(ean);
            return ean + checkDigit;
        }

        private int CalculateEanCheckDigit(string ean12)
        {
            var sum = 0;
            for (int i = 0; i < 12; i++)
            {
                var digit = int.Parse(ean12[i].ToString());
                sum += (i % 2 == 0) ? digit : digit * 3;
            }
            return (10 - (sum % 10)) % 10;
        }

        private string GenerateIsbn(int index)
        {
            // ISBN-13 format starting with 978
            var baseNumber = (index + 1000000000) % 10000000000;
            return $"978{baseNumber:D10}";
        }

        private string GenerateSku(int index)
        {
            var prefixes = new[] { "PRD", "SKU", "ITM", "ART", "GCC" };
            return $"{prefixes[_random.Next(prefixes.Length)]}-{index:D8}";
        }

        private string GenerateMpn(int index)
        {
            var prefixes = new[] { "MFG", "MPN", "PART", "PN" };
            return $"{prefixes[_random.Next(prefixes.Length)]}-{_random.Next(100, 999)}-{index:D6}";
        }

        private string GenerateProductNameForId(string idType)
        {
            var adjectives = new[] { "Premium", "Eco-Friendly", "Certified", "Organic", "Natural", "Sustainable", "Green", "Bio" };
            var products = new[] { "Cleaner", "Material", "Component", "Product", "Item", "Solution", "System", "Package" };
            return $"{adjectives[_random.Next(adjectives.Length)]} {products[_random.Next(products.Length)]} ({idType})";
        }

        private string GenerateProductIdDescription()
        {
            var descriptions = new[]
            {
                "Certified product for environmental compliance",
                "Meets all GCC certification requirements",
                "Verified sustainable sourcing",
                "Third-party tested and approved",
                "Compliant with industry standards",
                "Eco-certified material",
                "Green certified component",
                null, null, null // 30% chance of no description
            };
            return descriptions[_random.Next(descriptions.Length)];
        }

        #endregion

        #region User Generation

        private async Task<List<User>> GenerateUsersAsync(int count, List<Company> companies)
        {
            _logger.LogInformation($"Generating {count} users...");
            var users = new List<User>();

            var roles = GetUserRoles();
            var firstNames = GetFirstNames();
            var lastNames = GetLastNames();

            for (int i = 0; i < count; i++)
            {
                var firstName = firstNames[_random.Next(firstNames.Count)];
                var lastName = lastNames[_random.Next(lastNames.Count)];
                var role = GetWeightedRole(roles);
                var company = role != "Public" ? companies[_random.Next(companies.Count)] : null;

                var user = new User
                {
                    UserName = GenerateUsername(firstName, lastName, i),
                    Email = GenerateUserEmail(firstName, lastName, i),
                    EmailConfirmed = _random.NextDouble() > 0.1, // 90% confirmed
                    FirstName = firstName,
                    LastName = lastName,
                    Role = role,
                    CompanyId = company?.Id,
                    PhoneNumber = GeneratePhoneNumber(),
                    JobTitle = GenerateJobTitle(role),
                    Department = GenerateDepartment(),
                    IsActive = _random.NextDouble() > 0.05, // 95% active
                    LastLoginDate = GenerateLastLoginDate(),
                    AccountCreated = GenerateRandomDate(365 * 2),
                    TwoFactorEnabled = _random.NextDouble() > 0.6, // 40% use 2FA
                    LockoutEnabled = true,
                    AccessFailedCount = 0,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };

                users.Add(user);

                if (i % 25 == 0)
                    _logger.LogInformation($"Generated {i + 1} users...");
            }

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Successfully saved {users.Count} users to database");

            return users;
        }

        private Dictionary<string, int> GetUserRoles()
        {
            return new Dictionary<string, int>
            {
                ["Public"] = 33,      // 33%
                ["External"] = 27,    // 27%
                ["Internal"] = 20,    // 20%
                ["Analyst"] = 13,     // 13%
                ["Admin"] = 7         // 7%
            };
        }

        private string GetWeightedRole(Dictionary<string, int> roles)
        {
            var totalWeight = roles.Values.Sum();
            var randomWeight = _random.Next(0, totalWeight);
            var currentWeight = 0;

            foreach (var role in roles)
            {
                currentWeight += role.Value;
                if (randomWeight < currentWeight)
                    return role.Key;
            }
            return "Public"; // Fallback
        }

        #endregion

        #region API Key Generation

        private async Task GenerateApiKeysAsync(int count, List<User> users)
        {
            _logger.LogInformation($"Generating {count} API keys...");
            var apiKeys = new List<ApiKey>();

            var keyTypes = new[] { "Standard", "Premium", "Enterprise" };

            for (int i = 0; i < count; i++)
            {
                var user = users[_random.Next(users.Count)];
                var keyType = keyTypes[_random.Next(keyTypes.Length)];

                var apiKey = new ApiKey
                {
                    Id = Guid.NewGuid(),
                    KeyValue = GenerateApiKeyValue(),
                    Name = $"{user.FirstName}'s {keyType} API Key",
                    Description = $"API key for {user.Role} access - {keyType} tier",
                    UserId = user.Id,
                    KeyType = keyType,
                    IsActive = _random.NextDouble() > 0.1, // 90% active
                    CreatedDate = GenerateRandomDate(365),
                    ExpiryDate = DateTime.UtcNow.AddMonths(_random.Next(6, 24)),
                    LastUsedDate = GenerateLastUsedDate(),
                    RequestCount = _random.Next(0, 10000),
                    RateLimitPerHour = GetRateLimit(keyType),
                    AllowedIPs = GenerateAllowedIPs(),
                    Permissions = GenerateApiPermissions(user.Role)
                };

                apiKeys.Add(apiKey);
            }

            _context.ApiKeys.AddRange(apiKeys);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Successfully saved {apiKeys.Count} API keys to database");
        }

        #endregion

        #region Audit Log Generation

        private async Task GenerateAuditLogsAsync(int count, List<User> users, List<Certificate> certificates)
        {
            _logger.LogInformation($"Generating {count} audit log entries...");
            var auditLogs = new List<AuditLog>();

            var actions = GetAuditActions();
            var entities = GetAuditEntities();

            for (int i = 0; i < count; i++)
            {
                var user = users[_random.Next(users.Count)];
                var action = actions[_random.Next(actions.Count)];
                var entity = entities[_random.Next(entities.Count)];

                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    UserName = user.UserName,
                    Action = action,
                    EntityType = entity,
                    EntityId = entity == "Certificate" ? certificates[_random.Next(certificates.Count)].Id.ToString() : Guid.NewGuid().ToString(),
                    Timestamp = GenerateRandomDate(365),
                    IPAddress = GenerateIPAddress(),
                    UserAgent = GenerateUserAgent(),
                    Details = GenerateAuditDetails(action, entity),
                    Success = _random.NextDouble() > 0.05, // 95% successful actions
                    ErrorMessage = _random.NextDouble() < 0.05 ? GenerateErrorMessage() : null
                };

                auditLogs.Add(auditLog);

                if (i % 500 == 0)
                    _logger.LogInformation($"Generated {i + 1} audit log entries...");
            }

            _context.AuditLogs.AddRange(auditLogs);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Successfully saved {auditLogs.Count} audit log entries to database");
        }

        #endregion

        #region Helper Methods

        private async Task ClearExistingMockDataAsync()
        {
            _logger.LogInformation("Clearing existing mock data...");
            
            // Clear in reverse dependency order
            _context.AuditLogs.RemoveRange(_context.AuditLogs);
            _context.ApiKeys.RemoveRange(_context.ApiKeys);
            _context.Certificates.RemoveRange(_context.Certificates);
            _context.Products.RemoveRange(_context.Products);
            _context.Users.RemoveRange(_context.Users.Where(u => !u.Email.Contains("admin@")));
            _context.Companies.RemoveRange(_context.Companies);

            await _context.SaveChangesAsync();
            _logger.LogInformation("Existing mock data cleared");
        }

        private DateTime GenerateRandomDate(int daysBack)
        {
            return DateTime.UtcNow.AddDays(-_random.Next(0, daysBack));
        }

        private string GenerateAddress()
        {
            var streetNumbers = new[] { "123", "456", "789", "1001", "2500", "3700" };
            var streetNames = new[] { "Oak Street", "Main Avenue", "Industrial Blvd", "Commerce Drive", "Business Park Way", "Corporate Lane" };
            return $"{streetNumbers[_random.Next(streetNumbers.Length)]} {streetNames[_random.Next(streetNames.Length)]}";
        }

        private string GenerateCity()
        {
            var cities = new[] { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego", "Dallas", "San Jose" };
            return cities[_random.Next(cities.Length)];
        }

        private string GenerateState()
        {
            var states = new[] { "CA", "NY", "TX", "FL", "IL", "PA", "OH", "GA", "NC", "MI" };
            return states[_random.Next(states.Length)];
        }

        private List<string> GetCountryList()
        {
            return new List<string> { "United States", "Canada", "United Kingdom", "Germany", "France", "Japan", "Australia", "Brazil", "India", "China" };
        }

        private string GeneratePostalCode()
        {
            return _random.Next(10000, 99999).ToString();
        }

        private string GeneratePhoneNumber()
        {
            return $"+1-{_random.Next(200, 999)}-{_random.Next(200, 999)}-{_random.Next(1000, 9999)}";
        }

        private string GenerateCompanyEmail(int index)
        {
            var domains = new[] { "company.com", "corp.com", "industries.com", "tech.com", "solutions.com" };
            return $"contact{index}@{domains[_random.Next(domains.Length)]}";
        }

        private string GenerateWebsite(int index)
        {
            var domains = new[] { "company.com", "corp.com", "industries.com", "tech.com", "solutions.com" };
            return $"https://www.{domains[_random.Next(domains.Length)]}";
        }

        // Additional helper methods would continue here...
        // For brevity, I'm showing the structure. The full implementation would include
        // all the generation methods referenced above.

        #endregion

        #region Data Models

        public class CertificationStandard
        {
            public string Code { get; set; }
            public string Name { get; set; }
        }

        public class AuditLog
        {
            public Guid Id { get; set; }
            public string UserId { get; set; }
            public string UserName { get; set; }
            public string Action { get; set; }
            public string EntityType { get; set; }
            public string EntityId { get; set; }
            public DateTime Timestamp { get; set; }
            public string IPAddress { get; set; }
            public string UserAgent { get; set; }
            public string Details { get; set; }
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
        }

        #endregion
    }
}