using GameStore.Catalog.Domain.Common;
using GameStore.Catalog.Domain.Exceptions;

namespace GameStore.Catalog.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency = "BRL")
    {
        Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant();
    }

    public static Money Create(decimal amount, string currency = "BRL")
    {
        if (amount < 0)
            throw new ValidationException(nameof(Money), "O valor não pode ser negativo.");

        if (string.IsNullOrWhiteSpace(currency))
            currency = "BRL";

        return new Money(amount, currency);
    }

    public static Money Zero(string currency = "BRL") => new(0, currency);

    public Money ApplyDiscount(decimal discountPercentage)
    {
        if (discountPercentage < 0 || discountPercentage > 100)
            throw new ValidationException("O percentual de desconto deve estar entre 0 e 100.");

        var discounted = Amount * (1 - (discountPercentage / 100m));
        return Create(discounted, Currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Currency} {Amount:N2}";

    public static implicit operator decimal(Money money) => money.Amount;
}

