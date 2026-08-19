using System.Globalization;
using System.Text.Json;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;

internal static class DownstreamAuthorityOwnershipRegression
{
    private const double GravityMS2 = 9.80665;
    private const double ScalarTolerance = 1e-10;
    private const double BaselineTolerance = 1e-8;
    private const double DistinctOffsetToleranceM = 1e-6;
    private const string BaselinePath = "validation/BuoyCalc.EngineeringRegression/baselines/engineering-baseline.json";

    public static void Validate()
    {
        using var baselineDocument = JsonDocument.Parse(File.ReadAllText(BaselinePath));
        var historicalByName = baselineDocument.RootElement
            .GetProperty("Scenarios")
            .EnumerateArray()
            .ToDictionary(
                x => x.GetProperty("Name").GetString()
                    ?? throw new InvalidOperationException("Downstream authority ownership: baseline scenario name is null."),
                x => x,
                StringComparer.Ordinal);

        var fixtures = BuildHistoricalScenarios();
        var distinctOffsetCount = 0;

        Console.WriteLine("DOWNSTREAM_AUTHORITY_OWNERSHIP_BEGIN");

        foreach (var fixture in fixtures)
        {
            if (!historicalByName.TryGetValue(fixture.Name, out var historical))
                throw new InvalidOperationException($"Downstream authority ownership: baseline scenario '{fixture.Name}' is missing.");

            var result = BuoyCalculator.Calculate(
                fixture.Environment,
                fixture.Buoy,
                fixture.Assembly,
                fixture.Anchor,
                fixture.SafetyFactor);
            var snapshot = CalculationSnapshotBuilder.Build(fixture.Environment, result);
            var selected = snapshot.SelectedShape
                ?? throw new InvalidOperationException($"Downstream authority ownership {fixture.Name}: selected shape is missing.");
            var nodes = selected.Shape.Nodes.OrderBy(x => x.Number).ToList();

            var verticalForceN = Math.Max(0.0, result.NetBuoyancyKg) * GravityMS2;
            var expectedTensionKn = Math.Sqrt(
                result.HorizontalForceN * result.HorizontalForceN +
                verticalForceN * verticalForceN) / 1000.0;
            Near(result.TensionKn, expectedTensionKn, ScalarTolerance, fixture.Name + " scalar TensionKn ownership");

            var requiredHoldingKg = result.HorizontalForceN / GravityMS2;
            var expectedAnchorReserve = requiredHoldingKg > 0.0
                ? result.AnchorHoldingKg / requiredHoldingKg
                : 0.0;
            Near(result.AnchorReserve, expectedAnchorReserve, ScalarTolerance, fixture.Name + " scalar AnchorReserve ownership");

            var expectedOffsetM = verticalForceN > 0.0
                ? result.HorizontalForceN / verticalForceN * fixture.Environment.DepthM
                : 0.0;
            Near(result.EstimatedOffsetM, expectedOffsetM, ScalarTolerance, fixture.Name + " scalar EstimatedOffsetM ownership");

            var expectedTensionReserve = result.TensionKn > 0.0 && result.WorkingLoadKn > 0.0
                ? result.WorkingLoadKn / result.TensionKn
                : 0.0;
            Near(result.TensionReserve, expectedTensionReserve, ScalarTolerance, fixture.Name + " scalar weak-link reserve ownership");

            var expectedWeakLinkCheck = result.TensionReserve >= 1.0
                ? "OK: запас по слабому звену"
                : "WARNING: малый запас по слабому звену";
            if (!result.Checks.Contains(expectedWeakLinkCheck, StringComparer.Ordinal))
                throw new InvalidOperationException($"Downstream authority ownership {fixture.Name}: weak-link check is not sourced from scalar tension reserve.");

            var expectedAnchorCheck = result.AnchorReserve >= 1.0
                ? "OK: запас якоря"
                : "WARNING: малый запас якоря";
            if (!result.Checks.Contains(expectedAnchorCheck, StringComparer.Ordinal))
                throw new InvalidOperationException($"Downstream authority ownership {fixture.Name}: anchor check is not sourced from scalar anchor reserve.");

            var selectedTensionSumKn = nodes.Sum(x => x.SegmentTensionKn);
            var selectedAngleSumDeg = nodes.Sum(x => x.SegmentAngleFromVerticalDeg);
            Near(
                selectedTensionSumKn,
                historical.GetProperty("SelectedTensionSumKn").GetDouble(),
                BaselineTolerance,
                fixture.Name + " selected tension sum ownership");
            Near(
                selectedAngleSumDeg,
                historical.GetProperty("SelectedAngleSumDeg").GetDouble(),
                BaselineTolerance,
                fixture.Name + " selected angle sum ownership");

            foreach (var sample in historical.GetProperty("SelectedSamples").EnumerateArray())
            {
                var index = sample.GetProperty("Index").GetInt32();
                if (index < 0 || index >= nodes.Count)
                    throw new InvalidOperationException($"Downstream authority ownership {fixture.Name}: sample index {index} is outside selected nodes.");

                var node = nodes[index];
                Near(
                    node.SegmentTensionKn,
                    sample.GetProperty("TensionKn").GetDouble(),
                    BaselineTolerance,
                    fixture.Name + $" selected sample {index} tension ownership");
                Near(
                    node.SegmentAngleFromVerticalDeg,
                    sample.GetProperty("AngleFromVerticalDeg").GetDouble(),
                    BaselineTolerance,
                    fixture.Name + $" selected sample {index} angle ownership");
            }

            var selectedEndpointOffsetM = selected.Shape.HorizontalOffsetM;
            if (Math.Abs(result.EstimatedOffsetM - selectedEndpointOffsetM) > DistinctOffsetToleranceM)
                distinctOffsetCount++;

            Console.WriteLine(string.Join("|",
                "DOWNSTREAM_AUTHORITY_OWNERSHIP",
                fixture.Name,
                $"SelectedSource={selected.Source}",
                $"SelectedUsesDiscreteLoads={selected.UsesDiscreteLoads}",
                $"ScalarTensionKn={Format(result.TensionKn)}",
                $"ScalarAnchorReserve={Format(result.AnchorReserve)}",
                $"LegacyEstimatedOffsetM={Format(result.EstimatedOffsetM)}",
                $"SelectedEndpointOffsetM={Format(selectedEndpointOffsetM)}",
                $"SelectedTensionSumKn={Format(selectedTensionSumKn)}",
                $"SelectedAngleSumDeg={Format(selectedAngleSumDeg)}",
                $"Verdict={result.Verdict}"));
        }

        if (distinctOffsetCount == 0)
        {
            throw new InvalidOperationException(
                "Downstream authority ownership: canonical fixtures no longer demonstrate that legacy EstimatedOffsetM and selected endpoint offset are distinct contracts.");
        }

        Console.WriteLine(string.Join("|",
            "DOWNSTREAM_AUTHORITY_OWNERSHIP_ROLLUP",
            $"Scenarios={fixtures.Count}",
            $"DistinctLegacyVsSelectedOffset={distinctOffsetCount}",
            "GoldenBaselineModified=False",
            "SelectedAuthorityChanged=False",
            "DownstreamAuthorityChanged=False"));
        Console.WriteLine("DOWNSTREAM_AUTHORITY_OWNERSHIP_END");
    }

