# Post-1.0 dependency-ordered roadmap

This roadmap favors vertical, releasable increments. Every phase preserves PostgreSQL/MySQL isolation, deterministic reviewed output, tests, and documentation. The generator remains offline and never executes SQL.

## Phase 0 — Planning and issue structure (current milestone)

- **Goal/value:** turn legacy ideas into an actionable dependency graph and reviewable backlog.
- **Scope/deliverables:** audit, architecture review, ADR proposals, labels, roadmap milestone, tracking issue, and v1.1 issues.
- **Non-goals/prerequisites:** no production features; requires only the stable 1.0 baseline.
- **Tests/docs/exit:** existing validation remains green; documents cite repository evidence; GitHub items have owners-by-milestone, dependencies, and acceptance criteria.
- **Version/dependencies:** planning only; precedes 1.1.

## Phase 1 — Multi-object document foundation

Implementation status: in progress for version 1.1.0; the implementation PR covers issues #1–#8 without adding later SQL syntax.

- **Goal/value:** generate a deterministic script containing several typed objects while keeping v1.0 files working.
- **Scope/deliverables:** versioned envelope, common object identity, ordered objects/statements, dependency metadata and stable ordering, capability-aware renderer orchestration, CLI integration, fixtures, and migration guide.
- **Non-goals/prerequisites:** no new SQL object syntax, GUI, execution, or general SQL AST; ADRs 0004–0008 must be resolved.
- **Tests/docs/exit:** both legacy/new JSON, polymorphic round trips, cycles/missing references/tie ordering, exact multi-statement output for both dialects, CLI compatibility; schema and user docs published.
- **Version/dependencies:** **1.1.0**; depends on Phase 0.

## Phase 2 — First-class schemas

- **Goal/value:** declare namespaces and safely qualify later objects.
- **Scope/deliverables:** reusable qualified names, PostgreSQL `CREATE SCHEMA` and optional ownership, schema references; explicit MySQL database/schema behavior.
- **Non-goals/prerequisites:** no assumption that dialect semantics match; no schema import; requires Phase 1 identities/capabilities.
- **Tests/docs/exit:** exact dialect outputs, unsupported-option and dependency tests, JSON examples; support matrix updated.
- **Version/dependencies:** **1.2.0**; Phase 1.

## Phase 3 — Indexes

- **Goal/value:** define performant access paths after their tables.
- **Scope/deliverables:** basic, unique, composite, direction, optional names; PostgreSQL partial/method and MySQL prefix options where justified.
- **Non-goals/prerequisites:** no full expression-index AST or optimizer advice; requires object references, schemas, capabilities, and ordering.
- **Tests/docs/exit:** missing columns, duplicate names, dialect options, stable post-table ordering, exact outputs/examples.
- **Version/dependencies:** **1.3.0**; Phases 1–2.

## Phase 4 — Comments and table metadata

- **Goal/value:** preserve human-readable schema documentation.
- **Scope/deliverables:** table/column comments, escaped string-literal type, PostgreSQL `COMMENT ON`, correct MySQL inline/alter syntax.
- **Non-goals/prerequisites:** no arbitrary raw metadata SQL; requires object identity and literal escaping.
- **Tests/docs/exit:** quote/control-character tests, deterministic follow-up ordering, dialect examples and safety guidance.
- **Version/dependencies:** **1.4.0**; Phases 1–2.

## Phase 5 — Expanded table operations

- **Goal/value:** support safe, explicit schema evolution primitives.
- **Scope/deliverables:** **1.5.0** add/rename columns and add/drop constraints; **1.6.0** nullability/default changes, drop columns, rename tables, with destructive-operation markers.
- **Non-goals/prerequisites:** no “all ALTER syntax,” automatic diff, or execution; needs stable identities, statement ordering, capabilities, and safety classification.
- **Tests/docs/exit:** one typed operation per supported action, dialect exact output, invalid transition/destructive warning tests, migration examples.
- **Version/dependencies:** **1.5.0–1.6.0**; Phases 1–4.

## Phase 6 — Views

- **Goal/value:** generate reusable query objects with explicit dependencies.
- **Scope/deliverables:** create view, explicit columns, supported replace behavior, structured source dependencies, clearly wrapped raw query body.
- **Non-goals/prerequisites:** no SELECT parser or materialized views initially; requires raw boundary, schemas, dependencies, and capabilities.
- **Tests/docs/exit:** dependency order, raw-body preservation, dialect replacement validation, exact outputs and safety documentation.
- **Version/dependencies:** **1.7.0**; Phases 1–2, preferably Phase 5.

## Phase 7 — PostgreSQL sequences and generated values

