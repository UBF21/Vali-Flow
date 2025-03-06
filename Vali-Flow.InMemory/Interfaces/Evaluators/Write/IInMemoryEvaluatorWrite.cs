namespace Vali_Flow.InMemory.Interfaces.Evaluators.Write;

public interface IInMemoryEvaluatorWrite<T>
{
    public void Add(T entity);
    public T? Update(T entity);
    public bool Delete(T entity);
    public void AddRange(IEnumerable<T> entities);
    public IEnumerable<T> UpdateRange(IEnumerable<T> entities);
    public int DeleteRange(IEnumerable<T> entities);
    public void SaveChanges();
}