extern alias KrytenApplication;

using FluentAssertions;
using KrytenAssist.Core.Cruises;
using KrytenAssist.Avalonia.Cruises.Discovery;
using KrytenAssist.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ApplicationDependencyInjection = KrytenApplication::KrytenAssist.Application.DependencyInjection;
using DiscoveryRepository = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseDiscoveryRepository;
using CatalogueEntry = KrytenApplication::KrytenAssist.Application.Cruises.CruiseItineraryCatalogueEntry;
using RepositoryResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseDiscoveryRepositoryRecordResult;
using RepositoryState = KrytenApplication::KrytenAssist.Application.Cruises.CruiseDiscoveryRecordState;
using OperationStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseDiscoveryOperationStatus;
using RecordCheck = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseDiscoveryCheck;
using ListItineraries = KrytenApplication::KrytenAssist.Application.Cruises.ListFirstObservedCruiseItineraries;
using GetItinerary = KrytenApplication::KrytenAssist.Application.Cruises.GetCruiseItineraryDiscovery;
using ListChecks = KrytenApplication::KrytenAssist.Application.Cruises.ListCruiseDiscoveryChecks;
using CaptureCandidate = KrytenApplication::KrytenAssist.Application.Cruises.CruiseItineraryCaptureCandidateResult;
using ListItineraryDetails = KrytenApplication::KrytenAssist.Application.Cruises.ListFirstObservedCruiseItineraryDetails;
using AlertRepository = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseAlertRepository;
using AlertSettingsRepository = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseAlertSettingsRepository;
using RecordAndAlert = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseDiscoveryCheckAndEvaluateAlerts;
using MaterializeAlerts = KrytenApplication::KrytenAssist.Application.Cruises.MaterializeCruiseAlertCandidates;
using AlertQuery = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertQuery;
using AlertAddResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertAddRepositoryResult;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

