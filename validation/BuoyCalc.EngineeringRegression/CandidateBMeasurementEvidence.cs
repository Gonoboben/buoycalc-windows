using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class CandidateBMeasurementEvidence
{
    private static readonly SeabedPreset RegressionSeabed = new(
        "candidate-b-evidence:sand",
        "Candidate-B evidence sand",
        1.2,
        "Deterministic Candidate-B measurement seabed.");

    private static readonly BuoyInput RegressionBuoy = new(
        "Candidate-B evidence buoy",
        1.0,
        100.0,
        0.8,
        0.8);

    private static readonly AnchorInput RegressionAnchor = new(
        "Candidate-B evidence anchor",
        "Concrete block",
        "Concrete",
        1000.0,
        0.4,
        1.0);

    private static readonly RopePreset RegressionLine = new(
        "candidate-b-evidence:line",
        "Candidate-B evidence line",
        "Polyester",
        20.0,
        100.0,
        0.1,
        1.2,
        "Deterministic Candidate-B measurement line.");

    private static readonly ConnectorPreset RegressionConnector = new(
        "candidate-b-evidence:connector",
        "Candidate-B evidence connector",
        "Shackle",
        5.0,
        0.0007,
        60.0,
        0.01,
        1.0,
        "Deterministic Candidate-B measurement connector.");

    public static void Print()
    {
        PrintScenario(
            "heavy-payload-uniform-current",
            Environment(depthM: 50, currentSpeedMS: 0.5, waveHeightM: 0.5, wavePeriodS: 5.0),
            new AssemblyItemInput[]
            {
                Line("Upper line", 30),
                Payload("Heavy payload", 40.0, 0.005, 0.05, 1.0),
                Line("Lower line", 25)
            });

        PrintScenario(
            "grouped-connector-payload-uniform-current",
            Environment(depthM: 50, currentSpeedMS: 0.5, waveHeightM: 0.5, wavePeriodS: 5.0),
            new AssemblyItemInput[]
            {
                Line("Upper line", 30),
                Connector("Connector", RegressionConnector),
                Payload("Heavy payload", 40.0, 0.005, 0.05, 1.0),
                Line("Lower line", 25)
            });

        PrintScenario(
            "buoyant-payload-uniform-current",
            Environment(depthM: 50, currentSpeedMS: 0.4, waveHeightM: 0, wavePeriodS: 0),
            new AssemblyItemInput[]
            {
                Line("Upper line", 30),
                Payload("Buoyant payload", 1.0, 0.01, 0.03, 1.0),
                Line("Lower line", 25)
            });

        PrintScenario(
            "payload-depth-varying-current",
            new EnvironmentInput(
                1025.0,
                50.0,
                0.2,
                0.5,
                5.0,
                RegressionSeabed,
                true,
                new[]
                {
                    new CurrentProfilePointInput(0, 0.7, 0, 0, 1025),
                    new CurrentProfilePointInput(20, 0.45, 0, 0, 1025),
                    new CurrentProfilePointInput(35, 0.25, 0, 0, 1025),
                    new CurrentProfilePointInput(50, 0.1, 0, 0, 1025)
                }),
            new AssemblyItemInput[]
            {
                Line("Upper line", 30),
                Payload("Profile payload", 40.0, 0.005, 0.05, 1.0),
                Line("Lower line", 25)
            });

        PrintScenario(
            "vertical-zero-current-internal-payload",
            Environment(depthM: 50, currentSpeedMS: 0, waveHeightM: 0, wavePeriodS: 0),
            new AssemblyItemInput[]
            {
                Line("Upper vertical line", 25),
                Payload("Vertical payload", 20.0, 0.002, 0.02, 1.0),
                Line("Lower vertical line", 25)
            });
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
        var data = CalculationSnapshotBuilder.Build(environment, result).TechnicalReportData;

        Console.Error.WriteLine("BEGIN_CANDIDATE_B_MEASUREMENT");
        Console.Error.WriteLine($"Scenario={name}");
        PrintResidual("Pre", data.SignedNodeEquilibrium);
        PrintResidual("Final", data.FinalIterationSignedNodeEquilibrium);
        Console.Error.WriteLine($"FallbackOffsetM={data.Shape.HorizontalOffsetM:R}");
        Console.Error.WriteLine($"PreCandidateOffsetM={data.DiscreteLoadShape.DiscreteHorizontalOffsetM:R}");
        Console.Error.WriteLine($"FinalShapeOffsetM={Format(data.IterativeSolver.FinalShape?.HorizontalOffsetM)}");
        Console.Error.WriteLine($"PreTopDiscreteTensionKn={data.DiscreteLoadTensions.TopDiscreteTensionKn:R}");
        Console.Error.WriteLine($"FinalTopDiscreteTensionKn={Format(data.IterativeSolver.FinalDiscreteLoadTensions?.TopDiscreteTensionKn)}");
        Console.Error.WriteLine($"IterationCount={data.IterativeSolver.IterationCount}");
        Console.Error.WriteLine($"Converged={data.IterativeSolver.Converged}");
        Console.Error.WriteLine($"StopReason={data.IterativeSolver.StopReason}");
        Console.Error.WriteLine($"FinalOffsetChangeM={data.IterativeSolver.FinalOffsetChangeM:R}");
        Console.Error.WriteLine($"FinalMaxNodeDeltaM={data.IterativeSolver.FinalMaxNodeDeltaM:R}");
        Console.Error.WriteLine($"FinalGeometryResidualM={data.IterativeSolver.FinalGeometryResidualM:R}");
        foreach (var iteration in data.IterativeSolver.Rows)
        {
            Console.Error.WriteLine(
                $"Iteration{iteration.IterationNumber}=" +
                $"InputX:{iteration.InputOffsetM:R};" +
                $"OutputX:{iteration.OutputOffsetM:R};" +
                $"DeltaX:{iteration.OffsetChangeM:R};" +
                $"ShapeLineForceN:{iteration.ShapeLineForceN:R};" +
                $"TopShapeTensionKn:{iteration.TopShapeTensionKn:R};" +
                $"TopDiscreteTensionKn:{iteration.TopDiscreteTensionKn:R};" +
                $"MaxNodeDeltaM:{iteration.MaxNodeDeltaM:R};" +
                $"GeometryResidualM:{iteration.GeometryResidualM:R};" +
                $"StopReason:{iteration.StopReason}");
        }
        Console.Error.WriteLine("END_CANDIDATE_B_MEASUREMENT");
    }

    private static void PrintResidual(
        string prefix,
        MooringSignedNodeEquilibriumResult? equilibrium)
    {
        Console.Error.WriteLine($"{prefix}ResultAvailable={equilibrium is not null}");
        if (equilibrium is null)
        {
            return;
        }

        Console.Error.WriteLine($"{prefix}NodeCount={equilibrium.NodeCount}");
        Console.Error.WriteLine($"{prefix}AvailableNodeCount={equilibrium.AvailableNodeCount}");
        Console.Error.WriteLine($"{prefix}MaxResidualN={Format(equilibrium.MaxResidualN)}");
        Console.Error.WriteLine($"{prefix}MaxRelativeResidual={Format(equilibrium.MaxRelativeResidual)}");

        var row = equilibrium.Rows.FirstOrDefault(x => x.IsAvailable)
            ?? equilibrium.Rows.FirstOrDefault();
        if (row is null)
        {
            return;
        }

        Console.Error.WriteLine($"{prefix}NodePositionM={row.PositionAlongLineM:R}");
        Console.Error.WriteLine($"{prefix}SourceElementCount={row.SourceElementCount}");
        Console.Error.WriteLine($"{prefix}NodeForceXN={row.NodeForceXN:R}");
        Console.Error.WriteLine($"{prefix}NodeForceZN={row.NodeForceZN:R}");
        Console.Error.WriteLine($"{prefix}UpperTensionN={Format(row.UpperTensionN)}");
        Console.Error.WriteLine($"{prefix}LowerTensionN={Format(row.LowerTensionN)}");
        Console.Error.WriteLine($"{prefix}ResidualXN={Format(row.ResidualXN)}");
        Console.Error.WriteLine($"{prefix}ResidualZN={Format(row.ResidualZN)}");
        Console.Error.WriteLine($"{prefix}ResidualN={Format(row.ResidualN)}");
        Console.Error.WriteLine($"{prefix}RelativeResidual={Format(row.RelativeResidual)}");
        Console.Error.WriteLine($"{prefix}Status={row.Status}");
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

    private static AssemblyItemInput Line(string title, double lengthM)
    {
        return new AssemblyItemInput(
            AssemblyItemKind.Line,
            title,
            true,
            RegressionLine,
            null,
            lengthM,
            1,
            0,
            0,
            0,
            0);
    }

    private static AssemblyItemInput Connector(string title, ConnectorPreset connector)
    {
        return new AssemblyItemInput(
            AssemblyItemKind.Connector,
            title,
            true,
            null,
            connector,
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
