# Context menus and selection

For every new or changed WPF page, inventory actionable rows, headers, previews, and empty
collection surfaces. Update `docs/architecture/context-menus.md` alongside the implementation.

- Right click, the row's three-dot button, and Apps / Shift+F10 must call the same menu factory.
- Use `Controls/ActionMenus.cs` and `Styles/ContextMenus.xaml`. Use `Behaviors/ContextActions.cs`
  for explicit supported profiles; never install blanket window-wide right-click interception.
- Preserve an existing multiple selection when the clicked item belongs to it. Otherwise select
  only the clicked item before creating a menu. Capture the action's targets when opening it.
- Multiple selection exposes supported batch actions with target counts. Hide single-target
  navigation/editing actions. Respect minimum/maximum counts, structural rules, debug locks,
  credentials protections, cancellation, privacy, and the canonical command's CanExecute.
- Do not put a second business rule in a menu factory. Extend the existing domain/application
  owner and reuse it from toolbar, keyboard, context menu, and overflow entry points.
- Keep native text/secret editing menus and unrelated control gestures intact.
- Menu labels must be German/English resources. Destructive actions are separated and red;
  disabled entries remain readable; icons, keyboard navigation and automation names are required.
- Assign icons through the central action-key mapping in `ActionMenus`. Pass explicit identities
  for custom row labels and batch labels; never infer an action from command type names or broad
  text fragments. Add appropriate mappings for new actions and distinguish their opposites.
- Add risk-based tests for target selection, batch limits and changed persistence/export behavior.
  Render changed popup states in both themes and inspect them. Run the mandatory Full gate.

If a surface deliberately has no menu or no multiple selection, record the reason in the matrix.
