# Migration report

## Rationale and mapping

.NET 10 supplies the requested current LTS-era platform, cross-platform single-file publishing, nullable analysis, records, `System.Text.Json`, and a strong standard library. The migration maps Java `DatabaseScriptGenerator` to `ISqlDialectRenderer`/`SqlGenerator`; nested enums and raw maps to typed records and `SqlTypeCatalogs`; PostgreSQL/MySQL generators to separate renderers; `UserInteraction`/`Main` to `CliApplication`; and JUnit placeholders to xUnit suites.

## Preserved and corrected

Preserved: interactive table definition, PostgreSQL/MySQL selection, useful legacy scalar types, table/database DDL, and constraints. Corrected: deterministic column order, valid FK parentheses, implemented MySQL constraints, safe nonempty assembly, loop-based prompts, explicit dialect keys, dialect-specific type rejection, quoted/validated identifiers, collected errors, and executable distribution.

## Scope decisions and new behavior

MongoDB placeholders and the incomplete JavaFX GUI are intentionally excluded. MongoDB is not SQL; a future document-schema generator would be a separate product. Exclusion constraints are excluded until they can be represented safely. New capabilities include JSON input/output files, composite keys, referential actions, structured type arguments, identity/auto-increment, defaults/nullability, stable exit codes, Ctrl+C cancellation, exact-output tests, CI, and single-file publishing.

All identifiers are conservatively validated then always quoted for deterministic safety. Raw default/check expressions remain explicit review-required values and are never executed. PostgreSQL types render lowercase; MySQL types uppercase. One JSON document defines one table.

## Component summary and limitations

- `Models.cs`: immutable domain and polymorphic constraints.
- `Catalogs.cs`: independently maintained dialect catalogs.
- `Validation.cs`: aggregate structural/dialect validation.
- `Rendering.cs`: registry plus isolated renderers and centralized formatting.
- `Serialization.cs`: stable case-insensitive strict JSON behavior.
- CLI `Program.cs`: parsing, file I/O, orchestration, and iterative interaction.
- Tests/examples/docs/CI: replace absent legacy quality and packaging infrastructure.

Remaining limitations: raw SQL expressions are not parsed; database and table objects cannot be mixed; there are no indexes, alter statements, schema import, or live database verification.

## Version 1.1 foundation

Version 1.1 adds a canonical, versioned multi-object envelope while preserving all valid 1.0 single-table files through a dedicated adapter. Both formats now share document validation, capability checks, stable dependency ordering, rendering, and CLI output. Only existing table and database SQL objects are supported. Database/table mixing is rejected because database creation requires a separate connection context. See `docs/json-format-v1.md` for migration details.
