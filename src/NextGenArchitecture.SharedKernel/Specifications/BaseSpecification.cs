using System.Linq.Expressions;

namespace NextGenArchitecture.SharedKernel.Specifications;

/// <summary>
/// Base implementation of the Specification pattern with fluent API support.
/// Provides a foundation for creating complex, composable business rules and queries.
/// </summary>
/// <typeparam name="T">The type of entity the specification applies to.</typeparam>
public abstract class BaseSpecification<T> : ISpecification<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSpecification{T}"/> class.
    /// </summary>
    /// <param name="criteria">The criteria expression for the specification.</param>
    protected BaseSpecification(Expression<Func<T, bool>>? criteria = null)
    {
        Criteria = criteria ?? (_ => true);
        Includes = new List<Expression<Func<T, object>>>();
        IncludeStrings = new List<string>();
    }

    /// <inheritdoc />
    public Expression<Func<T, bool>> Criteria { get; }

    /// <inheritdoc />
    public List<Expression<Func<T, object>>> Includes { get; }

    /// <inheritdoc />
    public List<string> IncludeStrings { get; }

    /// <inheritdoc />
    public Expression<Func<T, object>>? OrderBy { get; private set; }

    /// <inheritdoc />
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    /// <inheritdoc />
    public Expression<Func<T, object>>? GroupBy { get; private set; }

    /// <inheritdoc />
    public int? Take { get; private set; }

    /// <inheritdoc />
    public int? Skip { get; private set; }

    /// <inheritdoc />
    public bool IsPagingEnabled => Skip.HasValue;

    /// <inheritdoc />
    public virtual bool IsSatisfiedBy(T entity)
    {
        var compiledExpression = Criteria.Compile();
        return compiledExpression(entity);
    }

    /// <summary>
    /// Adds an include expression for eager loading related entities.
    /// </summary>
    /// <param name="includeExpression">The include expression.</param>
    /// <returns>The current specification instance for method chaining.</returns>
    protected virtual BaseSpecification<T> AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
        return this;
    }

    /// <summary>
    /// Adds an include string for eager loading related entities.
    /// </summary>
    /// <param name="includeString">The include string.</param>
    /// <returns>The current specification instance for method chaining.</returns>
    protected virtual BaseSpecification<T> AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
        return this;
    }

    /// <summary>
    /// Adds an order by expression for sorting results in ascending order.
    /// </summary>
    /// <param name="orderByExpression">The order by expression.</param>
    /// <returns>The current specification instance for method chaining.</returns>
    protected virtual BaseSpecification<T> ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
        return this;
    }

    /// <summary>
    /// Adds an order by expression for sorting results in descending order.
    /// </summary>
    /// <param name="orderByDescExpression">The order by descending expression.</param>
    /// <returns>The current specification instance for method chaining.</returns>
    protected virtual BaseSpecification<T> ApplyOrderByDescending(Expression<Func<T, object>> orderByDescExpression)
    {
        OrderByDescending = orderByDescExpression;
        return this;
    }

    /// <summary>
    /// Adds a group by expression for grouping results.
    /// </summary>
    /// <param name="groupByExpression">The group by expression.</param>
    /// <returns>The current specification instance for method chaining.</returns>
    protected virtual BaseSpecification<T> ApplyGroupBy(Expression<Func<T, object>> groupByExpression)
    {
        GroupBy = groupByExpression;
        return this;
    }

    /// <summary>
    /// Applies paging to the specification.
    /// </summary>
    /// <param name="skip">The number of results to skip.</param>
    /// <param name="take">The number of results to take.</param>
    /// <returns>The current specification instance for method chaining.</returns>
    protected virtual BaseSpecification<T> ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        return this;
    }

    /// <summary>
    /// Combines this specification with another using logical AND.
    /// </summary>
    /// <param name="specification">The specification to combine with.</param>
    /// <returns>A new specification representing the logical AND of both specifications.</returns>
    public virtual ISpecification<T> And(ISpecification<T> specification)
    {
        return new AndSpecification<T>(this, specification);
    }

    /// <summary>
    /// Combines this specification with another using logical OR.
    /// </summary>
    /// <param name="specification">The specification to combine with.</param>
    /// <returns>A new specification representing the logical OR of both specifications.</returns>
    public virtual ISpecification<T> Or(ISpecification<T> specification)
    {
        return new OrSpecification<T>(this, specification);
    }

    /// <summary>
    /// Creates the logical NOT of this specification.
    /// </summary>
    /// <returns>A new specification representing the logical NOT of this specification.</returns>
    public virtual ISpecification<T> Not()
    {
        return new NotSpecification<T>(this);
    }
}