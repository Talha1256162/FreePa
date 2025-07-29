using NextGenArchitecture.Domain.ValueObjects;
using NextGenArchitecture.SharedKernel.Events;

namespace NextGenArchitecture.Domain.Events;

public sealed class UserEmailChangedEvent : BaseDomainEvent
{
    public UserEmailChangedEvent(Guid userId, Email oldEmail, Email newEmail)
    {
        UserId = userId;
        OldEmail = oldEmail;
        NewEmail = newEmail;
    }

    public Guid UserId { get; }
    public Email OldEmail { get; }
    public Email NewEmail { get; }
}