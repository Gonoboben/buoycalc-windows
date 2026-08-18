using System.Globalization;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class VerticalLimitingForceStateRegression
{
    private const double G = 9.80665;
    private const double NumericTolerance = 1e-8;
    private const double ZeroForceToleranceN = 1e-10;

    public static void Validate()
    {
        ValidateHistoricalVerticalFixture();
        ValidateControlledForceFamilies();
    }

    private static void ValidateHistoricalVerticalFixture()
    {
        var seabed = new SeabedPreset(
            "reg:sand",
            "Regression sand",
            1.2,
            "Deterministic regression seabed preset.");
        var environment = new EnvironmentInput(1025.0, 50.0, 0.0, 0.0, 0.0, seabed);
        var buoy = new BuoyInput("Regression buoy", 1.0, 100.0, 0.8, 0.8);
        var rope = new RopePreset(
            "reg:heavy-line",
            "Regression heavy line",
            "Polyester",
            20.0,
            100.0,
            0.1,
            1.2,
            "Deterministic heavy-line regression preset.");
        var assembly = new[]
        {
            new AssemblyItemInput(
                AssemblyItemKind.Line,
                "Vertical line",
                true,
                rope,
                null,
                50.0,
                1,
                0.0,
                0.0,
                0.0,
                0.0)
        };
        var anchor = new AnchorInput(
            "Regression concrete anchor",
            "Concrete block",
            "Concrete",
            1000.0,
            0.4,
            1.0);

        var run = ApplicationCalculationRunner.Run(
            environment,
            buoy,
            assembly,
            anchor,
            3.0);

        var boundary = run.Snapshot.TechnicalReportData.SurfaceBoundaryInfo;
        if (boundary.Classification != MooringSurfaceBoundaryInfoClassification.VerticalGeometryBoundaryNonUnique)
        {
            throw new InvalidOperationException(
                $"Vertical limiting force-state historical fixture: expected current production classification VerticalGeometryBoundaryNonUnique, got {boundary.Classification}.");
        }

        if (boundary.Solved)
            throw new InvalidOperationException("Vertical limiting force-state historical fixture: current production analyzer unexpectedly marked the non-unique force-state branch as solved.");

        Near(run.Result.CurrentForceN, 0.0, NumericTolerance, "historical steady current force");
        Near(run.Result.LineLengthM, 50.0, NumericTolerance, "historical line length");

        var qRequiredN = MaximumCumulativeSignedWeightN(run.Result.SegmentRows);
        var totalSignedWeightN = run.Result.SegmentRows.Sum(x => x.WeightWaterKg * G);
        var expectedLineWeightN = 50.0 * 0.1 * G;
        var expectedQCapacityN = (1025.0 - 100.0) * G;

        Near(totalSignedWeightN, expectedLineWeightN, NumericTolerance, "historical signed line weight");
        Near(qRequiredN, expectedLineWeightN, NumericTolerance, "historical Q required");
        Near(Required(boundary.QCapacityN, "Q capacity"), expectedQCapacityN, NumericTolerance, "historical Q capacity");

        var currentAnalyzerMinimumQ = Required(
            boundary.MinimumQForDownwardVerticalGeometryN,
            "current analyzer minimum Q");
        if (currentAnalyzerMinimumQ < qRequiredN || currentAnalyzerMinimumQ - qRequiredN > 1e-6)
        {
            throw new InvalidOperationException(
                $"Vertical limiting force-state historical fixture: current analyzer minimum Q {currentAnalyzerMinimumQ:R} is not the expected signed-weight lower bound {qRequiredN:R} plus only numerical epsilon.");
        }

        var capacityState = boundary.CapacityBoundaryState
            ?? throw new InvalidOperationException("Vertical limiting force-state historical fixture: missing capacity boundary state.");
        Near(capacityState.EndpointXM, 0.0, NumericTolerance, "capacity-state X");
        Near(capacityState.EndpointZM, 50.0, NumericTolerance, "capacity-state Z");
        Near(capacityState.EndHN, 0.0, NumericTolerance, "capacity-state end H");
        Near(capacityState.EndVN, expectedQCapacityN - expectedLineWeightN, 1e-7, "capacity-state end V");

        var limiting = IntegrateVertical(run.Result.SegmentRows, qRequiredN);
        Near(limiting.EndpointXM, 0.0, NumericTolerance, "limiting X");
        Near(limiting.EndpointZM, 50.0, NumericTolerance, "limiting Z");
        Near(limiting.EndVN, 0.0, 1e-7, "limiting endpoint V");
        if (limiting.IndeterminateSegmentCount != 0)
            throw new InvalidOperationException("Vertical limiting force-state historical fixture: endpoint-zero limit must not manufacture an interior indeterminate segment.");
        if (limiting.NegativeDzCount != 0)
            throw new InvalidOperationException("Vertical limiting force-state historical fixture: endpoint-zero limit contains an unexpected upward segment.");

        var qStrictN = (qRequiredN + expectedQCapacityN) / 2.0;
        var strict = IntegrateVertical(run.Result.SegmentRows, qStrictN);
        Near(strict.EndpointXM, 0.0, NumericTolerance, "strict-family X");
        Near(strict.EndpointZM, 50.0, NumericTolerance, "strict-family Z");
        if (!(strict.EndVN > 0.0))
            throw new InvalidOperationException("Vertical limiting force-state historical fixture: strict-family endpoint tension must be positive.");
        if (strict.IndeterminateSegmentCount != 0 || strict.NegativeDzCount != 0)
            throw new InvalidOperationException("Vertical limiting force-state historical fixture: strict-family state must remain strictly downward and determinate.");

        Near(strict.EndpointXM, limiting.EndpointXM, NumericTolerance, "family geometry X identity");
        Near(strict.EndpointZM, limiting.EndpointZM, NumericTolerance, "family geometry Z identity");
        if (Math.Abs(strict.EndVN - limiting.EndVN) <= 1.0)
            throw new InvalidOperationException("Vertical limiting force-state historical fixture: distinct admissible Q0 values unexpectedly produced indistinguishable force states.");

        var below = IntegrateVertical(run.Result.SegmentRows, qRequiredN - 1.0);
        if (below.NegativeDzCount <= 0)
            throw new InvalidOperationException("Vertical limiting force-state historical fixture: Q0 below Q required did not produce the expected negative-V/upward tangent region.");
        if (!(below.EndpointZM < 50.0))
            throw new InvalidOperationException("Vertical limiting force-state historical fixture: Q0 below Q required unexpectedly preserved full downward depth closure.");

        Console.WriteLine(string.Join("|",
            "VERTICAL_LIMITING_FORCE_STATE",
            "vertical-zero-current",
            $"QRequiredN={Format(qRequiredN)}",
            $"QCapacityN={Format(expectedQCapacityN)}",
            $"CurrentAnalyzerMinimumQN={Format(currentAnalyzerMinimumQ)}",
            $"LimitingQ0N={Format(qRequiredN)}",
            $"LimitingX={Format(limiting.EndpointXM)}",
            $"LimitingZ={Format(limiting.EndpointZM)}",
            $"LimitingEndVN={Format(limiting.EndVN)}",
            $"StrictQ0N={Format(qStrictN)}",
            $"StrictX={Format(strict.EndpointXM)}",
            $"StrictZ={Format(strict.EndpointZM)}",
            $"StrictEndVN={Format(strict.EndVN)}",
            $"GeometryIdentity={NearValue(strict.EndpointZM, limiting.EndpointZM) && NearValue(strict.EndpointXM, limiting.EndpointXM)}",
            "ForceStateUnique=False",
            "EndpointZeroTensionLimit=True",
            "ProductionAnalyzerChanged=False"));
    }

    private static void ValidateControlledForceFamilies()
    {
        var capacityInsufficient = ClassifyForceFamily(
            new[] { 10.0, 5.0 },
            14.0);
        Expect(
            capacityInsufficient,
            ControlledForceClassification.CapacityInsufficient,
            "capacity below Q required");

        var endpointLimit = ClassifyForceFamily(
            new[] { 10.0, 5.0 },
            15.0);
        Expect(
            endpointLimit,
            ControlledForceClassification.EndpointZeroTensionLimit,
            "capacity equals endpoint cumulative maximum");

        var interiorLimit = ClassifyForceFamily(
            new[] { 10.0, 5.0, -8.0 },
            15.0);
        Expect(
            interiorLimit,
            ControlledForceClassification.InteriorZeroTensionIndeterminate,
            "capacity equals interior cumulative maximum");

        var family = ClassifyForceFamily(
            new[] { 10.0, 5.0 },
            25.0);
        Expect(
            family,
            ControlledForceClassification.ForceStateFamilyAvailable,
            "capacity above Q required");

        var signed = RequiredQFromSignedEvents(new[] { 10.0, -20.0, 5.0 });
        Near(signed.QRequiredN, 10.0, NumericTolerance, "signed cumulative Q required");
        Near(signed.TotalAbsoluteLoadN, 35.0, NumericTolerance, "signed cumulative absolute control");
        if (!(signed.QRequiredN < signed.TotalAbsoluteLoadN))
            throw new InvalidOperationException("Vertical limiting force-state controlled signed case: Q required incorrectly behaves like total absolute weight.");

        var zeroMidpoint = IntegrateControlledVerticalSegments(
            new[] { new ControlledSegment(1.0, 2.0) },
            1.0);
        if (zeroMidpoint.IndeterminateSegmentCount != 1)
            throw new InvalidOperationException("Vertical limiting force-state controlled zero-resultant segment: expected one indeterminate segment.");
        Near(zeroMidpoint.EndpointZM, 0.0, NumericTolerance, "zero-resultant segment must not fabricate dz");

        Console.WriteLine(string.Join("|",
            "VERTICAL_LIMITING_FORCE_STATE_CONTROLLED",
            $"CapacityInsufficient={capacityInsufficient}",
            $"EndpointEquality={endpointLimit}",
            $"InteriorEquality={interiorLimit}",
            $"FamilyAvailable={family}",
            $"SignedQRequiredN={Format(signed.QRequiredN)}",
            $"SignedAbsoluteLoadN={Format(signed.TotalAbsoluteLoadN)}",
            $"ZeroResultantIndeterminateSegments={zeroMidpoint.IndeterminateSegmentCount}",
            $"ZeroResultantZ={Format(zeroMidpoint.EndpointZM)}"));
    }

    private static double MaximumCumulativeSignedWeightN(IReadOnlyList<SegmentCalculationRow> segments)
    {
        var cumulativeN = 0.0;
        var maximumN = 0.0;
        foreach (var segment in segments.OrderBy(x => x.Number))
        {
            cumulativeN += segment.WeightWaterKg * G;
            maximumN = Math.Max(maximumN, cumulativeN);
        }
        return maximumN;
    }

    private static VerticalIntegrationResult IntegrateVertical(
        IReadOnlyList<SegmentCalculationRow> segments,
        double q0N)
    {
        var controlled = segments
            .OrderBy(x => x.Number)
            .Select(x => new ControlledSegment(x.SegmentLengthM, x.WeightWaterKg * G))
            .ToArray();
        return IntegrateControlledVerticalSegments(controlled, q0N);
    }

    private static VerticalIntegrationResult IntegrateControlledVerticalSegments(
        IReadOnlyList<ControlledSegment> segments,
        double q0N)
    {
        var vN = q0N;
        var xM = 0.0;
        var zM = 0.0;
        var indeterminate = 0;
        var negativeDz = 0;

        foreach (var segment in segments)
        {
            var midVN = vN - 0.5 * segment.SignedWeightForceN;
            if (Math.Abs(midVN) <= ZeroForceToleranceN)
            {
                indeterminate++;
            }
            else
            {
                var tangentZ = Math.Sign(midVN);
                zM += segment.LengthM * tangentZ;
                if (tangentZ < 0)
                    negativeDz++;
            }

            vN -= segment.SignedWeightForceN;
        }

        return new VerticalIntegrationResult(xM, zM, vN, indeterminate, negativeDz);
    }

    private static ControlledForceClassification ClassifyForceFamily(
        IReadOnlyList<double> signedLoadEventsN,
        double qCapacityN)
    {
        var signed = RequiredQFromSignedEvents(signedLoadEventsN);
        if (qCapacityN < signed.QRequiredN - ZeroForceToleranceN)
            return ControlledForceClassification.CapacityInsufficient;

        if (Math.Abs(qCapacityN - signed.QRequiredN) <= ZeroForceToleranceN)
        {
            return signed.LastMaximumEventIndex == signedLoadEventsN.Count - 1 &&
                   signed.MaximumOccurrenceCount == 1
                ? ControlledForceClassification.EndpointZeroTensionLimit
                : ControlledForceClassification.InteriorZeroTensionIndeterminate;
        }

        return ControlledForceClassification.ForceStateFamilyAvailable;
    }

    private static SignedLoadSummary RequiredQFromSignedEvents(IReadOnlyList<double> signedLoadEventsN)
    {
        var cumulativeN = 0.0;
        var maximumN = 0.0;
        var lastMaximumIndex = -1;
        var maximumOccurrenceCount = 0;
        var totalAbsoluteN = 0.0;

        for (var i = 0; i < signedLoadEventsN.Count; i++)
        {
            var loadN = signedLoadEventsN[i];
            cumulativeN += loadN;
            totalAbsoluteN += Math.Abs(loadN);

            if (cumulativeN > maximumN + ZeroForceToleranceN)
            {
                maximumN = cumulativeN;
                lastMaximumIndex = i;
                maximumOccurrenceCount = 1;
            }
            else if (Math.Abs(cumulativeN - maximumN) <= ZeroForceToleranceN && maximumN > ZeroForceToleranceN)
            {
                lastMaximumIndex = i;
                maximumOccurrenceCount++;
            }
        }

        return new SignedLoadSummary(
            Math.Max(0.0, maximumN),
            lastMaximumIndex,
            maximumOccurrenceCount,
            totalAbsoluteN);
    }

    private static void Expect(
        ControlledForceClassification actual,
        ControlledForceClassification expected,
        string label)
    {
        if (actual != expected)
            throw new InvalidOperationException($"Vertical limiting force-state {label}: expected {expected}, got {actual}.");
    }

    private static double Required(double? value, string label) =>
        value ?? throw new InvalidOperationException($"Vertical limiting force-state historical fixture: missing {label}.");

    private static bool NearValue(double actual, double expected) =>
        Math.Abs(actual - expected) <= NumericTolerance;

    private static void Near(double actual, double expected, double tolerance, string label)
    {
        if (!double.IsFinite(actual) || Math.Abs(actual - expected) > tolerance)
        {
            throw new InvalidOperationException(
                $"Vertical limiting force-state {label}: expected {expected:R}, got {actual:R}, tolerance {tolerance:R}.");
        }
    }

    private static string Format(double value) =>
        value.ToString("R", CultureInfo.InvariantCulture);

    private sealed record ControlledSegment(double LengthM, double SignedWeightForceN);

    private sealed record VerticalIntegrationResult(
        double EndpointXM,
        double EndpointZM,
        double EndVN,
        int IndeterminateSegmentCount,
        int NegativeDzCount);

    private sealed record SignedLoadSummary(
        double QRequiredN,
        int LastMaximumEventIndex,
        int MaximumOccurrenceCount,
        double TotalAbsoluteLoadN);

    private enum ControlledForceClassification
    {
        CapacityInsufficient,
        EndpointZeroTensionLimit,
        InteriorZeroTensionIndeterminate,
        ForceStateFamilyAvailable
    }
}
