using SqlScriptGen.Core;

namespace SqlScriptGen.Core.Tests;

public sealed class DocumentValidationAndOrderingTests
{
    [Fact] public void Identity_IsCaseInsensitiveAndStable() { var a = new DatabaseObjectIdentity(DatabaseObjectKind.Table, "Users", "Public"); var b = new DatabaseObjectIdentity(DatabaseObjectKind.Table, "users", "public"); Assert.Equal(a, b); Assert.Equal(a.GetHashCode(), b.GetHashCode()); }
    [Fact] public void QualifiedAndUnqualifiedTables_Differ() => Assert.NotEqual(new DatabaseObjectIdentity(DatabaseObjectKind.Table, "t"), new DatabaseObjectIdentity(DatabaseObjectKind.Table, "t", "public"));
    [Fact] public void DatabaseAndTableKinds_Differ() => Assert.NotEqual(new DatabaseObjectIdentity(DatabaseObjectKind.Database, "x"), new DatabaseObjectIdentity(DatabaseObjectKind.Table, "x"));
    [Fact] public void DuplicateObjects_AreRejected() => Invalid(Document(Table("Users"), Table("users")), "Duplicate object", "objects[1].name");
    [Fact] public void EmptyDocument_IsRejected() => Invalid(Document(), "At least one", "objects");
    [Fact] public void MixedDatabaseAndTables_AreRejected() => Invalid(Document(Table("t"), new DatabaseDefinition("db")), "cannot be mixed", "objects");
    [Fact] public void MissingExplicitDependency_IsRejected() { var t = Table("t") with { DependsOn = [Id("missing")] }; Invalid(Document(t), "does not exist", "objects[0].dependsOn[0]"); }
    [Fact] public void SelfDependency_IsRejected() { var t = Table("t") with { DependsOn = [Id("t")] }; Invalid(Document(t), "depend on itself", "objects[0].dependsOn[0]"); }
    [Fact] public void ExternalForeignKey_IsAllowed() { var t = Table("t", new ForeignKeyConstraint("fk", ["id"], "external", ["id"])); Assert.True(SqlDefinitionDocumentValidator.Validate(Document(t), DatabaseDialect.PostgreSql).IsValid); }
    [Fact] public void SelfReferencingForeignKey_DoesNotCreateDependencyCycle() { var t = Table("t", new ForeignKeyConstraint("fk", ["id"], "t", ["id"])); Assert.True(SqlDefinitionDocumentValidator.Validate(Document(t), DatabaseDialect.PostgreSql).IsValid); }
    [Fact] public void InternalForeignKey_OrdersTargetFirst() { var child = Table("child", new ForeignKeyConstraint("fk", ["id"], "parent", ["id"])); Assert.Equal(["parent", "child"], DatabaseObjectOrderer.Order(Document(child, Table("parent"))).Select(x => x.Identity.Name)); }
    [Fact] public void SimpleChain_IsOrdered() { var c = Depends(Table("c"), "b"); var b = Depends(Table("b"), "a"); Assert.Equal(["a", "b", "c"], Names(DatabaseObjectOrderer.Order(Document(c, b, Table("a"))))); }
    [Fact] public void Diamond_IsOrderedStably() { var d = Table("d") with { DependsOn = [Id("b"), Id("c")] }; var b = Depends(Table("b"), "a"); var c = Depends(Table("c"), "a"); Assert.Equal(["a", "c", "b", "d"], Names(DatabaseObjectOrderer.Order(Document(d, c, b, Table("a"))))); }
    [Fact] public void DisconnectedObjects_KeepDeclarationTies() => Assert.Equal(["z", "a", "m"], Names(DatabaseObjectOrderer.Order(Document(Table("z"), Table("a"), Table("m")))));
    [Fact] public void DuplicateEdges_DoNotChangeOrder() { var b = Table("b") with { DependsOn = [Id("a"), Id("A")] }; Assert.Equal(["a", "b"], Names(DatabaseObjectOrderer.Order(Document(b, Table("a"))))); }
    [Fact] public void MultiNodeCycle_IsRejectedWithIdentities() { var a = Depends(Table("a"), "b"); var b = Depends(Table("b"), "a"); var result = SqlDefinitionDocumentValidator.Validate(Document(a, b), DatabaseDialect.PostgreSql); Assert.Contains(result.Errors, x => x.Message.Contains("Table:a") && x.Message.Contains("Table:b")); }
    [Fact] public void Ordering_IsRepeatableAndDoesNotMutateInput() { IDatabaseObject[] input = [Depends(Table("b"), "a"), Table("a"), Table("c")]; var document = Document(input); var first = Names(DatabaseObjectOrderer.Order(document)); var second = Names(DatabaseObjectOrderer.Order(document)); Assert.Equal(first, second); Assert.Equal(["b", "a", "c"], input.Select(x => x.Identity.Name)); }
    [Fact] public void MultipleErrors_AreCollectedWithStablePaths() { var bad = new TableDefinition("bad-name", [new("bad-col", new("missing"))]) with { DependsOn = [Id("absent")] }; var result = SqlDefinitionDocumentValidator.Validate(Document(bad), DatabaseDialect.MySql); Assert.True(result.Errors.Count >= 3); Assert.All(result.Errors, x => Assert.StartsWith("objects", x.Path)); }
    [Fact] public void MySqlCapabilityRejectsSetDefault() { var fk = new ForeignKeyConstraint("fk", ["id"], "external", ["id"], OnDelete: ReferentialAction.SetDefault); Invalid(Document(Table("t", fk)), "referential action", "objects[0].constraints"); }
    [Fact] public void ExistingConstructorsAndOverloads_RemainUsable() { var table = new TableDefinition("t", [new("id", new("int"))]); var database = new DatabaseDefinition("db"); var generator = new SqlGenerator(); Assert.Contains("CREATE TABLE", generator.Generate(table, DatabaseDialect.PostgreSql).Sql); Assert.Contains("CREATE DATABASE", generator.Generate(database, DatabaseDialect.PostgreSql).Sql); }
    private static SqlDefinitionDocument Document(params IDatabaseObject[] objects) => new(1, objects);
    private static TableDefinition Table(string name, params TableConstraint[] constraints) => new(name, [new("id", new("int"))], constraints);
    private static TableDefinition Depends(TableDefinition table, string name) => table with { DependsOn = [Id(name)] };
    private static DatabaseObjectIdentity Id(string name) => new(DatabaseObjectKind.Table, name);
    private static string[] Names(IReadOnlyList<IDatabaseObject> objects) => objects.Select(x => x.Identity.Name).ToArray();
    private static void Invalid(SqlDefinitionDocument document, string message, string path) { var result = SqlDefinitionDocumentValidator.Validate(document, DatabaseDialect.MySql); Assert.Contains(result.Errors, x => x.Path.StartsWith(path, StringComparison.Ordinal) && x.Message.Contains(message, StringComparison.OrdinalIgnoreCase)); }
}
