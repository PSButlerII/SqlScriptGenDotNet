# ADR 0005: Narrow common database-object abstraction

Status: Accepted, 2026-07-18.

## Context

`TableDefinition` and `DatabaseDefinition` are unrelated roots, while future schemas, indexes, views, and routines need common orchestration.

## Decision

Introduce a narrow typed database-object abstraction carrying kind, stable qualified identity, and declared dependencies. Keep object-specific immutable records, validators, and renderer paths. Do not introduce a generic property bag or raw-SQL object.

## Consequences

Document validation/rendering can be shared without erasing dialect or object semantics. Each new object still requires explicit code and tests.
