namespace vali_flow.Classes.Evaluators;

public interface IInMemoryEvaluator<T>
{
    bool Evaluate(T entity);
    bool EvaluateAny(IEnumerable<T> entities);
    int EvaluateCount(IEnumerable<T> entities);
    T? GetFirstFailed(IEnumerable<T> entities);
    T? GetFirst(IEnumerable<T> entities);

    IEnumerable<T> EvaluateAllFailed<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        Func<T, TKey>? thenBy = null,
        bool thenAscending = true);

    IEnumerable<T> EvaluateAll<TKey>(
        IEnumerable<T> entities,
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        Func<T, TKey>? thenBy = null,
        bool thenAscending = true);

    IEnumerable<T> EvaluatePaged<TKey>(
        IEnumerable<T> entities, 
        int page, 
        int pageSize, 
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        Func<T, TKey>? thenBy = null,
        bool thenAscending = true);

    IEnumerable<T> EvaluateTop<TKey>(
        IEnumerable<T> entities, 
        int count, 
        Func<T, TKey>? orderBy = null,
        bool ascending = true,
        Func<T, TKey>? thenBy = null,
        bool thenAscending = true);

    IEnumerable<T> EvaluateDistinct<TKey>(IEnumerable<T> entities, Func<T, TKey> keySelector);

    IEnumerable<T> EvaluateDuplicates<TKey>(IEnumerable<T> entities, Func<T, TKey> keySelector);
    
    int GetFirstMatchIndex(IEnumerable<T> entities);
    
    int GetLastMatchIndex(IEnumerable<T> entities);
    
    T? GetLastFailed(IEnumerable<T> entities);
    
    T? GetLast(IEnumerable<T> entities);
    
    TResult EvaluateMin<TResult>(IEnumerable<T> entities, Func<T, TResult> selector);
    
    TResult EvaluateMax<TResult>(IEnumerable<T> entities, Func<T, TResult> selector);
    
    TResult EvaluateAverage<TResult>(IEnumerable<T> entities, Func<T, TResult> selector);
    
    TResult EvaluateSum<TResult>(IEnumerable<T> entities, Func<T, TResult> selector);

    TResult EvaluateAggregate<TResult>(IEnumerable<T> entities, Func<T, TResult> selector,
        Func<TResult, TResult, TResult> aggregator);
}