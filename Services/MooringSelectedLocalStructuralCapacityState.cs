using System;
using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public enum MooringLocalStructuralCapacityStatus
{
    Ok,
    Insufficient,
    DemandUnavailable,
    CapacityUnavailable,
    SafetyFactorUnavailable,
    UnsupportedConnectorCount,
    NotRatedByCurrentModel,
    NoPositiveDemand
}

/// <summary>
/// Capacity comparison for one internal sequence element. The demand/provenance
/// comes only from the validated F3-B selected local-demand map. This row does
/// not modify legacy ElementCalculationRow reserve/status fields.
/// </summary>
public sealed record MooringSelectedLocalStructuralCapacityRow(
    int ElementNumber,
    string Kind,
    string Title,
    string PresetName,
    int Count,
    bool IsExpectedStructuralElement,
    bool IsCapacityCandidate,
    double StartAlongLineM,
    double EndAlongLineM,
    double PositionAlongLineM,
    double? LocalDesignDemandN,
    double? LocalDesignDemandKn,
    double? BreakingLoadKn,
    double? WorkingLoadKn,
    double? LocalReserve,
    MooringLocalStructuralCapacityStatus Status,
    MooringLocalElementDemandLocationKind? DemandLocation,
    int? DemandSegmentNumber,
    double? DemandAlongLineM,
    string Note);

/// <summary>
/// Selected local structural-capacity / weak-link authority. A governing row may
/// be reported among valid rated elements even when StructuralCapacityCoverageComplete
/// is false; incomplete coverage must never be interpreted as an overall safe/pass claim.
/// </summary>
public sealed record MooringSelectedLocalStructuralCapacityState(
    MooringShapeSourceIdentity SourceIdentity,
    double WaveHorizontalIncrementN,
    IReadOnlyList<MooringSelectedLocalStructuralCapacityRow> Rows,
    int ExpectedStructuralElementCount,
    int RatedStructuralElementCount,
    int IncompleteStructuralElementCount,
    int InsufficientElementCount,
    bool StructuralCapacityCoverageComplete,
    int? GoverningElementNumber,
    string? GoverningTitle,
    string? GoverningPresetName,
    double? GoverningReserve,
    double? GoverningDemandN,
    double? GoverningWorkingLoadKn,
    MooringLocalStructuralCapacityStatus? GoverningStatus,
    string MethodNote);

public static class MooringSelectedLocalStructuralCapacityStateProjector
{
    public static MooringSelectedLocalStructuralCapacityState? Project(
        CalculationResult result,
        MooringSelectedLocalElementDemandState? localDemand)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (localDemand is null)
            return null;

        if (localDemand.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback)
        {
            throw new InvalidOperationException(
                "Selected local structural capacity requires SignedBoundaryFeedback local-demand identity.");
        }

        if (!double.IsFinite(localDemand.WaveHorizontalIncrementN) ||
            localDemand.WaveHorizontalIncrementN < 0.0)
        {
            throw new InvalidOperationException(
                "Selected local structural capacity requires a finite non-negative F3-B wave increment.");
        }

        var elementByNumber = result.ElementRows.ToDictionary(x => x.Number);
        var rows = new List<MooringSelectedLocalStructuralCapacityRow>(localDemand.Rows.Count);

        foreach (var demand in localDemand.Rows.OrderBy(x => x.ElementNumber))
        {
            if (!elementByNumber.TryGetValue(demand.ElementNumber, out var element))
            {
                throw new InvalidOperationException(
                    $"Selected local structural capacity cannot join sequence element {demand.ElementNumber} to CalculationResult.ElementRows.");
            }

            if (!string.Equals(demand.Kind, element.Kind, StringComparison.Ordinal) ||
                !string.Equals(demand.Title, element.Title, StringComparison.Ordinal) ||
                !string.Equals(demand.PresetName, element.PresetName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Selected local structural capacity identity mismatch at element {demand.ElementNumber}.");
            }

            rows.Add(BuildRow(result, demand, element));
        }

        var expectedStructural = rows.Where(x => x.IsExpectedStructuralElement).ToList();
        var ratedStructural = expectedStructural.Count(x =>
            x.BreakingLoadKn.HasValue &&
            x.WorkingLoadKn.HasValue &&
            x.Status is MooringLocalStructuralCapacityStatus.Ok
                or MooringLocalStructuralCapacityStatus.Insufficient
                or MooringLocalStructuralCapacityStatus.NoPositiveDemand);
        var incompleteStructural = expectedStructural.Count(x =>
            x.Status is MooringLocalStructuralCapacityStatus.DemandUnavailable
                or MooringLocalStructuralCapacityStatus.CapacityUnavailable
                or MooringLocalStructuralCapacityStatus.SafetyFactorUnavailable
                or MooringLocalStructuralCapacityStatus.UnsupportedConnectorCount);
        var insufficient = expectedStructural.Count(x =>
            x.Status == MooringLocalStructuralCapacityStatus.Insufficient);
        var coverageComplete = expectedStructural.Count > 0 && incompleteStructural == 0;

        var governing = rows
            .Where(x => x.IsCapacityCandidate && x.LocalReserve.HasValue)
            .OrderBy(x => x.LocalReserve!.Value)
            .ThenBy(x => x.ElementNumber)
            .FirstOrDefault();

        return new MooringSelectedLocalStructuralCapacityState(
            localDemand.SourceIdentity,
            localDemand.WaveHorizontalIncrementN,
            rows,
            expectedStructural.Count,
            ratedStructural,
            incompleteStructural,
            insufficient,
            coverageComplete,
            governing?.ElementNumber,
            governing?.Title,
            governing?.PresetName,
            governing?.LocalReserve,
            governing?.LocalDesignDemandN,
            governing?.WorkingLoadKn,
            governing?.Status,
            "Selected v1 local structural-capacity authority: F3-B local design demand is compared only with existing element MBL and existing SafetyFactor via WLL=MBL/SF; governing weak link is minimum valid local reserve with sequence-number tie break. Payloads have no MBL in the current model and are not capacity-rated. Connector Count must be exactly one; no parallel/series strength scaling is inferred. Legacy CalculationResult weak-link/reserve/status fields remain unchanged.");
    }

