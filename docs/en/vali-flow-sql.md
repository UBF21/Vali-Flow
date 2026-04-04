# Vali-Flow.Sql — Complete Reference

## Table of Contents

1. [Overview](#overview)
2. [Dialects](#dialects)
3. [Extension Methods](#extension-methods-valiflowsqlextensions)
4. [SqlResult / SqlQueryResult](#sqlresult--sqlqueryresult)
5. [SqlQueryBuilder](#sqlquerybuilder)
   - [SELECT](#select)
   - [FROM](#from)
   - [JOINs](#joins)
   - [WHERE](#where)
   - [GROUP BY / HAVING](#group-by--having)
   - [ORDER BY](#order-by)
   - [Pagination](#pagination)
   - [DISTINCT / WithHint](#distinct--withhint)
   - [Set Operations](#set-operations)
   - [CTEs](#ctes)
   - [Row Locking](#row-locking)
   - [Tag / Build / ToPreviewSql](#tag--build--topreviewsql)
6. [SqlInsertBuilder](#sqlinsertbuilder)
7. [SqlUpdateBuilder](#sqlupdatebuilder)
8. [SqlDeleteBuilder](#sqldeletebuilder)
9. [SqlTruncateBuilder](#sqltruncatebuilder)
10. [SqlMergeBuilder](#sqlmergebuilder)

---

## Overview

**Vali-Flow.Sql** translates strongly-typed C# expressions and fluent builders into parameterized SQL statements. It targets Dapper, ADO.NET, or any framework that accepts a SQL string plus a parameter dictionary.

Key features:

- Expression tree → SQL WHERE clause translation via `ValiFlow<T>.ToSql(dialect)`
- Full SELECT query construction via `SqlQueryBuilder<T>`
- INSERT, UPDATE, DELETE, TRUNCATE, and MERGE builders
- Five built-in SQL dialects (SQL Server, PostgreSQL, MySQL, SQLite, Oracle)
- All queries are parameterized — no string interpolation of user values
- Window functions: ROW_NUMBER, RANK, DENSE_RANK, LAG, LEAD, FIRST_VALUE, LAST_VALUE, and aggregate OVER functions
- CTEs (regular and recursive), subquery JOINs, UNION / EXCEPT / INTERSECT, row locking

**Install**

```bash
dotnet add package Vali-Flow.Sql
```

**Quick example**

```csharp
var filter = new ValiFlow<User>()
    .NotNull(x => x.Name)
    .GreaterThan(x => x.Age, 18);

var sql = filter.ToSql(new SqlServerDialect());
// sql.Sql        → "[Name] IS NOT NULL AND [Age] > @p0"
// sql.Parameters → { "p0": 18 }

// Dapper
var users = connection.Query<User>($"SELECT * FROM Users WHERE {sql.Sql}", sql.Parameters);

// ADO.NET
cmd.CommandText = $"SELECT * FROM Users WHERE {sql.Sql}";
sql.ApplyTo(cmd);
```

---

## Dialects

Every builder and extension method requires an `ISqlDialect` instance. Choose the one matching your database engine.

| Feature | `SqlServerDialect` | `PostgreSqlDialect` | `MySqlDialect` | `SqliteDialect` | `OracleDialect` |
|---|---|---|---|---|---|
| **Parameter prefix** | `@` | `@` | `@` | `@` | `:` |
| **Column quoting** | `[col]` | `"col"` | `` `col` `` | `"col"` | `"col"` |
| **Table quoting** | `[tbl]` | `"tbl"` | `` `tbl` `` | `"tbl"` | `"tbl"` |
| **Case-insensitive LIKE** | `LOWER([col]) LIKE LOWER(@p)` | `"col" ILIKE @p` | `` LOWER(`col`) LIKE LOWER(@p) `` | `LOWER("col") LIKE LOWER(@p)` | `LOWER("col") LIKE LOWER(:p)` |
| **TOP / LIMIT** | `TOP N` (no OFFSET) / `OFFSET … ROWS FETCH NEXT N ROWS ONLY` | `LIMIT N OFFSET M` | `LIMIT N OFFSET M` | `LIMIT N OFFSET M` | `FETCH FIRST N ROWS ONLY` |
| **TRUNCATE support** | Yes | Yes | Yes | No (throws) | Yes |
| **MERGE support** | Yes | No | No | No | No |
| **OUTPUT INSERTED/DELETED** | Yes | No | No | No | No |
| **RETURNING clause** | No | Yes | Yes (INSERT only) | Yes | No |
| **ON CONFLICT** | No | Yes | No | Yes | No |
| **ON DUPLICATE KEY** | No | No | Yes | No | No |
| **INSERT OR IGNORE/REPLACE** | No | No | No | Yes | No |
| **FOR UPDATE / FOR SHARE** | No (use `WithHint`) | Yes | Yes | No (silently ignored) | Yes |
| **NULLS FIRST/LAST** | No (silently ignored) | Yes | No | Yes | Yes |
| **Recursive CTE keyword** | *(omitted)* | `RECURSIVE` | `RECURSIVE` | `RECURSIVE` | *(omitted)* |
| **UPDATE … FROM/JOIN** | Yes (FROM after SET) | Yes (FROM after SET) | Yes (JOIN before SET) | No | No |
| **IS DISTINCT FROM** | Emulated | Native | Emulated | Native | Emulated |
| **Date part extraction** | `DATEPART(part, col)` | `EXTRACT(part FROM col)` | `EXTRACT(part FROM col)` | `strftime('%Y', col)` | `EXTRACT(part FROM col)` |
| **Current timestamp** | `GETDATE()` | `NOW()` | `NOW()` | `CURRENT_TIMESTAMP` | `SYSDATE` |
| **String length** | `LEN(col)` | `LENGTH(col)` | `LENGTH(col)` | `LENGTH(col)` | `LENGTH(col)` |
| **String concatenation** | `a + b` | `a \|\| b` | `CONCAT(a, b)` | `a \|\| b` | `a \|\| b` |
| **Trim** | `LTRIM(RTRIM(col))` | `TRIM(col)` | `TRIM(col)` | `TRIM(col)` | `TRIM(col)` |

**Usage**

```csharp
ISqlDialect dialect = new SqlServerDialect();
ISqlDialect dialect = new PostgreSqlDialect();
ISqlDialect dialect = new MySqlDialect();
ISqlDialect dialect = new SqliteDialect();
ISqlDialect dialect = new OracleDialect();
```

---

## Extension Methods (`ValiFlowSqlExtensions`)

### `ToSql` — ValiFlow overload

**Signature**
```csharp
SqlResult ToSql<T>(this ValiFlow<T> flow, ISqlDialect dialect)
```

**Description**
Translates all conditions built in a `ValiFlow<T>` into a parameterized SQL WHERE fragment (without the `WHERE` keyword).

**Example**
```csharp
var filter = new ValiFlow<User>()
    .NotNull(x => x.Name)
    .GreaterThan(x => x.Age, 18);

var sql = filter.ToSql(new SqlServerDialect());
// sql.Sql        → "[Name] IS NOT NULL AND [Age] > @p0"
// sql.Parameters → { "p0": 18 }
```

---

### `ToSql` — ValiFlowQuery overload

**Signature**
```csharp
SqlResult ToSql<T>(this ValiFlowQuery<T> flow, ISqlDialect dialect)
```

**Description**
Same as above but for `ValiFlowQuery<T>` instances.

---

### `ToSql` — Expression overload

**Signature**
```csharp
SqlResult ToSql<T>(this Expression<Func<T, bool>> expression, ISqlDialect dialect)
```

**Description**
Translates a prebuilt `Expression<Func<T, bool>>` directly, without requiring a `ValiFlow<T>` wrapper.

**Example**
```csharp
Expression<Func<User, bool>> expr = u => u.IsActive && u.Age > 18;
var sql = expr.ToSql(new PostgreSqlDialect());
// sql.Sql → "\"IsActive\" = true AND \"Age\" > @p0"
```

---

### `ToSqlCount` — ValiFlow overload

**Signature**
```csharp
SqlQueryResult ToSqlCount<T>(
    this ValiFlow<T>? flow,
    ISqlDialect dialect,
    string? tableName = null) where T : class
```

**Description**
Generates a `SELECT COUNT(*) FROM [table] WHERE ...` query. Pass `null` as `flow` to count all rows.

**Example**
```csharp
var countResult = new ValiFlow<User>()
    .EqualTo(x => x.IsActive, true)
    .ToSqlCount(new SqlServerDialect(), "Users");

// countResult.Sql → "SELECT COUNT(*) FROM [Users] WHERE [IsActive] = @p0"
var count = connection.ExecuteScalar<int>(countResult.Sql, countResult.Parameters);
```

---

### `ToSqlCount` — ValiFlowQuery overload

**Signature**
```csharp
SqlQueryResult ToSqlCount<T>(
    this ValiFlowQuery<T>? flow,
    ISqlDialect dialect,
    string? tableName = null) where T : class
```

**Description**
Same as above for `ValiFlowQuery<T>`.

---

## SqlResult / SqlQueryResult

### `SqlResult`

Returned by `ToSql()` extension methods. Holds a WHERE fragment only (no SELECT / FROM).

| Property / Method | Type | Description |
|---|---|---|
| `Sql` | `string` | Parameterized SQL WHERE fragment (without `WHERE` keyword) |
| `Parameters` | `IReadOnlyDictionary<string, object>` | Named parameters ready for Dapper or ADO.NET |
| `ApplyTo(IDbCommand)` | `void` | Adds all parameters to an ADO.NET command |
| `ToString()` | `string` | Returns `Sql` |

**ApplyTo example**
```csharp
var sql = filter.ToSql(new SqliteDialect());
cmd.CommandText = $"SELECT * FROM Products WHERE {sql.Sql}";
sql.ApplyTo(cmd);
var reader = cmd.ExecuteReader();
```

---

### `SqlQueryResult`

Returned by `Build()` on all builders, and by `ToSqlCount()`. Holds a complete SQL statement.

| Property / Method | Type | Description |
|---|---|---|
| `Sql` | `string` | Full parameterized SQL query |
| `Parameters` | `IReadOnlyDictionary<string, object>` | Named parameters |
| `ApplyTo(IDbCommand)` | `void` | Adds all parameters to an ADO.NET command |
| `ToDebugSql()` | `string` | Returns SQL with parameter values inlined — **for logging only, never execute** |
| `ToString()` | `string` | Returns `Sql` |

**ToDebugSql example**
```csharp
var result = new SqlQueryBuilder<User>(new SqlServerDialect())
    .From("Users")
    .Where(x => x.Age > 18)
    .Build();

Console.WriteLine(result.ToDebugSql());
// SELECT * FROM [Users] WHERE [Age] > 18
```

> **Warning:** `ToDebugSql()` inlines raw values into the SQL string for readability. Never pass this string to a database — it is intended only for logging and debugging.

---

## SqlQueryBuilder

`SqlQueryBuilder<T>` is the main entry point for building complete SELECT queries. It is a sealed generic class — `T` is the entity type whose properties are available as typed columns.

```csharp
var result = new SqlQueryBuilder<Order>(new SqlServerDialect())
    .From("Orders", schema: "dbo")
    .Select(x => x.Id, x => x.Total)
    .Where(x => x.Total > 100)
    .OrderBy(x => x.CreatedAt, ascending: false)
    .Page(1, 20)
    .Build();
```

---

### SELECT

#### `Select` — multiple columns

**Signature**
```csharp
SqlQueryBuilder<T> Select(params Expression<Func<T, object>>[] columns)
```

**Description**
Adds one or more typed columns to the SELECT list. If never called, the query defaults to `SELECT *`.

**Example**
```csharp
builder.Select(x => x.Id, x => x.Name);
// → SELECT [Id], [Name] FROM ...
```

---

#### `Select` — single column with alias

**Signature**
```csharp
SqlQueryBuilder<T> Select(Expression<Func<T, object>> column, string? alias)
```

**Description**
Adds a single typed column, optionally aliased.

**Example**
```csharp
builder.Select(x => x.Name, "UserName");
// → SELECT [Name] AS [UserName] FROM ...
```

---

#### `SelectAll`

**Signature**
```csharp
SqlQueryBuilder<T> SelectAll()
```

**Description**
Resets the column list to `SELECT *`, clearing any previously added columns.

---

#### `SelectRaw`

**Signature**
```csharp
SqlQueryBuilder<T> SelectRaw(string rawSql)
```

**Description**
Appends a raw SQL fragment to the SELECT list. Use for expressions that cannot be expressed through typed columns.

**Example**
```csharp
builder.SelectRaw("GETDATE() AS [Now]");
// → SELECT GETDATE() AS [Now] FROM ...
```

---

#### `SelectRawIf`

**Signature**
```csharp
SqlQueryBuilder<T> SelectRawIf(bool condition, string rawSql)
```

**Description**
Adds a raw SELECT expression only when `condition` is `true`.

---

#### `SelectIf`

**Signature**
```csharp
SqlQueryBuilder<T> SelectIf(bool condition, Expression<Func<T, object>> column, string? alias = null)
```

**Description**
Adds a typed column only when `condition` is `true`.

---

#### `SelectCount` — star

**Signature**
```csharp
SqlQueryBuilder<T> SelectCount(string? alias = null)
```

**Description**
Adds `COUNT(*)` to the SELECT list.

**Example**
```csharp
builder.SelectCount("total");
// → SELECT COUNT(*) AS [total] FROM ...
```

---

#### `SelectCount` — column

**Signature**
```csharp
SqlQueryBuilder<T> SelectCount(Expression<Func<T, object>> column, string? alias = null)
```

**Description**
Adds `COUNT([col])` to the SELECT list.

---

#### `SelectCountDistinct`

**Signature**
```csharp
SqlQueryBuilder<T> SelectCountDistinct(Expression<Func<T, object>> column, string? alias = null)
```

**Description**
Adds `COUNT(DISTINCT [col])` to the SELECT list.

---

#### `SelectSum`

**Signature**
```csharp
SqlQueryBuilder<T> SelectSum(Expression<Func<T, object>> column, string? alias = null)
```

**Description**
Adds `SUM([col])` to the SELECT list.

---

#### `SelectAvg`

**Signature**
```csharp
SqlQueryBuilder<T> SelectAvg(Expression<Func<T, object>> column, string? alias = null)
```

**Description**
Adds `AVG([col])` to the SELECT list.

---

#### `SelectMin`

**Signature**
```csharp
SqlQueryBuilder<T> SelectMin(Expression<Func<T, object>> column, string? alias = null)
```

**Description**
Adds `MIN([col])` to the SELECT list.

---

#### `SelectMax`

**Signature**
```csharp
SqlQueryBuilder<T> SelectMax(Expression<Func<T, object>> column, string? alias = null)
```

**Description**
Adds `MAX([col])` to the SELECT list.

---

#### `SelectCase`

**Signature**
```csharp
SqlQueryBuilder<T> SelectCase(CaseWhenBuilder caseWhen)
```

**Description**
Adds a `CASE WHEN ... THEN ... ELSE ... END` expression to the SELECT list, built via `CaseWhenBuilder`.

**Example**
```csharp
var caseExpr = new CaseWhenBuilder()
    .When("[Status] = 1", "Active")
    .When("[Status] = 2", "Inactive")
    .Else("Unknown")
    .As("StatusLabel");

builder.SelectCase(caseExpr);
// → SELECT CASE WHEN [Status] = 1 THEN 'Active' WHEN [Status] = 2 THEN 'Inactive' ELSE 'Unknown' END AS [StatusLabel]
```

---

#### `SelectCoalesce`

**Signature**
```csharp
SqlQueryBuilder<T> SelectCoalesce(
    Expression<Func<T, object>> column, string fallbackSql, string alias)
```

**Description**
Adds `COALESCE([col], fallbackSql) AS [alias]` to the SELECT list.

**Example**
```csharp
builder.SelectCoalesce(x => x.Department, "'N/A'", "Dept");
// → SELECT COALESCE([Department], 'N/A') AS [Dept] FROM ...
```

---

#### `SelectCast`

**Signature**
```csharp
SqlQueryBuilder<T> SelectCast(
    Expression<Func<T, object>> column, string typeName, string alias)
```

**Description**
Adds `CAST([col] AS typeName) AS [alias]` to the SELECT list.

**Example**
```csharp
builder.SelectCast(x => x.Age, "FLOAT", "AgeFloat");
// → SELECT CAST([Age] AS FLOAT) AS [AgeFloat] FROM ...
```

---

#### `SelectConcat`

**Signature**
```csharp
SqlQueryBuilder<T> SelectConcat(string alias, params Expression<Func<T, object>>[] columns)
```

**Description**
Adds a dialect-aware string concatenation expression. The operator differs per dialect:
- SQL Server: `[col1] + [col2]`
- PostgreSQL / SQLite: `"col1" || "col2"`
- MySQL: `` CONCAT(`col1`, `col2`) ``

**Example**
```csharp
builder.SelectConcat("FullName", x => x.FirstName, x => x.LastName);
// SQL Server → SELECT [FirstName] + [LastName] AS [FullName]
// PostgreSQL → SELECT "FirstName" || "LastName" AS "FullName"
```

---

### Window Functions

#### `SelectRowNumber`

**Signature**
```csharp
SqlQueryBuilder<T> SelectRowNumber(
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>> orderBy,
    bool ascending = true,
    string? alias = "RowNum")

// Multi-partition overload
SqlQueryBuilder<T> SelectRowNumber(
    IReadOnlyList<Expression<Func<T, object>>> partitionBy,
    Expression<Func<T, object>> orderBy,
    bool ascending = true,
    string? alias = "RowNum")
```

**Description**
Adds `ROW_NUMBER() OVER (PARTITION BY [col] ORDER BY [col] ASC|DESC) AS [alias]`. Pass `null` as `partitionBy` to omit the PARTITION BY clause.

**Example**
```csharp
builder.SelectRowNumber(x => x.Department, x => x.Salary, ascending: false, alias: "SalaryRank");
// → SELECT ROW_NUMBER() OVER (PARTITION BY [Department] ORDER BY [Salary] DESC) AS [SalaryRank]
```

---

#### `SelectRank`

**Signature**
```csharp
SqlQueryBuilder<T> SelectRank(
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>> orderBy,
    bool ascending = true,
    string? alias = "Rank")
```

**Description**
Adds `RANK() OVER (...)`. Same gap behavior as standard SQL RANK.

---

#### `SelectDenseRank`

**Signature**
```csharp
SqlQueryBuilder<T> SelectDenseRank(
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>> orderBy,
    bool ascending = true,
    string? alias = "DenseRank")
```

**Description**
Adds `DENSE_RANK() OVER (...)`. No gaps in ranking values.

---

#### `SelectLag`

**Signature**
```csharp
SqlQueryBuilder<T> SelectLag(
    Expression<Func<T, object>> column,
    int offset,
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>> orderBy,
    bool ascending = true,
    string? alias = null)
```

**Description**
Adds `LAG([col], offset) OVER ([PARTITION BY ...] ORDER BY ...)`. Retrieves the value from a previous row within the partition.

**Example**
```csharp
builder.SelectLag(x => x.Price, 1, null, x => x.CreatedAt, alias: "PrevPrice");
// → SELECT LAG([Price], 1) OVER (ORDER BY [CreatedAt] ASC) AS [PrevPrice]
```

---

#### `SelectLead`

**Signature**
```csharp
SqlQueryBuilder<T> SelectLead(
    Expression<Func<T, object>> column,
    int offset,
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>> orderBy,
    bool ascending = true,
    string? alias = null)
```

**Description**
Adds `LEAD([col], offset) OVER (...)`. Retrieves the value from a subsequent row within the partition.

---

#### `SelectFirstValue`

**Signature**
```csharp
SqlQueryBuilder<T> SelectFirstValue(
    Expression<Func<T, object>> column,
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>> orderBy,
    bool ascending = true,
    string? alias = null)
```

**Description**
Adds `FIRST_VALUE([col]) OVER (...)`. Retrieves the first value in the ordered window frame.

---

#### `SelectLastValue`

**Signature**
```csharp
SqlQueryBuilder<T> SelectLastValue(
    Expression<Func<T, object>> column,
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>> orderBy,
    bool ascending = true,
    string? alias = null)
```

**Description**
Adds `LAST_VALUE([col]) OVER (...)`. Retrieves the last value in the ordered window frame.

---

#### `SelectSumOver`

**Signature**
```csharp
SqlQueryBuilder<T> SelectSumOver(
    Expression<Func<T, object>> column,
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>>? orderBy,
    bool ascending = true,
    string? alias = null)
```

**Description**
Adds `SUM([col]) OVER ([PARTITION BY ...] [ORDER BY ...])`. Useful for running totals. The alias defaults to `Running{ColumnName}`.

**Example**
```csharp
builder.SelectSumOver(x => x.Amount, x => x.CustomerId, x => x.CreatedAt, alias: "RunningTotal");
// → SELECT SUM([Amount]) OVER (PARTITION BY [CustomerId] ORDER BY [CreatedAt] ASC) AS [RunningTotal]
```

---

#### `SelectAvgOver`

**Signature**
```csharp
SqlQueryBuilder<T> SelectAvgOver(
    Expression<Func<T, object>> column,
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>>? orderBy,
    bool ascending = true,
    string? alias = null)
```

**Description**
Adds `AVG([col]) OVER (...)`.

---

#### `SelectCountOver`

**Signature**
```csharp
SqlQueryBuilder<T> SelectCountOver(
    Expression<Func<T, object>> column,
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>>? orderBy,
    bool ascending = true,
    string? alias = null)
```

**Description**
Adds `COUNT([col]) OVER (...)`.

---

#### `SelectMinOver`

**Signature**
```csharp
SqlQueryBuilder<T> SelectMinOver(
    Expression<Func<T, object>> column,
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>>? orderBy,
    bool ascending = true,
    string? alias = null)
```

**Description**
Adds `MIN([col]) OVER (...)`.

---

#### `SelectMaxOver`

**Signature**
```csharp
SqlQueryBuilder<T> SelectMaxOver(
    Expression<Func<T, object>> column,
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>>? orderBy,
    bool ascending = true,
    string? alias = null)
```

**Description**
Adds `MAX([col]) OVER (...)`.

---

#### `SelectWindowRaw`

**Signature**
```csharp
SqlQueryBuilder<T> SelectWindowRaw(string windowExpression, string alias)
```

**Description**
Adds a raw window function expression to SELECT. Use when the typed overloads don't cover your case.

**Example**
```csharp
builder.SelectWindowRaw("NTILE(4) OVER (ORDER BY [Score] DESC)", "Quartile");
// → SELECT NTILE(4) OVER (ORDER BY [Score] DESC) AS [Quartile]
```

---

### FROM

#### `From` — table name

**Signature**
```csharp
SqlQueryBuilder<T> From(string tableName, string? schema = null)
```

**Description**
Sets the FROM table. If not called, defaults to `typeof(T).Name`. Schema is optional.

**Example**
```csharp
builder.From("Users", schema: "dbo");
// SQL Server → FROM [dbo].[Users]
// PostgreSQL → FROM "dbo"."Users"
```

---

#### `From` — subquery

**Signature**
```csharp
SqlQueryBuilder<T> From(SqlQueryResult subquery, string alias)
```

**Description**
Uses a prebuilt `SqlQueryResult` as the FROM source: `FROM (subquery) alias`.

**Example**
```csharp
var inner = new SqlQueryBuilder<User>(dialect)
    .From("Users")
    .Where(x => x.IsActive)
    .Build();

new SqlQueryBuilder<User>(dialect)
    .From(inner, "active")
    .Select(x => x.Name)
    .Build();
// → SELECT "Name" FROM (SELECT * FROM "Users" WHERE "IsActive" = true) active
```

---

### JOINs

All join methods accept a table name (quoted by the dialect), an optional alias, and a raw ON condition string.

#### `InnerJoin`

**Signature**
```csharp
SqlQueryBuilder<T> InnerJoin(string table, string? alias, string on)
```

**Example**
```csharp
builder.From("Users").InnerJoin("Orders", "o", "[Users].[Id] = [o].[UserId]");
// → FROM [Users] INNER JOIN [Orders] o ON [Users].[Id] = [o].[UserId]
```

---

#### `LeftJoin`

**Signature**
```csharp
SqlQueryBuilder<T> LeftJoin(string table, string? alias, string on)
```

**Description**
Adds a `LEFT JOIN` clause.

---

#### `RightJoin`

**Signature**
```csharp
SqlQueryBuilder<T> RightJoin(string table, string? alias, string on)
```

**Description**
Adds a `RIGHT JOIN` clause.

---

#### `FullOuterJoin`

**Signature**
```csharp
SqlQueryBuilder<T> FullOuterJoin(string table, string? alias, string on)
```

**Description**
Adds a `FULL OUTER JOIN` clause.

---

#### `CrossJoin`

**Signature**
```csharp
SqlQueryBuilder<T> CrossJoin(string table, string? alias = null)
```

**Description**
Adds a `CROSS JOIN` clause. No ON condition is required.

---

#### `InnerJoinSubquery`

**Signature**
```csharp
SqlQueryBuilder<T> InnerJoinSubquery(SqlQueryResult subquery, string alias, string on)
```

**Description**
Adds an `INNER JOIN` against a derived table (subquery). Parameters from the subquery are automatically remapped to avoid collisions.

**Example**
```csharp
var sub = new SqlQueryBuilder<Order>(dialect)
    .From("Orders")
    .Select(x => x.UserId)
    .SelectSum(x => x.Total, "TotalSpent")
    .GroupBy(x => x.UserId)
    .Build();

builder.From("Users").InnerJoinSubquery(sub, "totals", "[Users].[Id] = [totals].[UserId]");
// → FROM [Users] INNER JOIN (SELECT [UserId], SUM([Total]) AS [TotalSpent] FROM [Orders] GROUP BY [UserId]) [totals] ON [Users].[Id] = [totals].[UserId]
```

---

#### `LeftJoinSubquery`

**Signature**
```csharp
SqlQueryBuilder<T> LeftJoinSubquery(SqlQueryResult subquery, string alias, string on)
```

**Description**
Adds a `LEFT JOIN` against a derived table (subquery).

---

#### `RightJoinSubquery`

**Signature**
```csharp
SqlQueryBuilder<T> RightJoinSubquery(SqlQueryResult subquery, string alias, string on)
```

**Description**
Adds a `RIGHT JOIN` against a derived table (subquery).

---

#### `FullOuterJoinSubquery`

**Signature**
```csharp
SqlQueryBuilder<T> FullOuterJoinSubquery(SqlQueryResult subquery, string alias, string on)
```

**Description**
Adds a `FULL OUTER JOIN` against a derived table (subquery).

---

### WHERE

Multiple WHERE calls are ANDed together. The `ValiFlow<T>` / `Expression` overload and the `SqlWhereBuilder` overload use different parameter name prefixes (`p` vs `pw`) and can coexist safely.

#### `Where` — ValiFlow

**Signature**
```csharp
SqlQueryBuilder<T> Where(ValiFlow<T> filter)
```

**Description**
Sets the WHERE clause from a `ValiFlow<T>` filter. Parameters use the `p` prefix.

**Example**
```csharp
var filter = new ValiFlow<User>().EqualTo(x => x.IsActive, true).GreaterThan(x => x.Age, 18);
builder.From("Users").Where(filter);
// → WHERE [IsActive] = @p0 AND [Age] > @p1
```

---

#### `Where` — Expression

**Signature**
```csharp
SqlQueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
```

**Description**
Sets the WHERE clause from a lambda expression. Parameters use the `p` prefix.

> **Note:** Calling both `Where(ValiFlow<T>)` and `Where(Expression<...>)` is not supported — the second call overwrites the first. Use only one typed predicate; combine with `Where(SqlWhereBuilder)` for composite conditions.

---

#### `Where` — SqlWhereBuilder (instance)

**Signature**
```csharp
SqlQueryBuilder<T> Where(SqlWhereBuilder<T> whereBuilder)
```

**Description**
Sets the WHERE clause from a pre-configured `SqlWhereBuilder<T>`. Parameters use the `pw` prefix. Can be combined with the typed predicate overload.

---

#### `Where` — SqlWhereBuilder (inline)

**Signature**
```csharp
SqlQueryBuilder<T> Where(Action<SqlWhereBuilder<T>> configure)
```

**Description**
Configures the WHERE clause inline using a `SqlWhereBuilder<T>` action.

**Example**
```csharp
builder.Where(w => w.EqualTo(x => x.IsActive, true).GreaterThan(x => x.Age, 18));
// → WHERE [IsActive] = @pw0 AND [Age] > @pw1
```

---

#### `WhereRaw`

**Signature**
```csharp
SqlQueryBuilder<T> WhereRaw(string rawSql)
```

**Description**
Appends a raw SQL fragment to the WHERE clause (ANDed with other conditions). Parameters in the raw fragment are the caller's responsibility.

**Example**
```csharp
builder.WhereRaw("[CreatedAt] > DATEADD(day, -30, GETDATE())");
// → WHERE [CreatedAt] > DATEADD(day, -30, GETDATE())
```

---

#### `WhereExists`

**Signature**
```csharp
SqlQueryBuilder<T> WhereExists(SqlQueryResult subquery)
```

**Description**
Appends `EXISTS (subquery)` to the WHERE clause. Subquery parameters are automatically merged and renamed.

**Example**
```csharp
var sub = new SqlQueryBuilder<Order>(dialect)
    .From("Orders")
    .WhereRaw("[Orders].[UserId] = [Users].[Id]")
    .Build();

builder.From("Users").WhereExists(sub);
// → WHERE EXISTS (SELECT * FROM [Orders] WHERE [Orders].[UserId] = [Users].[Id])
```

---

#### `WhereNotExists`

**Signature**
```csharp
SqlQueryBuilder<T> WhereNotExists(SqlQueryResult subquery)
```

**Description**
Appends `NOT EXISTS (subquery)` to the WHERE clause.

---

#### `WhereInSubquery`

**Signature**
```csharp
SqlQueryBuilder<T> WhereInSubquery<TValue>(Expression<Func<T, TValue>> column, SqlQueryResult subquery)
```

**Description**
Appends `[col] IN (subquery)` to the WHERE clause. Subquery parameters are automatically remapped.

**Example**
```csharp
var sub = new SqlQueryBuilder<Order>(dialect)
    .From("Orders")
    .Select(x => x.UserId)
    .Build();

builder.From("Users").WhereInSubquery(x => x.Id, sub);
// → WHERE [Id] IN (SELECT [UserId] FROM [Orders])
```

---

#### `WhereNotInSubquery`

**Signature**
```csharp
SqlQueryBuilder<T> WhereNotInSubquery<TValue>(Expression<Func<T, TValue>> column, SqlQueryResult subquery)
```

**Description**
Appends `[col] NOT IN (subquery)` to the WHERE clause.

---

#### `WhereIf` — builder overload

**Signature**
```csharp
SqlQueryBuilder<T> WhereIf(bool condition, Action<SqlWhereBuilder<T>> configure)
```

**Description**
Applies a `SqlWhereBuilder` WHERE clause only when `condition` is `true`.

---

#### `WhereIf` — expression overload

**Signature**
```csharp
SqlQueryBuilder<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate)
```

**Description**
Applies a lambda WHERE predicate only when `condition` is `true`.

---

### GROUP BY / HAVING

#### `GroupBy`

**Signature**
```csharp
SqlQueryBuilder<T> GroupBy(params Expression<Func<T, object>>[] columns)
```

**Description**
Adds GROUP BY columns.

**Example**
```csharp
builder.GroupBy(x => x.Department, x => x.Status);
// → GROUP BY [Department], [Status]
```

---

#### `GroupByRaw`

**Signature**
```csharp
SqlQueryBuilder<T> GroupByRaw(string rawSql)
```

**Description**
Adds a raw SQL fragment to the GROUP BY clause. Useful for multi-table queries where table-qualified column names are needed.

**Example**
```csharp
builder.GroupByRaw("[Products].[Id], [Products].[Name]");
```

---

#### `GroupByIf`

**Signature**
```csharp
SqlQueryBuilder<T> GroupByIf(bool condition, Expression<Func<T, object>> column)
```

**Description**
Adds a GROUP BY column only when `condition` is `true`.

---

#### `Having` — raw string

**Signature**
```csharp
SqlQueryBuilder<T> Having(string rawHavingSql)
```

**Description**
Sets a raw HAVING clause. Used after `GroupBy`.

**Example**
```csharp
builder.GroupBy(x => x.Department).Having("COUNT(*) > 5");
// → GROUP BY [Department] HAVING COUNT(*) > 5
```

---

#### `Having` — SqlHavingBuilder (instance)

**Signature**
```csharp
SqlQueryBuilder<T> Having(SqlHavingBuilder<T> havingBuilder)
```

**Description**
Sets the HAVING clause from a `SqlHavingBuilder<T>`. Can be combined with a raw HAVING string — both are ANDed.

---

#### `Having` — SqlHavingBuilder (inline)

**Signature**
```csharp
SqlQueryBuilder<T> Having(Action<SqlHavingBuilder<T>> configure)
```

**Description**
Configures the HAVING clause inline.

**Example**
```csharp
builder.Having(h => h.CountGreaterThan(5).SumGreaterThan(x => x.Amount, 1000m));
```

---

#### `HavingIf` — builder overload

**Signature**
```csharp
SqlQueryBuilder<T> HavingIf(bool condition, Action<SqlHavingBuilder<T>> configure)
```

**Description**
Applies a fluent HAVING clause only when `condition` is `true`.

---

#### `HavingIf` — raw string overload

**Signature**
```csharp
SqlQueryBuilder<T> HavingIf(bool condition, string rawHavingSql)
```

**Description**
Applies a raw HAVING clause only when `condition` is `true`.

---

### ORDER BY

#### `OrderBy`

**Signature**
```csharp
SqlQueryBuilder<T> OrderBy(Expression<Func<T, object>> column, bool ascending = true)
```

**Description**
Adds a primary ORDER BY column.

**Example**
```csharp
builder.OrderBy(x => x.CreatedAt, ascending: false);
// → ORDER BY [CreatedAt] DESC
```

---

#### `OrderBy` — with NullsOrder

**Signature**
```csharp
SqlQueryBuilder<T> OrderBy(Expression<Func<T, object>> column, bool ascending, NullsOrder nulls)
```

**Description**
Adds ORDER BY with optional `NULLS FIRST` / `NULLS LAST`. The `nulls` parameter is silently ignored on dialects that don't support it (SQL Server, MySQL).

**Example**
```csharp
builder.OrderBy(x => x.DeletedAt, ascending: true, NullsOrder.Last);
// PostgreSQL → ORDER BY "DeletedAt" ASC NULLS LAST
// SQL Server → ORDER BY [DeletedAt] ASC  (nulls clause omitted)
```

---

#### `ThenBy`

**Signature**
```csharp
SqlQueryBuilder<T> ThenBy(Expression<Func<T, object>> column, bool ascending = true)
```

**Description**
Adds a secondary ORDER BY column. Functionally equivalent to a second `OrderBy` call.

---

#### `OrderByIf`

**Signature**
```csharp
SqlQueryBuilder<T> OrderByIf(bool condition, Expression<Func<T, object>> column, bool ascending = true)
```

**Description**
Applies ORDER BY only when `condition` is `true`.

---

#### `OrderByRaw`

**Signature**
```csharp
SqlQueryBuilder<T> OrderByRaw(string rawSql)
```

**Description**
Appends a raw ORDER BY expression verbatim. Useful for multi-table or computed sort expressions.

**Example**
```csharp
builder.OrderByRaw("[Products].[Price] DESC, [Name] ASC");
```

---

### Pagination

#### `Take`

**Signature**
```csharp
SqlQueryBuilder<T> Take(int count)
```

**Description**
Limits the number of rows returned. Maps to `TOP N` (SQL Server without OFFSET) or `LIMIT N`.

**Example**
```csharp
builder.From("Users").Take(10);
// SQL Server → SELECT TOP 10 * FROM [Users]
// PostgreSQL → SELECT * FROM "Users" LIMIT 10
```

---

#### `Skip`

**Signature**
```csharp
SqlQueryBuilder<T> Skip(int count)
```

**Description**
Skips the specified number of rows. Maps to OFFSET in all dialects.

**Example**
```csharp
builder.From("Users").OrderBy(x => x.Id).Skip(20).Take(10);
// SQL Server → SELECT * FROM [Users] ORDER BY [Id] ASC OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY
// PostgreSQL → SELECT * FROM "Users" ORDER BY "Id" ASC LIMIT 10 OFFSET 20
```

> **Note:** SQL Server requires an ORDER BY clause when using OFFSET.

---

#### `Page`

**Signature**
```csharp
SqlQueryBuilder<T> Page(int pageNumber, int pageSize)
```

**Description**
Sets 1-based page pagination. `Page(2, 20)` is equivalent to `Skip(20).Take(20)`.

**Example**
```csharp
builder.From("Users").OrderBy(x => x.Id).Page(3, 25);
// SQL Server → OFFSET 50 ROWS FETCH NEXT 25 ROWS ONLY
```

---

### DISTINCT / WithHint

#### `Distinct`

**Signature**
```csharp
SqlQueryBuilder<T> Distinct()
```

**Description**
Adds `DISTINCT` to the SELECT clause.

**Example**
```csharp
builder.From("Orders").Select(x => x.CustomerId).Distinct();
// → SELECT DISTINCT [CustomerId] FROM [Orders]
```

---

#### `WithHint`

**Signature**
```csharp
SqlQueryBuilder<T> WithHint(string hint)
```

**Description**
Appends a table hint after FROM. Primarily used with SQL Server for hints like `NOLOCK`.

**Example**
```csharp
builder.From("Users").WithHint("NOLOCK");
// SQL Server → FROM [Users] WITH (NOLOCK)
```

> **Note:** This generates the hint for all dialects. Use only with SQL Server — other dialects will include an invalid `WITH (...)` fragment.

---

### Set Operations

#### `Union`

**Signature**
```csharp
SqlQueryBuilder<T> Union(SqlQueryResult other)
```

**Description**
Appends a `UNION` (deduplicating) with a prebuilt query.

**Example**
```csharp
var active = new SqlQueryBuilder<User>(dialect).From("ActiveUsers").Build();
var inactive = new SqlQueryBuilder<User>(dialect).From("InactiveUsers").Build();
active.Union(inactive);  // not fluent — use on the builder before Build()

// Fluent:
builder.From("ActiveUsers").Union(
    new SqlQueryBuilder<User>(dialect).From("InactiveUsers").Build());
// → SELECT * FROM [ActiveUsers] UNION SELECT * FROM [InactiveUsers]
```

---

#### `UnionAll`

**Signature**
```csharp
SqlQueryBuilder<T> UnionAll(SqlQueryResult other)
```

**Description**
Appends a `UNION ALL` (including duplicates) with a prebuilt query.

---

#### `Except`

**Signature**
```csharp
SqlQueryBuilder<T> Except(SqlQueryResult other)
```

**Description**
Appends `EXCEPT` — returns rows in the main query that are not in the other query.

---

#### `Intersect`

**Signature**
```csharp
SqlQueryBuilder<T> Intersect(SqlQueryResult other)
```

**Description**
Appends `INTERSECT` — returns only rows that appear in both queries.

---

### CTEs

#### `WithCte` — SqlQueryResult

**Signature**
```csharp
SqlQueryBuilder<T> WithCte(string name, SqlQueryResult cteQuery)
```

**Description**
Prepends a Common Table Expression: `WITH name AS (cteQuery)`. Multiple calls add multiple CTEs.

**Example**
```csharp
var activeCte = new SqlQueryBuilder<User>(dialect)
    .From("Users")
    .Where(w => w.EqualTo(x => x.IsActive, true))
    .Build();

new SqlQueryBuilder<User>(dialect)
    .WithCte("ActiveUsers", activeCte)
    .From("ActiveUsers")
    .Build();
// → WITH "ActiveUsers" AS (SELECT * FROM "Users" WHERE ...) SELECT * FROM "ActiveUsers"
```

---

#### `WithCte` — inline

**Signature**
```csharp
SqlQueryBuilder<T> WithCte(string name, Action<SqlQueryBuilder<T>> configure)
```

**Description**
Prepends a CTE defined inline. Creates a nested `SqlQueryBuilder<T>` internally.

---

#### `WithRecursive`

**Signature**
```csharp
SqlQueryBuilder<T> WithRecursive(string name, SqlQueryResult anchor, SqlQueryResult recursive)
```

**Description**
Adds a recursive CTE. Generates: `WITH [RECURSIVE] name AS (anchor UNION ALL recursive)`. The `RECURSIVE` keyword is added automatically for dialects that require it (PostgreSQL, SQLite, MySQL).

**Example**
```csharp
var anchor = new SqlQueryBuilder<Category>(dialect).From("Categories").Where(x => x.ParentId == null).Build();
var rec    = new SqlQueryBuilder<Category>(dialect).From("Categories").WhereRaw("[Categories].[ParentId] = [tree].[Id]").Build();

builder.WithRecursive("tree", anchor, rec).From("tree").Build();
// PostgreSQL → WITH RECURSIVE "tree" AS (... UNION ALL ...) SELECT * FROM "tree"
// SQL Server → WITH [tree] AS (... UNION ALL ...) SELECT * FROM [tree]
```

---

### Row Locking

#### `ForUpdate`

**Signature**
```csharp
SqlQueryBuilder<T> ForUpdate()
```

**Description**
Appends `FOR UPDATE` at the end of the query for exclusive row locking. No-op on dialects that don't support row locking (SQL Server — use `WithHint("UPDLOCK")` instead; SQLite — silently ignored).

---

#### `ForShare`

**Signature**
```csharp
SqlQueryBuilder<T> ForShare()
```

**Description**
Appends `FOR SHARE` at the end of the query for shared row locking. No-op on dialects that don't support it.

---

### Tag / Build / ToPreviewSql

#### `Tag` — simple

**Signature**
```csharp
SqlQueryBuilder<T> Tag(string description)
```

**Description**
Labels the query. When `Build()` is called, the description is prepended as a SQL comment (`-- description`). Useful for log tracing.

**Example**
```csharp
builder.From("Users").Tag("Get active users").Build();
// SQL: -- Get active users
//      SELECT * FROM [Users]
```

---

#### `Tag` — with logger

**Signature**
```csharp
SqlQueryBuilder<T> Tag(string description, Action<string>? logger)
```

**Description**
Same as above, but also invokes `logger` with `"[SQL] {description}"` when `Build()` is called. If `logger` is `null`, the tag is embedded only as a SQL comment.

**Example**
```csharp
builder.Tag("Get active users", msg => Console.WriteLine(msg));
// Console output: [SQL] Get active users
```

---

#### `ToPreviewSql`

**Signature**
```csharp
string ToPreviewSql()
```

**Description**
Returns a preview of the SQL mid-chain without finalizing the builder. Useful in a debugger watch window. Returns a fallback message if the builder state is incomplete.

---

#### `Build`

**Signature**
```csharp
SqlQueryResult Build()
```

**Description**
Assembles and returns the final `SqlQueryResult`. This call is terminal — the builder can be reused but the result is immutable.

---

## SqlInsertBuilder

`SqlInsertBuilder<T>` builds parameterized `INSERT` statements. Supports single-row, multi-row, INSERT … SELECT, and conflict resolution patterns.

```csharp
var result = new SqlInsertBuilder<User>(new SqlServerDialect())
    .Into("Users")
    .Set(x => x.Name, "Alice")
    .Set(x => x.Age, 30)
    .Build();
// → INSERT INTO [Users] ([Name], [Age]) VALUES (@pi0, @pi1)
```

---

### `Into`

**Signature**
```csharp
SqlInsertBuilder<T> Into(string tableName, string? schema = null)
```

**Description**
Sets the target table name and optional schema.

---

### `Set`

**Signature**
```csharp
SqlInsertBuilder<T> Set<TValue>(Expression<Func<T, TValue>> column, TValue value)
```

**Description**
Maps a typed column to its insert value for the current row.

**Example**
```csharp
builder.Into("Users").Set(x => x.Name, "Alice").Set(x => x.Age, 30);
// → INSERT INTO [Users] ([Name], [Age]) VALUES (@pi0, @pi1)
```

---

### `NextRow`

**Signature**
```csharp
SqlInsertBuilder<T> NextRow()
```

**Description**
Starts a new row for a multi-row insert. Subsequent `Set` calls populate the new row. All rows must have the same number of columns.

**Example**
```csharp
builder.Into("Users")
    .Set(x => x.Name, "Alice").Set(x => x.Age, 30)
    .NextRow()
    .Set(x => x.Name, "Bob").Set(x => x.Age, 25)
    .Build();
// → INSERT INTO [Users] ([Name], [Age]) VALUES (@pi0, @pi1), (@pi2, @pi3)
```

---

### `OutputInserted`

**Signature**
```csharp
SqlInsertBuilder<T> OutputInserted()
```

**Description**
Appends `OUTPUT INSERTED.*` before VALUES. SQL Server only — use for retrieving auto-generated identity or computed columns after insert.

**Example**
```csharp
builder.Into("Users").Set(x => x.Name, "Alice").OutputInserted().Build();
// → INSERT INTO [Users] ([Name]) OUTPUT INSERTED.* VALUES (@pi0)
```

> **Note:** Throws `InvalidOperationException` if used with a non-SQL Server dialect. Use `Returning()` for PostgreSQL / SQLite.

---

### `Returning`

**Signature**
```csharp
SqlInsertBuilder<T> Returning(params Expression<Func<T, object>>[] columns)
```

**Description**
Appends `RETURNING *` or `RETURNING col1, col2` after VALUES. Supported by PostgreSQL and SQLite 3.35+.

**Example**
```csharp
builder.Into("Users").Set(x => x.Name, "Alice")
    .Returning(x => x.Id, x => x.CreatedAt)
    .Build();
// → INSERT INTO "Users" ("Name") VALUES (@pi0) RETURNING "Id", "CreatedAt"
```

---

### `SelectFrom`

**Signature**
```csharp
SqlInsertBuilder<T> SelectFrom(SqlQueryResult selectQuery)
```

**Description**
Specifies a SELECT query whose results will be inserted into the table. Mutually exclusive with `Set`.

**Example**
```csharp
var select = new SqlQueryBuilder<User>(dialect)
    .From("Archive")
    .Select(x => x.Name)
    .Build();

new SqlInsertBuilder<User>(dialect)
    .Into("Users")
    .Columns(x => x.Name)
    .SelectFrom(select)
    .Build();
// → INSERT INTO [Users] ([Name]) SELECT [Name] FROM [Archive]
```

---

### `Columns`

**Signature**
```csharp
SqlInsertBuilder<T> Columns(params Expression<Func<T, object>>[] columns)
```

**Description**
Specifies the target columns for INSERT … SELECT. If not called, the column list is omitted: `INSERT INTO table SELECT …`.

---

### `Tag`

**Signature**
```csharp
SqlInsertBuilder<T> Tag(string description)
```

**Description**
Labels the query with a comment prepended to the SQL.

---

### Conflict Resolution

#### `OrIgnore` (SQLite)

**Signature**
```csharp
SqlInsertBuilder<T> OrIgnore()
```

**Description**
Emits `INSERT OR IGNORE INTO ...`. SQLite only.

---

#### `OrReplace` (SQLite)

**Signature**
```csharp
SqlInsertBuilder<T> OrReplace()
```

**Description**
Emits `INSERT OR REPLACE INTO ...`. SQLite only.

---

#### `OnConflictDoNothing` (PostgreSQL / SQLite)

**Signature**
```csharp
SqlInsertBuilder<T> OnConflictDoNothing()
```

**Description**
Appends `ON CONFLICT DO NOTHING` after VALUES.

**Example**
```csharp
builder.Into("Users").Set(x => x.Email, "a@b.com").OnConflictDoNothing().Build();
// → INSERT INTO "Users" ("Email") VALUES (@pi0) ON CONFLICT DO NOTHING
```

---

#### `OnConflictDoUpdate` (PostgreSQL / SQLite)

**Signature**
```csharp
SqlInsertBuilder<T> OnConflictDoUpdate(
    Action<SqlInsertBuilder<T>> conflictKeys,
    Action<SqlInsertBuilder<T>> updateAssignments)
```

**Description**
Appends `ON CONFLICT (cols) DO UPDATE SET ...` after VALUES. Pass actions that call `AddConflictKey` and `AddConflictUpdate`.

**Example**
```csharp
builder.Into("Users")
    .Set(x => x.Email, "a@b.com")
    .Set(x => x.Name, "Alice")
    .OnConflictDoUpdate(
        keys    => keys.AddConflictKey(x => x.Email),
        updates => updates.AddConflictUpdate(x => x.Name, "Alice"))
    .Build();
// → INSERT INTO "Users" ("Email", "Name") VALUES (@pi0, @pi1)
//   ON CONFLICT ("Email") DO UPDATE SET "Name" = @pu0
```

---

#### `AddConflictKey`

**Signature**
```csharp
SqlInsertBuilder<T> AddConflictKey<TValue>(Expression<Func<T, TValue>> column)
```

**Description**
Registers a column as part of the ON CONFLICT target column list. Called inside the `conflictKeys` action of `OnConflictDoUpdate`.

---

#### `AddConflictUpdate`

**Signature**
```csharp
SqlInsertBuilder<T> AddConflictUpdate<TValue>(Expression<Func<T, TValue>> column, TValue value)
```

**Description**
Registers a column = value assignment for the DO UPDATE SET clause. Called inside the `updateAssignments` action of `OnConflictDoUpdate`.

---

#### `InsertIgnore` (MySQL)

**Signature**
```csharp
SqlInsertBuilder<T> InsertIgnore()
```

**Description**
Emits `INSERT IGNORE INTO ...`. MySQL only.

---

#### `OnDuplicateKeyUpdate` (MySQL)

**Signature**
```csharp
SqlInsertBuilder<T> OnDuplicateKeyUpdate(Action<SqlInsertBuilder<T>> configure)
```

**Description**
Appends `ON DUPLICATE KEY UPDATE col = @pu...` after VALUES. MySQL only.

**Example**
```csharp
builder.Into("Users")
    .Set(x => x.Email, "a@b.com")
    .OnDuplicateKeyUpdate(u => u.AddDuplicateKeyAssignment(x => x.Name, "Alice"))
    .Build();
// → INSERT INTO `Users` (`Email`) VALUES (@pi0) ON DUPLICATE KEY UPDATE `Name` = @pu0
```

---

#### `AddDuplicateKeyAssignment`

**Signature**
```csharp
SqlInsertBuilder<T> AddDuplicateKeyAssignment<TValue>(Expression<Func<T, TValue>> column, TValue value)
```

**Description**
Registers a column = value assignment for the ON DUPLICATE KEY UPDATE clause.

---

### `Build` (Insert)

**Signature**
```csharp
SqlQueryResult Build()
```

**Description**
Assembles and returns the final `SqlQueryResult`.

---

## SqlUpdateBuilder

`SqlUpdateBuilder<T>` builds parameterized `UPDATE` statements.

```csharp
var result = new SqlUpdateBuilder<User>(new SqlServerDialect())
    .Table("Users")
    .Set(x => x.Name, "Bob")
    .Set(x => x.IsActive, false)
    .Where(w => w.EqualTo(x => x.Id, 42))
    .Build();
// → UPDATE [Users] SET [Name] = @pu0, [IsActive] = @pu1 WHERE [Id] = @pw0
```

> **Safety:** By default, `Build()` throws if no WHERE clause is set. Call `AllowUpdateAll()` to explicitly allow updating all rows.

---

### `Table`

**Signature**
```csharp
SqlUpdateBuilder<T> Table(string tableName, string? schema = null)
```

**Description**
Sets the target table name and optional schema.

---

### `Set`

**Signature**
```csharp
SqlUpdateBuilder<T> Set<TValue>(Expression<Func<T, TValue>> column, TValue value)
```

**Description**
Adds a parameterized SET clause. Parameters use the `pu` prefix.

**Example**
```csharp
builder.Table("Users").Set(x => x.Name, "Bob").Set(x => x.IsActive, false);
// SET [Name] = @pu0, [IsActive] = @pu1
```

---

### `SetRaw`

**Signature**
```csharp
SqlUpdateBuilder<T> SetRaw(Expression<Func<T, object>> column, string rawExpression)
```

**Description**
Adds a raw (non-parameterized) SET clause. Use for server-side expressions like `GETDATE()` or arithmetic on existing values.

**Example**
```csharp
builder.SetRaw(x => x.Counter, "Counter + 1");
// SET [Counter] = Counter + 1

builder.SetRaw(x => x.UpdatedAt, "GETDATE()");
// SET [UpdatedAt] = GETDATE()
```

---

### `SetColumn`

**Signature**
```csharp
SqlUpdateBuilder<T> SetColumn<TValue>(
    Expression<Func<T, TValue>> target,
    Expression<Func<T, TValue>> source)
```

**Description**
Copies one column's current value to another column with no parameters.

**Example**
```csharp
builder.SetColumn(x => x.NameBackup, x => x.Name);
// SET [NameBackup] = [Name]
```

---

### `Where` (Update)

Three overloads are available — same semantics as `SqlQueryBuilder`:

```csharp
SqlUpdateBuilder<T> Where(SqlWhereBuilder<T> whereBuilder)
SqlUpdateBuilder<T> Where(Action<SqlWhereBuilder<T>> configure)
SqlUpdateBuilder<T> Where(Expression<Func<T, bool>> predicate)
SqlUpdateBuilder<T> Where(ValiFlow<T> filter)
```

---

### `FromTable`

**Signature**
```csharp
SqlUpdateBuilder<T> FromTable(string sourceTable, string? alias = null)
```

**Description**
Adds a FROM (SQL Server / PostgreSQL) or JOIN (MySQL) source table to the UPDATE statement, enabling multi-table updates. Throws on SQLite and Oracle.

**Example**
```csharp
// SQL Server
builder.Table("Orders")
    .Set(x => x.Status, "Shipped")
    .FromTable("Shipments", "s")
    .JoinOn("INNER JOIN", "[Shipments].[OrderId] = [Orders].[Id]")
    .Where(x => x.Status == "Pending")
    .Build();
// → UPDATE [Orders] SET [Status] = @pu0 FROM [Shipments] [s] INNER JOIN [Shipments].[OrderId] = [Orders].[Id]
//   WHERE [Status] = @p0
```

---

### `JoinOn`

**Signature**
```csharp
SqlUpdateBuilder<T> JoinOn(string joinType, string onCondition)
```

**Description**
Adds a JOIN condition to the FROM clause of the UPDATE statement. Used with `FromTable`.

---

### `AllowUpdateAll`

**Signature**
```csharp
SqlUpdateBuilder<T> AllowUpdateAll()
```

**Description**
Explicitly allows generating an UPDATE without a WHERE clause (affects all rows). Call only when a full-table update is intentional.

---

### `OutputUpdated`

**Signature**
```csharp
SqlUpdateBuilder<T> OutputUpdated()
```

**Description**
Appends `OUTPUT INSERTED.*` after SET. SQL Server only. Returns the updated rows' new values.

---

### `Returning` (Update)

**Signature**
```csharp
SqlUpdateBuilder<T> Returning(params Expression<Func<T, object>>[] columns)
```

**Description**
Appends `RETURNING *` or specified columns after WHERE. PostgreSQL / SQLite only.

---

### `Tag` (Update)

**Signature**
```csharp
SqlUpdateBuilder<T> Tag(string description)
```

**Description**
Labels the query with a SQL comment.

---

### `Build` (Update)

**Signature**
```csharp
SqlQueryResult Build()
```

**Description**
Assembles and returns the final `SqlQueryResult`. Throws if no SET assignments are present or if no WHERE clause is set (unless `AllowUpdateAll()` was called).

---

## SqlDeleteBuilder

`SqlDeleteBuilder<T>` builds parameterized `DELETE` statements.

```csharp
var result = new SqlDeleteBuilder<User>(new SqlServerDialect())
    .From("Users")
    .Where(w => w.EqualTo(x => x.Id, 42))
    .Build();
// → DELETE FROM [Users] WHERE [Id] = @pw0
```

> **Safety:** By default, `Build()` throws if no WHERE clause is set. Call `AllowDeleteAll()` to explicitly allow deleting all rows.

---

### `From` (Delete)

**Signature**
```csharp
SqlDeleteBuilder<T> From(string tableName, string? schema = null)
```

**Description**
Sets the target table name and optional schema.

---

### `Where` (Delete)

Four overloads — same semantics as `SqlUpdateBuilder`:

```csharp
SqlDeleteBuilder<T> Where(SqlWhereBuilder<T> whereBuilder)
SqlDeleteBuilder<T> Where(Action<SqlWhereBuilder<T>> configure)
SqlDeleteBuilder<T> Where(Expression<Func<T, bool>> predicate)
SqlDeleteBuilder<T> Where(ValiFlow<T> filter)
```

---

### `OutputDeleted`

**Signature**
```csharp
SqlDeleteBuilder<T> OutputDeleted()
```

**Description**
Appends `OUTPUT DELETED.*` after DELETE FROM. SQL Server only — returns the deleted rows.

**Example**
```csharp
builder.From("Users").Where(x => x.Id == 42).OutputDeleted().Build();
// → DELETE FROM [Users] OUTPUT DELETED.* WHERE [Id] = @p0
```

---

### `Returning` (Delete)

**Signature**
```csharp
SqlDeleteBuilder<T> Returning(params Expression<Func<T, object>>[] columns)
```

**Description**
Appends `RETURNING *` or specified columns. PostgreSQL / SQLite only.

**Example**
```csharp
builder.From("Users").Where(x => x.Id == 42).Returning(x => x.Id, x => x.Name).Build();
// → DELETE FROM "Users" WHERE "Id" = @p0 RETURNING "Id", "Name"
```

---

### `AllowDeleteAll`

**Signature**
```csharp
SqlDeleteBuilder<T> AllowDeleteAll()
```

**Description**
Explicitly allows generating a DELETE without a WHERE clause (affects all rows).

---

### `Tag` (Delete)

**Signature**
```csharp
SqlDeleteBuilder<T> Tag(string description)
```

**Description**
Labels the query with a SQL comment.

---

### `Build` (Delete)

**Signature**
```csharp
SqlQueryResult Build()
```

**Description**
Assembles and returns the final `SqlQueryResult`.

---

## SqlTruncateBuilder

`SqlTruncateBuilder<T>` builds `TRUNCATE TABLE` statements.

> **Note:** SQLite does not support TRUNCATE TABLE. `Build()` throws `InvalidOperationException` when using `SqliteDialect`. Use `SqlDeleteBuilder` with `AllowDeleteAll()` instead.

```csharp
var result = new SqlTruncateBuilder<User>(new SqlServerDialect())
    .Table("Users")
    .Build();
// → TRUNCATE TABLE [Users]
```

---

### `Table` (Truncate)

**Signature**
```csharp
SqlTruncateBuilder<T> Table(string tableName, string? schema = null)
```

**Description**
Sets the table to truncate. If not called, uses `typeof(T).Name`.

---

### `Tag` (Truncate)

**Signature**
```csharp
SqlTruncateBuilder<T> Tag(string description)
```

**Description**
Adds a SQL comment header for traceability.

---

### `Build` (Truncate)

**Signature**
```csharp
SqlQueryResult Build()
```

**Description**
Builds and returns the `TRUNCATE TABLE` statement as a `SqlQueryResult` with an empty parameters dictionary.

**Example with schema**
```csharp
new SqlTruncateBuilder<Order>(new SqlServerDialect())
    .Table("Orders", schema: "dbo")
    .Tag("Clear orders table")
    .Build();
// → -- Clear orders table
//   TRUNCATE TABLE [dbo].[Orders]
```

---

## SqlMergeBuilder

`SqlMergeBuilder<TTarget, TSource>` builds SQL Server `MERGE` statements. For PostgreSQL upsert, use `SqlInsertBuilder.OnConflictDoUpdate` instead.

> **Note:** `Build()` throws `InvalidOperationException` on dialects other than `SqlServerDialect`. Only SQL Server supports the MERGE syntax as implemented here.

```csharp
var result = new SqlMergeBuilder<User, UserDto>(new SqlServerDialect())
    .Into("Users")
    .Using("UserUpdates", "src")
    .On(t => t.Id, s => s.Id)
    .WhenMatchedUpdate(m => m
        .MatchedSetColumn(t => t.Name, s => s.Name)
        .MatchedSetColumn(t => t.Email, s => s.Email))
    .WhenNotMatchedInsert(i => i
        .NotMatchedInsertColumn(t => t.Id, s => s.Id)
        .NotMatchedInsertColumn(t => t.Name, s => s.Name))
    .Build();
```

---

### `Into` (Merge)

**Signature**
```csharp
SqlMergeBuilder<TTarget, TSource> Into(string tableName, string? schema = null)
```

**Description**
Sets the target table for the MERGE.

---

### `Using`

**Signature**
```csharp
SqlMergeBuilder<TTarget, TSource> Using(string sourceTable, string alias = "src")
```

**Description**
Sets the source table name and alias used in the `USING` clause.

---

### `On`

**Signature**
```csharp
SqlMergeBuilder<TTarget, TSource> On(
    Expression<Func<TTarget, object>> targetKey,
    Expression<Func<TSource, object>> sourceKey)
```

**Description**
Adds a join condition: `target.col = source.col`. Can be called multiple times for composite keys.

**Example**
```csharp
builder.On(t => t.TenantId, s => s.TenantId).On(t => t.ExternalId, s => s.ExternalId);
// ON target.[TenantId] = src.[TenantId] AND target.[ExternalId] = src.[ExternalId]
```

---

### `WhenMatchedUpdate`

**Signature**
```csharp
SqlMergeBuilder<TTarget, TSource> WhenMatchedUpdate(
    Action<SqlMergeBuilder<TTarget, TSource>> configure)
```

**Description**
Configures the `WHEN MATCHED THEN UPDATE SET ...` clause. The `configure` action calls `MatchedSetColumn` and/or `MatchedSetValue`.

---

### `MatchedSetColumn`

**Signature**
```csharp
SqlMergeBuilder<TTarget, TSource> MatchedSetColumn(
    Expression<Func<TTarget, object>> targetCol,
    Expression<Func<TSource, object>> sourceCol)
```

**Description**
In WHEN MATCHED UPDATE: sets `target.targetCol = source.sourceCol` (column-to-column copy, no parameters).

---

### `MatchedSetValue`

**Signature**
```csharp
SqlMergeBuilder<TTarget, TSource> MatchedSetValue<TValue>(
    Expression<Func<TTarget, object>> targetCol, TValue value)
```

**Description**
In WHEN MATCHED UPDATE: sets `target.targetCol = @pmN` (parameterized value).

---

### `WhenNotMatchedInsert`

**Signature**
```csharp
SqlMergeBuilder<TTarget, TSource> WhenNotMatchedInsert(
    Action<SqlMergeBuilder<TTarget, TSource>> configure)
```

**Description**
Configures the `WHEN NOT MATCHED BY TARGET THEN INSERT (...)` clause. The `configure` action calls `NotMatchedInsertColumn` and/or `NotMatchedInsertValue`.

---

### `NotMatchedInsertColumn`

**Signature**
```csharp
SqlMergeBuilder<TTarget, TSource> NotMatchedInsertColumn(
    Expression<Func<TTarget, object>> targetCol,
    Expression<Func<TSource, object>> sourceCol)
```

**Description**
In WHEN NOT MATCHED INSERT: maps `targetCol = source.sourceCol`.

---

### `NotMatchedInsertValue`

**Signature**
```csharp
SqlMergeBuilder<TTarget, TSource> NotMatchedInsertValue<TValue>(
    Expression<Func<TTarget, object>> targetCol, TValue value)
```

**Description**
In WHEN NOT MATCHED INSERT: maps `targetCol = @pmN` (parameterized value).

---

### `WhenNotMatchedBySourceDelete`

**Signature**
```csharp
SqlMergeBuilder<TTarget, TSource> WhenNotMatchedBySourceDelete()
```

**Description**
Adds `WHEN NOT MATCHED BY SOURCE THEN DELETE`. Removes target rows that have no corresponding source row.

---

### `Tag` (Merge)

**Signature**
```csharp
SqlMergeBuilder<TTarget, TSource> Tag(string description)
```

**Description**
Adds a SQL comment header and enables console tag for traceability.

---

### `Build` (Merge)

**Signature**
```csharp
SqlQueryResult Build()
```

**Description**
Builds and returns the parameterized MERGE statement. Throws if `Into()`, `Using()`, `On()`, and at least one WHEN clause have not been called.

**Full example**
```csharp
var result = new SqlMergeBuilder<User, UserDto>(new SqlServerDialect())
    .Into("Users", schema: "dbo")
    .Using("Staging", "src")
    .On(t => t.Email, s => s.Email)
    .WhenMatchedUpdate(m => m
        .MatchedSetColumn(t => t.Name,       s => s.Name)
        .MatchedSetColumn(t => t.UpdatedAt,  s => s.UpdatedAt))
    .WhenNotMatchedInsert(i => i
        .NotMatchedInsertColumn(t => t.Email,     s => s.Email)
        .NotMatchedInsertColumn(t => t.Name,      s => s.Name)
        .NotMatchedInsertValue(t => t.CreatedAt,  DateTime.UtcNow))
    .WhenNotMatchedBySourceDelete()
    .Tag("Sync users from staging")
    .Build();

// → -- Sync users from staging
//   MERGE INTO [dbo].[Users] AS target
//   USING [Staging] AS src ON target.[Email] = src.[Email]
//   WHEN MATCHED THEN
//       UPDATE SET target.[Name] = src.[Name], target.[UpdatedAt] = src.[UpdatedAt]
//   WHEN NOT MATCHED BY TARGET THEN
//       INSERT ([Email], [Name], [CreatedAt]) VALUES (src.[Email], src.[Name], @pm0)
//   WHEN NOT MATCHED BY SOURCE THEN DELETE;
```
