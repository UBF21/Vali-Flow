using System.Linq.Expressions;

namespace Vali_Flow.Classes.Options;

public class EfOrderThenBy<T,TKey>
{
    public EfOrderThenBy(Expression<Func<T, TKey>> thenBy)
    {
        ThenBy = thenBy;
    }  
    public EfOrderThenBy(Expression<Func<T, TKey>> thenBy, bool ascending)
    {
        ThenBy = thenBy;
        Ascending = ascending;
    }

    public Expression<Func<T, TKey>> ThenBy { get; set; }
    public bool Ascending { get; set; } = true;
}