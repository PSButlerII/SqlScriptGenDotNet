# ADR 0009: First-class schema objects and dialect semantics

Status: Proposed, 2026-07-19.

## Context

Version 1.1 introduced ordered, typed database objects, but schemas remain string qualifiers on tables. Version 1.2 needs to create schemas explicitly without weakening legacy input compatibility, confusing MySQL schemas with canonical database objects, or changing the offline generation boundary.

The authoritative syntax references are the PostgreSQL [`CREATE SCHEMA`](https://www.postgresql.org/docs/current/sql-createschema.html) documentation and the MySQL [`CREATE DATABASE`](https://dev.mysql.com/doc/refman/8.0/en/create-database.html) documentation. PostgreSQL defines a schema as a namespace within the current database and supports an optional `AUTHORIZATION` role. MySQL defines `CREATE SCHEMA` as a synonym for `CREATE DATABASE`. The model must preserve that semantic difference rather than imply dialect equivalence.

## Decision

### Canonical schema object

Add this canonical object discriminator in the v1.2 implementation:

```json
{
  "kind": "schema",
  "name": "sales"
}
```

The proposed immutable model is:

```csharp
SchemaDefinition(
    string Name,
    string? Owner = null)
```

`SchemaDefinition` will implement `IDatabaseObject`. Its identity is `Schema:<name>` and has no containing schema. `name` and `owner` are validated SQL identifiers; `owner` is not a raw SQL expression.

### Document format version

Retain canonical `"formatVersion": 1`. Recognizing a new object kind is additive: existing v1.1 documents are unchanged, the v1.0 adapter remains unchanged, and older applications rejecting a newer object kind is expected forward-version behavior. Changing existing discriminators or removing compatibility remains a major-version concern. A format version 2 is not justified solely by adding schemas.

### PostgreSQL semantics

Render a schema without an owner as:

```sql
CREATE SCHEMA "sales";
```

Render a schema with an owner as:

```sql
CREATE SCHEMA "sales" AUTHORIZATION "application_owner";
```

This follows PostgreSQL's documented `CREATE SCHEMA schema_name [ AUTHORIZATION role_specification ]` form. Role creation and role dependencies are not modeled. Inline schema elements and `IF NOT EXISTS` remain out of scope unless approved separately.

### MySQL semantics

Render the canonical schema object as:

```sql
CREATE SCHEMA `sales`;
```

MySQL documents `SCHEMA` as a synonym for `DATABASE`, but the canonical object remains a schema rather than being silently converted to `DatabaseDefinition`. MySQL schema ownership is unsupported, so a non-null `owner` produces a capability-validation error. Existing `DatabaseDefinition` and `CREATE DATABASE` behavior remain unchanged. Schema and database objects cannot coexist in one document, avoiding an ambiguous document whose canonical objects map to the same MySQL namespace operation.

### Allowed document composition

The following combinations are allowed:

```text
schema
schema + schema
table
table + table
schema + table
schema + schema + table + table
database
database + database
```

The following combinations are rejected:

```text
database + table
database + schema
database + schema + table
```

Database creation remains a separate connection-context workflow. Schema and table creation operate within one database context. Applying the same composition rule to PostgreSQL and MySQL avoids ambiguous overlap while retaining explicit dialect rendering.

### Schema-to-table ordering

A qualified table implicitly depends on a schema only when the document contains an exact rendered-name match. For example, this table:

```json
{
  "kind": "table",
  "name": "customers",
  "schema": "sales"
}
```

depends on this internal schema:

```json
{
  "kind": "schema",
  "name": "sales"
}
```

and is generated after it:

```sql
CREATE SCHEMA "sales";

CREATE TABLE "sales"."customers" (...);
```

Internal matching uses exact ordinal spelling because renderers quote identifiers. `sales` and `Sales` are different rendered identities. A matching internal schema adds an ordering edge. An absent or differently cased schema remains an external reference and does not add an edge. The generator never rewrites casing, and existing qualified tables remain valid without an internal schema object. This follows the established internal/external foreign-key distinction.

### Explicit dependencies

Explicit dependency identities may use:

```json
{
  "kind": "schema",
  "name": "sales"
}
```

The exact wire token is `schema`. Canonical deserialization and the public canonical options must accept only that exact string: no integer ordinal, incorrect casing, surrounding whitespace, or normalization.

### Generated statements

Add `GeneratedSqlStatementKind.CreateSchema`. The generated statement's source identity is the schema identity.

### Dialect capabilities

Capabilities distinguish schema object creation, schema ownership, schema-qualified tables, and schema/table mixing:

| Capability | PostgreSQL | MySQL |
| --- | --- | --- |
| Schema objects | Supported | Supported |
| Schema ownership | Supported | Unsupported |
| Schema-qualified tables | Supported | Supported |
| Schema/table documents | Supported | Supported |

The shared capability names do not imply identical database semantics. Unsupported options fail validation instead of being ignored or approximated.

## Compatibility boundaries

The implementation must preserve:

- all valid legacy v1.0 JSON;
- all valid canonical v1.1 JSON;
- current `TableDefinition.Schema` behavior;
- existing `DatabaseDefinition` behavior;
- existing renderer overloads;
- existing CLI syntax and exit codes;
- all established SQL golden output; and
- the offline boundary: no database connectivity or SQL execution.

## Non-goals

- Schema import or inspection
- Default schema selection
- Search-path management
- `USE` statements
- Database connection switching
- Drop or alter schema
- Inline schema elements
- Roles or role creation
- Grants and permissions
- Indexes
- Comments
- Views
- Migration diffing
- SQL execution
- GUI changes

## Alternatives considered

1. **Treat schemas as strings only.** Rejected because strings cannot represent schema creation, statement metadata, validation, or dependency ordering.
2. **Map MySQL schemas directly to `DatabaseDefinition`.** Rejected because it silently changes canonical identity and obscures user intent even though MySQL renders equivalent operations.
3. **Make schema support PostgreSQL-only.** Rejected because MySQL explicitly supports `CREATE SCHEMA`; capability validation can represent the ownership difference honestly.
4. **Introduce document format version 2.** Rejected because the new object kind is additive and existing format-1 contracts remain intact.
5. **Require every qualified table to declare an internal schema.** Rejected because partial documents and externally managed schemas are valid existing workflows.
6. **Allow database and schema objects to mix freely.** Rejected because database creation changes connection context and MySQL treats schema/database creation as synonymous.
7. **Infer schemas from table names or connection state.** Rejected because the generator is offline, inference would depend on unavailable runtime state, and rewriting identities would conflict with quoted-identifier semantics.

## Consequences

Schema creation becomes a first-class, ordered operation with explicit dialect capabilities. The document model remains backward compatible, but readers predating v1.2 may reject the new discriminator. Exact-case internal matching protects generated SQL semantics, while external schema references preserve partial-document workflows. PostgreSQL ownership adds an option that MySQL must reject explicitly.
