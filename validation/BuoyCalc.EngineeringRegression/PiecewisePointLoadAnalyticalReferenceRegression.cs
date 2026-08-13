internal static class PiecewisePointLoadAnalyticalReferenceRegression
{
    private const double G = 9.80665;
    private const double WaterDensityKgM3 = 1025.0;
    private const double CurrentSpeedMS = 0.5;

    private const double UpperLengthM = 30.0;
    private const double LowerLengthM = 25.0;
    private const double TotalLengthM = UpperLengthM + LowerLengthM;
    private const double TargetDepthM = 50.0;

    private const double H0N = 82.0;
    private const double QxNPerM = 3.075;
    private const double WeightNPerM = 0.980665;
    private const double QCapacityN = 9071.15125;

    private const double ConnectorDryMassKg = 5.0;
    private const double ConnectorVolumeM3 = 0.0007;
    private const double ConnectorAreaM2 = 0.01;
    private const double PayloadDryMassKg = 40.0;
    private const double PayloadVolumeM3 = 0.005;
    private const double PayloadAreaM2 = 0.05;
    private const double PointCd = 1.0;

    private const double ExpectedPointForceXN = 7.6875;
    private const double ExpectedPointWeightWaterKg = 39.1575;
    private const double ExpectedPointWeightForceN = 384.003897375;
    private const double ExpectedExactQ0N = 741.26584043891;
    private const double ExpectedExactXM = 19.48029920259;

    private const double ForceEpsilonN = 1e-12;
    private const double RootDepthToleranceM = 1e-12;
    private const double RootIntervalToleranceN = 1e-11;

    public static void Validate()
    {
        var point = BuildDeterministicPointGroup();
        ValidatePointInputReconstruction(point);
        ValidateExactPointReference(point);
        ValidateZeroPointIdentity();
        ValidateSamePositionGrouping(point);
        ValidateSignedBuoyantPoint();
        ValidatePointPositionSensitivity(point);
        ValidateMidpointMeshConvergence(point);
    }

    private static PointLoad BuildDeterministicPointGroup()
    {
        var connectorWeightWaterKg =
            ConnectorDryMassKg - ConnectorVolumeM3 * WaterDensityKgM3;
        var payloadWeightWaterKg =
            PayloadDryMassKg - PayloadVolumeM3 * WaterDensityKgM3;
        var pointWeightWaterKg = connectorWeightWaterKg + payloadWeightWaterKg;

        var connectorDragN =
            0.5 * WaterDensityKgM3 * PointCd * ConnectorAreaM2 * CurrentSpeedMS * CurrentSpeedMS;
        var payloadDragN =
            0.5 * WaterDensityKgM3 * PointCd * PayloadAreaM2 * CurrentSpeedMS * CurrentSpeedMS;

        return new PointLoad(
            connectorDragN + payloadDragN,
            pointWeightWaterKg * G,
            pointWeightWaterKg,
            2);
    }

    private static void ValidatePointInputReconstruction(PointLoad point)
    {
        AssertNear(ExpectedPointForceXN, point.ForceXN, 1e-12, "point steady drag reconstruction");
        AssertNear(ExpectedPointWeightWaterKg, point.WeightWaterKg, 1e-12, "point submerged weight reconstruction");
        AssertNear(ExpectedPointWeightForceN, point.WeightForceN, 1e-9, "point submerged weight force reconstruction");

        if (point.SourceCount != 2)
        {
            throw new InvalidOperationException(
                $"piecewise point reference: expected two grouped source rows, got {point.SourceCount}.");
        }
    }

    private static void ValidateExactPointReference(PointLoad point)
    {
        var solved = SolveExactPointRoot(point, UpperLengthM);
        RequireAvailable(solved.Geometry, "exact point solved geometry");

        AssertNear(ExpectedExactQ0N, solved.Q0N, 2e-8, "exact point Q0 reference");
        AssertNear(ExpectedExactXM, solved.Geometry.XM, 2e-9, "exact point X reference");
        AssertNear(TargetDepthM, solved.Geometry.ZM, 2e-11, "exact point depth closure");

        if (solved.Q0N <= 0 || solved.Q0N >= QCapacityN)
        {
            throw new InvalidOperationException(
                $"piecewise point reference: exact Q0={solved.Q0N:R} N must lie strictly inside buoyancy capacity.");
        }

        AssertNear(
            H0N + QxNPerM * TotalLengthM + point.ForceXN,
            solved.Geometry.TerminalHN,
            1e-9,
            "exact point terminal H");
        AssertNear(
            solved.Q0N - WeightNPerM * TotalLengthM - point.WeightForceN,
            solved.Geometry.TerminalVN,
            1e-9,
            "exact point terminal V");

        AssertNear(0, solved.Geometry.PointDeltaXM, 0, "point geometry jump X");
        AssertNear(0, solved.Geometry.PointDeltaZM, 0, "point geometry jump Z");
        AssertNear(point.ForceXN, solved.Geometry.PostPointHN - solved.Geometry.PrePointHN, 1e-12, "point jump H");
        AssertNear(-point.WeightForceN, solved.Geometry.PostPointVN - solved.Geometry.PrePointVN, 1e-9, "point jump V");

        Console.WriteLine(
            "POINT_LOAD_REFERENCE " +
            $"Q0N={solved.Q0N:R}; X={solved.Geometry.XM:R}; Z={solved.Geometry.ZM:R}; " +
            $"pointFxN={point.ForceXN:R}; pointWeightWaterKg={point.WeightWaterKg:R}; " +
            $"pointWeightForceN={point.WeightForceN:R}; terminalH={solved.Geometry.TerminalHN:R}; terminalV={solved.Geometry.TerminalVN:R}");
    }

    private static void ValidateZeroPointIdentity()
    {
        var zeroPoint = new PointLoad(0, 0, 0, 0);
        var piecewise = SolveExactPointRoot(zeroPoint, UpperLengthM);
        var single = SolveExactSingleIntervalRoot();

        RequireAvailable(piecewise.Geometry, "zero-point piecewise geometry");
        RequireAvailable(single.Geometry, "zero-point single geometry");

        AssertNear(single.Q0N, piecewise.Q0N, 2e-9, "zero-point Q0 identity");
        AssertNear(single.Geometry.XM, piecewise.Geometry.XM, 2e-10, "zero-point X identity");
        AssertNear(single.Geometry.ZM, piecewise.Geometry.ZM, 2e-10, "zero-point Z identity");
        AssertNear(single.Geometry.TerminalHN, piecewise.Geometry.TerminalHN, 2e-9, "zero-point terminal H identity");
        AssertNear(single.Geometry.TerminalVN, piecewise.Geometry.TerminalVN, 2e-9, "zero-point terminal V identity");
    }

    private static void ValidateSamePositionGrouping(PointLoad grouped)
    {
        var connectorWeightWaterKg = ConnectorDryMassKg - ConnectorVolumeM3 * WaterDensityKgM3;
        var payloadWeightWaterKg = PayloadDryMassKg - PayloadVolumeM3 * WaterDensityKgM3;
        var connector = new PointLoad(
            0.5 * WaterDensityKgM3 * PointCd * ConnectorAreaM2 * CurrentSpeedMS * CurrentSpeedMS,
            connectorWeightWaterKg * G,
            connectorWeightWaterKg,
            1);
        var payload = new PointLoad(
            0.5 * WaterDensityKgM3 * PointCd * PayloadAreaM2 * CurrentSpeedMS * CurrentSpeedMS,
            payloadWeightWaterKg * G,
            payloadWeightWaterKg,
            1);

        var q0 = ExpectedExactQ0N;
        var upper = ExactInterval(H0N, q0, QxNPerM, WeightNPerM, UpperLengthM);
        RequireAvailable(upper, "same-s grouping upper interval");

        var groupedState = ApplyPointJump(upper.TerminalHN, upper.TerminalVN, grouped);
        var afterConnector = ApplyPointJump(upper.TerminalHN, upper.TerminalVN, connector);
        var sequentialState = ApplyPointJump(afterConnector.HN, afterConnector.VN, payload);

        AssertNear(groupedState.HN, sequentialState.HN, 1e-12, "same-s grouped/sequential H");
        AssertNear(groupedState.VN, sequentialState.VN, 1e-9, "same-s grouped/sequential V");

        var groupedLower = ExactInterval(groupedState.HN, groupedState.VN, QxNPerM, WeightNPerM, LowerLengthM);
        var sequentialLower = ExactInterval(sequentialState.HN, sequentialState.VN, QxNPerM, WeightNPerM, LowerLengthM);
        RequireAvailable(groupedLower, "same-s grouped lower interval");
        RequireAvailable(sequentialLower, "same-s sequential lower interval");

        AssertNear(groupedLower.XM, sequentialLower.XM, 1e-12, "same-s grouped/sequential lower X");
        AssertNear(groupedLower.ZM, sequentialLower.ZM, 1e-12, "same-s grouped/sequential lower Z");
    }

    private static void ValidateSignedBuoyantPoint()
    {
        const double syntheticBuoyantWeightForceN = -120.0;
        var buoyantPoint = new PointLoad(
            ForceXN: 0,
            WeightForceN: syntheticBuoyantWeightForceN,
            WeightWaterKg: syntheticBuoyantWeightForceN / G,
            SourceCount: 1);

        var before = new ForceState(100, 250);
        var after = ApplyPointJump(before.HN, before.VN, buoyantPoint);

        if (!(after.VN > before.VN))
        {
            throw new InvalidOperationException(
                $"piecewise point reference: negative point WeightWater must increase downward cable V; before={before.VN:R}, after={after.VN:R}.");
        }

        AssertNear(120.0, after.VN - before.VN, 1e-12, "signed buoyant point V jump");
    }

    private static void ValidatePointPositionSensitivity(PointLoad point)
    {
        var at20 = SolveExactPointRoot(point, 20.0);
        var at30 = SolveExactPointRoot(point, 30.0);
        var at40 = SolveExactPointRoot(point, 40.0);

        RequireAvailable(at20.Geometry, "point position 20 m");
        RequireAvailable(at30.Geometry, "point position 30 m");
        RequireAvailable(at40.Geometry, "point position 40 m");

        if (Math.Abs(at20.Q0N - at30.Q0N) < 1.0 ||
            Math.Abs(at30.Q0N - at40.Q0N) < 1.0 ||
            Math.Abs(at20.Geometry.XM - at30.Geometry.XM) < 0.1 ||
            Math.Abs(at30.Geometry.XM - at40.Geometry.XM) < 0.1)
        {
            throw new InvalidOperationException(
                "piecewise point reference: moving a finite internal point load along s must materially change the boundary root and endpoint X.");
        }

        Console.WriteLine(
            "POINT_POSITION_REFERENCE " +
            $"s20_Q0={at20.Q0N:R}; s20_X={at20.Geometry.XM:R}; " +
            $"s30_Q0={at30.Q0N:R}; s30_X={at30.Geometry.XM:R}; " +
            $"s40_Q0={at40.Q0N:R}; s40_X={at40.Geometry.XM:R}");
    }

    private static void ValidateMidpointMeshConvergence(PointLoad point)
    {
        var exact = SolveExactPointRoot(point, UpperLengthM);
        var targets = new[] { 0.8, 0.4, 0.2, 0.1, 0.05 };
        var solutions = targets
            .Select(target => SolveMidpointPointRoot(point, target))
            .ToList();

        var qErrors = solutions.Select(x => Math.Abs(x.Q0N - exact.Q0N)).ToArray();
        var xErrors = solutions.Select(x => Math.Abs(x.Geometry.XM - exact.Geometry.XM)).ToArray();

        AssertStrictlyDecreasing(qErrors, "piecewise midpoint Q0 mesh errors");
        AssertStrictlyDecreasing(xErrors, "piecewise midpoint X mesh errors");

        AssertRatioAtLeast(qErrors[1], qErrors[2], 3.5, "piecewise Q0 0.4 -> 0.2 ratio");
        AssertRatioAtLeast(qErrors[2], qErrors[3], 3.5, "piecewise Q0 0.2 -> 0.1 ratio");
        AssertRatioAtLeast(xErrors[1], xErrors[2], 3.5, "piecewise X 0.4 -> 0.2 ratio");
        AssertRatioAtLeast(xErrors[2], xErrors[3], 3.5, "piecewise X 0.2 -> 0.1 ratio");

        var productionIndex = Array.IndexOf(targets, 0.2);
        if (productionIndex < 0)
        {
            throw new InvalidOperationException("piecewise point reference: 0.20 m validation target missing.");
        }

        if (qErrors[productionIndex] > 1.5e-4)
        {
            throw new InvalidOperationException(
                $"piecewise point reference: 0.20 m midpoint Q0 error too large: {qErrors[productionIndex]:R} N.");
        }

        if (xErrors[productionIndex] > 1.0e-5)
        {
            throw new InvalidOperationException(
                $"piecewise point reference: 0.20 m midpoint X error too large: {xErrors[productionIndex]:R} m.");
        }

        for (var i = 0; i < targets.Length; i++)
        {
            var solution = solutions[i];
            Console.WriteLine(
                "POINT_LOAD_MESH " +
                $"targetDs={targets[i]:R}; upperN={solution.UpperSegmentCount}; lowerN={solution.LowerSegmentCount}; " +
                $"upperDs={solution.UpperStepM:R}; lowerDs={solution.LowerStepM:R}; " +
                $"Q0N={solution.Q0N:R}; X={solution.Geometry.XM:R}; Z={solution.Geometry.ZM:R}; " +
                $"QErrorN={solution.Q0N - exact.Q0N:R}; XErrorM={solution.Geometry.XM - exact.Geometry.XM:R}");
        }
    }

    private static ExactRootSolution SolveExactSingleIntervalRoot()
    {
        var lowQ = 0.0;
        var highQ = QCapacityN;
        var low = ExactInterval(H0N, lowQ, QxNPerM, WeightNPerM, TotalLengthM);
        var high = ExactInterval(H0N, highQ, QxNPerM, WeightNPerM, TotalLengthM);
        RequireAvailable(low, "single exact low root bound");
        RequireAvailable(high, "single exact high root bound");

        var lowResidual = low.ZM - TargetDepthM;
        var highResidual = high.ZM - TargetDepthM;
        RequireBracket(lowResidual, highResidual, "single exact root");

        for (var iteration = 0; iteration < 140; iteration++)
        {
            var q = (lowQ + highQ) / 2.0;
            var state = ExactInterval(H0N, q, QxNPerM, WeightNPerM, TotalLengthM);
            RequireAvailable(state, "single exact root midpoint");
            var residual = state.ZM - TargetDepthM;

            if (Math.Abs(residual) <= RootDepthToleranceM ||
                Math.Abs(highQ - lowQ) <= RootIntervalToleranceN)
            {
                return new ExactRootSolution(q, FromSingleInterval(state));
            }

            if (lowResidual * residual <= 0)
            {
                highQ = q;
                highResidual = residual;
            }
            else
            {
                lowQ = q;
                lowResidual = residual;
            }
        }

        var finalQ = (lowQ + highQ) / 2.0;
        var finalState = ExactInterval(H0N, finalQ, QxNPerM, WeightNPerM, TotalLengthM);
        RequireAvailable(finalState, "single exact final root");
        return new ExactRootSolution(finalQ, FromSingleInterval(finalState));
    }

    private static ExactRootSolution SolveExactPointRoot(PointLoad point, double pointPositionM)
    {
        if (pointPositionM <= 0 || pointPositionM >= TotalLengthM)
        {
            throw new ArgumentOutOfRangeException(nameof(pointPositionM));
        }

        var lowQ = 0.0;
        var highQ = QCapacityN;
        var low = ExactPiecewise(H0N, lowQ, point, pointPositionM);
        var high = ExactPiecewise(H0N, highQ, point, pointPositionM);
        RequireAvailable(low, "piecewise exact low root bound");
        RequireAvailable(high, "piecewise exact high root bound");

        var lowResidual = low.ZM - TargetDepthM;
        var highResidual = high.ZM - TargetDepthM;
        RequireBracket(lowResidual, highResidual, "piecewise exact root");

        for (var iteration = 0; iteration < 140; iteration++)
        {
            var q = (lowQ + highQ) / 2.0;
            var state = ExactPiecewise(H0N, q, point, pointPositionM);
            RequireAvailable(state, "piecewise exact root midpoint");
            var residual = state.ZM - TargetDepthM;

            if (Math.Abs(residual) <= RootDepthToleranceM ||
                Math.Abs(highQ - lowQ) <= RootIntervalToleranceN)
            {
                return new ExactRootSolution(q, state);
            }

            if (lowResidual * residual <= 0)
            {
                highQ = q;
                highResidual = residual;
            }
            else
            {
                lowQ = q;
                lowResidual = residual;
            }
        }

        var finalQ = (lowQ + highQ) / 2.0;
        var finalState = ExactPiecewise(H0N, finalQ, point, pointPositionM);
        RequireAvailable(finalState, "piecewise exact final root");
        return new ExactRootSolution(finalQ, finalState);
    }

    private static MidpointRootSolution SolveMidpointPointRoot(PointLoad point, double targetStepM)
    {
        var upperN = (int)Math.Ceiling(UpperLengthM / targetStepM);
        var lowerN = (int)Math.Ceiling(LowerLengthM / targetStepM);
        var upperStepM = UpperLengthM / upperN;
        var lowerStepM = LowerLengthM / lowerN;

        var lowQ = 0.0;
        var highQ = QCapacityN;
        var low = MidpointPiecewise(lowQ, point, upperN, lowerN);
        var high = MidpointPiecewise(highQ, point, upperN, lowerN);
        var lowResidual = low.ZM - TargetDepthM;
        var highResidual = high.ZM - TargetDepthM;
        RequireBracket(lowResidual, highResidual, $"midpoint target {targetStepM:R}");

        for (var iteration = 0; iteration < 140; iteration++)
        {
            var q = (lowQ + highQ) / 2.0;
            var state = MidpointPiecewise(q, point, upperN, lowerN);
            var residual = state.ZM - TargetDepthM;

            if (Math.Abs(residual) <= RootDepthToleranceM ||
                Math.Abs(highQ - lowQ) <= RootIntervalToleranceN)
            {
                return new MidpointRootSolution(q, state, upperN, lowerN, upperStepM, lowerStepM);
            }

            if (lowResidual * residual <= 0)
            {
                highQ = q;
                highResidual = residual;
            }
            else
            {
                lowQ = q;
                lowResidual = residual;
            }
        }

        var finalQ = (lowQ + highQ) / 2.0;
        var finalState = MidpointPiecewise(finalQ, point, upperN, lowerN);
        return new MidpointRootSolution(finalQ, finalState, upperN, lowerN, upperStepM, lowerStepM);
    }

    private static PiecewiseState ExactPiecewise(
        double h0N,
        double q0N,
        PointLoad point,
        double pointPositionM)
    {
        var upper = ExactInterval(h0N, q0N, QxNPerM, WeightNPerM, pointPositionM);
        if (!upper.Available)
        {
            return PiecewiseState.Indeterminate(upper.State);
        }

        var prePointH = upper.TerminalHN;
        var prePointV = upper.TerminalVN;
        var postPoint = ApplyPointJump(prePointH, prePointV, point);
        var lowerLengthM = TotalLengthM - pointPositionM;
        var lower = ExactInterval(postPoint.HN, postPoint.VN, QxNPerM, WeightNPerM, lowerLengthM);
        if (!lower.Available)
        {
            return PiecewiseState.Indeterminate(lower.State);
        }

        return new PiecewiseState(
            true,
            upper.XM + lower.XM,
            upper.ZM + lower.ZM,
            lower.TerminalHN,
            lower.TerminalVN,
            prePointH,
            prePointV,
            postPoint.HN,
            postPoint.VN,
            0,
            0,
            "Available");
    }

    private static PiecewiseState MidpointPiecewise(
        double q0N,
        PointLoad point,
        int upperN,
        int lowerN)
    {
        var h = H0N;
        var v = q0N;
        var x = 0.0;
        var z = 0.0;

        var upperStep = UpperLengthM / upperN;
        for (var i = 0; i < upperN; i++)
        {
            var hMid = h + 0.5 * QxNPerM * upperStep;
            var vMid = v - 0.5 * WeightNPerM * upperStep;
            var tension = Math.Sqrt(hMid * hMid + vMid * vMid);
            if (!double.IsFinite(tension) || tension <= ForceEpsilonN)
            {
                return PiecewiseState.Indeterminate("IndeterminateUpperMidpointResultant");
            }

            x += upperStep * hMid / tension;
            z += upperStep * vMid / tension;
            h += QxNPerM * upperStep;
            v -= WeightNPerM * upperStep;
        }

        var prePointH = h;
        var prePointV = v;
        var postPoint = ApplyPointJump(h, v, point);
        h = postPoint.HN;
        v = postPoint.VN;

        var lowerStep = LowerLengthM / lowerN;
        for (var i = 0; i < lowerN; i++)
        {
            var hMid = h + 0.5 * QxNPerM * lowerStep;
            var vMid = v - 0.5 * WeightNPerM * lowerStep;
            var tension = Math.Sqrt(hMid * hMid + vMid * vMid);
            if (!double.IsFinite(tension) || tension <= ForceEpsilonN)
            {
                return PiecewiseState.Indeterminate("IndeterminateLowerMidpointResultant");
            }

            x += lowerStep * hMid / tension;
            z += lowerStep * vMid / tension;
            h += QxNPerM * lowerStep;
            v -= WeightNPerM * lowerStep;
        }

        return new PiecewiseState(
            true,
            x,
            z,
            h,
            v,
            prePointH,
            prePointV,
            postPoint.HN,
            postPoint.VN,
            0,
            0,
            "Available");
    }

    private static IntervalState ExactInterval(
        double h0N,
        double v0N,
        double qxNPerM,
        double weightNPerM,
        double lengthM)
    {
        var qzNPerM = -weightNPerM;
        var qNorm = Math.Sqrt(qxNPerM * qxNPerM + qzNPerM * qzNPerM);
        var r0Norm = Math.Sqrt(h0N * h0N + v0N * v0N);

        if (qNorm <= ForceEpsilonN)
        {
            if (r0Norm <= ForceEpsilonN)
            {
                return IntervalState.Indeterminate("IndeterminateZeroResultant");
            }

            return new IntervalState(
                true,
                lengthM * h0N / r0Norm,
                lengthM * v0N / r0Norm,
                h0N,
                v0N,
                "Available");
        }

        var ex = qxNPerM / qNorm;
        var ez = qzNPerM / qNorm;
        var nx = -qzNPerM / qNorm;
        var nz = qxNPerM / qNorm;

        var u0 = ex * h0N + ez * v0N;
        var u1 = u0 + qNorm * lengthM;
        var c = nx * h0N + nz * v0N;

        if (Math.Abs(c) <= ForceEpsilonN)
        {
            if (u0 == 0 || u1 == 0 || u0 * u1 < 0)
            {
                return IntervalState.Indeterminate("IndeterminateCollinearZeroCrossing");
            }

            var sign = Math.Sign(u0);
            return new IntervalState(
                true,
                ex * sign * lengthM,
                ez * sign * lengthM,
                h0N + qxNPerM * lengthM,
                v0N - weightNPerM * lengthM,
                "Available");
        }

        var r0 = Math.Sqrt(u0 * u0 + c * c);
        var r1 = Math.Sqrt(u1 * u1 + c * c);
        var asinhDelta =
            Math.Asinh(u1 / Math.Abs(c)) -
            Math.Asinh(u0 / Math.Abs(c));

        var x =
            ex * (r1 - r0) / qNorm +
            nx * (c / qNorm) * asinhDelta;
        var z =
            ez * (r1 - r0) / qNorm +
            nz * (c / qNorm) * asinhDelta;

        if (!double.IsFinite(x) || !double.IsFinite(z))
        {
            return IntervalState.Indeterminate("IndeterminateNonFiniteIntegral");
        }

        return new IntervalState(
            true,
            x,
            z,
            h0N + qxNPerM * lengthM,
            v0N - weightNPerM * lengthM,
            "Available");
    }

    private static ForceState ApplyPointJump(double hN, double vN, PointLoad point)
    {
        return new ForceState(
            hN + point.ForceXN,
            vN - point.WeightForceN);
    }

    private static PiecewiseState FromSingleInterval(IntervalState state)
    {
        return new PiecewiseState(
            state.Available,
            state.XM,
            state.ZM,
            state.TerminalHN,
            state.TerminalVN,
            state.TerminalHN,
            state.TerminalVN,
            state.TerminalHN,
            state.TerminalVN,
            0,
            0,
            state.State);
    }

    private static void RequireAvailable(PiecewiseState state, string label)
    {
        if (!state.Available)
        {
            throw new InvalidOperationException(
                $"piecewise point reference {label}: expected available state, got {state.State}.");
        }
    }

    private static void RequireAvailable(IntervalState state, string label)
    {
        if (!state.Available)
        {
            throw new InvalidOperationException(
                $"piecewise point reference {label}: expected available state, got {state.State}.");
        }
    }

    private static void RequireBracket(double lowResidual, double highResidual, string label)
    {
        if (!double.IsFinite(lowResidual) ||
            !double.IsFinite(highResidual) ||
            lowResidual * highResidual >= 0)
        {
            throw new InvalidOperationException(
                $"piecewise point reference {label}: expected sign-changing bracket, got {lowResidual:R}, {highResidual:R} m.");
        }
    }

    private static void AssertStrictlyDecreasing(IReadOnlyList<double> values, string label)
    {
        for (var i = 1; i < values.Count; i++)
        {
            if (!(values[i] < values[i - 1]))
            {
                throw new InvalidOperationException(
                    $"piecewise point reference {label}: error did not decrease at index {i}: {values[i - 1]:R} -> {values[i]:R}.");
            }
        }
    }

    private static void AssertRatioAtLeast(double coarseError, double fineError, double minimumRatio, string label)
    {
        if (fineError <= 0)
        {
            return;
        }

        var ratio = coarseError / fineError;
        if (ratio < minimumRatio)
        {
            throw new InvalidOperationException(
                $"piecewise point reference {label}: expected ratio >= {minimumRatio:R}, got {ratio:R}.");
        }
    }

    private static void AssertNear(double expected, double actual, double tolerance, string label)
    {
        if (!double.IsFinite(expected) ||
            !double.IsFinite(actual) ||
            Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException(
                $"piecewise point reference {label}: expected {expected:R}, got {actual:R}, tolerance {tolerance:R}.");
        }
    }

    private sealed record PointLoad(
        double ForceXN,
        double WeightForceN,
        double WeightWaterKg,
        int SourceCount);

    private sealed record ForceState(double HN, double VN);

    private sealed record IntervalState(
        bool Available,
        double XM,
        double ZM,
        double TerminalHN,
        double TerminalVN,
        string State)
    {
        public static IntervalState Indeterminate(string state) =>
            new(false, 0, 0, 0, 0, state);
    }

    private sealed record PiecewiseState(
        bool Available,
        double XM,
        double ZM,
        double TerminalHN,
        double TerminalVN,
        double PrePointHN,
        double PrePointVN,
        double PostPointHN,
        double PostPointVN,
        double PointDeltaXM,
        double PointDeltaZM,
        string State)
    {
        public static PiecewiseState Indeterminate(string state) =>
            new(false, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, state);
    }

    private sealed record ExactRootSolution(double Q0N, PiecewiseState Geometry);

    private sealed record MidpointRootSolution(
        double Q0N,
        PiecewiseState Geometry,
        int UpperSegmentCount,
        int LowerSegmentCount,
        double UpperStepM,
        double LowerStepM);
}
