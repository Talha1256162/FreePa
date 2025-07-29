using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NextGenArchitecture.Application.Authentication.Commands.Login;
using NextGenArchitecture.Application.Authentication.Commands.Register;
using NextGenArchitecture.Application.Authentication.Commands.RefreshToken;
using NextGenArchitecture.Application.Authentication.Commands.Logout;
using NextGenArchitecture.Application.Authentication.Commands.SetupMfa;
using NextGenArchitecture.Application.Authentication.Commands.VerifyMfa;
using NextGenArchitecture.SharedKernel.Results;
using System.Net;
using System.Security.Claims;

namespace NextGenArchitecture.API.Controllers.V1;

/// <summary>
/// The most secure authentication controller implementing enterprise-grade security measures.
/// Features comprehensive protection suitable for banking and high-security applications.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
public sealed class AuthenticationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthenticationController> _logger;
    private readonly IDeviceFingerprintService _deviceFingerprintService;
    private readonly ISecurityMonitoringService _securityMonitoringService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationController"/> class.
    /// </summary>
    /// <param name="mediator">The mediator for handling commands and queries.</param>
    /// <param name="logger">The logger for diagnostic information.</param>
    /// <param name="deviceFingerprintService">The device fingerprinting service.</param>
    /// <param name="securityMonitoringService">The security monitoring service.</param>
    public AuthenticationController(
        IMediator mediator, 
        ILogger<AuthenticationController> logger,
        IDeviceFingerprintService deviceFingerprintService,
        ISecurityMonitoringService securityMonitoringService)
    {
        _mediator = mediator;
        _logger = logger;
        _deviceFingerprintService = deviceFingerprintService;
        _securityMonitoringService = securityMonitoringService;
    }

    /// <summary>
    /// Registers a new user account with comprehensive security validation.
    /// </summary>
    /// <param name="command">The registration command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The registration result with security information.</returns>
    /// <response code="201">User registered successfully.</response>
    /// <response code="400">Invalid registration data or security policy violation.</response>
    /// <response code="409">User already exists.</response>
    /// <response code="429">Too many registration attempts.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("register")]
    [EnableRateLimiting("RegistrationPolicy")]
    [ProducesResponseType(typeof(RegisterResponse), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken cancellationToken = default)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = GetUserAgent();
        var deviceFingerprint = await _deviceFingerprintService.GenerateFingerprintAsync(Request);

        _logger.LogInformation("Registration attempt from IP: {IpAddress}, Device: {DeviceFingerprint}", 
            ipAddress, deviceFingerprint);

        // Enhanced command with security context
        var enhancedCommand = command with 
        { 
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceFingerprint = deviceFingerprint
        };

        var result = await _mediator.Send(enhancedCommand, cancellationToken);

        if (result.IsFailure)
        {
            await _securityMonitoringService.LogSecurityEventAsync(new SecurityEvent
            {
                EventType = SecurityEventType.RegistrationFailed,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceFingerprint = deviceFingerprint,
                Details = result.Error,
                Severity = SecurityEventSeverity.Medium
            });

            _logger.LogWarning("Registration failed from {IpAddress}: {Error}", ipAddress, result.Error);
            
            return result.Error!.Contains("already exists") 
                ? Conflict(new ErrorResponse { Error = result.Error })
                : BadRequest(new ErrorResponse { Error = result.Error });
        }

        await _securityMonitoringService.LogSecurityEventAsync(new SecurityEvent
        {
            EventType = SecurityEventType.RegistrationSuccessful,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceFingerprint = deviceFingerprint,
            UserId = result.Value.UserId,
            Severity = SecurityEventSeverity.Low
        });

        _logger.LogInformation("User registered successfully: {UserId}", result.Value.UserId);
        
        return CreatedAtAction(nameof(GetProfile), new { id = result.Value.UserId }, result.Value);
    }

    /// <summary>
    /// Authenticates a user with comprehensive security checks and optional MFA.
    /// </summary>
    /// <param name="command">The login command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authentication result with tokens or MFA challenge.</returns>
    /// <response code="200">Authentication successful or MFA required.</response>
    /// <response code="400">Invalid credentials or security policy violation.</response>
    /// <response code="401">Authentication failed.</response>
    /// <response code="423">Account locked due to security violations.</response>
    /// <response code="429">Too many login attempts.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("login")]
    [EnableRateLimiting("LoginPolicy")]
    [ProducesResponseType(typeof(LoginResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(MfaChallengeResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.Locked)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken = default)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = GetUserAgent();
        var deviceFingerprint = await _deviceFingerprintService.GenerateFingerprintAsync(Request);

        _logger.LogInformation("Login attempt for {Email} from IP: {IpAddress}, Device: {DeviceFingerprint}", 
            command.Email, ipAddress, deviceFingerprint);

        // Check for suspicious activity
        var isSuspicious = await _securityMonitoringService.IsSuspiciousActivityAsync(
            ipAddress, userAgent, deviceFingerprint);

        if (isSuspicious)
        {
            await _securityMonitoringService.LogSecurityEventAsync(new SecurityEvent
            {
                EventType = SecurityEventType.SuspiciousLoginAttempt,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceFingerprint = deviceFingerprint,
                Details = "Suspicious activity detected",
                Severity = SecurityEventSeverity.High
            });

            _logger.LogWarning("Suspicious login attempt blocked from {IpAddress}", ipAddress);
            return BadRequest(new ErrorResponse { Error = "Login temporarily unavailable. Please try again later." });
        }

        // Enhanced command with security context
        var enhancedCommand = command with 
        { 
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceFingerprint = deviceFingerprint
        };

        var result = await _mediator.Send(enhancedCommand, cancellationToken);

        if (result.IsFailure)
        {
            await _securityMonitoringService.LogSecurityEventAsync(new SecurityEvent
            {
                EventType = SecurityEventType.LoginFailed,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceFingerprint = deviceFingerprint,
                Details = result.Error,
                Severity = SecurityEventSeverity.Medium
            });

            _logger.LogWarning("Login failed for {Email} from {IpAddress}: {Error}", 
                command.Email, ipAddress, result.Error);

            return result.Error!.Contains("locked") 
                ? StatusCode((int)HttpStatusCode.Locked, new ErrorResponse { Error = result.Error })
                : Unauthorized(new ErrorResponse { Error = result.Error });
        }

        // Check if MFA is required
        if (result.Value.RequiresMfa)
        {
            _logger.LogInformation("MFA challenge issued for user {UserId}", result.Value.UserId);
            return Ok(new MfaChallengeResponse
            {
                ChallengeId = result.Value.MfaChallengeId!,
                AvailableMethods = result.Value.AvailableMfaMethods!,
                Message = "Multi-factor authentication required."
            });
        }

        await _securityMonitoringService.LogSecurityEventAsync(new SecurityEvent
        {
            EventType = SecurityEventType.LoginSuccessful,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceFingerprint = deviceFingerprint,
            UserId = result.Value.UserId,
            Severity = SecurityEventSeverity.Low
        });

        _logger.LogInformation("User {UserId} logged in successfully from {IpAddress}", 
            result.Value.UserId, ipAddress);

        // Set secure HTTP-only cookies for tokens
        SetSecureTokenCookies(result.Value.AccessToken, result.Value.RefreshToken);

        return Ok(result.Value);
    }

    /// <summary>
    /// Verifies multi-factor authentication and completes the login process.
    /// </summary>
    /// <param name="command">The MFA verification command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authentication result with tokens.</returns>
    /// <response code="200">MFA verification successful.</response>
    /// <response code="400">Invalid MFA code or challenge expired.</response>
    /// <response code="401">MFA verification failed.</response>
    /// <response code="429">Too many MFA attempts.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("verify-mfa")]
    [EnableRateLimiting("MfaPolicy")]
    [ProducesResponseType(typeof(LoginResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaCommand command, CancellationToken cancellationToken = default)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = GetUserAgent();
        var deviceFingerprint = await _deviceFingerprintService.GenerateFingerprintAsync(Request);

        _logger.LogInformation("MFA verification attempt for challenge {ChallengeId} from {IpAddress}", 
            command.ChallengeId, ipAddress);

        // Enhanced command with security context
        var enhancedCommand = command with 
        { 
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceFingerprint = deviceFingerprint
        };

        var result = await _mediator.Send(enhancedCommand, cancellationToken);

        if (result.IsFailure)
        {
            await _securityMonitoringService.LogSecurityEventAsync(new SecurityEvent
            {
                EventType = SecurityEventType.MfaVerificationFailed,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceFingerprint = deviceFingerprint,
                Details = result.Error,
                Severity = SecurityEventSeverity.Medium
            });

            _logger.LogWarning("MFA verification failed for challenge {ChallengeId} from {IpAddress}: {Error}", 
                command.ChallengeId, ipAddress, result.Error);

            return Unauthorized(new ErrorResponse { Error = result.Error });
        }

        await _securityMonitoringService.LogSecurityEventAsync(new SecurityEvent
        {
            EventType = SecurityEventType.MfaVerificationSuccessful,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceFingerprint = deviceFingerprint,
            UserId = result.Value.UserId,
            Severity = SecurityEventSeverity.Low
        });

        _logger.LogInformation("MFA verification successful for user {UserId}", result.Value.UserId);

        // Set secure HTTP-only cookies for tokens
        SetSecureTokenCookies(result.Value.AccessToken, result.Value.RefreshToken);

        return Ok(result.Value);
    }

    /// <summary>
    /// Sets up multi-factor authentication for the authenticated user.
    /// </summary>
    /// <param name="command">The MFA setup command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The MFA setup result with QR code and backup codes.</returns>
    /// <response code="200">MFA setup successful.</response>
    /// <response code="400">Invalid setup request.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="409">MFA already enabled.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("setup-mfa")]
    [Authorize]
    [ProducesResponseType(typeof(SetupMfaResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> SetupMfa([FromBody] SetupMfaCommand command, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var ipAddress = GetClientIpAddress();

        _logger.LogInformation("MFA setup request from user {UserId} at {IpAddress}", userId, ipAddress);

        var enhancedCommand = command with { UserId = userId };
        var result = await _mediator.Send(enhancedCommand, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("MFA setup failed for user {UserId}: {Error}", userId, result.Error);
            
            return result.Error!.Contains("already enabled") 
                ? Conflict(new ErrorResponse { Error = result.Error })
                : BadRequest(new ErrorResponse { Error = result.Error });
        }

        await _securityMonitoringService.LogSecurityEventAsync(new SecurityEvent
        {
            EventType = SecurityEventType.MfaSetupCompleted,
            IpAddress = ipAddress,
            UserId = userId,
            Severity = SecurityEventSeverity.Low
        });

        _logger.LogInformation("MFA setup completed for user {UserId}", userId);
        return Ok(result.Value);
    }

    /// <summary>
    /// Refreshes the access token using a valid refresh token.
    /// </summary>
    /// <param name="command">The refresh token command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>New access and refresh tokens.</returns>
    /// <response code="200">Token refresh successful.</response>
    /// <response code="400">Invalid or expired refresh token.</response>
    /// <response code="401">Refresh token authentication failed.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("refresh")]
    [EnableRateLimiting("RefreshPolicy")]
    [ProducesResponseType(typeof(RefreshTokenResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = GetUserAgent();

        _logger.LogDebug("Token refresh attempt from {IpAddress}", ipAddress);

        // Try to get refresh token from cookie if not provided in body
        if (string.IsNullOrEmpty(command.RefreshToken))
        {
            command = command with { RefreshToken = Request.Cookies["refreshToken"] ?? string.Empty };
        }

        var enhancedCommand = command with 
        { 
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        var result = await _mediator.Send(enhancedCommand, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Token refresh failed from {IpAddress}: {Error}", ipAddress, result.Error);
            return Unauthorized(new ErrorResponse { Error = result.Error });
        }

        _logger.LogDebug("Token refresh successful for user {UserId}", result.Value.UserId);

        // Set new secure HTTP-only cookies
        SetSecureTokenCookies(result.Value.AccessToken, result.Value.RefreshToken);

        return Ok(result.Value);
    }

    /// <summary>
    /// Logs out the user and invalidates all tokens.
    /// </summary>
    /// <param name="command">The logout command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Logout confirmation.</returns>
    /// <response code="200">Logout successful.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(LogoutResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand command, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var ipAddress = GetClientIpAddress();

        _logger.LogInformation("Logout request from user {UserId} at {IpAddress}", userId, ipAddress);

        var enhancedCommand = command with { UserId = userId };
        var result = await _mediator.Send(enhancedCommand, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Logout failed for user {UserId}: {Error}", userId, result.Error);
            return BadRequest(new ErrorResponse { Error = result.Error });
        }

        await _securityMonitoringService.LogSecurityEventAsync(new SecurityEvent
        {
            EventType = SecurityEventType.LogoutSuccessful,
            IpAddress = ipAddress,
            UserId = userId,
            Severity = SecurityEventSeverity.Low
        });

        // Clear authentication cookies
        ClearTokenCookies();

        _logger.LogInformation("User {UserId} logged out successfully", userId);
        return Ok(result.Value);
    }

    /// <summary>
    /// Gets the current user's profile information.
    /// </summary>
    /// <param name="id">The user ID (from route).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user profile information.</returns>
    /// <response code="200">Profile retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="404">User not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("profile/{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetProfile([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();
        
        // Users can only access their own profile unless they have admin privileges
        if (id != currentUserId && !User.IsInRole("ADMIN"))
        {
            return Forbid();
        }

        // This would typically use a GetUserProfileQuery
        // For demonstration, returning a placeholder response
        await Task.Delay(1, cancellationToken);

        return Ok(new UserProfileResponse
        {
            Id = id,
            Email = "user@example.com",
            Username = "user123",
            FullName = "Demo User",
            MfaEnabled = true,
            AccountStatus = "Verified",
            LastLoginAt = DateTime.UtcNow.AddHours(-2),
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        });
    }

    /// <summary>
    /// Gets the client's IP address from the request.
    /// </summary>
    /// <returns>The client's IP address.</returns>
    private string GetClientIpAddress()
    {
        // Check for forwarded IP first (for load balancers/proxies)
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Gets the user agent from the request.
    /// </summary>
    /// <returns>The user agent string.</returns>
    private string GetUserAgent()
    {
        return Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
    }

    /// <summary>
    /// Gets the current authenticated user's ID.
    /// </summary>
    /// <returns>The user ID.</returns>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Sets secure HTTP-only cookies for authentication tokens.
    /// </summary>
    /// <param name="accessToken">The access token.</param>
    /// <param name="refreshToken">The refresh token.</param>
    private void SetSecureTokenCookies(string accessToken, string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, // Prevent XSS attacks
            Secure = true,   // HTTPS only
            SameSite = SameSiteMode.Strict, // CSRF protection
            Expires = DateTimeOffset.UtcNow.AddDays(7) // 7 days for refresh token
        };

        Response.Cookies.Append("accessToken", accessToken, cookieOptions with { Expires = DateTimeOffset.UtcNow.AddMinutes(15) });
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    /// <summary>
    /// Clears authentication cookies.
    /// </summary>
    private void ClearTokenCookies()
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(-1) // Expire immediately
        };

        Response.Cookies.Append("accessToken", "", cookieOptions);
        Response.Cookies.Append("refreshToken", "", cookieOptions);
    }
}

// Response DTOs for the authentication endpoints
public sealed record RegisterResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public bool RequiresEmailVerification { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record LoginResponse
{
    public Guid UserId { get; init; }
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public bool RequiresMfa { get; init; }
    public string? MfaChallengeId { get; init; }
    public List<string>? AvailableMfaMethods { get; init; }
}

public sealed record MfaChallengeResponse
{
    public string ChallengeId { get; init; } = string.Empty;
    public List<string> AvailableMethods { get; init; } = new();
    public string Message { get; init; } = string.Empty;
}

public sealed record SetupMfaResponse
{
    public string QrCodeData { get; init; } = string.Empty;
    public List<string> BackupCodes { get; init; } = new();
    public string Message { get; init; } = string.Empty;
}

public sealed record RefreshTokenResponse
{
    public Guid UserId { get; init; }
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}

public sealed record LogoutResponse
{
    public string Message { get; init; } = string.Empty;
    public DateTime LoggedOutAt { get; init; }
}

public sealed record UserProfileResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public bool MfaEnabled { get; init; }
    public string AccountStatus { get; init; } = string.Empty;
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record ErrorResponse
{
    public string Error { get; init; } = string.Empty;
    public string? Details { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}