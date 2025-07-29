using MediatR;
using NextGenArchitecture.SharedKernel.Results;

namespace NextGenArchitecture.Application.Users.Queries.GetUsers;

/// <summary>
/// Query to retrieve users with filtering, sorting, and pagination.
/// Demonstrates CQRS query pattern with comprehensive search capabilities.
/// </summary>
public sealed record GetUsersQuery : IRequest<Result<GetUsersResponse>>
{
    /// <summary>
    /// Gets the search term for filtering users by name or email.
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Gets the status filter for users.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Gets the role filter for users.
    /// </summary>
    public string? Role { get; init; }

    /// <summary>
    /// Gets a value indicating whether to include only email verified users.
    /// </summary>
    public bool? EmailVerified { get; init; }

    /// <summary>
    /// Gets the field to sort by.
    /// </summary>
    public string SortBy { get; init; } = "CreatedAt";

    /// <summary>
    /// Gets the sort direction (asc or desc).
    /// </summary>
    public string SortDirection { get; init; } = "desc";

    /// <summary>
    /// Gets the page number (1-based).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; init; } = 20;
}