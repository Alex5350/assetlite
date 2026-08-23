using AssetLite.Domain.Identities;

namespace AssetLite.Domain.Assets;

/// <summary>
/// A historical assignment of an asset to a person. Child entity of the
/// <see cref="Asset"/> aggregate; created only through <see cref="Asset.AssignTo"/>.
/// An assignment is <see cref="IsOpen">open</see> until the asset is returned, reassigned,
/// moved to maintenance from an assigned state, or retired.
/// </summary>
public sealed class Assignment
{
    private Assignment(AssignmentId id, string assigneeName, string assigneeEmail, DateTimeOffset assignedAtUtc, DateTimeOffset? returnedAtUtc)
    {
        Id = id;
        AssigneeName = assigneeName;
        AssigneeEmail = assigneeEmail;
        AssignedAtUtc = assignedAtUtc;
        ReturnedAtUtc = returnedAtUtc;
    }

#pragma warning disable CS8618 // EF Core materializes aggregates through the private parameterless constructor.
    private Assignment()
    {
    }
#pragma warning restore CS8618

    /// <summary>Gets the unique identifier of the assignment record.</summary>
    public AssignmentId Id { get; private set; }

    /// <summary>Gets the assignee's display name at the time of assignment.</summary>
    public string AssigneeName { get; private set; }

    /// <summary>Gets the assignee's email address at the time of assignment.</summary>
    public string AssigneeEmail { get; private set; }

    /// <summary>Gets the UTC moment the asset was handed over.</summary>
    public DateTimeOffset AssignedAtUtc { get; private set; }

    /// <summary>Gets the UTC moment the asset came back, or <see langword="null"/> while open.</summary>
    public DateTimeOffset? ReturnedAtUtc { get; private set; }

    /// <summary>Gets a value indicating whether the assignment is still open (asset not yet returned).</summary>
    public bool IsOpen => ReturnedAtUtc is null;

    /// <summary>Creates a new open assignment. Called only by the <see cref="Asset"/> aggregate.</summary>
    /// <param name="assigneeName">Assignee display name (already validated).</param>
    /// <param name="assigneeEmail">Assignee email (already validated).</param>
    /// <param name="assignedAtUtc">UTC hand-over moment.</param>
    /// <returns>A new open <see cref="Assignment"/>.</returns>
    internal static Assignment Create(string assigneeName, string assigneeEmail, DateTimeOffset assignedAtUtc) =>
        new(AssignmentId.New(), assigneeName, assigneeEmail, assignedAtUtc, null);

    /// <summary>Closes the assignment at <paramref name="returnedAtUtc"/> if it is still open.</summary>
    /// <param name="returnedAtUtc">UTC return moment.</param>
    internal void Close(DateTimeOffset returnedAtUtc)
    {
        if (ReturnedAtUtc is null)
        {
            ReturnedAtUtc = returnedAtUtc;
        }
    }
}
