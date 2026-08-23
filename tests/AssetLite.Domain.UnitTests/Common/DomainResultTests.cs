using AssetLite.Domain.Common;
using AssetLite.Domain.Errors;
using Xunit;

namespace AssetLite.Domain.UnitTests.Common;

/// <summary>Unit tests for the <see cref="DomainResult"/> and <see cref="DomainResult{T}"/> helpers.</summary>
public sealed class DomainResultTests
{
    private static readonly DomainError SampleError = new("Test.Failure", "Something went wrong.");

    [Fact]
    public void Success_WithoutValue_IsASuccessfulResult()
    {
        var result = DomainResult.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_CarriesTheError()
    {
        var result = DomainResult.Failure(SampleError);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(SampleError, result.Error);
    }

    [Fact]
    public void GenericSuccess_CarriesTheValue()
    {
        var result = DomainResult<string>.Success("value");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("value", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void GenericFailure_HasNoValueAndCarriesTheError()
    {
        var result = DomainResult<string>.Failure(SampleError);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Null(result.Value);
        Assert.Equal(SampleError, result.Error);
    }

    [Fact]
    public void GetValueOrThrow_OnSuccess_ReturnsTheValue()
    {
        var result = DomainResult<int>.Success(42);

        Assert.Equal(42, result.GetValueOrThrow());
    }

    [Fact]
    public void GetValueOrThrow_OnGenericFailure_ThrowsInvalidOperationException()
    {
        var result = DomainResult<int>.Failure(SampleError);

        var exception = Assert.Throws<InvalidOperationException>(() => result.GetValueOrThrow());
        Assert.Contains(SampleError.Code, exception.Message);
    }

    [Fact]
    public void IsFailure_IsTheInverseOfIsSuccess()
    {
        Assert.True(DomainResult.Failure(SampleError).IsFailure);
        Assert.False(DomainResult.Success().IsFailure);
    }

    [Fact]
    public void DomainError_IsAValueRecord()
    {
        var left = new DomainError("Asset.NotFound", "Asset was not found.");
        var right = new DomainError("Asset.NotFound", "Asset was not found.");

        Assert.Equal(left, right);
        Assert.Equal("Asset.NotFound", left.Code);
        Assert.Equal("Asset was not found.", left.Message);
    }
}
