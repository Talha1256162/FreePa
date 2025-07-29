using System.Linq.Expressions;

namespace NextGenArchitecture.SharedKernel.Specifications;

/// <summary>
/// Interface for the Specification pattern.
/// Specifications encapsulate business rules and can be combined using logical operators.
/// They provide a clean way to express domain rules and can be used for validation and querying.
/// </summary>
/// <typeparam name="T">The type of entity the specification applies to.</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Gets the expression tree that represents the specification's criteria.
    /// This can be used with LINQ providers like Entity Framework for database queries.
    /// </summary>
    Expression<Func<T, bool>> Criteria { get; }

    /// <summary>
    /// Gets the list of include expressions for eager loading related entities.
    /// </summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Gets the list of include expressions for eager loading related entities as strings.
    /// This is useful for complex navigation properties that can't be expressed as expressions.
    /// </summary>
    List<string> IncludeStrings { get; }

    /// <summary>
    /// Gets the order by expression for sorting results.
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Gets the order by descending expression for sorting results.
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Gets the group by expression for grouping results.
    /// </summary>
    Expression<Func<T, object>>? GroupBy { get; }

    /// <summary>
    /// Gets the number of results to take (for pagination).
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Gets the number of results to skip (for pagination).
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Gets a value indicating whether paging is enabled.
    /// </summary>
    bool IsPagingEnabled { get; }

    /// <summary>
    /// Determines whether the specified entity satisfies the specification.
    /// </summary>
    /// <param name="entity">The entity to evaluate.</param>
    /// <returns>true if the entity satisfies the specification; otherwise, false.</returns>
    bool IsSatisfiedBy(T entity);
}