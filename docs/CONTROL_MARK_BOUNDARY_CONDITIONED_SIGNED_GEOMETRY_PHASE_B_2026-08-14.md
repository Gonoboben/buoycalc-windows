# Control mark: boundary-conditioned signed geometry Phase B

Date: 2026-08-14
Issue: #407
Depends on: completed #413
Scope: validation-only restart of signed planar geometry after the surface-boundary reaction was solved and exposed as a passive trace. No production selected-X/Z change.

## 1. Why Phase B can resume now

The original signed-geometry study under #407 correctly demonstrated that absolute-value angle handling loses quadrant information, but it also demonstrated that the old `SegmentTensionRows` signed H/V ledger was not a valid anchored-cable direction field because it lacked solved boundary reactions.

That blocker has now been resolved separately under #413.

The merged boundary path now provides:

- solved free-surface buoy reaction `(D_b,Q0)`;
- exact existing distributed/point-load ownership;
- signed submerged weights;
- one shared frozen-load integration kernel;
- passive `MooringSurfaceBoundaryTensionTraceResult` with per-segment signed H/V and tangent components;
- terminal reaction accounting;
- no selected-shape authority.

Therefore future Phase-B signed geometry validation must use the **boundary-conditioned trace**, not the old raw `SegmentTensionRows` orientation ledger.

## 2. Validation geometry rule

For each available trace row with finite tangent:

`dx = ds * TangentX`

`dz = ds * TangentZ`

with project convention:

- local `+X` points buoy -> anchor in the approved 2D plane;
- project `+Z` is downward;
- `TangentX = H / |T|`;
- `TangentZ = V / |T|`;
- signed H/V and tangent components are authoritative for this validation;
- scalar angle is display/comparison only.

No `Abs(H)`, `Abs(V)`, absolute angle or `0..89°` clamp may be introduced in the validation geometry reconstruction.

## 3. First required identity gate

Before any force/shape feedback experiment, validation must reconstruct geometry from the stored trace and prove that it reproduces the parent frozen-load surface-boundary integration.

For canonical A–E, require:

- `sum(dx)` equals `SurfaceBoundaryInfo.SolutionState.EndpointXM` to numerical tolerance;
- `sum(dz)` equals `SurfaceBoundaryInfo.SolutionState.EndpointZM` to numerical tolerance;
- row count equals production segment count;
- point-load crossing ownership remains unchanged;
- signed tangent normalization is preserved;
- terminal H/V remains unchanged;
- the current authoritative `SelectedShape` is not used as an input to this reconstruction.

This identity is intentionally a consistency check of one physics owner, not an independent physical validation claim.

## 4. Signed/quadrant validation

The Phase-B regression must retain explicit signed-orientation coverage beyond the normal A–E cases.

At minimum it must verify controlled states in which:

- downward `V > 0` gives `TangentZ > 0`;
- upward `V < 0` gives `TangentZ < 0`;
- zero resultant is indeterminate rather than divided by zero;
- signed buoyant distributed or point loads preserve their sign through the boundary-conditioned propagation;
- discrete point loads are crossed exactly once.

A `dz < 0` example may be used only when produced by a mechanically defined boundary/load case. Validation must not manufacture it by flipping a tangent or post-processing an otherwise downward solution.

If no solved normal free-surface case produces a local `V < 0`, that fact should be recorded rather than forcing an artificial acceptance example into the free-surface solver.

## 5. Comparison to historical selected X/Z

The validation may report differences against the current authoritative selected geometry, but it must not use those differences as pass/fail criteria.

The already merged #407/#413 evidence shows the boundary-conditioned frozen-load X is not numerically interchangeable with current `MooringIterativeSolver.FinalShape`, especially in profile-current D/E.

Therefore:

- selected X/Z remains measurement-only in this Phase-B package;
- no tolerance for `boundary X - selected X` is introduced;
- no golden baseline is rewritten;
- no candidate is promoted to `MooringPrimaryShapeGate`.

## 6. Feedback coupling remains a later package

Only after the geometry identity/signed-quadrant gate is green may validation introduce a feedback-coupling experiment.

That later experiment must use boundary-conditioned tangents as the orientation state and must explicitly state which hydrodynamic force field is updated at each iteration.

Iteration-budget evidence remains validation-only and should use the RFC sequence:

`4 -> 8 -> 16 -> 32 -> 64`

with convergence history recorded before any production `MaxIterations` discussion.

A large iteration count is not a substitute for a correct boundary condition.

## 7. Relationship to profile-current projection

For profile-current mode, the approved signed East/North/W -> planar X/Z projection boundary remains separate from the cable-tangent boundary.

The frozen surface-boundary trace currently uses the existing calculated segment force ownership. It does not by itself authorize switching profile-current segment forces to the newer signed planar projection.

A future coupled experiment that combines signed profile vectors with boundary-conditioned tangents must make that force-input change explicit and isolated.

## 8. Non-consumers / no production change

This Phase-B restart does not authorize changes to:

- `BuoyCalculator`;
- `MooringShapeSolver`;
- `MooringShapeForceAnalyzer` production behavior;
- `MooringShapeTensionAnalyzer`;
- `MooringDiscreteLoadTensionAnalyzer`;
- `MooringDiscreteLoadShapeBuilder`;
- `MooringIterativeSolver`;
- `MooringPrimaryShapeGate` / selector;
- selected X/Z;
- 2D/PDF geometry;
- anchor/weak-link calculations;
- verdict;
- production MaxIterations;
- signed `WeightWaterKg`;
- production 0.20 m segmentation target or unlimited segment count;
- engineering golden baseline;
- 3D.

## 9. Next allowed code package

Validation-only `BoundaryConditionedSignedGeometryRegression` (or equivalent):

1. reconstruct `dx/dz` from `MooringSurfaceBoundaryTensionTraceResult` tangents;
2. prove exact/numerical identity with the parent frozen-load endpoint across A–E;
3. verify signed tangent/quadrant and indeterminate-state semantics using controlled mechanical cases;
4. verify discrete ownership is unchanged;
5. log historical selected X/Z only for comparison;
6. change no production file.

Only after this package is merged may #407 proceed to a boundary-conditioned feedback-coupling measurement.
