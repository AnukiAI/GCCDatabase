using Microsoft.AspNetCore.Mvc;
using GCCWebApp.Services;

namespace GCCWebApp.Controllers
{
    public class TestController : Controller
    {
        private readonly IAdminService _adminService;

        public TestController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        public IActionResult ProductTypes()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProductTypesTest()
        {
            try
            {
                var productTypes = await _adminService.GetProductTypesAsync();
                return Json(new { success = true, data = productTypes, count = productTypes.Count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}