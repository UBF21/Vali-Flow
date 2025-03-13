namespace Vali_Flow.Interfaces.Specification;

public interface IIncludeExpression<T>
{
    IQueryable<T> ApplyInclude(IQueryable<T> query);
}