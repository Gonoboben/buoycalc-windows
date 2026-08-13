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
    }

    private static void ValidateLegacyJson()
    {
        const string json = "{\"Id\":\"user:legacy\",\"Name\":\"Legacy\",\"DragCoefficient\":1.2}";
        var item = JsonSerializer.Deserialize<RopeLibraryItem>(json)
            ?? throw new InvalidOperationException("Rope metadata regression: legacy JSON did not deserialize.");

        if (item.NormalDragCoefficient.HasValue || item.TangentialDragCoefficient.HasValue)
            throw new InvalidOperationException("Rope metadata regression: legacy JSON must not manufacture explicit metadata.");

        var resolved = RopeCoefficientMetadataResolver.Resolve(item);
        Near(1.2, resolved.LegacyDragCoefficient, "legacy Cd");
        Near(1.2, resolved.EffectiveNormalCoefficient, "legacy effective normal");
        Require(resolved.NormalSource == "LegacyDragCoefficient", "legacy normal source");
        Require(!resolved.TangentialDataAvailable, "legacy tangential unavailable");

        var preset = item.ToRopePreset();
        Near(1.2, preset.DragCoefficient, "legacy RopePreset Cd unchanged");
    }

    private static void ValidateExplicitNormalOnly()
    {
        var item = NewItem(legacy: 1.2, normal: 1.05, tangential: null);
        var resolved = RopeCoefficientMetadataResolver.Resolve(item);
        Near(1.05, resolved.EffectiveNormalCoefficient, "explicit normal effective");
        Require(resolved.NormalSource == "ExplicitNormalDragCoefficient", "explicit normal source");
        Require(!resolved.TangentialDataAvailable, "normal-only tangential unavailable");
        Near(1.2, item.ToRopePreset().DragCoefficient, "normal-only production Cd unchanged");
    }

    private static void ValidateExplicitNormalAndTangential()
    {
        var item = NewItem(legacy: 1.2, normal: 1.05, tangential: 0.12);
        var resolved = RopeCoefficientMetadataResolver.Resolve(item);
        Near(1.05, resolved.EffectiveNormalCoefficient, "full explicit normal");
        Near(0.12, resolved.ExplicitTangentialDragCoefficient ?? double.NaN, "full explicit tangential");
        Require(resolved.TangentialDataAvailable, "full explicit tangential available");
        Require(resolved.TangentialSource == "ExplicitTangentialDragCoefficient", "full tangential source");
        Near(1.2, item.ToRopePreset().DragCoefficient, "full metadata production Cd unchanged");
    }

    private static void ValidateMissingVersusExplicitZero()
    {
        var missing = RopeCoefficientMetadataResolver.Resolve(NewItem(1.2, 1.05, null));
        var zero = RopeCoefficientMetadataResolver.Resolve(NewItem(1.2, 1.05, 0.0));

        Require(!missing.TangentialDataAvailable, "missing tangential must be unavailable");
        Require(missing.ExplicitTangentialDragCoefficient is null, "missing tangential must remain null");
        Require(zero.TangentialDataAvailable, "explicit zero tangential must be available data");
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
            Require(item.NormalDragCoefficient is null, $"built-in {item.Id} explicit normal must remain null");
            Require(item.TangentialDragCoefficient is null, $"built-in {item.Id} explicit tangential must remain null");
            Near(item.DragCoefficient, item.ToRopePreset().DragCoefficient, $"built-in {item.Id} production Cd unchanged");
        }
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
