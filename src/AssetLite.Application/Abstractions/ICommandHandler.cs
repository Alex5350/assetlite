using ErrorOr;

namespace AssetLite.Application.Abstractions;

/// <summary>
/// Handles a command that produces no payload. The unit <see cref="Success"/> value (from the
/// ErrorOr package) keeps the result pipeline composable without per-feature result types.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>Executes the command.</summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Success"/> or a collection of errors.</returns>
    Task<ErrorOr<Success>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>Handles a command that produces a payload (e.g. a created resource).</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The produced payload type.</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand
{
    /// <summary>Executes the command.</summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The produced payload or a collection of errors.</returns>
    Task<ErrorOr<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
