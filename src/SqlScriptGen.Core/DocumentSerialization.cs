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

        ValidateCanonicalStructure(parsed.RootElement);
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

    private static void ValidateCanonicalStructure(JsonElement root)
    {
        var objects = RequireArray(root, "objects", "objects");

        var objectIndex = 0;
        foreach (var databaseObject in objects.EnumerateArray())
        {
            var objectPath = $"objects[{objectIndex}]";
            RequireObject(databaseObject, objectPath);
            var kind = RequireString(databaseObject, "kind", $"{objectPath}.kind");
            RequireString(databaseObject, "name", $"{objectPath}.name");
            ValidateDependencies(databaseObject, objectPath);
            if (kind.Equals("table", StringComparison.Ordinal)) ValidateTable(databaseObject, objectPath);
            objectIndex++;
        }
    }

    private static void ValidateTable(JsonElement table, string path)
    {
        var columns = RequireArray(table, "columns", $"{path}.columns");
        var columnIndex = 0;
        foreach (var column in columns.EnumerateArray())
        {
            var columnPath = $"{path}.columns[{columnIndex}]";
            RequireObject(column, columnPath);
            RequireString(column, "name", $"{columnPath}.name");
            var type = RequireObjectProperty(column, "type", $"{columnPath}.type");
            RequireString(type, "name", $"{columnPath}.type.name");
            columnIndex++;
        }

        if (!TryGetProperty(table, "constraints", out var constraints) || constraints.ValueKind == JsonValueKind.Null) return;
        if (constraints.ValueKind != JsonValueKind.Array) throw JsonError($"{path}.constraints", "A JSON array or null is required.");
        var constraintIndex = 0;
        foreach (var constraint in constraints.EnumerateArray())
        {
            var constraintPath = $"{path}.constraints[{constraintIndex}]";
            RequireObject(constraint, constraintPath);
            var kind = RequireString(constraint, "kind", $"{constraintPath}.kind");
            RequireString(constraint, "name", $"{constraintPath}.name");
            switch (kind)
            {
                case "primaryKey":
                case "unique":
                    ValidateStringArray(constraint, "columns", $"{constraintPath}.columns");
                    break;
                case "check":
                    var expression = RequireObjectProperty(constraint, "expression", $"{constraintPath}.expression");
                    RequireString(expression, "value", $"{constraintPath}.expression.value");
                    break;
                case "foreignKey":
                    ValidateStringArray(constraint, "columns", $"{constraintPath}.columns");
                    RequireString(constraint, "referencedTable", $"{constraintPath}.referencedTable");
                    ValidateStringArray(constraint, "referencedColumns", $"{constraintPath}.referencedColumns");
                    ValidateOptionalStringOrNull(constraint, "onDelete", $"{constraintPath}.onDelete");
                    ValidateOptionalStringOrNull(constraint, "onUpdate", $"{constraintPath}.onUpdate");
                    break;
            }
            constraintIndex++;
        }
    }

    private static void ValidateDependencies(JsonElement databaseObject, string path)
    {
        if (!TryGetProperty(databaseObject, "dependsOn", out var dependencies)) return;
        if (dependencies.ValueKind != JsonValueKind.Array) throw JsonError($"{path}.dependsOn", "A non-null JSON array is required.");
        var dependencyIndex = 0;
        foreach (var dependency in dependencies.EnumerateArray())
        {
            var dependencyPath = $"{path}.dependsOn[{dependencyIndex}]";
            RequireObject(dependency, dependencyPath);
            RequireString(dependency, "kind", $"{dependencyPath}.kind");
            RequireString(dependency, "name", $"{dependencyPath}.name");
            dependencyIndex++;
        }
    }

    private static void ValidateStringArray(JsonElement parent, string propertyName, string path)
    {
        var values = RequireArray(parent, propertyName, path);
        var index = 0;
        foreach (var value in values.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.String) throw JsonError($"{path}[{index}]", "A non-null string is required.");
            index++;
        }
    }

    private static void ValidateOptionalStringOrNull(JsonElement parent, string propertyName, string path)
    {
        if (!TryGetProperty(parent, propertyName, out var value) || value.ValueKind is JsonValueKind.String or JsonValueKind.Null) return;
        throw JsonError(path, "A string or null is required.");
    }

    private static JsonElement RequireArray(JsonElement parent, string propertyName, string path)
    {
        if (!TryGetProperty(parent, propertyName, out var value)) throw JsonError(path, "The required array is missing.");
        if (value.ValueKind != JsonValueKind.Array) throw JsonError(path, "A non-null JSON array is required.");
        return value;
    }

    private static JsonElement RequireObjectProperty(JsonElement parent, string propertyName, string path)
    {
        if (!TryGetProperty(parent, propertyName, out var value)) throw JsonError(path, "The required object is missing.");
        RequireObject(value, path);
        return value;
    }

    private static void RequireObject(JsonElement value, string path)
    {
        if (value.ValueKind != JsonValueKind.Object) throw JsonError(path, "A non-null JSON object is required.");
    }

    private static string RequireString(JsonElement parent, string propertyName, string path)
    {
        if (!TryGetProperty(parent, propertyName, out var value)) throw JsonError(path, "The required string is missing.");
        if (value.ValueKind != JsonValueKind.String) throw JsonError(path, "A non-null string is required.");
        return value.GetString()!;
    }

    private static JsonException JsonError(string path, string message) => new($"{path}: {message}", path, null, null);

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
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new StrictReferentialActionJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private sealed class StrictReferentialActionJsonConverter : JsonConverter<ReferentialAction>
    {
        public override ReferentialAction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String) throw new JsonException("A referential action must be a string.");
            return reader.GetString() switch
            {
                string value when value.Equals("noAction", StringComparison.OrdinalIgnoreCase) => ReferentialAction.NoAction,
                string value when value.Equals("restrict", StringComparison.OrdinalIgnoreCase) => ReferentialAction.Restrict,
                string value when value.Equals("cascade", StringComparison.OrdinalIgnoreCase) => ReferentialAction.Cascade,
                string value when value.Equals("setNull", StringComparison.OrdinalIgnoreCase) => ReferentialAction.SetNull,
                string value when value.Equals("setDefault", StringComparison.OrdinalIgnoreCase) => ReferentialAction.SetDefault,
                _ => throw new JsonException("The referential action is not supported.")
            };
        }

        public override void Write(Utf8JsonWriter writer, ReferentialAction value, JsonSerializerOptions options) => writer.WriteStringValue(value switch
        {
            ReferentialAction.NoAction => "noAction",
            ReferentialAction.Restrict => "restrict",
            ReferentialAction.Cascade => "cascade",
            ReferentialAction.SetNull => "setNull",
            ReferentialAction.SetDefault => "setDefault",
            _ => throw new JsonException($"Referential action '{value}' is not supported.")
        });
    }
}
