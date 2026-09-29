# Localization

Never add user-visible text directly to XAML or view models.

Every new or changed UI string must use a stable semantic key present in both:

- `DesktopAutomationApp/Resources/Strings.resx`
- `DesktopAutomationApp/Resources/Strings.en.resx`

German and English resources must contain matching keys, no duplicates, and no empty values.
Do not use generated hash-like keys. Run `eng/checks/localization.ps1`; it also rejects unknown
references and directly embedded XAML UI text.
