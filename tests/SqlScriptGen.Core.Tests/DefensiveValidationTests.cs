using SqlScriptGen.Core;

namespace SqlScriptGen.Core.Tests;

public sealed class DefensiveValidationTests
{
    [Fact] public void NullColumns_ReturnsValidationError() => Invalid(new("t", null!), "columns");
    [Fact] public void NullColumnEntry_ReturnsIndexedError() => Invalid(new("t", [null!]), "columns[0]");
    [Fact] public void NullColumnType_ReturnsIndexedError() => Invalid(new("t", [new("id", null!)]), "columns[0].type");
    [Fact] public void NullConstraintEntry_ReturnsIndexedError() => Invalid(new TableDefinition("t", [new("id", new("int"))], [null!]), "constraints[0]");
    [Fact] public void NullPrimaryKeyColumns_ReturnsIndexedError() => Invalid(Table(new PrimaryKeyConstraint("pk", null!)), "constraints[0].columns");
    [Fact] public void NullCheckExpression_ReturnsIndexedError() => Invalid(Table(new CheckConstraint("ck", null!)), "constraints[0].expression");
    [Fact] public void NullForeignKeyColumns_ReturnsIndexedError() => Invalid(Table(new ForeignKeyConstraint("fk", null!, "parent", ["id"])), "constraints[0].columns");
    [Fact] public void NullForeignKeyReferencedColumns_ReturnsIndexedError() => Invalid(Table(new ForeignKeyConstraint("fk", ["id"], "parent", null!)), "constraints[0].referencedColumns");
    [Fact] public void NullConstraintColumnName_ReturnsIndexedError() => Invalid(Table(new UniqueConstraint("uq", [null!])), "constraints[0].columns[0]");
    [Fact] public void Generator_ConvertsMalformedTableToValidationException() => Assert.Throws<SqlValidationException>(() => new SqlGenerator().Generate(new TableDefinition("t", null!), DatabaseDialect.PostgreSql));
    [Fact] public void DocumentGenerator_ConvertsMalformedNestedModelToValidationException() { var table = Table(new CheckConstraint("ck", null!)); var document = new SqlDefinitionDocument(1, [table]); Assert.Throws<SqlValidationException>(() => new SqlGenerator().Generate(document, DatabaseDialect.PostgreSql)); }
    [Fact] public void NullDocumentObject_ReturnsErrorAndGeneratorThrowsValidationException() { var document = new SqlDefinitionDocument(1, [null!]); var result = SqlDefinitionDocumentValidator.Validate(document, DatabaseDialect.PostgreSql); Assert.Contains(result.Errors, x => x.Path == "objects[0]"); Assert.Throws<SqlValidationException>(() => new SqlGenerator().Generate(document, DatabaseDialect.PostgreSql)); }
    [Fact] public void NullDependency_ReturnsIndexedErrorWithoutGraphConstruction() { var table = Table() with { DependsOn = [null!] }; var result = SqlDefinitionDocumentValidator.Validate(new(1, [table]), DatabaseDialect.PostgreSql); Assert.Contains(result.Errors, x => x.Path == "objects[0].dependsOn[0]"); }
    private static TableDefinition Table(params TableConstraint[] constraints) => new("t", [new("id", new("int"))], constraints);
    private static void Invalid(TableDefinition table, string path) { var result = SqlDefinitionValidator.Validate(table, DatabaseDialect.PostgreSql); Assert.Contains(result.Errors, x => x.Path == path); }
}
