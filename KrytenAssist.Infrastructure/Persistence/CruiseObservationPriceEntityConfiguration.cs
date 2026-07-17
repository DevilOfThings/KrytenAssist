using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class CruiseObservationPriceEntityConfiguration : IEntityTypeConfiguration<CruiseObservationPriceEntity>
{
    public void Configure(EntityTypeBuilder<CruiseObservationPriceEntity> builder)
    {
        builder.ToTable("CruiseObservationPrices", table =>
        {
            table.HasCheckConstraint("CK_CruiseObservationPrices_Amount", "CAST(\"Amount\" AS REAL) >= 0");
            table.HasCheckConstraint("CK_CruiseObservationPrices_Currency", "length(\"Currency\") = 3 AND \"Currency\" GLOB '[A-Z][A-Z][A-Z]'");
            table.HasCheckConstraint("CK_CruiseObservationPrices_Basis_Length", "\"Basis\" IS NULL OR length(\"Basis\") BETWEEN 1 AND 500");
            table.HasCheckConstraint("CK_CruiseObservationPrices_DisplayOrder", "\"DisplayOrder\" >= 0");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Amount).HasConversion(CruisePersistenceConversions.Decimal).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.Currency).HasMaxLength(3).IsRequired();
        builder.Property(entity => entity.Basis).HasMaxLength(500);
        builder.HasOne(entity => entity.Observation)
            .WithMany(observation => observation.Prices)
            .HasForeignKey(entity => entity.CruiseObservationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(entity => new { entity.CruiseObservationId, entity.DisplayOrder })
            .IsUnique()
            .HasDatabaseName("UX_CruiseObservationPrices_Observation_Order");
    }
}
