# Контрольная отметка: единый источник длины в MooringShapeSolver

Дата: 2026-08-12  
Issue: #378  
Scope: documentation only

## Причина

Candidate A из #374/#377 выявил существующую внутреннюю численную несогласованность `MooringShapeSolver`.

В одном расчёте используются два представления общей длины линии:

```text
A. CalculationResult.LineLengthM
B. Σ SegmentCalculationRow.SegmentLengthM
```

Математически они должны описывать одну и ту же физическую длину, но B является реконструкцией после сегментации и накопления `double`.

Для канонического случая:

```text
LineLengthM = 50
Depth = 50
250 сегментов
```

фактически:

```text
A = 50
B = 50.00000000000016
```

Это достаточно, чтобы `BuildNodes -> ScaleAngle` интерпретировал строго вертикальную линию как формально slack `L > Depth` и добавил ненулевой геометрический угол.

## Текущая последовательность

`MooringShapeSolver.Build(...)`:

```text
lineLengthM = result.LineLengthM

SolveAngleScale(
    orderedSegments,
    tensionRows,
    lineLengthM,
    targetAnchorDepthM)
```

Но затем `BuildNodes(...)` не получает этот `lineLengthM` и вычисляет заново:

```text
var lineLengthM = orderedSegments.Sum(x => x.SegmentLengthM);
```

После этого локальное значение используется в:

```text
VerticalSpan(...)
ScaleAngle(...)
lineShorterThanDepth classification
```

Таким образом, `SolveAngleScale` и `BuildNodes` могут использовать разные значения физически одного параметра.

## Воспроизведённый limiting case

В #377 exact-head validation доказано:

```text
CalculationResult.LineLengthM = 50
Σ SegmentLengthM = 50.00000000000016
Current = 0
SegmentCurrentForceN = 0
BaseCumulativeH = 0
BaseAngle = 0 deg
AngleScale = 1
```

Но fallback geometry содержит:

```text
ShapeNodeAngle = 4.597711793052989e-6 deg
dx ≈ 1.6049041769309896e-8 m на сегменте 0.2 m
```

Угол полностью объясняется:

```text
acos(50 / 50.00000000000016)
= 4.597711793052989e-6 deg
```

То есть источник отклонения известен и детерминирован.

## Design decision

Авторитетным источником **общей инженерной длины линии** внутри `MooringShapeSolver` должен быть:

```text
CalculationResult.LineLengthM
```

### Причины

1. Это значение строится непосредственно из включённых `AssemblyItemInput` line lengths:

```text
lineItems.Sum(max(0, item.LengthM))
```

и представляет проектную/инженерную длину постановки до дискретизации.

2. Оно уже является источником:

```text
MooringShapeResult.LineLengthM
SolveAngleScale input
```

3. `Σ SegmentLengthM` — проверяемая реконструкция после разбиения линии. Она нужна для diagnostics и segment-chain consistency, но не должна заново определять глобальный физический параметр внутри того же solver run.

4. `MooringDiscreteLoadShapeBuilder` уже читает:

```text
originalShape.LineLengthM
```

поэтому единый source в fallback solver уменьшает внутреннее расхождение между fallback и candidate geometry paths.

## Разрешённый будущий production change

Минимальный implementation package должен менять только передачу уже рассчитанного `lineLengthM` внутрь `BuildNodes`.

Предпочтительная форма:

```text
Build(...)
  lineLengthM = result.LineLengthM
  ...
  BuildNodes(
      orderedSegments,
      tensionRows,
      targetAnchorDepthM,
      lineLengthM,
      iteration.AngleScale)
```

`BuildNodes` после этого **не** должен выполнять:

```text
orderedSegments.Sum(x => x.SegmentLengthM)
```

как источник глобальной line length.

Тот же авторитетный `lineLengthM` должен использоваться внутри `BuildNodes` для:

```text
VerticalSpan
ScaleAngle geometric fallback
lineShorterThanDepth
```

## Что НЕ меняется

Запрещено в #378 одновременно менять:

