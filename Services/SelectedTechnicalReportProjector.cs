using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

/// <summary>
/// Presentation-only projection that replaces legacy user-conclusion sections of the
/// existing technical Markdown with retained selected F1/F2/F3/F4 authorities.
/// The legacy renderer remains the exact fallback when no selected assessment exists.
/// </summary>
public static class SelectedTechnicalReportProjector
{
    public static string Project(string legacyReport, CalculationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(legacyReport);
        ArgumentNullException.ThrowIfNull(snapshot);

        var assessment = snapshot.SelectedEngineeringAssessment;
        if (assessment is null)
            return legacyReport;

        var tension = snapshot.SelectedDesignTensionDemand
            ?? throw new InvalidOperationException("Selected technical report requires retained F1 design-tension authority.");
        var anchorReaction = snapshot.SelectedAnchorReaction
            ?? throw new InvalidOperationException("Selected technical report requires retained F2 anchor-reaction authority.");
        var capacity = snapshot.SelectedLocalStructuralCapacity
            ?? throw new InvalidOperationException("Selected technical report requires retained F3 local-capacity authority.");

        RequireCommonSource(assessment.SourceIdentity, tension.SourceIdentity, anchorReaction.SourceIdentity, capacity.SourceIdentity);

        var newline = legacyReport.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var lines = legacyReport.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var output = new List<string>(lines.Length + 80);
        var verdictReplaced = false;
        var mainRiskReplaced = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (!verdictReplaced && line.StartsWith("Вердикт: ", StringComparison.Ordinal))
            {
                output.Add($"Вердикт: {assessment.Verdict}");
                verdictReplaced = true;
                continue;
            }

            if (!mainRiskReplaced && line.StartsWith("Главный риск: ", StringComparison.Ordinal))
            {
                output.Add($"Главный риск: {assessment.MainRisk}");
                mainRiskReplaced = true;
                continue;
            }

            if (line == "## Итоги")
            {
                AppendSelectedAuthoritySection(output, assessment, tension, anchorReaction, capacity);
                output.Add(line);
                continue;
            }

            if (line == "## Таблица элементов")
            {
                AppendSelectedElementTable(output, snapshot);
                i = SkipSection(lines, i);
                continue;
            }

            if (line == "## Проверки")
            {
                AppendSelectedChecks(output, assessment);
                i = SkipSection(lines, i);
                continue;
            }

            output.Add(MarkLegacyAuthorityAsCompatibilityOnly(line));
        }

        if (!verdictReplaced || !mainRiskReplaced)
            throw new InvalidOperationException("Selected technical report could not locate legacy verdict/main-risk headline fields.");

