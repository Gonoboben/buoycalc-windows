# ReportBuilder responsibility audit — 2026-07-01

## Context

Plan item:

```text
5. UserReportBuilder и TechnicalReportBuilder
```

Status: audit-only step in branch `step-13`.

This document records current responsibilities of `Services/ReportBuilder.cs` before extracting technical report orchestration into `TechnicalReportBuilder`.

No application code is changed in this step.

## Current finding

`ReportBuilder.Build(...)` currently mixes three different responsibilities:

```text
1. Technical data orchestration.
2. Global shape store side effects.
3. Markdown formatting of the full technical report.
```

This makes it difficult to treat `ReportBuilder` as just a report formatter.

## Evidence from current code

At the start of `Build(...)`, `ReportBuilder` creates technical result objects:

```text
SegmentTensionAnalyzer.Build(...)
MooringShapeSolver.Build(...)
MooringShapeProjection.Build(...)
MooringShapeForceAnalyzer.Build(...)
MooringShapeTensionAnalyzer.Build(...)
MooringSequencePositioner.Build(...)
MooringDiscreteLoadTensionAnalyzer.Build(...)
MooringDiscreteLoadShapeBuilder.Build(...)
MooringAlternativeDiscreteNodeProjector.Build(...)
MooringIterativeSolver.Build(...)
EngineeringDiagnostics.Build(...)
MooringVectorBalance.Build(...)
```

The same method also writes global stores:

```text
MooringShapeStore.Set(shape)
MooringIterativeSolverStore.Set(iterativeSolver)
```

The same method then formats the Markdown report by calling many `Append...` sections:

```text
AppendEnvironment
AppendBuoy
AppendAnchor
AppendTotals
AppendDiagnostics
AppendVectorBalanceRows
AppendElementRows
AppendSequencePositionRows
AppendModelCoverageRows
AppendSegmentRows
AppendTensionRows
AppendShapeRows
AppendShapeProjectionRows
AppendShapeForceRows
AppendShapeTensionRows
AppendDiscreteLoadTensionRows
AppendDiscreteLoadShapeRows
AppendAlternativeDiscreteNodeRows
AppendIterativeSolverRows
AppendChecks
```

## Responsibility groups

### 1. Technical report computation/orchestration

Should move behind a technical report boundary first, without changing solver behavior:

```text
Building shape result
Building projection result
Building shape force result
Building shape tension result
Building sequence positions
Building discrete load tensions
Building discrete load shape
Building alternative discrete nodes
Building iterative solver result
Building diagnostics
Building vector balance
```

Candidate name:

```text
TechnicalReportDataBuilder
```

Candidate result model:

```text
TechnicalReportData
```

### 2. Store side effects

Currently `ReportBuilder.Build(...)` writes shape stores as part of report creation.

This must not be changed in the first extraction step because 2D/PDF may still depend on the stores being populated after calculation.

Recommended transition:

```text
Step A: keep store writes in the same order, but move them behind an explicitly named method.
Step B: later move store writes closer to calculation/session result ownership.
```

Candidate method:

```text
PublishTechnicalReportStores(TechnicalReportData data)
```

### 3. Technical Markdown formatting

Append methods are mostly formatting responsibilities and can stay in `ReportBuilder` temporarily, or move later to a formatter after data extraction.

Candidate name after extraction:

```text
TechnicalReportMarkdownBuilder
```

## Safe extraction order

Recommended next small PRs:

```text
1. Add TechnicalReportData record/model containing the objects currently built at the top of ReportBuilder.Build(...).
2. Add TechnicalReportDataBuilder.Build(...) that performs the same analyzer calls in the same order.
3. Keep ReportBuilder.Build(...) output unchanged by consuming TechnicalReportData.
4. Keep MooringShapeStore.Set(...) and MooringIterativeSolverStore.Set(...) in the same sequence during the first step.
5. Only after CI success, consider moving append/formatting methods to TechnicalReportMarkdownBuilder.
```

## What must not change during extraction

```text
No solver physics changes.
No changes to MooringShapeSolver.
No changes to MooringIterativeSolver.
No changes to MooringPrimaryShapeGate.
No changes to BuoyCalculator.
No changes to PDF geometry.
No changes to 2D geometry.
No changes to user-facing report text in the first extraction step.
No changes to store write order in the first extraction step.
```

## Acceptance criteria for next code step

```text
TechnicalReportBuilder.Build(...) still returns exactly the same report text for the same input.
ReportBuilder.Build(...) remains callable until the migration is complete.
MooringShapeStore and MooringIterativeSolverStore are populated as before.
CI passes.
Diff is limited to report-boundary/data-builder files.
```

## Next allowed step

```text
refactor: introduce TechnicalReportData model without changing ReportBuilder output
```

## CI status

```text
ожидает проверки PR
```
