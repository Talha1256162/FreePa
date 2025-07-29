namespace NextGenArchitecture.SharedKernel.Abstractions;

/// <summary>
/// Interface for entities that support auditing capabilities.
/// Provides standardized tracking of creation and modification timestamps and users.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// Gets or sets the timestamp when the entity was created.
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who created the entity.
    /// </summary>
    string CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the entity was last modified.
    /// </summary>
    DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last modified the entity.
    /// </summary>
    string? ModifiedBy { get; set; }
}