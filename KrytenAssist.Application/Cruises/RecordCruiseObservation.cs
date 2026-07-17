using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed class RecordCruiseObservation
{
    private readonly ICruiseObservationRepository _repository;
    private readonly CruisePriceHistoryAnalyzer _analyzer;

    public RecordCruiseObservation(
        ICruiseObservationRepository repository,
        CruisePriceHistoryAnalyzer analyzer)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(analyzer);
        _repository = repository;
        _analyzer = analyzer;
    }

    public async Task<CruiseObservationRecordResult> ExecuteAsync(
        CruiseObservation observation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observation);
        if (cancellationToken.IsCancellationRequested)
        {
            return CruiseObservationRecordResult.Cancelled();
        }

        var sailingKey = CruiseSailingKey.From(observation);
        var fingerprint = CruiseObservationFingerprint.From(observation);
        try
        {
            var repositoryResult = await _repository.RecordAsync(
                sailingKey,
                fingerprint,
                observation,
                cancellationToken);
            var summary = repositoryResult.History.Analyze(_analyzer);
            var status = repositoryResult.State switch
            {
                CruiseObservationRepositoryRecordState.FirstObservationRecorded =>
                    CruiseObservationRecordStatus.FirstObservationRecorded,
                CruiseObservationRepositoryRecordState.ChangedObservationRecorded =>
                    CruiseObservationRecordStatus.ChangedObservationRecorded,
                CruiseObservationRepositoryRecordState.AlreadyCurrent =>
                    CruiseObservationRecordStatus.AlreadyCurrent,
                _ => throw new InvalidOperationException("Unknown repository record state.")
            };
            return CruiseObservationRecordResult.Recorded(
                status,
                summary,
                repositoryResult.History.LastSeenAt);
        }
        catch (OperationCanceledException)
        {
            return CruiseObservationRecordResult.Cancelled();
        }
        catch (Exception)
        {
            return CruiseObservationRecordResult.Failed();
        }
    }
}
