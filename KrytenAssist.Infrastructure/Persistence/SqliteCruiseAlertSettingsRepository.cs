using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class SqliteCruiseAlertSettingsRepository(KrytenAssistDbContext dbContext) : ICruiseAlertSettingsRepository
{
    private const int ProfileId = 1;
    public async Task<CruiseAlertSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = await dbContext.CruiseAlertSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == ProfileId, cancellationToken);
        return value is null ? new() : new(value.PriceDropEnabled, value.PromotionEnabled, value.SavedCriteriaEnabled, value.MinimumPriceDropPercentage, value.CabinAvailabilityEnabled);
    }

    public async Task SaveAsync(CruiseAlertSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings); cancellationToken.ThrowIfCancellationRequested();
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "CruiseAlertSettings" ("Id", "PriceDropEnabled", "PromotionEnabled", "SavedCriteriaEnabled", "MinimumPriceDropPercentage", "CabinAvailabilityEnabled")
            VALUES ({ProfileId}, {settings.PriceDropEnabled}, {settings.PromotionEnabled}, {settings.SavedCriteriaEnabled}, {settings.MinimumPriceDropPercentage.ToString("G29", System.Globalization.CultureInfo.InvariantCulture)}, {settings.CabinAvailabilityEnabled})
            ON CONFLICT("Id") DO UPDATE SET
              "PriceDropEnabled" = excluded."PriceDropEnabled", "PromotionEnabled" = excluded."PromotionEnabled",
              "SavedCriteriaEnabled" = excluded."SavedCriteriaEnabled", "MinimumPriceDropPercentage" = excluded."MinimumPriceDropPercentage",
              "CabinAvailabilityEnabled" = excluded."CabinAvailabilityEnabled"
            """, cancellationToken);
    }
}
