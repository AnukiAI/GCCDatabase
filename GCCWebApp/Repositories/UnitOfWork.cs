using Microsoft.EntityFrameworkCore.Storage;
using GCCWebApp.Data;
using GCCWebApp.Models;

namespace GCCWebApp.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;

            // Initialize repositories
            Certificates = new Repository<Certificate>(_context);
            Companies = new Repository<Company>(_context);
            Products = new Repository<Product>(_context);
            Users = new Repository<User>(_context);
            CertificationTypes = new Repository<CertificationType>(_context);
            ProductCategories = new Repository<ProductCategory>(_context);
            ProductTypes = new Repository<ProductType>(_context);
            ProductSubcategories = new Repository<ProductSubcategory>(_context);
            Tags = new Repository<Tag>(_context);
            Roles = new Repository<Role>(_context);
            Permissions = new Repository<Permission>(_context);
            CustomFields = new Repository<CustomField>(_context);
            CustomFieldValues = new Repository<CustomFieldValue>(_context);
        }

        public IRepository<Certificate> Certificates { get; private set; }
        public IRepository<Company> Companies { get; private set; }
        public IRepository<Product> Products { get; private set; }
        public IRepository<User> Users { get; private set; }
        public IRepository<CertificationType> CertificationTypes { get; private set; }
        public IRepository<ProductCategory> ProductCategories { get; private set; }
        public IRepository<ProductType> ProductTypes { get; private set; }
        public IRepository<ProductSubcategory> ProductSubcategories { get; private set; }
        public IRepository<Tag> Tags { get; private set; }
        public IRepository<Role> Roles { get; private set; }
        public IRepository<Permission> Permissions { get; private set; }
        public IRepository<CustomField> CustomFields { get; private set; }
        public IRepository<CustomFieldValue> CustomFieldValues { get; private set; }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}