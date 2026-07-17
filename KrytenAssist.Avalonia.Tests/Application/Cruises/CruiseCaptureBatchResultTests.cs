extern alias KrytenApplication;

using KrytenAssist.Core.Cruises;
using CruiseCaptureBatchResult =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureBatchResult;
using CruiseCaptureBatchStatus =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureBatchStatus;
using CruiseCaptureCandidateResult =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureCandidateResult;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

public sealed class CruiseCaptureBatchResultTests
{
    [Fact]
    public void Completed_RetainsOneCandidate()
    {
        var candidate = ReadyCandidate(1);

        var result = CruiseCaptureBatchResult.Completed([candidate]);

        Assert.True(result.IsCompleted);
        Assert.Equal(CruiseCaptureBatchStatus.Completed, result.Status);
        Assert.Equal([candidate], result.Candidates);
        Assert.Null(result.Message);
        Assert.False(result.WasTruncated);
        Assert.Equal(1, result.ReadyCount);
        Assert.Equal(0, result.IncompleteCount);
        Assert.Equal(0, result.FailedCount);
    }

    [Fact]
    public void Completed_RetainsMixedCandidatesInOrderAndComputesCounts()
    {
        var ready = ReadyCandidate(1);
        var incomplete = IncompleteCandidate(2);
        var failed = FailedCandidate(3);

        var result = CruiseCaptureBatchResult.Completed(
            [ready, incomplete, failed],
            wasTruncated: true);

        Assert.Equal([ready, incomplete, failed], result.Candidates);
        Assert.True(result.WasTruncated);
        Assert.Equal(1, result.ReadyCount);
        Assert.Equal(1, result.IncompleteCount);
        Assert.Equal(1, result.FailedCount);
    }

    [Fact]
    public void Completed_AllowsNoReadyCandidates()
    {
        var incomplete = IncompleteCandidate(1);
        var failed = FailedCandidate(2);

        var result = CruiseCaptureBatchResult.Completed([incomplete, failed]);

        Assert.Equal([incomplete, failed], result.Candidates);
        Assert.Equal(0, result.ReadyCount);
        Assert.Equal(1, result.IncompleteCount);
        Assert.Equal(1, result.FailedCount);
    }

