using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
//using GreenCircleCertified.Models;
//using GreenCircleCertified.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using GCCWebApp.Models;
using System.Runtime.InteropServices;
using Microsoft.Graph;

namespace GCCWebApp.Controllers
{
    ////    What these scenarios test:
    //// Long Product Names: Superior Essex cable with 20+ product variants
    //// Long Company Names: ASSA ABLOY with full division details
    //// Multiple Locations: 5 locations across US and Canada
    //// International Addresses: Mexico location with long street address
    //// Minimal Content: Short names for layout comparison
    //    Access URLs for testing:

    ///Certificate/External/Demo/customer/longproduct - Very long product name
    ///Certificate/External/Demo/customer/longcompany - Long company name
    ///Certificate/External/Demo/customer/multilocations - Multiple locations
    ///Certificate/External/Demo/customer/minimal - Minimal content
    ///Certificate/External/Demo/customer/international - International location
    ///Certificate/Demo/ContentLengths - JSON overview of all scenarios

    ///Certificate/CertificateSingleView(shows default 24-1605)
    ///Certificate/CertificateSingleView/24-5637 (GAF insulation)
    ///Certificate/CertificateSingleView/24-5712 (Domtar paper)
    ///Certificate/CertificateSingleView/21-0476 (ASSA ABLOY door hardware)

    /// <summary>
    /// Controller for Certificate functionality
    /// Handles both basic CRUD operations and advanced certificate list functionality
    /// </summary>
    public class CertificateController : Controller
    {

        #region Basic CRUD Operations

        /// <summary>
        /// Main certificates listing page
        /// GET: /Certificate/Certificates
        /// </summary>
        public IActionResult Certificates()
        {
            return View();
        }

        /// <summary>
        /// Certificate tile view
        /// GET: /Certificate/CertificateTileView
        /// </summary>
        public IActionResult CertificateTileView()
        {
            return View();
        }

        /// <summary>
        /// Certificate single view
        /// GET: /Certificate/CertificateTileViewExternal
        /// </summary>
        public IActionResult CertificateTileViewExternal()
        {
            return View();
        }

        /// <summary>
        /// Certificate single view
        /// GET: /Certificate/CertificateTileViewExternal
        /// </summary>
        public IActionResult CertificateTileViewInternal()
        {
            return View();
        }


        /// <summary>
        /// Certificate external view
        /// GET: /Certificate/CertificateExternalView
        /// </summary>
        public IActionResult CertificateExternalView()
        {
            // Create mock certificate data for external view
            var mockCertificate = CreateMockCertificate();

            // Set default external permissions for customer
            SetMockExternalPermissions("customer");

            return View(mockCertificate);
        }

        /// <summary>
        /// Certificate internal view
        /// GET: /Certificate/CertificateInternalView
        /// </summary>
        public IActionResult CertificateInternalView()
        {
            // Create mock certificate data for internal view
            var mockCertificate = CreateMockCertificateWithInternalData();

            // Set default internal permissions for analyst
            SetMockInternalPermissions("analyst");

            return View(mockCertificate);
        }

