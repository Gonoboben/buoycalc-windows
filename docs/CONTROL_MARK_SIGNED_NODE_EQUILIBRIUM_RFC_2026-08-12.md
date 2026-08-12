# Контрольная отметка: signed segment/node equilibrium residual

Дата: 2026-08-12  
Issue: #383  
Scope: documentation/source-validation only

## Цель

Зафиксировать физическую и программную границу следующего этапа после Candidate A.

Candidate A (`MooringForceShapeConsistencyResult`) уже существует и показывает magnitude-only согласованность:

```text
existing cumulative shape-force direction
vs
existing X/Z tangent
```

Он не является signed equilibrium residual.

Candidate B должен перейти к signed vector balance, но не может быть реализован до явного определения cut/tangent/load ownership и подтверждения первичного источника Берто.

## Source hierarchy

### 1. Primary project source

```text
H. O. Berteaux / Г. О. Берто
Buoy Engineering, Wiley, 1976
Океанографические буи, Судостроение, 1979
Часть II — Механика буйрепов
Глава 2 — Статика буйрепов
§2.1 — одноточечная якорная постановка, гибкие буйрепы
```

Точная русская страница/рисунок/номер уравнения внутри §2.1 остаётся обязательным blocker перед production implementation Candidate B.

Локальный проектный DjVu image-only и не содержит текстового слоя. Формулы из OCR не принимаются как первичное доказательство, если можно получить/прочитать оригинальную страницу.

### 2. Berteaux-attributed element free body

Peer-reviewed ISOPE 2008 work, прямо ссылающаяся на Berteaux (1976), описывает элемент кабеля `ds` с силами:

```text
T
T + dT
D ds   normal pressure drag
F ds   tangential friction drag
P ds   immersed/net weight
dφ     изменение угла на элементе
```

и формулирует статическое условие как нулевую векторную сумму с разложением на normal/tangential направления.

### 3. Independent statics cross-check

Современные обзоры marine-cable statics используют ту же структуру элементарного равновесия: изменение натяжения/направления уравновешивает распределённые гидродинамические нагрузки и вес в воде.

### 4. Berteaux-attributed vector governing equation

В поздней литературе, прямо приписывающей governing equation Berteaux, используется структура:

```text
m dV/dt
=
hydrodynamic force
+ dT_vector/ds
+ net weight
+ inertia terms
```

В static limit time/inertia terms исчезают, оставляя signed vector force balance.

## Project coordinate convention

Для Candidate B используется явная система:

```text
s : от буя/верха к якорю/низу
X : положительный existing planar horizontal load direction
Z : положительный вниз, как MooringShapePoint.ZDepthM
```

Для сегмента между узлами `j` и `j+1`:

```text
dx = x_(j+1) - x_j
dz = z_(j+1) - z_j
L  = sqrt(dx² + dz²)

t = (dx/L, dz/L)
```

`t` — signed top-to-bottom tangent.

Погружённый вес переводится в силу без потери знака:

```text
F_weight,z = WeightWaterKg * g
g = 9.80665 m/s²
```

Следовательно:

```text
heavy / negatively buoyant -> +Z
buoyant / WeightWaterKg < 0 -> -Z
```

Запрещено применять `Abs` к signed Z load.

## Candidate B1 — internal-node equilibrium

Для внутреннего узла `j`:

```text
t_up   = tangent segment above, top -> node
t_down = tangent segment below, node -> bottom
T_up   = tension magnitude on upper-side cut
T_down = tension magnitude on lower-side cut
F_node = explicit signed point load at node
```

Сила верхнего участка на узел направлена к верхнему соседу, нижнего — к нижнему соседу:

```text
R_node = -T_up * t_up + T_down * t_down + F_node
```

Компоненты:

```text
R_x = -T_up*t_up,x + T_down*t_down,x + F_node,x
R_z = -T_up*t_up,z + T_down*t_down,z + F_node,z
R   = sqrt(R_x² + R_z²)
```

Возможная нормировка для diagnostic read model:

```text
R_rel = R / max(T_up, T_down, |F_node|, 1 N)
```

Это proposed project discretization, согласующаяся со структурой flexible-line statics. Она не получает engineering acceptance threshold до primary/reference validation.

## Candidate B2 — distributed segment equilibrium

Для распределённого элемента физическая структура:

```text
R_segment = T_end_vector - T_start_vector + F_distributed
```

где `F_distributed` включает:

```text
signed submerged weight
hydrodynamic distributed line load
```

Но B2 не реализуется первым шагом: current piecewise geometry не должна притворяться, что одна straight-segment tangent автоматически задаёт оба boundary tension directions криволинейного элемента.

## Exact current cut semantics

`SegmentTensionAnalyzer` работает снизу вверх.

Для segment `i` сначала добавляются:

```text
CurrentForce_i
WeightWater_i * g
```

к cumulative state ниже, затем сохраняется row `i`.

Поэтому:

