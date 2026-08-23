using AssetLite.Domain.Assets;
using AssetLite.Domain.Common;
using AssetLite.Domain.Enums;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using AssetLite.Domain.ValueObjects;
using Xunit;

namespace AssetLite.Domain.UnitTests.Assets;

/// <summary>Shared factory helpers for asset aggregate tests.</summary>
public abstract class AssetTestBase
{
    protected static readonly DateTimeOffset T0 = new(2026, 1, 10, 9, 0, 0, TimeSpan.Zero);
    protected static readonly DateTimeOffset T1 = new(2026, 2, 1, 9, 0, 0, TimeSpan.Zero);
    protected static readonly DateTimeOffset T2 = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
    protected static readonly DateTimeOffset T3 = new(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);

    protected static AssetTag Tag(int number) => AssetTag.FromNumber(number).GetValueOrThrow();

    protected static DomainResult<Asset> CreateResult(
        string name = "Dell Latitude 5540",
        AssetTag? tag = null,
        CategoryId? categoryId = null,
        OfficeId? officeId = null,
        string? manufacturer = null,
        string? model = null,
        string? serialNumber = null,
        string? notes = null,
        Money? purchaseCost = null,
        AssetId? id = null) =>
        Asset.Create(
            tag ?? Tag(1),
            categoryId ?? CategoryId.New(),
            officeId ?? OfficeId.New(),
            name,
            AssetCondition.Good,
            T0,
            manufacturer,
            model,
            serialNumber,
            purchaseDate: null,
            purchaseCost,
            notes,
            id);

    protected static Asset CreateAsset() => CreateResult().GetValueOrThrow();

    protected static Asset CreateInStatus(AssetStatus status)
    {
        var asset = CreateAsset();
        switch (status)
        {
            case AssetStatus.InStock:
                break;
            case AssetStatus.Assigned:
                asset.AssignTo("Sarah Chen", "sarah.chen@assetlite.example", T1);
                break;
            case AssetStatus.Maintenance:
                asset.StartMaintenance(T1);
                break;
            case AssetStatus.Retired:
                asset.Retire(T1);
                break;
            case AssetStatus.Disposed:
                asset.Retire(T1);
                asset.Dispose();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }

        asset.PullEvents(); // discard events raised while building the requested status
        return asset;
    }
}

/// <summary>Unit tests for the <see cref="Asset"/> factory method.</summary>
public sealed class AssetCreateTests : AssetTestBase
{
    [Fact]
    public void Create_WithValidShape_ReturnsInStockAsset()
    {
        var id = AssetId.New();
        var categoryId = CategoryId.New();
        var officeId = OfficeId.New();
        var cost = Money.Create(1149.99m).GetValueOrThrow();

        var result = CreateResult(
            "  Dell Latitude 5540  ",
            categoryId: categoryId,
            officeId: officeId,
            manufacturer: "  Dell  ",
            model: " Latitude 5540 ",
            serialNumber: " 5CG1430ZQ2 ",
            notes: "  Front desk laptop.  ",
            purchaseCost: cost,
            id: id);

        Assert.True(result.IsSuccess);
        var asset = result.GetValueOrThrow();
        Assert.Equal(id, asset.Id);
        Assert.Equal(Tag(1), asset.Tag);
        Assert.Equal(categoryId, asset.CategoryId);
        Assert.Equal(officeId, asset.OfficeId);
        Assert.Equal("Dell Latitude 5540", asset.Name);
        Assert.Equal("Dell", asset.Manufacturer);
        Assert.Equal("Latitude 5540", asset.Model);
        Assert.Equal("5CG1430ZQ2", asset.SerialNumber);
        Assert.Equal("Front desk laptop.", asset.Notes);
        Assert.Equal(AssetStatus.InStock, asset.Status);
        Assert.Equal(AssetCondition.Good, asset.Condition);
        Assert.Equal(cost, asset.PurchaseCost);
        Assert.Equal(T0, asset.CreatedAtUtc);
        Assert.Empty(asset.Assignments);
        Assert.Null(asset.OpenAssignment);
        Assert.Empty(asset.PullEvents());
    }

