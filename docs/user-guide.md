# User guide

Run `dotnet run --project src/SqlScriptGen.Cli -- --help`. Interactive mode prompts for the supported dialect, table name, and ordered columns. Enter `done` as the next column name to render. Invalid choices are re-prompted with loops; Ctrl+C exits with code 130.

For repeatable generation:

```powershell
dotnet run --project src/SqlScriptGen.Cli -- generate --dialect postgresql --input examples/postgresql-customer.json
dotnet run --project src/SqlScriptGen.Cli -- generate --dialect mysql --input examples/mysql-customer.json --output customer.sql
```

JSON has `name`, optional `schema`, ordered `columns`, and optional `constraints`. A column has `name`, `type: { name, length?, precision?, scale? }`, `nullable`, optional `default: { value }`, and `identity`. Constraint objects use a `kind`: `primaryKey`, `unique`, `check`, or `foreignKey`. Foreign keys accept `columns`, `referencedTable`, `referencedColumns`, optional `referencedSchema`, `onDelete`, and `onUpdate`; actions are `noAction`, `restrict`, `cascade`, `setNull`, and PostgreSQL-only `setDefault`.

Use `list-types --dialect mysql` to inspect supported types and `create-database --dialect postgresql --name example_database` for database DDL. Output goes to stdout unless `--output` is supplied. Exit codes: 0 success, 1 arguments, 2 validation, 3 JSON, 4 file I/O, 130 cancellation.

Default/check expressions are raw SQL by necessity. The tool only writes text, but generated scripts must be reviewed before execution.
