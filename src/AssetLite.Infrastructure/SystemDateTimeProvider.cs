using AssetLite.Application.Abstractions;

namespace AssetLite.Infrastructure;

/// <summary>
/// Production <see cref="IDateTimeProvider"/>: always UTC (<see cref="DateTimeOffset.UtcNow"/>),
/// never local time.
/// </summary>
internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
