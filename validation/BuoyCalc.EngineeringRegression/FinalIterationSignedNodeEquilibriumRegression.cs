using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class FinalIterationSignedNodeEquilibriumRegression
{
    private const double IdentityTolerance = 1e-12;

    private static readonly SeabedPreset RegressionSeabed = new(
        "final-node:sand",
        "Final-node regression sand",
        1.2,
        "Deterministic final-node regression seabed.");

    private static readonly BuoyInput RegressionBuoy = new(
        "Final-node regression buoy",
        1.0,
        100.0,
        0.8,
        0.8);

    private static readonly AnchorInput RegressionAnchor = new(
        "Final-node regression anchor",
        "Concrete block",
        "Concrete",
        1000.0,
        0.4,
        1.0);

    private static readonly RopePreset RegressionLine = new(
        "final-node:line",
        "Final-node line",
        "Polyester",
        20.0,
        100.0,
        0.1,
        1.2,
        "Deterministic final-node line.");

    public static void Validate()
    {
        ValidateUnavailableWithoutFinalIteration();
        ValidateFinalStateWithoutInternalNode();
        ValidateFinalStateWithInternalNode();
    }

    private static void ValidateUnavailableWithoutFinalIteration()
    {
        var environment = Environment(depthM: 50, currentSpeedMS: 0.3);
        var result = BuoyCalculator.Calculate(
            environment,
            RegressionBuoy,
            Array.Empty<AssemblyItemInput>(),
            RegressionAnchor,
            3.0);
        var snapshot = CalculationSnapshotBuilder.Build(environment, result);
        var data = snapshot.TechnicalReportData;

        if (data.IterativeSolver.IterationCount != 0)
        {
            throw new InvalidOperationException("final-node unavailable case: iterative solver unexpectedly executed.");
        }

        if (data.FinalIterationSignedNodeEquilibrium is not null)
        {
            throw new InvalidOperationException("final-node unavailable case: final Candidate B must be null when no final iteration state exists.");
        }
    }

    private static void ValidateFinalStateWithoutInternalNode()
    {
        var environment = Environment(depthM: 50, currentSpeedMS: 0.5);
        var assembly = new[] { Line("Slack line", 55.0) };
        var data = CalculateData(environment, assembly);

        if (data.IterativeSolver.FinalDiscreteLoadTensions is null ||
            data.IterativeSolver.FinalDiscreteLoadShape is null)
        {
            throw new InvalidOperationException("final-node no-discrete case: retained final iteration state is missing.");
        }

        var finalResidual = data.FinalIterationSignedNodeEquilibrium
            ?? throw new InvalidOperationException("final-node no-discrete case: final residual result is missing.");

        if (finalResidual.NodeCount != 0 || finalResidual.Rows.Count != 0)
        {
            throw new InvalidOperationException("final-node no-discrete case: no internal discrete node should produce an empty final Candidate-B result.");
        }

        ValidateDirectIdentity("final-node no-discrete", data);
    }

    private static void ValidateFinalStateWithInternalNode()
    {
        var environment = Environment(depthM: 50, currentSpeedMS: 0.5);
        var assembly = new AssemblyItemInput[]
        {
            Line("Upper line", 30.0),
            Payload("Instrument", 40.0, 0.005, 0.05, 1.0),
            Line("Lower line", 25.0)
        };
        var data = CalculateData(environment, assembly);
        var finalResidual = data.FinalIterationSignedNodeEquilibrium
            ?? throw new InvalidOperationException("final-node discrete case: final residual result is missing.");

        if (finalResidual.NodeCount != 1 || finalResidual.Rows.Count != 1)
        {
            throw new InvalidOperationException(
                $"final-node discrete case: expected one grouped node, got nodes={finalResidual.NodeCount}, rows={finalResidual.Rows.Count}.");
        }

        var row = finalResidual.Rows[0];
        if (!row.IsAvailable)
        {
            throw new InvalidOperationException($"final-node discrete case: expected available internal-node residual; {row.Status}");
        }

        AssertFinite(row.NodeForceXN, "final-node Fx");
        AssertFinite(row.NodeForceZN, "final-node Fz");
        AssertFinite(Require(row.ResidualXN, "final-node Rx"), "final-node Rx");
        AssertFinite(Require(row.ResidualZN, "final-node Rz"), "final-node Rz");
        AssertFinite(Require(row.ResidualN, "final-node R"), "final-node R");
        AssertFinite(Require(row.RelativeResidual, "final-node R_rel"), "final-node R_rel");

        ValidateDirectIdentity("final-node discrete", data);
    }

    private static void ValidateDirectIdentity(string name, TechnicalReportData data)
    {
        var retainedTensions = data.IterativeSolver.FinalDiscreteLoadTensions
            ?? throw new InvalidOperationException($"{name}: retained final tensions are missing.");
        var retainedShape = data.IterativeSolver.FinalDiscreteLoadShape
            ?? throw new InvalidOperationException($"{name}: retained final shape is missing.");
        var published = data.FinalIterationSignedNodeEquilibrium
            ?? throw new InvalidOperationException($"{name}: published final Candidate B is missing.");
        var direct = MooringSignedNodeEquilibriumAnalyzer.Build(
            data.SequencePositions,
            retainedTensions,
            retainedShape);

        if (published.NodeCount != direct.NodeCount ||
            published.AvailableNodeCount != direct.AvailableNodeCount ||
            published.IndeterminateNodeCount != direct.IndeterminateNodeCount ||
            published.WorstNodeNumber != direct.WorstNodeNumber ||
            published.Rows.Count != direct.Rows.Count)
        {
            throw new InvalidOperationException($"{name}: published final Candidate-B summary differs from direct retained-state analyzer result.");
        }

        AssertNullableNear(published.MaxResidualN, direct.MaxResidualN, $"{name}: max R identity");
        AssertNullableNear(published.MaxRelativeResidual, direct.MaxRelativeResidual, $"{name}: max R_rel identity");

        for (var i = 0; i < published.Rows.Count; i++)
        {
            var a = published.Rows[i];
            var b = direct.Rows[i];
            if (a.Number != b.Number ||
                a.SourceElementCount != b.SourceElementCount ||
                a.SourceElements != b.SourceElements ||
                a.IsAvailable != b.IsAvailable ||
                a.Status != b.Status)
            {
                throw new InvalidOperationException($"{name}: row {i} metadata differs from direct retained-state result.");
            }

            AssertNear(a.PositionAlongLineM, b.PositionAlongLineM, $"{name}: row {i} s identity");
            AssertNear(a.NodeForceXN, b.NodeForceXN, $"{name}: row {i} Fx identity");
            AssertNear(a.NodeForceZN, b.NodeForceZN, $"{name}: row {i} Fz identity");
            AssertNullableNear(a.ResidualXN, b.ResidualXN, $"{name}: row {i} Rx identity");
            AssertNullableNear(a.ResidualZN, b.ResidualZN, $"{name}: row {i} Rz identity");
            AssertNullableNear(a.ResidualN, b.ResidualN, $"{name}: row {i} R identity");
            AssertNullableNear(a.RelativeResidual, b.RelativeResidual, $"{name}: row {i} R_rel identity");
        }
    }

    private static TechnicalReportData CalculateData(
        EnvironmentInput environment,
        IReadOnlyList<AssemblyItemInput> assembly)
    {
        var result = BuoyCalculator.Calculate(
            environment,
            RegressionBuoy,
            assembly,
            RegressionAnchor,
            3.0);
        return CalculationSnapshotBuilder.Build(environment, result).TechnicalReportData;
    }

    private static EnvironmentInput Environment(double depthM, double currentSpeedMS)
    {
        return new EnvironmentInput(
            1025.0,
            depthM,
            0.0,
            0.5,
            5.0,
            RegressionSeabed,
            true,
            new[]
            {
                new CurrentProfilePointInput(0.0, currentSpeedMS, 0.0, 0.0, 1025.0),
                new CurrentProfilePointInput(depthM, currentSpeedMS, 0.0, 0.0, 1025.0)
            });
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

    private static double Require(double? value, string name)
    {
        return value ?? throw new InvalidOperationException($"{name}: expected value, got null.");
    }

    private static void AssertNullableNear(double? expected, double? actual, string name)
    {
        if (!expected.HasValue && !actual.HasValue)
        {
            return;
        }

        if (!expected.HasValue || !actual.HasValue)
        {
            throw new InvalidOperationException($"{name}: nullable availability differs.");
        }

        AssertNear(expected.Value, actual.Value, name);
    }

    private static void AssertNear(double expected, double actual, string name)
    {
        if (!double.IsFinite(expected) ||
            !double.IsFinite(actual) ||
            Math.Abs(expected - actual) > IdentityTolerance)
        {
            throw new InvalidOperationException(
                $"{name}: expected {expected:R}, got {actual:R}, tolerance {IdentityTolerance:R}.");
        }
    }

    private static void AssertFinite(double value, string name)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidOperationException($"{name}: non-finite value {value}.");
        }
    }
}
