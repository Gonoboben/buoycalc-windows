# Control mark: boundary-conditioned feedback measurement evidence

Date: 2026-08-18
Issue: #407
Prerequisites: merged #477, #478, #479
Scope: record what the validation-only raw fixed-point experiment has established, and what remains unproven before any production solver/shape change.

## 1. Status

The boundary-conditioned feedback experiment defined in `CONTROL_MARK_BOUNDARY_CONDITIONED_FEEDBACK_EXPERIMENT_2026-08-14.md` is now implemented and executing in the engineering regression suite.

Implementation PR #478 was merged after exact-head success of all required repository gates. Follow-up PR #479 added validation-only log roll-up observability without changing the feedback algorithm or any production calculation.

Current merged checkpoints:

- #478 exact tested head: `9deedaa06ab92fa562277e0a9c6b5fa6d1a097ba`;
- #478 `.NET Build` run #921: success;
- #478 Selected Shape Consumer Scan #378: success;
- #478 Report Store Consumer Scan #381: success;
- #479 exact tested head: `fef0aa9855e8fb2835d6059cb1f0fca0dad2386d`;
- #479 `.NET Build` run #923: success;
- #479 Selected Shape Consumer Scan #379: success;
- #479 Report Store Consumer Scan #382: success;
- main after #479 squash merge: `758d77ad5588a19e042baf87b330a48d80ba7f6b`.

## 2. Experiment actually encoded

`BoundaryConditionedFeedbackCouplingRegression` runs the raw fixed-point map with:

- `alpha = 1.0` — no under-relaxation, damping, clipping or averaging;
- independent iteration budgets `4, 8, 16, 32, 64`;
- every budget restarted from the same initial application state;
- canonical surface-boundary measurement scenarios supplied by `SurfaceBoundaryCanonicalMeasurementRegression.BuildCanonicalScenarios()`;
- one deterministic controlled buoyant-line scenario with negative signed water weight;
- existing `MooringShapeForceAnalyzer` as the orientation-dependent distributed line-force update;
- a fresh `MooringSurfaceBoundaryInfoAnalyzer` solve after every force-field update;
- boundary-conditioned signed geometry reconstructed only from trace tangents;
- existing discrete connector/payload point loads owned only by the boundary integration path.

The controlled buoyant-line case is permitted to record an explicit unsolved initial-boundary classification rather than changing loads or constraints to manufacture a solved case. Canonical scenarios require an initially solved boundary state.

## 3. Invariants now regression-protected

A successful engineering regression run confirms that the executed path did not violate the assertions encoded by the harness, including:

- one-to-one segment/shape-force row mapping;
- finite, non-negative experimental `ShapeForceN` within the existing tolerance contract;
- solved boundary/available trace contracts whenever an iteration continues;
- trace row count matching the segment mesh;
- terminal trace H/V matching the solved boundary terminal H/V;
- point-load crossing count matching the boundary solution and the expected internal sequence points;
- monotone point-load crossing ownership along the trace;
- finite signed tangent components;
- tangent normalization `tx^2 + tz^2 = 1` within validation tolerance;
- finite signed geometry increments `dx = ds * tx`, `dz = ds * tz`;
- preservation of segment projected length within the validation geometry tolerance;
- unchanged node count between successive feedback geometries;
- finite node-displacement measurements;
- explicit boundary-conditioned point-load jump-closure measurement.

These are validation invariants. They are not a new production acceptance gate.

## 4. Measurements emitted by the harness

For each scenario the regression emits initial-state evidence and, for each budget, the resulting state including:

- iteration count and stop classification;
- endpoint X/Z;
- solved `Q0`;
- final `DeltaX`, `DeltaZ`, `DeltaQ0`;
- max node displacement;
- total distributed line force and its change;
- max per-segment force change;
- target-depth residual;
- local negative-`dz` count;
- point-load crossing count;
- max point-load jump residual.

The 64-iteration budget additionally emits the per-iteration trajectory. PR #479 repeats scenario/budget/terminal summary lines at the end of the validation output under `BOUNDARY_FEEDBACK_ROLLUP` so the numerical evidence has a stable compact log boundary.

## 5. What the green CI result establishes

The successful #478 and #479 `.NET Build` runs establish that the validation experiment executes under the repository regression suite without triggering the encoded invariant failures.

They also establish that adding the experiment and its roll-up did not trigger either selected-shape or report-store consumer scan.

This is useful evidence that the experiment is isolated from production selected-shape/report consumer authority.

## 6. What the green CI result does NOT establish

A green build does **not** prove that the raw `alpha = 1` fixed-point map converges.

Convergence was deliberately not made a pass/fail assertion. Therefore this control mark must not infer any of the following from CI success alone:

- that endpoint X/Z settles;
- that `Q0` settles;
- that force deltas decay monotonically;
- that the map is contractive;
- that profile-current scenarios behave the same as the other canonical cases;
- that relaxation is or is not required;
- that the boundary-conditioned geometry is ready to replace production selected X/Z.

A numerical classification requires inspection of the emitted `BOUNDARY_FEEDBACK_ROLLUP` / 64-iteration trajectory values. No damping/oscillation/convergence classification is recorded here without that numerical evidence.

## 7. Current decision boundary

Until the numerical trajectory is explicitly reviewed and recorded:

- do not introduce under-relaxation;
- do not change the production solver iteration law;
- do not switch selected X/Z to boundary-conditioned feedback geometry;
- do not change 2D/PDF/report geometry;
- do not change force coefficients, current projection, boundary equations, anchor/weak-link calculations or verdict;
- do not update engineering golden values to fit this experiment;
- do not mix #430 signed profile-current projection into #407.

The next allowed physics-validation package depends on the measured raw trajectory:

- if raw `alpha = 1` settles cleanly, document that evidence and proceed to the next independent/reference acceptance requirement;
- if it oscillates, diverges or settles too slowly, define a separate validation-only relaxation/reference experiment before implementation;
- if a scenario loses a solved boundary state, record that terminal classification rather than tuning the scenario to force success.

## 8. Production authority remains unchanged

Nothing in #478, #479 or this measurement record authorizes changes to:

- `BuoyCalculator`;
- `MooringShapeSolver`;
- production `MooringShapeForceAnalyzer` behavior;
- `MooringIterativeSolver`;
- `MooringPrimaryShapeGate` / selected shape;
- selected X/Z;
- 2D/PDF geometry;
- report physics;
- anchor/weak-link calculations;
- verdict;
- signed `WeightWaterKg` / `WeightWaterKgM` semantics;
- the 0.20 m production segmentation target;
- profile-current production projection;
- JSON/DTO;
- 3D.

The production behavior remains frozen pending the remaining #407 acceptance evidence.