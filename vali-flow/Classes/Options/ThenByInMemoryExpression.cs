namespace vali_flow.Classes.Options;

public class ThenByInMemoryExpression<T,TKey>
{
    public Func<T, TKey> ThenBy { get; set; }
    public bool Ascending { get; set; }
}