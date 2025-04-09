using System.Linq.Expressions;
using Vali_Flow.Core.Interfaces.General;

namespace Vali_Flow.Core.Classes.Base;

public class BaseExpression<TBuilder, T> : IExpression<TBuilder, T>
    where TBuilder : BaseExpression<TBuilder, T>, new()
{
    private readonly List<(Expression<Func<T, bool>> Condition, bool IsAnd)> _conditions = new();
    private bool _nextIsAnd = true;
    private List<Expression<Func<T, bool>>> _groupConditions = new();

    public Expression<Func<T, bool>> Build()
    {
        if (!_conditions.Any() && (!_groupConditions.Any()))
        {
            return _ => true;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression body = null;

        // Combinar las condiciones principales
        foreach (var (condition, isAnd) in _conditions)
        {
            var conditionBody = new ParameterReplacer(condition.Parameters[0], parameter).Visit(condition.Body);
            body = body == null
                ? conditionBody
                : isAnd
                    ? Expression.AndAlso(body, conditionBody)
                    : Expression.OrElse(body, conditionBody);
        }

        // Si hay un grupo de condiciones (por ejemplo, combinadas con OR), combinarlas
        if (_groupConditions.Any())
        {
            Expression groupBody = null;
            foreach (var condition in _groupConditions)
            {
                var conditionBody = new ParameterReplacer(condition.Parameters[0], parameter).Visit(condition.Body);
                groupBody = groupBody == null
                    ? conditionBody
                    : Expression.OrElse(groupBody, conditionBody);
            }

            body = body == null
                ? groupBody
                : _nextIsAnd
                    ? Expression.AndAlso(body, groupBody)
                    : Expression.OrElse(body, groupBody);
        }

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    public Expression<Func<T, bool>> BuildNegated()
    {
        Expression<Func<T, bool>> condition = Build();
        var parameter = condition.Parameters[0];
        var negatedBody = Expression.Not(condition.Body);
        return Expression.Lambda<Func<T, bool>>(negatedBody, parameter);
    }

    public TBuilder Add(Expression<Func<T, bool>> expression)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));
        EnsureValidCondition(expression);

        if (_groupConditions != null && _groupConditions.Any())
        {
            _groupConditions.Add(expression);
        }
        else
        {
            _conditions.Add((expression, _nextIsAnd));
        }

        _nextIsAnd = true; // Restablecer a AND por defecto
        return (TBuilder)this;
    }

    public TBuilder Add<TValue>(Expression<Func<T, TValue>> selector, Expression<Func<TValue, bool>> predicate)
    {
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        EnsureValidCondition(predicate);

        var parameter = selector.Parameters[0];
        var selectorBody = selector.Body;
        var predicateBody = new ParameterReplacer(predicate.Parameters[0], selectorBody).Visit(predicate.Body);

        Expression<Func<T, bool>> combinedCondition = Expression.Lambda<Func<T, bool>>(predicateBody, parameter);

        return Add(combinedCondition);
    }

    public TBuilder AddSubGroup(Action<IExpression<TBuilder, T>> group)
    {
        TBuilder groupBuilderInstance = new TBuilder();
        group(groupBuilderInstance);

        Expression<Func<T, bool>> groupCondition = groupBuilderInstance.Build();
        EnsureValidCondition(groupCondition);

        return Add(groupCondition);
    }

    public TBuilder And()
    {
        _nextIsAnd = true;
        _groupConditions = null; // Finalizar cualquier grupo de OR
        return (TBuilder)this;
    }

    public TBuilder Or()
    {
        _nextIsAnd = false;
        if (_conditions.Any() && _groupConditions == null)
        {
            // Mover la última condición de _conditions a _groupConditions para iniciar el grupo
            var lastCondition = _conditions.Last().Condition;
            _conditions.RemoveAt(_conditions.Count - 1);
            _groupConditions = new List<Expression<Func<T, bool>>> { lastCondition };
        }
        else if (_groupConditions == null)
        {
            _groupConditions = new List<Expression<Func<T, bool>>>();
        }

        return (TBuilder)this;
    }

    private Expression<Func<T, bool>> And(Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
    {
        var parameter = first.Parameters[0];
        var secondBody = new ParameterReplacer(second.Parameters[0], parameter).Visit(second.Body);
        var body = Expression.AndAlso(first.Body, secondBody);
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private Expression<Func<T, bool>> Or(Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
    {
        var parameter = first.Parameters[0];
        var secondBody = new ParameterReplacer(second.Parameters[0], parameter).Visit(second.Body);
        var body = Expression.OrElse(first.Body, secondBody);
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private void EnsureValidCondition(Expression<Func<T, bool>> condition)
    {
        switch (condition.Body)
        {
            case ConstantExpression constant when constant.Value is bool value && value:
                throw new ArgumentException("The condition provided has no effect because it is always 'true'.");
            case ConstantExpression constantFalse when constantFalse.Value is bool valueFalse && !valueFalse:
                throw new ArgumentException("The condition provided has no effect because it is always 'false'.");
            case ConstantExpression constantNull when constantNull.Value == null:
                throw new ArgumentException("The condition provided has no effect because it is always 'null'.");
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
            case ConstantExpression constant when constant.Value is bool value && value:
                throw new ArgumentException("The condition provided has no effect because it is always 'true'.");
            case ConstantExpression constantFalse when constantFalse.Value is bool valueFalse && !valueFalse:
                throw new ArgumentException("The condition provided has no effect because it is always 'false'.");
            case ConstantExpression constantNull when constantNull.Value == null:
                throw new ArgumentException("The condition provided has no effect because it is always 'null'.");
            case BinaryExpression binaryExpression when
                binaryExpression.Left is ConstantExpression leftConstant &&
                leftConstant.Value is int leftValue && leftValue == 0:
                throw new ArgumentException("The condition provided has no effect because it is always '0'.");
        }
    }

    private class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly Expression _newExpression;

        public ParameterReplacer(ParameterExpression oldParameter, Expression newExpression)
        {
            _oldParameter = oldParameter;
            _newExpression = newExpression;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newExpression : base.VisitParameter(node);
        }
    }
}