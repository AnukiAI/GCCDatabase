using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GCCWebApp.Models;
using GCCWebApp.Data;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Services.Audit;
using System.Text;

namespace GCCWebApp.Controllers
{
    [Authorize(Policy = "ExternalAccess")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<ProductController> _logger;
        private readonly bool _showDetailedErrors;

        public ProductController(
            ApplicationDbContext context,
            IAuditService auditService,
            ILogger<ProductController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
            _showDetailedErrors = configuration.GetValue<bool>("FeatureFlags:ShowDetailedApiErrors", false);
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
                var userPartnerNames = await GetCurrentUserPartnerNamesAsync();

                var productsQuery = _context.Products
                    .Include(p => p.Certificates)
                    .AsQueryable();
                var companiesQuery = _context.Companies.AsQueryable();

                // Apply role-based filtering (per Design Recommendations - NO OR logic)
                // Partner role: sees ALL products with partner-tagged certificates
                // External role: sees ONLY products from their assigned companies
                // Admin/Analyst: sees all (no filtering)
                if (userRole == "Partner")
                {
                    // Partner users see products that have certificates tagged with their partner
                    if (userPartnerNames.Any())
                    {
                        productsQuery = productsQuery.Where(p =>
                            p.Certificates.Any(c => userPartnerNames.Contains(c.Partner ?? "None")));
                    }
                    else
                    {
                        // Partner user with no partner association - show nothing
                        productsQuery = productsQuery.Where(p => false);
                    }
                }
                else if (userRole == "External")
                {
                    // External users see ONLY products from their assigned companies
                    // (No partner filtering - that's for Partner role only)
                    if (userCompanyIds.Any())
                    {
                        productsQuery = productsQuery.Where(p =>
                            p.CompanyId.HasValue && userCompanyIds.Contains(p.CompanyId.Value));
                        companiesQuery = companiesQuery.Where(c => userCompanyIds.Contains(c.Id));
                    }
                    else
                    {
                        // External user with no company association - show nothing
                        productsQuery = productsQuery.Where(p => false);
                    }
                }

                var totalProducts = await productsQuery.CountAsync();
                var totalCategories = await _context.ProductCategories.CountAsync();
                var totalTypes = await _context.ProductTypes.CountAsync();
                var totalCertifiedProducts = await productsQuery
                    .Where(p => p.Certificates.Any())
                    .CountAsync();

                // For role-filtered users, show most popular category within their data scope
                var mostPopularCategory = await _context.ProductCategories
                    .Include(pc => pc.Products)
                        .ThenInclude(p => p.Certificates)
                    .Where(pc => pc.Products.Any(p =>
                        // Admin/Analyst see all
                        (userRole != "External" && userRole != "Partner") ||
                        // Partner role: products with partner-tagged certificates
                        (userRole == "Partner" && p.Certificates.Any(c => userPartnerNames.Contains(c.Partner ?? "None"))) ||
                        // External role: products from their companies
                        (userRole == "External" && p.CompanyId.HasValue && userCompanyIds.Contains(p.CompanyId.Value))
                    ))
                    .OrderByDescending(pc => pc.Products.Count(p =>
                        (userRole != "External" && userRole != "Partner") ||
                        (userRole == "Partner" && p.Certificates.Any(c => userPartnerNames.Contains(c.Partner ?? "None"))) ||
                        (userRole == "External" && p.CompanyId.HasValue && userCompanyIds.Contains(p.CompanyId.Value))
                    ))
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
                var userPartnerNames = await GetCurrentUserPartnerNamesAsync();

                var query = _context.Products
                    .Where(p => !p.IsDeleted)
                    .Include(p => p.Category)
                    .Include(p => p.ProductType)
                    .Include(p => p.Company)
                    .Include(p => p.Certificates)
                        .ThenInclude(c => c.ProductIds)
                    .AsQueryable();

                // Apply role-based filtering (per Design Recommendations - NO OR logic)
                // Partner role: sees ALL products with partner-tagged certificates
                // External role: sees ONLY products from their assigned companies
                // Admin/Analyst: sees all (no filtering)
                if (userRole == "Partner")
                {
                    // Partner users see products that have certificates tagged with their partner
                    if (userPartnerNames.Any())
                    {
                        query = query.Where(p =>
                            p.Certificates.Any(c => userPartnerNames.Contains(c.Partner ?? "None")));
                    }
                    else
                    {
                        // Partner user with no partner association - show nothing
                        query = query.Where(p => false);
                    }
                }
                else if (userRole == "External")
                {
                    // External users see ONLY products from their assigned companies
                    // (No partner filtering - that's for Partner role only)
                    if (userCompanyIds.Any())
                    {
                        query = query.Where(p =>
                            p.CompanyId.HasValue && userCompanyIds.Contains(p.CompanyId.Value));
                    }
                    else
                    {
                        // External user with no company association - show nothing
                        query = query.Where(p => false);
                    }
                }
                // Admin and Analyst can see all products (no filtering needed)

                // First, get the products with related data
                // Now pulling identifiers from CERTIFICATE level (CertificateProductId) instead of Product level
                var productsData = await query
                    // DEF-253 #2: AsSplitQuery on the two nested Certificates.{ProductIds,Partners}
                    // sub-queries. Measured both ways on the REAL endpoint (not the page shell): split =
                    // ~26s cold then 1 fast; single = ~25s cold + a 2nd ~17s call. So split is neutral-
                    // to-slightly-better and is KEPT. The ~25s FIRST call is an inherent EF cold-start on
                    // this full-table load — NOT caused by split — tracked separately as DEF-255 (it can
                    // make the post-save loadProducts() refresh time out and look like a failed save).
                    .AsSplitQuery()
                    .OrderBy(p => p.Id)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Description,
                        p.Model,
                        p.Sku,
                        p.CompanyId,
                        p.CategoryId,
                        p.ProductTypeId,
                        p.ProductSubcategoryId,
                        CompanyName = p.Company != null ? p.Company.Name : "",
                        CategoryName = p.Category != null ? p.Category.Name : "",
                        ProductTypeName = p.ProductType != null ? p.ProductType.Name : "",
                        ProductSubcategoryName = p.ProductSubcategory != null ? p.ProductSubcategory.Name : "",
                        p.IsActive,
                        CertificateCount = p.Certificates.Count(),
                        p.CreatedAt,
                        // Get identifiers from certificate-level (CertificateProductId) - flatten all certificates' product IDs
                        CertificateIdentifiers = p.Certificates
                            .SelectMany(c => c.ProductIds)
                            .Where(pid => pid.IsActive)
                            .Select(pid => new { pid.ProductIdType, pid.ProductIdValue })
                            .ToList(),
                        // DEF-202 (QC #184): distinct Partner labels from this product's certs so the
                        // Product Management export can include a Partner column. Empty/None values
                        // dropped client-side below.
                        CertPartners = p.Certificates
                            .Select(c => c.Partner)
                            .ToList()
                    })
                    .ToListAsync();

