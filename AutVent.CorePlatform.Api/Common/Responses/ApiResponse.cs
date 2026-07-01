namespace AutVent.CorePlatform.Api.Common.Responses;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IReadOnlyCollection<ApiError> Errors { get; init; } = [];
    public object? Meta { get; init; }
    public string? TraceId { get; init; }
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;

    public static ApiResponse<T> Ok(T? data, string message = "Request completed successfully", object? meta = null, string? traceId = null) =>
        new()
        {
            Success = true,
            StatusCode = StatusCodes.Status200OK,
            Message = message,
            Data = data,
            Meta = meta,
            TraceId = traceId
        };

    public static ApiResponse<T> Created(T? data, string message = "Resource created successfully", object? meta = null, string? traceId = null) =>
        new()
        {
            Success = true,
            StatusCode = StatusCodes.Status201Created,
            Message = message,
            Data = data,
            Meta = meta,
            TraceId = traceId
        };

    public static ApiResponse<T> Failed(
        int statusCode,
        string message,
        IEnumerable<ApiError>? errors = null,
        object? meta = null,
        string? traceId = null) =>
        new()
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            Errors = errors?.ToArray() ?? [],
            Meta = meta,
            TraceId = traceId
        };

    public static ApiResponse<T> ValidationFailed(
        string message,
        IEnumerable<ApiError> errors,
        object? meta = null,
        string? traceId = null) =>
        Failed(StatusCodes.Status400BadRequest, message, errors, meta, traceId);
}

public sealed record ApiError(string Code, string Message, string? Field = null);
