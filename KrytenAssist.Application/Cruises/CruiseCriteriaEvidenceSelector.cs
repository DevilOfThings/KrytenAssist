using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed class CruiseCriteriaEvidenceSelector
{
    public CruiseCriteriaEvidence Select(
        SavedCruise savedCruise,
        IEnumerable<CruiseRecordedHistory> histories)
    {
        ArgumentNullException.ThrowIfNull(savedCruise);
        ArgumentNullException.ThrowIfNull(histories);

        var latest = histories
            .Where(history => history.SailingKey == savedCruise.SailingKey)
            .SelectMany(history => history.Observations)
            .Select(observation => new
            {
                Observation = observation,
                Fingerprint = CruiseObservationFingerprint.From(observation)
            })
            .OrderByDescending(item => item.Observation.ObservedAt)
            .ThenByDescending(item => item.Fingerprint.PersistenceKey, StringComparer.Ordinal)
            .FirstOrDefault();

        if (latest is not null)
        {
            return new CruiseCriteriaEvidence(
                CruiseAlertEvidenceOrigin.RecordedObservation,
                latest.Fingerprint.PersistenceKey,
                latest.Observation.ObservedAt,
                latest.Observation.Snapshot.Prices);
        }

        var snapshot = savedCruise.Snapshot;
        return new CruiseCriteriaEvidence(
            CruiseAlertEvidenceOrigin.SavedSnapshot,
            SavedSnapshotEvidenceKey(snapshot),
            snapshot.SavedAt,
            [snapshot.DisplayedPrice]);
    }

    private static string SavedSnapshotEvidenceKey(SavedCruiseSnapshot snapshot)
    {
        var evidence = string.Join(
            '|',
            "saved-snapshot-evidence:v1",
            snapshot.SailingKey.OperatorId,
            snapshot.SailingKey.ShipName,
            snapshot.SailingKey.DepartureDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            snapshot.SailingKey.DurationNights,
            snapshot.DisplayedPrice.Amount.ToString("G29", System.Globalization.CultureInfo.InvariantCulture),
            snapshot.DisplayedPrice.Currency,
            NormalizeOptional(snapshot.DisplayedPrice.Basis) ?? "-",
            snapshot.SavedAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        return CruiseAlertEventKey.Create(
            CruiseAlertType.SavedCriteria,
            snapshot.SailingKey,
            null,
            evidence);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
}
