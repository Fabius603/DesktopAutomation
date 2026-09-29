---
name: maintain-localization
description: Add, change, audit, or repair DesktopAutomation German and English UI localization resources and their code or XAML references.
---

# Maintain localization

Read `../../instructions/localization.md`. Use stable semantic keys and update German and English
resources in the same change. Reuse an existing key only when its meaning is genuinely identical.

Run `eng/checks/localization.ps1` during development. The full completion gate runs it again and
also builds the resource-consuming application.
