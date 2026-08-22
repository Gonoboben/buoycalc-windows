# BuoyCalc Windows v1.0.0 release process

## Статус

Приложение использует identity `v1.0.0`, но до завершения Release Candidate smoke это **не опубликованный релиз**.

Запрещено создавать git tag `v1.0.0` или GitHub Release до явного подтверждения пользователя после ручной проверки Windows RC.

## Замороженная инженерная база

F1-F4 завершены до начала release freeze. F5 не должен менять инженерные формулы или authority chain.

Замороженные инварианты:

```text
segmentation = 0.20 m
signed feedback budget = 64
signed WeightWaterKgM semantics = unchanged
Accepted signed candidate = exact deterministic fixed point
selected authority = retained F1/F2/F3/F4 calculation-core state
renderers = presentation only
3D = post-v1
```

Canonical engineering regression baseline фиксируется отдельной F5-A контрольной отметкой и не регенерируется ради смены версии.

## Локальная Windows x64 сборка

Текущий publish entry point:

```powershell
./scripts/publish-windows.ps1
```

Он публикует `Release`, `win-x64`, `self-contained` single-file приложение в:

```text
artifacts/publish/BuoyCalc-Windows-win-x64
```

F5-B дополнительно зафиксирует детерминированное имя RC-архива, manifest и SHA-256 checksum.

## GitHub Actions RC build

Workflow:

```text
BuoyCalc Windows Release
```

До F5-B он остаётся ручным `workflow_dispatch` publish workflow. После F5-B RC должен строиться только из exact `main`, с проверяемым commit SHA, детерминированным именем артефакта и SHA-256.

## Обязательный gate перед RC

На exact `main` должны быть зелёными:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
BuoyCalc Windows Build: success
```

Нельзя использовать RC artifact от commit, для которого обязательные проверки `pending` или `failure`.

## Обязательный ручной Windows 11 smoke

После F5-B и создания exact-main RC пользователь вручную проверяет **тот же artifact**, который предполагается выпустить:

1. приложение запускается;
2. отображается identity `v1.0.0` и Release Candidate note;
3. создаётся новый проект;
4. проект сохраняется;
5. сохранённый проект загружается обратно;
6. расчёт выполняется без ошибки;
7. последовательность элементов отображается корректно;
8. выбранная 2D-схема открывается;
9. PDF экспортируется;
10. PDF использует рассчитанную выбранную геометрию/read models;
11. полный technical report открывается и показывает selected authority там, где она доступна;
12. проверяются несколько реальных постановок, включая рабочий сценарий пользователя.

Если smoke выявляет дефект, tag/release не создаётся. Исправление идёт отдельным PR, после чего RC строится заново и smoke повторяется.

## Финальный выпуск

Только после явного подтверждения успешного Windows smoke допускается:

1. удостовериться, что commit RC всё ещё является exact release commit;
2. создать tag `v1.0.0` на проверенном commit;
3. создать GitHub Release из этого tag;
4. прикрепить **тот же проверенный** Windows artifact, checksum и release notes.

Не следует менять код или пересобирать другой commit между успешным smoke и финальным выпуском.

## Принцип

Release packaging не владеет инженерной физикой. Он только упаковывает уже проверенный deterministic quasi-static v1 calculation core и его presentation read models.
