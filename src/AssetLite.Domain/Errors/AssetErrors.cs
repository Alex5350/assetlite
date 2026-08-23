using AssetLite.Domain.Common;

namespace AssetLite.Domain.Errors;

/// <summary>
/// Stable error catalog for the <see cref="AssetLite.Domain.Assets.Asset"/> aggregate.
/// All codes are prefixed <c>"Asset."</c>.
/// </summary>
public static class AssetErrors
{
    /// <summary>No asset with the given id exists ("Asset.NotFound").</summary>
    public static readonly DomainError NotFound = new("Asset.NotFound", "Asset was not found.");

    /// <summary>The asset tag is missing ("Asset.InvalidTag").</summary>
    public static readonly DomainError InvalidTag = new("Asset.InvalidTag", "Asset tag is required.");

    /// <summary>The category id is unset ("Asset.EmptyCategoryId").</summary>
    public static readonly DomainError EmptyCategoryId = new("Asset.EmptyCategoryId", "Category is required.");

    /// <summary>The office id is unset ("Asset.EmptyOfficeId").</summary>
    public static readonly DomainError EmptyOfficeId = new("Asset.EmptyOfficeId", "Office is required.");

    /// <summary>Asset name empty or too long ("Asset.InvalidName").</summary>
    public static readonly DomainError InvalidName = new(
        "Asset.InvalidName",
        $"Asset name is required and must be at most {AssetLite.Domain.Assets.Asset.NameMaxLength} characters.");

    /// <summary>Manufacturer/model/serial number too long ("Asset.InvalidMetadata").</summary>
    public static readonly DomainError InvalidMetadata = new(
        "Asset.InvalidMetadata",
        $"Manufacturer, model and serial number must be at most {AssetLite.Domain.Assets.Asset.MetadataMaxLength} characters.");

    /// <summary>Notes too long ("Asset.InvalidNotes").</summary>
    public static readonly DomainError InvalidNotes = new(
        "Asset.InvalidNotes",
        $"Notes must be at most {AssetLite.Domain.Assets.Asset.NotesMaxLength} characters.");

    /// <summary>Assignee name empty or too long ("Asset.InvalidAssigneeName").</summary>
    public static readonly DomainError InvalidAssigneeName = new(
        "Asset.InvalidAssigneeName",
        "Assignee name is required and must be at most 100 characters.");

    /// <summary>Assignee email malformed ("Asset.InvalidAssigneeEmail").</summary>
    public static readonly DomainError InvalidAssigneeEmail = new(
        "Asset.InvalidAssigneeEmail",
        "Assignee email address is not valid.");

    /// <summary>Assignment attempted while under maintenance ("Asset.CannotAssignMaintenance").</summary>
    public static readonly DomainError CannotAssignMaintenance = new(
        "Asset.CannotAssignMaintenance",
        "An asset under maintenance cannot be assigned.");

    /// <summary>Assignment attempted on a retired asset ("Asset.CannotAssignRetired").</summary>
    public static readonly DomainError CannotAssignRetired = new(
        "Asset.CannotAssignRetired",
        "A retired asset cannot be assigned.");

    /// <summary>Assignment attempted on a disposed asset ("Asset.CannotAssignDisposed").</summary>
    public static readonly DomainError CannotAssignDisposed = new(
        "Asset.CannotAssignDisposed",
        "A disposed asset cannot be assigned.");

    /// <summary>Return attempted while no assignment is open ("Asset.NotAssigned").</summary>
    public static readonly DomainError NotAssigned = new(
        "Asset.NotAssigned",
        "Only an assigned asset can be returned to stock.");

    /// <summary>Maintenance already active ("Asset.AlreadyInMaintenance").</summary>
    public static readonly DomainError AlreadyInMaintenance = new(
        "Asset.AlreadyInMaintenance",
        "Asset is already under maintenance.");

    /// <summary>Maintenance attempted on a retired asset ("Asset.CannotStartMaintenanceRetired").</summary>
    public static readonly DomainError CannotStartMaintenanceRetired = new(
        "Asset.CannotStartMaintenanceRetired",
        "A retired asset cannot be moved to maintenance.");

    /// <summary>Maintenance attempted on a disposed asset ("Asset.CannotStartMaintenanceDisposed").</summary>
    public static readonly DomainError CannotStartMaintenanceDisposed = new(
        "Asset.CannotStartMaintenanceDisposed",
        "A disposed asset cannot be moved to maintenance.");

    /// <summary>Resume attempted while not under maintenance ("Asset.NotInMaintenance").</summary>
    public static readonly DomainError NotInMaintenance = new(
        "Asset.NotInMaintenance",
        "Only an asset under maintenance can be returned to stock.");

    /// <summary>Retire attempted on an already retired asset ("Asset.AlreadyRetired").</summary>
    public static readonly DomainError AlreadyRetired = new(
        "Asset.AlreadyRetired",
        "Asset is already retired.");

    /// <summary>Retire attempted on a disposed asset ("Asset.CannotRetireDisposed").</summary>
    public static readonly DomainError CannotRetireDisposed = new(
        "Asset.CannotRetireDisposed",
        "A disposed asset cannot be retired.");

    /// <summary>Dispose attempted on an asset that is not retired ("Asset.NotRetired").</summary>
    public static readonly DomainError NotRetired = new(
        "Asset.NotRetired",
        "Only a retired asset can be disposed.");

    /// <summary>The target office failed application-level verification ("Asset.InvalidTargetOffice").</summary>
    public static readonly DomainError InvalidTargetOffice = new(
        "Asset.InvalidTargetOffice",
        "The target office is not valid.");

    /// <summary>Asset already located in the target office ("Asset.AlreadyInTargetOffice").</summary>
    public static readonly DomainError AlreadyInTargetOffice = new(
        "Asset.AlreadyInTargetOffice",
        "Asset is already located in the target office.");

    /// <summary>Transfer attempted on a disposed asset ("Asset.CannotTransferDisposed").</summary>
    public static readonly DomainError CannotTransferDisposed = new(
        "Asset.CannotTransferDisposed",
        "A disposed asset cannot be transferred.");

    /// <summary>Details edit attempted on a disposed asset ("Asset.CannotUpdateDisposed").</summary>
    public static readonly DomainError CannotUpdateDisposed = new(
        "Asset.CannotUpdateDisposed",
        "A disposed asset's details can no longer be changed.");
}
