using System.Collections;
using System.Linq.Expressions;
using System.Text;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;

namespace Vali_Flow.Sql.Translators;

/// <summary>
/// Translates a <see cref="Expression{TDelegate}"/> produced by Vali-Flow.Core
/// into a parameterized SQL WHERE clause string.
/// </summary>
internal sealed class ExpressionToSqlVisitor : ExpressionVisitor
{
    private readonly ISqlDialect _dialect;
    private readonly StringBuilder _sql = new();
    private readonly Dictionary<string, object> _parameters = new();
    private int _paramCounter;

    private ExpressionToSqlVisitor(ISqlDialect dialect)
    {
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
    }

    /// <summary>Translates the body of a boolean lambda expression to SQL.</summary>
    public static SqlResult Translate<T>(Expression<Func<T, bool>> expression, ISqlDialect dialect)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        if (dialect == null)
        {
            throw new ArgumentNullException(nameof(dialect));
        }

        var visitor = new ExpressionToSqlVisitor(dialect);
        visitor.Visit(expression.Body);
        return new SqlResult(visitor._sql.ToString(), visitor._parameters);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        // Handle null comparisons
        if (node.NodeType == ExpressionType.Equal && IsNullConstant(node.Right))
        {
            var col = GetColumnSql(node.Left);
            _sql.Append(_dialect.NullCheck(col));
            return node;
        }

        if (node.NodeType == ExpressionType.NotEqual && IsNullConstant(node.Right))
        {
            var col = GetColumnSql(node.Left);
            _sql.Append(_dialect.NotNullCheck(col));
            return node;
        }

        if (node.NodeType == ExpressionType.Equal && IsNullConstant(node.Left))
        {
            var col = GetColumnSql(node.Right);
            _sql.Append(_dialect.NullCheck(col));
            return node;
        }

        if (node.NodeType == ExpressionType.NotEqual && IsNullConstant(node.Left))
        {
            var col = GetColumnSql(node.Right);
            _sql.Append(_dialect.NotNullCheck(col));
            return node;
        }

        // AND / OR grouping
        if (node.NodeType == ExpressionType.AndAlso || node.NodeType == ExpressionType.OrElse)
        {
            _sql.Append('(');
            Visit(node.Left);
            _sql.Append(node.NodeType == ExpressionType.AndAlso ? " AND " : " OR ");
            Visit(node.Right);
            _sql.Append(')');
            return node;
        }

        // Standard binary: col OP @pN
        // BUG-07 fix: handle constant-on-left (e.g. 18 < x.Age) by detecting which side is the column
        bool leftIsColumn = IsColumnExpression(node.Left);
        bool rightIsColumn = IsColumnExpression(node.Right);

        Expression columnExpr;
        Expression valueExpr;
        ExpressionType effectiveNodeType = node.NodeType;

        if (leftIsColumn)
        {
            columnExpr = node.Left;
            valueExpr = node.Right;
        }
        else if (rightIsColumn)
        {
            // Flip: e.g. 18 < x.Age → x.Age > 18
            columnExpr = node.Right;
            valueExpr = node.Left;
            effectiveNodeType = FlipComparisonOperator(node.NodeType);
        }
        else
        {
            columnExpr = node.Left;
            valueExpr = node.Right;
        }

        // Arithmetic operators: produce inline expressions (col + @p0, col * @p0, etc.)
        string? arithmeticOp = effectiveNodeType switch
        {
            ExpressionType.Add => "+",
            ExpressionType.Subtract => "-",
            ExpressionType.Multiply => "*",
            ExpressionType.Divide => "/",
            ExpressionType.Modulo => "%",
            _ => null
        };

        if (arithmeticOp != null)
        {
            var leftSql = GetColumnSql(columnExpr);
            var rightParamName = AddParameter(EvaluateExpression(valueExpr));
            _sql.Append($"{leftSql} {arithmeticOp} {_dialect.ParameterPrefix}{rightParamName}");
            return node;
        }

