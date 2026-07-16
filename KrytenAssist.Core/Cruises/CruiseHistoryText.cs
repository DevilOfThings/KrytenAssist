namespace KrytenAssist.Core.Cruises;

internal static class CruiseHistoryText
{
    internal static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return Normalize(value);
    }

    internal static string Normalize(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();

    internal static string? NormalizeOptional(string? value) =>
        value is null ? null : Normalize(value);

    internal static string Component(string? value) =>
        value is null ? "-" : $"{value.Length}:{value}";
}
