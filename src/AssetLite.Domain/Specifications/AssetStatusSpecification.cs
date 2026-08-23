using AssetLite.Domain.Assets;
using AssetLite.Domain.Enums;

namespace AssetLite.Domain.Specifications;

/// <summary>Specification matching assets currently in a given <see cref="Enums.AssetStatus"/>.</summary>
/// <param name="status">The status to match.</param>
public sealed class AssetStatusSpecification(AssetStatus status) : Specification<Asset>
{
    /// <summary>Gets the status matched by this specification.</summary>
    public AssetStatus Status { get; } = status;

    /// <inheritdoc />
    public override bool IsSatisfiedBy(Asset candidate) => candidate.Status == Status;
}
