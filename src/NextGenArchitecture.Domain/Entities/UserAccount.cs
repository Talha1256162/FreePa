using NextGenArchitecture.Domain.ValueObjects;
using NextGenArchitecture.SharedKernel.Common;
using NextGenArchitecture.SharedKernel.Events;

namespace NextGenArchitecture.Domain.Entities;

/// <summary>
/// Represents a secure user account with comprehensive authentication and security features.
/// Implements enterprise-grade security measures suitable for banking and high-security applications.
/// </summary>
public sealed class UserAccount : BaseEntity
{
    private readonly List<LoginAttempt> _loginAttempts = new();
    private readonly List<SecurityEvent> _securityEvents = new();
    private readonly List<TrustedDevice> _trustedDevices = new();
    private readonly List<ActiveSession> _activeSessions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="UserAccount"/> class.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="username">The unique username.</param>
    /// <param name="passwordHash">The hashed password.</param>
    /// <param name="salt">The password salt.</param>
    private UserAccount(Email email, string username, string passwordHash, string salt)
    {
        Email = email;
        Username = username;
        PasswordHash = passwordHash;
        Salt = salt;
        AccountStatus = AccountStatus.PendingVerification;
        SecurityLevel = SecurityLevel.Standard;
        RequiresMfaSetup = true;
        PasswordLastChanged = DateTime.UtcNow;
        AccountCreatedAt = DateTime.UtcNow;
        LastSecurityAudit = DateTime.UtcNow;
        
        AddDomainEvent(new UserAccountCreatedEvent(Id, email, username));
    }

    /// <summary>
    /// Gets the user's email address.
    /// </summary>
    public Email Email { get; private set; }

    /// <summary>
    /// Gets the unique username.
    /// </summary>
    public string Username { get; private set; }

    /// <summary>
    /// Gets the hashed password using Argon2id.
    /// </summary>
    public string PasswordHash { get; private set; }

    /// <summary>
    /// Gets the password salt.
    /// </summary>
    public string Salt { get; private set; }

    /// <summary>
    /// Gets the account status.
    /// </summary>
    public AccountStatus AccountStatus { get; private set; }

    /// <summary>
    /// Gets the security level of the account.
    /// </summary>
    public SecurityLevel SecurityLevel { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the account requires MFA setup.
    /// </summary>
    public bool RequiresMfaSetup { get; private set; }

    /// <summary>
    /// Gets a value indicating whether MFA is enabled.
    /// </summary>
    public bool MfaEnabled { get; private set; }

    /// <summary>
    /// Gets the MFA secret key (encrypted).
    /// </summary>
    public string? MfaSecretKey { get; private set; }

    /// <summary>
    /// Gets the backup codes for MFA (encrypted).
    /// </summary>
    public string? MfaBackupCodes { get; private set; }

    /// <summary>
    /// Gets the number of consecutive failed login attempts.
    /// </summary>
    public int FailedLoginAttempts { get; private set; }

    /// <summary>
    /// Gets the timestamp when the account was locked.
    /// </summary>
    public DateTime? LockedUntil { get; private set; }

    /// <summary>
    /// Gets the timestamp when the password was last changed.
    /// </summary>
    public DateTime PasswordLastChanged { get; private set; }

    /// <summary>
    /// Gets the timestamp when the account was created.
    /// </summary>
    public DateTime AccountCreatedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp of the last successful login.
    /// </summary>
    public DateTime? LastSuccessfulLogin { get; private set; }

    /// <summary>
    /// Gets the IP address of the last successful login.
    /// </summary>
    public string? LastLoginIpAddress { get; private set; }

    /// <summary>
    /// Gets the user agent of the last successful login.
    /// </summary>
    public string? LastLoginUserAgent { get; private set; }

    /// <summary>
    /// Gets the timestamp of the last security audit.
    /// </summary>
    public DateTime LastSecurityAudit { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the account requires password change.
    /// </summary>
    public bool RequiresPasswordChange { get; private set; }

    /// <summary>
    /// Gets the password expiration date based on security policy.
    /// </summary>
    public DateTime? PasswordExpiresAt { get; private set; }

    /// <summary>
    /// Gets the login attempts for this account.
    /// </summary>
    public IReadOnlyCollection<LoginAttempt> LoginAttempts => _loginAttempts.AsReadOnly();

    /// <summary>
    /// Gets the security events for this account.
    /// </summary>
    public IReadOnlyCollection<SecurityEvent> SecurityEvents => _securityEvents.AsReadOnly();

    /// <summary>
    /// Gets the trusted devices for this account.
    /// </summary>
    public IReadOnlyCollection<TrustedDevice> TrustedDevices => _trustedDevices.AsReadOnly();

    /// <summary>
    /// Gets the active sessions for this account.
    /// </summary>
    public IReadOnlyCollection<ActiveSession> ActiveSessions => _activeSessions.AsReadOnly();

    /// <summary>
    /// Creates a new secure user account.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="username">The unique username.</param>
    /// <param name="password">The plain text password.</param>
    /// <param name="passwordHasher">The password hashing service.</param>
    /// <returns>A new UserAccount instance.</returns>
    public static UserAccount Create(string email, string username, string password, IPasswordHasher passwordHasher)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be null or empty.", nameof(username));

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));

