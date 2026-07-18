using System.Text.Json.Serialization;

namespace SqlScriptGen.Core;

[JsonConverter(typeof(JsonStringEnumConverter<DatabaseDialect>))]
public enum DatabaseDialect { PostgreSql, MySql }

[JsonConverter(typeof(JsonStringEnumConverter<DatabaseObjectKind>))]
public enum DatabaseObjectKind { Table, Database }

[JsonConverter(typeof(JsonStringEnumConverter<ReferentialAction>))]
public enum ReferentialAction { NoAction, Restrict, Cascade, SetNull, SetDefault }

public sealed record SqlExpression(string Value);
public sealed record DataTypeDefinition(string Name, int? Length = null, int? Precision = null, int? Scale = null);
public sealed record ColumnDefinition(string Name, DataTypeDefinition Type, bool Nullable = true, SqlExpression? Default = null, bool Identity = false);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(PrimaryKeyConstraint), "primaryKey")]
[JsonDerivedType(typeof(UniqueConstraint), "unique")]
[JsonDerivedType(typeof(CheckConstraint), "check")]
[JsonDerivedType(typeof(ForeignKeyConstraint), "foreignKey")]
public abstract record TableConstraint(string Name);
public sealed record PrimaryKeyConstraint(string Name, IReadOnlyList<string> Columns) : TableConstraint(Name);
public sealed record UniqueConstraint(string Name, IReadOnlyList<string> Columns) : TableConstraint(Name);
public sealed record CheckConstraint(string Name, SqlExpression Expression) : TableConstraint(Name);
public sealed record ForeignKeyConstraint(string Name, IReadOnlyList<string> Columns, string ReferencedTable, IReadOnlyList<string> ReferencedColumns, string? ReferencedSchema = null, ReferentialAction? OnDelete = null, ReferentialAction? OnUpdate = null) : TableConstraint(Name);
public sealed record QualifiedObjectName(string Name, string? Schema = null);

public sealed class DatabaseObjectIdentity : IEquatable<DatabaseObjectIdentity>
{
    [JsonConstructor]
    public DatabaseObjectIdentity(DatabaseObjectKind kind, string name, string? schema = null) => (Kind, Name, Schema) = (kind, name, schema);
    public DatabaseObjectKind Kind { get; }
    public string Name { get; }
    public string? Schema { get; }
    public bool Equals(DatabaseObjectIdentity? other) => other is not null && Kind == other.Kind && StringComparer.OrdinalIgnoreCase.Equals(Name, other.Name) && StringComparer.OrdinalIgnoreCase.Equals(Schema, other.Schema);
    public override bool Equals(object? obj) => Equals(obj as DatabaseObjectIdentity);
    public override int GetHashCode() => HashCode.Combine(Kind, StringComparer.OrdinalIgnoreCase.GetHashCode(Name ?? string.Empty), Schema is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Schema));
    public override string ToString() => Schema is null ? $"{Kind}:{Name}" : $"{Kind}:{Schema}.{Name}";
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(TableDefinition), "table")]
[JsonDerivedType(typeof(DatabaseDefinition), "database")]
public interface IDatabaseObject
{
    [JsonIgnore] DatabaseObjectKind ObjectKind { get; }
    [JsonIgnore] DatabaseObjectIdentity Identity { get; }
    IReadOnlyList<DatabaseObjectIdentity>? DependsOn { get; }
}

public sealed record TableDefinition(string Name, IReadOnlyList<ColumnDefinition> Columns, IReadOnlyList<TableConstraint>? Constraints = null, string? Schema = null) : IDatabaseObject
{
    [JsonIgnore] public DatabaseObjectKind ObjectKind => DatabaseObjectKind.Table;
    [JsonIgnore] public DatabaseObjectIdentity Identity => new(ObjectKind, Name, Schema);
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public IReadOnlyList<DatabaseObjectIdentity>? DependsOn { get; init; }
}

public sealed record DatabaseDefinition(string Name) : IDatabaseObject
{
    [JsonIgnore] public DatabaseObjectKind ObjectKind => DatabaseObjectKind.Database;
    [JsonIgnore] public DatabaseObjectIdentity Identity => new(ObjectKind, Name);
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public IReadOnlyList<DatabaseObjectIdentity>? DependsOn { get; init; }
}

public sealed record SqlDefinitionDocument(int FormatVersion, IReadOnlyList<IDatabaseObject> Objects)
{
    public const int CurrentFormatVersion = 1;
}

public enum GeneratedSqlStatementKind { CreateTable, CreateDatabase }
public sealed record GeneratedSqlStatement(DatabaseObjectIdentity Source, GeneratedSqlStatementKind Kind, string Sql, int DeclarationOrder, int GeneratedOrder);
public sealed record GeneratedSqlDocument(string Sql)
{
    public IReadOnlyList<GeneratedSqlStatement> Statements { get; init; } = [];
}
public sealed record ValidationError(string Path, string Message);
public sealed record ValidationResult(IReadOnlyList<ValidationError> Errors) { public bool IsValid => Errors.Count == 0; }
