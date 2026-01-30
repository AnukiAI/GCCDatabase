using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Models;
using GCCWebApp.Services;
using GCCWebApp.Data;
using GCCWebApp.Middleware;
using System.Text.Json;

namespace GCCWebApp.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly RateLimitOptions _rateLimitOptions;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger, ApplicationDbContext context, RateLimitOptions rateLimitOptions)
        {
            _adminService = adminService;
            _logger = logger;
            _context = context;
            _rateLimitOptions = rateLimitOptions;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }

        // GET: Admin Dashboard
        public async Task<IActionResult> Index()
        {
            var dashboardModel = await _adminService.GetDashboardDataAsync();
            return View(dashboardModel);
        }

        // User Management
        public IActionResult UserManagement()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _adminService.GetUsersAsync();
                return Json(new ApiResponse<List<UserViewModel>> { Success = true, Data = users });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<UserViewModel>> 
                { 
                    Success = false, 
                    Message = "Error retrieving users: " + ex.Message 
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse<object> 
                { 
                    Success = false, 
                    Message = "Invalid data provided",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var currentUserId = GetCurrentUserId() ?? 0;
                var currentUsername = User.Identity?.Name ?? "System";
                var success = await _adminService.CreateUserAsync(model, currentUserId, currentUsername);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "User created successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Failed to create user" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error creating user: " + ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse<object> 
                { 
                    Success = false, 
                    Message = "Invalid data provided",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var currentUserId = GetCurrentUserId() ?? 0;
                var currentUsername = User.Identity?.Name ?? "System";
                var success = await _adminService.UpdateUserAsync(id, model, currentUserId, currentUsername);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "User updated successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "User not found or update failed" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error updating user: " + ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId() ?? 0;
                var currentUsername = User.Identity?.Name ?? "System";
                var success = await _adminService.DeleteUserAsync(id, currentUserId, currentUsername);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "User deleted successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Cannot delete user. Admin users cannot be deleted or user not found." });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error deleting user: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportUsers(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "No file provided" });
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Only CSV files are supported" });
            }

            // Get current user info for audit trail
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var currentUsername = User.Identity?.Name ?? "System";
            var userId = int.TryParse(currentUserId, out var id) ? id : 0;

            try
            {
                var successCount = 0;
                var errorCount = 0;
                var errors = new List<string>();

                using (var stream = new StreamReader(file.OpenReadStream()))
                {
                    var line = await stream.ReadLineAsync(); // Skip header
                    var lineNumber = 1;

                    while ((line = await stream.ReadLineAsync()) != null)
                    {
                        lineNumber++;
                        try
                        {
                            var values = ParseCsvLine(line);

                            if (values.Length < 4) // Minimum required fields
                            {
                                errors.Add($"Line {lineNumber}: Insufficient fields");
                                errorCount++;
                                continue;
                            }

                            // CSV Format: Name, Email, Role, Password, CompanyIds (comma-separated)
                            var model = new CreateUserViewModel
                            {
                                Name = values[0],
                                Email = values[1],
                                Role = values[2],
                                Password = values.Length > 3 ? values[3] : "TempPassword123!",
                                CompanyIds = values.Length > 4 && !string.IsNullOrWhiteSpace(values[4])
                                    ? values[4].Split(';').Select(int.Parse).ToList()
                                    : new List<int>(),
                                RequirePasswordReset = true
                            };

                            if (string.IsNullOrWhiteSpace(model.Name))
                            {
                                errors.Add($"Line {lineNumber}: Name is required");
                                errorCount++;
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(model.Email))
                            {
                                errors.Add($"Line {lineNumber}: Email is required");
                                errorCount++;
                                continue;
                            }

                            var success = await _adminService.CreateUserAsync(model, userId, currentUsername);
                            if (success)
                            {
                                successCount++;
                            }
                            else
                            {
                                errors.Add($"Line {lineNumber}: Failed to create user (email may already exist)");
                                errorCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Line {lineNumber}: {ex.Message}");
                            errorCount++;
                        }
                    }
                }

                var message = $"Import completed. {successCount} users imported successfully";
                if (errorCount > 0)
                {
                    message += $", {errorCount} errors occurred";
                }

                return Json(new ApiResponse<object>
                {
                    Success = true,
                    Data = new { successCount, errorCount, errors = errors.Take(10).ToList() },
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

        // Role Management
        public IActionResult RoleManagement()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var roles = await _adminService.GetRolesAsync();
                return Json(new ApiResponse<List<RoleViewModel>> { Success = true, Data = roles });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<RoleViewModel>> 
                { 
                    Success = false, 
                    Message = "Error retrieving roles: " + ex.Message 
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPermissions()
        {
            try
            {
                var permissions = await _adminService.GetPermissionsAsync();
                return Json(new ApiResponse<List<PermissionViewModel>> { Success = true, Data = permissions });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<PermissionViewModel>> 
                { 
                    Success = false, 
                    Message = "Error retrieving permissions: " + ex.Message 
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole([FromBody] RoleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid data provided",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var success = await _adminService.CreateRoleAsync(model);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Role created successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Failed to create role" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error creating role: " + ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] RoleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid data provided",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var success = await _adminService.UpdateRoleAsync(id, model);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Role updated successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Role not found or update failed" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error updating role: " + ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteRole(int id)
        {
            try
            {
                var success = await _adminService.DeleteRoleAsync(id);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Role deleted successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Cannot delete role. System roles cannot be deleted or role not found." });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error deleting role: " + ex.Message });
            }
        }

        // Google Analytics
        public async Task<IActionResult> GoogleAnalytics()
        {
            var settings = await _context.GoogleAnalyticsSettings
                .Include(s => s.UpdatedByUser)
                .FirstOrDefaultAsync();

            var model = new GoogleAnalyticsViewModel
            {
                TrackingId = settings?.TrackingId ?? "",
                IsEnabled = settings?.IsEnabled ?? false,
                CustomScript = settings?.CustomScript ?? "",
                AnonymizeIp = settings?.AnonymizeIp ?? true,
                EnhancedLinkAttribution = settings?.EnhancedLinkAttribution ?? false,
                DebugMode = settings?.DebugMode ?? false,
                LastUpdated = settings?.UpdatedAt,
                UpdatedBy = settings?.UpdatedByUser?.Name ?? ""
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGoogleAnalytics([FromBody] GoogleAnalyticsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid data provided",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var settings = await _context.GoogleAnalyticsSettings.FirstOrDefaultAsync();
                var userId = GetCurrentUserId();

                if (settings == null)
                {
                    // Create new settings
                    settings = new GoogleAnalyticsSettings
                    {
                        TrackingId = model.TrackingId,
                        IsEnabled = model.IsEnabled,
                        CustomScript = model.CustomScript,
                        AnonymizeIp = model.AnonymizeIp,
                        EnhancedLinkAttribution = model.EnhancedLinkAttribution,
                        DebugMode = model.DebugMode,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = userId
                    };
                    _context.GoogleAnalyticsSettings.Add(settings);
                }
                else
                {
                    // Update existing settings
                    settings.TrackingId = model.TrackingId;
                    settings.IsEnabled = model.IsEnabled;
                    settings.CustomScript = model.CustomScript;
                    settings.AnonymizeIp = model.AnonymizeIp;
                    settings.EnhancedLinkAttribution = model.EnhancedLinkAttribution;
                    settings.DebugMode = model.DebugMode;
                    settings.UpdatedAt = DateTime.UtcNow;
                    settings.UpdatedBy = userId;
                }

                await _context.SaveChangesAsync();

                return Json(new ApiResponse<object> { Success = true, Message = "Google Analytics settings saved successfully" });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Error saving settings: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCompanies()
        {
            try
            {
                var companies = await _adminService.GetCompaniesAsync();
                return Json(new ApiResponse<List<LookupItem>> { Success = true, Data = companies });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<LookupItem>>
                {
                    Success = false,
                    Message = "Error retrieving companies: " + ex.Message
                });
            }
        }

        // Product Types
        public IActionResult ProductTypes()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProductTypes()
        {
            try
            {
                var productTypes = await _adminService.GetProductTypesAsync();
                return Json(new ApiResponse<List<ProductTypeViewModel>> { Success = true, Data = productTypes });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<ProductTypeViewModel>>
                {
                    Success = false,
                    Message = "Error retrieving product types: " + ex.Message
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProductType([FromBody] ProductTypeViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new ApiResponse<ProductTypeViewModel>
                    {
                        Success = false,
                        Message = "Invalid data provided"
                    });
                }

                var result = await _adminService.CreateProductTypeAsync(model);
                return Json(new ApiResponse<ProductTypeViewModel>
                {
                    Success = result.Success,
                    Data = result.Data,
                    Message = result.Success ? "Product type created successfully" : result.ErrorMessage ?? "Failed to create product type"
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<ProductTypeViewModel>
                {
                    Success = false,
                    Message = "Error creating product type: " + ex.Message
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProductType(int id, [FromBody] ProductTypeViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new ApiResponse<ProductTypeViewModel>
                    {
                        Success = false,
                        Message = "Invalid data provided"
                    });
                }

                model.Id = id;
                var result = await _adminService.UpdateProductTypeAsync(model);
                return Json(new ApiResponse<ProductTypeViewModel>
                {
                    Success = result.Success,
                    Message = result.Success ? "Product type updated successfully" : result.ErrorMessage ?? "Failed to update product type"
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<ProductTypeViewModel>
                {
                    Success = false,
                    Message = "Error updating product type: " + ex.Message
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProductType(int id)
        {
            try
            {
                var (success, message) = await _adminService.DeleteProductTypeAsync(id);
                return Json(new ApiResponse<object>
                {
                    Success = success,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error deleting product type: " + ex.Message
                });
            }
        }

        // Certificate Types
        public IActionResult CertificateTypes()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCertificateTypes()
        {
            try
            {
                var certificateTypes = await _adminService.GetCertificateTypesAsync();
                return Json(new ApiResponse<List<CertificateTypeViewModel>> { Success = true, Data = certificateTypes });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<CertificateTypeViewModel>>
                {
                    Success = false,
                    Message = "Error retrieving certificate types: " + ex.Message
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCertificateType([FromBody] CertificateTypeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid data provided",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var result = await _adminService.CreateCertificateTypeAsync(model);
                if (result.Success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Certificate type created successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = result.ErrorMessage ?? "Failed to create certificate type" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error creating certificate type: " + ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateCertificateType(int id, [FromBody] CertificateTypeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid data provided",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var result = await _adminService.UpdateCertificateTypeAsync(id, model);
                if (result.Success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Certificate type updated successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = result.ErrorMessage ?? "Certificate type not found or update failed" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error updating certificate type: " + ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteCertificateType(int id)
        {
            try
            {
                var success = await _adminService.DeleteCertificateTypeAsync(id);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Certificate type deleted successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Certificate type not found or delete failed" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error deleting certificate type: " + ex.Message });
            }
        }

        // Certificate Fields
        public IActionResult CertificateFields()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCertificateFields()
        {
            try
            {
                var fields = await _adminService.GetCertificateFieldsAsync();
                return Json(new ApiResponse<List<CertificateFieldViewModel>> { Success = true, Data = fields });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<CertificateFieldViewModel>>
                {
                    Success = false,
                    Message = "Error retrieving certificate fields: " + ex.Message
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCertificateField([FromBody] CertificateFieldViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid data provided",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var success = await _adminService.CreateCertificateFieldAsync(model);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Certificate field created successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Failed to create certificate field" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error creating certificate field: " + ex.Message });
            }
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCertificateField(int id, [FromBody] CertificateFieldViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid data provided",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var success = await _adminService.UpdateCertificateFieldAsync(id, model);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Certificate field updated successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Certificate field not found or update failed" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error updating certificate field: " + ex.Message });
            }
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCertificateField(int id)
        {
            try
            {
                var success = await _adminService.DeleteCertificateFieldAsync(id);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Certificate field deleted successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Certificate field not found or delete failed" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error deleting certificate field: " + ex.Message });
            }
        }

        // CSF Attributes
        public IActionResult CSFAttributes()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCSFAttributes()
        {
            try
            {
                var attributes = await _adminService.GetCSFAttributesAsync();
                return Json(new ApiResponse<List<CSFAttributeViewModel>> { Success = true, Data = attributes });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<CSFAttributeViewModel>>
                {
                    Success = false,
                    Message = "Error retrieving CSF attributes: " + ex.Message
                });
            }
        }

        // Tags
        public IActionResult Tags()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetTags()
        {
            try
            {
                var tags = await _adminService.GetTagsAsync();
                return Json(new ApiResponse<List<TagViewModel>> { Success = true, Data = tags });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<TagViewModel>>
                {
                    Success = false,
                    Message = "Error retrieving tags: " + ex.Message
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTag([FromBody] TagViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid data provided",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var success = await _adminService.CreateTagAsync(model);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Tag created successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Failed to create tag" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error creating tag: " + ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateTag(int id, [FromBody] TagViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid data provided",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var success = await _adminService.UpdateTagAsync(id, model);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Tag updated successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Tag not found or update failed" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error updating tag: " + ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteTag(int id)
        {
            try
            {
                var success = await _adminService.DeleteTagAsync(id);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Tag deleted successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Tag not found or delete failed" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error deleting tag: " + ex.Message });
            }
        }

        // Partners
        public IActionResult Partners()
        {
            ViewData["Title"] = "Partners Management";
            ViewData["BodyClass"] = "role-admin";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPartners()
        {
            try
            {
                var partners = await _adminService.GetPartnersAsync();
                return Json(new ApiResponse<List<PartnerViewModel>> { Success = true, Data = partners });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving partners");
                return Json(new ApiResponse<List<PartnerViewModel>> { Success = false, Message = "Error retrieving partners", Data = new List<PartnerViewModel>() });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePartner([FromBody] PartnerViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Invalid data provided" });
                }

                var result = await _adminService.CreatePartnerAsync(model);
                if (result)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Partner created successfully" });
                }

                return Json(new ApiResponse<object> { Success = false, Message = "Failed to create partner" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating partner");
                return Json(new ApiResponse<object> { Success = false, Message = "Error creating partner" });
            }
        }

        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePartner(int id, [FromBody] PartnerViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Invalid data provided" });
                }

                var result = await _adminService.UpdatePartnerAsync(id, model);
                if (result)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Partner updated successfully" });
                }

                return Json(new ApiResponse<object> { Success = false, Message = "Partner not found or update failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating partner {PartnerId}", id);
                return Json(new ApiResponse<object> { Success = false, Message = "Error updating partner" });
            }
        }

        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePartner(int id)
        {
            try
            {
                var result = await _adminService.DeletePartnerAsync(id);
                if (result)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Partner deleted successfully" });
                }

                return Json(new ApiResponse<object> { Success = false, Message = "Partner not found or delete failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting partner {PartnerId}", id);
                return Json(new ApiResponse<object> { Success = false, Message = "Error deleting partner" });
            }
        }

        // Product Management
        public IActionResult ProductManagement()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var products = await _adminService.GetProductsAsync();
                return Json(new ApiResponse<List<ProductViewModel>> { Success = true, Data = products });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<ProductViewModel>>
                {
                    Success = false,
                    Message = "Error retrieving products: " + ex.Message
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct([FromBody] ProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid data provided",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var success = await _adminService.CreateProductAsync(model);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Product created successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Failed to create product" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error creating product: " + ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid data provided",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var success = await _adminService.UpdateProductAsync(id, model);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Product updated successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Product not found or update failed" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error updating product: " + ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var success = await _adminService.DeleteProductAsync(id);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Product deleted successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Product not found or delete failed" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error deleting product: " + ex.Message });
            }
        }

        // GCC API Keys Management
        public IActionResult GCCAPI()
        {
            // API key management is handled via /api/v1/apikeys endpoints
            // This view provides the UI for managing API keys
            return View();
        }

        // Partner API Access Management
        public IActionResult PartnerAPI()
        {
            // Manage API keys for external partners (Amazon, Novi, etc.)
            // Partners use these keys to access GCC REST API endpoints
            ViewData["PartnerApiRateLimit"] = _rateLimitOptions.PartnerApiKeyLimit;
            return View();
        }

        // Lookup data for dropdowns
        [HttpGet]
        public async Task<IActionResult> GetRolesList()
        {
            try
            {
                var roles = await _adminService.GetRolesListAsync();
                return Json(new ApiResponse<List<LookupItem>> { Success = true, Data = roles });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<LookupItem>>
                {
                    Success = false,
                    Message = "Error retrieving roles: " + ex.Message
                });
            }
        }

        // Generic CRUD operations for simple entities
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateEntity(string entityType, [FromBody] object data)
        {
            // Generic create method - in real app, route to specific service based on entityType
            return Json(new ApiResponse<object> { Success = true, Message = $"{entityType} created successfully" });
        }

        [HttpPut]
        public IActionResult UpdateEntity(string entityType, int id, [FromBody] object data)
        {
            // Generic update method
            return Json(new ApiResponse<object> { Success = true, Message = $"{entityType} updated successfully" });
        }

        [HttpDelete]
        public IActionResult DeleteEntity(string entityType, int id)
        {
            // Generic delete method
            return Json(new ApiResponse<object> { Success = true, Message = $"{entityType} deleted successfully" });
        }

        // Export functionality
        [HttpGet]
        public async Task<IActionResult> ExportData(string entityType, string format = "csv")
        {
            try
            {
                var bytes = await _adminService.ExportDataAsync(entityType, format);
                var fileName = $"{entityType}_{DateTime.Now:yyyyMMdd}.{format}";
                
                var contentType = format.ToLower() switch
                {
                    "csv" => "text/csv",
                    "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    _ => "text/plain"
                };
                
                return File(bytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                // Return error as plain text file
                var errorContent = $"Error exporting {entityType}: {ex.Message}";
                var errorBytes = System.Text.Encoding.UTF8.GetBytes(errorContent);
                return File(errorBytes, "text/plain", $"error_{DateTime.Now:yyyyMMdd}.txt");
            }
        }

        // Legacy CreateUser view was a mockup - redirect to User Management page
        public IActionResult CreateUser()
        {
            return RedirectToAction("UserManagement");
        }

        // 2FA Management Actions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset2FA([FromBody] UserIdRequest request)
        {
            try
            {
                var success = await _adminService.Reset2FAAsync(request.UserId);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "2FA reset successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "User not found or 2FA reset failed" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error resetting 2FA: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable2FA([FromBody] UserIdRequest request)
        {
            try
            {
                var success = await _adminService.Disable2FAAsync(request.UserId);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "2FA disabled successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "User not found or 2FA disable failed" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error disabling 2FA: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enable2FA([FromBody] Enable2FARequest request)
        {
            try
            {
                var success = await _adminService.Enable2FAAsync(request.UserId, request.TwoFactorMethod ?? "TOTP");
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "2FA enabled successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "User not found or 2FA enable failed" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error enabling 2FA: " + ex.Message });
            }
        }
    }

    // Request model for user ID operations
    public class UserIdRequest
    {
        public int UserId { get; set; }
    }

    // Request model for enabling 2FA
    public class Enable2FARequest
    {
        public int UserId { get; set; }
        public string? TwoFactorMethod { get; set; }
    }
}