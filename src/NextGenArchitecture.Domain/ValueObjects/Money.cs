using NextGenArchitecture.SharedKernel.Common;

namespace NextGenArchitecture.Domain.ValueObjects;

/// <summary>
/// Represents a monetary value with currency information.
/// Provides precise decimal arithmetic for financial calculations and prevents currency mixing errors.
/// </summary>
public sealed class Money : ValueObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Money"/> class.
    /// </summary>
    /// <param name="amount">The monetary amount.</param>
    /// <param name="currency">The currency code (ISO 4217).</param>
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Gets the monetary amount.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// Gets the currency code (ISO 4217).
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// Gets a value indicating whether this money instance has zero amount.
    /// </summary>
    public bool IsZero => Amount == 0;

    /// <summary>
    /// Gets a value indicating whether this money instance has a positive amount.
    /// </summary>
    public bool IsPositive => Amount > 0;

    /// <summary>
    /// Gets a value indicating whether this money instance has a negative amount.
    /// </summary>
    public bool IsNegative => Amount < 0;

    /// <summary>
    /// Creates a new Money instance.
    /// </summary>
    /// <param name="amount">The monetary amount.</param>
    /// <param name="currency">The currency code (ISO 4217).</param>
    /// <returns>A new Money instance.</returns>
    /// <exception cref="ArgumentException">Thrown when currency is invalid.</exception>
    public static Money Create(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be null or empty.", nameof(currency));

        var normalizedCurrency = currency.Trim().ToUpperInvariant();

        if (normalizedCurrency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO 4217 code.", nameof(currency));

        return new Money(amount, normalizedCurrency);
    }

    /// <summary>
    /// Creates a zero Money instance for the specified currency.
    /// </summary>
    /// <param name="currency">The currency code (ISO 4217).</param>
    /// <returns>A Money instance with zero amount.</returns>
    public static Money Zero(string currency)
    {
        return Create(0, currency);
    }

    /// <summary>
    /// Adds two Money instances.
    /// </summary>
    /// <param name="other">The Money instance to add.</param>
    /// <returns>A new Money instance with the sum.</returns>
    /// <exception cref="InvalidOperationException">Thrown when currencies don't match.</exception>
    public Money Add(Money other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>
    /// Subtracts another Money instance from this one.
    /// </summary>
    /// <param name="other">The Money instance to subtract.</param>
    /// <returns>A new Money instance with the difference.</returns>
    /// <exception cref="InvalidOperationException">Thrown when currencies don't match.</exception>
    public Money Subtract(Money other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    /// <summary>
    /// Multiplies the Money instance by a scalar value.
    /// </summary>
    /// <param name="multiplier">The multiplier.</param>
    /// <returns>A new Money instance with the product.</returns>
    public Money Multiply(decimal multiplier)
    {
        return new Money(Amount * multiplier, Currency);
    }

    /// <summary>
    /// Divides the Money instance by a scalar value.
    /// </summary>
    /// <param name="divisor">The divisor.</param>
    /// <returns>A new Money instance with the quotient.</returns>
    /// <exception cref="DivideByZeroException">Thrown when divisor is zero.</exception>
    public Money Divide(decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide money by zero.");

        return new Money(Amount / divisor, Currency);
    }

    /// <summary>
    /// Returns the absolute value of the Money instance.
    /// </summary>
    /// <returns>A new Money instance with the absolute amount.</returns>
    public Money Abs()
    {
        return new Money(Math.Abs(Amount), Currency);
    }

    /// <summary>
    /// Returns the negated value of the Money instance.
    /// </summary>
    /// <returns>A new Money instance with the negated amount.</returns>
    public Money Negate()
    {
        return new Money(-Amount, Currency);
    }

    /// <summary>
    /// Rounds the Money instance to the specified number of decimal places.
    /// </summary>
    /// <param name="decimals">The number of decimal places.</param>
    /// <param name="mode">The rounding mode.</param>
    /// <returns>A new Money instance with the rounded amount.</returns>
    public Money Round(int decimals = 2, MidpointRounding mode = MidpointRounding.ToEven)
    {
        return new Money(Math.Round(Amount, decimals, mode), Currency);
    }

    /// <summary>
    /// Compares this Money instance with another for ordering.
    /// </summary>
    /// <param name="other">The Money instance to compare with.</param>
    /// <returns>A value indicating the relative order.</returns>
    /// <exception cref="InvalidOperationException">Thrown when currencies don't match.</exception>
    public int CompareTo(Money other)
    {
        if (other == null)
            return 1;

        EnsureSameCurrency(other);
        return Amount.CompareTo(other.Amount);
    }

    /// <summary>
    /// Allocates the Money instance proportionally based on the given ratios.
    /// This is useful for splitting amounts while maintaining precision.
    /// </summary>
    /// <param name="ratios">The allocation ratios.</param>
    /// <returns>An array of Money instances representing the allocated amounts.</returns>
    /// <exception cref="ArgumentException">Thrown when ratios are invalid.</exception>
    public Money[] Allocate(params decimal[] ratios)
    {
        if (ratios == null || ratios.Length == 0)
            throw new ArgumentException("Ratios cannot be null or empty.", nameof(ratios));

        if (ratios.Any(r => r < 0))
            throw new ArgumentException("Ratios cannot be negative.", nameof(ratios));

        var totalRatio = ratios.Sum();
        if (totalRatio == 0)
            throw new ArgumentException("Total ratio cannot be zero.", nameof(ratios));

        var results = new Money[ratios.Length];
        var remainder = Amount;

        for (int i = 0; i < ratios.Length - 1; i++)
        {
            var allocated = Math.Floor(Amount * ratios[i] / totalRatio * 100) / 100;
            results[i] = new Money(allocated, Currency);
            remainder -= allocated;
        }

        // Assign the remainder to the last allocation to handle rounding
        results[^1] = new Money(remainder, Currency);

        return results;
    }

    /// <summary>
    /// Formats the Money instance as a currency string.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <returns>A formatted currency string.</returns>
    public string ToString(string format)
    {
        return $"{Amount.ToString(format)} {Currency}";
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    /// <summary>
    /// Ensures that two Money instances have the same currency.
    /// </summary>
    /// <param name="other">The other Money instance.</param>
    /// <exception cref="InvalidOperationException">Thrown when currencies don't match.</exception>
    private void EnsureSameCurrency(Money other)
    {
        if (!Currency.Equals(other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Cannot perform operation on different currencies: {Currency} and {other.Currency}");
    }

    /// <summary>
    /// Adds two Money instances.
    /// </summary>
    /// <param name="left">The first Money instance.</param>
    /// <param name="right">The second Money instance.</param>
    /// <returns>The sum of the two Money instances.</returns>
    public static Money operator +(Money left, Money right)
    {
        return left.Add(right);
    }

    /// <summary>
    /// Subtracts two Money instances.
    /// </summary>
    /// <param name="left">The first Money instance.</param>
    /// <param name="right">The second Money instance.</param>
    /// <returns>The difference of the two Money instances.</returns>
    public static Money operator -(Money left, Money right)
    {
        return left.Subtract(right);
    }

    /// <summary>
    /// Multiplies a Money instance by a scalar.
    /// </summary>
    /// <param name="left">The Money instance.</param>
    /// <param name="right">The scalar multiplier.</param>
    /// <returns>The product.</returns>
    public static Money operator *(Money left, decimal right)
    {
        return left.Multiply(right);
    }

    /// <summary>
    /// Divides a Money instance by a scalar.
    /// </summary>
    /// <param name="left">The Money instance.</param>
    /// <param name="right">The scalar divisor.</param>
    /// <returns>The quotient.</returns>
    public static Money operator /(Money left, decimal right)
    {
        return left.Divide(right);
    }

    /// <summary>
    /// Compares two Money instances for greater than.
    /// </summary>
    /// <param name="left">The first Money instance.</param>
    /// <param name="right">The second Money instance.</param>
    /// <returns>true if left is greater than right; otherwise, false.</returns>
    public static bool operator >(Money left, Money right)
    {
        return left.CompareTo(right) > 0;
    }

    /// <summary>
    /// Compares two Money instances for less than.
    /// </summary>
    /// <param name="left">The first Money instance.</param>
    /// <param name="right">The second Money instance.</param>
    /// <returns>true if left is less than right; otherwise, false.</returns>
    public static bool operator <(Money left, Money right)
    {
        return left.CompareTo(right) < 0;
    }

    /// <summary>
    /// Compares two Money instances for greater than or equal.
    /// </summary>
    /// <param name="left">The first Money instance.</param>
    /// <param name="right">The second Money instance.</param>
    /// <returns>true if left is greater than or equal to right; otherwise, false.</returns>
    public static bool operator >=(Money left, Money right)
    {
        return left.CompareTo(right) >= 0;
    }

    /// <summary>
    /// Compares two Money instances for less than or equal.
    /// </summary>
    /// <param name="left">The first Money instance.</param>
    /// <param name="right">The second Money instance.</param>
    /// <returns>true if left is less than or equal to right; otherwise, false.</returns>
    public static bool operator <=(Money left, Money right)
    {
        return left.CompareTo(right) <= 0;
    }

    /// <summary>
    /// Returns a string representation of the Money instance.
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString()
    {
        return $"{Amount:C} {Currency}";
    }
}