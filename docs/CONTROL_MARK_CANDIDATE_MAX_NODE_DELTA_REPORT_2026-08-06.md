# Контрольная отметка: максимальное смещение узла кандидатной формы

Дата: 2026-08-06
Issue: #220
Scope: documentation only

## Причина

Таблица `Выбор основной формы` в `MooringIterativeSolver` содержит две строки:

```text
Candidate max Δузла, м
Candidate Z-невязка, м
```

Сейчас первая строка выводит:

```csharp
candidateShape.ConvergenceResidualM
```

В `ToShapeResult(...)` поле `ConvergenceResidualM` кандидатной формы получает значение `verticalResidualM`.

Следовательно, обе строки фактически показывают одну Z-невязку:

```text
Candidate max Δузла = Z-невязка
Candidate Z-невязка = Z-невязка
```

Это ошибка отображения результатов, а не ошибка solver.

## Доступный правильный источник

Фактическое максимальное смещение узла уже рассчитывается для каждой итерации и хранится в:

```text
MooringIterativeSolverIteration.MaxNodeDeltaM
```

Финальная итерация доступна как:

```csharp
var last = rows.LastOrDefault();
```

Поэтому правильный источник строки:

```text
last?.MaxNodeDeltaM ?? 0
```

## Production-правка

В приватный метод:

```csharp
BuildPrimarySelectionReportTable(...)
```

добавляется параметр:

```text
double candidateMaxNodeDeltaM
```

При вызове передаётся:

```text
last?.MaxNodeDeltaM ?? 0
```

Строка таблицы использует:

```text
Candidate max Δузла, м = candidateMaxNodeDeltaM
```

Строка:

```text
Candidate Z-невязка, м = candidateShape.VerticalResidualM
```

остаётся без изменений.

## Инварианты

```text
- геометрия fallback и candidate не меняется;
- MaxNodeDeltaM и VerticalResidualM не пересчитываются;
- критерии сходимости и divergence guard не меняются;
- stop reason не меняется;
- gate и selected-shape routing не меняются;
- stores, 2D и PDF не меняются;
- другие строки и порядок таблицы не меняются;
- публичные records не меняются;
- JSON, DTO, XAML, команды и версия не меняются;
- 3D не добавляется.
```

## Разрешённый production-diff

Только:

```text
Services/MooringIterativeSolver.cs
```

Разрешены:

```text
- один приватный параметр метода;
- один аргумент вызова;
- один источник значения в строке таблицы.
```

## Safety gate

Production PR сливается только после зелёных:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```
