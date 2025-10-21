using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using GCCWebApp.Models;

namespace GCCWebApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Core entities
        public DbSet<Certificate> Certificates { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }

        // Certificate related entities
        public DbSet<CertificationType> CertificationTypes { get; set; }
        public DbSet<CustomField> CustomFields { get; set; }
        public DbSet<CustomFieldValue> CustomFieldValues { get; set; }
        public DbSet<CertificateDocument> CertificateDocuments { get; set; }
        public DbSet<CertificateProductId> CertificateProductIds { get; set; }

        // Product related entities
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<ProductType> ProductTypes { get; set; }
        public DbSet<ProductSubcategory> ProductSubcategories { get; set; }
        public DbSet<ProductIdentifier> ProductIdentifiers { get; set; }

        // Tags and attributes
        public DbSet<Tag> Tags { get; set; }

        // User management
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Partner> Partners { get; set; }

        // API management
        public DbSet<ApiKey> ApiKeys { get; set; }

        // ETL entities
        public DbSet<ETLConfiguration> ETLConfigurations { get; set; }
        public DbSet<ETLLogEntry> ETLLogEntries { get; set; }
        public DbSet<ETLAlert> ETLAlerts { get; set; }
        public DbSet<ETLDataQualityIssue> ETLDataQualityIssues { get; set; }
        public DbSet<CertificateAggregateData> CertificateAggregates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Certificate configuration
            modelBuilder.Entity<Certificate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Number).IsUnique();
                
                // Performance indexes for common search patterns
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ExpirationDate);
                entity.HasIndex(e => e.EffectiveDate);
                entity.HasIndex(e => e.Country);
                entity.HasIndex(e => e.CompanyId);
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.CertificationTypeId);
                entity.HasIndex(e => e.CreatedAt);
                
                // Composite indexes for complex queries
                entity.HasIndex(e => new { e.Status, e.ExpirationDate });
                entity.HasIndex(e => new { e.CompanyId, e.Status });
                entity.HasIndex(e => new { e.ProductId, e.Status });
                entity.HasIndex(e => new { e.Country, e.Status });
                entity.HasIndex(e => new { e.CertificationTypeId, e.Status });
                entity.HasIndex(e => new { e.ExpirationDate, e.Status, e.CompanyId });
                
                // Relationships
                entity.HasOne(e => e.Company)
                    .WithMany(c => c.Certificates)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.Certificates)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CertificationType)
                    .WithMany(ct => ct.Certificates)
                    .HasForeignKey(e => e.CertificationTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedByUser)
                    .WithMany(u => u.CreatedCertificates)
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany(u => u.UpdatedCertificates)
                    .HasForeignKey(e => e.UpdatedBy)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Company configuration
            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Explicitly configure Name column for SQL Server compatibility
                entity.Property(e => e.Name)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.HasIndex(e => e.Name).IsUnique();
                
                // Performance indexes for company searches
                entity.HasIndex(e => e.Country);
                entity.HasIndex(e => e.Website);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.CreatedAt);
                
                // Composite indexes
                entity.HasIndex(e => new { e.Country, e.IsActive });
                entity.HasIndex(e => new { e.Name, e.Country });
            });

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Performance indexes for product searches
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.CategoryId);
                entity.HasIndex(e => e.ProductTypeId);
                entity.HasIndex(e => e.CompanyId);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.CreatedAt);
                
                // Composite indexes for complex queries
                entity.HasIndex(e => new { e.CategoryId, e.CompanyId });
                entity.HasIndex(e => new { e.ProductTypeId, e.IsActive });
                entity.HasIndex(e => new { e.CompanyId, e.IsActive });
                
                entity.HasOne(e => e.Category)
                    .WithMany(pc => pc.Products)
                    .HasForeignKey(e => e.CategoryId);

                entity.HasOne(e => e.ProductType)
                    .WithMany(pt => pt.Products)
                    .HasForeignKey(e => e.ProductTypeId);

                entity.HasOne(e => e.ProductSubcategory)
                    .WithMany(ps => ps.Products)
                    .HasForeignKey(e => e.ProductSubcategoryId);

                entity.HasOne(e => e.Company)
                    .WithMany(c => c.Products)
                    .HasForeignKey(e => e.CompanyId);
            });

            // Product identifier configuration
            modelBuilder.Entity<ProductIdentifier>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ProductId, e.IdentifierType, e.IdentifierValue }).IsUnique();
                
                entity.HasOne(e => e.Product)
                    .WithMany(p => p.ProductIdentifiers)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Product category configuration
            modelBuilder.Entity<ProductCategory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Product type configuration
            modelBuilder.Entity<ProductType>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.Category)
                    .WithMany(pc => pc.ProductTypes)
                    .HasForeignKey(e => e.CategoryId);

                entity.HasOne(e => e.Parent)
                    .WithMany(pt => pt.Children)
                    .HasForeignKey(e => e.ParentId);
            });

            // Product subcategory configuration
            modelBuilder.Entity<ProductSubcategory>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.ProductType)
                    .WithMany(pt => pt.ProductSubcategories)
                    .HasForeignKey(e => e.ProductTypeId);
            });

            // Certification type configuration
            modelBuilder.Entity<CertificationType>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Custom field configuration
            modelBuilder.Entity<CustomField>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.CertificationType)
                    .WithMany(ct => ct.CustomFields)
                    .HasForeignKey(e => e.CertificateTypeId);
            });

            // Custom field value configuration
            modelBuilder.Entity<CustomFieldValue>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.HasOne(e => e.Certificate)
                    .WithMany(c => c.CustomFieldValues)
                    .HasForeignKey(e => e.CertificateId);

                entity.HasOne(e => e.Field)
                    .WithMany(cf => cf.CustomFieldValues)
                    .HasForeignKey(e => e.FieldId);
            });

            // Certificate document configuration
            modelBuilder.Entity<CertificateDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CertificateId);
                entity.HasIndex(e => e.FileHash);

                entity.HasOne(e => e.Certificate)
                    .WithMany(c => c.Documents)
                    .HasForeignKey(e => e.CertificateId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.UploadedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UploadedBy)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Certificate product ID configuration
            modelBuilder.Entity<CertificateProductId>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Performance indexes for product ID searches
                entity.HasIndex(e => e.CertificateId);
                entity.HasIndex(e => e.ProductIdType);
                entity.HasIndex(e => e.ProductIdValue);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Partner);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.CreatedAt);

                // Composite indexes for complex queries
                entity.HasIndex(e => new { e.CertificateId, e.ProductIdType });
                entity.HasIndex(e => new { e.ProductIdType, e.ProductIdValue }).IsUnique();
                entity.HasIndex(e => new { e.Partner, e.Status, e.IsActive });
                entity.HasIndex(e => new { e.CertificateId, e.IsActive });
                entity.HasIndex(e => new { e.Status, e.IsActive, e.CreatedAt });

                // Relationships
                entity.HasOne(e => e.Certificate)
                    .WithMany(c => c.ProductIds)
                    .HasForeignKey(e => e.CertificateId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedBy)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                
                // Performance indexes for user queries  
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.LastLoginAt);
                
                // Composite indexes
                entity.HasIndex(e => new { e.Status, e.IsActive });
                entity.HasIndex(e => new { e.IsActive, e.CreatedAt });
            });

            // Role configuration
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Permission configuration
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Codename).IsUnique();
            });

            // Tag configuration
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Partner configuration
            modelBuilder.Entity<Partner>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
                
                // Performance indexes for partner searches
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.SortOrder);
                entity.HasIndex(e => e.CreatedAt);
                
                // Composite indexes
                entity.HasIndex(e => new { e.IsActive, e.SortOrder });
            });
            
            // ApiKey configuration
            modelBuilder.Entity<ApiKey>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Explicitly configure KeyHash for SQL Server compatibility
                entity.Property(e => e.KeyHash)
                    .HasMaxLength(450); // SQL Server max for indexed nvarchar columns

                // Configure decimal property with precision and scale
                entity.Property(e => e.UsageCostPerRequest)
                    .HasPrecision(18, 6); // 18 total digits, 6 after decimal point

                entity.HasIndex(e => e.KeyHash).IsUnique();

                // Performance indexes for API key validation
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.CreatedBy);
                entity.HasIndex(e => e.CreatedAt);

                // Composite indexes for API authentication
                entity.HasIndex(e => new { e.KeyHash, e.IsActive, e.ExpiresAt });
                entity.HasIndex(e => new { e.CreatedBy, e.IsActive });
            });

            // Many-to-many relationships
            modelBuilder.Entity<Certificate>()
                .HasMany(c => c.Tags)
                .WithMany(t => t.Certificates);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.Tags)
                .WithMany(t => t.Products);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Roles)
                .WithMany(r => r.Users);

            modelBuilder.Entity<Role>()
                .HasMany(r => r.Permissions)
                .WithMany(p => p.Roles);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Companies)
                .WithMany(c => c.Users);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Partners)
                .WithMany(p => p.Users);

            // ETL Configuration
            modelBuilder.Entity<ETLConfiguration>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name);

                var stringListComparer = new ValueComparer<List<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList());

                entity.Property(e => e.DataSources)
                    .HasConversion(
                        v => string.Join(',', v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                    .Metadata.SetValueComparer(stringListComparer);

                entity.Property(e => e.Transformations)
                    .HasConversion(
                        v => string.Join(',', v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                    .Metadata.SetValueComparer(stringListComparer);

                entity.Property(e => e.Destinations)
                    .HasConversion(
                        v => string.Join(',', v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                    .Metadata.SetValueComparer(stringListComparer);

                var dictionaryComparer = new ValueComparer<Dictionary<string, object>>(
                    (c1, c2) => c1 != null && c2 != null && c1.Count == c2.Count && c1.Keys.All(k => c2.ContainsKey(k) && Equals(c1[k], c2[k])),
                    c => c != null ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value != null ? v.Value.GetHashCode() : 0)) : 0,
                    c => c != null ? new Dictionary<string, object>(c) : new Dictionary<string, object>());

                entity.Property(e => e.Settings)
                    .HasColumnType("TEXT")
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
                    .Metadata.SetValueComparer(dictionaryComparer);
            });

            // ETL Log Entry
            modelBuilder.Entity<ETLLogEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.JobId);
                entity.HasIndex(e => e.Timestamp);

                var dictionaryComparer = new ValueComparer<Dictionary<string, object>>(
                    (c1, c2) => c1 != null && c2 != null && c1.Count == c2.Count && c1.Keys.All(k => c2.ContainsKey(k) && Equals(c1[k], c2[k])),
                    c => c != null ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value != null ? v.Value.GetHashCode() : 0)) : 0,
                    c => c != null ? new Dictionary<string, object>(c) : new Dictionary<string, object>());

                entity.Property(e => e.Properties)
                    .HasColumnType("TEXT")
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
                    .Metadata.SetValueComparer(dictionaryComparer);
            });

            // ETL Alert
            modelBuilder.Entity<ETLAlert>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.IsResolved);

                var dictionaryComparer = new ValueComparer<Dictionary<string, object>>(
                    (c1, c2) => c1 != null && c2 != null && c1.Count == c2.Count && c1.Keys.All(k => c2.ContainsKey(k) && Equals(c1[k], c2[k])),
                    c => c != null ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value != null ? v.Value.GetHashCode() : 0)) : 0,
                    c => c != null ? new Dictionary<string, object>(c) : new Dictionary<string, object>());

                entity.Property(e => e.Context)
                    .HasColumnType("TEXT")
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
                    .Metadata.SetValueComparer(dictionaryComparer);
            });

            // ETL Data Quality Issue
            modelBuilder.Entity<ETLDataQualityIssue>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.DetectedAt);
                entity.HasIndex(e => e.IssueType);
                entity.Property(e => e.ExpectedValue)
                    .HasColumnType("TEXT")
                    .HasConversion(
                        v => v != null ? System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null) : null,
                        v => v != null ? System.Text.Json.JsonSerializer.Deserialize<object>(v, (System.Text.Json.JsonSerializerOptions?)null) : null);
                entity.Property(e => e.ActualValue)
                    .HasColumnType("TEXT")
                    .HasConversion(
                        v => v != null ? System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null) : null,
                        v => v != null ? System.Text.Json.JsonSerializer.Deserialize<object>(v, (System.Text.Json.JsonSerializerOptions?)null) : null);
            });

            // Certificate Aggregate Data
            modelBuilder.Entity<CertificateAggregateData>(entity =>
            {
                entity.HasKey(e => new { e.Period, e.PeriodDate });
                entity.HasIndex(e => e.PeriodDate);

                var dictionaryIntComparer = new ValueComparer<Dictionary<string, int>>(
                    (c1, c2) => c1!.Count == c2!.Count && c1.Keys.All(k => c2.ContainsKey(k) && c1[k] == c2[k]),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
                    c => new Dictionary<string, int>(c));

                entity.Property(e => e.CertificatesByType)
                    .HasColumnType("TEXT")
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, int>())
                    .Metadata.SetValueComparer(dictionaryIntComparer);

                entity.Property(e => e.CertificatesByCountry)
                    .HasColumnType("TEXT")
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, int>())
                    .Metadata.SetValueComparer(dictionaryIntComparer);

                entity.Property(e => e.CertificatesByCompany)
                    .HasColumnType("TEXT")
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, int>())
                    .Metadata.SetValueComparer(dictionaryIntComparer);
            });
        }
    }
}