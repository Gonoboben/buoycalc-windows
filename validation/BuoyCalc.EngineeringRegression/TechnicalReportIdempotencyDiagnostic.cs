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

        var first = TechnicalReportBuilder.Build("F4-B1 idempotency", environment, buoy, anchor, run.Snapshot);
        var second = TechnicalReportBuilder.Build("F4-B1 idempotency", environment, buoy, anchor, run.Snapshot);
        if (first == second)
        {
            Console.WriteLine("F4B1_TECHNICAL_REPORT_IDEMPOTENCY|DirectDoubleRender=Exact");
            return;
        }

        var a = Normalize(first);
        var b = Normalize(second);
        var count = Math.Max(a.Length, b.Length);
        for (var i = 0; i < count; i++)
        {
            var left = i < a.Length ? a[i] : "<missing>";
            var right = i < b.Length ? b[i] : "<missing>";
            if (left == right) continue;

            var firstHeading = FindHeading(a, i);
            var secondHeading = FindHeading(b, i);
            var firstContext = Context(a, i);
            var secondContext = Context(b, i);
            throw new InvalidOperationException(
                $"F4-B1 direct technical report render is not idempotent at line {i + 1}. " +
                $"First heading='{firstHeading}', second heading='{secondHeading}'. " +
                $"FIRST CONTEXT: {firstContext} SECOND CONTEXT: {secondContext}");
        }

        throw new InvalidOperationException("F4-B1 direct technical report render differs only by trailing representation.");
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