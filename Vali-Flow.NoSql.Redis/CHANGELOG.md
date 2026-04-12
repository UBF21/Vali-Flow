# Changelog — Vali-Flow.NoSql.Redis

All notable changes to this package are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) · Versioning: [SemVer](https://semver.org/)

---

## [Unreleased]

---

## [1.0.0] — Initial release

### Added
- `RedisSearchFilterTranslator` — translates Vali-Flow IR nodes into RediSearch query strings
- Support for: numeric range filters (`@field:[min max]`), text search (`@field:value`), tag filters (`@field:{value}`), AND/OR/NOT logic, IN list as tag union
- Automatic type routing: numeric types → range query, strings → text/tag query, enums → tag query by name, Guids → tag query
- `ValiFlowRedisSearchExtensions.ToRedisQuery<T>()` — extension method on `ValiFlow<T>`
- XML documentation on all public types and members
