namespace AssetLite.Domain.Common;

/// <summary>
/// Marker interface for domain events raised by aggregates. Events are plain immutable records;
/// aggregates buffer them and hand them over via a <c>PullEvents()</c> method so the persistence
/// layer can dispatch them only after changes have been saved successfully.
/// </summary>
public interface IDomainEvent;
