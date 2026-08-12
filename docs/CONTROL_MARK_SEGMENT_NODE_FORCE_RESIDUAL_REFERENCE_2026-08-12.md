# Контрольная отметка: reference boundary для segment/node force residual

Дата: 2026-08-12  
Issue: #374  
Scope: documentation only

## Цель

Зафиксировать физический смысл, source hierarchy и точное соответствие существующих строк сил геометрическим сегментам до добавления первой force↔shape диагностики.

Эта отметка продолжает `CONTROL_MARK_FORCE_SHAPE_CONSISTENCY_RFC_2026-08-12.md`, но не меняет solver и не утверждает, что текущая X/Z-форма является решением полного нелинейного равновесия.

## Source hierarchy

### Уровень 1 — основной источник проекта

```text
H. O. Berteaux / Г. О. Берто
Buoy Engineering / Океанографические буи
Wiley, 1976 / Судостроение, Ленинград, 1979
```

Для русского издания подтверждено:

```text
Часть II. МЕХАНИКА БУЙРЕПОВ — стр. 32
Глава 2. Статика буйрепов — стр. 32
```

Основной locus для текущей одноякорной planar X/Z постановки — глава 2, §2.1, особенно материал о гибких буйрепах и течении.

Точная страница/рисунок/номер уравнения элементарного статического равновесия внутри §2.1 пока должны быть подтверждены непосредственно по скану. До этого запрещено ссылаться на конкретную формулу как на дословную формулу Берто.

### Уровень 2 — Berteaux-attributed secondary evidence

Bayraktar & Kükner, ISOPE 2008, pp. 282–288, используют Berteaux (1976) как источник элементарной модели троса.

В free-body элементарного участка участвуют:

```text
T
T + dT
dφ
D ds — normal drag
F ds — tangential drag/friction
P ds — immersed/net weight
```

Статическое условие формулируется как векторное равновесие с разложением на normal/tangential компоненты.

Следовательно, source-backed физический объект — равновесие элементарного участка троса, а не только совпадение двух отображаемых углов.

### Уровень 3 — современный независимый cross-check

Современный обзор методов статического равновесия морских тросов приводит normal/tangent equations вида:

```text
T* dφ = (w cosφ + D) ds
dT*  = (w sinφ - F) ds
```

с apparent tension и распределёнными нагрузками.

Это используется только как cross-check структуры уравнений и не заменяет primary Berteaux citation.

### Уровень 4 — dynamic equation static limit

В поздней литературе уравнение гибкого буйрепа, приписываемое Berteaux, записывается как векторный баланс:

```text
m dV/dt = hydrodynamic forces + dT/ds + net weight
```

В статическом пределе time/inertia terms исчезают. Это дополнительно подтверждает, что будущий более сильный residual должен быть силовым векторным балансом.

## Точная семантика текущего BuoyCalc

### SegmentTensionRow

`SegmentTensionAnalyzer` обходит сегменты снизу вверх.

Для сегмента `i` выполняется:

```text
H_i = H_below + CurrentForce_i
V_i = V_below + WeightWater_i * g
T_i = sqrt(H_i² + V_i²)
```

и только затем формируется `SegmentTensionRow i`.

Поэтому строка `i` соответствует cumulative force/tension на:

```text
TOP / START CUT сегмента i
```

Она включает распределённую нагрузку самого сегмента `i` и всё, что находится ниже него.

### Геометрия X/Z

`MooringShapeSolver.BuildNodes` создаёт:

```text
node[0] = верхний конец / буй
node[i] = узел после прохождения segment i
```

Следовательно:

```text
geometry(segment i) = node[i-1] -> node[i]
```

Вектор геометрической касательной:

```text
dx_i = x_i - x_(i-1)
dz_i = z_i - z_(i-1)
L_i  = sqrt(dx_i² + dz_i²)
```

### Важная ловушка отображения

Созданный `node[i]` получает:

```text
SegmentTensionKn = tensionRow[i].TensionKn
```

но это только attachment результата к узлу для модели/отображения.

Нельзя делать вывод:

```text
node[i].SegmentTensionKn == end-cut tension физического сегмента i
```

потому что исходная `SegmentTensionRow i` семантически является start-cut cumulative state.

Будущий residual должен использовать явный segment/cut mapping, а не положение поля в record `MooringShapePoint`.

## Bottom/end cut непрерывного сегмента

Для внутреннего участка без точечной нагрузки непосредственно на границе:

```text
start-cut state segment i = row i
bottom/end-cut state segment i ≈ start-cut state segment i+1
```

поскольку row `i+1` содержит нагрузки сегмента `i+1` и всего, что ниже, но уже не содержит распределённую нагрузку segment `i`.

Это только структурная связь текущего массива строк.

Она не является готовым signed node-equilibrium contract, потому что необходимо отдельно определить:

```text
- направление tension vector на каждой стороне cut;
- знак X и Z;
- point-load ownership на junction;
- terminal boundary forces.
```

## Дискретные нагрузки

`MooringDiscreteLoadTensionAnalyzer` включает дискретные нагрузки по условию:

```text
PositionAlongLineM >= segment.StartLengthM
```

Для каждого segment row также строится start-cut cumulative state.

Перед signed node balance необходимо документировать, кому принадлежит discrete load, если:

```text
PositionAlongLineM == segment boundary
```

