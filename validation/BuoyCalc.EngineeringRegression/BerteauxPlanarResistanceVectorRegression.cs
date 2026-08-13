internal static class BerteauxPlanarResistanceVectorRegression
{
    private const double Rho = 1025.0;
    private const double Cn = 1.5;
    private const double Gamma = 0.02;
    private const double Ct = Gamma * Cn;
    private const double DiameterM = 0.01;
    private const double SegmentLengthM = 2.0;
    private const double SpeedMS = 1.2;
    private const double Tolerance = 1e-10;

    public static void Validate()
    {
        ValidateParallelCase();
        ValidateNormalCase();
        ValidateFortyFiveDegreeCase();
        ValidateCurrentReversal();
        ValidateZeroCurrent();
        ValidateTangentOrientationInvariance();
        ValidateScalarReduction();
    }

    private static void ValidateParallelCase()
    {
        var current = new Vec(SpeedMS, 0.0);
        var tangent = new Vec(1.0, 0.0);
        var state = Build(current, tangent);

        AssertVecNear(Vec.Zero, state.NormalVelocity, "parallel normal velocity");
        AssertVecNear(current, state.TangentialVelocity, "parallel tangential velocity");
        AssertVecNear(Vec.Zero, state.NormalForce, "parallel normal force");

        if (!(state.TangentialForce.X > 0.0))
        {
            throw new InvalidOperationException("Berteaux planar vector regression: parallel tangential force must follow +X current.");
        }

        AssertNear(0.0, Cross(state.TangentialForce, tangent), "parallel tangential force collinearity");
    }

    private static void ValidateNormalCase()
    {
        var current = new Vec(SpeedMS, 0.0);
        var tangent = new Vec(0.0, 1.0);
        var state = Build(current, tangent);

        AssertVecNear(current, state.NormalVelocity, "normal-case normal velocity");
        AssertVecNear(Vec.Zero, state.TangentialVelocity, "normal-case tangential velocity");
        AssertVecNear(Vec.Zero, state.TangentialForce, "normal-case tangential force");

        if (!(state.NormalForce.X > 0.0))
        {
            throw new InvalidOperationException("Berteaux planar vector regression: normal force must follow +X current in the normal-incidence case.");
        }

        AssertNear(0.0, Dot(state.NormalForce, tangent), "normal force orthogonality");
    }

    private static void ValidateFortyFiveDegreeCase()
    {
        var s = 1.0 / Math.Sqrt(2.0);
        var current = new Vec(SpeedMS, 0.0);
        var tangent = new Vec(s, s);
        var state = Build(current, tangent);

        var expectedTangentialVelocity = new Vec(SpeedMS / 2.0, SpeedMS / 2.0);
        var expectedNormalVelocity = new Vec(SpeedMS / 2.0, -SpeedMS / 2.0);

        AssertVecNear(expectedTangentialVelocity, state.TangentialVelocity, "45-degree tangential velocity");
        AssertVecNear(expectedNormalVelocity, state.NormalVelocity, "45-degree normal velocity");
        AssertVecNear(current, state.NormalVelocity + state.TangentialVelocity, "45-degree velocity reconstruction");
        AssertNear(0.0, Dot(state.NormalForce, tangent), "45-degree normal force orthogonality");
        AssertNear(0.0, Cross(state.TangentialForce, tangent), "45-degree tangential force collinearity");

        if (!(state.NormalForce.X > 0.0 && state.NormalForce.Z < 0.0))
        {
            throw new InvalidOperationException("Berteaux planar vector regression: 45-degree normal force quadrant mismatch.");
        }

        if (!(state.TangentialForce.X > 0.0 && state.TangentialForce.Z > 0.0))
        {
            throw new InvalidOperationException("Berteaux planar vector regression: 45-degree tangential force quadrant mismatch.");
        }
    }

    private static void ValidateCurrentReversal()
    {
        var s = 1.0 / Math.Sqrt(2.0);
        var tangent = new Vec(s, s);
        var positive = Build(new Vec(SpeedMS, 0.0), tangent);
        var negative = Build(new Vec(-SpeedMS, 0.0), tangent);

        AssertVecNear(-positive.NormalVelocity, negative.NormalVelocity, "reversed normal velocity");
        AssertVecNear(-positive.TangentialVelocity, negative.TangentialVelocity, "reversed tangential velocity");
        AssertVecNear(-positive.NormalForce, negative.NormalForce, "reversed normal force");
        AssertVecNear(-positive.TangentialForce, negative.TangentialForce, "reversed tangential force");
    }

    private static void ValidateZeroCurrent()
    {
        var tangent = Unit(new Vec(0.3, 0.7));
        var state = Build(Vec.Zero, tangent);

        AssertVecNear(Vec.Zero, state.NormalVelocity, "zero-current normal velocity");
        AssertVecNear(Vec.Zero, state.TangentialVelocity, "zero-current tangential velocity");
        AssertVecNear(Vec.Zero, state.NormalForce, "zero-current normal force");
        AssertVecNear(Vec.Zero, state.TangentialForce, "zero-current tangential force");
        AssertVecNear(Vec.Zero, state.TotalForce, "zero-current total force");
    }

    private static void ValidateTangentOrientationInvariance()
    {
        var current = new Vec(0.9, -0.4);
        var tangent = Unit(new Vec(0.6, 0.8));
        var forward = Build(current, tangent);
        var reverse = Build(current, -tangent);

        AssertVecNear(forward.NormalVelocity, reverse.NormalVelocity, "tangent reversal normal velocity");
        AssertVecNear(forward.TangentialVelocity, reverse.TangentialVelocity, "tangent reversal tangential velocity");
        AssertVecNear(forward.NormalForce, reverse.NormalForce, "tangent reversal normal force");
        AssertVecNear(forward.TangentialForce, reverse.TangentialForce, "tangent reversal tangential force");
    }

    private static void ValidateScalarReduction()
    {
        var referenceNormalIncidenceForce = 0.5 * Rho * Cn * DiameterM * SegmentLengthM * SpeedMS * SpeedMS;

        foreach (var phiDeg in new[] { 0.0, 30.0, 45.0, 60.0, 90.0 })
        {
            var phiRad = phiDeg * Math.PI / 180.0;
            var tangent = new Vec(Math.Cos(phiRad), Math.Sin(phiRad));
            var state = Build(new Vec(SpeedMS, 0.0), tangent);

            var expectedNormal = referenceNormalIncidenceForce * Math.Sin(phiRad) * Math.Sin(phiRad);
            var expectedTangential = Math.PI * Gamma * referenceNormalIncidenceForce * Math.Cos(phiRad) * Math.Cos(phiRad);

            AssertNear(expectedNormal, Norm(state.NormalForce), $"scalar normal reduction phi={phiDeg:R}");
            AssertNear(expectedTangential, Norm(state.TangentialForce), $"scalar tangential reduction phi={phiDeg:R}");
        }
    }

    private static ResistanceState Build(Vec current, Vec tangent)
    {
        var unitTangent = Unit(tangent);
        var tangentialVelocity = Dot(current, unitTangent) * unitTangent;
        var normalVelocity = current - tangentialVelocity;

        var normalFactor = 0.5 * Rho * Cn * DiameterM * SegmentLengthM;
        var tangentialFactor = 0.5 * Rho * Ct * Math.PI * DiameterM * SegmentLengthM;

        var normalForce = normalFactor * Norm(normalVelocity) * normalVelocity;
        var tangentialForce = tangentialFactor * Norm(tangentialVelocity) * tangentialVelocity;

        return new ResistanceState(
            normalVelocity,
            tangentialVelocity,
            normalForce,
            tangentialForce,
            normalForce + tangentialForce);
    }

    private static Vec Unit(Vec value)
    {
        var norm = Norm(value);
        if (norm <= Tolerance)
        {
            throw new InvalidOperationException("Berteaux planar vector regression: synthetic tangent must be non-zero.");
        }

        return (1.0 / norm) * value;
    }

    private static double Dot(Vec a, Vec b) => a.X * b.X + a.Z * b.Z;
    private static double Cross(Vec a, Vec b) => a.X * b.Z - a.Z * b.X;
    private static double Norm(Vec value) => Math.Sqrt(value.X * value.X + value.Z * value.Z);

    private static void AssertNear(double expected, double actual, string label)
    {
        if (Math.Abs(expected - actual) > Tolerance)
        {
            throw new InvalidOperationException(
                $"Berteaux planar vector regression {label}: expected {expected:R}, got {actual:R}.");
        }
    }

    private static void AssertVecNear(Vec expected, Vec actual, string label)
    {
        AssertNear(expected.X, actual.X, $"{label} X");
        AssertNear(expected.Z, actual.Z, $"{label} Z");
    }

    private readonly record struct Vec(double X, double Z)
    {
        public static Vec Zero => new(0.0, 0.0);

        public static Vec operator +(Vec a, Vec b) => new(a.X + b.X, a.Z + b.Z);
        public static Vec operator -(Vec a, Vec b) => new(a.X - b.X, a.Z - b.Z);
        public static Vec operator -(Vec value) => new(-value.X, -value.Z);
        public static Vec operator *(double scalar, Vec value) => new(scalar * value.X, scalar * value.Z);
    }

    private sealed record ResistanceState(
        Vec NormalVelocity,
        Vec TangentialVelocity,
        Vec NormalForce,
        Vec TangentialForce,
        Vec TotalForce);
}
