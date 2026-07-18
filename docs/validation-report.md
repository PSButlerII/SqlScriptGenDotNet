# Validation report

Validated on Windows x64 on 2026-07-18.

- `dotnet --info`: SDK 10.0.302, runtime 10.0.10, RID `win-x64`; repository `global.json` active.
- `dotnet restore`: succeeded; all four projects restored.
- `dotnet build --configuration Release`: succeeded with 0 warnings and 0 errors.
- `dotnet test --configuration Release`: succeeded; 38 passed, 0 failed, 0 skipped (30 Core and 8 CLI tests).
- `dotnet publish .\src\SqlScriptGen.Cli\SqlScriptGen.Cli.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true`: succeeded.
- Compiled CLI PostgreSQL generation matched `examples/postgresql-customer.sql` byte-for-byte.
- Compiled CLI MySQL generation matched `examples/mysql-customer.sql` byte-for-byte.
- `dotnet format --verify-no-changes`: succeeded before commit.

Generated `bin`, `obj`, test, and publish artifacts are excluded from version control.
