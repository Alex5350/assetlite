namespace AssetLite.Application.Abstractions;

/// <summary>
/// Marker interface for commands (intention to change state). Handlers are resolved via
/// <c>ICommandHandler&lt;T&gt;</c> (no payload) or <c>ICommandHandler&lt;T,TResponse&gt;</c> (with payload).
/// </summary>
public interface ICommand;
