namespace Vali_Flow.InMemory.Interfaces.Evaluators.Write;

public interface IInMemoryEvaluatorWrite<T>
{
    public bool Add(T entity,IEnumerable<T>? entities = null);
    public T? Update(T entity,IEnumerable<T>? entities = null);
    public bool Delete(T entity,IEnumerable<T>? entities = null);
    public void AddRange(IEnumerable<T> entitiesToAdd,IEnumerable<T>? entities = null);
    public IEnumerable<T> UpdateRange(IEnumerable<T> entitiesToUpdate,IEnumerable<T>? entities = null);
    public int DeleteRange(IEnumerable<T> entitiesToDelete,IEnumerable<T>? entities = null);
    public void SaveChanges(IEnumerable<T>? entities = null);
}