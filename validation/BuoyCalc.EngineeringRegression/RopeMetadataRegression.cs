using System.Text.Json;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class RopeMetadataRegression
{
    private const double Tol = 1e-12;

    public static void Validate()
    {
        ValidateLegacyJson();
        ValidateExplicitNormalOnly();
        ValidateExplicitNormalAndTangential();
        ValidateMissingVersusExplicitZero();
        ValidateRoundTrip();
        ValidateBuiltInsRemainLegacyOnly();
        ValidateMissingIncomingPreservesMetadata();
        ValidateExplicitIncomingWins();
    }

    private static void ValidateLegacyJson()
    {
        const string json = "{\"Id\":\"user:legacy\",\"Name\":\"Legacy\",\"DragCoefficient\":1.2}";
        var item = JsonSerializer.Deserialize<RopeLibraryItem>(json)
            ?? throw new InvalidOperationException("Rope metadata regression: legacy JSON did not deserialize.");
        Require(!item.NormalDragCoefficient.HasValue && !item.TangentialDragCoefficient.HasValue, "legacy JSON must not manufacture metadata");
        var resolved = RopeCoefficientMetadataResolver.Resolve(item);
        Near(1.2, resolved.LegacyDragCoefficient, "legacy Cd");
        Near(1.2, resolved.EffectiveNormalCoefficient, "legacy effective normal");
        Require(resolved.NormalSource == "LegacyDragCoefficient", "legacy normal source");
        Require(!resolved.TangentialDataAvailable, "legacy tangential unavailable");
        Near(1.2, item.ToRopePreset().DragCoefficient, "legacy production Cd unchanged");
    }

    private static void ValidateExplicitNormalOnly()
    {
        var item = NewItem(1.2, 1.05, null);
        var resolved = RopeCoefficientMetadataResolver.Resolve(item);
        Near(1.05, resolved.EffectiveNormalCoefficient, "explicit normal effective");
        Require(resolved.NormalSource == "ExplicitNormalDragCoefficient", "explicit normal source");
        Require(!resolved.TangentialDataAvailable, "normal-only tangential unavailable");
        Near(1.2, item.ToRopePreset().DragCoefficient, "normal-only production Cd unchanged");
    }

    private static void ValidateExplicitNormalAndTangential()
    {
        var item = NewItem(1.2, 1.05, 0.12);
        var resolved = RopeCoefficientMetadataResolver.Resolve(item);
        Near(1.05, resolved.EffectiveNormalCoefficient, "full explicit normal");
        Near(0.12, resolved.ExplicitTangentialDragCoefficient ?? double.NaN, "full explicit tangential");
        Require(resolved.TangentialDataAvailable, "full explicit tangential available");
        Near(1.2, item.ToRopePreset().DragCoefficient, "full metadata production Cd unchanged");
    }

    private static void ValidateMissingVersusExplicitZero()
    {
        var missing = RopeCoefficientMetadataResolver.Resolve(NewItem(1.2, 1.05, null));
        var zero = RopeCoefficientMetadataResolver.Resolve(NewItem(1.2, 1.05, 0.0));
        Require(!missing.TangentialDataAvailable && missing.ExplicitTangentialDragCoefficient is null, "missing tangential remains unavailable");
        Require(zero.TangentialDataAvailable, "explicit zero tangential is available data");
        Near(0.0, zero.ExplicitTangentialDragCoefficient ?? double.NaN, "explicit zero tangential value");
    }

    private static void ValidateRoundTrip()
    {
        var original = NewItem(1.2, 1.05, null);
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<RopeLibraryItem>(json)
            ?? throw new InvalidOperationException("Rope metadata regression: round-trip JSON did not deserialize.");
        Near(1.2, restored.DragCoefficient, "round-trip legacy Cd");
        Near(1.05, restored.NormalDragCoefficient ?? double.NaN, "round-trip explicit normal");
        Require(restored.TangentialDragCoefficient is null, "round-trip missing tangential remains null");
    }

    private static void ValidateBuiltInsRemainLegacyOnly()
    {
        foreach (var item in RopeLibraryStorage.BuiltInRopes)
        {
            Require(item.NormalDragCoefficient is null && item.TangentialDragCoefficient is null, $"built-in {item.Id} remains legacy-only");
            Near(item.DragCoefficient, item.ToRopePreset().DragCoefficient, $"built-in {item.Id} production Cd unchanged");
        }
    }

    private static void ValidateMissingIncomingPreservesMetadata()
    {
        var existing = NewItem(1.2, 1.05, 0.12);
        var incoming = NewItem(1.3, null, null);
        var merged = RopeCoefficientMetadataResolver.MergeOptionalMetadata(existing, incoming);
        Near(1.3, merged.DragCoefficient, "incoming legacy Cd retained");
        Near(1.05, merged.NormalDragCoefficient ?? double.NaN, "normal metadata preserved");
        Near(0.12, merged.TangentialDragCoefficient ?? double.NaN, "tangential metadata preserved");
    }

    private static void ValidateExplicitIncomingWins()
    {
        var existing = NewItem(1.2, 1.05, 0.12);
        var incoming = NewItem(1.3, 0.95, 0.0);
        var merged = RopeCoefficientMetadataResolver.MergeOptionalMetadata(existing, incoming);
        Near(0.95, merged.NormalDragCoefficient ?? double.NaN, "explicit incoming normal wins");
        Near(0.0, merged.TangentialDragCoefficient ?? double.NaN, "explicit incoming zero tangential wins");
    }

    private static RopeLibraryItem NewItem(double legacy, double? normal, double? tangential)
    {
        return new RopeLibraryItem
        {
            Id = "user:test",
            Name = "Test",
            DragCoefficient = legacy,
            NormalDragCoefficient = normal,
            TangentialDragCoefficient = tangential
        };
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("Rope metadata regression failed: " + label);
    }

    private static void Near(double expected, double actual, string label)
    {
        if (!double.IsFinite(actual) || Math.Abs(expected - actual) > Tol * Math.Max(1.0, Math.Abs(expected)))
            throw new InvalidOperationException($"Rope metadata regression {label}: expected {expected:R}, got {actual:R}.");
    }
}
