# Control mark: application calculation run boundary

Date: 2026-08-11  
Issue: #365  
Phase: architecture optimization, documentation-first

## Purpose

Define the next ownership boundary after retirement of mutable shape/report compatibility stores.

This control mark does **not** change calculations. It records where calculation orchestration lives today and the exact application-level boundary that later implementation PRs may introduce without changing engineering behavior.

## Current call path

The current presentation flow is:

```text
MainWindowViewModel.Calculate
  -> MainWindowCalculationInputBuilder.Build(...)
  -> BuoyCalculator.Calculate(
       input.Environment,
       input.Buoy,
       input.AssemblyItems,
       input.Anchor,
       input.SafetyFactor)
     -> CalculationResult
  -> MainWindowCalculationDisplayBuilder.Build(..., result)
     -> CalculationSnapshotBuilder.Build(input.Environment, result)
        -> TechnicalReportDataBuilder.Build(...)
        -> SelectedMooringShapeProvider.Build(...)
        -> immutable CalculationSnapshot
     -> ReportBuildBoundary.Build(..., snapshot)
     -> display/read-model assembly
```

Two presentation classes therefore share ownership of one engineering run:

1. `MainWindowViewModel` invokes the calculation core directly.
2. `MainWindowCalculationDisplayBuilder` creates the engineering snapshot from that result.

The reports themselves are already passive consumers; this boundary is the remaining orchestration leak.

## Target application boundary

A later implementation package may introduce an application-level run contract equivalent to:

```text
ApplicationCalculationRunner.Run(
    EnvironmentInput,
    BuoyInput,
    IReadOnlyList<AssemblyItemInput>,
    AnchorInput,
    safetyFactor)
  -> BuoyCalculator.Calculate(...)
  -> CalculationSnapshotBuilder.Build(environment, result)
  -> ApplicationCalculationRun(
       CalculationResult Result,
       CalculationSnapshot Snapshot)
```

The exact type names are implementation details. The invariant is that one application operation owns both the core calculation and immediate immutable snapshot creation.

## Required post-migration flow

Presentation should become:

```text
MainWindowViewModel.Calculate
  -> build/parse user inputs
  -> application calculation use-case
     -> completed Result + Snapshot
  -> MainWindowCalculationDisplayBuilder
     -> consume completed Result + Snapshot
     -> report/display assembly only
```

`MainWindowCalculationDisplayBuilder` must no longer execute `CalculationSnapshotBuilder.Build(...)` once the migration is complete.

## Behavior-preserving constraints

The application boundary must preserve exactly:

- one `BuoyCalculator.Calculate(...)` invocation for one user calculation;
- the same five arguments in the same engineering meaning and units;
- the returned `CalculationResult` values;
- the `CalculationSnapshotBuilder.Build(environment, result)` input pair;
- `TechnicalReportData` values;
- selected X/Z source, coordinates, gate decision and `UsesDiscreteLoads`;
- report input data;
- 2D/PDF selected-X/Z handoff;
- fixed production segmentation target `0.20 m` with no segment-count cap;
- signed line `WeightWaterKgM`, including intentionally negative buoyant-line values.

## Forbidden changes in this architecture sequence

Do not change:

- solver equations or iteration criteria;
- `MooringPrimaryShapeGate` / selector logic;
- force, tension, buoyancy, anchor or wave formulas;
- diagnostics thresholds or meanings;
- segmentation rules;
- current-profile physics;
- selected X/Z values;
- PDF/2D geometry behavior;
- project JSON schema;
- application version;
- golden regression baseline;
- introduce global mutable calculation state;
- introduce 3D.

## Implementation sequence

Use small PRs:

1. **Application type/facade only** — add the application run result and runner that wrap the existing calculator + snapshot builder. No presentation caller change yet.
2. **ViewModel handoff** — replace the direct `BuoyCalculator.Calculate(...)` call with the application runner; pass completed result/snapshot forward.
3. **Display boundary** — make `MainWindowCalculationDisplayBuilder` consume the supplied snapshot instead of constructing it.
4. **Architecture guards** — reject direct presentation-layer calculator calls and snapshot construction inside render/report/display builders.

Do not combine solver/physics work into these PRs.

## Validation contract

Every implementation PR must keep the committed deterministic engineering baseline unchanged and have successful exact-head:

- `.NET Build`;
- `Selected Shape Consumer Scan`;
- `Report Store Consumer Scan`.

The `.NET Build` includes the five canonical engineering regression scenarios, including the intentionally negative signed water weight case.

## Completion criterion

Issue #365 is complete when:

- the ViewModel no longer calls `BuoyCalculator.Calculate(...)` directly;
- the display/report layer no longer calls `CalculationSnapshotBuilder.Build(...)`;
- one application use-case returns the completed calculation result and immutable snapshot;
- all reports/2D/PDF remain consumers only;
- golden numerical behavior remains unchanged.
