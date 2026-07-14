using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace KrytenAssist.Avalonia.Skills.Models;

public sealed record SkillRequest
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyParameters =
        new ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?>());

    public SkillRequest(string operation)
        : this(operation, EmptyParameters)
    {
    }

    public SkillRequest(
        string operation,
        IReadOnlyDictionary<string, object?> parameters)
    {
        Operation = operation;
        Parameters = parameters.Count == 0
            ? EmptyParameters
            : new ReadOnlyDictionary<string, object?>(
                new Dictionary<string, object?>(parameters));
    }

    public string Operation { get; }

    public IReadOnlyDictionary<string, object?> Parameters { get; }
}
