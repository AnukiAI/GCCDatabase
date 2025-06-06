using Microsoft.AspNetCore.Mvc;

namespace GCCWebApp.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult RoleManagement()
        {
            return View();
        }

        public IActionResult UserManagement()
        {
            return View();
        }

        public IActionResult GoogleAnalytics()
        {
            return View();
        }

        public IActionResult AmazonAPI()
        {
            return View();
        }

        public IActionResult CertificateTypes()
        {
            return View();
        }

        public IActionResult ProductTypes()
        {
            return View();
        }

        public IActionResult CertificateFields()
        {
            return View();
        }

        public IActionResult CSFAttributes()
        {
            return View();
        }

        public IActionResult Tags()
        {
            return View();
        }

        public IActionResult Partners()
        {
            return View();
        }
    }
}