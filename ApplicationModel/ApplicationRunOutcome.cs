using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

namespace BuoyCalc.Windows.ApplicationModel;

public enum ApplicationRunOutcomeKind
{
    Calculated,
    PreflightPhysicalRejected
}

public enum PreflightPhysicalRejectionKind
{
    LineShorterThanDepth
}

public enum PreflightPhysicalRejectionVerdict
{
    NotSuitable
}

/// <summary>
/// Canonical engineering evidence for the structural short-line preflight rejection.
/// This is application authority only; localized presentation text is deliberately absent.
/// </summary>
public sealed record ShortLinePreflightPhysicalRejectionEvidence(
    double DepthM,
    double AvailableActiveLineLengthM,
    double MinimumRequiredActiveLineLengthM,
    double DeficitM);

/// <summary>
/// Terminal physical-rejection authority formed before the calculation core is invoked.
/// It cannot carry selected geometry, calculated loads, or F1-F4 state.
/// </summary>
public sealed record PreflightPhysicalRejectionState
{
    private PreflightPhysicalRejectionState(
        PreflightPhysicalRejectionKind classification,
        string diagnosticCode,
        ShortLinePreflightPhysicalRejectionEvidence evidence)
    {
        Classification = classification;
        DiagnosticCode = diagnosticCode;
        Evidence = evidence;
    }

    public PreflightPhysicalRejectionKind Classification { get; }
    public string DiagnosticCode { get; }
    public PreflightPhysicalRejectionVerdict VerdictKind => PreflightPhysicalRejectionVerdict.NotSuitable;
    public string Verdict => "Не подходит";
    public bool HasHardFailure => true;
    public bool BlocksEngineeringGeometry => true;
    public ShortLinePreflightPhysicalRejectionEvidence Evidence { get; }

    internal static PreflightPhysicalRejectionState CreateLineShorterThanDepth(
        string diagnosticCode,
        double depthM,
        double availableActiveLineLengthM,
        double minimumRequiredActiveLineLengthM,
        double deficitM)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(diagnosticCode);

        if (!double.IsFinite(depthM) ||
            !double.IsFinite(availableActiveLineLengthM) ||
            !double.IsFinite(minimumRequiredActiveLineLengthM) ||
            !double.IsFinite(deficitM))
        {
            throw new ArgumentOutOfRangeException(
                nameof(depthM),
                "Preflight physical-rejection evidence must contain finite values.");
        }

        if (depthM <= 0.0 ||
            availableActiveLineLengthM < 0.0 ||
            minimumRequiredActiveLineLengthM != depthM ||
            availableActiveLineLengthM + MooringSurfaceBoundaryIntegrationKernel.LengthToleranceM >= minimumRequiredActiveLineLengthM ||
            deficitM <= 0.0 ||
            deficitM != minimumRequiredActiveLineLengthM - availableActiveLineLengthM)
        {
            throw new ArgumentException(
                "LineShorterThanDepth evidence must exactly retain depth, available active line, minimum required line, and positive deficit under the existing signed length-tolerance contract.");
        }

        return new PreflightPhysicalRejectionState(
            PreflightPhysicalRejectionKind.LineShorterThanDepth,
            diagnosticCode,
            new ShortLinePreflightPhysicalRejectionEvidence(
                depthM,
                availableActiveLineLengthM,
                minimumRequiredActiveLineLengthM,
                deficitM));
    }
}

/// <summary>
/// Discriminated completed application-run authority. Consumers must pattern-match the
/// sealed branch instead of combining unrelated nullable result and rejection fields.
/// </summary>
public abstract record ApplicationRunOutcome
{
    private protected ApplicationRunOutcome(
        ApplicationRunOutcomeKind kind,
        CalculationRunProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(provenance);
        Kind = kind;
        Provenance = provenance;
    }

    public ApplicationRunOutcomeKind Kind { get; }
    public CalculationRunProvenance Provenance { get; }
}

public sealed record CalculatedApplicationRunOutcome : ApplicationRunOutcome
{
    internal CalculatedApplicationRunOutcome(ApplicationCalculationRun calculation)
        : base(
            ApplicationRunOutcomeKind.Calculated,
            calculation?.Snapshot.Provenance
                ?? throw new ArgumentException(
                    "Calculated outcome requires a completed calculation run with retained provenance.",
                    nameof(calculation)))
    {
        Calculation = calculation
            ?? throw new ArgumentNullException(nameof(calculation));
    }

    public ApplicationCalculationRun Calculation { get; }
}

