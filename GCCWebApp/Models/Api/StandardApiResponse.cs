using System.Text.Json.Serialization;

namespace GCCWebApp.Models.Api
{
    // Response envelope — shape matches Amazon Developer Guide §5.1 / §5.2 / §6.
    // Success response: { success, pagination?, data }
    // Error response:   { success: false, errors: [...] }  (message/errors suppressed when empty)
    public class StandardApiResponse<T>
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyOrder(3)]
        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyOrder(4)]
        [JsonPropertyName("message")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Message { get; set; }

        [JsonPropertyOrder(5)]
        [JsonPropertyName("errors")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ApiError>? Errors { get; set; }

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
            Errors = new List<ApiError> { error };
        }

        public StandardApiResponse(List<ApiError> errors)
        {
            Success = false;
            Errors = errors ?? new List<ApiError>();
        }

        public StandardApiResponse(string errorMessage, string errorCode = "GENERAL_ERROR")
        {
            Success = false;
            Errors = new List<ApiError> { new ApiError(errorCode, errorMessage) };
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
        [JsonPropertyOrder(2)]
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
                HasNext = pageNumber < (int)Math.Ceiling(totalCount / (double)pageSize),
                HasPrevious = pageNumber > 1
            };
        }
    }

    // Field order matches Amazon Developer Guide §5.1 pagination example.
    public class PaginationMetadata
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyOrder(2)]
        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; }

        [JsonPropertyOrder(3)]
        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyOrder(4)]
        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyOrder(5)]
        [JsonPropertyName("hasNext")]
        public bool HasNext { get; set; }

        [JsonPropertyOrder(6)]
        [JsonPropertyName("hasPrevious")]
        public bool HasPrevious { get; set; }
    }

    public class ApiError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("field")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Field { get; set; }

        [JsonPropertyName("details")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
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
