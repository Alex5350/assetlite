namespace AssetLite.Domain.Common;

/// <summary>
/// A generic domain operation result carrying a value on success or a <see cref="DomainError"/>
/// on failure. Used by value-object and aggregate factory methods.
/// </summary>
/// <typeparam name="T">The type of the produced value.</typeparam>
/// <remarks>
/// Expected business failures are represented as values, never as exceptions. The
/// <paramref name="Error"/> component is guaranteed to be non-null for results created via
/// <see cref="Failure"/> and null for results created via <see cref="Success(T)"/>.
/// </remarks>
public readonly record struct DomainResult<T>(bool IsSuccess, T? Value, DomainError? Error)
{
    /// <summary>Creates a successful result carrying <paramref name="value"/>.</summary>
    /// <param name="value">The produced value.</param>
    /// <returns>A successful result.</returns>
    public static DomainResult<T> Success(T value) => new(true, value, null);

    /// <summary>Creates a failed result carrying <paramref name="error"/>.</summary>
    /// <param name="error">The error describing the business rule violation.</param>
    /// <returns>A failed result.</returns>
    public static DomainResult<T> Failure(DomainError error) => new(false, default, error);

    /// <summary>Gets a value indicating whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Returns the value of a successful result. Throws <see cref="InvalidOperationException"/>
    /// when called on a failed result: accessing a missing value is a programming error, not an
    /// expected domain failure.
    /// </summary>
    /// <returns>The produced value.</returns>
    public T GetValueOrThrow() =>
        IsSuccess && Value is not null
            ? Value
            : throw new InvalidOperationException(
                $"Cannot access the value of a failed domain result (error: {Error?.Code ?? "unknown"}).");
}
