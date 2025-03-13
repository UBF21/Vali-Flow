using System.Linq.Expressions;
using Vali_Flow.Core.Builder;
using Vali_Flow.Interfaces.Specification;

namespace Vali_Flow.Classes.Specification;

public class Specification<T> : ISpecification<T> where T : class
{
    private ValiFlow<T> _filter;
    private readonly List<IIncludeExpression<T>> _includes = new();
    private bool _asNoTracking = true;
    private bool _asSplitQuery;

    public ValiFlow<T> Filter => _filter;
    public IEnumerable<IIncludeExpression<T>> Includes => _includes;
    public bool AsNoTracking => _asNoTracking;
    public bool AsSplitQuery => _asSplitQuery;


    public Specification(ValiFlow<T> filter)
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter), "The filter cannot be null.");
    }

    public Specification(
        ValiFlow<T> filter,
        bool asNoTracking = true,
        bool asSplitQuery = false
    )
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter), "The filter cannot be null.");
        _asNoTracking = asNoTracking;
        _asSplitQuery = asSplitQuery;
    }

    public Specification<T> WithFilter(ValiFlow<T> filter)
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter), "The filter cannot be null.");
        return this;
    }

    public Specification<T> AddInclude<TProperty>(Expression<Func<T, TProperty>> include)
    {
        _includes.Add(new IncludeExpression<T, TProperty>(include));
        return this;
    }

    public Specification<T> AddIncludes<TProperty>(IEnumerable<Expression<Func<T, TProperty>>> includes)
    {
        foreach (var include in includes)
        {
            _includes.Add(new IncludeExpression<T, TProperty>(include));
        }

        return this;
    }

    public Specification<T> WithAsNoTracking(bool asNoTracking)
    {
        _asNoTracking = asNoTracking;
        return this;
    }

    public Specification<T> WithAsSplitQuery(bool asSplitQuery)
    {
        _asSplitQuery = asSplitQuery;
        return this;
    }
}