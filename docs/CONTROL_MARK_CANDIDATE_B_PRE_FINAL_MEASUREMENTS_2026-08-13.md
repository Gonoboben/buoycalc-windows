# Контрольная отметка: Candidate B до и после iterative feedback

Дата: 2026-08-13  
Issue: #399  
PR measurement branch: #400  
Scope: validation evidence only

## Цель

Зафиксировать, как уже реализованный signed internal-node Candidate B изменяется между двумя согласованными состояниями модели:

```text
1. pre-iterative discrete-load candidate;
2. final iterative candidate.
```

Эта отметка не вводит engineering tolerance и не меняет solver.

## Provenance измерений

Измерения получены временным validation logger на draft PR #400.

Exact measurement head:

```text
42f6f974d74469818bdb6f9c20623ab2ddb48ea5
```

GitHub Actions:

```text
workflow: .NET Build
run: 31675031189
run number: 759
job: 94367638991
```

На этом head:

```text
.NET Build: success
Selected Shape Consumer Scan: success
Report Store Consumer Scan: success
Engineering regression verification: passed, 5 scenarios
```

Временный logger использовался только для печати уже рассчитанных read-model значений и iteration metadata. Он удаляется до merge этой контрольной отметки.

## Главный наблюдаемый результат

Во всех пяти контролируемых сценариях:

```text
pre-iterative Candidate B == final-iteration Candidate B
```

для выведенных signed residual полей:

```text
R_x
R_z
R
R_rel
T_above
T_below
F_node,x
F_node,z
```

Также во всех пяти сценариях:

```text
PreCandidateOffsetM == FinalShapeOffsetM
PreTopDiscreteTensionKn == FinalTopDiscreteTensionKn
```

Это наблюдение относится к проверенным сценариям и не утверждается как универсальное свойство всех возможных постановок.

## Сводная таблица residual

| Сценарий | s, м | Источников | R pre, Н | R final, Н | R_rel pre | R_rel final | Итераций | StopReason |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| heavy-payload-uniform-current | 30 | 1 | 55.20541432460539 | 55.20541432460539 | 0.14687524140319072 | 0.14687524140319072 | 2 | Converged |
| grouped-connector-payload-uniform-current | 30 | 2 | 55.913570300961894 | 55.913570300961894 | 0.13402718460963164 | 0.13402718460963164 | 2 | Converged |
| buoyant-payload-uniform-current | 30 | 1 | 118.73052292321702 | 118.73052292321702 | 1.3083993896371422 | 1.3083993896371422 | 2 | Converged |
| payload-depth-varying-current | 30 | 1 | 29.9482529248828 | 29.9482529248828 | 0.08145874638908397 | 0.08145874638908397 | 2 | Converged |
| vertical-zero-current-internal-payload | 25 | 1 | 0 | 0 | 0 | 0 | 1 | Converged |

Большое значение `R_rel > 1` для buoyant payload является измеренным INFO-значением. Оно не означает автоматически failure: engineering acceptance threshold для Candidate B не утверждён.

## 1. Heavy payload / uniform current

Node load:

```text
s = 30 m
F_node,x = 6.40625 N
F_node,z = 342.00691874999995 N
```

Pre и final одинаковы:

```text
T_above = 375.8660329487371 N
T_below = 80.6897176001428 N
R_x = -26.130926383507884 N
R_z = 48.62933741159793 N
R = 55.20541432460539 N
R_rel = 0.14687524140319072
```

Geometry / top discrete state:

```text
Fallback X = 22.90416481852323 m
Pre candidate X = 19.171876524737762 m
Final shape X = 19.171876524737762 m

Pre top discrete tension = 0.4331079195414456 kN
Final top discrete tension = 0.4331079195414456 kN
```

Iteration trace:

```text
Iteration 1:
  InputX = 22.90416481852323
  OutputX = 19.171876524737762
  DeltaX = -3.7322882937854693
  ShapeLineForceN = 139.79504808082254
  TopShapeTensionKn = 0.14983927919824713
  TopDiscreteTensionKn = 0.4331079195414456
  MaxNodeDeltaM = 8.134993255343948
  GeometryResidualM = 0
  StopReason = Continue

Iteration 2:
  InputX = 19.171876524737762
  OutputX = 19.171876524737762
  DeltaX = 0
  ShapeLineForceN = 141.06875408824067
  TopShapeTensionKn = 0.15102830033718553
  TopDiscreteTensionKn = 0.4331079195414456
  MaxNodeDeltaM = 0
  GeometryResidualM = 0
  StopReason = Converged
```

## 2. Grouped connector + payload / uniform current

Same-s grouping подтверждено одним mechanical node:

```text
s = 30 m
SourceElementCount = 2
F_node,x = 7.6875 N
F_node,z = 384.00389737499995 N
```

Pre и final:

```text
T_above = 417.18081644269466 N
T_below = 80.6897176001428 N
R_x = -26.47017378837151 N
R_z = 49.250961852678756 N
R = 55.913570300961894 N
R_rel = 0.13402718460963164
```

State:

```text
Fallback X = 22.90416481852323 m
Pre candidate X = Final shape X = 18.906914306513368 m
Pre/final top discrete tension = 0.4722864782103005 kN
```

Iteration 1 меняет X на `-3.997250512009863 m`; iteration 2 оставляет X и top discrete tension без изменений и завершается `Converged`.

При этом intermediate values меняются:

```text
ShapeLineForceN: 139.79504808082254 -> 141.0632656876233
TopShapeTensionKn: 0.14983927919824713 -> 0.1510231738813206
```

## 3. Buoyant payload / uniform current

Этот сценарий особенно важен для signed Z semantics:

```text
F_node,x = 2.46 N
F_node,z = -90.7115125 N
```

