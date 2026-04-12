using System.Collections;
using System.Linq.Expressions;
using Vali_Flow.Abstractions.Helpers;
using Vali_Flow.NoSql.IR;
using static Vali_Flow.Abstractions.Helpers.ExpressionInspector;

namespace Vali_Flow.NoSql.Translators;

/// <summary>
/// Walks an <see cref="Expression{TDelegate}"/> produced by Vali-Flow.Core
/// and builds a provider-agnostic <see cref="IConditionNode"/> tree (the NoSql IR).
/// </summary>
internal sealed class ExpressionToIRVisitor : ExpressionVisitor
{
    private IConditionNode? _result;

    private ExpressionToIRVisitor() { }

    /// <summary>
    /// Translates the body of a boolean lambda expression into an <see cref="IConditionNode"/> tree.
    /// </summary>
    /// <typeparam name="T">The entity type the expression operates on.</typeparam>
    /// <param name="expression">A boolean lambda expression.</param>
    /// <returns>The root <see cref="IConditionNode"/> of the translated tree.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the expression contains a pattern that has no NoSql equivalent
    /// (arithmetic, bitwise, CASE WHEN, SQL-specific functions, etc.).
    /// </exception>
    public static IConditionNode Translate<T>(Expression<Func<T, bool>> expression)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));

        var visitor = new ExpressionToIRVisitor();
        visitor.Visit(expression.Body);
        return visitor._result
               ?? throw new InvalidOperationException($"Translation produced no result for: {expression.Body}");
    }

    // ── Dispatch to a fresh visitor and return the result ─────────────────────
    private static IConditionNode TranslateSubExpr(Expression expr)
    {
        var sub = new ExpressionToIRVisitor();
        sub.Visit(expr);
        return sub._result
               ?? throw new InvalidOperationException($"Cannot translate sub-expression: {expr}");
    }

    // ── Binary — dispatcher (SRP: delegates each case to a focused method) ────
    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType is ExpressionType.Equal or ExpressionType.NotEqual)
        {
            var nullCheck = TryTranslateNullCheck(node);
            if (nullCheck != null) { _result = nullCheck; return node; }
        }

        if (node.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
        {
            _result = TranslateLogical(node);
            return node;
        }

        _result = TranslateComparison(node);
        return node;
    }

    /// <summary>Handles <c>field == null</c> / <c>field != null</c>. Returns <c>null</c> if not a null check.</summary>
    private static IConditionNode? TryTranslateNullCheck(BinaryExpression node)
    {
        bool rightIsNull = IsNullConstant(node.Right);
        bool leftIsNull  = IsNullConstant(node.Left);

        if (!rightIsNull && !leftIsNull) return null;

        Expression fieldExpr = rightIsNull ? node.Left : node.Right;
        string field = GetMemberName(fieldExpr);
        bool isEqual = node.NodeType == ExpressionType.Equal;
        return new NullNode(field, isEqual ? NullCheckOp.IsNull : NullCheckOp.IsNotNull);
    }

    /// <summary>Handles <c>AndAlso</c> / <c>OrElse</c> into <see cref="AndNode"/> / <see cref="OrNode"/>.</summary>
    private static IConditionNode TranslateLogical(BinaryExpression node)
        => node.NodeType == ExpressionType.AndAlso
            ? new AndNode(TranslateSubExpr(node.Left), TranslateSubExpr(node.Right))
            : new OrNode(TranslateSubExpr(node.Left), TranslateSubExpr(node.Right));

    /// <summary>
    /// Handles comparison operators, including the constant-on-left flip
    /// (e.g. <c>18 &lt; x.Age</c> → <c>x.Age &gt; 18</c>).
    /// </summary>
    private static IConditionNode TranslateComparison(BinaryExpression node)
    {
        bool leftIsColumn  = IsColumnExpression(node.Left);
        bool rightIsColumn = IsColumnExpression(node.Right);

        Expression columnExpr;
        Expression valueExpr;
        ExpressionType effectiveType;

        if (leftIsColumn)
        {
            columnExpr    = node.Left;
            valueExpr     = node.Right;
            effectiveType = node.NodeType;
        }
        else if (rightIsColumn)
        {
            columnExpr    = node.Right;
            valueExpr     = node.Left;
            effectiveType = FlipComparisonOperator(node.NodeType);
        }
        else
        {
            columnExpr    = node.Left;
            valueExpr     = node.Right;
            effectiveType = node.NodeType;
        }

        string fieldName = GetMemberName(columnExpr);
        object? value    = EvaluateExpression(valueExpr);

        return effectiveType switch
        {
            ExpressionType.Equal              => new EqualNode(fieldName, value!, false),
            ExpressionType.NotEqual           => new EqualNode(fieldName, value!, true),
            ExpressionType.GreaterThan        => new ComparisonNode(fieldName, value!, ComparisonOp.GreaterThan),
            ExpressionType.GreaterThanOrEqual => new ComparisonNode(fieldName, value!, ComparisonOp.GreaterThanOrEqual),
            ExpressionType.LessThan           => new ComparisonNode(fieldName, value!, ComparisonOp.LessThan),
            ExpressionType.LessThanOrEqual    => new ComparisonNode(fieldName, value!, ComparisonOp.LessThanOrEqual),
            _ => throw new NotSupportedException(
                $"Binary operator '{node.NodeType}' is not supported in NoSql IR translation. " +
                $"Arithmetic, bitwise, and CASE WHEN patterns have no document-store equivalent.")
        };
    }

    // ── Unary ─────────────────────────────────────────────────────────────────
    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Not)
        {
            _result = new NotNode(TranslateSubExpr(node.Operand));
            return node;
        }

        // Convert/ConvertChecked: transparent wrapper — pass through
        if (node.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
        {
            return Visit(node.Operand);
        }

        return base.VisitUnary(node);
    }

    // ── Member (bool property used directly: x.IsActive) ─────────────────────
    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Type == typeof(bool) && IsColumnExpression(node))
        {
            _result = new EqualNode(GetMemberName(node), true, false);
        }
        else if (IsColumnExpression(node))
        {
            throw new NotSupportedException(
                $"Member '{node.Member.Name}' of type '{node.Type.Name}' cannot be used as a standalone filter condition. " +
                "Use a comparison expression (e.g., x.Status == MyEnum.Active).");
        }

        return node;
    }

    // ── Method calls ──────────────────────────────────────────────────────────
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // string.Contains / StartsWith / EndsWith on a member
        if (node.Object != null && node.Object.Type == typeof(string) &&
            node.Arguments.Count >= 1 && IsColumnExpression(node.Object))
        {
            string field   = GetMemberName(node.Object);
            string pattern = EvaluateExpression(node.Arguments[0])?.ToString() ?? string.Empty;

            _result = node.Method.Name switch
            {
                nameof(string.Contains)   => new LikeNode(field, pattern, LikeOp.Contains),
                nameof(string.StartsWith) => new LikeNode(field, pattern, LikeOp.StartsWith),
                nameof(string.EndsWith)   => new LikeNode(field, pattern, LikeOp.EndsWith),
                _ => throw new NotSupportedException(
                    $"String method '{node.Method.Name}' is not supported in NoSql IR translation.")
            };
            return node;
        }

        // Enumerable.Contains(collection, x.Field)
        if (node.Method.Name == "Contains" &&
            node.Method.DeclaringType == typeof(Enumerable) &&
            node.Arguments.Count == 2)
        {
            string field  = GetMemberName(node.Arguments[1]);
            var    values = ExtractCollection(node.Arguments[0]);
            _result = new InNode(field, values);
            return node;
        }

        // ICollection<T>.Contains(x.Field)
        if (node.Method.Name == "Contains" && node.Object != null && node.Arguments.Count == 1)
        {
            string field  = GetMemberName(node.Arguments[0]);
            var    values = ExtractCollection(node.Object);
            _result = new InNode(field, values);
            return node;
        }

        throw new NotSupportedException(
            $"Method '{node.Method.DeclaringType?.Name}.{node.Method.Name}' is not supported in NoSql IR translation. " +
            $"Only Contains/StartsWith/EndsWith on strings and collection.Contains are supported.");
    }

    // ── NoSql-specific helpers (not shared — distinct from SQL's GetColumnSql) ─

    /// <summary>Extracts the member name from a column expression, stripping Convert wrappers.</summary>
    private static string GetMemberName(Expression expr)
    {
        while (expr is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
            expr = u.Operand;

        if (expr is MemberExpression member)
            return member.Member.Name;

        throw new NotSupportedException(
            $"Cannot extract a field name from expression '{expr}'. " +
            $"Only direct property access on the lambda parameter is supported.");
    }

    /// <summary>Evaluates and enumerates a collection expression into a list.</summary>
    private static IReadOnlyList<object?> ExtractCollection(Expression collectionExpr)
    {
        var collection = EvaluateExpression(collectionExpr) as IEnumerable;
        if (collection == null) return Array.Empty<object?>();

        var list = new List<object?>();
        foreach (var item in collection) list.Add(item);
        return list;
    }
}
