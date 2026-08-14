# Control mark: canonical surface-boundary INFO measurements

Date: 2026-08-14
Issue: #413
Depends on: #407
Scope: validation-only measurements of the already merged frozen-load surface-boundary INFO read model. No production physics or selected-X/Z change.

## 1. Purpose

Run the existing canonical engineering scenarios A–E through the runtime `MooringSurfaceBoundaryInfoAnalyzer` using the typed `BuoyInput` snapshot path and record the resulting boundary classification and capacity measurements.

The scenarios are the same A–E inputs already used by the engineering golden regression harness. This package does not invent new deployment inputs and does not alter the golden baseline.

The values below are copied from the successful GitHub Actions `.NET Build` run #876 for PR #457 on exact head `00ed3ade61ebe1e6dafee070e946f85eb368891a`.

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
| A | SolvedByBoundedBisection | 131.2000 | 4241.3761 | 855.3166 | 0.20166 | 0.32628 | 21.6460 | 50.0003 | 11 | 3 |
| B | SolvedByBoundedBisection | 369.0000 | 10591.1820 | 2518.5086 | 0.23779 | 0.33075 | 57.8485 | 120.0022 | 11 | 4 |
| C | SolvedByBoundedBisection | 203.8725 | 19172.0008 | 3196.8937 | 0.16675 | 0.27760 | 139.1318 | 379.9917 | 12 | 4 |
| D | SolvedByBoundedBisection | 1109.9725 | 19172.0008 | 7423.5335 | 0.38721 | 0.46873 | 150.7430 | 379.9943 | 11 | 4 |
| E | SolvedByBoundedBisection | 523.9004 | 19174.5897 | 4001.3380 | 0.20868 | 0.31394 | 149.5170 | 379.9971 | 14 | 4 |

All five solved `Q0` values are strictly inside `[0, Q_capacity]` and all solved endpoint depth residuals satisfy the existing INFO integration tolerance of approximately `0.01 m`.

The measured `Q0/Q_capacity` range for these five canonical inputs is approximately `0.1667 ... 0.3872`. This is an observed range only; it is **not** an engineering acceptance threshold.

## 3. Boundary residuals

The endpoint residuals at the two allowed `Q0` boundaries were:

| Scenario | Z(Q0=0)-Depth, m | Z(Q_capacity)-Depth, m |
|---|---:|---:|
| A | -60.5808 | +4.7851 |
| B | -131.1161 | +13.9894 |
| C | -454.3866 | +29.1369 |
| D | -407.3093 | +25.1754 |
| E | -431.9140 | +28.6531 |

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

The check passed in `.NET Build` run #876. This confirms that the runtime INFO boundary is independent of the existing wave term, as required by the Chapter-2 steady-current contract. This does not change the existing wave contribution elsewhere in `BuoyCalculator`.

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
