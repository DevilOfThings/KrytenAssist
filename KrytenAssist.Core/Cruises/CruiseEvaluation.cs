namespace KrytenAssist.Core.Cruises;

public sealed record CruiseEvaluation
{
    public const int MaximumNotesLength = 4000;
    public static CruiseEvaluation Empty { get; } = new();

    public CruiseEvaluation(
        CruiseInterestLevel? interestLevel = null,
        int? overallRating = null,
        int? itineraryRating = null,
        int? shipRating = null,
        int? valueRating = null,
        string? notes = null)
    {
        if (interestLevel is not null && !Enum.IsDefined(interestLevel.Value))
            throw new ArgumentOutOfRangeException(nameof(interestLevel));
        ValidateRating(overallRating, nameof(overallRating));
        ValidateRating(itineraryRating, nameof(itineraryRating));
        ValidateRating(shipRating, nameof(shipRating));
        ValidateRating(valueRating, nameof(valueRating));

        var normalizedNotes = NormalizeOptional(notes);
        if (normalizedNotes?.Length > MaximumNotesLength)
            throw new ArgumentException($"Notes cannot exceed {MaximumNotesLength} characters.", nameof(notes));

        InterestLevel = interestLevel;
        OverallRating = overallRating;
        ItineraryRating = itineraryRating;
        ShipRating = shipRating;
        ValueRating = valueRating;
        Notes = normalizedNotes;
    }

    public CruiseInterestLevel? InterestLevel { get; }
    public int? OverallRating { get; }
    public int? ItineraryRating { get; }
    public int? ShipRating { get; }
    public int? ValueRating { get; }
    public string? Notes { get; }

    private static void ValidateRating(int? rating, string parameterName)
    {
        if (rating is < 1 or > 5)
            throw new ArgumentOutOfRangeException(parameterName, rating, "Ratings must be between 1 and 5.");
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
