namespace BuoyCalc.Windows.Services;

public sealed record SelectedShapeReadModel(
    MooringShapeResult Shape,
    string Source,
    bool UsesDiscreteLoads,
    bool HasGateSelection,
    MooringPrimaryShapeGateDecision? GateDecision,
    string DecisionText,
    string MethodNote)
{
    public string SourceDescription =>
        string.Equals(Source, MooringShapeSourceIdentity.SignedBoundaryFeedback.ToString(), StringComparison.Ordinal)
            ? "signed boundary-feedback форма"
            : UsesDiscreteLoads
                ? "дискретно-массовая форма"
                : "резервная форма";
}
