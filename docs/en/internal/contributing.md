# Contributing to Vali-Flow

This document is for contributors working on the Vali-Flow packages: adding features, fixing bugs, writing tests, or improving documentation.

---

## Repository layout

```
Vali-Flow/                      ← this repo
├── Vali-Flow/                  EF Core evaluator
├── Vali-Flow.InMemory/         In-memory evaluator
├── Vali-Flow.Sql/              SQL translator + builders
├── Vali-Flow.NoSql/            Shared IR + visitor base
├── Vali-Flow.NoSql.MongoDB/    MongoDB adapter
├── Vali-Flow.NoSql.DynamoDB/   DynamoDB adapter
├── Vali-Flow.NoSql.Elasticsearch/  Elasticsearch adapter
├── Vali-Flow.NoSql.Redis/      Redis adapter
├── Vali-Flow.Abstractions/     Shared interfaces
├── Vali-Flow.Benchmarks/       BenchmarkDotNet benchmarks
├── Vali-Flow.Tests/            EF Core evaluator tests
├── Vali-Flow.InMemory.Tests/   InMemory evaluator tests
├── Vali-Flow.Sql.Tests/        SQL translator tests
├── Vali-Flow.NoSql.Tests/      NoSQL adapter tests
├── Vali-Flow.NoSql.DynamoDB.Tests/  DynamoDB tests
└── docs/                       Documentation (en/ + es/)

Vali-Flow.Core/                 ← separate repo
  /Users/feliperafaelmontenegro/RiderProjects/Vali-Flow.Core
```

`Vali-Flow.Core` is maintained in a separate repository. Changes to Core are made there; changes to the evaluators and translators are made here.

---

## Prerequisites

- .NET 8 SDK or .NET 9 SDK
- Rider or Visual Studio (or VS Code with C# DevKit)
- `Vali-Flow.Core` cloned at `/Users/feliperafaelmontenegro/RiderProjects/Vali-Flow.Core` (the local `ProjectReference` path)

---

## Build

```bash
# Build the full solution
dotnet build

# Build in Release (used before packing)
dotnet build --configuration Release
```

---

## Run tests

```bash
# All test projects
dotnet test

# Specific project
dotnet test Vali-Flow.Sql.Tests/

# With verbosity
dotnet test --logger "console;verbosity=detailed"
```

Test projects use **xUnit** and **FluentAssertions**.

---

## Pack a NuGet package

```bash
dotnet pack Vali-Flow/Vali-Flow.csproj --configuration Release
dotnet pack Vali-Flow.InMemory/Vali-Flow.InMemory.csproj --configuration Release
dotnet pack Vali-Flow.Sql/Vali-Flow.Sql.csproj --configuration Release
```

The `.nupkg` files land in `<project>/bin/Release/`.

---

## Adding a new ValiFlow<T> method (Core)

New filter methods (e.g., a new string operator) go in `Vali-Flow.Core`. The process:

1. Add the new IR node to `Vali-Flow.Core/IR/` (if a new node type is needed).
2. Add the fluent method to `ValiFlow<T>` that creates the node.
3. Update `ExpressionToIRVisitor` in `Vali-Flow.NoSql` to handle the new node.
4. Update each NoSQL adapter's translator to emit the corresponding native query.
5. Update the SQL `ExpressionToSqlVisitor` to emit the SQL fragment.
6. Add tests in all affected test projects.
7. Update documentation in `docs/en/` and `docs/es/`.

---

## Adding a new SQL dialect

1. Create a class implementing `ISqlDialect` in `Vali-Flow.Sql/Dialects/`.
2. Implement:
   - `QuoteIdentifier(string name)` — return the dialect-specific quoted form.
   - `ParameterPrefix` — return `"@"`, `":"`, etc.
   - `CaseInsensitiveLike` — return the operator or function for case-insensitive LIKE.
3. Add tests in `Vali-Flow.Sql.Tests/`.
4. Document in `docs/en/vali-flow-sql.md` and `docs/es/vali-flow-sql.md`.

---

## Adding a new NoSQL adapter

1. Create a new project `Vali-Flow.NoSql.<Technology>/`.
2. Reference `Vali-Flow.NoSql` (for the IR base) and the technology's official NuGet client.
3. Implement a `<Technology>FilterTranslator` that walks `IRNode` and emits the native query.
4. Expose an extension method on `ValiFlow<T>` and on `Expression<Func<T, bool>>`.
5. Add a test project `Vali-Flow.NoSql.<Technology>.Tests/`.
6. Add documentation in `docs/en/` and `docs/es/`.

---

## Documentation

Documentation lives in `docs/en/` (English) and `docs/es/` (Spanish). Both languages must be kept in sync.

- Reference docs for each package live in `docs/{lang}/vali-flow-<package>.md`.
- Guides live in `docs/{lang}/guides/`.
- Architecture docs live in `docs/{lang}/architecture/`.
- Internal contributor docs live in `docs/{lang}/internal/`.

When you add or change a feature, update both `en/` and `es/` docs in the same commit.

---

## Benchmarks

`Vali-Flow.Benchmarks` uses BenchmarkDotNet. Run benchmarks in Release mode:

```bash
cd Vali-Flow.Benchmarks
dotnet run --configuration Release
```

Results land in `BenchmarkDotNet.Artifacts/`. Do not commit benchmark results.

---

## Commit style

- Use conventional commits: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`.
- Keep commits focused: one logical change per commit.
- Reference issue numbers in the commit body when applicable.

---

## Core version management

The local `ProjectReference` to Core must stay consistent across all package `.csproj` files. When Core is published to NuGet:

1. Replace every `<ProjectReference Include="...Vali-Flow.Core.csproj" />` with:
   ```xml
   <PackageReference Include="Vali-Flow.Core" Version="2.6.0" />
   ```
2. Verify all projects build and all tests pass.
3. Bump the version of each package that changes.

---

## See also

- [Architecture overview](../architecture/overview.md)
- [Design decisions](../architecture/design-decisions.md)