        var columnSql = GetColumnSql(columnExpr);
        var op = effectiveNodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "!=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            _ => throw new NotSupportedException($"Binary operator {node.NodeType} is not supported.")
        };

        var paramName = AddParameter(EvaluateExpression(valueExpr));
        _sql.Append($"{columnSql} {op} {_dialect.ParameterPrefix}{paramName}");
        return node;
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Not)
        {
            _sql.Append("NOT (");
            Visit(node.Operand);
            _sql.Append(')');
            return node;
        }

        if (node.NodeType == ExpressionType.Convert || node.NodeType == ExpressionType.ConvertChecked)
        {
            return Visit(node.Operand);
        }

        return base.VisitUnary(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // Math.Abs(x.Value) → ABS([Value])
        if (node.Method.Name == nameof(Math.Abs) && node.Method.DeclaringType == typeof(Math) &&
            node.Arguments.Count == 1)
        {
            var inner = GetColumnSql(node.Arguments[0]);
            _sql.Append($"ABS({inner})");
            return node;
        }

        // string.ToLower() / string.ToUpper() on a column — x.Name.ToLower() → LOWER([Name])
        if (node.Object != null && node.Object.Type == typeof(string) &&
            node.Arguments.Count == 0)
        {
            if (node.Method.Name == nameof(string.ToLower))
            {
                var col = GetColumnSql(node.Object);
                _sql.Append($"LOWER({col})");
                return node;
            }

            if (node.Method.Name == nameof(string.ToUpper))
            {
                var col = GetColumnSql(node.Object);
                _sql.Append($"UPPER({col})");
                return node;
            }
        }

        // string.Contains / StartsWith / EndsWith
        if (node.Object != null && node.Object.Type == typeof(string))
        {
            var col = GetColumnSql(node.Object);
            var value = EvaluateExpression(node.Arguments[0])?.ToString() ?? string.Empty;

            if (node.Method.Name == nameof(string.Contains))
            {
                var paramName = AddParameter($"%{value}%");
                _sql.Append($"{col} {_dialect.LikeOperator} {_dialect.ParameterPrefix}{paramName}");
                return node;
            }

            if (node.Method.Name == nameof(string.StartsWith))
            {
                var paramName = AddParameter($"{value}%");
                _sql.Append($"{col} {_dialect.LikeOperator} {_dialect.ParameterPrefix}{paramName}");
                return node;
            }

            if (node.Method.Name == nameof(string.EndsWith))
            {
                var paramName = AddParameter($"%{value}");
                _sql.Append($"{col} {_dialect.LikeOperator} {_dialect.ParameterPrefix}{paramName}");
                return node;
            }
        }

        // Enumerable.Contains(collection, item) — translates to IN (...)
        if (node.Method.Name == "Contains" && node.Method.DeclaringType == typeof(Enumerable))
        {
            // Enumerable.Contains(values, selector) — args[0]=collection, args[1]=member
            var collection = EvaluateExpression(node.Arguments[0]) as IEnumerable;
            var col = GetColumnSql(node.Arguments[1]);

            if (collection != null)
            {
                var paramNames = new List<string>();
                foreach (var item in collection)
                {
                    paramNames.Add($"{_dialect.ParameterPrefix}{AddParameter(item)}");
                }

                _sql.Append($"{col} IN ({string.Join(", ", paramNames)})");
                return node;
            }
        }

        // HashSet.Contains or ICollection.Contains — col IN (@p0) or col = @p0
        if (node.Method.Name == "Contains" && node.Object != null)
        {
            var collection = EvaluateExpression(node.Object) as IEnumerable;
            var col = GetColumnSql(node.Arguments[0]);

            if (collection != null)
            {
                var paramNames = new List<string>();
                foreach (var item in collection)
                {
                    paramNames.Add($"{_dialect.ParameterPrefix}{AddParameter(item)}");
                }

                if (paramNames.Count == 0)
                {
                    _sql.Append("1=0"); // IN () is invalid SQL; empty set always false
                }
                else
                {
                    _sql.Append($"{col} IN ({string.Join(", ", paramNames)})");
                }

                return node;
            }
        }

        throw new NotSupportedException($"Method call '{node.Method.DeclaringType?.Name}.{node.Method.Name}' is not supported by the SQL translator.");
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is ParameterExpression)
        {
            // BUG-08 fix: bool member used as condition body (x => x.IsActive) → [IsActive] = 1
            if (node.Type == typeof(bool))
            {
                _sql.Append($"{_dialect.QuoteIdentifier(node.Member.Name)} = {_dialect.TrueValue}");
            }
            else
            {
                // Direct property access on the root parameter → column name
                _sql.Append(_dialect.QuoteIdentifier(node.Member.Name));
            }
            return node;
        }

        // DateTime property access: x.CreatedAt.Year / .Month / .Day / .Hour / .Minute / .Second
        if (node.Expression is MemberExpression dateTimeMember &&
            dateTimeMember.Expression is ParameterExpression &&
            IsDateTimeType(dateTimeMember.Type))
        {
            string? datePart = node.Member.Name switch
            {
                "Year" => "YEAR",
                "Month" => "MONTH",
                "Day" => "DAY",
                "Hour" => "HOUR",
                "Minute" => "MINUTE",
                "Second" => "SECOND",
                _ => null
            };

            if (datePart != null)
            {
                var colSql = _dialect.QuoteIdentifier(dateTimeMember.Member.Name);
                _sql.Append(_dialect.DatePartExpression(colSql, datePart));
                return node;
            }
        }

        // Nested property: x.Address.City → [Address_City] or evaluate as constant
        if (node.Expression is MemberExpression)
        {
            // Try to evaluate as a closed-over value (constant)
            try
            {
                var value = EvaluateExpression(node);
                var paramName = AddParameter(value!);
                _sql.Append($"{_dialect.ParameterPrefix}{paramName}");
                return node;
            }
            catch
            {
                // Fall through to column name
                _sql.Append(_dialect.QuoteIdentifier(node.Member.Name));
                return node;
            }
        }

        // Closed-over variable (captured in lambda) — treat as constant value
        var constValue = EvaluateExpression(node);
        var pName = AddParameter(constValue!);
        _sql.Append($"{_dialect.ParameterPrefix}{pName}");
        return node;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        // Root parameter — should not appear directly in WHERE
        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value == null)
        {
            _sql.Append("NULL");
            return node;
        }

        var paramName = AddParameter(node.Value);
        _sql.Append($"{_dialect.ParameterPrefix}{paramName}");
        return node;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string GetColumnSql(Expression expr)
    {
        if (expr is MemberExpression member && member.Expression is ParameterExpression)
        {
            return _dialect.QuoteIdentifier(member.Member.Name);
        }

        if (expr is UnaryExpression unary &&
            (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked) &&
            unary.Operand is MemberExpression innerMember &&
            innerMember.Expression is ParameterExpression)
        {
            return _dialect.QuoteIdentifier(innerMember.Member.Name);
        }

        // Fallback: visit into a sub-builder
        var sub = new ExpressionToSqlVisitor(_dialect);
        sub.Visit(expr);
        return sub._sql.ToString();
    }

    private string AddParameter(object? value)
    {
        var name = $"p{_paramCounter++}";
        _parameters[name] = value ?? DBNull.Value;
        return name;
    }

    private static bool IsNullConstant(Expression expr)
        => expr is ConstantExpression c && c.Value == null;

    /// <summary>Returns true if expr is a direct property access on the root lambda parameter.</summary>
    private static bool IsColumnExpression(Expression expr)
    {
        if (expr is MemberExpression m && m.Expression is ParameterExpression)
            return true;

        if (expr is UnaryExpression u &&
            (u.NodeType == ExpressionType.Convert || u.NodeType == ExpressionType.ConvertChecked) &&
            u.Operand is MemberExpression im && im.Expression is ParameterExpression)
            return true;

        return false;
    }

    /// <summary>Flips a comparison operator so the column can always be on the left side.</summary>
    private static ExpressionType FlipComparisonOperator(ExpressionType op) => op switch
    {
        ExpressionType.GreaterThan => ExpressionType.LessThan,
        ExpressionType.GreaterThanOrEqual => ExpressionType.LessThanOrEqual,
        ExpressionType.LessThan => ExpressionType.GreaterThan,
        ExpressionType.LessThanOrEqual => ExpressionType.GreaterThanOrEqual,
        _ => op // Equal and NotEqual are symmetric
    };

    private static bool IsDateTimeType(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(DateOnly);
    }

    private static object? EvaluateExpression(Expression expr)
    {
        if (expr is ConstantExpression constant)
        {
            return constant.Value;
        }

        // Compile and invoke to get the value (for captured variables, etc.)
        try
        {
            var lambda = Expression.Lambda(expr);
            return lambda.Compile().DynamicInvoke();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to evaluate expression '{expr}'. Ensure the expression only captures constant or closed-over values.", ex);
        }
    }
}
