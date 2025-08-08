using Microsoft.AspNetCore.Mvc;
using GCCWebApp.Models;

namespace GCCWebApp.Controllers
{
    public class ProductController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Management()
        {
            return View();
        }

        public IActionResult Categories()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetCategories()
        {
            // Sample data - replace with actual data service
            var categories = new List<ProductCategoryViewModel>
            {
                new ProductCategoryViewModel 
                { 
                    Id = 1, 
                    Name = "Building Materials", 
                    Level = 1, 
                    ParentCategory = "", 
                    ProductCount = 45, 
                    Status = "Active" 
                },
                new ProductCategoryViewModel 
                { 
                    Id = 2, 
                    Name = "Lumber", 
                    Level = 2, 
                    ParentCategory = "Building Materials", 
                    ProductCount = 25, 
                    Status = "Active" 
                },
                new ProductCategoryViewModel 
                { 
                    Id = 3, 
                    Name = "Hardwood", 
                    Level = 3, 
                    ParentCategory = "Lumber", 
                    ProductCount = 15, 
                    Status = "Active" 
                },
                new ProductCategoryViewModel 
                { 
                    Id = 4, 
                    Name = "Softwood", 
                    Level = 3, 
                    ParentCategory = "Lumber", 
                    ProductCount = 10, 
                    Status = "Active" 
                },
                new ProductCategoryViewModel 
                { 
                    Id = 5, 
                    Name = "Flooring", 
                    Level = 2, 
                    ParentCategory = "Building Materials", 
                    ProductCount = 20, 
                    Status = "Active" 
                },
                new ProductCategoryViewModel 
                { 
                    Id = 6, 
                    Name = "Furniture", 
                    Level = 1, 
                    ParentCategory = "", 
                    ProductCount = 32, 
                    Status = "Active" 
                },
                new ProductCategoryViewModel 
                { 
                    Id = 7, 
                    Name = "Office Furniture", 
                    Level = 2, 
                    ParentCategory = "Furniture", 
                    ProductCount = 18, 
                    Status = "Active" 
                },
                new ProductCategoryViewModel 
                { 
                    Id = 8, 
                    Name = "Home Furniture", 
                    Level = 2, 
                    ParentCategory = "Furniture", 
                    ProductCount = 14, 
                    Status = "Active" 
                }
            };

            return Json(new ApiResponse<List<ProductCategoryViewModel>> { Success = true, Data = categories });
        }

        [HttpPost]
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
}