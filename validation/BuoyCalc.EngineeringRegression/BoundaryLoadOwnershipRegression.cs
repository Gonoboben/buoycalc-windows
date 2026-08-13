using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class BoundaryLoadOwnershipRegression
{
    private const double G = 9.80665;
    private const double AbsoluteTolerance = 1e-9;
    private const double RelativeTolerance = 1e-10;

    private static readonly SeabedPreset RegressionSeabed = new(
        "boundary-load:sand",
        "Boundary-load regression sand",
        1.2,
        "Deterministic boundary-load ownership seabed.");

    private static readonly BuoyInput RegressionBuoy = new(
        "Boundary-load regression buoy",
        1.0,
        100.0,
        0.8,
        0.8);

    private static readonly AnchorInput RegressionAnchor = new(
        "Boundary-load regression anchor",
        "Concrete block",
        "Concrete",
        1000.0,
        0.4,
        1.0);

    private static readonly RopePreset HeavyLine = new(
        "boundary-load:heavy-line",
        "Boundary-load heavy line",
        "Polyester",
        20.0,
        100.0,
        0.1,
        1.2,
        "Deterministic heavy line.");

    private static readonly RopePreset BuoyantLine = new(
        "boundary-load:buoyant-line",
        "Boundary-load buoyant line",
        "Synthetic buoyant",
        20.0,
        100.0,
        -0.05,
        1.2,
        "Negative signed water weight is intentional.");

    private static readonly ConnectorPreset RegressionConnector = new(
        "boundary-load:connector",
        "Boundary-load connector",
        "Shackle",
        5.0,
        0.0007,
        60.0,
        0.01,
        1.0,
        "Deterministic connector.");

    public static void Validate()
    {
        ValidateZeroCurrentHeavyLine();
        ValidateUniformCurrentHeavyLineWithWaveExcluded();
        ValidateBuoyantLineSignedWeight();
        ValidateDiscretePointLoads();
        ValidateDepthVaryingCurrentProfile();
    }

    private static void ValidateZeroCurrentHeavyLine()
    {
        var state = BuildState(
            "zero-current-heavy-line",
            Environment(depthM: 50, currentSpeedMS: 0, waveHeightM: 0, wavePeriodS: 0),
            new[] { Line("Vertical line", HeavyLine, 50) });

        AssertNear(0, state.ReconstructedBuoySteadyDragN, "zero-current buoy steady drag");
        AssertNear(0, state.Result.CurrentForceN, "zero-current total steady drag");
        AssertNear(0, state.Result.WaveForceN, "zero-current wave force");
        AssertOwnershipClosure(state);
    }

    private static void ValidateUniformCurrentHeavyLineWithWaveExcluded()
    {
        var state = BuildState(
            "uniform-current-heavy-line",
            Environment(depthM: 50, currentSpeedMS: 0.5, waveHeightM: 1.0, wavePeriodS: 6.0),
            new[] { Line("Slack line", HeavyLine, 55) });

        if (state.ReconstructedBuoySteadyDragN <= 0 || state.Result.WaveForceN <= 0)
        {
            throw new InvalidOperationException(
                "uniform-current-heavy-line: expected positive steady buoy drag and a separate positive wave term.");
        }

        AssertNear(
            state.Result.CurrentForceN + state.Result.WaveForceN,
            state.Result.HorizontalForceN,
            "uniform-current horizontal aggregate");

        // Static Chapter-2 ownership ledger deliberately closes on CurrentForceN only.
        AssertNear(
            state.Result.CurrentForceN,
            state.TerminalExternalFxN,
            "uniform-current steady ledger excludes wave force");

        if (Math.Abs(state.TerminalExternalFxN - state.Result.HorizontalForceN) <= AbsoluteTolerance)
        {
            throw new InvalidOperationException(
                "uniform-current-heavy-line: static ownership ledger must not silently include WaveForceN.");
        }

        AssertOwnershipClosure(state);
    }

    private static void ValidateBuoyantLineSignedWeight()
    {
        var state = BuildState(
            "buoyant-line",
            Environment(depthM: 30, currentSpeedMS: 0.3, waveHeightM: 0, wavePeriodS: 0),
            new[] { Line("Buoyant line", BuoyantLine, 30) });

        if (state.Result.SegmentRows.Count == 0 || state.Result.SegmentRows.Any(x => x.WeightWaterKg >= 0))
        {
            throw new InvalidOperationException(
                "buoyant-line: every deterministic line segment must retain negative signed WeightWaterKg.");
        }

        if (state.SegmentWeightWaterKg >= 0)
        {
            throw new InvalidOperationException(
                $"buoyant-line: expected negative distributed water weight sum, got {state.SegmentWeightWaterKg:R} kg.");
        }

        AssertOwnershipClosure(state);
    }

    private static void ValidateDiscretePointLoads()
    {
        var state = BuildState(
            "discrete-payload",
            Environment(depthM: 50, currentSpeedMS: 0.5, waveHeightM: 0.5, wavePeriodS: 5.0),
            new AssemblyItemInput[]
            {
                Line("Upper line", HeavyLine, 30),
                Connector("Shackle", RegressionConnector),
                Payload("Payload", 40.0, 0.005, 0.05, 1.0),
                Line("Lower line", HeavyLine, 25)
            });

        if (state.Sequence.DiscreteElementCount != 2)
        {
            throw new InvalidOperationException(
                $"discrete-payload: expected two internal discrete source elements, got {state.Sequence.DiscreteElementCount}.");
        }

        if (Math.Abs(state.Sequence.DiscreteWeightWaterKg) <= AbsoluteTolerance ||
            state.Sequence.DiscreteCurrentForceN <= 0)
        {
            throw new InvalidOperationException(
                "discrete-payload: expected non-zero point-load water weight and positive point-load steady drag.");
        }

        var internalDiscreteRows = state.Sequence.Rows
            .Where(x => x.IsDiscrete && x.Kind != "Буй" && x.Kind != "Якорь")
            .ToList();

        AssertNear(
            internalDiscreteRows.Sum(x => x.WeightWaterKg),
            state.Sequence.DiscreteWeightWaterKg,
            "discrete-payload point weight ownership");
        AssertNear(
            internalDiscreteRows.Sum(x => x.CurrentForceN),
            state.Sequence.DiscreteCurrentForceN,
            "discrete-payload point drag ownership");

        AssertOwnershipClosure(state);
    }

    private static void ValidateDepthVaryingCurrentProfile()
    {
        var state = BuildState(
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

        var distinctRoundedSegmentForces = state.Result.SegmentRows
            .Select(x => Math.Round(x.CurrentForceN, 12))
            .Distinct()
            .Count();

        if (distinctRoundedSegmentForces <= 1)
        {
            throw new InvalidOperationException(
                "depth-varying-current-profile: expected varying distributed segment drag across depth.");
        }

        AssertOwnershipClosure(state);
    }

    private static BoundaryOwnershipState BuildState(
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
        var sequence = MooringSequencePositioner.Build(result);

        var segmentCurrentForceN = result.SegmentRows.Sum(x => x.CurrentForceN);
        var segmentWeightWaterKg = result.SegmentRows.Sum(x => x.WeightWaterKg);

        var reconstructedBuoySteadyDragN =
            result.CurrentForceN -
            segmentCurrentForceN -
            sequence.DiscreteCurrentForceN;

        var reconstructedBuoySignedWeightWaterKg =
            -result.NetBuoyancyKg -
            segmentWeightWaterKg -
            sequence.DiscreteWeightWaterKg;

        var terminalExternalFxN =
            reconstructedBuoySteadyDragN +
            segmentCurrentForceN +
            sequence.DiscreteCurrentForceN;

        var terminalExternalFzN =
            (reconstructedBuoySignedWeightWaterKg +
             segmentWeightWaterKg +
             sequence.DiscreteWeightWaterKg) * G;

        var buoyRows = result.ElementRows
            .Where(x => string.Equals(x.Kind, "Буй", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (buoyRows.Count != 1)
        {
            throw new InvalidOperationException(
                $"{name}: validation provenance expected exactly one existing buoy ElementRow, got {buoyRows.Count}.");
        }

        return new BoundaryOwnershipState(
            name,
            result,
            sequence,
            buoyRows[0],
            segmentCurrentForceN,
            segmentWeightWaterKg,
            reconstructedBuoySteadyDragN,
            reconstructedBuoySignedWeightWaterKg,
            terminalExternalFxN,
            terminalExternalFzN);
    }

    private static void AssertOwnershipClosure(BoundaryOwnershipState state)
    {
        AssertNear(
            state.BuoyRow.CurrentForceN,
            state.ReconstructedBuoySteadyDragN,
            $"{state.Name} reconstructed buoy steady drag");
        AssertNear(
            state.BuoyRow.WeightWaterKg,
            state.ReconstructedBuoySignedWeightWaterKg,
            $"{state.Name} reconstructed buoy signed water weight");

        AssertNear(
            state.Result.CurrentForceN,
            state.TerminalExternalFxN,
            $"{state.Name} terminal steady Fx ownership");
        AssertNear(
            -state.Result.NetBuoyancyKg * G,
            state.TerminalExternalFzN,
            $"{state.Name} terminal signed Fz ownership");

        var elementInternalCurrentForceN = state.Result.ElementRows
            .Where(x => x.Kind != "Буй" && x.Kind != "Якорь" && x.Kind != "Линия")
            .Sum(x => x.CurrentForceN);
        var elementInternalWeightWaterKg = state.Result.ElementRows
            .Where(x => x.Kind != "Буй" && x.Kind != "Якорь" && x.Kind != "Линия")
            .Sum(x => x.WeightWaterKg);

        AssertNear(
            elementInternalCurrentForceN,
            state.Sequence.DiscreteCurrentForceN,
            $"{state.Name} sequence point-drag ownership");
        AssertNear(
            elementInternalWeightWaterKg,
            state.Sequence.DiscreteWeightWaterKg,
            $"{state.Name} sequence point-weight ownership");
    }

    private static void AssertNear(double expected, double actual, string label)
    {
        if (!double.IsFinite(expected) || !double.IsFinite(actual))
        {
            throw new InvalidOperationException(
                $"boundary-load ownership {label}: expected/actual must be finite ({expected:R}, {actual:R}).");
        }

        var tolerance = Math.Max(
            AbsoluteTolerance,
            RelativeTolerance * Math.Max(Math.Abs(expected), Math.Abs(actual)));

        if (Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException(
                $"boundary-load ownership {label}: expected {expected:R}, got {actual:R}, tolerance {tolerance:R}.");
        }
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

    private sealed record BoundaryOwnershipState(
        string Name,
        CalculationResult Result,
        MooringSequencePositionResult Sequence,
        ElementCalculationRow BuoyRow,
        double SegmentCurrentForceN,
        double SegmentWeightWaterKg,
        double ReconstructedBuoySteadyDragN,
        double ReconstructedBuoySignedWeightWaterKg,
        double TerminalExternalFxN,
        double TerminalExternalFzN);
}