    [Fact]
    public void Completed_EnumeratesSourceOnceAndRetainsDefensiveImmutableCopy()
    {
        var source = new List<CruiseCaptureCandidateResult> { ReadyCandidate(1) };
        var singleUseSource = new SingleUseEnumerable<CruiseCaptureCandidateResult>(source);

        var result = CruiseCaptureBatchResult.Completed(singleUseSource);
        source.Add(ReadyCandidate(2));

        Assert.Single(result.Candidates);
        Assert.Equal(1, singleUseSource.EnumerationCount);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<CruiseCaptureCandidateResult>)result.Candidates).Add(ReadyCandidate(3)));
    }

    [Fact]
    public void Completed_RejectsInvalidCandidateSequences()
    {
        Assert.Throws<ArgumentNullException>(() => CruiseCaptureBatchResult.Completed(null!));
        Assert.Throws<ArgumentException>(() => CruiseCaptureBatchResult.Completed([]));
        Assert.Throws<ArgumentException>(() => CruiseCaptureBatchResult.Completed(
            new CruiseCaptureCandidateResult[] { null! }));
        Assert.Throws<ArgumentException>(() => CruiseCaptureBatchResult.Completed(
            Enumerable.Range(1, CruiseCaptureBatchResult.MaximumCandidateCount + 1)
                .Select(ReadyCandidate)));
    }

    [Fact]
    public void Completed_RejectsExactDuplicateCandidateReferences()
    {
        var first = ReadyCandidate(1);
        var duplicate = CruiseCaptureCandidateResult.Failed(
            "Another label",
            first.SourceReference,
            "Mapping failed.");

        Assert.Throws<ArgumentException>(() =>
            CruiseCaptureBatchResult.Completed([first, duplicate]));
    }

    [Theory]
    [InlineData(CruiseCaptureBatchStatus.Incomplete)]
    [InlineData(CruiseCaptureBatchStatus.Unsupported)]
    [InlineData(CruiseCaptureBatchStatus.Failed)]
    [InlineData(CruiseCaptureBatchStatus.Cancelled)]
    public void NonCompletedFactories_RetainSafeMessageAndNoCandidates(
        CruiseCaptureBatchStatus status)
    {
        var result = CreateNonCompleted(status, "The batch could not be completed.");

        Assert.Equal(status, result.Status);
        Assert.False(result.IsCompleted);
        Assert.Equal("The batch could not be completed.", result.Message);
        Assert.Empty(result.Candidates);
        Assert.False(result.WasTruncated);
        Assert.Equal(0, result.ReadyCount);
        Assert.Equal(0, result.IncompleteCount);
        Assert.Equal(0, result.FailedCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NonCompletedFactories_RequireMessage(string? message)
    {
        Assert.ThrowsAny<ArgumentException>(() => CruiseCaptureBatchResult.Incomplete(message!));
        Assert.ThrowsAny<ArgumentException>(() => CruiseCaptureBatchResult.Unsupported(message!));
        Assert.ThrowsAny<ArgumentException>(() => CruiseCaptureBatchResult.Failed(message!));
        Assert.ThrowsAny<ArgumentException>(() => CruiseCaptureBatchResult.Cancelled(message!));
    }

    [Fact]
    public void NonCompletedFactories_RejectMessageAboveMaximumLength()
    {
        var message = new string('a', CruiseCaptureBatchResult.MaximumMessageLength + 1);

        Assert.Throws<ArgumentException>(() => CruiseCaptureBatchResult.Incomplete(message));
        Assert.Throws<ArgumentException>(() => CruiseCaptureBatchResult.Unsupported(message));
        Assert.Throws<ArgumentException>(() => CruiseCaptureBatchResult.Failed(message));
        Assert.Throws<ArgumentException>(() => CruiseCaptureBatchResult.Cancelled(message));
    }

    private static CruiseCaptureBatchResult CreateNonCompleted(
        CruiseCaptureBatchStatus status,
        string message) => status switch
        {
            CruiseCaptureBatchStatus.Incomplete => CruiseCaptureBatchResult.Incomplete(message),
            CruiseCaptureBatchStatus.Unsupported => CruiseCaptureBatchResult.Unsupported(message),
            CruiseCaptureBatchStatus.Failed => CruiseCaptureBatchResult.Failed(message),
            CruiseCaptureBatchStatus.Cancelled => CruiseCaptureBatchResult.Cancelled(message),
            _ => throw new InvalidOperationException()
        };

    private static CruiseCaptureCandidateResult ReadyCandidate(int index)
    {
        var reference = Reference(index);
        var offer = new CruiseOffer(
            new CruiseProvider("fictional-operator", "Fictional Operator"),
            $"offer-{index}",
            $"Cruise {index}",
            "Example Voyager",
            new DateOnly(2027, 4, index),
            7);
        var observation = new CruiseObservation(
            new CruiseSnapshot(offer, [new CruisePrice(799m + index, "GBP")]),
            new DateTimeOffset(2026, 7, 17, 9, 30, 0, TimeSpan.FromHours(1)),
            reference,
            new CruiseSource("fictional-retailer", "Fictional Retailer"));
        return CruiseCaptureCandidateResult.Ready($"Cruise {index}", reference, observation);
    }

    private static CruiseCaptureCandidateResult IncompleteCandidate(int index) =>
        CruiseCaptureCandidateResult.Incomplete(
            $"Cruise {index}", Reference(index), "Prices are missing.", ["prices"]);

    private static CruiseCaptureCandidateResult FailedCandidate(int index) =>
        CruiseCaptureCandidateResult.Failed(
            $"Cruise {index}", Reference(index), "Mapping failed.");

    private static string Reference(int index) =>
        $"https://example.test/cruises/cruise-{index}";

    private sealed class SingleUseEnumerable<T>(IEnumerable<T> values) : IEnumerable<T>
    {
        public int EnumerationCount { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            EnumerationCount++;
            if (EnumerationCount > 1)
            {
                throw new InvalidOperationException("The sequence was enumerated more than once.");
            }

            return values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}
