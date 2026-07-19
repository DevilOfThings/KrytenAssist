using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Application.Cruises;
using KrytenAssist.Core.Cruises;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class SqliteCruiseCabinObservationRepository : ICruiseCabinObservationRepository
{
    private const int MaximumRecordAttempts = 3;
    private readonly KrytenAssistDbContext _dbContext;

    public SqliteCruiseCabinObservationRepository(KrytenAssistDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<CruiseCabinRepositoryRecordResult> RecordAsync(
        CruiseCabinObservation observation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observation);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateHash(observation.SeriesKey, nameof(observation));
        ValidateHash(observation.StateFingerprint, nameof(observation));
        ValidateSource(observation.Source);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await RecordAttemptAsync(observation, cancellationToken);
            }
            catch (Exception exception) when (attempt < MaximumRecordAttempts && IsTransientConcurrencyFailure(exception))
            {
                _dbContext.ChangeTracker.Clear();
                await Task.Delay(TimeSpan.FromMilliseconds(10 * attempt), cancellationToken);
            }
        }
    }

    public async Task<CruiseCabinRecordedHistory?> GetAsync(string seriesKey, CancellationToken cancellationToken = default)
    {
        ValidateHash(seriesKey, nameof(seriesKey));
        cancellationToken.ThrowIfCancellationRequested();
        var entity = await LoadSeriesAsync(seriesKey, tracking: false, cancellationToken);
        return entity is null ? null : MapHistory(entity);
    }

    public async Task<IReadOnlyList<CruiseCabinRecordedHistory>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var values = await CompleteQuery(tracking: false)
            .OrderBy(x => x.DepartureDate)
            .ThenBy(x => x.OperatorId)
            .ThenBy(x => x.ShipName)
            .ThenBy(x => x.DurationNights)
            .ThenBy(x => x.RetailSourceId)
            .ThenBy(x => x.ContextFingerprint)
            .ThenBy(x => x.SeriesKey)
            .ToArrayAsync(cancellationToken);
        return Array.AsReadOnly(values.Select(MapHistory).ToArray());
    }

    private async Task<CruiseCabinRepositoryRecordResult> RecordAttemptAsync(
        CruiseCabinObservation observation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var series = await LoadSeriesAsync(observation.SeriesKey, tracking: true, cancellationToken);
        CruiseCabinRepositoryRecordState state;
        if (series is null)
        {
            series = CreateSeries(observation);
            series.Observations.Add(CreateObservation(observation, sequence: 1));
            _dbContext.CruiseCabinSeries.Add(series);
            state = CruiseCabinRepositoryRecordState.FirstObservationRecorded;
        }
        else
        {
            VerifySeries(series);
            var current = CurrentObservation(series.Observations);
            if (string.Equals(current.StateFingerprint, observation.StateFingerprint, StringComparison.Ordinal))
            {
                state = CruiseCabinRepositoryRecordState.AlreadyCurrent;
            }
            else
            {
                series.Observations.Add(CreateObservation(observation, series.Observations.Max(x => x.Sequence) + 1));
                if (observation.ObservedAt.UtcTicks < series.FirstObservedAtUtcTicks)
                {
                    series.FirstObservedAt = observation.ObservedAt;
                    series.FirstObservedAtUtcTicks = observation.ObservedAt.UtcTicks;
                }
                state = CruiseCabinRepositoryRecordState.ChangedObservationRecorded;
            }

            if (observation.ObservedAt.UtcTicks > series.LastSeenAtUtcTicks)
            {
                series.LastSeenAt = observation.ObservedAt;
                series.LastSeenAtUtcTicks = observation.ObservedAt.UtcTicks;
            }
            UpdateLatestEvidence(series, observation);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        await transaction.CommitAsync(cancellationToken);
        return new(state, MapHistory(series));
    }

    private Task<CruiseCabinSeriesEntity?> LoadSeriesAsync(string seriesKey, bool tracking, CancellationToken token) =>
        CompleteQuery(tracking).SingleOrDefaultAsync(x => x.SeriesKey == seriesKey, token);

    private IQueryable<CruiseCabinSeriesEntity> CompleteQuery(bool tracking)
    {
        IQueryable<CruiseCabinSeriesEntity> query = _dbContext.CruiseCabinSeries;
        if (!tracking) query = query.AsNoTracking();
        return query.Include(x => x.ChildAges).Include(x => x.Observations).ThenInclude(x => x.States);
    }

    private static CruiseCabinSeriesEntity CreateSeries(CruiseCabinObservation observation)
    {
        var context = observation.SearchContext;
        var entity = new CruiseCabinSeriesEntity
        {
            SeriesKey = observation.SeriesKey,
            OperatorId = observation.SailingKey.OperatorId,
            ShipName = observation.SailingKey.ShipName,
            DepartureDate = observation.SailingKey.DepartureDate,
            DurationNights = observation.SailingKey.DurationNights,
            RetailSourceId = Normalize(observation.Source.Id),
            RetailSourceName = observation.Source.Name.Trim(),
            ContextFingerprint = context.Fingerprint,
            AdultCount = context.AdultCount,
            ChildCount = context.ChildCount,
            ChildAgesKnown = context.ChildAgesKnown,
            PackageMode = (int)context.PackageMode,
            DepartureAirportId = context.DepartureAirportId,
            CabinQuantity = context.CabinQuantity,
            FirstObservedAt = observation.ObservedAt,
            FirstObservedAtUtcTicks = observation.ObservedAt.UtcTicks,
            LastSeenAt = observation.ObservedAt,
            LastSeenAtUtcTicks = observation.ObservedAt.UtcTicks,
            LatestEvidenceKey = observation.EvidenceKey,
            LatestSourceReference = observation.SourceReference,
            LatestEvidenceObservedAt = observation.ObservedAt,
            LatestEvidenceObservedAtUtcTicks = observation.ObservedAt.UtcTicks
        };
        entity.ChildAges.AddRange(context.ChildAges.Select((age, index) => new CruiseCabinContextChildAgeEntity
        {
            DisplayOrder = index,
            Age = age
        }));
        return entity;
    }

    private static CruiseCabinObservationEntity CreateObservation(CruiseCabinObservation observation, int sequence)
    {
        var entity = new CruiseCabinObservationEntity
        {
            Sequence = sequence,
            StateFingerprint = observation.StateFingerprint,
            Coverage = (int)observation.Coverage,
            ObservedAt = observation.ObservedAt,
            ObservedAtUtcTicks = observation.ObservedAt.UtcTicks,
            EvidenceKey = observation.EvidenceKey,
            SourceReference = observation.SourceReference
        };
        entity.States.AddRange(observation.States.Select(state => new CruiseCabinObservationStateEntity
        {
            CabinType = (int)state.CabinType,
            Availability = (int)state.Availability
        }));
        return entity;
    }

    private static CruiseCabinRecordedHistory MapHistory(CruiseCabinSeriesEntity series)
    {
        var context = MapContext(series);
        var key = new CruiseSailingKey(series.OperatorId, series.ShipName, series.DepartureDate, series.DurationNights);
        var source = new CruiseSource(series.RetailSourceId, series.RetailSourceName);
        var sequenced = series.Observations.OrderBy(x => x.Sequence).ToArray();
        if (sequenced.Select(x => x.Sequence).Where((value, index) => value != index + 1).Any())
            throw new InvalidDataException("Persisted cabin observation sequence is not contiguous.");
        var observations = series.Observations
            .OrderBy(x => x.ObservedAtUtcTicks)
            .ThenBy(x => x.StateFingerprint, StringComparer.Ordinal)
            .Select(value => MapObservation(series, value, key, source, context))
            .ToArray();
        if (observations.Length == 0) throw new InvalidDataException("Persisted cabin series has no observations.");
        if (series.FirstObservedAtUtcTicks != observations.Min(x => x.ObservedAt.UtcTicks) ||
            series.LastSeenAtUtcTicks < observations.Max(x => x.ObservedAt.UtcTicks))
            throw new InvalidDataException("Persisted cabin series observation range is inconsistent.");
        return new CruiseCabinRecordedHistory(series.SeriesKey, series.LastSeenAt, observations,
            new CruiseCabinLatestEvidence(series.LatestEvidenceKey, series.LatestSourceReference, series.LatestEvidenceObservedAt));
    }

    private static CruiseCabinSearchContext MapContext(CruiseCabinSeriesEntity series)
    {
        if (series.FirstObservedAt.UtcTicks != series.FirstObservedAtUtcTicks ||
            series.LastSeenAt.UtcTicks != series.LastSeenAtUtcTicks ||
            series.LatestEvidenceObservedAt.UtcTicks != series.LatestEvidenceObservedAtUtcTicks)
            throw new InvalidDataException("Persisted cabin series timestamp identity is inconsistent.");
        if (series.FirstObservedAtUtcTicks > series.LastSeenAtUtcTicks || series.LatestEvidenceObservedAtUtcTicks > series.LastSeenAtUtcTicks)
            throw new InvalidDataException("Persisted cabin series time range is inconsistent.");
        try
        {
            var orderedAges = series.ChildAges.OrderBy(x => x.DisplayOrder).ToArray();
            if (orderedAges.Select(x => x.DisplayOrder).Where((value, index) => value != index).Any())
                throw new InvalidDataException("Persisted cabin child-age order is not contiguous.");
            var ages = orderedAges.Select(x => x.Age).ToArray();
            var context = new CruiseCabinSearchContext(series.AdultCount, series.ChildCount, ages,
                series.ChildAgesKnown, (CruiseCabinPackageMode)series.PackageMode,
                series.DepartureAirportId, series.CabinQuantity);
            if (!string.Equals(context.Fingerprint, series.ContextFingerprint, StringComparison.Ordinal))
                throw new InvalidDataException("Persisted cabin context fingerprint is inconsistent.");
            return context;
        }
        catch (InvalidDataException) { throw; }
        catch (Exception exception) { throw new InvalidDataException("Persisted cabin context is invalid.", exception); }
    }

    private static CruiseCabinObservation MapObservation(CruiseCabinSeriesEntity series,
        CruiseCabinObservationEntity value, CruiseSailingKey key, CruiseSource source, CruiseCabinSearchContext context)
    {
        if (value.ObservedAt.UtcTicks != value.ObservedAtUtcTicks)
            throw new InvalidDataException("Persisted cabin observation timestamp identity is inconsistent.");
        try
        {
            var states = value.States.OrderBy(x => x.CabinType)
                .Select(x => new CruiseCabinState((CruiseCabinType)x.CabinType, (CruiseCabinAvailabilityState)x.Availability));
            var observation = new CruiseCabinObservation(key, source, context, (CruiseCabinEvidenceCoverage)value.Coverage,
                states, value.ObservedAt, value.EvidenceKey, value.SourceReference);
            if (!string.Equals(observation.SeriesKey, series.SeriesKey, StringComparison.Ordinal) ||
                !string.Equals(observation.StateFingerprint, value.StateFingerprint, StringComparison.Ordinal))
                throw new InvalidDataException("Persisted cabin observation identity is inconsistent.");
            return observation;
        }
        catch (InvalidDataException) { throw; }
        catch (Exception exception) { throw new InvalidDataException("Persisted cabin observation is invalid.", exception); }
    }

    private static void VerifySeries(CruiseCabinSeriesEntity series) => _ = MapHistory(series);

    private static CruiseCabinObservationEntity CurrentObservation(IEnumerable<CruiseCabinObservationEntity> observations) =>
        observations.OrderBy(x => x.ObservedAtUtcTicks).ThenBy(x => x.StateFingerprint, StringComparer.Ordinal).Last();

    private static void UpdateLatestEvidence(CruiseCabinSeriesEntity series, CruiseCabinObservation observation)
    {
        var replace = observation.ObservedAt.UtcTicks > series.LatestEvidenceObservedAtUtcTicks;
        if (observation.ObservedAt.UtcTicks == series.LatestEvidenceObservedAtUtcTicks)
        {
            var existing = string.Join('\n', series.LatestEvidenceKey, series.LatestSourceReference ?? string.Empty);
            var candidate = string.Join('\n', observation.EvidenceKey, observation.SourceReference ?? string.Empty);
            replace = StringComparer.Ordinal.Compare(candidate, existing) > 0;
        }
        if (!replace) return;
        series.RetailSourceName = observation.Source.Name.Trim();
        series.LatestEvidenceKey = observation.EvidenceKey;
        series.LatestSourceReference = observation.SourceReference;
        series.LatestEvidenceObservedAt = observation.ObservedAt;
        series.LatestEvidenceObservedAtUtcTicks = observation.ObservedAt.UtcTicks;
    }

    private static void ValidateSource(CruiseSource source)
    {
        if (Normalize(source.Id).Length > SavedCruiseSnapshot.MaximumRetailSourceIdLength ||
            source.Name.Trim().Length > SavedCruiseSnapshot.MaximumRetailSourceNameLength)
            throw new ArgumentException("Cabin retail source exceeds persistence bounds.", nameof(source));
    }

    private static void ValidateHash(string value, string parameter)
    {
        if (value.Length != 64 || value.Any(character => character is not (>= '0' and <= '9') and not (>= 'a' and <= 'f')))
            throw new ArgumentException("A lowercase SHA-256 identity is required.", parameter);
    }

    private static string Normalize(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    private static bool IsTransientConcurrencyFailure(Exception exception)
    {
        var sqlite = FindSqliteException(exception);
        return sqlite is not null && (sqlite.SqliteErrorCode is 5 or 6 || sqlite.SqliteExtendedErrorCode is 1555 or 2067);
    }

    private static SqliteException? FindSqliteException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is SqliteException sqlite) return sqlite;
            if (current.InnerException is null) break;
        }
        return null;
    }
}
