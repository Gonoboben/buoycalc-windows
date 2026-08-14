# Control mark: boundary-conditioned feedback experiment

Date: 2026-08-14
Issue: #407
Prerequisites: completed #413; merged #474, #475, #476
Scope: define the next validation-only fixed-point experiment before implementation. No production solver change.

## 1. Purpose

Phase-B signed geometry is now able to reconstruct the solved frozen-load surface-boundary geometry directly from signed boundary-conditioned tangents without quadrant loss.

The next question is deliberately narrower:

> If the distributed line drag is recomputed from that signed candidate geometry, and the surface boundary reaction is then re-solved for the updated force field, does the resulting boundary-conditioned geometry converge under repeated feedback?

This package defines how that question is measured. It does not authorize a production geometry switch.

## 2. Force field selected for the first experiment

The first feedback experiment must use the **existing `MooringShapeForceAnalyzer` force contract unchanged**.

That choice is intentional:

- it already computes orientation-dependent normal drag from segment geometry;
- it is already part of the current production diagnostic/candidate path;
- using it isolates boundary-conditioned feedback from a second simultaneous change in current-vector projection;
- the newer signed East/North/W -> planar X/Z profile-current projection remains a separate boundary and is not introduced here.

Therefore the experiment must preserve the exact current-vector convention already implemented by `MooringShapeForceAnalyzer` for this package. No change to drag coefficient, projected area, water density or segment current sampling is authorized.

## 3. Fixed-point map

For each scenario, start from the ordinary application calculation and its solved surface-boundary state.

### State 0

Use:

- the original `CalculationResult` segment mesh and signed `WeightWaterKg`;
- the existing `MooringSequencePositionResult`;
- the solved `MooringSurfaceBoundaryInfoResult`;
- the stored/rebuilt `MooringSurfaceBoundaryTensionTraceResult`.

Reconstruct the validation geometry only from trace tangents:

`dx_i = ds_i * TangentX_i`

`dz_i = ds_i * TangentZ_i`

No `Abs(H)`, `Abs(V)`, unsigned scalar-angle geometry or `0..89°` clamp may be used.

### Iteration k -> k+1

1. Build a validation projection from the current boundary-conditioned geometry on the unchanged segment mesh.
2. Call existing `MooringShapeForceAnalyzer.Build(...)` with that projection.
3. Replace only each segment's experimental `CurrentForceN` by the corresponding `ShapeForceN` for the next validation state.
4. Keep buoy steady drag and discrete connector/payload loads as their existing model values; they are not shape-normalized in this experiment.
5. Recompute the experimental total steady-current force consistently as:

   `buoy steady drag + updated distributed line drag + existing discrete current loads`.

6. Preserve wave force only as the unchanged historical report/result field. The surface-boundary solver remains steady-current only and does not add wave to the boundary solve.
7. Re-run `MooringSurfaceBoundaryInfoAnalyzer.Build(...)` against the updated experimental segment force field.
8. If and only if the boundary result is solved with finite `Q0`, build the new boundary-conditioned tension trace.
9. Reconstruct the next signed validation geometry from the new trace tangents.
10. Record the iteration measurements before continuing.

Crucially, `Q0` is re-solved after every distributed force-field update. An old `Q0` may not be carried into a changed hydrodynamic state.

## 4. Discrete-load ownership

Do not route this experiment through `MooringDiscreteLoadTensionAnalyzer` or `MooringDiscreteLoadShapeBuilder`.

The solved surface-boundary integration already owns connector/payload point-load crossings through the sequence-position model. Adding the historical discrete-load path on top would double-count those loads.

For every iteration require/record:

- point-load crossing count;
- cumulative crossing monotonicity along the trace;
- terminal H/V;
- point-load jump closure at internal discrete positions where available.

Each discrete load must be crossed exactly once under the existing sequence ownership convention.

## 5. Relaxation policy

The first experiment uses raw fixed-point feedback:

`alpha = 1.0`

No under-relaxation, damping, averaging or clipping is allowed in this initial measurement package.

Reason: the purpose is first to classify the natural map. If the raw map oscillates or diverges, that is evidence to record, not something to hide by adding an unstated stabilizer.

Any later experiment with `0 < alpha < 1` requires a separate control mark that states the update equation and compares it against the raw `alpha = 1` trajectory.

## 6. Iteration-budget study

Validation-only budgets:

`4 -> 8 -> 16 -> 32 -> 64`

Production `MooringIterativeSolver.MaxIterations` remains unchanged.

Each budget run starts from the same State 0. The harness may stop a run early only for a mechanically explicit terminal condition such as:

- boundary solve becomes unavailable/unsolved;
- non-finite force/geometry state;
- missing segment mapping;
- indeterminate trace tangent prevents reconstruction.

A run must not be labelled failed merely because it has not converged by a given budget. Lack of convergence is a measurement result.

## 7. Measurements per iteration

Record at least:

- iteration number;
- boundary classification;
- `Q0`;
- endpoint X and Z;
- endpoint `DeltaX` and `DeltaZ` versus previous iteration;
- total distributed line force;
- change in total distributed line force;
- max per-segment force change;
- representative signed H/V and tangent X/Z cuts;
- terminal H/V;
- point-load crossing count;
- max node displacement between consecutive boundary-conditioned geometries on the same mesh;
- target-depth residual `EndpointZ - TargetDepth`;
- count of local `dz < 0` rows;
- boundary-conditioned point-load jump residual where applicable;
- stop reason / terminal classification.

## 8. Candidate-B measurement boundary

The current `MooringSignedNodeEquilibriumAnalyzer` is tied to the historical discrete-tension/discrete-shape candidate family. It is not the state owner for the new boundary-conditioned loop.

Therefore:

- do not feed Candidate B into the fixed-point map;
- log the existing snapshot Candidate-B max residual / relative residual once per scenario as comparison-only historical evidence where available;
- separately compute/record boundary-conditioned point-load jump closure from the trace for each iteration;
- do not invent a new production Candidate-B equation in this package.

This keeps the RFC measurement requirement while avoiding model mixing.

## 9. Convergence classification

This experiment does not set production acceptance thresholds.

For measurement, report the trajectory of:

- `abs(DeltaX)`;
- `abs(DeltaZ)`;
- max node displacement;
- `abs(DeltaQ0)`;
- max segment-force change;
- target-depth residual.

A validation summary may call a run `numerically settled` only when these quantities become small and remain small over consecutive iterations; this wording is diagnostic and must not be reused as `MooringPrimaryShapeGate` authority.

The first implementation package should avoid hard-coding a new engineering threshold merely to force a green result. Its regression assertions should instead protect invariants: finite arithmetic, one-to-one segment mapping, solved-state contracts, signed tangent normalization and discrete-load ownership.

## 10. Scenario set

Required measurements:

1. canonical A;
2. canonical B;
3. canonical C;
4. canonical D profile-current case;
5. canonical E profile-current/vector case;
6. one controlled buoyant-line scenario with negative signed `WeightWaterKgM`, reusing an already established deterministic validation input where practical.

If the controlled buoyant scenario cannot obtain a solved bounded free-surface state under the current boundary model, record the exact classification and stop that scenario. Do not alter loads or boundary conditions merely to manufacture a solvable/upward case.

## 11. Expected implementation location

The experiment belongs entirely under:

`validation/BuoyCalc.EngineeringRegression/`

Preferred package shape:

- `BoundaryConditionedFeedbackCouplingRegression.cs` or equivalent validation harness;
- one `ValidationEntryPoint` call;
- no production file change.

Small validation-only helper records/functions may live in that file. Do not add a reusable production service until the experiment establishes that the algorithm is physically and numerically justified.

## 12. Non-consumers / unchanged production authority

This experiment must not change:

- `BuoyCalculator`;
- `MooringShapeSolver`;
- production `MooringShapeForceAnalyzer` behavior;
- `MooringShapeTensionAnalyzer`;
- `MooringDiscreteLoadTensionAnalyzer`;
- `MooringDiscreteLoadShapeBuilder`;
- `MooringIterativeSolver`;
- `MooringPrimaryShapeGate` / selector;
- selected X/Z;
- 2D/PDF geometry;
- report physics;
- anchor/weak-link calculations;
- verdict;
- production iteration limit;
- signed `WeightWaterKg`;
- 0.20 m production segmentation target or segment-count policy;
- profile-current production force projection;
- JSON/DTO;
- engineering golden baseline;
- 3D.

## 13. Decision after the experiment

Only the measured trajectory may determine the next RFC step.

Possible outcomes include:

- raw boundary-conditioned feedback settles cleanly;
- it converges slowly and needs a separately justified relaxation study;
- it oscillates/diverges;
- some scenarios lose a solved surface-boundary state;
- profile-current cases expose a separate force-projection limitation.

None of those outcomes by itself authorizes a production solver switch. A later production proposal still needs the full #407 acceptance set: source-backed signed orientation, synthetic quadrants, analytical limits, convergence evidence, independent/reference comparison and explicit review of every historical golden change.
