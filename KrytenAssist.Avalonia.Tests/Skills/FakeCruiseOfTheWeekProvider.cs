extern alias KrytenApplication;

using KrytenAssist.Core.Cruises;
using CruiseOfTheWeekException =
    KrytenApplication::KrytenAssist.Application.Cruises.CruiseOfTheWeekException;
using ICruiseOfTheWeekProvider =
    KrytenApplication::KrytenAssist.Application.Cruises.ICruiseOfTheWeekProvider;

namespace KrytenAssist.Avalonia.Tests.Skills;

internal sealed class FakeCruiseOfTheWeekProvider : ICruiseOfTheWeekProvider
{
    public CruiseObservation? Observation { get; set; }

    public Exception? Exception { get; set; }

    public int InvocationCount { get; private set; }

    public DateTimeOffset? ReceivedObservedAt { get; private set; }

    public CancellationToken ReceivedCancellationToken { get; private set; }

    public Task<CruiseObservation> GetCurrentAsync(
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default)
    {
        InvocationCount++;
        ReceivedObservedAt = observedAt;
        ReceivedCancellationToken = cancellationToken;
        cancellationToken.ThrowIfCancellationRequested();

        if (Exception is not null)
        {
            return Task.FromException<CruiseObservation>(Exception);
        }

        return Task.FromResult(
            Observation ?? throw new CruiseOfTheWeekException("No test observation was configured."));
    }
}
