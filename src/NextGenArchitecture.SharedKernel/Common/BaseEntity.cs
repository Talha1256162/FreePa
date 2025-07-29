using NextGenArchitecture.SharedKernel.Abstractions;
using NextGenArchitecture.SharedKernel.Events;

namespace NextGenArchitecture.SharedKernel.Common;

/// <summary>
/// Base implementation for all domain entities.
/// Provides auditing, soft deletion, and domain event capabilities.
/// This class serves as the foundation for all entities in the system.
/// </summary>
public abstract class BaseEntity : IEntity, IAuditable, ISoftDeletable
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEntity"/> class.
    /// </summary>
    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        CreatedBy = string.Empty; // Will be set by interceptors
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEntity"/> class with a specific ID.
    /// </summary>
    /// <param name="id">The unique identifier for the entity.</param>
    protected BaseEntity(Guid id) : this()
    {
        Id = id;
    }

    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public string CreatedBy { get; set; } = string.Empty;

    /// <inheritdoc />
    public DateTime? ModifiedAt { get; set; }

    /// <inheritdoc />
    public string? ModifiedBy { get; set; }

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }

    /// <inheritdoc />
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Gets the collection of domain events raised by this entity.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <inheritdoc />
    public void Delete(string deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        
        AddDomainEvent(new EntityDeletedEvent(Id, GetType().Name, deletedBy));
    }

    /// <inheritdoc />
    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        
        AddDomainEvent(new EntityRestoredEvent(Id, GetType().Name));
    }

    /// <summary>
    /// Adds a domain event to be published when the entity is saved.
    /// </summary>
    /// <param name="domainEvent">The domain event to add.</param>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Removes a domain event from the entity.
    /// </summary>
    /// <param name="domainEvent">The domain event to remove.</param>
    protected void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Clears all domain events from the entity.
    /// This is typically called after events have been published.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current entity.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns>true if the specified object is equal to the current entity; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        return Id.Equals(other.Id);
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current entity.</returns>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <summary>
    /// Determines whether two entities are equal.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns>true if the entities are equal; otherwise, false.</returns>
    public static bool operator ==(BaseEntity? left, BaseEntity? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two entities are not equal.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns>true if the entities are not equal; otherwise, false.</returns>
    public static bool operator !=(BaseEntity? left, BaseEntity? right)
    {
        return !Equals(left, right);
    }
}