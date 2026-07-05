from pathlib import Path

path = Path("ViewModels/MainWindowViewModel.cs")
text = path.read_text(encoding="utf-8")

old_buoy = '''    private void ApplySelectedBuoyPreset()
    {
        if (SelectedBuoyPreset is null) return;
        BuoyName = SelectedBuoyPreset.Name;
        BuoyVolume = FormatDouble(SelectedBuoyPreset.VolumeM3);
        BuoyWeight = FormatDouble(SelectedBuoyPreset.WeightKg);
        BuoyArea = FormatDouble(SelectedBuoyPreset.ProjectedAreaM2);
        BuoyCd = FormatDouble(SelectedBuoyPreset.DragCoefficient);
    }
'''

new_buoy = '''    private void ApplySelectedBuoyPreset()
    {
        if (SelectedBuoyPreset is null) return;
        var display = MainWindowSelectedPresetDisplayBuilder.BuildBuoy(SelectedBuoyPreset);
        BuoyName = display.Name;
        BuoyVolume = display.Volume;
        BuoyWeight = display.Weight;
        BuoyArea = display.ProjectedArea;
        BuoyCd = display.DragCoefficient;
    }
'''

old_anchor = '''    private void ApplySelectedAnchorPreset()
    {
        if (SelectedAnchorPreset is null) return;
        AnchorName = SelectedAnchorPreset.Name;
        AnchorType = SelectedAnchorPreset.Type;
        AnchorMaterial = SelectedAnchorPreset.Material;
        AnchorWeight = FormatDouble(SelectedAnchorPreset.WeightAirKg);
        AnchorVolume = FormatDouble(SelectedAnchorPreset.VolumeM3);
        AnchorCoefficient = FormatDouble(SelectedAnchorPreset.BaseHoldingCoefficient);
    }
'''

new_anchor = '''    private void ApplySelectedAnchorPreset()
    {
        if (SelectedAnchorPreset is null) return;
        var display = MainWindowSelectedPresetDisplayBuilder.BuildAnchor(SelectedAnchorPreset);
        AnchorName = display.Name;
        AnchorType = display.Type;
        AnchorMaterial = display.Material;
        AnchorWeight = display.Weight;
        AnchorVolume = display.Volume;
        AnchorCoefficient = display.BaseHoldingCoefficient;
    }
'''

for label, old in (("ApplySelectedBuoyPreset", old_buoy), ("ApplySelectedAnchorPreset", old_anchor)):
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"Expected exactly one {label} block, found {count}")

text = text.replace(old_buoy, new_buoy)
text = text.replace(old_anchor, new_anchor)
path.write_text(text, encoding="utf-8")
