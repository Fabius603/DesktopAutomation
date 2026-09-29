---
name: winforms-development
description: Implement, debug, or review Windows Forms controls and WPF-WinForms interoperability in DesktopAutomation, including lifecycle, threading, DPI, designer safety, accessibility, and UI tests. Use only when WinForms code or interop is actually involved.
---

# Windows Forms development

Confirm that the affected surface is Windows Forms or an explicit interop boundary. The main
application UI is WPF; do not introduce WinForms for ordinary WPF features.

Read `../../instructions/ui-desktop.md` and `references/winforms-guide.md`. Preserve designer-owned
code, control lifetime, DPI behavior, and UI-thread access. Keep domain rules in shared projects.
Use stable accessibility names and automation IDs, add behavior-focused tests at the appropriate
layer, and finish with `eng/verify.ps1 -Mode Full`.