        return string.Join(newline, output);
    }

    private static void AppendSelectedAuthoritySection(
        List<string> output,
        MooringSelectedEngineeringAssessmentState assessment,
        MooringSelectedDesignTensionDemandState tension,
        MooringSelectedAnchorReactionState anchorReaction,
        MooringSelectedLocalStructuralCapacityState capacity)
    {
        output.Add("## Выбранная инженерная оценка");
        output.Add("Эта секция является authoritative selected-оценкой. Значения legacy-моделей ниже сохранены только для трассируемости и явно помечены compatibility-only там, где могли бы выглядеть как расчётная capacity/резерв.");
        output.Add($"- Источник selected authority: {assessment.SourceIdentity}");
        output.Add($"- Вердикт F4: {assessment.Verdict}");
        output.Add($"- Главный риск F4: {assessment.MainRisk} ({assessment.MainRiskCode})");
        output.Add($"- Расчётная selected design-нагрузка F1: {tension.DemandKn:0.####} кН ({tension.DemandN:0.####} Н)");
        output.Add($"- Положение governing design demand F1: {tension.LocationKind}; s={tension.AlongLineM:0.####} м; segment={tension.SegmentNumber?.ToString() ?? "n/a"}");
        output.Add($"- F3 structural coverage: expected={capacity.ExpectedStructuralElementCount}; rated={capacity.RatedStructuralElementCount}; incomplete={capacity.IncompleteStructuralElementCount}; insufficient={capacity.InsufficientElementCount}; complete={capacity.StructuralCapacityCoverageComplete}");

        if (capacity.GoverningElementNumber.HasValue)
        {
            output.Add($"- Определяющий локальный несущий элемент F3: #{capacity.GoverningElementNumber.Value} {capacity.GoverningTitle} / {capacity.GoverningPresetName}");
            output.Add($"- Локальная design-нагрузка governing элемента F3: {Format(capacity.GoverningDemandN)} Н");
            output.Add($"- WLL governing элемента F3: {Format(capacity.GoverningWorkingLoadKn)} кН");
            output.Add($"- Локальный запас governing элемента F3: {Format(capacity.GoverningReserve)}");
            output.Add($"- Статус governing элемента F3: {capacity.GoverningStatus?.ToString() ?? "n/a"}");
        }
        else
        {
            output.Add("- Определяющий локальный несущий элемент F3: не определён среди элементов с доступной capacity-моделью");
        }

        output.Add($"- Контакт якоря F2: {anchorReaction.ContactClassification}");
        output.Add($"- Горизонтальная selected-нагрузка якоря F2: {anchorReaction.HorizontalDemandN:0.####} Н");
        output.Add($"- Signed normal reaction якоря F2: {anchorReaction.SignedNormalReactionN:0.####} Н");
        output.Add($"- Compressive normal reaction якоря F2: {anchorReaction.CompressiveNormalReactionN:0.####} Н");
        output.Add($"- Uplift excess якоря F2: {anchorReaction.UpliftExcessN:0.####} Н");
        output.Add($"- Горизонтальная capacity якоря F4: {assessment.AnchorHorizontalCapacityDisposition} — требуется отдельная валидированная модель якорь/грунт; legacy AnchorReserve не является selected-authority основанием для прохода.");
        output.Add(string.Empty);
    }

    private static void AppendSelectedElementTable(List<string> output, CalculationSnapshot snapshot)
    {
        var rows = SelectedElementCalculationDisplayProjector.Project(snapshot);
        output.Add("## Таблица элементов");
        output.Add("Selected-authority presentation: structural MBL/WLL/local reserve/status берутся из F3-C; буй и якорь используют F4/F2 disposition. Legacy element reserve/status здесь не являются authority.");
        output.Add("| № | Тип | Элемент | Пресет | Длина, м | Кол-во | Вес в воде, кг | Площадь, м² | Cd | Сила, Н | MBL, кН | WLL, кН | Локальный запас | Selected статус |");
        output.Add("|---:|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (var row in rows)
        {
            output.Add($"| {row.Number} | {Escape(row.Kind)} | {Escape(row.Title)} | {Escape(row.PresetName)} | {row.LengthM} | {row.Count} | {row.WeightWaterKg} | {row.ProjectedAreaM2} | {row.DragCoefficient} | {row.CurrentForceN} | {row.BreakingLoadKn} | {row.WorkingLoadKn} | {row.Reserve} | {Escape(row.Status)} |");
        }
        output.Add(string.Empty);
    }

    private static void AppendSelectedChecks(
        List<string> output,
        MooringSelectedEngineeringAssessmentState assessment)
    {
        output.Add("## Проверки");
        output.Add("Selected F4 checks; legacy CalculationResult.Checks не используются для selected verdict.");
        foreach (var check in assessment.Checks)
        {
            output.Add($"- [{CheckStatus(check.Status)}] {check.Kind} / {check.Code}: {check.Summary} {check.Detail}");
        }
        output.Add(string.Empty);
    }

    private static int SkipSection(string[] lines, int headingIndex)
    {
        var i = headingIndex + 1;
        while (i < lines.Length && !lines[i].StartsWith("## ", StringComparison.Ordinal))
            i++;
        return i - 1;
    }

    private static string MarkLegacyAuthorityAsCompatibilityOnly(string line)
    {
        string[] prefixes =
        {
            "- Базовый коэф. удержания якоря:",
            "- Множитель типа якоря:",
            "- Множитель грунта:",
            "- Формула удержания:",
            "- Расчётная нагрузка для проверки слабого звена:",
            "- Слабое звено:",
            "- MBL слабого звена:",
            "- WLL слабого звена:",
            "- Запас по слабому звену:",
            "- Требуемое удержание якоря:",
            "- Удержание якоря:",
            "- Запас удержания якоря по базовой горизонтальной нагрузке:",
            "- Горизонтальная удерживающая способность якоря:",
            "- Контрольный запас удержания по Rx векторной ведомости:"
        };

        foreach (var prefix in prefixes)
        {
            if (line.StartsWith(prefix, StringComparison.Ordinal))
                return "- compatibility-only — " + line[2..];
        }

        return line;
    }

    private static void RequireCommonSource(params MooringShapeSourceIdentity[] sources)
    {
        if (sources.Length == 0 || sources.Any(x => x != MooringShapeSourceIdentity.SignedBoundaryFeedback) || sources.Any(x => x != sources[0]))
            throw new InvalidOperationException("Selected technical report requires one retained SignedBoundaryFeedback authority chain across F1/F2/F3/F4.");
    }

    private static string CheckStatus(MooringEngineeringAssessmentCheckStatus status) => status switch
    {
        MooringEngineeringAssessmentCheckStatus.Ok => "OK",
        MooringEngineeringAssessmentCheckStatus.RequiresReview => "REVIEW",
        MooringEngineeringAssessmentCheckStatus.HardFailure => "FAIL",
        _ => status.ToString()
    };

    private static string Format(double? value) => value.HasValue ? value.Value.ToString("0.####") : "n/a";

    private static string Escape(string value) => (value ?? string.Empty).Replace("|", "\\|", StringComparison.Ordinal);
}
