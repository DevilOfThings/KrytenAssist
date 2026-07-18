using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed class SavedCruiseSnapshotFactory
{
    public SavedCruiseSnapshot Create(CruiseObservation observation, DateTimeOffset savedAt)
    {
        ArgumentNullException.ThrowIfNull(observation);
        var offer = observation.Snapshot.Offer;
        var price = observation.Snapshot.Prices.FirstOrDefault()
            ?? throw new ArgumentException("A saved cruise requires an advertised price.", nameof(observation));
        return new SavedCruiseSnapshot(
            CruiseSailingKey.From(observation), offer.Title, offer.Provider.Name,
            price, savedAt, offer.DeparturePort, offer.ItinerarySummary,
            observation.Source, observation.SourceReference);
    }
}
