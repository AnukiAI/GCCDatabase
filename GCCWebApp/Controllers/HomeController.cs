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

        /// <summary>
        /// DEF-441: the friendly "page not found" page. web.config's httpErrors has pointed 404s
        /// at /Home/NotFound since the project began, but the action never existed, so every
        /// missing page returned an empty 404 and the browser showed its own generic error.
        /// Reached two ways: re-executed by UseStatusCodePages (Program.cs) for any browser 404
        /// with no body, and rendered directly by CertificateSingleView with a specific message.
        /// Named PageNotFound because NotFound() is ControllerBase's result helper; the route
        /// stays /Home/NotFound so web.config needs no change.
        /// </summary>
        [AllowAnonymous]
        [ActionName("NotFound")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult PageNotFound()
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("NotFound");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
