using vali_flow.Classes.Base;
using vali_flow.Interfaces.Types;

namespace vali_flow.Classes.Types;

public class BooleanExpression<TBuilder, T> : IBooleanExpression<TBuilder, T>
    where TBuilder : BaseExpression<TBuilder, T>, IBooleanExpression<TBuilder, T>, new()
{
    private readonly BaseExpression<TBuilder, T> _builder;

    public BooleanExpression(BaseExpression<TBuilder, T> builder)
    {
        _builder = builder;
    }

    public TBuilder IsTrue(Func<T, bool> selector)
    {
        return _builder.Add(x => selector(x) == true);
    }

    public TBuilder IsFalse(Func<T, bool> selector)
    {
        return _builder.Add(x => selector(x) == false);
    }
}