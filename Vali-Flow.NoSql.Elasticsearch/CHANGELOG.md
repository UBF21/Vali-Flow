# Changelog — Vali-Flow.NoSql.Elasticsearch

All notable changes to this package are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) · Versioning: [SemVer](https://semver.org/)

---

## [Unreleased]

---

## [1.0.0] — Initial release

### Added
- `ElasticsearchFilterTranslator` — translates Vali-Flow IR nodes into Elasticsearch `Query` objects (Elastic.Clients.Elasticsearch)
- Support for: `TermQuery`, `TermsQuery`, `RangeQuery`, `MatchQuery`, `WildcardQuery`, `ExistsQuery`, `BoolQuery` (must / must_not / should)
- Null checks: `IsNull` → `must_not exists`, `IsNotNull` → `exists`
- `ValiFlowElasticsearchExtensions.ToElasticsearchQuery<T>()` — extension method on `ValiFlow<T>`
- XML documentation on all public types and members
