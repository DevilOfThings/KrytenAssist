using KrytenAssist.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class KrytenAssistDbContext : DbContext
{
    public KrytenAssistDbContext(DbContextOptions<KrytenAssistDbContext> options)
        : base(options)
    {
    }

    public DbSet<PromptCard> PromptCards => Set<PromptCard>();
    public DbSet<CruiseHistoryEntity> CruiseHistories => Set<CruiseHistoryEntity>();
    public DbSet<CruiseObservationEntity> CruiseObservations => Set<CruiseObservationEntity>();
    public DbSet<CruiseObservationPriceEntity> CruiseObservationPrices => Set<CruiseObservationPriceEntity>();
    public DbSet<SavedCruiseEntity> SavedCruises => Set<SavedCruiseEntity>();
    public DbSet<FavouriteCruiseShipEntity> FavouriteCruiseShips => Set<FavouriteCruiseShipEntity>();
    public DbSet<CruisePreferenceProfileEntity> CruisePreferenceProfiles => Set<CruisePreferenceProfileEntity>();
    public DbSet<CruisePreferenceMonthEntity> CruisePreferenceMonths => Set<CruisePreferenceMonthEntity>();
    public DbSet<CruisePreferenceCabinEntity> CruisePreferenceCabins => Set<CruisePreferenceCabinEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KrytenAssistDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
