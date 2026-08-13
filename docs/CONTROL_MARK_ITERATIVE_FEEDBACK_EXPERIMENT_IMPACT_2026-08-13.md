# Контрольная отметка: impact genuine iterative feedback coupling experiment

Дата: 2026-08-13  
Issue: #403  
Experimental PR: #405 — closed without merge  
Scope: validation evidence / decision record only

## Цель

Зафиксировать результат controlled physics experiment после документации:

```text
docs/CONTROL_MARK_ITERATIVE_FEEDBACK_CUT_STATE_2026-08-13.md
```

и объяснить, почему первая реализация genuine feedback coupling **не допускается к merge** несмотря на корректность cut-state contract.

Эта отметка не меняет production code, solver, golden baseline или selected X/Z.

## Stable main

Эксперимент выполнялся от проверенного `main`:

```text
40d49587bf086caf5aa5576fd56f95ffda167d3a
```

Production experiment из PR #405 не был merged.

## Что менял experiment

В экспериментальной ветке supplied `SegmentTensionRow` становился authoritative distributed cut-state в:

```text
MooringDiscreteLoadTensionAnalyzer
```

Вместо повторного накопления базовых `CalculationResult.SegmentRows` использовалось:

```text
H_dist = supplied.CumulativeHorizontalForceN
V_dist = supplied.CumulativeVerticalForceN
```

и существующие point loads добавлялись отдельно:

```text
H_next = H_dist + H_point
V_next = V_dist + V_point
```

где:

```text
H_point = Σ CurrentForceN
V_point = Σ WeightWaterKg * g
```

для loads на/ниже того же segment start-cut.

Predicate ownership:

```text
load.PositionAlongLineM >= segment.StartLengthM
```

не менялся.

Signed `WeightWaterKg` не нормализовался через `Abs`.

## Focused contract validation

В experiment была добавлена отдельная:

```text
IterativeFeedbackCouplingRegression
```

Она прошла до запуска historical golden verifier.

Проверены:

```text
1. pre-iterative base distributed + point-load identity;
2. supplied artificial H feedback shift проходит в candidate-driving cumulative H;
3. no-point-load case воспроизводит supplied distributed H/V;
4. buoyant point load сохраняет отрицательный signed Z;
5. supplied local segment force provenance используется без повторного base-force accumulation.
```

Production C# experiment head собирался:

```text
0 Warning(s)
0 Error(s)
```

Также успешно проходили:

```text
Selected Shape Consumer Scan
Report Store Consumer Scan
```

Следовательно обнаруженный historical impact не объясняется compile/architecture failure: genuine coupling действительно достиг candidate-driving state.

## Первый exact experimental head

Первый evidence head:

```text
b39357a61ac2c3d6ef7656e847c4eb59bd12d91e
```

`.NET Build` дошёл до committed five-scenario golden verifier и остановился на historical mismatch.

Первое зафиксированное изменение:

```text
Scenarios[1].SelectedHorizontalOffsetM
22.904164818523228
->
22.919104847399282 m
```

Golden baseline не изменялся.

## Full golden diff evidence

Для полного анализа использовался временный validation helper, который:

```text
- создавал actual baseline только во временном runner file;
- сравнивал его с committed engineering-baseline.json;
- печатал все differences;
- не записывал новый committed baseline.
```

Exact evidence head:

```text
f0897faf6a075ac9bc696172db8e7f0970a90370
```

Результат:

```text
DifferenceCount = 60
```

То есть изменение feedback coupling затрагивает существенную часть historical solver/read-model outputs, а не один изолированный scalar.

## Scenario 0 — vertical-zero-current

Для первого canonical limiting case golden differences не обнаружены.

Это важный контроль:

```text
zero horizontal current
vertical geometry
```

не был нарушен самой заменой cut-state source.

Но отсутствие изменений в одном limiting case недостаточно для разрешения solver merge.

## Scenario 1 — uniform-current-slack-line

Сценарий остаётся сходящимся, но изменяются selected geometry и tensions.

Зафиксировано, например:

```text
SelectedHorizontalOffsetM:
22.904164818523228
->
22.919104847399282 m
```

Также golden diff содержит изменения:

```text
SelectedAngleSumDeg
SelectedTensionSumKn
SelectedXSumM
SelectedZSumM
нескольких SelectedSamples X/Z/angle/tension
```

Следовательно coupling достигает распределённой геометрии, а не только report metadata.

## Scenario 2 — buoyant-line

Это наиболее серьёзное изменение поведения.

Golden diff:

```text
IterativeConverged:
true -> false

IterativeStopReason:
Converged -> DivergenceGuard

SelectedSource:
MooringIterativeSolver.FinalShape
->
MooringShapeSolver fallback
```

Изменяются также selected geometry/tension sample values.

Это означает, что при существующей solver/angle/feedback architecture simple authoritative feedback coupling нарушает historical convergence в buoyant distributed-line case.

### Почему это нельзя автоматически считать ошибкой только нового cut-state

Buoyant line имеет:

```text
WeightWaterKg < 0
```

при этом несколько существующих angle paths используют:

```text
atan2(|H|, |V|)
```

и geometric angle reconstruction.

Поэтому прежде чем менять coupling, необходимо отдельно проверить:

```text
- направление signed tension vector при смене знака V;
- соответствие unsigned display/shape angle signed equilibrium state;
- возможную потерю направления при Abs(V);
- необходимость отдельного signed tangent/force representation внутри solver.
```

Нельзя лечить divergence увеличением порога или отключением signed buoyancy.

## Scenario 3 — discrete-payload

Golden diff показывает:

```text
IterativeConverged:
true -> false

IterativeStopReason:
Converged -> MaxIterationsReached
```

При этом изменяются:

```text
SelectedHorizontalOffsetM
SelectedAngleSumDeg
SelectedTensionSumKn
SelectedXSumM
SelectedZSumM
selected sample X/Z/angle/tension values
```

Текущий production limit:

```text
MaxIterations = 4
```

не менялся в experiment.

### Интерпретация

Из `MaxIterationsReached` нельзя заключить ни:

```text
"coupling неверен"
```

ни:

```text
"надо просто увеличить MaxIterations"
```

без отдельного convergence study.

Нужно проверить trajectory residual/offset/node delta при большем iteration budget **только в validation**, не изменяя production limit.

## Scenario 4 — depth-varying-current-profile

Сценарий остаётся сходящимся, но selected X/Z и tensions меняются.

Golden diff включает:

```text
SelectedHorizontalOffsetM
SelectedAngleSumDeg
SelectedTensionSumKn
SelectedXSumM
SelectedZSumM
selected sample X/Z/angle/tension values
```

Следовательно shape-dependent line drag действительно меняет дальнейший candidate state при profile current.

Это ожидаемо для genuine feedback, но величина и правильность изменения должны быть проверены reference model.

## Главный вывод experiment

Experiment доказал две разные вещи.

### 1. Предыдущий feedback path действительно был disconnected

После использования supplied shape-based distributed cut H/V candidate/read-model outputs реально изменились.

Следовательно прежний fixed-point pattern #400 объяснялся code path, а не универсальной физической сходимостью после первой candidate geometry.

### 2. Простое включение feedback не готово к production

Historical impact включает:

```text
- 60 golden differences;
- изменение selected X/Z/tensions;
- DivergenceGuard для buoyant-line;
- MaxIterationsReached для discrete-payload.
```

Поэтому изменение не может быть принято как обычный bug fix.

## Решение по PR #405

PR #405 закрыт:

```text
WITHOUT MERGE
```

Никакой production code из экспериментальной ветки не переносится в `main` на этом этапе.

Committed golden baseline не обновляется.

## Почему нельзя обновить golden baseline сейчас

Golden mismatch здесь является evidence изменения физического solver behavior.

Автоматическое принятие нового baseline означало бы:

```text
"новые значения правильные, потому что новый код их выдал"
```

что является circular validation.

Перед любой intentional baseline migration необходимо независимое подтверждение физического результата.

## Следующая validation hierarchy

### A. Signed distributed-force / angle audit

Особенно для:

```text
WeightWaterKg < 0
```

проверить:

```text
- signed H/V representation;
- signed tangent orientation;
- conversion to angle;
- where Abs(H)/Abs(V) is display-only and where it drives geometry;
- whether a buoyant segment requires an oriented angle beyond 0..90° magnitude convention.
```

### B. Iteration-budget convergence study

Validation-only запуск с большим числом итераций для:

```text
uniform-current slack line
buoyant line
discrete payload
depth-varying profile
```

Записать по каждой итерации:

```text
InputX
OutputX
ΔX
max node delta
geometry residual
shape line force
top shape tension
top discrete tension
Candidate B R/R_rel where available
```

Production `MaxIterations=4` пока не менять.

### C. Analytical limiting cases

Минимум:

```text
vertical heavy line, zero current
vertical buoyant line where geometry/sign convention is analytically known
straight/slack line under uniform planar current within model assumptions
single internal point load with known vector balance
```

### D. Independent quasi-static/reference solver

Для representative cases сравнить:

```text
X/Z shape
top tension
selected internal-node tensions
horizontal offset
signed force balance
```

с независимой quasi-static implementation/tool, а не с текущим BuoyCalc baseline.

### E. Mesh sensitivity

Проверить sensitivity вокруг production segmentation:

```text
0.20 m
```

только validation runs.

Production segmentation этой работой не менять.

## Candidate B policy

Ненулевой Candidate B residual сам по себе пока не является engineering failure.

Даже если future coupling уменьшает `R_rel`, это недостаточно для merge без reference validation.

Candidate B остаётся:

```text
INFO / diagnostic only.
```

## Gate / verdict policy

До завершения validation запрещено добавлять feedback experiment или Candidate B в:

```text
- convergence criterion;
- divergence guard;
- stop reason;
- MooringPrimaryShapeGate;
- selected X/Z decision;
- CalculationResult.Verdict;
- anchor reserve;
- weak-link/WLL.
```

## Source policy

Primary signed flexible-line mechanics остаётся Berteaux 1979 / source control mark #387.

Следующая работа не должна подменять source-backed vector equilibrium удобной формой текущих unsigned display angles.

## Non-goals этой отметки

```text
- solver implementation;
- новый MaxIterations;
- изменение tolerance;
- изменение Candidate B;
- golden baseline update;
- anchor/seabed contact physics;
- 2D/PDF physics;
- JSON/DTO;
- 3D.
```

## Следующий допустимый шаг

Не новый production coupling PR.

Сначала:

```text
Physics validation: signed angle/orientation audit for buoyant and heavy line states
+
validation-only convergence study with expanded iteration budget.
```

После этого можно решить, требуется ли:

```text
- signed internal solver angle representation;
- другой feedback state transform;
- damping/relaxation;
- larger iteration budget;
- или иной nonlinear equilibrium strategy.
```

Ни один из этих вариантов заранее не выбран.

## Merge gate этой документации

Final docs-only PR должен иметь:

```text
.NET Build: success
Selected Shape Consumer Scan: success
Report Store Consumer Scan: success
```

и не содержать experimental production/temporary validation code.