        public IActionResult CertificateSingleView(string id = "24-1605")
        {
            // Ensure all ViewBag properties are set to avoid null reference errors
            ViewBag.CertificateId = id ?? "24-1605";
            ViewBag.CertificationType = "Certified Environmental Facts";
            ViewBag.Company = "ASSA ABLOY Access & Egress Hardware Group, Inc. - Corbin Russwin Division";
            ViewBag.ProductName = "900ET Exit Device Trim with EcoFlex®";
            ViewBag.ProductType = "Exit device";
            ViewBag.Location = "Berlin, CT";
            ViewBag.Country = "United States";
            ViewBag.EffectiveDate = "9/1/2024";
            ViewBag.ExpirationDate = "8/31/2027";
            ViewBag.CompanyContact = "";
            ViewBag.CompanyUrl = "";
            ViewBag.CompanyEmail = "";

            // Override with specific data based on ID
            if (id == "24-5637")
            {
                ViewBag.CertificationType = "Recycled Content";
                ViewBag.Company = "GAF";
                ViewBag.ProductName = "EnergyGuard™ Polyiso Roof Insulation - 1 to 1.9";
                ViewBag.ProductType = "Roof Insulation";
                ViewBag.Location = "Cedar City, UT";
                ViewBag.EffectiveDate = "11/1/2024";
                ViewBag.ExpirationDate = "10/31/2025";
            }
            else if (id == "24-5712")
            {
                ViewBag.Company = "Domtar Corporation";
                ViewBag.ProductName = "Amazon Basics Multipurpose Copy Printer Paper (92 GE Bright White)";
                ViewBag.ProductType = "Paper";
                ViewBag.Location = "Ashdown, AR";
                ViewBag.EffectiveDate = "11/25/2024";
                ViewBag.ExpirationDate = "11/24/2027";
            }
            else if (id == "24-5788")
            {
                ViewBag.CertificationType = "LCA Optimized";
                ViewBag.Company = "ASSA ABLOY Access & Egress Hardware Group, Inc. - SARGENT Manufacturing Co. Division";
                ViewBag.ProductName = "SARGENT PE80 Mechanical Exit Device";
                ViewBag.Location = "New Haven, CT";
                ViewBag.EffectiveDate = "12/23/2024";
                ViewBag.ExpirationDate = "12/22/2027";
            }
            else if (id == "21-0476")
            {
                ViewBag.CertificationType = "GreenCircle Audited";
                ViewBag.Company = "ASSA ABLOY";
                ViewBag.ProductName = "Mercury Thermal Break Frame, Fleming Adjustable Thermal Resistant Frame, and Mercury2 Adjustable Thermal Break Frame / AADG TrioE / 777E";
                ViewBag.ProductType = "Door Hardware";
                ViewBag.Location = "New Haven, CT";
                ViewBag.EffectiveDate = "5/1/2021";
                ViewBag.ExpirationDate = "4/30/2025";
            }

            return View();
        }

        #endregion

        // Add these methods to your CertificateController for mock data demonstrations

        /// <summary>
        /// Mock External View for Client Demo
        /// </summary>
        [HttpGet]
        [Route("Certificate/External/Demo/{userType?}")]
        public IActionResult ExternalViewDemo(string userType = "customer")
        {
            // Create mock certificate data
            var mockCertificate = CreateMockCertificate();

            // Set mock permissions based on user type
            SetMockExternalPermissions(userType);

            return View("CertificateExternalView", mockCertificate);
        }

        /// <summary>
        /// Mock Internal View for Client Demo
        /// </summary>
        [HttpGet]
        [Route("Certificate/Internal/Demo/{userRole?}")]
        public IActionResult InternalViewDemo(string userRole = "analyst")
        {
            // Create mock certificate data with additional internal fields
            var mockCertificate = CreateMockCertificateWithInternalData();

            // Set mock permissions based on user role
            SetMockInternalPermissions(userRole);

            return View("CertificateInternalView", mockCertificate);
        }

        /// <summary>
        /// Create mock certificate data
        /// </summary>
        private CertificateViewModel CreateMockCertificate()
        {
            return new CertificateViewModel
            {
                // Basic Certificate Information
                CertificateNumber = "24-1605",
                CertificationType = "Certified Environmental Facts",
                CompanyName = "ASSA ABLOY Access & Egress Hardware Group, Inc. - Corbin Russwin Division",
                ProductName = "900ET Exit Device Trim with EcoFlex®",
                ProductType = "Exit device",
                Status = "Active",
                EffectiveDate = new DateTime(2024, 9, 1),
                ExpirationDate = new DateTime(2027, 8, 31),
                Location = "Berlin, CT",
                Country = "United States",

                // Product Classification
                ProductCategory = "Construction / Building Products",
                ProductTypeSubcategory = "Door Hardware",
                Partner = "Amazon",

                // Contact Information
                CompanyContactEmail = "sustainability@assaabloy.com",
                CompanyContactURL = "https://www.assaabloy.com/sustainability",

                // Product Identifiers
                ASIN = "B08XYZ1234",
                UPC = "123456789012",
                GTIN = "01234567890123",
                EAN = "1234567890123",

                // CSF Attributes
                CSFAttributes = "Carbon Neutral, Recycled Content",

                // Tags
                ExternalTag = "Environmental Facts",
                InternalTag = "High Priority Client",

                // File Information
                CertificateUploadPath = "/img/certpreview.png",
                VerificationURL = "https://greencirclecertified.com/verify/24-1605",

                // Record Information
                RecordCreatedDate = new DateTime(2024, 8, 15),
                RecordLastUpdatedDate = new DateTime(2024, 12, 10),
                CreatedBy = "John Smith",
                LastEditedBy = "Sarah Johnson"
            };
        }

