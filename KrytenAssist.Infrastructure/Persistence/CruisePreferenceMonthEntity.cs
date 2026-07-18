namespace KrytenAssist.Infrastructure.Persistence;
public sealed class CruisePreferenceMonthEntity { public long Id { get; set; } public int ProfileId { get; set; } public int Month { get; set; } public CruisePreferenceProfileEntity Profile { get; set; } = null!; }
