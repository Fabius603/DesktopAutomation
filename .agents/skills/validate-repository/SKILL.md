---
name: validate-repository
description: Select, run, and interpret DesktopAutomation repository checks based on changed behavior and risk. Use for local validation, CI, and release verification.
---

# Validate the repository

Read [the testing policy](../../instructions/testing.md) to determine required scope. Do not run
checks solely because an agent iteration or reply ended.

For documentation and agent-guidance changes:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1 -Mode Focused -Checks repository,documentation,skills
```

For a specific behavior in a test suite:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1 -Mode Focused -Checks integration-tests -TestFilter 'FullyQualifiedName~Verification'
```

Check IDs are script basenames from `eng/test-manifest.json`; `-Checks` accepts comma-separated IDs.
Prerequisites are automatically included once, in manifest order: test suites require restore and
a Release solution build because they use `--no-build`. The filter applies only to selected test
checks; a filter that executes no tests fails instead of producing a misleading pass.
Focused runs still clear shared verification artifacts under the repository mutex, so
never rely on a previous run's binaries there. For faster project-specific development feedback,
use `dotnet build` or `dotnet test --filter` on the affected project with a fresh build.

Run the complete manifest in CI, before releases, when explicitly requested, or when focused
validation cannot establish sufficient confidence:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1 -Mode Full
```

Fix failures caused by the current work. Do not weaken, skip, or convert a required check into a
warning merely to obtain a successful result. If an environmental prerequisite cannot be met,
report the exact failed check as a blocker instead of claiming completion. Report selected checks,
filters, and material validation gaps; a Focused pass is not a Full pass.

For website/reference generation or changes affecting documented product contracts, include the
`website-docs` check. It validates canonical exports, examples, generation, coverage, links and the
complete UI/JSON reference coverage. The check runs `website/Build.ps1 -Check -Strict` and permits
no deferred explanations. See [website maintenance](../../instructions/website-documentation.md).
