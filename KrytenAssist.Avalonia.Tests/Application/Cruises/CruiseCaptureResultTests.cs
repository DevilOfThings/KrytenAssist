extern alias KrytenApplication;

using KrytenAssist.Core.Cruises;
using CruiseCaptureResult =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureResult;
using CruiseCaptureStatus =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureStatus;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

public sealed class CruiseCaptureResultTests
{
    [Fact]
    public void Succeeded_RequiresAndRetainsObservationOnly()
    {
        var observation = CreateObservation();

        var result = CruiseCaptureResult.Succeeded(observation);

        Assert.True(result.IsSuccess);
        Assert.Equal(CruiseCaptureStatus.Success, result.Status);
        Assert.Same(observation, result.Observation);
        Assert.Null(result.Message);
        Assert.Empty(result.MissingFields);
        Assert.Throws<ArgumentNullException>(() => CruiseCaptureResult.Succeeded(null!));
    }

    [Fact]
    public void Incomplete_RequiresDistinctValidatedBoundedMissingFields()
    {
        var result = CruiseCaptureResult.Incomplete(
            "Required cruise details are missing.",
            ["shipName", "departureDate"]);

        Assert.Equal(CruiseCaptureStatus.Incomplete, result.Status);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Observation);
        Assert.Equal(["shipName", "departureDate"], result.MissingFields);
        Assert.Throws<ArgumentException>(() =>
            CruiseCaptureResult.Incomplete("Missing.", []));
        Assert.Throws<ArgumentException>(() =>
            CruiseCaptureResult.Incomplete("Missing.", ["ship", "SHIP"]));
        Assert.Throws<ArgumentException>(() =>
            CruiseCaptureResult.Incomplete("Missing.", ["ship", " "]));
        Assert.Throws<ArgumentException>(() => CruiseCaptureResult.Incomplete(
            "Missing.",
            Enumerable.Range(1, CruiseCaptureResult.MaximumMissingFieldCount + 1)
                .Select(index => $"field{index}")));
    }

    [Theory]
    [InlineData(CruiseCaptureStatus.Ambiguous)]
    [InlineData(CruiseCaptureStatus.Unsupported)]
    [InlineData(CruiseCaptureStatus.Failed)]
    [InlineData(CruiseCaptureStatus.Cancelled)]
    public void NonSuccessFactories_ContainSafeMessageAndNoObservationOrFields(
        CruiseCaptureStatus status)
    {
        var result = status switch
        {
            CruiseCaptureStatus.Ambiguous => CruiseCaptureResult.Ambiguous("More than one cruise was found."),
            CruiseCaptureStatus.Unsupported => CruiseCaptureResult.Unsupported("This page is not supported."),
            CruiseCaptureStatus.Failed => CruiseCaptureResult.Failed("The page could not be captured."),
            CruiseCaptureStatus.Cancelled => CruiseCaptureResult.Cancelled("Capture was cancelled."),
            _ => throw new InvalidOperationException()
        };

        Assert.Equal(status, result.Status);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Observation);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
        Assert.Empty(result.MissingFields);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NonSuccessFactories_RequireMessage(string? message)
    {
        Assert.ThrowsAny<ArgumentException>(() => CruiseCaptureResult.Ambiguous(message!));
        Assert.ThrowsAny<ArgumentException>(() => CruiseCaptureResult.Unsupported(message!));
        Assert.ThrowsAny<ArgumentException>(() => CruiseCaptureResult.Failed(message!));
        Assert.ThrowsAny<ArgumentException>(() => CruiseCaptureResult.Cancelled(message!));
        Assert.ThrowsAny<ArgumentException>(() =>
            CruiseCaptureResult.Incomplete(message!, ["ship"]));
    }

    private static CruiseObservation CreateObservation()
    {
        var offer = new CruiseOffer(
            new CruiseProvider("operator", "Operator"),
            "offer-1",
            "Example Cruise",
            "Example Ship",
            new DateOnly(2027, 1, 10),
            7);
        var snapshot = new CruiseSnapshot(offer, [new CruisePrice(999m, "GBP")]);
        return new CruiseObservation(
            snapshot,
            DateTimeOffset.UtcNow,
            "https://example.com/cruise",
            new CruiseSource("retailer", "Retailer"));
    }
}
