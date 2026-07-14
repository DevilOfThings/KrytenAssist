namespace KrytenAssist.Application.Cruises;

public sealed class CruiseOfTheWeekException : Exception
{
    public CruiseOfTheWeekException(string message)
        : base(message)
    {
    }

    public CruiseOfTheWeekException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
