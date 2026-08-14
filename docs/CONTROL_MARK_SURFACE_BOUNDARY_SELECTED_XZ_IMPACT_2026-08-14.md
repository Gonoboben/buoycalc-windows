# Control mark: selected X/Z vs surface-boundary diagnostic impact

Date: 2026-08-14
Issue: #413
Depends on: #407
Scope: validation-only comparison of the current authoritative selected X/Z against the frozen-load surface-boundary INFO diagnostic. No production authority change.

## 1. Purpose

Measure the impact that would exist if the frozen-load surface-boundary diagnostic geometry were ever considered near the current selected-shape path.

This is deliberately a comparison, not a candidate promotion. The current authoritative X/Z remains `CalculationSnapshot.SelectedShape`, built through `SelectedMooringShapeProvider` and `MooringPrimaryShapeSelector`.

The compared boundary geometry remains the existing INFO-only result from `MooringSurfaceBoundaryInfoAnalyzer`:

- frozen-load midpoint integration;
- steady current;
- wave excluded;
- `+Z` downward;
- bounded `Q0`;
- diagnostic X/Z not eligible for selected shape, 2D or PDF geometry.

The values below come from GitHub Actions `.NET Build` run #880 for PR #459 on exact head `2bd972e2d55ec8a80ee646e6b0cb9bebf611ba2e`.

## 2. Selected-shape authority observed in A-E

All five canonical scenarios used the same current production selection path:

- `SelectedSource = MooringIterativeSolver.FinalShape`;
- `UsesDiscreteLoads = true`;
- `HasGateSelection = true`;
- `GateDecision = CandidateReadyForPrimary`.

Therefore this comparison is not against the fallback `MooringShapeSolver` in these cases. It compares the frozen-load boundary diagnostic against the currently promoted iterative final shape.

## 3. Horizontal X impact

| Scenario | Selected X, m | Boundary X, m | Boundary - Selected, m | Boundary / Selected | Relative difference |
|---|---:|---:|---:|---:|---:|
| A | 21.1518 | 21.6460 | +0.4942 | 1.02336 | +2.34% |
| B | 58.2692 | 57.8485 | -0.4207 | 0.99278 | -0.72% |
| C | 143.9061 | 139.1318 | -4.7742 | 0.96682 | -3.32% |
| D | 138.6337 | 150.7430 | +12.1093 | 1.08735 | +8.73% |
| E | 130.0129 | 149.5170 | +19.5041 | 1.15002 | +15.00% |

The observed absolute horizontal difference spans approximately `0.42 ... 19.50 m` across these canonical cases.

The sign is not uniform: the frozen-load diagnostic can produce either a smaller or larger horizontal offset than the current selected shape.

No tolerance or acceptance threshold is inferred from this table.

## 4. Vertical Z impact

| Scenario | Selected anchor Z, m | Boundary Z, m | Boundary - Selected, m |
|---|---:|---:|---:|
| A | 50.0000 | 50.0003 | +0.00032 |
| B | 120.0000 | 120.0022 | +0.00216 |
| C | 380.0000 | 379.9917 | -0.00826 |
| D | 380.0000 | 379.9943 | -0.00569 |
| E | 380.0000 | 379.9971 | -0.00293 |

The selected-shape vertical residual reported by the current authoritative path is `0` for all A-E cases. The frozen-load boundary diagnostic also closes the target depth within its existing approximately `0.01 m` integration tolerance.

Thus the material model difference visible in this comparison is primarily horizontal X, not endpoint-depth closure.

## 5. Interpretation

These measurements establish that the frozen-load boundary diagnostic is **not numerically interchangeable** with the current authoritative selected-shape path on canonical A-E.

The difference is especially visible in profile-current scenarios D and E, where the measured horizontal offsets differ by approximately `12.1 m` and `19.5 m` respectively. This is an observation only. It does not prove that either geometry is more physically correct.

The two paths have different model contracts. The surface-boundary read model intentionally freezes already calculated distributed/discrete loads and solves a bounded surface vertical reaction. The current selected path remains the existing iterative/gated production shape path. A difference between them must therefore be treated as an engineering-model difference, not automatically as an error in either result.

## 6. Consequence for production authority

This comparison does **not** support a direct switch of selected X/Z to `SurfaceBoundaryInfo.SolutionState`.

A direct switch would change canonical horizontal geometry by non-negligible amounts, including double-digit metres in D/E, while bypassing the current selected-shape gate contract.

Therefore the following remain unchanged:

- `SelectedMooringShapeProvider`;
- `MooringPrimaryShapeSelector`;
- `MooringPrimaryShapeGate`;
- solver feedback;
- selected X/Z;
- 2D geometry;
- PDF diagram geometry;
- anchor and weak-link calculations;
- `CalculationResult.Verdict`;
- engineering golden baseline.

## 7. Next safe question

Before production authority can be reconsidered, #407/#413 need an engineering explanation for the horizontal model delta, especially for current-profile cases D/E.

The next safe work is validation/source analysis of the causes of that delta, including at least:

1. frozen-load versus iterative load/orientation coupling;
2. treatment of current-profile direction and the existing signed-planar boundary;
3. discrete-load geometry participation;
4. top-boundary reaction semantics;
5. Berteaux source overlap and assumptions.

That work must remain diagnostic/validation-only until a separate Physics RFC explicitly authorizes any production solver or selected-X/Z change.
