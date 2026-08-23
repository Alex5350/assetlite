using AssetLite.Application.Abstractions;
using ErrorOr;
using FluentValidation;

namespace AssetLite.Api.Dispatching;

/// <summary>
/// Thin boundary dispatcher used by every controller action. It runs the FluentValidation
/// validator registered for the request (when one exists — see AddApplication) before handing
/// the request to its application-layer handler. Validation failures are surfaced as
/// <see cref="ErrorType.Validation"/> errors so controllers translate every outcome through one
/// uniform ErrorOr → problem-details path.
/// </summary>
/// <param name="services">Request service provider (resolves validators and handlers).</param>
public sealed class RequestDispatcher(IServiceProvider services)
{
    /// <summary>Validates and executes a query through its handler.</summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TResponse">The query response type.</typeparam>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The handler result, or validation errors.</returns>
    public async Task<ErrorOr<TResponse>> QueryAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken)
        where TQuery : IQuery<TResponse>
    {
        if (await ValidateAsync(query, cancellationToken) is { } validationErrors)
        {
            return validationErrors;
        }

        var handler = services.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        return await handler.HandleAsync(query, cancellationToken);
    }

    /// <summary>Validates and executes a command that produces a payload.</summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResponse">The produced payload type.</typeparam>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The handler result, or validation errors.</returns>
    public async Task<ErrorOr<TResponse>> CommandAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
        where TCommand : ICommand
    {
        if (await ValidateAsync(command, cancellationToken) is { } validationErrors)
        {
            return validationErrors;
        }

        var handler = services.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        return await handler.HandleAsync(command, cancellationToken);
    }

    /// <summary>Validates and executes a command that produces no payload.</summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="Success"/>, or validation errors.</returns>
    public async Task<ErrorOr<Success>> CommandAsync<TCommand>(TCommand command, CancellationToken cancellationToken)
        where TCommand : ICommand
    {
        if (await ValidateAsync(command, cancellationToken) is { } validationErrors)
        {
            return validationErrors;
        }

        var handler = services.GetRequiredService<ICommandHandler<TCommand>>();
        return await handler.HandleAsync(command, cancellationToken);
    }

    private async Task<List<Error>?> ValidateAsync<TRequest>(TRequest request, CancellationToken cancellationToken)
    {
        if (services.GetService<IValidator<TRequest>>() is not { } validator)
        {
            return null;
        }

        var result = await validator.ValidateAsync(request, cancellationToken);
        return result.IsValid
            ? null
            : result.Errors.Select(failure => Error.Validation(failure.PropertyName, failure.ErrorMessage)).ToList();
    }
}