```text
- SegmentCalculationRow generation;
- targetSegmentLengthM = 0.20 m;
- segment count policy;
- SegmentTensionAnalyzer;
- force equations;
- drag model;
- signed WeightWaterKg;
- DepthToleranceM;
- MaxIterations;
- ScaleAngle formula;
- baseAngle = max(force angle, geometric angle);
- bisection logic;
- MooringDiscreteLoadShapeBuilder;
- MooringIterativeSolver;
- MooringPrimaryShapeGate;
- CalculationResult.Verdict;
- anchor / weak-link / WLL calculations;
- 2D / PDF;
- JSON / DTO;
- 3D.
```

То есть меняется не физическая модель углов, а **источник одного уже существующего глобального параметра** внутри fallback solver.

## Segment sum остаётся важной диагностикой

После изменения нельзя скрывать расхождение:

```text
Σ SegmentLengthM - CalculationResult.LineLengthM
```

Существующие diagnostics segment-length/chain consistency продолжают проверять корректность дискретизации.

Иными словами:

```text
CalculationResult.LineLengthM
  = authoritative engineering input/result

Σ SegmentLengthM
  = discretization reconstruction / consistency evidence
```

Они имеют разные роли и не должны взаимозаменяться внутри solver geometry.

## Expected limiting behavior

### Case 1 — exact vertical line

```text
LineLengthM = Depth
Current = 0
force angle = 0
```

Ожидается после #378:

```text
geometric fallback angle = 0
X offset = 0
Candidate-A Δθ -> 0
Candidate-A R_rel -> 0
```

без изменения force state.

### Case 2 — genuine slack line

```text
LineLengthM > Depth
```

Геометрический fallback angle остаётся:

```text
acos(Depth / LineLengthM)
```

с использованием authoritative project length.

Физический смысл slack geometry не меняется.

### Case 3 — genuine short line

```text
LineLengthM + DepthToleranceM < Depth
```

short-line/submerged classification должна остаться той же по проектной длине.

### Case 4 — multiple line items

Для последовательности:

```text
Line A + connectors/payloads + Line B + ...
```

глобальная line length остаётся суммой line item inputs независимо от количества созданных segment rows.

### Case 5 — signed buoyant line

Отрицательный `WeightWaterKgM` не имеет отношения к выбору глобального line-length source и должен сохраниться без изменений.

## Golden baseline policy

Это solver change, поэтому нельзя автоматически переписать golden baseline.

Порядок:

1. выполнить production change только в `MooringShapeSolver.cs`;
2. запустить существующий five-scenario golden verifier;
3. если baseline не меняется — merge только после обычных checks;
4. если baseline меняется хотя бы в одном числовом поле — остановить merge;
5. объяснить каждое изменение через line-length source correction;
6. менять baseline только отдельным явно reviewed decision, а не вместе с кодом «чтобы CI прошёл».

Предпочтительный результат — существующий golden baseline остаётся неизменным либо изменения ограничены только ранее ошибочным fallback-derived geometry, не пользовательским engineering result.

## Candidate A interaction

#377 использует representation-derived numerical budget для текущего pre-#378 поведения.

После исправления:

```text
segmentSum > Depth из-за floating reconstruction
```

больше не должен создавать геометрический X в `MooringShapeSolver`, поскольку solver использует `CalculationResult.LineLengthM`.

При этом validation budget #377 намеренно вычисляется из segment representation и остаётся **верхней границей**, поэтому уменьшение actual residual не требует ослабления теста.

Отдельным последующим validation cleanup можно будет ужесточить limiting-case assertion до near-zero identity, но это не обязательно делать в solver PR.

## Source / physics boundary

Изменение #378 не вводит новое уравнение равновесия и не заменяет Berteaux-derived physics.

Оно устраняет несогласованность представления одного входного параметра перед дальнейшей source-backed validation #374.

Поэтому exact primary Berteaux page/equation citation для Candidate B остаётся отдельной задачей и не блокирует этот numerical consistency fix.

## Разрешённый production diff

Предпочтительно:

```text
Services/MooringShapeSolver.cs
```

плюс только focused validation assertions при необходимости.

Не добавлять report/UI changes в тот же PR.

## Safety gate

Implementation PR допускается к merge только при successful exact final head:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```

и после подтверждения, что five-scenario golden baseline не был молча изменён.
