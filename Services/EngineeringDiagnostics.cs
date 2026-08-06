using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public enum EngineeringCheckSeverity
{
    Info,
    Ok,
    Warning,
    Error
}

public sealed record EngineeringDiagnosticRow(
    string CheckName,
    string Value,
    string Tolerance,
    EngineeringCheckSeverity Severity,
    string Note);

public sealed record EngineeringForceResiduals(
    double LineSumFxN,
    double LineSumFzN,
    double TopTensionFxN,
    double TopTensionFzN,
    double ResidualFxN,
    double ResidualFzN,
    double RelativeResidualFx,
    double RelativeResidualFz,
    bool InternalLineBalanceOk);

public sealed record EngineeringDiagnosticsResult(
    IReadOnlyList<EngineeringDiagnosticRow> Rows,
    EngineeringForceResiduals ForceResiduals,
    EngineeringCheckSeverity OverallSeverity,
    string Summary);

public static class EngineeringDiagnostics
{
    private const double G = 9.80665;
    private const double MaximumAllowedSegmentLengthM = 0.20;
    private const double SegmentLengthToleranceM = 1e-9;

    public static EngineeringDiagnosticsResult Build(
        EnvironmentInput environment,
        CalculationResult result,
        MooringShapeResult shape,
        IReadOnlyList<SegmentTensionRow> tensionRows)
    {
        var rows = new List<EngineeringDiagnosticRow>();
        var depthM = Math.Max(0, environment.DepthM);
        var lineLengthM = Math.Max(0, result.LineLengthM);
        var segmentLengthM = result.SegmentRows.Sum(x => x.SegmentLengthM);
        var segmentLengthResidualM = Math.Abs(segmentLengthM - lineLengthM);
        var relativeSegmentLengthResidual = segmentLengthResidualM / Math.Max(1.0, Math.Abs(lineLengthM));
        var scalarCurrentIsActive = !environment.UseCurrentProfile || environment.EffectiveCurrentProfile.Count == 0;
        var effectiveWaterDensityKgM3 = environment.EffectiveWaterDensityKgM3;
        var invalidSegmentDensityCount = result.SegmentRows.Count(x => !double.IsFinite(x.WaterDensityKgM3) || x.WaterDensityKgM3 <= 0);
        var minimumSegmentDensityKgM3 = result.SegmentRows.Count > 0 ? result.SegmentRows.Min(x => x.WaterDensityKgM3) : double.NaN;
        var invalidProfileDepthCount = environment.UseCurrentProfile
            ? environment.EffectiveCurrentProfile.Count(x => !double.IsFinite(x.DepthM) || x.DepthM < 0)
            : 0;
        var minimumProfileDepthM = environment.UseCurrentProfile && environment.EffectiveCurrentProfile.Count > 0
            ? environment.EffectiveCurrentProfile.Min(x => x.DepthM)
            : double.NaN;
        var duplicateProfileDepthGroups = environment.UseCurrentProfile && environment.EffectiveCurrentProfile.Count >= 2
            ? environment.EffectiveCurrentProfile
                .GroupBy(x => x.DepthM)
                .Where(x => x.Count() > 1)
                .OrderBy(x => x.Key)
                .ToList()
            : new List<IGrouping<double, CurrentProfilePointInput>>();
        var duplicateProfilePointCount = duplicateProfileDepthGroups.Sum(x => x.Count() - 1);
        var duplicateProfileDepthText = string.Join(", ", duplicateProfileDepthGroups.Take(8).Select(x => $"{x.Key:0.####}"));
        if (duplicateProfileDepthGroups.Count > 8)
        {
            duplicateProfileDepthText += ", ...";
        }
        var nonPositiveSegmentCount = result.SegmentRows.Count(x => x.SegmentLengthM <= 0);
        var minimumSegmentLengthM = result.SegmentRows.Count > 0 ? result.SegmentRows.Min(x => x.SegmentLengthM) : double.NaN;
        var excessiveSegmentLengthCount = result.SegmentRows.Count(x => x.SegmentLengthM > MaximumAllowedSegmentLengthM + SegmentLengthToleranceM);
        var maximumSegmentLengthM = result.SegmentRows.Count > 0 ? result.SegmentRows.Max(x => x.SegmentLengthM) : double.NaN;
        var buoyDepthM = shape.BuoyPoint?.ZDepthM ?? double.NaN;
        var anchorDepthM = shape.AnchorPoint?.ZDepthM ?? double.NaN;
        var lengthResidualM = shape.AnchorPoint is null ? double.NaN : Math.Abs(shape.AnchorPoint.AlongLineM - lineLengthM);
        var anchorDepthResidualM = double.IsNaN(anchorDepthM) ? double.NaN : Math.Abs(anchorDepthM - depthM);
        var maxTensionKn = tensionRows.Count > 0 ? tensionRows.Max(x => x.TensionKn) : result.TensionKn;
        var forceResiduals = BuildForceResiduals(tensionRows);
        var vectorBalance = MooringVectorBalance.Build(result);
        var horizontalForceResidualN = Math.Abs(vectorBalance.SumExternalFxN - result.HorizontalForceN);
        var relativeHorizontalForceResidual = horizontalForceResidualN / Math.Max(1.0, Math.Abs(result.HorizontalForceN));
        var lineElementWeightWaterKg = result.ElementRows.Where(x => x.Kind == "Линия").Sum(x => x.WeightWaterKg);
        var segmentWeightWaterKg = result.SegmentRows.Sum(x => x.WeightWaterKg);
        var lineWeightResidualKg = Math.Abs(segmentWeightWaterKg - lineElementWeightWaterKg);
        var relativeLineWeightResidual = lineWeightResidualKg / Math.Max(1.0, Math.Abs(lineElementWeightWaterKg));
        var lineElementForceN = result.ElementRows.Where(x => x.Kind == "Линия").Sum(x => x.CurrentForceN);
        var segmentForceN = result.SegmentRows.Sum(x => x.CurrentForceN);
        var lineForceResidualN = Math.Abs(segmentForceN - lineElementForceN);
        var relativeLineForceResidual = lineForceResidualN / Math.Max(1.0, Math.Abs(lineElementForceN));
        var wllConsistencyApplies =
            double.IsFinite(result.SafetyFactor) &&
            result.SafetyFactor > 0 &&
            result.WeakLinkBreakingLoadKn > 0;
        var expectedWorkingLoadKn = wllConsistencyApplies
            ? result.WeakLinkBreakingLoadKn / result.SafetyFactor
            : double.NaN;
        var workingLoadResidualKn = wllConsistencyApplies
            ? Math.Abs(result.WorkingLoadKn - expectedWorkingLoadKn)
            : double.NaN;
        var relativeWorkingLoadResidual = wllConsistencyApplies
            ? workingLoadResidualKn / Math.Max(1.0, Math.Abs(expectedWorkingLoadKn))
            : 0;
        var invalidAnchorHoldingCoefficientCount = new[]
        {
            result.AnchorBaseHoldingCoefficient,
            result.AnchorTypeMultiplier,
            result.SeabedHoldingMultiplier
        }.Count(x => !double.IsFinite(x) || x <= 0);
        var invalidElementProjectedAreaCount = result.ElementRows.Count(x => !double.IsFinite(x.ProjectedAreaM2) || x.ProjectedAreaM2 < 0);
        var invalidSegmentProjectedAreaCount = result.SegmentRows.Count(x => !double.IsFinite(x.ProjectedAreaM2) || x.ProjectedAreaM2 < 0);
        var minimumElementProjectedAreaM2 = result.ElementRows.Count > 0 ? result.ElementRows.Min(x => x.ProjectedAreaM2) : double.NaN;
        var minimumSegmentProjectedAreaM2 = result.SegmentRows.Count > 0 ? result.SegmentRows.Min(x => x.ProjectedAreaM2) : double.NaN;
        var invalidElementDragCoefficientCount = result.ElementRows.Count(x => !double.IsFinite(x.DragCoefficient) || x.DragCoefficient < 0);
        var invalidSegmentDragCoefficientCount = result.SegmentRows.Count(x => !double.IsFinite(x.DragCoefficient) || x.DragCoefficient < 0);
        var minimumElementDragCoefficient = result.ElementRows.Count > 0 ? result.ElementRows.Min(x => x.DragCoefficient) : double.NaN;
        var minimumSegmentDragCoefficient = result.SegmentRows.Count > 0 ? result.SegmentRows.Min(x => x.DragCoefficient) : double.NaN;
        var minimumElementProjectedAreaText = double.IsNaN(minimumElementProjectedAreaM2) ? "нет данных" : $"min A={minimumElementProjectedAreaM2:0.####} м²";
        var minimumSegmentProjectedAreaText = double.IsNaN(minimumSegmentProjectedAreaM2) ? "нет данных" : $"min A={minimumSegmentProjectedAreaM2:0.####} м²";
        var minimumElementDragCoefficientText = double.IsNaN(minimumElementDragCoefficient) ? "нет данных" : $"min Cd={minimumElementDragCoefficient:0.####}";
        var minimumSegmentDragCoefficientText = double.IsNaN(minimumSegmentDragCoefficient) ? "нет данных" : $"min Cd={minimumSegmentDragCoefficient:0.####}";
        var invalidElementMblCount = result.ElementRows.Count(x => !double.IsFinite(x.BreakingLoadKn) || x.BreakingLoadKn < 0);
        var zeroElementMblCount = result.ElementRows.Count(x => x.BreakingLoadKn == 0);
        var minimumElementMblKn = result.ElementRows.Count > 0 ? result.ElementRows.Min(x => x.BreakingLoadKn) : double.NaN;
        var invalidElementCurrentForceCount = result.ElementRows.Count(x => !double.IsFinite(x.CurrentForceN) || x.CurrentForceN < 0);
        var invalidSegmentCurrentForceCount = result.SegmentRows.Count(x => !double.IsFinite(x.CurrentForceN) || x.CurrentForceN < 0);
        var minimumElementCurrentForceN = result.ElementRows.Count > 0 ? result.ElementRows.Min(x => x.CurrentForceN) : double.NaN;
        var minimumSegmentCurrentForceN = result.SegmentRows.Count > 0 ? result.SegmentRows.Min(x => x.CurrentForceN) : double.NaN;
        var invalidAggregateDragForceCount = new[]
        {
            result.CurrentForceN,
            result.WaveForceN,
            result.HorizontalForceN
        }.Count(x => !double.IsFinite(x) || x < 0);
        var minimumElementCurrentForceText = double.IsNaN(minimumElementCurrentForceN) ? "нет данных" : $"min F={minimumElementCurrentForceN:0.####} Н";
        var minimumSegmentCurrentForceText = double.IsNaN(minimumSegmentCurrentForceN) ? "нет данных" : $"min F={minimumSegmentCurrentForceN:0.####} Н";

        rows.Add(new EngineeringDiagnosticRow(
            "Положительная проектная глубина",
            $"Depth={environment.DepthM:0.####} м",
            "Depth > 0 и конечна",
            double.IsFinite(environment.DepthM) && environment.DepthM > 0
                ? EngineeringCheckSeverity.Ok
                : EngineeringCheckSeverity.Error,
            "Проверяется исходная проектная глубина до локального геометрического ограничения значением 0."));

        rows.Add(new EngineeringDiagnosticRow(
            "Неотрицательная активная скалярная скорость течения",
            scalarCurrentIsActive
                ? $"Uскал={environment.CurrentSpeedMS:0.####} м/с"
                : $"скалярное значение не используется; активных точек {environment.EffectiveCurrentProfile.Count}",
            scalarCurrentIsActive ? "Uскал ≥ 0 и конечна" : "локальный инвариант не применяется",
            !scalarCurrentIsActive || double.IsFinite(environment.CurrentSpeedMS) && environment.CurrentSpeedMS >= 0
                ? EngineeringCheckSeverity.Ok
                : EngineeringCheckSeverity.Error,
            scalarCurrentIsActive
                ? "Скалярное поле участвует в расчёте как модуль скорости течения."
                : "Непустой активный профиль заменяет скалярное поле подписанными компонентами U/V/W."));

        rows.Add(new EngineeringDiagnosticRow(
            "Неотрицательная высота волны",
            $"H={environment.WaveHeightM:0.####} м",
            "H ≥ 0 и конечна",
            double.IsFinite(environment.WaveHeightM) && environment.WaveHeightM >= 0
                ? EngineeringCheckSeverity.Ok
                : EngineeringCheckSeverity.Error,
            "Высота используется в оценке волновой скорости и не нормализуется по модулю."));

        rows.Add(new EngineeringDiagnosticRow(
            "Согласованность периода и высоты волны",
            $"H={environment.WaveHeightM:0.####} м; T={environment.WavePeriodS:0.####} с",
            environment.WaveHeightM > 0 ? "T > 0 при H > 0" : "T ≥ 0 при H = 0",
            double.IsFinite(environment.WavePeriodS) &&
            (environment.WaveHeightM > 0 ? environment.WavePeriodS > 0 : environment.WavePeriodS >= 0)
                ? EngineeringCheckSeverity.Ok
                : EngineeringCheckSeverity.Error,
            environment.WaveHeightM > 0
                ? "Положительная высота требует положительного периода для расчёта ненулевой волновой добавки."
                : "При нулевой высоте допускается нулевой период; отрицательный период физически недопустим."));

        rows.Add(new EngineeringDiagnosticRow(
            "Положительная эффективная плотность воды",
            $"ρэфф={effectiveWaterDensityKgM3:0.####} кг/м³",
            "ρэфф > 0 и конечна",
            double.IsFinite(effectiveWaterDensityKgM3) && effectiveWaterDensityKgM3 > 0
                ? EngineeringCheckSeverity.Ok
                : EngineeringCheckSeverity.Error,
            "Проверяется плотность, используемая общей базовой моделью и fallback-семантикой профиля течения."));

        rows.Add(new EngineeringDiagnosticRow(
            "Положительная плотность расчётных сегментов",
            double.IsNaN(minimumSegmentDensityKgM3)
                ? "сегментов нет; нарушений 0"
                : $"min ρ={minimumSegmentDensityKgM3:0.####} кг/м³; нарушений {invalidSegmentDensityCount}",
            "каждый ρ > 0 и конечен",
            invalidSegmentDensityCount == 0 ? EngineeringCheckSeverity.Ok : EngineeringCheckSeverity.Error,
            double.IsNaN(minimumSegmentDensityKgM3)
                ? "Коллекция сегментов пуста; этот локальный инвариант не проверяет наличие расчётной линии."
                : "Проверяется плотность, используемая сегментным drag и shape-based X/Z силой линии."));

        rows.Add(new EngineeringDiagnosticRow(
            "Неотрицательные площади сопротивления",
            $"элементы: {minimumElementProjectedAreaText}; нарушений {invalidElementProjectedAreaCount}; сегменты: {minimumSegmentProjectedAreaText}; нарушений {invalidSegmentProjectedAreaCount}",
            "каждая A ≥ 0 и конечна",
            invalidElementProjectedAreaCount == 0 && invalidSegmentProjectedAreaCount == 0
                ? EngineeringCheckSeverity.Ok
                : EngineeringCheckSeverity.Error,
            "Ноль допускается для строк, намеренно не участвующих в drag; значения не исправляются автоматически."));

        rows.Add(new EngineeringDiagnosticRow(
            "Неотрицательные коэффициенты сопротивления",
            $"элементы: {minimumElementDragCoefficientText}; нарушений {invalidElementDragCoefficientCount}; сегменты: {minimumSegmentDragCoefficientText}; нарушений {invalidSegmentDragCoefficientCount}",
            "каждый Cd ≥ 0 и конечен",
            invalidElementDragCoefficientCount == 0 && invalidSegmentDragCoefficientCount == 0
                ? EngineeringCheckSeverity.Ok
                : EngineeringCheckSeverity.Error,
            "Нулевой Cd допускается для строк без сопротивления; отрицательные и неконечные значения только диагностируются."));

        rows.Add(new EngineeringDiagnosticRow(
            "Неотрицательные MBL элементов",
            result.ElementRows.Count == 0
                ? "строк элементов нет; нарушений 0; нулевых 0"
                : $"min MBL={minimumElementMblKn:0.####} кН; отрицательных/неконечных {invalidElementMblCount}; нулевых {zeroElementMblCount}",
            "каждая MBL ≥ 0 и конечна; 0 = не задана",
            invalidElementMblCount == 0
                ? EngineeringCheckSeverity.Ok
                : EngineeringCheckSeverity.Error,
            "Проверяется знак опубликованных MBL; наличие слабого звена и полнота положительных MBL контролируются существующими проверками."));

        rows.Add(new EngineeringDiagnosticRow(
            "Неотрицательные рассчитанные силы сопротивления",
            $"элементы: {minimumElementCurrentForceText}; нарушений {invalidElementCurrentForceCount}; сегменты: {minimumSegmentCurrentForceText}; нарушений {invalidSegmentCurrentForceCount}; агрегаты: Fтеч={result.CurrentForceN:0.####} Н; Fволн={result.WaveForceN:0.####} Н; Fгор={result.HorizontalForceN:0.####} Н; нарушений {invalidAggregateDragForceCount}",
            "каждая F ≥ 0 и конечна",
            invalidElementCurrentForceCount == 0 && invalidSegmentCurrentForceCount == 0 && invalidAggregateDragForceCount == 0
                ? EngineeringCheckSeverity.Ok
                : EngineeringCheckSeverity.Error,
            "Проверяется знак опубликованных сил-модулей; исходные плотность, площадь, Cd и согласованность агрегации контролируются отдельными строками."));

        rows.Add(new EngineeringDiagnosticRow(
            "Неотрицательные глубины активного профиля течения",
            !environment.UseCurrentProfile
                ? "профиль отключён"
                : double.IsNaN(minimumProfileDepthM)
                    ? "точек 0; отрицательных/неконечных глубин 0"
                    : $"min z={minimumProfileDepthM:0.####} м; отрицательных/неконечных глубин {invalidProfileDepthCount}",
            "каждая DepthM ≥ 0 и конечна",
            !environment.UseCurrentProfile || invalidProfileDepthCount == 0
                ? EngineeringCheckSeverity.Ok
                : EngineeringCheckSeverity.Error,
            !environment.UseCurrentProfile
                ? "Профиль не участвует в текущем расчёте."
                : "Проверяется нижняя физическая граница глубины; точки глубже проектной глубины не запрещаются."));

        rows.Add(new EngineeringDiagnosticRow(
            "Уникальные глубины активного профиля течения",
            !environment.UseCurrentProfile
                ? "профиль отключён"
                : environment.EffectiveCurrentProfile.Count < 2
                    ? $"точек {environment.EffectiveCurrentProfile.Count}; конфликтов интервалов нет"
                    : duplicateProfileDepthGroups.Count == 0
                        ? $"точек {environment.EffectiveCurrentProfile.Count}; дублированных глубин 0"
                        : $"глубин-дублей {duplicateProfileDepthGroups.Count}; лишних точек {duplicateProfilePointCount}; z={duplicateProfileDepthText} м",
            "точные DepthM уникальны",
            duplicateProfileDepthGroups.Count == 0 ? EngineeringCheckSeverity.Ok : EngineeringCheckSeverity.Error,
            !environment.UseCurrentProfile
                ? "Профиль не участвует в текущем расчёте."
                : environment.EffectiveCurrentProfile.Count < 2
                    ? "Менее двух точек не образуют конфликтующий интерполяционный интервал."
                    : duplicateProfileDepthGroups.Count == 0
                        ? "Каждая опорная глубина активного профиля имеет единственный набор U/V/W/ρ."
                        : "Точные дубли DepthM делают скачок профиля зависимым от порядка строк; значения не объединяются и не усредняются автоматически."));

        rows.Add(Check(
            "Якорь на проектной глубине",
            double.IsNaN(anchorDepthResidualM) ? "нет данных" : $"невязка {anchorDepthResidualM:0.####} м",
            "≤ 0,01 м",
            !double.IsNaN(anchorDepthResidualM) && anchorDepthResidualM <= 0.01,
            double.IsNaN(anchorDepthResidualM) ? EngineeringCheckSeverity.Warning : EngineeringCheckSeverity.Error,
            double.IsNaN(anchorDepthResidualM) ? "Якорный узел отсутствует." : $"Z якоря = {anchorDepthM:0.####} м, Depth = {depthM:0.####} м."));

        rows.Add(Check(
            "Длина расчётной линии",
            double.IsNaN(lengthResidualM) ? "нет данных" : $"невязка {lengthResidualM:0.####} м",
            "≤ 0,01 м",
            !double.IsNaN(lengthResidualM) && lengthResidualM <= 0.01,
            double.IsNaN(lengthResidualM) ? EngineeringCheckSeverity.Warning : EngineeringCheckSeverity.Error,
            double.IsNaN(lengthResidualM) ? "Последний узел формы отсутствует." : $"s последнего узла = {shape.AnchorPoint!.AlongLineM:0.####} м, L линии = {lineLengthM:0.####} м."));

        rows.Add(new EngineeringDiagnosticRow(
            "Состояние буя",
            DisplayBuoyState(shape.BuoyState),
            "Surface / Submerged / Overloaded",
            shape.BuoyState == BuoyShapeState.Overloaded ? EngineeringCheckSeverity.Error : EngineeringCheckSeverity.Ok,
            double.IsNaN(buoyDepthM) ? "Глубина буя не определена." : $"Z буя = {buoyDepthM:0.####} м."));

        rows.Add(new EngineeringDiagnosticRow(
            "Геометрия: линия и глубина",
            $"L/Depth = {(depthM > 0 ? lineLengthM / depthM : 0):0.####}",
            "информационно",
            EngineeringCheckSeverity.Info,
            depthM <= 0
                ? "Глубина не задана."
                : lineLengthM >= depthM
                    ? "Длина линии не меньше глубины; поверхностное положение буя геометрически возможно."
                    : "Длина линии меньше глубины; верхний узел должен быть под водой."));

        rows.Add(Check(
            "Положение буя выше якоря",
            double.IsNaN(buoyDepthM) || double.IsNaN(anchorDepthM) ? "нет данных" : $"Zбуй={buoyDepthM:0.####}, Zякорь={anchorDepthM:0.####}",
            "Zбуй < Zякорь",
            !double.IsNaN(buoyDepthM) && !double.IsNaN(anchorDepthM) && buoyDepthM < anchorDepthM,
            EngineeringCheckSeverity.Error,
            "Буй должен находиться выше нижнего граничного узла якоря."));

        rows.Add(new EngineeringDiagnosticRow(
            "Согласованность длины линии и расчётных сегментов",
            $"ΔL={segmentLengthResidualM:0.####} м ({relativeSegmentLengthResidual:0.####})",
            "relative ≤ 1e-6",
            relativeSegmentLengthResidual <= 1e-6 ? EngineeringCheckSeverity.Ok : EngineeringCheckSeverity.Error,
            $"Длина линии={lineLengthM:0.####} м; Σ длин сегментов={segmentLengthM:0.####} м. Проверяется сохранение длины распределённой линии при сегментации."));

        rows.Add(new EngineeringDiagnosticRow(
            "Положительная длина расчётных сегментов",
            double.IsNaN(minimumSegmentLengthM)
                ? "сегментов нет; нарушений 0"
                : $"min L={minimumSegmentLengthM:0.####} м; нарушений {nonPositiveSegmentCount}",
            "каждый L > 0",
            nonPositiveSegmentCount == 0 ? EngineeringCheckSeverity.Ok : EngineeringCheckSeverity.Error,
            double.IsNaN(minimumSegmentLengthM)
                ? "Коллекция сегментов пуста; этот локальный инвариант не проверяет наличие расчётной линии."
                : "Проверяется отсутствие нулевых и отрицательных строк в сегментном read model."));

        rows.Add(new EngineeringDiagnosticRow(
            "Максимальная длина расчётного сегмента",
            double.IsNaN(maximumSegmentLengthM)
                ? "сегментов нет; превышений 0"
                : $"max L={maximumSegmentLengthM:0.##########} м; превышений {excessiveSegmentLengthCount}",
            "каждый L ≤ 0,20 м (+1e-9 м числового допуска)",
            excessiveSegmentLengthCount == 0 ? EngineeringCheckSeverity.Ok : EngineeringCheckSeverity.Error,
            double.IsNaN(maximumSegmentLengthM)
                ? "Коллекция сегментов пуста; этот локальный инвариант не проверяет наличие расчётной линии."
                : "Проверяется соблюдение фиксированного целевого шага сегментации 0,20 м без ограничения количества сегментов."));

        rows.Add(new EngineeringDiagnosticRow(
            "Согласованность веса линии и расчётных сегментов",
            $"Δm={lineWeightResidualKg:0.####} кг ({relativeLineWeightResidual:0.####})",
            "relative ≤ 1e-6",
            relativeLineWeightResidual <= 1e-6 ? EngineeringCheckSeverity.Ok : EngineeringCheckSeverity.Error,
            $"Вес участков линии={lineElementWeightWaterKg:0.####} кг; Σ веса сегментов={segmentWeightWaterKg:0.####} кг. Проверяется сохранение распределённого веса линии при сегментации."));

        rows.Add(new EngineeringDiagnosticRow(
            "Согласованность силы линии и расчётных сегментов",
            $"ΔF={lineForceResidualN:0.####} Н ({relativeLineForceResidual:0.####})",
            "relative ≤ 1e-6",
            relativeLineForceResidual <= 1e-6 ? EngineeringCheckSeverity.Ok : EngineeringCheckSeverity.Error,
            $"Сила участков линии={lineElementForceN:0.####} Н; Σ сил сегментов={segmentForceN:0.####} Н. Проверяется сохранение распределённой силы течения линии при агрегации сегментов."));

        rows.Add(new EngineeringDiagnosticRow(
            "ΣFx линии",
            $"{forceResiduals.LineSumFxN:0.####} Н",
            "информационно",
            EngineeringCheckSeverity.Info,
            "Сумма горизонтальных сил сопротивления по сегментам линии."));

        rows.Add(new EngineeringDiagnosticRow(
            "ΣFz линии",
            $"{forceResiduals.LineSumFzN:0.####} Н",
            "информационно",
            EngineeringCheckSeverity.Info,
            "Сумма вертикальных весовых сил по сегментам линии в воде."));

        rows.Add(new EngineeringDiagnosticRow(
            "Контроль накопления ΣFx линии",
            $"{forceResiduals.ResidualFxN:0.####} Н ({forceResiduals.RelativeResidualFx:0.####})",
            "relative ≤ 1e-6",
            tensionRows.Count > 0 && forceResiduals.RelativeResidualFx <= 1e-6 ? EngineeringCheckSeverity.Ok : EngineeringCheckSeverity.Error,
            "Внутренний контроль накопления сил: сравнение суммы сегментных сил линии с верхней горизонтальной компонентой натяжения. Не является проверкой полного равновесия постановки."));

        rows.Add(new EngineeringDiagnosticRow(
            "Контроль накопления ΣFz линии",
            $"{forceResiduals.ResidualFzN:0.####} Н ({forceResiduals.RelativeResidualFz:0.####})",
            "relative ≤ 1e-6",
            tensionRows.Count > 0 && forceResiduals.RelativeResidualFz <= 1e-6 ? EngineeringCheckSeverity.Ok : EngineeringCheckSeverity.Error,
            "Внутренний контроль накопления сил: сравнение суммы весовых сил линии с верхней вертикальной компонентой натяжения. Не является проверкой полного равновесия постановки."));

        rows.Add(new EngineeringDiagnosticRow(
            "Согласованность базовой нагрузки и векторной ведомости",
            $"ΔFx={horizontalForceResidualN:0.####} Н ({relativeHorizontalForceResidual:0.####})",
            "relative ≤ 1e-6",
            relativeHorizontalForceResidual <= 1e-6 ? EngineeringCheckSeverity.Ok : EngineeringCheckSeverity.Error,
            $"HorizontalForceN={result.HorizontalForceN:0.####} Н; ΣFx ведомости={vectorBalance.SumExternalFxN:0.####} Н. Проверяется восстановление базовой горизонтальной нагрузки из строк элементов и волновой добавки."));

        rows.Add(new EngineeringDiagnosticRow(
            "ΣFx учтённых сил постановки",
            $"{vectorBalance.SumExternalFxN:0.####} Н",
            "информационно",
            EngineeringCheckSeverity.Info,
            "Сумма горизонтальных внешних сил по векторной ведомости: буй, соединители, приборы, линия, якорь и волновая добавка, если она есть."));

        rows.Add(new EngineeringDiagnosticRow(
            "ΣFz учтённых сил постановки",
            $"{vectorBalance.SumExternalFzN:0.####} Н",
            "информационно",
            EngineeringCheckSeverity.Info,
            "Сумма вертикальных внешних сил по векторной ведомости. Положительное направление Z принято вверх."));

        rows.Add(new EngineeringDiagnosticRow(
            "Требуемая реакция якоря Rx",
            $"{vectorBalance.RequiredReactionFxN:0.####} Н; контрольный запас удержания {vectorBalance.AnchorHorizontalReserve:0.####}",
            "информационно",
            EngineeringCheckSeverity.Info,
            "Требуемое значение для замыкания ΣFx=0; не является решённой реакцией опоры."));

        rows.Add(new EngineeringDiagnosticRow(
            "Требуемая реакция якоря Rz",
            $"{vectorBalance.RequiredReactionFzN:0.####} Н",
            "информационно",
            EngineeringCheckSeverity.Info,
            "Требуемое значение для замыкания ΣFz=0; контактная/грунтовая модель в этой ведомости не решается."));

        rows.Add(new EngineeringDiagnosticRow(
            "Ведомость требуемых реакций",
            "ведомость сформирована; показаны требуемые реакции",
            "проверочная ведомость; не решение равновесия",
            EngineeringCheckSeverity.Info,
            vectorBalance.MethodNote));

        rows.Add(new EngineeringDiagnosticRow(
            "Максимальное натяжение",
            $"{maxTensionKn:0.####} кН",
            "информационно",
            EngineeringCheckSeverity.Info,
            tensionRows.Count > 0 ? "По сегментной оценке натяжений." : "По общей оценке результата."));

        rows.Add(new EngineeringDiagnosticRow(
            "Коэффициент запаса слабого звена",
            $"SF={result.SafetyFactor:0.####}",
            "SF > 0; рекомендуемое SF ≥ 3",
            !double.IsFinite(result.SafetyFactor) || result.SafetyFactor <= 0
                ? EngineeringCheckSeverity.Error
                : result.SafetyFactor < 3
                    ? EngineeringCheckSeverity.Warning
                    : EngineeringCheckSeverity.Ok,
            result.SafetyFactor >= 3
                ? "Коэффициент соответствует текущей рекомендуемой проектной границе."
                : result.SafetyFactor > 0
                    ? "Положительный коэффициент ниже текущей рекомендуемой проектной границы 3."
                    : "Нулевой, отрицательный или неконечный коэффициент не может задавать допустимую рабочую нагрузку."));

        rows.Add(new EngineeringDiagnosticRow(
            "Согласованность WLL и коэффициента запаса",
            wllConsistencyApplies
                ? $"ΔWLL={workingLoadResidualKn:0.########} кН ({relativeWorkingLoadResidual:0.########})"
                : !(result.WeakLinkBreakingLoadKn > 0)
                    ? "не применяется: MBL слабого звена не определена"
                    : "не применяется: коэффициент запаса недопустим",
            "relative ≤ 1e-6 при MBL > 0 и SF > 0",
            !wllConsistencyApplies || relativeWorkingLoadResidual <= 1e-6
                ? EngineeringCheckSeverity.Ok
                : EngineeringCheckSeverity.Error,
            wllConsistencyApplies
                ? $"MBL={result.WeakLinkBreakingLoadKn:0.####} кН; SF={result.SafetyFactor:0.####}; ожидаемая WLL={expectedWorkingLoadKn:0.####} кН; опубликованная WLL={result.WorkingLoadKn:0.####} кН."
                : "Локальный контроль формулы не заменяет отдельные проверки коэффициента запаса и наличия слабого звена."));

        rows.Add(new EngineeringDiagnosticRow(
            "Запас слабого звена",
            $"{result.TensionReserve:0.####}",
            "> 1",
            result.TensionReserve >= 1 ? EngineeringCheckSeverity.Ok : EngineeringCheckSeverity.Error,
            $"WLL слабого звена = {result.WorkingLoadKn:0.####} кН."));

        rows.Add(new EngineeringDiagnosticRow(
            "Положительные коэффициенты удержания якоря",
            $"Kякоря={result.AnchorBaseHoldingCoefficient:0.####}; Kтипа={result.AnchorTypeMultiplier:0.####}; Kгрунта={result.SeabedHoldingMultiplier:0.####}; нарушений {invalidAnchorHoldingCoefficientCount}",
            "каждый K > 0 и конечен",
            invalidAnchorHoldingCoefficientCount == 0
                ? EngineeringCheckSeverity.Ok
                : EngineeringCheckSeverity.Error,
            "Проверяются только множители модели удержания; вес якоря в воде и итоговый запас контролируются отдельными строками."));

        rows.Add(new EngineeringDiagnosticRow(
            "Запас удержания якоря по базовой горизонтальной нагрузке",
            $"{result.AnchorReserve:0.####}",
            "> 1",
            result.AnchorReserve >= 1 ? EngineeringCheckSeverity.Ok : EngineeringCheckSeverity.Error,
            $"Удержание = {result.AnchorHoldingKg:0.####} кг, требуется = {result.RequiredAnchorHoldingKg:0.####} кг."));

        rows.Add(new EngineeringDiagnosticRow(
            "Итерации solver формы",
            $"{shape.IterationCount}; scale={shape.AngleScale:0.####}",
            "информационно",
            EngineeringCheckSeverity.Info,
            "Масштаб углов сегментов подбирается итерационно для геометрического замыкания якорной глубины."));

        rows.Add(new EngineeringDiagnosticRow(
            "Невязка сходимости формы",
            $"{shape.ConvergenceResidualM:0.####} м",
            shape.ConvergenceCriterion,
            shape.Converged ? EngineeringCheckSeverity.Ok : EngineeringCheckSeverity.Warning,
            "Геометрическая невязка между глубиной якорного узла и проектной глубиной."));

        rows.Add(new EngineeringDiagnosticRow(
            "Статус сходимости формы",
            shape.Converged ? "Converged" : "Not converged",
            shape.ConvergenceCriterion,
            shape.Converged ? EngineeringCheckSeverity.Ok : EngineeringCheckSeverity.Warning,
            shape.MethodNote));

        var overall = rows.Any(x => x.Severity == EngineeringCheckSeverity.Error)
            ? EngineeringCheckSeverity.Error
            : rows.Any(x => x.Severity == EngineeringCheckSeverity.Warning)
                ? EngineeringCheckSeverity.Warning
                : EngineeringCheckSeverity.Ok;

        return new EngineeringDiagnosticsResult(rows, forceResiduals, overall, DisplayOverall(overall));
    }

