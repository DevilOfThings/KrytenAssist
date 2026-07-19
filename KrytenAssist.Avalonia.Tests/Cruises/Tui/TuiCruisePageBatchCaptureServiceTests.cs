extern alias KrytenApplication;
extern alias KrytenInfrastructure;

using System.Text.Json;
using KrytenAssist.Core.Cruises;
using CruiseCaptureBatchStatus =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureBatchStatus;
using CruiseCaptureCandidateStatus =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureCandidateStatus;
using CruisePageCaptureRequest =
    KrytenApplication::KrytenAssist.Application.Cruises.CruisePageCaptureRequest;
using ICruisePageBatchCaptureService =
    KrytenApplication::KrytenAssist.Application.Cruises.ICruisePageBatchCaptureService;
using TuiCruisePageCaptureService =
    KrytenInfrastructure::KrytenAssist.Infrastructure.Cruises.Tui.TuiCruisePageCaptureService;

namespace KrytenAssist.Avalonia.Tests.Cruises.Tui;

public sealed class TuiCruisePageBatchCaptureServiceTests
{
    private static readonly DateTimeOffset ObservedAt =
        new(2026, 7, 17, 14, 20, 0, TimeSpan.FromHours(1));

    private readonly ICruisePageBatchCaptureService _service =
        new TuiCruisePageCaptureService();

    [Fact]
    public async Task CaptureAsync_MapsCompleteCandidatesInExactOrderWithOwnReferences()
    {
        var first = CompleteCandidate(1, "Island Dreams", 899m);
        var second = CompleteCandidate(2, "Aegean Shores", 1099m);
        var source = new CruiseSource("tui", "TUI");

        var result = await _service.CaptureAsync(CreateRequest(
            CreatePayload([first, second]),
            source: source));

        Assert.Equal(CruiseCaptureBatchStatus.Completed, result.Status);
        Assert.Equal(2, result.ReadyCount);
        Assert.Equal(0, result.IncompleteCount);
        Assert.Equal(["Island Dreams", "Aegean Shores"],
            result.Candidates.Select(candidate => candidate.DisplayLabel));
        Assert.Collection(
            result.Candidates,
            candidate => AssertReadyCandidate(candidate, first, source, 899m),
            candidate => AssertReadyCandidate(candidate, second, source, 1099m));
    }

    [Fact]
    public async Task CaptureAsync_MapsModernResultPriceAndOnlinePromotionPayload()
    {
        var candidate = CompleteCandidate(1, "Modern Result", 1439m) with
        {
            ShipName = "Marella Voyager",
            DepartureDate = "2026-10-02",
            PromotionSummary = "Includes £38pp online discount",
            Prices =
            [
                new PriceDto(1439m, "GBP", "per person"),
                new PriceDto(2877m, "GBP", "total based on 2 sharing")
            ]
        };

        var result = await _service.CaptureAsync(CreateRequest(CreatePayload([candidate])));

        var observation = Assert.Single(result.Candidates).Observation!;
        Assert.Equal(new DateOnly(2026, 10, 2), observation.Snapshot.Offer.DepartureDate);
        Assert.Equal("Marella Voyager", observation.Snapshot.Offer.ShipName);
        Assert.Equal(
            [
                new CruisePrice(1439m, "GBP", "per person"),
                new CruisePrice(2877m, "GBP", "total based on 2 sharing")
            ],
            observation.Snapshot.Prices);
        Assert.Equal("Includes £38pp online discount", observation.Snapshot.PromotionSummary);
    }

    [Fact]
    public async Task CaptureAsync_RetainsReadyCandidateBesideIndependentIncompleteCandidate()
    {
        var complete = CompleteCandidate(1, "Island Dreams", 899m);
        var incomplete = CompleteCandidate(2, "Aegean Shores", 1099m) with
        {
            ShipName = null,
            Prices = []
        };

        var result = await _service.CaptureAsync(CreateRequest(
            CreatePayload([complete, incomplete])));

        Assert.Equal(CruiseCaptureBatchStatus.Completed, result.Status);
        Assert.Equal(1, result.ReadyCount);
        Assert.Equal(1, result.IncompleteCount);
        Assert.Equal(CruiseCaptureCandidateStatus.Ready, result.Candidates[0].Status);
        Assert.Equal(CruiseCaptureCandidateStatus.Incomplete, result.Candidates[1].Status);
        Assert.Equal(["shipName", "prices"], result.Candidates[1].MissingFields);
        Assert.Null(result.Candidates[1].Observation);
    }

