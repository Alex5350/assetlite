using AssetLite.Application.Abstractions;
using AssetLite.Application.Offices;
using AssetLite.Domain.Common;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using AssetLite.Domain.Offices;
using ErrorOr;
using NSubstitute;
using Xunit;

namespace AssetLite.Application.UnitTests.Offices;

/// <summary>Unit tests for <see cref="CreateOfficeHandler"/> and <see cref="MoveOfficeHandler"/>.</summary>
public sealed class OfficeCommandHandlerTests
{
    private readonly IOfficeRepository _officeRepository = Substitute.For<IOfficeRepository>();
    private readonly IOfficeHierarchy _officeHierarchy = Substitute.For<IOfficeHierarchy>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateOfficeHandler CreateCreateHandler() => new(_officeRepository, _officeHierarchy, _unitOfWork);
    private MoveOfficeHandler CreateMoveHandler() => new(_officeRepository, _officeHierarchy, _unitOfWork);

    private void ParentExists(Office parent) =>
        _officeRepository
            .GetByIdAsync(Arg.Any<OfficeId>(), Arg.Any<CancellationToken>())
            .Returns(parent);

    [Fact]
    public async Task CreateOffice_WithValidParent_ReturnsDtoAndPersists()
    {
        var parent = Office.Create("Headquarters", "ASTHQ", null).GetValueOrThrow();
        ParentExists(parent);
        _officeHierarchy
            .EnsureValidParentAsync(Arg.Any<OfficeId>(), Arg.Any<OfficeId>(), Arg.Any<CancellationToken>())
            .Returns(DomainResult.Success());
        _officeRepository
            .CodeExistsAsync(Arg.Any<string>(), Arg.Any<OfficeId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        Office? staged = null;
        _ = _officeRepository
            .AddAsync(Arg.Do<Office>(office => staged = office), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var handler = CreateCreateHandler();

        var result = await handler.HandleAsync(
            new CreateOfficeCommand("  East Region  ", "ASTEAST", parent.Id),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("East Region", result.Value.Name);
        Assert.Equal("ASTEAST", result.Value.Code);
        Assert.Equal(parent.Id, result.Value.ParentOfficeId);
        Assert.NotNull(staged);
        Assert.Equal("East Region", staged!.Name);
        await _officeRepository.Received(1).AddAsync(Arg.Any<Office>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOffice_WithoutParentWhenRootAlreadyExists_ReturnsRootAlreadyExists()
    {
        var existingRoot = Office.Create("Headquarters", "ASTHQ", null).GetValueOrThrow();
        _officeRepository.GetRootAsync(Arg.Any<CancellationToken>()).Returns(existingRoot);
        var handler = CreateCreateHandler();

        var result = await handler.HandleAsync(
            new CreateOfficeCommand("Second HQ", "ASTHQ2", null),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(OfficeErrors.RootAlreadyExists.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        await _officeRepository.DidNotReceive().AddAsync(Arg.Any<Office>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOffice_WithoutParentWhenNoRootExists_CreatesTheRootOffice()
    {
        _officeRepository.GetRootAsync(Arg.Any<CancellationToken>()).Returns((Office?)null);
        _officeRepository
            .CodeExistsAsync(Arg.Any<string>(), Arg.Any<OfficeId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var handler = CreateCreateHandler();

        var result = await handler.HandleAsync(
            new CreateOfficeCommand("Headquarters", "ASTHQ", null),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Null(result.Value.ParentOfficeId);
        await _officeRepository.Received(1).AddAsync(Arg.Any<Office>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOffice_WithUnknownParent_ReturnsOfficeNotFound()
    {
        _officeRepository
            .GetByIdAsync(Arg.Any<OfficeId>(), Arg.Any<CancellationToken>())
            .Returns((Office?)null);
        var handler = CreateCreateHandler();

        var result = await handler.HandleAsync(
            new CreateOfficeCommand("East Region", "ASTEAST", OfficeId.New()),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(OfficeErrors.NotFound.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        await _officeRepository.DidNotReceive().AddAsync(Arg.Any<Office>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOffice_WhenDepthWouldBeExceeded_ReturnsMaxDepthExceeded()
    {
        var parent = Office.Create("Server Room", "ASTSRV", OfficeId.New()).GetValueOrThrow();
        ParentExists(parent);
        _officeHierarchy
            .EnsureValidParentAsync(Arg.Any<OfficeId>(), Arg.Any<OfficeId>(), Arg.Any<CancellationToken>())
            .Returns(DomainResult.Failure(OfficeErrors.MaxDepthExceeded));
        var handler = CreateCreateHandler();

        var result = await handler.HandleAsync(
            new CreateOfficeCommand("Rack 5", "ASTRCK", parent.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(OfficeErrors.MaxDepthExceeded.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        await _officeRepository.DidNotReceive().AddAsync(Arg.Any<Office>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOffice_WithDuplicateCode_ReturnsDuplicateCode()
    {
        var parent = Office.Create("Headquarters", "ASTHQ", null).GetValueOrThrow();
        ParentExists(parent);
        _officeHierarchy
            .EnsureValidParentAsync(Arg.Any<OfficeId>(), Arg.Any<OfficeId>(), Arg.Any<CancellationToken>())
            .Returns(DomainResult.Success());
        _officeRepository
            .CodeExistsAsync("ASTEAST", Arg.Any<OfficeId?>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = CreateCreateHandler();

        var result = await handler.HandleAsync(
            new CreateOfficeCommand("East Region", "ASTEAST", parent.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(OfficeErrors.DuplicateCode.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        await _officeRepository.DidNotReceive().AddAsync(Arg.Any<Office>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOffice_WithInvalidCode_ReturnsInvalidCode()
    {
        var handler = CreateCreateHandler();

        var result = await handler.HandleAsync(
            new CreateOfficeCommand("East Region", "east", OfficeId.New()),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(OfficeErrors.InvalidCode.Code, result.FirstError.Code);
    }

    [Fact]
    public async Task MoveOffice_WithValidNewParent_ReparentsAndPersists()
    {
        var hq = Office.Create("Headquarters", "ASTHQ", null).GetValueOrThrow();
        var east = Office.Create("East Region", "ASTEAST", hq.Id).GetValueOrThrow();
        var west = Office.Create("West Region", "ASTWEST", hq.Id).GetValueOrThrow();
        _officeRepository.GetByIdAsync(east.Id, Arg.Any<CancellationToken>()).Returns(east);
        _officeRepository.GetByIdAsync(west.Id, Arg.Any<CancellationToken>()).Returns(west);
        _officeHierarchy
            .EnsureValidParentAsync(east.Id, west.Id, Arg.Any<CancellationToken>())
            .Returns(DomainResult.Success());
        var handler = CreateMoveHandler();

        var result = await handler.HandleAsync(
            new MoveOfficeCommand(east.Id, west.Id),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(west.Id, east.ParentOfficeId);
        await _officeRepository.Received(1).UpdateAsync(east, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveOffice_WithUnknownOffice_ReturnsOfficeNotFound()
    {
        _officeRepository
            .GetByIdAsync(Arg.Any<OfficeId>(), Arg.Any<CancellationToken>())
            .Returns((Office?)null);
        var handler = CreateMoveHandler();

        var result = await handler.HandleAsync(
            new MoveOfficeCommand(OfficeId.New(), OfficeId.New()),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(OfficeErrors.NotFound.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveOffice_WithUnknownTargetParent_ReturnsOfficeNotFound()
    {
        var east = Office.Create("East Region", "ASTEAST", null).GetValueOrThrow();
        _officeRepository.GetByIdAsync(east.Id, Arg.Any<CancellationToken>()).Returns(east);
        _officeRepository
            .GetByIdAsync(Arg.Is<OfficeId>(id => id != east.Id), Arg.Any<CancellationToken>())
            .Returns((Office?)null);
        var handler = CreateMoveHandler();

        var result = await handler.HandleAsync(
            new MoveOfficeCommand(east.Id, OfficeId.New()),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(OfficeErrors.NotFound.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task MoveOffice_UnderOwnDescendant_ReturnsCannotMoveUnderDescendant()
    {
        var hq = Office.Create("Headquarters", "ASTHQ", null).GetValueOrThrow();
        var east = Office.Create("East Region", "ASTEAST", hq.Id).GetValueOrThrow();
        _officeRepository.GetByIdAsync(Arg.Any<OfficeId>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.ArgAt<OfficeId>(0) == hq.Id ? hq : east);
        _officeHierarchy
            .EnsureValidParentAsync(hq.Id, east.Id, Arg.Any<CancellationToken>())
            .Returns(DomainResult.Failure(OfficeErrors.CannotMoveUnderDescendant));
        var handler = CreateMoveHandler();

        var result = await handler.HandleAsync(
            new MoveOfficeCommand(hq.Id, east.Id),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(OfficeErrors.CannotMoveUnderDescendant.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
        Assert.Null(hq.ParentOfficeId); // unchanged: reparent never ran
        await _officeRepository.DidNotReceive().UpdateAsync(Arg.Any<Office>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

/// <summary>Unit tests for <see cref="GetOfficeTreeHandler"/> and <see cref="ListOfficesHandler"/>.</summary>
public sealed class OfficeQueryHandlerTests
{
    private readonly IOfficeRepository _officeRepository = Substitute.For<IOfficeRepository>();

    [Fact]
    public async Task GetOfficeTree_WithNoOffices_ReturnsRootNotFound()
    {
        _officeRepository.ListAllAsync(Arg.Any<CancellationToken>()).Returns([]);
        var handler = new GetOfficeTreeHandler(_officeRepository);

        var result = await handler.HandleAsync(new GetOfficeTreeQuery(), TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(OfficeErrors.RootNotFound.Code, result.FirstError.Code);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task GetOfficeTree_WithMultipleRoots_ReturnsRootNotFound()
    {
        var first = Office.Create("Headquarters", "ASTHQ", null).GetValueOrThrow();
        var second = Office.Create("Second HQ", "ASTHQ2", null).GetValueOrThrow();
        _officeRepository.ListAllAsync(Arg.Any<CancellationToken>()).Returns([first, second]);
        var handler = new GetOfficeTreeHandler(_officeRepository);

        var result = await handler.HandleAsync(new GetOfficeTreeQuery(), TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Equal(OfficeErrors.RootNotFound.Code, result.FirstError.Code);
    }

    [Fact]
    public async Task GetOfficeTree_WithHierarchy_ReturnsTreeWithChildrenOrderedByName()
    {
        var hq = Office.Create("Headquarters", "ASTHQ", null).GetValueOrThrow();
        var west = Office.Create("West Region", "ASTWEST", hq.Id).GetValueOrThrow();
        var east = Office.Create("East Region", "ASTEAST", hq.Id).GetValueOrThrow();
        var nyc = Office.Create("New York Site", "ASTNYC", east.Id).GetValueOrThrow();
        _officeRepository
            .ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([nyc, west, hq, east]); // deliberately unsorted input
        var handler = new GetOfficeTreeHandler(_officeRepository);

        var result = await handler.HandleAsync(new GetOfficeTreeQuery(), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var tree = result.Value;
        Assert.Equal("Headquarters", tree.Name);
        Assert.Null(tree.ParentOfficeId);
        Assert.Equal(["East Region", "West Region"], tree.Children.Select(child => child.Name));
        var eastNode = Assert.Single(tree.Children, child => child.Name == "East Region");
        Assert.Equal(["New York Site"], eastNode.Children.Select(child => child.Name));
    }

    [Fact]
    public async Task ListOffices_ReturnsFlatListOrderedByName()
    {
        var hq = Office.Create("Headquarters", "ASTHQ", null).GetValueOrThrow();
        var west = Office.Create("West Region", "ASTWEST", hq.Id).GetValueOrThrow();
        var east = Office.Create("East Region", "ASTEAST", hq.Id).GetValueOrThrow();
        _officeRepository
            .ListAllAsync(Arg.Any<CancellationToken>())
            .Returns([west, east, hq]);
        var handler = new ListOfficesHandler(_officeRepository);

        var result = await handler.HandleAsync(new ListOfficesQuery(), TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(["East Region", "Headquarters", "West Region"], result.Value.Select(office => office.Name));
    }
}
