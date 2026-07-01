namespace BuoyCalc.Windows.Services;

public static class TechnicalReportStorePublisher
{
    public static void Publish(TechnicalReportData data)
    {
        MooringShapeStore.Set(data.Shape);
        MooringIterativeSolverStore.Set(data.IterativeSolver);
    }
}