    [Fact]
    public async Task CaptureAsync_AllowsCompletedBatchWithNoReadyCandidate()
    {
        var first = CompleteCandidate(1, "Island Dreams", 899m) with { Prices = [] };
        var second = CompleteCandidate(2, "Aegean Shores", 1099m) with { ShipName = " " };

        var result = await _service.CaptureAsync(CreateRequest(
            CreatePayload([first, second])));

        Assert.Equal(CruiseCaptureBatchStatus.Completed, result.Status);
        Assert.Equal(0, result.ReadyCount);
        Assert.Equal(2, result.IncompleteCount);
    }

    [Fact]
    public async Task CaptureAsync_IsolatesCandidateLocalMappingFailure()
    {
        var complete = CompleteCandidate(1, "Island Dreams", 899m);
        var failed = CompleteCandidate(2, "Aegean Shores", 1099m) with
        {
            PromotionSummary = new string('a', 513)
        };

        var result = await _service.CaptureAsync(CreateRequest(
            CreatePayload([complete, failed])));

        Assert.Equal(CruiseCaptureBatchStatus.Completed, result.Status);
        Assert.Equal(1, result.ReadyCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(CruiseCaptureCandidateStatus.Ready, result.Candidates[0].Status);
        Assert.Equal(CruiseCaptureCandidateStatus.Failed, result.Candidates[1].Status);
        Assert.Null(result.Candidates[1].Observation);
        Assert.Empty(result.Candidates[1].MissingFields);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CaptureAsync_RetainsControlledTruncationEvidence(bool wasTruncated)
    {
        var result = await _service.CaptureAsync(CreateRequest(
            CreatePayload([CompleteCandidate(1, "Island Dreams", 899m)], wasTruncated)));

        Assert.Equal(CruiseCaptureBatchStatus.Completed, result.Status);
        Assert.Equal(wasTruncated, result.WasTruncated);
    }

    [Fact]
    public async Task CaptureAsync_DeduplicatesExactReferencesByFirstOccurrence()
    {
        var first = CompleteCandidate(1, "First label", 899m);
        var duplicate = first with { Title = "Duplicate label", Price = 999m };

        var result = await _service.CaptureAsync(CreateRequest(
            CreatePayload([first, duplicate])));

        var candidate = Assert.Single(result.Candidates);
        Assert.Equal("First label", candidate.DisplayLabel);
        Assert.Equal(899m, candidate.Observation!.Snapshot.Prices[0].Amount);
    }

    [Fact]
    public async Task CaptureAsync_RejectsOversizedPublishedCandidateCollection()
    {
        var candidates = Enumerable.Range(1, 11)
            .Select(index => CompleteCandidate(index, $"Cruise {index}", 800m + index))
            .ToArray();

        var result = await _service.CaptureAsync(CreateRequest(CreatePayload(candidates)));

        Assert.Equal(CruiseCaptureBatchStatus.Failed, result.Status);
        Assert.Empty(result.Candidates);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("cruise/bookitineraries/Island-1?itineraryCodeOne=1")]
    [InlineData("http://www.tui.co.uk/cruise/bookitineraries/Island-1?itineraryCodeOne=1")]
    [InlineData("https://example.test/cruise/bookitineraries/Island-1?itineraryCodeOne=1")]
    [InlineData("https://www.tui.co.uk/cruise/deals/island?itineraryCodeOne=1")]
    [InlineData("https://www.tui.co.uk/cruise/bookitineraries/Island-1")]
    public async Task CaptureAsync_RejectsUntrustedCandidateReference(string? sourceReference)
    {
        var candidate = CompleteCandidate(1, "Island Dreams", 899m) with
        {
            SourceReference = sourceReference
        };

        var result = await _service.CaptureAsync(CreateRequest(
            CreatePayload([candidate])));

        Assert.Equal(CruiseCaptureBatchStatus.Failed, result.Status);
        Assert.Empty(result.Candidates);
        Assert.DoesNotContain("example.test", result.Message!, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("{}", CruiseCaptureBatchStatus.Unsupported)]
    [InlineData("{\"version\":2,\"candidates\":[]}", CruiseCaptureBatchStatus.Unsupported)]
    [InlineData("{not-json", CruiseCaptureBatchStatus.Failed)]
    [InlineData("{\"version\":1,\"candidates\":[]}", CruiseCaptureBatchStatus.Incomplete)]
    [InlineData("{\"version\":1,\"candidates\":[null]}", CruiseCaptureBatchStatus.Failed)]
    public async Task CaptureAsync_ReturnsControlledBatchWideResult(
        string payload,
        CruiseCaptureBatchStatus expectedStatus)
    {
        var result = await _service.CaptureAsync(CreateRequest(payload));

        Assert.Equal(expectedStatus, result.Status);
        Assert.Empty(result.Candidates);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
        Assert.DoesNotContain("{not-json", result.Message!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CaptureAsync_RejectsUnsupportedRequestSource()
    {
        var result = await _service.CaptureAsync(CreateRequest(
            CreatePayload([CompleteCandidate(1, "Island Dreams", 899m)]),
            sourceReference: "https://www.tui.co.uk.evil.example/cruise/deals"));

        Assert.Equal(CruiseCaptureBatchStatus.Unsupported, result.Status);
        Assert.Empty(result.Candidates);
        Assert.DoesNotContain("evil", result.Message!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CaptureAsync_PreCancelledTokenPublishesNoCandidates()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var result = await _service.CaptureAsync(
            CreateRequest(CreatePayload([CompleteCandidate(1, "Island Dreams", 899m)])),
            cancellation.Token);

        Assert.Equal(CruiseCaptureBatchStatus.Cancelled, result.Status);
        Assert.Empty(result.Candidates);
    }

    private static void AssertReadyCandidate(
        KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureCandidateResult result,
        Candidate candidate,
        CruiseSource source,
        decimal expectedPrice)
    {
        Assert.Equal(CruiseCaptureCandidateStatus.Ready, result.Status);
        Assert.Equal(candidate.SourceReference, result.SourceReference);
        var observation = Assert.IsType<CruiseObservation>(result.Observation);
        Assert.Equal(candidate.SourceReference, observation.SourceReference);
        Assert.Same(source, observation.Source);
        Assert.Equal(ObservedAt, observation.ObservedAt);
        Assert.Equal("marella", observation.Snapshot.Offer.Provider.Id);
        Assert.Equal("Marella Cruises", observation.Snapshot.Offer.Provider.Name);
        Assert.Equal(expectedPrice, observation.Snapshot.Prices[0].Amount);
    }

    private static CruisePageCaptureRequest CreateRequest(
        string payload,
        string sourceIdentifier = "marella-cruise-of-the-week",
        CruiseSource? source = null,
        string sourceReference = "https://www.tui.co.uk/cruise/deals/fictional-deals") =>
        new(
            sourceIdentifier,
            source ?? new CruiseSource("tui", "TUI"),
            sourceReference,
            ObservedAt,
            payload);

    private static string CreatePayload(
        IReadOnlyList<Candidate> candidates,
        bool wasTruncated = false) =>
        JsonSerializer.Serialize(new Payload(1, wasTruncated, candidates));

    private static Candidate CompleteCandidate(
        int index,
        string title,
        decimal price) =>
        new(
            $"https://www.tui.co.uk/cruise/bookitineraries/{title.Replace(' ', '-')}-{1000 + index}?itineraryCodeOne={1000 + index}",
            $"fictional-offer-{index}",
            title,
            "Marella Voyager",
            "2027-08-14",
            7,
            "Palma",
            price,
            "GBP",
            "per person",
            "Fictional promotion");

    private sealed record Payload(
        int Version,
        bool WasTruncated,
        IReadOnlyList<Candidate> Candidates);

    private sealed record Candidate(
        string? SourceReference,
        string? ProviderOfferId,
        string? Title,
        string? ShipName,
        string? DepartureDate,
        int? DurationNights,
        string? DeparturePort,
        decimal? Price,
        string? Currency,
        string? Basis,
        string? PromotionSummary)
    {
        public IReadOnlyList<PriceDto> Prices { get; init; } =
            Price is null ? [] : [new PriceDto(Price, Currency, Basis)];
    }

    private sealed record PriceDto(decimal? Amount, string? Currency, string? Basis);
}
