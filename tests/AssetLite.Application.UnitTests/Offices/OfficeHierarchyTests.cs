using AssetLite.Application.Abstractions;
using AssetLite.Application.Offices;
using AssetLite.Application.UnitTests.TestInfrastructure;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using AssetLite.Domain.Offices;
using Xunit;

namespace AssetLite.Application.UnitTests.Offices;

/// <summary>Unit tests for the <see cref="OfficeHierarchy"/> domain service implementation.</summary>
public sealed class OfficeHierarchyTests
{
    private readonly OfficeStore _store = new();
    private readonly IOfficeRepository _repository;

    public OfficeHierarchyTests()
    {
        _repository = _store.AsRepository();
    }

    private OfficeHierarchy CreateHierarchy() => new(_repository);

    [Fact]
    public async Task IsDescendantOfAsync_WithDirectChild_ReturnsTrue()
    {
        var root = _store.Add("Headquarters", "ASTHQ");
        var east = _store.Add("East Region", "ASTEAST", root.Id);
        var hierarchy = CreateHierarchy();

        var isDescendant = await hierarchy.IsDescendantOfAsync(east.Id, root.Id, TestContext.Current.CancellationToken);

        Assert.True(isDescendant);
    }

    [Fact]
    public async Task IsDescendantOfAsync_WithGrandChild_ReturnsTrue()
    {
        var root = _store.Add("Headquarters", "ASTHQ");
        var east = _store.Add("East Region", "ASTEAST", root.Id);
        var nyc = _store.Add("New York Site", "ASTNYC", east.Id);
        var hierarchy = CreateHierarchy();

        var isDescendant = await hierarchy.IsDescendantOfAsync(nyc.Id, root.Id, TestContext.Current.CancellationToken);

        Assert.True(isDescendant);
    }

    [Fact]
    public async Task IsDescendantOfAsync_WithUnrelatedOffices_ReturnsFalse()
    {
        var root = _store.Add("Headquarters", "ASTHQ");
        var east = _store.Add("East Region", "ASTEAST", root.Id);
        var west = _store.Add("West Region", "ASTWEST", root.Id);
        var hierarchy = CreateHierarchy();

        var isDescendant = await hierarchy.IsDescendantOfAsync(west.Id, east.Id, TestContext.Current.CancellationToken);

        Assert.False(isDescendant);
    }

    [Fact]
    public async Task IsDescendantOfAsync_WithSelf_ReturnsFalse()
    {
        var root = _store.Add("Headquarters", "ASTHQ");
        var hierarchy = CreateHierarchy();

        var isDescendant = await hierarchy.IsDescendantOfAsync(root.Id, root.Id, TestContext.Current.CancellationToken);

        Assert.False(isDescendant);
    }

    [Fact]
    public async Task IsDescendantOfAsync_WithAncestorChainReachingRoot_ReturnsFalse()
    {
        var root = _store.Add("Headquarters", "ASTHQ");
        var east = _store.Add("East Region", "ASTEAST", root.Id);
        var hierarchy = CreateHierarchy();

        // Walking up from the root stops at the root; the ancestor is never found.
        var isDescendant = await hierarchy.IsDescendantOfAsync(root.Id, east.Id, TestContext.Current.CancellationToken);

        Assert.False(isDescendant);
    }

    [Fact]
    public async Task IsDescendantOfAsync_WithUnknownCandidate_ReturnsFalse()
    {
        var ancestor = _store.Add("Headquarters", "ASTHQ");
        var hierarchy = CreateHierarchy();

        var isDescendant = await hierarchy.IsDescendantOfAsync(OfficeId.New(), ancestor.Id, TestContext.Current.CancellationToken);

        Assert.False(isDescendant);
    }

    [Fact]
    public async Task IsDescendantOfAsync_WithCyclicChain_BlocksByTreatingItAsDescendant()
    {
        var x = _store.Add("X Office", "ASTXXX");
        var y = _store.Add("Y Office", "ASTYYY");
        var z = _store.Add("Z Office", "ASTZZZ");
        x.Reparent(y.Id);
        y.Reparent(z.Id);
        z.Reparent(x.Id); // cycle: x -> y -> z -> x
        var missingAncestor = OfficeId.New();
        var hierarchy = CreateHierarchy();

        // The ancestor walk never terminates; the step cap makes the operation fail closed.
        var isDescendant = await hierarchy.IsDescendantOfAsync(x.Id, missingAncestor, TestContext.Current.CancellationToken);

        Assert.True(isDescendant);
    }

