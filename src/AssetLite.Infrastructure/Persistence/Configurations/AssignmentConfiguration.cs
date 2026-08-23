using AssetLite.Domain.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetLite.Infrastructure.Persistence.Configurations;

/// <summary>Mapping for the <see cref="Assignment"/> child entity (one table, FK to its asset).</summary>
public sealed class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");

        builder.HasKey(assignment => assignment.Id);
        builder.Property(assignment => assignment.Id)
            .HasConversion(EfConverters.AssignmentIdConverter)
            .ValueGeneratedNever();

        // Shadow FK: the domain child keeps no back-reference to its parent asset.
        builder.Property<Domain.Identities.AssetId>("AssetId")
            .HasConversion(EfConverters.AssetIdConverter)
            .IsRequired();

        builder.Property(assignment => assignment.AssigneeName).HasMaxLength(Asset.AssigneeNameMaxLength).IsRequired();
        builder.Property(assignment => assignment.AssigneeEmail).HasMaxLength(Asset.AssigneeEmailMaxLength).IsRequired();
        builder.Property(assignment => assignment.AssignedAtUtc).IsRequired();
        builder.Property(assignment => assignment.ReturnedAtUtc);

        // History rows are append-only; query the latest open assignment quickly.
        builder.HasIndex("AssetId", nameof(Assignment.ReturnedAtUtc));
    }
}
