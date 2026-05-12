namespace Ledger.Shared;

/// <summary>
/// Lightweight Result type to keep domain code free of exception-driven control flow.
/// Use <see cref="Result.Ok"/> / <see cref="Result.Fail"/> for void results,
/// and <see cref="Result{T}"/> when returning a value.
/// </summary>
public readonly record struct Result(bool IsSuccess, string? Error)
{
    public static Result Ok() => new(true, null);
    public static Result Fail(string error) => new(false, error);

    public bool IsFailure => !IsSuccess;
}

public readonly record struct Result<T>(bool IsSuccess, T? Value, string? Error)
{
    public static Result<T> Ok(T value) => new(true, value, null);
    public static Result<T> Fail(string error) => new(false, default, error);

    public bool IsFailure => !IsSuccess;
}
