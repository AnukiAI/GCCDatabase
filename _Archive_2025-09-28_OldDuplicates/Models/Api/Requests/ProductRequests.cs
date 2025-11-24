using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GCCWebApp.Models.Api.Requests
{
    public class CreateProductRequest
    {
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, ErrorMessage = "Product name must not exceed 200 characters")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description must not exceed 1000 characters")]
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [StringLength(100, ErrorMessage = "Model must not exceed 100 characters")]
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [StringLength(50, ErrorMessage = "SKU must not exceed 50 characters")]
        [JsonPropertyName("sku")]
        public string? Sku { get; set; }

        [StringLength(50, ErrorMessage = "UPC must not exceed 50 characters")]
        [JsonPropertyName("upc")]
        public string? Upc { get; set; }

        [StringLength(50, ErrorMessage = "ASIN must not exceed 50 characters")]
        [JsonPropertyName("asin")]
        public string? Asin { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Category ID must be a positive number")]
        [JsonPropertyName("categoryId")]
        public int? CategoryId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Product Type ID must be a positive number")]
        [JsonPropertyName("productTypeId")]
        public int? ProductTypeId { get; set; }

        [Required(ErrorMessage = "Company ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Company ID must be a positive number")]
        [JsonPropertyName("companyId")]
        public int CompanyId { get; set; }

        [JsonPropertyName("identifiers")]
        public List<ProductIdentifierRequest> Identifiers { get; set; } = new();
    }

    public class UpdateProductRequest : CreateProductRequest
    {
        [Required(ErrorMessage = "Product ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Product ID must be a positive number")]
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    public class ProductSearchRequest : PaginationParams
    {
        [JsonPropertyName("term")]
        public new string? SearchTerm { get; set; }

        [JsonPropertyName("categoryId")]
        public int? CategoryId { get; set; }

        [JsonPropertyName("productTypeId")]
        public int? ProductTypeId { get; set; }

        [JsonPropertyName("companyId")]
        public int? CompanyId { get; set; }

        [JsonPropertyName("includeInactive")]
        public bool IncludeInactive { get; set; } = false;
    }

    public class ProductIdentifierRequest
    {
        [Required(ErrorMessage = "Identifier type is required")]
        [StringLength(50, ErrorMessage = "Identifier type must not exceed 50 characters")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "Identifier value is required")]
        [StringLength(100, ErrorMessage = "Identifier value must not exceed 100 characters")]
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }
}