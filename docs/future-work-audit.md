# Future-work audit

This audit maps the legacy `SqlScriptGen/README.md` wish list to the released .NET 1.0 implementation. The Java repository remains read-only. Evidence refers to the replacement repository unless explicitly prefixed `legacy`.

| Legacy item | Version 1.0 status | Current implementation evidence | Remaining work | Dependencies | Recommended phase | Recommended release | Notes |
|---|---|---|---|---|---|---|---|
| Other SQL commands | Partially completed | `ISqlDialectRenderer` renders create table/database; CLI exposes `generate` and `create-database`. | Add typed statement families incrementally. | Multi-object foundation. | 1 onward | 1.1+ | Never one untyped “any SQL” model. |
| Other database types | Reframed | `DatabaseDialect` supports PostgreSQL/MySQL with isolated renderers. | Evaluate SQLite, then SQL Server on value and maintenance cost. | Stable capability model and object abstractions. | 11 | 3.0 candidate | MongoDB is rejected as a SQL dialect. |
| Other data types | Partially completed | `SqlTypeCatalogs` contains explicit per-dialect catalogs and argument metadata. | Expand only with renderer/validation/tests per dialect. | Capability model. | Continuous | Minor releases | Not an endless cross-dialect checklist. |
| Constraint options | Completed for 1.0 table scope | `TableConstraint` has primary, unique, check, foreign key, composite columns, and referential actions; tests cover them. | Later alter-table constraint operations and dialect-specific options. | Alter-table model. | 5 | 1.5–1.6 | Do not duplicate existing create-table constraint work. |
| Comments | Not started | No comment model or renderer method. | Table/column comments, string literal escaping, dialect placement. | Multi-object model; literal abstraction. | 4 | 1.4 | May be metadata plus emitted follow-up statements. |
| Indexes | Not started | No index definition; renderers only create table/database. | Basic/unique/composite and dialect-specific options. | Multi-object model, object references, dependencies. | 3 | 1.3 | Ordered after tables. |
| Triggers | Not started | No trigger model or procedural body boundary. | Separate PostgreSQL/MySQL trigger shapes and dependencies. | Objects, views/sequences, raw-body ADR, capabilities. | 8 | 2.0 candidate | Do not flatten dialect differences. |
| Views | Not started | No view definition or query-body abstraction. | Basic create view with explicit raw query boundary and dependencies. | Multi-object foundation and schema support. | 6 | 1.7 | No SELECT parser initially. |
| Stored procedures | Not started | No routine model or delimiter handling. | Parameters, bodies, security and dialect options. | Procedural foundation and raw-body policy. | 9 | 2.1+ | Functions and procedures remain distinct. |
| Functions | Not started | No function model. | Returns, language/determinism/volatility, quoting and bodies. | Triggers often reference functions; raw-body policy. | 9 | 2.1+ | Separate dialect-specific extensions. |
| Users | Not started | No security principal model. | Login/user creation without committed secrets. | Capability and sensitive-value policy. | 10 | 2.3+ | Never put real passwords in JSON/examples. |
| Roles | Not started | No role model. | Role creation and membership. | Multi-object/dependency and security model. | 10 | 2.3+ | PostgreSQL/MySQL semantics differ. |
| Permissions | Not started | No grant/revoke or privilege model. | Object/schema/database privileges and membership. | Objects must have stable identities; roles/users. | 10 | 2.3+ | Security review required. |
| Databases | Completed for basic creation | `DatabaseDefinition`, `SqlGenerator.Generate(DatabaseDefinition, …)`, and exact-output tests support both dialects. | Optional database properties only when typed and dialect-supported. | Capability model. | Continuous | Minor releases | Basic legacy request is complete. |
| Schemas | Partially completed | `TableDefinition.Schema`, FK `ReferencedSchema`, validation, and qualified rendering exist. | First-class `CREATE SCHEMA`, references, ownership, MySQL terminology. | Multi-object foundation. | 2 | 1.2 | Current schema strings are not reusable object identities. |
| GUI or web interface | Not started | CLI is the sole presentation layer; Core has no `Console` dependency. | Select a UI only after document/API maturity. | Stable multi-object Core and format. | 14 | Post-3.0 evaluation | Framework selection is intentionally deferred. |

## Capabilities already complete in 1.0

PostgreSQL, MySQL, create-table, basic create-database, multiple dialect-specific types, primary and composite primary keys, unique/check constraints, foreign and composite foreign keys, referential actions, interactive CLI, JSON input, identifier validation/quoting, ordered columns, identity/auto-increment, tests, CI, and self-contained publishing are shipped and should not receive duplicate roadmap issues.
