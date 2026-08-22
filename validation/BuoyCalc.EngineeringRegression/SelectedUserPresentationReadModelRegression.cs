using System.Collections;
using System.Globalization;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SelectedUserPresentationReadModelRegression
{
    private static readonly HashSet<string> AcceptedFixtures = new(StringComparer.Ordinal)
    {
        "uniform-current-slack-line",
        "discrete-payload"
    };

    public static void Validate()
    {
        var definitions = HistoricalDefinitions().Cast<object>().ToList();
        var selectedCount = 0;
        var fallbackCount = 0;

        Console.WriteLine("F4B1_SELECTED_USER_PRESENTATION_BEGIN");

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
            var legacySummary = UserReportBuilder.Build(environment, run.Result);
            var legacyRows = run.Result.ElementRows.Select(ElementCalculationDisplayRow.From).ToList();
            var legacyTechnical = TechnicalReportBuilder.Build("F4-B1 regression", environment, buoy, anchor, snapshot);

            var resultVerdict = run.Result.Verdict;
            var resultMainRisk = run.Result.MainRisk;
            var resultChecks = run.Result.Checks.ToArray();
            var resultWeakLinkKn = run.Result.WeakLinkBreakingLoadKn;
            var resultWeakLinkName = run.Result.WeakLinkName;
            var resultTensionReserve = run.Result.TensionReserve;
            var resultAnchorHoldingKg = run.Result.AnchorHoldingKg;
            var resultRequiredAnchorHoldingKg = run.Result.RequiredAnchorHoldingKg;
            var resultAnchorReserve = run.Result.AnchorReserve;
            var resultRows = run.Result.ElementRows.ToArray();
            var selectedShape = snapshot.SelectedShape;
            var selectedCoreShape = snapshot.ShadowSelectedCore?.Shape;

            var boundary = ReportBuildBoundary.Build(
                "F4-B1 regression",
                environment,
                buoy,
                anchor,
                snapshot);
            var projectedRows = SelectedElementCalculationDisplayProjector.Project(snapshot);

            if (!AcceptedFixtures.Contains(name))
            {
                if (snapshot.SelectedEngineeringAssessment is not null ||
                    snapshot.SelectedLocalStructuralCapacity is not null)
                {
                    throw new InvalidOperationException($"F4-B1 {name}: fallback fixture unexpectedly has selected assessment/capacity state.");
                }

                if (boundary.UserResultText != legacySummary)
                    throw new InvalidOperationException($"F4-B1 {name}: non-selected user summary is not exact legacy fallback.");
                AssertRowsEqual(projectedRows, legacyRows, name + " legacy rows");
                if (boundary.TechnicalReportText != legacyTechnical)
                    throw new InvalidOperationException($"F4-B1 {name}: technical report changed before F4-B2.");

                fallbackCount++;
                Console.WriteLine(string.Join("|",
                    "F4B1_SELECTED_USER_PRESENTATION",
                    name,
                    "Selected=False",
                    "UserSummary=LegacyExact",
                    "ElementRows=LegacyExact",
                    "TechnicalReportMigration=False"));
            }
            else
            {
                var assessment = snapshot.SelectedEngineeringAssessment
                    ?? throw new InvalidOperationException($"F4-B1 {name}: selected assessment missing.");
                var capacity = snapshot.SelectedLocalStructuralCapacity
                    ?? throw new InvalidOperationException($"F4-B1 {name}: selected local capacity missing.");
                var anchorReaction = snapshot.SelectedAnchorReaction
                    ?? throw new InvalidOperationException($"F4-B1 {name}: selected anchor reaction missing.");

                ValidateSelectedSummary(name, boundary.UserResultText, assessment, capacity, anchorReaction, legacySummary);
                ValidateSelectedRows(name, run.Result, projectedRows, assessment, capacity);

                if (boundary.TechnicalReportText != legacyTechnical)
                    throw new InvalidOperationException($"F4-B1 {name}: technical report changed before F4-B2.");

                selectedCount++;
                Console.WriteLine(string.Join("|",
                    "F4B1_SELECTED_USER_PRESENTATION",
                    name,
                    "Selected=True",
                    $"Verdict={assessment.Verdict}",
                    $"AnchorContact={assessment.AnchorContactClassification}",
                    $"GoverningElement={assessment.GoverningWeakLinkElementNumber?.ToString(CultureInfo.InvariantCulture) ?? "None"}",
                    $"GoverningReserve={F(assessment.GoverningWeakLinkReserve)}",
                    "LegacyAnchorReserveDisplayed=False",
                    "TechnicalReportMigration=False"));
            }

            if (run.Result.Verdict != resultVerdict || run.Result.MainRisk != resultMainRisk)
                throw new InvalidOperationException($"F4-B1 {name}: CalculationResult Verdict/MainRisk mutated by presentation projection.");
            if (!run.Result.Checks.SequenceEqual(resultChecks))
                throw new InvalidOperationException($"F4-B1 {name}: CalculationResult.Checks mutated by presentation projection.");
            Exact(run.Result.WeakLinkBreakingLoadKn, resultWeakLinkKn, name + " weak-link MBL unchanged");
            if (run.Result.WeakLinkName != resultWeakLinkName)
                throw new InvalidOperationException($"F4-B1 {name}: legacy WeakLinkName mutated.");
            Exact(run.Result.TensionReserve, resultTensionReserve, name + " legacy tension reserve unchanged");
            Exact(run.Result.AnchorHoldingKg, resultAnchorHoldingKg, name + " legacy anchor holding unchanged");
            Exact(run.Result.RequiredAnchorHoldingKg, resultRequiredAnchorHoldingKg, name + " legacy required anchor holding unchanged");
            Exact(run.Result.AnchorReserve, resultAnchorReserve, name + " legacy anchor reserve unchanged");
            if (!run.Result.ElementRows.SequenceEqual(resultRows))
                throw new InvalidOperationException($"F4-B1 {name}: CalculationResult.ElementRows mutated.");
            if (!ReferenceEquals(snapshot.SelectedShape, selectedShape) ||
                !ReferenceEquals(snapshot.ShadowSelectedCore?.Shape, selectedCoreShape))
            {
                throw new InvalidOperationException($"F4-B1 {name}: selected X/Z read-model identity changed.");
            }
        }

        if (definitions.Count != 5 || selectedCount != 2 || fallbackCount != 3)
        {
            throw new InvalidOperationException(
                $"F4-B1 canonical coverage mismatch: scenarios={definitions.Count}, selected={selectedCount}, fallback={fallbackCount}.");
        }

        Console.WriteLine(
            "F4B1_SELECTED_USER_PRESENTATION_ROLLUP|CanonicalScenarios=5|Selected=2|LegacyFallback=3|UserSummarySelectedAuthority=True|ElementTableSelectedAuthority=True|PdfRendererPhysicsChanged=False|TechnicalReportMigration=False|CalculationResultMutated=False|SelectedGeometryChanged=False");
        Console.WriteLine("F4B1_SELECTED_USER_PRESENTATION_END");
    }

    private static void ValidateSelectedSummary(
        string scenario,
        string summary,
        MooringSelectedEngineeringAssessmentState assessment,
        MooringSelectedLocalStructuralCapacityState capacity,
        MooringSelectedAnchorReactionState anchorReaction,
        string legacySummary)
    {
        RequireContains(summary, $"Вердикт: {assessment.Verdict}", scenario);
        RequireContains(summary, $"Главный риск: {assessment.MainRisk}", scenario);
        RequireContains(summary, "Расчётная selected design-нагрузка:", scenario);
        RequireContains(summary, "Определяющий локальный несущий элемент:", scenario);
        RequireContains(summary, "Локальный запас определяющего элемента:", scenario);
        RequireContains(summary, "Контакт якоря:", scenario);
        RequireContains(summary, $"Горизонтальная selected-нагрузка на якорь: {anchorReaction.HorizontalDemandN:0.##} Н", scenario);
        RequireContains(summary, "Горизонтальная удерживающая способность якоря: требуется отдельная валидированная модель якорь/грунт", scenario);

        if (assessment.GoverningWeakLinkElementNumber.HasValue)
        {
            RequireContains(summary, $"#{assessment.GoverningWeakLinkElementNumber.Value}", scenario);
            if (!string.IsNullOrWhiteSpace(assessment.GoverningWeakLinkTitle))
                RequireContains(summary, assessment.GoverningWeakLinkTitle!, scenario);
        }

        if (summary.Contains("Запас якоря:", StringComparison.Ordinal) ||
            summary.Contains("Нагрузка слабого звена:", StringComparison.Ordinal) ||
            summary.Contains("Запас слабого звена:", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"F4-B1 {scenario}: selected summary leaks legacy weak-link/anchor authority labels.");
        }

        if (summary == legacySummary)
            throw new InvalidOperationException($"F4-B1 {scenario}: selected summary did not switch from legacy presentation.");

        if (capacity.GoverningReserve != assessment.GoverningWeakLinkReserve ||
            capacity.GoverningElementNumber != assessment.GoverningWeakLinkElementNumber)
        {
            throw new InvalidOperationException($"F4-B1 {scenario}: selected summary governing capacity provenance changed before rendering.");
        }
    }

    private static void ValidateSelectedRows(
        string scenario,
        CalculationResult result,
        IReadOnlyList<ElementCalculationDisplayRow> projectedRows,
        MooringSelectedEngineeringAssessmentState assessment,
        MooringSelectedLocalStructuralCapacityState capacity)
    {
        if (projectedRows.Count != result.ElementRows.Count)
            throw new InvalidOperationException($"F4-B1 {scenario}: projected row count changed.");

        var projected = projectedRows.ToDictionary(x => x.Number);
        var source = result.ElementRows.ToDictionary(x => x.Number);
        var capacityByNumber = capacity.Rows.ToDictionary(x => x.ElementNumber);

        foreach (var sourceRow in result.ElementRows)
        {
            if (!projected.TryGetValue(sourceRow.Number, out var display))
                throw new InvalidOperationException($"F4-B1 {scenario}: projected row {sourceRow.Number} missing.");

            if (display.Number != sourceRow.Number || display.Kind != sourceRow.Kind || display.Title != sourceRow.Title)
                throw new InvalidOperationException($"F4-B1 {scenario}: sequence identity changed at row {sourceRow.Number}.");

            if (string.Equals(sourceRow.Kind, "Буй", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(display.BreakingLoadKn) ||
                    !string.IsNullOrEmpty(display.WorkingLoadKn) ||
                    !string.IsNullOrEmpty(display.Reserve))
                {
                    throw new InvalidOperationException($"F4-B1 {scenario}: buoy row fabricated structural capacity fields.");
                }
                continue;
            }

            if (string.Equals(sourceRow.Kind, "Якорь", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(display.Reserve) ||
                    display.Status.Contains("запас якоря", StringComparison.OrdinalIgnoreCase) ||
                    !display.Status.Contains("модели якорь/грунт", StringComparison.OrdinalIgnoreCase) ||
                    !display.PresetName.StartsWith("compatibility holding factors only; ", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"F4-B1 {scenario}: anchor display still exposes legacy holding reserve as selected authority.");
                }
                continue;
            }

            if (!capacityByNumber.TryGetValue(sourceRow.Number, out var selected))
                throw new InvalidOperationException($"F4-B1 {scenario}: F3-C row missing at internal element {sourceRow.Number}.");

            var expectedMbl = Format(selected.BreakingLoadKn);
            var expectedWll = Format(selected.WorkingLoadKn);
            var expectedReserve = Format(selected.LocalReserve);
            if (display.BreakingLoadKn != expectedMbl ||
                display.WorkingLoadKn != expectedWll ||
                display.Reserve != expectedReserve)
            {
                throw new InvalidOperationException(
                    $"F4-B1 {scenario}: selected capacity display mismatch at element {sourceRow.Number}: expected MBL/WLL/Reserve {expectedMbl}/{expectedWll}/{expectedReserve}, got {display.BreakingLoadKn}/{display.WorkingLoadKn}/{display.Reserve}.");
            }

            if (selected.Status == MooringLocalStructuralCapacityStatus.NotRatedByCurrentModel)
            {
                if (!display.Status.Contains("не оценивается", StringComparison.OrdinalIgnoreCase) ||
                    !string.IsNullOrEmpty(display.BreakingLoadKn) ||
                    !string.IsNullOrEmpty(display.WorkingLoadKn) ||
                    !string.IsNullOrEmpty(display.Reserve))
                {
                    throw new InvalidOperationException($"F4-B1 {scenario}: payload/non-rated row was presented as capacity-rated at {sourceRow.Number}.");
                }
            }
            else if (selected.Status == MooringLocalStructuralCapacityStatus.Insufficient &&
                     !display.Status.Contains("локальный запас < 1", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"F4-B1 {scenario}: insufficient local reserve status lost at {sourceRow.Number}.");
            }
        }

        var anchorSource = source.Values.Single(x => string.Equals(x.Kind, "Якорь", StringComparison.OrdinalIgnoreCase));
        var anchorDisplay = projected[anchorSource.Number];
        if (assessment.AnchorHorizontalCapacityDisposition != MooringAnchorHorizontalCapacityDisposition.RequiresAdditionalPhysicalModel ||
            !anchorDisplay.Status.Contains("модели якорь/грунт", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"F4-B1 {scenario}: selected anchor-capacity review disposition was not projected.");
        }
    }

    private static void AssertRowsEqual(
        IReadOnlyList<ElementCalculationDisplayRow> actual,
        IReadOnlyList<ElementCalculationDisplayRow> expected,
        string label)
    {
        if (actual.Count != expected.Count)
            throw new InvalidOperationException($"F4-B1 {label}: row count differs.");

        for (var i = 0; i < actual.Count; i++)
        {
            var a = actual[i];
            var e = expected[i];
            if (a.Number != e.Number || a.Kind != e.Kind || a.Title != e.Title || a.PresetName != e.PresetName ||
                a.LengthM != e.LengthM || a.Count != e.Count || a.WeightWaterKg != e.WeightWaterKg ||
                a.ProjectedAreaM2 != e.ProjectedAreaM2 || a.DragCoefficient != e.DragCoefficient ||
                a.CurrentForceN != e.CurrentForceN || a.BreakingLoadKn != e.BreakingLoadKn ||
                a.WorkingLoadKn != e.WorkingLoadKn || a.Reserve != e.Reserve || a.Status != e.Status)
            {
                throw new InvalidOperationException($"F4-B1 {label}: display row {i + 1} is not exact legacy fallback.");
            }
        }
    }

    private static void RequireContains(string text, string expected, string scenario)
    {
        if (!text.Contains(expected, StringComparison.Ordinal))
            throw new InvalidOperationException($"F4-B1 {scenario}: selected user summary missing '{expected}'.");
    }

    private static IEnumerable HistoricalDefinitions()
    {
        var builder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("F4-B1: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");
        return builder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException("F4-B1: historical fixtures are unavailable.");
    }

    private static T Property<T>(object source, string name)
    {
        var property = source.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"F4-B1: property {source.GetType().Name}.{name} was not found.");
        var value = property.GetValue(source);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"F4-B1: property {source.GetType().Name}.{name} is not {typeof(T).Name}.");
    }

    private static void Exact(double actual, double expected, string label)
    {
        if (actual != expected)
            throw new InvalidOperationException($"F4-B1 {label}: expected exact {expected:R}, got {actual:R}.");
    }

    private static string Format(double? value) =>
        value.HasValue ? value.Value.ToString("0.####", CultureInfo.InvariantCulture) : string.Empty;

    private static string F(double? value) =>
        value.HasValue ? value.Value.ToString("R", CultureInfo.InvariantCulture) : "None";
}
