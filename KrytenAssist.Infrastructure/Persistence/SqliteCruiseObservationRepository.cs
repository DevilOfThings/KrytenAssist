using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Application.Cruises;
using KrytenAssist.Core.Cruises;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class SqliteCruiseObservationRepository : ICruiseObservationRepository
{
    private const string NoRetailSource = "";
    private const int MaximumRecordAttempts = 3;
    private readonly KrytenAssistDbContext _dbContext;

    public SqliteCruiseObservationRepository(KrytenAssistDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<CruiseObservationRepositoryRecordResult> RecordAsync(
        CruiseSailingKey sailingKey,
        CruiseObservationFingerprint fingerprint,
        CruiseObservation observation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sailingKey);
        ArgumentNullException.ThrowIfNull(fingerprint);
        ArgumentNullException.ThrowIfNull(observation);
        cancellationToken.ThrowIfCancellationRequested();
        if (fingerprint.SailingKey != sailingKey || CruiseSailingKey.From(observation) != sailingKey)
        {
            throw new ArgumentException("Observation identity must match the supplied sailing key.", nameof(observation));
        }

        var observationFingerprint = CruiseObservationFingerprint.From(observation);
        if (!observationFingerprint.Equals(fingerprint))
        {
            throw new ArgumentException("Observation evidence must match the supplied fingerprint.", nameof(fingerprint));
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await RecordAttemptAsync(
                    sailingKey,
                    fingerprint,
                    observation,
                    cancellationToken);
            }
            catch (Exception exception) when (
                attempt < MaximumRecordAttempts
                && IsTransientConcurrencyFailure(exception))
            {
                _dbContext.ChangeTracker.Clear();
                await Task.Delay(TimeSpan.FromMilliseconds(10 * attempt), cancellationToken);
            }
        }
    }

    public async Task<CruiseRecordedHistory?> GetAsync(
        CruiseSailingKey sailingKey,
        CruiseSource? source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sailingKey);
        cancellationToken.ThrowIfCancellationRequested();
        var retailSourceId = CruiseObservationFingerprint.RetailSourceKey(source) ?? NoRetailSource;
        var history = await LoadEntityAsync(sailingKey, retailSourceId, tracking: false, cancellationToken);
        return history is null ? null : MapHistory(history);
    }

    public async Task<IReadOnlyList<CruiseRecordedHistory>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var histories = await _dbContext.CruiseHistories
            .AsNoTracking()
            .Include(history => history.Observations)
            .ThenInclude(observation => observation.Prices)
            .OrderBy(history => history.DepartureDate)
            .ThenBy(history => history.OperatorId)
            .ThenBy(history => history.NormalizedShipName)
            .ThenBy(history => history.DurationNights)
            .ThenBy(history => history.RetailSourceId)
            .ToListAsync(cancellationToken);
        return Array.AsReadOnly(histories.Select(MapHistory).ToArray());
    }

    private Task<CruiseHistoryEntity?> LoadEntityAsync(
        CruiseSailingKey sailingKey,
        string retailSourceId,
        bool tracking,
        CancellationToken cancellationToken)
    {
        IQueryable<CruiseHistoryEntity> query = _dbContext.CruiseHistories;
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query
            .Include(history => history.Observations)
            .ThenInclude(observation => observation.Prices)
            .SingleOrDefaultAsync(
                history => history.OperatorId == sailingKey.OperatorId
                    && history.NormalizedShipName == sailingKey.ShipName
                    && history.DepartureDate == sailingKey.DepartureDate
                    && history.DurationNights == sailingKey.DurationNights
                    && history.RetailSourceId == retailSourceId,
                cancellationToken);
    }

    private async Task<CruiseObservationRepositoryRecordResult> RecordAttemptAsync(
        CruiseSailingKey sailingKey,
        CruiseObservationFingerprint fingerprint,
        CruiseObservation observation,
        CancellationToken cancellationToken)
    {
        var retailSourceId = fingerprint.RetailSourceId ?? NoRetailSource;
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var history = await LoadEntityAsync(sailingKey, retailSourceId, tracking: true, cancellationToken);
        CruiseObservationRepositoryRecordState state;
        if (history is null)
        {
            history = CreateHistory(sailingKey, fingerprint, observation);
            history.Observations.Add(CreateObservation(fingerprint, observation, sequence: 1));
            _dbContext.CruiseHistories.Add(history);
            state = CruiseObservationRepositoryRecordState.FirstObservationRecorded;
        }
        else
        {
            var latest = CurrentObservation(history.Observations);
            if (string.Equals(latest.Fingerprint, fingerprint.PersistenceKey, StringComparison.Ordinal))
            {
                state = CruiseObservationRepositoryRecordState.AlreadyCurrent;
            }
            else
            {
                var nextSequence = history.Observations.Max(item => item.Sequence) + 1;
                history.Observations.Add(CreateObservation(fingerprint, observation, nextSequence));
                history.FirstObservedAt = Earlier(history.FirstObservedAt, observation.ObservedAt);
                state = CruiseObservationRepositoryRecordState.ChangedObservationRecorded;
            }

            history.LastSeenAt = Later(history.LastSeenAt, observation.ObservedAt);
            UpdateLatestEvidence(history, observation);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        await transaction.CommitAsync(cancellationToken);
        return new CruiseObservationRepositoryRecordResult(state, MapHistory(history));
    }

    private static CruiseHistoryEntity CreateHistory(
        CruiseSailingKey sailingKey,
        CruiseObservationFingerprint fingerprint,
        CruiseObservation observation) =>
        new()
        {
            OperatorId = sailingKey.OperatorId,
            NormalizedShipName = sailingKey.ShipName,
            DepartureDate = sailingKey.DepartureDate,
            DurationNights = sailingKey.DurationNights,
            RetailSourceId = fingerprint.RetailSourceId ?? NoRetailSource,
            RetailSourceName = observation.Source?.Name,
            FirstObservedAt = observation.ObservedAt,
            LastSeenAt = observation.ObservedAt,
            LatestProviderOfferId = observation.Snapshot.Offer.ProviderOfferId,
            LatestSourceReference = observation.SourceReference,
            LatestEvidenceObservedAt = observation.ObservedAt
        };

    private static CruiseObservationEntity CreateObservation(
        CruiseObservationFingerprint fingerprint,
        CruiseObservation observation,
        int sequence)
    {
        var offer = observation.Snapshot.Offer;
        var entity = new CruiseObservationEntity
        {
            Sequence = sequence,
            Fingerprint = fingerprint.PersistenceKey,
            ProviderOfferId = offer.ProviderOfferId,
            OperatorName = offer.Provider.Name,
            Title = offer.Title,
            ShipName = offer.ShipName,
            DepartureDate = offer.DepartureDate,
            DurationNights = offer.DurationNights,
            DeparturePort = offer.DeparturePort,
            ItinerarySummary = offer.ItinerarySummary,
            PromotionSummary = observation.Snapshot.PromotionSummary,
            SourceReference = observation.SourceReference,
            ObservedAt = observation.ObservedAt
        };
        entity.Prices.AddRange(observation.Snapshot.Prices.Select((price, index) =>
            new CruiseObservationPriceEntity
            {
                Amount = price.Amount,
                Currency = price.Currency,
                Basis = price.Basis,
                DisplayOrder = index
            }));
        return entity;
    }

    private static CruiseRecordedHistory MapHistory(CruiseHistoryEntity history)
    {
        var source = history.RetailSourceId.Length == 0
            ? null
            : new CruiseSource(history.RetailSourceId, history.RetailSourceName!);
        var observations = history.Observations
            .OrderBy(observation => observation.ObservedAt)
            .ThenBy(observation => observation.Fingerprint, StringComparer.Ordinal)
            .Select(observation => MapObservation(history.OperatorId, source, observation))
            .ToArray();
        return new CruiseRecordedHistory(
            new CruiseSailingKey(
                history.OperatorId,
                history.NormalizedShipName,
                history.DepartureDate,
                history.DurationNights),
            history.LastSeenAt,
            observations,
            new CruiseLatestEvidence(
                history.LatestProviderOfferId,
                history.LatestSourceReference,
                history.LatestEvidenceObservedAt));
    }

    private static CruiseObservation MapObservation(
        string operatorId,
        CruiseSource? source,
        CruiseObservationEntity observation)
    {
        var provider = new CruiseProvider(operatorId, observation.OperatorName);
        var offer = new CruiseOffer(
            provider,
            observation.ProviderOfferId,
            observation.Title,
            observation.ShipName,
            observation.DepartureDate,
            observation.DurationNights,
            observation.DeparturePort,
            observation.ItinerarySummary);
        var prices = observation.Prices
            .OrderBy(price => price.DisplayOrder)
            .Select(price => new CruisePrice(price.Amount, price.Currency, price.Basis));
        return new CruiseObservation(
            new CruiseSnapshot(offer, prices, observation.PromotionSummary),
            observation.ObservedAt,
            observation.SourceReference,
            source);
    }

    private static DateTimeOffset Earlier(DateTimeOffset left, DateTimeOffset right) => left <= right ? left : right;
    private static DateTimeOffset Later(DateTimeOffset left, DateTimeOffset right) => left >= right ? left : right;

    private static CruiseObservationEntity CurrentObservation(IEnumerable<CruiseObservationEntity> observations) =>
        observations
            .OrderBy(item => item.ObservedAt)
            .ThenBy(item => item.Fingerprint, StringComparer.Ordinal)
            .Last();

    private static void UpdateLatestEvidence(
        CruiseHistoryEntity history,
        CruiseObservation observation)
    {
        if (!ShouldReplaceLatestEvidence(history, observation))
        {
            return;
        }

        history.LatestProviderOfferId = observation.Snapshot.Offer.ProviderOfferId;
        history.LatestSourceReference = observation.SourceReference;
        history.LatestEvidenceObservedAt = observation.ObservedAt;
    }

    private static bool ShouldReplaceLatestEvidence(
        CruiseHistoryEntity history,
        CruiseObservation observation)
    {
        if (observation.ObservedAt != history.LatestEvidenceObservedAt)
        {
            return observation.ObservedAt > history.LatestEvidenceObservedAt;
        }

        var existingKey = string.Join('\n', history.LatestProviderOfferId, history.LatestSourceReference ?? string.Empty);
        var candidateKey = string.Join(
            '\n',
            observation.Snapshot.Offer.ProviderOfferId,
            observation.SourceReference ?? string.Empty);
        return StringComparer.Ordinal.Compare(candidateKey, existingKey) > 0;
    }

    private static bool IsTransientConcurrencyFailure(Exception exception)
    {
        var sqlite = FindSqliteException(exception);
        return sqlite is not null
            && (sqlite.SqliteErrorCode is 5 or 6
                || sqlite.SqliteExtendedErrorCode is 1555 or 2067);
    }

    private static SqliteException? FindSqliteException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is SqliteException sqlite)
            {
                return sqlite;
            }

            if (current.InnerException is null)
            {
                break;
            }
        }

        return null;
    }
}
