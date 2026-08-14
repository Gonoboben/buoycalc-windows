# Control mark: surface-boundary vs iterative-path decomposition

Date: 2026-08-14
Issue: #413
Depends on: #407
Scope: validation-only decomposition of the canonical selected-X difference. No production solver or selected-X/Z change.

## 1. Purpose

The selected-vs-boundary impact comparison showed that the frozen-load surface-boundary diagnostic is not numerically interchangeable with the current authoritative selected shape, especially in canonical current-profile cases D/E.

This control mark separates three existing geometry states without promoting any of them:

1. fallback X from `MooringShapeSolver`;
2. current iterative selected X from `MooringIterativeSolver.FinalShape`;
3. frozen-load boundary X from `MooringSurfaceBoundaryInfoAnalyzer`.

The measurements below come from GitHub Actions `.NET Build` run #884 for PR #461 on exact head `c1737a62f3cf54780e49425c08822682a61cfca1`.

## 2. X-path measurements

| Scenario | Fallback X, m | Selected X, m | Boundary X, m | Selected - Fallback, m | Boundary - Fallback, m | Boundary - Selected, m |
|---|---:|---:|---:|---:|---:|---:|
| A | 22.9107 | 21.1518 | 21.6460 | -1.7588 | -1.2647 | +0.4942 |
| B | 61.8309 | 58.2692 | 57.8485 | -3.5617 | -3.9824 | -0.4207 |
| C | 153.9689 | 143.9061 | 139.1318 | -10.0628 | -14.8370 | -4.7742 |
| D | 149.1964 | 138.6337 | 150.7430 | -10.5627 | +1.5466 | +12.1093 |
| E | 143.9521 | 130.0129 | 149.5170 | -13.9392 | +5.5649 | +19.5041 |

All current authoritative iterative candidates converged in two recorded iterations. In each case the first iteration produced the material X change and the second recorded zero additional X change under the current convergence path.

## 3. Initial orientation-dependent line-force effect

Before the discrete iterative feedback is applied, `MooringShapeForceAnalyzer` already changes line current force relative to the original segment-force total because it projects velocity normal to the fallback shape.

| Scenario | Original line force, N | Shape line force, N | Shape/original |
|---|---:|---:|---:|
| A | 432.9600 | 357.8326 | 0.8265 |
| B | 1660.5000 | 1312.1763 | 0.7902 |
| C | 1815.4800 | 1559.4507 | 0.8590 |
| D | 2316.6830 | 1868.9830 | 0.8067 |
| E | 1316.3190 | 956.8898 | 0.7270 |

Thus the current iterative production candidate is not operating on the same frozen horizontal load field as the surface-boundary INFO model. Orientation-dependent force feedback is already material before the first selected-shape update.

In D/E the second iteration further changes the shape-line force to approximately `1738.39 N` and `901.30 N` respectively while the current selected X remains at the first-iteration value.

## 4. What this isolates

### A/B/C

For A/B/C, both the iterative selected path and the boundary-conditioned path move X below the fallback value, although by different amounts. The remaining boundary-vs-selected difference therefore cannot be described simply as "one path applies an offset in the opposite direction".

### D/E

D/E are qualitatively different:

- iterative selected X moves **below** fallback X;
- frozen-load boundary X moves **above** fallback X.

Therefore the large D/E boundary-vs-selected difference is not only the accumulated magnitude of the ordinary iterative correction. The two model contracts respond differently to the profile-current load distribution.

Scenario D is particularly important because its canonical current profile is East-only with `VerticalCurrentMS = 0` at every profile point. The opposite-direction X response in D therefore does not require non-zero vertical current or an East/North signed-axis ambiguity. Depth-varying load distribution plus the different geometry/boundary closure contracts is already sufficient to expose the divergence.

Scenario E adds changing East/North components, non-zero vertical current and density variation, so it combines additional signed-planar/profile effects on top of the more basic profile-distribution difference already visible in D.

## 5. Model-contract difference visible in code

The current fallback/iterative path and the boundary INFO path solve different problems:

- `MooringShapeSolver` derives segment angles from accumulated tension rows and applies a global angle scale to close depth;
- `MooringIterativeSolver` repeatedly computes orientation-dependent line force, shape tension and discrete-load shape, then gates the resulting candidate;
- `MooringSurfaceBoundaryInfoAnalyzer` solves a bounded physical top reaction `Q0` and integrates the existing frozen segment/point loads without iterative orientation-dependent load feedback.

The measured difference is therefore an expected engineering-model comparison until these assumptions are reconciled. It is not evidence that one existing result should automatically replace the other.

## 6. Production consequence

No direct promotion of `SurfaceBoundaryInfo.SolutionState` to selected X/Z is justified by this evidence.

No change is authorized to:

- `MooringShapeSolver`;
- `MooringIterativeSolver`;
- `MooringPrimaryShapeGate`;
- selected X/Z;
- 2D/PDF geometry;
- anchor/weak-link calculations;
- verdict;
- signed submerged weight;
- 0.20 m segmentation;
- golden baseline.

## 7. Next safe validation question

The next useful isolation is to distinguish **top-boundary reaction / signed vertical tension** from **orientation-dependent horizontal-force feedback** under controlled overlap cases.

A validation-only experiment should compare the same canonical load ownership with:

1. frozen original horizontal segment forces;
2. shape-normal horizontal segment forces frozen after one projection;
3. the same bounded `Q0` closure on both frozen fields;
4. point loads unchanged and crossed once.

This would measure how much of the X delta comes from the horizontal load-field change before attempting any coupled production solver.

No such experiment may become a selected-shape consumer without a later explicit Physics RFC decision.
