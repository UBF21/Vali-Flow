using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Vali_Flow.Interfaces.Options;

namespace Vali_Flow.Classes.Options;

/// <summary>
/// Applies an Include followed by a ThenInclude for a collection navigation property.
/// </summary>
/// <typeparam name="T">The root entity type.</typeparam>
/// <typeparam name="TProperty">The type of the first-level collection element.</typeparam>
/// <typeparam name="TNav">The type of the nested navigation property.</typeparam>
public sealed class EfThenInclude<T, TProperty, TNav> : IEfInclude<T> where T : class
{
    private readonly Expression<Func<T, IEnumerable<TProperty>>> _include;
    private readonly Expression<Func<TProperty, TNav>> _thenInclude;

    /// <summary>Initializes a new ThenInclude for a collection navigation.</summary>
    public EfThenInclude(
        Expression<Func<T, IEnumerable<TProperty>>> include,
        Expression<Func<TProperty, TNav>> thenInclude)
    {
        _include = include;
        _thenInclude = thenInclude;
    }

    /// <inheritdoc/>
    public IQueryable<T> ApplyInclude(IQueryable<T> query)
        => query.Include(_include).ThenInclude(_thenInclude);
}

/// <summary>
/// Applies an Include followed by a ThenInclude for a reference navigation property.
/// </summary>
/// <typeparam name="T">The root entity type.</typeparam>
/// <typeparam name="TProperty">The type of the first-level reference navigation.</typeparam>
/// <typeparam name="TNav">The type of the nested navigation property.</typeparam>
public sealed class EfThenIncludeReference<T, TProperty, TNav> : IEfInclude<T>
    where T : class
    where TProperty : class
{
    private readonly Expression<Func<T, TProperty?>> _include;
    private readonly Expression<Func<TProperty?, TNav>> _thenInclude;

    /// <summary>Initializes a new ThenInclude for a reference navigation.</summary>
    public EfThenIncludeReference(
        Expression<Func<T, TProperty?>> include,
        Expression<Func<TProperty?, TNav>> thenInclude)
    {
        _include = include;
        _thenInclude = thenInclude;
    }

    /// <inheritdoc/>
    public IQueryable<T> ApplyInclude(IQueryable<T> query)
        => query.Include(_include).ThenInclude(_thenInclude);
}
