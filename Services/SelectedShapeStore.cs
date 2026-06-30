namespace BuoyCalc.Windows.Services;

public sealed record SelectedShapeReadModel(
    MooringShapeResult Shape,
    string Source,
    bool UsesDiscreteLoads,
    bool HasGateSelection,
    MooringPrimaryShapeGateDecision? GateDecision,
    string DecisionText,
    string MethodNote);

public static class SelectedShapeStore
{
    public static SelectedShapeReadModel? Current => BuildCurrent();

    public static SelectedShapeReadModel? BuildCurrent()
    {
        var selection = MooringPrimaryShapeSelectionStore.Current;
        if (selection is not null)
        {
            return new SelectedShapeReadModel(
                selection.Shape,
                selection.Source,
                selection.UsesDiscreteLoads,
                true,
                selection.Gate.Decision,
                selection.Gate.DecisionText,
                selection.MethodNote);
        }

        var fallbackShape = MooringShapeStore.Current;
        if (fallbackShape is null)
        {
            return null;
        }

        return new SelectedShapeReadModel(
            fallbackShape,
            "MooringShapeStore.Current",
            false,
            false,
            null,
            "Форма выбрана без gate selection; используется текущая форма MooringShapeStore.",
            "SelectedShapeStore: read-model fallback для этапа архитектурной стабилизации. Расчёт формы не выполняется.");
    }
}
