using MediatR;
using NextGenArchitecture.SharedKernel.Results;

namespace NextGenArchitecture.Application.Users.Commands.CreateUser;

/// <summary>
/// Command to create a new user in the system.
/// Demonstrates CQRS pattern with MediatR for command handling.
/// </summary>
public sealed record CreateUserCommand : IRequest<Result<CreateUserResponse>>
{
    /// <summary>
    /// Gets the user's email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Gets the user's first name.
    /// </summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the user's last name.
    /// </summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the initial roles to assign to the user.
    /// </summary>
    public List<string> Roles { get; init; } = new();
}