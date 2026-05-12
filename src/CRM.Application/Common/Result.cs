namespace CRM.Application.Common;

/// <summary>Generic result wrapper used by Application services to avoid throwing on expected failures.</summary>
public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, string? error, string? errorCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorCode = errorCode;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T? Value { get; }

    public string? Error { get; }

    public string? ErrorCode { get; }

    public static Result<T> Success(T value) => new(true, value, null, null);

    public static Result<T> Failure(string error, string? errorCode = null) =>
        new(false, default, error, errorCode);
}

public static class ResultErrorCodes
{
    public const string NotFound = "not_found";
    public const string Duplicate = "duplicate";
    public const string Validation = "validation";
    public const string Unauthorized = "unauthorized";
    public const string Forbidden = "forbidden";
    public const string Conflict = "conflict";
}