        /// <summary>
        /// Create mock certificate with additional internal data
        /// </summary>
        private CertificateViewModel CreateMockCertificateWithInternalData()
        {
            var certificate = CreateMockCertificate();

            // Add additional internal-only data
            certificate.InternalTag = "High Priority Client, Q4 Review";
            certificate.CreatedBy = "John Smith (Analyst)";
            certificate.LastEditedBy = "Sarah Johnson (Senior Analyst)";

            return certificate;
        }

        /// <summary>
        /// Set mock permissions for external users
        /// </summary>
        private void SetMockExternalPermissions(string userType)
        {
            switch (userType.ToLower())
            {
                case "customer":
                    // Customer permissions - limited access
                    ViewBag.ShowProductIdentifiers = false;
                    ViewBag.ShowInternalTags = false;
                    ViewBag.ShowRecordInfo = false;
                    ViewBag.CanDownload = true;
                    ViewBag.CanEdit = false;
                    ViewBag.CanDelete = false;
                    ViewBag.UserType = "Customer";
                    ViewBag.UserCompanies = new List<string> { "ASSA ABLOY Access & Egress Hardware Group, Inc." };
                    break;

                case "partner":
                    // Partner permissions - elevated access for Amazon, Wayfair, etc.
                    ViewBag.ShowProductIdentifiers = true;
                    ViewBag.ShowInternalTags = false;
                    ViewBag.ShowRecordInfo = true;
                    ViewBag.CanDownload = true;
                    ViewBag.CanEdit = false;
                    ViewBag.CanDelete = false;
                    ViewBag.UserType = "Partner";
                    ViewBag.UserCompanies = new List<string> { "Amazon CPF Program" };
                    break;

                case "limitedcustomer":
                    // Limited customer - very restricted
                    ViewBag.ShowProductIdentifiers = false;
                    ViewBag.ShowInternalTags = false;
                    ViewBag.ShowRecordInfo = false;
                    ViewBag.CanDownload = false;
                    ViewBag.CanEdit = false;
                    ViewBag.CanDelete = false;
                    ViewBag.UserType = "Customer";
                    ViewBag.UserCompanies = new List<string> { "ASSA ABLOY Access & Egress Hardware Group, Inc." };
                    break;
            }
        }

