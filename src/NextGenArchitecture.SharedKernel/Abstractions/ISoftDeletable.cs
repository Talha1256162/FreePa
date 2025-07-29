namespace NextGenArchitecture.SharedKernel.Abstractions;

/// <summary>
/// Interface for entities that support soft deletion.
/// Soft deletion allows marking entities as deleted without physically removing them from the database.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Gets or sets a value indicating whether the entity is deleted.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the entity was deleted.
    /// </summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who deleted the entity.
    /// </summary>
    string? DeletedBy { get; set; }

    /// <summary>
    /// Marks the entity as deleted.
    /// </summary>
    /// <param name="deletedBy">The identifier of the user performing the deletion.</param>
    void Delete(string deletedBy);

    /// <summary>
    /// Restores a soft-deleted entity.
    /// </summary>
    void Restore();
}