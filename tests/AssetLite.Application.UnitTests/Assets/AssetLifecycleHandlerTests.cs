using AssetLite.Application.Abstractions;
using AssetLite.Application.Assets;
using AssetLite.Application.UnitTests.TestInfrastructure;
using AssetLite.Domain.Assets;
using AssetLite.Domain.Common;
using AssetLite.Domain.Enums;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using AssetLite.Domain.Offices;
using ErrorOr;
using NSubstitute;
using Xunit;

namespace AssetLite.Application.UnitTests.Assets;

/// <summary>
/// Unit tests for the lifecycle command handlers: return, maintenance, resume, retire, dispose
/// and transfer. Each handler is covered for its happy path, its NotFound mapping and its
/// DomainResult error mapping (domain codes are preserved; NotFound suffix maps to 404).
/// </summary>
public sealed class AssetLifecycleHandlerTests
{
    private readonly IAssetRepository _assetRepository = Substitute.For<IAssetRepository>();
    private readonly IOfficeRepository _officeRepository = Substitute.For<IOfficeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _clock = TestHarness.FrozenClock();
    private readonly IDomainEventDispatcher _domainEventDispatcher = Substitute.For<IDomainEventDispatcher>();

    private void ArrangeAsset(Asset asset) =>
        _assetRepository.GetByIdAsync(asset.Id, Arg.Any<CancellationToken>()).Returns(asset);

    private void ArrangeOfficeExists() =>
        _officeRepository
            .GetByIdAsync(Arg.Any<OfficeId>(), Arg.Any<CancellationToken>())
            .Returns(Office.Create("New York Site", "ASTNYC", null).GetValueOrThrow());

