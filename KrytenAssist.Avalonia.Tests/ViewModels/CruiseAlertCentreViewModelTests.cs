extern alias KrytenApplication;

using KrytenAssist.Avalonia.ViewModels;
using KrytenAssist.Core.Cruises;
using AlertRepository = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseAlertRepository;
using SettingsRepository = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseAlertSettingsRepository;
using ListAlerts = KrytenApplication::KrytenAssist.Application.Cruises.ListCruiseAlerts;
using ChangeStatus = KrytenApplication::KrytenAssist.Application.Cruises.ChangeCruiseAlertStatus;
using CountUnread = KrytenApplication::KrytenAssist.Application.Cruises.CountUnreadCruiseAlerts;
using GetSettings = KrytenApplication::KrytenAssist.Application.Cruises.GetCruiseAlertSettings;
using SaveSettings = KrytenApplication::KrytenAssist.Application.Cruises.SaveCruiseAlertSettings;
using AlertQuery = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertQuery;
using AddResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseAlertAddRepositoryResult;

namespace KrytenAssist.Avalonia.Tests.ViewModels;

public sealed class CruiseAlertCentreViewModelTests
{
    [Fact]
    public void ItemProjection_UsesTypedEvidenceWithoutOpaqueKeys()
    {
        var alert = PriceDrop(CruiseAlertStatus.Unread);

        var item = new CruiseAlertItemViewModel(alert);

        Assert.Contains("£100", item.Summary);
        Assert.Contains("£90", item.Summary);
        Assert.Contains("10%", item.Summary);
        Assert.Contains("Reduction: £10", item.DetailText);
        Assert.DoesNotContain(alert.EventKey, item.DetailText);
        Assert.DoesNotContain("evidence-key", item.DetailText);
    }

    [Fact]
    public async Task Activate_LoadsSnapshotAndDefaultActiveFilter()
    {
        var repository = new InMemoryRepository([PriceDrop(CruiseAlertStatus.Unread), Promotion(CruiseAlertStatus.Dismissed)]);
        var viewModel = Create(repository);

        await viewModel.ActivateAsync();

        Assert.Single(viewModel.Items);
        Assert.Equal(CruiseAlertType.PriceDrop, viewModel.SelectedItem!.Type);
        Assert.Equal(1, viewModel.Items[0].Status == CruiseAlertStatus.Unread ? repository.UnreadCount : 0);
        Assert.Equal(1, repository.ListCalls);
        Assert.Equal(1, repository.CountCalls);
    }

    [Fact]
    public async Task Filters_ComposeAndDismissedRemainsRecoverable()
    {
        var repository = new InMemoryRepository([PriceDrop(CruiseAlertStatus.Unread), Promotion(CruiseAlertStatus.Dismissed)]);
        var viewModel = Create(repository);
        await viewModel.ActivateAsync();

        viewModel.IsDismissedLifecycle = true;
        viewModel.IsPromotions = true;

        Assert.Single(viewModel.Items);
        Assert.True(viewModel.SelectedItem!.IsDismissed);
        await ExecuteAsync(viewModel.RestoreCommand);
        Assert.Equal(CruiseAlertStatus.Unread, repository.Alerts.Single(a => a.Type == CruiseAlertType.Promotion).Status);
        Assert.Equal(2, repository.CountCalls);
    }

    [Fact]
    public async Task Selection_DoesNotChangeLifecycle()
    {
        var alert = PriceDrop(CruiseAlertStatus.Unread);
        var repository = new InMemoryRepository([alert]);
        var viewModel = Create(repository);
        await viewModel.ActivateAsync();

        viewModel.SelectedItem = viewModel.Items[0];

        Assert.Equal(CruiseAlertStatus.Unread, repository.Alerts[0].Status);
        Assert.Equal(0, repository.UpdateCalls);
    }

    [Fact]
    public async Task Settings_RejectInvalidPercentageAndPreservesDirtyDraftAfterFailure()
    {
        var repository = new InMemoryRepository([]) { FailSettingsSave = true };
        var settings = new CruiseAlertSettingsViewModel(new GetSettings(repository), new SaveSettings(repository));
        await settings.ActivateAsync();
        settings.MinimumPriceDropPercentage = "101";

        await settings.SaveAsync();

        Assert.True(settings.HasValidationError);
        Assert.Equal(0, repository.SettingsSaveCalls);
        settings.MinimumPriceDropPercentage = "12.5";
        await settings.SaveAsync();
        Assert.True(settings.HasUnsavedChanges);
        Assert.True(settings.HasError);
        Assert.Equal(1, repository.SettingsSaveCalls);
    }

    [Fact]
    public async Task NewItineraryFilterAndSettings_UseTypedRouteEvidenceAndPreserveAllFlags()
    {
        var newAlert = NewItinerary();
        var repository = new InMemoryRepository([PriceDrop(CruiseAlertStatus.Unread), newAlert]);
        var viewModel = Create(repository);
        await viewModel.ActivateAsync();

        viewModel.IsNewItineraries = true;

        Assert.Single(viewModel.Items);
        Assert.Equal(CruiseAlertType.NewItinerary, viewModel.SelectedItem!.Type);
        Assert.Contains("First observed by Kryten", viewModel.SelectedItem.DetailText);
        Assert.DoesNotContain("currently available", viewModel.SelectedItem.Summary, StringComparison.OrdinalIgnoreCase);

        await viewModel.Settings.ActivateAsync();
        viewModel.Settings.CabinAvailabilityEnabled = false;
        viewModel.Settings.NewItineraryEnabled = false;
        await viewModel.Settings.SaveAsync();
        Assert.False(repository.Settings.CabinAvailabilityEnabled);
        Assert.False(repository.Settings.NewItineraryEnabled);
        Assert.True(repository.Settings.PriceDropEnabled);
    }

