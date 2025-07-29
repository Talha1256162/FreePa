namespace NextGenArchitecture.Application.Users.Commands.CreateUser;

/// <summary>
/// Response model for the create user command.
/// Contains the essential information about the newly created user.
/// </summary>
public sealed record CreateUserResponse
{
    /// <summary>
    /// Gets the unique identifier of the created user.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the user's email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Gets the user's full name.
    /// </summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the user's status.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the user was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the roles assigned to the user.
    /// </summary>
    public List<string> Roles { get; init; } = new();
}