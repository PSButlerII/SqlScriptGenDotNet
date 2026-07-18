# ADR 0002: Separate domain definitions from dialect renderers

Status: Accepted, 2026-07-18.

Typed immutable definitions are validated before an explicit dialect renderer receives them. This preserves ordering, prevents raw-map coupling, and isolates syntax differences. It adds small mapping overhead but makes new dialects and testing predictable.
