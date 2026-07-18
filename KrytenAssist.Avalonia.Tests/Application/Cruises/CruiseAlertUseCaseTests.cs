extern alias KrytenApplication;

using FluentAssertions;
using KrytenAssist.Core.Cruises;
using ChangeCruiseAlertStatus = KrytenApplication::KrytenAssist.Application.Cruises.ChangeCruiseAlertStatus;
using CountUnreadCruiseAlerts = KrytenApplication::KrytenAssist.Application.Cruises.CountUnreadCruiseAlerts;
using CruiseAlertAddRepositoryResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertAddRepositoryResult;
using CruiseAlertEvaluationResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertEvaluationResult;
using CruiseAlertOperationStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertOperationStatus;
using CruiseAlertQuery = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertQuery;
using CruiseCriteriaEvidenceSelector = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCriteriaEvidenceSelector;
using CruiseObservationRecordResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRecordResult;
using CruiseObservationRecordStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRecordStatus;
using CruiseRecordAndAlertResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseRecordAndAlertResult;
using CruiseRecordedHistory = KrytenApplication::KrytenAssist.Application.Cruises.CruiseRecordedHistory;
using EvaluateSavedCruiseCriteriaAlerts = KrytenApplication::KrytenAssist.Application.Cruises.EvaluateSavedCruiseCriteriaAlerts;
using GetCruiseAlertSettings = KrytenApplication::KrytenAssist.Application.Cruises.GetCruiseAlertSettings;
using ICruiseAlertRepository = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseAlertRepository;
using ICruiseAlertSettingsRepository = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseAlertSettingsRepository;
using ISavedCruiseCriteriaStateRepository = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ISavedCruiseCriteriaStateRepository;
using ListCruiseAlerts = KrytenApplication::KrytenAssist.Application.Cruises.ListCruiseAlerts;
using MaterializeCruiseAlertCandidates = KrytenApplication::KrytenAssist.Application.Cruises.MaterializeCruiseAlertCandidates;
using SaveCruiseAlertSettings = KrytenApplication::KrytenAssist.Application.Cruises.SaveCruiseAlertSettings;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

public sealed class CruiseAlertUseCaseTests
{
    [Fact]
    public async Task Materialize_ReportsCreatedAndDeduplicatedUsingExplicitTimestamp()
    {
        var repository = new AlertRepository();
        var useCase = new MaterializeCruiseAlertCandidates(repository);
        var candidate = Candidate();
        var createdAt = new DateTimeOffset(2026, 7, 18, 12, 0, 0, TimeSpan.Zero);

        var created = await useCase.ExecuteAsync([candidate], createdAt);
        var duplicate = await useCase.ExecuteAsync([candidate], createdAt.AddDays(1));

        created.Status.Should().Be(CruiseAlertOperationStatus.Success);
        created.CandidateCount.Should().Be(1);
        created.CreatedAlerts.Single().CreatedAt.Should().Be(createdAt);
        duplicate.CreatedAlerts.Should().BeEmpty();
        duplicate.ExistingCount.Should().Be(1);
    }

    [Fact]
    public async Task List_IsDeterministicAndLifecycleSupportsUpdatedUnchangedAndNotFound()
    {
        var repository = new AlertRepository();
        var older = new CruiseAlert(Guid.Parse("00000000-0000-0000-0000-000000000001"), Candidate(), DateTimeOffset.UtcNow);
        var newerCandidate = Candidate("second", DateTimeOffset.UtcNow.AddDays(1));
        var newer = new CruiseAlert(Guid.Parse("00000000-0000-0000-0000-000000000002"), newerCandidate, DateTimeOffset.UtcNow);
        await repository.AddIfAbsentAsync(older);
        await repository.AddIfAbsentAsync(newer);

        var listed = await new ListCruiseAlerts(repository).ExecuteAsync(new CruiseAlertQuery());
        var changed = new ChangeCruiseAlertStatus(repository);

        listed.Alerts.Should().ContainInOrder(newer, older);
        (await changed.ExecuteAsync(older.Id, CruiseAlertStatus.Read)).Status.Should().Be(CruiseAlertOperationStatus.Updated);
        (await changed.ExecuteAsync(older.Id, CruiseAlertStatus.Read)).Status.Should().Be(CruiseAlertOperationStatus.Unchanged);
        (await changed.ExecuteAsync(Guid.NewGuid(), CruiseAlertStatus.Read)).Status.Should().Be(CruiseAlertOperationStatus.NotFound);
        (await new CountUnreadCruiseAlerts(repository).ExecuteAsync()).Count.Should().Be(1);
    }

