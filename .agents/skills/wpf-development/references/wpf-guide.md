# WPF repository guide

## Binding and state

- Prefer explicit binding modes when editing state.
- Load persisted state before dependent selection callbacks can clear it.
- Notify only semantic changes; avoid full editor rebuilds on incidental property notifications.
- Do not put persistence, validation, or runtime meaning in converters or code-behind.

## Threading and lifetime

- Keep blocking I/O and long-running work off the dispatcher.
- Marshal only the UI mutation back to the dispatcher.
- Detach event handlers and cancel owned operations when views or dialogs close.

## Rendering and accessibility

- Use dynamic resources for theme-sensitive brushes and styles.
- Test keyboard navigation, focus visibility, scaling, text wrapping, and constrained widths.
- Set stable `AutomationProperties.AutomationId` or names on important interactive elements.
- Custom controls must expose meaningful automation peers when built-in peers are insufficient.

## Verification

Logic-only view-model changes need behavior tests. Binding, template, and resource contracts belong
in UI tests. Visual issues require inspection of the running Release application in addition to
automated checks.
