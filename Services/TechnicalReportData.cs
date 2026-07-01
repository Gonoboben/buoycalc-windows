using System.Collections.Generic;

namespace BuoyCalc.Windows.Services;

public sealed record TechnicalReportData(
    IReadOnlyList<SegmentTensionRow> TensionRows,
    MooringShapeResult Shape,
    MooringShapeProjectionResult ShapeProjection,
    MooringShapeForceResult ShapeForces,
    MooringShapeTensionResult ShapeTensions,
    MooringSequencePositionResult SequencePositions,
    MooringDiscreteLoadTensionResult DiscreteLoadTensions,
    MooringDiscreteLoadShapeResult DiscreteLoadShape,
    MooringAlternativeDiscreteNodeResult AlternativeDiscreteNodes,
    MooringIterativeSolverResult IterativeSolver,
    EngineeringDiagnosticsResult Diagnostics,
    MooringVectorBalanceResult VectorBalance);
