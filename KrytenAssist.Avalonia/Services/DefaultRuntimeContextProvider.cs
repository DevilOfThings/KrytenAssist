using System;
using KrytenAssist.Avalonia.Tools;

namespace KrytenAssist.Avalonia.Services;

public sealed class DefaultRuntimeContextProvider : IRuntimeContextProvider
{
    private readonly IClock _clock;

    public DefaultRuntimeContextProvider(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        
        _clock = clock;
    }

    public string GetRuntimeContext()
    {
        var now = _clock.Now;

        return $"""
                Current Runtime Context

                CurrentDate: {now:dd MMMM yyyy}
                CurrentTime: {now:HH:mm}
                TimeZone: {TimeZoneInfo.Local.Id}
                """;
    }
}