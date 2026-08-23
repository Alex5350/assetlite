using AssetLite.Domain.Assets;
using AssetLite.Domain.Enums;
using AssetLite.Domain.Identities;
using AssetLite.Domain.Specifications;
using AssetLite.Domain.ValueObjects;
using Xunit;

namespace AssetLite.Domain.UnitTests.Specifications;

/// <summary>Unit tests for <see cref="AssetStatusSpecification"/> and specification combinators.</summary>
public sealed class AssetStatusSpecificationTests
{
    private static Asset NewAsset(AssetStatus status)
    {
        var asset = Asset.Create(
            AssetTag.FromNumber(1).GetValueOrThrow(),
            CategoryId.New(),
            OfficeId.New(),
            "Dell Latitude 5540",
            AssetCondition.Good,
            DateTimeOffset.UtcNow).GetValueOrThrow();
        return Transition(asset, status);
    }

    private static Asset Transition(Asset asset, AssetStatus status)
    {
        switch (status)
        {
            case AssetStatus.Assigned:
                asset.AssignTo("Sarah Chen", "sarah.chen@assetlite.example", DateTimeOffset.UtcNow);
                break;
            case AssetStatus.Maintenance:
                asset.StartMaintenance(DateTimeOffset.UtcNow);
                break;
            case AssetStatus.Retired:
                asset.Retire(DateTimeOffset.UtcNow);
                break;
            case AssetStatus.Disposed:
                asset.Retire(DateTimeOffset.UtcNow);
                asset.Dispose();
                break;
        }

        return asset;
    }

    [Theory]
    [InlineData(AssetStatus.InStock)]
    [InlineData(AssetStatus.Assigned)]
    [InlineData(AssetStatus.Maintenance)]
    [InlineData(AssetStatus.Retired)]
    [InlineData(AssetStatus.Disposed)]
    public void IsSatisfiedBy_WithAssetInMatchingStatus_ReturnsTrue(AssetStatus status)
    {
        ISpecification<Asset> specification = new AssetStatusSpecification(status);

        Assert.True(specification.IsSatisfiedBy(NewAsset(status)));
    }

    [Fact]
    public void IsSatisfiedBy_WithAssetInDifferentStatus_ReturnsFalse()
    {
        ISpecification<Asset> specification = new AssetStatusSpecification(AssetStatus.Assigned);

        Assert.False(specification.IsSatisfiedBy(NewAsset(AssetStatus.InStock)));
    }

    [Fact]
    public void Exposes_TheMatchedStatus()
    {
        var specification = new AssetStatusSpecification(AssetStatus.Retired);

        Assert.Equal(AssetStatus.Retired, specification.Status);
    }

    [Theory]
    [InlineData(AssetStatus.InStock, AssetStatus.Assigned, AssetStatus.Assigned)]  // left false, right true
    [InlineData(AssetStatus.Assigned, AssetStatus.InStock, AssetStatus.InStock)]   // left true, right false
    public void Or_IsSatisfied_WhenEitherOperandMatches(
        AssetStatus leftStatus,
        AssetStatus rightStatus,
        AssetStatus candidateStatus)
    {
        Specification<Asset> specification =
            new AssetStatusSpecification(leftStatus).Or(new AssetStatusSpecification(rightStatus));

        Assert.True(specification.IsSatisfiedBy(NewAsset(candidateStatus)));
    }

    [Fact]
    public void Or_IsNotSatisfied_WhenNeitherOperandMatches()
    {
        Specification<Asset> specification =
            new AssetStatusSpecification(AssetStatus.InStock).Or(new AssetStatusSpecification(AssetStatus.Assigned));

        Assert.False(specification.IsSatisfiedBy(NewAsset(AssetStatus.Retired)));
    }

    [Theory]
    [InlineData(AssetStatus.InStock, AssetStatus.Assigned, AssetStatus.InStock)]
    [InlineData(AssetStatus.InStock, AssetStatus.Assigned, AssetStatus.Assigned)]
    public void And_IsNotSatisfied_WhenOnlyOneOperandMatches(
        AssetStatus leftStatus,
        AssetStatus rightStatus,
        AssetStatus candidateStatus)
    {
        Specification<Asset> specification =
            new AssetStatusSpecification(leftStatus).And(new AssetStatusSpecification(rightStatus));

        Assert.False(specification.IsSatisfiedBy(NewAsset(candidateStatus)));
    }

    [Fact]
    public void And_IsSatisfied_WhenBothOperandsMatch()
    {
        Specification<Asset> specification =
            new AssetStatusSpecification(AssetStatus.Retired).And(new AssetStatusSpecification(AssetStatus.Retired));

        Assert.True(specification.IsSatisfiedBy(NewAsset(AssetStatus.Retired)));
    }

    [Fact]
    public void BitwiseOrOperator_MatchesTheOrMethod()
    {
        Specification<Asset> byMethod = new AssetStatusSpecification(AssetStatus.InStock).Or(new AssetStatusSpecification(AssetStatus.Maintenance));
        Specification<Asset> byOperator = new AssetStatusSpecification(AssetStatus.InStock) | new AssetStatusSpecification(AssetStatus.Maintenance);

        Assert.Equal(byMethod.IsSatisfiedBy(NewAsset(AssetStatus.Maintenance)), byOperator.IsSatisfiedBy(NewAsset(AssetStatus.Maintenance)));
        Assert.True(byOperator.IsSatisfiedBy(NewAsset(AssetStatus.InStock)));
    }

    [Fact]
    public void BitwiseAndOperator_MatchesTheAndMethod()
    {
        Specification<Asset> byMethod = new AssetStatusSpecification(AssetStatus.InStock).And(new AssetStatusSpecification(AssetStatus.InStock));
        Specification<Asset> byOperator = new AssetStatusSpecification(AssetStatus.InStock) & new AssetStatusSpecification(AssetStatus.InStock);

        Assert.Equal(byMethod.IsSatisfiedBy(NewAsset(AssetStatus.InStock)), byOperator.IsSatisfiedBy(NewAsset(AssetStatus.InStock)));
        Assert.False(byOperator.IsSatisfiedBy(NewAsset(AssetStatus.Assigned)));
    }

    [Fact]
    public void Combinators_ComposeIntoLargerExpressions()
    {
        // (Assigned OR Maintenance) AND NOT disposed -> expressed with And/Or only.
        Specification<Asset> active =
            new AssetStatusSpecification(AssetStatus.Assigned) | new AssetStatusSpecification(AssetStatus.Maintenance);
        Specification<Asset> notDisposed =
            new AssetStatusSpecification(AssetStatus.InStock)
                | new AssetStatusSpecification(AssetStatus.Assigned)
                | new AssetStatusSpecification(AssetStatus.Maintenance)
                | new AssetStatusSpecification(AssetStatus.Retired);
        Specification<Asset> combined = active & notDisposed;

        Assert.True(combined.IsSatisfiedBy(NewAsset(AssetStatus.Assigned)));
        Assert.True(combined.IsSatisfiedBy(NewAsset(AssetStatus.Maintenance)));
        Assert.False(combined.IsSatisfiedBy(NewAsset(AssetStatus.Disposed)));
    }
}
