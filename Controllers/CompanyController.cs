using Microsoft.AspNetCore.Mvc;
using GCCWebApp.Models;

namespace GCCWebApp.Controllers
{
    public class CompanyController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Management()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetCompanies()
        {
            // Sample data - replace with actual data service
            var companies = new List<CompanyViewModel>
            {
                new CompanyViewModel 
                { 
                    Id = 1, 
                    Name = "Acme Corporation", 
                    Email = "info@acme.com", 
                    Url = "https://www.acme.com" 
                },
                new CompanyViewModel 
                { 
                    Id = 2, 
                    Name = "Globex Industries", 
                    Email = "sales@globex.com", 
                    Url = "https://www.globex.com" 
                },
                new CompanyViewModel 
                { 
                    Id = 3, 
                    Name = "Stark Enterprises", 
                    Email = "hello@stark.com", 
                    Url = "https://www.stark.com" 
                },
                new CompanyViewModel 
                { 
                    Id = 4, 
                    Name = "Wayne Technologies", 
                    Email = "contact@waynetech.com", 
                    Url = "https://www.waynetech.com" 
                }
            };

            return Json(new ApiResponse<List<CompanyViewModel>> { Success = true, Data = companies });
        }

        [HttpPost]
        public IActionResult SaveCompany([FromBody] CompanyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new ApiResponse<object> { Success = false, Message = "Invalid data provided" });
            }

            // Add logic to save company
            // For now, return success
            return Json(new ApiResponse<CompanyViewModel> { Success = true, Data = model, Message = "Company saved successfully" });
        }

        [HttpDelete]
        public IActionResult DeleteCompany(int id)
        {
            // Add logic to delete company
            // For now, return success
            return Json(new ApiResponse<object> { Success = true, Message = "Company deleted successfully" });
        }
    }

    public class CompanyViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}