using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class CruiseAlertEntityConfiguration : IEntityTypeConfiguration<CruiseAlertEntity>
{
    public void Configure(EntityTypeBuilder<CruiseAlertEntity> builder)
    {
        builder.ToTable("CruiseAlerts", table =>
        {
            table.HasCheckConstraint("CK_CruiseAlerts_EventKey", "length(\"EventKey\") = 64 AND \"EventKey\" NOT GLOB '*[^0-9a-f]*'");
            table.HasCheckConstraint("CK_CruiseAlerts_Type", "\"Type\" BETWEEN 0 AND 4");
            table.HasCheckConstraint("CK_CruiseAlerts_Status", "\"Status\" BETWEEN 0 AND 2");
            table.HasCheckConstraint("CK_CruiseAlerts_Subject", "(\"Type\" BETWEEN 0 AND 3 AND \"OperatorId\" IS NOT NULL AND \"ShipName\" IS NOT NULL AND \"DepartureDate\" IS NOT NULL AND \"DurationNights\" > 0 AND \"ItineraryOperatorId\" IS NULL AND \"ProviderItineraryId\" IS NULL) OR (\"Type\" = 4 AND \"OperatorId\" IS NULL AND \"ShipName\" IS NULL AND \"DepartureDate\" IS NULL AND \"DurationNights\" IS NULL AND \"ItineraryOperatorId\" IS NOT NULL AND \"ProviderItineraryId\" IS NOT NULL)");
            table.HasCheckConstraint("CK_CruiseAlerts_SourcePair", "(\"RetailSourceId\" IS NULL AND \"RetailSourceName\" IS NULL) OR (\"RetailSourceId\" IS NOT NULL AND \"RetailSourceName\" IS NOT NULL)");
            table.HasCheckConstraint("CK_CruiseAlerts_TypeSource", "(\"Type\" IN (0, 1, 3, 4) AND \"RetailSourceId\" IS NOT NULL) OR (\"Type\" = 2 AND \"RetailSourceId\" IS NULL)");
            Length(table, "OperatorId", 200, true); Length(table, "ShipName", 500, true);
            Length(table, "ItineraryOperatorId", 200, true); Length(table, "ProviderItineraryId", CruiseItineraryKey.MaximumProviderItineraryIdLength, true);
            Length(table, "RetailSourceId", SavedCruiseSnapshot.MaximumRetailSourceIdLength, true);
            Length(table, "RetailSourceName", SavedCruiseSnapshot.MaximumRetailSourceNameLength, true);
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.OperatorId).HasMaxLength(200);
        builder.Property(x => x.ShipName).HasMaxLength(500);
        builder.Property(x => x.DepartureDate).HasConversion(CruisePersistenceConversions.NullableDateOnly).HasMaxLength(10);
        builder.Property(x => x.ItineraryOperatorId).HasMaxLength(200);
        builder.Property(x => x.ProviderItineraryId).HasMaxLength(CruiseItineraryKey.MaximumProviderItineraryIdLength);
        builder.Property(x => x.RetailSourceId).HasMaxLength(SavedCruiseSnapshot.MaximumRetailSourceIdLength);
        builder.Property(x => x.RetailSourceName).HasMaxLength(SavedCruiseSnapshot.MaximumRetailSourceNameLength);
        builder.Property(x => x.EventTime).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired();
        builder.Property(x => x.CreatedAt).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired();
        builder.HasIndex(x => x.EventKey).IsUnique().HasDatabaseName("UX_CruiseAlerts_EventKey");
        builder.HasIndex(x => new { x.EventTimeUtcTicks, x.CreatedAtUtcTicks, x.EventKey }).HasDatabaseName("IX_CruiseAlerts_Newest");
        builder.HasIndex(x => new { x.Status, x.Type, x.EventTimeUtcTicks }).HasDatabaseName("IX_CruiseAlerts_Status_Type_Event");
    }

    private static void Length(TableBuilder<CruiseAlertEntity> table, string column, int maximum, bool optional) =>
        table.HasCheckConstraint($"CK_CruiseAlerts_{column}_Length", optional ? $"\"{column}\" IS NULL OR length(\"{column}\") BETWEEN 1 AND {maximum}" : $"length(\"{column}\") BETWEEN 1 AND {maximum}");
}

public sealed class CruisePriceDropAlertDetailEntityConfiguration : IEntityTypeConfiguration<CruisePriceDropAlertDetailEntity>
{
    public void Configure(EntityTypeBuilder<CruisePriceDropAlertDetailEntity> builder)
    {
        builder.ToTable("CruisePriceDropAlertDetails", table =>
        {
            table.HasCheckConstraint("CK_CruisePriceDropDetails_Amounts", "CAST(\"PreviousAmount\" AS REAL) >= 0 AND CAST(\"CurrentAmount\" AS REAL) >= 0 AND CAST(\"Reduction\" AS REAL) > 0");
            table.HasCheckConstraint("CK_CruisePriceDropDetails_Percentage", "CAST(\"PercentageReduction\" AS REAL) BETWEEN 0 AND 100");
            Currency(table, "PreviousCurrency"); Currency(table, "CurrentCurrency");
            Length(table, "PreviousBasis", 500, true); Length(table, "CurrentBasis", 500, true); Length(table, "EvidenceKey", 4000, false);
        });
        builder.HasKey(x => x.CruiseAlertId);
        ConfigureDecimal(builder.Property(x => x.PreviousAmount)); ConfigureDecimal(builder.Property(x => x.CurrentAmount));
        ConfigureDecimal(builder.Property(x => x.Reduction)); ConfigureDecimal(builder.Property(x => x.PercentageReduction));
        builder.Property(x => x.PreviousCurrency).HasMaxLength(3).IsRequired(); builder.Property(x => x.CurrentCurrency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.PreviousBasis).HasMaxLength(500); builder.Property(x => x.CurrentBasis).HasMaxLength(500); builder.Property(x => x.EvidenceKey).HasMaxLength(4000).IsRequired();
        builder.HasOne(x => x.Alert).WithOne(x => x.PriceDropDetails).HasForeignKey<CruisePriceDropAlertDetailEntity>(x => x.CruiseAlertId).OnDelete(DeleteBehavior.Cascade);
    }
    private static void ConfigureDecimal(PropertyBuilder<decimal> property) => property.HasConversion(CruisePersistenceConversions.Decimal).HasMaxLength(64).IsRequired();
    internal static void Currency(TableBuilder<CruisePriceDropAlertDetailEntity> table, string column) => table.HasCheckConstraint($"CK_CruisePriceDropDetails_{column}", $"length(\"{column}\") = 3 AND \"{column}\" GLOB '[A-Z][A-Z][A-Z]'");
    internal static void Length(TableBuilder<CruisePriceDropAlertDetailEntity> table, string column, int maximum, bool optional) => table.HasCheckConstraint($"CK_CruisePriceDropDetails_{column}_Length", optional ? $"\"{column}\" IS NULL OR length(\"{column}\") BETWEEN 1 AND {maximum}" : $"length(\"{column}\") BETWEEN 1 AND {maximum}");
}

public sealed class CruisePromotionAlertDetailEntityConfiguration : IEntityTypeConfiguration<CruisePromotionAlertDetailEntity>
{
    public void Configure(EntityTypeBuilder<CruisePromotionAlertDetailEntity> builder)
    {
        builder.ToTable("CruisePromotionAlertDetails", table =>
        {
            Length(table, "PreviousSummary", CruisePromotionAlertDetails.MaximumSummaryLength, true);
            Length(table, "CurrentSummary", CruisePromotionAlertDetails.MaximumSummaryLength, false); Length(table, "EvidenceKey", 4000, false);
        });
        builder.HasKey(x => x.CruiseAlertId);
        builder.Property(x => x.PreviousSummary).HasMaxLength(CruisePromotionAlertDetails.MaximumSummaryLength);
        builder.Property(x => x.CurrentSummary).HasMaxLength(CruisePromotionAlertDetails.MaximumSummaryLength).IsRequired(); builder.Property(x => x.EvidenceKey).HasMaxLength(4000).IsRequired();
        builder.HasOne(x => x.Alert).WithOne(x => x.PromotionDetails).HasForeignKey<CruisePromotionAlertDetailEntity>(x => x.CruiseAlertId).OnDelete(DeleteBehavior.Cascade);
    }
    private static void Length(TableBuilder<CruisePromotionAlertDetailEntity> table, string column, int maximum, bool optional) => table.HasCheckConstraint($"CK_CruisePromotionDetails_{column}_Length", optional ? $"\"{column}\" IS NULL OR length(\"{column}\") BETWEEN 1 AND {maximum}" : $"length(\"{column}\") BETWEEN 1 AND {maximum}");
}

public sealed class CruiseSavedCriteriaAlertDetailEntityConfiguration : IEntityTypeConfiguration<CruiseSavedCriteriaAlertDetailEntity>
{
    public void Configure(EntityTypeBuilder<CruiseSavedCriteriaAlertDetailEntity> builder)
    {
        builder.ToTable("CruiseSavedCriteriaAlertDetails", table =>
        {
            table.HasCheckConstraint("CK_CruiseSavedCriteriaDetails_Booleans", "\"MonthConfiguredAndMatched\" IN (0,1) AND \"CabinPreferencesUnavailable\" IN (0,1)");
            table.HasCheckConstraint("CK_CruiseSavedCriteriaDetails_Origin", "\"EvidenceOrigin\" BETWEEN 0 AND 1");
            table.HasCheckConstraint("CK_CruiseSavedCriteriaDetails_BudgetTuple", "(\"ConfiguredBudgetAmount\" IS NULL AND \"ConfiguredBudgetCurrency\" IS NULL AND \"ConfiguredBudgetBasis\" IS NULL AND \"MatchedPriceAmount\" IS NULL AND \"MatchedPriceCurrency\" IS NULL AND \"MatchedPriceBasis\" IS NULL) OR (\"ConfiguredBudgetAmount\" IS NOT NULL AND \"ConfiguredBudgetCurrency\" IS NOT NULL AND \"ConfiguredBudgetBasis\" IS NOT NULL AND \"MatchedPriceAmount\" IS NOT NULL AND \"MatchedPriceCurrency\" IS NOT NULL)");
            table.HasCheckConstraint("CK_CruiseSavedCriteriaDetails_Amounts", "(\"ConfiguredBudgetAmount\" IS NULL OR CAST(\"ConfiguredBudgetAmount\" AS REAL) >= 0) AND (\"MatchedPriceAmount\" IS NULL OR CAST(\"MatchedPriceAmount\" AS REAL) >= 0)");
            table.HasCheckConstraint("CK_CruiseSavedCriteriaDetails_BudgetBasis", "\"ConfiguredBudgetBasis\" IS NULL OR \"ConfiguredBudgetBasis\" BETWEEN 0 AND 1");
            table.HasCheckConstraint("CK_CruiseSavedCriteriaDetails_Currencies", "(\"ConfiguredBudgetCurrency\" IS NULL OR (length(\"ConfiguredBudgetCurrency\") = 3 AND \"ConfiguredBudgetCurrency\" GLOB '[A-Z][A-Z][A-Z]')) AND (\"MatchedPriceCurrency\" IS NULL OR (length(\"MatchedPriceCurrency\") = 3 AND \"MatchedPriceCurrency\" GLOB '[A-Z][A-Z][A-Z]'))");
            table.HasCheckConstraint("CK_CruiseSavedCriteriaDetails_Required", "length(\"CriteriaFingerprint\") BETWEEN 1 AND 128 AND length(\"EvidenceKey\") BETWEEN 1 AND 4000");
            table.HasCheckConstraint("CK_CruiseSavedCriteriaDetails_CabinResult", "\"CabinCriterionResult\" BETWEEN 0 AND 2");
            table.HasCheckConstraint("CK_CruiseSavedCriteriaDetails_CabinContext", "\"CabinContextFingerprint\" IS NULL OR (length(\"CabinContextFingerprint\") = 64 AND \"CabinContextFingerprint\" NOT GLOB '*[^0-9a-f]*')");
            table.HasCheckConstraint("CK_CruiseSavedCriteriaDetails_CabinEvidence", "(\"CabinEvidenceKey\" IS NULL AND \"CabinEvidenceTime\" IS NULL) OR (\"CabinEvidenceKey\" IS NOT NULL AND \"CabinEvidenceTime\" IS NOT NULL)");
            table.HasCheckConstraint("CK_CruiseSavedCriteriaDetails_CabinEvidenceKey", $"\"CabinEvidenceKey\" IS NULL OR length(\"CabinEvidenceKey\") BETWEEN 1 AND {CruiseCabinObservation.MaximumEvidenceKeyLength}");
        });
        builder.HasKey(x => x.CruiseAlertId);
        builder.Property(x => x.ConfiguredBudgetAmount).HasConversion(CruisePersistenceConversions.NullableDecimal).HasMaxLength(64);
        builder.Property(x => x.MatchedPriceAmount).HasConversion(CruisePersistenceConversions.NullableDecimal).HasMaxLength(64);
        builder.Property(x => x.ConfiguredBudgetCurrency).HasMaxLength(3); builder.Property(x => x.MatchedPriceCurrency).HasMaxLength(3);
        builder.Property(x => x.MatchedPriceBasis).HasMaxLength(500); builder.Property(x => x.CriteriaFingerprint).HasMaxLength(128).IsRequired(); builder.Property(x => x.EvidenceKey).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.EvidenceTime).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired();
        builder.Property(x => x.CabinContextFingerprint).HasMaxLength(64);
        builder.Property(x => x.CabinEvidenceKey).HasMaxLength(CruiseCabinObservation.MaximumEvidenceKeyLength);
        builder.Property(x => x.CabinEvidenceTime).HasConversion(CruisePersistenceConversions.NullableDateTimeOffset).HasMaxLength(35);
        builder.HasOne(x => x.Alert).WithOne(x => x.SavedCriteriaDetails).HasForeignKey<CruiseSavedCriteriaAlertDetailEntity>(x => x.CruiseAlertId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CruiseSavedCriteriaAlertCabinEntityConfiguration : IEntityTypeConfiguration<CruiseSavedCriteriaAlertCabinEntity>
{
    public void Configure(EntityTypeBuilder<CruiseSavedCriteriaAlertCabinEntity> builder)
    {
        builder.ToTable("CruiseSavedCriteriaAlertCabins", table =>
        {
            table.HasCheckConstraint("CK_CruiseSavedCriteriaAlertCabins_CabinType", "\"CabinType\" BETWEEN 0 AND 4");
            table.HasCheckConstraint("CK_CruiseSavedCriteriaAlertCabins_IsMatched", "\"IsMatched\" IN (0,1)");
        });
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.Details).WithMany(x => x.Cabins).HasForeignKey(x => x.CruiseAlertId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.CruiseAlertId, x.CabinType }).IsUnique().HasDatabaseName("UX_CruiseSavedCriteriaAlertCabins_Alert_Cabin");
    }
}

public sealed class CruiseCabinAvailabilityAlertDetailEntityConfiguration : IEntityTypeConfiguration<CruiseCabinAvailabilityAlertDetailEntity>
{
    public void Configure(EntityTypeBuilder<CruiseCabinAvailabilityAlertDetailEntity> builder)
    {
        builder.ToTable("CruiseCabinAvailabilityAlertDetails", table =>
        {
            table.HasCheckConstraint("CK_CruiseCabinAvailabilityDetails_CabinType", "\"CabinType\" BETWEEN 0 AND 4");
            table.HasCheckConstraint("CK_CruiseCabinAvailabilityDetails_Coverage", "\"Coverage\" BETWEEN 0 AND 1");
            table.HasCheckConstraint("CK_CruiseCabinAvailabilityDetails_Transition", "(\"PreviousState\" = 2 AND \"CurrentState\" = 1 AND \"Direction\" = 0) OR (\"PreviousState\" = 1 AND \"CurrentState\" = 2 AND \"Direction\" = 1)");
            table.HasCheckConstraint("CK_CruiseCabinAvailabilityDetails_ContextFingerprint", "length(\"ContextFingerprint\") = 64 AND \"ContextFingerprint\" NOT GLOB '*[^0-9a-f]*'");
            table.HasCheckConstraint("CK_CruiseCabinAvailabilityDetails_StateFingerprint", "length(\"StateFingerprint\") = 64 AND \"StateFingerprint\" NOT GLOB '*[^0-9a-f]*'");
            table.HasCheckConstraint("CK_CruiseCabinAvailabilityDetails_EvidenceKey", $"length(\"EvidenceKey\") BETWEEN 1 AND {CruiseCabinObservation.MaximumEvidenceKeyLength}");
        });
        builder.HasKey(x => x.CruiseAlertId);
        builder.Property(x => x.ContextFingerprint).HasMaxLength(64).IsRequired();
        builder.Property(x => x.StateFingerprint).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EvidenceKey).HasMaxLength(CruiseCabinObservation.MaximumEvidenceKeyLength).IsRequired();
        builder.Property(x => x.EvidenceTime).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired();
        builder.HasOne(x => x.Alert).WithOne(x => x.CabinAvailabilityDetails).HasForeignKey<CruiseCabinAvailabilityAlertDetailEntity>(x => x.CruiseAlertId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CruiseNewItineraryAlertDetailEntityConfiguration : IEntityTypeConfiguration<CruiseNewItineraryAlertDetailEntity>
{
    public void Configure(EntityTypeBuilder<CruiseNewItineraryAlertDetailEntity> builder)
    {
        builder.ToTable("CruiseNewItineraryAlertDetails", table =>
        {
            table.HasCheckConstraint("CK_CruiseNewItineraryDetails_Hashes", "length(\"ScopeFingerprint\") = 64 AND \"ScopeFingerprint\" NOT GLOB '*[^0-9a-f]*' AND length(\"CheckEvidenceKey\") = 64 AND \"CheckEvidenceKey\" NOT GLOB '*[^0-9a-f]*' AND length(\"OccurrenceFingerprint\") = 64 AND \"OccurrenceFingerprint\" NOT GLOB '*[^0-9a-f]*' AND length(\"FirstObservedEventKey\") = 64 AND \"FirstObservedEventKey\" NOT GLOB '*[^0-9a-f]*'");
            table.HasCheckConstraint("CK_CruiseNewItineraryDetails_Duration", "\"DurationNights\" IS NULL OR \"DurationNights\" BETWEEN 1 AND 365");
            Length(table, "OperatorId", 200, false); Length(table, "ProviderItineraryId", CruiseItineraryKey.MaximumProviderItineraryIdLength, false);
            Length(table, "ProviderEvidenceKey", CruiseItineraryOccurrence.MaximumSummaryLength, false);
            Length(table, "Title", CruiseItineraryOccurrence.MaximumDisplayLength, true); Length(table, "ShipName", CruiseItineraryOccurrence.MaximumDisplayLength, true);
            Length(table, "DeparturePort", CruiseItineraryOccurrence.MaximumDisplayLength, true); Length(table, "ItinerarySummary", CruiseItineraryOccurrence.MaximumSummaryLength, true);
            Length(table, "SourceReference", CruiseItineraryOccurrence.MaximumSourceReferenceLength, true);
        });
        builder.HasKey(x => x.CruiseAlertId);
        builder.Property(x => x.OperatorId).HasMaxLength(200).IsRequired(); builder.Property(x => x.ProviderItineraryId).HasMaxLength(CruiseItineraryKey.MaximumProviderItineraryIdLength).IsRequired();
        builder.Property(x => x.ScopeFingerprint).HasMaxLength(64).IsRequired(); builder.Property(x => x.CheckEvidenceKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.OccurrenceFingerprint).HasMaxLength(64).IsRequired(); builder.Property(x => x.ProviderEvidenceKey).HasMaxLength(CruiseItineraryOccurrence.MaximumSummaryLength).IsRequired();
        builder.Property(x => x.FirstObservedEventKey).HasMaxLength(64).IsRequired(); builder.Property(x => x.FirstObservedAt).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(CruiseItineraryOccurrence.MaximumDisplayLength); builder.Property(x => x.ShipName).HasMaxLength(CruiseItineraryOccurrence.MaximumDisplayLength);
        builder.Property(x => x.DepartureDate).HasConversion(CruisePersistenceConversions.NullableDateOnly).HasMaxLength(10); builder.Property(x => x.DeparturePort).HasMaxLength(CruiseItineraryOccurrence.MaximumDisplayLength);
        builder.Property(x => x.ItinerarySummary).HasMaxLength(CruiseItineraryOccurrence.MaximumSummaryLength); builder.Property(x => x.SourceReference).HasMaxLength(CruiseItineraryOccurrence.MaximumSourceReferenceLength);
        builder.HasOne(x => x.Alert).WithOne(x => x.NewItineraryDetails).HasForeignKey<CruiseNewItineraryAlertDetailEntity>(x => x.CruiseAlertId).OnDelete(DeleteBehavior.Cascade);
    }
    private static void Length(TableBuilder<CruiseNewItineraryAlertDetailEntity> table, string column, int maximum, bool optional) =>
        table.HasCheckConstraint($"CK_CruiseNewItineraryDetails_{column}_Length", optional ? $"\"{column}\" IS NULL OR length(\"{column}\") BETWEEN 1 AND {maximum}" : $"length(\"{column}\") BETWEEN 1 AND {maximum}");
}

public sealed class CruiseAlertSettingsEntityConfiguration : IEntityTypeConfiguration<CruiseAlertSettingsEntity>
{
    public void Configure(EntityTypeBuilder<CruiseAlertSettingsEntity> builder)
    {
        builder.ToTable("CruiseAlertSettings", table =>
        {
            table.HasCheckConstraint("CK_CruiseAlertSettings_Singleton", "\"Id\" = 1");
            table.HasCheckConstraint("CK_CruiseAlertSettings_Booleans", "\"PriceDropEnabled\" IN (0,1) AND \"PromotionEnabled\" IN (0,1) AND \"SavedCriteriaEnabled\" IN (0,1) AND \"CabinAvailabilityEnabled\" IN (0,1) AND \"NewItineraryEnabled\" IN (0,1)");
            table.HasCheckConstraint("CK_CruiseAlertSettings_Percentage", "CAST(\"MinimumPriceDropPercentage\" AS REAL) BETWEEN 0 AND 100");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CabinAvailabilityEnabled).HasDefaultValue(true);
        builder.Property(x => x.NewItineraryEnabled).HasDefaultValue(true);
        builder.Property(x => x.MinimumPriceDropPercentage).HasConversion(CruisePersistenceConversions.Decimal).HasMaxLength(64).IsRequired();
    }
}

public sealed class SavedCruiseCriteriaEvaluationStateEntityConfiguration : IEntityTypeConfiguration<SavedCruiseCriteriaEvaluationStateEntity>
{
    public void Configure(EntityTypeBuilder<SavedCruiseCriteriaEvaluationStateEntity> builder)
    {
        builder.ToTable("SavedCruiseCriteriaEvaluationStates", table =>
        {
            table.HasCheckConstraint("CK_SavedCriteriaStates_Duration", "\"DurationNights\" > 0");
            table.HasCheckConstraint("CK_SavedCriteriaStates_Result", "\"Result\" BETWEEN 0 AND 2");
            table.HasCheckConstraint("CK_SavedCriteriaStates_Required", "length(\"OperatorId\") BETWEEN 1 AND 200 AND length(\"ShipName\") BETWEEN 1 AND 500 AND length(\"CriteriaFingerprint\") BETWEEN 1 AND 128 AND length(\"EvidenceKey\") BETWEEN 1 AND 4000");
        });
        builder.HasKey(x => x.Id); builder.Property(x => x.OperatorId).HasMaxLength(200).IsRequired(); builder.Property(x => x.ShipName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DepartureDate).HasConversion(CruisePersistenceConversions.DateOnly).HasMaxLength(10).IsRequired(); builder.Property(x => x.CriteriaFingerprint).HasMaxLength(128).IsRequired(); builder.Property(x => x.EvidenceKey).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.EvidenceTime).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired();
        builder.HasIndex(x => new { x.OperatorId, x.ShipName, x.DepartureDate, x.DurationNights, x.CriteriaFingerprint }).IsUnique().HasDatabaseName("UX_SavedCriteriaStates_Sailing_Fingerprint");
    }
}
