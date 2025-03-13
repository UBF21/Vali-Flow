using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Interfaces.Specification;

namespace Vali_Flow.Classes.Specification;

public class IncludeExpression<T, TProperty> : IIncludeExpression<T> where T : class
{
    private Expression<Func<T, TProperty>> Expression { get; }

    public IncludeExpression(Expression<Func<T, TProperty>> expression)
    {
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    public IQueryable<T> ApplyInclude(IQueryable<T> query)
    {
        return query.Include(Expression);
    }
}