# Контрольная отметка: согласованность WLL и коэффициента запаса

Дата: 2026-08-06  
Issue: #290  
Область: инженерная диагностика расчётного read model

## 1. Контекст

После Issue #286 `CalculationResult` хранит точный коэффициент запаса `SafetyFactor`, использованный расчётным ядром.

В результате одновременно доступны:

```text
WeakLinkBreakingLoadKn
SafetyFactor
WorkingLoadKn
```

Расчётная формула остаётся:

```text
WorkingLoadKn = WeakLinkBreakingLoadKn / SafetyFactor
```

при положительных MBL и коэффициенте запаса. Если одно из этих условий не выполнено, расчётная WLL равна нулю.

Новая диагностика не пересчитывает физику и не заменяет результат. Она независимо проверяет согласованность уже опубликованных полей read model.

## 2. Область применимости

Контроль формулы применяется только когда одновременно:

```text
SafetyFactor конечен и > 0
WeakLinkBreakingLoadKn > 0
```

При выполнении условий:

```text
expectedWllKn = WeakLinkBreakingLoadKn / SafetyFactor
absoluteResidualKn = abs(WorkingLoadKn - expectedWllKn)
relativeResidual = absoluteResidualKn / max(1.0, abs(expectedWllKn))
```

## 3. Диагностическая строка

Название:

```text
Согласованность WLL и коэффициента запаса
```

Значение при применимости:

```text
ΔWLL=<absoluteResidualKn> кН (<relativeResidual>)
```

Допуск:

```text
relative <= 1e-6 при MBL > 0 и SF > 0
```

Статус:

```text
OK    — relativeResidual <= 1e-6
ERROR — relativeResidual > 1e-6
```

Примечание должно показывать:

- MBL слабого звена;
- SafetyFactor;
- ожидаемую WLL;
- опубликованную WLL.

## 4. Неприменимая ветвь

Если MBL слабого звена отсутствует либо коэффициент запаса недопустим, этот локальный контроль формулы не выполняется.

Строка получает `OK` как локально неприменимая и явно сообщает причину:

```text
не применяется: MBL слабого звена не определена
```

или:

```text
не применяется: коэффициент запаса недопустим
```

Такой `OK` не означает, что исходные данные корректны. За них отвечают отдельные существующие диагностики:

- коэффициент запаса слабого звена;
- наличие слабого звена и его MBL;
- фактический запас слабого звена.

## 5. Сохранение поведения

Не изменять:

- формулу WLL;
- формулу `TensionReserve`;
- выбор слабого звена;
- `CalculationResult.Verdict`, `MainRisk` и `Checks`;
- пользовательский ввод и finite-нормализацию;
- силы, веса, плавучесть, волну и течение;
- якорную модель;
- сегментацию 0,20 м и отсутствие лимита числа сегментов;
- solver, gate и selected shape;
- 2D и PDF-координаты;
- project JSON/DTO;
- XAML, команды и версию;
- 3D-область.

## 6. Production scope

Допустимый production-файл:

```text
Services/EngineeringDiagnostics.cs
```

Отчёт меняется только через уже существующую таблицу инженерной диагностики.

## 7. Проверки перед merge

Документационный и production PR допускаются к merge только после успешных:

- `.NET Build`;
- `Selected Shape Consumer Scan`;
- `Report Store Consumer Scan`.