# Windows desktop UI

Context and overflow menu coverage must follow `context-menus.md` in this directory.

The application shell is WPF. Use WPF, MVVM, resource dictionaries, dependency properties,
commands, and UI Automation peers for normal application work. Use Windows Forms only when an
existing interop boundary or explicit requirement calls for it.

- Keep controls compact, value-first, and visually quiet.
- Use dynamic theme resources and localized resource keys.
- Do not block the dispatcher; marshal UI work explicitly when required.
- Give interactive controls stable automation names or IDs suitable for accessibility and UI tests.
- Verify the actual rendered or saved surface named by the user; XAML plausibility is not enough.
- For custom controls, keep keyboard access, focus behavior, scaling, and high-DPI layouts working.

Load the WPF or WinForms repository skill before framework-specific work.
