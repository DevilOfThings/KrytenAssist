extern alias KrytenApplication;

using KrytenAssist.Core.Cruises;
using CruiseCaptureBatchResult =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureBatchResult;
using CruisePageCaptureRequest =
    KrytenApplication::KrytenAssist.Application.Cruises.CruisePageCaptureRequest;
using ICruisePageBatchCaptureService =
    KrytenApplication::KrytenAssist.Application.Cruises.ICruisePageBatchCaptureService;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

public sealed class CruisePageBatchCaptureServiceContractTests
{
    [Fact]
    public async Task Interface_AcceptsExistingRequestAndForwardsExactCancellationToken()
    {
        var service = new RecordingBatchCaptureService();
        var request = new CruisePageCaptureRequest(
            "fictional-source",
            new CruiseSource("fictional-retailer", "Fictional Retailer"),
            "https://example.test/cruises/deals",
            new DateTimeOffset(2026, 7, 17, 9, 30, 0, TimeSpan.FromHours(1)),
            "bounded transport-neutral payload");
        using var cancellation = new CancellationTokenSource();

        var result = await service.CaptureAsync(request, cancellation.Token);

        Assert.Same(request, service.Request);
        Assert.Equal(cancellation.Token, service.CancellationToken);
        Assert.Equal(
            KrytenApplication::KrytenAssist.Application.Cruises.CruiseCaptureBatchStatus.Cancelled,
            result.Status);
    }

    private sealed class RecordingBatchCaptureService : ICruisePageBatchCaptureService
    {
        public CruisePageCaptureRequest? Request { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task<CruiseCaptureBatchResult> CaptureAsync(
            CruisePageCaptureRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            CancellationToken = cancellationToken;
            return Task.FromResult(CruiseCaptureBatchResult.Cancelled("Capture was cancelled."));
        }
    }
}
