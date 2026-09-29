---
name: record-decision
description: Create or supersede a durable DesktopAutomation architecture or design decision record when a choice constrains future implementations, dependencies, contracts, persistence, testing, or operations.
---

# Record a durable decision

Use a decision record when future work needs the reason, alternatives, and consequences—not for a
temporary task plan or historical requirement snapshot.

Copy `.agents/templates/decision-record.md` to
`docs/decisions/YYYY-MM-DD-present-tense-title.md`. Mark it `proposed` until the user accepts a
material choice. When replacing a decision, create a new file and cross-link `supersedes` and
`superseded_by`; do not rewrite the old rationale. Update `docs/decisions/README.md` and run the
documentation check.
