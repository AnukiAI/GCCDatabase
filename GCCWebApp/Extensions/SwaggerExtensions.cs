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
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1.1",
                    Title = "GCC Web API",
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

### Rate Limits
- Anonymous: 100 requests/hour
- Authenticated: 500 requests/hour
- API Key: 1,000 requests/hour
- Partner API Key: 2,000 requests/hour
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

                // Add servers
                c.AddServer(new OpenApiServer
                {
                    Url = "https://api.greencirclecertified.com",
                    Description = "Production Server"
                });

                c.AddServer(new OpenApiServer
                {
                    Url = "https://staging-api.greencirclecertified.com",
                    Description = "Staging Server"
                });

                c.AddServer(new OpenApiServer
                {
                    Url = "http://localhost:5101",
                    Description = "Development Server"
                });

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
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GCC Web API v1");
                    c.RoutePrefix = "swagger";
                });
            }

            return app;
        }
    }
}