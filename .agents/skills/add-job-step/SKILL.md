---
name: add-job-step
description: Add or modify DesktopAutomation job steps across contracts, persistence, execution, generated editors, localization, result bindings, details, release notes, and behavior tests.
---

# Add or change a job step

Read `references/adding-job-step.md` and `../../../docs/architecture/result-contracts.md` before
editing. Treat the backend definition and contracts as authoritative; the WPF application renders
and adapts them but does not redefine their meaning.

Trace the complete integration: persisted model and defaults, stable input/result IDs, registry,
handler, shared validation, editor metadata, localization, details, backward compatibility, and
tests. Use the generated editor path and reusable picker adapters. Finish with the full repository
verification gate.

Also follow [website documentation maintenance](../../instructions/website-documentation.md):
update affected explanations and examples in the same task, regenerate the reference, and run
the `website-docs` verification check. Existing deferred explanations are not a waiver for new ones.
