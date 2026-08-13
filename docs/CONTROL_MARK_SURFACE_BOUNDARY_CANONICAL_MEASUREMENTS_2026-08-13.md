# Control mark: canonical surface-boundary INFO measurements

Date: 2026-08-13
Issue: #413
Depends on: #407
Scope: validation-only measurements of the already merged frozen-load surface-boundary INFO read model. No production physics or selected-X/Z change.

## 1. Purpose

Run the existing canonical engineering scenarios A–E through the runtime `MooringSurfaceBoundaryInfoAnalyzer` using the typed `BuoyInput` snapshot path and record the resulting boundary classification and capacity measurements.

The scenarios are the same A–E inputs already used by the engineering golden regression harness. This package does not invent new deployment inputs and does not alter the golden baseline.

The INFO model remains:

- frozen-load midpoint integration;
- steady-current only;
- wave excluded;
- project `+Z` downward;
- `Q0` bounded by full-volume buoyancy capacity;
- diagnostic X/Z is not a selected-shape source.

## 2. Measured results

| Scenario | Classification | D_b, N | Q_capacity, N | Q0, N | Q0/Q_capacity | B_actual/B_max | Xdiag, m | Zdiag, m | Iterations | Internal point loads |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| A | SolvedByBoundedBisection | 268.96 | 4241.8761 | 382.5564 | 0.09019 | 0.15340 | 24.6360 | 49.9910 | 13 | 1 |
| B | SolvedByBoundedBisection | 492.00 | 10594.5821 | 844.4892 | 0.07971 | 0.20757 | 54.0876 | 119.9965 | 15 | 2 |
| C | SolvedByBoundedBisection | 297.4199 | 19132.9675 | 1256.0010 | 0.06565 | 0.19468 | 137.0576 | 379.9924 | 14 | 2 |
| D | SolvedByBoundedBisection | 1619.2859 | 19132.9675 | 1990.2272 | 0.10402 | 0.27535 | 152.8158 | 379.9983 | 15 | 2 |
| E | SolvedByBoundedBisection | 864.4610 | 19132.9675 | 1459.5015 | 0.07628 | 0.22712 | 143.1204 | 380.0086 | 14 | 2 |

All five solved `Q0` values are strictly inside `[0, Q_capacity]` and all solved endpoint depth residuals satisfy the existing INFO integration tolerance of approximately `0.01 m`.

The measured `Q0/Q_capacity` range for these five canonical inputs is approximately `0.0656 ... 0.1040`. This is an observed range only; it is **not** an engineering acceptance threshold.

## 3. Boundary residuals

The endpoint residuals at the two allowed `Q0` boundaries were:

| Scenario | Z(Q0=0)-Depth, m | Z(Q_capacity)-Depth, m |
|---|---:|---:|
| A | -43.3923 | +4.8786 |
| B | -111.3581 | +9.9351 |
| C | -357.9440 | +29.8938 |
| D | -362.2051 | +29.8253 |
| E | -358.8295 | +29.8785 |

Thus the frozen-load depth root was bracketed inside the buoy-capacity interval for every canonical A–E case.

## 4. Wave-exclusion check

Scenario A was recalculated with the wave input changed from its canonical `1.0 m / 6.0 s` to `0 / 0` while all steady-current inputs were retained.

The validation requires equality to numerical tolerance (`1e-10`) for:

- classification / availability / solved state;
- `D_b`;
- `Q_capacity`;
- solved `Q0`;
- lower and capacity depth residuals;
- diagnostic endpoint X/Z.

The check passed. This confirms that the runtime INFO boundary is independent of the existing wave term, as required by the Chapter-2 steady-current contract. This does not change the existing wave contribution elsewhere in `BuoyCalculator`.

## 5. Interpretation limits

These measurements establish that the current frozen-load INFO boundary produces a bounded, numerically resolved surface-buoy vertical reaction for the five existing canonical scenarios.

They do **not** establish that:

- the diagnostic X/Z should replace current selected X/Z;
- the frozen-load model is a full angle-coupled Berteaux cable solution;
- `Q0/Q_capacity` alone is an acceptance or safety criterion;
- the measured diagnostic offsets should be used by 2D/PDF geometry;
- anchor reserve, weak-link verdicts or existing buoyancy checks should be recomputed from this INFO model.

No threshold, gate, verdict or golden-baseline change is introduced here.

## 6. Next safe boundary

Before any production shape consumer switch, #407/#413 still require an explicit impact comparison between the current authoritative selected-X/Z path and this boundary-conditioned frozen-load diagnostic, including the existing canonical scenarios and the signed-load/Berteaux source constraints.

That comparison must remain validation-only until a separate Physics RFC package explicitly authorizes any change in production authority.
