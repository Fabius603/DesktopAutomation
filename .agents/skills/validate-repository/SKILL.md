---
name: validate-repository
description: Run and interpret DesktopAutomation's deterministic repository checks before completing any task that changed tracked or untracked repository files.
---

# Validate the repository

During development, run focused scripts from `eng/checks/` as needed. Before handoff, always run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\eng\verify.ps1 -Mode Full
```

Fix failures caused by the current work. Do not weaken, skip, or convert a required check into a
warning merely to obtain a successful result. If an environmental prerequisite cannot be met,
report the exact failed check as a blocker instead of claiming completion.
