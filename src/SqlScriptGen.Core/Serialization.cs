using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqlScriptGen.Core;

public static class DefinitionJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();
    public static TableDefinition Deserialize(string json) => JsonSerializer.Deserialize<TableDefinition>(json, Options) ?? throw new JsonException("The document is empty.");
    public static string Serialize(TableDefinition table) => JsonSerializer.Serialize(table, Options);
    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
