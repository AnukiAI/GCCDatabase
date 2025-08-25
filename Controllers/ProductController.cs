using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GCCWebApp.Models;
using GCCWebApp.Data;
using Microsoft.EntityFrameworkCore;

namespace GCCWebApp.Controllers
{
    [Authorize(Policy = "ExternalAccess")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Removed Index action - not part of sitemap/wireframes
        // Use Management action instead for product-related views

        public IActionResult Management()
        {
            return View();
        }

        public IActionResult ManagementAlt()
        {
            return View();
        }

        public IActionResult ManagementV2()
        {
            return View();
        }

        public IActionResult Categories()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var userCompanyIds = await GetCurrentUserCompanyIdsAsync();
                
                var productsQuery = _context.Products.AsQueryable();
                var companiesQuery = _context.Companies.AsQueryable();

                // Apply filtering for External users
                if (userRole == "External" && userCompanyIds.Any())
                {
                    productsQuery = productsQuery.Where(p => p.CompanyId.HasValue && userCompanyIds.Contains(p.CompanyId.Value));
                    companiesQuery = companiesQuery.Where(c => userCompanyIds.Contains(c.Id));
                }

                var totalProducts = await productsQuery.CountAsync();
                var totalCategories = await _context.ProductCategories.CountAsync();
                var totalTypes = await _context.ProductTypes.CountAsync();
                var totalCertifiedProducts = await productsQuery
                    .Where(p => p.Certificates.Any())
                    .CountAsync();

                // For External users, show most popular category within their companies
                var mostPopularCategory = await _context.ProductCategories
                    .Include(pc => pc.Products)
                    .Where(pc => pc.Products.Any(p => userRole != "External" || 
                                                    (p.CompanyId.HasValue && userCompanyIds.Contains(p.CompanyId.Value))))
                    .OrderByDescending(pc => pc.Products.Count(p => userRole != "External" || 
                                                                 (p.CompanyId.HasValue && userCompanyIds.Contains(p.CompanyId.Value))))
                    .Select(pc => pc.Name)
                    .FirstOrDefaultAsync();

                var recentProducts = await productsQuery
                    .Where(p => p.CreatedAt >= DateTime.UtcNow.AddMonths(-1))
                    .CountAsync();

                var activeProducts = await productsQuery
                    .Where(p => p.IsActive)
                    .CountAsync();

                var companiesWithProducts = await companiesQuery
                    .Where(c => c.Products.Any())
                    .CountAsync();

                var stats = new
                {
                    totalProducts,
                    totalCategories,
                    totalTypes,
                    totalCertifiedProducts,
                    mostPopularCategory = mostPopularCategory ?? "N/A",
                    recentProducts,
                    activeProducts,
                    companiesWithProducts
                };

                return Json(new ApiResponse<object> { Success = true, Data = stats });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var userCompanyIds = await GetCurrentUserCompanyIdsAsync();
                
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductType)
                    .Include(p => p.Company)
                    .Include(p => p.Certificates)
                    .Include(p => p.ProductIdentifiers)
                    .AsQueryable();

                // Apply filtering based on user role
                if (userRole == "External")
                {
                    // External users can only see products from their associated companies
                    if (userCompanyIds.Any())
                    {
                        query = query.Where(p => p.CompanyId.HasValue && userCompanyIds.Contains(p.CompanyId.Value));
                    }
                    else
                    {
                        // If no companies associated, return empty result
                        query = query.Where(p => false);
                    }
                }
                // Admin and Analyst can see all products (no additional filtering needed)

