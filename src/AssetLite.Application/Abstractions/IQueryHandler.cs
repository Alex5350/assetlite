using ErrorOr;

namespace AssetLite.Application.Abstractions;

/// <summary>Handles a query and returns its result.</summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The query result type.</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>Executes the query.</summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The query result or a collection of errors.</returns>
    Task<ErrorOr<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