    private static MooringSelectedLocalStructuralCapacityRow BuildRow(
        CalculationResult result,
        MooringSelectedLocalElementDemandRow demand,
        ElementCalculationRow element)
    {
        var isConnector = string.Equals(element.Kind, "Соединитель", StringComparison.OrdinalIgnoreCase);
        var isExpectedStructural = demand.IsDistributed || isConnector;

        if (!isExpectedStructural)
        {
            return Create(
                demand,
                element,
                false,
                false,
                demand.DesignDemandN,
                null,
                null,
                null,
                MooringLocalStructuralCapacityStatus.NotRatedByCurrentModel,
                "Current payload/instrument model exposes local demand but no MBL capacity field.");
        }

        if (!demand.Available ||
            !demand.DesignDemandN.HasValue ||
            !double.IsFinite(demand.DesignDemandN.Value) ||
            demand.DesignDemandN.Value < 0.0)
        {
            return Create(
                demand,
                element,
                true,
                false,
                demand.DesignDemandN,
                null,
                null,
                null,
                MooringLocalStructuralCapacityStatus.DemandUnavailable,
                "Validated local design demand is unavailable for this structural element.");
        }

        if (isConnector && element.Count != 1)
        {
            return Create(
                demand,
                element,
                true,
                false,
                demand.DesignDemandN,
                null,
                null,
                null,
                MooringLocalStructuralCapacityStatus.UnsupportedConnectorCount,
                "Connector Count is not one; no parallel/series MBL scaling is defined by the current model.");
        }

        if (!double.IsFinite(element.BreakingLoadKn) || element.BreakingLoadKn <= 0.0)
        {
            return Create(
                demand,
                element,
                true,
                false,
                demand.DesignDemandN,
                null,
                null,
                null,
                MooringLocalStructuralCapacityStatus.CapacityUnavailable,
                "Positive finite MBL is not available for this structural element.");
        }

        if (!double.IsFinite(result.SafetyFactor) || result.SafetyFactor <= 0.0)
        {
            return Create(
                demand,
                element,
                true,
                false,
                demand.DesignDemandN,
                element.BreakingLoadKn,
                null,
                null,
                MooringLocalStructuralCapacityStatus.SafetyFactorUnavailable,
                "Positive finite SafetyFactor is required before WLL/local reserve can be defined.");
        }

        var workingLoadKn = element.BreakingLoadKn / result.SafetyFactor;
        if (!double.IsFinite(workingLoadKn) || workingLoadKn <= 0.0)
        {
            throw new InvalidOperationException(
                $"Selected local structural capacity produced invalid WLL at element {element.Number}.");
        }

        if (element.WorkingLoadKn != workingLoadKn)
        {
            throw new InvalidOperationException(
                $"Selected local structural capacity WLL identity differs from legacy element WLL at element {element.Number}.");
        }

        if (demand.DesignDemandN.Value == 0.0)
        {
            return Create(
                demand,
                element,
                true,
                false,
                0.0,
                element.BreakingLoadKn,
                workingLoadKn,
                null,
                MooringLocalStructuralCapacityStatus.NoPositiveDemand,
                "Local design demand is exactly zero; no finite governing reserve is fabricated.");
        }

        var reserve = workingLoadKn * 1000.0 / demand.DesignDemandN.Value;
        if (!double.IsFinite(reserve) || reserve < 0.0)
        {
            throw new InvalidOperationException(
                $"Selected local structural capacity produced invalid reserve at element {element.Number}.");
        }

        var status = reserve >= 1.0
            ? MooringLocalStructuralCapacityStatus.Ok
            : MooringLocalStructuralCapacityStatus.Insufficient;

        return Create(
            demand,
            element,
            true,
            true,
            demand.DesignDemandN,
            element.BreakingLoadKn,
            workingLoadKn,
            reserve,
            status,
            string.Empty);
    }

    private static MooringSelectedLocalStructuralCapacityRow Create(
        MooringSelectedLocalElementDemandRow demand,
        ElementCalculationRow element,
        bool isExpectedStructural,
        bool isCapacityCandidate,
        double? demandN,
        double? breakingLoadKn,
        double? workingLoadKn,
        double? reserve,
        MooringLocalStructuralCapacityStatus status,
        string note)
    {
        return new MooringSelectedLocalStructuralCapacityRow(
            demand.ElementNumber,
            demand.Kind,
            demand.Title,
            demand.PresetName,
            element.Count,
            isExpectedStructural,
            isCapacityCandidate,
            demand.StartAlongLineM,
            demand.EndAlongLineM,
            demand.PositionAlongLineM,
            demandN,
            demandN.HasValue ? demandN.Value / 1000.0 : null,
            breakingLoadKn,
            workingLoadKn,
            reserve,
            status,
            demand.GoverningLocation,
            demand.GoverningSegmentNumber,
            demand.GoverningAlongLineM,
            note);
    }
}
