using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class ForceShapeConsistencyRegression
{
    private const double ExactResidualTolerance = 1e-10;
    private const double VerticalRelativeTolerance = 1e-8;
    private const double VerticalAngleToleranceDeg = 1e-6;

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

    internal static void Validate()
    {
        ValidateSyntheticRows();
        ValidateCanonicalScenarios();
    }

    private static void ValidateSyntheticRows()
    {
        var aligned = BuildSynthetic(dxM: 3, dzM: 4, horizontalForceN: 30, verticalForceN: 40);
        var alignedRow = RequireSingleAvailable(aligned, "synthetic aligned");
        AssertNear(alignedRow.ResidualN!.Value, 0, ExactResidualTolerance, "synthetic aligned residual");
        AssertNear(alignedRow.RelativeResidual!.Value, 0, ExactResidualTolerance, "synthetic aligned relative residual");
        AssertNear(alignedRow.AngleDifferenceDeg!.Value, 0, ExactResidualTolerance, "synthetic aligned angle");
        AssertReconstructionIdentity(alignedRow, "synthetic aligned identity");

        var mismatch = BuildSynthetic(dxM: 0, dzM: 5, horizontalForceN: 30, verticalForceN: 40);
        var mismatchRow = RequireSingleAvailable(mismatch, "synthetic mismatch");
        if (mismatchRow.ResidualN!.Value <= 0 || mismatchRow.AngleDifferenceDeg!.Value <= 0)
        {
            throw new InvalidOperationException("synthetic mismatch: deliberate direction mismatch was not detected.");
        }
        AssertReconstructionIdentity(mismatchRow, "synthetic mismatch identity");

        var mismatch10 = BuildSyntheticAtGeometryAngle(angleFromVerticalDeg: 10, tensionN: 50);
        var mismatch30 = BuildSyntheticAtGeometryAngle(angleFromVerticalDeg: 30, tensionN: 50);
        var row10 = RequireSingleAvailable(mismatch10, "synthetic 10-degree mismatch");
        var row30 = RequireSingleAvailable(mismatch30, "synthetic 30-degree mismatch");
        if (row30.ResidualN!.Value <= row10.ResidualN!.Value ||
            row30.AngleDifferenceDeg!.Value <= row10.AngleDifferenceDeg!.Value)
        {
            throw new InvalidOperationException("synthetic mismatch: residual must grow with angle mismatch at fixed tension.");
        }

        var zeroForce = BuildSynthetic(dxM: 0, dzM: 5, horizontalForceN: 0, verticalForceN: 0);
        var zeroForceRow = RequireSingleIndeterminate(zeroForce, "synthetic zero-force");
        AssertResidualsUnavailable(zeroForceRow, "synthetic zero-force");

        var zeroGeometry = BuildSynthetic(dxM: 0, dzM: 0, horizontalForceN: 30, verticalForceN: 40);
        var zeroGeometryRow = RequireSingleIndeterminate(zeroGeometry, "synthetic zero-geometry");
        AssertResidualsUnavailable(zeroGeometryRow, "synthetic zero-geometry");
    }

    private static void ValidateCanonicalScenarios()
    {
        ValidateCanonical(
            "vertical-zero-current",
            Environment(depthM: 50, currentSpeedMS: 0, waveHeightM: 0, wavePeriodS: 0),
            new[] { Line("Vertical line", HeavyLine, 50) },
            expectVerticalZeroCurrent: true);

        ValidateCanonical(
            "uniform-current-slack-line",
            Environment(depthM: 50, currentSpeedMS: 0.5, waveHeightM: 1.0, wavePeriodS: 6.0),
            new[] { Line("Slack line", HeavyLine, 55) });

        ValidateCanonical(
            "buoyant-line",
            Environment(depthM: 30, currentSpeedMS: 0.3, waveHeightM: 0, wavePeriodS: 0),
            new[] { Line("Buoyant line", BuoyantLine, 30) });

        ValidateCanonical(
            "discrete-payload",
            Environment(depthM: 50, currentSpeedMS: 0.5, waveHeightM: 0.5, wavePeriodS: 5.0),
            new AssemblyItemInput[]
            {
                Line("Upper line", HeavyLine, 30),
                Connector("Shackle", RegressionConnector),
                Payload("Payload", 40.0, 0.005, 0.05, 1.0),
                Line("Lower line", HeavyLine, 25)
            });

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
            new[] { Line("Profile line", HeavyLine, 50) });
    }

    private static void ValidateCanonical(
        string name,
        EnvironmentInput environment,
        IReadOnlyList<AssemblyItemInput> assemblyItems,
        bool expectVerticalZeroCurrent = false)
    {
        var result = BuoyCalculator.Calculate(
            environment,
            RegressionBuoy,
            assemblyItems,
            RegressionAnchor,
            3.0);
        var snapshot = CalculationSnapshotBuilder.Build(environment, result);
        var data = snapshot.TechnicalReportData;
        var consistency = data.ForceShapeConsistency;

        if (consistency.Rows.Count != data.ShapeProjection.Rows.Count)
        {
            throw new InvalidOperationException(
                $"{name}: force-shape row count {consistency.Rows.Count} does not match projection row count {data.ShapeProjection.Rows.Count}.");
        }

        if (consistency.Rows.Count == 0 || consistency.AvailableRowCount == 0)
        {
            throw new InvalidOperationException($"{name}: no available force-shape consistency rows were published.");
        }

        if (consistency.AvailableRowCount + consistency.IndeterminateRowCount != consistency.Rows.Count)
        {
            throw new InvalidOperationException($"{name}: available/indeterminate force-shape row accounting is inconsistent.");
        }

        foreach (var row in consistency.Rows.Where(x => x.IsAvailable))
        {
            AssertAvailableFinite(row, name);
            AssertReconstructionIdentity(row, $"{name}: segment {row.SegmentNumber} reconstruction identity");
        }

        AssertNullableFinite(consistency.MaxResidualN, $"{name}: max residual");
        AssertNullableFinite(consistency.MaxRelativeResidual, $"{name}: max relative residual");
        AssertNullableFinite(consistency.MaxAngleDifferenceDeg, $"{name}: max angle difference");

        if (expectVerticalZeroCurrent)
        {
            if (consistency.IndeterminateRowCount != 0)
            {
                throw new InvalidOperationException($"{name}: vertical zero-current case contains indeterminate rows.");
            }

            if (consistency.MaxRelativeResidual!.Value > VerticalRelativeTolerance)
            {
                throw new InvalidOperationException(
                    $"{name}: max relative residual {consistency.MaxRelativeResidual.Value:R} exceeds {VerticalRelativeTolerance:R}.");
            }

            if (consistency.MaxAngleDifferenceDeg!.Value > VerticalAngleToleranceDeg)
            {
                throw new InvalidOperationException(
                    $"{name}: max angle difference {consistency.MaxAngleDifferenceDeg.Value:R} deg exceeds {VerticalAngleToleranceDeg:R} deg.");
            }
        }
    }

    private static MooringForceShapeConsistencyResult BuildSyntheticAtGeometryAngle(
        double angleFromVerticalDeg,
        double tensionN)
    {
        const double lengthM = 5;
        var angleRad = angleFromVerticalDeg * Math.PI / 180.0;
        return BuildSynthetic(
            lengthM * Math.Sin(angleRad),
            lengthM * Math.Cos(angleRad),
            horizontalForceN: 0,
            verticalForceN: tensionN);
    }

    private static MooringForceShapeConsistencyResult BuildSynthetic(
        double dxM,
        double dzM,
        double horizontalForceN,
        double verticalForceN)
    {
        var geometryLengthM = Math.Sqrt(dxM * dxM + dzM * dzM);
        var projection = new MooringShapeProjectionResult(
            new[]
            {
                new MooringShapeProjectionRow(
                    1,
                    1,
                    "Synthetic segment",
                    geometryLengthM,
                    dxM,
                    dzM,
                    geometryLengthM,
                    0,
                    Math.Atan2(Math.Abs(dxM), Math.Abs(dzM)) * 180.0 / Math.PI,
                    0,
                    "OK")
            },
            dxM,
            dzM,
            geometryLengthM,
            geometryLengthM,
            0,
            dxM,
            dzM,
            0,
            0,
            0,
            0,
            true,
            "Synthetic projection fixture.");

        var tensionN = Math.Sqrt(horizontalForceN * horizontalForceN + verticalForceN * verticalForceN);
        var forceAngleDeg = tensionN > 0
            ? Math.Atan2(Math.Abs(horizontalForceN), Math.Abs(verticalForceN)) * 180.0 / Math.PI
            : 0;
        var shapeTensions = new MooringShapeTensionResult(
            new[]
            {
                new MooringShapeTensionRow(
                    1,
                    1,
                    "Synthetic segment",
                    0,
                    geometryLengthM,
                    0,
                    0,
                    0,
                    0,
                    tensionN / 1000.0,
                    0,
                    0,
                    forceAngleDeg,
                    forceAngleDeg,
                    horizontalForceN,
                    verticalForceN,
                    "OK")
            },
            0,
            tensionN / 1000.0,
            0,
            0,
            0,
            tensionN / 1000.0,
            0,
            true,
            "Synthetic tension fixture.");

        return MooringForceShapeConsistencyAnalyzer.Build(projection, shapeTensions);
    }

    private static MooringForceShapeConsistencyRow RequireSingleAvailable(
        MooringForceShapeConsistencyResult result,
        string label)
    {
        if (result.Rows.Count != 1 || result.AvailableRowCount != 1 || result.IndeterminateRowCount != 0)
        {
            throw new InvalidOperationException($"{label}: expected one available row.");
        }

        return result.Rows[0];
    }

    private static MooringForceShapeConsistencyRow RequireSingleIndeterminate(
        MooringForceShapeConsistencyResult result,
        string label)
    {
        if (result.Rows.Count != 1 || result.AvailableRowCount != 0 || result.IndeterminateRowCount != 1)
        {
            throw new InvalidOperationException($"{label}: expected one indeterminate row.");
        }

        return result.Rows[0];
    }

    private static void AssertAvailableFinite(MooringForceShapeConsistencyRow row, string label)
    {
        if (!row.IsAvailable)
        {
            throw new InvalidOperationException($"{label}: segment {row.SegmentNumber} was expected to be available.");
        }

        var values = new double?[]
        {
            row.GeometricAngleFromVerticalDeg,
            row.ForceAngleFromVerticalDeg,
            row.ForceHorizontalN,
            row.ForceVerticalN,
            row.TensionN,
            row.GeometricHorizontalForceN,
            row.GeometricVerticalForceN,
            row.ResidualHorizontalN,
            row.ResidualVerticalN,
            row.ResidualN,
            row.RelativeResidual,
            row.AngleDifferenceDeg
        };

        if (values.Any(x => !x.HasValue || !double.IsFinite(x.Value)))
        {
            throw new InvalidOperationException($"{label}: segment {row.SegmentNumber} contains unavailable/non-finite proxy values.");
        }
    }

    private static void AssertResidualsUnavailable(MooringForceShapeConsistencyRow row, string label)
    {
        if (row.IsAvailable ||
            row.ResidualHorizontalN.HasValue ||
            row.ResidualVerticalN.HasValue ||
            row.ResidualN.HasValue ||
            row.RelativeResidual.HasValue ||
            row.AngleDifferenceDeg.HasValue)
        {
            throw new InvalidOperationException($"{label}: indeterminate row must not publish artificial residual values.");
        }
    }

    private static void AssertReconstructionIdentity(MooringForceShapeConsistencyRow row, string label)
    {
        AssertAvailableFinite(row, label);
        var tensionN = row.TensionN!.Value;
        var angleRad = row.AngleDifferenceDeg!.Value * Math.PI / 180.0;
        var expectedResidualN = 2.0 * tensionN * Math.Sin(angleRad / 2.0);
        var tolerance = ExactResidualTolerance * Math.Max(1.0, tensionN);
        AssertNear(row.ResidualN!.Value, expectedResidualN, tolerance, label);
    }

    private static void AssertNullableFinite(double? value, string label)
    {
        if (!value.HasValue || !double.IsFinite(value.Value))
        {
            throw new InvalidOperationException($"{label}: expected finite value.");
        }
    }

    private static void AssertNear(double actual, double expected, double tolerance, string label)
    {
        if (!double.IsFinite(actual) || !double.IsFinite(expected) || Math.Abs(actual - expected) > tolerance)
        {
            throw new InvalidOperationException(
                $"{label}: expected {expected:R} ± {tolerance:R}, got {actual:R}.");
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
}
