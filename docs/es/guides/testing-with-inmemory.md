# Testing con Vali-Flow.InMemory

`Vali-Flow.InMemory` es un evaluador sincrónico sin dependencias que ejecuta la misma lógica de filtros que el evaluador EF Core — pero sobre una lista `IEnumerable<T>` en lugar de un `DbContext`. Esto lo hace ideal para tests unitarios y capas de caché que necesitan filtrar datos ya cargados.

---

## ¿Por qué usar InMemory para tests?

- **Sin base de datos ni setup de EF Core** — sin `DbContextOptions`, sin SQLite en memoria, sin migraciones.
- **Sincrónico** — sin overhead de `async/await` en las aserciones de tests.
- **Misma API de filtros** — los mismos constructores `ValiFlow<T>` usados en producción se testean exactamente igual.
- **Mutable** — agrega, actualiza o elimina ítems durante un test y vuelve a consultar sin reconstruir el evaluador.

---

## Configuración

```bash
dotnet add package Vali-Flow.InMemory
```

La firma del constructor:

```csharp
new ValiFlowEvaluator<T, TProperty>(
    IEnumerable<T> initialData,
    ValiFlow<T>?   filter,
    Func<T, TProperty> getId
)
```

| Parámetro | Propósito |
|-----------|-----------|
| `initialData` | Los datos semilla. El evaluador copia la lista internamente. |
| `filter` | Filtro base opcional aplicado a cada consulta. Puede ser `null` para ningún filtro por defecto. |
| `getId` | Selector que identifica cada entidad (usado por update/delete por ID). |

---

## Ejemplo básico

```csharp
using Vali_Flow.Core.Builder;
using Vali_Flow.InMemory.Classes.Evaluators;
using Xunit;

public class ProductQueryTests
{
    private static List<Product> Seed() => new()
    {
        new() { Id = 1, Name = "Widget",      Price = 29.99m,  IsActive = true,  Category = "Herramientas" },
        new() { Id = 2, Name = "Gadget",      Price = 149.99m, IsActive = true,  Category = "Electrónica" },
        new() { Id = 3, Name = "Doohickey",   Price = 9.99m,   IsActive = false, Category = "Herramientas" },
        new() { Id = 4, Name = "Thingamajig", Price = 49.99m,  IsActive = true,  Category = "Herramientas" },
    };

    [Fact]
    public void ProductosActivosMayorA20_RetornaConteoCorrect()
    {
        var filter = new ValiFlow<Product>()
            .EqualTo(x => x.IsActive, true)
            .GreaterThan(x => x.Price, 20m);

        var evaluator = new ValiFlowEvaluator<Product, int>(Seed(), filter, p => p.Id);

        int count = evaluator.EvaluateCount();

        Assert.Equal(3, count);
    }

    [Fact]
    public void GetFirst_RetornaElDeIdMenor()
    {
        var filter = new ValiFlow<Product>().EqualTo(x => x.Category, "Herramientas");
        var evaluator = new ValiFlowEvaluator<Product, int>(Seed(), filter, p => p.Id);

        Product? first = evaluator.EvaluateGetFirst();

        Assert.NotNull(first);
        Assert.Equal(1, first.Id);
    }
}
```

---

## Testing de operaciones de escritura

El evaluador expone la misma superficie de escritura que el evaluador EF Core, pero todas las operaciones son sincrónicas y mutan el almacén en memoria:

```csharp
[Fact]
public void Add_NuevoProducto_IncrementaConteo()
{
    var evaluator = new ValiFlowEvaluator<Product, int>(new List<Product>(), null, p => p.Id);

    evaluator.Add(new Product { Id = 10, Name = "Nuevo", Price = 5m, IsActive = true });

    Assert.Equal(1, evaluator.EvaluateCount());
}

[Fact]
public void Delete_EliminaItemsQueCoinciden()
{
    var data = new List<Product>
    {
        new() { Id = 1, IsActive = true,  Price = 10m },
        new() { Id = 2, IsActive = false, Price = 10m },
    };
    var evaluator = new ValiFlowEvaluator<Product, int>(data, null, p => p.Id);

    var inactive = new ValiFlow<Product>().EqualTo(x => x.IsActive, false);
    evaluator.DeleteByCondition(inactive);

    Assert.Equal(1, evaluator.EvaluateCount());
}
```

