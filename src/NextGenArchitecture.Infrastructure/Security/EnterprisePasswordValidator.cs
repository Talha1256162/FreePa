using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextGenArchitecture.Domain.ValueObjects;

namespace NextGenArchitecture.Infrastructure.Security;

/// <summary>
/// Enterprise-grade password validator implementing comprehensive security policies.
/// Suitable for banking and high-security applications with strict password requirements.
/// </summary>
public sealed class EnterprisePasswordValidator : IPasswordValidator
{
    private readonly PasswordPolicyOptions _options;
    private readonly ILogger<EnterprisePasswordValidator> _logger;
    private readonly IBreachedPasswordChecker _breachedPasswordChecker;
    
    // Common weak passwords and patterns
    private static readonly HashSet<string> CommonWeakPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "123456", "password123", "admin", "qwerty", "letmein", "welcome",
        "monkey", "1234567890", "abc123", "111111", "dragon", "master", "monkey",
        "letmein", "login", "princess", "qwertyuiop", "solo", "passw0rd"
    };

    private static readonly string[] CommonPatterns = 
    {
        @"(.)\1{2,}", // Repeated characters (aaa, 111, etc.)
        @"(012|123|234|345|456|567|678|789|890)", // Sequential numbers
        @"(abc|bcd|cde|def|efg|fgh|ghi|hij|ijk|jkl|klm|lmn|mno|nop|opq|pqr|qrs|rst|stu|tuv|uvw|vwx|wxy|xyz)", // Sequential letters
        @"(qwe|wer|ert|rty|tyu|yui|uio|iop|asd|sdf|dfg|fgh|ghj|hjk|jkl|zxc|xcv|cvb|vbn|bnm)" // Keyboard patterns
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="EnterprisePasswordValidator"/> class.
    /// </summary>
    /// <param name="options">The password policy options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="breachedPasswordChecker">The breached password checker service.</param>
    public EnterprisePasswordValidator(
        IOptions<PasswordPolicyOptions> options, 
        ILogger<EnterprisePasswordValidator> logger,
        IBreachedPasswordChecker breachedPasswordChecker)
    {
        _options = options.Value;
        _logger = logger;
        _breachedPasswordChecker = breachedPasswordChecker;
    }

    /// <summary>
    /// Validates a password against comprehensive security policies.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <param name="username">The username (to check for similarity).</param>
    /// <param name="email">The email address (to check for similarity).</param>
    /// <returns>A validation result indicating success or failure with detailed errors.</returns>
    public async Task<PasswordValidationResult> ValidatePasswordAsync(string password, string? username = null, Email? email = null)
    {
        var errors = new List<string>();

        try
        {
            // Basic null/empty check
            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Password cannot be empty.");
                return new PasswordValidationResult(false, errors);
            }

            // Length validation
            if (password.Length < _options.MinimumLength)
            {
                errors.Add($"Password must be at least {_options.MinimumLength} characters long.");
            }

            if (password.Length > _options.MaximumLength)
            {
                errors.Add($"Password cannot exceed {_options.MaximumLength} characters.");
            }

            // Character composition validation
            ValidateCharacterComposition(password, errors);

            // Pattern validation
            ValidatePatterns(password, errors);

            // Common password check
            if (IsCommonWeakPassword(password))
            {
                errors.Add("Password is too common and easily guessable.");
            }

            // Username/email similarity check
            ValidateSimilarity(password, username, email?.Value, errors);

            // Dictionary word check
            if (_options.PreventDictionaryWords && ContainsDictionaryWords(password))
            {
                errors.Add("Password contains common dictionary words.");
            }

            // Breached password check (if enabled)
            if (_options.CheckBreachedPasswords)
            {
                var isBreached = await _breachedPasswordChecker.IsPasswordBreachedAsync(password);
                if (isBreached)
                {
                    errors.Add("This password has been found in data breaches and cannot be used.");
                    _logger.LogWarning("Attempted use of breached password");
                }
            }

            // Entropy check
            if (_options.MinimumEntropy > 0)
            {
                var entropy = CalculatePasswordEntropy(password);
                if (entropy < _options.MinimumEntropy)
                {
                    errors.Add($"Password complexity is insufficient. Required entropy: {_options.MinimumEntropy}, actual: {entropy:F1}");
                }
            }

            var isValid = errors.Count == 0;
            
            if (isValid)
            {
                _logger.LogDebug("Password validation successful");
            }
            else
            {
                _logger.LogInformation("Password validation failed with {ErrorCount} errors", errors.Count);
            }

            return new PasswordValidationResult(isValid, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password validation failed due to exception");
            errors.Add("Password validation failed due to an internal error.");
            return new PasswordValidationResult(false, errors);
        }
    }

    /// <summary>
    /// Synchronous version of password validation for backward compatibility.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <param name="username">The username.</param>
    /// <param name="email">The email address.</param>
    /// <returns>A validation result.</returns>
    public PasswordValidationResult ValidatePassword(string password, string? username = null, Email? email = null)
    {
        return ValidatePasswordAsync(password, username, email).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Validates the character composition of the password.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <param name="errors">The error list to populate.</param>
    private void ValidateCharacterComposition(string password, List<string> errors)
    {
        var hasLower = password.Any(char.IsLower);
        var hasUpper = password.Any(char.IsUpper);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

        var characterTypes = 0;
        if (hasLower) characterTypes++;
        if (hasUpper) characterTypes++;
        if (hasDigit) characterTypes++;
        if (hasSpecial) characterTypes++;

        if (characterTypes < _options.MinimumCharacterTypes)
        {
            var requirements = new List<string>();
            if (_options.RequireLowercase && !hasLower) requirements.Add("lowercase letters");
            if (_options.RequireUppercase && !hasUpper) requirements.Add("uppercase letters");
            if (_options.RequireDigits && !hasDigit) requirements.Add("digits");
            if (_options.RequireSpecialCharacters && !hasSpecial) requirements.Add("special characters");

            if (requirements.Any())
            {
                errors.Add($"Password must contain: {string.Join(", ", requirements)}.");
            }
            else
            {
                errors.Add($"Password must contain at least {_options.MinimumCharacterTypes} different character types.");
            }
        }
    }

    /// <summary>
    /// Validates password patterns to prevent weak patterns.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <param name="errors">The error list to populate.</param>
    private void ValidatePatterns(string password, List<string> errors)
    {
        // Check for repeated characters
        if (HasRepeatedCharacters(password, _options.MaxConsecutiveIdenticalChars))
        {
            errors.Add($"Password cannot contain more than {_options.MaxConsecutiveIdenticalChars} consecutive identical characters.");
        }

        // Check for common patterns
        foreach (var pattern in CommonPatterns)
        {
            if (Regex.IsMatch(password.ToLowerInvariant(), pattern))
            {
                errors.Add("Password contains predictable patterns (sequential characters, keyboard patterns, etc.).");
                break;
            }
        }

        // Check for dates
        if (ContainsDatePatterns(password))
        {
            errors.Add("Password should not contain date patterns.");
        }
    }

    /// <summary>
    /// Validates similarity between password and personal information.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <param name="username">The username.</param>
    /// <param name="email">The email address.</param>
    /// <param name="errors">The error list to populate.</param>
    private void ValidateSimilarity(string password, string? username, string? email, List<string> errors)
    {
        var passwordLower = password.ToLowerInvariant();

        // Check username similarity
        if (!string.IsNullOrEmpty(username))
        {
            var usernameLower = username.ToLowerInvariant();
            if (passwordLower.Contains(usernameLower) || usernameLower.Contains(passwordLower))
            {
                errors.Add("Password cannot be similar to the username.");
            }
        }

        // Check email similarity
        if (!string.IsNullOrEmpty(email))
        {
            var emailParts = email.ToLowerInvariant().Split('@');
            var localPart = emailParts[0];
            
            if (passwordLower.Contains(localPart) || localPart.Contains(passwordLower))
            {
                errors.Add("Password cannot be similar to the email address.");
            }
        }
    }

    /// <summary>
    /// Checks if the password is a common weak password.
    /// </summary>
    /// <param name="password">The password to check.</param>
    /// <returns>True if it's a common weak password; otherwise, false.</returns>
    private static bool IsCommonWeakPassword(string password)
    {
        return CommonWeakPasswords.Contains(password);
    }

    /// <summary>
    /// Checks if the password contains dictionary words.
    /// </summary>
    /// <param name="password">The password to check.</param>
    /// <returns>True if it contains dictionary words; otherwise, false.</returns>
    private static bool ContainsDictionaryWords(string password)
    {
        // In a real implementation, this would check against a comprehensive dictionary
        // For now, we'll do a basic check for common English words
        var commonWords = new[] { "password", "admin", "user", "login", "welcome", "hello", "world", "computer", "security" };
        
        return commonWords.Any(word => password.ToLowerInvariant().Contains(word));
    }

    /// <summary>
    /// Checks if the password has too many repeated characters.
    /// </summary>
    /// <param name="password">The password to check.</param>
    /// <param name="maxConsecutive">The maximum allowed consecutive identical characters.</param>
    /// <returns>True if it has too many repeated characters; otherwise, false.</returns>
    private static bool HasRepeatedCharacters(string password, int maxConsecutive)
    {
        var count = 1;
        for (var i = 1; i < password.Length; i++)
        {
            if (password[i] == password[i - 1])
            {
                count++;
                if (count > maxConsecutive)
                    return true;
            }
            else
            {
                count = 1;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if the password contains date patterns.
    /// </summary>
    /// <param name="password">The password to check.</param>
    /// <returns>True if it contains date patterns; otherwise, false.</returns>
    private static bool ContainsDatePatterns(string password)
    {
        // Check for common date patterns
        var datePatterns = new[]
        {
            @"\d{4}", // 4-digit years
            @"\d{2}/\d{2}/\d{4}", // MM/DD/YYYY
            @"\d{2}-\d{2}-\d{4}", // MM-DD-YYYY
            @"\d{8}" // YYYYMMDD or MMDDYYYY
        };

        return datePatterns.Any(pattern => Regex.IsMatch(password, pattern));
    }

    /// <summary>
    /// Calculates the entropy of a password.
    /// </summary>
    /// <param name="password">The password to analyze.</param>
    /// <returns>The entropy value in bits.</returns>
    private static double CalculatePasswordEntropy(string password)
    {
        if (string.IsNullOrEmpty(password))
            return 0;

        var charsetSize = 0;
        
        if (password.Any(char.IsLower)) charsetSize += 26; // a-z
        if (password.Any(char.IsUpper)) charsetSize += 26; // A-Z
        if (password.Any(char.IsDigit)) charsetSize += 10; // 0-9
        if (password.Any(c => !char.IsLetterOrDigit(c))) charsetSize += 32; // Special characters (estimate)

        return password.Length * Math.Log2(charsetSize);
    }
}

/// <summary>
/// Configuration options for password policy.
/// </summary>
public sealed class PasswordPolicyOptions
{
    /// <summary>
    /// Gets or sets the minimum password length.
    /// </summary>
    public int MinimumLength { get; set; } = 12;

    /// <summary>
    /// Gets or sets the maximum password length.
    /// </summary>
    public int MaximumLength { get; set; } = 128;

    /// <summary>
    /// Gets or sets whether lowercase letters are required.
    /// </summary>
    public bool RequireLowercase { get; set; } = true;

    /// <summary>
    /// Gets or sets whether uppercase letters are required.
    /// </summary>
    public bool RequireUppercase { get; set; } = true;

    /// <summary>
    /// Gets or sets whether digits are required.
    /// </summary>
    public bool RequireDigits { get; set; } = true;

    /// <summary>
    /// Gets or sets whether special characters are required.
    /// </summary>
    public bool RequireSpecialCharacters { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum number of character types required.
    /// </summary>
    public int MinimumCharacterTypes { get; set; } = 3;

    /// <summary>
    /// Gets or sets the maximum number of consecutive identical characters allowed.
    /// </summary>
    public int MaxConsecutiveIdenticalChars { get; set; } = 2;

    /// <summary>
    /// Gets or sets whether to prevent dictionary words.
    /// </summary>
    public bool PreventDictionaryWords { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to check against breached passwords.
    /// </summary>
    public bool CheckBreachedPasswords { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum entropy required (in bits).
    /// </summary>
    public double MinimumEntropy { get; set; } = 50.0;
}

/// <summary>
/// Represents the result of password validation.
/// </summary>
public sealed class PasswordValidationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordValidationResult"/> class.
    /// </summary>
    /// <param name="isValid">Whether the password is valid.</param>
    /// <param name="errors">The validation errors.</param>
    public PasswordValidationResult(bool isValid, IEnumerable<string> errors)
    {
        IsValid = isValid;
        Errors = errors.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets a value indicating whether the password is valid.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public IReadOnlyCollection<string> Errors { get; }
}

/// <summary>
/// Interface for password validation services.
/// </summary>
public interface IPasswordValidator
{
    /// <summary>
    /// Validates a password asynchronously.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <param name="username">The username.</param>
    /// <param name="email">The email address.</param>
    /// <returns>A validation result.</returns>
    Task<PasswordValidationResult> ValidatePasswordAsync(string password, string? username = null, Email? email = null);

    /// <summary>
    /// Validates a password synchronously.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <param name="username">The username.</param>
    /// <param name="email">The email address.</param>
    /// <returns>A validation result.</returns>
    PasswordValidationResult ValidatePassword(string password, string? username = null, Email? email = null);
}

/// <summary>
/// Interface for checking breached passwords.
/// </summary>
public interface IBreachedPasswordChecker
{
    /// <summary>
    /// Checks if a password has been found in data breaches.
    /// </summary>
    /// <param name="password">The password to check.</param>
    /// <returns>True if breached; otherwise, false.</returns>
    Task<bool> IsPasswordBreachedAsync(string password);
}