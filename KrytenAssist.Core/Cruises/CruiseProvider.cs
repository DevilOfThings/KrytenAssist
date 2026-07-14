namespace KrytenAssist.Core.Cruises;

public sealed record CruiseProvider
{
    public CruiseProvider(string id, string name)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Name = name;
    }

    public string Id { get; }

    public string Name { get; }
}
