using NextGenArchitecture.Domain.ValueObjects;
using NextGenArchitecture.SharedKernel.Events;

namespace NextGenArchitecture.Domain.Events;

/// <summary>
/// Domain event raised when a new user is created.
/// </summary>
public sealed class UserCreatedEvent : BaseDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserCreatedEvent"/> class.
    /// </summary>
    /// <param name="userId">The unique identifier of the created user.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="fullName">The user's full name.</param>
    public UserCreatedEvent(Guid userId, Email email, string fullName)
    {
        UserId = userId;
        Email = email;
        FullName = fullName;
    }

    /// <summary>
    /// Gets the unique identifier of the created user.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Gets the user's email address.
    /// </summary>
    public Email Email { get; }

    /// <summary>
    /// Gets the user's full name.
    /// </summary>
    public string FullName { get; }
}