# Контрольная отметка: final-iteration discrete force-state boundary

Дата: 2026-08-13  
Issue: #392  
Scope: documentation / architecture-physics boundary only

## Цель

Создать точную immutable-границу данных, необходимую для будущей проверки signed equilibrium именно той iterative candidate-формы, которая может пройти `MooringPrimaryShapeGate`.

Текущий Candidate B намеренно относится только к pre-iterative discrete-load candidate, потому что после завершения `MooringIterativeSolver` сохраняется `FinalShape`, но не сохраняется force/tension state той же последней итерации.

Эта отметка не меняет solver и не вводит новый residual.

## Текущий iteration pipeline

В каждой итерации `MooringIterativeSolver.Build(...)` уже выполняет последовательность:

```text
currentShape
 -> MooringShapeProjection.Build(currentShape)
 -> MooringShapeForceAnalyzer.Build(...)
 -> MooringShapeTensionAnalyzer.Build(...)
 -> BuildFeedbackTensions(...)
 -> MooringDiscreteLoadTensionAnalyzer.Build(...) = discreteTensions
 -> MooringDiscreteLoadShapeBuilder.Build(currentShape, discreteTensions) = nextShape
 -> ToShapeResult(currentShape, nextShape, ...) = next currentShape
```

Следовательно внутри каждой итерации существует согласованная пара:

```text
MooringDiscreteLoadTensionResult discreteTensions
MooringDiscreteLoadShapeResult   nextShape
```

`nextShape` построен непосредственно из `discreteTensions` этой же итерации.

## Что теряется сейчас

После цикла публикуется:

```text
MooringIterativeSolverResult.FinalShape
```

где `FinalShape` получен преобразованием последнего `nextShape` через `ToShapeResult(...)`.

Но сам последний:

```text
discreteTensions
nextShape
```

не входит в `MooringIterativeSolverResult`.

Поэтому после выхода из solver нельзя корректно взять:

```text
FinalShape geometry
+
pre-iterative TechnicalReportData.DiscreteLoadTensions
```

и назвать это equilibrium одной model state: это разные этапы расчёта.

## Решение boundary

Будущий implementation package должен добавить в `MooringIterativeSolverResult` nullable immutable результаты:

```text
MooringDiscreteLoadTensionResult? FinalDiscreteLoadTensions
MooringDiscreteLoadShapeResult?   FinalDiscreteLoadShape
```

Названия выше принимаются как целевые.

Эти значения должны быть присвоены **не после повторного расчёта**, а непосредственно из локальных `discreteTensions` и `nextShape` последней реально выполненной итерации.

## Same-iteration invariant

Если solver выполнил хотя бы одну итерацию:

```text
FinalDiscreteLoadTensions
FinalDiscreteLoadShape
FinalShape
```

должны относиться к одному и тому же последнему iteration number.

`FinalShape` остаётся результатом `ToShapeResult(...)` для того же `FinalDiscreteLoadShape`.

## Geometry identity

Для каждой строки последнего candidate shape:

```text
FinalDiscreteLoadShape.Rows[i].XOffsetM
FinalDiscreteLoadShape.Rows[i].ZDepthM
```

должны совпадать с соответствующими:

```text
FinalShape.Nodes[i].XOffsetM
FinalShape.Nodes[i].ZDepthM
```

с точностью прямого переноса данных.

Эта проверка является software-state identity, а не инженерным допуском.

## Tension identity

Для последней строки `MooringIterativeSolverIteration`:

```text
Rows[^1].TopDiscreteTensionKn
```

должно соответствовать:

```text
FinalDiscreteLoadTensions.TopDiscreteTensionKn
```

с arithmetic/software tolerance.

Это подтверждает, что опубликован именно force state последней итерации.

## Stop reason semantics

Retained state публикуется для фактически последней выполненной итерации независимо от причины остановки:

```text
Converged
MaxIterationsReached
GeometryNotClosed
DivergenceGuard
```

Он является диагностическим evidence того состояния, на котором solver остановился.

Наличие retained state не означает, что candidate прошёл gate.

## Invalid / no-iteration case

Если solver не выполнил ни одной итерации:

```text
FinalDiscreteLoadTensions = null
FinalDiscreteLoadShape = null
```

Не создавать synthetic empty result и не называть его final iteration state.

## Selected-shape distinction

Retained final state относится к iterative candidate.

Только если существующий gate выбирает `MooringIterativeSolver.FinalShape` как основную форму, этот same-iteration state одновременно соответствует selected candidate geometry.

Если gate отклоняет candidate и selected shape остаётся fallback:

```text
FinalDiscreteLoadTensions / FinalDiscreteLoadShape
```

остаются diagnostic evidence rejected candidate, а не selected-shape state.

Будущий residual обязан явно учитывать это различие в названии/статусе.

## Почему boundary не меняет физику

Implementation не должен менять:

```text
- порядок вычислений внутри iteration;
- projection;
- shape force equations;
- shape tension accumulation;
- BuildFeedbackTensions;
- discrete-load tension equations;
- discrete-load shape equations;
- AngleScale;
- MaxIterations;
- convergence criteria;
- divergence guard;
- stop reason;
- primary gate;
- selected X/Z.
```

Это retention существующих результатов, а не новый расчёт.

## Candidate B follow-up

После реализации boundary отдельная задача сможет построить final-iteration Candidate B из одной coherent family:

```text
TechnicalReportData.SequencePositions
+ IterativeSolver.FinalDiscreteLoadTensions
+ IterativeSolver.FinalDiscreteLoadShape
```

с существующей source-backed формой:

```text
R_node = -T_above*t_above
         +T_below*t_below
         +F_node
```

Но этот будущий residual остаётся отдельным PR и не входит в boundary implementation.

## Report / UI semantics

Этот boundary сам по себе не должен добавлять пользовательскую таблицу.

Он предназначен для validation/read-model evidence.

PDF и 2D продолжают читать только существующий selected X/Z boundary.

## Validation implementation

Минимальные проверки будущего implementation PR:

```text
1. invalid/no-iteration -> оба retained results null;
2. одна итерация -> retained state присутствует;
3. FinalDiscreteLoadShape row count == FinalShape node count;
4. X/Z retained candidate rows == FinalShape nodes;
5. FinalDiscreteLoadTensions.TopDiscreteTensionKn == Rows[^1].TopDiscreteTensionKn;
6. converged case сохраняет same-iteration state;
7. non-converged/max-iteration case также сохраняет actual last state;
8. five canonical engineering scenarios остаются с прежним golden baseline.
```

## Не входит в scope

```text
- новый equilibrium residual;
- engineering threshold;
- solver convergence residual;
- gate condition;
- CalculationResult.Verdict;
- anchor/seabed reaction;
- weak-link/WLL;
- 0.20 m segmentation;
- signed WeightWaterKg semantics;
- JSON/DTO;
- 2D/PDF physics;
- 3D.
```

## Следующий допустимый implementation package

После merge этой отметки:

```text
one focused MooringIterativeSolverResult retention PR
```

Ожидаемый production diff:

```text
- расширить MooringIterativeSolverResult двумя nullable полями;
- запоминать lastDiscreteTensions / lastDiscreteShape в существующем loop;
- вернуть их без повторного Build;
- обновить Empty(...) null-значениями;
- добавить focused validation.
```

Никаких других physics/solver changes в том же PR.

## Merge gate

Documentation PR и последующий implementation PR могут merge только когда exact head имеет:

```text
.NET Build: success
Selected Shape Consumer Scan: success
Report Store Consumer Scan: success
```

Committed five-scenario golden baseline не переписывается ради этой границы.
