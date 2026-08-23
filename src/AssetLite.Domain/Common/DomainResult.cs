namespace AssetLite.Domain.Common;

/// <summary>
/// A non-generic domain operation result: either success or a <see cref="DomainError"/>.
/// Used by domain operations that change state without producing a value.
/// </summary>
/// <remarks>
/// Expected business failures are represented as values, never as exceptions. The
/// <paramref name="Error"/> component is guaranteed to be non-null for results created via
/// <see cref="Failure"/> and null for results created via <see cref="Success"/>.
/// </remarks>
public readonly record struct DomainResult(bool IsSuccess, DomainError? Error)
{
    /// <summary>Creates a successful result.</summary>
    /// <returns>A successful result with no error.</returns>
    public static DomainResult Success() => new(true, null);

    /// <summary>Creates a failed result carrying <paramref name="error"/>.</summary>
    /// <param name="error">The error describing the business rule violation.</param>
    /// <returns>A failed result.</returns>
    public static DomainResult Failure(DomainError error) => new(false, error);

    /// <summary>Gets a value indicating whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;
}
