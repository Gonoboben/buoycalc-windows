# Контрольная отметка: authoritative cut-state для iterative discrete feedback

Дата: 2026-08-13  
Issue: #403  
Scope: documentation / code-mapping / physics boundary only

## Цель

Зафиксировать, почему текущий `MooringIterativeSolver` в проверенных сценариях приходит к неизменному discrete candidate state после первой кандидатной формы и какой cut-state contract должен быть доказан до изменения solver.

Эта отметка не меняет production code и не разрешает автоматическое обновление golden baseline.

## Связь с предыдущей validation

Merged control mark #400:

```text
docs/CONTROL_MARK_CANDIDATE_B_PRE_FINAL_MEASUREMENTS_2026-08-13.md
```

показал для пяти deterministic сценариев:

```text
PreCandidateOffsetM == FinalShapeOffsetM
PreTopDiscreteTensionKn == FinalTopDiscreteTensionKn
pre Candidate B == final Candidate B
```

В четырёх current-driven случаях iteration 2 при этом меняла некоторые intermediate значения:

```text
ShapeLineForceN
TopShapeTensionKn
```

но не меняла:

```text
TopDiscreteTensionKn
MooringDiscreteLoadShape X/Z
Candidate B residual
```

## 1. Текущий iterative pipeline

`MooringIterativeSolver.Build(...)` выполняет:

```text
currentShape
 -> MooringShapeProjection.Build(currentShape)
 -> MooringShapeForceAnalyzer.Build(result, projection)
 -> MooringShapeTensionAnalyzer.Build(result, feedbackTensions, shapeForces)
 -> BuildFeedbackTensions(shapeTensions)
 -> MooringDiscreteLoadTensionAnalyzer.Build(
        result,
        nextFeedbackTensions,
        sequencePositions)
 -> MooringDiscreteLoadShapeBuilder.Build(currentShape, discreteTensions)
 -> next currentShape
```

По интерфейсу выглядит так, будто `nextFeedbackTensions` должны определять новый discrete candidate state.

## 2. Что содержит `BuildFeedbackTensions`

`BuildFeedbackTensions(shapeTensions)` переносит для каждого segment:

```text
ShapeSegmentForceN
CumulativeShapeHorizontalForceN
CumulativeVerticalForceN
ShapeTensionKn
ShapeAngleFromVerticalDeg
```

в новый `SegmentTensionRow`.

Таким образом feedback row действительно содержит актуальное shape-based distributed cut state для текущей X/Z-формы.

## 3. Current disconnect в `MooringDiscreteLoadTensionAnalyzer`

Текущий analyzer принимает:

```text
IReadOnlyList<SegmentTensionRow> originalTensionRows
```

но candidate-driving cumulative state вычисляет не из этих rows.

Он заново накапливает базовые `CalculationResult.SegmentRows`:

```text
cumulativeSegmentHorizontalForceN += segment.CurrentForceN
cumulativeSegmentVerticalForceN += segment.WeightWaterKg * g
```

и затем добавляет discrete point loads:

```text
cumulativeHorizontalForceN =
    cumulativeSegmentHorizontalForceN + discreteForceBelowN

cumulativeVerticalForceN =
    cumulativeSegmentVerticalForceN + discreteWeightBelowKg * g
```

Из этих значений строятся:

```text
DiscreteTensionKn
DiscreteAngleFromVerticalDeg
```

Следовательно изменение `originalTensionRows` не изменяет candidate-driving:

```text
CumulativeHorizontalForceN
CumulativeVerticalForceN
DiscreteTensionKn
DiscreteAngleFromVerticalDeg
```

## 4. Где feedback rows всё-таки используются

`originalTensionRows` используются только для comparative fields:

```text
OriginalTensionKn
OriginalAngleFromVerticalDeg
TensionDifferenceKn
AngleDifferenceDeg
relative difference / status
```

То есть iterative feedback сейчас влияет на отчёт о различии между состояниями, но не на состояние, из которого строится новая discrete candidate geometry.

## 5. Почему candidate X/Z остаётся неизменным

`MooringDiscreteLoadShapeBuilder` строит candidate angle из:

```text
row.DiscreteAngleFromVerticalDeg
```

и сохраняет:

```text
row.DiscreteTensionKn
```

как candidate tension evidence.

