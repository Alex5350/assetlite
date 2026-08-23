using AssetLite.Domain.Common;
using AssetLite.Domain.Enums;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;
using AssetLite.Domain.ValueObjects;

namespace AssetLite.Domain.Assets;

/// <summary>
/// Aggregate root for a physical asset tracked by the organization (computer, monitor, phone,
/// networking gear, ...). Owns its assignment history and buffers domain events.
/// </summary>
/// <remarks>
/// <para>
/// State machine (<see cref="AssetStatus"/>): InStock ⇄ Assigned, InStock/Assigned → Maintenance
/// → InStock, any non-terminal status → Retired → Disposed. Illegal transitions return typed
/// errors from the <see cref="AssetErrors"/> catalog; expected failures never throw.
/// </para>
/// <para>
/// Timestamps are passed in by callers (Application supplies them via IDateTimeProvider) so the
/// aggregate stays deterministic and unit-testable.
/// </para>
/// </remarks>
public sealed class Asset
{
    /// <summary>Maximum length of <see cref="Name"/>.</summary>
    public const int NameMaxLength = 200;

    /// <summary>Maximum length of <see cref="Manufacturer"/>, <see cref="Model"/> and <see cref="SerialNumber"/>.</summary>
    public const int MetadataMaxLength = 100;

    /// <summary>Maximum length of <see cref="Notes"/>.</summary>
    public const int NotesMaxLength = 2000;

    /// <summary>Maximum length of an assignee name.</summary>
    public const int AssigneeNameMaxLength = 100;

    /// <summary>Maximum length of an assignee email address.</summary>
    public const int AssigneeEmailMaxLength = 254;

    private readonly List<Assignment> _assignments = [];
    private readonly List<IDomainEvent> _events = [];

    private Asset(
        AssetId id,
        AssetTag tag,
        CategoryId categoryId,
        OfficeId officeId,
        string name,
        string? manufacturer,
        string? model,
        string? serialNumber,
        AssetStatus status,
        AssetCondition condition,
        DateOnly? purchaseDate,
        Money? purchaseCost,
        string? notes,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        Tag = tag;
        CategoryId = categoryId;
        OfficeId = officeId;
        Name = name;
        Manufacturer = manufacturer;
        Model = model;
        SerialNumber = serialNumber;
        Status = status;
        Condition = condition;
        PurchaseDate = purchaseDate;
        PurchaseCost = purchaseCost;
        Notes = notes;
        CreatedAtUtc = createdAtUtc;
    }

#pragma warning disable CS8618 // EF Core materializes aggregates through the private parameterless constructor.
    private Asset()
    {
    }
#pragma warning restore CS8618

    /// <summary>Gets the unique identifier of the asset.</summary>
    public AssetId Id { get; private set; }

    /// <summary>Gets the unique asset tag (e.g. AST-000123).</summary>
    public AssetTag Tag { get; private set; }

    /// <summary>Gets the category of the asset.</summary>
    public CategoryId CategoryId { get; private set; }

    /// <summary>Gets the office currently holding the asset.</summary>
    public OfficeId OfficeId { get; private set; }

    /// <summary>Gets the display name.</summary>
    public string Name { get; private set; }

    /// <summary>Gets the optional manufacturer.</summary>
    public string? Manufacturer { get; private set; }

    /// <summary>Gets the optional model.</summary>
    public string? Model { get; private set; }

    /// <summary>Gets the optional serial number.</summary>
    public string? SerialNumber { get; private set; }

    /// <summary>Gets the lifecycle status.</summary>
    public AssetStatus Status { get; private set; }

    /// <summary>Gets the physical condition.</summary>
    public AssetCondition Condition { get; private set; }

    /// <summary>Gets the optional purchase date.</summary>
    public DateOnly? PurchaseDate { get; private set; }

    /// <summary>Gets the optional purchase cost (2 decimal places).</summary>
    public Money? PurchaseCost { get; private set; }

    /// <summary>Gets the optional free-form notes.</summary>
    public string? Notes { get; private set; }

    /// <summary>Gets the UTC creation moment.</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>
    /// Gets the assignment history in insertion order. At most one assignment is
    /// <see cref="Assignment.IsOpen">open</see> at any time.
    /// </summary>
    public IReadOnlyList<Assignment> Assignments => _assignments;

