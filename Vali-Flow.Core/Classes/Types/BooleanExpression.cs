using Vali_Flow.Core.Classes.Base;
using Vali_Flow.Core.Interfaces.Types;

namespace Vali_Flow.Core.Classes.Types;

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