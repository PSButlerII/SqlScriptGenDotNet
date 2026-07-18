# User guide

Run `dotnet run --project src/SqlScriptGen.Cli -- --help`. Interactive mode prompts for the supported dialect, table name, and ordered columns. Enter `done` as the next column name to render. Invalid choices are re-prompted with loops; Ctrl+C exits with code 130.

For repeatable generation:

```powershell
dotnet run --project src/SqlScriptGen.Cli -- generate --dialect postgresql --input examples/postgresql-customer.json
dotnet run --project src/SqlScriptGen.Cli -- generate --dialect mysql --input examples/mysql-customer.json --output customer.sql
dotnet run --project src/SqlScriptGen.Cli -- generate --dialect postgresql --input examples/v1/postgresql-multi-table.json
```

Canonical JSON uses `{ "formatVersion": 1, "objects": [...] }`. Objects are ordered and use `kind: table` or `kind: database`. Legacy unversioned single-table JSON remains accepted. Table columns and constraints keep the 1.0 shapes. Objects can declare `dependsOn` identities; explicit targets must exist. Internal foreign keys order tables automatically, while absent FK targets are treated as external references. Database and table objects cannot be mixed in one document.

See [JSON format version 1](json-format-v1.md) for the complete contract, migration guidance, ordering/cycle rules, and examples.

Use `list-types --dialect mysql` to inspect supported types and `create-database --dialect postgresql --name example_database` for database DDL. Output goes to stdout unless `--output` is supplied. Exit codes: 0 success, 1 arguments, 2 validation, 3 JSON, 4 file I/O, 130 cancellation.

Default/check expressions are raw SQL by necessity. The tool only writes text, but generated scripts must be reviewed before execution.
