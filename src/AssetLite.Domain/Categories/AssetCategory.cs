using AssetLite.Domain.Common;
using AssetLite.Domain.Errors;
using AssetLite.Domain.Identities;

namespace AssetLite.Domain.Categories;

/// <summary>
/// A configuration entity describing a class of assets (e.g. "Laptop", "Monitor"), including
/// the expected lifespan used for lifecycle reporting.
/// </summary>
public sealed class AssetCategory
{
    /// <summary>Maximum length of <see cref="Name"/>.</summary>
    public const int NameMaxLength = 100;

    /// <summary>Maximum length of <see cref="Description"/>.</summary>
    public const int DescriptionMaxLength = 500;

    private AssetCategory(CategoryId id, string name, string? description, int expectedLifespanMonths)
    {
        Id = id;
        Name = name;
        Description = description;
        ExpectedLifespanMonths = expectedLifespanMonths;
    }

#pragma warning disable CS8618 // EF Core materializes aggregates through the private parameterless constructor.
    private AssetCategory()
    {
    }
#pragma warning restore CS8618

    /// <summary>Gets the unique identifier of the category.</summary>
    public CategoryId Id { get; private set; }

    /// <summary>Gets the display name (trimmed, 1 - <see cref="NameMaxLength"/> characters).</summary>
    public string Name { get; private set; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the expected lifespan in months (always positive).</summary>
    public int ExpectedLifespanMonths { get; private set; }

    /// <summary>Creates a new asset category after validating its shape.</summary>
    /// <param name="name">Display name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="expectedLifespanMonths">Expected lifespan in months; must be positive.</param>
    /// <returns>
    /// A successful result with the category, or <see cref="CategoryErrors.InvalidName"/>,
    /// <see cref="CategoryErrors.InvalidDescription"/> or <see cref="CategoryErrors.InvalidLifespan"/>.
    /// </returns>
    public static DomainResult<AssetCategory> Create(string name, string? description, int expectedLifespanMonths)
    {
        var validated = Validate(name, description, expectedLifespanMonths);
        if (validated.IsFailure)
        {
            return DomainResult<AssetCategory>.Failure(validated.Error!);
        }

        return DomainResult<AssetCategory>.Success(
            new AssetCategory(CategoryId.New(), validated.Value!.Name, validated.Value!.Description, expectedLifespanMonths));
    }

    /// <summary>Updates the category's editable fields after validating their shape.</summary>
    /// <param name="name">Display name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="expectedLifespanMonths">Expected lifespan in months; must be positive.</param>
    /// <returns>
    /// A successful result, or <see cref="CategoryErrors.InvalidName"/>,
    /// <see cref="CategoryErrors.InvalidDescription"/> or <see cref="CategoryErrors.InvalidLifespan"/>.
    /// </returns>
    public DomainResult Update(string name, string? description, int expectedLifespanMonths)
    {
        var validated = Validate(name, description, expectedLifespanMonths);
        if (validated.IsFailure)
        {
            return DomainResult.Failure(validated.Error!);
        }

        Name = validated.Value!.Name;
        Description = validated.Value!.Description;
        ExpectedLifespanMonths = expectedLifespanMonths;
        return DomainResult.Success();
    }

    private static DomainResult<(string Name, string? Description)> Validate(string name, string? description, int expectedLifespanMonths)
    {
        var normalizedName = (name ?? string.Empty).Trim();
        if (normalizedName.Length is < 1 or > NameMaxLength)
        {
            return DomainResult<(string, string?)>.Failure(CategoryErrors.InvalidName);
        }

        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (normalizedDescription is { Length: > DescriptionMaxLength })
        {
            return DomainResult<(string, string?)>.Failure(CategoryErrors.InvalidDescription);
        }

        if (expectedLifespanMonths < 1)
        {
            return DomainResult<(string, string?)>.Failure(CategoryErrors.InvalidLifespan);
        }

        return DomainResult<(string, string?)>.Success((normalizedName, normalizedDescription));
    }
}
