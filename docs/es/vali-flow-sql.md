# Vali-Flow.Sql — Referencia Completa

## Tabla de Contenidos

1. [Descripción General](#descripción-general)
2. [Dialectos](#dialectos)
3. [Métodos de Extensión](#métodos-de-extensión-valiflowsqlextensions)
4. [SqlResult / SqlQueryResult](#sqlresult--sqlqueryresult)
5. [SqlQueryBuilder](#sqlquerybuilder)
   - [SELECT](#select)
   - [FROM](#from)
   - [JOINs](#joins)
   - [WHERE](#where)
   - [GROUP BY / HAVING](#group-by--having)
   - [ORDER BY](#order-by)
   - [Paginación](#paginación)
   - [DISTINCT / WithHint](#distinct--withhint)
   - [Operaciones de Conjunto](#operaciones-de-conjunto)
   - [CTEs](#ctes)
   - [Bloqueo de Filas](#bloqueo-de-filas)
   - [Tag / Build / ToPreviewSql](#tag--build--topreviewsql)
6. [SqlInsertBuilder](#sqlinsertbuilder)
7. [SqlUpdateBuilder](#sqlupdatebuilder)
8. [SqlDeleteBuilder](#sqldeletebuilder)
9. [SqlTruncateBuilder](#sqltruncatebuilder)
10. [SqlMergeBuilder](#sqlmergebuilder)

---

## Descripción General

**Vali-Flow.Sql** traduce expresiones C# fuertemente tipadas y constructores fluidos en sentencias SQL parametrizadas. Está diseñado para integrarse con Dapper, ADO.NET o cualquier framework que acepte un string SQL más un diccionario de parámetros.

Características principales:

- Traducción de árboles de expresión a cláusula WHERE via `ValiFlow<T>.ToSql(dialect)`
- Construcción de consultas SELECT completas via `SqlQueryBuilder<T>`
- Constructores para INSERT, UPDATE, DELETE, TRUNCATE y MERGE
- Cinco dialectos SQL integrados (SQL Server, PostgreSQL, MySQL, SQLite, Oracle)
- Todas las consultas son parametrizadas — sin interpolación de valores del usuario
- Funciones de ventana: ROW_NUMBER, RANK, DENSE_RANK, LAG, LEAD, FIRST_VALUE, LAST_VALUE y funciones de agregado OVER
- CTEs (regulares y recursivas), JOINs con subconsultas, UNION / EXCEPT / INTERSECT, bloqueo de filas

**Instalación**

```bash
dotnet add package Vali-Flow.Sql
```

**Ejemplo rápido**

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

## Dialectos

Cada constructor y método de extensión requiere una instancia de `ISqlDialect`. Selecciona el que corresponda a tu motor de base de datos.

| Característica | `SqlServerDialect` | `PostgreSqlDialect` | `MySqlDialect` | `SqliteDialect` | `OracleDialect` |
|---|---|---|---|---|---|
| **Prefijo de parámetro** | `@` | `@` | `@` | `@` | `:` |
| **Comillas de columna** | `[col]` | `"col"` | `` `col` `` | `"col"` | `"col"` |
| **Comillas de tabla** | `[tbl]` | `"tbl"` | `` `tbl` `` | `"tbl"` | `"tbl"` |
| **LIKE sin distinción de mayúsculas** | `LOWER([col]) LIKE LOWER(@p)` | `"col" ILIKE @p` | `` LOWER(`col`) LIKE LOWER(@p) `` | `LOWER("col") LIKE LOWER(@p)` | `LOWER("col") LIKE LOWER(:p)` |
| **TOP / LIMIT** | `TOP N` (sin OFFSET) / `OFFSET … ROWS FETCH NEXT N ROWS ONLY` | `LIMIT N OFFSET M` | `LIMIT N OFFSET M` | `LIMIT N OFFSET M` | `FETCH FIRST N ROWS ONLY` |
| **Soporte TRUNCATE** | Sí | Sí | Sí | No (lanza excepción) | Sí |
| **Soporte MERGE** | Sí | No | No | No | No |
| **OUTPUT INSERTED/DELETED** | Sí | No | No | No | No |
| **Cláusula RETURNING** | No | Sí | Sí (solo INSERT) | Sí | No |
| **ON CONFLICT** | No | Sí | No | Sí | No |
| **ON DUPLICATE KEY** | No | No | Sí | No | No |
| **INSERT OR IGNORE/REPLACE** | No | No | No | Sí | No |
| **FOR UPDATE / FOR SHARE** | No (usa `WithHint`) | Sí | Sí | No (ignorado silenciosamente) | Sí |
| **NULLS FIRST/LAST** | No (ignorado silenciosamente) | Sí | No | Sí | Sí |
| **Palabra clave CTE recursiva** | *(omitida)* | `RECURSIVE` | `RECURSIVE` | `RECURSIVE` | *(omitida)* |
| **UPDATE … FROM/JOIN** | Sí (FROM después de SET) | Sí (FROM después de SET) | Sí (JOIN antes de SET) | No | No |
| **IS DISTINCT FROM** | Emulado | Nativo | Emulado | Nativo | Emulado |
| **Extracción de parte de fecha** | `DATEPART(part, col)` | `EXTRACT(part FROM col)` | `EXTRACT(part FROM col)` | `strftime('%Y', col)` | `EXTRACT(part FROM col)` |
| **Timestamp actual** | `GETDATE()` | `NOW()` | `NOW()` | `CURRENT_TIMESTAMP` | `SYSDATE` |
| **Longitud de cadena** | `LEN(col)` | `LENGTH(col)` | `LENGTH(col)` | `LENGTH(col)` | `LENGTH(col)` |
| **Concatenación de cadenas** | `a + b` | `a \|\| b` | `CONCAT(a, b)` | `a \|\| b` | `a \|\| b` |
| **Recorte de espacios** | `LTRIM(RTRIM(col))` | `TRIM(col)` | `TRIM(col)` | `TRIM(col)` | `TRIM(col)` |

**Uso**

```csharp
ISqlDialect dialect = new SqlServerDialect();
ISqlDialect dialect = new PostgreSqlDialect();
ISqlDialect dialect = new MySqlDialect();
ISqlDialect dialect = new SqliteDialect();
ISqlDialect dialect = new OracleDialect();
```

---

## Métodos de Extensión (`ValiFlowSqlExtensions`)

### `ToSql` — sobrecarga ValiFlow

**Firma**
```csharp
SqlResult ToSql<T>(this ValiFlow<T> flow, ISqlDialect dialect)
```

**Descripción**
Traduce todas las condiciones construidas en un `ValiFlow<T>` en un fragmento SQL parametrizado para la cláusula WHERE (sin la palabra clave `WHERE`).

**Ejemplo**
```csharp
var filter = new ValiFlow<User>()
    .NotNull(x => x.Name)
    .GreaterThan(x => x.Age, 18);

var sql = filter.ToSql(new SqlServerDialect());
// sql.Sql        → "[Name] IS NOT NULL AND [Age] > @p0"
// sql.Parameters → { "p0": 18 }
```

---

### `ToSql` — sobrecarga ValiFlowQuery

**Firma**
```csharp
SqlResult ToSql<T>(this ValiFlowQuery<T> flow, ISqlDialect dialect)
```

**Descripción**
Igual que la sobrecarga anterior pero para instancias de `ValiFlowQuery<T>`.

---

### `ToSql` — sobrecarga Expression

**Firma**
```csharp
SqlResult ToSql<T>(this Expression<Func<T, bool>> expression, ISqlDialect dialect)
```

**Descripción**
Traduce directamente un `Expression<Func<T, bool>>` precompilado, sin necesitar un wrapper `ValiFlow<T>`.

**Ejemplo**
```csharp
Expression<Func<User, bool>> expr = u => u.IsActive && u.Age > 18;
var sql = expr.ToSql(new PostgreSqlDialect());
// sql.Sql → "\"IsActive\" = true AND \"Age\" > @p0"
```

---

### `ToSqlCount` — sobrecarga ValiFlow

**Firma**
```csharp
SqlQueryResult ToSqlCount<T>(
    this ValiFlow<T>? flow,
    ISqlDialect dialect,
    string? tableName = null) where T : class
```

**Descripción**
Genera una consulta `SELECT COUNT(*) FROM [tabla] WHERE ...`. Pasa `null` como `flow` para contar todas las filas.

**Ejemplo**
```csharp
var countResult = new ValiFlow<User>()
    .EqualTo(x => x.IsActive, true)
    .ToSqlCount(new SqlServerDialect(), "Users");

// countResult.Sql → "SELECT COUNT(*) FROM [Users] WHERE [IsActive] = @p0"
var count = connection.ExecuteScalar<int>(countResult.Sql, countResult.Parameters);
```

---

### `ToSqlCount` — sobrecarga ValiFlowQuery

**Firma**
```csharp
SqlQueryResult ToSqlCount<T>(
    this ValiFlowQuery<T>? flow,
    ISqlDialect dialect,
    string? tableName = null) where T : class
```

**Descripción**
Igual que la sobrecarga anterior para `ValiFlowQuery<T>`.

---

## SqlResult / SqlQueryResult

### `SqlResult`

Devuelto por los métodos de extensión `ToSql()`. Contiene solo un fragmento WHERE (sin SELECT / FROM).

| Propiedad / Método | Tipo | Descripción |
|---|---|---|
| `Sql` | `string` | Fragmento SQL parametrizado para WHERE (sin la palabra clave `WHERE`) |
| `Parameters` | `IReadOnlyDictionary<string, object>` | Parámetros nominales listos para Dapper o ADO.NET |
| `ApplyTo(IDbCommand)` | `void` | Agrega todos los parámetros a un comando ADO.NET |
| `ToString()` | `string` | Retorna `Sql` |

**Ejemplo con ApplyTo**
```csharp
var sql = filter.ToSql(new SqliteDialect());
cmd.CommandText = $"SELECT * FROM Products WHERE {sql.Sql}";
sql.ApplyTo(cmd);
var reader = cmd.ExecuteReader();
```

---

### `SqlQueryResult`

Devuelto por `Build()` en todos los constructores, y por `ToSqlCount()`. Contiene una sentencia SQL completa.

| Propiedad / Método | Tipo | Descripción |
|---|---|---|
| `Sql` | `string` | Consulta SQL parametrizada completa |
| `Parameters` | `IReadOnlyDictionary<string, object>` | Parámetros nominales |
| `ApplyTo(IDbCommand)` | `void` | Agrega todos los parámetros a un comando ADO.NET |
| `ToDebugSql()` | `string` | Retorna el SQL con los valores de parámetros incrustados — **solo para logging, nunca ejecutar** |
| `ToString()` | `string` | Retorna `Sql` |

**Ejemplo con ToDebugSql**
```csharp
var result = new SqlQueryBuilder<User>(new SqlServerDialect())
    .From("Users")
    .Where(x => x.Age > 18)
    .Build();

Console.WriteLine(result.ToDebugSql());
// SELECT * FROM [Users] WHERE [Age] > 18
```

> **Advertencia:** `ToDebugSql()` incrusta los valores reales en el string SQL para legibilidad. Nunca pases este string a una base de datos — es únicamente para logging y depuración.

---

## SqlQueryBuilder

`SqlQueryBuilder<T>` es el punto de entrada principal para construir consultas SELECT completas. Es una clase sellada genérica — `T` es el tipo de entidad cuyas propiedades están disponibles como columnas tipadas.

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

#### `Select` — múltiples columnas

**Firma**
```csharp
SqlQueryBuilder<T> Select(params Expression<Func<T, object>>[] columns)
```

**Descripción**
Agrega una o más columnas tipadas a la lista SELECT. Si nunca se llama, la consulta utiliza `SELECT *` por defecto.

**Ejemplo**
```csharp
builder.Select(x => x.Id, x => x.Name);
// → SELECT [Id], [Name] FROM ...
```

---

#### `Select` — columna única con alias

**Firma**
```csharp
SqlQueryBuilder<T> Select(Expression<Func<T, object>> column, string? alias)
```

**Descripción**
Agrega una columna tipada con alias opcional.

**Ejemplo**
```csharp
builder.Select(x => x.Name, "UserName");
// → SELECT [Name] AS [UserName] FROM ...
```

---

#### `SelectAll`

**Firma**
```csharp
SqlQueryBuilder<T> SelectAll()
```

**Descripción**
Restablece la lista de columnas a `SELECT *`, limpiando cualquier columna previamente agregada.

---

#### `SelectRaw`

**Firma**
```csharp
SqlQueryBuilder<T> SelectRaw(string rawSql)
```

**Descripción**
Agrega un fragmento SQL literal a la lista SELECT. Útil para expresiones que no se pueden representar mediante columnas tipadas.

**Ejemplo**
```csharp
builder.SelectRaw("GETDATE() AS [Now]");
// → SELECT GETDATE() AS [Now] FROM ...
```

---

#### `SelectRawIf`

**Firma**
```csharp
SqlQueryBuilder<T> SelectRawIf(bool condition, string rawSql)
```

**Descripción**
Agrega una expresión SELECT literal solo cuando `condition` es `true`.

---

#### `SelectIf`

**Firma**
```csharp
SqlQueryBuilder<T> SelectIf(bool condition, Expression<Func<T, object>> column, string? alias = null)
```

**Descripción**
Agrega una columna tipada solo cuando `condition` es `true`.

---

#### `SelectCount` — estrella

**Firma**
```csharp
SqlQueryBuilder<T> SelectCount(string? alias = null)
```

**Descripción**
Agrega `COUNT(*)` a la lista SELECT.

**Ejemplo**
```csharp
builder.SelectCount("total");
// → SELECT COUNT(*) AS [total] FROM ...
```

---

#### `SelectCount` — columna

**Firma**
```csharp
SqlQueryBuilder<T> SelectCount(Expression<Func<T, object>> column, string? alias = null)
```

**Descripción**
Agrega `COUNT([col])` a la lista SELECT.

---

#### `SelectCountDistinct`

**Firma**
```csharp
SqlQueryBuilder<T> SelectCountDistinct(Expression<Func<T, object>> column, string? alias = null)
```

**Descripción**
Agrega `COUNT(DISTINCT [col])` a la lista SELECT.

---

#### `SelectSum`

**Firma**
```csharp
SqlQueryBuilder<T> SelectSum(Expression<Func<T, object>> column, string? alias = null)
```

**Descripción**
Agrega `SUM([col])` a la lista SELECT.

---

#### `SelectAvg`

**Firma**
```csharp
SqlQueryBuilder<T> SelectAvg(Expression<Func<T, object>> column, string? alias = null)
```

**Descripción**
Agrega `AVG([col])` a la lista SELECT.

---

#### `SelectMin`

**Firma**
```csharp
SqlQueryBuilder<T> SelectMin(Expression<Func<T, object>> column, string? alias = null)
```

**Descripción**
Agrega `MIN([col])` a la lista SELECT.

---

#### `SelectMax`

**Firma**
```csharp
SqlQueryBuilder<T> SelectMax(Expression<Func<T, object>> column, string? alias = null)
```

**Descripción**
Agrega `MAX([col])` a la lista SELECT.

---

#### `SelectCase`

**Firma**
```csharp
SqlQueryBuilder<T> SelectCase(CaseWhenBuilder caseWhen)
```

**Descripción**
Agrega una expresión `CASE WHEN ... THEN ... ELSE ... END` a la lista SELECT, construida mediante `CaseWhenBuilder`.

**Ejemplo**
```csharp
var caseExpr = new CaseWhenBuilder()
    .When("[Status] = 1", "Activo")
    .When("[Status] = 2", "Inactivo")
    .Else("Desconocido")
    .As("EstadoLabel");

builder.SelectCase(caseExpr);
// → SELECT CASE WHEN [Status] = 1 THEN 'Activo' WHEN [Status] = 2 THEN 'Inactivo' ELSE 'Desconocido' END AS [EstadoLabel]
```

---

#### `SelectCoalesce`

**Firma**
```csharp
SqlQueryBuilder<T> SelectCoalesce(
    Expression<Func<T, object>> column, string fallbackSql, string alias)
```

**Descripción**
Agrega `COALESCE([col], fallbackSql) AS [alias]` a la lista SELECT.

**Ejemplo**
```csharp
builder.SelectCoalesce(x => x.Department, "'Sin Dept'", "Departamento");
// → SELECT COALESCE([Department], 'Sin Dept') AS [Departamento] FROM ...
```

---

#### `SelectCast`

**Firma**
```csharp
SqlQueryBuilder<T> SelectCast(
    Expression<Func<T, object>> column, string typeName, string alias)
```

**Descripción**
Agrega `CAST([col] AS typeName) AS [alias]` a la lista SELECT.

**Ejemplo**
```csharp
builder.SelectCast(x => x.Age, "FLOAT", "EdadDecimal");
// → SELECT CAST([Age] AS FLOAT) AS [EdadDecimal] FROM ...
```

---

#### `SelectConcat`

**Firma**
```csharp
SqlQueryBuilder<T> SelectConcat(string alias, params Expression<Func<T, object>>[] columns)
```

**Descripción**
Agrega una expresión de concatenación de cadenas adaptada al dialecto. El operador varía por dialecto:
- SQL Server: `[col1] + [col2]`
- PostgreSQL / SQLite: `"col1" || "col2"`
- MySQL: `` CONCAT(`col1`, `col2`) ``

**Ejemplo**
```csharp
builder.SelectConcat("NombreCompleto", x => x.FirstName, x => x.LastName);
// SQL Server → SELECT [FirstName] + [LastName] AS [NombreCompleto]
// PostgreSQL → SELECT "FirstName" || "LastName" AS "NombreCompleto"
```

---

### Funciones de Ventana

#### `SelectRowNumber`

**Firma**
```csharp
SqlQueryBuilder<T> SelectRowNumber(
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>> orderBy,
    bool ascending = true,
    string? alias = "RowNum")

// Sobrecarga multi-partición
SqlQueryBuilder<T> SelectRowNumber(
    IReadOnlyList<Expression<Func<T, object>>> partitionBy,
    Expression<Func<T, object>> orderBy,
    bool ascending = true,
    string? alias = "RowNum")
```

**Descripción**
Agrega `ROW_NUMBER() OVER (PARTITION BY [col] ORDER BY [col] ASC|DESC) AS [alias]`. Pasa `null` como `partitionBy` para omitir la cláusula PARTITION BY.

**Ejemplo**
```csharp
builder.SelectRowNumber(x => x.Department, x => x.Salary, ascending: false, alias: "RangoSalario");
// → SELECT ROW_NUMBER() OVER (PARTITION BY [Department] ORDER BY [Salary] DESC) AS [RangoSalario]
```

---

#### `SelectRank`

**Firma**
```csharp
SqlQueryBuilder<T> SelectRank(
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>> orderBy,
    bool ascending = true,
    string? alias = "Rank")
```

**Descripción**
Agrega `RANK() OVER (...)`. Mismo comportamiento de saltos que el RANK estándar de SQL.

---

#### `SelectDenseRank`

**Firma**
```csharp
SqlQueryBuilder<T> SelectDenseRank(
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>> orderBy,
    bool ascending = true,
    string? alias = "DenseRank")
```

**Descripción**
Agrega `DENSE_RANK() OVER (...)`. Sin saltos en los valores de ranking.

---

#### `SelectLag`

**Firma**
```csharp
SqlQueryBuilder<T> SelectLag(
    Expression<Func<T, object>> column,
    int offset,
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>> orderBy,
    bool ascending = true,
    string? alias = null)
```

**Descripción**
Agrega `LAG([col], offset) OVER ([PARTITION BY ...] ORDER BY ...)`. Recupera el valor de una fila anterior dentro de la partición.

**Ejemplo**
```csharp
builder.SelectLag(x => x.Price, 1, null, x => x.CreatedAt, alias: "PrecioAnterior");
// → SELECT LAG([Price], 1) OVER (ORDER BY [CreatedAt] ASC) AS [PrecioAnterior]
```

---

#### `SelectLead`

**Firma**
```csharp
SqlQueryBuilder<T> SelectLead(
    Expression<Func<T, object>> column,
    int offset,
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>> orderBy,
    bool ascending = true,
    string? alias = null)
```

**Descripción**
Agrega `LEAD([col], offset) OVER (...)`. Recupera el valor de una fila siguiente dentro de la partición.

---

#### `SelectFirstValue`

**Firma**
```csharp
SqlQueryBuilder<T> SelectFirstValue(
    Expression<Func<T, object>> column,
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>> orderBy,
    bool ascending = true,
    string? alias = null)
```

**Descripción**
Agrega `FIRST_VALUE([col]) OVER (...)`. Recupera el primer valor en el marco de ventana ordenado.

---

#### `SelectLastValue`

**Firma**
```csharp
SqlQueryBuilder<T> SelectLastValue(
    Expression<Func<T, object>> column,
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>> orderBy,
    bool ascending = true,
    string? alias = null)
```

**Descripción**
Agrega `LAST_VALUE([col]) OVER (...)`. Recupera el último valor en el marco de ventana ordenado.

---

#### `SelectSumOver`

**Firma**
```csharp
SqlQueryBuilder<T> SelectSumOver(
    Expression<Func<T, object>> column,
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>>? orderBy,
    bool ascending = true,
    string? alias = null)
```

**Descripción**
Agrega `SUM([col]) OVER ([PARTITION BY ...] [ORDER BY ...])`. Útil para totales acumulados. El alias por defecto es `Running{NombreColumna}`.

**Ejemplo**
```csharp
builder.SelectSumOver(x => x.Amount, x => x.CustomerId, x => x.CreatedAt, alias: "TotalAcumulado");
// → SELECT SUM([Amount]) OVER (PARTITION BY [CustomerId] ORDER BY [CreatedAt] ASC) AS [TotalAcumulado]
```

---

#### `SelectAvgOver`

**Firma**
```csharp
SqlQueryBuilder<T> SelectAvgOver(
    Expression<Func<T, object>> column,
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>>? orderBy,
    bool ascending = true,
    string? alias = null)
```

**Descripción**
Agrega `AVG([col]) OVER (...)`.

---

#### `SelectCountOver`

**Firma**
```csharp
SqlQueryBuilder<T> SelectCountOver(
    Expression<Func<T, object>> column,
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>>? orderBy,
    bool ascending = true,
    string? alias = null)
```

**Descripción**
Agrega `COUNT([col]) OVER (...)`.

---

#### `SelectMinOver`

**Firma**
```csharp
SqlQueryBuilder<T> SelectMinOver(
    Expression<Func<T, object>> column,
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>>? orderBy,
    bool ascending = true,
    string? alias = null)
```

**Descripción**
Agrega `MIN([col]) OVER (...)`.

---

#### `SelectMaxOver`

**Firma**
```csharp
SqlQueryBuilder<T> SelectMaxOver(
    Expression<Func<T, object>> column,
    Expression<Func<T, object>>? partitionBy,
    Expression<Func<T, object>>? orderBy,
    bool ascending = true,
    string? alias = null)
```

**Descripción**
Agrega `MAX([col]) OVER (...)`.

---

#### `SelectWindowRaw`

**Firma**
```csharp
SqlQueryBuilder<T> SelectWindowRaw(string windowExpression, string alias)
```

**Descripción**
Agrega una expresión de función de ventana literal al SELECT. Útil cuando las sobrecargas tipadas no cubren tu caso.

**Ejemplo**
```csharp
builder.SelectWindowRaw("NTILE(4) OVER (ORDER BY [Score] DESC)", "Cuartil");
// → SELECT NTILE(4) OVER (ORDER BY [Score] DESC) AS [Cuartil]
```

---

### FROM

#### `From` — nombre de tabla

**Firma**
```csharp
SqlQueryBuilder<T> From(string tableName, string? schema = null)
```

**Descripción**
Establece la tabla FROM. Si no se llama, toma `typeof(T).Name` como valor por defecto. El esquema es opcional.

**Ejemplo**
```csharp
builder.From("Users", schema: "dbo");
// SQL Server → FROM [dbo].[Users]
// PostgreSQL → FROM "dbo"."Users"
```

---

#### `From` — subconsulta

**Firma**
```csharp
SqlQueryBuilder<T> From(SqlQueryResult subquery, string alias)
```

**Descripción**
Usa un `SqlQueryResult` precompilado como fuente FROM: `FROM (subquery) alias`.

**Ejemplo**
```csharp
var inner = new SqlQueryBuilder<User>(dialect)
    .From("Users")
    .Where(x => x.IsActive)
    .Build();

new SqlQueryBuilder<User>(dialect)
    .From(inner, "activos")
    .Select(x => x.Name)
    .Build();
// → SELECT "Name" FROM (SELECT * FROM "Users" WHERE "IsActive" = true) activos
```

---

### JOINs

Todos los métodos de join aceptan un nombre de tabla (entre comillas según el dialecto), un alias opcional y una condición ON como string literal.

#### `InnerJoin`

**Firma**
```csharp
SqlQueryBuilder<T> InnerJoin(string table, string? alias, string on)
```

**Ejemplo**
```csharp
builder.From("Users").InnerJoin("Orders", "o", "[Users].[Id] = [o].[UserId]");
// → FROM [Users] INNER JOIN [Orders] o ON [Users].[Id] = [o].[UserId]
```

---

#### `LeftJoin`

**Firma**
```csharp
SqlQueryBuilder<T> LeftJoin(string table, string? alias, string on)
```

**Descripción**
Agrega una cláusula `LEFT JOIN`.

---

#### `RightJoin`

**Firma**
```csharp
SqlQueryBuilder<T> RightJoin(string table, string? alias, string on)
```

**Descripción**
Agrega una cláusula `RIGHT JOIN`.

---

#### `FullOuterJoin`

**Firma**
```csharp
SqlQueryBuilder<T> FullOuterJoin(string table, string? alias, string on)
```

**Descripción**
Agrega una cláusula `FULL OUTER JOIN`.

---

#### `CrossJoin`

**Firma**
```csharp
SqlQueryBuilder<T> CrossJoin(string table, string? alias = null)
```

**Descripción**
Agrega una cláusula `CROSS JOIN`. No requiere condición ON.

---

#### `InnerJoinSubquery`

**Firma**
```csharp
SqlQueryBuilder<T> InnerJoinSubquery(SqlQueryResult subquery, string alias, string on)
```

**Descripción**
Agrega un `INNER JOIN` contra una tabla derivada (subconsulta). Los parámetros de la subconsulta se renombran automáticamente para evitar colisiones.

**Ejemplo**
```csharp
var sub = new SqlQueryBuilder<Order>(dialect)
    .From("Orders")
    .Select(x => x.UserId)
    .SelectSum(x => x.Total, "TotalGastado")
    .GroupBy(x => x.UserId)
    .Build();

builder.From("Users").InnerJoinSubquery(sub, "totales", "[Users].[Id] = [totales].[UserId]");
// → FROM [Users] INNER JOIN (SELECT [UserId], SUM([Total]) AS [TotalGastado] FROM [Orders] GROUP BY [UserId]) [totales] ON [Users].[Id] = [totales].[UserId]
```

---

#### `LeftJoinSubquery`

**Firma**
```csharp
SqlQueryBuilder<T> LeftJoinSubquery(SqlQueryResult subquery, string alias, string on)
```

**Descripción**
Agrega un `LEFT JOIN` contra una tabla derivada (subconsulta).

---

#### `RightJoinSubquery`

**Firma**
```csharp
SqlQueryBuilder<T> RightJoinSubquery(SqlQueryResult subquery, string alias, string on)
```

**Descripción**
Agrega un `RIGHT JOIN` contra una tabla derivada (subconsulta).

---

#### `FullOuterJoinSubquery`

**Firma**
```csharp
SqlQueryBuilder<T> FullOuterJoinSubquery(SqlQueryResult subquery, string alias, string on)
```

**Descripción**
Agrega un `FULL OUTER JOIN` contra una tabla derivada (subconsulta).

---

### WHERE

Múltiples llamadas WHERE se combinan con AND. La sobrecarga de `ValiFlow<T>` / `Expression` y la de `SqlWhereBuilder` usan prefijos de nombre de parámetro diferentes (`p` vs `pw`) y pueden coexistir sin conflictos.

#### `Where` — ValiFlow

**Firma**
```csharp
SqlQueryBuilder<T> Where(ValiFlow<T> filter)
```

**Descripción**
Establece la cláusula WHERE a partir de un filtro `ValiFlow<T>`. Los parámetros usan el prefijo `p`.

**Ejemplo**
```csharp
var filter = new ValiFlow<User>().EqualTo(x => x.IsActive, true).GreaterThan(x => x.Age, 18);
builder.From("Users").Where(filter);
// → WHERE [IsActive] = @p0 AND [Age] > @p1
```

---

#### `Where` — Expression

**Firma**
```csharp
SqlQueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
```

**Descripción**
Establece la cláusula WHERE a partir de una expresión lambda. Los parámetros usan el prefijo `p`.

> **Nota:** Llamar tanto a `Where(ValiFlow<T>)` como a `Where(Expression<...>)` no está soportado — la segunda llamada sobreescribe la primera. Usa solo un predicado tipado; combínalo con `Where(SqlWhereBuilder)` para condiciones compuestas.

---

#### `Where` — SqlWhereBuilder (instancia)

**Firma**
```csharp
SqlQueryBuilder<T> Where(SqlWhereBuilder<T> whereBuilder)
```

**Descripción**
Establece la cláusula WHERE a partir de un `SqlWhereBuilder<T>` preconfigurado. Los parámetros usan el prefijo `pw`. Puede combinarse con la sobrecarga de predicado tipado.

---

#### `Where` — SqlWhereBuilder (inline)

**Firma**
```csharp
SqlQueryBuilder<T> Where(Action<SqlWhereBuilder<T>> configure)
```

**Descripción**
Configura la cláusula WHERE de forma inline usando una acción `SqlWhereBuilder<T>`.

**Ejemplo**
```csharp
builder.Where(w => w.EqualTo(x => x.IsActive, true).GreaterThan(x => x.Age, 18));
// → WHERE [IsActive] = @pw0 AND [Age] > @pw1
```

---

#### `WhereRaw`

**Firma**
```csharp
SqlQueryBuilder<T> WhereRaw(string rawSql)
```

**Descripción**
Agrega un fragmento SQL literal a la cláusula WHERE (combinado con AND). Los parámetros en el fragmento son responsabilidad del caller.

**Ejemplo**
```csharp
builder.WhereRaw("[CreatedAt] > DATEADD(day, -30, GETDATE())");
// → WHERE [CreatedAt] > DATEADD(day, -30, GETDATE())
```

---

#### `WhereExists`

**Firma**
```csharp
SqlQueryBuilder<T> WhereExists(SqlQueryResult subquery)
```

**Descripción**
Agrega `EXISTS (subquery)` a la cláusula WHERE. Los parámetros de la subconsulta se fusionan y renombran automáticamente.

**Ejemplo**
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

**Firma**
```csharp
SqlQueryBuilder<T> WhereNotExists(SqlQueryResult subquery)
```

**Descripción**
Agrega `NOT EXISTS (subquery)` a la cláusula WHERE.

---

#### `WhereInSubquery`

**Firma**
```csharp
SqlQueryBuilder<T> WhereInSubquery<TValue>(Expression<Func<T, TValue>> column, SqlQueryResult subquery)
```

**Descripción**
Agrega `[col] IN (subquery)` a la cláusula WHERE. Los parámetros de la subconsulta se renombran automáticamente.

**Ejemplo**
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

**Firma**
```csharp
SqlQueryBuilder<T> WhereNotInSubquery<TValue>(Expression<Func<T, TValue>> column, SqlQueryResult subquery)
```

**Descripción**
Agrega `[col] NOT IN (subquery)` a la cláusula WHERE.

---

#### `WhereIf` — sobrecarga builder

**Firma**
```csharp
SqlQueryBuilder<T> WhereIf(bool condition, Action<SqlWhereBuilder<T>> configure)
```

**Descripción**
Aplica una cláusula WHERE con `SqlWhereBuilder` solo cuando `condition` es `true`.

---

#### `WhereIf` — sobrecarga expression

**Firma**
```csharp
SqlQueryBuilder<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate)
```

**Descripción**
Aplica un predicado WHERE lambda solo cuando `condition` es `true`.

---

### GROUP BY / HAVING

#### `GroupBy`

**Firma**
```csharp
SqlQueryBuilder<T> GroupBy(params Expression<Func<T, object>>[] columns)
```

**Descripción**
Agrega columnas GROUP BY.

**Ejemplo**
```csharp
builder.GroupBy(x => x.Department, x => x.Status);
// → GROUP BY [Department], [Status]
```

---

#### `GroupByRaw`

**Firma**
```csharp
SqlQueryBuilder<T> GroupByRaw(string rawSql)
```

**Descripción**
Agrega un fragmento SQL literal a la cláusula GROUP BY. Útil para consultas multi-tabla donde se necesitan nombres de columna calificados con la tabla.

**Ejemplo**
```csharp
builder.GroupByRaw("[Products].[Id], [Products].[Name]");
```

---

#### `GroupByIf`

**Firma**
```csharp
SqlQueryBuilder<T> GroupByIf(bool condition, Expression<Func<T, object>> column)
```

**Descripción**
Agrega una columna GROUP BY solo cuando `condition` es `true`.

---

#### `Having` — string literal

**Firma**
```csharp
SqlQueryBuilder<T> Having(string rawHavingSql)
```

**Descripción**
Establece una cláusula HAVING literal. Se usa después de `GroupBy`.

**Ejemplo**
```csharp
builder.GroupBy(x => x.Department).Having("COUNT(*) > 5");
// → GROUP BY [Department] HAVING COUNT(*) > 5
```

---

#### `Having` — SqlHavingBuilder (instancia)

**Firma**
```csharp
SqlQueryBuilder<T> Having(SqlHavingBuilder<T> havingBuilder)
```

**Descripción**
Establece la cláusula HAVING a partir de un `SqlHavingBuilder<T>`. Puede combinarse con la sobrecarga de string literal — ambas se unen con AND.

---

#### `Having` — SqlHavingBuilder (inline)

**Firma**
```csharp
SqlQueryBuilder<T> Having(Action<SqlHavingBuilder<T>> configure)
```

**Descripción**
Configura la cláusula HAVING de forma inline.

**Ejemplo**
```csharp
builder.Having(h => h.CountGreaterThan(5).SumGreaterThan(x => x.Amount, 1000m));
```

---

#### `HavingIf` — sobrecarga builder

**Firma**
```csharp
SqlQueryBuilder<T> HavingIf(bool condition, Action<SqlHavingBuilder<T>> configure)
```

**Descripción**
Aplica una cláusula HAVING fluida solo cuando `condition` es `true`.

---

#### `HavingIf` — sobrecarga string literal

**Firma**
```csharp
SqlQueryBuilder<T> HavingIf(bool condition, string rawHavingSql)
```

**Descripción**
Aplica una cláusula HAVING literal solo cuando `condition` es `true`.

---

### ORDER BY

#### `OrderBy`

**Firma**
```csharp
SqlQueryBuilder<T> OrderBy(Expression<Func<T, object>> column, bool ascending = true)
```

**Descripción**
Agrega una columna ORDER BY principal.

**Ejemplo**
```csharp
builder.OrderBy(x => x.CreatedAt, ascending: false);
// → ORDER BY [CreatedAt] DESC
```

---

#### `OrderBy` — con NullsOrder

**Firma**
```csharp
SqlQueryBuilder<T> OrderBy(Expression<Func<T, object>> column, bool ascending, NullsOrder nulls)
```

**Descripción**
Agrega ORDER BY con `NULLS FIRST` / `NULLS LAST` opcional. El parámetro `nulls` se ignora silenciosamente en dialectos que no lo soportan (SQL Server, MySQL).

**Ejemplo**
```csharp
builder.OrderBy(x => x.DeletedAt, ascending: true, NullsOrder.Last);
// PostgreSQL → ORDER BY "DeletedAt" ASC NULLS LAST
// SQL Server → ORDER BY [DeletedAt] ASC  (cláusula nulls omitida)
```

---

#### `ThenBy`

**Firma**
```csharp
SqlQueryBuilder<T> ThenBy(Expression<Func<T, object>> column, bool ascending = true)
```

**Descripción**
Agrega una columna ORDER BY secundaria. Funcionalmente equivalente a una segunda llamada a `OrderBy`.

---

#### `OrderByIf`

**Firma**
```csharp
SqlQueryBuilder<T> OrderByIf(bool condition, Expression<Func<T, object>> column, bool ascending = true)
```

**Descripción**
Aplica ORDER BY solo cuando `condition` es `true`.

---

#### `OrderByRaw`

**Firma**
```csharp
SqlQueryBuilder<T> OrderByRaw(string rawSql)
```

**Descripción**
Agrega una expresión ORDER BY literal textualmente. Útil para expresiones de ordenamiento multi-tabla o calculadas.

**Ejemplo**
```csharp
builder.OrderByRaw("[Products].[Price] DESC, [Name] ASC");
```

---

### Paginación

#### `Take`

**Firma**
```csharp
SqlQueryBuilder<T> Take(int count)
```

**Descripción**
Limita el número de filas devueltas. Se mapea a `TOP N` (SQL Server sin OFFSET) o `LIMIT N`.

**Ejemplo**
```csharp
builder.From("Users").Take(10);
// SQL Server → SELECT TOP 10 * FROM [Users]
// PostgreSQL → SELECT * FROM "Users" LIMIT 10
```

---

#### `Skip`

**Firma**
```csharp
SqlQueryBuilder<T> Skip(int count)
```

**Descripción**
Omite el número de filas especificado. Se mapea a OFFSET en todos los dialectos.

**Ejemplo**
```csharp
builder.From("Users").OrderBy(x => x.Id).Skip(20).Take(10);
// SQL Server → SELECT * FROM [Users] ORDER BY [Id] ASC OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY
// PostgreSQL → SELECT * FROM "Users" ORDER BY "Id" ASC LIMIT 10 OFFSET 20
```

> **Nota:** SQL Server requiere una cláusula ORDER BY cuando se usa OFFSET.

---

#### `Page`

**Firma**
```csharp
SqlQueryBuilder<T> Page(int pageNumber, int pageSize)
```

**Descripción**
Establece paginación basada en número de página (base 1). `Page(2, 20)` equivale a `Skip(20).Take(20)`.

**Ejemplo**
```csharp
builder.From("Users").OrderBy(x => x.Id).Page(3, 25);
// SQL Server → OFFSET 50 ROWS FETCH NEXT 25 ROWS ONLY
```

---

### DISTINCT / WithHint

#### `Distinct`

**Firma**
```csharp
SqlQueryBuilder<T> Distinct()
```

**Descripción**
Agrega `DISTINCT` a la cláusula SELECT.

**Ejemplo**
```csharp
builder.From("Orders").Select(x => x.CustomerId).Distinct();
// → SELECT DISTINCT [CustomerId] FROM [Orders]
```

---

#### `WithHint`

**Firma**
```csharp
SqlQueryBuilder<T> WithHint(string hint)
```

**Descripción**
Agrega un table hint después del FROM. Principalmente usado con SQL Server para hints como `NOLOCK`.

**Ejemplo**
```csharp
builder.From("Users").WithHint("NOLOCK");
// SQL Server → FROM [Users] WITH (NOLOCK)
```

> **Nota:** Este método genera el hint para todos los dialectos. Úsalo únicamente con SQL Server — otros dialectos incluirán un fragmento `WITH (...)` inválido.

---

### Operaciones de Conjunto

#### `Union`

**Firma**
```csharp
SqlQueryBuilder<T> Union(SqlQueryResult other)
```

**Descripción**
Agrega un `UNION` (eliminando duplicados) con una consulta precompilada.

**Ejemplo**
```csharp
builder.From("ActiveUsers").Union(
    new SqlQueryBuilder<User>(dialect).From("InactiveUsers").Build());
// → SELECT * FROM [ActiveUsers] UNION SELECT * FROM [InactiveUsers]
```

---

#### `UnionAll`

**Firma**
```csharp
SqlQueryBuilder<T> UnionAll(SqlQueryResult other)
```

**Descripción**
Agrega un `UNION ALL` (incluyendo duplicados) con una consulta precompilada.

---

#### `Except`

**Firma**
```csharp
SqlQueryBuilder<T> Except(SqlQueryResult other)
```

**Descripción**
Agrega `EXCEPT` — retorna filas de la consulta principal que no están en la otra consulta.

---

#### `Intersect`

**Firma**
```csharp
SqlQueryBuilder<T> Intersect(SqlQueryResult other)
```

**Descripción**
Agrega `INTERSECT` — retorna solo las filas que aparecen en ambas consultas.

---

### CTEs

#### `WithCte` — SqlQueryResult

**Firma**
```csharp
SqlQueryBuilder<T> WithCte(string name, SqlQueryResult cteQuery)
```

**Descripción**
Antepone una Expresión de Tabla Común: `WITH nombre AS (cteQuery)`. Múltiples llamadas agregan múltiples CTEs.

**Ejemplo**
```csharp
var activosCte = new SqlQueryBuilder<User>(dialect)
    .From("Users")
    .Where(w => w.EqualTo(x => x.IsActive, true))
    .Build();

new SqlQueryBuilder<User>(dialect)
    .WithCte("UsuariosActivos", activosCte)
    .From("UsuariosActivos")
    .Build();
// → WITH "UsuariosActivos" AS (SELECT * FROM "Users" WHERE ...) SELECT * FROM "UsuariosActivos"
```

---

#### `WithCte` — inline

**Firma**
```csharp
SqlQueryBuilder<T> WithCte(string name, Action<SqlQueryBuilder<T>> configure)
```

**Descripción**
Antepone un CTE definido de forma inline. Crea un `SqlQueryBuilder<T>` anidado internamente.

---

#### `WithRecursive`

**Firma**
```csharp
SqlQueryBuilder<T> WithRecursive(string name, SqlQueryResult anchor, SqlQueryResult recursive)
```

**Descripción**
Agrega un CTE recursivo. Genera: `WITH [RECURSIVE] nombre AS (anchor UNION ALL recursive)`. La palabra clave `RECURSIVE` se agrega automáticamente para dialectos que la requieren (PostgreSQL, SQLite, MySQL).

**Ejemplo**
```csharp
var anchor = new SqlQueryBuilder<Category>(dialect).From("Categories").Where(x => x.ParentId == null).Build();
var rec    = new SqlQueryBuilder<Category>(dialect).From("Categories").WhereRaw("[Categories].[ParentId] = [arbol].[Id]").Build();

builder.WithRecursive("arbol", anchor, rec).From("arbol").Build();
// PostgreSQL → WITH RECURSIVE "arbol" AS (... UNION ALL ...) SELECT * FROM "arbol"
// SQL Server → WITH [arbol] AS (... UNION ALL ...) SELECT * FROM [arbol]
```

---

### Bloqueo de Filas

#### `ForUpdate`

**Firma**
```csharp
SqlQueryBuilder<T> ForUpdate()
```

**Descripción**
Agrega `FOR UPDATE` al final de la consulta para bloqueo exclusivo de filas. No hace nada en dialectos que no lo soportan (SQL Server — usa `WithHint("UPDLOCK")`; SQLite — ignorado silenciosamente).

---

#### `ForShare`

**Firma**
```csharp
SqlQueryBuilder<T> ForShare()
```

**Descripción**
Agrega `FOR SHARE` al final de la consulta para bloqueo compartido de filas. No hace nada en dialectos que no lo soportan.

---

### Tag / Build / ToPreviewSql

#### `Tag` — simple

**Firma**
```csharp
SqlQueryBuilder<T> Tag(string description)
```

**Descripción**
Etiqueta la consulta. Cuando se llama `Build()`, la descripción se antepone como un comentario SQL (`-- descripción`). Útil para rastreo en logs.

**Ejemplo**
```csharp
builder.From("Users").Tag("Obtener usuarios activos").Build();
// SQL: -- Obtener usuarios activos
//      SELECT * FROM [Users]
```

---

#### `Tag` — con logger

**Firma**
```csharp
SqlQueryBuilder<T> Tag(string description, Action<string>? logger)
```

**Descripción**
Igual que el anterior, pero también invoca `logger` con `"[SQL] {descripción}"` cuando se llama `Build()`. Si `logger` es `null`, el tag solo se incrusta como comentario SQL.

**Ejemplo**
```csharp
builder.Tag("Obtener usuarios activos", msg => Console.WriteLine(msg));
// Salida en consola: [SQL] Obtener usuarios activos
```

---

#### `ToPreviewSql`

**Firma**
```csharp
string ToPreviewSql()
```

**Descripción**
Devuelve una vista previa del SQL en medio de la cadena fluida sin finalizar el constructor. Útil en la ventana de observación del depurador. Devuelve un mensaje alternativo si el estado del constructor está incompleto.

---

#### `Build`

**Firma**
```csharp
SqlQueryResult Build()
```

**Descripción**
Ensambla y devuelve el `SqlQueryResult` final. Esta llamada es terminal — el constructor puede reutilizarse pero el resultado es inmutable.

---

## SqlInsertBuilder

`SqlInsertBuilder<T>` construye sentencias `INSERT` parametrizadas. Soporta inserción de fila única, multi-fila, INSERT … SELECT, y patrones de resolución de conflictos.

```csharp
var result = new SqlInsertBuilder<User>(new SqlServerDialect())
    .Into("Users")
    .Set(x => x.Name, "Alicia")
    .Set(x => x.Age, 30)
    .Build();
// → INSERT INTO [Users] ([Name], [Age]) VALUES (@pi0, @pi1)
```

---

### `Into`

**Firma**
```csharp
SqlInsertBuilder<T> Into(string tableName, string? schema = null)
```

**Descripción**
Establece el nombre de la tabla destino y el esquema opcional.

---

### `Set`

**Firma**
```csharp
SqlInsertBuilder<T> Set<TValue>(Expression<Func<T, TValue>> column, TValue value)
```

**Descripción**
Mapea una columna tipada a su valor de inserción para la fila actual.

**Ejemplo**
```csharp
builder.Into("Users").Set(x => x.Name, "Alicia").Set(x => x.Age, 30);
// → INSERT INTO [Users] ([Name], [Age]) VALUES (@pi0, @pi1)
```

---

### `NextRow`

**Firma**
```csharp
SqlInsertBuilder<T> NextRow()
```

**Descripción**
Inicia una nueva fila para una inserción multi-fila. Las llamadas `Set` posteriores llenan la nueva fila. Todas las filas deben tener el mismo número de columnas.

**Ejemplo**
```csharp
builder.Into("Users")
    .Set(x => x.Name, "Alicia").Set(x => x.Age, 30)
    .NextRow()
    .Set(x => x.Name, "Roberto").Set(x => x.Age, 25)
    .Build();
// → INSERT INTO [Users] ([Name], [Age]) VALUES (@pi0, @pi1), (@pi2, @pi3)
```

---

### `OutputInserted`

**Firma**
```csharp
SqlInsertBuilder<T> OutputInserted()
```

**Descripción**
Agrega `OUTPUT INSERTED.*` antes de VALUES. Solo para SQL Server — útil para recuperar columnas de identidad o calculadas después de la inserción.

**Ejemplo**
```csharp
builder.Into("Users").Set(x => x.Name, "Alicia").OutputInserted().Build();
// → INSERT INTO [Users] ([Name]) OUTPUT INSERTED.* VALUES (@pi0)
```

> **Nota:** Lanza `InvalidOperationException` si se usa con un dialecto que no sea SQL Server. Usa `Returning()` para PostgreSQL / SQLite.

---

### `Returning`

**Firma**
```csharp
SqlInsertBuilder<T> Returning(params Expression<Func<T, object>>[] columns)
```

**Descripción**
Agrega `RETURNING *` o `RETURNING col1, col2` después de VALUES. Compatible con PostgreSQL y SQLite 3.35+.

**Ejemplo**
```csharp
builder.Into("Users").Set(x => x.Name, "Alicia")
    .Returning(x => x.Id, x => x.CreatedAt)
    .Build();
// → INSERT INTO "Users" ("Name") VALUES (@pi0) RETURNING "Id", "CreatedAt"
```

---

### `SelectFrom`

**Firma**
```csharp
SqlInsertBuilder<T> SelectFrom(SqlQueryResult selectQuery)
```

**Descripción**
Especifica una consulta SELECT cuyos resultados se insertarán en la tabla. Mutuamente exclusivo con `Set`.

**Ejemplo**
```csharp
var select = new SqlQueryBuilder<User>(dialect)
    .From("Archivo")
    .Select(x => x.Name)
    .Build();

new SqlInsertBuilder<User>(dialect)
    .Into("Users")
    .Columns(x => x.Name)
    .SelectFrom(select)
    .Build();
// → INSERT INTO [Users] ([Name]) SELECT [Name] FROM [Archivo]
```

---

### `Columns`

**Firma**
```csharp
SqlInsertBuilder<T> Columns(params Expression<Func<T, object>>[] columns)
```

**Descripción**
Especifica las columnas destino para INSERT … SELECT. Si no se llama, la lista de columnas se omite: `INSERT INTO tabla SELECT …`.

---

### `Tag` (Insert)

**Firma**
```csharp
SqlInsertBuilder<T> Tag(string description)
```

**Descripción**
Etiqueta la consulta con un comentario antepuesto al SQL.

---

### Resolución de Conflictos

#### `OrIgnore` (SQLite)

**Firma**
```csharp
SqlInsertBuilder<T> OrIgnore()
```

**Descripción**
Emite `INSERT OR IGNORE INTO ...`. Solo para SQLite.

---

#### `OrReplace` (SQLite)

**Firma**
```csharp
SqlInsertBuilder<T> OrReplace()
```

**Descripción**
Emite `INSERT OR REPLACE INTO ...`. Solo para SQLite.

---

#### `OnConflictDoNothing` (PostgreSQL / SQLite)

**Firma**
```csharp
SqlInsertBuilder<T> OnConflictDoNothing()
```

**Descripción**
Agrega `ON CONFLICT DO NOTHING` después de VALUES.

**Ejemplo**
```csharp
builder.Into("Users").Set(x => x.Email, "a@b.com").OnConflictDoNothing().Build();
// → INSERT INTO "Users" ("Email") VALUES (@pi0) ON CONFLICT DO NOTHING
```

---

#### `OnConflictDoUpdate` (PostgreSQL / SQLite)

**Firma**
```csharp
SqlInsertBuilder<T> OnConflictDoUpdate(
    Action<SqlInsertBuilder<T>> conflictKeys,
    Action<SqlInsertBuilder<T>> updateAssignments)
```

**Descripción**
Agrega `ON CONFLICT (cols) DO UPDATE SET ...` después de VALUES. Pasa acciones que llaman a `AddConflictKey` y `AddConflictUpdate`.

**Ejemplo**
```csharp
builder.Into("Users")
    .Set(x => x.Email, "a@b.com")
    .Set(x => x.Name, "Alicia")
    .OnConflictDoUpdate(
        claves    => claves.AddConflictKey(x => x.Email),
        updates   => updates.AddConflictUpdate(x => x.Name, "Alicia"))
    .Build();
// → INSERT INTO "Users" ("Email", "Name") VALUES (@pi0, @pi1)
//   ON CONFLICT ("Email") DO UPDATE SET "Name" = @pu0
```

---

#### `AddConflictKey`

**Firma**
```csharp
SqlInsertBuilder<T> AddConflictKey<TValue>(Expression<Func<T, TValue>> column)
```

**Descripción**
Registra una columna como parte de la lista de columnas objetivo del ON CONFLICT. Se llama dentro de la acción `conflictKeys` de `OnConflictDoUpdate`.

---

#### `AddConflictUpdate`

**Firma**
```csharp
SqlInsertBuilder<T> AddConflictUpdate<TValue>(Expression<Func<T, TValue>> column, TValue value)
```

**Descripción**
Registra una asignación columna = valor para la cláusula DO UPDATE SET. Se llama dentro de la acción `updateAssignments` de `OnConflictDoUpdate`.

---

#### `InsertIgnore` (MySQL)

**Firma**
```csharp
SqlInsertBuilder<T> InsertIgnore()
```

**Descripción**
Emite `INSERT IGNORE INTO ...`. Solo para MySQL.

---

#### `OnDuplicateKeyUpdate` (MySQL)

**Firma**
```csharp
SqlInsertBuilder<T> OnDuplicateKeyUpdate(Action<SqlInsertBuilder<T>> configure)
```

**Descripción**
Agrega `ON DUPLICATE KEY UPDATE col = @pu...` después de VALUES. Solo para MySQL.

**Ejemplo**
```csharp
builder.Into("Users")
    .Set(x => x.Email, "a@b.com")
    .OnDuplicateKeyUpdate(u => u.AddDuplicateKeyAssignment(x => x.Name, "Alicia"))
    .Build();
// → INSERT INTO `Users` (`Email`) VALUES (@pi0) ON DUPLICATE KEY UPDATE `Name` = @pu0
```

---

#### `AddDuplicateKeyAssignment`

**Firma**
```csharp
SqlInsertBuilder<T> AddDuplicateKeyAssignment<TValue>(Expression<Func<T, TValue>> column, TValue value)
```

**Descripción**
Registra una asignación columna = valor para la cláusula ON DUPLICATE KEY UPDATE.

---

### `Build` (Insert)

**Firma**
```csharp
SqlQueryResult Build()
```

**Descripción**
Ensambla y devuelve el `SqlQueryResult` final.

---

## SqlUpdateBuilder

`SqlUpdateBuilder<T>` construye sentencias `UPDATE` parametrizadas.

```csharp
var result = new SqlUpdateBuilder<User>(new SqlServerDialect())
    .Table("Users")
    .Set(x => x.Name, "Roberto")
    .Set(x => x.IsActive, false)
    .Where(w => w.EqualTo(x => x.Id, 42))
    .Build();
// → UPDATE [Users] SET [Name] = @pu0, [IsActive] = @pu1 WHERE [Id] = @pw0
```

> **Seguridad:** Por defecto, `Build()` lanza una excepción si no se establece ninguna cláusula WHERE. Llama a `AllowUpdateAll()` para permitir explícitamente la actualización de todas las filas.

---

### `Table`

**Firma**
```csharp
SqlUpdateBuilder<T> Table(string tableName, string? schema = null)
```

**Descripción**
Establece el nombre de la tabla destino y el esquema opcional.

---

### `Set` (Update)

**Firma**
```csharp
SqlUpdateBuilder<T> Set<TValue>(Expression<Func<T, TValue>> column, TValue value)
```

**Descripción**
Agrega una cláusula SET parametrizada. Los parámetros usan el prefijo `pu`.

**Ejemplo**
```csharp
builder.Table("Users").Set(x => x.Name, "Roberto").Set(x => x.IsActive, false);
// SET [Name] = @pu0, [IsActive] = @pu1
```

---

### `SetRaw`

**Firma**
```csharp
SqlUpdateBuilder<T> SetRaw(Expression<Func<T, object>> column, string rawExpression)
```

**Descripción**
Agrega una cláusula SET literal (sin parametrizar). Útil para expresiones del lado del servidor como `GETDATE()` o aritmética sobre valores existentes.

**Ejemplo**
```csharp
builder.SetRaw(x => x.Counter, "Counter + 1");
// SET [Counter] = Counter + 1

builder.SetRaw(x => x.UpdatedAt, "GETDATE()");
// SET [UpdatedAt] = GETDATE()
```

---

### `SetColumn`

**Firma**
```csharp
SqlUpdateBuilder<T> SetColumn<TValue>(
    Expression<Func<T, TValue>> target,
    Expression<Func<T, TValue>> source)
```

**Descripción**
Copia el valor actual de una columna a otra sin parámetros.

**Ejemplo**
```csharp
builder.SetColumn(x => x.NameBackup, x => x.Name);
// SET [NameBackup] = [Name]
```

---

### `Where` (Update)

Cuatro sobrecargas disponibles — misma semántica que `SqlQueryBuilder`:

```csharp
SqlUpdateBuilder<T> Where(SqlWhereBuilder<T> whereBuilder)
SqlUpdateBuilder<T> Where(Action<SqlWhereBuilder<T>> configure)
SqlUpdateBuilder<T> Where(Expression<Func<T, bool>> predicate)
SqlUpdateBuilder<T> Where(ValiFlow<T> filter)
```

---

### `FromTable`

**Firma**
```csharp
SqlUpdateBuilder<T> FromTable(string sourceTable, string? alias = null)
```

**Descripción**
Agrega una tabla fuente FROM (SQL Server / PostgreSQL) o JOIN (MySQL) a la sentencia UPDATE, habilitando actualizaciones multi-tabla. Lanza excepción en SQLite y Oracle.

**Ejemplo**
```csharp
// SQL Server
builder.Table("Orders")
    .Set(x => x.Status, "Enviado")
    .FromTable("Shipments", "s")
    .JoinOn("INNER JOIN", "[Shipments].[OrderId] = [Orders].[Id]")
    .Where(x => x.Status == "Pendiente")
    .Build();
// → UPDATE [Orders] SET [Status] = @pu0 FROM [Shipments] [s] INNER JOIN [Shipments].[OrderId] = [Orders].[Id]
//   WHERE [Status] = @p0
```

---

### `JoinOn`

**Firma**
```csharp
SqlUpdateBuilder<T> JoinOn(string joinType, string onCondition)
```

**Descripción**
Agrega una condición JOIN a la cláusula FROM de la sentencia UPDATE. Se usa junto con `FromTable`.

---

### `AllowUpdateAll`

**Firma**
```csharp
SqlUpdateBuilder<T> AllowUpdateAll()
```

**Descripción**
Permite explícitamente generar un UPDATE sin cláusula WHERE (afecta todas las filas). Llama solo cuando la actualización de toda la tabla es intencional.

---

### `OutputUpdated`

**Firma**
```csharp
SqlUpdateBuilder<T> OutputUpdated()
```

**Descripción**
Agrega `OUTPUT INSERTED.*` después de SET. Solo para SQL Server. Devuelve los nuevos valores de las filas actualizadas.

---

### `Returning` (Update)

**Firma**
```csharp
SqlUpdateBuilder<T> Returning(params Expression<Func<T, object>>[] columns)
```

**Descripción**
Agrega `RETURNING *` o columnas especificadas después de WHERE. Solo para PostgreSQL / SQLite.

---

### `Tag` (Update)

**Firma**
```csharp
SqlUpdateBuilder<T> Tag(string description)
```

**Descripción**
Etiqueta la consulta con un comentario SQL.

---

### `Build` (Update)

**Firma**
```csharp
SqlQueryResult Build()
```

**Descripción**
Ensambla y devuelve el `SqlQueryResult` final. Lanza excepción si no hay asignaciones SET o si no hay cláusula WHERE (a menos que se haya llamado `AllowUpdateAll()`).

---

## SqlDeleteBuilder

`SqlDeleteBuilder<T>` construye sentencias `DELETE` parametrizadas.

```csharp
var result = new SqlDeleteBuilder<User>(new SqlServerDialect())
    .From("Users")
    .Where(w => w.EqualTo(x => x.Id, 42))
    .Build();
// → DELETE FROM [Users] WHERE [Id] = @pw0
```

> **Seguridad:** Por defecto, `Build()` lanza una excepción si no se establece ninguna cláusula WHERE. Llama a `AllowDeleteAll()` para permitir explícitamente la eliminación de todas las filas.

---

### `From` (Delete)

**Firma**
```csharp
SqlDeleteBuilder<T> From(string tableName, string? schema = null)
```

**Descripción**
Establece el nombre de la tabla destino y el esquema opcional.

---

### `Where` (Delete)

Cuatro sobrecargas — misma semántica que `SqlUpdateBuilder`:

```csharp
SqlDeleteBuilder<T> Where(SqlWhereBuilder<T> whereBuilder)
SqlDeleteBuilder<T> Where(Action<SqlWhereBuilder<T>> configure)
SqlDeleteBuilder<T> Where(Expression<Func<T, bool>> predicate)
SqlDeleteBuilder<T> Where(ValiFlow<T> filter)
```

---

### `OutputDeleted`

**Firma**
```csharp
SqlDeleteBuilder<T> OutputDeleted()
```

**Descripción**
Agrega `OUTPUT DELETED.*` después de DELETE FROM. Solo para SQL Server — devuelve las filas eliminadas.

**Ejemplo**
```csharp
builder.From("Users").Where(x => x.Id == 42).OutputDeleted().Build();
// → DELETE FROM [Users] OUTPUT DELETED.* WHERE [Id] = @p0
```

---

### `Returning` (Delete)

**Firma**
```csharp
SqlDeleteBuilder<T> Returning(params Expression<Func<T, object>>[] columns)
```

**Descripción**
Agrega `RETURNING *` o columnas especificadas. Solo para PostgreSQL / SQLite.

**Ejemplo**
```csharp
builder.From("Users").Where(x => x.Id == 42).Returning(x => x.Id, x => x.Name).Build();
// → DELETE FROM "Users" WHERE "Id" = @p0 RETURNING "Id", "Name"
```

---

### `AllowDeleteAll`

**Firma**
```csharp
SqlDeleteBuilder<T> AllowDeleteAll()
```

**Descripción**
Permite explícitamente generar un DELETE sin cláusula WHERE (afecta todas las filas).

---

### `Tag` (Delete)

**Firma**
```csharp
SqlDeleteBuilder<T> Tag(string description)
```

**Descripción**
Etiqueta la consulta con un comentario SQL.

---

### `Build` (Delete)

**Firma**
```csharp
SqlQueryResult Build()
```

**Descripción**
Ensambla y devuelve el `SqlQueryResult` final.

---

## SqlTruncateBuilder

`SqlTruncateBuilder<T>` construye sentencias `TRUNCATE TABLE`.

> **Nota:** SQLite no soporta TRUNCATE TABLE. `Build()` lanza `InvalidOperationException` al usar `SqliteDialect`. Usa `SqlDeleteBuilder` con `AllowDeleteAll()` en su lugar.

```csharp
var result = new SqlTruncateBuilder<User>(new SqlServerDialect())
    .Table("Users")
    .Build();
// → TRUNCATE TABLE [Users]
```

---

### `Table` (Truncate)

**Firma**
```csharp
SqlTruncateBuilder<T> Table(string tableName, string? schema = null)
```

**Descripción**
Establece la tabla a truncar. Si no se llama, usa `typeof(T).Name`.

---

### `Tag` (Truncate)

**Firma**
```csharp
SqlTruncateBuilder<T> Tag(string description)
```

**Descripción**
Agrega un encabezado de comentario SQL para trazabilidad.

---

### `Build` (Truncate)

**Firma**
```csharp
SqlQueryResult Build()
```

**Descripción**
Construye y devuelve la sentencia `TRUNCATE TABLE` como un `SqlQueryResult` con un diccionario de parámetros vacío.

**Ejemplo con esquema**
```csharp
new SqlTruncateBuilder<Order>(new SqlServerDialect())
    .Table("Orders", schema: "dbo")
    .Tag("Limpiar tabla de pedidos")
    .Build();
// → -- Limpiar tabla de pedidos
//   TRUNCATE TABLE [dbo].[Orders]
```

---

## SqlMergeBuilder

`SqlMergeBuilder<TTarget, TSource>` construye sentencias `MERGE` de SQL Server. Para upsert en PostgreSQL, usa `SqlInsertBuilder.OnConflictDoUpdate` en su lugar.

> **Nota:** `Build()` lanza `InvalidOperationException` en dialectos distintos a `SqlServerDialect`. Solo SQL Server soporta la sintaxis MERGE tal como está implementada aquí.

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

**Firma**
```csharp
SqlMergeBuilder<TTarget, TSource> Into(string tableName, string? schema = null)
```

**Descripción**
Establece la tabla destino del MERGE.

---

### `Using`

**Firma**
```csharp
SqlMergeBuilder<TTarget, TSource> Using(string sourceTable, string alias = "src")
```

**Descripción**
Establece el nombre y alias de la tabla fuente usados en la cláusula `USING`.

---

### `On`

**Firma**
```csharp
SqlMergeBuilder<TTarget, TSource> On(
    Expression<Func<TTarget, object>> targetKey,
    Expression<Func<TSource, object>> sourceKey)
```

**Descripción**
Agrega una condición de unión: `target.col = source.col`. Se puede llamar múltiples veces para claves compuestas.

**Ejemplo**
```csharp
builder.On(t => t.TenantId, s => s.TenantId).On(t => t.ExternalId, s => s.ExternalId);
// ON target.[TenantId] = src.[TenantId] AND target.[ExternalId] = src.[ExternalId]
```

---

### `WhenMatchedUpdate`

**Firma**
```csharp
SqlMergeBuilder<TTarget, TSource> WhenMatchedUpdate(
    Action<SqlMergeBuilder<TTarget, TSource>> configure)
```

**Descripción**
Configura la cláusula `WHEN MATCHED THEN UPDATE SET ...`. La acción `configure` llama a `MatchedSetColumn` y/o `MatchedSetValue`.

---

### `MatchedSetColumn`

**Firma**
```csharp
SqlMergeBuilder<TTarget, TSource> MatchedSetColumn(
    Expression<Func<TTarget, object>> targetCol,
    Expression<Func<TSource, object>> sourceCol)
```

**Descripción**
En WHEN MATCHED UPDATE: establece `target.targetCol = source.sourceCol` (copia columna a columna, sin parámetros).

---

### `MatchedSetValue`

**Firma**
```csharp
SqlMergeBuilder<TTarget, TSource> MatchedSetValue<TValue>(
    Expression<Func<TTarget, object>> targetCol, TValue value)
```

**Descripción**
En WHEN MATCHED UPDATE: establece `target.targetCol = @pmN` (valor parametrizado).

---

### `WhenNotMatchedInsert`

**Firma**
```csharp
SqlMergeBuilder<TTarget, TSource> WhenNotMatchedInsert(
    Action<SqlMergeBuilder<TTarget, TSource>> configure)
```

**Descripción**
Configura la cláusula `WHEN NOT MATCHED BY TARGET THEN INSERT (...)`. La acción `configure` llama a `NotMatchedInsertColumn` y/o `NotMatchedInsertValue`.

---

### `NotMatchedInsertColumn`

**Firma**
```csharp
SqlMergeBuilder<TTarget, TSource> NotMatchedInsertColumn(
    Expression<Func<TTarget, object>> targetCol,
    Expression<Func<TSource, object>> sourceCol)
```

**Descripción**
En WHEN NOT MATCHED INSERT: mapea `targetCol = source.sourceCol`.

---

### `NotMatchedInsertValue`

**Firma**
```csharp
SqlMergeBuilder<TTarget, TSource> NotMatchedInsertValue<TValue>(
    Expression<Func<TTarget, object>> targetCol, TValue value)
```

**Descripción**
En WHEN NOT MATCHED INSERT: mapea `targetCol = @pmN` (valor parametrizado).

---

### `WhenNotMatchedBySourceDelete`

**Firma**
```csharp
SqlMergeBuilder<TTarget, TSource> WhenNotMatchedBySourceDelete()
```

**Descripción**
Agrega `WHEN NOT MATCHED BY SOURCE THEN DELETE`. Elimina filas del destino que no tienen fila correspondiente en la fuente.

---

### `Tag` (Merge)

**Firma**
```csharp
SqlMergeBuilder<TTarget, TSource> Tag(string description)
```

**Descripción**
Agrega un encabezado de comentario SQL y habilita el tag de consola para trazabilidad.

---

### `Build` (Merge)

**Firma**
```csharp
SqlQueryResult Build()
```

**Descripción**
Construye y devuelve la sentencia MERGE parametrizada. Lanza excepción si `Into()`, `Using()`, `On()` y al menos una cláusula WHEN no han sido llamados.

**Ejemplo completo**
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
    .Tag("Sincronizar usuarios desde staging")
    .Build();

// → -- Sincronizar usuarios desde staging
//   MERGE INTO [dbo].[Users] AS target
//   USING [Staging] AS src ON target.[Email] = src.[Email]
//   WHEN MATCHED THEN
//       UPDATE SET target.[Name] = src.[Name], target.[UpdatedAt] = src.[UpdatedAt]
//   WHEN NOT MATCHED BY TARGET THEN
//       INSERT ([Email], [Name], [CreatedAt]) VALUES (src.[Email], src.[Name], @pm0)
//   WHEN NOT MATCHED BY SOURCE THEN DELETE;
```
