using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class SavedCruiseEntityConfiguration : IEntityTypeConfiguration<SavedCruiseEntity>
{
    public void Configure(EntityTypeBuilder<SavedCruiseEntity> builder)
    {
        builder.ToTable("SavedCruises", table =>
        {
            table.HasCheckConstraint("CK_SavedCruises_Duration", "\"DurationNights\" > 0");
            table.HasCheckConstraint("CK_SavedCruises_Status", "\"Status\" BETWEEN 0 AND 1");
            table.HasCheckConstraint("CK_SavedCruises_Interest", "\"InterestLevel\" IS NULL OR \"InterestLevel\" BETWEEN 0 AND 1");
            foreach (var name in new[] { "OverallRating", "ItineraryRating", "ShipRating", "ValueRating" })
                table.HasCheckConstraint($"CK_SavedCruises_{name}", $"\"{name}\" IS NULL OR \"{name}\" BETWEEN 1 AND 5");
            table.HasCheckConstraint("CK_SavedCruises_Price", "CAST(\"DisplayedPriceAmount\" AS REAL) >= 0");
            table.HasCheckConstraint("CK_SavedCruises_Currency", "length(\"DisplayedPriceCurrency\") = 3 AND \"DisplayedPriceCurrency\" GLOB '[A-Z][A-Z][A-Z]'");
            Length(table, "OperatorId", 200, false); Length(table, "ShipName", 500, false);
            Length(table, "Title", SavedCruiseSnapshot.MaximumTitleLength, false);
            Length(table, "OperatorName", SavedCruiseSnapshot.MaximumOperatorNameLength, false);
            Length(table, "DeparturePort", SavedCruiseSnapshot.MaximumDeparturePortLength, true);
            Length(table, "ItinerarySummary", SavedCruiseSnapshot.MaximumItineraryLength, true);
            Length(table, "DisplayedPriceBasis", 500, true);
            Length(table, "RetailSourceId", SavedCruiseSnapshot.MaximumRetailSourceIdLength, true);
            Length(table, "RetailSourceName", SavedCruiseSnapshot.MaximumRetailSourceNameLength, true);
            Length(table, "SourceReference", SavedCruiseSnapshot.MaximumSourceReferenceLength, true);
            Length(table, "Notes", CruiseEvaluation.MaximumNotesLength, true);
            table.HasCheckConstraint("CK_SavedCruises_SourcePair", "(\"RetailSourceId\" IS NULL AND \"RetailSourceName\" IS NULL) OR (\"RetailSourceId\" IS NOT NULL AND \"RetailSourceName\" IS NOT NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OperatorId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ShipName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DepartureDate).HasConversion(CruisePersistenceConversions.DateOnly).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(SavedCruiseSnapshot.MaximumTitleLength).IsRequired();
        builder.Property(x => x.OperatorName).HasMaxLength(SavedCruiseSnapshot.MaximumOperatorNameLength).IsRequired();
        builder.Property(x => x.DeparturePort).HasMaxLength(SavedCruiseSnapshot.MaximumDeparturePortLength);
        builder.Property(x => x.ItinerarySummary).HasMaxLength(SavedCruiseSnapshot.MaximumItineraryLength);
        builder.Property(x => x.DisplayedPriceAmount).HasPrecision(18, 2);
        builder.Property(x => x.DisplayedPriceCurrency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.DisplayedPriceBasis).HasMaxLength(500);
        builder.Property(x => x.RetailSourceId).HasMaxLength(SavedCruiseSnapshot.MaximumRetailSourceIdLength);
        builder.Property(x => x.RetailSourceName).HasMaxLength(SavedCruiseSnapshot.MaximumRetailSourceNameLength);
        builder.Property(x => x.SourceReference).HasMaxLength(SavedCruiseSnapshot.MaximumSourceReferenceLength);
        builder.Property(x => x.SavedAt).HasConversion(CruisePersistenceConversions.DateTimeOffset).HasMaxLength(35).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(CruiseEvaluation.MaximumNotesLength);
        builder.HasIndex(x => new { x.OperatorId, x.ShipName, x.DepartureDate, x.DurationNights }).IsUnique().HasDatabaseName("UX_SavedCruises_Sailing");
    }

    private static void Length(TableBuilder<SavedCruiseEntity> table, string column, int maximum, bool optional) =>
        table.HasCheckConstraint($"CK_SavedCruises_{column}_Length", optional ? $"\"{column}\" IS NULL OR length(\"{column}\") BETWEEN 1 AND {maximum}" : $"length(\"{column}\") BETWEEN 1 AND {maximum}");
}
