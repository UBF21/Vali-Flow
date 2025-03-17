using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Vali_Flow.Interfaces.Options;

namespace Vali_Flow.Classes.Options;

public class EfThenInclude<TParent, TPreviousProperty, TProperty> : IEfThenInclude<TParent, TPreviousProperty, TProperty>
    where TParent : class
    where TPreviousProperty : class
{
    private readonly Expression<Func<TPreviousProperty, TProperty>> _thenIncludeExpression;
   
    public EfThenInclude(Expression<Func<TPreviousProperty, TProperty>> thenIncludeExpression)
    {
        _thenIncludeExpression = thenIncludeExpression ?? throw new ArgumentNullException(nameof(thenIncludeExpression));
    }
    
    public IQueryable<TParent> ApplyThenInclude(IQueryable<TParent> query)
    {
        IIncludableQueryable<TParent,TPreviousProperty> includableQuery = (IIncludableQueryable<TParent, TPreviousProperty>)query;
        return includableQuery.ThenInclude(_thenIncludeExpression);
    }
}