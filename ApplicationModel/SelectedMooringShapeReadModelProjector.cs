using BuoyCalc.Windows.Services;

namespace BuoyCalc.Windows.ApplicationModel;

/// <summary>
/// Projects the already-arbitrated typed selected-core result into the user-facing
/// selected-shape read model. Selection authority remains in calculation-core types.
/// </summary>
public static class SelectedMooringShapeReadModelProjector
{
    public static SelectedShapeReadModel Project(
        SelectedShapeReadModel legacySelected,
        MooringSelectedShapeResult? selectedCore)
    {
        ArgumentNullException.ThrowIfNull(legacySelected);

        // A non-signed arbitration result is the existing production authority.
        // Preserve the complete legacy read model exactly, including its gate metadata.
        if (selectedCore is null ||
            selectedCore.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback)
        {
            return legacySelected;
        }

        return new SelectedShapeReadModel(
            selectedCore.Shape,
            selectedCore.SourceIdentity.ToString(),
            selectedCore.SelectedUsesDiscreteLoads,
            HasGateSelection: false,
            GateDecision: null,
            DecisionText: "Выбрана принятая форма signed boundary-feedback; legacy iterative gate к этому источнику не применяется.",
            MethodNote: selectedCore.MethodNote);
    }
}
