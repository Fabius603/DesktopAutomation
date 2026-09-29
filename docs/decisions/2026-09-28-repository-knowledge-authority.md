---
date: 2026-09-28
status: accepted
supersedes:
superseded_by:
---

# Separate current instructions, decisions, and historical specifications

## Context

Repository guidance was distributed across one large agent file, local READMEs, design QA notes,
and implementation guides. Their authority and lifetime were not explicit.

## Decision

Use a small root `AGENTS.md` as the authority and routing entrypoint. Keep current agent policy and
skills in `.agents/`, current architecture in `docs/architecture/`, durable decisions in
`docs/decisions/`, and non-binding dated snapshots in `docs/specs/`.

An agent must confirm an old specification with the user before treating it as current.

## Alternatives considered

- One comprehensive `AGENTS.md`: easy to find but expensive to load and hard to maintain.
- Treat all documentation as equally current: simple storage but ambiguous authority.

## Consequences

Guidance is discoverable and loaded progressively. New documentation must be classified by its
intended lifetime. Links from old locations may remain only as migration pointers.

## Verification

`eng/checks/documentation.ps1` validates required locations, metadata, names, and links.