Иначе одна и та же физическая point load может быть неверно интерпретирована при сравнении соседних cut states.

## Почему текущая геометрия не равна force equilibrium

`MooringShapeSolver` и `MooringDiscreteLoadShapeBuilder` используют force-derived angle как вход, но затем изменяют его ради геометрического замыкания.

Fallback solver содержит:

```text
geometricAngle = acos(Depth / LineLength)
baseAngle = max(abs(tensionAngle), geometricAngle)
usedAngle = clamp(baseAngle * AngleScale, 0, 89°)
```

Discrete-load builder также применяет общий `AngleScale`.

Поэтому:

```text
used geometric tangent != necessarily force-derived direction
```

и:

```text
GeometryClosed != full static equilibrium solved
```

## Candidate A — разрешённый первый production scope

Candidate A из #374 сохраняется, но его название и интерпретация фиксируются как:

```text
force-direction / X-Z-tangent consistency proxy
```

а не:

```text
segment equilibrium residual
```

Он использует уже рассчитанные magnitude semantics текущей модели:

```text
H = abs(CumulativeShapeHorizontalForceN)
V = abs(CumulativeVerticalForceN)
T = sqrt(H² + V²)

tx = abs(dx) / L
tz = abs(dz) / L

H_geom = T * tx
V_geom = T * tz

R_H   = H - H_geom
R_V   = V - V_geom
R     = sqrt(R_H² + R_V²)
R_rel = R / max(T, 1 N)
Δθ    = abs(φ_geometry - θ_force)
```

### Физический смысл Candidate A

Он отвечает на узкий вопрос:

```text
Насколько направление существующего cumulative force state
отличается от фактической X/Z-касательной, построенной solver-ом?
```

Он не доказывает:

```text
- signed equilibrium узла;
- balance внешних/внутренних векторов;
- buoy boundary equilibrium;
- anchor reaction;
- seabed/contact equilibrium.
```

## Разрешённые результаты Phase A

Допустимо добавить additive read-model/diagnostic data:

```text
per-segment R_H [N]
per-segment R_V [N]
per-segment R [N]
per-segment R_rel [-]
per-segment Δθ [deg]
max R
max R_rel
max Δθ
worst segment number/source
Available / Indeterminate status
method note
```

Разрешено публиковать эти значения в техническом отчёте как INFO.

## Degenerate handling

Если:

```text
L <= numerical geometry floor
или
T <= numerical force floor
или
input/result non-finite
```

результат не должен становиться искусственным нулём.

Требуется явная семантика:

```text
Unavailable / Indeterminate / NotApplicable
```

с причиной.

## Software-validation tolerances

Численные критерии #374 разрешены только для synthetic/limiting identities:

```text
vertical zero-current:
  max R_rel <= 1e-8
  max Δθ <= 1e-6 deg

pure algebraic reconstruction identities:
  relative <= 1e-10
```

Это:

```text
software correctness tolerances
```

а не:

```text
engineering acceptance limits for arbitrary moorings
```

Для реальных сценариев до reference validation:

```text
severity = INFO
engineering threshold = TBD
```

## Candidate B — будущий source-backed equilibrium target

Более сильная физическая проверка должна использовать signed vectors.

Концептуально:

```text
R_segment/node = T_out_vector - T_in_vector + F_external
```

с эквивалентной формой после окончательного выбора ориентации.

Для распределённого элемента это является дискретным аналогом элементарного векторного равновесия гибкого троса.

Но Candidate B заблокирован до явного контракта:

```text
1. signed tangent orientation;
2. start/end cut vector convention;
3. signed WeightWaterKg -> X/Z force convention;
4. distributed drag ownership;
5. exact discrete node ownership;
6. buoy boundary force/reaction;
7. anchor horizontal/vertical reaction;
8. seabed/touchdown/contact model.
```

## Validation sequence

Перед любым использованием residual в solver/gate:

```text
1. сохранить пять существующих golden scenarios;
2. vertical zero-current analytical case;
3. uniform-current continuous line;
4. controlled single point load;
5. signed buoyant line section;
6. degenerate near-zero tension case;
7. mesh sensitivity around production 0.20 m target;
8. independent quasi-static reference solver comparison;
9. direct primary Berteaux §2.1 page/equation citation.
```

## Запрещённые изменения в первом implementation PR

```text
- MooringShapeSolver equations;
- MooringDiscreteLoadShapeBuilder AngleScale;
- MooringIterativeSolver convergence criteria;
- MooringPrimaryShapeGate;
- selected X/Z;
- CalculationResult.Verdict;
- anchor sizing/reserve;
- weak-link/WLL calculations;
- signed WeightWaterKg behavior;
- 0.20 m target segmentation;
- project JSON/DTO;
- 2D/PDF geometry;
- 3D.
```

## Golden boundary

Пять существующих deterministic golden scenarios должны остаться неизменными.

Новая диагностика проверяется дополнительными assertions/results и не переписывает исторический baseline существующих инженерных результатов.

## Решение контрольной отметки

Следующий production micro-package после merge этой документации может реализовать только:

```text
Candidate A consistency proxy
+ additive result type
+ deterministic validation
```

без solver feedback.

Подключение residual к convergence/gate или переход к Candidate B требует отдельного Physics RFC decision и доказательной базы.

## Safety gate

Documentation PR и любой последующий implementation PR сливаются только после successful exact-head:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```
