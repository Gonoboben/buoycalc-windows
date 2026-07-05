from pathlib import Path

path = Path("ViewModels/MainWindowViewModel.cs")
text = path.read_text(encoding="utf-8")

old_profile = '''    private void ResetCurrentProfile()
    {
        ClearCurrentProfilePoints();
        AddCurrentProfilePoint(new CurrentProfilePointViewModel { DepthM = "0", EastCurrentMS = CurrentSpeed, NorthCurrentMS = "0", VerticalCurrentMS = "0", WaterDensityKgM3 = WaterDensity });
        AddCurrentProfilePoint(new CurrentProfilePointViewModel { DepthM = "10", EastCurrentMS = "0.45", NorthCurrentMS = "0", VerticalCurrentMS = "0", WaterDensityKgM3 = WaterDensity });
        AddCurrentProfilePoint(new CurrentProfilePointViewModel { DepthM = "25", EastCurrentMS = "0.3", NorthCurrentMS = "0", VerticalCurrentMS = "0", WaterDensityKgM3 = WaterDensity });
        AddCurrentProfilePoint(new CurrentProfilePointViewModel { DepthM = Depth, EastCurrentMS = "0.1", NorthCurrentMS = "0", VerticalCurrentMS = "0", WaterDensityKgM3 = WaterDensity });
        UpdateCurrentProfileSummary();
    }
'''

new_profile = '''    private void ResetCurrentProfile()
    {
        var template = MainWindowDefaultProjectTemplateBuilder.Build();
        ClearCurrentProfilePoints();
        foreach (var pointTemplate in template.CurrentProfilePoints)
        {
            AddCurrentProfilePoint(CreateDefaultCurrentProfilePoint(pointTemplate));
        }
        UpdateCurrentProfileSummary();
    }
'''

old_project = '''    private void ResetToDefaultProject()
    {
        ProjectName = "Тестовый проект";
        ProjectFilePath = ProjectJsonStorage.DefaultProjectPath;
        WaterDensity = "1025";
        Depth = "50";
        CurrentSpeed = "0.5";
        UseCurrentProfile = false;
        WaveHeight = "1.0";
        WavePeriod = "6.0";
        SelectedSeabedPreset = SeabedCatalog.ById("unknown");
        SelectedBuoyPreset = BuoyPresets.FirstOrDefault();
        SelectedAnchorPreset = AnchorPresets.FirstOrDefault(x => x.Id == "built-in:concrete_500") ?? AnchorPresets.FirstOrDefault();
        SafetyFactor = "5";
        ResultText = "Нажмите «Рассчитать».";
        ReportText = "";
        ElementRows.Clear();
        SequenceDiagramLines.Clear();

        ClearCurrentProfilePoints();
        ResetCurrentProfile();
        UseCurrentProfile = false;

        ClearAssemblyItems();
        AddAssemblyItem(new AssemblyItemViewModel { Kind = "Connector", Title = "Скоба под буем", ConnectorPresetStorageId = "built-in:shackle_55", Count = "1" });
        AddAssemblyItem(new AssemblyItemViewModel { Kind = "Line", Title = "Верхний буйреп", RopePresetStorageId = "built-in:polyester_20", LengthM = "45" });
        AddAssemblyItem(new AssemblyItemViewModel { Kind = "Connector", Title = "Вертлюг", ConnectorPresetStorageId = "built-in:swivel_60", Count = "1" });
        AddAssemblyItem(new AssemblyItemViewModel { Kind = "Payload", Title = "ADCP", PayloadPresetStorageId = "built-in:adcp_40" });
        AddAssemblyItem(new AssemblyItemViewModel { Kind = "Line", Title = "Нижняя цепь", RopePresetStorageId = "built-in:chain_10", LengthM = "10" });

        UpdateSequenceSummary();
    }
'''

new_project = '''    private void ResetToDefaultProject()
    {
        var template = MainWindowDefaultProjectTemplateBuilder.Build();

        ProjectName = template.ProjectName;
        ProjectFilePath = template.ProjectFilePath;
        WaterDensity = template.WaterDensity;
        Depth = template.Depth;
        CurrentSpeed = template.CurrentSpeed;
        UseCurrentProfile = template.UseCurrentProfile;
        WaveHeight = template.WaveHeight;
        WavePeriod = template.WavePeriod;
        SelectedSeabedPreset = SeabedCatalog.ById(template.SeabedPresetId);
        SelectedBuoyPreset = template.BuoyPresetId is null
            ? BuoyPresets.FirstOrDefault()
            : BuoyPresets.FirstOrDefault(x => x.Id == template.BuoyPresetId) ?? BuoyPresets.FirstOrDefault();
        SelectedAnchorPreset = AnchorPresets.FirstOrDefault(x => x.Id == template.PreferredAnchorPresetId) ?? AnchorPresets.FirstOrDefault();
        SafetyFactor = template.SafetyFactor;
        ResultText = template.ResultText;
        ReportText = template.ReportText;
        ElementRows.Clear();
        SequenceDiagramLines.Clear();

        ClearCurrentProfilePoints();
        ResetCurrentProfile();
        UseCurrentProfile = template.UseCurrentProfile;

        ClearAssemblyItems();
        foreach (var itemTemplate in template.AssemblyItems)
        {
            AddAssemblyItem(CreateDefaultAssemblyItem(itemTemplate));
        }

        UpdateSequenceSummary();
    }

    private CurrentProfilePointViewModel CreateDefaultCurrentProfilePoint(MainWindowDefaultCurrentProfilePointTemplate template)
    {
        return new CurrentProfilePointViewModel
        {
            DepthM = ResolveDefaultProjectValue(template.DepthM),
            EastCurrentMS = ResolveDefaultProjectValue(template.EastCurrentMS),
            NorthCurrentMS = ResolveDefaultProjectValue(template.NorthCurrentMS),
            VerticalCurrentMS = ResolveDefaultProjectValue(template.VerticalCurrentMS),
            WaterDensityKgM3 = ResolveDefaultProjectValue(template.WaterDensityKgM3)
        };
    }

    private string ResolveDefaultProjectValue(MainWindowDefaultProjectValue value)
    {
        return value.Source switch
        {
            MainWindowDefaultProjectValueSource.CurrentSpeed => CurrentSpeed,
            MainWindowDefaultProjectValueSource.Depth => Depth,
            MainWindowDefaultProjectValueSource.WaterDensity => WaterDensity,
            _ => value.Value
        };
    }

    private static AssemblyItemViewModel CreateDefaultAssemblyItem(MainWindowDefaultAssemblyItemTemplate template)
    {
        var item = new AssemblyItemViewModel
        {
            Kind = template.Kind,
            Title = template.Title
        };

        if (template.RopePresetStorageId is not null) item.RopePresetStorageId = template.RopePresetStorageId;
        if (template.ConnectorPresetStorageId is not null) item.ConnectorPresetStorageId = template.ConnectorPresetStorageId;
        if (template.PayloadPresetStorageId is not null) item.PayloadPresetStorageId = template.PayloadPresetStorageId;
        if (template.LengthM is not null) item.LengthM = template.LengthM;
        if (template.Count is not null) item.Count = template.Count;

        return item;
    }
'''

for label, old in (("ResetCurrentProfile", old_profile), ("ResetToDefaultProject", old_project)):
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"Expected exactly one {label} block, found {count}")

text = text.replace(old_profile, new_profile)
text = text.replace(old_project, new_project)
path.write_text(text, encoding="utf-8")
