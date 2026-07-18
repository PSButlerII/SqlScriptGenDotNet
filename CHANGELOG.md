# Changelog

## 1.1.0 - Unreleased

- Add canonical format-version-1 documents containing ordered table or database objects.
- Preserve legacy 1.0 single-table JSON through a compatibility adapter and unchanged public overloads.
- Add stable dependency ordering, internal/external foreign-key distinction, document validation, cycle diagnostics, and dialect capabilities.
- Add ordered statement metadata, multi-statement CLI generation, JSON Schema, examples, tests, and migration documentation.
- Continue to exclude later roadmap SQL objects and all database execution/connectivity.

## 1.0.0 - 2026-07-18

First stable release: a complete C# and .NET 10 rewrite of the legacy Java application.

### Added

- PostgreSQL and MySQL `CREATE TABLE` and `CREATE DATABASE` generation.
- Interactive and JSON-driven CLI workflows with stable exit codes.
- Deterministic formatting and ordered, typed table, column, data type, and constraint models.
- Primary, composite primary, unique, check, foreign-key, and composite foreign-key constraints with referential actions.
- PostgreSQL identity and MySQL auto-increment support.
- Identifier validation, dialect-correct quoting, automated tests, GitHub Actions, and self-contained publishing.

### Migration corrections

- Removed misleading MongoDB support and ordinal-based database dispatch.
- Preserved user-defined column order and completed MySQL constraint generation.
- Corrected PostgreSQL foreign-key syntax.
- Replaced recursive retries, mixed Java `Scanner` behavior, and fragile trailing-delimiter removal.
- Added structured model and identifier validation.

### Known limitations

- One table definition per JSON document; no indexes, `ALTER TABLE`, or schema import.
- Raw SQL expressions are not semantically parsed or validated. Generated SQL must be reviewed before execution.
