extern alias KrytenApplication;

using KrytenAssist.Avalonia.ViewModels;
using KrytenAssist.Core.Cruises;
using CabinRepository = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseCabinObservationRepository;
using PreferenceRepository = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruisePreferencesRepository;
using CabinHistory = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinRecordedHistory;
using CabinLatestEvidence = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinLatestEvidence;
using CabinRecordResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinRepositoryRecordResult;
using ListHistories = KrytenApplication::KrytenAssist.Application.Cruises.ListCruiseCabinHistories;
using GetPreferences = KrytenApplication::KrytenAssist.Application.Cruises.GetCruisePreferences;
using CabinDetails = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinHistoryDetails;

namespace KrytenAssist.Avalonia.Tests.ViewModels;

public sealed class CruiseCabinAvailabilityViewModelTests
{
    [Fact]
    public void Projection_ShowsCanonicalStatesKnownContextAndPreferredOrMatch()
    {
        var observation = Observation(new CruiseCabinSearchContext(2, 0, [], true,
            CruiseCabinPackageMode.FlyCruise, "STN", 1), CruiseCabinEvidenceCoverage.Partial,
            CruiseCabinAvailabilityState.Available, CruiseCabinAvailabilityState.Unknown,
            CruiseCabinAvailabilityState.Unknown, CruiseCabinAvailabilityState.Unknown,
            CruiseCabinAvailabilityState.Unknown);
        var item = Item([observation], preferences: new CruisePreferences(preferredCabins:
            [CruiseCabinType.Inside, CruiseCabinType.Balcony]));

        Assert.Equal(["Inside", "Outside", "Balcony", "Suite", "Solo"], item.CategoryStates.Select(row => row.CabinType));
        Assert.Equal("Available when recorded for this search", item.CategoryStates[0].StateText);
        Assert.All(item.CategoryStates.Skip(1), row => Assert.Equal("Unknown", row.StateText));
        Assert.Equal("2", item.AdultCountText);
        Assert.Equal("0", item.ChildCountText);
        Assert.Equal("Known (none)", item.ChildAgesText);
        Assert.Equal("Fly cruise", item.PackageModeText);
        Assert.Equal("STN", item.DepartureAirportText);
        Assert.Equal("1", item.CabinQuantityText);
        Assert.Contains("Unknown categories were not proven unavailable", item.CoverageExplanation);
        Assert.Equal("Matches your cabin preferences for this search: Inside", item.PreferenceStatus);
    }

    [Fact]
    public void Projection_PreservesUnknownContextAndPreferenceStates()
    {
        var observation = Observation(new CruiseCabinSearchContext(childCount: 1), CruiseCabinEvidenceCoverage.Partial,
            CruiseCabinAvailabilityState.Unavailable, CruiseCabinAvailabilityState.Unknown,
            CruiseCabinAvailabilityState.Unavailable, CruiseCabinAvailabilityState.Unavailable,
            CruiseCabinAvailabilityState.Unavailable);

        var unknownMatch = Item([observation], new CruisePreferences(preferredCabins: [CruiseCabinType.Outside]));
        Assert.Equal("Unknown", unknownMatch.AdultCountText);
        Assert.Equal("1", unknownMatch.ChildCountText);
        Assert.Equal("Unknown", unknownMatch.ChildAgesText);
        Assert.Equal("Unknown", unknownMatch.PackageModeText);
        Assert.Equal("Unknown", unknownMatch.DepartureAirportText);
        Assert.Equal("Unknown", unknownMatch.CabinQuantityText);
        Assert.Contains("Preference match unknown", unknownMatch.PreferenceStatus);

        Assert.Contains("No preferred cabin type was available", Item([observation],
            new CruisePreferences(preferredCabins: [CruiseCabinType.Inside, CruiseCabinType.Balcony])).PreferenceStatus);
        Assert.Equal("No preferred cabin types configured", Item([observation], new CruisePreferences()).PreferenceStatus);
        Assert.Equal("Preference matching is temporarily unavailable.", Item([observation], null, true).PreferenceStatus);
    }

    [Fact]
    public void Projection_ShowsExplicitAndKnowledgeChangesWithoutConflatingThem()
    {
        var first = Observation(new CruiseCabinSearchContext(2), CruiseCabinEvidenceCoverage.Partial,
            CruiseCabinAvailabilityState.Unavailable, CruiseCabinAvailabilityState.Unknown,
            CruiseCabinAvailabilityState.Available, CruiseCabinAvailabilityState.Unknown,
            CruiseCabinAvailabilityState.Unknown, hour: 9);
        var latest = Observation(new CruiseCabinSearchContext(2), CruiseCabinEvidenceCoverage.Partial,
            CruiseCabinAvailabilityState.Available, CruiseCabinAvailabilityState.Unavailable,
            CruiseCabinAvailabilityState.Unknown, CruiseCabinAvailabilityState.Unknown,
            CruiseCabinAvailabilityState.Unknown, hour: 10);
        var item = Item([latest, first]);

        Assert.Contains(item.LatestChanges, change => change.CabinType == "Inside" && change.ChangeText == "Became available when recorded");
        Assert.Contains(item.LatestChanges, change => change.CabinType == "Outside" && change.ChangeText == "New evidence recorded");
        Assert.Contains(item.LatestChanges, change => change.CabinType == "Balcony" && change.ChangeText.Contains("no longer confirms"));
        Assert.StartsWith("evidence-10-", item.Timeline[0].EvidenceKey, StringComparison.Ordinal);
        Assert.True(item.Timeline[0].IsLatest);
    }

