namespace Vali_Flow.Interfaces.Options;

public interface IEfThenInclude<TParent, TPreviousProperty, TProperty>
    where TParent : class
    where TPreviousProperty : class
{
    IQueryable<TParent> ApplyThenInclude(IQueryable<TParent> query);
}