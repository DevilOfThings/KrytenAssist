namespace KrytenAssist.Infrastructure.Persistence;

public sealed class CruisePreferenceProfileEntity
{
    public int Id { get; set; }
    public decimal? MaximumBudgetAmount { get; set; }
    public string? MaximumBudgetCurrency { get; set; }
    public int? MaximumBudgetBasis { get; set; }
    public List<CruisePreferenceMonthEntity> Months { get; set; } = [];
    public List<CruisePreferenceCabinEntity> Cabins { get; set; } = [];
}
