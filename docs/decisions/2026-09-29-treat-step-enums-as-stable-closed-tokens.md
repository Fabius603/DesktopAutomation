---
date: 2026-09-29
status: accepted
supersedes:
superseded_by:
---

# Treat step enums as stable closed tokens

## Context

Step fields, CLR enums, localized labels, dynamic result schemas, and picker selections previously
shared the word "enum" without sharing one contract. Presentation code matched values without
case sensitivity, selected the first option for missing values, and added unknown persisted values
to the picker. Backend validation used exact matching. Merely opening an editor could therefore
make an invalid value appear valid or silently replace it.

## Decision

A step enum is a closed set of stable, case-sensitive persisted string tokens. A
`StepFieldOptionDescriptor.Value` is the token; its label is presentation-only. Required enum
fields declare an explicit `DefaultValue`, and list order never supplies a default.

`StepEnumRules` is the canonical owner for fixed step-field token membership and defaults.
`StepDefinitionCatalog` rejects missing options, missing required defaults, unknown defaults, and
divergent allowed-value lists. Editors bind selection by token and may display, but never add or
normalize, an unknown value. Unknown persisted values stay intact and invalid until the user
chooses a supported token. CLR conversion is exact and cannot fall back to enum value zero.

Dynamic result enums keep their existing schema identity in `EnumTypeName`. Typed and untyped enum
schemas are not compatible, and comparison tokens are also matched exactly. Persisted JSON remains
a string, so no storage-format migration is introduced. Any future historical rename requires an
explicit frontend-neutral migration for the documented old token.

## Alternatives considered

- Match tokens without case sensitivity: convenient for accidental casing changes, but silently
  changes a persisted compatibility contract.
- Treat the first option as the default: compact, but makes presentation order a business rule.
- Add unknown stored values to the picker: preserves visibility, but falsely presents invalid data
  as a supported option.
- Persist localized labels or CLR ordinals: both are unstable across translations and refactors.

## Consequences

Changing option labels is safe; changing tokens or schema IDs is a persistence migration. Invalid
legacy values remain inspectable and repairable but block materialization. Frontends must project
the shared descriptor and may not invent defaults or token aliases. Step definitions with CLR enum
settings must use the shared exact reader after validation.

## Verification

Unit and contract tests verify descriptor invariants, exact comparison, dynamic schema identity,
and rejection of invalid CLR tokens. UI tests verify token-based binding, absence of implicit
selection, preservation of unknown values, visible validation, and constrained picker layout.
