using System.Text.Json;
using SqlScriptGen.Core;

namespace SqlScriptGen.Core.Tests;

public sealed class DocumentSerializationTests
{
    [Fact] public void MissingDependencyKind_IsRejectedEvenWhenTargetExists() { var ex = Assert.Throws<JsonException>(() => SqlDefinitionDocumentJson.Read(DependencyDocument("{\"name\":\"parent\"}"))); Assert.Contains("dependsOn[0]", ex.Path ?? string.Empty); }
    [Fact] public void MissingDependencyName_IsRejected() { var ex = Assert.Throws<JsonException>(() => SqlDefinitionDocumentJson.Read(DependencyDocument("{\"kind\":\"table\"}"))); Assert.Contains("dependsOn[0]", ex.Path ?? string.Empty); }
    [Fact] public void ValidDependency_DeserializesRequiredIdentity() { var document = SqlDefinitionDocumentJson.Read(DependencyDocument("{\"kind\":\"table\",\"name\":\"parent\"}")); var dependency = Assert.Single(document.Objects[1].DependsOn!); Assert.Equal(DatabaseObjectKind.Table, dependency.Kind); Assert.Equal("parent", dependency.Name); }
    [Fact] public void UnknownDependencyKind_IsRejected() => Assert.Throws<JsonException>(() => SqlDefinitionDocumentJson.Read(DependencyDocument("{\"kind\":\"view\",\"name\":\"parent\"}")));
    [Fact] public void CanonicalDocument_RoundTrips() { var value = Document(Table("a"), new DatabaseDefinition("db")); var roundTrip = SqlDefinitionDocumentJson.Read(SqlDefinitionDocumentJson.Serialize(value)); Assert.Collection(roundTrip.Objects, x => Assert.IsType<TableDefinition>(x), x => Assert.IsType<DatabaseDefinition>(x)); }
    [Fact] public void LegacyTable_IsAdapted() { var value = SqlDefinitionDocumentJson.Read("{\"name\":\"legacy\",\"columns\":[{\"name\":\"id\",\"type\":{\"name\":\"int\"}}]}"); Assert.Equal(SqlDefinitionDocument.CurrentFormatVersion, value.FormatVersion); Assert.Equal("legacy", Assert.IsType<TableDefinition>(Assert.Single(value.Objects)).Name); }
    [Fact] public void LegacySerialization_DoesNotGainDocumentMetadata() => Assert.DoesNotContain("DependsOn", DefinitionJson.Serialize(Table("legacy")));
    [Fact] public void CanonicalSerialization_EmitsEnvelopeOnly() { var json = SqlDefinitionDocumentJson.Serialize(Document(Table("a"))); Assert.StartsWith("{\n  \"formatVersion\": 1,\n  \"objects\": [", json); Assert.Contains("\"kind\": \"table\"", json); Assert.EndsWith("\n", json); Assert.DoesNotContain("\r", json); }
    [Fact] public void UnknownFormatVersion_FailsClearly() { var ex = Assert.Throws<JsonException>(() => SqlDefinitionDocumentJson.Read("{\"formatVersion\":2,\"objects\":[]}")); Assert.Contains("formatVersion", ex.Message); Assert.Contains("unsupported", ex.Message); }
    [Fact] public void MissingFormatVersion_FailsClearly() { var ex = Assert.Throws<JsonException>(() => SqlDefinitionDocumentJson.Read("{\"objects\":[]}")); Assert.Contains("formatVersion", ex.Message); }
    [Fact] public void UnknownObjectKind_Fails() { var ex = Assert.Throws<JsonException>(() => SqlDefinitionDocumentJson.Read("{\"formatVersion\":1,\"objects\":[{\"kind\":\"view\",\"name\":\"v\"}]}")); Assert.Contains("discriminator", ex.Message, StringComparison.OrdinalIgnoreCase); }
    [Fact] public void UnknownMember_Fails() => Assert.Throws<JsonException>(() => SqlDefinitionDocumentJson.Read("{\"formatVersion\":1,\"objects\":[{\"kind\":\"database\",\"name\":\"db\",\"mystery\":true}]}"));
    [Fact] public void PropertyNames_AreCaseInsensitive() { var value = SqlDefinitionDocumentJson.Read("{\"FORMATVERSION\":1,\"OBJECTS\":[{\"kind\":\"database\",\"NAME\":\"db\"}]}"); Assert.Equal("db", Assert.IsType<DatabaseDefinition>(Assert.Single(value.Objects)).Name); }
    [Fact] public void ObjectDeclarationOrder_IsPreserved() { var value = SqlDefinitionDocumentJson.Read(SqlDefinitionDocumentJson.Serialize(Document(Table("z"), Table("a")))); Assert.Equal(["z", "a"], value.Objects.Select(x => x.Identity.Name)); }
    [Fact] public void ConstraintPolymorphism_IsPreserved() { var table = new TableDefinition("t", [new("id", new("int"))], [new PrimaryKeyConstraint("pk", ["id"])]); var value = SqlDefinitionDocumentJson.Read(SqlDefinitionDocumentJson.Serialize(Document(table))); Assert.IsType<PrimaryKeyConstraint>(Assert.Single(Assert.IsType<TableDefinition>(Assert.Single(value.Objects)).Constraints!)); }
    [Fact] public void ExplicitDependencies_RoundTrip() { var table = Table("b") with { DependsOn = [new(DatabaseObjectKind.Table, "a", "public")] }; var roundTrip = SqlDefinitionDocumentJson.Read(SqlDefinitionDocumentJson.Serialize(Document(table))); Assert.Equal(new DatabaseObjectIdentity(DatabaseObjectKind.Table, "A", "PUBLIC"), Assert.Single(roundTrip.Objects[0].DependsOn!)); }
    [Fact] public void UnsupportedVersion_CannotBeSerialized() => Assert.Throws<ArgumentException>(() => SqlDefinitionDocumentJson.Serialize(new(2, [Table("a")])));
    private static SqlDefinitionDocument Document(params IDatabaseObject[] objects) => new(1, objects);
    private static TableDefinition Table(string name) => new(name, [new("id", new("int"))]);
    private static string DependencyDocument(string dependency) => "{\"formatVersion\":1,\"objects\":[{\"kind\":\"table\",\"name\":\"parent\",\"columns\":[{\"name\":\"id\",\"type\":{\"name\":\"int\"}}]},{\"kind\":\"table\",\"name\":\"child\",\"dependsOn\":[DEPENDENCY],\"columns\":[{\"name\":\"id\",\"type\":{\"name\":\"int\"}}]}]}".Replace("DEPENDENCY", dependency, StringComparison.Ordinal);
}
