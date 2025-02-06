namespace vali_flow.Interfaces.Types;

public interface IBooleanExpression<out TBuilder,T>
{
    TBuilder IsTrue(Func<T, bool> selector);
    TBuilder IsFalse(Func<T, bool> selector);
}