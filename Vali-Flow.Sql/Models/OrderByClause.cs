using Vali_Flow.Sql.Builder;

namespace Vali_Flow.Sql.Models;

/// <summary>Represents a single ORDER BY column with its sort direction and optional NULLS ordering.</summary>
internal readonly record struct OrderByClause(string ColumnSql, bool Ascending, NullsOrder Nulls = NullsOrder.Default);
