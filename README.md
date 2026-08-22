# BuoyCalc Windows

Windows-приложение для инженерного расчёта буйковой постановки на C# и Avalonia.

## Текущий статус

Пользовательская и assembly identity Release Candidate:

```text
v1.0.0
```

Это **Release Candidate**, а не опубликованный GitHub Release. Тег `v1.0.0` и финальный GitHub Release запрещено создавать до обязательного ручного smoke test на Windows 11 и явного подтверждения пользователя.

Инженерные этапы F1-F4 завершены. Проект находится в F5 — release freeze / packaging / RC verification.

## Зафиксированная v1 инженерная цепочка

Для Accepted `SignedBoundaryFeedback` selected-path пользовательские выводы строятся из уже рассчитанных typed authorities:

```text
environment
  -> selected signed/quasi-static geometry
  -> F1 wave-aware selected design tension demand
  -> F2 anchor-end H/V reaction + contact/uplift state
  -> F3 local per-element structural demand/capacity/reserve
  -> F4 selected checks / Verdict / MainRisk
  -> UI / PDF / technical report projection only
```

Основные правила:

1. Инженерная физика живёт только в расчётном ядре.
2. UI, PDF, 2D и отчёты только отображают рассчитанные данные и не пересчитывают tension, anchor reaction, reserve или verdict.
3. Non-Accepted signed candidates сохраняют legacy fallback read model.
4. Production segmentation остаётся точно `0.20 m`.
5. Signed feedback budget остаётся `64`.
6. Signed `WeightWaterKgM` semantics не меняются.
7. Accepted signed candidate остаётся exact deterministic fixed point, без convergence epsilon.
8. `s=0` соответствует бую/поверхности, `s=L` — якорю/дну.
9. Горизонтальная удерживающая способность якоря не считается валидированной без отдельной модели якорь/грунт; legacy holding multipliers и `AnchorReserve` не являются selected-authority основанием для прохода.
10. 3D и полноценная динамика не входят в v1.

## Что входит в v1 RC

- произвольная последовательность элементов постановки: буй → соединители → линии → приборы/грузы → якорь;
- выбранная X/Z геометрия и локальные сегментные состояния;
- wave-aware quasi-static selected design demand;
- anchor-end reaction/contact classification;
- local structural capacity/reserve по фактическому положению элемента;
- selected engineering assessment, verdict и main risk;
- 2D отображение выбранной формы;
- PDF и полный technical report как read-model consumers;
- сохранение/загрузка проекта;
- canonical engineering regression suite.

## Явные ограничения v1

В первую версию намеренно не входят:

- full time-domain dynamic solver;
- 6-DOF динамика буя;
- irregular wave spectra / RAO coupling;
- динамические slack/taut transitions;
- distributed line-seabed touchdown/friction mechanics;
- fatigue/cycle counting;
- stochastic extremes;
- 3D-визуализация.

Эти задачи относятся к post-v1 development и не должны менять детерминированное quasi-static ядро v1 побочным образом.

## Обязательные проверки PR

Каждый PR до merge должен пройти exact-head:

```text
.NET Build
Selected Shape Consumer Scan
Report Store Consumer Scan
```

Также перед merge проверяется classic status:

```text
BuoyCalc Windows Build: success
```

Нельзя объединять PR при `failure` или `pending`.

## Release Candidate и выпуск

Текущий F5 процесс:

1. F5-A — заморозить инженерную модель, canonical regression baseline и identity `v1.0.0`.
2. F5-B — сделать детерминированный self-contained `win-x64` RC artifact, SHA-256 и release-flow guardrails.
3. Построить RC только из exact `main` после зелёного CI.
4. Вручную проверить RC на Windows 11: запуск, create/save/load, расчёт, 2D, PDF, technical report.
5. Только после явного подтверждения пользователя допускаются git tag `v1.0.0` и GitHub Release.

Подробности: `RELEASE.md`.

## Основной инженерный источник

```text
Г. О. Берто. Океанографические буи.
Перевод с английского H. O. Berteaux, Buoy Engineering.
Ленинград: Судостроение, 1979.
```

Книжный источник используется для инженерного обоснования. Реализованные формулы, validated boundaries и presentation layers должны оставаться явно разделены.
