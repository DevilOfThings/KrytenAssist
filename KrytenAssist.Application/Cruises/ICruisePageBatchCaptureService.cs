namespace KrytenAssist.Application.Cruises;

public interface ICruisePageBatchCaptureService
{
    /// <summary>
    /// Captures a bounded batch of cruise candidates in deterministic page order
    /// from a validated, transport-neutral page payload. Implementations should
    /// honour cancellation.
    /// An orchestration boundary may translate <see cref="OperationCanceledException"/>
    /// into a cancelled result. The interface itself implies no external work.
    /// </summary>
    Task<CruiseCaptureBatchResult> CaptureAsync(
        CruisePageCaptureRequest request,
        CancellationToken cancellationToken = default);
}
