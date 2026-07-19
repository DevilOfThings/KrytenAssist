extern alias KrytenApplication;

using FluentAssertions;
using KrytenAssist.Core.Cruises;
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
    public void Application_registration_includes_only_repository_independent_discovery_service()
    {
        var services = new ServiceCollection();
        ApplicationDependencyInjection.AddApplication(services);

        services.Should().Contain(x => x.ServiceType == typeof(CruiseNewItineraryDetector));
        services.Should().NotContain(x => x.ServiceType == typeof(RecordCheck));
        services.Should().NotContain(x => x.ServiceType == typeof(ListItineraries));
        services.Should().NotContain(x => x.ServiceType == typeof(GetItinerary));
        services.Should().NotContain(x => x.ServiceType == typeof(ListChecks));
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
}
