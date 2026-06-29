# BuoyCalc Windows release build

## Назначение

Этот файл описывает подготовку release build для Windows-версии BuoyCalc.

v0.46 не меняет расчётную физику, solver, 2D-координаты или PDF-геометрию. Он добавляет только выпускной контур сборки.

## Локальная сборка Windows x64

Из корня репозитория:

```powershell
./scripts/publish-windows.ps1
```

По умолчанию скрипт выполняет:

```text
dotnet restore BuoyCalc.Windows.csproj
dotnet publish BuoyCalc.Windows.csproj --configuration Release --runtime win-x64 --self-contained true
```

Результат:

```text
artifacts/publish/BuoyCalc-Windows-win-x64
```

## Ручная сборка через GitHub Actions

В GitHub Actions доступен workflow:

```text
BuoyCalc Windows Release
```

Он запускается вручную через `workflow_dispatch` и создаёт artifact:

```text
BuoyCalc-Windows-win-x64
```

## Проверка перед распространением

1. Запустить обычную CI-проверку:

```text
BuoyCalc Windows Build: success
```

2. Запустить release workflow вручную.
3. Скачать artifact `BuoyCalc-Windows-win-x64`.
4. Запустить приложение на Windows.
5. Проверить минимальный smoke test:

```text
расчёт выполняется
2D окно открывается
PDF экспортируется
PDF показывает форму с дискретными элементами
полный отчёт открывается
```

## Принцип

Release build не должен менять расчёт. Он только упаковывает уже проверенную версию приложения.
