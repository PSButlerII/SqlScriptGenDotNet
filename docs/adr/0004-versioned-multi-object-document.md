# ADR 0004: Versioned multi-object document envelope

Status: Implemented, 2026-07-18.

## Context

`DefinitionJson` currently reads/writes one unversioned `TableDefinition`. Future objects and dependencies need an evolvable root without breaking 1.0 files.

## Decision

Add a canonical envelope with an explicit format version and ordered typed object collection. Continue accepting the v1.0 single-table shape through an adapter. Reject unsupported major format versions clearly. Do not silently reinterpret ambiguous documents.

## Consequences

New features gain one stable root and migration path. Parsing becomes more complex and requires compatibility fixtures. Removing the legacy adapter is a potential 2.0 change.
