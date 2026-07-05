from pathlib import Path

path = Path("ViewModels/MainWindowViewModel.cs")
text = path.read_text(encoding="utf-8")

old = '''    private void UpdateCurrentProfileSummary()
    {
        if (!UseCurrentProfile)
        {
            CurrentProfileSummary = $"Профиль течения отключён. Используется одно значение скорости: {CurrentSpeed} м/с.";
            return;
        }

        if (CurrentProfilePoints.Count == 0)
        {
            CurrentProfileSummary = "Профиль включён, но точки не заданы. Будет использовано одно значение скорости.";
            return;
        }

        var inputs = CurrentProfilePoints.Select(x => x.ToInput()).OrderBy(x => x.DepthM).ToList();
        var maxSpeed = inputs.Max(x => x.HorizontalSpeedMS);
        var minDepth = inputs.Min(x => x.DepthM);
        var maxDepth = inputs.Max(x => x.DepthM);
        CurrentProfileSummary = $"Профиль включён: {inputs.Count} точек, глубины {minDepth:0.##}–{maxDepth:0.##} м, max |Uгор|={maxSpeed:0.###} м/с. В v0.19 расчёт использует эту max-скорость как переходную оценку.";
    }
'''

new = '''    private void UpdateCurrentProfileSummary()
    {
        if (!UseCurrentProfile || CurrentProfilePoints.Count == 0)
        {
            CurrentProfileSummary = MainWindowCurrentProfileSummaryBuilder.Build(
                UseCurrentProfile,
                CurrentSpeed,
                Array.Empty<CurrentProfilePointInput>());
            return;
        }

        var inputs = CurrentProfilePoints.Select(x => x.ToInput()).ToList();
        CurrentProfileSummary = MainWindowCurrentProfileSummaryBuilder.Build(
            UseCurrentProfile,
            CurrentSpeed,
            inputs);
    }
'''

count = text.count(old)
if count != 1:
    raise SystemExit(f"Expected exactly one UpdateCurrentProfileSummary block, found {count}")

path.write_text(text.replace(old, new), encoding="utf-8")
