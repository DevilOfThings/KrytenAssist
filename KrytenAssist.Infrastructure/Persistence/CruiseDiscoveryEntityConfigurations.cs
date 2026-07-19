using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KrytenAssist.Infrastructure.Persistence;

internal static class CruiseDiscoverySchema
{
    internal static void Hash<T>(TableBuilder<T> table, string column) where T : class =>
        table.HasCheckConstraint($"CK_{typeof(T).Name}_{column}", $"length(\"{column}\") = 64 AND \"{column}\" NOT GLOB '*[^0-9a-f]*'");
    internal static void Required<T>(TableBuilder<T> table, string column, int maximum) where T : class =>
        table.HasCheckConstraint($"CK_{typeof(T).Name}_{column}_Length", $"length(\"{column}\") BETWEEN 1 AND {maximum}");
    internal static void Optional<T>(TableBuilder<T> table, string column, int maximum) where T : class =>
        table.HasCheckConstraint($"CK_{typeof(T).Name}_{column}_Length", $"\"{column}\" IS NULL OR length(\"{column}\") BETWEEN 1 AND {maximum}");
}

public sealed class CruiseDiscoveryScopeEntityConfiguration : IEntityTypeConfiguration<CruiseDiscoveryScopeEntity>
{
    public void Configure(EntityTypeBuilder<CruiseDiscoveryScopeEntity> b)
    {
        b.ToTable("CruiseDiscoveryScopes", t => { CruiseDiscoverySchema.Hash(t, "ScopeFingerprint"); CruiseDiscoverySchema.Required(t, "RetailSourceId", 200); CruiseDiscoverySchema.Required(t, "RetailSourceName", 500); CruiseDiscoverySchema.Required(t, "OperatorId", 200); t.HasCheckConstraint("CK_CruiseDiscoveryScopes_Surface", "\"Surface\" = 0"); t.HasCheckConstraint("CK_CruiseDiscoveryScopes_Version", "\"CaptureContractVersion\" BETWEEN 1 AND 1000"); t.HasCheckConstraint("CK_CruiseDiscoveryScopes_Time", "\"FirstCheckedAtUtcTicks\" <= \"LastCheckedAtUtcTicks\""); });
        b.HasKey(x => x.Id); b.Property(x => x.ScopeFingerprint).HasMaxLength(64).IsRequired(); b.Property(x => x.RetailSourceId).HasMaxLength(200).IsRequired(); b.Property(x => x.RetailSourceName).HasMaxLength(500).IsRequired(); b.Property(x => x.OperatorId).HasMaxLength(200).IsRequired();
        b.Property(x => x.FirstCheckedAt).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired(); b.Property(x => x.LastCheckedAt).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired();
        b.HasIndex(x => x.ScopeFingerprint).IsUnique().HasDatabaseName("UX_CruiseDiscoveryScopes_Fingerprint"); b.HasIndex(x => new { x.LastCheckedAtUtcTicks, x.ScopeFingerprint }).HasDatabaseName("IX_CruiseDiscoveryScopes_LastCheck");
    }
}

public sealed class CruiseDiscoveryScopeCriterionEntityConfiguration : IEntityTypeConfiguration<CruiseDiscoveryScopeCriterionEntity>
{
    public void Configure(EntityTypeBuilder<CruiseDiscoveryScopeCriterionEntity> b)
    {
        b.ToTable("CruiseDiscoveryScopeCriteria", t => { CruiseDiscoverySchema.Required(t, "Name", CruiseDiscoveryCriterion.MaximumNameLength); t.HasCheckConstraint("CK_CruiseDiscoveryScopeCriteria_State", "\"State\" BETWEEN 0 AND 1"); });
        b.HasKey(x => x.Id); b.Property(x => x.Name).HasMaxLength(CruiseDiscoveryCriterion.MaximumNameLength).IsRequired(); b.HasOne(x => x.Scope).WithMany(x => x.Criteria).HasForeignKey(x => x.CruiseDiscoveryScopeId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x => new { x.CruiseDiscoveryScopeId, x.Name }).IsUnique().HasDatabaseName("UX_CruiseDiscoveryScopeCriteria_Scope_Name");
    }
}

