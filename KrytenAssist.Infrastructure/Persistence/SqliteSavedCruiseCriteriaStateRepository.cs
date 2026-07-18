using KrytenAssist.Application.Abstractions.Persistence;
using KrytenAssist.Core.Cruises;
using Microsoft.EntityFrameworkCore;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class SqliteSavedCruiseCriteriaStateRepository(KrytenAssistDbContext dbContext) : ISavedCruiseCriteriaStateRepository
{
    public async Task<SavedCruiseCriteriaEvaluationState?> GetAsync(CruiseSailingKey sailingKey, string criteriaFingerprint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sailingKey); ArgumentException.ThrowIfNullOrWhiteSpace(criteriaFingerprint); cancellationToken.ThrowIfCancellationRequested();
        var entity = await Query(sailingKey, criteriaFingerprint).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task UpsertAsync(SavedCruiseCriteriaEvaluationState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state); cancellationToken.ThrowIfCancellationRequested();
        var key = state.SailingKey;
        var departure = key.DepartureDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var evidenceTime = state.EvidenceTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "SavedCruiseCriteriaEvaluationStates"
              ("OperatorId", "ShipName", "DepartureDate", "DurationNights", "CriteriaFingerprint", "EvidenceKey", "EvidenceTime", "EvidenceTimeUtcTicks", "Result")
            VALUES ({key.OperatorId}, {key.ShipName}, {departure}, {key.DurationNights}, {state.CriteriaFingerprint}, {state.EvidenceKey}, {evidenceTime}, {state.EvidenceTime.UtcTicks}, {(int)state.Result})
            ON CONFLICT("OperatorId", "ShipName", "DepartureDate", "DurationNights", "CriteriaFingerprint") DO UPDATE SET
              "EvidenceKey" = excluded."EvidenceKey", "EvidenceTime" = excluded."EvidenceTime",
              "EvidenceTimeUtcTicks" = excluded."EvidenceTimeUtcTicks", "Result" = excluded."Result"
            WHERE excluded."EvidenceTimeUtcTicks" > "SavedCruiseCriteriaEvaluationStates"."EvidenceTimeUtcTicks"
               OR (excluded."EvidenceTimeUtcTicks" = "SavedCruiseCriteriaEvaluationStates"."EvidenceTimeUtcTicks"
                   AND excluded."EvidenceKey" > "SavedCruiseCriteriaEvaluationStates"."EvidenceKey")
            """, cancellationToken);
    }

    private IQueryable<SavedCruiseCriteriaEvaluationStateEntity> Query(CruiseSailingKey key, string fingerprint) =>
        dbContext.SavedCruiseCriteriaEvaluationStates.Where(x => x.OperatorId == key.OperatorId && x.ShipName == key.ShipName && x.DepartureDate == key.DepartureDate && x.DurationNights == key.DurationNights && x.CriteriaFingerprint == fingerprint);
    private static SavedCruiseCriteriaEvaluationState Map(SavedCruiseCriteriaEvaluationStateEntity x) => new(new(x.OperatorId, x.ShipName, x.DepartureDate, x.DurationNights), x.CriteriaFingerprint, x.EvidenceKey, x.EvidenceTime, (SavedCruiseCriteriaResult)x.Result);
}
