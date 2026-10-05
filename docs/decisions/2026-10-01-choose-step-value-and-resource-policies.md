---
date: 2026-10-01
status: proposed
supersedes:
superseded_by:
---

# Choose step-value and resource policies

## Context

The [step-values audit](../architecture/step-values-audit-2026-10-01.md) exposed divergent
validation, graph traversal and execution. Its defects are repaired under the accepted
[single-owner rule](2026-09-28-single-owner-for-business-rules.md). The user requested a list of
remaining choices that have no single technically correct answer. The choices below remain
proposed; the stated current behavior supplies a working, conservative implementation.

## Decision

| Product question | Current implementation | Alternative |
| --- | --- | --- |
| What happens to inactive inputs? | Preserve their persisted values and references. Skip their validation and resolution while inactive. Preserve valid derived settings; substitute safe derived defaults for malformed inactive settings when applying a draft. | Clear inactive values when switching modes, with an explicit warning about loss. |
| May a structured override create missing members or collection rows? | Both authoring and execution require existing paths. Target schemas own member and item types; incompatible schemas remain invalid rather than being rewritten. | Populate missing, schema-declared members and rows with explicit defaults before applying overrides. Unknown schema members must still be rejected. |
| How should primitive collection variables be edited? | Existing collections use a JSON editor with validation; scalar controls remain reserved for scalar variables. | Add a typed list editor with insert, remove and reorder actions. |
| How should multiple video steps interact? | Preserve the existing shared recorder per job. The first executed video step with a usable frame sets the resolved output path, name and dimensions. Later video steps append frames. A job without usable frames creates no recording. | Use a separate recorder and file per step, or explicitly group recordings. |
| When should YOLO models be loaded? | Preserve preloading of enabled steps at job start, using resolved input values. Track and release successfully loaded models, including cancellation during preload. Disabled steps do not preload. | Load models only when a matching step is actually executed, trading start-up cost for possible first-detection delay. |
| Should secrets be selectable for new text inputs? | Preserve the current exclusion from new source selections. Existing persisted secret references remain readable through an explicit legacy text-provider contract; the editor keeps the reference and masks its preview. | Enable secrets for selected text fields, or offer an explicit migration that removes old secret inputs. |

## Alternatives considered

Automatically correcting unknown tokens, selecting the first enum option or changing invalid
stored values on opening is a defect, not a product choice. Invalid values remain available for
explicit repair. Provider identities, unique IDs within a provider and complete result metadata
also do not require a product preference.

## Consequences

No storage-format migration is introduced. Changing any proposed policy later requires behavior
tests and an explicit compatibility treatment for existing jobs. Video encoder frame normalization
continues to apply when later frames have different dimensions. This repair does not redesign
cross-job sharing or ownership of the application-wide YOLO manager.

## Verification

`StepValuesAuditTests`, `StepValueGraphTests`, `StepResourceInputTests` and
`StoredValueRepairTests` cover the implemented behavior. The repair report records the full
repository verification result. This proposed record is not an instruction to pause authorized
bug fixes or to reinterpret an older accepted decision.