public sealed class CruiseDiscoveryScopeCriterionValueEntityConfiguration : IEntityTypeConfiguration<CruiseDiscoveryScopeCriterionValueEntity>
{
    public void Configure(EntityTypeBuilder<CruiseDiscoveryScopeCriterionValueEntity> b)
    {
        b.ToTable("CruiseDiscoveryScopeCriterionValues", t => { CruiseDiscoverySchema.Required(t, "Value", CruiseDiscoveryCriterion.MaximumValueLength); t.HasCheckConstraint("CK_CruiseDiscoveryScopeCriterionValues_Order", "\"DisplayOrder\" >= 0"); });
        b.HasKey(x => x.Id); b.Property(x => x.Value).HasMaxLength(CruiseDiscoveryCriterion.MaximumValueLength).IsRequired(); b.HasOne(x => x.Criterion).WithMany(x => x.Values).HasForeignKey(x => x.CruiseDiscoveryScopeCriterionId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x => new { x.CruiseDiscoveryScopeCriterionId, x.DisplayOrder }).IsUnique().HasDatabaseName("UX_CruiseDiscoveryScopeCriterionValues_Criterion_Order"); b.HasIndex(x => new { x.CruiseDiscoveryScopeCriterionId, x.Value }).IsUnique().HasDatabaseName("UX_CruiseDiscoveryScopeCriterionValues_Criterion_Value");
    }
}

public sealed class CruiseDiscoveryCheckEntityConfiguration : IEntityTypeConfiguration<CruiseDiscoveryCheckEntity>
{
    public void Configure(EntityTypeBuilder<CruiseDiscoveryCheckEntity> b)
    {
        b.ToTable("CruiseDiscoveryChecks", t => { CruiseDiscoverySchema.Hash(t, "EvidenceKey"); t.HasCheckConstraint("CK_CruiseDiscoveryChecks_Truncated", "\"WasTruncated\" IN (0,1)"); t.HasCheckConstraint("CK_CruiseDiscoveryChecks_Accepted", "\"AcceptedCount\" BETWEEN 1 AND 10"); t.HasCheckConstraint("CK_CruiseDiscoveryChecks_Rejected", "\"RejectedCount\" BETWEEN 0 AND 10"); });
        b.HasKey(x => x.Id); b.Property(x => x.EvidenceKey).HasMaxLength(64).IsRequired(); b.Property(x => x.ObservedAt).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired(); b.HasOne(x => x.Scope).WithMany(x => x.Checks).HasForeignKey(x => x.CruiseDiscoveryScopeId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x => x.EvidenceKey).IsUnique().HasDatabaseName("UX_CruiseDiscoveryChecks_EvidenceKey"); b.HasIndex(x => new { x.ObservedAtUtcTicks, x.EvidenceKey }).HasDatabaseName("IX_CruiseDiscoveryChecks_Observed");
    }
}

public sealed class CruiseDiscoveryOccurrenceEntityConfiguration : IEntityTypeConfiguration<CruiseDiscoveryOccurrenceEntity>
{
    public void Configure(EntityTypeBuilder<CruiseDiscoveryOccurrenceEntity> b)
    {
        b.ToTable("CruiseDiscoveryOccurrences", t => { CruiseDiscoverySchema.Hash(t, "CatalogueKey"); CruiseDiscoverySchema.Hash(t, "OccurrenceFingerprint"); CruiseDiscoverySchema.Required(t, "OperatorId", 200); CruiseDiscoverySchema.Required(t, "ProviderItineraryId", CruiseItineraryKey.MaximumProviderItineraryIdLength); CruiseDiscoverySchema.Required(t, "RetailSourceId", 200); CruiseDiscoverySchema.Required(t, "RetailSourceName", 500); CruiseDiscoverySchema.Optional(t, "Title", 1000); CruiseDiscoverySchema.Optional(t, "ShipName", 1000); CruiseDiscoverySchema.Optional(t, "DeparturePort", 1000); CruiseDiscoverySchema.Optional(t, "ItinerarySummary", 4000); CruiseDiscoverySchema.Optional(t, "ProviderOfferId", 1000); CruiseDiscoverySchema.Required(t, "EvidenceKey", 4000); CruiseDiscoverySchema.Optional(t, "SourceReference", 4000); t.HasCheckConstraint("CK_CruiseDiscoveryOccurrences_Duration", "\"DurationNights\" IS NULL OR \"DurationNights\" BETWEEN 1 AND 365"); });
        b.HasKey(x => x.Id); b.Property(x => x.CatalogueKey).HasMaxLength(64).IsRequired(); b.Property(x => x.OccurrenceFingerprint).HasMaxLength(64).IsRequired(); b.Property(x => x.OperatorId).HasMaxLength(200).IsRequired(); b.Property(x => x.ProviderItineraryId).HasMaxLength(1000).IsRequired(); b.Property(x => x.RetailSourceId).HasMaxLength(200).IsRequired(); b.Property(x => x.RetailSourceName).HasMaxLength(500).IsRequired(); b.Property(x => x.Title).HasMaxLength(1000); b.Property(x => x.ShipName).HasMaxLength(1000); b.Property(x => x.DepartureDate).HasConversion(CruisePersistenceConversions.NullableDateOnly).HasMaxLength(10); b.Property(x => x.DeparturePort).HasMaxLength(1000); b.Property(x => x.ItinerarySummary).HasMaxLength(4000); b.Property(x => x.ProviderOfferId).HasMaxLength(1000); b.Property(x => x.ObservedAt).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired(); b.Property(x => x.EvidenceKey).HasMaxLength(4000).IsRequired(); b.Property(x => x.SourceReference).HasMaxLength(4000); b.HasOne(x => x.Check).WithMany(x => x.Occurrences).HasForeignKey(x => x.CruiseDiscoveryCheckId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x => new { x.CruiseDiscoveryCheckId, x.CatalogueKey }).IsUnique().HasDatabaseName("UX_CruiseDiscoveryOccurrences_Check_Catalogue"); b.HasIndex(x => new { x.CatalogueKey, x.ObservedAtUtcTicks, x.OccurrenceFingerprint }).HasDatabaseName("IX_CruiseDiscoveryOccurrences_Catalogue_Observed");
    }
}

