extern alias KrytenApplication;

using KrytenAssist.Core.Cruises;
using Repository = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseObservationRepository;
using RecordedHistory = KrytenApplication::KrytenAssist.Application.Cruises.CruiseRecordedHistory;
using RepositoryResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseObservationRepositoryRecordResult;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

internal sealed class FakeCruiseObservationRepository : Repository
{
    internal int RecordCalls { get; private set; }
    internal int GetCalls { get; private set; }
    internal int ListCalls { get; private set; }
    internal CruiseSailingKey? RecordedKey { get; private set; }
    internal CruiseObservationFingerprint? RecordedFingerprint { get; private set; }
    internal CruiseObservation? RecordedObservation { get; private set; }
    internal CancellationToken RecordedToken { get; private set; }
    internal CruiseSailingKey? RequestedKey { get; private set; }
    internal CruiseSource? RequestedSource { get; private set; }

    internal RepositoryResult? RecordResult { get; set; }
    internal RecordedHistory? GetResult { get; set; }
    internal IReadOnlyList<RecordedHistory> ListResult { get; set; } = [];
    internal Exception? RecordException { get; set; }
    internal Exception? GetException { get; set; }
    internal Exception? ListException { get; set; }

    public Task<RepositoryResult> RecordAsync(
        CruiseSailingKey sailingKey,
        CruiseObservationFingerprint fingerprint,
        CruiseObservation observation,
        CancellationToken cancellationToken = default)
    {
        RecordCalls++;
        RecordedKey = sailingKey;
        RecordedFingerprint = fingerprint;
        RecordedObservation = observation;
        RecordedToken = cancellationToken;
        return RecordException is null
            ? Task.FromResult(RecordResult!)
            : Task.FromException<RepositoryResult>(RecordException);
    }

    public Task<RecordedHistory?> GetAsync(
        CruiseSailingKey sailingKey,
        CruiseSource? source,
        CancellationToken cancellationToken = default)
    {
        GetCalls++;
        RequestedKey = sailingKey;
        RequestedSource = source;
        return GetException is null
            ? Task.FromResult(GetResult)
            : Task.FromException<RecordedHistory?>(GetException);
    }

    public Task<IReadOnlyList<RecordedHistory>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        ListCalls++;
        return ListException is null
            ? Task.FromResult(ListResult)
            : Task.FromException<IReadOnlyList<RecordedHistory>>(ListException);
    }
}
