using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class CruiseCabinSeriesEntityConfiguration : IEntityTypeConfiguration<CruiseCabinSeriesEntity>
{
    public void Configure(EntityTypeBuilder<CruiseCabinSeriesEntity> builder)
    {
        builder.ToTable("CruiseCabinSeries", table =>
        {
            Hash(table, "SeriesKey"); Hash(table, "ContextFingerprint");
            RequiredLength(table, "OperatorId", 200); RequiredLength(table, "ShipName", 500);
            RequiredLength(table, "RetailSourceId", SavedCruiseSnapshot.MaximumRetailSourceIdLength);
            RequiredLength(table, "RetailSourceName", SavedCruiseSnapshot.MaximumRetailSourceNameLength);
            OptionalLength(table, "DepartureAirportId", CruiseCabinSearchContext.MaximumAirportIdLength);
            RequiredLength(table, "LatestEvidenceKey", CruiseCabinObservation.MaximumEvidenceKeyLength);
            OptionalLength(table, "LatestSourceReference", CruiseCabinObservation.MaximumSourceReferenceLength);
            table.HasCheckConstraint("CK_CruiseCabinSeries_Duration", "\"DurationNights\" > 0");
            table.HasCheckConstraint("CK_CruiseCabinSeries_Adults", $"\"AdultCount\" IS NULL OR \"AdultCount\" BETWEEN 0 AND {CruiseCabinSearchContext.MaximumPartySize}");
            table.HasCheckConstraint("CK_CruiseCabinSeries_Children", $"\"ChildCount\" IS NULL OR \"ChildCount\" BETWEEN 0 AND {CruiseCabinSearchContext.MaximumPartySize}");
            table.HasCheckConstraint("CK_CruiseCabinSeries_ChildAgesKnown", "\"ChildAgesKnown\" IN (0,1)");
            table.HasCheckConstraint("CK_CruiseCabinSeries_PackageMode", "\"PackageMode\" BETWEEN 0 AND 3");
            table.HasCheckConstraint("CK_CruiseCabinSeries_CabinQuantity", $"\"CabinQuantity\" IS NULL OR \"CabinQuantity\" BETWEEN 1 AND {CruiseCabinSearchContext.MaximumCabinQuantity}");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SeriesKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.OperatorId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ShipName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DepartureDate).HasConversion(CruisePersistenceConversions.DateOnly).HasMaxLength(10).IsRequired();
        builder.Property(x => x.RetailSourceId).HasMaxLength(SavedCruiseSnapshot.MaximumRetailSourceIdLength).IsRequired();
        builder.Property(x => x.RetailSourceName).HasMaxLength(SavedCruiseSnapshot.MaximumRetailSourceNameLength).IsRequired();
        builder.Property(x => x.ContextFingerprint).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DepartureAirportId).HasMaxLength(CruiseCabinSearchContext.MaximumAirportIdLength);
        builder.Property(x => x.FirstObservedAt).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired();
        builder.Property(x => x.LastSeenAt).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired();
        builder.Property(x => x.LatestEvidenceKey).HasMaxLength(CruiseCabinObservation.MaximumEvidenceKeyLength).IsRequired();
        builder.Property(x => x.LatestSourceReference).HasMaxLength(CruiseCabinObservation.MaximumSourceReferenceLength);
        builder.Property(x => x.LatestEvidenceObservedAt).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired();
        builder.HasIndex(x => x.SeriesKey).IsUnique().HasDatabaseName("UX_CruiseCabinSeries_SeriesKey");
        builder.HasIndex(x => new { x.DepartureDate, x.OperatorId, x.ShipName, x.RetailSourceId, x.ContextFingerprint })
            .HasDatabaseName("IX_CruiseCabinSeries_List");
    }

    private static void Hash(TableBuilder<CruiseCabinSeriesEntity> table, string column) =>
        table.HasCheckConstraint($"CK_CruiseCabinSeries_{column}", $"length(\"{column}\") = 64 AND \"{column}\" NOT GLOB '*[^0-9a-f]*'");
    private static void RequiredLength(TableBuilder<CruiseCabinSeriesEntity> table, string column, int maximum) =>
        table.HasCheckConstraint($"CK_CruiseCabinSeries_{column}_Length", $"length(\"{column}\") BETWEEN 1 AND {maximum}");
    private static void OptionalLength(TableBuilder<CruiseCabinSeriesEntity> table, string column, int maximum) =>
        table.HasCheckConstraint($"CK_CruiseCabinSeries_{column}_Length", $"\"{column}\" IS NULL OR length(\"{column}\") BETWEEN 1 AND {maximum}");
}

