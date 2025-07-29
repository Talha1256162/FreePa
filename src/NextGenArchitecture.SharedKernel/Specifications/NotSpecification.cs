using System.Linq.Expressions;

namespace NextGenArchitecture.SharedKernel.Specifications;

/// <summary>
/// Specification that represents the logical NOT of another specification.
/// This allows for negation of complex specifications using fluent syntax.
/// </summary>
/// <typeparam name="T">The type of entity the specification applies to.</typeparam>
public sealed class NotSpecification<T> : BaseSpecification<T>
{
    private readonly ISpecification<T> _specification;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotSpecification{T}"/> class.
    /// </summary>
    /// <param name="specification">The specification to negate.</param>
    public NotSpecification(ISpecification<T> specification)
        : base(NegateExpression(specification.Criteria))
    {
        _specification = specification;
    }

    /// <inheritdoc />
    public override bool IsSatisfiedBy(T entity)
    {
        return !_specification.IsSatisfiedBy(entity);
    }

    /// <summary>
    /// Negates the given expression.
    /// </summary>
    /// <param name="expression">The expression to negate.</param>
    /// <returns>A negated expression.</returns>
    private static Expression<Func<T, bool>> NegateExpression(Expression<Func<T, bool>> expression)
    {
        var parameter = expression.Parameters[0];
        var notExpression = Expression.Not(expression.Body);
        
        return Expression.Lambda<Func<T, bool>>(notExpression, parameter);
    }
}