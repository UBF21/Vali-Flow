namespace Vali_Flow.Sql.Models;

/// <summary>Represents a single ORDER BY column with its sort direction.</summary>
internal readonly record struct OrderByClause(string ColumnSql, bool Ascending);
