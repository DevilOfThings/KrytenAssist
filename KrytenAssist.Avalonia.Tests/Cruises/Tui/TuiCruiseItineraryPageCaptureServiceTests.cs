extern alias KrytenApplication;
extern alias KrytenInfrastructure;

using System.Text.Json;
using KrytenAssist.Core.Cruises;
using Request = KrytenApplication::KrytenAssist.Application.Cruises.CruiseItineraryPageCaptureRequest;
using CandidateStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseItineraryCaptureCandidateStatus;
using BatchStatus = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureBatchStatus;
using Service = KrytenInfrastructure::KrytenAssist.Infrastructure.Cruises.Tui.TuiCruiseItineraryPageCaptureService;

namespace KrytenAssist.Avalonia.Tests.Cruises.Tui;

public sealed class TuiCruiseItineraryPageCaptureServiceTests
{
    private const string Page = "https://www.tui.co.uk/cruise/packages?from%5B%5D=STN%3AAirport&to%5B%5D=&when=01-10-2026&flexibility=false&flexibleDays=1&duration=1-7&addAStay=0&until=&choiceSearch=true&noOfAdults=2&noOfChildren=0&childrenAge=&searchRequestType=ins&searchType=search&sp=true&room=";
    private static readonly DateTimeOffset ObservedAt = new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);
    private readonly Service _service = new();

    [Fact]
    public async Task Capture_maps_trusted_identity_scope_and_optional_evidence()
    {
        var result = await _service.CaptureAsync(RequestFor(Payload(Candidate("1001"))));

        Assert.Equal(BatchStatus.Completed, result.Status); Assert.False(result.WasTruncated);
        var occurrence = Assert.Single(result.Candidates).Occurrence!;
        Assert.Equal("marella", occurrence.ItineraryKey.OperatorId); Assert.Equal("1001", occurrence.ItineraryKey.ProviderItineraryId);
        Assert.Equal(ObservedAt, occurrence.ObservedAt); Assert.Equal("package-1001", occurrence.ProviderOfferId);
        Assert.Equal(16, result.Scope!.Criteria.Count);
        Assert.Equal(["stn"], result.Scope.Criteria.Single(x => x.Name == "departure-airport").Values);
        Assert.Equal(CruiseDiscoveryCriterionState.Unknown, result.Scope.Criteria.Single(x => x.Name == "destination").State);
        Assert.StartsWith("tui-itinerary:v1:", occurrence.EvidenceKey);
    }

    [Fact]
    public async Task Scope_is_order_stable_ignores_tracking_and_rejects_unknown_material_keys()
    {
        var first = await _service.CaptureAsync(RequestFor(Payload(Candidate("1001"))));
        var reordered = "https://www.tui.co.uk/cruise/packages?utm_source=test&sort=price&noOfAdults=2&from%5B%5D=STN%3AAirport&when=01-10-2026&duration=1-7&flexibility=false&flexibleDays=1&addAStay=0&choiceSearch=true&noOfChildren=0&searchRequestType=ins&searchType=search&sp=true";
        var second = await _service.CaptureAsync(RequestFor(Payload(Candidate("1001")), reordered));
        var unknown = await _service.CaptureAsync(RequestFor(Payload(Candidate("1001")), Page + "&newFilter=value"));

        Assert.Equal(first.Scope!.Fingerprint, second.Scope!.Fingerprint);
        Assert.Equal(BatchStatus.Incomplete, unknown.Status); Assert.Contains("newFilter", unknown.Message);
    }

    [Fact]
    public async Task Duplicate_sailings_for_one_itinerary_produce_one_ready_occurrence()
    {
        var later = Candidate("1001") with { SourceReference = Reference("1001") + "&sailingDate=02Oct26", ProviderOfferId = "later" };
        var result = await _service.CaptureAsync(RequestFor(Payload(Candidate("1001"), later, truncated: true)));

        Assert.True(result.WasTruncated); Assert.Single(result.Candidates, x => x.Status == CandidateStatus.Ready);
        Assert.Single(result.Candidates, x => x.Status == CandidateStatus.Ineligible);
    }

    [Fact]
    public async Task Missing_mismatched_and_untrusted_identity_are_controlled()
    {
        var missing = await _service.CaptureAsync(RequestFor(Payload(Candidate("1001") with { ProviderItineraryId = null })));
        var mismatch = await _service.CaptureAsync(RequestFor(Payload(Candidate("1001") with { ProviderItineraryId = "other" })));
        var untrusted = await _service.CaptureAsync(RequestFor(Payload(Candidate("1001") with { SourceReference = "https://example.test/cruise/bookitineraries/x?itineraryCodeOne=1001" })));

        Assert.Equal(CandidateStatus.Ineligible, Assert.Single(missing.Candidates).Status);
        Assert.Equal(CandidateStatus.Failed, Assert.Single(mismatch.Candidates).Status);
        Assert.Equal(CandidateStatus.Failed, Assert.Single(untrusted.Candidates).Status);
    }

    [Theory]
    [InlineData("{", BatchStatus.Failed)]
    [InlineData("{\"version\":2,\"candidates\":[]}", BatchStatus.Unsupported)]
    [InlineData("{\"version\":3,\"candidates\":[]}", BatchStatus.Incomplete)]
    public async Task Invalid_payload_states_are_controlled(string payload, BatchStatus expected) =>
        Assert.Equal(expected, (await _service.CaptureAsync(RequestFor(payload))).Status);

    [Fact]
    public async Task Cancellation_and_wrong_page_are_controlled()
    {
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.Equal(BatchStatus.Cancelled, (await _service.CaptureAsync(RequestFor(Payload(Candidate("1"))), cancellation.Token)).Status);
        Assert.Equal(BatchStatus.Unsupported, (await _service.CaptureAsync(RequestFor(Payload(Candidate("1")), "https://www.tui.co.uk/cruise/deals"))).Status);
    }

    private static Request RequestFor(string payload, string page = Page) => new("marella-cruise-of-the-week", new("tui", "TUI"), page, ObservedAt, payload);
    private static string Payload(CandidateData first, CandidateData? second = null, bool truncated = false) => JsonSerializer.Serialize(new { version = 3, wasTruncated = truncated, candidates = new[] { first, second }.Where(x => x is not null) });
    private static CandidateData Candidate(string code) => new(Reference(code), code, $"package-{code}", "Island Discovery", "Marella Explorer", "2027-01-02", 7, null, "Mediterranean");
    private static string Reference(string code) => $"https://www.tui.co.uk/cruise/bookitineraries/Island-{code}?itineraryCodeOne={code}";
    private sealed record CandidateData(string SourceReference, string? ProviderItineraryId, string? ProviderOfferId, string? Title, string? ShipName, string? DepartureDate, int? DurationNights, string? DeparturePort, string? ItinerarySummary);
}
