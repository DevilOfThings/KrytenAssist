using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KrytenAssist.Infrastructure.Persistence;

internal static class CruisePersistenceConversions
{
    public static readonly ValueConverter<DateOnly, string> DateOnly = new(
        value => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        value => System.DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture));

    public static readonly ValueConverter<DateTimeOffset, string> DateTimeOffset = new(
        value => value.ToString("O", CultureInfo.InvariantCulture),
        value => System.DateTimeOffset.ParseExact(value, "O", CultureInfo.InvariantCulture));

    public static readonly ValueConverter<DateTimeOffset?, string?> NullableDateTimeOffset = new(
        value => value.HasValue ? value.Value.ToString("O", CultureInfo.InvariantCulture) : null,
        value => value == null ? null : System.DateTimeOffset.ParseExact(value, "O", CultureInfo.InvariantCulture));

    public static readonly ValueConverter<decimal, string> Decimal = new(
        value => value.ToString("G29", CultureInfo.InvariantCulture),
        value => decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture));

    public static readonly ValueConverter<decimal?, string?> NullableDecimal = new(
        value => value.HasValue ? value.Value.ToString("G29", CultureInfo.InvariantCulture) : null,
        value => value == null ? null : decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture));
}
