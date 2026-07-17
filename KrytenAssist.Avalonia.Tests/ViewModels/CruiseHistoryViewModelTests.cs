extern alias KrytenApplication;

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

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset Now => new(2026, 7, 17, 10, 0, 0, TimeSpan.FromHours(1));
    }
}
