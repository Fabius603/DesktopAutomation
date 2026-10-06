---
date: 2026-10-06
status: proposed
supersedes:
superseded_by:
---

# Capture log UI facts at their owner

## Context

Review of the four selected log mockups exposed gaps between captured v2 events and the application
read model: capped attention counts, missing historical trigger context, unresolved related
instances, inferred failure consequences, ambiguous paths and mismatched history export filters.
The user explicitly requested implementing these missing backend structures and documenting them.

## Decision

Add optional typed v2 facts at their canonical owner: trigger snapshots in AutomationEngine,
flow effects in step execution, path roles at resolved I/O boundaries, and shared semantic display
keys in LogPresentation. LogQueryService coordinates complete counts, grouped automation history,
run/event details and canonical run selection. LogExportService consumes the same selection rule.
The frontend resolves localization/resources and interaction; it does not parse messages or derive
business decisions. The requested work is authorized; this record remains proposed for the exact
contract naming and grouping choices until separately accepted, without imposing an approval gate.

## Alternatives considered

- Join current definitions in WPF: rewrites history after edits and loses deleted definitions.
- Guess cause from failed-step order: mistakes continued cleanup failures for stopping failures.
- Export only visible rows: silently omits matching runs on other pages.
- Persist formatted German descriptions: ties meaning to a language and duplicates UI rules.

## Consequences

Older v2 records remain readable with empty/unknown optional facts. No job or automation migration
is introduced. Historical text logs remain excluded. Retained scans remain proportional to retained
data, and a catalog checkpoint does not freeze mutable run outcomes. The origin/path capture
allowlist is sanitized identically for live, persistence and export. Related links can be explicitly
unavailable rather than invented. Run exports intentionally include all retained events of selected
runs even if those events fall outside the run-start date filter.

## Verification

Use contract tests for optional-field defaults and localization keys; unit tests for paths, result
meaning and causal phase separation; integration tests for full counts, filters/checkpoints,
exports, snapshots after edits/deletion, missing/parallel run links and privacy. Finish with the
mandatory full repository verification entrypoint.
