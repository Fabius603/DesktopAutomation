# Testing policy

Tests describe observable behavior and durable invariants. They must remain useful after an
internal refactor. Do not write tests whose primary assertion reproduces the current sequence of
method calls, exact implementation text, or a private algorithm.

Choose the narrowest level that proves the risk:

- Unit: fast, isolated domain rules and state transitions without infrastructure.
- Contract: serialization, stable IDs, registries, metadata, schemas, and compatibility.
- Architecture: dependency direction and repository-wide structural rules.
- Integration: real collaboration between components, files, resources, or composition roots.
- UI: WPF rendering contracts, bindings, automation properties, and interaction behavior.
- End-to-end: a small set of critical workflows through the packaged application.

For behavior changes, cover the meaningful success and failure paths. Add missing-input,
cancellation, skipped execution, and backward-compatibility cases only where the behavior makes
them relevant. Prefer data-driven tests for rule matrices.

Determinism is mandatory: use controlled clocks, fixed random seeds, isolated temporary
directories, explicit cultures, bounded timeouts, and fakes at operating-system or network
boundaries. Tests must not depend on execution order or developer-machine state.

Use `.agents/skills/create-tests/SKILL.md` when adding or reorganizing tests. Every changed
repository must pass
`powershell -NoProfile -ExecutionPolicy Bypass -File eng/verify.ps1 -Mode Full` before handoff.
