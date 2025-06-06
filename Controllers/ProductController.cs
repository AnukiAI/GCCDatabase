using Microsoft.AspNetCore.Mvc;

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
    }
}