```text
SegmentTensionRow i
=
cumulative START / TOP-CUT state segment i
```

Геометрия:

```text
segment i = node[i-1] -> node[i]
```

Поле:

```text
MooringShapePoint.SegmentTensionKn
```

является attached result и не определяет физическую сторону cut только по месту хранения в node record.

## Internal junction mapping

Для двух соседних непрерывных сегментов без point load точно на junction:

```text
end-cut state segment i
структурно связан с
start-cut state segment i+1
```

Но signed vector reconstruction требует отдельно определить:

```text
- force direction on each side of cut;
- tangent used at each side;
- distributed-load ownership;
- point-load ownership.
```

## Discrete-load ownership blocker

`MooringDiscreteLoadTensionAnalyzer` сейчас включает discrete load по условию:

```text
PositionAlongLineM >= segment.StartLengthM
```

Перед Candidate B необходимо зафиксировать, кому принадлежит нагрузка при:

```text
PositionAlongLineM
== segment.EndLengthM
== nextSegment.StartLengthM
```

Требование:

```text
каждая физическая connector/payload load входит в node balance ровно один раз
```

Недопустимо:

```text
double count
loss at boundary
floating comparison ambiguity
```

## Point-load vector

Для существующего connector/payload:

```text
F_node,x = CurrentForceN
F_node,z = WeightWaterKg * g
```

с сохранением signed `WeightWaterKg`.

## Boundary nodes excluded from first implementation

### Buoy/top

Полный balance требует явных signed terms:

```text
buoy submerged weight / buoyancy
buoy current drag
wave model contribution
upper-line tension
future vessel interaction if modeled
```

### Anchor/bottom

Полный balance требует:

```text
line tension at anchor
anchor submerged weight
horizontal seabed reaction
vertical seabed reaction
holding/contact/touchdown semantics
```

`MooringVectorBalance.RequiredReactionFxN/FzN` — algebraically required reactions, а не solved anchor reactions.

Поэтому первый Candidate B допускается только для **internal nodes**.

## Tension source unresolved

До production implementation необходимо выбрать один согласованный источник cut-state magnitude/components:

```text
A. SegmentTensionRow
B. MooringShapeTensionRow
C. MooringDiscreteLoadTensionRow
```

Нельзя смешивать:

```text
force state одной модели
с geometry другой модели
```

без явного названия такого comparison residual.

## Availability semantics

Candidate B возвращает `INDETERMINATE`, а не fake zero, если:

```text
- boundary reaction не решена;
- отсутствует соседний cut/segment;
- tangent degenerate;
- tension direction undefined;
- point-load ownership ambiguous;
- input/result non-finite;
- seabed/touchdown/contact semantics отсутствуют.
```

## Limiting cases

### Loadless internal junction

При отсутствии нагрузки на junction и непрерывном tangent:

```text
T_up_vector = T_down_vector
R -> 0
```

### Heavy vertical line

Нельзя требовать:

```text
T_up == T_down
```

через распределённый тяжёлый участок, потому что submerged weight изменяет tension along line.

Node-only residual применяется только к полностью определённой node free-body.

### Point load

В validated static solution:

```text
-T_up*t_up + T_down*t_down + F_node -> 0
```

### Buoyant point load

При:

```text
WeightWaterKg < 0
```

меняется только знак `F_node,z`; `Abs` запрещён.

## Acceptance policy

Engineering threshold пока:

```text
TBD
```

Первый допустимый implementation, после снятия blockers:

```text
INFO-only additive diagnostic
```

Разрешены строгие software tolerances только для synthetic constructed identities, где analytical resultant = 0.

Пять project golden scenarios используются для измерения/регрессии, но их residual values не превращаются автоматически в engineering OK/WARNING/ERROR.

## Required validation before production implementation

```text
1. direct Berteaux §2.1 page/figure/equation;
2. signed cut/tangent diagram in docs;
3. exact balancing synthetic node;
4. deliberate X residual;
5. deliberate Z residual;
6. buoyant point load;
7. point load exactly at segment boundary;
8. multiple adjacent discrete loads;
9. five existing canonical scenarios without golden rewrite;
10. independent quasi-static/reference solver comparison before engineering tolerance.
```

## Forbidden in this phase

```text
- solver equations change;
- MooringShapeSolver geometry change;
- MooringIterativeSolver convergence change;
- MooringPrimaryShapeGate change;
- CalculationResult.Verdict change;
- selected X/Z change;
- anchor/seabed reaction fabrication;
- anchor/weak-link/WLL change;
- signed WeightWaterKg normalization;
- 0.20 m segmentation change;
- PDF/2D geometry change;
- JSON/DTO change;
- 3D.
```

## Decision

Candidate B direction is accepted for **RFC/source-validation work only**:

```text
signed internal-node vector balance
```

Production implementation remains blocked until exact primary Berteaux evidence and node/cut/load ownership contracts are complete.

## Safety gate

Любой docs или future implementation PR merges only after successful exact-head:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```
