using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class CruiseHistoryEntityConfiguration : IEntityTypeConfiguration<CruiseHistoryEntity>
{
    public void Configure(EntityTypeBuilder<CruiseHistoryEntity> builder)
    {
        builder.ToTable("CruiseHistories", table =>
        {
            table.HasCheckConstraint("CK_CruiseHistories_DurationNights", "\"DurationNights\" > 0");
            table.HasCheckConstraint("CK_CruiseHistories_OperatorId_Length", "length(\"OperatorId\") BETWEEN 1 AND 200");
            table.HasCheckConstraint("CK_CruiseHistories_ShipName_Length", "length(\"NormalizedShipName\") BETWEEN 1 AND 500");
            table.HasCheckConstraint("CK_CruiseHistories_SourceId_Length", "length(\"RetailSourceId\") <= 200");
            table.HasCheckConstraint("CK_CruiseHistories_SourceName_Length", "\"RetailSourceName\" IS NULL OR length(\"RetailSourceName\") BETWEEN 1 AND 500");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.OperatorId).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.NormalizedShipName).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.DepartureDate).HasConversion(CruisePersistenceConversions.DateOnly).HasMaxLength(10).IsRequired();
        builder.Property(entity => entity.RetailSourceId).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.RetailSourceName).HasMaxLength(500);
        builder.Property(entity => entity.FirstObservedAt).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired();
        builder.Property(entity => entity.LastSeenAt).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired();
        builder.HasIndex(entity => new
            {
                entity.OperatorId,
                entity.NormalizedShipName,
                entity.DepartureDate,
                entity.DurationNights,
                entity.RetailSourceId
            })
            .IsUnique()
            .HasDatabaseName("UX_CruiseHistories_Sailing_Source");
    }
}
