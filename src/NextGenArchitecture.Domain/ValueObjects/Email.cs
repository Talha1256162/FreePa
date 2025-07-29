using NextGenArchitecture.SharedKernel.Common;
using System.Text.RegularExpressions;

namespace NextGenArchitecture.Domain.ValueObjects;

/// <summary>
/// Represents an email address value object.
/// Ensures email validity and provides a rich domain model for email addresses.
/// </summary>
public sealed class Email : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="Email"/> class.
    /// </summary>
    /// <param name="value">The email address value.</param>
    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the email address value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the local part of the email address (before the @ symbol).
    /// </summary>
    public string LocalPart => Value.Split('@')[0];

    /// <summary>
    /// Gets the domain part of the email address (after the @ symbol).
    /// </summary>
    public string Domain => Value.Split('@')[1];

    /// <summary>
    /// Creates a new Email instance from a string value.
    /// </summary>
    /// <param name="email">The email address string.</param>
    /// <returns>A new Email instance if valid; otherwise, throws an exception.</returns>
    /// <exception cref="ArgumentException">Thrown when the email format is invalid.</exception>
    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty.", nameof(email));

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (!IsValidEmail(normalizedEmail))
            throw new ArgumentException($"Invalid email format: {email}", nameof(email));

        return new Email(normalizedEmail);
    }

    /// <summary>
    /// Tries to create a new Email instance from a string value.
    /// </summary>
    /// <param name="email">The email address string.</param>
    /// <param name="result">The created Email instance if successful.</param>
    /// <returns>true if the email is valid and was created successfully; otherwise, false.</returns>
    public static bool TryCreate(string email, out Email? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(email))
            return false;

        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (!IsValidEmail(normalizedEmail))
            return false;

        result = new Email(normalizedEmail);
        return true;
    }

    /// <summary>
    /// Validates if the given string is a valid email format.
    /// </summary>
    /// <param name="email">The email string to validate.</param>
    /// <returns>true if the email is valid; otherwise, false.</returns>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return EmailRegex.IsMatch(email);
    }

    /// <summary>
    /// Determines if this email belongs to the specified domain.
    /// </summary>
    /// <param name="domain">The domain to check.</param>
    /// <returns>true if the email belongs to the specified domain; otherwise, false.</returns>
    public bool BelongsToDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return false;

        return Domain.Equals(domain.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Masks the email address for privacy purposes.
    /// Example: john.doe@example.com becomes j***@example.com
    /// </summary>
    /// <returns>A masked version of the email address.</returns>
    public string ToMaskedString()
    {
        if (LocalPart.Length <= 1)
            return $"*@{Domain}";

        return $"{LocalPart[0]}***@{Domain}";
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <summary>
    /// Implicitly converts an Email to a string.
    /// </summary>
    /// <param name="email">The Email instance.</param>
    /// <returns>The email address string.</returns>
    public static implicit operator string(Email email)
    {
        return email.Value;
    }

    /// <summary>
    /// Returns the email address string.
    /// </summary>
    /// <returns>The email address string.</returns>
    public override string ToString()
    {
        return Value;
    }
}