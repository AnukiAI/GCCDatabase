using Microsoft.AspNetCore.Mvc;

namespace GCCWebApp.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}