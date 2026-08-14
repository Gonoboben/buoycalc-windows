# Control mark: shape-normal frozen-field isolation

Date: 2026-08-14
Issue: #413
Depends on: #407
Scope: validation-only isolation of the horizontal segment-force field inside the existing bounded surface-boundary INFO solve. No production physics or selected-X/Z change.

## 1. Purpose

The selected-X/Z impact and iteration-path measurements showed a material horizontal difference between the current authoritative iterative selected shape and the frozen-load surface-boundary diagnostic, especially in canonical current-profile cases D/E.

This experiment changes exactly one part of the frozen boundary input field:

- keep the same buoy steady drag `D_b`;
- keep the same connector/payload point loads and positions;
- keep the same signed segment/point water weights;
- keep the same bounded `Q0` closure and midpoint integration;
- replace each frozen segment `CurrentForceN` with the already calculated `MooringShapeForceAnalyzer.ShapeForceN` from the current fallback-shape projection.

The alternate field exists only inside validation code. It is not stored in `CalculationResult`, not used by `SelectedShape`, and does not modify the production iterative solver.

The values below come from GitHub Actions `.NET Build` run #888 for PR #463 on exact head `13df2bf59e6cfd02466b679d48f9ce8767bce89e`.

## 2. Horizontal line-force change

| Scenario | Original line force, N | Shape-normal frozen line force, N | Reduction |
|---|---:|---:|---:|
| A | 432.9600 | 357.8326 | 17.35% |
| B | 1660.5000 | 1312.1763 | 20.98% |
| C | 1815.4800 | 1559.4507 | 14.10% |
| D | 2316.6830 | 1868.9830 | 19.33% |
| E | 1316.3190 | 956.8898 | 27.31% |

The orientation-dependent fallback-shape projection therefore materially reduces the frozen distributed horizontal line load in every canonical case.

`D_b` and internal point-load horizontal forces were held unchanged, so this table isolates only the distributed segment field.

## 3. Effect on solved Q0

| Scenario | Original Q0, N | Shape-normal-field Q0, N | Reduction |
|---|---:|---:|---:|
| A | 855.3166 | 766.2642 | 10.41% |
| B | 2518.5086 | 2157.7982 | 14.32% |
| C | 3196.8937 | 2849.3544 | 10.87% |
| D | 7423.5335 | 6503.7830 | 12.39% |
| E | 4001.3380 | 3288.6107 | 17.81% |

All alternate cases remain `SolvedByBoundedBisection`.

Thus changing only the distributed horizontal force field also changes the vertical boundary reaction required to satisfy the same depth closure. The two quantities cannot be interpreted as independent corrections in this bounded geometry problem.

## 4. Effect on boundary X

| Scenario | Original boundary X, m | Shape-normal-field boundary X, m | Change, m | Change |
|---|---:|---:|---:|---:|
| A | 21.6460 | 21.8187 | +0.1726 | +0.80% |
| B | 57.8485 | 58.3597 | +0.5112 | +0.88% |
| C | 139.1318 | 139.6609 | +0.5290 | +0.38% |
| D | 150.7430 | 151.0874 | +0.3444 | +0.23% |
| E | 149.5170 | 150.2361 | +0.7191 | +0.48% |

Despite the sizeable reduction in distributed horizontal line force, the solved horizontal excursion increases slightly in every case because the simultaneously solved top vertical reaction `Q0` is also reduced.

This is a useful non-intuitive result: lowering the frozen horizontal drag does not, by itself, imply a smaller depth-closing horizontal excursion when the surface vertical reaction is re-solved at the same time.

## 5. Remaining difference to current selected X

| Scenario | Shape-normal boundary X, m | Current selected X, m | Shape-normal boundary - selected, m |
|---|---:|---:|---:|
| A | 21.8187 | 21.1518 | +0.6668 |
| B | 58.3597 | 58.2692 | +0.0905 |
| C | 139.6609 | 143.9061 | -4.2452 |
| D | 151.0874 | 138.6337 | +12.4537 |
| E | 150.2361 | 130.0129 | +20.2232 |

Only B becomes nearly coincident under this controlled substitution. A becomes slightly farther from the selected result, C remains materially below selected X, and D/E remain strongly above selected X.

For D/E the difference is slightly larger than with the original frozen segment field.

## 6. Engineering interpretation

This experiment rules out a simple explanation in which the D/E selected-vs-boundary delta is primarily caused by the iterative path using a smaller orientation-dependent horizontal line force.

The orientation-dependent force reduction is real and sizeable, but inserting that reduced field into the same bounded `Q0` closure changes boundary X by less than about 0.9% in these canonical cases. It does not reproduce the current selected shape.

The remaining model difference therefore lies substantially in the different vertical-reaction/tension/geometry closure contracts, and potentially in how those contracts interact with discrete-load geometry and iterative feedback.

The alternate force field in this experiment is specifically the `ShapeForceN` field from the current fallback-shape projection. It is **not** a fully coupled or final-iteration force field, and this control mark does not claim otherwise.

## 7. Production consequence

No evidence here justifies switching selected X/Z to the surface-boundary diagnostic or feeding this validation field into production.

No change is authorized to:

- `BuoyCalculator`;
- `MooringShapeSolver`;
- `MooringShapeForceAnalyzer`;
- `MooringIterativeSolver`;
- `MooringPrimaryShapeGate`;
- selected X/Z;
- 2D/PDF geometry;
- anchor/weak-link calculations;
- verdict;
- signed submerged-weight semantics;
- 0.20 m segmentation;
- engineering golden baseline.

## 8. Next safe isolation

The next validation question should isolate the **vertical boundary/tension contract** rather than applying another horizontal-force substitution.

A safe experiment should compare, on the same canonical ownership field:

1. the current bounded solved `Q0`;
2. the vertical component implied at the top by the current selected/iterative tension state where that quantity can be reconstructed without changing production;
3. resulting tangent/geometry differences under frozen loads;
4. discrete-load contributions kept identical.

The purpose is diagnostic attribution only. Any new physical top-boundary condition or coupled solver remains a separate Physics RFC decision under #407/#413.
