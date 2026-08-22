# BuoyCalc Windows v1.0.0 release process

## Статус

Приложение использует identity `v1.0.0`, но до завершения Release Candidate smoke это **не опубликованный релиз**.

Запрещено создавать git tag `v1.0.0` или GitHub Release до явного подтверждения пользователя после ручной проверки Windows RC.

## Замороженная инженерная база

F1-F4 завершены до начала release freeze. F5 не меняет инженерные формулы или authority chain.

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

Canonical engineering regression baseline зафиксирован F5-A и не регенерируется ради release packaging.

## Локальная Windows x64 публикация

Низкоуровневый publish entry point:

```powershell
./scripts/publish-windows.ps1
```

Он публикует `Release`, `win-x64`, `self-contained` single-file приложение и проверяет, что результат содержит ровно один исполняемый файл `BuoyCalc.Windows.exe`.

Полный RC packaging entry point:

```powershell
./scripts/package-windows-rc.ps1 -Version v1.0.0 -Runtime win-x64
```

После package step обязательно выполняется:

```powershell
./scripts/verify-windows-rc.ps1 -Version v1.0.0 -Runtime win-x64
```

## RC evidence set

Для `v1.0.0` / `win-x64` создаются ровно три release-файла:

```text
BuoyCalc-Windows-v1.0.0-win-x64.zip
BuoyCalc-Windows-v1.0.0-win-x64.sha256
BuoyCalc-Windows-v1.0.0-win-x64-manifest.json
```

Manifest содержит:

- version;
- runtime;
- exact source commit SHA;
- имя ZIP;
- SHA-256 ZIP;
- имя и SHA-256 `BuoyCalc.Windows.exe`;
- `selfContained=true`;
- `singleFile=true`;
- параметры нормализации ZIP.

ZIP содержит только один файл:

```text
BuoyCalc-Windows-v1.0.0-win-x64/BuoyCalc.Windows.exe
```

Для одинакового входного EXE процедура упаковки использует стабильный порядок, store/no-compression и фиксированное время ZIP entry `2000-01-01T00:00:00Z`. Это исключает случайную зависимость checksum от времени упаковки или порядка файлов.

## GitHub Actions RC build

Workflow:

```text
BuoyCalc Windows Release
```

Он сохраняет ручной `workflow_dispatch` и дополнительно запускается при push специальной ветки:

```text
release-candidate/v1.0.0
```

Перед publish workflow выполняет `git fetch origin main` и требует одновременно:

```text
checked-out HEAD == github.sha
checked-out HEAD == origin/main
```

Поэтому RC-ветка является только триггером. Она не может собрать отдельный от `main` commit.

Workflow artifact имеет фиксированное имя:

```text
BuoyCalc-Windows-v1.0.0-win-x64-RC
```

и содержит ровно ZIP + SHA-256 + manifest.

## Обязательный gate перед созданием RC trigger

На exact `main` должны быть зелёными:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
BuoyCalc Windows Build: success
```

Только после этого `release-candidate/v1.0.0` создаётся/перемещается на **тот же SHA main**. Нельзя использовать RC artifact от commit, для которого обязательные проверки `pending` или `failure`.

## Обязательный ручной Windows 11 smoke

После создания exact-main RC пользователь вручную проверяет **тот же ZIP**, который предполагается выпустить:

1. распаковать `BuoyCalc-Windows-v1.0.0-win-x64.zip`;
2. приложение запускается;
3. отображается identity `v1.0.0` и Release Candidate note;
4. создаётся новый проект;
5. проект сохраняется;
6. сохранённый проект загружается обратно;
7. расчёт выполняется без ошибки;
8. последовательность элементов отображается корректно;
9. выбранная 2D-схема открывается;
10. PDF экспортируется;
11. PDF использует рассчитанную выбранную геометрию/read models;
12. полный technical report открывается и показывает selected authority там, где она доступна;
13. проверяются несколько реальных постановок, включая рабочий сценарий пользователя.

Перед smoke нужно сверить SHA-256 ZIP со значением одновременно в `.sha256` и manifest.

Если smoke выявляет дефект, tag/release не создаётся. Исправление идёт отдельным PR, после чего RC строится заново и smoke повторяется.

## Финальный выпуск

Только после явного подтверждения успешного Windows smoke допускается:

1. удостовериться, что source commit из manifest всё ещё является проверенным release commit;
2. создать tag `v1.0.0` на этом commit;
3. создать GitHub Release из этого tag;
4. прикрепить **тот же проверенный** ZIP, checksum, manifest и release notes.

Не следует менять код или пересобирать другой commit между успешным smoke и финальным выпуском.

## Принцип

Release packaging не владеет инженерной физикой. Он только упаковывает уже проверенный deterministic quasi-static v1 calculation core и его presentation read models.