        /// <summary>
        /// Set mock permissions for internal users
        /// </summary>
        private void SetMockInternalPermissions(string userRole)
        {
            switch (userRole.ToLower())
            {
                case "analyst":
                    // Analyst permissions - full view, limited edit
                    ViewBag.ShowProductIdentifiers = true;
                    ViewBag.ShowInternalTags = true;
                    ViewBag.ShowRecordInfo = true;
                    ViewBag.CanDownload = true;
                    ViewBag.CanEdit = true;
                    ViewBag.CanDelete = false;
                    ViewBag.CanManageFields = false;
                    ViewBag.CanViewAuditLog = true;
                    ViewBag.UserType = "Internal";
                    ViewBag.UserRole = "Analyst";
                    break;

                case "admin":
                    // Admin permissions - full access
                    ViewBag.ShowProductIdentifiers = true;
                    ViewBag.ShowInternalTags = true;
                    ViewBag.ShowRecordInfo = true;
                    ViewBag.CanDownload = true;
                    ViewBag.CanEdit = true;
                    ViewBag.CanDelete = true;
                    ViewBag.CanManageFields = true;
                    ViewBag.CanViewAuditLog = true;
                    ViewBag.UserType = "Internal";
                    ViewBag.UserRole = "Admin";
                    break;

                case "readonly":
                    // Read-only internal user
                    ViewBag.ShowProductIdentifiers = true;
                    ViewBag.ShowInternalTags = true;
                    ViewBag.ShowRecordInfo = true;
                    ViewBag.CanDownload = true;
                    ViewBag.CanEdit = false;
                    ViewBag.CanDelete = false;
                    ViewBag.CanManageFields = false;
                    ViewBag.CanViewAuditLog = false;
                    ViewBag.UserType = "Internal";
                    ViewBag.UserRole = "ReadOnly";
                    break;
            }
        }

        /// <summary>
        /// Demo data for multiple certificate scenarios
        /// </summary>
        [HttpGet]
        [Route("Certificate/Demo/Scenarios")]
        public IActionResult DemoScenarios()
        {
            var scenarios = new
            {
                ExternalCustomer = new
                {
                    Url = "/Certificate/External/Demo/customer",
                    Description = "Standard external customer view - limited access to product identifiers and internal data",
                    UserType = "Customer",
                    Company = "ASSA ABLOY",
                    Permissions = new[] { "View Certificate", "Download PDF" }
                },
                ExternalPartner = new
                {
                    Url = "/Certificate/External/Demo/partner",
                    Description = "Partner view (Amazon, Wayfair) - elevated access including product identifiers",
                    UserType = "Partner",
                    Company = "Amazon CPF Program",
                    Permissions = new[] { "View Certificate", "Download PDF", "View Product IDs", "View Record Info" }
                },
                ExternalLimited = new
                {
                    Url = "/Certificate/External/Demo/limitedcustomer",
                    Description = "Limited customer view - very restricted access, no downloads",
                    UserType = "Limited Customer",
                    Company = "ASSA ABLOY",
                    Permissions = new[] { "View Certificate Only" }
                },
                InternalAnalyst = new
                {
                    Url = "/Certificate/Internal/Demo/analyst",
                    Description = "Internal analyst view - full data access with edit capabilities",
                    UserType = "Internal",
                    Role = "Analyst",
                    Permissions = new[] { "View All Data", "Edit Certificate", "Download", "View Audit Log" }
                },
                InternalAdmin = new
                {
                    Url = "/Certificate/Internal/Demo/admin",
                    Description = "Internal admin view - complete access including delete and field management",
                    UserType = "Internal",
                    Role = "Admin",
                    Permissions = new[] { "Full Access", "Delete Certificate", "Manage Fields", "View Audit Log" }
                },
                InternalReadOnly = new
                {
                    Url = "/Certificate/Internal/Demo/readonly",
                    Description = "Internal read-only view - full visibility but no edit permissions",
                    UserType = "Internal",
                    Role = "ReadOnly",
                    Permissions = new[] { "View All Data", "Download Only" }
                }
            };

            return Json(scenarios);
        }

        /// <summary>
        /// Additional mock certificates for variety in demos
        /// </summary>
        [HttpGet]
        [Route("Certificate/External/Demo/{userType}/{certType}")]
        public IActionResult ExternalViewDemoVariations(string userType, string certType)
        {
            var mockCertificate = certType switch
            {
                "sustainability" => CreateMockSustainabilityCertificate(),
                "recycled" => CreateMockRecycledContentCertificate(),
                "energy" => CreateMockEnergySavingsCertificate(),
                "longproduct" => CreateMockLongProductNameCertificate(),
                "longcompany" => CreateMockLongCompanyNameCertificate(),
                "multilocations" => CreateMockMultipleLocationsCertificate(),
                "minimal" => CreateMockMinimalContentCertificate(),
                "international" => CreateMockInternationalCertificate(),
                _ => CreateMockCertificate()
            };

            SetMockExternalPermissions(userType);
            return View("CertificateExternalView", mockCertificate);
        }

