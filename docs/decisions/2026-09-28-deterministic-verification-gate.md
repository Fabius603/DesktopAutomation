---
date: 2026-09-28
status: accepted
supersedes:
superseded_by:
---

# Use one deterministic completion gate

## Context

Local work and CI previously encoded overlapping build and test commands independently. Passing a
partial command did not prove the repository met every deterministic contract.

## Decision

`eng/verify.ps1 -Mode Full` is the only completion gate for changed repository work. CI invokes the
same script. Required checks must fail closed and the overall command succeeds only with exit code
zero.

## Alternatives considered

- Document several commands for agents to select manually.
- Keep separate local and CI orchestration.

## Consequences

One script becomes a maintained product interface. Fast focused commands may be used during work,
but never replace the full final run.

## Verification

CI calls `eng/verify.ps1`, and `eng/checks/repository.ps1` rejects divergent workflow commands.
