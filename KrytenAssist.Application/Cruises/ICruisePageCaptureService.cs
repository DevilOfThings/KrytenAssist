namespace KrytenAssist.Application.Cruises;

public interface ICruisePageCaptureService
{
    /// <summary>
    /// Captures one cruise from a validated, transport-neutral page payload.
    /// Implementations should honour cancellation. An orchestration boundary may
    /// translate <see cref="OperationCanceledException"/> into a cancelled result.
    /// </summary>
    Task<CruiseCaptureResult> CaptureAsync(
        CruisePageCaptureRequest request,
        CancellationToken cancellationToken = default);
}
