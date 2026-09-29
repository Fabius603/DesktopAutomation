---
name: wpf-development
description: Implement, debug, or review DesktopAutomation WPF views, controls, bindings, resources, MVVM interaction, accessibility, and rendered UI behavior. Use for WPF work; do not use for pure Windows Forms code.
---

# WPF development

Read `../../instructions/ui-desktop.md` and `references/wpf-guide.md` before changing WPF code.

Trace a UI behavior through its shared contract, view model, binding, template, resources, and
rendered surface. Keep business rules outside `DesktopAutomationApp`. Add semantic localization
keys for visible text and stable automation properties for interactive controls.

Validate logic at the lowest useful layer, then verify the actual WPF surface when layout,
visibility, focus, hit targets, scrolling, templates, or styling are involved. Finish with the
repository verification gate.
