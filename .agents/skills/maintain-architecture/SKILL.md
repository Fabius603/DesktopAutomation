---
name: maintain-architecture
description: Preserve DesktopAutomation layer boundaries and consolidate duplicated or divergent business rules when implementing features, fixing bugs, or refactoring shared behavior.
---

# Maintain architecture boundaries

Read `.agents/instructions/architecture.md` and `docs/architecture/overview.md` before editing.
For a durable boundary change, also use `.agents/skills/record-decision/SKILL.md`.

1. Write down the observable invariant being changed.
2. Trace its path across persistence, contracts, application logic, execution, and all frontends.
3. Search for every implementation and consumer; distinguish presentation projections from
   independent business decisions.
4. Name one canonical owner in the lowest appropriate frontend-neutral layer.
5. Move or extend the rule there and make callers delegate to it. Use explicit policy inputs when
   callers legitimately need different behavior.
6. Remove, redirect, or document competing implementations in the touched scope.
7. Test the invariant at the canonical owner. Test frontends only for binding, interaction,
   localization, accessibility, and rendering behavior.
8. Add an architecture test when dependency direction or the forbidden dependency can be checked
   deterministically.

Before handoff, state the canonical owner and disclose any duplicate left behind with its migration
record. Do not call the work complete if new business logic remains in presentation code.
