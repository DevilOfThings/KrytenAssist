using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class CruisePreferenceProfileEntityConfiguration : IEntityTypeConfiguration<CruisePreferenceProfileEntity>
{
    public void Configure(EntityTypeBuilder<CruisePreferenceProfileEntity> builder)
    {
        builder.ToTable("CruisePreferenceProfiles", table =>
        {
            table.HasCheckConstraint("CK_CruisePreferenceProfiles_Singleton", "\"Id\" = 1");
            table.HasCheckConstraint("CK_CruisePreferenceProfiles_BudgetTuple", "(\"MaximumBudgetAmount\" IS NULL AND \"MaximumBudgetCurrency\" IS NULL AND \"MaximumBudgetBasis\" IS NULL) OR (\"MaximumBudgetAmount\" IS NOT NULL AND \"MaximumBudgetCurrency\" IS NOT NULL AND \"MaximumBudgetBasis\" IS NOT NULL)");
            table.HasCheckConstraint("CK_CruisePreferenceProfiles_BudgetAmount", "\"MaximumBudgetAmount\" IS NULL OR CAST(\"MaximumBudgetAmount\" AS REAL) >= 0");
            table.HasCheckConstraint("CK_CruisePreferenceProfiles_BudgetCurrency", "\"MaximumBudgetCurrency\" IS NULL OR (length(\"MaximumBudgetCurrency\") = 3 AND \"MaximumBudgetCurrency\" GLOB '[A-Z][A-Z][A-Z]')");
            table.HasCheckConstraint("CK_CruisePreferenceProfiles_BudgetBasis", "\"MaximumBudgetBasis\" IS NULL OR \"MaximumBudgetBasis\" BETWEEN 0 AND 1");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MaximumBudgetAmount).HasPrecision(18, 2);
        builder.Property(x => x.MaximumBudgetCurrency).HasMaxLength(3);
    }
}

public sealed class CruisePreferenceMonthEntityConfiguration : IEntityTypeConfiguration<CruisePreferenceMonthEntity>
{
    public void Configure(EntityTypeBuilder<CruisePreferenceMonthEntity> builder)
    {
        builder.ToTable("CruisePreferenceMonths", table => table.HasCheckConstraint("CK_CruisePreferenceMonths_Month", "\"Month\" BETWEEN 1 AND 12"));
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.Profile).WithMany(x => x.Months).HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.ProfileId, x.Month }).IsUnique().HasDatabaseName("UX_CruisePreferenceMonths_Profile_Month");
    }
}

public sealed class CruisePreferenceCabinEntityConfiguration : IEntityTypeConfiguration<CruisePreferenceCabinEntity>
{
    public void Configure(EntityTypeBuilder<CruisePreferenceCabinEntity> builder)
    {
        builder.ToTable("CruisePreferenceCabins", table => table.HasCheckConstraint("CK_CruisePreferenceCabins_Cabin", "\"Cabin\" BETWEEN 0 AND 4"));
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.Profile).WithMany(x => x.Cabins).HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.ProfileId, x.Cabin }).IsUnique().HasDatabaseName("UX_CruisePreferenceCabins_Profile_Cabin");
    }
}
