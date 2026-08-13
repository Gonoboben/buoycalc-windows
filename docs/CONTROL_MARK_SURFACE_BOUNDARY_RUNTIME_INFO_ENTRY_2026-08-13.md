# Control mark: surface-boundary runtime INFO entry

Date: 2026-08-13
Issue: #413
Depends on: #407
Scope: architecture / behavior-preserving runtime-entry decision only. No production solver or numerical result change.

## Purpose

Define the next safe transition after validation of the normal free-surface-buoy vertical boundary reaction.

Validation has established the bounded frozen-load unknown `Q0` (downward buoy-side vertical cable-tension component), point/distributed load ownership, signed submerged-weight handling, analytical constant/piecewise references, 0.20 m mesh evidence, taut/no-root classifications, and Berteaux vector/constitutive boundaries where assumptions overlap.

This document does not authorize a production shape solver. It only defines how a future passive INFO read model may enter the runtime path without becoming authoritative for selected X/Z.

## Current runtime boundary

`ApplicationCalculationRunner.Run(...)` owns typed `EnvironmentInput` and `BuoyInput`, calls `BuoyCalculator.Calculate(...)`, then currently calls `CalculationSnapshotBuilder.Build(environment, result)`.

`CalculationSnapshotBuilder` builds `TechnicalReportData` before deriving `SelectedShape` from the existing shape/iterative-solver data.

A passive diagnostic can therefore live in `TechnicalReportData` without becoming a selected-shape consumer, provided the existing selected-shape source remains unchanged.

## Typed buoy input must be preserved

Surface capacity needs buoy dry mass `W_b`. `BuoyInput.WeightKg` is available in `ApplicationCalculationRunner`, but typed `BuoyInput` is currently dropped before the snapshot/report-data boundary.

A future runtime surface-boundary INFO analyzer shall receive typed `BuoyInput` through behavior-preserving plumbing. It shall not infer buoy identity or dry mass from localized presentation strings such as `ElementRow.Kind == "Буй"`.

The existing `BuoyCalculator` signature and calculations remain authoritative and unchanged.

## Allowed frozen-load inputs

A future INFO analyzer may consume only already-calculated/current authoritative data:

- `EnvironmentInput.DepthM` for target depth;
- `BuoyInput.WeightKg` for buoy dry mass;
- `CalculationResult.BuoyancyKg` as existing full-volume capacity in kg-equivalent units;
- `CalculationResult.CurrentForceN` and `SegmentRows`;
- `MooringSequencePositionResult` for connector/payload positional ownership.

The steady buoy drag contribution shall use the regression-backed ownership identity:

`D_b = CurrentForceN - sum(SegmentRows.CurrentForceN) - SequencePositions.DiscreteCurrentForceN`.

It shall not be recomputed from a duplicate drag formula. Connector/payload point loads are taken exactly once from the sequence-position boundary. Buoy and anchor remain boundaries. `WaveForceN` remains excluded from this Chapter-2 steady-current diagnostic.

## Capacity semantics

`B_max = CalculationResult.BuoyancyKg * g`

`W_b = BuoyInput.WeightKg * g`

`Q_capacity = max(0, B_max - W_b)`.

For a solved frozen-load diagnostic state, `B_actual = W_b + Q0`.

`B_max` remains a capacity, not automatically the actual equilibrium displacement of a free surface buoy. Existing net-buoyancy, anchor and weak-link checks are not redefined by this INFO model.

## Frozen-load integration contract

The passive analyzer may reproduce only the validated frozen-load construction:

- local `+X_shape` points buoy -> anchor;
- `+Z` is downward;
- `H(0+) = D_b` and `V(0+) = Q0`;
- each distributed segment contributes existing `CurrentForceN` and signed `WeightWaterKg` exactly once;
- connector/payload point loads are crossed exactly once at existing sequence coordinate;
- no `Abs(WeightWaterKg)`;
- geometry increments use signed tension direction only inside this diagnostic;
- production segmentation remains exactly 0.20 m with no new segment-count cap.

The result must be labeled **frozen-load INFO diagnostic**, not a full angle-coupled Berteaux cable solution.

## Required result states

The future read model must classify rather than manufacture a root. At minimum:

- solved bounded slack case;
- line shorter than depth;
- taut `L == D` with non-zero horizontal load: no finite exact inextensible root;
- zero-horizontal taut/vertical geometry where geometry alone does not uniquely determine `Q0`;
- insufficient buoyancy capacity;
- invalid/degenerate numerical input;
- unavailable because required typed/source data is missing.

A small depth residual must never override an analytical no-finite-root classification.

## Required passive outputs

Without affecting verdicts, expose classification/availability, solved `Q0`, `Q_capacity`, capacity ratios, diagnostic endpoint X/Z, vertical residual, min/max H/V, V-sign-change flag, discrete point-load crossing count, numerical stop reason/iteration count, and a provenance note stating frozen-load, steady-current, wave-excluded behavior.

Diagnostic X/Z is not eligible for `SelectedShape`, 2D or PDF geometry.

## Runtime placement and next package

The next behavior-preserving code package may only propagate typed `BuoyInput` through

`ApplicationCalculationRunner -> CalculationSnapshotBuilder -> TechnicalReportDataBuilder`.

That plumbing package must not add the analyzer and must not change output. After it is regression-verified, a separate package may add the passive surface-boundary INFO read model to `TechnicalReportData`.

`SelectedMooringShapeProvider.Build(data.Shape, data.IterativeSolver)` must remain unchanged during both packages.

## Production authority remains blocked

This control mark does not authorize changes to `BuoyCalculator` physics, shape/discrete/iterative solvers, feedback, primary gate, selected X/Z, anchor/weak-link calculations, verdict, PDF/2D geometry, golden baseline, production MaxIterations, 0.20 m segmentation/unlimited count, signed submerged-weight semantics, or 3D.

Any future production consumer switch remains a separate Physics RFC package under #407/#413 with explicit selected-X/Z baseline impact review.
