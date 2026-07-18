# ADR 0003: Treat the legacy repository as read-only

Status: Accepted, 2026-07-18.

The Java repository is evidence and behavioral source material only. All code, docs, Git history, and distribution work occur in `SqlScriptGenDotNet`; the legacy repository is neither modified nor reorganized. This preserves provenance and makes the replacement independently releasable.
