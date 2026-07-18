using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public enum PersonalCruisePreferenceMutationStatus { Updated, Unchanged, Cancelled, Failed }
public sealed record PersonalCruisePreferenceMutationResult(PersonalCruisePreferenceMutationStatus Status, string? Message)
{
    public static PersonalCruisePreferenceMutationResult Updated() => new(PersonalCruisePreferenceMutationStatus.Updated, null);
    public static PersonalCruisePreferenceMutationResult Unchanged() => new(PersonalCruisePreferenceMutationStatus.Unchanged, null);
    public static PersonalCruisePreferenceMutationResult Cancelled() => new(PersonalCruisePreferenceMutationStatus.Cancelled, "The preference change was cancelled.");
    public static PersonalCruisePreferenceMutationResult Failed() => new(PersonalCruisePreferenceMutationStatus.Failed, "The preference change could not be completed locally.");
}

public enum PersonalCruisePreferenceQueryStatus { Success, Cancelled, Failed }
public sealed record FavouriteCruiseShipListResult(PersonalCruisePreferenceQueryStatus Status, IReadOnlyList<CruiseShipKey> Ships, string? Message)
{
    public static FavouriteCruiseShipListResult Success(IEnumerable<CruiseShipKey> ships) => new(PersonalCruisePreferenceQueryStatus.Success, Array.AsReadOnly((ships ?? throw new ArgumentNullException(nameof(ships))).ToArray()), null);
    public static FavouriteCruiseShipListResult Cancelled() => new(PersonalCruisePreferenceQueryStatus.Cancelled, [], "Loading favourite ships was cancelled.");
    public static FavouriteCruiseShipListResult Failed() => new(PersonalCruisePreferenceQueryStatus.Failed, [], "Favourite ships could not be loaded locally.");
}

public sealed record CruisePreferencesQueryResult(PersonalCruisePreferenceQueryStatus Status, CruisePreferences? Preferences, string? Message)
{
    public static CruisePreferencesQueryResult Success(CruisePreferences preferences) => new(PersonalCruisePreferenceQueryStatus.Success, preferences ?? throw new ArgumentNullException(nameof(preferences)), null);
    public static CruisePreferencesQueryResult Cancelled() => new(PersonalCruisePreferenceQueryStatus.Cancelled, null, "Loading cruise preferences was cancelled.");
    public static CruisePreferencesQueryResult Failed() => new(PersonalCruisePreferenceQueryStatus.Failed, null, "Cruise preferences could not be loaded locally.");
}