    [Fact]
    public void Create_WithWhitespaceOnlyOptionalFields_NormalizesThemToNull()
    {
        var result = CreateResult(manufacturer: "   ", model: "", serialNumber: "   ", notes: "  ");

        Assert.True(result.IsSuccess);
        var asset = result.GetValueOrThrow();
        Assert.Null(asset.Manufacturer);
        Assert.Null(asset.Model);
        Assert.Null(asset.SerialNumber);
        Assert.Null(asset.Notes);
    }

    [Fact]
    public void Create_WithoutId_GeneratesNewId()
    {
        var asset = CreateResult().GetValueOrThrow();

        Assert.False(asset.Id.IsEmpty);
    }

    [Fact]
    public void Create_WithNullTag_ReturnsInvalidTagError()
    {
        var result = Asset.Create(
            tag: null!,
            CategoryId.New(),
            OfficeId.New(),
            "Dell Latitude 5540",
            AssetCondition.Good,
            T0);

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.InvalidTag, result.Error);
    }

    [Fact]
    public void Create_WithEmptyCategoryId_ReturnsEmptyCategoryIdError()
    {
        var result = CreateResult(categoryId: new CategoryId(Guid.Empty));

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.EmptyCategoryId, result.Error);
    }

    [Fact]
    public void Create_WithEmptyOfficeId_ReturnsEmptyOfficeIdError()
    {
        var result = CreateResult(officeId: new OfficeId(Guid.Empty));

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.EmptyOfficeId, result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ReturnsInvalidNameError(string? name)
    {
        var result = CreateResult(name: name!);

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.InvalidName, result.Error);
    }

    [Fact]
    public void Create_WithNameLongerThanMaxLength_ReturnsInvalidNameError()
    {
        var result = CreateResult(name: new string('N', Asset.NameMaxLength + 1));

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.InvalidName, result.Error);
    }

    [Theory]
    [InlineData("manufacturer")]
    [InlineData("model")]
    [InlineData("serial")]
    public void Create_WithMetadataFieldLongerThanMaxLength_ReturnsInvalidMetadataError(string field)
    {
        var tooLong = new string('M', Asset.MetadataMaxLength + 1);

        var result = CreateResult(
            manufacturer: field == "manufacturer" ? tooLong : null,
            model: field == "model" ? tooLong : null,
            serialNumber: field == "serial" ? tooLong : null);

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.InvalidMetadata, result.Error);
    }

    [Fact]
    public void Create_WithNotesLongerThanMaxLength_ReturnsInvalidNotesError()
    {
        var result = CreateResult(notes: new string('X', Asset.NotesMaxLength + 1));

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.InvalidNotes, result.Error);
    }
}

/// <summary>Unit tests for <see cref="Asset.AssignTo"/> and <see cref="Asset.ReturnToStock"/>.</summary>
public sealed class AssetAssignmentTests : AssetTestBase
{
    [Fact]
    public void AssignTo_FromInStock_AssignsAssetAndRaisesEvent()
    {
        var asset = CreateAsset();

        var result = asset.AssignTo("  Sarah Chen  ", "sarah.chen@assetlite.example", T1);

        Assert.True(result.IsSuccess);
        Assert.Equal(AssetStatus.Assigned, asset.Status);
        var assignment = Assert.Single(asset.Assignments);
        Assert.True(assignment.IsOpen);
        Assert.Null(assignment.ReturnedAtUtc);
        Assert.Equal("Sarah Chen", assignment.AssigneeName);
        Assert.Equal("sarah.chen@assetlite.example", assignment.AssigneeEmail);
        Assert.Equal(T1, assignment.AssignedAtUtc);
        Assert.Equal(assignment, asset.OpenAssignment);

        var raised = Assert.Single(asset.PullEvents());
        var assigned = Assert.IsType<AssetAssignedDomainEvent>(raised);
        Assert.Equal(asset.Id, assigned.AssetId);
        Assert.Equal(asset.Tag, assigned.Tag);
        Assert.Equal("Sarah Chen", assigned.AssigneeName);
        Assert.Equal("sarah.chen@assetlite.example", assigned.AssigneeEmail);
        Assert.Equal(T1, assigned.AssignedAtUtc);
    }

