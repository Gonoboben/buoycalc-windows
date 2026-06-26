namespace BuoyCalc.Windows.Models;

public sealed class RopeLibraryItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = "User";
    public string Material { get; set; } = string.Empty;
    public double DiameterMm { get; set; }
    public double BreakingLoadKn { get; set; }
    public double WeightWaterKgM { get; set; }
    public double DragCoefficient { get; set; }
    public string Note { get; set; } = string.Empty;

    public string DisplayName => $"{Name} · {DiameterMm:0.##} мм · MBL={BreakingLoadKn:0.##} кН";

    public override string ToString() => DisplayName;

    public RopePreset ToRopePreset()
    {
        return new RopePreset(Id, Name, Material, DiameterMm, BreakingLoadKn, WeightWaterKgM, DragCoefficient, Note);
    }
}
