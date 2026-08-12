# Контрольная отметка: Physics RFC force-to-shape consistency

Дата: 2026-08-12  
Issue: #373  
Scope: documentation only

## Причина

После завершения application calculation-run boundary (#365) следующий этап дорожной карты — намеренная физическая валидация solver.

Текущий итерационный X/Z-кандидат может выполнить действующие критерии геометрической сходимости без отдельной проверки того, что геометрическое направление каждого участка линии согласовано с направлением рассчитанного результирующего натяжения.

Это принципиально отличается от уже реализованной проверки #223:

```text
#223:
Σ локальных сил/весов линии
vs
накопленные компоненты верхнего сечения
```

Такая проверка подтверждает корректность накопления величин, но не подтверждает соответствие между силовым состоянием и X/Z-формой.

## Подтверждённая текущая цепочка

```text
MooringShapeProjection
  -> фактические ΔX / ΔZ и геометрический угол сегмента

MooringShapeForceAnalyzer
  -> нормальная к фактической X/Z-ориентации скорость
  -> shape-based drag

MooringShapeTensionAnalyzer
  -> накопленные H / V
  -> shape-based tension / force-derived angle

MooringDiscreteLoadTensionAnalyzer
  -> H / V с дискретными connector/payload loads
  -> discrete tension / force-derived angle

MooringDiscreteLoadShapeBuilder
  -> применяет общий AngleScale
  -> строит новую X/Z-геометрию

MooringIterativeSolver
  -> проверяет Δoffset, maxΔnode, vertical geometry residual, divergence

MooringPrimaryShapeGate
  -> принимает/rejects candidate по solver convergence/stop reason
```

В действующей цепочке отсутствует самостоятельная force↔shape residual, входящая в критерий сходимости или gate.

## Важное текущее ограничение

`MooringDiscreteLoadShapeBuilder` использует:

```text
UsedAngle = ScaleAngle(DiscreteAngleFromVerticalDeg, AngleScale, ...)
```

то есть геометрический угол может отличаться от force-derived angle ради замыкания глубины.

Следовательно:

```text
GeometryClosed != доказанное force equilibrium
```

и текущий `MooringPrimaryShapeGate` не должен интерпретироваться как физическая проверка полного равновесия.

## Граница RFC #373

Первый допустимый production-шаг — только диагностический слой.

Запрещено на первом шаге:

```text
- менять solver equations;
- менять MooringDiscreteLoadShapeBuilder.AngleScale;
- менять MooringIterativeSolver convergence criteria;
- менять MooringPrimaryShapeGate;
- менять CalculationResult.Verdict;
- менять selected X/Z;
- менять anchor / weak-link calculations;
- менять 2D / PDF geometry;
- менять JSON / DTO;
- добавлять 3D.
```

## Предлагаемая диагностическая величина

Для сегмента `i` фактическая X/Z-касательная:

```text
dx_i = x_(i+1) - x_i
dz_i = z_(i+1) - z_i
Lgeom_i = sqrt(dx_i^2 + dz_i^2)

t_i = (dx_i, dz_i) / Lgeom_i
```

После явного приведения силовых компонент к той же системе координат X/Z:

```text
q_i = normalized force-direction vector
```

предпочтительная первичная невязка:

```text
r_perp_i = abs(t_x_i * q_z_i - t_z_i * q_x_i)
```

Для единичных векторов:

```text
r_perp = |sin(Δθ)|
0 -> коллинеарно
1 -> перпендикулярно
```

Угол может выводиться как интерпретационная величина:

```text
r_angle = asin(clamp(r_perp, 0, 1))
```

На первой стадии это только proposal из RFC, а не утверждённая новая физика production solver.

## Система знаков — обязательный unresolved пункт до реализации

Сейчас используются разные представления вертикального направления:

```text
MooringShapePoint.ZDepthM:
  +Z = вниз

segment / shape tension accumulation:
  signed WeightWaterKg * g

MooringVectorBalance external ledger:
  Fz = -WeightWaterKg * g
  +Fz = вверх
```

Поэтому diagnostic implementation обязана сначала привести H/V к единой X/Z-конвенции.

Критически важно:

```text
signed WeightWaterKg сохраняется;
negative buoyant values не превращаются в abs(...);
существующие display angles с abs(H)/abs(V) не используются как авторитетный signed residual input.
```

## Degenerate cases

Если:

```text
Lgeom -> 0
или
T -> 0
```

результат должен быть явным:

```text
NotApplicable / Indeterminate
```

а не искусственным `residual = 0`.

## Будущая более сильная проверка

Отдельный следующий физический этап может проверять равновесие внутреннего узла:

```text
T_out - T_in + F_node = 0
```

Но он не входит в первый production-шаг #373, пока не определены и не провалидированы:

```text
- направление входящего/исходящего tension vector;
- exact cut/segment mapping;
- buoy boundary force;
- anchor/seabed reaction;
- touchdown/contact semantics.
```

## Validation plan до engineering tolerance

До назначения pass/fail tolerance требуется:

```text
1. пять существующих deterministic golden scenarios;
2. zero-current / vertical analytical case;
3. uniform-current continuous-line case;
4. controlled single discrete load;
5. signed buoyant WeightWaterKg < 0 case;
6. near-zero tension / degenerate-vector case;
7. sensitivity around existing 0.20 m segmentation target;
8. independent quasi-static/reference solver comparison;
9. сопоставление с проектным источником Берто с точными страницами/разделами.
```

Рекомендуемые метрики baseline:

```text
max r_perp
RMS r_perp
max interpreted |Δθ|
worst segment/location
AngleScale
solver stop reason
selected-shape source
```

## Tolerance policy

На этой контрольной отметке инженерный tolerance намеренно не назначается.

До появления evidence:

```text
severity = INFO / diagnostic
solver stop = unchanged
gate = unchanged
verdict = unchanged
```

Запрещено выбирать tolerance только так, чтобы текущие сценарии стали зелёными.

## Источники и reference-validation

Для RFC определены независимые reference directions:

```text
- Goodman et al. / National Data Buoy Center, 1972:
  Static and dynamic analysis of a moored buoy system.
  Модель связывает конфигурацию постановки и внутренние натяжения.

- Hall et al. / NREL-DOE, 2021:
  MoorPy (Quasi-Static Mooring Analysis in Python),
  DOI 10.11578/dc.20210726.1.
  Квазистатическая reference implementation рассчитывает распределённые положение и натяжение линии.

- H. O. Berteaux / Г. О. Берто:
  Buoy Engineering / Океанографические буи.
  Основной источник проекта; точная chapter/page mapping обязательна до production physics change.
```

## Инварианты Phase A

```text
- solver physics unchanged;
- CalculationResult numeric values unchanged;
- selected X/Z unchanged;
- iteration count / stop reason unchanged;
- gate decision unchanged;
- signed WeightWaterKg preserved;
- target segment length = 0.20 m preserved;
- segment count remains unlimited;
- 2D/PDF remain passive read-model consumers;
- no 3D.
```

## Safety gate

Любой будущий production PR по Phase A допускается только отдельно от этой documentation-only отметки и только после успешных exact-head:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```

Изменение convergence/gate после диагностического этапа требует отдельного явного Physics RFC decision и reviewed validation evidence.
