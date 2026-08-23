using AssetLite.Application.Abstractions;
using AssetLite.Application.Assets;
using AssetLite.Application.UnitTests.TestInfrastructure;
using AssetLite.Domain.Assets;
using AssetLite.Domain.Common;
using AssetLite.Domain.Enums;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using ErrorOr;
using NSubstitute;
using Xunit;

namespace AssetLite.Application.UnitTests.Assets;

/// <summary>Unit tests for <see cref="AssignAssetHandler"/>.</summary>
public sealed class AssignAssetHandlerTests
{
    private readonly IAssetRepository _assetRepository = Substitute.For<IAssetRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = TestHarness.FrozenClock();
    private readonly IDomainEventDispatcher _domainEventDispatcher = Substitute.For<IDomainEventDispatcher>();
    private IReadOnlyList<IDomainEvent>? _dispatchedEvents;

    public AssignAssetHandlerTests()
    {
        _ = _domainEventDispatcher
            .DispatchAsync(
                Arg.Do<IReadOnlyList<IDomainEvent>>(events => _dispatchedEvents = events),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    private AssignAssetHandler CreateHandler() =>
        new(_assetRepository, _unitOfWork, _clock, _domainEventDispatcher);

    [Fact]
    public async Task HandleAsync_WithInStockAsset_AssignsUsingFrozenClockAndPersistsInOrder()
    {
        var asset = AssetFactory.NewAsset();
        _assetRepository.GetByIdAsync(asset.Id, Arg.Any<CancellationToken>()).Returns(asset);
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new AssignAssetCommand(asset.Id, "Sarah Chen", "sarah.chen@assetlite.example"),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(AssetStatus.Assigned, asset.Status);
        Assert.Equal(TestHarness.FixedNow, asset.OpenAssignment!.AssignedAtUtc); // frozen clock, not wall time

        Assert.NotNull(_dispatchedEvents);
        var assigned = Assert.IsType<AssetAssignedDomainEvent>(Assert.Single(_dispatchedEvents!));
        Assert.Equal(asset.Id, assigned.AssetId);
        Assert.Equal("Sarah Chen", assigned.AssigneeName);
        await _domainEventDispatcher
            .Received(1)
            .DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());

        // NSubstitute's InOrder takes a sync Action; the awaits inside complete immediately
        // because the calls were already made during the handler invocation under test.
        Received.InOrder(async () =>
        {
            await _assetRepository.Received(1).UpdateAsync(asset, Arg.Any<CancellationToken>());
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
            await _domainEventDispatcher.Received(1).DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task HandleAsync_WithUnknownAsset_ReturnsAssetNotFound()
    {
        _assetRepository
            .GetByIdAsync(Arg.Any<AssetId>(), Arg.Any<CancellationToken>())
            .Returns((Asset?)null);
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new AssignAssetCommand(AssetId.New(), "Sarah Chen", "sarah.chen@assetlite.example"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(AssetErrors.NotFound.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        await _assetRepository.DidNotReceive().UpdateAsync(Arg.Any<Asset>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _domainEventDispatcher.DidNotReceive().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithRetiredAsset_MapsDomainErrorToConflict()
    {
        var asset = AssetFactory.RetiredAsset();
        _assetRepository.GetByIdAsync(asset.Id, Arg.Any<CancellationToken>()).Returns(asset);
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new AssignAssetCommand(asset.Id, "Sarah Chen", "sarah.chen@assetlite.example"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(AssetErrors.CannotAssignRetired.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        await _assetRepository.DidNotReceive().UpdateAsync(Arg.Any<Asset>(), Arg.Any<CancellationToken>());
        await _domainEventDispatcher.DidNotReceive().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithInvalidEmail_MapsDomainValidationError()
    {
        var asset = AssetFactory.NewAsset();
        _assetRepository.GetByIdAsync(asset.Id, Arg.Any<CancellationToken>()).Returns(asset);
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new AssignAssetCommand(asset.Id, "Sarah Chen", "not-an-email"),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(AssetErrors.InvalidAssigneeEmail.Code, result.FirstError.Code);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithAlreadyAssignedAsset_ReassignsAndRaisesEventForNewAssignee()
    {
        var asset = AssetFactory.AssignedAsset();
        _assetRepository.GetByIdAsync(asset.Id, Arg.Any<CancellationToken>()).Returns(asset);
        var handler = CreateHandler();

        var result = await handler.HandleAsync(
            new AssignAssetCommand(asset.Id, "Marcus Webb", "marcus.webb@assetlite.example"),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("Marcus Webb", asset.OpenAssignment!.AssigneeName);
        Assert.Equal(2, asset.Assignments.Count);
        Assert.False(asset.Assignments[0].IsOpen);
    }
}
