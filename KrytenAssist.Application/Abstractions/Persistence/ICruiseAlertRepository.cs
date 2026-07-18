using KrytenAssist.Application.Cruises;
using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Abstractions.Persistence;

public interface ICruiseAlertRepository
{
    Task<CruiseAlert?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CruiseAlert>> ListAsync(CruiseAlertQuery query, CancellationToken cancellationToken = default);
    Task<int> CountUnreadAsync(CancellationToken cancellationToken = default);
    Task<CruiseAlertAddRepositoryResult> AddIfAbsentAsync(CruiseAlert alert, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(Guid id, CruiseAlertStatus status, CancellationToken cancellationToken = default);
}

public interface ICruiseAlertSettingsRepository
{
    Task<CruiseAlertSettings> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(CruiseAlertSettings settings, CancellationToken cancellationToken = default);
}

public interface ISavedCruiseCriteriaStateRepository
{
    Task<SavedCruiseCriteriaEvaluationState?> GetAsync(CruiseSailingKey sailingKey, string criteriaFingerprint, CancellationToken cancellationToken = default);
    Task UpsertAsync(SavedCruiseCriteriaEvaluationState state, CancellationToken cancellationToken = default);
}
