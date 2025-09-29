using System.Text.Json.Serialization;

namespace GCCWebApp.Models.Api
{
    public class StandardApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("errors")]
        public List<ApiError> Errors { get; set; } = new();

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        public StandardApiResponse() { }

        public StandardApiResponse(T data, string? message = null)
        {
            Success = true;
            Data = data;
            Message = message;
        }

        public StandardApiResponse(ApiError error)
        {
            Success = false;
            Errors.Add(error);
        }

        public StandardApiResponse(List<ApiError> errors)
        {
            Success = false;
            Errors = errors ?? new List<ApiError>();
        }

        public StandardApiResponse(string errorMessage, string errorCode = "GENERAL_ERROR")
        {
            Success = false;
            Errors.Add(new ApiError(errorCode, errorMessage));
        }

        public static StandardApiResponse<T> SuccessResult(T data, string? message = null)
        {
            return new StandardApiResponse<T>(data, message);
        }

        public static StandardApiResponse<T> ErrorResult(string message, string code = "GENERAL_ERROR")
        {
            return new StandardApiResponse<T>(message, code);
        }

        public static StandardApiResponse<T> ErrorResult(List<ApiError> errors)
        {
            return new StandardApiResponse<T>(errors);
        }
    }

    public class StandardPaginatedResponse<T> : StandardApiResponse<T>
    {
        [JsonPropertyName("pagination")]
        public PaginationMetadata Pagination { get; set; } = new();

        public StandardPaginatedResponse() { }

        public StandardPaginatedResponse(T data, int totalCount, int pageNumber, int pageSize, string? message = null)
            : base(data, message)
        {
            Pagination = new PaginationMetadata
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                HasPrevious = pageNumber > 1,
                HasNext = pageNumber < (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
    }

    public class PaginationMetadata
    {
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("hasPrevious")]
        public bool HasPrevious { get; set; }

        [JsonPropertyName("hasNext")]
        public bool HasNext { get; set; }

        [JsonPropertyName("firstPage")]
        public int FirstPage => 1;

        [JsonPropertyName("lastPage")]
        public int LastPage => TotalPages;
    }

    public class ApiError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("field")]
        public string? Field { get; set; }

        [JsonPropertyName("details")]
        public object? Details { get; set; }

        public ApiError(string code, string message, string? field = null, object? details = null)
        {
            Code = code;
            Message = message;
            Field = field;
            Details = details;
        }
    }

    public static class ApiErrorCodes
    {
        public const string ValidationError = "VALIDATION_ERROR";
        public const string NotFound = "RESOURCE_NOT_FOUND";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string Forbidden = "FORBIDDEN";
        public const string Conflict = "RESOURCE_CONFLICT";
        public const string InternalError = "INTERNAL_SERVER_ERROR";
        public const string BadRequest = "BAD_REQUEST";
        public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
        public const string InvalidApiKey = "INVALID_API_KEY";
        public const string ExpiredToken = "EXPIRED_TOKEN";
        public const string DuplicateResource = "DUPLICATE_RESOURCE";
        public const string BusinessRuleViolation = "BUSINESS_RULE_VIOLATION";
    }
}