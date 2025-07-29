namespace NextGenArchitecture.SharedKernel.Events;

/// <summary>
/// Domain event raised when an entity is soft deleted.
/// This event can be used to trigger cleanup operations, notifications, or audit logging.
/// </summary>
public sealed class EntityDeletedEvent : BaseDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityDeletedEvent"/> class.
    /// </summary>
    /// <param name="entityId">The unique identifier of the deleted entity.</param>
    /// <param name="entityType">The type name of the deleted entity.</param>
    /// <param name="deletedBy">The identifier of the user who deleted the entity.</param>
    public EntityDeletedEvent(Guid entityId, string entityType, string deletedBy)
    {
        EntityId = entityId;
        EntityType = entityType;
        DeletedBy = deletedBy;
    }

    /// <summary>
    /// Gets the unique identifier of the deleted entity.
    /// </summary>
    public Guid EntityId { get; }

    /// <summary>
    /// Gets the type name of the deleted entity.
    /// </summary>
    public string EntityType { get; }

    /// <summary>
    /// Gets the identifier of the user who deleted the entity.
    /// </summary>
    public string DeletedBy { get; }
}