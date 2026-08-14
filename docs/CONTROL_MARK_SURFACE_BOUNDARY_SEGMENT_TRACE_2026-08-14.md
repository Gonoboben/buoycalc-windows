# Control mark: boundary-conditioned per-segment trace

Date: 2026-08-14
Issue: #413
Depends on: #407
Scope: validation-only per-segment comparison of the solved surface-boundary H/V/tangent field against the current final discrete-tension/raw-used-angle field. No production authority change.

## 1. Purpose

The top-vector comparison established a structural difference between the solved free-surface buoy boundary vector `(D_b,Q0)` and the current internal cumulative top H/V field used by the promoted iterative candidate.

This control mark traces that difference along every production segment on the canonical A–E scenarios.

For the diagnostic trace only:

- start from solved `H=D_b`, `V=Q0`;
- cross the same connector/payload point loads exactly once at the existing sequence positions;
- use the same production segment `CurrentForceN` and signed `WeightWaterKg`;
- evaluate boundary-conditioned H/V at each segment midpoint;
- compute tangent angle from that signed boundary-conditioned vector;
- compare against `FinalDiscreteLoadTensions` cumulative H/V and raw `DiscreteAngleFromVerticalDeg`;
- compare against the final `UsedAngleFromVerticalDeg` after `MooringDiscreteLoadShapeBuilder` angle scaling.

No result from this trace is a selected-shape consumer.

The values below come from GitHub Actions `.NET Build` run #896 for PR #467 on exact head `b5a5e00bce13d55d3ceaa14b5762f12da66025bf`.

## 2. Aggregate angle differences

| Scenario | Segments | Mean |boundary - raw angle|, ° | Max, ° | Max raw-delta depth, m | Mean |boundary - used angle|, ° | Max, ° | Max used-delta depth, m |
|---|---:|---:|---:|---:|---:|---:|---:|
| A | 275 | 20.3460 | 42.7361 | 0.2727 | 14.6982 | 32.6653 | 49.9091 |
| B | 675 | 28.4683 | 57.4688 | 0.2667 | 15.3392 | 36.1059 | 119.9111 |
| C | 2050 | 29.7266 | 58.0091 | 0.2780 | 14.3370 | 34.0739 | 379.9073 |
| D | 2050 | 18.2638 | 59.6619 | 0.2780 | 11.9855 | 30.5540 | 0.2780 |
| E | 2050 | 16.4507 | 47.0447 | 0.2780 | 14.8482 | 36.4117 | 0.2780 |

The differences are tens of degrees, not numerical noise around the existing geometry tolerances.

For all five cases the largest raw-angle discrepancy is at or immediately below the top of the line. After the production global angle scale, the location of the largest remaining angle difference depends on the scenario: near the bottom for A/B/C and near the top for D/E.

## 3. H/V field differences

| Scenario | Max |boundary H - production H|, N | Depth, m | Max |boundary V - production V|, N | Depth, m |
|---|---:|---:|---:|---:|
| A | 535.5584 | 49.9091 | 456.4507 | 0.2727 |
| B | 1933.5600 | 119.9111 | 1698.4471 | 0.2667 |
| C | 1984.8141 | 379.9073 | 2161.0859 | 0.2780 |
| D | 3245.6978 | 379.9073 | 6387.7257 | 379.9073 |
| E | 1754.8174 | 379.9073 | 2965.5672 | 0.2780 |

The large D vertical difference is not confined to the surface. It remains substantial to the bottom and reaches its maximum near the anchor-side end.

## 4. Direction of the two traces

A common pattern is visible in the five representative samples from each scenario.

The boundary-conditioned trace starts with a nearly vertical surface vector and, as current load is accumulated downward, its horizontal component grows. The corresponding boundary tangent angle therefore generally increases with distance down the line.

The current final discrete-tension field is assembled from loads on the opposite side of the section: cumulative H/V are largest toward the upper segment and decrease toward the bottom. Its raw angle therefore generally decreases downward. The subsequent global `AngleScale` changes the magnitude of the angle but does not change that underlying load-side construction.

This creates two geometry-driving fields with opposite along-line trends.

### Scenario A representative angles

Boundary angle: approximately `9.27° -> 16.63° -> 23.75° -> 30.34° -> 36.22°`.

