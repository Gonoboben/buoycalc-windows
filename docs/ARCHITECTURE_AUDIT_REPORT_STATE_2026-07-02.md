# 2026-07-02 — architecture audit addendum: technical report state

Status: done in branch `step-45-architecture-audit-report-state`.

This file is an addendum to `docs/ARCHITECTURE_AUDIT.md`.

## What changed since the base architecture audit

The original audit described `Services/ReportBuilder.cs` as the technical Markdown report builder and diagnostic pipeline owner.
That is no longer the current state.

Current state:

```text
Services/ReportBuilder.cs has been removed.
TechnicalReportBuilder delegates to TechnicalReportMarkdownBuilder.
TechnicalReportMarkdownBuilder owns technical Markdown assembly.
TechnicalReportMarkdownSectionBridge routes known section names to dedicated renderer classes.
The bridge no longer has reflection fallback to ReportBuilder.
```

## Current technical report path

```text
TechnicalReportBuilder
  -> TechnicalReportMarkdownBuilder
  -> TechnicalReportMarkdownSectionBridge
  -> TechnicalReportMarkdownMovedSections
  -> TechnicalReportMarkdownDiscreteShapeSections
  -> TechnicalReportMarkdownDiscreteTensionSections
  -> TechnicalReportMarkdownDiscreteNodeSections
  -> TechnicalReportMarkdownIterativeSolverSections
  -> TechnicalReportMarkdownCheckSections
```

## Current CI guard

The existing `.NET Build` workflow now also runs:

```text
tools/check-reportbuilder-usage.ps1
```

The script checks for `ReportBuilder.Build(...)` calls before publishing the success commit status.

## What this stabilizes

```text
The technical Markdown report no longer depends on legacy ReportBuilder.
Known report sections have explicit renderer ownership.
Unknown report section names fail explicitly instead of silently calling a legacy reflection fallback.
```

## What is not solved yet

The base architecture risks are still valid outside the removed ReportBuilder path:

```text
- PDF, 2D, main UI and technical report still need a single result/read model strategy.
- Shape selection is still spread across fallback / alternative / candidate / selected concepts.
- User-facing statuses still need a single policy separate from solver diagnostics.
- 2D still needs to stop parsing Markdown and become a renderer of model data only.
- PDF still needs to stop depending on mixed text/store inputs and use user-facing report models.
```

## Next allowed stabilization direction

```text
Start the next architecture phase around a unified calculation result/read model.
Do not change solver physics.
Do not change PDF or 2D behavior until the model boundary is explicit.
```

CI status:

```text
pending PR check
```
