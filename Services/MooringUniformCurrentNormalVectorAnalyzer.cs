using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public sealed record MooringUniformCurrentNormalVectorRow(
    int Number,
    int SegmentNumber,
    string SourceElement,
    double CurrentXMS,
    double CurrentZMS,
    double TangentX,
    double TangentZ,
    double NormalVelocityXMS,
    double NormalVelocityZMS,
    double NormalSpeedMS,
    double NormalForceXN,
    double NormalForceZN,
    double NormalForceMagnitudeN,
    double ExistingShapeForceN,
    double MagnitudeDifferenceN,
    string Status);

public sealed record MooringUniformCurrentNormalVectorResult(
    bool Available,
    IReadOnlyList<MooringUniformCurrentNormalVectorRow> Rows,
    double SumNormalForceXN,
    double SumNormalForceZN,
    double SumNormalForceMagnitudeN,
    double MaxMagnitudeDifferenceN,
    string MethodNote);

public static class MooringUniformCurrentNormalVectorAnalyzer
{
    private const double VectorEpsilon = 1e-12;
    private const double RelativeSoftwareTolerance = 1e-9;

    public static MooringUniformCurrentNormalVectorResult Build(
        EnvironmentInput environment,
        CalculationResult result,
        MooringShapeProjectionResult projection,
        MooringShapeForceResult shapeForces)
    {
        if (environment.UseCurrentProfile)
        {
            return Unavailable(
                "INFO: вектор нормального сопротивления недоступен для профиля течения до утверждения signed East/North -> planar X/Z projection и знака VerticalCurrentMS.");
        }

        if (result.SegmentRows.Count == 0 || projection.Rows.Count == 0 || shapeForces.Rows.Count == 0)
        {
            return Unavailable(
                "INFO: нет согласованных сегментов, X/Z-проекций или shape-force rows для uniform-current normal-vector diagnostic.");
        }

        var segmentsByNumber = result.SegmentRows.ToDictionary(x => x.Number);
        var shapeForceBySegment = shapeForces.Rows.ToDictionary(x => x.SegmentNumber);
        var rows = new List<MooringUniformCurrentNormalVectorRow>();
        var currentX = -Math.Abs(environment.CurrentSpeedMS);
        const double currentZ = 0.0;

        foreach (var projectionRow in projection.Rows)
        {
            if (!segmentsByNumber.TryGetValue(projectionRow.SegmentNumber, out var segment) ||
                !shapeForceBySegment.TryGetValue(projectionRow.SegmentNumber, out var shapeForce))
            {
                return Unavailable(
                    $"INFO: segment/projection/shape-force mapping неоднозначен для segment #{projectionRow.SegmentNumber}; вектор не публикуется.");
            }

            var tangentLength = Math.Sqrt(
                projectionRow.DeltaXM * projectionRow.DeltaXM +
                projectionRow.DeltaZM * projectionRow.DeltaZM);

            if (!double.IsFinite(tangentLength) || tangentLength <= VectorEpsilon)
            {
                return Unavailable(
                    $"INFO: segment #{projectionRow.SegmentNumber} имеет вырожденную X/Z-касательную; вектор не публикуется.");
            }

            var tx = projectionRow.DeltaXM / tangentLength;
            var tz = projectionRow.DeltaZM / tangentLength;
            var dot = currentX * tx + currentZ * tz;
            var normalVelocityX = currentX - dot * tx;
            var normalVelocityZ = currentZ - dot * tz;
            var normalSpeed = Math.Sqrt(
                normalVelocityX * normalVelocityX +
                normalVelocityZ * normalVelocityZ);

            var coefficient = 0.5 * segment.WaterDensityKgM3 * segment.DragCoefficient * segment.ProjectedAreaM2;
            var vectorFactor = coefficient * normalSpeed;
            var forceX = vectorFactor * normalVelocityX;
            var forceZ = vectorFactor * normalVelocityZ;
            var forceMagnitude = Math.Sqrt(forceX * forceX + forceZ * forceZ);
            var difference = forceMagnitude - shapeForce.ShapeForceN;
            var tolerance = RelativeSoftwareTolerance * Math.Max(1.0, Math.Abs(shapeForce.ShapeForceN));

            rows.Add(new MooringUniformCurrentNormalVectorRow(
                rows.Count + 1,
                projectionRow.SegmentNumber,
                segment.SourceElement,
                currentX,
                currentZ,
                tx,
                tz,
                normalVelocityX,
                normalVelocityZ,
                normalSpeed,
                forceX,
                forceZ,
                forceMagnitude,
                shapeForce.ShapeForceN,
                difference,
                Math.Abs(difference) <= tolerance
                    ? "OK"
                    : "INDETERMINATE: magnitude differs from existing shape-force path"));
        }

        if (rows.Count == 0)
        {
            return Unavailable("INFO: uniform-current normal-vector diagnostic produced no rows.");
        }

        return new MooringUniformCurrentNormalVectorResult(
            true,
            rows,
            rows.Sum(x => x.NormalForceXN),
            rows.Sum(x => x.NormalForceZN),
            rows.Sum(x => x.NormalForceMagnitudeN),
            rows.Max(x => Math.Abs(x.MagnitudeDifferenceN)),
            "INFO-only Berteaux gamma=0 normal-resistance vector for scalar/uniform-current mode. +X_shape points buoy -> anchor, so environmental current is U=(-|CurrentSpeedMS|,0). Existing segment DragCoefficient is reused only as the historical normal-force coefficient candidate; profile-current projection, tangential resistance, solver feedback, gate and verdict are unchanged.");
    }

    private static MooringUniformCurrentNormalVectorResult Unavailable(string methodNote)
    {
        return new MooringUniformCurrentNormalVectorResult(
            false,
            Array.Empty<MooringUniformCurrentNormalVectorRow>(),
            0.0,
            0.0,
            0.0,
            0.0,
            methodNote);
    }
}