    private static EngineeringForceResiduals BuildForceResiduals(IReadOnlyList<SegmentTensionRow> tensionRows)
    {
        if (tensionRows.Count == 0)
        {
            return new EngineeringForceResiduals(0, 0, 0, 0, 0, 0, 0, 0, false);
        }

        var topRow = tensionRows.OrderBy(x => x.Number).First();
        var lineSumFxN = tensionRows.Sum(x => x.SegmentCurrentForceN);
        var lineSumFzN = tensionRows.Sum(x => x.WeightWaterKg) * G;
        var topTensionFxN = topRow.CumulativeHorizontalForceN;
        var topTensionFzN = topRow.CumulativeVerticalForceN;
        var residualFxN = Math.Abs(lineSumFxN - topTensionFxN);
        var residualFzN = Math.Abs(lineSumFzN - topTensionFzN);
        var relativeFx = residualFxN / Math.Max(1.0, Math.Abs(lineSumFxN));
        var relativeFz = residualFzN / Math.Max(1.0, Math.Abs(lineSumFzN));

        return new EngineeringForceResiduals(
            lineSumFxN,
            lineSumFzN,
            topTensionFxN,
            topTensionFzN,
            residualFxN,
            residualFzN,
            relativeFx,
            relativeFz,
            relativeFx <= 1e-6 && relativeFz <= 1e-6);
    }

    private static EngineeringDiagnosticRow Check(
        string checkName,
        string value,
        string tolerance,
        bool ok,
        EngineeringCheckSeverity failureSeverity,
        string note)
    {
        return new EngineeringDiagnosticRow(
            checkName,
            value,
            tolerance,
            ok ? EngineeringCheckSeverity.Ok : failureSeverity,
            note);
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

    public static string DisplaySeverity(EngineeringCheckSeverity severity)
    {
        return severity switch
        {
            EngineeringCheckSeverity.Ok => "OK",
            EngineeringCheckSeverity.Warning => "WARNING",
            EngineeringCheckSeverity.Error => "ERROR",
            _ => "INFO"
        };
    }

    private static string DisplayOverall(EngineeringCheckSeverity severity)
    {
        return severity switch
        {
            EngineeringCheckSeverity.Ok => "диагностика без ошибок",
            EngineeringCheckSeverity.Warning => "есть предупреждения предварительной модели",
            EngineeringCheckSeverity.Error => "есть инженерные ошибки/несогласованности",
            _ => "информационная диагностика"
        };
    }
}
