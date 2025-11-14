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

                // Partner API Documentation (Separate Document)
                c.SwaggerDoc("partner", new OpenApiInfo
                {
                    Version = "v1.0",
                    Title = "GCC Partner API",
                    Description = @"
**Partner API Management & Administration**

This API provides administrative endpoints for managing partner API access, API keys, and usage statistics.
Partners (Amazon, Wayfair, etc.) use API keys to access GCC data through dedicated endpoints.

### Partner API Features
- 🔑 Generate and manage partner API keys
- 📊 View detailed usage statistics and analytics
- ⚙️ Configure partner access permissions
- 📈 Monitor API usage and rate limits
- 🔒 Revoke and rotate API keys for security

### Authentication
All Partner API endpoints require **Admin role** authentication using:
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

                // Document filter to separate Partner API endpoints
                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"];

                    if (docName == "partner")
                    {
                        // Partner document only includes PartnerApi controller
                        return controllerName?.Equals("PartnerApi", StringComparison.OrdinalIgnoreCase) == true;
                    }
                    else if (docName == "v1")
                    {
                        // Admin/General document excludes PartnerApi controller
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
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    // Admin & General API Documentation
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GCC Web API - Admin & General");

                    // Partner API Documentation (Separate)
                    c.SwaggerEndpoint("/swagger/partner/swagger.json", "GCC Partner API");

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