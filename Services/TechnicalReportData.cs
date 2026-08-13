using System.Collections.Generic;

namespace BuoyCalc.Windows.Services;

public sealed record TechnicalReportData(
    IReadOnlyList<SegmentTensionRow> TensionRows,
    MooringShapeResult Shape,
    MooringShapeProjectionResult ShapeProjection,
    MooringShapeForceResult ShapeForces,
    MooringShapeTensionResult ShapeTensions,
    MooringForceShapeConsistencyResult ForceShapeConsistency,
    MooringSequencePositionResult SequencePositions,
    MooringDiscreteLoadTensionResult DiscreteLoadTensions,
    MooringDiscreteLoadShapeResult DiscreteLoadShape,
    MooringSignedNodeEquilibriumResult SignedNodeEquilibrium,
    MooringAlternativeDiscreteNodeResult AlternativeDiscreteNodes,
    MooringIterativeSolverResult IterativeSolver,
    EngineeringDiagnosticsResult Diagnostics,
    MooringVectorBalanceResult VectorBalance);
