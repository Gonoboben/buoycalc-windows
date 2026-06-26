namespace BuoyCalc.Windows.Models;

public sealed class BuoyLibraryItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = "User";
    public double VolumeM3 { get; set; }
    public double WeightKg { get; set; }
    public double ProjectedAreaM2 { get; set; }
    public double DragCoefficient { get; set; }
    public string Note { get; set; } = string.Empty;

    public string DisplayName => $"{Name} · V={VolumeM3:0.###} м³ · {WeightKg:0.##} кг";
}
