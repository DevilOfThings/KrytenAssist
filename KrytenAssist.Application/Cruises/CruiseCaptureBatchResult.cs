namespace KrytenAssist.Application.Cruises;

public sealed record CruiseCaptureBatchResult
{
    public const int MaximumCandidateCount = 10;
    public const int MaximumMessageLength = 1_000;

    private CruiseCaptureBatchResult(
        CruiseCaptureBatchStatus status,
        IReadOnlyList<CruiseCaptureCandidateResult> candidates,
        string? message,
        bool wasTruncated)
    {
        Status = status;
        Candidates = candidates;
        Message = message;
        WasTruncated = wasTruncated;
        ReadyCount = candidates.Count(candidate =>
            candidate.Status == CruiseCaptureCandidateStatus.Ready);
        IncompleteCount = candidates.Count(candidate =>
            candidate.Status == CruiseCaptureCandidateStatus.Incomplete);
        FailedCount = candidates.Count(candidate =>
            candidate.Status == CruiseCaptureCandidateStatus.Failed);
    }

    public CruiseCaptureBatchStatus Status { get; }

    public IReadOnlyList<CruiseCaptureCandidateResult> Candidates { get; }

    public string? Message { get; }

    public bool WasTruncated { get; }

    public bool IsCompleted => Status == CruiseCaptureBatchStatus.Completed;

    public int ReadyCount { get; }

    public int IncompleteCount { get; }

    public int FailedCount { get; }

    public static CruiseCaptureBatchResult Completed(
        IEnumerable<CruiseCaptureCandidateResult> candidates,
        bool wasTruncated = false)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var items = candidates.ToList();
        if (items.Count == 0 || items.Count > MaximumCandidateCount)
        {
            throw new ArgumentException(
                $"Candidates must contain between 1 and {MaximumCandidateCount} values.",
                nameof(candidates));
        }

        if (items.Any(candidate => candidate is null))
        {
            throw new ArgumentException(
                "Candidates cannot contain null values.",
                nameof(candidates));
        }

        if (items
            .Select(candidate => candidate.SourceReference)
            .Distinct(StringComparer.Ordinal)
            .Count() != items.Count)
        {
            throw new ArgumentException(
                "Candidate source references must be distinct.",
                nameof(candidates));
        }

        return new CruiseCaptureBatchResult(
            CruiseCaptureBatchStatus.Completed,
            items.AsReadOnly(),
            null,
            wasTruncated);
    }

    public static CruiseCaptureBatchResult Incomplete(string message) =>
        Failure(CruiseCaptureBatchStatus.Incomplete, message);

    public static CruiseCaptureBatchResult Unsupported(string message) =>
        Failure(CruiseCaptureBatchStatus.Unsupported, message);

    public static CruiseCaptureBatchResult Failed(string message) =>
        Failure(CruiseCaptureBatchStatus.Failed, message);

    public static CruiseCaptureBatchResult Cancelled(string message) =>
        Failure(CruiseCaptureBatchStatus.Cancelled, message);

    private static CruiseCaptureBatchResult Failure(
        CruiseCaptureBatchStatus status,
        string message) =>
        new(
            status,
            Array.Empty<CruiseCaptureCandidateResult>(),
            ValidateMessage(message),
            false);

    private static string ValidateMessage(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (message.Length > MaximumMessageLength)
        {
            throw new ArgumentException(
                $"The message cannot exceed {MaximumMessageLength} characters.",
                nameof(message));
        }

        return message;
    }
}
