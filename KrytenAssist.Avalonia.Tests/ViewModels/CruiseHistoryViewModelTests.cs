extern alias KrytenApplication;

using System.ComponentModel;
using System.Windows.Input;
using KrytenAssist.Avalonia.Tests.Application.Cruises;
using KrytenAssist.Avalonia.Cruises.Discovery;
using KrytenAssist.Avalonia.Tools;
using KrytenAssist.Avalonia.ViewModels;
using KrytenAssist.Avalonia.Navigation.Models;
using KrytenAssist.Avalonia.Skills.Models;
using KrytenAssist.Core.Cruises;
using GetHistory = KrytenApplication::KrytenAssist.Application.Cruises.GetCruiseHistory;
using ListHistories = KrytenApplication::KrytenAssist.Application.Cruises.ListCruiseHistories;
using RecordObservation = KrytenApplication::KrytenAssist.Application.Cruises.RecordCruiseObservation;
using RepositoryResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRepositoryRecordResult;
using RepositoryState = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRepositoryRecordState;

namespace KrytenAssist.Avalonia.Tests.ViewModels;

public sealed class CruiseHistoryViewModelTests
{
    [Fact]
    public void Record_IsDisabledUntilCapturedObservationIsSupplied()
    {
        var (viewModel, _) = CreateViewModel();

        Assert.False(viewModel.RecordObservationCommand.CanExecute(null));
        viewModel.SetCapturedObservation(CruiseHistoryApplicationTestData.Observation());

        Assert.True(viewModel.RecordObservationCommand.CanExecute(null));
    }

    [Fact]
    public void FirstRecord_PassesExactObservationRefreshesAndSelectsHistory()
    {
        var observation = CruiseHistoryApplicationTestData.Observation();
        var history = CruiseHistoryApplicationTestData.History(observation);
        var (viewModel, repository) = CreateViewModel();
        repository.RecordResult = new RepositoryResult(RepositoryState.FirstObservationRecorded, history);
        repository.ListResult = [history];
        viewModel.SetCapturedObservation(observation);

        viewModel.RecordObservationCommand.Execute(null);

        Assert.Equal(1, repository.RecordCalls);
        Assert.Same(observation, repository.RecordedObservation);
        Assert.Equal(1, repository.ListCalls);
        Assert.Contains("first price seen", viewModel.RecordMessage, StringComparison.OrdinalIgnoreCase);
        Assert.False(viewModel.RecordObservationCommand.CanExecute(null));
        Assert.Single(viewModel.Histories);
        Assert.NotNull(viewModel.SelectedHistory);
        Assert.Equal("Atlantic Discovery", viewModel.SelectedHistory.Title);
    }

    [Fact]
    public void ChangedAndAlreadyCurrent_UseAccurateControlledMessages()
    {
        var first = CruiseHistoryApplicationTestData.Observation();
        var lower = CruiseHistoryApplicationTestData.Observation(
            949m,
            CruiseHistoryApplicationTestData.FirstObserved.AddDays(1));
        var changedHistory = CruiseHistoryApplicationTestData.History(first, lower);
        var (changedViewModel, changedRepository) = CreateViewModel();
        changedRepository.RecordResult = new RepositoryResult(
            RepositoryState.ChangedObservationRecorded,
            changedHistory);
        changedRepository.ListResult = [changedHistory];
        changedViewModel.SetCapturedObservation(lower);

        changedViewModel.RecordObservationCommand.Execute(null);

        Assert.Contains("£39", changedViewModel.RecordMessage);
        Assert.Contains("lower", changedViewModel.RecordMessage, StringComparison.OrdinalIgnoreCase);

        var (currentViewModel, currentRepository) = CreateViewModel();
        currentRepository.RecordResult = new RepositoryResult(
            RepositoryState.AlreadyCurrent,
            CruiseHistoryApplicationTestData.History(first));
        currentRepository.ListResult = [CruiseHistoryApplicationTestData.History(first)];
        currentViewModel.SetCapturedObservation(first);

        currentViewModel.RecordObservationCommand.Execute(null);

        Assert.Contains("No new snapshot", currentViewModel.RecordMessage);
        Assert.False(currentViewModel.RecordObservationCommand.CanExecute(null));
    }

