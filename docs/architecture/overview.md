# Architecture overview

DesktopAutomation is a Windows .NET 8 solution with a WPF application shell.

- `DesktopAutomationApp` owns presentation, interaction, localization, and composition.
- `DesktopAutomation.Application` coordinates application-level use cases.
- `TaskAutomation` owns job, automation, macro, validation, execution, and Windows integration.
- `TaskAutomation.Contracts` contains transportable frontend-neutral descriptors and contracts.
- `Common.JsonRepository` owns application data paths and JSON persistence infrastructure.
- Image, capture, overlay, hotkey, and input projects provide bounded technical capabilities.

Dependencies should point from presentation and infrastructure toward stable application and
contract abstractions. Shared business rules must not depend on WPF or Windows Forms. Each
business rule has one canonical owner; frontends project its result rather than reimplementing it.
See the accepted [single-owner decision](../decisions/2026-09-28-single-owner-for-business-rules.md).

The implemented dependency direction is:

1. `TaskAutomation.Contracts` and `Common.JsonRepository` have no production-project dependencies.
2. Technical image, capture, overlay, logging, and Windows adapters depend only on lower technical
   capabilities and shared contracts.
3. `TaskAutomation` consumes contracts, persistence infrastructure, and bounded technical
   capabilities. It owns domain and runtime decisions and does not enable WPF.
4. `DesktopAutomation.Application` consumes `TaskAutomation` and persistence infrastructure to
   coordinate use cases and application-wide settings.
5. `DesktopAutomationApp` consumes application, domain, contract, and technical services as the
   composition and presentation root.

Important canonical owners are `JobValidation` and `ConditionRules` for job validity,
`ControlFlowStructureAnalyzer` for job structure, step definitions and result metadata for editor
contracts, `AutomationValidation` for automation validity, `WindowsCapabilitySelectionRules` for
required Windows capability inputs, `JobStepsSnapshotService` and `MakroSnapshotService` for
domain cloning/snapshots, and `UserPreferencesService` in the application layer for preference
defaults and persistence. The current audit evidence is maintained in the
[architecture conformance register](conformance-register.md).

Detailed contracts:

- [Desktop capture](desktop-capture.md)

- [Structured user logs](logging.md)

- [Step result contracts](result-contracts.md)
- [Windows integration](windows-integration.md)
- [Testing strategy](testing-strategy.md)
