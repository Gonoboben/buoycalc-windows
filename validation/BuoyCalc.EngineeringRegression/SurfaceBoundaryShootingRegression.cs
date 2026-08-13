using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SurfaceBoundaryShootingRegression
{
    private const double G = 9.80665;
    private const double DepthToleranceM = 0.01;
    private const double CoordinateToleranceM = 1e-9;
    private const double VectorEpsilonN = 1e-9;
    private const int MaxBisectionIterations = 80;

    private static readonly SeabedPreset RegressionSeabed = new(
        "surface-boundary:sand",
        "Surface-boundary regression sand",
        1.2,
        "Deterministic surface-boundary shooting seabed.");

    private static readonly BuoyInput RegressionBuoy = new(
        "Surface-boundary regression buoy",
        1.0,
        100.0,
        0.8,
        0.8);

    private static readonly BuoyInput LowCapacityBuoy = new(
        "Surface-boundary low-capacity buoy",
        0.10,
        100.0,
        0.8,
        0.8);

    private static readonly AnchorInput RegressionAnchor = new(
        "Surface-boundary regression anchor",
        "Concrete block",
        "Concrete",
        1000.0,
        0.4,
        1.0);

    private static readonly RopePreset HeavyLine = new(
        "surface-boundary:heavy-line",
        "Surface-boundary heavy line",
        "Polyester",
        20.0,
        100.0,
        0.1,
        1.2,
        "Deterministic heavy line.");

    private static readonly RopePreset BuoyantLine = new(
        "surface-boundary:buoyant-line",
        "Surface-boundary buoyant line",
        "Synthetic buoyant",
        20.0,
        100.0,
        -0.05,
        1.2,
        "Negative signed water weight is intentional.");

    private static readonly ConnectorPreset RegressionConnector = new(
        "surface-boundary:connector",
        "Surface-boundary connector",
        "Shackle",
        5.0,
        0.0007,
        60.0,
        0.01,
        1.0,
        "Deterministic connector.");

    public static void Validate()
    {
        var results = new[]
        {
            Solve(
                "A-zero-current-vertical-heavy",
                Environment(depthM: 50, currentSpeedMS: 0, waveHeightM: 0, wavePeriodS: 0),
                RegressionBuoy,
                new[] { Line("Vertical heavy line", HeavyLine, 50) }),

            Solve(
                "B-uniform-current-slack-heavy",
                Environment(depthM: 50, currentSpeedMS: 0.5, waveHeightM: 1.0, wavePeriodS: 6.0),
                RegressionBuoy,
                new[] { Line("Slack heavy line", HeavyLine, 55) }),

            Solve(
                "C-buoyant-slack-line",
                Environment(depthM: 30, currentSpeedMS: 0.3, waveHeightM: 0, wavePeriodS: 0),
                RegressionBuoy,
                new[] { Line("Slack buoyant line", BuoyantLine, 33) }),

            Solve(
                "D-discrete-payload",
                Environment(depthM: 50, currentSpeedMS: 0.5, waveHeightM: 0.5, wavePeriodS: 5.0),
                RegressionBuoy,
                new AssemblyItemInput[]
                {
                    Line("Upper line", HeavyLine, 30),
                    Connector("Shackle", RegressionConnector),
                    Payload("Payload", 40.0, 0.005, 0.05, 1.0),
                    Line("Lower line", HeavyLine, 25)
                }),

            Solve(
                "E-depth-varying-current-profile",
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
                RegressionBuoy,
                new[] { Line("Slack profile line", HeavyLine, 55) }),

            Solve(
                "F-line-shorter-than-depth",
                Environment(depthM: 50, currentSpeedMS: 0.2, waveHeightM: 0, wavePeriodS: 0),
                RegressionBuoy,
                new[] { Line("Short line", HeavyLine, 49) }),

            Solve(
                "G-taut-nonzero-current",
                Environment(depthM: 50, currentSpeedMS: 0.5, waveHeightM: 0, wavePeriodS: 0),
                RegressionBuoy,
                new[] { Line("Taut current line", HeavyLine, 50) }),

            Solve(
                "H-insufficient-buoyancy-capacity",
                Environment(depthM: 50, currentSpeedMS: 0.5, waveHeightM: 0, wavePeriodS: 0),
                LowCapacityBuoy,
                new[] { Line("Low-capacity slack line", HeavyLine, 55) })
        };

        AssertClassification(
            results[0],
            SurfaceBoundaryClassification.VerticalGeometryBoundaryNonUnique);
        AssertClassification(results[1], SurfaceBoundaryClassification.Solved);
        AssertClassification(results[2], SurfaceBoundaryClassification.Solved);
        AssertClassification(results[3], SurfaceBoundaryClassification.Solved);
        AssertClassification(results[4], SurfaceBoundaryClassification.Solved);
        AssertClassification(
            results[5],
            SurfaceBoundaryClassification.NoGeometricSolutionLineShorterThanDepth);
        AssertClassification(
            results[6],
            SurfaceBoundaryClassification.NoFiniteRootTautWithHorizontalLoad);
        AssertClassification(
            results[7],
            SurfaceBoundaryClassification.NoRootWithinBuoyancyCapacity);

        var buoyantSample = RequireSolvedSample(results[2]);
        if (buoyantSample.MaxVN <= buoyantSample.Q0N)
        {
            throw new InvalidOperationException(
                "surface-boundary C: negative signed line weight must increase the downward cable-tension component V when crossed top-to-bottom.");
        }

        var discreteSample = RequireSolvedSample(results[3]);
        if (discreteSample.PointCrossingCount != 1)
        {
            throw new InvalidOperationException(
                $"surface-boundary D: expected one grouped same-s point crossing, got {discreteSample.PointCrossingCount}.");
        }

        var tautUpper = results[6].UpperCapacitySample
            ?? throw new InvalidOperationException("surface-boundary G: expected an upper capacity sample.");
        if (Math.Abs(tautUpper.VerticalResidualM) > DepthToleranceM)
        {
            throw new InvalidOperationException(
                $"surface-boundary G: regression expects the finite-capacity trial to fall inside the numerical 0.01 m band so the analytical no-finite-root override remains protected; got {tautUpper.VerticalResidualM:R} m.");
        }

        foreach (var result in results)
        {
            ValidateResultInvariants(result);
            PrintEvidence(result);
        }
    }

    private static SurfaceBoundaryResult Solve(
        string name,
        EnvironmentInput environment,
        BuoyInput buoy,
        IReadOnlyList<AssemblyItemInput> assembly)
    {
        var calculation = BuoyCalculator.Calculate(
            environment,
            buoy,
            assembly,
            RegressionAnchor,
            3.0);
        var sequence = MooringSequencePositioner.Build(calculation);

        var depthM = Math.Max(0, environment.DepthM);
        var lineLengthM = Math.Max(0, calculation.LineLengthM);
        var segmentSteadyDragN = calculation.SegmentRows.Sum(x => x.CurrentForceN);
        var buoySteadyDragN =
            calculation.CurrentForceN -
            segmentSteadyDragN -
            sequence.DiscreteCurrentForceN;

        var maxBuoyancyN = Math.Max(0, calculation.BuoyancyKg) * G;
        var buoyWeightN = Math.Max(0, buoy.WeightKg) * G;
        var qCapacityN = Math.Max(0, maxBuoyancyN - buoyWeightN);
        var internalPoints = InternalPointRows(sequence);

        if (lineLengthM + DepthToleranceM < depthM)
        {
            return Classified(
                name,
                SurfaceBoundaryClassification.NoGeometricSolutionLineShorterThanDepth,
                calculation,
                qCapacityN,
                buoySteadyDragN,
                null,
                null,
                null,
                0,
                "L < D: an inextensible surface line cannot reach the prescribed depth.");
        }

        var lower = Integrate(
            calculation,
            internalPoints,
            buoySteadyDragN,
            q0N: 0,
            depthM);
        var upper = Integrate(
            calculation,
            internalPoints,
            buoySteadyDragN,
            qCapacityN,
            depthM);

        var totalHorizontalSteadyN = Math.Max(0, calculation.CurrentForceN);
        if (Math.Abs(lineLengthM - depthM) <= DepthToleranceM)
        {
            if (totalHorizontalSteadyN > VectorEpsilonN)
            {
                return Classified(
                    name,
                    SurfaceBoundaryClassification.NoFiniteRootTautWithHorizontalLoad,
                    calculation,
                    qCapacityN,
                    buoySteadyDragN,
                    null,
                    lower,
                    upper,
                    0,
                    "L == D with non-zero steady horizontal load: no finite inextensible non-vertical shape can have vertical span equal to arc length.");
            }

            if (upper.IsAvailable && Math.Abs(upper.VerticalResidualM) <= DepthToleranceM)
            {
                return Classified(
                    name,
                    SurfaceBoundaryClassification.VerticalGeometryBoundaryNonUnique,
                    calculation,
                    qCapacityN,
                    buoySteadyDragN,
                    null,
                    lower,
                    upper,
                    0,
                    "Zero-horizontal taut vertical geometry closes, but depth alone does not uniquely identify Q0.");
            }

            return Classified(
                name,
                SurfaceBoundaryClassification.NoRootWithinBuoyancyCapacity,
                calculation,
                qCapacityN,
                buoySteadyDragN,
                null,
                lower,
                upper,
                0,
                "Taut zero-horizontal geometry cannot be maintained within the available buoyancy capacity.");
        }

        if (!lower.IsAvailable || !upper.IsAvailable)
        {
            return Classified(
                name,
                SurfaceBoundaryClassification.IndeterminateDegenerateTension,
                calculation,
                qCapacityN,
                buoySteadyDragN,
                null,
                lower,
                upper,
                0,
                "A capacity-bound trial contains a degenerate zero-tension tangent; no artificial orientation is manufactured.");
        }

        if (Math.Abs(lower.VerticalResidualM) <= DepthToleranceM)
        {
            return Solved(
                name,
                calculation,
                qCapacityN,
                buoySteadyDragN,
                lower,
                lower,
                upper,
                0,
                "Depth closure occurs at the lower buoy-boundary limit Q0=0.");
        }

        if (Math.Abs(upper.VerticalResidualM) <= DepthToleranceM)
        {
            return Solved(
                name,
                calculation,
                qCapacityN,
                buoySteadyDragN,
                upper,
                lower,
                upper,
                0,
                "Depth closure occurs at full available buoyancy capacity.");
        }

        if (!HasSignChangingBracket(lower.VerticalResidualM, upper.VerticalResidualM))
        {
            return Classified(
                name,
                SurfaceBoundaryClassification.NoRootWithinBuoyancyCapacity,
                calculation,
                qCapacityN,
                buoySteadyDragN,
                null,
                lower,
                upper,
                0,
                "Depth residual has no sign-changing bracket inside [0, Q_capacity].");
        }

        var lowQ = 0.0;
        var highQ = qCapacityN;
        var lowSample = lower;

        for (var iteration = 1; iteration <= MaxBisectionIterations; iteration++)
        {
            var midQ = (lowQ + highQ) / 2.0;
            var mid = Integrate(
                calculation,
                internalPoints,
                buoySteadyDragN,
                midQ,
                depthM);

            if (!mid.IsAvailable)
            {
                return Classified(
                    name,
                    SurfaceBoundaryClassification.IndeterminateDegenerateTension,
                    calculation,
                    qCapacityN,
                    buoySteadyDragN,
                    midQ,
                    lower,
                    upper,
                    iteration,
                    "Bisection encountered a degenerate zero-tension tangent.");
            }

            if (Math.Abs(mid.VerticalResidualM) <= DepthToleranceM)
            {
                return Solved(
                    name,
                    calculation,
                    qCapacityN,
                    buoySteadyDragN,
                    mid,
                    lower,
                    upper,
                    iteration,
                    "Bounded bisection found a depth-closing surface-boundary reaction.");
            }

            if (HasSignChangingBracket(lowSample.VerticalResidualM, mid.VerticalResidualM))
            {
                highQ = midQ;
            }
            else
            {
                lowQ = midQ;
                lowSample = mid;
            }
        }

        var finalQ = (lowQ + highQ) / 2.0;
        var final = Integrate(
            calculation,
            internalPoints,
            buoySteadyDragN,
            finalQ,
            depthM);

        return Classified(
            name,
            SurfaceBoundaryClassification.NumericalIterationLimitReached,
            calculation,
            qCapacityN,
            buoySteadyDragN,
            finalQ,
            lower,
            upper,
            MaxBisectionIterations,
            $"Bisection did not reach the numerical depth target; final residual={final.VerticalResidualM:R} m.");
    }

    private static SurfaceBoundaryIntegrationSample Integrate(
        CalculationResult calculation,
        IReadOnlyList<MooringSequencePositionRow> internalPoints,
        double buoySteadyDragN,
        double q0N,
        double targetDepthM)
    {
        var orderedSegments = calculation.SegmentRows.OrderBy(x => x.Number).ToList();
        var pointGroups = internalPoints
            .GroupBy(x => x.PositionAlongLineM)
            .OrderBy(x => x.Key)
            .Select(group => new PointGroup(
                group.Key,
                group.Count(),
                group.Sum(x => x.CurrentForceN),
                group.Sum(x => x.WeightWaterKg) * G))
            .ToList();

        var pointIndex = 0;
        var pointCrossingCount = 0;
        var hN = buoySteadyDragN;
        var vN = q0N;
        var xM = 0.0;
        var zM = 0.0;
        var minH = hN;
        var maxH = hN;
        var minV = vN;
        var maxV = vN;
        var vChangedSign = false;

        foreach (var segment in orderedSegments)
        {
            while (pointIndex < pointGroups.Count &&
                   pointGroups[pointIndex].PositionAlongLineM <= segment.StartLengthM + CoordinateToleranceM)
            {
                var point = pointGroups[pointIndex++];
                var previousV = vN;
                hN += point.CurrentForceN;
                vN -= point.WeightWaterForceN;
                pointCrossingCount++;
                vChangedSign |= CrossedZero(previousV, vN);
                minH = Math.Min(minH, hN);
                maxH = Math.Max(maxH, hN);
                minV = Math.Min(minV, vN);
                maxV = Math.Max(maxV, vN);
            }

            // Berteaux step approximation: geometry from one node to the next
            // follows the resultant tension vector at the node. Here this is the
            // current top/start-cut state before crossing this segment's load.
            var tensionN = Math.Sqrt(hN * hN + vN * vN);
            if (!double.IsFinite(tensionN) || tensionN <= VectorEpsilonN)
            {
                return new SurfaceBoundaryIntegrationSample(
                    q0N,
                    false,
                    xM,
                    zM,
                    zM - targetDepthM,
                    minH,
                    maxH,
                    minV,
                    maxV,
                    vChangedSign,
                    pointCrossingCount,
                    "Indeterminate: zero/degenerate tension vector at a segment start cut.");
            }

            var tangentX = hN / tensionN;
            var tangentZ = vN / tensionN;
            var segmentLengthM = Math.Max(0, segment.SegmentLengthM);
            xM += segmentLengthM * tangentX;
            zM += segmentLengthM * tangentZ;

            var previousSegmentV = vN;
            hN += segment.CurrentForceN;
            vN -= segment.WeightWaterKg * G;
            vChangedSign |= CrossedZero(previousSegmentV, vN);
            minH = Math.Min(minH, hN);
            maxH = Math.Max(maxH, hN);
            minV = Math.Min(minV, vN);
            maxV = Math.Max(maxV, vN);
        }

        while (pointIndex < pointGroups.Count)
        {
            var point = pointGroups[pointIndex++];
            var previousV = vN;
            hN += point.CurrentForceN;
            vN -= point.WeightWaterForceN;
            pointCrossingCount++;
            vChangedSign |= CrossedZero(previousV, vN);
            minH = Math.Min(minH, hN);
            maxH = Math.Max(maxH, hN);
            minV = Math.Min(minV, vN);
            maxV = Math.Max(maxV, vN);
        }

        return new SurfaceBoundaryIntegrationSample(
            q0N,
            true,
            xM,
            zM,
            zM - targetDepthM,
            minH,
            maxH,
            minV,
            maxV,
            vChangedSign,
            pointCrossingCount,
            "Available frozen-load stepped geometry.");
    }

    private static SurfaceBoundaryResult Solved(
        string name,
        CalculationResult calculation,
        double qCapacityN,
        double buoySteadyDragN,
        SurfaceBoundaryIntegrationSample solved,
        SurfaceBoundaryIntegrationSample lower,
        SurfaceBoundaryIntegrationSample upper,
        int iterations,
        string note)
    {
        return new SurfaceBoundaryResult(
            name,
            SurfaceBoundaryClassification.Solved,
            calculation.LineLengthM,
            Math.Max(0, calculation.BuoyancyKg) * G,
            qCapacityN,
            buoySteadyDragN,
            solved.Q0N,
            solved,
            lower,
            upper,
            iterations,
            note);
    }

    private static SurfaceBoundaryResult Classified(
        string name,
        SurfaceBoundaryClassification classification,
        CalculationResult calculation,
        double qCapacityN,
        double buoySteadyDragN,
        double? q0N,
        SurfaceBoundaryIntegrationSample? lower,
        SurfaceBoundaryIntegrationSample? upper,
        int iterations,
        string note)
    {
        return new SurfaceBoundaryResult(
            name,
            classification,
            calculation.LineLengthM,
            Math.Max(0, calculation.BuoyancyKg) * G,
            qCapacityN,
            buoySteadyDragN,
            q0N,
            q0N.HasValue && lower is not null && Math.Abs(lower.Q0N - q0N.Value) <= VectorEpsilonN
                ? lower
                : q0N.HasValue && upper is not null && Math.Abs(upper.Q0N - q0N.Value) <= VectorEpsilonN
                    ? upper
                    : null,
            lower,
            upper,
            iterations,
            note);
    }

    private static IReadOnlyList<MooringSequencePositionRow> InternalPointRows(
        MooringSequencePositionResult sequence)
    {
        if (sequence.Rows.Count < 2)
        {
            return Array.Empty<MooringSequencePositionRow>();
        }

        var firstNumber = sequence.Rows.Min(x => x.Number);
        var lastNumber = sequence.Rows.Max(x => x.Number);
        return sequence.Rows
            .Where(x => x.IsDiscrete && x.Number != firstNumber && x.Number != lastNumber)
            .OrderBy(x => x.Number)
            .ToList();
    }

    private static bool HasSignChangingBracket(double a, double b)
    {
        return double.IsFinite(a) &&
               double.IsFinite(b) &&
               ((a < 0 && b > 0) || (a > 0 && b < 0));
    }

    private static bool CrossedZero(double before, double after)
    {
        return (before < 0 && after > 0) || (before > 0 && after < 0);
    }

    private static void AssertClassification(
        SurfaceBoundaryResult result,
        SurfaceBoundaryClassification expected)
    {
        if (result.Classification != expected)
        {
            throw new InvalidOperationException(
                $"surface-boundary {result.Name}: expected {expected}, got {result.Classification}. {result.Note}");
        }
    }

    private static SurfaceBoundaryIntegrationSample RequireSolvedSample(SurfaceBoundaryResult result)
    {
        if (result.Classification != SurfaceBoundaryClassification.Solved ||
            result.SolvedSample is null ||
            !result.SolvedSample.IsAvailable)
        {
            throw new InvalidOperationException(
                $"surface-boundary {result.Name}: expected an available solved sample.");
        }

        return result.SolvedSample;
    }

    private static void ValidateResultInvariants(SurfaceBoundaryResult result)
    {
        if (!double.IsFinite(result.MaxBuoyancyN) ||
            !double.IsFinite(result.QCapacityN) ||
            result.MaxBuoyancyN < 0 ||
            result.QCapacityN < 0 ||
            result.QCapacityN > result.MaxBuoyancyN + VectorEpsilonN)
        {
            throw new InvalidOperationException(
                $"surface-boundary {result.Name}: invalid B_max/Q_capacity pair ({result.MaxBuoyancyN:R}, {result.QCapacityN:R}) N.");
        }

        if (result.Q0N.HasValue &&
            (result.Q0N.Value < -VectorEpsilonN || result.Q0N.Value > result.QCapacityN + VectorEpsilonN))
        {
            throw new InvalidOperationException(
                $"surface-boundary {result.Name}: Q0={result.Q0N.Value:R} N lies outside [0, Q_capacity={result.QCapacityN:R} N].");
        }

        if (result.Classification == SurfaceBoundaryClassification.Solved)
        {
            var solved = RequireSolvedSample(result);
            if (Math.Abs(solved.VerticalResidualM) > DepthToleranceM)
            {
                throw new InvalidOperationException(
                    $"surface-boundary {result.Name}: solved residual {solved.VerticalResidualM:R} m exceeds numerical target {DepthToleranceM:R} m.");
            }

            var bActualN = result.MaxBuoyancyN - result.QCapacityN + solved.Q0N;
            if (bActualN < -VectorEpsilonN || bActualN > result.MaxBuoyancyN + VectorEpsilonN)
            {
                throw new InvalidOperationException(
                    $"surface-boundary {result.Name}: B_actual={bActualN:R} N lies outside [0, B_max={result.MaxBuoyancyN:R} N].");
            }
        }
    }

    private static void PrintEvidence(SurfaceBoundaryResult result)
    {
        var solved = result.SolvedSample;
        var q0Text = result.Q0N?.ToString("R") ?? "n/a";
        var qRatio = result.Q0N.HasValue && result.QCapacityN > 0
            ? result.Q0N.Value / result.QCapacityN
            : double.NaN;
        var bActualRatio = solved is not null && result.MaxBuoyancyN > 0
            ? (result.MaxBuoyancyN - result.QCapacityN + solved.Q0N) / result.MaxBuoyancyN
            : double.NaN;
        var lowerResidual = result.LowerCapacitySample?.VerticalResidualM;
        var upperResidual = result.UpperCapacitySample?.VerticalResidualM;

        Console.WriteLine(
            "SURFACE_BOUNDARY " +
            $"name={result.Name}; " +
            $"classification={result.Classification}; " +
            $"Q0N={q0Text}; " +
            $"QCapacityN={result.QCapacityN:R}; " +
            $"QRatio={(double.IsFinite(qRatio) ? qRatio.ToString("R") : "n/a")}; " +
            $"BActualToMaxRatio={(double.IsFinite(bActualRatio) ? bActualRatio.ToString("R") : "n/a")}; " +
            $"buoySteadyDragN={result.BuoySteadyDragN:R}; " +
            $"X={(solved?.EndpointXM.ToString("R") ?? "n/a")}; " +
            $"Z={(solved?.EndpointZM.ToString("R") ?? "n/a")}; " +
            $"residual={(solved?.VerticalResidualM.ToString("R") ?? "n/a")}; " +
            $"lowerResidual={(lowerResidual?.ToString("R") ?? "n/a")}; " +
            $"upperResidual={(upperResidual?.ToString("R") ?? "n/a")}; " +
            $"minH={(solved?.MinHN.ToString("R") ?? "n/a")}; " +
            $"maxH={(solved?.MaxHN.ToString("R") ?? "n/a")}; " +
            $"minV={(solved?.MinVN.ToString("R") ?? "n/a")}; " +
            $"maxV={(solved?.MaxVN.ToString("R") ?? "n/a")}; " +
            $"VSignChange={(solved?.VChangedSign.ToString() ?? "n/a")}; " +
            $"pointCrossings={(solved?.PointCrossingCount.ToString() ?? "n/a")}; " +
            $"iterations={result.Iterations}; " +
            $"note={result.Note}");
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

    private enum SurfaceBoundaryClassification
    {
        Solved,
        VerticalGeometryBoundaryNonUnique,
        NoGeometricSolutionLineShorterThanDepth,
        NoFiniteRootTautWithHorizontalLoad,
        NoRootWithinBuoyancyCapacity,
        IndeterminateDegenerateTension,
        NumericalIterationLimitReached
    }

    private sealed record PointGroup(
        double PositionAlongLineM,
        int SourceCount,
        double CurrentForceN,
        double WeightWaterForceN);

    private sealed record SurfaceBoundaryIntegrationSample(
        double Q0N,
        bool IsAvailable,
        double EndpointXM,
        double EndpointZM,
        double VerticalResidualM,
        double MinHN,
        double MaxHN,
        double MinVN,
        double MaxVN,
        bool VChangedSign,
        int PointCrossingCount,
        string Note);

    private sealed record SurfaceBoundaryResult(
        string Name,
        SurfaceBoundaryClassification Classification,
        double LineLengthM,
        double MaxBuoyancyN,
        double QCapacityN,
        double BuoySteadyDragN,
        double? Q0N,
        SurfaceBoundaryIntegrationSample? SolvedSample,
        SurfaceBoundaryIntegrationSample? LowerCapacitySample,
        SurfaceBoundaryIntegrationSample? UpperCapacitySample,
        int Iterations,
        string Note);
}
