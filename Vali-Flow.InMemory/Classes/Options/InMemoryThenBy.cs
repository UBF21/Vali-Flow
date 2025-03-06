namespace Vali_Flow.InMemory.Classes.Options;

public class InMemoryThenBy<T,TKey>
{
    public InMemoryThenBy(Func<T, TKey> thenBy)
    {
        ThenBy = thenBy;
    }

    public InMemoryThenBy(Func<T, TKey> thenBy,bool ascending)
    {
        ThenBy = thenBy;
        Ascending = ascending;
    }

    public Func<T, TKey> ThenBy { get; set; }
    public bool Ascending { get; set; } = true;
}