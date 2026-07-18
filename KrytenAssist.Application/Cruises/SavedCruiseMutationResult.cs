using KrytenAssist.Core.Cruises;

namespace KrytenAssist.Application.Cruises;

public sealed record SavedCruiseMutationResult
{
    private SavedCruiseMutationResult(SavedCruiseMutationStatus status, SavedCruise? savedCruise, string? message)
    {
        Status = status;
        SavedCruise = savedCruise;
        Message = message;
    }

    public SavedCruiseMutationStatus Status { get; }
    public SavedCruise? SavedCruise { get; }
    public string? Message { get; }

    public static SavedCruiseMutationResult Success(SavedCruiseMutationStatus status, SavedCruise? savedCruise = null)
    {
        if (status is SavedCruiseMutationStatus.Cancelled or SavedCruiseMutationStatus.Failed or SavedCruiseMutationStatus.NotFound)
            throw new ArgumentOutOfRangeException(nameof(status));
        return new(status, savedCruise, null);
    }

    public static SavedCruiseMutationResult NotFound() => new(SavedCruiseMutationStatus.NotFound, null, null);
    public static SavedCruiseMutationResult Cancelled() => new(SavedCruiseMutationStatus.Cancelled, null, "The saved-cruise change was cancelled.");
    public static SavedCruiseMutationResult Failed() => new(SavedCruiseMutationStatus.Failed, null, "The saved-cruise change could not be completed locally.");
}
