using Microsoft.EntityFrameworkCore;
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

        // Product related entities
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<ProductType> ProductTypes { get; set; }
        public DbSet<ProductSubcategory> ProductSubcategories { get; set; }

        // Tags and attributes
        public DbSet<Tag> Tags { get; set; }

        // User management
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Certificate configuration
            modelBuilder.Entity<Certificate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Number).IsUnique();
                
                // Relationships
                entity.HasOne(e => e.Company)
                    .WithMany(c => c.Certificates)
                    .HasForeignKey(e => e.CompanyId);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.Certificates)
                    .HasForeignKey(e => e.ProductId);

                entity.HasOne(e => e.CertificationType)
                    .WithMany(ct => ct.Certificates)
                    .HasForeignKey(e => e.CertificationTypeId);

                entity.HasOne(e => e.CreatedByUser)
                    .WithMany(u => u.CreatedCertificates)
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany(u => u.UpdatedCertificates)
                    .HasForeignKey(e => e.UpdatedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Company configuration
            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                
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

            // Product category configuration
            modelBuilder.Entity<ProductCategory>(entity =>
            {
                entity.HasKey(e => e.Id);
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

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Role configuration
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
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
                entity.HasIndex(e => e.Name).IsUnique();
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
        }
    }
}