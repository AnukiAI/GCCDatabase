using Microsoft.AspNetCore.Mvc;
using GCCWebApp.Models.Api;

namespace GCCWebApp.Controllers.Api
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    public abstract class BaseApiController : ControllerBase
    {
        protected string RequestId => HttpContext.TraceIdentifier ?? Guid.NewGuid().ToString();

        protected IActionResult HandleError(Exception ex, string message = "An error occurred")
        {
            var response = StandardApiResponse<object>.ErrorResult(message, ApiErrorCodes.InternalError);
            response.RequestId = RequestId;
            
            // Log the full exception details but never expose sensitive information in responses
            // Stack traces and detailed exception information should only be available through logs
            var logger = HttpContext.RequestServices.GetService<ILogger<BaseApiController>>();
            logger?.LogError(ex, "API error occurred for request {RequestId}", RequestId);
            
            // Even in development, limit exposed information to prevent information leakage
            if (HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
            {
                response.Errors[0].Details = new { 
                    message = ex.Message, 
                    requestId = RequestId,
                    timestamp = DateTime.UtcNow
                };
            }

            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }

        protected IActionResult NotFoundResponse(string message = "Resource not found")
        {
            var response = StandardApiResponse<object>.ErrorResult(message, ApiErrorCodes.NotFound);
            response.RequestId = RequestId;
            return NotFound(response);
        }

        protected IActionResult BadRequestResponse(string message = "Invalid request")
        {
            var response = StandardApiResponse<object>.ErrorResult(message, ApiErrorCodes.BadRequest);
            response.RequestId = RequestId;
            return BadRequest(response);
        }

        protected IActionResult UnauthorizedResponse(string message = "Unauthorized access")
        {
            var response = StandardApiResponse<object>.ErrorResult(message, ApiErrorCodes.Unauthorized);
            response.RequestId = RequestId;
            return Unauthorized(response);
        }

        protected IActionResult ForbiddenResponse(string message = "Access forbidden")
        {
            var response = StandardApiResponse<object>.ErrorResult(message, ApiErrorCodes.Forbidden);
            response.RequestId = RequestId;
            return StatusCode(StatusCodes.Status403Forbidden, response);
        }

        protected IActionResult ConflictResponse(string message = "Resource conflict")
        {
            var response = StandardApiResponse<object>.ErrorResult(message, ApiErrorCodes.Conflict);
            response.RequestId = RequestId;
            return Conflict(response);
        }

        protected IActionResult ValidationErrorResponse(List<ApiError> errors)
        {
            var response = StandardApiResponse<object>.ErrorResult(errors);
            response.RequestId = RequestId;
            return BadRequest(response);
        }

        protected IActionResult ValidationErrorResponse(string message, string field = "")
        {
            var error = new ApiError(ApiErrorCodes.ValidationError, message, field);
            var response = StandardApiResponse<object>.ErrorResult(new List<ApiError> { error });
            response.RequestId = RequestId;
            return BadRequest(response);
        }

        protected IActionResult SuccessResponse<T>(T data, string? message = null)
        {
            var response = StandardApiResponse<T>.SuccessResult(data, message);
            response.RequestId = RequestId;
            return Ok(response);
        }

        protected IActionResult CreatedResponse<T>(T data, string? location = null, string? message = null)
        {
            var response = StandardApiResponse<T>.SuccessResult(data, message ?? "Resource created successfully");
            response.RequestId = RequestId;

            if (!string.IsNullOrEmpty(location))
                return Created(location, response);

            return StatusCode(StatusCodes.Status201Created, response);
        }

        protected IActionResult PaginatedResponse<T>(T data, int totalCount, int pageNumber, int pageSize, string? message = null)
        {
            var response = new StandardPaginatedResponse<T>(data, totalCount, pageNumber, pageSize, message);
            response.RequestId = RequestId;
            return Ok(response);
        }

        protected List<ApiError> ModelStateToApiErrors()
        {
            var errors = new List<ApiError>();
            foreach (var modelError in ModelState)
            {
                foreach (var error in modelError.Value.Errors)
                {
                    errors.Add(new ApiError(
                        ApiErrorCodes.ValidationError, 
                        error.ErrorMessage, 
                        modelError.Key.ToLowerInvariant()
                    ));
                }
            }
            return errors;
        }
    }
}