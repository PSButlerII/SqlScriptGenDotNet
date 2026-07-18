using SqlScriptGen.Core;

namespace SqlScriptGen.Core.Tests;

public sealed class ValidationAndJsonTests
{
    [Fact] public void DuplicateColumns_AreRejected() => Invalid(new("t", [new("id", new("int")), new("ID", new("int"))]), "Duplicate");
    [Fact] public void MissingConstraintColumn_IsRejected() => Invalid(new("t", [new("id", new("int"))], [new PrimaryKeyConstraint("pk", ["missing"])]), "does not exist");
    [Fact] public void InvalidIdentifier_IsRejected() => Invalid(new("bad-name", [new("id", new("int"))]), "Identifier");
    [Fact] public void UnsupportedType_IsRejected() => Invalid(new("t", [new("id", new("jsonb"))]), "not supported", DatabaseDialect.MySql);
    [Fact] public void BrokenForeignKey_IsRejected() => Invalid(new("t", [new("a", new("int")), new("b", new("int"))], [new ForeignKeyConstraint("fk", ["a", "b"], "p", ["id"])]), "counts must match");
    [Fact] public void InvalidLength_IsRejected() => Invalid(new("t", [new("a", new("varchar", 0))]), "Length");
    [Fact] public void InvalidScale_IsRejected() => Invalid(new("t", [new("a", new("decimal", Precision: 2, Scale: 3))]), "Scale");
    [Fact] public void EmptyTable_IsRejected() => Invalid(new("t", []), "At least one column");
    [Fact] public void IdentityAndDefault_AreRejected() => Invalid(new("t", [new("id", new("int"), Default: new("1"), Identity: true)]), "Identity and default");
    [Fact] public void MultiplePrimaryKeys_AreRejected() => Invalid(new("t", [new("id", new("int"))], [new PrimaryKeyConstraint("pk1", ["id"]), new PrimaryKeyConstraint("pk2", ["id"])]), "Only one");
    [Fact] public void Json_RoundTripsPolymorphicConstraints() { var value = new TableDefinition("t", [new("id", new("bigint"), false)], [new PrimaryKeyConstraint("pk", ["id"])]); var roundTrip = DefinitionJson.Deserialize(DefinitionJson.Serialize(value)); Assert.IsType<PrimaryKeyConstraint>(Assert.Single(roundTrip.Constraints!)); }
    [Fact] public void Json_IsCaseInsensitive() { var value = DefinitionJson.Deserialize("{\"NAME\":\"t\",\"COLUMNS\":[{\"NAME\":\"id\",\"TYPE\":{\"NAME\":\"int\"}}]}"); Assert.Equal("t", value.Name); }
    private static void Invalid(TableDefinition table, string expected, DatabaseDialect dialect = DatabaseDialect.PostgreSql) { var ex = Assert.Throws<SqlValidationException>(() => new SqlGenerator().Generate(table, dialect)); Assert.Contains(ex.Errors, x => x.Message.Contains(expected, StringComparison.OrdinalIgnoreCase)); }
}