`OriginalTensionKn` / `OriginalAngleFromVerticalDeg` не определяют X/Z candidate.

Поэтому изменение shape-feedback rows без изменения `DiscreteAngleFromVerticalDeg` не может изменить candidate geometry.

Это объясняет измеренный fixed-point pattern #400.

## 6. Distributed shape state не содержит point loads

`MooringShapeForceAnalyzer` работает только по:

```text
CalculationResult.SegmentRows
+
MooringShapeProjectionResult.Rows
```

и для каждого line segment вычисляет shape-based hydrodynamic force по нормальной составляющей течения.

Connector/payload point loads здесь не добавляются.

`MooringShapeTensionAnalyzer` затем bottom-up накапливает:

```text
shape segment horizontal force
+
signed segment.WeightWaterKg * g
```

и не добавляет connector/payload point loads.

Следовательно:

```text
CumulativeShapeHorizontalForceN
CumulativeVerticalForceN
```

являются distributed-line cut state.

## 7. Point loads находятся в отдельной model family

`MooringSequencePositioner` классифицирует:

```text
line -> distributed interval
connector/payload -> zero-length discrete position s
buoy -> top boundary
anchor -> bottom boundary
```

При формировании discrete-load sums буй и якорь исключаются.

`MooringDiscreteLoadTensionAnalyzer` строит point-load list только из:

```text
sequencePositions.Rows
where IsDiscrete
and Kind != Буй
and Kind != Якорь
```

Следовательно point loads являются отдельным слагаемым и не присутствуют в shape distributed cut state.

## 8. Cut ownership совпадает

Для каждого `SegmentCalculationRow i` существующие tension analyzers работают bottom-up и записывают row после включения нагрузки segment `i` и всего, что ниже.

Поэтому row `i` соответствует:

```text
START / TOP CUT of segment i
```

`MooringDiscreteLoadTensionAnalyzer` на том же cut добавляет point load, если:

```text
load.PositionAlongLineM >= segment.StartLengthM
```

Этот predicate уже зафиксирован #385 и не меняется этой отметкой.

Таким образом distributed feedback row и discrete point-load sum относятся к одному intended cut.

## 9. Authoritative cut-state contract

Для iterative candidate следующего шага authoritative **distributed** state должен приходить из переданного `SegmentTensionRow` feedback state, а не повторно из `CalculationResult.SegmentRows`.

Для segment start-cut `i`:

```text
H_dist,i = distributedRow.CumulativeHorizontalForceN
V_dist,i = distributedRow.CumulativeVerticalForceN
```

Point loads на/ниже cut:

```text
H_point,i = Σ load.CurrentForceN
            where load.PositionAlongLineM >= segment.StartLengthM

V_point,i = Σ load.WeightWaterKg * g
            where load.PositionAlongLineM >= segment.StartLengthM
```

Следующий discrete cut state:

```text
H_next,i = H_dist,i + H_point,i
V_next,i = V_dist,i + V_point,i
```

и далее:

```text
T_next,i = sqrt(H_next,i² + V_next,i²)
angle_next,i = atan2(|H_next,i|, |V_next,i|)
```

в рамках текущей planar angle convention.

Это project discretization of distributed + point-load superposition, а не новая физическая формула.

## 10. Почему здесь нет double count

При соблюдении source contract:

```text
H_dist / V_dist
```

содержат только distributed line loads.

```text
H_point / V_point
```

содержат только connector/payload point loads.

Поэтому сумма не дублирует одну и ту же physical load family.

Нельзя одновременно:

```text
re-sum segment.CurrentForceN / WeightWaterKg
+
use distributed feedback cumulative H/V
```

потому что это уже привело бы к двойному учёту distributed line loads.

## 11. Pre-iterative behavior-preservation identity

Первый вызов `MooringDiscreteLoadTensionAnalyzer` до iterative feedback получает:

```text
SegmentTensionAnalyzer.Build(result)
```

Эти rows уже содержат cumulative state, построенный из тех же базовых:

```text
segment.CurrentForceN
segment.WeightWaterKg * g
```

которые текущий discrete analyzer повторно накапливает самостоятельно.

Следовательно при переходе к authoritative supplied distributed rows первый/pre-iterative result должен оставаться численно идентичным в пределах arithmetic software tolerance.

Это обязательная regression перед любым solver experiment.

## 12. Iterative behavior intentionally may change

