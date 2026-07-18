namespace SqlScriptGen.Core;

public interface ISqlDialectRenderer
{
    DatabaseDialect Dialect { get; }
    GeneratedSqlDocument RenderCreateTable(TableDefinition table);
    GeneratedSqlDocument RenderCreateDatabase(DatabaseDefinition database);
}

public sealed class SqlGenerator
{
    private readonly IReadOnlyDictionary<DatabaseDialect, ISqlDialectRenderer> renderers;
    public SqlGenerator(IEnumerable<ISqlDialectRenderer>? renderers = null) => this.renderers = (renderers ?? [new PostgreSqlRenderer(), new MySqlRenderer()]).ToDictionary(x => x.Dialect);
    public GeneratedSqlDocument Generate(TableDefinition table, DatabaseDialect dialect)
    {
        var result = SqlDefinitionValidator.Validate(table, dialect);
        if (!result.IsValid) throw new SqlValidationException(result.Errors);
        return renderers[dialect].RenderCreateTable(table);
    }
    public GeneratedSqlDocument Generate(DatabaseDefinition database, DatabaseDialect dialect)
    {
        var result = SqlDefinitionValidator.ValidateIdentifier(database.Name, "name");
        if (!result.IsValid) throw new SqlValidationException(result.Errors);
        return renderers[dialect].RenderCreateDatabase(database);
    }

    public GeneratedSqlDocument Generate(SqlDefinitionDocument document, DatabaseDialect dialect)
    {
        var validation = SqlDefinitionDocumentValidator.Validate(document, dialect);
        if (!validation.IsValid) throw new SqlValidationException(validation.Errors);

        var renderer = renderers[dialect];
        var declarationOrder = document.Objects.Select((item, index) => (item.Identity, index)).ToDictionary(x => x.Identity, x => x.index);
        var orderedObjects = DatabaseObjectOrderer.Order(document);
        var statements = new List<GeneratedSqlStatement>(orderedObjects.Count);
        for (var generatedOrder = 0; generatedOrder < orderedObjects.Count; generatedOrder++)
        {
            var source = orderedObjects[generatedOrder];
            var rendered = source switch
            {
                TableDefinition table => (renderer.RenderCreateTable(table), GeneratedSqlStatementKind.CreateTable),
                DatabaseDefinition database => (renderer.RenderCreateDatabase(database), GeneratedSqlStatementKind.CreateDatabase),
                _ => throw new NotSupportedException($"Object type '{source.GetType().Name}' cannot be rendered.")
            };
            statements.Add(new(source.Identity, rendered.Item2, rendered.Item1.Sql, declarationOrder[source.Identity], generatedOrder));
        }

        var sql = string.Join("\n\n", statements.Select(statement => statement.Sql.TrimEnd('\r', '\n'))) + "\n";
        return new(sql) { Statements = statements };
    }
}

public sealed class SqlValidationException(IReadOnlyList<ValidationError> errors) : Exception("SQL definition validation failed.") { public IReadOnlyList<ValidationError> Errors { get; } = errors; }

public abstract class SqlDialectRenderer : ISqlDialectRenderer
{
    public abstract DatabaseDialect Dialect { get; }
    protected abstract char QuoteCharacter { get; }
    protected abstract string IdentityClause { get; }
    protected virtual string TypeName(string name) => name.ToLowerInvariant();
    protected string Quote(string identifier) => $"{QuoteCharacter}{identifier.Replace(QuoteCharacter.ToString(), new string(QuoteCharacter, 2), StringComparison.Ordinal)}{QuoteCharacter}";
    public GeneratedSqlDocument RenderCreateTable(TableDefinition table)
    {
        var name = table.Schema is null ? Quote(table.Name) : $"{Quote(table.Schema)}.{Quote(table.Name)}";
        var lines = table.Columns.Select(RenderColumn).Concat((table.Constraints ?? []).Select(RenderConstraint)).ToArray();
        return new($"CREATE TABLE {name} (\n{string.Join(",\n", lines.Select(x => "    " + x))}\n);\n");
    }
    public GeneratedSqlDocument RenderCreateDatabase(DatabaseDefinition database) => new($"CREATE DATABASE {Quote(database.Name)};\n");
    private string RenderColumn(ColumnDefinition c)
    {
        var type = TypeName(c.Type.Name);
        if (c.Type.Length is not null) type += $"({c.Type.Length})";
        else if (c.Type.Precision is not null) type += c.Type.Scale is null ? $"({c.Type.Precision})" : $"({c.Type.Precision},{c.Type.Scale})";
        return $"{Quote(c.Name)} {type}{(c.Identity ? " " + IdentityClause : "")}{(c.Default is null ? "" : " DEFAULT " + c.Default.Value)}{(c.Nullable ? "" : " NOT NULL")}";
    }
    private string RenderConstraint(TableConstraint c) => c switch
    {
        PrimaryKeyConstraint x => $"CONSTRAINT {Quote(x.Name)} PRIMARY KEY ({Columns(x.Columns)})",
        UniqueConstraint x => $"CONSTRAINT {Quote(x.Name)} UNIQUE ({Columns(x.Columns)})",
        CheckConstraint x => $"CONSTRAINT {Quote(x.Name)} CHECK ({x.Expression.Value})",
        ForeignKeyConstraint x => $"CONSTRAINT {Quote(x.Name)} FOREIGN KEY ({Columns(x.Columns)}) REFERENCES {(x.ReferencedSchema is null ? "" : Quote(x.ReferencedSchema) + ".")}{Quote(x.ReferencedTable)} ({Columns(x.ReferencedColumns)}){Action("ON DELETE", x.OnDelete)}{Action("ON UPDATE", x.OnUpdate)}",
        _ => throw new NotSupportedException()
    };
    private string Columns(IEnumerable<string> columns) => string.Join(", ", columns.Select(Quote));
    private static string Action(string keyword, ReferentialAction? action) => action is null ? "" : $" {keyword} {action.Value switch { ReferentialAction.NoAction => "NO ACTION", ReferentialAction.SetNull => "SET NULL", ReferentialAction.SetDefault => "SET DEFAULT", _ => action.Value.ToString().ToUpperInvariant() }}";
}

public sealed class PostgreSqlRenderer : SqlDialectRenderer { public override DatabaseDialect Dialect => DatabaseDialect.PostgreSql; protected override char QuoteCharacter => '"'; protected override string IdentityClause => "GENERATED BY DEFAULT AS IDENTITY"; }
public sealed class MySqlRenderer : SqlDialectRenderer { public override DatabaseDialect Dialect => DatabaseDialect.MySql; protected override char QuoteCharacter => '`'; protected override string IdentityClause => "AUTO_INCREMENT"; protected override string TypeName(string name) => name.ToUpperInvariant(); }
