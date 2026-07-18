# Development

Install the .NET 10 SDK, clone the repository, then run:

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
dotnet format --verify-no-changes
```

Keep Core free of console/filesystem dependencies. Add exact output and validation tests with every renderer change. Format using `dotnet format`, follow `.editorconfig`, and use focused conventional commits. CI restores, builds Release, and runs all tests on pushes and pull requests.

Framework-dependent publish: `dotnet publish src/SqlScriptGen.Cli -c Release`. Self-contained single-file commands for `win-x64` and `linux-x64` are in the README. Build output belongs under ignored `bin`/`obj` and must not be committed.