    private static CruiseAlertCentreViewModel Create(InMemoryRepository repository)
    {
        var coordinator = new CruiseAlertCoordinator(new CountUnread(repository));
        var settings = new CruiseAlertSettingsViewModel(new GetSettings(repository), new SaveSettings(repository));
        return new CruiseAlertCentreViewModel(new ListAlerts(repository), new ChangeStatus(repository), coordinator, settings);
    }

    private static async Task ExecuteAsync(System.Windows.Input.ICommand command)
    {
        command.Execute(null);
        for (var i = 0; i < 100 && !command.CanExecute(null); i++) await Task.Delay(1);
    }

    private static CruiseAlert PriceDrop(CruiseAlertStatus status) => Create(
        CruiseAlertType.PriceDrop,
        new CruisePriceDropAlertDetails(new CruisePrice(100, "GBP", "per person"), new CruisePrice(90, "GBP", "per person"), "evidence-key"),
        status,
        new CruiseSource("tui", "TUI"));

    private static CruiseAlert Promotion(CruiseAlertStatus status) => Create(
        CruiseAlertType.Promotion,
        new CruisePromotionAlertDetails(null, "£100 off per person", "promotion-evidence"),
        status,
        new CruiseSource("tui", "TUI"));

    private static CruiseAlert NewItinerary()
    {
        var time = new DateTimeOffset(2026, 7, 20, 10, 0, 0, TimeSpan.Zero);
        var source = new CruiseSource("tui", "TUI");
        var occurrence = new CruiseItineraryOccurrence(new("marella", "ATL-01"), source, time, "provider-evidence", "Atlantic Islands");
        var observed = new CruiseItineraryFirstObservedEvent(occurrence, new string('a', 64), new string('b', 64));
        var candidate = new CruiseNewItineraryAlertDetector().Detect([observed], new()).Single();
        return new CruiseAlert(Guid.NewGuid(), candidate, time.AddMinutes(1));
    }

    private static CruiseAlert Create(CruiseAlertType type, CruiseAlertDetails details, CruiseAlertStatus status, CruiseSource source)
    {
        var key = new CruiseSailingKey("marella", type == CruiseAlertType.PriceDrop ? "Explorer" : "Discovery", new DateOnly(2027, 2, 3), 7);
        var candidate = new CruiseAlertCandidate(type, key, source, details, new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero), details switch
        {
            CruisePriceDropAlertDetails x => x.EvidenceKey,
            CruisePromotionAlertDetails x => x.EvidenceKey,
            _ => throw new InvalidOperationException()
        });
        return new CruiseAlert(Guid.NewGuid(), candidate, new DateTimeOffset(2026, 7, 18, 10, 1, 0, TimeSpan.Zero), status);
    }

    private sealed class InMemoryRepository(IEnumerable<CruiseAlert> alerts) : AlertRepository, SettingsRepository
    {
        public List<CruiseAlert> Alerts { get; } = [.. alerts];
        public int ListCalls { get; private set; }
        public int CountCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public int SettingsSaveCalls { get; private set; }
        public bool FailSettingsSave { get; set; }
        public int UnreadCount => Alerts.Count(a => a.Status == CruiseAlertStatus.Unread);
        private CruiseAlertSettings _settings = new();
        public CruiseAlertSettings Settings => _settings;

        public Task<CruiseAlert?> GetAsync(Guid id, CancellationToken token = default) => Task.FromResult(Alerts.FirstOrDefault(a => a.Id == id));
        public Task<IReadOnlyList<CruiseAlert>> ListAsync(AlertQuery query, CancellationToken token = default) { ListCalls++; return Task.FromResult<IReadOnlyList<CruiseAlert>>(Alerts.ToArray()); }
        public Task<int> CountUnreadAsync(CancellationToken token = default) { CountCalls++; return Task.FromResult(UnreadCount); }
        public Task<AddResult> AddIfAbsentAsync(CruiseAlert alert, CancellationToken token = default) => throw new NotSupportedException();
        public Task<bool> UpdateStatusAsync(Guid id, CruiseAlertStatus status, CancellationToken token = default)
        {
            UpdateCalls++;
            var index = Alerts.FindIndex(a => a.Id == id);
            if (index < 0) return Task.FromResult(false);
            Alerts[index] = Alerts[index].WithStatus(status);
            return Task.FromResult(true);
        }
        Task<CruiseAlertSettings> SettingsRepository.GetAsync(CancellationToken token) => Task.FromResult(_settings);
        public Task SaveAsync(CruiseAlertSettings settings, CancellationToken token = default)
        {
            SettingsSaveCalls++;
            if (FailSettingsSave) throw new InvalidOperationException("test");
            _settings = settings;
            return Task.CompletedTask;
        }
    }
}
