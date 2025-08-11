using System.Diagnostics;
using GCCWebApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace GCCWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // For public users (not authenticated), redirect to certificates page
            // This ensures the initial load always shows certificates for public access
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("CertificateTileView", "Certificate");
            }

            // For authenticated users, redirect based on their role
            var userRole = HttpContext.Session.GetString("UserRole");
            return userRole switch
            {
                "External" => RedirectToAction("Index", "Dashboard"),
                "Analyst" or "Admin" => RedirectToAction("Index", "Dashboard"),
                _ => RedirectToAction("CertificateTileView", "Certificate")
            };
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
