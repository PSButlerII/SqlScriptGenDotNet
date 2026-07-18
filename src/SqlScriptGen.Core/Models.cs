using System.Text.Json.Serialization;

namespace SqlScriptGen.Core;

[JsonConverter(typeof(JsonStringEnumConverter<DatabaseDialect>))]
public enum DatabaseDialect { PostgreSql, MySql }

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
public sealed record TableDefinition(string Name, IReadOnlyList<ColumnDefinition> Columns, IReadOnlyList<TableConstraint>? Constraints = null, string? Schema = null);
public sealed record DatabaseDefinition(string Name);
public sealed record GeneratedSqlDocument(string Sql);
public sealed record ValidationError(string Path, string Message);
public sealed record ValidationResult(IReadOnlyList<ValidationError> Errors) { public bool IsValid => Errors.Count == 0; }
