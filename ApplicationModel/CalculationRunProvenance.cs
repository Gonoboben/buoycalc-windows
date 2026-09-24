using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

namespace BuoyCalc.Windows.ApplicationModel;

/// <summary>
/// Immutable identity of one completed application calculation authority.
/// Renderers consume this value unchanged; they never create run identifiers or hashes.
/// </summary>
public sealed record CalculationRunProvenance(
    string RunId,
    DateTimeOffset CalculationTimestampUtc,
    string InputHash,
    string ResultHash,
    string SourceIdentity);

/// <summary>
/// Canonical SHA-256 contracts for calculation input and retained result identity.
///
/// Contract: UTF-8 JSON, no indentation, ordinal property names, explicit nulls,
/// invariant JSON numbers, record declaration order and retained list order.
/// The schema identifier is the first field and must change before the field set,
/// ordering, encoding or null rules change. No dictionary or UI/localized rendering
/// state participates in either canonical representation.
/// </summary>
public static class CalculationRunFingerprint
{
    public const string Algorithm = "SHA-256";
    public const string Encoding = "UTF-8 JSON";
    public const string InputSchema = "buoycalc-engineering-input/v1";
    public const string ResultSchema = "buoycalc-engineering-result/v2";

    private static readonly JsonSerializerOptions CanonicalOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null,
        DictionaryKeyPolicy = null
    };

    public static string ComputeInputHash(
        EnvironmentInput environment,
        BuoyInput buoy,
        IReadOnlyList<AssemblyItemInput> assemblyItems,
        AnchorInput anchor,
        double safetyFactor)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(buoy);
        ArgumentNullException.ThrowIfNull(assemblyItems);
        ArgumentNullException.ThrowIfNull(anchor);

        var canonical = new CanonicalInput(
            InputSchema,
            new CanonicalEnvironment(
                environment.WaterDensityKgM3,
                environment.DepthM,
                environment.WaveHeightM,
                environment.WavePeriodS,
                new CanonicalSeabed(
                    environment.Seabed.Id,
                    environment.Seabed.Name,
                    environment.Seabed.HoldingMultiplier),
                environment.EffectiveCurrentProfile
                    .Select(x => new CanonicalCurrentPoint(
                        x.DepthM,
                        x.EastCurrentMS,
                        x.NorthCurrentMS,
                        x.VerticalCurrentMS,
                        x.WaterDensityKgM3))
                    .ToArray()),
            new CanonicalBuoy(
                buoy.Name,
                buoy.VolumeM3,
                buoy.WeightKg,
                buoy.ProjectedAreaM2,
                buoy.DragCoefficient),
            assemblyItems.Select(ProjectAssemblyItem).ToArray(),
            new CanonicalAnchor(
                anchor.Name,
                anchor.Type,
                anchor.Material,
                anchor.WeightAirKg,
                anchor.VolumeM3,
                anchor.BaseHoldingCoefficient),
            safetyFactor);

        return Hash(canonical);
    }

    public static string ComputeResultHash(CalculationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var canonical = new CanonicalResult(
            ResultSchema,
            snapshot.Result,
            snapshot.SelectedShape,
            snapshot.SignedCandidate,
            snapshot.PhysicalDisposition,
            snapshot.ShadowSelectedCore,
            snapshot.SelectedDesignEnvelope,
            snapshot.SelectedDesignTensionDemand,
            snapshot.SelectedAnchorReaction,
            snapshot.SelectedLocalElementDemand,
            snapshot.SelectedLocalStructuralCapacity,
            snapshot.SelectedEngineeringAssessment);

        return Hash(canonical);
    }

    private static CanonicalAssemblyItem ProjectAssemblyItem(AssemblyItemInput item)
    {
        return new CanonicalAssemblyItem(
            item.Kind,
            item.Title,
            item.IsEnabled,
            item.RopePreset is null
                ? null
                : new CanonicalRope(
                    item.RopePreset.Id,
                    item.RopePreset.Name,
                    item.RopePreset.Material,
                    item.RopePreset.DiameterMm,
                    item.RopePreset.BreakingLoadKn,
                    item.RopePreset.WeightWaterKgM,
                    item.RopePreset.DragCoefficient),
            item.ConnectorPreset is null
                ? null
                : new CanonicalConnector(
                    item.ConnectorPreset.Id,
                    item.ConnectorPreset.Name,
                    item.ConnectorPreset.Type,
                    item.ConnectorPreset.WeightAirKg,
                    item.ConnectorPreset.VolumeM3,
                    item.ConnectorPreset.BreakingLoadKn,
                    item.ConnectorPreset.ProjectedAreaM2,
                    item.ConnectorPreset.DragCoefficient),
            item.LengthM,
            item.Count,
            item.PayloadWeightAirKg,
            item.PayloadVolumeM3,
            item.PayloadProjectedAreaM2,
            item.PayloadDragCoefficient);
    }

    private static string Hash<T>(T canonical)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(canonical, CanonicalOptions);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private sealed record CanonicalInput(
        string Schema,
        CanonicalEnvironment Environment,
        CanonicalBuoy Buoy,
        IReadOnlyList<CanonicalAssemblyItem> AssemblyItems,
        CanonicalAnchor Anchor,
        double SafetyFactor);

    private sealed record CanonicalEnvironment(
        double WaterDensityKgM3,
        double DepthM,
        double WaveHeightM,
        double WavePeriodS,
        CanonicalSeabed Seabed,
        IReadOnlyList<CanonicalCurrentPoint> CurrentProfile);

    private sealed record CanonicalSeabed(string Id, string Name, double HoldingMultiplier);

    private sealed record CanonicalCurrentPoint(
        double DepthM,
        double EastCurrentMS,
        double NorthCurrentMS,
        double VerticalCurrentMS,
        double WaterDensityKgM3);

    private sealed record CanonicalBuoy(
        string Name,
        double VolumeM3,
        double WeightKg,
        double ProjectedAreaM2,
        double DragCoefficient);

    private sealed record CanonicalAssemblyItem(
        AssemblyItemKind Kind,
        string Title,
        bool IsEnabled,
        CanonicalRope? RopePreset,
        CanonicalConnector? ConnectorPreset,
        double LengthM,
        int Count,
        double PayloadWeightAirKg,
        double PayloadVolumeM3,
        double PayloadProjectedAreaM2,
        double PayloadDragCoefficient);

    private sealed record CanonicalRope(
        string Id,
        string Name,
        string Material,
        double DiameterMm,
        double BreakingLoadKn,
        double WeightWaterKgM,
        double DragCoefficient);

    private sealed record CanonicalConnector(
        string Id,
        string Name,
        string Type,
        double WeightAirKg,
        double VolumeM3,
        double BreakingLoadKn,
        double ProjectedAreaM2,
        double DragCoefficient);

    private sealed record CanonicalAnchor(
        string Name,
        string Type,
        string Material,
        double WeightAirKg,
        double VolumeM3,
        double BaseHoldingCoefficient);

    private sealed record CanonicalResult(
        string Schema,
        CalculationResult Result,
        SelectedShapeReadModel? SelectedShape,
        MooringSignedCandidateResult? SignedCandidate,
        MooringSignedPhysicalDispositionState? PhysicalDisposition,
        MooringSelectedShapeResult? SelectedCore,
        MooringSelectedDesignEnvelopeState? DesignEnvelope,
        MooringSelectedDesignTensionDemandState? DesignTensionDemand,
        MooringSelectedAnchorReactionState? AnchorReaction,
        MooringSelectedLocalElementDemandState? LocalElementDemand,
        MooringSelectedLocalStructuralCapacityState? LocalStructuralCapacity,
        MooringSelectedEngineeringAssessmentState? EngineeringAssessment);
}

internal static class CalculationRunProvenanceFactory
{
    internal static CalculationRunProvenance Create(
        string inputHash,
        CalculationSnapshot snapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputHash);
        ArgumentNullException.ThrowIfNull(snapshot);

        return new CalculationRunProvenance(
            Guid.NewGuid().ToString("D"),
            DateTimeOffset.UtcNow,
            inputHash,
            CalculationRunFingerprint.ComputeResultHash(snapshot),
            CalculationSourceIdentity.Current);
    }
}

public static class CalculationSourceIdentity
{
    public static string Current { get; } = Build();

    private static string Build()
    {
        var assembly = typeof(ApplicationCalculationRunner).Assembly;
        var name = assembly.GetName().Name ?? "BuoyCalc.Windows";
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        var version = string.IsNullOrWhiteSpace(informational)
            ? assembly.GetName().Version?.ToString() ?? "unknown"
            : informational.Trim();

        var plus = version.LastIndexOf('+');
        var revision = plus >= 0 ? version[(plus + 1)..] : string.Empty;
        var exactRevision = revision.Length == 40 && revision.All(Uri.IsHexDigit)
            ? revision.ToLowerInvariant()
            : "unavailable";

        return $"{name}/{version};source-revision={exactRevision}";
    }
}
