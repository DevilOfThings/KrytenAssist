using KrytenAssist.Avalonia.Skills.Models;
using KrytenAssist.Avalonia.Skills.Services;
using KrytenAssist.Avalonia.Tools;
using KrytenAssist.Avalonia.ViewModels;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Avalonia.Tests.ViewModels;

public sealed class CruiseOfTheWeekViewModelTests
{
    private static readonly DateTimeOffset RequestedAt =
        new(2026, 7, 14, 10, 30, 0, TimeSpan.FromHours(1));

    [Fact]
    public void Constructor_RejectsNullDependencies()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CruiseOfTheWeekViewModel(null!, new TestClock()));
        Assert.Throws<ArgumentNullException>(() =>
            new CruiseOfTheWeekViewModel(new SkillRegistry(), null!));
    }

    [Fact]
    public void Constructor_PerformsNoExecutionOrClockRead()
    {
        var skill = new TestCruiseSkill();
        var clock = new TestClock();

        var viewModel = CreateViewModel(skill, clock);

        Assert.Equal(0, skill.ExecutionCount);
        Assert.Equal(0, clock.ReadCount);
        Assert.False(viewModel.HasObservation);
        Assert.False(viewModel.IsBusy);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task RetrieveAsync_UsesExpectedRequestContextAndMapsObservation()
    {
        var observation = CreateObservation();
        var skill = new TestCruiseSkill
        {
            Handler = (_, _, _) => Task.FromResult(SkillResult.Success(observation))
        };
        var clock = new TestClock { NowValue = RequestedAt };
        var viewModel = CreateViewModel(skill, clock);

        await viewModel.RetrieveAsync();

        Assert.Equal(1, skill.ExecutionCount);
        Assert.Equal("get-current", skill.LastRequest?.Operation);
        Assert.Empty(skill.LastRequest!.Parameters);
        Assert.Equal(RequestedAt, skill.LastContext?.RequestedAt);
        Assert.Equal(1, clock.ReadCount);
        Assert.Same(observation, viewModel.Observation);
        Assert.True(viewModel.HasObservation);
        Assert.False(viewModel.IsBusy);
        Assert.False(viewModel.HasError);
        Assert.Equal("Refresh", viewModel.RetrieveButtonText);
        Assert.Equal("Mediterranean Medley", viewModel.CruiseTitle);
        Assert.Equal("Marella Explorer", viewModel.ShipName);
        Assert.Equal("27 October 2026", viewModel.DepartureDateText);
        Assert.Equal("Palma", viewModel.DeparturePort);
        Assert.Equal("7 nights", viewModel.DurationText);
        Assert.Equal("£903", viewModel.PriceText);
        Assert.Equal("per person", viewModel.PriceBasis);
        Assert.Equal("Marella Cruises", viewModel.ProviderName);
        Assert.Equal("Save £300", viewModel.PromotionSummary);
        Assert.Equal("https://example.test/cruise", viewModel.SourceReference);
        Assert.Contains(
            "Cruise of the Week is Mediterranean Medley on Marella Explorer",
            viewModel.Summary);
        Assert.Contains("departing Palma on 27 October 2026", viewModel.Summary);
        Assert.EndsWith("from £903 per person.", viewModel.Summary);
    }

    [Fact]
    public async Task RetrieveAsync_MissingSkillProducesControlledUnavailableState()
    {
        var clock = new TestClock();
        var viewModel = new CruiseOfTheWeekViewModel(new SkillRegistry(), clock);

        await viewModel.RetrieveAsync();

        Assert.True(viewModel.HasError);
        Assert.Equal("Cruise of the Week is currently unavailable.", viewModel.ErrorMessage);
        Assert.Equal(0, clock.ReadCount);
    }

    [Fact]
    public async Task RetrieveAsync_FailedResultDisplaysSkillMessageAndCanRetry()
    {
        var attempts = 0;
        var observation = CreateObservation();
        var skill = new TestCruiseSkill
        {
            Handler = (_, _, _) => Task.FromResult(
                ++attempts == 1
                    ? SkillResult.Failure("Marella is temporarily unavailable.")
                    : SkillResult.Success(observation))
        };
        var viewModel = CreateViewModel(skill, new TestClock());

        await viewModel.RetrieveAsync();

        Assert.True(viewModel.HasError);
        Assert.Equal("Marella is temporarily unavailable.", viewModel.ErrorMessage);
        Assert.False(viewModel.HasObservation);

        await viewModel.RetrieveAsync();

        Assert.False(viewModel.HasError);
        Assert.Same(observation, viewModel.Observation);
        Assert.Equal(2, skill.ExecutionCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong data")]
    public async Task RetrieveAsync_InvalidSuccessfulDataProducesControlledError(object? data)
    {
        var skill = new TestCruiseSkill
        {
            Handler = (_, _, _) => Task.FromResult(SkillResult.Success(data))
        };
        var viewModel = CreateViewModel(skill, new TestClock());

        await viewModel.RetrieveAsync();

        Assert.True(viewModel.HasError);
        Assert.Equal(
            "Cruise of the Week could not be retrieved. Please try again.",
            viewModel.ErrorMessage);
        Assert.False(viewModel.HasObservation);
    }

    [Fact]
    public async Task RetrieveAsync_UnexpectedExceptionProducesControlledError()
    {
        var skill = new TestCruiseSkill
        {
            Handler = (_, _, _) => throw new InvalidOperationException("Provider detail")
        };
        var viewModel = CreateViewModel(skill, new TestClock());

        await viewModel.RetrieveAsync();

        Assert.True(viewModel.HasError);
        Assert.DoesNotContain("Provider detail", viewModel.ErrorMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task RetrieveAsync_IgnoresReentryAndCancellationIsNotAnError()
    {
        var completion = new TaskCompletionSource<SkillResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var skill = new TestCruiseSkill
        {
            Handler = (_, _, token) =>
            {
                token.Register(() => completion.TrySetCanceled(token));
                return completion.Task;
            }
        };
        var viewModel = CreateViewModel(skill, new TestClock());

        var retrieval = viewModel.RetrieveAsync();
        var reentry = viewModel.RetrieveAsync();

        Assert.True(viewModel.IsBusy);
        Assert.False(viewModel.CanRetrieve);
        Assert.Equal(1, skill.ExecutionCount);
        Assert.False(viewModel.RetrieveCommand.CanExecute(null));
        Assert.True(viewModel.CancelCommand.CanExecute(null));

        viewModel.CancelCommand.Execute(null);
        await Task.WhenAll(retrieval, reentry);

        Assert.False(viewModel.IsBusy);
        Assert.False(viewModel.HasError);
        Assert.True(skill.LastCancellationToken.IsCancellationRequested);
        Assert.True(viewModel.RetrieveCommand.CanExecute(null));
    }

    [Fact]
    public async Task RefreshAndCancellation_PreservePreviousObservation()
    {
        var original = CreateObservation();
        var refresh = new TaskCompletionSource<SkillResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = 0;
        var skill = new TestCruiseSkill
        {
            Handler = (_, _, token) =>
            {
                attempts++;
                if (attempts == 1)
                {
                    return Task.FromResult(SkillResult.Success(original));
                }

                token.Register(() => refresh.TrySetCanceled(token));
                return refresh.Task;
            }
        };
        var viewModel = CreateViewModel(skill, new TestClock());
        await viewModel.RetrieveAsync();

        var retrieval = viewModel.RetrieveAsync();

        Assert.True(viewModel.IsBusy);
        Assert.Same(original, viewModel.Observation);

        viewModel.CancelCommand.Execute(null);
        await retrieval;

        Assert.Same(original, viewModel.Observation);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task Summary_HandlesMissingOptionalsOneNightAndNonGbpCurrency()
    {
        var observation = CreateObservation(
            departurePort: null,
            durationNights: 1,
            currency: "EUR",
            price: 500m,
            basis: null,
            promotion: null,
            source: null);
        var skill = new TestCruiseSkill
        {
            Handler = (_, _, _) => Task.FromResult(SkillResult.Success(observation))
        };
        var viewModel = CreateViewModel(skill, new TestClock());

        await viewModel.RetrieveAsync();

        Assert.Contains("departing on 27 October 2026", viewModel.Summary);
        Assert.Contains("for 1 night from EUR 500.", viewModel.Summary);
        Assert.DoesNotContain("£", viewModel.Summary);
        Assert.False(viewModel.HasDeparturePort);
        Assert.False(viewModel.HasPriceBasis);
        Assert.False(viewModel.HasPromotionSummary);
        Assert.False(viewModel.HasSourceReference);
    }

    [Fact]
    public async Task RetrieveAsync_RaisesNotificationsForPublicState()
    {
        var skill = new TestCruiseSkill
        {
            Handler = (_, _, _) => Task.FromResult(SkillResult.Success(CreateObservation()))
        };
        var viewModel = CreateViewModel(skill, new TestClock());
        var notifications = new List<string?>();
        viewModel.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        await viewModel.RetrieveAsync();

        Assert.Contains(nameof(CruiseOfTheWeekViewModel.IsBusy), notifications);
        Assert.Contains(nameof(CruiseOfTheWeekViewModel.CanRetrieve), notifications);
        Assert.Contains(nameof(CruiseOfTheWeekViewModel.Observation), notifications);
        Assert.Contains(nameof(CruiseOfTheWeekViewModel.HasObservation), notifications);
        Assert.Contains(nameof(CruiseOfTheWeekViewModel.Summary), notifications);
        Assert.Contains(nameof(CruiseOfTheWeekViewModel.RetrieveButtonText), notifications);
    }

    private static CruiseOfTheWeekViewModel CreateViewModel(
        TestCruiseSkill skill,
        TestClock clock)
    {
        var registry = new SkillRegistry();
        registry.Register(skill);
        return new CruiseOfTheWeekViewModel(registry, clock);
    }

    private static CruiseObservation CreateObservation(
        string? departurePort = "Palma",
        int durationNights = 7,
        string currency = "GBP",
        decimal price = 903m,
        string? basis = "per person",
        string? promotion = "Save £300",
        string? source = "https://example.test/cruise")
    {
        var offer = new CruiseOffer(
            new CruiseProvider("marella", "Marella Cruises"),
            "mediterranean-medley",
            "Mediterranean Medley",
            "Marella Explorer",
            new DateOnly(2026, 10, 27),
            durationNights,
            departurePort);
        var snapshot = new CruiseSnapshot(
            offer,
            [new CruisePrice(price, currency, basis)],
            promotion);

        return new CruiseObservation(snapshot, RequestedAt, source);
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset NowValue { get; init; } = RequestedAt;

        public int ReadCount { get; private set; }

        public DateTimeOffset Now
        {
            get
            {
                ReadCount++;
                return NowValue;
            }
        }
    }

    private sealed class TestCruiseSkill : ISkill
    {
        public SkillManifest Manifest { get; } = new(
            "cruise.of-the-week",
            "Cruise of the Week",
            "Test Cruise Skill",
            "1.0.0");

        public Func<SkillRequest, SkillContext, CancellationToken, Task<SkillResult>> Handler
            { get; init; } = (_, _, _) =>
                Task.FromResult(SkillResult.Failure("Not configured"));

        public int ExecutionCount { get; private set; }

        public SkillRequest? LastRequest { get; private set; }

        public SkillContext? LastContext { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public Task<SkillResult> ExecuteAsync(
            SkillRequest request,
            SkillContext context,
            CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            LastRequest = request;
            LastContext = context;
            LastCancellationToken = cancellationToken;
            return Handler(request, context, cancellationToken);
        }
    }
}
