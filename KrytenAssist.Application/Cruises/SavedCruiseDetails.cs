using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed record SavedCruiseDetails
{
    public SavedCruiseDetails(
        SavedCruise savedCruise,
        bool isFavouriteShip,
        IEnumerable<CruiseHistoryDetails>? histories = null)
    {
        ArgumentNullException.ThrowIfNull(savedCruise);
        var matchingHistories = (histories ?? [])
            .Select(history => history ?? throw new ArgumentException("History cannot contain null values.", nameof(histories)))
            .Where(history => history.History.SailingKey == savedCruise.SailingKey)
            .OrderByDescending(history => history.LastSeenAt)
            .ThenBy(history => history.History.Source?.Id ?? string.Empty, StringComparer.Ordinal)
            .ToArray();

        SavedCruise = savedCruise;
        IsFavouriteShip = isFavouriteShip;
        Histories = Array.AsReadOnly(matchingHistories);
        LatestRecordedObservation = matchingHistories
            .SelectMany(history => history.History.Observations)
            .OrderByDescending(observation => observation.ObservedAt)
            .ThenByDescending(CruiseObservationFingerprint.From)
            .FirstOrDefault();
    }

    public SavedCruise SavedCruise { get; }
    public bool IsFavouriteShip { get; }
    public IReadOnlyList<CruiseHistoryDetails> Histories { get; }
    public CruiseObservation? LatestRecordedObservation { get; }
    public bool HasRecordedHistory => Histories.Count > 0;
    public int RecordedSourceCount => Histories.Count;
    public int RecordedObservationCount => Histories.Sum(history => history.Summary.ObservationCount);
}