        private CertificateViewModel CreateMockSustainabilityCertificate()
        {
            return new CertificateViewModel
            {
                CertificateNumber = "24-2108",
                CertificationType = "Certified Sustainability Facts",
                CompanyName = "Interface Inc.",
                ProductName = "Carbon Negative Carpet Tile Collection",
                ProductType = "Flooring",
                Status = "Active",
                EffectiveDate = new DateTime(2024, 10, 1),
                ExpirationDate = new DateTime(2027, 9, 30),
                Location = "Dalton, GA",
                Country = "United States",
                ProductCategory = "Construction / Building Products",
                ProductTypeSubcategory = "Flooring Materials",
                Partner = "Wayfair",
                CompanyContactEmail = "sustainability@interface.com",
                CompanyContactURL = "https://www.interface.com/sustainability",
                ASIN = "B09ABC5678",
                UPC = "987654321098",
                GTIN = "09876543210987",
                EAN = "9876543210987",
                CSFAttributes = "Carbon Negative, Biobased Content, Recycled Content",
                ExternalTag = "Sustainability Facts",
                InternalTag = "Carbon Negative Leader",
                CertificateUploadPath = "/img/certpreview.png",
                VerificationURL = "https://greencirclecertified.com/verify/24-2108",
                RecordCreatedDate = new DateTime(2024, 9, 20),
                RecordLastUpdatedDate = new DateTime(2024, 12, 5),
                CreatedBy = "Maria Rodriguez",
                LastEditedBy = "David Chen"
            };
        }

        private CertificateViewModel CreateMockRecycledContentCertificate()
        {
            return new CertificateViewModel
            {
                CertificateNumber = "24-1847",
                CertificationType = "Certified Recycled Content",
                CompanyName = "Mohawk Industries",
                ProductName = "EverStrand Recycled Carpet",
                ProductType = "Carpet",
                Status = "Active",
                EffectiveDate = new DateTime(2024, 8, 15),
                ExpirationDate = new DateTime(2027, 8, 14),
                Location = "Calhoun, GA",
                Country = "United States",
                ProductCategory = "Construction / Building Products",
                ProductTypeSubcategory = "Flooring Materials",
                Partner = "",
                CompanyContactEmail = "green@mohawk.com",
                CompanyContactURL = "https://www.mohawk.com/sustainability",
                ASIN = "B07DEF9012",
                UPC = "456789012345",
                GTIN = "04567890123456",
                EAN = "4567890123456",
                CSFAttributes = "Recycled Content",
                ExternalTag = "Recycled Content",
                InternalTag = "Standard Cert",
                CertificateUploadPath = "/img/certpreview.png",
                VerificationURL = "https://greencirclecertified.com/verify/24-1847",
                RecordCreatedDate = new DateTime(2024, 8, 1),
                RecordLastUpdatedDate = new DateTime(2024, 11, 28),
                CreatedBy = "Jennifer Park",
                LastEditedBy = "Michael Torres"
            };
        }

        private CertificateViewModel CreateMockEnergySavingsCertificate()
        {
            return new CertificateViewModel
            {
                CertificateNumber = "24-1923",
                CertificationType = "Certified Energy Savings",
                CompanyName = "Philips Lighting",
                ProductName = "LED Office Lighting System",
                ProductType = "Lighting",
                Status = "Active",
                EffectiveDate = new DateTime(2024, 11, 1),
                ExpirationDate = new DateTime(2027, 10, 31),
                Location = "Somerset, NJ",
                Country = "United States",
                ProductCategory = "Electronics / Electrical",
                ProductTypeSubcategory = "Lighting Systems",
                Partner = "Amazon",
                CompanyContactEmail = "sustainability@philips.com",
                CompanyContactURL = "https://www.philips.com/sustainability",
                ASIN = "B08GHI3456",
                UPC = "789012345678",
                GTIN = "07890123456789",
                EAN = "7890123456789",
                CSFAttributes = "Energy Savings, Certified Environmental Facts",
                ExternalTag = "Energy Savings",
                InternalTag = "High Impact Product",
                CertificateUploadPath = "/img/certpreview.png",
                VerificationURL = "https://greencirclecertified.com/verify/24-1923",
                RecordCreatedDate = new DateTime(2024, 10, 15),
                RecordLastUpdatedDate = new DateTime(2024, 12, 12),
                CreatedBy = "Lisa Wang",
                LastEditedBy = "Robert Kim"
            };
        }

