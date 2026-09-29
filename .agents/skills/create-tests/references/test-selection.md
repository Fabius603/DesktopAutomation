# Test selection

| Risk | Primary level | Add another level when |
|---|---|---|
| Pure rule or calculation | Unit | Serialization or composition is separately risky |
| Stable ID, JSON shape, registry | Contract | Runtime resolution also needs proof |
| Dependency direction | Architecture | A user workflow can still violate the intent |
| File/resource/service collaboration | Integration | A critical packaged flow needs assurance |
| WPF binding, template, focus, input | UI | The complete user journey is critical |
| Installation-to-outcome workflow | End-to-end | Keep the set intentionally small |

Each test name should state behavior, scenario, and expected result. Control time, randomness,
culture, files, network, processes, and device state. A retry is not a determinism strategy.
