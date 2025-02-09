using System.Linq.Expressions;
using vali_flow.Interfaces.Types;

namespace vali_flow.Classes.Base;

public  class BaseExpression<TBuilder, T> : IExpression<TBuilder, T>
    where TBuilder : BaseExpression<TBuilder, T>, new()
{
    private readonly HashSet<Expression<Func<T, bool>>> _conditions = new();
    private Expression<Func<T, bool>>? _orCondition;
    private bool _isAnd = true;

    public Expression<Func<T, bool>> Build()
    {
        if (!_conditions.Any() && _orCondition is null)
        {
            return _ => true; // Devuelve una expresión que siempre es true si no hay condiciones.
        }

        var andCondition = _conditions
            .DefaultIfEmpty(_ => true) // Si no hay condiciones, devuelve una que siempre es true.
            .Aggregate(And);

        return _orCondition is null ? andCondition : Or(andCondition, _orCondition);
    }

    public TBuilder Add(Expression<Func<T, bool>> expression)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));
        EnsureValidCondition(expression);

        if (_isAnd)
        {
            _conditions.Add(expression);
        }
        else
        {
            _orCondition = _orCondition is null
                ? expression
                : Or(_orCondition, expression);
        }

        return (TBuilder)this;
    }

    public TBuilder Add<TValue>(Expression<Func<T, TValue>> selector, Expression<Func<TValue, bool>> predicate)
    {
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        EnsureValidCondition(predicate);

        ParameterExpression parameter = Expression.Parameter(typeof(T), "Type");
        InvocationExpression selectorBody = Expression.Invoke(selector, parameter);
        InvocationExpression predicateBody = Expression.Invoke(predicate, selectorBody);

        Expression<Func<T, bool>> combinedCondition = Expression.Lambda<Func<T, bool>>(predicateBody, parameter);

        return Add(combinedCondition);
    }

    public TBuilder AddSubGroup(Action<TBuilder> group)
    {
        TBuilder groupBuilderInstance = new TBuilder();
        group(groupBuilderInstance);

        // Construir la condición del grupo
        Expression<Func<T, bool>> groupCondition = groupBuilderInstance.Build();

        // Verificar si la condición es trivial (si es null o una constante true)
        if (groupCondition.Body is ConstantExpression constant && constant.Value is bool value && value)
        {
            return (TBuilder)this; // No agrega condiciones triviales (si la condición es true o null).
        }

        // Si la condición no es trivial, agregarla
        return Add(groupCondition);
    }

    public bool Evaluate(T entity)
    {
        try
        {
            Func<T, bool> compiledCondition = Build().Compile(); // Compila la expresión internamente.
            return compiledCondition(entity);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error in evaluating the conditions.", ex);
        }
    }

    public IEnumerable<T> EvaluateAll(IEnumerable<T> entities)
    {
        if (entities == null || !entities.Any())
        {
            throw new ArgumentException("La colección no puede ser nula o vacía.", nameof(entities));
        }

        Func<T, bool> compiledCondition = Build().Compile();
        return entities.Where(compiledCondition);
    }

    public TBuilder And()
    {
        _isAnd = true;
        return (TBuilder)this;
    }

    public TBuilder Or()
    {
        _isAnd = false;
        return (TBuilder)this;
    }

    /// <summary>
    /// Combina dos expresiones de tipo <see>
    ///     <cref>Expression{Func{T, bool}}</cref>
    /// </see>
    /// utilizando una operación lógica "AND".
    /// </summary>
    /// <param name="first">La primera expresión a combinar.</param>
    /// <param name="second">La segunda expresión a combinar.</param>
    /// <returns>Una nueva expresión que representa la combinación de ambas expresiones con un "AND" lógico.</returns>
    /// <exception cref="ArgumentNullException">Lanza si alguna de las expresiones proporcionadas es nula.</exception>
    private Expression<Func<T, bool>> And(Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
    {
        ParameterExpression parameter = Expression.Parameter(typeof(T));
        BinaryExpression body =
            Expression.AndAlso(Expression.Invoke(first, parameter), Expression.Invoke(second, parameter));
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    /// <summary>
    /// Combina dos expresiones de tipo <see>
    ///     <cref>Expression{Func{T, bool}}</cref>
    /// </see>
    /// utilizando una operación lógica "OR".
    /// </summary>
    /// <param name="first">La primera expresión a combinar.</param>
    /// <param name="second">La segunda expresión a combinar.</param>
    /// <returns>Una nueva expresión que representa la combinación de ambas expresiones con un "OR" lógico.</returns>
    /// <exception cref="ArgumentNullException">Lanza si alguna de las expresiones proporcionadas es nula.</exception>
    private Expression<Func<T, bool>> Or(Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
    {
        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.OrElse(
            Expression.Invoke(first, parameter),
            Expression.Invoke(second, parameter)
        );

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private void EnsureValidCondition(Expression<Func<T, bool>> condition)
    {
        switch (condition.Body)
        {
            // Verificar si la condición es una constante que siempre es 'true'
            case ConstantExpression constant when constant.Value is bool value && value:
                throw new ArgumentException("The condition provided has no effect because it is always 'true'.");
            // Verificar si la condición es una constante que siempre es 'false'
            case ConstantExpression constantFalse when constantFalse.Value is bool valueFalse && !valueFalse:
                throw new ArgumentException("The condition provided has no effect because it is always 'false'.");
            // Verificar si la condición es una constante de tipo nulo (como x => null)
            case ConstantExpression constantNull when constantNull.Value == null:
                throw new ArgumentException("The condition provided has no effect because it is always 'null'.");
            // Verificar si la condición está comparando a '0' (ejemplo: x => 0)
            case BinaryExpression binaryExpression when
                binaryExpression.Left is ConstantExpression leftConstant &&
                leftConstant.Value is int leftValue && leftValue == 0:
                throw new ArgumentException("The condition provided has no effect because it is always '0'.");
        }
    }

    private void EnsureValidCondition<TValue>(Expression<Func<TValue, bool>> condition)
    {
        switch (condition.Body)
        {
            // Verificar si la condición es una constante que siempre es 'true'
            case ConstantExpression constant when constant.Value is bool value && value:
                throw new ArgumentException("The condition provided has no effect because it is always 'true'.");
            // Verificar si la condición es una constante que siempre es 'false'
            case ConstantExpression constantFalse when constantFalse.Value is bool valueFalse && !valueFalse:
                throw new ArgumentException("The condition provided has no effect because it is always 'false'.");
            // Verificar si la condición es una constante de tipo nulo (como x => null)
            case ConstantExpression constantNull when constantNull.Value == null:
                throw new ArgumentException("The condition provided has no effect because it is always 'null'.");
            // Verificar si la condición está comparando a '0' (ejemplo: x => 0)
            case BinaryExpression binaryExpression when
                binaryExpression.Left is ConstantExpression leftConstant &&
                leftConstant.Value is int leftValue && leftValue == 0:
                throw new ArgumentException("The condition provided has no effect because it is always '0'.");
        }
    }
}