Во второй и следующих итерациях supplied distributed rows являются shape-feedback rows:

```text
ShapeSegmentForceN
CumulativeShapeHorizontalForceN
CumulativeVerticalForceN
```

Поэтому после исправления coupling допустимо и ожидаемо, что изменятся:

```text
DiscreteTensionKn
DiscreteAngleFromVerticalDeg
candidate X/Z
iteration count
convergence metrics
Candidate B residual
```

Эти изменения нельзя автоматически считать улучшением.

Они должны быть отдельно валидированы.

## 13. Parameter semantics

Название текущего параметра:

```text
originalTensionRows
```

становится двусмысленным, если rows являются authoritative distributed state.

Целевой смысл следует документировать как:

```text
distributedTensionRows
```

или эквивалентное явное имя.

Существующие row fields:

```text
OriginalTensionKn
OriginalAngleFromVerticalDeg
```

могут продолжать означать tension/angle distributed state **до добавления point loads**.

Это не требует менять публичный отчёт в первом experiment package, но naming semantics должны быть явно зафиксированы.

## 14. Required mapping checks

До solver merge обязательны deterministic checks:

```text
1. каждый SegmentCalculationRow имеет ровно один supplied distributed row того же SegmentNumber;
2. отсутствие row -> explicit unavailable/error path, а не silent zero;
3. supplied Start/segment mapping соответствует current SegmentNumber ordering;
4. same-s point loads добавляются ровно один раз;
5. buoy/anchor boundary loads не попадают в internal point-load sum;
6. signed negative WeightWaterKg сохраняет знак;
7. pre-iterative current behavior воспроизводится численно;
8. no-point-load case возвращает supplied distributed H/V без изменения.
```

## 15. First production experiment policy

Следующий допустимый production branch должен рассматриваться как **physics experiment**, а не готовый solver fix.

Он может:

```text
- сделать supplied distributed rows authoritative в MooringDiscreteLoadTensionAnalyzer;
- добавить focused regression на cut-state identities;
- запустить существующие canonical scenarios;
- зафиксировать все изменения candidate/iteration/golden outputs.
```

Но он не должен автоматически merge, если golden baseline изменился.

При golden mismatch нужно остановиться и классифицировать каждое изменение.

## 16. Golden policy

Запрещено:

```text
перегенерировать engineering-baseline.json
только чтобы CI стал зелёным.
```

Если genuine feedback coupling меняет historical output:

```text
- записать старое значение;
- записать новое значение;
- объяснить physical reason;
- проверить limiting/reference behavior;
- отдельным control mark решить, допустимо ли изменение baseline.
```

## 17. Solver/gate policy

Даже после coupling Candidate B остаётся INFO-only.

Нельзя в том же PR добавлять Candidate B в:

```text
- convergence condition;
- divergence condition;
- stop reason;
- MooringPrimaryShapeGate;
- selected X/Z decision;
- CalculationResult.Verdict;
- anchor/weak-link decisions.
```

## 18. Source-backed signs остаются прежними

Point-load signed Z:

```text
F_point,z = WeightWaterKg * g
```

с `+Z` вниз.

Запрещено использовать `Abs(WeightWaterKg)`.

Berteaux source boundary #387 не меняется.

## 19. Не входит в scope этой отметки

```text
- implementation solver coupling;
- изменение target segmentation 0.20 m;
- segment-count limit;
- изменение current drag equation;
- изменение Candidate B equation;
- engineering residual threshold;
- buoy/top reaction;
- anchor/seabed reaction;
- gate/verdict;
- 2D/PDF physics;
- JSON/DTO;
- 3D.
```

## 20. Следующий допустимый шаг

После green merge этой документации:

```text
focused draft physics experiment:
make supplied distributed cut rows authoritative
inside MooringDiscreteLoadTensionAnalyzer
```

Обязательные этапы:

```text
A. prove pre-iterative numerical identity;
B. run controlled iterative scenarios;
C. inspect Candidate B before/after each iteration;
D. inspect any five-scenario golden mismatch;
E. do not merge changed physics until reference/limiting evidence supports it.
```

## Merge gate этой документации

Exact final head:

```text
.NET Build: success
Selected Shape Consumer Scan: success
Report Store Consumer Scan: success
```

Production code и golden baseline этой отметкой не меняются.
