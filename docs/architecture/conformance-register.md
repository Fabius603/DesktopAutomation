# Architecture conformance register

This register records the repository-wide audit completed on 2026-09-28. A `verified` entry has
one canonical owner, no competing implementation in the audited scope, and automated evidence.

## Audit scope and baseline

- Production projects and every `ProjectReference` were inspected.
- `DesktopAutomationApp` view models, converters, code-behind, services, JSON/file access,
  validation branches, defaults, normalization, step/operator switches, and direct path access
  were searched and classified.
- Job, macro, automation, settings, stable IDs, descriptors, result bindings, migrations, and
  legacy deserialization paths were compared with their runtime consumers.
- All tests were classified into unit, contract, architecture, integration, UI, and end-to-end
  projects. Direct compilation of production UI sources into tests was removed.
- Baseline full verification passed with 557 unit, 81 contract, 3 architecture, 74 integration,
  141 UI, and 17 end-to-end tests. Existing `NU1701` compatibility warnings are external package
  warnings and were unchanged by the migration.

Presentation-side JSON-node conversion in generated editors is a projection into the shared
`StepDraft` contract, not independent persistence semantics. File/folder dialogs, Explorer
launching, focus, and localized formatting remain presentation concerns. Windows Forms usage in
`TaskAutomation.WindowsIntegration`, capture, hotkey, and screen adapters is an explicit technical
Windows boundary; WPF is reserved for `DesktopAutomationApp`.

## Verified findings

| ID | Priority | Invariant | Finding and consumers | Canonical owner | Compatibility risk | Tests | Dependencies | Status | Evidence |
|---|---|---|---|---|---|---|---|---|---|
| ARCH-001 | P0 | Core project dependencies follow one documented direction. | Dependency rules previously protected only the reverse reference to the presentation project. | `DesktopAutomation.ArchitectureTests.ProjectDependencyRulesTests` | none | Architecture | none | verified | `CoreProjects_ReferenceOnlyDocumentedLowerLayers`; `SharedProjects_DoNotReferencePresentationProject` |
| ARCH-002 | P3 | Shared job/runtime code does not depend on WPF. | `PixelGeometryAdapters` exposed unused WPF conversions and `HotkeyTextFormatter` used WPF key mapping; all hotkey, automation, and geometry consumers were checked. | `TaskAutomation.Geometry.PixelGeometryAdapters` and `TaskAutomation.Hotkeys.HotkeyTextFormatter`, using frontend-neutral contracts and Windows technical APIs | low; WPF adapters had no production consumers | Unit, Architecture | ARCH-001 | verified | `TaskAutomation_DoesNotEnableWpf`; `PixelGeometryTests`; full build |
| ARCH-003 | P1 | Automation presentation is localized exactly once. | `AutomationTrigger.GetDisplayText` and `AutomationAction.DisplayText` duplicated the localized UI formatter with fixed German text. UI consumers already used the localized formatter. | `DesktopAutomationApp.Localization.AutomationDisplayFormatter` | none; unused duplicate API removed | UI, Localization | ARCH-002 | verified | repository search has no domain `GetDisplayText`; localization check |
| ARCH-004 | P1 | Automation validation is decided in shared code and only localized in the UI. | A disabled duplicate validation block and a hard-coded Windows-event message remained in the view model. | `TaskAutomation.Automations.AutomationValidation` | none | Unit, UI, Localization | ARCH-003 | verified | `AutomationValidationTests`; `Validation.WindowsEventRequired` in both resources |
| ARCH-005 | P1 | Required Windows capability parameters have one rule for editor, automation, step validation, and runtime service. | Four consumers independently checked required descriptor values. | `TaskAutomation.WindowsIntegration.WindowsCapabilitySelectionRules` | low; existing behavior retained | Unit, Integration | ARCH-004 | verified | automation and Windows-setting validation tests; consumer search |
| ARCH-006 | P0 | Job-step snapshot, clone, and polymorphic round-trip semantics are not owned by WPF. | `JobStepsSnapshotService` lived in `DesktopAutomationApp` and its contract tests were classified as UI tests. | `TaskAutomation.Jobs.JobStepsSnapshotService` | medium; persisted polymorphic step shape retained | Contract | ARCH-001 | verified | `JobStepsSnapshotServiceTests` round-trip every concrete `JobStep` type |
| ARCH-007 | P2 | Macro cloning and equality snapshots use one serialization policy. | `MakroStepsViewModel` owned JSON options, cloning, and semantic snapshot serialization. | `TaskAutomation.Makros.MakroSnapshotService` | low; identical serializer options retained | Unit, existing UI interaction tests | ARCH-001 | verified | `MakroSnapshotServiceTests`; no JSON serializer remains in the macro view model |
| ARCH-008 | P0 | User-preference defaults, normalization, and atomic persistence are application concerns, not WPF concerns. | Model, port, JSON recovery, and storage implementation lived in `DesktopAutomationApp.Settings`; all consumers and compatibility tests were traced. | `DesktopAutomation.Application.Settings.UserPreferencesService` | medium; JSON names and defaults retained | Contract, Integration | ARCH-001 | verified | `UserPreferencesTests`; `UserPreferencesServiceTests` |
| ARCH-009 | P2 | Tests consume compiled production assemblies rather than compiling private production sources again. | Unit and UI projects linked UI source files, causing duplicate type definitions and warnings. | Production assemblies plus `InternalsVisibleTo` for the explicit test assemblies | none | Architecture, all test levels | ARCH-001 | verified | no production-source `Compile Include` remains; full build |

## Canonical owners confirmed by audit

| Area | Canonical owner |
|---|---|
| Job validation, condition operators, and comparison values | `TaskAutomation.Jobs.JobValidation`, `ConditionRules` |
| Job control-flow structure | `TaskAutomation.Jobs.ControlFlow.ControlFlowStructureAnalyzer` |
| Step descriptors, defaults, draft validation, input and result metadata | `TaskAutomation.Steps.Definitions` and `TaskAutomation.Steps.StepResultMetadata` |
| Job-step snapshots and clone semantics | `TaskAutomation.Jobs.JobStepsSnapshotService` |
| Macro validation and snapshots | `TaskAutomation.Makros.MakroValidation`, `MakroSnapshotService` |
| Automation validation and execution | `TaskAutomation.Automations.AutomationValidation`, `AutomationEngine` |
| Windows capability catalog and required-parameter rule | `TaskAutomation.WindowsIntegration.WindowsCapabilityCatalog`, `WindowsCapabilitySelectionRules` |
| User-preference defaults and persistence | `DesktopAutomation.Application.Settings.UserPreferencesService` |
| Library organization rules | `DesktopAutomation.Application.Services.LibraryOrganizationService` |
| Application data paths and generic JSON repositories | `Common.ApplicationData.AppPaths`, `Common.JsonRepository` |
| Localized automation presentation | `DesktopAutomationApp.Localization.AutomationDisplayFormatter` |

No open architecture violation or undocumented duplicate remains in the audited categories.
