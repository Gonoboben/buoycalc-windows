# Signed-Geometry Downstream Authority Contract

Date: 2026-08-20  
RFC: #487  
Package: E1  
Status: control contract only; no runtime/output change

## Purpose

Define the production ownership boundary for downstream tension, anchor, weak-link, offset, and verdict quantities before any signed-geometry selected-X/Z switch.

This package changes no solver, formulas, selected source, selected X/Z, baseline, report, PDF, 2D, UI, elasticity, or 3D behavior.

## 1. Existing ownership is split

The current pipeline has two distinct data families that must not be conflated.

### Legacy scalar calculation result

`BuoyCalculator.Calculate` produces `CalculationResult` before selected-shape arbitration. In the current model:

- `TensionKn` is derived from scalar horizontal force and net-buoyancy vertical force;
- `AnchorReserve` is derived from anchor holding capacity and scalar horizontal-force demand;
- `EstimatedOffsetM` is the scalar `H/V * depth` estimate;
- weak-link reserve/status is derived from the same scalar `TensionKn`;
- `Verdict` and `MainRisk` are derived from checks that include tension reserve and anchor reserve.

These values are therefore not automatically owned by whichever X/Z shape later becomes selected.

### Selected-shape node values

The engineering baseline obtains these fields from the selected `MooringShapeResult.Nodes`:

- `SelectedTensionSumKn` = sum of selected node `SegmentTensionKn`;
- `SelectedAngleSumDeg` = sum of selected node `SegmentAngleFromVerticalDeg`;
- `SelectedSamples.TensionKn` = selected sampled-node `SegmentTensionKn`;
- `SelectedSamples.AngleFromVerticalDeg` = selected sampled-node `SegmentAngleFromVerticalDeg`.

These values do follow the selected shape object and therefore require truthful per-node tension/angle semantics for any future signed candidate.

## 2. Geometry promotion does not rewrite scalar equilibrium

A future signed candidate becoming eligible for selected X/Z does not authorize silently replacing, retaining under a misleading label, or recomputing `CalculationResult.TensionKn`, `AnchorReserve`, `EstimatedOffsetM`, weak-link reserve, or `Verdict` in presentation code.

Any change to those quantities must be owned by a calculation-core equilibrium state with an explicit production contract.

## 3. Per-node tension/angle gate

A signed `MooringShapeResult` must not become production-selected while its `SegmentTensionKn` or `SegmentAngleFromVerticalDeg` values are placeholders, copied from an unrelated candidate, or reconstructed only for display.

Before promotion, the signed candidate must either:

1. provide validated per-node tension and angle values from the same signed equilibrium state; or
2. participate in an explicitly modeled mixed-authority result whose source metadata makes the split unambiguous to all downstream consumers.

The current schema does not yet establish option 2. Therefore no production switch is authorized by this document.

## 4. Anchor and weak-link ownership

Anchor demand, weak-link reserve, and their checks must consume a calculation-core force/equilibrium state.

They must not be derived from rendered coordinates, report tables, PDF geometry, or UI measurements.

If a signed equilibrium state later changes the horizontal/vertical force state used by the current scalar formulas, anchor reserve and weak-link reserve must be recomputed in the core from that same validated state before the result can be presented as a coherent signed solution.

## 5. Verdict ownership

`Verdict` and `MainRisk` are downstream engineering decisions, not presentation labels. Because current verdict checks depend on scalar tension and anchor reserve, a future authority migration must keep verdict provenance coherent with the force state that owns those checks.

A signed-selected geometry must not be reported as a fully signed engineering solution while verdict-critical quantities still belong to a different, undisclosed equilibrium state.

## 6. Estimated offset semantics

`EstimatedOffsetM` is currently a scalar `H/V * depth` estimate and is not the selected shape endpoint.

A future signed selected geometry may have an authoritative endpoint horizontal offset distinct from this legacy estimate. The two meanings must remain separate unless a later explicit migration changes the field contract.

No package may silently redefine `EstimatedOffsetM` to mean signed-shape endpoint offset.

## 7. Required validation before downstream authority migration

Before any production package transfers signed authority into these downstream fields, validation must prove or explicitly isolate:

1. source of top/system tension used by `TensionKn`;
2. source and sign convention of per-node/segment tensions;
3. source of per-node angles and their display-vs-physics role;
4. anchor horizontal-demand source;
5. weak-link demand source and reserve calculation;
6. verdict/check provenance;
7. relationship between legacy `EstimatedOffsetM` and selected endpoint offset;
8. consistency for discrete connector/payload loads;
9. deterministic fallback/rejection behavior when signed equilibrium is unavailable;
10. exact old/new field table for every historical fixture before any golden change.

## 8. Historical baseline disposition

The fields previously classified by the historical golden-impact audit as `ProductionIntegrationRequired` remain exactly that until a later validated migration package.

No committed golden value may be changed merely because a signed geometry candidate exists. For every production-solvable fixture, any future field change requires a reviewed old/new table and a stated physical source.

## 9. Package E1 exit condition

E1 is complete when this ownership contract merges with no runtime change.

The next safe package is validation-only: encode the current ownership identities and field-source split as deterministic regressions. That validation package must remain behavior-preserving and must not switch selected X/Z or downstream authority.
