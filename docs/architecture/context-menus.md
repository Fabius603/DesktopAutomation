# Context menus

The selected first design is the common WPF popup with compact sizing: 30-DIP rows, a seven-DIP rounded surface,
subtle border and soft shadow, 16-DIP outline icons, 13-DIP text, right shortcut hints,
cyan hover/focus and separated red destructive actions. Dynamic application brushes keep the same structure legible in light and dark themes.
`DesktopAutomationApp/Styles/ContextMenus.xaml` owns this appearance; `Controls/ActionMenus.cs`
owns menu presentation and selection normalization. Existing value-source menus use this style. Named styles are assigned by the shared factory and
recursively when existing menus open, so MahApps theme replacement cannot override the design.
The cyan menu highlight uses a dedicated resource rather than the configurable application accent.
`ActionMenus` maps stable action/resource keys to semantic icons, independently of command class
names and translated labels. XAML translation bindings retain their resource key; custom labels
(including folder destinations and multiple-selection labels) must pass an explicit identity.
Group/ungroup use grouped elements, enable/disable use play/pause, and breakpoint removal uses a
crossed breakpoint rather than a trash can. Only delete/uninstall actions use the deletion color.
New actions must extend the central mapping; an unknown action gets neutral dots rather than a
misleading open-link arrow. The UI render host checks distinct/opposing actions in both languages
and produces `Actions-Light.png` and `Actions-Dark.png` for review.

## Surface inventory

| Surface | Actions | Multiple selection |
| --- | --- | --- |
| Job, macro, automation library entries | Open, execute/stop where supported, rename, duplicate, Explorer, move to folder/root, delete; automations activate/deactivate | Extended selection; duplicate/move/delete and automation state apply to the selection; only running entries can stop; open/rename are single-target |
| Library folders in the left mixed tree and right contents / empty surface | Open, create item/subfolder, rename, move to folder/root or up, delete folder; blank area creates item/folder | Folder navigation remains single-target; folder deletion and cycle prevention use the existing folder policy |
| Job steps | Edit/debug, block expansion, clipboard copy/paste, duplicate, move, enable/disable, set/remove breakpoints, branch actions, delete | Existing section-aware selection and control-flow rules; batch mutations retain undo and runtime synchronization |
| Macro steps / groups | Edit, clipboard, duplicate, move, create/dissolve group, rename/toggle group, remove from group, delete | Existing group-aware selection and eligibility; right click and three dots share the factory |
| Job variables | Copy names, duplicate, apply drafts, delete | Batch deletion first checks all references; one confirmation; a used variable prevents the whole operation |
| Variable usages | Open owning step, apply to this usage | Single-target reference operation |
| User-choice answers | Duplicate, move up/down, remove | Extended selection; retain at least two and at most eighteen answers |
| Conditions | Duplicate, remove | Extended selection; retain at least one condition; invalid duplicates disabled |
| Point and axis-expression editors | Duplicate, remove | Extended selection; nested fields and references copied through editor owners |
| Detection/text overlays | Duplicate, move, remove | Extended selection; stable order and independent row copies |
| Credentials | Edit, replace, copy name, delete | Single-target to preserve protected secret editing; secret values never enter context clipboard actions |
| YOLO models | Download eligible models, copy identifiers, open folder, uninstall installed models | Extended selection; count only eligible download/uninstall targets |
| Dashboard executions | Open definition/logs, stop | Extended selection; navigation single-target, stop selected instances |
| Dashboard automations | Open, disable | Extended selection; open single-target, disable selection |
| Log runs, executions, events, triggers | Navigate, copy, export, mark seen/resolved/new | Extended selection; navigation single-target; selection export captures event/run IDs and snapshot sequence |
| Log groups, links and paths | Open group/run or perform existing path checks | Single-target navigation; no invented history or filesystem actions |
| Log detail panel | Copy, export, origin, resolve/reopen | Current detail scope |
| Library editor headers | Save, discard, rename, Explorer | One definition; right click projects the same overflow instance |
| Image preview / path picker | Copy image, save PNG; copy path, reveal in Explorer | One displayed value |
| Text boxes, password boxes, combo boxes | Native editing/source selection | Native control semantics; application profiles do not intercept these menus |
| Tray | Open, stop jobs/macros, pause/resume automations, exit | WinForms notification icon opens the shared WPF menu on its dispatcher; application execution owners retained |

## Invariants and ownership

Right click inside a selection preserves it; right click outside selects the clicked row. Three-dot
entry points normalize selection identically. Apps and Shift+F10 open the same menu. Single-target
editing/navigation is hidden for multiple selection; batch labels show the number of targets.
Menu factories capture row objects before asynchronous work. Never infer an action's target from a
later selection or a translated label. Exact labels are only a fallback for legacy icon decoration.

`TaskAutomation/Steps/CollectionSelectionRules.cs` owns ordinary collection batch limits and
stable movement. Job structural operations retain their existing control-flow owner. Definition
copies use the existing job/macro serializers and `AutomationCopies`; new definitions have new
IDs while internal references remain valid in their separate scope. Automation copies start
disabled without run history. Service saves retain current file formats.

`LogExportService.ExportSelectionAsync` retains the existing ZIP schema, privacy sanitation,
cancellation, temporary-file handling and destination protection. It restricts events to captured
IDs or selected runs at the captured checkpoint. Missing retained selections produce partial
export issues instead of claiming a complete result. Attention updates use the existing service.

## Maintaining coverage

Follow `.agents/instructions/context-menus.md` for every new page or collection. Add the surface to
this matrix, identify its canonical commands, decide whether multiple selection is meaningful,
and connect all entry points to one factory. Test clicked-row targeting, selection preservation,
mixed eligibility, minimum/maximum counts and destructive confirmation. Render the changed
popup and inspect light/dark, disabled, submenu and localized states. Complete repository
verification through `eng/verify.ps1 -Mode Full`. A new page is incomplete until its menu coverage
or explicit reason for omission is recorded here.

The left library tree includes files and folders; its keyboard/right-click menus use the same factory as the right contents. The left tree deliberately has no row overflow buttons, as requested, leaving more room for names. Tree selection is single-target. The named root uses a normal folder row with a standard folder icon and expansion arrow; its selection navigates to direct root contents and its expansion hides/shows the left tree only; the pane resize grip has no context action.
