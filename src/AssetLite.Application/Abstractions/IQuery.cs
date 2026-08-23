namespace AssetLite.Application.Abstractions;

/// <summary>
/// Marker interface for queries (read-only side effects). The response type is part of the
/// marker so handlers can be resolved via <c>IQueryHandler&lt;T,TResponse&gt;</c>.
/// </summary>
/// <typeparam name="TResponse">The query result type.</typeparam>
public interface IQuery<TResponse>;
