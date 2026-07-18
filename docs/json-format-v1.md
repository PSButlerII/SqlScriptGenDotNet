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

Every database object and table constraint requires a `kind` discriminator. Missing discriminators are invalid JSON-format errors.

Required canonical members and collection entries cannot be null. The optional `constraints` member may be omitted or set to null; other structural null violations are invalid JSON-format errors.

Objects may declare `dependsOn` entries with `kind`, `name`, and optional `schema`. Explicit targets must exist in the same document. Foreign keys to tables present in the document create internal ordering dependencies. A foreign key to a table absent from the document remains a valid external SQL reference. Raw default and check expressions are never parsed for dependencies.

`dependsOn` is canonical document metadata and is not part of the legacy unversioned single-table shape. Dependency `kind` values are exactly `table` and `database`, and canonical serialization emits those lowercase values. Legacy inputs that require dependencies must be migrated into the versioned envelope.

When a foreign-key target table is declared in the same document, every referenced column must exist on that table; inconsistencies are semantic validation errors. A target absent from the document is treated as an external reference, so its column existence cannot be verified by SqlScriptGen.

`referencedSchema` alone controls foreign-key qualification. A source table's `schema` is never inherited by its foreign keys: omitting `referencedSchema` preserves an unqualified SQL reference, and internal matching and ordering use that same unqualified identity. Specify `referencedSchema` explicitly to guarantee a same-schema target. An explicit `dependsOn` can supply ordering independently, but it does not alter the rendered foreign-key reference.

Foreign-key `onDelete` and `onUpdate` values are optional and may be null. Non-null values must be one of the documented strings `noAction`, `restrict`, `cascade`, `setNull`, or `setDefault`; numeric enum values and other JSON token kinds are invalid.

Dependencies are ordered with a stable topological sort: prerequisites precede dependents, while declaration order breaks ties between otherwise available objects. Missing explicit dependencies, self-dependencies, and cycles are validation errors. Input collections are never mutated.

Database-only and table-only documents are supported. Mixing database and table objects is rejected because `CREATE DATABASE` requires a separate connection context. Interactive mode remains a single-table workflow.

## Version 1.0 compatibility

An unversioned root containing one table remains accepted and is adapted into the canonical domain document. Both input shapes use the same validation, ordering, rendering, and CLI output pipeline. Existing APIs `DefinitionJson.Deserialize`, `DefinitionJson.Serialize(TableDefinition)`, and the table/database `SqlGenerator.Generate` overloads remain available. Use the new document API for canonical serialization.

The machine-readable structural contract is [`schemas/sqlscriptgen-document-v1.schema.json`](../schemas/sqlscriptgen-document-v1.schema.json). Runtime validation additionally enforces identifiers, dialect types/capabilities, object uniqueness, dependencies, mixed-document policy, and cycles. Error paths use `objects[n]` prefixes.
