using System.Linq.Expressions;

namespace vali_flow.Classes.Options;

public class ThenByDataBaseExpression<T,TKey>
{
    public ThenByDataBaseExpression(Expression<Func<T, TKey>> thenBy)
    {
        ThenBy = thenBy;
    }  
    public ThenByDataBaseExpression(Expression<Func<T, TKey>> thenBy, bool ascending)
    {
        ThenBy = thenBy;
        Ascending = ascending;
    }

    public Expression<Func<T, TKey>> ThenBy { get; set; }
    public bool Ascending { get; set; } = true;
}