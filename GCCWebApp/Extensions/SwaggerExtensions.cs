using Microsoft.OpenApi.Models;
using System.Reflection;

namespace GCCWebApp.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                // Admin/General API Documentation
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1.1",
                    Title = "GCC Web API - Admin & General",
                    Description = @"
**Green Circle Certified Web Application API**

This API provides comprehensive access to certificate management, company and product data,
and administrative functions for the GCC certification system.

### Features
- 🔐 Multiple authentication methods (API Key, JWT, Cookie)
- 📊 Comprehensive certificate and company data access
- 🚀 High-performance with caching and rate limiting
- 📈 Health monitoring and metrics
- 📋 Bulk operations for efficient data management
- 🔄 Real-time data synchronization

### Authentication Required
All API requests must be authenticated. Anonymous access is not permitted.

### Rate Limits
- Authenticated (Cookie/JWT): 500 requests/hour
- API Key: 1,000 requests/hour
- Admin: 5,000 requests/hour

### Support
- Documentation: [API Docs](https://docs.greencirclecertified.com)
- Status Page: [Status](https://status.greencirclecertified.com)
- Support: support@greencirclecertified.com",
                    TermsOfService = new Uri("https://greencirclecertified.com/terms"),
                    Contact = new OpenApiContact
                    {
                        Name = "GCC API Support Team",
                        Email = "api-support@greencirclecertified.com",
                        Url = new Uri("https://greencirclecertified.com/contact")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "GCC API License",
                        Url = new Uri("https://greencirclecertified.com/api-license")
                    }
                });

                // Partner API Management Documentation (Admin-only)
                c.SwaggerDoc("partner-management", new OpenApiInfo
                {
                    Version = "v1.0",
                    Title = "GCC Partner API Management",
                    Description = @"
**Partner API Management & Administration**

This API provides administrative endpoints for managing partner API access, API keys, and usage statistics.
Partners (Amazon, Novi, etc.) use API keys to access GCC data through dedicated endpoints.

### Partner API Features
- 🔑 Generate and manage partner API keys
- 📊 View detailed usage statistics and analytics
- ⚙️ Configure partner access permissions
- 📈 Monitor API usage and rate limits
- 🔒 Revoke and rotate API keys for security

### Authentication
All Partner API Management endpoints require **Admin role** authentication using:
- Cookie-based session (GCC.Session)
- JWT Bearer token with Admin privileges

### Rate Limits for Partners
Partner API keys have higher rate limits:
- **Partner API Key**: 2,000 requests/hour
- This applies to partners using their API keys to access GCC data endpoints

### Security Best Practices
- Store API keys securely - they cannot be retrieved after generation
- Rotate keys regularly for security
- Revoke compromised keys immediately
- Monitor usage statistics for unusual activity

### Support
- Technical Support: api-support@greencirclecertified.com
- Partner Portal: https://partners.greencirclecertified.com
- Documentation: https://docs.greencirclecertified.com/partner-api",
                    TermsOfService = new Uri("https://greencirclecertified.com/partner-terms"),
                    Contact = new OpenApiContact
                    {
                        Name = "GCC Partner API Team",
                        Email = "partner-api@greencirclecertified.com",
                        Url = new Uri("https://greencirclecertified.com/partner-support")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "GCC Partner API License",
                        Url = new Uri("https://greencirclecertified.com/partner-api-license")
                    }
                });

                // Partner Data Access API (Partner-facing, Read-only)
                c.SwaggerDoc("partner-data", new OpenApiInfo
                {
                    Version = "v1.0",
                    Title = "GCC Partner Data Access API",
                    Description = @"
**Partner Data Access API - Read-Only Certificate & Product Data**

This API provides secure, read-only access to certificate and product data for authorized external partners (Amazon, Novi, etc.).

⚠️ **IMPORTANT - Automatic Data Filtering**
All API responses are **automatically filtered** based on your user account's associations:
- **Company-based filtering**: You only see data for companies you are associated with
- **Partner-based filtering**: You only see data for your partner relationship (e.g., Amazon, Novi)
- **No access to other partners' data**: Data is strictly scoped to your authorized access level

### 🔐 Data Access Scope
Your API access is determined by your user account configuration:
1. **If associated with Companies**: See certificates/products for those specific companies only
2. **If associated with Partners**: See certificates where Partner field matches your partnership (Amazon, Novi, etc.)
3. **If associated with BOTH**: See data matching EITHER company OR partner (OR logic)
4. **If no associations**: No data access (empty results)

### 📋 Recommended Endpoints for Partners

#### Primary Endpoint - Search by Product ID
🔍 **Most commonly used by partners** - Search certificates by ASIN, UPC, GTIN, EAN, etc.
```
GET /api/v1/certificates/search/productid?productId=B08XQRT456&type=ASIN
```
**Supported types**: ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN
**Returns**: Only certificates you have access to based on your associations

#### Other Available Endpoints
- **GET /api/v1/certificates** - List all accessible certificates (filtered to your scope)
- **GET /api/v1/certificates/{id}** - Get specific certificate by number or ID (if you have access)
- **GET /api/v1/certificates/{id}/download** - Download certificate PDF (if you have access)

### 🔑 Authentication
All requests require authentication via:
- **Cookie-based session** (when logged in via web interface)
- **Partner API Key** in Authorization header:
```
Authorization: Bearer gcc_partner_[your_64_char_key]
```

### ⏱️ Rate Limits
- **Authenticated Users**: 500 requests/hour
- **Partner API Keys**: 2,000 requests/hour
- **Burst limit**: 10 requests/minute

### 📄 Pagination
All list endpoints support pagination:
- `page` - Page number (default: 1)
- `limit` - Results per page (default: 50, max: 100)

### 🔍 Additional Filtering (Optional)
You can further filter already-scoped results by:
- `partner` - Partner name (Amazon, Novi) - only shows results within your access scope
- `status` - Certificate status (Active, Expired, Draft, etc.)
- `companyId` - Company ID - only shows if you have access to that company

**Note**: Server-side filtering ensures you can NEVER access data outside your authorized scope, regardless of query parameters.

### 📊 Response Format
All responses follow this structure:
```json
{
  ""success"": true,
  ""data"": {...},
  ""message"": ""Optional message"",
  ""pagination"": {...}
}
```

### ⚠️ Error Codes
- **400** - Bad Request (invalid parameters)
- **401** - Unauthorized (not logged in or invalid API key)
- **403** - Forbidden (authenticated but no access to requested resource)
- **404** - Not Found (resource doesn't exist OR you don't have access to it)
- **429** - Rate Limit Exceeded
- **500** - Internal Server Error

### 📞 Support
- Technical Support: api-support@greencirclecertified.com
- Documentation: https://docs.greencirclecertified.com/partner-api
- Status Page: https://status.greencirclecertified.com",
                    TermsOfService = new Uri("https://greencirclecertified.com/partner-terms"),
                    Contact = new OpenApiContact
                    {
                        Name = "GCC Partner API Support",
                        Email = "api-support@greencirclecertified.com",
                        Url = new Uri("https://greencirclecertified.com/partner-support")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "GCC Partner Data API License",
                        Url = new Uri("https://greencirclecertified.com/partner-data-license")
                    }
                });

                // Document filter to separate API endpoints by audience
                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];
                    var actionName = apiDesc.ActionDescriptor.RouteValues["action"];
                    var httpMethod = apiDesc.HttpMethod;
                    var relativePath = apiDesc.RelativePath?.ToLower() ?? "";

                    if (docName == "partner-data")
                    {
                        // Partner Data API: Only specific READ operations relevant to partners
                        var isGetMethod = httpMethod?.Equals("GET", StringComparison.OrdinalIgnoreCase) == true;

                        if (!isGetMethod)
                            return false;

                        // Certificates: Only show partner-relevant endpoints
                        if (controllerName?.Equals("Certificates", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            // Allow these specific certificate endpoints:
                            // - List certificates (GET /)
                            // - Get by ID (GET {id})
                            // - Get by number (GET number/{certificateNumber})
                            // - Search by product ID (GET search/productid) - PRIMARY USE CASE
                            // - Download PDF (GET {id}/download)

                            // Block administrative endpoints like:
                            // - GET expiring (administrative)
                            // - GET search (too broad, use search/productid instead)

                            var blockedPaths = new[] {
                                "api/v1/certificates/expiring",
                                "api/v1/certificates/search" // Partners should use search/productid instead
                            };

                            return !blockedPaths.Any(blocked => relativePath.Contains(blocked));
                        }

                        // Products: Only show basic read operations
                        if (controllerName?.Equals("Products", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            // Allow:
                            // - GET / (list products - filtered to user's access)
                            // - GET {id} (get specific product)
                            return true;
                        }

                        return false;
                    }
                    else if (docName == "partner-management")
                    {
                        // Partner Management API: Admin-only PartnerApi controller
                        return controllerName?.Equals("PartnerApi", StringComparison.OrdinalIgnoreCase) == true;
                    }
                    else if (docName == "v1")
                    {
                        // Admin/General API: All endpoints except PartnerApi
                        return controllerName?.Equals("PartnerApi", StringComparison.OrdinalIgnoreCase) != true;
                    }

                    return false;
                });

                // Include XML comments
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }

                // Add operation tags
                c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] ?? "Default" });

                // Enable annotations
                c.EnableAnnotations();

                // Add servers (configure production URLs as needed)
                c.AddServer(new OpenApiServer
                {
                    Url = "http://localhost:5101",
                    Description = "Development Server"
                });

                // TODO: Add production server URL when available
                // c.AddServer(new OpenApiServer
                // {
                //     Url = "https://your-production-domain.com",
                //     Description = "Production Server"
                // });

                // Security Definitions
                c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = "X-API-Key",
                    Description = @"
