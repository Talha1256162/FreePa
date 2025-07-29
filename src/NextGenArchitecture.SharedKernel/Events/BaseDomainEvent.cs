namespace NextGenArchitecture.SharedKernel.Events;

/// <summary>
/// Base implementation for domain events.
/// Provides common properties and behavior for all domain events in the system.
/// </summary>
public abstract class BaseDomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseDomainEvent"/> class.
    /// </summary>
    protected BaseDomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
        EventType = GetType().Name;
        Version = 1; // Default version, can be overridden in derived classes
    }

    /// <inheritdoc />
    public Guid EventId { get; private set; }

    /// <inheritdoc />
    public DateTime OccurredOn { get; private set; }

    /// <inheritdoc />
    public string EventType { get; private set; }

    /// <inheritdoc />
    public virtual int Version { get; protected set; }

    /// <summary>
    /// Returns a string representation of the domain event.
    /// </summary>
    /// <returns>A string that represents the current domain event.</returns>
    public override string ToString()
    {
        return $"{EventType} (Id: {EventId}, OccurredOn: {OccurredOn:yyyy-MM-dd HH:mm:ss} UTC, Version: {Version})";
    }
}