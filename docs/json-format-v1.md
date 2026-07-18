# JSON document format version 1

SqlScriptGen document format version 1 is independent from the application package version. The canonical root is an envelope:

```json
{
  "formatVersion": 1,
  "objects": [
    {
      "kind": "table",
      "name": "customers",
      "schema": "public",
      "columns": [
        { "name": "id", "type": { "name": "bigint" }, "nullable": false, "identity": true }
      ]
    }
  ]
}
```

`formatVersion` and the nonempty ordered `objects` array are required. Version 1 supports only `table` and `database` discriminators. Unknown versions, object kinds, and properties are rejected. Canonical serialization emits the envelope, camel-case property names, indented JSON, and omits null optional values; it never emits the legacy shape.

JSON object-property order is not significant. The examples place `kind` first for readability, but canonical readers accept required properties in any order.

Objects may declare `dependsOn` entries with `kind`, `name`, and optional `schema`. Explicit targets must exist in the same document. Foreign keys to tables present in the document create internal ordering dependencies. A foreign key to a table absent from the document remains a valid external SQL reference. Raw default and check expressions are never parsed for dependencies.

Dependencies are ordered with a stable topological sort: prerequisites precede dependents, while declaration order breaks ties between otherwise available objects. Missing explicit dependencies, self-dependencies, and cycles are validation errors. Input collections are never mutated.

Database-only and table-only documents are supported. Mixing database and table objects is rejected because `CREATE DATABASE` requires a separate connection context. Interactive mode remains a single-table workflow.

## Version 1.0 compatibility

An unversioned root containing one table remains accepted and is adapted into the canonical domain document. Both input shapes use the same validation, ordering, rendering, and CLI output pipeline. Existing APIs `DefinitionJson.Deserialize`, `DefinitionJson.Serialize(TableDefinition)`, and the table/database `SqlGenerator.Generate` overloads remain available. Use the new document API for canonical serialization.

The machine-readable structural contract is [`schemas/sqlscriptgen-document-v1.schema.json`](../schemas/sqlscriptgen-document-v1.schema.json). Runtime validation additionally enforces identifiers, dialect types/capabilities, object uniqueness, dependencies, mixed-document policy, and cycles. Error paths use `objects[n]` prefixes.