        /// <summary>
        /// Mock certificate with very long product name - Superior Essex cable
        /// </summary>
        private CertificateViewModel CreateMockLongProductNameCertificate()
        {
            return new CertificateViewModel
            {
                CertificateNumber = "24-5633",
                CertificationType = "Certified Environmental Facts",
                CompanyName = "Superior Essex®",
                ProductName = "10Gain® XP Category 6A - CMR, 10Gain® XP Category 6A - CMP, 10Gain® Category 6A - CMR, 10Gain® Category 6A - CMP, Category 6A ScTP (F/UTP) - CMR, Category 6A ScTP (F/UTP) - CMP, Category 6+ ScTP (F/UTP) - CMR, Category 6+ ScTP (F/UTP) - CMP, NextGain® Category 6eX - CMR, NextGain® Category 6eX - CMP, DataGain® Category 6+ - CMR, DataGain® Category 6+ - CMP, Category 6 - CMR, Category 6 - CMP, Category 6 with FEP Jacket - CMP, Category 6+ LSHF - CMR, PowerWise™ 1G 4PPoE - CMR, PowerWise™ 1G 4PPoE - CMP, and PowerWise™ 1G 4PPoE Indoor/Outdoor - CMR/CMX",
                ProductType = "Copper cable",
                Status = "Active",
                EffectiveDate = new DateTime(2024, 10, 1),
                ExpirationDate = new DateTime(2027, 9, 30),
                Location = "Hoisington, KS",
                Country = "United States",
                ProductCategory = "Electronics / Electrical",
                ProductTypeSubcategory = "Network Cables",
                Partner = "",
                CompanyContactEmail = "sustainability@superioressex.com",
                CompanyContactURL = "https://www.superioressex.com/sustainability",
                ASIN = "B09XYZ7890",
                UPC = "234567890123",
                GTIN = "02345678901234",
                EAN = "2345678901234",
                CSFAttributes = "Certified Environmental Facts, Recycled Content",
                ExternalTag = "Environmental Facts",
                InternalTag = "Long Product Name",
                CertificateUploadPath = "/img/certpreview.png",
                VerificationURL = "https://greencirclecertified.com/verify/24-5633",
                RecordCreatedDate = new DateTime(2024, 9, 15),
                RecordLastUpdatedDate = new DateTime(2024, 12, 8),
                CreatedBy = "Technical Team",
                LastEditedBy = "Quality Reviewer"
            };
        }

        /// <summary>
        /// Mock certificate with very long company name and multiple locations
        /// </summary>
        private CertificateViewModel CreateMockLongCompanyNameCertificate()
        {
            return new CertificateViewModel
            {
                CertificateNumber = "24-1623",
                CertificationType = "Certified Environmental Facts",
                CompanyName = "ASSA ABLOY Access & Egress Hardware Group, Inc. - SARGENT Manufacturing Co. Division",
                ProductName = "Sargent EcoFlex® Electrified Mortise Lock (Fail Safe & Fail Secure)",
                ProductType = "Lock",
                Status = "Active",
                EffectiveDate = new DateTime(2024, 9, 1),
                ExpirationDate = new DateTime(2027, 8, 31),
                Location = "New Haven, CT",
                Country = "United States",
                ProductCategory = "Construction / Building Products",
                ProductTypeSubcategory = "Door Hardware",
                Partner = "Amazon",
                CompanyContactEmail = "sustainability@assaabloy.com",
                CompanyContactURL = "https://www.assaabloy.com/sustainability",
                ASIN = "B08DEF4567",
                UPC = "345678901234",
                GTIN = "03456789012345",
                EAN = "3456789012345",
                CSFAttributes = "Certified Environmental Facts, Energy Efficient",
                ExternalTag = "Environmental Facts",
                InternalTag = "High Priority Client, Long Company Name",
                CertificateUploadPath = "/img/certpreview.png",
                VerificationURL = "https://greencirclecertified.com/verify/24-1623",
                RecordCreatedDate = new DateTime(2024, 8, 20),
                RecordLastUpdatedDate = new DateTime(2024, 12, 1),
                CreatedBy = "Sarah Wilson",
                LastEditedBy = "Michael Chen"
            };
        }

