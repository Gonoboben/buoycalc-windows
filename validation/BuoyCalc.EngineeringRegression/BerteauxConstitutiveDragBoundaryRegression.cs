using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class BerteauxConstitutiveDragBoundaryRegression
{
    private const double Rho = 1025.0;
    private const double CurrentSpeedMS = 1.2;
    private const double DiameterM = 0.01;
    private const double SegmentLengthM = 2.0;
    private const double NormalDragCoefficient = 1.5;
    private const double Gamma = 0.02;
    private const double Tolerance = 1e-10;

    public static void Validate()
    {
        ValidateSourceLimitingIdentities();
        ValidateExistingShapeNormalMagnitudeOverlap();
        ValidateBaseForceOrientationIndependence();
    }

    private static void ValidateSourceLimitingIdentities()
    {
        var r = ReferenceNormalIncidenceForceN();

        AssertNear(0.0, BerteauxNormalForceN(r, 0.0), "D(phi=0)");
        AssertNear(0.5 * r, BerteauxNormalForceN(r, 45.0), "D(phi=45)");
        AssertNear(r, BerteauxNormalForceN(r, 90.0), "D(phi=90)");

        var tangentialScale = Math.PI * Gamma * r;
        AssertNear(tangentialScale, BerteauxTangentialForceN(r, 0.0), "F(phi=0)");
        AssertNear(0.5 * tangentialScale, BerteauxTangentialForceN(r, 45.0), "F(phi=45)");
        AssertNear(0.0, BerteauxTangentialForceN(r, 90.0), "F(phi=90)");
    }

    private static void ValidateExistingShapeNormalMagnitudeOverlap()
    {
        var expectedR = ReferenceNormalIncidenceForceN();

        var parallel = BuildShapeForceAtCableAngleToCurrent(0.0);
        var diagonal = BuildShapeForceAtCableAngleToCurrent(45.0);
        var normal = BuildShapeForceAtCableAngleToCurrent(90.0);

        AssertNear(0.0, parallel.ShapeForceN, "existing shape normal force at phi=0");
        AssertNear(0.5 * expectedR, diagonal.ShapeForceN, "existing shape normal force at phi=45");
        AssertNear(expectedR, normal.ShapeForceN, "existing shape normal force at phi=90");

        AssertNear(CurrentSpeedMS, normal.NormalSpeedMS, "normal cable normal-speed magnitude");
        AssertNear(CurrentSpeedMS / Math.Sqrt(2.0), diagonal.NormalSpeedMS, "45-degree normal-speed magnitude");
        AssertNear(0.0, parallel.NormalSpeedMS, "parallel cable normal-speed magnitude");
    }

    private static void ValidateBaseForceOrientationIndependence()
    {
        var expectedR = ReferenceNormalIncidenceForceN();
        var parallel = BuildShapeForceAtCableAngleToCurrent(0.0);
        var diagonal = BuildShapeForceAtCableAngleToCurrent(45.0);
        var normal = BuildShapeForceAtCableAngleToCurrent(90.0);

        AssertNear(expectedR, parallel.OriginalForceN, "base force at phi=0");
        AssertNear(expectedR, diagonal.OriginalForceN, "base force at phi=45");
        AssertNear(expectedR, normal.OriginalForceN, "base force at phi=90");

        if (!(parallel.ShapeForceN < diagonal.ShapeForceN && diagonal.ShapeForceN < normal.ShapeForceN))
        {
            throw new InvalidOperationException(
                "Berteaux constitutive drag boundary: expected orientation-dependent normal shape force ordering 0 < 45 < 90 degrees.");
        }

        if (Math.Abs(parallel.OriginalForceN - normal.OriginalForceN) > Tolerance)
        {
            throw new InvalidOperationException(
                "Berteaux constitutive drag boundary: historical base CurrentForceN unexpectedly became orientation-dependent.");
        }
    }

    private static MooringShapeForceRow BuildShapeForceAtCableAngleToCurrent(double phiDeg)
    {
        var phiRad = phiDeg * Math.PI / 180.0;
        var dx = SegmentLengthM * Math.Cos(phiRad);
        var dz = SegmentLengthM * Math.Sin(phiRad);
        var angleFromVerticalDeg = 90.0 - phiDeg;
        var baseForce = ReferenceNormalIncidenceForceN();

        var segment = new SegmentCalculationRow(
            Number: 1,
            SourceElement: "Berteaux constitutive synthetic line",
            RopePresetName: "Synthetic",
            StartLengthM: 0.0,
            EndLengthM: SegmentLengthM,
            SegmentLengthM: SegmentLengthM,
            EstimatedDepthM: 1.0,
            EastCurrentMS: CurrentSpeedMS,
            NorthCurrentMS: 0.0,
            VerticalCurrentMS: 0.0,
            LocalSpeedMS: CurrentSpeedMS,
            WaterDensityKgM3: Rho,
            ProjectedAreaM2: DiameterM * SegmentLengthM,
            DragCoefficient: NormalDragCoefficient,
            CurrentForceN: baseForce,
            WeightWaterKg: 0.0);

        var result = new CalculationResult(
            Verdict: "Synthetic",
            MainRisk: string.Empty,
            BuoyancyKg: 0.0,
            TotalWeightWaterKg: 0.0,
            NetBuoyancyKg: 0.0,
            CurrentForceN: baseForce,
            WaveForceN: 0.0,
            HorizontalForceN: baseForce,
            TensionKn: 0.0,
            WeakLinkBreakingLoadKn: 0.0,
            WeakLinkName: string.Empty,
            WorkingLoadKn: 0.0,
            TensionReserve: 0.0,
            SafetyFactor: 1.0,
            AnchorWeightWaterKg: 0.0,
            AnchorBaseHoldingCoefficient: 0.0,
            AnchorTypeMultiplier: 0.0,
            SeabedHoldingMultiplier: 0.0,
            AnchorHoldingKg: 0.0,
            RequiredAnchorHoldingKg: 0.0,
            AnchorReserve: 0.0,
            LineLengthM: SegmentLengthM,
            EstimatedOffsetM: 0.0,
            ElementRows: Array.Empty<ElementCalculationRow>(),
            SegmentRows: new[] { segment },
            Checks: Array.Empty<string>());

        var projectionRow = new MooringShapeProjectionRow(
            Number: 1,
            SegmentNumber: 1,
            Label: $"phi={phiDeg:0.###}",
            SegmentLengthM: SegmentLengthM,
            DeltaXM: dx,
            DeltaZM: dz,
            ProjectedLengthM: SegmentLengthM,
            LengthResidualM: 0.0,
            AngleFromVerticalDeg: angleFromVerticalDeg,
            TensionKn: 0.0,
            Status: "OK");

        var projection = new MooringShapeProjectionResult(
            Rows: new[] { projectionRow },
            SumDeltaXM: dx,
            SumDeltaZM: dz,
            TotalSegmentLengthM: SegmentLengthM,
            TotalProjectedLengthM: SegmentLengthM,
            LengthResidualM: 0.0,
            EndpointHorizontalOffsetM: dx,
            EndpointVerticalSpanM: dz,
            EndpointResidualXM: 0.0,
            EndpointResidualZM: 0.0,
            MaxAngleFromVerticalDeg: angleFromVerticalDeg,
            AverageAngleFromVerticalDeg: angleFromVerticalDeg,
            GeometryClosed: true,
            MethodNote: "Synthetic Berteaux constitutive overlap case.");

        var shapeForces = MooringShapeForceAnalyzer.Build(result, projection);
        if (shapeForces.Rows.Count != 1)
        {
            throw new InvalidOperationException(
                $"Berteaux constitutive drag boundary: expected one shape-force row for phi={phiDeg:R}, got {shapeForces.Rows.Count}.");
        }

        return shapeForces.Rows[0];
    }

    private static double ReferenceNormalIncidenceForceN()
    {
        var areaM2 = DiameterM * SegmentLengthM;
        return 0.5 * Rho * NormalDragCoefficient * areaM2 * CurrentSpeedMS * CurrentSpeedMS;
    }

    private static double BerteauxNormalForceN(double r, double phiDeg)
    {
        var phiRad = phiDeg * Math.PI / 180.0;
        var sin = Math.Sin(phiRad);
        return r * sin * sin;
    }

    private static double BerteauxTangentialForceN(double r, double phiDeg)
    {
        var phiRad = phiDeg * Math.PI / 180.0;
        var cos = Math.Cos(phiRad);
        return Math.PI * Gamma * r * cos * cos;
    }

    private static void AssertNear(double expected, double actual, string label)
    {
        if (Math.Abs(expected - actual) > Tolerance)
        {
            throw new InvalidOperationException(
                $"Berteaux constitutive drag boundary {label}: expected {expected:R}, got {actual:R}.");
        }
    }
}