    [Fact]
    public void FailedRecord_RetainsReviewAndAllowsRetry()
    {
        var observation = CruiseHistoryApplicationTestData.Observation();
        var (viewModel, repository) = CreateViewModel();
        repository.RecordException = new InvalidOperationException("secret database path");
        viewModel.SetCapturedObservation(observation);

        viewModel.RecordObservationCommand.Execute(null);

        Assert.Same(observation, viewModel.CapturedObservation);
        Assert.Contains("could not be recorded", viewModel.RecordMessage);
        Assert.DoesNotContain("database", viewModel.RecordMessage);
        Assert.True(viewModel.RecordObservationCommand.CanExecute(null));
    }

    [Fact]
    public void Activate_LoadsOnceAndRefreshPreservesMatchingSelection()
    {
        var first = CruiseHistoryApplicationTestData.Observation(
            departure: new DateOnly(2025, 1, 1));
        var second = CruiseHistoryApplicationTestData.Observation(
            title: "Future cruise",
            ship: "Future Ship",
            departure: new DateOnly(2027, 1, 1));
        var (viewModel, repository) = CreateViewModel();
        repository.ListResult =
        [
            CruiseHistoryApplicationTestData.History(first),
            CruiseHistoryApplicationTestData.History(second)
        ];

        viewModel.Activate();
        viewModel.Activate();

        Assert.Equal(1, repository.ListCalls);
        Assert.Equal(2, viewModel.Histories.Count);
        Assert.True(viewModel.Histories[0].IsPastSailing);
        viewModel.SelectedHistory = viewModel.Histories[1];
        viewModel.RefreshHistoryCommand.Execute(null);

        Assert.Equal(2, repository.ListCalls);
        Assert.Equal("Future cruise", viewModel.SelectedHistory!.Title);
    }

    [Fact]
    public void BrowserClose_ClearsCapturedReviewButRetainsLoadedHistory()
    {
        var observation = CruiseHistoryApplicationTestData.Observation();
        var (historyViewModel, repository) = CreateViewModel();
        repository.ListResult = [CruiseHistoryApplicationTestData.History(observation)];
        historyViewModel.Activate();
        historyViewModel.SetCapturedObservation(observation);
        var browser = new CruiseBrowserFeasibilityViewModel(
            new CruiseDiscoverySourceCatalog(),
            new CruiseTrustedHostPolicy(),
            history: historyViewModel);

        browser.ReportBrowserClosed();

        Assert.Null(historyViewModel.CapturedObservation);
        Assert.Single(historyViewModel.Histories);
        Assert.NotNull(historyViewModel.SelectedHistory);
    }

    [Fact]
    public void Item_FormatsPricesTrendsAndDistinctObservationTimes()
    {
        var first = CruiseHistoryApplicationTestData.Observation();
        var lower = CruiseHistoryApplicationTestData.Observation(
            949m,
            CruiseHistoryApplicationTestData.FirstObserved.AddDays(1));
        var history = CruiseHistoryApplicationTestData.History(first, lower);
        var details = new KrytenApplication::KrytenAssist.Application.Cruises.CruiseHistoryDetails(
            history,
            history.Analyze(new CruisePriceHistoryAnalyzer()));

        var item = new CruiseHistoryItemViewModel(details, new FixedClock());

        Assert.Equal("£949 per person", item.CurrentPrice);
        Assert.Equal("£949 per person", item.LowestPrice);
        Assert.Equal("£988 per person", item.HighestPrice);
        Assert.Equal("Down £39", item.Trend);
        Assert.NotEqual(item.LastObserved, item.LastSeen);
        Assert.Equal("USD 25 total", CruiseHistoryItemViewModel.FormatPrice(new CruisePrice(25m, "USD", "total")));
        Assert.Equal("Comparable price unavailable", CruiseHistoryItemViewModel.FormatPrice(null));
    }

