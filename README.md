# BuoyCalc Windows v0.46

Windows-ветка проекта BuoyCalc на C# + Avalonia.

## Статус

v0.46 — подготовка release build. Расчётная физика, solver, 2D-координаты и PDF-геометрия не изменены. Добавлены только скрипт публикации, ручной GitHub Actions workflow и release-инструкция.

## План версий

```text
v0.39   — каркас итерационного solver
v0.39.1 — критерии сходимости solver
v0.39.2 — отчёт по итерациям
v0.39.3 — gate перед подключением кандидатной формы как основной
v0.40   — включение дискретных нагрузок в основной solver
v0.40.1 — отчёт о выборе основной формы
v0.40.2 — таблица выбора основной формы в отчёте
v0.41   — режимы постановки: surface/submerged/short/excess line
v0.42   — тестовые сценарии и автопроверки
v0.43   — улучшение базы элементов
v0.44   — UX редактора последовательности
v0.45   — финальная структура PDF-отчёта
v0.45.1 — русские пояснения PDF и цепочки
v0.45.2 — PDF выводит форму с дискретными элементами
v0.46   — подготовка release build
```

## Что добавлено

1. Скрипт локальной публикации:

```text
scripts/publish-windows.ps1
```

2. Ручной workflow GitHub Actions:

```text
.github/workflows/release-windows.yml
```

3. Release-инструкция:

```text
RELEASE.md
```

4. Версия приложения:

```text
v0.46 - release build preparation
```

## Как собрать локально

```powershell
./scripts/publish-windows.ps1
```

Результат:

```text
artifacts/publish/BuoyCalc-Windows-win-x64
```

## Как собрать через GitHub Actions

1. Открыть GitHub Actions.
2. Выбрать workflow:

```text
BuoyCalc Windows Release
```

3. Запустить вручную.
4. Скачать artifact:

```text
BuoyCalc-Windows-win-x64
```

## Что проверить перед распространением

```text
BuoyCalc Windows Build: success
release workflow завершился успешно
приложение запускается на Windows
расчёт выполняется
2D окно открывается
PDF экспортируется
PDF показывает форму с дискретными элементами
полный отчёт открывается
```

## Что не изменилось

1. Физика расчёта не изменилась.
2. Solver не изменился.
3. Gate не удалён.
4. Fallback-форма не удалена из приложения.
5. PDF не придумывает координаты: форма берётся из расчётного слоя дискретных нагрузок.
