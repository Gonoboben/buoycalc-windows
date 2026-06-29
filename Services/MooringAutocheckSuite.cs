using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public enum MooringAutocheckSeverity
{
    Pass,
    Info,
    Warning,
    Fail
}

public sealed record MooringAutocheckRow(
    int Number,
    string Scenario,
    string CheckName,
    string Expected,
    string Actual,
    MooringAutocheckSeverity Severity,
    string Status);

public sealed record MooringAutocheckResult(
    IReadOnlyList<MooringAutocheckRow> Rows,
    int PassCount,
    int InfoCount,
    int WarningCount,
    int FailCount,
    bool HasFailures,
    string Summary,
    string MethodNote);

public static class MooringAutocheckSuite
{
    public static MooringAutocheckResult Build(
        CalculationResult result,
        MooringShapeResult fallbackShape,
        MooringShapeResult candidateShape,
        bool iterativeSolverConverged,
        bool iterativeSolverDiverged,
        MooringIterativeSolverStopReason stopReason,
        MooringDeploymentModeResult deploymentMode)
    {
        var rows = new List<MooringAutocheckRow>();

        Add(rows, "входная геометрия", "Глубина задана", "Глубина > 0", $"Глубина={deploymentMode.DepthM:0.####} м", deploymentMode.DepthM > 0, "Нужно задать проектную глубину.");
        Add(rows, "входная геометрия", "Длина линии задана", "Длина линии > 0", $"Длина={deploymentMode.LineLengthM:0.####} м", deploymentMode.LineLengthM > 0, "Длина линии должна быть положительной.");
        Add(rows, "резервная форма", "Резервная форма имеет узлы", "Узлов >= 2", $"Узлов={fallbackShape.Nodes.Count}", fallbackShape.Nodes.Count >= 2, "Внутренняя резервная форма должна оставаться доступной для диагностики.");
        Add(rows, "форма с дискретными элементами", "Форма имеет узлы", "Узлов >= 2", $"Узлов={candidateShape.Nodes.Count}", candidateShape.Nodes.Count >= 2, "Форма с дискретными элементами должна иметь минимум два узла.", MooringAutocheckSeverity.Info);
        Add(rows, "итерационный solver", "Причина остановки согласована", "Сошёлся => StopReason=Converged; разошёлся => не сошёлся", $"Сошёлся={BoolRu(iterativeSolverConverged)}, разошёлся={BoolRu(iterativeSolverDiverged)}, StopReason={stopReason}", (!iterativeSolverConverged || stopReason == MooringIterativeSolverStopReason.Converged) && (!iterativeSolverDiverged || !iterativeSolverConverged), "Флаги сходимости solver не должны противоречить друг другу.");
        Add(rows, "режим постановки", "Режим определён", "Режим != unknown", $"Режим={deploymentMode.ModeCode}", deploymentMode.Mode != MooringDeploymentMode.Unknown, "Обычные исходные данные должны классифицироваться по режиму постановки.", MooringAutocheckSeverity.Warning);
        Add(rows, "режим постановки", "Короткая линия согласована", "short => длина < глубина", $"Режим={deploymentMode.ModeCode}, недостаток={deploymentMode.ShortageM:0.####} м", deploymentMode.Mode != MooringDeploymentMode.ShortLine || deploymentMode.LineLengthM < deploymentMode.DepthM, "Режим short должен означать, что линия короче глубины.");
        Add(rows, "режим постановки", "Избыточная линия согласована", "excess line => L/Depth >= 1.2", $"Режим={deploymentMode.ModeCode}, L/Depth={deploymentMode.LineToDepthRatio:0.####}", deploymentMode.Mode != MooringDeploymentMode.ExcessLine || deploymentMode.LineToDepthRatio >= 1.2, "Режим excess line должен означать заметный избыток длины.");
        Add(rows, "нагрузки", "Чистая плавучесть конечна", "Чистая плавучесть конечна", $"Чистая плавучесть={result.NetBuoyancyKg:0.####} кг", IsFinite(result.NetBuoyancyKg), "Чистая плавучесть должна быть конечной.");
        Add(rows, "выбранная форма", "Снос X конечен", "X конечен", $"резервная X={fallbackShape.HorizontalOffsetM:0.####} м, дискретная X={candidateShape.HorizontalOffsetM:0.####} м", IsFinite(fallbackShape.HorizontalOffsetM) && IsFinite(candidateShape.HorizontalOffsetM), "Координаты X должны быть конечными для визуализации и отчёта.");

        AppendElementDatabaseChecks(rows, result);

        var passCount = rows.Count(x => x.Severity == MooringAutocheckSeverity.Pass);
        var infoCount = rows.Count(x => x.Severity == MooringAutocheckSeverity.Info);
        var warningCount = rows.Count(x => x.Severity == MooringAutocheckSeverity.Warning);
        var failCount = rows.Count(x => x.Severity == MooringAutocheckSeverity.Fail);
        var hasFailures = failCount > 0;
        var summary = hasFailures
            ? $"ОШИБКА: критичных проверок не пройдено: {failCount}"
            : warningCount > 0
                ? $"ПРЕДУПРЕЖДЕНИЕ: критичных ошибок нет, предупреждений: {warningCount}"
                : "ОК: базовые автопроверки пройдены";

        return new MooringAutocheckResult(
            rows,
            passCount,
            infoCount,
            warningCount,
            failCount,
            hasFailures,
            summary,
            "v0.46.3: автопроверки включают контроль качества базы элементов и согласованности расчётных слоёв. Они не меняют физику; они показывают пропущенные названия, пресеты, неверные количества, длины, Cd/площадь, WLL/MBL и неконечные запасы.");
    }

