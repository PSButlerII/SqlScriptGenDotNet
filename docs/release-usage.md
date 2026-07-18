# SqlScriptGenDotNet 1.0.0 quick start

The executable is self-contained. Run `sqlscriptgen --help` (or `sqlscriptgen.exe --help` on Windows).

```text
sqlscriptgen generate --dialect postgresql --input postgresql-customer.json
sqlscriptgen generate --dialect mysql --input mysql-customer.json --output customer.sql
sqlscriptgen create-database --dialect postgresql --name example_database
sqlscriptgen interactive
```

This application only generates SQL text; it never connects to a database or executes SQL. Raw check/default expressions are not semantically validated. Review generated scripts before execution.
