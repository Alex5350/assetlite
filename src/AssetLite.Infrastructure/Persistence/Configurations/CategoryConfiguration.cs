using AssetLite.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetLite.Infrastructure.Persistence.Configurations;

/// <summary>Mapping for the <see cref="AssetCategory"/> configuration entity.</summary>
public sealed class CategoryConfiguration : IEntityTypeConfiguration<AssetCategory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AssetCategory> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id)
            .HasConversion(EfConverters.CategoryIdConverter)
            .ValueGeneratedNever();

        builder.Property(category => category.Name).HasMaxLength(AssetCategory.NameMaxLength).IsRequired();
        builder.Property(category => category.Description).HasMaxLength(AssetCategory.DescriptionMaxLength);
        builder.Property(category => category.ExpectedLifespanMonths).IsRequired();

        // Case-sensitive unique index; the repository's NameExistsAsync additionally compares
        // case-insensitively so create/update validation catches mixed-case duplicates too.
        builder.HasIndex(category => category.Name).IsUnique();
    }
}
