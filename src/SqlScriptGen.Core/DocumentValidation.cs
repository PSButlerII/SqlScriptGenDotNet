namespace SqlScriptGen.Core;

public sealed record SqlDialectCapabilities(
    DatabaseDialect Dialect,
    IReadOnlySet<DatabaseObjectKind> SupportedObjectKinds,
    IReadOnlySet<ReferentialAction> SupportedReferentialActions,
    bool SupportsSchemaQualifiedTables,
    bool SupportsIdentity,
    bool SupportsAutoIncrement);

public static class SqlDialectCapabilityCatalog
{
    private static readonly IReadOnlyDictionary<DatabaseDialect, SqlDialectCapabilities> Capabilities =
        new Dictionary<DatabaseDialect, SqlDialectCapabilities>
        {
            [DatabaseDialect.PostgreSql] = new(DatabaseDialect.PostgreSql,
                new HashSet<DatabaseObjectKind> { DatabaseObjectKind.Table, DatabaseObjectKind.Database },
                new HashSet<ReferentialAction>(Enum.GetValues<ReferentialAction>()), true, true, false),
            [DatabaseDialect.MySql] = new(DatabaseDialect.MySql,
                new HashSet<DatabaseObjectKind> { DatabaseObjectKind.Table, DatabaseObjectKind.Database },
                new HashSet<ReferentialAction> { ReferentialAction.NoAction, ReferentialAction.Restrict, ReferentialAction.Cascade, ReferentialAction.SetNull },
                true, false, true)
        };

    public static SqlDialectCapabilities For(DatabaseDialect dialect) => Capabilities[dialect];
}

public static class SqlDefinitionDocumentValidator
{
    public static ValidationResult Validate(SqlDefinitionDocument document, DatabaseDialect dialect)
    {
        var errors = new List<ValidationError>();
        if (document.FormatVersion != SqlDefinitionDocument.CurrentFormatVersion)
            errors.Add(new("formatVersion", $"Document format version {document.FormatVersion} is unsupported."));
        if (document.Objects is null || document.Objects.Count == 0)
        {
            errors.Add(new("objects", "At least one database object is required."));
            return new(errors);
        }

        var kinds = document.Objects.Where(x => x is not null).Select(x => x.ObjectKind).Distinct().ToArray();
        if (kinds.Contains(DatabaseObjectKind.Table) && kinds.Contains(DatabaseObjectKind.Database))
            errors.Add(new("objects", "Database and table objects cannot be mixed because they require separate connection contexts."));

        var capabilities = SqlDialectCapabilityCatalog.For(dialect);
        var identities = new Dictionary<DatabaseObjectIdentity, int>();
        for (var i = 0; i < document.Objects.Count; i++)
        {
            var current = document.Objects[i];
            var prefix = $"objects[{i}]";
            if (current is null)
            {
                errors.Add(new(prefix, "Database object cannot be null."));
                continue;
            }
            if (!capabilities.SupportedObjectKinds.Contains(current.ObjectKind))
                errors.Add(new(prefix + ".kind", $"{dialect} does not support object kind '{current.ObjectKind}'."));
            if (!identities.TryAdd(current.Identity, i))
                errors.Add(new(prefix + ".name", $"Duplicate object identity '{current.Identity}'."));

            switch (current)
            {
                case TableDefinition table:
                    AddPrefixed(errors, SqlDefinitionValidator.Validate(table, dialect), prefix);
                    break;
                case DatabaseDefinition database:
                    AddPrefixed(errors, SqlDefinitionValidator.ValidateIdentifier(database.Name, "name"), prefix);
                    break;
                default:
                    errors.Add(new(prefix + ".kind", $"Object type '{current.GetType().Name}' is not supported."));
                    break;
            }
        }

        for (var i = 0; i < document.Objects.Count; i++)
        {
            var current = document.Objects[i];
            if (current is null) continue;
            var dependencies = current.DependsOn ?? [];
            for (var j = 0; j < dependencies.Count; j++)
            {
                var dependency = dependencies[j];
                var path = $"objects[{i}].dependsOn[{j}]";
                if (dependency is null)
                {
                    errors.Add(new(path, "Dependency cannot be null."));
                    continue;
                }
                errors.AddRange(SqlDefinitionValidator.ValidateIdentifier(dependency.Name, path + ".name").Errors);
                if (dependency.Schema is not null) errors.AddRange(SqlDefinitionValidator.ValidateIdentifier(dependency.Schema, path + ".schema").Errors);
                if (dependency.Equals(current.Identity)) errors.Add(new(path, "An object cannot depend on itself."));
                else if (!identities.ContainsKey(dependency)) errors.Add(new(path, $"Explicit dependency '{dependency}' does not exist in this document."));
            }
        }

        if (identities.Count == document.Objects.Count)
        {
            var graph = DatabaseObjectDependencyGraph.Create(document.Objects, identities);
            if (graph.TryOrder(out _, out var cyclicIndexes) is false)
            {
                var cycle = string.Join(", ", cyclicIndexes.Select(index => document.Objects[index].Identity.ToString()));
                errors.Add(new("objects", $"Dependency cycle detected involving: {cycle}."));
            }
        }
        return new(errors);
    }