        var emailValue = Email.Create(email);
        var (hash, salt) = passwordHasher.HashPassword(password);
        
        return new UserAccount(emailValue, username.Trim(), hash, salt);
    }

    /// <summary>
    /// Verifies the account email.
    /// </summary>
    public void VerifyEmail()
    {
        if (AccountStatus == AccountStatus.Verified)
            throw new InvalidOperationException("Account is already verified.");

        AccountStatus = AccountStatus.Verified;
        AddDomainEvent(new AccountEmailVerifiedEvent(Id, Email));
        LogSecurityEvent(SecurityEventType.EmailVerified, "Email address verified successfully");
    }

    /// <summary>
    /// Sets up multi-factor authentication.
    /// </summary>
    /// <param name="secretKey">The encrypted MFA secret key.</param>
    /// <param name="backupCodes">The encrypted backup codes.</param>
    public void SetupMfa(string secretKey, string backupCodes)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new ArgumentException("MFA secret key cannot be null or empty.", nameof(secretKey));

        if (string.IsNullOrWhiteSpace(backupCodes))
            throw new ArgumentException("MFA backup codes cannot be null or empty.", nameof(backupCodes));

        MfaSecretKey = secretKey;
        MfaBackupCodes = backupCodes;
        MfaEnabled = true;
        RequiresMfaSetup = false;
        
        AddDomainEvent(new MfaEnabledEvent(Id, Email));
        LogSecurityEvent(SecurityEventType.MfaEnabled, "Multi-factor authentication enabled");
    }

    /// <summary>
    /// Disables multi-factor authentication.
    /// </summary>
    /// <param name="reason">The reason for disabling MFA.</param>
    public void DisableMfa(string reason)
    {
        if (!MfaEnabled)
            throw new InvalidOperationException("MFA is not enabled for this account.");

        MfaEnabled = false;
        MfaSecretKey = null;
        MfaBackupCodes = null;
        RequiresMfaSetup = true;
        
        AddDomainEvent(new MfaDisabledEvent(Id, Email, reason));
        LogSecurityEvent(SecurityEventType.MfaDisabled, $"Multi-factor authentication disabled: {reason}");
    }

    /// <summary>
    /// Records a login attempt.
    /// </summary>
    /// <param name="ipAddress">The IP address of the login attempt.</param>
    /// <param name="userAgent">The user agent string.</param>
    /// <param name="isSuccessful">Whether the login was successful.</param>
    /// <param name="failureReason">The reason for failure if unsuccessful.</param>
    /// <param name="deviceFingerprint">The device fingerprint.</param>
    /// <returns>The created login attempt.</returns>
    public LoginAttempt RecordLoginAttempt(string ipAddress, string userAgent, bool isSuccessful, 
        string? failureReason = null, string? deviceFingerprint = null)
    {
        var attempt = new LoginAttempt(Id, ipAddress, userAgent, isSuccessful, failureReason, deviceFingerprint);
        _loginAttempts.Add(attempt);

        if (isSuccessful)
        {
            FailedLoginAttempts = 0;
            LastSuccessfulLogin = DateTime.UtcNow;
            LastLoginIpAddress = ipAddress;
            LastLoginUserAgent = userAgent;
            LockedUntil = null;
            
            AddDomainEvent(new SuccessfulLoginEvent(Id, Email, ipAddress, userAgent, deviceFingerprint));
            LogSecurityEvent(SecurityEventType.SuccessfulLogin, $"Successful login from {ipAddress}");
        }
        else
        {
            FailedLoginAttempts++;
            
            // Lock account after 5 failed attempts
            if (FailedLoginAttempts >= 5)
            {
                var lockDuration = CalculateLockDuration(FailedLoginAttempts);
                LockedUntil = DateTime.UtcNow.Add(lockDuration);
                
                AddDomainEvent(new AccountLockedEvent(Id, Email, FailedLoginAttempts, LockedUntil.Value));
                LogSecurityEvent(SecurityEventType.AccountLocked, 
                    $"Account locked due to {FailedLoginAttempts} failed login attempts");
            }
            else
            {
                AddDomainEvent(new FailedLoginEvent(Id, Email, ipAddress, failureReason ?? "Unknown", FailedLoginAttempts));
                LogSecurityEvent(SecurityEventType.FailedLogin, 
                    $"Failed login attempt from {ipAddress}: {failureReason}");
            }
        }

        return attempt;
    }

    /// <summary>
    /// Changes the account password.
    /// </summary>
    /// <param name="currentPassword">The current password.</param>
    /// <param name="newPassword">The new password.</param>
    /// <param name="passwordHasher">The password hashing service.</param>
    /// <param name="passwordValidator">The password validation service.</param>
    public void ChangePassword(string currentPassword, string newPassword, 
        IPasswordHasher passwordHasher, IPasswordValidator passwordValidator)
    {
        if (!passwordHasher.VerifyPassword(currentPassword, PasswordHash, Salt))
            throw new InvalidOperationException("Current password is incorrect.");

        var validationResult = passwordValidator.ValidatePassword(newPassword, Username, Email);
        if (!validationResult.IsValid)
            throw new ArgumentException($"New password is invalid: {string.Join(", ", validationResult.Errors)}");

        var (hash, salt) = passwordHasher.HashPassword(newPassword);
        PasswordHash = hash;
        Salt = salt;
        PasswordLastChanged = DateTime.UtcNow;
        RequiresPasswordChange = false;
        PasswordExpiresAt = DateTime.UtcNow.AddDays(90); // 90-day password expiry

        AddDomainEvent(new PasswordChangedEvent(Id, Email));
        LogSecurityEvent(SecurityEventType.PasswordChanged, "Password changed successfully");
        
        // Invalidate all active sessions except current one
        InvalidateAllSessions(excludeCurrentSession: true);
    }

    /// <summary>
    /// Forces a password change requirement.
    /// </summary>
    /// <param name="reason">The reason for forcing password change.</param>
    public void ForcePasswordChange(string reason)
    {
        RequiresPasswordChange = true;
        AddDomainEvent(new PasswordChangeRequiredEvent(Id, Email, reason));
        LogSecurityEvent(SecurityEventType.PasswordChangeForced, $"Password change forced: {reason}");
    }

    /// <summary>
    /// Adds a trusted device.
    /// </summary>
    /// <param name="deviceFingerprint">The device fingerprint.</param>
    /// <param name="deviceName">The device name.</param>
    /// <param name="ipAddress">The IP address.</param>
    /// <param name="userAgent">The user agent.</param>
    /// <returns>The created trusted device.</returns>
    public TrustedDevice AddTrustedDevice(string deviceFingerprint, string deviceName, string ipAddress, string userAgent)
    {
        if (_trustedDevices.Any(d => d.DeviceFingerprint == deviceFingerprint && d.IsActive))
            throw new InvalidOperationException("Device is already trusted.");

        var trustedDevice = new TrustedDevice(Id, deviceFingerprint, deviceName, ipAddress, userAgent);
        _trustedDevices.Add(trustedDevice);
        
        AddDomainEvent(new TrustedDeviceAddedEvent(Id, Email, deviceFingerprint, deviceName));
        LogSecurityEvent(SecurityEventType.TrustedDeviceAdded, $"Trusted device added: {deviceName}");
        
        return trustedDevice;
    }

    /// <summary>
    /// Removes a trusted device.
    /// </summary>
    /// <param name="deviceFingerprint">The device fingerprint to remove.</param>
    public void RemoveTrustedDevice(string deviceFingerprint)
    {
        var device = _trustedDevices.FirstOrDefault(d => d.DeviceFingerprint == deviceFingerprint && d.IsActive);
        if (device == null)
            throw new InvalidOperationException("Trusted device not found.");

        device.Revoke();
        AddDomainEvent(new TrustedDeviceRemovedEvent(Id, Email, deviceFingerprint));
        LogSecurityEvent(SecurityEventType.TrustedDeviceRemoved, $"Trusted device removed: {device.DeviceName}");
    }

    /// <summary>
    /// Creates a new active session.
    /// </summary>
    /// <param name="sessionToken">The session token.</param>
    /// <param name="deviceFingerprint">The device fingerprint.</param>
    /// <param name="ipAddress">The IP address.</param>
    /// <param name="userAgent">The user agent.</param>
    /// <param name="expiresAt">The session expiration time.</param>
    /// <returns>The created active session.</returns>
    public ActiveSession CreateSession(string sessionToken, string deviceFingerprint, 
        string ipAddress, string userAgent, DateTime expiresAt)
    {
        var session = new ActiveSession(Id, sessionToken, deviceFingerprint, ipAddress, userAgent, expiresAt);
        _activeSessions.Add(session);
        
        LogSecurityEvent(SecurityEventType.SessionCreated, $"New session created from {ipAddress}");
        
        return session;
    }

    /// <summary>
    /// Invalidates a specific session.
    /// </summary>
    /// <param name="sessionToken">The session token to invalidate.</param>
    public void InvalidateSession(string sessionToken)
    {
        var session = _activeSessions.FirstOrDefault(s => s.SessionToken == sessionToken && s.IsActive);
        if (session != null)
        {
            session.Invalidate();
            LogSecurityEvent(SecurityEventType.SessionInvalidated, "Session invalidated");
        }
    }

    /// <summary>
    /// Invalidates all active sessions.
    /// </summary>
    /// <param name="excludeCurrentSession">Whether to exclude the current session.</param>
    public void InvalidateAllSessions(bool excludeCurrentSession = false)
    {
        var sessionsToInvalidate = excludeCurrentSession 
            ? _activeSessions.Where(s => s.IsActive).Skip(1) 
            : _activeSessions.Where(s => s.IsActive);

        foreach (var session in sessionsToInvalidate)
        {
            session.Invalidate();
        }

        AddDomainEvent(new AllSessionsInvalidatedEvent(Id, Email));
        LogSecurityEvent(SecurityEventType.AllSessionsInvalidated, "All sessions invalidated");
    }

    /// <summary>
    /// Locks the account.
    /// </summary>
    /// <param name="reason">The reason for locking the account.</param>
    /// <param name="lockDuration">The duration to lock the account.</param>
    public void LockAccount(string reason, TimeSpan lockDuration)
    {
        AccountStatus = AccountStatus.Locked;
        LockedUntil = DateTime.UtcNow.Add(lockDuration);
        
        AddDomainEvent(new AccountLockedEvent(Id, Email, FailedLoginAttempts, LockedUntil.Value));
        LogSecurityEvent(SecurityEventType.AccountLocked, $"Account manually locked: {reason}");
        
        InvalidateAllSessions();
    }

    /// <summary>
    /// Unlocks the account.
    /// </summary>
    public void UnlockAccount()
    {
        if (AccountStatus != AccountStatus.Locked)
            throw new InvalidOperationException("Account is not locked.");

        AccountStatus = AccountStatus.Verified;
        LockedUntil = null;
        FailedLoginAttempts = 0;
        
        AddDomainEvent(new AccountUnlockedEvent(Id, Email));
        LogSecurityEvent(SecurityEventType.AccountUnlocked, "Account unlocked");
    }

    /// <summary>
    /// Checks if the account is currently locked.
    /// </summary>
    /// <returns>True if the account is locked; otherwise, false.</returns>
    public bool IsLocked()
    {
        return AccountStatus == AccountStatus.Locked || 
               (LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow);
    }

    /// <summary>
    /// Checks if a device is trusted.
    /// </summary>
    /// <param name="deviceFingerprint">The device fingerprint to check.</param>
    /// <returns>True if the device is trusted; otherwise, false.</returns>
    public bool IsDeviceTrusted(string deviceFingerprint)
    {
        return _trustedDevices.Any(d => d.DeviceFingerprint == deviceFingerprint && 
                                       d.IsActive && 
                                       d.ExpiresAt > DateTime.UtcNow);
    }

    /// <summary>
    /// Elevates the security level of the account.
    /// </summary>
    /// <param name="newLevel">The new security level.</param>
    /// <param name="reason">The reason for elevation.</param>
    public void ElevateSecurityLevel(SecurityLevel newLevel, string reason)
    {
        if (newLevel <= SecurityLevel)
            throw new ArgumentException("New security level must be higher than current level.");

        SecurityLevel = newLevel;
        AddDomainEvent(new SecurityLevelElevatedEvent(Id, Email, newLevel, reason));
        LogSecurityEvent(SecurityEventType.SecurityLevelChanged, $"Security level elevated to {newLevel}: {reason}");
    }

    /// <summary>
    /// Logs a security event.
    /// </summary>
    /// <param name="eventType">The type of security event.</param>
    /// <param name="description">The event description.</param>
    private void LogSecurityEvent(SecurityEventType eventType, string description)
    {
        var securityEvent = new SecurityEvent(Id, eventType, description);
        _securityEvents.Add(securityEvent);
    }

    /// <summary>
    /// Calculates the lock duration based on the number of failed attempts.
    /// </summary>
    /// <param name="failedAttempts">The number of failed attempts.</param>
    /// <returns>The lock duration.</returns>
    private static TimeSpan CalculateLockDuration(int failedAttempts)
    {
        return failedAttempts switch
        {
            >= 10 => TimeSpan.FromHours(24), // 24 hours for 10+ attempts
            >= 8 => TimeSpan.FromHours(4),   // 4 hours for 8-9 attempts
            >= 6 => TimeSpan.FromHours(1),   // 1 hour for 6-7 attempts
            _ => TimeSpan.FromMinutes(15)    // 15 minutes for 5 attempts
        };
    }
}

/// <summary>
/// Represents the status of a user account.
/// </summary>
public enum AccountStatus
{
    /// <summary>
    /// Account is pending email verification.
    /// </summary>
    PendingVerification = 1,

    /// <summary>
    /// Account is verified and active.
    /// </summary>
    Verified = 2,

    /// <summary>
    /// Account is temporarily locked.
    /// </summary>
    Locked = 3,

    /// <summary>
    /// Account is suspended by an administrator.
    /// </summary>
    Suspended = 4,

    /// <summary>
    /// Account is permanently closed.
    /// </summary>
    Closed = 5
}

/// <summary>
/// Represents the security level of an account.
/// </summary>
public enum SecurityLevel
{
    /// <summary>
    /// Standard security level.
    /// </summary>
    Standard = 1,

    /// <summary>
    /// Enhanced security level with additional monitoring.
    /// </summary>
    Enhanced = 2,

    /// <summary>
    /// High security level for privileged accounts.
    /// </summary>
    High = 3,

    /// <summary>
    /// Maximum security level for critical accounts.
    /// </summary>
    Maximum = 4
}