using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public sealed record RopeCoefficientMetadataResult(
    double LegacyDragCoefficient,
    double? ExplicitNormalDragCoefficient,
    double EffectiveNormalCoefficient,
    string NormalSource,
    double? ExplicitTangentialDragCoefficient,
    string TangentialSource,
    bool TangentialDataAvailable,
    string MethodNote);

public static class RopeCoefficientMetadataResolver
{
    public static RopeCoefficientMetadataResult Resolve(RopeLibraryItem rope)
    {
        var explicitNormal = FiniteNullable(rope.NormalDragCoefficient);
        var explicitTangential = FiniteNullable(rope.TangentialDragCoefficient);
        var effectiveNormal = explicitNormal ?? rope.DragCoefficient;

        return new RopeCoefficientMetadataResult(
            rope.DragCoefficient,
            explicitNormal,
            effectiveNormal,
            explicitNormal.HasValue ? "ExplicitNormalDragCoefficient" : "LegacyDragCoefficient",
            explicitTangential,
            explicitTangential.HasValue ? "ExplicitTangentialDragCoefficient" : "Unavailable",
            explicitTangential.HasValue,
            "Data-only metadata resolver. Existing production calculations continue to consume RopePreset.DragCoefficient; explicit normal/tangential metadata is not wired into force or solver physics.");
    }

    private static double? FiniteNullable(double? value)
    {
        return value.HasValue && double.IsFinite(value.Value) ? value.Value : null;
    }
}
