using System.Collections;

namespace vali_flow.Classes.Base;

public class Grouping<TKey,TResult> : IGrouping<TKey,TResult>
{
    public TKey Key { get; }
    private readonly IEnumerable<TResult> _values;

    public Grouping(TKey key, IEnumerable<TResult> values)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        _values = values ?? throw new ArgumentNullException(nameof(values));
    }
    
    public IEnumerator<TResult> GetEnumerator()
    {
        return _values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}