Отрицательный `WeightWaterKg` корректно даёт отрицательную внешнюю Z-силу в системе `+Z вниз`.

Pre и final:

```text
T_above = 83.9673670608867 N
T_below = 54.97003639611959 N
R_x = -5.467323689097307 N
R_z = -118.604575988025 N
R = 118.73052292321702 N
R_rel = 1.3083993896371422
```

State:

```text
Fallback X = 22.9160110916542 m
Pre candidate X = Final shape X = 22.69862086799305 m
Pre/final top discrete tension = 0.11664855776274718 kN
```

Iteration trace:

```text
iteration 1 DeltaX = -0.2173902236611518 m
iteration 2 DeltaX = 0
```

Intermediate shape quantities изменяются немного, но discrete candidate state и Candidate B не изменяются.

## 4. Internal payload / depth-varying current

Point load:

```text
F_node,x = 12.556249999999999 N
F_node,z = 342.00691874999995 N
```

Pre и final:

```text
T_above = 367.64932253973495 N
T_below = 29.38156539016781 N
R_x = -29.2705506817571 N
R_z = 6.335038755954088 N
R = 29.9482529248828 N
R_rel = 0.08145874638908397
```

State:

```text
Fallback X = 21.353728012529356 m
Pre candidate X = Final shape X = 20.225706165871532 m
Pre/final top discrete tension = 0.4185085748369617 kN
```

Здесь изменение intermediate shape state особенно заметно:

```text
ShapeLineForceN:
91.3965659527439 -> 105.09562229732974 N

TopShapeTensionKn:
0.1061248622646216 -> 0.11812808281180916 kN
```

но:

```text
TopDiscreteTensionKn остается 0.4185085748369617 kN
candidate X остается 20.225706165871532 m
Candidate B остается без изменения
```

## 5. Vertical / zero current / internal payload

Limiting case:

```text
F_node,x = 0
F_node,z = 176.02936749999998 N
T_above = 200.54599250000024 N
T_below = 24.51662500000026 N
R_x = 0
R_z = 0
R = 0
R_rel = 0
```

Все offsets равны нулю.

Solver сходится за одну итерацию:

```text
InputX = OutputX = 0
ShapeLineForceN = 0
TopDiscreteTensionKn = 0.22506261750000014
MaxNodeDeltaM = 0
GeometryResidualM = 0
```

Это подтверждает ожидаемое поведение signed free body в простом вертикальном случае с внутренней точечной нагрузкой.

## Наблюдение о текущем iterative feedback

В четырёх nontrivial сценариях с горизонтальным течением последовательность выглядит одинаково по структуре:

```text
fallback shape
 -> iteration 1 строит discrete candidate
 -> candidate X становится равен independently built pre-iterative candidate X
 -> iteration 2 меняет некоторые shape-force / shape-tension intermediate values
 -> TopDiscreteTensionKn остается тем же
 -> discrete candidate X/Z остается тем же
 -> DeltaX = 0
 -> MaxNodeDelta = 0
 -> Converged
```

Следовательно в этих пяти сценариях текущий iterative feedback:

```text
не уменьшает
и не увеличивает
Candidate-B signed node residual.
```

Он оставляет Candidate B тем же, потому что final discrete candidate state совпадает с pre-iterative discrete candidate state.

## Что из этого НЕ следует

Эти измерения не доказывают:

```text
- что Candidate B equation неверна;
- что iterative solver должен немедленно использовать Candidate B как convergence target;
- что любой ненулевой R является failure;
- что одинаковый pre/final residual будет во всех проектах;
- что нужно менять gate или verdict;
- что нужен новый tolerance.
```

Вертикальный zero-current случай имеет точный `R=0`, а signed buoyant load сохраняет правильный отрицательный знак, поэтому базовые limiting/sign checks остаются согласованными с source-backed механикой.

## Следующий code-backed вопрос

До любой физической модификации solver необходимо выяснить, почему:

```text
ShapeLineForceN
и
TopShapeTensionKn
```

могут изменяться между iteration 1 и iteration 2, тогда как:

```text
TopDiscreteTensionKn
MooringDiscreteLoadShape
Candidate B
```

остаются неизменными.

Следующая задача должна быть audit/documentation-first:

```text
trace BuildFeedbackTensions
 -> MooringDiscreteLoadTensionAnalyzer
 -> MooringDiscreteLoadShapeBuilder
```

и точно определить, какие поля feedback реально участвуют в расчёте нового cumulative discrete state, а какие используются только как baseline/comparison metadata.

Нельзя заранее считать, что feedback «игнорируется», пока это не доказано по коду.

## Engineering tolerance policy

По результатам этих измерений engineering acceptance threshold НЕ задаётся.

Candidate B остается:

```text
INFO / diagnostic only.
```

Запрещено использовать эту контрольную отметку как основание для автоматического изменения:

```text
- iterative convergence;
- stop reason;
- MooringPrimaryShapeGate;
- selected X/Z;
- CalculationResult.Verdict;
- anchor reserve;
- weak-link/WLL.
```

## Отдельное техническое замечание: nullable warning

Measurement run сохранил существующий после report PR #398 warning:

```text
CS8604: Possible null reference argument
TechnicalReportMarkdownBuilder -> TechnicalReportMarkdownSectionBridge.Append(...)
```

Build при этом завершился:

```text
1 Warning(s)
0 Error(s)
```

Этот warning не связан с physics measurement и должен быть устранён отдельным маленьким report-only PR, без изменения Candidate B.

## Финальный scope этого PR

До merge:

```text
- временный CandidateBMeasurementEvidence.cs удаляется;
- вызов logger из ValidationEntryPoint удаляется;
- production code не меняется;
- final diff относительно main остается documentation-only;
- все три exact-head CI должны быть success.
```
