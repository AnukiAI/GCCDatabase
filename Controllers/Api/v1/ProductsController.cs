using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Controllers.Api;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Models.Api;
using Asp.Versioning;

namespace GCCWebApp.Controllers.Api.v1
{
    [ApiVersion("1.0")]
    [Authorize(AuthenticationSchemes = "ApiKey")]
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
        /// Get all products with optional pagination and filtering
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponse<IEnumerable<Product>>), StatusCodes.Status200OK)]
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
                        p.Name.Contains(parameters.SearchTerm) ||
                        p.Description.Contains(parameters.SearchTerm) ||
                        p.Model.Contains(parameters.SearchTerm) ||
                        p.Sku.Contains(parameters.SearchTerm));
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

                var totalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize);

                var response = new PaginatedResponse<IEnumerable<Product>>
                {
                    Success = true,
                    Data = products,
                    TotalCount = totalCount,
                    PageNumber = parameters.PageNumber,
                    PageSize = parameters.PageSize,
                    TotalPages = totalPages,
                    HasPrevious = parameters.PageNumber > 1,
                    HasNext = parameters.PageNumber < totalPages
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return HandleError(ex, "Failed to retrieve products");
            }
        }

        /// <summary>
        /// Get a product by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResult<Product>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProduct(int id)
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
        [ProducesResponseType(typeof(ApiResponse<Product>), StatusCodes.Status201Created)]
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
        [ProducesResponseType(typeof(ApiResult<Product>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product)
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
        [ProducesResponseType(typeof(PaginatedResponse<IEnumerable<Product>>), StatusCodes.Status200OK)]
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
                        p.Name.Contains(term) ||
                        p.Description.Contains(term) ||
                        p.Model.Contains(term) ||
                        p.Sku.Contains(term) ||
                        p.Upc.Contains(term) ||
                        p.Asin.Contains(term));
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

                var totalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize);

                var response = new PaginatedResponse<IEnumerable<Product>>
                {
                    Success = true,
                    Data = products,
                    TotalCount = totalCount,
                    PageNumber = parameters.PageNumber,
                    PageSize = parameters.PageSize,
                    TotalPages = totalPages,
                    HasPrevious = parameters.PageNumber > 1,
                    HasNext = parameters.PageNumber < totalPages
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products");
                return HandleError(ex, "Failed to search products");
            }
        }
    }
}