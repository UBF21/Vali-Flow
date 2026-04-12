# Contribuir a Vali-Flow

Este documento es para contribuidores que trabajan en los paquetes de Vali-Flow: agregando características, corrigiendo bugs, escribiendo tests o mejorando la documentación.

---

## Layout del repositorio

```
Vali-Flow/                      ← este repositorio
├── Vali-Flow/                  Evaluador EF Core
├── Vali-Flow.InMemory/         Evaluador en memoria
├── Vali-Flow.Sql/              Traductor SQL + constructores
├── Vali-Flow.NoSql/            IR compartido + base del visitor
├── Vali-Flow.NoSql.MongoDB/    Adaptador MongoDB
├── Vali-Flow.NoSql.DynamoDB/   Adaptador DynamoDB
├── Vali-Flow.NoSql.Elasticsearch/  Adaptador Elasticsearch
├── Vali-Flow.NoSql.Redis/      Adaptador Redis
├── Vali-Flow.Abstractions/     Interfaces compartidas
├── Vali-Flow.Benchmarks/       Benchmarks con BenchmarkDotNet
├── Vali-Flow.Tests/            Tests del evaluador EF Core
├── Vali-Flow.InMemory.Tests/   Tests del evaluador InMemory
├── Vali-Flow.Sql.Tests/        Tests del traductor SQL
├── Vali-Flow.NoSql.Tests/      Tests de adaptadores NoSQL
├── Vali-Flow.NoSql.DynamoDB.Tests/  Tests de DynamoDB
└── docs/                       Documentación (en/ + es/)

Vali-Flow.Core/                 ← repositorio separado
  /Users/feliperafaelmontenegro/RiderProjects/Vali-Flow.Core
```

`Vali-Flow.Core` se mantiene en un repositorio separado. Los cambios a Core se hacen allí; los cambios a los evaluadores y traductores se hacen aquí.

---

## Requisitos previos

- .NET 8 SDK o .NET 9 SDK
- Rider o Visual Studio (o VS Code con C# DevKit)
- `Vali-Flow.Core` clonado en `/Users/feliperafaelmontenegro/RiderProjects/Vali-Flow.Core` (la ruta del `ProjectReference` local)

---

## Build

```bash
# Build completo de la solución
dotnet build

# Build en Release (usado antes de empaquetar)
dotnet build --configuration Release
```

---

## Ejecutar tests

```bash
# Todos los proyectos de test
dotnet test

# Proyecto específico
dotnet test Vali-Flow.Sql.Tests/

# Con verbosidad
dotnet test --logger "console;verbosity=detailed"
```

Los proyectos de test usan **xUnit** y **FluentAssertions**.

---

## Empaquetar un paquete NuGet

```bash
dotnet pack Vali-Flow/Vali-Flow.csproj --configuration Release
dotnet pack Vali-Flow.InMemory/Vali-Flow.InMemory.csproj --configuration Release
dotnet pack Vali-Flow.Sql/Vali-Flow.Sql.csproj --configuration Release
```

Los archivos `.nupkg` se generan en `<proyecto>/bin/Release/`.

---

## Agregar un nuevo método a ValiFlow<T> (Core)

Los nuevos métodos de filtro (p.ej., un nuevo operador de cadenas) van en `Vali-Flow.Core`. El proceso:

1. Agregar el nuevo nodo IR a `Vali-Flow.Core/IR/` (si se necesita un nuevo tipo de nodo).
2. Agregar el método fluido a `ValiFlow<T>` que crea el nodo.
3. Actualizar `ExpressionToIRVisitor` en `Vali-Flow.NoSql` para manejar el nuevo nodo.
4. Actualizar el traductor de cada adaptador NoSQL para emitir la consulta nativa correspondiente.
5. Actualizar el `ExpressionToSqlVisitor` de SQL para emitir el fragmento SQL.
6. Agregar tests en todos los proyectos de test afectados.
7. Actualizar la documentación en `docs/en/` y `docs/es/`.

---

## Agregar un nuevo dialecto SQL

1. Crear una clase que implemente `ISqlDialect` en `Vali-Flow.Sql/Dialects/`.
2. Implementar:
   - `QuoteIdentifier(string name)` — retornar la forma entre comillas específica del dialecto.
   - `ParameterPrefix` — retornar `"@"`, `":"`, etc.
   - `CaseInsensitiveLike` — retornar el operador o función para LIKE insensible a mayúsculas.
3. Agregar tests en `Vali-Flow.Sql.Tests/`.
4. Documentar en `docs/en/vali-flow-sql.md` y `docs/es/vali-flow-sql.md`.

---

## Agregar un nuevo adaptador NoSQL

1. Crear un nuevo proyecto `Vali-Flow.NoSql.<Tecnología>/`.
2. Referenciar `Vali-Flow.NoSql` (para la base IR) y el cliente NuGet oficial de la tecnología.
3. Implementar un `<Tecnología>FilterTranslator` que recorra `IRNode` y emita la consulta nativa.
4. Exponer un método de extensión en `ValiFlow<T>` y en `Expression<Func<T, bool>>`.
5. Agregar un proyecto de test `Vali-Flow.NoSql.<Tecnología>.Tests/`.
6. Agregar documentación en `docs/en/` y `docs/es/`.

---

## Documentación

La documentación vive en `docs/en/` (inglés) y `docs/es/` (español). Ambos idiomas deben mantenerse sincronizados.

- Los docs de referencia de cada paquete viven en `docs/{idioma}/vali-flow-<paquete>.md`.
- Las guías viven en `docs/{idioma}/guides/`.
- Los docs de arquitectura viven en `docs/{idioma}/architecture/`.
- Los docs internos de contribuidores viven en `docs/{idioma}/internal/`.

Cuando agregues o cambies una característica, actualiza los docs en `en/` y `es/` en el mismo commit.

---

## Benchmarks

`Vali-Flow.Benchmarks` usa BenchmarkDotNet. Ejecuta los benchmarks en modo Release:

```bash
cd Vali-Flow.Benchmarks
dotnet run --configuration Release
```

Los resultados se generan en `BenchmarkDotNet.Artifacts/`. No commitear los resultados de benchmarks.

---

## Estilo de commits

- Usar commits convencionales: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`.
- Mantener los commits enfocados: un cambio lógico por commit.
- Referenciar números de issue en el cuerpo del commit cuando aplique.

---

## Gestión de la versión de Core

El `ProjectReference` local a Core debe mantenerse consistente en todos los archivos `.csproj`. Cuando Core se publique en NuGet:

1. Reemplazar cada `<ProjectReference Include="...Vali-Flow.Core.csproj" />` con:
   ```xml
   <PackageReference Include="Vali-Flow.Core" Version="2.6.0" />
   ```
2. Verificar que todos los proyectos compilen y que todos los tests pasen.
3. Incrementar la versión de cada paquete que cambie.

---

## Ver también

- [Visión general de la arquitectura](../architecture/overview.md)
- [Decisiones de diseño](../architecture/design-decisions.md)
