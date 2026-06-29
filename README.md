# BuoyCalc Windows v0.38.3

Windows-ветка проекта BuoyCalc на C# + Avalonia.

## Статус

v0.38.3 добавляет мост между GitHub Actions и classic commit status. Теперь workflow `.NET Build` должен не только показывать зелёную/красную галочку во вкладке Actions, но и записывать статус сборки в GitHub commit status API.

Это нужно, чтобы проверку сборки можно было читать через `get_commit_combined_status`.

## Что изменилось в v0.38.3

1. Обновлён workflow:

```text
.github/workflows/dotnet-build.yml
```

2. Добавлены права workflow:

```yaml
permissions:
  contents: read
  statuses: write
```

3. Restore и build теперь выполняются в одном PowerShell-шаге:

```text
Restore and build with commit status
```

4. Workflow записывает classic commit status с контекстом:

```text
BuoyCalc Windows Build
```

5. В начале сборки ставится:

```text
pending: Build started
```

6. Если `dotnet restore` и `dotnet build` прошли успешно, ставится:

```text
success: Build succeeded
```

7. Если restore/build падает, ставится:

```text
failure: Build failed
```

и сам workflow тоже падает.

8. Отображаемая версия окон и `AppInfo` обновлены:

```text
v0.38.3 - CI status bridge
```

## Что это исправляет

Раньше GitHub Actions показывал зелёные галочки, но `get_commit_combined_status` возвращал пустой список, потому что он смотрит classic commit statuses, а Actions живут в workflow runs/checks.

Теперь Actions должен дополнительно публиковать classic status, который можно прочитать через status API.

## Что не изменилось

Расчётная физика, PDF, 2D-схема и отчёт в v0.38.3 не менялись. Это только инфраструктурное изменение CI.

## Как проверить

1. Открыть GitHub:

```text
Actions -> .NET Build
```

2. Убедиться, что последний workflow зелёный.
3. Проверить, что commit status содержит контекст:

```text
BuoyCalc Windows Build
```

4. Если статус `success`, значит мост работает.
5. Если workflow зелёный, но classic status всё ещё пустой, значит нужно смотреть права `GITHUB_TOKEN` или политику репозитория для `statuses: write`.

## Что осталось от v0.38.2

Короткая линия `LineLength < Depth` по-прежнему отображается как режим погружённого буя / нештатной поверхностной постановки. Волновая нагрузка не отключается.

## Как обновиться локально

```text
Git -> Pull
Build -> Clean Solution
Build -> Rebuild Solution
```

## Следующий шаг

После проверки CI status bridge можно продолжать инженерную часть: сравнение PDF-вариантов и подготовку итерационного solver.