    /// <summary>Gets the single open assignment, or <see langword="null"/> when the asset is not currently assigned.</summary>
    public Assignment? OpenAssignment => _assignments.FirstOrDefault(assignment => assignment.IsOpen);

    /// <summary>Gets the domain events raised since the last <see cref="PullEvents"/> call.</summary>
    public IReadOnlyList<IDomainEvent> Events => _events;

    /// <summary>Returns and clears the buffered domain events. Call after changes were persisted.</summary>
    /// <returns>The events raised since the previous pull.</returns>
    public IReadOnlyList<IDomainEvent> PullEvents()
    {
        var events = _events.ToArray();
        _events.Clear();
        return events;
    }

    /// <summary>
    /// Creates a new asset in the <see cref="AssetStatus.InStock"/> status, validating the
    /// shapes of the tag, category id, office id and descriptive fields.
    /// </summary>
    /// <param name="tag">The unique tag allocated by the application layer.</param>
    /// <param name="categoryId">The asset category.</param>
    /// <param name="officeId">The office holding the asset.</param>
    /// <param name="name">Display name.</param>
    /// <param name="condition">Physical condition.</param>
    /// <param name="createdAtUtc">UTC creation moment (from IDateTimeProvider).</param>
    /// <param name="manufacturer">Optional manufacturer.</param>
    /// <param name="model">Optional model.</param>
    /// <param name="serialNumber">Optional serial number.</param>
    /// <param name="purchaseDate">Optional purchase date.</param>
    /// <param name="purchaseCost">Optional purchase cost.</param>
    /// <param name="notes">Optional free-form notes.</param>
    /// <param name="id">Optional pre-generated id (a new Guid v7 is created when omitted).</param>
    /// <returns>A successful result with the asset, or a typed error from <see cref="AssetErrors"/>.</returns>
    public static DomainResult<Asset> Create(
        AssetTag tag,
        CategoryId categoryId,
        OfficeId officeId,
        string name,
        AssetCondition condition,
        DateTimeOffset createdAtUtc,
        string? manufacturer = null,
        string? model = null,
        string? serialNumber = null,
        DateOnly? purchaseDate = null,
        Money? purchaseCost = null,
        string? notes = null,
        AssetId? id = null)
    {
        if (tag is null)
        {
            return DomainResult<Asset>.Failure(AssetErrors.InvalidTag);
        }

        if (categoryId.IsEmpty)
        {
            return DomainResult<Asset>.Failure(AssetErrors.EmptyCategoryId);
        }

        if (officeId.IsEmpty)
        {
            return DomainResult<Asset>.Failure(AssetErrors.EmptyOfficeId);
        }

        var normalizedName = (name ?? string.Empty).Trim();
        if (normalizedName.Length is < 1 or > NameMaxLength)
        {
            return DomainResult<Asset>.Failure(AssetErrors.InvalidName);
        }

        var normalizedManufacturer = NormalizeOptional(manufacturer);
        var normalizedModel = NormalizeOptional(model);
        var normalizedSerialNumber = NormalizeOptional(serialNumber);
        if (normalizedManufacturer is { Length: > MetadataMaxLength }
            || normalizedModel is { Length: > MetadataMaxLength }
            || normalizedSerialNumber is { Length: > MetadataMaxLength })
        {
            return DomainResult<Asset>.Failure(AssetErrors.InvalidMetadata);
        }

        var normalizedNotes = NormalizeOptional(notes);
        if (normalizedNotes is { Length: > NotesMaxLength })
        {
            return DomainResult<Asset>.Failure(AssetErrors.InvalidNotes);
        }

        return DomainResult<Asset>.Success(new Asset(
            id ?? AssetId.New(),
            tag,
            categoryId,
            officeId,
            normalizedName,
            normalizedManufacturer,
            normalizedModel,
            normalizedSerialNumber,
            AssetStatus.InStock,
            condition,
            purchaseDate,
            purchaseCost,
            normalizedNotes,
            createdAtUtc));
    }

