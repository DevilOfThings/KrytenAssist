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
    public DbSet<CruiseAlertEntity> CruiseAlerts => Set<CruiseAlertEntity>();
    public DbSet<CruisePriceDropAlertDetailEntity> CruisePriceDropAlertDetails => Set<CruisePriceDropAlertDetailEntity>();
    public DbSet<CruisePromotionAlertDetailEntity> CruisePromotionAlertDetails => Set<CruisePromotionAlertDetailEntity>();
    public DbSet<CruiseSavedCriteriaAlertDetailEntity> CruiseSavedCriteriaAlertDetails => Set<CruiseSavedCriteriaAlertDetailEntity>();
    public DbSet<CruiseSavedCriteriaAlertCabinEntity> CruiseSavedCriteriaAlertCabins => Set<CruiseSavedCriteriaAlertCabinEntity>();
    public DbSet<CruiseCabinAvailabilityAlertDetailEntity> CruiseCabinAvailabilityAlertDetails => Set<CruiseCabinAvailabilityAlertDetailEntity>();
    public DbSet<CruiseNewItineraryAlertDetailEntity> CruiseNewItineraryAlertDetails => Set<CruiseNewItineraryAlertDetailEntity>();
    public DbSet<CruiseAlertSettingsEntity> CruiseAlertSettings => Set<CruiseAlertSettingsEntity>();
    public DbSet<SavedCruiseCriteriaEvaluationStateEntity> SavedCruiseCriteriaEvaluationStates => Set<SavedCruiseCriteriaEvaluationStateEntity>();
    public DbSet<CruiseCabinSeriesEntity> CruiseCabinSeries => Set<CruiseCabinSeriesEntity>();
    public DbSet<CruiseCabinContextChildAgeEntity> CruiseCabinContextChildAges => Set<CruiseCabinContextChildAgeEntity>();
    public DbSet<CruiseCabinObservationEntity> CruiseCabinObservations => Set<CruiseCabinObservationEntity>();
    public DbSet<CruiseCabinObservationStateEntity> CruiseCabinObservationStates => Set<CruiseCabinObservationStateEntity>();
    public DbSet<CruiseDiscoveryScopeEntity> CruiseDiscoveryScopes => Set<CruiseDiscoveryScopeEntity>();
    public DbSet<CruiseDiscoveryScopeCriterionEntity> CruiseDiscoveryScopeCriteria => Set<CruiseDiscoveryScopeCriterionEntity>();
    public DbSet<CruiseDiscoveryScopeCriterionValueEntity> CruiseDiscoveryScopeCriterionValues => Set<CruiseDiscoveryScopeCriterionValueEntity>();
    public DbSet<CruiseDiscoveryCheckEntity> CruiseDiscoveryChecks => Set<CruiseDiscoveryCheckEntity>();
    public DbSet<CruiseDiscoveryOccurrenceEntity> CruiseDiscoveryOccurrences => Set<CruiseDiscoveryOccurrenceEntity>();
    public DbSet<CruiseDiscoveryRejectionEntity> CruiseDiscoveryRejections => Set<CruiseDiscoveryRejectionEntity>();
    public DbSet<CruiseItineraryCatalogueEntity> CruiseItineraryCatalogue => Set<CruiseItineraryCatalogueEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KrytenAssistDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