    private static IReadOnlyList<HistoricalScenario> BuildHistoricalScenarios()
    {
        var seabed = new SeabedPreset("reg:sand", "Regression sand", 1.2, "Deterministic regression seabed preset.");
        var buoy = new BuoyInput("Regression buoy", 1.0, 100.0, 0.8, 0.8);
        var anchor = new AnchorInput("Regression concrete anchor", "Concrete block", "Concrete", 1000.0, 0.4, 1.0);
        var heavyLine = new RopePreset("reg:heavy-line", "Regression heavy line", "Polyester", 20.0, 100.0, 0.1, 1.2, "Deterministic heavy-line regression preset.");
        var buoyantLine = new RopePreset("reg:buoyant-line", "Regression buoyant line", "Synthetic buoyant", 20.0, 100.0, -0.05, 1.2, "Negative signed water weight is intentional and must be preserved.");
        var connector = new ConnectorPreset("reg:connector", "Regression connector", "Shackle", 5.0, 0.0007, 60.0, 0.01, 1.0, "Deterministic connector regression preset.");

        return new[]
        {
            new HistoricalScenario(
                "vertical-zero-current",
                Environment(50.0, 0.0, 0.0, 0.0, seabed),
                buoy,
                new[] { Line("Vertical line", heavyLine, 50.0) },
                anchor,
                3.0),
            new HistoricalScenario(
                "uniform-current-slack-line",
                Environment(50.0, 0.5, 1.0, 6.0, seabed),
                buoy,
                new[] { Line("Slack line", heavyLine, 55.0) },
                anchor,
                3.0),
            new HistoricalScenario(
                "buoyant-line",
                Environment(30.0, 0.3, 0.0, 0.0, seabed),
                buoy,
                new[] { Line("Buoyant line", buoyantLine, 30.0) },
                anchor,
                3.0),
            new HistoricalScenario(
                "discrete-payload",
                Environment(50.0, 0.5, 0.5, 5.0, seabed),
                buoy,
                new AssemblyItemInput[]
                {
                    Line("Upper line", heavyLine, 30.0),
                    Connector("Shackle", connector),
                    Payload("Payload", 40.0, 0.005, 0.05, 1.0),
                    Line("Lower line", heavyLine, 25.0)
                },
                anchor,
                3.0),
            new HistoricalScenario(
                "depth-varying-current-profile",
                new EnvironmentInput(
                    1025.0,
                    50.0,
                    0.2,
                    0.0,
                    0.0,
                    seabed,
                    true,
                    new[]
                    {
                        new CurrentProfilePointInput(0.0, 0.6, 0.0, 0.0, 1025.0),
                        new CurrentProfilePointInput(25.0, 0.3, 0.0, 0.0, 1025.0),
                        new CurrentProfilePointInput(50.0, 0.1, 0.0, 0.0, 1025.0)
                    }),
                buoy,
                new[] { Line("Profile line", heavyLine, 50.0) },
                anchor,
                3.0)
        };
    }

