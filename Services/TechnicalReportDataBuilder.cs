using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public static class TechnicalReportDataBuilder
{
    public static TechnicalReportData Build(EnvironmentInput environment, CalculationResult result)
    {
        return Build(environment, null, result);
    }

    public static TechnicalReportData Build(EnvironmentInput environment, BuoyInput? buoy, CalculationResult result)
    {
        var tensionRows = SegmentTensionAnalyzer.Build(result);
        var signedOrientation = MooringSignedOrientationAnalyzer.Build(tensionRows);
        var shape = MooringShapeSolver.Build(environment, result);
        var shapeProjection = MooringShapeProjection.Build(shape);
        var shapeForces = MooringShapeForceAnalyzer.Build(result, shapeProjection);
        var uniformCurrentNormalVector = MooringUniformCurrentNormalVectorAnalyzer.Build(environment, result, shapeProjection, shapeForces);
        var shapeTensions = MooringShapeTensionAnalyzer.Build(result, tensionRows, shapeForces);
        var forceShapeConsistency = MooringForceShapeConsistencyAnalyzer.Build(shapeProjection, shapeTensions);
        var sequencePositions = MooringSequencePositioner.Build(result);
        var surfaceBoundaryInfo = MooringSurfaceBoundaryInfoAnalyzer.Build(environment, buoy, result, sequencePositions);
        var surfaceBoundaryTensionTrace = MooringSurfaceBoundaryTensionTraceBuilder.Build(result, sequencePositions, surfaceBoundaryInfo);
        var discreteLoadTensions = MooringDiscreteLoadTensionAnalyzer.Build(result, tensionRows, sequencePositions);
        var discreteLoadShape = MooringDiscreteLoadShapeBuilder.Build(shape, discreteLoadTensions);
        var signedNodeEquilibrium = MooringSignedNodeEquilibriumAnalyzer.Build(sequencePositions, discreteLoadTensions, discreteLoadShape);
        var alternativeDiscreteNodes = MooringAlternativeDiscreteNodeProjector.Build(sequencePositions, discreteLoadShape, shape);
        var iterativeSolver = MooringIterativeSolver.Build(result, shape, sequencePositions, tensionRows);
        var finalIterationSignedNodeEquilibrium =
            iterativeSolver.FinalDiscreteLoadTensions is not null && iterativeSolver.FinalDiscreteLoadShape is not null
                ? MooringSignedNodeEquilibriumAnalyzer.Build(sequencePositions, iterativeSolver.FinalDiscreteLoadTensions, iterativeSolver.FinalDiscreteLoadShape)
                : null;
        var diagnostics = EngineeringDiagnostics.Build(environment, result, shape, tensionRows);
        var vectorBalance = MooringVectorBalance.Build(result);

        return new TechnicalReportData(
            tensionRows,
            signedOrientation,
            shape,
            shapeProjection,
            shapeForces,
            uniformCurrentNormalVector,
            shapeTensions,
            forceShapeConsistency,
            sequencePositions,
            surfaceBoundaryInfo,
            surfaceBoundaryTensionTrace,
            discreteLoadTensions,
            discreteLoadShape,
            signedNodeEquilibrium,
            alternativeDiscreteNodes,
            iterativeSolver,
            finalIterationSignedNodeEquilibrium,
            diagnostics,
            vectorBalance);
    }
}