    public static string BuildReportTable(MooringAutocheckResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Автопроверки сценариев и базы элементов v0.46.3");
        sb.AppendLine(result.MethodNote);
        sb.AppendLine();
        sb.AppendLine($"Итог: {result.Summary}; пройдено={result.PassCount}, информация={result.InfoCount}, предупреждения={result.WarningCount}, ошибки={result.FailCount}.");
        sb.AppendLine();
        sb.AppendLine("| № | Сценарий | Проверка | Ожидается | Фактически | Статус | Примечание |");
        sb.AppendLine("|---:|---|---|---|---|---|---|");
        foreach (var row in result.Rows)
        {
            sb.AppendLine($"| {row.Number} | {Escape(row.Scenario)} | {Escape(row.CheckName)} | {Escape(row.Expected)} | {Escape(row.Actual)} | {SeverityRu(row.Severity)} | {Escape(row.Status)} |");
        }
        sb.AppendLine();
        return sb.ToString();
    }

    private static void AppendElementDatabaseChecks(List<MooringAutocheckRow> rows, CalculationResult result)
    {
        var elements = result.ElementRows;
        var missingNames = elements.Count(x => string.IsNullOrWhiteSpace(x.Title));
        var missingKinds = elements.Count(x => string.IsNullOrWhiteSpace(x.Kind));
        var missingPresets = elements.Count(x => string.IsNullOrWhiteSpace(x.PresetName));
        var invalidCounts = elements.Count(x => x.Count <= 0);
        var negativeLengths = elements.Count(x => x.LengthM < 0);
        var distributedRows = elements.Count(x => x.LengthM > 0);
        var dragAreaWithoutCd = elements.Count(x => x.ProjectedAreaM2 > 0 && x.DragCoefficient <= 0);
        var cdWithoutArea = elements.Count(x => x.DragCoefficient > 0 && x.ProjectedAreaM2 <= 0 && x.CurrentForceN > 0);
        var wllAboveMbl = elements.Count(x => x.BreakingLoadKn > 0 && x.WorkingLoadKn > x.BreakingLoadKn);
        var nonFiniteReserve = elements.Count(x => !IsFinite(x.Reserve));
        var negativeBreakingLoad = elements.Count(x => x.BreakingLoadKn < 0 || x.WorkingLoadKn < 0);

        Add(rows, "база элементов", "Строки элементов есть", "Количество строк > 0", $"строк={elements.Count}", elements.Count > 0, "В отчёте должны быть минимум буй, линия и якорь.");
        Add(rows, "база элементов", "Названия элементов заполнены", "пустых названий = 0", $"пустых={missingNames}", missingNames == 0, "У каждой строки должно быть видимое название.", MooringAutocheckSeverity.Warning);
        Add(rows, "база элементов", "Типы элементов заполнены", "пустых типов = 0", $"пустых={missingKinds}", missingKinds == 0, "У каждой строки должен быть тип элемента.");
        Add(rows, "база элементов", "Пресеты заполнены", "пустых пресетов = 0", $"пустых={missingPresets}", missingPresets == 0, "Пустой пресет ухудшает трассировку строки к базе элементов.", MooringAutocheckSeverity.Info);
        Add(rows, "база элементов", "Количество положительное", "неверных количеств = 0", $"неверных={invalidCounts}", invalidCounts == 0, "Количество элемента должно быть больше нуля.");
        Add(rows, "база элементов", "Нет отрицательных длин", "отрицательных длин = 0", $"отрицательных={negativeLengths}", negativeLengths == 0, "Длина не может быть отрицательной.");
        Add(rows, "база элементов", "Есть распределённые участки", "строк с длиной > 0 >= 1", $"распределённых={distributedRows}", distributedRows >= 1, "В постановке обычно должен быть хотя бы один распределённый участок линии.", MooringAutocheckSeverity.Warning);
        Add(rows, "база элементов", "Площадь имеет Cd", "A > 0 => Cd > 0", $"ошибок={dragAreaWithoutCd}", dragAreaWithoutCd == 0, "Площадь без Cd делает силу течения сомнительной.", MooringAutocheckSeverity.Warning);
        Add(rows, "база элементов", "Cd имеет площадь при наличии силы", "Cd > 0 и сила > 0 => A > 0", $"ошибок={cdWithoutArea}", cdWithoutArea == 0, "Cd без площади не объясняет силу течения.", MooringAutocheckSeverity.Warning);
        Add(rows, "база элементов", "WLL не больше MBL", "WLL <= MBL", $"ошибок={wllAboveMbl}", wllAboveMbl == 0, "Рабочая нагрузка не должна превышать разрывную.");
        Add(rows, "база элементов", "Прочности неотрицательные", "MBL/WLL >= 0", $"ошибок={negativeBreakingLoad}", negativeBreakingLoad == 0, "Прочностные значения не должны быть отрицательными.");
        Add(rows, "база элементов", "Запас конечен", "Запас конечен", $"ошибок={nonFiniteReserve}", nonFiniteReserve == 0, "Запас должен быть конечным для отчёта и проверок.");
    }

    private static void Add(List<MooringAutocheckRow> rows, string scenario, string checkName, string expected, string actual, bool passed, string note, MooringAutocheckSeverity failedSeverity = MooringAutocheckSeverity.Fail)
    {
        rows.Add(new MooringAutocheckRow(rows.Count + 1, scenario, checkName, expected, actual, passed ? MooringAutocheckSeverity.Pass : failedSeverity, passed ? "ОК" : note));
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private static string BoolRu(bool value)
    {
        return value ? "да" : "нет";
    }

    private static string SeverityRu(MooringAutocheckSeverity value)
    {
        return value switch
        {
            MooringAutocheckSeverity.Pass => "ОК",
            MooringAutocheckSeverity.Info => "Информация",
            MooringAutocheckSeverity.Warning => "Предупреждение",
            MooringAutocheckSeverity.Fail => "Ошибка",
            _ => value.ToString()
        };
    }

    private static string Escape(string value)
    {
        return (value ?? string.Empty).Replace("|", "/");
    }
}