    /// <summary>
    /// Assigns the asset to a person. Allowed from <see cref="AssetStatus.InStock"/> (new
    /// assignment) and <see cref="AssetStatus.Assigned"/> (reassignment: the previous open
    /// assignment is closed first). Raises <see cref="AssetAssignedDomainEvent"/>.
    /// </summary>
    /// <param name="assigneeName">Assignee display name.</param>
    /// <param name="assigneeEmail">Assignee email address.</param>
    /// <param name="assignedAtUtc">UTC hand-over moment (from IDateTimeProvider).</param>
    /// <returns>
    /// A successful result, or <see cref="AssetErrors.CannotAssignMaintenance"/>,
    /// <see cref="AssetErrors.CannotAssignRetired"/>, <see cref="AssetErrors.CannotAssignDisposed"/>,
    /// <see cref="AssetErrors.InvalidAssigneeName"/> or <see cref="AssetErrors.InvalidAssigneeEmail"/>.
    /// </returns>
    public DomainResult AssignTo(string assigneeName, string assigneeEmail, DateTimeOffset assignedAtUtc)
    {
        if (Status is AssetStatus.Maintenance)
        {
            return DomainResult.Failure(AssetErrors.CannotAssignMaintenance);
        }

        if (Status is AssetStatus.Retired)
        {
            return DomainResult.Failure(AssetErrors.CannotAssignRetired);
        }

        if (Status is AssetStatus.Disposed)
        {
            return DomainResult.Failure(AssetErrors.CannotAssignDisposed);
        }

        var normalizedAssigneeName = (assigneeName ?? string.Empty).Trim();
        if (normalizedAssigneeName.Length is < 1 or > AssigneeNameMaxLength)
        {
            return DomainResult.Failure(AssetErrors.InvalidAssigneeName);
        }

        var normalizedAssigneeEmail = (assigneeEmail ?? string.Empty).Trim();
        if (!IsPlausibleEmail(normalizedAssigneeEmail))
        {
            return DomainResult.Failure(AssetErrors.InvalidAssigneeEmail);
        }

        // Reassignment closes the previous open assignment; a fresh record is appended so the
        // full history is preserved.
        OpenAssignment?.Close(assignedAtUtc);
        _assignments.Add(Assignment.Create(normalizedAssigneeName, normalizedAssigneeEmail, assignedAtUtc));
        Status = AssetStatus.Assigned;
        Raise(new AssetAssignedDomainEvent(Id, Tag, normalizedAssigneeName, normalizedAssigneeEmail, assignedAtUtc));
        return DomainResult.Success();
    }

    /// <summary>
    /// Returns an assigned asset to stock: closes the open assignment and raises
    /// <see cref="AssetReturnedDomainEvent"/>.
    /// </summary>
    /// <param name="returnedAtUtc">UTC return moment (from IDateTimeProvider).</param>
    /// <returns>A successful result, or <see cref="AssetErrors.NotAssigned"/>.</returns>
    public DomainResult ReturnToStock(DateTimeOffset returnedAtUtc)
    {
        if (Status is not AssetStatus.Assigned || OpenAssignment is null)
        {
            return DomainResult.Failure(AssetErrors.NotAssigned);
        }

        OpenAssignment.Close(returnedAtUtc);
        Status = AssetStatus.InStock;
        Raise(new AssetReturnedDomainEvent(Id, Tag, returnedAtUtc));
        return DomainResult.Success();
    }

    /// <summary>
    /// Moves the asset to <see cref="AssetStatus.Maintenance"/> from
    /// <see cref="AssetStatus.InStock"/> or <see cref="AssetStatus.Assigned"/>. When the asset is
    /// currently assigned, the open assignment is closed at <paramref name="asOfUtc"/>.
    /// </summary>
    /// <param name="asOfUtc">UTC moment used to close an open assignment (from IDateTimeProvider).</param>
    /// <returns>
    /// A successful result, or <see cref="AssetErrors.AlreadyInMaintenance"/>,
    /// <see cref="AssetErrors.CannotStartMaintenanceRetired"/> or
    /// <see cref="AssetErrors.CannotStartMaintenanceDisposed"/>.
    /// </returns>
    public DomainResult StartMaintenance(DateTimeOffset asOfUtc)
    {
        if (Status is AssetStatus.Maintenance)
        {
            return DomainResult.Failure(AssetErrors.AlreadyInMaintenance);
        }

        if (Status is AssetStatus.Retired)
        {
            return DomainResult.Failure(AssetErrors.CannotStartMaintenanceRetired);
        }

        if (Status is AssetStatus.Disposed)
        {
            return DomainResult.Failure(AssetErrors.CannotStartMaintenanceDisposed);
        }

        OpenAssignment?.Close(asOfUtc);
        Status = AssetStatus.Maintenance;
        return DomainResult.Success();
    }

