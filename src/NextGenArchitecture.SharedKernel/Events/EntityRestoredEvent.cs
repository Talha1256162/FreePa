namespace NextGenArchitecture.SharedKernel.Events;

/// <summary>
/// Domain event raised when a soft-deleted entity is restored.
/// This event can be used to trigger restoration operations, notifications, or audit logging.
/// </summary>
public sealed class EntityRestoredEvent : BaseDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityRestoredEvent"/> class.
    /// </summary>
    /// <param name="entityId">The unique identifier of the restored entity.</param>
    /// <param name="entityType">The type name of the restored entity.</param>
    public EntityRestoredEvent(Guid entityId, string entityType)
    {
        EntityId = entityId;
        EntityType = entityType;
    }

    /// <summary>
    /// Gets the unique identifier of the restored entity.
    /// </summary>
    public Guid EntityId { get; }

    /// <summary>
    /// Gets the type name of the restored entity.
    /// </summary>
    public string EntityType { get; }
}