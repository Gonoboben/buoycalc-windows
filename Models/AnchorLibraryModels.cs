namespace BuoyCalc.Windows.Models;

public sealed class AnchorLibraryItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = "User";
    public string Type { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public double WeightAirKg { get; set; }
    public double VolumeM3 { get; set; }
    public double BaseHoldingCoefficient { get; set; }
    public string Note { get; set; } = string.Empty;

    public string DisplayName => $"{Name} · {Type} · {WeightAirKg:0.##} кг · K={BaseHoldingCoefficient:0.##}";

    public override string ToString() => DisplayName;

    public AnchorPreset ToAnchorPreset()
    {
        return new AnchorPreset(Id, Name, Type, Material, WeightAirKg, VolumeM3, BaseHoldingCoefficient, Note);
    }
}
