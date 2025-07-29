using MediatR;
using Microsoft.Extensions.Logging;
using NextGenArchitecture.Application.Common.Interfaces;
using NextGenArchitecture.Domain.Entities;
using NextGenArchitecture.Domain.Specifications;
using NextGenArchitecture.SharedKernel.Results;

namespace NextGenArchitecture.Application.Users.Commands.CreateUser;

/// <summary>
/// Handler for the CreateUserCommand.
/// Implements comprehensive business logic, validation, and error handling for user creation.
/// </summary>
public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<CreateUserResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateUserHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUserHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work for data access.</param>
    /// <param name="logger">The logger for diagnostic information.</param>
    public CreateUserHandler(IUnitOfWork unitOfWork, ILogger<CreateUserHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Handles the CreateUserCommand and creates a new user.
    /// </summary>
    /// <param name="request">The create user command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the created user information or error details.</returns>
    public async Task<Result<CreateUserResponse>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating user with email: {Email}", request.Email);

        try
        {
            // Check if user with email already exists
            var userRepository = _unitOfWork.Repository<User>();
            var existingUserSpec = new UserByEmailSpecification(request.Email);
            var existingUser = await userRepository.GetFirstAsync(existingUserSpec, cancellationToken);

            if (existingUser != null)
            {
                _logger.LogWarning("User creation failed - email already exists: {Email}", request.Email);
                return Result.Failure<CreateUserResponse>("A user with this email address already exists.");
            }

            // Create new user entity
            var user = User.Create(request.Email, request.FirstName, request.LastName);

            // Add roles if specified
            foreach (var role in request.Roles)
            {
                try
                {
                    user.AddRole(role);
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning("Invalid role specified during user creation: {Role}. Error: {Error}", role, ex.Message);
                    return Result.Failure<CreateUserResponse>($"Invalid role: {role}");
                }
            }

            // Save user to database
            var addResult = await userRepository.AddAsync(user, cancellationToken);
            if (addResult.IsFailure)
            {
                _logger.LogError("Failed to add user to repository: {Error}", addResult.Error);
                return Result.Failure<CreateUserResponse>("Failed to create user in the database.");
            }

            // Commit changes
            var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (saveResult <= 0)
            {
                _logger.LogError("Failed to save user changes - no entities affected");
                return Result.Failure<CreateUserResponse>("Failed to save user changes.");
            }

            _logger.LogInformation("Successfully created user {UserId} with email: {Email}", user.Id, request.Email);

            // Create response
            var response = new CreateUserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Status = user.Status.ToString(),
                CreatedAt = user.CreatedAt,
                Roles = user.Roles.ToList()
            };

            return Result.Success(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("User creation failed due to invalid arguments: {Error}", ex.Message);
            return Result.Failure<CreateUserResponse>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("User creation failed due to invalid operation: {Error}", ex.Message);
            return Result.Failure<CreateUserResponse>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while creating user with email: {Email}", request.Email);
            return Result.Failure<CreateUserResponse>("An unexpected error occurred while creating the user.");
        }
    }
}