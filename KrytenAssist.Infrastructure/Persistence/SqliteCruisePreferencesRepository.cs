using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class SqliteCruisePreferencesRepository(KrytenAssistDbContext dbContext) : ICruisePreferencesRepository
{
    private const int ProfileId = 1;

    public async Task<CruisePreferences> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var profile = await dbContext.CruisePreferenceProfiles.AsNoTracking().Include(x => x.Months).Include(x => x.Cabins).SingleOrDefaultAsync(x => x.Id == ProfileId, cancellationToken);
        if (profile is null) return new CruisePreferences();
        CruiseBudget? budget = profile.MaximumBudgetAmount is null ? null : new CruiseBudget(profile.MaximumBudgetAmount.Value, profile.MaximumBudgetCurrency!, (CruiseBudgetBasis)profile.MaximumBudgetBasis!.Value);
        return new CruisePreferences(profile.Months.OrderBy(x => x.Month).Select(x => x.Month), profile.Cabins.OrderBy(x => x.Cabin).Select(x => (CruiseCabinType)x.Cabin), budget);
    }

    public async Task SaveAsync(CruisePreferences value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value); cancellationToken.ThrowIfCancellationRequested();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var profile = await dbContext.CruisePreferenceProfiles.Include(x => x.Months).Include(x => x.Cabins).SingleOrDefaultAsync(x => x.Id == ProfileId, cancellationToken);
        if (profile is null) { profile = new CruisePreferenceProfileEntity { Id = ProfileId }; dbContext.CruisePreferenceProfiles.Add(profile); }
        else { dbContext.CruisePreferenceMonths.RemoveRange(profile.Months); dbContext.CruisePreferenceCabins.RemoveRange(profile.Cabins); }
        profile.MaximumBudgetAmount = value.MaximumBudget?.Amount; profile.MaximumBudgetCurrency = value.MaximumBudget?.Currency; profile.MaximumBudgetBasis = (int?)value.MaximumBudget?.Basis;
        profile.Months = value.DepartureMonths.Select(month => new CruisePreferenceMonthEntity { ProfileId = ProfileId, Month = month }).ToList();
        profile.Cabins = value.PreferredCabins.Select(cabin => new CruisePreferenceCabinEntity { ProfileId = ProfileId, Cabin = (int)cabin }).ToList();
        await dbContext.SaveChangesAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        await transaction.CommitAsync(cancellationToken);
    }
}
