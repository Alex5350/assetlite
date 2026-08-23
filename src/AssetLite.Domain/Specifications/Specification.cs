namespace AssetLite.Domain.Specifications;

/// <summary>
/// Base class for combinable specifications. Combine with <see cref="And"/>/<see cref="Or"/> or
/// the <c>&amp;</c>/<c>|</c> operators.
/// </summary>
/// <typeparam name="T">The type of object being screened.</typeparam>
public abstract class Specification<T> : ISpecification<T>
{
    /// <inheritdoc />
    public abstract bool IsSatisfiedBy(T candidate);

    /// <summary>Creates a specification satisfied only when both operands are satisfied.</summary>
    /// <param name="other">The other operand.</param>
    /// <returns>A conjunction of this specification and <paramref name="other"/>.</returns>
    public Specification<T> And(Specification<T> other) => new AndSpecification(this, other);

    /// <summary>Creates a specification satisfied when either operand is satisfied.</summary>
    /// <param name="other">The other operand.</param>
    /// <returns>A disjunction of this specification and <paramref name="other"/>.</returns>
    public Specification<T> Or(Specification<T> other) => new OrSpecification(this, other);

    /// <summary>Combines two specifications with a logical AND.</summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>A conjunction of both operands.</returns>
    public static Specification<T> operator &(Specification<T> left, Specification<T> right) => left.And(right);

    /// <summary>Combines two specifications with a logical OR.</summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>A disjunction of both operands.</returns>
    public static Specification<T> operator |(Specification<T> left, Specification<T> right) => left.Or(right);

    private sealed class AndSpecification(Specification<T> left, Specification<T> right) : Specification<T>
    {
        public override bool IsSatisfiedBy(T candidate) =>
            left.IsSatisfiedBy(candidate) && right.IsSatisfiedBy(candidate);
    }

    private sealed class OrSpecification(Specification<T> left, Specification<T> right) : Specification<T>
    {
        public override bool IsSatisfiedBy(T candidate) =>
            left.IsSatisfiedBy(candidate) || right.IsSatisfiedBy(candidate);
    }
}
