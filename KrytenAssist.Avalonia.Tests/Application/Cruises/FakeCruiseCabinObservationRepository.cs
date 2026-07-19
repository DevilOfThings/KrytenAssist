extern alias KrytenApplication;

using KrytenAssist.Core.Cruises;
using CabinRepository = KrytenApplication::KrytenAssist.Application.Abstractions.Persistence.ICruiseCabinObservationRepository;
using CabinHistory = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinRecordedHistory;
using CabinRepositoryResult = KrytenApplication::KrytenAssist.Application.Cruises.CruiseCabinRepositoryRecordResult;

namespace KrytenAssist.Avalonia.Tests.Application.Cruises;

internal sealed class FakeCruiseCabinObservationRepository : CabinRepository
{
    internal CabinRepositoryResult? RecordResult { get; set; }
    internal CabinHistory? GetResult { get; set; }
    internal IReadOnlyList<CabinHistory> ListResult { get; set; } = [];
    internal Exception? RecordException { get; set; }
    internal Exception? GetException { get; set; }
    internal Exception? ListException { get; set; }
    internal int ListCalls { get; private set; }

    public Task<CabinRepositoryResult> RecordAsync(CruiseCabinObservation observation, CancellationToken cancellationToken = default)
    {
        if (RecordException is not null) return Task.FromException<CabinRepositoryResult>(RecordException);
        if (RecordResult is not null) GetResult = RecordResult.History;
        return Task.FromResult(RecordResult!);
    }

    public Task<CabinHistory?> GetAsync(string seriesKey, CancellationToken cancellationToken = default) =>
        GetException is null ? Task.FromResult(GetResult) : Task.FromException<CabinHistory?>(GetException);

    public Task<IReadOnlyList<CabinHistory>> ListAsync(CancellationToken cancellationToken = default)
    {
        ListCalls++;
        return ListException is null ? Task.FromResult(ListResult) : Task.FromException<IReadOnlyList<CabinHistory>>(ListException);
    }
}
