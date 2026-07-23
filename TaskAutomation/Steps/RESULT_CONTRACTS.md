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
- `ResultBinding` persists both `property_id` and the legacy `property_path`.
  New code resolves the stable ID first and falls back to the path.

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
[`ADDING_A_JOB_STEP.md`](ADDING_A_JOB_STEP.md).
