using MediatR;

namespace NextGenArchitecture.SharedKernel.Events;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something significant that happened in the domain.
/// They are used to decouple domain logic and enable eventual consistency.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Gets the unique identifier for the domain event.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the timestamp when the domain event occurred.
    /// </summary>
    DateTime OccurredOn { get; }

    /// <summary>
    /// Gets the name of the event type.
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// Gets the version of the event schema.
    /// Used for event evolution and backward compatibility.
    /// </summary>
    int Version { get; }
}