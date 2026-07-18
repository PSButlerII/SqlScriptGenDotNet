using System.Text.RegularExpressions;

namespace SqlScriptGen.Core;

public static partial class SqlDefinitionValidator
{
    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_$]*$", RegexOptions.CultureInvariant)] private static partial Regex IdentifierPattern();
    public static ValidationResult ValidateIdentifier(string? value, string path)
    {
        var errors = new List<ValidationError>();
        if (string.IsNullOrWhiteSpace(value)) errors.Add(new(path, "Identifier is required."));
        else if (value.Length > 63 || !IdentifierPattern().IsMatch(value)) errors.Add(new(path, "Identifier must start with a letter or underscore, contain only letters, digits, underscores, or $, and be at most 63 characters."));
        return new(errors);
    }
    public static ValidationResult Validate(TableDefinition table, DatabaseDialect dialect)
    {
        var errors = new List<ValidationError>();
        errors.AddRange(ValidateIdentifier(table.Name, "name").Errors);
        if (table.Schema is not null) errors.AddRange(ValidateIdentifier(table.Schema, "schema").Errors);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var renderedColumns = new HashSet<string>(StringComparer.Ordinal);
        var catalog = SqlTypeCatalogs.For(dialect);
        if (table.Columns is null) errors.Add(new("columns", "Columns are required."));
        else if (table.Columns.Count == 0) errors.Add(new("columns", "At least one column is required."));
        else for (var i = 0; i < table.Columns.Count; i++)
        {
            var c = table.Columns[i]; var path = $"columns[{i}]";
            if (c is null) { errors.Add(new(path, "Column cannot be null.")); continue; }
            errors.AddRange(ValidateIdentifier(c.Name, path + ".name").Errors);
            if (c.Name is not null && !seen.Add(c.Name)) errors.Add(new(path + ".name", $"Duplicate column '{c.Name}'."));
            if (c.Name is not null) renderedColumns.Add(c.Name);
            if (c.Type is null) errors.Add(new(path + ".type", "Column type is required."));
            else if (string.IsNullOrWhiteSpace(c.Type.Name)) errors.Add(new(path + ".type.name", "Type name is required."));
            else if (!catalog.TryGetValue(c.Type.Name, out var descriptor)) errors.Add(new(path + ".type.name", $"Type '{c.Type.Name}' is not supported by {dialect}."));
            else
            {
                if (c.Type.Length is not null && (!descriptor.AllowsLength || c.Type.Length <= 0)) errors.Add(new(path + ".type.length", "Length is unsupported or must be positive."));
                if (c.Type.Precision is not null && (!descriptor.AllowsPrecisionScale || c.Type.Precision <= 0)) errors.Add(new(path + ".type.precision", "Precision is unsupported or must be positive."));
                if (c.Type.Scale is not null && (c.Type.Precision is null || c.Type.Scale < 0 || c.Type.Scale > c.Type.Precision)) errors.Add(new(path + ".type.scale", "Scale requires precision and must be between zero and precision."));
            }
            if (c.Identity && c.Default is not null) errors.Add(new(path, "Identity and default cannot both be specified."));
            if (c.Default is not null && c.Default.Value is null) errors.Add(new(path + ".default.value", "Default expression value is required."));
        }
        var constraints = table.Constraints ?? [];
        var primaryKeys = constraints.OfType<PrimaryKeyConstraint>().Count();
        if (primaryKeys > 1) errors.Add(new("constraints", "Only one primary-key constraint is allowed."));
        for (var i = 0; i < constraints.Count; i++)
        {
            var constraint = constraints[i]; var path = $"constraints[{i}]";
            if (constraint is null) { errors.Add(new(path, "Constraint cannot be null.")); continue; }
            errors.AddRange(ValidateIdentifier(constraint.Name, path + ".name").Errors);
            var local = constraint switch { PrimaryKeyConstraint x => x.Columns, UniqueConstraint x => x.Columns, ForeignKeyConstraint x => x.Columns, _ => null };
            ValidateConstraintColumns(local, path + ".columns", renderedColumns, errors, constraint is CheckConstraint);
            if (constraint is CheckConstraint check)
            {
                if (check.Expression is null) errors.Add(new(path + ".expression", "Check expression is required."));
                else if (check.Expression.Value is null) errors.Add(new(path + ".expression.value", "Check expression value is required."));
            }
            if (constraint is ForeignKeyConstraint fk)
            {
                errors.AddRange(ValidateIdentifier(fk.ReferencedTable, path + ".referencedTable").Errors);
                if (fk.ReferencedSchema is not null) errors.AddRange(ValidateIdentifier(fk.ReferencedSchema, path + ".referencedSchema").Errors);
                if (fk.ReferencedColumns is null) errors.Add(new(path + ".referencedColumns", "Referenced columns are required."));
                else
                {
                    for (var j = 0; j < fk.ReferencedColumns.Count; j++) errors.AddRange(ValidateIdentifier(fk.ReferencedColumns[j], $"{path}.referencedColumns[{j}]").Errors);
                    if (fk.Columns is not null && fk.Columns.Count != fk.ReferencedColumns.Count) errors.Add(new(path, "Foreign-key local and referenced column counts must match."));
                }
                var supportedActions = SqlDialectCapabilityCatalog.For(dialect).SupportedReferentialActions;
                if ((fk.OnDelete is not null && !supportedActions.Contains(fk.OnDelete.Value)) || (fk.OnUpdate is not null && !supportedActions.Contains(fk.OnUpdate.Value)))
                    errors.Add(new(path, $"{dialect} does not support the selected referential action."));
            }
        }
        return new(errors);
    }

    private static void ValidateConstraintColumns(IReadOnlyList<string>? columns, string path, IReadOnlySet<string> tableColumns, List<ValidationError> errors, bool optional)
    {
        if (optional) return;
        if (columns is null) { errors.Add(new(path, "Columns are required.")); return; }
        if (columns.Count == 0) errors.Add(new(path, "At least one column is required."));
        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i]; var columnPath = $"{path}[{i}]";
            errors.AddRange(ValidateIdentifier(column, columnPath).Errors);
            if (column is not null && !tableColumns.Contains(column)) errors.Add(new(columnPath, $"Column '{column}' does not exist."));
        }
    }
}
