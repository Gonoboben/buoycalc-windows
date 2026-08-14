# Control mark: boundary-conditioned tension-trace read-model boundary

Date: 2026-08-14
Issue: #413
Depends on: #407
Scope: architecture / behavior-preserving runtime read-model boundary only. No production selected-X/Z authority change.

## 1. Purpose

Validation now establishes a consistent frozen-load boundary accounting path:

- the free-surface buoy reaction is solved as `(D_b,Q0)`;
- existing signed segment and internal discrete loads are crossed exactly once;
- the resulting per-segment H/V/tangent field differs structurally from the current internal cumulative candidate field;
- whole-line accounting closes to the existing steady `CalculationResult.CurrentForceN` horizontally and leaves an explicit non-zero anchor-side terminal cable component vertically.

The next safe runtime capability is a **passive boundary-conditioned tension trace** that makes this already validated field observable without becoming a geometry or verdict authority.

This control mark defines that boundary before code is moved from validation into the production assembly.

## 2. One physics owner, not two copied integrators

The validation regression currently reconstructs the boundary-conditioned trace independently for measurement. That duplication is acceptable inside a temporary validation experiment but must not become the long-term runtime architecture.

Production code shall have exactly one physics-owned frozen-load propagation implementation for:

- surface start `H=D_b`, `V=Q0`;
- point-load crossing order;
- segment distributed H/V increments;
- signed submerged-weight semantics;
- midpoint tangent evaluation;
- terminal H/V accounting.

`MooringSurfaceBoundaryInfoAnalyzer` and the future tension-trace read model must share that implementation or the existing analyzer must emit the trace from its own integration kernel.

UI, Markdown, PDF and 2D code shall not reproduce these equations.

## 3. Allowed authoritative inputs

The passive trace may consume only already authoritative/calculated runtime data:

- solved `MooringSurfaceBoundaryInfoResult` for `D_b`, `Q0`, classification and provenance;
- `CalculationResult.SegmentRows` for existing segment length, position, `CurrentForceN` and signed `WeightWaterKg`;
- `MooringSequencePositionResult` for existing internal connector/payload point-load ownership and `s` positions.

No drag coefficient, water-density or current-speed formula is recomputed by the trace.

`WaveForceN` remains excluded because the parent surface-boundary INFO contract is steady-current only.

## 4. Availability

The trace is available only when the parent surface-boundary state provides a solved bounded `Q0` and valid required load ownership.

If the parent state is unavailable or unsolved, the trace must return an explicit unavailable result that preserves the parent classification/provenance. It must not manufacture `Q0=0`, default buoy drag, or substitute a selected-shape tension field.

Analytical no-root classifications remain authoritative and must not be overridden by a trace builder.

## 5. Per-segment row contract

Each trace row should identify the existing production segment and its source position, at minimum:

- segment number;
- start/end/mid `s` along line;
- existing estimated/source depth metadata when available;
- number of internal point-load crossings applied before the segment midpoint under the frozen-load ordering;
- boundary-conditioned midpoint `H_N`;
- boundary-conditioned midpoint signed `V_N`;
- midpoint tension magnitude;
- signed tangent components `t_x`, `t_z` in project `+Z down` convention;
- a comparison-friendly angle representation whose sign convention is explicit.

Signed `V` must remain observable. A convenience absolute angle must never replace the signed H/V or tangent components as the authoritative trace data.

The trace may also expose start/end H/V if that improves diagnostics, but the side-of-discontinuity convention at a point load must be explicit.

## 6. Point-load crossing convention

Connector/payload point loads use the same positional ownership already validated for `SurfaceBoundaryInfo`.

The runtime trace must preserve the same deterministic crossing convention as the shared integration kernel. A point load is crossed exactly once; buoy and anchor remain boundary objects rather than internal point loads.

The implementation must not identify boundaries by localized display strings such as `"Буй"` or `"Якорь"`.

## 7. Terminal reaction contract

The trace result must expose the terminal anchor-side cable components produced by the same propagation pass.

Regression must preserve the validated identities:

`H_terminal = D_b + ΣF_x,internal = CalculationResult.CurrentForceN`

for the steady-current model, and

`V_terminal = Q0 - ΣW_internal,signed`.

The trace terminal H/V must agree with `MooringSurfaceBoundaryInfoResult.SolutionState.EndHN/EndVN` to numerical tolerance.

