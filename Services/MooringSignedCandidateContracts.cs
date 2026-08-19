using System;
using System.Collections.Generic;
using System.Linq;

namespace BuoyCalc.Windows.Services;

/// <summary>
/// Typed identity of the calculation-core geometry source.
/// Display/read-model strings must be derived from this identity, not used as authority.
/// </summary>
public enum MooringShapeSourceIdentity
{
    FallbackShapeSolver,
    IterativeDiscreteSolver,
    SignedBoundaryFeedback
}

/// <summary>
/// Calculation-core outcome of the signed boundary/feedback candidate.
/// This is candidate state, not selected-source state.
/// </summary>
public enum MooringSignedCandidateStatus
{
    Accepted,
    RejectedPhysical,
    RejectedNumerical,
    BudgetExhausted,
    Indeterminate,
    Unavailable
}

/// <summary>
/// In-memory signed candidate truth. This type is intentionally independent of
/// persistence, report, PDF, 2D and UI models.
/// </summary>
public sealed record MooringSignedCandidateResult
{
    public const int ProductionFeedbackBudget = 64;

    private MooringSignedCandidateResult(
        MooringSignedCandidateStatus status,
        MooringShapeResult? shape,
        MooringSurfaceBoundaryInfoResult? boundary,
        bool exactFixedPointReached,
        int feedbackIterations,
        bool containsDiscreteLoads,
        int pointLoadCrossings,
        string diagnosticCode,
        string diagnosticText)
    {
        SourceIdentity = MooringShapeSourceIdentity.SignedBoundaryFeedback;
        Status = status;
        Shape = shape;
        Boundary = boundary;
        ExactFixedPointReached = exactFixedPointReached;
        FeedbackIterations = feedbackIterations;
        ContainsDiscreteLoads = containsDiscreteLoads;
        PointLoadCrossings = pointLoadCrossings;
        DiagnosticCode = diagnosticCode;
        DiagnosticText = diagnosticText;
    }

    public MooringShapeSourceIdentity SourceIdentity { get; }
    public MooringSignedCandidateStatus Status { get; }
    public MooringShapeResult? Shape { get; }
    public MooringSurfaceBoundaryInfoResult? Boundary { get; }
    public bool ExactFixedPointReached { get; }
    public int FeedbackIterations { get; }
    public bool ContainsDiscreteLoads { get; }
    public int PointLoadCrossings { get; }
    public string DiagnosticCode { get; }
    public string DiagnosticText { get; }

    public static MooringSignedCandidateResult CreateAccepted(
        MooringShapeResult shape,
        MooringSurfaceBoundaryInfoResult boundary,
        int feedbackIterations,
        bool containsDiscreteLoads,
        int pointLoadCrossings,
        string diagnosticCode,
        string diagnosticText)
    {
        ArgumentNullException.ThrowIfNull(shape);
        ArgumentNullException.ThrowIfNull(boundary);
        ValidateDiagnostic(diagnosticCode, diagnosticText);
        ValidateIterations(feedbackIterations);
        ValidateDiscreteIdentity(containsDiscreteLoads, pointLoadCrossings);
        ValidateShape(shape);

        if (!boundary.Solved ||
            boundary.SolutionState is null ||
            !boundary.Q0N.HasValue ||
            !double.IsFinite(boundary.Q0N.Value))
        {
            throw new ArgumentException(
                "Accepted signed candidate requires a solved boundary with one finite Q0 state.",
                nameof(boundary));
        }

        if (boundary.SolutionState.PointLoadCrossings != pointLoadCrossings)
        {
            throw new ArgumentException(
                "Accepted signed candidate point-load crossings must match the solved boundary state.",
                nameof(pointLoadCrossings));
        }

        return new MooringSignedCandidateResult(
            MooringSignedCandidateStatus.Accepted,
            shape,
            boundary,
            true,
            feedbackIterations,
            containsDiscreteLoads,
            pointLoadCrossings,
            diagnosticCode,
            diagnosticText);
    }

    public static MooringSignedCandidateResult CreateNonAccepted(
        MooringSignedCandidateStatus status,
        MooringShapeResult? shape,
        MooringSurfaceBoundaryInfoResult? boundary,
        int feedbackIterations,
        bool containsDiscreteLoads,
        int pointLoadCrossings,
        string diagnosticCode,
        string diagnosticText)
    {
        if (status == MooringSignedCandidateStatus.Accepted)
            throw new ArgumentException("Use CreateAccepted for Accepted state.", nameof(status));

        ValidateDiagnostic(diagnosticCode, diagnosticText);
        ValidateIterationsAllowZero(feedbackIterations);
        ValidateDiscreteIdentity(containsDiscreteLoads, pointLoadCrossings);
        if (shape is not null)
            ValidateShape(shape);

        if (status == MooringSignedCandidateStatus.BudgetExhausted &&
            feedbackIterations != ProductionFeedbackBudget)
        {
            throw new ArgumentException(
                $"BudgetExhausted requires exactly {ProductionFeedbackBudget} completed feedback iterations.",
                nameof(feedbackIterations));
        }

        if (status == MooringSignedCandidateStatus.Indeterminate && boundary?.Q0N is double q0 && double.IsFinite(q0))
        {
            throw new ArgumentException(
                "Indeterminate signed candidate must not claim one finite Q0 state.",
                nameof(boundary));
        }

        return new MooringSignedCandidateResult(
            status,
            shape,
            boundary,
            false,
            feedbackIterations,
            containsDiscreteLoads,
            pointLoadCrossings,
            diagnosticCode,
            diagnosticText);
    }

