extern alias KrytenApplication;

using KrytenAssist.Core.Cruises;
using Repository = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseObservationRepository;
using RecordedHistory = KrytenApplication::KrytenAssist.Application.Cruises.CruiseRecordedHistory;
using RepositoryResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRepositoryRecordResult;
using AlertRepository = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseAlertRepository;
using AlertSettingsRepository = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseAlertSettingsRepository;
using AlertAddResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertAddRepositoryResult;
using AlertQuery = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertQuery;
using CompositeRecorder = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseObservationAndEvaluateAlerts;
using EvaluateAlerts = KrytenApplication::KrytenAssist.Application.Cruises.EvaluateRecordedCruiseAlerts;
using GetHistory = KrytenApplication::KrytenAssist.Application.Cruises.GetCruiseHistory;
using MaterializeAlerts = KrytenApplication::KrytenAssist.Application.Cruises.MaterializeCruiseAlertCandidates;
using RecordObservation = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseObservation;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

internal sealed class FakeCruiseObservationRepository : Repository
{
    internal int RecordCalls { get; private set; }
    internal int GetCalls { get; private set; }
    internal int ListCalls { get; private set; }
    internal CruiseSailingKey? RecordedKey { get; private set; }
    internal CruiseObservationFingerprint? RecordedFingerprint { get; private set; }
    internal CruiseObservation? RecordedObservation { get; private set; }
    internal CancellationToken RecordedToken { get; private set; }
    internal CancellationToken ListToken { get; private set; }
    internal CruiseSailingKey? RequestedKey { get; private set; }
    internal CruiseSource? RequestedSource { get; private set; }

    internal RepositoryResult? RecordResult { get; set; }
    internal RecordedHistory? GetResult { get; set; }
    internal IReadOnlyList<RecordedHistory> ListResult { get; set; } = [];
    internal Exception? RecordException { get; set; }
    internal Exception? GetException { get; set; }
    internal Exception? ListException { get; set; }
    internal Func<CruiseSailingKey, CruiseObservationFingerprint, CruiseObservation, CancellationToken, Task<RepositoryResult>>? RecordHandler { get; set; }
    internal Func<CancellationToken, Task<IReadOnlyList<RecordedHistory>>>? ListHandler { get; set; }

    public async Task<RepositoryResult> RecordAsync(
        CruiseSailingKey sailingKey,
        CruiseObservationFingerprint fingerprint,
        CruiseObservation observation,
        CancellationToken cancellationToken = default)
    {
        RecordCalls++;
        RecordedKey = sailingKey;
        RecordedFingerprint = fingerprint;
        RecordedObservation = observation;
        RecordedToken = cancellationToken;
        if (RecordHandler is not null)
        {
            var handled = await RecordHandler(sailingKey, fingerprint, observation, cancellationToken);
            GetResult = handled.History;
            return handled;
        }

        if (RecordException is not null)
        {
            throw RecordException;
        }

        GetResult = RecordResult!.History;
        return RecordResult;
    }

    public Task<RecordedHistory?> GetAsync(
        CruiseSailingKey sailingKey,
        CruiseSource? source,
        CancellationToken cancellationToken = default)
    {
        GetCalls++;
        RequestedKey = sailingKey;
        RequestedSource = source;
        return GetException is null
            ? Task.FromResult(GetResult)
            : Task.FromException<RecordedHistory?>(GetException);
    }

    public Task<IReadOnlyList<RecordedHistory>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        ListCalls++;
        ListToken = cancellationToken;
        if (ListHandler is not null)
        {
            return ListHandler(cancellationToken);
        }

        return ListException is null
            ? Task.FromResult(ListResult)
            : Task.FromException<IReadOnlyList<RecordedHistory>>(ListException);
    }
}

internal static class CruiseAlertApplicationTestFactory
{
    internal static CompositeRecorder CreateRecorder(
        FakeCruiseObservationRepository repository,
        CruisePriceHistoryAnalyzer analyzer,
        AlertRepository? alerts = null,
        AlertSettingsRepository? settings = null,
        FakeSavedCruiseRepository? saved = null,
        FakeCruisePreferencesRepository? preferences = null)
    {
        saved ??= new FakeSavedCruiseRepository();
        preferences ??= new FakeCruisePreferencesRepository();
        var alertRepository = alerts ?? new TestAlertRepository();
        return new(
            new RecordObservation(repository, analyzer),
            new GetHistory(repository, analyzer),
            new EvaluateAlerts(
                new CruiseObservationAlertDetector(analyzer),
                settings ?? new TestAlertSettingsRepository(),
                new MaterializeAlerts(alertRepository)),
            CruiseCriteriaTestFactory.CreateForSailing(
                saved,
                preferences,
                repository,
                alertRepository as TestAlertRepository));
    }
}

internal sealed class TestAlertRepository : AlertRepository
{
    private readonly List<CruiseAlert> _alerts = [];
    internal bool ThrowOnNextAdd { get; set; }
    internal int AddCalls { get; private set; }

    public Task<CruiseAlert?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_alerts.SingleOrDefault(alert => alert.Id == id));

    public Task<IReadOnlyList<CruiseAlert>> ListAsync(
        AlertQuery query,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CruiseAlert>>(_alerts.ToArray());

    public Task<int> CountUnreadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_alerts.Count(alert => alert.Status == CruiseAlertStatus.Unread));

    public Task<AlertAddResult> AddIfAbsentAsync(
        CruiseAlert alert,
        CancellationToken cancellationToken = default)
    {
        AddCalls++;
        if (ThrowOnNextAdd)
        {
            ThrowOnNextAdd = false;
            return Task.FromException<AlertAddResult>(new InvalidOperationException("private failure"));
        }
        var existing = _alerts.SingleOrDefault(item => item.EventKey == alert.EventKey);
        if (existing is not null)
        {
            return Task.FromResult(new AlertAddResult(false, existing));
        }

        _alerts.Add(alert);
        return Task.FromResult(new AlertAddResult(true, alert));
    }

    public Task<bool> UpdateStatusAsync(
        Guid id,
        CruiseAlertStatus status,
        CancellationToken cancellationToken = default) => Task.FromResult(false);
}

internal sealed class TestAlertSettingsRepository(Exception? exception = null) : AlertSettingsRepository
{
    public Task<CruiseAlertSettings> GetAsync(CancellationToken cancellationToken = default) =>
        exception is null
            ? Task.FromResult(new CruiseAlertSettings())
            : Task.FromException<CruiseAlertSettings>(exception);

    public Task SaveAsync(
        CruiseAlertSettings settings,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
