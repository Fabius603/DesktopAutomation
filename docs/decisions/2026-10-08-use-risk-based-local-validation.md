---
date: 2026-10-08
status: accepted
supersedes: 2026-09-28-deterministic-verification-gate.md (validation scope); older Full-before-handoff clauses in other decisions
superseded_by:
---

# Use risk-based local validation

## Context

Requiring the entire manifest after every repository change repeats restore, build, and all six
test suites even for documentation or small follow-up edits. The user accepted focused local
validation with full verification in CI and before releases.

## Decision

`.agents/instructions/testing.md` is the canonical owner of validation scope. Agent turns are not
validation boundaries. Local completion requires relevant checks for changed behavior and affected
contracts; successful checks need repeating only if subsequent changes could invalidate them.
Documentation-only changes require relevant static checks. Code changes require affected builds
and behavior tests. Full verification remains mandatory in CI and before releases, and locally
when explicitly requested or when focused checks cannot cover broad impact.

`eng/verify.ps1` keeps Full as its backward-compatible default and adds Focused with explicit
check IDs and an optional test filter. The existing manifest owns checks and their prerequisites;
both modes preserve fail-closed execution, artifact serialization, and diagnostic summaries.
Full rejects restrictions. Focused never claims complete repository coverage.

This decision supersedes the local Full-before-handoff requirements in the
[original gate decision](2026-09-28-deterministic-verification-gate.md) and equivalent validation
clauses in older decisions, including 2026-10-07-own-job-child-executions. Other domain, UI,
compatibility, and rendering requirements in those records remain authoritative.

## Alternatives considered

- Keep the full local gate and merely discourage intermediate test runs: every final reply would
  still trigger redundant full validation.
- Remove local testing entirely: this loses fast feedback about the changed behavior.
- Maintain a separate local script: this duplicates orchestration and prerequisites.

## Consequences

Agents explain selected checks and material gaps. Focused test-suite checks still build the solution
because existing suite scripts require fresh Release outputs. Project-specific dotnet commands
remain useful for narrower development feedback. A focused pass does not imply unrelated changes
or omitted suites were validated.

## Verification

Isolated subprocess tests cover selection, prerequisites, filter forwarding, invalid requests,
failure propagation, and Full compatibility. Existing concurrency tests cover mutex release and
shared-artifact protection. Documentation and skill checks validate the updated guidance.
