# Контрольная отметка: ownership дискретной нагрузки для signed node balance

Дата: 2026-08-12  
Issue: #383  
Scope: documentation / code-mapping only

## Цель

Закрыть программную часть Candidate B, не требующую изменения физики:

```text
- где находится внутренний дискретный узел по координате s;
- как группировать несколько connector/payload на одной s;
- какая существующая cumulative row содержит point load;
- как получить состояние ниже point load без изменения текущего analyzer;
- какую пару tension/geometry разрешено использовать в первом Candidate-B diagnostic.
```

Primary-source blocker Берто §2.1 сохраняется.

## Позиционная модель

`MooringSequencePositioner` изменяет `s` только для line elements.

Для линии:

```text
start = s
end   = s + LengthM
position = (start + end) / 2
s = end
```

Для connector/payload:

```text
start = s
end = s
position = s
```

Следовательно, один или несколько zero-length discrete items между двумя line items занимают один и тот же физический junction:

```text
s_node
```

Пример:

```text
Line A
Connector 1
Payload
Connector 2
Line B
```

даёт:

```text
End(Line A) = s_node
Position(Connector 1) = s_node
Position(Payload) = s_node
Position(Connector 2) = s_node
Start(Line B) = s_node
```

## Segment boundary mapping

`BuildSegmentRows` сегментирует каждый line item отдельно и после завершения line item выполняет:

```text
accumulatedLength += itemLength
```

Поэтому intended internal discrete junction соответствует границе line items:

```text
upper last segment.EndLengthM
≈ s_node
≈ lower first segment.StartLengthM
```

При implementation matching используется существующий малый coordinate tolerance, а не округление `s` до пользовательского количества знаков.

## Grouping rule

Несколько discrete UI elements на одной `s` являются одним механическим free-body node.

Перед Candidate B требуется группировка:

```text
nodeItems = all internal discrete loads at s_node
```

Суммарный signed load:

```text
F_node,x = Σ CurrentForceN
F_node,z = Σ WeightWaterKg * g

g = 9.80665 m/s²
```

`WeightWaterKg` сохраняет знак.

Запрещено:

```text
Abs(WeightWaterKg)
```

поскольку buoyant payload должен давать отрицательный Z load в системе `+Z вниз`.

## Current discrete cumulative semantics

`MooringDiscreteLoadTensionAnalyzer` для каждого segment start использует:

```text
discreteWeightBelow = Σ load.WeightWaterKg
                      where load.PositionAlongLineM >= segment.StartLengthM

discreteForceBelow = Σ load.CurrentForceN
                     where load.PositionAlongLineM >= segment.StartLengthM
```

Для первого segment ниже internal node:

```text
segment.StartLengthM == s_node
```

point load на `s_node` включается в cumulative row.

Обозначение:

```text
C_inclusive = (H_inclusive, V_inclusive)
```

где:

```text
H_inclusive = row.CumulativeHorizontalForceN
V_inclusive = row.CumulativeVerticalForceN
```

Эта строка содержит:

```text
- distributed loads lower segment и всех сегментов ниже;
- все discrete loads ниже;
- grouped point load ровно на s_node.
```

## Mechanical interpretation of inclusive state

Для Candidate-B mapping эта строка трактуется как algebraic cut state непосредственно **над grouped point load**.

Причина: она включает point load плюс всё, что находится ниже узла.

Это interpretation existing data; analyzer semantics не меняются.

## Lower-side exclusive state

Состояние непосредственно **под point load** получается вычитанием только grouped node load:

```text
H_below = H_inclusive - F_node,x
V_below = V_inclusive - F_node,z
```

Нельзя для Candidate B менять существующий predicate:

```text
>=
```

на:

```text
>
```

поскольку это изменило бы уже действующую discrete-load candidate/iterative feedback semantics.

Candidate B должен быть additive consumer текущих результатов.

## Algebraic ownership check

До любой геометрии можно детерминированно проверить:

```text
H_inclusive - H_below = F_node,x
V_inclusive - V_below = F_node,z
```

Это:

```text
software/data ownership identity
```

а не:

```text
physical equilibrium proof.
```

## Independent upper-segment continuity check

Для последнего segment непосредственно выше узла его start-cut state содержит:

```text
upper segment distributed load
+ grouped point load
+ everything below
```

Следовательно после удаления только distributed load самого upper segment должно получиться то же `C_inclusive`:

```text
H_upperStart - SegmentForce_upper = H_inclusive
V_upperStart - SegmentWeight_upper*g = V_inclusive
```

Это даёт независимую software-consistency проверку mapping между соседними segment rows.

## Geometry source for first Candidate B

Первый coherent Candidate-B diagnostic после снятия source blocker должен использовать одну model family:

```text
MooringSequencePositionResult
MooringDiscreteLoadTensionResult
MooringDiscreteLoadShapeResult
```

