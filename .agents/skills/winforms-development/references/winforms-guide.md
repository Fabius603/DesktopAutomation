# Windows Forms repository guide

## Designer and ownership

- Treat `.Designer.cs` and `.resx` files as generated pairs; make ordinary logic changes in the
  non-designer partial class.
- Dispose owned controls, components, images, fonts, handles, and cancellation sources.
- Do not dispose objects owned by a caller or shared application service.

## Threading

- Access controls only from their creating UI thread.
- Use `InvokeRequired` plus `BeginInvoke` for external callbacks, and stop callbacks during disposal.
- Never block the UI thread waiting for work that needs the UI thread to complete.

## Layout, DPI, and interoperability

- Prefer layout panels and anchors over fixed pixel positioning.
- Verify high-DPI scaling, font scaling, tab order, keyboard access, and localization expansion.
- Keep `WindowsFormsHost` or `ElementHost` boundaries small and document ownership/focus behavior.
- Give important controls stable accessible names for UI Automation.

## Testing

Keep logic outside controls and unit-test it independently. Run control tests on an STA thread.
Reserve end-to-end automation for critical workflows and execute it on an interactive Windows
runner with an isolated user-data directory.
