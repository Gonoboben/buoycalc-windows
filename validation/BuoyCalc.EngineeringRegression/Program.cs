using System.Text.Json;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class Program
{
    private const double AbsoluteTolerance = 1e-8;
    private const double RelativeTolerance = 1e-8;
    private const double SegmentLengthToleranceM = 1e-9;
    private const double GeometryToleranceM = 0.011;

    private static readonly SeabedPreset RegressionSeabed = new(
        "reg:sand",
        "Regression sand",
        1.2,
        "Deterministic regression seabed preset.");

    private static readonly BuoyInput RegressionBuoy = new(
        "Regression buoy",
        1.0,
        100.0,
        0.8,
        0.8);

    private static readonly AnchorInput RegressionAnchor = new(
        "Regression concrete anchor",
        "Concrete block",
        "Concrete",
        1000.0,
        0.4,
        1.0);

    private static readonly RopePreset HeavyLine = new(
        "reg:heavy-line",
        "Regression heavy line",
        "Polyester",
        20.0,
        100.0,
        0.1,
        1.2,
        "Deterministic heavy-line regression preset.");

    private static readonly RopePreset BuoyantLine = new(
        "reg:buoyant-line",
        "Regression buoyant line",
        "Synthetic buoyant",
        20.0,
        100.0,
        -0.05,
        1.2,
        "Negative signed water weight is intentional and must be preserved.");

    private static readonly ConnectorPreset RegressionConnector = new(
        "reg:connector",
        "Regression connector",
        "Shackle",
        5.0,
        0.0007,
        60.0,
        0.01,
        1.0,
        "Deterministic connector regression preset.");

    public static int Main(string[] args)
    {
        try
        {
            var baseline = BuildBaseline();
            ValidateInvariants(baseline);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            if (args.Length == 2 && args[0] == "--write-baseline")
            {
                var path = Path.GetFullPath(args[1]);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, JsonSerializer.Serialize(baseline, options) + System.Environment.NewLine);
                Console.WriteLine($"Engineering regression baseline written: {path}");
                return 0;
            }

            if (args.Length == 2 && args[0] == "--verify")
            {
                VerifyBaseline(args[1], baseline, options);
                Console.WriteLine($"Engineering regression verification passed: {baseline.Scenarios.Count} scenarios.");
                return 0;
            }

            Console.WriteLine("BEGIN_ENGINEERING_REGRESSION_BASELINE");
            Console.WriteLine(JsonSerializer.Serialize(baseline, options));
            Console.WriteLine("END_ENGINEERING_REGRESSION_BASELINE");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Engineering regression failure:");
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static RegressionBaseline BuildBaseline()
    {
        var scenarios = new[]
        {
            RunScenario(new ScenarioDefinition(
                "vertical-zero-current",
                Environment(depthM: 50, currentSpeedMS: 0, waveHeightM: 0, wavePeriodS: 0),
                new[] { Line("Vertical line", HeavyLine, 50) },
                ExpectZeroCurrent: true)),

            RunScenario(new ScenarioDefinition(
                "uniform-current-slack-line",
                Environment(depthM: 50, currentSpeedMS: 0.5, waveHeightM: 1.0, wavePeriodS: 6.0),
                new[] { Line("Slack line", HeavyLine, 55) })),

            RunScenario(new ScenarioDefinition(
                "buoyant-line",
                Environment(depthM: 30, currentSpeedMS: 0.3, waveHeightM: 0, wavePeriodS: 0),
                new[] { Line("Buoyant line", BuoyantLine, 30) },
                ExpectNegativeLineWeight: true)),

            RunScenario(new ScenarioDefinition(
                "discrete-payload",
                Environment(depthM: 50, currentSpeedMS: 0.5, waveHeightM: 0.5, wavePeriodS: 5.0),
                new AssemblyItemInput[]
                {
                    Line("Upper line", HeavyLine, 30),
                    Connector("Shackle", RegressionConnector),
                    Payload("Payload", 40.0, 0.005, 0.05, 1.0),
                    Line("Lower line", HeavyLine, 25)
                },
                ExpectDiscreteLoads: true)),

            RunScenario(new ScenarioDefinition(
                "depth-varying-current-profile",
                new EnvironmentInput(
                    1025.0,
                    50.0,
                    0.2,
                    0,
                    0,
                    RegressionSeabed,
                    true,
                    new[]
                    {
                        new CurrentProfilePointInput(0, 0.6, 0, 0, 1025),
                        new CurrentProfilePointInput(25, 0.3, 0, 0, 1025),
                        new CurrentProfilePointInput(50, 0.1, 0, 0, 1025)
                    }),
                new[] { Line("Profile line", HeavyLine, 50) },
                ExpectProfileVariation: true))
        };

        return new RegressionBaseline(1, scenarios);
    }

    private static ScenarioSnapshot RunScenario(ScenarioDefinition definition)
    {
        ClearCompatibilityStores();

        var result = BuoyCalculator.Calculate(
            definition.Environment,
            RegressionBuoy,
            definition.AssemblyItems,
            RegressionAnchor,
            3.0);

        var snapshot = CalculationSnapshotBuilder.Build(definition.Environment, result);
        var selected = snapshot.SelectedShape
            ?? throw new InvalidOperationException($"{definition.Name}: selected X/Z shape is missing.");
        var shape = selected.Shape;
        var nodes = shape.Nodes.OrderBy(x => x.Number).ToList();
        var segments = result.SegmentRows.OrderBy(x => x.Number).ToList();
        var data = snapshot.TechnicalReportData;

        var samples = SampleNodes(nodes);
        var lineElementWeightWaterKg = result.ElementRows
            .Where(x => x.SourceLengthM > 0 && x.SourceDiameterMm > 0)
            .Sum(x => x.WeightWaterKg);

        var regression = new ScenarioSnapshot(
            definition.Name,
            definition.Environment.DepthM,
            result.BuoyancyKg,
            result.TotalWeightWaterKg,
            result.NetBuoyancyKg,
            result.CurrentForceN,
            result.WaveForceN,
            result.HorizontalForceN,
            result.TensionKn,
            result.AnchorHoldingKg,
            result.AnchorReserve,
            result.LineLengthM,
            result.EstimatedOffsetM,
            result.ElementRows.Count,
            segments.Count,
            segments.Sum(x => x.SegmentLengthM),
            segments.Count > 0 ? segments.Max(x => x.SegmentLengthM) : 0,
            segments.Sum(x => x.CurrentForceN),
            segments.Sum(x => x.WeightWaterKg),
            segments.Count > 0 ? segments.Min(x => x.LocalSpeedMS) : 0,
            segments.Count > 0 ? segments.Max(x => x.LocalSpeedMS) : 0,
            lineElementWeightWaterKg,
            selected.Source,
            selected.UsesDiscreteLoads,
            nodes.Count,
            shape.HorizontalOffsetM,
            shape.AnchorPoint?.ZDepthM ?? 0,
            shape.VerticalResidualM,
            shape.Converged,
            nodes.Sum(x => x.XOffsetM),
            nodes.Sum(x => x.ZDepthM),
            nodes.Sum(x => x.XOffsetM * x.XOffsetM),
            nodes.Sum(x => x.SegmentTensionKn),
            nodes.Sum(x => x.SegmentAngleFromVerticalDeg),
            samples,
            data.IterativeSolver.Converged,
            data.IterativeSolver.StopReason.ToString(),
            data.SequencePositions.DiscreteElementCount,
            data.Diagnostics.OverallSeverity.ToString());

        ValidateScenarioInvariants(definition, regression, result, snapshot);
        return regression;
    }

    private static void ValidateInvariants(RegressionBaseline baseline)
    {
        if (baseline.FormatVersion != 1)
        {
            throw new InvalidOperationException("Unexpected engineering regression format version.");
        }

        if (baseline.Scenarios.Count != 5)
        {
            throw new InvalidOperationException($"Expected 5 canonical scenarios, got {baseline.Scenarios.Count}.");
        }

        var duplicateNames = baseline.Scenarios
            .GroupBy(x => x.Name, StringComparer.Ordinal)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicateNames.Count > 0)
        {
            throw new InvalidOperationException("Duplicate regression scenario names: " + string.Join(", ", duplicateNames));
        }
    }

    private static void ValidateScenarioInvariants(
        ScenarioDefinition definition,
        ScenarioSnapshot regression,
        CalculationResult result,
        CalculationSnapshot snapshot)
    {
        AssertFinite(regression);

        AssertNear(
            regression.SegmentLengthSumM,
            regression.LineLengthM,
            SegmentLengthToleranceM,
            $"{definition.Name}: segment length conservation");

        if (regression.SegmentCount <= 0)
        {
            throw new InvalidOperationException($"{definition.Name}: no calculation segments were published.");
        }

        if (regression.MaxSegmentLengthM > 0.200000001)
        {
            throw new InvalidOperationException($"{definition.Name}: segment length {regression.MaxSegmentLengthM} m exceeds the fixed 0.20 m target.");
        }

        if (regression.SelectedNodeCount < 2)
        {
            throw new InvalidOperationException($"{definition.Name}: selected X/Z contains fewer than two nodes.");
        }

        if (regression.LineLengthM + SegmentLengthToleranceM >= definition.Environment.DepthM)
        {
            AssertNear(
                regression.SelectedAnchorDepthM,
                definition.Environment.DepthM,
                GeometryToleranceM,
                $"{definition.Name}: selected anchor depth closure");
        }

        if (definition.ExpectZeroCurrent)
        {
            AssertNear(regression.CurrentForceN, 0, AbsoluteTolerance, $"{definition.Name}: zero total current force");
            AssertNear(regression.SegmentCurrentForceSumN, 0, AbsoluteTolerance, $"{definition.Name}: zero segment current force");
            AssertNear(regression.SelectedHorizontalOffsetM, 0, GeometryToleranceM, $"{definition.Name}: vertical X/Z offset");
        }

        if (definition.ExpectNegativeLineWeight)
        {
            if (regression.SegmentWeightWaterSumKg >= 0 || regression.LineElementWeightWaterKg >= 0)
            {
                throw new InvalidOperationException($"{definition.Name}: negative signed line water weight was not preserved.");
            }
        }

        if (definition.ExpectDiscreteLoads)
        {
            if (regression.DiscreteElementCount <= 0)
            {
                throw new InvalidOperationException($"{definition.Name}: no discrete sequence positions were published.");
            }

            if (snapshot.TechnicalReportData.DiscreteLoadTensions.Rows.Count == 0)
            {
                throw new InvalidOperationException($"{definition.Name}: no discrete-load tension rows were published.");
            }
        }

        if (definition.ExpectProfileVariation)
        {
            if (regression.MaxLocalSpeedMS - regression.MinLocalSpeedMS <= 0.05)
            {
                throw new InvalidOperationException($"{definition.Name}: current-profile local segment speeds do not vary with depth.");
            }
        }

        var segmentForceResidual = Math.Abs(regression.SegmentCurrentForceSumN - result.SegmentRows.Sum(x => x.CurrentForceN));
        if (segmentForceResidual > AbsoluteTolerance)
        {
            throw new InvalidOperationException($"{definition.Name}: segment current-force aggregation is inconsistent.");
        }
    }

    private static IReadOnlyList<NodeSample> SampleNodes(IReadOnlyList<MooringShapePoint> nodes)
    {
        var indices = new SortedSet<int>
        {
            0,
            nodes.Count / 4,
            nodes.Count / 2,
            (nodes.Count * 3) / 4,
            nodes.Count - 1
        };

        return indices.Select(index =>
        {
            var node = nodes[index];
            return new NodeSample(
                index,
                node.XOffsetM,
                node.ZDepthM,
                node.SegmentTensionKn,
                node.SegmentAngleFromVerticalDeg);
        }).ToList();
    }

    private static void VerifyBaseline(
        string baselinePath,
        RegressionBaseline actual,
        JsonSerializerOptions options)
    {
        var path = Path.GetFullPath(baselinePath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Engineering regression baseline is missing.", path);
        }

        using var expectedDocument = JsonDocument.Parse(File.ReadAllText(path));
        using var actualDocument = JsonDocument.Parse(JsonSerializer.Serialize(actual, options));
        CompareJson(expectedDocument.RootElement, actualDocument.RootElement, "$", AbsoluteTolerance, RelativeTolerance);
    }

    private static void CompareJson(
        JsonElement expected,
        JsonElement actual,
        string path,
        double absoluteTolerance,
        double relativeTolerance)
    {
        if (expected.ValueKind != actual.ValueKind)
        {
            throw new InvalidOperationException($"{path}: JSON kind changed from {expected.ValueKind} to {actual.ValueKind}.");
        }

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
            {
                var expectedProperties = expected.EnumerateObject().ToDictionary(x => x.Name, x => x.Value, StringComparer.Ordinal);
                var actualProperties = actual.EnumerateObject().ToDictionary(x => x.Name, x => x.Value, StringComparer.Ordinal);

                if (!expectedProperties.Keys.OrderBy(x => x).SequenceEqual(actualProperties.Keys.OrderBy(x => x)))
                {
                    throw new InvalidOperationException($"{path}: baseline property set changed.");
                }

                foreach (var property in expectedProperties)
                {
                    CompareJson(property.Value, actualProperties[property.Key], path + "." + property.Key, absoluteTolerance, relativeTolerance);
                }

                break;
            }
            case JsonValueKind.Array:
            {
                var expectedItems = expected.EnumerateArray().ToArray();
                var actualItems = actual.EnumerateArray().ToArray();
                if (expectedItems.Length != actualItems.Length)
                {
                    throw new InvalidOperationException($"{path}: array length changed from {expectedItems.Length} to {actualItems.Length}.");
                }

                for (var index = 0; index < expectedItems.Length; index++)
                {
                    CompareJson(expectedItems[index], actualItems[index], $"{path}[{index}]", absoluteTolerance, relativeTolerance);
                }

                break;
            }
            case JsonValueKind.Number:
            {
                if (expected.TryGetInt64(out var expectedInteger) && actual.TryGetInt64(out var actualInteger))
                {
                    if (expectedInteger != actualInteger)
                    {
                        throw new InvalidOperationException($"{path}: integer changed from {expectedInteger} to {actualInteger}.");
                    }
                    break;
                }

                var expectedNumber = expected.GetDouble();
                var actualNumber = actual.GetDouble();
                if (!NearlyEqual(expectedNumber, actualNumber, absoluteTolerance, relativeTolerance))
                {
                    throw new InvalidOperationException($"{path}: number changed from {expectedNumber:R} to {actualNumber:R}.");
                }
                break;
            }
            case JsonValueKind.String:
                if (!string.Equals(expected.GetString(), actual.GetString(), StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"{path}: string changed from '{expected.GetString()}' to '{actual.GetString()}'.");
                }
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
                if (expected.GetBoolean() != actual.GetBoolean())
                {
                    throw new InvalidOperationException($"{path}: boolean changed.");
                }
                break;
            case JsonValueKind.Null:
                break;
            default:
                throw new InvalidOperationException($"{path}: unsupported JSON value kind {expected.ValueKind}.");
        }
    }

    private static bool NearlyEqual(double expected, double actual, double absoluteTolerance, double relativeTolerance)
    {
        if (!double.IsFinite(expected) || !double.IsFinite(actual))
        {
            return false;
        }

        var difference = Math.Abs(expected - actual);
        if (difference <= absoluteTolerance)
        {
            return true;
        }

        return difference <= relativeTolerance * Math.Max(1.0, Math.Max(Math.Abs(expected), Math.Abs(actual)));
    }

    private static void AssertNear(double actual, double expected, double tolerance, string label)
    {
        if (!double.IsFinite(actual) || !double.IsFinite(expected) || Math.Abs(actual - expected) > tolerance)
        {
            throw new InvalidOperationException($"{label}: expected {expected:R} ± {tolerance:R}, got {actual:R}.");
        }
    }

    private static void AssertFinite(ScenarioSnapshot scenario)
    {
        var numbers = new[]
        {
            scenario.DepthM,
            scenario.BuoyancyKg,
            scenario.TotalWeightWaterKg,
            scenario.NetBuoyancyKg,
            scenario.CurrentForceN,
            scenario.WaveForceN,
            scenario.HorizontalForceN,
            scenario.TensionKn,
            scenario.AnchorHoldingKg,
            scenario.AnchorReserve,
            scenario.LineLengthM,
            scenario.EstimatedOffsetM,
            scenario.SegmentLengthSumM,
            scenario.MaxSegmentLengthM,
            scenario.SegmentCurrentForceSumN,
            scenario.SegmentWeightWaterSumKg,
            scenario.MinLocalSpeedMS,
            scenario.MaxLocalSpeedMS,
            scenario.LineElementWeightWaterKg,
            scenario.SelectedHorizontalOffsetM,
            scenario.SelectedAnchorDepthM,
            scenario.SelectedVerticalResidualM,
            scenario.SelectedXSumM,
            scenario.SelectedZSumM,
            scenario.SelectedXSquaredSumM2,
            scenario.SelectedTensionSumKn,
            scenario.SelectedAngleSumDeg
        };

        if (numbers.Any(x => !double.IsFinite(x)))
        {
            throw new InvalidOperationException($"{scenario.Name}: regression snapshot contains non-finite values.");
        }

        foreach (var sample in scenario.SelectedSamples)
        {
            if (!double.IsFinite(sample.XOffsetM) ||
                !double.IsFinite(sample.ZDepthM) ||
                !double.IsFinite(sample.TensionKn) ||
                !double.IsFinite(sample.AngleFromVerticalDeg))
            {
                throw new InvalidOperationException($"{scenario.Name}: selected X/Z sample contains non-finite values.");
            }
        }
    }

    private static void ClearCompatibilityStores()
    {
        MooringAlternativeShapeStore.Clear();
    }

    private static EnvironmentInput Environment(
        double depthM,
        double currentSpeedMS,
        double waveHeightM,
        double wavePeriodS)
    {
        return new EnvironmentInput(
            1025.0,
            depthM,
            currentSpeedMS,
            waveHeightM,
            wavePeriodS,
            RegressionSeabed);
    }

    private static AssemblyItemInput Line(string title, RopePreset preset, double lengthM)
    {
        return new AssemblyItemInput(
            AssemblyItemKind.Line,
            title,
            true,
            preset,
            null,
            lengthM,
            1,
            0,
            0,
            0,
            0);
    }

    private static AssemblyItemInput Connector(string title, ConnectorPreset preset)
    {
        return new AssemblyItemInput(
            AssemblyItemKind.Connector,
            title,
            true,
            null,
            preset,
            0,
            1,
            0,
            0,
            0,
            0);
    }

    private static AssemblyItemInput Payload(
        string title,
        double weightAirKg,
        double volumeM3,
        double projectedAreaM2,
        double dragCoefficient)
    {
        return new AssemblyItemInput(
            AssemblyItemKind.Payload,
            title,
            true,
            null,
            null,
            0,
            1,
            weightAirKg,
            volumeM3,
            projectedAreaM2,
            dragCoefficient);
    }

    private sealed record ScenarioDefinition(
        string Name,
        EnvironmentInput Environment,
        IReadOnlyList<AssemblyItemInput> AssemblyItems,
        bool ExpectZeroCurrent = false,
        bool ExpectNegativeLineWeight = false,
        bool ExpectDiscreteLoads = false,
        bool ExpectProfileVariation = false);

    private sealed record RegressionBaseline(
        int FormatVersion,
        IReadOnlyList<ScenarioSnapshot> Scenarios);

    private sealed record ScenarioSnapshot(
        string Name,
        double DepthM,
        double BuoyancyKg,
        double TotalWeightWaterKg,
        double NetBuoyancyKg,
        double CurrentForceN,
        double WaveForceN,
        double HorizontalForceN,
        double TensionKn,
        double AnchorHoldingKg,
        double AnchorReserve,
        double LineLengthM,
        double EstimatedOffsetM,
        int ElementCount,
        int SegmentCount,
        double SegmentLengthSumM,
        double MaxSegmentLengthM,
        double SegmentCurrentForceSumN,
        double SegmentWeightWaterSumKg,
        double MinLocalSpeedMS,
        double MaxLocalSpeedMS,
        double LineElementWeightWaterKg,
        string SelectedSource,
        bool SelectedUsesDiscreteLoads,
        int SelectedNodeCount,
        double SelectedHorizontalOffsetM,
        double SelectedAnchorDepthM,
        double SelectedVerticalResidualM,
        bool SelectedConverged,
        double SelectedXSumM,
        double SelectedZSumM,
        double SelectedXSquaredSumM2,
        double SelectedTensionSumKn,
        double SelectedAngleSumDeg,
        IReadOnlyList<NodeSample> SelectedSamples,
        bool IterativeConverged,
        string IterativeStopReason,
        int DiscreteElementCount,
        string DiagnosticsSeverity);

    private sealed record NodeSample(
        int Index,
        double XOffsetM,
        double ZDepthM,
        double TensionKn,
        double AngleFromVerticalDeg);
}