Production raw angle: approximately `49.12° -> 45.55° -> 36.07° -> 22.68° -> 5.20°`.

Production used angle: approximately `33.58° -> 31.14° -> 24.66° -> 15.50° -> 3.55°`.

### Scenario B representative angles

Boundary angle: `8.53° -> 17.76° -> 26.35° -> 33.99° -> 40.56°`.

Production raw angle: `64.96° -> 60.72° -> 51.90° -> 36.08° -> 8.11°`.

Production used angle: `35.70° -> 33.37° -> 28.52° -> 19.83° -> 4.46°`.

### Scenario C representative angles

Boundary angle: `3.71° -> 12.08° -> 20.37° -> 28.25° -> 35.48°`.

Production raw angle: `60.75° -> 56.80° -> 48.52° -> 32.96° -> 2.94°`.

Production used angle: `29.03° -> 27.14° -> 23.18° -> 15.75° -> 1.40°`.

### Scenario D representative angles

Boundary angle: `8.59° -> 19.41° -> 23.30° -> 25.08° -> 25.90°`.

Production raw angle: `67.51° -> 48.71° -> 31.08° -> 19.75° -> 15.24°`.

Production used angle: `38.73° -> 27.95° -> 17.83° -> 11.33° -> 8.74°`.

### Scenario E representative angles

Boundary angle: `7.57° -> 18.35° -> 23.29° -> 25.72° -> 26.85°`.

Production raw angle: `53.58° -> 35.08° -> 19.37° -> 10.28° -> 7.33°`.

Production used angle: `43.15° -> 28.25° -> 15.60° -> 8.28° -> 5.90°`.

## 5. D/E profile-current interpretation

D and E are especially informative because their boundary-conditioned V remains large over the full line while the current production internal cumulative V remains much smaller.

For D, representative boundary V decreases only from approximately `7381 N` near the surface to `7060 N` near the bottom, while production V decreases from approximately `1036 N` to `672 N`.

For E, representative boundary V decreases from approximately `3959 N` to `3638 N`, while production V decreases from approximately `1036 N` to `672 N`.

At the same time boundary H grows downward whereas production cumulative H falls downward. The fields therefore cross in angle somewhere along the line rather than remaining separated by a near-constant correction.

This is consistent with the previously measured D/E selected-X differences and confirms that the mismatch is distributed along the line, not a local surface-node defect.

## 6. Cut-side semantics

This trace comparison must not be misread as a direct force residual between two quantities that were intended to represent the same side of a cut.

The boundary-conditioned field propagates the solved surface reaction downward by accumulating loads already crossed from the buoy side.

The current `MooringDiscreteLoadTensionAnalyzer` cumulative field is assembled bottom-to-top from line and internal discrete loads on the opposite side of the section and does not include the solved surface reaction or an explicit anchor-side boundary reaction.

The large H/V and angle differences therefore expose a **boundary/reference-side contract difference** in the present candidate-geometry construction.

They do not by themselves establish which complete future boundary-value formulation should become authoritative.

## 7. Production consequence

No production change is authorized by this trace.

In particular, do not:

- replace `FinalDiscreteLoadTensions` with the diagnostic trace;
- feed diagnostic midpoint angles into `MooringDiscreteLoadShapeBuilder`;
- remove or retune `AngleScale` as part of this package;
- change `MooringIterativeSolver` feedback;
- change `MooringPrimaryShapeGate`;
- change selected X/Z;
- change 2D/PDF geometry;
- change anchor/weak-link calculations or verdict;
- change signed submerged-weight semantics;
- change the 0.20 m segmentation target or add a segment cap;
- change the engineering golden baseline.

## 8. Next safe boundary

The next useful validation step is to close the global reaction accounting explicitly.

Using the already solved surface reaction and the same signed internal loads, validation should expose the implied anchor-side cable reaction and verify whole-line force balance. In parallel, it should state exactly which boundary reaction is absent from the current bottom-to-top candidate tension accumulation.

That step would distinguish a simple reference-side transformation from a genuinely different boundary condition before any boundary-conditioned runtime tension read model or coupled production solver is proposed.

Any production authority change remains a separate Physics RFC decision under #407/#413.
