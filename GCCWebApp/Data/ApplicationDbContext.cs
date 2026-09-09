using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using GCCWebApp.Models;
using GCCWebApp.Models.Import;
using GCCWebApp.Search;

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

        // DEF-385: browsers a user has chosen to trust, so 2FA is not re-challenged for 30 days.
        public DbSet<TrustedDevice> TrustedDevices { get; set; }

        // Certificate related entities
        public DbSet<CertificationType> CertificationTypes { get; set; }
        public DbSet<CustomField> CustomFields { get; set; }
        public DbSet<CustomFieldValue> CustomFieldValues { get; set; }
        public DbSet<CertificateCountry> CertificateCountries { get; set; }
        public DbSet<CertificateDocument> CertificateDocuments { get; set; }
        public DbSet<CertificateProductId> CertificateProductIds { get; set; }
        public DbSet<CertificateStatusHistory> CertificateStatusHistory { get; set; }
        public DbSet<AuditHistory> AuditHistory { get; set; }
        public DbSet<ImportBatch> ImportBatches { get; set; }

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
        public DbSet<Pathway> Pathways { get; set; }

        // API management
        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<PartnerApiKey> PartnerApiKeys { get; set; }

        // Settings
        public DbSet<GoogleAnalyticsSettings> GoogleAnalyticsSettings { get; set; }

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

                // CR-038: FILTERED unique index. The client issued 137 certificate numbers to more
                // than one certificate and those numbers cannot be changed, so migrated records
                // flagged AllowsDuplicateNumber are exempt — but every ordinary certificate is still
                // guaranteed unique BY THE DATABASE, not merely by a screen check.
                //
                // Two things here are load-bearing:
                //   * The NAME must stay IX_Certificates_Number. CertificateRepository
                //     .IsUniqueNumberViolation matches the SQL error on that literal string to drive
                //     DEF-193's retry (two analysts creating a certificate at the same moment).
                //     Rename it and that retry silently stops working.
                //   * The filter cannot see exempt rows, so it does not stop a NEW certificate
                //     taking a grandfathered number. ValidationService covers that case.
                entity.HasIndex(e => e.Number)
                      .IsUnique()
                      .HasFilter("[AllowsDuplicateNumber] = 0")
                      .HasDatabaseName("IX_Certificates_Number");
                
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

                // DEF-181: persisted computed column for numeric-aware sort on CertificationResults.
                // See Certificate.CertificationResultsNumeric for the why. NULL when the raw value
                // isn't parseable — non-numeric values fall out of the numeric sort dimension.
                //
                // CR-045 (client, 2026-09-04): the x100 branch is GONE. CertificationResults is a
                // true free-text field now — whatever the analyst types is what publishes — so the
                // sort key must read the same value the user sees, or display and sort diverge and
                // we recreate DEF-383 from the other side (a future `0.5` would show as "0.5" and
                // sort as 50).
                //
                // The `%`-strip STAYS: "23%" is a formatted 23 and must sort as 23. That is
                // reading the value, not reinterpreting it. Every populated row on TEST and DEV is
                // an explicit XX% string after CR-042, so removing the x100 branch changes no
                // existing sort position — verified before the change, which is what made it safe.
                //
                // MUST move in the same deploy as the call-site change. Rewriting a PERSISTED
                // computed column needs a migration that drops the index, drops the column, and
                // re-adds both — EF's AlterColumn generates exactly that.
                entity.Property(e => e.CertificationResultsNumeric)
                    .HasComputedColumnSql(@"
                        CASE
                          WHEN [CertificationResults] IS NULL OR [CertificationResults] = N'' THEN NULL
                          WHEN [CertificationResults] LIKE N'%[%]%' THEN TRY_CAST(REPLACE([CertificationResults], N'%', N'') AS DECIMAL(18,4))
                          ELSE TRY_CAST([CertificationResults] AS DECIMAL(18,4))
                        END", stored: true);
                entity.HasIndex(e => e.CertificationResultsNumeric);

                // CR-187: persisted computed search-normalized columns. SQL is generated from the
                // same SearchNormalizer.RemovableChars used to normalize the search input, so the
                // stored value and the query term can never drift. Indexed for prefix/autocomplete.
                entity.Property(e => e.NormalizedNumber)
                    .HasComputedColumnSql(SearchNormalizer.ToSqlExpression("[Number]"), stored: true);
                entity.HasIndex(e => e.NormalizedNumber);
                entity.Property(e => e.NormalizedLocation)
                    .HasComputedColumnSql(SearchNormalizer.ToSqlExpression("[Location]"), stored: true);
                entity.HasIndex(e => e.NormalizedLocation);
                entity.Property(e => e.NormalizedCountry)
                    .HasComputedColumnSql(SearchNormalizer.ToSqlExpression("[Country]"), stored: true);
                entity.HasIndex(e => e.NormalizedCountry);

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

                // Certification type subcategory (optional)
                entity.HasIndex(e => e.CertificationTypeSubcategoryId);
                entity.HasOne(e => e.CertificationTypeSubcategory)
                    .WithMany()
                    .HasForeignKey(e => e.CertificationTypeSubcategoryId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

                // Product Category (optional)
                entity.HasIndex(e => e.ProductCategoryId);
                entity.HasOne(e => e.ProductCategory)
                    .WithMany()
                    .HasForeignKey(e => e.ProductCategoryId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

                // Product Type (optional) - Level 1 ProductType
                entity.HasIndex(e => e.ProductTypeId);
                entity.HasOne(e => e.ProductType)
                    .WithMany()
                    .HasForeignKey(e => e.ProductTypeId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

                // Product Subcategory (optional) - Level 2 child ProductType
                entity.HasIndex(e => e.ProductSubcategoryId);
                entity.HasOne(e => e.ProductSubcategory)
                    .WithMany()
                    .HasForeignKey(e => e.ProductSubcategoryId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

                // Pathway (optional)
                entity.HasIndex(e => e.PathwayId);
                entity.HasOne(e => e.Pathway)
                    .WithMany(p => p.Certificates)
                    .HasForeignKey(e => e.PathwayId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

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

                // CR-187: persisted computed search-normalized Name (see SearchNormalizer).
                entity.Property(e => e.NormalizedName)
                    .HasComputedColumnSql(SearchNormalizer.ToSqlExpression("[Name]"), stored: true);
                entity.HasIndex(e => e.NormalizedName);

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

                // CR-187: persisted computed search-normalized Name (see SearchNormalizer).
                entity.Property(e => e.NormalizedName)
                    .HasComputedColumnSql(SearchNormalizer.ToSqlExpression("[Name]"), stored: true);
                entity.HasIndex(e => e.NormalizedName);

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

                // ProductSubcategoryId points at a Level-2 child row in ProductTypes
                // (matches Certificate.ProductSubcategory). The legacy ProductSubcategories
                // table is no longer referenced by Product.
                entity.HasOne(e => e.ProductSubcategory)
                    .WithMany()
                    .HasForeignKey(e => e.ProductSubcategoryId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

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

                // CR-187: persisted computed search-normalized Name (see SearchNormalizer).
                entity.Property(e => e.NormalizedName)
                    .HasComputedColumnSql(SearchNormalizer.ToSqlExpression("[Name]"), stored: true);
                entity.HasIndex(e => e.NormalizedName);
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

                // Unique constraint for child items: same name allowed under different parents
                entity.HasIndex(e => new { e.Name, e.ParentId })
                    .IsUnique()
                    .HasFilter("[ParentId] IS NOT NULL");

                // Unique constraint for top-level items: no duplicate names at root level
                entity.HasIndex(e => e.Name)
                    .IsUnique()
                    .HasDatabaseName("IX_ProductTypes_Name_TopLevel")
                    .HasFilter("[ParentId] IS NULL");
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

                // CR-187: persisted computed search-normalized Name (see SearchNormalizer).
                entity.Property(e => e.NormalizedName)
                    .HasComputedColumnSql(SearchNormalizer.ToSqlExpression("[Name]"), stored: true);
                entity.HasIndex(e => e.NormalizedName);

                // Unique constraint for subcategories: same name allowed under different parents
                entity.HasIndex(e => new { e.Name, e.ParentId })
                    .IsUnique()
                    .HasFilter("[ParentId] IS NOT NULL");

                // Unique constraint for top-level types: no duplicate names at root level
                entity.HasIndex(e => e.Name)
                    .IsUnique()
                    .HasDatabaseName("IX_CertificationTypes_Name_TopLevel")
                    .HasFilter("[ParentId] IS NULL");
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
                entity.HasIndex(e => new { e.CertificateId, e.ProductIdType, e.ProductIdValue }).IsUnique();
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
                // Filtered unique indexes (DEF-195): soft-deleted rows are excluded so
                // an email/username freed by DeleteUserAsync becomes immediately reusable
                // without depending on the __deleted__ suffix trick. Same pattern as
                // Pathways.Name (FixPathwayUniqueIndexForSoftDelete, 2026-02-13).
                entity.HasIndex(e => e.Username).IsUnique().HasFilter("[IsDeleted] = 0");
                entity.HasIndex(e => e.Email).IsUnique().HasFilter("[IsDeleted] = 0");
                
                // Performance indexes for user queries  
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.LastLoginAt);
                
                // Composite indexes
                entity.HasIndex(e => new { e.Status, e.IsActive });
                entity.HasIndex(e => new { e.IsActive, e.CreatedAt });
            });

            // DEF-385: trusted devices (the "Trust this device for 30 days" 2FA option).
            modelBuilder.Entity<TrustedDevice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TokenHash).HasMaxLength(64).IsRequired();

                // The lookup is always (TokenHash, UserId) together — a token must belong to the
                // account being signed in, so a device trusted for one user cannot skip 2FA for
                // another on the same browser. Indexed as a pair to match that query exactly.
                entity.HasIndex(e => new { e.TokenHash, e.UserId });
                entity.HasIndex(e => e.UserId);

                // Cascade: a deleted user's trusts must not outlive the account.
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
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

                // CR-187: persisted computed search-normalized Name (see SearchNormalizer).
                entity.Property(e => e.NormalizedName)
                    .HasComputedColumnSql(SearchNormalizer.ToSqlExpression("[Name]"), stored: true);
                entity.HasIndex(e => e.NormalizedName);
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

            // Pathway configuration
            modelBuilder.Entity<Pathway>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.HasIndex(e => e.Name).IsUnique().HasFilter("[IsDeleted] = 0");

                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.SortOrder);

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
                .WithMany(r => r.Users)
                .UsingEntity(j => j.ToTable("UserRoles"));

            modelBuilder.Entity<Role>()
                .HasMany(r => r.Permissions)
                .WithMany(p => p.Roles);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Companies)
                .WithMany(c => c.Users);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Partners)
                .WithMany(p => p.Users);

            // CR-020 (revised QC #106): User.UserCompany is now a free-text string (the user's
            // own employer), no longer an FK to Companies. No relationship config needed — the
            // [MaxLength(255)] attribute on the model maps it to nvarchar(255) NULL.

            // Certificate-Partner many-to-many (multi-partner support)
            modelBuilder.Entity<Certificate>()
                .HasMany(c => c.Partners)
                .WithMany(p => p.Certificates)
                .UsingEntity(j => j.ToTable("CertificatePartners"));

            // CR-037: Certificate-Country. A one-to-many onto a string join table rather than a
            // many-to-many, because Country is not an entity (see CertificateCountry for why).
            // Cascade delete so the join rows go with the certificate — and note the delete audit
            // must capture these rows BEFORE SaveChanges, same as the other children (DEF-337).
            modelBuilder.Entity<CertificateCountry>(entity =>
            {
                entity.ToTable("CertificateCountries");

                entity.HasOne(cc => cc.Certificate)
                    .WithMany(c => c.Countries)
                    .HasForeignKey(cc => cc.CertificateId)
                    .OnDelete(DeleteBehavior.Cascade);

                // CR-187: same normalizer expression as Certificate.NormalizedCountry — one source
                // of truth for normalization, never a second hand-rolled helper.
                entity.Property(cc => cc.NormalizedCountry)
                    .HasComputedColumnSql(SearchNormalizer.ToSqlExpression("[Country]"), stored: true);

                // Drives the "does this country exist on the cert" filter across the tile APIs.
                entity.HasIndex(cc => cc.Country);
                entity.HasIndex(cc => cc.NormalizedCountry);

                // A certificate lists a given country once. Guards the sync path against
                // double-inserting when an edit resubmits an unchanged set.
                entity.HasIndex(cc => new { cc.CertificateId, cc.Country }).IsUnique();
            });

            // CR-171 Phase 2: CertificateProductId-Partner many-to-many (per-product partner sharing).
            // No inverse navigation on Partner — product-ID membership is read from the product-ID side.
            modelBuilder.Entity<CertificateProductId>()
                .HasMany(pid => pid.Partners)
                .WithMany()
                .UsingEntity(j => j.ToTable("CertificateProductIdPartners"));

            // Certificate-CertificationTypeSubcategories many-to-many (multi-select subcategories)
            modelBuilder.Entity<Certificate>()
                .HasMany(c => c.CertificationTypeSubcategories)
                .WithMany()
                .UsingEntity(j => j.ToTable("CertificateCertificationTypeSubcategories"));

            // Company-Partner optional relationship (for reporting)
            modelBuilder.Entity<Company>()
                .HasOne(c => c.Partner)
                .WithMany(p => p.Companies)
                .HasForeignKey(c => c.PartnerId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

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

            // Google Analytics Settings configuration
            modelBuilder.Entity<GoogleAnalyticsSettings>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.TrackingId)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(e => e.CustomScript)
                    .HasColumnType("TEXT");

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedBy)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Certificate Status History configuration
            modelBuilder.Entity<CertificateStatusHistory>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Performance indexes for status history queries
                entity.HasIndex(e => e.CertificateId);
                entity.HasIndex(e => e.ChangedBy);
                entity.HasIndex(e => e.ChangedAt);
                entity.HasIndex(e => e.NewStatus);
                entity.HasIndex(e => e.PreviousStatus);

                // Composite indexes for complex queries
                entity.HasIndex(e => new { e.CertificateId, e.ChangedAt });
                entity.HasIndex(e => new { e.CertificateId, e.NewStatus });
                entity.HasIndex(e => new { e.ChangedAt, e.NewStatus });

                // Relationships
                entity.HasOne(e => e.Certificate)
                    .WithMany(c => c.StatusHistory)
                    .HasForeignKey(e => e.CertificateId)
                    .OnDelete(DeleteBehavior.Cascade); // Delete history when certificate is deleted

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.ChangedBy)
                    .OnDelete(DeleteBehavior.NoAction); // Don't delete history when user is deleted
            });

            // Audit History configuration (generic entity change tracking)
            modelBuilder.Entity<AuditHistory>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Performance indexes for audit queries
                entity.HasIndex(e => e.EntityType);
                entity.HasIndex(e => e.EntityId);
                entity.HasIndex(e => e.ChangedBy);
                entity.HasIndex(e => e.ChangedAt);
                entity.HasIndex(e => e.Action);

                // Composite indexes for common query patterns
                entity.HasIndex(e => new { e.EntityType, e.EntityId, e.ChangedAt });
                entity.HasIndex(e => new { e.EntityType, e.Action });
                entity.HasIndex(e => new { e.ChangedBy, e.ChangedAt });
                entity.HasIndex(e => new { e.ChangedAt, e.EntityType });

                // Column constraints
                entity.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EntityId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.EntityName).HasMaxLength(255);
                entity.Property(e => e.Action).IsRequired().HasMaxLength(20);
                entity.Property(e => e.FieldName).HasMaxLength(100);
                entity.Property(e => e.ChangedByUsername).IsRequired().HasMaxLength(255);
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.ChangeSource).HasMaxLength(50);
                entity.Property(e => e.Reason).HasMaxLength(1000);
                entity.Property(e => e.SessionId).HasMaxLength(100);

                // Relationship to User
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.ChangedBy)
                    .OnDelete(DeleteBehavior.NoAction); // Don't delete audit when user is deleted
            });
        }
    }
}