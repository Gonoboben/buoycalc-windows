using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public static class ReportBuilder
{
    public static string Build(string projectName, EnvironmentInput environment, BuoyInput buoy, AnchorInput anchor, CalculationResult result)
    {
        var sb = new StringBuilder();
        var tensionRows = SegmentTensionAnalyzer.Build(result);
        var shape = MooringShapeSolver.Build(environment, result);
        var shapeProjection = MooringShapeProjection.Build(shape);
        var shapeForces = MooringShapeForceAnalyzer.Build(result, shapeProjection);
        var shapeTensions = MooringShapeTensionAnalyzer.Build(result, tensionRows, shapeForces);
        var sequencePositions = MooringSequencePositioner.Build(result);
        var discreteLoadTensions = MooringDiscreteLoadTensionAnalyzer.Build(result, tensionRows, sequencePositions);
        var discreteLoadShape = MooringDiscreteLoadShapeBuilder.Build(shape, discreteLoadTensions);
        var diagnostics = EngineeringDiagnostics.Build(environment, result, shape, tensionRows);
        var vectorBalance = MooringVectorBalance.Build(result);
        MooringShapeStore.Set(shape);

        sb.AppendLine("# BuoyCalc Windows — предварительный отчёт");
        sb.AppendLine();
        sb.AppendLine($"Проект: {projectName}");
        sb.AppendLine($"Вердикт: {result.Verdict}");
        sb.AppendLine($"Главный риск: {result.MainRisk}");
        sb.AppendLine($"Инженерная диагностика: {diagnostics.Summary}");
        sb.AppendLine();

        AppendEnvironment(sb, environment);
        AppendBuoy(sb, buoy, shape);
        AppendAnchor(sb, anchor, result);
        AppendTotals(sb, result, tensionRows, shape, shapeProjection, shapeForces, shapeTensions, sequencePositions, discreteLoadTensions, discreteLoadShape, diagnostics);
        AppendDiagnostics(sb, diagnostics);
        AppendVectorBalanceRows(sb, vectorBalance);
        AppendElementRows(sb, result);
        AppendSequencePositionRows(sb, sequencePositions);
        AppendModelCoverageRows(sb, result);
        AppendSegmentRows(sb, result);
        AppendTensionRows(sb, tensionRows);
        AppendShapeRows(sb, shape);
        AppendShapeProjectionRows(sb, shapeProjection);
        AppendShapeForceRows(sb, shapeForces);
        AppendShapeTensionRows(sb, shapeTensions);
        AppendDiscreteLoadTensionRows(sb, discreteLoadTensions);
        AppendDiscreteLoadShapeRows(sb, discreteLoadShape);
        AppendChecks(sb, result);

        sb.AppendLine("## Ограничения");
        sb.AppendLine(shape.MethodNote);
        sb.AppendLine(shapeProjection.MethodNote);
        sb.AppendLine(shapeForces.MethodNote);
        sb.AppendLine(shapeTensions.MethodNote);
        sb.AppendLine(sequencePositions.MethodNote);
        sb.AppendLine(discreteLoadTensions.MethodNote);
        sb.AppendLine(discreteLoadShape.MethodNote);
        sb.AppendLine(vectorBalance.MethodNote);
        sb.AppendLine("v0.35 добавляет альтернативную форму X/Z по натяжениям с дискретными нагрузками. Это сравнительная форма: основной solver пока не заменён.");

        return sb.ToString();
    }

    private static void AppendEnvironment(StringBuilder sb, EnvironmentInput environment)
    {
        sb.AppendLine("## Условия");
        sb.AppendLine($"- Плотность воды базовая: {environment.WaterDensityKgM3:0.####} кг/м³");
        sb.AppendLine($"- Плотность воды расчётная: {environment.EffectiveWaterDensityKgM3:0.####} кг/м³");
        sb.AppendLine($"- Глубина: {environment.DepthM:0.####} м");
        sb.AppendLine($"- Течение базовое: {environment.CurrentSpeedMS:0.####} м/с");
        sb.AppendLine($"- Течение расчётное: {environment.EffectiveCurrentSpeedMS:0.####} м/с");
        sb.AppendLine($"- Профиль течения: {(environment.UseCurrentProfile ? "используется" : "не используется")}");
        sb.AppendLine($"- Волна: {environment.WaveHeightM:0.####} м / {environment.WavePeriodS:0.####} с");
        sb.AppendLine($"- Грунт: {environment.Seabed.Name}");
        sb.AppendLine($"- Множитель грунта: {environment.Seabed.HoldingMultiplier:0.####}");
        sb.AppendLine($"- Примечание по грунту: {environment.Seabed.Note}");
        sb.AppendLine();

        if (environment.EffectiveCurrentProfile.Count == 0)
        {
            return;
        }

        sb.AppendLine("## Профиль течения по глубине");
        sb.AppendLine("| Глубина, м | U East, м/с | V North, м/с | W Vertical, м/с | |U|, м/с | ρ, кг/м³ |");
        sb.AppendLine("|---:|---:|---:|---:|---:|---:|");
        foreach (var p in environment.EffectiveCurrentProfile)
        {
            sb.AppendLine($"| {p.DepthM:0.####} | {p.EastCurrentMS:0.####} | {p.NorthCurrentMS:0.####} | {p.VerticalCurrentMS:0.####} | {p.SpeedMS:0.####} | {p.WaterDensityKgM3:0.####} |");
        }
        sb.AppendLine();
    }

    private static void AppendBuoy(StringBuilder sb, BuoyInput buoy, MooringShapeResult shape)
    {
        sb.AppendLine("## Буй");
        sb.AppendLine($"- Название: {buoy.Name}");
        sb.AppendLine($"- Объём: {buoy.VolumeM3:0.####} м³");
        sb.AppendLine($"- Масса: {buoy.WeightKg:0.####} кг");
        sb.AppendLine($"- Площадь: {buoy.ProjectedAreaM2:0.####} м²");
        sb.AppendLine($"- Cd: {buoy.DragCoefficient:0.####}");
        sb.AppendLine($"- Состояние по форме: {DisplayBuoyState(shape.BuoyState)}");
        sb.AppendLine($"- Глубина узла буя: {shape.BuoyPoint?.ZDepthM ?? 0:0.####} м");
        sb.AppendLine();
    }

    private static void AppendAnchor(StringBuilder sb, AnchorInput anchor, CalculationResult result)
    {
        sb.AppendLine("## Якорь");
        sb.AppendLine($"- Название: {anchor.Name}");
        sb.AppendLine($"- Тип: {anchor.Type}");
        sb.AppendLine($"- Материал: {anchor.Material}");
        sb.AppendLine($"- Масса: {anchor.WeightAirKg:0.####} кг");
        sb.AppendLine($"- Объём: {anchor.VolumeM3:0.####} м³");
        sb.AppendLine($"- Вес якоря в воде: {result.AnchorWeightWaterKg:0.####} кг");
        sb.AppendLine($"- Базовый коэф. удержания якоря: {result.AnchorBaseHoldingCoefficient:0.####}");
        sb.AppendLine($"- Множитель типа якоря: {result.AnchorTypeMultiplier:0.####}");
        sb.AppendLine($"- Множитель грунта: {result.SeabedHoldingMultiplier:0.####}");
        sb.AppendLine("- Формула удержания: вес в воде × K якоря × K типа × K грунта");
        sb.AppendLine();
    }

    private static void AppendTotals(StringBuilder sb, CalculationResult result, IReadOnlyList<SegmentTensionRow> tensionRows, MooringShapeResult shape, MooringShapeProjectionResult shapeProjection, MooringShapeForceResult shapeForces, MooringShapeTensionResult shapeTensions, MooringSequencePositionResult sequencePositions, MooringDiscreteLoadTensionResult discreteLoadTensions, MooringDiscreteLoadShapeResult discreteLoadShape, EngineeringDiagnosticsResult diagnostics)
    {
        sb.AppendLine("## Итоги");
        sb.AppendLine($"- Полная плавучесть буя: {result.BuoyancyKg:0.####} кг");
        sb.AppendLine($"- Вес постановки в воде: {result.TotalWeightWaterKg:0.####} кг");
        sb.AppendLine($"- Чистая плавучесть: {result.NetBuoyancyKg:0.####} кг");
        sb.AppendLine($"- Сила течения исходной модели: {result.CurrentForceN:0.####} Н");
        sb.AppendLine($"- Волновая составляющая: {result.WaveForceN:0.####} Н");
        sb.AppendLine($"- Горизонтальная сила исходной модели: {result.HorizontalForceN:0.####} Н");
        sb.AppendLine($"- Расчётная нагрузка для проверки слабого звена: {result.TensionKn:0.####} кН");
        sb.AppendLine($"- Слабое звено: {result.WeakLinkName}");
        sb.AppendLine($"- MBL слабого звена: {result.WeakLinkBreakingLoadKn:0.####} кН");
        sb.AppendLine($"- WLL слабого звена: {result.WorkingLoadKn:0.####} кН");
        sb.AppendLine($"- Запас по слабому звену: {result.TensionReserve:0.####}");
        sb.AppendLine($"- Требуемое удержание якоря: {result.RequiredAnchorHoldingKg:0.####} кг");
        sb.AppendLine($"- Удержание якоря: {result.AnchorHoldingKg:0.####} кг");
        sb.AppendLine($"- Запас якоря: {result.AnchorReserve:0.####}");
        sb.AppendLine($"- Длина линии: {result.LineLengthM:0.####} м");
        sb.AppendLine($"- Старая оценка сноса: {result.EstimatedOffsetM:0.####} м");
        sb.AppendLine($"- Дискретных элементов с координатой s: {sequencePositions.DiscreteElementCount}; вес в воде: {sequencePositions.DiscreteWeightWaterKg:0.####} кг; Fx: {sequencePositions.DiscreteCurrentForceN:0.####} Н");
        sb.AppendLine($"- Диагностика: {diagnostics.Summary}");

        if (tensionRows.Count > 0)
        {
            var maxTension = tensionRows.OrderByDescending(x => x.TensionKn).First();
            sb.AppendLine($"- Макс. натяжение линии по сегментной модели: {maxTension.TensionKn:0.####} кН, сегмент №{maxTension.Number}, z≈{maxTension.EstimatedDepthM:0.####} м");
        }

        if (shape.Nodes.Count > 0)
        {
            sb.AppendLine($"- Снос формы X/Z: {shape.HorizontalOffsetM:0.####} м");
            sb.AppendLine($"- Глубина якорного узла X/Z: {shape.AnchorPoint?.ZDepthM ?? 0:0.####} м");
            sb.AppendLine($"- Невязка якорной глубины: {shape.VerticalResidualM:0.####} м");
            sb.AppendLine($"- Невязка solver формы: {shape.ConvergenceResidualM:0.####} м");
            sb.AppendLine($"- Итераций solver формы: {shape.IterationCount}");
            sb.AppendLine($"- ΣdX формы: {shapeProjection.SumDeltaXM:0.####} м");
            sb.AppendLine($"- ΣdZ формы: {shapeProjection.SumDeltaZM:0.####} м");
            sb.AppendLine($"- Статус проекций формы: {(shapeProjection.GeometryClosed ? "OK" : "WARNING")}");
            sb.AppendLine($"- Shape-based сила линии: {shapeForces.ShapeLineForceN:0.####} Н");
            sb.AppendLine($"- Отличие shape-based силы линии: {shapeForces.DifferenceN:0.####} Н ({shapeForces.RelativeDifference:0.####})");
            sb.AppendLine($"- Статус shape-based сил: {(shapeForces.WithinTolerance ? "OK" : "INFO: отличается от старой оценки")}");
            sb.AppendLine($"- Top T старая / shape: {shapeTensions.TopOriginalTensionKn:0.####} / {shapeTensions.TopShapeTensionKn:0.####} кН");
            sb.AppendLine($"- Макс. отличие shape-based натяжения: {shapeTensions.MaxTensionDifferenceKn:0.####} кН; угол Δmax={shapeTensions.MaxAngleDifferenceDeg:0.####}°");
            sb.AppendLine($"- Статус shape-based натяжений: {(shapeTensions.WithinTolerance ? "OK" : "INFO: отличается от старой оценки")}");
            sb.AppendLine($"- Top T старая / с дискретными нагрузками: {discreteLoadTensions.TopOriginalTensionKn:0.####} / {discreteLoadTensions.TopDiscreteTensionKn:0.####} кН");
            sb.AppendLine($"- Макс. отличие натяжения от дискретных нагрузок: {discreteLoadTensions.MaxTensionDifferenceKn:0.####} кН; угол Δmax={discreteLoadTensions.MaxAngleDifferenceDeg:0.####}°");
            sb.AppendLine($"- Статус натяжений с дискретными нагрузками: {(discreteLoadTensions.WithinTolerance ? "OK" : "INFO: дискретные нагрузки заметно меняют натяжение")}");
            sb.AppendLine($"- Снос альтернативной формы с дискретными нагрузками: {discreteLoadShape.DiscreteHorizontalOffsetM:0.####} м");
            sb.AppendLine($"- Отличие альтернативной формы от основной: ΔXснос={discreteLoadShape.OffsetDifferenceM:0.####} м; max Δузла={discreteLoadShape.MaxNodeDeltaM:0.####} м");
            sb.AppendLine($"- Статус альтернативной формы: {(discreteLoadShape.Converged ? "OK" : "WARNING")}");
            sb.AppendLine($"- Узлов формы: {shape.Nodes.Count}");
        }
        sb.AppendLine();
    }

    private static void AppendDiagnostics(StringBuilder sb, EngineeringDiagnosticsResult diagnostics)
    {
        sb.AppendLine("## Контроль инженерной модели");
        sb.AppendLine($"Общий статус: {EngineeringDiagnostics.DisplaySeverity(diagnostics.OverallSeverity)} — {diagnostics.Summary}.");
        sb.AppendLine();
        sb.AppendLine("| Проверка | Значение | Допуск / критерий | Статус | Примечание |");
        sb.AppendLine("|---|---:|---|---|---|");
        foreach (var row in diagnostics.Rows)
        {
            sb.AppendLine($"| {Escape(row.CheckName)} | {Escape(row.Value)} | {Escape(row.Tolerance)} | {EngineeringDiagnostics.DisplaySeverity(row.Severity)} | {Escape(row.Note)} |");
        }
        sb.AppendLine();
    }

    private static void AppendVectorBalanceRows(StringBuilder sb, MooringVectorBalanceResult balance)
    {
        sb.AppendLine("## Векторная ведомость сил постановки");
        sb.AppendLine("Положительное направление X принято по направлению горизонтальной нагрузки. Положительное направление Z принято вверх.");
        sb.AppendLine("Эта таблица показывает силовые члены, уже переданные расчётным ядром. Она не является результатом итерационного solver равновесной формы.");
        sb.AppendLine();
        sb.AppendLine("| № | Тип | Элемент | Fx, Н | Fz, Н | Примечание |");
        sb.AppendLine("|---:|---|---|---:|---:|---|");
        var number = 1;
        foreach (var term in balance.Terms)
        {
            sb.AppendLine($"| {number++} | {Escape(term.Kind)} | {Escape(term.Name)} | {term.FxN:0.####} | {term.FzN:0.####} | {Escape(term.Note)} |");
        }
        sb.AppendLine($"|  | **Σ** | **Сумма учтённых сил** | **{balance.SumExternalFxN:0.####}** | **{balance.SumExternalFzN:0.####}** | Сумма строк таблицы. |");
        sb.AppendLine($"|  | **R** | **Требуемая реакция для ΣF=0** | **{balance.RequiredReactionFxN:0.####}** | **{balance.RequiredReactionFzN:0.####}** | Вычислена как минус сумма учтённых сил; пока не найдена solver-ом. |");
        sb.AppendLine();
        sb.AppendLine($"- Горизонтальная удерживающая способность якоря: {balance.AnchorHorizontalCapacityN:0.####} Н");
        sb.AppendLine($"- Запас горизонтального удержания по требуемой реакции Rx: {balance.AnchorHorizontalReserve:0.####}");
        sb.AppendLine($"- Статус баланса: {(balance.IsSolved ? "решён" : "каркас собран; реакции не решены")}");
        sb.AppendLine($"- Методическое примечание: {balance.MethodNote}");
        sb.AppendLine();
    }

    private static void AppendElementRows(StringBuilder sb, CalculationResult result)
    {
        sb.AppendLine("## Таблица элементов");
        sb.AppendLine("| № | Тип | Элемент | Пресет | Длина, м | Кол-во | Вес в воде, кг | Площадь, м² | Cd | Сила, Н | MBL, кН | WLL, кН | Запас | Статус |");
        sb.AppendLine("|---:|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in result.ElementRows)
        {
            sb.AppendLine($"| {row.Number} | {Escape(row.Kind)} | {Escape(row.Title)} | {Escape(row.PresetName)} | {row.LengthM:0.####} | {row.Count} | {row.WeightWaterKg:0.####} | {row.ProjectedAreaM2:0.####} | {row.DragCoefficient:0.####} | {row.CurrentForceN:0.####} | {row.BreakingLoadKn:0.####} | {row.WorkingLoadKn:0.####} | {row.Reserve:0.####} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
    }

    private static void AppendSequencePositionRows(StringBuilder sb, MooringSequencePositionResult positions)
    {
        sb.AppendLine("## Позиционная модель последовательности по s");
        sb.AppendLine("Координата s отсчитывается вдоль линии от верхнего конца к якорю. Линейные элементы занимают интервал s0–s1; соединители, приборы, буй и якорь имеют точечную позицию s.");
        sb.AppendLine($"Длина линии по позиционной модели: {positions.TotalLineLengthM:0.####} м; распределённых участков: {positions.DistributedElementCount}; дискретных элементов без буя/якоря: {positions.DiscreteElementCount}.");
        sb.AppendLine();
        sb.AppendLine("| № | Тип | Элемент | Пресет | s0, м | s1, м | s, м | L, м | Вес в воде, кг | Fx, Н | Роль в solver | Следующий шаг |");
        sb.AppendLine("|---:|---|---|---|---:|---:|---:|---:|---:|---:|---|---|");
        foreach (var row in positions.Rows)
        {
            sb.AppendLine($"| {row.Number} | {Escape(row.Kind)} | {Escape(row.Title)} | {Escape(row.PresetName)} | {row.StartAlongLineM:0.####} | {row.EndAlongLineM:0.####} | {row.PositionAlongLineM:0.####} | {row.LengthM:0.####} | {row.WeightWaterKg:0.####} | {row.CurrentForceN:0.####} | {Escape(row.SolverRole)} | {Escape(row.NextStepNote)} |");
        }
        sb.AppendLine();
        sb.AppendLine(positions.MethodNote);
        sb.AppendLine();
    }

    private static void AppendModelCoverageRows(StringBuilder sb, CalculationResult result)
    {
        sb.AppendLine("## Область учёта элементов в текущей модели");
        sb.AppendLine("Эта таблица показывает, где элемент уже участвует в расчётах. В v0.35 дискретные элементы добавлены в альтернативные натяжения и альтернативную форму X/Z, но основной solver формы ещё не заменён.");
        sb.AppendLine();
        sb.AppendLine("| № | Элемент | Тип | Ведомость элементов | Векторный баланс | Позиция s | Дискретные натяжения | Альт. форма X/Z | Основная форма X/Z | Основные натяжения | Примечание |");
        sb.AppendLine("|---:|---|---|---|---|---|---|---|---|---|---|");
        foreach (var row in result.ElementRows)
        {
            var shapeScope = row.Kind switch
            {
                "Буй" => "граничный узел",
                "Якорь" => "граничный узел",
                "Линия" => "да, сегменты",
                _ => "нет, дискретный узел пока не вставлен"
            };
            var tensionScope = row.Kind == "Линия" ? "да" : "нет, только общая сила/вес";
            var discreteTensionScope = row.Kind switch
            {
                "Буй" => "нет, граничное условие",
                "Якорь" => "нет, граничное условие",
                "Линия" => "как распределённый участок",
                _ => "да, как локальная нагрузка по s"
            };
            var alternativeShapeScope = row.Kind switch
            {
                "Буй" => "граничный узел",
                "Якорь" => "граничный узел",
                "Линия" => "да, через новые углы",
                _ => "влияет через натяжения, но не как отдельный узел"
            };
            var note = row.Kind switch
            {
                "Линия" => "используется в SegmentRows, ShapeRows, shape-based силах и натяжениях",
                "Буй" => "задаёт верхнее граничное условие и плавучесть",
                "Якорь" => "задаёт нижнее граничное условие и удержание",
                _ => "следующий слой: вставить как отдельный локальный узел формы"
            };
            sb.AppendLine($"| {row.Number} | {Escape(row.Title)} | {Escape(row.Kind)} | да | да | да | {discreteTensionScope} | {alternativeShapeScope} | {shapeScope} | {tensionScope} | {Escape(note)} |");
        }
        sb.AppendLine();
    }

    private static void AppendSegmentRows(StringBuilder sb, CalculationResult result)
    {
        if (result.SegmentRows.Count == 0) return;
        sb.AppendLine("## Расчётные сегменты линии");
        sb.AppendLine($"Линия разбита на {result.SegmentRows.Count} сегментов. В таблице показаны первые 40 и последние 40 сегментов, чтобы были видны верхние и нижние участки линии.");
        sb.AppendLine();
        sb.AppendLine("| № | Элемент | Пресет | s0, м | s1, м | L, м | z, м | U | V | W | |Uгор| | ρ | A, м² | Cd | Сила, Н |");
        sb.AppendLine("|---:|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var row in SampleRows(result.SegmentRows, 40, 40))
        {
            sb.AppendLine($"| {row.Number} | {Escape(row.SourceElement)} | {Escape(row.RopePresetName)} | {row.StartLengthM:0.####} | {row.EndLengthM:0.####} | {row.SegmentLengthM:0.####} | {row.EstimatedDepthM:0.####} | {row.EastCurrentMS:0.####} | {row.NorthCurrentMS:0.####} | {row.VerticalCurrentMS:0.####} | {row.LocalSpeedMS:0.####} | {row.WaterDensityKgM3:0.####} | {row.ProjectedAreaM2:0.####} | {row.DragCoefficient:0.####} | {row.CurrentForceN:0.####} |");
        }
        sb.AppendLine();
        sb.AppendLine($"Суммарная сила течения по сегментам линии: {result.SegmentRows.Sum(x => x.CurrentForceN):0.####} Н");
        sb.AppendLine();
    }

    private static void AppendTensionRows(StringBuilder sb, IReadOnlyList<SegmentTensionRow> rows)
    {
        if (rows.Count == 0) return;
        var maxTension = rows.OrderByDescending(x => x.TensionKn).First();
        sb.AppendLine("## Натяжения по сегментам линии");
        sb.AppendLine($"Расчёт ведётся снизу вверх: от якоря к бую. Показаны первые 40 и последние 40 сегментов.");
        sb.AppendLine($"Максимальное оценочное натяжение линии: {maxTension.TensionKn:0.####} кН на сегменте №{maxTension.Number}.");
        sb.AppendLine();
        sb.AppendLine("| № | Элемент | z, м | L, м | Вес в воде, кг | Fтек, Н | ΣFгор, Н | ΣFверт, Н | T, кН | Угол от вертикали, ° | Статус |");
        sb.AppendLine("|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in SampleRows(rows, 40, 40))
        {
            sb.AppendLine($"| {row.Number} | {Escape(row.SourceElement)} | {row.EstimatedDepthM:0.####} | {row.SegmentLengthM:0.####} | {row.WeightWaterKg:0.####} | {row.SegmentCurrentForceN:0.####} | {row.CumulativeHorizontalForceN:0.####} | {row.CumulativeVerticalForceN:0.####} | {row.TensionKn:0.####} | {row.AngleFromVerticalDeg:0.####} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
    }

    private static void AppendShapeRows(StringBuilder sb, MooringShapeResult shape)
    {
        if (shape.Nodes.Count == 0) return;
        sb.AppendLine("## Расчётная форма постановки X/Z");
        sb.AppendLine("Координаты являются выходом инженерного слоя MooringShapeSolver. Визуализация должна только отображать эти точки.");
        sb.AppendLine($"Состояние буя: {DisplayBuoyState(shape.BuoyState)}. Сходимость: {(shape.Converged ? "да" : "нет")}.");
        sb.AppendLine($"Итерации solver: {shape.IterationCount}, невязка: {shape.ConvergenceResidualM:0.####} м, scale={shape.AngleScale:0.####}. Критерий: {shape.ConvergenceCriterion}.");
        sb.AppendLine($"Показаны первые 45 и последние 45 узлов из {shape.Nodes.Count}.");
        sb.AppendLine();
        sb.AppendLine("| Узел | Сегмент | Элемент | s, м | X, м | Z, м | Lсег, м | Угол, ° | T, кН | Статус |");
        sb.AppendLine("|---:|---:|---|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in SampleRows(shape.Nodes, 45, 45))
        {
            sb.AppendLine($"| {row.Number} | {row.SegmentNumber} | {Escape(row.Label)} | {row.AlongLineM:0.####} | {row.XOffsetM:0.####} | {row.ZDepthM:0.####} | {row.SegmentLengthM:0.####} | {row.SegmentAngleFromVerticalDeg:0.####} | {row.SegmentTensionKn:0.####} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
    }

    private static void AppendShapeProjectionRows(StringBuilder sb, MooringShapeProjectionResult projection)
    {
        if (projection.Rows.Count == 0) return;
        sb.AppendLine("## Проекции формы X/Z по сегментам");
        sb.AppendLine("Этот раздел связывает узлы MooringShapeSolver с геометрией сегментов: dX, dZ, фактическая длина по координатам и угол от вертикали.");
        sb.AppendLine($"ΣdX={projection.SumDeltaXM:0.####} м; ΣdZ={projection.SumDeltaZM:0.####} м; Lпо координатам={projection.TotalProjectedLengthM:0.####} м; Lсегментов={projection.TotalSegmentLengthM:0.####} м.");
        sb.AppendLine($"Невязка длины={projection.LengthResidualM:0.####} м; невязка X конечных точек={projection.EndpointResidualXM:0.####} м; невязка Z конечных точек={projection.EndpointResidualZM:0.####} м; статус={(projection.GeometryClosed ? "OK" : "WARNING")}.");
        sb.AppendLine($"Максимальный угол от вертикали={projection.MaxAngleFromVerticalDeg:0.####}°; средний угол={projection.AverageAngleFromVerticalDeg:0.####}°.");
        sb.AppendLine();
        sb.AppendLine("| № | Сегмент | Элемент | L, м | dX, м | dZ, м | L по X/Z, м | Невязка L, м | Угол, ° | T, кН | Статус |");
        sb.AppendLine("|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in SampleRows(projection.Rows, 45, 45))
        {
            sb.AppendLine($"| {row.Number} | {row.SegmentNumber} | {Escape(row.Label)} | {row.SegmentLengthM:0.####} | {row.DeltaXM:0.####} | {row.DeltaZM:0.####} | {row.ProjectedLengthM:0.####} | {row.LengthResidualM:0.####} | {row.AngleFromVerticalDeg:0.####} | {row.TensionKn:0.####} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
        sb.AppendLine(projection.MethodNote);
        sb.AppendLine();
    }

    private static void AppendShapeForceRows(StringBuilder sb, MooringShapeForceResult forces)
    {
        if (forces.Rows.Count == 0) return;
        sb.AppendLine("## Shape-based силы линии по ориентации сегментов");
        sb.AppendLine("Эта таблица сравнивает старую силу сегмента с оценкой по нормальной составляющей скорости к фактической X/Z-ориентации сегмента.");
        sb.AppendLine($"Старая ΣFлинии={forces.OriginalLineForceN:0.####} Н; shape-based ΣFлинии={forces.ShapeLineForceN:0.####} Н; Δ={forces.DifferenceN:0.####} Н ({forces.RelativeDifference:0.####}).");
        sb.AppendLine($"Максимальное отличие строки={forces.MaxRowDifferenceN:0.####} Н; статус={(forces.WithinTolerance ? "OK" : "INFO: заметное отличие от старой оценки")}");
        sb.AppendLine();
        sb.AppendLine("| № | Сегмент | Элемент | L, м | U, м/с | Uнорм, м/с | Угол, ° | F старая, Н | F shape, Н | ΔF, Н | Ratio | Статус |");
        sb.AppendLine("|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in SampleRows(forces.Rows, 45, 45))
        {
            sb.AppendLine($"| {row.Number} | {row.SegmentNumber} | {Escape(row.Label)} | {row.SegmentLengthM:0.####} | {row.LocalSpeedMS:0.####} | {row.NormalSpeedMS:0.####} | {row.AngleFromVerticalDeg:0.####} | {row.OriginalForceN:0.####} | {row.ShapeForceN:0.####} | {row.DifferenceN:0.####} | {row.Ratio:0.####} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
        sb.AppendLine(forces.MethodNote);
        sb.AppendLine();
    }

    private static void AppendShapeTensionRows(StringBuilder sb, MooringShapeTensionResult tensions)
    {
        if (tensions.Rows.Count == 0) return;
        sb.AppendLine("## Shape-based натяжения линии");
        sb.AppendLine("Эта таблица пересчитывает накопленные натяжения снизу вверх, используя shape-based силы сегментов вместо старых сил SegmentCalculationRow.");
        sb.AppendLine($"Top T старая={tensions.TopOriginalTensionKn:0.####} кН; Top T shape={tensions.TopShapeTensionKn:0.####} кН; относительное отличие={tensions.RelativeTopTensionDifference:0.####}.");
        sb.AppendLine($"Max T старая={tensions.MaxOriginalTensionKn:0.####} кН; Max T shape={tensions.MaxShapeTensionKn:0.####} кН; Max ΔT={tensions.MaxTensionDifferenceKn:0.####} кН; Max Δугла={tensions.MaxAngleDifferenceDeg:0.####}°.");
        sb.AppendLine($"Статус={(tensions.WithinTolerance ? "OK" : "INFO: shape-based натяжение заметно отличается от старого")}");
        sb.AppendLine();
        sb.AppendLine("| № | Сегмент | Элемент | z, м | L, м | Вес, кг | F старая, Н | F shape, Н | T старая, кН | T shape, кН | ΔT, кН | Угол старый, ° | Угол shape, ° | Δугла, ° | Статус |");
        sb.AppendLine("|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in SampleRows(tensions.Rows, 45, 45))
        {
            sb.AppendLine($"| {row.Number} | {row.SegmentNumber} | {Escape(row.SourceElement)} | {row.EstimatedDepthM:0.####} | {row.SegmentLengthM:0.####} | {row.WeightWaterKg:0.####} | {row.OriginalSegmentForceN:0.####} | {row.ShapeSegmentForceN:0.####} | {row.OriginalTensionKn:0.####} | {row.ShapeTensionKn:0.####} | {row.TensionDifferenceKn:0.####} | {row.OriginalAngleFromVerticalDeg:0.####} | {row.ShapeAngleFromVerticalDeg:0.####} | {row.AngleDifferenceDeg:0.####} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
        sb.AppendLine(tensions.MethodNote);
        sb.AppendLine();
    }

    private static void AppendDiscreteLoadTensionRows(StringBuilder sb, MooringDiscreteLoadTensionResult tensions)
    {
        if (tensions.Rows.Count == 0) return;
        sb.AppendLine("## Натяжения линии с дискретными нагрузками по s");
        sb.AppendLine("Эта таблица добавляет к распределённой линии локальные нагрузки приборов и соединителей, у которых в v0.33 появилась координата s. Расчёт идёт снизу вверх: в каждом сечении учитываются дискретные элементы ниже или на этом s.");
        sb.AppendLine($"Дискретные нагрузки: вес в воде={tensions.TotalDiscreteWeightWaterKg:0.####} кг; Fx={tensions.TotalDiscreteForceN:0.####} Н.");
        sb.AppendLine($"Top T старая={tensions.TopOriginalTensionKn:0.####} кН; Top T с дискретными нагрузками={tensions.TopDiscreteTensionKn:0.####} кН; относительное отличие={tensions.RelativeTopTensionDifference:0.####}.");
        sb.AppendLine($"Max T старая={tensions.MaxOriginalTensionKn:0.####} кН; Max T с дискретными нагрузками={tensions.MaxDiscreteTensionKn:0.####} кН; Max ΔT={tensions.MaxTensionDifferenceKn:0.####} кН; Max Δугла={tensions.MaxAngleDifferenceDeg:0.####}°.");
        sb.AppendLine($"Статус={(tensions.WithinTolerance ? "OK" : "INFO: дискретные нагрузки заметно меняют натяжение")}");
        sb.AppendLine();
        sb.AppendLine("### Дискретные нагрузки по s");
        sb.AppendLine("| № | Тип | Элемент | s, м | Вес в воде, кг | Fx, Н |");
        sb.AppendLine("|---:|---|---|---:|---:|---:|");
        foreach (var load in tensions.DiscreteLoads)
        {
            sb.AppendLine($"| {load.Number} | {Escape(load.Kind)} | {Escape(load.Title)} | {load.PositionAlongLineM:0.####} | {load.WeightWaterKg:0.####} | {load.CurrentForceN:0.####} |");
        }
        sb.AppendLine();
        sb.AppendLine("### Накопленные натяжения с дискретными нагрузками");
        sb.AppendLine("| № | Сегмент | Элемент | s0, м | s1, м | z, м | Fсег, Н | Вес дискр. ниже, кг | Fx дискр. ниже, Н | T старая, кН | T дискр., кН | ΔT, кН | Угол старый, ° | Угол дискр., ° | Δугла, ° | Статус |");
        sb.AppendLine("|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in SampleRows(tensions.Rows, 45, 45))
        {
            sb.AppendLine($"| {row.Number} | {row.SegmentNumber} | {Escape(row.SourceElement)} | {row.StartAlongLineM:0.####} | {row.EndAlongLineM:0.####} | {row.EstimatedDepthM:0.####} | {row.SegmentForceN:0.####} | {row.DiscreteWeightBelowKg:0.####} | {row.DiscreteForceBelowN:0.####} | {row.OriginalTensionKn:0.####} | {row.DiscreteTensionKn:0.####} | {row.TensionDifferenceKn:0.####} | {row.OriginalAngleFromVerticalDeg:0.####} | {row.DiscreteAngleFromVerticalDeg:0.####} | {row.AngleDifferenceDeg:0.####} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
        sb.AppendLine(tensions.MethodNote);
        sb.AppendLine();
    }

    private static void AppendDiscreteLoadShapeRows(StringBuilder sb, MooringDiscreteLoadShapeResult shape)
    {
        if (shape.Rows.Count == 0) return;
        sb.AppendLine("## Альтернативная форма X/Z с дискретными нагрузками");
        sb.AppendLine("Эта таблица строит сравнительную форму по углам, полученным из натяжений с дискретными нагрузками. Основной solver пока не заменяется.");
        sb.AppendLine($"Снос основной формы={shape.OriginalHorizontalOffsetM:0.####} м; снос альтернативной формы={shape.DiscreteHorizontalOffsetM:0.####} м; Δсноса={shape.OffsetDifferenceM:0.####} м.");
        sb.AppendLine($"Глубина якоря={shape.AnchorDepthM:0.####} м; невязка={shape.VerticalResidualM:0.####} м; max Δузла={shape.MaxNodeDeltaM:0.####} м; scale={shape.AngleScale:0.####}; итераций={shape.IterationCount}; статус={(shape.Converged ? "OK" : "WARNING")}.");
        sb.AppendLine();
        sb.AppendLine("| Узел | Сегмент | Элемент | s, м | X дискр., м | Z дискр., м | Lсег, м | Угол старый, ° | Угол дискр., ° | Угол формы, ° | T дискр., кН | X осн., м | Z осн., м | ΔX, м | ΔZ, м | Статус |");
        sb.AppendLine("|---:|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in SampleRows(shape.Rows, 45, 45))
        {
            sb.AppendLine($"| {row.Number} | {row.SegmentNumber} | {Escape(row.SourceElement)} | {row.AlongLineM:0.####} | {row.XOffsetM:0.####} | {row.ZDepthM:0.####} | {row.SegmentLengthM:0.####} | {row.OriginalAngleFromVerticalDeg:0.####} | {row.DiscreteAngleFromVerticalDeg:0.####} | {row.UsedAngleFromVerticalDeg:0.####} | {row.DiscreteTensionKn:0.####} | {row.OriginalXOffsetM:0.####} | {row.OriginalZDepthM:0.####} | {row.DeltaXM:0.####} | {row.DeltaZM:0.####} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
        sb.AppendLine(shape.MethodNote);
        sb.AppendLine();
    }

    private static void AppendChecks(StringBuilder sb, CalculationResult result)
    {
        sb.AppendLine("## Проверки");
        foreach (var check in result.Checks)
        {
            sb.AppendLine($"- {check}");
        }
        sb.AppendLine();
    }

    private static IEnumerable<T> SampleRows<T>(IReadOnlyList<T> rows, int firstCount, int lastCount)
    {
        if (rows.Count <= firstCount + lastCount)
        {
            return rows;
        }

        return rows.Take(firstCount).Concat(rows.Skip(rows.Count - lastCount));
    }

    private static string DisplayBuoyState(BuoyShapeState state)
    {
        return state switch
        {
            BuoyShapeState.Surface => "на поверхности",
            BuoyShapeState.Submerged => "под водой",
            BuoyShapeState.Overloaded => "перегружен / отрицательная плавучесть",
            _ => "не определено"
        };
    }

    private static string Escape(string value)
    {
        return (value ?? string.Empty).Replace("|", "/");
    }
}