    [Fact]
    public async Task EnsureValidParentAsync_WithNullParent_Succeeds()
    {
        var root = _store.Add("Headquarters", "ASTHQ");
        var hierarchy = CreateHierarchy();

        var result = await hierarchy.EnsureValidParentAsync(root.Id, null, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task EnsureValidParentAsync_WithSelfAsParent_ReturnsCannotBeOwnParent()
    {
        var root = _store.Add("Headquarters", "ASTHQ");
        var hierarchy = CreateHierarchy();

        var result = await hierarchy.EnsureValidParentAsync(root.Id, root.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(OfficeErrors.CannotBeOwnParent, result.Error);
    }

    [Fact]
    public async Task EnsureValidParentAsync_WithMissingParent_ReturnsNotFound()
    {
        var root = _store.Add("Headquarters", "ASTHQ");
        var hierarchy = CreateHierarchy();

        var result = await hierarchy.EnsureValidParentAsync(root.Id, OfficeId.New(), TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(OfficeErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task EnsureValidParentAsync_WithOwnDescendantAsParent_ReturnsCannotMoveUnderDescendant()
    {
        var root = _store.Add("Headquarters", "ASTHQ");
        var east = _store.Add("East Region", "ASTEAST", root.Id);
        var nyc = _store.Add("New York Site", "ASTNYC", east.Id);
        var hierarchy = CreateHierarchy();

        var result = await hierarchy.EnsureValidParentAsync(root.Id, nyc.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(OfficeErrors.CannotMoveUnderDescendant, result.Error);
    }

    [Fact]
    public async Task EnsureValidParentAsync_WithParentAtMaxDepthMinusOne_Succeeds()
    {
        // HQ (1) -> region (2) -> site (3); a room under the site lands exactly at depth 4.
        var root = _store.Add("Headquarters", "ASTHQ");
        var region = _store.Add("East Region", "ASTEAST", root.Id);
        var site = _store.Add("New York Site", "ASTNYC", region.Id);
        var room = Office.Create("Server Room", "ASTSRV", null).GetValueOrThrow();
        var hierarchy = CreateHierarchy();

        var result = await hierarchy.EnsureValidParentAsync(room.Id, site.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task EnsureValidParentAsync_WithParentAtMaxDepth_ReturnsMaxDepthExceeded()
    {
        // HQ (1) -> region (2) -> site (3) -> room (4); anything below the room exceeds the cap.
        var root = _store.Add("Headquarters", "ASTHQ");
        var region = _store.Add("East Region", "ASTEAST", root.Id);
        var site = _store.Add("New York Site", "ASTNYC", region.Id);
        var room = _store.Add("Server Room", "ASTSRV", site.Id);
        var deeper = Office.Create("Rack 5", "ASTRCK", null).GetValueOrThrow();
        var hierarchy = CreateHierarchy();

        var result = await hierarchy.EnsureValidParentAsync(deeper.Id, room.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(OfficeErrors.MaxDepthExceeded, result.Error);
    }

    [Fact]
    public async Task CollectOfficeAndDescendantsAsync_ReturnsRootThenAllDescendants()
    {
        var root = _store.Add("Headquarters", "ASTHQ");
        var east = _store.Add("East Region", "ASTEAST", root.Id);
        var west = _store.Add("West Region", "ASTWEST", root.Id);
        var nyc = _store.Add("New York Site", "ASTNYC", east.Id);
        var sfo = _store.Add("San Francisco Site", "ASTSFO", west.Id);
        var hierarchy = CreateHierarchy();

        var collected = await hierarchy.CollectOfficeAndDescendantsAsync(root.Id, TestContext.Current.CancellationToken);

        Assert.Equal(5, collected.Count);
        Assert.Equal(root.Id, collected[0]); // breadth-first from the root
        Assert.Contains(east.Id, collected);
        Assert.Contains(west.Id, collected);
        Assert.Contains(nyc.Id, collected);
        Assert.Contains(sfo.Id, collected);
        // Descendants come before the root's grandchildren: both regions precede both sites.
        Assert.True(collected.ToList().IndexOf(east.Id) < collected.ToList().IndexOf(nyc.Id));
        Assert.True(collected.ToList().IndexOf(west.Id) < collected.ToList().IndexOf(sfo.Id));
    }

    [Fact]
    public async Task CollectOfficeAndDescendantsAsync_WithSubtreeRoot_ReturnsOnlyTheSubtree()
    {
        var root = _store.Add("Headquarters", "ASTHQ");
        var east = _store.Add("East Region", "ASTEAST", root.Id);
        _store.Add("West Region", "ASTWEST", root.Id);
        var nyc = _store.Add("New York Site", "ASTNYC", east.Id);
        var hierarchy = CreateHierarchy();

        var collected = await hierarchy.CollectOfficeAndDescendantsAsync(east.Id, TestContext.Current.CancellationToken);

        Assert.Equal([east.Id, nyc.Id], collected);
    }

    [Fact]
    public async Task CollectOfficeAndDescendantsAsync_WithUnknownRoot_ReturnsEmptyList()
    {
        var hierarchy = CreateHierarchy();

        var collected = await hierarchy.CollectOfficeAndDescendantsAsync(OfficeId.New(), TestContext.Current.CancellationToken);

        Assert.Empty(collected);
    }
}