    [Fact]
    public void ShellSelection_ActivatesOfflineHistoryLoad()
    {
        var (historyViewModel, repository) = CreateViewModel();
        var cruiseSkill = new ShellTestFactory.CountingSkill(new SkillManifest(
            "cruise.of-the-week",
            "Cruise of the Week",
            "Cruise research",
            "1.0.0"));
        var registry = ShellTestFactory.CreateRegistry(cruiseSkill);
        var cruise = new CruiseOfTheWeekViewModel(
            registry,
            new FixedClock(),
            history: historyViewModel);
        var shell = new ShellViewModel(
            ShellTestFactory.CreateAssistantWorkspace(),
            cruise,
            registry);
        var navigation = shell.NavigationItems.Single(item =>
            item.Kind == NavigationDestinationKind.Skill
            && item.SkillId == "cruise.of-the-week");

        shell.NavigateCommand.Execute(navigation);

        Assert.True(shell.IsCruiseOfTheWeekSelected);
        Assert.Equal(1, repository.ListCalls);
        Assert.False(cruise.BrowserFeasibility.HasStarted);
    }

    [Fact]
    public async Task Recording_CanBeCancelledAndRetriedWithoutDuplicateExecution()
    {
        var observation = CruiseHistoryApplicationTestData.Observation();
        var (viewModel, repository) = CreateViewModel();
        var firstCompletion = new TaskCompletionSource<RepositoryResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        repository.RecordHandler = (_, _, _, cancellationToken) =>
        {
            cancellationToken.Register(() => firstCompletion.TrySetCanceled(cancellationToken));
            return firstCompletion.Task;
        };
        viewModel.SetCapturedObservation(observation);

        viewModel.RecordObservationCommand.Execute(null);
        viewModel.RecordObservationCommand.Execute(null);

        Assert.True(viewModel.IsRecording);
        Assert.True(viewModel.CancelRecordingCommand.CanExecute(null));
        Assert.Equal(1, repository.RecordCalls);
        var cancelled = WaitUntilAsync(viewModel, () => !viewModel.IsRecording);
        var retryAvailable = WaitUntilCanExecuteAsync(viewModel.RecordObservationCommand);
        viewModel.CancelRecordingCommand.Execute(null);
        await cancelled;
        await retryAvailable;

        Assert.True(repository.RecordedToken.IsCancellationRequested);
        Assert.Contains("cancelled", viewModel.RecordMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Same(observation, viewModel.CapturedObservation);
        Assert.True(viewModel.RecordObservationCommand.CanExecute(null));

        var history = CruiseHistoryApplicationTestData.History(observation);
        repository.RecordHandler = (_, _, _, _) => Task.FromResult(
            new RepositoryResult(RepositoryState.FirstObservationRecorded, history));
        repository.ListResult = [history];

        viewModel.RecordObservationCommand.Execute(null);

        Assert.Equal(2, repository.RecordCalls);
        Assert.False(viewModel.RecordObservationCommand.CanExecute(null));
    }

    [Fact]
    public void ReplacingCapture_CancelsAndIgnoresLateRecordResult()
    {
        var first = CruiseHistoryApplicationTestData.Observation();
        var replacement = CruiseHistoryApplicationTestData.Observation(
            title: "Replacement cruise",
            ship: "Replacement Ship");
        var (viewModel, repository) = CreateViewModel();
        var completion = new TaskCompletionSource<RepositoryResult>();
        repository.RecordHandler = (_, _, _, _) => completion.Task;
        viewModel.SetCapturedObservation(first);
        viewModel.RecordObservationCommand.Execute(null);

        viewModel.SetCapturedObservation(replacement);
        Assert.True(viewModel.RecordObservationCommand.CanExecute(null));
        completion.SetResult(new RepositoryResult(
            RepositoryState.FirstObservationRecorded,
            CruiseHistoryApplicationTestData.History(first)));

        Assert.True(repository.RecordedToken.IsCancellationRequested);
        Assert.Same(replacement, viewModel.CapturedObservation);
        Assert.Null(viewModel.RecordMessage);
        Assert.False(viewModel.IsRecordCompleted);
        Assert.True(viewModel.RecordObservationCommand.CanExecute(null));
        Assert.Empty(viewModel.Histories);
    }

    [Theory]
    [InlineData(1025, "higher")]
    [InlineData(988, "unchanged")]
    public void ChangedRecord_DescribesHigherAndComparableUnchangedPrices(
        decimal currentPrice,
        string expectedMessage)
    {
        var first = CruiseHistoryApplicationTestData.Observation();
        var changed = CruiseHistoryApplicationTestData.Observation(
            currentPrice,
            CruiseHistoryApplicationTestData.FirstObserved.AddDays(1),
            promotion: "Different promotion");
        var history = CruiseHistoryApplicationTestData.History(first, changed);
        var (viewModel, repository) = CreateViewModel();
        repository.RecordResult = new RepositoryResult(
            RepositoryState.ChangedObservationRecorded,
            history);
        repository.ListResult = [history];
        viewModel.SetCapturedObservation(changed);

        viewModel.RecordObservationCommand.Execute(null);

        Assert.Contains(expectedMessage, viewModel.RecordMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SourceLessFirstRecord_UsesNeutralControlledWording()
    {
        var observation = WithoutSource(CruiseHistoryApplicationTestData.Observation());
        var history = CruiseHistoryApplicationTestData.History(observation);
        var (viewModel, repository) = CreateViewModel();
        repository.RecordResult = new RepositoryResult(
            RepositoryState.FirstObservationRecorded,
            history);
        repository.ListResult = [history];
        viewModel.SetCapturedObservation(observation);

        viewModel.RecordObservationCommand.Execute(null);

        Assert.Contains("from this source", viewModel.RecordMessage);
    }

    [Fact]
    public void ChangedRecord_WithUnavailableComparablePriceRemainsHonest()
    {
        var first = CruiseHistoryApplicationTestData.Observation();
        var unavailable = new CruiseObservation(
            new CruiseSnapshot(
                first.Snapshot.Offer,
                [
                    new CruisePrice(900m, "GBP", "per person"),
                    new CruisePrice(950m, "GBP", "per person")
                ],
                "Different promotion"),
            first.ObservedAt.AddDays(1),
            first.SourceReference,
            first.Source);
        var history = CruiseHistoryApplicationTestData.History(first, unavailable);
        var (viewModel, repository) = CreateViewModel();
        repository.RecordResult = new RepositoryResult(
            RepositoryState.ChangedObservationRecorded,
            history);
        repository.ListResult = [history];
        viewModel.SetCapturedObservation(unavailable);

        viewModel.RecordObservationCommand.Execute(null);

        Assert.Contains("unavailable", viewModel.RecordMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lower", viewModel.RecordMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("higher", viewModel.RecordMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Loading_CancellationRejectsStaleResultAndAllowsLaterRefresh()
    {
        var staleObservation = CruiseHistoryApplicationTestData.Observation(title: "Stale history");
        var currentObservation = CruiseHistoryApplicationTestData.Observation(
            title: "Current history",
            ship: "Current Ship");
        var (viewModel, repository) = CreateViewModel();
        var staleCompletion = new TaskCompletionSource<IReadOnlyList<KrytenApplication::KrytenAssist.Application.Cruises.CruiseRecordedHistory>>();
        repository.ListHandler = _ => repository.ListCalls == 1
            ? staleCompletion.Task
            : Task.FromResult<IReadOnlyList<KrytenApplication::KrytenAssist.Application.Cruises.CruiseRecordedHistory>>(
                [CruiseHistoryApplicationTestData.History(currentObservation)]);

        viewModel.Activate();

        Assert.True(viewModel.IsLoadingHistory);
        Assert.True(viewModel.CancelHistoryLoadingCommand.CanExecute(null));
        viewModel.Deactivate();
        Assert.True(repository.ListToken.IsCancellationRequested);
        Assert.False(viewModel.IsLoadingHistory);
        viewModel.RefreshHistoryCommand.Execute(null);
        Assert.Equal("Current history", Assert.Single(viewModel.Histories).Title);

        staleCompletion.SetResult([CruiseHistoryApplicationTestData.History(staleObservation)]);

        Assert.Equal("Current history", Assert.Single(viewModel.Histories).Title);
    }

    [Fact]
    public void FailedRefresh_RetainsLastGoodListThenSuccessfulRetryUsesSafeFallbackSelection()
    {
        var first = CruiseHistoryApplicationTestData.Observation(title: "First history");
        var replacement = CruiseHistoryApplicationTestData.Observation(
            title: "Replacement history",
            ship: "Replacement Ship");
        var (viewModel, repository) = CreateViewModel();
        repository.ListHandler = _ => repository.ListCalls switch
        {
            1 => Task.FromResult<IReadOnlyList<KrytenApplication::KrytenAssist.Application.Cruises.CruiseRecordedHistory>>(
                [CruiseHistoryApplicationTestData.History(first)]),
            2 => Task.FromException<IReadOnlyList<KrytenApplication::KrytenAssist.Application.Cruises.CruiseRecordedHistory>>(
                new InvalidOperationException("private database detail")),
            _ => Task.FromResult<IReadOnlyList<KrytenApplication::KrytenAssist.Application.Cruises.CruiseRecordedHistory>>(
                [CruiseHistoryApplicationTestData.History(replacement)])
        };
        viewModel.Activate();
        var originalSelection = viewModel.SelectedHistory;

        viewModel.RefreshHistoryCommand.Execute(null);

        Assert.Same(originalSelection, viewModel.SelectedHistory);
        Assert.Equal("First history", Assert.Single(viewModel.Histories).Title);
        Assert.True(viewModel.HasHistoryError);
        Assert.DoesNotContain("database", viewModel.HistoryErrorMessage);

        viewModel.RefreshHistoryCommand.Execute(null);

        Assert.False(viewModel.HasHistoryError);
        Assert.Equal("Replacement history", Assert.Single(viewModel.Histories).Title);
        Assert.Equal("Replacement history", viewModel.SelectedHistory!.Title);
    }

    [Fact]
    public void Refresh_RetainsSameSailingSelectionByRetailSource()
    {
        var tui = CruiseHistoryApplicationTestData.Observation(source: new CruiseSource("tui", "TUI"));
        var agent = CruiseHistoryApplicationTestData.Observation(source: new CruiseSource("agent", "Travel Agent"));
        var (viewModel, repository) = CreateViewModel();
        repository.ListResult =
        [
            CruiseHistoryApplicationTestData.History(tui),
            CruiseHistoryApplicationTestData.History(agent)
        ];
        viewModel.Activate();
        viewModel.SelectedHistory = viewModel.Histories.Single(item => item.RetailSource == "Travel Agent");

        viewModel.RefreshHistoryCommand.Execute(null);

        Assert.Equal(2, viewModel.Histories.Count);
        Assert.Equal("Travel Agent", viewModel.SelectedHistory!.RetailSource);
    }

    [Fact]
    public void Item_FormatsAllTrendDurationStatusAndMissingEvidenceStatesHonestly()
    {
        var departureDay = new DateOnly(2026, 7, 17);
        var first = ObservationWithPrices(
            [new CruisePrice(1000m, "EUR", "total")],
            departureDay,
            1,
            CruiseHistoryApplicationTestData.FirstObserved,
            sourceReference: null,
            source: null);
        var higher = ObservationWithPrices(
            [new CruisePrice(1100m, "EUR", "total")],
            departureDay,
            1,
            CruiseHistoryApplicationTestData.FirstObserved.AddDays(1),
            sourceReference: null,
            source: null);
        var higherItem = CreateItem(CruiseHistoryApplicationTestData.History(first, higher));
        var firstItem = CreateItem(CruiseHistoryApplicationTestData.History(first));
        var unchanged = ObservationWithPrices(
            [new CruisePrice(1000m, "EUR", "total")],
            departureDay,
            1,
            CruiseHistoryApplicationTestData.FirstObserved.AddDays(1),
            sourceReference: null,
            source: null);
        var unchangedItem = CreateItem(CruiseHistoryApplicationTestData.History(first, unchanged));
        var unavailable = ObservationWithPrices(
            [
                new CruisePrice(900m, "GBP", "per person"),
                new CruisePrice(950m, "GBP", "per person")
            ],
            departureDay,
            7,
            CruiseHistoryApplicationTestData.FirstObserved,
            sourceReference: null,
            source: null);
        var unavailableItem = CreateItem(CruiseHistoryApplicationTestData.History(unavailable));

        Assert.Equal("EUR 1,100 total", higherItem.CurrentPrice);
        Assert.Equal("Up EUR 100", higherItem.Trend);
        Assert.Equal("📈", higherItem.TrendIndicator);
        Assert.Equal("1 night", higherItem.Duration);
        Assert.Equal("Upcoming sailing", higherItem.SailingStatus);
        Assert.Equal("🗓️", higherItem.SailingStatusIndicator);
        Assert.Equal("First observation", firstItem.Trend);
        Assert.Equal("🆕", firstItem.TrendIndicator);
        Assert.Equal("Unchanged", unchangedItem.Trend);
        Assert.Equal("➖", unchangedItem.TrendIndicator);
        Assert.Equal("Source unavailable", firstItem.RetailSource);
        Assert.False(firstItem.HasLatestSourceReference);
        Assert.Equal("Comparable price unavailable", unavailableItem.CurrentPrice);
        Assert.Equal("Comparable price unavailable", unavailableItem.Trend);
        Assert.Equal("❔", unavailableItem.TrendIndicator);
        Assert.DoesNotContain("discount", higherItem.CurrentPrice, StringComparison.OrdinalIgnoreCase);
    }

    private static (CruiseHistoryViewModel ViewModel, FakeCruiseObservationRepository Repository) CreateViewModel()
    {
        var repository = new FakeCruiseObservationRepository();
        var analyzer = new CruisePriceHistoryAnalyzer();
        return (
            new CruiseHistoryViewModel(
                new RecordObservation(repository, analyzer),
                new GetHistory(repository, analyzer),
                new ListHistories(repository, analyzer),
                new FixedClock()),
            repository);
    }

    private static async Task WaitUntilAsync(
        CruiseHistoryViewModel viewModel,
        Func<bool> condition)
    {
        if (condition())
        {
            return;
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        PropertyChangedEventHandler? handler = null;
        handler = (_, _) =>
        {
            if (condition())
            {
                viewModel.PropertyChanged -= handler;
                completion.TrySetResult();
            }
        };
        viewModel.PropertyChanged += handler;
        await completion.Task;
    }

    private static async Task WaitUntilCanExecuteAsync(ICommand command)
    {
        if (command.CanExecute(null))
        {
            return;
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            if (command.CanExecute(null))
            {
                command.CanExecuteChanged -= handler;
                completion.TrySetResult();
            }
        };
        command.CanExecuteChanged += handler;
        await completion.Task;
    }

    private static CruiseObservation WithoutSource(CruiseObservation observation) =>
        new(observation.Snapshot, observation.ObservedAt, observation.SourceReference, source: null);

    private static CruiseObservation ObservationWithPrices(
        IReadOnlyList<CruisePrice> prices,
        DateOnly departure,
        int duration,
        DateTimeOffset observedAt,
        string? sourceReference,
        CruiseSource? source) =>
        new(
            new CruiseSnapshot(
                new CruiseOffer(
                    new CruiseProvider("marella", "Marella Cruises"),
                    "formatting-offer",
                    "Formatting cruise",
                    "Formatting Ship",
                    departure,
                    duration,
                    "Tenerife",
                    "Canarian itinerary"),
                prices,
                promotionSummary: null),
            observedAt,
            sourceReference,
            source);

    private static CruiseHistoryItemViewModel CreateItem(
        KrytenApplication::KrytenAssist.Application.Cruises.CruiseRecordedHistory history)
    {
        var details = new KrytenApplication::KrytenAssist.Application.Cruises.CruiseHistoryDetails(
            history,
            history.Analyze(new CruisePriceHistoryAnalyzer()));
        return new CruiseHistoryItemViewModel(details, new FixedClock());
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset Now => new(2026, 7, 17, 10, 0, 0, TimeSpan.FromHours(1));
    }
}
