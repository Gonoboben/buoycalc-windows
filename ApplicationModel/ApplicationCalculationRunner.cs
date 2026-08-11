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
    public static ApplicationCalculationRun Run(
        EnvironmentInput environment,
        BuoyInput buoy,
        IReadOnlyList<AssemblyItemInput> assemblyItems,
        AnchorInput anchor,
        double safetyFactor)
    {
        var result = BuoyCalculator.Calculate(
            environment,
            buoy,
            assemblyItems,
            anchor,
            safetyFactor);

        var snapshot = CalculationSnapshotBuilder.Build(environment, result);

        return new ApplicationCalculationRun(result, snapshot);
    }
}