                var products = await query
                    .Select(p => new ProductListViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Model = p.Model,
                        Sku = p.Sku,
                        CompanyName = p.Company != null ? p.Company.Name : "",
                        CategoryName = p.Category.Name,
                        ProductTypeName = p.ProductType != null ? p.ProductType.Name : "",
                        Status = p.IsActive ? "Active" : "Inactive",
                        CertificateCount = p.Certificates.Count(),
                        CreatedAt = p.CreatedAt,
                        Identifiers = p.ProductIdentifiers
                            .GroupBy(pi => pi.IdentifierType)
                            .ToDictionary(g => g.Key.ToLower(), g => g.Select(pi => pi.IdentifierValue).ToList())
                    })
                    .ToListAsync();

                return Json(new ApiResponse<List<ProductListViewModel>> { Success = true, Data = products });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<ProductListViewModel>> { Success = false, Message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCompanies()
        {
            try
            {
                var userRole = GetCurrentUserRole();
                var userCompanyIds = await GetCurrentUserCompanyIdsAsync();
                
                var query = _context.Companies.Where(c => c.IsActive);

                // Apply filtering for External users
                if (userRole == "External" && userCompanyIds.Any())
                {
                    query = query.Where(c => userCompanyIds.Contains(c.Id));
                }

                var companies = await query
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync();

                return Json(new ApiResponse<object> { Success = true, Data = companies });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.ProductCategories
                    .Where(pc => pc.IsActive)
                    .Include(pc => pc.Products)
                    .Select(pc => new ProductCategoryViewModel
                    {
                        Id = pc.Id,
                        Name = pc.Name,
                        Description = pc.Description ?? "",
                        Level = 1, // Default level since model doesn't have hierarchy
                        ParentCategory = "", // No parent since model doesn't have hierarchy
                        ProductCount = pc.Products.Count,
                        Status = pc.IsActive ? "Active" : "Inactive",
                        SortOrder = pc.SortOrder
                    })
                    .OrderBy(pc => pc.SortOrder)
                    .ToListAsync();

                return Json(new ApiResponse<List<ProductCategoryViewModel>> { Success = true, Data = categories });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<ProductCategoryViewModel>> { Success = false, Message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> SaveProduct([FromBody] ProductSaveModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Invalid data provided" });
            }

            try
            {
                Product product;
                
                if (model.Id > 0)
                {
                    // Update existing product
                    product = await _context.Products.FindAsync(model.Id);
                    if (product == null)
                    {
                        return Json(new ApiResponse<object> { Success = false, Message = "Product not found" });
                    }
                    
                    product.Name = model.Name;
                    product.Description = model.Description;
                    product.Model = model.Model;
                    product.Sku = model.Sku;
                    product.CategoryId = model.CategoryId;
                    product.ProductTypeId = model.ProductTypeId;
                    product.CompanyId = model.CompanyId;
                    product.IsActive = model.IsActive;
                    product.UpdatedAt = DateTime.UtcNow;
                    
                    _context.Products.Update(product);
                }
                else
                {
                    // Create new product
                    product = new Product
                    {
                        Name = model.Name,
                        Description = model.Description,
                        Model = model.Model,
                        Sku = model.Sku,
                        CategoryId = model.CategoryId,
                        ProductTypeId = model.ProductTypeId,
                        CompanyId = model.CompanyId,
                        IsActive = model.IsActive,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    _context.Products.Add(product);
                }

                await _context.SaveChangesAsync();

                var productViewModel = new ProductViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    IsActive = product.IsActive
                };

                return Json(new ApiResponse<ProductViewModel> { Success = true, Data = productViewModel, Message = model.Id > 0 ? "Product updated successfully" : "Product created successfully" });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Product not found" });
                }

                // Check if product has certificates before deleting
                var hasCertificates = await _context.Certificates.AnyAsync(c => c.ProductId == id);
                if (hasCertificates)
                {
                    // Soft delete - just mark as inactive
                    product.IsActive = false;
                    product.UpdatedAt = DateTime.UtcNow;
                    _context.Products.Update(product);
                }
                else
                {
                    // Hard delete if no certificates
                    _context.Products.Remove(product);
                }

                await _context.SaveChangesAsync();
                
                return Json(new ApiResponse<object> { Success = true, Message = "Product deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProductTypes()
        {
            try
            {
                var productTypes = await _context.ProductTypes
                    .Where(pt => pt.IsActive)
                    .Include(pt => pt.Category)
                    .Select(pt => new { pt.Id, pt.Name, CategoryId = pt.CategoryId, CategoryName = pt.Category.Name })
                    .ToListAsync();

                return Json(new ApiResponse<object> { Success = true, Data = productTypes });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AnalystOrAdmin")]
        public IActionResult SaveCategory([FromBody] ProductCategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Invalid data provided" });
            }

            // Add logic to save category
            // For now, return success
            return Json(new ApiResponse<ProductCategoryViewModel> { Success = true, Data = model, Message = "Category saved successfully" });
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AnalystOrAdmin")]
        public IActionResult DeleteCategory(int id)
        {
            // Add logic to delete category
            // For now, return success
            return Json(new ApiResponse<object> { Success = true, Message = "Category deleted successfully" });
        }

        [HttpGet]
        public IActionResult GetParentCategories()
        {
            // Sample data for parent category dropdown
            var parentCategories = new List<object>
            {
                new { Id = 1, Name = "Building Materials" },
                new { Id = 2, Name = "Lumber" },
                new { Id = 5, Name = "Flooring" },
                new { Id = 6, Name = "Furniture" },
                new { Id = 7, Name = "Office Furniture" },
                new { Id = 8, Name = "Home Furniture" }
            };

            return Json(new ApiResponse<List<object>> { Success = true, Data = parentCategories });
        }

        private string GetCurrentUserRole()
        {
            return User?.FindFirst("Role")?.Value ?? "Public";
        }

        private async Task<List<int>> GetCurrentUserCompanyIdsAsync()
        {
            var userEmail = User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            
            if (string.IsNullOrEmpty(userEmail))
                return new List<int>();

            // Find user and their associated companies
            var user = await _context.Users
                .Include(u => u.Companies)
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user?.Companies != null)
            {
                return user.Companies.Select(c => c.Id).ToList();
            }

            return new List<int>();
        }
    }

    public class ProductCategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Level { get; set; }
        public string ParentCategory { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    public class ProductListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Model { get; set; }
        public string? Sku { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ProductTypeName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int CertificateCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, List<string>> Identifiers { get; set; } = new Dictionary<string, List<string>>();
    }

    public class ProductSaveModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Model { get; set; }
        public string? Sku { get; set; }
        public int CategoryId { get; set; }
        public int? ProductTypeId { get; set; }
        public int? CompanyId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}