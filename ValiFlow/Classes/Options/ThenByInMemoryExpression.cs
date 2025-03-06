namespace vali_flow.Classes.Options;

public class ThenByInMemoryExpression<T,TKey>
{
    public ThenByInMemoryExpression(Func<T, TKey> thenBy)
    {
        ThenBy = thenBy;
    }

    public ThenByInMemoryExpression(Func<T, TKey> thenBy,bool ascending)
    {
        ThenBy = thenBy;
        Ascending = ascending;
    }

    public Func<T, TKey> ThenBy { get; set; }
    public bool Ascending { get; set; } = true;
}