# Design: Release pipeline for Vali-Flow (GitHub Actions)

**Date:** 2026-10-06
**Status:** Approved

## Problem

The Vali-Flow repository ships 12 independent NuGet packages with a two-level dependency chain
(`Vali-Flow.Abstractions` → `Vali-Flow`/`Vali-Flow.InMemory`/`Vali-Flow.Sql`/`Vali-Flow.NoSql` →
the 7 NoSQL provider packages), and has **no CI/CD at all** today — no `.github/workflows/`
directory exists. Publishing has been manual. This session's work (observability, bug fixes found
via stress testing, new providers) needs to go out, but every package's committed `<Version>`
already equals what's currently live on NuGet.org — nothing can be republished without bumping
versions first, and the dependency chain means publishing out of order will push packages whose
declared `PackageReference` points at a version that isn't live yet.

Two sibling repositories in the same ecosystem (`Vali-Mediator`, `Vali-Flow.Core`) already solved
this with a `ci.yml` + `release.yml` pair. Vali-Flow's job is to extend that same pattern to a third
dependency wave, not invent a new one.

## Non-goals

- This spec does not design the test/coverage CI that will also eventually run on every PR — that
  is `ci.yml`'s job, reused here, but a from-scratch CI design is out of scope; it follows the
  already-proven shape from `Vali-Flow.Core`.
