using System.Text.Json;
using System.Text.Json.Nodes;
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

        ValidateCanonicalDiscriminators(parsed.RootElement);
        return JsonSerializer.Deserialize<SqlDefinitionDocument>(NormalizeDiscriminatorPropertyNames(json), Options) ?? throw new JsonException("$: The document is empty.");
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

    private static void ValidateCanonicalDiscriminators(JsonElement root)
    {
        if (!TryGetProperty(root, "objects", out var objects) || objects.ValueKind != JsonValueKind.Array) return;

        var objectIndex = 0;
        foreach (var databaseObject in objects.EnumerateArray())
        {
            var objectPath = $"objects[{objectIndex}]";
            if (databaseObject.ValueKind != JsonValueKind.Object) throw new JsonException($"{objectPath}: A JSON object is required.");
            var kind = RequireStringDiscriminator(databaseObject, objectPath);
            if (kind.Equals("table", StringComparison.Ordinal) && TryGetProperty(databaseObject, "constraints", out var constraints) && constraints.ValueKind == JsonValueKind.Array)
            {
                var constraintIndex = 0;
                foreach (var constraint in constraints.EnumerateArray())
                {
                    if (constraint.ValueKind == JsonValueKind.Object) RequireStringDiscriminator(constraint, $"{objectPath}.constraints[{constraintIndex}]");
                    constraintIndex++;
                }
            }
            objectIndex++;
        }
    }

    private static string RequireStringDiscriminator(JsonElement element, string path)
    {
        if (!TryGetProperty(element, "kind", out var kind)) throw new JsonException($"{path}.kind: The required discriminator is missing.");
        if (kind.ValueKind != JsonValueKind.String) throw new JsonException($"{path}.kind: A string discriminator is required.");
        return kind.GetString()!;
    }

    private static string NormalizeDiscriminatorPropertyNames(string json)
    {
        var root = JsonNode.Parse(json)!.AsObject();
        if (GetProperty(root, "objects") is not JsonArray objects) return json;
        var changed = false;
        foreach (var node in objects)
        {
            if (node is not JsonObject databaseObject) continue;
            var kind = NormalizeDiscriminatorPropertyName(databaseObject, ref changed);
            if (!string.Equals(kind, "table", StringComparison.Ordinal) || GetProperty(databaseObject, "constraints") is not JsonArray constraints) continue;
            foreach (var constraint in constraints.OfType<JsonObject>()) NormalizeDiscriminatorPropertyName(constraint, ref changed);
        }
        return changed ? root.ToJsonString() : json;
    }

    private static string? NormalizeDiscriminatorPropertyName(JsonObject value, ref bool changed)
    {
        if (value.TryGetPropertyValue("kind", out var exact)) return exact?.GetValue<string>();
        var property = value.FirstOrDefault(x => x.Key.Equals("kind", StringComparison.OrdinalIgnoreCase));
        if (property.Key is null) return null;
        value.Remove(property.Key);
        value["kind"] = property.Value;
        changed = true;
        return property.Value?.GetValue<string>();
    }

    private static JsonNode? GetProperty(JsonObject value, string name) => value.FirstOrDefault(x => x.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Value;

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            AllowOutOfOrderMetadataProperties = true,
            RespectRequiredConstructorParameters = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