    [Fact]
    public void AssignTo_FromAssigned_ReassignsAndClosesPriorAssignment()
    {
        var asset = CreateAsset();
        asset.AssignTo("Sarah Chen", "sarah.chen@assetlite.example", T1);

        var result = asset.AssignTo("Marcus Webb", "marcus.webb@assetlite.example", T2);

        Assert.True(result.IsSuccess);
        Assert.Equal(AssetStatus.Assigned, asset.Status);
        Assert.Equal(2, asset.Assignments.Count);

        var prior = asset.Assignments[0];
        Assert.False(prior.IsOpen);
        Assert.Equal(T2, prior.ReturnedAtUtc); // closed at the reassignment moment
        Assert.Equal("Sarah Chen", prior.AssigneeName);

        var current = asset.Assignments[1];
        Assert.True(current.IsOpen);
        Assert.Equal("Marcus Webb", current.AssigneeName);
        Assert.Equal(current, asset.OpenAssignment);

        var events = asset.PullEvents();
        Assert.Equal(2, events.Count); // one per AssignTo call
        Assert.All(events, domainEvent => Assert.IsType<AssetAssignedDomainEvent>(domainEvent));
    }

    [Theory]
    [InlineData(AssetStatus.Maintenance, "Asset.CannotAssignMaintenance")]
    [InlineData(AssetStatus.Retired, "Asset.CannotAssignRetired")]
    [InlineData(AssetStatus.Disposed, "Asset.CannotAssignDisposed")]
    public void AssignTo_FromNonAssignableStatus_ReturnsTypedErrorAndKeepsState(AssetStatus status, string expectedCode)
    {
        var asset = CreateInStatus(status);

        var result = asset.AssignTo("Sarah Chen", "sarah.chen@assetlite.example", T2);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error!.Code);
        Assert.Equal(status, asset.Status);
        Assert.Empty(asset.PullEvents());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AssignTo_WithEmptyAssigneeName_ReturnsInvalidAssigneeNameError(string? name)
    {
        var asset = CreateAsset();

        var result = asset.AssignTo(name!, "sarah.chen@assetlite.example", T1);

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.InvalidAssigneeName, result.Error);
        Assert.Equal(AssetStatus.InStock, asset.Status);
        Assert.Empty(asset.Assignments);
    }

    [Fact]
    public void AssignTo_WithAssigneeNameLongerThanMaxLength_ReturnsInvalidAssigneeNameError()
    {
        var asset = CreateAsset();

        var result = asset.AssignTo(new string('S', Asset.AssigneeNameMaxLength + 1), "sarah.chen@assetlite.example", T1);

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.InvalidAssigneeName, result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sarah.chen.assetlite.example")] // missing @
    [InlineData("@assetlite.example")]          // missing local part
    [InlineData("sarah.chen@")]                 // missing domain
    [InlineData("sarah@assetlite@example.com")] // two @ signs
    public void AssignTo_WithInvalidEmail_ReturnsInvalidAssigneeEmailError(string? email)
    {
        var asset = CreateAsset();

        var result = asset.AssignTo("Sarah Chen", email!, T1);

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.InvalidAssigneeEmail, result.Error);
        Assert.Equal(AssetStatus.InStock, asset.Status);
        Assert.Empty(asset.Assignments);
    }

    [Fact]
    public void ReturnToStock_FromAssigned_ClosesAssignmentAndRaisesEvent()
    {
        var asset = CreateAsset();
        asset.AssignTo("Sarah Chen", "sarah.chen@assetlite.example", T1);
        asset.PullEvents(); // discard the assignment event; the return is under test

        var result = asset.ReturnToStock(T2);

        Assert.True(result.IsSuccess);
        Assert.Equal(AssetStatus.InStock, asset.Status);
        var assignment = Assert.Single(asset.Assignments);
        Assert.False(assignment.IsOpen);
        Assert.Equal(T2, assignment.ReturnedAtUtc);
        Assert.Null(asset.OpenAssignment);

        var raised = Assert.Single(asset.PullEvents());
        var returned = Assert.IsType<AssetReturnedDomainEvent>(raised);
        Assert.Equal(asset.Id, returned.AssetId);
        Assert.Equal(asset.Tag, returned.Tag);
        Assert.Equal(T2, returned.ReturnedAtUtc);
    }

    [Fact]
    public void ReturnToStock_FromInStock_ReturnsNotAssignedError()
    {
        var asset = CreateAsset();

        var result = asset.ReturnToStock(T1);

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.NotAssigned, result.Error);
        Assert.Equal(AssetStatus.InStock, asset.Status);
        Assert.Empty(asset.PullEvents());
    }

    [Fact]
    public void ReturnToStock_AfterAlreadyReturned_ReturnsNotAssignedError()
    {
        var asset = CreateAsset();
        asset.AssignTo("Sarah Chen", "sarah.chen@assetlite.example", T1);
        asset.ReturnToStock(T2);

        var result = asset.ReturnToStock(T3);

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.NotAssigned, result.Error);
        Assert.Equal(AssetStatus.InStock, asset.Status);
    }

    [Theory]
    [InlineData(AssetStatus.Maintenance)]
    [InlineData(AssetStatus.Retired)]
    [InlineData(AssetStatus.Disposed)]
    public void ReturnToStock_FromNonAssignedStatus_ReturnsNotAssignedError(AssetStatus status)
    {
        var asset = CreateInStatus(status);
        asset.PullEvents();

        var result = asset.ReturnToStock(T2);

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.NotAssigned, result.Error);
    }

    [Fact]
    public void PullEvents_ReturnsEventsOnceAndClearsTheBuffer()
    {
        var asset = CreateAsset();
        asset.AssignTo("Sarah Chen", "sarah.chen@assetlite.example", T1);

        Assert.Single(asset.PullEvents());
        Assert.Empty(asset.PullEvents());
    }
}