### Почему именно эта family

`MooringSequencePositionResult`:

```text
- задаёт s_node;
- задаёт исходные WeightWaterKg / CurrentForceN;
- сохраняет traceability до connector/payload.
```

`MooringDiscreteLoadTensionResult`:

```text
- содержит cumulative components с теми же discrete loads;
- непосредственно используется для alternative candidate shape.
```

`MooringDiscreteLoadShapeResult`:

```text
- строится непосредственно из MooringDiscreteLoadTensionResult;
- содержит геометрию того же pre-iterative candidate state.
```

Поэтому эта пара не смешивает результаты разных solver stages.

## Exact name / claim

Первый B1 diagnostic должен называться по смыслу, например:

```text
pre-iterative discrete-load candidate internal-node equilibrium residual
```

Запрещённое название:

```text
selected-shape equilibrium residual
```

пока не существует immutable final-iteration per-segment force state, соответствующего `MooringIterativeSolver.FinalShape`.

## Почему selected FinalShape пока исключён

Итерационный solver повторно вычисляет:

```text
shape projection
shape forces
shape tensions
discrete tensions
candidate shape
```

но `MooringIterativeSolverResult` не публикует полный final per-segment signed cumulative state как отдельный immutable read model.

Следовательно использование:

```text
FinalShape geometry
+
pre-iterative MooringDiscreteLoadTensionResult
```

создало бы hybrid residual между разными состояниями модели.

Это запрещено.

## Future selected-shape boundary

Если позже потребуется equilibrium diagnostic именно выбранной iterative shape, сначала нужен отдельный architecture/physics package:

```text
immutable final-iteration per-segment force/tension state
```

который относится к той же итерации, что и `FinalShape`.

Только после этого можно строить selected-shape signed residual.

## Candidate B1 after ownership mapping

После primary-source validation internal grouped node получает:

```text
F_node = (F_node,x, F_node,z)
```

Inclusive cumulative state:

```text
C_above = C_inclusive
```

Exclusive lower state:

```text
C_below = C_inclusive - F_node
```

Magnitude candidates:

```text
T_above = |C_above|
T_below = |C_below|
```

Signed geometric tangents:

```text
t_above = tangent of segment immediately above s_node
t_below = tangent of segment immediately below s_node
```

Proposed physical residual remains:

```text
R_node = -T_above*t_above + T_below*t_below + F_node
```

Этот residual не tautological, потому что magnitude cut states соединяются с фактическими geometric tangents.

Сам algebraic jump `C_above - C_below = F_node` проверяется отдельно и не подменяет physical residual.

## Multiple loads on same node

Все loads одной `s` суммируются до вычисления residual.

Не допускается:

```text
residual connector 1
residual payload
residual connector 2
```

как три независимых mechanical nodes, если между ними нет line length.

Правильно:

```text
one s_node
one grouped F_node
one internal free body
source list = connector 1 + payload + connector 2
```

## Boundary filtering

Из первого B1 исключаются:

```text
s = 0 / buoy-top boundary
s = LineLength / anchor-bottom boundary
```

потому что там отсутствуют solved boundary reactions / complete free bodies.

## Degenerate / unavailable cases

Node получает `INDETERMINATE`, если:

```text
- нет segment выше или ниже;
- junction не удаётся однозначно match к segment boundary;
- tangent degenerate;
- grouped load содержит non-finite data;
- required cumulative row отсутствует;
- node относится к boundary/contact/touchdown.
```

Нельзя публиковать fake zero.

## Validation requirements after source blocker

Первый implementation package должен включать synthetic cases:

```text
1. one connector exactly at line-item boundary;
2. connector + payload + connector at same s -> one grouped node;
3. positive WeightWaterKg;
4. negative WeightWaterKg;
5. horizontal-only node load;
6. vertical-only node load;
7. exact ownership identities above/below;
8. deliberate geometry mismatch -> nonzero physical residual;
9. no boundary nodes in B1 rows;
10. five canonical project scenarios measured without golden rewrite.
```

## Remaining primary blocker

Эта контрольная отметка закрывает code ownership/state-family вопрос, но не снимает:

```text
direct visual Berteaux §2.1 page/figure/equation verification
```

До него production Candidate B не создаётся.

## Инварианты

```text
- MooringDiscreteLoadTensionAnalyzer unchanged;
- >= predicate unchanged;
- discrete candidate geometry unchanged;
- iterative solver unchanged;
- gate unchanged;
- verdict unchanged;
- selected X/Z unchanged;
- signed WeightWaterKg preserved;
- 0.20 m target segmentation unchanged;
- anchor/seabed reactions not fabricated;
- report/UI/PDF/2D unchanged;
- JSON/DTO unchanged;
- golden baseline unchanged;
- no 3D.
```

## Safety gate

Docs PR merge only after successful exact-head:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```