public sealed class CruiseCabinContextChildAgeEntityConfiguration : IEntityTypeConfiguration<CruiseCabinContextChildAgeEntity>
{
    public void Configure(EntityTypeBuilder<CruiseCabinContextChildAgeEntity> builder)
    {
        builder.ToTable("CruiseCabinContextChildAges", table =>
        {
            table.HasCheckConstraint("CK_CruiseCabinContextChildAges_Order", "\"DisplayOrder\" >= 0");
            table.HasCheckConstraint("CK_CruiseCabinContextChildAges_Age", "\"Age\" BETWEEN 0 AND 17");
        });
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.Series).WithMany(x => x.ChildAges).HasForeignKey(x => x.CruiseCabinSeriesId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.CruiseCabinSeriesId, x.DisplayOrder }).IsUnique().HasDatabaseName("UX_CruiseCabinContextChildAges_Series_Order");
    }
}

public sealed class CruiseCabinObservationEntityConfiguration : IEntityTypeConfiguration<CruiseCabinObservationEntity>
{
    public void Configure(EntityTypeBuilder<CruiseCabinObservationEntity> builder)
    {
        builder.ToTable("CruiseCabinObservations", table =>
        {
            table.HasCheckConstraint("CK_CruiseCabinObservations_Sequence", "\"Sequence\" > 0");
            table.HasCheckConstraint("CK_CruiseCabinObservations_StateFingerprint", "length(\"StateFingerprint\") = 64 AND \"StateFingerprint\" NOT GLOB '*[^0-9a-f]*'");
            table.HasCheckConstraint("CK_CruiseCabinObservations_Coverage", "\"Coverage\" BETWEEN 0 AND 1");
            table.HasCheckConstraint("CK_CruiseCabinObservations_EvidenceKey_Length", $"length(\"EvidenceKey\") BETWEEN 1 AND {CruiseCabinObservation.MaximumEvidenceKeyLength}");
            table.HasCheckConstraint("CK_CruiseCabinObservations_SourceReference_Length", $"\"SourceReference\" IS NULL OR length(\"SourceReference\") BETWEEN 1 AND {CruiseCabinObservation.MaximumSourceReferenceLength}");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StateFingerprint).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ObservedAt).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired();
        builder.Property(x => x.EvidenceKey).HasMaxLength(CruiseCabinObservation.MaximumEvidenceKeyLength).IsRequired();
        builder.Property(x => x.SourceReference).HasMaxLength(CruiseCabinObservation.MaximumSourceReferenceLength);
        builder.HasOne(x => x.Series).WithMany(x => x.Observations).HasForeignKey(x => x.CruiseCabinSeriesId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.CruiseCabinSeriesId, x.Sequence }).IsUnique().HasDatabaseName("UX_CruiseCabinObservations_Series_Sequence");
        builder.HasIndex(x => new { x.CruiseCabinSeriesId, x.StateFingerprint }).HasDatabaseName("IX_CruiseCabinObservations_Series_State");
        builder.HasIndex(x => new { x.CruiseCabinSeriesId, x.ObservedAtUtcTicks, x.StateFingerprint }).HasDatabaseName("IX_CruiseCabinObservations_Series_Observed");
    }
}

public sealed class CruiseCabinObservationStateEntityConfiguration : IEntityTypeConfiguration<CruiseCabinObservationStateEntity>
{
    public void Configure(EntityTypeBuilder<CruiseCabinObservationStateEntity> builder)
    {
        builder.ToTable("CruiseCabinObservationStates", table =>
        {
            table.HasCheckConstraint("CK_CruiseCabinObservationStates_CabinType", "\"CabinType\" BETWEEN 0 AND 4");
            table.HasCheckConstraint("CK_CruiseCabinObservationStates_Availability", "\"Availability\" BETWEEN 0 AND 2");
        });
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.Observation).WithMany(x => x.States).HasForeignKey(x => x.CruiseCabinObservationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.CruiseCabinObservationId, x.CabinType }).IsUnique().HasDatabaseName("UX_CruiseCabinObservationStates_Observation_Cabin");
    }
}
