extern alias KrytenApplication;

using KrytenAssist.Core.Cruises;
using CruisePageCaptureRequest =
    KrytenApplication::KrytenAssist.Application.Cruises.CruisePageCaptureRequest;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

public sealed class CruisePageCaptureRequestTests
{
    private static readonly DateTimeOffset ObservedAt =
        new(2026, 7, 16, 14, 30, 0, TimeSpan.FromHours(1));

    [Fact]
    public void Constructor_RetainsTransportNeutralValues()
    {
        var source = new CruiseSource("tui", "TUI");

        var request = new CruisePageCaptureRequest(
            "marella-cruise-of-the-week",
            source,
            "https://www.tui.co.uk/cruise/example",
            ObservedAt,
            "{\"title\":\"Example cruise\"}");

        Assert.Equal("marella-cruise-of-the-week", request.SourceIdentifier);
        Assert.Same(source, request.Source);
        Assert.Equal("https://www.tui.co.uk/cruise/example", request.SourceReference);
        Assert.Equal(ObservedAt, request.ObservedAt);
        Assert.Equal("{\"title\":\"Example cruise\"}", request.PagePayload);
    }

    [Theory]
    [InlineData(null, "https://example.com/cruise", "payload", "sourceIdentifier")]
    [InlineData("", "https://example.com/cruise", "payload", "sourceIdentifier")]
    [InlineData("source", null, "payload", "sourceReference")]
    [InlineData("source", "", "payload", "sourceReference")]
    [InlineData("source", "relative/reference", "payload", "sourceReference")]
    [InlineData("source", "https:relative-reference", "payload", "sourceReference")]
    [InlineData("source", "http://example.com/cruise", "payload", "sourceReference")]
    [InlineData("source", "https://example.com/cruise", null, "pagePayload")]
    [InlineData("source", "https://example.com/cruise", "", "pagePayload")]
    public void Constructor_RejectsInvalidStrings(
        string? identifier,
        string? reference,
        string? payload,
        string parameter)
    {
        var exception = Assert.ThrowsAny<ArgumentException>(() =>
            new CruisePageCaptureRequest(
                identifier!,
                new CruiseSource("retailer", "Retailer"),
                reference!,
                ObservedAt,
                payload!));

        Assert.Equal(parameter, exception.ParamName);
    }

    [Fact]
    public void Constructor_RejectsNullRetailSource()
    {
        Assert.Throws<ArgumentNullException>(() => new CruisePageCaptureRequest(
            "source",
            null!,
            "https://example.com/cruise",
            ObservedAt,
            "payload"));
    }

    [Fact]
    public void Constructor_AcceptsPayloadAtMaximumAndRejectsPayloadAboveMaximum()
    {
        var maximum = new string('x', CruisePageCaptureRequest.MaximumPagePayloadLength);
        var aboveMaximum = maximum + "x";

        var request = Create(maximum);
        var exception = Assert.Throws<ArgumentException>(() => Create(aboveMaximum));

        Assert.Equal(CruisePageCaptureRequest.MaximumPagePayloadLength, request.PagePayload.Length);
        Assert.Equal("pagePayload", exception.ParamName);
    }

    private static CruisePageCaptureRequest Create(string payload) => new(
        "source",
        new CruiseSource("retailer", "Retailer"),
        "https://example.com/cruise",
        ObservedAt,
        payload);
}
