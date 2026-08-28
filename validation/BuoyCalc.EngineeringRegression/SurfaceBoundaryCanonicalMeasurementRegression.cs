using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SurfaceBoundaryCanonicalMeasurementRegression
{
    private const double ResidualToleranceM = 0.011;

    internal static IReadOnlyList<CanonicalScenario> BuildCanonicalScenarios()
    {
        return new[]
        {
            BuildA(),
            BuildB(),
            BuildC(),
            BuildD(),
            BuildE()
        };
    }

    public static void Validate()
    {
        var scenarios = BuildCanonicalScenarios();

        var measured = new Dictionary<string, MooringSurfaceBoundaryInfoResult>(StringComparer.Ordinal);
        foreach (var scenario in scenarios)
        {
            var run = ApplicationCalculationRunner.Run(
                scenario.Environment,
                scenario.Buoy,
                scenario.Assembly,
                scenario.Anchor,
                scenario.SafetyFactor);
            var info = run.Snapshot.TechnicalReportData.SurfaceBoundaryInfo;
            RequireCanonicalState(scenario.Label, info);
            measured[scenario.Label] = info;
            Console.WriteLine(FormatMeasurement(scenario.Label, info));
        }

        ValidateWaveExclusion(measured["A"], BuildA());
    }

    private static void ValidateWaveExclusion(
        MooringSurfaceBoundaryInfoResult original,
        CanonicalScenario source)
    {
        var noWaveEnvironment = source.Environment with
        {
            WaveHeightM = 0.0,
            WavePeriodS = 0.0
        };
        var noWaveRun = ApplicationCalculationRunner.Run(
            noWaveEnvironment,
            source.Buoy,
            source.Assembly,
            source.Anchor,
            source.SafetyFactor);
        var noWave = noWaveRun.Snapshot.TechnicalReportData.SurfaceBoundaryInfo;

        if (original.Classification != noWave.Classification ||
            original.Available != noWave.Available ||
            original.Solved != noWave.Solved)
        {
            throw new InvalidOperationException(
                $"Surface-boundary canonical regression wave exclusion changed state: original={original.Classification}/{original.Available}/{original.Solved}, no-wave={noWave.Classification}/{noWave.Available}/{noWave.Solved}.");
        }

        NearNullable(original.BuoySteadyDragN, noWave.BuoySteadyDragN, 1e-10, "wave exclusion D_b");
        NearNullable(original.QCapacityN, noWave.QCapacityN, 1e-10, "wave exclusion Q_capacity");
        NearNullable(original.Q0N, noWave.Q0N, 1e-10, "wave exclusion Q0");
        NearNullable(original.LowerResidualM, noWave.LowerResidualM, 1e-10, "wave exclusion lower residual");
        NearNullable(original.CapacityResidualM, noWave.CapacityResidualM, 1e-10, "wave exclusion capacity residual");
        NearNullable(original.SolutionState?.EndpointXM, noWave.SolutionState?.EndpointXM, 1e-10, "wave exclusion endpoint X");
        NearNullable(original.SolutionState?.EndpointZM, noWave.SolutionState?.EndpointZM, 1e-10, "wave exclusion endpoint Z");
    }

    private static void RequireCanonicalState(string label, MooringSurfaceBoundaryInfoResult info)
    {
        if (!info.Available)
            throw new InvalidOperationException($"Surface-boundary canonical regression {label}: INFO result unexpectedly unavailable ({info.Classification}).");

        if (info.Classification is
            MooringSurfaceBoundaryInfoClassification.UnavailableMissingBuoyInput or
            MooringSurfaceBoundaryInfoClassification.UnavailableMissingBoundaryRows or
            MooringSurfaceBoundaryInfoClassification.InvalidInput)
        {
            throw new InvalidOperationException($"Surface-boundary canonical regression {label}: invalid/unavailable classification {info.Classification}.");
        }

        if (!double.IsFinite(info.BuoySteadyDragN ?? double.NaN) ||
            !double.IsFinite(info.QCapacityN ?? double.NaN) ||
            (info.QCapacityN ?? -1.0) < 0.0)
        {
            throw new InvalidOperationException($"Surface-boundary canonical regression {label}: non-finite boundary capacity/drag state.");
        }

        if (!info.Solved)
        {
            if (info.Q0N.HasValue || info.SolutionState is not null)
            {
                throw new InvalidOperationException($"Surface-boundary canonical regression {label}: unsolved state must not publish a solved Q0/geometry.");
            }
            return;
        }

        if (!info.Q0N.HasValue || !info.QCapacityN.HasValue || info.SolutionState is null)
            throw new InvalidOperationException($"Surface-boundary canonical regression {label}: solved state is missing Q0/capacity/geometry.");

        if (!double.IsFinite(info.Q0N.Value) ||
            info.Q0N.Value < -1e-9 ||
            info.Q0N.Value > info.QCapacityN.Value + 1e-9)
        {
            throw new InvalidOperationException(
                $"Surface-boundary canonical regression {label}: solved Q0={info.Q0N.Value:R} is outside [0,{info.QCapacityN.Value:R}].");
        }

        var targetDepth = info.TargetDepthM
            ?? throw new InvalidOperationException($"Surface-boundary canonical regression {label}: target depth missing.");
        var residual = info.SolutionState.EndpointZM - targetDepth;
        if (!double.IsFinite(residual) || Math.Abs(residual) > ResidualToleranceM)
        {
            throw new InvalidOperationException(
                $"Surface-boundary canonical regression {label}: solved depth residual {residual:R} m exceeds {ResidualToleranceM:R} m.");
        }
    }

    private static string FormatMeasurement(string label, MooringSurfaceBoundaryInfoResult info)
    {
        return string.Join("|",
            "SURFACE_BOUNDARY_CANONICAL",
            label,
            $"Class={info.Classification}",
            $"Available={info.Available}",
            $"Solved={info.Solved}",
            $"DbN={Format(info.BuoySteadyDragN)}",
            $"QcapN={Format(info.QCapacityN)}",
            $"Q0N={Format(info.Q0N)}",
            $"Qratio={Format(info.Q0CapacityRatio)}",
            $"BactualRatio={Format(info.ActualBuoyancyRatio)}",
            $"LowerR={Format(info.LowerResidualM)}",
            $"CapacityR={Format(info.CapacityResidualM)}",
            $"X={Format(info.SolutionState?.EndpointXM)}",
            $"Z={Format(info.SolutionState?.EndpointZM)}",
            $"Iterations={info.Iterations}",
            $"PointLoads={info.SolutionState?.PointLoadCrossings.ToString() ?? "n/a"}");
    }

    private static string Format(double? value) => value.HasValue ? value.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture) : "n/a";

    private static void NearNullable(double? expected, double? actual, double tolerance, string label)
    {
        if (expected.HasValue != actual.HasValue)
            throw new InvalidOperationException($"Surface-boundary canonical regression {label}: nullable state changed.");
        if (!expected.HasValue)
            return;
        if (Math.Abs(expected.Value - actual!.Value) > tolerance)
        {
            throw new InvalidOperationException(
                $"Surface-boundary canonical regression {label}: expected {expected.Value:R}, got {actual.Value:R}.");
        }
    }

    private static CanonicalScenario BuildA()
    {
        return new CanonicalScenario(
            "A",
            ConstantProfileEnvironment(50.0, 0.8, 1.0, 6.0, new SeabedPreset("a", "Sand", 1.3, string.Empty)),
            new BuoyInput("A buoy", 0.5, 80.0, 0.5, 0.8),
            StandardAssembly(55.0),
            new AnchorInput("A anchor", "Deadweight", "Concrete", 1000.0, 0.4, 1.0),
            5.0);
    }

    private static CanonicalScenario BuildB()
    {
        return new CanonicalScenario(
            "B",
            ConstantProfileEnvironment(120.0, 1.0, 2.0, 7.0, new SeabedPreset("b", "Mud", 1.8, string.Empty)),
            new BuoyInput("B buoy", 1.2, 150.0, 0.9, 0.8),
            StandardAssembly(135.0, includeSecondPayload: true),
            new AnchorInput("B anchor", "Drag embedment", "Steel", 1800.0, 0.25, 1.0),
            4.0);
    }

    private static CanonicalScenario BuildC()
    {
        return new CanonicalScenario(
            "C",
            ConstantProfileEnvironment(380.0, 0.6, 1.5, 8.0, new SeabedPreset("c", "Silt", 1.5, string.Empty)),
            new BuoyInput("C buoy", 2.2, 300.0, 1.3, 0.85),
            StandardAssembly(410.0, includeSecondPayload: true),
            new AnchorInput("C anchor", "Deadweight", "Concrete", 3000.0, 1.2, 1.1),
            5.0);
    }

    private static CanonicalScenario BuildD()
    {
        var environment = new EnvironmentInput(
            1025.0,
            380.0,
            0.0,
            1.5,
            8.0,
            new SeabedPreset("d", "Silt", 1.5, string.Empty),
            true,
            new[]
            {
                new CurrentProfilePointInput(0.0, 1.4, 0.0, 0.0, 1025.0),
                new CurrentProfilePointInput(50.0, 1.0, 0.0, 0.0, 1025.0),
                new CurrentProfilePointInput(150.0, 0.6, 0.0, 0.0, 1025.0),
                new CurrentProfilePointInput(300.0, 0.3, 0.0, 0.0, 1025.0),
                new CurrentProfilePointInput(380.0, 0.2, 0.0, 0.0, 1025.0)
            });

        return new CanonicalScenario(
            "D",
            environment,
            new BuoyInput("D buoy", 2.2, 300.0, 1.3, 0.85),
            StandardAssembly(410.0, includeSecondPayload: true),
            new AnchorInput("D anchor", "Deadweight", "Concrete", 3000.0, 1.2, 1.1),
            5.0);
    }

    private static CanonicalScenario BuildE()
    {
        var environment = new EnvironmentInput(
            1025.0,
            380.0,
            0.0,
            1.5,
            8.0,
            new SeabedPreset("e", "Silt", 1.5, string.Empty),
            true,
            new[]
            {
                new CurrentProfilePointInput(0.0, 0.85, 0.45, 0.00, 1025.0),
                new CurrentProfilePointInput(50.0, 0.65, 0.35, 0.05, 1024.8),
                new CurrentProfilePointInput(150.0, 0.40, 0.30, 0.08, 1024.5),
                new CurrentProfilePointInput(300.0, 0.18, 0.16, 0.04, 1025.3),
                new CurrentProfilePointInput(380.0, 0.10, 0.08, 0.02, 1026.0)
            });

        return new CanonicalScenario(
            "E",
            environment,
            new BuoyInput("E buoy", 2.2, 300.0, 1.3, 0.85),
            StandardAssembly(410.0, includeSecondPayload: true),
            new AnchorInput("E anchor", "Deadweight", "Concrete", 3000.0, 1.2, 1.1),
            5.0);
    }

    private static EnvironmentInput ConstantProfileEnvironment(
        double depthM,
        double currentSpeedMS,
        double waveHeightM,
        double wavePeriodS,
        SeabedPreset seabed)
    {
        return new EnvironmentInput(
            1025.0,
            depthM,
            0.0,
            waveHeightM,
            wavePeriodS,
            seabed,
            true,
            new[]
            {
                new CurrentProfilePointInput(0.0, currentSpeedMS, 0.0, 0.0, 1025.0),
                new CurrentProfilePointInput(depthM, currentSpeedMS, 0.0, 0.0, 1025.0)
            });
    }

    private static IReadOnlyList<AssemblyItemInput> StandardAssembly(double lineLengthM, bool includeSecondPayload = false)
    {
        var rope = new RopePreset(
            "canonical:rope",
            "Polyester rope",
            "Polyester",
            20.0,
            120.0,
            0.08,
            1.2,
            string.Empty);
        var connector = new ConnectorPreset(
            "canonical:connector",
            "Shackle",
            "Shackle",
            5.0,
            0.0007,
            100.0,
            0.002,
            1.1,
            string.Empty);

        var items = new List<AssemblyItemInput>
        {
            new(AssemblyItemKind.Connector, "Top shackle", true, null, connector, 0.0, 1, 0.0, 0.0, 0.0, 1.0),
            new(AssemblyItemKind.Line, "Main line", true, rope, null, lineLengthM, 1, 0.0, 0.0, 0.0, 1.0),
            new(AssemblyItemKind.Payload, "Instrument 1", true, null, null, 0.0, 1, 40.0, 0.012, 0.08, 1.0)
        };

        if (includeSecondPayload)
        {
            items.Add(new AssemblyItemInput(
                AssemblyItemKind.Payload,
                "Instrument 2",
                true,
                null,
                null,
                0.0,
                1,
                55.0,
                0.018,
                0.10,
                1.0));
        }

        items.Add(new AssemblyItemInput(
            AssemblyItemKind.Connector,
            "Bottom shackle",
            true,
            null,
            connector,
            0.0,
            1,
            0.0,
            0.0,
            0.0,
            1.0));
        return items;
    }

    internal sealed record CanonicalScenario(
        string Label,
        EnvironmentInput Environment,
        BuoyInput Buoy,
        IReadOnlyList<AssemblyItemInput> Assembly,
        AnchorInput Anchor,
        double SafetyFactor);
}