/// <summary>Unit tests for the maintenance / retirement / disposal state machine.</summary>
public sealed class AssetLifecycleTests : AssetTestBase
{
    [Fact]
    public void StartMaintenance_FromInStock_MovesAssetToMaintenance()
    {
        var asset = CreateAsset();

        var result = asset.StartMaintenance(T1);

        Assert.True(result.IsSuccess);
        Assert.Equal(AssetStatus.Maintenance, asset.Status);
        Assert.Empty(asset.Assignments);
        Assert.Empty(asset.PullEvents()); // maintenance raises no domain event
    }

    [Fact]
    public void StartMaintenance_FromAssigned_ClosesOpenAssignment()
    {
        var asset = CreateAsset();
        asset.AssignTo("Sarah Chen", "sarah.chen@assetlite.example", T1);

        var result = asset.StartMaintenance(T2);

        Assert.True(result.IsSuccess);
        Assert.Equal(AssetStatus.Maintenance, asset.Status);
        var assignment = Assert.Single(asset.Assignments);
        Assert.False(assignment.IsOpen);
        Assert.Equal(T2, assignment.ReturnedAtUtc);
        Assert.Null(asset.OpenAssignment);
    }

    [Fact]
    public void StartMaintenance_WhileAlreadyInMaintenance_ReturnsAlreadyInMaintenanceError()
    {
        var asset = CreateInStatus(AssetStatus.Maintenance);

        var result = asset.StartMaintenance(T2);

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.AlreadyInMaintenance, result.Error);
        Assert.Equal(AssetStatus.Maintenance, asset.Status);
    }

    [Theory]
    [InlineData(AssetStatus.Retired, "Asset.CannotStartMaintenanceRetired")]
    [InlineData(AssetStatus.Disposed, "Asset.CannotStartMaintenanceDisposed")]
    public void StartMaintenance_FromTerminalStatus_ReturnsTypedError(AssetStatus status, string expectedCode)
    {
        var asset = CreateInStatus(status);

        var result = asset.StartMaintenance(T2);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error!.Code);
        Assert.Equal(status, asset.Status);
    }

    [Fact]
    public void ResumeFromMaintenance_FromMaintenance_ReturnsAssetToStock()
    {
        var asset = CreateInStatus(AssetStatus.Maintenance);

        var result = asset.ResumeFromMaintenance();

        Assert.True(result.IsSuccess);
        Assert.Equal(AssetStatus.InStock, asset.Status);
    }

    [Theory]
    [InlineData(AssetStatus.InStock)]
    [InlineData(AssetStatus.Assigned)]
    [InlineData(AssetStatus.Retired)]
    [InlineData(AssetStatus.Disposed)]
    public void ResumeFromMaintenance_FromAnyOtherStatus_ReturnsNotInMaintenanceError(AssetStatus status)
    {
        var asset = CreateInStatus(status);

        var result = asset.ResumeFromMaintenance();

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.NotInMaintenance, result.Error);
        Assert.Equal(status, asset.Status);
    }

    [Theory]
    [InlineData(AssetStatus.InStock)]
    [InlineData(AssetStatus.Assigned)]
    [InlineData(AssetStatus.Maintenance)]
    public void Retire_FromAnyLiveStatus_MovesAssetToRetiredAndRaisesEvent(AssetStatus status)
    {
        var asset = CreateInStatus(status);
        asset.PullEvents();
        var hadOpenAssignment = asset.OpenAssignment is not null;

        var result = asset.Retire(T2);

        Assert.True(result.IsSuccess);
        Assert.Equal(AssetStatus.Retired, asset.Status);
        Assert.Null(asset.OpenAssignment);
        if (hadOpenAssignment)
        {
            Assert.All(asset.Assignments, assignment => Assert.Equal(T2, assignment.ReturnedAtUtc));
        }

        var raised = Assert.Single(asset.PullEvents());
        var retired = Assert.IsType<AssetRetiredDomainEvent>(raised);
        Assert.Equal(asset.Id, retired.AssetId);
        Assert.Equal(asset.Tag, retired.Tag);
        Assert.Equal(T2, retired.RetiredAtUtc);
    }

    [Fact]
    public void Retire_FromAssigned_ClosesOpenAssignment()
    {
        var asset = CreateAsset();
        asset.AssignTo("Sarah Chen", "sarah.chen@assetlite.example", T1);

        var result = asset.Retire(T2);

        Assert.True(result.IsSuccess);
        var assignment = Assert.Single(asset.Assignments);
        Assert.False(assignment.IsOpen);
        Assert.Equal(T2, assignment.ReturnedAtUtc);
    }

    [Fact]
    public void Retire_WhenAlreadyRetired_ReturnsAlreadyRetiredError()
    {
        var asset = CreateInStatus(AssetStatus.Retired);
        asset.PullEvents();

        var result = asset.Retire(T2);

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.AlreadyRetired, result.Error);
        Assert.Empty(asset.PullEvents());
    }

    [Fact]
    public void Retire_FromDisposed_ReturnsCannotRetireDisposedError()
    {
        var asset = CreateInStatus(AssetStatus.Disposed);

        var result = asset.Retire(T2);

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.CannotRetireDisposed, result.Error);
        Assert.Equal(AssetStatus.Disposed, asset.Status);
    }

    [Fact]
    public void Dispose_FromRetired_MovesAssetToDisposed()
    {
        var asset = CreateInStatus(AssetStatus.Retired);

        var result = asset.Dispose();

        Assert.True(result.IsSuccess);
        Assert.Equal(AssetStatus.Disposed, asset.Status);
        Assert.Empty(asset.PullEvents()); // disposal raises no domain event
    }

    [Theory]
    [InlineData(AssetStatus.InStock)]
    [InlineData(AssetStatus.Assigned)]
    [InlineData(AssetStatus.Maintenance)]
    [InlineData(AssetStatus.Disposed)]
    public void Dispose_FromAnyStatusOtherThanRetired_ReturnsNotRetiredError(AssetStatus status)
    {
        var asset = CreateInStatus(status);

        var result = asset.Dispose();

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.NotRetired, result.Error);
        Assert.Equal(status, asset.Status);
    }
}

