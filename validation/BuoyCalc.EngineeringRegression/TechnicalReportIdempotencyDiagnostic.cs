using System.Collections;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class TechnicalReportIdempotencyDiagnostic
{
    public static void Validate()
    {
        var builder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("F4-B1 diagnostic: historical scenario builder missing.");
        var definitions = builder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException("F4-B1 diagnostic: historical scenarios unavailable.");
        var definition = definitions.Cast<object>().First(x => Property<string>(x, "Name") == "uniform-current-slack-line");

        var environment = Property<EnvironmentInput>(definition, "Environment");
        var buoy = Property<BuoyInput>(definition, "Buoy");
        var assembly = Property<IReadOnlyList<AssemblyItemInput>>(definition, "Assembly");
        var anchor = Property<AnchorInput>(definition, "Anchor");
        var safetyFactor = Property<double>(definition, "SafetyFactor");
        var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, safetyFactor);
        var snapshot = run.Snapshot;

        var baseline = Render(environment, buoy, anchor, snapshot);
        AssertExact(baseline, Render(environment, buoy, anchor, snapshot), "direct second render");
        Console.WriteLine("F4B1_TECHNICAL_REPORT_IDEMPOTENCY|DirectDoubleRender=Exact");

        _ = UserReportBuilder.Build(environment, run.Result);
        AssertExact(baseline, Render(environment, buoy, anchor, snapshot), "after legacy user summary");
        Console.WriteLine("F4B1_TECHNICAL_REPORT_IDEMPOTENCY|AfterLegacySummary=Exact");

        _ = run.Result.ElementRows.Select(ElementCalculationDisplayRow.From).ToList();
        AssertExact(baseline, Render(environment, buoy, anchor, snapshot), "after legacy element rows");
        Console.WriteLine("F4B1_TECHNICAL_REPORT_IDEMPOTENCY|AfterLegacyElementRows=Exact");

        _ = run.Result.Verdict;
        _ = run.Result.MainRisk;
        _ = run.Result.Checks.ToArray();
        _ = run.Result.WeakLinkBreakingLoadKn;
        _ = run.Result.WeakLinkName;
        _ = run.Result.TensionReserve;
        _ = run.Result.AnchorHoldingKg;
        _ = run.Result.RequiredAnchorHoldingKg;
        _ = run.Result.AnchorReserve;
        _ = run.Result.ElementRows.ToArray();
        _ = snapshot.SelectedShape;
        _ = snapshot.ShadowSelectedCore?.Shape;
        AssertExact(baseline, Render(environment, buoy, anchor, snapshot), "after regression capture reads");
        Console.WriteLine("F4B1_TECHNICAL_REPORT_IDEMPOTENCY|AfterCaptureReads=Exact");

        _ = UserReportBuilder.Build(environment, snapshot);
        AssertExact(baseline, Render(environment, buoy, anchor, snapshot), "after selected user summary");
        Console.WriteLine("F4B1_TECHNICAL_REPORT_IDEMPOTENCY|AfterSelectedSummary=Exact");

        _ = SelectedElementCalculationDisplayProjector.Project(snapshot);
        AssertExact(baseline, Render(environment, buoy, anchor, snapshot), "after selected element projector");
        Console.WriteLine("F4B1_TECHNICAL_REPORT_IDEMPOTENCY|AfterElementProjector=Exact");

        _ = ReportBuildBoundary.Build(
            "F4-B1 idempotency",
            environment,
            buoy,
            anchor,
            snapshot);
        AssertExact(baseline, Render(environment, buoy, anchor, snapshot), "after report build boundary");
        Console.WriteLine("F4B1_TECHNICAL_REPORT_IDEMPOTENCY|AfterReportBuildBoundary=Exact");
    }

    private static string Render(
        EnvironmentInput environment,
        BuoyInput buoy,
        AnchorInput anchor,
        CalculationSnapshot snapshot) =>
        TechnicalReportBuilder.Build("F4-B1 idempotency", environment, buoy, anchor, snapshot);

    private static void AssertExact(string expected, string actual, string stage)
    {
        if (expected == actual)
            return;

        var a = Normalize(expected);
        var b = Normalize(actual);
        var count = Math.Max(a.Length, b.Length);
        for (var i = 0; i < count; i++)
        {
            var left = i < a.Length ? a[i] : "<missing>";
            var right = i < b.Length ? b[i] : "<missing>";
            if (left == right) continue;

            throw new InvalidOperationException(
                $"F4-B1 technical report changed {stage} at line {i + 1}. " +
                $"Expected heading='{FindHeading(a, i)}', actual heading='{FindHeading(b, i)}'. " +
                $"EXPECTED CONTEXT: {Context(a, i)} ACTUAL CONTEXT: {Context(b, i)}");
        }

        throw new InvalidOperationException(
            $"F4-B1 technical report changed {stage}; lengths expected/actual={expected.Length}/{actual.Length}.");
    }

    private static string[] Normalize(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

    private static string FindHeading(string[] lines, int index)
    {
        for (var i = Math.Min(index, lines.Length - 1); i >= 0; i--)
        {
            if (lines[i].StartsWith("## ", StringComparison.Ordinal)) return lines[i];
        }
        return "<no heading>";
    }

    private static string Context(string[] lines, int index)
    {
        var start = Math.Max(0, index - 3);
        var end = Math.Min(lines.Length - 1, index + 2);
        return string.Join(" || ", Enumerable.Range(start, end - start + 1).Select(i => $"L{i + 1}:{lines[i]}"));
    }

    private static T Property<T>(object source, string name)
    {
        var property = source.GetType().GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"F4-B1 diagnostic: property {source.GetType().Name}.{name} missing.");
        var value = property.GetValue(source);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"F4-B1 diagnostic: property {source.GetType().Name}.{name} has wrong type.");
    }
}
