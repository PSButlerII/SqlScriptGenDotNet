# ADR 0007: Explicit raw SQL boundaries

Status: Proposed, 2026-07-18.

## Context

`SqlExpression` correctly marks defaults/checks, but views and routines will require larger query or procedural bodies. Identifiers, string values, expressions, and bodies have different escaping and safety properties.

## Decision

Keep dedicated wrappers for expressions, view queries, and procedural bodies. Add a separately escaped SQL string-literal value type for comments and metadata. Never treat raw bodies as identifiers or parsed SQL, and never execute them.

## Consequences

Safety boundaries remain visible in APIs/JSON/docs. Semantic validation is intentionally limited, requiring prominent review warnings and adversarial quoting tests.
