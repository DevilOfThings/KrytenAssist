using System.Data;
using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Application.Cruises;
using KrytenAssist.Core.Cruises;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class SqliteCruiseDiscoveryRepository(KrytenAssistDbContext db) : ICruiseDiscoveryRepository
{
    private const int MaximumAttempts = 3;

    public async Task<CruiseDiscoveryRepositoryRecordResult> RecordAsync(CruiseDiscoveryCheck check, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(check); ValidateHash(check.EvidenceKey, nameof(check)); ValidateHash(check.Scope.Fingerprint, nameof(check));
        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var existing = await LoadCheckQuery().SingleOrDefaultAsync(x => x.EvidenceKey == check.EvidenceKey, cancellationToken);
                if (existing is not null)
                {
                    var mapped = MapCheck(existing);
                    var existingEvents = await EventsForCheckAsync(existing, cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return new(CruiseDiscoveryRecordState.AlreadyRecorded, mapped, existingEvents);
                }

                var scope = await db.CruiseDiscoveryScopes.Include(x => x.Criteria).ThenInclude(x => x.Values)
                    .SingleOrDefaultAsync(x => x.ScopeFingerprint == check.Scope.Fingerprint, cancellationToken);
                var baseline = scope is null;
                if (scope is null)
                {
                    scope = CreateScope(check.Scope, check.ObservedAt);
                    db.CruiseDiscoveryScopes.Add(scope);
                }
                else VerifyScope(scope, check.Scope);

                var checkEntity = CreateCheck(scope, check);
                db.CruiseDiscoveryChecks.Add(checkEntity);
                await db.SaveChangesAsync(cancellationToken);

                var events = new List<CruiseItineraryFirstObservedEvent>();
                foreach (var occurrenceEntity in checkEntity.Occurrences.OrderBy(x => x.CatalogueKey, StringComparer.Ordinal))
                {
                    var catalogue = await db.CruiseItineraryCatalogue.Include(x => x.LatestOccurrence).ThenInclude(x => x.Check)
                        .SingleOrDefaultAsync(x => x.CatalogueKey == occurrenceEntity.CatalogueKey, cancellationToken);
                    if (catalogue is null)
                    {
                        var occurrence = MapOccurrence(occurrenceEntity);
                        var discovered = baseline ? null : new CruiseItineraryFirstObservedEvent(occurrence, check.Scope.Fingerprint, check.EvidenceKey);
                        catalogue = CreateCatalogue(occurrenceEntity, discovered?.EventKey);
                        db.CruiseItineraryCatalogue.Add(catalogue);
                        if (discovered is not null) events.Add(discovered);
                    }
                    else UpdateLatest(catalogue, occurrenceEntity);
                }

                UpdateScopeLatest(scope, check);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                db.ChangeTracker.Clear();
                var state = baseline ? CruiseDiscoveryRecordState.BaselineSeeded : events.Count == 0
                    ? CruiseDiscoveryRecordState.RecordedNoNewItineraries : CruiseDiscoveryRecordState.RecordedWithFirstObserved;
                return new(state, check, Array.AsReadOnly(events.OrderBy(x => x.Occurrence.CatalogueKey.PersistenceKey, StringComparer.Ordinal).ToArray()));
            }
            catch (Exception exception) when (attempt < MaximumAttempts && IsTransient(exception))
            {
                await transaction.RollbackAsync(CancellationToken.None); db.ChangeTracker.Clear();
                await Task.Delay(TimeSpan.FromMilliseconds(10 * attempt), cancellationToken);
            }
        }
    }

    public async Task<IReadOnlyList<CruiseItineraryCatalogueEntry>> ListFirstObservedAsync(CancellationToken cancellationToken = default)
    {
        var rows = await CatalogueQuery().Where(x => x.FirstObservedEventKey != null).AsNoTracking()
            .OrderByDescending(x => x.FirstSeenAtUtcTicks).ThenBy(x => x.FirstObservedEventKey).ThenBy(x => x.CatalogueKey)
            .ToArrayAsync(cancellationToken);
        return Array.AsReadOnly(rows.Select(MapCatalogue).ToArray());
    }

    public async Task<CruiseItineraryCatalogueEntry?> GetAsync(string catalogueKey, CancellationToken cancellationToken = default)
    {
        ValidateHash(catalogueKey, nameof(catalogueKey));
        var row = await CatalogueQuery().AsNoTracking().SingleOrDefaultAsync(x => x.CatalogueKey == catalogueKey, cancellationToken);
        return row is null ? null : MapCatalogue(row);
    }

    public async Task<IReadOnlyList<CruiseDiscoveryCheck>> ListChecksAsync(CancellationToken cancellationToken = default)
    {
        var rows = await LoadCheckQuery().AsNoTracking().OrderByDescending(x => x.ObservedAtUtcTicks).ThenBy(x => x.EvidenceKey).ToArrayAsync(cancellationToken);
        return Array.AsReadOnly(rows.Select(MapCheck).ToArray());
    }

    private IQueryable<CruiseDiscoveryCheckEntity> LoadCheckQuery() => db.CruiseDiscoveryChecks
        .Include(x => x.Scope).ThenInclude(x => x.Criteria).ThenInclude(x => x.Values)
        .Include(x => x.Occurrences).Include(x => x.Rejections);

    private IQueryable<CruiseItineraryCatalogueEntity> CatalogueQuery() => db.CruiseItineraryCatalogue
        .Include(x => x.FirstOccurrence).ThenInclude(x => x.Check).ThenInclude(x => x.Scope).ThenInclude(x => x.Criteria).ThenInclude(x => x.Values)
        .Include(x => x.LatestOccurrence).ThenInclude(x => x.Check).ThenInclude(x => x.Scope).ThenInclude(x => x.Criteria).ThenInclude(x => x.Values);

    private async Task<IReadOnlyList<CruiseItineraryFirstObservedEvent>> EventsForCheckAsync(CruiseDiscoveryCheckEntity check, CancellationToken token)
    {
        var mapped = MapCheck(check); var values = new List<CruiseItineraryFirstObservedEvent>();
        foreach (var occurrence in mapped.Occurrences)
        {
            var candidate = new CruiseItineraryFirstObservedEvent(occurrence, mapped.Scope.Fingerprint, mapped.EvidenceKey);
            if (await db.CruiseItineraryCatalogue.AnyAsync(x => x.CatalogueKey == occurrence.CatalogueKey.PersistenceKey && x.FirstObservedEventKey == candidate.EventKey, token)) values.Add(candidate);
        }
        return Array.AsReadOnly(values.OrderBy(x => x.Occurrence.CatalogueKey.PersistenceKey, StringComparer.Ordinal).ToArray());
    }

    private static CruiseDiscoveryScopeEntity CreateScope(CruiseDiscoveryScope scope, DateTimeOffset time) => new()
    {
        ScopeFingerprint = scope.Fingerprint, RetailSourceId = Normalize(scope.Source.Id), RetailSourceName = scope.Source.Name.Trim(), OperatorId = scope.OperatorId,
        Surface = (int)scope.Surface, CaptureContractVersion = scope.CaptureContractVersion, FirstCheckedAt = time, FirstCheckedAtUtcTicks = time.UtcTicks, LastCheckedAt = time, LastCheckedAtUtcTicks = time.UtcTicks,
        Criteria = scope.Criteria.Select(c => new CruiseDiscoveryScopeCriterionEntity { Name = c.Name, State = (int)c.State, Values = c.Values.Select((v, i) => new CruiseDiscoveryScopeCriterionValueEntity { DisplayOrder = i, Value = v }).ToList() }).ToList()
    };

    private static CruiseDiscoveryCheckEntity CreateCheck(CruiseDiscoveryScopeEntity scope, CruiseDiscoveryCheck check) => new()
    {
        Scope = scope, EvidenceKey = check.EvidenceKey, ObservedAt = check.ObservedAt, ObservedAtUtcTicks = check.ObservedAt.UtcTicks, WasTruncated = check.WasTruncated,
        AcceptedCount = check.Occurrences.Count, RejectedCount = check.Rejections.Count,
        Occurrences = check.Occurrences.Select(o => new CruiseDiscoveryOccurrenceEntity { CatalogueKey = o.CatalogueKey.PersistenceKey, OccurrenceFingerprint = o.Fingerprint, OperatorId = o.ItineraryKey.OperatorId, ProviderItineraryId = o.ItineraryKey.ProviderItineraryId, RetailSourceId = o.CatalogueKey.RetailSourceId, RetailSourceName = o.Source.Name.Trim(), Title = o.Title, ShipName = o.ShipName, DepartureDate = o.DepartureDate, DurationNights = o.DurationNights, DeparturePort = o.DeparturePort, ItinerarySummary = o.ItinerarySummary, ProviderOfferId = o.ProviderOfferId, ObservedAt = o.ObservedAt, ObservedAtUtcTicks = o.ObservedAt.UtcTicks, EvidenceKey = o.EvidenceKey, SourceReference = o.SourceReference }).ToList(),
        Rejections = check.Rejections.Select((r, i) => new CruiseDiscoveryRejectionEntity { DisplayOrder = i, CandidateKey = r.CandidateKey, Reason = r.Reason }).ToList()
    };

    private static CruiseItineraryCatalogueEntity CreateCatalogue(CruiseDiscoveryOccurrenceEntity occurrence, string? eventKey) => new()
    {
        CatalogueKey = occurrence.CatalogueKey, RetailSourceId = occurrence.RetailSourceId, RetailSourceName = occurrence.RetailSourceName, OperatorId = occurrence.OperatorId, ProviderItineraryId = occurrence.ProviderItineraryId,
        FirstOccurrence = occurrence, LatestOccurrence = occurrence, FirstSeenAt = occurrence.ObservedAt, FirstSeenAtUtcTicks = occurrence.ObservedAtUtcTicks, LastSeenAt = occurrence.ObservedAt, LastSeenAtUtcTicks = occurrence.ObservedAtUtcTicks, FirstObservedEventKey = eventKey
    };

    private static void UpdateLatest(CruiseItineraryCatalogueEntity catalogue, CruiseDiscoveryOccurrenceEntity occurrence)
    {
        VerifyCatalogueIdentity(catalogue, occurrence);
        var incomingTieBreak = string.Join('\n', occurrence.OccurrenceFingerprint, occurrence.Check.EvidenceKey);
        var currentTieBreak = string.Join('\n', catalogue.LatestOccurrence.OccurrenceFingerprint, catalogue.LatestOccurrence.Check.EvidenceKey);
        var replace = occurrence.ObservedAtUtcTicks > catalogue.LastSeenAtUtcTicks || occurrence.ObservedAtUtcTicks == catalogue.LastSeenAtUtcTicks && StringComparer.Ordinal.Compare(incomingTieBreak, currentTieBreak) > 0;
        if (!replace) return;
        catalogue.LatestOccurrence = occurrence; catalogue.LastSeenAt = occurrence.ObservedAt; catalogue.LastSeenAtUtcTicks = occurrence.ObservedAtUtcTicks; catalogue.RetailSourceName = occurrence.RetailSourceName;
    }

    private static void UpdateScopeLatest(CruiseDiscoveryScopeEntity scope, CruiseDiscoveryCheck check)
    {
        if (check.ObservedAt.UtcTicks < scope.LastCheckedAtUtcTicks) return;
        scope.LastCheckedAt = check.ObservedAt; scope.LastCheckedAtUtcTicks = check.ObservedAt.UtcTicks; scope.RetailSourceName = check.Scope.Source.Name.Trim();
    }

    private static CruiseDiscoveryScope MapScope(CruiseDiscoveryScopeEntity row)
    {
        VerifyTime(row.FirstCheckedAt, row.FirstCheckedAtUtcTicks); VerifyTime(row.LastCheckedAt, row.LastCheckedAtUtcTicks);
        var criteria = row.Criteria.OrderBy(x => x.Name, StringComparer.Ordinal).Select(c =>
        {
            var values = c.Values.OrderBy(x => x.DisplayOrder).ToArray();
            if (values.Where((x, i) => x.DisplayOrder != i).Any()) throw new InvalidDataException("Persisted discovery criterion value order is invalid.");
            return new CruiseDiscoveryCriterion(c.Name, (CruiseDiscoveryCriterionState)c.State, values.Select(x => x.Value));
        });
        var scope = new CruiseDiscoveryScope(new(row.RetailSourceId, row.RetailSourceName), row.OperatorId, (CruiseDiscoverySurface)row.Surface, row.CaptureContractVersion, criteria);
        if (scope.Fingerprint != row.ScopeFingerprint) throw new InvalidDataException("Persisted discovery scope identity is inconsistent.");
        return scope;
    }

    private static CruiseItineraryOccurrence MapOccurrence(CruiseDiscoveryOccurrenceEntity row)
    {
        VerifyTime(row.ObservedAt, row.ObservedAtUtcTicks);
        var value = new CruiseItineraryOccurrence(new(row.OperatorId, row.ProviderItineraryId), new(row.RetailSourceId, row.RetailSourceName), row.ObservedAt, row.EvidenceKey, row.Title, row.ShipName, row.DepartureDate, row.DurationNights, row.DeparturePort, row.ItinerarySummary, row.ProviderOfferId, row.SourceReference);
        if (value.CatalogueKey.PersistenceKey != row.CatalogueKey || value.Fingerprint != row.OccurrenceFingerprint) throw new InvalidDataException("Persisted discovery occurrence identity is inconsistent.");
        return value;
    }

    private static CruiseDiscoveryCheck MapCheck(CruiseDiscoveryCheckEntity row)
    {
        VerifyTime(row.ObservedAt, row.ObservedAtUtcTicks); var scope = MapScope(row.Scope);
        if (row.AcceptedCount != row.Occurrences.Count || row.RejectedCount != row.Rejections.Count) throw new InvalidDataException("Persisted discovery check counts are inconsistent.");
        var rejections = row.Rejections.OrderBy(x => x.DisplayOrder).ToArray(); if (rejections.Where((x, i) => x.DisplayOrder != i).Any()) throw new InvalidDataException("Persisted rejection order is invalid.");
        var check = new CruiseDiscoveryCheck(scope, row.ObservedAt, row.Occurrences.Select(MapOccurrence), rejections.Select(x => new CruiseDiscoveryRejection(x.CandidateKey, x.Reason)), row.WasTruncated);
        if (check.EvidenceKey != row.EvidenceKey) throw new InvalidDataException("Persisted discovery check identity is inconsistent."); return check;
    }

    private static CruiseItineraryCatalogueEntry MapCatalogue(CruiseItineraryCatalogueEntity row)
    {
        VerifyTime(row.FirstSeenAt, row.FirstSeenAtUtcTicks); VerifyTime(row.LastSeenAt, row.LastSeenAtUtcTicks); var first = MapOccurrence(row.FirstOccurrence); var latest = MapOccurrence(row.LatestOccurrence);
        if (row.FirstSeenAtUtcTicks != first.ObservedAt.UtcTicks || row.LastSeenAtUtcTicks != latest.ObservedAt.UtcTicks || row.FirstSeenAtUtcTicks > row.LastSeenAtUtcTicks) throw new InvalidDataException("Persisted catalogue time range is inconsistent.");
        var key = new CruiseItineraryCatalogueKey(new(row.RetailSourceId, row.RetailSourceName), new(row.OperatorId, row.ProviderItineraryId)); if (key.PersistenceKey != row.CatalogueKey || first.CatalogueKey != key || latest.CatalogueKey != key) throw new InvalidDataException("Persisted catalogue identity is inconsistent.");
        if (row.FirstObservedEventKey is not null) { ValidateHash(row.FirstObservedEventKey, nameof(row.FirstObservedEventKey)); var expected = new CruiseItineraryFirstObservedEvent(first, row.FirstOccurrence.Check.Scope.ScopeFingerprint, row.FirstOccurrence.Check.EvidenceKey).EventKey; if (expected != row.FirstObservedEventKey) throw new InvalidDataException("Persisted first-observed event identity is inconsistent."); }
        return new(key, first, latest, row.FirstSeenAt, row.LastSeenAt, row.FirstObservedEventKey);
    }

    private static void VerifyScope(CruiseDiscoveryScopeEntity row, CruiseDiscoveryScope supplied) { if (MapScope(row).Fingerprint != supplied.Fingerprint) throw new InvalidDataException("Stored scope does not match supplied scope."); }
    private static void VerifyCatalogueIdentity(CruiseItineraryCatalogueEntity row, CruiseDiscoveryOccurrenceEntity occurrence) { if (row.CatalogueKey != occurrence.CatalogueKey || row.OperatorId != occurrence.OperatorId || row.ProviderItineraryId != occurrence.ProviderItineraryId || row.RetailSourceId != occurrence.RetailSourceId) throw new InvalidDataException("Stored catalogue identity is inconsistent."); }
    private static void VerifyTime(DateTimeOffset value, long ticks) { if (value.UtcTicks != ticks) throw new InvalidDataException("Persisted discovery timestamp identity is inconsistent."); }
    private static string Normalize(string value) => string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
    private static void ValidateHash(string value, string parameter) { if (value.Length != 64 || value.Any(c => c is not (>= '0' and <= '9') and not (>= 'a' and <= 'f'))) throw new ArgumentException("A lowercase SHA-256 identity is required.", parameter); }
    private static bool IsTransient(Exception exception) { var sqlite = FindSqlite(exception); return sqlite is not null && (sqlite.SqliteErrorCode is 5 or 6 || sqlite.SqliteExtendedErrorCode is 1555 or 2067); }
    private static SqliteException? FindSqlite(Exception exception) { for (var current = exception; current is not null; current = current.InnerException!) { if (current is SqliteException sqlite) return sqlite; if (current.InnerException is null) break; } return null; }
}
