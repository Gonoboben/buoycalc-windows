using System.Collections.Generic;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.ApplicationModel;

/// <summary>
/// Completed application-level engineering run: the calculation result together
/// with the immutable snapshot derived immediately from that exact result.
/// </summary>
public sealed record ApplicationCalculationRun(
    CalculationResult Result,
    CalculationSnapshot Snapshot);

public static class ApplicationCalculationRunner
{
    public static ApplicationRunOutcome RunOutcome(
        EnvironmentInput environment,
        BuoyInput buoy,
        IReadOnlyList<AssemblyItemInput> assemblyItems,
        AnchorInput anchor,
        double safetyFactor)
    {
        return RunOutcome(
            environment,
            buoy,
            assemblyItems,
            anchor,
            safetyFactor,
            BuoyCalculator.Calculate);
    }

    internal static ApplicationRunOutcome RunOutcome(
        EnvironmentInput environment,
        BuoyInput buoy,
        IReadOnlyList<AssemblyItemInput> assemblyItems,
        AnchorInput anchor,
        double safetyFactor,
        Func<EnvironmentInput, BuoyInput, IReadOnlyList<AssemblyItemInput>, AnchorInput, double, CalculationResult> calculate)
    {
        ArgumentNullException.ThrowIfNull(calculate);
        EngineeringInputValidator.Validate(environment, buoy, assemblyItems, anchor, safetyFactor);
        CurrentProfileRequirement.EnsureUsable(environment);

        if (ApplicationRunOutcomeFactory.TryCreateLineShorterThanDepthFromValidatedInputs(
                environment,
                buoy,
                assemblyItems,
                anchor,
                safetyFactor,
                out var rejected))
        {
            return rejected;
        }

        return ApplicationRunOutcomeFactory.FromCalculated(
            RunValidated(environment, buoy, assemblyItems, anchor, safetyFactor, calculate));
    }

    /// <summary>
    /// Typed calculated branch of the completed application-run outcome contract.
    /// Existing callers may continue to use <see cref="Run"/> without losing their
    /// non-null CalculationResult/CalculationSnapshot convenience contract.
    /// </summary>
    public static CalculatedApplicationRunOutcome RunCalculatedOutcome(
        EnvironmentInput environment,
        BuoyInput buoy,
        IReadOnlyList<AssemblyItemInput> assemblyItems,
        AnchorInput anchor,
        double safetyFactor)
    {
        return ApplicationRunOutcomeFactory.FromCalculated(
            Run(environment, buoy, assemblyItems, anchor, safetyFactor));
    }

    public static ApplicationCalculationRun Run(
        EnvironmentInput environment,
        BuoyInput buoy,
        IReadOnlyList<AssemblyItemInput> assemblyItems,
        AnchorInput anchor,
        double safetyFactor)
    {
        EngineeringInputValidator.Validate(environment, buoy, assemblyItems, anchor, safetyFactor);
        CurrentProfileRequirement.EnsureUsable(environment);

        return RunValidated(
            environment,
            buoy,
            assemblyItems,
            anchor,
            safetyFactor,
            BuoyCalculator.Calculate);
    }

    private static ApplicationCalculationRun RunValidated(
        EnvironmentInput environment,
        BuoyInput buoy,
        IReadOnlyList<AssemblyItemInput> assemblyItems,
        AnchorInput anchor,
        double safetyFactor,
        Func<EnvironmentInput, BuoyInput, IReadOnlyList<AssemblyItemInput>, AnchorInput, double, CalculationResult> calculate)
    {
        var inputHash = CalculationRunFingerprint.ComputeInputHash(
            environment,
            buoy,
            assemblyItems,
            anchor,
            safetyFactor);

        var result = calculate(
            environment,
            buoy,
            assemblyItems,
            anchor,
            safetyFactor);

        var snapshot = CalculationSnapshotBuilder.Build(environment, buoy, result);
        var provenance = CalculationRunProvenanceFactory.Create(inputHash, snapshot);
        snapshot = snapshot with { Provenance = provenance };

        return new ApplicationCalculationRun(result, snapshot);
    }
}
