using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public static class TechnicalReportDataBuilder
{
    public static TechnicalReportData Build(EnvironmentInput environment, CalculationResult result)
    {
        var tensionRows = SegmentTensionAnalyzer.Build(result);
        var shape = MooringShapeSolver.Build(environment, result);
        var shapeProjection = MooringShapeProjection.Build(shape);
        var shapeForces = MooringShapeForceAnalyzer.Build(result, shapeProjection);
        var shapeTensions = MooringShapeTensionAnalyzer.Build(result, tensionRows, shapeForces);
        var sequencePositions = MooringSequencePositioner.Build(result);
        var discreteLoadTensions = MooringDiscreteLoadTensionAnalyzer.Build(result, tensionRows, sequencePositions);
        var discreteLoadShape = MooringDiscreteLoadShapeBuilder.Build(shape, discreteLoadTensions);
        var alternativeDiscreteNodes = MooringAlternativeDiscreteNodeProjector.Build(sequencePositions, discreteLoadShape, shape);
        var iterativeSolver = MooringIterativeSolver.Build(result, shape, sequencePositions, tensionRows);
        var diagnostics = EngineeringDiagnostics.Build(environment, result, shape, tensionRows);
        var vectorBalance = MooringVectorBalance.Build(result);

        return new TechnicalReportData(
            tensionRows,
            shape,
            shapeProjection,
            shapeForces,
            shapeTensions,
            sequencePositions,
            discreteLoadTensions,
            discreteLoadShape,
            alternativeDiscreteNodes,
            iterativeSolver,
            diagnostics,
            vectorBalance);
    }
}
