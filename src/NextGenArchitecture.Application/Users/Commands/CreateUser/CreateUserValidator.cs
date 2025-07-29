using FluentValidation;
using NextGenArchitecture.Domain.ValueObjects;

namespace NextGenArchitecture.Application.Users.Commands.CreateUser;

/// <summary>
/// Validator for the CreateUserCommand using FluentValidation.
/// Ensures all business rules and data validation are enforced before processing the command.
/// </summary>
public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUserValidator"/> class.
    /// </summary>
    public CreateUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .Must(BeValidEmail)
            .WithMessage("Email format is invalid.");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.")
            .Length(2, 50)
            .WithMessage("First name must be between 2 and 50 characters.")
            .Matches("^[a-zA-Z\\s'-]+$")
            .WithMessage("First name can only contain letters, spaces, hyphens, and apostrophes.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .Length(2, 50)
            .WithMessage("Last name must be between 2 and 50 characters.")
            .Matches("^[a-zA-Z\\s'-]+$")
            .WithMessage("Last name can only contain letters, spaces, hyphens, and apostrophes.");

        RuleFor(x => x.Roles)
            .NotNull()
            .WithMessage("Roles collection cannot be null.");

        RuleForEach(x => x.Roles)
            .NotEmpty()
            .WithMessage("Role name cannot be empty.")
            .Length(2, 30)
            .WithMessage("Role name must be between 2 and 30 characters.")
            .Matches("^[A-Z_]+$")
            .WithMessage("Role name must be uppercase letters and underscores only.");

        RuleFor(x => x.Roles)
            .Must(HaveUniqueRoles)
            .WithMessage("Duplicate roles are not allowed.")
            .Must(NotExceedMaxRoles)
            .WithMessage("Maximum of 10 roles allowed per user.");
    }

    /// <summary>
    /// Validates if the email format is correct using the domain Email value object.
    /// </summary>
    /// <param name="email">The email to validate.</param>
    /// <returns>true if the email is valid; otherwise, false.</returns>
    private static bool BeValidEmail(string email)
    {
        return Email.IsValidEmail(email);
    }

    /// <summary>
    /// Validates that all roles in the collection are unique.
    /// </summary>
    /// <param name="roles">The roles collection to validate.</param>
    /// <returns>true if all roles are unique; otherwise, false.</returns>
    private static bool HaveUniqueRoles(List<string> roles)
    {
        return roles.Distinct(StringComparer.OrdinalIgnoreCase).Count() == roles.Count;
    }

    /// <summary>
    /// Validates that the number of roles doesn't exceed the maximum allowed.
    /// </summary>
    /// <param name="roles">The roles collection to validate.</param>
    /// <returns>true if the role count is within limits; otherwise, false.</returns>
    private static bool NotExceedMaxRoles(List<string> roles)
    {
        return roles.Count <= 10;
    }
}