    /// <summary>
    /// Returns the asset from <see cref="AssetStatus.Maintenance"/> to
    /// <see cref="AssetStatus.InStock"/>.
    /// </summary>
    /// <returns>A successful result, or <see cref="AssetErrors.NotInMaintenance"/>.</returns>
    public DomainResult ResumeFromMaintenance()
    {
        if (Status is not AssetStatus.Maintenance)
        {
            return DomainResult.Failure(AssetErrors.NotInMaintenance);
        }

        Status = AssetStatus.InStock;
        return DomainResult.Success();
    }

    /// <summary>
    /// Retires the asset from any non-terminal status, closing any open assignment and raising
    /// <see cref="AssetRetiredDomainEvent"/>.
    /// </summary>
    /// <param name="retiredAtUtc">UTC retirement moment (from IDateTimeProvider).</param>
    /// <returns>
    /// A successful result, or <see cref="AssetErrors.AlreadyRetired"/> or
    /// <see cref="AssetErrors.CannotRetireDisposed"/>.
    /// </returns>
    public DomainResult Retire(DateTimeOffset retiredAtUtc)
    {
        if (Status is AssetStatus.Retired)
        {
            return DomainResult.Failure(AssetErrors.AlreadyRetired);
        }

        if (Status is AssetStatus.Disposed)
        {
            return DomainResult.Failure(AssetErrors.CannotRetireDisposed);
        }

        OpenAssignment?.Close(retiredAtUtc);
        Status = AssetStatus.Retired;
        Raise(new AssetRetiredDomainEvent(Id, Tag, retiredAtUtc));
        return DomainResult.Success();
    }

    /// <summary>
    /// Disposes a retired asset. Terminal transition: only
    /// <see cref="AssetStatus.Retired"/> → <see cref="AssetStatus.Disposed"/> is allowed.
    /// </summary>
    /// <returns>A successful result, or <see cref="AssetErrors.NotRetired"/>.</returns>
    public DomainResult Dispose()
    {
        if (Status is not AssetStatus.Retired)
        {
            return DomainResult.Failure(AssetErrors.NotRetired);
        }

        Status = AssetStatus.Disposed;
        return DomainResult.Success();
    }

    /// <summary>
    /// Transfers the asset to another office. The caller (Application layer) verifies the target
    /// office exists and passes <paramref name="targetIsValid"/> accordingly; hierarchy rules
    /// apply to offices, not assets, so no further checks are made here.
    /// </summary>
    /// <param name="targetOfficeId">The destination office.</param>
    /// <param name="targetIsValid"><see langword="true"/> when the target was verified by the application layer.</param>
    /// <returns>
    /// A successful result, or <see cref="AssetErrors.InvalidTargetOffice"/>,
    /// <see cref="AssetErrors.AlreadyInTargetOffice"/> or
    /// <see cref="AssetErrors.CannotTransferDisposed"/>.
    /// </returns>
    public DomainResult TransferTo(OfficeId targetOfficeId, bool targetIsValid)
    {
        if (!targetIsValid || targetOfficeId.IsEmpty)
        {
            return DomainResult.Failure(AssetErrors.InvalidTargetOffice);
        }

        if (Status is AssetStatus.Disposed)
        {
            return DomainResult.Failure(AssetErrors.CannotTransferDisposed);
        }

        if (targetOfficeId == OfficeId)
        {
            return DomainResult.Failure(AssetErrors.AlreadyInTargetOffice);
        }

        OfficeId = targetOfficeId;
        return DomainResult.Success();
    }

    private void Raise(IDomainEvent domainEvent) => _events.Add(domainEvent);

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static bool IsPlausibleEmail(string email)
    {
        // Shape check only; full RFC validation happens at the API boundary with FluentValidation.
        var atIndex = email.IndexOf('@');
        return email.Length is >= 3 and <= AssigneeEmailMaxLength
            && atIndex > 0
            && atIndex < email.Length - 1
            && email.IndexOf('@', atIndex + 1) < 0;
    }
}
