using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqlScriptGen.Core;

public static class SqlDefinitionDocumentJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    public static SqlDefinitionDocument Read(string json)
    {
        using var parsed = JsonDocument.Parse(json);
        if (parsed.RootElement.ValueKind != JsonValueKind.Object) throw new JsonException("$: A JSON object is required.");

        var hasVersion = TryGetProperty(parsed.RootElement, "formatVersion", out var versionElement);
        var hasObjects = TryGetProperty(parsed.RootElement, "objects", out _);
        if (!hasVersion && !hasObjects)
        {
            var legacyTable = DefinitionJson.Deserialize(json);
            return new(SqlDefinitionDocument.CurrentFormatVersion, [legacyTable]);
        }

        if (!hasVersion) throw new JsonException("formatVersion: The canonical document format version is required.");
        if (versionElement.ValueKind != JsonValueKind.Number || !versionElement.TryGetInt32(out var version)) throw new JsonException("formatVersion: An integer is required.");
        if (version != SqlDefinitionDocument.CurrentFormatVersion) throw new JsonException($"formatVersion: Version {version} is unsupported; only version {SqlDefinitionDocument.CurrentFormatVersion} is supported.");
        if (!hasObjects) throw new JsonException("objects: The canonical object collection is required.");

        return JsonSerializer.Deserialize<SqlDefinitionDocument>(json, Options) ?? throw new JsonException("$: The document is empty.");
    }

    public static string Serialize(SqlDefinitionDocument document)
    {
        if (document.FormatVersion != SqlDefinitionDocument.CurrentFormatVersion) throw new ArgumentException($"Only document format version {SqlDefinitionDocument.CurrentFormatVersion} can be serialized.", nameof(document));
        return JsonSerializer.Serialize(document, Options).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
