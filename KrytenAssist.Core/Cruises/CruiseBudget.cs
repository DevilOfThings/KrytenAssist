namespace KrytenAssist.Core.Cruises;

public sealed record CruiseBudget
{
    public CruiseBudget(decimal amount, string currency, CruiseBudgetBasis basis)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ArgumentNullException.ThrowIfNull(currency);
        if (currency.Length != 3 || !currency.All(char.IsAsciiLetter))
            throw new ArgumentException("Currency must contain exactly three alphabetic characters.", nameof(currency));
        if (!Enum.IsDefined(basis))
            throw new ArgumentOutOfRangeException(nameof(basis));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
        Basis = basis;
    }

    public decimal Amount { get; }
    public string Currency { get; }
    public CruiseBudgetBasis Basis { get; }
}