**API Key Authentication**

Use your API key in the `X-API-Key` header for all requests.

**Example:**
```
X-API-Key: your-api-key-here
```

**To get an API key:**
1. Log in to the GCC portal
2. Navigate to Settings > API Keys
3. Click 'Generate New Key'
4. Copy and securely store your key

**Security Notes:**
- Keep your API key secure and never share it
- API keys can be restricted by IP address
- Keys can be revoked at any time from the portal"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = @"
**JWT Bearer Token Authentication**

Use JWT tokens for session-based authentication.

**Example:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

**To get a token:**
1. POST to `/api/v1/auth/login` with credentials
2. Extract the `accessToken` from the response
3. Use in Authorization header as shown above

**Token Details:**
- Tokens expire after 24 hours
- Refresh tokens are valid for 7 days
- Include `Bearer ` prefix (note the space)"
                });

                c.AddSecurityDefinition("Cookie", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Cookie,
                    Name = "GCC.Session",
                    Description = @"
**Cookie-based Authentication**

Automatically handled by browsers when logged in through the web interface.
Cookies are secure, HTTP-only, and have appropriate SameSite settings."
                });

                // Global security requirements (applied to all endpoints unless overridden)
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiKey"
                            }
                        },
                        Array.Empty<string>()
                    },
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Custom operation filter for response examples
                c.OperationFilter<SwaggerResponseExamplesFilter>();

                // Custom schema filter for model examples
                c.SchemaFilter<SwaggerSchemaExamplesFilter>();

                // Document all response types
                c.DocumentFilter<SwaggerResponseDocumentFilter>();
            });

            return services;
        }

        public static WebApplication UseSwaggerConfiguration(this WebApplication app, object? apiVersionDescriptionProvider = null)
        {
            if (app.Environment.IsDevelopment())
            {
                // Add role-based authorization middleware for Swagger endpoints
                app.Use(async (context, next) =>
                {
                    // Check if the request is for Swagger UI or Swagger JSON
                    if (context.Request.Path.StartsWithSegments("/swagger"))
                    {
                        var user = context.User;
                        var isAdmin = user?.HasClaim("Role", "Admin") == true;
                        var isAnalyst = user?.HasClaim("Role", "Analyst") == true;
                        var isExternal = user?.HasClaim("Role", "External") == true;
                        var isAuthenticated = user?.Identity?.IsAuthenticated == true;

                        // Block unauthenticated users entirely
                        if (!isAuthenticated)
                        {
                            context.Response.StatusCode = 403;
                            context.Response.Redirect("/Account/AccessDenied");
                            return;
                        }

                        // Check specific swagger document access
                        var path = context.Request.Path.Value?.ToLower() ?? "";

                        // Admin API (v1) - Admin and Analyst only
                        if (path.Contains("/swagger/v1/") && !isAdmin && !isAnalyst)
                        {
                            context.Response.StatusCode = 403;
                            context.Response.Redirect("/Account/AccessDenied");
                            return;
                        }

                        // Partner Management API - Admin only
                        if (path.Contains("/swagger/partner-management/") && !isAdmin)
                        {
                            context.Response.StatusCode = 403;
                            context.Response.Redirect("/Account/AccessDenied");
                            return;
                        }

                        // Partner Data API - External, Analyst, and Admin
                        if (path.Contains("/swagger/partner-data/") && !isAdmin && !isAnalyst && !isExternal)
                        {
                            context.Response.StatusCode = 403;
                            context.Response.Redirect("/Account/AccessDenied");
                            return;
                        }

                        // Main Swagger UI page - allow all authenticated users, but they'll only see docs they have access to
                        if (path == "/swagger" || path == "/swagger/" || path == "/swagger/index.html")
                        {
                            // Allow access - the DocInclusionPredicate will filter what they can see
                        }
                    }

                    await next();
                });

                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    // Admin & General API Documentation
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GCC Web API - Admin & General");

                    // Partner Data Access API (Partner-facing, Read-only)
                    c.SwaggerEndpoint("/swagger/partner-data/swagger.json", "Partner Data Access API");

                    // Partner API Management (Admin-only)
                    c.SwaggerEndpoint("/swagger/partner-management/swagger.json", "Partner API Management");

                    c.RoutePrefix = "swagger";
                    c.DocumentTitle = "GCC API Documentation";
                    c.DefaultModelsExpandDepth(2);
                    c.DefaultModelExpandDepth(2);
                    c.DisplayRequestDuration();
                    c.EnableDeepLinking();
                    c.EnableFilter();
                });
            }

            return app;
        }
    }
}