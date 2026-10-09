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

Use `.agents/skills/create-tests/SKILL.md` when adding or reorganizing tests.

## Risk-based validation

Validation follows changed behavior and risk, not each agent turn or intermediate iteration.

- Analysis, advice, and unchanged code require no test run.
- During development, run focused checks when they provide useful feedback. Batch related edits
  before checking them; do not rerun successful checks unless later changes could invalidate them.
- Before completing a code change, build affected projects and run tests covering the changed
  behavior and affected contracts. Select the narrowest useful level, adding broader coverage only
  for a distinct risk. Shared contracts, composition, build configuration, or unclear impact may
  require broader checks.
- Documentation-only changes require relevant documentation, skills, or repository checks, not
  application tests. Resource-only changes require relevant localization or resource checks;
  WPF resources affecting rendering also require appropriate UI validation.
- Full repository verification is mandatory in CI and before releases. Run it locally when
  explicitly requested or when broad changes make focused validation insufficient.
- Report checks performed, failures, and material validation gaps. Never describe focused or
  filtered results as a full repository pass. An unavailable or failing required check blocks the
  corresponding completion claim; do not weaken it to obtain a successful result.

Use `.agents/skills/validate-repository/SKILL.md` for commands. This policy supersedes older local
Full-before-handoff requirements; see the accepted decision
[Use risk-based local validation](../../docs/decisions/2026-10-08-use-risk-based-local-validation.md).
