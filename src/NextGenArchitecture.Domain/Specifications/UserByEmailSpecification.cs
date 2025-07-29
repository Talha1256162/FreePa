using NextGenArchitecture.Domain.Entities;
using NextGenArchitecture.Domain.ValueObjects;
using NextGenArchitecture.SharedKernel.Specifications;

namespace NextGenArchitecture.Domain.Specifications;

/// <summary>
/// Specification for finding users by their email address.
/// Demonstrates the specification pattern for encapsulating query logic.
/// </summary>
public sealed class UserByEmailSpecification : BaseSpecification<User>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserByEmailSpecification"/> class.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    public UserByEmailSpecification(string email) 
        : base(user => user.Email == Email.Create(email))
    {
    }
}