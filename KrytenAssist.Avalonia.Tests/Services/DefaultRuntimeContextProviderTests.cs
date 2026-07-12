using KrytenAssist.Avalonia.Services;
using KrytenAssist.Avalonia.Tools;

namespace KrytenAssist.Avalonia.Tests.Services;

public sealed class DefaultRuntimeContextProviderTests
{
    [Fact]
    public void GetRuntimeContext_ReturnsCurrentDateTimeAndTimeZone()
    {
        var clock = new TestClock(
            new DateTimeOffset(
                year: 2026,
                month: 7,
                day: 12,
                hour: 14,
                minute: 35,
                second: 0,
                offset: TimeSpan.FromHours(1)));

        var provider = new DefaultRuntimeContextProvider(clock);

        var result = provider.GetRuntimeContext();

        Assert.Contains("Current Runtime Context", result);
        Assert.Contains("CurrentDate: 12 July 2026", result);
        Assert.Contains("CurrentTime: 14:35", result);
        Assert.Contains($"TimeZone: {TimeZoneInfo.Local.Id}", result);
    }

    [Fact]
    public void GetRuntimeContext_UsesCurrentClockValueForEachCall()
    {
        var clock = new TestClock(
            new DateTimeOffset(
                year: 2026,
                month: 7,
                day: 12,
                hour: 14,
                minute: 35,
                second: 0,
                offset: TimeSpan.FromHours(1)));

        var provider = new DefaultRuntimeContextProvider(clock);

        var firstResult = provider.GetRuntimeContext();

        clock.Now = new DateTimeOffset(
            year: 2026,
            month: 7,
            day: 13,
            hour: 8,
            minute: 10,
            second: 0,
            offset: TimeSpan.FromHours(1));

        var secondResult = provider.GetRuntimeContext();

        Assert.Contains("CurrentDate: 12 July 2026", firstResult);
        Assert.Contains("CurrentTime: 14:35", firstResult);

        Assert.Contains("CurrentDate: 13 July 2026", secondResult);
        Assert.Contains("CurrentTime: 08:10", secondResult);
    }
    
    private sealed class TestClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset Now { get; set; } = now;
    }
    
}