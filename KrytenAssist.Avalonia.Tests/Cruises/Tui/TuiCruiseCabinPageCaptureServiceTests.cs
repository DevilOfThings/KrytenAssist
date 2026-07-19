extern alias KrytenApplication;
extern alias KrytenInfrastructure;

using System.Text.Json;
using KrytenAssist.Core.Cruises;
using CruiseCabinPageCaptureRequest = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinPageCaptureRequest;
using CruiseCaptureBatchStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureBatchStatus;
using CruiseCabinCaptureStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinCaptureStatus;
using TuiService = KrytenInfrastructure::KrytenAssist.Infrastructure.Cruises.Tui.TuiCruiseCabinPageCaptureService;

namespace KrytenAssist.Avalonia.Tests.Cruises.Tui;

public sealed class TuiCruiseCabinPageCaptureServiceTests
{
    private static readonly DateTimeOffset ObservedAt = new(2026, 7, 19, 10, 0, 0, TimeSpan.FromHours(1));
    private readonly TuiService _service = new();

    [Fact]
    public async Task CaptureAsync_MapsDemonstratedInsideEvidenceAndExplicitContext()
    {
        var result = await _service.CaptureAsync(Request(Payload(CreateCandidate())));

        Assert.Equal(CruiseCaptureBatchStatus.Completed, result.Status);
        var candidate = Assert.Single(result.Candidates);
        Assert.Equal(CruiseCabinCaptureStatus.Ready, candidate.Status);
        var observation = Assert.IsType<CruiseCabinObservation>(candidate.Observation);
        Assert.Equal(CruiseCabinEvidenceCoverage.Partial, observation.Coverage);
        Assert.Equal(CruiseCabinAvailabilityState.Available, observation.StateFor(CruiseCabinType.Inside));
        Assert.All(Enum.GetValues<CruiseCabinType>().Where(x => x != CruiseCabinType.Inside),
            type => Assert.Equal(CruiseCabinAvailabilityState.Unknown, observation.StateFor(type)));
        Assert.Equal(2, observation.SearchContext.AdultCount);
        Assert.Equal(0, observation.SearchContext.ChildCount);
        Assert.True(observation.SearchContext.ChildAgesKnown);
        Assert.Empty(observation.SearchContext.ChildAges);
        Assert.Equal("stn", observation.SearchContext.DepartureAirportId);
        Assert.Equal(CruiseCabinPackageMode.FlyCruise, observation.SearchContext.PackageMode);
        Assert.Equal(1, observation.SearchContext.CabinQuantity);
        Assert.Equal(new DateOnly(2026, 10, 2), observation.SailingKey.DepartureDate);
        Assert.StartsWith("tui-cabin:v1:", observation.EvidenceKey, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CaptureAsync_MissingCabinEvidenceProducesNoObservation()
    {
        var result = await _service.CaptureAsync(Request(Payload(CreateCandidate() with { CabinEvidence = null })));
        var candidate = Assert.Single(result.Candidates);
        Assert.Equal(CruiseCabinCaptureStatus.Incomplete, candidate.Status);
        Assert.Null(candidate.Observation);
        Assert.Equal(["cabinEvidence"], candidate.MissingFields);
    }

    [Theory]
    [InlineData("Outside Cabin", "Cheapest available")]
    [InlineData("Inside Cabin", "Selected")]
    public async Task CaptureAsync_UnmappedWordingIsControlledUnsupported(string label, string qualifier)
    {
        var value = CreateCandidate() with { CabinEvidence = new(label, 1, qualifier) };
        var result = await _service.CaptureAsync(Request(Payload(value)));
        Assert.Equal(CruiseCabinCaptureStatus.Unsupported, Assert.Single(result.Candidates).Status);
    }

    [Fact]
    public async Task CaptureAsync_RepeatedEvidenceHasStableKeyButDifferentContextHasDifferentSeries()
    {
        var first = Assert.Single((await _service.CaptureAsync(Request(Payload(CreateCandidate())))).Candidates).Observation!;
        var repeated = Assert.Single((await _service.CaptureAsync(Request(Payload(CreateCandidate()), adults: 2))).Candidates).Observation!;
        var other = Assert.Single((await _service.CaptureAsync(Request(Payload(CreateCandidate()), adults: 1))).Candidates).Observation!;
        Assert.Equal(first.EvidenceKey, repeated.EvidenceKey);
        Assert.Equal(first.SeriesKey, repeated.SeriesKey);
        Assert.NotEqual(first.SeriesKey, other.SeriesKey);
    }

    [Fact]
    public async Task CaptureAsync_AcceptsMixedCandidatesAndDeduplicatesReferences()
    {
        var ready = CreateCandidate();
        var unsupported = CreateCandidate("1002") with { CabinEvidence = new("Balcony Cabin", 1, "Cheapest available") };
        var result = await _service.CaptureAsync(Request(Payload(ready, ready, unsupported, truncated: true)));
        Assert.True(result.WasTruncated);
        Assert.Equal(2, result.Candidates.Count);
        Assert.Equal(1, result.ReadyCount);
        Assert.Equal(1, result.UnsupportedCount);
    }

    [Theory]
    [InlineData("{not-json", CruiseCaptureBatchStatus.Failed)]
    [InlineData("{\"version\":1,\"candidates\":[]}", CruiseCaptureBatchStatus.Unsupported)]
    [InlineData("{\"version\":2,\"candidates\":[]}", CruiseCaptureBatchStatus.Incomplete)]
    public async Task CaptureAsync_ReturnsControlledBatchFailures(string payload, CruiseCaptureBatchStatus status)
    {
        var result = await _service.CaptureAsync(Request(payload));
        Assert.Equal(status, result.Status);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public async Task CaptureAsync_RejectsUntrustedPageAndCandidateReferences()
    {
        var page = await _service.CaptureAsync(Request(Payload(CreateCandidate()), page: "https://example.test/cruise/packages"));
        Assert.Equal(CruiseCaptureBatchStatus.Unsupported, page.Status);
        var bad = CreateCandidate() with { SourceReference = "https://example.test/cruise/bookitineraries/x?itineraryCodeOne=1" };
        var candidate = await _service.CaptureAsync(Request(Payload(bad)));
        Assert.Equal(CruiseCaptureBatchStatus.Failed, candidate.Status);
    }

    [Fact]
    public async Task CaptureAsync_HonoursPreCancellation()
    {
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var result = await _service.CaptureAsync(Request(Payload(CreateCandidate())), cancellation.Token);
        Assert.Equal(CruiseCaptureBatchStatus.Cancelled, result.Status);
    }

    private static CruiseCabinPageCaptureRequest Request(string payload, int adults = 2,
        string page = "https://www.tui.co.uk/cruise/packages", string? query = null) =>
        new("marella-cruise-of-the-week", new CruiseSource("tui", "TUI"),
            query is null ? $"{page}?from%5B%5D=STN%3AAirport&when=01-10-2026&noOfAdults={adults}&noOfChildren=0&childrenAge=&room=" : $"{page}?{query}", ObservedAt, payload);

    private static string Payload(Candidate first, Candidate? second = null, Candidate? third = null, bool truncated = false) =>
        JsonSerializer.Serialize(new { version = 2, wasTruncated = truncated, candidates = new[] { first, second, third }.Where(x => x is not null) });

    private static Candidate CreateCandidate(string code = "1001") => new(
        $"https://www.tui.co.uk/cruise/bookitineraries/Iconic-Islands-{code}?itineraryCodeOne={code}&sailingDate=02Oct26&cruiseDuration=7",
        "Iconic Islands", "Marella Voyager", "2026-10-02", 7,
        new("Inside Cabin", 1, "Cheapest available"));

    private sealed record Candidate(string SourceReference, string Title, string ShipName,
        string DepartureDate, int DurationNights, CabinEvidence? CabinEvidence);
    private sealed record CabinEvidence(string RetailerLabel, int Quantity, string Qualifier);
}
