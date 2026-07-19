extern alias KrytenApplication;
extern alias KrytenInfrastructure;

using KrytenAssist.Core.Cruises;
using CruiseCaptureStatus =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureStatus;
using CruisePageCaptureRequest =
    KrytenApplication::KrytenAssist.Application.Cruises.CruisePageCaptureRequest;
using TuiCruisePageCaptureService =
    KrytenInfrastructure::KrytenAssist.Infrastructure.Cruises.Tui.TuiCruisePageCaptureService;

namespace KrytenAssist.Avalonia.Tests.Cruises.Tui;

public sealed class TuiCruisePageCaptureServiceTests
{
    private static readonly DateTimeOffset ObservedAt =
        new(2026, 7, 16, 15, 45, 0, TimeSpan.FromHours(1));

    private readonly TuiCruisePageCaptureService _service = new();

    [Fact]
    public async Task CaptureAsync_MapsCompleteFictionalPayload()
    {
        var source = new CruiseSource("tui", "TUI");
        var request = CreateRequest(LoadCompletePayload(), source: source);

        var result = await _service.CaptureAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(CruiseCaptureStatus.Success, result.Status);
        var observation = Assert.IsType<CruiseObservation>(result.Observation);
        Assert.Same(source, observation.Source);
        Assert.Equal(ObservedAt, observation.ObservedAt);
        Assert.Equal(request.SourceReference, observation.SourceReference);
        Assert.Equal("marella", observation.Snapshot.Offer.Provider.Id);
        Assert.Equal("Marella Cruises", observation.Snapshot.Offer.Provider.Name);
        Assert.Equal("fictional-offer-001", observation.Snapshot.Offer.ProviderOfferId);
        Assert.Equal("Island Discovery", observation.Snapshot.Offer.Title);
        Assert.Equal("Marella Example", observation.Snapshot.Offer.ShipName);
        Assert.Equal(new DateOnly(2027, 1, 15), observation.Snapshot.Offer.DepartureDate);
        Assert.Equal(7, observation.Snapshot.Offer.DurationNights);
        Assert.Equal("Santa Cruz", observation.Snapshot.Offer.DeparturePort);
        Assert.Equal(
            "Santa Cruz, Madeira and Gran Canaria",
            observation.Snapshot.Offer.ItinerarySummary);
        Assert.Equal("Fictional test promotion", observation.Snapshot.PromotionSummary);
        Assert.Collection(
            observation.Snapshot.Prices,
            price =>
            {
                Assert.Equal(999m, price.Amount);
                Assert.Equal("GBP", price.Currency);
                Assert.Equal("per person", price.Basis);
            },
            price =>
            {
                Assert.Equal(1998m, price.Amount);
                Assert.Equal("total based on 2 sharing", price.Basis);
            });
    }

    [Theory]
    [InlineData("other-source", "tui", "TUI", "https://www.tui.co.uk/cruise/example")]
    [InlineData("marella-cruise-of-the-week", "iglu", "Iglu", "https://www.tui.co.uk/cruise/example")]
    [InlineData("marella-cruise-of-the-week", "tui", "Not TUI", "https://www.tui.co.uk/cruise/example")]
    [InlineData("marella-cruise-of-the-week", "tui", "TUI", "https://www.tui.co.uk.evil.example/cruise")]
    [InlineData("marella-cruise-of-the-week", "tui", "TUI", "https://example.com/cruise")]
    public async Task CaptureAsync_RejectsUnsupportedSourceRetailerOrHost(
        string sourceIdentifier,
        string retailerId,
        string retailerName,
        string sourceReference)
    {
        var request = CreateRequest(
            LoadCompletePayload(),
            sourceIdentifier,
            new CruiseSource(retailerId, retailerName),
            sourceReference);

        var result = await _service.CaptureAsync(request);

        Assert.Equal(CruiseCaptureStatus.Unsupported, result.Status);
        Assert.Null(result.Observation);
        Assert.DoesNotContain("evil", result.Message!, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"version\":0,\"candidates\":[]}")]
    [InlineData("{\"version\":4,\"candidates\":[]}")]
    public async Task CaptureAsync_RejectsMissingOrUnsupportedPayloadVersion(string payload)
    {
        var result = await _service.CaptureAsync(CreateRequest(payload));

        Assert.Equal(CruiseCaptureStatus.Unsupported, result.Status);
    }

