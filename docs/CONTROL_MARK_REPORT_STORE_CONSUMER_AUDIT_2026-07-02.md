# 2026-07-02 — Report store consumer audit checkpoint

Status: done in branch `stores-audit`.

## Purpose

Start the store-consumer audit for the next read-model phase without changing solver, PDF, 2D, UI, or generated report output.

This checkpoint intentionally separates verified facts from unresolved consumer mapping.

## Verified facts

`TechnicalReportStorePublisher` is the current report-store publication boundary.

Current publisher path:

```text
TechnicalReportMarkdownBuilder
  -> TechnicalReportDataBuilder.Build(environment, result)
  -> TechnicalReportStorePublisher.Publish(data)
```

Current publisher writes:

```text
MooringShapeStore.Set(data.Shape)
MooringIterativeSolverStore.Set(data.IterativeSolver)
```

This means current report-store publication is limited to:

```text
- shape data
- iterative solver data
```

## Search limitation found during audit

GitHub code search is not reliable enough for this repository audit.

Observed during this step:

```text
Search for TechnicalReportStorePublisher returned no results, even though Services/TechnicalReportStorePublisher.cs exists.
Search for MooringShapeStore returned no results, even though TechnicalReportStorePublisher references it.
Search for MooringIterativeSolverStore returned no results, even though TechnicalReportStorePublisher references it.
```

Therefore store cleanup must not rely on GitHub code search alone.

## What is not yet proven

The complete consumer map is not yet proven.

Not yet proven:

```text
- all readers of MooringShapeStore
- all readers of MooringIterativeSolverStore
- whether any PDF path reads report stores directly
- whether any 2D path reads report stores directly
- whether any UI path still parses Markdown instead of consuming model/read-model data
```

## Boundary decision

Do not refactor stores yet.

Before any store refactor, first produce a complete consumer map from repository files, not from code search alone.

## Next allowed steps

1. Find the physical file paths for `MooringShapeStore` and `MooringIterativeSolverStore`.
2. Identify all direct readers of each store.
3. Classify each reader as:

```text
- report renderer
- PDF renderer
- 2D renderer
- UI/view-model
- diagnostic/debug path
```

4. Add a CI smoke check only after the consumer map is proven.
5. Only then begin moving consumers toward explicit read-model data.

## Non-goals for this checkpoint

This checkpoint does not change:

```text
- solver physics
- numerical formulas
- generated Markdown output
- PDF output
- 2D output
- UI behavior
- store behavior
```
