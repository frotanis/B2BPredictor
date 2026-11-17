namespace B2BPredictor.Application.Contracts;

public enum ServiceErrorType
{
    None = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4
}

public sealed record ServiceResult<T>(
    T? Value,
    string? ErrorMessage = null,
    ServiceErrorType ErrorType = ServiceErrorType.None)
{
    public bool IsSuccess => ErrorType == ServiceErrorType.None;
}

public static class ServiceResult
{
    public static ServiceResult<T> Success<T>(T value) => new(value);

    public static ServiceResult<T> Failure<T>(string message, ServiceErrorType errorType) =>
        new(default, message, errorType);
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling((double)TotalCount / PageSize)
        : 0;
}