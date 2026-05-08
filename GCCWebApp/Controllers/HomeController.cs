using System.Diagnostics;
using GCCWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace GCCWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            // Check if user is authenticated
            if (User.Identity?.IsAuthenticated == true)
            {
                // Authenticated users - redirect based on their role
                var userRole = HttpContext.Session.GetString("UserRole");
                return userRole switch
                {
                    "External" => RedirectToAction("CertificateTileViewExternal", "Certificate"),
                    "Partner" => RedirectToAction("CertificateTileViewExternal", "Certificate"),
                    "Analyst" => RedirectToAction("CertificateTileViewInternal", "Certificate"),
                    "Admin" => RedirectToAction("CertificateTileViewInternal", "Certificate"),
                    _ => RedirectToAction("CertificateTileViewInternal", "Certificate")
                };
            }
            else
            {
                // Unauthenticated users - redirect to public certificates view
                return RedirectToAction("CertificateTileView", "Certificate");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
