# Контрольная отметка: основной и контрольный запас удержания якоря

Дата: 2026-08-06
Issue: #202
Scope: documentation only

## Причина

Технический отчёт показывает два близко названных значения:

```text
Запас якоря
Запас горизонтального удержания по требуемой реакции Rx
```

Без пояснения пользователь может принять их за два независимых инженерных критерия. Фактически это основной расчёт и контрольное представление того же отношения через векторную ведомость.

## Основной запас удержания

`BuoyCalculator` вычисляет:

```text
RequiredAnchorHoldingKg = HorizontalForceN / g
AnchorReserve = AnchorHoldingKg / RequiredAnchorHoldingKg
```

Следовательно:

```text
AnchorReserve = AnchorHoldingKg × g / HorizontalForceN
```

Это основной запас удержания якоря по базовой горизонтальной нагрузке, используемый в verdict/checks расчётного ядра.

## Контрольный запас по Rx

`MooringVectorBalance` независимо собирает горизонтальные строки ведомости и вычисляет:

```text
RequiredReactionFxN = -SumExternalFxN
AnchorHorizontalCapacityN = AnchorHoldingKg × g
AnchorHorizontalReserve = AnchorHorizontalCapacityN / |RequiredReactionFxN|
```

Следовательно:

```text
AnchorHorizontalReserve = AnchorHoldingKg × g / |RequiredReactionFxN|
```

Это контрольный пересчёт через требуемую реакцию `Rx` векторной ведомости.

## Условие совпадения

Если ведомость восстанавливает ту же базовую горизонтальную нагрузку:

```text
|RequiredReactionFxN| = HorizontalForceN
```

то:

```text
AnchorHorizontalReserve = AnchorReserve
```

Поэтому второе значение не является отдельным критерием выбора якоря. Оно позволяет проверить согласованность суммы `Fx` векторной ведомости с базовой горизонтальной нагрузкой.

## Точные production-правки

### Итоги технического отчёта

В `Services/TechnicalReportMarkdownBuilder.cs` заменить только подпись:

```diff
- Запас якоря
+ Запас удержания якоря по базовой горизонтальной нагрузке
```

Поле `result.AnchorReserve`, порядок строки и формат `0.####` сохраняются.

### Векторная ведомость

В `Services/TechnicalReportMarkdownMovedSections.cs` заменить только подпись:

```diff
- Запас горизонтального удержания по требуемой реакции Rx
+ Контрольный запас удержания по Rx векторной ведомости
```

Поле `balance.AnchorHorizontalReserve`, порядок строки и формат `0.####` сохраняются.

### MethodNote

В `Services/MooringVectorBalance.cs` к действующему `MethodNote` добавить:

```text
При согласованной сумме Fx этот контрольный запас совпадает с запасом удержания якоря в итогах.
```

## Разрешённый production-diff

Только три файла:

```text
Services/TechnicalReportMarkdownBuilder.cs
Services/TechnicalReportMarkdownMovedSections.cs
Services/MooringVectorBalance.cs
```

Разрешены только три строковые замены:

```text
- подпись основного AnchorReserve;
- подпись контрольного AnchorHorizontalReserve;
- дополнение MooringVectorBalance.MethodNote.
```

## Инварианты

```text
- HorizontalForceN не меняется;
- RequiredAnchorHoldingKg не меняется;
- AnchorHoldingKg и AnchorReserve не меняются;
- SumExternalFxN и RequiredReactionFxN не меняются;
- AnchorHorizontalCapacityN и AnchorHorizontalReserve не меняются;
- verdict и checks не меняются;
- IsSolved остаётся false;
- строки, единицы, порядок и числовой формат отчёта не меняются;
- solver, stores, selected shape, 2D и PDF не меняются;
- JSON, DTO, XAML, команды и версия приложения не меняются;
- 3D не добавляется.
```

## Safety gate

Production PR сливается только после зелёных:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```
