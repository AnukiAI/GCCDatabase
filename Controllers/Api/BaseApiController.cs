using Microsoft.AspNetCore.Mvc;

namespace GCCWebApp.Controllers.Api
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    public abstract class BaseApiController : ControllerBase
    {
        protected IActionResult HandleError(Exception ex, string message = "An error occurred")
        {
            var problemDetails = new ProblemDetails
            {
                Title = message,
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError,
                Instance = HttpContext.Request.Path
            };

            return StatusCode(StatusCodes.Status500InternalServerError, problemDetails);
        }

        protected IActionResult NotFoundResponse(string message = "Resource not found")
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Not Found",
                Detail = message,
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            };

            return NotFound(problemDetails);
        }

        protected IActionResult BadRequestResponse(string message = "Invalid request")
        {
            var problemDetails = new ProblemDetails
            {
                Title = "Bad Request",
                Detail = message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            };

            return BadRequest(problemDetails);
        }

        protected IActionResult SuccessResponse<T>(T data, string message = null)
        {
            var response = new
            {
                success = true,
                message,
                data,
                timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }

        protected IActionResult CreatedResponse<T>(T data, string location = null)
        {
            var response = new
            {
                success = true,
                data,
                timestamp = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(location))
                return Created(location, response);

            return StatusCode(StatusCodes.Status201Created, response);
        }
    }
}