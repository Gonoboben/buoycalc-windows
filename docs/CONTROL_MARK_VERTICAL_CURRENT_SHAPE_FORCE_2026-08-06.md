# Контрольная отметка: вертикальное течение W в shape-based модели

Дата: 2026-08-06
Issue: #252
Решение владельца проекта: Option A
Scope: documentation only

## Принятое инженерное решение

Действующая численная модель сохраняется без изменений.

В проекте существуют два разных уровня расчёта силы течения:

```text
Базовая сегментная модель
- использует горизонтальный модуль скорости |Uгор| = sqrt(U² + V²)
- не использует W в формуле базового сопротивления
- формирует CurrentForceN, базовое натяжение, базовую проверку слабого звена и базовую проверку удержания якоря

Ориентационно-зависимая X/Z shape-based модель
- использует горизонтальный модуль как компоненту currentX
- использует W как компоненту currentZ
- вычисляет нормальную скорость относительно фактической X/Z-ориентации сегмента
- формирует ShapeForceN и последующие shape-based натяжения
- участвует в feedback-цикле кандидатной формы
```

## Формулы действующей границы

### Базовая сила сегмента

```text
Uгор = sqrt(U² + V²)
Fbase = 0.5 × rho × Cd × A × Uгор²
```

`W` сохраняется и интерполируется в `SegmentCalculationRow.VerticalCurrentMS`, но не входит в `Fbase`.

### Shape-based сила сегмента

Для касательного единичного вектора сегмента X/Z:

```text
Vxz = (Uгор, W)
Vnormal = component of Vxz normal to the X/Z segment
Fshape = 0.5 × rho × Cd × A × Vnormal²
```

Следовательно, при `W != 0` shape-based сила может отличаться даже при неизменной горизонтальной скорости.

## Разрешённая причинная цепочка W

```text
W
→ MooringShapeForceResult.ShapeLineForceN
→ MooringShapeTensionAnalyzer
→ feedback tensions
→ iterative candidate shape
→ gate diagnostics / gate decision
→ selected shape when candidate is accepted
→ selected-shape coordinates in 2D and PDF
```

Это не означает, что 2D или PDF рассчитывают физику. Они по-прежнему отображают выбранную форму из расчётной модели.

## Что W не изменяет

В действующей архитектуре `W` не входит непосредственно в:

```text
- базовый CurrentForceN сегмента
- базовую сумму силы течения CalculationResult.CurrentForceN
- базовую горизонтальную нагрузку CalculationResult.HorizontalForceN
- базовое натяжение CalculationResult.TensionKn
- базовую проверку слабого звена
- базовую проверку удержания якоря
```

## Требуемое пользовательское описание

Глобальная фраза:

```text
W сохраняется, но в модели сопротивления не используется
```

является неточной и должна быть заменена на двухуровневое описание:

```text
Базовая модель сопротивления использует только |Uгор|.
Компонента W сохраняется и интерполируется и используется только в ориентационно-зависимой X/Z shape-based оценке силы линии.
Поэтому W может повлиять на кандидатную и выбранную форму, но не на базовые проверки якоря и слабого звена.
```

## Разрешённые production-изменения

```text
- пользовательские пояснения окна профиля течения
- подписи и методические строки технического отчёта
- MethodNote shape-based силы, если требуется уточнение границы
- документация текущего метода
```

## Инварианты

```text
- MooringShapeForceAnalyzer не меняется
- формулы normalSpeed и ShapeForceN не меняются
- базовая формула CurrentForceN не меняется
- U/V/W parsing, interpolation and persistence не меняются
- shape tensions, iterative solver, gate and selector routing не меняются
- selected-shape coordinates не меняются
- 2D and PDF remain consumers only
- no public records or signatures
- no JSON/DTO, commands, application version or 3D changes
```

## Последовательность

1. Смёржить эту контрольную отметку.
2. Отдельным PR уточнить текст окна профиля течения.
3. Отдельным PR уточнить технический отчёт и оставшиеся пользовательские строки.
4. Закрыть Issue #252 после зелёных проверок всех production PR.

Каждый PR проходит:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```