/// <summary>Unit tests for <see cref="Asset.TransferTo"/>.</summary>
public sealed class AssetTransferTests : AssetTestBase
{
    [Fact]
    public void TransferTo_WithValidTarget_MovesAssetToTargetOffice()
    {
        var asset = CreateAsset();
        var target = OfficeId.New();

        var result = asset.TransferTo(target, targetIsValid: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(target, asset.OfficeId);
        Assert.Empty(asset.PullEvents()); // transfers raise no domain event
    }

    [Fact]
    public void TransferTo_WhileAssigned_StillSucceeds()
    {
        var asset = CreateInStatus(AssetStatus.Assigned);
        var target = OfficeId.New();

        var result = asset.TransferTo(target, targetIsValid: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(target, asset.OfficeId);
        Assert.NotNull(asset.OpenAssignment); // assignment is unaffected
    }

    [Fact]
    public void TransferTo_WithUnverifiedTarget_ReturnsInvalidTargetOfficeError()
    {
        var asset = CreateAsset();

        var result = asset.TransferTo(OfficeId.New(), targetIsValid: false);

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.InvalidTargetOffice, result.Error);
    }

    [Fact]
    public void TransferTo_WithEmptyTargetId_ReturnsInvalidTargetOfficeError()
    {
        var asset = CreateAsset();
        var originalOffice = asset.OfficeId;

        var result = asset.TransferTo(new OfficeId(Guid.Empty), targetIsValid: true);

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.InvalidTargetOffice, result.Error);
        Assert.Equal(originalOffice, asset.OfficeId);
    }

