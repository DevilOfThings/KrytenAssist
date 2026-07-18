namespace KrytenAssist.Core.Cruises;

public sealed class CruisePreferences : IEquatable<CruisePreferences>
{
    public CruisePreferences(
        IEnumerable<int>? departureMonths = null,
        IEnumerable<CruiseCabinType>? preferredCabins = null,
        CruiseBudget? maximumBudget = null)
    {
        var months = (departureMonths ?? []).Distinct().Order().ToArray();
        if (months.Any(month => month is < 1 or > 12))
            throw new ArgumentOutOfRangeException(nameof(departureMonths), "Departure months must be between 1 and 12.");

        var cabins = (preferredCabins ?? []).Distinct().Order().ToArray();
        if (cabins.Any(cabin => !Enum.IsDefined(cabin)))
            throw new ArgumentOutOfRangeException(nameof(preferredCabins));

        DepartureMonths = Array.AsReadOnly(months);
        PreferredCabins = Array.AsReadOnly(cabins);
        MaximumBudget = maximumBudget;
    }

    public IReadOnlyList<int> DepartureMonths { get; }
    public IReadOnlyList<CruiseCabinType> PreferredCabins { get; }
    public CruiseBudget? MaximumBudget { get; }

    public bool Equals(CruisePreferences? other) => other is not null
        && DepartureMonths.SequenceEqual(other.DepartureMonths)
        && PreferredCabins.SequenceEqual(other.PreferredCabins)
        && MaximumBudget == other.MaximumBudget;

    public override bool Equals(object? obj) => Equals(obj as CruisePreferences);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var month in DepartureMonths) hash.Add(month);
        foreach (var cabin in PreferredCabins) hash.Add(cabin);
        hash.Add(MaximumBudget);
        return hash.ToHashCode();
    }
}
