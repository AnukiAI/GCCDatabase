Folder Structure

/Controllers
    └── CertificateController.cs
    └── HomeController.cs
    └── [OtherFeature]Controller.cs

/Models
    └── Certificate.cs
    └── FilterOptions.cs
    └── [Other Models].cs

/Views
    └── Shared/
    └── Certificate/
        └── Management.cshtml
        └── Create.cshtml
        └── Edit.cshtml
        └── Details.cshtml
    └── Home/
        └── Index.cshtml
        └── Error.cshtml

/wwwroot
    └── css/
    └── js/
    └── images/


    -------------------------------------------------------------------------------

    f"{project_name}/Controllers/HomeController.cs": "// HomeController",
    f"{project_name}/Controllers/CertificateController.cs": "// CertificateController",
    f"{project_name}/Controllers/CompanyController.cs": "// CompanyController",
    f"{project_name}/Controllers/ProductController.cs": "// ProductController",
    f"{project_name}/Controllers/DashboardController.cs": "// DashboardController",
    f"{project_name}/Controllers/AdminController.cs": "// AdminController",

    f"{project_name}/Models/Certificate.cs": "// Certificate model",
    f"{project_name}/Models/Company.cs": "// Company model",
    f"{project_name}/Models/Product.cs": "// Product model",
    f"{project_name}/Models/User.cs": "// User model",

    f"{project_name}/Views/Home/Index.cshtml": "@* Home Index *@",
    f"{project_name}/Views/Home/Error.cshtml": "@* Error View *@",

    f"{project_name}/Views/Certificate/Index.cshtml": "@* Certificate Index *@",
    f"{project_name}/Views/Certificate/Management.cshtml": "@* Certificate Management *@",
    f"{project_name}/Views/Certificate/Create.cshtml": "@* Create Certificate *@",
    f"{project_name}/Views/Certificate/Edit.cshtml": "@* Edit Certificate *@",
    f"{project_name}/Views/Certificate/Details.cshtml": "@* Certificate Details *@",
    f"{project_name}/Views/Certificate/Import.cshtml": "@* Import Certificates *@",
    f"{project_name}/Views/Company/Index.cshtml": "@* Company Index *@",
    f"{project_name}/Views/Product/Index.cshtml": "@* Product Index *@",
    f"{project_name}/Views/Product/Management.cshtml": "@* Product Management *@",
    f"{project_name}/Views/Dashboard/Index.cshtml": "@* Dashboard View *@",
    f"{project_name}/Views/Admin/RoleManagement.cshtml": "@* Role Management *@",
    f"{project_name}/Views/Admin/UserManagement.cshtml": "@* User Management *@",
    f"{project_name}/Views/Admin/GoogleAnalytics.cshtml": "@* Google Analytics *@",
    f"{project_name}/Views/Admin/AmazonAPI.cshtml": "@* Amazon API Integration *@",
    f"{project_name}/Views/Admin/CertificateTypes.cshtml": "@* Certificate Types *@",
    f"{project_name}/Views/Admin/ProductTypes.cshtml": "@* Product Types *@",
    f"{project_name}/Views/Admin/CertificateFields.cshtml": "@* Certificate Fields *@",
    f"{project_name}/Views/Admin/CSFAttributes.cshtml": "@* CSF Attributes *@",
    f"{project_name}/Views/Admin/Tags.cshtml": "@* Tags View *@",
    f"{project_name}/Views/Admin/Partners.cshtml": "@* Partners View *@",



    f"{project_name}/Views/Shared/_Layout.cshtml": "@* Shared layout *@",
    f"{project_name}/Views/Shared/_ViewImports.cshtml": "@using GCCWebApp\n@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers",
    f"{project_name}/Views/Shared/_ViewStart.cshtml": "@{\n    Layout = \"_Layout\";\n}",


    f"{project_name}/wwwroot/css/site.css": "/* Main site CSS */",
    f"{project_name}/wwwroot/js/site.js": "// Main site JS"