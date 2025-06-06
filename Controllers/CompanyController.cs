using Microsoft.AspNetCore.Mvc;

namespace GCCWebApp.Controllers
{
    public class CompanyController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}