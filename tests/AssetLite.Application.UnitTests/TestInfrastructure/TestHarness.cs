using AssetLite.Application.Abstractions;
using AssetLite.Domain.Identities;
using AssetLite.Domain.Offices;
using AssetLite.Domain.ValueObjects;
using NSubstitute;

namespace AssetLite.Application.UnitTests.TestInfrastructure;

/// <summary>Shared helpers for Application-layer handler tests (frozen clock, tag factory).</summary>
public static class TestHarness
{
    /// <summary>A single frozen instant used by all handler tests for determinism.</summary>
    public static readonly DateTimeOffset FixedNow = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    /// <summary>The frozen UTC date matching <see cref="FixedNow"/>.</summary>
    public static readonly DateOnly FixedToday = new(2026, 6, 15);

    /// <summary>Creates an <see cref="IDateTimeProvider"/> substitute pinned to <see cref="FixedNow"/>.</summary>
    public static IDateTimeProvider FrozenClock()
    {
        var provider = Substitute.For<IDateTimeProvider>();
        provider.UtcNow.Returns(FixedNow);
        provider.Today.Returns(FixedToday);
        return provider;
    }

    /// <summary>Creates a canonical asset tag from a tag number.</summary>
    public static AssetTag Tag(int number) => AssetTag.FromNumber(number).GetValueOrThrow();
}

/// <summary>
/// An in-memory office graph exposed through an <see cref="IOfficeRepository"/> substitute;
/// hierarchy rules are exercised against real parent chains.
/// </summary>
public sealed class OfficeStore
{
    private readonly Dictionary<OfficeId, Office> _offices = [];

    /// <summary>Creates and stores an office, returning it with its generated id.</summary>
    public Office Add(string name, string code, OfficeId? parentOfficeId = null)
    {
        var office = Office.Create(name, code, parentOfficeId).GetValueOrThrow();
        _offices[office.Id] = office;
        return office;
    }

    /// <summary>Looks up a stored office by id.</summary>
    public Office? Find(OfficeId id) => _offices.GetValueOrDefault(id);

    /// <summary>Builds a repository substitute backed by this store.</summary>
    public IOfficeRepository AsRepository()
    {
        var repository = Substitute.For<IOfficeRepository>();
        repository
            .GetByIdAsync(Arg.Any<OfficeId>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Find(callInfo.ArgAt<OfficeId>(0)));
        repository
            .ListChildrenAsync(Arg.Any<OfficeId>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => _offices.Values
                .Where(office => office.ParentOfficeId == callInfo.ArgAt<OfficeId>(0))
                .ToList());
        repository
            .ListAllAsync(Arg.Any<CancellationToken>())
            .Returns(_ => _offices.Values.ToList());
        return repository;
    }
}
