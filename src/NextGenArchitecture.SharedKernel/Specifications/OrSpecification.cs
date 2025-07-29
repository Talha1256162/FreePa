using System.Linq.Expressions;

namespace NextGenArchitecture.SharedKernel.Specifications;

/// <summary>
/// Specification that represents the logical OR of two specifications.
/// This allows for complex specification composition using fluent syntax.
/// </summary>
/// <typeparam name="T">The type of entity the specification applies to.</typeparam>
public sealed class OrSpecification<T> : BaseSpecification<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrSpecification{T}"/> class.
    /// </summary>
    /// <param name="left">The left specification.</param>
    /// <param name="right">The right specification.</param>
    public OrSpecification(ISpecification<T> left, ISpecification<T> right)
        : base(CombineExpressions(left.Criteria, right.Criteria))
    {
        _left = left;
        _right = right;
    }

    /// <inheritdoc />
    public override bool IsSatisfiedBy(T entity)
    {
        return _left.IsSatisfiedBy(entity) || _right.IsSatisfiedBy(entity);
    }

    /// <summary>
    /// Combines two expressions using logical OR.
    /// </summary>
    /// <param name="left">The left expression.</param>
    /// <param name="right">The right expression.</param>
    /// <returns>A combined expression representing logical OR of both expressions.</returns>
    private static Expression<Func<T, bool>> CombineExpressions(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        
        var leftBody = ReplaceParameter(left.Body, left.Parameters[0], parameter);
        var rightBody = ReplaceParameter(right.Body, right.Parameters[0], parameter);
        
        var orExpression = Expression.OrElse(leftBody, rightBody);
        
        return Expression.Lambda<Func<T, bool>>(orExpression, parameter);
    }

    /// <summary>
    /// Replaces parameter in expression with a new parameter.
    /// </summary>
    /// <param name="expression">The expression to modify.</param>
    /// <param name="oldParameter">The old parameter to replace.</param>
    /// <param name="newParameter">The new parameter to use.</param>
    /// <returns>The modified expression with replaced parameter.</returns>
    private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        return new ParameterReplacerVisitor(oldParameter, newParameter).Visit(expression);
    }

    /// <summary>
    /// Expression visitor for replacing parameters in expressions.
    /// </summary>
    private sealed class ParameterReplacerVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ParameterReplacerVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}