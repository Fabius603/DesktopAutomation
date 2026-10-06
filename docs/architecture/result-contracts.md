# Step result contracts

Step result behaviour is owned by the backend.

- A handler returns one `StepResultBase` object.
- Every selectable public result property must declare an explicit
  `ResultProperty` ID. Contract discovery fails immediately when an ID is
  missing or duplicated.
- Public result properties are discovered once and exposed through
  `ResultTypeDescriptor`.
- The single type system, `ResultValueKind` plus `ResultCardinality`, defines compatibility with backend
  input contracts.
- `ResultPropertyAttribute` assigns stable persisted property IDs independent
  of CLR member names.
- `StepResultContractRegistry` resolves the contract of the fully configured
  step. Fixed steps use their registered CLR result type; dynamic steps use a
  dedicated provider.
- New value references persist only `provider_id` and the provider-owned
  `source_id`. The `step_result` provider encodes the stable step and property
  IDs in its versioned source ID. Legacy `source_step_id`, `property_id`, and
  `property_path` values remain readable for existing jobs.
- Compound inputs persist a `ResultBinding` tree. A node may reference a complete
  base value and override stable `members` or indexed `items`. Every compound
  root uses a versioned `schema_id`; provider references are never embedded in
  the variable's JSON value. Legacy dotted input keys are migrated into this tree.
- `ValueBindingSchemaRegistry` owns the stable member IDs, item schemas, types,
  cardinalities, and provider policies for compound values. Validation and runtime
  materialization traverse the same tree recursively.
- Every non-step provider exposes typed `ValueProviderSourceDescriptor`
  metadata to editors and implements `IRuntimeValueProvider` for resolution.
  Compatibility is decided by the same input-contract shapes used for step
  results; editors must not add provider-specific compatibility rules.
- Sensitive providers expose metadata only while editing. Their values are
  loaded only for referenced sources when execution starts, remain outside the
  serialized job, and must never be written to logs or condition diagnostics.
- `StepInputDescriptor.AllowedProviderIds` may restrict an input to explicit
  providers. A missing restriction means that every registered provider is
  allowed; type and cardinality compatibility still applies in both cases.
- The shared `ValueReferencePicker` owns provider grouping, search, empty and
  missing-reference states, sensitive-value masking, and compatible job-variable
  creation. Step editors configure only their stable input-contract ID.

## Capture provenance and consecutive 3D movements

Capture results expose `frame_version` and `frame_timestamp` (monotonic Stopwatch/QPC
ticks, zero when unavailable). Desktop versions advance only for an actual DXGI
desktop-image update; cached and pointer-only frames preserve the image version,
presentation timestamp and UTC timestamp. Camera timestamps describe receipt, not
the camera's exposure or processing of application input. Versions are runtime
identities within their capture producer, not persisted or cross-source ordering.

Detection and prediction results preserve `source_frame_version` and
`source_frame_timestamp`, including misses. Consumers obtain provenance through
the existing result binding, without a second image binding.

`KlickOnPoint3DStepHandler` owns consecutive-movement suppression. The job-run
context retains the last successfully sent integer X/Y offset for each step ID
across result resets. Offsets within `movement_threshold_px` on both axes after
scaling and rounding suppress both movement and click and report `movement_blocked`.
The advanced setting defaults to 10 pixels (also for existing jobs); zero restores
exact equality. The boundary is inclusive and differences use widened arithmetic.
Only a successfully sent offset outside this range replaces the remembered movement;
blocked offsets cannot gradually shift the comparison baseline. Missing, stale, timed-out, failed or
cancelled actions do not clear it. Each new job context starts empty. This is
duplicate suppression, not confirmation that a target application processed input.

The same handler retains the QPC timestamp immediately after each successful input
macro. A detection with a known source-frame timestamp at or before that boundary
suppresses movement and click even if its offset differs and its capture is fresh.
Suppression does not advance either state; only successful input does. Unknown
timestamps (zero) retain legacy behavior. This adds a temporal ordering check without
a delay or an image dependency in the input step. A later presentation still does not
prove that the target application processed the input; camera receipt times provide
an even weaker ordering guarantee. Diagnostic summaries retain frame identity and
timestamps so subsequent runs can be checked against the input boundary.

## Adding a fixed-result step

1. Create a focused record derived from `StepResultBase`.
2. Return it from `JobStepHandler<TStep, TResult>`.
3. Register the handler and result type in `StepPipelineRegistry`.
4. Add backend input contracts for every result binding consumed by the step.

## Adding a configuration-dependent result

1. Create one focused result record per meaningful schema.
2. Implement an `IStepResultContractProvider`.
3. Register the provider in `StepResultContractRegistry`.
4. Derive the handler from `DynamicJobStepHandler<TStep>`. Runtime contract
   validation then rejects mismatches between configuration and returned type.

The frontend may localize and render descriptors, but it must not invent
result properties or compatibility rules.

The complete project checklist for adding a new step is documented in
[`add-job-step`](../../.agents/skills/add-job-step/SKILL.md).
