using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public enum SavedCruiseQueryStatus { Found, NotFound, Cancelled, Failed }
public sealed record SavedCruiseQueryResult(SavedCruiseQueryStatus Status, SavedCruise? SavedCruise, string? Message)
{
    public static SavedCruiseQueryResult Found(SavedCruise value) => new(SavedCruiseQueryStatus.Found, value ?? throw new ArgumentNullException(nameof(value)), null);
    public static SavedCruiseQueryResult NotFound() => new(SavedCruiseQueryStatus.NotFound, null, null);
    public static SavedCruiseQueryResult Cancelled() => new(SavedCruiseQueryStatus.Cancelled, null, "Loading the saved cruise was cancelled.");
    public static SavedCruiseQueryResult Failed() => new(SavedCruiseQueryStatus.Failed, null, "The saved cruise could not be loaded locally.");
}
