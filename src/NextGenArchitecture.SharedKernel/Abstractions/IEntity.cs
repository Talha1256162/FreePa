namespace NextGenArchitecture.SharedKernel.Abstractions;

/// <summary>
/// Base interface for all entities in the domain.
/// Provides a common contract for entity identification.
/// </summary>
/// <typeparam name="TKey">The type of the entity's primary key.</typeparam>
public interface IEntity<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    TKey Id { get; set; }
}

/// <summary>
/// Base interface for entities with Guid primary keys.
/// This is the most common entity type in the architecture.
/// </summary>
public interface IEntity : IEntity<Guid>
{
}