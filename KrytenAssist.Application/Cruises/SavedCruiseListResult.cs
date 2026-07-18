using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public enum SavedCruiseListStatus { Success, Cancelled, Failed }
public sealed record SavedCruiseListResult(SavedCruiseListStatus Status, IReadOnlyList<SavedCruise> SavedCruises, string? Message)
{
    public static SavedCruiseListResult Success(IEnumerable<SavedCruise> values) => new(SavedCruiseListStatus.Success, Array.AsReadOnly((values ?? throw new ArgumentNullException(nameof(values))).ToArray()), null);
    public static SavedCruiseListResult Cancelled() => new(SavedCruiseListStatus.Cancelled, [], "Loading saved cruises was cancelled.");
    public static SavedCruiseListResult Failed() => new(SavedCruiseListStatus.Failed, [], "Saved cruises could not be loaded locally.");
}
