---
date: 2026-09-28
status: accepted
supersedes:
superseded_by:
---

# Give every business rule one canonical owner

## Context

Some behavior is implemented in frontend code even though it governs validation, defaults,
persistence, execution, or control flow. Other rules exist in multiple locations and can evolve
differently. A correct change in one surface can therefore leave another surface inconsistent.

## Decision

Every business rule has one canonical implementation in the lowest appropriate frontend-neutral
layer. Presentation code consumes shared decisions and owns only interaction and rendering. When
callers need legitimate variation, the canonical implementation accepts an explicit policy or
strategy rather than being copied.

Agents must search for related implementations before adding behavior, consolidate competing
implementations in the touched scope, and identify the canonical owner in the final handoff.
Compatibility adapters may translate legacy data but must delegate its meaning to the canonical
implementation.

## Alternatives considered

- Keep small rules near each UI surface: initially convenient, but creates inconsistent behavior
  and makes non-UI reuse difficult.
- Synchronize duplicate implementations with tests: detects some drift but preserves multiple
  sources of truth and unnecessary maintenance.
- Centralize all logic in one large service: removes some duplication but replaces clear ownership
  with a broad, tightly coupled abstraction.

## Consequences

Features may require shared contracts or services before UI work begins. Existing duplication is
consolidated incrementally whenever its behavior is touched. Frontend tests become focused on
presentation, while shared behavior is tested once at its owning layer. Temporary exceptions need
an explicit migration record and may not become a third implementation.

## Verification

Architecture tests enforce dependency direction where it is mechanically observable. Behavior
tests exercise rules through their canonical owner. Repository review and the mandatory agent
workflow require a pre-change and post-change search for competing implementations.