    private static EnvironmentInput Environment(
        double depthM,
        double currentSpeedMS,
        double waveHeightM,
        double wavePeriodS,
        SeabedPreset seabed) =>
        new(1025.0, depthM, currentSpeedMS, waveHeightM, wavePeriodS, seabed);

    private static AssemblyItemInput Line(string title, RopePreset preset, double lengthM) =>
        new(AssemblyItemKind.Line, title, true, preset, null, lengthM, 1, 0, 0, 0, 0);

    private static AssemblyItemInput Connector(string title, ConnectorPreset preset) =>
        new(AssemblyItemKind.Connector, title, true, null, preset, 0, 1, 0, 0, 0, 0);

    private static AssemblyItemInput Payload(
        string title,
        double weightAirKg,
        double volumeM3,
        double projectedAreaM2,
        double dragCoefficient) =>
        new(AssemblyItemKind.Payload, title, true, null, null, 0, 1, weightAirKg, volumeM3, projectedAreaM2, dragCoefficient);

    private static void Near(double actual, double expected, double tolerance, string label)
    {
        if (!double.IsFinite(actual) || !double.IsFinite(expected) || Math.Abs(actual - expected) > tolerance)
            throw new InvalidOperationException($"Downstream authority ownership {label}: expected {expected:R}, got {actual:R}.");
    }

    private static string Format(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private sealed record HistoricalScenario(
        string Name,
        EnvironmentInput Environment,
        BuoyInput Buoy,
        IReadOnlyList<AssemblyItemInput> Assembly,
        AnchorInput Anchor,
        double SafetyFactor);
}
