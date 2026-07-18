namespace KrytenAssist.Infrastructure.Persistence;
public sealed class CruisePreferenceCabinEntity { public long Id { get; set; } public int ProfileId { get; set; } public int Cabin { get; set; } public CruisePreferenceProfileEntity Profile { get; set; } = null!; }
