using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class CruiseObservationEntityConfiguration : IEntityTypeConfiguration<CruiseObservationEntity>
{
    public void Configure(EntityTypeBuilder<CruiseObservationEntity> builder)
    {
        builder.ToTable("CruiseObservations", table =>
        {
            table.HasCheckConstraint("CK_CruiseObservations_DurationNights", "\"DurationNights\" > 0");
            table.HasCheckConstraint("CK_CruiseObservations_Sequence", "\"Sequence\" > 0");
            AddRequiredLength(table, "Fingerprint", 16000);
            AddRequiredLength(table, "ProviderOfferId", 1000);
            AddRequiredLength(table, "OperatorName", 500);
            AddRequiredLength(table, "Title", 1000);
            AddRequiredLength(table, "ShipName", 500);
            AddOptionalLength(table, "DeparturePort", 500);
            AddOptionalLength(table, "ItinerarySummary", 4000);
            AddOptionalLength(table, "PromotionSummary", 4000);
            AddOptionalLength(table, "SourceReference", 4000);
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Fingerprint).HasMaxLength(16000).IsRequired();
        builder.Property(entity => entity.ProviderOfferId).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.OperatorName).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.Title).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.ShipName).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.DepartureDate).HasConversion(CruisePersistenceConversions.DateOnly).HasMaxLength(10).IsRequired();
        builder.Property(entity => entity.DeparturePort).HasMaxLength(500);
        builder.Property(entity => entity.ItinerarySummary).HasMaxLength(4000);
        builder.Property(entity => entity.PromotionSummary).HasMaxLength(4000);
        builder.Property(entity => entity.SourceReference).HasMaxLength(4000);
        builder.Property(entity => entity.ObservedAt).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired();
        builder.HasOne(entity => entity.History)
            .WithMany(history => history.Observations)
            .HasForeignKey(entity => entity.CruiseHistoryId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(entity => new { entity.CruiseHistoryId, entity.Sequence })
            .IsUnique()
            .HasDatabaseName("UX_CruiseObservations_History_Sequence");
        builder.HasIndex(entity => new { entity.CruiseHistoryId, entity.Fingerprint })
            .HasDatabaseName("IX_CruiseObservations_History_Fingerprint");
        builder.HasIndex(entity => new { entity.CruiseHistoryId, entity.ObservedAt, entity.Id })
            .HasDatabaseName("IX_CruiseObservations_History_ObservedAt");
    }

    private static void AddRequiredLength(TableBuilder<CruiseObservationEntity> table, string column, int maximum) =>
        table.HasCheckConstraint($"CK_CruiseObservations_{column}_Length", $"length(\"{column}\") BETWEEN 1 AND {maximum}");

    private static void AddOptionalLength(TableBuilder<CruiseObservationEntity> table, string column, int maximum) =>
        table.HasCheckConstraint($"CK_CruiseObservations_{column}_Length", $"\"{column}\" IS NULL OR length(\"{column}\") BETWEEN 1 AND {maximum}");
}