    [Fact]
    public void TransferTo_ToCurrentOffice_ReturnsAlreadyInTargetOfficeError()
    {
        var asset = CreateAsset();

        var result = asset.TransferTo(asset.OfficeId, targetIsValid: true);

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.AlreadyInTargetOffice, result.Error);
    }

    [Fact]
    public void TransferTo_FromDisposed_ReturnsCannotTransferDisposedError()
    {
        var asset = CreateInStatus(AssetStatus.Disposed);
        var target = OfficeId.New();

        var result = asset.TransferTo(target, targetIsValid: true);

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.CannotTransferDisposed, result.Error);
        Assert.NotEqual(target, asset.OfficeId);
    }
}

/// <summary>Unit tests for <see cref="Asset.UpdateDetails"/>.</summary>
public sealed class AssetUpdateDetailsTests : AssetTestBase
{
    [Fact]
    public void UpdateDetails_WithChangedFields_MutatesAssetAndRaisesEvent()
    {
        var asset = CreateAsset();
        var newCategoryId = CategoryId.New();
        var cost = Money.Create(1499.00m, "EUR").GetValueOrThrow();
        var purchaseDate = new DateOnly(2025, 6, 1);

        var result = asset.UpdateDetails(
            "  Dell Latitude 5550  ",
            newCategoryId,
            AssetCondition.Fair,
            purchaseDate,
            cost,
            T2,
            manufacturer: "  Dell  ",
            model: "Latitude 5550",
            serialNumber: "   ",
            notes: "  Replaced the 5540.  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Dell Latitude 5550", asset.Name);
        Assert.Equal(newCategoryId, asset.CategoryId);
        Assert.Equal(AssetCondition.Fair, asset.Condition);
        Assert.Equal(purchaseDate, asset.PurchaseDate);
        Assert.Equal(cost, asset.PurchaseCost);
        Assert.Equal("Dell", asset.Manufacturer);
        Assert.Equal("Latitude 5550", asset.Model);
        Assert.Null(asset.SerialNumber); // whitespace-only input normalizes to null
        Assert.Equal("Replaced the 5540.", asset.Notes);

        var @event = Assert.IsType<AssetDetailsUpdatedDomainEvent>(Assert.Single(asset.PullEvents()));
        Assert.Equal(asset.Id, @event.AssetId);
        Assert.Equal(asset.Tag, @event.Tag);
        Assert.Equal(T2, @event.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateDetails_WithIdenticalValues_SucceedsWithoutRaisingEvent()
    {
        var asset = CreateAsset();

        var result = asset.UpdateDetails(asset.Name, asset.CategoryId, AssetCondition.Good, purchaseDate: null, purchaseCost: null, updatedAtUtc: T2);

        Assert.True(result.IsSuccess);
        Assert.Empty(asset.PullEvents());
    }

    [Fact]
    public void UpdateDetails_KeepsTagOfficeAndStatusUntouched()
    {
        var asset = CreateAsset();
        var officeId = asset.OfficeId;
        var status = asset.Status;

        var result = asset.UpdateDetails("Renamed asset", CategoryId.New(), AssetCondition.Good, null, null, T2);

        Assert.True(result.IsSuccess);
        Assert.Equal(Tag(1), asset.Tag);
        Assert.Equal(officeId, asset.OfficeId);
        Assert.Equal(status, asset.Status);
    }

    [Fact]
    public void UpdateDetails_OnDisposedAsset_FailsWithTypedError()
    {
        var asset = CreateInStatus(AssetStatus.Disposed);

        var result = asset.UpdateDetails("New name", asset.CategoryId, AssetCondition.Good, null, null, T2);

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.CannotUpdateDisposed, result.Error);
        Assert.Equal("Dell Latitude 5540", asset.Name);
    }

    [Fact]
    public void UpdateDetails_OnRetiredAsset_SucceedsAsRecordCorrection()
    {
        var asset = CreateInStatus(AssetStatus.Retired);

        var result = asset.UpdateDetails("Corrected name", asset.CategoryId, AssetCondition.Poor, null, null, T2);

        Assert.True(result.IsSuccess);
        Assert.Equal("Corrected name", asset.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_WithBlankName_FailsWithInvalidName(string name)
    {
        var asset = CreateAsset();

        var result = asset.UpdateDetails(name, asset.CategoryId, AssetCondition.Good, null, null, T2);

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.InvalidName, result.Error);
    }

    [Fact]
    public void UpdateDetails_WithOverlongName_FailsWithInvalidName()
    {
        var asset = CreateAsset();

        var result = asset.UpdateDetails(new string('n', Asset.NameMaxLength + 1), asset.CategoryId, AssetCondition.Good, null, null, T2);

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.InvalidName, result.Error);
    }

    [Fact]
    public void UpdateDetails_WithOverlongMetadata_FailsWithInvalidMetadata()
    {
        var asset = CreateAsset();

        var result = asset.UpdateDetails("Valid name", asset.CategoryId, AssetCondition.Good, null, null, T2, manufacturer: new string('m', Asset.MetadataMaxLength + 1));

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.InvalidMetadata, result.Error);
    }

    [Fact]
    public void UpdateDetails_WithOverlongNotes_FailsWithInvalidNotes()
    {
        var asset = CreateAsset();

        var result = asset.UpdateDetails("Valid name", asset.CategoryId, AssetCondition.Good, null, null, T2, notes: new string('x', Asset.NotesMaxLength + 1));

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.InvalidNotes, result.Error);
    }

    [Fact]
    public void UpdateDetails_WithEmptyCategoryId_Fails()
    {
        var asset = CreateAsset();

        var result = asset.UpdateDetails("Valid name", new CategoryId(Guid.Empty), AssetCondition.Good, null, null, T2);

        Assert.True(result.IsFailure);
        Assert.Equal(AssetErrors.EmptyCategoryId, result.Error);
    }
}
