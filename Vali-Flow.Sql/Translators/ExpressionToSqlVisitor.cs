using System.Collections;
using System.Linq.Expressions;
using System.Text;
using Vali_Flow.Abstractions.Helpers;
using Vali_Flow.Sql.Dialects;
using Vali_Flow.Sql.Models;
using static Vali_Flow.Abstractions.Helpers.ExpressionInspector;

namespace Vali_Flow.Sql.Translators;

/// <summary>
/// Translates a <see cref="Expression{TDelegate}"/> produced by Vali-Flow.Core
/// into a parameterized SQL WHERE clause string.
/// </summary>
internal sealed class ExpressionToSqlVisitor : ExpressionVisitor
{
    private readonly ISqlDialect _dialect;
    private readonly StringBuilder _sql = new();
    private readonly Dictionary<string, object> _parameters;
    private int _paramCounter;

    private ExpressionToSqlVisitor(ISqlDialect dialect)
    {
        _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        _parameters = new Dictionary<string, object>();
    }

    /// <summary>Creates a sub-visitor that shares the parent's parameter dictionary and counter state.</summary>
    private ExpressionToSqlVisitor(ISqlDialect dialect, Dictionary<string, object> sharedParams, int startCounter)
    {
        _dialect = dialect;
        _parameters = sharedParams;
        _paramCounter = startCounter;
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

        // Arithmetic and bitwise operators: produce inline expressions (col + @p0, col & @p0, etc.)
        string? arithmeticOp = effectiveNodeType switch
        {
            ExpressionType.Add         => "+",
            ExpressionType.Subtract    => "-",
            ExpressionType.Multiply    => "*",
            ExpressionType.Divide      => "/",
            ExpressionType.Modulo      => "%",
            ExpressionType.And         => "&",
            ExpressionType.Or          => "|",
            ExpressionType.ExclusiveOr => "^",
            _ => null
        };

        if (arithmeticOp != null)
        {
            var leftSql = GetColumnSql(columnExpr);
            if (IsColumnExpression(valueExpr))
            {
                // Both sides are columns: col & col, col + col, etc.
                _sql.Append($"{leftSql} {arithmeticOp} {GetColumnSql(valueExpr)}");
            }
            else
            {
                var rightParamName = AddParameter(EvaluateExpression(valueExpr));
                _sql.Append($"{leftSql} {arithmeticOp} {_dialect.ParameterPrefix}{rightParamName}");
            }
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

        if (node.NodeType == ExpressionType.Negate || node.NodeType == ExpressionType.NegateChecked)
        {
            _sql.Append("-(");
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

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        // Translates (test ? ifTrue : ifFalse) → CASE WHEN test THEN ifTrue ELSE ifFalse END
        _sql.Append("CASE WHEN ");
        Visit(node.Test);
        _sql.Append(" THEN ");
        Visit(node.IfTrue);
        _sql.Append(" ELSE ");
        Visit(node.IfFalse);
        _sql.Append(" END");
        return node;
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

        // string.Contains / StartsWith / EndsWith / Substring / Trim / Replace / IndexOf
        if (node.Object != null && node.Object.Type == typeof(string))
        {
            var col = GetColumnSql(node.Object);

            if (node.Method.Name == nameof(string.Contains) && node.Arguments.Count >= 1)
            {
                var value = EvaluateExpression(node.Arguments[0])?.ToString() ?? string.Empty;
                var paramName = AddParameter($"%{value}%");
                _sql.Append($"{col} {_dialect.LikeOperator} {_dialect.ParameterPrefix}{paramName}");
                return node;
            }

            if (node.Method.Name == nameof(string.StartsWith) && node.Arguments.Count >= 1)
            {
                var value = EvaluateExpression(node.Arguments[0])?.ToString() ?? string.Empty;
                var paramName = AddParameter($"{value}%");
                _sql.Append($"{col} {_dialect.LikeOperator} {_dialect.ParameterPrefix}{paramName}");
                return node;
            }

            if (node.Method.Name == nameof(string.EndsWith) && node.Arguments.Count >= 1)
            {
                var value = EvaluateExpression(node.Arguments[0])?.ToString() ?? string.Empty;
                var paramName = AddParameter($"%{value}");
                _sql.Append($"{col} {_dialect.LikeOperator} {_dialect.ParameterPrefix}{paramName}");
                return node;
            }

            // x.Name.Substring(start) or x.Name.Substring(start, length) — 0-indexed → 1-indexed
            if (node.Method.Name == nameof(string.Substring) && node.Arguments.Count >= 1)
            {
                int start = Convert.ToInt32(EvaluateExpression(node.Arguments[0])) + 1;
                if (node.Arguments.Count == 2)
                {
                    int length = Convert.ToInt32(EvaluateExpression(node.Arguments[1]));
                    _sql.Append(_dialect.SubstringExpression(col, start.ToString(), length.ToString()));
                }
                else
                {
                    _sql.Append(_dialect.SubstringExpression(col, start.ToString()));
                }
                return node;
            }

            // x.Name.Replace(old, new)
            if (node.Method.Name == nameof(string.Replace) && node.Arguments.Count == 2)
            {
                var oldVal = EvaluateExpression(node.Arguments[0])?.ToString() ?? string.Empty;
                var newVal = EvaluateExpression(node.Arguments[1])?.ToString() ?? string.Empty;
                var oldParam = AddParameter(oldVal);
                var newParam = AddParameter(newVal);
                _sql.Append($"REPLACE({col}, {_dialect.ParameterPrefix}{oldParam}, {_dialect.ParameterPrefix}{newParam})");
                return node;
            }

            // x.Name.IndexOf(value) → 0-based index
            if (node.Method.Name == nameof(string.IndexOf) && node.Arguments.Count >= 1)
            {
                var searchVal = EvaluateExpression(node.Arguments[0])?.ToString() ?? string.Empty;
                var searchParam = AddParameter(searchVal);
                _sql.Append(_dialect.IndexOfExpression($"{_dialect.ParameterPrefix}{searchParam}", col));
                return node;
            }
        }

        // string zero-arg methods: Trim / TrimStart / TrimEnd (already handled ToLower/ToUpper above)
        if (node.Object != null && node.Object.Type == typeof(string) && node.Arguments.Count == 0)
        {
            if (node.Method.Name == nameof(string.Trim))
            {
                var col = GetColumnSql(node.Object);
                _sql.Append(_dialect.TrimExpression(col));
                return node;
            }

            if (node.Method.Name == nameof(string.TrimStart))
            {
                var col = GetColumnSql(node.Object);
                _sql.Append($"LTRIM({col})");
                return node;
            }

            if (node.Method.Name == nameof(string.TrimEnd))
            {
                var col = GetColumnSql(node.Object);
                _sql.Append($"RTRIM({col})");
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

        // DateTime.AddDays / AddMonths / AddYears — x.CreatedAt.AddDays(5)
        if (node.Object != null && IsDateTimeType(node.Object.Type) && node.Arguments.Count == 1 &&
            (node.Method.Name == "AddDays" || node.Method.Name == "AddMonths" || node.Method.Name == "AddYears"))
        {
            var col = GetColumnSql(node.Object);
            var amount = Convert.ToInt32(EvaluateExpression(node.Arguments[0]));
            string datePart = node.Method.Name switch
            {
                "AddDays"   => "DAY",
                "AddMonths" => "MONTH",
                "AddYears"  => "YEAR",
                _           => "DAY"
            };
            _sql.Append(_dialect.DateAddExpression(datePart, amount.ToString(), col));
            return node;
        }

        // Math functions
        if (node.Method.DeclaringType == typeof(Math))
        {
            // Math.Round(x.Salary, 2) or Math.Round(x.Salary)
            if (node.Method.Name == nameof(Math.Round) && node.Arguments.Count >= 1)
            {
                var col = GetColumnSql(node.Arguments[0]);
                if (node.Arguments.Count >= 2)
                {
                    int decimals = Convert.ToInt32(EvaluateExpression(node.Arguments[1]));
                    _sql.Append($"ROUND({col}, {decimals})");
                }
                else
                {
                    _sql.Append($"ROUND({col}, 0)");
                }
                return node;
            }

            // Math.Ceiling(x.Salary) → CEILING or CEIL per dialect
            if (node.Method.Name == nameof(Math.Ceiling) && node.Arguments.Count == 1)
            {
                var col = GetColumnSql(node.Arguments[0]);
                _sql.Append(_dialect.CeilingExpression(col));
                return node;
            }

            // Math.Floor(x.Salary) → FLOOR(col)
            if (node.Method.Name == nameof(Math.Floor) && node.Arguments.Count == 1)
            {
                var col = GetColumnSql(node.Arguments[0]);
                _sql.Append($"FLOOR({col})");
                return node;
            }

            // Math.Sqrt(x.Salary) → SQRT(col)
            if (node.Method.Name == nameof(Math.Sqrt) && node.Arguments.Count == 1)
            {
                var col = GetColumnSql(node.Arguments[0]);
                _sql.Append($"SQRT({col})");
                return node;
            }

            // Math.Pow(x.Salary, 2) → POWER(col, exp)
            if (node.Method.Name == nameof(Math.Pow) && node.Arguments.Count == 2)
            {
                var col = GetColumnSql(node.Arguments[0]);
                int exp = Convert.ToInt32(EvaluateExpression(node.Arguments[1]));
                _sql.Append($"POWER({col}, {exp})");
                return node;
            }
        }

        // ── Enum.HasFlag ──────────────────────────────────────────────────────────
        if (node.Method.Name == nameof(Enum.HasFlag) &&
            node.Object is MemberExpression flagMember &&
            flagMember.Expression is ParameterExpression)
        {
            var col = _dialect.QuoteIdentifier(flagMember.Member.Name);
            var flagValue = Convert.ToInt64(EvaluateExpression(node.Arguments[0]));
            _sql.Append($"({col} & {flagValue}) = {flagValue}");
            return node;
        }

        // ── string.IsNullOrEmpty / IsNullOrWhiteSpace (static methods) ───────────
        if (node.Method.DeclaringType == typeof(string))
        {
            if (node.Method.Name == nameof(string.IsNullOrEmpty))
            {
                var col = GetColumnSql(node.Arguments[0]);
                _sql.Append($"({col} IS NULL OR {col} = '')");
                return node;
            }

            if (node.Method.Name == nameof(string.IsNullOrWhiteSpace))
            {
                var col = GetColumnSql(node.Arguments[0]);
                _sql.Append(_dialect.IsNullOrWhitespaceExpression(col));
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

        // string.Length property: x.Name.Length → LEN([Name]) or LENGTH([Name])
        if (node.Member.Name == "Length" &&
            node.Expression is MemberExpression strLenMember &&
            strLenMember.Expression is ParameterExpression &&
            strLenMember.Type == typeof(string))
        {
            var col = _dialect.QuoteIdentifier(strLenMember.Member.Name);
            _sql.Append(_dialect.StringLengthExpression(col));
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

        // Fallback: visit into a sub-builder that shares our parameter dictionary and counter
        var sub = new ExpressionToSqlVisitor(_dialect, _parameters, _paramCounter);
        sub.Visit(expr);
        _paramCounter = sub._paramCounter; // sync counter back so subsequent params don't collide
        return sub._sql.ToString();
    }

    private string AddParameter(object? value)
    {
        var name = $"p{_paramCounter++}";
        _parameters[name] = value ?? DBNull.Value;
        return name;
    }

    // IsNullConstant, IsColumnExpression, FlipComparisonOperator, EvaluateExpression
    // are shared via ExpressionInspector (using static above).

    private static bool IsDateTimeType(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(DateOnly);
    }

}
