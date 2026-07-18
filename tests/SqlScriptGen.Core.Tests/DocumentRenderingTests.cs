using SqlScriptGen.Core;

namespace SqlScriptGen.Core.Tests;

public sealed class DocumentRenderingTests
{
    [Fact] public void PostgreSqlMultiTable_IsExact() { var child = Table("child") with { DependsOn = [Id("parent")] }; var actual = new SqlGenerator().Generate(Document(child, Table("parent")), DatabaseDialect.PostgreSql); Assert.Equal("CREATE TABLE \"parent\" (\n    \"id\" int NOT NULL\n);\n\nCREATE TABLE \"child\" (\n    \"id\" int NOT NULL\n);\n", actual.Sql); Assert.Equal(["parent", "child"], actual.Statements.Select(x => x.Source.Name)); }
    [Fact] public void MySqlMultiTable_IsExact() { var child = Table("child") with { DependsOn = [Id("parent")] }; var actual = new SqlGenerator().Generate(Document(child, Table("parent")), DatabaseDialect.MySql); Assert.Equal("CREATE TABLE `parent` (\n    `id` INT NOT NULL\n);\n\nCREATE TABLE `child` (\n    `id` INT NOT NULL\n);\n", actual.Sql); }
    [Theory, InlineData(DatabaseDialect.PostgreSql, "CREATE DATABASE \"first\";\n\nCREATE DATABASE \"second\";\n"), InlineData(DatabaseDialect.MySql, "CREATE DATABASE `first`;\n\nCREATE DATABASE `second`;\n")]
    public void MultiDatabase_IsExact(DatabaseDialect dialect, string expected) => Assert.Equal(expected, new SqlGenerator().Generate(Document(new DatabaseDefinition("first"), new DatabaseDefinition("second")), dialect).Sql);
    [Fact] public void SingleDocumentStatement_MatchesLegacyOverload() { var table = Table("t"); var generator = new SqlGenerator(); Assert.Equal(generator.Generate(table, DatabaseDialect.PostgreSql).Sql, generator.Generate(Document(table), DatabaseDialect.PostgreSql).Sql); }
    [Fact] public void OutputHasOneFinalLfAndNoTrailingWhitespace() { var sql = new SqlGenerator().Generate(Document(Table("a"), Table("b")), DatabaseDialect.PostgreSql).Sql; Assert.EndsWith(";\n", sql); Assert.False(sql.EndsWith(";\n\n", StringComparison.Ordinal)); Assert.DoesNotContain("\r", sql); Assert.DoesNotContain(" \n", sql); }
    [Fact] public void StatementsRecordDeclarationAndGeneratedOrder() { var b = Table("b") with { DependsOn = [Id("a")] }; var statements = new SqlGenerator().Generate(Document(b, Table("a")), DatabaseDialect.PostgreSql).Statements; Assert.Equal((1, 0), (statements[0].DeclarationOrder, statements[0].GeneratedOrder)); Assert.Equal((0, 1), (statements[1].DeclarationOrder, statements[1].GeneratedOrder)); }
    private static SqlDefinitionDocument Document(params IDatabaseObject[] objects) => new(1, objects);
    private static TableDefinition Table(string name) => new(name, [new("id", new("int"), false)]);
    private static DatabaseObjectIdentity Id(string name) => new(DatabaseObjectKind.Table, name);
}
