using System.Net;
using System.Text.Json;
using GCCWebApp.Models.Api;
using Microsoft.EntityFrameworkCore;

namespace GCCWebApp.Middleware
{
    public class ApiExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public ApiExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ApiExceptionHandlingMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    await HandleApiExceptionAsync(context, ex);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task HandleApiExceptionAsync(HttpContext context, Exception exception)
        {
            var requestId = context.TraceIdentifier ?? Guid.NewGuid().ToString();

            _logger.LogError(exception, "API Exception occurred. RequestId: {RequestId}, Path: {Path}, Method: {Method}",
                requestId, context.Request.Path, context.Request.Method);

            var response = CreateErrorResponse(exception, requestId);
            var statusCode = GetStatusCode(exception);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            context.Response.Headers.Append("X-Request-Id", requestId);
            context.Response.Headers.Append("X-Error-Code", response.Errors.FirstOrDefault()?.Code ?? "UNKNOWN_ERROR");

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment()
            };

            var jsonResponse = JsonSerializer.Serialize(response, options);
            await context.Response.WriteAsync(jsonResponse);
        }

        private StandardApiResponse<object> CreateErrorResponse(Exception exception, string requestId)
        {
            var response = exception switch
            {
                ValidationException validationEx => StandardApiResponse<object>.ErrorResult(
                    CreateValidationErrors(validationEx)),

                UnauthorizedAccessException _ => StandardApiResponse<object>.ErrorResult(
                    "You are not authorized to access this resource",
                    ApiErrorCodes.Unauthorized),

                KeyNotFoundException _ => StandardApiResponse<object>.ErrorResult(
                    "The requested resource was not found",
                    ApiErrorCodes.NotFound),

                InvalidOperationException invalidEx when invalidEx.Message.Contains("business rule", StringComparison.OrdinalIgnoreCase) =>
                    StandardApiResponse<object>.ErrorResult(
                        invalidEx.Message,
                        ApiErrorCodes.BusinessRuleViolation),

                DbUpdateException dbEx when dbEx.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true =>
                    StandardApiResponse<object>.ErrorResult(
                        "A resource with the same key already exists",
                        ApiErrorCodes.DuplicateResource),

                DbUpdateConcurrencyException _ => StandardApiResponse<object>.ErrorResult(
                    "The resource has been modified by another user. Please refresh and try again",
                    ApiErrorCodes.Conflict),

                TimeoutException _ => StandardApiResponse<object>.ErrorResult(
                    "The operation timed out. Please try again",
                    "TIMEOUT_ERROR"),

                ArgumentException argEx => StandardApiResponse<object>.ErrorResult(
                    argEx.Message,
                    ApiErrorCodes.BadRequest),

                NotSupportedException _ => StandardApiResponse<object>.ErrorResult(
                    "This operation is not supported",
                    "NOT_SUPPORTED"),

                _ => StandardApiResponse<object>.ErrorResult(
                    _environment.IsDevelopment()
                        ? exception.Message
                        : "An unexpected error occurred. Please try again later",
                    ApiErrorCodes.InternalError)
            };

            if (_environment.IsDevelopment() && exception is not ValidationException && response.Errors != null)
            {
                response.Errors.FirstOrDefault()!.Details = new
                {
                    type = exception.GetType().Name,
                    stackTrace = exception.StackTrace?.Split('\n').Take(5),
                    innerException = exception.InnerException?.Message
                };
            }

            return response;
        }

        private List<ApiError> CreateValidationErrors(ValidationException validationException)
        {
            var errors = new List<ApiError>();

            if (validationException.Data.Contains("Errors") &&
                validationException.Data["Errors"] is Dictionary<string, List<string>> errorDict)
            {
                foreach (var (field, messages) in errorDict)
                {
                    foreach (var message in messages)
                    {
                        errors.Add(new ApiError(
                            ApiErrorCodes.ValidationError,
                            message,
                            field.ToLowerInvariant()));
                    }
                }
            }
            else
            {
                errors.Add(new ApiError(
                    ApiErrorCodes.ValidationError,
                    validationException.Message));
            }

            return errors;
        }

        private HttpStatusCode GetStatusCode(Exception exception)
        {
            return exception switch
            {
                ValidationException => HttpStatusCode.BadRequest,
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                KeyNotFoundException => HttpStatusCode.NotFound,
                InvalidOperationException invalidEx when invalidEx.Message.Contains("business rule", StringComparison.OrdinalIgnoreCase) =>
                    HttpStatusCode.UnprocessableEntity,
                DbUpdateException dbEx when dbEx.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true =>
                    HttpStatusCode.Conflict,
                DbUpdateConcurrencyException => HttpStatusCode.Conflict,
                TimeoutException => HttpStatusCode.RequestTimeout,
                ArgumentException => HttpStatusCode.BadRequest,
                NotSupportedException => HttpStatusCode.NotImplemented,
                _ => HttpStatusCode.InternalServerError
            };
        }
    }

    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }

        public ValidationException(string message, Dictionary<string, List<string>> errors) : base(message)
        {
            Data["Errors"] = errors;
        }
    }

    public static class ApiExceptionHandlingExtensions
    {
        public static IApplicationBuilder UseApiExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ApiExceptionHandlingMiddleware>();
        }
    }
}