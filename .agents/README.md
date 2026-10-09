# Agent knowledge hub

This directory is the single entry point for repository-specific agent guidance.

- `instructions/` contains current mandatory repository policies.
- `skills/` contains task-specific workflows following the Agent Skills `SKILL.md` format.
- `templates/` contains canonical templates referenced by instructions and skills.

Project documentation for humans and agents lives under `docs/`. Current architecture,
durable decisions, and historical specifications deliberately have different authority; see
the root `AGENTS.md` before using them.

## Skill routing

Context-menu, overflow-menu, and selection changes also require `instructions/context-menus.md`.
Visual changes also require `instructions/visual-design.md`.
Changes affecting steps, values, automations, macros or logs also require
`instructions/website-documentation.md`.

- WPF UI or MVVM work: `skills/wpf-development/SKILL.md`
- Windows Forms interop or WinForms work: `skills/winforms-development/SKILL.md`
- Architecture boundaries, shared rules, or duplicate implementations:
  `skills/maintain-architecture/SKILL.md`
- Job-step work: `skills/add-job-step/SKILL.md`
- Test design or test creation: `skills/create-tests/SKILL.md`
- Logging, log storage, diagnostics, run history, automation correlation, or log export:
  `skills/maintain-logging/SKILL.md`
- Localization changes: `skills/maintain-localization/SKILL.md`
- Sustainable design decisions: `skills/record-decision/SKILL.md`
- Final repository verification: `skills/validate-repository/SKILL.md`
- Website and documentation screenshots: `skills/maintain-website-screenshots/SKILL.md`
