---
date: 2026-10-12
status: superseded
supersedes:
superseded_by: 2026-10-12-remove-legacy-input-contract-adapters.md
---

# Unify semantic input contracts

## Context

The user approved implementing the input contract consolidation plan. Root bindings,
nested members and generated editors had separate descriptors and derived source policy
from editor hints. Similar compatibility bugs affected selection, legacy references
and materialization. Shared shapes alone did not consolidate provider checks or conversions.

## Decision

InputContractNode, InputContractSchema, source kinds/cardinalities and result property
metadata live in TaskAutomation.Contracts, retaining their namespaces and stable IDs.
InputContractRegistry owns root and schema definitions, field policy derivation and
member/item path resolution. Step descriptors explicitly declare InputSchemaId,
OwnsStructuredInputContract and specialized InputValueKind; presentation hints do not
determine source policy.

InputCompatibilityEvaluator is the canonical metadata evaluator. It returns exact,
converted, legacy or rejected compatibility with a conversion plan and stable diagnostic
code. NewSelection and ExistingBinding are distinct from Authoring and Runtime.
InputValueConverter executes approved conversions. TextValueFormatter continues to
own display text. Known authoring values and runtime values use the same composition,
source checking and conversion path. Actual target constraints and execution availability
remain with the existing draft validators and materializer.

JsonNumberRules owns finite Number reading for validity checks and execution, including
in-memory float and long values. Number retains its double runtime representation;
Integer retains its 32-bit bounds. Stored JSON is not rewritten.

The generated editor resolves compound contracts by path. Settings members storing
overlay result references use the declared overlay text contract and are not value
overrides. ConditionRules supplies comparison policy using the same evaluator.
Dynamic enum identities remain explicit.

## Alternatives considered

Independent view-model checks or separate root/member evaluators retain competing
semantics. Generics alone cannot validate dynamically referenced persisted graphs.
A new job format is unnecessary and would add migration risk.

## Consequences

Jobs, bindings, schema/provider IDs and legacy aliases remain compatible.
StepInputDescriptor, StepInputContractRegistry, ValueBindingMemberDescriptor and
ValueBindingSchemaRegistry remain compatibility projections and construction adapters;
they have no independent evaluator or registry definition set. Their Boolean methods
delegate to the canonical evaluator. This preserves integrations and the exported
bindingSchemas/fieldSchemas keys. New versioned inputContracts exports describe the
semantic nodes and schemas. Registry structure is checked at catalog setup.

Schemas and nodes freeze lists, provider sets and constraint options. Namespace
compatibility does not promise binary compatibility with independently compiled plugins;
all repository consumers rebuild against the moved transportable types.

Missing input, skipping, ROI feedback, target validation and condition operators retain
their owners. No universal runtime-value envelope or general implicit conversion system
is introduced. The existing source compatibility and text conversion decisions continue
to apply through the canonical registry.

## Verification

Behavior tests cover approved/rejected conversions, legacy colors/providers, missing
optional values, enum identities, invalid paths, policy immutability, transport roundtrips
and numeric storage consistency. Contract matrices cover every built-in field independently
of EditorHint and all schema roundtrips. Architecture tests verify model/evaluator ownership.
Existing UI/materialization/end-to-end tests and the Full gate cover integration boundaries.
