using System.Collections.Generic;

namespace BuoyCalc.Windows.Services;

public sealed record TechnicalReportData(
    IReadOnlyList<SegmentTensionRow> TensionRows,
    MooringSignedOrientationResult SignedOrientation,
    MooringShapeResult Shape,
    MooringShapeProjectionResult ShapeProjection,
    MooringShapeForceResult ShapeForces,
    MooringUniformCurrentNormalVectorResult UniformCurrentNormalVector,
    MooringShapeTensionResult ShapeTensions,
    MooringForceShapeConsistencyResult ForceShapeConsistency,
    MooringSequencePositionResult SequencePositions,
    MooringSurfaceBoundaryInfoResult SurfaceBoundaryInfo,
    MooringSurfaceBoundaryTensionTraceResult SurfaceBoundaryTensionTrace,
    MooringDiscreteLoadTensionResult DiscreteLoadTensions,
    MooringDiscreteLoadShapeResult DiscreteLoadShape,
    MooringSignedNodeEquilibriumResult SignedNodeEquilibrium,
    MooringAlternativeDiscreteNodeResult AlternativeDiscreteNodes,
    MooringIterativeSolverResult IterativeSolver,
    MooringSignedNodeEquilibriumResult? FinalIterationSignedNodeEquilibrium,
    EngineeringDiagnosticsResult Diagnostics,
    MooringVectorBalanceResult VectorBalance);