    [Fact]
    public async Task Activate_LoadsSeparateOrderedSeriesAndRetainsExactSelection()
    {
        var later = Observation(new CruiseCabinSearchContext(2), sourceId: "tui-b", departureDay: 22);
        var earlierA = Observation(new CruiseCabinSearchContext(2, cabinQuantity: 1), sourceId: "tui-a", departureDay: 20);
        var earlierB = Observation(new CruiseCabinSearchContext(2, cabinQuantity: 2), sourceId: "tui-a", departureDay: 20);
        var repository = new MemoryCabinRepository([History(later), History(earlierB), History(earlierA)]);
        var viewModel = Create(repository);

        await viewModel.ActivateAsync();

        Assert.Equal(3, viewModel.Items.Count);
        Assert.Equal(earlierA.SeriesKey, viewModel.Items[0].SeriesKey);
        viewModel.SelectedItem = viewModel.Items[1];
        var selectedKey = viewModel.SelectedItem.SeriesKey;
        await viewModel.RefreshAsync();
        Assert.Equal(selectedKey, viewModel.SelectedItem?.SeriesKey);
    }

    [Fact]
    public async Task PreferenceFailure_LeavesCabinHistoryUsable()
    {
        var viewModel = Create(new MemoryCabinRepository([History(Observation(new CruiseCabinSearchContext(2)))]),
            new MemoryPreferenceRepository { Failure = new InvalidOperationException("private detail") });

        await viewModel.ActivateAsync();

        Assert.Single(viewModel.Items);
        Assert.True(viewModel.HasPreferenceMessage);
        Assert.False(viewModel.HasError);
        Assert.DoesNotContain("private detail", viewModel.PreferenceMessage);
    }

    [Fact]
    public async Task RefreshFailure_RetainsPriorSuccessfulItemsAndSelection()
    {
        var observation = Observation(new CruiseCabinSearchContext(2));
        var repository = new MemoryCabinRepository([History(observation)]);
        var viewModel = Create(repository);
        await viewModel.ActivateAsync();
        var selected = viewModel.SelectedItem;
        repository.Failure = new InvalidOperationException("database detail");

        await viewModel.RefreshAsync();

        Assert.Single(viewModel.Items);
        Assert.Same(selected, viewModel.SelectedItem);
        Assert.True(viewModel.HasError);
        Assert.DoesNotContain("database detail", viewModel.ErrorMessage);
    }

    private static CruiseCabinAvailabilityViewModel Create(MemoryCabinRepository cabins,
        MemoryPreferenceRepository? preferences = null) => new(
        new ListHistories(cabins, new CruiseCabinHistoryAnalyzer()),
        new GetPreferences(preferences ?? new MemoryPreferenceRepository()));

    private static CruiseCabinAvailabilityItemViewModel Item(IEnumerable<CruiseCabinObservation> observations,
        CruisePreferences? preferences = null, bool preferenceUnavailable = false)
    {
        var history = History(observations.ToArray());
        var analyzer = new CruiseCabinHistoryAnalyzer();
        return new(new CabinDetails(history, analyzer.Analyze(history.Observations, history.LastSeenAt)), preferences, preferenceUnavailable);
    }

    private static CabinHistory History(params CruiseCabinObservation[] observations)
    {
        var latest = observations.OrderBy(value => value.ObservedAt).Last();
        return new(latest.SeriesKey, latest.ObservedAt.AddHours(2), observations,
            new CabinLatestEvidence(latest.EvidenceKey, latest.SourceReference, latest.ObservedAt));
    }

    private static CruiseCabinObservation Observation(CruiseCabinSearchContext context,
        CruiseCabinEvidenceCoverage coverage = CruiseCabinEvidenceCoverage.Complete,
        CruiseCabinAvailabilityState inside = CruiseCabinAvailabilityState.Available,
        CruiseCabinAvailabilityState outside = CruiseCabinAvailabilityState.Unavailable,
        CruiseCabinAvailabilityState balcony = CruiseCabinAvailabilityState.Unavailable,
        CruiseCabinAvailabilityState suite = CruiseCabinAvailabilityState.Unavailable,
        CruiseCabinAvailabilityState solo = CruiseCabinAvailabilityState.Unavailable,
        int hour = 10, string sourceId = "tui", int departureDay = 20) => new(
            new CruiseSailingKey("marella", "Marella Explorer", new DateOnly(2026, 10, departureDay), 7),
            new CruiseSource(sourceId, sourceId.ToUpperInvariant()), context, coverage,
            [new(CruiseCabinType.Inside, inside), new(CruiseCabinType.Outside, outside),
             new(CruiseCabinType.Balcony, balcony), new(CruiseCabinType.Suite, suite), new(CruiseCabinType.Solo, solo)],
            new DateTimeOffset(2026, 7, 19, hour, 0, 0, TimeSpan.Zero), $"evidence-{hour}-{context.Fingerprint}",
            "https://www.tui.co.uk/cruise/example");

    private sealed class MemoryCabinRepository(IReadOnlyList<CabinHistory> histories) : CabinRepository
    {
        public Exception? Failure { get; set; }
        public Task<IReadOnlyList<CabinHistory>> ListAsync(CancellationToken cancellationToken = default) =>
            Failure is null ? Task.FromResult(histories) : Task.FromException<IReadOnlyList<CabinHistory>>(Failure);
        public Task<CabinHistory?> GetAsync(string seriesKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(histories.FirstOrDefault(value => value.SeriesKey == seriesKey));
        public Task<CabinRecordResult> RecordAsync(CruiseCabinObservation observation, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class MemoryPreferenceRepository : PreferenceRepository
    {
        public Exception? Failure { get; init; }
        public CruisePreferences Preferences { get; init; } = new();
        public Task<CruisePreferences> GetAsync(CancellationToken cancellationToken = default) =>
            Failure is null ? Task.FromResult(Preferences) : Task.FromException<CruisePreferences>(Failure);
        public Task SaveAsync(CruisePreferences preferences, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
