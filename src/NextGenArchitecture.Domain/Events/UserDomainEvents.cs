using NextGenArchitecture.Domain.ValueObjects;
using NextGenArchitecture.SharedKernel.Events;

namespace NextGenArchitecture.Domain.Events;

public sealed class UserNameChangedEvent : BaseDomainEvent
{
    public UserNameChangedEvent(Guid userId, string oldFullName, string newFullName)
    {
        UserId = userId;
        OldFullName = oldFullName;
        NewFullName = newFullName;
    }

    public Guid UserId { get; }
    public string OldFullName { get; }
    public string NewFullName { get; }
}

public sealed class UserEmailVerifiedEvent : BaseDomainEvent
{
    public UserEmailVerifiedEvent(Guid userId, Email email)
    {
        UserId = userId;
        Email = email;
    }

    public Guid UserId { get; }
    public Email Email { get; }
}

public sealed class UserLoggedInEvent : BaseDomainEvent
{
    public UserLoggedInEvent(Guid userId, Email email, DateTime loginTime)
    {
        UserId = userId;
        Email = email;
        LoginTime = loginTime;
    }

    public Guid UserId { get; }
    public Email Email { get; }
    public DateTime LoginTime { get; }
}

public sealed class UserLoginFailedEvent : BaseDomainEvent
{
    public UserLoginFailedEvent(Guid userId, Email email, int failedAttempts)
    {
        UserId = userId;
        Email = email;
        FailedAttempts = failedAttempts;
    }

    public Guid UserId { get; }
    public Email Email { get; }
    public int FailedAttempts { get; }
}

public sealed class UserSuspendedDueToFailedLoginsEvent : BaseDomainEvent
{
    public UserSuspendedDueToFailedLoginsEvent(Guid userId, Email email, int failedAttempts)
    {
        UserId = userId;
        Email = email;
        FailedAttempts = failedAttempts;
    }

    public Guid UserId { get; }
    public Email Email { get; }
    public int FailedAttempts { get; }
}

public sealed class UserRoleAddedEvent : BaseDomainEvent
{
    public UserRoleAddedEvent(Guid userId, Email email, string role)
    {
        UserId = userId;
        Email = email;
        Role = role;
    }

    public Guid UserId { get; }
    public Email Email { get; }
    public string Role { get; }
}

public sealed class UserRoleRemovedEvent : BaseDomainEvent
{
    public UserRoleRemovedEvent(Guid userId, Email email, string role)
    {
        UserId = userId;
        Email = email;
        Role = role;
    }

    public Guid UserId { get; }
    public Email Email { get; }
    public string Role { get; }
}

public sealed class UserActivatedEvent : BaseDomainEvent
{
    public UserActivatedEvent(Guid userId, Email email)
    {
        UserId = userId;
        Email = email;
    }

    public Guid UserId { get; }
    public Email Email { get; }
}

public sealed class UserSuspendedEvent : BaseDomainEvent
{
    public UserSuspendedEvent(Guid userId, Email email)
    {
        UserId = userId;
        Email = email;
    }

    public Guid UserId { get; }
    public Email Email { get; }
}

public sealed class UserDeactivatedEvent : BaseDomainEvent
{
    public UserDeactivatedEvent(Guid userId, Email email)
    {
        UserId = userId;
        Email = email;
    }

    public Guid UserId { get; }
    public Email Email { get; }
}