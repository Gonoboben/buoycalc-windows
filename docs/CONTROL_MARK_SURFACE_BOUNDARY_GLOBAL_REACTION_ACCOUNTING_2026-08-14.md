# Control mark: whole-line surface/anchor reaction accounting

Date: 2026-08-14
Issue: #413
Depends on: #407
Scope: validation-only whole-line reaction accounting for the frozen-load surface-boundary model versus the current internal cumulative candidate field. No production solver, anchor-check, or selected-X/Z change.

## 1. Purpose

The per-segment trace showed that the boundary-conditioned H/V field and the current final discrete-tension field use different reference-side/boundary contracts along the line.

This control mark closes the whole-line accounting explicitly.

For the already solved frozen-load INFO state:

- surface-side horizontal component is `H_top = D_b`;
- surface-side vertical component is `V_top = Q0`;
- internal horizontal loads are the existing segment current forces plus existing internal connector/payload current forces;
- internal vertical loads are the existing signed segment and internal discrete submerged weights;
- the trace is propagated to the anchor-side end without inventing a new boundary force.

The validation then compares the resulting terminal H/V with the current final discrete-tension top/bottom accumulation.

The values below come from GitHub Actions `.NET Build` run #899 for PR #468 on exact head `12eca846d42f5f5b7b157b8cb4d0cd045d4d3a50`.

## 2. Horizontal whole-line identity

For every canonical A–E case, validation verifies to `1e-6 N`:

`InternalFx = Σ segment CurrentForceN + SequencePositions.DiscreteCurrentForceN`

`H_terminal = D_b + InternalFx`

and

`H_terminal = CalculationResult.CurrentForceN`.

Measured values:

| Scenario | D_b, N | Internal Fx, N | H_terminal, N | CalculationResult.CurrentForceN, N |
|---|---:|---:|---:|---:|
| A | 131.2000 | 460.6432 | 591.8432 | 591.8432 |
| B | 369.0000 | 1755.0050 | 2124.0050 | 2124.0050 |
| C | 203.8725 | 1849.5018 | 2053.3743 | 2053.3743 |
| D | 1109.9725 | 2501.9128 | 3611.8853 | 3611.8853 |
| E | 523.9004 | 1403.7463 | 1927.6467 | 1927.6467 |

This is the steady-current identity only. `WaveForceN` remains outside the Chapter-2 frozen-load boundary model and is not removed or redefined elsewhere in `BuoyCalculator` or the anchor checks.

## 3. Vertical whole-line identity

Validation also verifies:

`InternalWeightN = (Σ segment WeightWaterKg + SequencePositions.DiscreteWeightWaterKg) * g`

and

`V_terminal = Q0 - InternalWeightN`.

Equivalently:

`Q0 = InternalWeightN + V_terminal`.

Measured values:

| Scenario | Q0, N | Internal signed weight, N | V_terminal, N | V_terminal / Q0 |
|---|---:|---:|---:|---:|
| A | 855.3166 | 398.7874 | 456.5291 | 0.5338 |
| B | 2518.5086 | 819.9830 | 1698.5256 | 0.6744 |
| C | 3196.8937 | 1035.7293 | 2161.1643 | 0.6760 |
| D | 7423.5335 | 1035.7293 | 6387.8042 | 0.8605 |
| E | 4001.3380 | 1035.6924 | 2965.6456 | 0.7412 |

The anchor-side terminal vertical cable component is therefore materially non-zero in all five canonical cases. It is especially large in D/E.

No `Abs(WeightWaterKg)` is introduced: the identity uses the existing signed submerged-weight convention.

## 4. Current production cumulative top field

The same regression independently verifies that the current final `MooringDiscreteLoadTensionAnalyzer` top row equals the internal load sums:

`ProductionTopH = InternalFx`

`ProductionTopV = InternalWeightN`.

Measured top values:

| Scenario | Production top H, N | Production top V, N |
|---|---:|---:|
| A | 460.6432 | 398.7874 |
| B | 1755.0050 | 819.9830 |
| C | 1849.5018 | 1035.7293 |
| D | 2501.9128 | 1035.7293 |
| E | 1403.7463 | 1035.6924 |

