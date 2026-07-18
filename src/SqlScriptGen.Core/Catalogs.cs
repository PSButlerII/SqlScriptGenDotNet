namespace SqlScriptGen.Core;

public sealed record SqlTypeDescriptor(string Name, bool AllowsLength = false, bool AllowsPrecisionScale = false);

public static class SqlTypeCatalogs
{
    private static readonly IReadOnlyDictionary<string, SqlTypeDescriptor> PostgreSqlTypes = Build(
        new("smallint"), new("int"), new("integer"), new("bigint"), new("decimal", AllowsPrecisionScale: true), new("numeric", AllowsPrecisionScale: true),
        new("real"), new("double precision"), new("money"), new("varchar", AllowsLength: true), new("char", AllowsLength: true), new("text"),
        new("bytea"), new("timestamp"), new("date"), new("time"), new("interval"), new("boolean"), new("bit", AllowsLength: true),
        new("varbit", AllowsLength: true), new("uuid"), new("xml"), new("json"), new("jsonb"), new("point"), new("inet"));
    private static readonly IReadOnlyDictionary<string, SqlTypeDescriptor> MySqlTypes = Build(
        new("tinyint"), new("smallint"), new("mediumint"), new("int"), new("bigint"), new("decimal", AllowsPrecisionScale: true),
        new("float"), new("double"), new("bit", AllowsLength: true), new("char", AllowsLength: true), new("varchar", AllowsLength: true),
        new("tinytext"), new("text"), new("mediumtext"), new("longtext"), new("date"), new("datetime"), new("timestamp"), new("time"),
        new("year"), new("binary", AllowsLength: true), new("varbinary", AllowsLength: true), new("blob"), new("json"), new("boolean"));
    public static IReadOnlyDictionary<string, SqlTypeDescriptor> For(DatabaseDialect dialect) => dialect == DatabaseDialect.PostgreSql ? PostgreSqlTypes : MySqlTypes;
    private static IReadOnlyDictionary<string, SqlTypeDescriptor> Build(params SqlTypeDescriptor[] types) => types.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
}
