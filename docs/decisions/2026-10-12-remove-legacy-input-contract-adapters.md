---
date: 2026-10-12
status: accepted
supersedes: 2026-10-12-unify-semantic-input-contracts.md
superseded_by:
---

# Remove legacy input contract adapters

## Context

The user explicitly requested switching every consumer to the new model and removing
the former descriptor and registry types completely. Although their compatibility
methods already delegated, root and member definitions still used them as construction
adapters. Their continued presence allowed new consumers to use the old API.

## Decision

InputContractRegistry defines roots and schemas directly using InputContractNode and
InputContractSchema. StepInputDescriptor, StepInputContractRegistry,
ValueBindingMemberDescriptor, ValueBindingSchemaDescriptor and ValueBindingSchemaRegistry
are deleted. No type aliases or obsolete forwarding classes replace them. Frontends,
validation, migration, runtime, documentation and tests use the canonical model.

InputCompatibilityEvaluator owns source and shape evaluation, with named compatibility
profiles. Its frontend-neutral extension entry points operate on InputContractNode;
the DTO itself does not acquire backend dependencies. Derived record policies freeze
caller-owned collections and constraint options, including assignments through `with`.
Contracts admitting several exact source kinds use a null TargetKind for a union;
display text explicitly declares its Text target and DisplayText conversion.

InputSchemaIds in TaskAutomation.Contracts contains unchanged persisted schema IDs.
The existing job reader continues interpreting legacy references and dotted binding
paths. These persisted representations do not require old C# contract models.

Published bindingSchemas, fieldSchemas and root input reference keys remain stable.
InputContractExport in the website exporter projects canonical data to those JSON keys;
it has no compatibility decisions, validation methods or domain descriptor types.
The semantic inputContracts reference continues exporting canonical models directly.

## Alternatives considered

Obsolete attributes leave the old types available and allow warnings to be ignored.
Aliases or renamed replicas retain duplicate models. Deleting persisted compatibility
would unnecessarily break existing jobs and was not authorized by the user.

## Consequences

The repository rebuilds all consumers. References to the deleted types now fail to
compile. A compiled-assembly architecture test rejects their reintroduction. Consumers
compiled independently against those old public C# types need to adopt the new model;
job files and reference JSON remain backward compatible.

The previous canonical owners for operators, conversions, missing runtime values,
target constraints, schema traversal and Number reading remain unchanged. This is a
structural removal and does not add a user-visible feature or require another changelog item.

## Verification

Retain the source-policy and transport matrices, authoring/materialization regressions,
UI interactions, legacy persisted job cases and numeric roundtrip tests. Add a compiled
type-absence architecture test and a derived-node immutability test. Regenerate and
strictly check product references, maintain screenshot dependencies and inspect affected
captures. Complete the Full repository gate after all production changes.
