---
date: 2026-10-08
status: proposed
supersedes:
superseded_by:
---

# Isolate step state and make failure policies explicit

## Context

A step audit found 23 defects involving file safety, reusable detector state, resource ownership,
missing inputs, feedback across iterations, and native execution. The user requested fixes and
behavior tests for every finding. This record captures the implemented choices for review;
it complements the accepted child-execution ownership decision without superseding it.

## Decision

| Finding | Implemented behavior and user consequence |
| --- | --- |
| 1. Cross-volume move rollback | After successful copying, failure while deleting the source fails the step and retains the complete destination. The remaining source may need manual cleanup; the recoverable copy is never rolled back. |
| 2. Copy target race | Copy into a unique sibling staging path and publish with a non-overwriting move. Cleanup removes only the owned staging path. An independently created target survives and the step fails. |
| 3. 3D action from reusable point | Local/shared point values do not require detector metadata. They execute normally; detector freshness checks remain applicable to detector results. |
| 4. Template matching termination | Suppressed candidate scores cannot pass any permitted threshold, including zero. Long multiple-match searches observe cancellation between native searches. |
| 5. Parallel child admission | Each phase scope admits at most 32 simultaneous owned operations. Producers await free capacity; operations are neither dropped nor failed merely because capacity is full. Cancellation interrupts admission. |
| 6. Matching mode reuse | Every template step applies its own matching mode before detection, including when the matcher is reused. |
| 7. Multiple points | Honor the existing persisted flag. Disabled means one best match; enabled means all accepted unsuppressed matches. The flag is included in the generated editor and details. |
| 8. Supported matching modes | The editor offers the three normalized modes supported by the algorithm. Unsupported persisted tokens remain explicit validation errors, rather than silently switching algorithms. |
| 9. Multiple video steps | One recorder per step ID, including its own path, name, dimensions and frame stream. Each recorder is finalized and disposed even if another fails. Log results belong to the correct step. |
| 10. Template confidence scale | Return confidence in the common 0..1 scale. Percentage formatting now displays the correct value; consumers with manually compensated thresholds should remove that compensation. |
| 11. Per-item confidence | Every detection retains its own confidence instead of inheriting the best detection's confidence. |
| 12. OCR confidence scale | Tesseract page confidence is already normalized; preserve it. Iterator confidence still needs normalization. Thresholds now operate on the actual recognition quality. |
| 13. ROI from job variables | A reusable rectangle can supply a dynamic ROI without a source step ID. Capture offsets are still applied. |
| 14. ROI feedback | Only a DynamicRoiStep may supply a forward reference for the dynamicRoi input. Before its first execution, use the base search area. Across iterations, reuse only its geometry state, never general old results. Reset and periodic base searches remain effective. The picker exposes this narrow feedback choice. |
| 15. Missing optional inputs | An existing, correctly typed optional producer that did not execute causes its consumer to be skipped with NoInput, including optional scalar settings and control steps. A skipped EndJob therefore does not end the job or invent an end-phase policy. The debugger also shows the skip. Required missing inputs, unknown sources, nonexistent properties and wrong types remain errors. Empty executed results retain their existing handler behavior. |
| 16. OCR outside image | An enabled ROI with no image intersection produces an empty result without OCR work. It never expands implicitly to the entire image. |
| 17. Stale prediction samples | Sample expiry uses the current clock, not the cached capture timestamp. An expired frozen frame cannot create or sustain a prediction. |
| 18. Point arithmetic | Widen coordinate differences before subtraction and squaring. Large valid coordinates cannot wrap into small distances or crash absolute-difference comparisons. |
| 19. All point comparison | All requires a value from every configured point source and all resolved points to match. Any can still succeed using an available matching point. TotalCount continues to report resolved points. |
| 20. Parallel macros | Serialize whole macros through the shared executor. Release held input before granting the next macro access. A queued macro can be cancelled without sending input. This can delay macros from parallel jobs. |
| 21. OCR engine initialization | Create one lazy native engine per language combination; competing factory calls do not allocate unused engines. Failed initialization can be retried. |
| 22. Encoder initialization | Share default FFmpeg initialization and allow callers to cancel their wait. Recheck cancellation before launching the encoder. A shared download may continue for other callers, but a cancelled recording launches no process. |
| 23. Recording errors | Conversion/write/finalization errors and nonzero encoder exits propagate. A missing or empty expected output also fails. A failed recorder cannot be reported as successfully saved, including on repeated stop calls. |

Persisted job format and existing source IDs are retained. No data migration is introduced.

## Alternatives considered

Deleting the destination on move failure was rejected because source deletion may already be
partial. Copying directly into the final target makes ownership ambiguous on races. Silent
fallbacks for unsupported modes or invalid references hide configuration errors. A shared video
recorder mixes user-selected destinations. Unlimited child launches exhaust processes and
memory; dropping excess executions loses requested work. Retaining all previous-iteration
results makes inactive branches read stale values. Macro key ownership alone does not prevent
mouse commands or multi-command combinations from interleaving.

## Consequences

Temporary copies require destination capacity until publication. Cross-volume deletion failures
can leave partial sources alongside a complete destination. One-best-match mode, normalized
confidence, optional-input skips, bounded admission and macro serialization are deliberate
observable corrections. ROI feedback is narrow and starts with a full/base search. Native OCR
engines and the shared downloader remain application-level resources; recordings are step-owned.

## Verification

- Real temporary-file tests reproduce publication races and injected partial source deletion.
- Native template tests cover all normalized modes, zero thresholds, cancellation, mode changes,
  one/multiple results, and normalized confidence.
- Native OCR tests check actual recognition confidence and competing calls with one engine.
- Value tests cover reusable points/rectangles, unavailable optional/required inputs, broken
  references, ROI feedback initialization/reset, missing point values and wide coordinates.
- Deterministic clock tests expire frozen predictions; controlled async gates test admission,
  macro serialization and cancellation without timing-based sleeps.
- Encoder boundary tests use a local subprocess, no downloads, and verify cancellation,
  encoder exit errors and missing outputs.
- Job-level tests execute actual handlers and materialization across repeating ROI feedback,
  skipped optional producers, and independent video finalization including one failed recorder.
- The repository Full gate remains required before completion.