public sealed record PreflightPhysicalRejectedApplicationRunOutcome : ApplicationRunOutcome
{
    private const string LineShorterThanDepthDiagnosticCode = "Preflight_LineShorterThanDepth";

    private PreflightPhysicalRejectedApplicationRunOutcome(
        PreflightPhysicalRejectionState rejection,
        CalculationRunProvenance provenance)
        : base(ApplicationRunOutcomeKind.PreflightPhysicalRejected, provenance)
    {
        ArgumentNullException.ThrowIfNull(rejection);
        Rejection = rejection;
    }

    public PreflightPhysicalRejectionState Rejection { get; }

    internal static PreflightPhysicalRejectedApplicationRunOutcome CreateLineShorterThanDepth(
        EnvironmentInput environment,
        BuoyInput buoy,
        IReadOnlyList<AssemblyItemInput> assemblyItems,
        AnchorInput anchor,
        double safetyFactor)
    {
        EngineeringInputValidator.Validate(environment, buoy, assemblyItems, anchor, safetyFactor);
        CurrentProfileRequirement.EnsureUsable(environment);

        if (TryCreateLineShorterThanDepthFromValidatedInputs(
                environment,
                buoy,
                assemblyItems,
                anchor,
                safetyFactor,
                out var outcome))
        {
            return outcome;
        }

        throw new InvalidOperationException(
            "LineShorterThanDepth preflight authority requires the active line to be shorter than depth under the existing signed length-tolerance contract.");
    }

    internal static bool TryCreateLineShorterThanDepthFromValidatedInputs(
        EnvironmentInput environment,
        BuoyInput buoy,
        IReadOnlyList<AssemblyItemInput> assemblyItems,
        AnchorInput anchor,
        double safetyFactor,
        out PreflightPhysicalRejectedApplicationRunOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(buoy);
        ArgumentNullException.ThrowIfNull(assemblyItems);
        ArgumentNullException.ThrowIfNull(anchor);

        var depthM = environment.DepthM;
        var availableActiveLineLengthM = assemblyItems
            .Where(x => x.IsEnabled &&
                        x.Kind == AssemblyItemKind.Line &&
                        x.RopePreset is not null)
            .Sum(x => Math.Max(0.0, x.LengthM));
        var minimumRequiredActiveLineLengthM = depthM;
        if (availableActiveLineLengthM + MooringSurfaceBoundaryIntegrationKernel.LengthToleranceM >=
            minimumRequiredActiveLineLengthM)
        {
            outcome = null!;
            return false;
        }

        var deficitM = minimumRequiredActiveLineLengthM - availableActiveLineLengthM;
        var rejection = PreflightPhysicalRejectionState.CreateLineShorterThanDepth(
            LineShorterThanDepthDiagnosticCode,
            depthM,
            availableActiveLineLengthM,
            minimumRequiredActiveLineLengthM,
            deficitM);
        var inputHash = CalculationRunFingerprint.ComputeInputHash(
            environment,
            buoy,
            assemblyItems,
            anchor,
            safetyFactor);
        var provenance = CalculationRunProvenanceFactory.Create(inputHash, rejection);

        outcome = new PreflightPhysicalRejectedApplicationRunOutcome(rejection, provenance);
        return true;
    }
}

public static class ApplicationRunOutcomeFactory
{
    public static CalculatedApplicationRunOutcome FromCalculated(
        ApplicationCalculationRun calculation)
    {
        ArgumentNullException.ThrowIfNull(calculation);
        return new CalculatedApplicationRunOutcome(calculation);
    }

    public static PreflightPhysicalRejectedApplicationRunOutcome CreateLineShorterThanDepthPreflightPhysicalRejected(
        EnvironmentInput environment,
        BuoyInput buoy,
        IReadOnlyList<AssemblyItemInput> assemblyItems,
        AnchorInput anchor,
        double safetyFactor)
    {
        return PreflightPhysicalRejectedApplicationRunOutcome.CreateLineShorterThanDepth(
            environment,
            buoy,
            assemblyItems,
            anchor,
            safetyFactor);
    }

    internal static bool TryCreateLineShorterThanDepthFromValidatedInputs(
        EnvironmentInput environment,
        BuoyInput buoy,
        IReadOnlyList<AssemblyItemInput> assemblyItems,
        AnchorInput anchor,
        double safetyFactor,
        out PreflightPhysicalRejectedApplicationRunOutcome outcome)
    {
        return PreflightPhysicalRejectedApplicationRunOutcome.TryCreateLineShorterThanDepthFromValidatedInputs(
            environment,
            buoy,
            assemblyItems,
            anchor,
            safetyFactor,
            out outcome);
    }
}
