using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Controllers.Api;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Models.Api;
using Asp.Versioning;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Controllers.Api.v1
{
    /// <summary>
    /// Product management API endpoints
    /// Provides read operations for products with comprehensive details and filtering
    /// </summary>
    /// <remarks>
    /// This controller manages product-related operations including:
    /// - Retrieving products with pagination and filtering
    /// - Getting detailed product information including identifiers (ASIN, UPC, etc.)
    /// - Searching products by various criteria
    /// - Viewing associated certificates
    ///
    /// All endpoints require authentication via API Key or JWT token.
    /// Partners receive filtered product data based on their associations.
    /// </remarks>
    [ApiVersion("1.0")]
    [Authorize(AuthenticationSchemes = "ApiKey")]
    [SwaggerTag("Products")]
    public class ProductsController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Retrieve products with pagination and filtering
        /// </summary>
        /// <remarks>
        /// Returns a paginated list of products with optional search filtering.
        ///
        /// **Features:**
        /// - Pagination support (configurable page size)
        /// - Search across product name, description, model, and SKU
        /// - Sorting by multiple fields
        /// - Includes product identifiers (ASIN, UPC, etc.)
        /// - Shows associated certificates
        ///
        /// **Search Examples:**
        /// - `searchTerm=Bamboo` - Find products containing "Bamboo"
        /// - `searchTerm=B08` - Find products with ASIN starting with "B08"
        /// - `searchTerm=012345` - Find products with UPC containing "012345"
        ///
        /// **Sorting Options:**
        /// - `sortBy=name` - Sort by product name (default)
        /// - `sortBy=model` - Sort by product model
        /// - `sortBy=createdat` - Sort by creation date
        ///
        /// Add `sortDescending=true` for descending order.
        ///
        /// **Response includes:**
        /// - Product details (name, description, model, SKU)
        /// - Company information
        /// - Product type and category
        /// - Product identifiers (ASIN, UPC, GTIN, EAN, etc.)
        /// - Associated certificates
        /// </remarks>
        /// <param name="parameters">Pagination and filtering parameters</param>
        /// <returns>Paginated list of products matching the search criteria</returns>
        /// <response code="200">Returns the paginated product list</response>
        /// <response code="400">Invalid pagination parameters</response>
        /// <response code="401">Authentication required</response>
        /// <response code="429">Rate limit exceeded</response>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Get products with pagination",
            Description = "Retrieve a paginated list of products with optional search and sorting",
            OperationId = "GetProducts",
            Tags = new[] { "Products", "Partner API" }
        )]
        [ProducesResponseType(typeof(StandardPaginatedResponse<IEnumerable<Product>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetProducts([FromQuery] PaginationParams parameters)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductType)
                    .Include(p => p.Company)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(parameters.SearchTerm))
                {
                    query = query.Where(p => 
                        (p.Name != null && p.Name.Contains(parameters.SearchTerm)) ||
                        (p.Description != null && p.Description.Contains(parameters.SearchTerm)) ||
                        (p.Model != null && p.Model.Contains(parameters.SearchTerm)) ||
                        (p.Sku != null && p.Sku.Contains(parameters.SearchTerm)));
                }

                var totalCount = await query.CountAsync();

                if (!string.IsNullOrEmpty(parameters.SortBy))
                {
                    query = parameters.SortBy.ToLower() switch
                    {
                        "name" => parameters.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                        "model" => parameters.SortDescending ? query.OrderByDescending(p => p.Model) : query.OrderBy(p => p.Model),
                        "createdat" => parameters.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
                        _ => query.OrderBy(p => p.Name)
                    };
                }
                else
                {
                    query = query.OrderBy(p => p.Name);
                }

                var products = await query
                    .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .ToListAsync();

                return PaginatedResponse(products, totalCount, parameters.PageNumber, parameters.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return HandleError(ex, "Failed to retrieve products");
            }
        }

        /// <summary>
        /// Get detailed product information by ID
        /// </summary>
        /// <remarks>
        /// Returns comprehensive details for a specific product including all identifiers and certificates.
        ///
        /// **Response includes:**
        /// - Product basic information (name, description, model, SKU)
        /// - Company details
        /// - Product type and category
        /// - All product identifiers:
        ///   - ASIN (Amazon Standard Identification Number)
        ///   - UPC (Universal Product Code)
        ///   - GTIN (Global Trade Item Number)
        ///   - EAN (European Article Number)
        ///   - ISBN, SKU, MPN
        /// - Associated active certificates
        /// - Metadata (creation/update timestamps)
        ///
        /// **Use Cases:**
        /// - Getting complete product details for display
        /// - Verifying product certifications
        /// - Retrieving all product identifiers for cross-platform mapping
        /// </remarks>
        /// <param name="id">Product database ID</param>
        /// <returns>Detailed product information</returns>
        /// <response code="200">Returns the product details</response>
        /// <response code="400">Invalid product ID</response>
        /// <response code="401">Authentication required</response>
        /// <response code="404">Product not found</response>
        [HttpGet("{id}")]
        [SwaggerOperation(
            Summary = "Get product by ID",
            Description = "Retrieve detailed product information including all identifiers and certificates",
            OperationId = "GetProduct",
            Tags = new[] { "Products", "Partner API" }
        )]
        [ProducesResponseType(typeof(StandardApiResponse<Product>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProduct([FromRoute][Range(1, int.MaxValue, ErrorMessage = "Product ID must be greater than 0")] int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductType)
                    .Include(p => p.Company)
                    .Include(p => p.ProductIdentifiers)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                    return NotFoundResponse($"Product with ID {id} not found");

                return SuccessResponse(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product {ProductId}", id);
                return HandleError(ex, $"Failed to retrieve product {id}");
            }
        }

        /// <summary>
        /// Create a new product
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(StandardApiResponse<Product>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateProduct([FromBody] Product product)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                product.CreatedAt = DateTime.UtcNow;
                product.UpdatedAt = DateTime.UtcNow;

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                var createdProduct = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductType)
                    .Include(p => p.Company)
                    .FirstOrDefaultAsync(p => p.Id == product.Id);

                return CreatedResponse(createdProduct, $"/api/v1/products/{product.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return HandleError(ex, "Failed to create product");
            }
        }

        /// <summary>
        /// Update an existing product
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(StandardApiResponse<Product>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateProduct(
            [FromRoute][Range(1, int.MaxValue, ErrorMessage = "Product ID must be greater than 0")] int id, 
            [FromBody][Required] Product product)
        {
            try
            {
                if (id != product.Id)
                    return BadRequestResponse("Product ID mismatch");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null)
                    return NotFoundResponse($"Product with ID {id} not found");

                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Model = product.Model;
                existingProduct.Sku = product.Sku;
                existingProduct.Upc = product.Upc;
                existingProduct.Asin = product.Asin;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.ProductTypeId = product.ProductTypeId;
                existingProduct.CompanyId = product.CompanyId;
                existingProduct.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var updatedProduct = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductType)
                    .Include(p => p.Company)
                    .FirstOrDefaultAsync(p => p.Id == id);

                return SuccessResponse(updatedProduct, "Product updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                return HandleError(ex, $"Failed to update product {id}");
            }
        }

        /// <summary>
        /// Delete a product
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFoundResponse($"Product with ID {id} not found");

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                return HandleError(ex, $"Failed to delete product {id}");
            }
        }

        /// <summary>
        /// Search products by various criteria
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(StandardPaginatedResponse<IEnumerable<Product>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchProducts(
            [FromQuery] string term,
            [FromQuery] int? categoryId,
            [FromQuery] int? productTypeId,
            [FromQuery] PaginationParams parameters)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductType)
                    .Include(p => p.Company)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(term))
                {
                    query = query.Where(p =>
                        (p.Name != null && p.Name.Contains(term)) ||
                        (p.Description != null && p.Description.Contains(term)) ||
                        (p.Model != null && p.Model.Contains(term)) ||
                        (p.Sku != null && p.Sku.Contains(term)) ||
                        (p.Upc != null && p.Upc.Contains(term)) ||
                        (p.Asin != null && p.Asin.Contains(term)));
                }

                if (categoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                if (productTypeId.HasValue)
                {
                    query = query.Where(p => p.ProductTypeId == productTypeId.Value);
                }

                var totalCount = await query.CountAsync();

                var products = await query
                    .OrderBy(p => p.Name)
                    .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .ToListAsync();

                return PaginatedResponse(products, totalCount, parameters.PageNumber, parameters.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                return HandleError(ex, "Failed to search products");
            }
        }
    }
}