---

## Testing de paginación y ordenamiento

```csharp
[Fact]
public void EvaluatePaged_RetornaPaginaCorrecta()
{
    var products = Enumerable.Range(1, 20)
        .Select(i => new Product { Id = i, Name = $"P{i}", Price = i * 10m, IsActive = true })
        .ToList();

    var evaluator = new ValiFlowEvaluator<Product, int>(products, null, p => p.Id);

    // Página 2, 5 ítems por página
    var page = evaluator.EvaluatePaged(page: 2, pageSize: 5).ToList();

    Assert.Equal(5, page.Count);
    Assert.Equal(6, page.First().Id);
}
```

---

## Reutilizar un filtro en tests unitarios e integración

Un patrón común es definir el filtro en un método compartido y ejecutarlo tanto contra el evaluador InMemory (rápido, test unitario) como contra el evaluador EF Core real (más lento, test de integración):

```csharp
// Fábrica de filtro compartida
public static ValiFlow<Product> FiltroActivosEconomicos() =>
    new ValiFlow<Product>()
        .EqualTo(x => x.IsActive, true)
        .LessThanOrEqualTo(x => x.Price, 100m);

// Test unitario — InMemory
[Fact]
public void Unitario_ActivosEconomicos_Conteo()
{
    var evaluator = new ValiFlowEvaluator<Product, int>(Seed(), null, p => p.Id);
    int count = evaluator.EvaluateCount(FiltroActivosEconomicos());
    Assert.Equal(2, count);
}

// Test de integración — EF Core (requiere un DbContext real o SQLite)
[Fact]
public async Task Integracion_ActivosEconomicos_Conteo()
{
    await using var context = CrearContextoDeTest();
    var evaluator = new ValiFlowEvaluator<Product>(context);
    var spec = new QuerySpecification<Product>(FiltroActivosEconomicos());
    int count = await evaluator.EvaluateCountAsync(spec);
    Assert.Equal(2, count);
}
```

Esto asegura que el mismo comportamiento del filtro se valida en ambos niveles sin duplicar lógica.

---

## Testing de métodos agregados

```csharp
[Fact]
public void Sum_PrecioDeProductosActivos()
{
    var filter = new ValiFlow<Product>().EqualTo(x => x.IsActive, true);
    var evaluator = new ValiFlowEvaluator<Product, int>(Seed(), filter, p => p.Id);

    decimal total = evaluator.EvaluateSum(x => x.Price);

    Assert.Equal(229.97m, total);
}

[Fact]
public void ConteoPorGrupo_PorCategoria()
{
    var evaluator = new ValiFlowEvaluator<Product, int>(Seed(), null, p => p.Id);

    var counts = evaluator.EvaluateCountByGroup(x => x.Category).ToList();

    Assert.Equal(3, counts.First(g => g.Key == "Herramientas").Count);
}
```

---

## Consejos

- Pasa `null` como parámetro `filter` cuando quieras empezar sin filtro global y pasar diferentes filtros por llamada.
- El evaluador acepta un `IEnumerable<T>` pero lo itera eagerly — las modificaciones a la lista fuente después de la construcción no tienen efecto.
- Para escenarios Upsert, `getId` debe retornar un valor que identifique unívocamente cada entidad — los duplicados causarán comportamiento incorrecto en las actualizaciones.
- Usa `EvaluateAny()` como guardia en tests para asertir que se alcanzó un estado antes de asertir propiedades más detalladas.
