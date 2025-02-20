using System.Linq.Expressions;

namespace vali_flow.Classes.Options;

public class ThenByDataBaseExpression<T,TKey>
{
    public Expression<Func<T, TKey>> ThenBy { get; set; }
    public bool Ascending { get; set; }
}