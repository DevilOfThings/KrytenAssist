extern alias KrytenApplication;

using KrytenAssist.Core.Cruises;
using CruiseCaptureResult =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureResult;
using CruisePageCaptureRequest =
    KrytenApplication::KrytenAssist.Application.Cruises.CruisePageCaptureRequest;
using ICruisePageCaptureService =
    KrytenApplication::KrytenAssist.Application.Cruises.ICruisePageCaptureService;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

public sealed class CruisePageCaptureServiceContractTests
{
    [Fact]
    public async Task Interface_AcceptsValidatedRequestAndCancellationToken()
    {
        var service = new RecordingCaptureService();
        var request = new CruisePageCaptureRequest(
            "source",
            new CruiseSource("retailer", "Retailer"),
            "https://example.com/cruise",
            DateTimeOffset.UtcNow,
            "payload");
        using var cancellation = new CancellationTokenSource();

        var result = await service.CaptureAsync(request, cancellation.Token);

        Assert.Same(request, service.Request);
        Assert.Equal(cancellation.Token, service.CancellationToken);
        Assert.Equal(
            KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureStatus.Cancelled,
            result.Status);
    }

    private sealed class RecordingCaptureService : ICruisePageCaptureService
    {
        public CruisePageCaptureRequest? Request { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task<CruiseCaptureResult> CaptureAsync(
            CruisePageCaptureRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            CancellationToken = cancellationToken;
            return Task.FromResult(CruiseCaptureResult.Cancelled("Capture was cancelled."));
        }
    }
}