        /// <summary>
        /// Mock certificate with multiple long locations (international)
        /// </summary>
        private CertificateViewModel CreateMockMultipleLocationsCertificate()
        {
            return new CertificateViewModel
            {
                CertificateNumber = "24-5801",
                CertificationType = "Recycled Content",
                CompanyName = "CertainTeed Insulation Group",
                ProductName = "Batt and Roll",
                ProductType = "Insulation",
                Status = "Active",
                EffectiveDate = new DateTime(2025, 1, 1),
                ExpirationDate = new DateTime(2025, 12, 31),
                Location = "Athens, GA; Chowchilla, CA; Kansas City, KS; Ottawa, Ontario, Canada; Redcliff, Alberta, Canada",
                Country = "United States",
                ProductCategory = "Construction / Building Products",
                ProductTypeSubcategory = "Insulation Materials",
                Partner = "Wayfair",
                CompanyContactEmail = "green@certainteed.com",
                CompanyContactURL = "https://www.certainteed.com/sustainability",
                ASIN = "B07GHI5678",
                UPC = "456789012345",
                GTIN = "04567890123456",
                EAN = "4567890123456",
                CSFAttributes = "Recycled Content, Sustainable Manufacturing",
                ExternalTag = "Recycled Content",
                InternalTag = "Multi-Location, International",
                CertificateUploadPath = "/img/certpreview.png",
                VerificationURL = "https://greencirclecertified.com/verify/24-5801",
                RecordCreatedDate = new DateTime(2024, 12, 15),
                RecordLastUpdatedDate = new DateTime(2024, 12, 28),
                CreatedBy = "Global Team",
                LastEditedBy = "Location Coordinator"
            };
        }

        /// <summary>
        /// Mock certificate with short/minimal content for comparison
        /// </summary>
        private CertificateViewModel CreateMockMinimalContentCertificate()
        {
            return new CertificateViewModel
            {
                CertificateNumber = "23-0525",
                CertificationType = "Material Ingredient Reporting",
                CompanyName = "MAPEI",
                ProductName = "Ultralite Mortar",
                ProductType = "Lightweight Polymer Modified Mortars",
                Status = "Active",
                EffectiveDate = new DateTime(2023, 8, 1),
                ExpirationDate = new DateTime(2025, 7, 31),
                Location = "Deerfield Beach, FL",
                Country = "United States",
                ProductCategory = "Construction / Building Products",
                ProductTypeSubcategory = "Mortars",
                Partner = "",
                CompanyContactEmail = "info@mapei.com",
                CompanyContactURL = "https://www.mapei.com",
                ASIN = "B06JKL8901",
                UPC = "567890123456",
                GTIN = "05678901234567",
                EAN = "5678901234567",
                CSFAttributes = "Material Ingredient Reporting",
                ExternalTag = "Ingredient Reporting",
                InternalTag = "Standard Cert",
                CertificateUploadPath = "/img/certpreview.png",
                VerificationURL = "https://greencirclecertified.com/verify/23-0525",
                RecordCreatedDate = new DateTime(2023, 7, 15),
                RecordLastUpdatedDate = new DateTime(2024, 6, 10),
                CreatedBy = "Alex Rodriguez",
                LastEditedBy = "Quality Team"
            };
        }

