namespace AssetLite.Application.Abstractions;

/// <summary>
/// Provides UTC time to the application layer so handlers stay deterministic and testable.
/// </summary>
/// <remarks>
/// Implementations must use <see cref="DateTimeOffset.UtcNow"/> — never
/// <see cref="DateTime.Now"/> or other local-time APIs.
/// </remarks>
public interface IDateTimeProvider
{
    /// <summary>Gets the current UTC instant.</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>Gets the current UTC date.</summary>
    DateOnly Today { get; }
}
