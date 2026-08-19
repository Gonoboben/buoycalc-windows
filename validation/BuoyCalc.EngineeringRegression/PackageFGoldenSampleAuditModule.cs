using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

internal static class PackageFGoldenSampleAuditModule
{
    private const string BaselinePath = "validation/BuoyCalc.EngineeringRegression/baselines/engineering-baseline.json";

    [ModuleInitializer]
    public static void Run()
    {
        var regressionType = typeof(HistoricalGoldenImpactRegression);
        var buildScenarios = regressionType.GetMethod("BuildHistoricalScenarios", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Package F audit: BuildHistoricalScenarios method not found.");
        var runCandidate = regressionType.GetMethod("RunCandidate", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Package F audit: RunCandidate method not found.");

        var definitions = buildScenarios.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException("Package F audit: fixture definitions are unavailable.");

        using var baselineDocument = JsonDocument.Parse(File.ReadAllText(BaselinePath));
        var historicalByName = baselineDocument.RootElement
            .GetProperty("Scenarios")
            .EnumerateArray()
            .ToDictionary(
                x => x.GetProperty("Name").GetString()
                    ?? throw new InvalidOperationException("Package F audit: baseline scenario name is null."),
                x => x,
                StringComparer.Ordinal);

        Console.WriteLine("PACKAGE_F_SAMPLE_AUDIT_BEGIN");

        foreach (var definition in definitions.Cast<object>())
        {
            var definitionType = definition.GetType();
            var name = definitionType.GetProperty("Name")?.GetValue(definition) as string
                ?? throw new InvalidOperationException("Package F audit: fixture name is unavailable.");

            var candidate = runCandidate.Invoke(null, new[] { definition })
                ?? throw new InvalidOperationException($"Package F audit {name}: candidate is unavailable.");
            var candidateType = candidate.GetType();
            var available = candidateType.GetProperty("Available")?.GetValue(candidate) as bool?
                ?? throw new InvalidOperationException($"Package F audit {name}: availability is unavailable.");

            if (!available)
                continue;

            if (!historicalByName.TryGetValue(name, out var historical))
                throw new InvalidOperationException($"Package F audit {name}: historical baseline entry is missing.");

            var nodeObjects = (candidateType.GetProperty("Nodes")?.GetValue(candidate) as IEnumerable)?.Cast<object>().ToList()
                ?? throw new InvalidOperationException($"Package F audit {name}: candidate nodes are unavailable.");

            foreach (var sample in historical.GetProperty("SelectedSamples").EnumerateArray())
            {
                var index = sample.GetProperty("Index").GetInt32();
                if (index < 0 || index >= nodeObjects.Count)
                    throw new InvalidOperationException($"Package F audit {name}: sample index {index} is outside candidate nodes.");

                var node = nodeObjects[index];
                var nodeType = node.GetType();
                var candidateX = Convert.ToDouble(nodeType.GetProperty("XM")?.GetValue(node), CultureInfo.InvariantCulture);
                var candidateZ = Convert.ToDouble(nodeType.GetProperty("ZM")?.GetValue(node), CultureInfo.InvariantCulture);
                var historicalX = sample.GetProperty("XOffsetM").GetDouble();
                var historicalZ = sample.GetProperty("ZDepthM").GetDouble();

                Console.WriteLine(string.Join("|",
                    "PACKAGE_F_SAMPLE",
                    name,
                    $"Index={index}",
                    $"HistoricalX={historicalX.ToString("R", CultureInfo.InvariantCulture)}",
                    $"CandidateX={candidateX.ToString("R", CultureInfo.InvariantCulture)}",
                    $"DeltaX={(candidateX - historicalX).ToString("R", CultureInfo.InvariantCulture)}",
                    $"HistoricalZ={historicalZ.ToString("R", CultureInfo.InvariantCulture)}",
                    $"CandidateZ={candidateZ.ToString("R", CultureInfo.InvariantCulture)}",
                    $"DeltaZ={(candidateZ - historicalZ).ToString("R", CultureInfo.InvariantCulture)}"));
            }
        }

        Console.WriteLine("PACKAGE_F_SAMPLE_AUDIT_END");
    }
}
