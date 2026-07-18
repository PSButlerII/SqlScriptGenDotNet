using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SqlScriptGen.Core;

public static class DefinitionJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();
    public static TableDefinition Deserialize(string json) => JsonSerializer.Deserialize<TableDefinition>(json, Options) ?? throw new JsonException("The document is empty.");
    public static string Serialize(TableDefinition table) => JsonSerializer.Serialize(table, Options);
    private static JsonSerializerOptions CreateOptions()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(typeInfo =>
        {
            if (typeInfo.Type != typeof(TableDefinition)) return;
            var dependencyProperty = typeInfo.Properties.FirstOrDefault(property => property.Name == nameof(TableDefinition.DependsOn));
            if (dependencyProperty is not null) typeInfo.Properties.Remove(dependencyProperty);
        });
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, TypeInfoResolver = resolver };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
