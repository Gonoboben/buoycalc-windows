using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

/// <summary>
/// Presentation-only projection that replaces legacy user-conclusion sections of the
/// existing technical Markdown with retained selected engineering authority.
/// RejectedPhysical uses its dedicated terminal disposition without fabricating F1/F2/F3/F4;
/// other non-Accepted states retain the exact legacy renderer fallback.
/// </summary>
public static class SelectedTechnicalReportProjector
{
    public static string Project(string legacyReport, CalculationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(legacyReport);
        ArgumentNullException.ThrowIfNull(snapshot);

        var physicalDisposition = snapshot.PhysicalDisposition;
        if (physicalDisposition is not null)
            return ProjectPhysicalRejection(legacyReport, physicalDisposition);

        var assessment = snapshot.SelectedEngineeringAssessment;
        if (assessment is null)
            return legacyReport;

        var tension = snapshot.SelectedDesignTensionDemand;
        var anchorReaction = snapshot.SelectedAnchorReaction;
        var capacity = snapshot.SelectedLocalStructuralCapacity;
        var selectedShape = snapshot.SelectedShape
            ?? throw new InvalidOperationException("Selected technical report requires retained selected X/Z authority.");

        if (!assessment.IsDirectHardFailureTerminal &&
            (tension is null || anchorReaction is null || capacity is null))
        {
            throw new InvalidOperationException(
                "Non-terminal selected technical report requires retained F1/F2/F3 authorities.");
        }

        RequireCommonSource(
            assessment.SourceIdentity,
            tension?.SourceIdentity,
            anchorReaction?.SourceIdentity,
            capacity?.SourceIdentity);
        if (!string.Equals(selectedShape.Source, assessment.SourceIdentity.ToString(), StringComparison.Ordinal))
            throw new InvalidOperationException("Selected technical report requires one retained source across selected X/Z and F1/F2/F3/F4.");

        var newline = legacyReport.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var lines = legacyReport.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var output = new List<string>(lines.Length + 80);
        var verdictReplaced = false;
        var mainRiskReplaced = false;
        var inLegacySummary = false;

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
                AppendSelectedGeometrySection(output, selectedShape);
                AppendSelectedAuthoritySection(output, assessment, tension, anchorReaction, capacity);
                output.Add("## Итоги базовой/fallback-модели — compatibility-only");
                output.Add("Секция сохранена как compatibility evidence. Её X/Z, solver и derived shape-значения не являются selected engineering geometry этого Accepted run.");
                inLegacySummary = true;
                continue;
            }

            if (inLegacySummary && line.StartsWith("## ", StringComparison.Ordinal))
                inLegacySummary = false;

            if (inLegacySummary && line.StartsWith("- ", StringComparison.Ordinal))
            {
                output.Add(line.StartsWith("- compatibility-only — ", StringComparison.Ordinal)
                    ? line
                    : "- compatibility-only — " + line[2..]);
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

            output.Add(MarkAcceptedLegacyGeometryAsCompatibilityOnly(line));
        }

        if (!verdictReplaced || !mainRiskReplaced)
            throw new InvalidOperationException("Selected technical report could not locate legacy verdict/main-risk headline fields.");

