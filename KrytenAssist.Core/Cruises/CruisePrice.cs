namespace KrytenAssist.Core.Cruises;

public sealed record CruisePrice
{
    public CruisePrice(decimal amount, string currency, string? basis = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ArgumentNullException.ThrowIfNull(currency);

        if (currency.Length != 3 || !currency.All(char.IsAsciiLetter))
        {
            throw new ArgumentException(
                "Currency must contain exactly three alphabetic characters.",
                nameof(currency));
        }

        if (basis is not null && string.IsNullOrWhiteSpace(basis))
        {
            throw new ArgumentException(
                "Price basis cannot be empty or whitespace.",
                nameof(basis));
        }

        Amount = amount;
        Currency = currency.ToUpperInvariant();
        Basis = basis;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public string? Basis { get; }
}
