# Vali-Flow — Design Decisions

This document records the significant architectural choices made in the Vali-Flow ecosystem, the reasoning behind each, and the trade-offs accepted.

---

## 1. Expression trees as the DSL, not strings

**Decision:** Use C# expression trees (`Expression<Func<T, bool>>`) as the filter representation rather than a string-based query language or a dictionary of conditions.

**Rationale:**
- Compile-time type safety: the compiler catches misspelled property names and type mismatches at build time, not at runtime.
- Refactoring support: renaming a property propagates correctly in IDEs.
- Composable: `ValiFlow<T>` conditions are combinable with `.And()`, `.Or()`, `.Not()` without string concatenation.

**Trade-off:** Expression trees are more complex to inspect and serialize than strings. The IR layer (see §3) exists specifically to bridge this gap for NoSQL backends.

---

## 2. Specification pattern for EF Core

**Decision:** Query criteria are expressed as `BasicSpecification<T>` and `QuerySpecification<T>` objects rather than passing raw LINQ expressions directly to evaluator methods.

**Rationale:**
- Decouples "what to query" from "how to execute it". A spec can be built in a domain service and passed to an infrastructure evaluator.
- Carries not just the filter but also includes, ordering, pagination, and EF hints — a single handoff object.
- Testable: specs are plain objects that can be instantiated in unit tests without a DbContext.

**Trade-off:** One more abstraction layer. For simple queries, building a full spec can feel verbose. The InMemory evaluator accepts raw `ValiFlow<T>` instances directly to ease this.

---

## 3. Intermediate Representation (IR) for NoSQL adapters

**Decision:** NoSQL adapters do not walk the raw expression tree directly — they first convert it to a tree of simple IR nodes (sealed records: `EqualNode`, `ComparisonNode`, `LikeNode`, `InNode`, `NullNode`, `AndNode`, `OrNode`, `NotNode`) using `ExpressionToIRVisitor`.

**Rationale:**
- Expression trees contain C# compiler artifacts (closures, lifted nullables, constant folding) that are hard to map 1:1 to NoSQL query languages.
- IR nodes are simple and well-defined — adapters translate IR, not raw expression trees, making each adapter 100–200 lines instead of 500+.
- IR nodes are immutable sealed records — safe to cache and share.

**Trade-off:** A two-step translation (expression → IR → native query) adds a visit pass. The overhead is negligible at runtime (the IR tree is tiny) but adds a conceptual layer for contributors.

---

## 4. No execution concern in translators

**Decision:** `ValiFlow<T>`, the SQL translator, and all NoSQL adapters are pure transformers. They do not hold connections, execute queries, or manage state.

**Rationale:**
- Single Responsibility: a translator translates; a driver executes.
- Testable in isolation: the entire translator can be tested by asserting on the output string/document without a real database.
- Composable: output from the translator can be combined with manually-written fragments.

**Trade-off:** Users must wire the output to a driver themselves (a few lines of code). This is explicitly chosen over convenience wrappers that would create tight coupling to a specific driver version.

---

## 5. Dialect objects, not flags

**Decision:** SQL dialect differences (quoting, parameter prefix, case-insensitive operators) are encoded in `ISqlDialect` implementations rather than in a dialect enum or a configuration dictionary.

**Rationale:**
- Open/Closed principle: adding a new dialect requires only a new class, not changes to the translator.
- Testable: each dialect is a plain object that can be instantiated in tests.
- Composable: applications can create custom dialects for unusual environments.

**Built-in dialects:** `SqlServerDialect`, `PostgreSqlDialect`, `MySqlDialect`, `SqliteDialect`, `OracleDialect`.

---

## 6. Synchronous InMemory evaluator

**Decision:** `Vali-Flow.InMemory` is entirely synchronous — no `Task<T>` return types on any method.

**Rationale:**
- In-memory operations are CPU-bound and complete instantly. Wrapping them in `Task.FromResult` adds noise without value.
- Test code reads more naturally without `await` everywhere.
- Matches the EF Core evaluator's method names so the same specification objects work in both contexts.

**Trade-off:** Production code that calls the InMemory evaluator through an abstraction must expose synchronous or overloaded methods. For async-only interfaces, callers wrap with `Task.FromResult`.

---

## 7. `negateCondition` parameter instead of a `.Not()` wrapper

**Decision:** Evaluator read methods accept a `bool negateCondition` parameter that inverts the filter, rather than requiring callers to wrap the filter in a `ValiFlow<T>.Not()` call.

**Rationale:**
- Convenience: querying "failed" records (those that do not match a spec) is a common use case. `negateCondition: true` costs one parameter flip instead of building a new spec.
- Avoids spec duplication: the same spec object is reused for both passing and failing queries.

**Trade-off:** A boolean parameter is less discoverable than a named method. `EvaluateQueryFailedAsync` and `EvaluateGetFirstFailedAsync` are convenience wrappers that set `negateCondition: true` internally for the most common cases.

---

## 8. Bulk operations via EFCore.BulkExtensions

**Decision:** `BulkInsertAsync`, `BulkUpdateAsync`, `BulkDeleteAsync`, and `BulkInsertOrUpdateAsync` delegate to `EFCore.BulkExtensions` rather than implementing a custom bulk mechanism.

**Rationale:**
- EFCore.BulkExtensions is the de-facto standard for EF Core bulk operations, battle-tested across SQL Server, PostgreSQL, SQLite, and MySQL.
- Implementing correct bulk operations (batching, identity output, generated columns) for multiple databases is a large surface area that is not Vali-Flow's concern.
- Callers can pass a `BulkConfig` to control all EFCore.BulkExtensions settings.

**Trade-off:** Takes a dependency on EFCore.BulkExtensions. Applications that do not use bulk operations still pull the dependency.

---

## 9. `ValiSort<T>` for dynamic ordering

**Decision:** Dynamic ordering (ordering by a property name known only at runtime) is provided by `ValiSort<T>` from Core rather than by reflection-based LINQ OrderBy helpers.

**Rationale:**
- Centralizes the ordering logic in Core where it can be used by all packages.
- `ValiSort<T>` compiles the ordering key selector once, avoiding repeated reflection calls.

**Trade-off:** Dynamic ordering by a runtime string requires the property to be mapped in the sort configuration. Unmapped properties throw at runtime — this is by design to surface misconfigurations early.

---

## 10. Local ProjectReference for Core during development

**Decision:** All packages reference `Vali-Flow.Core` via a local `ProjectReference` rather than a NuGet reference during active development.

**Rationale:**
- Core and the packages are iterated together. A local reference allows changes to Core to be picked up immediately without publishing.
- Avoids version skew between Core and the packages during development cycles.

**Migration path:** Once Core v2.6.0 is published to NuGet, all `ProjectReference` entries will be replaced with `<PackageReference Include="Vali-Flow.Core" Version="2.6.0" />`.

---

## See also

- [Architecture overview](overview.md)
- [Contributing guide](../internal/contributing.md)
