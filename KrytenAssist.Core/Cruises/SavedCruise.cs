namespace KrytenAssist.Core.Cruises;

public sealed record SavedCruise
{
    public SavedCruise(
        SavedCruiseSnapshot snapshot,
        SavedCruiseStatus status = SavedCruiseStatus.Shortlisted,
        CruiseEvaluation? evaluation = null,
        bool isFavourite = false)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        Snapshot = snapshot;
        Status = status;
        Evaluation = evaluation ?? CruiseEvaluation.Empty;
        IsFavourite = isFavourite;
    }

    public CruiseSailingKey SailingKey => Snapshot.SailingKey;
    public SavedCruiseSnapshot Snapshot { get; }
    public SavedCruiseStatus Status { get; }
    public CruiseEvaluation Evaluation { get; }
    public bool IsFavourite { get; }

    public SavedCruise RefreshSnapshot(SavedCruiseSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.SailingKey != SailingKey)
            throw new ArgumentException("A saved snapshot must describe the same sailing.", nameof(snapshot));
        return new(snapshot, Status, Evaluation, IsFavourite);
    }

    public SavedCruise WithEvaluation(CruiseEvaluation evaluation) =>
        new(Snapshot, Status, evaluation ?? throw new ArgumentNullException(nameof(evaluation)), IsFavourite);

    public SavedCruise WithStatus(SavedCruiseStatus status) =>
        new(Snapshot, status, Evaluation, IsFavourite);

    public SavedCruise WithFavourite(bool isFavourite) =>
        new(Snapshot, Status, Evaluation, isFavourite);
}