        /// <summary>
        /// Mock certificate with international location (Mexico)
        /// </summary>
        private CertificateViewModel CreateMockInternationalCertificate()
        {
            return new CertificateViewModel
            {
                CertificateNumber = "24-5770",
                CertificationType = "Recycled Content",
                CompanyName = "Alcamare International Recycling Group",
                ProductName = "HDPE 99 MC -Post-Consumer HDPE Mixed Color Pellets",
                ProductType = "Plastic Pellets",
                Status = "Active",
                EffectiveDate = new DateTime(2024, 12, 6),
                ExpirationDate = new DateTime(2025, 12, 5),
                Location = "Libramiento Empalme Escobedo 110-C, Comonfort, Guanajuato, Mexico",
                Country = "Mexico",
                ProductCategory = "Materials / Plastics",
                ProductTypeSubcategory = "Recycled Plastic Pellets",
                Partner = "",
                CompanyContactEmail = "sustainability@alcamare.com",
                CompanyContactURL = "https://www.alcamare.com/sustainability",
                ASIN = "B08MNO9012",
                UPC = "678901234567",
                GTIN = "06789012345678",
                EAN = "6789012345678",
                CSFAttributes = "Recycled Content, Post-Consumer Material",
                ExternalTag = "Recycled Content",
                InternalTag = "International, Mexico",
                CertificateUploadPath = "/img/certpreview.png",
                VerificationURL = "https://greencirclecertified.com/verify/24-5770",
                RecordCreatedDate = new DateTime(2024, 11, 20),
                RecordLastUpdatedDate = new DateTime(2024, 12, 15),
                CreatedBy = "International Team",
                LastEditedBy = "Regional Coordinator"
            };
        }

        /// <summary>
        /// Demo data for content length testing scenarios
        /// </summary>
        [HttpGet]
        [Route("Certificate/Demo/ContentLengths")]
        public IActionResult ContentLengthScenarios()
        {
            var scenarios = new
            {
                LongProductName = new
                {
                    Url = "/Certificate/External/Demo/customer/longproduct",
                    Description = "Very long product name with multiple cable specifications - tests text wrapping and layout",
                    ProductName = "10Gain® XP Category 6A - CMR, 10Gain® XP Category 6A - CMP... (20+ product variants)",
                    CertNumber = "24-5633",
                    TestCase = "Product name text overflow and wrapping"
                },
                LongCompanyName = new
                {
                    Url = "/Certificate/External/Demo/customer/longcompany",
                    Description = "Long company name with division details - tests company field layout",
                    CompanyName = "ASSA ABLOY Access & Egress Hardware Group, Inc. - SARGENT Manufacturing Co. Division",
                    CertNumber = "24-1623",
                    TestCase = "Company name field width and truncation"
                },
                MultipleLocations = new
                {
                    Url = "/Certificate/External/Demo/customer/multilocations",
                    Description = "Multiple locations including international - tests location field capacity",
                    Locations = "Athens, GA; Chowchilla, CA; Kansas City, KS; Ottawa, Ontario, Canada; Redcliff, Alberta, Canada",
                    CertNumber = "24-5801",
                    TestCase = "Location field with multiple entries"
                },
                MinimalContent = new
                {
                    Url = "/Certificate/External/Demo/customer/minimal",
                    Description = "Short content for comparison - tests minimal data display",
                    ProductName = "Ultralite Mortar",
                    CompanyName = "MAPEI",
                    CertNumber = "23-0525",
                    TestCase = "Minimal content layout and spacing"
                },
                InternationalLocation = new
                {
                    Url = "/Certificate/External/Demo/customer/international",
                    Description = "International location (Mexico) with long address - tests international formatting",
                    Location = "Libramiento Empalme Escobedo 110-C, Comonfort, Guanajuato, Mexico",
                    CertNumber = "24-5770",
                    TestCase = "International address formatting"
                }
            };

            return Json(scenarios);
        }
    }
}