public sealed class CruiseDiscoveryRejectionEntityConfiguration : IEntityTypeConfiguration<CruiseDiscoveryRejectionEntity>
{
    public void Configure(EntityTypeBuilder<CruiseDiscoveryRejectionEntity> b)
    {
        b.ToTable("CruiseDiscoveryRejections", t => { t.HasCheckConstraint("CK_CruiseDiscoveryRejections_Order", "\"DisplayOrder\" >= 0"); CruiseDiscoverySchema.Required(t, "CandidateKey", 1000); CruiseDiscoverySchema.Required(t, "Reason", 1000); }); b.HasKey(x => x.Id); b.Property(x => x.CandidateKey).HasMaxLength(1000).IsRequired(); b.Property(x => x.Reason).HasMaxLength(1000).IsRequired(); b.HasOne(x => x.Check).WithMany(x => x.Rejections).HasForeignKey(x => x.CruiseDiscoveryCheckId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x => new { x.CruiseDiscoveryCheckId, x.DisplayOrder }).IsUnique().HasDatabaseName("UX_CruiseDiscoveryRejections_Check_Order");
    }
}

public sealed class CruiseItineraryCatalogueEntityConfiguration : IEntityTypeConfiguration<CruiseItineraryCatalogueEntity>
{
    public void Configure(EntityTypeBuilder<CruiseItineraryCatalogueEntity> b)
    {
        b.ToTable("CruiseItineraryCatalogue", t => { CruiseDiscoverySchema.Hash(t, "CatalogueKey"); CruiseDiscoverySchema.Required(t, "RetailSourceId", 200); CruiseDiscoverySchema.Required(t, "RetailSourceName", 500); CruiseDiscoverySchema.Required(t, "OperatorId", 200); CruiseDiscoverySchema.Required(t, "ProviderItineraryId", 1000); t.HasCheckConstraint("CK_CruiseItineraryCatalogue_EventKey", "\"FirstObservedEventKey\" IS NULL OR (length(\"FirstObservedEventKey\") = 64 AND \"FirstObservedEventKey\" NOT GLOB '*[^0-9a-f]*')"); t.HasCheckConstraint("CK_CruiseItineraryCatalogue_Time", "\"FirstSeenAtUtcTicks\" <= \"LastSeenAtUtcTicks\""); });
        b.HasKey(x => x.Id); b.Property(x => x.CatalogueKey).HasMaxLength(64).IsRequired(); b.Property(x => x.RetailSourceId).HasMaxLength(200).IsRequired(); b.Property(x => x.RetailSourceName).HasMaxLength(500).IsRequired(); b.Property(x => x.OperatorId).HasMaxLength(200).IsRequired(); b.Property(x => x.ProviderItineraryId).HasMaxLength(1000).IsRequired(); b.Property(x => x.FirstSeenAt).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired(); b.Property(x => x.LastSeenAt).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired(); b.Property(x => x.FirstObservedEventKey).HasMaxLength(64); b.HasOne(x => x.FirstOccurrence).WithMany().HasForeignKey(x => x.FirstOccurrenceId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x => x.LatestOccurrence).WithMany().HasForeignKey(x => x.LatestOccurrenceId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => x.CatalogueKey).IsUnique().HasDatabaseName("UX_CruiseItineraryCatalogue_Key"); b.HasIndex(x => x.FirstObservedEventKey).IsUnique().HasFilter("\"FirstObservedEventKey\" IS NOT NULL").HasDatabaseName("UX_CruiseItineraryCatalogue_EventKey"); b.HasIndex(x => new { x.FirstSeenAtUtcTicks, x.CatalogueKey }).HasDatabaseName("IX_CruiseItineraryCatalogue_FirstSeen");
    }
}
