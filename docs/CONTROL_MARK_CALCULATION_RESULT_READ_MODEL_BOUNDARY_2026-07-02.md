# 2026-07-02 — Calculation result read-model boundary

Status: done in branch `readmodel`.

Related issue:

```text
#46 — Architecture: define calculation result read model boundary
```

## Current observed state

The technical report path is now explicit and protected by CI smoke checks.

Current report entry point:

```text
TechnicalReportBuilder.Build(projectName, environment, buoy, anchor, result)
```

Current report boundary:

```text
TechnicalReportBuilder
  -> TechnicalReportMarkdownBuilder
  -> TechnicalReportDataBuilder
  -> TechnicalReportStorePublisher
  -> TechnicalReportMarkdownSectionBridge
  -> dedicated markdown renderer classes
```

`CalculationResult` is still the solver-facing result object passed into report generation.

`TechnicalReportDataBuilder.Build(environment, result)` already creates report-facing data from that solver-facing object.

`TechnicalReportStorePublisher.Publish(data)` publishes that report-facing data for other consumers.

## Boundary decision

Treat `CalculationResult` as solver-facing output.

Treat explicit read models such as technical report data as renderer-facing/user-facing input.

Future Markdown, PDF, 2D, and UI work should move toward this direction:

```text
CalculationResult
  -> explicit read-model builder
  -> Markdown / PDF / 2D / UI renderers
```

## Non-goals for this marker

This marker does not change:

```text
- solver physics
- numerical formulas
- generated Markdown output
- PDF output
- 2D output
- UI behavior
- public app workflow
```

## Why this matters

The next architecture phase should avoid making PDF, 2D, or UI parse Markdown or depend on mixed report stores.

The safer direction is to make renderers consume explicit read-model data.

## Next allowed steps

1. Add a CI smoke check for the read-model boundary.
2. Identify all consumers of `TechnicalReportStorePublisher` and report stores.
3. Document which consumers should move first to shared read-model data.
4. Only after that, refactor PDF, 2D, or UI consumers in small PRs.

## Safety gate

Every follow-up PR must keep `.NET Build` green.

Every follow-up PR must state explicitly whether it changes output.

No solver physics changes are allowed in this phase.
