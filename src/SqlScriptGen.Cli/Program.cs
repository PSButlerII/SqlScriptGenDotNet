using System.Reflection;
using System.Text.Json;
using SqlScriptGen.Core;

return await CliApplication.RunAsync(args, Console.In, Console.Out, Console.Error, CancellationToken.None);

public static class CliApplication
{
    public static async Task<int> RunAsync(string[] args, TextReader input, TextWriter output, TextWriter error, CancellationToken cancellationToken)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h" or "help") { await output.WriteLineAsync(Help); return 0; }
        if (args[0] is "--version" or "-v") { await output.WriteLineAsync($"sqlscriptgen {Version}"); return 0; }
        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "generate" => await GenerateAsync(args[1..], output, cancellationToken),
                "create-database" => await CreateDatabaseAsync(args[1..], output, cancellationToken),
                "list-types" => await ListTypesAsync(args[1..], output),
                "interactive" => await InteractiveAsync(input, output, cancellationToken),
                _ => await FailAsync(error, $"Unknown command '{args[0]}'.")
            };
        }
        catch (OperationCanceledException) { await error.WriteLineAsync("Cancelled."); return 130; }
        catch (SqlValidationException ex) { foreach (var item in ex.Errors) await error.WriteLineAsync($"{item.Path}: {item.Message}"); return 2; }
        catch (JsonException ex) { await error.WriteLineAsync($"Invalid JSON: {ex.Message}"); return 3; }
        catch (IOException ex) { await error.WriteLineAsync($"File error: {ex.Message}"); return 4; }
        catch (UnauthorizedAccessException ex) { await error.WriteLineAsync($"File error: {ex.Message}"); return 4; }
        catch (ArgumentException ex) { await error.WriteLineAsync(ex.Message); return 1; }
    }

    private static async Task<int> GenerateAsync(string[] args, TextWriter output, CancellationToken ct)
    {
        var dialect = ParseDialect(Required(args, "--dialect")); var path = Required(args, "--input"); var outPath = Optional(args, "--output");
        var json = await File.ReadAllTextAsync(path, ct); var table = DefinitionJson.Deserialize(json); var sql = new SqlGenerator().Generate(table, dialect).Sql;
        if (outPath is null) await output.WriteAsync(sql); else await File.WriteAllTextAsync(outPath, sql, ct);
        return 0;
    }
    private static async Task<int> CreateDatabaseAsync(string[] args, TextWriter output, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); var dialect = ParseDialect(Required(args, "--dialect")); var name = Required(args, "--name");
        await output.WriteAsync(new SqlGenerator().Generate(new DatabaseDefinition(name), dialect).Sql); return 0;
    }
    private static async Task<int> ListTypesAsync(string[] args, TextWriter output)
    {
        var dialect = ParseDialect(Required(args, "--dialect")); foreach (var type in SqlTypeCatalogs.For(dialect).Keys.Order()) await output.WriteLineAsync(type); return 0;
    }
    private static async Task<int> InteractiveAsync(TextReader input, TextWriter output, CancellationToken ct)
    {
        var dialect = await PromptChoice(input, output, "Dialect (postgresql/mysql): ", ["postgresql", "mysql"], ct);
        var tableName = await PromptRequired(input, output, "Table name: ", ct);
        var columns = new List<ColumnDefinition>();
        while (true)
        {
            var name = await PromptRequired(input, output, "Column name (or 'done'): ", ct); if (name.Equals("done", StringComparison.OrdinalIgnoreCase)) break;
            var type = await PromptRequired(input, output, "Data type: ", ct);
            var nullable = await PromptChoice(input, output, "Nullable (yes/no): ", ["yes", "no"], ct);
            columns.Add(new(name, new(type), nullable == "yes"));
        }
        var sql = new SqlGenerator().Generate(new TableDefinition(tableName, columns), ParseDialect(dialect)).Sql; await output.WriteAsync(sql); return 0;
    }
    private static async Task<string> PromptRequired(TextReader input, TextWriter output, string prompt, CancellationToken ct)
    { while (true) { ct.ThrowIfCancellationRequested(); await output.WriteAsync(prompt); var value = await input.ReadLineAsync(ct); if (value is null) throw new OperationCanceledException(); if (!string.IsNullOrWhiteSpace(value)) return value.Trim(); await output.WriteLineAsync("A value is required."); } }
    private static async Task<string> PromptChoice(TextReader input, TextWriter output, string prompt, string[] choices, CancellationToken ct)
    { while (true) { var value = (await PromptRequired(input, output, prompt, ct)).ToLowerInvariant(); if (choices.Contains(value)) return value; await output.WriteLineAsync($"Choose {string.Join(" or ", choices)}."); } }
    private static DatabaseDialect ParseDialect(string value) => value.ToLowerInvariant() switch { "postgresql" or "postgres" => DatabaseDialect.PostgreSql, "mysql" => DatabaseDialect.MySql, _ => throw new ArgumentException($"Unsupported dialect '{value}'. Choose postgresql or mysql.") };
    private static string Required(string[] args, string name) => Optional(args, name) ?? throw new ArgumentException($"Missing required option {name}.");
    private static string? Optional(string[] args, string name) { var i = Array.FindIndex(args, x => x.Equals(name, StringComparison.OrdinalIgnoreCase)); if (i < 0) return null; if (i + 1 >= args.Length || args[i + 1].StartsWith('-')) throw new ArgumentException($"Option {name} requires a value."); return args[i + 1]; }
    private static async Task<int> FailAsync(TextWriter error, string message) { await error.WriteLineAsync(message); await error.WriteLineAsync("Run sqlscriptgen --help for usage."); return 1; }

    private const string Help = """
SqlScriptGenDotNet - deterministic PostgreSQL and MySQL DDL generation

Usage:
  sqlscriptgen interactive
  sqlscriptgen generate --dialect <postgresql|mysql> --input <file> [--output <file>]
  sqlscriptgen create-database --dialect <postgresql|mysql> --name <name>
  sqlscriptgen list-types --dialect <postgresql|mysql>
  sqlscriptgen --help | --version
""";

    private static string Version => typeof(CliApplication).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        .Split('+', 2)[0] ?? "unknown";
}
