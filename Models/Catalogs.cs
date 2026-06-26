using System.Collections.Generic;
using System.Linq;

namespace BuoyCalc.Windows.Models;

public static class RopeCatalog
{
    public static IReadOnlyList<RopePreset> Presets { get; } = new List<RopePreset>
    {
        new("polyester_20", "Polyester 20 mm", "Polyester / Полиэстер", 20, 70, 0.15, 1.20, "Учебный пресет синтетического каната."),
        new("polyester_30", "Polyester 30 mm", "Polyester / Полиэстер", 30, 140, 0.34, 1.20, "Учебный пресет синтетического каната."),
        new("chain_10", "Chain 10 mm", "Steel chain / Стальная цепь", 10, 90, 1.90, 2.00, "Учебный пресет цепи."),
        new("steel_wire_8", "Steel wire 8 mm", "Steel wire / Стальной трос", 8, 45, 0.28, 1.10, "Учебный пресет стального троса.")
    };

    public static RopePreset ById(string id) => Presets.FirstOrDefault(x => x.Id == id) ?? Presets[0];
}

public static class ConnectorCatalog
{
    public static IReadOnlyList<ConnectorPreset> Presets { get; } = new List<ConnectorPreset>
    {
        new("shackle_55", "Shackle 55 kN", "Скоба", 1.2, 0.00008, 55, 0.004, 1.2, "Учебный пресет скобы."),
        new("shackle_85", "Shackle 85 kN", "Усиленная скоба", 2.1, 0.00014, 85, 0.006, 1.2, "Учебный пресет усиленной скобы."),
        new("swivel_60", "Swivel 60 kN", "Вертлюг", 2.5, 0.00018, 60, 0.010, 1.3, "Учебный пресет вертлюга."),
        new("acoustic_release_35", "Acoustic Release 35 kN", "Акустический релиз", 18.0, 0.006, 35, 0.050, 1.0, "Учебный пресет акустического релиза как соединительного элемента.")
    };

    public static ConnectorPreset ById(string id) => Presets.FirstOrDefault(x => x.Id == id) ?? Presets[0];
}

public static class AnchorCatalog
{
    public static IReadOnlyList<AnchorPreset> Presets { get; } = new List<AnchorPreset>
    {
        new("concrete_500", "Concrete 500 kg", "Deadweight", "Concrete / Бетон", 500, 0.20, 1.0, "Учебный бетонный груз."),
        new("concrete_1000", "Concrete 1000 kg", "Deadweight", "Concrete / Бетон", 1000, 0.40, 1.0, "Учебный бетонный груз."),
        new("mushroom_300", "Mushroom 300 kg", "Mushroom", "Steel / Сталь", 300, 0.038, 2.5, "Учебный грибовидный якорь."),
        new("plate_250", "Plate 250 kg", "Plate", "Steel / Сталь", 250, 0.032, 4.0, "Учебный плитный якорь.")
    };

    public static AnchorPreset ById(string id) => Presets.FirstOrDefault(x => x.Id == id) ?? Presets[0];
}

public static class SeabedCatalog
{
    public static IReadOnlyList<SeabedPreset> Presets { get; } = new List<SeabedPreset>
    {
        new("unknown", "Неизвестный грунт", 1.0, "Без поправки на грунт."),
        new("mud", "Ил", 1.3, "Условно лучше удержание для некоторых якорей."),
        new("sand", "Песок", 1.1, "Умеренное удержание."),
        new("rock", "Камень", 0.7, "Сниженное удержание для грузовых якорей.")
    };

    public static SeabedPreset ById(string id) => Presets.FirstOrDefault(x => x.Id == id) ?? Presets[0];
}
