# ADR 0008: Explicit dialect feature capabilities

Status: Proposed, 2026-07-18.

## Context

PostgreSQL and MySQL share core DDL concepts but diverge on schemas, indexes, sequences, comments, triggers, and routines. Interface methods alone cannot express option-level support.

## Decision

Give each dialect an explicit, queryable capability description used by validation and CLI discovery. Capabilities identify supported object kinds and typed options; renderers remain dialect-specific. Unsupported features produce validation errors rather than approximated SQL.

## Consequences

The model avoids a false lowest common denominator and supports honest documentation. Capability/version compatibility must be tested whenever a dialect changes.
