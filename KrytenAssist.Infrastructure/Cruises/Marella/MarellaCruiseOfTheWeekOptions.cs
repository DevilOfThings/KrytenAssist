namespace KrytenAssist.Infrastructure.Cruises.Marella;

public sealed class MarellaCruiseOfTheWeekOptions
{
    public string SourceUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; }
}
