# Release Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bump all 12 Vali-Flow package versions, revert the 11 temporary `ProjectReference`s back to `PackageReference`, and add `.github/workflows/ci.yml` + `.github/workflows/release.yml` replicating the proven `Vali-Flow.Core`/`Vali-Mediator` pattern extended to this repo's 3-tier dependency chain.

**Architecture:** Two-stage mechanical prep (version bumps, then reference reverts, in dependency order so nothing ever points at a version that doesn't exist yet) followed by two new workflow files that reuse the exact job shapes already proven in `Vali-Flow.Core`. No application code changes — this plan only touches `.csproj` metadata and adds CI/CD config.

**Tech Stack:** .NET SDK 9.0.3 (net8.0/net9.0 multi-target), GitHub Actions, `dotnet pack`/`dotnet nuget push`, Python 3 (inline, for the coverage-merge and package-verification scripts — same tool the reference pattern already uses).

**Spec:** `docs/superpowers/specs/2026-10-06-release-pipeline-design.md`

## Global Constraints

- Coverage gate: **95% line coverage per package**, enforced by `ci.yml`'s `coverage` job. Starting gate, not an audited baseline for the 10 packages not measured this session.
- Every package must multi-target `net8.0;net9.0` (already true for all 12 — do not change `TargetFrameworks`).
- No breaking changes in this plan — every version bump is **minor** (new/changed major version is out of scope; if a task's diff looks breaking, stop and flag it instead of bumping differently).
- Never create the GitHub `nuget` environment or its `NUGET_API_KEY` secret — that's the user's manual prerequisite, out of scope for every task below.
- Every `.csproj` edit must preserve the file's existing indentation style (some use 4 spaces, some use 2 — match whatever the file already uses, don't reformat the whole file).
- No task in this plan runs `git push`, triggers the real GitHub Actions workflow, or runs `dotnet nuget push` — this plan stops at a local, verified dry run.

---

### Task 1: Bump `Vali-Flow.Abstractions` to 1.2.0 (wave 1)

**Files:**
- Modify: `Vali-Flow.Abstractions/Vali-Flow.Abstractions.csproj`

**Interfaces:**
- Consumes: nothing from other tasks.
- Produces: `Vali-Flow.Abstractions` version `1.2.0`, which Tasks 2 and 4 depend on by exact string.

- [ ] **Step 1: Confirm the current state before editing**

Run: `grep -n "<Version>" "Vali-Flow.Abstractions/Vali-Flow.Abstractions.csproj"`
Expected output: `17:        <Version>1.1.0</Version>`

- [ ] **Step 2: Bump the version and add a release note**

In `Vali-Flow.Abstractions/Vali-Flow.Abstractions.csproj`, replace:

```xml
        <Version>1.1.0</Version>
```

with:

```xml
        <Version>1.2.0</Version>
```

And replace:

```xml
        <PackageReleaseNotes>v1.1.0 — Enhanced documentation and internal contracts alignment with ecosystem refactor.</PackageReleaseNotes>
```

with:

```xml
        <PackageReleaseNotes>v1.2.0 — Add ValiFlowDiagnostics: a shared ActivitySource-based observability helper (StartActivity/RecordException) used across every Vali-Flow package for Activity-based tracing. No breaking changes.

v1.1.0 — Enhanced documentation and internal contracts alignment with ecosystem refactor.</PackageReleaseNotes>
```

- [ ] **Step 3: Verify the package builds and packs at the new version**

Run: `dotnet pack Vali-Flow.Abstractions/Vali-Flow.Abstractions.csproj --configuration Release -o /tmp/pack-check`
Expected: `Compilación correcta` (or `Build succeeded`), and `/tmp/pack-check/Vali-Flow.Abstractions.1.2.0.nupkg` exists.

Run: `ls /tmp/pack-check/Vali-Flow.Abstractions.1.2.0.nupkg`
Expected: the file is listed (no "No such file" error).

- [ ] **Step 4: Commit**

```bash
git add Vali-Flow.Abstractions/Vali-Flow.Abstractions.csproj
git commit -m "chore(abstractions): bump version to 1.2.0 for release"
```

---

### Task 2: Bump and re-point the 4 wave-2 packages (`Vali-Flow`, `Vali-Flow.InMemory`, `Vali-Flow.Sql`, `Vali-Flow.NoSql`)

**Files:**
- Modify: `Vali-Flow/Vali-Flow.csproj`
- Modify: `Vali-Flow.InMemory/Vali-Flow.InMemory.csproj`
- Modify: `Vali-Flow.Sql/Vali-Flow.Sql.csproj`
- Modify: `Vali-Flow.NoSql/Vali-Flow.NoSql.csproj`

**Interfaces:**
- Consumes: `Vali-Flow.Abstractions` version `1.2.0` (from Task 1) — every file below must pin exactly this string in its `PackageReference`.
- Produces: `Vali-Flow` 1.4.0, `Vali-Flow.InMemory` 1.2.0, `Vali-Flow.Sql` 1.2.0, `Vali-Flow.NoSql` 1.1.1 — `Vali-Flow.NoSql`'s version is consumed by Task 3.

Each of the 4 files gets the same two changes: (a) swap the `ProjectReference` to `Vali-Flow.Abstractions` back to a `PackageReference` pinned at `1.2.0`, (b) bump its own `<Version>` + add a release note. Do all 4 in this one task since they share the same dependency edge and are meaningless to test independently (none of them compile standalone against the old `Vali-Flow.Abstractions` NuGet package — that package doesn't have `ValiFlowDiagnostics` yet, which is exactly why this task exists).

- [ ] **Step 1: `Vali-Flow/Vali-Flow.csproj` — re-point to PackageReference and bump version**

Replace:

```xml
        <Version>1.3.4</Version>
```

with:

```xml
        <Version>1.4.0</Version>
```

Replace:

```xml
        <PackageReleaseNotes>v1.3.4 — Updated Vali-Flow.Core dependency to 2.0.2 (fixes IsNull/NotNull WHERE 0=1 bug with EF Core GlobalQueryFilter). No API changes.
```

with:

```xml
        <PackageReleaseNotes>v1.4.0 — Add GenericRepository&lt;T,TKey&gt; (GetById/GetAll/GetPaged/Add/Update/Delete/SaveChanges, wraps ValiFlowEvaluator&lt;T&gt;). Instrument read/write evaluator methods with ValiFlowDiagnostics Activity tracing. Fix UpsertRangeAsync duplicate-key bug and a PK-overwrite bug in upsert-by-business-key scenarios. Serialize concurrent BulkInsertOrUpdateAsync/BulkUpdateAsync calls per entity type with a generic transient-failure retry (SQL Server/PostgreSQL/MySQL/SQLite). No breaking changes.

v1.3.4 — Updated Vali-Flow.Core dependency to 2.0.2 (fixes IsNull/NotNull WHERE 0=1 bug with EF Core GlobalQueryFilter). No API changes.
```

Replace:

```xml
        <PackageReference Include="Vali-Flow.Core" Version="2.0.2" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Vali-Flow.Abstractions\Vali-Flow.Abstractions.csproj" />
    </ItemGroup>
```

with:

```xml
        <PackageReference Include="Vali-Flow.Core" Version="2.0.2" />
        <PackageReference Include="Vali-Flow.Abstractions" Version="1.2.0" />
    </ItemGroup>
```

- [ ] **Step 2: `Vali-Flow.InMemory/Vali-Flow.InMemory.csproj` — re-point to PackageReference and bump version**

Replace:

```xml
        <Version>1.1.5</Version>
```

with:

```xml
        <Version>1.2.0</Version>
```

Replace:

```xml
        <PackageReleaseNotes>v1.1.5 — Updated Vali-Flow.Core dependency to 2.0.2 (fixes IsNull/NotNull WHERE 0=1 bug with EF Core GlobalQueryFilter). No API changes.
```

with:

```xml
        <PackageReleaseNotes>v1.2.0 — Fix Update(entity, externalList) leaking into the internal store on a later bare SaveChanges() call. Split ValiFlowEvaluator&lt;T,TProperty&gt; into partial files (Bridge/Read/Write/Grouped). Add an optional tag parameter across read/write/grouped methods, wired to ValiFlowDiagnostics Activity tracing. No breaking changes.

v1.1.5 — Updated Vali-Flow.Core dependency to 2.0.2 (fixes IsNull/NotNull WHERE 0=1 bug with EF Core GlobalQueryFilter). No API changes.
```

Replace:

```xml
    <ItemGroup>
        <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
        <PackageReference Include="Vali-Flow.Core" Version="2.0.2" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Vali-Flow.Abstractions\Vali-Flow.Abstractions.csproj" />
    </ItemGroup>
```

with:

```xml
    <ItemGroup>
        <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
        <PackageReference Include="Vali-Flow.Core" Version="2.0.2" />
        <PackageReference Include="Vali-Flow.Abstractions" Version="1.2.0" />
    </ItemGroup>
```

- [ ] **Step 3: `Vali-Flow.Sql/Vali-Flow.Sql.csproj` — re-point to PackageReference and bump version**

Replace:

```xml
        <Version>1.1.1</Version>
```

with:

```xml
        <Version>1.2.0</Version>
```

Replace:

```xml
        <PackageReleaseNotes>v1.1.1 — Updated Vali-Flow.Core dependency to 2.0.2 (fixes IsNull/NotNull WHERE 0=1 bug with EF Core GlobalQueryFilter). No API changes.
```

with:

```xml
        <PackageReleaseNotes>v1.2.0 — Add SqlIdentifierGuard (regex whitelist validation for table/schema/type names) to every builder, fixing a SQL injection vector. Add SetAllFrom(entity, exclude...) reflection-based bulk column mapper to Insert/Update builders. Fix OrIgnore/OrReplace dialect guard, Set()-after-SelectFrom() silent data loss, and Where()-called-twice silent overwrite bugs. Instrument all 6 builders' Build() with ValiFlowDiagnostics Activity tracing. No breaking changes.

v1.1.1 — Updated Vali-Flow.Core dependency to 2.0.2 (fixes IsNull/NotNull WHERE 0=1 bug with EF Core GlobalQueryFilter). No API changes.
```

Replace:

```xml
    <ItemGroup>
        <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
        <PackageReference Include="Vali-Flow.Core" Version="2.0.2" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Vali-Flow.Abstractions\Vali-Flow.Abstractions.csproj" />
    </ItemGroup>
```

with:

```xml
    <ItemGroup>
        <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
        <PackageReference Include="Vali-Flow.Core" Version="2.0.2" />
        <PackageReference Include="Vali-Flow.Abstractions" Version="1.2.0" />
    </ItemGroup>
```

- [ ] **Step 4: `Vali-Flow.NoSql/Vali-Flow.NoSql.csproj` — re-point to PackageReference and bump version**

Replace:

```xml
    <Version>1.1.0</Version>
```

with:

```xml
    <Version>1.1.1</Version>
```

Replace:

```xml
    <PackageReleaseNotes>v1.1.0 — Enhanced documentation and contracts alignment, improved adapter integration pattern.</PackageReleaseNotes>
```

with:

```xml
    <PackageReleaseNotes>v1.1.1 — Fix: x.Field == nullVariable (a closure-captured null) now translates to NullNode like the x.Field == null literal path instead of throwing ArgumentNullException. No breaking changes.</PackageReleaseNotes>
```

Replace:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
    <PackageReference Include="Vali-Flow.Core" Version="2.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Vali-Flow.Abstractions\Vali-Flow.Abstractions.csproj" />
  </ItemGroup>
```

with:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
    <PackageReference Include="Vali-Flow.Core" Version="2.0.0" />
    <PackageReference Include="Vali-Flow.Abstractions" Version="1.2.0" />
  </ItemGroup>
```

- [ ] **Step 5: Verify all 4 packages build and pack against the real `Vali-Flow.Abstractions` 1.2.0 NuGet package**

This is the real test of this task: these 4 `.csproj` now declare a `PackageReference` to `Vali-Flow.Abstractions` `1.2.0`, but that version is **not yet on NuGet.org** (Task 1 only packed it locally, it was never pushed — this plan never pushes). `dotnet restore`/`dotnet pack` will fail to resolve that package unless it's available from a local source. Add the local pack output from Task 1 as a NuGet source for this verification only:

Run: `dotnet nuget add source /tmp/pack-check --name local-verify-only 2>&1 || true`
(The `|| true` tolerates "source already exists" if this step is re-run.)

Run: `dotnet pack Vali-Flow/Vali-Flow.csproj --configuration Release -o /tmp/pack-check`
Expected: `Compilación correcta`, `/tmp/pack-check/Vali-Flow.1.4.0.nupkg` exists.

Run: `dotnet pack Vali-Flow.InMemory/Vali-Flow.InMemory.csproj --configuration Release -o /tmp/pack-check`
Expected: `Compilación correcta`, `/tmp/pack-check/Vali-Flow.InMemory.1.2.0.nupkg` exists.

Run: `dotnet pack Vali-Flow.Sql/Vali-Flow.Sql.csproj --configuration Release -o /tmp/pack-check`
Expected: `Compilación correcta`, `/tmp/pack-check/Vali-Flow.Sql.1.2.0.nupkg` exists.

Run: `dotnet pack Vali-Flow.NoSql/Vali-Flow.NoSql.csproj --configuration Release -o /tmp/pack-check`
Expected: `Compilación correcta`, `/tmp/pack-check/Vali-Flow.NoSql.1.1.1.nupkg` exists.

Run: `dotnet nuget remove source local-verify-only`
(Cleanup — this local source must not linger in the machine's global NuGet config after this task.)

- [ ] **Step 6: Commit**

```bash
git add Vali-Flow/Vali-Flow.csproj Vali-Flow.InMemory/Vali-Flow.InMemory.csproj Vali-Flow.Sql/Vali-Flow.Sql.csproj Vali-Flow.NoSql/Vali-Flow.NoSql.csproj
git commit -m "chore(wave-2): bump versions and re-point Vali-Flow.Abstractions to PackageReference 1.2.0"
```

---

### Task 3: Bump and re-point the 7 wave-3 NoSQL provider packages

**Files:**
- Modify: `Vali-Flow.NoSql.MongoDB/Vali-Flow.NoSql.MongoDB.csproj`
- Modify: `Vali-Flow.NoSql.Elasticsearch/Vali-Flow.NoSql.Elasticsearch.csproj`
- Modify: `Vali-Flow.NoSql.Redis/Vali-Flow.NoSql.Redis.csproj`
- Modify: `Vali-Flow.NoSql.DynamoDB/Vali-Flow.NoSql.DynamoDB.csproj`
- Modify: `Vali-Flow.NoSql.Couchbase/Vali-Flow.NoSql.Couchbase.csproj`
- Modify: `Vali-Flow.NoSql.CosmosDb/Vali-Flow.NoSql.CosmosDb.csproj`
- Modify: `Vali-Flow.NoSql.Firestore/Vali-Flow.NoSql.Firestore.csproj`

**Interfaces:**
- Consumes: `Vali-Flow.NoSql` version `1.1.1` (from Task 2) — every file below must pin exactly this string.
- Produces: 7 packages ready for wave 3 of `release.yml` (Task 5).

- [ ] **Step 1: `Vali-Flow.NoSql.MongoDB` — bump version and re-point**

Replace:

```xml
    <Version>1.1.0</Version>
```

with:

```xml
    <Version>1.2.0</Version>
```

Replace:

```xml
    <PackageReleaseNotes>v1.1.0 — Enhanced documentation and adapter integration pattern alignment.</PackageReleaseNotes>
```

with:

```xml
    <PackageReleaseNotes>v1.2.0 — Add a MaxInValues cap (10,000) to InNode translation, a conservative estimate to avoid unbounded $in clauses. Instrument Translate(...) with ValiFlowDiagnostics Activity tracing (tag + entityType). No breaking changes.</PackageReleaseNotes>
```

Replace:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
    <PackageReference Include="MongoDB.Bson" Version="2.29.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Vali-Flow.NoSql\Vali-Flow.NoSql.csproj" />
  </ItemGroup>
```

with:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
    <PackageReference Include="MongoDB.Bson" Version="2.29.0" />
    <PackageReference Include="Vali-Flow.NoSql" Version="1.1.1" />
  </ItemGroup>
```

- [ ] **Step 2: `Vali-Flow.NoSql.Elasticsearch` — bump version and re-point**

Replace:

```xml
    <Version>1.1.0</Version>
```

with:

```xml
    <Version>1.2.0</Version>
```

Replace:

```xml
    <PackageReleaseNotes>v1.1.0 — Enhanced documentation and adapter integration pattern alignment.</PackageReleaseNotes>
```

with:

```xml
    <PackageReleaseNotes>v1.2.0 — Fix a swallowed inner exception in ToDouble (now wrapped with context instead of lost). Add a MaxInValues cap (65,536, matching index.max_terms_count). Instrument Translate(...) with ValiFlowDiagnostics Activity tracing. No breaking changes.</PackageReleaseNotes>
```

Replace:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
    <PackageReference Include="Elastic.Clients.Elasticsearch" Version="8.15.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Vali-Flow.NoSql\Vali-Flow.NoSql.csproj" />
  </ItemGroup>
```

with:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
    <PackageReference Include="Elastic.Clients.Elasticsearch" Version="8.15.0" />
    <PackageReference Include="Vali-Flow.NoSql" Version="1.1.1" />
  </ItemGroup>
```

- [ ] **Step 3: `Vali-Flow.NoSql.Redis` — bump version and re-point**

Replace:

```xml
    <Version>1.1.0</Version>
```

with:

```xml
    <Version>1.2.0</Version>
```

Replace:

```xml
    <PackageReleaseNotes>v1.1.0 — Enhanced documentation and adapter integration pattern alignment.</PackageReleaseNotes>
```

with:

```xml
    <PackageReleaseNotes>v1.2.0 — Security fix: escape RediSearch query-syntax special characters in LIKE patterns before adding wildcards, closing a query-injection vector (e.g. a pattern containing ')' or '|' could previously break out of the field's scope). Fix customConverter not being applied in VisitComparison/numeric VisitIn. Add a MaxInValues cap (1,000). Instrument Translate(...) with ValiFlowDiagnostics Activity tracing. No breaking changes.</PackageReleaseNotes>
```

Replace:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
    <PackageReference Include="NRedisStack" Version="1.3.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Vali-Flow.NoSql\Vali-Flow.NoSql.csproj" />
  </ItemGroup>
```

with:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
    <PackageReference Include="NRedisStack" Version="1.3.0" />
    <PackageReference Include="Vali-Flow.NoSql" Version="1.1.1" />
  </ItemGroup>
```

- [ ] **Step 4: `Vali-Flow.NoSql.DynamoDB` — bump version and re-point**

Replace:

```xml
    <Version>1.1.0</Version>
```

with:

```xml
    <Version>1.2.0</Version>
```

Replace:

```xml
    <PackageReleaseNotes>v1.1.0 — Enhanced documentation and adapter integration pattern alignment.</PackageReleaseNotes>
```

with:

```xml
    <PackageReleaseNotes>v1.2.0 — Add a MaxInValues cap. Instrument Translate(...) with ValiFlowDiagnostics Activity tracing (tag + entityType). No breaking changes.</PackageReleaseNotes>
```

Replace:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
    <PackageReference Include="AWSSDK.DynamoDBv2" Version="3.7.406.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Vali-Flow.NoSql\Vali-Flow.NoSql.csproj" />
  </ItemGroup>
```

with:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
    <PackageReference Include="AWSSDK.DynamoDBv2" Version="3.7.406.1" />
    <PackageReference Include="Vali-Flow.NoSql" Version="1.1.1" />
  </ItemGroup>
```

- [ ] **Step 5: `Vali-Flow.NoSql.Couchbase` — bump version and re-point**

Replace:

```xml
    <Version>1.0.0</Version>
```

with:

```xml
    <Version>1.1.0</Version>
```

Replace:

```xml
    <PackageReleaseNotes>v1.0.0 — Initial release. N1QL (SQL++) WHERE clause translator.</PackageReleaseNotes>
```

with:

```xml
    <PackageReleaseNotes>v1.1.0 — Fix: decimal values were bound as N1QL string parameters instead of native numbers, making every comparison operator (=, &lt;&gt;, &gt;, &gt;=, &lt;, &lt;=) against a decimal field silently match zero rows (N1QL does not coerce between string and number). Add a MaxInValues cap. Instrument Translate(...) with ValiFlowDiagnostics Activity tracing. No breaking changes.</PackageReleaseNotes>
```

Replace:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Vali-Flow.NoSql\Vali-Flow.NoSql.csproj" />
  </ItemGroup>
```

with:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
    <PackageReference Include="Vali-Flow.NoSql" Version="1.1.1" />
  </ItemGroup>
```

- [ ] **Step 6: `Vali-Flow.NoSql.CosmosDb` — bump version and re-point**

Replace:

```xml
    <Version>1.0.0</Version>
```

with:

```xml
    <Version>1.1.0</Version>
```

Replace:

```xml
    <PackageReleaseNotes>v1.0.0 — Initial release. Cosmos DB SQL API WHERE clause translator.</PackageReleaseNotes>
```

with:

```xml
    <PackageReleaseNotes>v1.1.0 — Add a MaxInValues cap. Instrument Translate(...) with ValiFlowDiagnostics Activity tracing. No breaking changes.</PackageReleaseNotes>
```

Replace:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Vali-Flow.NoSql\Vali-Flow.NoSql.csproj" />
  </ItemGroup>
```

with:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
    <PackageReference Include="Vali-Flow.NoSql" Version="1.1.1" />
  </ItemGroup>
```

- [ ] **Step 7: `Vali-Flow.NoSql.Firestore` — bump version and re-point**

Replace:

```xml
    <Version>1.0.0</Version>
```

with:

```xml
    <Version>1.1.0</Version>
```

Replace:

```xml
    <PackageReleaseNotes>v1.0.0 — Initial release. Firestore native Filter translator.</PackageReleaseNotes>
```

with:

```xml
    <PackageReleaseNotes>v1.1.0 — Instrument Translate(...) with ValiFlowDiagnostics Activity tracing (tag + entityType). No breaking changes.</PackageReleaseNotes>
```

Replace:

```xml
  <ItemGroup>
    <PackageReference Include="Google.Cloud.Firestore" Version="4.4.0" />
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Vali-Flow.NoSql\Vali-Flow.NoSql.csproj" />
  </ItemGroup>
```

with:

```xml
  <ItemGroup>
    <PackageReference Include="Google.Cloud.Firestore" Version="4.4.0" />
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
    <PackageReference Include="Vali-Flow.NoSql" Version="1.1.1" />
  </ItemGroup>
```

- [ ] **Step 8: Verify all 7 providers build and pack against the real `Vali-Flow.NoSql` 1.1.1 NuGet package**

Same technique as Task 2 Step 5 — these 7 `.csproj` now reference `Vali-Flow.NoSql` `1.1.1`, which isn't on NuGet.org yet; point `dotnet` at the local pack output.

Run: `dotnet nuget add source /tmp/pack-check --name local-verify-only 2>&1 || true`

Run each of the following and expect `Compilación correcta` plus the named `.nupkg` present in `/tmp/pack-check`:

```bash
dotnet pack Vali-Flow.NoSql.MongoDB/Vali-Flow.NoSql.MongoDB.csproj --configuration Release -o /tmp/pack-check
dotnet pack Vali-Flow.NoSql.Elasticsearch/Vali-Flow.NoSql.Elasticsearch.csproj --configuration Release -o /tmp/pack-check
dotnet pack Vali-Flow.NoSql.Redis/Vali-Flow.NoSql.Redis.csproj --configuration Release -o /tmp/pack-check
dotnet pack Vali-Flow.NoSql.DynamoDB/Vali-Flow.NoSql.DynamoDB.csproj --configuration Release -o /tmp/pack-check
dotnet pack Vali-Flow.NoSql.Couchbase/Vali-Flow.NoSql.Couchbase.csproj --configuration Release -o /tmp/pack-check
dotnet pack Vali-Flow.NoSql.CosmosDb/Vali-Flow.NoSql.CosmosDb.csproj --configuration Release -o /tmp/pack-check
dotnet pack Vali-Flow.NoSql.Firestore/Vali-Flow.NoSql.Firestore.csproj --configuration Release -o /tmp/pack-check
```

Expected `.nupkg` files: `Vali-Flow.NoSql.MongoDB.1.2.0.nupkg`, `Vali-Flow.NoSql.Elasticsearch.1.2.0.nupkg`, `Vali-Flow.NoSql.Redis.1.2.0.nupkg`, `Vali-Flow.NoSql.DynamoDB.1.2.0.nupkg`, `Vali-Flow.NoSql.Couchbase.1.1.0.nupkg`, `Vali-Flow.NoSql.CosmosDb.1.1.0.nupkg`, `Vali-Flow.NoSql.Firestore.1.1.0.nupkg`.

Run: `dotnet nuget remove source local-verify-only`

- [ ] **Step 9: Commit**

```bash
git add Vali-Flow.NoSql.MongoDB/Vali-Flow.NoSql.MongoDB.csproj Vali-Flow.NoSql.Elasticsearch/Vali-Flow.NoSql.Elasticsearch.csproj Vali-Flow.NoSql.Redis/Vali-Flow.NoSql.Redis.csproj Vali-Flow.NoSql.DynamoDB/Vali-Flow.NoSql.DynamoDB.csproj Vali-Flow.NoSql.Couchbase/Vali-Flow.NoSql.Couchbase.csproj Vali-Flow.NoSql.CosmosDb/Vali-Flow.NoSql.CosmosDb.csproj Vali-Flow.NoSql.Firestore/Vali-Flow.NoSql.Firestore.csproj
git commit -m "chore(wave-3): bump versions and re-point Vali-Flow.NoSql to PackageReference 1.1.1 in the 7 NoSQL providers"
```

---

### Task 4: Create `.github/workflows/ci.yml`

**Files:**
- Create: `.github/workflows/ci.yml`

**Interfaces:**
- Consumes: the 11 test project names and 12 package project names (all real paths in this repo, listed below).
- Produces: a `workflow_call`-able workflow with a `version-suffix` input, used by Task 5's `release.yml` as `uses: ./.github/workflows/ci.yml`.

- [ ] **Step 1: Write `.github/workflows/ci.yml`**

```yaml
name: CI

# Pull requests, pushes to main and reuse from release.yml (workflow_call). Single required check
# for branch protection: "CI OK".
on:
  pull_request:
  push:
    branches: [main]
  workflow_call:
    inputs:
      version-suffix:
        description: Prerelease label appended to every package version (for example rc.1). Empty packs the versions declared in the csproj files.
        type: string
        default: ''

permissions:
  contents: read

concurrency:
  group: ci-${{ github.ref }}
  cancel-in-progress: ${{ github.event_name == 'pull_request' }}

env:
  DOTNET_NOLOGO: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true

jobs:
  build:
    name: Build
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: |
            8.0.x
            9.0.x

      - name: Restore
        run: dotnet restore vali-flow.sln

      - name: Build
        run: dotnet build vali-flow.sln -c Release --no-restore

  test:
    name: test / ${{ matrix.os }} / ${{ matrix.tfm }}
    runs-on: ${{ matrix.os }}
    strategy:
      fail-fast: false
      matrix:
        os: [ubuntu-latest, windows-latest]
        tfm: [net8.0, net9.0]
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: |
            8.0.x
            9.0.x

      - name: Environment
        shell: bash
        run: |
          mkdir -p logs
          {
            echo "### ${{ matrix.os }} / ${{ matrix.tfm }}"
            uname -a || true
            dotnet --info
          } 2>&1 | tee logs/00-environment.log

      - name: Restore
        shell: bash
        run: |
          set -o pipefail
          dotnet restore vali-flow.sln 2>&1 | tee logs/01-restore.log

      - name: Build
        shell: bash
        run: |
          set -o pipefail
          dotnet build vali-flow.sln -c Release --no-restore -v:minimal -bl:logs/build.binlog 2>&1 | tee logs/02-build.log

      # The 11 test projects, one per package family (Vali-Flow.NoSql.Tests also covers the
      # MongoDB translator — there is no separate Vali-Flow.NoSql.MongoDB.Tests project).
      - name: Unit tests with coverage on ${{ matrix.tfm }}
        shell: bash
        run: |
          set -o pipefail
          status=0
          projects="Vali-Flow.Abstractions.Tests Vali-Flow.Tests Vali-Flow.InMemory.Tests Vali-Flow.Sql.Tests Vali-Flow.NoSql.Tests Vali-Flow.NoSql.Elasticsearch.Tests Vali-Flow.NoSql.Redis.Tests Vali-Flow.NoSql.DynamoDB.Tests Vali-Flow.NoSql.Couchbase.Tests Vali-Flow.NoSql.CosmosDb.Tests Vali-Flow.NoSql.Firestore.Tests"
          for project in $projects; do
            echo "::group::$project on ${{ matrix.tfm }}"
            dotnet test "$project" -c Release --no-build -f "${{ matrix.tfm }}" \
              --logger "console;verbosity=detailed" \
              --logger "trx;LogFileName=$project-${{ matrix.tfm }}.trx" \
              --collect:"XPlat Code Coverage" --results-directory "TestResults/${{ matrix.tfm }}/$project" \
              2>&1 | tee "logs/03-test-$project.log" || status=1
            echo "::endgroup::"
          done
          exit $status

      - name: Test totals
        if: always()
        shell: bash
        run: |
          python3 - <<'PY'
          import glob
          import os
          import xml.etree.ElementTree as ET

          rows = ["| Project | Total | Passed | Failed | Skipped |", "|---|---:|---:|---:|---:|"]
          for path in sorted(glob.glob("TestResults/**/*.trx", recursive=True)):
              counters = ET.parse(path).getroot().find(".//{*}Counters")
              if counters is None:
                  continue
              name = os.path.basename(path)[:-4]
              failed = int(counters.get("failed", 0))
              skipped = int(counters.get("total", 0)) - int(counters.get("executed", 0))
              rows.append(f"| {name} | {counters.get('total')} | {counters.get('passed')} | {failed}{' (!)' if failed else ''} | {skipped} |")
          report = "### Tests ${{ matrix.os }} / ${{ matrix.tfm }}\n\n" + "\n".join(rows) + "\n"
          print(report)
          with open(os.environ["GITHUB_STEP_SUMMARY"], "a", encoding="utf-8") as handle:
              handle.write(report + "\n")
          PY

      - name: Upload coverage reports
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: coverage-${{ matrix.os }}-${{ matrix.tfm }}
          path: TestResults/**/coverage.cobertura.xml
          if-no-files-found: error
          retention-days: 7

      - name: Upload full trace
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: trace-${{ matrix.os }}-${{ matrix.tfm }}
          path: |
            logs/**
            TestResults/**/*.trx
          if-no-files-found: warn
          retention-days: 14

  trace:
    name: Full trace (single file)
    needs: test
    if: always()
    runs-on: ubuntu-latest
    steps:
      - uses: actions/download-artifact@v4
        with:
          pattern: trace-*
          path: trace

      - name: Merge every log into one file
        shell: bash
        run: |
          out=full-trace.txt
          : > "$out"
          while IFS= read -r f; do
            rel="${f#trace/}"
            {
              echo
              echo "=================================================================="
              echo "== ${rel%%/*} :: ${rel#*/}"
              echo "=================================================================="
              cat "$f"
            } >> "$out"
          done < <(find trace -name '*.log' | sort)
          wc -l "$out"
          {
            echo "### Full trace"
            echo
            echo "Download the **full-trace** artifact of this run: one text file with the environment, restore, build and"
            echo "detailed test output of every OS x target framework cell."
          } >> "$GITHUB_STEP_SUMMARY"

      - name: Upload the single-file trace
        uses: actions/upload-artifact@v4
        with:
          name: full-trace
          path: full-trace.txt
          if-no-files-found: error
          retention-days: 14

  coverage:
    # NOTE: 95% is a starting gate, not an audited baseline for the 10 packages not measured in the
    # session that stood this pipeline up (Vali-Flow itself was: 97.42%). Adjust per-library after
    # the first green run shows the real numbers.
    name: Coverage gate (>= 95% per library)
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/download-artifact@v4
        with:
          pattern: coverage-*
          path: coverage

      - name: Merge reports and enforce the gate
        env:
          THRESHOLD: '95'
        run: |
          python3 - <<'PY'
          import glob
          import os
          import sys
          import xml.etree.ElementTree as ET

          LIBS = [
              "Vali-Flow.Abstractions", "Vali-Flow", "Vali-Flow.InMemory", "Vali-Flow.Sql",
              "Vali-Flow.NoSql", "Vali-Flow.NoSql.MongoDB", "Vali-Flow.NoSql.Elasticsearch",
              "Vali-Flow.NoSql.Redis", "Vali-Flow.NoSql.DynamoDB", "Vali-Flow.NoSql.Couchbase",
              "Vali-Flow.NoSql.CosmosDb", "Vali-Flow.NoSql.Firestore",
          ]
          THRESHOLD = float(os.environ["THRESHOLD"])
          OSES = ["ubuntu-latest", "windows-latest"]
          TFMS = ["net8.0", "net9.0"]

          missing = [
              f"coverage-{os_name}-{tfm}"
              for os_name in OSES
              for tfm in TFMS
              if not glob.glob(f"coverage/coverage-{os_name}-{tfm}/**/coverage.cobertura.xml", recursive=True)
          ]

          lines = {lib: {} for lib in LIBS}
          reports = glob.glob("coverage/**/coverage.cobertura.xml", recursive=True)
          for path in reports:
              for package in ET.parse(path).getroot().iter("package"):
                  store = lines.get(package.get("name"))
                  if store is None:
                      continue
                  for cls in package.iter("class"):
                      parts = cls.get("filename").replace("\\", "/").split("/")
                      file_key = "/".join(parts[-2:])
                      for line in cls.iter("line"):
                          key = (file_key, int(line.get("number")))
                          store[key] = max(store.get(key, 0), int(line.get("hits")))

          rows = ["| Library | Lines | Covered | Line coverage | Gate |", "|---|---:|---:|---:|:---:|"]
          failed = []
          for lib in LIBS:
              total = len(lines[lib])
              covered = sum(1 for hits in lines[lib].values() if hits > 0)
              pct = 100.0 * covered / total if total else 0.0
              ok = total > 0 and pct >= THRESHOLD
              if not ok:
                  failed.append(lib)
              rows.append(f"| {lib} | {total} | {covered} | {pct:.1f}% | {'pass' if ok else 'FAIL'} |")

          cells = ", ".join(f"{o} / {t}" for o in OSES for t in TFMS)
          report = (f"### Unit-test line coverage (gate >= {THRESHOLD:g}%, {len(reports)} reports merged)\n\n"
                    + "\n".join(rows)
                    + f"\n\nExpected cells: {cells}\n"
                    + (f"\nMissing reports: {', '.join(missing)}\n" if missing else "\nAll 4 OS x TFM reports present.\n"))
          print(report)
          with open(os.environ["GITHUB_STEP_SUMMARY"], "a", encoding="utf-8") as handle:
              handle.write(report + "\n")
          if missing:
              sys.exit("Coverage gate failed: missing reports for " + ", ".join(missing))
          if failed:
              sys.exit("Coverage gate failed for: " + ", ".join(failed))
          PY

  pack:
    name: Pack NuGet packages
    needs: build
    runs-on: ubuntu-latest
    env:
      VERSION_SUFFIX: ${{ inputs.version-suffix }}
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: |
            8.0.x
            9.0.x

      - name: Apply the prerelease label
        if: ${{ inputs.version-suffix != '' }}
        run: |
          if ! [[ "$VERSION_SUFFIX" =~ ^[0-9A-Za-z.-]+$ ]]; then
            echo "::error::version-suffix may only contain letters, digits, dots and hyphens"
            exit 1
          fi
          cat > Directory.Build.targets <<'XML'
          <Project>
            <PropertyGroup Condition="'$(PrereleaseLabel)' != ''">
              <Version>$(Version)-$(PrereleaseLabel)</Version>
            </PropertyGroup>
          </Project>
          XML

      - name: Pack every package in dependency order
        run: |
          for project in Vali-Flow.Abstractions Vali-Flow Vali-Flow.InMemory Vali-Flow.Sql Vali-Flow.NoSql Vali-Flow.NoSql.MongoDB Vali-Flow.NoSql.Elasticsearch Vali-Flow.NoSql.Redis Vali-Flow.NoSql.DynamoDB Vali-Flow.NoSql.Couchbase Vali-Flow.NoSql.CosmosDb Vali-Flow.NoSql.Firestore; do
            dotnet pack "$project/$project.csproj" -c Release -o artifacts ${VERSION_SUFFIX:+-p:PrereleaseLabel="$VERSION_SUFFIX"}
          done

      - name: Verify package contents
        run: |
          python3 - <<'PY'
          import glob
          import sys
          import xml.etree.ElementTree as ET
          import zipfile

          PACKAGES = {
              "Vali-Flow.Abstractions", "Vali-Flow", "Vali-Flow.InMemory", "Vali-Flow.Sql",
              "Vali-Flow.NoSql", "Vali-Flow.NoSql.MongoDB", "Vali-Flow.NoSql.Elasticsearch",
              "Vali-Flow.NoSql.Redis", "Vali-Flow.NoSql.DynamoDB", "Vali-Flow.NoSql.Couchbase",
              "Vali-Flow.NoSql.CosmosDb", "Vali-Flow.NoSql.Firestore",
          }
          TFMS = {"net8.0", "net9.0"}

          errors = []
          found = {}
          for path in sorted(glob.glob("artifacts/*.nupkg")):
              with zipfile.ZipFile(path) as archive:
                  names = archive.namelist()
                  root = ET.fromstring(archive.read(next(n for n in names if n.endswith(".nuspec"))))
              ns = {"n": root.tag.split("}")[0].strip("{")}
              pid = root.find("n:metadata/n:id", ns).text
              version = root.find("n:metadata/n:version", ns).text
              libs = {n.split("/")[1] for n in names if n.startswith("lib/") and n.endswith(".dll")}
              found[pid] = (version, libs)

          for pid in sorted(PACKAGES - set(found)):
              errors.append(f"{pid}: package missing from artifacts/")
          for pid, (version, libs) in sorted(found.items()):
              if libs != TFMS:
                  errors.append(f"{pid} {version}: lib/ has {sorted(libs)}, expected {sorted(TFMS)}")
              print(f"{pid} {version}: lib {sorted(libs)}")
          if errors:
              sys.exit("Package check failed:\n  " + "\n  ".join(errors))
          print("Package check passed.")
          PY

      - name: Upload packages
        uses: actions/upload-artifact@v4
        with:
          name: nuget-packages
          path: artifacts/*
          if-no-files-found: error
          retention-days: 14

  ci-ok:
    name: CI OK
    if: always()
    needs: [build, test, coverage, pack]
    runs-on: ubuntu-latest
    steps:
      - name: Every job must have succeeded
        env:
          RESULTS: ${{ toJSON(needs.*.result) }}
        run: |
          echo "$RESULTS"
          if echo "$RESULTS" | grep -Eq '"(failure|cancelled|skipped)"'; then
            exit 1
          fi
```

- [ ] **Step 2: Validate the YAML is syntactically well-formed**

Run: `python3 -c "import yaml, sys; yaml.safe_load(open('.github/workflows/ci.yml'))" 2>&1 || python3 -c "import json; print('yaml module unavailable, skipping — will be validated by GitHub on first push')"`
Expected: no exception printed (either the yaml module parses it cleanly, or the fallback message prints — either way, no traceback).

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: add reusable CI workflow (build, test matrix, 95% coverage gate, pack)"
```

---

### Task 5: Create `.github/workflows/release.yml`

**Files:**
- Create: `.github/workflows/release.yml`

**Interfaces:**
- Consumes: `.github/workflows/ci.yml` (Task 4) via `uses: ./.github/workflows/ci.yml`.
- Produces: the manually-triggered release workflow. Nothing downstream in this plan depends on it — this is the final deliverable.

- [ ] **Step 1: Write `.github/workflows/release.yml`**

```yaml
name: Release

# Manual only. By default it runs the full CI (net8.0/net9.0 on Linux and Windows) and uploads the
# packed NuGet packages as a workflow artifact; it publishes NOTHING. Publishing needs BOTH
# `publish: true` and an approval of the `nuget` environment (Settings > Environments > nuget >
# Required reviewers), and uses the NUGET_API_KEY secret of that environment.
#
# Publication order — 3 waves, because this repo has a 2-level dependency chain on top of the core:
#   Wave 1: Vali-Flow.Abstractions alone.
#   Wave 2: Vali-Flow, Vali-Flow.InMemory, Vali-Flow.Sql, Vali-Flow.NoSql (all depend on Abstractions).
#   Wave 3: the 7 NoSQL provider packages (all depend on Vali-Flow.NoSql from wave 2).
# Each wave waits for NuGet.org to index the package the next wave needs before proceeding.
on:
  workflow_dispatch:
    inputs:
      version-suffix:
        description: Prerelease label appended to every package version, for example rc.1 (empty = versions from the csproj files)
        type: string
        required: false
        default: ''
      publish:
        description: Publish the packages to NuGet.org after the approval of the nuget environment
        type: boolean
        default: false

permissions:
  contents: read

concurrency:
  group: release-${{ github.ref }}
  cancel-in-progress: false

jobs:
  ci:
    name: CI (build, test on net8.0/net9.0, coverage gate, pack)
    uses: ./.github/workflows/ci.yml
    with:
      version-suffix: ${{ inputs.version-suffix }}

  publish:
    name: Publish to NuGet (needs approval)
    needs: ci
    if: ${{ inputs.publish }}
    runs-on: ubuntu-latest
    environment: nuget
    env:
      NUGET_SOURCE: https://api.nuget.org/v3/index.json
    steps:
      - uses: actions/download-artifact@v4
        with:
          name: nuget-packages
          path: artifacts

      - name: Every package must contain net8.0 and net9.0
        run: |
          for package in artifacts/*.nupkg; do
            for tfm in net8.0 net9.0; do
              unzip -l "$package" | grep -q "lib/$tfm/" || { echo "::error::$package has no lib/$tfm"; exit 1; }
            done
          done

      - name: Check NUGET_API_KEY is configured
        env:
          NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
        run: |
          if [ -z "$NUGET_API_KEY" ]; then
            echo "::error::NUGET_API_KEY is not set in the nuget environment"
            exit 1
          fi

      # ---- Wave 1: Vali-Flow.Abstractions alone ----
      - name: 'Wave 1: push Vali-Flow.Abstractions'
        env:
          NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
        run: |
          # Vali-Flow.Abstractions is the only package whose id is followed by a version digit
          # among files matching "Vali-Flow.Abstractions.*" (there is no other package with that
          # prefix), so a plain glob is unambiguous here.
          dotnet nuget push artifacts/Vali-Flow.Abstractions.[0-9]*.nupkg --source "$NUGET_SOURCE" --api-key "$NUGET_API_KEY" --skip-duplicate

      - name: 'Wave 1: wait until NuGet.org lists Vali-Flow.Abstractions'
        run: |
          pkg=$(basename artifacts/Vali-Flow.Abstractions.[0-9]*.nupkg .nupkg)
          version=$(echo "${pkg#Vali-Flow.Abstractions.}" | tr '[:upper:]' '[:lower:]')
          for attempt in $(seq 1 40); do
            if curl -fsS "https://api.nuget.org/v3-flatcontainer/vali-flow.abstractions/index.json" | grep -q "\"$version\""; then
              echo "Vali-Flow.Abstractions $version is indexed"
              exit 0
            fi
            echo "attempt $attempt/40: Vali-Flow.Abstractions $version not indexed yet"
            sleep 30
          done
          echo "::error::Vali-Flow.Abstractions $version was not indexed in time; re-run this job (--skip-duplicate makes it safe)"
          exit 1

      # ---- Wave 2: Vali-Flow, Vali-Flow.InMemory, Vali-Flow.Sql, Vali-Flow.NoSql ----
      - name: 'Wave 2: push Vali-Flow, Vali-Flow.InMemory, Vali-Flow.Sql, Vali-Flow.NoSql'
        env:
          NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
        run: |
          # Vali-Flow.[0-9]* matches only the main package (not Vali-Flow.InMemory.*, Vali-Flow.Sql.*,
          # or Vali-Flow.NoSql.* — those have a literal "." before a letter, not a digit).
          for pattern in 'Vali-Flow.[0-9]*' 'Vali-Flow.InMemory.[0-9]*' 'Vali-Flow.Sql.[0-9]*' 'Vali-Flow.NoSql.[0-9]*'; do
            dotnet nuget push "artifacts/$pattern.nupkg" --source "$NUGET_SOURCE" --api-key "$NUGET_API_KEY" --skip-duplicate
          done

      - name: 'Wave 2: wait until NuGet.org lists Vali-Flow.NoSql'
        run: |
          # Only Vali-Flow.NoSql gates wave 3 — the 7 providers depend on it, not on Vali-Flow/InMemory/Sql.
          pkg=$(basename artifacts/Vali-Flow.NoSql.[0-9]*.nupkg .nupkg)
          version=$(echo "${pkg#Vali-Flow.NoSql.}" | tr '[:upper:]' '[:lower:]')
          for attempt in $(seq 1 40); do
            if curl -fsS "https://api.nuget.org/v3-flatcontainer/vali-flow.nosql/index.json" | grep -q "\"$version\""; then
              echo "Vali-Flow.NoSql $version is indexed"
              exit 0
            fi
            echo "attempt $attempt/40: Vali-Flow.NoSql $version not indexed yet"
            sleep 30
          done
          echo "::error::Vali-Flow.NoSql $version was not indexed in time; re-run this job (--skip-duplicate makes it safe)"
          exit 1

      # ---- Wave 3: the 7 NoSQL provider packages ----
      - name: 'Wave 3: push the 7 NoSQL provider packages'
        env:
          NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
        run: |
          for package in artifacts/Vali-Flow.NoSql.*.nupkg; do
            case "$(basename "$package")" in
              Vali-Flow.NoSql.[0-9]*) continue ;;  # already pushed in wave 2
            esac
            dotnet nuget push "$package" --source "$NUGET_SOURCE" --api-key "$NUGET_API_KEY" --skip-duplicate
          done
```

- [ ] **Step 2: Validate the YAML is syntactically well-formed**

Run: `python3 -c "import yaml, sys; yaml.safe_load(open('.github/workflows/release.yml'))" 2>&1 || python3 -c "import json; print('yaml module unavailable, skipping — will be validated by GitHub on first push')"`
Expected: no traceback.

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/release.yml
git commit -m "ci: add manual release workflow (3-wave dependency-ordered NuGet publish)"
```

---

### Task 6: Full local dry-run validation

**Files:** none created or modified — this task only runs commands.

**Interfaces:**
- Consumes: the final state of all 12 `.csproj` (Tasks 1-3) and both workflow files (Tasks 4-5).
- Produces: a go/no-go signal for whether this repo is actually ready for a real `workflow_dispatch` run. This is the task the plan's "Goal" and "Testing" section in the spec point to.

- [ ] **Step 1: Full solution build from a clean restore**

Run: `dotnet restore vali-flow.sln && dotnet build vali-flow.sln --configuration Release --no-restore`
Expected: `0 Errores` / `0 Error(s)`. If this fails with a missing-package error for `Vali-Flow.Abstractions` 1.2.0 or `Vali-Flow.NoSql` 1.1.1, a reference from Tasks 2/3 was left on the old `ProjectReference` or missed the version bump — go back and check `git diff` against Tasks 2/3's exact replacements.

- [ ] **Step 2: Full test suite**

Run: `dotnet test vali-flow.sln --configuration Release --no-build`
Expected: every one of the 11 test projects reports `Correctas!` / 0 failures (the exact counts don't matter here — this plan didn't change any production logic, only package metadata and dependency wiring — but a sudden failure means a reference edit broke something and must be investigated before continuing).

- [ ] **Step 3: Pack all 12 packages at their final bumped versions, with no local-source workaround**

This step is the real end-to-end check: if Tasks 1-3 are fully consistent, every package now resolves its sibling dependency as a normal transitive `PackageReference`-of-a-`PackageReference` chain once wave 1/2 packages are copied into a local feed — simulate that without ever touching the real NuGet.org:

```bash
mkdir -p /tmp/local-feed
for project in Vali-Flow.Abstractions Vali-Flow Vali-Flow.InMemory Vali-Flow.Sql Vali-Flow.NoSql Vali-Flow.NoSql.MongoDB Vali-Flow.NoSql.Elasticsearch Vali-Flow.NoSql.Redis Vali-Flow.NoSql.DynamoDB Vali-Flow.NoSql.Couchbase Vali-Flow.NoSql.CosmosDb Vali-Flow.NoSql.Firestore; do
  dotnet pack "$project/$project.csproj" --configuration Release -o /tmp/local-feed
done
ls /tmp/local-feed/*.nupkg | wc -l
```

Expected: `24` lines from the `ls | wc -l` (12 packages × `.nupkg` + `.snupkg` each — if the count is `12`, symbol packages aren't being produced, which is a packaging regression worth flagging before moving on, not silently accepted).

Expected version list — run `ls /tmp/local-feed/*.nupkg | sort` and confirm it matches exactly:

```
Vali-Flow.1.4.0.nupkg
Vali-Flow.Abstractions.1.2.0.nupkg
Vali-Flow.InMemory.1.2.0.nupkg
Vali-Flow.NoSql.1.1.1.nupkg
Vali-Flow.NoSql.Couchbase.1.1.0.nupkg
Vali-Flow.NoSql.CosmosDb.1.1.0.nupkg
Vali-Flow.NoSql.DynamoDB.1.2.0.nupkg
Vali-Flow.NoSql.Elasticsearch.1.2.0.nupkg
Vali-Flow.NoSql.Firestore.1.1.0.nupkg
Vali-Flow.NoSql.MongoDB.1.2.0.nupkg
Vali-Flow.NoSql.Redis.1.2.0.nupkg
Vali-Flow.Sql.1.2.0.nupkg
```

- [ ] **Step 4: Confirm no stray local NuGet source was left behind on this machine**

Run: `dotnet nuget list source`
Expected: no entry named `local-verify-only` (added and removed in Tasks 2 and 3 — if it's still listed, remove it now with `dotnet nuget remove source local-verify-only`, since leaving a `/tmp` path as a permanent NuGet source would silently break restores on this machine after `/tmp` is cleared).

- [ ] **Step 5: Report the dry-run result**

No commit for this task (nothing was created or modified). Summarize in the chat: build result, test result, the 12-package version/pack confirmation, and that `.github/workflows/{ci.yml,release.yml}` exist and are ready for the user to exercise with a real `workflow_dispatch` (`publish: false` first, to validate the GitHub-hosted runners end-to-end before ever approving a real publish) once the `nuget` GitHub environment prerequisite is in place.