    [Fact]
    public async Task CaptureAsync_ReturnsSafeFailureForMalformedJson()
    {
        var result = await _service.CaptureAsync(CreateRequest("{not-json"));

        Assert.Equal(CruiseCaptureStatus.Failed, result.Status);
        Assert.DoesNotContain("JSON", result.Message!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("{not-json", result.Message!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CaptureAsync_ReturnsIncompleteWhenNoCandidateExists()
    {
        var result = await _service.CaptureAsync(CreateRequest(
            "{\"version\":1,\"candidates\":[]}"));

        Assert.Equal(CruiseCaptureStatus.Incomplete, result.Status);
        Assert.Equal(["candidates"], result.MissingFields);
    }

    [Fact]
    public async Task CaptureAsync_ReturnsAmbiguousRatherThanChoosingFirstCandidate()
    {
        var result = await _service.CaptureAsync(CreateRequest(
            "{\"version\":1,\"candidates\":[{},{}]}"));

        Assert.Equal(CruiseCaptureStatus.Ambiguous, result.Status);
        Assert.Null(result.Observation);
    }

    [Fact]
    public async Task CaptureAsync_HandlesNullCandidateWithoutCrashing()
    {
        var result = await _service.CaptureAsync(CreateRequest(
            "{\"version\":1,\"candidates\":[null]}"));

        Assert.Equal(CruiseCaptureStatus.Failed, result.Status);
        Assert.Null(result.Observation);
    }

    [Theory]
    [InlineData("\"providerOfferId\": \"fictional-offer-001\"", "\"providerOfferId\": \" \"", "providerOfferId")]
    [InlineData("\"title\": \"Island Discovery\"", "\"title\": null", "title")]
    [InlineData("\"shipName\": \"Marella Example\"", "\"shipName\": \"\"", "shipName")]
    [InlineData("\"departureDate\": \"2027-01-15\"", "\"departureDate\": \"15/01/2027\"", "departureDate")]
    [InlineData("\"durationNights\": 7", "\"durationNights\": 0", "durationNights")]
    [InlineData("\"amount\": 999.00", "\"amount\": -1", "prices.amount")]
    [InlineData("\"currency\": \"GBP\"", "\"currency\": \"GB\"", "prices.currency")]
    public async Task CaptureAsync_ReportsStableMissingFieldForInvalidRequiredValue(
        string existing,
        string replacement,
        string expectedField)
    {
        var payload = LoadCompletePayload().Replace(existing, replacement, StringComparison.Ordinal);

        var result = await _service.CaptureAsync(CreateRequest(payload));

        Assert.Equal(CruiseCaptureStatus.Incomplete, result.Status);
        Assert.Contains(expectedField, result.MissingFields);
        Assert.Equal(result.MissingFields.Count, result.MissingFields.Distinct().Count());
    }

    [Fact]
    public async Task CaptureAsync_ReturnsIncompleteWhenPricesAreMissing()
    {
        const string payload = """
            {"version":1,"candidates":[{
              "providerOfferId":"offer","title":"Title","shipName":"Ship",
              "departureDate":"2027-01-15","durationNights":7
            }]}
            """;

        var result = await _service.CaptureAsync(CreateRequest(payload));

        Assert.Equal(CruiseCaptureStatus.Incomplete, result.Status);
        Assert.Contains("prices", result.MissingFields);
    }

    [Fact]
    public async Task CaptureAsync_MapsBlankOptionalValuesAsNull()
    {
        const string payload = """
            {"version":1,"candidates":[{
              "providerOfferId":"offer","title":"Title","shipName":"Ship",
              "departureDate":"2027-01-15","durationNights":7,
              "departurePort":" ","itinerarySummary":"","promotionSummary":null,
              "prices":[{"amount":0,"currency":"gbp","basis":" "}]
            }]}
            """;

        var result = await _service.CaptureAsync(CreateRequest(payload));

        var observation = Assert.IsType<CruiseObservation>(result.Observation);
        Assert.Null(observation.Snapshot.Offer.DeparturePort);
        Assert.Null(observation.Snapshot.Offer.ItinerarySummary);
        Assert.Null(observation.Snapshot.PromotionSummary);
        Assert.Null(observation.Snapshot.Prices[0].Basis);
        Assert.Equal("GBP", observation.Snapshot.Prices[0].Currency);
    }

    [Fact]
    public async Task CaptureAsync_ReturnsControlledCancelledResultForPreCancelledToken()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var result = await _service.CaptureAsync(
            CreateRequest(LoadCompletePayload()),
            cancellation.Token);

        Assert.Equal(CruiseCaptureStatus.Cancelled, result.Status);
        Assert.Null(result.Observation);
    }

    private static CruisePageCaptureRequest CreateRequest(
        string payload,
        string sourceIdentifier = "marella-cruise-of-the-week",
        CruiseSource? source = null,
        string sourceReference = "https://www.tui.co.uk/cruise/fictional-example") =>
        new(
            sourceIdentifier,
            source ?? new CruiseSource("tui", "TUI"),
            sourceReference,
            ObservedAt,
            payload);

    private static string LoadCompletePayload() => File.ReadAllText(Path.Combine(
        AppContext.BaseDirectory,
        "Fixtures",
        "Cruises",
        "Tui",
        "complete-capture.json"));
}
