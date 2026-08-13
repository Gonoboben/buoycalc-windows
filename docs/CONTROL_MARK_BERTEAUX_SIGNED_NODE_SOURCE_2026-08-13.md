# Контрольная отметка: первичный источник Берто для signed internal-node balance

Дата: 2026-08-13  
Issue: #386  
Scope: documentation / source validation only

## Цель

Зафиксировать первичную физическую основу для следующего этапа Candidate B — signed internal-node equilibrium diagnostic — до появления production-кода.

Эта отметка не меняет solver, формулы, gate, verdict, selected X/Z, 2D/PDF или пользовательские расчётные результаты.

## Источник

Основной источник проекта:

```text
Г. О. Берто
Океанографические буи
Перевод с английского Г. П. Лисова
Л.: Судостроение, 1979
русское издание H. O. Berteaux, Buoy Engineering, 1976
```

Проверка выполнена по предоставленному владельцем проекта OCR-searchable PDF русского издания.

Релевантная область:

```text
Часть II — Механика буйрепов
Глава 2 — Статика буйрепов
§2.1 — системы, поставленные на якорь в одной точке
```

## 1. Signed submerged load: P = W - B

На печатной стр. 34 Берто определяет «гравитационную» силу на единицу длины троса:

```text
P = W - B                                      (2.1)
```

где:

```text
W — вес единицы длины троса;
B — вес воды, вытесненной единицей длины троса.
```

Следствие:

```text
P > 0  -> тяжёлый в воде участок;
P = 0  -> нейтральный участок;
P < 0  -> плавучий в воде участок.
```

Знак не должен уничтожаться нормализацией через `Abs`.

Это согласуется с уже действующим проектным контрактом signed `WeightWaterKg` / `WeightWaterKgM`.

## 2. Нормальная и касательная гидродинамические силы

На печатной стр. 35 Берто раскладывает сопротивление элементарного участка троса на нормальную и касательную составляющие.

Для угла `φ` между осью троса и направлением течения вводятся:

```text
D ds — нормальная составляющая гидродинамического сопротивления;
F ds — касательная составляющая сопротивления трения.
```

В частности, нормальная часть приводится к форме:

```text
D = R sin² φ                                    (2.3)
```

а касательная определяется отдельным коэффициентом касательного сопротивления и касательной составляющей скорости, далее приводясь к формам (2.4)–(2.5).

Для Candidate B важно не копировать эти scalar drag-функции заново: первый node diagnostic должен использовать уже рассчитанные проектом `CurrentForceN` из согласованной model family.

## 3. Элементарный участок: Fig. 2.4 и component equilibrium

На печатной стр. 38 подпись к рис. 2.4 определяет силы на элементарном участке `ds`:

```text
T        — натяжение;
ΔT       — изменение натяжения на элементарном участке;
D ds     — нормальная составляющая сопротивления;
F ds     — касательное сопротивление трения;
P ds     — сила тяжести / submerged resultant elementary load;
φ        — угол между осью троса и направлением течения V;
dφ       — изменение угла φ на длине ds;
ds       — длина элементарного участка.
```

Берто прямо формулирует условие статического равновесия как нулевую векторную сумму действующих сил и разрешает её на нормальную и касательную составляющие.

Перед differential simplification OCR первичного текста восстанавливает:

```text
(T + dT) sin(dφ) - D ds - P cos(φ) ds = 0

-T + (T + dT) cos(dφ) - P sin(φ) ds + F ds = 0
```

При:

```text
sin(dφ) ≈ dφ
cos(dφ) ≈ 1
```

и отбрасывании произведения второго порядка `dT*dφ` получаются исходные дифференциальные уравнения статики Берто:

```text
T dφ = (D + P cos φ) ds                         (2.6)

dT   = (P sin φ - F) ds                         (2.7)
```

Дальнейший текст книги даёт внутреннюю проверку знаков: при пренебрежении сопротивлением троса уравнения (2.6)–(2.7) переходят в:

```text
T dφ = P cos φ ds                               (2.8)
dT   = P sin φ ds                               (2.9)
```

## 4. Более сильная primary-source проверка направления tension vectors

Для Candidate B ключевым является не только scalar `T`, а направление силы натяжения на двух границах элементарного участка.

На печатных стр. 76–77 Берто переходит к пространственной векторной постановке.

Для единичного касательного вектора `u` он явно задаёт:

