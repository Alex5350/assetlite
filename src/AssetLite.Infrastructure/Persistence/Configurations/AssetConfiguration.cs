using AssetLite.Domain.Assets;
using AssetLite.Domain.Categories;
using AssetLite.Domain.Offices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetLite.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapping for the <see cref="Asset"/> aggregate root: typed id and tag converters, the
/// <see cref="Money"/> complex type (amount + currency columns), referential integrity to offices
/// and categories, and the assignment history collection through its private backing field.
/// </summary>
public sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets");

        builder.HasKey(asset => asset.Id);
        builder.Property(asset => asset.Id)
            .HasConversion(EfConverters.AssetIdConverter)
            .ValueGeneratedNever();

        // Canonical fixed-width string storage: see EfConverters.AssetTagConverter remarks.
        builder.Property(asset => asset.Tag)
            .HasConversion(EfConverters.AssetTagConverter)
            .HasColumnName("Tag")
            .HasMaxLength(10)
            .IsRequired();
        builder.HasIndex(asset => asset.Tag).IsUnique();

        builder.Property(asset => asset.CategoryId)
            .HasConversion(EfConverters.CategoryIdConverter)
            .IsRequired();
        builder.Property(asset => asset.OfficeId)
            .HasConversion(EfConverters.OfficeIdConverter)
            .IsRequired();

        builder.Property(asset => asset.Name).HasMaxLength(Asset.NameMaxLength).IsRequired();
        builder.Property(asset => asset.Manufacturer).HasMaxLength(Asset.MetadataMaxLength);
        builder.Property(asset => asset.Model).HasMaxLength(Asset.MetadataMaxLength);
        builder.Property(asset => asset.SerialNumber).HasMaxLength(Asset.MetadataMaxLength);
        builder.Property(asset => asset.Notes).HasMaxLength(Asset.NotesMaxLength);
        builder.Property(asset => asset.Status).IsRequired();
        builder.Property(asset => asset.Condition).IsRequired();
        builder.Property(asset => asset.PurchaseDate);
        builder.Property(asset => asset.CreatedAtUtc).IsRequired();

        // Money as an optional complex type flattened into two columns. Default complex-type
        // column names (PurchaseCost_Amount / PurchaseCost_Currency) are kept deliberately:
        // composable raw-SQL searches (see AssetRepository) project those exact names.
        builder.ComplexProperty(
            asset => asset.PurchaseCost,
            money =>
            {
                money.Property(m => m.Amount);
                money.Property(m => m.Currency).HasMaxLength(3).IsRequired();
                money.IsRequired(false);
            });

        // The aggregate references category/office without domain navigations; the database still
        // enforces integrity. Restrict: assets must be moved or deleted before their office/category.
        builder.HasOne<AssetCategory>()
            .WithMany()
            .HasForeignKey(asset => asset.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Office>()
            .WithMany()
            .HasForeignKey(asset => asset.OfficeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Assignment history lives in the private _assignments backing field and is part of the
        // aggregate, so it always loads with it (AutoInclude) and mutates only through the domain.
        builder.HasMany(asset => asset.Assignments)
            .WithOne()
            .HasForeignKey("AssetId") // shadow FK: Assignment keeps no back-reference to its parent
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(asset => asset.Assignments)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();

        // Computed/derived members must not be mapped.
        builder.Ignore(asset => asset.OpenAssignment);
        builder.Ignore(asset => asset.Events);
    }
}
