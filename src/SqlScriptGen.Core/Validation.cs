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
        if (table.Columns.Count == 0) errors.Add(new("columns", "At least one column is required."));
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var catalog = SqlTypeCatalogs.For(dialect);
        for (var i = 0; i < table.Columns.Count; i++)
        {
            var c = table.Columns[i]; var path = $"columns[{i}]";
            errors.AddRange(ValidateIdentifier(c.Name, path + ".name").Errors);
            if (!seen.Add(c.Name)) errors.Add(new(path + ".name", $"Duplicate column '{c.Name}'."));
            if (!catalog.TryGetValue(c.Type.Name, out var descriptor)) errors.Add(new(path + ".type.name", $"Type '{c.Type.Name}' is not supported by {dialect}."));
            else
            {
                if (c.Type.Length is not null && (!descriptor.AllowsLength || c.Type.Length <= 0)) errors.Add(new(path + ".type.length", "Length is unsupported or must be positive."));
                if (c.Type.Precision is not null && (!descriptor.AllowsPrecisionScale || c.Type.Precision <= 0)) errors.Add(new(path + ".type.precision", "Precision is unsupported or must be positive."));
                if (c.Type.Scale is not null && (c.Type.Precision is null || c.Type.Scale < 0 || c.Type.Scale > c.Type.Precision)) errors.Add(new(path + ".type.scale", "Scale requires precision and must be between zero and precision."));
            }
            if (c.Identity && c.Default is not null) errors.Add(new(path, "Identity and default cannot both be specified."));
        }
        var constraints = table.Constraints ?? [];
        var primaryKeys = constraints.OfType<PrimaryKeyConstraint>().Count();
        if (primaryKeys > 1) errors.Add(new("constraints", "Only one primary-key constraint is allowed."));
        foreach (var constraint in constraints)
        {
            errors.AddRange(ValidateIdentifier(constraint.Name, "constraints.name").Errors);
            IReadOnlyList<string> local = constraint switch { PrimaryKeyConstraint x => x.Columns, UniqueConstraint x => x.Columns, ForeignKeyConstraint x => x.Columns, _ => [] };
            foreach (var col in local.Where(col => !seen.Contains(col))) errors.Add(new("constraints.columns", $"Column '{col}' does not exist."));
            if (local.Count == 0 && constraint is not CheckConstraint) errors.Add(new("constraints.columns", "At least one column is required."));
            if (constraint is ForeignKeyConstraint fk)
            {
                errors.AddRange(ValidateIdentifier(fk.ReferencedTable, "constraints.referencedTable").Errors);
                if (fk.ReferencedSchema is not null) errors.AddRange(ValidateIdentifier(fk.ReferencedSchema, "constraints.referencedSchema").Errors);
                if (fk.Columns.Count != fk.ReferencedColumns.Count) errors.Add(new("constraints", "Foreign-key local and referenced column counts must match."));
                foreach (var col in fk.ReferencedColumns) errors.AddRange(ValidateIdentifier(col, "constraints.referencedColumns").Errors);
                if (dialect == DatabaseDialect.MySql && (fk.OnDelete == ReferentialAction.SetDefault || fk.OnUpdate == ReferentialAction.SetDefault)) errors.Add(new("constraints", "MySQL does not support SET DEFAULT referential actions."));
            }
        }
        return new(errors);
    }
}