- **Goal/value:** support explicit sequence lifecycle where the dialect has it.
- **Scope/deliverables:** PostgreSQL start/increment/min/max/cache/cycle/ownership and typed default integration; MySQL explicitly reports unsupported.
- **Non-goals/prerequisites:** no fake MySQL equivalent; requires capability model, object dependency, and schemas.
- **Tests/docs/exit:** option bounds, ownership/order, identity interaction, rejection in MySQL, exact examples.
- **Version/dependencies:** **1.8.0**; Phases 1–2.

## Phase 8 — Triggers

- **Goal/value:** describe event-driven database behavior without collapsing dialect differences.
- **Scope/deliverables:** common target/dependency concepts plus distinct PostgreSQL timing/event/function and MySQL timing/event/body models.
- **Non-goals/prerequisites:** no generic shared trigger enum/body and no execution; requires mature objects, routines design, raw-body policy, capabilities.
- **Tests/docs/exit:** dialect matrices, dependency ordering, delimiter/body preservation, unsupported combinations, security review.
- **Version/dependencies:** **2.0.0 candidate**; Phases 1–7 and routine design. Major only if public document/CLI contracts must break.

## Phase 9 — Functions and stored procedures

- **Goal/value:** generate typed routine declarations needed by advanced schemas and triggers.
- **Scope/deliverables:** distinct function/procedure records, parameters, returns, language/determinism/volatility/security options, PostgreSQL dollar quoting, MySQL delimiter-safe packaging, explicit raw bodies.
- **Non-goals/prerequisites:** no procedural-language parser; requires raw-body and capability decisions plus dependencies.
- **Tests/docs/exit:** delimiter/quoting adversarial cases, dialect-only options, sensitive-content guidance, exact output suites.
- **Version/dependencies:** **2.1.0–2.2.0**; Phase 1–8 design. Split by dialect or routine kind if needed.

## Phase 10 — Users, roles, and permissions

- **Goal/value:** represent deployable authorization policy deliberately.
- **Scope/deliverables:** roles/logins, membership, grant/revoke, object/schema/database privileges, secret-free password references if ever approved.
- **Non-goals/prerequisites:** no real credentials in documents, examples, logs, or issues; requires stable identities, security design, capability model.
- **Tests/docs/exit:** privilege matrices, redaction/no-secret tests, dialect outputs, security review and operational warnings.
- **Version/dependencies:** **2.3.0+**; Phases 1–2 and mature object catalog.

## Phase 11 — Additional SQL dialects

- **Goal/value:** broaden use only where sustained maintenance value exceeds divergence cost.
- **Scope/deliverables:** evaluate SQLite first and SQL Server second using capability coverage, fixtures, CI feasibility, and documentation burden.
- **Non-goals/prerequisites:** no MongoDB in `DatabaseDialect`, no lowest-common-denominator renderer; requires proven capability model and conformance suite.
- **Tests/docs/exit:** decision record, full supported-object matrix, exact output and validation suite, distribution docs.
- **Version/dependencies:** **3.0.0 candidate** for a substantial public capability expansion; after core object model stabilizes.

## Phase 12 — Schema import and inspection

- **Goal/value:** bootstrap definitions from existing designs.
- **Scope/deliverables:** structured offline import first, SQL-DDL import feasibility second, live metadata only after separate connectivity/credential approval.
- **Non-goals/prerequisites:** no connectivity in this roadmap milestone; requires mature versioned schema model and provenance/error representation.
- **Tests/docs/exit:** loss/unsupported-feature reporting, round-trip fixtures, threat model before drivers or credentials.
- **Version/dependencies:** **3.1.0+**; Phases 1–11 as relevant.

## Phase 13 — Migration diff generation

- **Goal/value:** compare structured schemas and produce an ordered, reviewable migration plan.
- **Scope/deliverables:** typed differences, dependency ordering, destructive classification, explicit approval boundary, dialect migration rendering.
- **Non-goals/prerequisites:** no automatic execution or claim of lossless universal diff; requires schemas, indexes, alter operations, object identities.
- **Tests/docs/exit:** idempotent comparisons, rename ambiguity, destructive cases, dependency/cycle tests, safety guide.
- **Version/dependencies:** **3.2.0+ or 4.0.0** depending API/format impact; Phases 1–5 and expanded object coverage.

## Phase 14 — GUI or web presentation

- **Goal/value:** make the mature generation model easier to explore without duplicating domain logic.
- **Scope/deliverables:** evaluate desktop, local ASP.NET Core UI, and hosted service against security, distribution, and maintenance evidence.
- **Non-goals/prerequisites:** no framework selection now and no SQL execution; requires stable Core APIs/document format and accessibility/security requirements.
- **Tests/docs/exit:** decision record, presentation-only integration boundary, Core parity tests, deployment/security model.
- **Version/dependencies:** **post-3.0 evaluation**; after document and schema APIs mature.

## Versioning guardrail

The new envelope is additive in 1.1 because the v1.0 adapter remains supported. Removing that adapter, changing existing CLI output semantics, or making incompatible object discriminators requires **2.0.0**. Procedural features alone do not justify a major version unless they force such breaks.
