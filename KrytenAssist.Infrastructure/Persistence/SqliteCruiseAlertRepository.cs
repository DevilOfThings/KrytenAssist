using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Application.Cruises;
using KrytenAssist.Core.Cruises;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class SqliteCruiseAlertRepository(KrytenAssistDbContext dbContext) : ICruiseAlertRepository
{
    public async Task<CruiseAlert?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Alert id is required.", nameof(id));
        cancellationToken.ThrowIfCancellationRequested();
        var entity = await CompleteQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<CruiseAlert>> ListAsync(CruiseAlertQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query); cancellationToken.ThrowIfCancellationRequested();
        var values = CompleteQuery();
        if (query.Type is not null) values = values.Where(x => x.Type == (int)query.Type.Value);
        if (query.Status is not null) values = values.Where(x => x.Status == (int)query.Status.Value);
        var entities = await values.OrderByDescending(x => x.EventTimeUtcTicks).ThenByDescending(x => x.CreatedAtUtcTicks)
            .ThenBy(x => x.EventKey).ThenBy(x => x.Id).ToArrayAsync(cancellationToken);
        return Array.AsReadOnly(entities.Select(Map).ToArray());
    }

    public Task<int> CountUnreadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return dbContext.CruiseAlerts.CountAsync(x => x.Status == (int)CruiseAlertStatus.Unread, cancellationToken);
    }

    public async Task<CruiseAlertAddRepositoryResult> AddIfAbsentAsync(CruiseAlert alert, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(alert); cancellationToken.ThrowIfCancellationRequested();
        var known = await FindByEventKeyAsync(alert.EventKey, cancellationToken);
        if (known is not null) return new(false, known);

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            dbContext.CruiseAlerts.Add(ToEntity(alert));
            await dbContext.SaveChangesAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            await transaction.CommitAsync(cancellationToken);
            return new(true, alert);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraint(exception))
        {
            dbContext.ChangeTracker.Clear();
            var existing = await FindByEventKeyAsync(alert.EventKey, cancellationToken);
            if (existing is null) throw;
            return new(false, existing);
        }
    }

    public async Task<bool> UpdateStatusAsync(Guid id, CruiseAlertStatus status, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Alert id is required.", nameof(id));
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        cancellationToken.ThrowIfCancellationRequested();
        return await dbContext.CruiseAlerts.Where(x => x.Id == id)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, (int)status), cancellationToken) > 0;
    }

    private IQueryable<CruiseAlertEntity> CompleteQuery() => dbContext.CruiseAlerts.AsNoTracking()
        .Include(x => x.PriceDropDetails).Include(x => x.PromotionDetails)
        .Include(x => x.SavedCriteriaDetails).ThenInclude(x => x!.Cabins)
        .Include(x => x.CabinAvailabilityDetails).Include(x => x.NewItineraryDetails);

    private async Task<CruiseAlert?> FindByEventKeyAsync(string eventKey, CancellationToken token)
    {
        var entity = await CompleteQuery().SingleOrDefaultAsync(x => x.EventKey == eventKey, token);
        return entity is null ? null : Map(entity);
    }

    private static CruiseAlertEntity ToEntity(CruiseAlert alert)
    {
        var entity = new CruiseAlertEntity
        {
            Id = alert.Id, EventKey = alert.EventKey, Type = (int)alert.Type, Status = (int)alert.Status,
            RetailSourceId = alert.Source?.Id, RetailSourceName = alert.Source?.Name,
            EventTime = alert.EventTime, EventTimeUtcTicks = alert.EventTime.UtcTicks,
            CreatedAt = alert.CreatedAt, CreatedAtUtcTicks = alert.CreatedAt.UtcTicks
        };
        switch (alert.Subject)
        {
            case CruiseSailingAlertSubject sailing:
                entity.OperatorId = sailing.SailingKey.OperatorId; entity.ShipName = sailing.SailingKey.ShipName;
                entity.DepartureDate = sailing.SailingKey.DepartureDate; entity.DurationNights = sailing.SailingKey.DurationNights;
                break;
            case CruiseItineraryAlertSubject itinerary:
                entity.ItineraryOperatorId = itinerary.CatalogueKey.ItineraryKey.OperatorId;
                entity.ProviderItineraryId = itinerary.CatalogueKey.ItineraryKey.ProviderItineraryId;
                break;
            default: throw new InvalidOperationException("Unsupported alert subject.");
        }
        switch (alert.Details)
        {
            case CruisePriceDropAlertDetails detail:
                entity.PriceDropDetails = new CruisePriceDropAlertDetailEntity
                {
                    PreviousAmount = detail.PreviousPrice.Amount, PreviousCurrency = detail.PreviousPrice.Currency, PreviousBasis = detail.PreviousPrice.Basis,
                    CurrentAmount = detail.CurrentPrice.Amount, CurrentCurrency = detail.CurrentPrice.Currency, CurrentBasis = detail.CurrentPrice.Basis,
                    Reduction = detail.Reduction, PercentageReduction = detail.PercentageReduction, EvidenceKey = detail.EvidenceKey
                };
                break;
            case CruisePromotionAlertDetails detail:
                entity.PromotionDetails = new CruisePromotionAlertDetailEntity { PreviousSummary = detail.PreviousSummary, CurrentSummary = detail.CurrentSummary, EvidenceKey = detail.EvidenceKey };
                break;
            case CruiseSavedCriteriaAlertDetails detail:
                entity.SavedCriteriaDetails = new CruiseSavedCriteriaAlertDetailEntity
                {
                    MonthConfiguredAndMatched = detail.MonthConfiguredAndMatched,
                    ConfiguredBudgetAmount = detail.ConfiguredBudget?.Amount, ConfiguredBudgetCurrency = detail.ConfiguredBudget?.Currency, ConfiguredBudgetBasis = (int?)detail.ConfiguredBudget?.Basis,
                    MatchedPriceAmount = detail.MatchedPrice?.Amount, MatchedPriceCurrency = detail.MatchedPrice?.Currency, MatchedPriceBasis = detail.MatchedPrice?.Basis,
                    CriteriaFingerprint = detail.CriteriaFingerprint, EvidenceOrigin = (int)detail.EvidenceOrigin, EvidenceKey = detail.EvidenceKey,
                    EvidenceTime = detail.EvidenceTime, CabinPreferencesUnavailable = detail.CabinPreferencesUnavailable,
                    CabinCriterionResult = (int)detail.CabinCriterionResult,
                    CabinContextFingerprint = detail.CabinContextFingerprint,
                    CabinEvidenceKey = detail.CabinEvidenceKey,
                    CabinEvidenceTime = detail.CabinEvidenceTime,
                    Cabins = detail.ConfiguredCabins.Select(cabin => new CruiseSavedCriteriaAlertCabinEntity
                    {
                        CabinType = (int)cabin,
                        IsMatched = detail.MatchedCabins.Contains(cabin)
                    }).ToList()
                };
                break;
            case CruiseCabinAvailabilityAlertDetails detail:
                entity.CabinAvailabilityDetails = new CruiseCabinAvailabilityAlertDetailEntity
                {
                    CabinType = (int)detail.CabinType, PreviousState = (int)detail.PreviousState,
                    CurrentState = (int)detail.CurrentState, Direction = (int)detail.Direction,
                    ContextFingerprint = detail.ContextFingerprint, Coverage = (int)detail.Coverage,
                    StateFingerprint = detail.StateFingerprint, EvidenceKey = detail.EvidenceKey,
                    EvidenceTime = detail.EvidenceTime
                };
                break;
            case CruiseNewItineraryAlertDetails detail:
                entity.NewItineraryDetails = new CruiseNewItineraryAlertDetailEntity
                {
                    OperatorId = detail.ItineraryKey.OperatorId, ProviderItineraryId = detail.ItineraryKey.ProviderItineraryId,
                    ScopeFingerprint = detail.ScopeFingerprint, CheckEvidenceKey = detail.CheckEvidenceKey,
                    OccurrenceFingerprint = detail.OccurrenceFingerprint, ProviderEvidenceKey = detail.ProviderEvidenceKey,
                    FirstObservedEventKey = detail.FirstObservedEventKey, FirstObservedAt = detail.FirstObservedAt,
                    Title = detail.Title, ShipName = detail.ShipName, DepartureDate = detail.DepartureDate,
                    DurationNights = detail.DurationNights, DeparturePort = detail.DeparturePort,
                    ItinerarySummary = detail.ItinerarySummary, SourceReference = detail.SourceReference
                };
                break;
            default: throw new InvalidOperationException("Unsupported alert details.");
        }
        return entity;
    }

    private static CruiseAlert Map(CruiseAlertEntity entity)
    {
        var source = entity.RetailSourceId is null ? null : new CruiseSource(entity.RetailSourceId, entity.RetailSourceName!);
        var detailCount = new object?[] { entity.PriceDropDetails, entity.PromotionDetails, entity.SavedCriteriaDetails, entity.CabinAvailabilityDetails, entity.NewItineraryDetails }.Count(value => value is not null);
        if (detailCount != 1) throw new InvalidDataException("Persisted alert must have exactly one typed detail payload.");
        CruiseAlertDetails details = (CruiseAlertType)entity.Type switch
        {
            CruiseAlertType.PriceDrop when entity.PriceDropDetails is not null => MapPriceDrop(entity.PriceDropDetails),
            CruiseAlertType.Promotion when entity.PromotionDetails is not null => MapPromotion(entity.PromotionDetails),
            CruiseAlertType.SavedCriteria when entity.SavedCriteriaDetails is not null => MapCriteria(entity.SavedCriteriaDetails),
            CruiseAlertType.CabinAvailability when entity.CabinAvailabilityDetails is not null => MapCabinAvailability(entity.CabinAvailabilityDetails),
            CruiseAlertType.NewItinerary when entity.NewItineraryDetails is not null => MapNewItinerary(entity.NewItineraryDetails),
            _ => throw new InvalidDataException("Persisted alert has an invalid typed detail payload.")
        };
        CruiseAlertSubject subject = (CruiseAlertType)entity.Type == CruiseAlertType.NewItinerary
            ? new CruiseItineraryAlertSubject(new CruiseItineraryCatalogueKey(source!, new CruiseItineraryKey(entity.ItineraryOperatorId!, entity.ProviderItineraryId!)))
            : new CruiseSailingAlertSubject(new CruiseSailingKey(entity.OperatorId!, entity.ShipName!, entity.DepartureDate!.Value, entity.DurationNights!.Value));
        var evidenceKey = details switch { CruisePriceDropAlertDetails x => x.EvidenceKey, CruisePromotionAlertDetails x => x.EvidenceKey, CruiseSavedCriteriaAlertDetails x => x.EvidenceKey, CruiseCabinAvailabilityAlertDetails x => $"{x.StateFingerprint}:{(int)x.CabinType}", CruiseNewItineraryAlertDetails x => x.FirstObservedEventKey, _ => throw new InvalidOperationException() };
        var criteria = (details as CruiseSavedCriteriaAlertDetails)?.CriteriaFingerprint;
        var candidate = new CruiseAlertCandidate((CruiseAlertType)entity.Type, subject, source, details, entity.EventTime, evidenceKey, criteria);
        if (!string.Equals(candidate.EventKey, entity.EventKey, StringComparison.Ordinal)) throw new InvalidDataException("Persisted alert event identity is inconsistent.");
        return new CruiseAlert(entity.Id, candidate, entity.CreatedAt, (CruiseAlertStatus)entity.Status);
    }

    private static CruisePriceDropAlertDetails MapPriceDrop(CruisePriceDropAlertDetailEntity entity)
    {
        var details = new CruisePriceDropAlertDetails(new(entity.PreviousAmount, entity.PreviousCurrency, entity.PreviousBasis), new(entity.CurrentAmount, entity.CurrentCurrency, entity.CurrentBasis), entity.EvidenceKey);
        if (details.Reduction != entity.Reduction || details.PercentageReduction != entity.PercentageReduction) throw new InvalidDataException("Persisted Price Drop calculation is inconsistent.");
        return details;
    }
    private static CruisePromotionAlertDetails MapPromotion(CruisePromotionAlertDetailEntity entity) => new(entity.PreviousSummary, entity.CurrentSummary, entity.EvidenceKey);
    private static CruiseSavedCriteriaAlertDetails MapCriteria(CruiseSavedCriteriaAlertDetailEntity entity)
    {
        CruiseBudget? budget = entity.ConfiguredBudgetAmount is null ? null : new(entity.ConfiguredBudgetAmount.Value, entity.ConfiguredBudgetCurrency!, (CruiseBudgetBasis)entity.ConfiguredBudgetBasis!.Value);
        CruisePrice? price = entity.MatchedPriceAmount is null ? null : new(entity.MatchedPriceAmount.Value, entity.MatchedPriceCurrency!, entity.MatchedPriceBasis);
        var configured = entity.Cabins.OrderBy(x => x.CabinType).Select(x => (CruiseCabinType)x.CabinType).ToArray();
        var matched = entity.Cabins.Where(x => x.IsMatched).OrderBy(x => x.CabinType).Select(x => (CruiseCabinType)x.CabinType).ToArray();
        return new(entity.MonthConfiguredAndMatched, budget, price, entity.CriteriaFingerprint,
            (CruiseAlertEvidenceOrigin)entity.EvidenceOrigin, entity.EvidenceKey, entity.EvidenceTime,
            entity.CabinPreferencesUnavailable, configured, matched,
            (SavedCruiseCriteriaResult)entity.CabinCriterionResult, entity.CabinContextFingerprint,
            entity.CabinEvidenceKey, entity.CabinEvidenceTime);
    }
    private static CruiseCabinAvailabilityAlertDetails MapCabinAvailability(CruiseCabinAvailabilityAlertDetailEntity entity)
    {
        var details = new CruiseCabinAvailabilityAlertDetails((CruiseCabinType)entity.CabinType,
            (CruiseCabinAvailabilityState)entity.PreviousState, (CruiseCabinAvailabilityState)entity.CurrentState,
            entity.ContextFingerprint, (CruiseCabinEvidenceCoverage)entity.Coverage,
            entity.StateFingerprint, entity.EvidenceKey, entity.EvidenceTime);
        if ((int)details.Direction != entity.Direction)
            throw new InvalidDataException("Persisted Cabin Availability direction is inconsistent.");
        return details;
    }
    private static CruiseNewItineraryAlertDetails MapNewItinerary(CruiseNewItineraryAlertDetailEntity entity) =>
        new(new CruiseItineraryKey(entity.OperatorId, entity.ProviderItineraryId), entity.ScopeFingerprint,
            entity.CheckEvidenceKey, entity.OccurrenceFingerprint, entity.ProviderEvidenceKey,
            entity.FirstObservedEventKey, entity.FirstObservedAt, entity.Title, entity.ShipName,
            entity.DepartureDate, entity.DurationNights, entity.DeparturePort, entity.ItinerarySummary, entity.SourceReference);
    private static bool IsUniqueConstraint(DbUpdateException exception) => exception.InnerException is SqliteException { SqliteErrorCode: 19 };
}
