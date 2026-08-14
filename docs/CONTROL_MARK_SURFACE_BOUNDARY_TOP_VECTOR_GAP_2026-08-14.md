# Control mark: surface-boundary vs internal top-vector gap

Date: 2026-08-14
Issue: #413
Depends on: #407
Scope: validation-only comparison of the solved surface-buoy boundary vector against the current final-iteration internal cumulative top H/V field and the first angle used by the discrete candidate-shape builder. No production authority change.

## 1. Purpose

Earlier isolation showed that replacing only the frozen distributed horizontal line-force field with the fallback shape-normal field does not reproduce current selected X, especially in canonical profile-current cases D/E.

This control mark measures a more fundamental difference between the two model contracts.

The surface-boundary INFO model explicitly solves the buoy-side cable vector

`H_top = D_b`

`V_top = Q0`

subject to the bounded free-surface buoy capacity and depth closure.

The current iterative/discrete candidate path does not consume that boundary vector. `MooringDiscreteLoadTensionAnalyzer` forms its first/top segment row by accumulating line and internal discrete loads from the line model. `MooringDiscreteLoadShapeBuilder` then converts those internal cumulative H/V values into angles and applies a separate global angle scale to close the target depth.

The current internal cumulative top H/V is therefore reported here as an **internal load-sum field**, not renamed as a physical buoy boundary reaction.

The measurements below come from GitHub Actions `.NET Build` run #892 for PR #465 on exact head `9a9af80bcf5042d2dafa13a07f2a4cc4d1ec4e47`.

## 2. Top-vector comparison

| Scenario | Boundary H = D_b, N | Boundary V = Q0, N | Internal top H, N | Internal top V, N | Internal H / D_b | Internal V / Q0 |
|---|---:|---:|---:|---:|---:|---:|
| A | 131.2000 | 855.3166 | 460.6432 | 398.7874 | 3.5110 | 0.4662 |
| B | 369.0000 | 2518.5086 | 1755.0050 | 819.9830 | 4.7561 | 0.3256 |
| C | 203.8725 | 3196.8937 | 1849.5018 | 1035.7293 | 9.0719 | 0.3240 |
| D | 1109.9725 | 7423.5335 | 2501.9128 | 1035.7293 | 2.2540 | 0.1395 |
| E | 523.9004 | 4001.3380 | 1403.7463 | 1035.6924 | 2.6794 | 0.2588 |

The mismatch is material in every canonical case:

- the internal cumulative horizontal field at the top segment is much larger than buoy steady drag `D_b`;
- the internal cumulative vertical field is much smaller than the solved surface reaction `Q0`.

In the validation regression, `InternalTopV` is independently checked against the signed line plus internal-discrete submerged-weight sum. This confirms that the value is the existing internal load accumulation, not a hidden form of the solved buoy reaction.

## 3. Vector magnitude

The ratio of the internal cumulative vector magnitude to the solved surface-boundary vector magnitude is:

| Scenario | |Internal top vector| / |Boundary vector| |
|---|---:|
| A | 0.7041 |
| B | 0.7610 |
| C | 0.6617 |
| D | 0.3608 |
| E | 0.4323 |

The difference is not a simple common scale factor. Horizontal and vertical components change by different factors and the factors vary significantly across scenarios.

Therefore multiplying the existing production tension magnitude by one correction coefficient would not reconcile the two contracts.

## 4. Angle comparison

| Scenario | Boundary angle from vertical, ° | Internal raw angle, ° | Final angle scale | First segment angle actually used, ° |
|---|---:|---:|---:|---:|
| A | 8.7208 | 49.1166 | 0.6836 | 33.5758 |
| B | 8.3354 | 64.9568 | 0.5496 | 35.6977 |
| C | 3.6489 | 60.7510 | 0.4778 | 29.0258 |
| D | 8.5039 | 67.5117 | 0.5737 | 38.7335 |
| E | 7.4594 | 53.5799 | 0.8054 | 43.1543 |

The surface-boundary vector is nearly vertical in all A-E cases: approximately `3.6° ... 8.7°` from vertical.

The current internal cumulative top vector is much more horizontal: approximately `49° ... 68°` from vertical.

The separate geometry-closing angle scale reduces those raw internal angles, but the first segment angle actually used by the final discrete candidate remains approximately `29° ... 43°`, still far from the solved surface-boundary direction.

This is a structural model-contract difference, not a small numerical residual.

## 5. Relationship to the measured X gap

The corresponding canonical horizontal offsets remain:

| Scenario | Boundary X, m | Current selected X, m |
|---|---:|---:|
| A | 21.6460 | 21.1518 |
| B | 57.8485 | 58.2692 |
| C | 139.1318 | 143.9061 |
| D | 150.7430 | 138.6337 |
| E | 149.5170 | 130.0129 |

The large difference in top-vector direction provides a direct mechanism by which the same line length and target depth can produce materially different X distributions even when both paths close endpoint Z.

This finding is consistent with the earlier observation that changing only horizontal segment force did not reconcile D/E. The dominant unresolved difference is not merely drag magnitude; it is the boundary/tension/geometry closure contract itself.

## 6. Engineering interpretation

The current iterative candidate path is useful and internally consistent with its present architecture, but its geometry-driving tension rows are **not boundary-conditioned by the free-surface buoy equilibrium solved in `SurfaceBoundaryInfo`**.

Specifically, the current final discrete-tension field:

- accumulates line and internal discrete H/V loads;
- does not receive solved `Q0`;
- does not receive `D_b` as its top boundary H;
- derives raw segment angles from that internal cumulative field;
- then uses a separate global `AngleScale` to force target-depth closure.

The surface-boundary INFO path instead starts from `(D_b,Q0)` and integrates the signed frozen loads downward without a global angle-rescaling closure.

The two paths therefore solve different boundary-value constructions. Their X difference should not be treated as solver noise.

## 7. Production consequence

This evidence strengthens the existing prohibition on directly promoting `SurfaceBoundaryInfo.SolutionState` to selected X/Z without a dedicated coupled solver package.

It also means that simply injecting `Q0` as a reporting number into the existing discrete shape builder would be insufficient: the horizontal and vertical boundary components, tension propagation and geometry closure need one coherent physics contract.

No change is authorized to:

- `BuoyCalculator`;
- `MooringShapeSolver`;
- `MooringShapeTensionAnalyzer`;
- `MooringDiscreteLoadTensionAnalyzer`;
- `MooringDiscreteLoadShapeBuilder`;
- `MooringIterativeSolver`;
- `MooringPrimaryShapeGate`;
- selected X/Z;
- 2D/PDF geometry;
- anchor/weak-link calculations;
- verdict;
- signed submerged-weight semantics;
- 0.20 m segmentation;
- engineering golden baseline.

## 8. Next safe boundary

The next safe validation work is a **boundary-conditioned per-segment tension trace** that starts from the already solved `(D_b,Q0)` and crosses the same distributed and discrete loads exactly once.

That trace should expose, for every segment:

- H and V at the segment boundary/midpoint;
- tangent angle from the signed H/V vector;
- cumulative discrete-load crossings;
- comparison against the current final discrete-tension row at the same segment/s coordinate.

The trace must remain diagnostic-only. It should not feed `MooringDiscreteLoadShapeBuilder`, `MooringIterativeSolver`, selected X/Z, 2D or PDF until a later Physics RFC explicitly approves a boundary-conditioned production solver.
