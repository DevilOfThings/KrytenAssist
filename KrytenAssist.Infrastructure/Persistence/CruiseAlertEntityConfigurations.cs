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
            table.HasCheckConstraint("CK_CruiseAlerts_Type", "\"Type\" BETWEEN 0 AND 2");
            table.HasCheckConstraint("CK_CruiseAlerts_Status", "\"Status\" BETWEEN 0 AND 2");
            table.HasCheckConstraint("CK_CruiseAlerts_Duration", "\"DurationNights\" > 0");
            table.HasCheckConstraint("CK_CruiseAlerts_SourcePair", "(\"RetailSourceId\" IS NULL AND \"RetailSourceName\" IS NULL) OR (\"RetailSourceId\" IS NOT NULL AND \"RetailSourceName\" IS NOT NULL)");
            table.HasCheckConstraint("CK_CruiseAlerts_TypeSource", "(\"Type\" IN (0, 1) AND \"RetailSourceId\" IS NOT NULL) OR (\"Type\" = 2 AND \"RetailSourceId\" IS NULL)");
            Length(table, "OperatorId", 200, false); Length(table, "ShipName", 500, false);
            Length(table, "RetailSourceId", SavedCruiseSnapshot.MaximumRetailSourceIdLength, true);
            Length(table, "RetailSourceName", SavedCruiseSnapshot.MaximumRetailSourceNameLength, true);
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.OperatorId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ShipName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DepartureDate).HasConversion(CruisePersistenceConversions.DateOnly).HasMaxLength(10).IsRequired();
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
        });
        builder.HasKey(x => x.CruiseAlertId);
        builder.Property(x => x.ConfiguredBudgetAmount).HasConversion(CruisePersistenceConversions.NullableDecimal).HasMaxLength(64);
        builder.Property(x => x.MatchedPriceAmount).HasConversion(CruisePersistenceConversions.NullableDecimal).HasMaxLength(64);
        builder.Property(x => x.ConfiguredBudgetCurrency).HasMaxLength(3); builder.Property(x => x.MatchedPriceCurrency).HasMaxLength(3);
        builder.Property(x => x.MatchedPriceBasis).HasMaxLength(500); builder.Property(x => x.CriteriaFingerprint).HasMaxLength(128).IsRequired(); builder.Property(x => x.EvidenceKey).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.EvidenceTime).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired();
        builder.HasOne(x => x.Alert).WithOne(x => x.SavedCriteriaDetails).HasForeignKey<CruiseSavedCriteriaAlertDetailEntity>(x => x.CruiseAlertId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CruiseAlertSettingsEntityConfiguration : IEntityTypeConfiguration<CruiseAlertSettingsEntity>
{
    public void Configure(EntityTypeBuilder<CruiseAlertSettingsEntity> builder)
    {
        builder.ToTable("CruiseAlertSettings", table =>
        {
            table.HasCheckConstraint("CK_CruiseAlertSettings_Singleton", "\"Id\" = 1");
            table.HasCheckConstraint("CK_CruiseAlertSettings_Booleans", "\"PriceDropEnabled\" IN (0,1) AND \"PromotionEnabled\" IN (0,1) AND \"SavedCriteriaEnabled\" IN (0,1)");
            table.HasCheckConstraint("CK_CruiseAlertSettings_Percentage", "CAST(\"MinimumPriceDropPercentage\" AS REAL) BETWEEN 0 AND 100");
        });
        builder.HasKey(x => x.Id); builder.Property(x => x.MinimumPriceDropPercentage).HasConversion(CruisePersistenceConversions.Decimal).HasMaxLength(64).IsRequired();
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
