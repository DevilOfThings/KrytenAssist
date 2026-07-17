extern alias KrytenApplication;

using KrytenAssist.Core.Cruises;
using CruiseCaptureCandidateResult =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureCandidateResult;
using CruiseCaptureCandidateStatus =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureCandidateStatus;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

public sealed class CruiseCaptureCandidateResultTests
{
    private const string Reference = "https://example.test/cruises/island-dreams";

    [Fact]
    public void Ready_RetainsExactValuesAndExposesOnlyObservation()
    {
        var observation = CreateObservation(Reference);

        var result = CruiseCaptureCandidateResult.Ready(
            "  Island Dreams  ",
            Reference,
            observation);

        Assert.Equal(CruiseCaptureCandidateStatus.Ready, result.Status);
        Assert.Equal("  Island Dreams  ", result.DisplayLabel);
        Assert.Equal(Reference, result.SourceReference);
        Assert.Same(observation, result.Observation);
        Assert.Null(result.Message);
        Assert.Empty(result.MissingFields);
        Assert.True(result.IsReady);
    }

    [Fact]
    public void Ready_RequiresObservationWithMatchingNonNullSourceReference()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CruiseCaptureCandidateResult.Ready("Island Dreams", Reference, null!));
        Assert.Throws<ArgumentException>(() => CruiseCaptureCandidateResult.Ready(
            "Island Dreams",
            Reference,
            CreateObservation(null)));
        Assert.Throws<ArgumentException>(() => CruiseCaptureCandidateResult.Ready(
            "Island Dreams",
            Reference,
            CreateObservation("https://example.test/cruises/another-cruise")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Factories_RequireDisplayLabel(string? displayLabel)
    {
        Assert.ThrowsAny<ArgumentException>(() => CruiseCaptureCandidateResult.Ready(
            displayLabel!, Reference, CreateObservation(Reference)));
        Assert.ThrowsAny<ArgumentException>(() => CruiseCaptureCandidateResult.Incomplete(
            displayLabel!, Reference, "Details are missing.", ["prices"]));
        Assert.ThrowsAny<ArgumentException>(() => CruiseCaptureCandidateResult.Failed(
            displayLabel!, Reference, "Mapping failed."));
    }

    [Fact]
    public void Factories_RejectDisplayLabelAboveMaximumLength()
    {
        var label = new string('a', CruiseCaptureCandidateResult.MaximumDisplayLabelLength + 1);

        Assert.Throws<ArgumentException>(() => CruiseCaptureCandidateResult.Ready(
            label, Reference, CreateObservation(Reference)));
        Assert.Throws<ArgumentException>(() => CruiseCaptureCandidateResult.Incomplete(
            label, Reference, "Details are missing.", ["prices"]));
        Assert.Throws<ArgumentException>(() => CruiseCaptureCandidateResult.Failed(
            label, Reference, "Mapping failed."));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("cruises/island-dreams")]
    [InlineData("http://example.test/cruises/island-dreams")]
    public void Factories_RequireAbsoluteHttpsSourceReference(string? sourceReference)
    {
        Assert.ThrowsAny<ArgumentException>(() => CruiseCaptureCandidateResult.Ready(
            "Island Dreams", sourceReference!, CreateObservation(sourceReference)));
        Assert.ThrowsAny<ArgumentException>(() => CruiseCaptureCandidateResult.Incomplete(
            "Island Dreams", sourceReference!, "Details are missing.", ["prices"]));
        Assert.ThrowsAny<ArgumentException>(() => CruiseCaptureCandidateResult.Failed(
            "Island Dreams", sourceReference!, "Mapping failed."));
    }

    [Fact]
    public void Factories_RejectSourceReferenceAboveMaximumLength()
    {
        var sourceReference =
            "https://example.test/" +
            new string('a', CruiseCaptureCandidateResult.MaximumSourceReferenceLength);

        Assert.Throws<ArgumentException>(() => CruiseCaptureCandidateResult.Ready(
            "Island Dreams", sourceReference, CreateObservation(sourceReference)));
        Assert.Throws<ArgumentException>(() => CruiseCaptureCandidateResult.Incomplete(
            "Island Dreams", sourceReference, "Details are missing.", ["prices"]));
        Assert.Throws<ArgumentException>(() => CruiseCaptureCandidateResult.Failed(
            "Island Dreams", sourceReference, "Mapping failed."));
    }

    [Fact]
    public void Incomplete_RetainsOrderedMissingFieldsDefensively()
    {
        var missingFields = new List<string> { "shipName", "prices" };

        var result = CruiseCaptureCandidateResult.Incomplete(
            "Island Dreams", Reference, "Details are missing.", missingFields);
        missingFields.Add("departureDate");

        Assert.Equal(CruiseCaptureCandidateStatus.Incomplete, result.Status);
        Assert.False(result.IsReady);
        Assert.Null(result.Observation);
        Assert.Equal("Details are missing.", result.Message);
        Assert.Equal(["shipName", "prices"], result.MissingFields);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<string>)result.MissingFields).Add("departureDate"));
    }

    [Fact]
    public void Incomplete_RejectsInvalidMissingFields()
    {
        Assert.Throws<ArgumentNullException>(() => CruiseCaptureCandidateResult.Incomplete(
            "Island Dreams", Reference, "Details are missing.", null!));
        Assert.Throws<ArgumentException>(() => CruiseCaptureCandidateResult.Incomplete(
            "Island Dreams", Reference, "Details are missing.", []));
        Assert.Throws<ArgumentException>(() => CruiseCaptureCandidateResult.Incomplete(
            "Island Dreams", Reference, "Details are missing.", ["shipName", "SHIPNAME"]));
        Assert.Throws<ArgumentException>(() => CruiseCaptureCandidateResult.Incomplete(
            "Island Dreams", Reference, "Details are missing.", ["shipName", " "]));
        Assert.Throws<ArgumentException>(() => CruiseCaptureCandidateResult.Incomplete(
            "Island Dreams",
            Reference,
            "Details are missing.",
            [new string('a', CruiseCaptureCandidateResult.MaximumMissingFieldLength + 1)]));
        Assert.Throws<ArgumentException>(() => CruiseCaptureCandidateResult.Incomplete(
            "Island Dreams",
            Reference,
            "Details are missing.",
            Enumerable.Range(1, CruiseCaptureCandidateResult.MaximumMissingFieldCount + 1)
                .Select(index => $"field{index}")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IncompleteAndFailed_RequireMessage(string? message)
    {
        Assert.ThrowsAny<ArgumentException>(() => CruiseCaptureCandidateResult.Incomplete(
            "Island Dreams", Reference, message!, ["prices"]));
        Assert.ThrowsAny<ArgumentException>(() => CruiseCaptureCandidateResult.Failed(
            "Island Dreams", Reference, message!));
    }

    [Fact]
    public void IncompleteAndFailed_RejectMessageAboveMaximumLength()
    {
        var message = new string('a', CruiseCaptureCandidateResult.MaximumMessageLength + 1);

        Assert.Throws<ArgumentException>(() => CruiseCaptureCandidateResult.Incomplete(
            "Island Dreams", Reference, message, ["prices"]));
        Assert.Throws<ArgumentException>(() => CruiseCaptureCandidateResult.Failed(
            "Island Dreams", Reference, message));
    }

    [Fact]
    public void Failed_ExposesOnlySafeFailureEvidence()
    {
        var result = CruiseCaptureCandidateResult.Failed(
            "Island Dreams", Reference, "The candidate could not be mapped.");

        Assert.Equal(CruiseCaptureCandidateStatus.Failed, result.Status);
        Assert.False(result.IsReady);
        Assert.Null(result.Observation);
        Assert.Equal("The candidate could not be mapped.", result.Message);
        Assert.Empty(result.MissingFields);
    }

    private static CruiseObservation CreateObservation(string? sourceReference)
    {
        var offer = new CruiseOffer(
            new CruiseProvider("fictional-operator", "Fictional Operator"),
            "island-dreams",
            "Island Dreams",
            "Example Voyager",
            new DateOnly(2027, 3, 14),
            7);
        var snapshot = new CruiseSnapshot(offer, [new CruisePrice(899m, "GBP")]);
        return new CruiseObservation(
            snapshot,
            new DateTimeOffset(2026, 7, 17, 9, 30, 0, TimeSpan.FromHours(1)),
            sourceReference,
            new CruiseSource("fictional-retailer", "Fictional Retailer"));
    }
}
