# Testing strategy

The repository uses separate test assemblies so scope, speed, dependencies, and failures remain
visible. Tests assert behavior and invariants rather than mirroring implementation flow.

| Level | Purpose | Infrastructure |
|---|---|---|
| Unit | Domain rules and state transitions | None |
| Contract | Persistence, metadata, stable IDs, compatibility | In-memory or fixtures |
| Architecture | Dependency and repository structure | Compiled assemblies or source tree |
| Integration | Real collaboration of components | Isolated files and controlled adapters |
| UI | WPF bindings, resources, automation, interactions | STA Windows process where required |
| End-to-end | Critical packaged workflows | Interactive isolated Windows runner |

`powershell -NoProfile -ExecutionPolicy Bypass -File eng/verify.ps1 -Mode Full` is the single
required local and CI entrypoint. Checks use explicit environment settings, fixed output
locations, bounded execution, and no reliance on test order.

Test generation means selecting the required levels and scaffolding scenarios from an observable
behavior. It does not mean producing assertions that merely repeat the current implementation.
