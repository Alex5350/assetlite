namespace AssetLite.Domain.Common;

/// <summary>
/// A domain-level failure with a stable, machine-readable code (e.g. <c>"Asset.CannotAssignRetired"</c>)
/// and a human-readable message suitable for display or problem details.
/// </summary>
/// <remarks>
/// <para>
/// The Domain layer is intentionally free of external packages and never throws for expected
/// business failures. Instead, aggregate factories and state-transition methods return
/// <see cref="DomainResult"/> / <see cref="DomainResult{T}"/> carrying one of these errors.
/// The Application layer maps <see cref="DomainError"/> values to the ErrorOr result type at
/// its boundary (see <c>AssetLite.Application.Abstractions.DomainResultMapping</c>).
/// </para>
/// <para>
/// Error codes follow the <c>{Aggregate}.{Rule}</c> convention, are part of the public contract
/// consumed by the API layer and tests, and must not be renamed once released.
/// </para>
/// </remarks>
public sealed record DomainError(string Code, string Message);
