namespace BuoyCalc.Windows.Services;

public sealed record SelectedShapeReadModel(
    MooringShapeResult Shape,
    string Source,
    bool UsesDiscreteLoads,
    bool HasGateSelection,
    MooringPrimaryShapeGateDecision? GateDecision,
    string DecisionText,
    string MethodNote);
