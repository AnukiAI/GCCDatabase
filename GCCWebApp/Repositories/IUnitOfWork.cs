using GCCWebApp.Models;

namespace GCCWebApp.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Certificate> Certificates { get; }
        IRepository<Company> Companies { get; }
        IRepository<Product> Products { get; }
        IRepository<User> Users { get; }
        IRepository<CertificationType> CertificationTypes { get; }
        IRepository<ProductCategory> ProductCategories { get; }
        IRepository<ProductType> ProductTypes { get; }
        IRepository<ProductSubcategory> ProductSubcategories { get; }
        IRepository<Tag> Tags { get; }
        IRepository<Role> Roles { get; }
        IRepository<Permission> Permissions { get; }
        IRepository<CustomField> CustomFields { get; }
        IRepository<CustomFieldValue> CustomFieldValues { get; }
        IRepository<Partner> Partners { get; }

        Task<int> CompleteAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}