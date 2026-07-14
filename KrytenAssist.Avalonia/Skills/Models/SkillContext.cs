using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace KrytenAssist.Avalonia.Skills.Models;

public sealed record SkillContext
{
    private static readonly IReadOnlyDictionary<string, object?> EmptyValues =
        new ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?>());

    public SkillContext(DateTimeOffset requestedAt)
        : this(requestedAt, EmptyValues)
    {
    }

    public SkillContext(
        DateTimeOffset requestedAt,
        IReadOnlyDictionary<string, object?> values)
    {
        RequestedAt = requestedAt;
        Values = values.Count == 0
            ? EmptyValues
            : new ReadOnlyDictionary<string, object?>(
                new Dictionary<string, object?>(values));
    }

    public DateTimeOffset RequestedAt { get; }

    public IReadOnlyDictionary<string, object?> Values { get; }
}
