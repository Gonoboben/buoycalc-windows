using System.Collections.Generic;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.ApplicationModel;

/// <summary>
/// Application orchestration boundary for one complete engineering calculation run.
///
/// The core calculation runs first. The immutable calculation snapshot is then built once
/// before any report or presentation renderer consumes the result.
/// </summary>
public static class CalculationRunBuilder
{
    public static CalculationSnapshot Build(
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

        return CalculationSnapshotBuilder.Build(environment, result);
    }
}
