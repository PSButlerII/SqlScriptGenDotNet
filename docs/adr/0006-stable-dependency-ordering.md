# ADR 0006: Stable dependency ordering

Status: Implemented, 2026-07-18.

## Context

Declaration order is deterministic today, but multiple objects may require schemas/tables before indexes, foreign keys, views, or routines.

## Decision

Represent dependencies by stable object identity and use deterministic topological ordering. Preserve declaration order among otherwise independent objects. Report missing references and cycles as collected validation errors; never guess through a cycle.

## Consequences

Generated scripts remain reproducible and understandable. Explicit ordering metadata and graph tests become part of the public document contract.
