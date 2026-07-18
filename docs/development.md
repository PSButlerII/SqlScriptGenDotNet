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

## Release process

1. Update the centralized version metadata in `Directory.Build.props` and add the dated changelog entry.
2. Run restore, Release build, tests, formatting verification, CLI version verification, and both golden examples.
3. Run `./scripts/Build-Release.ps1 -Version 1.0.0`. It publishes untrimmed self-contained single files, creates Windows ZIP and Linux tar.gz packages, and writes `SHA256SUMS.txt` under `artifacts/release/1.0.0`.
4. Verify both executables (use WSL for Linux when available), package contents, and checksums.
5. Commit and push release preparation. Create and push annotated tag `v1.0.0` only on that validated commit.
6. Create the stable GitHub Release with the tag, two archives, checksum file, and reviewed release notes; then verify its public metadata and assets.

Never commit `artifacts/`, tag an unpushed commit, or publish while validation fails.
