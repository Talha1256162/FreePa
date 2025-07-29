using NextGenArchitecture.SharedKernel.Common;

namespace NextGenArchitecture.Domain.Entities;

/// <summary>
/// Represents a login attempt for security auditing and monitoring.
/// Tracks all login attempts with detailed information for security analysis.
/// </summary>
public sealed class LoginAttempt : BaseEntity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginAttempt"/> class.
    /// </summary>
    /// <param name="userAccountId">The user account ID.</param>
    /// <param name="ipAddress">The IP address of the attempt.</param>
    /// <param name="userAgent">The user agent string.</param>
    /// <param name="isSuccessful">Whether the attempt was successful.</param>
    /// <param name="failureReason">The reason for failure if unsuccessful.</param>
    /// <param name="deviceFingerprint">The device fingerprint.</param>
    internal LoginAttempt(Guid userAccountId, string ipAddress, string userAgent, 
        bool isSuccessful, string? failureReason = null, string? deviceFingerprint = null)
    {
        UserAccountId = userAccountId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        IsSuccessful = isSuccessful;
        FailureReason = failureReason;
        DeviceFingerprint = deviceFingerprint;
        AttemptedAt = DateTime.UtcNow;
        
        // Detect suspicious patterns
        IsSuspicious = DetectSuspiciousActivity(ipAddress, userAgent, deviceFingerprint);
        
        // Geo-location (would be populated by a service)
        Country = DetermineCountryFromIp(ipAddress);
        City = DetermineCityFromIp(ipAddress);
    }

    /// <summary>
    /// Gets the user account ID this attempt belongs to.
    /// </summary>
    public Guid UserAccountId { get; private set; }

    /// <summary>
    /// Gets the IP address of the login attempt.
    /// </summary>
    public string IpAddress { get; private set; }

    /// <summary>
    /// Gets the user agent string of the login attempt.
    /// </summary>
    public string UserAgent { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the login attempt was successful.
    /// </summary>
    public bool IsSuccessful { get; private set; }

    /// <summary>
    /// Gets the reason for failure if the attempt was unsuccessful.
    /// </summary>
    public string? FailureReason { get; private set; }

    /// <summary>
    /// Gets the device fingerprint for the attempt.
    /// </summary>
    public string? DeviceFingerprint { get; private set; }

    /// <summary>
    /// Gets the timestamp when the attempt was made.
    /// </summary>
    public DateTime AttemptedAt { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this attempt is considered suspicious.
    /// </summary>
    public bool IsSuspicious { get; private set; }

    /// <summary>
    /// Gets the country derived from the IP address.
    /// </summary>
    public string? Country { get; private set; }

    /// <summary>
    /// Gets the city derived from the IP address.
    /// </summary>
    public string? City { get; private set; }

    /// <summary>
    /// Gets the browser information extracted from user agent.
    /// </summary>
    public string? Browser => ExtractBrowserFromUserAgent(UserAgent);

    /// <summary>
    /// Gets the operating system information extracted from user agent.
    /// </summary>
    public string? OperatingSystem => ExtractOsFromUserAgent(UserAgent);

    /// <summary>
    /// Detects suspicious activity based on various factors.
    /// </summary>
    /// <param name="ipAddress">The IP address.</param>
    /// <param name="userAgent">The user agent.</param>
    /// <param name="deviceFingerprint">The device fingerprint.</param>
    /// <returns>True if the activity is suspicious; otherwise, false.</returns>
    private static bool DetectSuspiciousActivity(string ipAddress, string userAgent, string? deviceFingerprint)
    {
        // Check for suspicious IP ranges (Tor, VPN, etc.)
        if (IsSuspiciousIp(ipAddress))
            return true;

        // Check for suspicious user agents
        if (IsSuspiciousUserAgent(userAgent))
            return true;

        // Check for missing device fingerprint (could indicate automation)
        if (string.IsNullOrEmpty(deviceFingerprint))
            return true;

        return false;
    }

    /// <summary>
    /// Checks if an IP address is suspicious.
    /// </summary>
    /// <param name="ipAddress">The IP address to check.</param>
    /// <returns>True if suspicious; otherwise, false.</returns>
    private static bool IsSuspiciousIp(string ipAddress)
    {
        // In a real implementation, this would check against:
        // - Known malicious IP databases
        // - Tor exit nodes
        // - VPN/Proxy services
        // - Geo-location restrictions
        
        // Placeholder implementation
        return ipAddress.StartsWith("10.") || // Private IP (could be proxy)
               ipAddress.StartsWith("192.168.") || // Private IP
               ipAddress == "127.0.0.1"; // Localhost
    }

    /// <summary>
    /// Checks if a user agent is suspicious.
    /// </summary>
    /// <param name="userAgent">The user agent to check.</param>
    /// <returns>True if suspicious; otherwise, false.</returns>
    private static bool IsSuspiciousUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return true;

        var suspiciousPatterns = new[]
        {
            "bot", "crawler", "spider", "scraper", "curl", "wget", "python", "java",
            "automated", "test", "phantom", "headless"
        };

        return suspiciousPatterns.Any(pattern => 
            userAgent.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines the country from an IP address.
    /// </summary>
    /// <param name="ipAddress">The IP address.</param>
    /// <returns>The country name or null if unknown.</returns>
    private static string? DetermineCountryFromIp(string ipAddress)
    {
        // In a real implementation, this would use a GeoIP service
        // like MaxMind GeoLite2 or similar
        return null; // Placeholder
    }

    /// <summary>
    /// Determines the city from an IP address.
    /// </summary>
    /// <param name="ipAddress">The IP address.</param>
    /// <returns>The city name or null if unknown.</returns>
    private static string? DetermineCityFromIp(string ipAddress)
    {
        // In a real implementation, this would use a GeoIP service
        return null; // Placeholder
    }

    /// <summary>
    /// Extracts browser information from user agent string.
    /// </summary>
    /// <param name="userAgent">The user agent string.</param>
    /// <returns>The browser name and version.</returns>
    private static string? ExtractBrowserFromUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return null;

        // Simplified browser detection
        if (userAgent.Contains("Chrome"))
            return "Chrome";
        if (userAgent.Contains("Firefox"))
            return "Firefox";
        if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome"))
            return "Safari";
        if (userAgent.Contains("Edge"))
            return "Edge";

        return "Unknown";
    }

    /// <summary>
    /// Extracts operating system information from user agent string.
    /// </summary>
    /// <param name="userAgent">The user agent string.</param>
    /// <returns>The operating system name.</returns>
    private static string? ExtractOsFromUserAgent(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return null;

        // Simplified OS detection
        if (userAgent.Contains("Windows"))
            return "Windows";
        if (userAgent.Contains("Mac OS"))
            return "macOS";
        if (userAgent.Contains("Linux"))
            return "Linux";
        if (userAgent.Contains("Android"))
            return "Android";
        if (userAgent.Contains("iPhone") || userAgent.Contains("iPad"))
            return "iOS";

        return "Unknown";
    }
}