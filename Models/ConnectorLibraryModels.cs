namespace BuoyCalc.Windows.Models;

public sealed class ConnectorLibraryItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = "User";
    public string Type { get; set; } = string.Empty;
    public double WeightAirKg { get; set; }
    public double VolumeM3 { get; set; }
    public double BreakingLoadKn { get; set; }
    public double ProjectedAreaM2 { get; set; }
    public double DragCoefficient { get; set; }
    public string Note { get; set; } = string.Empty;

    public string DisplayName => $"{Name} · {Type} · MBL={BreakingLoadKn:0.##} кН";

    public override string ToString() => DisplayName;

    public ConnectorPreset ToConnectorPreset()
    {
        return new ConnectorPreset(Id, Name, Type, WeightAirKg, VolumeM3, BreakingLoadKn, ProjectedAreaM2, DragCoefficient, Note);
    }
}
