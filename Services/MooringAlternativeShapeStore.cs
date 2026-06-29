namespace BuoyCalc.Windows.Services;

public sealed record MooringAlternativeShapeDisplayData(
    MooringDiscreteLoadShapeResult Shape,
    MooringAlternativeDiscreteNodeResult DiscreteNodes);

public static class MooringAlternativeShapeStore
{
    public static MooringAlternativeShapeDisplayData? Current { get; private set; }

    public static void Set(MooringDiscreteLoadShapeResult shape, MooringAlternativeDiscreteNodeResult discreteNodes)
    {
        Current = new MooringAlternativeShapeDisplayData(shape, discreteNodes);
    }

    public static void Clear()
    {
        Current = null;
    }
}
