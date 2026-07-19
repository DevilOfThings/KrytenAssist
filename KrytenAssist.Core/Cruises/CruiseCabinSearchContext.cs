using System.Globalization;

namespace KrytenAssist.Core.Cruises;

public enum CruiseCabinPackageMode
{
    Unknown,
    FlyCruise,
    CruiseOnly,
    CruiseAndStay
}

public sealed class CruiseCabinSearchContext : IEquatable<CruiseCabinSearchContext>
{
    public const int MaximumPartySize = 32;
    public const int MaximumCabinQuantity = 16;
    public const int MaximumAirportIdLength = 100;

    public CruiseCabinSearchContext(
        int? adultCount = null,
        int? childCount = null,
        IEnumerable<int>? childAges = null,
        bool childAgesKnown = false,
        CruiseCabinPackageMode packageMode = CruiseCabinPackageMode.Unknown,
        string? departureAirportId = null,
        int? cabinQuantity = null)
    {
        if (adultCount is < 0 or > MaximumPartySize)
            throw new ArgumentOutOfRangeException(nameof(adultCount));
        if (childCount is < 0 or > MaximumPartySize)
            throw new ArgumentOutOfRangeException(nameof(childCount));
        if (cabinQuantity is < 1 or > MaximumCabinQuantity)
            throw new ArgumentOutOfRangeException(nameof(cabinQuantity));
        if (!Enum.IsDefined(packageMode))
            throw new ArgumentOutOfRangeException(nameof(packageMode));

        var ages = childAges?.ToArray() ?? [];
        if (!childAgesKnown && ages.Length > 0)
            throw new ArgumentException("Child ages cannot be supplied when they are unknown.", nameof(childAges));
        if (childAgesKnown && childCount is null)
            throw new ArgumentException("Known child ages require a known child count.", nameof(childAgesKnown));
        if (childAgesKnown && ages.Length != childCount)
            throw new ArgumentException("Known child ages must match the child count.", nameof(childAges));
        if (ages.Any(age => age is < 0 or > 17))
            throw new ArgumentOutOfRangeException(nameof(childAges));

        AdultCount = adultCount;
        ChildCount = childCount;
        ChildAgesKnown = childAgesKnown;
        ChildAges = Array.AsReadOnly(ages);
        PackageMode = packageMode;
        DepartureAirportId = NormalizeAirport(departureAirportId);
        CabinQuantity = cabinQuantity;
    }

    public int? AdultCount { get; }
    public int? ChildCount { get; }
    public bool ChildAgesKnown { get; }
    public IReadOnlyList<int> ChildAges { get; }
    public CruiseCabinPackageMode PackageMode { get; }
    public string? DepartureAirportId { get; }
    public int? CabinQuantity { get; }

    public string Fingerprint => CruiseAlertSettings.Hash(string.Join('|',
        "cabin-context:v1",
        Number(AdultCount),
        Number(ChildCount),
        ChildAgesKnown ? string.Join(',', ChildAges.Select(age => age.ToString(CultureInfo.InvariantCulture))) : "?",
        ((int)PackageMode).ToString(CultureInfo.InvariantCulture),
        DepartureAirportId ?? "?",
        Number(CabinQuantity)));

    public bool Equals(CruiseCabinSearchContext? other) => other is not null &&
        AdultCount == other.AdultCount && ChildCount == other.ChildCount &&
        ChildAgesKnown == other.ChildAgesKnown && ChildAges.SequenceEqual(other.ChildAges) &&
        PackageMode == other.PackageMode && DepartureAirportId == other.DepartureAirportId &&
        CabinQuantity == other.CabinQuantity;

    public override bool Equals(object? obj) => Equals(obj as CruiseCabinSearchContext);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Fingerprint);

    private static string Number(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "?";

    private static string? NormalizeAirport(string? value)
    {
        var normalized = CruiseHistoryText.NormalizeOptional(value);
        if (normalized?.Length > MaximumAirportIdLength)
            throw new ArgumentException("Departure airport identity is too long.", nameof(value));
        return normalized;
    }
}
