using SqlScriptGen.Core;

namespace SqlScriptGen.Core.Tests;

public sealed class ConstraintColumnCasingTests
{
    [Theory]
    [InlineData("primaryKey")]
    [InlineData("unique")]
    [InlineData("foreignKey")]
    public void ExactCaseLocalConstraintColumn_IsAccepted(string kind) => Assert.True(Validate(kind, "ID").IsValid);

    [Theory]
    [InlineData("primaryKey")]
    [InlineData("unique")]
    [InlineData("foreignKey")]
    public void MismatchedCaseLocalConstraintColumn_IsRejectedAtIndexedPath(string kind)
    {
        var error = Assert.Single(Validate(kind, "id").Errors, x => x.Path == "constraints[0].columns[0]");
        Assert.Contains("id", error.Message);
    }

    private static ValidationResult Validate(string kind, string column)
    {
        TableConstraint constraint = kind switch
        {
            "primaryKey" => new PrimaryKeyConstraint("pk", [column]),
            "unique" => new UniqueConstraint("uq", [column]),
            _ => new ForeignKeyConstraint("fk", [column], "External", ["ID"])
        };
        return SqlDefinitionValidator.Validate(new TableDefinition("Parent", [new("ID", new("int"))], [constraint]), DatabaseDialect.PostgreSql);
    }
}