This confirms the existing code contract: the candidate tension field is an internal cumulative-load field. It does not contain `D_b` as a surface horizontal boundary component and does not contain solved `Q0` as a surface vertical boundary component.

## 5. Anchor-side direction gap

The boundary-conditioned terminal vector has the following direction from vertical:

| Scenario | H_terminal, N | V_terminal, N | Boundary terminal angle, ° | Production bottom raw angle, ° | Production bottom used angle, ° |
|---|---:|---:|---:|---:|---:|
| A | 591.8432 | 456.5291 | 52.3545 | 5.1960 | 3.5520 |
| B | 2124.0050 | 1698.5256 | 51.3514 | 8.1138 | 4.4590 |
| C | 2053.3743 | 2161.1643 | 43.5349 | 2.9381 | 1.4038 |
| D | 3611.8853 | 6387.8042 | 29.4853 | 15.2379 | 8.7424 |
| E | 1927.6467 | 2965.6456 | 33.0236 | 7.3264 | 5.9008 |

The current production bottom row is not an explicit anchor-side boundary reaction. It is the remaining bottom-side internal cumulative load represented by the existing analyzer. The final `UsedAngleFromVerticalDeg` then applies the separate geometry-closing angle scale.

The boundary-conditioned terminal vector is therefore not recoverable from the current bottom candidate angle by a small residual correction.

## 6. Engineering interpretation

The combined top-vector, per-segment, and whole-line measurements now establish a coherent accounting picture:

1. the frozen-load boundary model starts from the solved surface reaction `(D_b,Q0)`;
2. crossing existing signed internal loads produces a non-zero terminal anchor-side cable vector;
3. its terminal horizontal component closes exactly to the existing total steady `CurrentForceN`;
4. its terminal vertical component is the part of `Q0` not consumed by signed internal submerged weight;
5. the current candidate tension field instead represents cumulative internal loads without injecting either solved surface reaction or an explicit terminal anchor-side reaction;
6. `MooringDiscreteLoadShapeBuilder` then uses a global `AngleScale` to close geometry.

This is a boundary-value-contract difference, not a floating-point discrepancy.

## 7. What this does not change

This validation does **not** redefine the current anchor engineering checks.

In particular:

- `CalculationResult.CurrentForceN` remains the existing steady-current total;
- `HorizontalForceN` and its wave contribution remain unchanged;
- anchor holding, required holding, reserve and verdict remain unchanged;
- the terminal boundary vector is not automatically an anchor holding load or a new anchor verdict input;
- seabed and anchor-type multipliers are not touched.

A later physics package would need an explicit engineering contract before any boundary-conditioned cable reaction could affect anchor calculations.

## 8. Production consequence

No change is authorized to:

- `BuoyCalculator`;
- `MooringShapeSolver`;
- `MooringDiscreteLoadTensionAnalyzer`;
- `MooringDiscreteLoadShapeBuilder`;
- `MooringIterativeSolver`;
- `MooringPrimaryShapeGate`;
- selected X/Z;
- 2D/PDF geometry;
- anchor/weak-link calculations;
- verdict;
- signed submerged-weight semantics;
- 0.20 m segmentation or unlimited segment count;
- engineering golden baseline.

## 9. Next safe boundary

The validation evidence is now sufficient to define a **passive boundary-conditioned tension-trace read model** in the runtime/report-data layer, provided it remains diagnostic-only and is built from the already solved `SurfaceBoundaryInfo` plus existing segment/sequence loads.

That read model may expose per-segment boundary-conditioned H/V/tangent and terminal reaction provenance, but it must not feed:

- `MooringDiscreteLoadShapeBuilder`;
- `MooringIterativeSolver`;
- selected X/Z;
- 2D/PDF geometry;
- anchor or verdict calculations.

Before such a read model is coded, a small architecture control mark should define its ownership, naming, availability and no-consumer boundary so the validation algorithm is not copied ad hoc into UI/report code.
