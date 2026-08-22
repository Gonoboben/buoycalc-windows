using System.Collections;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SelectedTechnicalReportReadModelRegression
{
    private static readonly HashSet<string> AcceptedFixtures = new(StringComparer.Ordinal)
    {
        "uniform-current-slack-line",
        "discrete-payload"
    };

    private static readonly string[] LegacyAuthorityPrefixes =
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

    public static void Validate()
    {
        var definitions = HistoricalDefinitions().Cast<object>().ToList();
        var selectedCount = 0;
        var fallbackCount = 0;

        Console.WriteLine("F4B2_SELECTED_TECHNICAL_REPORT_BEGIN");

        foreach (var definition in definitions)
        {
            var name = Property<string>(definition, "Name");
            var environment = Property<EnvironmentInput>(definition, "Environment");
            var buoy = Property<BuoyInput>(definition, "Buoy");
            var assembly = Property<IReadOnlyList<AssemblyItemInput>>(definition, "Assembly");
            var anchor = Property<AnchorInput>(definition, "Anchor");
            var safetyFactor = Property<double>(definition, "SafetyFactor");

            var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, safetyFactor);
            var snapshot = run.Snapshot;
            const string projectName = "F4-B2 regression";

            var legacyTechnical = TechnicalReportMarkdownBuilder.Build(projectName, environment, buoy, anchor, snapshot);
            var userSummaryBefore = UserReportBuilder.Build(environment, snapshot);
            var elementRowsBefore = DisplayRowsFingerprint(SelectedElementCalculationDisplayProjector.Project(snapshot));

            var resultVerdict = run.Result.Verdict;
            var resultMainRisk = run.Result.MainRisk;
            var resultChecks = run.Result.Checks.ToArray();
            var resultWeakLinkKn = run.Result.WeakLinkBreakingLoadKn;
            var resultWeakLinkName = run.Result.WeakLinkName;
            var resultWorkingLoadKn = run.Result.WorkingLoadKn;
            var resultTensionReserve = run.Result.TensionReserve;
            var resultAnchorHoldingKg = run.Result.AnchorHoldingKg;
            var resultRequiredAnchorHoldingKg = run.Result.RequiredAnchorHoldingKg;
            var resultAnchorReserve = run.Result.AnchorReserve;
            var resultRows = run.Result.ElementRows.ToArray();
            var selectedShape = snapshot.SelectedShape;
            var selectedCoreShape = snapshot.ShadowSelectedCore?.Shape;

            var technical = TechnicalReportBuilder.Build(projectName, environment, buoy, anchor, snapshot);

            if (!AcceptedFixtures.Contains(name))
            {
                if (snapshot.SelectedEngineeringAssessment is not null ||
                    snapshot.SelectedDesignTensionDemand is not null ||
                    snapshot.SelectedAnchorReaction is not null ||
                    snapshot.SelectedLocalStructuralCapacity is not null)
                {
                    throw new InvalidOperationException($"F4-B2 {name}: non-Accepted fixture unexpectedly exposes selected authority state.");
                }

                if (technical != legacyTechnical)
                    throw new InvalidOperationException($"F4-B2 {name}: non-selected technical report is not exact legacy fallback.");

                fallbackCount++;
                Console.WriteLine($"F4B2_SELECTED_TECHNICAL_REPORT|{name}|Selected=False|TechnicalReport=LegacyExact");
            }
            else
            {
                var assessment = snapshot.SelectedEngineeringAssessment
                    ?? throw new InvalidOperationException($"F4-B2 {name}: selected F4 assessment missing.");
                var tension = snapshot.SelectedDesignTensionDemand
                    ?? throw new InvalidOperationException($"F4-B2 {name}: selected F1 design demand missing.");
                var anchorReaction = snapshot.SelectedAnchorReaction
                    ?? throw new InvalidOperationException($"F4-B2 {name}: selected F2 anchor reaction missing.");
                var capacity = snapshot.SelectedLocalStructuralCapacity
                    ?? throw new InvalidOperationException($"F4-B2 {name}: selected F3 local capacity missing.");

                ValidateSelectedReport(name, technical, legacyTechnical, snapshot, assessment, tension, anchorReaction, capacity);
                selectedCount++;
                Console.WriteLine($"F4B2_SELECTED_TECHNICAL_REPORT|{name}|Selected=True|Verdict={assessment.Verdict}|AnchorContact={anchorReaction.ContactClassification}|LegacyAuthority=CompatibilityOnly");
            }

            if (run.Result.Verdict != resultVerdict || run.Result.MainRisk != resultMainRisk)
                throw new InvalidOperationException($"F4-B2 {name}: CalculationResult Verdict/MainRisk mutated by report projection.");
            if (!run.Result.Checks.SequenceEqual(resultChecks))
                throw new InvalidOperationException($"F4-B2 {name}: CalculationResult.Checks mutated by report projection.");
            Exact(run.Result.WeakLinkBreakingLoadKn, resultWeakLinkKn, name + " legacy weak-link MBL unchanged");
            if (run.Result.WeakLinkName != resultWeakLinkName)
                throw new InvalidOperationException($"F4-B2 {name}: legacy WeakLinkName mutated.");
            Exact(run.Result.WorkingLoadKn, resultWorkingLoadKn, name + " legacy WLL unchanged");
            Exact(run.Result.TensionReserve, resultTensionReserve, name + " legacy tension reserve unchanged");
            Exact(run.Result.AnchorHoldingKg, resultAnchorHoldingKg, name + " legacy anchor holding unchanged");
            Exact(run.Result.RequiredAnchorHoldingKg, resultRequiredAnchorHoldingKg, name + " legacy required anchor holding unchanged");
            Exact(run.Result.AnchorReserve, resultAnchorReserve, name + " legacy anchor reserve unchanged");
            if (!run.Result.ElementRows.SequenceEqual(resultRows))
                throw new InvalidOperationException($"F4-B2 {name}: CalculationResult.ElementRows mutated.");
            if (!ReferenceEquals(snapshot.SelectedShape, selectedShape) ||
                !ReferenceEquals(snapshot.ShadowSelectedCore?.Shape, selectedCoreShape))
            {
                throw new InvalidOperationException($"F4-B2 {name}: selected X/Z identity changed.");
            }

            var userSummaryAfter = UserReportBuilder.Build(environment, snapshot);
            if (userSummaryAfter != userSummaryBefore)
                throw new InvalidOperationException($"F4-B2 {name}: F4-B1 compact user summary changed after technical-report projection.");
            var elementRowsAfter = DisplayRowsFingerprint(SelectedElementCalculationDisplayProjector.Project(snapshot));
            if (elementRowsAfter != elementRowsBefore)
                throw new InvalidOperationException($"F4-B2 {name}: F4-B1 selected element-table read model changed after technical-report projection.");

            var legacyTechnicalAfter = TechnicalReportMarkdownBuilder.Build(projectName, environment, buoy, anchor, snapshot);
            if (legacyTechnicalAfter != legacyTechnical)
                throw new InvalidOperationException($"F4-B2 {name}: legacy technical renderer output mutated after selected projection.");
        }

        if (definitions.Count != 5 || selectedCount != 2 || fallbackCount != 3)
        {
            throw new InvalidOperationException(
                $"F4-B2 canonical coverage mismatch: scenarios={definitions.Count}, selected={selectedCount}, fallback={fallbackCount}.");
        }

        Console.WriteLine("F4B2_SELECTED_TECHNICAL_REPORT_ROLLUP|CanonicalScenarios=5|Selected=2|LegacyFallback=3|F1F2F3F4Authority=True|LegacyCapacityCompatibilityOnly=True|CalculationResultMutated=False|SelectedGeometryChanged=False|F4B1PresentationChanged=False");
        Console.WriteLine("F4B2_SELECTED_TECHNICAL_REPORT_END");
    }

    private static void ValidateSelectedReport(
        string scenario,
        string technical,
        string legacyTechnical,
        CalculationSnapshot snapshot,
        MooringSelectedEngineeringAssessmentState assessment,
        MooringSelectedDesignTensionDemandState tension,
        MooringSelectedAnchorReactionState anchorReaction,
        MooringSelectedLocalStructuralCapacityState capacity)
    {
        if (technical == legacyTechnical)
            throw new InvalidOperationException($"F4-B2 {scenario}: Accepted technical report did not switch to selected presentation.");

        RequireContains(technical, $"Вердикт: {assessment.Verdict}", scenario);
        RequireContains(technical, $"Главный риск: {assessment.MainRisk}", scenario);
        RequireContains(technical, "## Выбранная инженерная оценка", scenario);
        RequireContains(technical, $"Расчётная selected design-нагрузка F1: {tension.DemandKn:0.####} кН ({tension.DemandN:0.####} Н)", scenario);
        RequireContains(technical, $"Контакт якоря F2: {anchorReaction.ContactClassification}", scenario);
        RequireContains(technical, $"Горизонтальная selected-нагрузка якоря F2: {anchorReaction.HorizontalDemandN:0.####} Н", scenario);
        RequireContains(technical, $"Signed normal reaction якоря F2: {anchorReaction.SignedNormalReactionN:0.####} Н", scenario);
        RequireContains(technical, assessment.AnchorHorizontalCapacityDisposition.ToString(), scenario);
        RequireContains(technical, "требуется отдельная валидированная модель якорь/грунт", scenario);
        RequireContains(technical, "legacy AnchorReserve не является selected-authority основанием для прохода", scenario);
        RequireContains(technical, "Selected F4 checks; legacy CalculationResult.Checks не используются для selected verdict.", scenario);

        foreach (var check in assessment.Checks)
            RequireContains(technical, $"{check.Kind} / {check.Code}", scenario);

        RequireContains(technical,
            $"F3 structural coverage: expected={capacity.ExpectedStructuralElementCount}; rated={capacity.RatedStructuralElementCount}; incomplete={capacity.IncompleteStructuralElementCount}; insufficient={capacity.InsufficientElementCount}; complete={capacity.StructuralCapacityCoverageComplete}",
            scenario);

        if (capacity.GoverningElementNumber.HasValue)
        {
            RequireContains(technical, $"Определяющий локальный несущий элемент F3: #{capacity.GoverningElementNumber.Value}", scenario);
            RequireContains(technical, $"Локальный запас governing элемента F3: {Format(capacity.GoverningReserve)}", scenario);
        }

        RequireContains(technical, "compatibility-only", scenario);
        foreach (var line in Lines(technical))
        {
            foreach (var prefix in LegacyAuthorityPrefixes)
            {
                if (line.StartsWith(prefix, StringComparison.Ordinal))
                    throw new InvalidOperationException($"F4-B2 {scenario}: unlabelled legacy authority line remains: {line}");
            }
        }

        var tableSection = Section(technical, "## Таблица элементов");
        RequireContains(tableSection, "Selected-authority presentation", scenario + " selected element table");
        foreach (var row in SelectedElementCalculationDisplayProjector.Project(snapshot))
            RequireContains(tableSection, ExpectedRow(row), scenario + $" selected element row {row.Number}");

        var checksSection = Section(technical, "## Проверки");
        foreach (var check in assessment.Checks)
            RequireContains(checksSection, check.Code, scenario + " selected checks");
    }

    private static string ExpectedRow(ElementCalculationDisplayRow row) =>
        $"| {row.Number} | {Escape(row.Kind)} | {Escape(row.Title)} | {Escape(row.PresetName)} | {row.LengthM} | {row.Count} | {row.WeightWaterKg} | {row.ProjectedAreaM2} | {row.DragCoefficient} | {row.CurrentForceN} | {row.BreakingLoadKn} | {row.WorkingLoadKn} | {row.Reserve} | {Escape(row.Status)} |";

    private static string DisplayRowsFingerprint(IReadOnlyList<ElementCalculationDisplayRow> rows) =>
        string.Join("\n", rows.Select(ExpectedRow));

    private static string Section(string report, string heading)
    {
        var lines = Lines(report);
        var start = Array.FindIndex(lines, x => x == heading);
        if (start < 0)
            throw new InvalidOperationException($"F4-B2 section not found: {heading}");
        var end = start + 1;
        while (end < lines.Length && !lines[end].StartsWith("## ", StringComparison.Ordinal))
            end++;
        return string.Join("\n", lines[start..end]);
    }

    private static string[] Lines(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

    private static IEnumerable HistoricalDefinitions()
    {
        var builder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("F4-B2: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");
        return builder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException("F4-B2: historical fixtures are unavailable.");
    }

    private static T Property<T>(object source, string name)
    {
        var property = source.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"F4-B2: property {source.GetType().Name}.{name} was not found.");
        var value = property.GetValue(source);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"F4-B2: property {source.GetType().Name}.{name} is not {typeof(T).Name}.");
    }

    private static void RequireContains(string value, string expected, string label)
    {
        if (!value.Contains(expected, StringComparison.Ordinal))
            throw new InvalidOperationException($"F4-B2 {label}: expected text missing: {expected}");
    }

    private static void Exact(double actual, double expected, string label)
    {
        if (actual != expected)
            throw new InvalidOperationException($"F4-B2 {label}: expected exact {expected:R}, got {actual:R}.");
    }

    private static string Format(double? value) => value.HasValue ? value.Value.ToString("0.####") : "n/a";

    private static string Escape(string value) => (value ?? string.Empty).Replace("|", "\\|", StringComparison.Ordinal);
}
