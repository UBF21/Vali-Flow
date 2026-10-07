# Changelog

[English](CHANGELOG.md) | **Español**

Todos los cambios relevantes del ecosistema Vali-Flow se documentan en este archivo.

El formato sigue [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) y este proyecto sigue [Versionado Semántico](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

---

## Vali-Flow.Abstractions — 1.2.0

### Agregado

- **`ValiFlowDiagnostics`** — helper compartido basado en `ActivitySource` con `StartActivity`/`RecordException` para trazabilidad compatible con OpenTelemetry, ahora conectado en el evaluador de EF Core, `Vali-Flow.InMemory`, los seis builders de `Vali-Flow.Sql`, y los traductores de Mongo/DynamoDB/Elasticsearch/Redis.

---

## Vali-Flow — 1.4.0

### Agregado

- **`GenericRepository<T, TKey>`** — un repositorio ligero que envuelve a `ValiFlowEvaluator<T>` con `GetById`/`GetAll`/`GetPaged`/`Add`/`Update`/`Delete`/`SaveChanges`, para consumidores que prefieren una superficie de repositorio convencional en lugar de llamar al evaluador directamente.
- Benchmark de EF Core usando el proveedor `EFCore.InMemory` (`Vali-Flow.Benchmarks`).
- Trazabilidad de `ValiFlowDiagnostics` conectada en las rutas de lectura/escritura del evaluador.

### Corregido

- `UpsertRangeAsync` perdía entradas con claves duplicadas (usaba un acumulador de lista en lugar de un diccionario) — corregido a un acumulador de diccionario.
- `UpsertRangeAsync.PartitionUpsertRange` podía descartar la clave primaria de una entidad rastreada — ahora se preserva.
- `BulkInsertOrUpdateAsync` podía entrar en condición de carrera bajo llamadas concurrentes para el mismo tipo de entidad + columnas clave — las llamadas ahora se serializan por esa combinación.
- **Contención en upsert masivo bajo concurrencia**: se reemplazó el retry ad-hoc por una política de retry transitorio genérica (`BulkUpsertRetryPolicy`) que cubre SQL Server, PostgreSQL, MySQL y SQLite, controlada por tabla (no por el conjunto de claves de `UpdateByProperties`) para evitar serializar upserts no relacionados entre sí. El presupuesto de reintentos se aumentó en dos pasos (4 → 7 → 12 intentos) según la contención observada bajo carga.
- Se eliminó una prueba inestable de captura de `Activity` (la aserción dependiente del tiempo fue reemplazada por una captura determinista).

### Cambiado

- La lógica de construcción de consultas se extrajo de `ValiFlowEvaluator` hacia un `QuerySpecificationBuilder` dedicado (separación por responsabilidad única, sin cambios en la API pública).
- La cobertura de pruebas subió de 74.37% a 97.42%.

---

## Vali-Flow.InMemory — 1.2.0

### Agregado

- Trazabilidad de `ValiFlowDiagnostics` conectada en las rutas de lectura/escritura del evaluador.

### Corregido

- `Update(entity, externalList)` podía filtrar la entidad actualizada hacia el almacén interno en una llamada posterior a `SaveChanges()` sin argumentos.
- Se eliminó una prueba inestable de captura de `Activity` (la aserción dependiente del tiempo fue reemplazada por una captura determinista).

### Cambiado

- `ValiFlowEvaluator<T, TProperty>` (904 líneas) se dividió en archivos parciales `Bridge`/`Read`/`Write`/`Grouped`; sin cambios en la API pública.

---

## Vali-Flow.Sql — 1.2.0

### Agregado

- `SqlInsertBuilder`/`SqlUpdateBuilder.SetAllFrom(entity, exclude...)` — mapeador masivo de columnas basado en reflexión.
- Trazabilidad de `ValiFlowDiagnostics` conectada en los seis builders.

### Corregido

- **Seguridad — inyección de identificadores SQL**: los builders interpolaban directamente nombres de tabla/esquema/tipo; se agregó `SqlIdentifierGuard` (lista blanca por expresión regular) que valida todos los identificadores antes de interpolarlos.
- Error de guardia de dialecto en `OrIgnore`/`OrReplace`, pérdida silenciosa de datos al llamar `Set()` después de `SelectFrom()`, y llamada doble a `Where()` que sobrescribía silenciosamente la primera condición en lugar de combinarla — todo corregido.
- Se agregó la guardia `AllowDeleteUnmatched()`, requerida antes de `WhenNotMatchedBySourceDelete` en `SqlMergeBuilder` (evita eliminaciones no controladas accidentales en un MERGE).
- Se eliminó una prueba inestable de captura de `Activity` (la aserción dependiente del tiempo fue reemplazada por una captura determinista).

---

## Vali-Flow.NoSql — 1.1.1

### Agregado

- Trazabilidad de `ValiFlowDiagnostics` conectada en la base de traducción compartida usada por los proveedores Mongo y Elasticsearch.

### Corregido

- Los valores `null` capturados por closure en una condición `ValiFlow<T>` no se traducían al mismo `NullNode` que un `null` literal, produciendo filtros inconsistentes entre `.EqualTo(x => x.Prop, someNullVariable)` y `.EqualTo(x => x.Prop, null)`.
- Las listas de valores `IN` sin límite podían exceder los límites propios del motor; se agregaron topes de `MaxInValues` (Mongo 10,000; Elasticsearch 65,536, en línea con `index.max_terms_count`).

---

## Vali-Flow.NoSql.MongoDB — 1.2.0

### Agregado

- Trazabilidad de `ValiFlowDiagnostics` conectada en `MongoFilterTranslator`.

---

## Vali-Flow.NoSql.Elasticsearch — 1.2.0

### Agregado

- Trazabilidad de `ValiFlowDiagnostics` conectada en `ElasticsearchFilterTranslator`.

### Corregido

- Excepción interna silenciada en la conversión `ToDouble` — los fallos se mostraban como un error genérico sin causa raíz.

---

## Vali-Flow.NoSql.Redis — 1.2.0

### Agregado

- Trazabilidad de `ValiFlowDiagnostics` conectada en `RedisSearchFilterTranslator`.

### Corregido

- **Seguridad — inyección de consultas RediSearch**: los caracteres especiales de la sintaxis de consulta (p. ej. `)`/`|`) no se escapaban en los patrones `LIKE` antes de envolverlos en comodines, lo que permitía inyección de estructura de consulta. Ahora se escapan antes de aplicar los comodines.
- `customConverter` no se aplicaba en `VisitComparison`/`VisitIn` numérico, ignorando silenciosamente las conversiones de tipo personalizadas en condiciones de comparación e `IN`.

---

## Vali-Flow.NoSql.DynamoDB — 1.2.0

### Agregado

- Trazabilidad de `ValiFlowDiagnostics` conectada en `DynamoFilterTranslator`.

---

## Vali-Flow.NoSql.Couchbase — 1.1.0 *(lanzamiento inicial)*

### Agregado

- `CouchbaseFilterTranslator` — traduce nodos IR de `ValiFlow<T>` a un fragmento WHERE en N1QL/SQL++, devuelto como `CouchbaseFilterExpression { WhereClause, Parameters }` — sin dependencia del SDK de Couchbase; el consumidor aplica el fragmento con su propio cliente.
- `ValiFlowCouchbaseExtensions` — método de extensión sobre `ValiFlow<T>`.

### Corregido

- Los valores `decimal` se vinculaban como literales de cadena en N1QL en lugar de números nativos.

---

## Vali-Flow.NoSql.CosmosDb — 1.1.0 *(lanzamiento inicial)*

### Agregado

- `CosmosFilterTranslator` — traduce nodos IR de `ValiFlow<T>` a un fragmento WHERE de la SQL API de Azure Cosmos DB, devuelto como `CosmosFilterExpression { WhereClause, Parameters }` — sin dependencia de `Microsoft.Azure.Cosmos`; el consumidor aplica el fragmento con su propio cliente.
- `ValiFlowCosmosExtensions` — método de extensión sobre `ValiFlow<T>`.

---

## Vali-Flow.NoSql.Firestore — 1.1.0 *(lanzamiento inicial)*

### Agregado

- `FirestoreFilterTranslator` — traduce nodos IR de `ValiFlow<T>` a un `Filter` nativo de Google Cloud Firestore, dependiendo del SDK oficial `Google.Cloud.Firestore`.
- `ValiFlowFirestoreExtensions` — método de extensión sobre `ValiFlow<T>`.
- Nota: `LikeNode` (los 3 operadores) y el `NotNode` genérico lanzan `NotSupportedException` — el SDK de Firestore no tiene consulta por coincidencia de patrones ni NOT sobre un sub-filtro arbitrario, solo negación a nivel de campo (`EqualNode.IsNegated` → `NotEqualTo`/`NotInArray`).

---

## Vali-Flow — 1.3.3

### Corregido
- Se eliminó el modificador `sealed` de todas las declaraciones parciales de `ValiFlowEvaluator<T>`:
  - ValiFlowEvaluator.cs (principal)
  - ValiFlowEvaluator.Read.cs
  - ValiFlowEvaluator.Write.cs
  - ValiFlowEvaluator.Aggregates.cs
  - ValiFlowEvaluator.Grouped.cs
  - ValiFlowEvaluator.Bridge.cs

---

## Vali-Flow.InMemory — 1.1.4

### Corregido
- Se eliminó el modificador `sealed` de todas las clases para habilitar herencia completa:
  - `ValiFlowEvaluator<T, TProperty>` (evaluador principal)
  - `AsyncInMemoryAdapter<T, TProperty>` (envoltorio async)
  - `InMemoryWriteStore<T, TProperty>` (almacén de operaciones de escritura)
  - `PagedResult<T>` (modelo de resultado de paginación)

Los usuarios ahora pueden crear implementaciones personalizadas extendiendo cualquiera de estas clases.

---

## Vali-Flow — 1.1.0

### Agregado
- `EvaluatePagedAsync` devuelve un `PagedResult<T>` con `Items`, `TotalCount`, `TotalPages`, `HasNextPage` y `HasPreviousPage`
- `EvaluateTopByGroupAsync` — entidades top-N por clave de grupo
- `EvaluateDistinctAsync` y `EvaluateDuplicatesAsync` — detección de distintos y duplicados por selector de clave
- `EvaluateAggregateAsync` — agregación genérica sobre cualquier selector numérico
- `EvaluateGroupedAsync`, `EvaluateCountByGroupAsync`, `EvaluateSumByGroupAsync`, `EvaluateMinByGroupAsync`, `EvaluateMaxByGroupAsync`, `EvaluateAverageByGroupAsync` — superficie completa de agregados agrupados
- `ExecuteUpdateAsync` — actualización masiva in-place vía `ExecuteUpdate` (sin cargar entidades)
- `ExecuteTransactionAsync` — envuelve múltiples operaciones en una sola transacción de EF Core
- `BulkInsertAsync`, `BulkUpdateAsync`, `BulkDeleteAsync`, `BulkInsertOrUpdateAsync` — operaciones masivas de alto rendimiento vía `EFCore.BulkExtensions`
- `UpsertRangeAsync` — upsert por lotes con selector de clave configurable
- `DeleteByConditionAsync` — eliminación por predicado directo sin cargar entidades
- `EvaluateGetLastAsync` / `EvaluateGetLastFailedAsync` — recuperación de la última coincidencia
- `QuerySpecification<T>.WithTop`, `WithPagination`, `WithOrderBy`, `AddThenBy`, `WithValiSort` — builder fluido de especificación completo
- `BasicSpecification<T>` — especificación simplificada sin ordenamiento/paginación
- Documentación XML en todos los tipos y miembros públicos

### Cambiado
- `ValiFlowEvaluator<T>` ahora es una `sealed partial class` dividida en archivos enfocados (Read, Write, Grouped, Aggregates, Bridge)
- Mensajes de excepción mejorados para combinaciones de especificación inválidas (Top + Pagination, Pagination sin OrderBy)
- `EvaluateAllAsync` y `EvaluateAllFailedAsync` marcados `[Obsolete]` — usar `EvaluateQueryAsync` / `EvaluateQueryFailedAsync`
- La licencia cambió de Apache-2.0 a MIT

### Corregido
- Problemas de seguridad de hilos (thread-safety) en uso concurrente del evaluador
- Violaciones de caché y DRY en la ejecución diferida de `UpsertCore`
- Advertencias de nulabilidad CS87xx en las clases del evaluador y de especificación

---

## Vali-Flow.InMemory — 1.0.0 *(lanzamiento inicial)*

### Agregado
- `ValiFlowEvaluator<T, TProperty>` — evaluador síncrono en memoria sobre `IEnumerable<T>`
- Superficie de lectura completa: `Evaluate`, `EvaluateAny`, `EvaluateCount`, `GetFirst`, `GetLast`, `EvaluateAll`, `EvaluateAllFailed`, `EvaluatePaged`, `EvaluatePagedResult`, `EvaluateTop`, `EvaluateDistinct`, `EvaluateDuplicates`, `GetFirstMatchIndex`, `GetLastMatchIndex`
- Superficie de agregados completa: `EvaluateMin`, `EvaluateMax`, `EvaluateAverage`, `EvaluateSum`, `EvaluateAggregate`
- Superficie agrupada completa: `EvaluateGrouped`, `EvaluateCountByGroup`, `EvaluateSumByGroup`, `EvaluateMinByGroup`, `EvaluateMaxByGroup`, `EvaluateAverageByGroup`, `EvaluateDuplicatesByGroup`, `EvaluateUniquesByGroup`, `EvaluateTopByGroup`
- Superficie de escritura completa: `Add`, `Update`, `Delete`, `AddRange`, `UpdateRange`, `DeleteRange`, `Upsert`, `UpsertRange`, `DeleteByCondition`, `SaveChanges`
- `SetValiFlow` — reemplaza el filtro activo en tiempo de ejecución
- `InMemoryWriteStore<T>` — almacén de escritura extraído para facilitar pruebas y cumplir SRP
- Documentación XML en todos los tipos y miembros públicos

---

## Vali-Flow.Sql — 1.0.0 *(lanzamiento inicial)*

### Agregado
- `SqlQueryBuilder<T>` — builder fluido de SELECT con JOINs, WHERE, GROUP BY, HAVING, ORDER BY, UNION, EXISTS, CASE WHEN, paginación, hints de consulta
- `SqlInsertBuilder<T>` — INSERT con mapeo de columnas y soporte multi-fila
- `SqlUpdateBuilder<T>` — UPDATE con cláusulas SET y condiciones WHERE
- `SqlDeleteBuilder<T>` — DELETE con condiciones WHERE
- `SqlTruncateBuilder<T>` — TRUNCATE TABLE
- `SqlMergeBuilder<TTarget, TSource>` — builder de instrucciones MERGE / UPSERT
- `SqlWhereBuilder<T>` — builder independiente de cláusula WHERE con encadenamiento fluido AND/OR
- `SqlHavingBuilder<T>` — builder independiente de cláusula HAVING
- `CaseWhenBuilder` — builder fluido de CASE WHEN / THEN / ELSE
- `SqlExpressionTranslator` — convierte árboles de expresión de `ValiFlow<T>` a SQL parametrizado
- `SqlResult` — contiene la cadena SQL generada y el diccionario de parámetros; `ApplyTo(IDbCommand)` aplica los parámetros a comandos ADO.NET
- `SqlQueryResult` — combina `SqlResult` con metadatos de paginación
- Soporte de dialectos: `SqlServerDialect`, `PostgreSqlDialect`, `MySqlDialect`, `SqliteDialect`, `OracleDialect`
- `ValiFlowSqlExtensions.ToSql(dialect)` — método de extensión sobre `ValiFlow<T>`
- Documentación XML en todos los tipos y miembros públicos

---

## Vali-Flow.NoSql — incluido con los paquetes proveedores

### Vali-Flow.NoSql.MongoDB — 1.0.0 *(lanzamiento inicial)*
- `MongoFilterTranslator` — traduce nodos IR a `FilterDefinition<T>` de MongoDB
- `ValiFlowMongoExtensions.ToMongoFilter()` — extensión sobre `ValiFlow<T>`

### Vali-Flow.NoSql.DynamoDB — 1.0.0 *(lanzamiento inicial)*
- `DynamoFilterTranslator` — traduce nodos IR a `FilterExpression` + `ExpressionAttributeValues` de DynamoDB
- `DynamoFilterExpression` — contiene la cadena de expresión y el mapa de atributos
- `ValiFlowDynamoExtensions.ToDynamoFilter()` — extensión sobre `ValiFlow<T>`

### Vali-Flow.NoSql.Elasticsearch — 1.0.0 *(lanzamiento inicial)*
- `ElasticsearchFilterTranslator` — traduce nodos IR a `Query` de Elasticsearch (Elastic.Clients.Elasticsearch)
- `ValiFlowElasticsearchExtensions.ToElasticsearchQuery()` — extensión sobre `ValiFlow<T>`

### Vali-Flow.NoSql.Redis — 1.0.0 *(lanzamiento inicial)*
- `RedisSearchFilterTranslator` — traduce nodos IR a cadenas de consulta RediSearch
- `ValiFlowRedisSearchExtensions.ToRedisQuery()` — extensión sobre `ValiFlow<T>`

---

## Vali-Flow — 1.0.0 *(lanzamiento inicial)*

### Agregado
- `ValiFlowEvaluator<T>` — evaluador async de EF Core que implementa `IEvaluatorRead<T>` e `IEvaluatorWrite<T>`
- `BasicSpecification<T>` y `QuerySpecification<T>` — patrón de especificación con API de builder fluido
- `EvaluateAsync`, `EvaluateAnyAsync`, `EvaluateCountAsync`, `EvaluateGetFirstAsync`, `EvaluateGetLastAsync`, `EvaluateQueryAsync`, `EvaluateQueryFailedAsync`
- `AddAsync`, `UpdateAsync`, `DeleteAsync`, `AddRangeAsync`, `UpdateRangeAsync`, `DeleteRangeAsync`, `UpsertAsync`, `SaveChangesAsync`
- `IEvaluatorRead<T>`, `IEvaluatorWrite<T>`, `ISpecification<T>`, `IBasicSpecification<T>`, `IQuerySpecification<T>`
- `EfInclude<T,TProperty>`, `EfOrderBy<T,TProperty>`, `EfOrderThenBy<T,TProperty>` — envoltorios de opciones de EF Core
- `PagedResult<T>` — modelo de resultado de paginación
- Dependencia de `Vali-Flow.Core` para la construcción de árboles de expresión (`ValiFlow<T>`, `ValiSort<T>`)