These values are cable-reaction diagnostics only. They are not automatically anchor holding inputs.

## 8. Provenance

The result must carry an explicit method/provenance note equivalent to:

- boundary-conditioned frozen-load tension trace;
- source surface reaction `(D_b,Q0)`;
- midpoint segment evaluation;
- signed submerged weights;
- existing sequence point-load ownership;
- steady-current, wave excluded;
- diagnostic only, not selected-shape authority.

Consumers must be able to distinguish this trace from:

- `SegmentTensionAnalyzer` rows;
- `MooringShapeTensionAnalyzer` rows;
- `MooringDiscreteLoadTensionAnalyzer` rows;
- final iterative tension rows.

## 9. Runtime placement

The intended passive runtime placement is:

`TechnicalReportDataBuilder`

1. build existing `SequencePositions`;
2. build existing `SurfaceBoundaryInfo`;
3. build boundary-conditioned tension trace from the shared surface-boundary integration owner;
4. continue building the existing discrete/iterative candidate path unchanged.

The trace can then be retained in `TechnicalReportData` / `CalculationSnapshot` as a diagnostic read model.

`CalculationSnapshotBuilder` must continue selecting X/Z only through:

`SelectedMooringShapeProvider.Build(data.Shape, data.IterativeSolver)`.

## 10. Explicit non-consumers

During this phase the new trace is forbidden as an input to:

- `MooringShapeSolver`;
- `MooringShapeForceAnalyzer`;
- `MooringShapeTensionAnalyzer`;
- `MooringDiscreteLoadTensionAnalyzer`;
- `MooringDiscreteLoadShapeBuilder`;
- `MooringIterativeSolver`;
- `MooringPrimaryShapeGate` / selector;
- `SelectedShapeReadModel` construction;
- 2D geometry;
- PDF diagram geometry;
- anchor holding / reserve;
- weak-link calculations;
- `CalculationResult.Verdict`.

A technical-report INFO section may consume the stored trace only in a later presentation-only package after data wiring is separately regression-verified.

## 11. Numerical invariants

The shared kernel / trace implementation must preserve:

- production segmentation target exactly `0.20 m`;
- no new segment-count cap;
- signed `WeightWaterKg` without `Abs`;
- existing `g` convention used by the surface-boundary model;
- exact existing point-load ownership/order;
- existing bounded `Q0` classifications and solver behavior;
- existing `SurfaceBoundaryInfo` numerical outputs;
- engineering golden baseline.

A refactor that changes existing `SurfaceBoundaryInfo` values is not behavior-preserving and must not be merged under this package.

## 12. Required regression before runtime wiring

Before adding the trace to `TechnicalReportData`, a behavior-preserving kernel package must prove at least:

1. all existing `SurfaceBoundaryInfoAnalyzerRegression` and canonical A–E measurements remain unchanged;
2. trace terminal H/V equals existing `SolutionState.EndHN/EndVN`;
3. trace midpoint samples reproduce the merged validation evidence;
4. point-load crossing count is unchanged;
5. signed buoyant-weight cases remain signed;
6. no selected-shape consumer map changes.

## 13. Package order

The safe implementation order is:

### Package A — shared integration-kernel boundary

Refactor only enough frozen-load propagation code so `SurfaceBoundaryInfo` and a future trace can share one implementation. No new runtime consumer and no report output.

### Package B — passive tension-trace read model

Create the trace result/rows and verify them against the merged validation measurements. Still no `TechnicalReportData` consumer if a smaller package is practical.

### Package C — snapshot/report-data wiring

Store the trace in `TechnicalReportData` after `SurfaceBoundaryInfo`. Verify `SelectedShape` is byte/numerically unchanged.

### Package D — optional INFO presentation

Only after the above, render selected diagnostics in technical report/UI if useful. No physics recomputation in presentation code.

## 14. Production authority remains blocked

This control mark does not authorize a boundary-conditioned production cable solver or a selected-X/Z switch.

A future authority change requires a separate Physics RFC decision under #407/#413 that reconciles:

- surface and anchor boundary reactions;
- signed per-segment tension propagation;
- orientation-dependent current-force coupling;
- discrete-load geometry;
- Berteaux assumptions/source overlap;
- impact on canonical selected X/Z and engineering golden baselines.

No 3D is introduced.