    [Fact]
    public async Task ReturnAsset_WithAssignedAsset_ReturnsToStockAndDispatchesEvent()
    {
        var asset = AssetFactory.AssignedAsset();
        ArrangeAsset(asset);
        var handler = new ReturnAssetHandler(_assetRepository, _unitOfWork, _clock, _domainEventDispatcher);

        var result = await handler.HandleAsync(
            new ReturnAssetCommand(asset.Id),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(AssetStatus.InStock, asset.Status);
        Assert.Equal(TestHarness.FixedNow, asset.Assignments[0].ReturnedAtUtc);
        await _assetRepository.Received(1).UpdateAsync(asset, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _domainEventDispatcher
            .Received(1)
            .DispatchAsync(Arg.Is<IReadOnlyList<IDomainEvent>>(events => events.Single() is AssetReturnedDomainEvent), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnAsset_WithUnknownAsset_ReturnsAssetNotFound()
    {
        _assetRepository
            .GetByIdAsync(Arg.Any<AssetId>(), Arg.Any<CancellationToken>())
            .Returns((Asset?)null);
        var handler = new ReturnAssetHandler(_assetRepository, _unitOfWork, _clock, _domainEventDispatcher);

        var result = await handler.HandleAsync(new ReturnAssetCommand(AssetId.New()), TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(AssetErrors.NotFound.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnAsset_WithUnassignedAsset_MapsNotAssignedConflict()
    {
        var asset = AssetFactory.NewAsset();
        ArrangeAsset(asset);
        var handler = new ReturnAssetHandler(_assetRepository, _unitOfWork, _clock, _domainEventDispatcher);

        var result = await handler.HandleAsync(new ReturnAssetCommand(asset.Id), TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(AssetErrors.NotAssigned.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartMaintenance_WithInStockAsset_MovesToMaintenance()
    {
        var asset = AssetFactory.NewAsset();
        ArrangeAsset(asset);
        var handler = new StartMaintenanceHandler(_assetRepository, _unitOfWork, _clock);

        var result = await handler.HandleAsync(new StartMaintenanceCommand(asset.Id), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(AssetStatus.Maintenance, asset.Status);
        await _assetRepository.Received(1).UpdateAsync(asset, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartMaintenance_WithUnknownAsset_ReturnsAssetNotFound()
    {
        _assetRepository
            .GetByIdAsync(Arg.Any<AssetId>(), Arg.Any<CancellationToken>())
            .Returns((Asset?)null);
        var handler = new StartMaintenanceHandler(_assetRepository, _unitOfWork, _clock);

        var result = await handler.HandleAsync(new StartMaintenanceCommand(AssetId.New()), TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(AssetErrors.NotFound.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task StartMaintenance_WithRetiredAsset_MapsCannotStartMaintenanceRetired()
    {
        var asset = AssetFactory.RetiredAsset();
        ArrangeAsset(asset);
        var handler = new StartMaintenanceHandler(_assetRepository, _unitOfWork, _clock);

        var result = await handler.HandleAsync(new StartMaintenanceCommand(asset.Id), TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(AssetErrors.CannotStartMaintenanceRetired.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
    }

    [Fact]
    public async Task ResumeFromMaintenance_WithAssetInMaintenance_ReturnsToStock()
    {
        var asset = AssetFactory.InStatus(AssetStatus.Maintenance);
        ArrangeAsset(asset);
        var handler = new ResumeFromMaintenanceHandler(_assetRepository, _unitOfWork);

        var result = await handler.HandleAsync(new ResumeFromMaintenanceCommand(asset.Id), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(AssetStatus.InStock, asset.Status);
        await _assetRepository.Received(1).UpdateAsync(asset, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResumeFromMaintenance_WithUnknownAsset_ReturnsAssetNotFound()
    {
        _assetRepository
            .GetByIdAsync(Arg.Any<AssetId>(), Arg.Any<CancellationToken>())
            .Returns((Asset?)null);
        var handler = new ResumeFromMaintenanceHandler(_assetRepository, _unitOfWork);

        var result = await handler.HandleAsync(new ResumeFromMaintenanceCommand(AssetId.New()), TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(AssetErrors.NotFound.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task ResumeFromMaintenance_WithAssetNotInMaintenance_MapsNotInMaintenance()
    {
        var asset = AssetFactory.AssignedAsset();
        ArrangeAsset(asset);
        var handler = new ResumeFromMaintenanceHandler(_assetRepository, _unitOfWork);

        var result = await handler.HandleAsync(new ResumeFromMaintenanceCommand(asset.Id), TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(AssetErrors.NotInMaintenance.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
    }

    [Fact]
    public async Task RetireAsset_WithAssignedAsset_ClosesAssignmentAndDispatchesRetiredEvent()
    {
        var asset = AssetFactory.AssignedAsset();
        ArrangeAsset(asset);
        var handler = new RetireAssetHandler(_assetRepository, _unitOfWork, _clock, _domainEventDispatcher);

        var result = await handler.HandleAsync(new RetireAssetCommand(asset.Id), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(AssetStatus.Retired, asset.Status);
        Assert.Equal(TestHarness.FixedNow, asset.Assignments[0].ReturnedAtUtc);
        await _assetRepository.Received(1).UpdateAsync(asset, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _domainEventDispatcher
            .Received(1)
            .DispatchAsync(Arg.Is<IReadOnlyList<IDomainEvent>>(events => events.Single() is AssetRetiredDomainEvent), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetireAsset_WithUnknownAsset_ReturnsAssetNotFound()
    {
        _assetRepository
            .GetByIdAsync(Arg.Any<AssetId>(), Arg.Any<CancellationToken>())
            .Returns((Asset?)null);
        var handler = new RetireAssetHandler(_assetRepository, _unitOfWork, _clock, _domainEventDispatcher);

        var result = await handler.HandleAsync(new RetireAssetCommand(AssetId.New()), TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(AssetErrors.NotFound.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        await _domainEventDispatcher.DidNotReceive().DispatchAsync(Arg.Any<IReadOnlyList<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetireAsset_WhenAlreadyRetired_MapsAlreadyRetired()
    {
        var asset = AssetFactory.RetiredAsset();
        ArrangeAsset(asset);
        var handler = new RetireAssetHandler(_assetRepository, _unitOfWork, _clock, _domainEventDispatcher);

        var result = await handler.HandleAsync(new RetireAssetCommand(asset.Id), TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(AssetErrors.AlreadyRetired.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
    }

    [Fact]
    public async Task DisposeAsset_WithRetiredAsset_Disposes()
    {
        var asset = AssetFactory.RetiredAsset();
        ArrangeAsset(asset);
        var handler = new DisposeAssetHandler(_assetRepository, _unitOfWork);

        var result = await handler.HandleAsync(new DisposeAssetCommand(asset.Id), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(AssetStatus.Disposed, asset.Status);
        await _assetRepository.Received(1).UpdateAsync(asset, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisposeAsset_WithUnknownAsset_ReturnsAssetNotFound()
    {
        _assetRepository
            .GetByIdAsync(Arg.Any<AssetId>(), Arg.Any<CancellationToken>())
            .Returns((Asset?)null);
        var handler = new DisposeAssetHandler(_assetRepository, _unitOfWork);

        var result = await handler.HandleAsync(new DisposeAssetCommand(AssetId.New()), TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(AssetErrors.NotFound.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task DisposeAsset_WithInStockAsset_MapsNotRetiredConflict()
    {
        var asset = AssetFactory.NewAsset();
        ArrangeAsset(asset);
        var handler = new DisposeAssetHandler(_assetRepository, _unitOfWork);

        var result = await handler.HandleAsync(new DisposeAssetCommand(asset.Id), TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(AssetErrors.NotRetired.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        await _assetRepository.DidNotReceive().UpdateAsync(Arg.Any<Asset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransferAsset_WithExistingTargetOffice_MovesAsset()
    {
        var asset = AssetFactory.NewAsset();
        ArrangeAsset(asset);
        var target = OfficeId.New();
        _officeRepository.GetByIdAsync(target, Arg.Any<CancellationToken>())
            .Returns(Office.Create("Boston Site", "ASTBOS", null).GetValueOrThrow());
        var handler = new TransferAssetHandler(_assetRepository, _officeRepository, _unitOfWork);

        var result = await handler.HandleAsync(
            new TransferAssetCommand(asset.Id, target),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(target, asset.OfficeId);
        await _assetRepository.Received(1).UpdateAsync(asset, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransferAsset_WithUnknownAsset_ReturnsAssetNotFound()
    {
        _assetRepository
            .GetByIdAsync(Arg.Any<AssetId>(), Arg.Any<CancellationToken>())
            .Returns((Asset?)null);
        ArrangeOfficeExists();
        var handler = new TransferAssetHandler(_assetRepository, _officeRepository, _unitOfWork);

        var result = await handler.HandleAsync(
            new TransferAssetCommand(AssetId.New(), OfficeId.New()),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(AssetErrors.NotFound.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task TransferAsset_WithUnknownTargetOffice_ReturnsOfficeNotFound()
    {
        var asset = AssetFactory.NewAsset();
        ArrangeAsset(asset);
        _officeRepository
            .GetByIdAsync(Arg.Any<OfficeId>(), Arg.Any<CancellationToken>())
            .Returns((Office?)null);
        var handler = new TransferAssetHandler(_assetRepository, _officeRepository, _unitOfWork);

        var result = await handler.HandleAsync(
            new TransferAssetCommand(asset.Id, OfficeId.New()),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(OfficeErrors.NotFound.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task TransferAsset_ToCurrentOffice_MapsAlreadyInTargetOffice()
    {
        var asset = AssetFactory.NewAsset();
        ArrangeAsset(asset);
        _officeRepository.GetByIdAsync(asset.OfficeId, Arg.Any<CancellationToken>())
            .Returns(Office.Create("New York Site", "ASTNYC", null).GetValueOrThrow());
        var handler = new TransferAssetHandler(_assetRepository, _officeRepository, _unitOfWork);

        var result = await handler.HandleAsync(
            new TransferAssetCommand(asset.Id, asset.OfficeId),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(AssetErrors.AlreadyInTargetOffice.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
    }

    [Fact]
    public async Task TransferAsset_WithDisposedAsset_MapsCannotTransferDisposed()
    {
        var asset = AssetFactory.InStatus(AssetStatus.Disposed);
        ArrangeAsset(asset);
        ArrangeOfficeExists();
        var handler = new TransferAssetHandler(_assetRepository, _officeRepository, _unitOfWork);

        var result = await handler.HandleAsync(
            new TransferAssetCommand(asset.Id, OfficeId.New()),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(AssetErrors.CannotTransferDisposed.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
