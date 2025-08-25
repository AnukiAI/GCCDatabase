using GCCWebApp.Data;
using GCCWebApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace GCCWebApp.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;

        public AdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardViewModel> GetDashboardDataAsync()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalCompanies = await _context.Companies.CountAsync();
            var totalCertificates = await _context.Certificates.CountAsync();
            var totalProducts = await _context.Products.CountAsync();
            
            var activeUsers = await _context.Users.Where(u => u.IsActive).CountAsync();
            var pendingCertificates = await _context.Certificates.Where(c => c.Status == "Pending").CountAsync();
            
            var recentCertificates = await _context.Certificates
                .OrderByDescending(c => c.CreatedAt)
                .Take(4)
                .Include(c => c.Product)
                .Include(c => c.Company)
                .ToListAsync();

            var recentActivity = recentCertificates.Select(c => 
                $"Certificate updated for Product {c.Product?.Name ?? "Unknown"}"
            ).ToList();

            return new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalCompanies = totalCompanies,
                TotalCertificates = totalCertificates,
                TotalProducts = totalProducts,
                ActiveUsers = activeUsers,
                PendingCertificates = pendingCertificates,
                LastDataUpdate = DateTime.Now,
                RecentActivity = recentActivity
            };
        }

        public async Task<List<UserViewModel>> GetUsersAsync()
        {
            return await _context.Users
                .Include(u => u.Companies)
                .Include(u => u.Roles)
                .Where(u => !u.IsDeleted)
                .Select(u => new UserViewModel
                {
                    Id = u.Id,
                    Name = u.FullName ?? u.Username,
                    Email = u.Email,
                    Role = u.Roles.FirstOrDefault()!.Name ?? "user",
                    Status = u.IsActive ? "active" : "inactive",
                    CompanyNames = u.Companies.Select(c => c.Name).ToList(),
                    LastLogin = u.LastLoginAt
                })
                .ToListAsync();
        }

        public async Task<UserViewModel?> GetUserByIdAsync(int id)
        {
            var user = await _context.Users
                .Include(u => u.Companies)
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (user == null) return null;

            return new UserViewModel
            {
                Id = user.Id,
                Name = user.FullName ?? user.Username,
                Email = user.Email,
                Role = user.Roles.FirstOrDefault()?.Name ?? "user",
                Status = user.IsActive ? "active" : "inactive",
                CompanyIds = user.Companies.Select(c => c.Id).ToList(),
                CompanyNames = user.Companies.Select(c => c.Name).ToList(),
                LastLogin = user.LastLoginAt
            };
        }

        public async Task<bool> CreateUserAsync(CreateUserViewModel model)
        {
            try
            {
                var user = new User
                {
                    Username = model.Email,
                    Email = model.Email,
                    FullName = model.Name,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Add role
                if (!string.IsNullOrEmpty(model.Role))
                {
                    var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == model.Role);
                    if (role != null)
                    {
                        user.Roles.Add(role);
                    }
                }

                // Add companies
                if (model.CompanyIds.Any())
                {
                    var companies = await _context.Companies
                        .Where(c => model.CompanyIds.Contains(c.Id))
                        .ToListAsync();
                    
                    foreach (var company in companies)
                    {
                        user.Companies.Add(company);
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(int id, UserViewModel model)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Companies)
                    .Include(u => u.Roles)
                    .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

                if (user == null) return false;

                user.FullName = model.Name;
                user.Email = model.Email;
                user.IsActive = model.Status == "active";
                user.UpdatedAt = DateTime.UtcNow;

                // Update role
                user.Roles.Clear();
                if (!string.IsNullOrEmpty(model.Role))
                {
                    var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == model.Role);
                    if (role != null)
                    {
                        user.Roles.Add(role);
                    }
                }

                // Update companies
                user.Companies.Clear();
                if (model.CompanyIds.Any())
                {
                    var companies = await _context.Companies
                        .Where(c => model.CompanyIds.Contains(c.Id))
                        .ToListAsync();
                    
                    foreach (var company in companies)
                    {
                        user.Companies.Add(company);
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
                if (user == null) return false;

                user.IsDeleted = true;
                user.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<RoleViewModel>> GetRolesAsync()
        {
            return await _context.Roles
                .Include(r => r.Users)
                .Include(r => r.Permissions)
                .Select(r => new RoleViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Permissions = r.Permissions.Select(p => p.Codename).ToList(),
                    UserCount = r.Users.Count()
                })
                .ToListAsync();
        }

        public async Task<RoleViewModel?> GetRoleByIdAsync(int id)
        {
            var role = await _context.Roles
                .Include(r => r.Permissions)
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null) return null;

            return new RoleViewModel
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                Permissions = role.Permissions.Select(p => p.Codename).ToList(),
                UserCount = role.Users.Count()
            };
        }

        public async Task<bool> CreateRoleAsync(RoleViewModel model)
        {
            try
            {
                var role = new Role
                {
                    Name = model.Name,
                    Description = model.Description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Roles.Add(role);
                await _context.SaveChangesAsync();

                // Add permissions
                if (model.Permissions.Any())
                {
                    var permissions = await _context.Permissions
                        .Where(p => model.Permissions.Contains(p.Codename))
                        .ToListAsync();
                    
                    foreach (var permission in permissions)
                    {
                        role.Permissions.Add(permission);
                    }
                    
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateRoleAsync(int id, RoleViewModel model)
        {
            try
            {
                var role = await _context.Roles
                    .Include(r => r.Permissions)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (role == null) return false;

                role.Name = model.Name;
                role.Description = model.Description;
                role.UpdatedAt = DateTime.UtcNow;

                // Update permissions
                role.Permissions.Clear();
                if (model.Permissions.Any())
                {
                    var permissions = await _context.Permissions
                        .Where(p => model.Permissions.Contains(p.Codename))
                        .ToListAsync();
                    
                    foreach (var permission in permissions)
                    {
                        role.Permissions.Add(permission);
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteRoleAsync(int id)
        {
            try
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id);
                if (role == null) return false;

                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<PermissionViewModel>> GetPermissionsAsync()
        {
            return await _context.Permissions
                .Where(p => p.IsActive)
                .Select(p => new PermissionViewModel
                {
                    Name = p.Codename,
                    DisplayName = p.Name,
                    Category = p.Category,
                    Description = p.Description
                })
                .ToListAsync();
        }

        public async Task<List<ProductTypeViewModel>> GetProductTypesAsync()
        {
            // Get all product types with their categories and children
            var allProductTypes = await _context.ProductTypes
                .Include(pt => pt.Category)
                .Include(pt => pt.Parent)
                .Include(pt => pt.Children)
                .Include(pt => pt.Products)
                .OrderBy(pt => pt.Level)
                .ThenBy(pt => pt.SortOrder)
                .ToListAsync();

            // Build hierarchical structure
            var result = new List<ProductTypeViewModel>();
            var lookup = new Dictionary<int, ProductTypeViewModel>();

            // First pass: create all items
            foreach (var pt in allProductTypes)
            {
                var viewModel = new ProductTypeViewModel
                {
                    Id = pt.Id,
                    Name = pt.Name,
                    ParentId = pt.ParentId,
                    Level = pt.Level,
                    SortOrder = pt.SortOrder,
                    ParentName = pt.Parent?.Name ?? "",
                    Children = new List<ProductTypeViewModel>()
                };
                lookup[pt.Id] = viewModel;
            }

            // Second pass: build hierarchy
            foreach (var pt in allProductTypes)
            {
                var viewModel = lookup[pt.Id];
                if (pt.ParentId.HasValue && lookup.ContainsKey(pt.ParentId.Value))
                {
                    lookup[pt.ParentId.Value].Children.Add(viewModel);
                }
                else
                {
                    // This is a root level item (or category level)
                    result.Add(viewModel);
                }
            }

            return result;
        }

        public async Task<ProductTypeViewModel?> GetProductTypeByIdAsync(int id)
        {
            var productType = await _context.ProductTypes
                .Include(pt => pt.Parent)
                .FirstOrDefaultAsync(pt => pt.Id == id);

            if (productType == null) return null;

            return new ProductTypeViewModel
            {
                Id = productType.Id,
                Name = productType.Name,
                ParentId = productType.ParentId,
                Level = productType.Level,
                SortOrder = productType.SortOrder,
                ParentName = productType.Parent?.Name ?? ""
            };
        }

        public async Task<bool> CreateProductTypeAsync(ProductTypeViewModel model)
        {
            try
            {
                var productType = new ProductType
                {
                    Name = model.Name,
                    ParentId = model.ParentId,
                    Level = model.Level,
                    SortOrder = model.SortOrder,
                    IsActive = true
                };

                _context.ProductTypes.Add(productType);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateProductTypeAsync(int id, ProductTypeViewModel model)
        {
            try
            {
                var productType = await _context.ProductTypes.FirstOrDefaultAsync(pt => pt.Id == id);
                if (productType == null) return false;

                productType.Name = model.Name;
                productType.ParentId = model.ParentId;
                productType.Level = model.Level;
                productType.SortOrder = model.SortOrder;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteProductTypeAsync(int id)
        {
            try
            {
                var productType = await _context.ProductTypes.FirstOrDefaultAsync(pt => pt.Id == id);
                if (productType == null) return false;

                _context.ProductTypes.Remove(productType);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<CertificateTypeViewModel>> GetCertificateTypesAsync()
        {
            return await _context.CertificationTypes
                .Include(ct => ct.Certificates)
                .Select(ct => new CertificateTypeViewModel
                {
                    Id = ct.Id,
                    Name = ct.Name,
                    Description = ct.Description,
                    IsActive = ct.IsActive,
                    CertificateCount = ct.Certificates.Count()
                })
                .ToListAsync();
        }

        public async Task<CertificateTypeViewModel?> GetCertificateTypeByIdAsync(int id)
        {
            var certType = await _context.CertificationTypes
                .Include(ct => ct.Certificates)
                .FirstOrDefaultAsync(ct => ct.Id == id);

            if (certType == null) return null;

            return new CertificateTypeViewModel
            {
                Id = certType.Id,
                Name = certType.Name,
                Description = certType.Description,
                IsActive = certType.IsActive,
                CertificateCount = certType.Certificates.Count()
            };
        }

        public async Task<bool> CreateCertificateTypeAsync(CertificateTypeViewModel model)
        {
            try
            {
                var certType = new CertificationType
                {
                    Name = model.Name,
                    Description = model.Description,
                    IsActive = model.IsActive
                };

                _context.CertificationTypes.Add(certType);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateCertificateTypeAsync(int id, CertificateTypeViewModel model)
        {
            try
            {
                var certType = await _context.CertificationTypes.FirstOrDefaultAsync(ct => ct.Id == id);
                if (certType == null) return false;

                certType.Name = model.Name;
                certType.Description = model.Description;
                certType.IsActive = model.IsActive;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteCertificateTypeAsync(int id)
        {
            try
            {
                var certType = await _context.CertificationTypes.FirstOrDefaultAsync(ct => ct.Id == id);
                if (certType == null) return false;

                _context.CertificationTypes.Remove(certType);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<CertificateFieldViewModel>> GetCertificateFieldsAsync()
        {
            return await _context.CustomFields
                .OrderBy(cf => cf.SortOrder)
                .Select(cf => new CertificateFieldViewModel
                {
                    Id = cf.Id,
                    Name = cf.Name,
                    FieldType = cf.FieldType,
                    IsRequired = cf.IsRequired,
                    IsVisible = cf.IsVisible,
                    SortOrder = cf.SortOrder,
                    Options = cf.Options ?? "",
                    DefaultValue = cf.DefaultValue ?? "",
                    ValidationRules = cf.ValidationRules ?? ""
                })
                .ToListAsync();
        }

        public async Task<CertificateFieldViewModel?> GetCertificateFieldByIdAsync(int id)
        {
            var field = await _context.CustomFields.FirstOrDefaultAsync(cf => cf.Id == id);
            if (field == null) return null;

            return new CertificateFieldViewModel
            {
                Id = field.Id,
                Name = field.Name,
                FieldType = field.FieldType,
                IsRequired = field.IsRequired,
                IsVisible = field.IsVisible,
                SortOrder = field.SortOrder,
                Options = field.Options ?? "",
                DefaultValue = field.DefaultValue ?? "",
                ValidationRules = field.ValidationRules ?? ""
            };
        }

        public async Task<bool> CreateCertificateFieldAsync(CertificateFieldViewModel model)
        {
            try
            {
                var field = new CustomField
                {
                    Name = model.Name,
                    FieldType = model.FieldType,
                    IsRequired = model.IsRequired,
                    IsVisible = model.IsVisible,
                    SortOrder = model.SortOrder,
                    Options = model.Options,
                    DefaultValue = model.DefaultValue,
                    ValidationRules = model.ValidationRules
                };

                _context.CustomFields.Add(field);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateCertificateFieldAsync(int id, CertificateFieldViewModel model)
        {
            try
            {
                var field = await _context.CustomFields.FirstOrDefaultAsync(cf => cf.Id == id);
                if (field == null) return false;

                field.Name = model.Name;
                field.FieldType = model.FieldType;
                field.IsRequired = model.IsRequired;
                field.IsVisible = model.IsVisible;
                field.SortOrder = model.SortOrder;
                field.Options = model.Options;
                field.DefaultValue = model.DefaultValue;
                field.ValidationRules = model.ValidationRules;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteCertificateFieldAsync(int id)
        {
            try
            {
                var field = await _context.CustomFields.FirstOrDefaultAsync(cf => cf.Id == id);
                if (field == null) return false;

                _context.CustomFields.Remove(field);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<TagViewModel>> GetTagsAsync()
        {
            return await _context.Tags
                .Include(t => t.Products)
                .Select(t => new TagViewModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    TagType = t.TagType,
                    ProductCount = t.Products.Count(),
                    Description = t.Description ?? ""
                })
                .ToListAsync();
        }

        public async Task<TagViewModel?> GetTagByIdAsync(int id)
        {
            var tag = await _context.Tags
                .Include(t => t.Products)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tag == null) return null;

            return new TagViewModel
            {
                Id = tag.Id,
                Name = tag.Name,
                TagType = tag.TagType,
                ProductCount = tag.Products.Count(),
                Description = tag.Description ?? ""
            };
        }

        public async Task<bool> CreateTagAsync(TagViewModel model)
        {
            try
            {
                var tag = new Tag
                {
                    Name = model.Name,
                    Description = model.Description,
                    TagType = model.TagType,
                    IsActive = true
                };

                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateTagAsync(int id, TagViewModel model)
        {
            try
            {
                var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Id == id);
                if (tag == null) return false;

                tag.Name = model.Name;
                tag.Description = model.Description;
                tag.TagType = model.TagType;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteTagAsync(int id)
        {
            try
            {
                var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Id == id);
                if (tag == null) return false;

                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // For partners, products, and other entities, I'll implement stubs since the models aren't fully defined
        public Task<List<PartnerViewModel>> GetPartnersAsync() => Task.FromResult(new List<PartnerViewModel>());
        public Task<PartnerViewModel?> GetPartnerByIdAsync(int id) => Task.FromResult<PartnerViewModel?>(null);
        public Task<bool> CreatePartnerAsync(PartnerViewModel model) => Task.FromResult(false);
        public Task<bool> UpdatePartnerAsync(int id, PartnerViewModel model) => Task.FromResult(false);
        public Task<bool> DeletePartnerAsync(int id) => Task.FromResult(false);

        public async Task<List<ProductViewModel>> GetProductsAsync()
        {
            return await _context.Products
                .Include(p => p.ProductType)
                .Include(p => p.Company)
                .Include(p => p.Certificates)
                .Select(p => new ProductViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description ?? "",
                    ProductTypeName = p.ProductType != null ? p.ProductType.Name : "",
                    CompanyName = p.Company != null ? p.Company.Name : "",
                    IsActive = p.IsActive,
                    CertificateCount = p.Certificates.Count(),
                    CreatedDate = p.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<ProductViewModel?> GetProductByIdAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductType)
                .Include(p => p.Company)
                .Include(p => p.Certificates)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null) return null;

            return new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description ?? "",
                ProductTypeName = product.ProductType?.Name ?? "",
                CompanyName = product.Company?.Name ?? "",
                IsActive = product.IsActive,
                CertificateCount = product.Certificates.Count(),
                CreatedDate = product.CreatedAt
            };
        }

        public async Task<bool> CreateProductAsync(ProductViewModel model)
        {
            try
            {
                var product = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                // Set ProductType if provided
                if (!string.IsNullOrEmpty(model.ProductTypeName))
                {
                    var productType = await _context.ProductTypes
                        .FirstOrDefaultAsync(pt => pt.Name == model.ProductTypeName);
                    if (productType != null)
                    {
                        product.ProductTypeId = productType.Id;
                    }
                }

                // Set Company if provided
                if (!string.IsNullOrEmpty(model.CompanyName))
                {
                    var company = await _context.Companies
                        .FirstOrDefaultAsync(c => c.Name == model.CompanyName);
                    if (company != null)
                    {
                        product.CompanyId = company.Id;
                    }
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                // Log error here in production
                return false;
            }
        }

        public async Task<bool> UpdateProductAsync(int id, ProductViewModel model)
        {
            try
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

                if (product == null) return false;

                product.Name = model.Name;
                product.Description = model.Description;
                product.IsActive = model.IsActive;
                product.UpdatedAt = DateTime.UtcNow;

                // Update ProductType if provided
                if (!string.IsNullOrEmpty(model.ProductTypeName))
                {
                    var productType = await _context.ProductTypes
                        .FirstOrDefaultAsync(pt => pt.Name == model.ProductTypeName);
                    product.ProductTypeId = productType?.Id;
                }

                // Update Company if provided
                if (!string.IsNullOrEmpty(model.CompanyName))
                {
                    var company = await _context.Companies
                        .FirstOrDefaultAsync(c => c.Name == model.CompanyName);
                    product.CompanyId = company?.Id;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                // Log error here in production
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

                if (product == null) return false;

                // Soft delete
                product.IsDeleted = true;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                // Log error here in production
                return false;
            }
        }

        public async Task<List<LookupItem>> GetCompaniesAsync()
        {
            return await _context.Companies
                .Where(c => c.IsActive)
                .Select(c => new LookupItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    IsActive = c.IsActive
                })
                .ToListAsync();
        }

        public async Task<List<LookupItem>> GetRolesListAsync()
        {
            return await _context.Roles
                .Where(r => r.IsActive)
                .Select(r => new LookupItem
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    IsActive = r.IsActive
                })
                .ToListAsync();
        }

        public async Task<byte[]> ExportDataAsync(string entityType, string format)
        {
            // Basic CSV export implementation
            var content = new StringBuilder();
            
            switch (entityType.ToLower())
            {
                case "users":
                    var users = await GetUsersAsync();
                    content.AppendLine("Id,Name,Email,Role,Status,Companies,LastLogin");
                    foreach (var user in users)
                    {
                        content.AppendLine($"{user.Id},{user.Name},{user.Email},{user.Role},{user.Status}," +
                                         $"\"{string.Join(", ", user.CompanyNames)}\",{user.LastLogin?.ToString("yyyy-MM-dd HH:mm")}");
                    }
                    break;
                    
                case "roles":
                    var roles = await GetRolesAsync();
                    content.AppendLine("Id,Name,Description,UserCount,Permissions");
                    foreach (var role in roles)
                    {
                        content.AppendLine($"{role.Id},{role.Name},{role.Description},{role.UserCount}," +
                                         $"\"{string.Join(", ", role.Permissions)}\"");
                    }
                    break;
                    
                default:
                    content.AppendLine("No data available for export");
                    break;
            }
            
            return Encoding.UTF8.GetBytes(content.ToString());
        }
    }
}