    [Fact]
    public async Task Settings_DefaultAndChangedValuesReturnControlledStatuses()
    {
        var repository = new SettingsRepository();
        var get = await new GetCruiseAlertSettings(repository).ExecuteAsync();
        var save = new SaveCruiseAlertSettings(repository);
        var changed = new CruiseAlertSettings(minimumPriceDropPercentage: 5);

        get.Settings.Should().Be(new CruiseAlertSettings());
        (await save.ExecuteAsync(changed)).Status.Should().Be(CruiseAlertOperationStatus.Updated);
        (await save.ExecuteAsync(changed)).Status.Should().Be(CruiseAlertOperationStatus.Unchanged);
    }

    [Fact]
    public async Task CriteriaEvaluation_PersistsStateWhenNoAlertIsCreated()
    {
        var alerts = new AlertRepository();
        var states = new StateRepository();
        var useCase = new EvaluateSavedCruiseCriteriaAlerts(
            new SavedCruiseCriteriaAlertDetector(), new SettingsRepository(), states,
            new MaterializeCruiseAlertCandidates(alerts));
        var saved = SavedCruise();
        var evidence = new CruiseCriteriaEvidence(CruiseAlertEvidenceOrigin.SavedSnapshot, "snapshot", saved.Snapshot.SavedAt, []);

        var result = await useCase.ExecuteAsync(saved, new CruisePreferences([11]), evidence, saved.Snapshot.SavedAt);

        result.Status.Should().Be(CruiseAlertOperationStatus.Success);
        result.CandidateCount.Should().Be(0);
        result.CriteriaState!.Result.Should().Be(SavedCruiseCriteriaResult.NotMet);
        states.Value.Should().Be(result.CriteriaState);
    }

    [Fact]
    public async Task RepositoryFailuresAndCancellationAreContained()
    {
        var failed = new AlertRepository { Throw = true };
        (await new ListCruiseAlerts(failed).ExecuteAsync(new CruiseAlertQuery())).Status.Should().Be(CruiseAlertOperationStatus.Failed);
        (await new MaterializeCruiseAlertCandidates(failed).ExecuteAsync([Candidate()], DateTimeOffset.UtcNow)).Status.Should().Be(CruiseAlertOperationStatus.Failed);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        (await new CountUnreadCruiseAlerts(failed).ExecuteAsync(cancellation.Token)).Status.Should().Be(CruiseAlertOperationStatus.Cancelled);
    }

    [Fact]
    public void CompositeResult_PreservesSuccessfulRecordingWhenAlertEvaluationFails()
    {
        var observation = Observation(Key(), 900m, DateTimeOffset.UtcNow, "recorded");
        var recording = CruiseObservationRecordResult.Recorded(
            CruiseObservationRecordStatus.ChangedObservationRecorded,
            new CruisePriceHistoryAnalyzer().Analyze([observation]),
            observation.ObservedAt);

        var composite = new CruiseRecordAndAlertResult(recording, CruiseAlertEvaluationResult.Failed());

        composite.RecordingSucceeded.Should().BeTrue();
        composite.Alerts!.Status.Should().Be(CruiseAlertOperationStatus.Failed);
    }

    [Fact]
    public void EvidenceSelector_PrefersLatestRecordedEvidenceAndFallsBackToSnapshot()
    {
        var saved = SavedCruise();
        var selector = new CruiseCriteriaEvidenceSelector();
        var older = Observation(saved.SailingKey, 900m, saved.Snapshot.SavedAt.AddDays(1), "one");
        var latest = Observation(saved.SailingKey, 800m, saved.Snapshot.SavedAt.AddDays(2), "two");
        var history = new CruiseRecordedHistory(saved.SailingKey, latest.ObservedAt, [latest, older]);

        selector.Select(saved, [history]).Origin.Should().Be(CruiseAlertEvidenceOrigin.RecordedObservation);
        selector.Select(saved, [history]).Prices.Single().Amount.Should().Be(800m);
        selector.Select(saved, []).Origin.Should().Be(CruiseAlertEvidenceOrigin.SavedSnapshot);
    }