- This spec does not perform the version bump or revert the 11 `.csproj` files currently on
  `ProjectReference` (temporary, for this session's stress testing) back to `PackageReference`.
  That is a prerequisite carried out before the pipeline can run, tracked separately.
- No changes to `Vali-Flow.Core` (unchanged this session, already published at the version every
  package here pins).

## Reference pattern (what we're extending)

`Vali-Flow.Core/.github/workflows/{ci.yml,release.yml}` and `Vali-Mediator`'s equivalent files
already implement, for a **two-wave** dependency chain (core, then everything else):

- `ci.yml`: `workflow_call`-able. Jobs: `build` → `test` (matrix `{ubuntu-latest, windows-latest} ×
  {net8.0, net9.0}`, coverage collected via `--collect:"XPlat Code Coverage"`) → `coverage` (merges
  Cobertura reports across every OS/TFM cell, per-library gate) → `pack` (applies an optional
  prerelease suffix, packs every project, verifies every `.nupkg` contains both TFMs) → `ci-ok`
  (single required status check).
- `release.yml`: `workflow_dispatch` only, inputs `version-suffix` (optional prerelease label) and
  `publish` (boolean, default false). Calls `ci.yml`. If `publish` is true, a second job
  (`environment: nuget`, gated on GitHub's environment-approval + required reviewers) downloads the
  packed artifact, pushes the core package, polls `https://api.nuget.org/v3-flatcontainer/<id>/index.json`
  every 30s (up to 40 attempts) until the core version is indexed, then pushes every dependent
  package. `--skip-duplicate` on every push makes a re-run safe.

Vali-Flow reuses this shape unchanged except for the publish job, which needs a third wave.

## Design

### 1. `ci.yml` (new, reusable)

Same shape as `Vali-Flow.Core`'s: `build` (restore + build the whole `.sln`) → `test` (matrix
`{ubuntu-latest, windows-latest} × {net8.0, net9.0}`, runs all 11 test projects, collects per-cell
Cobertura coverage, uploads a `.trx`-based summary table and a merged full-trace artifact) →
`coverage` (merges Cobertura across all 4 cells per library, **95% line-coverage gate per
package** — the 12 packages below are "Library" rows the Python merge script checks by name) →
`pack` (packs all 12 projects with the optional `version-suffix` prerelease label applied via the
same `Directory.Build.targets` injection trick as `Vali-Flow.Core`, verifies every `.nupkg` has
both `lib/net8.0` and `lib/net9.0`) → `ci-ok`.

**Coverage libraries list** (mirrors `Vali-Flow.Core`'s `LIBS` array, one row per package):
`Vali-Flow.Abstractions`, `Vali-Flow`, `Vali-Flow.InMemory`, `Vali-Flow.Sql`, `Vali-Flow.NoSql`,
`Vali-Flow.NoSql.MongoDB`, `Vali-Flow.NoSql.Elasticsearch`, `Vali-Flow.NoSql.Redis`,
`Vali-Flow.NoSql.DynamoDB`, `Vali-Flow.NoSql.Couchbase`, `Vali-Flow.NoSql.CosmosDb`,
`Vali-Flow.NoSql.Firestore`. `Vali-Flow.Benchmarks` is excluded (no tests, it's a BenchmarkDotNet
project).

**Threshold note** (same framing as `Vali-Flow.Core`'s own comment): 95% is a starting gate, not an
audited baseline for the 10 packages that weren't measured this session (`Vali-Flow` itself was:
97.42%). The first real CI run will show every package's actual number; any package under 95% needs
either more tests or an explicitly-justified threshold adjustment at that point — not a silent
pre-emptive lowering now.

### 2. `release.yml` (new)

Identical `workflow_dispatch` inputs and `ci` job as the reference pattern. The `publish` job
(same `environment: nuget`, same `NUGET_API_KEY` secret, same `--skip-duplicate` everywhere) grows
from 2 steps-with-a-wait to **3 waves**:

```
Wave 1: push Vali-Flow.Abstractions.<version>.nupkg
        wait until nuget.org indexes vali-flow.abstractions/<version>

Wave 2: push Vali-Flow.<version>.nupkg, Vali-Flow.InMemory.<version>.nupkg,
             Vali-Flow.Sql.<version>.nupkg, Vali-Flow.NoSql.<version>.nupkg
        wait until nuget.org indexes vali-flow.nosql/<version>
        (Vali-Flow/InMemory/Sql don't gate wave 3 — only NoSql does, since only the
         7 providers in wave 3 depend on it)

Wave 3: push the 7 Vali-Flow.NoSql.{MongoDB,Elasticsearch,Redis,DynamoDB,Couchbase,CosmosDb,Firestore}
        .<version>.nupkg packages
```

Each wave's push step globs its packages by filename prefix, same technique the reference pattern
uses to distinguish the core from everything else (e.g. `Vali-Flow.[0-9]*.nupkg` matches only the
main package, not `Vali-Flow.InMemory.*` or `Vali-Flow.Sql.*`, because of the literal dot before the
version digit). The indexed-version check script is parameterized by package id instead of
hardcoded, since there are now two intermediate gates instead of one.

### 3. Operational prerequisites (not part of this pipeline's code, but required before first use)

- **GitHub environment `nuget`** must exist on this repo (Settings → Environments) with a
  `NUGET_API_KEY` secret and required reviewers configured — identical setup to `Vali-Mediator`/
  `Vali-Flow.Core`. The user sets this up; the pipeline cannot create its own secrets or approval
  gates.
- **Version bump + reference cleanup**: every package's `<Version>` must be bumped past what's
  already live (table below) and the 11 `.csproj` files currently on a temporary `ProjectReference`
  (from this session's stress testing) must be reverted to `PackageReference` pinned at the new
  version numbers, before any release run — otherwise `pack` either fails to resolve the reference
  or ships a package that depends on a version not yet on NuGet.

  | Package | Published (= current csproj) | → New version |
  |---|---|---|
  | Vali-Flow.Abstractions | 1.1.0 | 1.2.0 |
  | Vali-Flow.NoSql | 1.1.0 | 1.1.1 |
  | Vali-Flow | 1.3.4 | 1.4.0 |
  | Vali-Flow.InMemory | 1.1.5 | 1.2.0 |
  | Vali-Flow.Sql | 1.1.1 | 1.2.0 |
  | Vali-Flow.NoSql.MongoDB | 1.1.0 | 1.2.0 |
  | Vali-Flow.NoSql.Elasticsearch | 1.1.0 | 1.2.0 |
  | Vali-Flow.NoSql.Redis | 1.1.0 | 1.2.0 |
  | Vali-Flow.NoSql.DynamoDB | 1.1.0 | 1.2.0 |
  | Vali-Flow.NoSql.Couchbase | 1.0.0 | 1.1.0 |
  | Vali-Flow.NoSql.CosmosDb | 1.0.0 | 1.1.0 |
  | Vali-Flow.NoSql.Firestore | 1.0.0 | 1.1.0 |

  All bumps are minor (additive API surface / non-breaking fixes only — no package introduced a
  breaking change this session).

## Testing

The pipeline's own correctness is validated by running it: a `publish: false` dry run exercises
`build`/`test`/`coverage`/`pack` without touching NuGet, which is the safe way to validate the
workflow YAML and the coverage/pack scripts before ever approving a real publish. The first real
`publish: true` run is the actual end-to-end test of the 3-wave ordering and indexing waits.

## Error handling

Same as the reference pattern: `--skip-duplicate` on every `dotnet nuget push` makes any step safe
to re-run after a transient failure (e.g. the indexing wait timing out at 40×30s) without
double-publishing. The `coverage` and `pack` verification steps fail the whole run before anything
is ever pushed, so a broken package can't reach NuGet even on a rushed manual dispatch.
