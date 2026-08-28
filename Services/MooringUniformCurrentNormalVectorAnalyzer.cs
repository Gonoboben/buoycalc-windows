using System;
using System.Collections.Generic;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

// Legacy compatibility container retained while the v1 technical-report data shape is frozen.
// The scalar/uniform-current diagnostic itself is retired: production current authority is
// always the explicit depth profile.
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
    public static MooringUniformCurrentNormalVectorResult Build(
        EnvironmentInput environment,
        CalculationResult result,
        MooringShapeProjectionResult projection,
        MooringShapeForceResult shapeForces)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(shapeForces);

        // Deliberately do not inspect CurrentSpeedMS or UseCurrentProfile here.
        // The historical scalar/uniform-current branch has no production meaning now that
        // every calculation requires a depth profile. A future profile-vector diagnostic
        // would require its own validated physics package rather than reviving this shortcut.
        return new MooringUniformCurrentNormalVectorResult(
            false,
            Array.Empty<MooringUniformCurrentNormalVectorRow>(),
            0.0,
            0.0,
            0.0,
            0.0,
            string.Empty);
    }
}
