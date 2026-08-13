using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SignedGeometryStudyEvidence
{
    private static readonly SeabedPreset RegressionSeabed = new(
        "signed-geometry:sand",
        "Signed geometry sand",
        1.2,
        "Deterministic signed-geometry study seabed.");

    private static readonly BuoyInput RegressionBuoy = new(
        "Signed geometry buoy",
        1.0,
        100.0,
        0.8,
        0.8);

    private static readonly AnchorInput RegressionAnchor = new(
        "Signed geometry anchor",
        "Concrete block",
        "Concrete",
        1000.0,
        0.4,
        1.0);

    private static readonly RopePreset HeavyLine = new(
        "signed-geometry:heavy-line",
        "Signed geometry heavy line",
        "Polyester",
        20.0,
        100.0,
        0.1,
        1.2,
        "Deterministic heavy line.");

    private static readonly RopePreset BuoyantLine = new(
        "signed-geometry:buoyant-line",
        "Signed geometry buoyant line",
        "Synthetic buoyant",
        20.0,
        100.0,
        -0.05,
        1.2,
        "Negative signed water weight is intentional.");

    private static readonly ConnectorPreset RegressionConnector = new(
        "signed-geometry:connector",
        "Signed geometry connector",
        "Shackle",
        5.0,
        0.0007,
        60.0,
        0.01,
        1.0,
        "Deterministic connector.");

    public static void Print()
    {
        PrintScenario(
            "vertical-zero-current",
            Environment(depthM: 50, currentSpeedMS: 0, waveHeightM: 0, wavePeriodS: 0),
            new[] { Line("Vertical line", HeavyLine, 50) });

        PrintScenario(
            "uniform-current-slack-line",
            Environment(depthM: 50, currentSpeedMS: 0.5, waveHeightM: 1.0, wavePeriodS: 6.0),
            new[] { Line("Slack line", HeavyLine, 55) });

        PrintScenario(
            "buoyant-line",
            Environment(depthM: 30, currentSpeedMS: 0.3, waveHeightM: 0, wavePeriodS: 0),
            new[] { Line("Buoyant line", BuoyantLine, 30) });

        PrintScenario(
            "discrete-payload",
            Environment(depthM: 50, currentSpeedMS: 0.5, waveHeightM: 0.5, wavePeriodS: 5.0),
            new AssemblyItemInput[]
            {
                Line("Upper line", HeavyLine, 30),
                Connector("Shackle", RegressionConnector),
                Payload("Payload", 40.0, 0.005, 0.05, 1.0),
                Line("Lower line", HeavyLine, 25)
            });

        PrintScenario(
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
            new[] { Line("Profile line", HeavyLine, 50) });
    }

    private static void PrintScenario(
        string name,
        EnvironmentInput environment,
        IReadOnlyList<AssemblyItemInput> assembly)
    {
        var result = BuoyCalculator.Calculate(
            environment,
            RegressionBuoy,
            assembly,
            RegressionAnchor,
            3.0);
        var snapshot = CalculationSnapshotBuilder.Build(environment, result);
        var data = snapshot.TechnicalReportData;
        var orientations = data.SignedOrientation.Rows.OrderBy(x => x.Number).ToList();
        var tensionByNumber = data.TensionRows.ToDictionary(x => x.Number);

        var x = 0.0;
        var z = 0.0;
        var negativeDzCount = 0;
        var positiveDzCount = 0;
        var zeroDzCount = 0;
        var indeterminateCount = 0;
        var upwardArcLengthM = 0.0;
        var downwardArcLengthM = 0.0;

        foreach (var orientation in orientations)
        {
            if (!tensionByNumber.TryGetValue(orientation.SegmentNumber, out var tension) ||
                !orientation.TangentX.HasValue ||
                !orientation.TangentZ.HasValue)
            {
                indeterminateCount++;
                continue;
            }

            var lengthM = Math.Max(0, tension.SegmentLengthM);
            var dx = lengthM * orientation.TangentX.Value;
            var dz = lengthM * orientation.TangentZ.Value;
            x += dx;
            z += dz;

            if (dz < -1e-12)
            {
                negativeDzCount++;
                upwardArcLengthM += lengthM;
            }
            else if (dz > 1e-12)
            {
                positiveDzCount++;
                downwardArcLengthM += lengthM;
            }
            else
            {
                zeroDzCount++;
            }
        }

        var selected = snapshot.SelectedShape;
        var minVerticalForceN = orientations.Count > 0 ? orientations.Min(x => x.VerticalForceN) : 0;
        var maxVerticalForceN = orientations.Count > 0 ? orientations.Max(x => x.VerticalForceN) : 0;
        var negativeVerticalCount = orientations.Count(x => x.VerticalForceN < -1e-12);
        var positiveVerticalCount = orientations.Count(x => x.VerticalForceN > 1e-12);

        Console.Error.WriteLine("BEGIN_SIGNED_GEOMETRY_STUDY");
        Console.Error.WriteLine($"Scenario={name}");
        Console.Error.WriteLine($"DepthM={environment.DepthM:R}");
        Console.Error.WriteLine($"LineLengthM={result.LineLengthM:R}");
        Console.Error.WriteLine($"SignedEndpointXM={x:R}");
        Console.Error.WriteLine($"SignedEndpointZM={z:R}");
        Console.Error.WriteLine($"SignedDepthResidualM={(z - environment.DepthM):R}");
        Console.Error.WriteLine($"SelectedSource={selected?.Source ?? "null"}");
        Console.Error.WriteLine($"SelectedEndpointXM={Format(selected?.Shape.AnchorPoint?.XOffsetM)}");
        Console.Error.WriteLine($"SelectedEndpointZM={Format(selected?.Shape.AnchorPoint?.ZDepthM)}");
        Console.Error.WriteLine($"NegativeDzSegmentCount={negativeDzCount}");
        Console.Error.WriteLine($"PositiveDzSegmentCount={positiveDzCount}");
        Console.Error.WriteLine($"ZeroDzSegmentCount={zeroDzCount}");
        Console.Error.WriteLine($"IndeterminateSegmentCount={indeterminateCount}");
        Console.Error.WriteLine($"UpwardArcLengthM={upwardArcLengthM:R}");
        Console.Error.WriteLine($"DownwardArcLengthM={downwardArcLengthM:R}");
        Console.Error.WriteLine($"NegativeVerticalForceRowCount={negativeVerticalCount}");
        Console.Error.WriteLine($"PositiveVerticalForceRowCount={positiveVerticalCount}");
        Console.Error.WriteLine($"MinVerticalForceN={minVerticalForceN:R}");
        Console.Error.WriteLine($"MaxVerticalForceN={maxVerticalForceN:R}");

        PrintRepresentative("Top", orientations.FirstOrDefault());
        PrintRepresentative("Middle", orientations.Count > 0 ? orientations[orientations.Count / 2] : null);
        PrintRepresentative("Bottom", orientations.LastOrDefault());

        Console.Error.WriteLine("END_SIGNED_GEOMETRY_STUDY");
    }

    private static void PrintRepresentative(string label, MooringSignedOrientationRow? row)
    {
        if (row is null)
        {
            Console.Error.WriteLine($"{label}=null");
            return;
        }

        Console.Error.WriteLine(
            $"{label}=" +
            $"Segment:{row.SegmentNumber};" +
            $"H:{row.HorizontalForceN:R};" +
            $"V:{row.VerticalForceN:R};" +
            $"T:{row.TensionN:R};" +
            $"Tx:{Format(row.TangentX)};" +
            $"Tz:{Format(row.TangentZ)};" +
            $"SignedAngle:{Format(row.SignedAngleFromVerticalDeg)};" +
            $"HistoricalUnsignedAngle:{row.HistoricalUnsignedAngleFromVerticalDeg:R};" +
            $"State:{row.OrientationState}");
    }

    private static string Format(double? value)
    {
        return value.HasValue ? value.Value.ToString("R") : "null";
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
}