```text
на одном конце элементарного участка:   -T u
на другом конце:                         (T + dT)(u + du)
```

где `du` — изменение направления троса на длине элементарного участка.

Это прямое текстовое определение первичного источника.

Следовательно, знак end tensions не требуется выводить по внешнему пересказу или угадывать по OCR-стрелкам рисунка.

## 5. Вектор submerged weight в пространственной постановке

На печатной стр. 76 Берто записывает результирующую гравитационную силу элементарного участка как:

```text
-p ds k                                          (2.88)
```

где `p` — разность между весом и плавучестью единицы длины троса.

То есть пространственная запись сохраняет тот же signed смысл `W - B`.

## 6. Переход к системе координат BuoyCalc

В первом Candidate B используется существующая planar X/Z система проекта:

```text
s : возрастает от буя / верхнего конца к якорю / нижнему концу;
X : положительное направление существующей результирующей горизонтальной нагрузки;
Z : положительно вниз, как `MooringShapePoint.ZDepthM`.
```

Для геометрического сегмента:

```text
dx = x_(j+1) - x_j
dz = z_(j+1) - z_j
L  = sqrt(dx² + dz²)

t = (dx/L, dz/L)
```

`t` направлен по `s`, то есть сверху вниз.

### Преобразование веса

У Берто submerged weight направлен противоположно положительному `k`.

В BuoyCalc `+Z` выбран вниз, поэтому project-space signed load принимает непосредственную форму:

```text
F_weight,z = WeightWaterKg * g

g = 9.80665 m/s²
```

Следовательно:

```text
WeightWaterKg > 0 -> +Z;
WeightWaterKg < 0 -> -Z.
```

Запрещено:

```text
Abs(WeightWaterKg)
Abs(F_node,z)
```

в Candidate B.

## 7. Internal-node free body в проектной ориентации

Пусть внутренний узел соединяет верхний и нижний сегменты.

Оба tangent vector определяются в одной project orientation top -> bottom:

```text
t_above : upper neighbor -> node
t_below : node -> lower neighbor
```

Сила верхнего сегмента, действующая на узел, направлена к верхнему соседу:

```text
-T_above * t_above
```

Сила нижнего сегмента, действующая на узел, направлена к нижнему соседу:

```text
+T_below * t_below
```

Для grouped point load:

```text
F_node,x = Σ CurrentForceN
F_node,z = Σ WeightWaterKg * g
```

Поэтому source-backed проектная форма residual:

```text
R_node = -T_above * t_above
         +T_below * t_below
         +F_node
```

или по компонентам:

```text
R_x = -T_above*t_above,x + T_below*t_below,x + F_node,x
R_z = -T_above*t_above,z + T_below*t_below,z + F_node,z
R   = sqrt(R_x² + R_z²)
```

Это дискретная project implementation элементарного signed force balance, а не новый закон физики.

## 8. Ownership дискретной нагрузки — решение #385 сохраняется

Первый Candidate B должен соблюдать уже зафиксированный software/data contract:

```text
- connector/payload items с одной и той же s образуют один механический internal node;
- F_node суммируется до расчёта residual;
- existing MooringDiscreteLoadTensionAnalyzer predicate
  PositionAlongLineM >= segment.StartLengthM
  не меняется;
- cumulative row нижнего segment на s_node остаётся inclusive state;
- exclusive state непосредственно ниже point load получается:

  C_below = C_inclusive - F_node.
```

Алгебраическое равенство:

```text
C_inclusive - C_below = F_node
```

является software ownership check, а не physical equilibrium proof.

## 9. Разрешённая model family для первого Candidate B

Чтобы не смешивать состояния разных итераций, первый production diagnostic после этой отметки может потреблять только согласованную pre-iterative family:

```text
MooringSequencePositionResult
+ MooringDiscreteLoadTensionResult
+ MooringDiscreteLoadShapeResult
```

Причина:

```text
- sequence result задаёт s и source point loads;
- discrete tension result содержит cumulative state с этими же loads;
- discrete-load shape строится непосредственно из этих же tension rows.
```

Запрещено называть этот residual:

```text
selected-shape equilibrium residual
```

Правильный смысл:

```text
pre-iterative discrete-load candidate internal-node equilibrium residual
```

## 10. Запрет на hybrid state

Нельзя объединять:

```text
MooringIterativeSolver.FinalShape geometry
+
pre-iterative MooringDiscreteLoadTensionResult
```

