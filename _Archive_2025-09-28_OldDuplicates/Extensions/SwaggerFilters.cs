using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using Swashbuckle.AspNetCore.SwaggerGen;
using GCCWebApp.Models.Api;
using GCCWebApp.Models;
using System.Text.Json;

namespace GCCWebApp.Extensions
{
    public class SwaggerResponseExamplesFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Add common response examples for all operations
            if (operation.Responses.ContainsKey("200"))
            {
                var response = operation.Responses["200"];
                if (response.Content.ContainsKey("application/json"))
                {
                    var mediaType = response.Content["application/json"];

                    // Add example based on operation type
                    if (operation.OperationId?.Contains("GetCertificates") == true)
                    {
                        mediaType.Example = new OpenApiString(JsonSerializer.Serialize(
                            GetCertificateListExample()));
                    }
                    else if (operation.OperationId?.Contains("GetCertificate") == true)
                    {
                        mediaType.Example = new OpenApiString(JsonSerializer.Serialize(
                            GetSingleCertificateExample()));
                    }
                }
            }

            // Add error response examples
            AddErrorResponseExamples(operation);
        }

        private void AddErrorResponseExamples(OpenApiOperation operation)
        {
            var errorCodes = new[] { "400", "401", "403", "404", "429", "500" };

            foreach (var code in errorCodes)
            {
                if (!operation.Responses.ContainsKey(code))
                {
                    operation.Responses.Add(code, new OpenApiResponse
                    {
                        Description = GetErrorDescription(code),
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            ["application/json"] = new OpenApiMediaType
                            {
                                Schema = new OpenApiSchema
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.Schema,
                                        Id = nameof(StandardApiResponse<object>)
                                    }
                                },
                                Example = new OpenApiString(
                                    JsonSerializer.Serialize(GetErrorExample(code)))
                            }
                        }
                    });
                }
            }
        }

        private string GetErrorDescription(string code) => code switch
        {
            "400" => "Bad Request - Invalid input or validation errors",
            "401" => "Unauthorized - Authentication required",
            "403" => "Forbidden - Insufficient permissions",
            "404" => "Not Found - Resource does not exist",
            "429" => "Too Many Requests - Rate limit exceeded",
            "500" => "Internal Server Error - Unexpected error occurred",
            _ => "Error response"
        };

        private object GetErrorExample(string code) => code switch
        {
            "400" => StandardApiResponse<object>.ErrorResult("Validation failed", ApiErrorCodes.ValidationError),
            "401" => StandardApiResponse<object>.ErrorResult("Authentication required", ApiErrorCodes.Unauthorized),
            "403" => StandardApiResponse<object>.ErrorResult("Access forbidden", ApiErrorCodes.Forbidden),
            "404" => StandardApiResponse<object>.ErrorResult("Resource not found", ApiErrorCodes.NotFound),
            "429" => StandardApiResponse<object>.ErrorResult("Rate limit exceeded", ApiErrorCodes.RateLimitExceeded),
            "500" => StandardApiResponse<object>.ErrorResult("Internal server error", ApiErrorCodes.InternalError),
            _ => StandardApiResponse<object>.ErrorResult("Unknown error", "UNKNOWN_ERROR")
        };

        private object GetCertificateListExample()
        {
            var certificates = new[]
            {
                new Certificate
                {
                    Id = 1,
                    Number = "24-0001",
                    EffectiveDate = DateTime.Parse("2024-01-01"),
                    ExpirationDate = DateTime.Parse("2025-01-01"),
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow,
                    Product = new Product { Id = 1, Name = "Recycled Paper", Description = "100% recycled content paper" },
                    Company = new Company { Id = 1, Name = "Green Paper Co.", Email = "contact@greenpaper.com" }
                },
                new Certificate
                {
                    Id = 2,
                    Number = "24-0002",
                    EffectiveDate = DateTime.Parse("2024-02-01"),
                    ExpirationDate = DateTime.Parse("2025-02-01"),
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow.AddDays(-25),
                    UpdatedAt = DateTime.UtcNow,
                    Product = new Product { Id = 2, Name = "Sustainable Packaging", Description = "Biodegradable packaging materials" },
                    Company = new Company { Id = 2, Name = "EcoPackage Inc.", Email = "info@ecopackage.com" }
                }
            };

            return new StandardPaginatedResponse<Certificate[]>(
                certificates, 150, 1, 20, "Certificates retrieved successfully");
        }

        private object GetSingleCertificateExample()
        {
            var certificate = new Certificate
            {
                Id = 1,
                Number = "24-0001",
                EffectiveDate = DateTime.Parse("2024-01-01"),
                ExpirationDate = DateTime.Parse("2025-01-01"),
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow,
                Product = new Product { Id = 1, Name = "Recycled Paper", Description = "100% recycled content paper" },
                Company = new Company { Id = 1, Name = "Green Paper Co.", Email = "contact@greenpaper.com" },
                CertificationType = new CertificationType { Id = 1, Name = "Recycled Content", Description = "Certified recycled content materials" }
            };

            return StandardApiResponse<Certificate>.SuccessResult(certificate, "Certificate retrieved successfully");
        }
    }

    public class SwaggerSchemaExamplesFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(Certificate))
            {
                schema.Example = new OpenApiString(JsonSerializer.Serialize(new
                {
                    id = 1,
                    number = "24-0001",
                    productId = 1,
                    companyId = 1,
                    certificationTypeId = 1,
                    effectiveDate = "2024-01-01T00:00:00Z",
                    expirationDate = "2025-01-01T00:00:00Z",
                    status = "Active",
                    partner = "None",
                    certificateFile = "certificates/24-0001.pdf",
                    createdAt = "2024-08-01T10:30:00Z",
                    updatedAt = "2024-09-15T14:20:00Z"
                }));
            }
            else if (context.Type == typeof(Company))
            {
                schema.Example = new OpenApiString(JsonSerializer.Serialize(new
                {
                    id = 1,
                    name = "Green Paper Co.",
                    email = "contact@greenpaper.com",
                    phone = "+1-555-0123",
                    address = "123 Eco Street, Green City, GC 12345",
                    website = "https://greenpaper.com",
                    description = "Leading manufacturer of recycled paper products",
                    isActive = true,
                    createdAt = "2023-01-15T09:00:00Z"
                }));
            }
            else if (context.Type == typeof(Product))
            {
                schema.Example = new OpenApiString(JsonSerializer.Serialize(new
                {
                    id = 1,
                    name = "Recycled Paper",
                    description = "100% recycled content paper products",
                    category = "Paper Products",
                    isActive = true,
                    createdAt = "2023-02-01T10:00:00Z"
                }));
            }
        }
    }

    public class SwaggerResponseDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // Add global response schemas
            if (!swaggerDoc.Components.Schemas.ContainsKey("StandardApiResponse"))
            {
                swaggerDoc.Components.Schemas.Add("StandardApiResponse", new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>
                    {
                        ["success"] = new OpenApiSchema { Type = "boolean", Description = "Indicates if the request was successful" },
                        ["data"] = new OpenApiSchema { Type = "object", Description = "Response data payload" },
                        ["message"] = new OpenApiSchema { Type = "string", Description = "Optional success or informational message" },
                        ["errors"] = new OpenApiSchema
                        {
                            Type = "array",
                            Items = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "ApiError" } },
                            Description = "List of errors (empty for successful requests)"
                        },
                        ["timestamp"] = new OpenApiSchema { Type = "string", Format = "date-time", Description = "Response timestamp in UTC" },
                        ["requestId"] = new OpenApiSchema { Type = "string", Description = "Unique request identifier for tracing" },
                        ["version"] = new OpenApiSchema { Type = "string", Description = "API version used for this response" }
                    },
                    Required = new HashSet<string> { "success", "timestamp", "requestId", "version" }
                });
            }

            if (!swaggerDoc.Components.Schemas.ContainsKey("ApiError"))
            {
                swaggerDoc.Components.Schemas.Add("ApiError", new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>
                    {
                        ["code"] = new OpenApiSchema { Type = "string", Description = "Error code for programmatic handling" },
                        ["message"] = new OpenApiSchema { Type = "string", Description = "Human-readable error message" },
                        ["field"] = new OpenApiSchema { Type = "string", Description = "Field name for validation errors (optional)" },
                        ["details"] = new OpenApiSchema { Type = "object", Description = "Additional error details (development mode only)" }
                    },
                    Required = new HashSet<string> { "code", "message" }
                });
            }

            // Add operation tags for better organization
            var tags = new[]
            {
                new OpenApiTag { Name = "Certificates", Description = "Certificate management operations" },
                new OpenApiTag { Name = "Companies", Description = "Company management operations" },
                new OpenApiTag { Name = "Products", Description = "Product management operations" },
                new OpenApiTag { Name = "Authentication", Description = "Authentication and authorization" },
                new OpenApiTag { Name = "API Keys", Description = "API key management" },
                new OpenApiTag { Name = "Bulk Operations", Description = "Batch processing operations" },
                new OpenApiTag { Name = "Health", Description = "Health monitoring and status" },
                new OpenApiTag { Name = "Metrics", Description = "Analytics and reporting" }
            };

            swaggerDoc.Tags = tags.ToList();
        }
    }
}