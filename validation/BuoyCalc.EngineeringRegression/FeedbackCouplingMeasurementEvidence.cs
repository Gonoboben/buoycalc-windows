using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class FeedbackCouplingMeasurementEvidence
{
    private static readonly SeabedPreset RegressionSeabed = new(
        "feedback-impact:sand",
        "Feedback impact sand",
        1.2,
        "Deterministic feedback-impact seabed.");

    private static readonly BuoyInput RegressionBuoy = new(
        "Feedback impact buoy",
        1.0,
        100.0,
        0.8,
        0.8);

    private static readonly AnchorInput RegressionAnchor = new(
        "Feedback impact anchor",
        "Concrete block",
        "Concrete",
        1000.0,
        0.4,
        1.0);

    private static readonly RopePreset HeavyLine = new(
        "feedback-impact:heavy-line",
        "Feedback impact heavy line",
        "Polyester",
        20.0,
        100.0,
        0.1,
        1.2,
        "Deterministic feedback-impact heavy line.");

    private static readonly RopePreset BuoyantLine = new(
        "feedback-impact:buoyant-line",
        "Feedback impact buoyant line",
        "Synthetic buoyant",
        20.0,
        100.0,
        -0.05,
        1.2,
        "Negative signed water weight is intentional.");

    private static readonly ConnectorPreset RegressionConnector = new(
        "feedback-impact:connector",
        "Feedback impact connector",
        "Shackle",
        5.0,
        0.0007,
        60.0,
        0.01,
        1.0,
        "Deterministic feedback-impact connector.");

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
                    new CurrentProfilePointInput(50, 0.1, 0, 1025)
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

        Console.Error.WriteLine("BEGIN_FEEDBACK_COUPLING_MEASUREMENT");
        Console.Error.WriteLine($"Scenario={name}");
        Console.Error.WriteLine($"FallbackOffsetM={data.Shape.HorizontalOffsetM:R}");
        Console.Error.WriteLine($"PreCandidateOffsetM={data.DiscreteLoadShape.DiscreteHorizontalOffsetM:R}");
        Console.Error.WriteLine($"FinalShapeOffsetM={Format(data.IterativeSolver.FinalShape?.HorizontalOffsetM)}");
        Console.Error.WriteLine($"SelectedOffsetM={snapshot.SelectedShape?.Shape.HorizontalOffsetM:R}");
        Console.Error.WriteLine($"IterationCount={data.IterativeSolver.IterationCount}");
        Console.Error.WriteLine($"Converged={data.IterativeSolver.Converged}");
        Console.Error.WriteLine($"StopReason={data.IterativeSolver.StopReason}");
        Console.Error.WriteLine($"FinalOffsetChangeM={data.IterativeSolver.FinalOffsetChangeM:R}");
        Console.Error.WriteLine($"FinalMaxNodeDeltaM={data.IterativeSolver.FinalMaxNodeDeltaM:R}");
        Console.Error.WriteLine($"FinalGeometryResidualM={data.IterativeSolver.FinalGeometryResidualM:R}");
        PrintResidual("PreCandidateB", data.SignedNodeEquilibrium);
        PrintResidual("FinalCandidateB", data.FinalIterationSignedNodeEquilibrium);

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

        Console.Error.WriteLine("END_FEEDBACK_COUPLING_MEASUREMENT");
    }

    private static void PrintResidual(
        string prefix,
        MooringSignedNodeEquilibriumResult? equilibrium)
    {
        Console.Error.WriteLine($"{prefix}Available={equilibrium is not null}");
        if (equilibrium is null)
        {
            return;
        }

        Console.Error.WriteLine($"{prefix}NodeCount={equilibrium.NodeCount}");
        Console.Error.WriteLine($"{prefix}MaxResidualN={Format(equilibrium.MaxResidualN)}");
        Console.Error.WriteLine($"{prefix}MaxRelativeResidual={Format(equilibrium.MaxRelativeResidual)}");

        var row = equilibrium.Rows.FirstOrDefault(x => x.IsAvailable);
        if (row is null)
        {
            return;
        }

        Console.Error.WriteLine($"{prefix}NodePositionM={row.PositionAlongLineM:R}");
        Console.Error.WriteLine($"{prefix}NodeForceXN={row.NodeForceXN:R}");
        Console.Error.WriteLine($"{prefix}NodeForceZN={row.NodeForceZN:R}");
        Console.Error.WriteLine($"{prefix}UpperTensionN={Format(row.UpperTensionN)}");
        Console.Error.WriteLine($"{prefix}LowerTensionN={Format(row.LowerTensionN)}");
        Console.Error.WriteLine($"{prefix}ResidualXN={Format(row.ResidualXN)}");
        Console.Error.WriteLine($"{prefix}ResidualZN={Format(row.ResidualZN)}");
        Console.Error.WriteLine($"{prefix}ResidualN={Format(row.ResidualN)}");
        Console.Error.WriteLine($"{prefix}RelativeResidual={Format(row.RelativeResidual)}");
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

    private static AssemblyItemInput Line(string title, RopePreset rope, double lengthM)
    {
        return new AssemblyItemInput(
            AssemblyItemKind.Line,
            title,
            true,
            rope,
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