        return string.Join(newline, output);
    }

    private static string ProjectPhysicalRejection(
        string legacyReport,
        MooringSignedPhysicalDispositionState disposition)
    {
        if (disposition.CandidateStatus != MooringSignedCandidateStatus.RejectedPhysical ||
            !disposition.HasHardFailure ||
            !disposition.BlocksEngineeringGeometry)
        {
            throw new InvalidOperationException(
                "Physical-rejection technical report requires a terminal RejectedPhysical hard-failure disposition.");
        }

        var newline = legacyReport.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var lines = legacyReport.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var output = new List<string>(lines.Length + 40);
        var verdictReplaced = false;
        var mainRiskReplaced = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (!verdictReplaced && line.StartsWith("Вердикт: ", StringComparison.Ordinal))
            {
                output.Add($"Вердикт: {disposition.Verdict}");
                verdictReplaced = true;
                continue;
            }

            if (!mainRiskReplaced && line.StartsWith("Главный риск: ", StringComparison.Ordinal))
            {
                output.Add($"Главный риск: {disposition.MainRisk}");
                mainRiskReplaced = true;
                continue;
            }

            if (line == "## Итоги")
            {
                AppendPhysicalRejectionSection(output, disposition);
                output.Add(line);
                continue;
            }

            if (line == "## Таблица элементов")
            {
                output.Add(line);
                output.Add("Таблица ниже сохранена как compatibility evidence выполненного базового расчёта. Она не является selected F3 authority, поскольку signed candidate физически отклонён.");
                continue;
            }

            if (line == "## Проверки")
            {
                AppendPhysicalRejectionChecks(output, disposition);
                i = SkipSection(lines, i);
                continue;
            }

            output.Add(MarkLegacyAuthorityAsCompatibilityOnly(line));
        }

        if (!verdictReplaced || !mainRiskReplaced)
        {
            throw new InvalidOperationException(
                "Physical-rejection technical report could not locate legacy verdict/main-risk headline fields.");
        }

        return string.Join(newline, output);
    }

    private static void AppendPhysicalRejectionSection(
        List<string> output,
        MooringSignedPhysicalDispositionState disposition)
    {
        output.Add("## Физическая невозможность signed candidate");
        output.Add("Signed calculation authority классифицировал постановку как физически недопустимую в рамках текущей нерастяжимой модели. Fallback/iterative X/Z не используется как инженерная selected-геометрия.");
        output.Add($"- Вердикт: {disposition.Verdict}");
        output.Add($"- Код физического отказа: {disposition.DiagnosticCode}");
        output.Add($"- Диагностика: {disposition.DiagnosticText}");
        output.Add("- Selected X/Z authority: отсутствует");
        output.Add("- Selected F1/F2/F3/F4 authority: отсутствует; значения не синтезируются без физически допустимой selected-геометрии");
        output.Add(string.Empty);
    }

    private static void AppendPhysicalRejectionChecks(
        List<string> output,
        MooringSignedPhysicalDispositionState disposition)
    {
        output.Add("## Проверки");
        output.Add("Signed physical-disposition authority; legacy CalculationResult.Checks не определяют этот terminal verdict.");
        output.Add($"- [FAIL] RejectedPhysical / {disposition.DiagnosticCode}: {disposition.DiagnosticText}");
        output.Add("- [BLOCK] SelectedGeometry: fallback/iterative X/Z заблокирована как engineering authority.");
        output.Add("- [N/A] F1/F2/F3/F4: не создаются без физически допустимой selected-геометрии.");
        output.Add(string.Empty);
    }

    private static void AppendSelectedAuthoritySection(
        List<string> output,
        MooringSelectedEngineeringAssessmentState assessment,
        MooringSelectedDesignTensionDemandState? tension,
        MooringSelectedAnchorReactionState? anchorReaction,
        MooringSelectedLocalStructuralCapacityState? capacity)
    {
        output.Add("## Выбранная инженерная оценка");
        output.Add("Эта секция является authoritative selected-оценкой. Значения legacy-моделей ниже сохранены только для трассируемости и явно помечены compatibility-only там, где могли бы выглядеть как расчётная capacity/резерв.");
        output.Add($"- Источник selected authority: {assessment.SourceIdentity}");
        output.Add($"- Вердикт F4: {assessment.Verdict}");
        output.Add($"- Главный риск F4: {assessment.MainRisk} ({assessment.MainRiskCode})");

        if (tension is null)
        {
            output.Add("- Расчётная selected design-нагрузка F1: недоступна; terminal direct hard failure не синтезирует F1 authority");
        }
        else
        {
            output.Add($"- Расчётная selected design-нагрузка F1: {tension.DemandKn:0.####} кН ({tension.DemandN:0.####} Н)");
            output.Add($"- Положение governing design demand F1: {tension.LocationKind}; s={tension.AlongLineM:0.####} м; segment={tension.SegmentNumber?.ToString() ?? "n/a"}");
        }

        if (capacity is null)
        {
            output.Add("- F3 structural coverage: недоступна; terminal direct hard failure не синтезирует F3 authority");
        }
        else
        {
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
        }

        if (anchorReaction is null)
        {
            output.Add("- Контакт якоря F2: недоступен; authority не создаётся при неположительном весе якоря в воде");
            output.Add("- Горизонтальная selected-нагрузка якоря F2: недоступна");
            output.Add("- Signed normal reaction якоря F2: недоступна");
            output.Add("- Compressive normal reaction якоря F2: недоступна");
            output.Add("- Uplift excess якоря F2: недоступен");
            output.Add("- Горизонтальная capacity якоря F4: недоступна без F2; никакая contact/capacity authority не синтезирована");
        }
        else
        {
            output.Add($"- Контакт якоря F2: {anchorReaction.ContactClassification}");
            output.Add($"- Горизонтальная selected-нагрузка якоря F2: {anchorReaction.HorizontalDemandN:0.####} Н");
            output.Add($"- Signed normal reaction якоря F2: {anchorReaction.SignedNormalReactionN:0.####} Н");
            output.Add($"- Compressive normal reaction якоря F2: {anchorReaction.CompressiveNormalReactionN:0.####} Н");
            output.Add($"- Uplift excess якоря F2: {anchorReaction.UpliftExcessN:0.####} Н");
            output.Add($"- Горизонтальная capacity якоря F4: {assessment.AnchorHorizontalCapacityDisposition} — требуется отдельная валидированная модель якорь/грунт; legacy AnchorReserve не является selected-authority основанием для прохода.");
        }
        output.Add(string.Empty);
    }

    private static void AppendSelectedGeometrySection(
        List<string> output,
        SelectedShapeReadModel selectedShape)
    {
        var shape = selectedShape.Shape;
        output.Add("## Выбранная инженерная геометрия X/Z");
        output.Add("Единственная authoritative selected X/Z geometry этого Accepted run. UI, typed PDF/read model и эта Full TXT секция используют CalculationSnapshot.SelectedShape; остальные X/Z-разделы ниже являются compatibility-only diagnostic evidence.");
        output.Add($"- Источник selected X/Z authority: {selectedShape.Source}");
        output.Add($"- Authoritative horizontal offset X, m: {Exact(shape.HorizontalOffsetM)}");
        output.Add($"- Authoritative node count: {shape.Nodes.Count.ToString(CultureInfo.InvariantCulture)}");
        output.Add("| Узел | X, м | Z, м |");
        output.Add("|---:|---:|---:|");
        foreach (var node in shape.Nodes)
            output.Add($"| {node.Number.ToString(CultureInfo.InvariantCulture)} | {Exact(node.XOffsetM)} | {Exact(node.ZDepthM)} |");
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

    private static string MarkAcceptedLegacyGeometryAsCompatibilityOnly(string line)
    {
        string[] diagnosticHeadings =
        {
            "## Расчётная форма постановки X/Z",
            "## Проекции формы X/Z по сегментам",
            "## Силы линии по форме X/Z и ориентации сегментов",
            "## Натяжения линии по форме X/Z",
            "## Согласованность направления силы и X/Z-касательной",
            "## Натяжения линии с дискретными нагрузками по s",
            "## Альтернативная форма X/Z с дискретными нагрузками",
            "## Signed-равновесие внутренних дискретных узлов",
            "## Дискретные X/Z-узлы альтернативной формы",
            "## Итерационный solver — итерации и кандидатная форма",
            "## Выбор основной формы",
            "## Signed-равновесие внутренних узлов — финальная итерационная кандидатная форма"
        };

        if (diagnosticHeadings.Contains(line, StringComparer.Ordinal))
            return "## compatibility-only diagnostic — " + line[3..];

        return MarkLegacyAuthorityAsCompatibilityOnly(line);
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

    private static void RequireCommonSource(
        MooringShapeSourceIdentity assessment,
        params MooringShapeSourceIdentity?[] consumers)
    {
        if (assessment != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
            consumers.Any(x => x.HasValue && x.Value != assessment))
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

    private static string Exact(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static string Escape(string value) => (value ?? string.Empty).Replace("|", "\\|", StringComparison.Ordinal);
}