public sealed class CruiseDiscoveryApplicationTests
{
    private static readonly DateTimeOffset ObservedAt = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(RepositoryState.BaselineSeeded, OperationStatus.BaselineSeeded)]
    [InlineData(RepositoryState.RecordedNoNewItineraries, OperationStatus.RecordedNoNewItineraries)]
    [InlineData(RepositoryState.RecordedWithFirstObserved, OperationStatus.RecordedWithFirstObserved)]
    [InlineData(RepositoryState.AlreadyRecorded, OperationStatus.AlreadyRecorded)]
    public async Task Record_maps_confirmed_repository_states(RepositoryState state, OperationStatus expected)
    {
        var check = Check("one");
        var repository = new FakeRepository { RecordResult = new(state, check, []) };

        var result = await new RecordCheck(repository).ExecuteAsync(check);

        result.Status.Should().Be(expected);
        result.Check.Should().BeSameAs(check);
    }

    [Fact]
    public async Task Queries_apply_deterministic_newest_first_order()
    {
        var older = Entry("older", ObservedAt);
        var newer = Entry("newer", ObservedAt.AddDays(1));
        var earlierCheck = Check("one", ObservedAt);
        var laterCheck = Check("two", ObservedAt.AddDays(1));
        var repository = new FakeRepository { Entries = [older, newer], Checks = [earlierCheck, laterCheck], GetResult = older };

        (await new ListItineraries(repository).ExecuteAsync()).Entries.Should().Equal(newer, older);
        (await new ListChecks(repository).ExecuteAsync()).Checks.Should().Equal(laterCheck, earlierCheck);
        (await new GetItinerary(repository).ExecuteAsync(older.CatalogueKey.PersistenceKey)).Status.Should().Be(OperationStatus.Found);
    }

    [Fact]
    public async Task PresentationQuery_ResolvesExactConfirmingScopeAndRejectsContradictoryEvidence()
    {
        var check = Check("new", ObservedAt);
        var occurrence = check.Occurrences.Single();
        var observedEvent = new CruiseItineraryFirstObservedEvent(occurrence, check.Scope.Fingerprint, check.EvidenceKey);
        var entry = new CatalogueEntry(occurrence.CatalogueKey, occurrence, occurrence, ObservedAt, ObservedAt, observedEvent.EventKey);
        var repository = new FakeRepository { Entries = [entry], Checks = [check] };

        var result = await new ListItineraryDetails(repository).ExecuteAsync();

        result.Status.Should().Be(OperationStatus.Success);
        result.Items.Should().ContainSingle().Which.ConfirmingCheck.Should().Be(check);

        var contradictory = new FakeRepository { Entries = [entry], Checks = [Check("other", ObservedAt)] };
        (await new ListItineraryDetails(contradictory).ExecuteAsync()).Status.Should().Be(OperationStatus.Failed);
    }

    [Fact]
    public async Task NewItinerariesPresentation_LoadsHonestLocalEvidence()
    {
        var check = Check("new", ObservedAt);
        var occurrence = check.Occurrences.Single();
        var observedEvent = new CruiseItineraryFirstObservedEvent(occurrence, check.Scope.Fingerprint, check.EvidenceKey);
        var entry = new CatalogueEntry(occurrence.CatalogueKey, occurrence, occurrence, ObservedAt, ObservedAt, observedEvent.EventKey);
        var viewModel = new CruiseNewItinerariesViewModel(new ListItineraryDetails(new FakeRepository { Entries = [entry], Checks = [check] }),
            new CruiseDiscoverySourceCatalog(), new CruiseTrustedHostPolicy());

        await viewModel.ActivateAsync();

        viewModel.Items.Should().ContainSingle();
        viewModel.SelectedItem!.FirstObservedHeading.Should().Contain("First observed by Kryten");
        viewModel.SelectedItem.Disclaimer.Should().Contain("does not prove when TUI published");
        viewModel.CanOpenSelectedInDiscovery.Should().BeFalse();
    }

    [Fact]
    public async Task Use_cases_contain_cancellation_and_repository_failure()
    {
        var repository = new FakeRepository { Exception = new InvalidOperationException() };
        (await new RecordCheck(repository).ExecuteAsync(Check("one"))).Status.Should().Be(OperationStatus.Failed);
        (await new ListItineraries(repository).ExecuteAsync()).Status.Should().Be(OperationStatus.Failed);
        (await new GetItinerary(repository).ExecuteAsync("key")).Status.Should().Be(OperationStatus.Failed);
        (await new ListChecks(repository).ExecuteAsync()).Status.Should().Be(OperationStatus.Failed);

        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        (await new RecordCheck(repository).ExecuteAsync(Check("one"), cancellation.Token)).Status.Should().Be(OperationStatus.Cancelled);
    }

    [Fact]
    public void Capture_candidates_enforce_controlled_ready_and_ineligible_shapes()
    {
        CaptureCandidate.Ready("Itinerary", Occurrence("one", ObservedAt)).Occurrence.Should().NotBeNull();
        CaptureCandidate.Ineligible("Unknown", ["itineraryId"], "No stable itinerary id").MissingFields.Should().Equal("itineraryId");
        FluentActions.Invoking(() => CaptureCandidate.Ineligible("Unknown", [], "Missing"))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Application_registration_includes_discovery_services_once_repository_contract_is_available()
    {
        var services = new ServiceCollection();
        ApplicationDependencyInjection.AddApplication(services);

        services.Should().Contain(x => x.ServiceType == typeof(CruiseNewItineraryDetector));
        services.Should().Contain(x => x.ServiceType == typeof(RecordCheck));
        services.Should().Contain(x => x.ServiceType == typeof(ListItineraries));
        services.Should().Contain(x => x.ServiceType == typeof(GetItinerary));
        services.Should().Contain(x => x.ServiceType == typeof(ListChecks));
        services.Should().Contain(x => x.ServiceType == typeof(ListItineraryDetails));
        services.Should().Contain(x => x.ServiceType == typeof(CruiseNewItineraryAlertDetector));
        services.Should().Contain(x => x.ServiceType == typeof(RecordAndAlert));
    }

    [Fact]
    public async Task RecordingOrchestration_CommitsFirstThenMaterializesAndRetriesIdempotently()
    {
        var check = Check("new");
        var discovered = new CruiseItineraryFirstObservedEvent(check.Occurrences.Single(), check.Scope.Fingerprint, check.EvidenceKey);
        var discovery = new FakeRepository { RecordResult = new(RepositoryState.RecordedWithFirstObserved, check, [discovered]) };
        var alerts = new FakeAlertRepository();
        var useCase = new RecordAndAlert(new RecordCheck(discovery), new FakeSettingsRepository(),
            new CruiseNewItineraryAlertDetector(), new MaterializeAlerts(alerts));

        var first = await useCase.ExecuteAsync(check, ObservedAt.AddMinutes(1));
        var retry = await useCase.ExecuteAsync(check, ObservedAt.AddMinutes(2));

        first.Recording.Status.Should().Be(OperationStatus.RecordedWithFirstObserved);
        first.AlertEvaluation.Should().Be(KrytenApplication::KrytenAssist.Application.Cruises.CruiseDiscoveryAlertEvaluationStatus.Success);
        first.Alerts!.CreatedAlerts.Should().ContainSingle();
        retry.Alerts!.CreatedAlerts.Should().BeEmpty(); retry.Alerts!.ExistingCount.Should().Be(1);
        alerts.Values.Should().ContainSingle();
    }

    [Fact]
    public async Task RecordingOrchestration_PreservesCommittedResultWhenAlertSettingsFail()
    {
        var check = Check("new");
        var discovered = new CruiseItineraryFirstObservedEvent(check.Occurrences.Single(), check.Scope.Fingerprint, check.EvidenceKey);
        var discovery = new FakeRepository { RecordResult = new(RepositoryState.AlreadyRecorded, check, [discovered]) };
        var result = await new RecordAndAlert(new RecordCheck(discovery), new FakeSettingsRepository { Exception = new InvalidOperationException() },
            new CruiseNewItineraryAlertDetector(), new MaterializeAlerts(new FakeAlertRepository())).ExecuteAsync(check, ObservedAt);

        result.Recording.Status.Should().Be(OperationStatus.AlreadyRecorded);
        result.AlertEvaluation.Should().Be(KrytenApplication::KrytenAssist.Application.Cruises.CruiseDiscoveryAlertEvaluationStatus.Failed);
    }

    private static CatalogueEntry Entry(string id, DateTimeOffset time)
    {
        var occurrence = Occurrence(id, time);
        return new(occurrence.CatalogueKey, occurrence, occurrence, time, time, $"event-{id}");
    }

    private static CruiseDiscoveryCheck Check(string id, DateTimeOffset? time = null)
    {
        var observedAt = time ?? ObservedAt;
        var scope = new CruiseDiscoveryScope(new("tui", "TUI"), "marella", CruiseDiscoverySurface.CruisePackages, 1);
        return new(scope, observedAt, [Occurrence(id, observedAt)]);
    }

    private static CruiseItineraryOccurrence Occurrence(string id, DateTimeOffset time) =>
        new(new CruiseItineraryKey("marella", id), new CruiseSource("tui", "TUI"), time, $"evidence-{id}");

    private sealed class FakeRepository : DiscoveryRepository
    {
        public RepositoryResult? RecordResult { get; init; }
        public IReadOnlyList<CatalogueEntry> Entries { get; init; } = [];
        public CatalogueEntry? GetResult { get; init; }
        public IReadOnlyList<CruiseDiscoveryCheck> Checks { get; init; } = [];
        public Exception? Exception { get; init; }
        public Task<RepositoryResult> RecordAsync(CruiseDiscoveryCheck check, CancellationToken cancellationToken = default) =>
            Exception is null ? Task.FromResult(RecordResult!) : Task.FromException<RepositoryResult>(Exception);
        public Task<IReadOnlyList<CatalogueEntry>> ListFirstObservedAsync(CancellationToken cancellationToken = default) =>
            Exception is null ? Task.FromResult(Entries) : Task.FromException<IReadOnlyList<CatalogueEntry>>(Exception);
        public Task<CatalogueEntry?> GetAsync(string catalogueKey, CancellationToken cancellationToken = default) =>
            Exception is null ? Task.FromResult(GetResult) : Task.FromException<CatalogueEntry?>(Exception);
        public Task<IReadOnlyList<CruiseDiscoveryCheck>> ListChecksAsync(CancellationToken cancellationToken = default) =>
            Exception is null ? Task.FromResult(Checks) : Task.FromException<IReadOnlyList<CruiseDiscoveryCheck>>(Exception);
    }

    private sealed class FakeSettingsRepository : AlertSettingsRepository
    {
        public Exception? Exception { get; init; }
        public Task<CruiseAlertSettings> GetAsync(CancellationToken cancellationToken = default) =>
            Exception is null ? Task.FromResult(new CruiseAlertSettings()) : Task.FromException<CruiseAlertSettings>(Exception);
        public Task SaveAsync(CruiseAlertSettings settings, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeAlertRepository : AlertRepository
    {
        public List<CruiseAlert> Values { get; } = [];
        public Task<CruiseAlert?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Values.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<CruiseAlert>> ListAsync(AlertQuery query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CruiseAlert>>(Values);
        public Task<int> CountUnreadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Values.Count);
        public Task<AlertAddResult> AddIfAbsentAsync(CruiseAlert alert, CancellationToken cancellationToken = default)
        {
            var existing = Values.SingleOrDefault(x => x.EventKey == alert.EventKey);
            if (existing is not null) return Task.FromResult(new AlertAddResult(false, existing));
            Values.Add(alert); return Task.FromResult(new AlertAddResult(true, alert));
        }
        public Task<bool> UpdateStatusAsync(Guid id, CruiseAlertStatus status, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}
