namespace AssetLite.Domain.Specifications;

/// <summary>
/// A specification: a named, combinable predicate over domain objects. Implementations are
/// plain in-memory predicates; persistence layers may translate them to queries.
/// </summary>
/// <typeparam name="T">The type of object being screened.</typeparam>
public interface ISpecification<in T>
{
    /// <summary>Determines whether <paramref name="candidate"/> satisfies the specification.</summary>
    /// <param name="candidate">The object to check.</param>
    /// <returns><see langword="true"/> when the candidate satisfies the specification.</returns>
    bool IsSatisfiedBy(T candidate);
}
