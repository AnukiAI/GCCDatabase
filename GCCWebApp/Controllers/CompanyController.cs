using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GCCWebApp.Models;
using GCCWebApp.Services;

namespace GCCWebApp.Controllers
{
    [Authorize(Policy = "AnalystOrAdmin")]
    public class CompanyController : Controller
    {
        private readonly ICompanyService _companyService;

        public CompanyController(ICompanyService companyService)
        {
            _companyService = companyService;
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

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Management()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCompanies()
        {
            try
            {
                var companies = await _companyService.GetCompaniesAsync();
                return Json(new ApiResponse<List<CompanyViewModel>> { Success = true, Data = companies });
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<List<CompanyViewModel>> 
                { 
                    Success = false, 
                    Message = "Error retrieving companies: " + ex.Message 
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCompany([FromBody] CompanyViewModel model)
        {
            // Convert empty strings to null for optional fields
            if (string.IsNullOrWhiteSpace(model.Email))
                model.Email = null;
            if (string.IsNullOrWhiteSpace(model.Url))
                model.Url = null;

            // Clear and revalidate model state after normalizing empty strings
            ModelState.Clear();
            TryValidateModel(model);

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
                var userId = GetCurrentUserId();
                var username = GetCurrentUsername();

                CompanyViewModel result;
                if (model.Id > 0)
                {
                    // Update existing company
                    var success = await _companyService.UpdateCompanyAsync(model.Id, model, userId, username);
                    if (success)
                    {
                        result = await _companyService.GetCompanyByIdAsync(model.Id) ?? new CompanyViewModel();
                        return Json(new ApiResponse<CompanyViewModel> { Success = true, Data = result, Message = "Company updated successfully" });
                    }
                    else
                    {
                        return Json(new ApiResponse<object> { Success = false, Message = "Company not found or update failed" });
                    }
                }
                else
                {
                    // Create new company
                    var newId = await _companyService.CreateCompanyAsync(model, userId, username);
                    if (newId > 0)
                    {
                        result = await _companyService.GetCompanyByIdAsync(newId) ?? new CompanyViewModel();
                        return Json(new ApiResponse<CompanyViewModel> { Success = true, Data = result, Message = "Company created successfully" });
                    }
                    else
                    {
                        return Json(new ApiResponse<object> { Success = false, Message = "Failed to create company" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error saving company: " + ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteCompany(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var username = GetCurrentUsername();

                var success = await _companyService.DeleteCompanyAsync(id, userId, username);
                if (success)
                {
                    return Json(new ApiResponse<object> { Success = true, Message = "Company deleted successfully" });
                }
                else
                {
                    return Json(new ApiResponse<object> { Success = false, Message = "Company not found or delete failed" });
                }
            }
            catch (Exception ex)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Error deleting company: " + ex.Message });
            }
        }
    }
}