    private static CruiseAlertCandidate Candidate(string evidence = "evidence", DateTimeOffset? eventTime = null)
    {
        var source = new CruiseSource("retailer", "Retailer");
        var key = Key();
        return new CruiseAlertCandidate(CruiseAlertType.Promotion, key, source,
            new CruisePromotionAlertDetails(null, "New offer", evidence),
            eventTime ?? new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero), evidence);
    }

    private static CruiseSailingKey Key() => new("operator", "ship", new DateOnly(2026, 12, 18), 7);

    private static SavedCruise SavedCruise()
    {
        var snapshot = new SavedCruiseSnapshot(Key(), "Cruise", "Operator", new CruisePrice(900m, "GBP", "per person"),
            new DateTimeOffset(2026, 7, 18, 9, 0, 0, TimeSpan.Zero));
        return new SavedCruise(snapshot);
    }

    private static CruiseObservation Observation(CruiseSailingKey key, decimal price, DateTimeOffset time, string offerId) =>
        new(new CruiseSnapshot(new CruiseOffer(new CruiseProvider(key.OperatorId, "Operator"), offerId, "Cruise", key.ShipName,
            key.DepartureDate, key.DurationNights), [new CruisePrice(price, "GBP", "per person")]), time, source: new CruiseSource("retailer", "Retailer"));

    private sealed class AlertRepository : ICruiseAlertRepository
    {
        private readonly List<CruiseAlert> _alerts = [];
        public bool Throw { get; init; }
        public Task<CruiseAlert?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Throw ? Task.FromException<CruiseAlert?>(new InvalidOperationException()) : Task.FromResult(_alerts.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<CruiseAlert>> ListAsync(CruiseAlertQuery query, CancellationToken cancellationToken = default) =>
            Throw ? Task.FromException<IReadOnlyList<CruiseAlert>>(new InvalidOperationException()) : Task.FromResult<IReadOnlyList<CruiseAlert>>(_alerts.Where(x => (query.Type is null || x.Type == query.Type) && (query.Status is null || x.Status == query.Status)).ToArray());
        public Task<int> CountUnreadAsync(CancellationToken cancellationToken = default) =>
            Throw ? Task.FromException<int>(new InvalidOperationException()) : Task.FromResult(_alerts.Count(x => x.Status == CruiseAlertStatus.Unread));
        public Task<CruiseAlertAddRepositoryResult> AddIfAbsentAsync(CruiseAlert alert, CancellationToken cancellationToken = default)
        {
            if (Throw) return Task.FromException<CruiseAlertAddRepositoryResult>(new InvalidOperationException());
            var existing = _alerts.SingleOrDefault(x => x.EventKey == alert.EventKey);
            if (existing is not null) return Task.FromResult(new CruiseAlertAddRepositoryResult(false, existing));
            _alerts.Add(alert); return Task.FromResult(new CruiseAlertAddRepositoryResult(true, alert));
        }
        public Task<bool> UpdateStatusAsync(Guid id, CruiseAlertStatus status, CancellationToken cancellationToken = default)
        {
            var index = _alerts.FindIndex(x => x.Id == id);
            if (index < 0) return Task.FromResult(false);
            _alerts[index] = _alerts[index].WithStatus(status); return Task.FromResult(true);
        }
    }

    private sealed class SettingsRepository : ICruiseAlertSettingsRepository
    {
        private CruiseAlertSettings _settings = new();
        public Task<CruiseAlertSettings> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(_settings);
        public Task SaveAsync(CruiseAlertSettings settings, CancellationToken cancellationToken = default) { _settings = settings; return Task.CompletedTask; }
    }

    private sealed class StateRepository : ISavedCruiseCriteriaStateRepository
    {
        public SavedCruiseCriteriaEvaluationState? Value { get; private set; }
        public Task<SavedCruiseCriteriaEvaluationState?> GetAsync(CruiseSailingKey sailingKey, string criteriaFingerprint, CancellationToken cancellationToken = default) => Task.FromResult(Value);
        public Task UpsertAsync(SavedCruiseCriteriaEvaluationState state, CancellationToken cancellationToken = default) { Value = state; return Task.CompletedTask; }
    }
}
