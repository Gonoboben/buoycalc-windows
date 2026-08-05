# Контрольная отметка: текущие методические примечания отчёта

Дата: 2026-08-05
Issue: #177
Scope: documentation only

## Причина

Полный технический отчёт напрямую выводит десять значений `MethodNote` в разделе `Ограничения`. Часть строк содержит номера исторических релизов, а часть описывает промежуточные этапы разработки, которые уже не соответствуют текущей архитектуре.

История реализации должна оставаться в `docs/` и Git history. Пользовательский инженерный отчёт должен описывать текущий метод, текущий источник данных и действующее ограничение.

## Граница вывода

`TechnicalReportMarkdownBuilder` выводит:

```text
shape.MethodNote
shapeProjection.MethodNote
shapeForces.MethodNote
shapeTensions.MethodNote
sequencePositions.MethodNote
discreteLoadTensions.MethodNote
discreteLoadShape.MethodNote
alternativeDiscreteNodes.MethodNote
iterativeSolver.MethodNote
vectorBalance.MethodNote
```

`iterativeSolver.MethodNote` дополнительно содержит таблицы выбора основной формы, режима постановки и автопроверок.

## Категория A: убрать только номер версии

Содержательная часть остаётся действующей:

```text
MooringShapeSolver
- геометрическая X/Z-форма является fallback
- бисекция замыкает глубину
- это не полный нелинейный solver равновесия сил и формы

MooringVectorBalance
- силы собраны в векторную ведомость
- реакция вычисляется как требуемая для ΣF=0
- реакция не является найденным solver-ом равновесным решением

MooringDeploymentModeClassifier
- классифицирует режим постановки
- не меняет силы, натяжения или форму

MooringAutocheckSuite
- проверяет данные и согласованность результатов
- не меняет инженерную физику
```

## Категория B: заменить устаревшее содержание

### MooringShapeProjection

Устарело:

```text
пересчёт сил по ориентации является будущим шагом
```

Текущее состояние:

```text
проекции уже используются MooringShapeForceAnalyzer
и участвуют в итерационном candidate-shape feedback-цикле
```

Ограничение:

```text
сама проекция проверяет геометрию, но не решает полный нелинейный баланс
```

### MooringShapeForceAnalyzer

Устарело:

```text
shape-based силы не подставляются обратно
```

Текущее состояние:

```text
силы по форме X/Z используются MooringShapeTensionAnalyzer
и итерационным solver для кандидатной формы
```

Ограничение:

```text
они не заменяют CurrentForceN базового CalculationResult
и не переписывают базовые проверки якоря и слабого звена
```

### MooringShapeTensionAnalyzer

Устарело:

```text
альтернативные натяжения не перестраивают форму
```

Текущее состояние:

```text
они преобразуются в feedback tensions
и участвуют в построении кандидатной формы итерационного solver
```

Ограничение:

```text
они остаются сравнительным слоем относительно базовой сегментной модели
до принятия формы через gate
```

### MooringSequencePositioner

Устарело:

```text
дискретные элементы только размечены по s
локальные скачки ещё не вставлены
```

Текущее состояние:

```text
соединители и приборы имеют координату s
их вес в воде и горизонтальная сила учитываются
в MooringDiscreteLoadTensionAnalyzer и кандидатной форме
```

### MooringDiscreteLoadTensionAnalyzer

Устарело:

```text
форма X/Z не перестраивается по дискретным натяжениям
```

Текущее состояние:

```text
MooringDiscreteLoadShapeBuilder строит по ним альтернативную форму
итерационный solver использует эту форму в feedback-цикле
```

### MooringDiscreteLoadShapeBuilder

Устарело:

```text
основной solver не заменён
```

Текущая граница:

```text
builder формирует альтернативную X/Z-форму
в итерационном цикле она становится кандидатной формой
только MooringPrimaryShapeGate разрешает сделать кандидата выбранной основной формой
при отклонении сохраняется MooringShapeSolver fallback
```

### MooringAlternativeDiscreteNodeProjector

Текущая роль:

```text
проецирует дискретные элементы на альтернативную форму
публикует отчётно-визуальные X/Z-точки
не рассчитывает новую физику и не выбирает основную форму
```

## Категория C: текущая логика с историческими labels

Убрать `v0.xx` из пользовательского вывода без изменения поведения:

```text
MooringIterativeSolver.MethodNote
MooringIterativeSolver.ConvergenceCriterion
feedback row status
temporary candidate-shape MethodNote
embedded primary-selection heading
embedded deployment-mode heading
embedded autocheck heading
MooringPrimaryShapeGate MethodNote and DecisionText
MooringPrimaryShapeSelector MethodNote
MooringDeploymentModeClassifier MethodNote and heading
MooringAutocheckSuite MethodNote and heading
```

## Renderer-level stale strings

Подлежат отдельной малой правке:

```text
TechnicalReportMarkdownMovedSections
- модель покрытия с утверждением про v0.39 и незаменённые 2D/PDF
- описания участия элементов в v0.39 feedback-цикле

TechnicalReportMarkdownDiscreteTensionSections
- вводная строка с v0.33

TechnicalReportMarkdownDiscreteShapeSections
- утверждение «основной solver пока не заменяется»
```

## Разрешённые изменения

```text
- только строковые литералы пользовательского вывода
- MethodNote, DecisionText, ConvergenceCriterion text
- заголовки встроенных Markdown-таблиц
- SolverRole и NextStepNote позиционной ведомости
```

## Инварианты

```text
- формулы и численные значения не меняются
- tolerances и MaxIterations не меняются
- StopReason enum и решение остановки не меняются
- gate decision и selector routing не меняются
- result records и свойства не переименовываются
- порядок и количество строк таблиц не меняются
- единицы и формат 0.#### не меняются
- selected shape, stores, 2D и PDF diagram source не меняются
- JSON, DTO, XAML, команды и версия приложения не меняются
- 3D не добавляется
```

## Последовательность production PR

1. Renderer framing.
2. Baseline and X/Z method notes.
3. Position and discrete-load method notes.
4. Iterative, gate, deployment-mode and autocheck presentation strings.

Каждый PR должен отдельно пройти:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```
