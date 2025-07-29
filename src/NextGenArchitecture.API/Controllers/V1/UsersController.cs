using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextGenArchitecture.Application.Users.Commands.CreateUser;
using NextGenArchitecture.Application.Users.Queries.GetUsers;
using NextGenArchitecture.SharedKernel.Results;
using System.Net;

namespace NextGenArchitecture.API.Controllers.V1;

/// <summary>
/// Controller for managing users in the system.
/// Demonstrates RESTful API design with CQRS, comprehensive validation, and proper HTTP status codes.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[Produces("application/json")]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsersController"/> class.
    /// </summary>
    /// <param name="mediator">The mediator for handling commands and queries.</param>
    /// <param name="logger">The logger for diagnostic information.</param>
    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new user in the system.
    /// </summary>
    /// <param name="command">The user creation command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created user information.</returns>
    /// <response code="201">User created successfully.</response>
    /// <response code="400">Invalid request data or business rule violation.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="409">User with email already exists.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost]
    [ProducesResponseType(typeof(CreateUserResponse), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.Conflict)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating user with email: {Email}", command.Email);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("User creation failed: {Error}", result.Error);
            
            // Return appropriate status code based on error type
            if (result.Error!.Contains("already exists"))
            {
                return Conflict(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        _logger.LogInformation("User created successfully with ID: {UserId}", result.Value.Id);
        
        return CreatedAtAction(
            nameof(GetUser), 
            new { id = result.Value.Id }, 
            result.Value);
    }

    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user information.</returns>
    /// <response code="200">User retrieved successfully.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">User not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CreateUserResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetUser([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving user with ID: {UserId}", id);

        // This would typically use a GetUserQuery
        // For demonstration, returning a placeholder response
        await Task.Delay(1, cancellationToken); // Simulate async operation

        return Ok(new
        {
            id,
            email = "demo@example.com",
            fullName = "Demo User",
            status = "Active",
            createdAt = DateTime.UtcNow.AddDays(-30),
            roles = new[] { "USER" }
        });
    }

    /// <summary>
    /// Retrieves a paginated list of users with optional filtering.
    /// </summary>
    /// <param name="query">The query parameters for filtering and pagination.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of users.</returns>
    /// <response code="200">Users retrieved successfully.</response>
    /// <response code="400">Invalid query parameters.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet]
    [ProducesResponseType(typeof(GetUsersResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetUsers([FromQuery] GetUsersQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving users with search term: {SearchTerm}, page: {Page}, pageSize: {PageSize}", 
            query.SearchTerm, query.Page, query.PageSize);

        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to retrieve users: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="id">The unique identifier of the user to update.</param>
    /// <param name="request">The update request data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated user information.</returns>
    /// <response code="200">User updated successfully.</response>
    /// <response code="400">Invalid request data or business rule violation.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">User not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CreateUserResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> UpdateUser([FromRoute] Guid id, [FromBody] object request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating user with ID: {UserId}", id);

        // This would typically use an UpdateUserCommand
        // For demonstration, returning a placeholder response
        await Task.Delay(1, cancellationToken); // Simulate async operation

        return Ok(new
        {
            id,
            email = "updated@example.com",
            fullName = "Updated User",
            status = "Active",
            createdAt = DateTime.UtcNow.AddDays(-30),
            roles = new[] { "USER" }
        });
    }

    /// <summary>
    /// Soft deletes a user from the system.
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content on successful deletion.</returns>
    /// <response code="204">User deleted successfully.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">User not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> DeleteUser([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting user with ID: {UserId}", id);

        // This would typically use a DeleteUserCommand
        // For demonstration, simulating successful deletion
        await Task.Delay(1, cancellationToken); // Simulate async operation

        return NoContent();
    }

    /// <summary>
    /// Adds a role to a user.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="role">The role to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content on successful role addition.</returns>
    /// <response code="204">Role added successfully.</response>
    /// <response code="400">Invalid role or business rule violation.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">User not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost("{id:guid}/roles/{role}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> AddRole([FromRoute] Guid id, [FromRoute] string role, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding role {Role} to user {UserId}", role, id);

        // This would typically use an AddUserRoleCommand
        // For demonstration, simulating successful role addition
        await Task.Delay(1, cancellationToken); // Simulate async operation

        return NoContent();
    }

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="role">The role to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>No content on successful role removal.</returns>
    /// <response code="204">Role removed successfully.</response>
    /// <response code="400">Invalid role or business rule violation.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">User or role not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpDelete("{id:guid}/roles/{role}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> RemoveRole([FromRoute] Guid id, [FromRoute] string role, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing role {Role} from user {UserId}", role, id);

        // This would typically use a RemoveUserRoleCommand
        // For demonstration, simulating successful role removal
        await Task.Delay(1, cancellationToken); // Simulate async operation

        return NoContent();
    }
}