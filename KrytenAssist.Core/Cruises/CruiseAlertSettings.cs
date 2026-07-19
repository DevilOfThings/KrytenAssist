using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace KrytenAssist.Core.Cruises;

public sealed record CruiseAlertSettings
{
    public CruiseAlertSettings(
        bool priceDropEnabled = true,
        bool promotionEnabled = true,
        bool savedCriteriaEnabled = true,
        decimal minimumPriceDropPercentage = 0,
        bool cabinAvailabilityEnabled = true)
    {
        if (minimumPriceDropPercentage is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(minimumPriceDropPercentage));
        PriceDropEnabled = priceDropEnabled;
        PromotionEnabled = promotionEnabled;
        SavedCriteriaEnabled = savedCriteriaEnabled;
        MinimumPriceDropPercentage = minimumPriceDropPercentage;
        CabinAvailabilityEnabled = cabinAvailabilityEnabled;
    }

    public bool PriceDropEnabled { get; }
    public bool PromotionEnabled { get; }
    public bool SavedCriteriaEnabled { get; }
    public decimal MinimumPriceDropPercentage { get; }
    public bool CabinAvailabilityEnabled { get; }
    public string Fingerprint => Hash($"alert-settings:v2|{PriceDropEnabled}|{PromotionEnabled}|{SavedCriteriaEnabled}|{MinimumPriceDropPercentage.ToString("G29", CultureInfo.InvariantCulture)}|{CabinAvailabilityEnabled}");

    internal static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
