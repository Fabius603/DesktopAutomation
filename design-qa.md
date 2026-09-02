# Design QA

## Control-flow blocks

- Status: Passed
- Verified surface: `Choose` job in the Release build of the WPF app.
- A complete If/Else-if structure renders as one connected, open C-shaped element with a shared left rail and rounded footer.
- Outer and concave inner transitions are rounded consistently.
- Inserted 330 px step cards retain spacing from the surrounding control-flow element.
- Step names are vertically centered in regular cards and control-flow headers.
- The container uses a dedicated neutral surface color rather than the normal card or accent color.
- Hovering any part of the container highlights the complete connected block without resizing it; node-specific actions remain tied to the hovered row.
- Moving the pointer from a step card across the reserved gap to its outside action buttons keeps the hover controls visible and clickable.
- Double-clicking a step card opens its read-only detail popup; editing remains available only from the pencil action.
- The detail popup uses an opaque raised surface and closes when another part of the view is clicked.
- Empty control-flow branches reserve space and show a localized dashed drop placeholder inside the block.
- The block remains visually connected while scrolling from its first branch to its footer.

Evidence screenshots were captured from the running Release app in the local temporary directory as `DesktopAutomation-control-flow-qa.png`, `DesktopAutomation-control-flow-bottom-qa.png`, and `DesktopAutomation-control-flow-footer-qa.png`.
The updated color, rounded inner corners, vertical alignment, and whole-block hover were verified in `DesktopAutomation-control-flow-neutral-qa.png` and `DesktopAutomation-control-flow-neutral-hover-qa.png`.