    private static void AddPrefixed(List<ValidationError> errors, ValidationResult result, string prefix) =>
        errors.AddRange(result.Errors.Select(error => new ValidationError($"{prefix}.{error.Path}", error.Message)));
}

public static class DatabaseObjectOrderer
{
    public static IReadOnlyList<IDatabaseObject> Order(SqlDefinitionDocument document)
    {
        var identities = document.Objects.Select((item, index) => (item.Identity, index)).ToDictionary(x => x.Identity, x => x.index);
        var graph = DatabaseObjectDependencyGraph.Create(document.Objects, identities);
        if (!graph.TryOrder(out var orderedIndexes, out _)) throw new InvalidOperationException("Cannot order a document containing dependency cycles.");
        return orderedIndexes.Select(index => document.Objects[index]).ToArray();
    }
}

internal sealed class DatabaseObjectDependencyGraph
{
    private readonly IReadOnlyList<HashSet<int>> prerequisites;
    private DatabaseObjectDependencyGraph(IReadOnlyList<HashSet<int>> prerequisites) => this.prerequisites = prerequisites;

    public static DatabaseObjectDependencyGraph Create(IReadOnlyList<IDatabaseObject> objects, IReadOnlyDictionary<DatabaseObjectIdentity, int> identities)
    {
        var prerequisites = Enumerable.Range(0, objects.Count).Select(_ => new HashSet<int>()).ToArray();
        for (var i = 0; i < objects.Count; i++)
        {
            foreach (var dependency in objects[i].DependsOn ?? [])
                if (dependency is not null && identities.TryGetValue(dependency, out var target)) prerequisites[i].Add(target);

            if (objects[i] is not TableDefinition table) continue;
            foreach (var foreignKey in (table.Constraints ?? []).OfType<ForeignKeyConstraint>())
            {
                var targetIdentity = new DatabaseObjectIdentity(DatabaseObjectKind.Table, foreignKey.ReferencedTable, foreignKey.ReferencedSchema ?? table.Schema);
                if (identities.TryGetValue(targetIdentity, out var target) && target != i) prerequisites[i].Add(target);
            }
        }
        return new(prerequisites);
    }

    public bool TryOrder(out IReadOnlyList<int> orderedIndexes, out IReadOnlyList<int> cyclicIndexes)
    {
        var indegrees = prerequisites.Select(x => x.Count).ToArray();
        var dependents = Enumerable.Range(0, prerequisites.Count).Select(_ => new List<int>()).ToArray();
        for (var dependent = 0; dependent < prerequisites.Count; dependent++)
            foreach (var prerequisite in prerequisites[dependent]) dependents[prerequisite].Add(dependent);

        var available = new SortedSet<int>(Enumerable.Range(0, indegrees.Length).Where(index => indegrees[index] == 0));
        var result = new List<int>(indegrees.Length);
        while (available.Count > 0)
        {
            var next = available.Min;
            available.Remove(next);
            result.Add(next);
            foreach (var dependent in dependents[next])
                if (--indegrees[dependent] == 0) available.Add(dependent);
        }

        orderedIndexes = result;
        cyclicIndexes = Enumerable.Range(0, indegrees.Length).Where(index => indegrees[index] > 0).ToArray();
        return result.Count == prerequisites.Count;
    }
}
