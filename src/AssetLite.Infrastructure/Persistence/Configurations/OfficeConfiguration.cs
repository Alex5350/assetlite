using AssetLite.Domain.Offices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetLite.Infrastructure.Persistence.Configurations;

/// <summary>Mapping for the <see cref="Office"/> aggregate, including its self-referencing FK.</summary>
public sealed class OfficeConfiguration : IEntityTypeConfiguration<Office>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Office> builder)
    {
        builder.ToTable("Offices");

        builder.HasKey(office => office.Id);
        builder.Property(office => office.Id)
            .HasConversion(EfConverters.OfficeIdConverter)
            .ValueGeneratedNever();

        builder.Property(office => office.Name).HasMaxLength(Office.NameMaxLength).IsRequired();
        builder.Property(office => office.Code).HasMaxLength(Office.CodeMaxLength).IsRequired();
        builder.HasIndex(office => office.Code).IsUnique();

        // Self-referencing hierarchy: root offices (HQ) store a NULL ParentOfficeId.
        builder.Property(office => office.ParentOfficeId)
            .HasConversion(EfConverters.NullableOfficeIdConverter);
        builder.HasOne<Office>()
            .WithMany()
            .HasForeignKey(office => office.ParentOfficeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(office => office.ParentOfficeId);
    }
}