и называть результат равновесием.

Если в будущем потребуется residual именно для final/selected iterative shape, сначала нужен immutable final-iteration per-segment signed force/tension read model той же итерации.

## 11. Boundary nodes не входят в первый Candidate B

Первый diagnostic ограничивается внутренними узлами.

Не вычислять solved residual для:

```text
s = 0            — buoy/top boundary;
s = LineLength   — anchor/bottom boundary.
```

Причина:

```text
- верхняя граница требует полного buoy free body;
- нижняя граница требует solved seabed/anchor reaction;
- текущий RequiredReaction ledger не является solved support reaction.
```

На boundary node должен публиковаться `INDETERMINATE`/not-applicable semantics, а не искусственный ноль.

## 12. Degenerate cases

Первый analyzer должен возвращать `INDETERMINATE`, если:

```text
- нет соседнего upper/lower segment;
- junction не matchится однозначно к segment boundary;
- tangent имеет нулевую/невалидную длину;
- required tension state отсутствует;
- grouped load или geometry содержит non-finite value;
- point-load ownership неоднозначен;
- node относится к top/bottom boundary.
```

Не подменять unavailable physical free body residual=0.

## 13. Engineering tolerance пока не задан

Первый Candidate B, если будет реализован, остаётся:

```text
INFO / diagnostic only.
```

Нельзя пока использовать residual для:

```text
- solver convergence;
- MooringPrimaryShapeGate;
- selected shape;
- CalculationResult.Verdict;
- anchor reserve;
- weak-link/WLL decision.
```

Software tolerances допустимы только для synthetic algebraic identities с аналитически известным нулём.

Engineering acceptance threshold для реальных постановок требует отдельной reference-validation работы.

## 14. Что именно подтверждено первичным источником

Подтверждено непосредственно русским Берто 1979:

```text
[x] signed submerged load P = W - B;
[x] нормальная/касательная декомпозиция drag;
[x] нулевая vector sum как условие static equilibrium;
[x] differential component equations (2.6), (2.7);
[x] end tension sign structure -T u и +(T+dT)(u+du);
[x] submerged-weight vector -p ds k;
[x] допустимость отрицательного weight-minus-buoyancy quantity.
```

Рис. 2.4 не был отдельно визуально проинспектирован через текущий инструментальный интерфейс.

Это не является sign blocker, потому что направления end tension vectors явно заданы текстом первичного источника на стр. 76–77. Визуальная проверка рисунка остаётся полезной corroboration, но знаки residual не выводятся из предположения о стрелках.

## 15. Не меняется этой отметкой

Этот PR/документ не разрешает и не выполняет изменений:

```text
- BuoyCalculator;
- drag / buoyancy / weight formulas;
- MooringShapeSolver;
- MooringDiscreteLoadTensionAnalyzer predicate/semantics;
- MooringDiscreteLoadShapeBuilder;
- MooringIterativeSolver;
- MooringPrimaryShapeGate;
- CalculationResult.Verdict;
- selected X/Z;
- anchor/seabed model;
- weak-link/WLL;
- target segmentation 0.20 m;
- unlimited segment-count policy;
- signed WeightWaterKg;
- report/UI/PDF/2D physics;
- JSON/DTO;
- 3D.
```

## 16. Следующий допустимый production package

Только после merge этой source control mark разрешён отдельный маленький PR:

```text
additive Candidate-B internal-node analyzer
```

с условиями:

```text
- coherent pre-iterative state family only;
- internal nodes only;
- grouped same-s point loads exactly once;
- signed X/Z components;
- INDETERMINATE for incomplete free bodies;
- INFO only;
- no solver/gate/verdict feedback;
- no report rendering in the same first production package unless explicitly split and reviewed;
- unchanged five-scenario golden baseline.
```

Минимальная deterministic validation:

```text
1. exactly balancing signed synthetic node;
2. deliberate X residual;
3. deliberate Z residual;
4. buoyant point load / negative WeightWaterKg;
5. multiple same-s discrete loads grouped once;
6. boundary node -> INDETERMINATE;
7. five canonical project scenarios measured without baseline rewrite.
```

## Контроль merge

Документационный source package можно merge только если exact PR head имеет:

```text
.NET Build: success
Selected Shape Consumer Scan: success
Report Store Consumer Scan: success
```

После этого #386 может быть закрыт как completed, а Candidate B production work должен получить отдельный issue/PR.
