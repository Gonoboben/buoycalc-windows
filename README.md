# BuoyCalc Windows v0.25

Windows-ветка проекта BuoyCalc на **C# + Avalonia**.

Это портирование расчётной логики и интерфейса в desktop-приложение:

```text
SwiftUI iPhone prototype
↓
C# / Avalonia Windows desktop prototype
```

## Статус

v0.25 начинает перевод проекта на инженерную архитектуру: форма постановки теперь создаётся как отдельный расчётный объект `MooringShapeResult`, а не извлекается из Markdown-отчёта.

## Что изменилось в v0.25

1. добавлен `MooringShapeSolver`;
2. добавлен `MooringShapeResult`;
3. добавлены точки формы `MooringShapePoint`;
4. добавлено состояние буя `BuoyShapeState`:
   - `Surface`;
   - `Submerged`;
   - `Overloaded`;
   - `Unknown`;
5. добавлен `MooringShapeStore` для хранения последней рассчитанной формы;
6. `ReportBuilder` теперь строит форму через `MooringShapeSolver`;
7. 2D-схема сначала берёт форму из `MooringShapeStore`;
8. Markdown-парсинг оставлен только как аварийный fallback для старого состояния;
9. в отчёте появился раздел `Расчётная форма постановки X/Z`.

## Новый принцип

```text
расчётное ядро
↓
MooringShapeResult
↓
отчёт / 2D / PDF
```

Визуализация не должна быть источником инженерной логики. Она должна только отображать расчётные координаты.

## Что ещё остаётся переходным

`MooringShapeSolver` пока использует предварительную квазистатическую модель на основе уже существующих сегментов, сил, натяжений и углов. Полный итерационный расчёт равновесия ещё не включён.

Текущее поле:

```text
Converged = false
```

означает, что форма уже вынесена в инженерный объект, но полноценная сходимость ещё будет добавляться следующим этапом.

## Как обновиться локально

```text
Git → Pull
```

Затем:

```text
Build → Clean Solution
Build → Rebuild Solution
```

## Как проверить

1. Запустить программу.
2. Выполнить расчёт.
3. Открыть отчёт.
4. Найти раздел `Расчётная форма постановки X/Z`.
5. Открыть `2D-схема`.
6. Проверить подпись `форма из MooringShapeSolver`.
7. Убедиться, что 2D больше не зависит от парсинга Markdown как основного источника.

## Где хранятся пользовательские библиотеки

```text
Документы\BuoyCalc\Libraries\user-buoys.json
Документы\BuoyCalc\Libraries\user-ropes.json
Документы\BuoyCalc\Libraries\user-connectors.json
Документы\BuoyCalc\Libraries\user-anchors.json
Документы\BuoyCalc\Libraries\user-payloads.json
```

## Следующий шаг

Рекомендуемый v0.26:

1. убрать `MooringShapeStore` как временное глобальное хранилище;
2. передавать `MooringShapeResult` через ViewModel;
3. добавить численные невязки `ΣFx`, `ΣFz`, ошибку по глубине и ошибку по длине;
4. подготовить итерационный solver равновесной формы.
