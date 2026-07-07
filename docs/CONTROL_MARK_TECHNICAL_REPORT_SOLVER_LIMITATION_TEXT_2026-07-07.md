# Control mark: technical-report solver limitation text

Date: 2026-07-07
Related issue: #159
Scope: documentation only

This record verifies the current selected-shape flow before replacing one stale sentence in the final `## Ограничения` section of `Services/TechnicalReportMarkdownBuilder.cs`.

No production code, report section order, solver, gate, shape selection, stores, 2D, PDF, calculation, engineering physics, JSON, DTO, XAML, commands, storage, version, or 3D behavior changes are made by this document.

## Current stale sentence

```text
v0.39 добавляет диагностический итерационный solver-слой. Он замыкает существующие блоки в цикл, но основной solver, 2D и PDF-схемы пока не заменяются.
```

This described the original diagnostic-only v0.39 stage. It is no longer accurate.

## Current calculation and publication flow

`TechnicalReportDataBuilder.Build(...)` creates:

```text
fallback shape from MooringShapeSolver
+ iterative candidate from MooringIterativeSolver
```

`TechnicalReportStorePublisher.Publish(...)` then performs:

```text
MooringShapeStore.Set(fallback shape)
MooringIterativeSolverStore.Set(iterative result)
```

`MooringIterativeSolverStore.Set(...)`:

```text
- evaluates MooringPrimaryShapeSelector
- stores the gate selection
- replaces MooringShapeStore.Current with the selected shape
```

## Promotion gate

A candidate becomes primary only when all current conditions allow it:

```text
- final shape exists
- at least two nodes exist
- iterative solver converged
- divergence guard did not trigger
- stop reason is Converged
```

When accepted:

```text
source = MooringIterativeSolver.FinalShape
uses discrete loads = true
```

When rejected or unavailable:

```text
source = MooringShapeSolver fallback
uses discrete loads = false
```

Therefore the iterative result is no longer diagnostics-only.

## Selected-shape boundary

`SelectedShapeStore.Current` reads `MooringPrimaryShapeSelectionStore.Current` and exposes the selected shape plus source and gate metadata. If no selection exists, it falls back to `MooringShapeStore.Current`.

## 2D source behavior

`Mooring2DDiagramSourceSelector` uses this order:

```text
1. valid SelectedShapeStore.Current shape
   + optional alternative-shape overlay
2. X/Z nodes parsed from report text
3. synthetic visualization fallback
```

Thus a promoted iterative candidate can become the primary 2D shape.

## PDF source behavior

`PdfDiagramSourceSelector` keeps this independent order:

```text
1. MooringAlternativeShapeStore.Current
2. SelectedShapeStore.Current
3. report-text shape metrics
4. visualization offset
```

Thus the PDF can use the selected gate result, but an available alternative discrete-load display shape has higher priority.

## Exact replacement sentence

Replace only the stale final sentence with:

```text
Расчёт формы остаётся предварительным: итерационный solver формирует кандидатную форму с дискретными нагрузками. Только кандидат, прошедший MooringPrimaryShapeGate, становится основной выбранной формой; иначе используется fallback MooringShapeSolver. 2D читает выбранную форму, а PDF сохраняет собственный порядок источников: альтернативная форма, выбранная форма, метрики отчёта и визуализационный fallback.
```

## Allowed production diff

Only this file may change:

```text
Services/TechnicalReportMarkdownBuilder.cs
```

Only the final `AppendLine(...)` sentence after the current method notes may change.

## Required invariants

```text
- report section order unchanged
- all method-note lines unchanged
- all tables and calculated values unchanged
- TechnicalReportDataBuilder unchanged
- TechnicalReportStorePublisher unchanged
- MooringIterativeSolver unchanged
- MooringPrimaryShapeGate and selector unchanged
- SelectedShapeStore unchanged
- 2D and PDF selectors unchanged
- no physics or solver result change
- no version bump
- no 3D
```
