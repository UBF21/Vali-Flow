namespace Vali_Flow.Sql.Models;

/// <summary>Represents a SQL JOIN clause (INNER, LEFT, RIGHT, FULL OUTER, CROSS).</summary>
internal readonly record struct JoinClause(string JoinType, string TableSql, string OnSql);
