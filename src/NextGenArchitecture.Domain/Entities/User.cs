using NextGenArchitecture.Domain.ValueObjects;
using NextGenArchitecture.SharedKernel.Common;
using NextGenArchitecture.SharedKernel.Events;

namespace NextGenArchitecture.Domain.Entities;

/// <summary>
/// Represents a user in the system.
/// This entity demonstrates rich domain modeling with business rules, validation, and domain events.
/// </summary>
public sealed class User : BaseEntity
{
    private readonly List<string> _roles = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="firstName">The user's first name.</param>
    /// <param name="lastName">The user's last name.</param>
    private User(Email email, string firstName, string lastName)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Status = UserStatus.Active;
        EmailVerified = false;
        LastLoginAt = null;
        FailedLoginAttempts = 0;
        
        AddDomainEvent(new UserCreatedEvent(Id, email, $"{firstName} {lastName}"));
    }

    /// <summary>
    /// Gets the user's email address.
    /// </summary>
    public Email Email { get; private set; }

    /// <summary>
    /// Gets the user's first name.
    /// </summary>
    public string FirstName { get; private set; }

    /// <summary>
    /// Gets the user's last name.
    /// </summary>
    public string LastName { get; private set; }

    /// <summary>
    /// Gets the user's full name.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// Gets the user's status.
    /// </summary>
    public UserStatus Status { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the user's email is verified.
    /// </summary>
    public bool EmailVerified { get; private set; }

    /// <summary>
    /// Gets the timestamp of the user's last login.
    /// </summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>
    /// Gets the number of failed login attempts.
    /// </summary>
    public int FailedLoginAttempts { get; private set; }

    /// <summary>
    /// Gets the user's roles.
    /// </summary>
    public IReadOnlyCollection<string> Roles => _roles.AsReadOnly();

    /// <summary>
    /// Creates a new User instance.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="firstName">The user's first name.</param>
    /// <param name="lastName">The user's last name.</param>
    /// <returns>A new User instance.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public static User Create(string email, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be null or empty.", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be null or empty.", nameof(lastName));

        var emailValue = Email.Create(email);
        
        return new User(emailValue, firstName.Trim(), lastName.Trim());
    }

    /// <summary>
    /// Updates the user's email address.
    /// </summary>
    /// <param name="newEmail">The new email address.</param>
    /// <exception cref="InvalidOperationException">Thrown when the user is not active.</exception>
    public void UpdateEmail(string newEmail)
    {
        EnsureUserIsActive();
        
        var emailValue = Email.Create(newEmail);
        
        if (Email.Equals(emailValue))
            return;

        var oldEmail = Email;
        Email = emailValue;
        EmailVerified = false; // Require re-verification for new email
        
        AddDomainEvent(new UserEmailChangedEvent(Id, oldEmail, Email));
    }

    /// <summary>
    /// Updates the user's name.
    /// </summary>
    /// <param name="firstName">The new first name.</param>
    /// <param name="lastName">The new last name.</param>
    /// <exception cref="InvalidOperationException">Thrown when the user is not active.</exception>
    public void UpdateName(string firstName, string lastName)
    {
        EnsureUserIsActive();
        
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be null or empty.", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be null or empty.", nameof(lastName));

        var oldFullName = FullName;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        
        AddDomainEvent(new UserNameChangedEvent(Id, oldFullName, FullName));
    }

    /// <summary>
    /// Verifies the user's email address.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the email is already verified or user is not active.</exception>
    public void VerifyEmail()
    {
        EnsureUserIsActive();
        
        if (EmailVerified)
            throw new InvalidOperationException("Email is already verified.");

        EmailVerified = true;
        AddDomainEvent(new UserEmailVerifiedEvent(Id, Email));
    }

    /// <summary>
    /// Records a successful login.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the user is not active.</exception>
    public void RecordSuccessfulLogin()
    {
        EnsureUserIsActive();
        
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0; // Reset failed attempts on successful login
        
        AddDomainEvent(new UserLoggedInEvent(Id, Email, LastLoginAt.Value));
    }

    /// <summary>
    /// Records a failed login attempt.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the user is not active.</exception>
    public void RecordFailedLoginAttempt()
    {
        EnsureUserIsActive();
        
        FailedLoginAttempts++;
        
        // Auto-suspend user after 5 failed attempts
        if (FailedLoginAttempts >= 5)
        {
            Suspend();
            AddDomainEvent(new UserSuspendedDueToFailedLoginsEvent(Id, Email, FailedLoginAttempts));
        }
        else
        {
            AddDomainEvent(new UserLoginFailedEvent(Id, Email, FailedLoginAttempts));
        }
    }

    /// <summary>
    /// Adds a role to the user.
    /// </summary>
    /// <param name="role">The role to add.</param>
    /// <exception cref="InvalidOperationException">Thrown when the user is not active or role already exists.</exception>
    public void AddRole(string role)
    {
        EnsureUserIsActive();
        
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be null or empty.", nameof(role));

        var normalizedRole = role.Trim().ToUpperInvariant();
        
        if (_roles.Contains(normalizedRole))
            throw new InvalidOperationException($"User already has role: {role}");

        _roles.Add(normalizedRole);
        AddDomainEvent(new UserRoleAddedEvent(Id, Email, role));
    }

    /// <summary>
    /// Removes a role from the user.
    /// </summary>
    /// <param name="role">The role to remove.</param>
    /// <exception cref="InvalidOperationException">Thrown when the user is not active or role doesn't exist.</exception>
    public void RemoveRole(string role)
    {
        EnsureUserIsActive();
        
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be null or empty.", nameof(role));

        var normalizedRole = role.Trim().ToUpperInvariant();
        
        if (!_roles.Contains(normalizedRole))
            throw new InvalidOperationException($"User doesn't have role: {role}");

        _roles.Remove(normalizedRole);
        AddDomainEvent(new UserRoleRemovedEvent(Id, Email, role));
    }

    /// <summary>
    /// Checks if the user has the specified role.
    /// </summary>
    /// <param name="role">The role to check.</param>
    /// <returns>true if the user has the role; otherwise, false.</returns>
    public bool HasRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return false;

        return _roles.Contains(role.Trim().ToUpperInvariant());
    }

    /// <summary>
    /// Activates the user.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the user is already active.</exception>
    public void Activate()
    {
        if (Status == UserStatus.Active)
            throw new InvalidOperationException("User is already active.");

        Status = UserStatus.Active;
        FailedLoginAttempts = 0; // Reset failed attempts when activating
        
        AddDomainEvent(new UserActivatedEvent(Id, Email));
    }

    /// <summary>
    /// Suspends the user.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the user is already suspended.</exception>
    public void Suspend()
    {
        if (Status == UserStatus.Suspended)
            throw new InvalidOperationException("User is already suspended.");

        Status = UserStatus.Suspended;
        AddDomainEvent(new UserSuspendedEvent(Id, Email));
    }

    /// <summary>
    /// Deactivates the user.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the user is already deactivated.</exception>
    public void Deactivate()
    {
        if (Status == UserStatus.Deactivated)
            throw new InvalidOperationException("User is already deactivated.");

        Status = UserStatus.Deactivated;
        AddDomainEvent(new UserDeactivatedEvent(Id, Email));
    }

    /// <summary>
    /// Ensures the user is in an active state for operations.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the user is not active.</exception>
    private void EnsureUserIsActive()
    {
        if (Status != UserStatus.Active)
            throw new InvalidOperationException($"Operation not allowed. User status is: {Status}");
    }
}

/// <summary>
/// Represents the status of a user.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// The user is active and can perform operations.
    /// </summary>
    Active = 1,

    /// <summary>
    /// The user is suspended and cannot perform operations.
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// The user is deactivated and cannot perform operations.
    /// </summary>
    Deactivated = 3
}