    private static void ValidateIterations(int feedbackIterations)
    {
        if (feedbackIterations < 1 || feedbackIterations > ProductionFeedbackBudget)
        {
            throw new ArgumentOutOfRangeException(
                nameof(feedbackIterations),
                feedbackIterations,
                $"Accepted feedback iterations must be in [1, {ProductionFeedbackBudget}].");
        }
    }

    private static void ValidateIterationsAllowZero(int feedbackIterations)
    {
        if (feedbackIterations < 0 || feedbackIterations > ProductionFeedbackBudget)
        {
            throw new ArgumentOutOfRangeException(
                nameof(feedbackIterations),
                feedbackIterations,
                $"Feedback iterations must be in [0, {ProductionFeedbackBudget}].");
        }
    }

    private static void ValidateDiscreteIdentity(bool containsDiscreteLoads, int pointLoadCrossings)
    {
        if (pointLoadCrossings < 0)
            throw new ArgumentOutOfRangeException(nameof(pointLoadCrossings));

        if (containsDiscreteLoads != (pointLoadCrossings > 0))
        {
            throw new ArgumentException(
                "ContainsDiscreteLoads must exactly describe whether internal point-load crossings were consumed.",
                nameof(containsDiscreteLoads));
        }
    }

    private static void ValidateDiagnostic(string diagnosticCode, string diagnosticText)
    {
        if (string.IsNullOrWhiteSpace(diagnosticCode))
            throw new ArgumentException("Diagnostic code is required.", nameof(diagnosticCode));
        if (string.IsNullOrWhiteSpace(diagnosticText))
            throw new ArgumentException("Diagnostic text is required.", nameof(diagnosticText));
    }

    private static void ValidateShape(MooringShapeResult shape)
    {
        if (shape.Nodes.Count < 2)
            throw new ArgumentException("Signed candidate shape must contain at least two ordered nodes.", nameof(shape));

        if (!Finite(shape.DepthM) ||
            !Finite(shape.LineLengthM) ||
            !Finite(shape.HorizontalOffsetM) ||
            !Finite(shape.VerticalResidualM) ||
            !Finite(shape.ConvergenceResidualM) ||
            !Finite(shape.AngleScale))
        {
            throw new ArgumentException("Signed candidate shape contains a non-finite scalar.", nameof(shape));
        }

        if (shape.Nodes.Any(node =>
                !Finite(node.AlongLineM) ||
                !Finite(node.XOffsetM) ||
                !Finite(node.ZDepthM) ||
                !Finite(node.SegmentLengthM) ||
                !Finite(node.SegmentAngleFromVerticalDeg) ||
                !Finite(node.SegmentTensionKn)))
        {
            throw new ArgumentException("Signed candidate shape contains a non-finite node value.", nameof(shape));
        }
    }

    private static bool Finite(double value) => double.IsFinite(value);
}

/// <summary>
/// Typed calculation/application result for the geometry source that has actually
/// been selected. It is deliberately upstream of SelectedShapeReadModel.
/// </summary>
public sealed record MooringSelectedShapeResult
{
    private MooringSelectedShapeResult(
        MooringShapeResult shape,
        MooringShapeSourceIdentity sourceIdentity,
        bool selectedConverged,
        bool selectedUsesDiscreteLoads,
        string methodNote)
    {
        Shape = shape;
        SourceIdentity = sourceIdentity;
        SelectedConverged = selectedConverged;
        SelectedUsesDiscreteLoads = selectedUsesDiscreteLoads;
        MethodNote = methodNote;
    }

    public MooringShapeResult Shape { get; }
    public MooringShapeSourceIdentity SourceIdentity { get; }
    public bool SelectedConverged { get; }
    public bool SelectedUsesDiscreteLoads { get; }
    public string MethodNote { get; }

    public static MooringSelectedShapeResult Create(
        MooringShapeResult shape,
        MooringShapeSourceIdentity sourceIdentity,
        bool selectedConverged,
        bool selectedUsesDiscreteLoads,
        string methodNote)
    {
        ArgumentNullException.ThrowIfNull(shape);
        if (shape.Nodes.Count < 2)
            throw new ArgumentException("Selected shape must contain at least two nodes.", nameof(shape));
        if (string.IsNullOrWhiteSpace(methodNote))
            throw new ArgumentException("Selected-shape method note is required.", nameof(methodNote));

        if (sourceIdentity == MooringShapeSourceIdentity.SignedBoundaryFeedback && !selectedConverged)
        {
            throw new ArgumentException(
                "SignedBoundaryFeedback may be selected only as an accepted/converged signed candidate.",
                nameof(selectedConverged));
        }

        return new MooringSelectedShapeResult(
            shape,
            sourceIdentity,
            selectedConverged,
            selectedUsesDiscreteLoads,
            methodNote);
    }
}
