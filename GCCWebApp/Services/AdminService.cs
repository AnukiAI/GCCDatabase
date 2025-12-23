using GCCWebApp.Data;
using GCCWebApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using ClosedXML.Excel;
using GCCWebApp.Services.Audit;

namespace GCCWebApp.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminService> _logger;
        private readonly IAuditService _auditService;

        public AdminService(ApplicationDbContext context, ILogger<AdminService> logger, IAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
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
                .Include(u => u.Partners)
                .Include(u => u.Roles)
                .Where(u => !u.IsDeleted)
                .Select(u => new UserViewModel
                {
                    Id = u.Id,
                    Name = u.FullName ?? u.Username,
                    Email = u.Email,
                    Role = u.Roles.FirstOrDefault() != null ? u.Roles.FirstOrDefault()!.Name : "Public",
                    Status = u.IsActive ? "active" : "inactive",
                    CompanyNames = u.Companies.Select(c => c.Name).ToList(),
                    PartnerNames = u.Partners.Select(p => p.Name).ToList(),
                    RequirePasswordReset = u.RequirePasswordReset,
                    LastLogin = u.LastLoginAt,
                    TwoFactorEnabled = u.TwoFactorEnabled,
                    TwoFactorMethod = u.TwoFactorMethod
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

        public async Task<bool> CreateUserAsync(CreateUserViewModel model, int userId, string username)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    Username = model.Email,
                    Email = model.Email,
                    FullName = model.Name,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Status = "active",
                    TwoFactorMethod = "Email",
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

                // Log audit history
                try
                {
                    await _auditService.LogEntityChangeAsync(
                        user.Id.ToString(),
                        user.Email,
                        "CREATE",
                        null,
                        user,
                        userId,
                        username,
                        $"Created user with role: {model.Role ?? "None"}",
                        "Web UI"
                    );
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to log audit history for user creation");
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to create user with email {Email}", model.Email);
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(int id, UserViewModel model, int userId, string username)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users
                    .Include(u => u.Companies)
                    .Include(u => u.Roles)
                    .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

                if (user == null) return false;

                // Capture old state for audit (detached from context)
                var oldUser = await _context.Users
                    .AsNoTracking()
                    .Include(u => u.Roles)
                    .Include(u => u.Companies)
                    .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

                var oldRole = oldUser?.Roles.FirstOrDefault()?.Name ?? "None";
                var newRole = model.Role ?? "None";

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

                // Log audit history
                try
                {
                    var roleChange = oldRole != newRole ? $" | Role changed from {oldRole} to {newRole}" : "";
                    await _auditService.LogEntityChangeAsync(
                        user.Id.ToString(),
                        user.Email,
                        "UPDATE",
                        oldUser,
                        user,
                        userId,
                        username,
                        $"Updated user{roleChange}",
                        "Web UI"
                    );
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to log audit history for user update");
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to update user with ID {UserId}", id);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int id, int userId, string username)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Roles)
                    .FirstOrDefaultAsync(u => u.Id == id);
                if (user == null) return false;

                // Prevent deletion of Admin users
                if (user.Roles.Any(r => r.Name == "Admin"))
                {
                    _logger.LogWarning("Attempted to delete Admin user: {UserEmail}", user.Email);
                    return false;
                }

                // Capture user state before deletion for audit
                var userEmail = user.Email;
                var oldUser = await _context.Users
                    .AsNoTracking()
                    .Include(u => u.Roles)
                    .FirstOrDefaultAsync(u => u.Id == id);

                user.IsDeleted = true;
                user.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Log audit history
                try
                {
                    await _auditService.LogEntityChangeAsync(
                        id.ToString(),
                        userEmail,
                        "DELETE",
                        oldUser,
                        null, // User is soft deleted
                        userId,
                        username,
                        "Soft delete - User marked as deleted",
                        "Web UI"
                    );
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to log audit history for user deletion");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete user with ID {UserId}", id);
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
                    Description = r.Description ?? string.Empty,
                    Permissions = r.Permissions.Select(p => p.Name).ToList(),
                    UserCount = r.Users.Count(),
                    IsSystem = r.IsSystem
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
                Description = role.Description ?? string.Empty,
                Permissions = role.Permissions.Select(p => p.Name).ToList(),
                UserCount = role.Users.Count(),
                IsSystem = role.IsSystem
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
                    // Try to parse as IDs first, fall back to codenames
                    var permissionIds = model.Permissions
                        .Where(p => int.TryParse(p, out _))
                        .Select(p => int.Parse(p))
                        .ToList();

                    var permissionCodenames = model.Permissions
                        .Where(p => !int.TryParse(p, out _))
                        .ToList();

                    var permissions = await _context.Permissions
                        .Where(p => permissionIds.Contains(p.Id) || permissionCodenames.Contains(p.Codename))
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
                    // Try to parse as IDs first, fall back to codenames
                    var permissionIds = model.Permissions
                        .Where(p => int.TryParse(p, out _))
                        .Select(p => int.Parse(p))
                        .ToList();

                    var permissionCodenames = model.Permissions
                        .Where(p => !int.TryParse(p, out _))
                        .ToList();

                    var permissions = await _context.Permissions
                        .Where(p => permissionIds.Contains(p.Id) || permissionCodenames.Contains(p.Codename))
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

                // Prevent deletion of system roles
                if (role.IsSystem)
                {
                    _logger.LogWarning("Attempted to delete system role: {RoleName}", role.Name);
                    return false;
                }

                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete role with ID {RoleId}", id);
                return false;
            }
        }

        public async Task<List<PermissionViewModel>> GetPermissionsAsync()
        {
            return await _context.Permissions
                .Where(p => p.IsActive)
                .Select(p => new PermissionViewModel
                {
                    Id = p.Id,
                    Name = p.Codename,
                    DisplayName = p.Name,
                    Category = p.Category ?? string.Empty,
                    Description = p.Description ?? string.Empty
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
            
            // Return empty list if no data exists (no mock data for production)
            if (!allProductTypes.Any())
            {
                return new List<ProductTypeViewModel>();
            }

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
                    ProductCount = pt.Products?.Count ?? 0,
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

        public async Task<ProductTypeViewModel?> CreateProductTypeAsync(ProductTypeViewModel model)
        {
            try
            {
                // Determine level based on parent
                int level = 0;
                if (model.ParentId.HasValue)
                {
                    var parent = await _context.ProductTypes.FirstOrDefaultAsync(pt => pt.Id == model.ParentId.Value);
                    if (parent != null)
                    {
                        level = parent.Level + 1;
                    }
                }

                var productType = new ProductType
                {
                    Name = model.Name,
                    ParentId = model.ParentId,
                    Level = level,
                    SortOrder = model.SortOrder,
                    IsActive = true
                };

                _context.ProductTypes.Add(productType);
                await _context.SaveChangesAsync();
                
                // Return the created product type as view model
                return new ProductTypeViewModel
                {
                    Id = productType.Id,
                    Name = productType.Name,
                    ParentId = productType.ParentId,
                    Level = productType.Level,
                    SortOrder = productType.SortOrder
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> UpdateProductTypeAsync(ProductTypeViewModel model)
        {
            try
            {
                var productType = await _context.ProductTypes.FirstOrDefaultAsync(pt => pt.Id == model.Id);
                if (productType == null) return false;

                // Update level based on new parent
                if (model.ParentId != productType.ParentId)
                {
                    if (model.ParentId.HasValue)
                    {
                        var parent = await _context.ProductTypes.FirstOrDefaultAsync(pt => pt.Id == model.ParentId.Value);
                        if (parent != null)
                        {
                            productType.Level = parent.Level + 1;
                        }
                    }
                    else
                    {
                        productType.Level = 0;
                    }
                }

                productType.Name = model.Name;
                productType.ParentId = model.ParentId;
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
                .Include(ct => ct.Parent)
                .Select(ct => new CertificateTypeViewModel
                {
                    Id = ct.Id,
                    Name = ct.Name,
                    Description = ct.Description ?? string.Empty,
                    IsActive = ct.IsActive,
                    CertificateCount = ct.Certificates.Count(),
                    ParentId = ct.ParentId,
                    Level = ct.Level,
                    ParentName = ct.Parent != null ? ct.Parent.Name : null
                })
                .OrderBy(ct => ct.ParentId == null ? ct.Name : "")
                .ThenBy(ct => ct.ParentName)
                .ThenBy(ct => ct.Name)
                .ToListAsync();
        }

        public async Task<CertificateTypeViewModel?> GetCertificateTypeByIdAsync(int id)
        {
            var certType = await _context.CertificationTypes
                .Include(ct => ct.Certificates)
                .Include(ct => ct.Parent)
                .FirstOrDefaultAsync(ct => ct.Id == id);

            if (certType == null) return null;

            return new CertificateTypeViewModel
            {
                Id = certType.Id,
                Name = certType.Name,
                Description = certType.Description ?? string.Empty,
                IsActive = certType.IsActive,
                CertificateCount = certType.Certificates.Count(),
                ParentId = certType.ParentId,
                Level = certType.Level,
                ParentName = certType.Parent?.Name
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
                    IsActive = model.IsActive,
                    ParentId = model.ParentId,
                    Level = model.ParentId.HasValue ? 2 : 1  // Set level based on whether it has a parent
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
                certType.ParentId = model.ParentId;
                certType.Level = model.ParentId.HasValue ? 2 : 1;  // Set level based on whether it has a parent

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
                    RoleVisibility = cf.RoleVisibility ?? "Public",
                    SortOrder = cf.SortOrder,
                    Options = cf.Options ?? "",
                    DefaultValue = cf.DefaultValue ?? "",
                    ValidationRules = cf.ValidationRules ?? "",
                    Section = cf.Section
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
                ValidationRules = field.ValidationRules ?? "",
                Section = field.Section
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
                    RoleVisibility = model.RoleVisibility ?? "Public",
                    SortOrder = model.SortOrder,
                    Options = model.Options,
                    DefaultValue = model.DefaultValue,
                    ValidationRules = model.ValidationRules,
                    Section = model.Section ?? "Core Certificate Information",
                    CertificateTypeId = 1 // Default to first certificate type for now
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
                field.RoleVisibility = model.RoleVisibility ?? "Public";
                field.SortOrder = model.SortOrder;
                field.Options = model.Options;
                field.DefaultValue = model.DefaultValue;
                field.ValidationRules = model.ValidationRules;
                field.Section = model.Section;

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

        // ============================================
        // CSF Attributes (stored in Tags table with TagType = 'csf')
        // ============================================

        public async Task<List<CSFAttributeViewModel>> GetCSFAttributesAsync()
        {
            return await _context.Tags
                .Where(t => t.TagType == "csf")
                .OrderBy(t => t.Name)
                .Select(t => new CSFAttributeViewModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description ?? "",
                    IsActive = t.IsActive
                })
                .ToListAsync();
        }

        public async Task<CSFAttributeViewModel?> GetCSFAttributeByIdAsync(int id)
        {
            var attribute = await _context.Tags
                .Where(t => t.TagType == "csf")
                .FirstOrDefaultAsync(t => t.Id == id);

            if (attribute == null) return null;

            return new CSFAttributeViewModel
            {
                Id = attribute.Id,
                Name = attribute.Name,
                Description = attribute.Description ?? "",
                IsActive = attribute.IsActive
            };
        }

        public async Task<bool> CreateCSFAttributeAsync(CSFAttributeViewModel model)
        {
            try
            {
                var attribute = new Tag
                {
                    Name = model.Name,
                    TagType = "csf",
                    Description = model.Description,
                    IsActive = model.IsActive
                };

                _context.Tags.Add(attribute);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateCSFAttributeAsync(int id, CSFAttributeViewModel model)
        {
            try
            {
                var attribute = await _context.Tags
                    .Where(t => t.TagType == "csf")
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (attribute == null) return false;

                attribute.Name = model.Name;
                attribute.Description = model.Description;
                attribute.IsActive = model.IsActive;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteCSFAttributeAsync(int id)
        {
            try
            {
                var attribute = await _context.Tags
                    .Where(t => t.TagType == "csf")
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (attribute == null) return false;

                _context.Tags.Remove(attribute);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ============================================
        // Tags
        // ============================================

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

        // Partners
        public async Task<List<PartnerViewModel>> GetPartnersAsync()
        {
            return await _context.Partners
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .Select(p => new PartnerViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description ?? "",
                    Website = p.Website ?? "",
                    IsActive = p.IsActive,
                    UserCount = p.Users.Count,
                    CreatedDate = p.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<PartnerViewModel?> GetPartnerByIdAsync(int id)
        {
            var partner = await _context.Partners
                .Where(p => p.Id == id && !p.IsDeleted)
                .FirstOrDefaultAsync();

            if (partner == null) return null;

            return new PartnerViewModel
            {
                Id = partner.Id,
                Name = partner.Name,
                Description = partner.Description ?? "",
                Website = partner.Website ?? "",
                IsActive = partner.IsActive,
                UserCount = partner.Users.Count,
                CreatedDate = partner.CreatedAt
            };
        }

        public async Task<bool> CreatePartnerAsync(PartnerViewModel model)
        {
            try
            {
                var partner = new Partner
                {
                    Name = model.Name,
                    Description = model.Description,
                    Website = model.Website,
                    IsActive = model.IsActive,
                    SortOrder = model.SortOrder,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Partners.Add(partner);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating partner {PartnerName}", model.Name);
                return false;
            }
        }

        public async Task<bool> UpdatePartnerAsync(int id, PartnerViewModel model)
        {
            try
            {
                var partner = await _context.Partners
                    .Where(p => p.Id == id && !p.IsDeleted)
                    .FirstOrDefaultAsync();

                if (partner == null) return false;

                partner.Name = model.Name;
                partner.Description = model.Description;
                partner.Website = model.Website;
                partner.IsActive = model.IsActive;
                partner.SortOrder = model.SortOrder;
                partner.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating partner {PartnerId}", id);
                return false;
            }
        }

        public async Task<bool> DeletePartnerAsync(int id)
        {
            try
            {
                var partner = await _context.Partners
                    .Where(p => p.Id == id && !p.IsDeleted)
                    .FirstOrDefaultAsync();

                if (partner == null) return false;

                // Soft delete
                partner.IsDeleted = true;
                partner.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting partner {PartnerId}", id);
                return false;
            }
        }

        public async Task<List<ProductViewModel>> GetProductsAsync()
        {
            return await _context.Products
                .Include(p => p.ProductType)
                .Include(p => p.Company)
                .Include(p => p.Certificates)
                .Include(p => p.ProductIdentifiers)
                .AsSplitQuery()
                .Select(p => new ProductViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description ?? string.Empty ?? "",
                    ProductTypeName = p.ProductType != null ? p.ProductType.Name : "",
                    CompanyName = p.Company != null ? p.Company.Name : "",
                    IsActive = p.IsActive,
                    CertificateCount = p.Certificates.Count(),
                    CreatedDate = p.CreatedAt,
                    ProductIdentifiers = p.ProductIdentifiers.Select(pi => new ProductIdentifierViewModel
                    {
                        Id = pi.Id,
                        IdentifierType = pi.IdentifierType,
                        IdentifierValue = pi.IdentifierValue
                    }).ToList()
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
                    Description = r.Description ?? string.Empty,
                    IsActive = r.IsActive
                })
                .ToListAsync();
        }

        public async Task<byte[]> ExportDataAsync(string entityType, string format)
        {
            format = format.ToLower();
            
            if (format == "xlsx")
            {
                return await GenerateExcelExport(entityType);
            }
            else
            {
                return await GenerateCsvExport(entityType);
            }
        }
        
        private async Task<byte[]> GenerateCsvExport(string entityType)
        {
            var content = new StringBuilder();
            
            switch (entityType.ToLower())
            {
                case "users":
                    var users = await GetUsersAsync();
                    content.AppendLine("Id,Name,Email,Role,Status,Companies,LastLogin");
                    foreach (var user in users)
                    {
                        content.AppendLine($"{user.Id},\"{user.Name}\",\"{user.Email}\",\"{user.Role}\",\"{user.Status}\"," +
                                         $"\"{string.Join(", ", user.CompanyNames)}\",\"{user.LastLogin?.ToString("yyyy-MM-dd HH:mm")}\"");
                    }
                    break;
                    
                case "roles":
                    var roles = await GetRolesAsync();
                    content.AppendLine("Id,Name,Description,UserCount,Permissions");
                    foreach (var role in roles)
                    {
                        content.AppendLine($"{role.Id},\"{role.Name}\",\"{role.Description}\",{role.UserCount}," +
                                         $"\"{string.Join(", ", role.Permissions)}\"");
                    }
                    break;
                    
                case "certificate-types":
                case "certificatetypes":
                case "certificate_types":
                    var certTypes = await GetCertificateTypesAsync();
                    content.AppendLine("Id,Name,Description,IsActive,CertificateCount");
                    foreach (var certType in certTypes)
                    {
                        content.AppendLine($"{certType.Id},\"{certType.Name}\",\"{certType.Description}\"," +
                                         $"{certType.IsActive},{certType.CertificateCount}");
                    }
                    break;
                    
                case "certificate-fields":
                case "certificatefields":
                case "certificate_fields":
                    var certFields = await GetCertificateFieldsAsync();
                    content.AppendLine("Id,Name,FieldType,IsRequired,IsVisible,SortOrder,Options,DefaultValue,ValidationRules");
                    foreach (var field in certFields)
                    {
                        content.AppendLine($"{field.Id},\"{field.Name}\",\"{field.FieldType}\"," +
                                         $"{field.IsRequired},{field.IsVisible},{field.SortOrder}," +
                                         $"\"{field.Options}\",\"{field.DefaultValue}\",\"{field.ValidationRules}\"");
                    }
                    break;
                    
                case "partners":
                    var partners = await GetPartnersAsync();
                    content.AppendLine("Id,Name,Description,Website,IsActive,UserCount,CreatedDate");
                    foreach (var partner in partners)
                    {
                        content.AppendLine($"{partner.Id},\"{partner.Name}\",\"{partner.Description}\"," +
                                         $"\"{partner.Website}\",{partner.IsActive},{partner.UserCount}," +
                                         $"\"{partner.CreatedDate:yyyy-MM-dd HH:mm}\"");
                    }
                    break;
                    
                case "product-types":
                case "producttypes":
                case "product_types":
                case "product_types_hierarchy":
                    var productTypes = await GetProductTypesAsync();
                    content.AppendLine("Id,Name,Type,Parent,Level,SortOrder,ProductCount");
                    foreach (var productType in productTypes)
                    {
                        var typeLabel = productType.Level == 0 ? "Category" : productType.Level == 1 ? "Type" : "Subcategory";
                        content.AppendLine($"{productType.Id},\"{productType.Name}\",\"{typeLabel}\"," +
                                         $"\"{productType.ParentName}\",{productType.Level + 1},{productType.SortOrder},{productType.ProductCount}");
                    }
                    break;
                    
                default:
                    content.AppendLine("No data available for export");
                    break;
            }
            
            return Encoding.UTF8.GetBytes(content.ToString());
        }
        
        private async Task<byte[]> GenerateExcelExport(string entityType)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(entityType);
                
                switch (entityType.ToLower())
                {
                    case "users":
                        var users = await GetUsersAsync();
                        
                        // Headers
                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "Name";
                        worksheet.Cell(1, 3).Value = "Email";
                        worksheet.Cell(1, 4).Value = "Role";
                        worksheet.Cell(1, 5).Value = "Status";
                        worksheet.Cell(1, 6).Value = "Companies";
                        worksheet.Cell(1, 7).Value = "Last Login";
                        
                        // Data
                        int row = 2;
                        foreach (var user in users)
                        {
                            worksheet.Cell(row, 1).Value = user.Id;
                            worksheet.Cell(row, 2).Value = user.Name;
                            worksheet.Cell(row, 3).Value = user.Email;
                            worksheet.Cell(row, 4).Value = user.Role;
                            worksheet.Cell(row, 5).Value = user.Status;
                            worksheet.Cell(row, 6).Value = string.Join(", ", user.CompanyNames);
                            worksheet.Cell(row, 7).Value = user.LastLogin?.ToString("yyyy-MM-dd HH:mm");
                            row++;
                        }
                        break;
                        
                    case "roles":
                        var roles = await GetRolesAsync();
                        
                        // Headers
                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "Name";
                        worksheet.Cell(1, 3).Value = "Description";
                        worksheet.Cell(1, 4).Value = "User Count";
                        worksheet.Cell(1, 5).Value = "Permissions";
                        
                        // Data
                        row = 2;
                        foreach (var role in roles)
                        {
                            worksheet.Cell(row, 1).Value = role.Id;
                            worksheet.Cell(row, 2).Value = role.Name;
                            worksheet.Cell(row, 3).Value = role.Description;
                            worksheet.Cell(row, 4).Value = role.UserCount;
                            worksheet.Cell(row, 5).Value = string.Join(", ", role.Permissions);
                            row++;
                        }
                        break;
                        
                    case "certificate-types":
                    case "certificatetypes":
                    case "certificate_types":
                        var certTypes = await GetCertificateTypesAsync();
                        
                        // Headers
                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "Name";
                        worksheet.Cell(1, 3).Value = "Description";
                        worksheet.Cell(1, 4).Value = "Is Active";
                        worksheet.Cell(1, 5).Value = "Certificate Count";
                        
                        // Data
                        row = 2;
                        foreach (var certType in certTypes)
                        {
                            worksheet.Cell(row, 1).Value = certType.Id;
                            worksheet.Cell(row, 2).Value = certType.Name;
                            worksheet.Cell(row, 3).Value = certType.Description;
                            worksheet.Cell(row, 4).Value = certType.IsActive;
                            worksheet.Cell(row, 5).Value = certType.CertificateCount;
                            row++;
                        }
                        break;
                        
                    case "certificate-fields":
                    case "certificatefields":
                    case "certificate_fields":
                        var certFields = await GetCertificateFieldsAsync();
                        
                        // Headers
                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "Name";
                        worksheet.Cell(1, 3).Value = "Field Type";
                        worksheet.Cell(1, 4).Value = "Required";
                        worksheet.Cell(1, 5).Value = "Visible";
                        worksheet.Cell(1, 6).Value = "Sort Order";
                        worksheet.Cell(1, 7).Value = "Options";
                        worksheet.Cell(1, 8).Value = "Default Value";
                        worksheet.Cell(1, 9).Value = "Validation Rules";
                        
                        // Data
                        row = 2;
                        foreach (var field in certFields)
                        {
                            worksheet.Cell(row, 1).Value = field.Id;
                            worksheet.Cell(row, 2).Value = field.Name;
                            worksheet.Cell(row, 3).Value = field.FieldType;
                            worksheet.Cell(row, 4).Value = field.IsRequired;
                            worksheet.Cell(row, 5).Value = field.IsVisible;
                            worksheet.Cell(row, 6).Value = field.SortOrder;
                            worksheet.Cell(row, 7).Value = field.Options;
                            worksheet.Cell(row, 8).Value = field.DefaultValue;
                            worksheet.Cell(row, 9).Value = field.ValidationRules;
                            row++;
                        }
                        break;
                        
                    case "partners":
                        var partnersData = await GetPartnersAsync();
                        
                        // Headers
                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "Name";
                        worksheet.Cell(1, 3).Value = "Description";
                        worksheet.Cell(1, 4).Value = "Website";
                        worksheet.Cell(1, 5).Value = "Active";
                        worksheet.Cell(1, 6).Value = "User Count";
                        worksheet.Cell(1, 7).Value = "Created Date";
                        
                        // Data
                        row = 2;
                        foreach (var partner in partnersData)
                        {
                            worksheet.Cell(row, 1).Value = partner.Id;
                            worksheet.Cell(row, 2).Value = partner.Name;
                            worksheet.Cell(row, 3).Value = partner.Description;
                            worksheet.Cell(row, 4).Value = partner.Website;
                            worksheet.Cell(row, 5).Value = partner.IsActive;
                            worksheet.Cell(row, 6).Value = partner.UserCount;
                            worksheet.Cell(row, 7).Value = partner.CreatedDate.ToString("yyyy-MM-dd HH:mm");
                            row++;
                        }
                        break;
                        
                    case "product-types":
                    case "producttypes":
                    case "product_types":
                    case "product_types_hierarchy":
                        var productTypes = await GetProductTypesAsync();
                        
                        // Headers
                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "Name";
                        worksheet.Cell(1, 3).Value = "Type";
                        worksheet.Cell(1, 4).Value = "Parent";
                        worksheet.Cell(1, 5).Value = "Level";
                        worksheet.Cell(1, 6).Value = "Sort Order";
                        worksheet.Cell(1, 7).Value = "Product Count";
                        
                        // Data
                        row = 2;
                        foreach (var productType in productTypes)
                        {
                            var typeLabel = productType.Level == 0 ? "Category" : productType.Level == 1 ? "Type" : "Subcategory";
                            worksheet.Cell(row, 1).Value = productType.Id;
                            worksheet.Cell(row, 2).Value = productType.Name;
                            worksheet.Cell(row, 3).Value = typeLabel;
                            worksheet.Cell(row, 4).Value = productType.ParentName;
                            worksheet.Cell(row, 5).Value = productType.Level + 1;
                            worksheet.Cell(row, 6).Value = productType.SortOrder;
                            worksheet.Cell(row, 7).Value = productType.ProductCount;
                            row++;
                        }
                        break;
                        
                    default:
                        worksheet.Cell(1, 1).Value = "No data available for export";
                        break;
                }
                
                // Style the headers
                var headerRange = worksheet.Range(1, 1, 1, worksheet.LastCellUsed()?.Address.ColumnNumber ?? 1);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                
                // Auto-fit columns
                worksheet.Columns().AdjustToContents();
                
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        // Two-Factor Authentication Management
        public async Task<bool> Reset2FAAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                // Reset 2FA settings
                user.TwoFactorEnabled = false;
                user.TwoFactorSecret = null;
                user.RecoveryCodes = null;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                // Log error here in production
                return false;
            }
        }

        public async Task<bool> Disable2FAAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                // Disable 2FA
                user.TwoFactorEnabled = false;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                // Log error here in production
                return false;
            }
        }

        public async Task<bool> Enable2FAAsync(int userId, string twoFactorMethod)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                // Enable 2FA with specified method
                user.TwoFactorEnabled = true;
                user.TwoFactorMethod = twoFactorMethod;
                // User will need to set up 2FA on next login (secret is null until they complete setup)
                user.TwoFactorSecret = null;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                // Log error here in production
                return false;
            }
        }
    }
}