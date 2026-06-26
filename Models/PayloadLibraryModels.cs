namespace BuoyCalc.Windows.Models;

public sealed class PayloadLibraryItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = "User";
    public string Type { get; set; } = string.Empty;
    public double WeightAirKg { get; set; }
    public double VolumeM3 { get; set; }
    public double ProjectedAreaM2 { get; set; }
    public double DragCoefficient { get; set; }
    public string Note { get; set; } = string.Empty;

    public string DisplayName => $"{Name} · {Type} · {WeightAirKg:0.##} кг · A={ProjectedAreaM2:0.###} м²";

    public override string ToString() => DisplayName;
}
