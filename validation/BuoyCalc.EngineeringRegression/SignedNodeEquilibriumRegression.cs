using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SignedNodeEquilibriumRegression
{
    private const double G = 9.80665;
    private const double SyntheticTolerance = 1e-10;

    private static readonly SeabedPreset RegressionSeabed = new(
        "signed-node:sand",
        "Signed-node regression sand",
        1.2,
        "Deterministic signed-node regression seabed.");

    private static readonly BuoyInput RegressionBuoy = new(
        "Signed-node regression buoy",
        1.0,
        100.0,
        0.8,
        0.8);

    private static readonly AnchorInput RegressionAnchor = new(
        "Signed-node regression anchor",
        "Concrete block",
        "Concrete",
        1000.0,
        0.4,
        1.0);

    private static readonly RopePreset HeavyLine = new(
        "signed-node:heavy-line",
        "Signed-node heavy line",
        "Polyester",
        20.0,
        100.0,
        0.1,
        1.2,
        "Deterministic heavy line.");

    private static readonly RopePreset BuoyantLine = new(
        "signed-node:buoyant-line",
        "Signed-node buoyant line",
        "Synthetic buoyant",
        20.0,
        100.0,
        -0.05,
        1.2,
        "Negative signed water weight is intentional.");

    private static readonly ConnectorPreset RegressionConnector = new(
        "signed-node:connector",
        "Signed-node connector",
        "Shackle",
        5.0,
        0.0007,
        60.0,
        0.01,
        1.0,
        "Deterministic connector.");

    public static void Validate()
    {
        ValidateExactBalancedNode();
        ValidateDirectionalMismatch();
        ValidateBuoyantPointLoad();
        ValidateSamePositionGrouping();
        ValidateBoundaryNode();
        ValidateDegenerateTangent();
        ValidateCanonicalScenarios();
    }

    private static void ValidateExactBalancedNode()
    {
        var nodeWeightKg = 10.0 / G;
        var result = BuildSynthetic(
            new[] { new SyntheticLoad(2, "Balanced load", 1.0, nodeWeightKg, 0.0) },
            inclusiveHorizontalN: 0.0,
            inclusiveVerticalN: 30.0,
            upperStart: new Point(0, 0),
            node: new Point(0, 1),
            lowerEnd: new Point(0, 2));

        var row = SingleAvailable(result, "exact-balanced");
        AssertNear(0, row.NodeForceXN, SyntheticTolerance, "exact-balanced node Fx");
        AssertNear(10, row.NodeForceZN, SyntheticTolerance, "exact-balanced node Fz");
        AssertNear(30, Require(row.UpperTensionN, "exact-balanced upper T"), SyntheticTolerance, "exact-balanced upper T");
        AssertNear(20, Require(row.LowerTensionN, "exact-balanced lower T"), SyntheticTolerance, "exact-balanced lower T");
        AssertNear(0, Require(row.ResidualXN, "exact-balanced Rx"), SyntheticTolerance, "exact-balanced Rx");
        AssertNear(0, Require(row.ResidualZN, "exact-balanced Rz"), SyntheticTolerance, "exact-balanced Rz");
        AssertNear(0, Require(row.ResidualN, "exact-balanced R"), SyntheticTolerance, "exact-balanced R");
        AssertNear(
            row.NodeForceXN,
            Require(row.InclusiveHorizontalForceN, "exact-balanced inclusive H") - Require(row.BelowHorizontalForceN, "exact-balanced below H"),
            SyntheticTolerance,
            "exact-balanced ownership H");
        AssertNear(
            row.NodeForceZN,
            Require(row.InclusiveVerticalForceN, "exact-balanced inclusive V") - Require(row.BelowVerticalForceN, "exact-balanced below V"),
            SyntheticTolerance,
            "exact-balanced ownership V");
    }

    private static void ValidateDirectionalMismatch()
    {
        var nodeWeightKg = 10.0 / G;
        var result = BuildSynthetic(
            new[] { new SyntheticLoad(2, "Mismatch load", 1.0, nodeWeightKg, 0.0) },
            inclusiveHorizontalN: 0.0,
            inclusiveVerticalN: 30.0,
            upperStart: new Point(0, 0),
            node: new Point(0, 1),
            lowerEnd: new Point(0.6, 1.8));

        var row = SingleAvailable(result, "directional-mismatch");
        var rx = Require(row.ResidualXN, "directional-mismatch Rx");
        var rz = Require(row.ResidualZN, "directional-mismatch Rz");
        var residual = Require(row.ResidualN, "directional-mismatch R");

        if (rx <= 0)
        {
            throw new InvalidOperationException($"directional-mismatch: expected positive signed Rx, got {rx:R}.");
        }

        if (rz >= 0)
        {
            throw new InvalidOperationException($"directional-mismatch: expected negative signed Rz, got {rz:R}.");
        }

        if (residual <= 0)
        {
            throw new InvalidOperationException("directional-mismatch: expected non-zero residual.");
        }
    }

    private static void ValidateBuoyantPointLoad()
    {
        var buoyantWeightKg = -10.0 / G;
        var result = BuildSynthetic(
            new[] { new SyntheticLoad(2, "Buoyant load", 1.0, buoyantWeightKg, 0.0) },
            inclusiveHorizontalN: 0.0,
            inclusiveVerticalN: 20.0,
            upperStart: new Point(0, 0),
            node: new Point(0, 1),
            lowerEnd: new Point(0, 2));

        var row = SingleAvailable(result, "buoyant-load");
        if (row.NodeForceZN >= 0)
        {
            throw new InvalidOperationException($"buoyant-load: signed node Fz must be negative, got {row.NodeForceZN:R}.");
        }

        var inclusiveV = Require(row.InclusiveVerticalForceN, "buoyant inclusive V");
        var belowV = Require(row.BelowVerticalForceN, "buoyant below V");
        if (belowV <= inclusiveV)
        {
            throw new InvalidOperationException("buoyant-load: subtracting a negative point load must increase below-cut V in this constructed case.");
        }

        AssertNear(0, Require(row.ResidualN, "buoyant R"), SyntheticTolerance, "buoyant balanced residual");
    }

    private static void ValidateSamePositionGrouping()
    {
        var loads = new[]
        {
            new SyntheticLoad(2, "Connector A", 1.0, 4.0 / G, 2.0),
            new SyntheticLoad(3, "Payload", 1.0, 6.0 / G, 3.0)
        };
        var result = BuildSynthetic(
            loads,
            inclusiveHorizontalN: 5.0,
            inclusiveVerticalN: 30.0,
            upperStart: new Point(0, 0),
            node: new Point(0, 1),
            lowerEnd: new Point(0, 2));

        if (result.NodeCount != 1 || result.Rows.Count != 1)
        {
            throw new InvalidOperationException($"same-position: expected one grouped mechanical node, got {result.NodeCount}.");
        }

        var row = SingleAvailable(result, "same-position");
        if (row.SourceElementCount != 2)
        {
            throw new InvalidOperationException($"same-position: expected two grouped source elements, got {row.SourceElementCount}.");
        }

        AssertNear(5, row.NodeForceXN, SyntheticTolerance, "same-position grouped Fx");
        AssertNear(10, row.NodeForceZN, SyntheticTolerance, "same-position grouped Fz");
        AssertNear(
            row.NodeForceXN,
            Require(row.InclusiveHorizontalForceN, "same-position inclusive H") - Require(row.BelowHorizontalForceN, "same-position below H"),
            SyntheticTolerance,
            "same-position ownership H");
        AssertNear(
            row.NodeForceZN,
            Require(row.InclusiveVerticalForceN, "same-position inclusive V") - Require(row.BelowVerticalForceN, "same-position below V"),
            SyntheticTolerance,
            "same-position ownership V");
    }

    private static void ValidateBoundaryNode()
    {
        var result = BuildSynthetic(
            new[] { new SyntheticLoad(1, "Top boundary load", 0.0, 1.0, 0.0) },
            inclusiveHorizontalN: 0.0,
            inclusiveVerticalN: 10.0,
            upperStart: new Point(0, 0),
            node: new Point(0, 1),
            lowerEnd: new Point(0, 2));

        if (result.Rows.Count != 1 || result.Rows[0].IsAvailable)
        {
            throw new InvalidOperationException("boundary-node: top boundary must be INDETERMINATE, not an available solved free body.");
        }

        if (result.Rows[0].ResidualN.HasValue)
        {
            throw new InvalidOperationException("boundary-node: unavailable residual must remain null, not artificial zero.");
        }
    }

    private static void ValidateDegenerateTangent()
    {
        var result = BuildSynthetic(
            new[] { new SyntheticLoad(2, "Degenerate load", 1.0, 1.0, 0.0) },
            inclusiveHorizontalN: 0.0,
            inclusiveVerticalN: 20.0,
            upperStart: new Point(0, 1),
            node: new Point(0, 1),
            lowerEnd: new Point(0, 2));

        if (result.Rows.Count != 1 || result.Rows[0].IsAvailable)
        {
            throw new InvalidOperationException("degenerate-tangent: zero-length adjacent tangent must be INDETERMINATE.");
        }

        if (result.Rows[0].ResidualN.HasValue)
        {
            throw new InvalidOperationException("degenerate-tangent: unavailable residual must remain null.");
        }
    }

    private static void ValidateCanonicalScenarios()
    {
        ValidateCanonical(
            "vertical-zero-current",
            Environment(depthM: 50, currentSpeedMS: 0, waveHeightM: 0, wavePeriodS: 0),
            new[] { Line("Vertical line", HeavyLine, 50) },
            expectedNodeCount: 0);

        ValidateCanonical(
            "uniform-current-slack-line",
            Environment(depthM: 50, currentSpeedMS: 0.5, waveHeightM: 1.0, wavePeriodS: 6.0),
            new[] { Line("Slack line", HeavyLine, 55) },
            expectedNodeCount: 0);

        ValidateCanonical(
            "buoyant-line",
            Environment(depthM: 30, currentSpeedMS: 0.3, waveHeightM: 0, wavePeriodS: 0),
            new[] { Line("Buoyant line", BuoyantLine, 30) },
            expectedNodeCount: 0);

        ValidateCanonical(
            "discrete-payload",
            Environment(depthM: 50, currentSpeedMS: 0.5, waveHeightM: 0.5, wavePeriodS: 5.0),
            new AssemblyItemInput[]
            {
                Line("Upper line", HeavyLine, 30),
                Connector("Shackle", RegressionConnector),
                Payload("Payload", 40.0, 0.005, 0.05, 1.0),
                Line("Lower line", HeavyLine, 25)
            },
            expectedNodeCount: 1,
            expectedGroupedSourceCount: 2);

        ValidateCanonical(
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
            expectedNodeCount: 0);
    }

    private static void ValidateCanonical(
        string name,
        EnvironmentInput environment,
        IReadOnlyList<AssemblyItemInput> assembly,
        int expectedNodeCount,
        int? expectedGroupedSourceCount = null)
    {
        var result = BuoyCalculator.Calculate(
            environment,
            RegressionBuoy,
            assembly,
            RegressionAnchor,
            3.0);
        var snapshot = CalculationSnapshotBuilder.Build(environment, result);
        var signed = snapshot.TechnicalReportData.SignedNodeEquilibrium;

        if (signed.NodeCount != expectedNodeCount)
        {
            throw new InvalidOperationException($"{name}: expected Candidate-B node count {expectedNodeCount}, got {signed.NodeCount}.");
        }

        if (expectedNodeCount == 0)
        {
            if (signed.Rows.Count != 0 || signed.AvailableNodeCount != 0 || signed.IndeterminateNodeCount != 0)
            {
                throw new InvalidOperationException($"{name}: no discrete internal nodes should produce an empty Candidate-B result.");
            }
            return;
        }

        var row = SingleAvailable(signed, name);
        if (expectedGroupedSourceCount.HasValue && row.SourceElementCount != expectedGroupedSourceCount.Value)
        {
            throw new InvalidOperationException($"{name}: expected {expectedGroupedSourceCount.Value} grouped sources, got {row.SourceElementCount}.");
        }

        AssertFinite(name, row.NodeForceXN);
        AssertFinite(name, row.NodeForceZN);
        AssertFinite(name, Require(row.UpperTensionN, $"{name} upper T"));
        AssertFinite(name, Require(row.LowerTensionN, $"{name} lower T"));
        AssertFinite(name, Require(row.ResidualXN, $"{name} Rx"));
        AssertFinite(name, Require(row.ResidualZN, $"{name} Rz"));
        AssertFinite(name, Require(row.ResidualN, $"{name} R"));
        AssertFinite(name, Require(row.RelativeResidual, $"{name} R_rel"));

        AssertNear(
            row.NodeForceXN,
            Require(row.InclusiveHorizontalForceN, $"{name} inclusive H") - Require(row.BelowHorizontalForceN, $"{name} below H"),
            SyntheticTolerance,
            $"{name} ownership H");
        AssertNear(
            row.NodeForceZN,
            Require(row.InclusiveVerticalForceN, $"{name} inclusive V") - Require(row.BelowVerticalForceN, $"{name} below V"),
            SyntheticTolerance,
            $"{name} ownership V");
    }

    private static MooringSignedNodeEquilibriumResult BuildSynthetic(
        IReadOnlyList<SyntheticLoad> loads,
        double inclusiveHorizontalN,
        double inclusiveVerticalN,
        Point upperStart,
        Point node,
        Point lowerEnd)
    {
        const double totalLineLengthM = 2.0;
        var primaryPosition = loads.Count > 0 ? loads[0].PositionAlongLineM : 1.0;
        var sequenceRows = new List<MooringSequencePositionRow>
        {
            new(1, "Линия", "Upper line", "Synthetic", 0, 1, 0.5, 1, 0, 0, true, false, "distributed", "synthetic")
        };
        sequenceRows.AddRange(loads.Select(x => new MooringSequencePositionRow(
            x.Number,
            "Прибор",
            x.Title,
            "Synthetic",
            x.PositionAlongLineM,
            x.PositionAlongLineM,
            x.PositionAlongLineM,
            0,
            x.WeightWaterKg,
            x.CurrentForceN,
            false,
            true,
            "discrete",
            "synthetic")));
        sequenceRows.Add(new MooringSequencePositionRow(
            100,
            "Линия",
            "Lower line",
            "Synthetic",
            1,
            2,
            1.5,
            1,
            0,
            0,
            true,
            false,
            "distributed",
            "synthetic"));

        var sequence = new MooringSequencePositionResult(
            sequenceRows,
            totalLineLengthM,
            2,
            loads.Count,
            loads.Sum(x => x.WeightWaterKg),
            loads.Sum(x => x.CurrentForceN),
            "Synthetic signed-node regression sequence.");

        var discreteEntries = loads.Select(x => new MooringDiscreteLoadEntry(
            x.Number,
            "Прибор",
            x.Title,
            x.PositionAlongLineM,
            x.WeightWaterKg,
            x.CurrentForceN)).ToList();
        var discreteWeightKg = loads.Sum(x => x.WeightWaterKg);
        var discreteForceN = loads.Sum(x => x.CurrentForceN);
        var tensionRows = new[]
        {
            TensionRow(
                number: 1,
                startM: 0,
                endM: 1,
                cumulativeHorizontalN: inclusiveHorizontalN,
                cumulativeVerticalN: inclusiveVerticalN),
            TensionRow(
                number: 2,
                startM: primaryPosition,
                endM: 2,
                cumulativeHorizontalN: inclusiveHorizontalN,
                cumulativeVerticalN: inclusiveVerticalN)
        };
        var discreteTensions = new MooringDiscreteLoadTensionResult(
            tensionRows,
            discreteEntries,
            discreteWeightKg,
            discreteForceN,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            true,
            "Synthetic signed-node regression tensions.");

        var shapeRows = new[]
        {
            ShapeRow(0, 1, 0, upperStart.X, upperStart.Z, 0),
            ShapeRow(1, 1, 1, node.X, node.Z, 1),
            ShapeRow(2, 2, 2, lowerEnd.X, lowerEnd.Z, 1)
        };
        var discreteShape = new MooringDiscreteLoadShapeResult(
            shapeRows,
            0,
            lowerEnd.X,
            lowerEnd.X,
            lowerEnd.Z,
            0,
            0,
            1,
            0,
            true,
            "Synthetic signed-node regression shape.");

        return MooringSignedNodeEquilibriumAnalyzer.Build(sequence, discreteTensions, discreteShape);
    }

    private static MooringDiscreteLoadTensionRow TensionRow(
        int number,
        double startM,
        double endM,
        double cumulativeHorizontalN,
        double cumulativeVerticalN)
    {
        var tensionN = Math.Sqrt(
            cumulativeHorizontalN * cumulativeHorizontalN +
            cumulativeVerticalN * cumulativeVerticalN);
        return new MooringDiscreteLoadTensionRow(
            number,
            number,
            $"Synthetic segment {number}",
            startM,
            endM,
            (startM + endM) / 2.0,
            Math.Max(0, endM - startM),
            0,
            0,
            0,
            0,
            cumulativeHorizontalN,
            cumulativeVerticalN,
            0,
            tensionN / 1000.0,
            0,
            0,
            0,
            0,
            "INFO");
    }

    private static MooringDiscreteLoadShapeRow ShapeRow(
        int number,
        int segmentNumber,
        double alongLineM,
        double x,
        double z,
        double segmentLengthM)
    {
        return new MooringDiscreteLoadShapeRow(
            number,
            segmentNumber,
            $"Synthetic segment {segmentNumber}",
            alongLineM,
            x,
            z,
            segmentLengthM,
            0,
            0,
            0,
            0,
            x,
            z,
            0,
            0,
            "INFO");
    }

    private static MooringSignedNodeEquilibriumRow SingleAvailable(
        MooringSignedNodeEquilibriumResult result,
        string name)
    {
        if (result.Rows.Count != 1 || !result.Rows[0].IsAvailable)
        {
            var status = result.Rows.Count == 1 ? result.Rows[0].Status : $"rows={result.Rows.Count}";
            throw new InvalidOperationException($"{name}: expected one available Candidate-B node; {status}.");
        }

        return result.Rows[0];
    }

    private static double Require(double? value, string name)
    {
        return value ?? throw new InvalidOperationException($"{name}: expected value, got null.");
    }

    private static void AssertNear(double expected, double actual, double tolerance, string name)
    {
        if (double.IsNaN(actual) || double.IsInfinity(actual) || Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException($"{name}: expected {expected:R}, got {actual:R}, tolerance {tolerance:R}.");
        }
    }

    private static void AssertFinite(string name, double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new InvalidOperationException($"{name}: non-finite Candidate-B value {value}.");
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

    private sealed record SyntheticLoad(
        int Number,
        string Title,
        double PositionAlongLineM,
        double WeightWaterKg,
        double CurrentForceN);

    private sealed record Point(double X, double Z);
}
