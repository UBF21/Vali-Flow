# Benchmarks de Vali-Flow

El proyecto `Vali-Flow.Benchmarks` mide el rendimiento del evaluador InMemory, del constructor SQL y del evaluador EF Core usando [BenchmarkDotNet](https://benchmarkdotnet.org/).

---

## Ejecutar los benchmarks

Siempre ejecutar en modo Release. Las builds de Debug desactivan las fases de warmup y medición de BenchmarkDotNet.

```bash
cd Vali-Flow.Benchmarks
dotnet run --configuration Release
```

Los resultados se generan en `BenchmarkDotNet.Artifacts/results/`. No commitear estos archivos — están en .gitignore.

Para ejecutar una clase de benchmark específica:

```bash
dotnet run --configuration Release -- --filter "*InMemoryFilter*"
dotnet run --configuration Release -- --filter "*SqlBuilder*"
dotnet run --configuration Release -- --filter "*EfCore*"
```

---

## Suites de benchmarks

### Evaluador InMemory

Todos los benchmarks InMemory parametrizan sobre `N ∈ { 1 000, 10 000, 100 000 }` para mostrar cómo escala cada operación.

| Clase | Qué mide |
|-------|----------|
| `InMemoryFilterBenchmarks` | `EvaluateQuery` (enumerar todos) y `EvaluateAny` — baseline es LINQ crudo `.Where()` |
| `InMemoryCountBenchmarks` | `EvaluateCount` con un filtro simple vs un filtro compuesto |
| `InMemoryAggregateBenchmarks` | `EvaluateSum`, `EvaluateAverage`, `EvaluateMin`, `EvaluateMax`, `EvaluateCountByGroup` |
| `InMemoryWriteBenchmarks` | `Add`, `Update`, `Delete`, `Upsert` — costo por operación |
| `InMemoryPaginationBenchmarks` | `EvaluatePaged` en diferentes tamaños de página |
| `InMemoryLookupBenchmarks` | Búsqueda directa por ID vs búsqueda con filtro |

### Constructor SQL

| Clase | Qué mide |
|-------|----------|
| `SqlBuilderBenchmarks` | `SqlQueryBuilder` SELECT con JOINs, INSERT, UPDATE — solo el costo de construcción (sin round-trip a BD) |

### Evaluador EF Core

Usa el proveedor **InMemory de EF Core** (`Microsoft.EntityFrameworkCore.InMemory`) — mide el overhead de `ValiFlowEvaluator<T>`/`QuerySpecificationBuilder<T>` (construcción de query + traducción LINQ), no I/O real de base de datos. Parametrizado sobre `N ∈ { 1 000, 10 000, 100 000 }`.

| Clase | Qué mide |
|-------|----------|
| `EfCoreBenchmarks` | Filtro con `EvaluateQueryAsync` (baseline: LINQ crudo `.Where()` sobre el mismo `DbContext`), `EvaluatePagedAsync`, `EvaluateCountAsync` |

---

## Interpretar los resultados

BenchmarkDotNet reporta:

| Columna | Significado |
|---------|-------------|
| `Mean` | Tiempo promedio por operación en todas las iteraciones |
| `Ratio` | Relativo al método marcado como `[Benchmark(Baseline = true)]` |
| `Allocated` | Heap administrado asignado por operación (requiere `[MemoryDiagnoser]`) |

**Qué buscar:**
- `Ratio ≈ 1.0` — al nivel del baseline (típicamente LINQ crudo)
- `Ratio < 2.0` — overhead aceptable para la capa de abstracción del evaluador
- Bytes asignados creciendo con `N` — esperado; asignación plana entre tamaños de `N` indica un posible problema

---

## Agregar un nuevo benchmark

1. Crear una clase en `Vali-Flow.Benchmarks/Benchmarks/` anotada con `[MemoryDiagnoser]`.
2. Parametrizar con `[Params(1_000, 10_000, 100_000)]` para operaciones sensibles al tamaño de colección.
3. Marcar un método como `[Benchmark(Baseline = true)]` — usar la operación LINQ/directa equivalente como baseline.
4. Usar `BenchmarkDataFactory.Generate(N)` para producir datos de prueba consistentes y reproducibles.
5. Evitar efectos secundarios en los benchmarks — escribir resultados a un `Consumer` (el sink de BenchmarkDotNet), no a `Console`.

---

## Limitaciones conocidas

- **Sin benchmarks NoSQL** — los traductores `Vali-Flow.NoSql.*` (7 proveedores) no tienen benchmarks. La traducción es solo CPU (sin I/O) y los árboles son pequeños, por lo que el overhead esperado es sub-microsegundo.
- **Los benchmarks de EF Core miden construcción, no I/O real** — `EfCoreBenchmarks` usa el proveedor InMemory de EF Core, así que aísla el overhead de `ValiFlowEvaluator<T>`/`QuerySpecificationBuilder<T>`. El rendimiento real contra una base de datos está dominado por el round-trip y el plan de query, no por esta capa — para eso hace falta profiling contra un proveedor real.

---

## Ver también

- [Guía de contribución](contributing.md)
- [Visión general de la arquitectura](../architecture/overview.md)