                // Now transform to view model with client-side grouping of identifiers
                var products = productsData.Select(p => new ProductListViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Model = p.Model,
                    Sku = p.Sku,
                    CompanyId = p.CompanyId,
                    CategoryId = p.CategoryId,
                    ProductTypeId = p.ProductTypeId,
                    ProductSubcategoryId = p.ProductSubcategoryId,
                    CompanyName = p.CompanyName,
                    CategoryName = p.CategoryName,
                    ProductTypeName = p.ProductTypeName,
                    ProductSubcategoryName = p.ProductSubcategoryName,
                    Status = p.IsActive ? "Active" : "Inactive",
                    CertificateCount = p.CertificateCount,
                    CreatedAt = p.CreatedAt,
                    // Group certificate-level identifiers by type (ASIN, UPC, etc.)
                    Identifiers = p.CertificateIdentifiers
                        .GroupBy(pi => pi.ProductIdType)
                        .ToDictionary(g => g.Key.ToLower(), g => g.Select(pi => pi.ProductIdValue).Distinct().ToList()),
                    // DEF-202 (QC #184): distinct Partner names — empty/None dropped.
                    Partners = (p.CertPartners ?? new List<string?>())
                        .Where(name => !string.IsNullOrWhiteSpace(name) && name != "None")
                        .Select(name => name!)
                        .Distinct()
                        .OrderBy(name => name)
                        .ToList()
                }).ToList();

                return Json(new ApiResponse<List<ProductListViewModel>> { Success = true, Data = products });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");

                if (_showDetailedErrors)
                {
                    return Json(new ApiResponse<List<ProductListViewModel>>
                    {
                        Success = false,
                        Message = $"Error: {ex.Message} | Inner: {ex.InnerException?.Message} | Stack: {ex.StackTrace}"
                    });
                }

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

            if (!model.CompanyId.HasValue || model.CompanyId.Value <= 0)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Company is required. Please select a company." });
            }

            if (model.CategoryId <= 0)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Category is required. Please select a category." });
            }

            try
            {
                Product product;
                Product? oldProduct = null;
                var isUpdate = model.Id > 0;

                if (isUpdate)
                {
                    // Update existing product
                    var existingProduct = await _context.Products.FindAsync(model.Id);
                    if (existingProduct == null)
                    {
                        return Json(new ApiResponse<object> { Success = false, Message = "Product not found" });
                    }

                    // Capture old state for audit (detached from context)
                    oldProduct = await _context.Products
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == model.Id);

                    product = existingProduct;

                    product.Name = model.Name;
                    product.Description = model.Description;
                    // Sku and Model are not editable from the Product Mgmt modal (removed in QC #92).
                    // The JS payload omits them, so deserialization makes them null — assigning would
                    // wipe values set by CSV import or any other path. Only update when the caller
                    // explicitly sent a non-null value. (QC #91 fix)
                    if (model.Sku != null) product.Sku = model.Sku;
                    if (model.Model != null) product.Model = model.Model;
                    product.CategoryId = model.CategoryId;
                    product.ProductTypeId = model.ProductTypeId;
                    product.ProductSubcategoryId = model.ProductSubcategoryId;
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
                        ProductSubcategoryId = model.ProductSubcategoryId,
                        CompanyId = model.CompanyId,
                        IsActive = model.IsActive,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Products.Add(product);
                }

                await _context.SaveChangesAsync();

                // Log audit history
                try
                {
                    var userId = GetCurrentUserId();
                    var username = GetCurrentUsername();
                    var action = isUpdate ? "UPDATE" : "CREATE";

                    await _auditService.LogEntityChangeAsync(
                        product.Id.ToString(),
                        product.Name,
                        action,
                        oldProduct,
                        product,
                        userId,
                        username,
                        null, // No reason provided
                        "Web UI"
                    );
                }
                catch (Exception auditEx)
                {
                    // Log audit failure but don't fail the operation
                    Console.WriteLine($"Failed to log audit history: {auditEx.Message}");
                }

                var productViewModel = new ProductViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description ?? string.Empty,
                    IsActive = product.IsActive
                };

                return Json(new ApiResponse<ProductViewModel> { Success = true, Data = productViewModel, Message = isUpdate ? "Product updated successfully" : "Product created successfully" });
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

                // Capture product state before deletion for audit
                var productName = product.Name;
                var oldProduct = await _context.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id);

                // Check if product has certificates before deleting
                var certificateCount = await _context.Certificates.CountAsync(c => c.ProductId == id);

                if (certificateCount > 0)
                {
                    // Block deletion — product is tied to certificates
                    return Json(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Cannot delete \"{productName}\" because it is associated with {certificateCount} certificate{(certificateCount != 1 ? "s" : "")}. Remove the certificate associations first before deleting this product."
                    });
                }

                // Hard delete — no certificates reference this product
                _context.Products.Remove(product);

                await _context.SaveChangesAsync();

                // Log audit history
                try
                {
                    var userId = GetCurrentUserId();
                    var username = GetCurrentUsername();

                    await _auditService.LogEntityChangeAsync(
                        id.ToString(),
                        productName,
                        "DELETE",
                        oldProduct,
                        null,
                        userId,
                        username,
                        "Hard delete - Product has no certificates",
                        "Web UI"
                    );
                }
                catch (Exception auditEx)
                {
                    Console.WriteLine($"Failed to log audit history: {auditEx.Message}");
                }

                return Json(new ApiResponse<object> { Success = true, Message = $"\"{productName}\" has been deleted successfully." });
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
                // Roots only (ParentId IS NULL). Level-2 children belong in the Subcategory
                // cascade, not the Type dropdown — mixing them was DEF-134 follow-up noise.
                var productTypes = await _context.ProductTypes
                    .Where(pt => pt.IsActive && pt.ParentId == null)
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

        private async Task<List<string>> GetCurrentUserPartnerNamesAsync()
        {
            var userEmail = User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userEmail))
                return new List<string>();

            // Find user and their associated partners
            var user = await _context.Users
                .Include(u => u.Partners)
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user?.Partners != null && user.Partners.Any())
            {
                return user.Partners.Select(p => p.Name).ToList();
            }

            return new List<string>();
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetCurrentUsername()
        {
            return User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                ?? User?.Identity?.Name
                ?? "Unknown";
        }

        /// <summary>
        /// Import products from CSV file
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> ImportProducts(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "No file provided" });
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Only CSV files are supported" });
            }

            try
            {
                var results = new { successCount = 0, errorCount = 0, errors = new List<string>() };
                var successCount = 0;
                var errorCount = 0;
                var errors = new List<string>();

                // Load companies into memory for efficient lookups and track auto-created ones
                var companies = await _context.Companies.Where(c => !c.IsDeleted).ToListAsync();
                var createdCompanies = new List<Company>();

                using (var stream = new StreamReader(file.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    var line = await stream.ReadLineAsync(); // Skip header
                    var lineNumber = 1;

                    while ((line = await stream.ReadLineAsync()) != null)
                    {
                        lineNumber++;
                        try
                        {
                            var values = ParseCsvLine(line);

                            if (values.Length < 2) // Minimum: Name and Company
                            {
                                errors.Add($"Line {lineNumber}: Insufficient fields (Name and Company are required)");
                                errorCount++;
                                continue;
                            }

                            // CSV Format: Name, Company, Description, Category, Product Type, Subcategory, Status
                            var productName = values[0];
                            var companyName = values.Length > 1 ? values[1] : "";
                            var description = values.Length > 2 ? values[2] : null;
                            var categoryName = values.Length > 3 ? values[3] : "";
                            var productTypeName = values.Length > 4 ? values[4] : "";
                            var subcategoryName = values.Length > 5 ? values[5] : "";
                            var statusValue = values.Length > 6 ? values[6] : "";

                            // Status: blank or "Active" → IsActive=true; "Inactive" → IsActive=false
                            bool isActive = !statusValue.Trim().Equals("Inactive", StringComparison.OrdinalIgnoreCase);

                            if (string.IsNullOrWhiteSpace(productName))
                            {
                                errors.Add($"Line {lineNumber}: Product name is required");
                                errorCount++;
                                continue;
                            }

                            // Find or create company (case-insensitive)
                            Company? company = null;
                            if (!string.IsNullOrWhiteSpace(companyName))
                            {
                                company = companies.FirstOrDefault(c =>
                                    c.Name.Equals(companyName, StringComparison.OrdinalIgnoreCase));

                                if (company == null)
                                {
                                    // Auto-create the company
                                    company = new Company
                                    {
                                        Name = companyName,
                                        CreatedAt = DateTime.UtcNow,
                                        UpdatedAt = DateTime.UtcNow,
                                        IsDeleted = false,
                                        IsActive = true
                                    };

                                    _context.Companies.Add(company);
                                    companies.Add(company); // Add to in-memory list for subsequent lookups
                                    createdCompanies.Add(company); // Track for response
                                }
                            }

                            // Find category
                            var category = await _context.ProductCategories.FirstOrDefaultAsync(c => c.Name == categoryName);
                            if (category == null && !string.IsNullOrWhiteSpace(categoryName))
                            {
                                errors.Add($"Line {lineNumber}: Category '{categoryName}' not found");
                                errorCount++;
                                continue;
                            }

                            // Find product type (Level-1, ParentId == null)
                            int? productTypeId = null;
                            if (!string.IsNullOrWhiteSpace(productTypeName))
                            {
                                var productType = await _context.ProductTypes
                                    .FirstOrDefaultAsync(pt => pt.Name == productTypeName && pt.ParentId == null);
                                if (productType != null) productTypeId = productType.Id;
                            }

                            // Find subcategory (Level-2 ProductTypes child of the resolved type)
                            int? productSubcategoryId = null;
                            if (!string.IsNullOrWhiteSpace(subcategoryName) && productTypeId.HasValue)
                            {
                                var subcategory = await _context.ProductTypes
                                    .FirstOrDefaultAsync(pt => pt.Name == subcategoryName && pt.ParentId == productTypeId.Value);
                                if (subcategory != null) productSubcategoryId = subcategory.Id;
                            }

                            // CR-186 SPLIT: a product is unique by (Company, Name, Category, Type, Subcategory).
                            // Dedup on the full identity tuple — same Name with a DIFFERENT Type/Subcategory is a
                            // distinct product (its own line item), not a duplicate. (Was: Name-only, which
                            // wrongly rejected genuinely-distinct combos.) Null Type/Subcategory matched in-memory.
                            int categoryIdForProduct = category?.Id ?? 0;
                            int? companyIdForProduct = company?.Id;
                            var dupCandidates = await _context.Products
                                .Where(p => !p.IsDeleted && p.Name == productName && p.CategoryId == categoryIdForProduct)
                                .ToListAsync();
                            var existingProduct = dupCandidates.FirstOrDefault(p =>
                                p.CompanyId == companyIdForProduct &&
                                p.ProductTypeId == productTypeId &&
                                p.ProductSubcategoryId == productSubcategoryId);

                            if (existingProduct != null)
                            {
                                errors.Add($"Line {lineNumber}: Product '{productName}' with the same Category/Type/Subcategory already exists");
                                errorCount++;
                                continue;
                            }

                            // Create product
                            var product = new Product
                            {
                                Name = productName,
                                Description = description,
                                CategoryId = category?.Id ?? 0,
                                ProductTypeId = productTypeId,
                                ProductSubcategoryId = productSubcategoryId,
                                CompanyId = company?.Id,
                                IsActive = isActive,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            _context.Products.Add(product);
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Line {lineNumber}: {ex.Message}");
                            errorCount++;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Log audit trail for auto-created companies
                if (createdCompanies.Any())
                {
                    var userId = GetCurrentUserId();
                    var username = GetCurrentUsername();
                    foreach (var createdCompany in createdCompanies)
                    {
                        try
                        {
                            await _auditService.LogEntityChangeAsync(
                                createdCompany.Id.ToString(),
                                createdCompany.Name,
                                "CREATE",
                                null,
                                createdCompany,
                                userId,
                                username,
                                "Auto-created during product import",
                                "Web UI"
                            );
                        }
                        catch (Exception auditEx)
                        {
                            Console.WriteLine($"Failed to log company creation audit: {auditEx.Message}");
                        }
                    }
                }

                var message = $"Import completed. {successCount} products imported successfully";
                if (errorCount > 0)
                {
                    message += $", {errorCount} errors occurred";
                }
                if (createdCompanies.Any())
                {
                    message += $", {createdCompanies.Count} new {(createdCompanies.Count == 1 ? "company" : "companies")} created";
                }

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        successCount,
                        errorCount,
                        errors = errors.Take(10).ToList(),
                        companiesCreated = createdCompanies.Select(c => new { c.Id, c.Name }).ToList()
                    },
                    Message = message
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Import failed: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Download sample CSV template for product import
        /// </summary>
        [HttpGet]
        public IActionResult DownloadProductImportTemplate()
        {
            // Headers only — no sample data rows. 6 columns matching the Add New Product modal
            // and the Product Management table (QC #103: template/table/popup must be "level").
            var csv = "Product Name,Company,Description,Category,Product Type,Subcategory";

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", "product_import_template.csv");
        }

        private string[] ParseCsvLine(string line)
        {
            var values = new List<string>();
            var current = new System.Text.StringBuilder();
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Escaped quote
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            values.Add(current.ToString().Trim());
            return values.ToArray();
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
        public int? CompanyId { get; set; }
        public int CategoryId { get; set; }
        public int? ProductTypeId { get; set; }
        public int? ProductSubcategoryId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ProductTypeName { get; set; } = string.Empty;
        public string ProductSubcategoryName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int CertificateCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, List<string>> Identifiers { get; set; } = new Dictionary<string, List<string>>();

        // DEF-202 (QC #184): distinct Partner values derived from the product's certificates.
        // Cert.Partner is a single string column today (multi-partner per cert is tracked as
        // QC #171). For a product touched by multiple cert→partner pairs, we surface the
        // unique set so the Product Management export can show "Amazon; Novi" etc. without
        // forcing the analyst to drill into each cert.
        public List<string> Partners { get; set; } = new List<string>();
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
        public int? ProductSubcategoryId { get; set; }
        public int? CompanyId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}