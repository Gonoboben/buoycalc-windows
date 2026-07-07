# Control mark: iterative-solver report labels

Date: 2026-07-07
Related issue: #162
Scope: documentation only

This record fixes the report-copy boundary before removing stale `v0.39` and diagnostics-only wording from the iterative-solver summary and section framing.

No production code, solver model, convergence criteria, stop reason, divergence guard, gate, selected-shape flow, report tables, calculation, engineering physics, 2D, PDF, JSON, DTO, XAML, commands, application version, or 3D behavior changes are made by this document.

## Current stale strings

`TechnicalReportMarkdownBuilder.AppendTotals(...)` currently emits:

```text
- Итерационный solver v0.39: {OK / каркас / не сошёлся}; итераций=...; ΔXпосл=...; max Δузла=...
```

`TechnicalReportMarkdownIterativeSolverSections` currently emits:

```text
## Итерационный solver v0.39 — диагностика
Этот раздел показывает отдельный feedback-слой ... Он не заменяет основной MooringShapeSolver.
Итог: {OK / каркас / не сошлось}; ...
```

These strings describe the original diagnostic skeleton rather than the current solver and gate-selection behavior.

## Existing result fields

`MooringIterativeSolverResult` already provides all data needed for accurate report wording:

```text
Converged
IterationCount
FinalOffsetChangeM
FinalMaxNodeDeltaM
FinalGeometryResidualM
Diverged
StopReason
StopReasonText
ConvergenceCriterion
MethodNote
```

No result-model change is required.

## Current architectural meaning

The solver runs the feedback cycle:

```text
shape
→ shape forces
→ shape tensions
→ discrete-load tensions
→ new shape
→ convergence and divergence checks
```

Its final shape is a candidate. A candidate may become the selected primary shape only through `MooringPrimaryShapeGate`; otherwise the fallback `MooringShapeSolver` result remains selected.

Therefore:

```text
- “v0.39” is not an appropriate current section label
- “каркас” is not an appropriate current non-convergence status
- “не заменяет основной MooringShapeSolver” is no longer universally true
- solver-generated MethodNote and versioned engineering metadata remain legitimate and must not be rewritten by this issue
```

## Exact totals replacement

Replace the totals line with:

```text
- Итерационный solver: {сошёлся / не сошёлся}; итераций=...; ΔXпосл=... м; max Δузла=... м; невязка Z=... м; причина остановки: {StopReasonText}
```

Required formatting:

```text
FinalOffsetChangeM: 0.####
FinalMaxNodeDeltaM: 0.####
FinalGeometryResidualM: 0.####
```

## Exact section replacements

Heading:

```text
## Итерационный solver — итерации и кандидатная форма
```

Introduction:

```text
Раздел показывает feedback-цикл: форма → силы по форме → натяжения → дискретные нагрузки → новая форма → проверка сходимости. Финальная форма является кандидатом: при выполнении критериев и прохождении MooringPrimaryShapeGate она может стать основной; иначе сохраняется fallback MooringShapeSolver.
```

Result line:

```text
Итог: {сошёлся / не сошёлся}; {StopReasonText} Итераций=...; финальная ΔX=... м; финальный max Δузла=... м; финальная невязка Z=... м; divergence=YES/NO.
```

## Allowed production diff

Only these files may change:

```text
Services/TechnicalReportMarkdownBuilder.cs
Services/TechnicalReportMarkdownIterativeSolverSections.cs
```

Only four report strings may change:

```text
- totals iterative-solver line
- section heading
- section introduction
- section result line
```

## Required unchanged content

```text
- solver.ConvergenceCriterion output
- iteration table heading, columns and rows
- solver.MethodNote output
- report section order
- every calculated numeric value
- MooringIterativeSolver and result records
- gate, selector and stores
- selected-shape, 2D and PDF source behavior
```

## Explicit exclusions

```text
- no global removal of historical v0.39/v0.40/v0.42 method metadata
- no solver or convergence changes
- no report-table refactor
- no physics or calculation changes
- no version bump
- no 3D
```
